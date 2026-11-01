// T-501: torres del horizonte. Sin luces ni sombras: ventanas encendidas por código (celdas del mundo) o una textura de pantalla.
// La niebla del juego se come todo a más de 100 m, así que este shader mezcla con su propio color de niebla (_Niebla) y no usa la del juego.
Shader "MechaCyberpunk/TorreHorizonte"
{
    Properties
    {
        [HDR] _ColorBase("Color base", Color) = (0.02, 0.03, 0.08, 1)
        _MainTex("Textura (modo pantalla)", 2D) = "white" {}
        [Toggle] _UsarTextura("Usar textura", Float) = 0
        _Semilla("Semilla", Float) = 0
        _Ventana("Celda de ventana (m)", Vector) = (3, 3.6, 0, 0)
        _ProbLuz("Probabilidad de luz", Range(0, 1)) = 0.45
        _Brillo("Brillo", Float) = 2
        [HDR] _ColorNiebla("Color de niebla", Color) = (0.08, 0.1, 0.25, 1)
        _Niebla("Mezcla de niebla", Range(0, 1)) = 0.45
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }
        Pass
        {
            Name "Unlit"
            Tags { "LightMode" = "UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            float _T501Noche;
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _ColorBase;
                float4 _MainTex_ST;
                float _UsarTextura;
                float _Semilla;
                float4 _Ventana;
                float _ProbLuz;
                float _Brillo;
                float4 _ColorNiebla;
                float _Niebla;
            CBUFFER_END

            struct Atributos { float4 pos : POSITION; float3 normal : NORMAL; float2 uv : TEXCOORD0; };
            struct Varyings { float4 clip : SV_POSITION; float3 mundo : TEXCOORD0; float3 n : TEXCOORD1; float2 uv : TEXCOORD2; };

            Varyings vert(Atributos a)
            {
                Varyings o;
                float3 w = TransformObjectToWorld(a.pos.xyz);
                o.clip = TransformWorldToHClip(w);
                o.mundo = w;
                o.n = TransformObjectToWorldNormal(a.normal);
                o.uv = a.uv * _MainTex_ST.xy + _MainTex_ST.zw;
                return o;
            }

            float Hash(float2 p)
            {
                return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
            }

            half4 frag(Varyings i) : SV_Target
            {
                float noche = saturate(_T501Noche);
                float3 col = _ColorBase.rgb;
                if (_UsarTextura > 0.5)
                {
                    col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv).rgb * _Brillo * noche;
                }
                else if (abs(i.n.y) < 0.5)
                {
                    float2 uv = abs(i.n.x) > 0.5 ? i.mundo.zy : i.mundo.xy;
                    float2 celda = floor(uv / _Ventana.xy);
                    float2 f = frac(uv / _Ventana.xy);
                    float h = Hash(celda + _Semilla);
                    float h2 = Hash(celda * 1.7 + _Semilla + 3.1);
                    bool ventana = f.x > 0.25 && f.x < 0.75 && f.y > 0.3 && f.y < 0.7;
                    bool luz = h > (1.0 - _ProbLuz);
                    float3 tinte = h2 < 0.55 ? float3(1.0, 0.78, 0.45) : (h2 < 0.8 ? float3(0.55, 0.85, 1.0) : (h2 < 0.9 ? float3(1.0, 0.25, 0.8) : float3(0.4, 1.0, 0.5)));
                    col += (ventana && luz) ? tinte * _Brillo * noche : 0;
                }
                float3 colorNiebla = lerp(float3(0.42, 0.58, 0.85), _ColorNiebla.rgb, noche);
                col = lerp(col, colorNiebla, _Niebla);
                return half4(col, 1);
            }
            ENDHLSL
        }
    }
}
