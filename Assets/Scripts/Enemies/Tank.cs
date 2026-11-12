using UnityEngine;

namespace EmberArena.Enemies
{
    public class Tank : Enemy
    {
        [SerializeField, Tooltip("Distance at which it starts hitting the player.")]
        private float attackRange = 1.1f;

        [SerializeField, Tooltip("Damage per hit.")]
        private int contactDamage = 18;

        // Polymorphism: the Tank never flees, so it only chooses between Chase and Attack.
        protected override EnemyState Decide(float distanceToPlayer)
        {
            return distanceToPlayer <= attackRange ? EnemyState.Attack : EnemyState.Chase;
        }

        protected override void PerformAttack()
        {
            Player.TakeDamage(contactDamage);
        }
    }
}
