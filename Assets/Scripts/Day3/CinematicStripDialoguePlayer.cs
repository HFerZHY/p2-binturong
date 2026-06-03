using TMPro;
using Otowa.IndoorDialogue;
using UnityEngine;
using UnityEngine.UI;

namespace Otowa.Day3
{
    public enum CinematicStripPortraitFocus
    {
        None,
        Left,
        Right,
        SecondaryRight,
        Both
    }

    public class CinematicStripDialoguePlayer : MonoBehaviour
    {
        private GameObject _root;
        private Transform _stripTransform;
        private Image _stripBackground;
        private Image _leftPortrait;
        private Image _rightPortrait;
        private Image _secondaryRightPortrait;
        private TextMeshProUGUI _speakerText;
        private TextMeshProUGUI _bodyText;
        private IndoorDialogueTextPlayer _textPlayer;

        public bool IsTyping => _textPlayer != null && _textPlayer.IsTyping;

        public void Initialize(Transform canvasRoot, TMP_FontAsset font, float charactersPerSecond)
        {
            if (_root != null)
                return;

            _root = CreateRect("CinematicStrip", canvasRoot, Vector2.zero, Vector2.one);
            var blackBackground = _root.AddComponent<Image>();
            blackBackground.color = Color.black;

            var strip = CreateRect("PassengerStrip", _root.transform, new Vector2(0f, 0.23f), new Vector2(1f, 0.81f));
            _stripTransform = strip.transform;
            _stripBackground = strip.AddComponent<Image>();
            _stripBackground.color = Color.white;
            _stripBackground.raycastTarget = false;

            _rightPortrait = CreatePortrait("RightPortrait", strip.transform, new Vector2(0.34f, -0.03f), new Vector2(0.66f, 0.98f));
            _secondaryRightPortrait = CreatePortrait("SecondaryRightPortrait", strip.transform, new Vector2(0.52f, -0.08f), new Vector2(0.78f, 0.93f));
            _secondaryRightPortrait.enabled = false;

            var subtitleBar = CreateRect("SubtitleBar", _root.transform, new Vector2(0f, 0f), new Vector2(1f, 0.23f));
            var subtitleImage = subtitleBar.AddComponent<Image>();
            subtitleImage.color = new Color(0f, 0f, 0f, 0.97f);

            _speakerText = CreateText("Speaker", subtitleBar.transform, font, new Vector2(0.08f, 0.57f), new Vector2(0.80f, 0.91f));
            _speakerText.fontSize = 34f;
            _speakerText.fontStyle = FontStyles.Bold;
            _speakerText.color = new Color(0.97f, 0.79f, 0.47f);

            _bodyText = CreateText("Dialogue", subtitleBar.transform, font, new Vector2(0.08f, 0.15f), new Vector2(0.80f, 0.63f));
            _bodyText.fontSize = 31f;
            _bodyText.color = Color.white;

            var prompt = CreateText("Prompt", subtitleBar.transform, font, new Vector2(0.78f, 0.04f), new Vector2(0.97f, 0.22f));
            prompt.text = "click to continue";
            prompt.fontSize = 18f;
            prompt.fontStyle = FontStyles.Italic;
            prompt.alignment = TextAlignmentOptions.BottomRight;
            prompt.color = new Color(1f, 1f, 1f, 0.66f);

            _textPlayer = gameObject.GetComponent<IndoorDialogueTextPlayer>();
            if (_textPlayer == null)
                _textPlayer = gameObject.AddComponent<IndoorDialogueTextPlayer>();

            _textPlayer.Initialize(prompt, charactersPerSecond);
            _root.SetActive(false);
        }

        public void SetVisible(bool visible)
        {
            if (_root != null)
                _root.SetActive(visible);
        }

        public void SetStripBackground(Sprite sprite)
        {
            _stripBackground.sprite = sprite;
        }

        public void SetDecorativeItemSilhouettes(Sprite[] sprites)
        {
            if (_stripTransform == null || sprites == null)
                return;

            var anchors = new[]
            {
                (new Vector2(0.015f, 0.54f), new Vector2(0.105f, 0.92f)),
                (new Vector2(0.115f, 0.50f), new Vector2(0.205f, 0.88f)),
                (new Vector2(0.015f, 0.10f), new Vector2(0.105f, 0.48f)),
                (new Vector2(0.115f, 0.14f), new Vector2(0.205f, 0.52f)),
                (new Vector2(0.795f, 0.50f), new Vector2(0.885f, 0.88f)),
                (new Vector2(0.895f, 0.54f), new Vector2(0.985f, 0.92f)),
                (new Vector2(0.795f, 0.14f), new Vector2(0.885f, 0.52f)),
                (new Vector2(0.895f, 0.10f), new Vector2(0.985f, 0.48f)),
            };

            var count = Mathf.Min(sprites.Length, anchors.Length);
            for (var i = 0; i < count; i++)
            {
                var silhouette = CreatePortrait(
                    $"ItemSilhouette{i + 1:00}",
                    _stripTransform,
                    anchors[i].Item1,
                    anchors[i].Item2);
                silhouette.sprite = sprites[i];
                silhouette.color = new Color(0.76f, 0.70f, 0.58f, 0.28f);
                silhouette.transform.SetSiblingIndex(i);
            }
        }

        public void SetPortraits(Sprite left, Sprite right)
        {
            SetPortrait(_leftPortrait, null);
            SetPassengerPortraits(right);
        }

        public void SetPassengerPortraits(Sprite primary, Sprite secondary = null)
        {
            SetPortrait(_rightPortrait, primary);
            SetPortrait(_secondaryRightPortrait, secondary);
            SetPassengerLayout(secondary != null);
        }

        public void SetCenteredFullBodyPortrait(Sprite portrait)
        {
            SetPortrait(_rightPortrait, portrait);
            SetPortrait(_secondaryRightPortrait, null);

            var rect = (RectTransform)_rightPortrait.transform;
            rect.anchorMin = new Vector2(0.34f, -0.24f);
            rect.anchorMax = new Vector2(0.66f, 1.02f);
        }

        public void PlayLine(string speaker, string text, CinematicStripPortraitFocus focus)
        {
            _speakerText.text = speaker;
            SetPortraitAlpha(_leftPortrait, focus == CinematicStripPortraitFocus.Left || focus == CinematicStripPortraitFocus.Both);
            SetPortraitAlpha(_rightPortrait, focus == CinematicStripPortraitFocus.Right || focus == CinematicStripPortraitFocus.Both);
            SetPortraitAlpha(_secondaryRightPortrait, focus == CinematicStripPortraitFocus.SecondaryRight || focus == CinematicStripPortraitFocus.Both);
            _textPlayer.Play(_bodyText, text);
        }

        public void SkipTyping()
        {
            if (_textPlayer != null)
                _textPlayer.Skip();
        }

        private static Image CreatePortrait(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax)
        {
            var imageObject = CreateRect(name, parent, anchorMin, anchorMax);
            var image = imageObject.AddComponent<Image>();
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        private static void SetPortrait(Image image, Sprite sprite)
        {
            if (image == null)
                return;

            image.sprite = sprite;
            image.enabled = sprite != null;
        }

        private static void SetPortraitAlpha(Image image, bool active)
        {
            if (image == null)
                return;

            if (!image.enabled)
                return;

            image.color = active ? Color.white : new Color(1f, 1f, 1f, 0.34f);
        }

        private void SetPassengerLayout(bool paired)
        {
            var primaryRect = (RectTransform)_rightPortrait.transform;
            primaryRect.anchorMin = paired ? new Vector2(0.22f, -0.03f) : new Vector2(0.34f, -0.03f);
            primaryRect.anchorMax = paired ? new Vector2(0.52f, 0.98f) : new Vector2(0.66f, 0.98f);
        }

        private static GameObject CreateRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            var rect = gameObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return gameObject;
        }

        private static TextMeshProUGUI CreateText(string name, Transform parent, TMP_FontAsset font, Vector2 anchorMin, Vector2 anchorMax)
        {
            var textObject = CreateRect(name, parent, anchorMin, anchorMax);
            var text = textObject.AddComponent<TextMeshProUGUI>();
            text.font = font;
            text.enableWordWrapping = true;
            text.raycastTarget = false;
            return text;
        }
    }
}
