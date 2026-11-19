// T-1399: halos cálidos de la niebla alrededor de los faroles de sodio. Cada farol es un quad que mira siempre a la cámara (la malla guarda el centro del
// farol en la posición y la esquina en el UV, de -1 a 1; el vértice lo abre en el espacio de la vista). Aditivo, sin escribir profundidad, con la niebla
// del juego (a lo lejos se apaga) y de día no se ve (_T501Noche). Una sola malla por zona = un dibujo por zona.
Shader "MechaCyberpunk/Fondos/BrumaHalos"
{
    Properties
    {
        [HDR] _Color ("Color del halo", Color) = (1, 0.55, 0.2, 1)
        _Intensidad ("Intensidad", Range(0, 3)) = 0.45
        _Tamano ("Radio del halo (m)", Float) = 4.5
        _CercaCamara ("Se apaga a menos de (m)", Float) = 3
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Pass
        {
            Name "Halos"
            Tags { "LightMode" = "UniversalForward" }
            Blend One One
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            float _T501Noche;

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half _Intensidad;
                float _Tamano;
                float _CercaCamara;
            CBUFFER_END

            struct Atributos { float4 pos : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 clip : SV_POSITION; float2 uv : TEXCOORD0; half cerca : TEXCOORD1; half niebla : TEXCOORD2; };

            Varyings vert(Atributos a)
            {
                Varyings o;
                float3 centro = TransformObjectToWorld(a.pos.xyz);
                float3 vista = TransformWorldToView(centro);
                o.cerca = saturate((length(vista) - _CercaCamara) / 6.0);
                vista.xy += a.uv * _Tamano;
                o.clip = TransformWViewToHClip(vista);
                o.uv = a.uv;
                o.niebla = ComputeFogFactor(o.clip.z);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                half r = saturate(1.0 - length(i.uv));
                half f = r * r * r;
                half3 color = _Color.rgb * (_Intensidad * f * i.cerca * saturate(_T501Noche));
                color = MixFogColor(color, half3(0, 0, 0), i.niebla);
                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
