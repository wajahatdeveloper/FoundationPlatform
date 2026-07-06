#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Draws identity string in Scene view for selected GameObjects with IdentityComponent.
/// </summary>
[InitializeOnLoad]
public static class IdentityGizmoDrawer
{
	static IdentityGizmoDrawer()
	{
		SceneView.duringSceneGui += OnSceneGUI;
	}

	private static void OnSceneGUI(SceneView sceneView)
	{
		if (Selection.gameObjects == null || Selection.gameObjects.Length == 0) return;

		foreach (var go in Selection.gameObjects)
		{
			if (go == null) continue;
			var comp = go.GetComponent<IdentityComponent>();
			if (comp == null) continue;

			var id = comp.Identity;
			if (!id.IsValid) continue;

			var pos = go.transform.position + Vector3.up * 1.5f;
			var style = new GUIStyle(EditorStyles.label)
			{
				normal = { textColor = Color.white },
				alignment = TextAnchor.MiddleCenter,
				fontSize = 10
			};
			Handles.Label(pos, id.Value, style);
		}
	}
}
#endif
