using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// URP Renderer Feature for the night view effect.
///
/// Setup:
///   1. Create a Material using the NightViewURP shader.
///   2. Select your URP Renderer asset (Assets/Settings/URP-Renderer.asset).
///   3. Add Renderer Feature → NightViewRendererFeature.
///   4. Assign the material to the "Night Material" slot.
///   5. Add NightViewController to any active GameObject in your scene.
///
/// To change night color/alpha at runtime, either:
///   a) Tweak the material properties directly in the Inspector, or
///   b) Call nightMaterial.SetColor/_SetFloat from NightViewController.
/// </summary>
public class NightViewRendererFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingTransparents;

        [Tooltip("A material using the NightViewURP shader.")]
        public Material nightMaterial;
    }

    public Settings settings = new Settings();

    private NightViewPass _pass;

    public override void Create()
    {
        if (settings.nightMaterial == null)
            Debug.LogWarning("[NightViewRendererFeature] No material assigned. " +
                             "Create a material using NightViewURP shader and assign it here.");

        _pass = new NightViewPass(settings.renderPassEvent, settings.nightMaterial);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (NightViewController.Instance == null) return;
        if (!NightViewController.Instance.isActiveAndEnabled) return;
        if (settings.nightMaterial == null) return;

        renderer.EnqueuePass(_pass);
    }

    protected override void Dispose(bool disposing)
    {
        // _pass?.Dispose();
    }
}