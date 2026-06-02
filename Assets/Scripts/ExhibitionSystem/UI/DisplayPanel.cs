using System.Collections.Generic;
using ExhibitionSystem.Core;
using ExhibitionSystem.Data;
using UnityEngine;
using UnityEngine.UI;

namespace ExhibitionSystem.UI
{
    /// <summary>
    /// Display area where each theme creates empty exhibit slots with editable labels.
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
            ExhibitionManager.OnThemeSelected += HandleThemeSelected;
            ExhibitionManager.OnDisplaySlotsInitialized += HandleDisplaySlotsInitialized;
            ExhibitionManager.OnSlotInspirationChanged += HandleSlotInspirationChanged;
            ExhibitionManager.OnItemPlaced += HandleItemPlaced;
            ExhibitionManager.OnItemRemoved += HandleItemRemoved;
            ExhibitionManager.OnItemsSwapped += HandleItemsSwapped;
            ExhibitionManager.OnExhibitionStarted += HandleExhibitionStarted;
            ExhibitionManager.OnVisitorReacted += HandleVisitorReacted;
            ExhibitionManager.OnCurationCleared += HandleCurationCleared;
        }

        private void OnDisable()
        {
            ExhibitionManager.OnThemeSelected -= HandleThemeSelected;
            ExhibitionManager.OnDisplaySlotsInitialized -= HandleDisplaySlotsInitialized;
            ExhibitionManager.OnSlotInspirationChanged -= HandleSlotInspirationChanged;
            ExhibitionManager.OnItemPlaced -= HandleItemPlaced;
            ExhibitionManager.OnItemRemoved -= HandleItemRemoved;
            ExhibitionManager.OnItemsSwapped -= HandleItemsSwapped;
            ExhibitionManager.OnExhibitionStarted -= HandleExhibitionStarted;
            ExhibitionManager.OnVisitorReacted -= HandleVisitorReacted;
            ExhibitionManager.OnCurationCleared -= HandleCurationCleared;
        }

        public void RebuildSlots(int slotCount)
        {
            foreach (var slot in _slots)
            {
                if (slot != null)
                    Destroy(slot.gameObject);
            }
            _slots.Clear();

            if (_slotPrefab == null || _slotContainer == null || slotCount <= 0)
                return;

            ConfigureGrid(slotCount);

            var manager = ExhibitionManager.Instance;
            for (int i = 0; i < slotCount; i++)
            {
                var slot = Instantiate(_slotPrefab, _slotContainer);
                var inspiration = manager != null && i < manager.SlotInspirations.Count
                    ? manager.SlotInspirations[i]
                    : null;
                var item = manager != null && i < manager.DisplaySlots.Count
                    ? manager.DisplaySlots[i]
                    : null;
                slot.SetData(i, inspiration, item);
                if (manager != null && manager.TryGetSlotValidation(i, out var validation))
                    slot.ShowFeedback(validation);
                _slots.Add(slot);
            }
        }

        public void ClearAllFeedback()
        {
            foreach (var slot in _slots)
                slot?.ClearFeedback();
        }

        private void HandleThemeSelected(ExhibitionTheme theme)
        {
            if (theme == null)
                RebuildSlots(0);
        }

        private void HandleDisplaySlotsInitialized(int slotCount)
        {
            RebuildSlots(slotCount);
        }

        private void HandleSlotInspirationChanged(int slotIndex, InspirationData inspiration)
        {
            if (slotIndex < 0 || slotIndex >= _slots.Count) return;
            _slots[slotIndex].SetInspiration(inspiration);
            _slots[slotIndex].ClearFeedback();
        }

        private void HandleItemPlaced(int slotIndex, ExhibitItemData item)
        {
            if (slotIndex < 0 || slotIndex >= _slots.Count) return;
            _slots[slotIndex].SetItem(item);
            _slots[slotIndex].ClearFeedback();
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
            {
                _slots[i].SetItem(manager.DisplaySlots[i]);
                _slots[i].ClearFeedback();
            }
        }

        private void HandleExhibitionStarted()
        {
            var manager = ExhibitionManager.Instance;
            for (int i = 0; i < _slots.Count; i++)
            {
                if (manager != null && manager.TryGetSlotValidation(i, out var validation))
                    _slots[i].ShowFeedback(validation);
                else
                    _slots[i].ClearFeedback();
            }
        }

        private void HandleVisitorReacted(
            int slotIndex,
            InspirationData inspiration,
            ExhibitItemData item,
            ExhibitionSlotValidation validation,
            int satisfaction)
        {
            if (slotIndex < 0 || slotIndex >= _slots.Count) return;
            _slots[slotIndex].ShowFeedback(validation);
        }

        private void HandleCurationCleared()
        {
            RebuildSlots(0);
        }

        private void ConfigureGrid(int slotCount)
        {
            if (_gridLayout == null) return;

            _gridLayout.constraintCount = Mathf.Clamp(slotCount, 1, 4);
            _gridLayout.cellSize = new Vector2(240f, 230f);
        }
    }
}
