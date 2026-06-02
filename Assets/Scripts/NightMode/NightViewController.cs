using UnityEngine;

/// <summary>
/// Data and API component for the night view effect.
/// Does NOT need to be on the Camera in URP — place it on any active GameObject.
/// The actual rendering is done by NightViewRendererFeature + NightViewPass.
///
/// Setup:
///   1. Add NightViewRendererFeature to your URP Renderer asset.
///   2. Add this component to any GameObject in the scene (e.g. a "Managers" object).
///   3. Adjust nightColor and nightAlpha in the Inspector.
/// </summary>
[ExecuteAlways]
public class NightViewController : MonoBehaviour
{
    // ──────────────────────────────────────────────────────────────
    //  Singleton
    // ──────────────────────────────────────────────────────────────
    public static NightViewController Instance { get; private set; }

    // ──────────────────────────────────────────────────────────────
    //  Inspector fields
    // ──────────────────────────────────────────────────────────────
    [Header("Night Overlay")]
    [Tooltip("Base color of the night tint. Keep it dark and blueish.")]
    public Color nightColor = new Color(0.04f, 0.06f, 0.18f, 1f);

    [Range(0f, 1f)]
    [Tooltip("Darkness of the overlay. 0 = no effect, 1 = pitch black.")]
    public float nightAlpha = 0.85f;

    [Header("Transition")]
    [Tooltip("When enabled, nightAlpha smoothly moves toward targetNightAlpha each frame.")]
    public bool useTransition = false;

    [Range(0f, 1f)]
    public float targetNightAlpha = 0.85f;

    [Min(0.01f)]
    public float transitionSpeed = 1f;

    // ──────────────────────────────────────────────────────────────
    //  Unity messages
    // ──────────────────────────────────────────────────────────────
    private void OnEnable()
    {
        if (Instance != null && Instance != this)
            Debug.LogWarning("[NightViewController] Multiple instances detected. " +
                             "Only one should be active at a time.", this);
        Instance = this;
    }

    private void OnDisable()
    {
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        if (!useTransition) return;
        nightAlpha = Mathf.MoveTowards(nightAlpha, targetNightAlpha,
                                       transitionSpeed * Time.deltaTime);
    }

    // ──────────────────────────────────────────────────────────────
    //  Public API
    // ──────────────────────────────────────────────────────────────

    /// <summary>Smoothly fade to the given darkness level.</summary>
    /// <param name="alpha">Target darkness (0 = day, 1 = pitch black).</param>
    /// <param name="speed">Units per second.</param>
    public void FadeTo(float alpha, float speed = 1f)
    {
        targetNightAlpha = Mathf.Clamp01(alpha);
        transitionSpeed  = Mathf.Max(0.01f, speed);
        useTransition    = true;
    }

    /// <summary>Instantly set darkness with no transition.</summary>
    public void SetAlphaImmediate(float alpha)
    {
        nightAlpha       = Mathf.Clamp01(alpha);
        targetNightAlpha = nightAlpha;
        useTransition    = false;
    }

    /// <summary>Toggle between day and night with a smooth fade.</summary>
    public void Toggle(float speed = 1.5f)
    {
        if (nightAlpha > 0.01f)
            FadeTo(0f, speed);
        else
            FadeTo(targetNightAlpha > 0.01f ? targetNightAlpha : 0.85f, speed);
    }
}
