using System;
using System.Collections.Generic;
using UnityEngine;

namespace DialogueSystem.Data
{
    /// <summary>
    /// Editor-only positional metadata for each node in the graph canvas.
    /// Stored on DialogueGraph inside an #if UNITY_EDITOR block so it
    /// strips cleanly from builds.
    /// </summary>
    [Serializable]
    public class DialogueNodeEditorData
    {
        [Tooltip("Matches DialogueNode.id")]
        public string nodeId;

        [Tooltip("Canvas position of the node's top-left corner.")]
        public Vector2 position;

        public DialogueNodeEditorData() { }

        public DialogueNodeEditorData(string nodeId, Vector2 position)
        {
            this.nodeId   = nodeId;
            this.position = position;
        }
    }
}
