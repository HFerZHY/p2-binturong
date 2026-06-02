using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;

public class NightViewPass : ScriptableRenderPass
{
    private readonly Material _material;

    private class PassData
    {
        public TextureHandle source;
        public Material      material;
    }

    public NightViewPass(RenderPassEvent evt, Material material)
    {
        renderPassEvent  = evt;
        profilingSampler = new ProfilingSampler("NightView");
        _material        = material;
    }

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        if (_material == null) return;

        NightViewController ctrl = NightViewController.Instance;
        if (ctrl == null || !ctrl.isActiveAndEnabled) return;

        _material.SetColor("_NightCol", ctrl.nightColor);
        _material.SetFloat("_Alpha",    ctrl.nightAlpha);

        var resourceData = frameData.Get<UniversalResourceData>();

        var desc = resourceData.activeColorTexture.GetDescriptor(renderGraph);
        desc.depthBufferBits = 0;
        desc.name            = "_NightViewDest";
        TextureHandle dest = renderGraph.CreateTexture(desc);

        using (var builder = renderGraph.AddRasterRenderPass<PassData>("NightView", out var data))
        {
            data.source   = resourceData.activeColorTexture;
            data.material = _material;

            builder.UseTexture(resourceData.activeColorTexture, AccessFlags.Read);
            builder.SetRenderAttachment(dest, 0, AccessFlags.Write);
            builder.AllowPassCulling(false);

            builder.SetRenderFunc((PassData d, RasterGraphContext ctx) =>
            {
                Blitter.BlitTexture(ctx.cmd, d.source, new Vector4(1, 1, 0, 0), d.material, 0);
            });
        }

        resourceData.cameraColor = dest;
    }
}