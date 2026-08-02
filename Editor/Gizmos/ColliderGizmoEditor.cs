using UnityEngine;
using UnityEditor;

namespace AetherNexus.FoundationPlatform.Gizmos
{
	[CustomEditor(typeof(ColliderGizmo)), CanEditMultipleObjects]
	public class ColliderGizmoEditor : UnityEditor.Editor
	{
		private SerializedProperty _enabledProperty;
		private SerializedProperty _alphaProperty;
		private SerializedProperty _drawWireProperty;
		private SerializedProperty _wireColorProperty;
		private SerializedProperty _drawFillProperty;
		private SerializedProperty _fillColorProperty;
		private SerializedProperty _drawCenterProperty;
		private SerializedProperty _centerColorProperty;
		private SerializedProperty _centerRadiusProperty;

		private SerializedProperty _includeChilds;

		private ColliderGizmo _target;

		private int _collidersCount;

		private void OnEnable()
		{
			_target = target as ColliderGizmo;

			_enabledProperty = serializedObject.FindProperty("m_Enabled");
			_alphaProperty = serializedObject.FindProperty("Alpha");

			_drawWireProperty = serializedObject.FindProperty("DrawWire");
			_wireColorProperty = serializedObject.FindProperty("WireColor");

			_drawFillProperty = serializedObject.FindProperty("DrawFill");
			_fillColorProperty = serializedObject.FindProperty("FillColor");

			_drawCenterProperty = serializedObject.FindProperty("DrawCenter");
			_centerColorProperty = serializedObject.FindProperty("CenterColor");
			_centerRadiusProperty = serializedObject.FindProperty("CenterMarkerRadius");

			_includeChilds = serializedObject.FindProperty("IncludeChildColliders");

			_collidersCount = CollidersCount();
		}


		public override void OnInspectorGUI()
		{
			Undo.RecordObject(_target, "CG_State");

			EditorGUILayout.PropertyField(_enabledProperty);

			EditorGUI.BeginChangeCheck();
			_target.Preset = (ColliderGizmo.Presets)EditorGUILayout.EnumPopup("Color Preset", _target.Preset);
			if (EditorGUI.EndChangeCheck())
			{
				foreach (var singleTarget in targets)
				{
					var gizmo = (ColliderGizmo)singleTarget;
					gizmo.ChangePreset(_target.Preset);
					EditorUtility.SetDirty(gizmo);
				}
			}

			_alphaProperty.floatValue = EditorGUILayout.Slider("Overall Transparency", _alphaProperty.floatValue, 0, 1);


			EditorGUI.BeginChangeCheck();
			using (new EditorGUILayout.HorizontalScope())
			{
				EditorGUILayout.PropertyField(_drawWireProperty);
				if (_drawWireProperty.boolValue) EditorGUILayout.PropertyField(_wireColorProperty, new GUIContent(""));
			}

			using (new EditorGUILayout.HorizontalScope())
			{
				EditorGUILayout.PropertyField(_drawFillProperty);
				if (_drawFillProperty.boolValue) EditorGUILayout.PropertyField(_fillColorProperty, new GUIContent(""));
			}

			using (new EditorGUILayout.HorizontalScope())
			{
				EditorGUILayout.PropertyField(_drawCenterProperty);
				if (_drawCenterProperty.boolValue)
				{
					EditorGUILayout.PropertyField(_centerColorProperty, GUIContent.none);
					EditorGUILayout.PropertyField(_centerRadiusProperty);
				}
			}


			if (EditorGUI.EndChangeCheck())
			{
				var presetProp = serializedObject.FindProperty("Preset");
				var customWireColor = serializedObject.FindProperty("CustomWireColor");
				var customFillColor = serializedObject.FindProperty("CustomFillColor");
				var customCenterColor = serializedObject.FindProperty("CustomCenterColor");

				presetProp.enumValueIndex = (int)ColliderGizmo.Presets.Custom;
				customWireColor.colorValue = _wireColorProperty.colorValue;
				customFillColor.colorValue = _fillColorProperty.colorValue;
				customCenterColor.colorValue = _centerColorProperty.colorValue;
			}

			EditorGUILayout.PropertyField(_includeChilds);

			int collidersCountCheck = CollidersCount();
			bool collidersCountChanged = collidersCountCheck != _collidersCount;
			_collidersCount = collidersCountCheck;

			if (GUI.changed || collidersCountChanged)
			{
				serializedObject.ApplyModifiedProperties();
				EditorUtility.SetDirty(_target);

				_target.Refresh();
			}
		}

		private int CollidersCount()
		{
			if (_includeChilds.boolValue)
			{
				return _target.gameObject.GetComponentsInChildren<Collider>().Length +
				       _target.gameObject.GetComponentsInChildren<Collider2D>().Length;
			}

			return _target.gameObject.GetComponents<Collider>().Length +
			       _target.gameObject.GetComponents<Collider2D>().Length;
		}
	}
}
