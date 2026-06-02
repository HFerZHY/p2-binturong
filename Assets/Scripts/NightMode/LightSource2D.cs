using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Add to any GameObject to make it a light source that punches through
/// the NightViewController night overlay.
///
/// Draws a soft-edged shape (Circle, Capsule, or Rectangle) into the
/// shared light-mask RenderTexture via immediate-mode GL calls.
/// The NightViewPass calls DrawToMaskRT() each frame while the mask RT
/// is active.
/// </summary>
[ExecuteAlways]
public class LightSource2D : MonoBehaviour
{
    // ──────────────────────────────────────────────────────────────
    //  Registry
    // ──────────────────────────────────────────────────────────────
    public static readonly HashSet<LightSource2D> ActiveSources = new HashSet<LightSource2D>();

    // ──────────────────────────────────────────────────────────────
    //  Enums
    // ──────────────────────────────────────────────────────────────
    public enum LightShape { Circle, Capsule, Rectangle }

    // ──────────────────────────────────────────────────────────────
    //  Inspector fields
    // ──────────────────────────────────────────────────────────────
    [Header("Shape")]
    public LightShape shape = LightShape.Circle;

    [Tooltip("Circle: X = radius.\nCapsule: X = half-width (cap radius), Y = half-height.\nRectangle: X = half-width, Y = half-height.")]
    public Vector2 size = new Vector2(2f, 2f);

    [Header("Light")]
    [ColorUsage(false, true)]
    public Color lightColor = Color.white;

    [Range(0f, 1f)]
    [Tooltip("How much of the darkness this source removes.")]
    public float intensity = 1f;

    [Range(0f, 1f)]
    [Tooltip("Extra brightness added on top of removing darkness (bloom-like boost).")]
    public float extraLight = 0f;

    [Range(0f, 1f)]
    [Tooltip("Edge feathering. 0 = hard edge, 1 = fully soft halo.")]
    public float falloff = 0.5f;

    // ──────────────────────────────────────────────────────────────
    //  Unity messages
    // ──────────────────────────────────────────────────────────────
    private void OnEnable()  { ActiveSources.Add(this);    }
    private void OnDisable() { ActiveSources.Remove(this); }

    // ──────────────────────────────────────────────────────────────
    //  Public API — called by NightViewPass while mask RT is active
    // ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Draws this light's shape into the currently active RenderTexture
    /// using immediate-mode GL. Call only while the mask RT is set as
    /// RenderTexture.active and GL matrices are already loaded.
    /// </summary>
    public void DrawToMaskRT(Material glMaterial)
    {
        glMaterial.SetColor("_Color",      lightColor);
        glMaterial.SetFloat("_Intensity",  intensity);
        glMaterial.SetFloat("_ExtraLight", extraLight);
        glMaterial.SetPass(0);

        switch (shape)
        {
            case LightShape.Circle:    DrawCircle();    break;
            case LightShape.Capsule:   DrawCapsule();   break;
            case LightShape.Rectangle: DrawRectangle(); break;
        }
    }

    // ──────────────────────────────────────────────────────────────
    //  Shape drawing
    //  UV.x encodes the falloff gradient: 1 = centre (full brightness)
    //                                     0 = outer edge (zero brightness)
    // ──────────────────────────────────────────────────────────────

    private void DrawCircle()
    {
        const int Segments = 48;
        float     r        = size.x;
        Vector3   center   = transform.position;
        // Inner radius of the hard core (shrinks as falloff increases)
        float     coreR    = r * Mathf.Lerp(1f, 0f, falloff);

        GL.Begin(GL.TRIANGLES);

        for (int i = 0; i < Segments; i++)
        {
            float a0 = (i       / (float)Segments) * Mathf.PI * 2f;
            float a1 = ((i + 1) / (float)Segments) * Mathf.PI * 2f;

            Vector3 c0 = new Vector3(Mathf.Cos(a0), Mathf.Sin(a0));
            Vector3 c1 = new Vector3(Mathf.Cos(a1), Mathf.Sin(a1));

            // Core triangle (full brightness)
            GL.TexCoord2(1f, 0f); GL.Vertex(center);
            GL.TexCoord2(1f, 0f); GL.Vertex(center + c0 * coreR);
            GL.TexCoord2(1f, 0f); GL.Vertex(center + c1 * coreR);

            // Falloff ring (brightness 1 → 0)
            if (falloff > 0f)
            {
                GL.TexCoord2(1f, 0f); GL.Vertex(center + c0 * coreR);
                GL.TexCoord2(1f, 0f); GL.Vertex(center + c1 * coreR);
                GL.TexCoord2(0f, 0f); GL.Vertex(center + c1 * r);

                GL.TexCoord2(1f, 0f); GL.Vertex(center + c0 * coreR);
                GL.TexCoord2(0f, 0f); GL.Vertex(center + c1 * r);
                GL.TexCoord2(0f, 0f); GL.Vertex(center + c0 * r);
            }
        }

        GL.End();
    }

    private void DrawCapsule()
    {
        float   hw     = size.x;               // cap radius / half-width
        float   hh     = size.y;               // total half-height
        float   bodyH  = Mathf.Max(0f, hh - hw); // half-height of rectangular body
        Vector3 pos    = transform.position;
        float   fe     = Mathf.Lerp(1f, 0f, falloff); // inner scale factor

        GL.Begin(GL.TRIANGLES);

        // Central rectangle body (solid core)
        DrawQuadGL(
            pos + new Vector3(-hw * fe, -bodyH),
            pos + new Vector3( hw * fe, -bodyH),
            pos + new Vector3( hw * fe,  bodyH),
            pos + new Vector3(-hw * fe,  bodyH), 1f);

        // Top and bottom semi-circle caps
        DrawSemiCircle(pos + Vector3.up   * bodyH, hw, 0f,        Mathf.PI,      fe);
        DrawSemiCircle(pos + Vector3.down * bodyH, hw, Mathf.PI,  Mathf.PI * 2f, fe);

        // Side falloff strips
        if (falloff > 0f)
        {
            DrawQuadGL(
                pos + new Vector3(-hw,      -bodyH),
                pos + new Vector3(-hw * fe, -bodyH),
                pos + new Vector3(-hw * fe,  bodyH),
                pos + new Vector3(-hw,       bodyH),
                0f, 1f, 1f, 0f);

            DrawQuadGL(
                pos + new Vector3( hw * fe, -bodyH),
                pos + new Vector3( hw,      -bodyH),
                pos + new Vector3( hw,       bodyH),
                pos + new Vector3( hw * fe,  bodyH),
                1f, 0f, 0f, 1f);
        }

        GL.End();
    }

    private void DrawRectangle()
    {
        float   hw  = size.x;
        float   hh  = size.y;
        Vector3 pos = transform.position;
        float   fe  = Mathf.Lerp(1f, 0f, falloff);
        float   iw  = hw * fe;
        float   ih  = hh * fe;

        GL.Begin(GL.TRIANGLES);

        // Solid core
        DrawQuadGL(
            pos + new Vector3(-iw, -ih),
            pos + new Vector3( iw, -ih),
            pos + new Vector3( iw,  ih),
            pos + new Vector3(-iw,  ih), 1f);

        if (falloff > 0f)
        {
            // Four edge strips
            DrawQuadGL(pos+new Vector3(-iw,-hh), pos+new Vector3( iw,-hh), pos+new Vector3( iw,-ih), pos+new Vector3(-iw,-ih), 0f,0f,1f,1f);
            DrawQuadGL(pos+new Vector3(-iw, ih), pos+new Vector3( iw, ih), pos+new Vector3( iw, hh), pos+new Vector3(-iw, hh), 1f,1f,0f,0f);
            DrawQuadGL(pos+new Vector3(-hw,-hh), pos+new Vector3(-iw,-hh), pos+new Vector3(-iw, hh), pos+new Vector3(-hw, hh), 0f,1f,1f,0f);
            DrawQuadGL(pos+new Vector3( iw,-hh), pos+new Vector3( hw,-hh), pos+new Vector3( hw, hh), pos+new Vector3( iw, hh), 1f,0f,0f,1f);

            // Four corner quads
            DrawQuadGL(pos+new Vector3(-hw,-hh), pos+new Vector3(-iw,-hh), pos+new Vector3(-iw,-ih), pos+new Vector3(-hw,-ih), 0f,0f,1f,0f);
            DrawQuadGL(pos+new Vector3( iw,-hh), pos+new Vector3( hw,-hh), pos+new Vector3( hw,-ih), pos+new Vector3( iw,-ih), 0f,0f,0f,1f);
            DrawQuadGL(pos+new Vector3(-hw, ih), pos+new Vector3(-iw, ih), pos+new Vector3(-iw, hh), pos+new Vector3(-hw, hh), 0f,1f,0f,0f);
            DrawQuadGL(pos+new Vector3( iw, ih), pos+new Vector3( hw, ih), pos+new Vector3( hw, hh), pos+new Vector3( iw, hh), 1f,0f,0f,0f);
        }

        GL.End();
    }

    // ──────────────────────────────────────────────────────────────
    //  GL helpers
    // ──────────────────────────────────────────────────────────────

    private static void DrawQuadGL(Vector3 bl, Vector3 br, Vector3 tr, Vector3 tl,
                                   float uBL, float uBR, float uTR, float uTL)
    {
        GL.TexCoord2(uBL, 0f); GL.Vertex(bl);
        GL.TexCoord2(uBR, 0f); GL.Vertex(br);
        GL.TexCoord2(uTR, 0f); GL.Vertex(tr);

        GL.TexCoord2(uBL, 0f); GL.Vertex(bl);
        GL.TexCoord2(uTR, 0f); GL.Vertex(tr);
        GL.TexCoord2(uTL, 0f); GL.Vertex(tl);
    }

    private static void DrawQuadGL(Vector3 bl, Vector3 br, Vector3 tr, Vector3 tl, float u)
        => DrawQuadGL(bl, br, tr, tl, u, u, u, u);

    private static void DrawSemiCircle(Vector3 center, float r,
                                       float startAngle, float endAngle, float fe)
    {
        const int Segs = 24;
        float range = endAngle - startAngle;
        float ri    = r * fe; // inner (core) radius

        for (int i = 0; i < Segs; i++)
        {
            float a0 = startAngle + (i       / (float)Segs) * range;
            float a1 = startAngle + ((i + 1) / (float)Segs) * range;

            Vector3 d0 = new Vector3(Mathf.Cos(a0), Mathf.Sin(a0));
            Vector3 d1 = new Vector3(Mathf.Cos(a1), Mathf.Sin(a1));

            // Core triangle
            GL.TexCoord2(fe, 0f); GL.Vertex(center);
            GL.TexCoord2(fe, 0f); GL.Vertex(center + d0 * ri);
            GL.TexCoord2(fe, 0f); GL.Vertex(center + d1 * ri);

            // Falloff ring
            if (fe < 1f)
            {
                GL.TexCoord2(fe, 0f); GL.Vertex(center + d0 * ri);
                GL.TexCoord2(fe, 0f); GL.Vertex(center + d1 * ri);
                GL.TexCoord2(0f, 0f); GL.Vertex(center + d1 * r);

                GL.TexCoord2(fe, 0f); GL.Vertex(center + d0 * ri);
                GL.TexCoord2(0f, 0f); GL.Vertex(center + d1 * r);
                GL.TexCoord2(0f, 0f); GL.Vertex(center + d0 * r);
            }
        }
    }

    // ──────────────────────────────────────────────────────────────
    //  Gizmos
    // ──────────────────────────────────────────────────────────────
    private void OnDrawGizmosSelected()
    {
        Color c = lightColor;
        Gizmos.color = new Color(c.r, c.g, c.b, 0.4f);
        DrawGizmoShape(size);

        if (falloff > 0f)
        {
            Gizmos.color = new Color(c.r, c.g, c.b, 0.15f);
            DrawGizmoShape(size * (1f + falloff * 0.5f));
        }
    }

    private void DrawGizmoShape(Vector2 s)
    {
        Vector3 p = transform.position;
        switch (shape)
        {
            case LightShape.Circle:
                Gizmos.DrawWireSphere(p, s.x);
                break;
            case LightShape.Capsule:
                float bodyH = Mathf.Max(0f, s.y - s.x);
                Gizmos.DrawWireSphere(p + Vector3.up   * bodyH, s.x);
                Gizmos.DrawWireSphere(p + Vector3.down * bodyH, s.x);
                Gizmos.DrawLine(p + new Vector3(-s.x,  bodyH), p + new Vector3(-s.x, -bodyH));
                Gizmos.DrawLine(p + new Vector3( s.x,  bodyH), p + new Vector3( s.x, -bodyH));
                break;
            case LightShape.Rectangle:
                Gizmos.DrawWireCube(p, new Vector3(s.x * 2f, s.y * 2f, 0f));
                break;
        }
    }

    // ──────────────────────────────────────────────────────────────
    //  Public convenience setters
    // ──────────────────────────────────────────────────────────────
    public void SetIntensity(float v)  => intensity  = Mathf.Clamp01(v);
    public void SetExtraLight(float v) => extraLight = Mathf.Clamp01(v);
    public void SetSize(Vector2 v)     => size = v;
}