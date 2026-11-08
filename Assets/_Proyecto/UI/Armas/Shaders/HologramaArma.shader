// T-814: holograma del menú de mejora de armas (URP, sin luces ni niebla). Cian con borde de Fresnel, líneas horizontales que suben,
// parpadeo y un poco de glitch en los vértices. Mezcla aditiva sobre fondo negro. _Resalte pinta la pieza elegida (naranja) y _Destello
// la hace brillar al comprar. Con _ModoImagen = 1 dibuja una imagen holográfica (H02-H04, fondo negro) en un quad con las mismas líneas.
Shader "MechaCyberpunk/UI/HologramaArma"
{
    Properties
    {
        _MainTex ("Imagen (modo imagen)", 2D) = "white" {}
        [HDR] _Color ("Color", Color) = (0.15, 0.85, 1, 1)
        [HDR] _ColorBorde ("Color del borde", Color) = (0.6, 1, 1, 1)
        [HDR] _ColorResalte ("Color de la pieza elegida", Color) = (1, 0.5, 0.1, 1)
        _Base ("Brillo del cuerpo", Range(0, 2)) = 0.22
        _Borde ("Brillo del borde", Range(0, 4)) = 1.4
        _PotenciaBorde ("Potencia del Fresnel", Range(0.5, 8)) = 2.2
        _DensidadLineas ("Líneas por unidad", Float) = 70
        _VelocidadLineas ("Velocidad de las líneas (u/s)", Float) = 0.35
        _GrosorLineas ("Grosor de las líneas (0 a 1)", Range(0, 1)) = 0.35
        _BrilloLineas ("Brillo de las líneas", Range(0, 2)) = 0.45
        _BandaAncha ("Brillo de la banda que barre", Range(0, 2)) = 0.6
        _Parpadeo ("Parpadeo (0 a 1)", Range(0, 1)) = 0.12
        _Glitch ("Glitch de vértices (u)", Range(0, 0.1)) = 0.012
        _Resalte ("Resalte (0 a 1)", Range(0, 1)) = 0
        _Destello ("Destello (0 a 1)", Range(0, 1)) = 0
        _Alfa ("Opacidad general", Range(0, 1)) = 1
        _ModoImagen ("Modo imagen (0 o 1)", Range(0, 1)) = 0
        _BarridoDir ("Eje del barrido (mundo)", Vector) = (1, 0, 0, 0)
        _BarridoPos ("Posición del barrido (m sobre el eje)", Float) = 0
        _BarridoAncho ("Ancho del barrido (m)", Float) = 0.06
        _BarridoFuerza ("Fuerza del barrido (0 a 1)", Range(0, 1)) = 0
        _Revelar ("Solo se ve lo ya barrido (0 o 1)", Range(0, 1)) = 0
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 2
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }

        Pass
        {
            Name "Holograma"
            Tags { "LightMode" = "UniversalForward" }
            Blend One One
            ZWrite Off
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _Color;
                half4 _ColorBorde;
                half4 _ColorResalte;
                half _Base;
                half _Borde;
                half _PotenciaBorde;
                float _DensidadLineas;
                float _VelocidadLineas;
                half _GrosorLineas;
                half _BrilloLineas;
                half _BandaAncha;
                half _Parpadeo;
                float _Glitch;
                half _Resalte;
                half _Destello;
                half _Alfa;
                half _ModoImagen;
                float4 _BarridoDir;
                float _BarridoPos;
                float _BarridoAncho;
                half _BarridoFuerza;
                half _Revelar;
            CBUFFER_END

            struct Entrada
            {
                float4 posicion : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Salida
            {
                float4 posicion : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalMundo : TEXCOORD1;
                float3 posMundo : TEXCOORD2;
            };

            float Azar(float n)
            {
                return frac(sin(n * 12.9898) * 43758.5453);
            }

            Salida vert(Entrada entrada)
            {
                Salida salida;
                float3 posMundo = TransformObjectToWorld(entrada.posicion.xyz);
                // Glitch: de vez en cuando una franja horizontal se corre de lado.
                float paso = floor(_Time.y * 14.0);
                float franja = floor(posMundo.y * 18.0);
                float salto = step(0.93, Azar(paso + franja * 0.37)) * (Azar(paso * 1.7 + franja) - 0.5) * 2.0;
                posMundo.x += salto * _Glitch;
                salida.posMundo = posMundo;
                salida.normalMundo = TransformObjectToWorldNormal(entrada.normal);
                salida.posicion = TransformWorldToHClip(posMundo);
                salida.uv = TRANSFORM_TEX(entrada.uv, _MainTex);
                return salida;
            }

            half4 frag(Salida salida) : SV_Target
            {
                float3 vista = normalize(GetCameraPositionWS() - salida.posMundo);
                float3 normal = normalize(salida.normalMundo);
                half fresnel = pow(1.0 - saturate(abs(dot(normal, vista))), _PotenciaBorde);

                // Líneas finas que suben y una banda ancha que barre de abajo arriba.
                float y = salida.posMundo.y * _DensidadLineas - _Time.y * _VelocidadLineas * _DensidadLineas;
                half lineas = smoothstep(_GrosorLineas, 0.0, abs(frac(y) - 0.5) * 2.0);
                float barrido = frac(salida.posMundo.y * 0.8 - _Time.y * 0.25);
                half banda = smoothstep(0.92, 1.0, barrido) * _BandaAncha;

                half parpadeo = 1.0 - _Parpadeo * (0.5 + 0.5 * sin(_Time.y * 37.0)) * step(0.6, Azar(floor(_Time.y * 9.0)));

                half3 tinte = lerp(_Color.rgb, _ColorResalte.rgb, _Resalte);
                half3 borde = lerp(_ColorBorde.rgb, _ColorResalte.rgb * 1.3, _Resalte);
                half3 cuerpo = tinte * (_Base + _Resalte * 0.25) + borde * fresnel * _Borde;
                half3 color3D = cuerpo + tinte * (lineas * _BrilloLineas + banda);

                half4 imagen = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, salida.uv);
                half luz = max(imagen.r, max(imagen.g, imagen.b)) * imagen.a;
                half3 colorImagen = imagen.rgb * imagen.a * (1.5 + lineas * _BrilloLineas * 0.6 + banda * 0.5) + tinte * luz * _Resalte * 0.4;

                half3 color = lerp(color3D, colorImagen, _ModoImagen);
                color += (_ColorBorde.rgb * 0.9 + 0.2) * _Destello * lerp(1.0, luz, _ModoImagen);
                // T-895: la franja que recorre la pieza al escanear; con _Revelar lo que falta por barrer no se dibuja (T-896).
                float avance = dot(salida.posMundo, _BarridoDir.xyz) - _BarridoPos;
                half franjaEscaneo = smoothstep(_BarridoAncho, 0.0, abs(avance)) * _BarridoFuerza;
                color += (_ColorBorde.rgb * 1.4 + 0.3) * franjaEscaneo;
                color *= lerp(1.0, step(avance, 0.0), _Revelar);
                color *= parpadeo * _Alfa;
                return half4(color, saturate(max(color.r, max(color.g, color.b))));
            }
            ENDHLSL
        }
    }
}
