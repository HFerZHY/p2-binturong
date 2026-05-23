using ExhibitionSystem.Data;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ExhibitionSystem.UI
{
    /// <summary>
    /// Base class for draggable UI items.
    /// Handles drag-and-drop with ghost image and tooltip triggers.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class DraggableItem : MonoBehaviour,
        IBeginDragHandler, IDragHandler, IEndDragHandler,
        IPointerEnterHandler, IPointerExitHandler
    {
        // ── Static Drag State ───────────────────────────────────────────────────

        /// <summary>Currently dragged item data (null if not dragging).</summary>
        public static ExhibitItemData CurrentlyDragging { get; internal set; }

        /// <summary>The source DraggableItem being dragged.</summary>
        public static DraggableItem CurrentDragSource { get; internal set; }

        private static GameObject _dragGhost;

        // ── Serialized Fields ───────────────────────────────────────────────────

        [SerializeField] protected Image _icon;
        [SerializeField] private Vector2 _ghostSize = new(80, 80);

        // ── Runtime State ───────────────────────────────────────────────────────

        protected ExhibitItemData _itemData;
        protected CanvasGroup _canvasGroup;
        protected Canvas _rootCanvas;
        private bool _isDragging;

        // ── Public Properties ───────────────────────────────────────────────────

        public ExhibitItemData ItemData => _itemData;

        // ── Unity Lifecycle ─────────────────────────────────────────────────────

        protected virtual void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            _rootCanvas = GetComponentInParent<Canvas>();

            // Find the root canvas (not nested canvases)
            while (_rootCanvas != null && !_rootCanvas.isRootCanvas)
            {
                var parent = _rootCanvas.transform.parent;
                _rootCanvas = parent != null ? parent.GetComponentInParent<Canvas>() : null;
            }
        }

        // ── Public Methods ──────────────────────────────────────────────────────

        /// <summary>
        /// Sets the item data and updates the icon.
        /// </summary>
        public virtual void SetData(ExhibitItemData item)
        {
            _itemData = item;

            if (_icon != null)
            {
                _icon.sprite = item?.icon;
                _icon.enabled = item != null && item.icon != null;
            }
        }

        /// <summary>
        /// Gets the current item data.
        /// </summary>
        public ExhibitItemData GetData() => _itemData;

        /// <summary>
        /// Returns true if this item can be dragged.
        /// Override to add conditions.
        /// </summary>
        public virtual bool CanDrag() => _itemData != null;

        // ── Drag Handlers ───────────────────────────────────────────────────────

        public virtual void OnBeginDrag(PointerEventData eventData)
        {
            if (!CanDrag())
            {
                eventData.pointerDrag = null;
                return;
            }

            _isDragging = true;
            CurrentlyDragging = _itemData;
            CurrentDragSource = this;

            // Fade source and disable raycasts
            _canvasGroup.alpha = 0.4f;
            _canvasGroup.blocksRaycasts = false;

            // Create ghost
            CreateDragGhost(eventData);

            // Hide tooltip during drag
            ItemTooltip.Instance?.Hide();
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_isDragging) return;
            MoveGhostToPointer(eventData);
        }

        public virtual void OnEndDrag(PointerEventData eventData)
        {
            if (!_isDragging) return;

            _isDragging = false;
            CurrentlyDragging = null;
            CurrentDragSource = null;

            // Restore source
            _canvasGroup.alpha = 1f;
            _canvasGroup.blocksRaycasts = true;

            // Destroy ghost
            DestroyDragGhost();
        }

        // ── Pointer Handlers (Tooltip) ──────────────────────────────────────────

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_isDragging || CurrentlyDragging != null) return;
            if (_itemData == null) return;

            ItemTooltip.Instance?.Show(_itemData, transform.position);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            ItemTooltip.Instance?.Hide();
        }

        // ── Ghost Management ────────────────────────────────────────────────────

        private void CreateDragGhost(PointerEventData eventData)
        {
            if (_rootCanvas == null || _itemData?.icon == null) return;

            _dragGhost = new GameObject("DragGhost");
            _dragGhost.transform.SetParent(_rootCanvas.transform, false);

            // Configure RectTransform
            var rt = _dragGhost.AddComponent<RectTransform>();
            rt.sizeDelta = _ghostSize;

            // Configure Image
            var img = _dragGhost.AddComponent<Image>();
            img.sprite = _itemData.icon;
            img.raycastTarget = false; // Important: don't block raycasts

            // Configure CanvasGroup for transparency
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
    }
}
