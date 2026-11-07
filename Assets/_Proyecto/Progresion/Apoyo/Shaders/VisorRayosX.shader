// T-866: silueta de un enemigo vista a través de las cosas (visor). Solo se dibuja donde hay algo enfrente (ZTest Greater), aditiva, más fuerte
// en la orilla (fresnel) para que se lea la forma.
Shader "MechaCyberpunk/Progresion/VisorRayosX"
{
    Properties
    {
        _Color ("Color", Color) = (1, 0.45, 0.1, 1)
        _Fuerza ("Fuerza", Range(0, 4)) = 1.4
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent+100" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "RayosX"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Blend One One
            ZWrite Off
            ZTest Greater
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half _Fuerza;
            CBUFFER_END

            struct Entrada { float4 posicion : POSITION; float3 normal : NORMAL; };
            struct Salida { float4 posicion : SV_POSITION; float3 normal : TEXCOORD0; float3 vista : TEXCOORD1; };

            Salida vert(Entrada e)
            {
                Salida s;
                float3 mundo = TransformObjectToWorld(e.posicion.xyz);
                s.posicion = TransformWorldToHClip(mundo);
                s.normal = TransformObjectToWorldNormal(e.normal);
                s.vista = GetWorldSpaceViewDir(mundo);
                return s;
            }

            half4 frag(Salida s) : SV_Target
            {
                half orilla = 1.0 - saturate(dot(normalize(s.normal), normalize(s.vista)));
                half luz = (0.25 + orilla * orilla) * _Fuerza;
                return half4(_Color.rgb * luz, 1.0);
            }
            ENDHLSL
        }
    }
}
