#pragma warning disable 618
#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace AetherNexus.FoundationPlatform.Editor.Utilities
{
public static class EditorX
{
	/// <summary>
        /// Gets all children of `SerializedProperty` at 1 level depth.
        /// </summary>
        /// <param name="serializedProperty">Parent `SerializedProperty`.</param>
        /// <returns>Collection of `SerializedProperty` children.</returns>
        public static IEnumerable<SerializedProperty> GetChildren(this SerializedProperty serializedProperty)
        {
            SerializedProperty currentProperty = serializedProperty.Copy();
            SerializedProperty nextSiblingProperty = serializedProperty.Copy();
            {
                nextSiblingProperty.Next(false);
            }

            if (currentProperty.Next(true))
            {
                do
                {
                    if (SerializedProperty.EqualContents(currentProperty, nextSiblingProperty))
                        break;

                    yield return currentProperty;
                }
                while (currentProperty.Next(false));
            }
        }

        /// <summary>
        /// Gets visible children of `SerializedProperty` at 1 level depth.
        /// </summary>
        /// <param name="serializedProperty">Parent `SerializedProperty`.</param>
        /// <returns>Collection of `SerializedProperty` children.</returns>
        public static IEnumerable<SerializedProperty> GetVisibleChildren(this SerializedProperty serializedProperty)
        {
            SerializedProperty currentProperty = serializedProperty.Copy();
            SerializedProperty nextSiblingProperty = serializedProperty.Copy();
            {
                nextSiblingProperty.NextVisible(false);
            }

            if (currentProperty.NextVisible(true))
            {
                do
                {
                    if (SerializedProperty.EqualContents(currentProperty, nextSiblingProperty))
                        break;

                    yield return currentProperty;
                }
                while (currentProperty.NextVisible(false));
            }
        }

	/// <summary>
		/// Draw line with arrows showing direction
		/// </summary>
		public static void DrawDirectionalLine(Vector3 fromPos, Vector3 toPos, float screenSpaceSize, float arrowsDensity)
		{
			var arrowSize = screenSpaceSize / 4;

			Handles.DrawLine(fromPos, toPos);

			var direction = toPos - fromPos;
			var distance = Vector3.Distance(fromPos, toPos);
			var arrowsCount = (int) (distance / arrowsDensity);
			var delta = 1f / arrowsCount;
			for (int i = 1; i <= arrowsCount; i++)
			{
				var currentDelta = delta * i;
				var currentPosition = Vector3.Lerp(fromPos, toPos, currentDelta);
				DrawTinyArrow(currentPosition, direction, arrowSize);
			}
		}

		/// <summary>Draws directional lines with a 0.5 arrows density.</summary>
		public static void DrawDirectionalLine(Vector3 fromPos, Vector3 toPos, float screenSpaceSize) => DrawDirectionalLine(fromPos, toPos, screenSpaceSize, .5f);

		/// <summary>
		/// Draw arrow in position to direction
		/// </summary>
		public static void DrawTinyArrow(Vector3 position, Vector3 direction, float headLength, float headAngle)
		{
			var lookRotation = Quaternion.LookRotation(direction);
			var rightVector = new Vector3(0, 0, 1);
			Vector3 right = lookRotation * Quaternion.Euler(0, 180 + headAngle, 0) * rightVector;
			Vector3 left = lookRotation * Quaternion.Euler(0, 180 - headAngle, 0) * rightVector;
			Handles.DrawLine(position, position + right * headLength);
			Handles.DrawLine(position, position + left * headLength);
		}

		/// <summary>Draws a tiny arrow with a 20 degree head angle.</summary>
		public static void DrawTinyArrow(Vector3 position, Vector3 direction, float headLength) => DrawTinyArrow(position, direction, headLength, 20.0f);


		/// <summary>
		/// Draw arrowed gizmo in scene view to visualize path
		/// </summary>
		public static void VisualizePath(NavMeshPath path)
		{
			var corners = path.corners;
			for (var i = 1; i < corners.Length; i++)
			{
				var cornerA = corners[i - 1];
				var cornerB = corners[i];
				var size = HandleUtility.GetHandleSize(new Vector3(cornerA.x, cornerA.y, cornerA.z));
				DrawDirectionalLine(cornerA, cornerB, size, size);
			}
		}

		/// <summary>
		/// Draw flying path of height prom pointA to pointB
		/// </summary>
		public static void DrawFlyPath(Vector3 pointA, Vector3 pointB, float height)
		{
			var color = Handles.color;
			var pointAOffset = new Vector3(pointA.x, pointA.y + height, pointA.z);
			var pointBOffset = new Vector3(pointB.x, pointB.y + height, pointB.z);
			Handles.DrawBezier(pointA, pointB, pointAOffset, pointBOffset, color, null, 3);
		}

		/// <summary>Draws a flying path with a height of 3.</summary>
		public static void DrawFlyPath(Vector3 pointA, Vector3 pointB) => DrawFlyPath(pointA, pointB, 3);
	
    	#region Hierarchy Management

		/// <summary>
		/// Fold/Unfold GameObject hierarchy
		/// </summary>
		public static void FoldInHierarchy(GameObject go, bool expand)
		{
			if (go == null) return;
			var type = typeof(EditorWindow).Assembly.GetType("UnityEditor.SceneHierarchyWindow");
			var methodInfo = type.GetMethod("SetExpandedRecursive");
			if (methodInfo == null) return;

			EditorApplication.ExecuteMenuItem("Window/General/Hierarchy");
			var window = EditorWindow.focusedWindow;

			methodInfo.Invoke(window, new object[] { go.GetInstanceID(), expand });
		}

		/// <summary>
		/// Fold objects hierarchy for all opened scenes
		/// </summary>
		public static void FoldSceneHierarchy()
		{
			for (var i = 0; i < SceneManager.sceneCount; i++)
			{
				var scene = SceneManager.GetSceneAt(i);
				if (!scene.isLoaded) continue;
				var roots = SceneManager.GetSceneAt(i).GetRootGameObjects();
				for (var o = 0; o < roots.Length; o++)
				{
					FoldInHierarchy(roots[o], false);
				}
			}
		}

		#endregion


		#region GameObject Rename Mode

		/// <summary>
		/// Set currently selected object to Rename Mode
		/// </summary>
		public static void InitiateObjectRename(GameObject objectToRename)
		{
			EditorApplication.update += ObjectRename;
			_renameTimestamp = EditorApplication.timeSinceStartup + 0.4d;
			EditorApplication.ExecuteMenuItem("Window/Hierarchy");
			Selection.activeGameObject = objectToRename;
		}

		private static void ObjectRename()
		{
			if (EditorApplication.timeSinceStartup >= _renameTimestamp)
			{
				EditorApplication.update -= ObjectRename;
				var type = typeof(EditorWindow).Assembly.GetType("UnityEditor.SceneHierarchyWindow");
				var hierarchyWindow = EditorWindow.GetWindow(type);
				var renameMethod = type.GetMethod("RenameGO", BindingFlags.Instance | BindingFlags.NonPublic);
				if (renameMethod == null)
				{
					Debug.LogError("RenameGO method is obsolete?");
					return;
				}

				renameMethod.Invoke(hierarchyWindow, null);
			}
		}

		private static double _renameTimestamp;

		#endregion


		#region Prefab Management

		/// <summary>
		/// Apply changes on GameObject to prefab
		/// </summary>
		public static void ApplyPrefab(GameObject instance)
		{
			var instanceRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(instance);

			if (instanceRoot == null)
			{
				Debug.LogError("ApplyPrefab failed. Target object " + instance.name + " is not a prefab instance");
				return;
			}

			var prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(instanceRoot);
			if (string.IsNullOrEmpty(prefabPath))
			{
				Debug.LogError("ApplyPrefab failed. Could not find prefab asset path for " + instance.name);
				return;
			}

			PrefabUtility.SaveAsPrefabAsset(instanceRoot, prefabPath);
		}

		/// <summary>
		/// Get Prefab path in Asset Database
		/// </summary>
		/// <returns>Null if not a prefab</returns>
		public static string GetPrefabPath(GameObject gameObject, bool withAssetName)
		{
			if (gameObject == null) return null;

			Object prefabParent = PrefabUtility.GetCorrespondingObjectFromSource(gameObject);
			if (prefabParent == null) return null;
			var assetPath = AssetDatabase.GetAssetPath(prefabParent);

			return !withAssetName ? Path.GetDirectoryName(assetPath) : assetPath;
		}

		/// <summary>Gets the prefab path including the asset name.</summary>
		public static string GetPrefabPath(GameObject gameObject) => GetPrefabPath(gameObject, true);

		public static bool IsPrefabInstance(this GameObject go)
		{
			return PrefabUtility.IsPartOfPrefabInstance(go);
		}

		#endregion


		#region Set Editor Icon

		/// <summary>
		/// Set Editor Icon (the one that appear in SceneView)
		/// </summary>
		public static void SetEditorIcon(this GameObject gameObject, bool textIcon, int iconIndex)
		{
			GUIContent[] icons = textIcon ? GetTextures("sv_label_", string.Empty, 0, 8) : GetTextures("sv_icon_dot", "_pix16_gizmo", 0, 16);

			var egu = typeof(EditorGUIUtility);
			var flags = BindingFlags.InvokeMethod | BindingFlags.Static | BindingFlags.NonPublic;
			var args = new object[] { gameObject, icons[iconIndex].image };
			var setIconMethod = egu.GetMethod("SetIconForObject", flags, null, new[] { typeof(Object), typeof(Texture2D) }, null);
			if (setIconMethod != null) setIconMethod.Invoke(null, args);
		}

		private static GUIContent[] GetTextures(string baseName, string postFix, int startIndex, int count)
		{
			GUIContent[] array = new GUIContent[count];
			for (int i = 0; i < count; i++)
			{
				array[i] = EditorGUIUtility.IconContent(baseName + (startIndex + i) + postFix);
			}

			return array;
		}

		#endregion


		#region Get Fields With Attribute

		/// <summary>
		/// Get all fields with specified attribute on all Unity Objects
		/// </summary>
		public static List<ObjectField> GetFieldsWithAttributeFromScenes<T>() where T : Attribute
		{
			var allObjects = GetAllBehavioursInScenes();

			// ReSharper disable once CoVariantArrayConversion
			return GetFieldsWithAttribute<T>(allObjects);
		}
		
		/// <summary>
		/// Get all fields with specified attribute on all Unity Objects
		/// </summary>
		public static List<ObjectField> GetFieldsWithAttributeFromAll<T>() where T : Attribute
		{
			var allObjects = GetAllUnityObjects();

			return GetFieldsWithAttribute<T>(allObjects);
		}
		
		/// <summary>
		/// Get all fields with specified attribute from Prefab Root GO
		/// </summary>
		public static List<ObjectField> GetFieldsWithAttribute<T>(GameObject root) where T : Attribute
		{
			var allObjects = root.GetComponentsInChildren<MonoBehaviour>();

			// ReSharper disable once CoVariantArrayConversion
			return GetFieldsWithAttribute<T>(allObjects);
		}

		/// <summary>
		/// Get all fields with specified attribute from set of Unity Objects
		/// </summary>
		public static List<ObjectField> GetFieldsWithAttribute<T>(Object[] objects) where T : Attribute
		{
			var desiredAttribute = typeof(T);
			var result = new List<ObjectField>();
			foreach (var o in objects)
			{
				if (o == null) continue;
				var fields = o.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
				foreach (var field in fields)
				{
					if (!field.IsDefined(desiredAttribute, false)) continue;
					result.Add(new ObjectField(field, o));
				}
			}

			return result;
		}

		/// <summary>
		/// Get all Components in the same scene as a specified GameObject,
		/// including inactive components.
		/// </summary>
		public static IEnumerable<Component> GetAllComponentsInSceneOf(Object obj,
			Type type)
		{
			GameObject contextGO;
			if (obj is Component comp) contextGO = comp.gameObject;
			else if (obj is GameObject go) contextGO = go;
			else return Array.Empty<Component>();
			if (contextGO.scene.isLoaded) return contextGO.scene.GetRootGameObjects()
				.SelectMany(rgo => rgo.GetComponentsInChildren(type, true));
			return Array.Empty<Component>();
		}

		public struct ObjectField
		{
			public readonly FieldInfo Field;
			public readonly Object Context;

			public ObjectField(FieldInfo field, Object context)
			{
				Field = field;
				Context = context;
			}
		}

		/// <summary>
		/// Get every assets possible, including lazily-loaded assets.
		/// </summary>
		public static Object[] GetAllUnityObjects()
		{
			LoadAllAssetsOfType(typeof(ScriptableObject));
			LoadAllAssetsOfType("Prefab");
			return Resources.FindObjectsOfTypeAll(typeof(Object));
		}
		
		/// <summary>
		/// It's like FindObjectsOfType, but allows to get disabled objects
		/// </summary>
		public static MonoBehaviour[] GetAllBehavioursInScenes()
		{
			var components = new List<MonoBehaviour>();

			for (var i = 0; i < SceneManager.sceneCount; i++)
			{
				var scene = SceneManager.GetSceneAt(i);
				if (!scene.isLoaded) continue;
				
				var root = scene.GetRootGameObjects();
				foreach (var gameObject in root)
				{
					var behaviours = gameObject.GetComponentsInChildren<MonoBehaviour>(true);
					foreach (var behaviour in behaviours) components.Add(behaviour);
				}
			}

			return components.ToArray();
		}

		#endregion


		#region Get Script Asseet Path

		/// <summary>
		/// Get relative to Assets folder path to script file location
		/// </summary>
		public static string GetRelativeScriptAssetsPath(ScriptableObject so)
		{
			MonoScript ms = MonoScript.FromScriptableObject(so);
			return AssetDatabase.GetAssetPath(ms);
		}

		/// <summary>
		/// Get full path to script file location
		/// </summary>
		public static string GetScriptAssetPath(ScriptableObject so)
		{
			var assetsPath = GetRelativeScriptAssetsPath(so);
			return new FileInfo(assetsPath).DirectoryName;
		}

		/// <summary>
		/// Get relative to Assets folder path to script file location
		/// </summary>
		public static string GetRelativeScriptAssetsPath(MonoBehaviour mb)
		{
			MonoScript ms = MonoScript.FromMonoBehaviour(mb);
			return AssetDatabase.GetAssetPath(ms);
		}

		/// <summary>
		/// Get full path to script file location
		/// </summary>
		public static string GetScriptAssetPath(MonoBehaviour mb)
		{
			var assetsPath = GetRelativeScriptAssetsPath(mb);
			return new FileInfo(assetsPath).DirectoryName;
		}

		#endregion

		/// <summary>
		/// Force Unity Editor to load lazily-loaded types such as ScriptableObject.
		/// </summary>
		public static void LoadAllAssetsOfType(Type type)
		{
			foreach (var p in AssetDatabase.FindAssets($"t:{type.FullName}"))
				AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(p), type);
		}

		/// <summary>
		/// Force Unity Editor to load lazily-loaded types such as ScriptableObject.
		/// </summary>
		public static void LoadAllAssetsOfType(string typeName)
		{
			foreach (var p in AssetDatabase.FindAssets($"t:{typeName}"))
				AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(p), typeof(UnityEngine.Object));
		}

		public static void CopyToClipboard(string text)
		{
			TextEditor te = new TextEditor();
			te.text = text;
			te.SelectAll();
			te.Copy();
		}
}
}
#endif
#pragma warning restore 618
