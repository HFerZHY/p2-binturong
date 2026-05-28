using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ExhibitionSystem.UI
{
    /// <summary>
    /// Keeps a RectTransform in a saved reference coordinate space while fitting it to its parent.
    /// Child RectTransforms can be hand-positioned in edit mode and will keep their relative
    /// placement when the Game view size changes.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class ReferenceRectScaler : MonoBehaviour
    {
        [SerializeField] private Vector2 _referenceSize;
        [SerializeField] private bool _captureParentSizeWhenUnset = true;

        private RectTransform _rectTransform;
        private bool _isApplying;

        public Vector2 ReferenceSize => _referenceSize;

        private void OnEnable()
        {
            ApplyLayout();
        }

        private void OnValidate()
        {
            ApplyLayout();
        }

        private void Update()
        {
            if (!Application.isPlaying)
                ApplyLayout();
        }

        private void LateUpdate()
        {
            if (Application.isPlaying)
                ApplyLayout();
        }

        private void OnRectTransformDimensionsChange()
        {
            ApplyLayout();
        }

        public void SetReferenceSize(Vector2 referenceSize)
        {
            _referenceSize = referenceSize;
            ApplyLayout();
        }

        [ContextMenu("Capture Current Parent Size")]
        public void CaptureCurrentParentSize()
        {
            CacheReferences();
            var parentRect = _rectTransform != null ? _rectTransform.parent as RectTransform : null;
            if (parentRect == null)
                return;

            var parentSize = parentRect.rect.size;
            if (parentSize.x <= 0f || parentSize.y <= 0f)
                return;

            _referenceSize = parentSize;
            ApplyLayout();

#if UNITY_EDITOR
            if (!Application.isPlaying)
                EditorUtility.SetDirty(this);
#endif
        }

        private void ApplyLayout()
        {
            if (_isApplying)
                return;

            CacheReferences();
            if (_rectTransform == null)
                return;

            var parentRect = _rectTransform.parent as RectTransform;
            if (parentRect == null)
                return;

            if ((_referenceSize.x <= 0f || _referenceSize.y <= 0f) && _captureParentSizeWhenUnset)
            {
                var parentSize = parentRect.rect.size;
                if (parentSize.x <= 0f || parentSize.y <= 0f)
                    return;

                _referenceSize = parentSize;
            }

            if (_referenceSize.x <= 0f || _referenceSize.y <= 0f)
                return;

            var parentRectSize = parentRect.rect.size;
            if (parentRectSize.x <= 0f || parentRectSize.y <= 0f)
                return;

            float scale = Mathf.Min(parentRectSize.x / _referenceSize.x, parentRectSize.y / _referenceSize.y);
            if (scale <= 0f)
                return;

            _isApplying = true;
            SetVector2IfChanged(value => _rectTransform.anchorMin = value, _rectTransform.anchorMin, new Vector2(0.5f, 0.5f));
            SetVector2IfChanged(value => _rectTransform.anchorMax = value, _rectTransform.anchorMax, new Vector2(0.5f, 0.5f));
            SetVector2IfChanged(value => _rectTransform.pivot = value, _rectTransform.pivot, new Vector2(0.5f, 0.5f));
            SetVector2IfChanged(value => _rectTransform.anchoredPosition = value, _rectTransform.anchoredPosition, Vector2.zero);
            SetVector2IfChanged(value => _rectTransform.sizeDelta = value, _rectTransform.sizeDelta, _referenceSize);

            var targetScale = new Vector3(scale, scale, 1f);
            if (!Approximately(_rectTransform.localScale, targetScale))
                _rectTransform.localScale = targetScale;

            _isApplying = false;
        }

        private void CacheReferences()
        {
            if (_rectTransform == null)
                _rectTransform = GetComponent<RectTransform>();
        }

        private static void SetVector2IfChanged(System.Action<Vector2> setter, Vector2 current, Vector2 target)
        {
            if (!Approximately(current, target))
                setter(target);
        }

        private static bool Approximately(Vector2 a, Vector2 b)
        {
            return Mathf.Abs(a.x - b.x) <= 0.001f && Mathf.Abs(a.y - b.y) <= 0.001f;
        }

        private static bool Approximately(Vector3 a, Vector3 b)
        {
            return Mathf.Abs(a.x - b.x) <= 0.001f &&
                   Mathf.Abs(a.y - b.y) <= 0.001f &&
                   Mathf.Abs(a.z - b.z) <= 0.001f;
        }
    }
}
