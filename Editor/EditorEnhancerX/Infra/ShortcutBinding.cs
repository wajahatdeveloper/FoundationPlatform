using System;
using UnityEditor;
using UnityEngine;

namespace AetherNexus.FoundationPlatform.EditorEnhancerX {
    /// <summary>
    /// A user-rebindable keyboard shortcut: key + exact modifier mask + enable flag.
    /// Edited in Project Settings ▸ EditorEnhancerX; matched by <see cref="KeyRouter"/>.
    /// </summary>
    [Serializable]
    public struct ShortcutBinding {
        public bool enabled;
        public KeyCode key;
        public EventModifiers modifiers;

        public ShortcutBinding(bool enabled, KeyCode key, EventModifiers modifiers) {
            this.enabled = enabled;
            this.key = key;
            this.modifiers = modifiers;
        }

        /// <summary>True when the event is a KeyDown exactly matching this binding.</summary>
        public bool Matches(Event e) {
            if (!enabled || e == null || e.type != EventType.KeyDown || e.keyCode != key || key == KeyCode.None)
                return false;
            // FunctionKey is set by Unity for arrows/F-keys/etc. — strip it so users don't have to care.
            var eventMods = e.modifiers & ~(EventModifiers.FunctionKey | EventModifiers.CapsLock | EventModifiers.Numeric);
            var wantMods = modifiers & ~(EventModifiers.FunctionKey | EventModifiers.CapsLock | EventModifiers.Numeric);
            return eventMods == wantMods;
        }

        public override string ToString() {
            if (key == KeyCode.None) return "None";
            var s = "";
            if ((modifiers & EventModifiers.Control) != 0) s += "Ctrl+";
            if ((modifiers & EventModifiers.Command) != 0) s += "Cmd+";
            if ((modifiers & EventModifiers.Shift) != 0) s += "Shift+";
            if ((modifiers & EventModifiers.Alt) != 0) s += "Alt+";
            return s + key;
        }
    }

    /// <summary>Settings-provider field drawer for <see cref="ShortcutBinding"/> (enable + key popup + modifier toggles).</summary>
    public static class ShortcutBindingUI {

        /// <summary>Draws one binding row via SerializedProperty (keeps undo/apply semantics of the provider).</summary>
        public static void Field(SerializedProperty binding, GUIContent label) {
            var enabled = binding.FindPropertyRelative("enabled");
            var key = binding.FindPropertyRelative("key");
            var modifiers = binding.FindPropertyRelative("modifiers");

            using (new EditorGUILayout.HorizontalScope()) {
                enabled.boolValue = EditorGUILayout.ToggleLeft(label, enabled.boolValue, GUILayout.Width(EditorGUIUtility.labelWidth));
                using (new EditorGUI.DisabledScope(!enabled.boolValue)) {
                    var mods = (EventModifiers)modifiers.intValue;
                    mods = ModToggle(mods, EventModifiers.Control, "Ctrl");
                    mods = ModToggle(mods, EventModifiers.Shift, "Shift");
                    mods = ModToggle(mods, EventModifiers.Alt, "Alt");
                    modifiers.intValue = (int)mods;

                    var current = (KeyCode)key.intValue;
                    var next = (KeyCode)EditorGUILayout.EnumPopup(current, GUILayout.MinWidth(90f));
                    if (next != current) key.intValue = (int)next;
                }
            }
        }

        private static EventModifiers ModToggle(EventModifiers mods, EventModifiers flag, string label) {
            var on = (mods & flag) != 0;
            var next = GUILayout.Toggle(on, label, EditorStyles.miniButton, GUILayout.Width(40f));
            if (next == on) return mods;
            return next ? mods | flag : mods & ~flag;
        }
    }
}
