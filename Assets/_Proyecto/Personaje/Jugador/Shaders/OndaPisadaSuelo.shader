// T-1525: onda de una pisada sobre cualquier piso mojado que no usa el shader de charco (asfalto mojado, banquetas, decals de charco).
// Dibuja exactamente el mismo anillo que MechaCyberpunk/Mundo/Charco (T-1503: cresta que se abre a 1.5 m/s y se apaga en 2.2 s) pero sumado
// sobre el piso, sin cambiar el material del piso. Un cuadro horizontal por pisada (AnillosSuelo); _Origen = xyz posicion, w momento.
Shader "MechaCyberpunk/Personaje/OndaPisadaSuelo"
{
    Properties
    {
        _Origen ("Origen (xyz) y momento (w)", Vector) = (0, 0, 0, -100)
        _ColorOnda ("Color de la onda", Color) = (0.55, 0.7, 0.85, 1)
        _Intensidad ("Intensidad", Range(0, 2)) = 1
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent-50" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }

        Pass
        {
            Name "OndaSuelo"
            Tags { "LightMode" = "UniversalForward" }
            Blend One One
            ZWrite Off
            Cull Off
            Offset -2, -2

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            float _TiempoOndasCharco; // global: la publica EfectosPieJugador con Time.time

            CBUFFER_START(UnityPerMaterial)
                float4 _Origen;
                half4 _ColorOnda;
                half _Intensidad;
            CBUFFER_END

            struct Entrada
            {
                float4 posicion : POSITION;
            };

            struct Salida
            {
                float4 posicion : SV_POSITION;
                float3 mundo : TEXCOORD0;
            };

            Salida vert(Entrada entrada)
            {
                Salida salida;
                salida.mundo = TransformObjectToWorld(entrada.posicion.xyz);
                salida.posicion = TransformWorldToHClip(salida.mundo);
                return salida;
            }

            half4 frag(Salida salida) : SV_Target
            {
                float edad = _TiempoOndasCharco - _Origen.w;
                float d = length(salida.mundo.xz - _Origen.xz);
                float x = d - edad * 1.5;
                if (edad < 0.0 || edad > 2.2 || abs(x) > 0.5)
                {
                    return half4(0, 0, 0, 0);
                }
                float vida = saturate(1.0 - edad / 2.2);
                float cresta = exp(-x * x * 40.0) * vida * vida;
                float onda = sin(x * 30.0) * cresta;
                half brillo = saturate(max(onda, 0.0)) * 0.5 + saturate(cresta) * 0.12;
                return half4(_ColorOnda.rgb * (brillo * _Intensidad), 1);
            }
            ENDHLSL
        }
    }
}
