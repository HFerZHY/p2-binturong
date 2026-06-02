Shader "Custom/LightSource2DBlit"
{
    Properties
    {
        _Color      ("Light Color",  Color)      = (1,1,1,1)
        _Intensity  ("Intensity",    Range(0,1)) = 1.0
        _CoreRadius ("Core Radius",  Range(0,1)) = 0.4
        // UV space: 0 = quad edge, 1 = quad centre (mapped from world radius)
        // Core   : uv distance < _CoreRadius         → full brightness
        // Falloff: uv distance in [_CoreRadius, 1]   → smooth fade to 0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend One One       // additive into the mask buffer
        ZWrite Off
        Cull Off

        Pass
        {
            Name "LightCircle"
            HLSLPROGRAM
            #pragma vertex   Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0; // [-1,1] in both axes from mesh
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
            };

            float4 _Color;
            float  _Intensity;
            float  _CoreRadius;

            Varyings Vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv         = IN.uv;
                return OUT;
            }

            half4 Frag(Varyings IN) : SV_Target
            {
                // Distance from centre in UV space (0 = centre, 1 = quad edge)
                float d = length(IN.uv);

                // Discard outside the circle
                clip(1.0 - d);

                // Full brightness inside core, smooth falloff in the ring
                float brightness = (d <= _CoreRadius)
                    ? 1.0
                    : 1.0 - smoothstep(_CoreRadius, 1.0, d);

                half3 col = _Color.rgb * _Intensity * brightness;
                return half4(col, brightness); // alpha carries the mask value too
            }
            ENDHLSL
        }
    }
}
