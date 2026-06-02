using ExhibitionSystem.Core;
using ExhibitionSystem.Data;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ExhibitionSystem.UI
{
    public class InspirationDisplaySlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private TMP_Text _inspirationText;
        [SerializeField] private TMP_Text _statusText;
        [SerializeField] private DisplaySlotUI _displaySlot;
        [SerializeField] private GameObject _tooltipPanel;
        [SerializeField] private TMP_Text _tooltipText;
        [SerializeField] private Button _labelButton;
        [SerializeField] private Image _labelBackground;

        [Header("Label Colors")]
        [SerializeField] private Color _labelDefaultColor = Color.white;
        [SerializeField] private Color _labelCorrectColor = new(0.65f, 0.92f, 0.58f, 1f);
        [SerializeField] private Color _labelIncorrectColor = new(0.95f, 0.48f, 0.44f, 1f);

        private int _slotIndex;
        private InspirationData _inspiration;
        private ExhibitItemData _item;

        public DisplaySlotUI DisplaySlot => _displaySlot;
        public InspirationData Inspiration => _inspiration;

        private void Awake()
        {
            EnsureLabelReferences();
            if (_labelButton != null)
                _labelButton.onClick.AddListener(HandleLabelClicked);
        }

        public void SetData(int slotIndex, InspirationData inspiration, ExhibitItemData item)
        {
            _slotIndex = slotIndex;
            SetInspiration(inspiration);
            SetItem(item);

            if (_tooltipPanel != null)
                _tooltipPanel.SetActive(false);

            if (_displaySlot != null)
            {
                _displaySlot.SetSlotIndex(slotIndex);
                _displaySlot.SetLocked(false);
            }

            ClearFeedback();
        }

        public void SetInspiration(InspirationData inspiration)
        {
            _inspiration = inspiration;

            if (_inspirationText != null)
                _inspirationText.text = inspiration != null ? ToShortLabel(inspiration.text) : "Select inspiration";

            if (_tooltipText != null)
                _tooltipText.text = inspiration != null ? inspiration.text : "Click to choose an inspiration label.";

            UpdateLabelInteractable();
        }

        public void SetItem(ExhibitItemData item)
        {
            _item = item;
            if (_displaySlot != null)
                _displaySlot.SetItem(item);

            UpdateLabelInteractable();
        }

        public void ClearItem()
        {
            _item = null;
            if (_displaySlot != null)
                _displaySlot.ClearItem();

            UpdateLabelInteractable();
        }

        public void ShowFeedback(ExhibitionSlotValidation validation)
        {
            if (_displaySlot != null)
                _displaySlot.ShowFeedback(validation.ItemCorrect);

            SetLabelColor(validation.InspirationCorrect switch
            {
                true => _labelCorrectColor,
                false => _labelIncorrectColor,
                _ => _labelDefaultColor
            });

            if (_statusText != null)
                _statusText.text = validation.IsCorrect ? "Correct" : "Try again";
        }

        public void ClearFeedback()
        {
            if (_displaySlot != null)
                _displaySlot.ClearFeedback();

            SetLabelColor(_labelDefaultColor);
            if (_statusText != null)
                _statusText.text = _inspiration != null ? "Label selected" : "Choose a label";
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_tooltipPanel != null)
                _tooltipPanel.SetActive(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_tooltipPanel != null)
                _tooltipPanel.SetActive(false);
        }

        private void HandleLabelClicked()
        {
            var manager = ExhibitionManager.Instance;
            if (manager == null || manager.IsRunning || _item == null)
                return;

            ExhibitionUIManager.Instance?.ShowInspirationPopupForSlot(_slotIndex);
        }

        private void EnsureLabelReferences()
        {
            var label = transform.Find("LabelStrip");
            if (label == null)
                return;

            if (_labelBackground == null)
                _labelBackground = label.GetComponent<Image>();

            if (_labelButton == null)
            {
                _labelButton = label.GetComponent<Button>();
                if (_labelButton == null)
                    _labelButton = label.gameObject.AddComponent<Button>();
            }

            if (_labelButton != null && _labelBackground != null)
                _labelButton.targetGraphic = _labelBackground;
        }

        private void UpdateLabelInteractable()
        {
            EnsureLabelReferences();
            if (_labelButton != null)
                _labelButton.interactable = _item != null && !(ExhibitionManager.Instance?.IsRunning ?? false);
        }

        private void SetLabelColor(Color color)
        {
            EnsureLabelReferences();
            if (_labelBackground != null)
                _labelBackground.color = color;
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
