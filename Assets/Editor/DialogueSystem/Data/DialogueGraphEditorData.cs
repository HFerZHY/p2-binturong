using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DialogueSystem.Data
{
    /// <summary>
    /// A companion ScriptableObject that stores editor-only layout data
    /// (node canvas positions) alongside a DialogueGraph asset.
    /// 
    /// Saved as "GraphName_EditorData.asset" next to the graph asset.
    /// Excluded from builds via the Editor/ folder convention.
    /// </summary>
    public class DialogueGraphEditorData : ScriptableObject
    {
        [Tooltip("Positional data for every node, keyed by node id.")]
        public List<DialogueNodeEditorData> nodePositions = new();

        public Vector2 GetPosition(string nodeId)
        {
            var entry = nodePositions.FirstOrDefault(e => e.nodeId == nodeId);
            return entry?.position ?? Vector2.zero;
        }

        public void SetPosition(string nodeId, Vector2 position)
        {
            var entry = nodePositions.FirstOrDefault(e => e.nodeId == nodeId);
            if (entry != null)
                entry.position = position;
            else
                nodePositions.Add(new DialogueNodeEditorData(nodeId, position));
        }

        public void RemovePosition(string nodeId)
        {
            nodePositions.RemoveAll(e => e.nodeId == nodeId);
        }

        public void Clear() => nodePositions.Clear();
    }
}
