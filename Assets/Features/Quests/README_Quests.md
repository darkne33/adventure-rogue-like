# Demo quests and unlocks

The demo uses staged quests to unlock characters, weapons, scrolls and relics. Five relics are available from the start. Completing a quest makes its associated unlock available for purchase; the player must spend persistent silver before the content becomes usable. QUESTS uses one scrollable list ordered by progression stage, with a Hide completed filter; UNLOCKS uses category tabs and an eight-column grid. Both windows use the project's own sprites and a selected-entry detail panel.

## Configuration

Edit `Assets/Features/Quests/Configs/DemoProgressionConfiguration.asset` in the Inspector. Its `ProgressionConfiguration` ScriptableObject is registered in Addressables and loaded by `GameplayAssetService` during `BootstrapState`, before quest services are initialized or gameplay scenes are loaded.

- **Default Characters / Abilities / Relics** control what is available on a new save, independently of purchases.
- **Quests** contain a stable ID, title, condition text, category, metric, target and one-time silver reward. `Minimum Completed Runs`, `Required Quest Ids` and `Required Unlock Ids` define progression stages and branches. Optional `Character Id` restricts progress to that character; `Ability` identifies the asset tracked by `SpecificAbilityLevel`. Optional `Legacy Quest Ids` preserve completed requirements and claimed rewards when a condition is replaced.
- **Unlocks** contain a stable ID, category, character ID or ability/relic reference, required quest ID and silver cost. Optional name, description and icon override the referenced asset. `Unlocked By Default` can make an individual catalog entry immediately owned.
- **Fallback Content Icons** contain currency and fallback portrait references. Character portraits also use the existing character-selection portrait cache. Window appearance is authored in prefabs.

Keep quest and unlock IDs stable: saves refer to these IDs, even when an old numerical ID no longer describes the tuned condition. Turbo Skates retain their catalog entry and quest ID, but are owned from the start; their quest still awards silver. Wallet and Lump of Coal are purchasable for 2 silver each without a quest requirement. Content absent from both the default arrays and the unlock catalog is excluded from normal demo selection and reward pools. The fortune wheel only offers owned relics. It can repeat an eligible owned relic of the required rarity if distinct options run out; if no eligible owned relic exists, the slot shows the existing empty reward. Character-specific starting weapons remain upgradeable after selecting an owned character; character weapon exclusions still apply.

## Starting content

- Character: RABBIT.
- Weapons: Rabbitarang, Fireball and Earth Rock. Rabbitarang is RABBIT's starting weapon.
- Scrolls: Damage, Armor, Attack Speed, Crit Damage and Crit Chance.
- Relics: Hot Dog and Venom Blade (green), Iron Hammer and Cupid's Arrow (blue), Turbo Skates (purple). No legendary relic is unlocked by default. These are five available collection entries, not five items granted to the run inventory.

Bullet Explosion and Punch are their characters' signature weapons. They become available with those characters and are not separate purchases.

## Progress, purchases and silver

- Eligibility is snapshotted at the start of each run, using completed run count, completed prerequisite quests, owned prerequisite unlocks and the selected character. Completing or purchasing a prerequisite makes the next step eligible for a subsequent run, never retroactively for the current or previous runs.
- Single-run goals store the best result reached in one eligible run; partial results from different runs are not added together. Ability goals use the selected build's level for the configured weapon or scroll.
- Lifetime goals accumulate across eligible runs, including lost runs. Each quest has its own progress, so earlier totals cannot bypass a newly opened branch.
- A run counts toward `Minimum Completed Runs` once it ends after clearing at least one combat room. Death, retry, returning to the menu and shutdown use the same idempotent end path. Empty starts and deaths before clearing a room do not count. Pausing or saving progress does not end a run.
- Combat time counts only active, unfinished combat rooms. Pauses, transitions, cleared rooms and safe rooms do not advance it.
- Boss victories count completed boss rooms, so a splitting boss grants one victory.
- Collected gold includes gold already spent. Starting relics do not count as pickups.
- Completing a quest immediately makes its associated unlock purchasable in UNLOCKS. Buying it makes the content available in the game independently of CLAIM. Quest silver is an additional reward collected once with CLAIM in QUESTS after completion; claiming does not purchase content, and buying content does not claim the silver. Viewing the quest does not claim its reward. Every positive silver gain in the run wallet is also credited to the persistent wallet immediately, including final pickup callbacks after death. Ending a run does not deposit the same silver again.
- Global metric totals, individual quest progress, completed quest IDs, purchased unlock IDs, claimed reward IDs, viewed unlock IDs and persistent silver are saved together under the existing PlayerPrefs key `little_rush.quests.v1` (payload version 4). Purchases, completions and balances are retained. Pre-v4 global partial progress is imported only for ungated goals; gated and replaced goals start recording eligible progress under the new rules. Pre-v3 completed quests are marked as claimed because their silver was already awarded automatically. Old completed quest IDs do not automatically count as purchases.
- Completion, purchases and collected silver save immediately. Unfinished progress also saves periodically and at run/lifecycle boundaries.
- `QuestService` is the ownership authority for character selection, upgrade offers and relic pools. A completed quest alone does not bypass the purchase requirement. The old relic-specific `UnlockQuestId` / `UnlockCost` fields are not the demo's purchase configuration.

## Quest conditions and unlock prices

Targets, stage gates, quest silver rewards and purchase prices are editable in the configuration. Quest silver is claimed after completion; the purchase price is the separate amount spent to own the unlocked content. Purchase prices, starting content and all current quest/purchase IDs remain stable. Existing special-performance conditions have been replaced with ordinary build, room and boss goals; this catalog does not require low-health starts, uninterrupted movement, timed kill chains or restricted runs.

The pacing target for a new save is an early wave during the first 2–3 runs, the main branches over roughly 10–15 runs, and major rewards after boss victories and branch completion. These are design targets, not measured playtest results. A stage number below is the earliest eligible run, assuming all prerequisites and purchases are already satisfied; it does not guarantee completion on that run.

| Earliest run | Quest ID | Condition and prerequisites | Purchasable unlock | Quest silver | Price |
|---:|---|---|---|---:|---:|
| 1 | rooms_1 | Clear 3 combat rooms in one run. | Evasion Scroll | 1 | 2 |
| 1 | run_kills_50 | Defeat 75 enemies in one run. | Cactus | 1 | 2 |
| 2 | level_5 | Reach character level 7 in one run. | Max HP Scroll | 1 | 2 |
| 2 | run_gold_100 | Collect 150 gold in one run, including spent gold. | Golden Boot | 1 | 3 |
| 3 | weapon_5 | Raise any weapon to level 6 in one run. | Fire Trail | 2 | 3 |
| 3 | combat_120 | Spend 180 seconds in active combat in one run. | Movement Speed Scroll | 1 | 2 |
| 3 | chests_1 | Open 3 relic chests across eligible runs. | Luck Scroll | 1 | 3 |
| 4 | run_kills_150 | Complete `weapon_5` and `level_5`, then defeat a boss in an eligible run. | DUKE | 3 | 5 |
| 4 | armor_scroll_3 | Own Max HP Scroll; raise Armor Scroll to level 5 in one run. | Spiky Shield | 1 | 3 |
| 4 | projectile_targets_3 | Complete `weapon_5`; raise Rabbitarang to level 6 in one run. | Rubber Cement | 1 | 3 |
| 4 | total_gold_500 | Own Golden Boot; collect 2000 gold across eligible runs, including spent gold. | Gain Gold Scroll | 2 | 3 |
| 4 | combat_distance_500 | Own Movement Speed Scroll; travel 1500 m during active combat in one run. | Turbo Skates (already owned) | 3 | — |
| 5 | scroll_5 | Own Fire Trail; raise it to level 5 in one run. | Duration Scroll | 2 | 3 |
| 5 | moving_room | Own Movement Speed Scroll; raise it to level 5 in one run. | Aquarius | 1 | 3 |
| 6 | aura_close_kills_50 | Own DUKE; raise Bullet Explosion to level 5 as DUKE in one run. | Aura | 2 | 3 |
| 6 | level_10 | Complete `armor_scroll_3` and own Max HP Scroll; raise Max HP Scroll to level 5 in one run. | Shield Scroll | 2 | 4 |
| 7 | total_kills_500 | Own Spiky Shield; raise Armor Scroll to level 7 in one run. | Thorns Damage Scroll | 2 | 3 |
| 7 | run_hits_300 | Complete `projectile_targets_3` and own Duration Scroll; raise Attack Speed Scroll to level 6 in one run. | Soy Milk | 2 | 4 |
| 8 | gold_held_150 | Own Gain Gold Scroll; raise it to level 5 in one run. | Money = Power | 2 | 4 |
| 9 | rooms_3 | Complete `aura_close_kills_50` and `armor_scroll_3`, own DUKE; defeat 2 bosses as DUKE across eligible runs. | MR POCKET | 4 | 8 |
| 10 | low_health_room | Own MR POCKET; clear 24 combat rooms as MR POCKET across eligible runs. | Voodoo Doll | 3 | 5 |
| 10 | burst_kills_3 | Complete `run_hits_300` and own DUKE; raise Bullet Explosion to level 7 as DUKE in one run. | Explosivo | 3 | 5 |
| 11 | chests_8 | Complete the Fire Trail, Armor and Gain Gold mastery quests (`scroll_5`, `armor_scroll_3`, `gold_held_150`); defeat 2 bosses across eligible runs. | Overpowered Chalice | 4 | 6 |
| 12 | hit_damage_75 | Complete `chests_8` and own MR POCKET; defeat a boss as MR POCKET in an eligible run. | Polyphemus | 4 | 5 |
| 1 | — | No quest requirement. | Wallet | — | 2 |
| 1 | — | No quest requirement. | Lump of Coal | — | 2 |

The main branches are Fire Trail → Duration → Soy Milk, Max HP/Armor → Spiky Shield → Shield/Thorns, Golden Boot → Gain Gold → Money = Power, and DUKE → Aura → MR POCKET. Rubber Cement and movement mastery provide additional related goals. The three mastered branches converge on Overpowered Chalice, followed by the MR POCKET boss victory for Polyphemus.

Quest categories follow their rewards: Characters for DUKE and MR POCKET, Weapons for Fire Trail and Aura, Scrolls for all eight scrolls and Relics for relic quests. Fire Trail is the display name of the existing Fire Field ability asset (`AbilityName.FireField`). Aura (`AbilityName.Aura`) is shared by all characters and follows the normal quest-then-purchase flow.

## Metric tracking

`QuestRunTracker` owns subscriptions, run/activity filtering, selected-character eligibility and exact ability-level reporting. `QuestService` snapshots eligible quests and owns their saved progress. Boss and total-room counters use a set of completed room instances to prevent repeat events counting twice. New metric enum values are appended; existing values and saved metric names remain unchanged.

- Movement uses actual horizontal displacement during unfinished combat rooms. A speed of at least 0.1 m/s counts as moving. Pauses and transitions suspend position sampling and the stationary timer; displacements over 15 m are ignored as teleports. Distance resets each run, while the best result is persisted.
- Golden Boot counts all gold collected in an eligible run, including gold already spent. Gain Gold accumulates gold after its prerequisites are satisfied.
- Ability mastery uses the configured `AbilityConfiguration.AbilityName` and `UpgradeBuildEntry.Level`; other weapons or scrolls cannot satisfy that goal. Spiky Shield retains the existing specific Armor Scroll metric.
- DUKE and MR POCKET goals use the character selected at run start. Clearing a boss room, rather than individual boss parts, supplies a boss victory.
- Existing low-health, movement-restriction, burst-kill, close-range, distinct-projectile-target and hit-strength metrics remain available for save compatibility, but are not used as unlock conditions by this catalog.

## Existing save compatibility

Purchase IDs and the PlayerPrefs save key remain unchanged, preserving owned relics and silver. Payload v4 adds per-quest progress and completed-run totals. All current completed quests remain completed under their stable IDs, including conditions now replaced or made harder. Existing claimed/unclaimed rewards keep their status; rewards already claimed are never granted again. Old saves have no completed-run counter, so remaining staged goals begin accumulating that counter from zero without removing any owned content. Older completed requirements still map through `Legacy Quest Ids`:

| Previous quest | Replacement |
| --- | --- |
| `two_weapons_3` | `combat_distance_500` |
| `bosses_1` | `low_health_room` |
| `critical_25` | `armor_scroll_3` |
| `combat_300` | `chests_8` |

An old completed requirement preserves purchase eligibility. Its claimed status transfers, preventing a second silver reward; an unclaimed reward remains claimable at the configured reward amount. Partial pre-v4 global progress is not imported into gated goals or unrelated replacement metrics. Migration does not replay completion notifications or grant purchases. The current default relic list applies to both new and existing saves; purchased unlocks remain owned. Wallet and Lump of Coal retain their own purchase entries.

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

Quest and unlock detail text shows the next unmet run, quest or purchase prerequisite and the number of additional prerequisites still needed. Compact rows keep the main condition; once prerequisites are met, the selected detail panel explains that progress starts in the next eligible run. The existing detail labels use prefab-authored TMP auto-sizing to fit the longer requirement text without creating new UI objects.

`MainMenuPanel.prefab` references the two window prefabs. The view scripts instantiate the assigned window and row/cell prefabs and bind data; they do not construct UI hierarchies or add UI components at runtime. The iron frame textures already included in the project are arranged as editable RawImage edge/corner pieces, so their original SpriteImporter settings remain unchanged.

The former QUESTS `SilverBalance` belongs to `MainMenuPanel.prefab` and appears at the top left, mirrored from its original top-right position. It stays visible over QUESTS and UNLOCKS, updates when `QuestService.Changed` fires, hides during character selection, and reappears when returning to the main menu.

## Controls

Mouse clicks select entries and UNLOCKS tabs; the mouse wheel scrolls without changing the selected quest or footer. The quest checkbox is checked only after CLAIM collects its silver reward; its progress bar hides at completion independently of CLAIM. The reward marker stays visible until the additional silver is claimed. Quest details distinguish content available to buy in UNLOCKS, owned content, and silver available to claim or already claimed. Hide completed filters finished quests out of the list while keeping any unclaimed silver rewards visible. Select a completed quest and press CLAIM to collect its silver; the button is also reachable with keyboard/controller navigation. Navigation selects entries and keeps them visible; Escape/B or the close control returns to the main menu. Locked unlock entries show silhouettes; quest-complete entries show their price and a blue marker until first viewed; owned entries show their full-color icon. The detail panel shows the quest requirement, purchase action or owned state.
