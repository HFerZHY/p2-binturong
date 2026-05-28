using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Base;
using ExhibitionSystem.Data;
using UnityEngine;

namespace ExhibitionSystem.Core
{
    public enum ExhibitionState
    {
        ThemeSelection,
        InspirationSelection,
        DisplayArrangement,
        ExhibitionRunning,
        Result
    }

    /// <summary>
    /// Manages the museum inspiration curation loop.
    /// </summary>
    public class ExhibitionManager : MonoSingleton<ExhibitionManager>
    {
        [Header("Available Content")]
        [SerializeField] private List<ExhibitionTheme> _allThemes = new();
        [SerializeField] private List<InspirationData> _allInspirations = new();
        [SerializeField] private List<ExhibitItemData> _allItems = new();

        [Header("Timing")]
        [SerializeField] private float _visitorDelay = 1.5f;

        private static readonly HashSet<int> KnownInspirationMatchIds = new();

        private ExhibitionTheme _currentTheme;
        private readonly List<InspirationData> _selectedInspirations = new();
        private readonly List<ExhibitItemData> _displaySlots = new();
        private readonly List<bool> _lockedSlots = new();
        private int _satisfaction;
        private int _visitorIndex;
        private bool _isRunning;
        private ExhibitionState _state = ExhibitionState.ThemeSelection;

        public ExhibitionTheme CurrentTheme => _currentTheme;
        public IReadOnlyList<InspirationData> SelectedInspirations => _selectedInspirations;
        public IReadOnlyList<ExhibitItemData> DisplaySlots => _displaySlots;
        public IReadOnlyList<ExhibitionTheme> AllThemes => _allThemes;
        public IReadOnlyList<InspirationData> AllInspirations => _allInspirations;
        public IReadOnlyList<ExhibitItemData> AllItems => _allItems;
        public int Satisfaction => _satisfaction;
        public bool IsRunning => _isRunning;
        public int SlotCount => _displaySlots.Count;
        public ExhibitionState State => _state;
        public bool HasConfirmedInspirations => _selectedInspirations.Count > 0;

        public static event Action<ExhibitionState> OnStateChanged;
        public static event Action<ExhibitionTheme> OnThemeSelected;
        public static event Action<IReadOnlyList<InspirationData>> OnInspirationsConfirmed;
        public static event Action<int, ExhibitItemData> OnItemPlaced;
        public static event Action<int> OnItemRemoved;
        public static event Action<int, int> OnItemsSwapped;
        public static event Action OnExhibitionStarted;
        public static event Action<int, InspirationData, ExhibitItemData, bool, int> OnVisitorReacted;
        public static event Action<bool, int, int> OnExhibitionEnded;
        public static event Action OnCurationCleared;
        public static event Action OnInspirationValidationAttempted;
        public static event Action<string> OnPlayerHint;

        protected override void Awake()
        {
            base.Awake();
            LoadResourcesIfEmpty();
            SetState(ExhibitionState.ThemeSelection);
        }

        private void LoadResourcesIfEmpty()
        {
            if (HasMissingEntries(_allThemes))
                _allThemes = Resources.LoadAll<ExhibitionTheme>("Exhibitions/Themes")
                    .OrderBy(theme => theme.day)
                    .ThenBy(theme => theme.title)
                    .ToList();

            if (HasMissingEntries(_allInspirations))
                _allInspirations = Resources.LoadAll<InspirationData>("Exhibitions/Inspirations")
                    .OrderBy(inspiration => inspiration.id)
                    .ToList();

            if (HasMissingEntries(_allItems))
                _allItems = Resources.LoadAll<ExhibitItemData>("Exhibitions/Items")
                    .OrderBy(item => item.sortOrder)
                    .ToList();
        }

        private static bool HasMissingEntries<T>(IReadOnlyCollection<T> entries) where T : UnityEngine.Object
        {
            return entries == null || entries.Count == 0 || entries.Any(entry => entry == null);
        }

        public void SelectTheme(ExhibitionTheme theme)
        {
            if (theme == null)
            {
                Debug.LogWarning("[ExhibitionManager] Cannot select a null theme.");
                return;
            }

            if (_isRunning)
            {
                Debug.LogWarning("[ExhibitionManager] Cannot change theme while exhibition is running.");
                return;
            }

            _currentTheme = theme;
            _selectedInspirations.Clear();
            _displaySlots.Clear();
            _lockedSlots.Clear();
            _satisfaction = 0;
            _visitorIndex = 0;

            SetState(ExhibitionState.InspirationSelection);
            OnThemeSelected?.Invoke(theme);
        }

        public bool TryConfirmInspirations(IReadOnlyList<InspirationData> selectedInspirations, out string hint)
        {
            hint = string.Empty;
            OnInspirationValidationAttempted?.Invoke();

            if (_currentTheme == null)
            {
                hint = "Rin: (I should choose an exhibition theme first.)";
                OnPlayerHint?.Invoke(hint);
                return false;
            }

            if (selectedInspirations == null || selectedInspirations.Count != _currentTheme.requiredInspirations)
            {
                hint = $"Rin: (I need exactly {_currentTheme.requiredInspirations} ideas for this exhibition.)";
                OnPlayerHint?.Invoke(hint);
                return false;
            }

            if (!_currentTheme.IsSelectionValid(selectedInspirations))
            {
                hint = PickSelectionErrorHint(selectedInspirations);
                OnPlayerHint?.Invoke(hint);
                return false;
            }

            _selectedInspirations.Clear();
            _selectedInspirations.AddRange(selectedInspirations.OrderBy(inspiration => inspiration.id));
            BuildDisplaySlotsFromInspirations();

            SetState(ExhibitionState.DisplayArrangement);
            OnInspirationsConfirmed?.Invoke(_selectedInspirations);
            return true;
        }

        public ExhibitItemData PlaceItem(int slotIndex, ExhibitItemData item)
        {
            if (!ValidateSlotIndex(slotIndex)) return null;
            if (_isRunning || IsSlotLocked(slotIndex) || item == null) return null;

            var previousItem = _displaySlots[slotIndex];
            if (previousItem == item)
                return previousItem;

            int existingIndex = _displaySlots.IndexOf(item);
            if (existingIndex >= 0 && existingIndex != slotIndex && !IsSlotLocked(existingIndex))
            {
                _displaySlots[existingIndex] = null;
                OnItemRemoved?.Invoke(existingIndex);
            }

            if (previousItem != null)
            {
                _displaySlots[slotIndex] = null;
                OnItemRemoved?.Invoke(slotIndex);
            }

            _displaySlots[slotIndex] = item;
            OnItemPlaced?.Invoke(slotIndex, item);
            return previousItem;
        }

        public ExhibitItemData RemoveItem(int slotIndex)
        {
            if (!ValidateSlotIndex(slotIndex)) return null;
            if (_isRunning || IsSlotLocked(slotIndex)) return null;

            var removed = _displaySlots[slotIndex];
            _displaySlots[slotIndex] = null;
            if (removed != null)
                OnItemRemoved?.Invoke(slotIndex);

            return removed;
        }

        public void SwapItems(int slotA, int slotB)
        {
            if (!ValidateSlotIndex(slotA) || !ValidateSlotIndex(slotB)) return;
            if (_isRunning || slotA == slotB) return;
            if (IsSlotLocked(slotA) || IsSlotLocked(slotB)) return;

            (_displaySlots[slotA], _displaySlots[slotB]) = (_displaySlots[slotB], _displaySlots[slotA]);
            OnItemsSwapped?.Invoke(slotA, slotB);
        }

        public void StartExhibition()
        {
            if (_currentTheme == null)
            {
                Debug.LogWarning("[ExhibitionManager] No theme selected.");
                return;
            }

            if (_selectedInspirations.Count == 0)
            {
                Debug.LogWarning("[ExhibitionManager] Inspirations have not been confirmed.");
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
            SetState(ExhibitionState.ExhibitionRunning);
            OnExhibitionStarted?.Invoke();

            StartCoroutine(ProcessVisitorsCoroutine());
        }

        public void RetryExhibition()
        {
            if (_currentTheme == null || _selectedInspirations.Count == 0 || _isRunning) return;
            StartExhibition();
        }

        public void ClearCompletedCurationForThemeSelection()
        {
            if (_isRunning || _currentTheme == null || !_currentTheme.isCompleted)
                return;

            _currentTheme = null;
            _selectedInspirations.Clear();
            _displaySlots.Clear();
            _lockedSlots.Clear();
            _satisfaction = 0;
            _visitorIndex = 0;

            SetState(ExhibitionState.ThemeSelection);
            OnCurationCleared?.Invoke();
            OnThemeSelected?.Invoke(null);
        }

        public bool IsItemPlaced(ExhibitItemData item)
        {
            return item != null && _displaySlots.Contains(item);
        }

        public bool IsSlotLocked(int index)
        {
            return index >= 0 && index < _lockedSlots.Count && _lockedSlots[index];
        }

        public bool IsInspirationMatchKnown(InspirationData inspiration)
        {
            return inspiration != null && KnownInspirationMatchIds.Contains(inspiration.id);
        }

        public static void ResetKnownInspirationMatches()
        {
            KnownInspirationMatchIds.Clear();
        }

        public IEnumerable<InspirationData> GetKnownInspirationsForItem(ExhibitItemData item)
        {
            if (item == null) yield break;

            foreach (var inspiration in _allInspirations)
            {
                if (inspiration != null &&
                    inspiration.mappedItem == item &&
                    KnownInspirationMatchIds.Contains(inspiration.id))
                {
                    yield return inspiration;
                }
            }
        }

        public IEnumerable<ExhibitItemData> GetAvailableItems()
        {
            return _allItems.Where(item => item != null && item.isUnlocked && !IsItemPlaced(item));
        }

        public int CountEmptySlots()
        {
            return _displaySlots.Count(item => item == null);
        }

        public bool AreAllSlotsFilled()
        {
            return _displaySlots.Count > 0 && CountEmptySlots() == 0;
        }

        private string PickSelectionErrorHint(IReadOnlyList<InspirationData> selectedInspirations)
        {
            var selectedIds = selectedInspirations
                .Where(inspiration => inspiration != null)
                .Select(inspiration => inspiration.id)
                .ToHashSet();

            var missingIds = _currentTheme.validInspirationIds
                .Where(id => !selectedIds.Contains(id))
                .ToList();

            if (missingIds.Count > 0)
            {
                int id = missingIds[UnityEngine.Random.Range(0, missingIds.Count)];
                return _currentTheme.GetHintForMissingId(id);
            }

            return _currentTheme.GetInvalidHint();
        }

        private void BuildDisplaySlotsFromInspirations()
        {
            _displaySlots.Clear();
            _lockedSlots.Clear();

            foreach (var inspiration in _selectedInspirations)
            {
                bool known = IsInspirationMatchKnown(inspiration);
                _displaySlots.Add(known ? inspiration.mappedItem : null);
                _lockedSlots.Add(known);
            }
        }

        private bool ValidateSlotIndex(int index)
        {
            if (index < 0 || index >= _displaySlots.Count)
            {
                Debug.LogWarning($"[ExhibitionManager] Invalid slot index: {index}");
                return false;
            }
            return true;
        }

        private IEnumerator ProcessVisitorsCoroutine()
        {
            while (_visitorIndex < _selectedInspirations.Count)
            {
                ProcessCurrentVisitor();
                _visitorIndex++;
                yield return new WaitForSeconds(_visitorDelay);
            }

            EndExhibition();
        }

        private void ProcessCurrentVisitor()
        {
            var inspiration = _selectedInspirations[_visitorIndex];
            var item = _displaySlots[_visitorIndex];
            bool isCorrect = inspiration != null && item != null && inspiration.mappedItem == item;

            if (isCorrect)
            {
                _satisfaction++;
                KnownInspirationMatchIds.Add(inspiration.id);
            }

            OnVisitorReacted?.Invoke(_visitorIndex, inspiration, item, isCorrect, _satisfaction);
        }

        private void EndExhibition()
        {
            _isRunning = false;

            int threshold = _selectedInspirations.Count;
            bool success = _satisfaction >= threshold;

            if (success)
                _currentTheme.MarkCompleted();

            SetState(ExhibitionState.Result);
            OnExhibitionEnded?.Invoke(success, _satisfaction, threshold);
        }

        private void SetState(ExhibitionState state)
        {
            if (_state == state) return;
            _state = state;
            OnStateChanged?.Invoke(_state);
        }
    }
}
