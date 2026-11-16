// T-1103: torres grandes (Neón Alto y Nova) que se ven nítidas de lejos. Iluminado como URP/Lit (sol, luces, sombras recibidas, Forward+),
// con emisión de ventanas y pantallas que sube de noche (global _T501Noche de VolumenSigueNoche). La niebla es la de Unity (sigue el color y
// la densidad del ciclo de día y noche) pero la distancia se multiplica por _FactorNiebla antes de calcularla y nunca se pierde más de
// _NieblaMaxima del color. Solo lo usan las torres: el resto de la ciudad conserva su niebla normal.
Shader "MechaCyberpunk/TorreNitida"
{
    Properties
    {
        [MainColor] _BaseColor("Color", Color) = (1, 1, 1, 1)
        [MainTexture] _BaseMap("Textura", 2D) = "white" {}
        _Smoothness("Brillo", Range(0, 1)) = 0.3
        _Metallic("Metal", Range(0, 1)) = 0
        _EmissionMap("Emisión (ventanas y pantallas)", 2D) = "white" {}
        [HDR] _EmissionColor("Color de emisión", Color) = (0, 0, 0, 1)
        _EmisionDia("Emisión de día (0 a 1)", Range(0, 1)) = 0.12
        _FactorNiebla("Factor de niebla (0 sin niebla, 1 normal)", Range(0, 1)) = 0.3
        _NieblaMaxima("Niebla máxima (fracción del color)", Range(0, 1)) = 0.45
    }

    HLSLINCLUDE
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
    TEXTURE2D(_EmissionMap); SAMPLER(sampler_EmissionMap);
    float _T501Noche;
    CBUFFER_START(UnityPerMaterial)
        float4 _BaseColor;
        float4 _BaseMap_ST;
        float4 _EmissionMap_ST;
        float4 _EmissionColor;
        float _Smoothness;
        float _Metallic;
        float _EmisionDia;
        float _FactorNiebla;
        float _NieblaMaxima;
    CBUFFER_END
    ENDHLSL

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
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Atributos { float4 pos : POSITION; float3 normal : NORMAL; float2 uv : TEXCOORD0; };
            struct Varyings
            {
                float4 clip : SV_POSITION;
                float3 mundo : TEXCOORD0;
                float3 n : TEXCOORD1;
                float2 uv : TEXCOORD2;
                float niebla : TEXCOORD3;
            };

            Varyings vert(Atributos a)
            {
                Varyings o;
                float3 w = TransformObjectToWorld(a.pos.xyz);
                o.clip = TransformWorldToHClip(w);
                o.mundo = w;
                o.n = TransformObjectToWorldNormal(a.normal);
                o.uv = a.uv * _BaseMap_ST.xy + _BaseMap_ST.zw;
                // La distancia se encoge con _FactorNiebla antes de la niebla de Unity (lineal, exp o exp2).
                o.niebla = ComputeFogFactorZ0ToFar(distance(w, _WorldSpaceCameraPos) * _FactorNiebla);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float3 n = normalize(i.n);
                half4 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv);
                SurfaceData sd = (SurfaceData)0;
                sd.albedo = tex.rgb * _BaseColor.rgb;
                sd.metallic = _Metallic;
                sd.smoothness = _Smoothness;
                sd.occlusion = 1;
                sd.alpha = 1;
                sd.normalTS = half3(0, 0, 1);
                half noche = saturate(_T501Noche);
                sd.emission = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, i.uv).rgb * _EmissionColor.rgb * lerp(_EmisionDia, 1.0, noche);

                InputData id = (InputData)0;
                id.positionWS = i.mundo;
                id.normalWS = n;
                id.viewDirectionWS = GetWorldSpaceNormalizeViewDir(i.mundo);
                id.shadowCoord = TransformWorldToShadowCoord(i.mundo);
                id.fogCoord = i.niebla;
                id.bakedGI = SampleSH(n);
                id.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(i.clip);
                id.shadowMask = half4(1, 1, 1, 1);
                half4 c = UniversalFragmentPBR(id, sd);
                // Niebla de Unity con tope: la intensidad (1 = nítido) nunca baja de 1 - _NieblaMaxima.
                half intensidad = max(ComputeFogIntensity(i.niebla), 1.0 - _NieblaMaxima);
                c.rgb = lerp(unity_FogColor.rgb, c.rgb, intensidad);
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
            struct Atributos { float4 pos : POSITION; float3 normal : NORMAL; };
            struct Varyings { float4 clip : SV_POSITION; float3 n : TEXCOORD0; };
            Varyings vert(Atributos a) { Varyings o; o.clip = TransformObjectToHClip(a.pos.xyz); o.n = TransformObjectToWorldNormal(a.normal); return o; }
            half4 frag(Varyings i) : SV_Target { return half4(NormalizeNormalPerPixel(i.n), 0); }
            ENDHLSL
        }
    }
}
