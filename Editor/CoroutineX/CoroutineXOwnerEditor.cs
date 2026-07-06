#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CoroutineXOwner))]
public class CoroutineXOwnerEditor : UnityEditor.Editor
{
    private SerializedProperty _coroutinesProperty;

    private bool showDetails;

    private void OnEnable()
    {
        _coroutinesProperty = serializedObject.FindProperty("_Coroutines");
        SyncUpdateSubscription();
    }

    private void OnDisable() => EditorApplication.update -= Update;

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

        GUI.enabled = false;
        EditorGUILayout.ObjectField("Script:", MonoScript.FromMonoBehaviour((CoroutineXOwner)target), typeof(CoroutineXOwner), false);
        GUI.enabled = true;

        GUILayout.Space(5f);

        #region Main line
        GUILayout.BeginHorizontal();

        var showButtonText = $"{(showDetails ? "Hide" : "Show")} CoroutinesX";
        if (GUILayout.Button(showButtonText, GUILayout.Width(EditorStyles.label.CalcSize(new GUIContent(showButtonText)).x + 10f)))
        {
            showDetails = !showDetails;
            SyncUpdateSubscription();
        }

        GUILayout.BeginHorizontal(EditorStyles.label);
        GUILayout.Label($":  {_coroutinesProperty.arraySize}");
        GUILayout.EndHorizontal();

        GUILayout.EndHorizontal();
        #endregion

        GUILayout.Space(2f);

        if (!showDetails)
            return;

        var columnWidth = (Screen.width - 22f) / 4f;
        var columnOptions = GUILayout.Width(columnWidth);

        #region Headers
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        GUILayout.BeginHorizontal();
        GUILayout.Label("Index", EditorStyles.boldLabel, columnOptions);
        GUILayout.Label("Name", EditorStyles.boldLabel, columnOptions);
        GUILayout.Label("State", EditorStyles.boldLabel, columnOptions);
        GUILayout.Label("Last Result", EditorStyles.boldLabel, columnOptions);
        GUILayout.EndHorizontal();
        #endregion

        for (int i = 0; i < _coroutinesProperty.arraySize; i++)
        {
            var coroutine = (CoroutineX)_coroutinesProperty.GetArrayElementAtIndex(i).managedReferenceValue;

            GUILayout.BeginHorizontal();
            #region Index
            GUILayout.Label(i.ToString(), EditorStyles.label, columnOptions);
            #endregion

            #region Name
            var oldColor = GUI.contentColor;
            GUI.contentColor = coroutine.Name == null ? Color.gray : Color.white;

            var name = string.IsNullOrEmpty(coroutine.Name) ? "[noname]" : coroutine.Name;
            GUILayout.Label(new GUIContent(name, name), EditorStyles.label, columnOptions);

            GUI.contentColor = oldColor;
            #endregion

            #region State
            oldColor = GUI.contentColor;
            GUI.contentColor = coroutine.CurrentState switch
            {
                CoroutineX.State.Reseted => Color.gray,
                CoroutineX.State.Running => Color.white,
                CoroutineX.State.Stopped => Color.yellow,
                CoroutineX.State.Completed => Color.green,
                _ => Color.white
            };

            var state = coroutine.CurrentState.ToString();
            GUILayout.Label(new GUIContent(state, state), EditorStyles.label, columnOptions);

            GUI.contentColor = oldColor;
            #endregion

            #region LastResult
            var lastResult = coroutine.LastResult?.ToString() ?? "null";
            GUILayout.Label(new GUIContent(lastResult, lastResult), EditorStyles.label, columnOptions);
            #endregion
            GUILayout.EndHorizontal();
        }

        EditorGUILayout.EndVertical();
    }


}
#endif