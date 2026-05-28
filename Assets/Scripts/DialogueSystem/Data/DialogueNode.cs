using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using InventorySystem.Data;

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

        [FormerlySerializedAs("speakerName")] [Header("Content")]
        // [Tooltip("Localization string table key. Points to the name displayed in the dialogue UI (e.g. 'Guard', 'Merchant').")]
        // public string speakerNameKey;
        //
        // [Tooltip("Portrait sprite shown alongside the dialogue line.")]
        // public Sprite speakerPortrait;
        public Character speaker;

        public string speakerPortraitKey;

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

        [Header("Item Rewards")]
        [Tooltip("Items granted to the player when this node is entered.")]
        public List<DialogueItemReward> itemRewards = new();

        [Tooltip("If true, this node will only grant its item rewards once per dialogue session.")]
        public bool grantItemRewardsOnlyOnce = true;

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
        [FormerlySerializedAs("label")]
        [Tooltip("Localization string table key. Points to the text shown on the choice button.")]
        public string labelKey;

        [Tooltip("ID of the node to jump to when this choice is selected.")]
        public string targetNodeId;

        [Tooltip("Optional conditions — if any fail the choice is hidden/greyed out.")]
        public List<DialogueCondition> conditions = new();

        [Tooltip("If true and conditions fail, the choice is shown greyed out rather than hidden.")]
        public bool showIfFailed = false;

        [Tooltip("Items removed from the player's inventory when this choice is selected.")]
        public List<DialogueItemCost> itemCosts = new();
    }

    // -------------------------------------------------------------------------

    [Serializable]
    public class DialogueItemReward
    {
        [Tooltip("The item granted when this dialogue node is entered.")]
        public ItemData item;

        [Tooltip("How many units of the item to grant.")]
        [Min(1)]
        public int amount = 1;
    }
    // -------------------------------------------------------------------------

    [Serializable]
    public class DialogueItemCost
    {
        [Tooltip("The item removed when this dialogue choice is selected.")]
        public ItemData item;

        [Tooltip("How many units of the item to remove.")]
        [Min(1)]
        public int amount = 1;
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
