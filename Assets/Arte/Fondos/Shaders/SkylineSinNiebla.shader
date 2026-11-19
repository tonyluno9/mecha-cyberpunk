// T-1392: skyline lejano (SK01) en aditivo, sin luces, sin sombras y SIN la niebla del juego: a 500-900 m la niebla exponencial lo borraba por completo.
// De día se atenúa (_T501Noche, global, lo pone VolumenSigueNoche: 0 día, 1 noche) porque la pintura es de noche.
Shader "MechaCyberpunk/Fondos/SkylineSinNiebla"
{
    Properties
    {
        _BaseMap("Pintura", 2D) = "white" {}
        [HDR] _BaseColor("Color (brillo)", Color) = (1.6, 1.6, 1.6, 1)
        _BrilloDia("Brillo de día (fracción)", Range(0, 1)) = 0.15
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" "IgnoreProjector" = "True" }
        Pass
        {
            Name "Unlit"
            Tags { "LightMode" = "UniversalForward" }
            Blend One One
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            float _T501Noche;
            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float _BrilloDia;
            CBUFFER_END

            struct Atributos { float4 pos : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 clip : SV_POSITION; float2 uv : TEXCOORD0; };

            Varyings vert(Atributos a)
            {
                Varyings o;
                o.clip = TransformObjectToHClip(a.pos.xyz);
                o.uv = a.uv * _BaseMap_ST.xy + _BaseMap_ST.zw;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float3 pintura = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv).rgb * _BaseColor.rgb;
                float noche = lerp(_BrilloDia, 1.0, saturate(_T501Noche));
                return half4(pintura * noche, 1.0);
            }
            ENDHLSL
        }
    }
}
