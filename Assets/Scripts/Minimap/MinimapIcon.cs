using UnityEngine;

namespace Otowa.Minimap
{
    /// <summary>
    /// Attach to any character (player or NPC) to make them appear on the minimap.
    /// Auto-registers with MinimapController when enabled.
    /// </summary>
    public class MinimapIcon : MonoBehaviour
    {
        [SerializeField] private Sprite portrait;
        [SerializeField] private Color iconColor = Color.white;
        [SerializeField] private string displayName;

        public Sprite Portrait => portrait;
        public Color IconColor => iconColor;
        public string DisplayName => displayName;

        private void OnEnable()
        {
            MinimapController.Instance?.Register(this);
        }

        private void OnDisable()
        {
            MinimapController.Instance?.Unregister(this);
        }
    }
}
