// T-1461: derrame de color de un neón en la pared y el piso. Quads aditivos con el color en el color de vértice (la malla de la zona trae todos los quads);
// la caída es radial desde el centro del quad (UV de 0 a 1). Sin escribir profundidad, la niebla lo apaga a lo lejos y de día no se ve (_T501Noche).
Shader "MechaCyberpunk/Fondos/DerrameNeon"
{
    Properties
    {
        _Intensidad ("Intensidad", Range(0, 4)) = 0.5
        _Caida ("Caída radial", Range(0.5, 6)) = 2
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Pass
        {
            Name "Derrame"
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
                half _Intensidad;
                half _Caida;
            CBUFFER_END

            struct Atributos { float4 pos : POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };
            struct Varyings { float4 clip : SV_POSITION; float2 uv : TEXCOORD0; half4 color : TEXCOORD1; half niebla : TEXCOORD2; };

            Varyings vert(Atributos a)
            {
                Varyings o;
                o.clip = TransformObjectToHClip(a.pos.xyz);
                o.uv = a.uv;
                o.color = a.color;
                o.niebla = ComputeFogFactor(o.clip.z);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                half r = saturate(1.0 - length(i.uv * 2.0 - 1.0));
                half f = pow(r, _Caida);
                half3 color = i.color.rgb * (_Intensidad * f * saturate(_T501Noche));
                color = MixFogColor(color, half3(0, 0, 0), i.niebla);
                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
