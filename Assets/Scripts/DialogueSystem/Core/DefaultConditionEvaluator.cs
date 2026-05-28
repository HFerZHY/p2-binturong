using System.Collections.Generic;
using UnityEngine;
using DialogueSystem.Data;
using DialogueSystem.Interfaces;
using InventorySystem;


namespace DialogueSystem.Core
{
    /// <summary>
    /// Default IConditionEvaluator.
    ///
    /// Supports: QuestFlag, HasItem, RelationshipMin, CustomEvaluator.
    /// Replace or extend this with your own game-state lookups.
    ///
    /// Register via:
    ///   DialogueManager.Instance.RegisterConditionEvaluator(new DefaultConditionEvaluator(...));
    /// </summary>
    public class DefaultConditionEvaluator : IConditionEvaluator
    {
        // ── Game-state accessors (injected at construction) ───────────────────

        private readonly IQuestFlagProvider _questFlags;
        private readonly IInventoryProvider _inventory;
        private readonly IRelationshipProvider _relationships;

        // ── Custom evaluators registered at runtime ───────────────────────────

        private readonly Dictionary<string, System.Func<DialogueCondition, bool>> _customEvaluators = new();

        public DefaultConditionEvaluator(
            IQuestFlagProvider questFlags = null,
            IInventoryProvider inventory = null,
            IRelationshipProvider relationships = null)
        {
            _questFlags = questFlags;
            _inventory = inventory;
            _relationships = relationships;
        }

        // ── Register custom evaluators (keyed by condition.key) ───────────────

        public void RegisterCustom(string key, System.Func<DialogueCondition, bool> evaluator)
            => _customEvaluators[key] = evaluator;

        // ── IConditionEvaluator ───────────────────────────────────────────────

        public bool Evaluate(DialogueCondition condition)
        {
            bool result = EvaluateInternal(condition);
            return condition.negate ? !result : result;
        }

        private bool EvaluateInternal(DialogueCondition condition)
        {
            switch (condition.type)
            {
                case ConditionType.QuestFlag:
                    if (_questFlags == null)
                    {
                        Debug.LogWarning("[ConditionEvaluator] No IQuestFlagProvider registered.");
                        return true;
                    }
                    return _questFlags.GetFlag(condition.key) == condition.value;

                case ConditionType.HasItem:
                    {
                        int qty = int.TryParse(condition.value, out int q) ? q : 1;

                        if (_inventory != null)
                            return _inventory.HasItem(condition.key, qty);

                        if (InventoryManager.Instance != null)
                            return InventoryManager.Instance.HasItem(condition.key, qty);

                        Debug.LogWarning("[ConditionEvaluator] No inventory provider or InventoryManager found.");
                        return false;
                    }

                case ConditionType.RelationshipMin:
                    if (_relationships == null)
                    {
                        Debug.LogWarning("[ConditionEvaluator] No IRelationshipProvider registered.");
                        return true;
                    }
                    int threshold = int.TryParse(condition.value, out int t) ? t : 0;
                    return _relationships.GetRelationship(condition.key) >= threshold;

                case ConditionType.CustomEvaluator:
                    if (_customEvaluators.TryGetValue(condition.key, out var fn))
                        return fn(condition);
                    Debug.LogWarning($"[ConditionEvaluator] No custom evaluator registered for key '{condition.key}'.");
                    return true;

                default:
                    Debug.LogWarning($"[ConditionEvaluator] Unhandled condition type: {condition.type}");
                    return true;
            }
        }
    }

    // =========================================================================
    // Stub interfaces — replace with your actual game systems
    // =========================================================================

    /// <summary>Provides quest flag state. Implement against your quest system.</summary>
    public interface IQuestFlagProvider
    {
        string GetFlag(string key);
    }

    /// <summary>Provides inventory queries. Implement against your inventory system.</summary>
    public interface IInventoryProvider
    {
        bool HasItem(string itemId, int quantity);
    }

    /// <summary>Provides relationship values. Implement against your relationship system.</summary>
    public interface IRelationshipProvider
    {
        int GetRelationship(string npcId);
    }
}
