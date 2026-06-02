using System;
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
        private static readonly Color BodyFg = new Color32(0xc8, 0xd4, 0xc8, 0xFF);

        public static void ConfigureContainer(GameObject container)
        {
            if (container == null) return;

            var rect = container.transform as RectTransform;
            if (rect != null)
            {
                rect.anchorMin = new Vector2(0.25f, 0.32f);
                rect.anchorMax = new Vector2(0.75f, 0.72f);
                rect.anchoredPosition = Vector2.zero;
                rect.sizeDelta = Vector2.zero;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }

            var layout = container.GetComponent<VerticalLayoutGroup>();
            if (layout == null)
                layout = container.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.spacing = 14f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
        }

        public static Button AddButton(Transform parent, string name, string label,
                                       TMP_FontAsset font, Action action)
        {
            var buttonObject = new GameObject(name, typeof(RectTransform));
            buttonObject.transform.SetParent(parent, false);
            var rect = (RectTransform)buttonObject.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var image = buttonObject.AddComponent<Image>();
            var button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => action?.Invoke());

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

            var image = button.GetComponent<Image>();
            if (image != null)
            {
                image.color = ChoiceBg;
                image.type = Image.Type.Simple;
            }

            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color32(0xd9, 0xe2, 0xd9, 0xFF);
            colors.pressedColor = new Color32(0xb8, 0xc8, 0xb8, 0xFF);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color32(0x80, 0x88, 0x80, 0x88);
            button.colors = colors;

            var layout = button.GetComponent<LayoutElement>();
            if (layout == null)
                layout = button.gameObject.AddComponent<LayoutElement>();
            layout.minHeight = 64f;
            layout.preferredHeight = 72f;
            layout.flexibleWidth = 1f;

            var label = button.GetComponentInChildren<TMP_Text>();
            if (label == null) return;
            if (font != null) label.font = font;
            label.fontSize = 28f;
            label.fontStyle = FontStyles.Normal;
            label.color = BodyFg;
            label.alignment = TextAlignmentOptions.Center;
            label.margin = new Vector4(24f, 8f, 24f, 8f);
            label.raycastTarget = false;
        }
    }
}
