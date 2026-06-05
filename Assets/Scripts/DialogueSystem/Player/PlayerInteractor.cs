using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using DialogueSystem.Core;
using DialogueSystem.Interfaces;
using DialogueSystem.UI;
using Otowa.SaveSystem;
using UnityEngine.Serialization;

namespace DialogueSystem.Player
{
    /// <summary>
    /// Attached to the Player GameObject.
    ///
    /// Responsibilities:
    ///   1. Maintain a list of IInteractables currently in range (via trigger collider).
    ///   2. On [Interact] InputAction → interact with the closest eligible target,
    ///      OR advance dialogue if a conversation is already running.
    ///   3. Show/hide the interaction prompt via IDialogueView.
    ///
    /// Setup:
    ///   - Add a trigger sphere/capsule Collider to a child GameObject on the Player.
    ///   - Wire the InteractAction in the Inspector (PlayerInput component or direct reference).
    ///   - Set the interactableLayerMask to the layer your NPCs/interactables live on.
    /// </summary>
    // [RequireComponent(typeof(PlayerInput))]
    public class PlayerInteractor : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Detection")]
        [Tooltip("Radius used to find the closest interactable each frame (physics overlap).")]
        [SerializeField] private float interactRadius = 2.5f;

        [Tooltip("Layers that contain interactable objects.")]
        [SerializeField] private LayerMask interactableLayerMask;

        [Tooltip("Point from which the overlap sphere is cast (defaults to this transform).")]
        [SerializeField] private Transform interactOrigin;

        [Header("Input")]
        [Tooltip("Name of the InputAction as defined in your InputActionAsset (e.g. 'Interact').")]
        [SerializeField] private string interactActionName = "Interact";

        // ── Private state ─────────────────────────────────────────────────────

        // private PlayerInput _playerInput;
        private InputAction _interactAction;
        private IInteractable _closestInteractable;
        private bool _externalInteractionLocked;

        // Collider buffer — avoids GC allocs each physics step
        private readonly Collider2D[] _overlapBuffer = new Collider2D[16];

        // ── Unity lifecycle ───────────────────────────────────────────────────

        private void Awake()
        {
            // _playerInput = GetComponent<PlayerInput>();
            _interactAction = InputSystem.actions.FindAction(interactActionName);

            if (_interactAction == null)
                Debug.LogError($"[PlayerInteractor] InputAction '{interactActionName}' not found in asset.");

            if (interactOrigin == null)
                interactOrigin = transform;
        }

        private void Start()
        {
            Debug.Log("Interact Add");
            _interactAction.performed += OnInteractPerformed;
            _interactAction.performed += (InputAction.CallbackContext ctx) => {Debug.Log("Heyo");};
        }

        private void OnDestroy()
        {
            _interactAction.performed -= OnInteractPerformed;
        }

        private void Update()
        {
            UpdateClosestInteractable();
        }

        // ── Interact input handler ────────────────────────────────────────────

        private void OnInteractPerformed(InputAction.CallbackContext ctx)
        {
            if (PauseMenuController.ShouldSuppressWorldAdvance)
                return;

            if (_externalInteractionLocked)
                return;

            Debug.Log("Interact");
            // DialogueUIController owns dialogue advance keys once a conversation is open.
            if (DialogueManager.Instance != null && DialogueManager.Instance.IsActive)
            {
                return;
            }
            Debug.Log("No Dialogue Manager");
            // Otherwise, interact with the closest valid target
            if (_closestInteractable != null && _closestInteractable.CanInteract)
            {
                TurnPlayerTowardNpc(_closestInteractable);
                _closestInteractable.Interact(gameObject);
            }
        }

        private void TurnPlayerTowardNpc(IInteractable interactable)
        {
            if (interactable is not Component targetComponent)
                return;

            if (targetComponent.GetComponent<NPCMovement>() == null)
                return;

            var movement = GetComponent<PlayerMovement>();
            movement?.TurnToward(targetComponent.transform.position);
        }

        // ── Proximity scan ────────────────────────────────────────────────────

        private void UpdateClosestInteractable()
        {
            if (_externalInteractionLocked)
            {
                if (_closestInteractable != null)
                {
                    _closestInteractable = null;
                    DialogueManager.Instance?.ShowInteractPrompt(null);
                }
                return;
            }

            // Don't update the prompt while mid-dialogue
            if (DialogueManager.Instance != null && DialogueManager.Instance.IsActive)
                return;
            ContactFilter2D contactFilter = new();
            contactFilter.SetLayerMask(interactableLayerMask);
            contactFilter.useTriggers = true;
            int count = Physics2D.OverlapCircle(interactOrigin.position, interactRadius, contactFilter, _overlapBuffer);
            
            IInteractable best = null;
            float bestDist = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                var interactable = _overlapBuffer[i].GetComponentInParent<IInteractable>();
                if (interactable == null || !interactable.CanInteract) continue;

                float dist = Vector3.Distance(interactOrigin.position,
                                              _overlapBuffer[i].transform.position);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = interactable;
                }
            }

            // Only update UI when the nearest interactable changes
            if (best != _closestInteractable)
            {
                _closestInteractable = best;
                
                DialogueManager.Instance.ShowInteractPrompt(_closestInteractable);
            }
        }

        public void SetExternalInteractionLocked(bool locked)
        {
            _externalInteractionLocked = locked;
            if (!locked || _closestInteractable == null)
                return;

            _closestInteractable = null;
            DialogueManager.Instance?.ShowInteractPrompt(null);
        }

        // ── Gizmos ────────────────────────────────────────────────────────────

        private void OnDrawGizmosSelected()
        {
            if (interactOrigin == null) return;
            Gizmos.color = new Color(0f, 1f, 0.5f, 0.25f);
            Gizmos.DrawSphere(interactOrigin.position, interactRadius);
            Gizmos.color = new Color(0f, 1f, 0.5f, 1f);
            Gizmos.DrawWireSphere(interactOrigin.position, interactRadius);
        }
    }
}
