using ExhibitionSystem.Core;
using ExhibitionSystem.Data;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ExhibitionSystem.UI
{
    public class InspirationDisplaySlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private TMP_Text _inspirationText;
        [SerializeField] private TMP_Text _statusText;
        [SerializeField] private DisplaySlotUI _displaySlot;
        [SerializeField] private GameObject _tooltipPanel;
        [SerializeField] private TMP_Text _tooltipText;

        private int _slotIndex;
        private InspirationData _inspiration;

        public DisplaySlotUI DisplaySlot => _displaySlot;
        public InspirationData Inspiration => _inspiration;

        public void SetData(int slotIndex, InspirationData inspiration, ExhibitItemData item, bool locked)
        {
            _slotIndex = slotIndex;
            _inspiration = inspiration;

            if (_inspirationText != null)
                _inspirationText.text = inspiration != null ? ToShortLabel(inspiration.text) : string.Empty;

            if (_tooltipText != null)
                _tooltipText.text = inspiration != null ? inspiration.text : string.Empty;

            if (_tooltipPanel != null)
                _tooltipPanel.SetActive(false);

            if (_statusText != null)
                _statusText.text = locked ? "Matched" : "Find the item";

            if (_displaySlot != null)
            {
                _displaySlot.SetSlotIndex(slotIndex);
                _displaySlot.SetLocked(locked);
                _displaySlot.SetItem(item);
            }
        }

        public void SetItem(ExhibitItemData item)
        {
            if (_displaySlot != null)
                _displaySlot.SetItem(item);
        }

        public void ClearItem()
        {
            if (_displaySlot != null)
                _displaySlot.ClearItem();
        }

        public void ShowFeedback(bool isCorrect)
        {
            if (_displaySlot != null)
                _displaySlot.ShowFeedback(isCorrect);

            if (_statusText != null)
                _statusText.text = isCorrect ? "Correct" : "Try again";
        }

        public void ClearFeedback()
        {
            if (_displaySlot != null)
                _displaySlot.ClearFeedback();

            var manager = ExhibitionManager.Instance;
            bool locked = manager != null && manager.IsSlotLocked(_slotIndex);
            if (_statusText != null)
                _statusText.text = locked ? "Matched" : "Find the item";
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_tooltipPanel != null && _inspiration != null)
                _tooltipPanel.SetActive(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_tooltipPanel != null)
                _tooltipPanel.SetActive(false);
        }

        private static string ToShortLabel(string text)
        {
            const int MAX_LENGTH = 24;

            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            if (text.Length <= MAX_LENGTH)
                return text;

            int cut = text.LastIndexOf(' ', MAX_LENGTH);
            if (cut < 10)
                cut = MAX_LENGTH;

            return text.Substring(0, cut).TrimEnd('.', ',', ';', ':', ' ') + "...";
        }
    }
}
