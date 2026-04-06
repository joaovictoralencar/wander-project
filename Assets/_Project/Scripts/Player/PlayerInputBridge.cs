using System;
using HelloDev.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
using Wander.Character.Components;

namespace Wander.Player
{
    /// <summary>
    /// Receives Unity Input System events wired in the Inspector (PlayerInput → Behavior:
    /// "Invoke Unity Events") and writes <see cref="MoveInputComponent"/> into ECS each FixedUpdate.
    /// This bridge is purely a push bridge — it never reads from ECS.
    /// Movement physics, gravity, and animation are handled by separate bridges.
    ///
    /// Requires an <see cref="EcsEntityRoot"/> on this or a parent GameObject to provide
    /// the entity and world context. For manual spawning, call
    /// <see cref="EcsComponentBridge.Initialize(EcsWorld, Entity)"/> instead.
    /// </summary>
    [RequireComponent(typeof(PlayerInput))]
    public class PlayerInputBridge : EcsComponentBridge
    {
        [SerializeField] private Transform _cameraTransform;

        // Cached input state — updated by Unity Events, consumed in OnPushToEcs.
        private Vector2 _moveInput;
        private bool _sprint;
        private bool _jumpPressed;

        public override Type[] ProvidedComponents => new[] { typeof(MoveInputComponent) };

        private void Awake()
        {
            if (_cameraTransform == null && Camera.main != null)
                _cameraTransform = Camera.main.transform;
        }

        protected override void OnInitialize()
        {
            if (!World.HasComponent<MoveInputComponent>(Entity))
                World.AddComponent(Entity, new MoveInputComponent());
        }

        // Called before systems run each FixedUpdate.
        // Converts cached raw input into a camera-relative direction and writes MoveInputComponent.
        protected override void OnPushToEcs()
        {
            float3 direction = float3.zero;
            float inputMagnitude = math.min(1f, math.length(new float2(_moveInput.x, _moveInput.y)));

            if (inputMagnitude > 0.01f)
            {
                float3 worldDir;
                if (_cameraTransform != null)
                {
                    Vector3 fwd   = _cameraTransform.forward; fwd.y = 0f; fwd.Normalize();
                    Vector3 right = _cameraTransform.right;  right.y = 0f; right.Normalize();
                    worldDir = math.normalize((float3)(fwd * _moveInput.y + right * _moveInput.x));
                }
                else
                {
                    worldDir = math.normalize(new float3(_moveInput.x, 0f, _moveInput.y));
                }
                direction = worldDir * inputMagnitude;
            }

            World.SetComponent(Entity, new MoveInputComponent
            {
                Direction = direction,
                Sprint    = _sprint,
                Jump      = _jumpPressed,
            });
            _jumpPressed = false; // consumed — cleared so subsequent fixed steps don't re-jump
        }

        // ── Unity Event receivers (wire these in the PlayerInput Inspector) ───────

        public void OnMove(InputAction.CallbackContext context)
        {
            _moveInput = context.canceled ? Vector2.zero : context.ReadValue<Vector2>();
        }

        public void OnSprint(InputAction.CallbackContext context)
        {
            _sprint = context.performed;
        }

        public void OnJump(InputAction.CallbackContext context)
        {
            if (context.performed)
                _jumpPressed = true;
        }
    }
}