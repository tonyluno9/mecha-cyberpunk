// T-806: cielo de la ciudad hecho a mano (reemplaza a Skybox/Procedural, que con el tonemapping ACES y la exposición del posproceso de día
// se veía morado a mediodía y con una banda café en el horizonte). Tres colores que CicloDiaNoche cambia con la hora: cénit (_SkyTint),
// horizonte (_ColorHorizonte, el de la niebla, para que los edificios lejanos se fundan con el cielo) y suelo (_GroundColor).
// Una bruma suave sobre el horizonte, el brillo de la ciudad de noche y el sol (disco y halo) en la dirección de la luz principal.
Shader "MechaCyberpunk/CieloGradiente"
{
    Properties
    {
        _SkyTint ("Color del cénit", Color) = (0.3, 0.5, 0.72, 1)
        _ColorHorizonte ("Color del horizonte", Color) = (0.62, 0.72, 0.78, 1)
        _GroundColor ("Color bajo el horizonte", Color) = (0.4, 0.44, 0.48, 1)
        _ColorBrillo ("Brillo de la ciudad en el horizonte (rgb) y su fuerza (a)", Color) = (0, 0, 0, 0)
        _Curva ("Curva del degradado (menor: el cénit baja más)", Range(0.1, 2)) = 0.55
        _Bruma ("Alto de la bruma del horizonte (seno de la elevación)", Range(0.01, 0.5)) = 0.12
        _AltoBrillo ("Alto del brillo de la ciudad (seno de la elevación)", Range(0.01, 0.8)) = 0.25
        _Exposure ("Exposición del cénit, el brillo y el sol (el horizonte queda igual a la niebla)", Range(0, 4)) = 1
        _SunSize ("Tamaño del sol (0: sin sol)", Range(0, 0.2)) = 0.04
        _ColorSol ("Color del sol", Color) = (1, 0.93, 0.8, 1)
        _HaloSol ("Fuerza del halo del sol", Range(0, 2)) = 0.35
    }
    SubShader
    {
        Tags { "Queue" = "Background" "RenderType" = "Background" "PreviewType" = "Skybox" "RenderPipeline" = "UniversalPipeline" }
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _SkyTint;
                half4 _ColorHorizonte;
                half4 _GroundColor;
                half4 _ColorBrillo;
                half _Curva;
                half _Bruma;
                half _AltoBrillo;
                half _Exposure;
                half _SunSize;
                half4 _ColorSol;
                half _HaloSol;
            CBUFFER_END

            struct Entrada
            {
                float4 posicion : POSITION;
            };

            struct Salida
            {
                float4 posicion : SV_POSITION;
                float3 direccion : TEXCOORD0;
            };

            Salida vert(Entrada i)
            {
                Salida o;
                o.posicion = TransformObjectToHClip(i.posicion.xyz);
                o.direccion = i.posicion.xyz;
                return o;
            }

            half4 frag(Salida i) : SV_Target
            {
                float3 d = normalize(i.direccion);
                float y = d.y;
                float arriba = saturate(y);
                // Degradado del horizonte al cénit, con una bruma que se pega al horizonte.
                half3 cielo = lerp(_ColorHorizonte.rgb, _SkyTint.rgb * _Exposure, pow(arriba, _Curva));
                float bruma = exp(-arriba / _Bruma);
                cielo = lerp(cielo, _ColorHorizonte.rgb, bruma * 0.6);
                // Brillo de la ciudad (de noche): sube desde el horizonte y se apaga hacia arriba.
                // Empieza un poco arriba del horizonte, para que justo en el horizonte el cielo sea el color de la niebla.
                float brillo = (1.0 - saturate(arriba / _AltoBrillo)) * smoothstep(0.0, 0.05, arriba);
                cielo += _ColorBrillo.rgb * (_ColorBrillo.a * _Exposure * brillo * brillo);
                // Bajo el horizonte: del color del horizonte al del suelo en unos grados (casi nunca se ve: lo tapan los edificios).
                half3 suelo = lerp(_ColorHorizonte.rgb, _GroundColor.rgb, saturate(-y * 6.0));
                half3 color = y >= 0.0 ? cielo : suelo;

                // Sol: disco con borde suave y un halo que tiñe el cielo a su alrededor.
                if (_SunSize > 0.0001)
                {
                    float3 luz = normalize(_MainLightPosition.xyz);
                    float coseno = dot(d, luz);
                    float radio = _SunSize * 0.6;
                    float disco = smoothstep(cos(radio * 1.25), cos(radio), coseno);
                    float halo = pow(saturate(coseno), 24.0) * _HaloSol + pow(saturate(coseno), 4.0) * _HaloSol * 0.25;
                    color = lerp(color, _ColorSol.rgb * 1.6 * _Exposure, saturate(disco) * step(0.0, y));
                    color += _ColorSol.rgb * (halo * _Exposure) * step(0.0, y + 0.05);
                }
                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
