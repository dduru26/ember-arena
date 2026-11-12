namespace EmberArena.Core
{
    // Abstraction: I made this an interface so anything that can be hurt only has to promise TakeDamage.
    // Bullets will talk to IDamageable and never to Player or Enemy directly.
    public interface IDamageable
    {
        void TakeDamage(int amount);
    }
}
