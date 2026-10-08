# Relics System

Relics are passive run items. The system is data-driven: a designer adds a new `RelicDefinition` asset, assigns icon/effects/tags, and then adds it to `RelicPoolConfiguration`.

## Where Assets Live

- Definitions: `Assets/Features/Relics/Configs/Definitions`
- Icons: `Assets/Features/Relics/Sprites`
- Pool config: `Assets/Features/Relics/Configs/RelicPoolConfiguration.asset`
- Chest config: `Assets/Features/Relics/Configs/RelicChestConfiguration.asset`
- Pickup and inventory UI prefabs: `Assets/Features/Relics/Prefabs`

`GameplayAssetService` preloads the pool and chest configs through Addressables during bootstrap. The rogue-like installer uses these loaded assets when scene overrides are not assigned. Definitions, icons, and prefabs are serialized dependencies of the loaded content; no Resources fallback is used.

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

`QuestService` controls ownership through `DemoProgressionConfiguration.asset`. Its default collection contains two green relics (Hot Dog, Venom Blade), two blue relics (Iron Hammer, Cupid's Arrow), two purple relics (Turbo Skates, Sacrificial Dagger), and no legendary relics. These defaults apply to new and existing saves without adding items to the run inventory. The other thirteen relics are purchasable for persistent silver; Wallet and Lump of Coal cost 2 silver each without a quest requirement, and the other eleven require their configured quest. All nineteen definitions are present in progression as either defaults or purchasable entries. The old relic-specific `UnlockQuestId` / `UnlockCost` fields do not control this catalog. See `Assets/Features/Quests/README_Quests.md` for conditions, prices and save migration.

The fortune wheel first picks an eligible unlocked relic of the requested rarity. If none is left for that set, it may use a locked relic of the same rarity through `RelicPool.GetAvailable(..., includeLocked: true)`. This wheel-only fallback still respects run inventory stack limits and does not permanently unlock the relic. Different relics are preferred within a set; other reward sources continue to exclude locked relics.

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

When an enemy room is completed, `RelicChestSpawner` spawns a chest if at least one relic is available. Press `E` near the chest to start at the lowest available rarity (normally Common). Relics of that rarity cycle above the chest while slowly rising, then the chest can upgrade through Uncommon, Rare, and Legendary using the chances in `RelicChestConfiguration`. Every successful upgrade pumps the chest and recolors its treasure effects. The final relic is revealed, flies to the character, activates, and disappears before the coin fountain is disabled.

## Standalone Relic Room

`OnlyRelicRewardRoom.prefab` uses its own `OnlyRelicRoomData`, with the authored `RelicSpawnPoint` assigned in the prefab. `LevelRoomCatalog.OnlyRelicRooms` is a separate room pool. It follows the base Rebirth Treasure Room availability: one on floors 1–6, none on later floors. The room uses `Item_Door` on its entrance and return path, which opens on entry. Existing serialized `RoomType` values remain unchanged; `OnlyRelic` is value 6. See `PROCEDURAL_LEVELS.md` for the supported room mappings and generation rules.

`OnlyRelicRoomSpawner` creates one `OnlyRelicPickup.prefab` on the first visit. It calls the same rarity-upgrade roll in `RelicChestRollService` as a chest, using the unlocked pool, current inventory and shared reward reservations. The prefab enables smooth vertical levitation and automatic pickup within `RelicChestConfiguration.RelicPickupDistance`, including the character pickup-range multiplier. Pickup is limited to the current room, flies to the character and opens the existing take/skip relic description panel. A consumed pickup completes the room and never respawns on revisits, including when the player skips the offered relic. Uncollected rewards retain their reservations until their pickup is destroyed or the floor changes.

The pickup prefab is a serialized dependency of the existing Addressable chest configuration; the room is a dependency of the room catalog, and the crown sprite is assigned to the minimap room-icon prefab. They follow the existing asynchronous `IAddressableLoadService` ownership and release paths without extra runtime loads. Existing prefab GUIDs and the authored spawn point are preserved.

Discovered relic rooms display `MinimapRelicCrownIcon.png`: a gold pixel-art crown with a dark outline and transparent background. The sprite was generated with the built-in ImageGen tool on 2026-10-06. Prompt: "Use case: stylized-concept. Asset type: tiny 2D game minimap sprite for a treasure/relic room. Create one simple golden crown icon inspired by the readable treasure-room minimap symbols of The Binding of Isaac. Straight-on view, symmetrical crown with three clear points, broad flat base, chunky dark brown almost-black outline, flat warm gold fill, small pale yellow highlight, restrained orange shading. Authentic clean low-resolution pixel-art silhouette: looks designed on a 24 by 24 pixel grid, upscale with crisp hard pixel edges. No jewels, no letters, no ornament, no sparkles, no ground shadow, no surrounding border, no scene, no drop shadow. The single crown occupies 80 percent of a square canvas, centered, with even transparent padding. Actual transparent alpha background. Clear enough to remain recognizable at 30 pixels."

## Blood Room and Room Doors

`BloodRewardRoom.prefab` is a copy of `OnlyRelicRewardRoom.prefab`, with the same environment and authored reward point. Its `BloodRoomData` selects one reward on the first visit: the existing combat `RewardBag` with 60% probability, one existing relic chest with 20%, or one standalone relic pickup with 20%. The weights (60/20/20) are serialized in the room prefab. `BloodRoomSpawner` uses the existing reward spawners, relic pool, and shared relic reservations. The rolled type is retained if its spawn cannot complete; a successfully spawned reward stays in the room and never duplicates on revisits.

`LevelRoomCatalog.BloodRooms` contains the copied prefab. Blood follows the base Rebirth Curse Room roll: 50% on floors 1–9, at most one per floor, and none thereafter. The project has no Devil Room visit state or corresponding chance modifier. Its internal 60/20/20 reward weights remain serialized in the prefab. `RoomType.Blood` is value 7. The return path opens on entry.

The generator first grows a connected floor plan, then converts existing dead ends into special rooms, ordered by distance from the start. Optional rooms are not appended as additional branches. Chest rooms correspond to Vaults and replace eligible fortune-wheel/Arcade rooms; these two types are mutually exclusive. Floor size and the special-room rolls are retained throughout geometry retries.

`RoomDoor.prefab` contains configured `DoorView` references for the ordinary combat door and all five new models. Both sides of a passage use the special room's door: `Item_Door` for standalone relics, `Treasure_Door` for chest rooms, `Shop_Door` for the fortune wheel, `Boss_Door` for bosses (including the floor exit), and `Blood_Door` for Blood rooms. Opening hides the two authored leaves and the treasure door's lock; highlighting uses the prefab's assigned outline.

The Blood room and door prefabs are serialized dependencies of the existing Addressable room catalog and level configuration. Their materials, models, and reward prefabs follow the existing asynchronous `IAddressableLoadService` loading and release lifetimes. Existing asset GUIDs are preserved; the copied Blood room has its own `.meta` and GUID.

## Isaac-inspired build items

The pool contains the original ten relics and these nine additions. Values are adaptations for Little Rush and live in the definition assets.

| ID | Rarity / maximum stacks | Effect |
| --- | --- | --- |
| `soy_milk` | Rare / unique | Damage x0.25, attack frequency x4. |
| `polyphemus` | Rare / unique | Damage x3, attack frequency x0.45. |
| `cupids_arrow` | Uncommon / unique | Flying projectiles pierce enemies. |
| `rubber_cement` | Uncommon / 3 | Three additional enemy/wall ricochets per stack. With Cupid's Arrow, enemies are pierced and walls cause bounces. |
| `explosivo` | Rare / 3 | 15% on-hit chance per stack. Attach a bomb for 1.2 seconds; blast damage is 24 per stack + 50% of the triggering hit, radius 2.5 m. Six charges per target, 64 globally. No player damage. |
| `aquarius` | Rare / 3 | Moving in combat leaves a patch every meter, lasting 3 seconds; radius 0.85 m. Each patch deals 6 damage per stack every 0.4 seconds before modifiers. Maximum 12 live patches. |
| `lump_of_coal` | Common / 5 | +6% damage per meter of actual projectile travel per stack, capped at +150%. Aquarius uses current owner-to-enemy distance instead. |
| `sacrificial_dagger` | Rare / 3 | One orbiting dagger per stack; radius 2 m, 180 degrees/sec, base damage 12. Per-target hit interval 0.3 seconds before attack-speed modifiers. Blocks enemy projectiles. |
| `money_equals_power` | Uncommon / 3 | +0.5% damage per currently held gold per stack, capped at +100%; spending gold reduces the bonus. |

Soy Milk and Polyphemus multiply each other and the existing damage/attack-speed bonuses. Their damage multiplier is applied before critical hits. They affect every ability: standard cooldowns, shot/punch series intervals, Earth Rock rotation and respawns, Fire Field damage intervals, Aquarius damage intervals, and dagger hit intervals. Damage has the existing one-point floor; continuous damage intervals have a 0.05-second floor. The fixed portion of Explosivo's proc damage remains fixed; its hit-dependent portion inherits the triggering hit's modifiers.

Piercing/ricochet flight is shared by Fireball, Bullet Explosion and Rabbit Boomerang. The boomerang retains its native chain targeting. Earth Rock, Punch, Fire Field and orbiting daggers do not receive projectile piercing or ricochets. Coal tracks traveled path, including ricochets, rather than straight-line distance from the player.

### Builds with the existing relics

- **Frequent procs:** Soy Milk + Explosivo + Overpowered Chalice. Every direct hit rolls independently; Chalice can attach additional bombs on the same hit, within the charge limit. Venom Blade adds poison to this route.
- **Heavy hits:** Polyphemus + Iron Hammer + Venom Blade. Larger hits feed both poison and Explosivo's hit-dependent damage; the lower attack frequency remains a tradeoff.
- **Ricochet shots:** Cupid's Arrow + Rubber Cement + Lump of Coal. Shots pierce groups, bounce off walls and gain damage along their traveled path. A wall bounce allows another hit on a previously pierced enemy, with a short repeat-hit guard.
- **Movement:** Aquarius + Lump of Coal + Turbo Skates. Leave damaging water behind while moving; Coal rewards creating distance, while Skates accelerates ordinary attacks and dagger contacts.
- **Close combat:** Sacrificial Dagger + Venom Blade/Explosivo + Spiky Shield. Dagger hits use the usual critical-hit, lifesteal and on-hit pipeline. Voodoo Doll still triggers self-damage and can activate Cactus; blocking a projectile with the dagger does not count as taking damage.
- **Economy:** Money = Power + Golden Boot + Wallet. Held gold supplies a live damage bonus alongside Wallet's existing chest-based growth.
- **Kill sustain:** Hot Dog can heal on kills caused by daggers, water or bombs, using the existing kill event.

Bombs and water publish kill events but do not publish hit events. They cannot recursively attach bombs or trigger further on-hit chains. Water applies dynamic damage bonuses without rolling Iron Hammer. Dagger hits are direct hits and can proc on-hit relics.

### Runtime ownership and assets

The new mechanics live in `Scripts/Runtime/` as independent plain C# classes. `RelicManager.Builds.cs` has been replaced by composition, rather than further partial declarations of the manager. The replacement `RelicBuildRuntime.cs` retains the original script's `.meta` GUID.

| Class | Responsibility |
| --- | --- |
| `RelicBuildRuntime` | Composes the new mechanics and forwards tick, movement, successful effect triggers, inventory removal, room changes and disposal. Contains no item formulas or physics queries. |
| `ExplosivoRelicRuntime` | Owns attached charges, target tracking, limits, fuse timing and detonation. |
| `AquariusRelicRuntime` | Owns water patches, ground detection, lifetime, range and damage intervals. |
| `SacrificialDaggerRelicRuntime` | Owns orbiting daggers, projectile blocking and per-target hit timers. |
| `RelicBuildModifiers` | Calculates Soy Milk/Polyphemus multipliers, piercing/ricochet parameters and Coal/Money = Power bonuses. |
| `RelicCombatService` | Applies direct and periodic damage, critical hits, lifesteal and combat events. Explicit callbacks connect it to the existing outgoing-damage and area-damage pipelines. |
| `RelicEffectCollection<T>` | Releases owned pooled visuals on expiry/removal/clear. A version counter lets mechanics stop iteration when combat callbacks remove their instances. |
| `RelicProjectileContext` / `IRelicProjectileContext` | Supplies flight-specific modifiers, room/lifetime state and ricochet visuals. `RelicProjectileFlight` depends on this interface instead of `RelicManager`. |
| `RelicBuildEffects` | Defines the new effect IDs, lookup rules and special-passive classification. |

`RelicManager` remains the inventory and event entry point. It owns one `RelicBuildRuntime` and forwards lifecycle calls; the new runtimes have no reference to the manager. Chance, cooldown and Chalice rolls still occur in the manager before successful triggers are forwarded. Existing relic handlers stay in `RelicManager.Items.cs`. Coal/Money = Power calculations are delegated from the same place in the damage loop, preserving additive order and proc rolls. These classes require no GameObject components or additional scene/installer bindings.

Each mechanic owns its own instance collection. Pooled visuals are released on relic removal, inventory clear, character loss/death, room start and manager disposal. Modified projectile flights retain room/owner/manager lifetimes, a finite distance budget and a maximum 20-second lifetime. Effects use scaled game time.

All five new effects are prepared prefabs in `Effects/`, registered in `Effects Config.asset` and the Default Local Addressables group. They use the existing `EffectsService` warmup and `IAddressableLoadService` path; no runtime objects or components are assembled. Definitions and icons remain serialized dependencies of the preloaded relic pool.

| Prepared prefab | Retro Arsenal source under `Assets/third-party/Retro Arsenal/Prefabs/` |
| --- | --- |
| `RelicExplosivoChargeFx` | `Interactive/Loot/Obtainables/Orbs/Small/OrbSmallRed.prefab` |
| `RelicExplosivoExplosionFx` | `Combat/Explosions/Fire/FireExplosion.prefab` |
| `RelicAquariusTrailFx` | `Interactive/Zone/Round/RoundZoneBlue.prefab` |
| `RelicSacrificialDaggerFx` | `Interactive/Loot/Obtainables/Orbs/Small/OrbSmallPurple.prefab`, with a serialized dagger SpriteRenderer child |
| `RelicRicochetFx` | `Combat/Sword/SwordHit/SwordHitBlue.prefab` |

These are feature-owned copies with new GUIDs; original vendor assets and their material references are preserved. Each copy has an `EffectPlayer`, disabled play-on-awake and no automatic particle destruction. Charge/dagger particles follow their local transform. The pool owns transient impacts; each relic runtime owns its persistent visuals.

Icon prompts and source references are recorded in [ICON_PROMPTS.md](ICON_PROMPTS.md). Blue, green and purple pixel outlines match the existing rarity palette.

### Tuning fields

Special effects dispatch through `EffectPrefabId`. `Value`/`BossValue` hold damage/attack-frequency factors for Soy Milk and Polyphemus. For Explosivo they hold flat damage per stack / triggering-hit fraction. Aquarius uses `Value` for tick damage, `BossValue` for tick interval, `Duration` for lifetime, `Radius` for radius and `Cap` for patch count. Dagger uses `Value` for damage, `BossValue` for contact radius, `Duration` for hit interval, `Radius` for orbit radius and `Cap` for angular speed. Coal and Money = Power use `Value` for the additive factor and `Cap` for its total limit.

This addition was authored as file changes only; Unity import, compilation, Play Mode and automated tests have not been run.
