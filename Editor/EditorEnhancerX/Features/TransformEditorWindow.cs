using System.Linq;
using UnityEditor;
using UnityEngine;

namespace EditorEnhancerX {
    /// <summary>
    /// Batch transform operations on the selection: per-axis align (min/center/max),
    /// distribute evenly, snap to grid, and zero-out local TRS. All with Undo.
    /// </summary>
    internal sealed class TransformEditorWindow : EditorWindow {

        private float gridSize = 1f;

        [MenuItem("Tools/EditorEnhancerX/Transform Editor", false, 1200)]
        private static void Open() {
            GetWindow<TransformEditorWindow>("Transform Editor").Show();
        }

        private void OnSelectionChange() => Repaint();

        private void OnGUI() {
            var transforms = Selection.transforms;
            EditorGUILayout.LabelField($"Selected: {transforms.Length}", EditorStyles.miniLabel);
            using (new EditorGUI.DisabledScope(transforms.Length == 0)) {
                EditorGUILayout.Space(4f);
                EditorGUILayout.LabelField("Align", EditorStyles.boldLabel);
                AxisRow("X", 0, transforms);
                AxisRow("Y", 1, transforms);
                AxisRow("Z", 2, transforms);

                EditorGUILayout.Space(6f);
                EditorGUILayout.LabelField("Distribute (3+ objects)", EditorStyles.boldLabel);
                using (new EditorGUI.DisabledScope(transforms.Length < 3))
                using (new EditorGUILayout.HorizontalScope()) {
                    if (GUILayout.Button("X")) Distribute(transforms, 0);
                    if (GUILayout.Button("Y")) Distribute(transforms, 1);
                    if (GUILayout.Button("Z")) Distribute(transforms, 2);
                }

                EditorGUILayout.Space(6f);
                EditorGUILayout.LabelField("Snap", EditorStyles.boldLabel);
                using (new EditorGUILayout.HorizontalScope()) {
                    gridSize = EditorGUILayout.FloatField("Grid Size", gridSize);
                    if (GUILayout.Button("Snap Position", GUILayout.Width(110f)))
                        SnapToGrid(transforms);
                }

                EditorGUILayout.Space(6f);
                EditorGUILayout.LabelField("Reset Local", EditorStyles.boldLabel);
                using (new EditorGUILayout.HorizontalScope()) {
                    if (GUILayout.Button("Position")) ResetLocal(transforms, position: true);
                    if (GUILayout.Button("Rotation")) ResetLocal(transforms, rotation: true);
                    if (GUILayout.Button("Scale")) ResetLocal(transforms, scale: true);
                    if (GUILayout.Button("All")) ResetLocal(transforms, true, true, true);
                }
            }
        }

        private void AxisRow(string label, int axis, Transform[] transforms) {
            using (new EditorGUILayout.HorizontalScope()) {
                EditorGUILayout.LabelField(label, GUILayout.Width(16f));
                if (GUILayout.Button("Min")) Align(transforms, axis, 0f);
                if (GUILayout.Button("Center")) Align(transforms, axis, 0.5f);
                if (GUILayout.Button("Max")) Align(transforms, axis, 1f);
            }
        }

        private static void Align(Transform[] transforms, int axis, float t) {
            if (transforms.Length == 0)
                return;
            var min = transforms.Min(x => x.position[axis]);
            var max = transforms.Max(x => x.position[axis]);
            var target = Mathf.Lerp(min, max, t);

            Undo.RecordObjects(transforms, "Align");
            foreach (var tr in transforms) {
                var p = tr.position;
                p[axis] = target;
                tr.position = p;
            }
        }

        private static void Distribute(Transform[] transforms, int axis) {
            var sorted = transforms.OrderBy(x => x.position[axis]).ToArray();
            var min = sorted[0].position[axis];
            var max = sorted[sorted.Length - 1].position[axis];
            var step = (max - min) / (sorted.Length - 1);

            Undo.RecordObjects(sorted, "Distribute");
            for (var i = 1; i < sorted.Length - 1; i++) {
                var p = sorted[i].position;
                p[axis] = min + step * i;
                sorted[i].position = p;
            }
        }

        private void SnapToGrid(Transform[] transforms) {
            if (gridSize <= 0f)
                return;
            Undo.RecordObjects(transforms, "Snap To Grid");
            foreach (var tr in transforms) {
                var p = tr.position;
                p.x = Mathf.Round(p.x / gridSize) * gridSize;
                p.y = Mathf.Round(p.y / gridSize) * gridSize;
                p.z = Mathf.Round(p.z / gridSize) * gridSize;
                tr.position = p;
            }
        }

        private static void ResetLocal(Transform[] transforms, bool position = false, bool rotation = false, bool scale = false) {
            Undo.RecordObjects(transforms, "Reset Local Transform");
            foreach (var tr in transforms) {
                if (position) tr.localPosition = Vector3.zero;
                if (rotation) tr.localRotation = Quaternion.identity;
                if (scale) tr.localScale = Vector3.one;
            }
        }
    }
}
