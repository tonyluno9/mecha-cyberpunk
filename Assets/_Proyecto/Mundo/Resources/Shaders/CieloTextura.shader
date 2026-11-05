// T-635: capa del cielo (nubes o estrellas) en una esfera grande alrededor de la camara. Sin niebla y sin escribir profundidad:
// los edificios la tapan, y el desvanecido cerca del horizonte la funde con el cielo procedural.
Shader "MechaCyberpunk/CieloTextura"
{
    Properties
    {
        _MainTex ("Textura", 2D) = "white" {}
        _Color ("Color y opacidad", Color) = (1, 1, 1, 1)
        _Desvanecer ("Altura donde termina el desvanecido del horizonte", Range(0.01, 1)) = 0.2
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent-100" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Pass
        {
            Name "Cielo"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Front
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _Color;
                half _Desvanecer;
            CBUFFER_END

            struct Entrada
            {
                float4 posicion : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Salida
            {
                float4 posicion : SV_POSITION;
                float2 uv : TEXCOORD0;
                float altura : TEXCOORD1;
            };

            Salida vert(Entrada i)
            {
                Salida o;
                o.posicion = TransformObjectToHClip(i.posicion.xyz);
                o.uv = TRANSFORM_TEX(i.uv, _MainTex);
                o.altura = normalize(i.posicion.xyz).y;
                return o;
            }

            half4 frag(Salida i) : SV_Target
            {
                half4 t = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                half a = t.a * _Color.a * smoothstep(0.0, _Desvanecer, i.altura);
                return half4(t.rgb * _Color.rgb, a);
            }
            ENDHLSL
        }
    }
}
