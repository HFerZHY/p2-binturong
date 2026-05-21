using InventorySystem.Data;
using UnityEngine;

namespace StationWork.Data
{
    /// <summary>
    /// Defines one passenger encounter in the station work phase.
    /// Create via Assets > Otowa > Passenger Data.
    /// </summary>
    [CreateAssetMenu(fileName = "NewPassenger", menuName = "Otowa/Passenger Data")]
    public class PassengerData : ScriptableObject
    {
        [Tooltip("Display name shown in the UI.")]
        public string passengerName;

        [Tooltip("Optional avatar shown next to the dialogue.")]
        public Sprite avatar;

        [TextArea(3, 6)]
        [Tooltip("What the passenger says when they arrive.")]
        public string dialogue;

        [Tooltip("The label they are asking for (must match a label string in an ItemData).")]
        public string requiredLabel;

        [Tooltip("The item that satisfies this passenger's request.")]
        public ItemData correctItem;
    }
}
