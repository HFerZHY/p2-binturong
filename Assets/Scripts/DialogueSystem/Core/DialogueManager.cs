using System;
using System.Collections.Generic;
using Base;
using UnityEngine;
using DialogueSystem.Data;
using DialogueSystem.Interfaces;
using DialogueSystem.UI;
using InventorySystem;

namespace DialogueSystem.Core
{
    /// <summary>
    /// Central singleton that owns the active conversation state.
    /// NPCs hand their DialogueGraph to this manager and step back.
    ///
    /// Fully event-driven — no coroutines, no per-frame polling.
    ///
    /// State machine:
    ///   Idle ──StartConversation──► Displaying
    ///                                   │
    ///                     onTypingComplete callback
    ///                                   │
    ///                                   ▼
    ///                            WaitingForInput
    ///                            ╱              ╲
    ///               AdvanceDialogue()      OnChoiceSelected()
    ///                          │                  │
    ///                          └──────┬───────────┘
    ///                                 ▼
    ///                           ProcessNode()
    ///                          ╱             ╲
    ///                    Line/Branch        Terminal
    ///                    (Displaying)      EndConversation()
    ///                                           │
    ///                                          Idle
    /// </summary>
    public class DialogueManager : MonoSingleton<DialogueManager>, IDialogueTrigger
    {
        // ── Singleton ─────────────────────────────────────────────────────────

        // public static DialogueManager Instance { get; private set; }

        // ── Dependencies ──────────────────────────────────────────────────────

        [Header("Dependencies")]
        [Tooltip("The UI controller that implements IDialogueView.")]
        [SerializeField] private DialogueUIController dialogueUIController;
        
        private IConditionEvaluator _conditionEvaluator;

        // ── Runtime state ─────────────────────────────────────────────────────

        private DialogueGraph _activeGraph;
        private DialogueNode _currentNode;
        private Animator _npcAnimator;
        private ConversationState _state = ConversationState.Idle;
        private readonly HashSet<string> _rewardedNodeIds = new();
        private readonly HashSet<string> _inspirationUnlockedNodeIds = new();

        // ── Static events ─────────────────────────────────────────────────────

        public static event Action OnConversationStarted;
        public static event Action OnConversationEnded;
        public static event Action<DialogueNode> OnNodeEntered;
        public static event Action<string> OnActionRequested;

        // ── Public API ────────────────────────────────────────────────────────

        public bool IsActive => _state != ConversationState.Idle;

        // ── Unity lifecycle ───────────────────────────────────────────────────

        // private void Awake()
        // {
        //     if (Instance != null && Instance != this)
        //     {
        //         Destroy(gameObject);
        //         return;
        //     }
        //     Instance = this;
        //     DontDestroyOnLoad(gameObject);
        // }

        // ── Dependency registration ───────────────────────────────────────────

        /// <summary>Register the condition evaluator (call from your GameManager or bootstrapper).</summary>
        public void RegisterConditionEvaluator(IConditionEvaluator evaluator)
            => _conditionEvaluator = evaluator;

        // ── IDialogueTrigger ──────────────────────────────────────────────────

        public void TriggerDialogue(DialogueGraph graph)
            => BeginConversation(graph, null);

        // ── Start / End ───────────────────────────────────────────────────────

        /// <summary>Begin a conversation from an NPC provider.</summary>
        public void StartConversation(INPCDialogueProvider provider, GameObject initiator)
        {
            if (IsActive) return;
            BeginConversation(provider.GetDialogueGraph(), provider.NPCAnimator);
        }

        private void BeginConversation(DialogueGraph graph, Animator npcAnimator)
        {
            if (IsActive) return;

            if (graph == null)
            {
                Debug.LogWarning("[DialogueManager] Attempted to start conversation with null graph.");
                return;
            }

            _activeGraph = graph;
            _activeGraph.BuildLookup();
            _npcAnimator = npcAnimator;
            _rewardedNodeIds.Clear();
            _inspirationUnlockedNodeIds.Clear();

            var entry = _activeGraph.GetEntryNode();
            if (entry == null)
            {
                Debug.LogError("[DialogueManager] Entry node not found in graph.");
                return;
            }

            // Lock state before processing the first node so IsActive returns
            // true immediately — even before ProcessNode sets Displaying.
            _state = ConversationState.Displaying;
            OnConversationStarted?.Invoke();
            ProcessNode(entry);
        }

        public void EndConversation()
        {
            dialogueUIController?.HideUI();
            dialogueUIController?.HideChoices();

            _activeGraph = null;
            _currentNode = null;
            _npcAnimator = null;

            // Set Idle last so any OnConversationEnded listener
            // that checks IsActive still sees the conversation as active.
            _state = ConversationState.Idle;
            OnConversationEnded?.Invoke();
        }

        // ── AdvanceDialogue (keyboard / gamepad / continue button) ────────────

        /// <summary>
        /// Called by PlayerInteractor (input action) or the continue Button in the UI.
        /// - Displaying + typewriter running → skip to end of line.
        /// - WaitingForInput → advance to the next node.
        /// Branch nodes are advanced exclusively via OnChoiceSelected; this
        /// method is a no-op while choices are visible.
        /// </summary>
        public void AdvanceDialogue()
        {
            // Let the UI finish or turn the current page before moving to the next node.
            if (dialogueUIController != null && dialogueUIController.TryAdvancePage())
                return;

            switch (_state)
            {
                case ConversationState.WaitingForInput:
                    _currentNode.onExit?.Invoke();
                    ProcessNode(ResolveNextNode(_currentNode));
                    return;

                // Ignore presses during Branch (choices visible) or Idle.
            }
        }

        // ── Core node processor ───────────────────────────────────────────────

        /// <summary>
        /// Entry point for every node transition. Sets up the node, fires its
        /// enter events, and delegates to the appropriate handler.
        /// Returns immediately — all further progress is driven by callbacks.
        /// </summary>
        private void ProcessNode(DialogueNode node)
        {
            if (node == null)
            {
                EndConversation();
                return;
            }

            if (!EvaluateConditions(node.conditions))
            {
                Debug.LogWarning($"[DialogueManager] Node '{node.id}' conditions failed. Ending conversation.");
                EndConversation();
                return;
            }

            // Fire NPC animation trigger declared on this node.
            if (_npcAnimator != null && !string.IsNullOrEmpty(node.npcAnimatorTrigger))
                _npcAnimator.SetTrigger(node.npcAnimatorTrigger);

            GrantItemRewards(node);
            UnlockInspirations(node);
            node.onEnter?.Invoke();
            OnNodeEntered?.Invoke(node);
            _currentNode = node;

            switch (node.nodeType)
            {
                case NodeType.Line:
                    ProcessLineNode(node);
                    break;

                case NodeType.Branch:
                    ProcessBranchNode(node);
                    break;

                case NodeType.Terminal:
                    node.onExit?.Invoke();
                    EndConversation();
                    break;
            }
        }

        // ── Line node ─────────────────────────────────────────────────────────

        private void ProcessLineNode(DialogueNode node)
        {
            _state = ConversationState.Displaying;

            // When typing finishes the view fires this callback — no polling.
            dialogueUIController.ShowLine(node, onTypingComplete: () =>
            {
                _state = ConversationState.WaitingForInput;
                // Player must now call AdvanceDialogue() to proceed.
            });
        }

        // ── Branch node ───────────────────────────────────────────────────────

        private void ProcessBranchNode(DialogueNode node)
        {
            _state = ConversationState.Branch;
            dialogueUIController.ShowChoices(BuildFilteredChoices(node.choices), OnChoiceSelected);
        }

        // ── Choice callback ───────────────────────────────────────────────────

        private void OnChoiceSelected(DialogueChoice choice)
        {
            if (_state != ConversationState.Branch) return;

            if (choice == null)
                return;

            if (!string.IsNullOrEmpty(choice.actionKey))
            {
                string actionKey = choice.actionKey;
                dialogueUIController.HideChoices();
                _currentNode.onExit?.Invoke();
                EndConversation();
                OnActionRequested?.Invoke(actionKey);
                return;
            }

            if (string.IsNullOrEmpty(choice.targetNodeId))
            {
                Debug.LogWarning($"[DialogueManager] Choice '{choice.labelKey}' has no targetNodeId. Ending conversation.");
                dialogueUIController.HideChoices();
                _currentNode.onExit?.Invoke();
                EndConversation();
                return;
            }

            if (!TryConsumeChoiceCosts(choice))
            {
                Debug.LogWarning($"[DialogueManager] Could not consume item costs for choice '{choice.labelKey}'.");
                return;
            }

            dialogueUIController.HideChoices();
            _currentNode.onExit?.Invoke();

            ProcessNode(_activeGraph.GetNode(choice.targetNodeId));
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private DialogueNode ResolveNextNode(DialogueNode node)
        {
            if (string.IsNullOrEmpty(node.nextNodeId)) return null;
            return _activeGraph.GetNode(node.nextNodeId);
        }

        private void GrantItemRewards(DialogueNode node)
        {
            if (node == null || node.itemRewards == null || node.itemRewards.Count == 0)
                return;

            if (node.grantItemRewardsOnlyOnce && _rewardedNodeIds.Contains(node.id))
                return;

            if (InventoryManager.Instance == null)
            {
                Debug.LogError("[DialogueManager] No InventoryManager found in scene.");
                return;
            }

            foreach (var reward in node.itemRewards)
            {
                if (reward == null || reward.item == null || reward.amount <= 0)
                    continue;

                InventoryManager.Instance.Add(reward.item, reward.amount);
            }

            if (node.grantItemRewardsOnlyOnce)
                _rewardedNodeIds.Add(node.id);
        }

        private void UnlockInspirations(DialogueNode node)
        {
            if (node == null || node.inspirationUnlockIds == null || node.inspirationUnlockIds.Count == 0)
                return;

            if (node.unlockInspirationsOnlyOnce && _inspirationUnlockedNodeIds.Contains(node.id))
                return;

            foreach (int id in node.inspirationUnlockIds)
            {
                if (id < 1 || id > 16)
                {
                    Debug.LogWarning($"[DialogueManager] Inspiration ID {id} on node '{node.id}' is out of range. Expected 1–16.");
                    continue;
                }

                InspirationManager.Instance.Unlock(id);
            }

            if (node.unlockInspirationsOnlyOnce)
                _inspirationUnlockedNodeIds.Add(node.id);
        }

        private bool TryConsumeChoiceCosts(DialogueChoice choice)
        {
            if (choice.itemCosts == null || choice.itemCosts.Count == 0)
                return true;

            if (InventoryManager.Instance == null)
            {
                Debug.LogError("[DialogueManager] No InventoryManager found in scene.");
                return false;
            }

            foreach (var cost in choice.itemCosts)
            {
                if (cost == null || cost.item == null || cost.amount <= 0)
                    continue;

                if (!InventoryManager.Instance.Has(cost.item, cost.amount))
                {
                    Debug.LogWarning(
                        $"[DialogueManager] Missing item cost: {cost.amount}x {cost.item.nameKey}."
                    );
                    return false;
                }
            }

            foreach (var cost in choice.itemCosts)
            {
                if (cost == null || cost.item == null || cost.amount <= 0)
                    continue;

                InventoryManager.Instance.Remove(cost.item, cost.amount);
            }

            return true;
        }

        private List<(DialogueChoice choice, bool interactable)> BuildFilteredChoices(
            List<DialogueChoice> choices)
        {
            var result = new List<(DialogueChoice, bool)>();
            foreach (var c in choices)
            {
                bool pass = EvaluateConditions(c.conditions);
                if (pass)
                    result.Add((c, true));
                else if (c.showIfFailed)
                    result.Add((c, false));
                // else: fully hidden
            }
            return result;
        }

        private bool EvaluateConditions(List<DialogueCondition> conditions)
        {
            if (conditions == null || conditions.Count == 0) return true;
            if (_conditionEvaluator == null)
            {
                Debug.LogWarning("[DialogueManager] No IConditionEvaluator registered.");
                return true;
            }

            foreach (var c in conditions)
                if (!_conditionEvaluator.Evaluate(c)) return false;

            return true;
        }

        // ── Inner types ───────────────────────────────────────────────────────

        private enum ConversationState
        {
            Idle,
            Displaying,      // View is typing out a line
            WaitingForInput, // Line finished; waiting for AdvanceDialogue()
            Branch           // Choices are visible; waiting for OnChoiceSelected()
        }

        public void ShowInteractPrompt(IInteractable interactable)
        {
            if (interactable != null)
                dialogueUIController.ShowInteractPrompt(interactable.InteractPrompt);
            else
                dialogueUIController.HideInteractPrompt();
        }
    }
}
