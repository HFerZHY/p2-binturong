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
        private void Awake()
        {
            foreach (var inspiration in Resources.LoadAll<InspirationData>("Exhibitions/Inspirations"))
            {
                if (inspiration != null)
                    inspiration.isUnlocked = inspiration.id != 9;
            }

            foreach (var item in Resources.LoadAll<ExhibitItemData>("Exhibitions/Items"))
            {
                if (item != null)
                    item.isUnlocked = item.sortOrder != 16;
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
    }
}
