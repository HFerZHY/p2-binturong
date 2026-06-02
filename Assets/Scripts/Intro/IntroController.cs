using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using TMPro;
using Otowa.IndoorDialogue;

namespace Otowa.Intro
{
    /// <summary>
    /// Self-contained intro sequence controller.
    ///
    /// Setup: Create an empty scene, add an empty GameObject, attach this script.
    /// All UI is built at runtime — no prefabs required.
    ///
    /// Optional: assign Cormorant Garamond (serifFont) and Caveat (handwrittenFont)
    /// TMP font assets in the Inspector to match the webapp fonts exactly.
    /// </summary>
    public class IntroController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Fonts  (assign TMP Font Assets in Inspector)")]
        [SerializeField] private TMP_FontAsset serifFont;        // Cormorant Garamond
        [SerializeField] private TMP_FontAsset handwrittenFont;  // Caveat

        [Header("Settings")]
        [SerializeField] private string nextSceneName = "SampleScene";
        [SerializeField] private float  typewriterSpeed = 40f;   // visible chars/sec
        [SerializeField] private float  fadeDuration    = 0.35f;

        [Header("Ambient Music")]
        [SerializeField] private AudioClip ambientClip;
        [SerializeField] [Range(0f, 1f)] private float musicVolume = 0.35f;

        [Header("Bubbles")]
        [SerializeField] private bool  bubblesEnabled  = true;
        [SerializeField] private float bubbleSpawnRate = 0.55f;
        [SerializeField] private float bubbleMinSize   = 8f;
        [SerializeField] private float bubbleMaxSize   = 24f;
        [SerializeField] private float bubbleMinSpeed  = 70f;
        [SerializeField] private float bubbleMaxSpeed  = 160f;
        [SerializeField] private float bubbleDrift     = 28f;
        [SerializeField] private Color bubbleColor     = new Color(1f, 1f, 1f, 0.16f);

        // ── Colors (editable in Inspector) ───────────────────────────────────────

        [Header("Colors")]
        [SerializeField] private Color bgGreen    = new Color32(0x05, 0x18, 0x20, 0xFF);
        [SerializeField] private Color bgBrown    = new Color32(0x2a, 0x22, 0x18, 0xFF);
        [SerializeField] private Color textPrimary = new Color32(0xc8, 0xdc, 0xda, 0xFF);
        [SerializeField] private Color titleGreen  = new Color32(0x78, 0xb5, 0xb2, 0xFF);
        [SerializeField] private Color subtleGreen = new Color32(0x4e, 0x7f, 0x82, 0xFF);
        [SerializeField] private Color parchment   = new Color32(0xc4, 0xb8, 0xa0, 0xFF);
        [SerializeField] private Color inkDark     = new Color32(0x3a, 0x35, 0x2e, 0xFF);
        [SerializeField] private Color promptColor = new Color32(0xFF, 0xFF, 0xFF, 0xFF);

        // ── Font Sizes (editable in Inspector) ───────────────────────────────────

        [Header("Font Sizes")]
        [SerializeField] private float titleFontSize    = 110f;
        [SerializeField] private float subtitleFontSize = 22f;
        [SerializeField] private float bodyFontSize     = 36f;
        [SerializeField] private float letterFontSize   = 32f;
        [SerializeField] private float itemsTitleSize   = 34f;
        [SerializeField] private float itemLabelSize    = 24f;
        [SerializeField] private float promptFontSize   = 22f;

        // ── Screen data ───────────────────────────────────────────────────────

        private enum ScreenType { Opening, Letter, Items }

        private sealed class IntroScreen
        {
            public ScreenType Type;
            public string     Title;     // null → no title block shown
            public string     Subtitle;
            public string     Body;      // TMP rich text; null → skip typewriter
            public bool       IsLast;    // changes prompt text to "Click to begin"
            public Color      Background;
            public bool       HideBubbles;
            public bool       WaitForBackground;
        }

        // ── Runtime state ─────────────────────────────────────────────────────

        private List<IntroScreen> _screens;
        private int      _current   = -1;
        private bool     _inputLock = false;
        private IndoorDialogueTextPlayer _textPlayer;
        private Coroutine _glowCR;
        private Coroutine _backgroundTransition;

        // Static guard — prevents two instances from both calling LoadScene
        private static bool _loadingScene = false;

        private static readonly Color CityGray = new Color32(0x30, 0x32, 0x34, 0xFF);
        private static readonly Color SeasideBlue = new Color32(0x2f, 0x78, 0x98, 0xFF);
        private static readonly Color MountainGreen = new Color32(0x0b, 0x32, 0x2f, 0xFF);
        private static readonly Color Black = Color.black;

        // Screen index on which "Otowa" should glow.
        private const int OtowaScreenIndex = 12;

        // ── UI references (built at runtime) ──────────────────────────────────

        private Image      _bg;
        private CanvasGroup _fade;

        private GameObject _openingPanel;
        private TMP_Text   _titleTmp;
        private TMP_Text   _subtitleTmp;
        private TMP_Text   _bodyTmp;

        private GameObject _letterPanel;
        private TMP_Text   _letterTmp;

        private GameObject _itemsPanel;
        private TMP_Text   _itemsTitleTmp;

        private TMP_Text   _promptTmp;

        // ── Bubble state ──────────────────────────────────────────────────────
        private RectTransform _bubbleLayer;
        private Sprite        _bubbleSprite;
        private Coroutine     _bubbleSpawner;

        // ── Audio ─────────────────────────────────────────────────────────────
        private AudioSource _audioSource;

        // ─────────────────────────────────────────────────────────────────────
        //  Unity lifecycle
        // ─────────────────────────────────────────────────────────────────────

        private void Awake()
        {
            _loadingScene = false; // reset each time this scene starts fresh
            Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
            ApplyBlueTheme();
            BuildScreens();
            BuildUI();
            _textPlayer = gameObject.AddComponent<IndoorDialogueTextPlayer>();
            _textPlayer.Initialize(_promptTmp, typewriterSpeed);
            SetupAudio();
        }

        private void Start()
        {
            _fade.alpha = 0f;
            ShowScreen(0);
            StartCoroutine(FadeTo(1f));
        }

        private void Update()
        {
            if (_inputLock) return;

            var mouse    = Mouse.current;
            var keyboard = Keyboard.current;

            bool clicked = (mouse    != null && mouse.leftButton.wasPressedThisFrame)
                        || (keyboard != null && keyboard.spaceKey.wasPressedThisFrame)
                        || (keyboard != null && keyboard.enterKey.wasPressedThisFrame);

            if (clicked) HandleAdvance();
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Input handling
        // ─────────────────────────────────────────────────────────────────────

        private void HandleAdvance()
        {
            if (_textPlayer.IsTyping)
            {
                _textPlayer.Skip();
            }
            else
            {
                int next = _current + 1;
                if (next >= _screens.Count)
                    StartCoroutine(FadeAndLoad());
                else
                    ShowScreen(next);
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Screen display
        // ─────────────────────────────────────────────────────────────────────

        private void ShowScreen(int index)
        {
            if (_glowCR != null) { StopCoroutine(_glowCR); _glowCR = null; }
            _textPlayer?.Cancel();
            ClearText(_bodyTmp);
            ClearText(_letterTmp);

            _current = index;
            var s = _screens[index];

            Color targetBackground = s.Type == ScreenType.Letter ? bgBrown : s.Background;
            if (_backgroundTransition != null)
                StopCoroutine(_backgroundTransition);
            _openingPanel.SetActive(s.Type == ScreenType.Opening);
            _letterPanel.SetActive(s.Type == ScreenType.Letter);
            _itemsPanel.SetActive(s.Type == ScreenType.Items);

            if (bubblesEnabled)
            {
                if (s.Type == ScreenType.Opening && !s.HideBubbles && _bubbleSpawner == null)
                    _bubbleSpawner = StartCoroutine(BubbleSpawnerCR());
                else if ((s.Type != ScreenType.Opening || s.HideBubbles) && _bubbleSpawner != null)
                    StopBubbles();
            }

            SetPrompt(false);
            _promptTmp.text = s.IsLast ? "Click to begin" : "Click to continue  >";

            if (s.WaitForBackground)
            {
                _inputLock = true;
                _backgroundTransition = StartCoroutine(TransitionBackgroundAndShow(targetBackground, s));
                return;
            }

            _backgroundTransition = StartCoroutine(TransitionBackground(targetBackground));
            ShowScreenContents(s);
        }

        private void ShowScreenContents(IntroScreen s)
        {
            switch (s.Type)
            {
                case ScreenType.Opening: ShowOpening(s); break;
                case ScreenType.Letter:  ShowLetter(s);  break;
                case ScreenType.Items:   ShowItems(s);   break;
            }
        }

        private void ShowOpening(IntroScreen s)
        {
            bool hasTitle = !string.IsNullOrEmpty(s.Title);
            _titleTmp.gameObject.SetActive(hasTitle);
            _subtitleTmp.gameObject.SetActive(hasTitle && !string.IsNullOrEmpty(s.Subtitle));

            if (hasTitle)
            {
                _titleTmp.text    = s.Title ?? "";
                _subtitleTmp.text = s.Subtitle ?? "";
            }

            bool hasBody = !string.IsNullOrEmpty(s.Body);
            _bodyTmp.gameObject.SetActive(hasBody);

            if (hasBody)
                BeginTypewriter(_bodyTmp, s.Body);
            else
                SetPrompt(true);
        }

        private void ShowLetter(IntroScreen s)
        {
            _letterTmp.text = s.Body ?? "";
            BeginTypewriter(_letterTmp, s.Body);
        }

        private void ShowItems(IntroScreen s)
        {
            _itemsTitleTmp.text = s.Body ?? "";
            _textPlayer.Cancel();
            SetPrompt(true);
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Typewriter
        // ─────────────────────────────────────────────────────────────────────

        private void BeginTypewriter(TMP_Text tmp, string text)
        {
            _textPlayer.Play(tmp, text, HandleTextCompleted);
        }

        private void HandleTextCompleted()
        {
            if (_current == OtowaScreenIndex && _glowCR == null)
                _glowCR = StartCoroutine(GlowWordCR(_bodyTmp, "Otowa"));
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Transitions
        // ─────────────────────────────────────────────────────────────────────

        private IEnumerator CrossFade(int next)
        {
            _inputLock = true;
            yield return StartCoroutine(FadeTo(0f));
            ShowScreen(next);
            yield return StartCoroutine(FadeTo(1f));
            _inputLock = false;
        }

        private IEnumerator FadeAndLoad()
        {
            if (_loadingScene) yield break;
            _loadingScene = true;
            _inputLock = true;
            _textPlayer.Cancel();
            SetPrompt(false);
            ClearText(_titleTmp);
            ClearText(_subtitleTmp);
            ClearText(_bodyTmp);
            _openingPanel.SetActive(false);
            yield return null;
            yield return new WaitForSeconds(1.5f);
            yield return StartCoroutine(FadeTo(0f));
            SceneManager.LoadScene(nextSceneName);
        }

        private IEnumerator TransitionBackgroundAndShow(Color target, IntroScreen screen)
        {
            yield return TransitionBackground(target);
            ShowScreenContents(screen);
            _inputLock = false;
        }

        private IEnumerator TransitionBackground(Color target)
        {
            const float duration = 0.65f;
            Color start = _bg.color;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                _bg.color = Color.Lerp(start, target, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }

            _bg.color = target;
            _backgroundTransition = null;
        }

        private IEnumerator FadeTo(float target)
        {
            float start   = _fade.alpha;
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                _fade.alpha = Mathf.Lerp(start, target, elapsed / fadeDuration);
                elapsed += Time.deltaTime;
                yield return null;
            }
            _fade.alpha = target;
        }

        private void SetPrompt(bool show) => _textPlayer.SetPromptVisible(show);

        private static void ClearText(TMP_Text text)
        {
            if (text == null)
                return;

            text.text = string.Empty;
            text.maxVisibleCharacters = 0;
        }

        private void ApplyBlueTheme()
        {
            bgGreen = new Color32(0x05, 0x18, 0x20, 0xFF);
            textPrimary = new Color32(0xc8, 0xdc, 0xda, 0xFF);
            titleGreen = new Color32(0x78, 0xb5, 0xb2, 0xFF);
            subtleGreen = new Color32(0x4e, 0x7f, 0x82, 0xFF);
        }

        // ─────────────────────────────────────────────────────────────────────
        //  UI construction  (all UI created procedurally — no prefabs needed)
        // ─────────────────────────────────────────────────────────────────────

        private void BuildUI()
        {
            // Root canvas
            var canvasGo = new GameObject("IntroCanvas",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);

            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode        = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode     = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight  = 0.5f;

            _fade = canvasGo.AddComponent<CanvasGroup>();

            // Full-screen background
            _bg = MakeImage(canvasGo.transform, "BG", CityGray, Vector2.zero, Vector2.one);

            BuildOpeningPanel(canvasGo.transform);
            BuildLetterPanel(canvasGo.transform);
            BuildItemsPanel(canvasGo.transform);

            // Lightweight ambience behind the opening narration.
            _bubbleSprite = CreateCircleSprite(32);
            var bubbleGo  = new GameObject("BubbleLayer", typeof(RectTransform));
            bubbleGo.transform.SetParent(canvasGo.transform, false);
            _bubbleLayer = (RectTransform)bubbleGo.transform;
            _bubbleLayer.anchorMin = Vector2.zero;
            _bubbleLayer.anchorMax = new Vector2(1f, 0.42f);
            _bubbleLayer.offsetMin = _bubbleLayer.offsetMax = Vector2.zero;
            bubbleGo.AddComponent<RectMask2D>();
            bubbleGo.transform.SetSiblingIndex(1);

            // Continue prompt  — sits at bottom, always above panels
            _promptTmp = MakeTMP(canvasGo.transform, "Prompt",
                                 "Click to continue  >",
                                 promptFontSize, promptColor, TextAlignmentOptions.Center,
                                 new Vector2(0.2f, 0.02f), new Vector2(0.8f, 0.08f));
            _promptTmp.characterSpacing = 4f;
            UseFont(_promptTmp, serifFont);
            _promptTmp.gameObject.SetActive(false);
        }

        // ── Opening panel  (dark green bg, big title or story text) ──────────

        private void BuildOpeningPanel(Transform canvas)
        {
            _openingPanel = MakeRect(canvas, "OpeningPanel", Vector2.zero, Vector2.one);
            var p = _openingPanel.transform;

            _titleTmp = MakeTMP(p, "Title", "",
                                titleFontSize, titleGreen, TextAlignmentOptions.Center,
                                new Vector2(0.1f, 0.48f), new Vector2(0.9f, 0.80f));
            _titleTmp.characterSpacing = 20f;
            UseFont(_titleTmp, serifFont);

            _subtitleTmp = MakeTMP(p, "Subtitle", "",
                                   subtitleFontSize, subtleGreen, TextAlignmentOptions.Center,
                                   new Vector2(0.1f, 0.40f), new Vector2(0.9f, 0.50f));
            _subtitleTmp.characterSpacing = 10f;
            UseFont(_subtitleTmp, serifFont);

            _bodyTmp = MakeTMP(p, "Body", "",
                               bodyFontSize, textPrimary, TextAlignmentOptions.Center,
                               new Vector2(0.16f, 0.20f), new Vector2(0.84f, 0.80f));
            _bodyTmp.lineSpacing = 14f;
            UseFont(_bodyTmp, serifFont);
        }

        // ── Letter panel  (dark brown bg + parchment card + handwritten text) ─

        private void BuildLetterPanel(Transform canvas)
        {
            _letterPanel = MakeRect(canvas, "LetterPanel", Vector2.zero, Vector2.one);
            var p = _letterPanel.transform;

            // Drop shadow (created first → renders behind card)
            MakeImage(p, "CardShadow",
                      new Color32(0, 0, 0, 90),
                      new Vector2(0.185f, 0.075f), new Vector2(0.825f, 0.918f));

            // Parchment card
            var card = MakeImage(p, "LetterCard", parchment,
                                 new Vector2(0.18f, 0.08f), new Vector2(0.82f, 0.92f));

            // Handwritten text inside card
            _letterTmp = MakeTMP(card.transform, "LetterText", "",
                                 letterFontSize, inkDark, TextAlignmentOptions.Left,
                                 new Vector2(0.08f, 0.06f), new Vector2(0.92f, 0.94f));
            _letterTmp.lineSpacing = 10f;
            UseFont(_letterTmp, handwrittenFont);
        }

        // ── Items panel  (dark brown bg, two item images with glow circles) ───

        private void BuildItemsPanel(Transform canvas)
        {
            _itemsPanel = MakeRect(canvas, "ItemsPanel", Vector2.zero, Vector2.one);
            var p = _itemsPanel.transform;

            _itemsTitleTmp = MakeTMP(p, "ItemsTitle", "",
                                     itemsTitleSize, titleGreen, TextAlignmentOptions.Center,
                                     new Vector2(0.1f, 0.58f), new Vector2(0.9f, 0.88f));
            _itemsTitleTmp.lineSpacing = 10f;
            UseFont(_itemsTitleTmp, serifFont);

            BuildItemCard(p,
                          "Items/OtowaItems/bi", "Binoculars",
                          new Vector2(0.24f, 0.18f), new Vector2(0.46f, 0.58f),
                          new Vector2(0.24f, 0.10f), new Vector2(0.46f, 0.20f));

            BuildItemCard(p,
                          "Items/OtowaItems/stone", "Onsen Stone",
                          new Vector2(0.54f, 0.18f), new Vector2(0.76f, 0.58f),
                          new Vector2(0.54f, 0.10f), new Vector2(0.76f, 0.20f));
        }

        private void BuildItemCard(Transform parent,
                                   string resourcePath, string labelText,
                                   Vector2 imgMin, Vector2 imgMax,
                                   Vector2 lblMin, Vector2 lblMax)
        {
            // Soft glow circle behind item
            var glow = MakeImage(parent, $"Glow_{labelText}",
                                 new Color32(0x28, 0x68, 0x72, 0x55),
                                 imgMin, imgMax);
            glow.raycastTarget = false;

            // Item image
            var imgGo = MakeRect(parent, $"Img_{labelText}", imgMin, imgMax);
            var img   = imgGo.AddComponent<Image>();
            img.sprite = Resources.Load<Sprite>(resourcePath);
            img.preserveAspect = true;
            img.raycastTarget  = false;

            // Label beneath item
            var lbl = MakeTMP(parent, $"Label_{labelText}", labelText,
                              itemLabelSize, titleGreen, TextAlignmentOptions.Center,
                              lblMin, lblMax);
            UseFont(lbl, serifFont);
        }

        // ─────────────────────────────────────────────────────────────────────
        //  UI factory helpers
        // ─────────────────────────────────────────────────────────────────────

        private Image MakeImage(Transform parent, string name, Color color,
                                Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = color;
            var rt = (RectTransform)go.transform;
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return img;
        }

        private GameObject MakeRect(Transform parent, string name,
                                    Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return go;
        }

        private TMP_Text MakeTMP(Transform parent, string name, string text,
                                 float size, Color color, TextAlignmentOptions align,
                                 Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text      = text;
            tmp.fontSize  = size;
            tmp.color     = color;
            tmp.alignment = align;
            tmp.richText  = true;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            var rt = (RectTransform)go.transform;
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return tmp;
        }

        private void UseFont(TMP_Text tmp, TMP_FontAsset font)
        {
            if (font != null) tmp.font = font;
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Bubbles
        // ─────────────────────────────────────────────────────────────────────

        // ─────────────────────────────────────────────────────────────────────
        //  Word glow animation  (TMP vertex color)
        // ─────────────────────────────────────────────────────────────────────

        private IEnumerator GlowWordCR(TMP_Text tmp, string word)
        {
            // Wait one frame so the mesh is fully settled after the typewriter
            yield return null;
            tmp.ForceMeshUpdate();

            // Find the word's character indices in the parsed (visible) text
            var charInfo = tmp.textInfo.characterInfo;
            int charCount = tmp.textInfo.characterCount;
            int wordStart = -1;

            for (int i = 0; i <= charCount - word.Length; i++)
            {
                bool match = true;
                for (int j = 0; j < word.Length; j++)
                {
                    if (char.ToLower(charInfo[i + j].character) != char.ToLower(word[j]))
                    { match = false; break; }
                }
                if (match) { wordStart = i; break; }
            }

            if (wordStart < 0) yield break;

            // Base colour (#8fbc8f) and a subtly brighter highlight
            Color baseCol  = titleGreen;
            Color glowCol  = new Color(0.78f, 0.96f, 0.78f);  // ~#c7f5c7, soft lime

            while (true)
            {
                tmp.ForceMeshUpdate();
                var meshInfo  = tmp.textInfo.meshInfo;
                charInfo      = tmp.textInfo.characterInfo;

                for (int i = wordStart; i < wordStart + word.Length; i++)
                {
                    if (!charInfo[i].isVisible) continue;
                    // Stagger each letter's phase so they shimmer in a wave
                    float phase = Time.time * 2.5f + (i - wordStart) * 0.7f;
                    float t = (Mathf.Sin(phase) + 1f) * 0.5f;
                    Color32 c32 = Color.Lerp(baseCol, glowCol, t * 0.45f);
                    int mat  = charInfo[i].materialReferenceIndex;
                    int vert = charInfo[i].vertexIndex;
                    var cols = meshInfo[mat].colors32;
                    cols[vert]     = c32;
                    cols[vert + 1] = c32;
                    cols[vert + 2] = c32;
                    cols[vert + 3] = c32;
                }

                tmp.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
                yield return null;
            }
        }

        private void StopBubbles()
        {
            if (_bubbleSpawner != null) { StopCoroutine(_bubbleSpawner); _bubbleSpawner = null; }
            if (_bubbleLayer == null) return;
            foreach (Transform child in _bubbleLayer) Destroy(child.gameObject);
        }

        private IEnumerator BubbleSpawnerCR()
        {
            // Seed a few bubbles at staggered starting heights so it doesn't look empty at first
            for (int i = 0; i < 5; i++) SpawnBubble(startMidway: true);

            while (true)
            {
                yield return new WaitForSeconds(bubbleSpawnRate);
                SpawnBubble(startMidway: false);
            }
        }

        private void SpawnBubble(bool startMidway)
        {
            float size = Random.Range(bubbleMinSize, bubbleMaxSize);

            var go = new GameObject("Bubble", typeof(RectTransform));
            go.transform.SetParent(_bubbleLayer, false);

            var img    = go.AddComponent<Image>();
            img.sprite = _bubbleSprite;
            img.color  = bubbleColor;
            img.raycastTarget = false;

            var rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(size, size);

            float layerH  = _bubbleLayer.rect.height > 0 ? _bubbleLayer.rect.height : 400f;
            float layerW  = _bubbleLayer.rect.width  > 0 ? _bubbleLayer.rect.width  : 1920f;
            float startY  = startMidway ? Random.Range(0f, layerH * 0.7f) : 0f;
            float startX  = Random.Range(size * 0.5f, layerW - size * 0.5f);

            rt.anchorMin = rt.anchorMax = Vector2.zero;
            rt.pivot     = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(startX, startY);

            StartCoroutine(AnimateBubbleCR(rt, img, layerH, startY));
        }

        private IEnumerator AnimateBubbleCR(RectTransform rt, Image img, float layerH, float startY)
        {
            float speed     = Random.Range(bubbleMinSpeed, bubbleMaxSpeed);
            float driftFreq = Random.Range(0.4f, 0.9f);
            float driftPhase= Random.Range(0f, Mathf.PI * 2f);
            float originX   = rt.anchoredPosition.x;
            float y         = startY;
            float elapsed   = 0f;
            float totalTime = (layerH - startY) / speed;

            Color c = img.color;

            while (y < layerH)
            {
                elapsed += Time.deltaTime;
                y       += speed * Time.deltaTime;
                float x  = originX + Mathf.Sin(elapsed * driftFreq * Mathf.PI * 2f + driftPhase) * bubbleDrift;

                // Fade out in the top 30%
                float fadeStart = layerH * 0.7f;
                float alpha     = c.a;
                if (y > fadeStart) alpha = Mathf.Lerp(c.a, 0f, (y - fadeStart) / (layerH - fadeStart));
                img.color = new Color(c.r, c.g, c.b, alpha);

                rt.anchoredPosition = new Vector2(x, y);
                yield return null;
            }

            Destroy(rt.gameObject);
        }

        private static Sprite CreateCircleSprite(int radius)
        {
            int size   = radius * 2;
            var tex    = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color32[size * size];
            float cx   = radius - 0.5f;
            float cy   = radius - 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                    // Anti-alias the edge over 1 pixel
                    byte a = (byte)(Mathf.Clamp01(radius - dist) * 255);
                    pixels[y * size + x] = new Color32(255, 255, 255, a);
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();
            return Sprite.Create(tex,
                                 new Rect(0, 0, size, size),
                                 new Vector2(0.5f, 0.5f),
                                 size);
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Audio
        // ─────────────────────────────────────────────────────────────────────

        private void SetupAudio()
        {
            _audioSource        = gameObject.AddComponent<AudioSource>();
            _audioSource.clip   = ambientClip;
            _audioSource.volume = musicVolume;
            _audioSource.loop   = true;
            _audioSource.playOnAwake = false;
            if (ambientClip != null) _audioSource.Play();
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Screen definitions  (mirrors webapp pages 1–8 + items screen)
        // ─────────────────────────────────────────────────────────────────────

        private void BuildScreens()
        {
            // Rich-text colour helpers (match webapp CSS exactly)
            string H(string t)  => $"<color=#78b5b2>{t}</color>";          // highlight
            string D(string t)  => $"<i><color=#9fc9c6>{t}</color></i>";   // dialogue
            string SX(string t) => $"<color=#d4a8a8>{t}</color>";          // surprise
            string SB(string t) => $"<color=#346872>{t}</color>";          // scene break
            string E(string t)  => $"<b><color=#4d7780>{t}</color></b>";   // emphasis
            string W(string t)  => $"<b><color=#6a3a3a>{t}</color></b>";   // warning/red

            _screens = new List<IntroScreen>
            {
                // ── Screen 1: Leaving the city ───────────────────────────────
                new() {
                    Type = ScreenType.Opening,
                    Body = "The train slowly pulls out of the station. You leave the city for the final time.",
                    Background = CityGray,
                },
                new() {
                    Type = ScreenType.Opening,
                    Body = "You really hate this city; you are tired of the gray high-rises and passengers with exhausted faces.",
                    Background = CityGray,
                },
                new() {
                    Type = ScreenType.Opening,
                    Body = $"Your name is {H("Rin")}. A few days ago, you handed in your resignation to your boss. It's hard to explain exactly why; you only know that it has become increasingly difficult to breathe in the city.",
                    Background = CityGray,
                },

                // ── Screen 2: Hikaru's job posting ───────────────────────────
                new() {
                    Type = ScreenType.Opening,
                    Body = $"You wanted to find a quiet place to completely empty your mind. Then, you saw {H("Hikaru's")} job posting.",
                    Background = CityGray,
                },
                new() {
                    Type = ScreenType.Opening,
                    Body = $"{H("Position available: Station Attendant.")} In a faraway little mountain village.",
                    Background = CityGray,
                },
                new() {
                    Type = ScreenType.Opening,
                    Body = "You were drawn in by the attached photos. You wanted to go to that place full of greenery.",
                    Background = CityGray,
                },

                // ── Screen 3: The phone interview ────────────────────────────
                new() {
                    Type = ScreenType.Opening,
                    Body = $"During the phone interview, Hikaru told you that this job might be a bit {H("\"challenging.\"")}",
                    Background = CityGray,
                },
                new() {
                    Type = ScreenType.Opening,
                    Body = "You said it didn't matter. As long as you could escape the city, you weren't afraid of any challenges.",
                    Background = CityGray,
                },
                new() {
                    Type = ScreenType.Opening,
                    Body = "The train begins to accelerate, gradually leaving the high-rises behind.",
                    Background = SeasideBlue,
                },

                // ── Screen 4: Arriving at Otowa ──────────────────────────────
                new() {
                    Type = ScreenType.Opening,
                    Body = "Your field of vision suddenly opens up: to the left are summer fields and mountains, to the right is the sparkling golden sea. Even the air inside the train car seems to have grown fresher.",
                    Background = SeasideBlue,
                },
                new() {
                    Type = ScreenType.Opening,
                    Body = "The train enters the deep mountains. The shadows of massive trees darken the carriage.",
                    Background = MountainGreen,
                },
                new() {
                    Type = ScreenType.Opening,
                    Body = "You hear the sound of the train whistle. The train slows down.",
                    Background = MountainGreen,
                },
                new() {
                    Type   = ScreenType.Opening,
                    Body   = $"You've arrived at the station. This is your destination: {H("Otowa")}.",
                    Background = MountainGreen,
                },
                new() {
                    Type = ScreenType.Opening,
                    Body = "An elderly woman dressed in a black kimono stands on the platform, as if waiting to welcome you.",
                    Background = Black,
                    HideBubbles = true,
                    WaitForBackground = true,
                },
            };
        }
    }
}
