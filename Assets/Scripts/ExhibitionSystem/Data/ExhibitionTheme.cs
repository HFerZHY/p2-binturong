using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ExhibitionSystem.Data
{
    public enum InspirationSelectionMode
    {
        AnyFromPool,
        ExactSet
    }

    [Serializable]
    public class InspirationHint
    {
        public int inspirationId;

        [TextArea(2, 4)]
        public string hintText;
    }

    /// <summary>
    /// ScriptableObject defining a museum curation level.
    /// </summary>
    [CreateAssetMenu(fileName = "NewExhibition", menuName = "Museum/Exhibition Theme")]
    public class ExhibitionTheme : ScriptableObject
    {
        [Header("Display")]
        [Tooltip("Exhibition title, e.g., 'Summer Festival'")]
        public string title;

        [Tooltip("Localization key for the title")]
        public string titleKey;

        [TextArea(2, 4)]
        [Tooltip("Description of the exhibition theme")]
        public string description;

        [Tooltip("Localization key for the description")]
        public string descriptionKey;

        [Header("Level")]
        [Tooltip("Story day used for grouping in the test UI")]
        public int day = 2;

        [Tooltip("Number of inspirations the player must select")]
        [Range(1, 6)]
        public int requiredInspirations = 3;

        [Tooltip("How selected inspiration IDs are validated")]
        public InspirationSelectionMode selectionMode = InspirationSelectionMode.AnyFromPool;

        [Tooltip("IDs that are valid for this theme. ExactSet requires this exact set.")]
        public List<int> validInspirationIds = new();

        [Header("Hints")]
        [Tooltip("Theme-specific missing idea hints, keyed by inspiration ID")]
        public List<InspirationHint> missingIdeaHints = new();

        [TextArea(2, 4)]
        public string fallbackMissingHint = "Some of the ideas I need are still missing.";

        [TextArea(2, 4)]
        public string fallbackInvalidHint = "Something in this set of ideas does not fit the theme.";

        [Header("State")]
        [Tooltip("Whether this exhibition has been successfully completed")]
        [HideInInspector]
        public bool isCompleted;

        public int RequiredSlots => requiredInspirations;
        public int SuccessThreshold => requiredInspirations;

        public bool IsInspirationValid(int inspirationId)
        {
            return validInspirationIds != null && validInspirationIds.Contains(inspirationId);
        }

        public bool IsSelectionValid(IReadOnlyList<InspirationData> selectedInspirations)
        {
            if (selectedInspirations == null || selectedInspirations.Count != requiredInspirations)
                return false;

            var selectedIds = selectedInspirations.Select(inspiration => inspiration.id).ToList();
            if (selectionMode == InspirationSelectionMode.ExactSet)
                return SameSet(selectedIds, validInspirationIds);

            return selectedIds.All(IsInspirationValid);
        }

        public string GetHintForMissingId(int inspirationId)
        {
            if (missingIdeaHints != null)
            {
                var match = missingIdeaHints.FirstOrDefault(hint => hint.inspirationId == inspirationId);
                if (match != null && !string.IsNullOrWhiteSpace(match.hintText))
                    return match.hintText;
            }

            return fallbackMissingHint;
        }

        public string GetInvalidHint()
        {
            return string.IsNullOrWhiteSpace(fallbackInvalidHint)
                ? "Something in this set of ideas does not fit the theme."
                : fallbackInvalidHint;
        }

        public void MarkCompleted()
        {
            isCompleted = true;
        }

        public void ResetCompletion()
        {
            isCompleted = false;
        }

        private static bool SameSet(IReadOnlyCollection<int> a, IReadOnlyCollection<int> b)
        {
            if (a == null || b == null || a.Count != b.Count)
                return false;

            return !a.Except(b).Any() && !b.Except(a).Any();
        }
    }
}
