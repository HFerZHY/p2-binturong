using System.Collections.Generic;
using DialogueSystem.Core;
using DialogueSystem.Data;
using UnityEngine;
using UnityEngine.Events;

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

        private Character _rin;

        private static readonly string[] TutorialLines =
        {
            "Use <b>WASD</b> to walk around the village. Press <b>Space</b> or click to advance dialogue like this.",
            "Walk up to characters or objects and press <b>SPACE</b> to interact with them.",
            "For now, I should head to the Ryotei restaurant for the welcome banquet.",
        };

        private void Start()
        {
            if (autoTriggerOnStart)
                TriggerTutorial();
        }

        public void TriggerTutorial()
        {
            if (DialogueManager.Instance == null || DialogueManager.Instance.IsActive) return;
            _rin = Resources.Load<Character>("Characters/Rin");
            DialogueManager.Instance.TriggerDialogue(BuildGraph());
        }

        private DialogueGraph BuildGraph()
        {
            var graph = ScriptableObject.CreateInstance<DialogueGraph>();
            graph.name = "TutorialIntro";
            graph.hideFlags = HideFlags.HideAndDontSave;
            graph.entryNodeId = "line_01";
            graph.nodes = new List<DialogueNode>();

            for (int i = 0; i < TutorialLines.Length; i++)
            {
                string nodeId = $"line_{i + 1:00}";
                string nextId = i + 1 < TutorialLines.Length ? $"line_{i + 2:00}" : "end";
                graph.nodes.Add(new DialogueNode
                {
                    id          = nodeId,
                    nodeType    = NodeType.Line,
                    speaker     = _rin,
                    literalText = TutorialLines[i],
                    nextNodeId  = nextId,
                    onEnter     = new UnityEvent(),
                    onExit      = new UnityEvent(),
                });
            }

            graph.nodes.Add(new DialogueNode
            {
                id       = "end",
                nodeType = NodeType.Terminal,
                onEnter  = new UnityEvent(),
                onExit   = new UnityEvent(),
            });

            graph.BuildLookup();
            return graph;
        }
    }
}
