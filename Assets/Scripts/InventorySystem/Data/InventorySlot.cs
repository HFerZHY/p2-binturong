using System;
using System.Collections.Generic;

namespace InventorySystem.Data
{
    [Serializable]
    public class InventorySlot
    {
        public ItemData item;
        public int quantity;

        /// <summary>Labels permanently locked onto this item after correct passenger matches.</summary>
        public List<string> fixedLabels = new();

        /// <summary>Labels the player has manually dragged onto this item (not yet confirmed).</summary>
        public List<string> assignedLabels = new();

        public InventorySlot(ItemData item, int quantity)
        {
            this.item     = item;
            this.quantity = quantity;
        }

        public bool IsEmpty => quantity <= 0;

        public bool IsLabelFixed(string label) => fixedLabels.Contains(label);
        public bool IsLabelAssigned(string label) => assignedLabels.Contains(label);

        public void FixLabel(string label)
        {
            if (!fixedLabels.Contains(label))
                fixedLabels.Add(label);
            assignedLabels.Remove(label);
        }
    }
}
