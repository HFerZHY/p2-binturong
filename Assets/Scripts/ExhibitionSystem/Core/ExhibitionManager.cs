using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Base;
using ExhibitionSystem.Data;
using UnityEngine;

namespace ExhibitionSystem.Core
{
    /// <summary>
    /// Manages the museum exhibition gameplay loop.
    /// Handles theme selection, item placement, and visitor evaluation.
    ///
    /// USAGE:
    ///   1. Call SelectTheme() to choose an exhibition
    ///   2. Call PlaceItem()/RemoveItem()/SwapItems() to arrange the display
    ///   3. Call StartExhibition() to begin the evaluation phase
    ///   4. Subscribe to events for UI updates
    /// </summary>
    public class ExhibitionManager : MonoSingleton<ExhibitionManager>
    {
        // ── Configuration ────────────────────────────────────────────────────────

        [Header("Available Content")]
        [SerializeField] private List<ExhibitionTheme> _allThemes = new();
        [SerializeField] private List<ExhibitItemData> _allItems = new();

        [Header("Timing")]
        [SerializeField] private float _visitorDelay = 1.5f;

        [Header("Auto")]
        [Tooltip("Automatically select the first theme on Start")]
        [SerializeField] private bool _autoSelectFirstTheme = true;

        // ── Runtime State ────────────────────────────────────────────────────────

        private ExhibitionTheme _currentTheme;
        private List<ExhibitItemData> _displaySlots;
        private int _satisfaction;
        private int _visitorIndex;
        private bool _isRunning;

        // ── Public Properties ────────────────────────────────────────────────────

        public ExhibitionTheme CurrentTheme => _currentTheme;
        public IReadOnlyList<ExhibitItemData> DisplaySlots => _displaySlots;
        public IReadOnlyList<ExhibitionTheme> AllThemes => _allThemes;
        public IReadOnlyList<ExhibitItemData> AllItems => _allItems;
        public int Satisfaction => _satisfaction;
        public bool IsRunning => _isRunning;
        public int SlotCount => _displaySlots?.Count ?? 0;

        // ── Events (static for easy subscription) ────────────────────────────────

        /// <summary>Fired when a new theme is selected.</summary>
        public static event Action<ExhibitionTheme> OnThemeSelected;

        /// <summary>Fired when an item is placed in a slot.</summary>
        public static event Action<int, ExhibitItemData> OnItemPlaced;

        /// <summary>Fired when an item is removed from a slot.</summary>
        public static event Action<int> OnItemRemoved;

        /// <summary>Fired when two items are swapped.</summary>
        public static event Action<int, int> OnItemsSwapped;

        /// <summary>Fired when the exhibition evaluation starts.</summary>
        public static event Action OnExhibitionStarted;

        /// <summary>Fired for each visitor reaction. (slotIndex, isCorrect, currentSatisfaction)</summary>
        public static event Action<int, bool, int> OnVisitorReacted;

        /// <summary>Fired when the exhibition ends. (success, finalSatisfaction, threshold)</summary>
        public static event Action<bool, int, int> OnExhibitionEnded;

        // ── Unity Lifecycle ──────────────────────────────────────────────────────

        protected override void Awake()
        {
            base.Awake();
            LoadResourcesIfEmpty();
        }

        private void Start()
        {
            if (_autoSelectFirstTheme && _allThemes.Count > 0)
                SelectTheme(_allThemes[0]);
        }

        private void LoadResourcesIfEmpty()
        {
            // If not configured in Inspector, load from Resources folder
            if (_allThemes == null || _allThemes.Count == 0)
            {
                _allThemes = new List<ExhibitionTheme>(
                    Resources.LoadAll<ExhibitionTheme>("Exhibitions/Themes")
                );
                Debug.Log($"[ExhibitionManager] Loaded {_allThemes.Count} themes from Resources.");
            }

            if (_allItems == null || _allItems.Count == 0)
            {
                _allItems = new List<ExhibitItemData>(
                    Resources.LoadAll<ExhibitItemData>("Exhibitions/Items")
                );
                Debug.Log($"[ExhibitionManager] Loaded {_allItems.Count} items from Resources.");
            }
        }

        // ── Theme Selection ──────────────────────────────────────────────────────

        /// <summary>
        /// Selects a new exhibition theme, clearing current display slots.
        /// </summary>
        public void SelectTheme(ExhibitionTheme theme)
        {
            if (theme == null)
            {
                Debug.LogWarning("[ExhibitionManager] Cannot select null theme.");
                return;
            }

            if (_isRunning)
            {
                Debug.LogWarning("[ExhibitionManager] Cannot change theme while exhibition is running.");
                return;
            }

            // Clear current slots and initialize new ones
            _displaySlots = new List<ExhibitItemData>(new ExhibitItemData[theme.requiredSlots]);
            _currentTheme = theme;
            _satisfaction = 0;
            _visitorIndex = 0;

            OnThemeSelected?.Invoke(theme);
        }

        // ── Item Management ──────────────────────────────────────────────────────

        /// <summary>
        /// Places an item in the specified display slot.
        /// Returns the item that was previously in the slot (if any).
        /// </summary>
        public ExhibitItemData PlaceItem(int slotIndex, ExhibitItemData item)
        {
            if (!ValidateSlotIndex(slotIndex)) return null;
            if (_isRunning) return null;
            if (item == null) return null;

            // Check if item is already placed elsewhere
            int existingIndex = _displaySlots.IndexOf(item);
            if (existingIndex >= 0 && existingIndex != slotIndex)
            {
                // Remove from old position first
                _displaySlots[existingIndex] = null;
                OnItemRemoved?.Invoke(existingIndex);
            }

            ExhibitItemData previousItem = _displaySlots[slotIndex];
            _displaySlots[slotIndex] = item;

            OnItemPlaced?.Invoke(slotIndex, item);
            return previousItem;
        }

        /// <summary>
        /// Removes the item from the specified slot.
        /// Returns the removed item.
        /// </summary>
        public ExhibitItemData RemoveItem(int slotIndex)
        {
            if (!ValidateSlotIndex(slotIndex)) return null;
            if (_isRunning) return null;

            ExhibitItemData removed = _displaySlots[slotIndex];
            _displaySlots[slotIndex] = null;

            if (removed != null)
                OnItemRemoved?.Invoke(slotIndex);

            return removed;
        }

        /// <summary>
        /// Swaps items between two slots.
        /// </summary>
        public void SwapItems(int slotA, int slotB)
        {
            if (!ValidateSlotIndex(slotA) || !ValidateSlotIndex(slotB)) return;
            if (_isRunning) return;
            if (slotA == slotB) return;

            (_displaySlots[slotA], _displaySlots[slotB]) =
                (_displaySlots[slotB], _displaySlots[slotA]);

            OnItemsSwapped?.Invoke(slotA, slotB);
        }

        /// <summary>
        /// Checks if an item is currently placed in any display slot.
        /// </summary>
        public bool IsItemPlaced(ExhibitItemData item)
        {
            return _displaySlots != null && _displaySlots.Contains(item);
        }

        /// <summary>
        /// Gets the slot index where an item is placed, or -1 if not placed.
        /// </summary>
        public int GetItemSlotIndex(ExhibitItemData item)
        {
            return _displaySlots?.IndexOf(item) ?? -1;
        }

        // ── Exhibition Execution ─────────────────────────────────────────────────

        /// <summary>
        /// Starts the exhibition evaluation phase.
        /// Visitors will evaluate each slot in sequence.
        /// </summary>
        public void StartExhibition()
        {
            if (_currentTheme == null)
            {
                Debug.LogWarning("[ExhibitionManager] No theme selected.");
                return;
            }

            if (_isRunning)
            {
                Debug.LogWarning("[ExhibitionManager] Exhibition already running.");
                return;
            }

            _satisfaction = 0;
            _visitorIndex = 0;
            _isRunning = true;

            OnExhibitionStarted?.Invoke();

            StartCoroutine(ProcessVisitorsCoroutine());
        }

        /// <summary>
        /// Retries the current exhibition with the same slot configuration.
        /// </summary>
        public void RetryExhibition()
        {
            if (_currentTheme == null) return;
            if (_isRunning) return;

            StartExhibition();
        }

        // ── Private Methods ──────────────────────────────────────────────────────

        private bool ValidateSlotIndex(int index)
        {
            if (_displaySlots == null || index < 0 || index >= _displaySlots.Count)
            {
                Debug.LogWarning($"[ExhibitionManager] Invalid slot index: {index}");
                return false;
            }
            return true;
        }

        private IEnumerator ProcessVisitorsCoroutine()
        {
            while (_visitorIndex < _displaySlots.Count)
            {
                ProcessCurrentVisitor();
                _visitorIndex++;

                yield return new WaitForSeconds(_visitorDelay);
            }

            EndExhibition();
        }

        private void ProcessCurrentVisitor()
        {
            ExhibitItemData item = _displaySlots[_visitorIndex];
            bool isCorrect = _currentTheme.IsItemCorrect(item);

            if (isCorrect)
            {
                _satisfaction++;

                // Record usage history
                if (item != null)
                    item.RecordUsage(_currentTheme.title);
            }

            OnVisitorReacted?.Invoke(_visitorIndex, isCorrect, _satisfaction);
        }

        private void EndExhibition()
        {
            _isRunning = false;

            int threshold = _currentTheme.SuccessThreshold;
            bool success = _satisfaction >= threshold;

            if (success)
                _currentTheme.MarkCompleted();

            OnExhibitionEnded?.Invoke(success, _satisfaction, threshold);
        }

        // ── Helper Methods for UI ────────────────────────────────────────────────

        /// <summary>
        /// Gets all unlocked items that are not currently placed.
        /// </summary>
        public IEnumerable<ExhibitItemData> GetAvailableItems()
        {
            return _allItems.Where(item => item.isUnlocked && !IsItemPlaced(item));
        }

        /// <summary>
        /// Counts empty slots in the current display.
        /// </summary>
        public int CountEmptySlots()
        {
            return _displaySlots?.Count(item => item == null) ?? 0;
        }

        /// <summary>
        /// Checks if all slots are filled.
        /// </summary>
        public bool AreAllSlotsFilled()
        {
            return CountEmptySlots() == 0;
        }
    }
}
