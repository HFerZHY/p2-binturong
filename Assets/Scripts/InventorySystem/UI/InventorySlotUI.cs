using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace InventorySystem.UI
{
    /// <summary>
    /// A single inventory slot cell. Derives from <see cref="TabItem"/> so it
    /// participates fully in <see cref="TabGroup"/> selection — hover highlights
    /// the detail panel, click selects the slot.
    ///
    /// Requires an additional <see cref="Image"/> component (beyond the one
    /// already inside the inherited <see cref="Button"/>) to display the item icon.
    /// In the prefab, the icon Image should be a dedicated child object so it does
    /// not conflict with the Button's targetGraphic.
    ///
    /// PREFAB STRUCTURE (recommended)
    ///   InventorySlotUI  ← this component + Button (from TabItem) + LayoutElement
    ///   ├── Icon         ← Image  →  _iconImage  (assign in Inspector)
    ///   ├── ItemName     ← TextMeshProUGUI  →  _nameText
    ///   └── Quantity     ← TextMeshProUGUI  →  _quantityText
    /// </summary>
    [RequireComponent(typeof(Image))]   // icon image on this or a child
    public class InventorySlotUI : TabItem
    {
        [Header("Slot Visuals")]
        [SerializeField] private Image            _iconImage;
        [SerializeField] private TextMeshProUGUI  _nameText;
        [SerializeField] private TextMeshProUGUI  _quantityText;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>Populates the cell with item data.</summary>
        public void SetData(Sprite icon, string displayName, int quantity)
        {
            if (_iconImage != null)
            {
                _iconImage.sprite  = icon;
                _iconImage.enabled = icon != null;
            }

            if (_nameText     != null) _nameText.text     = displayName;
            if (_quantityText != null) _quantityText.text = quantity > 1 ? quantity.ToString() : string.Empty;
        }

        /// <summary>Resets the cell to an empty/invisible state.</summary>
        public void Clear()
        {
            if (_iconImage != null) { _iconImage.sprite = null; _iconImage.enabled = false; }
            if (_nameText     != null) _nameText.text     = string.Empty;
            if (_quantityText != null) _quantityText.text = string.Empty;
        }
    }
}