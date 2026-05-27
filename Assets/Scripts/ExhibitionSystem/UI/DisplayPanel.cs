using System.Collections.Generic;
using ExhibitionSystem.Core;
using ExhibitionSystem.Data;
using UnityEngine;
using UnityEngine.UI;

namespace ExhibitionSystem.UI
{
    /// <summary>
    /// Vertical display area where each selected inspiration owns one item slot.
    /// </summary>
    public class DisplayPanel : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private InspirationDisplaySlot _slotPrefab;
        [SerializeField] private Transform _slotContainer;
        [SerializeField] private GridLayoutGroup _gridLayout;

        private readonly List<InspirationDisplaySlot> _slots = new();

        public IReadOnlyList<InspirationDisplaySlot> Slots => _slots;
        public int SlotCount => _slots.Count;

        private void OnEnable()
        {
            ExhibitionManager.OnInspirationsConfirmed += HandleInspirationsConfirmed;
            ExhibitionManager.OnItemPlaced += HandleItemPlaced;
            ExhibitionManager.OnItemRemoved += HandleItemRemoved;
            ExhibitionManager.OnItemsSwapped += HandleItemsSwapped;
            ExhibitionManager.OnExhibitionStarted += HandleExhibitionStarted;
            ExhibitionManager.OnVisitorReacted += HandleVisitorReacted;
            ExhibitionManager.OnExhibitionEnded += HandleExhibitionEnded;
        }

        private void OnDisable()
        {
            ExhibitionManager.OnInspirationsConfirmed -= HandleInspirationsConfirmed;
            ExhibitionManager.OnItemPlaced -= HandleItemPlaced;
            ExhibitionManager.OnItemRemoved -= HandleItemRemoved;
            ExhibitionManager.OnItemsSwapped -= HandleItemsSwapped;
            ExhibitionManager.OnExhibitionStarted -= HandleExhibitionStarted;
            ExhibitionManager.OnVisitorReacted -= HandleVisitorReacted;
            ExhibitionManager.OnExhibitionEnded -= HandleExhibitionEnded;
        }

        public void RebuildSlots(IReadOnlyList<InspirationData> inspirations)
        {
            foreach (var slot in _slots)
            {
                if (slot != null)
                    Destroy(slot.gameObject);
            }
            _slots.Clear();

            if (_slotPrefab == null || _slotContainer == null || inspirations == null) return;

            ConfigureGrid(inspirations.Count);

            var manager = ExhibitionManager.Instance;
            for (int i = 0; i < inspirations.Count; i++)
            {
                var slot = Instantiate(_slotPrefab, _slotContainer);
                var item = manager != null && i < manager.DisplaySlots.Count ? manager.DisplaySlots[i] : null;
                bool locked = manager != null && manager.IsSlotLocked(i);
                slot.SetData(i, inspirations[i], item, locked);
                _slots.Add(slot);
            }
        }

        public void ClearAllFeedback()
        {
            foreach (var slot in _slots)
                slot?.ClearFeedback();
        }

        private void HandleInspirationsConfirmed(IReadOnlyList<InspirationData> inspirations)
        {
            RebuildSlots(inspirations);
        }

        private void HandleItemPlaced(int slotIndex, ExhibitItemData item)
        {
            if (slotIndex < 0 || slotIndex >= _slots.Count) return;
            _slots[slotIndex].SetItem(item);
        }

        private void HandleItemRemoved(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _slots.Count) return;
            _slots[slotIndex].ClearItem();
        }

        private void HandleItemsSwapped(int slotA, int slotB)
        {
            var manager = ExhibitionManager.Instance;
            if (manager == null) return;

            for (int i = 0; i < _slots.Count && i < manager.DisplaySlots.Count; i++)
                _slots[i].SetItem(manager.DisplaySlots[i]);
        }

        private void HandleExhibitionStarted()
        {
            ClearAllFeedback();
        }

        private void HandleVisitorReacted(int slotIndex, InspirationData inspiration, ExhibitItemData item, bool isCorrect, int satisfaction)
        {
            if (slotIndex < 0 || slotIndex >= _slots.Count) return;
            _slots[slotIndex].ShowFeedback(isCorrect);
        }

        private void HandleExhibitionEnded(bool success, int satisfaction, int threshold)
        {
        }

        private void ConfigureGrid(int slotCount)
        {
            if (_gridLayout == null) return;

            _gridLayout.constraintCount = Mathf.Clamp(slotCount, 1, 4);
            _gridLayout.cellSize = new Vector2(240f, 230f);
        }
    }
}
