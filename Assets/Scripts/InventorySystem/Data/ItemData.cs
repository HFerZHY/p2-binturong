using UnityEngine;

namespace InventorySystem.Data
{
    /// <summary>
    /// ScriptableObject representing a single item type in the game.
    ///
    /// LOCALIZATION
    ///   nameKey        — key into LocalizationManager.Instance.itemNameTable.
    ///   descriptionKey — key into LocalizationManager.Instance.itemDescriptionTable.
    ///   At runtime, call LocalizationManager.Instance.GetItemName(nameKey) /
    ///   GetItemDescription(descriptionKey) to get locale-correct strings.
    ///
    /// IDENTITY
    ///   nameKey doubles as the human-readable designer identifier and the lookup
    ///   key in the localization table. ItemInspector enforces uniqueness across
    ///   all ItemData assets, matching the same pattern used by CharacterInspector.
    /// </summary>
    [CreateAssetMenu(fileName = "NewItem", menuName = "Inventory/Item Data")]
    public class ItemData : ScriptableObject
    {
        [Tooltip("Localization key for this item's display name. " +
                 "Also used as the unique identifier for this asset across all ItemData.")]
        public string nameKey;

        [Tooltip("Localization key for this item's description text.")]
        public string descriptionKey;

        [Tooltip("Icon sprite shown in the inventory UI.")]
        public Sprite icon;
    }
}
