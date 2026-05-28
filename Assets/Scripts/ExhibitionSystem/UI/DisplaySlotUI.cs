using ExhibitionSystem.Core;
using ExhibitionSystem.Data;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ExhibitionSystem.UI
{
    /// <summary>
    /// UI slot for display area (curation slots).
    /// Accepts dropped items and shows feedback during evaluation.
    /// Also supports dragging items out to return to shelf.
    /// </summary>
    public class DisplaySlotUI : DropSlot,
        IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        // ── Serialized Fields ───────────────────────────────────────────────────

        [Header("Display Slot")]
        [SerializeField] private Image _frameImage;
        [SerializeField] private Image _itemIcon;
        [SerializeField] private Image _statusBadge;
        [SerializeField] private Sprite _defaultFrameSprite;
        [SerializeField] private Sprite _hoverFrameSprite;

        [Header("Status Badge Colors")]
        [SerializeField] private Color _badgeDefaultColor = Color.white;
        [SerializeField] private Color _badgeCorrectColor = new Color(0.3f, 0.9f, 0.3f, 1f);
        [SerializeField] private Color _badgeIncorrectColor = new Color(0.9f, 0.3f, 0.3f, 1f);

        [Header("Drag Settings")]
        [SerializeField] private Vector2 _ghostSize = new(80, 80);

        // ── Runtime State ───────────────────────────────────────────────────────

        private ExhibitItemData _placedItem;
        private CanvasGroup _canvasGroup;
        private Canvas _rootCanvas;
        private bool _isDragging;
        private bool _isLocked;
        private static GameObject _dragGhost;

        // ── Public Properties ───────────────────────────────────────────────────

        public ExhibitItemData PlacedItem => _placedItem;
        public bool HasItem => _placedItem != null;
        public bool IsLocked => _isLocked;

        // ── Unity Lifecycle ─────────────────────────────────────────────────────

        protected override void Awake()
        {
            base.Awake();

            if (_frameImage == null)
                _frameImage = GetComponent<Image>();

            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();

            _rootCanvas = GetComponentInParent<Canvas>();
            while (_rootCanvas != null && !_rootCanvas.isRootCanvas)
            {
                var parent = _rootCanvas.transform.parent;
                _rootCanvas = parent != null ? parent.GetComponentInParent<Canvas>() : null;
            }

            ClearItem();
        }

        // ── Public Methods ──────────────────────────────────────────────────────

        /// <summary>
        /// Sets the item displayed in this slot.
        /// </summary>
        public void SetItem(ExhibitItemData item)
        {
            _placedItem = item;

            if (_itemIcon != null)
            {
                if (item != null && item.icon != null)
                {
                    _itemIcon.sprite = item.icon;
                    _itemIcon.enabled = true;
                    _itemIcon.preserveAspect = true;
                    _itemIcon.rectTransform.localScale = Vector3.one * DraggableItem.GetIconScale(item);
                }
                else
                {
                    _itemIcon.sprite = null;
                    _itemIcon.enabled = false;
                    _itemIcon.rectTransform.localScale = Vector3.one;
                }
            }

            SetFrameSprite(_defaultFrameSprite);

            // Hide status badge when setting item (only show during validation)
            if (_statusBadge != null)
                _statusBadge.enabled = false;
        }

        public void SetLocked(bool locked)
        {
            _isLocked = locked;

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = locked ? 0.75f : 1f;
                _canvasGroup.blocksRaycasts = !locked;
            }
        }

        /// <summary>
        /// Clears the item from this slot.
        /// </summary>
        public void ClearItem()
        {
            _placedItem = null;

            if (_itemIcon != null)
            {
                _itemIcon.sprite = null;
                _itemIcon.enabled = false;
                _itemIcon.rectTransform.localScale = Vector3.one;
            }

            SetFrameSprite(_defaultFrameSprite);

            // Hide status badge
            if (_statusBadge != null)
                _statusBadge.enabled = false;

            ClearFeedback();
        }

        /// <summary>
        /// Shows feedback via status badge color (green = correct, red = incorrect).
        /// </summary>
        public void ShowFeedback(bool isCorrect)
        {
            if (_statusBadge == null) return;

            // Always show badge when showing feedback
            _statusBadge.enabled = true;
            _statusBadge.color = isCorrect ? _badgeCorrectColor : _badgeIncorrectColor;
        }

        /// <summary>
        /// Clears the feedback and resets status badge.
        /// </summary>
        public void ClearFeedback()
        {
            if (_statusBadge != null)
            {
                _statusBadge.color = _badgeDefaultColor;
                _statusBadge.enabled = false;
            }
        }

        // ── DropSlot Implementation ─────────────────────────────────────────────

        public override void OnDrop(PointerEventData eventData)
        {
            var item = DraggableItem.CurrentlyDragging;
            if (item == null) return;

            if (!CanAcceptDrop(item)) return;

            // Place the item
            ExhibitionManager.Instance?.PlaceItem(_slotIndex, item);

            SetHighlight(HighlightState.Normal);
        }

        protected override bool CanAcceptDrop(ExhibitItemData item)
        {
            if (item == null) return false;
            if (_isLocked) return false;

            var manager = ExhibitionManager.Instance;
            if (manager == null) return false;

            // Can't drop during exhibition
            if (manager.IsRunning) return false;

            return true;
        }

        protected override void SetHighlight(HighlightState state)
        {
            if (_highlight != null)
            {
                _highlight.enabled = false;
                _highlight.color = _normalColor;
            }

            SetFrameSprite(state == HighlightState.Valid ? _hoverFrameSprite : _defaultFrameSprite);
        }

        // ── Drag Handlers (to remove item) ──────────────────────────────────────

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_placedItem == null)
            {
                eventData.pointerDrag = null;
                return;
            }

            if (_isLocked)
            {
                eventData.pointerDrag = null;
                return;
            }

            var manager = ExhibitionManager.Instance;
            if (manager != null && manager.IsRunning)
            {
                eventData.pointerDrag = null;
                return;
            }

            _isDragging = true;

            // Set static drag state so drop targets know what's being dragged
            DraggableItem.CurrentlyDragging = _placedItem;
            DraggableItem.CurrentDragSource = null; // We're not a DraggableItem

            // Store the item being dragged
            var draggedItem = _placedItem;

            // Remove from manager
            ExhibitionManager.Instance?.RemoveItem(_slotIndex);

            // Create ghost
            CreateDragGhost(draggedItem, eventData);

            // Fade slot
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0.4f;
                _canvasGroup.blocksRaycasts = false;
            }

            // Hide tooltip
            ItemTooltip.Instance?.Hide();
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_isDragging) return;
            MoveGhostToPointer(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!_isDragging) return;

            _isDragging = false;

            // Clear static drag state
            DraggableItem.CurrentlyDragging = null;
            DraggableItem.CurrentDragSource = null;

            // Restore slot
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = _isLocked ? 0.75f : 1f;
                _canvasGroup.blocksRaycasts = !_isLocked;
            }

            // Destroy ghost
            DestroyDragGhost();

            SetFrameSprite(_defaultFrameSprite);
        }

        // ── Pointer Handlers ────────────────────────────────────────────────────

        public override void OnPointerEnter(PointerEventData eventData)
        {
            base.OnPointerEnter(eventData);

            // Show tooltip for placed item
            if (_placedItem != null && DraggableItem.CurrentlyDragging == null)
                ItemTooltip.Instance?.Show(_placedItem, transform.position);
        }

        public override void OnPointerExit(PointerEventData eventData)
        {
            base.OnPointerExit(eventData);
            ItemTooltip.Instance?.Hide();
        }

        // ── Ghost Management ────────────────────────────────────────────────────

        private void CreateDragGhost(ExhibitItemData item, PointerEventData eventData)
        {
            if (_rootCanvas == null || item?.icon == null) return;

            _dragGhost = new GameObject("DisplayDragGhost");
            _dragGhost.transform.SetParent(_rootCanvas.transform, false);

            var rt = _dragGhost.AddComponent<RectTransform>();
            rt.sizeDelta = _ghostSize * DraggableItem.GetIconScale(item);

            var img = _dragGhost.AddComponent<Image>();
            img.sprite = item.icon;
            img.preserveAspect = true;
            img.raycastTarget = false;

            var cg = _dragGhost.AddComponent<CanvasGroup>();
            cg.alpha = 0.8f;
            cg.blocksRaycasts = false;
            cg.interactable = false;

            MoveGhostToPointer(eventData);
        }

        private void MoveGhostToPointer(PointerEventData eventData)
        {
            if (_dragGhost == null || _rootCanvas == null) return;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _rootCanvas.GetComponent<RectTransform>(),
                eventData.position,
                _rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : eventData.pressEventCamera,
                out Vector2 localPoint);

            _dragGhost.GetComponent<RectTransform>().localPosition = localPoint;
        }

        private static void DestroyDragGhost()
        {
            if (_dragGhost != null)
            {
                Destroy(_dragGhost);
                _dragGhost = null;
            }
        }

        private void SetFrameSprite(Sprite sprite)
        {
            if (_frameImage == null || sprite == null) return;

            _frameImage.sprite = sprite;
            _frameImage.color = Color.white;
            _frameImage.preserveAspect = false;
        }
    }
}
