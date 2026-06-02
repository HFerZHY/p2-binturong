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
    /// Self-contained visual-novel dialogue controller for the Junko intro scene.
    ///
    /// Setup: create an empty scene, add an empty GameObject, attach this script.
    /// All UI is built at runtime — no prefabs required.
    ///
    /// Sprites: the script auto-loads Characters/CharacterSheet from Resources,
    /// splitting it at the horizontal midpoint (Rin = left half, Junko = right half).
    /// Assign serifFont (Cormorant Garamond) in the Inspector to match the webapp.
    /// </summary>
    public class IntroDialogueController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Scene")]
        [SerializeField] private string nextSceneName = "Intro-3";
        [SerializeField] private float  typewriterSpeed = 38f;
        [SerializeField] private float  fadeDuration    = 0.35f;

        [Header("Fonts")]
        [SerializeField] private TMP_FontAsset serifFont;

        [Header("Sprites  (leave blank to use auto-split CharacterSheet)")]
        [SerializeField] private Sprite rinSprite;
        [SerializeField] private Sprite junkoSprite;

        [Header("Audio")]
        [SerializeField] private AudioClip ambientClip;
        [SerializeField] [Range(0f, 1f)] private float musicVolume = 0.35f;

        [Header("Behaviour")]
        [SerializeField] private bool showMovementTutorial = false;
        [SerializeField] private bool skipDialogue = false;

        [Header("Colors")]
        [SerializeField] private Color bgColor        = new Color32(0x0e, 0x18, 0x0e, 0xFF);
        [SerializeField] private Color panelColor     = new Color32(0x06, 0x0e, 0x06, 0xEE);
        [SerializeField] private Color rinNameColor   = new Color32(0x8f, 0xbc, 0x8f, 0xFF);
        [SerializeField] private Color junkoNameColor = new Color32(0xc4, 0xb8, 0xa0, 0xFF);
        [SerializeField] private Color bodyColor      = new Color32(0xc8, 0xd4, 0xc8, 0xFF);
        [SerializeField] private Color tutorialBg     = new Color32(0x1a, 0x14, 0x0a, 0xF2);
        [SerializeField] private Color promptColor    = new Color32(0xFF, 0xFF, 0xFF, 0xBB);

        [Header("Font Sizes")]
        [SerializeField] private float speakerFontSize  = 28f;
        [SerializeField] private float bodyFontSize     = 27f;
        [SerializeField] private float promptFontSize   = 20f;
        [SerializeField] private float tutorialFontSize = 24f;

        // ── Dialogue data ─────────────────────────────────────────────────────

        private struct Line { public string Speaker; public string Text; }
        private List<Line> _lines;
        private int _current = -1;

        // ── State ─────────────────────────────────────────────────────────────

        private bool     _inputLock;
        private bool     _tutorialActive;
        private IndoorDialogueTextPlayer _textPlayer;

        // ── UI refs ───────────────────────────────────────────────────────────

        private CanvasGroup _fade;
        private GameObject  _tutorialPanel;
        private GameObject  _dialoguePanel;
        private Image       _rinImg;
        private Image       _junkoImg;
        private TMP_Text    _speakerTmp;
        private TMP_Text    _bodyTmp;
        private TMP_Text    _promptTmp;
        private AudioSource _audioSource;

        private const float ActiveAlpha   = 1.00f;
        private const float InactiveAlpha = 0.38f;

        // ─────────────────────────────────────────────────────────────────────

        private void Awake()
        {
            if (skipDialogue) return;
            Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
            _tutorialActive = showMovementTutorial;
            BuildLines();
            LoadCharacterSprites();
            BuildUI();
            _textPlayer = gameObject.AddComponent<IndoorDialogueTextPlayer>();
            _textPlayer.Initialize(_promptTmp, typewriterSpeed);
            SetupAudio();
        }

        private void Start()
        {
            if (skipDialogue) { SceneManager.LoadScene(nextSceneName); return; }
            _fade.alpha = 0f;
            StartCoroutine(FadeTo(1f));
        }

        private void Update()
        {
            if (_inputLock) return;

            var mouse = Mouse.current;
            var kb    = Keyboard.current;
            bool clicked = (mouse != null && mouse.leftButton.wasPressedThisFrame)
                        || (kb    != null && kb.spaceKey.wasPressedThisFrame)
                        || (kb    != null && kb.enterKey.wasPressedThisFrame);

            if (!clicked) return;

            if (_tutorialActive) { DismissTutorial(); return; }
            if (_textPlayer.IsTyping) { _textPlayer.Skip(); return; }
            AdvanceLine();
        }

        // ── Tutorial ──────────────────────────────────────────────────────────

        private void DismissTutorial()
        {
            _tutorialActive = false;
            _tutorialPanel.SetActive(false);
            AdvanceLine();
        }

        // ── Line advancement ──────────────────────────────────────────────────

        private void AdvanceLine()
        {
            int next = _current + 1;
            if (next >= _lines.Count) { StartCoroutine(FadeAndLoad()); return; }
            ShowLine(next);
        }

        private void ShowLine(int index)
        {
            _current = index;
            var line = _lines[index];

            _dialoguePanel.SetActive(true);

            bool isRin = line.Speaker == "Rin";

            // Speaker name: left-aligned for Rin, right-aligned for Junko
            _speakerTmp.text      = line.Speaker ?? "";
            _speakerTmp.color     = isRin ? rinNameColor : junkoNameColor;
            _speakerTmp.alignment = isRin ? TextAlignmentOptions.Left : TextAlignmentOptions.Right;

            // Active speaker is fully visible; inactive is dimmed
            SetAlpha(_rinImg,   isRin ? ActiveAlpha : InactiveAlpha);
            SetAlpha(_junkoImg, isRin ? InactiveAlpha : ActiveAlpha);

            _textPlayer.Play(_bodyTmp, line.Text);
        }

        // ── Transitions ───────────────────────────────────────────────────────

        private IEnumerator FadeAndLoad()
        {
            _inputLock = true;
            yield return StartCoroutine(FadeTo(0f));
            SceneManager.LoadScene(nextSceneName);
        }

        private IEnumerator FadeTo(float target)
        {
            float start = _fade.alpha, elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                _fade.alpha = Mathf.Lerp(start, target, elapsed / fadeDuration);
                elapsed += Time.deltaTime;
                yield return null;
            }
            _fade.alpha = target;
        }

        private void SetPrompt(bool show) => _textPlayer.SetPromptVisible(show);

        private static void SetAlpha(Image img, float a)
        {
            if (img == null) return;
            var c = img.color; c.a = a; img.color = c;
        }

        // ── Sprite loading ────────────────────────────────────────────────────

        private void LoadCharacterSprites()
        {
            // Use inspector-assigned sprites if provided
            if (rinSprite != null && junkoSprite != null) return;

            // Fall back to auto-splitting Characters/CharacterSheet
            // CharacterSheet.jpg: Rin occupies the left half, Junko the right half
            var tex = Resources.Load<Texture2D>("Characters/CharacterSheet");
            if (tex == null)
            {
                Debug.LogWarning("[IntroDialogueController] Characters/CharacterSheet not found in Resources.");
                return;
            }

            int w = tex.width, h = tex.height;
            const float ppu = 100f;

            if (rinSprite == null)
                rinSprite = Sprite.Create(tex,
                    new Rect(0f, 0, w * 0.5f, h), new Vector2(0.5f, 0f), ppu);

            if (junkoSprite == null)
                junkoSprite = Sprite.Create(tex,
                    new Rect(w * 0.5f, 0, w * 0.5f, h), new Vector2(0.5f, 0f), ppu);
        }

        // ── UI construction ───────────────────────────────────────────────────

        private void BuildUI()
        {
            var canvasGo = new GameObject("DialogueCanvas",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);

            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode     = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight  = 0.5f;

            _fade = canvasGo.AddComponent<CanvasGroup>();

            MakeImage(canvasGo.transform, "BG", bgColor, Vector2.zero, Vector2.one);

            BuildCharacterLayer(canvasGo.transform);
            if (showMovementTutorial) BuildTutorialPanel(canvasGo.transform);
            BuildDialoguePanel(canvasGo.transform);

            // Continue prompt — bottom-right, always above panels
            _promptTmp = MakeTMP(canvasGo.transform, "Prompt", "Click to continue  >",
                promptFontSize, promptColor, TextAlignmentOptions.Right,
                new Vector2(0.60f, 0.02f), new Vector2(0.97f, 0.08f));
            UseFont(_promptTmp, serifFont);
            _promptTmp.gameObject.SetActive(false);
        }

        // ── Character sprite layer (upper 70 % of screen) ────────────────────

        private void BuildCharacterLayer(Transform canvas)
        {
            // Rin — left quadrant
            var rinGo = MakeRect(canvas, "RinSprite",
                new Vector2(0.03f, 0.28f), new Vector2(0.38f, 0.98f));
            _rinImg = rinGo.AddComponent<Image>();
            _rinImg.sprite        = rinSprite;
            _rinImg.preserveAspect = true;
            _rinImg.color          = new Color(1, 1, 1, InactiveAlpha);
            _rinImg.raycastTarget  = false;

            // Junko — right quadrant, flipped to face Rin
            var junkoGo = MakeRect(canvas, "JunkoSprite",
                new Vector2(0.62f, 0.28f), new Vector2(0.97f, 0.98f));
            junkoGo.transform.localScale = new Vector3(-1f, 1f, 1f);
            _junkoImg = junkoGo.AddComponent<Image>();
            _junkoImg.sprite        = junkoSprite;
            _junkoImg.preserveAspect = true;
            _junkoImg.color          = new Color(1, 1, 1, InactiveAlpha);
            _junkoImg.raycastTarget  = false;
        }

        // ── Tutorial popup (centered, dismisses on click) ─────────────────────

        private void BuildTutorialPanel(Transform canvas)
        {
            var go = MakeRect(canvas, "TutorialPanel",
                new Vector2(0.22f, 0.32f), new Vector2(0.78f, 0.68f));
            _tutorialPanel = go;

            var bg = go.AddComponent<Image>();
            bg.color = tutorialBg;

            // Thin colored border via a slightly larger sibling behind — done with a frame image
            // (simple approach: just rely on the panel color; add a line separator)

            var txt = MakeTMP(go.transform, "TutorialText",
                "Press <b>WASD</b> to move the protagonist\n" +
                "Press <b>E</b> to interact with characters\n\n" +
                "<size=78%><color=#8fbc8f>— Click to continue —</color></size>",
                tutorialFontSize, bodyColor, TextAlignmentOptions.Center,
                new Vector2(0.06f, 0.06f), new Vector2(0.94f, 0.94f));
            UseFont(txt, serifFont);
        }

        // ── Dialogue panel (bottom 28 % of screen) ───────────────────────────

        private void BuildDialoguePanel(Transform canvas)
        {
            var go = MakeRect(canvas, "DialoguePanel",
                Vector2.zero, new Vector2(1f, 0.28f));
            _dialoguePanel = go;

            var bg = go.AddComponent<Image>();
            bg.color = panelColor;

            var pt = go.transform;

            // Speaker name — spans full width; alignment flips per speaker
            _speakerTmp = MakeTMP(pt, "SpeakerName", "",
                speakerFontSize, rinNameColor, TextAlignmentOptions.Left,
                new Vector2(0.04f, 0.68f), new Vector2(0.96f, 0.96f));
            _speakerTmp.fontStyle = FontStyles.Bold;
            UseFont(_speakerTmp, serifFont);

            // Body text
            _bodyTmp = MakeTMP(pt, "Body", "",
                bodyFontSize, bodyColor, TextAlignmentOptions.Left,
                new Vector2(0.04f, 0.04f), new Vector2(0.96f, 0.65f));
            _bodyTmp.lineSpacing = 6f;
            UseFont(_bodyTmp, serifFont);

            _dialoguePanel.SetActive(false);
        }

        // ── UI factory helpers ────────────────────────────────────────────────

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
            tmp.text             = text;
            tmp.fontSize         = size;
            tmp.color            = color;
            tmp.alignment        = align;
            tmp.richText         = true;
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

        // ── Audio ─────────────────────────────────────────────────────────────

        private void SetupAudio()
        {
            _audioSource             = gameObject.AddComponent<AudioSource>();
            _audioSource.clip        = ambientClip;
            _audioSource.volume      = musicVolume;
            _audioSource.loop        = true;
            _audioSource.playOnAwake = false;
            if (ambientClip != null) _audioSource.Play();
        }

        // ── Dialogue lines ────────────────────────────────────────────────────

        private void BuildLines()
        {
            _lines = new List<Line>
            {
                new() { Speaker = "Junko", Text = "You must be tired from your journey. You are Rin, right? I am Junko, the chief of Otowa village." },
                new() { Speaker = "Rin",   Text = "Hello, it's nice to meet you. The air here is so nice, completely different from the city." },
                new() { Speaker = "Junko", Text = "I am relieved to hear you say that. Welcome to Otowa." },
                new() { Speaker = "Rin",   Text = "Thank you. Excuse me, is Mr. Hikaru here? We agreed on the phone to hand over the work today." },
                new() { Speaker = "Junko", Text = "Ah... regarding that, I am truly very sorry." },
                new() { Speaker = "Rin",   Text = "What's wrong? Is he not in the village?" },
                new() { Speaker = "Junko", Text = "Yesterday afternoon, he suddenly packed a bag and left. He said there was something extremely important he had to go take care of immediately." },
                new() { Speaker = "Rin",   Text = "Something important?" },
                new() { Speaker = "Junko", Text = "Yes, though he didn't disclose exactly what it was to us. He left in quite a hurry." },
                new() { Speaker = "Rin",   Text = "He left... Then what about the work at the station?" },
                new() { Speaker = "Junko", Text = "So, for the next few days until Hikaru returns, I'm afraid we will have to impose on you to temporarily act as the acting stationmaster here." },
                new() { Speaker = "Rin",   Text = "Me, as the acting stationmaster? But I can't lead a train station!" },
                new() { Speaker = "Junko", Text = "Please don't feel too much pressure, Rin. We rarely get any trains stopping here anyway. Just think of it as taking a short vacation." },
                new() { Speaker = "Rin",   Text = "He mentioned on the phone that this job would be a bit challenging... Is this what he meant?" },
                new() { Speaker = "Junko", Text = "Sigh, that boy is always like this, doing things purely on impulse. He really has caused you a lot of trouble." },
                new() { Speaker = "Junko", Text = "To express our apologies, and to welcome your arrival, we've prepared a welcome banquet for you tonight at the village Ryotei." },
                new() { Speaker = "Junko", Text = "Please do me the honor of attending. It's been a long time since a young person came to the village; everyone is looking forward to meeting you." },
                new() { Speaker = "Rin",   Text = "Thank you for the invitation, I will be there." },
                new() { Speaker = "Junko", Text = "Well then, I will leave the stationmaster's office key with you. You can go settle in first. See you tonight." },
                new() { Speaker = "Rin",   Text = "Alright, see you tonight." },
            };
        }
    }
}
