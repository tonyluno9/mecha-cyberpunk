using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MechaCyberpunk.Mundo.Ambiente;
using MechaCyberpunk.Mundo.Datos;
using MechaCyberpunk.Mundo.Reloj;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-1399: niebla y cielo de noche, ronda 3. Las fotos de la ronda 2 (K11, K13, K15, K18) salían con el cielo azul marino liso y sin niebla visible.
    /// Causas medidas en el código y los assets (ver docs/tareas/hechas/T-1399-niebla-cielo.md):
    /// (1) el domo de nubes CieloNubes estaba apagado (ConfigAmbienteNoche_Mundo.opacidadNubes = 0) y las nubes con textura de CieloTexturizado tienen alfa 0.04 de noche;
    /// (2) ColocarS2cLook.Foto no armaba las nubes ni las estrellas (en el editor no corren Start ni LateUpdate), así que las fotos nunca las tenían;
    /// (3) el color de la niebla de noche (0.15, 0.21, 0.32) es casi el del cielo y el del edificio en sombra, y ACES lo oscurece más: la niebla existía pero no contrastaba;
    /// (4) la densidad de noche era 0.006 (exp. al cuadrado): a 100 m solo 30 %.
    /// Aplicar(): Global (ConfigCicloDia_Mundo: niebla más clara y más densa, cénit oscuro; compartido con los barrios viejos, cambio moderado), Domo (nubes visibles y teñidas por la ciudad),
    /// y por zona nueva: un plano de bruma de altura, halos cálidos en los faroles de sodio y un Volume local (contraste menor y lift en sombras, "lechoso").
    /// DEBE correr DESPUÉS de LookNocheT1392.Aplicar (usa sus volúmenes para medir cada zona). Idempotente.
    /// Batchmode (carpeta principal): -executeMethod MechaCyberpunk.Arte.Editor.LookNocheT1399.Aplicar, y en otra corrida .Fotos. Informe en Logs/t1399-look.txt.
    /// </summary>
    public static class LookNocheT1399
    {
        private const string CarpetaLook = "Assets/Arte/Mundo/Look";
        private const string RutaMallaPlano = CarpetaLook + "/Mallas/T1399_PlanoBruma.asset";
        private const string RutaMallaHalos = CarpetaLook + "/Mallas/T1399_Halos_";
        private const string RutaMaterialBruma = CarpetaLook + "/M_BrumaAltura_T1399.mat";
        private const string RutaMaterialHalos = CarpetaLook + "/M_HalosSodio_T1399.mat";
        private const string RutaPerfilBruma = CarpetaLook + "/PerfilBrumaZonas_T1399.asset";
        private const string RutaInforme = "Logs/t1399-look.txt";
        private const string RaizVolumenes1392 = "T1392_Volumenes";
        private const string RaizBruma = "T1399_Bruma";
        private const string ShaderBruma = "MechaCyberpunk/Fondos/BrumaAltura";
        private const string ShaderHalos = "MechaCyberpunk/Fondos/BrumaHalos";

        private static readonly string[] Zonas = { "T1354_Malecon", "T1355_Cerro", "T1356_Autopista", "T1357_Puerto", "T1388_Uniones" };

        // Valores de noche. Antes (T-1392): niebla (0.15, 0.21, 0.32), densidad 0.006, cénit (0.07, 0.11, 0.22), brillo de ciudad (0.2, 0.24, 0.32) a 0.12, nubes apagadas.
        // El color de la niebla es también el del horizonte del cielo (CicloDiaNoche.AplicarCielo). ACES + PerfilNoche lo dejan a ~35 % de brillo en pantalla:
        // (0.34, 0.42, 0.57) sale en pantalla cerca de #333f58, dentro del rango pedido (#3a4a66 a #55657f) con el volumen de zona (+0.7 de exposición).
        private static readonly Color NieblaNoche = new Color(0.34f, 0.42f, 0.57f);
        private static readonly Color CenitNoche = new Color(0.04f, 0.065f, 0.15f);
        private static readonly Color BrilloCiudad = new Color(0.3f, 0.38f, 0.52f);
        private const float FuerzaBrilloCiudad = 0.3f;
        private const float DensidadNoche = 0.009f; // exp. al cuadrado: 60 m 25 %, 100 m 55 %, 120 m 67 %, 250 m 99 %

        // Nubes
        private const float OpacidadDomo = 0.6f;
        private static readonly Color ColorNubes = new Color(0.3f, 0.35f, 0.47f);
        private static readonly Color BrilloNubes = new Color(0.5f, 0.5f, 0.6f); // tinte de la ciudad, un poco cálido abajo
        private const float CoberturaNubes = 0.62f;

        // Plano de bruma por zona
        private static readonly Color ColorBruma = new Color(0.36f, 0.44f, 0.6f);
        private const float OpacidadBruma = 0.5f;
        private const float AlturaBruma = 10f;
        private const float CercaBruma = 30f;
        private const float LejosBruma = 220f;

        // Halos de los faroles de sodio
        private static readonly Color ColorHalo = new Color(1f, 0.55f, 0.2f);
        private const float IntensidadHalo = 0.45f;
        private const float RadioHalo = 4.5f;
        private const float BajoElFarol = 0.3f;

        // Volumen local (se suma al de T-1392, prioridad mayor): menos contraste y sombras levantadas
        private const float PrioridadVolumen = 12f;
        private const float MezclaVolumen = 25f;
        private const float ContrasteBruma = 3f;
        private static readonly Vector4 LiftBruma = new Vector4(0.95f, 0.98f, 1.05f, 0.05f);

        private const float LejosFotos = 1100f;

        private static readonly StringBuilder Informe = new StringBuilder();

        // ================================================================ Aplicar

        [MenuItem("Mecha Cyberpunk/T1399/Aplicar niebla y cielo ronda 3")]
        public static void Aplicar()
        {
            Informe.Clear();
            Informe.AppendLine("T-1399: niebla y cielo de noche, ronda 3 (correr después de LookNocheT1392.Aplicar)");
            ColocarS2.Abrir();
            Global();
            Domo();
            AssetDatabase.SaveAssets();
            ColocarS2.Abrir();
            Bruma();
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Logs");
            File.WriteAllText(RutaInforme, Informe.ToString());
            Debug.Log("[Arte] " + Informe);
        }

        // ================================================================ Global: ConfigCicloDia_Mundo

        private static void Global()
        {
            Informe.AppendLine();
            Informe.AppendLine("== Global (ConfigCicloDia_Mundo: lo comparten los barrios viejos)");
            ConfigCicloDia config = AssetDatabase.LoadAssetAtPath<ConfigCicloDia>(ColocarS2cLook.RutaCicloDia);
            if (config == null)
            {
                Informe.AppendLine("AVISO: no se pudo cargar ConfigCicloDia_Mundo.");
                return;
            }
            SerializedObject so = new SerializedObject(config);
            // Niebla y horizonte: las claves de noche (fracción del día <= 0.28 o >= 0.8)
            PonerGradiente(so.FindProperty("colorNiebla"), "colorNiebla", NieblaNoche, k => k.time <= 0.28f || k.time >= 0.8f);
            // Cénit: solo la noche cerrada (medianoche y fin de la noche); el alba y el ocaso se quedan como estaban
            PonerGradiente(so.FindProperty("colorCielo"), "colorCielo", CenitNoche, k => k.time <= 0.05f || k.time >= 0.85f);
            // Densidad: toda la noche (de 21:00 a 4:30) igual
            SerializedProperty densidad = so.FindProperty("densidadNiebla");
            AnimationCurve curva = densidad.animationCurveValue;
            Keyframe[] llaves = curva.keys;
            for (int i = 0; i < llaves.Length; i++)
            {
                if (llaves[i].time <= 4.5f || llaves[i].time >= 21f)
                {
                    Informe.AppendLine("  densidadNiebla h=" + llaves[i].time.ToString("F1") + ": " + llaves[i].value + " -> " + DensidadNoche);
                    llaves[i].value = DensidadNoche;
                    llaves[i].inTangent = 0f;
                    llaves[i].outTangent = 0f;
                }
            }
            curva.keys = llaves;
            densidad.animationCurveValue = curva;
            Informe.AppendLine("  colorBrilloCiudad " + so.FindProperty("colorBrilloCiudad").colorValue + " -> " + BrilloCiudad + "; fuerza "
                + so.FindProperty("fuerzaBrilloCiudad").floatValue + " -> " + FuerzaBrilloCiudad);
            so.FindProperty("colorBrilloCiudad").colorValue = BrilloCiudad;
            so.FindProperty("fuerzaBrilloCiudad").floatValue = FuerzaBrilloCiudad;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
            Informe.AppendLine("Efecto de la niebla exp. al cuadrado (1 - e^-(0.009 d)^2): 30 m " + Niebla(30f).ToString("P0") + ", 60 m " + Niebla(60f).ToString("P0") + ", 100 m " + Niebla(100f).ToString("P0")
                + ", 120 m " + Niebla(120f).ToString("P0") + ", 250 m " + Niebla(250f).ToString("P0") + " (antes con 0.006: 100 m " + Niebla(100f, 0.006f).ToString("P0") + ", 250 m " + Niebla(250f, 0.006f).ToString("P0") + ")");
            RenderSettings.fog = true;
            RenderSettings.fogColor = NieblaNoche;
            RenderSettings.fogDensity = DensidadNoche;
        }

        private static float Niebla(float metros, float densidad = DensidadNoche)
        {
            float x = densidad * metros;
            return 1f - Mathf.Exp(-x * x);
        }

        private static void PonerGradiente(SerializedProperty propiedad, string nombre, Color objetivo, System.Func<GradientColorKey, bool> elegir)
        {
            Gradient g = propiedad.gradientValue;
            GradientColorKey[] claves = g.colorKeys;
            for (int i = 0; i < claves.Length; i++)
            {
                if (!elegir(claves[i]))
                {
                    continue;
                }
                Informe.AppendLine("  " + nombre + " t=" + claves[i].time.ToString("F2") + ": " + claves[i].color + " -> " + objetivo);
                claves[i].color = objetivo;
            }
            g.SetKeys(claves, g.alphaKeys);
            propiedad.gradientValue = g;
        }

        // ================================================================ Domo de nubes

        private static void Domo()
        {
            Informe.AppendLine();
            Informe.AppendLine("== Nubes de noche (domo CieloNubes)");
            foreach (CieloNubes domo in Object.FindObjectsByType<CieloNubes>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                SerializedObject sd = new SerializedObject(domo);
                ConfigAmbienteNoche amb = sd.FindProperty("config").objectReferenceValue as ConfigAmbienteNoche;
                if (amb != null)
                {
                    SerializedObject sa = new SerializedObject(amb);
                    Informe.AppendLine("  " + AssetDatabase.GetAssetPath(amb) + ": opacidadNubes " + sa.FindProperty("opacidadNubes").floatValue + " -> " + OpacidadDomo);
                    sa.FindProperty("opacidadNubes").floatValue = OpacidadDomo;
                    sa.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(amb);
                }
                MeshRenderer mr = domo.GetComponent<MeshRenderer>();
                Material m = mr != null ? mr.sharedMaterial : null;
                if (m == null)
                {
                    Informe.AppendLine("  AVISO: el domo " + domo.name + " no tiene material.");
                    continue;
                }
                Informe.AppendLine("  " + AssetDatabase.GetAssetPath(m) + ": _ColorNubes " + m.GetColor("_ColorNubes") + " -> " + ColorNubes + ", _ColorBrillo " + m.GetColor("_ColorBrillo") + " -> " + BrilloNubes
                    + ", _Cobertura " + m.GetFloat("_Cobertura") + " -> " + CoberturaNubes);
                m.SetColor("_ColorNubes", ColorNubes);
                m.SetColor("_ColorBrillo", BrilloNubes);
                m.SetFloat("_Cobertura", CoberturaNubes);
                EditorUtility.SetDirty(m);
            }
            Informe.AppendLine("  Nota: las nubes con textura de CieloTexturizado quedan con alfa 0.04 de noche (CalculoCieloTextura, código de Mundo con pruebas; no se tocó).");
        }

        // ================================================================ Por zona: plano de bruma, halos de sodio, volumen local

        private static void Bruma()
        {
            Informe.AppendLine();
            Informe.AppendLine("== Zonas nuevas: plano de bruma, halos de sodio y volumen local (escena, bajo " + RaizBruma + ")");
            GameObject vieja = GameObject.Find(RaizBruma);
            if (vieja != null)
            {
                Object.DestroyImmediate(vieja);
            }
            GameObject volumenes = GameObject.Find(RaizVolumenes1392);
            CicloDiaNoche ciclo = Object.FindFirstObjectByType<CicloDiaNoche>();
            if (volumenes == null || ciclo == null || ciclo.VolumeNoche == null)
            {
                Informe.AppendLine("AVISO: faltan " + RaizVolumenes1392 + " (corre LookNocheT1392.Aplicar) o el CicloDiaNoche con VolumeNoche: sin bruma por zona.");
                return;
            }
            Directory.CreateDirectory(CarpetaLook + "/Mallas");
            Shader sBruma = Shader.Find(ShaderBruma);
            Shader sHalos = Shader.Find(ShaderHalos);
            if (sBruma == null || sHalos == null)
            {
                Informe.AppendLine("AVISO: no se encontró el shader " + (sBruma == null ? ShaderBruma : ShaderHalos) + " (¿no se importó?): sin bruma por zona.");
                return;
            }
            Material matBruma = HacerMaterial(RutaMaterialBruma, sBruma);
            matBruma.SetColor("_Color", ColorBruma);
            matBruma.SetFloat("_Intensidad", OpacidadBruma);
            matBruma.SetFloat("_Cerca", CercaBruma);
            matBruma.SetFloat("_Lejos", LejosBruma);
            Material matHalos = HacerMaterial(RutaMaterialHalos, sHalos);
            matHalos.SetColor("_Color", ColorHalo);
            matHalos.SetFloat("_Intensidad", IntensidadHalo);
            matHalos.SetFloat("_Tamano", RadioHalo);
            EditorUtility.SetDirty(matBruma);
            EditorUtility.SetDirty(matHalos);
            Mesh plano = MallaPlano();
            VolumeProfile perfil = PerfilBruma();

            Transform raiz = new GameObject(RaizBruma).transform;
            Transform[] todos = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (string zona in Zonas)
            {
                Transform instancia = todos.FirstOrDefault(t => t.name == zona);
                Transform vol = volumenes.transform.Find("VolumenNoche_T1392_" + zona);
                BoxCollider caja = vol != null ? vol.GetComponent<BoxCollider>() : null;
                if (instancia == null || caja == null)
                {
                    Informe.AppendLine(zona + ": sin instancia o sin volumen de T-1392; sin bruma.");
                    continue;
                }
                Bounds b = caja.bounds;
                float suelo = b.min.y + 12f;
                float techoCalle = b.max.y - 45f;
                float y = (suelo + techoCalle) * 0.5f + AlturaBruma;

                // Plano de bruma
                GameObject goPlano = new GameObject("Bruma_" + zona);
                goPlano.transform.SetParent(raiz, false);
                goPlano.transform.position = new Vector3(b.center.x, y, b.center.z);
                goPlano.transform.localScale = new Vector3(b.size.x, 1f, b.size.z);
                goPlano.AddComponent<MeshFilter>().sharedMesh = plano;
                MeshRenderer mrPlano = goPlano.AddComponent<MeshRenderer>();
                mrPlano.sharedMaterial = matBruma;
                SinSombras(mrPlano);

                // Halos de sodio
                List<Vector3> faroles = new List<Vector3>();
                foreach (Light l in instancia.GetComponentsInChildren<Light>(false))
                {
                    if (EsSodio(l))
                    {
                        faroles.Add(l.transform.position + Vector3.down * BajoElFarol);
                    }
                }
                if (faroles.Count > 0)
                {
                    GameObject goHalos = new GameObject("HalosSodio_" + zona);
                    goHalos.transform.SetParent(raiz, false);
                    goHalos.AddComponent<MeshFilter>().sharedMesh = MallaHalos(zona, faroles);
                    MeshRenderer mrHalos = goHalos.AddComponent<MeshRenderer>();
                    mrHalos.sharedMaterial = matHalos;
                    SinSombras(mrHalos);
                }

                // Volumen local de bruma
                GameObject goVol = new GameObject("VolumenBruma_T1399_" + zona);
                goVol.transform.SetParent(raiz, false);
                goVol.transform.position = vol.position;
                BoxCollider col = goVol.AddComponent<BoxCollider>();
                col.isTrigger = true;
                col.size = caja.size;
                col.center = caja.center;
                Volume v = goVol.AddComponent<Volume>();
                v.isGlobal = false;
                v.blendDistance = MezclaVolumen;
                v.priority = PrioridadVolumen;
                v.weight = 1f;
                v.sharedProfile = perfil;
                VolumenSigueNoche sigue = goVol.AddComponent<VolumenSigueNoche>();
                SerializedObject so = new SerializedObject(sigue);
                so.FindProperty("origen").objectReferenceValue = ciclo.VolumeNoche;
                so.ApplyModifiedPropertiesWithoutUndo();
                Informe.AppendLine(zona + ": plano " + b.size.x.ToString("F0") + " x " + b.size.z.ToString("F0") + " m a y=" + y.ToString("F1") + ", " + faroles.Count + " halos de sodio, volumen prioridad " + PrioridadVolumen);
            }
            Informe.AppendLine("Costo estimado: por zona 1 plano (2 triángulos, relleno de pantalla solo donde se ve, sin lecturas de textura), 1 malla de halos (un dibujo, 4 vértices por farol) y 1 volumen (se mezcla en el mismo uber de posproceso: LiftGammaGain no agrega pasada). Sin volumetría ni lecturas de profundidad: bastante menos de 0.5 ms.");
        }

        private static void SinSombras(MeshRenderer mr)
        {
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            mr.lightProbeUsage = LightProbeUsage.Off;
            mr.reflectionProbeUsage = ReflectionProbeUsage.Off;
        }

        private static bool EsSodio(Light l)
        {
            Color c = l.color;
            bool naranja = c.r >= 0.85f && c.g >= 0.35f && c.g <= 0.8f && c.b <= 0.5f;
            string padre = l.transform.parent != null ? l.transform.parent.name : "";
            bool deFarol = padre.StartsWith("Poste") || padre.Contains("Farol");
            return deFarol && (l.type == LightType.Spot || l.type == LightType.Point) && (naranja || l.range >= 10f);
        }

        private static Material HacerMaterial(string ruta, Shader shader)
        {
            Material m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (m == null)
            {
                m = new Material(shader);
                AssetDatabase.CreateAsset(m, ruta);
            }
            m.shader = shader;
            return m;
        }

        private static Mesh MallaPlano()
        {
            Mesh m = AssetDatabase.LoadAssetAtPath<Mesh>(RutaMallaPlano);
            if (m == null)
            {
                m = new Mesh { name = "T1399_PlanoBruma" };
                AssetDatabase.CreateAsset(m, RutaMallaPlano);
            }
            m.Clear();
            m.SetVertices(new[] { new Vector3(-0.5f, 0f, -0.5f), new Vector3(0.5f, 0f, -0.5f), new Vector3(0.5f, 0f, 0.5f), new Vector3(-0.5f, 0f, 0.5f) });
            m.SetUVs(0, new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f) });
            m.SetTriangles(new[] { 0, 2, 1, 0, 3, 2 }, 0);
            m.bounds = new Bounds(Vector3.zero, new Vector3(1f, 0.1f, 1f));
            EditorUtility.SetDirty(m);
            return m;
        }

        private static Mesh MallaHalos(string zona, List<Vector3> centros)
        {
            string ruta = RutaMallaHalos + zona + ".asset";
            Mesh m = AssetDatabase.LoadAssetAtPath<Mesh>(ruta);
            if (m == null)
            {
                m = new Mesh { name = "T1399_Halos_" + zona };
                AssetDatabase.CreateAsset(m, ruta);
            }
            m.Clear();
            List<Vector3> pos = new List<Vector3>();
            List<Vector2> uv = new List<Vector2>();
            List<int> tri = new List<int>();
            Bounds caja = new Bounds(centros[0], Vector3.zero);
            foreach (Vector3 c in centros)
            {
                int i = pos.Count;
                for (int k = 0; k < 4; k++)
                {
                    pos.Add(c);
                }
                uv.Add(new Vector2(-1f, -1f));
                uv.Add(new Vector2(1f, -1f));
                uv.Add(new Vector2(1f, 1f));
                uv.Add(new Vector2(-1f, 1f));
                tri.AddRange(new[] { i, i + 2, i + 1, i, i + 3, i + 2 });
                caja.Encapsulate(c);
            }
            m.indexFormat = pos.Count > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
            m.SetVertices(pos);
            m.SetUVs(0, uv);
            m.SetTriangles(tri, 0);
            caja.Expand(RadioHalo * 2f + 2f);
            m.bounds = caja;
            EditorUtility.SetDirty(m);
            return m;
        }

        private static VolumeProfile PerfilBruma()
        {
            VolumeProfile perfil = AssetDatabase.LoadAssetAtPath<VolumeProfile>(RutaPerfilBruma);
            if (perfil == null)
            {
                perfil = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(perfil, RutaPerfilBruma);
            }
            foreach (Object viejo in AssetDatabase.LoadAllAssetsAtPath(RutaPerfilBruma))
            {
                if (viejo is VolumeComponent)
                {
                    Object.DestroyImmediate(viejo, true);
                }
            }
            perfil.components.Clear();
            ColorAdjustments ca = perfil.Add<ColorAdjustments>(true);
            ca.contrast.Override(ContrasteBruma);
            LiftGammaGain lgg = perfil.Add<LiftGammaGain>(true);
            lgg.lift.Override(LiftBruma);
            foreach (VolumeComponent c in new VolumeComponent[] { ca, lgg })
            {
                AssetDatabase.AddObjectToAsset(c, perfil);
            }
            EditorUtility.SetDirty(perfil);
            AssetDatabase.SaveAssets();
            Informe.AppendLine("Perfil " + RutaPerfilBruma + ": contraste " + ContrasteBruma + " (el de T-1392 es 10; este volumen tiene prioridad " + PrioridadVolumen + " y gana), lift " + LiftBruma + ".");
            return perfil;
        }

        // ================================================================ Fotos

        [MenuItem("Mecha Cyberpunk/T1399/Fotos niebla y cielo ronda 3")]
        public static void Fotos()
        {
            ColocarS2.Abrir();
            const string referencias = "imageassets/referencias/look/";
            const string salida = "docs/capturas/t-1399/";
            Directory.CreateDirectory(salida);
            float lejosAntes = ColocarS2cLook.LejosFoto;
            bool cieloAntes = ColocarS2cLook.CieloCompleto;
            ColocarS2cLook.LejosFoto = LejosFotos;
            ColocarS2cLook.CieloCompleto = true; // nubes, estrellas y domo como en el juego
            // Las cámaras de la ronda 2: K11 (ZonaMaleconT1393), K13 (ZonaCerroT1394), K15 (ZonaPuertoT1396) y K18 (ZonaAutopistaT1395, la primera variante)
            (string nombre, string referencia, Vector3 desde, Vector3 hacia, float campo)[] tomas =
            {
                ("malecon-k11", "K11_Malecon.png", new Vector3(40f, 1.95f, -235.6f), new Vector3(-59f, 7f, -221.7f), 60f),
                ("cerro-k13", "K13_ColoniaCerro.png", ZonaCerroT1394.K13Desde, ZonaCerroT1394.K13Hacia, ZonaCerroT1394.CampoFoto),
                ("puerto-k15", "K15_Puerto.png", ZonaPuertoT1396.CamaraK15, ZonaPuertoT1396.MiraK15, 55f),
                ("autopista-k18", "K18_AutopistaElevada.png", new Vector3(-148f, 2.8f, 217f), new Vector3(-300f, 20f, 238f), 75f),
            };
            try
            {
                foreach (var t in tomas)
                {
                    string juego = salida + t.nombre + ".png";
                    ColocarS2cLook.Foto(DejarLibre(t.desde), t.hacia, juego, t.campo);
                    if (File.Exists(referencias + t.referencia))
                    {
                        ColocarS2.LadoALado(referencias + t.referencia, juego, salida + t.nombre + "-lado.png");
                    }
                    else
                    {
                        Debug.LogWarning("[Arte] Falta la referencia " + referencias + t.referencia + " (solo hay referencias en la carpeta principal)");
                    }
                }
            }
            finally
            {
                ColocarS2cLook.LejosFoto = lejosAntes;
                ColocarS2cLook.CieloCompleto = cieloAntes;
            }
            EditorSceneManager.OpenScene(ColocarS2.RutaEscena, OpenSceneMode.Single); // sin guardar
        }

        private static Vector3 DejarLibre(Vector3 desde)
        {
            Physics.SyncTransforms();
            for (int i = 0; i < 6 && Physics.CheckSphere(desde, 1.5f, ~0, QueryTriggerInteraction.Ignore); i++)
            {
                desde += Vector3.up * 10f;
            }
            return desde;
        }
    }
}
