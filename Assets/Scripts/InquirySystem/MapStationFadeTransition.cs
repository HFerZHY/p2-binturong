using System.Collections;
using Otowa.Audio;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Otowa.Inquiry
{
    public static class MapStationFadeTransition
    {
        public const float Duration = 2f;

        public static IEnumerator FadeOutAndLoad(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
                yield break;

            GameAudioManager.Instance.StopBgm(Duration);

            var canvasObject = new GameObject(
                "MapStationFadeCanvas",
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster),
                typeof(CanvasGroup));

            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10000;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            var canvasGroup = canvasObject.GetComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;

            var overlayObject = new GameObject("BlackOverlay", typeof(RectTransform), typeof(Image));
            var overlayRect = overlayObject.GetComponent<RectTransform>();
            overlayRect.SetParent(canvasObject.transform, false);
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;

            var overlay = overlayObject.GetComponent<Image>();
            overlay.color = Color.black;
            overlay.raycastTarget = true;

            var elapsed = 0f;
            while (elapsed < Duration)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Clamp01(elapsed / Duration);
                yield return null;
            }

            canvasGroup.alpha = 1f;
            SceneManager.LoadScene(sceneName);
        }
    }
}
