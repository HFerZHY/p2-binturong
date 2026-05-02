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
        
        /// <summary>
        /// Fired when a speaker's translated value is edited in the inspector.
        /// All node views whose speakerNameKey matches should refresh their preview.
        /// </summary>
        public event Action<string> OnSpeakerValueChanged;

        // ── State ─────────────────────────────────────────────────────────────

        private Locale       _activeLocale;
        private List<Locale> _allLocales;

        // ── Constructor ───────────────────────────────────────────────────────

        public DialogueLocaleState()
        {
            Refresh();
        }

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>All locales available in the StringTable collection.</summary>
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
        /// Re-fetches available locales from the StringTable collection.
        /// Call after the collection changes (e.g. a new locale was added).
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
        /// Resolves a speaker name key in the active locale from the speaker collection.
        /// </summary>
        public string ResolveSpeaker(string key)
        {
            if (_activeLocale == null || string.IsNullOrEmpty(key)) return string.Empty;
            return LocalizationTableService.GetSpeakerEntry(_activeLocale, key);
        }
        
        public void NotifySpeakerValueChanged(string speakerKey)
            => OnSpeakerValueChanged?.Invoke(speakerKey);

        /// <summary>Legacy single-table resolve — kept for callers that don't care which table.</summary>
        public string Resolve(string key) => ResolveText(key);
    }
}
