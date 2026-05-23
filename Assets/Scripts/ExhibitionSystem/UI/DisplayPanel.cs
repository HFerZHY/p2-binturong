using System.Collections.Generic;
using ExhibitionSystem.Core;
using ExhibitionSystem.Data;
using UnityEngine;
using UnityEngine.UI;

namespace ExhibitionSystem.UI
{
    /// <summary>
    /// Panel containing the display slots (4-6 curation slots).
    /// Dynamically adjusts slot count based on selected theme.
    /// </summary>
    public class DisplayPanel : MonoBehaviour
    {
        // ── Serialized Fields ───────────────────────────────────────────────────

        [Header("References")]
        [SerializeField] private DisplaySlotUI _slotPrefab;
        [SerializeField] private Transform _slotContainer;

        [Header("Evaluation Visual")]
        [SerializeField] private Color _evaluatingColor = new(1, 1, 0.5f, 0.3f);
        [SerializeField] private float _evaluationHighlightDuration = 0.5f;

        // ── Runtime State ───────────────────────────────────────────────────────

        private readonly List<DisplaySlotUI> _slots = new();
        private int _currentEvaluatingSlot = -1;

        // ── Public Properties ───────────────────────────────────────────────────

        public IReadOnlyList<DisplaySlotUI> Slots => _slots;
        public int SlotCount => _slots.Count;

        // ── Unity Lifecycle ─────────────────────────────────────────────────────

        private void OnEnable()
        {
            ExhibitionManager.OnThemeSelected += HandleThemeSelected;
            ExhibitionManager.OnItemPlaced += HandleItemPlaced;
            ExhibitionManager.OnItemRemoved += HandleItemRemoved;
            ExhibitionManager.OnItemsSwapped += HandleItemsSwapped;
            ExhibitionManager.OnExhibitionStarted += HandleExhibitionStarted;
            ExhibitionManager.OnVisitorReacted += HandleVisitorReacted;
            ExhibitionManager.OnExhibitionEnded += HandleExhibitionEnded;
        }

        private void OnDisable()
        {
            ExhibitionManager.OnThemeSelected -= HandleThemeSelected;
            ExhibitionManager.OnItemPlaced -= HandleItemPlaced;
            ExhibitionManager.OnItemRemoved -= HandleItemRemoved;
            ExhibitionManager.OnItemsSwapped -= HandleItemsSwapped;
            ExhibitionManager.OnExhibitionStarted -= HandleExhibitionStarted;
            ExhibitionManager.OnVisitorReacted -= HandleVisitorReacted;
            ExhibitionManager.OnExhibitionEnded -= HandleExhibitionEnded;
        }

        // ── Public Methods ──────────────────────────────────────────────────────

        /// <summary>
        /// Rebuilds slots for the given number of required slots.
        /// </summary>
        public void RebuildSlots(int slotCount)
        {
            // Clear existing
            foreach (var slot in _slots)
            {
                if (slot != null)
                    Destroy(slot.gameObject);
            }
            _slots.Clear();

            if (_slotPrefab == null || _slotContainer == null) return;

            // Create new slots
            for (int i = 0; i < slotCount; i++)
            {
                var slot = Instantiate(_slotPrefab, _slotContainer);
                slot.SetSlotIndex(i);
                slot.ClearItem();
                _slots.Add(slot);
            }
        }

        /// <summary>
        /// Clears all feedback icons from slots.
        /// </summary>
        public void ClearAllFeedback()
        {
            foreach (var slot in _slots)
                slot?.ClearFeedback();

            _currentEvaluatingSlot = -1;
        }

        // ── Event Handlers ──────────────────────────────────────────────────────

        private void HandleThemeSelected(ExhibitionTheme theme)
        {
            if (theme == null) return;

            RebuildSlots(theme.requiredSlots);
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
            // Re-sync from manager
            var manager = ExhibitionManager.Instance;
            if (manager == null) return;

            var displaySlots = manager.DisplaySlots;
            for (int i = 0; i < _slots.Count && i < displaySlots.Count; i++)
            {
                _slots[i].SetItem(displaySlots[i]);
            }
        }

        private void HandleExhibitionStarted()
        {
            ClearAllFeedback();
        }

        private void HandleVisitorReacted(int slotIndex, bool isCorrect, int satisfaction)
        {
            if (slotIndex < 0 || slotIndex >= _slots.Count) return;

            _currentEvaluatingSlot = slotIndex;
            _slots[slotIndex].ShowFeedback(isCorrect);
        }

        private void HandleExhibitionEnded(bool success, int satisfaction, int threshold)
        {
            _currentEvaluatingSlot = -1;
        }
    }
}
