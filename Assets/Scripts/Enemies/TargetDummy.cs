using UnityEngine;
using EmberArena.Core;

namespace EmberArena.Enemies
{
    public class TargetDummy : Entity
    {
        private void OnEnable()
        {
            TargetRegistry.Register(this);
        }

        private void OnDisable()
        {
            TargetRegistry.Unregister(this);
        }

        protected override void Die()
        {
            Destroy(gameObject);
        }
    }
}
