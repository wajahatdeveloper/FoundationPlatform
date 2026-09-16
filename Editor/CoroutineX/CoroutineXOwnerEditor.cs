#if UNITY_EDITOR
using AetherNexus.FoundationPlatform.AetherInspector.Editor;
using AetherNexus.FoundationPlatform.CoroutineX;
using UnityEditor;
using UnityEngine;

namespace AetherNexus.FoundationPlatform.Editor.CoroutineX
{
[CustomEditor(typeof(CoroutineXOwner))]
public class CoroutineXOwnerEditor : AetherInspectorEditor
{
    private SerializedProperty _coroutinesProperty;

    private bool showDetails;

    protected override void OnEnable()
    {
        base.OnEnable();
        _coroutinesProperty = serializedObject.FindProperty("_Coroutines");
        SyncUpdateSubscription();
    }

    protected override void OnDisable()
    {
        EditorApplication.update -= Update;
        base.OnDisable();
    }

    private void Update() => EditorUtility.SetDirty(serializedObject.targetObject);

    private void SyncUpdateSubscription()
    {
        EditorApplication.update -= Update;
        if (showDetails)
            EditorApplication.update += Update;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.ObjectField("Script:", MonoScript.FromMonoBehaviour((CoroutineXOwner)target), typeof(CoroutineXOwner), false);
        }

        using (GuiKit.ActionRow())
        {
            if (GuiKit.ActionButton($"{(showDetails ? "Hide" : "Show")} CoroutinesX"))
            {
                showDetails = !showDetails;
                SyncUpdateSubscription();
            }
            GUILayout.Label($":  {_coroutinesProperty.arraySize}");
        }

        if (!showDetails)
            return;

        var columnWidth = (Screen.width - 22f) / 4f;
        var columnOptions = GUILayout.Width(columnWidth);

        GuiKit.BeginBox();
        using (GuiKit.ActionRow())
        {
            GUILayout.Label("Index", AetherInspectorTheme.FlatHeaderLabel, columnOptions);
            GUILayout.Label("Name", AetherInspectorTheme.FlatHeaderLabel, columnOptions);
            GUILayout.Label("State", AetherInspectorTheme.FlatHeaderLabel, columnOptions);
            GUILayout.Label("Last Result", AetherInspectorTheme.FlatHeaderLabel, columnOptions);
        }

        for (int i = 0; i < _coroutinesProperty.arraySize; i++)
        {
            var coroutine = (FoundationPlatform.CoroutineX.CoroutineX)_coroutinesProperty.GetArrayElementAtIndex(i).managedReferenceValue;

            using (GuiKit.ActionRow())
            {
                GUILayout.Label(i.ToString(), EditorStyles.label, columnOptions);

                var name = string.IsNullOrEmpty(coroutine.Name) ? "[noname]" : coroutine.Name;
                GUILayout.Label(new GUIContent(name, name), EditorStyles.label, columnOptions);

                var state = coroutine.CurrentState.ToString();
                GUILayout.Label(new GUIContent(state, state), EditorStyles.label, columnOptions);

                var lastResult = coroutine.LastResult?.ToString() ?? "null";
                GUILayout.Label(new GUIContent(lastResult, lastResult), EditorStyles.label, columnOptions);
            }
        }

        GuiKit.EndBox();
    }


}
}
#endif