using UnityEngine;

namespace EmberArena.Core
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class Bullet : MonoBehaviour
    {
        [SerializeField, Tooltip("Travel speed in world units per second.")]
        private float speed = 12f;

        [SerializeField, Tooltip("Damage dealt on hit.")]
        private int damage = 10;

        [SerializeField, Tooltip("Seconds before the bullet removes itself.")]
        private float lifetime = 2f;

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
            if (other.GetComponentInParent<PlayerController>() != null) return;
            if (other.GetComponent<EnemyBullet>() != null) return;

            // Abstraction: the bullet only knows IDamageable, so it works on any enemy type without knowing which one it hit.
            IDamageable target = other.GetComponentInParent<IDamageable>();
            if (target != null) target.TakeDamage(damage);

            Destroy(gameObject);
        }
    }
}
