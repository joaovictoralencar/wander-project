using System;
using System.Collections.Generic;
using HelloDev.Entities;
using Wander.Character.Components;
using Wander.Character.Events;

namespace Wander.Character.Systems
{
    [Serializable]
    public class DamageSystem : EcsSystemBase
    {
        public override int Order => 10;

        public override Type[] RequiredComponents => new[]
        {
            typeof(HealthComponent),
        };

        private IDisposable _hitSub;

        public override void Initialize(EcsWorld world)
        {
            _hitSub = world.Subscribe<HitEvent>(e =>
            {
                if (!world.IsAlive(e.Target)) return;
                if (!world.HasComponent<HealthComponent>(e.Target)) return;

                var health = world.GetComponent<HealthComponent>(e.Target);
                health.CurrentHealth -= e.Damage;
                if (health.CurrentHealth <= 0f)
                {
                    health.CurrentHealth = 0f;
                    health.IsDead = true;
                }
                world.SetComponent(e.Target, health);

                world.Send(new DamageTakenEvent
                {
                    Entity          = e.Target,
                    DamageAmount    = e.Damage,
                    RemainingHealth = health.CurrentHealth,
                });

                EcsDebug.Log($"Damage: {e.Damage} → Entity({e.Target.Id}), HP: {health.CurrentHealth}");
            });
        }

        public override void Dispose()
        {
            _hitSub?.Dispose();
        }
    }
}
