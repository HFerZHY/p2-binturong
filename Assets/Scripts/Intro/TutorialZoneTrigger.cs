using System.Collections.Generic;
using DialogueSystem.Core;
using DialogueSystem.Data;
using UnityEngine;
using UnityEngine.Events;

namespace Otowa.Intro
{
    /// <summary>
    /// Place on an invisible trigger zone in TutorialToRyotei. Fires a one-time
    /// contextual tip when the player walks into the zone.
    /// Requires a Collider2D (set Is Trigger = true) on the same GameObject.
    /// Set the Player tag on the player GameObject.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class TutorialZoneTrigger : MonoBehaviour
    {
        [Tooltip("Lines Rin will think when entering this zone.")]
        [SerializeField] private string[] lines = { "Tip goes here." };

        private bool _triggered;
        private bool _playerInZone;
        private Character _rin;

        private void Awake()
        {
            GetComponent<Collider2D>().isTrigger = true;
            _rin = Resources.Load<Character>("Characters/Rin");
        }

        private void Update()
        {
            if (_playerInZone && !_triggered)
                TryFire();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;
            _playerInZone = true;
            TryFire();
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;
            _playerInZone = false;
        }

        private void TryFire()
        {
            if (_triggered) return;
            if (DialogueManager.Instance == null || DialogueManager.Instance.IsActive) return;
            _triggered = true;
            DialogueManager.Instance.TriggerDialogue(BuildGraph());
        }

        private DialogueGraph BuildGraph()
        {
            var graph = ScriptableObject.CreateInstance<DialogueGraph>();
            graph.name = $"TutorialZone_{name}";
            graph.hideFlags = HideFlags.HideAndDontSave;
            graph.entryNodeId = "line_01";
            graph.nodes = new List<DialogueNode>();

            for (int i = 0; i < lines.Length; i++)
            {
                string nodeId = $"line_{i + 1:00}";
                string nextId = i + 1 < lines.Length ? $"line_{i + 2:00}" : "end";
                graph.nodes.Add(new DialogueNode
                {
                    id          = nodeId,
                    nodeType    = NodeType.Line,
                    speaker     = _rin,
                    literalText = lines[i],
                    nextNodeId  = nextId,
                    onEnter     = new UnityEvent(),
                    onExit      = new UnityEvent(),
                });
            }

            graph.nodes.Add(new DialogueNode
            {
                id       = "end",
                nodeType = NodeType.Terminal,
                onEnter  = new UnityEvent(),
                onExit   = new UnityEvent(),
            });

            graph.BuildLookup();
            return graph;
        }
    }
}
