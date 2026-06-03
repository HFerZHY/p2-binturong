using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using DialogueSystem.Data;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine.Serialization;

namespace DialogueSystem.Serialization
{
    /// <summary>
    /// Loads a DialogueGraph from JSON at runtime.
    /// </summary>
    public static class DialogueJsonLoader
    {
#if UNITY_EDITOR
        [MenuItem("Tools/DialogueGraph/Create From JSON")]
        public static void CreateFromJson()
        {
            string absolutePath = EditorUtility.OpenFilePanel("Select JSON", "", "json");
            if (string.IsNullOrEmpty(absolutePath)) return;

            string filePath = AbsoluteToResourcesPath(absolutePath);
            if (string.IsNullOrEmpty(filePath))
            {
                Debug.LogError($"Path {absolutePath} is not in Resources folder");
                return;
            }

            DialogueGraph graph = LoadFromResources(filePath);
            if (graph is null)
            {
                Debug.LogError("JSON load failed");
                return;
            }
            string assetPath = EditorUtility.SaveFilePanelInProject(
                "Save Graph Asset",
                "NewGraph",
                "asset",
                "Choose location"
            );

            if (!string.IsNullOrEmpty(assetPath))
            {
                AssetDatabase.CreateAsset(graph, assetPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }
#endif
        
        // ── Load from Resources ───────────────────────────────────────────────

        /// <summary>
        /// Load a graph from path.json
        /// e.g. LoadFromResources("Guard/GuardPatrol")
        /// </summary>
        public static DialogueGraph LoadFromResources(string resourcePath)
        {
            var asset = Resources.Load<TextAsset>(resourcePath);
            if (asset == null)
            {
                Debug.LogError($"[DialogueJsonLoader] Could not find TextAsset at {resourcePath}");
                return null;
            }
            return ParseJson(asset.text, resourcePath);
        }

        // ── Load from raw JSON string ─────────────────────────────────────────

        /// <summary>Parse a graph directly from a JSON string (e.g. received from a server).</summary>
        public static DialogueGraph ParseJson(string json, string debugName = "unknown")
        {
            if (string.IsNullOrEmpty(json))
            {
                Debug.LogError("[DialogueJsonLoader] JSON string is null or empty.");
                return null;
            }

            try
            {
                var dto = JsonUtility.FromJson<DialogueGraphJson>(json);
                return ConvertToGraph(dto, debugName);
            }
            catch (Exception e)
            {
                Debug.LogError($"[DialogueJsonLoader] Failed to parse JSON for '{debugName}': {e.Message}");
                return null;
            }
        }

        // ── DTO → ScriptableObject ────────────────────────────────────────────

        private static DialogueGraph ConvertToGraph(DialogueGraphJson dto, string debugName)
        {
            var graph = ScriptableObject.CreateInstance<DialogueGraph>();
            graph.name = debugName;
            graph.entryNodeId = dto.entryNodeId;
            graph.nodes = new List<DialogueNode>();

            if (dto.nodes == null) return graph;

            foreach (var n in dto.nodes)
            {
                Character speaker = Resources.Load<Character>(n.speakerName);
                var node = new DialogueNode
                {
                    id              = n.id,
                    nodeType        = ParseEnum<NodeType>(n.nodeType, NodeType.Line),
                    speaker         = speaker,
                    textKey         = n.textKey,
                    nextNodeId      = n.nextNodeId,
                    typewriterSpeed = n.typewriterSpeed,
                    npcAnimatorTrigger = n.npcAnimatorTrigger
                };

                // Choices
                if (n.choices != null)
                {
                    foreach (var c in n.choices)
                    {
                        var choice = new DialogueChoice
                        {
                            labelKey        = c.labelKey,
                            targetNodeId = c.targetNodeId,
                            showIfFailed = c.showIfFailed,
                            conditions   = ConvertConditions(c.conditions)
                        };
                        node.choices.Add(choice);
                    }
                }

                // Conditions
                node.conditions = ConvertConditions(n.conditions);

                graph.nodes.Add(node);
            }

            graph.BuildLookup();
            return graph;
        }

        private static List<DialogueCondition> ConvertConditions(List<DialogueConditionJson> dtos)
        {
            var result = new List<DialogueCondition>();
            if (dtos == null) return result;

            foreach (var d in dtos)
            {
                result.Add(new DialogueCondition
                {
                    type   = ParseEnum<ConditionType>(d.type, ConditionType.QuestFlag),
                    key    = d.key,
                    value  = d.value,
                    negate = d.negate
                });
            }
            return result;
        }

        private static T ParseEnum<T>(string value, T fallback) where T : struct
        {
            if (Enum.TryParse<T>(value, true, out var result)) return result;
            Debug.LogWarning($"[DialogueJsonLoader] Could not parse enum '{typeof(T).Name}' from '{value}', using {fallback}.");
            return fallback;
        }
        
        public static string AbsoluteToResourcesPath(string absolutePath)
        {
            if (string.IsNullOrEmpty(absolutePath))
                return string.Empty;

            // Normalize slashes (important on Windows)
            string normalized = absolutePath.Replace("\\", "/");

            // Find the LAST occurrence of "/Resources/"
            // (handles nested Resources folders correctly)
            const string marker = "/Resources/";
            int index = normalized.LastIndexOf(marker);

            if (index < 0)
                return string.Empty;

            // Extract path after "Resources/"
            string relative = normalized.Substring(index + marker.Length);

            if (string.IsNullOrEmpty(relative))
                return string.Empty;

            // Remove file extension
            relative = Path.ChangeExtension(relative, null);

            // Final safety cleanup (just in case)
            relative = relative.Replace("\\", "/");

            return relative;
        }
        
    }

    // =========================================================================
    // JSON DTOs (plain serializable classes, no Unity dependencies)
    // =========================================================================

    [Serializable]
    public class DialogueGraphJson
    {
        public string entryNodeId;
        public List<DialogueNodeJson> nodes;
    }

    [Serializable]
    public class DialogueNodeJson
    {
        public string id;
        public string nodeType = "Line";
        public string speakerName;
        public string speakerPortraitKey;
        public string textKey;
        public string nextNodeId;
        public float  typewriterSpeed = 0.03f;
        public string npcAnimatorTrigger;
        public List<DialogueChoiceJson>    choices;
        public List<DialogueConditionJson> conditions;
    }

    [Serializable]
    public class DialogueChoiceJson
    {
        public string labelKey;
        public string targetNodeId;
        public bool   showIfFailed;
        public List<DialogueConditionJson> conditions;
    }

    [Serializable]
    public class DialogueConditionJson
    {
        public string type;
        public string key;
        public string value;
        public bool   negate;
    }
}
