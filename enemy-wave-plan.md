# Enemy Wave System — Implementation Plan

## Problem Statement

Add enemies that spawn in waves and attack the player. Maximize reuse of existing ECS components, systems, and bridges. The player already has a working movement + combo attack + dodge + health/damage pipeline.

## Reusability Audit

### Fully reusable as-is (no changes needed)

| Layer | Asset | Notes |
|-------|-------|-------|
| **Component** | `MovementStateComponent` | Pure runtime velocity/grounded/canMove |
| **Component** | `MovementStatsComponent` | Config: walkSpeed, runSpeed, gravity, etc. |
| **Component** | `PositionComponent` | float3 position |
| **Component** | `AttackComponent` | Combo state machine (runtime only) |
| **Component** | `CombatStatsComponent` | BaseDamage, AttackSpeed, etc. |
| **Component** | `HealthComponent` | BaseHealth, CurrentHealth, IsDead |
| **Component** | `MoveInputComponent` | Direction, Sprint, Jump, Dodge, AttackInput |
| **Component** | `AnimationStateComponent` | SpeedBlend, IsGrounded for Animator |
| **System** | `CharacterPhysicsSystem` | Reads MoveInput → writes MovementState |
| **System** | `AttackSystem` | Combo state machine, fires events |
| **System** | `DamageSystem` | HitEvent → HealthComponent subtraction |
| **System** | `AnimationStateSystem` | Movement → animation blend values |
| **System** | `DodgeSystem` | Optional for enemies |
| **Bridge** | `MoveEntityBridge` | CharacterController ↔ ECS translation |
| **Bridge** | `AnimationBridge` | AnimationStateComponent → Animator params |
| **Bridge** | `AttackBridge` | Combo resolution, hitbox, anim override |
| **Bridge** | `CombatStatsBridge` | Config provider for damage stats |
| **Bridge** | `HealthBridge` | Health init + damage feedback |

### Must create new

| Asset | Purpose |
|-------|---------|
| `AIInputBridge` | Replaces PlayerInputBridge — writes MoveInputComponent from AI logic |
| `EnemyDeathBridge` | Reacts to IsDead — disables/destroys enemy |
| `WaveDefinition` | ScriptableObject: enemy count, spawn delay, prefab |
| `WaveManager` | MonoBehaviour: spawns waves, tracks alive count, advances |

### Architecture

```
AIInputBridge (Push — replaces PlayerInputBridge)
  reads: target position, own position, attack range
  writes: MoveInputComponent (direction toward target, AttackInput when in range)
  ↓
CharacterPhysicsSystem (reused)  →  AttackSystem (reused)
  ↓                                    ↓
MoveEntityBridge (reused)         AttackBridge (reused)
  ↓                                    ↓
DamageSystem (reused)             HealthBridge (reused)
  ↓
EnemyDeathBridge (new — listens DamageTakenEvent, handles death)

WaveManager (standalone MonoBehaviour)
  reads: WaveDefinition[], tracks living enemies
  spawns prefabs, fires wave-complete logic
```

The key insight: **MoveInputComponent is the universal interface**. Player fills it from InputSystem, AI fills it from behavior logic. Everything downstream is identical.

---

## Phases

### Phase 1 — AIInputBridge

**Path:** `Assets/_Project/Scripts/Enemy/AIInputBridge.cs`

The AI equivalent of PlayerInputBridge. Each FixedUpdate it:
1. Finds/caches the player transform (or entity position)
2. Computes direction toward player
3. If distance ≤ attackRange → sets AttackInput = Light
4. If distance > attackRange → sets Direction toward player
5. Writes MoveInputComponent

Config fields (Inspector):
- `float DetectRange` — max distance to chase player
- `float AttackRange` — distance to start attacking
- `float AttackCooldown` — minimum time between attack inputs
- `Transform _target` — player transform (set by WaveManager on spawn)

No pathfinding (V1) — direct line-of-sight movement. NavMesh can be added later without touching systems.

### Phase 2 — EnemyDeathBridge

**Path:** `Assets/_Project/Scripts/Enemy/EnemyDeathBridge.cs`

Subscribes to `DamageTakenEvent`. When `RemainingHealth <= 0`:
- Disables the CharacterController
- Optionally plays a death animation trigger
- Fires `EnemyDiedEvent { Entity }` (new event, for WaveManager)
- Destroys the GameObject after a delay

### Phase 3 — EnemyDiedEvent

**Path:** `Assets/_Project/Scripts/Enemy/EnemyDiedEvent.cs`

Simple event struct with Entity field. WaveManager subscribes to track alive count.

### Phase 4 — WaveDefinition

**Path:** `Assets/_Project/Scripts/Enemy/WaveDefinition.cs`

ScriptableObject with:
- `string WaveName`
- `GameObject EnemyPrefab`
- `int EnemyCount`
- `float SpawnInterval` — seconds between spawns
- `float DelayBeforeWave` — seconds before first spawn
- `Transform[] SpawnPoints` — or use radius around a point

### Phase 5 — WaveManager

**Path:** `Assets/_Project/Scripts/Enemy/WaveManager.cs`

MonoBehaviour (scene singleton). Holds `WaveDefinition[]`. On Start:
1. Subscribes to `EnemyDiedEvent`
2. Starts wave 0 after initial delay
3. Spawns enemies at intervals
4. Sets each spawned enemy's AIInputBridge._target to the player
5. Tracks alive count; when 0 → next wave (or win)

### Phase 6 — Enemy Prefab Setup (Manual Unity)

Prefab structure:
```
EnemyRoot (GameObject)
  ├── EcsEntityRoot
  ├── MoveEntityBridge         (reused — set WalkSpeed lower than player)
  ├── AIInputBridge            (new — set DetectRange, AttackRange)
  ├── AttackBridge             (reused — assign enemy ComboDefinition[])
  ├── CombatStatsBridge        (reused — set BaseDamage for enemy)
  ├── HealthBridge             (reused — set BaseHealth e.g. 30)
  ├── AnimationBridge          (reused — connect to Animator)
  ├── EnemyDeathBridge         (new)
  ├── CharacterController
  └── Model (child)
      ├── Animator
      └── AttackAnimEventProxy (reused)
```

All bridges except AIInputBridge and EnemyDeathBridge are **identical** to the player's. Different values in Inspector (slower speed, less health, simpler combos).

---

## Component Reuse Scorecard

- **Components reused:** 8/8 (100%)
- **Systems reused:** 5/5 (100%)
- **Bridges reused:** 5/7 (71%) — only AIInputBridge and EnemyDeathBridge are new
- **New code:** ~200 lines across 4 files + 1 event struct
- **Lines of reused code leveraged:** ~1500+

This validates the ECS architecture's composability. The MoveInputComponent abstraction is the key decoupling point — swap the input source, reuse everything else.
