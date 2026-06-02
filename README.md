# Project 2 - Team Binturong

This is the Unity Project for P2. Please use Unity Editor version 6000.3.9f1.

In commit messages, please use +, -, and * to indicate addition, removal, and modifications of elements and functionalities.

---

## Scene Flow

### Day 1

```
Intro-1 (START)  →  JunkoIntro  →  Intro-3  →  TutorialToRyotei  →  Intro-5  →  Day1World  →  Day1 End
```

| Scene | Description | Key Script(s) |
|---|---|---|
| **Intro-1** | Train arrival cutscene. Rin sees Otowa from the train window. | `IntroController` |
| **JunkoIntro** | Rin meets village chief Junko on the platform. First player-choice branches (reaction to acting stationmaster role; banquet attendance). | `OtowaIntroDialogueTrigger` |
| **Intro-3** | Stationmaster's office — Rin reads Hikaru's letter, reviews the inventory, and has the first inspector encounter. Inspector branch (kiss-up vs. direct). | `StationController` |
| **TutorialToRyotei** | Short tutorial walk through the village leading to the Ryotei. Teaches overworld movement and interaction. | — |
| **Intro-5** | Welcome banquet at the Ryotei. Rin meets Yuji and Jiro. Food/sake reaction branches. Inspector delivers the station-closure ultimatum. | `RyoteiController` |
| **Day1World** | Overworld exploration. Rin can visit the hot spring, talk to villagers, and collect inspiration items. Map available via Tab. | `MapController`, `MinimapController`, `DialogueManager` |
| **Day1 End** | End-of-day cutscene. Rin reflects on what she's learned and commits to saving the station. | — |

#### Branching Points

- **JunkoIntro** — reaction to being made acting stationmaster (confident / concerned / anxious) and whether to attend the banquet.
- **Intro-3** — Rin's response to the inspector's dismissal of the museum idea (kiss-up / direct).
- **Intro-5** — food reaction (genuine praise / honest uncertainty / polite bluff) and sake reaction (find it bitter / find it smooth).

#### Audio

Run **Tools → Otowa → Audio → Wire All Intro Scenes** after opening the project to assign ambient/SFX clips to Intro-1, Intro-3, and Intro-5.