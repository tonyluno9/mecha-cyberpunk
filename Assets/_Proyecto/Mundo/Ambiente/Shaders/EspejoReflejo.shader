// T-854: vidrio del espejo de verdad. Cada punto del vidrio se proyecta en la cámara del reflejo (EspejoReflejo, _ReflejoVP) y toma ese color
// de _ReflejoTex; un tinte frío y un poco de suciedad para que se lea como vidrio. Sin reflejo (lejos), se ve el color de reposo.
Shader "MechaCyberpunk/Mundo/EspejoReflejo"
{
    Properties
    {
        _ReflejoTex ("Reflejo", 2D) = "black" {}
        _Tinte ("Tinte", Color) = (0.92, 0.95, 1, 1)
        _Reposo ("Color sin reflejo", Color) = (0.18, 0.2, 0.23, 1)
        _Suciedad ("Suciedad", 2D) = "black" {}
        _CuantaSuciedad ("Cuánta suciedad", Range(0, 1)) = 0.15
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "Espejo"
            Tags { "LightMode" = "UniversalForward" }
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_ReflejoTex);
            SAMPLER(sampler_ReflejoTex);
            TEXTURE2D(_Suciedad);
            SAMPLER(sampler_Suciedad);

            CBUFFER_START(UnityPerMaterial)
                half4 _Tinte;
                half4 _Reposo;
                float4 _Suciedad_ST;
                half _CuantaSuciedad;
                float4x4 _ReflejoVP;
            CBUFFER_END

            struct Entrada
            {
                float4 posicion : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Salida
            {
                float4 posicion : SV_POSITION;
                float3 mundo : TEXCOORD0;
                float2 uv : TEXCOORD1;
            };

            Salida vert(Entrada entrada)
            {
                Salida salida;
                salida.mundo = TransformObjectToWorld(entrada.posicion.xyz);
                salida.posicion = TransformWorldToHClip(salida.mundo);
                salida.uv = TRANSFORM_TEX(entrada.uv, _Suciedad);
                return salida;
            }

            half4 frag(Salida salida) : SV_Target
            {
                float4 c = mul(_ReflejoVP, float4(salida.mundo, 1.0));
                float2 uv = c.xy / max(c.w, 1e-5) * 0.5 + 0.5;
                half4 reflejo = SAMPLE_TEXTURE2D(_ReflejoTex, sampler_ReflejoTex, uv);
                half hay = c.w > 0.0 ? 1.0 : 0.0;
                half3 color = lerp(_Reposo.rgb, reflejo.rgb * _Tinte.rgb, hay * _Tinte.a);
                half sucio = SAMPLE_TEXTURE2D(_Suciedad, sampler_Suciedad, salida.uv).r * _CuantaSuciedad;
                return half4(lerp(color, half3(0.5, 0.48, 0.45), sucio), 1.0);
            }
            ENDHLSL
        }
    }
}
