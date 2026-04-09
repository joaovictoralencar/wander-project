using HelloDev.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
using Wander.Character.Attack;
using Wander.Character.Components;

namespace Wander.Player
{
    /// <summary>
    /// Receives Unity Input System events and writes <see cref="MoveInputComponent"/> into ECS each FixedUpdate.
    /// Pure push bridge — never reads from ECS.
    /// </summary>
    [Provides(typeof(MoveInputComponent))]
    [RequireComponent(typeof(PlayerInput))]
    public class PlayerInputBridge : EcsComponentBridge
    {
        [SerializeField] private Transform _cameraTransform;

        private Vector2 _moveInput;
        private bool _sprint;
        private bool _jumpPressed;
        private bool _dodgePressed;
        private AttackInputType _attackInputType;

        private void Awake()
        {
            if (_cameraTransform == null && Camera.main != null)
                _cameraTransform = Camera.main.transform;
        }

        protected override void OnInitialize() => Add(new MoveInputComponent());

        protected override void OnPushToEcs()
        {
            float3 direction = float3.zero;
            float inputMagnitude = math.min(1f, math.length(new float2(_moveInput.x, _moveInput.y)));

            if (inputMagnitude > 0.01f)
            {
                float3 worldDir;
                if (_cameraTransform != null)
                {
                    Vector3 fwd = _cameraTransform.forward;
                    fwd.y = 0f;
                    fwd.Normalize();
                    Vector3 right = _cameraTransform.right;
                    right.y = 0f;
                    right.Normalize();
                    worldDir = math.normalize((float3)(fwd * _moveInput.y + right * _moveInput.x));
                }
                else
                {
                    worldDir = math.normalize(new float3(_moveInput.x, 0f, _moveInput.y));
                }

                direction = worldDir * inputMagnitude;
            }

            Set(new MoveInputComponent
            {
                Direction = direction,
                Sprint = _sprint,
                Jump = _jumpPressed,
                Dodge = _dodgePressed,
                AttackInput = _attackInputType,
            });

            _jumpPressed = false;
            _dodgePressed = false;
            _attackInputType = AttackInputType.None;
        }

        // ── Unity Event receivers ───────

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

        public void OnDodge(InputAction.CallbackContext context)
        {
            if (context.performed)
                _dodgePressed = true;
        }

        public void OnLightAttack(InputAction.CallbackContext context)
        {
            if (context.performed)
                _attackInputType = AttackInputType.Light;
        }

        public void OnHeavyAttack(InputAction.CallbackContext context)
        {
            if (context.performed)
                _attackInputType = AttackInputType.Heavy;
        }
    }
}