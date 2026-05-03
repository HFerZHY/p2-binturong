using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace InventorySystem.UI
{
    /// <summary>
    /// View component for a single slot cell in the inventory grid.
    ///
    /// Bind via Inspector to the Image and TextMeshProUGUI children of the
    /// slot prefab.  InventoryUIManager drives all data; this component is
    /// purely presentational.
    /// </summary>
    public class InventorySlotUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Image         _iconImage;
        [SerializeField] private TextMeshProUGUI _quantityText;
        [SerializeField] private TextMeshProUGUI _nameText;

        /// <summary>
        /// Populates the cell with item data.
        /// </summary>
        /// <param name="icon">Sprite to display.</param>
        /// <param name="displayName">Localized item name.</param>
        /// <param name="quantity">Stack size.</param>
        public void SetData(Sprite icon, string displayName, int quantity)
        {
            if (_iconImage != null)
            {
                _iconImage.sprite  = icon;
                _iconImage.enabled = icon != null;
            }

            if (_nameText != null)
                _nameText.text = displayName;

            if (_quantityText != null)
                _quantityText.text = quantity > 1 ? quantity.ToString() : string.Empty;
        }

        /// <summary>Clears the cell to an empty state.</summary>
        public void Clear()
        {
            if (_iconImage != null)
            {
                _iconImage.sprite  = null;
                _iconImage.enabled = false;
            }

            if (_nameText     != null) _nameText.text     = string.Empty;
            if (_quantityText != null) _quantityText.text = string.Empty;
        }
    }
}
