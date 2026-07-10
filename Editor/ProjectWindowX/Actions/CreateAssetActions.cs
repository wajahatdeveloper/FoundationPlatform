using System;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.ProjectWindowCallback;
using UnityEngine;

namespace ProjectWindowX {
    /// <summary>
    /// Create-asset implementations for the hover "+" menu. Each action defers
    /// the actual write and drops the new item straight into the Project window's
    /// inline rename field (same flow as Unity's own "Create" menu) — the file is
    /// written at the final path the user types, via <see cref="DeferredCreate"/>.
    /// </summary>
    internal static class CreateAssetActions {

        internal static string TargetFolder(ProjectWindowX.RowContext ctx) {
            if (ctx.isFolder)
                return ctx.path;
            var dir = Path.GetDirectoryName(ctx.path);
            return string.IsNullOrEmpty(dir) ? "Assets" : dir.Replace('\\', '/');
        }

        internal static void CreateScript(string folder, string template, string defaultName) {
            CreateScriptWithExtension(folder, template, defaultName, ".cs");
        }

        internal static void CreateScriptWithExtension(string folder, string template, string defaultName, string extension) {
            StartCreate($"{folder}/{defaultName}{extension}", ScriptIcon(extension), pathName => {
                var name = Path.GetFileNameWithoutExtension(pathName);
                File.WriteAllText(pathName, template.Replace("{NAME}", Sanitize(name)));
                AssetDatabase.ImportAsset(pathName);
                return AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(pathName);
            });
        }

        internal static void CreateAsmdef(string folder) {
            StartCreate($"{folder}/NewAssembly.asmdef", Icon("AssemblyDefinitionAsset Icon"), pathName => {
                var name = Path.GetFileNameWithoutExtension(pathName);
                File.WriteAllText(pathName, ScriptTemplates.Asmdef.Replace("{NAME}", name));
                AssetDatabase.ImportAsset(pathName);
                return AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(pathName);
            });
        }

        internal static void CreateMaterial(string folder) {
            StartCreate($"{folder}/New Material.mat", Icon("Material Icon"), pathName => {
                var material = new Material(DefaultShader());
                AssetDatabase.CreateAsset(material, pathName);
                return material;
            });
        }

        internal static void CreateMaterialFromShader(string shaderPath) {
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(shaderPath);
            if (shader == null)
                return;
            var folder = Path.GetDirectoryName(shaderPath)?.Replace('\\', '/') ?? "Assets";
            StartCreate($"{folder}/{Path.GetFileNameWithoutExtension(shaderPath)}.mat", Icon("Material Icon"), pathName => {
                var material = new Material(shader);
                AssetDatabase.CreateAsset(material, pathName);
                return material;
            });
        }

        internal static void CreateMaterialFromTexture(string texturePath) {
            var texture = AssetDatabase.LoadAssetAtPath<Texture>(texturePath);
            if (texture == null)
                return;
            var folder = Path.GetDirectoryName(texturePath)?.Replace('\\', '/') ?? "Assets";
            StartCreate($"{folder}/{Path.GetFileNameWithoutExtension(texturePath)}.mat", Icon("Material Icon"), pathName => {
                var material = new Material(DefaultShader()) { mainTexture = texture };
                AssetDatabase.CreateAsset(material, pathName);
                return material;
            });
        }

        internal static void CreateFolder(string folder) {
            StartCreate($"{folder}/New Folder", Icon("Folder Icon"), pathName => {
                var parent = Path.GetDirectoryName(pathName)?.Replace('\\', '/') ?? folder;
                var leaf = Path.GetFileName(pathName);
                var guid = AssetDatabase.CreateFolder(parent, leaf);
                var path = AssetDatabase.GUIDToAssetPath(guid);
                return string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
            });
        }

        internal static void CreateAnimationClip(string folder) {
            StartCreate($"{folder}/New Animation.anim", Icon("AnimationClip Icon"), pathName => {
                var clip = new AnimationClip();
                AssetDatabase.CreateAsset(clip, pathName);
                return clip;
            });
        }

        internal static void CreateAnimatorController(string folder) {
            StartCreate($"{folder}/New Animator Controller.controller", Icon("AnimatorController Icon"),
                pathName => AnimatorController.CreateAnimatorControllerAtPath(pathName));
        }

        /// <summary>Creates a [CustomEditor] stub next to the given MonoBehaviour script.</summary>
        internal static void CreateCustomEditor(string scriptPath) {
            var script = AssetDatabase.LoadAssetAtPath<MonoScript>(scriptPath);
            var type = script != null ? script.GetClass() : null;
            if (type == null) {
                EditorUtility.DisplayDialog("Custom Editor", "The script has no compiled class (compile errors, or filename doesn't match the class name).", "OK");
                return;
            }
            if (!typeof(UnityEngine.Object).IsAssignableFrom(type)) {
                EditorUtility.DisplayDialog("Custom Editor", $"{type.Name} is not a UnityEngine.Object — a custom editor cannot target it.", "OK");
                return;
            }

            var folder = Path.GetDirectoryName(scriptPath)?.Replace('\\', '/') ?? "Assets";
            StartCreate($"{folder}/{type.Name}Editor.cs", ScriptIcon(".cs"), pathName => {
                var content = ScriptTemplates.CustomEditor
                    .Replace("{TARGET}", type.FullName)
                    .Replace("{NAME}", Path.GetFileNameWithoutExtension(pathName));
                File.WriteAllText(pathName, content);
                AssetDatabase.ImportAsset(pathName);
                return AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(pathName);
            });
        }

        /// <summary>
        /// Enters the Project window's inline rename field with <paramref name="defaultPath"/>
        /// as the seed name; <paramref name="create"/> is invoked with the final path once the
        /// user commits (or immediately, if no Project window is open). It must write the asset
        /// and return it for selection.
        /// </summary>
        private static void StartCreate(string defaultPath, Texture2D icon, Func<string, UnityEngine.Object> create) {
            var endAction = ScriptableObject.CreateInstance<DeferredCreate>();
            endAction.create = create;
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, endAction, defaultPath, icon, null);
        }

        private static Shader DefaultShader() {
            var pipeline = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;
            if (pipeline != null && pipeline.defaultMaterial != null)
                return pipeline.defaultMaterial.shader;
            return Shader.Find("Standard");
        }

        private static Texture2D Icon(string name) {
            return EditorGUIUtility.IconContent(name)?.image as Texture2D;
        }

        private static Texture2D ScriptIcon(string extension) {
            return extension switch {
                ".cs" => Icon("cs Script Icon"),
                ".shader" => Icon("Shader Icon"),
                _ => Icon("TextAsset Icon"),
            };
        }

        private static string Sanitize(string name) {
            var chars = name.ToCharArray();
            for (var i = 0; i < chars.Length; i++)
                if (!char.IsLetterOrDigit(chars[i]) && chars[i] != '_')
                    chars[i] = '_';
            if (chars.Length > 0 && char.IsDigit(chars[0]))
                return "_" + new string(chars);
            return new string(chars);
        }

        /// <summary>
        /// One-shot end-name-edit action that runs an arbitrary create callback with the
        /// final, user-typed asset path. Backs every "+" menu create so all of them share
        /// the native inline-rename UX.
        /// </summary>
        private sealed class DeferredCreate : EndNameEditAction {
            internal Func<string, UnityEngine.Object> create;

            public override void Action(int instanceId, string pathName, string resourceFile) {
                var asset = create?.Invoke(pathName);
                if (asset != null)
                    ProjectWindowUtil.ShowCreatedAsset(asset);
            }
        }
    }
}
