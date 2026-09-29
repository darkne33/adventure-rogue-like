# Aura

`AbilityName.Aura` is a shared weapon available to every character after purchasing its quest unlock. Selecting it during a run creates a persistent green aura around the character. Each tick damages every living, active enemy within the horizontal radius and configured height tolerance. Leaving the radius stops subsequent aura damage.

## Configuration

Edit `Configs/Aura Ability Configuration.asset` to tune the ability:

| Parameter | Starting value | Common upgrade |
|---|---:|---:|
| Base damage per enemy per tick | 4 | +2 |
| Tick interval | 0.5 seconds | -0.05 seconds |
| Radius | 7 meters | +0.35 meters |

Damage rolls use 20% variation, followed by the existing character damage, critical-hit and relic modifiers. Hits also use the normal lifesteal, hit and kill events. Attack speed, relic attack speed and cooldown reduction affect the tick interval. The estimated DPS is for one enemy remaining inside the aura.

Upgrade amounts scale with the existing card rarity multiplier. Cards offer two distinct effects from damage, tick interval and radius. The base interval cannot fall below 0.15 seconds; at that limit, offers contain damage and radius. Global modifiers cannot reduce the effective interval below 0.05 seconds. Interval upgrades preserve the fraction of time remaining until the next tick.

## Prefab and loading

`Content/Aura/Aura.prefab` has an `AuraDamageArea` component on its root. The ability configuration references that component directly. Its serialized visual root points into the nested `MagicAuraGreen` prefab. The original vendor prefab and all existing asset GUIDs are retained; the local nested-instance ID is `100100001`, not the reserved prefab-asset ID.

The particle systems retain their original playback settings, including Looping. On creation, `AuraDamageArea.Initialize` calls `Play(true)` once on the root particle system to include its children. The aura is instantiated directly under the character root, with a local height offset of 0.15 meters. Local simulation and parenting make it follow the character without per-frame transform updates. The visual's authored radius is 1 meter, and the prefab's initial visual scale is 7. Initialization and radius upgrades adjust the visual scale to the damage radius, compensating for the character's scale at that time.

`AuraAbility` instantiates and initializes the prepared component, keeps the existing cooldown/upgrade logic, and calls `AuraDamageArea.DamageEnemies` on each tick. The component collects living, active enemies within the horizontal radius and height tolerance, then invokes the ability's normal damage calculation for each target. The first tick occurs immediately after selection and subsequent ticks use the configured interval. Enemy colliders are not changed. Damage ticks use the existing character ability loop, including its pause and transition handling; there is no second damage timer.

Unequipping disables and destroys the whole aura instance. Character destruction or scene unloading removes it with its owner; owner death also removes it. Radius upgrades update the existing effect and damage area together.

`AllAbilitiesConfiguration` registers the ability for gameplay. Its unlock entry in the Addressable `DemoProgressionConfiguration` references the ability configuration, which references the aura prefab and its dependencies. `GameplayAssetService` already awaits that root through `IAddressableLoadService` before gameplay and releases it with the service lifetime. No separate runtime asset loading is needed.

The card uses its own green energy-ring sprite, `Icons/AuraAbilityIcon.png`, referenced by the ability configuration and included through the same Addressables dependency chain.

## Unlock

The separate **DANGER ZONE** quest (`aura_close_kills_50`) requires defeating 50 enemies within 10 meters of the character in one run. It uses the existing `RunCloseRangeKills` metric, awards 2 silver, and enables purchasing `weapon_aura` for 3 persistent silver. Completing the quest alone does not grant ownership. The aura is excluded from the default ability list.
