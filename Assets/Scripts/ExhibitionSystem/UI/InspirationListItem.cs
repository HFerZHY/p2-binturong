using System;
using ExhibitionSystem.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ExhibitionSystem.UI
{
    public class InspirationListItem : MonoBehaviour
    {
        [SerializeField] private TMP_Text _idText;
        [SerializeField] private TMP_Text _bodyText;
        [SerializeField] private TMP_Text _matchText;
        [SerializeField] private Image _selectionImage;
        [SerializeField] private Image _selectionFrame;
        [SerializeField] private Button _button;
        [SerializeField] private Color _normalColor = new(0.74f, 0.63f, 0.48f, 0.98f);
        [SerializeField] private Color _selectedColor = new(0.92f, 0.76f, 0.42f, 1f);
        [SerializeField] private Color _invalidColor = new(0.72f, 0.18f, 0.14f, 1f);

        private InspirationData _inspiration;
        private Action<InspirationData> _onClick;
        private bool _selected;
        private bool _invalid;

        private void Awake()
        {
            if (_button != null)
                _button.onClick.AddListener(HandleClick);
        }

        public void SetData(
            InspirationData inspiration,
            bool selected,
            Action<InspirationData> onClick,
            bool invalid = false,
            bool animateInvalid = false)
        {
            _inspiration = inspiration;
            _onClick = onClick;
            _selected = selected;
            _invalid = invalid;

            if (_idText != null)
                _idText.gameObject.SetActive(false);

            if (_bodyText != null)
            {
                _bodyText.text = inspiration != null ? inspiration.text : string.Empty;
                _bodyText.maxVisibleLines = 2;
            }

            if (_matchText != null)
                _matchText.gameObject.SetActive(false);

            UpdateVisual();
        }

        public void SetSelected(bool selected)
        {
            _selected = selected;
            UpdateVisual();
        }

        public void SetInvalid(bool invalid, bool animate)
        {
            _invalid = invalid;
            UpdateVisual();
        }

        private void HandleClick()
        {
            if (_inspiration != null)
                _onClick?.Invoke(_inspiration);
        }

        private void UpdateVisual()
        {
            if (_selectionImage != null)
                _selectionImage.color = _invalid ? _invalidColor : (_selected ? _selectedColor : _normalColor);

            if (_selectionFrame != null)
                _selectionFrame.enabled = _selected || _invalid;
        }

    }
}
