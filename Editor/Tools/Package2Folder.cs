// new argument was added in 19.1.4

#if UNITY_2019_3_OR_NEWER
#define CS_P2F_NEW_ARGUMENT_2
#elif (UNITY_2019_1_OR_NEWER && !UNITY_2019_1_0 && !UNITY_2019_1_1 && !UNITY_2019_1_2 && !UNITY_2019_1_3) || (UNITY_2018_4_OR_NEWER && !UNITY_2018_4_0 && !UNITY_2018_4_1 && !UNITY_2018_4_2)
#define CS_P2F_NEW_ARGUMENT
#endif

#if UNITY_2019_3_OR_NEWER
#define CS_P2F_NEW_NON_INTERACTIVE_LOGIC
#endif

#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace FoundationPlatform.Editor.Utilities.Tools
{
    public static class Package2Folder
    {
        ///////////////////////////////////////////////////////////////
        // Delegates and properties with caching for reflection stuff
        ///////////////////////////////////////////////////////////////

        #region reflection stuff

#if CS_P2F_NEW_ARGUMENT_2
        private delegate object[] ExtractAndPrepareAssetListDelegate(string packagePath, out string packageIconPath,
            out string packageManagerDependenciesPath);
#elif CS_P2F_NEW_ARGUMENT
		private delegate object[] ExtractAndPrepareAssetListDelegate(string packagePath, out string packageIconPath, out bool allowReInstall, out string packageManagerDependenciesPath);
#else
		private delegate object[] ExtractAndPrepareAssetListDelegate(string packagePath, out string packageIconPath, out bool allowReInstall);
#endif

        private static Type packageUtilityType;

        private static Type PackageUtilityType
        {
            get
            {
                if (packageUtilityType == null)
                {
                    packageUtilityType = typeof(MenuItem).Assembly.GetType("UnityEditor.PackageUtility");
                    if (packageUtilityType == null)
                        throw new InvalidOperationException("Type 'UnityEditor.PackageUtility' not found. Unity API may have changed.");
                }
                return packageUtilityType;
            }
        }

        private static ExtractAndPrepareAssetListDelegate extractAndPrepareAssetList;

        private static ExtractAndPrepareAssetListDelegate ExtractAndPrepareAssetList
        {
            get
            {
                if (extractAndPrepareAssetList == null)
                {
                    var method = PackageUtilityType.GetMethod("ExtractAndPrepareAssetList", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                    if (method == null)
                        throw new InvalidOperationException("Couldn't find method 'ExtractAndPrepareAssetList' on UnityEditor.PackageUtility.");

                    extractAndPrepareAssetList = (ExtractAndPrepareAssetListDelegate)Delegate.CreateDelegate(
                        typeof(ExtractAndPrepareAssetListDelegate),
                        null,
                        method);
                }

                return extractAndPrepareAssetList;
            }
        }

        private static FieldInfo destinationAssetPathFieldInfo;

        private static FieldInfo DestinationAssetPathFieldInfo
        {
            get
            {
                if (destinationAssetPathFieldInfo == null)
                {
                    var importPackageItem = typeof(MenuItem).Assembly.GetType("UnityEditor.ImportPackageItem");
                    if (importPackageItem == null)
                        throw new InvalidOperationException("Type 'UnityEditor.ImportPackageItem' not found. Unity API may have changed.");
                    destinationAssetPathFieldInfo = importPackageItem.GetField("destinationAssetPath", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (destinationAssetPathFieldInfo == null)
                        throw new InvalidOperationException("Field 'destinationAssetPath' not found on UnityEditor.ImportPackageItem.");
                }

                return destinationAssetPathFieldInfo;
            }
        }

        private static MethodInfo importPackageAssetsMethodInfo;

        private static MethodInfo ImportPackageAssetsMethodInfo
        {
            get
            {
                if (importPackageAssetsMethodInfo == null)
                    importPackageAssetsMethodInfo = PackageUtilityType.GetMethod("ImportPackageAssets", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

                return importPackageAssetsMethodInfo;
            }
        }

        private static MethodInfo showImportPackageMethodInfo;

        private static MethodInfo ShowImportPackageMethodInfo
        {
            get
            {
                if (showImportPackageMethodInfo == null)
                {
                    var packageImport = typeof(MenuItem).Assembly.GetType("UnityEditor.PackageImport");
                    if (packageImport == null)
                        throw new InvalidOperationException("Type 'UnityEditor.PackageImport' not found. Unity API may have changed.");
                    showImportPackageMethodInfo = packageImport.GetMethod("ShowImportPackage", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                    if (showImportPackageMethodInfo == null)
                        throw new InvalidOperationException("Method 'ShowImportPackage' not found on UnityEditor.PackageImport.");
                }

                return showImportPackageMethodInfo;
            }
        }

        #endregion reflection stuff

        ///////////////////////////////////////////////////////////////
        // Unity Editor menus integration
        ///////////////////////////////////////////////////////////////

        [MenuItem("Assets/Import Package/Here...", true, 10)]
        private static bool IsImportToFolderCheck()
        {
            var selectedFolderPath = GetSelectedFolderPath();
            return !string.IsNullOrEmpty(selectedFolderPath);
        }

        [MenuItem("Assets/Import Package/Here...", false, 10)]
        private static void ImportPackageHereCommand()
        {
            var packagePath = EditorUtility.OpenFilePanel("Import package ...", "", "unitypackage");
            if (string.IsNullOrEmpty(packagePath)) return;
            if (!File.Exists(packagePath)) return;

            var selectedFolderPath = GetSelectedFolderPath();
            if (string.IsNullOrEmpty(selectedFolderPath))
            {
                EditorUtility.DisplayDialog("Import Package", "Please select a valid folder under 'Assets' in the Project window.", "OK");
                return;
            }
            ImportPackageToFolder(packagePath, selectedFolderPath, true);
        }

        ///////////////////////////////////////////////////////////////
        // Main logic
        ///////////////////////////////////////////////////////////////

        /// <summary>
        /// Allows to import package to the specified folder either via standard import window or silently.
        /// </summary>
        /// <param name="packagePath">Native path to the package.</param>
        /// <param name="selectedFolderPath">Path to the target folder where you wish to import package into.
        /// Relative to the project folder (should start with 'Assets')</param>
        /// <param name="interactive">If true - imports using standard import window, otherwise does this silently.</param>
        public static void ImportPackageToFolder(string packagePath, string selectedFolderPath, bool interactive)
        {
            if (string.IsNullOrEmpty(packagePath) || !File.Exists(packagePath))
                throw new ArgumentException("Invalid package path.", nameof(packagePath));
            if (string.IsNullOrEmpty(selectedFolderPath) || !selectedFolderPath.StartsWith("Assets/"))
                throw new ArgumentException("selectedFolderPath must be a valid path under 'Assets/'.", nameof(selectedFolderPath));

            string packageIconPath;
#if CS_P2F_NEW_ARGUMENT_2
            string packageManagerDependenciesPath;
            object[] assetsItems;
            try
            {
                assetsItems = ExtractAndPrepareAssetList(packagePath, out packageIconPath, out packageManagerDependenciesPath);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to extract asset list: {e.Message}\nFalling back to default ImportPackage dialog.");
                AssetDatabase.ImportPackage(packagePath, true);
                return;
            }
#elif CS_P2F_NEW_ARGUMENT
			bool allowReInstall;
			string packageManagerDependenciesPath;
			object[] assetsItems;
			try
			{
				assetsItems = ExtractAndPrepareAssetList(packagePath, out packageIconPath, out allowReInstall, out packageManagerDependenciesPath);
			}
			catch (Exception e)
			{
				Debug.LogError($"Failed to extract asset list: {e.Message}\nFalling back to default ImportPackage dialog.");
				AssetDatabase.ImportPackage(packagePath, true);
				return;
			}
#else
			bool allowReInstall;
			object[] assetsItems;
			try
			{
				assetsItems = ExtractAndPrepareAssetList(packagePath, out packageIconPath, out allowReInstall);
			}
			catch (Exception e)
			{
				Debug.LogError($"Failed to extract asset list: {e.Message}\nFalling back to default ImportPackage dialog.");
				AssetDatabase.ImportPackage(packagePath, true);
				return;
			}
#endif

            if (assetsItems == null) return;

            foreach (var item in assetsItems)
            {
                ChangeAssetItemPath(item, selectedFolderPath);
            }

            if (interactive)
            {
#if CS_P2F_NEW_ARGUMENT_2
                try
                {
                    ShowImportPackageWindow(packagePath, assetsItems, packageIconPath);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to open import window: {e.Message}\nFalling back to default ImportPackage dialog.");
                    AssetDatabase.ImportPackage(packagePath, true);
                }
#else
				try
				{
					ShowImportPackageWindow(packagePath, assetsItems, packageIconPath, allowReInstall);
				}
				catch (Exception e)
				{
					Debug.LogError($"Failed to open import window: {e.Message}\nFalling back to default ImportPackage dialog.");
					AssetDatabase.ImportPackage(packagePath, true);
				}
#endif
            }
            else
            {
                var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(packagePath);
                ImportPackageSilently(fileNameWithoutExtension, assetsItems);
            }
        }

        private static void ChangeAssetItemPath(object assetItem, string selectedFolderPath)
        {
            var destinationPath = (string)DestinationAssetPathFieldInfo.GetValue(assetItem);
            if (string.IsNullOrEmpty(destinationPath))
            {
                Debug.LogWarning("Package item has empty destination path.");
                return;
            }
            var firstSlashIndex = destinationPath.IndexOf('/');
            if (firstSlashIndex < 0)
            {
                Debug.LogWarning($"Unexpected package item path: {destinationPath}");
                return;
            }
            var basePath = selectedFolderPath.EndsWith("/") ? selectedFolderPath : selectedFolderPath + "/";
            var newPath = basePath + destinationPath.Substring(firstSlashIndex + 1);
            DestinationAssetPathFieldInfo.SetValue(assetItem, newPath);
        }
#if CS_P2F_NEW_ARGUMENT_2
        public static void ShowImportPackageWindow(string path, object[] array, string packageIconPath)
        {
            ShowImportPackageMethodInfo.Invoke(null, new object[]
            {
#if UNITY_2023_1_OR_NEWER
				path, array, packageIconPath, default, default, default, default
#else
                path, array, packageIconPath
#endif
            });
        }
#else
		public static void ShowImportPackageWindow(string path, object[] array, string packageIconPath, bool allowReInstall)
		{
			ShowImportPackageMethodInfo.Invoke(null, new object[] { path, array, packageIconPath, allowReInstall });
		}
#endif

        public static void ImportPackageSilently(string packageName, object[] assetsItems)
        {
#if CS_P2F_NEW_NON_INTERACTIVE_LOGIC
            try
            {
                AssetDatabase.StartAssetEditing();
                EditorUtility.DisplayProgressBar("Importing Package", packageName, 0.5f);
                ImportPackageAssetsMethodInfo.Invoke(null, new object[] { packageName, assetsItems });
            }
            catch (Exception e)
            {
                Debug.LogError($"Silent import failed for '{packageName}': {e}");
                throw;
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                AssetDatabase.StopAssetEditing();
                AssetDatabase.Refresh();
            }
#else
			try
			{
				AssetDatabase.StartAssetEditing();
				EditorUtility.DisplayProgressBar("Importing Package", packageName, 0.5f);
				ImportPackageAssetsMethodInfo.Invoke(null, new object[] { packageName, assetsItems, false });
			}
			catch (Exception e)
			{
				Debug.LogError($"Silent import failed for '{packageName}': {e}");
				throw;
			}
			finally
			{
				EditorUtility.ClearProgressBar();
				AssetDatabase.StopAssetEditing();
				AssetDatabase.Refresh();
			}
#endif
        }

        ///////////////////////////////////////////////////////////////
        // Utility methods
        ///////////////////////////////////////////////////////////////

        private static string GetSelectedFolderPath()
        {
            // Determine path from current selection
            string path = null;
            var activeObject = Selection.activeObject;
            if (activeObject != null)
            {
                path = AssetDatabase.GetAssetPath(activeObject);
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                {
                    path = Path.GetDirectoryName(path).Replace('\\', '/');
                }
            }

            if (string.IsNullOrEmpty(path))
            {
                if (Selection.assetGUIDs == null || Selection.assetGUIDs.Length == 0)
                    return null;

                var assetGuid = Selection.assetGUIDs[0];
                path = AssetDatabase.GUIDToAssetPath(assetGuid);
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                {
                    path = Path.GetDirectoryName(path).Replace('\\', '/');
                }
            }

            if (string.IsNullOrEmpty(path) || !path.StartsWith("Assets/")) return null;
            return !Directory.Exists(path) ? null : path;
        }
    }
}
#endif