using System;
using System.Collections;
using ExhibitionSystem.Core;
using ExhibitionSystem.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ExhibitionSystem.UI
{
    /// <summary>
    /// Keeps the selected curation hint visible after the failure tutorial is dismissed.
    /// </summary>
    public class ExhibitionErrorHintBar : MonoBehaviour
    {
        private const string RIN_SPRITE_RESOURCE = "Characters/WorldSprite/rin";
        private const string RIN_HEAD_SPRITE_NAME = "spritesheet_template_0";

        [SerializeField] private GameObject _panel;
        [SerializeField] private TMP_Text _bodyText;
        [SerializeField] private CanvasGroup _canvasGroup;

        private string _pendingHint;
        private Coroutine _showPendingCoroutine;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        internal static void EnsureErrorHintBarExists()
        {
            if (FindFirstObjectByType<ExhibitionErrorHintBar>(FindObjectsInactive.Include) != null)
                return;

            if (FindFirstObjectByType<ExhibitionManager>(FindObjectsInactive.Include) == null)
                return;

            var layoutRoot = GameObject.Find("LayoutRoot");
            if (layoutRoot == null)
                return;

            CreateRuntimeHintBar(layoutRoot.transform);
        }

        private void OnEnable()
        {
            ExhibitionManager.OnPlayerHint += HandlePlayerHint;
            ExhibitionManager.OnExhibitionStarted += Clear;
            ExhibitionManager.OnThemeSelected += HandleThemeSelected;
            ExhibitionManager.OnCurationCleared += Clear;
            TutorialPopup.OnPlayerHintDismissed += HandleTutorialHintDismissed;
        }

        private void OnDisable()
        {
            ExhibitionManager.OnPlayerHint -= HandlePlayerHint;
            ExhibitionManager.OnExhibitionStarted -= Clear;
            ExhibitionManager.OnThemeSelected -= HandleThemeSelected;
            ExhibitionManager.OnCurationCleared -= Clear;
            TutorialPopup.OnPlayerHintDismissed -= HandleTutorialHintDismissed;
        }

        private void Start()
        {
            Hide();
        }

        private void HandlePlayerHint(string hint)
        {
            if (string.IsNullOrWhiteSpace(hint))
                return;

            _pendingHint = NormalizeHint(hint);
            if (_showPendingCoroutine != null)
                StopCoroutine(_showPendingCoroutine);

            _showPendingCoroutine = StartCoroutine(ShowPendingAfterTutorialIfReady());
        }

        private IEnumerator ShowPendingAfterTutorialIfReady()
        {
            yield return null;
            _showPendingCoroutine = null;

            if (!TutorialPopup.IsShowingPlayerHint)
                ShowPendingHint();
        }

        private void HandleTutorialHintDismissed()
        {
            ShowPendingHint();
        }

        private void HandleThemeSelected(ExhibitionTheme theme)
        {
            Clear();
        }

        private void ShowPendingHint()
        {
            if (string.IsNullOrWhiteSpace(_pendingHint))
                return;

            if (_bodyText != null)
                _bodyText.text = _pendingHint;

            _pendingHint = null;

            if (_panel != null)
                _panel.SetActive(true);

            if (_canvasGroup != null)
                _canvasGroup.alpha = 1f;
        }

        private void Clear()
        {
            _pendingHint = null;
            Hide();
        }

        private void Hide()
        {
            if (_canvasGroup != null)
                _canvasGroup.alpha = 0f;

            if (_panel != null && _panel != gameObject)
                _panel.SetActive(false);
        }

        private static string NormalizeHint(string hint)
        {
            string normalized = hint.Trim();
            if (normalized.StartsWith("Rin:", StringComparison.OrdinalIgnoreCase))
                normalized = normalized.Substring(4).Trim();

            if (normalized.Length >= 2 && normalized[0] == '(' && normalized[^1] == ')')
                normalized = normalized.Substring(1, normalized.Length - 2).Trim();

            return normalized;
        }

        private static ExhibitionErrorHintBar CreateRuntimeHintBar(Transform parent)
        {
            var panelObj = CreateRuntimeChild(parent, "ExhibitionErrorHintBar");
            var panelRt = panelObj.GetComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0.5f, 0f);
            panelRt.anchorMax = new Vector2(0.5f, 0f);
            panelRt.pivot = new Vector2(0.5f, 0f);
            panelRt.anchoredPosition = new Vector2(0f, 130f);
            panelRt.sizeDelta = new Vector2(1460f, 74f);

            var background = panelObj.AddComponent<Image>();
            background.color = new Color(0f, 0f, 0f, 0.76f);
            background.raycastTarget = false;

            var canvasGroup = panelObj.AddComponent<CanvasGroup>();
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

            var portraitObj = CreateRuntimeChild(panelObj.transform, "RinPortrait");
            var portrait = portraitObj.AddComponent<Image>();
            portrait.sprite = LoadRinHeadSprite();
            portrait.color = Color.white;
            portrait.preserveAspect = true;
            portrait.raycastTarget = false;
            var portraitRt = portraitObj.GetComponent<RectTransform>();
            portraitRt.anchorMin = new Vector2(0f, 0.5f);
            portraitRt.anchorMax = new Vector2(0f, 0.5f);
            portraitRt.pivot = new Vector2(0f, 0.5f);
            portraitRt.anchoredPosition = new Vector2(14f, 0f);
            portraitRt.sizeDelta = new Vector2(58f, 58f);

            var body = CreateRuntimeText(panelObj.transform, "Body", string.Empty, 24f);
            body.color = Color.white;
            body.enableAutoSizing = true;
            body.fontSizeMin = 17f;
            body.fontSizeMax = 24f;
            body.textWrappingMode = TextWrappingModes.NoWrap;
            body.overflowMode = TextOverflowModes.Ellipsis;
            body.raycastTarget = false;
            var bodyRt = body.GetComponent<RectTransform>();
            bodyRt.anchorMin = Vector2.zero;
            bodyRt.anchorMax = Vector2.one;
            bodyRt.offsetMin = new Vector2(88f, 8f);
            bodyRt.offsetMax = new Vector2(-20f, -8f);

            var hintBar = panelObj.AddComponent<ExhibitionErrorHintBar>();
            hintBar._panel = panelObj;
            hintBar._bodyText = body;
            hintBar._canvasGroup = canvasGroup;
            return hintBar;
        }

        private static Sprite LoadRinHeadSprite()
        {
            Sprite fallback = null;
            foreach (var sprite in Resources.LoadAll<Sprite>(RIN_SPRITE_RESOURCE))
            {
                if (sprite == null)
                    continue;

                if (sprite.name == RIN_HEAD_SPRITE_NAME)
                    return sprite;

                if (fallback == null)
                    fallback = sprite;
            }

            return fallback;
        }

        private static GameObject CreateRuntimeChild(Transform parent, string name)
        {
            var obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            return obj;
        }

        private static TextMeshProUGUI CreateRuntimeText(Transform parent, string name, string text, float fontSize)
        {
            var obj = CreateRuntimeChild(parent, name);
            var tmp = obj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.color = Color.white;
            return tmp;
        }
    }
}
