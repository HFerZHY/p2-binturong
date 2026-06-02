using UnityEngine;
using UnityEngine.Rendering.Universal;

public class NightViewRendererFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingTransparents;

        [Tooltip("Material using the NightViewURP shader.")]
        public Material nightMaterial;

        [Tooltip("Material using the LightSource2DBlit shader.")]
        public Material lightCircleMaterial;
    }

    public Settings settings = new Settings();
    private NightViewPass _pass;

    public override void Create()
    {
        _pass = new NightViewPass(
            settings.renderPassEvent,
            settings.nightMaterial,
            settings.lightCircleMaterial);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (NightViewController.Instance == null) return;
        if (!NightViewController.Instance.isActiveAndEnabled) return;
        if (settings.nightMaterial == null || settings.lightCircleMaterial == null) return;

        renderer.EnqueuePass(_pass);
    }

    protected override void Dispose(bool disposing) => _pass?.Dispose();
}