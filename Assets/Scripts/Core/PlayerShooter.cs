using UnityEngine;
using EmberArena.Algorithms;

namespace EmberArena.Core
{
    [RequireComponent(typeof(PlayerController))]
    public class PlayerShooter : MonoBehaviour
    {
        [SerializeField, Tooltip("Bullet prefab to fire.")]
        private Bullet bulletPrefab;

        [SerializeField, Tooltip("Seconds between shots.")]
        private float fireInterval = 0.25f;

        private PlayerController player;
        private Entity currentTarget;
        private float nextFireTime;

        private void Awake()
        {
            player = GetComponent<PlayerController>();
        }

        private void Update()
        {
            if (player.IsDead) return;

            currentTarget = TargetFinder.Nearest(transform.position, TargetRegistry.Targets);

            if (currentTarget == null || Time.time < nextFireTime) return;

            Vector2 direction = (Vector2)currentTarget.transform.position - (Vector2)transform.position;
            Bullet bullet = Instantiate(bulletPrefab, transform.position, Quaternion.identity);
            bullet.Launch(direction);
            nextFireTime = Time.time + fireInterval;
        }

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying || currentTarget == null) return;
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, currentTarget.transform.position);
        }
    }
}
