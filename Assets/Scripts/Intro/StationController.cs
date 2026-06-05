using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.EventSystems;
using TMPro;
using ExhibitionSystem.Data;
using Otowa.Audio;
using Otowa.IndoorDialogue;
using Otowa.SaveSystem;

namespace Otowa.Intro
{
    /// <summary>
    /// Scene 3 (Intro-3): stationmaster's office discovery and Hikaru's letter.
    ///
    /// Setup: empty scene → empty GameObject → attach this script.
    /// Inspector fields: nextSceneName = "SampleScene", assign serifFont + handwrittenFont.
    /// Sprites: leave blank to auto-load CharacterSheet for Rin; Inspector uses a
    /// colored-rectangle placeholder until a real sprite is assigned.
    /// </summary>
    public class StationController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Scene")]
        [SerializeField] private string nextSceneName = "SampleScene";
        [SerializeField] private float  typewriterSpeed = 35f;
        [SerializeField] private float  fadeDuration    = 0.35f;

        [Header("Fonts")]
        [SerializeField] private TMP_FontAsset serifFont;
        [SerializeField] private TMP_FontAsset handwrittenFont;

        [Header("Sprites (leave blank to auto-load)")]
        [SerializeField] private Sprite rinSprite;
        [SerializeField] private Sprite inspectorSprite;

        // ── Beat data ─────────────────────────────────────────────────────────

        private enum BeatKind { Narration, Letter, Inventory, Dialogue }

        private struct BChoice { public string Label; public string TargetId; }

        private struct Beat
        {
            public BeatKind Kind;
            public string   Speaker;
            public string   Text;
            public bool     IsThought;
            public int      LetterPage;
            public bool     HidesRightChar;
            public string   Id;
            public string   JumpToId;
            public BChoice[] Choices;
        }

        private List<Beat> _beats;
        private Dictionary<string, int> _beatIndex;
        private int  _current       = -1;
        private bool _choosingBranch;

        // ── State ─────────────────────────────────────────────────────────────

        private bool      _inputLock;
        private IndoorDialogueTextPlayer _textPlayer;
        private bool      _inspRevealed;
        private CanvasGroup _inspOverlayCG;

        // ── UI refs ───────────────────────────────────────────────────────────

        private CanvasGroup _fade;
        private Image       _bgImage;

        private GameObject _narPanel;
        private TMP_Text   _narText;

        private GameObject _letterPanel;
        private TMP_Text   _ltTitle;
        private TMP_Text   _ltBody;
        private TMP_Text   _ltPageNum;
        private TMP_Text   _ltPrevArrow;
        private TMP_Text   _ltNextArrow;

        private GameObject _invPanel;

        private GameObject _dlgPanel;
        private Image      _rinImg;
        private Image      _inspImg;
        private TMP_Text   _speakerTmp;
        private TMP_Text   _bodyTmp;

        private TMP_Text    _promptTmp;
        private GameObject  _choicePanel;
        // ── Colours ───────────────────────────────────────────────────────────

        private static readonly Color NarBg     = new Color32(0x2a, 0x22, 0x18, 0xFF);
        private static readonly Color LetterBg  = new Color32(0x2a, 0x22, 0x18, 0xFF);
        private static readonly Color DlgBg     = new Color32(0x07, 0x18, 0x20, 0xFF);
        private static readonly Color PanelBg   = new Color32(0x04, 0x10, 0x16, 0xEE);
        private static readonly Color Parchment = new Color32(0xc4, 0xb8, 0xa0, 0xFF);
        private static readonly Color LetterTxt = new Color32(0x3a, 0x35, 0x2e, 0xFF);
        private static readonly Color LetterHdr = new Color32(0x5a, 0x52, 0x48, 0xFF);
        private static readonly Color LetterArrowC = new Color32(0x5a, 0x52, 0x48, 0xFF);
        private static readonly Color LetterArrowDisabledC = new Color32(0x5a, 0x52, 0x48, 0x70);
        private static readonly Color RinGreen  = new Color32(0x78, 0xb5, 0xb2, 0xFF);
        private static readonly Color InspBlue  = new Color32(0xa0, 0xa8, 0xc0, 0xFF);
        private static readonly Color UnknownC  = new Color32(0x80, 0x80, 0x80, 0xFF);
        private static readonly Color BodyC     = new Color32(0xc8, 0xdc, 0xda, 0xFF);
        private static readonly Color NarrationC = Color.white;
        private static readonly Color ThoughtC  = new Color32(0x91, 0xaa, 0xa8, 0xFF);
        private static readonly Color PromptC   = new Color32(0xFF, 0xFF, 0xFF, 0xBB);

        private const float ActiveAlpha   = 1.00f;
        private const float InactiveAlpha = 0.38f;
        private bool _playLetterPageTurn;

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
            ApplyOpeningAudio();
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
            if (PauseMenuController.ShouldSuppressWorldAdvance) return;
            if (InspirationManager.IsJournalOpen) return;

            if (IsShowingLetter())
            {
                HandleLetterNavigationInput();
                return;
            }

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
            if (_current >= 0 && _beats[_current].HidesRightChar)
                StartCoroutine(FadeOutRightChar());

            if (_current >= 0 && !string.IsNullOrEmpty(_beats[_current].JumpToId))
            {
                if (_beatIndex.TryGetValue(_beats[_current].JumpToId, out int jumpIdx))
                { ShowBeat(jumpIdx); return; }
            }

            int next = _current + 1;
            if (next >= _beats.Count) { StartCoroutine(FadeAndLoad()); return; }
            _playLetterPageTurn = IsShowingLetter() && _beats[next].Kind == BeatKind.Letter;
            ShowBeat(next);
        }

        private void JumpToBeat(string targetId)
        {
            if (!_beatIndex.TryGetValue(targetId, out int idx))
            {
                Debug.LogWarning($"[StationController] Branch target '{targetId}' not found.");
                int fallback = _current + 1;
                if (fallback < _beats.Count) ShowBeat(fallback);
                return;
            }
            _choosingBranch = false;
            _choicePanel.SetActive(false);
            _dlgPanel.SetActive(true);
            ShowBeat(idx);
        }

        private void ShowChoices(Beat b)
        {
            _choosingBranch = true;
            _dlgPanel.SetActive(false);
            _choicePanel.SetActive(true);

            foreach (Transform child in _choicePanel.transform)
                Destroy(child.gameObject);

            int   count = b.Choices.Length;

            for (int i = 0; i < count; i++)
            {
                var choice = b.Choices[i];
                string targetId = choice.TargetId;
                IndoorDialogueChoiceStyle.AddButton(
                    _choicePanel.transform, $"Choice{i}", choice.Label, serifFont,
                    () => JumpToBeat(targetId));
            }
        }

        private IEnumerator FadeOutRightChar()
        {
            const float duration = 0.4f;
            float elapsed = 0f;
            float startAlpha = _inspImg.color.a;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                SetAlpha(_inspImg, Mathf.Lerp(startAlpha, 0f, Mathf.Clamp01(elapsed / duration)));
                yield return null;
            }
            _inspImg.gameObject.SetActive(false);
        }

        private void ShowBeat(int index)
        {
            _textPlayer.Cancel();
            ClearText(_narText);
            ClearText(_bodyTmp);

            _current = index;
            var b = _beats[index];

            if (b.Choices != null && b.Choices.Length > 0)
            {
                ShowChoices(b);
                return;
            }

            _narPanel.SetActive(false);
            _letterPanel.SetActive(false);
            _invPanel.SetActive(false);
            _dlgPanel.SetActive(false);
            _choicePanel.SetActive(false);
            SetPrompt(false);

            switch (b.Kind)
            {
                case BeatKind.Narration: ShowNarration(b); break;
                case BeatKind.Letter:    ShowLetter(b);    break;
                case BeatKind.Inventory: ShowInventory();  break;
                case BeatKind.Dialogue:  ShowDialogue(b);  break;
            }
        }

        // ── Narration ─────────────────────────────────────────────────────────

        private void ShowNarration(Beat b)
        {
            _bgImage.color     = NarBg;
            _narPanel.SetActive(true);
            _narText.color     = NarrationC;
            _narText.fontStyle = FontStyles.Normal;
            _textPlayer.Play(_narText, b.Text);
        }

        // ── Letter ────────────────────────────────────────────────────────────

        private void ShowLetter(Beat b)
        {
            _bgImage.color  = LetterBg;
            _letterPanel.SetActive(true);
            _ltTitle.text   = "A Letter from Hikaru";
            _ltBody.text    = b.Text;
            _ltPageNum.text = $"{b.LetterPage}  /  5";
            UpdateLetterArrows(b.LetterPage > 1);
            if (_playLetterPageTurn)
                GameAudioManager.Instance.PlaySfxOnce(AudioId.PageTurn);
            _playLetterPageTurn = false;
            SetPrompt(false);
        }

        private bool IsShowingLetter()
        {
            return _current >= 0
                   && _current < _beats.Count
                   && _beats[_current].Kind == BeatKind.Letter;
        }

        private void HandleLetterNavigationInput()
        {
            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                var position = mouse.position.ReadValue();
                if (position.x < Screen.width * 0.5f)
                    ShowPreviousLetterPage();
                else
                    AdvanceBeat();
                return;
            }

            var keyboard = Keyboard.current;
            if (keyboard != null
                && (keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame))
                AdvanceBeat();
        }

        private void ShowPreviousLetterPage()
        {
            if (!IsShowingLetter())
                return;

            var currentPage = _beats[_current].LetterPage;
            if (currentPage <= 1)
                return;

            var previous = _current - 1;
            if (previous < 0 || _beats[previous].Kind != BeatKind.Letter)
                return;

            _playLetterPageTurn = true;
            ShowBeat(previous);
        }

        private void UpdateLetterArrows(bool canGoBack)
        {
            if (_ltPrevArrow != null)
                _ltPrevArrow.color = canGoBack ? LetterArrowC : LetterArrowDisabledC;

            if (_ltNextArrow != null)
                _ltNextArrow.color = LetterArrowC;
        }

        // ── Inventory ─────────────────────────────────────────────────────────

        private void ShowInventory()
        {
            _bgImage.color = NarBg;
            _invPanel.SetActive(true);
            SetPrompt(true);
        }

        // ── Dialogue ─────────────────────────────────────────────────────────

        private void ShowDialogue(Beat b)
        {
            _bgImage.color = DlgBg;
            _dlgPanel.SetActive(true);

            bool isRin  = b.Speaker == "Rin";
            bool isInsp = b.Speaker == "Inspector" || b.Speaker == "???";

            _speakerTmp.text = b.Speaker ?? "";
            if (isRin)
            {
                _speakerTmp.color     = RinGreen;
                _speakerTmp.alignment = TextAlignmentOptions.Left;
            }
            else if (b.Speaker == "???")
            {
                _speakerTmp.color     = UnknownC;
                _speakerTmp.alignment = TextAlignmentOptions.Right;
            }
            else
            {
                _speakerTmp.color     = InspBlue;
                _speakerTmp.alignment = TextAlignmentOptions.Right;
            }

            SetAlpha(_rinImg,  isRin  ? ActiveAlpha : InactiveAlpha);
            SetAlpha(_inspImg, isInsp ? ActiveAlpha : InactiveAlpha);

            if (b.Speaker == "Inspector" && !_inspRevealed)
            {
                _inspRevealed = true;
                StartCoroutine(RevealInspector());
            }

            _bodyTmp.color     = b.IsThought ? ThoughtC : BodyC;
            _bodyTmp.fontStyle = b.IsThought ? FontStyles.Italic : FontStyles.Normal;

            SetPrompt(false);
            _textPlayer.Play(_bodyTmp, b.Text);
        }

        // ── Transitions ───────────────────────────────────────────────────────

        private IEnumerator FadeAndLoad()
        {
            _inputLock = true;
            _textPlayer.Cancel();
            SetPrompt(false);
            ClearText(_narText);
            ClearText(_ltTitle);
            ClearText(_ltBody);
            ClearText(_ltPageNum);
            ClearText(_speakerTmp);
            ClearText(_bodyTmp);
            yield return new WaitForSeconds(1.5f);
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

        private static void ClearText(TMP_Text text)
        {
            if (text == null)
                return;

            text.text = string.Empty;
            text.maxVisibleCharacters = 0;
        }

        private static void SetAlpha(Image img, float a)
        {
            if (img == null) return;
            var c = img.color; c.a = a; img.color = c;
        }

        // ── Sprite loading ────────────────────────────────────────────────────

        private void LoadSprites()
        {
            if (rinSprite == null)
            {
                var tex = Resources.Load<Texture2D>("Characters/CharacterSheet");
                if (tex != null)
                    rinSprite = Sprite.Create(tex,
                        new Rect(0, 0, tex.width * 0.5f, tex.height),
                        new Vector2(0.5f, 0f), 100f);
            }

            if (inspectorSprite == null)
            {
                // Placeholder: dark-suit silhouette (solid colour).
                // Replace by assigning a real sprite in the Inspector field.
                var tex = new Texture2D(1, 1);
                tex.SetPixel(0, 0, new Color32(0x28, 0x28, 0x44, 0xFF));
                tex.Apply();
                inspectorSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1),
                    new Vector2(0.5f, 0f), 100f);
            }
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
            var cvGo = new GameObject("StationCanvas",
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

            _bgImage = MakeImage(cvGo.transform, "BG", NarBg,
                Vector2.zero, Vector2.one);

            BuildNarPanel(cvGo.transform);
            BuildLetterPanel(cvGo.transform);
            BuildInventoryPanel(cvGo.transform);
            BuildDialoguePanel(cvGo.transform);
            BuildChoicePanel(cvGo.transform);

            _promptTmp = MakeTMP(cvGo.transform, "Prompt", string.Empty,
                22f, PromptC, TextAlignmentOptions.Center,
                new Vector2(0.30f, 0.035f), new Vector2(0.70f, 0.095f));
            _promptTmp.characterSpacing = 4f;
            UseFont(_promptTmp, serifFont);
            _promptTmp.gameObject.SetActive(false);
        }

        // ── Narration panel ───────────────────────────────────────────────────

        private void BuildNarPanel(Transform cv)
        {
            _narPanel = MakeRect(cv, "NarPanel", Vector2.zero, Vector2.one);

            _narText = MakeTMP(_narPanel.transform, "NarText", "",
                40f, NarrationC, TextAlignmentOptions.Center,
                new Vector2(0.18f, 0.32f), new Vector2(0.82f, 0.68f));
            _narText.lineSpacing = 8f;
            _narText.fontStyle   = FontStyles.Normal;
            UseFont(_narText, serifFont);

            _narPanel.SetActive(false);
        }

        // ── Letter panel ──────────────────────────────────────────────────────

        private void BuildLetterPanel(Transform cv)
        {
            _letterPanel = MakeRect(cv, "LetterPanel", Vector2.zero, Vector2.one);
            _letterPanel.AddComponent<Image>().color = LetterBg;

            // Parchment card
            var paper = MakeRect(_letterPanel.transform, "Paper",
                new Vector2(0.12f, 0.05f), new Vector2(0.88f, 0.95f));
            paper.AddComponent<Image>().color = Parchment;

            var pt = paper.transform;

            _ltTitle = MakeTMP(pt, "LtTitle", "A Letter from Hikaru",
                54f, LetterHdr, TextAlignmentOptions.Center,
                new Vector2(0.04f, 0.84f), new Vector2(0.96f, 0.98f));
            _ltTitle.fontStyle = FontStyles.Bold | FontStyles.Italic;
            UseFont(_ltTitle, handwrittenFont);

            // Thin separator line
            var sep = MakeRect(pt, "Sep",
                new Vector2(0.04f, 0.825f), new Vector2(0.96f, 0.835f));
            sep.AddComponent<Image>().color = new Color32(0x9a, 0x90, 0x80, 0xFF);

            _ltBody = MakeTMP(pt, "LtBody", "",
                37f, LetterTxt, TextAlignmentOptions.Left,
                new Vector2(0.06f, 0.10f), new Vector2(0.94f, 0.82f));
            _ltBody.lineSpacing = 6f;
            UseFont(_ltBody, handwrittenFont);

            _ltPageNum = MakeTMP(pt, "LtPage", "1  /  5",
                26f, LetterHdr, TextAlignmentOptions.Center,
                new Vector2(0.04f, 0.02f), new Vector2(0.96f, 0.09f));
            UseFont(_ltPageNum, serifFont);

            _ltPrevArrow = MakeTMP(_letterPanel.transform, "LetterPrevArrow", "<",
                82f, LetterArrowDisabledC, TextAlignmentOptions.Center,
                new Vector2(0.04f, 0.42f), new Vector2(0.12f, 0.58f));
            _ltPrevArrow.fontStyle = FontStyles.Bold | FontStyles.Italic;
            _ltPrevArrow.raycastTarget = false;
            UseFont(_ltPrevArrow, serifFont);
            AddArrowGlow(_ltPrevArrow);

            _ltNextArrow = MakeTMP(_letterPanel.transform, "LetterNextArrow", ">",
                82f, LetterArrowC, TextAlignmentOptions.Center,
                new Vector2(0.88f, 0.42f), new Vector2(0.96f, 0.58f));
            _ltNextArrow.fontStyle = FontStyles.Bold | FontStyles.Italic;
            _ltNextArrow.raycastTarget = false;
            UseFont(_ltNextArrow, serifFont);
            AddArrowGlow(_ltNextArrow);

            _letterPanel.SetActive(false);
        }

        private static void AddArrowGlow(TMP_Text arrow)
        {
            var outline = arrow.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color32(0xff, 0xf1, 0xce, 0x78);
            outline.effectDistance = new Vector2(2.2f, -2.2f);
            outline.useGraphicAlpha = true;
        }

        // ── Inventory panel ───────────────────────────────────────────────────

        private void BuildInventoryPanel(Transform cv)
        {
            _invPanel = MakeRect(cv, "InvPanel", Vector2.zero, Vector2.one);
            _invPanel.AddComponent<Image>().color = new Color(0, 0, 0, 0.85f);

            var card = MakeRect(_invPanel.transform, "Card",
                new Vector2(0.02f, 0.02f), new Vector2(0.98f, 0.98f));
            card.AddComponent<Image>().color = new Color32(0x06, 0x1a, 0x22, 0xFF);
            var ct = card.transform;

            var titleTmp = MakeTMP(ct, "InvTitle", "INVENTORY",
                42f, RinGreen, TextAlignmentOptions.Center,
                new Vector2(0.04f, 0.89f), new Vector2(0.96f, 0.98f));
            titleTmp.fontStyle = FontStyles.Bold;
            UseFont(titleTmp, serifFont);

            var subTmp = MakeTMP(ct, "InvSub", "Items found in the stationmaster's office",
                24f, BodyC, TextAlignmentOptions.Center,
                new Vector2(0.04f, 0.82f), new Vector2(0.96f, 0.90f));
            UseFont(subTmp, serifFont);

            // Load all 16 items sorted by sortOrder
            var rawItems = Resources.LoadAll<ExhibitItemData>("Exhibitions/Items");
            System.Array.Sort(rawItems, (a, b) => a.sortOrder.CompareTo(b.sortOrder));

            var gridGo = MakeRect(ct, "Grid",
                new Vector2(0.01f, 0.06f), new Vector2(0.99f, 0.82f));
            var grid = gridGo.AddComponent<GridLayoutGroup>();
            grid.constraint      = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 4;
            grid.cellSize        = new Vector2(200f, 150f);
            grid.spacing         = new Vector2(16f, 16f);
            grid.childAlignment  = TextAnchor.MiddleCenter;
            grid.padding         = new RectOffset(8, 8, 8, 8);

            for (int i = 0; i < 16; i++)
            {
                var itemData = (i < rawItems.Length) ? rawItems[i] : null;
                bool isLocked = itemData != null && i >= rawItems.Length - 2;

                var slot = new GameObject($"Slot{i}", typeof(RectTransform));
                slot.transform.SetParent(gridGo.transform, false);
                var rt = (RectTransform)slot.transform;
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot     = new Vector2(0.5f, 0.5f);

                slot.AddComponent<Image>().color = isLocked
                    ? new Color32(0x28, 0x28, 0x2a, 0xFF)
                    : new Color32(0x13, 0x38, 0x40, 0xFF);

                if (itemData != null && itemData.icon != null)
                {
                    var iconGo  = MakeRect(slot.transform, "Icon",
                        new Vector2(0.1f, 0.25f), new Vector2(0.9f, 0.95f));
                    var iconImg = iconGo.AddComponent<Image>();
                    iconImg.sprite         = itemData.icon;
                    iconImg.preserveAspect = true;
                    iconImg.raycastTarget  = false;
                    if (isLocked) iconImg.color = new Color(1f, 1f, 1f, 0.35f);

                    var nameTmp = MakeTMP(slot.transform, "Name", itemData.itemName,
                        14f, isLocked ? new Color32(0x88, 0x88, 0x88, 0xFF) : new Color32(0xa5, 0xd0, 0xcd, 0xFF),
                        TextAlignmentOptions.Center,
                        new Vector2(0.02f, 0.02f), new Vector2(0.98f, 0.24f));
                    UseFont(nameTmp, serifFont);
                }
                else
                {
                    var lbl = MakeTMP(slot.transform, "Lbl",
                        itemData != null ? itemData.itemName : "?",
                        itemData != null ? 18f : 34f,
                        isLocked ? new Color32(0x88, 0x88, 0x88, 0xFF) : new Color32(0x48, 0x82, 0x87, 0xFF),
                        TextAlignmentOptions.Center,
                        Vector2.zero, Vector2.one);
                    UseFont(lbl, serifFont);
                }
            }

            _invPanel.SetActive(false);
        }

        // ── Inspector unknown overlay ─────────────────────────────────────────

        private void BuildInspectorOverlay(Transform parent)
        {
            var go = MakeRect(parent, "InspOverlay", Vector2.zero, Vector2.one);
            go.transform.localScale = new Vector3(-1f, 1f, 1f); // undo parent's flip so text reads correctly
            _inspOverlayCG = go.AddComponent<CanvasGroup>();

            // 99% opaque black — inspector completely hidden
            var bg = go.AddComponent<Image>();
            bg.color = new Color32(0x00, 0x00, 0x00, 0xFC);

            // Giant white "?"
            var qmark = MakeTMP(go.transform, "QMark", "?",
                300f, new Color32(0xFF, 0xFF, 0xFF, 0xFF), TextAlignmentOptions.Center,
                new Vector2(0.0f, 0.1f), new Vector2(1.0f, 1.0f));
            UseFont(qmark, serifFont);

            // "???" label at the bottom
            var lbl = MakeTMP(go.transform, "UnknownLbl", "???",
                26f, new Color32(0xAA, 0xAA, 0xAA, 0xFF), TextAlignmentOptions.Center,
                new Vector2(0.05f, 0.02f), new Vector2(0.95f, 0.15f));
            UseFont(lbl, serifFont);
        }

        private IEnumerator RevealInspector()
        {
            const float duration = 0.6f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                _inspOverlayCG.alpha = 1f - Mathf.Clamp01(elapsed / duration);
                yield return null;
            }
            _inspOverlayCG.gameObject.SetActive(false);
        }

        // ── Choice panel ──────────────────────────────────────────────────────

        private void BuildChoicePanel(Transform cv)
        {
            _choicePanel = MakeRect(cv, "ChoicePanel", new Vector2(0.25f, 0.32f), new Vector2(0.75f, 0.72f));
            IndoorDialogueChoiceStyle.ConfigureContainer(_choicePanel);
            _choicePanel.SetActive(false);
        }

        // ── Dialogue panel ────────────────────────────────────────────────────

        private void BuildDialoguePanel(Transform cv)
        {
            _dlgPanel = MakeRect(cv, "DlgPanel", Vector2.zero, Vector2.one);
            var t = _dlgPanel.transform;

            // Rin — left
            var rinGo = MakeRect(t, "RinSprite",
                new Vector2(0.03f, 0.28f), new Vector2(0.38f, 0.98f));
            _rinImg = rinGo.AddComponent<Image>();
            _rinImg.sprite         = rinSprite;
            _rinImg.preserveAspect = true;
            _rinImg.color          = new Color(1, 1, 1, InactiveAlpha);
            _rinImg.raycastTarget  = false;

            // Inspector — right, flipped to face Rin
            var inspGo = MakeRect(t, "InspSprite",
                new Vector2(0.62f, 0.28f), new Vector2(0.97f, 0.98f));
            inspGo.transform.localScale = new Vector3(-1f, 1f, 1f);
            _inspImg = inspGo.AddComponent<Image>();
            _inspImg.sprite         = inspectorSprite;
            _inspImg.preserveAspect = true;
            _inspImg.color          = new Color(1, 1, 1, InactiveAlpha);
            _inspImg.raycastTarget  = false;

            // Unknown overlay — counter-flipped so text reads correctly
            BuildInspectorOverlay(inspGo.transform);

            // Bottom dialogue panel
            var panel = MakeRect(t, "Panel", Vector2.zero, new Vector2(1f, 0.28f));
            panel.AddComponent<Image>().color = PanelBg;
            var pt = panel.transform;

            _speakerTmp = MakeTMP(pt, "Speaker", "",
                36f, RinGreen, TextAlignmentOptions.Left,
                new Vector2(0.04f, 0.68f), new Vector2(0.81f, 0.96f));
            _speakerTmp.fontStyle = FontStyles.Bold;
            UseFont(_speakerTmp, serifFont);

            _bodyTmp = MakeTMP(pt, "Body", "",
                31f, BodyC, TextAlignmentOptions.Left,
                new Vector2(0.04f, 0.04f), new Vector2(0.81f, 0.65f));
            _bodyTmp.lineSpacing = 6f;
            UseFont(_bodyTmp, serifFont);

            _dlgPanel.SetActive(false);
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
            rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return img;
        }

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

        private void UseFont(TMP_Text tmp, TMP_FontAsset font)
        {
            if (font != null) tmp.font = font;
        }

        // ── Audio ─────────────────────────────────────────────────────────────

        private static void ApplyOpeningAudio()
        {
            var audio = GameAudioManager.Instance;
            audio.StopSfxLoop(AudioId.ForestAtmosphere, 0.25f);
            audio.PlayBgm(AudioId.DayWalk, fadeIn: 0.35f);
            audio.PlaySfxOnce(AudioId.DoorOpen);
        }

        // ── Beat helpers ──────────────────────────────────────────────────────

        private static Beat N(bool thought, string text, string id = null, string jump = null) => new()
            { Kind = BeatKind.Narration, Text = text, IsThought = thought, Id = id, JumpToId = jump };

        private static Beat L(int page, string text) => new()
            { Kind = BeatKind.Letter, LetterPage = page, Text = text };

        private static Beat D(string speaker, bool thought, string text,
                               string id = null, string jump = null) => new()
            { Kind = BeatKind.Dialogue, Speaker = speaker, IsThought = thought, Text = text,
              Id = id, JumpToId = jump };

        private static Beat B(string id, params BChoice[] choices) => new()
            { Kind = BeatKind.Dialogue, Id = id, Choices = choices };

        private static BChoice C(string label, string target) =>
            new() { Label = label, TargetId = target };

        // ── Beat list ─────────────────────────────────────────────────────────

        private void BuildBeats()
        {
            _beats = new List<Beat>
            {
                // ── Phase 1: stationmaster's office thoughts ──────────────────
                N(false, "Is this the stationmaster's office... It's way too messy."),
                N(false, "Wooden boards, boxes, and this pile of weird stuff..."),
                N(false, "Binoculars... stones... why is there even a jug of liquor?"),
                N(false, "There's a letter??"),
                N(false, "\"Hi Rin,\" looks like it's for me."),

                // ── Phase 2: Hikaru's letter (5 pages) ───────────────────────
                L(1,
                    "Hi Rin, welcome to Otowa Station!\n\n" +
                    "By the time you read this letter, I should already be on a train heading to the city.\n\n" +
                    "I am so sorry. Even though I'm the one who hired you, I couldn't welcome you in person."),

                L(2,
                    "You asked me on the phone what the \"challenge\" in the job was. " +
                    "Actually, a while ago, an inspector from the railway company came for an evaluation and criticized us heavily.\n\n" +
                    "They said the station was dilapidated, not attractive at all, and demanded that we rectify the situation as soon as possible.\n\n" +
                    "I racked my brains for days and finally came up with a brilliant idea — " +
                    "I'm going to turn Otowa Station into a mini-museum!"),

                L(3,
                    "If we just show everyone the unique charm of Otowa, it will definitely make the inspector " +
                    "look at us in a new light!\n\n" +
                    "These things in the room are all \"treasures\" I've painstakingly gathered from the village recently.\n\n" +
                    "I've already set them all up. But... for some reason, it just doesn't feel quite right."),

                L(4,
                    "There are lots of museums in the big city, right? You're from the big city, so you must have " +
                    "seen a lot of beautiful exhibitions.\n\n" +
                    "So I am sincerely inviting you to be the \"curator\" of Otowa Station. " +
                    "I believe that with your eye, you can definitely make these treasures shine!"),

                L(5,
                    "I had originally planned to meet up with you today and tackle this together, " +
                    "but just now, I suddenly realized there was something extremely important.\n\n" +
                    "It's something I have to go and handle personally, and I can't delay for even a moment.\n\n" +
                    "So, I'm temporarily leaving the station in your hands for the next few days! " +
                    "You can study the exhibits in the room first, and I'll be back soon!\n\n" +
                    "Hikaru."),

                // ── Phase 3: post-letter thoughts ─────────────────────────────
                N(false, "...A \"brilliant idea,\" huh."),
                N(false, "Piling all these things haphazardly on the floor is called hoarding, not curating, Stationmaster Hikaru."),
                N(false, "So the so-called \"challenge\" is just cleaning up this stationmaster's mess."),
                N(false, "Still, turning the station into a little museum is an interesting idea. Let me think about how to approach it..."),
            };
        }
    }
}
