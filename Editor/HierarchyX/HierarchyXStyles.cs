using UnityEditor;
using UnityEngine;

namespace HierarchyX {
    /// <summary>Cached GUI styles, colors and generated textures for HierarchyX.</summary>
    public static class HierarchyXStyles {

        private static GUIStyle miniLabel;
        private static GUIStyle transparentButton;
        private static Texture2D gradient;

        public static readonly Color prefabColor = new Color(0.3f, 0.5f, 0.83f, 1f);
        public static readonly Color brokenPrefabColor = new Color(0.7f, 0.4f, 0.35f, 1f);

        public static GUIStyle MiniLabelStyle {
            get {
                if (miniLabel == null) {
                    miniLabel = new GUIStyle(EditorStyles.miniLabel) {
                        alignment = TextAnchor.MiddleRight,
                        richText = false,
                        padding = new RectOffset(0, 0, 0, 0),
                        margin = new RectOffset(0, 0, 0, 0),
                        clipping = TextClipping.Overflow,
                    };
                }
                return miniLabel;
            }
        }

        public static GUIStyle TransparentButton {
            get {
                if (transparentButton == null)
                    transparentButton = new GUIStyle(GUIStyle.none);
                return transparentButton;
            }
        }

        /// <summary>1x64 horizontal gradient, opaque on the right, transparent on the left.</summary>
        public static Texture2D GradientTexture {
            get {
                if (gradient == null || !gradient) {
                    const int w = 64;
                    gradient = new Texture2D(w, 1, TextureFormat.ARGB32, false, true) {
                        name = "HierarchyX_Gradient",
                        hideFlags = HideFlags.HideAndDontSave,
                        wrapMode = TextureWrapMode.Clamp,
                        filterMode = FilterMode.Bilinear,
                    };
                    for (var x = 0; x < w; x++) {
                        var t = x / (w - 1f);
                        gradient.SetPixel(x, 0, new Color(1f, 1f, 1f, t));
                    }
                    gradient.Apply();
                }
                return gradient;
            }
        }
    }
}
