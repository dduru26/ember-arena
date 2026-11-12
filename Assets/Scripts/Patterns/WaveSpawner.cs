using System.Collections;
using UnityEngine;
using EmberArena.Core;
using EmberArena.Enemies;

namespace EmberArena.Patterns
{
    public class WaveSpawner : MonoBehaviour
    {
        [SerializeField] private EnemyFactory factory;
        [SerializeField] private float timeBetweenWaves = 2f;
        [SerializeField] private float timeBetweenSpawns = 0.15f;
        [SerializeField] private int baseEnemies = 2;
        [SerializeField] private int enemiesPerWave = 2;
        [SerializeField] private Vector2 spawnHalfSize = new Vector2(7.5f, 4.5f);

        private void Start()
        {
            StartCoroutine(RunWaves());
        }

        private IEnumerator RunWaves()
        {
            while (GameManager.Instance.State == GameState.Playing)
            {
                yield return new WaitForSeconds(timeBetweenWaves);
                if (GameManager.Instance.State != GameState.Playing) yield break;

                int wave = GameManager.Instance.Wave + 1;
                GameEvents.RaiseWaveStarted(wave);

                int count = baseEnemies + wave * enemiesPerWave;
                for (int i = 0; i < count; i++)
                {
                    factory.Create(PickType(wave), RandomEdgePoint());
                    yield return new WaitForSeconds(timeBetweenSpawns);
                }

                yield return new WaitUntil(() =>
                    TargetRegistry.Targets.Count == 0 || GameManager.Instance.State != GameState.Playing);

                if (GameManager.Instance.State == GameState.Playing) GameEvents.RaiseWaveCleared(wave);
            }
        }

        private EnemyType PickType(int wave)
        {
            float roll = Random.value;
            if (wave >= 3 && roll > 0.8f) return EnemyType.Tank;
            if (wave >= 2 && roll > 0.55f) return EnemyType.Shooter;
            return EnemyType.Chaser;
        }

        private Vector2 RandomEdgePoint()
        {
            switch (Random.Range(0, 4))
            {
                case 0: return new Vector2(Random.Range(-spawnHalfSize.x, spawnHalfSize.x), spawnHalfSize.y);
                case 1: return new Vector2(Random.Range(-spawnHalfSize.x, spawnHalfSize.x), -spawnHalfSize.y);
                case 2: return new Vector2(-spawnHalfSize.x, Random.Range(-spawnHalfSize.y, spawnHalfSize.y));
                default: return new Vector2(spawnHalfSize.x, Random.Range(-spawnHalfSize.y, spawnHalfSize.y));
            }
        }
    }
}
