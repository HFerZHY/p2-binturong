using System;
using System.Collections.Generic;
using System.Linq;
using Base;
using InventorySystem;
using InventorySystem.Data;
using StationWork.Data;
using UnityEngine;

namespace StationWork.Core
{
    /// <summary>
    /// Drives the station work phase: cycles through passengers, validates
    /// item submissions, fixes labels, and tracks score.
    ///
    /// USAGE
    ///   Call Begin() to start. Subscribe to the static events for UI updates.
    ///   Call SubmitItem(ItemData) when the player drops/selects an item.
    ///
    /// SETUP (Inspector)
    ///   Assign the ordered list of PassengerData assets.
    ///   Optionally assign starter items to give the player at Begin().
    /// </summary>
    public class StationWorkManager : MonoSingleton<StationWorkManager>
    {
        [Header("Passengers")]
        [SerializeField] private List<PassengerData> passengers = new();

        [Header("Starter Items (given to player on Begin)")]
        [SerializeField] private List<ItemData> starterItems = new();

        [Header("Auto")]
        [Tooltip("Call Begin() automatically when the scene starts.")]
        [SerializeField] private bool _autoBegin = true;

        // ── Runtime state ──────────────────────────────────────────────────────

        private int _passengerIndex;
        private int _score;
        private int _attemptsOnCurrent;
        private bool _active;

        public PassengerData CurrentPassenger
            => (_active && _passengerIndex < passengers.Count) ? passengers[_passengerIndex] : null;

        public int Score => _score;
        public bool IsActive => _active;

        // ── Events (static so any UI can subscribe without a scene reference) ──

        /// <summary>Fired when a new passenger becomes active.</summary>
        public static event Action<PassengerData> OnPassengerArrived;

        /// <summary>
        /// Fired after an item is submitted.
        /// bool correct — whether the item matched.
        /// bool passengerLeaving — true if the passenger is done (correct OR 2nd wrong attempt).
        /// </summary>
        public static event Action<bool, bool> OnItemSubmitted;

        /// <summary>Fired when all passengers have been handled.</summary>
        public static event Action<int, int> OnPhaseComplete; // score, total

        // ── Unity lifecycle ────────────────────────────────────────────────────

        private void Start()
        {
            if (_autoBegin) Begin();
        }

        // ── Public API ─────────────────────────────────────────────────────────

        public void Begin()
        {
            _passengerIndex   = 0;
            _score            = 0;
            _attemptsOnCurrent = 0;
            _active           = true;

            foreach (var item in starterItems)
                InventoryManager.Instance?.Add(item);

            if (CurrentPassenger != null)
                OnPassengerArrived?.Invoke(CurrentPassenger);
            else
                Debug.LogWarning("[StationWorkManager] No passengers configured.");
        }

        /// <summary>
        /// Called when the player drops/selects an item for the current passenger.
        /// </summary>
        public void SubmitItem(ItemData item)
        {
            if (!_active || CurrentPassenger == null || item == null) return;

            bool correct = item == CurrentPassenger.correctItem;

            if (correct)
            {
                _score++;
                FixLabel(CurrentPassenger.correctItem, CurrentPassenger.requiredLabel);
                OnItemSubmitted?.Invoke(true, true);
                Advance();
            }
            else
            {
                _attemptsOnCurrent++;
                bool leaving = _attemptsOnCurrent >= 2;
                OnItemSubmitted?.Invoke(false, leaving);
                if (leaving) Advance();
            }
        }

        // ── Private ────────────────────────────────────────────────────────────

        private void Advance()
        {
            _passengerIndex++;
            _attemptsOnCurrent = 0;

            if (_passengerIndex >= passengers.Count)
            {
                _active = false;
                OnPhaseComplete?.Invoke(_score, passengers.Count);
            }
            else
            {
                OnPassengerArrived?.Invoke(CurrentPassenger);
            }
        }

        private void FixLabel(ItemData item, string label)
        {
            if (InventoryManager.Instance == null) return;
            var slot = InventoryManager.Instance.Slots.FirstOrDefault(s => s.item == item);
            slot?.FixLabel(label);
        }
    }
}
