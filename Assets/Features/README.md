# Структура Features

Каждая самостоятельная игровая механика размещается в собственной папке:
`Assets/Features/<FeatureName>`. Имя фичи и её папок задаётся в `PascalCase`.

## Основные папки

Создавайте только те папки, которые действительно нужны фиче:

- `Scripts` — runtime-скрипты механики.
- `Prefabs` — префабы, принадлежащие механике.
- `Configs` — конфигурации и `ScriptableObject`-ассеты.
- `Sprites` или `Textures` — собственная 2D-графика фичи.
- `Models`, `Materials`, `Animations`, `Sounds` — соответствующий контент.
- `Editor` — код, который должен компилироваться только для Unity Editor.
- `Resources` — только ассеты, которые действительно загружаются через `Resources`.
- `Content/<ContentName>` — изолированный набор контента со своими `Configs`,
  `Prefabs`, `Materials`, `Models`, `Textures` и другими подпапками.

Не храните `.cs` и `.prefab` непосредственно в корне фичи. Для групп однотипных
префабов используйте вложенную папку, например `Prefabs/Rewards`.

Не переносите исходные ассеты из `third-party` внутрь фичи. Создавайте
feature-owned префаб или вариант и оставляйте общие UI-иконки в их общей папке.
При перемещении Unity-ассета всегда перемещайте вместе с ним его `.meta`, чтобы
сохранить GUID и все сериализованные ссылки.

## Пример

```text
Assets/Features/RewardBag/
├── Materials/
│   └── KeyRewardParticle.mat
├── Textures/
│   └── KeyRewardAtlas.png
├── Scripts/
│   ├── RewardBag.cs
│   └── RewardBagSpawner.cs
└── Prefabs/
    ├── RewardBag.prefab
    └── Rewards/
        ├── CoinSilver.prefab
        └── KeyReward.prefab
```
