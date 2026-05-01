# Unity Dialogue System

A fully decoupled, data-driven dialogue system for Unity.
Supports ScriptableObject graphs, JSON loading, branching, conditions, and InputSystem integration.

---

## File Overview

```
DialogueSystem/
├── Data/
│   ├── DialogueNode.cs          — Single node (line, branch, or terminal)
│   └── DialogueGraph.cs         — ScriptableObject container of nodes
│
├── Interfaces/
│   └── IDialogueInterfaces.cs   — IInteractable, INPCDialogueProvider,
│                                  IConditionEvaluator, IDialogueTrigger
│
├── Core/
│   ├── DialogueManager.cs       — Singleton driving the conversation state machine
│   └── DefaultConditionEvaluator.cs — Evaluates quest/item/relationship conditions
│
├── Player/
│   └── PlayerInteractor.cs      — InputAction listener + proximity detection
│
├── NPC/
│   └── NPCDialogueController.cs — IInteractable + INPCDialogueProvider on NPCs
│
├── UI/
│   └── DialogueUIController.cs  — IDialogueView: typewriter, choices, prompt
│
├── Serialization/
│   └── DialogueJsonLoader.cs    — Load DialogueGraph from JSON at runtime
│
├── Resources/
│   └── Dialogues/Guard/GuardGate.json — Example dialogue JSON
│
└── DialogueGameBootstrapper.cs  — Wires evaluator to DialogueManager on Start()
```

---

## Quick Setup

### 1. Package Requirements
- **TextMeshPro** — for dialogue text rendering
- **Input System** — for InputAction-based interaction

### 2. Scene Hierarchy

```
[PERSISTENT] DialogueSystemRoot
├── DialogueManager          (DialogueManager.cs)
│   └── DialogueGameBootstrapper.cs
│
├── DialogueCanvas (Canvas)
│   ├── DialoguePanel
│   │   ├── PortraitImage       (Image)
│   │   ├── SpeakerNameText     (TMP_Text)
│   │   ├── DialogueBodyText    (TMP_Text)
│   │   ├── ContinueIndicator   (Button)
│   │   └── ChoicesContainer    (Vertical Layout Group)
│   └── InteractPromptRoot
│       └── InteractPromptText  (TMP_Text)
│
[PLAYER]
└── PlayerRoot
    ├── PlayerInput             (PlayerInput component with your InputActionAsset)
    ├── PlayerInteractor.cs
    └── InteractTrigger (child)
        └── Sphere Collider (IsTrigger = true)

[NPC]
└── GuardNPC
    ├── NPCDialogueController.cs  ← assign DialogueGraph asset here
    ├── Animator
    └── Collider (on "Interactable" layer, IsTrigger = true)
```

### 3. InputActionAsset

Add an action called **"Interact"** to your action map (type: Button).  
Bind it to `E`, `Gamepad South`, or whichever input you prefer.

### 4. Component Wiring

**DialogueManager** inspector:
- `dialogueViewBehaviour` → your `DialogueUIController` MonoBehaviour

**PlayerInteractor** inspector:
- `interactActionName` → `"Interact"` (must match your InputActionAsset)
- `interactableLayerMask` → your "Interactable" layer
- `dialogueViewBehaviour` → your `DialogueUIController` MonoBehaviour

**NPCDialogueController** inspector:
- `dialogueGraph` → a `DialogueGraph` ScriptableObject asset
- `npcAnimator` → the NPC's Animator (optional)

**DialogueUIController** inspector:
- Wire all the serialized UI references (panel, texts, button prefab, etc.)

---

## Creating a Dialogue Graph (ScriptableObject)

Right-click in the Project window:
**Create → Dialogue System → Dialogue Graph**, 
then use the Dialogue Graph Editor (blue button on the top of the inspector) 
to edit and save the Dialogue Graph. 

---

## Loading a Graph from JSON at Runtime

```csharp
// From Resources/Dialogues/Guard/GuardGate.json
var graph = DialogueJsonLoader.LoadFromResources("Guard/GuardGate");
npcController.SetDialogueGraph(graph);
```

---

## Loading a Graph from JSON in Editor Mode

In the top menu bar, select **Tools → DialogueGraph → CreatFromJSON**, 
select the JSON file (**must be in a folder named Resources**) 
and then save the resulting ScriptableObject as an asset file in the project.

---

## Adding Conditions

In a `DialogueNode` or `DialogueChoice`, add a `DialogueCondition`:

| Type              | key           | value         | effect                              |
|-------------------|---------------|---------------|-------------------------------------|
| QuestFlag         | "MetKing"     | "true"        | passes if flag == "true"            |
| HasItem           | "BaronLetter" | "1"           | passes if player has ≥ 1 of item    |
| RelationshipMin   | "Guard"       | "50"          | passes if relationship value ≥ 50   |
| CustomEvaluator   | "MySneakKey"  | (any)         | calls your registered custom lambda |

Register your game-system providers on `DefaultConditionEvaluator` via `DialogueGameBootstrapper`.

---

## Adding Custom Conditions

```csharp
var evaluator = new DefaultConditionEvaluator(questFlags, inventory, relationships);
evaluator.RegisterCustom("PlayerIsSneaking", c => PlayerController.IsSneaking);
DialogueManager.Instance.RegisterConditionEvaluator(evaluator);
```

---

## Events

Subscribe to static events for cross-system reactions:

```csharp
DialogueManager.OnConversationStarted += () => DisablePlayerMovement();
DialogueManager.OnConversationEnded   += () => EnablePlayerMovement();
DialogueManager.OnNodeEntered         += node => PlayVoiceLine(node.voiceClip);
```

Per-node `UnityEvent` callbacks (`onEnter`, `onExit`) can be wired directly in the Inspector.

---

## Triggering Dialogue from Code (cutscenes, quests)

```csharp
// Via IDialogueTrigger interface
IDialogueTrigger trigger = DialogueManager.Instance;
trigger.TriggerDialogue(myGraph);

// Or directly
DialogueManager.Instance.StartConversation(npcProvider, initiatorGO);
```
