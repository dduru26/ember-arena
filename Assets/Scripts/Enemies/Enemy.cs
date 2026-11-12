using UnityEngine;
using EmberArena.Core;
using EmberArena.Patterns;

namespace EmberArena.Enemies
{
    // Inheritance: Chaser, Shooter and Tank all extend this, so health, movement and the state machine are written once.
    // Abstraction: Decide and PerformAttack are abstract, so every enemy type has to supply its own version.
    [RequireComponent(typeof(Rigidbody2D))]
    public abstract class Enemy : Entity
    {
        [SerializeField, Tooltip("Movement speed in world units per second.")]
        private float moveSpeed = 3f;

        [SerializeField, Tooltip("Seconds between attacks.")]
        private float attackInterval = 1f;

        [SerializeField, Tooltip("Points awarded when this enemy dies.")]
        private int scoreValue = 10;

        private Rigidbody2D rb;
        private Vector2 desiredVelocity;
        private float nextAttackTime;

        protected PlayerController Player { get; private set; }

        public EnemyState State { get; private set; }
        public int ScoreValue => scoreValue;

        protected override void Awake()
        {
            base.Awake();
            rb = GetComponent<Rigidbody2D>();
        }

        private void Start()
        {
            Player = FindFirstObjectByType<PlayerController>();
        }

        private void OnEnable()
        {
            TargetRegistry.Register(this);
        }

        private void OnDisable()
        {
            TargetRegistry.Unregister(this);
        }

        // Algorithm: state-based decision logic. It is O(1) per enemy per frame, so O(n) for n enemies.
        private void Update()
        {
            if (Player == null || Player.IsDead)
            {
                desiredVelocity = Vector2.zero;
                return;
            }

            // Step 1: measure how far the player is.
            Vector2 toPlayer = (Vector2)Player.transform.position - (Vector2)transform.position;
            float distance = toPlayer.magnitude;

            // Step 2: ask this enemy type which state it wants to be in.
            State = Decide(distance);

            // Step 3: act on that state.
            switch (State)
            {
                case EnemyState.Chase:
                    desiredVelocity = toPlayer.normalized * moveSpeed;
                    break;
                case EnemyState.Flee:
                    desiredVelocity = -toPlayer.normalized * moveSpeed;
                    break;
                default:
                    desiredVelocity = Vector2.zero;
                    if (Time.time >= nextAttackTime)
                    {
                        nextAttackTime = Time.time + attackInterval;
                        PerformAttack();
                    }
                    break;
            }
        }

        private void FixedUpdate()
        {
            rb.linearVelocity = desiredVelocity;
        }

        protected abstract EnemyState Decide(float distanceToPlayer);

        protected abstract void PerformAttack();

        protected override void Die()
        {
            GameEvents.RaiseEnemyKilled(ScoreValue);
            Destroy(gameObject);
        }
    }
}
