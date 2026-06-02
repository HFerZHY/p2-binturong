using ExhibitionSystem.Data;
using UnityEngine;

namespace ExhibitionSystem.Core
{
    /// <summary>
    /// Applies the clean story state expected when the Day 2 exhibition opens.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class Day2ExhibitionInitializer : MonoBehaviour
    {
        private static readonly int[] Day2InspirationIds = { 7, 8, 10, 11, 12, 13, 14, 16 };

        private void Awake()
        {
            ApplyDay2Progress();
        }

        private void OnEnable()
        {
            ApplyDay2Progress();
        }

        private static void ApplyDay2Progress()
        {
            ExhibitionManager.ResetKnownInspirationMatches();

            foreach (var inspiration in Resources.LoadAll<InspirationData>("Exhibitions/Inspirations"))
            {
                if (inspiration != null)
                    inspiration.isUnlocked = System.Array.IndexOf(Day2InspirationIds, inspiration.id) >= 0;
            }

            foreach (var item in Resources.LoadAll<ExhibitItemData>("Exhibitions/Items"))
            {
                if (item != null)
                    item.isUnlocked = item.name != "Painting" && item.name != "OtowaBluesVinylRecord";
            }

            foreach (var theme in Resources.LoadAll<ExhibitionTheme>("Exhibitions/Themes"))
            {
                if (theme != null)
                    theme.ResetCompletion();
            }
        }
    }
}
