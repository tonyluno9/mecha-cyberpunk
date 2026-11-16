// T-1103: haz de luz vertical de la torre de Nova (cilindro de Unity de 2 m de alto en su espacio local). Aditivo, sin sombras ni profundidad,
// más fuerte abajo y se apaga arriba; el borde del cilindro se difumina con el ángulo de vista. No lleva niebla: se ve de lejos.
Shader "MechaCyberpunk/TorreHaz"
{
    Properties
    {
        [HDR] _Color("Color", Color) = (0.3, 0.9, 1.8, 1)
        _Potencia("Caida del borde", Range(0.5, 6)) = 2
        _Altura("Caida hacia arriba", Range(0.2, 4)) = 1.2
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" }
        Pass
        {
            Name "Haz"
            Tags { "LightMode" = "UniversalForward" }
            Blend One One
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float _Potencia;
                float _Altura;
            CBUFFER_END

            struct Atributos { float4 pos : POSITION; float3 normal : NORMAL; };
            struct Varyings { float4 clip : SV_POSITION; float3 n : TEXCOORD0; float3 vista : TEXCOORD1; float y : TEXCOORD2; };

            Varyings vert(Atributos a)
            {
                Varyings o;
                float3 w = TransformObjectToWorld(a.pos.xyz);
                o.clip = TransformWorldToHClip(w);
                o.n = TransformObjectToWorldNormal(a.normal);
                o.vista = _WorldSpaceCameraPos - w;
                o.y = saturate((a.pos.y + 1.0) * 0.5);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float borde = pow(saturate(abs(dot(normalize(i.n), normalize(i.vista)))), _Potencia);
                float caida = pow(saturate(1.0 - i.y), _Altura);
                return half4(_Color.rgb * borde * caida, 1);
            }
            ENDHLSL
        }
    }
}
