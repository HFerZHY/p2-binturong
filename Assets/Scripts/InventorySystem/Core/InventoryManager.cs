using System;
using System.Collections.Generic;
using System.Linq;
using Base;
using InventorySystem.Data;
using UnityEngine;

namespace InventorySystem
{
    /// <summary>
    /// Tracks every item the player is carrying.
    ///
    /// DESIGN
    ///   Items of the same ItemData are merged into a single InventorySlot whose
    ///   quantity is incremented/decremented accordingly.  There is no stack limit.
    ///
    ///   All mutation goes through Add / Remove / Clear so that InventoryChanged is
    ///   always fired and subscribers (e.g. InventoryUIManager) can redraw.
    ///
    /// USAGE
    ///   Wire this component into InventoryUIManager via the Inspector, or find it
    ///   via InventoryManager.Instance if you prefer a singleton pattern.
    /// </summary>
    public class InventoryManager : MonoSingleton<InventoryManager>
    {

        // ── State ────────────────────────────────────────────────────────────────

        /// <summary>All slots currently in the inventory. Read-only from outside.</summary>
        public IReadOnlyList<InventorySlot> Slots => _slots;

        private readonly List<InventorySlot> _slots = new();

        // ── Events ───────────────────────────────────────────────────────────────

        /// <summary>
        /// Fired whenever the inventory contents change (add, remove, or clear).
        /// Subscribers receive the full, up-to-date slot list.
        /// </summary>
        public event Action<IReadOnlyList<InventorySlot>> InventoryChanged;

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Adds <paramref name="amount"/> units of <paramref name="item"/> to the inventory.
        /// If a slot for that item already exists its quantity is incremented;
        /// otherwise a new slot is created.
        /// </summary>
        public void Add(ItemData item, int amount = 1)
        {
            if (item == null)
            {
                Debug.LogWarning("[InventoryManager] Add called with null ItemData.");
                return;
            }
            if (amount <= 0) return;

            var slot = _slots.FirstOrDefault(s => s.item == item);
            if (slot != null)
            {
                slot.quantity += amount;
            }
            else
            {
                _slots.Add(new InventorySlot(item, amount));
            }

            NotifyChanged();
        }

        /// <summary>
        /// Removes <paramref name="amount"/> units of <paramref name="item"/>.
        /// Returns true if the requested amount was available; false otherwise
        /// (inventory is not modified on failure).
        /// </summary>
        public bool Remove(ItemData item, int amount = 1)
        {
            if (item == null || amount <= 0) return false;

            var slot = _slots.FirstOrDefault(s => s.item == item);
            if (slot == null || slot.quantity < amount)
            {
                Debug.LogWarning(
                    $"[InventoryManager] Cannot remove {amount}× '{item.nameKey}' — " +
                    $"only {slot?.quantity ?? 0} available.");
                return false;
            }

            slot.quantity -= amount;
            if (slot.IsEmpty)
                _slots.Remove(slot);

            NotifyChanged();
            return true;
        }

        /// <summary>Returns how many of <paramref name="item"/> the player currently holds.</summary>
        public int GetCount(ItemData item)
            => _slots.FirstOrDefault(s => s.item == item)?.quantity ?? 0;

        /// <summary>Returns true if at least one unit of <paramref name="item"/> is in the inventory.</summary>
        public bool Has(ItemData item) => GetCount(item) > 0;

        /// <summary>Returns true if at least <paramref name="amount"/> units of <paramref name="item"/> are in the inventory.</summary>
        public bool Has(ItemData item, int amount)
            => item != null && amount > 0 && GetCount(item) >= amount;

        /// <summary> Returns true if at least quantity units of the item with the given nameKey are in the inventory.
        /// Used by dialogue conditions. </summary>
        public bool HasItem(string itemId, int quantity)
        {
            if (string.IsNullOrEmpty(itemId) || quantity <= 0)
                return false;

            var slot = _slots.FirstOrDefault(s =>
                s.item != null && s.item.nameKey == itemId);

            return slot != null && slot.quantity >= quantity;
}
            
        /// <summary>Empties the entire inventory.</summary>
        public void Clear()
        {
            _slots.Clear();
            NotifyChanged();
        }

        // ── Private helpers ────────────────────────────────────────────────────────

        private void NotifyChanged() => InventoryChanged?.Invoke(_slots);
    }
}
