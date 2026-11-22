// T14 (T-1605): capa de cielo con profundidad, encima del cielo base (CieloGradiente) y de las nubes de noche (NubesNoche). Domo alrededor de la cámara
// que solo se ve donde no hay geometría. De noche: degradado azul profundo a morado hacia el horizonte, estrellas tenues, luna con halo y nubes bajas
// procedurales (sin texturas) que se mueven lento y se iluminan desde abajo con el tinte de la ciudad (_TinteNubes, lo cambia CieloRealista por barrio).
// De día (_Noche = 0): solo nubes claras suaves. CieloRealista le pone _Noche, _TinteNubes y _DirLuna.
Shader "MechaCyberpunk/CieloRealista"
{
    Properties
    {
        _ColorCenit ("Cenit de noche", Color) = (0.012, 0.025, 0.11, 1)
        _ColorHorizonte ("Horizonte de noche", Color) = (0.24, 0.09, 0.34, 1)
        _FuerzaGradiente ("Fuerza del degradado de noche", Range(0, 1)) = 0.55
        _TinteNubes ("Tinte de la ciudad en las nubes", Color) = (1, 0.45, 0.15, 1)
        _ColorNubesNoche ("Color de las nubes de noche (sin luz)", Color) = (0.04, 0.045, 0.09, 1)
        _ColorNubesDia ("Color de las nubes de dia", Color) = (0.86, 0.89, 0.95, 1)
        _Noche ("Peso de noche", Range(0, 1)) = 1
        _Cobertura ("Cobertura de nubes", Range(0, 1)) = 0.5
        _Suavidad ("Suavidad del borde de las nubes", Range(0.02, 0.6)) = 0.22
        _Escala ("Escala de las nubes", Float) = 0.9
        _Velocidad ("Velocidad de las nubes (uv/s)", Vector) = (0.006, 0.003, 0, 0)
        _OpacidadNubesNoche ("Opacidad de las nubes de noche", Range(0, 1)) = 0.7
        _OpacidadNubesDia ("Opacidad de las nubes de dia", Range(0, 1)) = 0.45
        _DirLuna ("Direccion a la luna", Vector) = (0.35, 0.55, 0.45, 0)
        _TamanoLuna ("Tamano de la luna (radio, rad)", Range(0.005, 0.1)) = 0.03
        _FuerzaLuna ("Fuerza de la luna y su halo", Range(0, 4)) = 1.4
        _FuerzaEstrellas ("Fuerza de las estrellas", Range(0, 2)) = 0.7
        _DensidadEstrellas ("Densidad de las estrellas (celdas)", Range(40, 400)) = 180
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent-140" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Pass
        {
            Name "CieloRealista"
            Tags { "LightMode" = "UniversalForward" }
            // Premultiplicado: el color ya viene multiplicado por su opacidad, y las estrellas y la luna suman.
            Blend One OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _ColorCenit;
                half4 _ColorHorizonte;
                half _FuerzaGradiente;
                half4 _TinteNubes;
                half4 _ColorNubesNoche;
                half4 _ColorNubesDia;
                half _Noche;
                half _Cobertura;
                half _Suavidad;
                float _Escala;
                float4 _Velocidad;
                half _OpacidadNubesNoche;
                half _OpacidadNubesDia;
                float4 _DirLuna;
                half _TamanoLuna;
                half _FuerzaLuna;
                half _FuerzaEstrellas;
                half _DensidadEstrellas;
            CBUFFER_END

            struct Atributos
            {
                float4 posicion : POSITION;
            };

            struct Variables
            {
                float4 posicion : SV_POSITION;
                float3 direccion : TEXCOORD0;
            };

            Variables Vert(Atributos entrada)
            {
                Variables salida;
                float3 mundo = TransformObjectToWorld(entrada.posicion.xyz);
                salida.posicion = TransformWorldToHClip(mundo);
                // En el plano lejano, para que el domo nunca quede delante de un edificio lejano (igual que NubesNoche).
                salida.posicion.z = UNITY_RAW_FAR_CLIP_VALUE * salida.posicion.w;
                salida.direccion = mundo - GetCameraPositionWS();
                return salida;
            }

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float Hash31(float3 p)
            {
                p = frac(p * float3(443.897, 441.423, 437.195));
                p += dot(p, p.yzx + 19.19);
                return frac((p.x + p.y) * p.z);
            }

            float RuidoValor(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float a = Hash21(i);
                float b = Hash21(i + float2(1, 0));
                float c = Hash21(i + float2(0, 1));
                float d = Hash21(i + float2(1, 1));
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            float Fbm(float2 p)
            {
                float v = 0.0;
                float a = 0.5;
                for (int k = 0; k < 4; k++)
                {
                    v += a * RuidoValor(p);
                    p = p * 2.03 + 17.1;
                    a *= 0.5;
                }
                return v;
            }

            half4 Frag(Variables entrada) : SV_Target
            {
                float3 d = normalize(entrada.direccion);
                float arriba = saturate(d.y);
                float noche = _Noche;
                float visibleArriba = smoothstep(-0.03, 0.04, d.y);

                // Degradado: cenit azul profundo, horizonte morado.
                float curva = pow(1.0 - arriba, 3.0);
                half3 degradado = lerp(_ColorCenit.rgb, _ColorHorizonte.rgb, curva);
                float alfaDegradado = noche * _FuerzaGradiente * visibleArriba;
                half3 rgb = degradado * alfaDegradado;
                float alfa = alfaDegradado;

                // Estrellas tenues: celdas en la direccion, solo arriba (las nubes las cubren despues).
                // Punto pequeño en el centro de la celda (no la celda entera, que se veía como nieve).
                float3 pc = d * _DensidadEstrellas;
                float3 celda = floor(pc);
                float azar = Hash31(celda);
                float centro = length(frac(pc) - 0.5);
                float estrella = step(0.9975, azar) * (1.0 - smoothstep(0.06, 0.16, centro)) * smoothstep(0.12, 0.4, d.y);
                float titilar = 0.65 + 0.35 * sin(_Time.y * (1.5 + azar * 40.0) + azar * 100.0);
                rgb += half3(0.8, 0.88, 1.0) * estrella * titilar * _FuerzaEstrellas * noche;

                // Luna: disco y halo (aditivos).
                float3 luna = normalize(_DirLuna.xyz);
                float m = dot(d, luna);
                float coseno = cos(_TamanoLuna);
                float disco = smoothstep(coseno - 0.0006, coseno + 0.0004, m);
                float halo = pow(saturate(m), 260.0) * 0.55 + pow(saturate(m), 28.0) * 0.14 + pow(saturate(m), 6.0) * 0.03;
                half3 colorLuna = half3(0.82, 0.9, 1.0);
                rgb += colorLuna * (disco * 1.8 + halo) * _FuerzaLuna * noche * visibleArriba;

                // Nubes bajas procedurales proyectadas en un plano alto.
                float2 uv = d.xz / (arriba + 0.16) * _Escala;
                float2 mov = _Velocidad.xy * _Time.y;
                float densidad = Fbm(uv + mov);
                float umbral = 1.0 - _Cobertura;
                float nube = smoothstep(umbral - _Suavidad, umbral + _Suavidad, densidad);
                // Iluminacion desde abajo: los bordes y la parte baja de la capa (cerca del horizonte) reciben la luz de la ciudad.
                float borde = nube * (1.0 - nube) * 4.0;
                float luzCiudad = saturate(borde * 0.7 + (1.0 - arriba) * 0.65 - nube * 0.25 * arriba);
                half3 nocheNube = lerp(_ColorNubesNoche.rgb, _TinteNubes.rgb * 0.38, luzCiudad * luzCiudad);
                half3 diaNube = _ColorNubesDia.rgb * (0.82 + 0.18 * densidad);
                half3 colorNube = lerp(diaNube, nocheNube, noche);
                float opacidad = lerp(_OpacidadNubesDia, _OpacidadNubesNoche, noche);
                float horizonte = saturate(arriba / 0.07) * visibleArriba;
                float alfaNube = nube * opacidad * horizonte;
                rgb = rgb * (1.0 - alfaNube) + colorNube * alfaNube;
                alfa = alfa * (1.0 - alfaNube) + alfaNube;

                return half4(rgb, alfa);
            }
            ENDHLSL
        }
    }
}
