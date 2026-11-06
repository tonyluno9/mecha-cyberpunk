// T-808: no dibuja nada. Es el de Greybox_Marca: el cubito "MarcaImpacto" de PistolaAPie sigue existiendo pero ya no se ve (los huecos los
// pone HuecosBala).
Shader "MechaCyberpunk/Personaje/Invisible"
{
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }

        Pass
        {
            Name "Invisible"
            Tags { "LightMode" = "UniversalForward" }
            ColorMask 0
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            float4 vert(float4 posicion : POSITION) : SV_POSITION
            {
                return TransformObjectToHClip(posicion.xyz);
            }

            half4 frag() : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }
}
