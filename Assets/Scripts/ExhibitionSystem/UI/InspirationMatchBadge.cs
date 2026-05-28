using ExhibitionSystem.Data;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ExhibitionSystem.UI
{
    public class InspirationMatchBadge : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private Image _badgeIcon;
        [SerializeField] private GameObject _tooltipPanel;
        [SerializeField] private Image _itemIcon;
        [SerializeField] private TMP_Text _tooltipText;
        [SerializeField] private string _matchedText = "Already\nmatched";

        private ExhibitItemData _matchedItem;

        private void Awake()
        {
            if (_tooltipPanel != null)
                _tooltipPanel.SetActive(false);
        }

        private void OnDisable()
        {
            if (_tooltipPanel != null)
                _tooltipPanel.SetActive(false);
        }

        public void SetData(ExhibitItemData matchedItem)
        {
            _matchedItem = matchedItem;
            bool hasMatch = matchedItem != null;
            gameObject.SetActive(hasMatch);

            if (_badgeIcon != null)
                _badgeIcon.raycastTarget = hasMatch;

            if (_itemIcon != null)
            {
                _itemIcon.sprite = matchedItem != null ? matchedItem.icon : null;
                _itemIcon.enabled = matchedItem != null && matchedItem.icon != null;
                _itemIcon.preserveAspect = true;
                _itemIcon.raycastTarget = false;
            }

            if (_tooltipText != null)
                _tooltipText.text = _matchedText;

            if (_tooltipPanel != null)
                _tooltipPanel.SetActive(false);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_matchedItem != null && _tooltipPanel != null)
                _tooltipPanel.SetActive(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_tooltipPanel != null)
                _tooltipPanel.SetActive(false);
        }
    }
}
