using System;
using Otowa.SaveSystem;
using Otowa.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Otowa.IndoorDialogue
{
    /// <summary>
    /// Shared runtime choice styling based on the map dialogue choices used in JunkoIntro.
    /// </summary>
    public static class IndoorDialogueChoiceStyle
    {
        private static readonly Color ChoiceBg = new Color32(0x06, 0x0e, 0x06, 0xD8);
        private static readonly Color ChoiceDisabledBg = new Color32(0x06, 0x0a, 0x06, 0x80);
        private static readonly Color BodyFg = new Color32(0xc8, 0xd4, 0xc8, 0xFF);
        private static readonly Color HoverFg = new Color32(0xff, 0xf0, 0xb8, 0xFF);
        private static readonly Color DisabledFg = new Color32(0x80, 0x88, 0x80, 0x99);
        private static readonly Color HoverOutline = new Color32(0xff, 0xd8, 0x42, 0xE8);

        public static void ConfigureContainer(GameObject container)
        {
            if (container == null) return;

            var rect = container.transform as RectTransform;
            if (rect != null)
            {
                rect.anchorMin = new Vector2(0.20f, 0.30f);
                rect.anchorMax = new Vector2(0.80f, 0.76f);
                rect.anchoredPosition = Vector2.zero;
                rect.sizeDelta = Vector2.zero;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }

            var layout = container.GetComponent<VerticalLayoutGroup>();
            if (layout == null)
                layout = container.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.spacing = 18f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
        }

        public static Button AddButton(Transform parent, string name, string label,
                                       TMP_FontAsset font, Action action)
        {
            font = RuntimeFontLibrary.BreeSerifRegularOr(font);
            var buttonObject = new GameObject(name, typeof(RectTransform));
            buttonObject.transform.SetParent(parent, false);
            var rect = (RectTransform)buttonObject.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var image = buttonObject.AddComponent<Image>();
            var button = buttonObject.AddComponent<Button>();
            buttonObject.AddComponent<HoverConfirmButton>();
            button.targetGraphic = image;
            button.onClick.AddListener(() =>
            {
                PauseMenuController.SuppressWorldAdvanceForInputFrame();
                action?.Invoke();
            });

            var labelObject = new GameObject("Label", typeof(RectTransform));
            labelObject.transform.SetParent(buttonObject.transform, false);
            var labelRect = (RectTransform)labelObject.transform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            var text = labelObject.AddComponent<TextMeshProUGUI>();
            text.text = label;
            ApplyButton(button, font);
            return button;
        }

        public static void ApplyButton(Button button, TMP_FontAsset font)
        {
            if (button == null) return;
            font = RuntimeFontLibrary.BreeSerifRegularOr(font);

            var image = button.GetComponent<Image>();
            if (image != null)
            {
                image.color = ChoiceBg;
                image.type = Image.Type.Simple;
                image.raycastTarget = true;
            }

            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = Color.white;
            colors.pressedColor = Color.white;
            colors.selectedColor = Color.white;
            colors.disabledColor = Color.white;
            button.colors = colors;
            button.transition = Selectable.Transition.None;
            button.targetGraphic = image;

            var layout = button.GetComponent<LayoutElement>();
            if (layout == null)
                layout = button.gameObject.AddComponent<LayoutElement>();
            layout.minHeight = 88f;
            layout.preferredHeight = 98f;
            layout.flexibleWidth = 1f;

            var label = button.GetComponentInChildren<TMP_Text>();
            if (label == null) return;
            if (font != null) label.font = font;
            label.fontSize = 36f;
            label.fontStyle = FontStyles.Normal;
            label.color = BodyFg;
            label.alignment = TextAlignmentOptions.Center;
            label.margin = new Vector4(28f, 10f, 28f, 10f);
            label.raycastTarget = false;

            var hover = button.GetComponent<IndoorDialogueChoiceHover>();
            if (hover == null)
                hover = button.gameObject.AddComponent<IndoorDialogueChoiceHover>();
            hover.Initialize(button, image, label,
                ChoiceBg, ChoiceDisabledBg,
                BodyFg, HoverFg, DisabledFg, HoverOutline);
        }
    }
}
