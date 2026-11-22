// T-1621: material de la cápsula de caída CP01. _Holo (0 a 1) mezcla el material original con una emisión cian holográfica (scanlines y borde);
// _Fade (0 a 1) lo desvanece con tramado (dithered): opaco, sin ordenar transparencias, y con sombra que también se desvanece.
// Lo mueve CapsulaCaida con un MaterialPropertyBlock según los tiempos de CalculoCapsulaCaida.
Shader "MechaCyberpunk/CapsulaHolograma"
{
    Properties
    {
        _BaseMap ("Textura", 2D) = "white" {}
        _BaseColor ("Color", Color) = (0.6, 0.6, 0.65, 1)
        _EmisColor ("Emisión original", Color) = (0, 0, 0, 1)
        _HoloColor ("Color holográfico", Color) = (0.2, 0.9, 1, 1)
        _HoloIntensidad ("Intensidad holográfica", Float) = 4
        _Holo ("Holo", Range(0, 1)) = 0
        _Fade ("Fade", Range(0, 1)) = 0
        _LineasDensidad ("Densidad de scanlines (1/m)", Float) = 18
        _BordePotencia ("Potencia del borde", Range(0.5, 8)) = 2.5
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" "RenderPipeline" = "UniversalPipeline" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        TEXTURE2D(_BaseMap);
        SAMPLER(sampler_BaseMap);

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor;
            half4 _EmisColor;
            half4 _HoloColor;
            half _HoloIntensidad;
            half _Holo;
            half _Fade;
            half _LineasDensidad;
            half _BordePotencia;
        CBUFFER_END

        // Umbral de tramado 4x4 (Bayer) según el pixel en pantalla; con Fade = 1 se descarta todo.
        half UmbralTramado(float2 pixel)
        {
            const float matriz[16] =
            {
                0.0625, 0.5625, 0.1875, 0.6875,
                0.8125, 0.3125, 0.9375, 0.4375,
                0.2500, 0.7500, 0.1250, 0.6250,
                1.0000, 0.5000, 0.8750, 0.3750
            };
            uint x = (uint)fmod(pixel.x, 4.0);
            uint y = (uint)fmod(pixel.y, 4.0);
            return (half)matriz[y * 4 + x];
        }
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Entrada
            {
                float4 posicion : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Salida
            {
                float4 posicionClip : SV_POSITION;
                float3 normal : TEXCOORD0;
                float3 mundo : TEXCOORD1;
                float2 uv : TEXCOORD2;
            };

            Salida vert(Entrada i)
            {
                Salida o;
                o.mundo = TransformObjectToWorld(i.posicion.xyz);
                o.posicionClip = TransformWorldToHClip(o.mundo);
                o.normal = TransformObjectToWorldNormal(i.normal);
                o.uv = TRANSFORM_TEX(i.uv, _BaseMap);
                return o;
            }

            half4 frag(Salida i) : SV_Target
            {
                clip((1.0h - _Fade) - UmbralTramado(i.posicionClip.xy));

                half3 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv).rgb * _BaseColor.rgb;
                half3 n = normalize(i.normal);
                Light luz = GetMainLight();
                half3 iluminado = albedo * (luz.color * saturate(dot(n, luz.direction)) + SampleSH(n)) + _EmisColor.rgb;

                half3 haciaCamara = normalize(GetWorldSpaceViewDir(i.mundo));
                half borde = pow(1.0h - saturate(dot(n, haciaCamara)), _BordePotencia);
                half lineas = 0.65h + 0.35h * step(0.5h, frac(i.mundo.y * _LineasDensidad));
                half3 holo = _HoloColor.rgb * _HoloIntensidad * lineas * (0.55h + borde);

                return half4(lerp(iluminado, holo, _Holo), 1.0h);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma vertex vertSombra
            #pragma fragment fragSombra
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct EntradaSombra
            {
                float4 posicion : POSITION;
                float3 normal : NORMAL;
            };

            struct SalidaSombra
            {
                float4 posicionClip : SV_POSITION;
            };

            SalidaSombra vertSombra(EntradaSombra i)
            {
                SalidaSombra o;
                float3 mundo = TransformObjectToWorld(i.posicion.xyz);
                float3 normalMundo = TransformObjectToWorldNormal(i.normal);
            #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                float3 direccionLuz = normalize(_LightPosition - mundo);
            #else
                float3 direccionLuz = _LightDirection;
            #endif
                float4 clipPos = TransformWorldToHClip(ApplyShadowBias(mundo, normalMundo, direccionLuz));
            #if UNITY_REVERSED_Z
                clipPos.z = min(clipPos.z, UNITY_NEAR_CLIP_VALUE);
            #else
                clipPos.z = max(clipPos.z, UNITY_NEAR_CLIP_VALUE);
            #endif
                o.posicionClip = clipPos;
                return o;
            }

            half4 fragSombra(SalidaSombra i) : SV_Target
            {
                clip((1.0h - _Fade) - UmbralTramado(i.posicionClip.xy));
                return 0;
            }
            ENDHLSL
        }
    }
}
