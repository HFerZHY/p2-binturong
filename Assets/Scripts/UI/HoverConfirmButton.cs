using Otowa.Controls;
using Otowa.SaveSystem;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Otowa.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    public class HoverConfirmButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private Button _button;
        private bool _hovered;
        private int _lastHandledFrame = -1;

        private void Awake()
        {
            _button = GetComponent<Button>();
        }

        private void Update()
        {
            if (!_hovered || Time.frameCount == _lastHandledFrame)
                return;

            if (!UnifiedInput.WasKeyboardConfirmPressed())
                return;

            if (_button == null || !_button.gameObject.activeInHierarchy || !_button.IsInteractable())
                return;

            _lastHandledFrame = Time.frameCount;
            PauseMenuController.SuppressWorldAdvanceForInputFrame();
            _button.onClick.Invoke();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _hovered = true;
            EventSystem.current?.SetSelectedGameObject(null);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _hovered = false;
            if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gameObject)
                EventSystem.current.SetSelectedGameObject(null);
        }

        private void OnDisable()
        {
            _hovered = false;
        }
    }
}
