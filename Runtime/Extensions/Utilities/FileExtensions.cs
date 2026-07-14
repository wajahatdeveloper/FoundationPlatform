using System.IO;

namespace AetherNexus.FoundationPlatform.Extensions
{
/// <summary>
/// File IO extensions
/// </summary>
public static class FileExtensions
{
    #region CreateDirectoryIfNotExists

    /// <summary>
    /// Creates a directory at <paramref name="folder"/> if it doesn't exist
    /// </summary>
    /// <param name="folder"></param>
    public static void CreateDirectoryIfNotExists(this string folder)
    {
        if (folder.IsNullOrEmpty())
            return;

        // Create the folder itself (the argument is a directory path, per the name/doc), not its parent.
        if (!Directory.Exists(folder))
            Directory.CreateDirectory(folder);
    }

    #endregion
}}
