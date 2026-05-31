using System.Collections.Generic;
using InventorySystem.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace InventorySystem.UI
{
    /// <summary>
    /// Manages the inventory panel UI.
    ///
    /// ARCHITECTURE
    ///   • The legacy panel is kept hidden while the Journal replaces its
    ///     player-facing entry point.
    ///   • Calls TabGroup.RebuildTabs once on enable to populate the tab bar.
    ///   • Spawns InventorySlotUI cells (which derive from TabItem) into a slot
    ///     grid TabGroup. Hover/focus on a cell populates the detail panel via
    ///     onTabHighlighted / onTabUnhighlighted; click/submit locks in selection.
    ///   • Listens to InventoryManager.InventoryChanged and redraws while open.
    ///
    /// TWO TABGROUPS
    ///   _categoryTabGroup — the top tab bar filtering items by category.
    ///   _slotTabGroup     — the grid of InventorySlotUI cells. This TabGroup's
    ///                       buttonPrefab should be the InventorySlotUI prefab.
    ///
    /// SETUP (Inspector)
    ///   _inventoryManager      — scene InventoryManager.
    ///   _inventoryRoot         — CanvasGroup on the inventory panel root.
    ///   _categoryTabGroup      — TabGroup on the TabBar GameObject.
    ///   _slotTabGroup          — TabGroup on the SlotGrid GameObject.
    ///   _slotPrefab            — InventorySlotUI prefab (also used as buttonPrefab
    ///                            on _slotTabGroup if not already set).
    ///   _tabDefinitions        — ordered list of category tab descriptors.
    ///   _detailIcon/Name/Desc  — children of the detail panel.
    /// </summary>
    public class InventoryUIManager : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Dependencies")]
        [SerializeField] private InventoryManager _inventoryManager;

        [Header("Panel")]
        [SerializeField] private CanvasGroup _inventoryRoot;

        [Header("Category Tab Bar")]
        [SerializeField] private TabGroup                   _categoryTabGroup;
        [SerializeField] private List<InventoryTabDefinition> _tabDefinitions = new();

        [Header("Slot Grid")]
        [SerializeField] private TabGroup        _slotTabGroup;
        [SerializeField] private InventorySlotUI _slotPrefab;

        [Header("Detail Panel")]
        [SerializeField] private Image           _detailIcon;
        [SerializeField] private TextMeshProUGUI _detailName;
        [SerializeField] private TextMeshProUGUI _detailDescription;

        // ── Private state ─────────────────────────────────────────────────────

        private bool                  _isOpen;
        private string                _activeCategory;

        // Parallel list to _slotTabGroup.tabButtons so we can map tab id → slot.
        private readonly List<InventorySlot> _displayedSlots = new();

        // ── Unity lifecycle ───────────────────────────────────────────────────

        private void OnEnable()
        {
            if (_inventoryManager != null)
                _inventoryManager.InventoryChanged += OnInventoryChanged;

            BuildCategoryTabs();
            SetPanelVisible(false);
        }

        private void OnDisable()
        {
            if (_inventoryManager != null)
                _inventoryManager.InventoryChanged -= OnInventoryChanged;
        }

        // ── Category tab setup ────────────────────────────────────────────────

        private void BuildCategoryTabs()
        {
            if (_categoryTabGroup == null || _tabDefinitions.Count == 0) return;

            var defs = new List<TabGroup.TabDefinition>(_tabDefinitions.Count);
            foreach (var t in _tabDefinitions)
                defs.Add(new TabGroup.TabDefinition { id = t.id, label = t.label });

            _categoryTabGroup.onTabChange = OnCategoryTabChanged;
            _categoryTabGroup.RebuildTabs(defs);
        }

        private void OnCategoryTabChanged(string tabId)
        {
            var match = _tabDefinitions.Find(t => t.id == tabId);
            _activeCategory = match?.categoryFilter;
            RedrawSlots();
        }

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

        // ── Inventory event ───────────────────────────────────────────────────

        private void OnInventoryChanged(IReadOnlyList<InventorySlot> slots)
        {
            if (_isOpen) RedrawSlots();
        }

        // ── Slot rendering ────────────────────────────────────────────────────

        /// <summary>
        /// Rebuilds the slot grid TabGroup from the current inventory state.
        /// Each InventorySlotUI cell has its onTabHighlighted / onTabUnhighlighted
        /// wired to populate / clear the detail panel, replacing the old Button
        /// onClick approach.
        /// </summary>
        private void RedrawSlots()
        {
            if (_inventoryManager == null || _slotTabGroup == null || _slotPrefab == null) return;

            // Build a filtered snapshot so we can map tab index → slot.
            _displayedSlots.Clear();
            foreach (var slot in _inventoryManager.Slots)
            {
                if (MatchesCategory(slot.item))
                    _displayedSlots.Add(slot);
            }

            // Build TabDefinitions — id is the slot index as a string so we can
            // look up the slot in OnSlotHighlighted.
            var defs = new List<TabGroup.TabDefinition>(_displayedSlots.Count);
            for (int i = 0; i < _displayedSlots.Count; i++)
                defs.Add(new TabGroup.TabDefinition { id = i.ToString(), label = string.Empty });

            _slotTabGroup.RebuildTabs(defs, _slotPrefab.gameObject);

            // Populate each spawned InventorySlotUI cell and wire its callbacks.
            for (int i = 0; i < _displayedSlots.Count; i++)
            {
                if (_slotTabGroup.tabButtons[i] is not InventorySlotUI cell) continue;

                var slot = _displayedSlots[i];

                string displayName = LocalizationManager.Instance != null
                    ? LocalizationManager.Instance.GetItemName(slot.item.nameKey)
                    : slot.item.nameKey;

                cell.SetData(slot.item.icon, displayName, slot.quantity);

                // Highlight → show detail. Unhighlight → clear detail.
                // Reassign each redraw so stale closures don't reference old slots.
                var captured = slot;
                cell.onTabHighlighted   = _ => ShowDetail(captured);
                cell.onTabUnhighlighted = _ => ClearDetail();

                // Selection (click / submit) can do the same as highlight for now,
                // or be extended (e.g. equip, use, drop) without touching TabItem.
                cell.onTabSelected = _ => ShowDetail(captured);
            }

            if (_displayedSlots.Count == 0) ClearDetail();
        }

        private bool MatchesCategory(ItemData item)
        {
            if (string.IsNullOrEmpty(_activeCategory)) return true;
            // Extend once ItemData gains a category field.
            return false;
        }

        // ── Detail panel ──────────────────────────────────────────────────────

        private void ShowDetail(InventorySlot slot)
        {
            if (slot?.item == null) { ClearDetail(); return; }

            if (_detailIcon != null)
            {
                _detailIcon.sprite  = slot.item.icon;
                _detailIcon.enabled = slot.item.icon != null;
            }

            if (_detailName != null)
                _detailName.text = LocalizationManager.Instance != null
                    ? LocalizationManager.Instance.GetItemName(slot.item.nameKey)
                    : slot.item.nameKey;

            if (_detailDescription != null)
                _detailDescription.text = LocalizationManager.Instance != null
                    ? LocalizationManager.Instance.GetItemDescription(slot.item.descriptionKey)
                    : slot.item.descriptionKey;
        }

        private void ClearDetail()
        {
            if (_detailIcon        != null) { _detailIcon.sprite = null; _detailIcon.enabled = false; }
            if (_detailName        != null) _detailName.text        = string.Empty;
            if (_detailDescription != null) _detailDescription.text = string.Empty;
        }

        // ── Panel visibility ──────────────────────────────────────────────────

        private void SetPanelVisible(bool visible)
        {
            if (_inventoryRoot == null) return;
            _inventoryRoot.alpha          = visible ? 1f : 0f;
            _inventoryRoot.interactable   = visible;
            _inventoryRoot.blocksRaycasts = visible;
        }
    }

    // ── Tab data ──────────────────────────────────────────────────────────────

    [System.Serializable]
    public class InventoryTabDefinition
    {
        [Tooltip("Unique identifier matched against TabGroup's onTabChange callback.")]
        public string id;

        [Tooltip("Text shown on the tab button.")]
        public string label;

        [Tooltip("Only items whose category matches this string are shown. " +
                 "Leave empty to show all items.")]
        public string categoryFilter;
    }
}
