#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AetherNexus.FoundationPlatform.Utilities.Menus;
using UnityEngine.Networking;

namespace AetherNexus.FoundationPlatform.Editor.Tools
{
    
using AetherNexus.FoundationPlatform.DebugX;
    
public class DownloadSoundFromStoryBlock : EditorWindow
{
    private string websiteURL = "https://example.com";
    private string mp3Link = "";
    private string downloadLocation = "";
    private bool downloading = false;
    private bool fetching = false;
    private float downloadProgress = 0f;
    private string downloadStatus = "";
    private string errorMessage = "";
    private UnityWebRequest webRequest;
    private UnityWebRequest downloadRequest;
    private CancellationTokenSource cancellationTokenSource;

    [MenuItem(MenuPaths.Utilities.DownloadSound, false, MenuPriorities.Utilities + 1)]
    static void Init()
    {
        DownloadSoundFromStoryBlock window = GetWindow<DownloadSoundFromStoryBlock>("Story Block Audio Downloader");
        window.minSize = new Vector2(400, 300);
        window.Show();
    }

    private void OnEnable()
    {
        // Initialize download location to a sensible default
        if (string.IsNullOrEmpty(downloadLocation))
        {
            downloadLocation = Path.Combine(Application.dataPath, "Audio", "Downloaded");
        }
    }

    private void OnDisable()
    {
        // Clean up any ongoing operations
        CancelOperations();
    }

    void OnGUI()
    {
        EditorGUILayout.Space(10);
        
        // Title
        EditorGUILayout.LabelField("Story Block Audio Downloader", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);
        
        // URL Input
        EditorGUILayout.LabelField("Story Block URL:", EditorStyles.label);
        websiteURL = EditorGUILayout.TextField(websiteURL);
        
        // URL Validation
        if (!IsValidUrl(websiteURL) && !string.IsNullOrEmpty(websiteURL))
        {
            EditorGUILayout.HelpBox("Please enter a valid URL (e.g., https://example.com)", MessageType.Warning);
        }
        
        EditorGUILayout.Space(10);
        
        // Download Location
        EditorGUILayout.LabelField("Download Location:", EditorStyles.label);
        GUILayout.BeginHorizontal();
        EditorGUILayout.TextField(downloadLocation, EditorStyles.textField);
        if (GUILayout.Button("Browse", GUILayout.Width(60)))
        {
            SetDownloadLocation();
        }
        GUILayout.EndHorizontal();
        
        EditorGUILayout.Space(10);
        
        // Action Buttons
        GUILayout.BeginHorizontal();
        
        GUI.enabled = !fetching && !downloading && IsValidUrl(websiteURL);
        if (GUILayout.Button("Fetch & Download", GUILayout.Height(30)))
        {
            _ = FetchAndDownloadAsync();
        }
        GUI.enabled = true;
        
        if ((fetching || downloading) && GUILayout.Button("Cancel", GUILayout.Width(80), GUILayout.Height(30)))
        {
            CancelOperations();
        }
        
        GUILayout.EndHorizontal();
        
        EditorGUILayout.Space(10);
        
        // Status Display
        if (!string.IsNullOrEmpty(errorMessage))
        {
            EditorGUILayout.HelpBox(errorMessage, MessageType.Error);
        }
        
        if (fetching)
        {
            EditorGUILayout.HelpBox("Fetching webpage content...", MessageType.Info);
        }
        
        if (downloading)
        {
            EditorGUILayout.LabelField("Downloading:", EditorStyles.label);
            Rect progressRect = GUILayoutUtility.GetRect(0, 20, GUILayout.ExpandWidth(true));
            EditorGUI.ProgressBar(progressRect, downloadProgress, downloadStatus);
        }
        
        if (!string.IsNullOrEmpty(mp3Link))
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Found MP3 Link:", EditorStyles.label);
            EditorGUILayout.SelectableLabel(mp3Link, EditorStyles.textField, GUILayout.Height(20));
        }
    }

    private bool IsValidUrl(string url)
    {
        if (string.IsNullOrEmpty(url))
            return false;
            
        return Uri.TryCreate(url, UriKind.Absolute, out Uri result) && 
               (result.Scheme == Uri.UriSchemeHttp || result.Scheme == Uri.UriSchemeHttps);
    }

    private async Task FetchAndDownloadAsync()
    {
        try
        {
            errorMessage = "";
            fetching = true;

            // Wire up a fresh cancellation source so the in-loop token checks work from the first run.
            cancellationTokenSource?.Dispose();
            cancellationTokenSource = new CancellationTokenSource();

            Repaint();

            // Fetch the webpage
            string htmlSource = await FetchWebpageAsync();
            if (string.IsNullOrEmpty(htmlSource))
            {
                errorMessage = "Failed to fetch webpage content.";
                return;
            }

            // Find MP3 links
            mp3Link = ExtractMp3Link(htmlSource);
            if (string.IsNullOrEmpty(mp3Link))
            {
                errorMessage = "No MP3 links found on the webpage.";
                return;
            }

            // Start download
            await DownloadFileAsync();
        }
        catch (Exception ex)
        {
            errorMessage = $"Error: {ex.Message}";
            Debug.LogError($"DownloadSoundFromStoryBlock Error: {ex}");
        }
        finally
        {
            fetching = false;
            Repaint();
        }
    }

    private async Task<string> FetchWebpageAsync()
    {
        webRequest = UnityWebRequest.Get(websiteURL);
        
        try
        {
            var operation = webRequest.SendWebRequest();
            
            while (!operation.isDone)
            {
                if (cancellationTokenSource?.Token.IsCancellationRequested == true)
                {
                    webRequest.Abort();
                    return null;
                }
                await Task.Yield();
            }

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                errorMessage = $"Failed to fetch webpage: {webRequest.error}";
                return null;
            }

            return webRequest.downloadHandler.text;
        }
        finally
        {
            webRequest?.Dispose();
            webRequest = null;
        }
    }

    private string ExtractMp3Link(string htmlSource)
    {
        // Use regex to find MP3 links more reliably
        var mp3Pattern = @"https?://[^""'\s]+\.mp3(?:\?[^""'\s]*)?";
        var matches = Regex.Matches(htmlSource, mp3Pattern, RegexOptions.IgnoreCase);
        
        if (matches.Count > 0)
        {
            // Return the first valid MP3 link found
            return matches[0].Value;
        }

        // Fallback to the old method if regex doesn't find anything
        int firstIndex = htmlSource.IndexOf(".mp3");
        if (firstIndex == -1) return null;
        
        int secondIndex = htmlSource.IndexOf(".mp3", firstIndex + 1);
        if (secondIndex == -1) return null;

        int startIndex = htmlSource.LastIndexOf('"', secondIndex) + 1;
        int endIndex = htmlSource.IndexOf('"', secondIndex);
        
        if (startIndex > 0 && endIndex > startIndex)
        {
            return htmlSource.Substring(startIndex, endIndex - startIndex);
        }

        return null;
    }

    private async Task DownloadFileAsync()
    {
        try
        {
            downloading = true;
            downloadProgress = 0f;
            downloadStatus = "Preparing download...";
            Repaint();

            // Ensure download directory exists
            if (!Directory.Exists(downloadLocation))
            {
                Directory.CreateDirectory(downloadLocation);
            }

            // Extract filename from URL
            string filename = Path.GetFileName(new Uri(mp3Link).LocalPath);
            if (string.IsNullOrEmpty(filename))
            {
                filename = "downloaded_audio.mp3";
            }

            string filePath = Path.Combine(downloadLocation, filename);

            // Use UnityWebRequest for download
            downloadRequest = UnityWebRequest.Get(mp3Link);
            downloadRequest.downloadHandler = new DownloadHandlerFile(filePath);

            var operation = downloadRequest.SendWebRequest();

            while (!operation.isDone)
            {
                if (cancellationTokenSource?.Token.IsCancellationRequested == true)
                {
                    downloadRequest.Abort();
                    downloading = false;
                    downloadStatus = "Download cancelled";
                    return;
                }

                downloadProgress = operation.progress;
                downloadStatus = $"Downloading... {Mathf.RoundToInt(downloadProgress * 100)}%";
                Repaint();
                await Task.Yield();
            }

            if (downloadRequest.result == UnityWebRequest.Result.Success)
            {
                downloadStatus = "Download Complete!";
                downloadProgress = 1f;
                DebugX.Debug($"Audio file downloaded successfully to: {filePath}");
                
                // Refresh the asset database to show the new file
                AssetDatabase.Refresh();
            }
            else
            {
                errorMessage = $"Download failed: {downloadRequest.error}";
                downloadStatus = "Download Failed";
            }
        }
        catch (Exception ex)
        {
            errorMessage = $"Download error: {ex.Message}";
            downloadStatus = "Download Failed";
            Debug.LogError($"DownloadSoundFromStoryBlock Download Error: {ex}");
        }
        finally
        {
            downloading = false;
            downloadRequest?.Dispose();
            downloadRequest = null;
            Repaint();
        }
    }

    private void SetDownloadLocation()
    {
        string newLocation = EditorUtility.OpenFolderPanel("Select Download Location", downloadLocation, "");
        
        if (!string.IsNullOrEmpty(newLocation))
        {
            downloadLocation = newLocation;
            // Clear any previous error messages when location is changed
            errorMessage = "";
        }
    }

    private void CancelOperations()
    {
        // Cancel any ongoing operations
        cancellationTokenSource?.Cancel();
        cancellationTokenSource?.Dispose();
        cancellationTokenSource = new CancellationTokenSource();

        // Abort any ongoing web requests
        webRequest?.Abort();
        downloadRequest?.Abort();

        // Reset states
        fetching = false;
        downloading = false;
        downloadProgress = 0f;
        downloadStatus = "";
        errorMessage = "";

        Repaint();
    }
}
}
#endif