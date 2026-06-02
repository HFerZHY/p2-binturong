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
            "Press <b>E</b> (or click the journal icon in the top right) to open my inventory and journal.",
            "As I explore, I'll find items with stories behind them. Each item has a history — I should read carefully.",
            "Learning about items unlocks <b>Inspirations</b>: fragments of Otowa's cultural knowledge.",
            "Inspirations connect into <b>Themes</b>, which form the basis of the museum exhibition at the station.",
            "That's the real goal — curating a meaningful exhibition. The stories I learn here will shape it.",
            "Press <b>Tab</b> or click the map icon (top left) to open the map and see where characters and locations are.",
            "For now, I should head to the Ryotei restaurant for the welcome banquet. (Look for the red marker.)",
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
