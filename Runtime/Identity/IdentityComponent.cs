using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// MonoBehaviour that holds an Identity (string-only) and implements IIdentity.
/// Use AssignIdentity(id) for runtime; GenerateDesignTimeId() for editor/prefabs.
/// </summary>
namespace AetherNexus.FoundationPlatform.Identity
{
using AetherNexus.FoundationPlatform.Messaging;
	
[DisallowMultipleComponent]
public class IdentityComponent : MonoBehaviour, IIdentity
{
	[SerializeField] private string _id;

	/// <summary>Runtime Identity from serialized string.</summary>
	public Identity Identity => new Identity(_id);

	private void Start()
	{
		if (!Identity.IsValid)
		{
			Debug.LogError($"[IdentityComponent] Missing identity on '{gameObject.name}' ({GetPath(this)}). Ensure a valid identity is assigned.", this);
		}
	}

	private static string GetPath(IdentityComponent c)
	{
		if (c == null || c.transform == null) return "?";
		var t = c.transform;
		var parts = new List<string>();
		while (t != null) { parts.Add(t.name); t = t.parent; }
		parts.Reverse();
		return string.Join("/", parts);
	}

	/// <summary>Assign identity at runtime. Use for programmatic setup.</summary>
	public void AssignIdentity(string id)
	{
		_id = id;

		#if UNITY_EDITOR
		UnityEditor.EditorUtility.SetDirty(this);
		#endif
	}

	/// <summary>Generate a stable design-time ID for prefabs/scene objects (editor only).</summary>
	[ContextMenu(nameof(GenerateDesignTimeId))]
	public void GenerateDesignTimeId()
	{
		_id = NewDesignTimeId();
		#if UNITY_EDITOR
		UnityEditor.EditorUtility.SetDirty(this);
		#endif
	}

	/// <summary>
	/// Shared design-time ID format, also used by IdentityFieldDrawer's "New" button so the
	/// convention only needs to change in one place.
	/// </summary>
	public static string NewDesignTimeId() => $"e:{Guid.NewGuid():N}";

	/// <summary>Clear identity. Safe to call from editor or runtime.</summary>
	[ContextMenu(nameof(ClearIdentity))]
	public void ClearIdentity()
	{
		_id = null;
		#if UNITY_EDITOR
		UnityEditor.EditorUtility.SetDirty(this);
		#endif
	}
}
}
