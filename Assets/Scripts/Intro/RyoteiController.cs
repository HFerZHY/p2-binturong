using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using TMPro;
using Otowa.IndoorDialogue;

namespace Otowa.Intro
{
    /// <summary>
    /// Scene 5 (Ryotei): welcome banquet, character introductions, inspector confrontation.
    ///
    /// Setup: empty scene → empty GameObject → attach this script.
    /// Inspector fields: nextSceneName = "Day1World", assign serifFont.
    /// Background: assign a Sprite; falls back to dark-green solid colour.
    /// Sprites: auto-loaded from Resources/Characters/WorldSprite/*_portrait if not assigned.
    ///
    /// Inspiration unlocks 10, 11, 12 fire when the player advances past
    /// "Well, I guess that sparked some inspiration..." — InspirationManager
    /// shows the toast pop-ups and manages the E-key journal automatically.
    /// </summary>
    public class RyoteiController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Scene")]
        [SerializeField] private string nextSceneName   = "Day1World";
        [SerializeField] private float  typewriterSpeed = 35f;
        [SerializeField] private float  fadeDuration    = 0.35f;

        [Header("Fonts")]
        [SerializeField] private TMP_FontAsset serifFont;

        [Header("Background (leave blank for dark-green placeholder)")]
        [SerializeField] private Sprite backgroundSprite;

        [Header("Character Sprites (leave blank to auto-load portraits)")]
        [SerializeField] private Sprite rinSprite;
        [SerializeField] private Sprite junkoSprite;
        [SerializeField] private Sprite yujiSprite;
        [SerializeField] private Sprite jiroSprite;
        [SerializeField] private Sprite inspectorSprite;

        [Header("Audio")]
        [SerializeField] private AudioClip bgmClip;
        [SerializeField] private AudioClip drinkPourClip;
        [SerializeField] private AudioClip glassesToastClip;
        [SerializeField] [Range(0f, 1f)] private float musicVolume = 0.4f;

        // ── Beat data ─────────────────────────────────────────────────────────

        private struct Beat
        {
            public string   Speaker;
            public string   Text;
            public bool     IsThought;
            public bool     ShowsInspector;
            public bool     HidesInspector;
            public int[]    UnlocksInspirations;
            public string   Id;          // optional: target ID for branch jumps
            public string   JumpToId;   // if set, advance jumps to this ID instead of next sequential beat
            public BChoice[] Choices;    // if set, show choice buttons instead of auto-advancing
        }

        private struct BChoice
        {
            public string Label;
            public string TargetId;
        }

        private List<Beat> _beats;
        private Dictionary<string, int> _beatIndex;
        private int _current = -1;

        // ── State ─────────────────────────────────────────────────────────────

        private bool      _inputLock;
        private IndoorDialogueTextPlayer _textPlayer;
        private bool      _inspectorVisible;
        private bool      _choosingBranch;
        private AudioSource _sfxSource;
        private Coroutine _shimmerCR;

        // ── UI refs ───────────────────────────────────────────────────────────

        private CanvasGroup _fade;

        private GameObject _choicePanel;
        private GameObject _dlgPanel;
        private Image      _rinImg;
        private Image      _jiroImg;
        private Image      _junkoImg;
        private Image      _yujiImg;
        private Image      _inspImg;

        private TMP_Text   _speakerTmp;
        private TMP_Text   _bodyTmp;
        private TMP_Text   _promptTmp;

        private AudioSource _audioSource;

        // ── Colours ───────────────────────────────────────────────────────────

        private static readonly Color PanelBg  = new Color32(0x06, 0x0e, 0x06, 0xEE);
        private static readonly Color RinGreen = new Color32(0x8f, 0xbc, 0x8f, 0xFF);
        private static readonly Color JunkoC   = new Color32(0xd4, 0xa0, 0x60, 0xFF);
        private static readonly Color YujiC    = new Color32(0x80, 0xb8, 0xe8, 0xFF);
        private static readonly Color JiroC    = new Color32(0x98, 0x98, 0xa8, 0xFF);
        private static readonly Color InspC    = new Color32(0xa0, 0xa8, 0xc0, 0xFF);
        private static readonly Color BodyC    = new Color32(0xc8, 0xd4, 0xc8, 0xFF);
        private static readonly Color ThoughtC = new Color32(0x90, 0x9a, 0x90, 0xFF);
        private static readonly Color PromptC  = new Color32(0xFF, 0xFF, 0xFF, 0xBB);

        private const float ActiveAlpha   = 1.00f;
        private const float InactiveAlpha = 0.30f;

        // ─────────────────────────────────────────────────────────────────────

        private void Awake()
        {
            Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
            BuildBeats();
            _beatIndex = new Dictionary<string, int>();
            for (int i = 0; i < _beats.Count; i++)
                if (!string.IsNullOrEmpty(_beats[i].Id))
                    _beatIndex[_beats[i].Id] = i;
            LoadSprites();
            BuildUI();
            _textPlayer = gameObject.AddComponent<IndoorDialogueTextPlayer>();
            _textPlayer.Initialize(_promptTmp, typewriterSpeed);
            SetupAudio();
            // Share font with the persistent InspirationManager
            InspirationManager.Instance.SetFont(serifFont);
        }

        private void Start()
        {
            _fade.alpha = 0f;
            StartCoroutine(StartAfterFade());
        }

        private IEnumerator StartAfterFade()
        {
            yield return StartCoroutine(FadeTo(1f));
            AdvanceBeat();
        }

        private void Update()
        {
            if (_inputLock) return;
            if (InspirationManager.IsJournalOpen) return; // journal blocks dialogue input

            var mouse = Mouse.current;
            var kb    = Keyboard.current;
            bool clicked = (mouse != null && mouse.leftButton.wasPressedThisFrame)
                        || (kb    != null && kb.spaceKey.wasPressedThisFrame)
                        || (kb    != null && kb.enterKey.wasPressedThisFrame);
            if (!clicked) return;
            if (_choosingBranch) return;
            if (_textPlayer.IsTyping) { _textPlayer.Skip(); return; }
            AdvanceBeat();
        }

        // ── Beat flow ─────────────────────────────────────────────────────────

        private void AdvanceBeat()
        {
            FirePrevEffects();
            if (_current >= 0 && !string.IsNullOrEmpty(_beats[_current].JumpToId))
            {
                string jumpId = _beats[_current].JumpToId;
                if (_beatIndex.TryGetValue(jumpId, out int jumpIdx))
                { ShowBeat(jumpIdx); return; }
            }
            int next = _current + 1;
            if (next >= _beats.Count) { StartCoroutine(FadeAndLoad()); return; }
            ShowBeat(next);
        }

        private void JumpToBeat(string targetId)
        {
            if (_shimmerCR != null) { StopCoroutine(_shimmerCR); _shimmerCR = null; }
            if (!_beatIndex.TryGetValue(targetId, out int idx))
            {
                Debug.LogWarning($"[RyoteiController] Branch target '{targetId}' not found.");
                int fallback = _current + 1;
                if (fallback < _beats.Count) ShowBeat(fallback);
                return;
            }
            _choosingBranch = false;
            _choicePanel.SetActive(false);
            ShowBeat(idx);
        }

        private void FirePrevEffects()
        {
            if (_shimmerCR != null) { StopCoroutine(_shimmerCR); _shimmerCR = null; }
            if (_current < 0) return;
            var prev = _beats[_current];
            if (prev.HidesInspector)
                StartCoroutine(FadeOutInspector());
            if (prev.UnlocksInspirations != null)
                foreach (int id in prev.UnlocksInspirations)
                    InspirationManager.Instance.Unlock(id);
        }

        private void ShowChoices(Beat b)
        {
            _choosingBranch = true;
            _dlgPanel.SetActive(false);
            _choicePanel.SetActive(true);

            // Clear old buttons
            foreach (Transform child in _choicePanel.transform)
                Destroy(child.gameObject);

            float btnH = 0.16f;
            float gap   = 0.03f;
            int   count = b.Choices.Length;
            float totalH = count * btnH + (count - 1) * gap;
            float startY = 0.5f + totalH * 0.5f - btnH * 0.5f;

            for (int i = 0; i < count; i++)
            {
                var choice = b.Choices[i];
                float yMax = startY - i * (btnH + gap);
                float yMin = yMax - btnH;

                var btnGo = new GameObject($"Choice{i}", typeof(RectTransform));
                btnGo.transform.SetParent(_choicePanel.transform, false);
                var brt = (RectTransform)btnGo.transform;
                brt.anchorMin = new Vector2(0.2f, yMin);
                brt.anchorMax = new Vector2(0.8f, yMax);
                brt.offsetMin = brt.offsetMax = Vector2.zero;

                var bg  = btnGo.AddComponent<Image>();
                bg.color = new Color32(0x10, 0x28, 0x10, 0xDD);

                var btn = btnGo.AddComponent<Button>();
                string targetId = choice.TargetId;
                btn.onClick.AddListener(() => JumpToBeat(targetId));

                var lbl = new GameObject("Label", typeof(RectTransform));
                lbl.transform.SetParent(btnGo.transform, false);
                var lrt = (RectTransform)lbl.transform;
                lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
                lrt.offsetMin = lrt.offsetMax = Vector2.zero;
                var tmp = lbl.AddComponent<TextMeshProUGUI>();
                tmp.text      = choice.Label;
                tmp.fontSize  = 26f;
                tmp.color     = new Color32(0xc8, 0xd4, 0xc8, 0xFF);
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.richText  = true;
                if (serifFont != null) tmp.font = serifFont;
            }
        }

        private void ShowBeat(int index)
        {
            _current = index;
            var b = _beats[index];

            if (b.Choices != null && b.Choices.Length > 0)
            {
                ShowChoices(b);
                return;
            }

            _dlgPanel.SetActive(true);
            SetPrompt(false);

            // Inspector entrance: fade in and cut BGM
            if (b.ShowsInspector && !_inspectorVisible)
            {
                _inspectorVisible = true;
                _inspImg.gameObject.SetActive(true);
                SetAlpha(_inspImg, 0f);
                _audioSource.Stop();
                StartCoroutine(FadeInInspector(b.Speaker == "Inspector" ? ActiveAlpha : InactiveAlpha));
            }

            // Speaker label
            _speakerTmp.text = b.Speaker ?? "";
            _speakerTmp.color = b.Speaker switch
            {
                "Rin"       => RinGreen,
                "Junko"     => JunkoC,
                "Yuji"      => YujiC,
                "Jiro"      => JiroC,
                "Inspector" => InspC,
                _           => BodyC,
            };
            _speakerTmp.alignment = b.Speaker == "Rin"
                ? TextAlignmentOptions.Left
                : TextAlignmentOptions.Right;

            // Highlight active speaker, dim others
            SetAlpha(_rinImg,   b.Speaker == "Rin"   ? ActiveAlpha : InactiveAlpha);
            SetAlpha(_junkoImg, b.Speaker == "Junko" ? ActiveAlpha : InactiveAlpha);
            SetAlpha(_yujiImg,  b.Speaker == "Yuji"  ? ActiveAlpha : InactiveAlpha);
            SetAlpha(_jiroImg,  b.Speaker == "Jiro"  ? ActiveAlpha : InactiveAlpha);

            // Inspector alpha only managed here once visible (FadeIn handles the entrance)
            if (_inspectorVisible && !b.ShowsInspector)
                SetAlpha(_inspImg, b.Speaker == "Inspector" ? ActiveAlpha : InactiveAlpha);

            _bodyTmp.color     = b.IsThought ? ThoughtC : BodyC;
            _bodyTmp.fontStyle = b.IsThought ? FontStyles.Italic : FontStyles.Normal;

            // SFX triggers keyed to beat IDs
            if (b.Id == "offer_sake"   && drinkPourClip    != null) _sfxSource?.PlayOneShot(drinkPourClip);
            if (b.Id == "welcome_seat" && glassesToastClip != null) _sfxSource?.PlayOneShot(glassesToastClip);

            if (b.Id == "inspire_moment")
                _textPlayer.Play(_bodyTmp, b.Text,
                    () => { _shimmerCR = StartCoroutine(ShimmerWordCR(_bodyTmp, "inspiration")); });
            else
                _textPlayer.Play(_bodyTmp, b.Text);
        }

        // ── Inspector fade in / out ───────────────────────────────────────────

        private IEnumerator FadeInInspector(float targetAlpha)
        {
            const float duration = 0.5f;
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                SetAlpha(_inspImg, Mathf.Lerp(0f, targetAlpha, Mathf.Clamp01(t / duration)));
                yield return null;
            }
            SetAlpha(_inspImg, targetAlpha);
        }

        private IEnumerator ShimmerWordCR(TMP_Text tmp, string word)
        {
            yield return null;
            tmp.ForceMeshUpdate();

            var charInfo = tmp.textInfo.characterInfo;
            int charCount = tmp.textInfo.characterCount;
            int wordStart = -1;
            for (int i = 0; i <= charCount - word.Length; i++)
            {
                bool match = true;
                for (int j = 0; j < word.Length; j++)
                    if (char.ToLower(charInfo[i + j].character) != char.ToLower(word[j]))
                    { match = false; break; }
                if (match) { wordStart = i; break; }
            }
            if (wordStart < 0) yield break;

            Color baseCol = ThoughtC;
            Color glowCol = new Color(0.96f, 0.86f, 0.55f); // warm gold

            while (true)
            {
                tmp.ForceMeshUpdate();
                var meshInfo = tmp.textInfo.meshInfo;
                charInfo     = tmp.textInfo.characterInfo;

                for (int i = wordStart; i < wordStart + word.Length; i++)
                {
                    if (!charInfo[i].isVisible) continue;
                    float phase = Time.time * 2.5f + (i - wordStart) * 0.7f;
                    float t = (Mathf.Sin(phase) + 1f) * 0.5f;
                    Color32 c32 = Color.Lerp(baseCol, glowCol, t * 0.55f);
                    int mat  = charInfo[i].materialReferenceIndex;
                    int vert = charInfo[i].vertexIndex;
                    var cols = meshInfo[mat].colors32;
                    cols[vert] = cols[vert+1] = cols[vert+2] = cols[vert+3] = c32;
                }

                tmp.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
                yield return null;
            }
        }

        private IEnumerator FadeOutInspector()
        {
            const float duration = 0.4f;
            float startAlpha = _inspImg.color.a;
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                SetAlpha(_inspImg, Mathf.Lerp(startAlpha, 0f, Mathf.Clamp01(t / duration)));
                yield return null;
            }
            _inspImg.gameObject.SetActive(false);
            _inspectorVisible = false;
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

        private void LoadSprites()
        {
            rinSprite       = LoadPortrait(rinSprite,       "rin");
            junkoSprite     = LoadPortrait(junkoSprite,     "Junko");
            yujiSprite      = LoadPortrait(yujiSprite,      "Yuji");
            jiroSprite      = LoadPortrait(jiroSprite,      "Jiro");
            inspectorSprite = LoadPortrait(inspectorSprite, "Inspector");
        }

        private static Sprite LoadPortrait(Sprite existing, string charName)
        {
            if (existing != null) return existing;
            var tex = Resources.Load<Texture2D>($"Characters/WorldSprite/{charName}_portrait");
            if (tex != null)
                return Sprite.Create(tex,
                    new Rect(0, 0, tex.width, tex.height),
                    new Vector2(0.5f, 0f), 100f);
            var fb = new Texture2D(1, 1);
            fb.SetPixel(0, 0, new Color(0.3f, 0.3f, 0.35f, 1f));
            fb.Apply();
            return Sprite.Create(fb, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0f), 100f);
        }

        // ── UI construction ───────────────────────────────────────────────────

        private static void EnsureEventSystem()
        {
            if (FindObjectOfType<EventSystem>() != null) return;
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            var iiType = System.Type.GetType(
                "UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (iiType != null) go.AddComponent(iiType);
            else                go.AddComponent<StandaloneInputModule>();
        }

        private void BuildUI()
        {
            EnsureEventSystem();
            var cvGo = new GameObject("RyoteiCanvas",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            cvGo.transform.SetParent(transform, false);

            var canvas = cvGo.GetComponent<Canvas>();
            canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = cvGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode     = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight  = 0.5f;

            _fade = cvGo.AddComponent<CanvasGroup>();

            // Background
            var bgGo = MakeRect(cvGo.transform, "BG", Vector2.zero, Vector2.one);
            var bgImg = bgGo.AddComponent<Image>();
            if (backgroundSprite != null)
            {
                bgImg.sprite = backgroundSprite;
                bgImg.color  = Color.white;
            }
            else
            {
                bgImg.color = new Color32(0x0c, 0x1c, 0x0c, 0xFF);
            }

            BuildDialoguePanel(cvGo.transform);
            BuildChoicePanel(cvGo.transform);

            _promptTmp = MakeTMP(cvGo.transform, "Prompt", "Click to continue  ▼",
                22f, PromptC, TextAlignmentOptions.Right,
                new Vector2(0.60f, 0.02f), new Vector2(0.97f, 0.08f));
            UseFont(_promptTmp);
            _promptTmp.gameObject.SetActive(false);
        }

        private void BuildDialoguePanel(Transform cv)
        {
            _dlgPanel = MakeRect(cv, "DlgPanel", Vector2.zero, Vector2.one);
            var t = _dlgPanel.transform;

            // 5 equal columns (20% each), upper 70% of screen.
            // Rin faces right (no flip); all NPCs flip to face Rin.
            _rinImg   = CharSlot(t, "RinSprite",   0.00f, 0.20f, rinSprite,   false);
            _jiroImg  = CharSlot(t, "JiroSprite",  0.20f, 0.40f, jiroSprite,  true);
            _junkoImg = CharSlot(t, "JunkoSprite", 0.40f, 0.60f, junkoSprite, true);
            _yujiImg  = CharSlot(t, "YujiSprite",  0.60f, 0.80f, yujiSprite,  true);

            // Inspector — far right, hidden until his entrance
            var inspGo = MakeRect(t, "InspSprite",
                new Vector2(0.80f, 0.28f), new Vector2(1.00f, 0.98f));
            inspGo.transform.localScale = new Vector3(-1f, 1f, 1f);
            _inspImg = inspGo.AddComponent<Image>();
            _inspImg.sprite         = inspectorSprite;
            _inspImg.preserveAspect = true;
            _inspImg.color          = new Color(1, 1, 1, 0f);
            _inspImg.raycastTarget  = false;
            _inspImg.gameObject.SetActive(false);

            // Bottom text box
            var panel = MakeRect(t, "Panel", Vector2.zero, new Vector2(1f, 0.28f));
            panel.AddComponent<Image>().color = PanelBg;
            var pt = panel.transform;

            _speakerTmp = MakeTMP(pt, "Speaker", "",
                36f, RinGreen, TextAlignmentOptions.Left,
                new Vector2(0.04f, 0.68f), new Vector2(0.96f, 0.96f));
            _speakerTmp.fontStyle = FontStyles.Bold;
            UseFont(_speakerTmp);

            _bodyTmp = MakeTMP(pt, "Body", "",
                31f, BodyC, TextAlignmentOptions.Left,
                new Vector2(0.04f, 0.04f), new Vector2(0.96f, 0.65f));
            _bodyTmp.lineSpacing = 6f;
            UseFont(_bodyTmp);

            _dlgPanel.SetActive(false);
        }

        private void BuildChoicePanel(Transform cv)
        {
            _choicePanel = MakeRect(cv, "ChoicePanel", Vector2.zero, new Vector2(1f, 0.60f));
            _choicePanel.AddComponent<Image>().color = new Color(0, 0, 0, 0);
            _choicePanel.SetActive(false);
        }

        private Image CharSlot(Transform parent, string name,
                               float xMin, float xMax,
                               Sprite sprite, bool flip)
        {
            var go = MakeRect(parent, name,
                new Vector2(xMin, 0.28f), new Vector2(xMax, 0.98f));
            if (flip) go.transform.localScale = new Vector3(-1f, 1f, 1f);
            var img = go.AddComponent<Image>();
            img.sprite         = sprite;
            img.preserveAspect = true;
            img.color          = new Color(1, 1, 1, InactiveAlpha);
            img.raycastTarget  = false;
            return img;
        }

        // ── UI factory helpers ────────────────────────────────────────────────

        private GameObject MakeRect(Transform parent, string name,
                                    Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
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
            rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return tmp;
        }

        private void UseFont(TMP_Text tmp)
        {
            if (serifFont != null) tmp.font = serifFont;
        }

        // ── Audio ─────────────────────────────────────────────────────────────

        private void SetupAudio()
        {
            _audioSource             = gameObject.AddComponent<AudioSource>();
            _audioSource.clip        = bgmClip;
            _audioSource.volume      = musicVolume;
            _audioSource.loop        = true;
            _audioSource.playOnAwake = false;
            if (bgmClip != null) _audioSource.Play();

            _sfxSource             = gameObject.AddComponent<AudioSource>();
            _sfxSource.playOnAwake = false;
        }

        // ── Beat helpers ──────────────────────────────────────────────────────

        private static Beat D(string speaker, bool thought, string text,
                               int[] unlocks = null, string id = null, string jump = null) => new()
        {
            Speaker              = speaker,
            IsThought            = thought,
            Text                 = text,
            UnlocksInspirations  = unlocks,
            Id                   = id,
            JumpToId             = jump,
        };

        private static Beat Br(string id, params BChoice[] choices) => new()
        {
            Id      = id,
            Choices = choices,
        };

        private static BChoice Ch(string label, string target) =>
            new() { Label = label, TargetId = target };

        // ── Beat list ─────────────────────────────────────────────────────────

        private void BuildBeats()
        {
            _beats = new List<Beat>
            {
                // ── Arrival + introductions ───────────────────────────────────
                D("Junko", false, "Rin, you've arrived. Please, have a seat.", id: "welcome_seat"),
                D("Junko", false, "You've had a long day today. Welcome to Otowa."),
                D("Rin",   false, "It's just... this afternoon on the platform, I ran into a man in a suit who said he was here for a follow-up evaluation."),
                D("Junko", false, "Sigh... Let's save the heavy topics for later. Tonight is a welcome banquet prepared just for you."),

                D("Yuji",  false, "Exactly, exactly! Toss the work stuff right out of your head! Welcome to Otowa, I'm Yuji!"),
                D("Rin",   false, "Hello, Mr. Yuji."),
                D("Yuji",  false, "The Chief's been talking non-stop about a young person coming. Come hang out at my pub anytime — it's the hippest spot in the whole village!"),

                D("Jiro",  false, "Hmph. Don't go corrupting the youth the second they arrive."),
                D("Rin",   true,  "(The older man next to him is wearing a pristine chef's uniform, arms crossed, looking stern.)"),
                D("Jiro",  false, "I am the head chef here, Jiro. Try the food on the table. It'd be a waste if it gets cold."),
                D("Rin",   false, "Thank you, Mr. Jiro."),

                // ── Branch 1: Food reaction ───────────────────────────────────
                Br("branch_food",
                    Ch("(Take a bite.) ...This flavor is really something.", "food_good"),
                    Ch("(Take a bite.) Hmm... not sure what to make of this.", "food_bad"),
                    Ch("(Take a bite.) It's... really quite something, Mr. Jiro.", "food_lie")
                ),

                // Food good path
                D("Rin",   true,  "(Fascinating. A very unique, refreshing feel — like the mountains distilled into a single bite.)", id: "food_good"),
                D("Rin",   false, "This flavor... is really special. It tastes like the mountains."),
                D("Jiro",  false, "At least you have some taste, unlike those city folks used to eating cheap, mass-produced garbage.", jump: "food_merge"),

                // Food bad path
                D("Rin",   true,  "(Hmm... it's quite intense. Not what I was expecting at all.)", id: "food_bad"),
                D("Rin",   false, "It's... quite strong, isn't it."),
                D("Jiro",  false, "(Studies Rin's face for a long moment.) Your expression says it all. City palates. Ruined by processed food."),
                D("Jiro",  false, "Hmph. I'll tell you what you just ate anyway, since you clearly won't figure it out on your own.", jump: "food_merge"),

                // Food lie path
                D("Rin",   true,  "(Strange... I can't tell if I like it. But there's something genuinely compelling here.)", id: "food_lie"),
                D("Rin",   true,  "(Please don't ask a follow-up question...)"),
                D("Jiro",  false, "..."),
                D("Jiro",  false, "Hmph. At least you're not pretending to rave about it. I'll give you that much.", jump: "food_merge"),

                // ── All food paths converge ───────────────────────────────────
                D("Jiro",  false, "The soul of this dish is the shichimi powder I hand-grind and blend myself. It's a recreation of a recipe from hundreds of years ago.", id: "food_merge"),
                D("Rin",   false, "A centuries-old recipe? No wonder the flavor has so much depth."),

                // ── Sake ─────────────────────────────────────────────────────
                D("Yuji",  false, "How can you have good food without good booze! Come on, Rin, try my pride and joy.", id: "offer_sake"),
                D("Rin",   false, "Is this... sake?"),
                D("Yuji",  false, "This isn't just any ordinary sake. Look at that old newspaper on the wall — my sake won a prize at the local specialty competition over a decade ago!"),
                D("Yuji",  false, "I tweaked the recipe to make the mouthfeel softer. A lot of young people really love this flavor."),

                // ── Branch 2: Sake reaction ───────────────────────────────────
                Br("branch_sake",
                    Ch("(Sip.) Hmm... it's a bit bitter.", "sake_bad"),
                    Ch("(Sip.) Oh — this is actually quite smooth.", "sake_good")
                ),

                // Sake bad path
                D("Rin",   true,  "(So bitter! It's way too bitter... Do young people actually like this?)", id: "sake_bad"),
                D("Jiro",  false, "Newfangled nonsense. Brewing should follow the rules. Adding random ingredients just to pander to the youth is nothing but grandstanding."),
                D("Yuji",  false, "Times are changing, old man Jiro! Back in that competition, my booze and your cooking were fighting for the gold medal — and the judges gave their votes to me, didn't they?"),
                D("Jiro",  false, "That just proves the judges had terrible taste.", jump: "sake_merge"),

                // Sake good path
                D("Rin",   true,  "(Oh — this is quite smooth. A delicate sweetness underneath. I see what he means about 'soft mouthfeel'.)", id: "sake_good"),
                D("Rin",   false, "This is... really smooth. I can see why young people like it."),
                D("Yuji",  false, "Ha! See that, old man Jiro? Even the new kid gets it!"),
                D("Jiro",  false, "Hmph. Pandering to the masses isn't artistry. That's commerce."),
                D("Yuji",  false, "He's just sore that my sake beat his cooking at the competition!", jump: "sake_merge"),

                // ── All sake paths converge ───────────────────────────────────
                D("Rin",   false, "Haha... you two really are complete opposites.", id: "sake_merge"),

                // ── Shared flavor revelation ──────────────────────────────────
                D("Rin",   false, "But it's a bit strange. Whether it was the food or the sake just now, I tasted a very similar flavor in both."),
                D("Rin",   true,  "(Mr. Jiro's shichimi powder, Mr. Yuji's gold medal sake, and that specialty herb...)"),
                D("Rin",   true,  "(I saw those things in the stationmaster's office. I thought they were junk. I never expected them to have stories like this.)"),
                D("Rin",   true,  "(Well, I guess that sparked some <b>inspiration</b>...", unlocks: new[] { 10, 11, 12 }, id: "inspire_moment"),


                // ── Inspector arrives ─────────────────────────────────────────
                new Beat
                {
                    Speaker        = "Inspector",
                    Text           = "Good evening, everyone. It seems you're all in a rather leisurely mood.",
                    ShowsInspector = true,
                },

                D("Junko",     false, "You must be... the inspector from the railway company, right? Please, sit down and eat with us."),
                D("Inspector", false, "That won't be necessary. My time is limited, and I have already completed my on-site follow-up evaluation of Otowa Station."),
                D("Inspector", false, "The results of these past few inspections have shown zero signs of improvement. The railway company is not running a charity."),
                D("Junko",     false, "Mr. Inspector, we are working hard to rectify the situation. Please, just give us a little more time…"),
                D("Inspector", false, "The company's patience has been exhausted. Just now, my superiors replied with their final decision."),
                D("Inspector", false, "In two days, Otowa Station will be permanently closed. All trains will cease stopping here."),
                D("Yuji",      false, "Hey! Are you kidding me?! Two days? That's way too sudden!"),
                D("Junko",     false, "Two days from now is the Summer Festival! That is the most important day for us in Otowa! The trains absolutely cannot stop running then!"),
                D("Inspector", false, "Unless you can prove to me within these two days that this station possesses irreplaceable value, our decision will not change."),

                new Beat { Speaker = "Inspector", Text = "Excuse me.", HidesInspector = true },

                // ── Aftermath ─────────────────────────────────────────────────
                D("Rin",   true,  "(Silence. Yuji is tightly clenching his fists. Jiro has his head bowed without saying a word.)"),
                D("Junko", false, "Rin... I am truly very sorry. You came full of expectations, and right away we dragged you into a massive mess."),
                D("Junko", false, "Take the early train tomorrow and head back to the city. This shouldn't be yours to bear."),
                D("Rin",   false, "It may not be time to give up yet. In his letter, Mr. Hikaru said he wanted to turn the station into a museum that showcases Otowa's charm."),
                D("Rin",   false, "Even though he only collected a pile of \"junk,\" he asked me to be the curator. If I can properly exhibit these items that carry the soul of Otowa, wouldn't that make the inspector change his mind?"),
                D("Yuji",  false, "Turn the station into a museum? That guy Hikaru was doing something like that behind our backs?!"),
                D("Jiro",  false, "Hmph, with Hikaru's brain, he wouldn't be able to display anything decent. But, if it's you..."),
                D("Yuji",  false, "Yeah! Rin! If even a picky guy like Jiro approves of your taste, you can definitely pull it off!"),
                D("Junko", false, "Rin... are you really willing to help us with this?"),
                D("Rin",   false, "Yeah. Just leave it to me."),
                D("Junko", false, "No matter what happens... on behalf of all the villagers in Otowa, I thank you."),
                D("Junko", false, "We will prepare for this year's Summer Festival... but for it to be held successfully, we are counting on you."),
                D("Rin",   true,  "(The Summer Festival... what kind of day is that? Why do they value it so much?)"),
            };
        }
    }
}
