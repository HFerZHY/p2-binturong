using Otowa.SaveSystem;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Otowa.Controls
{
    public static class UnifiedInput
    {
        public static bool WasAdvancePressed(bool respectPauseSuppression = true)
        {
            return WasMouseConfirmPressed(respectPauseSuppression)
                   || WasKeyboardConfirmPressed(respectPauseSuppression);
        }

        public static bool WasMouseConfirmPressed(bool respectPauseSuppression = true)
        {
            if (respectPauseSuppression && PauseMenuController.ShouldSuppressWorldAdvance)
                return false;

            return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        }

        public static bool WasKeyboardConfirmPressed(bool respectPauseSuppression = true)
        {
            if (respectPauseSuppression && PauseMenuController.ShouldSuppressWorldAdvance)
                return false;

            var keyboard = Keyboard.current;
            return keyboard != null
                   && (keyboard.spaceKey.wasPressedThisFrame
                       || keyboard.enterKey.wasPressedThisFrame
                       || keyboard.numpadEnterKey.wasPressedThisFrame);
        }

        public static bool WasCancelPressed(bool respectPauseSuppression = true)
        {
            if (respectPauseSuppression && PauseMenuController.ShouldSuppressWorldAdvance)
                return false;

            return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
        }

        public static void SelectButton(Button button)
        {
            if (button == null || !button.isActiveAndEnabled || !button.interactable)
                return;

            EventSystem.current?.SetSelectedGameObject(button.gameObject);
        }
    }
}
