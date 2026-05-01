using UnityEngine;
using DialogueSystem.Core;
using DialogueSystem.Data;
using DialogueSystem.Interfaces;

namespace DialogueSystem.NPC
{
    /// <summary>
    /// Attach to an NPC GameObject to make it interactable and dialogue-capable.
    ///
    /// Implements both IInteractable (detected by PlayerInteractor)
    /// and INPCDialogueProvider (supplies graph data to DialogueManager).
    ///
    /// Setup:
    ///   - Assign a DialogueGraph asset in the Inspector.
    ///   - Ensure the GameObject (or its trigger collider child) is on the
    ///     interactable layer used by PlayerInteractor.
    ///   - Optionally assign the NPC's Animator for talking animations.
    /// </summary>
    public class NPCDialogueController : MonoBehaviour, IInteractable, INPCDialogueProvider
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Dialogue")]
        [Tooltip("The conversation this NPC will run when interacted with.")]
        [SerializeField] private DialogueGraph dialogueGraph;

        [Header("Interaction")]
        [Tooltip("Text shown in the interaction prompt (e.g. '[E] Talk').")]
        [SerializeField] private string interactPrompt = "[E] Talk";

        [Tooltip("Set false to temporarily disable interaction (e.g. mid-cutscene).")]
        [SerializeField] private bool canInteract = true;

        [Header("Animation")]
        [Tooltip("The NPC's Animator. Used by DialogueManager to fire talking triggers.")]
        [SerializeField] private Animator npcAnimator;

        [Tooltip("Trigger name fired when the player starts talking to this NPC.")]
        [SerializeField] private string talkStartTrigger = "TalkStart";

        [Tooltip("Trigger name fired when the conversation ends.")]
        [SerializeField] private string talkEndTrigger = "TalkEnd";

        // ── IInteractable ─────────────────────────────────────────────────────

        public bool CanInteract => canInteract && dialogueGraph != null
                                   && (DialogueManager.Instance == null || !DialogueManager.Instance.IsActive);

        public string InteractPrompt => interactPrompt;

        public void Interact(GameObject initiator)
        {
            if (!CanInteract) return;

            if (DialogueManager.Instance == null)
            {
                Debug.LogError("[NPCDialogueController] DialogueManager.Instance is null.");
                return;
            }

            // Fire talking animation on this NPC
            if (npcAnimator != null && !string.IsNullOrEmpty(talkStartTrigger))
                npcAnimator.SetTrigger(talkStartTrigger);

            // Subscribe to end so we can fire TalkEnd animation
            DialogueManager.OnConversationEnded += HandleConversationEnded;

            DialogueManager.Instance.StartConversation(this, initiator);
        }

        private void HandleConversationEnded()
        {
            DialogueManager.OnConversationEnded -= HandleConversationEnded;

            if (npcAnimator != null && !string.IsNullOrEmpty(talkEndTrigger))
                npcAnimator.SetTrigger(talkEndTrigger);
        }

        // ── INPCDialogueProvider ──────────────────────────────────────────────

        public DialogueGraph GetDialogueGraph() => dialogueGraph;

        public Animator NPCAnimator => npcAnimator;

        // ── Runtime API ───────────────────────────────────────────────────────

        /// <summary>Swap the dialogue graph at runtime (e.g. after a quest progresses).</summary>
        public void SetDialogueGraph(DialogueGraph graph) => dialogueGraph = graph;

        /// <summary>Enable or disable interaction at runtime.</summary>
        public void SetCanInteract(bool value) => canInteract = value;
    }
}
