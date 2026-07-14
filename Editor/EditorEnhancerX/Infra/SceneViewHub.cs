using System;
using System.Collections.Generic;
using UnityEditor;

namespace AetherNexus.FoundationPlatform.EditorEnhancerX {
    /// <summary>
    /// Single SceneView.duringSceneGui subscription fanning out to ordered passes.
    /// Each pass is exception-isolated: a throwing pass logs once and keeps siblings alive.
    /// </summary>
    [InitializeOnLoad]
    public static class SceneViewHub {

        private sealed class Pass {
            public string id;
            public int order;
            public Action<SceneView> draw;
            public bool faulted;
        }

        private static readonly List<Pass> passes = new List<Pass>();

        static SceneViewHub() {
            SceneView.duringSceneGui += Fan;
        }

        public static void Register(string id, int order, Action<SceneView> draw) {
            if (string.IsNullOrEmpty(id) || draw == null) return;
            passes.RemoveAll(p => p.id == id);
            passes.Add(new Pass { id = id, order = order, draw = draw });
            passes.Sort((a, b) => a.order.CompareTo(b.order));
        }

        public static void Unregister(string id) {
            passes.RemoveAll(p => p.id == id);
        }

        private static void Fan(SceneView view) {
            if (!EditorEnhancerXSettings.instance.masterEnabled) return;
            for (int i = 0; i < passes.Count; i++) {
                var pass = passes[i];
                try {
                    pass.draw(view);
                }
                catch (Exception ex) {
                    if (!pass.faulted) {
                        pass.faulted = true;
                        UnityEngine.Debug.LogError($"[EditorEnhancerX] SceneView pass '{pass.id}' threw and will keep running (logged once): {ex}");
                    }
                }
            }
        }
    }
}
