using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.EventSystems;
using TMPro;
using Otowa.Audio;
using Otowa.IndoorDialogue;
using Otowa.SaveSystem;
using Otowa.UI;

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
            public string[] ItemIconKeys;
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
        private RectTransform _itemIconRoot;
        private readonly List<Image> _itemIconImages = new();
        private readonly Dictionary<string, Sprite> _itemIconCache = new();

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
        private const string ItemHighlightColor = "#8B5A2B";
        private const float ItemIconBaseSize = 72f;
        private const float DefaultItemIconScale = 1.8f;
        private const float SakeItemIconScale = 2.5f;

        // ─────────────────────────────────────────────────────────────────────

        private void Awake()
        {
            Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
            serifFont = RuntimeFontLibrary.BreeSerifRegularOr(serifFont);
            BuildBeats();
            _beatIndex = new Dictionary<string, int>();
            for (int i = 0; i < _beats.Count; i++)
                if (!string.IsNullOrEmpty(_beats[i].Id))
                    _beatIndex[_beats[i].Id] = i;
            LoadSprites();
            BuildUI();
            _textPlayer = gameObject.AddComponent<IndoorDialogueTextPlayer>();
            _textPlayer.Initialize(_promptTmp, typewriterSpeed);
            // Share font with the persistent InspirationManager
            InspirationManager.Instance.SetFont(serifFont);
        }

        private void Start()
        {
            _fade.alpha = 0f;
            GameAudioManager.Instance.StopSfxLoop(AudioId.Wind, 0.25f);
            GameAudioManager.Instance.PlayBgm(AudioId.DayWalk, fadeIn: 0.35f);
            StartCoroutine(StartAfterFade());
        }

        private IEnumerator StartAfterFade()
        {
            yield return StartCoroutine(FadeTo(1f));
            GameAudioManager.Instance.PlaySfxOnce(AudioId.DrinkPour);
            yield return new WaitForSeconds(2f);
            GameAudioManager.Instance.PlaySfxOnce(AudioId.GlassesToast);
            AdvanceBeat();
        }

        private void Update()
        {
            if (_inputLock) return;
            if (PauseMenuController.ShouldSuppressWorldAdvance) return;
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
            if (_current >= 0
                && _beats[_current].Id == "inspire_moment"
                && _beats[next].ShowsInspector)
            {
                StartCoroutine(PrepareInspectorEntrance(next));
                return;
            }
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
            {
                SetVillagersFacingInspector(false);
                StartCoroutine(FadeOutInspector());
                GameAudioManager.Instance.StopBgm(0.35f);
                GameAudioManager.Instance.PlaySfxLoop(AudioId.Wind, fadeIn: 0.35f);
            }
            if (prev.UnlocksInspirations != null)
                InspirationManager.Instance.UnlockBatch(
                    prev.UnlocksInspirations,
                    toastHoldDuration: 0.8f);
        }

        private void ShowChoices(Beat b)
        {
            _choosingBranch = true;
            _dlgPanel.SetActive(false);
            _choicePanel.SetActive(true);
            SetItemIcons(null);

            // Clear old buttons
            foreach (Transform child in _choicePanel.transform)
                Destroy(child.gameObject);

            int count = b.Choices.Length;

            for (int i = 0; i < count; i++)
            {
                var choice = b.Choices[i];
                string targetId = choice.TargetId;
                IndoorDialogueChoiceStyle.AddButton(
                    _choicePanel.transform, $"Choice{i}", choice.Label, serifFont,
                    () => JumpToBeat(targetId));
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

            if (b.Id == "decision_resume_pause")
            {
                StartCoroutine(ResumeDecisionBgmAfterPause());
                return;
            }

            _dlgPanel.SetActive(true);
            SetPrompt(false);

            // Inspector entrance: audio timing is prepared before this beat is shown.
            if (b.ShowsInspector && !_inspectorVisible)
            {
                SetVillagersFacingInspector(true);
                _inspectorVisible = true;
                _inspImg.gameObject.SetActive(true);
                SetAlpha(_inspImg, 0f);
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
            _speakerTmp.alignment = TextAlignmentOptions.Left;
            SetItemIcons(b.ItemIconKeys);

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

            // SFX / BGM triggers keyed to beat IDs
            if (b.Id == "offer_sake")
                GameAudioManager.Instance.PlaySfxOnce(AudioId.DrinkPour);
            if (b.Id == "decision" || b.Id == "decision_start")
                PlayDecisionBgm();
            if (b.Id == "decision_stop")
                GameAudioManager.Instance.StopBgm(0.35f);

            if (b.Id == "inspire_moment")
                _textPlayer.Play(_bodyTmp, b.Text,
                    () => { _shimmerCR = StartCoroutine(ShimmerWordCR(_bodyTmp, "inspiration")); });
            else
                _textPlayer.Play(_bodyTmp, b.Text);
        }

        // ── Inspector fade in / out ───────────────────────────────────────────

        private IEnumerator PrepareInspectorEntrance(int inspectorBeatIndex)
        {
            _inputLock = true;
            yield return new WaitForSeconds(2f);

            const float bgmFadeOut = 0.35f;
            GameAudioManager.Instance.StopBgm(bgmFadeOut);
            yield return new WaitForSeconds(bgmFadeOut);

            GameAudioManager.Instance.PlaySfxOnce(AudioId.KnockingDoor);
            yield return new WaitForSeconds(2f);

            GameAudioManager.Instance.PlayBgm(AudioId.Crisis, fadeIn: 0.45f);
            ShowBeat(inspectorBeatIndex);
            _inputLock = false;
        }

        private void PlayDecisionBgm()
        {
            GameAudioManager.Instance.StopSfxLoop(AudioId.Wind, 0.35f);
            GameAudioManager.Instance.PlayBgm(AudioId.Decision, fadeIn: 0.45f);
        }

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

        private IEnumerator ResumeDecisionBgmAfterPause()
        {
            _inputLock = true;
            yield return new WaitForSeconds(1f);
            PlayDecisionBgm();
            AdvanceBeat();
            _inputLock = false;
        }

        // ── Transitions ───────────────────────────────────────────────────────

        private IEnumerator FadeAndLoad()
        {
            _inputLock = true;
            GameAudioManager.Instance.StopBgm(fadeDuration);
            GameAudioManager.Instance.StopSfxLoop(AudioId.Wind, fadeDuration);
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

        private void SetVillagersFacingInspector(bool facingInspector)
        {
            float direction = facingInspector ? 1f : -1f;
            SetHorizontalDirection(_jiroImg, direction);
            SetHorizontalDirection(_junkoImg, direction);
            SetHorizontalDirection(_yujiImg, direction);
        }

        private static void SetHorizontalDirection(Image image, float direction)
        {
            if (image == null) return;
            var scale = image.transform.localScale;
            scale.x = direction;
            image.transform.localScale = scale;
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
            var eventSystem = FindFirstObjectByType<EventSystem>();
            if (eventSystem == null)
                eventSystem = new GameObject("EventSystem").AddComponent<EventSystem>();

            if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
                eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
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
                38f, RinGreen, TextAlignmentOptions.Left,
                new Vector2(0.04f, 0.68f), new Vector2(0.81f, 0.96f));
            _speakerTmp.fontStyle = FontStyles.Bold;
            UseFont(_speakerTmp);

            _itemIconRoot = MakeRect(pt, "ItemIcons",
                new Vector2(0.11f, 0.36f), new Vector2(0.56f, 0.98f))
                .GetComponent<RectTransform>();
            _itemIconRoot.offsetMin = new Vector2(0f, 4f);
            _itemIconRoot.offsetMax = new Vector2(0f, -4f);

            var iconLayout = _itemIconRoot.gameObject.AddComponent<HorizontalLayoutGroup>();
            iconLayout.childAlignment = TextAnchor.MiddleLeft;
            iconLayout.childControlWidth = false;
            iconLayout.childControlHeight = false;
            iconLayout.childForceExpandWidth = false;
            iconLayout.childForceExpandHeight = false;
            iconLayout.spacing = 8f;
            _itemIconRoot.gameObject.SetActive(false);

            _bodyTmp = MakeTMP(pt, "Body", "",
                34f, BodyC, TextAlignmentOptions.Left,
                new Vector2(0.04f, 0.04f), new Vector2(0.81f, 0.65f));
            _bodyTmp.lineSpacing = 6f;
            _bodyTmp.raycastTarget = false;
            UseFont(_bodyTmp);

            _dlgPanel.SetActive(false);
        }

        private void BuildChoicePanel(Transform cv)
        {
            _choicePanel = MakeRect(cv, "ChoicePanel",
                new Vector2(0.25f, 0.32f), new Vector2(0.75f, 0.72f));
            IndoorDialogueChoiceStyle.ConfigureContainer(_choicePanel);
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

        // ── Beat helpers ──────────────────────────────────────────────────────

        private static Beat D(string speaker, bool thought, string text,
                               int[] unlocks = null, string id = null, string jump = null,
                               string[] itemIconKeys = null) => new()
        {
            Speaker              = speaker,
            IsThought            = thought,
            Text                 = text,
            UnlocksInspirations  = unlocks,
            Id                   = id,
            JumpToId             = jump,
            ItemIconKeys         = itemIconKeys,
        };

        private static Beat Br(string id, params BChoice[] choices) => new()
        {
            Id      = id,
            Choices = choices,
        };

        private static BChoice Ch(string label, string target) =>
            new() { Label = label, TargetId = target };

        private static string ItemWord(string word) =>
            $"<color={ItemHighlightColor}><u>{word}</u></color>";

        private void SetItemIcons(string[] iconKeys)
        {
            if (_itemIconRoot == null)
                return;

            int count = iconKeys?.Length ?? 0;
            _itemIconRoot.gameObject.SetActive(count > 0);
            SetBodyIconLayout(count > 0);

            for (int i = 0; i < count; i++)
            {
                Image iconImage = GetItemIconImage(i);
                iconImage.sprite = LoadItemIcon(iconKeys[i]);
                SetItemIconSize(iconImage, iconKeys[i]);
                iconImage.gameObject.SetActive(iconImage.sprite != null);
            }

            for (int i = count; i < _itemIconImages.Count; i++)
                _itemIconImages[i].gameObject.SetActive(false);
        }

        private void SetBodyIconLayout(bool hasIcons)
        {
            var bodyRect = _bodyTmp != null ? _bodyTmp.transform as RectTransform : null;
            if (bodyRect == null)
                return;

            bodyRect.anchorMax = hasIcons ? new Vector2(0.81f, 0.42f) : new Vector2(0.81f, 0.65f);
        }

        private Image GetItemIconImage(int index)
        {
            while (_itemIconImages.Count <= index)
            {
                var iconObject = new GameObject($"ItemIcon{_itemIconImages.Count + 1}",
                    typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                iconObject.transform.SetParent(_itemIconRoot, false);

                var iconRect = iconObject.GetComponent<RectTransform>();
                iconRect.sizeDelta = Vector2.one * ItemIconBaseSize;

                var iconImage = iconObject.GetComponent<Image>();
                iconImage.preserveAspect = true;
                iconImage.raycastTarget = false;
                _itemIconImages.Add(iconImage);
            }

            return _itemIconImages[index];
        }

        private static void SetItemIconSize(Image iconImage, string iconKey)
        {
            var iconRect = iconImage != null ? iconImage.transform as RectTransform : null;
            if (iconRect == null)
                return;

            float scale = IsSakeIcon(iconKey) ? SakeItemIconScale : DefaultItemIconScale;
            iconRect.sizeDelta = Vector2.one * (ItemIconBaseSize * scale);
        }

        private Sprite LoadItemIcon(string iconKey)
        {
            if (string.IsNullOrWhiteSpace(iconKey))
                return null;

            if (_itemIconCache.TryGetValue(iconKey, out Sprite cached))
                return cached;

            Sprite sprite = Resources.Load<Sprite>($"Exhibitions/Icons/{iconKey}");
            if (sprite == null)
                sprite = FindItemIconByName(iconKey);

            _itemIconCache[iconKey] = sprite;
            return sprite;
        }

        private static Sprite FindItemIconByName(string iconKey)
        {
            string normalizedKey = NormalizeIconName(iconKey);
            Sprite[] icons = Resources.LoadAll<Sprite>("Exhibitions/Icons");
            for (int i = 0; i < icons.Length; i++)
            {
                Sprite icon = icons[i];
                if (icon == null)
                    continue;

                string normalizedName = NormalizeIconName(icon.name);
                if (normalizedName == normalizedKey || normalizedName.StartsWith(normalizedKey))
                    return icon;
            }

            return null;
        }

        private static string NormalizeIconName(string value)
        {
            return string.IsNullOrEmpty(value)
                ? string.Empty
                : value.Replace(" ", string.Empty).ToLowerInvariant();
        }

        private static bool IsSakeIcon(string iconKey)
        {
            return NormalizeIconName(iconKey).StartsWith("sake");
        }

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
                D("Jiro",  false, $"The soul of this dish is the {ItemWord("shichimi")} powder I hand-grind and blend myself. It's a recreation of a recipe from hundreds of years ago.", id: "food_merge", itemIconKeys: new[] { "Shichimi" }),
                D("Rin",   false, "A centuries-old recipe? No wonder the flavor has so much depth."),

                // ── Sake ─────────────────────────────────────────────────────
                D("Yuji",  false, "How can you have good food without good booze! Come on, Rin, try my pride and joy.", id: "offer_sake"),
                D("Rin",   false, $"Is this... {ItemWord("sake")}?", itemIconKeys: new[] { "sake" }),
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
                D("Rin",   false, "However, it's a bit strange. Whether it was the food or the sake just now, I tasted a very similar flavor in both."),
                D("Yuji",  false, "Oh? You've got a sharp palate."),
                D("Yuji",  false, $"Whether it's my sake or his shichimi powder, we both added a kind of {ItemWord("herb")} unique to these mountains. Yep... this is the flavor of Otowa!", itemIconKeys: new[] { "herb" }),
                D("Rin",   true,  "(Mr. Jiro's shichimi powder, Mr. Yuji's gold medal sake, and the specialty herb...)"),
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
                D("Rin",   true,  "(Silence... Yuji is tightly clenching his fists, and Jiro has his head bowed without saying a word.)"),
                D("Junko", false, "Rin... I am truly very sorry. You came here full of expectations, and right after you got off the train, we dragged you into such a massive mess."),
                D("Junko", false, "Take the early train tomorrow and head back to the city. This mess shouldn't be yours to bear."),
                D("Rin",   false, "Thank you for thinking of me, Chief. But I'm not leaving."),
                D("Junko", false, "Eh?", id: "decision_start"),
                D("Rin",   false, "This inspector reminds me of the men who ruined my hometown."),
                D("Rin",   false, "The same suits, the same arrogance. They signed their contracts, cut down the trees, and built their factories."),
                D("Rin",   false, "And then the whole village was filled with the smell of burning. My parents took me away with them, but my grandmother stayed behind... and her health only kept getting worse."),
                D("Junko", false, "…I'm sorry to hear that, Rin. It must have been so hard — watching the place you grew up in change like that, and feeling there was nothing you could do."),
                D("Rin",   false, "So... I don't want to stand by and watch these company suits run wild again."),
                D("Rin",   false, "At the very least... I want to try to make the railway company take back this unreasonable decision."),
                D("Rin",   false, "I know it won't be easy. But as the acting stationmaster... I'm willing to try."),
                D("Junko", false, "...", id: "decision_stop"),
                new Beat { Id = "decision_resume_pause" },
                D("Yuji",  false, "Incredible, Rin. You coming here today is the luckiest thing that's ever happened to Otowa."),
                D("Jiro",  false, "Unbelievable... that someone from the city could have this heart..."),
                D("Rin",   false, "Since the inspector said there might be a turning point if we can prove the station's value, that means it's not time to give up entirely just yet."),
                D("Rin",   false, "In his letter, Mr. Hikaru said he wanted to transform the station into a museum that showcases Otowa's charm."),
                D("Rin",   false, "Even though he only collected a pile of cryptic odds and ends, he asked me to be the curator."),
                D("Rin",   false, "If I can exhibit these items properly and show passengers the charm of Otowa, wouldn't that make the company change its mind?"),
                D("Yuji",  false, "Turn the station into a museum? That guy Hikaru was actually doing something like that behind our backs?!"),
                D("Jiro",  false, "Hmph, with that kid's brain, he wouldn't be able to display anything decent anyway. But, if it's you..."),
                D("Yuji",  false, "Yeah! Rin! If even a picky guy like Jiro approves of your taste, you can definitely pull it off!"),
                D("Junko", false, "That's a truly wonderful idea, Rin."),
                D("Rin",   false, "Yeah. I'm starting to look forward to being a curator, actually."),
                D("Junko", false, "No matter what happens... on behalf of all the villagers in Otowa, I thank you."),
                D("Junko", false, "We will prepare properly for this year's Summer Festival... but for it to be held successfully, we are counting on you."),
                D("Rin",   true,  "(The Summer Festival... what kind of day is that? Why do they value it so much?)"),
                D("Rin",   false, "Anyway, I should explore for now."),
                D("Junko", false, "All right, Rin. We'll do everything we can to support you!"),
            };
        }
    }
}
