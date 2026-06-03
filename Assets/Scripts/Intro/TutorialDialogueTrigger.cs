using System.Collections.Generic;
using DialogueSystem.Core;
using DialogueSystem.Data;
using DialogueSystem.Player;
using Otowa.Audio;
using Otowa.Minimap;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Otowa.Intro
{
    /// <summary>
    /// Drop on a GameObject in TutorialToRyotei. Fires a brief Rin-narrated
    /// tutorial sequence on scene load, then leaves the player free to explore.
    /// </summary>
    [DefaultExecutionOrder(-500)]
    public class TutorialDialogueTrigger : MonoBehaviour
    {
        [SerializeField] private bool autoTriggerOnStart = true;

        private static readonly string[] GuideMessages =
        {
            "Move with <b>WASD</b>.\n\nClick dialogue boxes to continue.",
            "Stand near a person or object.\n\nPress <b>Space</b> to interact.",
            "Open the map with <b>Tab</b>.\n\nYou can also click the map button in the top-left corner.",
        };

        private Character _rin;
        private GameObject _guideRoot;
        private TMP_Text _guideBody;
        private PlayerMovement _playerMovement;
        private PlayerInteractor _playerInteractor;
        private int _guideIndex;
        private bool _controlsLocked;

        private void Start()
        {
            GameAudioManager.Instance.StopBgm();
            GameAudioManager.Instance.StopSfxLoop(AudioId.ForestAtmosphere, 0.2f);
            GameAudioManager.Instance.PlaySfxLoop(AudioId.Wind, fadeIn: 0.3f);
            if (autoTriggerOnStart)
                TriggerTutorial();
        }

        private void OnDestroy()
        {
            UnlockControls();
        }

        public void TriggerTutorial()
        {
            if (DialogueManager.Instance == null || DialogueManager.Instance.IsActive) return;
            BuildGuidePopup();
            LockControls();
            _guideIndex = 0;
            ShowGuideMessage();
        }

        private void ShowGuideMessage()
        {
            _guideBody.text = GuideMessages[_guideIndex];
            _guideRoot.SetActive(true);
        }

        private void ConfirmGuideMessage()
        {
            _guideIndex++;
            if (_guideIndex < GuideMessages.Length)
            {
                ShowGuideMessage();
                return;
            }

            _guideRoot.SetActive(false);
            TriggerObjectiveDialogue();
        }

        private void TriggerObjectiveDialogue()
        {
            if (DialogueManager.Instance == null || DialogueManager.Instance.IsActive)
            {
                UnlockControls();
                return;
            }

            _rin = Resources.Load<Character>("Characters/Rin");
            DialogueManager.Instance.TriggerDialogue(BuildGraph());
        }

        private DialogueGraph BuildGraph()
        {
            var graph = ScriptableObject.CreateInstance<DialogueGraph>();
            graph.name = "TutorialIntro";
            graph.hideFlags = HideFlags.HideAndDontSave;
            graph.entryNodeId = "objective";
            graph.nodes = new List<DialogueNode>
            {
                new()
                {
                    id          = "objective",
                    nodeType    = NodeType.Line,
                    speaker     = _rin,
                    literalText = "For now, I should head to the Ryotei restaurant for the welcome banquet.",
                    nextNodeId  = "end",
                    onEnter     = new UnityEvent(),
                    onExit      = new UnityEvent(),
                },
                new()
                {
                    id       = "end",
                    nodeType = NodeType.Terminal,
                    onEnter  = new UnityEvent(),
                    onExit   = new UnityEvent(),
                },
            };
            graph.nodes[1].onEnter.AddListener(UnlockControls);

            graph.BuildLookup();
            return graph;
        }

        private void LockControls()
        {
            if (_controlsLocked)
                return;

            _controlsLocked = true;
            _playerMovement = FindFirstObjectByType<PlayerMovement>();
            _playerInteractor = FindFirstObjectByType<PlayerInteractor>();
            _playerMovement?.SetExternalMovementLocked(true);
            _playerInteractor?.SetExternalInteractionLocked(true);
            MinimapController.Instance?.SetExternalToggleLocked(true);
        }

        private void UnlockControls()
        {
            if (!_controlsLocked)
                return;

            _controlsLocked = false;
            _playerMovement?.SetExternalMovementLocked(false);
            _playerInteractor?.SetExternalInteractionLocked(false);
            MinimapController.Instance?.SetExternalToggleLocked(false);
        }

        private void BuildGuidePopup()
        {
            if (_guideRoot != null)
                return;

            var canvasObject = new GameObject(
                "RyoteiTutorialGuideCanvas",
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);

            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 300;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            _guideRoot = MakeRect(canvasObject.transform, "GuidePopup", Vector2.zero, Vector2.one);
            _guideRoot.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.58f);

            var panel = MakeRect(
                _guideRoot.transform,
                "Panel",
                new Vector2(0.24f, 0.31f),
                new Vector2(0.76f, 0.69f));
            panel.AddComponent<Image>().color = new Color32(0x4b, 0x2f, 0x20, 0xF2);

            MakeRect(panel.transform, "Line", new Vector2(0.04f, 0.91f), new Vector2(0.96f, 0.935f))
                .AddComponent<Image>().color = new Color32(0xc9, 0x9b, 0x65, 0xFF);

            var title = MakeText(
                panel.transform,
                "Title",
                "Village guide",
                28f,
                new Color32(0xf0, 0xd7, 0xa5, 0xFF),
                TextAlignmentOptions.Center,
                new Vector2(0.06f, 0.73f),
                new Vector2(0.94f, 0.91f));
            title.fontStyle = FontStyles.Bold;

            _guideBody = MakeText(
                panel.transform,
                "Body",
                string.Empty,
                21f,
                new Color32(0xf7, 0xea, 0xc9, 0xFF),
                TextAlignmentOptions.Center,
                new Vector2(0.10f, 0.30f),
                new Vector2(0.90f, 0.70f));
            _guideBody.lineSpacing = 10f;

            var okayObject = MakeRect(
                panel.transform,
                "OkayButton",
                new Vector2(0.39f, 0.07f),
                new Vector2(0.61f, 0.24f));
            var okayBackground = okayObject.AddComponent<Image>();
            okayBackground.color = new Color32(0xe9, 0xce, 0x9c, 0xFF);

            var okayButton = okayObject.AddComponent<Button>();
            okayButton.targetGraphic = okayBackground;
            okayButton.onClick.AddListener(ConfirmGuideMessage);

            var okayText = MakeText(
                okayObject.transform,
                "Text",
                "Got it",
                20f,
                new Color32(0x52, 0x36, 0x25, 0xFF),
                TextAlignmentOptions.Center,
                Vector2.zero,
                Vector2.one);
            okayText.fontStyle = FontStyles.Bold;

            _guideRoot.SetActive(false);
        }

        private static GameObject MakeRect(Transform parent, string name,
                                           Vector2 anchorMin, Vector2 anchorMax)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            var rect = (RectTransform)gameObject.transform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return gameObject;
        }

        private static TMP_Text MakeText(Transform parent, string name, string value, float size,
                                         Color color, TextAlignmentOptions alignment,
                                         Vector2 anchorMin, Vector2 anchorMax)
        {
            var textObject = MakeRect(parent, name, anchorMin, anchorMax);
            var text = textObject.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = size;
            text.color = color;
            text.alignment = alignment;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.raycastTarget = false;
            return text;
        }
    }
}
