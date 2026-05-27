using System.Collections.Generic;
using ExhibitionSystem.Core;
using ExhibitionSystem.Data;
using UnityEngine;
using UnityEngine.UI;

namespace ExhibitionSystem.UI
{
    /// <summary>
    /// Panel containing the 4x4 grid of shelf slots.
    /// Displays all available exhibit items for dragging.
    /// </summary>
    public class ShelfPanel : MonoBehaviour
    {
        // ── Serialized Fields ───────────────────────────────────────────────────

        [Header("References")]
        [SerializeField] private ShelfSlotUI _slotPrefab;
        [SerializeField] private Transform _gridContainer;

        [Header("Grid Settings")]
        [SerializeField] private int _columns = 4;
        [SerializeField] private int _rows = 4;

        // ── Runtime State ───────────────────────────────────────────────────────

        private readonly List<ShelfSlotUI> _slots = new();

        // ── Unity Lifecycle ─────────────────────────────────────────────────────

        private void OnEnable()
        {
            ExhibitionManager.OnInspirationsConfirmed += HandleInspirationsConfirmed;
            ExhibitionManager.OnItemPlaced += HandleItemPlaced;
            ExhibitionManager.OnItemRemoved += HandleItemRemoved;
        }

        private void OnDisable()
        {
            ExhibitionManager.OnInspirationsConfirmed -= HandleInspirationsConfirmed;
            ExhibitionManager.OnItemPlaced -= HandleItemPlaced;
            ExhibitionManager.OnItemRemoved -= HandleItemRemoved;
        }

        private void Start()
        {
            RebuildSlots();
        }

        // ── Public Methods ──────────────────────────────────────────────────────

        /// <summary>
        /// Rebuilds all shelf slots from the ExhibitionManager's item list.
        /// </summary>
        public void RebuildSlots()
        {
            // Clear existing slots
            foreach (var slot in _slots)
            {
                if (slot != null)
                    Destroy(slot.gameObject);
            }
            _slots.Clear();

            if (_slotPrefab == null || _gridContainer == null) return;

            // Get items from manager
            var manager = ExhibitionManager.Instance;
            if (manager == null) return;

            var allItems = manager.AllItems;

            // Create slots
            int totalSlots = _columns * _rows;
            for (int i = 0; i < totalSlots; i++)
            {
                var slot = Instantiate(_slotPrefab, _gridContainer);
                slot.SetSlotIndex(i);

                // Assign item if available
                if (i < allItems.Count)
                {
                    slot.SetData(allItems[i]);
                    slot.SetPlacedState(manager.IsItemPlaced(allItems[i]));
                }
                else
                {
                    slot.SetData(null);
                }

                _slots.Add(slot);
            }
        }

        /// <summary>
        /// Refreshes the visual state of all slots.
        /// </summary>
        public void RefreshSlotStates()
        {
            var manager = ExhibitionManager.Instance;
            if (manager == null) return;

            foreach (var slot in _slots)
            {
                if (slot == null || slot.ItemData == null) continue;
                slot.SetPlacedState(manager.IsItemPlaced(slot.ItemData));
            }
        }

        // ── Event Handlers ──────────────────────────────────────────────────────

        private void HandleInspirationsConfirmed(IReadOnlyList<InspirationData> inspirations)
        {
            RefreshSlotStates();
        }

        private void HandleItemPlaced(int slotIndex, ExhibitItemData item)
        {
            // Find the shelf slot with this item and mark as placed
            foreach (var slot in _slots)
            {
                if (slot != null && slot.ItemData == item)
                {
                    slot.SetPlacedState(true);
                    break;
                }
            }
        }

        private void HandleItemRemoved(int slotIndex)
        {
            // Refresh all states when item is removed
            RefreshSlotStates();
        }
    }
}
