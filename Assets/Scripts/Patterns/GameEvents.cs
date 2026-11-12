using System;
using UnityEngine;

namespace EmberArena.Patterns
{
    // Observer: this is the one place where game events are announced.
    // The enemy, the player and the wave spawner only raise events here, and the score, the waves and later the HUD listen, so none of them need a reference to each other.
    public static class GameEvents
    {
        public static event Action<int> EnemyKilled;
        public static event Action<int> WaveStarted;
        public static event Action<int> WaveCleared;
        public static event Action<int> ScoreChanged;
        public static event Action PlayerDied;

        public static void RaiseEnemyKilled(int points)
        {
            EnemyKilled?.Invoke(points);
        }

        public static void RaiseWaveStarted(int wave)
        {
            WaveStarted?.Invoke(wave);
        }

        public static void RaiseWaveCleared(int wave)
        {
            WaveCleared?.Invoke(wave);
        }

        public static void RaiseScoreChanged(int score)
        {
            ScoreChanged?.Invoke(score);
        }

        public static void RaisePlayerDied()
        {
            PlayerDied?.Invoke();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetAll()
        {
            EnemyKilled = null;
            WaveStarted = null;
            WaveCleared = null;
            ScoreChanged = null;
            PlayerDied = null;
        }
    }
}
