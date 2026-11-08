// Solo para la captura del mapa (T-747): pinta la altura del mundo (y) de cada punto en dos canales de 8 bits (R alto, G bajo).
// altura = ((R*255*256 + G*255) / 100) - 2 metros. No entra en ninguna compilacion (carpeta Editor).
Shader "Hidden/MechaCyberpunk/AlturaMapa"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }
        Pass
        {
            Name "Altura"
            Tags { "LightMode" = "UniversalForward" }
            Cull Off
            ZWrite On
            ZTest LEqual
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Entrada { float4 posicion : POSITION; };
            struct Salida { float4 pos : SV_POSITION; float y : TEXCOORD0; };

            Salida vert(Entrada e)
            {
                Salida s;
                float3 mundo = TransformObjectToWorld(e.posicion.xyz);
                s.pos = TransformWorldToHClip(mundo);
                s.y = mundo.y;
                return s;
            }

            float4 frag(Salida s) : SV_Target
            {
                float v = clamp(round((s.y + 2.0) * 100.0), 1.0, 65000.0);
                float alto = floor(v / 256.0);
                float bajo = v - alto * 256.0;
                return float4(alto / 255.0, bajo / 255.0, 0.0, 1.0);
            }
            ENDHLSL
        }
    }
}
