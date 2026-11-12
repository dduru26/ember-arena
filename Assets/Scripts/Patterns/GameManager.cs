using UnityEngine;

namespace EmberArena.Patterns
{
    // Singleton: there is only ever one GameManager, and anything can reach it through GameManager.Instance.
    // I use it for the score, the wave number and the game state, so those live in one place.
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public int Score { get; private set; }
        public int Wave { get; private set; }
        public GameState State { get; private set; } = GameState.Playing;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void OnEnable()
        {
            if (Instance != this) return;
            GameEvents.EnemyKilled += AddScore;
            GameEvents.WaveStarted += SetWave;
            GameEvents.PlayerDied += EndGame;
        }

        private void OnDisable()
        {
            GameEvents.EnemyKilled -= AddScore;
            GameEvents.WaveStarted -= SetWave;
            GameEvents.PlayerDied -= EndGame;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void AddScore(int points)
        {
            Score += points;
            Debug.Log($"Score {Score}");
            GameEvents.RaiseScoreChanged(Score);
        }

        private void SetWave(int wave)
        {
            Wave = wave;
            Debug.Log($"Wave {wave} started");
        }

        private void EndGame()
        {
            State = GameState.GameOver;
            Debug.Log($"Game over. Final score {Score}");
        }
    }
}
