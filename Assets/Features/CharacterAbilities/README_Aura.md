# Aura

`AbilityName.Aura` is a shared weapon available to every character after purchasing its quest unlock. Selecting it during a run creates a persistent green aura around the character. Each tick damages every living, active enemy within the horizontal radius and configured height tolerance. Leaving the radius stops subsequent aura damage.

## Configuration

Edit `Configs/Aura Ability Configuration.asset` to tune the ability:

| Parameter | Starting value | Common upgrade |
|---|---:|---:|
| Base damage per enemy per tick | 4 | +2 |
| Tick interval | 0.5 seconds | -0.05 seconds |
| Radius | 6 meters | +0.35 meters |

Damage rolls use 20% variation, followed by the existing character damage, critical-hit and relic modifiers. Hits also use the normal lifesteal, hit and kill events. Attack speed, relic attack speed and cooldown reduction affect the tick interval. The estimated DPS is for one enemy remaining inside the aura.

Upgrade amounts scale with the existing card rarity multiplier. Cards offer two distinct effects from damage, tick interval and radius. The base interval cannot fall below 0.15 seconds; at that limit, offers contain damage and radius. Global modifiers cannot reduce the effective interval below 0.05 seconds. Interval upgrades preserve the fraction of time remaining until the next tick.

## Prefab and loading

`Content/Aura/Aura.prefab` has an `AuraDamageArea` component on its root. The ability configuration references that component directly. Its serialized visual root references the `OctaRingYellow` effect inside the aura prefab.

On creation, `AuraDamageArea.Initialize` sets simulation speed and calls `Play(true)` once on the root particle system to include its children. Before every damage tick, all particle systems under the visual root receive `simulationSpeed = 0.25 / effectiveTickInterval`: 0.5 seconds gives speed 0.5, 0.25 seconds gives speed 1, and 1 second gives speed 0.25. The interval includes attack speed, relic attack speed and cooldown reduction. This updates playback speed without restarting the effect or changing its authored Looping, lifetime or duration settings.

The aura is instantiated directly under the character root at local Y = 0 and follows it through parenting, without per-frame transform updates. Initialization and radius upgrades adjust only X and Z to the damage radius, preserving the prefab's authored Y scale and compensating for the character's scale at that time.

`AuraAbility` instantiates and initializes the prepared component, keeps the existing cooldown/upgrade logic, and calls `AuraDamageArea.DamageEnemies` on each tick. The component collects living, active enemies within the horizontal radius and height tolerance, then invokes the ability's normal damage calculation for each target. The first tick occurs immediately after selection and subsequent ticks use the configured interval. Enemy colliders are not changed. Damage ticks use the existing character ability loop, including its pause and transition handling; there is no second damage timer.

Unequipping disables and destroys the whole aura instance. Character destruction or scene unloading removes it with its owner; owner death also removes it. Radius upgrades update the existing effect and damage area together.

`AllAbilitiesConfiguration` registers the ability for gameplay. Its unlock entry in the Addressable `DemoProgressionConfiguration` references the ability configuration, which references the aura prefab and its dependencies. `GameplayAssetService` already awaits that root through `IAddressableLoadService` before gameplay and releases it with the service lifetime. No separate runtime asset loading is needed.

The UI uses the golden pixel-art energy-ring sprite on a transparent background, `Icons/AuraAbilityIcon.png`, referenced by the ability configuration and included through the same Addressables dependency chain. Upgrade cards, build slots, quest rewards and unlocks share this icon. Its existing `.meta` and GUID are preserved when replacing the image.

## Unlock

The separate **DANGER ZONE** quest (`aura_close_kills_50`) requires defeating 50 enemies within 10 meters of the character in one run. It uses the existing `RunCloseRangeKills` metric, awards 2 silver, and enables purchasing `weapon_aura` for 3 persistent silver. Completing the quest alone does not grant ownership. The aura is excluded from the default ability list.
