// Contornos finos de pantalla completa (docs/plan-look-ciudad.md §2.2), para el look ilustrado de K01. Va en un Full Screen Pass del
// renderer antes del posproceso. Detecta bordes con la profundidad (laplaciano de 1/z, que en un plano es cero: así el piso rasante no
// sale rayado) y con las normales (pliegues). La línea oscurece el color y se desvanece con la distancia; el cielo no lleva línea.
Shader "MechaCyberpunk/Mundo/Contornos"
{
    Properties
    {
        _ColorLinea ("Color de la línea", Color) = (0.04, 0.04, 0.07, 1)
        _Fuerza ("Fuerza", Range(0, 1)) = 0.75
        _Grosor ("Grosor (px)", Range(0.5, 3)) = 1
        _UmbralProfundidad ("Umbral de profundidad", Range(0.001, 0.5)) = 0.06
        _UmbralNormal ("Umbral de normales", Range(0.01, 1)) = 0.35
        _Inicio ("Empieza a desvanecerse (m)", Float) = 25
        _Fin ("Ya no hay línea (m)", Float) = 110
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off
        ZTest Always
        Cull Off

        Pass
        {
            Name "Contornos"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"

            half4 _ColorLinea;
            half _Fuerza;
            half _Grosor;
            half _UmbralProfundidad;
            half _UmbralNormal;
            float _Inicio;
            float _Fin;

            float InvZ(float2 uv)
            {
                return 1.0 / LinearEyeDepth(SampleSceneDepth(uv), _ZBufferParams);
            }

            half4 Frag(Varyings entrada) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(entrada);
                float2 uv = entrada.texcoord;
                half4 color = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);
                float crudo = SampleSceneDepth(uv);
                #if UNITY_REVERSED_Z
                    bool cielo = crudo <= 0.000001;
                #else
                    bool cielo = crudo >= 0.999999;
                #endif
                if (cielo)
                {
                    return color;
                }
                float2 px = _Grosor / _ScreenParams.xy;
                float z = LinearEyeDepth(crudo, _ZBufferParams);
                float c = 1.0 / z;
                float l = InvZ(uv - float2(px.x, 0));
                float r = InvZ(uv + float2(px.x, 0));
                float d = InvZ(uv - float2(0, px.y));
                float u = InvZ(uv + float2(0, px.y));
                // Laplaciano relativo de 1/z: cero en un plano, grande en una orilla o un pliegue de profundidad.
                float lap = abs(l + r + d + u - 4.0 * c) / max(c, 1e-6);
                half borde = saturate((lap - _UmbralProfundidad) / _UmbralProfundidad);

                half3 n0 = SampleSceneNormals(uv);
                half3 nl = SampleSceneNormals(uv - float2(px.x, 0));
                half3 nr = SampleSceneNormals(uv + float2(px.x, 0));
                half3 nd = SampleSceneNormals(uv - float2(0, px.y));
                half3 nu = SampleSceneNormals(uv + float2(0, px.y));
                half pliegue = 1.0 - min(min(dot(n0, nl), dot(n0, nr)), min(dot(n0, nd), dot(n0, nu)));
                borde = max(borde, saturate((pliegue - _UmbralNormal) / _UmbralNormal));

                half lejania = 1.0 - saturate((z - _Inicio) / max(_Fin - _Inicio, 0.01));
                half3 linea = color.rgb * _ColorLinea.rgb;
                color.rgb = lerp(color.rgb, linea, borde * _Fuerza * lejania);
                return color;
            }
            ENDHLSL
        }
    }
}
