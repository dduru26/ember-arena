using System.Collections.Generic;
using UnityEngine;
using EmberArena.Core;

namespace EmberArena.Algorithms
{
    public static class TargetFinder
    {
        public static int LastScanned { get; private set; }

        // Algorithm: nearest target search. It is a linear scan, so the time is O(n) and the extra space is O(1).
        public static Entity Nearest(Vector2 origin, IReadOnlyList<Entity> candidates)
        {
            Entity best = null;
            float bestSqrDistance = float.MaxValue;
            LastScanned = 0;

            for (int i = 0; i < candidates.Count; i++)
            {
                Entity candidate = candidates[i];

                // Step 1: skip anything that is gone or already dead.
                if (candidate == null || candidate.IsDead) continue;

                LastScanned++;

                // Step 2: compare squared distances, so I never pay for a square root.
                float sqrDistance = ((Vector2)candidate.transform.position - origin).sqrMagnitude;

                // Step 3: keep the closest one seen so far.
                if (sqrDistance < bestSqrDistance)
                {
                    bestSqrDistance = sqrDistance;
                    best = candidate;
                }
            }

            return best;
        }
    }
}
