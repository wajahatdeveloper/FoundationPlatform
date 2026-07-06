#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Linq.Expressions;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FoundationPlatform.Editor.Utilities
{
	public static class AnimationPreviewHelper
	{
		private const BindingFlags PRIVATE_FIELD_BINDING_FLAGS = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.GetField;
		private const BindingFlags PUBLIC_FIELD_BINDING_FLAGS = BindingFlags.Public | BindingFlags.Instance | BindingFlags.GetField;
		private const BindingFlags PUBLIC_PROPERTY_BINDING_FLAGS = BindingFlags.Public | BindingFlags.Instance | BindingFlags.GetProperty;

		private static readonly Type animationClipEditorType = Type.GetType("UnityEditor.AnimationClipEditor,UnityEditor");
		private static readonly Type avatarPreviewType = Type.GetType("UnityEditor.AvatarPreview,UnityEditor");
		private static readonly Type timeControlType = Type.GetType("UnityEditor.TimeControl,UnityEditor");
		private static readonly Type propertyEditorType = Type.GetType("UnityEditor.PropertyEditor, UnityEditor");

		public static Object animationClipEditor;
		public static Object propertyEditor;
		public static MethodInfo closeMethod;
		public static PropertyInfo normalizedTimeProperty;
		public static PropertyInfo playingProperty;
		public static object timeControl;

		private static Func<object, object> _getAvatarPreview;
		private static Func<object, object> _getTimeControl;
		private static Func<object, object> _getPlaying;
		private static Action<object, bool> _setPlaying;
		private static Action<object, float> _setNormalizedTime;

		public static bool repaint;
		private static AnimationClip currentlPreviewClip;

		private static bool wasWindowOpen;

		public static async void PlayPreview(AnimationClip clip)
		{
			propertyEditor = Resources.FindObjectsOfTypeAll(propertyEditorType).FirstOrDefault();
			await Task.Delay(100);
			animationClipEditor = Resources.FindObjectsOfTypeAll(animationClipEditorType).FirstOrDefault();
			SaveCurrentPreviewEditor();
			repaint = true;
			currentlPreviewClip = clip;
		}

		public static void ClosePreview()
		{
			if (propertyEditor != null)
			{
				if (closeMethod == null)
				{
					closeMethod = propertyEditorType.GetMethod("Close", BindingFlags.Public | BindingFlags.Instance);
				}

				if (closeMethod != null && propertyEditor.GetType() == propertyEditorType)
				{
					closeMethod.Invoke(propertyEditor, null);
				}
			}

			repaint = false;
		}

		private static Func<object, object> CompileFieldGetter(FieldInfo fieldInfo)
		{
			if (fieldInfo == null) return null;
			var param = Expression.Parameter(typeof(object), "instance");
			var castInstance = Expression.Convert(param, fieldInfo.DeclaringType);
			var fieldAccess = Expression.Field(castInstance, fieldInfo);
			var castResult = Expression.Convert(fieldAccess, typeof(object));
			return Expression.Lambda<Func<object, object>>(castResult, param).Compile();
		}

		private static Func<object, object> CompilePropertyGetter(PropertyInfo propertyInfo)
		{
			if (propertyInfo == null) return null;
			var param = Expression.Parameter(typeof(object), "instance");
			var castInstance = Expression.Convert(param, propertyInfo.DeclaringType);
			var propertyAccess = Expression.Property(castInstance, propertyInfo);
			var castResult = Expression.Convert(propertyAccess, typeof(object));
			return Expression.Lambda<Func<object, object>>(castResult, param).Compile();
		}

		private static Action<object, bool> CompileBoolPropertySetter(PropertyInfo propertyInfo)
		{
			if (propertyInfo == null) return null;
			var instParam = Expression.Parameter(typeof(object), "instance");
			var valParam = Expression.Parameter(typeof(bool), "value");
			var castInstance = Expression.Convert(instParam, propertyInfo.DeclaringType);
			var propertyAccess = Expression.Property(castInstance, propertyInfo);
			var assign = Expression.Assign(propertyAccess, valParam);
			return Expression.Lambda<Action<object, bool>>(assign, instParam, valParam).Compile();
		}

		private static Action<object, float> CompileFloatPropertySetter(PropertyInfo propertyInfo)
		{
			if (propertyInfo == null) return null;
			var instParam = Expression.Parameter(typeof(object), "instance");
			var valParam = Expression.Parameter(typeof(float), "value");
			var castInstance = Expression.Convert(instParam, propertyInfo.DeclaringType);
			var propertyAccess = Expression.Property(castInstance, propertyInfo);
			var castValue = Expression.Convert(valParam, propertyInfo.PropertyType);
			var assign = Expression.Assign(propertyAccess, castValue);
			return Expression.Lambda<Action<object, float>>(assign, instParam, valParam).Compile();
		}

		public static void RepaintWindow()
		{
			if (animationClipEditor != null)
			{
				if (_getPlaying == null || timeControl == null)
				{
					return;
				}

				var playing = (bool)_getPlaying(timeControl);
				if (playing)
				{
					((UnityEditor.Editor)animationClipEditor).Repaint();
				}
			}
		}

		public static void SetAnimationTime(float time, AnimationClip clip)
		{
			if (clip == currentlPreviewClip && _setNormalizedTime != null && timeControl != null && animationClipEditor != null)
			{
				_setNormalizedTime(timeControl, time);
				((UnityEditor.Editor)animationClipEditor).Repaint();
			}
		}

		private static void SaveCurrentPreviewEditor()
		{
			if (animationClipEditor == null)
			{
				return;
			}

			if (_getAvatarPreview == null)
			{
				var field = animationClipEditorType.GetField("m_AvatarPreview", PRIVATE_FIELD_BINDING_FLAGS);
				_getAvatarPreview = CompileFieldGetter(field);
			}

			var avatarPreview = _getAvatarPreview?.Invoke(animationClipEditor);
			if (avatarPreview == null)
			{
				return;
			}

			if (_getTimeControl == null)
			{
				var field = avatarPreviewType.GetField("timeControl", PUBLIC_FIELD_BINDING_FLAGS);
				_getTimeControl = CompileFieldGetter(field);
			}

			timeControl = _getTimeControl?.Invoke(avatarPreview);

			var editorType = ((UnityEditor.Editor)animationClipEditor).GetType();

			playingProperty = timeControlType.GetProperty("playing", PUBLIC_PROPERTY_BINDING_FLAGS);
			if (playingProperty != null)
			{
				if (_getPlaying == null)
					_getPlaying = CompilePropertyGetter(playingProperty);
				if (_setPlaying == null)
					_setPlaying = CompileBoolPropertySetter(playingProperty);
			}

			if (_setPlaying != null && timeControl != null)
			{
				_setPlaying(timeControl, true);
			}

			normalizedTimeProperty = timeControlType.GetProperty("normalizedTime", PUBLIC_PROPERTY_BINDING_FLAGS);
			if (normalizedTimeProperty != null)
			{
				if (_setNormalizedTime == null)
					_setNormalizedTime = CompileFloatPropertySetter(normalizedTimeProperty);
			}
		}

		[InitializeOnLoadMethod]
		private static void Initialize()
		{
			AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
			EditorApplication.update += Update;
		}

		private static void OnBeforeAssemblyReload()
		{
			// Unsubscribe so the Update handler does not accumulate across domain reloads.
			EditorApplication.update -= Update;
			AssemblyReloadEvents.beforeAssemblyReload -= OnBeforeAssemblyReload;
			ClosePreview();
		}

		private static void Update()
		{
			if (repaint)
			{
				var isWindowOpen = animationClipEditor != null && !animationClipEditor.Equals(null);
				if (wasWindowOpen && !isWindowOpen)
				{
					repaint = false;
				}

				wasWindowOpen = isWindowOpen;
				RepaintWindow();
			}
		}
	}
}
#endif