using InventorySystem.Data;
using StationWork.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace StationWork.UI
{
    /// <summary>
    /// Draggable item card shown in the station work scene.
    /// Drag it onto the PassengerPanel to submit the item.
    ///
    /// PREFAB STRUCTURE
    ///   ItemCardUI  ← this component + Image (background) + CanvasGroup
    ///   ├── Icon    ← Image  →  _icon
    ///   ├── Name    ← TextMeshProUGUI  →  _nameText
    ///   └── Labels  ← TextMeshProUGUI  →  _labelsText  (shows known labels)
    ///
    /// SETUP
    ///   The prefab must be on a Canvas that has a GraphicRaycaster.
    ///   The Canvas must be assigned to _rootCanvas, or found automatically.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class ItemCardUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Header("Visuals")]
        [SerializeField] private Image            _icon;
        [SerializeField] private TextMeshProUGUI  _nameText;
        [SerializeField] private TextMeshProUGUI  _labelsText;

        // ── Runtime ────────────────────────────────────────────────────────────

        private ItemData     _itemData;
        private InventorySlot _slot;
        private CanvasGroup  _canvasGroup;
        private Canvas       _rootCanvas;
        private RectTransform _rectTransform;

        // Shared drag ghost — one per drag, parented to root canvas
        private static GameObject _dragGhost;

        // ── Static accessor used by PassengerPanel ─────────────────────────────
        public static ItemData CurrentlyDragging { get; private set; }

        // ── Unity lifecycle ────────────────────────────────────────────────────

        private void Awake()
        {
            _canvasGroup   = GetComponent<CanvasGroup>();
            _rectTransform = GetComponent<RectTransform>();
        }

        // ── Public API ─────────────────────────────────────────────────────────

        public void SetData(InventorySlot slot)
        {
            _slot     = slot;
            _itemData = slot.item;

            if (_icon != null)
            {
                _icon.sprite  = _itemData.icon;
                _icon.enabled = _itemData.icon != null;
            }

            if (_nameText != null)
                _nameText.text = _itemData.nameKey;

            RefreshLabels();
        }

        public void RefreshLabels()
        {
            if (_labelsText == null || _slot == null) return;

            var lines = new System.Text.StringBuilder();
            foreach (string label in _itemData.labels)
            {
                if (string.IsNullOrEmpty(label)) continue;

                if (_slot.IsLabelFixed(label))
                    lines.AppendLine($"[{label}]");  // fixed = shown in brackets
                else if (_slot.IsLabelAssigned(label))
                    lines.AppendLine(label);
                else
                    lines.AppendLine("???");
            }
            _labelsText.text = lines.ToString().TrimEnd();
        }

        // ── Drag handlers ──────────────────────────────────────────────────────

        public void OnBeginDrag(PointerEventData eventData)
        {
            CurrentlyDragging = _itemData;

            // Make this card semi-transparent while dragging
            _canvasGroup.alpha          = 0.4f;
            _canvasGroup.blocksRaycasts = false;

            // Create ghost
            _rootCanvas = GetComponentInParent<Canvas>()?.rootCanvas;
            if (_rootCanvas == null) return;

            _dragGhost = new GameObject("DragGhost", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            _dragGhost.transform.SetParent(_rootCanvas.transform, false);
            _dragGhost.transform.SetAsLastSibling();

            var ghostImg = _dragGhost.GetComponent<Image>();
            ghostImg.sprite       = _itemData.icon;
            ghostImg.raycastTarget = false;

            var ghostGroup = _dragGhost.GetComponent<CanvasGroup>();
            ghostGroup.blocksRaycasts = false;
            ghostGroup.alpha          = 0.85f;

            var rt = _dragGhost.GetComponent<RectTransform>();
            rt.sizeDelta = _rectTransform.sizeDelta;

            MoveGhostToPointer(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            MoveGhostToPointer(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            CurrentlyDragging           = null;
            _canvasGroup.alpha          = 1f;
            _canvasGroup.blocksRaycasts = true;

            if (_dragGhost != null)
            {
                Destroy(_dragGhost);
                _dragGhost = null;
            }
        }

        // ── Helpers ────────────────────────────────────────────────────────────

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
    }
}
