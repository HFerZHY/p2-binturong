using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace DialogueSystem.Data
{
    /// <summary>
    /// A single node in a dialogue graph.
    /// Can represent a spoken line, a branch point, or a terminal end.
    /// </summary>
    [Serializable]
    public class DialogueNode
    {
        [Tooltip("Unique identifier for this node. Used by choices to point to their target.")]
        public string id;

        [Tooltip("The type of node — drives how DialogueManager processes it.")]
        public NodeType nodeType = NodeType.Line;

        [FormerlySerializedAs("speakerName")]
        [Header("Content")]
        [Tooltip("Localization string table key. Points to the name displayed in the dialogue UI (e.g. 'Guard', 'Merchant').")]
        public string speakerNameKey;

        [Tooltip("Portrait sprite shown alongside the dialogue line.")]
        public Sprite speakerPortrait;

        [FormerlySerializedAs("text")]
        [TextArea(3, 6)]
        [Tooltip("Localization string table key. Points to the dialogue text.")]
        public string textKey;

        [Header("Flow")]
        [Tooltip("ID of the next node to play after this one. Leave empty on choice/terminal nodes.")]
        public string nextNodeId;

        [Tooltip("Player choices shown when nodeType == Branch.")]
        public List<DialogueChoice> choices = new();

        [Header("Conditions")]
        [Tooltip("All conditions must pass for this node to be reached.")]
        public List<DialogueCondition> conditions = new();

        [Header("Events")]
        [Tooltip("Fired when this node is entered.")]
        public UnityEvent onEnter;

        [Tooltip("Fired when this node is exited (before advancing).")]
        public UnityEvent onExit;

        [Header("Presentation")]
        [Tooltip("Seconds per character for the typewriter effect. 0 = instant.")]
        public float typewriterSpeed = 0.03f;

        [Tooltip("Audio clip played when this line is spoken.")]
        public AudioClip voiceClip;

        [Tooltip("Animator trigger name to fire on the NPC when this node plays.")]
        public string npcAnimatorTrigger;
    }

    // -------------------------------------------------------------------------

    [Serializable]
    public class DialogueChoice
    {
        [Tooltip("Text shown on the choice button.")]
        public string label;

        [Tooltip("ID of the node to jump to when this choice is selected.")]
        public string targetNodeId;

        [Tooltip("Optional conditions — if any fail the choice is hidden/greyed out.")]
        public List<DialogueCondition> conditions = new();

        [Tooltip("If true and conditions fail, the choice is shown greyed out rather than hidden.")]
        public bool showIfFailed = false;
    }

    // -------------------------------------------------------------------------

    [Serializable]
    public class DialogueCondition
    {
        public ConditionType type;
        public string key;       // e.g. quest flag name, item ID
        public string value;     // expected value or threshold (parsed per type)
        public bool negate;      // invert the result
    }

    // -------------------------------------------------------------------------

    public enum NodeType
    {
        Line,       // Single spoken line, advances to nextNodeId
        Branch,     // Presents choices to the player
        Terminal    // Ends the conversation
    }

    public enum ConditionType
    {
        QuestFlag,          // GameState.GetFlag(key) == value
        HasItem,            // Inventory.HasItem(key, int.Parse(value))
        RelationshipMin,    // RelationshipSystem.Get(key) >= int.Parse(value)
        CustomEvaluator     // Defers to a registered IConditionEvaluator
    }
}
