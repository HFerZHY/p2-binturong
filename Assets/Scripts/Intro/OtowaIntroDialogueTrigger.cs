using System.Collections.Generic;
using DialogueSystem.Core;
using DialogueSystem.Data;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace Otowa.Intro
{
    /// <summary>
    /// Drop on any GameObject in Day1World (e.g. near the ticket office).
    /// On scene load, auto-triggers the Junko introduction dialogue using the
    /// world map's DialogueManager popup system instead of a cutscene.
    /// Loads nextSceneName when the conversation ends.
    /// </summary>
    [DefaultExecutionOrder(-500)]
    public class OtowaIntroDialogueTrigger : MonoBehaviour
    {
        [SerializeField] private bool autoTriggerOnStart = true;
        [SerializeField] private string nextSceneName = "Intro-3";

        private Character _junko;
        private Character _rin;

        private struct DialogueLine
        {
            public string Speaker;
            public string Text;
        }

        private static readonly DialogueLine[] Lines =
        {
            new() { Speaker = "Junko", Text = "You must be tired from your journey. You are Rin, right? I am Junko, the chief of Otowa village." },
            new() { Speaker = "Rin",   Text = "Hello, it's nice to meet you. The air here is so nice, completely different from the city." },
            new() { Speaker = "Junko", Text = "I am relieved to hear you say that. Welcome to Otowa." },
            new() { Speaker = "Rin",   Text = "Thank you. Excuse me, is Mr. Hikaru here? We agreed on the phone to hand over the work today." },
            new() { Speaker = "Junko", Text = "Ah... regarding that, I am truly very sorry." },
            new() { Speaker = "Rin",   Text = "What's wrong? Is he not in the village?" },
            new() { Speaker = "Junko", Text = "Yesterday afternoon, he suddenly packed a bag and left. He said there was something extremely important he had to go take care of immediately." },
            new() { Speaker = "Rin",   Text = "Something important?" },
            new() { Speaker = "Junko", Text = "Yes, though he didn't disclose exactly what it was to us. He left in quite a hurry." },
            new() { Speaker = "Rin",   Text = "He left... Then what about the work at the station?" },
            new() { Speaker = "Junko", Text = "So, for the next few days until Hikaru returns, I'm afraid we will have to impose on you to temporarily act as the acting stationmaster here." },
            new() { Speaker = "Rin",   Text = "Me, as the acting stationmaster? But I can't lead a train station!" },
            new() { Speaker = "Junko", Text = "Please don't feel too much pressure, Rin. We rarely get any trains stopping here anyway. Just think of it as taking a short vacation." },
            new() { Speaker = "Rin",   Text = "He mentioned on the phone that this job would be a bit challenging... Is this what he meant?" },
            new() { Speaker = "Junko", Text = "Sigh, that boy is always like this, doing things purely on impulse. He really has caused you a lot of trouble." },
            new() { Speaker = "Junko", Text = "To express our apologies, and to welcome your arrival, we've prepared a welcome banquet for you tonight at the village Ryotei." },
            new() { Speaker = "Junko", Text = "Please do me the honor of attending. It's been a long time since a young person came to the village; everyone is looking forward to meeting you." },
            new() { Speaker = "Rin",   Text = "Thank you for the invitation, I will be there." },
            new() { Speaker = "Junko", Text = "Well then, I will leave the stationmaster's office key with you. You can go settle in first. See you tonight." },
            new() { Speaker = "Rin",   Text = "Alright, see you tonight." },
        };

        private void Start()
        {
            if (autoTriggerOnStart)
                TriggerIntroDialogue();
        }

        public void TriggerIntroDialogue()
        {
            if (DialogueManager.Instance == null)
            {
                Debug.LogWarning("[OtowaIntroDialogueTrigger] DialogueManager not found in scene.");
                return;
            }

            if (DialogueManager.Instance.IsActive) return;

            _junko = Resources.Load<Character>("Characters/Junko");
            _rin   = Resources.Load<Character>("Characters/Rin");

            if (_junko == null) Debug.LogWarning("[OtowaIntroDialogueTrigger] Characters/Junko asset not found.");
            if (_rin   == null) Debug.LogWarning("[OtowaIntroDialogueTrigger] Characters/Rin asset not found.");

            DialogueManager.OnConversationEnded += HandleConversationEnded;
            DialogueManager.Instance.TriggerDialogue(BuildGraph());
        }

        private void HandleConversationEnded()
        {
            DialogueManager.OnConversationEnded -= HandleConversationEnded;
            if (!string.IsNullOrEmpty(nextSceneName))
                SceneManager.LoadScene(nextSceneName);
        }

        private DialogueGraph BuildGraph()
        {
            var graph = ScriptableObject.CreateInstance<DialogueGraph>();
            graph.name       = "OtowaIntroDialogue";
            graph.hideFlags  = HideFlags.HideAndDontSave;
            graph.entryNodeId = "line_01";
            graph.nodes      = new List<DialogueNode>();

            for (int i = 0; i < Lines.Length; i++)
            {
                string nodeId = $"line_{i + 1:00}";
                string nextId = i + 1 < Lines.Length ? $"line_{i + 2:00}" : "end";

                graph.nodes.Add(new DialogueNode
                {
                    id          = nodeId,
                    nodeType    = NodeType.Line,
                    speaker     = Lines[i].Speaker == "Junko" ? _junko : _rin,
                    literalText = Lines[i].Text,
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
