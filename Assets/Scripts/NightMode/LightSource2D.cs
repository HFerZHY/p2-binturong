using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Marks a GameObject as a circular light source for the NightView system.
/// The NightViewPass reads ActiveSources and draws a quad per source into
/// the light mask buffer using LightSource2DBlit.shader.
/// </summary>
[ExecuteAlways]
public class LightSource2D : MonoBehaviour
{
    public static readonly HashSet<LightSource2D> ActiveSources = new HashSet<LightSource2D>();

    [Header("Light Shape")]
    [Min(0.01f)]
    [Tooltip("World-space radius of the full light circle.")]
    public float radius = 2f;

    [Range(0f, 1f)]
    [Tooltip("Fraction of the radius that is fully bright (0 = full falloff, 1 = hard disc).")]
    public float coreRadius = 0.4f;

    [Header("Light")]
    [ColorUsage(false, true)]
    public Color lightColor = Color.white;

    [Range(0f, 1f)]
    public float intensity = 1f;

    private void OnEnable()  => ActiveSources.Add(this);
    private void OnDisable() => ActiveSources.Remove(this);

    // ── Gizmos ────────────────────────────────────────────────────
    private void OnDrawGizmosSelected()
    {
        Vector3 p = transform.position;
        Color   c = lightColor;

        // Outer radius
        Gizmos.color = new Color(c.r, c.g, c.b, 0.5f);
        DrawWireCircle(p, radius);

        // Core radius
        Gizmos.color = new Color(c.r, c.g, c.b, 0.9f);
        DrawWireCircle(p, radius * coreRadius);
    }

    private static void DrawWireCircle(Vector3 center, float r, int segments = 48)
    {
        for (int i = 0; i < segments; i++)
        {
            float a0 = (i       / (float)segments) * Mathf.PI * 2f;
            float a1 = ((i + 1) / (float)segments) * Mathf.PI * 2f;
            Gizmos.DrawLine(
                center + new Vector3(Mathf.Cos(a0), Mathf.Sin(a0)) * r,
                center + new Vector3(Mathf.Cos(a1), Mathf.Sin(a1)) * r);
        }
    }

    // ── Public convenience ────────────────────────────────────────
    public void SetIntensity(float v)  => intensity   = Mathf.Clamp01(v);
    public void SetRadius(float v)     => radius      = Mathf.Max(0.01f, v);
    public void SetCoreRadius(float v) => coreRadius  = Mathf.Clamp01(v);
}