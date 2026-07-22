# D20 Dice Roll — Baldur's Gate 3 Style

Механика броска D20 кубика по аналогии с Baldur's Gate 3. Unity-проект с чистой архитектурой на базе принципа Single Responsibility.

---

## О проекте

Реализация анимации броска D20 кубика для tabletop RPG механики проверки характеристик. Кубик физически перемещается по сцене, вращается, отражается от границ и возвращается в центр, после чего показывается результат броска с модификатором.

## Запуск

1. Открыть проект в **Unity 2021.3+** (URP)
2. Установить пакеты:
   - **DOTween** (免费) — через Package Manager или Asset Store
   - **Input System** — через Package Manager
   - **TextMeshPro** — при первом открытии项目的会提示导入
3. Открыть сцену `Assets/_Project/Scenes/DiceScene.unity`
4. Нажать Play

### Настройка в инспекторе

На сцене есть объект **GameControllers** — на нём расположены:
- `RollManager` — координатор, перетащи сюда все контроллеры
- `AudioController` — перетащи AudioSource и AudioClip'и

На объекте **Dice Warapper**:
- `DiceController` — ссылки на кубик, камеру, face transforms
- `DiceVisualController` — blur материалы, частицы, shine renderer

На объекте **UI (Canvas)**:
- `RollUIController` — UI группы (rollPrompt, rollButton, difficultyText)
- `ModifierController` — карточка модификатора и fly text
- `ResultController` — баннер результата и кнопка Continue

На объекте **D20_Dice**:
- `DiceButton` — UnityEvent → `RollManager.StartRoll`

---

## Архитектура

```
RollManager (координатор)
├── DiceController         — движение, вращение, snap кубика
├── DiceVisualController   — blur, материалы, частицы, pulse, shine
├── RollUIController       — intro-анимация, show/hide UI групп
├── ModifierController     — карточка модификатора + fly text
├── ResultController       — баннер Success/Failure + Continue
└── AudioController        — инкапсуляция всех PlayOneShot
```

### RollManager (`RollManager.cs` — 85 строк)

Координирует всю последовательность броска. Не содержит UI-анимаций и логики движения кубика.

```
StartRoll() → RollSequence()
  1. PrepareForRoll()          — скрыть intro UI
  2. ShowCard()                — показать модификатор
  3. PlayRoll()                — звук броска
  4. RollDice(result)          — запустить анимацию кубика
  5. Wait until !IsRolling     — ждать окончания
  6. AnimateFlyText()          — летающий текст модификатора
  7. SnapToFace(total)         — мгновенно показать результат
  8. PlayRollPulse()           — пульс кубика
  9. FadeCardToHalf()          — модификатор наполовину
  10. ShowBanner()             — Success/Failure
  11. HideCard()               — скрыть модификатор
  12. AnimateContinue()        — кнопка Continue
```

### DiceController (`DiceController.cs` — 199 строк)

Отвечает только за физику кубика:
- Перемещение с отскоком от границ (XZ)
- Вращение во время прокрутки
- Возврат в начальную позицию
- Snap к нужной грани (мгновенно)
- Состояние `IsRolling`

Не знает про UI, аудио или визуальные эффекты.

### DiceVisualController (`DiceVisualController.cs` — 175 строк)

Инкапсулирует визуальные эффекты кубика:
- **Blur** — переключение между normal/blur материалами, `SetBlur(float)` для анимации
- **Particles** — `PlayImpact()` при приземлении
- **Pulse** — intro-пульс (2×) и roll-пульс
- **Shine** — анимация `_ShinePosition` от 3 до -1

### RollUIController (`RollUIController.cs` — 148 строк)

Управляет intro-анимацией и видимостью UI:
- Intro-последовательность: modifier → rollButton → rollPrompt → dicePulse → shine
- `PrepareForRoll()` — останавливает intro, скрывает shine
- Не содержит игровой логики и вычислений

### ModifierController (`ModifierController.cs` — 120 строк)

Отвечает за отображение модификатора:
- Карточка модификатора (show, fade in, fade to half, hide)
- Fly text (appear, scale up, move to endpoint, disappear)
- Intro slide-анимация (снизу вверх)

### ResultController (`ResultController.cs` — 86 строк)

Отвечает за результат броска:
- Баннер Success/Failure (текст, цвет, fade)
- Кнопка Continue (fade, interactable)

### AudioController (`AudioController.cs` — 19 строк)

Три метода:
- `PlayRoll()` — звук начала броска
- `PlayModifier()` — звук добавления числа
- `PlaySuccess()` — звук результата

### RollUIConfig (`RollUIConfig.cs` — 30 строк)

Serializable-класс со всеми таймингами анимаций. Настраивается в инспекторе.

---

## Структура сцены

```
DiceScene
├── Main Camera              — URP, перспектива, вид сверху
├── Directional Light        — URP, мягкие тени
├── Global Volume            — URP пост-обработка
├── Dice Warapper            — DiceController + DiceVisualController
│   └── D20_Dice            — префаб кубика (10x scale, 20 face transforms)
├── UI [Canvas]              — RollUIController + ModifierController + ResultController
│   └── BG → Panel
│       ├── RollButton       — "Click dice to roll"
│       ├── DiceBG           — плейсхолдер для кубика
│       ├── RollPromt        — Difficulty Class
│       ├── Result           — Success/Failure баннер
│       ├── ModifierCard     — карточка модификатора
│       ├── FlyText          — летающий "+1"
│       └── ContinueButton   — кнопка Continue
├── GameControllers          — RollManager + AudioController
└── EventSystem
```

---

## Зависимости

| Пакет | Версия | Назначение |
|---|---|---|
| DOTween | Free | Анимации UI и кубика |
| Input System | 1.0+ | Клик мышью по кубику |
| TextMeshPro | 3.0+ | Рендеринг текста |
| URP | — | Рендеринг |

---

## Лицензия

MIT
