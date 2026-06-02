Shader "Custom/NightViewURP"
{
    Properties
    {
        _NightCol ("Night Color", Color)      = (0.04, 0.06, 0.18, 1)
        _Alpha    ("Night Alpha", Range(0,1)) = 0.85
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        ZWrite Off ZTest Always Cull Off

        Pass
        {
            Name "NightOverlay"
            HLSLPROGRAM
            #pragma vertex   Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float4 _NightCol;
            float  _Alpha;

            half4 Frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);
                half4 scene  = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, IN.texcoord);
                half3 result = lerp(scene.rgb, _NightCol.rgb, _Alpha);
                return half4(result, scene.a);
            }
            ENDHLSL
        }
    }
}
