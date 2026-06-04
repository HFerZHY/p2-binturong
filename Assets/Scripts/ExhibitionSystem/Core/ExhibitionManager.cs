using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Base;
using ExhibitionSystem.Data;
using Otowa.SaveSystem;
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
        private const string BIRDWATCHING_THEME_ASSET_NAME = "Birdwatching";
        private const int BIRD_DEITY_FAN_INSPIRATION_ID = 7;

        [Header("Available Content")]
        [SerializeField] private List<ExhibitionTheme> _allThemes = new();
        [SerializeField] private List<InspirationData> _allInspirations = new();
        [SerializeField] private List<ExhibitItemData> _allItems = new();

        [Header("Timing")]
        [SerializeField] private float _visitorDelay = 1.5f;

        private static readonly HashSet<int> KnownInspirationMatchIds = new();

        private sealed class ThemeCurationState
        {
            public readonly List<InspirationData> SlotInspirations = new();
            public readonly List<ExhibitItemData> DisplaySlots = new();
            public readonly List<ExhibitionSlotValidation?> SlotValidationResults = new();
        }

        private sealed class InspirationItemBinding
        {
            public ExhibitionTheme Theme;
            public int SlotIndex;
            public ExhibitItemData Item;
        }

        private ExhibitionTheme _currentTheme;
        private readonly List<InspirationData> _slotInspirations = new();
        private readonly List<ExhibitItemData> _displaySlots = new();
        private readonly List<ExhibitionSlotValidation> _validationResults = new();
        private readonly List<ExhibitionSlotValidation?> _slotValidationResults = new();
        private readonly Dictionary<ExhibitionTheme, ThemeCurationState> _themeCurationStates = new();
        private readonly Dictionary<InspirationData, InspirationItemBinding> _inspirationItemBindings = new();
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
        public bool HasValidationFeedback => _slotValidationResults.Any(result => result.HasValue);
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

        public ExhibitionSaveData CaptureSaveData(string sceneName)
        {
            SaveCurrentThemeState();

            var data = new ExhibitionSaveData
            {
                sceneName = sceneName,
                currentThemeTitle = _currentTheme != null ? _currentTheme.title : string.Empty,
                activeExhibitionTitle = _currentTheme != null ? _currentTheme.title : string.Empty,
                state = _state.ToString(),
                satisfaction = _satisfaction,
                visitorIndex = _visitorIndex,
                isRunning = _isRunning,
            };

            data.currentSlots.AddRange(CaptureSlots(_displaySlots, _slotInspirations, _slotValidationResults));

            foreach (int id in KnownInspirationMatchIds)
                data.knownInspirationIds.Add(id);

            foreach (var theme in _allThemes)
            {
                if (theme != null && theme.isCompleted)
                    data.completedThemeTitles.Add(theme.title);
            }

            foreach (var item in _allItems)
            {
                if (item == null)
                    continue;

                data.itemStates.Add(new ExhibitionItemStateSaveData
                {
                    sortOrder = item.sortOrder,
                    isUnlocked = item.isUnlocked,
                    usedInExhibitions = new List<string>(item.usedInExhibitions),
                });
            }

            foreach (var inspiration in _allInspirations)
            {
                if (inspiration == null)
                    continue;

                data.inspirationStates.Add(new ExhibitionInspirationStateSaveData
                {
                    id = inspiration.id,
                    isUnlocked = inspiration.isUnlocked,
                });
            }

            foreach (var pair in _themeCurationStates)
            {
                if (pair.Key == null || pair.Value == null)
                    continue;

                data.themeStates.Add(new ExhibitionThemeCurationSaveData
                {
                    themeTitle = pair.Key.title,
                    slots = CaptureSlots(
                        pair.Value.DisplaySlots,
                        pair.Value.SlotInspirations,
                        pair.Value.SlotValidationResults),
                });
            }

            return data;
        }

        public void ApplySaveData(ExhibitionSaveData data)
        {
            if (data == null || string.IsNullOrWhiteSpace(data.sceneName))
                return;

            StopAllCoroutines();
            LoadResourcesIfEmpty();
            ApplySavedResourceState(data);

            _themeCurationStates.Clear();
            _inspirationItemBindings.Clear();
            if (data.themeStates != null)
            {
                foreach (var savedTheme in data.themeStates)
                    RestoreSavedThemeState(savedTheme);
            }

            string activeExhibitionTitle = !string.IsNullOrWhiteSpace(data.activeExhibitionTitle)
                ? data.activeExhibitionTitle
                : data.currentThemeTitle;
            _currentTheme = FindThemeByTitle(activeExhibitionTitle);
            RestoreSlots(data.currentSlots, _currentTheme != null ? _currentTheme.RequiredSlots : 0);
            RebuildInspirationBindings();

            _satisfaction = Mathf.Max(0, data.satisfaction);
            _visitorIndex = Mathf.Max(0, data.visitorIndex);
            _validationResults.Clear();
            foreach (var result in _slotValidationResults)
            {
                if (result.HasValue)
                    _validationResults.Add(result.Value);
            }

            bool resumeRunning = data.isRunning || data.state == ExhibitionState.ExhibitionRunning.ToString();
            _isRunning = resumeRunning;
            if (!Enum.TryParse(data.state, out ExhibitionState restoredState))
                restoredState = _currentTheme == null
                    ? ExhibitionState.ThemeSelection
                    : ExhibitionState.DisplayArrangement;

            SetState(restoredState);
            OnThemeSelected?.Invoke(_currentTheme);
            OnDisplaySlotsInitialized?.Invoke(_displaySlots.Count);

            for (int i = 0; i < _displaySlots.Count; i++)
            {
                OnSlotInspirationChanged?.Invoke(i, _slotInspirations[i]);
                if (_displaySlots[i] != null)
                    OnItemPlaced?.Invoke(i, _displaySlots[i]);
            }

            if (resumeRunning && _currentTheme != null)
            {
                OnExhibitionStarted?.Invoke();
                StartCoroutine(ProcessVisitorsCoroutine());
            }
            else if (restoredState == ExhibitionState.Result && _currentTheme != null)
            {
                int threshold = _displaySlots.Count;
                OnExhibitionEnded?.Invoke(_satisfaction >= threshold, _satisfaction, threshold);
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRuntimeMatchKnowledge()
        {
            KnownInspirationMatchIds.Clear();
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

            if (_currentTheme == theme)
                return;

            if (_currentTheme != null && _currentTheme != theme)
                SaveCurrentThemeState();

            _currentTheme = theme;
            if (!TryRestoreThemeState(theme))
                InitializeDisplaySlots(theme.RequiredSlots);

            AutoAssignKnownInspirationsInCurrentTheme();
            RestoreVerifiedSlotProgress();
            _visitorIndex = 0;
            _validationResults.Clear();

            SetState(ExhibitionState.DisplayArrangement);
            OnThemeSelected?.Invoke(theme);
            OnDisplaySlotsInitialized?.Invoke(theme.RequiredSlots);
        }

        public void AssignInspiration(int slotIndex, InspirationData inspiration)
        {
            if (!ValidateSlotIndex(slotIndex) ||
                inspiration == null ||
                _isRunning ||
                IsSlotInspirationFixed(slotIndex) ||
                IsInspirationMatchKnown(inspiration))
            {
                return;
            }

            if (!ClearInspirationFromOtherSlots(inspiration, slotIndex))
                return;

            ClearBindingIfOwned(_slotInspirations[slotIndex], _currentTheme, slotIndex);
            _slotInspirations[slotIndex] = inspiration;
            ClearSlotValidation(slotIndex);
            RefreshBindingForCurrentSlot(slotIndex);
            OnSlotInspirationChanged?.Invoke(slotIndex, inspiration);
        }

        public ExhibitItemData PlaceItem(int slotIndex, ExhibitItemData item)
        {
            if (!ValidateSlotIndex(slotIndex)) return null;
            if (_isRunning || item == null || IsSlotLocked(slotIndex)) return null;

            var previousItem = _displaySlots[slotIndex];
            if (previousItem == item)
            {
                AutoAssignKnownInspiration(slotIndex, item);
                return previousItem;
            }

            int existingIndex = _displaySlots.IndexOf(item);
            if (existingIndex >= 0 && existingIndex != slotIndex)
            {
                if (IsSlotLocked(existingIndex))
                    return null;

                ClearFixedInspirationForItem(existingIndex, item);
                ClearBindingIfOwned(_slotInspirations[existingIndex], _currentTheme, existingIndex);
                _displaySlots[existingIndex] = null;
                ClearSlotValidation(existingIndex);
                OnItemRemoved?.Invoke(existingIndex);
            }

            if (previousItem != null)
            {
                ClearFixedInspirationForItem(slotIndex, previousItem);
                ClearBindingIfOwned(_slotInspirations[slotIndex], _currentTheme, slotIndex);
                _displaySlots[slotIndex] = null;
                ClearSlotValidation(slotIndex);
                OnItemRemoved?.Invoke(slotIndex);
            }

            _displaySlots[slotIndex] = item;
            ClearMismatchedKnownInspiration(slotIndex, item);
            ClearSlotValidation(slotIndex);
            RefreshBindingForCurrentSlot(slotIndex);
            OnItemPlaced?.Invoke(slotIndex, item);
            AutoAssignKnownInspiration(slotIndex, item);
            return previousItem;
        }

        public ExhibitItemData RemoveItem(int slotIndex)
        {
            if (!ValidateSlotIndex(slotIndex) || _isRunning || IsSlotLocked(slotIndex)) return null;

            var removed = _displaySlots[slotIndex];
            ClearFixedInspirationForItem(slotIndex, removed);
            ClearBindingIfOwned(_slotInspirations[slotIndex], _currentTheme, slotIndex);
            _displaySlots[slotIndex] = null;
            ClearSlotValidation(slotIndex);
            if (removed != null)
                OnItemRemoved?.Invoke(slotIndex);

            return removed;
        }

        public void SwapItems(int slotA, int slotB)
        {
            if (!ValidateSlotIndex(slotA) || !ValidateSlotIndex(slotB)) return;
            if (_isRunning || slotA == slotB || IsSlotLocked(slotA) || IsSlotLocked(slotB)) return;

            ClearFixedInspirationForItem(slotA, _displaySlots[slotA]);
            ClearFixedInspirationForItem(slotB, _displaySlots[slotB]);
            (_displaySlots[slotA], _displaySlots[slotB]) = (_displaySlots[slotB], _displaySlots[slotA]);
            ClearSlotValidation(slotA);
            ClearSlotValidation(slotB);
            RefreshBindingForCurrentSlot(slotA);
            RefreshBindingForCurrentSlot(slotB);
            AutoAssignKnownInspiration(slotA, _displaySlots[slotA]);
            AutoAssignKnownInspiration(slotB, _displaySlots[slotB]);
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

            _visitorIndex = 0;
            _validationResults.Clear();
            RestoreVerifiedSlotProgress();
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
            _slotValidationResults.Clear();
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

        public bool IsSlotInspirationFixed(int index)
        {
            return index >= 0 &&
                   index < _displaySlots.Count &&
                   GetKnownInspirationForItem(_displaySlots[index]) != null;
        }

        public bool IsSlotLocked(int index)
        {
            return IsSlotAlreadyVerifiedCorrect(index);
        }

        public bool TryGetSlotValidation(int index, out ExhibitionSlotValidation validation)
        {
            if (index >= 0 && index < _slotValidationResults.Count && _slotValidationResults[index].HasValue)
            {
                validation = _slotValidationResults[index].Value;
                return true;
            }

            validation = default;
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

        public static void SeedKnownInspirationMatches(IEnumerable<int> inspirationIds)
        {
            if (inspirationIds == null)
                return;

            foreach (int id in inspirationIds)
                KnownInspirationMatchIds.Add(id);
        }

        public ExhibitItemData GetHintItemForInspiration(InspirationData inspiration)
        {
            if (inspiration == null)
                return null;

            if (IsInspirationMatchKnown(inspiration))
                return inspiration.mappedItem;

            if (_inspirationItemBindings.TryGetValue(inspiration, out var binding) && binding.Item != null)
                return binding.Item;

            return null;
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

        public InspirationData GetKnownInspirationForItem(ExhibitItemData item)
        {
            if (item == null)
                return null;

            Day3ExhibitionInitializer.EnsureKnownInspirationMatchesIfLoaded();
            return _allInspirations.FirstOrDefault(inspiration =>
                inspiration != null &&
                inspiration.mappedItem == item &&
                IsInspirationMatchKnown(inspiration));
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
            _slotValidationResults.Clear();

            for (int i = 0; i < slotCount; i++)
            {
                _displaySlots.Add(null);
                _slotInspirations.Add(null);
                _slotValidationResults.Add(null);
            }
        }

        private void SaveCurrentThemeState()
        {
            if (_currentTheme == null)
                return;

            var state = new ThemeCurationState();
            state.DisplaySlots.AddRange(_displaySlots);
            state.SlotInspirations.AddRange(_slotInspirations);
            state.SlotValidationResults.AddRange(_slotValidationResults);
            _themeCurationStates[_currentTheme] = state;
        }

        private bool TryRestoreThemeState(ExhibitionTheme theme)
        {
            if (theme == null || !_themeCurationStates.TryGetValue(theme, out var state))
                return false;

            _displaySlots.Clear();
            _displaySlots.AddRange(state.DisplaySlots);
            _slotInspirations.Clear();
            _slotInspirations.AddRange(state.SlotInspirations);
            _slotValidationResults.Clear();
            _slotValidationResults.AddRange(state.SlotValidationResults);
            return true;
        }

        private List<ExhibitionSlotSaveData> CaptureSlots(
            IReadOnlyList<ExhibitItemData> items,
            IReadOnlyList<InspirationData> inspirations,
            IReadOnlyList<ExhibitionSlotValidation?> validations)
        {
            int count = Mathf.Max(items?.Count ?? 0, inspirations?.Count ?? 0);
            var slots = new List<ExhibitionSlotSaveData>(count);
            for (int i = 0; i < count; i++)
            {
                var item = items != null && i < items.Count ? items[i] : null;
                var inspiration = inspirations != null && i < inspirations.Count ? inspirations[i] : null;
                var slot = new ExhibitionSlotSaveData
                {
                    itemSortOrder = item != null ? item.sortOrder : 0,
                    inspirationId = inspiration != null ? inspiration.id : 0,
                };

                if (validations != null && i < validations.Count && validations[i].HasValue)
                {
                    var validation = validations[i].Value;
                    slot.hasValidation = true;
                    slot.itemCorrect = validation.ItemCorrect;
                    slot.hasInspirationCorrect = validation.InspirationCorrect.HasValue;
                    slot.inspirationCorrect = validation.InspirationCorrect.GetValueOrDefault();
                }

                slots.Add(slot);
            }

            return slots;
        }

        private void ApplySavedResourceState(ExhibitionSaveData data)
        {
            KnownInspirationMatchIds.Clear();
            if (data.knownInspirationIds != null)
            {
                foreach (int id in data.knownInspirationIds)
                    KnownInspirationMatchIds.Add(id);
            }

            if (data.inspirationStates != null)
            {
                foreach (var state in data.inspirationStates)
                {
                    var inspiration = FindInspirationById(state.id);
                    if (inspiration != null)
                        inspiration.isUnlocked = state.isUnlocked;
                }
            }

            if (data.itemStates != null)
            {
                foreach (var state in data.itemStates)
                {
                    var item = FindItemBySortOrder(state.sortOrder);
                    if (item == null)
                        continue;

                    item.isUnlocked = state.isUnlocked;
                    item.usedInExhibitions.Clear();
                    if (state.usedInExhibitions == null)
                        continue;

                    foreach (string themeTitle in state.usedInExhibitions)
                    {
                        if (!string.IsNullOrWhiteSpace(themeTitle))
                            item.usedInExhibitions.Add(themeTitle);
                    }
                }
            }

            foreach (var theme in _allThemes)
            {
                if (theme == null)
                    continue;

                bool completed = data.completedThemeTitles != null
                                 && data.completedThemeTitles.Contains(theme.title);
                if (completed)
                    theme.MarkCompleted();
                else
                    theme.ResetCompletion();
            }
        }

        private void RestoreSavedThemeState(ExhibitionThemeCurationSaveData savedTheme)
        {
            if (savedTheme == null)
                return;

            var theme = FindThemeByTitle(savedTheme.themeTitle);
            if (theme == null)
                return;

            var state = new ThemeCurationState();
            FillSlotLists(savedTheme.slots, theme.RequiredSlots,
                state.DisplaySlots,
                state.SlotInspirations,
                state.SlotValidationResults);
            _themeCurationStates[theme] = state;
        }

        private void RestoreSlots(IReadOnlyList<ExhibitionSlotSaveData> slots, int requiredSlots)
        {
            _displaySlots.Clear();
            _slotInspirations.Clear();
            _slotValidationResults.Clear();

            FillSlotLists(slots, requiredSlots, _displaySlots, _slotInspirations, _slotValidationResults);
        }

        private void FillSlotLists(
            IReadOnlyList<ExhibitionSlotSaveData> slots,
            int requiredSlots,
            List<ExhibitItemData> items,
            List<InspirationData> inspirations,
            List<ExhibitionSlotValidation?> validations)
        {
            int count = Mathf.Max(requiredSlots, slots?.Count ?? 0);
            for (int i = 0; i < count; i++)
            {
                var slot = slots != null && i < slots.Count ? slots[i] : null;
                items.Add(slot != null ? FindItemBySortOrder(slot.itemSortOrder) : null);
                inspirations.Add(slot != null ? FindInspirationById(slot.inspirationId) : null);
                validations.Add(slot != null ? RestoreValidation(slot) : null);
            }
        }

        private static ExhibitionSlotValidation? RestoreValidation(ExhibitionSlotSaveData slot)
        {
            if (slot == null || !slot.hasValidation)
                return null;

            bool? inspirationCorrect = slot.hasInspirationCorrect
                ? slot.inspirationCorrect
                : null;
            return new ExhibitionSlotValidation(slot.itemCorrect, inspirationCorrect);
        }

        private void RebuildInspirationBindings()
        {
            for (int i = 0; i < _slotInspirations.Count; i++)
                RefreshBindingForCurrentSlot(i);

            foreach (var pair in _themeCurationStates)
            {
                var theme = pair.Key;
                var state = pair.Value;
                if (theme == null || state == null)
                    continue;

                for (int i = 0; i < state.SlotInspirations.Count && i < state.DisplaySlots.Count; i++)
                {
                    var inspiration = state.SlotInspirations[i];
                    var item = state.DisplaySlots[i];
                    if (inspiration == null || item == null)
                        continue;

                    _inspirationItemBindings[inspiration] = new InspirationItemBinding
                    {
                        Theme = theme,
                        SlotIndex = i,
                        Item = item,
                    };
                }
            }
        }

        private ExhibitionTheme FindThemeByTitle(string title)
        {
            return _allThemes.FirstOrDefault(theme =>
                theme != null && theme.title == title);
        }

        private InspirationData FindInspirationById(int id)
        {
            return _allInspirations.FirstOrDefault(inspiration =>
                inspiration != null && inspiration.id == id);
        }

        private ExhibitItemData FindItemBySortOrder(int sortOrder)
        {
            return _allItems.FirstOrDefault(item =>
                item != null && item.sortOrder == sortOrder);
        }

        private void ClearSlotValidation(int slotIndex)
        {
            if (slotIndex >= 0 && slotIndex < _slotValidationResults.Count)
                _slotValidationResults[slotIndex] = null;
        }

        private bool IsSlotAlreadyVerifiedCorrect(int slotIndex)
        {
            if (slotIndex < 0 ||
                slotIndex >= _displaySlots.Count ||
                slotIndex >= _slotValidationResults.Count ||
                _currentTheme == null ||
                !_slotValidationResults[slotIndex].HasValue ||
                !_slotValidationResults[slotIndex].Value.IsCorrect)
            {
                return false;
            }

            var inspiration = _slotInspirations[slotIndex];
            var item = _displaySlots[slotIndex];
            return item != null &&
                   inspiration != null &&
                   inspiration.mappedItem == item &&
                   _currentTheme.IsInspirationValid(inspiration.id);
        }

        private void RestoreVerifiedSlotProgress()
        {
            _satisfaction = 0;

            for (int i = 0; i < _displaySlots.Count; i++)
            {
                if (IsSlotAlreadyVerifiedCorrect(i))
                {
                    _satisfaction++;
                    continue;
                }

                ClearSlotValidation(i);
            }
        }

        private void RememberVerifiedMatch(int slotIndex, InspirationData inspiration, ExhibitItemData item)
        {
            if (!ValidateSlotIndex(slotIndex) || inspiration == null || item == null)
                return;

            bool newlyKnown = KnownInspirationMatchIds.Add(inspiration.id);
            RemoveTransientUsesOfKnownInspiration(inspiration, slotIndex);
            RefreshBindingForCurrentSlot(slotIndex);
            item.RecordUsage(_currentTheme.title);

            if (newlyKnown)
                OnSlotInspirationChanged?.Invoke(slotIndex, inspiration);
        }

        private void RemoveTransientUsesOfKnownInspiration(InspirationData inspiration, int fixedSlotIndex)
        {
            for (int i = 0; i < _slotInspirations.Count; i++)
            {
                if (i == fixedSlotIndex ||
                    _slotInspirations[i] != inspiration ||
                    _displaySlots[i] == inspiration.mappedItem)
                {
                    continue;
                }

                _slotInspirations[i] = null;
                ClearSlotValidation(i);
                OnSlotInspirationChanged?.Invoke(i, null);
            }

            foreach (var state in _themeCurationStates.Values)
            {
                for (int i = 0; i < state.SlotInspirations.Count; i++)
                {
                    if (state.SlotInspirations[i] != inspiration ||
                        (i < state.DisplaySlots.Count && state.DisplaySlots[i] == inspiration.mappedItem))
                    {
                        continue;
                    }

                    state.SlotInspirations[i] = null;
                    if (i < state.SlotValidationResults.Count)
                        state.SlotValidationResults[i] = null;
                }
            }
        }

        private bool ClearInspirationFromOtherSlots(InspirationData inspiration, int targetSlotIndex)
        {
            if (inspiration == null)
                return true;

            for (int i = 0; i < _slotInspirations.Count; i++)
            {
                if (i == targetSlotIndex || _slotInspirations[i] != inspiration)
                    continue;

                _slotInspirations[i] = null;
                ClearSlotValidation(i);
                OnSlotInspirationChanged?.Invoke(i, null);
            }

            foreach (var pair in _themeCurationStates)
            {
                if (pair.Key == _currentTheme)
                    continue;

                var state = pair.Value;
                for (int i = 0; i < state.SlotInspirations.Count; i++)
                {
                    if (state.SlotInspirations[i] != inspiration)
                        continue;

                    state.SlotInspirations[i] = null;
                    if (i < state.SlotValidationResults.Count)
                        state.SlotValidationResults[i] = null;
                }
            }

            _inspirationItemBindings.Remove(inspiration);
            return true;
        }

        private void RefreshBindingForCurrentSlot(int slotIndex)
        {
            if (!ValidateSlotIndex(slotIndex))
                return;

            var inspiration = _slotInspirations[slotIndex];
            var item = _displaySlots[slotIndex];
            if (inspiration == null)
                return;

            if (item == null)
            {
                ClearBindingIfOwned(inspiration, _currentTheme, slotIndex);
                return;
            }

            _inspirationItemBindings[inspiration] = new InspirationItemBinding
            {
                Theme = _currentTheme,
                SlotIndex = slotIndex,
                Item = item
            };
        }

        private void ClearBindingIfOwned(InspirationData inspiration, ExhibitionTheme theme, int slotIndex)
        {
            if (inspiration == null ||
                !_inspirationItemBindings.TryGetValue(inspiration, out var binding) ||
                binding.Theme != theme ||
                binding.SlotIndex != slotIndex)
            {
                return;
            }

            _inspirationItemBindings.Remove(inspiration);
        }

        private void AutoAssignKnownInspiration(int slotIndex, ExhibitItemData item)
        {
            if (!ValidateSlotIndex(slotIndex) || item == null)
                return;

            var knownInspiration = GetKnownInspirationForItem(item);
            if (knownInspiration == null)
                return;

            RemoveTransientUsesOfKnownInspiration(knownInspiration, slotIndex);
            if (_slotInspirations[slotIndex] == knownInspiration)
            {
                RefreshBindingForCurrentSlot(slotIndex);
                return;
            }

            ClearBindingIfOwned(_slotInspirations[slotIndex], _currentTheme, slotIndex);
            _slotInspirations[slotIndex] = knownInspiration;
            ClearSlotValidation(slotIndex);
            RefreshBindingForCurrentSlot(slotIndex);
            OnSlotInspirationChanged?.Invoke(slotIndex, knownInspiration);
        }

        private void AutoAssignKnownInspirationsInCurrentTheme()
        {
            for (int i = 0; i < _displaySlots.Count; i++)
                AutoAssignKnownInspiration(i, _displaySlots[i]);
        }

        private void ClearFixedInspirationForItem(int slotIndex, ExhibitItemData item)
        {
            if (!ValidateSlotIndex(slotIndex) || item == null)
                return;

            var knownInspiration = GetKnownInspirationForItem(item);
            if (knownInspiration == null || _slotInspirations[slotIndex] != knownInspiration)
                return;

            ClearBindingIfOwned(knownInspiration, _currentTheme, slotIndex);
            _slotInspirations[slotIndex] = null;
            ClearSlotValidation(slotIndex);
            OnSlotInspirationChanged?.Invoke(slotIndex, null);
        }

        private void ClearMismatchedKnownInspiration(int slotIndex, ExhibitItemData item)
        {
            if (!ValidateSlotIndex(slotIndex))
                return;

            var inspiration = _slotInspirations[slotIndex];
            if (inspiration == null ||
                !IsInspirationMatchKnown(inspiration) ||
                inspiration.mappedItem == item)
            {
                return;
            }

            ClearBindingIfOwned(inspiration, _currentTheme, slotIndex);
            _slotInspirations[slotIndex] = null;
            ClearSlotValidation(slotIndex);
            OnSlotInspirationChanged?.Invoke(slotIndex, null);
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
                if (IsSlotAlreadyVerifiedCorrect(_visitorIndex))
                {
                    _visitorIndex++;
                    continue;
                }

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
            _slotValidationResults[_visitorIndex] = validation;
            if (validation.IsCorrect)
            {
                _satisfaction++;
                RememberVerifiedMatch(_visitorIndex, inspiration, item);
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
            {
                if (ShouldPrioritizeBirdwatchingFanHint(missingIds))
                    return _currentTheme.GetHintForMissingId(BIRD_DEITY_FAN_INSPIRATION_ID);

                return _currentTheme.GetHintForMissingId(missingIds[UnityEngine.Random.Range(0, missingIds.Count)]);
            }

            if (_validationResults.Any(result => !result.ItemCorrect))
                return "One of the exhibits does not fit. I should replace the item marked in red.";

            return "One of the exhibit labels does not fit. I should replace the label marked in red.";
        }

        private bool ShouldPrioritizeBirdwatchingFanHint(IReadOnlyCollection<int> missingIds)
        {
            return _currentTheme != null &&
                   _currentTheme.name == BIRDWATCHING_THEME_ASSET_NAME &&
                   missingIds != null &&
                   missingIds.Contains(BIRD_DEITY_FAN_INSPIRATION_ID);
        }

        private void SetState(ExhibitionState state)
        {
            if (_state == state) return;
            _state = state;
            OnStateChanged?.Invoke(_state);
        }
    }
}
