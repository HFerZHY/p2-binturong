# Museum Exhibition System - Setup Guide

## Quick Start

After pulling this branch, run these Unity menu commands in order:

1. **Tools → Museum → Generate Test Data**
   - Creates sample items and themes in `Resources/Exhibitions/`

2. **Tools → Museum → Rebuild Prefabs**
   - Generates UI prefabs (ShelfSlot, DisplaySlot, VisitorPanel, etc.)
   - Creates the VisitorRT RenderTexture for character rendering

3. **Tools → Museum → Build Exhibition Scene**
   - Creates `Scenes/ExhibitionScene.unity` with full UI layout

4. **Play the scene** to test the exhibition system

## Gameplay

1. Click "Select Theme" to choose an exhibition theme
2. Drag items from the left shelf to the display slots
3. Click "Start Exhibition" to begin visitor evaluation
4. Visitors react to each slot - matching items increase satisfaction
5. Meet the satisfaction threshold to succeed

## Dependencies

- Character assets in `Resources/Characters/` (YoungManBase, YoungWomanBase)
- Color palettes (SkinPalette, HairPalette, ClothesPalette)
- CharacterRecolorMaterial with the recolor shader

## File Structure

```
Assets/
├── Editor/
│   ├── ExhibitionSceneBuilder.cs    # Scene generation
│   ├── ExhibitionPrefabBuilder.cs   # Prefab generation
│   └── ExhibitionTestDataBuilder.cs # Test data generation
├── Scripts/ExhibitionSystem/
│   ├── Core/
│   │   ├── ExhibitionManager.cs     # Main game logic
│   │   └── VisitorCharacterGenerator.cs
│   ├── Data/
│   │   ├── ExhibitItemData.cs       # Item ScriptableObject
│   │   └── ExhibitionTheme.cs       # Theme ScriptableObject
│   └── UI/
│       ├── VisitorPanel.cs          # Visitor display with character
│       ├── ShelfPanel.cs            # Item shelf
│       ├── DisplayPanel.cs          # Display slots
│       └── ...
└── Resources/Exhibitions/
    ├── Items/                       # ExhibitItemData assets
    ├── Themes/                      # ExhibitionTheme assets
    └── Prefabs/                     # Generated UI prefabs
```

## Troubleshooting

| Problem | Solution |
|---------|----------|
| Missing script errors | Re-run "Rebuild Prefabs" then "Build Exhibition Scene" |
| No visitor character appears | Ensure CharacterBase assets exist in `Resources/Characters/` |
| Empty theme list | Run "Generate Test Data" first |
| Items not draggable | Check that InputSystemUIInputModule is on EventSystem |

## Notes

- This system uses Unity's new Input System (not legacy Input)
- UI is designed for 1920x1080 reference resolution
- Character rendering uses RenderTexture + Graphics.Blit
