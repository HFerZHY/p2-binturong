using System;

namespace InventorySystem.Data
{
    /// <summary>
    /// Represents one logical slot in the player's inventory.
    /// A slot holds a reference to an ItemData template and a quantity.
    /// There is no per-slot stack limit; quantity is unbounded.
    /// </summary>
    [Serializable]
    public class InventorySlot
    {
        public ItemData item;
        public int quantity;

        public InventorySlot(ItemData item, int quantity)
        {
            this.item     = item;
            this.quantity = quantity;
        }

        /// <summary>True when the slot has been fully depleted and should be removed.</summary>
        public bool IsEmpty => quantity <= 0;
    }
}
