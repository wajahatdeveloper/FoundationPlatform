using System.IO;
using AetherNexus.FoundationPlatform.DebugX;
using UnityEngine;
using UnityEngine.UIElements;

namespace AetherNexus.FoundationPlatform.DebugX.ConsoleView.Editor
{
    /// <summary>
    /// Builds an inline source-code snippet (a few lines around the log's caller line, with the caller
    /// line highlighted) for the detail pane. Coloured line-highlight only; a full C# tokenizer is
    /// deliberately avoided to sidestep rich-text escaping issues.
    /// </summary>
    internal static class ConsoleSnippet
    {
        private const int Context = 4;

        public static VisualElement Build(string filePath, int line)
        {
            if (string.IsNullOrEmpty(filePath) || line <= 0)
                return null;

            string full = filePath;
            if (!File.Exists(full))
            {
                string projectPath = UnityConsoleStackFormatter.ToUnityProjectPath(filePath);
                if (projectPath != null)
                    full = Path.Combine(Directory.GetCurrentDirectory(), projectPath);
            }
            if (!File.Exists(full))
                return null;

            string[] lines;
            try { lines = File.ReadAllLines(full); }
            catch { return null; }

            int from = Mathf.Max(0, line - 1 - Context);
            int to = Mathf.Min(lines.Length - 1, line - 1 + Context);

            var box = new VisualElement();
            box.style.marginTop = 4;
            box.style.borderLeftWidth = box.style.borderRightWidth = box.style.borderTopWidth = box.style.borderBottomWidth = 1;
            var border = new Color(0f, 0f, 0f, 0.3f);
            box.style.borderLeftColor = box.style.borderRightColor = box.style.borderTopColor = box.style.borderBottomColor = border;
            box.style.backgroundColor = new Color(0f, 0f, 0f, 0.15f);
            box.style.paddingTop = box.style.paddingBottom = 2;

            for (int i = from; i <= to; i++)
            {
                bool isTarget = i == line - 1;
                var row = new Label($"{(i + 1),5}  {lines[i]}");
                row.enableRichText = false;
                row.selection.isSelectable = true;
                row.style.fontSize = ConsoleColorConfig.FontSize - 1;
                row.style.paddingLeft = 4;
                row.style.whiteSpace = WhiteSpace.NoWrap;
                if (isTarget)
                {
                    row.style.backgroundColor = new Color(1f, 0.85f, 0.3f, 0.18f);
                    row.style.color = new Color(1f, 0.95f, 0.7f);
                }
                else
                {
                    row.style.color = new Color(0.75f, 0.75f, 0.78f);
                }
                box.Add(row);
            }

            return box;
        }
    }
}
