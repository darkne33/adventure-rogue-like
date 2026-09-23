# Demo quests and unlocks

The demo contains 17 quests and 17 purchasable unlocks: 2 characters, 1 weapon, 8 scrolls and 6 relics. Completing a quest makes its associated unlock available for purchase; the player must spend persistent silver before the content becomes usable. QUESTS uses one scrollable list with a Hide completed filter; UNLOCKS uses category tabs and an eight-column grid. Both windows use the project's own sprites and a selected-entry detail panel.

## Configuration

Edit `Assets/Features/Quests/Configs/DemoProgressionConfiguration.asset` in the Inspector. Its `ProgressionConfiguration` ScriptableObject is registered in Addressables and loaded by `GameplayAssetService` during `BootstrapState`, before quest services are initialized or gameplay scenes are loaded.

- **Default Characters / Abilities / Relics** control what is available on a new save, independently of purchases.
- **Quests** contain a stable ID, title, condition text, category, metric, target and one-time silver reward.
- **Unlocks** contain a stable ID, category, character ID or ability/relic reference, required quest ID and silver cost. Optional name, description and icon override the referenced asset. `Unlocked By Default` can make an individual catalog entry immediately owned.
- **Fallback Content Icons** contain currency and fallback portrait references. Character portraits also use the existing character-selection portrait cache. Window appearance is authored in prefabs.

Keep quest and unlock IDs stable: saves refer to these IDs. Each demo quest has one purchasable unlock. Content absent from both the default arrays and the unlock catalog is excluded from demo selection and reward pools. Character-specific starting weapons remain upgradeable after selecting an owned character; character weapon exclusions still apply.

## Starting content

- Character: RABBIT.
- Weapons: Rabbitarang, Fireball and Earth Rock. Rabbitarang is RABBIT's starting weapon.
- Scrolls: Damage, Armor, Attack Speed, Crit Damage and Crit Chance.
- Relics: Hot Dog, Wallet, Iron Hammer and Venom Blade.

Bullet Explosion and Punch are their characters' signature weapons. They become available with those characters and are not separate purchases.

## Progress, purchases and silver

- Single-run goals store the best result reached in one run; partial results from different runs are not added together.
- Lifetime goals accumulate across runs, including lost runs.
- Combat time counts only active, unfinished combat rooms. Pauses, transitions, cleared rooms and safe rooms do not advance it.
- Boss victories count completed boss rooms, so a splitting boss grants one victory.
- Collected gold includes gold already spent. Starting relics do not count as pickups.
- Quest silver is awarded automatically once. Every positive silver gain in the run wallet is also credited to the persistent wallet immediately, including final pickup callbacks after death. Ending a run does not deposit the same silver again.
- Progress, completed quest IDs, purchased unlock IDs and persistent silver are saved together under the existing PlayerPrefs key `little_rush.quests.v1` (payload version 2). Existing progress and balances are retained; old completed quest IDs do not automatically count as purchases.
- Completion, purchases and collected silver save immediately. Unfinished progress also saves periodically and at run/lifecycle boundaries.
- `QuestService` is the ownership authority for character selection, upgrade offers and relic pools. A completed quest alone does not bypass the purchase requirement. The old relic-specific `UnlockQuestId` / `UnlockCost` fields are not the demo's purchase configuration.

## Quest conditions and unlock prices

These conditions and IDs are retained from the original catalog. Targets, quest silver rewards and purchase prices are editable in the configuration. Quest silver is the automatic completion reward; the purchase price is the separate amount spent to own the unlocked content.

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
| two_weapons_3 | Have two weapons at level 3 or higher in one run. | Turbo Skates | 3 | 4 |
| bosses_1 | Clear a boss room. | Voodoo Doll | 3 | 5 |
| critical_25 | Land 25 critical hits across all runs. | Spiky Shield | 1 | 3 |
| combat_300 | Spend 300 seconds in active combat in one run. | Overpowered Chalice | 2 | 6 |

Quest categories follow their rewards: Characters for DUKE and MR POCKET, Weapons for Fire Trail, Scrolls for all eight scrolls and Relics for all six relics. Fire Trail is the display name of the existing Fire Field ability asset (`AbilityName.FireField`).

## UI prefabs

New quest completions display a non-interactive dark/gold notification at the top of the screen, slightly right of center. It shows the quest title and condition, automatically received silver, and any content now available to buy in UNLOCKS. Notifications appear in order, hold for four seconds, then fade out. Their animation uses unscaled time, and the project-level queue survives scene changes so a completion just before death is still shown. Existing completed quests are not replayed when loading a save.

Edit `Assets/Features/Quests/Prefabs/QuestCompletionNotification.prefab` to style the card. The prefab is loaded through Addressables during bootstrap and its handle is retained for the project lifetime. `QuestCompletionNotificationController` owns the queue and timing; `QuestCompletionNotificationView` only binds data and presentation. `SoundId.QuestComplete` uses a quiet version of the existing start-click cue through `SoundsCatalog`, respecting the player's SFX volume and mute settings.

Edit these assets directly in Prefab Mode:

- `Assets/Features/Quests/Prefabs/QuestsPanel.prefab` — full window, header, Hide completed button, scroll viewport, completion summary and footer.
- `Assets/Features/Quests/Prefabs/QuestRow.prefab` — checkbox, condition, progress, reward icon and selection corners.
- `Assets/Features/Quests/Prefabs/UnlocksPanel.prefab` — full window, four tabs, grid, currency and purchase details.
- `Assets/Features/Quests/Prefabs/UnlockCell.prefab` — item icon, price, available-purchase marker and selection corners. State colors are serialized on its view component.

`MainMenuPanel.prefab` references the two window prefabs. The view scripts instantiate the assigned window and row/cell prefabs and bind data; they do not construct UI hierarchies or add UI components at runtime. The iron frame textures already included in the project are arranged as editable RawImage edge/corner pieces, so their original SpriteImporter settings remain unchanged.

The former QUESTS `SilverBalance` belongs to `MainMenuPanel.prefab` and appears at the top left, mirrored from its original top-right position. It stays visible over QUESTS and UNLOCKS, updates when `QuestService.Changed` fires, hides during character selection, and reappears when returning to the main menu.

## Controls

Mouse clicks select entries and UNLOCKS tabs; the mouse wheel scrolls without changing the selected quest or footer. Hide completed filters completed quests out of the list. Keyboard/controller navigation selects entries and keeps them visible; Escape/B or the close control returns to the main menu. UNLOCKS opens with an empty detail panel until an item is selected. Locked entries show silhouettes; quest-complete entries show their price and purchase marker; owned entries show their full-color icon. The detail panel shows the quest requirement, purchase action or owned state.
