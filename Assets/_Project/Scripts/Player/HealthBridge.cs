using System;
using HelloDev.Entities;
using UnityEngine;
using Wander.Character.Components;
using Wander.Character.Events;

namespace Wander.Player
{
    [Provides(typeof(HealthComponent))]
    public class HealthBridge : EcsComponentBridge
    {
        [SerializeField] private HealthComponent _health = new() { BaseHealth = 100f };

        private IDisposable _damageSub;

        protected override void OnInitialize()
        {
            _health.MaxHealth = _health.BaseHealth;
            _health.CurrentHealth = _health.MaxHealth;
            _health.IsDead = false;
            Add(_health);

            _damageSub = World.Subscribe<DamageTakenEvent>(e =>
            {
                if (e.Entity != Entity) return;
                Debug.Log($"[HealthBridge] {gameObject.name} took {e.DamageAmount} damage. HP: {e.RemainingHealth}/{_health.MaxHealth}");
            });
        }

        protected override void OnDestroy()
        {
            _damageSub?.Dispose();
            base.OnDestroy();
        }
    }
}
