using UnityEngine;
using DialogueSystem.Core;

namespace DialogueSystem
{
    /// <summary>
    /// Scene bootstrapper — wires the DialogueManager to its condition evaluator.
    /// Place on a persistent GameObject alongside (or as a child of) DialogueManager.
    ///
    /// In a larger project this responsibility would live in your GameManager or
    /// a dedicated DI container. This class keeps the dialogue system self-contained
    /// for easy drop-in use.
    /// </summary>
    public class DialogueGameBootstrapper : MonoBehaviour
    {
        [Header("Optional game-system providers")]
        [Tooltip("MonoBehaviour that implements IQuestFlagProvider. Leave null to skip quest-flag conditions.")]
        [SerializeField] private MonoBehaviour questFlagProviderBehaviour;

        [Tooltip("MonoBehaviour that implements IInventoryProvider. Leave null to skip inventory conditions.")]
        [SerializeField] private MonoBehaviour inventoryProviderBehaviour;

        [Tooltip("MonoBehaviour that implements IRelationshipProvider. Leave null to skip relationship conditions.")]
        [SerializeField] private MonoBehaviour relationshipProviderBehaviour;

        private void Start()
        {
            if (DialogueManager.Instance == null)
            {
                Debug.LogError("[DialogueBootstrapper] DialogueManager.Instance is null. " +
                               "Make sure a DialogueManager is present and initialized before this Start().");
                return;
            }

            var questFlags    = questFlagProviderBehaviour    as IQuestFlagProvider;
            var inventory     = inventoryProviderBehaviour    as IInventoryProvider;
            var relationships = relationshipProviderBehaviour as IRelationshipProvider;

            var evaluator = new DefaultConditionEvaluator(questFlags, inventory, relationships);

            // Example: register a custom evaluator for a specific flag key
            // evaluator.RegisterCustom("PlayerIsSneaking", c => PlayerController.IsSneaking);

            DialogueManager.Instance.RegisterConditionEvaluator(evaluator);

            Debug.Log("[DialogueBootstrapper] Dialogue system initialized.");
        }
    }
}
