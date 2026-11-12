using UnityEngine;
using EmberArena.Core;

namespace EmberArena.Enemies
{
    public class Shooter : Enemy
    {
        [SerializeField, Tooltip("Enemy bullet prefab.")]
        private EnemyBullet bulletPrefab;

        [SerializeField, Tooltip("Closer than this and it backs away.")]
        private float fleeRange = 3f;

        [SerializeField, Tooltip("Within this distance it stops and shoots.")]
        private float shootRange = 6f;

        // Polymorphism: the Shooter keeps its distance, so its states depend on two ranges instead of one.
        protected override EnemyState Decide(float distanceToPlayer)
        {
            if (distanceToPlayer < fleeRange) return EnemyState.Flee;
            if (distanceToPlayer <= shootRange) return EnemyState.Attack;
            return EnemyState.Chase;
        }

        protected override void PerformAttack()
        {
            Vector2 direction = (Vector2)Player.transform.position - (Vector2)transform.position;
            EnemyBullet bullet = Instantiate(bulletPrefab, transform.position, Quaternion.identity);
            bullet.Launch(direction);
        }
    }
}
