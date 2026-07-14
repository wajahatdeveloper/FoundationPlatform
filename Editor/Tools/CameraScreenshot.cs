#if UNITY_EDITOR
using System;
using System.IO;
using AetherNexus.FoundationPlatform.DebugX;
using AetherNexus.FoundationPlatform.Utilities.Menus;
using UnityEditor;
using UnityEngine;

public static class CameraScreenshot
{
    private static readonly string screenshotsRoot =
        Path.Combine(Path.GetDirectoryName(Application.dataPath) ?? string.Empty, "Screenshots");

    public static void Take(Camera sourceCamera = null)
    {
        Camera camera = sourceCamera;
        if (camera == null)
            camera = Camera.main;

        if (camera == null && SceneView.lastActiveSceneView != null)
            camera = SceneView.lastActiveSceneView.camera;

        if (camera == null)
        {
            Debug.LogWarning("CameraScreenshot: No camera found to capture.");
            return;
        }

        int width = Math.Max(1, camera.pixelWidth);
        int height = Math.Max(1, camera.pixelHeight);

        RenderTexture previousActive = RenderTexture.active;
        RenderTexture previousTarget = camera.targetTexture;

        RenderTexture renderTexture = null;
        Texture2D texture = null;

        try
        {
            renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            renderTexture.Create();

            camera.targetTexture = renderTexture;
            RenderTexture.active = renderTexture;

            camera.Render();

            texture = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGBA32, false);
            texture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
            texture.Apply(false, false);

            if (!Directory.Exists(screenshotsRoot))
                Directory.CreateDirectory(screenshotsRoot);

            string filename = $"Screenshot_{Application.productName}_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.png";
            string fullPath = Path.Combine(screenshotsRoot, filename);

            byte[] png = texture.EncodeToPNG();
            File.WriteAllBytes(fullPath, png);

            DebugX.Debug($"Screenshot saved as \"{fullPath}\"");
        }
        catch (Exception ex)
        {
            Debug.LogError($"CameraScreenshot: Failed to take screenshot. {ex}");
        }
        finally
        {
            camera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;

            if (texture != null)
                UnityEngine.Object.DestroyImmediate(texture);

            if (renderTexture != null)
            {
                renderTexture.Release();
                UnityEngine.Object.DestroyImmediate(renderTexture);
            }
        }
    }

    [MenuItem(MenuPaths.Utilities.TakeScreenshot, false, MenuPriorities.Utilities)]
    private static void TakeScreenshot()
    {
        Take();
    }
}
#endif