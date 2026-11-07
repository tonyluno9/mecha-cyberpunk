// Halo de luz en la niebla bajo cada lámpara (docs/plan-look-ciudad.md §2.4): cono abierto, aditivo, más fuerte junto a la lámpara y que se
// desvanece hacia el piso, en las orillas del cono (donde se ve de canto) y donde toca paredes y piso (con la profundidad de la escena).
// La niebla lo apaga a lo lejos y de día no se ve (_T501Noche, global de VolumenSigueNoche). UV.y del cono: 0 en la lámpara, 1 en la base.
Shader "MechaCyberpunk/Mundo/HaloLuz"
{
    Properties
    {
        _Color ("Color", Color) = (1, 0.55, 0.2, 1)
        _Intensidad ("Intensidad", Range(0, 4)) = 0.35
        _Caida ("Caída hacia el piso", Range(0.2, 6)) = 1.6
        _Borde ("Suavidad de la orilla", Range(0.2, 6)) = 1.5
        _Suave ("Suavidad al tocar (m)", Range(0.05, 3)) = 0.8
        _Ruido ("Polvo en la niebla", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }

        Pass
        {
            Name "Halo"
            Tags { "LightMode" = "UniversalForward" }
            Blend One One
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            float _T501Noche;
            TEXTURE2D(_Ruido);
            SAMPLER(sampler_Ruido);

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half _Intensidad;
                half _Caida;
                half _Borde;
                half _Suave;
                float4 _Ruido_ST;
            CBUFFER_END

            struct Entrada
            {
                float4 posicion : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Salida
            {
                float4 posicion : SV_POSITION;
                float3 mundo : TEXCOORD0;
                float3 normal : TEXCOORD1;
                float2 uv : TEXCOORD2;
                float4 pantalla : TEXCOORD3;
                half niebla : TEXCOORD4;
            };

            Salida vert(Entrada entrada)
            {
                Salida salida;
                salida.mundo = TransformObjectToWorld(entrada.posicion.xyz);
                salida.normal = TransformObjectToWorldNormal(entrada.normal);
                salida.posicion = TransformWorldToHClip(salida.mundo);
                salida.uv = entrada.uv;
                salida.pantalla = ComputeScreenPos(salida.posicion);
                salida.niebla = ComputeFogFactor(salida.posicion.z);
                return salida;
            }

            half4 frag(Salida salida) : SV_Target
            {
                half3 vista = normalize(GetWorldSpaceViewDir(salida.mundo));
                half frente = pow(saturate(abs(dot(normalize(salida.normal), vista))), _Borde);
                half largo = pow(saturate(1.0 - salida.uv.y), _Caida);
                float2 uvPantalla = salida.pantalla.xy / salida.pantalla.w;
                float escena = LinearEyeDepth(SampleSceneDepth(uvPantalla), _ZBufferParams);
                float propia = LinearEyeDepth(salida.posicion.z, _ZBufferParams);
                half toque = saturate((escena - propia) / _Suave);
                half polvo = 0.7 + 0.3 * SAMPLE_TEXTURE2D(_Ruido, sampler_Ruido, salida.uv * _Ruido_ST.xy + _Ruido_ST.zw).r;
                half3 color = _Color.rgb * (_Intensidad * frente * largo * toque * polvo * saturate(_T501Noche));
                color = MixFogColor(color, half3(0, 0, 0), salida.niebla);
                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }
}
