using ExhibitionSystem.Data;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

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
        [SerializeField] private float _lockedAlpha = 0.5f;

        // ── Runtime State ───────────────────────────────────────────────────────

        private bool _isPlacedInDisplay;
        private int _slotIndex;

        // ── Public Properties ───────────────────────────────────────────────────

        public int SlotIndex => _slotIndex;
        public bool IsPlacedInDisplay => _isPlacedInDisplay;

        protected override void Awake()
        {
            base.Awake();
            EnsureIconReference();
            ConfigureRootImage();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            EnsureIconReference();
            ConfigureRootImage();

            if (_icon != null)
                _icon.enabled = _icon.sprite != null;
        }
#endif

        // ── Public Methods ──────────────────────────────────────────────────────

        public override void SetData(ExhibitItemData item)
        {
            _itemData = item;
            EnsureIconReference();
            ConfigureRootImage();

            if (_icon != null)
            {
                _icon.sprite = item?.icon;
                _icon.enabled = item != null && item.icon != null;
                _icon.preserveAspect = true;
                _icon.raycastTarget = true;
            }
        }

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
            else
            {
                _canvasGroup.alpha = 1f;
            }

            if (_icon != null)
                _icon.enabled = _itemData != null && _itemData.icon != null && !_isPlacedInDisplay;
        }

        private void ConfigureRootImage()
        {
            if (_icon == null) return;

            _icon.color = Color.white;
            _icon.preserveAspect = true;
            _icon.raycastTarget = true;
        }

        private void EnsureIconReference()
        {
            if (_icon != null) return;

            _icon = GetComponent<Image>();
            if (_icon != null) return;

            var iconTransform = transform.Find("Icon");
            if (iconTransform != null)
                _icon = iconTransform.GetComponent<Image>();
        }
    }
}
