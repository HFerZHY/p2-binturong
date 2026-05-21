using System.Collections.Generic;
using InventorySystem;
using InventorySystem.Data;
using StationWork.Core;
using UnityEngine;

namespace StationWork.UI
{
    /// <summary>
    /// Spawns and refreshes ItemCardUI prefabs from the player's inventory.
    /// Place this on a horizontal/grid layout group that holds the item row.
    ///
    /// SETUP (Inspector)
    ///   _itemCardPrefab — your ItemCardUI prefab
    ///   _cardContainer  — the layout group RectTransform for the item row
    ///   Call StationWorkManager.Instance.Begin() from a start button or Awake.
    /// </summary>
    public class StationWorkUIManager : MonoBehaviour
    {
        [SerializeField] private ItemCardUI   _itemCardPrefab;
        [SerializeField] private RectTransform _cardContainer;

        private readonly List<ItemCardUI> _cards = new();

        private void OnEnable()
        {
            if (InventoryManager.Instance != null)
                InventoryManager.Instance.InventoryChanged += OnInventoryChanged;

            StationWorkManager.OnPhaseComplete += OnPhaseCompleteHandler;
        }

        private void OnDisable()
        {
            if (InventoryManager.Instance != null)
                InventoryManager.Instance.InventoryChanged -= OnInventoryChanged;

            StationWorkManager.OnPhaseComplete -= OnPhaseCompleteHandler;
        }

        private void Start()
        {
            RebuildCards(InventoryManager.Instance?.Slots);
        }

        private void OnInventoryChanged(IReadOnlyList<InventorySlot> slots)
        {
            RebuildCards(slots);
        }

        private void RebuildCards(IReadOnlyList<InventorySlot> slots)
        {
            foreach (var card in _cards)
                if (card != null) Destroy(card.gameObject);
            _cards.Clear();

            if (slots == null || _itemCardPrefab == null || _cardContainer == null) return;

            foreach (var slot in slots)
            {
                var card = Instantiate(_itemCardPrefab, _cardContainer);
                card.SetData(slot);
                _cards.Add(card);
            }
        }

        private void RefreshAllCards()
        {
            if (InventoryManager.Instance == null) return;
            var slots = InventoryManager.Instance.Slots;
            for (int i = 0; i < _cards.Count && i < slots.Count; i++)
                _cards[i].RefreshLabels();
        }

        private void OnPhaseCompleteHandler(int score, int total) => OnPhaseComplete();

        private void OnPhaseComplete()
        {
            foreach (var card in _cards)
                if (card != null) card.gameObject.SetActive(false);
        }
    }
}
