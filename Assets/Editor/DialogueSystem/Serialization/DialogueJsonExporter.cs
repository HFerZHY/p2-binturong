using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using DialogueSystem.Data;

namespace DialogueSystem.Serialization
{
    /// <summary>
    /// Converts a DialogueGraph ScriptableObject back to the JSON schema
    /// understood by DialogueJsonLoader.
    /// </summary>
    public static class DialogueJsonExporter
    {
        /// <summary>
        /// Serializes <paramref name="graph"/> to a JSON string.
        /// </summary>
        public static string ToJson(DialogueGraph graph, bool prettyPrint = true)
        {
            if (graph == null) throw new ArgumentNullException(nameof(graph));

            var dto = new DialogueGraphJson
            {
                entryNodeId = graph.entryNodeId,
                nodes       = new List<DialogueNodeJson>()
            };

            foreach (var node in graph.nodes)
            {
                var nodeDto = new DialogueNodeJson
                {
                    id                 = node.id,
                    nodeType           = node.nodeType.ToString(),
                    speakerName        = node.speaker.name,
                    speakerPortraitKey = node.speakerPortraitKey,
                    textKey            = node.textKey,
                    nextNodeId         = node.nextNodeId,
                    typewriterSpeed    = node.typewriterSpeed,
                    npcAnimatorTrigger = node.npcAnimatorTrigger,
                    choices            = new List<DialogueChoiceJson>(),
                    conditions         = ConvertConditions(node.conditions)
                };

                foreach (var choice in node.choices)
                {
                    nodeDto.choices.Add(new DialogueChoiceJson
                    {
                        labelKey     = choice.labelKey,
                        targetNodeId = choice.targetNodeId,
                        showIfFailed = choice.showIfFailed,
                        conditions   = ConvertConditions(choice.conditions)
                    });
                }

                dto.nodes.Add(nodeDto);
            }

            return JsonUtility.ToJson(dto, prettyPrint);
        }

        /// <summary>
        /// Writes the graph to <paramref name="absolutePath"/> as a .json file.
        /// </summary>
        public static void WriteToFile(DialogueGraph graph, string absolutePath, bool prettyPrint = true)
        {
            string json = ToJson(graph, prettyPrint);
            File.WriteAllText(absolutePath, json, System.Text.Encoding.UTF8);
            Debug.Log($"[DialogueJsonExporter] Exported '{graph.name}' → {absolutePath}");
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static List<DialogueConditionJson> ConvertConditions(List<DialogueCondition> conditions)
        {
            var result = new List<DialogueConditionJson>();
            if (conditions == null) return result;

            foreach (var c in conditions)
            {
                result.Add(new DialogueConditionJson
                {
                    type   = c.type.ToString(),
                    key    = c.key,
                    value  = c.value,
                    negate = c.negate
                });
            }
            return result;
        }
    }
}
