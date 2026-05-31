using System.Collections.Generic;
using DialogueSystem.Data;
using UnityEngine;
using UnityEngine.Events;

namespace Otowa.Inquiry
{
    public static class Day1MapDialogueFactory
    {
        public static DialogueGraph CreateRinThought(string graphName, string text)
        {
            var graph = ScriptableObject.CreateInstance<DialogueGraph>();
            graph.name = graphName;
            graph.hideFlags = HideFlags.HideAndDontSave;
            graph.entryNodeId = "line";
            graph.nodes = new List<DialogueNode>
            {
                new()
                {
                    id = "line",
                    nodeType = NodeType.Line,
                    speaker = Resources.Load<Character>("Characters/Rin"),
                    literalText = text,
                    nextNodeId = "end",
                    onEnter = new UnityEvent(),
                    onExit = new UnityEvent(),
                },
                new()
                {
                    id = "end",
                    nodeType = NodeType.Terminal,
                    onEnter = new UnityEvent(),
                    onExit = new UnityEvent(),
                },
            };
            graph.BuildLookup();
            return graph;
        }
    }
}
