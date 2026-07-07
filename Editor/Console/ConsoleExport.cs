using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using FoundationPlatform.DebugX.ConsoleView;
using UnityEditor;
using UnityEngine;

namespace FoundationPlatform.DebugX.ConsoleView.Editor
{
    internal enum ExportFormat { Text, Csv, Ndjson }

    /// <summary>Exports the currently filtered rows to txt / CSV / NDJSON via a save dialog.</summary>
    internal static class ConsoleExport
    {
        public static void Export(List<RowRef> rows, ExportFormat format)
        {
            string ext = format == ExportFormat.Csv ? "csv" : format == ExportFormat.Ndjson ? "json" : "txt";
            string path = EditorUtility.SaveFilePanel("Export Console", "", "console-log", ext);
            if (string.IsNullOrEmpty(path)) return;

            var sb = new StringBuilder();
            switch (format)
            {
                case ExportFormat.Csv:
                    sb.AppendLine("timestamp,frame,level,source,channel,count,message,properties");
                    foreach (var r in rows) AppendCsv(sb, r);
                    break;
                case ExportFormat.Ndjson:
                    foreach (var r in rows) AppendNdjson(sb, r);
                    break;
                default:
                    foreach (var r in rows) AppendText(sb, r);
                    break;
            }

            File.WriteAllText(path, sb.ToString());
        }

        private static void AppendText(StringBuilder sb, RowRef r)
        {
            var e = r.Entry;
            ConsoleFormat.EnsureDerived(e);
            sb.Append('[').Append(e.Timestamp.ToString("HH:mm:ss.fff")).Append("] ");
            sb.Append(e.Level).Append(' ');
            if (!string.IsNullOrEmpty(e.Channel)) sb.Append('[').Append(e.Channel).Append("] ");
            if (r.Count > 1) sb.Append('(').Append(r.Count).Append("x) ");
            sb.Append(e.Message);
            if (!string.IsNullOrEmpty(e.PropertiesText)) sb.Append(" | ").Append(e.PropertiesText);
            sb.AppendLine();
            if (!string.IsNullOrEmpty(e.DisplayStack))
                sb.AppendLine(e.DisplayStack);
        }

        private static void AppendCsv(StringBuilder sb, RowRef r)
        {
            var e = r.Entry;
            ConsoleFormat.EnsureDerived(e);
            sb.Append(Csv(e.Timestamp.ToString("o", CultureInfo.InvariantCulture))).Append(',');
            if (e.FrameCount >= 0) sb.Append(e.FrameCount);
            sb.Append(',');
            sb.Append(Csv(e.Level.ToString())).Append(',');
            sb.Append(Csv(e.Source.ToString())).Append(',');
            sb.Append(Csv(e.Channel ?? "")).Append(',');
            sb.Append(r.Count).Append(',');
            sb.Append(Csv(e.Message ?? "")).Append(',');
            sb.Append(Csv(e.PropertiesText ?? "")).Append('\n');
        }

        private static void AppendNdjson(StringBuilder sb, RowRef r)
        {
            var e = r.Entry;
            ConsoleFormat.EnsureDerived(e);
            sb.Append('{')
              .Append("\"ts\":").Append(Json(e.Timestamp.ToString("o", CultureInfo.InvariantCulture))).Append(',')
              .Append("\"frame\":").Append(e.FrameCount).Append(',')
              .Append("\"level\":").Append(Json(e.Level.ToString())).Append(',')
              .Append("\"source\":").Append(Json(e.Source.ToString())).Append(',')
              .Append("\"channel\":").Append(Json(e.Channel ?? "")).Append(',')
              .Append("\"count\":").Append(r.Count).Append(',')
              .Append("\"message\":").Append(Json(e.Message ?? "")).Append(',')
              .Append("\"properties\":").Append(Json(e.PropertiesText ?? ""))
              .Append("}\n");
        }

        private static string Csv(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            bool quote = s.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0;
            s = s.Replace("\"", "\"\"");
            return quote ? "\"" + s + "\"" : s;
        }

        private static string Json(string s)
        {
            if (s == null) return "\"\"";
            var sb = new StringBuilder(s.Length + 2);
            sb.Append('"');
            foreach (char c in s)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default: sb.Append(c); break;
                }
            }
            sb.Append('"');
            return sb.ToString();
        }
    }
}
