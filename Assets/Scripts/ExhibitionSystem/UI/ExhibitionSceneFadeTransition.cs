using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ExhibitionSystem.UI
{
    public static class ExhibitionSceneFadeTransition
    {
        private const float FadeDuration = 0.65f;

        public static IEnumerator FadeOutAndLoad(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
                yield break;

            var canvasObject = new GameObject(
                "ExhibitionSceneFadeCanvas",
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster),
                typeof(CanvasGroup));

            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;

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
            while (elapsed < FadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Clamp01(elapsed / FadeDuration);
                yield return null;
            }

            canvasGroup.alpha = 1f;
            SceneManager.LoadScene(sceneName);
        }
    }
}
