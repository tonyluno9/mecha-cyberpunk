// Charco que refleja (docs/plan-look-ciudad.md §2.3). Cada punto del charco se proyecta en la cámara del reflejo planar (ReflejoCharcos,
// _ReflejoCharcosVP) y toma ese color de _ReflejoCharcosTex. Fresnel: de lejos y rasante refleja casi todo; mirando hacia abajo se ve el
// asfalto mojado oscuro. La forma sale de la máscara (canal R) y de un poco de ruido para que la orilla no se vea recortada.
Shader "MechaCyberpunk/Mundo/Charco"
{
    Properties
    {
        _Mascara ("Forma (R)", 2D) = "white" {}
        _ColorFondo ("Asfalto mojado", Color) = (0.03, 0.035, 0.045, 1)
        _Intensidad ("Intensidad del reflejo", Range(0, 2)) = 0.95
        _FresnelMin ("Reflejo mirando hacia abajo", Range(0, 1)) = 0.45
        _FresnelPotencia ("Potencia del fresnel", Range(0.5, 8)) = 3
        _Opacidad ("Opacidad", Range(0, 1)) = 0.92
        _Ondas ("Distorsión de las ondas", Range(0, 0.02)) = 0.003
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent-60" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }

        Pass
        {
            Name "Charco"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back
            Offset -1, -1

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_Mascara);
            SAMPLER(sampler_Mascara);
            TEXTURE2D(_ReflejoCharcosTex);
            SAMPLER(sampler_ReflejoCharcosTex);
            float4x4 _ReflejoCharcosVP;
            float _ReflejoCharcosHay;

            CBUFFER_START(UnityPerMaterial)
                float4 _Mascara_ST;
                half4 _ColorFondo;
                half _Intensidad;
                half _FresnelMin;
                half _FresnelPotencia;
                half _Opacidad;
                half _Ondas;
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
                half niebla : TEXCOORD2;
            };

            Salida vert(Entrada entrada)
            {
                Salida salida;
                salida.mundo = TransformObjectToWorld(entrada.posicion.xyz);
                salida.posicion = TransformWorldToHClip(salida.mundo);
                salida.uv = TRANSFORM_TEX(entrada.uv, _Mascara);
                salida.niebla = ComputeFogFactor(salida.posicion.z);
                return salida;
            }

            half4 frag(Salida salida) : SV_Target
            {
                half forma = SAMPLE_TEXTURE2D(_Mascara, sampler_Mascara, salida.uv).r;
                float2 ondas = float2(sin(salida.mundo.x * 3.1 + salida.mundo.z * 1.7), cos(salida.mundo.z * 2.9 - salida.mundo.x * 1.3)) * _Ondas;
                float4 c = mul(_ReflejoCharcosVP, float4(salida.mundo, 1.0));
                float2 uv = c.xy / max(c.w, 1e-5) * 0.5 + 0.5 + ondas;
                half3 reflejo = SAMPLE_TEXTURE2D(_ReflejoCharcosTex, sampler_ReflejoCharcosTex, uv).rgb * _ReflejoCharcosHay;
                half3 vista = normalize(GetWorldSpaceViewDir(salida.mundo));
                half fresnel = lerp(_FresnelMin, 1.0, pow(1.0 - saturate(vista.y), _FresnelPotencia));
                half3 color = lerp(_ColorFondo.rgb, reflejo * _Intensidad, fresnel);
                color = MixFog(color, salida.niebla);
                return half4(color, saturate(forma * _Opacidad));
            }
            ENDHLSL
        }
    }
}
