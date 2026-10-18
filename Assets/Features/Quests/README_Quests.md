# Demo quests and unlocks

The demo uses quests with one or two independent conditions to unlock characters, weapons, scrolls and relics. Five relics are available from the start. Completing a quest makes its associated unlock available for purchase; the player must spend persistent silver before the content becomes usable. QUESTS uses one scrollable list in configuration order, with a Hide completed filter; UNLOCKS uses category tabs and an eight-column grid. Both windows use the project's own sprites and a selected-entry detail panel.

## Configuration

Edit `Assets/Features/Quests/Configs/DemoProgressionConfiguration.asset` in the Inspector. Its `ProgressionConfiguration` ScriptableObject is registered in Addressables and loaded by `GameplayAssetService` during `BootstrapState`, before quest services are initialized or gameplay scenes are loaded.

- **Default Characters / Abilities / Relics** control what is available on a new save, independently of purchases.
- **Quests** contain a stable ID, title, full condition text, category, metric, target and one-time silver reward. There are no minimum-run requirements or prerequisite-quest chains. Optional `Character Id` restricts gameplay progress to that character; `Ability` identifies the asset tracked by `SpecificAbilityLevel`. Optional `Legacy Quest Ids` preserve completed requirements and claimed rewards when a condition is replaced. Disabled entries remain configured for compatibility and do not appear as active quests.
- Optional **Secondary Metric / Secondary Target** add an independent lifetime metric condition; a zero secondary target disables it. Alternatively, **Secondary Unlock Id** requires ownership of one unlock. Configure at most one secondary condition, so a quest has no more than two conditions. The full description states both conditions. Paired goals show completed conditions out of `DisplayTarget` (2); single goals show their metric progress against the main target.
- **Unlocks** contain a stable ID, category, character ID or ability/relic reference, required quest ID and silver cost. Optional `Granted With Unlock Id` makes an entry a companion reward granted in the same save snapshot when that source unlock is newly purchased; companion rewards cannot be purchased separately. Optional name, description and icon override the referenced asset. `Unlocked By Default` can make an individual catalog entry immediately owned.
- **Fallback Content Icons** contain currency and fallback portrait references. Character portraits also use the existing character-selection portrait cache. Window appearance is authored in prefabs.

Keep quest and unlock IDs stable: saves refer to these IDs, even when an old numerical ID no longer describes the tuned condition. Turbo Skates retain their catalog entry and quest ID, but are owned from the start; their quest still awards silver. Wallet and Lump of Coal are purchasable for 2 silver each without a quest requirement. Content absent from both the default arrays and the unlock catalog is excluded from normal demo selection and reward pools. The fortune wheel only offers owned relics. It can repeat an eligible owned relic of the required rarity if distinct options run out; if no eligible owned relic exists, the slot shows the existing empty reward. Character-specific starting weapons remain upgradeable after selecting an owned character. Once granted, Bullet Explosion and Punch can also appear in upgrade offers for every character; other configured character weapon exclusions still apply.

## Starting content

- Character: RABBIT.
- Weapons: Rabbitarang, Fireball and Earth Rock. Rabbitarang is RABBIT's starting weapon.
- Scrolls: Damage, Armor, Attack Speed, Crit Damage and Crit Chance.
- Relics: Hot Dog and Venom Blade (green), Iron Hammer and Cupid's Arrow (blue), Turbo Skates (purple). No legendary relic is unlocked by default. These are five available collection entries, not five items granted to the run inventory.

Bullet Explosion and Punch have separate Weapons entries in UNLOCKS. Their respective character quests show only DUKE and MR POCKET in reward icons, descriptions and completion notifications. Newly purchasing DUKE grants `weapon_bullet_explosion`; newly purchasing MR POCKET grants `weapon_punch`. These weapons become available to all characters without an extra silver payment. Completing the quest alone only enables the character purchase. The character and weapon ownership IDs are saved together. Loading an older save in which a character is already owned does not grant its shared weapon retroactively; that character retains its own starting weapon.

## Progress, purchases and silver

- All quests begin recording gameplay progress without waiting for completed runs, other quests or purchases. Optional character restrictions use the character selected at run start.
- A quest has one main objective and, optionally, one independent metric or ownership condition. Both conditions count in either order, in the same run or different runs. An ownership condition never blocks gameplay progress; buying the required unlock completes the quest immediately if its gameplay objective is already satisfied.
- DUKE's FIRST VICTORY (`run_kills_150`) requires reaching character level 7 and defeating one boss, with any character. Its progress shows completed conditions out of 2. The quest awards 3 silver, and DUKE costs 5 silver.
- Single-run goals store the best result reached in one run; partial results from different runs are not added together. This still applies to each main objective even when its second condition is completed in another run. Ability goals use the selected build's level for the configured weapon or scroll.
- Lifetime goals accumulate across runs, including lost runs. Each quest retains its own progress. Saved global totals and best results also restore progress for goals without a character restriction, except `SpecificAbilityLevel` goals, which retain their per-ability quest progress.
- Combat time counts only active, unfinished combat rooms. Pauses, transitions, cleared rooms and safe rooms do not advance it.
- Boss victories count completed boss rooms, so a splitting boss grants one victory.
- Collected gold includes gold already spent. Starting relics do not count as pickups.
- Completing a quest immediately makes its associated unlock purchasable in UNLOCKS. Buying it makes the content available in the game independently of CLAIM. Quest silver is an additional reward collected once with CLAIM in QUESTS after completion; claiming does not purchase content, and buying content does not claim the silver. Viewing the quest does not claim its reward. Every positive silver gain in the run wallet is also credited to the persistent wallet immediately, including final pickup callbacks after death. Ending a run does not deposit the same silver again.
- Global metric totals, individual quest progress, completed quest IDs, purchased unlock IDs, claimed reward IDs, viewed unlock IDs and persistent silver are saved together under the existing PlayerPrefs key `little_rush.quests.v1` (payload version 4). Purchases, completions, claims and balances are retained. Global progress is restored only for goals with no `Character Id` and a main metric other than `SpecificAbilityLevel`; general totals cannot be attributed to a particular character or ability. Pre-v3 completed quests are marked as claimed because their silver was already awarded automatically. Old completed quest IDs do not automatically count as purchases.
- Completion, purchases and collected silver save immediately. Unfinished progress also saves periodically and at run/lifecycle boundaries.
- `QuestService` is the ownership authority for character selection, upgrade offers and relic pools. A completed quest alone does not bypass the purchase requirement. The old relic-specific `UnlockQuestId` / `UnlockCost` fields are not the demo's purchase configuration.

## Editor shortcut

Use `Tools > Little Rush > Progression > Complete All Quests and Unlock All` to mark every configured quest completed and grant ownership of every unlock without spending silver. The command immediately saves through `QuestService` using the existing PlayerPrefs payload. Outside Play Mode it loads the configuration through `AssetDatabase` and updates the editor's saved progression; in Play Mode it updates the initialized project service and refreshes bound UI through `Changed`. Quest silver rewards retain their CLAIM status, and bulk completion does not queue completion notifications. This shortcut is editor-only.

## Quest conditions and unlock prices

Targets, optional second conditions, quest silver rewards and purchase prices are editable in the configuration. Quest silver is claimed after completion; the purchase price is the separate amount spent to own the unlocked content. Purchase prices, starting content and all current quest/purchase IDs remain stable. Existing special-performance conditions have been replaced with ordinary build, room and boss goals; this catalog does not require low-health starts, uninterrupted movement, timed kill chains or restricted runs.

All conditions are listed below. Paired conditions can be completed independently and in either order. THICK SKIN (`armor_scroll_3`) and ARMORED RESPONSE (`total_kills_500`) remain disabled in the configuration; their conditions are documented without making them active.

| Conditions | Quest ID | Full conditions | Purchasable unlock | Quest silver | Price |
|---:|---|---|---|---:|---:|
| 1 | rooms_1 | Clear 3 combat rooms in one run. | Evasion Scroll | 1 | 2 |
| 1 | run_kills_50 | Defeat 75 enemies in one run. | Cactus | 1 | 2 |
| 1 | level_5 | Reach character level 7 in one run. | Max HP Scroll | 1 | 2 |
| 1 | run_gold_100 | Collect 150 gold in one run, including spent gold. | Golden Boot | 1 | 3 |
| 1 | weapon_5 | Raise any weapon to level 6 in one run. | Fire Trail | 2 | 3 |
| 1 | combat_120 | Spend 180 seconds in active combat in one run. | Movement Speed Scroll | 1 | 2 |
| 1 | chests_1 | Open 3 relic chests across runs. | Luck Scroll | 1 | 3 |
| 2 | run_kills_150 | Reach character level 7 and defeat one boss, in either order, in the same or different runs. | DUKE + Bullet Explosion (shared weapon included) | 3 | 5 |
| 2 | armor_scroll_3 (disabled) | Raise Armor Scroll to level 5 in one run; own Max HP Scroll. | Spiky Shield | 1 | 3 |
| 1 | projectile_targets_3 | Raise Rabbitarang to level 6 in one run. | Rubber Cement | 1 | 3 |
| 2 | total_gold_500 | Collect 2000 gold across runs, including spent gold; own Golden Boot. | Gain Gold Scroll | 2 | 3 |
| 2 | combat_distance_500 | Travel 1500 m during active combat in one run; own Movement Speed Scroll. | Turbo Skates (already owned) | 3 | — |
| 1 | scroll_5 | Raise Fire Trail to level 5 in one run. | Duration Scroll | 2 | 3 |
| 1 | moving_room | Raise Movement Speed Scroll to level 5 in one run. | Aquarius | 1 | 3 |
| 1 | aura_close_kills_50 | Raise Bullet Explosion to level 5 as DUKE in one run. | Aura | 2 | 3 |
| 1 | level_10 | Raise Max HP Scroll to level 5 in one run. | Shield Scroll | 2 | 4 |
| 2 | total_kills_500 (disabled) | Raise Armor Scroll to level 7 in one run; own Spiky Shield. | Thorns Damage Scroll | 2 | 3 |
| 2 | run_hits_300 | Raise Attack Speed Scroll to level 6 in one run; own Duration Scroll. | Soy Milk | 2 | 4 |
| 1 | gold_held_150 | Raise Gain Gold Scroll to level 5 in one run. | Money = Power | 2 | 4 |
| 1 | rooms_3 | Defeat 2 bosses as DUKE across runs. | MR POCKET + Punch (shared weapon included) | 4 | 8 |
| 1 | low_health_room | Clear 24 combat rooms as MR POCKET across runs. | Voodoo Doll | 3 | 5 |
| 1 | burst_kills_3 | Raise Bullet Explosion to level 7 as DUKE in one run. | Explosivo | 3 | 5 |
| 1 | chests_8 | Defeat 2 bosses across runs. | Overpowered Chalice | 4 | 6 |
| 1 | hit_damage_75 | Defeat a boss as MR POCKET. | Polyphemus | 4 | 5 |
| — | — | No quest requirement. | Wallet | — | 2 |
| — | — | No quest requirement. | Lump of Coal | — | 2 |

Quest categories follow their rewards: Characters for DUKE and MR POCKET, Weapons for Fire Trail and Aura, Scrolls for all eight scrolls and Relics for relic quests. Fire Trail is the display name of the existing Fire Field ability asset (`AbilityName.FireField`). Aura (`AbilityName.Aura`) is shared by all characters and follows the normal quest-then-purchase flow.

## Metric tracking

`QuestRunTracker` owns subscriptions, run/activity filtering, selected-character filtering and exact ability-level reporting. `QuestService` selects character-compatible quests at run start and owns their saved progress. Ownership conditions are checked when completing a quest, independently of gameplay recording. Boss and total-room counters use a set of completed room instances to prevent repeat events counting twice. New metric enum values are appended; existing values and saved metric names remain unchanged.

- Movement uses actual horizontal displacement during unfinished combat rooms. A speed of at least 0.1 m/s counts as moving. Pauses and transitions suspend position sampling and the stationary timer; displacements over 15 m are ignored as teleports. Distance resets each run, while the best result is persisted.
- Golden Boot counts all gold collected in a run, including gold already spent. Gain Gold's quest accumulates gold before or after Golden Boot is purchased; the purchase is its independent second condition.
- Ability mastery uses the configured `AbilityConfiguration.AbilityName` and `UpgradeBuildEntry.Level`; other weapons or scrolls cannot satisfy that goal. Spiky Shield retains the existing specific Armor Scroll metric.
- Character-restricted goals use the character selected at run start; FIRST VICTORY can be completed with any character. Clearing a boss room, rather than individual boss parts, supplies a boss victory.
- Existing low-health, movement-restriction, burst-kill, close-range, distinct-projectile-target and hit-strength metrics remain available for save compatibility, but are not used as unlock conditions by this catalog.

## Existing save compatibility

FIRST VICTORY retains ID `run_kills_150`. Its two-condition rule uses the existing saved global boss and character-level metrics, without changing the save format. Historical progress counts toward both conditions, and existing DUKE quest completions, character purchases and claimed rewards retain their status.

Purchase IDs and the PlayerPrefs save key remain unchanged, preserving owned relics and silver. The existing payload version 4 remains in use, including per-quest progress and historical completed-run totals. Completed-run totals do not gate quests. All current completed quests remain completed under their stable IDs, including conditions now replaced or made harder. Existing claimed/unclaimed rewards keep their status; rewards already claimed are never granted again. Older completed requirements still map through `Legacy Quest Ids`:

| Previous quest | Replacement |
| --- | --- |
| `two_weapons_3` | `combat_distance_500` |
| `bosses_1` | `low_health_room` |
| `critical_25` | `armor_scroll_3` |
| `combat_300` | `chests_8` |

An old completed requirement preserves purchase eligibility. Its claimed status transfers, preventing a second silver reward; an unclaimed reward remains claimable at the configured reward amount. Saved global totals and best results restore the main objective only when its `Character Id` is empty and its metric is not `SpecificAbilityLevel`, without reducing existing per-quest progress. Character-restricted and specific-ability goals retain their own saved quest progress; general totals cannot establish which character or ability produced them. A secondary metric uses its saved lifetime progress, while a secondary ownership condition uses saved purchases. Existing completions are not replayed as notifications, and migration does not grant purchases. The current default relic list applies to both new and existing saves; purchased unlocks remain owned. Wallet and Lump of Coal retain their own purchase entries.

## UI prefabs

New quest completions display a non-interactive dark/gold notification at the top of the screen, slightly right of center. It shows the quest title and condition, silver available to claim in QUESTS, and any content now available to buy in UNLOCKS. Notifications appear in order, hold for four seconds, then fade out. Their animation uses unscaled time, and the project-level queue survives scene changes so a completion just before death is still shown. Existing completed quests are not replayed when loading a save.

Edit `Assets/Features/Quests/Prefabs/QuestCompletionNotification.prefab` to style the card. The prefab is loaded through Addressables during bootstrap and its handle is retained for the project lifetime. `QuestCompletionNotificationController` owns the queue and timing; `QuestCompletionNotificationView` only binds data and presentation. `SoundId.QuestComplete` uses a quiet version of the existing start-click cue through `SoundsCatalog`, respecting the player's SFX volume and mute settings.

Edit these assets directly in Prefab Mode:

- `Assets/Features/Quests/Prefabs/QuestsPanel.prefab` — full window, header, Hide completed button, scroll viewport, completion summary, footer and CLAIM button.
- `Assets/Features/Quests/Prefabs/QuestRow.prefab` — checkbox, condition, progress, single or paired reward icons, claimable-reward marker and selection corners.
- `Assets/Features/Quests/Prefabs/UnlocksPanel.prefab` — full window, four tabs, grid, currency and purchase details.
- `Assets/Features/Quests/Prefabs/UnlockReveal.prefab` — modal purchase reveal with a dark backdrop, gold title and rotating rays, large item/character icon, name, description and CONTINUE button. Its layout and animation settings are editable in the prefab.
- `Assets/Features/Quests/Prefabs/UnlockCell.prefab` — item icon, price, new-unlock marker and selection corners. State colors are serialized on its view component.
- `Assets/Features/Quests/Prefabs/ProgressionAlertYellow.prefab` — yellow pixel exclamation mark for main-menu buttons and UNLOCKS category tabs.
- `Assets/Features/Quests/Prefabs/ProgressionAlertBlue.prefab` — blue pixel exclamation mark for individual unlocks and claimable quest rewards.

Both alert prefabs use a non-interactive Unity UI Image with the `Sprites/ProgressionExclamation.png` sprite (an unchanged copy of the existing `Retro Arsenal/Textures/Icons/icon_exclamation.png` artwork, imported as a single Sprite with point filtering). Their size, tint and unscaled pulse/tilt are authored in prefabs; serialized dependencies are loaded with the existing Addressables-loaded main menu. No new runtime asset loading is needed. UNLOCKS and category markers indicate unseen, quest-eligible content regardless of the current silver balance. Selecting an unlock's details marks it viewed and saves that state. QUESTS and quest-row markers remain until the corresponding silver is claimed; a zero-silver quest never creates a claim marker.

Quest rows, selected quest details and locked unlock details show the full condition text. Paired goals show completed conditions out of 2; single goals retain their metric progress. No conditions are hidden behind a shortened prerequisite summary. The existing detail labels use prefab-authored TMP auto-sizing to fit the full requirement text without creating new UI objects.

`MainMenuPanel.prefab` references the two window prefabs. The view scripts instantiate the assigned window and row/cell prefabs and bind data; they do not construct UI hierarchies or add UI components at runtime. The iron frame textures already included in the project are arranged as editable RawImage edge/corner pieces, so their original SpriteImporter settings remain unchanged.

Every successful purchase in UNLOCKS opens the purchase reveal, including zero-cost purchases. Failed purchases do not open it. `UnlocksPanel.prefab` references the reveal prefab, so the existing Addressables-loaded main menu includes it and its serialized dependencies in the same loading lifetime. Character reveals reuse the menu's prepared portrait and portrait material. The existing purchase confirmation sound plays once; closing uses the existing UI close sound. The underlying unlock window cannot receive clicks, navigation or tab shortcuts while the reveal is visible. Appearance and closing use unscaled DOTween animations; a short input delay prevents the purchase press from immediately dismissing the reveal. CONTINUE/Submit or Escape/B closes the reveal and restores focus to the purchased cell. Hiding the unlock window stops and hides the reveal as well.

The former QUESTS `SilverBalance` belongs to `MainMenuPanel.prefab` and appears at the top left, mirrored from its original top-right position. It stays visible over QUESTS and UNLOCKS, updates when `QuestService.Changed` fires, hides during character selection, and reappears when returning to the main menu.

## Controls

Mouse clicks select entries and UNLOCKS tabs; the mouse wheel scrolls without changing the selected quest or footer. The quest checkbox is checked only after CLAIM collects its silver reward; its progress bar hides at completion independently of CLAIM. The reward marker stays visible until the additional silver is claimed. Quest rows and details show both the character and its companion weapon as separate icons; the detail panel and completion notification list both rewards. Quest details distinguish content available to buy in UNLOCKS, companion weapons included with a new character purchase, owned content, and silver available to claim or already claimed. Companion weapon cells have no price or purchase button; their detail panel names the source character. Hide completed filters finished quests out of the list while keeping any unclaimed silver rewards visible. Select a completed quest and press CLAIM to collect its silver; the button is also reachable with keyboard/controller navigation. Navigation selects entries and keeps them visible; Escape/B or the close control returns to the main menu. Locked unlock entries show silhouettes; quest-complete entries show their price and a blue marker until first viewed; owned entries show their full-color icon. The detail panel shows the quest requirement, purchase action or owned state.
