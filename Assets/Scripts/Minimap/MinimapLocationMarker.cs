using UnityEngine;

namespace Otowa.Minimap
{
    /// <summary>
    /// Attach to any location object (HotSpringEntrance, Ryotei, etc.) to show it
    /// as a labeled marker on the minimap. Auto-registers with MinimapController.
    /// </summary>
    public class MinimapLocationMarker : MonoBehaviour
    {
        [SerializeField] private string locationName  = "Location";
        [SerializeField] private Color  markerColor   = new Color(0.85f, 0.35f, 0.10f);
        [SerializeField] private Sprite markerIcon;
        [SerializeField] private Vector2 markerSize   = new(28f, 28f);

        public string   LocationName => locationName;
        public Color    MarkerColor  => markerColor;
        public Sprite   MarkerIcon   => markerIcon;
        public Vector2  MarkerSize   => markerSize;

        private void OnEnable()  => MinimapController.Instance?.RegisterLocation(this);
        private void OnDisable() => MinimapController.Instance?.UnregisterLocation(this);
    }
}
