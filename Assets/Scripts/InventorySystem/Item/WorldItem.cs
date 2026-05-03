using InventorySystem.Data;
using UnityEngine;

namespace InventorySystem
{
    /// <summary>
    /// Attaches to a scene pickup object. On trigger with the player collider,
    /// adds <see cref="item"/> to <see cref="InventoryManager"/> and destroys itself.
    ///
    /// SETUP
    ///   • Add a Collider2D (set Is Trigger = true).
    ///   • Assign an ItemData asset in the Inspector.
    ///   • Tag the player GameObject as "Player".
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class WorldItem : MonoBehaviour
    {
        [Tooltip("The item this pickup represents.")]
        public ItemData item;

        [Tooltip("How many units to add when collected.")]
        [Min(1)]
        public int amount = 1;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;

            if (InventoryManager.Instance == null)
            {
                Debug.LogError("[WorldItem] No InventoryManager found in scene.");
                return;
            }

            InventoryManager.Instance.Add(item, amount);
            Destroy(gameObject);
        }
    }
}
