using System.Collections.Generic;
using UnityEngine;

namespace ExhibitionSystem.Data
{
    /// <summary>
    /// ScriptableObject defining an exhibition theme with its correct items.
    /// Create via Assets > Create > Museum > Exhibition Theme.
    /// </summary>
    [CreateAssetMenu(fileName = "NewExhibition", menuName = "Museum/Exhibition Theme")]
    public class ExhibitionTheme : ScriptableObject
    {
        // ── Display Information ──────────────────────────────────────────────────

        [Tooltip("Exhibition title, e.g., 'Railway Heritage Exhibition'")]
        public string title;

        [Tooltip("Localization key for the title")]
        public string titleKey;

        [TextArea(2, 4)]
        [Tooltip("Description of the exhibition theme")]
        public string description;

        [Tooltip("Localization key for the description")]
        public string descriptionKey;

        // ── Configuration ────────────────────────────────────────────────────────

        [Tooltip("Number of display slots for this theme (4-6)")]
        [Range(4, 6)]
        public int requiredSlots = 4;

        [Tooltip("Items that count as correct for this exhibition (can be > requiredSlots)")]
        public List<ExhibitItemData> correctItems = new();

        // ── State ────────────────────────────────────────────────────────────────

        [Tooltip("Whether this exhibition has been successfully completed")]
        [HideInInspector]
        public bool isCompleted;

        // ── Computed Properties ──────────────────────────────────────────────────

        /// <summary>
        /// Satisfaction threshold for success: requiredSlots - 2 (minimum 1).
        /// </summary>
        public int SuccessThreshold => Mathf.Max(1, requiredSlots - 2);

        // ── Public Methods ───────────────────────────────────────────────────────

        /// <summary>
        /// Checks if the given item is correct for this theme.
        /// </summary>
        public bool IsItemCorrect(ExhibitItemData item)
        {
            return item != null && correctItems.Contains(item);
        }

        /// <summary>
        /// Marks this exhibition as completed.
        /// </summary>
        public void MarkCompleted()
        {
            isCompleted = true;
        }

        /// <summary>
        /// Resets the completion status (for testing/replay).
        /// </summary>
        public void ResetCompletion()
        {
            isCompleted = false;
        }
    }
}
