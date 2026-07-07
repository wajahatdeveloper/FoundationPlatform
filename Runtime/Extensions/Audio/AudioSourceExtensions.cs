using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Essential AudioSource extension methods for Unity development
/// </summary>
public static class AudioSourceExtensions
{
    // Plays a random clip from the list provided on the audio source.
    public static AudioClip PlayRandom(this AudioSource audioSource, AudioClip[] audioClips)
    {
        if (audioClips != null && audioClips.Length > 0)
        {
            int index = UnityEngine.Random.Range(0, audioClips.Length);
            audioSource.clip = audioClips[index];
            audioSource.Play();
            return audioClips[index];
        }

        return null;
    }

    /// <summary>
    /// Plays an audio clip with optional volume and pitch
    /// </summary>
    public static void PlayClip(this AudioSource audioSource, AudioClip clip, float volume = 1f, float pitch = 1f)
    {
        audioSource.clip = clip;
        audioSource.volume = volume;
        audioSource.pitch = pitch;
        audioSource.Play();
    }

    /// <summary>
    /// Fades the volume to a target value over time
    /// </summary>
    public static IEnumerator FadeToVolume(this AudioSource audioSource, float targetVolume, float duration)
    {
        float startVolume = audioSource.volume;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(startVolume, targetVolume, elapsed / duration);
            yield return null;
        }

        audioSource.volume = targetVolume;
    }

    /// <summary>
    /// Sets the volume with clamping
    /// </summary>
    public static void SetVolume(this AudioSource audioSource, float volume)
    {
        audioSource.volume = Mathf.Clamp01(volume);
    }

    /// <summary>
    /// Mutes the audio source
    /// </summary>
    public static void Mute(this AudioSource audioSource)
    {
        audioSource.mute = true;
    }

    /// <summary>
    /// Unmutes the audio source
    /// </summary>
    public static void Unmute(this AudioSource audioSource)
    {
        audioSource.mute = false;
    }

    /// <summary>
    /// Toggles the mute state
    /// </summary>
    public static void ToggleMute(this AudioSource audioSource)
    {
        audioSource.mute = !audioSource.mute;
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
}