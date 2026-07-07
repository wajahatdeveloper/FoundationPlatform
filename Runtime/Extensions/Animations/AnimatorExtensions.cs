using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Essential Animator extension methods for Unity development
/// </summary>
public static class AnimatorExtensions
{
    /// <summary>
    /// 获取动画组件切换进度
    /// </summary>
    public static float GetCrossFadeProgress(this Animator @this, int layer = 0)
    {
        if (@this.GetNextAnimatorStateInfo(layer).shortNameHash == 0)
        {
            return 1;
        }

        return @this.GetCurrentAnimatorStateInfo(layer).normalizedTime % 1;
    }

    public static bool HasParameter(this Animator animator, string name)
    {
        var allParameters = animator.parameters;
        foreach (var param in allParameters)
        {
            if (param.name == name) return true;
        }

        return false;
    }

    public static bool HasParameter(this Animator animator, int nameHash)
    {
        var allParameters = animator.parameters;
        foreach (var param in allParameters)
        {
            if (param.nameHash == nameHash) return true;
        }

        return false;
    }

    public static bool IsInState(this Animator animator, string stateName) =>
        IsInState(animator, 0, stateName);

    public static bool IsInState(this Animator animator, int stateHash) =>
        IsInState(animator, 0, stateHash);

    public static bool IsInState(this Animator animator, int layerIndex, string stateName) =>
        animator.GetCurrentAnimatorStateInfo(layerIndex).IsName(stateName);

    public static bool IsInState(this Animator animator, int layerIndex, int stateHash) =>
        animator.GetCurrentAnimatorStateInfo(layerIndex).fullPathHash == stateHash;

    #region Speed Control

    /// <summary>
    /// Sets the animator speed
    /// </summary>
    public static void SetSpeed(this Animator animator, float speed)
    {
        animator.speed = speed;
    }

    /// <summary>
    /// Pauses the animator
    /// </summary>
    public static void Pause(this Animator animator)
    {
        animator.speed = 0f;
    }

    /// <summary>
    /// Resumes the animator
    /// </summary>
    public static void Resume(this Animator animator)
    {
        animator.speed = 1f;
    }

    /// <summary>
    /// Toggles the animator pause state
    /// </summary>
    public static void TogglePause(this Animator animator)
    {
        animator.speed = animator.speed > 0f ? 0f : 1f;
    }

    #endregion

    #region Animation Events

    /// <summary>
    /// Waits for the current state to finish
    /// </summary>
    public static IEnumerator WaitForCurrentStateToFinish(this Animator animator, int layerIndex = 0)
    {
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(layerIndex);
        // For looping states normalizedTime exceeds 1; take only the fractional remainder so the
        // remaining-time computation stays non-negative (a negative WaitForSeconds returns instantly).
        float fraction = stateInfo.loop ? Mathf.Repeat(stateInfo.normalizedTime, 1f) : Mathf.Clamp01(stateInfo.normalizedTime);
        yield return new WaitForSeconds(stateInfo.length * (1f - fraction));
    }

    /// <summary>
    /// Waits for a specific state to finish. Gives up after <paramref name="timeoutSeconds"/>
    /// (real time) if the state never becomes current, so a mistyped/skipped state cannot hang the coroutine forever.
    /// </summary>
    public static IEnumerator WaitForStateToFinish(this Animator animator, string stateName, int layerIndex = 0, float timeoutSeconds = 5f)
    {
        int stateHash = Animator.StringToHash(stateName);
        float deadline = Time.realtimeSinceStartup + timeoutSeconds;
        while (animator.GetCurrentAnimatorStateInfo(layerIndex).shortNameHash != stateHash)
        {
            if (Time.realtimeSinceStartup >= deadline)
                yield break;
            yield return null;
        }
        yield return animator.WaitForCurrentStateToFinish(layerIndex);
    }

    #endregion
}