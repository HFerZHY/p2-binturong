using System.Collections;
using TMPro;
using Otowa.IndoorDialogue;
using Otowa.UI;
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
        private const float SUBTITLE_TOP = 0.23f;
        private const float SUBTITLE_SEAM_OVERLAP = 0.006f;
        private const float SUBTITLE_PORTRAIT_COVER = 0.065f;
        private const float PRIMARY_PORTRAIT_Y_MIN = 0f;
        private const float PRIMARY_PORTRAIT_Y_MAX = 1.01f;
        private const float SECONDARY_PORTRAIT_Y_MIN = -0.05f;
        private const float SECONDARY_PORTRAIT_Y_MAX = 0.96f;
        private const float CENTERED_PORTRAIT_Y_MIN = -0.19f;
        private const float CENTERED_PORTRAIT_Y_MAX = 1.07f;
        private static readonly Color BodyFg = new Color32(0xc8, 0xd4, 0xc8, 0xff);
        private static readonly Color RinFg = new Color32(0x8f, 0xbc, 0x8f, 0xff);
        private static readonly Color JunkoFg = new Color32(0xd4, 0xa0, 0x60, 0xff);
        private static readonly Color YujiFg = new Color32(0x80, 0xb8, 0xe8, 0xff);
        private static readonly Color JiroFg = new Color32(0x98, 0x98, 0xa8, 0xff);
        private static readonly Color InspectorFg = new Color32(0xa0, 0xa8, 0xc0, 0xff);
        private static readonly Color HikaruFg = new Color32(0xcf, 0x9a, 0x5d, 0xff);
        private static readonly Color HachiFg = new Color32(0xcf, 0x76, 0x68, 0xff);
        private static readonly Color MisakiFg = new Color32(0xc8, 0x70, 0x73, 0xff);
        private static readonly Color PassengerFg = new Color32(0xd0, 0xb0, 0x82, 0xff);
        private static readonly Color UnknownFg = new Color32(0xd2, 0xc5, 0xa8, 0xff);

        private GameObject _root;
        private Transform _stripTransform;
        private Image _stripBackground;
        private Image _leftPortrait;
        private Image _rightPortrait;
        private Image _secondaryRightPortrait;
        private TextMeshProUGUI _speakerText;
        private TextMeshProUGUI _bodyText;
        private IndoorDialogueTextPlayer _textPlayer;
        private Coroutine _rightPortraitFade;
        private Color _speakerColorOverride;
        private bool _hasSpeakerColorOverride;

        public bool IsTyping => _textPlayer != null && _textPlayer.IsTyping;

        public void Initialize(Transform canvasRoot, TMP_FontAsset font, float charactersPerSecond)
        {
            if (_root != null)
                return;

            font = RuntimeFontLibrary.BreeSerifRegularOr(font);

            _root = CreateRect("CinematicStrip", canvasRoot, Vector2.zero, Vector2.one);
            var blackBackground = _root.AddComponent<Image>();
            blackBackground.color = Color.black;

            var strip = CreateRect("PassengerStrip", _root.transform, new Vector2(0f, SUBTITLE_TOP), new Vector2(1f, 0.81f));
            _stripTransform = strip.transform;
            _stripBackground = strip.AddComponent<Image>();
            _stripBackground.color = Color.white;
            _stripBackground.raycastTarget = false;

            _rightPortrait = CreatePortrait("RightPortrait", strip.transform, new Vector2(0.34f, PRIMARY_PORTRAIT_Y_MIN), new Vector2(0.66f, PRIMARY_PORTRAIT_Y_MAX));
            _secondaryRightPortrait = CreatePortrait("SecondaryRightPortrait", strip.transform, new Vector2(0.52f, SECONDARY_PORTRAIT_Y_MIN), new Vector2(0.78f, SECONDARY_PORTRAIT_Y_MAX));
            _secondaryRightPortrait.enabled = false;

            var subtitleCover = CreateRect("SubtitlePortraitCover", _root.transform, new Vector2(0f, SUBTITLE_TOP), new Vector2(1f, SUBTITLE_TOP + SUBTITLE_PORTRAIT_COVER));
            var subtitleCoverImage = subtitleCover.AddComponent<Image>();
            subtitleCoverImage.color = Color.black;
            subtitleCoverImage.raycastTarget = false;

            var subtitleBar = CreateRect("SubtitleBar", _root.transform, new Vector2(0f, 0f), new Vector2(1f, SUBTITLE_TOP + SUBTITLE_SEAM_OVERLAP));
            var subtitleImage = subtitleBar.AddComponent<Image>();
            subtitleImage.color = Color.black;

            _speakerText = CreateText("Speaker", subtitleBar.transform, font, new Vector2(0.08f, 0.57f), new Vector2(0.80f, 0.91f));
            _speakerText.fontSize = 34f;
            _speakerText.fontStyle = FontStyles.Bold;
            _speakerText.color = PassengerFg;

            _bodyText = CreateText("Dialogue", subtitleBar.transform, font, new Vector2(0.08f, 0.15f), new Vector2(0.80f, 0.63f));
            _bodyText.fontSize = 31f;
            _bodyText.color = Color.white;

            var prompt = CreateText("Prompt", subtitleBar.transform, font, new Vector2(0.78f, 0.04f), new Vector2(0.97f, 0.22f));
            prompt.text = string.Empty;
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

        public void SetSpeakerColorOverride(Color color)
        {
            _speakerColorOverride = color;
            _hasSpeakerColorOverride = true;
        }

        public void SetCenteredFullBodyPortrait(Sprite portrait)
        {
            SetPortrait(_rightPortrait, portrait);
            SetPortrait(_secondaryRightPortrait, null);

            var rect = (RectTransform)_rightPortrait.transform;
            rect.anchorMin = new Vector2(0.34f, CENTERED_PORTRAIT_Y_MIN);
            rect.anchorMax = new Vector2(0.66f, CENTERED_PORTRAIT_Y_MAX);
        }

        public void PlayLine(string speaker, string text, CinematicStripPortraitFocus focus, bool fadeInFocusedPortrait = false)
        {
            _speakerText.text = speaker;
            _speakerText.color = ResolveSpeakerColor(speaker);
            _hasSpeakerColorOverride = false;
            SetPortraitAlpha(_leftPortrait, focus == CinematicStripPortraitFocus.Left || focus == CinematicStripPortraitFocus.Both);
            SetPortraitAlpha(
                _rightPortrait,
                focus == CinematicStripPortraitFocus.Right || focus == CinematicStripPortraitFocus.Both,
                fadeInFocusedPortrait);
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

        private void SetPortraitAlpha(Image image, bool active, bool fadeIn = false)
        {
            if (image == null)
                return;

            if (!image.enabled)
                return;

            var target = active ? Color.white : new Color(1f, 1f, 1f, 0.34f);
            if (fadeIn && active && image == _rightPortrait)
            {
                if (_rightPortraitFade != null)
                    StopCoroutine(_rightPortraitFade);

                var start = target;
                start.a = 0f;
                image.color = start;
                _rightPortraitFade = StartCoroutine(FadeRightPortraitTo(target, 0.55f));
                return;
            }

            if (image == _rightPortrait && _rightPortraitFade != null)
            {
                StopCoroutine(_rightPortraitFade);
                _rightPortraitFade = null;
            }

            image.color = target;
        }

        private IEnumerator FadeRightPortraitTo(Color target, float duration)
        {
            var start = _rightPortrait.color;
            var elapsed = 0f;
            while (elapsed < duration && _rightPortrait != null)
            {
                elapsed += Time.deltaTime;
                _rightPortrait.color = Color.Lerp(start, target, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }

            if (_rightPortrait != null)
                _rightPortrait.color = target;
            _rightPortraitFade = null;
        }

        private Color ResolveSpeakerColor(string speaker)
        {
            if (_hasSpeakerColorOverride)
                return _speakerColorOverride;

            if (string.IsNullOrWhiteSpace(speaker))
                return BodyFg;

            return speaker.Trim().ToLowerInvariant() switch
            {
                "rin" => RinFg,
                "junko" => JunkoFg,
                "yuji" => YujiFg,
                "jiro" => JiroFg,
                "inspector" => InspectorFg,
                "hikaru" => HikaruFg,
                "hachi" => HachiFg,
                "misaki" => MisakiFg,
                "passenger" => PassengerFg,
                "???" => UnknownFg,
                _ => BodyFg,
            };
        }

        private void SetPassengerLayout(bool paired)
        {
            var primaryRect = (RectTransform)_rightPortrait.transform;
            primaryRect.anchorMin = paired ? new Vector2(0.22f, PRIMARY_PORTRAIT_Y_MIN) : new Vector2(0.34f, PRIMARY_PORTRAIT_Y_MIN);
            primaryRect.anchorMax = paired ? new Vector2(0.52f, PRIMARY_PORTRAIT_Y_MAX) : new Vector2(0.66f, PRIMARY_PORTRAIT_Y_MAX);

            var secondaryRect = (RectTransform)_secondaryRightPortrait.transform;
            secondaryRect.anchorMin = new Vector2(0.52f, SECONDARY_PORTRAIT_Y_MIN);
            secondaryRect.anchorMax = new Vector2(0.78f, SECONDARY_PORTRAIT_Y_MAX);
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
