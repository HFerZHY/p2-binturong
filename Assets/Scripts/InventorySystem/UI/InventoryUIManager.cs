using System.Collections.Generic;
using InventorySystem.Data;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace InventorySystem.UI
{
    /// <summary>
    /// Manages the inventory panel UI.
    ///
    /// ARCHITECTURE
    ///   • Opens/closes the panel via an InputAction looked up by name from
    ///     InputSystem.actions (the project-wide InputActionAsset).
    ///   • Uses TabGroup / TabItem to categorise items (e.g. All / Weapons / Consumables).
    ///     Add tab names and categories by populating <see cref="_tabs"/> in the Inspector.
    ///   • Listens to InventoryManager.InventoryChanged and redraws the active tab's
    ///     slot grid on every change.
    ///   • The detail panel on the right shows the localized name, description, and icon
    ///     of whichever slot the player selects.
    ///
    /// SETUP (Inspector)
    ///   InventoryManager.Instance      — drag the scene InventoryManager here.
    ///   _inventoryRoot         — the root CanvasGroup of the whole inventory panel.
    ///   _slotGrid              — a LayoutGroup transform where slot cells are spawned.
    ///   _slotPrefab            — prefab with an InventorySlotUI component.
    ///   _tabGroup              — the TabGroup component driving the tab bar.
    ///   _tabs                  — list pairing tab names with optional item categories.
    ///   _detailIcon            — Image for the selected item's icon.
    ///   _detailName            — TMP text for the localized name.
    ///   _detailDescription     — TMP text for the localized description.
    ///   _toggleInventoryAction — name of the InputAction (e.g. "ToggleInventory").
    /// </summary>
    public class InventoryUIManager : MonoBehaviour
    {
        // ── Inspector fields ──────────────────────────────────────────────────────

        [Header("Panel")]
        [SerializeField] private CanvasGroup _inventoryRoot;

        [Header("Slot Grid")]
        [SerializeField] private Transform          _slotGrid;
        [SerializeField] private InventorySlotUI    _slotPrefab;

        [Header("Tab Bar")]
        [SerializeField] private TabGroup           _tabGroup;
        [SerializeField] private List<InventoryTab> _tabs = new();

        [Header("Detail Panel")]
        [SerializeField] private Image           _detailIcon;
        [SerializeField] private TextMeshProUGUI _detailName;
        [SerializeField] private TextMeshProUGUI _detailDescription;

        [Header("Input")]
        [Tooltip("Name of the InputAction in the project InputActionAsset that toggles the inventory.")]
        [SerializeField] private string _toggleInventoryAction = "ToggleInventory";

        // ── Private state ─────────────────────────────────────────────────────────

        private InputAction            _toggleAction;
        private bool                   _isOpen;
        private string                 _activeCategory; // null/empty = show all
        private List<InventorySlotUI>  _spawnedSlots = new();

        // ── Unity lifecycle ────────────────────────────────────────────────────────

        private void Awake()
        {
            // Find the InputAction by name from the project-wide InputActionAsset.
            _toggleAction = InputSystem.actions?.FindAction(_toggleInventoryAction);
            if (_toggleAction == null)
                Debug.LogWarning(
                    $"[InventoryUIManager] InputAction '{_toggleInventoryAction}' not found. " +
                    "Check the action name in the Inspector and verify it exists in your InputActionAsset.");
        }

        private void OnEnable()
        {
            if (_toggleAction != null)
                _toggleAction.performed += OnTogglePerformed;

            if (InventoryManager.Instance != null)
                InventoryManager.Instance.InventoryChanged += OnInventoryChanged;

            // Wire up tabs
            if (_tabGroup != null)
            {
                foreach (var tab in _tabs)
                {
                    if (tab.tabItem == null) continue;

                    // Subscribe to the TabGroup so it tracks this button.
                    tab.tabItem.tabGroup = _tabGroup;
                    _tabGroup.Subscribe(tab.tabItem);

                    // Capture for lambda.
                    var capturedCategory = tab.categoryFilter;
                    tab.tabItem.onTabSelected   += _ => OnTabSelected(capturedCategory);
                }

                // Select the first tab by default.
                if (_tabs.Count > 0 && _tabs[0].tabItem != null)
                    _tabGroup.OnTabSelected(_tabs[0].tabItem);
            }

            SetPanelVisible(false);
        }

        private void OnDisable()
        {
            if (_toggleAction != null)
                _toggleAction.performed -= OnTogglePerformed;

            if (InventoryManager.Instance != null)
                InventoryManager.Instance.InventoryChanged -= OnInventoryChanged;

            foreach (var tab in _tabs)
            {
                if (tab.tabItem == null) continue;
                tab.tabItem.onTabSelected   = null;
                tab.tabItem.onTabDeselected = null;
            }
        }

        // ── Input ──────────────────────────────────────────────────────────────────

        private void OnTogglePerformed(InputAction.CallbackContext ctx) => Toggle();

        public void Toggle()
        {
            _isOpen = !_isOpen;
            SetPanelVisible(_isOpen);
            if (_isOpen) RedrawSlots();
        }

        public void Open()
        {
            if (_isOpen) return;
            _isOpen = true;
            SetPanelVisible(true);
            RedrawSlots();
        }

        public void Close()
        {
            if (!_isOpen) return;
            _isOpen = false;
            SetPanelVisible(false);
        }

        // ── Tab handling ───────────────────────────────────────────────────────────

        private void OnTabSelected(string category)
        {
            _activeCategory = category;
            RedrawSlots();
        }

        // ── Inventory event ────────────────────────────────────────────────────────

        private void OnInventoryChanged(IReadOnlyList<InventorySlot> slots)
        {
            if (_isOpen) RedrawSlots();
        }

        // ── Slot rendering ─────────────────────────────────────────────────────────

        private void RedrawSlots()
        {
            if (InventoryManager.Instance == null || _slotGrid == null || _slotPrefab == null) return;

            // Filter slots by active category (empty = show all).
            var source = InventoryManager.Instance.Slots;

            // Pool: reuse existing cells, spawn new ones, hide excess.
            int index = 0;
            foreach (var slot in source)
            {
                if (!MatchesCategory(slot.item)) continue;

                InventorySlotUI cell = GetOrSpawnCell(index);

                string displayName = LocalizationManager.Instance != null
                    ? LocalizationManager.Instance.GetItemName(slot.item.nameKey)
                    : slot.item.nameKey;

                cell.SetData(slot.item.icon, displayName, slot.quantity);

                // Wire up selection — capture index for closure.
                var capturedSlot = slot;
                var btn = cell.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => ShowDetail(capturedSlot));
                }

                cell.gameObject.SetActive(true);
                index++;
            }

            // Hide unused cells beyond the active count.
            for (int i = index; i < _spawnedSlots.Count; i++)
                _spawnedSlots[i].gameObject.SetActive(false);

            // Clear detail panel if nothing is showing.
            if (index == 0) ClearDetail();
        }

        private InventorySlotUI GetOrSpawnCell(int index)
        {
            if (index < _spawnedSlots.Count)
                return _spawnedSlots[index];

            var cell = Instantiate(_slotPrefab, _slotGrid);
            _spawnedSlots.Add(cell);
            return cell;
        }

        private bool MatchesCategory(ItemData item)
        {
            if (string.IsNullOrEmpty(_activeCategory)) return true;
            // Extend this if ItemData gains a category field.
            return false;
        }

        // ── Detail panel ───────────────────────────────────────────────────────────

        private void ShowDetail(InventorySlot slot)
        {
            if (slot?.item == null) { ClearDetail(); return; }

            if (_detailIcon != null)
            {
                _detailIcon.sprite  = slot.item.icon;
                _detailIcon.enabled = slot.item.icon != null;
            }

            if (_detailName != null)
            {
                _detailName.text = LocalizationManager.Instance != null
                    ? LocalizationManager.Instance.GetItemName(slot.item.nameKey)
                    : slot.item.nameKey;
            }

            if (_detailDescription != null)
            {
                _detailDescription.text = LocalizationManager.Instance != null
                    ? LocalizationManager.Instance.GetItemDescription(slot.item.descriptionKey)
                    : slot.item.descriptionKey;
            }
        }

        private void ClearDetail()
        {
            if (_detailIcon        != null) { _detailIcon.sprite = null; _detailIcon.enabled = false; }
            if (_detailName        != null) _detailName.text        = string.Empty;
            if (_detailDescription != null) _detailDescription.text = string.Empty;
        }

        // ── Panel visibility ───────────────────────────────────────────────────────

        private void SetPanelVisible(bool visible)
        {
            if (_inventoryRoot == null) return;
            _inventoryRoot.alpha          = visible ? 1f : 0f;
            _inventoryRoot.interactable   = visible;
            _inventoryRoot.blocksRaycasts = visible;
        }
    }

    // ── Tab descriptor ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Pairs a <see cref="TabItem"/> with an optional category filter string.
    /// Leave <see cref="categoryFilter"/> empty to show all items.
    /// </summary>
    [System.Serializable]
    public class InventoryTab
    {
        [Tooltip("The TabItem button representing this tab.")]
        public TabItem tabItem;

        [Tooltip("Filter items whose category matches this string. " +
                 "Leave empty to show all items regardless of category.")]
        public string categoryFilter;
    }
}
