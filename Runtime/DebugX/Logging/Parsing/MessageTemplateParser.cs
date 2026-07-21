using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace AetherNexus.FoundationPlatform.DebugX
{
    /// <summary>
    /// Parses message templates like "Player {Name} has {Health} health"
    /// AOT-safe, no regex or complex parsing
    /// Handles escaped braces: {{ becomes {, }} becomes }
    /// </summary>
    public static class MessageTemplateParser
    {
        public static (string renderedMessage, LogProperty[] properties) Parse(
            string template, object[] values)
        {
            if (values == null || values.Length == 0)
                return (template, null);

            var properties = new List<LogProperty>();
            var renderedMessage = new StringBuilder(template.Length * 2);
            var valueIndex = 0;

            // Single-pass character-by-character parsing
            for (int i = 0; i < template.Length; i++)
            {
                char current = template[i];

                // Check for escaped braces
                if (current == '{' && i + 1 < template.Length && template[i + 1] == '{')
                {
                    // Escaped opening brace: {{
                    renderedMessage.Append('{');
                    i++; // Skip the second {
                    continue;
                }

                if (current == '}' && i + 1 < template.Length && template[i + 1] == '}')
                {
                    // Escaped closing brace: }}
                    renderedMessage.Append('}');
                    i++; // Skip the second }
                    continue;
                }

                // Check for placeholder start
                if (current == '{')
                {
                    // Find the closing brace
                    int closeBrace = template.IndexOf('}', i + 1);
                    if (closeBrace == -1)
                    {
                        // No closing brace, treat as literal
                        renderedMessage.Append(current);
                        continue;
                    }

                    // Extract property name
                    string propertyName = template.Substring(i + 1, closeBrace - i - 1);

                    if (valueIndex < values.Length)
                    {
                        var value = values[valueIndex];
                        properties.Add(new LogProperty(propertyName, value));

                        // Format the value
                        string formattedValue = FormatValue(value);
                        renderedMessage.Append(formattedValue);

                        valueIndex++;
                    }
                    else
                    {
                        // No value available, keep placeholder
                        renderedMessage.Append('{').Append(propertyName).Append('}');
                    }

                    i = closeBrace; // Move past the closing brace
                    continue;
                }

                // Regular character
                renderedMessage.Append(current);
            }

            return (renderedMessage.ToString(), properties.ToArray());
        }

        private static string FormatValue(object value)
        {
            if (value == null)
                return "null";

            // Primitives: use ToString()
            if (value is string || value is int || value is long || value is float || 
                value is double || value is bool || value is byte || value is short ||
                value is uint || value is ulong || value is ushort || value is sbyte ||
                value is char || value is decimal)
            {
                return value.ToString();
            }

            // Unity types that have good ToString()
            if (value is Vector2 || value is Vector3 || value is Vector4 ||
                value is Quaternion || value is Color || value is Rect)
            {
                return value.ToString();
            }

            var type = value.GetType();

            // Respect custom ToString() overrides (e.g. GameplayTag) before falling to JsonUtility
            var toStringMethod = type.GetMethod("ToString", System.Type.EmptyTypes);
            if (toStringMethod != null && toStringMethod.DeclaringType != typeof(object))
            {
                return value.ToString();
            }

            // Complex objects: try JsonUtility if it's a Unity-serializable type
            try
            {
                // Check if the type is marked as Serializable
                if (type.IsDefined(typeof(System.SerializableAttribute), false))
                {
                    string json = JsonUtility.ToJson(value);
                    // JsonUtility returns {} for non-serializable fields, so check if it's meaningful
                    if (!string.IsNullOrEmpty(json) && json != "{}")
                    {
                        return json;
                    }
                }
            }
            catch
            {
                // JsonUtility failed, fall through to ToString
            }

            // Fallback: use ToString for all other types
            return value.ToString();
        }
    }
}

