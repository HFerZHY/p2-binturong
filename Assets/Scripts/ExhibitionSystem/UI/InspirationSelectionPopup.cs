using System.Collections.Generic;
using System.Linq;
using ExhibitionSystem.Core;
using ExhibitionSystem.Data;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ExhibitionSystem.UI
{
    /// <summary>
    /// Slot-level inspiration picker opened by clicking a display label.
    /// </summary>
    public class InspirationSelectionPopup : MonoBehaviour, IPointerClickHandler
    {
        public static event System.Action OnInspirationClicked;
        public static event System.Action OnPopupOpened;

        [Header("UI References")]
        [SerializeField] private GameObject _panel;
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _themeText;
        [SerializeField] private Image _targetItemIcon;
        [SerializeField] private TMP_Text _progressText;
        [SerializeField] private TMP_Text _hintText;
        [SerializeField] private Image _hintBackground;
        [SerializeField] private Transform _selectedContainer;
        [SerializeField] private Transform _libraryContainer;
        [SerializeField] private Transform _progressContainer;
        [SerializeField] private Transform _listContainer;
        [SerializeField] private InspirationListItem _itemPrefab;
        [SerializeField] private Button _confirmButton;
        [SerializeField] private Button _closeButton;
        [SerializeField] private CanvasGroup _canvasGroup;

        private readonly List<InspirationListItem> _libraryItems = new();
        private int _targetSlotIndex = -1;
        private InspirationData _pendingInspiration;

        private void Start()
        {
            if (_confirmButton != null)
                _confirmButton.onClick.AddListener(ConfirmSelection);

            if (_closeButton != null)
                _closeButton.onClick.AddListener(Hide);
        }

        public void ShowForSlot(int slotIndex)
        {
            var manager = ExhibitionManager.Instance;
            if (manager == null ||
                manager.CurrentTheme == null ||
                 slotIndex < 0 ||
                 slotIndex >= manager.SlotCount ||
                 TutorialPopup.IsInspirationEditingBlocked ||
                 manager.IsSlotInspirationFixed(slotIndex))
            {
                return;
            }

            _targetSlotIndex = slotIndex;
            _pendingInspiration = manager.SlotInspirations[slotIndex];
            OnPopupOpened?.Invoke();
            ConfigureAsSelectionBar();

            if (_panel != null)
                _panel.SetActive(true);

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 1f;
                _canvasGroup.blocksRaycasts = true;
                _canvasGroup.interactable = true;
            }

            if (_titleText != null)
                _titleText.text = "Choose an Inspiration Label";

            if (_themeText != null)
            {
                _themeText.fontSize = 32f;
                _themeText.text = $"Theme: <b><color=#FFD96A>{manager.CurrentTheme.title}</color></b>";
            }

            UpdateTargetItemIcon(manager.DisplaySlots[slotIndex]);
            PopulateList();
            UpdateConfirmButton();
        }

        public void Show()
        {
            if (_targetSlotIndex >= 0)
                ShowForSlot(_targetSlotIndex);
        }

        public void Hide()
        {
            HideImmediate();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData != null && eventData.pointerPressRaycast.gameObject == gameObject)
                Hide();
        }

        private void HideImmediate()
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
                _canvasGroup.blocksRaycasts = false;
                _canvasGroup.interactable = false;
            }

            if (_panel != null)
                _panel.SetActive(false);
        }

        private void PopulateList()
        {
            foreach (var item in _libraryItems)
            {
                if (item != null)
                    Destroy(item.gameObject);
            }
            _libraryItems.Clear();

            var manager = ExhibitionManager.Instance;
            var container = _libraryContainer != null ? _libraryContainer : _listContainer;
            if (manager == null || _itemPrefab == null || container == null)
                return;

            foreach (var inspiration in manager.AllInspirations
                         .Where(inspiration => inspiration.isUnlocked)
                         .OrderBy(inspiration => manager.IsInspirationMatchKnown(inspiration) ? 1 : 0)
                         .ThenBy(inspiration => inspiration.id))
            {
                var item = Instantiate(_itemPrefab, container);
                item.SetData(inspiration, false, HandleInspirationSelected);
                _libraryItems.Add(item);
            }

            RefreshListItems();
        }

        private void HandleInspirationSelected(InspirationData inspiration)
        {
            var manager = ExhibitionManager.Instance;
            if (manager == null ||
                inspiration == null ||
                _targetSlotIndex < 0)
            {
                return;
            }

            OnInspirationClicked?.Invoke();

            if (manager.IsInspirationMatchKnown(inspiration))
            {
                ApplyKnownMatchToTargetSlot(manager, inspiration);
                return;
            }

            _pendingInspiration = inspiration;
            RefreshListItems();
            UpdateConfirmButton();
        }

        private void ConfirmSelection()
        {
            var manager = ExhibitionManager.Instance;
            if (manager == null ||
                 _pendingInspiration == null ||
                 _targetSlotIndex < 0 ||
                 manager.IsSlotInspirationFixed(_targetSlotIndex))
                return;

            manager.AssignInspiration(_targetSlotIndex, _pendingInspiration);
            Hide();
        }

        private void ConfigureAsSelectionBar()
        {
            var selectedPanel = GetColumnPanel(_selectedContainer);
            if (selectedPanel != null)
                selectedPanel.SetActive(false);

            if (_confirmButton != null)
            {
                _confirmButton.gameObject.SetActive(true);
                if (_confirmButton.GetComponentInChildren<TMP_Text>() is TMP_Text confirmText)
                    confirmText.text = "Confirm";
            }

            if (_closeButton != null && _closeButton.transform is RectTransform closeButtonRect)
                closeButtonRect.anchoredPosition = new Vector2(-150f, closeButtonRect.anchoredPosition.y);

            if (_hintBackground != null)
                _hintBackground.gameObject.SetActive(false);
            else if (_hintText != null && _hintText.transform.parent != null)
                _hintText.transform.parent.gameObject.SetActive(false);

            if (_progressText != null)
                _progressText.gameObject.SetActive(false);

            if (_progressContainer != null)
                _progressContainer.gameObject.SetActive(false);

            var container = _libraryContainer != null ? _libraryContainer : _listContainer;
            var libraryPanel = container?.parent?.parent?.parent as RectTransform;
            if (libraryPanel == null)
                return;

            libraryPanel.anchorMin = Vector2.zero;
            libraryPanel.anchorMax = Vector2.one;
            libraryPanel.offsetMin = new Vector2(40f, 104f);
            libraryPanel.offsetMax = new Vector2(-40f, -176f);
        }

        private void UpdateConfirmButton()
        {
            if (_confirmButton != null)
                _confirmButton.interactable = _pendingInspiration != null;
        }

        private void RefreshListItems()
        {
            var manager = ExhibitionManager.Instance;
            if (manager == null)
                return;

            var targetItem = _targetSlotIndex >= 0 && _targetSlotIndex < manager.DisplaySlots.Count
                ? manager.DisplaySlots[_targetSlotIndex]
                : null;
            var assignedToTarget = _targetSlotIndex >= 0 && _targetSlotIndex < manager.SlotInspirations.Count
                ? manager.SlotInspirations[_targetSlotIndex]
                : null;

            foreach (var item in _libraryItems)
            {
                var inspiration = item != null ? item.Inspiration : null;
                if (item == null || inspiration == null)
                    continue;

                UpdateListItem(item, inspiration, manager, targetItem, assignedToTarget);
            }
        }

        private void UpdateListItem(
            InspirationListItem item,
            InspirationData inspiration,
            ExhibitionManager manager,
            ExhibitItemData targetItem,
            InspirationData assignedToTarget)
        {
            bool knownMatch = manager.IsInspirationMatchKnown(inspiration);
            var hintItem = inspiration == _pendingInspiration
                ? targetItem
                : inspiration == assignedToTarget && assignedToTarget != _pendingInspiration
                    ? null
                    : manager.GetHintItemForInspiration(inspiration);
            item.SetData(
                inspiration,
                inspiration == _pendingInspiration,
                HandleInspirationSelected,
                false,
                false,
                hintItem,
                true,
                knownMatch);
        }

        private void ApplyKnownMatchToTargetSlot(ExhibitionManager manager, InspirationData inspiration)
        {
            if (manager == null ||
                inspiration == null ||
                inspiration.mappedItem == null ||
                _targetSlotIndex < 0 ||
                _targetSlotIndex >= manager.SlotCount ||
                manager.IsRunning ||
                manager.IsSlotLocked(_targetSlotIndex) ||
                manager.IsSlotInspirationFixed(_targetSlotIndex))
            {
                return;
            }

            manager.PlaceItem(_targetSlotIndex, inspiration.mappedItem);
            if (_targetSlotIndex < manager.DisplaySlots.Count &&
                manager.DisplaySlots[_targetSlotIndex] == inspiration.mappedItem)
            {
                Hide();
            }
        }

        private void UpdateTargetItemIcon(ExhibitItemData item)
        {
            EnsureTargetItemIcon();
            if (_targetItemIcon == null)
                return;

            _targetItemIcon.sprite = item != null ? item.icon : null;
            _targetItemIcon.enabled = _targetItemIcon.sprite != null;
            _targetItemIcon.gameObject.SetActive(_targetItemIcon.enabled);
        }

        private void EnsureTargetItemIcon()
        {
            if (_targetItemIcon != null || _themeText == null || _themeText.transform.parent == null)
                return;

            var parent = _themeText.transform.parent;
            var existing = parent.Find("TargetItemIcon");
            if (existing != null)
            {
                _targetItemIcon = existing.GetComponent<Image>();
                if (_targetItemIcon != null)
                    return;
            }

            var iconObject = new GameObject("TargetItemIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            iconObject.transform.SetParent(parent, false);
            iconObject.transform.SetSiblingIndex(_themeText.transform.GetSiblingIndex() + 1);

            var iconRect = iconObject.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.5f, 1f);
            iconRect.anchorMax = new Vector2(0.5f, 1f);
            iconRect.pivot = new Vector2(0.5f, 1f);
            iconRect.anchoredPosition = new Vector2(0f, -112f);
            iconRect.sizeDelta = new Vector2(56f, 56f);

            _targetItemIcon = iconObject.GetComponent<Image>();
            _targetItemIcon.preserveAspect = true;
            _targetItemIcon.raycastTarget = false;
        }

        private static GameObject GetColumnPanel(Transform listContainer)
        {
            return listContainer?.parent?.parent?.parent?.gameObject;
        }
    }
}
