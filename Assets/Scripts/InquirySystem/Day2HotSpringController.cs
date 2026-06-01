using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Otowa.Inquiry
{
    /// <summary>
    /// Day 2 hot spring presentation shell. The afternoon scene and navigation
    /// are established here so the Mizuki dialogue can be filled in separately.
    /// </summary>
    public class Day2HotSpringController : MonoBehaviour
    {
        [Header("Scene")]
        [SerializeField] private string nextSceneName = "Day2World";
        [SerializeField] private float fadeDuration = 0.35f;

        [Header("Fonts")]
        [SerializeField] private TMP_FontAsset serifFont;

        private CanvasGroup _fade;
        private bool _loading;

        private void Awake()
        {
            Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
            BuildUI();
            EnsureEventSystem();
            InspirationManager.Instance.SetFont(serifFont);
        }

        private void Start()
        {
            _fade.alpha = 0f;
            StartCoroutine(FadeTo(1f));
        }

        private void Leave()
        {
            if (_loading) return;

            StartCoroutine(FadeAndLoad());
        }

        private IEnumerator FadeAndLoad()
        {
            _loading = true;
            yield return FadeTo(0f);
            Day2InquiryProgress.Instance.RequestDay2MapSpawn(
                "Day2 HotSpring Entrance",
                new Vector3(0f, -2f, 0f));
            SceneManager.LoadScene(nextSceneName);
        }

        private IEnumerator FadeTo(float target)
        {
            float start = _fade.alpha;
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                _fade.alpha = Mathf.Lerp(start, target, elapsed / fadeDuration);
                elapsed += Time.deltaTime;
                yield return null;
            }

            _fade.alpha = target;
        }

        private void BuildUI()
        {
            var canvasObject = new GameObject("Day2HotSpringCanvas",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);

            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            _fade = canvasObject.AddComponent<CanvasGroup>();

            var backgroundObject = MakeRect(canvasObject.transform, "Background", Vector2.zero, Vector2.one);
            var background = backgroundObject.AddComponent<Image>();
            background.sprite = LoadSprite("Map/spring-afternoon", new Color32(0x7c, 0x9b, 0x91, 0xFF));
            background.color = Color.white;
            background.preserveAspect = false;
            background.raycastTarget = false;

            var leaveObject = MakeRect(canvasObject.transform, "Leave",
                new Vector2(0.78f, 0.04f), new Vector2(0.96f, 0.13f));
            var leaveBackground = leaveObject.AddComponent<Image>();
            leaveBackground.color = new Color(0.10f, 0.07f, 0.05f, 0.84f);
            var leaveButton = leaveObject.AddComponent<Button>();
            leaveButton.targetGraphic = leaveBackground;
            leaveButton.onClick.AddListener(Leave);

            var leaveText = MakeText(leaveObject.transform, "Label", "Leave",
                30f, new Color(0.96f, 0.88f, 0.72f, 1f),
                TextAlignmentOptions.Center, Vector2.zero, Vector2.one);
            leaveText.fontStyle = FontStyles.Bold;
        }

        private static Sprite LoadSprite(string resourcePath, Color fallbackColor)
        {
            var sprite = Resources.Load<Sprite>(resourcePath);
            if (sprite != null) return sprite;

            var texture = Resources.Load<Texture2D>(resourcePath);
            if (texture == null)
            {
                texture = new Texture2D(1, 1);
                texture.SetPixel(0, 0, fallbackColor);
                texture.Apply();
            }

            return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f), 100f);
        }

        private TMP_Text MakeText(Transform parent, string name, string value, float size,
                                  Color color, TextAlignmentOptions alignment,
                                  Vector2 anchorMin, Vector2 anchorMax)
        {
            var textObject = MakeRect(parent, name, anchorMin, anchorMax);
            var text = textObject.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = size;
            text.color = color;
            text.alignment = alignment;
            text.raycastTarget = false;
            if (serifFont != null)
                text.font = serifFont;
            return text;
        }

        private static GameObject MakeRect(Transform parent, string name,
                                           Vector2 anchorMin, Vector2 anchorMax)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            var rect = (RectTransform)gameObject.transform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return gameObject;
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;

            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }
    }
}
