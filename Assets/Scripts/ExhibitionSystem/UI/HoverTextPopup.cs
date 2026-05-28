using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ExhibitionSystem.UI
{
    [DisallowMultipleComponent]
    public class HoverTextPopup : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private string _message;
        [SerializeField] private bool _popupEnabled;
        [SerializeField] private Vector2 _offset = new(0f, 64f);

        private GameObject _popup;
        private TMP_Text _text;
        private RectTransform _popupRect;

        public void SetMessage(string message)
        {
            _message = message;
            if (_text != null)
                _text.text = _message;
        }

        public void SetPopupEnabled(bool enabled)
        {
            _popupEnabled = enabled;
            if (!_popupEnabled)
                Hide();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!_popupEnabled || string.IsNullOrWhiteSpace(_message))
                return;

            EnsurePopup();
            if (_popup == null)
                return;

            _text.text = _message;
            _popup.SetActive(true);
            PositionPopup();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            Hide();
        }

        private void Hide()
        {
            if (_popup != null)
                _popup.SetActive(false);
        }

        private void EnsurePopup()
        {
            if (_popup != null)
                return;

            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
                return;

            _popup = new GameObject("HoverTextPopup", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            _popup.transform.SetParent(canvas.transform, false);
            _popup.transform.SetAsLastSibling();
            _popup.SetActive(false);

            _popupRect = _popup.GetComponent<RectTransform>();
            _popupRect.anchorMin = new Vector2(0.5f, 0.5f);
            _popupRect.anchorMax = new Vector2(0.5f, 0.5f);
            _popupRect.pivot = new Vector2(0.5f, 0f);
            _popupRect.sizeDelta = new Vector2(360f, 74f);

            var background = _popup.GetComponent<Image>();
            background.color = new Color(0.1f, 0.07f, 0.04f, 0.96f);
            background.raycastTarget = false;

            var textObject = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(_popup.transform, false);
            _text = textObject.GetComponent<TextMeshProUGUI>();
            _text.text = _message;
            _text.fontSize = 20f;
            _text.alignment = TextAlignmentOptions.Center;
            _text.textWrappingMode = TextWrappingModes.Normal;
            _text.color = new Color(0.98f, 0.9f, 0.72f, 1f);
            _text.raycastTarget = false;

            var textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(14f, 10f);
            textRect.offsetMax = new Vector2(-14f, -10f);
        }

        private void PositionPopup()
        {
            if (_popupRect == null)
                return;

            var sourceRect = transform as RectTransform;
            if (sourceRect == null)
                return;

            var canvas = GetComponentInParent<Canvas>();
            var canvasRect = canvas != null ? canvas.GetComponent<RectTransform>() : null;
            if (canvasRect == null)
                return;

            Vector3 worldPosition = sourceRect.TransformPoint(sourceRect.rect.center);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                RectTransformUtility.WorldToScreenPoint(canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera, worldPosition),
                canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
                out Vector2 localPoint);

            _popupRect.anchoredPosition = localPoint + _offset;
        }
    }
}
