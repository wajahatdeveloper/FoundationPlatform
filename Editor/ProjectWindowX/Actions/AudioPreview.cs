using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace ProjectWindowX {
    /// <summary>
    /// Plays AudioClips in the editor via the internal UnityEditor.AudioUtil.
    /// Reflection-guarded: when the API shifts on a Unity upgrade, <see cref="Available"/>
    /// turns false and the menu entries disappear instead of throwing.
    /// </summary>
    internal static class AudioPreview {

        private static readonly MethodInfo play;
        private static readonly MethodInfo stopAll;

        internal static bool Available => play != null && stopAll != null;

        static AudioPreview() {
            try {
                var type = typeof(Editor).Assembly.GetType("UnityEditor.AudioUtil", false);
                if (type == null)
                    return;
                var flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
                play = type.GetMethod("PlayPreviewClip", flags, null, new[] { typeof(AudioClip), typeof(int), typeof(bool) }, null);
                stopAll = type.GetMethod("StopAllPreviewClips", flags, null, Type.EmptyTypes, null);
            } catch {
                play = null;
                stopAll = null;
            }
        }

        internal static void Play(string path) {
            if (!Available)
                return;
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip == null)
                return;
            stopAll.Invoke(null, null);
            play.Invoke(null, new object[] { clip, 0, false });
        }

        internal static void StopAll() {
            if (Available)
                stopAll.Invoke(null, null);
        }
    }
}
