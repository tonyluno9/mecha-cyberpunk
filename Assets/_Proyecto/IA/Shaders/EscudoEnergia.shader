// T-1110: esfera del escudo de energía del jefe final de Nova. Translúcida (alfa bajo en el centro) con el borde más brillante (fresnel),
// para que el jefe se vea con su textura dentro. _Brillo sube un instante cuando el escudo recibe un golpe.
Shader "MechaCyberpunk/EscudoEnergia"
{
    Properties
    {
        _Color ("Color", Color) = (0.25, 0.9, 1, 1)
        _AlfaCentro ("Alfa en el centro", Range(0, 1)) = 0.2
        _Potencia ("Potencia del borde", Range(0.5, 8)) = 3
        _Brillo ("Brillo", Float) = 1
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            Name "Escudo"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half _AlfaCentro;
                half _Potencia;
                half _Brillo;
            CBUFFER_END

            struct Entrada
            {
                float4 posicion : POSITION;
                float3 normal : NORMAL;
            };

            struct Salida
            {
                float4 posicionClip : SV_POSITION;
                float3 normal : TEXCOORD0;
                float3 haciaCamara : TEXCOORD1;
            };

            Salida vert(Entrada i)
            {
                Salida o;
                float3 mundo = TransformObjectToWorld(i.posicion.xyz);
                o.posicionClip = TransformWorldToHClip(mundo);
                o.normal = TransformObjectToWorldNormal(i.normal);
                o.haciaCamara = GetWorldSpaceViewDir(mundo);
                return o;
            }

            half4 frag(Salida i) : SV_Target
            {
                half borde = pow(1.0h - saturate(dot(normalize(i.normal), normalize(i.haciaCamara))), _Potencia);
                half alfa = saturate(_AlfaCentro + borde * (1.0h - _AlfaCentro)) * saturate(_Brillo * 0.8h + 0.2h);
                half3 color = _Color.rgb * _Brillo * (0.3h + borde * 1.8h);
                return half4(color, alfa);
            }
            ENDHLSL
        }
    }
}
