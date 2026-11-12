using UnityEngine;

namespace EmberArena.Core
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class EnemyBullet : MonoBehaviour
    {
        [SerializeField, Tooltip("Travel speed in world units per second.")]
        private float speed = 6f;

        [SerializeField, Tooltip("Damage dealt to the player on hit.")]
        private int damage = 8;

        [SerializeField, Tooltip("Seconds before the bullet removes itself.")]
        private float lifetime = 4f;

        private Rigidbody2D rb;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
        }

        private void Start()
        {
            Destroy(gameObject, lifetime);
        }

        public void Launch(Vector2 direction)
        {
            rb.linearVelocity = direction.normalized * speed;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponent<Bullet>() != null || other.GetComponent<EnemyBullet>() != null) return;

            IDamageable target = other.GetComponentInParent<IDamageable>();

            if (target is PlayerController)
            {
                target.TakeDamage(damage);
                Destroy(gameObject);
                return;
            }

            if (target != null) return;

            Destroy(gameObject);
        }
    }
}
