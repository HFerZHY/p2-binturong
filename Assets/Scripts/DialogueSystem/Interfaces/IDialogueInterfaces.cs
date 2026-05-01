using System.Collections.Generic;
using DialogueSystem.Data;

namespace DialogueSystem.Interfaces
{
    // =========================================================================
    // IInteractable
    // =========================================================================
    /// <summary>
    /// Attach to any GameObject the player can interact with (NPCs, doors, items).
    /// The PlayerInteractor calls Interact() when the player presses the interact key.
    /// </summary>
    public interface IInteractable
    {
        /// <summary>Whether the player can currently interact with this object.</summary>
        bool CanInteract { get; }

        /// <summary>Called by the PlayerInteractor when the player triggers interaction.</summary>
        void Interact(UnityEngine.GameObject initiator);

        /// <summary>Optional prompt text shown in the UI (e.g. "[E] Talk").</summary>
        string InteractPrompt { get; }
    }

    // =========================================================================
    // INPCDialogueProvider
    // =========================================================================
    /// <summary>
    /// Implemented by NPCs to supply their dialogue graph to the DialogueManager.
    /// </summary>
    public interface INPCDialogueProvider
    {
        /// <summary>Returns the graph to use for the current conversation context.</summary>
        DialogueGraph GetDialogueGraph();

        /// <summary>The Animator on this NPC, so the manager can fire animation triggers.</summary>
        UnityEngine.Animator NPCAnimator { get; }
    }
    
    // =========================================================================
    // IConditionEvaluator
    // =========================================================================
    /// <summary>
    /// Evaluates a DialogueCondition against current game state.
    /// Register a concrete implementation on the DialogueManager at startup.
    /// </summary>
    public interface IConditionEvaluator
    {
        bool Evaluate(DialogueCondition condition);
    }

    // =========================================================================
    // IDialogueTrigger
    // =========================================================================
    /// <summary>
    /// Implemented by anything that can start a dialogue from outside the normal
    /// player-interaction path (cutscene director, quest system, etc.).
    /// </summary>
    public interface IDialogueTrigger
    {
        void TriggerDialogue(DialogueGraph graph);
    }
}
