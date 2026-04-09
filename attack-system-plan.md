# Attack & Combo System — Full Implementation Guide

## Design Decisions (confirmed)
| Decision | Choice |
|----------|--------|
| Input model | Two buttons — Light + Heavy, timing-based chains |
| Movement during attack | Locked (`CanMove = false`, same as dodge) |
| Dodge cancel | Not allowed during attacks; dodge only after attack/combo fully ends |
| Combo data format | ScriptableObject per combo definition |
| Animation playback | AnimationClip refs on SO → AnimatorOverrideController |
| Timing windows | Animation Events on clips (not hardcoded timestamps) |
| Anim event routing | Proxy script on Animator child forwards to AttackBridge |
| Damage model | HealthComponent on entities; hitbox collider system |
| Test target | Dummy enemy prefab with EcsEntityRoot + HealthBridge |

## Architecture Overview

```
PlayerInputBridge (Push)
  ↓ MoveInputComponent.AttackInput (Light/Heavy/None)

AttackSystem (FixedExecute, Order 5)
  reads: MoveInputComponent, AttackComponent, MovementStateComponent
  writes: AttackComponent (state machine), MovementStateComponent (CanMove)
  sends: AttackComboStartEvent (new combo), AttackStepStartedEvent (advance), AttackEndedEvent
  NOTE: window flags (ComboWindowOpen, HitboxActive) are SET
        by Animation Events via proxy → bridge → component, NOT computed by the system.
        Dodge is blocked entirely while IsAttacking — no mid-attack cancel.
  ↓
AttackBridge (FixedPullFromEcs + Animation Event callbacks)
  plays AnimationClip, receives anim events via AttackAnimEventProxy,
  sets window flags on AttackComponent, enables/disables hitbox collider
  OnTriggerEnter → world.Send(HitEvent)
  ↓
DamageSystem (subscribes to HitEvent)
  writes: HealthComponent on target
  sends: DamageTakenEvent
  ↓
HealthBridge (subscribes to DamageTakenEvent)
  visual feedback (debug log / flash)
```

---

## Phase 1 — Data Definitions

### 1.1 Create `AttackInputType.cs`

**Path:** `Assets/_Project/Scripts/Character/Attack/AttackInputType.cs`

```csharp
namespace Wander.Character.Attack
{
    public enum AttackInputType : byte
    {
        None  = 0,
        Light = 1,
        Heavy = 2,
    }
}
```

### 1.2 Create `ComboStep.cs`

**Path:** `Assets/_Project/Scripts/Character/Attack/ComboStep.cs`

A serializable class (not struct — it holds a managed `AnimationClip` reference). Each step defines one animation in a combo sequence. **Timing windows (combo, hitbox) are defined via Animation Events on the clip, not here.** Only the clip reference and a damage multiplier live on the SO. Final damage is computed at hit time as `CombatStatsComponent.BaseDamage * DamageMultiplier`.

```csharp
using System;
using UnityEngine;

namespace Wander.Character.Attack
{
    [Serializable]
    public class ComboStep
    {
        [Header("Animation")]
        public AnimationClip Clip;

        [Header("Damage")]
        [Tooltip("Multiplied by CombatStatsComponent.BaseDamage at hit time")]
        public float DamageMultiplier = 1f;
    }
}
```

### 1.3 Create `AttackAnimEventProxy.cs`

**Path:** `Assets/_Project/Scripts/Character/Attack/AttackAnimEventProxy.cs`

Small proxy MonoBehaviour placed on the **same GameObject as the Animator**. It receives Animation Events and forwards them to the `AttackBridge` on the parent. This is needed because Unity dispatches Animation Events only to components on the Animator's own GameObject.

```csharp
using UnityEngine;

namespace Wander.Character.Attack
{
    /// <summary>
    /// Place this on the same GameObject as the Animator.
    /// Animation Events call these methods; the proxy forwards them to AttackBridge.
    /// </summary>
    public class AttackAnimEventProxy : MonoBehaviour
    {
        private IAttackAnimEventReceiver _receiver;

        private void Awake()
        {
            _receiver = GetComponentInParent<IAttackAnimEventReceiver>();
            if (_receiver == null)
                Debug.LogWarning($"[AttackAnimEventProxy] No IAttackAnimEventReceiver found on parents of '{gameObject.name}'.", this);
        }

        // ── Called by Animation Events on attack clips ──

        public void OnComboWindowOpen()    => _receiver?.OnComboWindowOpen();
        public void OnComboWindowClose()   => _receiver?.OnComboWindowClose();
        public void OnHitboxActivate()     => _receiver?.OnHitboxActivate();
        public void OnHitboxDeactivate()   => _receiver?.OnHitboxDeactivate();
    }
}
```

### 1.4 Create `IAttackAnimEventReceiver.cs`

**Path:** `Assets/_Project/Scripts/Character/Attack/IAttackAnimEventReceiver.cs`

Interface so the proxy doesn't depend directly on AttackBridge.

```csharp
namespace Wander.Character.Attack
{
    /// <summary>
    /// Implemented by AttackBridge. Called by AttackAnimEventProxy to forward Animation Events.
    /// </summary>
    public interface IAttackAnimEventReceiver
    {
        void OnComboWindowOpen();
        void OnComboWindowClose();
        void OnHitboxActivate();
        void OnHitboxDeactivate();
    }
}
```

### Animation Events Setup (per clip)

For each attack AnimationClip, add these events in Unity's Animation window:

| Event method name | When to place it | Purpose |
|---|---|---|
| `OnHitboxActivate` | When the weapon swing starts connecting | Enables the hitbox collider |
| `OnHitboxDeactivate` | When the swing ends | Disables the hitbox collider |
| `OnComboWindowOpen` | Mid-animation, after the main impact | Player can queue the next combo input |
| `OnComboWindowClose` | Late in the animation, before it ends | Combo window shuts — no more input accepted |

**Tip:** Open the clip in the Animation window, click the event timeline row, and add events. The function name must exactly match the method names above.

### 1.5 Create `ComboDefinition.cs`

**Path:** `Assets/_Project/Scripts/Character/Attack/ComboDefinition.cs`

A ScriptableObject asset. One per combo (e.g. "Light Rush", "Heavy Finisher"). The `InputPattern` array defines the sequence of button presses that triggers this combo. The `Steps` array defines the animations and windows.

```csharp
using UnityEngine;

namespace Wander.Character.Attack
{
    [CreateAssetMenu(fileName = "NewCombo", menuName = "Wander/Combo Definition")]
    public class ComboDefinition : ScriptableObject
    {
        [Tooltip("Display name (debug / UI)")]
        public string ComboName;

        [Tooltip("Sequence of inputs that selects this combo, e.g. [Light, Light, Heavy]")]
        public AttackInputType[] InputPattern;

        [Tooltip("One entry per step — clip, damage, timing windows")]
        public ComboStep[] Steps;
    }
}
```

**After creating the script**, right-click in Project → Create → Wander → Combo Definition to create assets.

**Example asset "Light Rush":**
```
ComboName: Light Rush
InputPattern: [Light, Light, Light]
Steps:
  [0] Clip: Slash01, DamageMultiplier: 1.0  (timing via Animation Events on Slash01)
  [1] Clip: Slash02, DamageMultiplier: 1.2  (timing via Animation Events on Slash02)
  [2] Clip: Slash03, DamageMultiplier: 1.8  (timing via Animation Events on Slash03)
```

**Example asset "Power Combo":**
```
ComboName: Power Combo
InputPattern: [Light, Light, Heavy]
Steps:
  [0] Clip: Slash01,    DamageMultiplier: 1.0
  [1] Clip: Slash02,    DamageMultiplier: 1.2
  [2] Clip: HeavySlam,  DamageMultiplier: 2.5  (heavy finisher hits harder)
```

---

## Phase 2 — ECS Components

### 2.1 Create `CombatStatsComponent.cs`

**Path:** `Assets/_Project/Scripts/Character/Components/CombatStatsComponent.cs`

Base combat stats — follows the same pattern as `MovementStatsComponent`. Config fields are serialized (editable in bridge Inspector). Runtime fields for future buffs/debuffs are `[NonSerialized]`.

A `CombatStatsBridge` provides this component (see Phase 6). When the player levels up or equips a weapon, you only change `BaseDamage` here — all combos scale automatically via their multipliers.

```csharp
using System;

namespace Wander.Character.Components
{
    [Serializable]
    public struct CombatStatsComponent
    {
        // Config — tweakable in Inspector via bridge
        public float BaseDamage;
        public float AttackSpeed;      // future: animation speed multiplier
        public float CritChance;       // future: 0–1 probability
        public float CritMultiplier;   // future: e.g. 1.5x

        // Runtime — for buffs/debuffs (future)
        [NonSerialized] public float BonusDamage;
    }
}
```

### 2.2 Create `AttackComponent.cs`

**Path:** `Assets/_Project/Scripts/Character/Components/AttackComponent.cs`

Pure runtime state — all `[NonSerialized]`. Config (which combos are available) lives on the bridge because ECS components must be `unmanaged` structs (no managed refs like arrays or SO references).

Window flags (`ComboWindowOpen`, `HitboxActive`) are **set directly by the bridge** when it receives Animation Events via the proxy — the system just reads them. Dodge is blocked entirely while `IsAttacking`; to exit a combo early, simply don't input during the combo window. The step duration is still copied from `clip.length` so the system knows when the step ends.

`StepDamageMultiplier` is copied from `ComboStep.DamageMultiplier` by the bridge when a step starts. At hit time, final damage = `(CombatStats.BaseDamage + CombatStats.BonusDamage) * StepDamageMultiplier`.

```csharp
using System;
using Wander.Character.Attack;

namespace Wander.Character.Components
{
    [Serializable]
    public struct AttackComponent
    {
        // ── Runtime state (written by AttackSystem + AttackBridge) ──

        [NonSerialized] public bool  IsAttacking;
        [NonSerialized] public int   CurrentComboIndex;   // index into bridge's ComboDefinition[]
        [NonSerialized] public int   CurrentStepIndex;    // step within the combo
        [NonSerialized] public float ElapsedTime;         // seconds since current step started
        [NonSerialized] public int   ComboInputCount;     // how many inputs registered so far in this combo

        // ── Per-step data (copied from ComboStep by bridge when step starts) ──

        [NonSerialized] public float StepDuration;          // clip.length
        [NonSerialized] public float StepDamageMultiplier;  // from ComboStep.DamageMultiplier
        [NonSerialized] public int   MaxSteps;              // total steps in resolved combo (set by bridge)

        // ── Flags (set by Animation Events via proxy → bridge) ──

        [NonSerialized] public bool  ComboWindowOpen;
        [NonSerialized] public bool  HitboxActive;
        [NonSerialized] public bool  HitLanded;           // prevents multi-hit per step

        // ── Input buffer ──

        [NonSerialized] public AttackInputType BufferedInput;
    }
}
```

### 2.3 Create `HealthComponent.cs`

**Path:** `Assets/_Project/Scripts/Character/Components/HealthComponent.cs`

Config field `BaseHealth` is serialized (editable in bridge Inspector). Runtime `MaxHealth` starts equal to `BaseHealth` but can be modified by buffs/equipment. `CurrentHealth` is `[NonSerialized]`.

```csharp
using System;

namespace Wander.Character.Components
{
    [Serializable]
    public struct HealthComponent
    {
        // Config
        public float BaseHealth;

        // Runtime
        [NonSerialized] public float MaxHealth;
        [NonSerialized] public float CurrentHealth;
        [NonSerialized] public bool  IsDead;
    }
}
```

---

## Phase 3 — Events

Follow the existing pattern: each event is a small struct with an `Entity` field.

### 3.1 Create `AttackComboStartEvent.cs`

**Path:** `Assets/_Project/Scripts/Character/Events/AttackComboStartEvent.cs`

Fired when a brand-new combo begins. The bridge subscribes to resolve which combo to play and play the first clip.

```csharp
using HelloDev.Entities;

namespace Wander.Character.Events
{
    /// <summary>Fired by AttackSystem when a new combo begins. Bridge resolves which combo to play.</summary>
    public struct AttackComboStartEvent
    {
        public Entity Entity;
    }
}
```

### 3.2 Create `AttackStepStartedEvent.cs`

**Path:** `Assets/_Project/Scripts/Character/Events/AttackStepStartedEvent.cs`

Fired when a combo step begins (advancing to step 2, 3, etc.). The bridge subscribes to play the correct AnimationClip. Note: step 0 is handled by `AttackComboStartEvent`, not this event.

```csharp
using HelloDev.Entities;

namespace Wander.Character.Events
{
    /// <summary>Fired by AttackSystem when a combo step begins.</summary>
    public struct AttackStepStartedEvent
    {
        public Entity Entity;
        public int    ComboIndex; // index into bridge's combo list
        public int    StepIndex;  // step within the combo
    }
}
```

### 3.3 Create `AttackEndedEvent.cs`

**Path:** `Assets/_Project/Scripts/Character/Events/AttackEndedEvent.cs`

```csharp
using HelloDev.Entities;

namespace Wander.Character.Events
{
    /// <summary>Fired by AttackSystem when a combo/attack fully ends.</summary>
    public struct AttackEndedEvent
    {
        public Entity Entity;
    }
}
```

### 3.4 Create `HitEvent.cs`

**Path:** `Assets/_Project/Scripts/Character/Events/HitEvent.cs`

Fired by the AttackBridge when the hitbox trigger collides with a target entity.

```csharp
using HelloDev.Entities;

namespace Wander.Character.Events
{
    /// <summary>Fired by AttackBridge when hitbox collides with a target entity.</summary>
    public struct HitEvent
    {
        public Entity Attacker;
        public Entity Target;
        public float  Damage;
    }
}
```

### 3.5 Create `DamageTakenEvent.cs`

**Path:** `Assets/_Project/Scripts/Character/Events/DamageTakenEvent.cs`

Fired by DamageSystem after applying damage — bridges subscribe for visual feedback.

```csharp
using HelloDev.Entities;

namespace Wander.Character.Events
{
    /// <summary>Fired by DamageSystem after damage is applied to an entity.</summary>
    public struct DamageTakenEvent
    {
        public Entity Entity;
        public float  DamageAmount;
        public float  RemainingHealth;
    }
}
```

---

## Phase 4 — Modify Input System

### 4.1 Modify `MoveInputComponent.cs`

**Path:** `Assets/_Project/Scripts/Character/Components/MoveInputComponent.cs`

Replace `bool Attack` with `AttackInputType AttackInput`. Add the using for the enum.

**Full file after change:**
```csharp
using System;
using Unity.Mathematics;
using Wander.Character.Attack;

namespace Wander.Character.Components
{
    [Serializable]
    public struct MoveInputComponent
    {
        // All runtime — written by PlayerInputBridge each frame
        [NonSerialized] public float3          Direction;
        [NonSerialized] public bool            Sprint;
        [NonSerialized] public bool            Jump;
        [NonSerialized] public bool            Dodge;
        [NonSerialized] public AttackInputType AttackInput;
    }
}
```

### 4.2 Modify `PlayerInputBridge.cs`

**Path:** `Assets/_Project/Scripts/Player/PlayerInputBridge.cs`

Changes:
1. Replace `bool _attackPressed` with `AttackInputType _attackInputType`
2. `OnLightAttack` sets `_attackInputType = AttackInputType.Light`
3. Add `OnHeavyAttack` that sets `_attackInputType = AttackInputType.Heavy`
4. In `OnPushToEcs`, write `AttackInput = _attackInputType` and reset to `None`

**Full file after change:**
```csharp
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
```

### 4.3 Manual Step — Input Action Map

Open `Assets/_Project/PlayerInputActionMap.inputactions` in Unity and add **LightAttack** and **HeavyAttack** actions (e.g. left mouse / right mouse). The callback names must match `OnLightAttack` and `OnHeavyAttack`.

---

## Phase 5 — AttackSystem

### 5.1 Create `AttackSystem.cs`

**Path:** `Assets/_Project/Scripts/Character/Systems/AttackSystem.cs`

This is the core combo state machine. Order 5 means it runs after DodgeSystem (0). Dodge is blocked entirely while `IsAttacking` (handled in DodgeSystem); the player exits a combo by not pressing attack during the combo window.

**Key design notes:**
- The system only reads unmanaged fields from `AttackComponent`.
- **Window flags are NOT computed here** — they're set by Animation Events via proxy → bridge. The system only reads `ComboWindowOpen` and `HitboxActive`.
- When the system decides a new step should start, it fires `AttackStepStartedEvent` — the bridge receives it, copies step data into the component, and plays the clip.

```csharp
using System;
using System.Collections.Generic;
using HelloDev.Entities;
using Wander.Character.Attack;
using Wander.Character.Components;
using Wander.Character.Events;

namespace Wander.Character.Systems
{
    /// <summary>
    /// Combo state machine running each physics step.
    /// Reads AttackComponent + MoveInputComponent, writes state,
    /// fires AttackComboStartEvent / AttackStepStartedEvent / AttackEndedEvent.
    /// Window flags (combo, hitbox) are set externally by Animation Events.
    /// </summary>
    [Serializable]
    public class AttackSystem : EcsSystemBase
    {
        public override int Order => 5;

        public override Type[] RequiredComponents => new[]
        {
            typeof(MoveInputComponent),
            typeof(MovementStateComponent),
            typeof(AttackComponent),
        };

        public override void Initialize(EcsWorld world) { }

        public override void FixedExecute(EcsWorld world, List<int> entities, float fixedDeltaTime)
        {
            for (var i = 0; i < entities.Count; i++)
            {
                var entity    = world.GetEntity(entities[i]);
                var input     = world.GetComponent<MoveInputComponent>(entity);
                var attack    = world.GetComponent<AttackComponent>(entity);
                var moveState = world.GetComponent<MovementStateComponent>(entity);

                // ── Buffer incoming input ──
                if (input.AttackInput != AttackInputType.None)
                {
                    attack.BufferedInput = input.AttackInput;
                    input.AttackInput = AttackInputType.None;
                    world.SetComponent(entity, input);
                }

                if (attack.IsAttacking)
                {
                    // ── Advance time ──
                    attack.ElapsedTime += fixedDeltaTime;

                    // Reset hit-landed when hitbox deactivates (for next step)
                    if (!attack.HitboxActive)
                        attack.HitLanded = false;

                    // ── Combo advance: input buffered while combo window is open ──
                    if (attack.ComboWindowOpen && attack.BufferedInput != AttackInputType.None)
                    {
                        int nextStep = attack.CurrentStepIndex + 1;

                        // If we've exhausted all steps in this combo, end attack
                        if (nextStep >= attack.MaxSteps)
                        {
                            attack.IsAttacking      = false;
                            attack.HitboxActive     = false;
                            attack.ComboWindowOpen  = false;
                            attack.BufferedInput    = AttackInputType.None;
                            moveState.CanMove       = true;

                            world.SetComponent(entity, attack);
                            world.SetComponent(entity, moveState);
                            world.Send(new AttackEndedEvent { Entity = entity });
                            continue;
                        }

                        attack.ComboInputCount++;
                        attack.BufferedInput    = AttackInputType.None;
                        attack.CurrentStepIndex = nextStep;
                        attack.ElapsedTime      = 0f;
                        attack.HitboxActive     = false;
                        attack.ComboWindowOpen  = false;
                        attack.HitLanded        = false;

                        world.SetComponent(entity, attack);
                        world.Send(new AttackStepStartedEvent
                        {
                            Entity     = entity,
                            ComboIndex = attack.CurrentComboIndex,
                            StepIndex  = nextStep,
                        });
                        continue;
                    }

                    // ── Step finished (elapsed >= duration, no combo input) → end attack ──
                    if (attack.ElapsedTime >= attack.StepDuration)
                    {
                        attack.IsAttacking      = false;
                        attack.HitboxActive     = false;
                        attack.ComboWindowOpen  = false;
                        attack.BufferedInput    = AttackInputType.None;
                        moveState.CanMove       = true;

                        world.SetComponent(entity, attack);
                        world.SetComponent(entity, moveState);
                        world.Send(new AttackEndedEvent { Entity = entity });
                        continue;
                    }

                    world.SetComponent(entity, attack);
                    continue;
                }

                // ── Not attacking — start new combo if input buffered and grounded ──
                if (attack.BufferedInput != AttackInputType.None && moveState.IsGrounded)
                {
                    attack.IsAttacking      = true;
                    attack.CurrentStepIndex = 0;
                    attack.ComboInputCount  = 1;
                    attack.ElapsedTime      = 0f;
                    attack.HitboxActive     = false;
                    attack.ComboWindowOpen  = false;
                    attack.HitLanded        = false;
                    moveState.CanMove       = false;

                    attack.BufferedInput = AttackInputType.None;

                    world.SetComponent(entity, attack);
                    world.SetComponent(entity, moveState);
                    world.Send(new AttackComboStartEvent { Entity = entity });
                    continue;
                }

                // Clear stale buffered input when not attacking and not grounded
                if (attack.BufferedInput != AttackInputType.None && !moveState.IsGrounded)
                {
                    attack.BufferedInput = AttackInputType.None;
                    world.SetComponent(entity, attack);
                }
            }
        }
    }
}
```

### 5.2 Combo Matching (in the Bridge)

The bridge handles combo matching because it owns the managed `ComboDefinition[]` references. When it receives `AttackComboStartEvent`, it:

1. Resolves the best-matching combo from `_inputHistory` (which already contains the first input, tracked in `OnPushToEcs`)
2. Sets `CurrentComboIndex` and `MaxSteps` on AttackComponent
3. Plays the first clip of the resolved combo

When it receives `AttackStepStartedEvent` (for steps 2+), it **re-resolves** the combo from the accumulated input history. This allows combo branching — e.g. combos `[L,L,L]` and `[L,L,H]` share steps 0-1, then diverge at step 2 based on the third input. The bridge updates `CurrentComboIndex` and `MaxSteps` if the resolved combo changes, then plays the correct clip.

---

## Phase 6 — AttackBridge

### 6.1 Create `AttackBridge.cs`

**Path:** `Assets/_Project/Scripts/Player/AttackBridge.cs`

This is the most complex bridge. It:
- Holds the config (`ComboDefinition[]`)
- Manages an `AnimatorOverrideController` for clip playback
- Tracks the input sequence for combo matching
- Subscribes to `AttackComboStartEvent` / `AttackStepStartedEvent` / `AttackEndedEvent`
- **Implements `IAttackAnimEventReceiver`** — receives forwarded Animation Events from the proxy
- Sets window flags (`ComboWindowOpen`, `HitboxActive`) on `AttackComponent`
- Manages the hitbox collider on/off
- Detects hits via `OnTriggerEnter`

```csharp
using System;
using System.Collections.Generic;
using HelloDev.Entities;
using UnityEngine;
using Wander.Character.Attack;
using Wander.Character.Components;
using Wander.Character.Events;
using Wander.Character.Systems;

namespace Wander.Player
{
    [RequiresSystem(typeof(AttackSystem))]
    [Provides(typeof(AttackComponent))]
    public class AttackBridge : EcsComponentBridge, IAttackAnimEventReceiver
    {
        [Header("Combo Data")]
        [SerializeField] private ComboDefinition[] _combos;

        [Header("References")]
        [SerializeField] private Animator _animator;
        [SerializeField] private Collider _hitboxCollider;

        [Header("Animator Override")]
        [Tooltip("Name of the placeholder state in the Animator Controller")]
        [SerializeField] private string _attackStateA = "AttackA";
        [SerializeField] private string _attackStateB = "AttackB";
        [SerializeField] private float  _crossFadeDuration = 0.1f;

        private AnimatorOverrideController _overrideController;
        private bool _useStateA = true;

        // Input history for combo matching
        private readonly List<AttackInputType> _inputHistory = new();

        private IDisposable _comboStartSub;
        private IDisposable _stepStartedSub;
        private IDisposable _attackEndedSub;

        private void Awake()
        {
            if (_animator == null)
                _animator = GetComponentInChildren<Animator>();

            if (_hitboxCollider != null)
            {
                _hitboxCollider.isTrigger = true;
                _hitboxCollider.enabled = false;
            }

            // Create override controller from the animator's current controller
            if (_animator != null && _animator.runtimeAnimatorController != null)
            {
                _overrideController = new AnimatorOverrideController(_animator.runtimeAnimatorController);
                _animator.runtimeAnimatorController = _overrideController;
            }
        }

        protected override void OnInitialize()
        {
            Add(new AttackComponent());

            _comboStartSub  = World.Subscribe<AttackComboStartEvent>(OnComboStart);
            _stepStartedSub = World.Subscribe<AttackStepStartedEvent>(OnStepStarted);
            _attackEndedSub = World.Subscribe<AttackEndedEvent>(OnAttackEnded);
        }

        // ── Animation Event Callbacks (from AttackAnimEventProxy) ──

        public void OnComboWindowOpen()
        {
            var attack = Get<AttackComponent>();
            if (!attack.IsAttacking) return;
            attack.ComboWindowOpen = true;
            Set(attack);
        }

        public void OnComboWindowClose()
        {
            using var attack = Modify<AttackComponent>();
            attack.Value.ComboWindowOpen = false;
        }

        public void OnHitboxActivate()
        {
            var attack = Get<AttackComponent>();
            if (!attack.IsAttacking) return;
            attack.HitboxActive = true;
            attack.HitLanded = false;
            Set(attack);

            if (_hitboxCollider != null)
                _hitboxCollider.enabled = true;
        }

        public void OnHitboxDeactivate()
        {
            using var attack = Modify<AttackComponent>();
            attack.Value.HitboxActive = false;

            if (_hitboxCollider != null)
                _hitboxCollider.enabled = false;
        }

        // ── ECS Event Handlers ──

        private void OnComboStart(AttackComboStartEvent e)
        {
            if (e.Entity != Entity) return;

            var attack = Get<AttackComponent>();

            // Resolve combo using _inputHistory (populated by TrackInput in OnPushToEcs).
            // Do NOT clear history before resolve — the first input is already tracked.
            int comboIndex = ResolveCombo();
            attack.CurrentComboIndex = comboIndex;

            if (comboIndex < 0 || comboIndex >= _combos.Length)
            {
                EndAttack();
                return;
            }

            var combo = _combos[comboIndex];
            if (combo.Steps.Length == 0)
            {
                EndAttack();
                return;
            }

            attack.MaxSteps = combo.Steps.Length;

            var step = combo.Steps[0];
            attack.StepDuration = step.Clip != null ? step.Clip.length : 0.5f;
            attack.StepDamageMultiplier = step.DamageMultiplier;
            Set(attack);

            PlayClip(step.Clip);
        }

        private void OnStepStarted(AttackStepStartedEvent e)
        {
            if (e.Entity != Entity) return;

            var attack = Get<AttackComponent>();

            // Re-resolve combo — player may have diverged to a different combo mid-chain.
            // E.g. combos [L,L,L] and [L,L,H]: after pressing L,L,H the resolve switches to the second.
            int comboIndex = ResolveCombo();
            if (comboIndex >= 0 && comboIndex < _combos.Length)
            {
                attack.CurrentComboIndex = comboIndex;
                attack.MaxSteps = _combos[comboIndex].Steps.Length;
            }
            else
            {
                comboIndex = attack.CurrentComboIndex;
            }

            if (comboIndex < 0 || comboIndex >= _combos.Length)
            {
                EndAttack();
                return;
            }

            var combo = _combos[comboIndex];
            int stepIndex = e.StepIndex;
            if (stepIndex >= combo.Steps.Length)
            {
                EndAttack();
                return;
            }

            var step = combo.Steps[stepIndex];
            attack.StepDuration = step.Clip != null ? step.Clip.length : 0.5f;
            attack.StepDamageMultiplier = step.DamageMultiplier;
            Set(attack);

            PlayClip(step.Clip);
        }

        private void OnAttackEnded(AttackEndedEvent e)
        {
            if (e.Entity != Entity) return;
            EndAttack();
        }

        private void EndAttack()
        {
            _inputHistory.Clear();
            if (_hitboxCollider != null)
                _hitboxCollider.enabled = false;
        }

        // ── Combo Matching ──

        /// <summary>
        /// Track each attack input the player makes (called from OnPushToEcs).
        /// </summary>
        public void TrackInput(AttackInputType inputType)
        {
            if (inputType != AttackInputType.None)
                _inputHistory.Add(inputType);
        }

        /// <summary>
        /// Find the best ComboDefinition matching the current input history.
        /// Returns the index into _combos, or -1 if no match.
        /// </summary>
        private int ResolveCombo()
        {
            int bestIndex = -1;
            int bestLength = 0;

            for (int c = 0; c < _combos.Length; c++)
            {
                var pattern = _combos[c].InputPattern;
                if (pattern == null || pattern.Length == 0) continue;
                if (_inputHistory.Count > pattern.Length) continue;

                bool matches = true;
                for (int j = 0; j < _inputHistory.Count && j < pattern.Length; j++)
                {
                    if (_inputHistory[j] != pattern[j])
                    {
                        matches = false;
                        break;
                    }
                }

                if (matches && pattern.Length > bestLength)
                {
                    bestIndex = c;
                    bestLength = pattern.Length;
                }
            }

            return bestIndex;
        }

        // ── Animation Playback ──

        private void PlayClip(AnimationClip clip)
        {
            if (_overrideController == null || clip == null) return;

            string stateName = _useStateA ? _attackStateA : _attackStateB;
            _overrideController[stateName] = clip;
            _animator.CrossFadeInFixedTime(stateName, _crossFadeDuration);
            _useStateA = !_useStateA;
        }

        // ── Push / Pull ──

        protected override void OnPushToEcs()
        {
            var input = Get<MoveInputComponent>();
            TrackInput(input.AttackInput);
        }

        protected override void OnFixedPullFromEcs() { }

        // ── Hitbox Collision ──

        private void OnTriggerEnter(Collider other)
        {
            var attack = Get<AttackComponent>();
            if (!attack.HitboxActive || attack.HitLanded) return;

            var targetRoot = other.GetComponentInParent<EcsEntityRoot>();
            if (targetRoot == null || targetRoot.Entity == Entity) return;

            attack.HitLanded = true;
            Set(attack);

            // Compute final damage from base stats × step multiplier
            var stats = Get<CombatStatsComponent>();
            float finalDamage = (stats.BaseDamage + stats.BonusDamage)
                              * attack.StepDamageMultiplier;

            World.Send(new HitEvent
            {
                Attacker = Entity,
                Target   = targetRoot.Entity,
                Damage   = finalDamage,
            });
        }

        protected override void OnDestroy()
        {
            _comboStartSub?.Dispose();
            _stepStartedSub?.Dispose();
            _attackEndedSub?.Dispose();
            base.OnDestroy();
        }
    }
}
```

### 6.2 Animator Controller Setup (Manual)

You need two empty states in your Animator Controller for clip overriding:

1. Open your Animator Controller (`Animator_Player.controller`)
2. Create two states: **AttackA** and **AttackB**
3. Assign any placeholder clip to both (can be an empty 1-frame clip)
4. Add transitions from **Any State** → **AttackA** and **Any State** → **AttackB** (triggered by script via CrossFade, no transition conditions needed — or use triggers if preferred)
5. Add transitions back from both states → your idle/locomotion blend tree (with exit time or a bool parameter)

The `AnimatorOverrideController` will swap the clip at runtime. The state name must match `_attackStateA` / `_attackStateB` on the bridge.

### 6.3 AttackAnimEventProxy Setup (Manual)

1. Find the child GameObject that has the **Animator** component (e.g. the model)
2. Add the **AttackAnimEventProxy** component to it
3. That's it — it auto-finds the `AttackBridge` via `GetComponentInParent`

---

## Phase 7 — DamageSystem

### 7.1 Create `DamageSystem.cs`

**Path:** `Assets/_Project/Scripts/Character/Systems/DamageSystem.cs`

Subscribes to `HitEvent` in `Initialize`. When a hit arrives, reads the target's `HealthComponent`, subtracts damage, writes it back, and fires `DamageTakenEvent`.

**Note:** This is a GlobalSystem (registered on `EcsSystemRunner.GlobalSystems`, not via `[RequiresSystem]`). It is purely event-driven — it does not iterate entities in `FixedExecute`. The `RequiredComponents` return `HealthComponent` for documentation purposes only (indicating which components this system touches). The runner passes entity lists but since `FixedExecute` is not overridden, they are unused.

```csharp
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
```

---

## Phase 8 — HealthBridge

### 8.1 Create `HealthBridge.cs`

**Path:** `Assets/_Project/Scripts/Player/HealthBridge.cs`

Simple bridge: serializes `BaseHealth` config, initializes `MaxHealth = BaseHealth` and `CurrentHealth = MaxHealth`, subscribes to `DamageTakenEvent` for debug feedback.

```csharp
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
```

### 8.2 Create `CombatStatsBridge.cs`

**Path:** `Assets/_Project/Scripts/Player/CombatStatsBridge.cs`

Provides `CombatStatsComponent`. Follows the same pattern as `MoveEntityBridge` providing `MovementStatsComponent` — config fields serialized in the Inspector, copied into the ECS component on init.

This is the **single source of truth** for an entity's base combat stats. Level-ups, weapon equips, or buff systems modify this component at runtime.

```csharp
using HelloDev.Entities;
using UnityEngine;
using Wander.Character.Components;

namespace Wander.Player
{
    [Provides(typeof(CombatStatsComponent))]
    public class CombatStatsBridge : EcsComponentBridge
    {
        [SerializeField] private CombatStatsComponent _stats = new()
        {
            BaseDamage     = 10f,
            AttackSpeed    = 1f,
            CritChance     = 0f,
            CritMultiplier = 1.5f,
        };

        protected override void OnInitialize()
        {
            _stats.BonusDamage = 0f;
            Add(_stats);
        }
    }
}
```

**Usage:** Add this component to the Player GameObject (alongside AttackBridge, MoveEntityBridge, etc.). Also add it to any enemy that can deal damage.

---

## Phase 9 — Dodge Block During Attack

### 9.1 Modify `DodgeSystem.cs`

**Path:** `Assets/_Project/Scripts/Character/Systems/DodgeSystem.cs`

**Changes:**
1. Before starting a dodge, check if the entity is attacking
2. If `IsAttacking` → block dodge entirely (no mid-attack cancel)
3. The player exits a combo by not pressing attack during the combo window — once the step finishes, `IsAttacking` goes false and dodge is available again

Since not all entities with dodge will have attack, use `world.TryGetComponent<AttackComponent>(entity, out var attack)` — a one-liner that combines IsAlive + HasComponent + GetComponent.

**Modify the dodge-start block** (around line 67–72). Change from:

```csharp
if (!input.Dodge || !moveState.IsGrounded || dodge.CooldownRemaining > 0f)
    continue;
```

To:

```csharp
if (!input.Dodge || !moveState.IsGrounded || dodge.CooldownRemaining > 0f)
    continue;

// Block dodge entirely while attacking
if (world.TryGetComponent<AttackComponent>(entity, out var attack) && attack.IsAttacking)
    continue;
```

This requires adding a using for:
```csharp
using Wander.Character.Components;
```
(AttackComponent is already in that namespace, and `Wander.Character.Events` is no longer needed here.)

---

## Phase 10 — Test Target (Manual in Unity)

### 10.1 Create Enemy Dummy Prefab

1. Create a new GameObject: "EnemyDummy"
2. Add components:
   - `EcsEntityRoot`
   - `HealthBridge` (set BaseHealth = 50 or whatever)
3. Add a child with a **Capsule** mesh (visual) and a **CapsuleCollider** (for hitbox detection)
4. Place it in the scene near the player

The hitbox detection works because `AttackBridge.OnTriggerEnter` looks for `EcsEntityRoot` on the collided object's parent. As long as the enemy has `EcsEntityRoot` + `HealthComponent`, it will receive damage.

### 10.2 Optional: Create `EnemyDummyBridge.cs`

**Not yet implemented** — create if you want the dummy to react visually to damage/death (e.g. change color, disable).

**Path:** `Assets/_Project/Scripts/Enemy/EnemyDummyBridge.cs`

```csharp
using System;
using HelloDev.Entities;
using UnityEngine;
using Wander.Character.Components;
using Wander.Character.Events;

namespace Wander.Enemy
{
    public class EnemyDummyBridge : EcsComponentBridge
    {
        [SerializeField] private Renderer _renderer;

        private IDisposable _damageSub;
        private MaterialPropertyBlock _propBlock;

        protected override void OnInitialize()
        {
            _propBlock = new MaterialPropertyBlock();

            _damageSub = World.Subscribe<DamageTakenEvent>(e =>
            {
                if (e.Entity != Entity) return;

                // Flash red
                if (_renderer != null)
                {
                    _propBlock.SetColor("_Color", Color.red);
                    _renderer.SetPropertyBlock(_propBlock);
                    // You could schedule a coroutine to reset color
                }

                if (e.RemainingHealth <= 0f)
                    Debug.Log($"[EnemyDummy] {gameObject.name} is dead!");
            });
        }

        protected override void OnDestroy()
        {
            _damageSub?.Dispose();
            base.OnDestroy();
        }
    }
}
```

---

## File Summary

| # | Action | Path | Phase | Status |
|---|--------|------|-------|--------|
| 1 | Create | `Scripts/Character/Attack/AttackInputType.cs` | 1 | ✅ Done |
| 2 | Create | `Scripts/Character/Attack/ComboStep.cs` | 1 | ✅ Done |
| 3 | Create | `Scripts/Character/Attack/ComboDefinition.cs` | 1 | ✅ Done |
| 4 | Create | `Scripts/Character/Attack/AttackAnimEventProxy.cs` | 1 | ✅ Done |
| 5 | Create | `Scripts/Character/Attack/IAttackAnimEventReceiver.cs` | 1 | ✅ Done |
| 6 | Create | `Scripts/Character/Components/CombatStatsComponent.cs` | 2 | ✅ Done |
| 7 | Create | `Scripts/Character/Components/AttackComponent.cs` | 2 | ⚠️ Needs `MaxSteps` field |
| 8 | Create | `Scripts/Character/Components/HealthComponent.cs` | 2 | ✅ Done |
| 9 | Create | `Scripts/Character/Events/AttackComboStartEvent.cs` | 3 | ✅ Done |
| 10 | Create | `Scripts/Character/Events/AttackStepStartedEvent.cs` | 3 | ✅ Done |
| 11 | Create | `Scripts/Character/Events/AttackEndedEvent.cs` | 3 | ✅ Done |
| 12 | Create | `Scripts/Character/Events/HitEvent.cs` | 3 | ✅ Done |
| 13 | Create | `Scripts/Character/Events/DamageTakenEvent.cs` | 3 | ✅ Done |
| 14 | Modify | `Scripts/Character/Components/MoveInputComponent.cs` | 4 | ✅ Done |
| 15 | Modify | `Scripts/Player/PlayerInputBridge.cs` | 4 | ✅ Done |
| 16 | Create | `Scripts/Character/Systems/AttackSystem.cs` | 5 | ⚠️ Needs `MaxSteps` bounds check |
| 17 | Create | `Scripts/Player/AttackBridge.cs` | 6 | ⚠️ Needs combo matching fix |
| 18 | Create | `Scripts/Player/CombatStatsBridge.cs` | 8 | ✅ Done |
| 19 | Create | `Scripts/Character/Systems/DamageSystem.cs` | 7 | ✅ Done |
| 20 | Create | `Scripts/Player/HealthBridge.cs` | 8 | ✅ Done |
| 21 | Modify | `Scripts/Character/Systems/DodgeSystem.cs` | 9 | ✅ Done |
| 22 | Create (optional) | `Scripts/Enemy/EnemyDummyBridge.cs` | 10 | ❌ Not created |

## Manual Unity Steps

| Step | Details |
|------|---------|
| Input Action Map | Add "LightAttack" and "HeavyAttack" actions in `PlayerInputActionMap.inputactions`, bind to desired buttons |
| Animator Controller | Add "AttackA" and "AttackB" empty states with placeholder clips |
| AttackAnimEventProxy | Add `AttackAnimEventProxy` component to the **Animator's GameObject** (the model child) |
| Animation Events | On each attack AnimationClip, add events: `OnHitboxActivate`, `OnHitboxDeactivate`, `OnComboWindowOpen`, `OnComboWindowClose` |
| Hitbox Collider | Add a trigger collider on the weapon/hand child of the player, assign to `AttackBridge._hitboxCollider` |
| ComboDefinition Assets | Create SO assets via Create → Wander → Combo Definition, fill in patterns + clips + DamageMultiplier |
| CombatStatsBridge | Add `CombatStatsBridge` to the Player prefab, set `BaseDamage` (e.g. 10) |
| Enemy Dummy Prefab | Create GameObject with EcsEntityRoot + HealthBridge + Capsule collider |

## Important Notes

- **Damage formula**: `finalDamage = (CombatStats.BaseDamage + CombatStats.BonusDamage) × ComboStep.DamageMultiplier`. Stats scale with progression; multipliers stay fixed per combo step. This decouples character growth from combo tuning.
- **Animation Events drive all timing**: Window flags (combo, hitbox) are NOT computed from timestamps. They are set by Animation Events on the clips, routed through `AttackAnimEventProxy` → `IAttackAnimEventReceiver` (AttackBridge). Dodge is blocked entirely while attacking — the player exits a combo by not pressing attack during the combo window. This means you **must** add the events to each attack clip for the system to work.
- **Unmanaged constraint**: ECS components are `unmanaged` structs — no arrays, strings, or class references. All managed data lives on bridges; bridges copy numeric values (duration, damage) into components.
- **No .meta files**: Unity auto-generates these. Never create them manually.
- **Bridge auto-registration**: `[RequiresSystem(typeof(X))]` on a bridge auto-registers that system. No need to manually add systems to `EcsEntityRoot.Systems` list.
- **GlobalSystems for bridgeless systems**: DamageSystem and other event-only systems (no bridge) should be added to the `GlobalSystems` list on `EcsSystemRunner` in the Inspector. This replaces the old workaround of piggybacking `[RequiresSystem]` on unrelated bridges.

---

## Framework Improvements (Post-Implementation)

The following framework additions were made while building the attack system:

### EcsManagedSystem

**Path:** `Assets/HelloDev/Entities/Runtime/Bridge/Entity/EcsManagedSystem.cs`

MonoBehaviour base class for systems that need Unity APIs (Animator, Collider, etc.). Lives on a GameObject like a bridge, but participates in the system execution pipeline. Has `Order`, `Entity`, `World`, and component helpers (`Get`, `Set`, `Add`, `Has`, `Modify`). Auto-discovered by `EcsEntityRoot` (same as bridges). Runs **after** pure systems, **before** `FlushEvents` each frame.

### ComponentScope / Modify\<T\>()

**Path:** `Assets/HelloDev/Entities/Runtime/Bridge/Entity/ComponentScope.cs`

`sealed class` (originally `ref struct`, changed to class because C# `using var` makes the variable readonly, preventing field mutation on value types — CS1654 error). Reads a component on creation and writes it back on `Dispose()`. Available as `Modify<T>()` on both `EcsComponentBridge` and `EcsManagedSystem`. Replaces the verbose Get/mutate/Set pattern:

```csharp
// Before:
var attack = Get<AttackComponent>();
attack.IsAttacking = true;
Set(attack);

// After:
using var scope = Modify<AttackComponent>();
scope.Value.IsAttacking = true;
```

### TryGetComponent

**Method on:** `EcsWorld`

Combines `IsAlive` + `HasComponent` + `GetComponent` in one call. Use for optional component checks in systems:

```csharp
if (world.TryGetComponent<AttackComponent>(entity, out var attack) && attack.IsAttacking)
    continue;
```

### GlobalSystems

**Field on:** `EcsSystemRunner`

`[SerializeReference] public List<EcsSystemBase> GlobalSystems` — Inspector-assignable list for systems that have no bridge (e.g. DamageSystem). Registered in `Awake` after world creation. Replaces the old workaround of using `EcsEntityRoot.Systems` or `[RequiresSystem]` on unrelated bridges.

### Dual-State Convention

Documented in `EcsComponentBridge` XML docs. ECS components hold state that systems read/write (unmanaged, deterministic). Bridge fields hold state that only Unity needs (managed references, visual state). If a system might need the data, put it in the component; otherwise keep it on the bridge.

---

## Required Code Changes (Bugs Found in Audit)

The following 3 files need updates to fix bugs discovered during plan audit. The code blocks in this plan already reflect the correct versions above.

### 1. `AttackComponent.cs` — Add `MaxSteps` field

Add `[NonSerialized] public int MaxSteps;` alongside the per-step data fields. This lets the system know how many steps the resolved combo has, preventing step overflow.

### 2. `AttackSystem.cs` — Add MaxSteps bounds check

Before advancing to `nextStep`, check `nextStep >= attack.MaxSteps`. If exceeded, end the attack immediately instead of firing `AttackStepStartedEvent` for an out-of-bounds step. This prevents the "dead time" bug where the character is locked after the final step while the system waits for the previous step's duration to expire.

### 3. `AttackBridge.cs` — Fix combo matching

Three sub-fixes:
1. **OnComboStart**: Remove `_inputHistory.Clear()` before `ResolveCombo()`. The history already contains the first input (tracked in `OnPushToEcs`). Without this, combo resolution always runs against empty history and picks the longest combo regardless of input type.
2. **OnStepStarted**: Re-resolve the combo on each step advancement. This enables combo branching — combos sharing a prefix (e.g. `[L,L,L]` and `[L,L,H]`) diverge when the player presses a different button. Also set `MaxSteps` on the component.
3. **ResolveCombo**: Remove the dead `int inputCount` parameter (was never used in the method body).

