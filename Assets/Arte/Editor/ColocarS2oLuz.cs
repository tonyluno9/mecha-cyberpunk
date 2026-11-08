using System.Collections.Generic;
using System.Linq;
using MechaCyberpunk.Mundo.Ambiente;
using MechaCyberpunk.Mundo.Datos;
using MechaCyberpunk.Mundo.Reloj;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-880 (playtest 7-nov, sesión 2): la luz de noche como K01 en toda la ciudad. La calle de prueba (s2c-calle-prueba-16) se veía de
    /// atardecer: exposición 2.5, luna a 1.1, ambiente gris claro, postes con alcance de 20 a 26 m, niebla delgada y nubes moradas. Aquí:
    /// exposición baja, ambiente y luna azul noche, el naranja solo bajo cada poste (alcance corto), niebla azul más espesa, cielo azul marino
    /// liso (sin nubes de noche), las luces de relleno de los edificios ("Luna_*") y las verdes del barrio parejas, y en la calle de prueba un
    /// edificio del kit que la cierra al fondo, más neones y charcos. Idempotente: los números se fijan (no se multiplican) y lo de la calle
    /// va en su raíz <see cref="Raiz"/>, que se rehace.
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ColocarS2oLuz.Aplicar
    /// </summary>
    public static class ColocarS2oLuz
    {
        public const string Raiz = "S2o_CalleK01";
        public const int Semilla = 8800;
        public const string RutaAmbienteNoche = "Assets/_Proyecto/Mundo/Datos/ConfigAmbienteNoche_Mundo.asset";

        // ---- Los números de la noche (se dejan públicos para afinarlos con el editor vivo).
        public static float Exposicion = 1.7f;
        /// <summary>T-889: el volumen de Neón Alto suma esto a la exposición de la noche. Entre torres altas la calle quedaba con la mitad
        /// del brillo que los demás barrios (16 contra 35-49 de promedio en las fotos de s2o-luz-zona); con 1.4 queda en 40.</summary>
        public static float ExposicionNeonAlto = 1.4f;
        public static float IntensidadLuna = 0.32f;
        public static Color ColorLuna = new Color(0.5f, 0.62f, 1f);
        public static Color Ambiente = new Color(0.4f, 0.41f, 0.47f);
        public static Color ColorNiebla = new Color(0.2f, 0.24f, 0.32f);
        public static Color Cielo = new Color(0.035f, 0.055f, 0.13f);
        public static float DensidadNiebla = 0.036f;
        public static float ExposicionCielo = 1f;
        public static float BrilloCiudad = 0.12f;

        /// <summary>Postes de la calle (C07 y N09): alcance corto para que el naranja quede en un círculo bajo cada uno.</summary>
        public static float RangoPoste = 9f;
        public static float IntensidadPoste = 40f;
        public static float AnguloPoste = 75f;
        /// <summary>Luces de relleno de las fachadas (Luna_*, una por edificio greybox) y las verdes del barrio: las que hacían zonas quemadas.</summary>
        public static float IntensidadRelleno = 3f;
        public static float IntensidadVerde = 14f;
        /// <summary>Foco de pared N01 del kit (KitS2c.FocoPared): un brillo chico junto a la puerta, no la fachada entera.</summary>
        public static float IntensidadFoco = 1.6f;
        public static float RangoFoco = 4.5f;

        [MenuItem("Mecha Cyberpunk/S2o/1 Luz de noche como K01")]
        public static void Aplicar()
        {
            ColocarS2.Informe.Clear();
            Posproceso();
            Ciclo();
            ColocarS2.Abrir();
            Luces();
            SinSondasViejas();
            Calle();
            ColocarS2.Guardar("s2o-luz");
        }

        /// <summary>Solo los números (posproceso, ciclo y luces), sin rehacer la calle: para afinar con el editor vivo.</summary>
        public static void Numeros()
        {
            Posproceso();
            Ciclo();
            Luces();
            SinSondasViejas();
        }

        // ---------------------------------------------------------------- Posproceso

        private static void Posproceso()
        {
            VolumeProfile noche = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ColocarS2cLook.RutaPerfilNoche);
            if (noche.TryGet(out ColorAdjustments color))
            {
                color.postExposure.Override(Exposicion);
                color.contrast.Override(14f);
                color.saturation.Override(2f);
            }
            if (noche.TryGet(out Bloom bloom))
            {
                bloom.threshold.Override(1.1f);
                bloom.intensity.Override(0.9f);
            }
            if (noche.TryGet(out WhiteBalance balance))
            {
                // Antes +9 (cálido): la noche de K01 es fría; lo naranja sale de las lámparas, no del balance.
                balance.temperature.Override(-2f);
                balance.tint.Override(0f);
            }
            if (noche.TryGet(out SplitToning partido))
            {
                partido.shadows.Override(new Color(0.47f, 0.5f, 0.56f));
                partido.highlights.Override(new Color(0.76f, 0.69f, 0.62f));
                partido.balance.Override(-20f);
            }
            if (noche.TryGet(out Vignette vineta))
            {
                vineta.intensity.Override(0.32f);
            }
            EditorUtility.SetDirty(noche);
            VolumeProfile neon = AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/Arte/Exteriores/PerfilNeonAlto.asset");
            if (neon != null && neon.TryGet(out ColorAdjustments ca))
            {
                // El volumen de Neón Alto suma a la exposición global; con la global baja queda casi neutro.
                ca.postExposure.Override(ExposicionNeonAlto);
                EditorUtility.SetDirty(neon);
            }
            AssetDatabase.SaveAssets();
            ColocarS2.Informe.AppendLine("Posproceso de noche: exposición " + Exposicion + ", balance frío, bloom 0.9");
        }

        // ---------------------------------------------------------------- Ciclo (luna, ambiente, niebla, cielo)

        private static void Ciclo()
        {
            ConfigCicloDia config = AssetDatabase.LoadAssetAtPath<ConfigCicloDia>(ColocarS2cLook.RutaCicloDia);
            SerializedObject so = new SerializedObject(config);
            CurvaDeNoche(so.FindProperty("densidadNiebla"), DensidadNiebla);
            CurvaDeNoche(so.FindProperty("intensidadLuz"), IntensidadLuna);
            CurvaDeNoche(so.FindProperty("exposicionCielo"), ExposicionCielo);
            DeNoche(so.FindProperty("colorLuz"), ColorLuna);
            DeNoche(so.FindProperty("colorAmbiente"), Ambiente);
            DeNoche(so.FindProperty("colorNiebla"), ColorNiebla);
            DeNoche(so.FindProperty("colorCielo"), Cielo);
            so.FindProperty("colorBrilloCiudad").colorValue = ColorNiebla;
            so.FindProperty("fuerzaBrilloCiudad").floatValue = BrilloCiudad;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);

            // Cielo azul marino liso: sin el domo de nubes de noche.
            ConfigAmbienteNoche ambiente = AssetDatabase.LoadAssetAtPath<ConfigAmbienteNoche>(RutaAmbienteNoche);
            if (ambiente != null)
            {
                SerializedObject sa = new SerializedObject(ambiente);
                sa.FindProperty("opacidadNubes").floatValue = 0f;
                sa.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(ambiente);
            }
            FondoDeNoche();
            AssetDatabase.SaveAssets();
            ColocarS2.Informe.AppendLine("Noche: luna " + IntensidadLuna + ", ambiente " + Ambiente + ", niebla " + DensidadNiebla + " " + ColorNiebla + ", cielo " + Cielo + " sin nubes");
        }

        /// <summary>Bruma de noche de los telones pintados (T513_Fondo): tenían nubes y un atardecer pintados que de noche se veían detrás de la
        /// calle. De noche quedan casi del color de la niebla: siluetas apenas, cielo liso.</summary>
        public static float BrumaTelon = 0.92f;
        public static float BrilloTelon = 0.3f;
        public static float TonoTelon = 0.4f;

        private static void FondoDeNoche()
        {
            HashSet<Material> hechos = new HashSet<Material>();
            foreach (string ruta in AssetDatabase.FindAssets("t:Material", new[] { "Assets/Arte/Fondos" }).Select(AssetDatabase.GUIDToAssetPath))
            {
                Material m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
                if (m == null || m.shader == null || m.shader.name != "MechaCyberpunk/FondoCiudad" || !hechos.Add(m))
                {
                    continue;
                }
                // El telón no lleva niebla ni pasa por el ambiente: su color va directo a la exposición de la noche, por eso va más oscuro que el cielo.
                m.SetColor("_ColorBrumaNoche", Cielo * TonoTelon);
                m.SetFloat("_BrumaNoche", BrumaTelon);
                m.SetFloat("_BrilloNoche", BrilloTelon);
                EditorUtility.SetDirty(m);
            }
            ColocarS2.Informe.AppendLine("Telones de fondo de noche con bruma " + BrumaTelon + ": " + hechos.Count + " materiales");
        }

        /// <summary>La noche entera (21:00 a 4:30) con el mismo valor, con llaves planas.</summary>
        private static void CurvaDeNoche(SerializedProperty p, float valor)
        {
            AnimationCurve curva = p.animationCurveValue;
            foreach (float hora in new[] { 0f, 4.5f, 21f, 24f })
            {
                if (!curva.keys.Any(k => Mathf.Abs(k.time - hora) < 0.01f))
                {
                    curva.AddKey(hora, valor);
                }
            }
            Keyframe[] llaves = curva.keys;
            for (int i = 0; i < llaves.Length; i++)
            {
                if (llaves[i].time <= 4.5f || llaves[i].time >= 21f)
                {
                    llaves[i].value = valor;
                    llaves[i].inTangent = 0f;
                    llaves[i].outTangent = 0f;
                }
            }
            curva.keys = llaves;
            p.animationCurveValue = curva;
        }

        private static void DeNoche(SerializedProperty color, Color nuevo)
        {
            Gradient g = color.gradientValue;
            GradientColorKey[] claves = g.colorKeys;
            for (int i = 0; i < claves.Length; i++)
            {
                if (claves[i].time <= 0.19f || claves[i].time >= 0.875f)
                {
                    claves[i].color = nuevo;
                }
            }
            g.SetKeys(claves, g.alphaKeys);
            color.gradientValue = g;
        }

        // ---------------------------------------------------------------- Luces de la ciudad

        /// <summary>Un poste de la calle: farola C07 (de la ciudad o del kit) o N09.</summary>
        public static bool EsPoste(Light l)
        {
            if (l.type != LightType.Spot || l.transform.parent == null)
            {
                return false;
            }
            string p = l.transform.parent.name;
            return (l.name == "Luz" || l.name.StartsWith(PostesS2t.NombreLuzNueva))
                   && (p.StartsWith("Farola") || p.StartsWith("Poste") || p.StartsWith("C07") || p.StartsWith("N09"));
        }

        public static bool EsRelleno(Light l) => l.type == LightType.Point && l.name.StartsWith("Luna_");
        public static bool EsVerde(Light l) => l.type == LightType.Point && l.name.StartsWith("Luz_Verde_");
        public static bool EsFoco(Light l) => l.type == LightType.Point && l.transform.parent != null && l.transform.parent.name == "FocoPared";

        private static void Luces()
        {
            int postes = 0, relleno = 0, verdes = 0, focos = 0;
            foreach (Light l in Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (EsPoste(l))
                {
                    l.range = RangoPoste;
                    l.intensity = IntensidadPoste;
                    l.spotAngle = AnguloPoste;
                    l.innerSpotAngle = AnguloPoste * 0.4f;
                    postes++;
                }
                else if (EsRelleno(l))
                {
                    l.intensity = IntensidadRelleno;
                    relleno++;
                }
                else if (EsVerde(l))
                {
                    l.intensity = IntensidadVerde;
                    verdes++;
                }
                else if (EsFoco(l))
                {
                    l.intensity = IntensidadFoco;
                    l.range = RangoFoco;
                    focos++;
                }
                else
                {
                    continue;
                }
                EditorUtility.SetDirty(l);
            }
            ColocarS2.Informe.AppendLine("Postes: " + postes + " (alcance " + RangoPoste + " m, " + IntensidadPoste + "); relleno de fachadas: " + relleno + " a " + IntensidadRelleno
                                         + "; verdes: " + verdes + " a " + IntensidadVerde
                                         + "; focos de pared: " + focos + " a " + IntensidadFoco);
        }

        /// <summary>
        /// La causa de la luz dispareja: el volumen de sondas (APV) de AMB_Noche se horneó con la ciudad de antes (edificios greybox). Las
        /// casas y calles nuevas caen dentro de sondas que quedaron dentro de aquellos edificios (negras) o junto a sus luces (quemadas), y no
        /// reciben el ambiente de la noche. Se apaga: toda la ciudad usa el mismo ambiente del ciclo. Para volver a usarlo hay que hornearlo de
        /// nuevo con la ciudad de hoy (Mecha Cyberpunk/Ambiente/Hornear APV) y prender estos dos objetos.
        /// </summary>
        private static void SinSondasViejas()
        {
            foreach (ProbeVolumePerSceneData datos in Object.FindObjectsByType<ProbeVolumePerSceneData>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                datos.enabled = false;
                EditorUtility.SetDirty(datos);
            }
            GameObject apv = ColocarS2.Buscar(RutaApv)?.gameObject;
            if (apv != null)
            {
                apv.SetActive(false);
            }
            ColocarS2.Informe.AppendLine("APV viejo apagado (" + RutaApv + " y ProbeVolumePerSceneData)");
            Material asfalto = AssetDatabase.LoadAssetAtPath<Material>(KitS2c.Carpeta + "/S2c_Asfalto.mat");
            if (asfalto != null)
            {
                // El mapa de metal del asfalto de AMB (rojo alto) lo hacía metal: sin luz difusa, negro de noche.
                asfalto.SetTexture("_MetallicGlossMap", null);
                asfalto.DisableKeyword("_METALLICSPECGLOSSMAP");
                asfalto.SetFloat("_Metallic", 0f);
                asfalto.SetFloat("_Smoothness", SuavidadAsfalto);
                asfalto.SetColor("_BaseColor", ColorAsfalto);
                EditorUtility.SetDirty(asfalto);
            }
            Material cielo = CicloDeLaEscena() is CicloDiaNoche ciclo ? new SerializedObject(ciclo).FindProperty("cielo")?.objectReferenceValue as Material : null;
            if (cielo != null)
            {
                // El degradado del cielo: el azul marino baja casi hasta las azoteas (antes la mitad del cielo era del color de la niebla).
                cielo.SetFloat("_Curva", CurvaCielo);
                cielo.SetFloat("_Bruma", BrumaCielo);
                EditorUtility.SetDirty(cielo);
            }
        }

        public const string RutaApv = "AMB_Noche/APV";

        /// <summary>El asfalto del kit: menos liso (se veía negro, espejo del cielo); el brillo mojado lo ponen los charcos.</summary>
        public static float SuavidadAsfalto = 0.45f;
        // La textura T535 promedia 44/255 (casi negra en lineal): se aclara para que de noche se vea gris y mojado como en K01.
        public static Color ColorAsfalto = new Color(2.4f, 2.3f, 2.4f);
        public static float CurvaCielo = 0.3f;
        public static float BrumaCielo = 0.07f;

        private static CicloDiaNoche CicloDeLaEscena() => Object.FindFirstObjectByType<CicloDiaNoche>();

        // ---------------------------------------------------------------- Calle de prueba

        private static void Calle()
        {
            Object.DestroyImmediate(GameObject.Find(Raiz));
            KitS2c.Obra o = KitS2c.Empezar(Raiz, Semilla);
            float suelo = KitS2c.Suelo(new Vector3(-95f, 0f, ColocarS2cCalle.ZCentro), 0.2f);
            float arroyo = suelo + 0.02f;
            float banqueta = arroyo + ColocarS2cCalle.AltoBanqueta;
            Transform casas = new GameObject("Fondo").transform;
            casas.SetParent(o.Raiz, false);
            Transform cosas = new GameObject("Cosas").transform;
            cosas.SetParent(o.Raiz, false);

            // El fondo: la calle llegaba al muro del borde de la ciudad (x = -134) y de ahí al vacío. Casas del kit de frente, pegadas
            // al muro, a lo ancho de la calle y un poco más, con dos pisos más arriba para que tape el horizonte.
            float z0 = ColocarS2cCalle.ZSur - 4f, z1 = ColocarS2cCalle.ZNorte + 4f;
            float x = ColocarS2cCalle.XBordePoniente - 0.2f;
            KitS2c.Estilo Fondo(int i) => new KitS2c.Estilo
            {
                PlantaBaja = i % 2 == 0 ? "C01_FachadaLocal_cy" : "C02_FachadaVivienda_cy",
                ColorPiso = (i + 1) % KitS2c.ColoresPiso.Length,
                Neon = i == 1 ? KitS2c.Magenta : i == 2 ? KitS2c.Turquesa : (Color?)null,
                NeonBandera = false,
                Tinaco = true,
                LucesVentanas = 0.6f,
            };
            List<Vector3> altos = KitS2c.Frente(o, casas, new Vector3(x, suelo, z1), new Vector3(x, suelo, z0), Vector3.right, 6f, Fondo);
            // Segundo cuerpo, atrás y más alto: el edificio que cierra la vista (K01 tiene la calle que se pierde entre casas).
            Transform atras = new GameObject("Atras").transform;
            atras.SetParent(casas, false);
            float alto = altos.Count > 0 ? altos.Max(a => a.y) + 0.4f - suelo : 6.2f;
            KitS2c.Frente(o, atras, new Vector3(x - 6.5f, suelo + alto, z1 - 2f), new Vector3(x - 6.5f, suelo + alto, z0 + 2f), Vector3.right, 6f,
                i => new KitS2c.Estilo { PlantaBaja = "C03_FachadaPisoAlto_cy", ColorPiso = i % KitS2c.ColoresPiso.Length, Tinaco = i % 2 == 0, LucesVentanas = 0.5f, Neon = i == 0 ? KitS2c.Turquesa : (Color?)null });

            // Más neones en las fachadas (K01 tiene uno o dos por casa) y más charcos, sobre todo a media calle.
            Neones(o, cosas, banqueta);
            for (int i = 0; i < 10; i++)
            {
                KitS2c.Charco(o, cosas, new Vector3(o.Rango(-122f, -82f), arroyo, ColocarS2cCalle.ZCentro + o.Rango(-3.2f, 3.2f)), o.Rango(1.4f, 3.2f), o.Rango(0.8f, 1.8f), o.Rango(60f, 120f));
            }
            KitS2c.Charco(o, cosas, new Vector3(-73f, arroyo, ColocarS2cCalle.ZCentro - 0.6f), 2.4f, 1.3f, 88f);
            KitS2c.Terminar(o);
            EnderezarCarro();
            ColocarS2.Informe.AppendLine("Calle de prueba: fondo de " + altos.Count + " casas + cuerpo alto, " + o.Neones + " neones y " + o.Charcos + " charcos más");
        }

        private static void Neones(KitS2c.Obra o, Transform padre, float banqueta)
        {
            (float xa, float xb)[] tramos = { (-60f, -104f), (-117f, -129f) };
            foreach ((float xa, float xb) in tramos)
            {
                for (float xn = xa - o.Rango(1f, 3f); xn > xb; xn -= o.Rango(6f, 9f))
                {
                    bool sur = o.Chance(0.5f);
                    float z = sur ? ColocarS2cCalle.ZSur : ColocarS2cCalle.ZNorte;
                    Vector3 normal = sur ? Vector3.forward : Vector3.back;
                    Vector3 p = new Vector3(xn, banqueta + o.Rango(3.3f, 4.4f), z);
                    if (Physics.Raycast(p + normal * 1.5f, -normal, out RaycastHit h, 3f))
                    {
                        p = h.point;
                    }
                    KitS2c.Neon(o, padre, p, normal, Vector3.left, o.Chance(0.55f) ? KitS2c.Magenta : KitS2c.Turquesa, o.Chance(0.6f));
                }
            }
        }

        /// <summary>El V01a de la calle de prueba quedaba atravesado: se gira para que su largo vaya a lo largo de la calle (x).</summary>
        private static void EnderezarCarro()
        {
            Transform c = ColocarS2.Buscar(ColocarS2cCalle.Raiz + "/Cosas/CarroEstacionado");
            if (c == null)
            {
                return;
            }
            Bounds b = Limites(c);
            if (b.size.z > b.size.x)
            {
                Vector3 centro = b.center;
                c.RotateAround(centro, Vector3.up, 90f);
                EditorUtility.SetDirty(c);
                ColocarS2.Informe.AppendLine("Carro estacionado enderezado a lo largo de la calle");
            }
        }

        public static Bounds Limites(Transform t)
        {
            Renderer[] rs = t.GetComponentsInChildren<Renderer>();
            Bounds b = rs[0].bounds;
            foreach (Renderer r in rs)
            {
                b.Encapsulate(r.bounds);
            }
            return b;
        }

        // ---------------------------------------------------------------- Fotos

        /// <summary>Una toma de calle de cada barrio con la luz nueva (docs/capturas/s2o-luz-zona-*.png), para ver que quede pareja.</summary>
        public static void FotosZonas(string sufijo)
        {
            List<(string nombre, Vector3 desde, Vector3 hacia)> tomas = new List<(string, Vector3, Vector3)>();
            tomas.AddRange(ColocarS2cSuburbios.Tomas.Where(t => t.nombre != "alto").Take(2));
            tomas.AddRange(ColocarS2cMercado.Tomas.Where(t => t.nombre != "alto").Take(1));
            tomas.AddRange(ColocarS2cNeonAlto.Tomas.Take(1));
            tomas.Add(("nova", new Vector3(58f, 2.6f, ColocarS2cNova.ZCentro - 0.5f), new Vector3(130f, 5f, ColocarS2cNova.ZCentro)));
            tomas.Add(("casa", new Vector3(-50f, 2.4f, -74f), new Vector3(-57f, 2f, -60f)));
            tomas.Add(("alto", new Vector3(-20f, 60f, -120f), new Vector3(-20f, 0f, -20f)));
            foreach ((string nombre, Vector3 desde, Vector3 hacia) in tomas)
            {
                ColocarS2cLook.Foto(desde, hacia, "docs/capturas/s2o-luz-zona-" + nombre + "-" + sufijo + ".png", nombre == "alto" ? 60f : 66f);
            }
        }

        /// <summary>La toma de K01 y su comparación lado a lado (docs/capturas/s2o-luz-k01-&lt;n&gt;.png).</summary>
        public static void Foto(int n)
        {
            float suelo = KitS2c.Suelo(new Vector3(-95f, 0f, ColocarS2cCalle.ZCentro), 0.2f);
            string juego = "docs/capturas/s2o-luz-juego-" + n + ".png";
            // En el juego el domo de nubes se apaga solo de noche (opacidad 0); en el editor no corre su LateUpdate.
            // (con opacidad 0 de noche, CieloNubes.Colocar apaga su MeshRenderer): para la foto se apaga igual y se vuelve a prender.
            List<MeshRenderer> domos = Object.FindObjectsByType<CieloNubes>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .Select(d => d.GetComponent<MeshRenderer>()).Where(r => r != null && r.enabled).ToList();
            domos.ForEach(r => r.enabled = false);
            // CicloDiaNoche copia el material del cielo una sola vez (cieloEnEjecucion): en el editor la copia no ve los cambios del original.
            if (RenderSettings.skybox != null)
            {
                RenderSettings.skybox.SetFloat("_Curva", CurvaCielo);
                RenderSettings.skybox.SetFloat("_Bruma", BrumaCielo);
            }
            ColocarS2cLook.Foto(ColocarS2cCalle.CamaraFoto + Vector3.up * suelo, ColocarS2cCalle.MiraFoto + Vector3.up * suelo, juego, ColocarS2cCalle.CampoFoto);
            ColocarS2.LadoALado("imageassets/referencias/look/K01_Barrio_HUDnuevo.png", juego, "docs/capturas/s2o-luz-k01-" + n + ".png");
            domos.ForEach(r => r.enabled = true);
        }
    }
}
