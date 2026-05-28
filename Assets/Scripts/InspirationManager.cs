using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
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

    private TMP_Text[] _themeEntryTitles;
    private TMP_Text[] _themeEntryStatuses;
    private Image[]    _themeEntryBgs;

    // ── Colours ───────────────────────────────────────────────────────────────

    private static readonly Color PopupBg      = new Color32(0x05, 0x10, 0x05, 0xF2);
    private static readonly Color PopupLine    = new Color32(0x60, 0xa8, 0x60, 0xFF);
    private static readonly Color PopupHdr     = new Color32(0x88, 0xd8, 0x88, 0xFF);
    private static readonly Color PopupBody    = new Color32(0xcc, 0xdc, 0xcc, 0xFF);
    private static readonly Color HintBg       = new Color32(0x06, 0x14, 0x06, 0xE8);
    private static readonly Color HintFg       = new Color32(0x88, 0xcc, 0x88, 0xFF);
    private static readonly Color JournalBg    = new Color32(0x03, 0x09, 0x03, 0xF4);
    private static readonly Color CardBg       = new Color32(0x08, 0x14, 0x08, 0xFF);
    private static readonly Color JournalHdr   = new Color32(0x88, 0xd8, 0x88, 0xFF);
    private static readonly Color SepLine      = new Color32(0x40, 0x80, 0x40, 0xFF);
    private static readonly Color CloseFg      = new Color32(0x50, 0x80, 0x50, 0xFF);
    private static readonly Color UnlockedFg   = new Color32(0xcc, 0xdc, 0xcc, 0xFF);
    private static readonly Color LockedFg     = new Color32(0x38, 0x50, 0x38, 0xFF);
    private static readonly Color TabActiveBg  = new Color32(0x10, 0x28, 0x10, 0xFF);
    private static readonly Color TabInactiveBg = new Color32(0x05, 0x0d, 0x05, 0xFF);
    private static readonly Color ItemHaveBg   = new Color32(0x09, 0x1e, 0x09, 0xFF);
    private static readonly Color ItemLackBg   = new Color32(0x04, 0x0c, 0x04, 0xFF);
    private static readonly Color ThemeHaveBg  = new Color32(0x08, 0x1e, 0x08, 0xFF);
    private static readonly Color ThemeLackBg  = new Color32(0x05, 0x10, 0x05, 0xFF);
    private static readonly Color CompletedFg  = new Color32(0x66, 0xcc, 0x66, 0xFF);

    // ── Unity lifecycle ───────────────────────────────────────────────────────

    private void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(gameObject); return; }
        _instance = this;
        DontDestroyOnLoad(gameObject);
        LoadGameData();
        BuildUI();
    }

    private void Update()
    {
        var kb = Keyboard.current;
        if (kb != null && kb.eKey.wasPressedThisFrame && _introduced)
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
        _journalOpen = !_journalOpen;
        _journalGo.SetActive(_journalOpen);
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
        BuildJournal(cvGo.transform);
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

    // ── Journal overlay ───────────────────────────────────────────────────────

    private void BuildJournal(Transform cv)
    {
        _journalGo = Rect(cv, "Journal", Vector2.zero, Vector2.one);
        _journalGo.AddComponent<Image>().color = JournalBg;

        var card = Rect(_journalGo.transform, "Card",
            new Vector2(0.07f, 0.04f), new Vector2(0.93f, 0.96f));
        card.AddComponent<Image>().color = CardBg;
        var ct = card.transform;

        Tmp(ct, "Title", "Rin's Journal",
            28f, JournalHdr, TextAlignmentOptions.Center,
            new Vector2(0.02f, 0.93f), new Vector2(0.78f, 0.99f));

        Tmp(ct, "CloseHint", "[ E ]  close",
            16f, CloseFg, TextAlignmentOptions.Right,
            new Vector2(0.74f, 0.93f), new Vector2(0.98f, 0.99f));

        Rect(ct, "Sep", new Vector2(0.02f, 0.912f), new Vector2(0.98f, 0.922f))
            .AddComponent<Image>().color = SepLine;

        BuildTabBar(ct);

        Rect(ct, "TabSep", new Vector2(0.02f, 0.855f), new Vector2(0.98f, 0.863f))
            .AddComponent<Image>().color = SepLine;

        _itemsPanel  = Rect(ct, "ItemsPanel",  new Vector2(0.01f, 0.02f), new Vector2(0.99f, 0.852f));
        _inspPanel   = Rect(ct, "InspPanel",   new Vector2(0.01f, 0.02f), new Vector2(0.99f, 0.852f));
        _themesPanel = Rect(ct, "ThemesPanel", new Vector2(0.01f, 0.02f), new Vector2(0.99f, 0.852f));

        BuildItemsContent(_itemsPanel.transform);
        BuildInspirationsContent(_inspPanel.transform);
        BuildThemesContent(_themesPanel.transform);

        SwitchTab(1); // default to Inspirations
        _journalGo.SetActive(false);
    }

    // ── Tab bar ───────────────────────────────────────────────────────────────

    private void BuildTabBar(Transform ct)
    {
        var tabBar = Rect(ct, "TabBar", new Vector2(0.02f, 0.863f), new Vector2(0.98f, 0.910f));
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
        const float colW = 1f / cols;
        const float rowH = 1f / rows;
        const float gapX = 0.004f;
        const float gapY = 0.006f;

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                int sortOrder = row * cols + col + 1; // 1-based
                if (sortOrder > 16) break;

                float xMin = col * colW + gapX;
                float xMax = (col + 1) * colW - gapX;
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

            var e = Tmp(col.transform, $"E{id}",
                known ? $"<b>{id:D2}.</b>  {Texts[id]}" : $"<b>{id:D2}.</b>  ???",
                18f,
                known ? UnlockedFg : LockedFg,
                TextAlignmentOptions.TopLeft,
                new Vector2(0f, yMin), new Vector2(1f, yMax));

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

        float step = 1f / count;

        for (int i = 0; i < count; i++)
        {
            var  theme = _themeData[i];
            bool done  = _themesCompleted[i];
            float yMax = 1f - i * step;
            float yMin = yMax - step + 0.008f;

            var themeGo = Rect(parent, $"Theme{i}", new Vector2(0.01f, yMin), new Vector2(0.99f, yMax));
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
