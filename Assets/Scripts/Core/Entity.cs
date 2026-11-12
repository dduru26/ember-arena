using System;
using UnityEngine;

namespace EmberArena.Core
{
    // Inheritance: Player and every enemy share this base class, so health and damage are written once.
    // Abstraction: it is abstract on purpose, so you can't put a plain Entity in the scene.
    public abstract class Entity : MonoBehaviour, IDamageable
    {
        [SerializeField, Tooltip("Starting and maximum health.")]
        private int maxHealth = 100;

        // Encapsulation: currentHealth is private, so the only way to change it is TakeDamage or Heal.
        private int currentHealth;

        public int MaxHealth => maxHealth;
        public int CurrentHealth => currentHealth;
        public bool IsDead { get; private set; }

        // Observer: other scripts subscribe to these events, so the health bar and score never need a reference to this entity.
        public event Action<int, int> HealthChanged;
        public event Action<Entity> Died;

        protected virtual void Awake()
        {
            currentHealth = maxHealth;
        }

        public virtual void TakeDamage(int amount)
        {
            if (IsDead || amount <= 0) return;

            currentHealth = Mathf.Max(0, currentHealth - amount);
            HealthChanged?.Invoke(currentHealth, maxHealth);

            if (currentHealth == 0)
            {
                IsDead = true;
                Died?.Invoke(this);
                Die();
            }
        }

        public void Heal(int amount)
        {
            if (IsDead || amount <= 0) return;
            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
            HealthChanged?.Invoke(currentHealth, maxHealth);
        }

        // Polymorphism: each subclass decides for itself what dying means.
        protected abstract void Die();
    }
}
