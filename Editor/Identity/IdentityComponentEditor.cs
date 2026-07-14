#if UNITY_EDITOR
using AetherNexus.FoundationPlatform.DebugX;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(IdentityComponent))]
public class IdentityComponentEditor : Editor
{
	private void OnEnable() { }

	public override void OnInspectorGUI()
	{
		serializedObject.Update();
		var comp = (IdentityComponent)target;

		EditorGUILayout.BeginHorizontal();
		EditorGUILayout.PrefixLabel("ID");
		EditorGUI.BeginDisabledGroup(true);
		EditorGUILayout.TextField(string.IsNullOrEmpty(comp.Identity.Value) ? "(not set)" : comp.Identity.Value);
		EditorGUI.EndDisabledGroup();
		EditorGUILayout.EndHorizontal();

		EditorGUILayout.BeginHorizontal();
		if (GUILayout.Button("Generate ID", GUILayout.Width(90)))
		{
			Undo.RecordObject(comp, "Generate ID");
			comp.GenerateDesignTimeId();
		}
		if (GUILayout.Button("Copy", GUILayout.Width(50)))
		{
			EditorGUIUtility.systemCopyBuffer = comp.Identity.Value;
			DebugX.Debug($"Copied Identity: {comp.Identity}");
		}
		if (GUILayout.Button("Clear", GUILayout.Width(50)))
		{
			Undo.RecordObject(comp, "Clear Identity");
			comp.ClearIdentity();
		}
		EditorGUILayout.EndHorizontal();

		serializedObject.ApplyModifiedProperties();
	}
}
#endif
