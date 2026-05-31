using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using TMPro;
using ExhibitionSystem.Data;

/// <summary>
/// Persistent singleton — survives scene loads via DontDestroyOnLoad.
///
/// Tracks inspirations (16), collected items (16), and completed themes.
/// Shows toast pop-ups at top of screen on inspiration unlock, and lets the
/// player toggle a full journal overlay with the [E] key.
///
/// Journal has three tabs:  Items | Inspirations | Themes
///
/// Auto-creates itself on first access — no prefab or scene setup needed.
///
/// Usage:
///   InspirationManager.Instance.Unlock(id)             // 1-based inspiration ID
///   InspirationManager.Instance.CollectItem(sortOrder) // 1-based item sort order
///   InspirationManager.Instance.CollectAllItems()      // mark all 16 collected
///   InspirationManager.Instance.CompleteTheme(title)   // mark a theme complete
///   InspirationManager.Instance.SetFont(serifFont)     // optional font match
///   InspirationManager.IsJournalOpen                   // true while overlay is up
/// </summary>
public class InspirationManager : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────────────────

    private static InspirationManager _instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void EnsureInitialized()
    {
        _ = Instance;
    }

    public static InspirationManager Instance
    {
        get
        {
            if (_instance != null) return _instance;
            var go = new GameObject("InspirationManager");
            _instance = go.AddComponent<InspirationManager>();
            return _instance;
        }
    }

    /// <summary>True while the journal overlay is open.</summary>
    public static bool IsJournalOpen => _instance != null && _instance._journalOpen;

    // ── Inspiration text ──────────────────────────────────────────────────────

    private static readonly string[] Texts = new string[17]
    {
        "",
        "Rare creatures dwell within the forests of Otowa.",
        "Wherever Rintaro goes, this is never far behind.",
        "A professor retired to Otowa to savor the quiet life.",
        "The color of the water, the color of the birds, the color of Otowa.",
        "A music boy left Otowa after a bitter quarrel with his father.",
        "Octopus traps, fleeting dreams under the summer moon.",
        "Legend speaks of an indigenous Otowa belief in an avian deity.",
        "When it blossoms in the sky, it marks the most beautiful night of summer.",
        "Bye Bye, my Otowa town.",
        "The source of Otowa's signature flavor, found in sake and local cuisine.",
        "A mysterious recipe dating back centuries.",
        "It won Otowa a gold medal at the regional specialty competition over a decade ago.",
        "A blessing from Otowa: health and peace.",
        "The healing properties of Otowa's hot springs.",
        "A father's silent love.",
        "On that day, all wandering souls journey back to Otowa.",
    };

    // ── Game data (loaded from Resources) ────────────────────────────────────

    private ExhibitItemData[]  _itemDataByOrder = new ExhibitItemData[17]; // 1-based by sortOrder
    private ExhibitionTheme[]  _themeData;

    // ── State ─────────────────────────────────────────────────────────────────

    private readonly bool[]     _unlocked       = new bool[17]; // inspirations, 1-based
    private readonly bool[]     _itemsCollected = new bool[17]; // items, 1-based by sortOrder
    private          bool[]     _themesCompleted;               // themes, 0-based

    private readonly TMP_Text[] _entries = new TMP_Text[17];    // inspiration journal rows, 1-based

    private bool _journalOpen;
    private bool _introduced;

    // ── UI refs ───────────────────────────────────────────────────────────────

    private Canvas _canvas;

    private GameObject  _popupGo;
    private CanvasGroup _popupCG;
    private TMP_Text    _popupTitle;
    private TMP_Text    _popupBody;

    private GameObject  _hintGo;
    private CanvasGroup _hintCG;

    private GameObject _journalGo;
    private GameObject _journalEntryGo;

    private readonly Queue<int> _toastQueue = new();
    private bool _toastActive;

    private TMP_FontAsset _font;

    // ── Journal tab refs ──────────────────────────────────────────────────────

    private int         _activeTab = 1; // 0=Items, 1=Inspirations, 2=Themes
    private GameObject  _itemsPanel;
    private GameObject  _inspPanel;
    private GameObject  _themesPanel;

    private readonly TMP_Text[]       _tabTexts = new TMP_Text[3];
    private readonly Image[]          _tabBgs   = new Image[3];
    private readonly RectTransform[]  _tabRects = new RectTransform[3];

    // Per-slot refs for dynamic refresh
    private readonly Image[]    _itemSlotImages = new Image[17];    // 1-based
    private readonly Image[]    _itemSlotBgs    = new Image[17];    // 1-based
    private readonly TMP_Text[] _itemSlotNames  = new TMP_Text[17]; // 1-based
    private readonly Image[]    _entryBgs       = new Image[17];    // inspiration rows, 1-based

    private TMP_Text[] _themeEntryTitles;
    private TMP_Text[] _themeEntryStatuses;
    private Image[]    _themeEntryBgs;

    // ── Colours ───────────────────────────────────────────────────────────────

    private static readonly Color PopupBg       = new Color32(0x4b, 0x2f, 0x20, 0xF2);
    private static readonly Color PopupLine     = new Color32(0xc9, 0x9b, 0x65, 0xFF);
    private static readonly Color PopupHdr      = new Color32(0xf0, 0xd7, 0xa5, 0xFF);
    private static readonly Color PopupBody     = new Color32(0xf7, 0xea, 0xc9, 0xFF);
    private static readonly Color HintBg        = new Color32(0x55, 0x36, 0x24, 0xEE);
    private static readonly Color HintFg        = new Color32(0xf0, 0xd7, 0xa5, 0xFF);
    private static readonly Color JournalBg     = new Color32(0x24, 0x16, 0x10, 0xF2);
    private static readonly Color BookCover     = new Color32(0x68, 0x42, 0x2b, 0xFF);
    private static readonly Color PageEdge      = new Color32(0xbf, 0x98, 0x64, 0xFF);
    private static readonly Color PageLeft      = new Color32(0xf3, 0xe3, 0xbd, 0xFF);
    private static readonly Color PageRight     = new Color32(0xee, 0xdc, 0xb4, 0xFF);
    private static readonly Color Spine         = new Color32(0x8f, 0x68, 0x43, 0xFF);
    private static readonly Color JournalHdr    = new Color32(0x6d, 0x43, 0x2d, 0xFF);
    private static readonly Color SepLine       = new Color32(0xb7, 0x86, 0x58, 0xFF);
    private static readonly Color CloseFg       = new Color32(0x7b, 0x5b, 0x42, 0xFF);
    private static readonly Color UnlockedFg    = new Color32(0x54, 0x3b, 0x2a, 0xFF);
    private static readonly Color LockedFg      = new Color32(0x9b, 0x84, 0x68, 0xFF);
    private static readonly Color TabActiveBg   = new Color32(0xe9, 0xce, 0x9c, 0xFF);
    private static readonly Color TabInactiveBg = new Color32(0xb7, 0xa0, 0x6c, 0xFF);
    private static readonly Color ItemHaveBg    = new Color32(0xe3, 0xc9, 0x97, 0xFF);
    private static readonly Color ItemLackBg    = new Color32(0xc5, 0xad, 0x86, 0xFF);
    private static readonly Color ThemeHaveBg   = new Color32(0xe6, 0xd0, 0xa5, 0xFF);
    private static readonly Color ThemeLackBg   = new Color32(0xcf, 0xba, 0x95, 0xFF);
    private static readonly Color CompletedFg   = new Color32(0xa1, 0x5e, 0x3c, 0xFF);
    private static readonly Color EntryBg       = new Color32(0x78, 0x50, 0x34, 0xE8);

    // ── Unity lifecycle ───────────────────────────────────────────────────────

    private void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(gameObject); return; }
        _instance = this;
        DontDestroyOnLoad(gameObject);
        LoadGameData();
        BuildUI();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void Update()
    {
        var kb = Keyboard.current;
        if (kb != null && kb.eKey.wasPressedThisFrame)
            ToggleJournal();

        if (_journalOpen)
        {
            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
                CheckTabClick(mouse.position.ReadValue());
        }
    }

    private void CheckTabClick(Vector2 screenPos)
    {
        for (int i = 0; i < 3; i++)
        {
            if (_tabRects[i] != null &&
                RectTransformUtility.RectangleContainsScreenPoint(_tabRects[i], screenPos, null))
            {
                SwitchTab(i);
                return;
            }
        }
    }

    // ── Data loading ──────────────────────────────────────────────────────────

    private void LoadGameData()
    {
        var rawItems = Resources.LoadAll<ExhibitItemData>("Exhibitions/Items");
        foreach (var item in rawItems)
            if (item != null && item.sortOrder >= 1 && item.sortOrder <= 16)
                _itemDataByOrder[item.sortOrder] = item;

        var rawThemes = Resources.LoadAll<ExhibitionTheme>("Exhibitions/Themes");
        System.Array.Sort(rawThemes, (a, b) =>
            (a != null ? a.day : 0).CompareTo(b != null ? b.day : 0));
        _themeData       = rawThemes;
        _themesCompleted = new bool[rawThemes.Length];
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Unlock inspiration id (1–16).
    /// Safe to call if already unlocked — it's a no-op.
    /// Queues a toast; the first-ever unlock also shows the E-key hint.
    /// </summary>
    public void Unlock(int id)
    {
        if (id < 1 || id > 16 || _unlocked[id]) return;
        _unlocked[id] = true;
        RefreshEntry(id);

        bool firstEver = !_introduced;
        _introduced = true;

        _toastQueue.Enqueue(id);
        if (!_toastActive)
            StartCoroutine(ProcessToastQueue(firstEver));
    }

    /// <summary>Mark item with the given sortOrder (1–16) as collected. No-op if already collected.</summary>
    public void CollectItem(int sortOrder)
    {
        if (sortOrder < 1 || sortOrder > 16 || _itemsCollected[sortOrder]) return;
        _itemsCollected[sortOrder] = true;
        RefreshItemSlot(sortOrder);
    }

    /// <summary>Mark all 16 items as collected at once.</summary>
    public void CollectAllItems()
    {
        for (int i = 1; i <= 16; i++)
            CollectItem(i);
    }

    /// <summary>Mark the theme with the given title as completed.</summary>
    public void CompleteTheme(string themeTitle)
    {
        if (_themeData == null) return;
        for (int i = 0; i < _themeData.Length; i++)
        {
            if (_themeData[i] != null && _themeData[i].title == themeTitle)
            {
                if (_themesCompleted[i]) return;
                _themesCompleted[i] = true;
                RefreshThemeEntry(i);
                break;
            }
        }
    }

    /// <summary>Returns true if inspiration id has been unlocked.</summary>
    public bool IsUnlocked(int id) => id >= 1 && id <= 16 && _unlocked[id];

    /// <summary>Number of inspirations currently unlocked.</summary>
    public int UnlockedCount
    {
        get
        {
            int n = 0;
            for (int i = 1; i <= 16; i++) if (_unlocked[i]) n++;
            return n;
        }
    }

    /// <summary>
    /// Pass the scene's TMP font so the journal/toasts match the rest of the UI.
    /// Call once from your scene controller's Awake().
    /// </summary>
    public void SetFont(TMP_FontAsset font)
    {
        if (font == null || font == _font) return;
        _font = font;
        foreach (var tmp in GetComponentsInChildren<TMP_Text>(true))
            tmp.font = font;
    }

    // ── Journal toggle ────────────────────────────────────────────────────────

    private void ToggleJournal()
    {
        SetJournalOpen(!_journalOpen);
    }

    private void SetJournalOpen(bool open)
    {
        _journalOpen = open;
        _journalGo.SetActive(_journalOpen);
        RefreshJournalEntryVisibility(SceneManager.GetActiveScene().name);
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (_journalOpen)
            SetJournalOpen(false);
        RefreshJournalEntryVisibility(scene.name);
    }

    private void RefreshJournalEntryVisibility(string sceneName)
    {
        if (_journalEntryGo == null) return;
        bool isWorldMap = sceneName == "WorldScene" || sceneName == "Day1World";
        _journalEntryGo.SetActive(isWorldMap && !_journalOpen);
    }

    // ── Tab switching ─────────────────────────────────────────────────────────

    private void SwitchTab(int tab)
    {
        _activeTab = tab;
        _itemsPanel.SetActive(tab == 0);
        _inspPanel.SetActive(tab == 1);
        _themesPanel.SetActive(tab == 2);

        for (int i = 0; i < 3; i++)
        {
            bool active = (i == tab);
            if (_tabBgs[i]   != null) _tabBgs[i].color  = active ? TabActiveBg  : TabInactiveBg;
            if (_tabTexts[i] != null) _tabTexts[i].color = active ? JournalHdr   : CloseFg;
        }
    }

    // ── Toast pipeline ────────────────────────────────────────────────────────

    private IEnumerator ProcessToastQueue(bool showHintAfterFirst)
    {
        _toastActive = true;
        bool hintPending = showHintAfterFirst;

        while (_toastQueue.Count > 0)
        {
            int id = _toastQueue.Dequeue();
            _popupTitle.text = "✦  Inspiration Unlocked  ✦";
            _popupBody.text  = $"<b>{id:D2}.</b>  {Texts[id]}";
            yield return StartCoroutine(RunToast(_popupGo, _popupCG, 0.30f, 2.50f, 0.50f));

            if (hintPending)
            {
                hintPending = false;
                yield return StartCoroutine(RunToast(_hintGo, _hintCG, 0.25f, 2.00f, 0.40f));
            }
        }

        _toastActive = false;
    }

    private IEnumerator RunToast(GameObject go, CanvasGroup cg,
                                  float fadeIn, float hold, float fadeOut)
    {
        go.SetActive(true);
        yield return Fade(cg, 0f, 1f, fadeIn);
        yield return new WaitForSeconds(hold);
        yield return Fade(cg, 1f, 0f, fadeOut);
        go.SetActive(false);
    }

    private IEnumerator Fade(CanvasGroup cg, float from, float to, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            cg.alpha = Mathf.Lerp(from, to, t / duration);
            t += Time.deltaTime;
            yield return null;
        }
        cg.alpha = to;
    }

    // ── Entry refresh ─────────────────────────────────────────────────────────

    private void RefreshEntry(int id)
    {
        var e = _entries[id];
        if (e == null) return;
        e.text      = $"<b>{id:D2}.</b>  {Texts[id]}";
        e.color     = UnlockedFg;
        e.fontStyle = FontStyles.Normal;
        if (_entryBgs[id] != null) _entryBgs[id].color = ThemeHaveBg;
    }

    private void RefreshItemSlot(int sortOrder)
    {
        if (sortOrder < 1 || sortOrder > 16) return;
        if (_itemSlotBgs[sortOrder]    != null) _itemSlotBgs[sortOrder].color    = ItemHaveBg;
        if (_itemSlotImages[sortOrder] != null) _itemSlotImages[sortOrder].color = Color.white;
        if (_itemSlotNames[sortOrder]  != null) _itemSlotNames[sortOrder].color  = UnlockedFg;
    }

    private void RefreshThemeEntry(int index)
    {
        if (_themeEntryBgs != null && index < _themeEntryBgs.Length && _themeEntryBgs[index] != null)
            _themeEntryBgs[index].color = ThemeHaveBg;

        if (_themeEntryTitles != null && index < _themeEntryTitles.Length && _themeEntryTitles[index] != null)
        {
            _themeEntryTitles[index].color     = UnlockedFg;
            _themeEntryTitles[index].fontStyle = FontStyles.Normal;
        }

        if (_themeEntryStatuses != null && index < _themeEntryStatuses.Length && _themeEntryStatuses[index] != null)
        {
            _themeEntryStatuses[index].text  = "✓ Complete";
            _themeEntryStatuses[index].color = CompletedFg;
        }
    }

    // ── UI construction ───────────────────────────────────────────────────────

    private void BuildUI()
    {
        var cvGo = new GameObject("InspirationCanvas",
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        cvGo.transform.SetParent(transform, false);

        _canvas = cvGo.GetComponent<Canvas>();
        _canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 200;

        var scaler = cvGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode     = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight  = 0.5f;

        BuildToastPopup(cvGo.transform);
        BuildHintBanner(cvGo.transform);
        BuildJournalEntryButton(cvGo.transform);
        BuildJournal(cvGo.transform);
        RefreshJournalEntryVisibility(SceneManager.GetActiveScene().name);
    }

    // ── Toast popup ───────────────────────────────────────────────────────────

    private void BuildToastPopup(Transform cv)
    {
        _popupGo = Rect(cv, "Toast",
            new Vector2(0.28f, 0.84f), new Vector2(0.72f, 0.97f));
        _popupGo.AddComponent<Image>().color = PopupBg;

        Rect(_popupGo.transform, "Line",
            new Vector2(0.02f, 0.90f), new Vector2(0.98f, 0.912f))
            .AddComponent<Image>().color = PopupLine;

        _popupCG = _popupGo.AddComponent<CanvasGroup>();
        _popupCG.blocksRaycasts = false;

        _popupTitle = Tmp(_popupGo.transform, "Title", "",
            17f, PopupHdr, TextAlignmentOptions.Center,
            new Vector2(0.03f, 0.70f), new Vector2(0.97f, 0.93f));

        _popupBody = Tmp(_popupGo.transform, "Body", "",
            21f, PopupBody, TextAlignmentOptions.Center,
            new Vector2(0.03f, 0.05f), new Vector2(0.97f, 0.68f));
        _popupBody.lineSpacing = 4f;

        _popupGo.SetActive(false);
    }

    // ── E-key hint ────────────────────────────────────────────────────────────

    private void BuildHintBanner(Transform cv)
    {
        _hintGo = Rect(cv, "Hint",
            new Vector2(0.32f, 0.78f), new Vector2(0.68f, 0.85f));
        _hintGo.AddComponent<Image>().color = HintBg;

        _hintCG = _hintGo.AddComponent<CanvasGroup>();
        _hintCG.blocksRaycasts = false;

        Tmp(_hintGo.transform, "HintText", "Press  [ E ]  to open your Journal",
            19f, HintFg, TextAlignmentOptions.Center,
            new Vector2(0.03f, 0.10f), new Vector2(0.97f, 0.90f));

        _hintGo.SetActive(false);
    }

    // ── World-map entry button ────────────────────────────────────────────────

    private void BuildJournalEntryButton(Transform cv)
    {
        _journalEntryGo = Rect(cv, "JournalEntry",
            new Vector2(0.90f, 0.82f), new Vector2(0.985f, 0.975f));

        var background = _journalEntryGo.AddComponent<Image>();
        background.color = EntryBg;

        var button = _journalEntryGo.AddComponent<Button>();
        button.targetGraphic = background;
        button.onClick.AddListener(() =>
        {
            _introduced = true;
            SetJournalOpen(true);
        });

        var iconGo = Rect(_journalEntryGo.transform, "Icon",
            new Vector2(0.08f, 0.20f), new Vector2(0.92f, 0.95f));
        var icon = iconGo.AddComponent<Image>();
        icon.sprite = LoadJournalIcon();
        icon.preserveAspect = true;
        icon.raycastTarget = false;

        Tmp(_journalEntryGo.transform, "Label", "Journal",
            15f, PopupBody, TextAlignmentOptions.Center,
            new Vector2(0.04f, 0.02f), new Vector2(0.96f, 0.24f));
    }

    private static Sprite LoadJournalIcon()
    {
        var sprites = Resources.LoadAll<Sprite>("Map/journal");
        if (sprites.Length > 0) return sprites[0];

        var texture = Resources.Load<Texture2D>("Map/journal");
        return texture != null
            ? Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f))
            : null;
    }

    // ── Journal overlay ───────────────────────────────────────────────────────

    private void BuildJournal(Transform cv)
    {
        _journalGo = Rect(cv, "Journal", Vector2.zero, Vector2.one);
        _journalGo.AddComponent<Image>().color = JournalBg;

        var book = Rect(_journalGo.transform, "BookCover",
            new Vector2(0.035f, 0.035f), new Vector2(0.965f, 0.965f));
        book.AddComponent<Image>().color = BookCover;

        var spread = Rect(book.transform, "PageSpread",
            new Vector2(0.025f, 0.045f), new Vector2(0.975f, 0.90f));
        spread.AddComponent<Image>().color = PageEdge;
        var ct = spread.transform;

        Rect(ct, "LeftPage", new Vector2(0.008f, 0.012f), new Vector2(0.495f, 0.988f))
            .AddComponent<Image>().color = PageLeft;
        Rect(ct, "RightPage", new Vector2(0.505f, 0.012f), new Vector2(0.992f, 0.988f))
            .AddComponent<Image>().color = PageRight;
        Rect(ct, "Spine", new Vector2(0.493f, 0.012f), new Vector2(0.507f, 0.988f))
            .AddComponent<Image>().color = Spine;

        Tmp(ct, "Title", "Rin's Journal",
            28f, JournalHdr, TextAlignmentOptions.Center,
            new Vector2(0.05f, 0.90f), new Vector2(0.47f, 0.975f));

        Tmp(ct, "CloseHint", "[ E ]  close",
            16f, CloseFg, TextAlignmentOptions.Right,
            new Vector2(0.80f, 0.91f), new Vector2(0.93f, 0.975f));

        var closeGo = Rect(ct, "CloseButton",
            new Vector2(0.94f, 0.91f), new Vector2(0.975f, 0.975f));
        var closeBg = closeGo.AddComponent<Image>();
        closeBg.color = TabActiveBg;
        var closeButton = closeGo.AddComponent<Button>();
        closeButton.targetGraphic = closeBg;
        closeButton.onClick.AddListener(() => SetJournalOpen(false));
        Tmp(closeGo.transform, "X", "X",
            20f, JournalHdr, TextAlignmentOptions.Center,
            new Vector2(0f, 0f), new Vector2(1f, 1f));

        Rect(ct, "Sep", new Vector2(0.04f, 0.885f), new Vector2(0.96f, 0.893f))
            .AddComponent<Image>().color = SepLine;

        BuildTabBar(book.transform);

        _itemsPanel  = Rect(ct, "ItemsPanel",  new Vector2(0.025f, 0.035f), new Vector2(0.975f, 0.86f));
        _inspPanel   = Rect(ct, "InspPanel",   new Vector2(0.025f, 0.035f), new Vector2(0.975f, 0.86f));
        _themesPanel = Rect(ct, "ThemesPanel", new Vector2(0.025f, 0.035f), new Vector2(0.975f, 0.86f));

        BuildItemsContent(_itemsPanel.transform);
        BuildInspirationsContent(_inspPanel.transform);
        BuildThemesContent(_themesPanel.transform);

        SwitchTab(1); // default to Inspirations
        _journalGo.SetActive(false);
    }

    // ── Tab bar ───────────────────────────────────────────────────────────────

    private void BuildTabBar(Transform ct)
    {
        var tabBar = Rect(ct, "TabBar", new Vector2(0.045f, 0.895f), new Vector2(0.61f, 0.99f));
        string[] tabNames = { "Items", "Inspirations", "Themes" };

        for (int i = 0; i < 3; i++)
        {
            float xMin = i / 3f;
            float xMax = (i + 1) / 3f;

            var tabGo      = Rect(tabBar.transform, $"Tab{i}", new Vector2(xMin, 0f), new Vector2(xMax, 1f));
            _tabBgs[i]     = tabGo.AddComponent<Image>();
            _tabRects[i]   = (RectTransform)tabGo.transform;

            _tabTexts[i] = Tmp(tabGo.transform, $"TabText{i}", tabNames[i],
                18f, CloseFg, TextAlignmentOptions.Center,
                new Vector2(0.05f, 0.1f), new Vector2(0.95f, 0.9f));
        }
    }

    // ── Items tab content ─────────────────────────────────────────────────────

    private void BuildItemsContent(Transform parent)
    {
        const int cols = 4;
        const int rows = 4;
        const float rowH = 1f / rows;
        const float gapX = 0.004f;
        const float gapY = 0.006f;

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                int sortOrder = row * cols + col + 1; // 1-based
                if (sortOrder > 16) break;

                int page = col / 2;
                int pageCol = col % 2;
                float pageMin = page == 0 ? 0f : 0.515f;
                float pageMax = page == 0 ? 0.485f : 1f;
                float pageWidth = pageMax - pageMin;
                float xMin = pageMin + pageCol * pageWidth / 2f + gapX;
                float xMax = pageMin + (pageCol + 1) * pageWidth / 2f - gapX;
                float yMax = 1f - row * rowH;
                float yMin = 1f - (row + 1) * rowH + gapY;

                var item      = _itemDataByOrder[sortOrder];
                bool collected = _itemsCollected[sortOrder];

                var slotGo = Rect(parent, $"ItemSlot{sortOrder}", new Vector2(xMin, yMin), new Vector2(xMax, yMax));
                var slotBg = slotGo.AddComponent<Image>();
                slotBg.color              = collected ? ItemHaveBg : ItemLackBg;
                _itemSlotBgs[sortOrder]   = slotBg;

                if (item != null && item.icon != null)
                {
                    var iconGo  = Rect(slotGo.transform, "Icon",
                        new Vector2(0.1f, 0.28f), new Vector2(0.9f, 0.97f));
                    var iconImg = iconGo.AddComponent<Image>();
                    iconImg.sprite         = item.icon;
                    iconImg.preserveAspect = true;
                    iconImg.raycastTarget  = false;
                    iconImg.color          = collected ? Color.white : new Color(0.3f, 0.3f, 0.3f, 0.5f);
                    _itemSlotImages[sortOrder] = iconImg;
                }

                var name    = item != null ? item.itemName : $"Item {sortOrder}";
                var nameTmp = Tmp(slotGo.transform, "Name", name,
                    12f, collected ? UnlockedFg : LockedFg, TextAlignmentOptions.Center,
                    new Vector2(0.02f, 0.02f), new Vector2(0.98f, 0.27f));
                _itemSlotNames[sortOrder] = nameTmp;
            }
        }
    }

    // ── Inspirations tab content ──────────────────────────────────────────────

    private void BuildInspirationsContent(Transform parent)
    {
        BuildJournalColumn(parent,  1,  8, new Vector2(0.01f, 0.02f), new Vector2(0.49f, 0.98f));
        BuildJournalColumn(parent,  9, 16, new Vector2(0.51f, 0.02f), new Vector2(0.99f, 0.98f));
    }

    private void BuildJournalColumn(Transform parent, int fromId, int toId,
                                    Vector2 colMin, Vector2 colMax)
    {
        var col   = Rect(parent, $"Col{fromId}", colMin, colMax);
        int count = toId - fromId + 1;
        float step = 1f / count;

        for (int i = 0; i < count; i++)
        {
            int   id    = fromId + i;
            float yMax  = 1f - i * step;
            float yMin  = yMax - step;
            bool  known = _unlocked[id];

            var row = Rect(col.transform, $"Row{id}",
                new Vector2(0.01f, yMin + 0.015f), new Vector2(0.99f, yMax - 0.015f));
            var rowBg = row.AddComponent<Image>();
            rowBg.color = known ? ThemeHaveBg : ThemeLackBg;
            _entryBgs[id] = rowBg;

            var e = Tmp(row.transform, $"E{id}",
                known ? $"<b>{id:D2}.</b>  {Texts[id]}" : $"<b>{id:D2}.</b>  ???",
                17f,
                known ? UnlockedFg : LockedFg,
                TextAlignmentOptions.Left,
                new Vector2(0.04f, 0.05f), new Vector2(0.96f, 0.95f));

            e.fontStyle = known ? FontStyles.Normal : FontStyles.Italic;
            _entries[id] = e;
        }
    }

    // ── Themes tab content ────────────────────────────────────────────────────

    private void BuildThemesContent(Transform parent)
    {
        if (_themeData == null || _themeData.Length == 0)
        {
            Tmp(parent, "NoThemes", "Themes unlock through story progression.",
                20f, LockedFg, TextAlignmentOptions.Center,
                new Vector2(0.1f, 0.4f), new Vector2(0.9f, 0.6f));
            return;
        }

        int count = _themeData.Length;
        _themeEntryTitles   = new TMP_Text[count];
        _themeEntryStatuses = new TMP_Text[count];
        _themeEntryBgs      = new Image[count];

        int perPage = Mathf.CeilToInt(count / 2f);
        float step = 1f / perPage;

        for (int i = 0; i < count; i++)
        {
            var  theme = _themeData[i];
            bool done  = _themesCompleted[i];
            int page = i / perPage;
            int row = i % perPage;
            float xMin = page == 0 ? 0.01f : 0.525f;
            float xMax = page == 0 ? 0.475f : 0.99f;
            float yMax = 1f - row * step;
            float yMin = yMax - step + 0.008f;

            var themeGo = Rect(parent, $"Theme{i}", new Vector2(xMin, yMin), new Vector2(xMax, yMax));
            var themeBg = themeGo.AddComponent<Image>();
            themeBg.color     = done ? ThemeHaveBg : ThemeLackBg;
            _themeEntryBgs[i] = themeBg;

            bool hasDesc = theme != null && !string.IsNullOrEmpty(theme.description);
            float titleYMin = hasDesc ? 0.50f : 0.15f;

            _themeEntryTitles[i] = Tmp(themeGo.transform, "Title",
                theme != null ? theme.title : $"Theme {i + 1}",
                17f, done ? UnlockedFg : LockedFg, TextAlignmentOptions.Left,
                new Vector2(0.02f, titleYMin), new Vector2(0.80f, 0.95f));
            _themeEntryTitles[i].fontStyle = done ? FontStyles.Normal : FontStyles.Italic;

            if (hasDesc)
            {
                Tmp(themeGo.transform, "Desc",
                    done ? theme.description : "???",
                    12f,
                    done ? new Color32(0x77, 0x99, 0x77, 0xFF) : LockedFg,
                    TextAlignmentOptions.Left,
                    new Vector2(0.02f, 0.05f), new Vector2(0.80f, 0.48f));
            }

            _themeEntryStatuses[i] = Tmp(themeGo.transform, "Status",
                done ? "✓ Complete" : "???",
                14f, done ? CompletedFg : LockedFg, TextAlignmentOptions.Right,
                new Vector2(0.70f, 0.30f), new Vector2(0.98f, 0.95f));
        }
    }

    // ── UI factory helpers ────────────────────────────────────────────────────

    private static GameObject Rect(Transform parent, string name,
                                   Vector2 anchorMin, Vector2 anchorMax)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return go;
    }

    private TMP_Text Tmp(Transform parent, string name, string text,
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
        if (_font != null) tmp.font = _font;
        return tmp;
    }
}
