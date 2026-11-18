// T-1351: mar de noche del Malecón y del Puerto. Transparente de un solo paso (para leer la profundidad de la escena y hacer espuma en la
// orilla), con color profundo que casi siempre es opaco. Olas suaves en el vértice (dos senos), normales de ruido que se desplaza,
// fresnel, reflejo de la sonda (cielo y ciudad), reflejos verticales alargados de la luz principal y de las luces adicionales
// (sin posiciones: solo el ángulo horizontal hacia la luz, así sirve igual con Forward+), espuma por profundidad (si el URP genera
// _CameraDepthTexture; los assets PC y Medio sí) o por color de vértice (R) y niebla de URP.
// _SoloEspuma = 1 convierte el material en la franja de espuma de la orilla (CrearOrilla): solo dibuja espuma sobre el agua.
Shader "MechaCyberpunk/Mar"
{
    Properties
    {
        _ColorProfundo ("Agua profunda", Color) = (0.008, 0.035, 0.05, 1)
        _ColorSuperficie ("Agua clara (rasante)", Color) = (0.02, 0.1, 0.12, 1)
        _ColorCielo ("Cielo de respaldo del reflejo", Color) = (0.03, 0.05, 0.1, 1)
        _AlturaOla ("Altura de ola (m)", Range(0, 1)) = 0.12
        _LargoOla ("Largo de ola (m)", Range(2, 60)) = 14
        _VelocidadOla ("Velocidad de ola", Range(0, 3)) = 0.6
        _EscalaRuido ("Escala del ruido de normales (1/m)", Range(0.02, 2)) = 0.35
        _VelocidadRuido ("Velocidad del ruido", Range(0, 2)) = 0.25
        _FuerzaNormal ("Fuerza de normales", Range(0, 2)) = 0.45
        _FresnelMin ("Reflejo mirando hacia abajo", Range(0, 1)) = 0.04
        _FresnelPotencia ("Potencia del fresnel", Range(1, 8)) = 4
        _Reflejo ("Intensidad del reflejo de la sonda", Range(0, 3)) = 0.6
        _Rugosidad ("Rugosidad del reflejo", Range(0, 1)) = 0.25
        _AnchoEstela ("Ancho de la estela de luz (rad)", Range(0.005, 0.3)) = 0.05
        _GananciaLuces ("Ganancia de la estela de luces", Range(0, 20)) = 4
        _GananciaLuna ("Ganancia de la luz principal", Range(0, 10)) = 0.35
        _AnchoOrilla ("Ancho de la espuma por profundidad (m)", Range(0.05, 5)) = 0.8
        _ColorEspuma ("Espuma", Color) = (0.85, 0.92, 0.95, 1)
        _FuerzaEspuma ("Fuerza de la espuma", Range(0, 1)) = 0
        _TransparenciaOrilla ("Transparencia en lo somero (m)", Range(0.05, 6)) = 1.2
        _SoloEspuma ("Solo espuma (franja de orilla)", Range(0, 1)) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent-100" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }

        Pass
        {
            Name "Mar"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _FORWARD_PLUS
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _ColorProfundo;
                half4 _ColorSuperficie;
                half4 _ColorCielo;
                half _AlturaOla;
                half _LargoOla;
                half _VelocidadOla;
                half _EscalaRuido;
                half _VelocidadRuido;
                half _FuerzaNormal;
                half _FresnelMin;
                half _FresnelPotencia;
                half _Reflejo;
                half _Rugosidad;
                half _AnchoEstela;
                half _GananciaLuces;
                half _GananciaLuna;
                half _AnchoOrilla;
                half4 _ColorEspuma;
                half _FuerzaEspuma;
                half _TransparenciaOrilla;
                half _SoloEspuma;
            CBUFFER_END

            struct Entrada
            {
                float4 posicion : POSITION;
                half4 color : COLOR;
            };

            struct Salida
            {
                float4 posicion : SV_POSITION;
                float3 mundo : TEXCOORD0;
                half2 espumaVertice : TEXCOORD1;
                half niebla : TEXCOORD2;
            };

            // Altura de las olas: dos senos cruzados, baratos y suaves. Se usa en el vértice y para las normales.
            float AlturaOlas(float2 p, float t)
            {
                float k = 6.2831853 / max(_LargoOla, 0.01);
                float a = sin(dot(p, float2(0.8, 0.6)) * k + t * _VelocidadOla * 2.0);
                float b = sin(dot(p, float2(-0.5, 0.86)) * k * 1.7 + t * _VelocidadOla * 2.7 + 1.3);
                return (a * 0.65 + b * 0.35) * _AlturaOla;
            }

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float RuidoValor(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(Hash21(i), Hash21(i + float2(1, 0)), f.x),
                            lerp(Hash21(i + float2(0, 1)), Hash21(i + float2(1, 1)), f.x), f.y);
            }

            // Altura de ruido a dos escalas que se desplazan en direcciones distintas.
            float AlturaRuido(float2 p, float t)
            {
                float2 q = p * _EscalaRuido;
                float v = t * _VelocidadRuido;
                return RuidoValor(q + float2(v, v * 0.6)) * 0.65 + RuidoValor(q * 2.3 - float2(v * 0.8, -v)) * 0.35;
            }

            // Altura total que ve la normal: ruido más las olas grandes normalizadas.
            float AlturaTotal(float2 p, float t)
            {
                return AlturaRuido(p, t) + AlturaOlas(p, t) / max(_AlturaOla, 0.01) * 0.5;
            }

            Salida vert(Entrada entrada)
            {
                Salida salida;
                float3 mundo = TransformObjectToWorld(entrada.posicion.xyz);
                // Las olas se apagan en la franja de orilla (R del vértice) para que la espuma no flote.
                mundo.y += AlturaOlas(mundo.xz, _Time.y) * (1.0 - entrada.color.r * 0.8);
                salida.mundo = mundo;
                salida.posicion = TransformWorldToHClip(mundo);
                salida.espumaVertice = half2(entrada.color.r, entrada.color.g);
                salida.niebla = ComputeFogFactor(salida.posicion.z);
                return salida;
            }

            // Una estela vertical de luz: fuerte cuando la dirección horizontal hacia la luz coincide con la de la vista
            // (el reflejo cae entre la cámara y la luz), con ancho angular _AnchoEstela y torcida por las normales.
            half Estela(float2 haciaLuz, float2 rumbo, float torcido)
            {
                float2 dl = haciaLuz / max(length(haciaLuz), 1e-4);
                float alineado = dot(dl, rumbo);
                float lateral = rumbo.x * dl.y - rumbo.y * dl.x + torcido;
                float ancho = max(_AnchoEstela, 1e-3);
                return (half)(exp(-(lateral * lateral) / (ancho * ancho)) * saturate(alineado * 4.0));
            }

            half4 frag(Salida salida) : SV_Target
            {
                float t = _Time.y;
                float3 mundo = salida.mundo;
                float3 vista = GetWorldSpaceNormalizeViewDir(mundo);
                float2 rumbo = -normalize(vista.xz + float2(1e-5, 0)); // de la cámara hacia el punto, en horizontal

                // Normal: diferencias finitas de la altura total.
                float e = 0.35;
                float h0 = AlturaTotal(mundo.xz, t);
                float hx = AlturaTotal(mundo.xz + float2(e, 0), t);
                float hz = AlturaTotal(mundo.xz + float2(0, e), t);
                float3 normal = normalize(float3(-(hx - h0) / e * _FuerzaNormal, 1.0, -(hz - h0) / e * _FuerzaNormal));

                half NdotV = saturate(dot(normal, vista));
                half fresnel = lerp(_FresnelMin, 1.0, pow(1.0 - NdotV, _FresnelPotencia));

                // Reflejo de la sonda (cielo y ciudad) con respaldo del color de cielo.
                float3 refl = reflect(-vista, normal);
                refl.y = abs(refl.y);
                half mip = _Rugosidad * 6.0;
                half4 sonda = SAMPLE_TEXTURECUBE_LOD(unity_SpecCube0, samplerunity_SpecCube0, refl, mip);
                half3 reflejoSonda = DecodeHDREnvironment(sonda, unity_SpecCube0_HDR) * _Reflejo + _ColorCielo.rgb;

                half3 cuerpo = lerp(_ColorProfundo.rgb, _ColorSuperficie.rgb, saturate(1.0 - NdotV));
                half3 color = lerp(cuerpo, reflejoSonda, fresnel);

                // Estelas de luz alargadas. El fresnel las cuida: casi no se ven mirando hacia abajo.
                float torcido = (h0 - 0.5) * 0.25 * _FuerzaNormal;
                half3 estelas = 0;
                Light principal = GetMainLight();
                estelas += principal.color * Estela(principal.direction.xz, rumbo, torcido) * _GananciaLuna
                           * saturate(principal.direction.y * 4.0 + 0.2);

                #if defined(_ADDITIONAL_LIGHTS) || defined(_FORWARD_PLUS)
                    InputData inputData = (InputData)0;
                    inputData.positionWS = mundo;
                    inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(salida.posicion);
                    uint cuentaLuces = GetAdditionalLightsCount();
                    #if USE_CLUSTER_LIGHT_LOOP
                        [loop] for (uint luzDir = 0u; luzDir < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); luzDir++)
                        {
                            Light luzD = GetAdditionalLight(luzDir, mundo, half4(1, 1, 1, 1));
                            estelas += luzD.color * Estela(luzD.direction.xz, rumbo, torcido) * _GananciaLuna;
                        }
                    #endif
                    LIGHT_LOOP_BEGIN(cuentaLuces)
                        Light luz = GetAdditionalLight(lightIndex, mundo, half4(1, 1, 1, 1));
                        estelas += luz.color * (luz.distanceAttenuation * Estela(luz.direction.xz, rumbo, torcido) * _GananciaLuces);
                    LIGHT_LOOP_END
                #endif
                color += estelas * lerp(0.25, 1.0, fresnel);

                // Espuma: por profundidad de la escena y por color de vértice.
                float2 uvPantalla = GetNormalizedScreenSpaceUV(salida.posicion);
                float escena = LinearEyeDepth(SampleSceneDepth(uvPantalla), _ZBufferParams);
                float diferencia = max(escena - salida.posicion.w, 0.0);
                float ruidoEspuma = RuidoValor(mundo.xz * 1.7 + t * 0.4);
                half franja = saturate(1.0 - diferencia / max(_AnchoOrilla, 0.01));
                half espumaProfundidad = franja * smoothstep(0.25, 0.75, franja * 0.6 + ruidoEspuma * 0.6);
                half espumaVertice = salida.espumaVertice.x * smoothstep(0.2, 0.7, 0.5 + ruidoEspuma * 0.8 - salida.espumaVertice.y * 0.5);
                half espuma = saturate(max(espumaProfundidad, espumaVertice)) * _FuerzaEspuma;

                half alfaAgua = saturate(diferencia / max(_TransparenciaOrilla, 0.01) + 0.35);
                alfaAgua = max(alfaAgua, step(escena, 0.0001)); // sin profundidad válida: opaco
                color = lerp(color, _ColorEspuma.rgb, espuma);
                half alfa = lerp(max(alfaAgua, espuma), espuma, _SoloEspuma);
                color = MixFog(color, salida.niebla);
                return half4(color, alfa);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
