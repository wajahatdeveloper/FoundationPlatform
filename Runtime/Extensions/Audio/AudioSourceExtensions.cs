using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Essential AudioSource extension methods for Unity development
/// </summary>
namespace AetherNexus.FoundationPlatform.Extensions
{
public static class AudioSourceExtensions
{

    /// <summary>
    /// Plays an audio clip with optional volume and pitch
    /// </summary>
    public static void PlayClip(this AudioSource audioSource, AudioClip clip, float volume, float pitch)
    {
        audioSource.clip = clip;
        audioSource.volume = volume;
        audioSource.pitch = pitch;
        audioSource.Play();
    }

    /// <summary>Plays an audio clip at volume 1 and pitch 1.</summary>
    public static void PlayClip(this AudioSource audioSource, AudioClip clip) => PlayClip(audioSource, clip, 1f, 1f);

    /// <summary>
    /// Sets the volume with clamping
    /// </summary>
    public static void SetVolume(this AudioSource audioSource, float volume)
    {
        audioSource.volume = Mathf.Clamp01(volume);
    }

    public static void Reset(this AudioSource @this)
    {
        @this.clip = null;
        @this.mute = false;
        @this.playOnAwake = true;
        @this.loop = false;
        @this.priority = 128;
        @this.volume = 1;
        @this.pitch = 1;
        @this.panStereo = 0;
        @this.spatialBlend = 0;
        @this.reverbZoneMix = 1;
        @this.dopplerLevel = 1;
        @this.spread = 0;
        @this.maxDistance = 500;
    }
}}
