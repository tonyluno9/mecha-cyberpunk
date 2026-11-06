// T-808: hueco de bala sobre una pared o un carro (un quad pegado a la superficie). Textura con alfa (D01 concreto o D02 metal, con el fondo
// blanco ya quitado al importar) por _Color; el alfa de _Color lo apaga al final. Sin luces, sin escribir profundidad y un poco hacia la cámara
// para no pelear con la pared.
Shader "MechaCyberpunk/Personaje/HuecoBala"
{
    Properties
    {
        _MainTex ("Hueco", 2D) = "white" {}
        _Color ("Color", Color) = (1, 1, 1, 1)
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent-50" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }

        Pass
        {
            Name "Hueco"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Offset -1, -1
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _Color;
            CBUFFER_END

            struct Entrada
            {
                float4 posicion : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Salida
            {
                float4 posicion : SV_POSITION;
                float2 uv : TEXCOORD0;
                half niebla : TEXCOORD1;
            };

            Salida vert(Entrada entrada)
            {
                Salida salida;
                salida.posicion = TransformObjectToHClip(entrada.posicion.xyz);
                salida.uv = TRANSFORM_TEX(entrada.uv, _MainTex);
                salida.niebla = ComputeFogFactor(salida.posicion.z);
                return salida;
            }

            half4 frag(Salida salida) : SV_Target
            {
                half4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, salida.uv) * _Color;
                color.rgb = MixFog(color.rgb, salida.niebla);
                return color;
            }
            ENDHLSL
        }
    }
}
