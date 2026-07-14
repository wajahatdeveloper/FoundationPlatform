using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using UnityEditor;
using UnityEngine;
#if UNITY_EDITOR
#endif

namespace AetherNexus.FoundationPlatform.DebugX
{
    public static class DebugX
    {
        // EditorPref keys shared by DebugXInitializer (startup read), DebugXMenuItems (menu toggles),
        // and the DebugX Console settings page. Single source so all UIs stay in sync.
        public const string PrefKeyCaptureFullStackTraces = "DebugX.CaptureFullStackTraces";
        public const string PrefKeySyncConsole = "DebugX.SyncConsoleForStackTraces";
        public const string PrefKeyEditorMinLevel = "DebugX.EditorMinLevel";

        #region Structured Logging - Simple Overloads

        /// <summary>
        /// Log with structured properties using message template
        /// </summary>
        [UnityEngine.HideInCallstack]
        public static void Log(string messageTemplate, object[] propertyValues, string filterName, GameObject context)
        {
            if (!LogPipeline.ShouldEmit(LogLevel.Information, filterName))
                return;

            var (renderedMessage, properties) = MessageTemplateParser.Parse(messageTemplate, propertyValues);
            var callerInfo = CallerInfoHelper.GetCallerInfo();
            var logEvent = new LogEvent(
                LogLevel.Information,
                messageTemplate,
                renderedMessage,
                properties,
                filterName,
                null,
                callerInfo,
                null,
                context
            );
            LogPipeline.Emit(logEvent);
        }

        /// <summary>
        /// Log at Debug level
        /// </summary>
        [UnityEngine.HideInCallstack]
        public static void Debug(string messageTemplate, params object[] propertyValues)
        {
            WriteStructured(LogLevel.Debug, messageTemplate, propertyValues);
        }

        /// <summary>
        /// Log at Information level
        /// </summary>
        [UnityEngine.HideInCallstack]
        public static void Info(string messageTemplate, params object[] propertyValues)
        {
            WriteStructured(LogLevel.Information, messageTemplate, propertyValues);
        }

        /// <summary>
        /// Log at Warning level
        /// </summary>
        [UnityEngine.HideInCallstack]
        public static void Warning(string messageTemplate, params object[] propertyValues)
        {
            WriteStructured(LogLevel.Warning, messageTemplate, propertyValues);
        }

        /// <summary>
        /// Log at Error level
        /// </summary>
        [UnityEngine.HideInCallstack]
        public static void Error(string messageTemplate, params object[] propertyValues)
        {
            WriteStructured(LogLevel.Error, messageTemplate, propertyValues);
        }

        /// <summary>
        /// Log at Error level with exception
        /// </summary>
        [UnityEngine.HideInCallstack]
        public static void Error(System.Exception exception, string messageTemplate, params object[] propertyValues)
        {
            WriteStructured(LogLevel.Error, messageTemplate, propertyValues, exception);
        }

        [UnityEngine.HideInCallstack]
        private static void WriteStructured(LogLevel level, string messageTemplate, object[] propertyValues,
                                            System.Exception exception = null)
        {
            if (!LogPipeline.ShouldEmit(level, null))
                return;

            if (level == LogLevel.Error && exception != null && ExplicitErrorDedupe.ShouldSkipErrorLog(exception))
                return;

            var (renderedMessage, properties) = MessageTemplateParser.Parse(messageTemplate, propertyValues);

            // Capture stack trace if error/fatal or explicitly enabled
            string stackTrace = null;
            if (level == LogLevel.Error || level == LogLevel.Fatal || DebugXBuilder.EnableFullStackTraces)
            {
                // Unity's extractor resolves user-assembly frames to clickable (at Assets/..:N) form,
                // unlike System.Diagnostics which renders many frames as <GUID>:0 in this project.
                stackTrace = UnityEngine.StackTraceUtility.ExtractStackTrace();
            }

            var callerInfo = CallerInfoHelper.GetCallerInfo();
            var logEvent = new LogEvent(
                level,
                messageTemplate,
                renderedMessage,
                properties,
                null,
                null,
                callerInfo,
                exception,
                null,
                stackTrace
            );
            LogPipeline.Emit(logEvent);

            if (level == LogLevel.Error && exception == null)
                ExplicitErrorDedupe.RegisterExplicitFailure(properties);
        }

        #endregion

        #region Watch

        /// <summary>
        /// Tracks a variable in the DebugX Console's Watch panel. Produces exactly one live row per name
        /// that updates in place — safe to call every frame from Update() without spamming the log stream.
        /// Editor-only (no-op in builds). Call on the main thread.
        /// </summary>
        public static void Watch(string name, object value)
        {
            #if UNITY_EDITOR
            ConsoleLogStore.SetWatch(name, value != null ? value.ToString() : "null");
            #endif
        }

        #endregion

        #if UNITY_EDITOR
        /// <summary>
        /// When true, stack traces are captured for all log levels (not just Warning+).
        /// Toggle via Tools/FoundationPlatform/DebugX/Capture Full Stack Traces. Persisted in EditorPrefs.
        /// </summary>
        public static bool CaptureFullStackTraces
        {
            get => DebugXBuilder.EnableFullStackTraces;
            set => DebugXBuilder.EnableFullStackTraces = value;
        }

        /// <summary>
        /// When true, console sinks run synchronously on main thread for correct stack traces.
        /// Toggle via Tools/FoundationPlatform/DebugX/Sync Console (Correct Stack Traces). Persisted in EditorPrefs.
        /// </summary>
        public static bool SyncConsoleForStackTraces
        {
            get => DebugXBuilder.UseSyncConsole;
            set => DebugXBuilder.UseSyncConsole = value;
        }
        #endif

        #region Builder API

        /// <summary>
        /// Zero-alloc logger for the given channel. Use for Info/Warning/Error etc.
        /// </summary>
        public static DebugXLogger Logger(LogChannel channel)
        {
            return new DebugXLogger(channel);
        }

        /// <summary>
        /// Builder with WithContext/WithProperty support. Allocates; use Logger() when not needed.
        /// </summary>
        public static IDebugXBuilder Builder(LogChannel channel)
        {
            return new DebugXBuilder().WithChannel(channel);
        }

        #endregion

        #region Log Array

        [System.ThreadStatic] private static StringBuilder _stringBuilder;

        public static void LogArray<T>(T[] toLog)
        {
            if (!LogPipeline.ShouldEmit(LogLevel.Information, null))
                return;

            var sb = _stringBuilder ?? (_stringBuilder = new StringBuilder());
            sb.Length = 0;

            sb.Append("Log Array: ").Append(typeof(T).Name).Append(" (").Append(toLog.Length).Append(")\n");
            for (var i = 0; i < toLog.Length; i++)
            {
                sb.Append("\n\t").Append(Colored(i.ToString(), "brown"))
                  .Append(": ").Append(toLog[i]);
            }

            UnityEngine.Debug.Log(sb.ToString());
        }

        public static void LogArray<T>(IList<T> toLog)
        {
            if (!LogPipeline.ShouldEmit(LogLevel.Information, null))
                return;

            var sb = _stringBuilder ?? (_stringBuilder = new StringBuilder());
            sb.Length = 0;

            var count = toLog.Count;
            sb.Append("Log Array: ").Append(typeof(T).Name).Append(" (").Append(count).Append(")\n");

            for (var i = 0; i < count; i++)
            {
                sb.Append("\n\t" + Colored(i.ToString(), "brown") + ": " +
                          toLog[i]);
            }

            UnityEngine.Debug.Log(sb.ToString());
        }

        #endregion

        #region Debug Bounds

        /// <summary>
        /// Draw bounds of Mesh
        /// </summary>
        public static void DrawDebugBounds(MeshFilter mesh, Color color)
        {
            #if UNITY_EDITOR
            if (mesh == null) return;
            var renderer = mesh.GetComponent<MeshRenderer>();
            DrawDebugBounds(renderer, color);
            #endif
        }

        /// <summary>
        /// Draw bounds of MeshRenderer
        /// </summary>
        public static void DrawDebugBounds(MeshRenderer renderer, Color color)
        {
            #if UNITY_EDITOR
            var bounds = renderer.bounds;
            DrawDebugBounds(bounds, color);
            #endif
        }

        /// <summary>
        /// Draw bounds of Bounds
        /// </summary>
        public static void DrawDebugBounds(Bounds bounds, Color color)
        {
            #if UNITY_EDITOR
            Vector3 v3Center = bounds.center;
            Vector3 v3Extents = bounds.extents;

            var v3FrontTopLeft =
                new Vector3(v3Center.x - v3Extents.x, v3Center.y + v3Extents.y,
                            v3Center.z - v3Extents.z); // Front top left corner
            var v3FrontTopRight =
                new Vector3(v3Center.x + v3Extents.x, v3Center.y + v3Extents.y,
                            v3Center.z - v3Extents.z); // Front top right corner
            var v3FrontBottomLeft =
                new Vector3(v3Center.x - v3Extents.x, v3Center.y - v3Extents.y,
                            v3Center.z - v3Extents.z); // Front bottom left corner
            var v3FrontBottomRight =
                new Vector3(v3Center.x + v3Extents.x, v3Center.y - v3Extents.y,
                            v3Center.z - v3Extents.z); // Front bottom right corner
            var v3BackTopLeft =
                new Vector3(v3Center.x - v3Extents.x, v3Center.y + v3Extents.y,
                            v3Center.z + v3Extents.z); // Back top left corner
            var v3BackTopRight =
                new Vector3(v3Center.x + v3Extents.x, v3Center.y + v3Extents.y,
                            v3Center.z + v3Extents.z); // Back top right corner
            var v3BackBottomLeft =
                new Vector3(v3Center.x - v3Extents.x, v3Center.y - v3Extents.y,
                            v3Center.z + v3Extents.z); // Back bottom left corner
            var v3BackBottomRight =
                new Vector3(v3Center.x + v3Extents.x, v3Center.y - v3Extents.y,
                            v3Center.z + v3Extents.z); // Back bottom right corner

            UnityEngine.Debug.DrawLine(v3FrontTopLeft, v3FrontTopRight, color);
            UnityEngine.Debug.DrawLine(v3FrontTopRight, v3FrontBottomRight, color);
            UnityEngine.Debug.DrawLine(v3FrontBottomRight, v3FrontBottomLeft, color);
            UnityEngine.Debug.DrawLine(v3FrontBottomLeft, v3FrontTopLeft, color);

            UnityEngine.Debug.DrawLine(v3BackTopLeft, v3BackTopRight, color);
            UnityEngine.Debug.DrawLine(v3BackTopRight, v3BackBottomRight, color);
            UnityEngine.Debug.DrawLine(v3BackBottomRight, v3BackBottomLeft, color);
            UnityEngine.Debug.DrawLine(v3BackBottomLeft, v3BackTopLeft, color);

            UnityEngine.Debug.DrawLine(v3FrontTopLeft, v3BackTopLeft, color);
            UnityEngine.Debug.DrawLine(v3FrontTopRight, v3BackTopRight, color);
            UnityEngine.Debug.DrawLine(v3FrontBottomRight, v3BackBottomRight, color);
            UnityEngine.Debug.DrawLine(v3FrontBottomLeft, v3BackBottomLeft, color);
            #endif
        }

        #endregion

        #region Debug Draw

        public static void DrawString(string text, Vector3 worldPos, Color? colour = null)
        {
            #if UNITY_EDITOR
            var defaultColor = GUI.color;

            Handles.BeginGUI();
            if (colour.HasValue) GUI.color = colour.Value;
            var view = SceneView.currentDrawingSceneView;
            Vector3 screenPos = view.camera.WorldToScreenPoint(worldPos);
            Vector2 size = GUI.skin.label.CalcSize(new GUIContent(text));
            GUI.Label(new Rect(screenPos.x - (size.x / 2), -screenPos.y + view.position.height + 4, size.x, size.y), text);

            Handles.EndGUI();

            GUI.color = defaultColor;
            #endif
        }

        /// <summary>
        /// Draw directional arrow
        /// </summary>
        public static void DrawArrowRay(Vector3 position, Vector3 direction, float headLength = 0.25f,
                                        float headAngle = 20.0f)
        {
            #if UNITY_EDITOR
            var rightVector = new Vector3(0, 0, 1);
            var directionRotation = Quaternion.LookRotation(direction);

            UnityEngine.Debug.DrawRay(position, direction);
            Vector3 right = directionRotation * Quaternion.Euler(0, 180 + headAngle, 0) * rightVector;
            Vector3 left = directionRotation * Quaternion.Euler(0, 180 - headAngle, 0) * rightVector;
            UnityEngine.Debug.DrawRay(position + direction, right * headLength);
            UnityEngine.Debug.DrawRay(position + direction, left * headLength);
            #endif
        }

        /// <summary>
        /// Draw XYZ dimensional RGB cross
        /// </summary>
        public static void DrawDimensionalCross(Vector3 position, float size)
        {
            #if UNITY_EDITOR
            var halfSize = size / 2;
            UnityEngine.Debug.DrawLine(OffsetY(position, -halfSize), OffsetY(position, halfSize), Color.green, .2f);
            UnityEngine.Debug.DrawLine(OffsetX(position, -halfSize), OffsetX(position, halfSize), Color.red, .2f);
            UnityEngine.Debug.DrawLine(OffsetZ(position, -halfSize), OffsetZ(position, halfSize), Color.blue, .2f);
            #endif
        }

        #endregion

        #region Helper Methods (to avoid Extensions dependency)

        /// <summary>
        /// Converts Color to hex string (minimal implementation to avoid Extensions dependency)
        /// </summary>
        private static string ColorToHex(Color color, bool includeAlpha = false)
        {
            Color32 color32 = color;
            var result = color32.r.ToString("X2") + color32.g.ToString("X2") + color32.b.ToString("X2");
            if (includeAlpha) result += color32.a.ToString("X2");
            return result;
        }

        /// <summary>
        /// Surround string with color tag (minimal implementation to avoid Extensions dependency)
        /// </summary>
        private static string Colored(string message, string colorCode)
        {
            return $"<color={colorCode}>{message}</color>";
        }

        /// <summary>
        /// Offset Vector3 X component (minimal implementation to avoid Extensions dependency)
        /// </summary>
        private static Vector3 OffsetX(Vector3 vector, float x)
        {
            return new Vector3(vector.x + x, vector.y, vector.z);
        }

        /// <summary>
        /// Offset Vector3 Y component (minimal implementation to avoid Extensions dependency)
        /// </summary>
        private static Vector3 OffsetY(Vector3 vector, float y)
        {
            return new Vector3(vector.x, vector.y + y, vector.z);
        }

        /// <summary>
        /// Offset Vector3 Z component (minimal implementation to avoid Extensions dependency)
        /// </summary>
        private static Vector3 OffsetZ(Vector3 vector, float z)
        {
            return new Vector3(vector.x, vector.y, vector.z + z);
        }

        #endregion

        #region Assert

        /// <summary>
        /// When condition is false, logs an error via DebugX (System channel). Compiles out in release builds.
        /// </summary>
        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void Assert(bool condition, string message)
        {
            if (!condition)
                Logger(LogChannels.DevTools).Error("Assert failed: {Message}", message ?? "unknown");
        }

        /// <summary>
        /// When value is null, logs an error with argument name. Compiles out in release builds.
        /// </summary>
        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void AssertNotNull<T>(T value, string argumentName) where T : class
        {
            if (value == null)
                Logger(LogChannels.DevTools).Error("Assert failed: {ArgumentName} is null", argumentName ?? "argument");
        }

        #endregion
    }
}