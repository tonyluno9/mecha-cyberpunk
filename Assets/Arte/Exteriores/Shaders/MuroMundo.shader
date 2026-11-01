// T-506: material de superficie con la textura puesta en metros del mundo (por la normal), para que una caja de cualquier tamaño o giro
// no estire la textura. Iluminado como URP/Lit (luces principales y adicionales, sombras, Forward+, niebla).
Shader "MechaCyberpunk/MuroMundo"
{
    Properties
    {
        [MainColor] _BaseColor("Tinte", Color) = (1, 1, 1, 1)
        [MainTexture] _BaseMap("Textura de lados", 2D) = "white" {}
        _TopMap("Textura de arriba y abajo", 2D) = "white" {}
        _Escala("Metros por repetición (x, y)", Vector) = (3, 3, 0, 0)
        _Smoothness("Brillo", Range(0, 1)) = 0.25
        _Metallic("Metal", Range(0, 1)) = 0
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile _ _FORWARD_PLUS
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            TEXTURE2D(_TopMap); SAMPLER(sampler_TopMap);
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _BaseMap_ST;
                float4 _TopMap_ST;
                float4 _Escala;
                float _Smoothness;
                float _Metallic;
            CBUFFER_END

            struct Atributos { float4 pos : POSITION; float3 normal : NORMAL; };
            struct Varyings
            {
                float4 clip : SV_POSITION;
                float3 mundo : TEXCOORD0;
                float3 n : TEXCOORD1;
                float4 fogYSombra : TEXCOORD2;
            };

            Varyings vert(Atributos a)
            {
                Varyings o;
                float3 w = TransformObjectToWorld(a.pos.xyz);
                o.clip = TransformWorldToHClip(w);
                o.mundo = w;
                o.n = TransformObjectToWorldNormal(a.normal);
                o.fogYSombra = float4(ComputeFogFactor(o.clip.z), 0, 0, 0);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float3 n = normalize(i.n);
                float3 an = abs(n);
                half3 tex;
                if (an.y > 0.6)
                {
                    tex = SAMPLE_TEXTURE2D(_TopMap, sampler_TopMap, i.mundo.xz / _Escala.xx).rgb;
                }
                else
                {
                    float2 uv = an.x > an.z ? float2(i.mundo.z, i.mundo.y) : float2(i.mundo.x, i.mundo.y);
                    tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv / _Escala.xy).rgb;
                }
                SurfaceData sd = (SurfaceData)0;
                sd.albedo = tex * _BaseColor.rgb;
                sd.metallic = _Metallic;
                sd.smoothness = _Smoothness;
                sd.occlusion = 1;
                sd.alpha = 1;
                sd.normalTS = half3(0, 0, 1);

                InputData id = (InputData)0;
                id.positionWS = i.mundo;
                id.normalWS = n;
                id.viewDirectionWS = GetWorldSpaceNormalizeViewDir(i.mundo);
                id.shadowCoord = TransformWorldToShadowCoord(i.mundo);
                id.fogCoord = i.fogYSombra.x;
                id.bakedGI = SampleSH(n);
                id.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(i.clip);
                id.shadowMask = half4(1, 1, 1, 1);
                half4 c = UniversalFragmentPBR(id, sd);
                c.rgb = MixFog(c.rgb, id.fogCoord);
                c.a = 1;
                return c;
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct Atributos { float4 pos : POSITION; float3 normal : NORMAL; };
            struct Varyings { float4 clip : SV_POSITION; };

            Varyings vert(Atributos a)
            {
                Varyings o;
                float3 w = TransformObjectToWorld(a.pos.xyz);
                float3 n = TransformObjectToWorldNormal(a.normal);
            #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                float3 dir = normalize(_LightPosition - w);
            #else
                float3 dir = _LightDirection;
            #endif
                float4 c = TransformWorldToHClip(ApplyShadowBias(w, n, dir));
            #if UNITY_REVERSED_Z
                c.z = min(c.z, UNITY_NEAR_CLIP_VALUE);
            #else
                c.z = max(c.z, UNITY_NEAR_CLIP_VALUE);
            #endif
                o.clip = c;
                return o;
            }

            half4 frag(Varyings i) : SV_Target { return 0; }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Atributos { float4 pos : POSITION; };
            struct Varyings { float4 clip : SV_POSITION; };
            Varyings vert(Atributos a) { Varyings o; o.clip = TransformObjectToHClip(a.pos.xyz); return o; }
            half frag(Varyings i) : SV_Target { return i.clip.z; }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            ZWrite On
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Atributos { float4 pos : POSITION; float3 normal : NORMAL; };
            struct Varyings { float4 clip : SV_POSITION; float3 n : TEXCOORD0; };
            Varyings vert(Atributos a) { Varyings o; o.clip = TransformObjectToHClip(a.pos.xyz); o.n = TransformObjectToWorldNormal(a.normal); return o; }
            half4 frag(Varyings i) : SV_Target { return half4(NormalizeNormalPerPixel(i.n), 0); }
            ENDHLSL
        }
    }
}
