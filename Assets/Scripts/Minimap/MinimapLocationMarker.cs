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

        private Sprite _resolvedLocationPortrait;
        private bool _locationPortraitResolved;

        public string   LocationName => locationName;
        public Color    MarkerColor  => markerColor;
        public Sprite   MarkerIcon   => markerIcon;
        public Sprite   LocationPortrait => ResolveLocationPortrait();
        public Vector2  MarkerSize   => markerSize;

        private void OnEnable()  => MinimapController.Instance?.RegisterLocation(this);
        private void OnDisable() => MinimapController.Instance?.UnregisterLocation(this);

        private Sprite ResolveLocationPortrait()
        {
            if (_locationPortraitResolved)
                return _resolvedLocationPortrait;

            _locationPortraitResolved = true;
            var resourcePath = GetLocationPortraitResourcePath();
            _resolvedLocationPortrait = string.IsNullOrEmpty(resourcePath)
                ? null
                : Resources.Load<Sprite>(resourcePath);
            return _resolvedLocationPortrait;
        }

        private string GetLocationPortraitResourcePath()
        {
            var sceneName = gameObject.scene.name;

            if (sceneName == "TutorialToRyotei" && MatchesLocation("ryotei"))
                return "Characters/WorldSprite/Junko_portrait";

            if ((sceneName == "Day1World" || sceneName == "Day2World")
                && MatchesLocation("hot spring", "hotspring"))
            {
                return "Characters/WorldSprite/Mizuki_portrait";
            }

            if (sceneName == "Day2World" && MatchesLocation("ryotei"))
                return "Characters/WorldSprite/Jiro_portrait";

            return null;
        }

        private bool MatchesLocation(params string[] terms)
        {
            var markerName = $"{locationName} {gameObject.name}".ToLowerInvariant();
            foreach (var term in terms)
            {
                if (markerName.Contains(term))
                    return true;
            }

            return false;
        }
    }
}
