using System.Collections.Generic;
using UnityEngine;

namespace EmberArena.Core
{
    public static class TargetRegistry
    {
        private static readonly List<Entity> targets = new List<Entity>();

        public static IReadOnlyList<Entity> Targets => targets;

        public static void Register(Entity target)
        {
            if (!targets.Contains(target)) targets.Add(target);
        }

        public static void Unregister(Entity target)
        {
            targets.Remove(target);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Clear()
        {
            targets.Clear();
        }
    }
}
