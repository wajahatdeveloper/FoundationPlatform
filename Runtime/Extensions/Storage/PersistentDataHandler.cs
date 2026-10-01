using System;
using UnityEngine;

namespace AetherNexus.FoundationPlatform.Extensions
{
/// <summary>
/// Canonical path for device-local keyed persistence in new code (JSON-backed via
/// <see cref="IPersistentDataAdapter"/>). <see cref="PlayerPrefsX"/> is the legacy binary-codec
/// alternative, kept for existing callers only.
/// </summary>
public static class PersistentDataHandler
{
    private const string CacheInitKey = "CacheInit";

    private static string dataFilePath = "";
    private static IPersistentDataAdapter adapter;
    private static bool isInitialized = false;
    private static readonly object initLock = new object();

    private static void EnsureInitialized()
    {
        if (isInitialized)
        {
            return;
        }

        lock (initLock)
        {
            if (isInitialized)
            {
                return;
            }

            Initialize();
            isInitialized = true;
        }
    }

    private static void Initialize()
    {
        if (dataFilePath == "") { dataFilePath = Application.persistentDataPath + "/gameData.json"; }

#if UNITY_EDITOR
        //* dataFilePath =  Application.persistentDataPath + $"/gameData_{MultiPlay.Utils.GetCurrentCloneIndex()}.json";
#endif

        adapter = new UnityJsonPersistentDataAdapter();
        adapter.Initialize(dataFilePath);
    }

    internal static void OnApplicationFocusChanged(bool hasFocus)
    {
        if (adapter != null)
        {
            adapter.OnApplicationFocusChanged(hasFocus);
        }
    }

    internal static void OnApplicationPauseChanged(bool isPaused)
    {
        if (adapter != null)
        {
            adapter.OnApplicationPauseChanged(isPaused);
        }
    }

    internal static void OnApplicationQuitting()
    {
        if (adapter != null)
        {
            adapter.OnApplicationQuitting();
        }
    }

    // The adapter caches file contents in memory; a new Play session re-reads the file.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        lock (initLock)
        {
            dataFilePath = "";
            adapter = null;
            isInitialized = false;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void SubscribeLifecycle()
    {
        PlatformHost.FocusChanged -= OnApplicationFocusChanged;
        PlatformHost.PauseChanged -= OnApplicationPauseChanged;
        PlatformHost.Quitting -= OnApplicationQuitting;
        PlatformHost.FocusChanged += OnApplicationFocusChanged;
        PlatformHost.PauseChanged += OnApplicationPauseChanged;
        PlatformHost.Quitting += OnApplicationQuitting;
    }

    public static bool ContainsKey(string key)
    {
        EnsureInitialized();
        return adapter.ContainsKey(key);
    }

    public static void SetData<T>(string key, T value)
    {
        EnsureInitialized();
        adapter.SetData(key, value);
    }

    public static T GetData<T>(string key, T defaultValue)
    {
        EnsureInitialized();
        return adapter.GetData(key, defaultValue);
    }

    /// <summary>Gets data using the default value of T.</summary>
    public static T GetData<T>(string key) => GetData(key, default(T));
}
}
