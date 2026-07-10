using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace ProjectWindowX {
    /// <summary>
    /// Create-asset implementations for the hover "+" menu. Assets are created
    /// directly at a unique path in the target folder, then pinged.
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
            var path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{defaultName}{extension}");
            var name = Path.GetFileNameWithoutExtension(path);
            File.WriteAllText(path, template.Replace("{NAME}", Sanitize(name)));
            AssetDatabase.ImportAsset(path);
            Ping(path);
        }

        internal static void CreateAsmdef(string folder) {
            var path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/NewAssembly.asmdef");
            var name = Path.GetFileNameWithoutExtension(path);
            File.WriteAllText(path, ScriptTemplates.Asmdef.Replace("{NAME}", name));
            AssetDatabase.ImportAsset(path);
            Ping(path);
        }

        internal static void CreateMaterial(string folder) {
            var material = new Material(DefaultShader());
            CreateAsset(material, $"{folder}/New Material.mat");
        }

        internal static void CreateMaterialFromShader(string shaderPath) {
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(shaderPath);
            if (shader == null)
                return;
            var folder = Path.GetDirectoryName(shaderPath)?.Replace('\\', '/') ?? "Assets";
            var material = new Material(shader);
            CreateAsset(material, $"{folder}/{Path.GetFileNameWithoutExtension(shaderPath)}.mat");
        }

        internal static void CreateMaterialFromTexture(string texturePath) {
            var texture = AssetDatabase.LoadAssetAtPath<Texture>(texturePath);
            if (texture == null)
                return;
            var folder = Path.GetDirectoryName(texturePath)?.Replace('\\', '/') ?? "Assets";
            var material = new Material(DefaultShader()) { mainTexture = texture };
            CreateAsset(material, $"{folder}/{Path.GetFileNameWithoutExtension(texturePath)}.mat");
        }

        internal static void CreateFolder(string folder) {
            var guid = AssetDatabase.CreateFolder(folder, "New Folder");
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (!string.IsNullOrEmpty(path))
                Ping(path);
        }

        internal static void CreateAnimationClip(string folder) {
            CreateAsset(new AnimationClip(), $"{folder}/New Animation.anim");
        }

        internal static void CreateAnimatorController(string folder) {
            var path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/New Animator Controller.controller");
            AnimatorController.CreateAnimatorControllerAtPath(path);
            Ping(path);
        }

        /// <summary>Creates a [CustomEditor] stub next to the given MonoBehaviour script.</summary>
        internal static void CreateCustomEditor(string scriptPath) {
            var script = AssetDatabase.LoadAssetAtPath<MonoScript>(scriptPath);
            var type = script != null ? script.GetClass() : null;
            if (type == null) {
                EditorUtility.DisplayDialog("Custom Editor", "The script has no compiled class (compile errors, or filename doesn't match the class name).", "OK");
                return;
            }
            if (!typeof(Object).IsAssignableFrom(type)) {
                EditorUtility.DisplayDialog("Custom Editor", $"{type.Name} is not a UnityEngine.Object — a custom editor cannot target it.", "OK");
                return;
            }

            var folder = Path.GetDirectoryName(scriptPath)?.Replace('\\', '/') ?? "Assets";
            var path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{type.Name}Editor.cs");
            var content = ScriptTemplates.CustomEditor
                .Replace("{TARGET}", type.FullName)
                .Replace("{NAME}", Path.GetFileNameWithoutExtension(path));
            File.WriteAllText(path, content);
            AssetDatabase.ImportAsset(path);
            Ping(path);
        }

        private static Shader DefaultShader() {
            var pipeline = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;
            if (pipeline != null && pipeline.defaultMaterial != null)
                return pipeline.defaultMaterial.shader;
            return Shader.Find("Standard");
        }

        private static void CreateAsset(Object asset, string preferredPath) {
            var path = AssetDatabase.GenerateUniqueAssetPath(preferredPath);
            AssetDatabase.CreateAsset(asset, path);
            Ping(path);
        }

        private static void Ping(string path) {
            var asset = AssetDatabase.LoadAssetAtPath<Object>(path);
            if (asset != null) {
                Selection.activeObject = asset;
                EditorGUIUtility.PingObject(asset);
            }
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
    }
}
