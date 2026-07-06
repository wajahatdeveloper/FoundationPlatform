using System;

/// <summary>
/// Interface for persistent data storage adapters.
/// Allows switching between different storage backends.
/// </summary>
public interface IPersistentDataAdapter
{
    /// <summary>
    /// Initialize the adapter with the specified file path.
    /// </summary>
    void Initialize(string filePath);

    /// <summary>
    /// Check if a key exists in the storage.
    /// </summary>
    bool ContainsKey(string key);

    /// <summary>
    /// Save data with the specified key.
    /// </summary>
    void SetData<T>(string key, T value);

    /// <summary>
    /// Load data with the specified key, returning defaultValue if not found.
    /// </summary>
    T GetData<T>(string key, T defaultValue = default);

    /// <summary>
    /// Check if the storage file exists.
    /// </summary>
    bool FileExists(string filePath);

    /// <summary>
    /// Save the current data to file.
    /// </summary>
    void SaveFile();

    /// <summary>
    /// Load data from file.
    /// </summary>
    void LoadFile();

    /// <summary>
    /// Called when application focus changes.
    /// </summary>
    void OnApplicationFocusChanged(bool hasFocus);

    /// <summary>
    /// Called when application pause state changes.
    /// </summary>
    void OnApplicationPauseChanged(bool isPaused);

    /// <summary>
    /// Called when application is quitting.
    /// </summary>
    void OnApplicationQuitting();
}

