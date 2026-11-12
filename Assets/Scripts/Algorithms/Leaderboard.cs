using System.Collections.Generic;
using UnityEngine;

namespace EmberArena.Algorithms
{
    public static class Leaderboard
    {
        private static readonly List<int> scores = new List<int>();

        public static void Add(int score)
        {
            scores.Add(score);
            SortDescending(scores);
        }

        public static List<int> Top(int count)
        {
            return scores.GetRange(0, Mathf.Min(count, scores.Count));
        }

        // Algorithm: insertion sort, highest score first. Worst case is O(n squared), but the list is already sorted except for the newest score, so in practice it is close to O(n).
        private static void SortDescending(List<int> list)
        {
            for (int i = 1; i < list.Count; i++)
            {
                // Step 1: take the next score out of the unsorted part.
                int key = list[i];
                int j = i - 1;

                // Step 2: shift every smaller score one place to the right.
                while (j >= 0 && list[j] < key)
                {
                    list[j + 1] = list[j];
                    j--;
                }

                // Step 3: drop the score into the gap.
                list[j + 1] = key;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Clear()
        {
            scores.Clear();
        }
    }
}
