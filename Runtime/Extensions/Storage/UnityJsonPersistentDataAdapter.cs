using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Unity JsonUtility-based persistent data adapter.
/// </summary>
namespace AetherNexus.FoundationPlatform.Extensions
{
    
using AetherNexus.FoundationPlatform.DebugX;
    
public class UnityJsonPersistentDataAdapter : IPersistentDataAdapter
{
    [Serializable]
    private class KeyValuePair
    {
        public string key;
        public string value;

        public KeyValuePair() { }

        public KeyValuePair(string k, string v)
        {
            key = k;
            value = v;
        }
    }

    [Serializable]
    private class DataContainer
    {
        public List<KeyValuePair> entries = new List<KeyValuePair>();
    }

    private string dataFilePath;
    private DataContainer dataContainer;
    private Dictionary<string, string> dataCache;
    private bool isInitialized = false;
    private readonly object initLock = new object();

    public void Initialize(string filePath)
    {
        lock (initLock)
        {
            if (isInitialized)
            {
                return;
            }

            dataFilePath = filePath;
            dataContainer = new DataContainer();
            dataCache = new Dictionary<string, string>();
            LoadFile();
            isInitialized = true;
        }
    }

    public bool ContainsKey(string key)
    {
        EnsureInitialized();
        return dataCache.ContainsKey(key);
    }

    [Serializable]
    private class ValueWrapper<T>
    {
        public T value;
    }

    public void SetData<T>(string key, T value)
    {
        EnsureInitialized();
        
        try
        {
            // Wrap value in a container for JsonUtility serialization
            var wrapper = new ValueWrapper<T> { value = value };
            string jsonValue = JsonUtility.ToJson(wrapper);
            // dataCache is the single source of truth; entries are rebuilt at SaveFile time.
            dataCache[key] = jsonValue;
        }
        catch (Exception e)
        {
            DebugX.Logger(LogChannels.DevTools).Error(e, "Failed to serialize value for key: {Key}", key);
            throw;
        }
    }

    public T GetData<T>(string key, T defaultValue)
    {
        EnsureInitialized();

        if (!dataCache.TryGetValue(key, out string jsonValue))
        {
            return defaultValue;
        }

        try
        {
            var wrapper = JsonUtility.FromJson<ValueWrapper<T>>(jsonValue);
            return wrapper != null ? wrapper.value : defaultValue;
        }
        catch (Exception e)
        {
            DebugX.Logger(LogChannels.DevTools).Error(e, "Failed to deserialize value for key: {Key}", key);
            return defaultValue;
        }
    }

    public bool FileExists(string filePath)
    {
        return File.Exists(filePath);
    }

    public void SaveFile()
    {
        if (!isInitialized)
        {
            return;
        }

        if (string.IsNullOrEmpty(dataFilePath))
        {
            DebugX.Logger(LogChannels.DevTools).Warning("Cannot save persistent data: file path is empty");
            return;
        }

        DebugX.Logger(LogChannels.DevTools).Info("Saving persistent data to file. Path: {FilePath}", dataFilePath);

        try
        {
            // Rebuild entries from the authoritative cache before serializing.
            dataContainer.entries.Clear();
            foreach (var kvp in dataCache)
            {
                dataContainer.entries.Add(new KeyValuePair(kvp.Key, kvp.Value));
            }

            string json = JsonUtility.ToJson(dataContainer, true);
            File.WriteAllText(dataFilePath, json);
        }
        catch (Exception e)
        {
            DebugX.Logger(LogChannels.DevTools).Error(e, "Failed to save persistent data file. Path: {FilePath}", dataFilePath);
        }
    }

    public void LoadFile()
    {
        if (!FileExists(dataFilePath))
        {
            DebugX.Logger(LogChannels.DevTools).Info("Persistent data file does not exist. Path: {FilePath}", dataFilePath);
            dataContainer = new DataContainer();
            return;
        }

        DebugX.Logger(LogChannels.DevTools).Info("Loading persistent data from file. Path: {FilePath}", dataFilePath);

        try
        {
            string json = File.ReadAllText(dataFilePath);
            if (!string.IsNullOrEmpty(json))
            {
                dataContainer = JsonUtility.FromJson<DataContainer>(json);
                if (dataContainer == null)
                {
                    dataContainer = new DataContainer();
                }
                if (dataContainer.entries == null)
                {
                    dataContainer.entries = new List<KeyValuePair>();
                }
                
                // Rebuild cache from entries
                dataCache.Clear();
                foreach (var entry in dataContainer.entries)
                {
                    if (entry != null && !string.IsNullOrEmpty(entry.key))
                    {
                        dataCache[entry.key] = entry.value;
                    }
                }
            }
            else
            {
                dataContainer = new DataContainer();
                dataCache.Clear();
            }
        }
        catch (Exception e)
        {
            DebugX.Logger(LogChannels.DevTools).Error(e, "Failed to load persistent data file. Path: {FilePath}", dataFilePath);
            dataContainer = new DataContainer();
            dataCache.Clear();
        }
    }

    public void OnApplicationFocusChanged(bool hasFocus)
    {
        DebugX.Logger(LogChannels.DevTools).Info("Application focus changed. HasFocus: {HasFocus}", hasFocus);
        if (!hasFocus)
        {
            SaveFile();
        }
    }

    public void OnApplicationPauseChanged(bool isPaused)
    {
        DebugX.Logger(LogChannels.DevTools).Info("Application pause changed. IsPaused: {IsPaused}", isPaused);
        if (isPaused)
        {
            SaveFile();
        }
    }

    public void OnApplicationQuitting()
    {
        DebugX.Logger(LogChannels.DevTools).Info("Application quitting - saving persistent data");
        SaveFile();
    }

    private void EnsureInitialized()
    {
        if (!isInitialized)
        {
            const string msg = "Adapter not initialized. Call Initialize() first.";
            DebugX.Logger(LogChannels.DevTools).Error("[Storage:ERROR] {Message}", msg);
            throw new InvalidOperationException(msg);
        }
    }
}
}

