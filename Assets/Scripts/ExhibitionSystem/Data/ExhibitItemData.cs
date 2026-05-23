using System.Collections.Generic;
using UnityEngine;

namespace ExhibitionSystem.Data
{
    /// <summary>
    /// ScriptableObject representing an exhibit item in the museum.
    /// Create via Assets > Create > Museum > Exhibit Item.
    /// </summary>
    [CreateAssetMenu(fileName = "NewExhibitItem", menuName = "Museum/Exhibit Item")]
    public class ExhibitItemData : ScriptableObject
    {
        // ── Display Information ──────────────────────────────────────────────────

        [Tooltip("Display name for the item (fallback if localization not found)")]
        public string itemName;

        [Tooltip("Localization key for this item's display name")]
        public string nameKey;

        [Tooltip("Icon sprite shown in the shelf and display slots")]
        public Sprite icon;

        [TextArea(2, 4)]
        [Tooltip("Description shown on hover (fallback if localization not found)")]
        public string description;

        [Tooltip("Localization key for the description")]
        public string descriptionKey;

        // ── State ────────────────────────────────────────────────────────────────

        [Tooltip("Whether this item is available for use")]
        public bool isUnlocked = true;

        [Tooltip("List of exhibition titles where this item was successfully used")]
        [HideInInspector]
        public List<string> usedInExhibitions = new();

        // ── Public Methods ───────────────────────────────────────────────────────

        /// <summary>
        /// Records that this item was successfully used in an exhibition.
        /// </summary>
        public void RecordUsage(string exhibitionTitle)
        {
            if (!string.IsNullOrEmpty(exhibitionTitle) && !usedInExhibitions.Contains(exhibitionTitle))
                usedInExhibitions.Add(exhibitionTitle);
        }

        /// <summary>
        /// Checks if this item has been used in the specified exhibition.
        /// </summary>
        public bool WasUsedIn(string exhibitionTitle)
        {
            return usedInExhibitions.Contains(exhibitionTitle);
        }

        /// <summary>
        /// Clears the usage history (for testing/reset).
        /// </summary>
        public void ClearUsageHistory()
        {
            usedInExhibitions.Clear();
        }
    }
}
