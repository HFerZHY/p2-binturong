using ExhibitionSystem.Data;
using UnityEngine;

namespace ExhibitionSystem.Core
{
    /// <summary>
    /// Applies the story progress expected when the Day 3 exhibition opens.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class Day3ExhibitionInitializer : MonoBehaviour
    {
        private static readonly int[] Day3KnownInspirationMatchIds = { 7, 8, 10, 12, 16 };

        private void Awake()
        {
            EnsureDay3KnownInspirationMatches();

            foreach (var inspiration in Resources.LoadAll<InspirationData>("Exhibitions/Inspirations"))
            {
                if (inspiration != null)
                    inspiration.isUnlocked = inspiration.id != 9;
            }

            foreach (var item in Resources.LoadAll<ExhibitItemData>("Exhibitions/Items"))
            {
                if (item != null)
                    item.isUnlocked = item.name != "OtowaBluesVinylRecord";
            }

            foreach (var theme in Resources.LoadAll<ExhibitionTheme>("Exhibitions/Themes"))
            {
                if (theme == null)
                    continue;

                if (theme.name == "Yuji" || theme.name == "SummerFestival")
                    theme.MarkCompleted();
                else
                    theme.ResetCompletion();
            }

            InspirationManager.Instance.SeedDay3JournalBaseline();
        }

        private void OnEnable()
        {
            EnsureDay3KnownInspirationMatches();
        }

        internal static void EnsureKnownInspirationMatchesIfLoaded()
        {
            var initializer = FindFirstObjectByType<Day3ExhibitionInitializer>(FindObjectsInactive.Include);
            if (initializer != null && initializer.isActiveAndEnabled)
                EnsureDay3KnownInspirationMatches();
        }

        private static void EnsureDay3KnownInspirationMatches()
        {
            ExhibitionManager.SeedKnownInspirationMatches(Day3KnownInspirationMatchIds);
        }
    }
}
