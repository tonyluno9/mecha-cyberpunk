// T-635: capa del cielo (nubes o estrellas) en una esfera grande alrededor de la camara. Sin niebla y sin escribir profundidad:
// los edificios la tapan, y el desvanecido cerca del horizonte la funde con el cielo.
// T-806: la textura se proyecta sobre un plano alto (direccion xz / altura), no con las uv de la esfera, que la juntaban en el cenit
// como rayos de estrella; y se dibuja en el plano lejano, para que nunca quede delante de un edificio lejano ni del fondo pintado.
Shader "MechaCyberpunk/CieloTextura"
{
    Properties
    {
        _MainTex ("Textura", 2D) = "white" {}
        _Color ("Color y opacidad", Color) = (1, 1, 1, 1)
        _Desvanecer ("Altura donde termina el desvanecido del horizonte", Range(0.01, 1)) = 0.2
        _Escala ("Escala de la proyección (repeticiones por unidad de plano)", Float) = 0.35
        _Desplazar ("Movimiento (uv/s)", Vector) = (0.003, 0.0015, 0, 0)
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent-150" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Pass
        {
            Name "Cielo"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Front
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _Color;
                half _Desvanecer;
                float _Escala;
                float4 _Desplazar;
            CBUFFER_END

            struct Entrada
            {
                float4 posicion : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Salida
            {
                float4 posicion : SV_POSITION;
                float3 direccion : TEXCOORD0;
            };

            Salida vert(Entrada i)
            {
                Salida o;
                o.posicion = TransformObjectToHClip(i.posicion.xyz);
                // En el plano lejano: solo se ve donde no hay nada dibujado.
                o.posicion.z = UNITY_RAW_FAR_CLIP_VALUE * o.posicion.w;
                o.direccion = i.posicion.xyz;
                return o;
            }

            half4 frag(Salida i) : SV_Target
            {
                float3 d = normalize(i.direccion);
                float arriba = max(d.y, 0.0);
                float2 uv = d.xz / (arriba + 0.15) * _Escala + _Desplazar.xy * _Time.y;
                uv = uv * _MainTex_ST.xy + _MainTex_ST.zw;
                half4 t = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
                half a = t.a * _Color.a * smoothstep(0.0, _Desvanecer, arriba);
                return half4(t.rgb * _Color.rgb, a);
            }
            ENDHLSL
        }
    }
}
