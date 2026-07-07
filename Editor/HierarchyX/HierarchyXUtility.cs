using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace HierarchyX {
    /// <summary>Misc public-API helpers shared by the HierarchyX draw passes.</summary>
    public static class HierarchyXUtility {

        /// <summary>Row color used to tint tree lines, matching the object's label appearance.</summary>
        public static Color GetHierarchyColor(Transform t) {
            if (!t)
                return Color.clear;
            return GetHierarchyColor(t.gameObject);
        }

        public static Color GetHierarchyColor(GameObject go) {
            if (!go)
                return Color.gray;

            var active = go.activeInHierarchy;
            var status = PrefabUtility.GetPrefabInstanceStatus(go);

            Color c;
            switch (status) {
                case PrefabInstanceStatus.Connected:
                    c = HierarchyXStyles.prefabColor;
                    break;
                case PrefabInstanceStatus.MissingAsset:
                    c = HierarchyXStyles.brokenPrefabColor;
                    break;
                default:
                    c = EditorStyles.label.normal.textColor;
                    break;
            }

            if (!active)
                c *= 0.6f;
            c.a = 1f;
            return c;
        }

        public static bool TransformIsLastChild(Transform t) {
            if (!t || !t.parent)
                return true;
            return t.GetSiblingIndex() == t.parent.childCount - 1;
        }

        /// <summary>Current object plus every other selected non-persistent GameObject.</summary>
        public static List<GameObject> GetSelectedAndCurrent(GameObject current) {
            var list = new List<GameObject>();
            if (current)
                list.Add(current);

            var selected = Selection.gameObjects;
            if (selected.Length <= 1)
                return list;

            for (var i = 0; i < selected.Length; i++) {
                var go = selected[i];
                if (go && !EditorUtility.IsPersistent(go) && !list.Contains(go))
                    list.Add(go);
            }
            return list;
        }
    }
}
