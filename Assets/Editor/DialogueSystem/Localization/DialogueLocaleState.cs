using System;
using System.Collections.Generic;
using UnityEngine.Localization;

namespace DialogueSystem.Editor
{
    /// <summary>
    /// Holds the currently-selected preview locale for one editor window
    /// instance and broadcasts changes to all subscribers.
    ///
    /// This is intentionally NOT static — each open editor window owns its
    /// own instance, so two windows can preview different locales simultaneously.
    ///
    /// Consumers (DialogueNodeView, DialogueNodeInspectorPanel) receive a
    /// reference to this object on construction and subscribe to OnLocaleChanged.
    /// They must unsubscribe when detached to avoid leaks.
    /// </summary>
    public class DialogueLocaleState
    {
        // ── Events ────────────────────────────────────────────────────────────

        /// <summary>
        /// Fired whenever <see cref="ActiveLocale"/> changes.
        /// Subscribers should refresh any locale-dependent display labels.
        /// </summary>
        public event Action OnLocaleChanged;

        // ── State ─────────────────────────────────────────────────────────────

        private Locale       _activeLocale;
        private List<Locale> _allLocales;

        // ── Constructor ───────────────────────────────────────────────────────

        public DialogueLocaleState()
        {
            Refresh();
        }

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>All locales available in the StringTable collections.</summary>
        public IReadOnlyList<Locale> AllLocales => _allLocales;

        /// <summary>
        /// The locale currently selected for canvas preview.
        /// Setting this fires <see cref="OnLocaleChanged"/>.
        /// </summary>
        public Locale ActiveLocale
        {
            get => _activeLocale;
            set
            {
                if (_activeLocale == value) return;
                _activeLocale = value;
                OnLocaleChanged?.Invoke();
            }
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Re-fetches available locales from the localization settings.
        /// Call after the locale set changes (e.g. a new locale was added).
        /// </summary>
        public void Refresh()
        {
            _allLocales   = LocalizationTableService.GetAllLocales();
            _activeLocale = _allLocales.Count > 0 ? _allLocales[0] : null;
        }

        /// <summary>
        /// Resolves a dialogue text key in the active locale from the text collection.
        /// </summary>
        public string ResolveText(string key)
        {
            if (_activeLocale == null || string.IsNullOrEmpty(key)) return string.Empty;
            return LocalizationTableService.GetTextEntry(_activeLocale, key);
        }

        /// <summary>
        /// Resolves a character's localized display name in the active locale.
        /// The key is Character.characterName.
        /// </summary>
        public string ResolveCharacterName(string characterNameKey)
        {
            if (_activeLocale == null || string.IsNullOrEmpty(characterNameKey)) return string.Empty;
            return LocalizationTableService.GetCharacterNameEntry(_activeLocale, characterNameKey);
        }

        /// <summary>
        /// Resolves a choice label key in the active locale from the choice label collection.
        /// </summary>
        public string ResolveChoiceLabel(string key)
        {
            if (_activeLocale == null || string.IsNullOrEmpty(key)) return string.Empty;
            return LocalizationTableService.GetChoiceLabelEntry(_activeLocale, key);
        }

        /// <summary>Legacy single-table resolve — kept for callers that don't care which table.</summary>
        public string Resolve(string key) => ResolveText(key);
    }
}
