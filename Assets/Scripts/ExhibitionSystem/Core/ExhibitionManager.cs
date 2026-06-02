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

    public readonly struct ExhibitionSlotValidation
    {
        public ExhibitionSlotValidation(bool itemCorrect, bool? inspirationCorrect)
        {
            ItemCorrect = itemCorrect;
            InspirationCorrect = inspirationCorrect;
        }

        public bool ItemCorrect { get; }
        public bool? InspirationCorrect { get; }
        public bool IsCorrect => ItemCorrect && InspirationCorrect == true;
    }

    /// <summary>
    /// Manages the museum curation loop. Each display slot owns an exhibit and a label.
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
        private readonly List<InspirationData> _slotInspirations = new();
        private readonly List<ExhibitItemData> _displaySlots = new();
        private readonly List<ExhibitionSlotValidation> _validationResults = new();
        private int _satisfaction;
        private int _visitorIndex;
        private bool _isRunning;
        private ExhibitionState _state = ExhibitionState.ThemeSelection;

        public ExhibitionTheme CurrentTheme => _currentTheme;
        public IReadOnlyList<InspirationData> SelectedInspirations => _slotInspirations;
        public IReadOnlyList<InspirationData> SlotInspirations => _slotInspirations;
        public IReadOnlyList<ExhibitItemData> DisplaySlots => _displaySlots;
        public IReadOnlyList<ExhibitionTheme> AllThemes => _allThemes;
        public IReadOnlyList<InspirationData> AllInspirations => _allInspirations;
        public IReadOnlyList<ExhibitItemData> AllItems => _allItems;
        public int Satisfaction => _satisfaction;
        public bool IsRunning => _isRunning;
        public int SlotCount => _displaySlots.Count;
        public ExhibitionState State => _state;
        public bool HasConfirmedInspirations => HasAllLabelsFilled;
        public bool HasAllLabelsFilled => _slotInspirations.Count > 0 &&
                                         _slotInspirations.All(inspiration => inspiration != null);
        public bool HasCurationProgress => _displaySlots.Any(item => item != null) ||
                                           _slotInspirations.Any(inspiration => inspiration != null);

        public static event Action<ExhibitionState> OnStateChanged;
        public static event Action<ExhibitionTheme> OnThemeSelected;
        public static event Action<int> OnDisplaySlotsInitialized;
        public static event Action<int, InspirationData> OnSlotInspirationChanged;
        public static event Action<int, ExhibitItemData> OnItemPlaced;
        public static event Action<int> OnItemRemoved;
        public static event Action<int, int> OnItemsSwapped;
        public static event Action OnExhibitionStarted;
        public static event Action<int, InspirationData, ExhibitItemData, ExhibitionSlotValidation, int> OnVisitorReacted;
        public static event Action<bool, int, int> OnExhibitionEnded;
        public static event Action OnCurationCleared;
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
            InitializeDisplaySlots(theme.RequiredSlots);
            _satisfaction = 0;
            _visitorIndex = 0;

            SetState(ExhibitionState.DisplayArrangement);
            OnThemeSelected?.Invoke(theme);
            OnDisplaySlotsInitialized?.Invoke(theme.RequiredSlots);
        }

        public void AssignInspiration(int slotIndex, InspirationData inspiration)
        {
            if (!ValidateSlotIndex(slotIndex) || inspiration == null || _isRunning)
                return;

            int existingIndex = _slotInspirations.IndexOf(inspiration);
            if (existingIndex >= 0 && existingIndex != slotIndex)
            {
                _slotInspirations[existingIndex] = null;
                OnSlotInspirationChanged?.Invoke(existingIndex, null);
            }

            _slotInspirations[slotIndex] = inspiration;
            OnSlotInspirationChanged?.Invoke(slotIndex, inspiration);
        }

        public ExhibitItemData PlaceItem(int slotIndex, ExhibitItemData item)
        {
            if (!ValidateSlotIndex(slotIndex)) return null;
            if (_isRunning || item == null) return null;

            var previousItem = _displaySlots[slotIndex];
            if (previousItem == item)
                return previousItem;

            int existingIndex = _displaySlots.IndexOf(item);
            if (existingIndex >= 0 && existingIndex != slotIndex)
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
            AutoAssignKnownInspiration(slotIndex, item);
            return previousItem;
        }

        public ExhibitItemData RemoveItem(int slotIndex)
        {
            if (!ValidateSlotIndex(slotIndex) || _isRunning) return null;

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

            if (!AreAllSlotsReady())
            {
                Debug.LogWarning("[ExhibitionManager] Every display slot needs an exhibit and an inspiration label.");
                return;
            }

            if (_isRunning)
            {
                Debug.LogWarning("[ExhibitionManager] Exhibition already running.");
                return;
            }

            _satisfaction = 0;
            _visitorIndex = 0;
            _validationResults.Clear();
            _isRunning = true;
            SetState(ExhibitionState.ExhibitionRunning);
            OnExhibitionStarted?.Invoke();

            StartCoroutine(ProcessVisitorsCoroutine());
        }

        public void RetryExhibition()
        {
            if (_currentTheme == null || _isRunning) return;
            StartExhibition();
        }

        public void ClearCompletedCurationForThemeSelection()
        {
            if (_isRunning || _currentTheme == null || !_currentTheme.isCompleted)
                return;

            _currentTheme = null;
            _slotInspirations.Clear();
            _displaySlots.Clear();
            _validationResults.Clear();
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
            return false;
        }

        public bool IsInspirationMatchKnown(InspirationData inspiration)
        {
            return inspiration != null && KnownInspirationMatchIds.Contains(inspiration.id);
        }

        public static void ResetKnownInspirationMatches()
        {
            KnownInspirationMatchIds.Clear();
        }

        public ExhibitItemData GetHintItemForInspiration(InspirationData inspiration)
        {
            if (inspiration == null)
                return null;

            int slotIndex = _slotInspirations.IndexOf(inspiration);
            if (slotIndex >= 0 && slotIndex < _displaySlots.Count && _displaySlots[slotIndex] != null)
                return _displaySlots[slotIndex];

            return IsInspirationMatchKnown(inspiration) ? inspiration.mappedItem : null;
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

        public bool AreAllSlotsReady()
        {
            return AreAllSlotsFilled() && HasAllLabelsFilled;
        }

        private void InitializeDisplaySlots(int slotCount)
        {
            _displaySlots.Clear();
            _slotInspirations.Clear();
            _validationResults.Clear();

            for (int i = 0; i < slotCount; i++)
            {
                _displaySlots.Add(null);
                _slotInspirations.Add(null);
            }
        }

        private void AutoAssignKnownInspiration(int slotIndex, ExhibitItemData item)
        {
            if (item == null || _currentTheme == null || _slotInspirations[slotIndex] != null)
                return;

            var knownInspiration = _allInspirations.FirstOrDefault(inspiration =>
                inspiration != null &&
                inspiration.mappedItem == item &&
                _currentTheme.IsInspirationValid(inspiration.id) &&
                IsInspirationMatchKnown(inspiration));

            if (knownInspiration != null)
                AssignInspiration(slotIndex, knownInspiration);
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
            var inspiration = _slotInspirations[_visitorIndex];
            var item = _displaySlots[_visitorIndex];
            bool itemCorrect = IsItemValidForTheme(item);
            bool? inspirationCorrect = itemCorrect
                ? inspiration != null &&
                  _currentTheme.IsInspirationValid(inspiration.id) &&
                  inspiration.mappedItem == item
                : null;
            var validation = new ExhibitionSlotValidation(itemCorrect, inspirationCorrect);

            _validationResults.Add(validation);
            if (validation.IsCorrect)
            {
                _satisfaction++;
                KnownInspirationMatchIds.Add(inspiration.id);
                item.RecordUsage(_currentTheme.title);
            }

            OnVisitorReacted?.Invoke(_visitorIndex, inspiration, item, validation, _satisfaction);
        }

        private bool IsItemValidForTheme(ExhibitItemData item)
        {
            if (_currentTheme == null || item == null)
                return false;

            return _allInspirations.Any(inspiration =>
                inspiration != null &&
                _currentTheme.IsInspirationValid(inspiration.id) &&
                inspiration.mappedItem == item);
        }

        private void EndExhibition()
        {
            _isRunning = false;

            int threshold = _displaySlots.Count;
            bool success = _satisfaction >= threshold;

            if (success)
                _currentTheme.MarkCompleted();

            SetState(ExhibitionState.Result);
            OnExhibitionEnded?.Invoke(success, _satisfaction, threshold);

            if (!success)
                OnPlayerHint?.Invoke(PickExhibitionErrorHint());
        }

        private string PickExhibitionErrorHint()
        {
            var correctlyMatchedIds = new HashSet<int>();
            for (int i = 0; i < _displaySlots.Count; i++)
            {
                var item = _displaySlots[i];
                var inspiration = _slotInspirations[i];
                if (item != null &&
                    inspiration != null &&
                    IsItemValidForTheme(item) &&
                    _currentTheme.IsInspirationValid(inspiration.id) &&
                    inspiration.mappedItem == item)
                {
                    correctlyMatchedIds.Add(inspiration.id);
                }
            }

            var missingIds = _currentTheme.validInspirationIds
                .Where(id => !correctlyMatchedIds.Contains(id))
                .ToList();

            if (missingIds.Count > 0)
                return _currentTheme.GetHintForMissingId(missingIds[UnityEngine.Random.Range(0, missingIds.Count)]);

            if (_validationResults.Any(result => !result.ItemCorrect))
                return "One of the exhibits does not fit. I should replace the item marked in red.";

            return "One of the exhibit labels does not fit. I should replace the label marked in red.";
        }

        private void SetState(ExhibitionState state)
        {
            if (_state == state) return;
            _state = state;
            OnStateChanged?.Invoke(_state);
        }
    }
}
