using UnityEngine;

namespace ExhibitionSystem.Data
{
    /// <summary>
    /// A clue sentence that maps to exactly one exhibit item.
    /// </summary>
    [CreateAssetMenu(fileName = "NewInspiration", menuName = "Museum/Inspiration")]
    public class InspirationData : ScriptableObject
    {
        [Tooltip("Stable story ID. IDs follow the writer-provided order.")]
        public int id;

        [TextArea(2, 5)]
        [Tooltip("Clue text shown to the player.")]
        public string text;

        [Tooltip("The one item that satisfies this inspiration.")]
        public ExhibitItemData mappedItem;

        [Tooltip("Whether this idea is unlocked in the current test scene.")]
        public bool isUnlocked = true;

        [TextArea(2, 4)]
        [Tooltip("Fallback hint if this idea is missing from a theme selection.")]
        public string fallbackHint = "Rin: (This idea might matter here.)";

        [Tooltip("Editor reset value only. Runtime match memory is stored in ExhibitionManager for the current Play Session.")]
        [HideInInspector]
        public bool isItemMatchKnownByDefault;
    }
}
