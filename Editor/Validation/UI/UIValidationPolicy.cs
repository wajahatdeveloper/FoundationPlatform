#if UNITY_EDITOR
using UnityEditor;

namespace AetherNexus.FoundationPlatform.Editor.Utilities.Validation.UI
{
    internal enum UIValidationRolloutMode
    {
        WarningFirst = 0,
        Strict = 1
    }

    internal static class UIValidationPolicy
    {
        private const string RolloutModePrefKey = "FoundationPlatform.UIValidation.RolloutMode";

        internal static UIValidationRolloutMode GetRolloutMode()
        {
            int value = EditorPrefs.GetInt(RolloutModePrefKey, (int)UIValidationRolloutMode.Strict);
            if (value != (int)UIValidationRolloutMode.WarningFirst && value != (int)UIValidationRolloutMode.Strict)
                return UIValidationRolloutMode.Strict;

            return (UIValidationRolloutMode)value;
        }

        internal static void SetRolloutMode(UIValidationRolloutMode mode)
        {
            EditorPrefs.SetInt(RolloutModePrefKey, (int)mode);
        }

        internal static UIValidationSeverity ResolveSeverity(string ruleId, UIValidationSeverity defaultSeverity)
        {
            UIValidationRolloutMode mode = GetRolloutMode();
            if (mode == UIValidationRolloutMode.Strict)
                return defaultSeverity;

            if (ruleId == UIValidationRuleIds.ConfigMissingOrInvalid)
                return UIValidationSeverity.Error;

            return UIValidationSeverity.Warning;
        }
    }
}
#endif
