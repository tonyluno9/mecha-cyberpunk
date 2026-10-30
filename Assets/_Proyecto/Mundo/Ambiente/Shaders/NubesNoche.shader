// Capa de nubes de noche (ambientación de noche): domo alrededor de la cámara, sin niebla, que solo se ve donde no hay
// geometría (va después de los opacos y no escribe profundidad). Las nubes se proyectan sobre un plano alto, se mueven
// con el tiempo y se tiñen abajo con el brillo de la ciudad. CieloNubes le pone la opacidad según la hora.
Shader "MechaCyberpunk/NubesNoche"
{
    Properties
    {
        _Ruido ("Ruido (tileable)", 2D) = "gray" {}
        _ColorNubes ("Color de las nubes", Color) = (0.07, 0.06, 0.13, 1)
        _ColorBrillo ("Brillo de la ciudad en las nubes", Color) = (0.45, 0.16, 0.42, 1)
        _Cobertura ("Cobertura", Range(0, 1)) = 0.55
        _Suavidad ("Suavidad del borde", Range(0.01, 1)) = 0.3
        _Escala ("Escala de las nubes", Float) = 0.35
        _Velocidad ("Velocidad (uv/s)", Vector) = (0.004, 0.002, 0, 0)
        _AlturaBrillo ("Altura del brillo (seno de la elevación)", Range(0.01, 1)) = 0.45
        _Opacidad ("Opacidad", Range(0, 1)) = 1
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent-100" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Pass
        {
            Name "Nubes"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_Ruido);
            SAMPLER(sampler_Ruido);

            CBUFFER_START(UnityPerMaterial)
                float4 _Ruido_ST;
                half4 _ColorNubes;
                half4 _ColorBrillo;
                half _Cobertura;
                half _Suavidad;
                float _Escala;
                float4 _Velocidad;
                half _AlturaBrillo;
                half _Opacidad;
            CBUFFER_END

            struct Atributos
            {
                float4 posicion : POSITION;
            };

            struct Variables
            {
                float4 posicion : SV_POSITION;
                float3 direccion : TEXCOORD0;
            };

            Variables Vert(Atributos entrada)
            {
                Variables salida;
                float3 mundo = TransformObjectToWorld(entrada.posicion.xyz);
                salida.posicion = TransformWorldToHClip(mundo);
                salida.direccion = mundo - GetCameraPositionWS();
                return salida;
            }

            half4 Frag(Variables entrada) : SV_Target
            {
                float3 d = normalize(entrada.direccion);
                float arriba = max(d.y, 0.0);
                float2 uv = d.xz / (arriba + 0.12) * _Escala;
                float t = _Time.y;
                float n1 = SAMPLE_TEXTURE2D(_Ruido, sampler_Ruido, uv + _Velocidad.xy * t).r;
                float n2 = SAMPLE_TEXTURE2D(_Ruido, sampler_Ruido, uv * 2.7 + float2(0.37, 0.71) - _Velocidad.xy * 1.6 * t).r;
                float n = n1 * 0.65 + n2 * 0.35;
                float umbral = 1.0 - _Cobertura;
                float alfa = smoothstep(umbral - _Suavidad, umbral + _Suavidad, n);
                float horizonte = saturate(arriba / 0.06);
                float brillo = 1.0 - saturate(arriba / _AlturaBrillo);
                half3 color = lerp(_ColorNubes.rgb, _ColorBrillo.rgb, brillo * brillo) * (0.55 + 0.45 * n);
                return half4(color, alfa * horizonte * _Opacidad);
            }
            ENDHLSL
        }
    }
}
