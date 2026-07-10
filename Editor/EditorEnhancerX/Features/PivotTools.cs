using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine;

namespace EditorEnhancerX {
    /// <summary>
    /// Pivot Rotate: rotates the whole selection around a freely placeable pivot point
    /// (drag the dot to relocate, use the rotation handle to rotate).
    /// </summary>
    [EditorTool("Pivot Rotate (EditorEnhancerX)")]
    internal sealed class PivotRotationTool : EditorTool {

        private Vector3 pivot;
        private Quaternion handleRotation = Quaternion.identity;
        private bool initialized;

        public override GUIContent toolbarIcon
            => EditorGUIUtility.IconContent("RotateTool", "|Pivot Rotate (EditorEnhancerX)");

        public override void OnActivated() {
            initialized = false;
        }

        public override void OnToolGUI(EditorWindow window) {
            if (!(window is SceneView))
                return;
            if (!EditorEnhancerXSettings.instance.pivotToolsEnabled)
                return;

            var transforms = Selection.transforms;
            if (transforms.Length == 0)
                return;

            if (!initialized) {
                initialized = true;
                pivot = UnityEditor.Tools.handlePosition;
                handleRotation = Quaternion.identity;
            }

            // Relocatable pivot dot.
            EditorGUI.BeginChangeCheck();
            var size = HandleUtility.GetHandleSize(pivot) * 0.1f;
            var newPivot = Handles.FreeMoveHandle(pivot, size, Vector3.zero, Handles.SphereHandleCap);
            if (EditorGUI.EndChangeCheck()) {
                pivot = newPivot;
                handleRotation = Quaternion.identity;
            }

            // Rotation handle applies the delta to every selected transform around the pivot.
            EditorGUI.BeginChangeCheck();
            var newRotation = Handles.RotationHandle(handleRotation, pivot);
            if (EditorGUI.EndChangeCheck()) {
                var delta = newRotation * Quaternion.Inverse(handleRotation);
                handleRotation = newRotation;

                Undo.RecordObjects(transforms, "Pivot Rotate");
                foreach (var t in transforms) {
                    var offset = t.position - pivot;
                    t.position = pivot + delta * offset;
                    t.rotation = delta * t.rotation;
                }
            }
        }
    }

    /// <summary>
    /// Move Pivot: repositions a GameObject's pivot without moving its children —
    /// the parent translates and every direct child is counter-translated.
    /// </summary>
    [EditorTool("Move Pivot (EditorEnhancerX)", typeof(Transform))]
    internal sealed class PivotMoveTool : EditorTool {

        public override GUIContent toolbarIcon
            => EditorGUIUtility.IconContent("MoveTool", "|Move Pivot (EditorEnhancerX)");

        public override void OnToolGUI(EditorWindow window) {
            if (!(window is SceneView))
                return;
            if (!EditorEnhancerXSettings.instance.pivotToolsEnabled)
                return;

            var t = Selection.activeTransform;
            if (t == null)
                return;

            EditorGUI.BeginChangeCheck();
            var newPosition = Handles.PositionHandle(t.position, t.rotation);
            if (EditorGUI.EndChangeCheck()) {
                var delta = newPosition - t.position;

                var toRecord = new Object[t.childCount + 1];
                toRecord[0] = t;
                for (var i = 0; i < t.childCount; i++)
                    toRecord[i + 1] = t.GetChild(i);
                Undo.RecordObjects(toRecord, "Move Pivot");

                t.position = newPosition;
                for (var i = 0; i < t.childCount; i++)
                    t.GetChild(i).position -= delta;
            }
        }
    }
}
