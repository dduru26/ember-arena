using System;
using UnityEngine;
using EmberArena.Enemies;

namespace EmberArena.Patterns
{
    // Factory: the spawner just asks for an enemy type and a position, and this class decides which prefab to build.
    // If I add a new enemy later, only this class changes and the spawner stays the same.
    public class EnemyFactory : MonoBehaviour
    {
        [SerializeField] private Enemy chaserPrefab;
        [SerializeField] private Enemy shooterPrefab;
        [SerializeField] private Enemy tankPrefab;

        public Enemy Create(EnemyType type, Vector2 position)
        {
            Enemy prefab = type switch
            {
                EnemyType.Chaser => chaserPrefab,
                EnemyType.Shooter => shooterPrefab,
                EnemyType.Tank => tankPrefab,
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };

            return Instantiate(prefab, position, Quaternion.identity);
        }
    }
}
