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
            return CreateRinThought(graphName, new[] { text });
        }

        public static DialogueGraph CreateRinThought(string graphName, IReadOnlyList<string> lines)
        {
            var graph = ScriptableObject.CreateInstance<DialogueGraph>();
            graph.name = graphName;
            graph.hideFlags = HideFlags.HideAndDontSave;
            graph.entryNodeId = "line_01";
            graph.nodes = new List<DialogueNode>();

            for (int i = 0; i < lines.Count; i++)
            {
                graph.nodes.Add(new DialogueNode
                {
                    id = $"line_{i + 1:00}",
                    nodeType = NodeType.Line,
                    speaker = Resources.Load<Character>("Characters/Rin"),
                    literalText = lines[i],
                    nextNodeId = i + 1 < lines.Count ? $"line_{i + 2:00}" : "end",
                    onEnter = new UnityEvent(),
                    onExit = new UnityEvent(),
                });
            }

            graph.nodes.Add(new DialogueNode
            {
                id = "end",
                nodeType = NodeType.Terminal,
                onEnter = new UnityEvent(),
                onExit = new UnityEvent(),
            });
            graph.BuildLookup();
            return graph;
        }
    }
}
