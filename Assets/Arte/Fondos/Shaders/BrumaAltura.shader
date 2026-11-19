// T-1399: plano de bruma de altura (solo en las zonas nuevas). Un quad grande y horizontal, a unos metros sobre la calle: transparente, sin escribir
// profundidad, sin niebla propia. Su opacidad crece con la distancia a la cámara (nada cerca, lechoso a 100-250 m), se desvanece en las orillas del plano
// y de día no se ve (_T501Noche, global de VolumenSigueNoche). Tapa lo lejano con el color de la bruma, como la niebla azul grisácea de K18 y K15.
Shader "MechaCyberpunk/Fondos/BrumaAltura"
{
    Properties
    {
        _Color ("Color de la bruma", Color) = (0.34, 0.42, 0.57, 1)
        _Intensidad ("Opacidad máxima", Range(0, 1)) = 0.5
        _Cerca ("Distancia donde empieza (m)", Float) = 30
        _Lejos ("Distancia de opacidad máxima (m)", Float) = 220
        _Orilla ("Desvanecido de las orillas (fracción)", Range(0.01, 0.5)) = 0.25
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent-20" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Pass
        {
            Name "Bruma"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            float _T501Noche;

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half _Intensidad;
                float _Cerca;
                float _Lejos;
                half _Orilla;
            CBUFFER_END

            struct Atributos { float4 pos : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 clip : SV_POSITION; float2 uv : TEXCOORD0; float3 mundo : TEXCOORD1; };

            Varyings vert(Atributos a)
            {
                Varyings o;
                o.mundo = TransformObjectToWorld(a.pos.xyz);
                o.clip = TransformWorldToHClip(o.mundo);
                o.uv = a.uv;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float d = distance(i.mundo, GetCameraPositionWS());
                half distancia = saturate((d - _Cerca) / max(_Lejos - _Cerca, 1.0));
                half2 e = smoothstep(0.0, _Orilla, i.uv) * smoothstep(0.0, _Orilla, 1.0 - i.uv);
                half alfa = _Intensidad * distancia * distancia * (3.0 - 2.0 * distancia) * e.x * e.y * saturate(_T501Noche);
                return half4(_Color.rgb, alfa);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
