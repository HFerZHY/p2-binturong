using UnityEngine;
using UnityEngine.EventSystems;

namespace ExhibitionSystem.UI
{
    /// <summary>
    /// UI slot for items in the shelf (inventory).
    /// Items can be dragged from here to display slots.
    /// </summary>
    public class ShelfSlotUI : DraggableItem
    {
        // ── Serialized Fields ───────────────────────────────────────────────────

        [Header("Shelf Slot")]
        [SerializeField] private float _placedAlpha = 0.3f;
        [SerializeField] private float _lockedAlpha = 0.5f;

        // ── Runtime State ───────────────────────────────────────────────────────

        private bool _isPlacedInDisplay;
        private int _slotIndex;

        // ── Public Properties ───────────────────────────────────────────────────

        public int SlotIndex => _slotIndex;
        public bool IsPlacedInDisplay => _isPlacedInDisplay;

        // ── Public Methods ──────────────────────────────────────────────────────

        /// <summary>
        /// Sets the slot index.
        /// </summary>
        public void SetSlotIndex(int index)
        {
            _slotIndex = index;
        }

        /// <summary>
        /// Sets whether this item is currently placed in a display slot.
        /// Adjusts visual appearance accordingly.
        /// </summary>
        public void SetPlacedState(bool placed)
        {
            _isPlacedInDisplay = placed;
            UpdateVisualState();
        }

        /// <summary>
        /// Override to prevent dragging items that are already placed or locked.
        /// </summary>
        public override bool CanDrag()
        {
            if (_itemData == null) return false;
            if (_isPlacedInDisplay) return false;
            if (!_itemData.isUnlocked) return false;
            return true;
        }

        // ── Drag Handlers ───────────────────────────────────────────────────────

        public override void OnBeginDrag(PointerEventData eventData)
        {
            if (!CanDrag())
            {
                eventData.pointerDrag = null;
                return;
            }

            base.OnBeginDrag(eventData);
        }

        public override void OnEndDrag(PointerEventData eventData)
        {
            base.OnEndDrag(eventData);
            UpdateVisualState();
        }

        // ── Private Methods ─────────────────────────────────────────────────────

        private void UpdateVisualState()
        {
            if (_canvasGroup == null) return;

            if (_itemData == null)
            {
                _canvasGroup.alpha = 0f;
            }
            else if (!_itemData.isUnlocked)
            {
                _canvasGroup.alpha = _lockedAlpha;
            }
            else if (_isPlacedInDisplay)
            {
                _canvasGroup.alpha = _placedAlpha;
            }
            else
            {
                _canvasGroup.alpha = 1f;
            }
        }
    }
}
