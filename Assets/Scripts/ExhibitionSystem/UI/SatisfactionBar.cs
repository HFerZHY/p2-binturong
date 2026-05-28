using ExhibitionSystem.Core;
using ExhibitionSystem.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ExhibitionSystem.UI
{
    /// <summary>
    /// Displays the visitor satisfaction progress bar.
    /// Shows current satisfaction vs threshold.
    /// </summary>
    public class SatisfactionBar : MonoBehaviour
    {
        // ── Serialized Fields ───────────────────────────────────────────────────

        [Header("UI References")]
        [SerializeField] private Slider _slider;
        [SerializeField] private Image _fillImage;
        [SerializeField] private TMP_Text _valueText;
        [SerializeField] private TMP_Text _labelText;

        [Header("Threshold Marker")]
        [SerializeField] private RectTransform _thresholdMarker;

        [Header("Colors")]
        [SerializeField] private Color _belowThresholdColor = new(0.9f, 0.3f, 0.3f, 1f);
        [SerializeField] private Color _aboveThresholdColor = new(0.3f, 0.9f, 0.3f, 1f);
        [SerializeField] private Color _neutralColor = new(0.7f, 0.7f, 0.7f, 1f);

        [Header("Animation")]
        [SerializeField] private float _animationSpeed = 5f;
        [SerializeField] private float _colorTransitionSpeed = 3f;

        // ── Runtime State ───────────────────────────────────────────────────────

        private int _maxValue;
        private int _threshold;
        private int _targetValue;
        private float _displayValue;
        private Color _targetColor;
        private Color _currentColor;

        // ── Unity Lifecycle ─────────────────────────────────────────────────────

        private void OnEnable()
        {
            ExhibitionManager.OnThemeSelected += HandleThemeSelected;
            ExhibitionManager.OnInspirationsConfirmed += HandleInspirationsConfirmed;
            ExhibitionManager.OnExhibitionStarted += HandleExhibitionStarted;
            ExhibitionManager.OnVisitorReacted += HandleVisitorReacted;
            ExhibitionManager.OnExhibitionEnded += HandleExhibitionEnded;
            ExhibitionManager.OnCurationCleared += HandleCurationCleared;
        }

        private void OnDisable()
        {
            ExhibitionManager.OnThemeSelected -= HandleThemeSelected;
            ExhibitionManager.OnInspirationsConfirmed -= HandleInspirationsConfirmed;
            ExhibitionManager.OnExhibitionStarted -= HandleExhibitionStarted;
            ExhibitionManager.OnVisitorReacted -= HandleVisitorReacted;
            ExhibitionManager.OnExhibitionEnded -= HandleExhibitionEnded;
            ExhibitionManager.OnCurationCleared -= HandleCurationCleared;
        }

        private void Update()
        {
            // Smooth value animation
            if (Mathf.Abs(_displayValue - _targetValue) > 0.01f)
            {
                _displayValue = Mathf.MoveTowards(_displayValue, _targetValue, _animationSpeed * Time.deltaTime);
                UpdateSlider(_displayValue);
            }

            // Smooth color transition
            if (_fillImage != null && _currentColor != _targetColor)
            {
                _currentColor = Color.Lerp(_currentColor, _targetColor, _colorTransitionSpeed * Time.deltaTime);
                _fillImage.color = _currentColor;
            }
        }

        // ── Public Methods ──────────────────────────────────────────────────────

        /// <summary>
        /// Sets the current satisfaction value.
        /// </summary>
        public void SetValue(int current)
        {
            _targetValue = Mathf.Clamp(current, 0, _maxValue);
            UpdateValueText(_targetValue);
        }

        /// <summary>
        /// Initializes the bar for a new theme.
        /// </summary>
        public void Initialize(int maxSlots, int threshold)
        {
            _maxValue = maxSlots;
            _threshold = threshold;
            _targetValue = 0;
            _displayValue = 0;

            if (_slider != null)
            {
                _slider.minValue = 0;
                _slider.maxValue = maxSlots;
                _slider.value = 0;
            }

            UpdateThresholdMarker();
            UpdateSlider(0);
            UpdateValueText(0);

            // Initialize color state
            _currentColor = _neutralColor;
            _targetColor = _neutralColor;
            if (_fillImage != null)
                _fillImage.color = _currentColor;

            if (_labelText != null)
                _labelText.text = "Visitor Satisfaction";
        }

        /// <summary>
        /// Resets the bar to initial state.
        /// </summary>
        public void Reset()
        {
            _targetValue = 0;
            _displayValue = 0;

            if (_slider != null)
                _slider.value = 0;

            UpdateSlider(0);
            UpdateValueText(0);

            // Reset color to neutral
            _currentColor = _neutralColor;
            _targetColor = _neutralColor;
            if (_fillImage != null)
                _fillImage.color = _currentColor;
        }

        // ── Event Handlers ──────────────────────────────────────────────────────

        private void HandleThemeSelected(ExhibitionTheme theme)
        {
            if (theme == null) return;

            Initialize(theme.requiredInspirations, theme.SuccessThreshold);
        }

        private void HandleInspirationsConfirmed(System.Collections.Generic.IReadOnlyList<InspirationData> inspirations)
        {
            var manager = ExhibitionManager.Instance;
            var theme = manager?.CurrentTheme;
            if (theme == null) return;

            Initialize(inspirations.Count, inspirations.Count);
        }

        private void HandleExhibitionStarted()
        {
            Reset();
        }

        private void HandleVisitorReacted(int slotIndex, InspirationData inspiration, ExhibitItemData item, bool isCorrect, int satisfaction)
        {
            SetValue(satisfaction);
            UpdateColor();
        }

        private void HandleExhibitionEnded(bool success, int satisfaction, int threshold)
        {
            if (threshold > 0)
                _maxValue = threshold;

            SetValueImmediate(satisfaction);
            UpdateColor();
        }

        private void HandleCurationCleared()
        {
            Initialize(0, 0);
        }

        // ── Private Methods ─────────────────────────────────────────────────────

        private void UpdateSlider(float value)
        {
            if (_slider != null)
                _slider.value = value;
        }

        private void UpdateValueText(int value)
        {
            if (_valueText != null)
                _valueText.text = $"{Mathf.Clamp(value, 0, _maxValue)} / {_maxValue}";
        }

        private void SetValueImmediate(int current)
        {
            int clampedValue = Mathf.Clamp(current, 0, _maxValue);
            _targetValue = clampedValue;
            _displayValue = clampedValue;
            UpdateSlider(_displayValue);
            UpdateValueText(clampedValue);
        }

        private void UpdateColor()
        {
            // Set target color for smooth transition
            if (_targetValue >= _threshold)
                _targetColor = _aboveThresholdColor;
            else if (_targetValue > 0)
                _targetColor = _belowThresholdColor;
            else
                _targetColor = _neutralColor;
        }

        private void UpdateThresholdMarker()
        {
            if (_thresholdMarker == null || _slider == null) return;

            // Position marker at threshold point on slider
            float normalizedThreshold = _maxValue > 0 ? (float)_threshold / _maxValue : 0;

            var sliderRect = _slider.GetComponent<RectTransform>();
            if (sliderRect != null)
            {
                float width = sliderRect.rect.width;
                float xPos = normalizedThreshold * width - width / 2;

                _thresholdMarker.anchoredPosition = new Vector2(xPos, _thresholdMarker.anchoredPosition.y);
            }
        }
    }
}
