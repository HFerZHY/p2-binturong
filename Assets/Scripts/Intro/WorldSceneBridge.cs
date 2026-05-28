using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ExhibitionSystem.Core;

namespace Otowa.Intro
{
    /// <summary>
    /// Drop one of these on any GameObject in WorldScene.
    ///
    /// Handles:
    ///   • Smooth fade-in from black at the start of the scene
    ///   • Sharing the scene's font with InspirationManager
    ///   • Bridging ExhibitionManager.OnExhibitionEnded → InspirationManager.CompleteTheme
    ///     (for when themes are completed in the Exhibition scene and need to show in the journal)
    ///
    /// Setup: empty GameObject in WorldScene → attach this script.
    /// Optionally assign serifFont to match the journal font.
    /// </summary>
    public class WorldSceneBridge : MonoBehaviour
    {
        [SerializeField] private float        fadeInDuration = 0.6f;
        [SerializeField] private TMP_FontAsset serifFont;

        private void Awake()
        {
            if (serifFont != null)
                InspirationManager.Instance.SetFont(serifFont);
        }

        private void OnEnable()
        {
            ExhibitionManager.OnExhibitionEnded += HandleExhibitionEnded;
        }

        private void OnDisable()
        {
            ExhibitionManager.OnExhibitionEnded -= HandleExhibitionEnded;
        }

        private IEnumerator Start()
        {
            // Build a temporary full-screen black overlay and fade it out.
            var cvGo = new GameObject("FadeInCanvas", typeof(Canvas), typeof(CanvasScaler));
            var cv = cvGo.GetComponent<Canvas>();
            cv.renderMode   = RenderMode.ScreenSpaceOverlay;
            cv.sortingOrder = 199; // just below InspirationManager's 200

            var scaler = cvGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            var overlayGo = new GameObject("BlackOverlay", typeof(RectTransform));
            overlayGo.transform.SetParent(cvGo.transform, false);
            var rt = (RectTransform)overlayGo.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            overlayGo.AddComponent<Image>().color = Color.black;

            var cg = cvGo.AddComponent<CanvasGroup>();
            cg.alpha         = 1f;
            cg.blocksRaycasts = true;

            float t = 0f;
            while (t < fadeInDuration)
            {
                cg.alpha = Mathf.Lerp(1f, 0f, t / fadeInDuration);
                t += Time.deltaTime;
                yield return null;
            }

            cg.alpha          = 0f;
            cg.blocksRaycasts = false;
            Destroy(cvGo);
        }

        private void HandleExhibitionEnded(bool success, int satisfaction, int threshold)
        {
            if (!success) return;
            var theme = ExhibitionManager.Instance?.CurrentTheme;
            if (theme != null)
                InspirationManager.Instance.CompleteTheme(theme.title);
        }
    }
}
