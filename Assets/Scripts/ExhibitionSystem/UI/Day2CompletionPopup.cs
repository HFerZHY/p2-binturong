using ExhibitionSystem.Core;
using Otowa.UI;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ExhibitionSystem.UI
{
    public class Day2CompletionPopup : MonoBehaviour
    {
        private const string DAY2_EXHIBITION_SCENE = "ExhibitionDay2Scene";

        [SerializeField] private GameObject _panel;
        [SerializeField] private TMP_Text _headlineText;
        [SerializeField] private TMP_Text _bodyText;
        [SerializeField] private Button _confirmButton;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private string _nextSceneName = "Day2World";

        private const string TitleText = "Today's work is done.";
        private const string BodyText = "Let's head back to the station.";
        private const string ButtonText = "Continue";

        private GameObject _canvasObject;
        private GameObject _popupRoot;
        private bool _pendingShow;
        private bool _isTransitioning;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        internal static void EnsurePopupExists()
        {
            if (SceneManager.GetActiveScene().name != DAY2_EXHIBITION_SCENE)
                return;

            if (FindFirstObjectByType<Day2CompletionPopup>(FindObjectsInactive.Include) != null)
                return;

            var popupObject = new GameObject(nameof(Day2CompletionPopup));
            popupObject.AddComponent<Day2CompletionPopup>();
        }

        private void Awake()
        {
            BuildRuntimePopup();
        }

        private void OnEnable()
        {
            ExhibitionManager.OnExhibitionEnded += HandleExhibitionEnded;
            RewardPopup.OnRewardConfirmed += HandleRewardConfirmed;
        }

        private void OnDisable()
        {
            ExhibitionManager.OnExhibitionEnded -= HandleExhibitionEnded;
            RewardPopup.OnRewardConfirmed -= HandleRewardConfirmed;
        }

        private void OnDestroy()
        {
            if (_canvasObject != null)
                Destroy(_canvasObject);
        }

        private void Start()
        {
            if (_confirmButton != null)
                _confirmButton.onClick.AddListener(HandleConfirmClicked);

            ConfigureModalInput();
            Hide();
        }

        private void HandleExhibitionEnded(bool success, int satisfaction, int threshold)
        {
            if (!success || threshold <= 0 || satisfaction < threshold || !AreAllThemesCompleted())
                return;

            _pendingShow = true;
        }

        private void HandleRewardConfirmed()
        {
            if (!_pendingShow)
                return;

            _pendingShow = false;
            Show();
        }

        private void Show()
        {
            if (_headlineText != null)
                _headlineText.text = TitleText;

            if (_bodyText != null)
                _bodyText.text = BodyText;

            if (_popupRoot != null)
                _popupRoot.SetActive(true);

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 1f;
                _canvasGroup.blocksRaycasts = true;
                _canvasGroup.interactable = true;
            }
        }

        private void HandleConfirmClicked()
        {
            if (_isTransitioning)
                return;

            _isTransitioning = true;
            if (_confirmButton != null)
                _confirmButton.interactable = false;

            StartCoroutine(ExhibitionSceneFadeTransition.FadeOutAndLoad(_nextSceneName));
        }

        private void Hide()
        {
            if (_popupRoot != null)
                _popupRoot.SetActive(false);

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
                _canvasGroup.blocksRaycasts = false;
                _canvasGroup.interactable = false;
            }

            if (_panel != null && _panel != gameObject && _panel != _popupRoot)
                _panel.SetActive(false);
        }

        private static bool AreAllThemesCompleted()
        {
            var manager = ExhibitionManager.Instance;
            if (manager == null || manager.AllThemes == null || manager.AllThemes.Count == 0)
                return false;

            foreach (var theme in manager.AllThemes)
            {
                if (theme == null || !theme.isCompleted)
                    return false;
            }

            return true;
        }

        private void BuildRuntimePopup()
        {
            if (_popupRoot != null)
                return;

            if (_panel != null && _panel != gameObject)
                _panel.SetActive(false);

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
                _canvasGroup.blocksRaycasts = false;
                _canvasGroup.interactable = false;
            }

            _canvasObject = new GameObject("Day2CompletionCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            SceneManager.MoveGameObjectToScene(_canvasObject, gameObject.scene);
            _canvasObject.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            _canvasObject.transform.localScale = Vector3.one;

            var canvas = _canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 320;

            var scaler = _canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            _popupRoot = CreateRect("PopupRoot", _canvasObject.transform, Vector2.zero, Vector2.one);
            var blocker = _popupRoot.AddComponent<Image>();
            blocker.color = new Color(0f, 0f, 0f, 0.56f);
            _canvasGroup = _popupRoot.AddComponent<CanvasGroup>();

            var panel = CreateRect("Panel", _popupRoot.transform, new Vector2(0.31f, 0.35f), new Vector2(0.69f, 0.65f));
            var panelImage = panel.AddComponent<Image>();
            panelImage.color = new Color(0.24f, 0.15f, 0.09f, 0.98f);

            _panel = _popupRoot;

            _headlineText = CreateText("Title", panel.transform, new Vector2(0.08f, 0.58f), new Vector2(0.92f, 0.86f));
            _headlineText.text = TitleText;
            _headlineText.fontSize = 52f;
            _headlineText.alignment = TextAlignmentOptions.Center;
            _headlineText.color = new Color(1f, 0.94f, 0.79f);

            _bodyText = CreateText("Body", panel.transform, new Vector2(0.08f, 0.35f), new Vector2(0.92f, 0.57f));
            _bodyText.text = BodyText;
            _bodyText.fontSize = 30f;
            _bodyText.alignment = TextAlignmentOptions.Center;
            _bodyText.color = Color.white;

            var buttonObject = CreateRect("ConfirmButton", panel.transform, new Vector2(0.36f, 0.10f), new Vector2(0.64f, 0.29f));
            var buttonImage = buttonObject.AddComponent<Image>();
            buttonImage.color = new Color(0.68f, 0.47f, 0.27f, 1f);
            _confirmButton = buttonObject.AddComponent<Button>();
            _confirmButton.targetGraphic = buttonImage;

            var buttonLabel = CreateText("Label", buttonObject.transform, Vector2.zero, Vector2.one);
            buttonLabel.text = ButtonText;
            buttonLabel.fontSize = 31f;
            buttonLabel.fontStyle = FontStyles.Bold;
            buttonLabel.alignment = TextAlignmentOptions.Center;
            buttonLabel.color = Color.white;

            _popupRoot.SetActive(false);
        }

        private void ConfigureModalInput()
        {
            if (_popupRoot == null || _confirmButton == null)
                return;

            var modalInput = _popupRoot.GetComponent<ModalConfirmInput>();
            if (modalInput == null)
                modalInput = _popupRoot.AddComponent<ModalConfirmInput>();

            modalInput.Configure(_confirmButton);
        }

        private static GameObject CreateRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            var rect = gameObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return gameObject;
        }

        private static TextMeshProUGUI CreateText(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax)
        {
            var textObject = CreateRect(name, parent, anchorMin, anchorMax);
            var text = textObject.AddComponent<TextMeshProUGUI>();
            text.textWrappingMode = TextWrappingModes.Normal;
            text.raycastTarget = false;
            return text;
        }
    }
}
