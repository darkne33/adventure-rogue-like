# Demo quests and unlocks

The demo contains 24 quests and 24 purchasable unlocks: 2 characters, 1 weapon, 8 scrolls and 13 relics. Six relics are available from the start. Completing a quest makes its associated unlock available for purchase; the player must spend persistent silver before the content becomes usable. QUESTS uses one scrollable list with a Hide completed filter; UNLOCKS uses category tabs and an eight-column grid. Both windows use the project's own sprites and a selected-entry detail panel.

## Configuration

Edit `Assets/Features/Quests/Configs/DemoProgressionConfiguration.asset` in the Inspector. Its `ProgressionConfiguration` ScriptableObject is registered in Addressables and loaded by `GameplayAssetService` during `BootstrapState`, before quest services are initialized or gameplay scenes are loaded.

- **Default Characters / Abilities / Relics** control what is available on a new save, independently of purchases.
- **Quests** contain a stable ID, title, condition text, category, metric, target and one-time silver reward. Optional `Legacy Quest Ids` preserve completed requirements and claimed rewards when a condition is replaced.
- **Unlocks** contain a stable ID, category, character ID or ability/relic reference, required quest ID and silver cost. Optional name, description and icon override the referenced asset. `Unlocked By Default` can make an individual catalog entry immediately owned.
- **Fallback Content Icons** contain currency and fallback portrait references. Character portraits also use the existing character-selection portrait cache. Window appearance is authored in prefabs.

Keep quest and unlock IDs stable: saves refer to these IDs. Turbo Skates and Sacrificial Dagger retain their catalog entries and quest IDs, but are now owned from the start; their quests still award silver. Wallet and Lump of Coal are purchasable for 2 silver each without a quest requirement. Content absent from both the default arrays and the unlock catalog is excluded from normal demo selection and reward pools. The fortune wheel can offer a locked relic of the required rarity if eligible unlocked relics run out; winning it does not purchase its collection entry. Character-specific starting weapons remain upgradeable after selecting an owned character; character weapon exclusions still apply.

## Starting content

- Character: RABBIT.
- Weapons: Rabbitarang, Fireball and Earth Rock. Rabbitarang is RABBIT's starting weapon.
- Scrolls: Damage, Armor, Attack Speed, Crit Damage and Crit Chance.
- Relics: Hot Dog and Venom Blade (green), Iron Hammer and Cupid's Arrow (blue), Turbo Skates and Sacrificial Dagger (purple). No legendary relic is unlocked by default. These are six available collection entries, not six items granted to the run inventory.

Bullet Explosion and Punch are their characters' signature weapons. They become available with those characters and are not separate purchases.

## Progress, purchases and silver

- Single-run goals store the best result reached in one run; partial results from different runs are not added together.
- Lifetime goals accumulate across runs, including lost runs.
- Combat time counts only active, unfinished combat rooms. Pauses, transitions, cleared rooms and safe rooms do not advance it.
- Boss victories count completed boss rooms, so a splitting boss grants one victory.
- Collected gold includes gold already spent. Starting relics do not count as pickups.
- Quest silver is collected once with CLAIM in QUESTS after the quest is completed. Viewing the quest does not claim its reward. Every positive silver gain in the run wallet is also credited to the persistent wallet immediately, including final pickup callbacks after death. Ending a run does not deposit the same silver again.
- Progress, completed quest IDs, purchased unlock IDs, claimed reward IDs, viewed unlock IDs and persistent silver are saved together under the existing PlayerPrefs key `little_rush.quests.v1` (payload version 3). Existing progress and balances are retained; completed quests from older saves are marked as claimed because their silver was already awarded automatically. Old completed quest IDs do not automatically count as purchases.
- Completion, purchases and collected silver save immediately. Unfinished progress also saves periodically and at run/lifecycle boundaries.
- `QuestService` is the ownership authority for character selection, upgrade offers and relic pools. A completed quest alone does not bypass the purchase requirement. The old relic-specific `UnlockQuestId` / `UnlockCost` fields are not the demo's purchase configuration.

## Quest conditions and unlock prices

Targets, quest silver rewards and purchase prices are editable in the configuration. Quest silver is claimed after completion; the purchase price is the separate amount spent to own the unlocked content. Existing purchase IDs remain stable. Four older relic conditions are replaced with thematic goals, with completed requirements migrated as described below.

| Quest ID | Condition | Purchasable unlock | Quest silver | Price in silver |
|---|---|---|---:|---:|
| run_kills_150 | Defeat 150 enemies in one run. | DUKE | 2 | 5 |
| rooms_3 | Clear 3 combat rooms in one run. | MR POCKET | 2 | 8 |
| weapon_5 | Raise any weapon to level 5 in one run. | Fire Trail | 2 | 3 |
| level_5 | Reach character level 5 in one run. | Max HP Scroll | 1 | 2 |
| combat_120 | Spend 120 seconds in active combat in one run. | Movement Speed Scroll | 1 | 2 |
| chests_1 | Open a relic chest. | Luck Scroll | 1 | 3 |
| scroll_5 | Raise any scroll to level 5 in one run. | Duration Scroll | 2 | 3 |
| rooms_1 | Clear one combat room in a run. | Evasion Scroll | 1 | 2 |
| total_gold_500 | Collect 500 gold across all runs. Spent gold still counts. | Gain Gold Scroll | 2 | 3 |
| total_kills_500 | Defeat 500 enemies across all runs. | Thorns Damage Scroll | 2 | 3 |
| level_10 | Reach character level 10 in one run. | Shield Scroll | 2 | 4 |
| run_kills_50 | Defeat 50 enemies in one run. | Cactus | 1 | 2 |
| run_gold_100 | Collect 100 gold in one run. Spent gold still counts. | Golden Boot | 1 | 3 |
| combat_distance_500 | Travel 500 m during active combat in one run. | Turbo Skates (owned from the start) | 3 | — |
| low_health_room | Clear a combat room entered with 25% HP or less. | Voodoo Doll | 3 | 5 |
| armor_scroll_3 | Raise the Armor Scroll to level 3 in one run. | Spiky Shield | 1 | 3 |
| chests_8 | Open 8 relic chests across all runs. | Overpowered Chalice | 2 | 6 |
| projectile_targets_3 | Damage 3 different enemies with one projectile, including piercing and native boomerang chains. | Rubber Cement | 1 | 3 |
| moving_room | Clear a combat room without standing still for more than 2 consecutive seconds. | Aquarius | 1 | 3 |
| run_hits_300 | Land 300 damaging hits in one run. | Soy Milk | 2 | 4 |
| gold_held_150 | Hold 150 gold at the same time in a run. | Money = Power | 2 | 4 |
| burst_kills_3 | Defeat 3 enemies within 2 seconds of active combat in the same room. | Explosivo | 2 | 5 |
| hit_damage_75 | Deal 75 damage in one hit, including critical hits and overkill. | Polyphemus | 2 | 5 |
| close_kills_30 | Defeat 30 enemies within 2 m of the character in one run. | Sacrificial Dagger (owned from the start) | 2 | — |
| — | No quest requirement. | Wallet | — | 2 |
| — | No quest requirement. | Lump of Coal | — | 2 |

Quest categories follow their rewards: Characters for DUKE and MR POCKET, Weapons for Fire Trail, Scrolls for all eight scrolls and Relics for all thirteen purchasable relics. Fire Trail is the display name of the existing Fire Field ability asset (`AbilityName.FireField`).

## Relic challenge tracking

`QuestRunTracker` owns subscriptions and run/activity filtering. `RelicQuestRunProgress` owns the added per-run and per-room counters. New metric enum values are appended; existing values and saved metric names remain unchanged.

- Movement uses actual horizontal displacement during unfinished combat rooms. A speed of at least 0.1 m/s counts as moving. Pauses and transitions suspend position sampling and the stationary timer; displacements over 15 m are ignored as teleports. Distance resets each run, while the best result is persisted.
- Aquarius fails the current room after a stop strictly longer than 2 seconds. Voodoo Doll snapshots health when entering a fresh combat room, so getting hurt just before the last kill cannot satisfy its requirement. Cleared rooms cannot be revisited to earn either condition; a room must have had active combat or a counted defeat.
- Explosivo's rolling kill window uses active combat time and resets between rooms. Enemy-provider defeat events count each removed, defeated enemy once and include kills caused by poison, water and explosions. The final enemy is counted before room-completion processing.
- Dagger progress checks the distance between the enemy and character transforms at the defeat event. No particular weapon is required.
- Soy Milk and Polyphemus use successful `RelicHitEvent` events. Bomb/water secondary damage does not emit hit events, so it cannot inflate hit totals. Polyphemus uses full hit damage, including overkill; zero-applied-damage hits do not progress either challenge.
- Each flying projectile's existing `PlayerCollisionDetector` owns its distinct damaged-target set. `SingleShootAbility` forwards the count in `RelicHitEvent.ProjectileDistinctTargets`. Repeated hits on one target and hits from separate projectiles cannot combine toward Rubber Cement. The set lasts only for that projectile and clears on initialization.
- Money = Power records maximum gold currently held; purchases do not count as held gold. Golden Boot retains its original cumulative gold-collected-in-a-run condition. Spiky Shield tracks the Armor Scroll's selected build level specifically.

## Existing save compatibility

Purchase IDs and the PlayerPrefs save key/payload version remain unchanged, preserving owned relics and silver. Completed old requirements map to their replacements through `Legacy Quest Ids`:

| Previous quest | Replacement |
| --- | --- |
| `two_weapons_3` | `combat_distance_500` |
| `bosses_1` | `low_health_room` |
| `critical_25` | `armor_scroll_3` |
| `combat_300` | `chests_8` |

An old completed requirement preserves purchase eligibility. Its claimed status transfers, preventing a second silver reward; an unclaimed reward remains claimable on the replacement quest, with the same amount. Partial progress in a replaced, unrelated metric does not become progress in the new condition. Migration does not replay completion notifications or grant purchases. The current default relic list applies to both new and existing saves; purchased unlocks remain owned. Wallet and Lump of Coal are no longer defaults and now have their own purchase entries.

## UI prefabs

New quest completions display a non-interactive dark/gold notification at the top of the screen, slightly right of center. It shows the quest title and condition, silver available to claim in QUESTS, and any content now available to buy in UNLOCKS. Notifications appear in order, hold for four seconds, then fade out. Their animation uses unscaled time, and the project-level queue survives scene changes so a completion just before death is still shown. Existing completed quests are not replayed when loading a save.

Edit `Assets/Features/Quests/Prefabs/QuestCompletionNotification.prefab` to style the card. The prefab is loaded through Addressables during bootstrap and its handle is retained for the project lifetime. `QuestCompletionNotificationController` owns the queue and timing; `QuestCompletionNotificationView` only binds data and presentation. `SoundId.QuestComplete` uses a quiet version of the existing start-click cue through `SoundsCatalog`, respecting the player's SFX volume and mute settings.

Edit these assets directly in Prefab Mode:

- `Assets/Features/Quests/Prefabs/QuestsPanel.prefab` — full window, header, Hide completed button, scroll viewport, completion summary, footer and CLAIM button.
- `Assets/Features/Quests/Prefabs/QuestRow.prefab` — checkbox, condition, progress, reward icon, claimable-reward marker and selection corners.
- `Assets/Features/Quests/Prefabs/UnlocksPanel.prefab` — full window, four tabs, grid, currency and purchase details.
- `Assets/Features/Quests/Prefabs/UnlockCell.prefab` — item icon, price, new-unlock marker and selection corners. State colors are serialized on its view component.
- `Assets/Features/Quests/Prefabs/ProgressionAlertYellow.prefab` — yellow pixel exclamation mark for main-menu buttons and UNLOCKS category tabs.
- `Assets/Features/Quests/Prefabs/ProgressionAlertBlue.prefab` — blue pixel exclamation mark for individual unlocks and claimable quest rewards.

Both alert prefabs use a non-interactive Unity UI Image with the `Sprites/ProgressionExclamation.png` sprite (an unchanged copy of the existing `Retro Arsenal/Textures/Icons/icon_exclamation.png` artwork, imported as a single Sprite with point filtering). Their size, tint and unscaled pulse/tilt are authored in prefabs; serialized dependencies are loaded with the existing Addressables-loaded main menu. No new runtime asset loading is needed. UNLOCKS and category markers indicate unseen, quest-eligible content regardless of the current silver balance. Selecting an unlock's details marks it viewed and saves that state. QUESTS and quest-row markers remain until the corresponding silver is claimed; a zero-silver quest never creates a claim marker.

`MainMenuPanel.prefab` references the two window prefabs. The view scripts instantiate the assigned window and row/cell prefabs and bind data; they do not construct UI hierarchies or add UI components at runtime. The iron frame textures already included in the project are arranged as editable RawImage edge/corner pieces, so their original SpriteImporter settings remain unchanged.

The former QUESTS `SilverBalance` belongs to `MainMenuPanel.prefab` and appears at the top left, mirrored from its original top-right position. It stays visible over QUESTS and UNLOCKS, updates when `QuestService.Changed` fires, hides during character selection, and reappears when returning to the main menu.

## Controls

Mouse clicks select entries and UNLOCKS tabs; the mouse wheel scrolls without changing the selected quest or footer. Hide completed filters finished quests out of the list while keeping any unclaimed silver rewards visible. Select a completed quest and press CLAIM to collect its silver; the button is also reachable with keyboard/controller navigation. Navigation selects entries and keeps them visible; Escape/B or the close control returns to the main menu. Locked unlock entries show silhouettes; quest-complete entries show their price and a blue marker until first viewed; owned entries show their full-color icon. The detail panel shows the quest requirement, purchase action or owned state.
