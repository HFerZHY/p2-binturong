using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;

/// <summary>
/// Two render graph passes:
///   Pass 1 - Light Mask : draws every LightSource2D's quad mesh into _LightMaskBuffer
///   Pass 2 - Night Blit : blends the night colour over the camera image, attenuated by the mask
/// </summary>
public class NightViewPass : ScriptableRenderPass
{
    private static readonly int LightMaskBufferID = Shader.PropertyToID("_LightMaskBuffer");

    private readonly Material _nightMaterial;      // NightViewURP.shader material
    private readonly Material _lightCircleMaterial; // LightSource2DBlit.shader material

    // ── Shared quad mesh (one unit quad centred at origin, UV in [-1,1]) ──
    private static Mesh _quadMesh;

    // ── Pass data ─────────────────────────────────────────────────
    private class MaskPassData
    {
        public Material              material;
        public Mesh                  mesh;
        public List<LightSource2D>   sources;  // snapshot taken at record time
    }

    private class BlitPassData
    {
        public TextureHandle source;
        public Material      material;
    }

    // ─────────────────────────────────────────────────────────────
    //  Constructor
    // ─────────────────────────────────────────────────────────────
    public NightViewPass(RenderPassEvent evt, Material nightMaterial, Material lightCircleMaterial)
    {
        renderPassEvent  = evt;
        profilingSampler = new ProfilingSampler("NightView");
        _nightMaterial        = CreateRuntimeMaterial(nightMaterial);
        _lightCircleMaterial  = CreateRuntimeMaterial(lightCircleMaterial);
        EnsureQuadMesh();
    }

    // ─────────────────────────────────────────────────────────────
    //  RecordRenderGraph
    // ─────────────────────────────────────────────────────────────
    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        if (_nightMaterial == null || _lightCircleMaterial == null) return;

        EnsureQuadMesh();
        if (_quadMesh == null) return;

        NightViewController ctrl = NightViewController.Instance;
        if (ctrl == null || !ctrl.isActiveAndEnabled) return;

        // Push controller values onto the night material every frame
        _nightMaterial.SetColor("_NightCol", ctrl.nightColor);
        _nightMaterial.SetFloat("_Alpha",    ctrl.nightAlpha);

        var resourceData = frameData.Get<UniversalResourceData>();

        var desc = resourceData.activeColorTexture.GetDescriptor(renderGraph);
        desc.depthBufferBits = 0;
        desc.msaaSamples     = MSAASamples.None;

        // ── Pass 1: draw light quads into the mask buffer ─────────
        desc.name = "_LightMaskBuffer";
        TextureHandle maskBuffer = renderGraph.CreateTexture(desc);

        using (var builder = renderGraph.AddRasterRenderPass<MaskPassData>(
                   "NightView - Light Mask", out var data))
        {
            // Snapshot the active sources so the list is stable inside the render func
            data.material = _lightCircleMaterial;
            data.mesh     = _quadMesh;
            data.sources  = new List<LightSource2D>(LightSource2D.ActiveSources);

            builder.SetRenderAttachment(maskBuffer, 0, AccessFlags.Write);
            builder.SetGlobalTextureAfterPass(maskBuffer, LightMaskBufferID);
            builder.AllowPassCulling(false);

            builder.SetRenderFunc((MaskPassData d, RasterGraphContext ctx) =>
            {
                ctx.cmd.ClearRenderTarget(false, true, Color.black);
                var properties = new MaterialPropertyBlock();

                foreach (LightSource2D ls in d.sources)
                {
                    if (ls == null) continue;

                    properties.Clear();
                    properties.SetColor("_Color",      ls.lightColor);
                    properties.SetFloat("_Intensity",  ls.intensity);
                    properties.SetFloat("_CoreRadius", ls.coreRadius);

                    // DrawMesh: the quad is 1×1 unit; scale it to world radius
                    float   r  = ls.radius;
                    Matrix4x4 m = Matrix4x4.TRS(
                        ls.transform.position,
                        Quaternion.identity,
                        new Vector3(r * 2f, r * 2f, 1f));

                    ctx.cmd.DrawMesh(d.mesh, m, d.material, 0, 0, properties);
                }
            });
        }

        // ── Pass 2: blit camera through the night overlay shader ──
        desc.name = "_NightViewDest";
        TextureHandle dest = renderGraph.CreateTexture(desc);

        using (var builder = renderGraph.AddRasterRenderPass<BlitPassData>(
                   "NightView - Composite", out var data))
        {
            data.source   = resourceData.activeColorTexture;
            data.material = _nightMaterial;

            builder.UseTexture(resourceData.activeColorTexture, AccessFlags.Read);
            builder.UseGlobalTexture(LightMaskBufferID);
            builder.SetRenderAttachment(dest, 0, AccessFlags.Write);
            builder.AllowPassCulling(false);

            builder.SetRenderFunc((BlitPassData d, RasterGraphContext ctx) =>
            {
                Blitter.BlitTexture(ctx.cmd, d.source, new Vector4(1, 1, 0, 0), d.material, 0);
            });
        }

        resourceData.cameraColor = dest;
    }

    // ─────────────────────────────────────────────────────────────
    //  Quad mesh  (UV in [-1, 1] so the shader gets distance from centre)
    // ─────────────────────────────────────────────────────────────
    private static void EnsureQuadMesh()
    {
        if (_quadMesh != null) return;
        _quadMesh = new Mesh { name = "NightView_LightQuad" };
        _quadMesh.hideFlags = HideFlags.HideAndDontSave;
        _quadMesh.SetVertices(new[]
        {
            new Vector3(-0.5f, -0.5f, 0f),
            new Vector3( 0.5f, -0.5f, 0f),
            new Vector3( 0.5f,  0.5f, 0f),
            new Vector3(-0.5f,  0.5f, 0f),
        });
        // UV in [-1,1]: shader uses length(uv) as distance from centre
        _quadMesh.SetUVs(0, new[]
        {
            new Vector2(-1f, -1f),
            new Vector2( 1f, -1f),
            new Vector2( 1f,  1f),
            new Vector2(-1f,  1f),
        });
        _quadMesh.SetTriangles(new[] { 0, 2, 1, 0, 3, 2 }, 0);
        _quadMesh.UploadMeshData(false);
    }

    private static Material CreateRuntimeMaterial(Material source)
    {
        if (source == null) return null;

        var material = new Material(source);
        material.hideFlags = HideFlags.HideAndDontSave;
        return material;
    }

    public void Dispose()
    {
        CoreUtils.Destroy(_nightMaterial);
        CoreUtils.Destroy(_lightCircleMaterial);
    }
}
