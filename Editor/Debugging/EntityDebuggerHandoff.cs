#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace AetherNexus.FoundationPlatform.Editor.Utilities.Debugging
{
	/// <summary>
	/// Hands off from an out-of-context tool (e.g. the Central Authoring Window) to the in-context
	/// <see cref="EntityDebuggerOverlay"/>. The overlay is a transient Scene-View overlay with no
	/// direct open API — it auto-shows purely as a function of the active selection. So the hand-off
	/// is: select the target GameObject and focus a Scene View; the overlay surfaces when a section
	/// applies. Gate the affordance with <see cref="CanReveal"/> so we only offer the jump when it
	/// will actually produce something.
	/// </summary>
	public static class EntityDebuggerHandoff
	{
		/// <summary>True when the overlay would show for this object (at least one section applies).</summary>
		public static bool CanReveal(GameObject go)
			=> go != null && EntityDebugSectionRegistry.HasApplicable(go);

		/// <summary>
		/// Select <paramref name="go"/> and focus a Scene View so the transient overlay surfaces beside
		/// it. Also pings it in the Hierarchy for orientation. No-op on null.
		/// </summary>
		public static void Reveal(GameObject go)
		{
			if (go == null)
				return;

			Selection.activeGameObject = go;
			EditorGUIUtility.PingObject(go);

			SceneView sv = SceneView.lastActiveSceneView;
			if (sv == null && SceneView.sceneViews != null && SceneView.sceneViews.Count > 0)
				sv = SceneView.sceneViews[0] as SceneView;
			if (sv != null)
				sv.Focus();
		}
	}
}
#endif
