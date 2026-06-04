using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Otowa.IndoorDialogue
{
    public class IndoorDialogueChoiceHover : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler,
        ISelectHandler, IDeselectHandler
    {
        private Button _button;
        private Image _background;
        private TMP_Text _label;
        private Outline _outline;

        private Color _normalBg;
        private Color _disabledBg;
        private Color _normalText;
        private Color _hoverText;
        private Color _disabledText;
        private Color _hoverOutline;
        private Color _clearOutline;

        private bool _hovered;
        private bool _selected;
        private bool _pressed;
        private bool _lastInteractable = true;

        public void Initialize(Button button, Image background, TMP_Text label,
                               Color normalBg, Color disabledBg,
                               Color normalText, Color hoverText, Color disabledText,
                               Color hoverOutline)
        {
            _button = button;
            _background = background;
            _label = label;
            _normalBg = normalBg;
            _disabledBg = disabledBg;
            _normalText = normalText;
            _hoverText = hoverText;
            _disabledText = disabledText;
            _hoverOutline = hoverOutline;
            _clearOutline = new Color(hoverOutline.r, hoverOutline.g, hoverOutline.b, 0f);

            if (_background != null)
            {
                _outline = _background.GetComponent<Outline>();
                if (_outline == null)
                    _outline = _background.gameObject.AddComponent<Outline>();

                _outline.effectDistance = new Vector2(4f, -4f);
                _outline.useGraphicAlpha = false;
            }

            _lastInteractable = IsInteractable();
            RefreshVisual();
        }

        private void OnEnable()
        {
            _hovered = false;
            _selected = false;
            _pressed = false;
            _lastInteractable = IsInteractable();
            RefreshVisual();
        }

        private void OnDisable()
        {
            _hovered = false;
            _selected = false;
            _pressed = false;
            RefreshVisual();
        }

        private void LateUpdate()
        {
            bool interactable = IsInteractable();
            if (interactable == _lastInteractable)
                return;

            _lastInteractable = interactable;
            if (!interactable)
            {
                _hovered = false;
                _selected = false;
                _pressed = false;
            }

            RefreshVisual();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!IsInteractable()) return;
            _hovered = true;
            RefreshVisual();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _hovered = false;
            _pressed = false;
            RefreshVisual();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!IsInteractable()) return;
            _pressed = true;
            RefreshVisual();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _pressed = false;
            RefreshVisual();
        }

        public void OnSelect(BaseEventData eventData)
        {
            if (!IsInteractable()) return;
            _selected = true;
            RefreshVisual();
        }

        public void OnDeselect(BaseEventData eventData)
        {
            _selected = false;
            _pressed = false;
            RefreshVisual();
        }

        private bool IsInteractable()
        {
            return _button == null || _button.IsInteractable();
        }

        private void RefreshVisual()
        {
            bool interactable = IsInteractable();
            Color bg = _normalBg;
            Color text = _normalText;
            Color outline = _clearOutline;

            if (!interactable)
            {
                bg = _disabledBg;
                text = _disabledText;
            }
            else if (_pressed)
            {
                bg = _normalBg;
                text = _hoverText;
                outline = _hoverOutline;
            }
            else if (_hovered || _selected)
            {
                bg = _normalBg;
                text = _hoverText;
                outline = _hoverOutline;
            }

            if (_background != null)
                _background.color = bg;
            if (_label != null)
                _label.color = text;
            if (_outline != null)
                _outline.effectColor = outline;
        }
    }
}
