#if UNITY_EDITOR
using AetherNexus.FoundationPlatform.AetherInspector.Editor;
using AetherNexus.FoundationPlatform.Identity;
using UnityEditor;
using UnityEngine;

namespace AetherNexus.FoundationPlatform.Editor.Identity
{ 
	using AetherNexus.FoundationPlatform.DebugX;
	
[CustomEditor(typeof(IdentityComponent))]
public class IdentityComponentEditor : AetherInspectorEditor
{
	public override void OnInspectorGUI()
	{
		serializedObject.Update();
		var comp = (IdentityComponent)target;

		using (GuiKit.ActionRow())
		{
			EditorGUILayout.PrefixLabel("ID");
			using (new EditorGUI.DisabledScope(true))
			{
				EditorGUILayout.TextField(string.IsNullOrEmpty(comp.Identity.Value) ? "(not set)" : comp.Identity.Value);
			}
		}

		using (GuiKit.ActionRow())
		{
			if (GuiKit.ActionButton("Generate ID", 90f))
			{
				Undo.RecordObject(comp, "Generate ID");
				comp.GenerateDesignTimeId();
			}
			if (GuiKit.ActionButton("Copy", 50f))
			{
				EditorGUIUtility.systemCopyBuffer = comp.Identity.Value;
				DebugX.Debug("Copied Identity: {}",comp.Identity);
			}
			if (GuiKit.ActionButton("Clear", 50f))
			{
				Undo.RecordObject(comp, "Clear Identity");
				comp.ClearIdentity();
			}
		}

		serializedObject.ApplyModifiedProperties();
	}
}
}
#endif
