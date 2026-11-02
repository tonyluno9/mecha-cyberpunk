// T-513: fondo lejano de la ciudad (matte painting) en un cilindro alrededor de Ciudad_Prologo. Sin luces, sin sombras y sin la niebla del juego.
// Arriba se vuelve transparente para fundirse con el cielo real (de día y de noche). De día la pintura (que es de noche) se aclara y se mezcla con
// una bruma del color del cielo diurno. _T501Noche (global, VolumenSigueNoche) va de 0 (día) a 1 (noche).
Shader "MechaCyberpunk/FondoCiudad"
{
    Properties
    {
        _MainTex("Pintura", 2D) = "white" {}
        _BrilloNoche("Brillo de noche", Float) = 0.2
        _BrilloDia("Brillo de día", Float) = 1.4
        [HDR] _ColorBrumaDia("Bruma de día", Color) = (0.55, 0.66, 0.85, 1)
        _BrumaDia("Mezcla de bruma de día", Range(0, 1)) = 0.6
        [HDR] _ColorBrumaNoche("Bruma de noche", Color) = (0.02, 0.03, 0.08, 1)
        _BrumaNoche("Mezcla de bruma de noche", Range(0, 1)) = 0.15
        _InicioCielo("Altura (0-1) donde empieza a fundirse con el cielo", Range(0, 1)) = 0.55
        _FinCielo("Altura (0-1) donde ya es cielo", Range(0, 1)) = 0.85
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent-100" "IgnoreProjector" = "True" }
        Pass
        {
            Name "Unlit"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            float _T501Noche;
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float _BrilloNoche;
                float _BrilloDia;
                float4 _ColorBrumaDia;
                float _BrumaDia;
                float4 _ColorBrumaNoche;
                float _BrumaNoche;
                float _InicioCielo;
                float _FinCielo;
            CBUFFER_END

            struct Atributos { float4 pos : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 clip : SV_POSITION; float2 uv : TEXCOORD0; };

            Varyings vert(Atributos a)
            {
                Varyings o;
                o.clip = TransformObjectToHClip(a.pos.xyz);
                o.uv = a.uv * _MainTex_ST.xy + _MainTex_ST.zw;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float noche = saturate(_T501Noche);
                float3 pintura = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv).rgb;
                float3 deNoche = lerp(pintura * _BrilloNoche, _ColorBrumaNoche.rgb, _BrumaNoche);
                float gris = dot(pintura, float3(0.3, 0.59, 0.11));
                float3 deDia = lerp(lerp(pintura, gris.xxx, 0.5) * _BrilloDia, _ColorBrumaDia.rgb, _BrumaDia);
                float3 col = lerp(deDia, deNoche, noche);
                float alfa = 1.0 - smoothstep(_InicioCielo, _FinCielo, i.uv.y);
                return half4(col, alfa);
            }
            ENDHLSL
        }
    }
}
