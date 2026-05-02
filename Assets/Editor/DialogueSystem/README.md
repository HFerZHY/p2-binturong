# Dialogue Graph Editor

A node-based visual editor for authoring localized dialogue in Unity, built on top of `GraphView`. Designers can create branching conversations, set conditions, wire up animation triggers, and author translated strings for every registered locale — all without touching a JSON file or a script.

---

## Requirements

| Dependency | Minimum Version |
|---|---|
| Unity | 2021.3 LTS |
| Unity Localization package | 1.5.x |
| Unity Addressables package | 1.21.x *(Localization dependency)* |

---

## Installation

1. Copy the `DialogueSystem/` folder into your project's `Assets/Editor/` directory. The final layout should be:
   ```
   Assets/
   └── Editor/
       ├── DialogueSystem/
   ```
2. Open **Window → Package Manager**, confirm the **Localization** package is installed.
3. Open **Edit → Project Settings → Localization** and complete first-time localization setup if you haven't already (add at least one locale).
4. Create the two required String Table Collections (see [Localization Setup](#localization-setup) below).
5. In Unity's menu bar, open **Tools → Dialogue System → Open Graph Editor** to launch the editor window.

---

## Localization Setup

The editor reads from and writes to two named String Table Collections. Create them in **Window → Asset Management → Localization Tables → Create → String Table Collection**:

| Collection name        | Purpose |
|------------------------|---|
| `DialogueSpeakerTable` | Speaker display names, one entry per speaker key, shared across graphs |
| `DialogueTextTable`        | Per-node dialogue lines, one entry per node text key |

Both names are declared as constants at the top of `LocalizationTableService.cs` and can be changed there if your project uses different names:

```csharp
public const string SpeakerCollectionName  = "DialogueSpeakerTable";
public const string DialogueCollectionName = "DialogueTextTable";
```

Each collection must have one String Table per locale you intend to support. The editor derives its locale list from the locales registered in **Edit → Project Settings → Localization**.

---

## Creating a Dialogue Graph

1. In the **Project** window, right-click → **Create → Dialogue System → Dialogue Graph** to create a new `DialogueGraph` asset.
2. Double-click the asset, or click **▶ Open in Dialogue Graph Editor** in its Inspector, to open the editor window.
3. Use the **+ Add Node** toolbar menu to add your first node, then click **★ Set as Entry Node** in the inspector panel to designate it as the conversation's starting point.

---

## Editor Layout

```
┌──────────────────────────────────────────────────────────────┐
│  Toolbar                                                     │
├────────────────────────────────────────┬─────────────────────┤
│                                        │                     │
│   Graph View (canvas)                  │   Node Inspector    │
│                                        │   (320 px)          │
│                                        │                     │
├────────────────────────────────────────┴─────────────────────┤
│  Validation bar                                              │
└──────────────────────────────────────────────────────────────┘
```

### Toolbar

| Control | Action |
|---|---|
| **Graph** object field | Load a `DialogueGraph` asset into the editor |
| **Preview** locale dropdown | Switch the locale shown in all canvas node previews |
| **+ Add Node** | Add a Line, Branch, or Terminal node at the canvas centre |
| **Auto Layout** | BFS left-to-right layout from the entry node |
| **Validate** | Run graph validation and show a popup with any errors |
| **Import JSON** | Load a graph from a `.json` file (auto-layout applied) |
| **Export JSON** | Save the current graph to a `.json` file |
| **💾 Save** | Save the graph asset, editor layout, and all StringTable edits |

### Canvas

- **Pan** — middle-mouse drag or Alt + drag
- **Zoom** — scroll wheel
- **Select** — click a node; Shift+click or drag-rectangle for multi-select
- **Move** — drag selected nodes; positions are saved on the next Save
- **Connect nodes** — drag from an output port to any input port; back-edges (loops) are fully supported
- **Disconnect** — right-click an edge → Delete, or select the edge and press Delete
- **Context menu** — right-click the canvas background to add a node at that position

### Node colours

| Colour | Node type |
|---|---|
| Blue | **Line** — a single spoken line, advances to one next node |
| Amber | **Branch** — presents player choices; one output port per choice |
| Red | **Terminal** — ends the conversation |

The **gold ENTRY badge** appears on whichever node is set as the graph's entry point.

---

## Node Inspector

Selecting a node on the canvas opens its full detail in the right-hand panel.

### Identity
Read-only display of the node's ID and type.

### Graph Role
Shows whether this node is the entry node. If it isn't, a **★ Set as Entry Node** button is shown.

### Localized Content *(Line and Branch nodes)*

**Speaker**

A dropdown lists every key registered in the `DialogueSpeakers` collection. Selecting a key sets `node.speakerNameKey`. Below the dropdown, one text field per locale allows editing the translated display name for that speaker.

Because speaker keys are shared across all nodes, editing a speaker's translated value for a locale immediately updates the canvas preview on *every* node that references that speaker — not just the selected one.

To add a new speaker, type a key name (e.g. `Guard`) in the **New speaker key** field and click **+ Add**. The key is registered in the collection immediately, and the node is auto-assigned to it.

**Text** *(Line nodes only)*

The **Text Key** field sets `node.textKey` — the key used to look up this node's dialogue line in the `DialogueTexts` collection. Below it, one text area per locale allows authoring the translated dialogue text. Text edits are held in memory and written to the StringTable when **💾 Save** is pressed.

> **Canvas preview** — the node box on the canvas always shows the resolved string for the locale selected in the toolbar dropdown. If a key has no entry in the current locale, the preview reads *(no translation)* in muted red.

### Flow *(Line nodes)*
**Next Node ID** — the ID of the node to advance to after this line plays. Changing this field rewires the output edge live.

### Choices *(Branch nodes)*
Each choice has a **Label** (the UI button text shown to the player), a **Target Node ID**, a **Show if conditions fail** toggle, and an optional list of conditions. Choices can be added and removed freely; the canvas output ports update to match.

### Presentation *(Line nodes)*
- **Typewriter Speed** — seconds per character for the text reveal effect (`0` = instant)
- **NPC Animator Trigger** — the `Animator.SetTrigger` name fired on the NPC when this node plays

### Node Conditions
A list of `DialogueCondition` entries that must all pass for the node to be reachable. Each condition has a type, a key, an expected value, and an optional negate flag.

| Condition type | Evaluation |
|---|---|
| `QuestFlag` | `GameState.GetFlag(key) == value` |
| `HasItem` | `Inventory.HasItem(key, int.Parse(value))` |
| `RelationshipMin` | `RelationshipSystem.Get(key) >= int.Parse(value)` |
| `CustomEvaluator` | Defers to a registered `IConditionEvaluator` |

---

## Saving

Press **💾 Save** in the toolbar (or close the window) to:

1. Save the `DialogueGraph` ScriptableObject asset.
2. Save the companion `*_EditorData.asset` (canvas node positions).
3. Flush any pending dialogue text edits into the `DialogueTexts` StringTable and save all dirty table assets.

Speaker name edits are written to the `DialogueSpeakers` StringTable immediately on change (because they are shared across nodes). Text edits are deferred to Save.

---

## JSON Import / Export

The JSON schema mirrors the `DialogueGraph` structure directly. Keys in the JSON correspond to `speakerNameKey` and `textKey` on each node — the localized strings themselves live in the StringTable collections, not in the JSON.

**Import** — opens a file picker, parses the JSON via `DialogueJsonLoader.ParseJson`, prompts for a save location, creates a new `DialogueGraph` asset, and applies auto-layout since no canvas positions exist yet.

**Export** — serializes the current graph via `DialogueJsonExporter` and writes a `.json` file to a location of your choice. Opens the file in Finder/Explorer afterwards.

---

## Validation

The validation bar at the bottom of the window runs `DialogueGraph.Validate()` on every save and inspector change. Errors are listed as bullet points; a green "✓ valid" message is shown when the graph is clean.

Common validation errors and their fixes:

| Error | Fix |
|---|---|
| `entryNodeId is empty` | Select a node and click **★ Set as Entry Node** |
| `Duplicate node id` | Edit the ID on one of the duplicates in the raw data inspector |
| `Node 'x' references missing nextNodeId 'y'` | Rewire the output edge or correct the Next Node ID field |
| `Choice references missing targetNodeId` | Rewire the choice port or correct the Target Node ID field |

---

## File Structure

```
DialogueSystem/
├── Runtime/
│   └── DialogueSystem.Runtime.asmdef
└── Editor/
    ├── DialogueSystem.Editor.asmdef
    ├── Data/
    │   ├── DialogueNodeEditorData.cs       # Per-node canvas position
    │   └── DialogueGraphEditorData.cs      # Companion asset storing all positions
    ├── DialogueSystem/
    │   └── Styles/
    │       ├── DialogueGraphView.uss       # Canvas node and edge styles
    │       └── DialogueEditorWindow.uss    # Toolbar, inspector panel, validation bar
    ├── Graph/
    │   ├── DialogueGraphEditorWindow.cs    # Main EditorWindow — layout and toolbar
    │   ├── DialogueGraphView.cs            # GraphView canvas
    │   ├── DialogueNodeView.cs             # Individual node box
    │   ├── DialogueNodeInspectorPanel.cs   # Right-hand detail panel
    │   ├── DialogueValidationPanel.cs      # Bottom status bar
    │   └── DialogueEditorResources.cs      # USS asset loader
    ├── Inspectors/
    │   └── DialogueGraphInspector.cs       # Custom Inspector for DialogueGraph assets
    ├── Localization/
    │   ├── LocalizationTableService.cs     # All StringTable read/write logic
    │   └── DialogueLocaleState.cs          # Window-scoped active locale + preview cache
    └── Serialization/
        └── DialogueJsonExporter.cs         # DialogueGraph → JSON
```

The runtime assembly (`DialogueSystem.Runtime`) contains `DialogueGraph`, `DialogueNode`, `DialogueJsonLoader`, and related data types. The editor assembly references it and is excluded from builds automatically by Unity's `Editor/` folder convention.

---

## Extending the Editor

**Adding a new condition type** — add a value to the `ConditionType` enum in `DialogueNode.cs`. The `EnumField` in the inspector panel picks it up automatically. Implement evaluation logic in your runtime `IConditionEvaluator`.

**Adding a new StringTable collection** — declare a new `const string` in `LocalizationTableService` alongside `SpeakerCollectionName` and `DialogueCollectionName`, and add corresponding `GetXEntry` / `SetXEntry` methods following the same pattern.

**Changing the USS styles** — edit the two `.uss` files in `Editor/DialogueSystem/Styles/`. The path is configured in `DialogueEditorResources.cs` if you move them.