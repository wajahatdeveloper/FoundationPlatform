using UnityEditor;
using UnityEngine;

namespace ProjectWindowX {
    /// <summary>
    /// Hover-only "+" button at the right edge of Project window list rows,
    /// opening a create-actions menu targeted at the row's folder.
    /// </summary>
    internal static class ContextActions {

        private const float Size = 16f;
        private static GUIContent plusIcon;

        /// <summary>Returns the right-edge width consumed so the extension label can sit to its left.</summary>
        internal static float Draw(ProjectWindowX.RowContext ctx, Rect rect) {
            if (!rect.Contains(Event.current.mousePosition))
                return 0f;

            if (plusIcon == null) {
                plusIcon = new GUIContent(EditorGUIUtility.IconContent("Toolbar Plus")) {
                    tooltip = "Create asset here"
                };
            }

            var button = new Rect(rect.xMax - Size - 2f, rect.yMin + (rect.height - Size) * 0.5f, Size, Size);
            if (GUI.Button(button, plusIcon, GUIStyle.none))
                ShowMenu(ctx);

            return Size + 4f;
        }

        private static void ShowMenu(ProjectWindowX.RowContext ctx) {
            var menu = new GenericMenu();
            var folder = CreateAssetActions.TargetFolder(ctx);

            menu.AddItem(new GUIContent("Folder"), false, () => CreateAssetActions.CreateFolder(folder));
            menu.AddSeparator("");

            menu.AddItem(new GUIContent("Script/MonoBehaviour"), false, () => CreateAssetActions.CreateScript(folder, ScriptTemplates.MonoBehaviour, "NewBehaviour"));
            menu.AddItem(new GUIContent("Script/Class"), false, () => CreateAssetActions.CreateScript(folder, ScriptTemplates.PlainClass, "NewClass"));
            menu.AddItem(new GUIContent("Script/Interface"), false, () => CreateAssetActions.CreateScript(folder, ScriptTemplates.Interface, "INewInterface"));
            menu.AddItem(new GUIContent("Script/Struct"), false, () => CreateAssetActions.CreateScript(folder, ScriptTemplates.Struct, "NewStruct"));
            menu.AddItem(new GUIContent("Script/Enum"), false, () => CreateAssetActions.CreateScript(folder, ScriptTemplates.Enum, "NewEnum"));
            menu.AddItem(new GUIContent("Script/ScriptableObject"), false, () => CreateAssetActions.CreateScript(folder, ScriptTemplates.ScriptableObject, "NewScriptableObject"));
            menu.AddItem(new GUIContent("Script/Editor Script"), false, () => CreateAssetActions.CreateScript(folder, ScriptTemplates.EditorScript, "NewEditorScript"));
            menu.AddItem(new GUIContent("Script/Assembly Definition"), false, () => CreateAssetActions.CreateAsmdef(folder));
            menu.AddSeparator("");

            menu.AddItem(new GUIContent("Material"), false, () => CreateAssetActions.CreateMaterial(folder));
            menu.AddItem(new GUIContent("Shader (Unlit)"), false, () => CreateAssetActions.CreateScriptWithExtension(folder, ScriptTemplates.UnlitShader, "NewShader", ".shader"));
            menu.AddItem(new GUIContent("Animation Clip"), false, () => CreateAssetActions.CreateAnimationClip(folder));
            menu.AddItem(new GUIContent("Animator Controller"), false, () => CreateAssetActions.CreateAnimatorController(folder));

            // Row-specific actions
            if (ctx.extension == ".shader")
                menu.AddItem(new GUIContent("Material From This Shader"), false, () => CreateAssetActions.CreateMaterialFromShader(ctx.path));
            if (IsTexture(ctx.extension))
                menu.AddItem(new GUIContent("Material From This Texture"), false, () => CreateAssetActions.CreateMaterialFromTexture(ctx.path));
            if (ctx.extension == ".cs")
                menu.AddItem(new GUIContent("Custom Editor For This Script"), false, () => CreateAssetActions.CreateCustomEditor(ctx.path));
            if (IsAudio(ctx.extension) && AudioPreview.Available) {
                menu.AddItem(new GUIContent("Play Audio"), false, () => AudioPreview.Play(ctx.path));
                menu.AddItem(new GUIContent("Stop Audio"), false, AudioPreview.StopAll);
            }

            menu.ShowAsContext();
        }

        private static bool IsTexture(string ext) {
            return ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".tga"
                || ext == ".psd" || ext == ".tif" || ext == ".tiff" || ext == ".exr" || ext == ".bmp";
        }

        private static bool IsAudio(string ext) {
            return ext == ".wav" || ext == ".mp3" || ext == ".ogg" || ext == ".aiff" || ext == ".aif";
        }
    }
}
