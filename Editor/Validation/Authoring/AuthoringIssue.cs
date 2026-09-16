#if UNITY_EDITOR
using System;

namespace AetherNexus.FoundationPlatform.Editor.Utilities.Validation
{
    /// <summary>
    /// Blocking, actionable, explanatory — the three levels every authoring surface shares. Severity
    /// semantics are fixed across tools by contract (see docs/13-AuthoringStandards.md): the same issue
    /// class must not be an Error in one surface and a Warning in another.
    /// </summary>
    public enum AuthoringIssueSeverity
    {
        Info = 0,
        Warning = 1,
        Error = 2
    }

    /// <summary>
    /// One authoring-time finding, whatever produced it. Every editor validator in the project emits this
    /// type, so the Project row badge, the Inspector strip, and Central Validation read one stream and
    /// speak one vocabulary rather than each rendering its own package's issue shape.
    /// <para>
    /// Deliberately carries no subsystem payload. The content-mapping fields that used to live on the hub's
    /// issue type (suggested folder, drift asset type) now ride inside <see cref="Fix"/>'s closure, which is
    /// what makes this type reusable by validators that have nothing to do with folder mapping.
    /// </para>
    /// </summary>
    [Serializable]
    public sealed class AuthoringIssue
    {
        public AuthoringIssueSeverity Severity;

        /// <summary>Which validator spoke, shown as the grouping key in Central Validation.</summary>
        public string Source;

        public string Message;

        /// <summary>Selection target for the row, when the finding is about something selectable.</summary>
        public UnityEngine.Object RelatedObject;

        public string AssetPath;

        /// <summary>Null when the correction is not deterministic enough to offer as a button.</summary>
        public ValidationFix Fix;

        public bool HasFix => Fix != null;

        public string GetDisplayMessage()
        {
            string message = Message ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(AssetPath) &&
                !message.StartsWith("Assets/", StringComparison.Ordinal))
            {
                return AssetPath + ": " + message;
            }

            if (string.IsNullOrWhiteSpace(message) && !string.IsNullOrWhiteSpace(AssetPath))
                return AssetPath;

            return message;
        }
    }
}
#endif
