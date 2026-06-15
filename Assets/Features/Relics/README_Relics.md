# Relics System

Relics are passive run items. The system is data-driven: a designer adds a new `RelicDefinition` asset, assigns icon/effects/tags, and then adds it to `RelicPoolConfiguration`.

## Where Assets Live

- Definitions: `Assets/Features/Relics/Resources/Relics/Definitions`
- Icons: `Assets/Features/Relics/Resources/Relics/Icons`
- Pool config: `Assets/Features/Relics/Resources/Relics/RelicPoolConfiguration.asset`
- Chest config: `Assets/Features/Relics/Resources/Relics/RelicChestConfiguration.asset`

The rogue-like installer loads the pool and chest config from `Resources/Relics` if scene fields are not assigned.

## Adding A Relic

1. Create a `RelicDefinition` asset.
2. Fill `Id`, `DisplayName`, `Description`, `Rarity`, `Tags`, `Icon`, `MaxStacks`, and `IsUnique`.
3. Add one or more `RelicEffectDefinition` entries.
4. Add the asset to `RelicPoolConfiguration.Relics`.
5. Test in runtime with `debug.relic.give <id>` or `debug.relic.random`.

## Rarity

Rarity affects both UI color and roll weight:

- Common: green
- Uncommon: blue
- Rare: purple
- Legendary: gold

Weights are configured in `RelicPoolConfiguration`.

## Unlocks

If `UnlockQuestId` is empty, the relic is available from the start. If it is set, `RelicUnlockService` must know that quest as completed or the relic must be explicitly unlocked.

## Triggers

Supported v1 triggers:

- `PassiveStat`: applies stat modifiers on pickup and removes them on clear/remove.
- `OnHit`: processes chance/cooldown effects like Hex and Meteor.
- `OnKill`: processes scaling and crate-spawn effects.
- `OnDamageTaken`, `OnHeal`, `OnChestOpen`: events are wired for future effects.
- `OnFatalDamage`: can cancel fatal damage once and break the relic.

## Stacking

Non-unique relics stack until `MaxStacks`. Unique relics cannot be duplicated. Chance effects multiply their chance by stack count and clamp to 100%.

## Debug Commands

- `debug.relic.give <id>`
- `debug.relic.random`
- `debug.relic.clear`
- `debug.relics`

## Runtime Chest Flow

When an enemy room is completed, `RelicChestSpawner` rolls one available relic and spawns a chest in the room. Press `E` near the chest to open it. The chest animates, drops a 2D pixel relic pickup, and pressing `E` near the pickup adds the relic to the run inventory.
