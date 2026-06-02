using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

namespace Otowa.Minimap
{
    /// <summary>
    /// Manages the minimap overlay. Procedurally builds the UI.
    ///
    /// Setup:
    ///   1. Create an empty GameObject and attach this script.
    ///   2. Set World Min / World Max to match your scene's walkable bounds.
    ///   3. Attach MinimapIcon to the player and each NPC.
    ///   4. Attach MinimapLocationMarker to entrances (HotSpring, Ryotei, etc.).
    ///   5. Optionally assign Map Background and Map Icon Sprite in the Inspector.
    ///
    /// Controls: Tab key or the Map button (top-left corner).
    /// </summary>
    [DefaultExecutionOrder(-200)]
    public class MinimapController : MonoBehaviour
    {
        public static MinimapController Instance { get; private set; }

        [Header("World Bounds — match your scene's walkable area")]
        [SerializeField] private Vector2 worldMin = new(-22f, -18f);
        [SerializeField] private Vector2 worldMax = new( 22f,  14f);

        [Header("Appearance")]
        [SerializeField] private Vector2 panelSize      = new(820f, 600f);
        [SerializeField] private Vector2 iconSize        = new( 40f,  40f);
        [SerializeField] private Vector2 locationSize    = new( 32f,  32f);
        [SerializeField] [Range(0f, 1f)] private float panelAlpha = 0.70f;
        [SerializeField] private Sprite  mapBackground;
        [SerializeField] private Sprite  mapIconSprite;

        // ── UI refs ───────────────────────────────────────────────────────────

        private GameObject    _panel;
        private GameObject    _toggleButton;
        private RectTransform _iconContainer;
        private bool          _isOpen;
        private bool          _externalToggleLocked;

        // ── Character icon tracking ───────────────────────────────────────────

        private readonly List<MinimapIcon>                      _icons     = new();
        private readonly Dictionary<MinimapIcon, RectTransform> _iconRects = new();

        // ── Location marker tracking ──────────────────────────────────────────

        private readonly List<MinimapLocationMarker>                      _locations     = new();
        private readonly Dictionary<MinimapLocationMarker, RectTransform> _locationRects = new();

        // ── Unity lifecycle ───────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            BuildUI();
        }

        private void Start()
        {
            // Catch components that enabled before this Awake ran
            foreach (var icon in Object.FindObjectsByType<MinimapIcon>(FindObjectsSortMode.None))
                Register(icon);
            foreach (var loc in Object.FindObjectsByType<MinimapLocationMarker>(FindObjectsSortMode.None))
                RegisterLocation(loc);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            if (Keyboard.current?.tabKey.wasPressedThisFrame == true)
                Toggle();

            if (_isOpen)
                UpdatePositions();
        }

        // ── Public API ────────────────────────────────────────────────────────

        public void Toggle()
        {
            if (_externalToggleLocked)
                return;

            _isOpen = !_isOpen;
            _panel.SetActive(_isOpen);
        }

        public void SetExternalToggleLocked(bool locked)
        {
            _externalToggleLocked = locked;
            if (_toggleButton != null)
                _toggleButton.SetActive(!locked);

            if (!locked || !_isOpen)
                return;

            _isOpen = false;
            _panel.SetActive(false);
        }

        public void Register(MinimapIcon icon)
        {
            if (_icons.Contains(icon)) return;
            _icons.Add(icon);
            CreateIconObject(icon);
        }

        public void Unregister(MinimapIcon icon)
        {
            if (_iconRects.TryGetValue(icon, out var rt))
            {
                if (rt != null) Destroy(rt.gameObject);
                _iconRects.Remove(icon);
            }
            _icons.Remove(icon);
        }

        public void RegisterLocation(MinimapLocationMarker loc)
        {
            if (_locations.Contains(loc)) return;
            _locations.Add(loc);
            CreateLocationObject(loc);
        }

        public void UnregisterLocation(MinimapLocationMarker loc)
        {
            if (_locationRects.TryGetValue(loc, out var rt))
            {
                if (rt != null) Destroy(rt.gameObject);
            }
            _locationRects.Remove(loc);
            _locations.Remove(loc);
        }

        // ── UI objects ────────────────────────────────────────────────────────

        private void CreateIconObject(MinimapIcon icon)
        {
            var go = new GameObject(icon.name, typeof(RectTransform));
            go.transform.SetParent(_iconContainer, false);

            var rt = (RectTransform)go.transform;
            rt.sizeDelta = iconSize;

            var img = go.AddComponent<Image>();
            if (icon.Portrait != null) { img.sprite = icon.Portrait; img.preserveAspect = true; }
            else img.color = icon.IconColor;

            _iconRects[icon] = rt;
        }

        private void CreateLocationObject(MinimapLocationMarker loc)
        {
            var go = new GameObject(loc.name, typeof(RectTransform));
            go.transform.SetParent(_iconContainer, false);

            var rt = (RectTransform)go.transform;
            rt.sizeDelta = loc.MarkerSize.sqrMagnitude > 0f ? loc.MarkerSize : locationSize;

            var img = go.AddComponent<Image>();
            if (loc.MarkerIcon != null) { img.sprite = loc.MarkerIcon; img.preserveAspect = true; }
            else img.color = loc.MarkerColor;

            if (!string.IsNullOrEmpty(loc.LocationName))
                AttachLocationLabel(go.transform, loc.LocationName);

            _locationRects[loc] = rt;
        }

        private static void AttachLocationLabel(Transform parent, string text)
        {
            var labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(parent, false);
            var label = labelGo.AddComponent<TextMeshProUGUI>();
            label.text      = text;
            label.fontSize  = 16f;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Top;
            label.color     = Color.white;
            var lrt = (RectTransform)labelGo.transform;
            lrt.anchorMin        = lrt.anchorMax = new Vector2(0.5f, 0f);
            lrt.pivot            = new Vector2(0.5f, 1f);
            lrt.anchoredPosition = new Vector2(0f, -4f);
            lrt.sizeDelta        = new Vector2(160f, 28f);
        }

        // ── Position updates ──────────────────────────────────────────────────

        private void UpdatePositions()
        {
            float mapW = _iconContainer.rect.width;
            float mapH = _iconContainer.rect.height;

            foreach (var (icon, rt) in _iconRects)
            {
                if (icon == null || rt == null) continue;
                rt.anchoredPosition = WorldToMap((Vector2)icon.transform.position, mapW, mapH);
            }

            foreach (var (loc, rt) in _locationRects)
            {
                if (loc == null || rt == null) continue;
                rt.anchoredPosition = WorldToMap((Vector2)loc.transform.position, mapW, mapH);
            }
        }

        private Vector2 WorldToMap(Vector2 world, float mapW, float mapH)
        {
            float nx = Mathf.InverseLerp(worldMin.x, worldMax.x, world.x);
            float ny = Mathf.InverseLerp(worldMin.y, worldMax.y, world.y);
            return new Vector2((nx - 0.5f) * mapW, (ny - 0.5f) * mapH);
        }

        // ── UI construction ───────────────────────────────────────────────────

        private void BuildUI()
        {
            var canvasGo = new GameObject("MinimapCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);

            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 150;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight  = 0.5f;

            BuildToggleButton(canvasGo.transform);
            BuildPanel(canvasGo.transform);
        }

        private void BuildToggleButton(Transform canvas)
        {
            var go = new GameObject("MapButton", typeof(RectTransform));
            go.transform.SetParent(canvas, false);
            _toggleButton = go;

            var rt = (RectTransform)go.transform;
            rt.anchorMin        = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot            = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(12f, -12f);
            rt.sizeDelta        = new Vector2(182f, 182f);

            go.AddComponent<Image>().color = Color.clear;
            var btn = go.AddComponent<Button>();
            btn.onClick.AddListener(Toggle);

            var innerGo = new GameObject("Content", typeof(RectTransform));
            innerGo.transform.SetParent(go.transform, false);
            var innerRt = (RectTransform)innerGo.transform;
            innerRt.anchorMin = new Vector2(0.1f, 0.1f);
            innerRt.anchorMax = new Vector2(0.9f, 0.9f);
            innerRt.offsetMin = innerRt.offsetMax = Vector2.zero;

            if (mapIconSprite != null)
            {
                var img = innerGo.AddComponent<Image>();
                img.sprite = mapIconSprite; img.preserveAspect = true;
            }
            else
            {
                var lbl = innerGo.AddComponent<TextMeshProUGUI>();
                lbl.text = "Map"; lbl.fontSize = 13f;
                lbl.alignment = TextAlignmentOptions.Center;
                lbl.color = new Color32(0xc8, 0xd4, 0xc8, 0xFF);
            }
        }

        private void BuildPanel(Transform canvas)
        {
            _panel = new GameObject("MinimapPanel", typeof(RectTransform));
            _panel.transform.SetParent(canvas, false);

            var panelRt = (RectTransform)_panel.transform;
            panelRt.anchorMin        = panelRt.anchorMax = new Vector2(0f, 1f);
            panelRt.pivot            = new Vector2(0f, 1f);
            panelRt.sizeDelta        = panelSize;
            panelRt.anchoredPosition = new Vector2(12f, -12f);

            // Semi-transparent panel background
            var bg = _panel.AddComponent<Image>();
            bg.color = new Color(0x06 / 255f, 0x0e / 255f, 0x06 / 255f, panelAlpha);

            // Title bar
            var titleGo = new GameObject("Title", typeof(RectTransform));
            titleGo.transform.SetParent(_panel.transform, false);
            var titleLabel = titleGo.AddComponent<TextMeshProUGUI>();
            titleLabel.text      = "MAP  <color=#d4a060>[TAB]</color>";
            titleLabel.fontSize  = 28f;
            titleLabel.fontStyle = FontStyles.Bold;
            titleLabel.alignment = TextAlignmentOptions.Center;
            titleLabel.color     = new Color32(0xc8, 0xd4, 0xc8, 0xFF);
            var trt = (RectTransform)titleGo.transform;
            trt.anchorMin = new Vector2(0f, 0.90f);
            trt.anchorMax = Vector2.one;
            trt.offsetMin = trt.offsetMax = Vector2.zero;

            // Close button (top-right)
            var closeGo = new GameObject("CloseBtn", typeof(RectTransform));
            closeGo.transform.SetParent(_panel.transform, false);
            var closeRt = (RectTransform)closeGo.transform;
            closeRt.anchorMin = new Vector2(0.90f, 0.90f);
            closeRt.anchorMax = Vector2.one;
            closeRt.offsetMin = closeRt.offsetMax = Vector2.zero;
            closeGo.AddComponent<Image>().color = new Color(0, 0, 0, 0);
            closeGo.AddComponent<Button>().onClick.AddListener(Toggle);
            var closeLbl = new GameObject("X", typeof(RectTransform));
            closeLbl.transform.SetParent(closeGo.transform, false);
            var xl = closeLbl.AddComponent<TextMeshProUGUI>();
            xl.text = "X"; xl.fontSize = 30f;
            xl.fontStyle = FontStyles.Bold;
            xl.alignment = TextAlignmentOptions.Center;
            xl.color = new Color32(0xc8, 0xd4, 0xc8, 0xFF);
            var xlrt = (RectTransform)closeLbl.transform;
            xlrt.anchorMin = Vector2.zero; xlrt.anchorMax = Vector2.one;
            xlrt.offsetMin = xlrt.offsetMax = Vector2.zero;

            // Map area
            var mapAreaGo = new GameObject("MapArea", typeof(RectTransform));
            mapAreaGo.transform.SetParent(_panel.transform, false);
            var mapAreaRt = (RectTransform)mapAreaGo.transform;
            mapAreaRt.anchorMin = new Vector2(0.02f, 0.02f);
            mapAreaRt.anchorMax = new Vector2(0.98f, 0.89f);
            mapAreaRt.offsetMin = mapAreaRt.offsetMax = Vector2.zero;
            mapAreaGo.AddComponent<Image>().color = new Color32(0x0a, 0x18, 0x0a, 0xFF);

            // Optional map background image (assign in Inspector)
            if (mapBackground != null)
            {
                var bgImgGo = new GameObject("MapBackground", typeof(RectTransform));
                bgImgGo.transform.SetParent(mapAreaGo.transform, false);
                var bgImgRt = (RectTransform)bgImgGo.transform;
                bgImgRt.anchorMin = Vector2.zero;
                bgImgRt.anchorMax = Vector2.one;
                bgImgRt.offsetMin = bgImgRt.offsetMax = Vector2.zero;
                var bgImg = bgImgGo.AddComponent<Image>();
                bgImg.sprite         = mapBackground;
                bgImg.preserveAspect = false;
                bgImg.color          = new Color(1f, 1f, 1f, 0.55f); // slightly dim so icons stand out
            }

            // Icon + location container (centered, icons positioned relative to center)
            var containerGo = new GameObject("IconContainer", typeof(RectTransform));
            containerGo.transform.SetParent(mapAreaGo.transform, false);
            _iconContainer = (RectTransform)containerGo.transform;
            _iconContainer.anchorMin = Vector2.zero;
            _iconContainer.anchorMax = Vector2.one;
            _iconContainer.offsetMin = _iconContainer.offsetMax = Vector2.zero;

            _panel.SetActive(false);
        }
    }
}
