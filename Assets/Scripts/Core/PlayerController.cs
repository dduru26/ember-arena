using UnityEngine;
using EmberArena.Patterns;

namespace EmberArena.Core
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : Entity
    {
        [SerializeField, Tooltip("Movement speed in world units per second.")]
        private float moveSpeed = 6.5f;

        [SerializeField, Tooltip("Seconds of immunity after being hit.")]
        private float invulnerabilityTime = 0.4f;

        [SerializeField, Tooltip("Health restored when a wave is cleared.")]
        private int waveClearHeal = 15;

        private Rigidbody2D rb;
        private Vector2 input;
        private float invulnerableUntil;

        protected override void Awake()
        {
            base.Awake();
            rb = GetComponent<Rigidbody2D>();
        }

        private void OnEnable()
        {
            HealthChanged += LogHealth;
            GameEvents.WaveCleared += HealBetweenWaves;
        }

        private void OnDisable()
        {
            HealthChanged -= LogHealth;
            GameEvents.WaveCleared -= HealBetweenWaves;
        }

        private void Update()
        {
            if (IsDead)
            {
                input = Vector2.zero;
                return;
            }
            input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized;
        }

        private void FixedUpdate()
        {
            rb.linearVelocity = input * moveSpeed;
        }

        // Polymorphism: I override TakeDamage so the player gets a short invulnerability window that enemies don't have.
        public override void TakeDamage(int amount)
        {
            if (Time.time < invulnerableUntil) return;
            invulnerableUntil = Time.time + invulnerabilityTime;
            base.TakeDamage(amount);
        }

        protected override void Die()
        {
            Debug.Log("Player died");
            rb.linearVelocity = Vector2.zero;
            GameEvents.RaisePlayerDied();
        }

        // Observer: the player listens to its own HealthChanged event and logs it, and the HUD listens to the same event for the health bar.
        private void LogHealth(int current, int max)
        {
            Debug.Log($"Player health {current}/{max}");
        }

        private void HealBetweenWaves(int wave)
        {
            Heal(waveClearHeal);
        }

        [ContextMenu("Test: Take 10 Damage")]
        private void TestDamage()
        {
            TakeDamage(10);
        }
    }
}
