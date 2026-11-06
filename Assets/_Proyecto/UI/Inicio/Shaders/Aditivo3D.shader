// Shader de mezcla aditiva para URP (lluvia, halos de neón y resplandor del horizonte en el fondo del inicio). Sin luces ni niebla.
Shader "MechaCyberpunk/Inicio/Aditivo3D"
{
    Properties
    {
        _MainTex ("Textura", 2D) = "white" {}
        [HDR] _Color ("Color", Color) = (1,1,1,1)
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "Aditivo"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha One
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _Color;
            CBUFFER_END

            struct Entrada
            {
                float4 posicion : POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            struct Salida
            {
                float4 posicion : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            Salida vert(Entrada entrada)
            {
                Salida salida;
                salida.posicion = TransformObjectToHClip(entrada.posicion.xyz);
                salida.uv = TRANSFORM_TEX(entrada.uv, _MainTex);
                salida.color = entrada.color;
                return salida;
            }

            half4 frag(Salida salida) : SV_Target
            {
                half4 textura = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, salida.uv);
                return textura * _Color * salida.color;
            }
            ENDHLSL
        }
    }
}
