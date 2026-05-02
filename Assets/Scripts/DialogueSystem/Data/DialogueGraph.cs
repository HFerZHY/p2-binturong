using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DialogueSystem.Data;
using UnityEngine.Localization.Tables;

namespace DialogueSystem.Data
{
    /// <summary>
    /// A complete dialogue conversation: a list of nodes and a declared entry point.
    /// Create via: Assets > Create > Dialogue System > Dialogue Graph
    /// </summary>
    [CreateAssetMenu(fileName = "NewDialogueGraph", menuName = "Dialogue System/Dialogue Graph")]
    public class DialogueGraph : ScriptableObject
    {
        [Tooltip("The ID of the node the conversation starts from.")]
        public string entryNodeId;

        [Tooltip("All nodes that make up this conversation.")]
        public List<DialogueNode> nodes = new();

        // ── Lookup helpers ────────────────────────────────────────────────────

        private Dictionary<string, DialogueNode> _lookup;

        private void OnEnable() => BuildLookup();

        public void BuildLookup()
        {
            _lookup = nodes.ToDictionary(n => n.id, n => n);
        }

        /// <summary>Returns the node with the given id, or null.</summary>
        public DialogueNode GetNode(string id)
        {
            if (_lookup == null) BuildLookup();
            _lookup.TryGetValue(id, out var node);
            return node;
        }

        /// <summary>Returns the designated entry node, or null if misconfigured.</summary>
        public DialogueNode GetEntryNode()
        {
            if (string.IsNullOrEmpty(entryNodeId))
            {
                Debug.LogWarning($"[DialogueGraph] '{name}' has no entryNodeId set.", this);
                return null;
            }
            return GetNode(entryNodeId);
        }

#if UNITY_EDITOR
        // Validation helper — called by the custom inspector
        public List<string> Validate()
        {
            var errors = new List<string>();

            if (string.IsNullOrEmpty(entryNodeId))
                errors.Add("entryNodeId is empty.");

            if (nodes.Count == 0)
                errors.Add("Graph has no nodes.");

            var ids = new HashSet<string>();
            foreach (var node in nodes)
            {
                if (string.IsNullOrEmpty(node.id))
                    errors.Add($"A node has an empty id (speaker: '{node.speaker.name}').");
                else if (!ids.Add(node.id))
                    errors.Add($"Duplicate node id: '{node.id}'.");

                if (!string.IsNullOrEmpty(node.nextNodeId) && !_lookup.ContainsKey(node.nextNodeId))
                    errors.Add($"Node '{node.id}' references missing nextNodeId '{node.nextNodeId}'.");

                foreach (var choice in node.choices)
                    if (!string.IsNullOrEmpty(choice.targetNodeId) && !_lookup.ContainsKey(choice.targetNodeId))
                        errors.Add($"Node '{node.id}' choice '{choice.labelKey}' references missing targetNodeId '{choice.targetNodeId}'.");
            }

            // ── Unique-key checks ─────────────────────────────────────────────
            // textKey must be unique across all nodes
            var textKeyCounts = nodes
                .Where(n => !string.IsNullOrEmpty(n.textKey))
                .GroupBy(n => n.textKey)
                .Where(g => g.Count() > 1);

            foreach (var g in textKeyCounts)
                errors.Add($"Duplicate textKey '{g.Key}' found on nodes: " +
                           string.Join(", ", g.Select(n => $"'{n.id}'")));

            // labelKey must be unique across all choices in the graph
            var allChoices = nodes.SelectMany(n => n.choices.Select(c => (NodeId: n.id, Choice: c)));
            var labelKeyCounts = allChoices
                .Where(x => !string.IsNullOrEmpty(x.Choice.labelKey))
                .GroupBy(x => x.Choice.labelKey)
                .Where(g => g.Count() > 1);

            foreach (var g in labelKeyCounts)
                errors.Add($"Duplicate labelKey '{g.Key}' found on choices in nodes: " +
                           string.Join(", ", g.Select(x => $"'{x.NodeId}'")));

            return errors;
        }
#endif
    }
}
