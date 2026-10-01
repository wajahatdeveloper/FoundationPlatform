using System;
using UnityEngine.LowLevel;

namespace AetherNexus.FoundationPlatform
{
    /// <summary>
    /// The one way project code edits the global player loop. Each insert first strips every entry
    /// carrying the same marker type: with domain reload disabled the previous Play session's entry is
    /// still in the native loop, and re-inserting without removal runs the update once more per session.
    /// </summary>
    public static class PlayerLoopInstaller
    {
        public static void InsertBefore(Type marker, Type anchor, PlayerLoopSystem.UpdateFunction update)
        {
            Install(marker, anchor, update, 0);
        }

        public static void InsertAfter(Type marker, Type anchor, PlayerLoopSystem.UpdateFunction update)
        {
            Install(marker, anchor, update, 1);
        }

        private static void Install(Type marker, Type anchor, PlayerLoopSystem.UpdateFunction update, int offset)
        {
            var root = PlayerLoop.GetCurrentPlayerLoop();
            Remove(ref root, marker);

            var system = new PlayerLoopSystem { type = marker, updateDelegate = update };
            if (!TryInsert(ref root, anchor, system, offset))
            {
                throw new InvalidOperationException(
                    $"[FoundationPlatform] PlayerLoopInstaller could not find '{anchor.FullName}' in the player loop " +
                    $"while installing '{marker.FullName}'.");
            }

            PlayerLoop.SetPlayerLoop(root);
        }

        // FullName as well as identity: after a script reload a surviving entry's Type is not the reloaded one.
        private static bool IsMarker(Type candidate, Type marker)
        {
            return candidate != null && (candidate == marker || candidate.FullName == marker.FullName);
        }

        private static void Remove(ref PlayerLoopSystem system, Type marker)
        {
            var list = system.subSystemList;
            if (list == null)
            {
                return;
            }

            int kept = 0;
            for (int i = 0; i < list.Length; i++)
            {
                if (!IsMarker(list[i].type, marker))
                {
                    kept++;
                }
            }

            if (kept != list.Length)
            {
                var newList = new PlayerLoopSystem[kept];
                int write = 0;
                for (int i = 0; i < list.Length; i++)
                {
                    if (!IsMarker(list[i].type, marker))
                    {
                        newList[write++] = list[i];
                    }
                }

                system.subSystemList = newList;
                list = newList;
            }

            for (int i = 0; i < list.Length; i++)
            {
                Remove(ref list[i], marker);
            }
        }

        private static bool TryInsert(ref PlayerLoopSystem system, Type anchor, PlayerLoopSystem newSystem, int offset)
        {
            var list = system.subSystemList;
            if (list == null)
            {
                return false;
            }

            for (int i = 0; i < list.Length; i++)
            {
                if (list[i].type == anchor)
                {
                    int at = i + offset;
                    var newList = new PlayerLoopSystem[list.Length + 1];
                    Array.Copy(list, 0, newList, 0, at);
                    newList[at] = newSystem;
                    Array.Copy(list, at, newList, at + 1, list.Length - at);
                    system.subSystemList = newList;
                    return true;
                }

                if (TryInsert(ref list[i], anchor, newSystem, offset))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
