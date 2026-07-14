#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
 
namespace AetherNexus.FoundationPlatform.Editor.Dialogs
{
public class EditorYesNoDialog : EditorWindow
{
    private string  description;
    private string  yesButton, noButton;
    private bool    initializedPosition = false;
    private bool    hasFocused = false;
    private Action  onYesButton, onNoButton;
    private bool    shouldClose = false;
 
    #region OnGUI()
    void OnGUI()
    {
        // Check if Esc/Return have been pressed
        var e = Event.current;
        if( e.type == EventType.KeyDown )
        {
            switch( e.keyCode )
            {
                // Escape pressed
                case KeyCode.Escape:
                    shouldClose = true;
                    break;
 
                // Enter pressed
                case KeyCode.Return:
                case KeyCode.KeypadEnter:
                    onYesButton?.Invoke();
                    shouldClose = true;
                    break;
            }
        }
 
        if( shouldClose ) {  // Close this dialog
            Close();
            //return;
        }

        if (!hasFocused) { Focus(); hasFocused = true; }
        
        // Draw our control
        var rect = EditorGUILayout.BeginVertical();
 
        EditorGUILayout.Space( 12 );
        EditorGUILayout.LabelField( description , GUILayout.MinHeight(60)  );
        EditorGUILayout.Space( 12 );
 
        // Draw Yes / No buttons
        var r = EditorGUILayout.GetControlRect();
        r.width /= 2;
        if( GUI.Button( r, yesButton ) ) {
            onYesButton?.Invoke();
            shouldClose = true;
        }
 
        r.x += r.width;
        if( GUI.Button( r, noButton ) ) {
            onNoButton?.Invoke();
            shouldClose = true;
        }
 
        EditorGUILayout.Space( 8 );
        EditorGUILayout.EndVertical();
 
        // Force change size of the window
        if( rect.width != 0 && minSize != rect.size ) {
            minSize = maxSize = rect.size;
        }
 
        // Set dialog position next to mouse position
        if( !initializedPosition ) {
            var mousePos = GUIUtility.GUIToScreenPoint( Event.current.mousePosition );
            position = new Rect( mousePos.x + 32, mousePos.y, position.width, position.height );
            initializedPosition = true;
        }
    }
    #endregion OnGUI()
 
    #region Show()
    /// <summary>
    /// Returns text player entered, or null if player cancelled the dialog.
    /// </summary>
    /// <param name="title"></param>
    /// <param name="description"></param>
    /// <param name="inputText"></param>
    /// <param name="yesButton"></param>
    /// <param name="noButton"></param>
    /// <returns></returns>
    public static void Show( string title, string description, Action onYes = null, Action onNo = null )
    {
        var window = CreateInstance<EditorYesNoDialog>();
        window.titleContent = new GUIContent( title );
        window.description = description;
        window.yesButton = "Yes";
        window.noButton = "No";
        window.onYesButton = onYes;
        window.onNoButton = onNo;
        window.ShowPopup();
    }
    #endregion Show()
}
}
#endif