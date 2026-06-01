using ExhibitionSystem.Core;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ExhibitionSystem.UI
{
    public class Day3CompletionPopup : MonoBehaviour
    {
        private const string DAY3_EXHIBITION_SCENE = "ExhibitionDay3Scene";
        private const string NEXT_SCENE = "Day3HikaruArrival";

        private GameObject _popupRoot;
        private Button _confirmButton;
        private bool _pendingShow;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsurePopupExists()
        {
            if (SceneManager.GetActiveScene().name != DAY3_EXHIBITION_SCENE)
                return;

            if (FindFirstObjectByType<Day3CompletionPopup>(FindObjectsInactive.Include) != null)
                return;

            var popupObject = new GameObject(nameof(Day3CompletionPopup));
            popupObject.AddComponent<Day3CompletionPopup>();
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
            _popupRoot.SetActive(true);
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
            var canvasObject = new GameObject("Day3CompletionCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);

            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 320;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            _popupRoot = CreateRect("PopupRoot", canvasObject.transform, Vector2.zero, Vector2.one);
            var blocker = _popupRoot.AddComponent<Image>();
            blocker.color = new Color(0f, 0f, 0f, 0.56f);

            var panel = CreateRect("Panel", _popupRoot.transform, new Vector2(0.31f, 0.35f), new Vector2(0.69f, 0.65f));
            var panelImage = panel.AddComponent<Image>();
            panelImage.color = new Color(0.24f, 0.15f, 0.09f, 0.98f);

            var title = CreateText("Title", panel.transform, new Vector2(0.08f, 0.58f), new Vector2(0.92f, 0.86f));
            title.text = "Today's work is done.";
            title.fontSize = 52f;
            title.alignment = TextAlignmentOptions.Center;
            title.color = new Color(1f, 0.94f, 0.79f);

            var body = CreateText("Body", panel.transform, new Vector2(0.08f, 0.35f), new Vector2(0.92f, 0.57f));
            body.text = "Someone is coming to the station...";
            body.fontSize = 30f;
            body.alignment = TextAlignmentOptions.Center;
            body.color = Color.white;

            var buttonObject = CreateRect("ConfirmButton", panel.transform, new Vector2(0.36f, 0.10f), new Vector2(0.64f, 0.29f));
            var buttonImage = buttonObject.AddComponent<Image>();
            buttonImage.color = new Color(0.68f, 0.47f, 0.27f, 1f);
            _confirmButton = buttonObject.AddComponent<Button>();
            _confirmButton.targetGraphic = buttonImage;
            _confirmButton.onClick.AddListener(() => SceneManager.LoadScene(NEXT_SCENE));

            var buttonLabel = CreateText("Label", buttonObject.transform, Vector2.zero, Vector2.one);
            buttonLabel.text = "Continue";
            buttonLabel.fontSize = 31f;
            buttonLabel.fontStyle = FontStyles.Bold;
            buttonLabel.alignment = TextAlignmentOptions.Center;
            buttonLabel.color = Color.white;

            _popupRoot.SetActive(false);
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
            text.enableWordWrapping = true;
            text.raycastTarget = false;
            return text;
        }
    }
}
