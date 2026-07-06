using FoundationPlatform.Attributes;
using UnityEditor;
using UnityEngine;

namespace FoundationPlatform.Editor.Utilities
{
    [CustomPropertyDrawer(typeof(TooltipIconAttribute))]
    public sealed class TooltipIconDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var attr = (TooltipIconAttribute)attribute;
            if (attr == null || string.IsNullOrWhiteSpace(attr.Tooltip))
            {
                EditorGUI.PropertyField(position, property, label, true);
                return;
            }

            const float iconWidth = 20f;
            const float iconSize = 16f;

            Rect fieldRect = new Rect(position.x, position.y, position.width - iconWidth, position.height);
            Rect iconRect = new Rect(position.xMax - iconWidth + (iconWidth - iconSize) * 0.5f, position.y + (position.height - iconSize) * 0.5f, iconSize, iconSize);

            EditorGUI.PropertyField(fieldRect, property, label, true);
            AuthoringUxShared.DrawTooltipIcon(iconRect, attr.Tooltip);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }
    }
}
