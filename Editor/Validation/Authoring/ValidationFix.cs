#if UNITY_EDITOR
using System;

namespace AetherNexus.FoundationPlatform.Editor.Utilities.Validation
{
    /// <summary>
    /// One deterministic, one-click correction for an <see cref="AuthoringIssue"/>, paired with its own
    /// button wherever the issue is rendered. Only issues with exactly one obviously-correct action carry
    /// a fix; ambiguous ones (a missing clip, an unresolvable reference) stay manual.
    /// <para>
    /// Data the fix needs beyond the issue itself — a destination folder, a replacement asset — is captured
    /// in <see cref="Apply"/>'s closure rather than added as a field on the issue. That is what keeps
    /// <see cref="AuthoringIssue"/> free of per-subsystem payload.
    /// </para>
    /// </summary>
    public sealed class ValidationFix
    {
        public readonly string Message;

        public readonly Action Apply;

        /// <summary>
        /// True when applying asks the designer something (a destination folder, a replacement to pick).
        /// Batch "Fix common issues" skips these, so a bulk run never blocks on a dialog; they stay
        /// available one at a time.
        /// </summary>
        public readonly bool RequiresChoice;

        public ValidationFix(string message, Action apply)
            : this(message, apply, false)
        {
        }

        public ValidationFix(string message, Action apply, bool requiresChoice)
        {
            if (string.IsNullOrWhiteSpace(message))
                throw new ArgumentException("A fix needs a message; it is the button's own label context.", nameof(message));

            Message = message;
            Apply = apply ?? throw new ArgumentNullException(nameof(apply));
            RequiresChoice = requiresChoice;
        }
    }
}
#endif
