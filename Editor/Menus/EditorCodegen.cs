#if UNITY_EDITOR
using System;
using System.IO;

namespace FoundationPlatform.Utilities.Menus
{
    /// <summary>
    /// Shared codegen file-write guard. Consolidates the identical <c>WriteIfChanged</c> that
    /// was duplicated in <c>Weaver</c> and <c>AbilityLogicRebuilder</c>. Only writes when the
    /// content actually differs, so codegen re-runs don't churn timestamps / trigger reimports.
    /// </summary>
    public static class EditorCodegen
    {
        /// <summary>
        /// Writes <paramref name="content"/> to <paramref name="path"/> only if the file is
        /// missing or its current contents differ (ordinal compare). Returns true if written.
        /// </summary>
        public static bool WriteIfChanged(string path, string content)
        {
            try
            {
                if (File.Exists(path) &&
                    string.Equals(File.ReadAllText(path), content, StringComparison.Ordinal))
                    return false;
                File.WriteAllText(path, content);
                return true;
            }
            catch (Exception e)
            {
                DebugX.Logger(LogChannels.Editor).Error("[Codegen] Failed to write {Path}: {Message}", path, e.Message);
                return false;
            }
        }
    }
}
#endif
