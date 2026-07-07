using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace HierarchyX {

    /// <summary>Draws a small right-aligned badge for a single fact about the row's GameObject.</summary>
    public abstract class MiniLabel {

        protected readonly GUIContent content = new GUIContent();

        /// <summary>Refresh <see cref="content"/> for the given object. Called once per row before measure/draw.</summary>
        public abstract void Fill(GameObject go, HierarchyXSettings s);

        /// <summary>True when the value is a default one and should be drawn dimmed.</summary>
        public abstract bool Faded(GameObject go);

        public bool HasValue { get { return content.text != null && content.text.Length > 0; } }

        public float Measure(GUIStyle style) {
            return HasValue ? style.CalcSize(content).x : 0f;
        }

        /// <summary>Draw at <paramref name="rect"/>. Return true if the value was edited.</summary>
        public virtual bool Draw(Rect rect, GUIStyle style, GameObject go) {
            GUI.Label(rect, content, style);
            return false;
        }

        public static MiniLabel Create(MiniLabelType type) {
            switch (type) {
                case MiniLabelType.Tag: return new TagMiniLabel();
                case MiniLabelType.Layer: return new LayerMiniLabel();
                case MiniLabelType.SortingLayer: return new SortingLayerMiniLabel();
                default: return null;
            }
        }
    }

    public sealed class TagMiniLabel : MiniLabel {
        public override void Fill(GameObject go, HierarchyXSettings s) {
            string tag;
            try { tag = go.tag; } catch { tag = "Untagged"; }
            var hide = s.hideDefaultTag && tag == "Untagged";
            content.text = hide ? string.Empty : tag;
        }

        public override bool Faded(GameObject go) {
            return go.CompareTag("Untagged");
        }

        public override bool Draw(Rect rect, GUIStyle style, GameObject go) {
            GUI.changed = false;
            var tag = EditorGUI.TagField(rect, go.tag, style);
            if (GUI.changed && tag != go.tag) {
                foreach (var obj in HierarchyXUtility.GetSelectedAndCurrent(go)) {
                    Undo.RecordObject(obj, "Change Tag");
                    obj.tag = tag;
                }
                return true;
            }
            return false;
        }
    }

    public sealed class LayerMiniLabel : MiniLabel {
        public override void Fill(GameObject go, HierarchyXSettings s) {
            var hide = s.hideDefaultLayer && go.layer == 0;
            content.text = hide ? string.Empty : LayerMask.LayerToName(go.layer);
        }

        public override bool Faded(GameObject go) {
            return go.layer == 0;
        }

        public override bool Draw(Rect rect, GUIStyle style, GameObject go) {
            GUI.changed = false;
            var layer = EditorGUI.LayerField(rect, go.layer, style);
            if (GUI.changed && layer != go.layer) {
                foreach (var obj in HierarchyXUtility.GetSelectedAndCurrent(go)) {
                    Undo.RecordObject(obj, "Change Layer");
                    obj.layer = layer;
                }
                return true;
            }
            return false;
        }
    }

    public sealed class SortingLayerMiniLabel : MiniLabel {
        private const string Default = "Default";
        private string layerName;
        private int order;

        public override void Fill(GameObject go, HierarchyXSettings s) {
            layerName = Default;
            order = 0;
            var has = false;

            var group = go.GetComponent<SortingGroup>();
            if (group) {
                layerName = group.sortingLayerName;
                order = group.sortingOrder;
                has = true;
            } else {
                var renderer = go.GetComponent<Renderer>();
                if (renderer) {
                    layerName = renderer.sortingLayerName;
                    order = renderer.sortingOrder;
                    has = true;
                }
            }

            content.text = has ? string.Format("{0}:{1}", layerName, order) : string.Empty;
        }

        public override bool Faded(GameObject go) {
            return layerName == Default && order == 0;
        }
    }
}
