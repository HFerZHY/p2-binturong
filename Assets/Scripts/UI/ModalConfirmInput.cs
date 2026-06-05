using Otowa.Controls;
using Otowa.SaveSystem;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Otowa.UI
{
    [DisallowMultipleComponent]
    public class ModalConfirmInput : MonoBehaviour
    {
        [SerializeField] private Button _confirmButton;
        [SerializeField] private Button _cancelButton;
        [SerializeField] private bool _cancelWithEscape;
        [SerializeField] private bool _selectConfirmOnEnable;

        private int _inputUnlockFrame;
        private int _lastHandledFrame = -1;

        public void Configure(
            Button confirmButton,
            Button cancelButton = null,
            bool cancelWithEscape = false,
            bool selectConfirmOnEnable = false)
        {
            _confirmButton = confirmButton;
            _cancelButton = cancelButton;
            _cancelWithEscape = cancelWithEscape;
            _selectConfirmOnEnable = selectConfirmOnEnable;

            if (isActiveAndEnabled && _selectConfirmOnEnable)
                UnifiedInput.SelectButton(_confirmButton);
        }

        private void OnEnable()
        {
            _inputUnlockFrame = Time.frameCount + 1;
            EventSystem.current?.SetSelectedGameObject(null);
            if (_selectConfirmOnEnable)
                UnifiedInput.SelectButton(_confirmButton);
        }

        private void Update()
        {
            if (Time.frameCount <= _inputUnlockFrame || Time.frameCount == _lastHandledFrame)
                return;

            if (!IsInputAvailable())
                return;

            if (_cancelWithEscape && UnifiedInput.WasCancelPressed() && TryInvoke(_cancelButton))
                return;

            if (UnifiedInput.WasKeyboardConfirmPressed())
                TryInvoke(_confirmButton);
        }

        private bool IsInputAvailable()
        {
            foreach (var canvasGroup in GetComponentsInParent<CanvasGroup>(true))
            {
                if (canvasGroup == null)
                    continue;

                if (canvasGroup.alpha <= 0.001f ||
                    !canvasGroup.interactable ||
                    !canvasGroup.blocksRaycasts)
                {
                    return false;
                }
            }

            return true;
        }

        private bool TryInvoke(Button button)
        {
            if (button == null || !button.gameObject.activeInHierarchy || !button.IsInteractable())
                return false;

            _lastHandledFrame = Time.frameCount;
            PauseMenuController.SuppressWorldAdvanceForInputFrame();
            button.onClick.Invoke();
            return true;
        }
    }
}
