# Aura

`AbilityName.Aura` is a shared weapon available to every character after purchasing its quest unlock. Selecting it during a run creates a persistent green aura around the character. Each tick damages every living, active enemy within the horizontal radius and configured height tolerance. Leaving the radius stops subsequent aura damage.

## Configuration

Edit `Configs/Aura Ability Configuration.asset` to tune the ability:

| Parameter | Starting value | Common upgrade |
|---|---:|---:|
| Base damage per enemy per tick | 4 | +2 |
| Tick interval | 0.5 seconds | -0.05 seconds |
| Radius | 2.5 meters | +0.35 meters |

Damage rolls use 20% variation, followed by the existing character damage, critical-hit and relic modifiers. Hits also use the normal lifesteal, hit and kill events. Attack speed, relic attack speed and cooldown reduction affect the tick interval. The estimated DPS is for one enemy remaining inside the aura.

Upgrade amounts scale with the existing card rarity multiplier. Cards offer two distinct effects from damage, tick interval and radius. The base interval cannot fall below 0.15 seconds; at that limit, offers contain damage and radius. Global modifiers cannot reduce the effective interval below 0.05 seconds. Interval upgrades preserve the fraction of time remaining until the next tick.

## Prefab and loading

`Content/Aura/Aura.prefab` contains the existing `MagicAuraGreen` prefab as a nested instance. Its three particle systems use local simulation space so the entire effect follows the character. The original vendor prefab and its GUID are retained. The visual's authored radius is 1 meter, and runtime scaling matches the configured damage radius independently of character scale.

The visual is instantiated from that prepared prefab and parented to the character. Unequipping destroys it; character destruction or scene unloading removes it with its owner. Damage ticks are driven by the existing character ability loop and respect its pause handling.

`AllAbilitiesConfiguration` registers the ability for gameplay. Its unlock entry in the Addressable `DemoProgressionConfiguration` references the ability configuration, which references the aura prefab and its dependencies. `GameplayAssetService` already awaits that root through `IAddressableLoadService` before gameplay and releases it with the service lifetime. No separate runtime asset loading is needed.

The card uses its own green energy-ring sprite, `Icons/AuraAbilityIcon.png`, referenced by the ability configuration and included through the same Addressables dependency chain.

## Unlock

The separate **DANGER ZONE** quest (`aura_close_kills_50`) requires defeating 50 enemies within 2 meters of the character in one run. It uses the existing `RunCloseRangeKills` metric, awards 2 silver, and enables purchasing `weapon_aura` for 3 persistent silver. Completing the quest alone does not grant ownership. The aura is excluded from the default ability list.
