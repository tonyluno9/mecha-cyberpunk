// Brillo que solo se prende de noche (docs/plan-look-ciudad.md §2.4): ventanas cálidas, letreros de neón chicos y la mica de las lámparas.
// Sin luz: color HDR × textura × _T501Noche (global de VolumenSigueNoche, 0 de día y 1 de noche), con un color de día apagado. La niebla lo
// funde con la distancia. Con el bloom del posproceso da el halo.
Shader "MechaCyberpunk/Mundo/EmisionNoche"
{
    Properties
    {
        _BaseMap ("Textura", 2D) = "white" {}
        [HDR] _Color ("Color de noche", Color) = (2.4, 1.5, 0.75, 1)
        _ColorDia ("Color de día", Color) = (0.18, 0.17, 0.16, 1)
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "Emision"
            Tags { "LightMode" = "UniversalForward" }
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            float _T501Noche;

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _Color;
                half4 _ColorDia;
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
                half niebla : TEXCOORD1;
            };

            Salida vert(Entrada entrada)
            {
                Salida salida;
                salida.posicion = TransformObjectToHClip(entrada.posicion.xyz);
                salida.uv = TRANSFORM_TEX(entrada.uv, _BaseMap);
                salida.niebla = ComputeFogFactor(salida.posicion.z);
                return salida;
            }

            half4 frag(Salida salida) : SV_Target
            {
                half3 textura = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, salida.uv).rgb;
                half noche = saturate(_T501Noche);
                half3 color = lerp(_ColorDia.rgb, _Color.rgb, noche) * textura;
                return half4(MixFog(color, salida.niebla), 1.0);
            }
            ENDHLSL
        }

        // Para que los contornos y el SSAO vean estas piezas (profundidad y normales).
        UsePass "Universal Render Pipeline/Unlit/DEPTHONLY"
        UsePass "Universal Render Pipeline/Unlit/DEPTHNORMALSONLY"
    }
}
