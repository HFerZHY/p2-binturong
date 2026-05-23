using ExhibitionSystem.Data;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ExhibitionSystem.UI
{
    /// <summary>
    /// Base class for drop target slots.
    /// Handles drop acceptance and visual feedback.
    /// </summary>
    public abstract class DropSlot : MonoBehaviour,
        IDropHandler, IPointerEnterHandler, IPointerExitHandler
    {
        // ── Serialized Fields ───────────────────────────────────────────────────

        [Header("Drop Slot Visual")]
        [SerializeField] protected Image _highlight;
        [SerializeField] protected Color _normalColor = new(1, 1, 1, 0.1f);
        [SerializeField] protected Color _hoverColor = new(1, 1, 0.5f, 0.4f);
        [SerializeField] protected Color _invalidColor = new(1, 0.3f, 0.3f, 0.4f);

        // ── Runtime State ───────────────────────────────────────────────────────

        protected int _slotIndex;

        // ── Public Properties ───────────────────────────────────────────────────

        public int SlotIndex => _slotIndex;

        // ── Unity Lifecycle ─────────────────────────────────────────────────────

        protected virtual void Awake()
        {
            SetHighlight(HighlightState.Normal);
        }

        // ── Public Methods ──────────────────────────────────────────────────────

        /// <summary>
        /// Sets the slot index for this drop slot.
        /// </summary>
        public void SetSlotIndex(int index)
        {
            _slotIndex = index;
        }

        // ── Abstract Methods ────────────────────────────────────────────────────

        /// <summary>
        /// Called when an item is dropped on this slot.
        /// </summary>
        public abstract void OnDrop(PointerEventData eventData);

        /// <summary>
        /// Returns true if this slot can accept the given item.
        /// </summary>
        protected abstract bool CanAcceptDrop(ExhibitItemData item);

        // ── Pointer Handlers ────────────────────────────────────────────────────

        public virtual void OnPointerEnter(PointerEventData eventData)
        {
            if (DraggableItem.CurrentlyDragging == null) return;

            bool canAccept = CanAcceptDrop(DraggableItem.CurrentlyDragging);
            SetHighlight(canAccept ? HighlightState.Valid : HighlightState.Invalid);
        }

        public virtual void OnPointerExit(PointerEventData eventData)
        {
            SetHighlight(HighlightState.Normal);
        }

        // ── Visual Feedback ─────────────────────────────────────────────────────

        protected enum HighlightState
        {
            Normal,
            Valid,
            Invalid
        }

        protected void SetHighlight(HighlightState state)
        {
            if (_highlight == null) return;

            _highlight.color = state switch
            {
                HighlightState.Valid => _hoverColor,
                HighlightState.Invalid => _invalidColor,
                _ => _normalColor
            };
        }
    }
}
