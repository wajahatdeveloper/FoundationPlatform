using System.Collections.Generic;
using AetherNexus.FoundationPlatform.Logging;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AetherNexus.FoundationPlatform.Editor.Utilities
{
    /// <summary>
    /// Removes <see cref="IEditorOnlyComponent"/> instances from scenes going into a player build.
    /// Entering Play mode also processes scenes; those are left untouched.
    /// </summary>
    internal sealed class EditorOnlyComponentStripper : IProcessSceneWithReport
    {
        public int callbackOrder => 0;

        private static readonly List<MonoBehaviour> Scratch = new();

        public void OnProcessScene(Scene scene, BuildReport report)
        {
            if (!BuildPipeline.isBuildingPlayer)
            {
                return;
            }

            int stripped = Strip(scene);
            if (stripped > 0)
            {
                DebugX.Logger(LogChannels.Editor).Info(
                    "[Build] Stripped {Count} editor-only component(s) from scene '{Scene}'.", stripped, scene.name);
            }
        }

        /// <summary>Destroys every <see cref="IEditorOnlyComponent"/> in <paramref name="scene"/>; returns the count.</summary>
        internal static int Strip(Scene scene)
        {
            int stripped = 0;
            foreach (var root in scene.GetRootGameObjects())
            {
                root.GetComponentsInChildren(true, Scratch);
                for (int i = 0; i < Scratch.Count; i++)
                {
                    if (Scratch[i] is IEditorOnlyComponent)
                    {
                        UnityEngine.Object.DestroyImmediate(Scratch[i], true);
                        stripped++;
                    }
                }
            }

            Scratch.Clear();
            return stripped;
        }
    }
}
