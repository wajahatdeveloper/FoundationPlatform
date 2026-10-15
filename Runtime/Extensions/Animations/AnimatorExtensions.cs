using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Essential Animator extension methods for Unity development
/// </summary>
namespace AetherNexus.FoundationPlatform.Extensions
{
public static class AnimatorExtensions
{

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

    #endregion
}}
