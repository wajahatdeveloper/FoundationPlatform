using System.Collections.Generic;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine;

namespace AetherNexus.FoundationPlatform.EditorEnhancerX {
    /// <summary>
    /// Duplicate Array: drag the handle away from the selected object to preview a row
    /// of copies spaced by the object's bounds along the drag direction; click Apply in
    /// the floating panel to instantiate them (prefab-aware, one Undo group).
    /// </summary>
    [EditorTool("Duplicate Array (EditorEnhancerX)", typeof(Transform))]
    internal sealed class DuplicateArrayTool : EditorTool {

        private Vector3 dragTarget;
        private bool initialized;

        public override GUIContent toolbarIcon
            => EditorGUIUtility.IconContent("TreeEditor.Duplicate", "|Duplicate Array (EditorEnhancerX)");

        public override void OnActivated() {
            initialized = false;
        }

        public override void OnToolGUI(EditorWindow window) {
            if (!(window is SceneView))
                return;
            if (!EditorEnhancerXSettings.instance.duplicateToolEnabled)
                return;

            var source = Selection.activeTransform;
            if (source == null)
                return;
            if (!SelectionBoundsUtility.TryGetBounds(source.gameObject, out var bounds))
                return;

            if (!initialized) {
                initialized = true;
                dragTarget = source.position;
            }

            EditorGUI.BeginChangeCheck();
            dragTarget = Handles.PositionHandle(dragTarget, Quaternion.identity);
            EditorGUI.EndChangeCheck();

            var offset = dragTarget - source.position;
            var step = ComputeStep(offset, bounds);
            var count = step.sqrMagnitude > 0.0001f
                ? Mathf.FloorToInt(offset.magnitude / step.magnitude)
                : 0;
            count = Mathf.Clamp(count, 0, 256);

            // Preview
            Handles.color = new Color(0.3f, 0.8f, 1f, 0.7f);
            for (var i = 1; i <= count; i++)
                Handles.DrawWireCube(bounds.center + step * i, bounds.size);
            Handles.color = Color.white;

            // Floating apply panel
            Handles.BeginGUI();
            var panel = new Rect(10f, 10f, 190f, 54f);
            GUILayout.BeginArea(panel, GUIContent.none, EditorStyles.helpBox);
            GUILayout.Label($"Copies: {count}", EditorStyles.miniLabel);
            using (new EditorGUILayout.HorizontalScope()) {
                using (new EditorGUI.DisabledScope(count == 0)) {
                    if (GUILayout.Button("Apply"))
                        Apply(source, step, count);
                }
                if (GUILayout.Button("Reset"))
                    dragTarget = source.position;
            }
            GUILayout.EndArea();
            Handles.EndGUI();
        }

        // Step = the source bounds size along the dominant drag axis (falls back to the raw offset).
        private static Vector3 ComputeStep(Vector3 offset, Bounds bounds) {
            if (offset.sqrMagnitude < 0.0001f)
                return Vector3.zero;

            var direction = offset.normalized;
            var absX = Mathf.Abs(direction.x);
            var absY = Mathf.Abs(direction.y);
            var absZ = Mathf.Abs(direction.z);

            float size;
            if (absX >= absY && absX >= absZ) size = Mathf.Max(bounds.size.x, 0.01f) / absX;
            else if (absY >= absZ) size = Mathf.Max(bounds.size.y, 0.01f) / absY;
            else size = Mathf.Max(bounds.size.z, 0.01f) / absZ;

            return direction * size;
        }

        private void Apply(Transform source, Vector3 step, int count) {
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Duplicate Array");
            var group = Undo.GetCurrentGroup();

            var created = new List<GameObject>();
            var prefabRoot = PrefabUtility.IsPartOfPrefabInstance(source.gameObject)
                ? PrefabUtility.GetCorrespondingObjectFromSource(source.gameObject)
                : null;

            for (var i = 1; i <= count; i++) {
                GameObject copy;
                if (prefabRoot != null) {
                    copy = (GameObject)PrefabUtility.InstantiatePrefab(prefabRoot, source.parent);
                    copy.transform.SetPositionAndRotation(source.position + step * i, source.rotation);
                    copy.transform.localScale = source.localScale;
                } else {
                    copy = Object.Instantiate(source.gameObject, source.position + step * i, source.rotation, source.parent);
                    copy.name = source.gameObject.name;
                }
                Undo.RegisterCreatedObjectUndo(copy, "Duplicate Array");
                created.Add(copy);
            }

            Undo.CollapseUndoOperations(group);
            if (created.Count > 0)
                Selection.objects = created.ToArray();
            dragTarget = source.position;
        }
    }
}
