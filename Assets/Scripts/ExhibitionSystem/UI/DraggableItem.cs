using ExhibitionSystem.Data;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

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

        [Header("Drag Highlight")]
        [SerializeField] private Color _highlightColor = new(1f, 0.9f, 0.5f, 1f);
        [SerializeField] private float _highlightWidth = 4f;

        // ── Runtime State ───────────────────────────────────────────────────────

        protected ExhibitItemData _itemData;
        protected CanvasGroup _canvasGroup;
        protected Canvas _rootCanvas;
        private bool _isDragging;
        private Outline _highlightOutline;

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
                _icon.preserveAspect = true;
                _icon.rectTransform.localScale = Vector3.one * GetIconScale(item);
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

            // Hide icon at source, keep slot visible for highlight
            if (_icon != null)
                _icon.enabled = false;

            _canvasGroup.blocksRaycasts = false;

            // Add highlight outline to source
            ShowHighlight();

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

            // Restore source icon
            if (_icon != null && _itemData != null && _itemData.icon != null)
                _icon.enabled = true;

            _canvasGroup.blocksRaycasts = true;

            // Remove highlight
            HideHighlight();

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
            rt.sizeDelta = _ghostSize * GetIconScale(_itemData);

            // Configure Image
            var img = _dragGhost.AddComponent<Image>();
            img.sprite = _itemData.icon;
            img.preserveAspect = true;
            img.raycastTarget = false; // Important: don't block raycasts

            // Configure CanvasGroup for transparency
            var cg = _dragGhost.AddComponent<CanvasGroup>();
            cg.alpha = 0.8f;
            cg.blocksRaycasts = false;
            cg.interactable = false;

            MoveGhostToPointer(eventData);
        }

        public static float GetIconScale(ExhibitItemData item)
        {
            return item != null ? Mathf.Clamp(item.iconScale, 0.5f, 1.5f) : 1f;
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

        // ── Highlight Management ───────────────────────────────────────────────

        private void ShowHighlight()
        {
            if (_highlightOutline == null)
            {
                _highlightOutline = gameObject.AddComponent<Outline>();
            }

            _highlightOutline.effectColor = _highlightColor;
            _highlightOutline.effectDistance = new Vector2(_highlightWidth, _highlightWidth);
            _highlightOutline.enabled = true;
        }

        private void HideHighlight()
        {
            if (_highlightOutline != null)
            {
                _highlightOutline.enabled = false;
            }
        }
    }
}
