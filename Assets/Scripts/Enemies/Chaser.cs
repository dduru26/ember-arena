using UnityEngine;

namespace EmberArena.Enemies
{
    public class Chaser : Enemy
    {
        [SerializeField, Tooltip("Distance at which it starts hitting the player.")]
        private float attackRange = 0.8f;

        [SerializeField, Tooltip("Damage per hit.")]
        private int contactDamage = 8;

        [SerializeField, Range(0f, 1f), Tooltip("Flees when health drops below this fraction.")]
        private float fleeHealthFraction = 0.3f;

        // Polymorphism: the Chaser has its own rules for picking a state, and it runs away when it is nearly dead.
        protected override EnemyState Decide(float distanceToPlayer)
        {
            if (CurrentHealth < MaxHealth * fleeHealthFraction) return EnemyState.Flee;
            if (distanceToPlayer <= attackRange) return EnemyState.Attack;
            return EnemyState.Chase;
        }

        protected override void PerformAttack()
        {
            Player.TakeDamage(contactDamage);
        }
    }
}
