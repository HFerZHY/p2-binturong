# Inspiration System — Developer Guide

## What it is

`InspirationManager` is a **persistent singleton** that:

1. Tracks which of the 16 inspirations have been unlocked.
2. Shows a small **toast pop-up** at the top of the screen on each unlock.
3. Lets the player press **[E]** to open a full **journal overlay** listing all 16 inspirations (locked ones shown as greyed-out `???`).

The manager survives scene loads via `DontDestroyOnLoad` and auto-creates itself on first use — **you never need to place it in a scene or create a prefab**.

---

## File locations

| File | Purpose |
|------|---------|
| `Assets/Scripts/InspirationManager.cs` | The singleton — handles all UI and state |
| `Assets/Scripts/Intro/RyoteiController.cs` | Scene 5 example: shows how to trigger unlocks from a dialogue beat |
| `Assets/Scripts/README_InspirationSystem.md` | This file |

---

## How to unlock an inspiration

Call `InspirationManager.Instance.Unlock(id)` with a **1-based ID** (1–16).

```csharp
// Unlock a single inspiration
InspirationManager.Instance.Unlock(5);

// Unlock several at once (toasts appear sequentially, each auto-dismisses)
InspirationManager.Instance.Unlock(10);
InspirationManager.Instance.Unlock(11);
InspirationManager.Instance.Unlock(12);
```

- Safe to call multiple times — already-unlocked IDs are silently ignored.
- The **first-ever** unlock also triggers the "Press [E] to open your Journal" hint banner automatically.

---

## Triggering unlocks from a dialogue beat (RyoteiController pattern)

`RyoteiController` stores a list of `Beat` structs. Each Beat has an optional `int[] UnlocksInspirations` field. When the player clicks to advance **past** that beat, all listed IDs are unlocked:

```csharp
// In BuildBeats():
D("Rin", true, "(Well, I guess that sparked some inspiration...)",
    unlocks: new[] { 10, 11, 12 }),
```

The toast pop-ups appear over the scene while dialogue continues normally — the player doesn't need to interact with them.

**To add unlocks to a new scene**, follow the same pattern:

1. Add `int[] UnlocksInspirations` to your Beat/line struct.
2. In `AdvanceBeat()`, after checking `HidesInspector` etc., add:

```csharp
if (prev.UnlocksInspirations != null)
    foreach (int id in prev.UnlocksInspirations)
        InspirationManager.Instance.Unlock(id);
```

---

## Blocking dialogue input while journal is open

Any scene controller that handles click-to-advance input should guard against the journal being open:

```csharp
private void Update()
{
    if (_inputLock) return;
    if (InspirationManager.IsJournalOpen) return; // add this line
    // ... rest of click handling
}
```

`RyoteiController` already has this guard. **Add the same line to any new scene controller you write.**  
The journal's full-screen overlay image also visually blocks the scene, but the input guard prevents invisible click-through on the dialogue panel.

---

## Matching the game font

Call `SetFont` once from your scene controller's `Awake()` to make the journal match the rest of your UI:

```csharp
// In Awake():
InspirationManager.Instance.SetFont(serifFont);
```

If you don't call this, TMP's built-in default font is used — functional but won't match the visual style.

---

## The 16 inspirations

| ID | Flavor text |
|----|-------------|
| 1 | Rare creatures dwell within the forests of Otowa. |
| 2 | Wherever Rintaro goes, this is never far behind. |
| 3 | A professor retired to Otowa to savor the quiet life. |
| 4 | The color of the water, the color of the birds, the color of Otowa. |
| 5 | A music boy left Otowa after a bitter quarrel with his father. |
| 6 | Octopus traps, fleeting dreams under the summer moon. |
| 7 | Legend speaks of an indigenous Otowa belief in an avian deity. |
| 8 | When it blossoms in the sky, it marks the most beautiful night of summer. |
| 9 | Bye Bye, my Otowa town. |
| 10 | The source of Otowa's signature flavor, found in sake and local cuisine. |
| 11 | A mysterious recipe dating back centuries. |
| 12 | It won Otowa a gold medal at the regional specialty competition over a decade ago. |
| 13 | A blessing from Otowa: health and peace. |
| 14 | The healing properties of Otowa's hot springs. |
| 15 | A father's silent love. |
| 16 | On that day, all wandering souls journey back to Otowa. |

**Currently unlocked in:** Scene 5 (Ryotei) → IDs 10, 11, 12  
**Remaining 13** are unlocked by other scenes — search `TODO: unlock inspiration` to find placeholder hooks.

---

## Reading the unlock state from StationWorkScene

`InspirationManager` is a persistent singleton, so it is accessible from any scene:

```csharp
// Check if a specific inspiration is unlocked
bool known = InspirationManager.Instance.IsUnlocked(5);

// Get count of unlocked inspirations
int count = InspirationManager.Instance.UnlockedCount;
```

> **Note:** `IsUnlocked` and `UnlockedCount` are not yet in the script. Add them when the StationWork gameplay loop needs them — the backing `_unlocked[]` array is already populated.

---

## Persistence (cross-session saves)

The current implementation keeps unlock state **in memory only** — it resets when the application quits. For a playtest this is fine. If you need state to survive restarts, add PlayerPrefs calls inside `InspirationManager.Unlock()`:

```csharp
// Save
PlayerPrefs.SetInt($"insp_{id}", 1);
PlayerPrefs.Save();

// Load (in Awake, after BuildUI)
for (int i = 1; i <= 16; i++)
    if (PlayerPrefs.GetInt($"insp_{i}", 0) == 1)
    {
        _unlocked[i] = true;
        RefreshEntry(i);
        if (!_introduced) _introduced = true;
    }
```

---

## Unity Setup Checklist (new scene)

Nothing is required for `InspirationManager` itself — it auto-creates. For a new dialogue scene that triggers unlocks:

- [ ] Add `if (InspirationManager.IsJournalOpen) return;` to `Update()`.
- [ ] Call `InspirationManager.Instance.SetFont(yourFont)` in `Awake()`.
- [ ] Add `UnlocksInspirations = new[] { id1, id2 }` to the relevant Beat(s).
- [ ] Fire `InspirationManager.Instance.Unlock(id)` in `AdvanceBeat()` when leaving those beats.
