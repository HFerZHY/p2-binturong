using System.Collections.Generic;
using System.Linq;
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
        [SerializeField] private List<ShelfSlotUI> _manualSlots = new();

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
            ExhibitionManager.OnCurationCleared += HandleCurationCleared;
        }

        private void OnDisable()
        {
            ExhibitionManager.OnInspirationsConfirmed -= HandleInspirationsConfirmed;
            ExhibitionManager.OnItemPlaced -= HandleItemPlaced;
            ExhibitionManager.OnItemRemoved -= HandleItemRemoved;
            ExhibitionManager.OnCurationCleared -= HandleCurationCleared;
        }

        private void Start()
        {
            RebuildSlots();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (Application.isPlaying || _manualSlots == null || _manualSlots.Count == 0)
                return;

            var previewItems = Resources.LoadAll<ExhibitItemData>("Exhibitions/Items")
                .OrderBy(item => item.sortOrder)
                .ToList();

            for (int i = 0; i < _manualSlots.Count; i++)
            {
                var slot = _manualSlots[i];
                if (slot == null) continue;

                slot.SetSlotIndex(i);
                slot.SetData(i < previewItems.Count ? previewItems[i] : null);
            }
        }
#endif

        // ── Public Methods ──────────────────────────────────────────────────────

        /// <summary>
        /// Rebuilds all shelf slots from the ExhibitionManager's item list.
        /// </summary>
        public void RebuildSlots()
        {
            var manager = ExhibitionManager.Instance;
            if (manager == null) return;

            var allItems = manager.AllItems;
            int totalSlots = _columns * _rows;

            if (_manualSlots != null && _manualSlots.Count > 0)
            {
                _slots.Clear();

                for (int i = 0; i < _manualSlots.Count; i++)
                {
                    var slot = _manualSlots[i];
                    if (slot == null) continue;

                    slot.SetSlotIndex(i);
                    slot.gameObject.SetActive(i < totalSlots);
                    SetSlotItem(slot, i, allItems, manager);
                    _slots.Add(slot);
                }

                return;
            }

            foreach (var slot in _slots)
            {
                if (slot != null)
                    Destroy(slot.gameObject);
            }
            _slots.Clear();

            if (_slotPrefab == null || _gridContainer == null) return;

            for (int i = 0; i < totalSlots; i++)
            {
                var slot = Instantiate(_slotPrefab, _gridContainer);
                slot.SetSlotIndex(i);
                SetSlotItem(slot, i, allItems, manager);
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
            RefreshSlotStates();
        }

        private void HandleItemRemoved(int slotIndex)
        {
            // Refresh all states when item is removed
            RefreshSlotStates();
        }

        private void HandleCurationCleared()
        {
            RefreshSlotStates();
        }

        private static void SetSlotItem(ShelfSlotUI slot, int index, IReadOnlyList<ExhibitItemData> allItems, ExhibitionManager manager)
        {
            if (slot == null) return;

            if (index < allItems.Count)
            {
                var item = allItems[index];
                slot.SetData(item);
                slot.SetPlacedState(manager.IsItemPlaced(item));
            }
            else
            {
                slot.SetData(null);
            }
        }
    }
}
