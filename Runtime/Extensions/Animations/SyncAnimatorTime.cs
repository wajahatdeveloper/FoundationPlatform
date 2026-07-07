using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SyncAnimatorTime : MonoBehaviour
{
	public enum UpdatePhase
	{
		Update,
		LateUpdate
	}

	public Animator sourceAnimator;
	public int srcLayerIndex;
	public string srcStateName;

	public int layerIndex;
	public string stateName;

	[Tooltip("If true, enters the target state when source matches")] public bool forceEnterTargetState = false;
	[Tooltip("When to sync. Use Update if you need this before IK.")] public UpdatePhase updatePhase = UpdatePhase.Update;

	private Animator animator;
	private int srcStateHash;
	private int dstStateHash;

	private void Start()
	{
		animator = GetComponent<Animator>();
		srcStateHash = Animator.StringToHash(srcStateName);
		dstStateHash = Animator.StringToHash(stateName);
	}

	private void Update()
	{
		if (updatePhase == UpdatePhase.Update)
		{
			Sync();
		}
	}

	private void LateUpdate()
	{
		if (updatePhase == UpdatePhase.LateUpdate)
		{
			Sync();
		}
	}

	private void Sync()
	{
		if (sourceAnimator == null || animator == null)
		{
			return;
		}

		if (srcLayerIndex < 0 || srcLayerIndex >= sourceAnimator.layerCount)
		{
			return;
		}

		if (layerIndex < 0 || layerIndex >= animator.layerCount)
		{
			return;
		}

		var srcInfo = sourceAnimator.GetCurrentAnimatorStateInfo(srcLayerIndex);
		// Compare cached short-name hashes instead of IsName(string) to avoid a managed string hash each frame.
		if (srcInfo.shortNameHash != srcStateHash)
		{
			return;
		}

		var dstInfo = animator.GetCurrentAnimatorStateInfo(layerIndex);
		if (dstInfo.shortNameHash != dstStateHash)
		{
			if (!forceEnterTargetState)
			{
				return;
			}
			animator.Play(stateName, layerIndex, 0f);
		}

		float t = Mathf.Repeat(srcInfo.normalizedTime, 1f);
		animator.Play(stateName, layerIndex, t);
	}
}