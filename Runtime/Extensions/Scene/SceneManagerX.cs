using System;
using System.Collections;
using AetherNexus.FoundationPlatform.Logging;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AetherNexus.FoundationPlatform.Extensions
{
    using CoroutineX = AetherNexus.FoundationPlatform.CoroutineX.CoroutineX;
    using DebugX = AetherNexus.FoundationPlatform.Logging.DebugX;
    
public static class SceneManagerX
{
    private static void LogSceneErrorAndThrowInvalidOp(string message)
    {
        DebugX.Logger(LogChannels.SceneTransition).Error("[Scene:ERROR] {Message}", message);
        throw new InvalidOperationException(message);
    }

    private static void LogSceneErrorAndThrowArg(string message, string paramName)
    {
        DebugX.Logger(LogChannels.SceneTransition).Error("[Scene:ERROR] {Message}", message);
        throw new ArgumentException(message, paramName);
    }

    private static void LogSceneErrorAndThrowArgOutOfRange(string paramName, object actualValue, string message)
    {
        DebugX.Logger(LogChannels.SceneTransition).Error("[Scene:ERROR] {Message}", message);
        throw new ArgumentOutOfRangeException(paramName, actualValue, message);
    }

    private static void ValidateSceneIndex(int index, string paramName)
    {
        int count = SceneManager.sceneCountInBuildSettings;
        if (index < 0 || index >= count)
            LogSceneErrorAndThrowArgOutOfRange(paramName, index, $"Scene index must be in range [0, {count}).");
    }

    private static IEnumerator WaitForAllOps(AsyncOperation[] ops)
    {
        if (ops == null || ops.Length == 0)
            yield break;
        for (int i = 0; i < ops.Length; i++)
            yield return ops[i];
    }

    private static IEnumerator UnloadSceneAsyncCore(AsyncOperation op)
    {
        if (op != null)
            yield return op;
    }

    private static IEnumerator LoadSceneAsyncCore(int mainIndex, int[] preAdditiveIndices, int[] postAdditiveIndices, string[] preAdditiveSceneNames, string[] postAdditiveSceneNames)
    {
        Scene sceneToUnload = SceneManager.GetActiveScene();

        if (preAdditiveIndices != null && preAdditiveIndices.Length > 0)
        {
            var ops = new AsyncOperation[preAdditiveIndices.Length];
            for (int i = 0; i < preAdditiveIndices.Length; i++)
            {
                var op = SceneManager.LoadSceneAsync(preAdditiveIndices[i], LoadSceneMode.Additive);
                if (op == null)
                    LogSceneErrorAndThrowInvalidOp("Failed to start pre additive scene load for index " + preAdditiveIndices[i]);
                ops[i] = op;
            }
            yield return WaitForAllOps(ops);
        }
        else if (preAdditiveSceneNames != null && preAdditiveSceneNames.Length > 0)
        {
            var ops = new AsyncOperation[preAdditiveSceneNames.Length];
            for (int i = 0; i < preAdditiveSceneNames.Length; i++)
            {
                var op = SceneManager.LoadSceneAsync(preAdditiveSceneNames[i], LoadSceneMode.Additive);
                if (op == null)
                    LogSceneErrorAndThrowInvalidOp("Failed to start pre additive scene load for " + preAdditiveSceneNames[i]);
                ops[i] = op;
            }
            yield return WaitForAllOps(ops);
        }

        DebugX.Logger(LogChannels.SceneTransition).Info("Loading scene {SceneIndex}", mainIndex);
        var mainOp = SceneManager.LoadSceneAsync(mainIndex, LoadSceneMode.Additive);
        if (mainOp == null)
            LogSceneErrorAndThrowInvalidOp("Failed to start scene load for index " + mainIndex);
        yield return mainOp;

        Scene mainScene = SceneManager.GetSceneByBuildIndex(mainIndex);
        if (!mainScene.IsValid() || !mainScene.isLoaded)
            LogSceneErrorAndThrowInvalidOp("Failed to load main scene for index " + mainIndex);
        SceneManager.SetActiveScene(mainScene);

        if (sceneToUnload.isLoaded)
        {
            var unloadOp = SceneManager.UnloadSceneAsync(sceneToUnload);
            if (unloadOp != null)
                yield return unloadOp;
        }

        if (postAdditiveIndices != null && postAdditiveIndices.Length > 0)
        {
            var ops = new AsyncOperation[postAdditiveIndices.Length];
            for (int i = 0; i < postAdditiveIndices.Length; i++)
            {
                var op = SceneManager.LoadSceneAsync(postAdditiveIndices[i], LoadSceneMode.Additive);
                if (op == null)
                    LogSceneErrorAndThrowInvalidOp("Failed to start post additive scene load for index " + postAdditiveIndices[i]);
                ops[i] = op;
            }
            yield return WaitForAllOps(ops);
        }
        else if (postAdditiveSceneNames != null && postAdditiveSceneNames.Length > 0)
        {
            var ops = new AsyncOperation[postAdditiveSceneNames.Length];
            for (int i = 0; i < postAdditiveSceneNames.Length; i++)
            {
                var op = SceneManager.LoadSceneAsync(postAdditiveSceneNames[i], LoadSceneMode.Additive);
                if (op == null)
                    LogSceneErrorAndThrowInvalidOp("Failed to start post additive scene load for " + postAdditiveSceneNames[i]);
                ops[i] = op;
            }
            yield return WaitForAllOps(ops);
        }
    }

    private static IEnumerator LoadSceneAsyncCoreByName(string mainSceneName, int[] preAdditiveIndices, int[] postAdditiveIndices, string[] preAdditiveSceneNames, string[] postAdditiveSceneNames)
    {
        Scene sceneToUnload = SceneManager.GetActiveScene();

        if (preAdditiveIndices != null && preAdditiveIndices.Length > 0)
        {
            var ops = new AsyncOperation[preAdditiveIndices.Length];
            for (int i = 0; i < preAdditiveIndices.Length; i++)
            {
                var op = SceneManager.LoadSceneAsync(preAdditiveIndices[i], LoadSceneMode.Additive);
                if (op == null)
                    LogSceneErrorAndThrowInvalidOp("Failed to start pre additive scene load for index " + preAdditiveIndices[i]);
                ops[i] = op;
            }
            yield return WaitForAllOps(ops);
        }
        else if (preAdditiveSceneNames != null && preAdditiveSceneNames.Length > 0)
        {
            var ops = new AsyncOperation[preAdditiveSceneNames.Length];
            for (int i = 0; i < preAdditiveSceneNames.Length; i++)
            {
                var op = SceneManager.LoadSceneAsync(preAdditiveSceneNames[i], LoadSceneMode.Additive);
                if (op == null)
                    LogSceneErrorAndThrowInvalidOp("Failed to start pre additive scene load for " + preAdditiveSceneNames[i]);
                ops[i] = op;
            }
            yield return WaitForAllOps(ops);
        }

        DebugX.Logger(LogChannels.SceneTransition).Info("Loading scene {SceneName}", mainSceneName);
        var mainOp = SceneManager.LoadSceneAsync(mainSceneName, LoadSceneMode.Additive);
        if (mainOp == null)
            LogSceneErrorAndThrowInvalidOp("Failed to start scene load for " + mainSceneName);
        yield return mainOp;

        Scene mainScene = SceneManager.GetSceneByName(mainSceneName);
        if (!mainScene.IsValid() || !mainScene.isLoaded)
            LogSceneErrorAndThrowInvalidOp("Failed to load main scene for " + mainSceneName);
        SceneManager.SetActiveScene(mainScene);

        if (sceneToUnload.isLoaded)
        {
            var unloadOp = SceneManager.UnloadSceneAsync(sceneToUnload);
            if (unloadOp != null)
                yield return unloadOp;
        }

        if (postAdditiveIndices != null && postAdditiveIndices.Length > 0)
        {
            var ops = new AsyncOperation[postAdditiveIndices.Length];
            for (int i = 0; i < postAdditiveIndices.Length; i++)
            {
                var op = SceneManager.LoadSceneAsync(postAdditiveIndices[i], LoadSceneMode.Additive);
                if (op == null)
                    LogSceneErrorAndThrowInvalidOp("Failed to start post additive scene load for index " + postAdditiveIndices[i]);
                ops[i] = op;
            }
            yield return WaitForAllOps(ops);
        }
        else if (postAdditiveSceneNames != null && postAdditiveSceneNames.Length > 0)
        {
            var ops = new AsyncOperation[postAdditiveSceneNames.Length];
            for (int i = 0; i < postAdditiveSceneNames.Length; i++)
            {
                var op = SceneManager.LoadSceneAsync(postAdditiveSceneNames[i], LoadSceneMode.Additive);
                if (op == null)
                    LogSceneErrorAndThrowInvalidOp("Failed to start post additive scene load for " + postAdditiveSceneNames[i]);
                ops[i] = op;
            }
            yield return WaitForAllOps(ops);
        }
    }

    public static CoroutineX LoadSceneAsync(int index)
    {
        ValidateSceneIndex(index, nameof(index));
        return CoroutineX.Run(LoadSceneAsyncCore(index, null, null, null, null));
    }

    public static CoroutineX LoadSceneAsync(int mainIndex, int[] additiveIndices)
    {
        return LoadSceneAsync(mainIndex, null, additiveIndices);
    }

    public static CoroutineX LoadSceneAsync(int mainIndex, string[] additiveSceneNames)
    {
        return LoadSceneAsync(mainIndex, null, additiveSceneNames);
    }

    public static CoroutineX LoadSceneAsync(int mainIndex, int[] preAdditiveIndices, int[] postAdditiveIndices)
    {
        ValidateSceneIndex(mainIndex, nameof(mainIndex));
        if (preAdditiveIndices != null)
            for (int i = 0; i < preAdditiveIndices.Length; i++)
                ValidateSceneIndex(preAdditiveIndices[i], $"{nameof(preAdditiveIndices)}[{i}]");
        if (postAdditiveIndices != null)
            for (int i = 0; i < postAdditiveIndices.Length; i++)
                ValidateSceneIndex(postAdditiveIndices[i], $"{nameof(postAdditiveIndices)}[{i}]");
        return CoroutineX.Run(LoadSceneAsyncCore(mainIndex, preAdditiveIndices, postAdditiveIndices, null, null));
    }

    public static CoroutineX LoadSceneAsync(int mainIndex, string[] preAdditiveSceneNames, string[] postAdditiveSceneNames)
    {
        ValidateSceneIndex(mainIndex, nameof(mainIndex));
        if (preAdditiveSceneNames != null)
            for (int i = 0; i < preAdditiveSceneNames.Length; i++)
                if (string.IsNullOrEmpty(preAdditiveSceneNames[i]))
                    LogSceneErrorAndThrowArg($"Pre additive scene name at index {i} is null or empty.", nameof(preAdditiveSceneNames));
        if (postAdditiveSceneNames != null)
            for (int i = 0; i < postAdditiveSceneNames.Length; i++)
                if (string.IsNullOrEmpty(postAdditiveSceneNames[i]))
                    LogSceneErrorAndThrowArg($"Post additive scene name at index {i} is null or empty.", nameof(postAdditiveSceneNames));
        return CoroutineX.Run(LoadSceneAsyncCore(mainIndex, null, null, preAdditiveSceneNames, postAdditiveSceneNames));
    }

    public static CoroutineX LoadSceneAsync(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
            LogSceneErrorAndThrowArg("Scene name must not be null or empty.", nameof(sceneName));
        return CoroutineX.Run(LoadSceneAsyncCoreByName(sceneName, null, null, null, null));
    }

    public static CoroutineX LoadSceneAsync(string mainSceneName, int[] additiveIndices)
    {
        return LoadSceneAsync(mainSceneName, null, additiveIndices);
    }

    public static CoroutineX LoadSceneAsync(string mainSceneName, string[] additiveSceneNames)
    {
        return LoadSceneAsync(mainSceneName, null, additiveSceneNames);
    }

    public static CoroutineX LoadSceneAsync(string mainSceneName, int[] preAdditiveIndices, int[] postAdditiveIndices)
    {
        if (string.IsNullOrEmpty(mainSceneName))
            LogSceneErrorAndThrowArg("Scene name must not be null or empty.", nameof(mainSceneName));
        if (preAdditiveIndices != null)
            for (int i = 0; i < preAdditiveIndices.Length; i++)
                ValidateSceneIndex(preAdditiveIndices[i], $"{nameof(preAdditiveIndices)}[{i}]");
        if (postAdditiveIndices != null)
            for (int i = 0; i < postAdditiveIndices.Length; i++)
                ValidateSceneIndex(postAdditiveIndices[i], $"{nameof(postAdditiveIndices)}[{i}]");
        return CoroutineX.Run(LoadSceneAsyncCoreByName(mainSceneName, preAdditiveIndices, postAdditiveIndices, null, null));
    }

    public static CoroutineX LoadSceneAsync(string mainSceneName, string[] preAdditiveSceneNames, string[] postAdditiveSceneNames)
    {
        if (string.IsNullOrEmpty(mainSceneName))
            LogSceneErrorAndThrowArg("Scene name must not be null or empty.", nameof(mainSceneName));
        if (preAdditiveSceneNames != null)
            for (int i = 0; i < preAdditiveSceneNames.Length; i++)
                if (string.IsNullOrEmpty(preAdditiveSceneNames[i]))
                    LogSceneErrorAndThrowArg($"Pre additive scene name at index {i} is null or empty.", nameof(preAdditiveSceneNames));
        if (postAdditiveSceneNames != null)
            for (int i = 0; i < postAdditiveSceneNames.Length; i++)
                if (string.IsNullOrEmpty(postAdditiveSceneNames[i]))
                    LogSceneErrorAndThrowArg($"Post additive scene name at index {i} is null or empty.", nameof(postAdditiveSceneNames));
        return CoroutineX.Run(LoadSceneAsyncCoreByName(mainSceneName, null, null, preAdditiveSceneNames, postAdditiveSceneNames));
    }

    public static CoroutineX UnloadSceneAsync(Scene scene)
    {
        return CoroutineX.Run(UnloadSceneAsyncCore(SceneManager.UnloadSceneAsync(scene)));
    }

    public static CoroutineX UnloadSceneAsync(int sceneBuildIndex)
    {
        return CoroutineX.Run(UnloadSceneAsyncCore(SceneManager.UnloadSceneAsync(sceneBuildIndex)));
    }

    public static CoroutineX UnloadSceneAsync(string sceneName)
    {
        return CoroutineX.Run(UnloadSceneAsyncCore(SceneManager.UnloadSceneAsync(sceneName)));
    }

    /// <summary>Delegate to <see cref="SceneManager.GetActiveScene"/>.</summary>
    public static Scene GetActiveScene() => SceneManager.GetActiveScene();

    /// <summary>Delegate to <see cref="SceneManager.sceneCount"/>.</summary>
    public static int sceneCount => SceneManager.sceneCount;

    /// <summary>Delegate to <see cref="SceneManager.sceneCountInBuildSettings"/>.</summary>
    public static int sceneCountInBuildSettings => SceneManager.sceneCountInBuildSettings;

    /// <summary>Delegate to <see cref="SceneManager.loadedSceneCount"/>.</summary>
    public static int loadedSceneCount => SceneManager.loadedSceneCount;

    /// <summary>Delegate to <see cref="SceneManager.GetSceneAt"/>.</summary>
    public static Scene GetSceneAt(int index) => SceneManager.GetSceneAt(index);

    /// <summary>Delegate to <see cref="SceneManager.GetSceneByBuildIndex"/>.</summary>
    public static Scene GetSceneByBuildIndex(int buildIndex) => SceneManager.GetSceneByBuildIndex(buildIndex);

    /// <summary>Delegate to <see cref="SceneManager.GetSceneByName"/>.</summary>
    public static Scene GetSceneByName(string name) => SceneManager.GetSceneByName(name);
}
}
