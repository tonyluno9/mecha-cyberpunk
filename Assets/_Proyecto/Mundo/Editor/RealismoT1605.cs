using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MechaCyberpunk.Mundo.Ambiente;
using MechaCyberpunk.Mundo.Datos;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.Mundo.Editor
{
    /// <summary>
    /// T14 (T-1605): más realismo visual. Aplicar() lo deja todo, sin duplicar nada si se corre otra vez: config, SSAO en los renderers, posproceso
    /// (bloom, ACES, gradación cine, viñeta, aberración), material y domo del cielo, polvo, vapor de alcantarillas, normales genéricos y la
    /// calidad. Diagnostico() lista materiales sin normal o sin textura en Ciudad_Prologo. Fotos() y FotosAntes() sacan capturas en docs/capturas/_tareas/T14.
    /// Orden en batchmode (cada uno en su corrida, con Unity cerrado):
    ///   -executeMethod MechaCyberpunk.Mundo.Editor.RealismoT1605.Diagnostico
    ///   -executeMethod MechaCyberpunk.Mundo.Editor.RealismoT1605.Aplicar
    ///   -executeMethod MechaCyberpunk.Mundo.Editor.RealismoT1605.Fotos   (corrida aparte)
    /// </summary>
    public static class RealismoT1605
    {
        public const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        public const string RutaConfig = "Assets/_Proyecto/Mundo/Datos/ConfigRealismo_Mundo.asset";
        public const string RutaConfigCiclo = "Assets/_Proyecto/Mundo/Datos/ConfigCicloDia_Mundo.asset";
        public const string RutaMaterialCielo = "Assets/_Proyecto/Mundo/Materiales/AMB_CieloRealista.mat";
        public const string RutaMaterialPolvo = "Assets/_Proyecto/Mundo/Materiales/AMB_HaloNiebla.mat";
        public const string RutaMaterialVapor = "Assets/_Proyecto/Mundo/Materiales/AMB_Vapor.mat";
        public const string RutaPerfilNoche = "Assets/_Proyecto/Mundo/Datos/PerfilNoche.asset";
        public const string RutaPerfilDia = "Assets/_Proyecto/Mundo/Datos/PerfilDia.asset";
        public const string RutaRendererPc = "Assets/Settings/PC_Renderer.asset";
        public const string RutaRendererMedio = "Assets/Settings/Medio_Renderer.asset";
        public const string CarpetaTexturas = "Assets/_Proyecto/Mundo/Texturas";
        public const string RutaInforme = "Logs/t1605-materiales.txt";
        public const string CarpetaFotos = "docs/capturas/_tareas/T14";
        public const string NombreRaiz = "Realismo_T1605";

        private const string ShaderCielo = "MechaCyberpunk/CieloRealista";
        private const string NombreSsao = "ScreenSpaceAmbientOcclusion";
        private const int LadoNormal = 256;
        private const float EscalaBump = 0.5f;
        private const int MaximoVapores = 40;
        private const int AnchoFoto = 1280;
        private const int AltoFoto = 720;
        private const float AlturaOjos = 1.7f;

        // Mismos puntos que la medición de fps del 21-nov (ConstruirMedicionAmbiente): pies y dirección de la mirada.
        private static readonly (string nombre, Vector3 pies, Vector3 mira)[] Puntos =
        {
            ("calle", new Vector3(-30f, 0.2f, -2f), new Vector3(1f, -0.04f, 0.12f)),
            ("callejon", new Vector3(-53f, 0.2f, -59f), new Vector3(-1f, -0.02f, 0f)),
            ("clandestino", new Vector3(0f, 0.2f, -1.5f), new Vector3(0f, 0.08f, 1f)),
            ("suburbios_calle", new Vector3(-120f, 0.2f, -2f), new Vector3(1f, -0.03f, 0.05f)),
            ("suburbios_casa", new Vector3(-100f, 0.2f, -45f), new Vector3(0f, 0f, 1f)),
            ("mercado_calle", new Vector3(-47f, 0.2f, 40f), new Vector3(0f, -0.03f, 1f)),
            ("mercado_puerta", new Vector3(10f, 0.2f, -45f), new Vector3(0f, 0f, -1f)),
            ("nova_calle", new Vector3(47f, 0.2f, -30f), new Vector3(0f, -0.03f, 1f)),
            ("neonalto_calle", new Vector3(47f, 0.2f, 45f), new Vector3(0f, -0.03f, 1f)),
            ("tianguis_avenida", new Vector3(-20f, 0.2f, -1f), new Vector3(1f, -0.03f, 0f)),
            ("casino_avenida", new Vector3(110f, 0.2f, 45f), new Vector3(-1f, -0.02f, 0.1f)),
            ("muro_este", new Vector3(118f, 0.2f, 0f), new Vector3(1f, 0.05f, 0f)),
            ("cruce_central", new Vector3(47f, 0.2f, 0f), new Vector3(-1f, 0f, 0f)),
        };

        // ================================================================== Aplicar

        [MenuItem("Mecha Cyberpunk/T14/Aplicar realismo")]
        public static void Aplicar()
        {
            ConfigRealismo config = CrearConfig();
            ScriptableRendererFeature[] ssao = AsegurarSsao();
            AsignarConfig(config, ssao);
            AjustarPerfiles();
            AplicarMateriales();

            Scene escena = AbrirEscena();
            PonerEnEscena(config);
            EditorSceneManager.MarkSceneDirty(escena);
            EditorSceneManager.SaveScene(escena);
            AssetDatabase.SaveAssets();
            Debug.Log("[T-1605] Aplicar listo: config, SSAO (" + ssao.Length + " features), posproceso, normales, cielo, polvo y vapor.");
            SalirEnBatch();
        }

        private static ConfigRealismo CrearConfig()
        {
            ConfigRealismo config = AssetDatabase.LoadAssetAtPath<ConfigRealismo>(RutaConfig);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<ConfigRealismo>();
                AssetDatabase.CreateAsset(config, RutaConfig);
            }
            return config;
        }

        private static void AsignarConfig(ConfigRealismo config, ScriptableRendererFeature[] ssao)
        {
            SerializedObject so = new SerializedObject(config);
            SerializedProperty lista = so.FindProperty("oclusionAmbiental");
            lista.arraySize = ssao.Length;
            for (int i = 0; i < ssao.Length; i++)
            {
                lista.GetArrayElementAtIndex(i).objectReferenceValue = ssao[i];
            }
            so.FindProperty("materialPolvo").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Material>(RutaMaterialPolvo);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
        }

        // ---------------------------------------------------------------- SSAO como Renderer Feature

        /// <summary>Se asegura de que PC_Renderer y Medio_Renderer tengan un feature de SSAO (si falta se agrega) y devuelve los de los dos.</summary>
        private static ScriptableRendererFeature[] AsegurarSsao()
        {
            List<ScriptableRendererFeature> resultado = new List<ScriptableRendererFeature>();
            foreach (string ruta in new[] { RutaRendererPc, RutaRendererMedio })
            {
                ScriptableRendererData datos = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(ruta);
                if (datos == null)
                {
                    Debug.LogWarning("[T-1605] No encuentro " + ruta);
                    continue;
                }
                ScriptableRendererFeature ssao = datos.rendererFeatures.FirstOrDefault(f => f is ScreenSpaceAmbientOcclusion);
                if (ssao == null)
                {
                    ssao = AgregarSsao(datos);
                }
                resultado.Add(ssao);
            }
            return resultado.ToArray();
        }

        private static ScriptableRendererFeature AgregarSsao(ScriptableRendererData datos)
        {
            ScreenSpaceAmbientOcclusion feature = ScriptableObject.CreateInstance<ScreenSpaceAmbientOcclusion>();
            feature.name = NombreSsao;
            AssetDatabase.AddObjectToAsset(feature, datos);
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(feature, out string _, out long idLocal);
            SerializedObject so = new SerializedObject(datos);
            SerializedProperty features = so.FindProperty("m_RendererFeatures");
            SerializedProperty mapa = so.FindProperty("m_RendererFeatureMap");
            int n = features.arraySize;
            features.arraySize = n + 1;
            features.GetArrayElementAtIndex(n).objectReferenceValue = feature;
            mapa.arraySize = n + 1;
            mapa.GetArrayElementAtIndex(n).longValue = idLocal;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(datos);
            return feature;
        }

        // ---------------------------------------------------------------- posproceso

        private static void AjustarPerfiles()
        {
            AjustarPerfil(AssetDatabase.LoadAssetAtPath<VolumeProfile>(RutaPerfilNoche), true);
            AjustarPerfil(AssetDatabase.LoadAssetAtPath<VolumeProfile>(RutaPerfilDia), false);
        }

        // Sombras azules y luces cálidas (en el día más suave), bloom que solo toma lo HDR (umbral ~1), ACES, viñeta suave y aberración muy leve.
        // Nitidez: URP 17 no trae un sharpen propio con escala de render 1 (FSR/STP solo actúan al reducir la escala), así que no se agrega.
        private static void AjustarPerfil(VolumeProfile perfil, bool noche)
        {
            if (perfil == null)
            {
                Debug.LogWarning("[T-1605] Falta un perfil de posproceso.");
                return;
            }
            Bloom bloom = Obtener<Bloom>(perfil);
            bloom.threshold.Override(1f);
            bloom.intensity.Override(noche ? 1.6f : 0.35f);
            bloom.scatter.Override(noche ? 0.65f : 0.5f);
            bloom.highQualityFiltering.Override(false);
            Tonemapping tono = Obtener<Tonemapping>(perfil);
            tono.mode.Override(TonemappingMode.ACES);
            Vignette vineta = Obtener<Vignette>(perfil);
            vineta.intensity.Override(noche ? 0.28f : 0.2f);
            vineta.smoothness.Override(0.5f);
            ChromaticAberration aberracion = Obtener<ChromaticAberration>(perfil);
            aberracion.intensity.Override(noche ? 0.04f : 0.025f);
            SplitToning partido = Obtener<SplitToning>(perfil);
            partido.shadows.Override(noche ? new Color(0.25f, 0.38f, 0.72f) : new Color(0.42f, 0.5f, 0.66f));
            partido.highlights.Override(noche ? new Color(1f, 0.7f, 0.42f) : new Color(1f, 0.86f, 0.68f));
            partido.balance.Override(noche ? -10f : 5f);
            EditorUtility.SetDirty(perfil);
        }

        private static T Obtener<T>(VolumeProfile perfil) where T : VolumeComponent
        {
            if (!perfil.TryGet(out T componente))
            {
                componente = perfil.Add<T>(true);
                AssetDatabase.AddObjectToAsset(componente, perfil);
            }
            componente.active = true;
            return componente;
        }

        // ---------------------------------------------------------------- escena: cielo, polvo y vapor

        private static Scene AbrirEscena()
        {
            Scene activa = SceneManager.GetActiveScene();
            if (activa.path == RutaEscena)
            {
                return activa;
            }
            return EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
        }

        private static void PonerEnEscena(ConfigRealismo config)
        {
            GameObject viejo = GameObject.Find(NombreRaiz);
            while (viejo != null)
            {
                Object.DestroyImmediate(viejo);
                viejo = GameObject.Find(NombreRaiz);
            }
            Transform raiz = new GameObject(NombreRaiz).transform;
            PonerCielo(raiz, config);

            GameObject polvo = new GameObject("Polvo");
            polvo.transform.SetParent(raiz, false);
            SerializedObject soPolvo = new SerializedObject(polvo.AddComponent<PolvoSigueJugador>());
            soPolvo.FindProperty("config").objectReferenceValue = config;
            soPolvo.ApplyModifiedPropertiesWithoutUndo();

            GameObject calidad = new GameObject("Calidad");
            calidad.transform.SetParent(raiz, false);
            SerializedObject soCalidad = new SerializedObject(calidad.AddComponent<CalidadRealismo>());
            soCalidad.FindProperty("config").objectReferenceValue = config;
            soCalidad.ApplyModifiedPropertiesWithoutUndo();

            PonerVapores(raiz);
        }

        private static Material MaterialCielo()
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(RutaMaterialCielo);
            Shader shader = Shader.Find(ShaderCielo);
            if (shader == null)
            {
                Debug.LogError("[T-1605] No encuentro el shader " + ShaderCielo);
                return material;
            }
            if (material == null)
            {
                material = new Material(shader) { name = "AMB_CieloRealista" };
                AssetDatabase.CreateAsset(material, RutaMaterialCielo);
            }
            material.shader = shader;
            material.renderQueue = (int)RenderQueue.Transparent - 140;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void PonerCielo(Transform raiz, ConfigRealismo config)
        {
            GameObject domo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            domo.name = "CieloRealista";
            Object.DestroyImmediate(domo.GetComponent<Collider>());
            domo.transform.SetParent(raiz, false);
            domo.transform.localScale = Vector3.one * (config.RadioDomo * 2f);
            MeshRenderer render = domo.GetComponent<MeshRenderer>();
            render.sharedMaterial = MaterialCielo();
            render.shadowCastingMode = ShadowCastingMode.Off;
            render.receiveShadows = false;
            render.lightProbeUsage = LightProbeUsage.Off;
            render.reflectionProbeUsage = ReflectionProbeUsage.Off;
            CieloRealista cielo = domo.AddComponent<CieloRealista>();
            SerializedObject so = new SerializedObject(cielo);
            so.FindProperty("config").objectReferenceValue = config;
            so.FindProperty("ciclo").objectReferenceValue = AssetDatabase.LoadAssetAtPath<ConfigCicloDia>(RutaConfigCiclo);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // Vapor en las alcantarillas y rejillas de la calle (objetos con "Alcantarilla" o "Rejilla" en el nombre), con el material AMB_Vapor.
        private static void PonerVapores(Transform raiz)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(RutaMaterialVapor);
            if (material == null)
            {
                Debug.LogWarning("[T-1605] Falta AMB_Vapor: no se pone vapor.");
                return;
            }
            Transform grupo = new GameObject("Vapor").transform;
            grupo.SetParent(raiz, false);
            Transform[] candidatos = Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .Where(t => t.childCount == 0 || t.GetComponent<MeshRenderer>() != null)
                .Where(t => t.name.Contains("Alcantarilla") || t.name.Contains("Rejilla"))
                .Where(t => !t.IsChildOf(raiz))
                .OrderBy(t => t.position.x).ThenBy(t => t.position.z)
                .ToArray();
            int n = 0;
            foreach (Transform t in candidatos)
            {
                // Uno cada ~8 m para no llenar la calle de humo.
                if (n >= MaximoVapores || !Separado(grupo, t.position))
                {
                    continue;
                }
                CrearVapor(grupo, "Vapor_" + t.name + "_" + n, t.position, material);
                n++;
            }
            Debug.Log("[T-1605] Vapor en " + n + " rejillas de " + candidatos.Length + " encontradas.");
        }

        private static bool Separado(Transform grupo, Vector3 punto)
        {
            for (int i = 0; i < grupo.childCount; i++)
            {
                if ((grupo.GetChild(i).position - punto).sqrMagnitude < 64f)
                {
                    return false;
                }
            }
            return true;
        }

        private static void CrearVapor(Transform grupo, string nombre, Vector3 posicion, Material material)
        {
            GameObject objeto = new GameObject(nombre);
            objeto.transform.SetParent(grupo, false);
            objeto.transform.position = posicion + Vector3.up * 0.1f;
            objeto.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
            ParticleSystem sistema = objeto.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = sistema.main;
            main.loop = true;
            main.prewarm = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(2f, 3.5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 0.9f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.4f, 0.8f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = new Color(0.8f, 0.82f, 0.9f, 0.2f);
            main.maxParticles = 24;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            ParticleSystem.EmissionModule emision = sistema.emission;
            emision.rateOverTime = 6f;
            ParticleSystem.ShapeModule forma = sistema.shape;
            forma.shapeType = ParticleSystemShapeType.Cone;
            forma.angle = 10f;
            forma.radius = 0.3f;
            ParticleSystem.SizeOverLifetimeModule tamano = sistema.sizeOverLifetime;
            tamano.enabled = true;
            tamano.size = new ParticleSystem.MinMaxCurve(3f, AnimationCurve.Linear(0f, 0.35f, 1f, 1f));
            ParticleSystem.ColorOverLifetimeModule color = sistema.colorOverLifetime;
            color.enabled = true;
            Gradient gradiente = new Gradient();
            gradiente.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.25f), new GradientAlphaKey(0f, 1f) });
            color.color = gradiente;
            ParticleSystemRenderer render = objeto.GetComponent<ParticleSystemRenderer>();
            render.sharedMaterial = material;
            render.shadowCastingMode = ShadowCastingMode.Off;
            render.receiveShadows = false;
        }

        // ================================================================== materiales

        [MenuItem("Mecha Cyberpunk/T14/Diagnostico de materiales")]
        public static void Diagnostico()
        {
            AbrirEscena();
            Dictionary<Material, int> usados = MaterialesDeLaEscena();
            StringBuilder sb = new StringBuilder("T1605 materiales de Ciudad_Prologo: " + usados.Count + " distintos\n");
            int sinNormal = 0;
            int sinTextura = 0;
            foreach (KeyValuePair<Material, int> par in usados.OrderByDescending(p => p.Value))
            {
                Material m = par.Key;
                bool lit = EsLit(m);
                bool normal = TieneTextura(m, "_BumpMap");
                bool textura = TieneTextura(m, "_BaseMap") || TieneTextura(m, "_MainTex");
                if (lit && !normal)
                {
                    sinNormal++;
                }
                if (!textura)
                {
                    sinTextura++;
                }
                if ((lit && !normal) || !textura)
                {
                    sb.AppendLine((lit && !normal ? "SIN_NORMAL " : "") + (!textura ? "SIN_TEXTURA " : "") + m.name + " | " + m.shader.name + " | usos " + par.Value
                        + " | tipo " + CalculoMaterialesT1605.Clasificar(m.name) + " | " + AssetDatabase.GetAssetPath(m));
                }
            }
            sb.Insert(0, "Sin normal map (URP Lit): " + sinNormal + ". Sin textura: " + sinTextura + ".\n");
            Directory.CreateDirectory("Logs");
            File.WriteAllText(RutaInforme, sb.ToString());
            Debug.Log("[T-1605] Diagnostico escrito en " + RutaInforme + " (sin normal " + sinNormal + ", sin textura " + sinTextura + ").");
            SalirEnBatch();
        }

        private static Dictionary<Material, int> MaterialesDeLaEscena()
        {
            Dictionary<Material, int> usados = new Dictionary<Material, int>();
            foreach (Renderer r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                foreach (Material m in r.sharedMaterials)
                {
                    if (m != null)
                    {
                        usados[m] = usados.TryGetValue(m, out int n) ? n + 1 : 1;
                    }
                }
            }
            return usados;
        }

        private static bool EsLit(Material m) => m.shader != null && m.shader.name == "Universal Render Pipeline/Lit";

        private static bool TieneTextura(Material m, string propiedad) => m.HasProperty(propiedad) && m.GetTexture(propiedad) != null;

        /// <summary>Asigna un normal map genérico (por tipo) a los materiales .mat URP Lit de la escena que no tienen; no cambia color ni textura base.</summary>
        private static void AplicarMateriales()
        {
            Scene escena = AbrirEscena();
            Dictionary<TipoMaterialT1605, Texture2D> normales = new Dictionary<TipoMaterialT1605, Texture2D>
            {
                { TipoMaterialT1605.Pared, CrearNormal("T1605_Normal_Pared", 5f, 0.9f, 0.05f) },
                { TipoMaterialT1605.Banqueta, CrearNormal("T1605_Normal_Banqueta", 3f, 1.2f, 0.12f) },
                { TipoMaterialT1605.Metal, CrearNormal("T1605_Normal_Metal", 14f, 0.5f, 0.03f) },
                { TipoMaterialT1605.Pintura, CrearNormal("T1605_Normal_Pintura", 24f, 0.25f, 0.015f) },
            };
            int cambiados = 0;
            foreach (Material m in MaterialesDeLaEscena().Keys)
            {
                string ruta = AssetDatabase.GetAssetPath(m);
                if (!ruta.EndsWith(".mat") || !ruta.StartsWith("Assets/") || ruta.StartsWith("Assets/UnityTechnologies") || !EsLit(m) || TieneTextura(m, "_BumpMap"))
                {
                    continue;
                }
                TipoMaterialT1605 tipo = CalculoMaterialesT1605.Clasificar(m.name);
                if (tipo == TipoMaterialT1605.Otro || m.IsKeywordEnabled("_EMISSION") && m.GetColor("_EmissionColor").maxColorComponent > 0.01f)
                {
                    continue;
                }
                m.SetTexture("_BumpMap", normales[tipo]);
                m.SetFloat("_BumpScale", EscalaBump);
                m.EnableKeyword("_NORMALMAP");
                // Rugosidad genérica solo si el material está sin tunear (metálico 0 y suavidad por defecto, sin mapa) y no es de las zonas S2c (su aspecto base ya está ajustado).
                bool sinMapaBrillo = !TieneTextura(m, "_MetallicGlossMap");
                bool porDefecto = m.GetFloat("_Metallic") < 0.01f && Mathf.Abs(m.GetFloat("_Smoothness") - 0.5f) < 0.01f;
                if (sinMapaBrillo && porDefecto && !m.name.StartsWith("S2c_"))
                {
                    if (tipo == TipoMaterialT1605.Pintura)
                    {
                        m.SetFloat("_Smoothness", 0.8f);
                        m.SetFloat("_Metallic", 0.3f);
                    }
                    else if (tipo == TipoMaterialT1605.Metal)
                    {
                        m.SetFloat("_Smoothness", 0.55f);
                        m.SetFloat("_Metallic", 0.6f);
                    }
                    else
                    {
                        m.SetFloat("_Smoothness", tipo == TipoMaterialT1605.Banqueta ? 0.45f : 0.3f);
                    }
                }
                EditorUtility.SetDirty(m);
                cambiados++;
            }
            Debug.Log("[T-1605] Normal map genérico asignado a " + cambiados + " materiales de " + escena.name + ".");
        }

        // Normal map procedural sin costuras: altura = ruido de valor tileable de "celdas" por lado (a más celdas, grano más fino), normal por diferencias.
        private static Texture2D CrearNormal(string nombre, float celdas, float fuerza, float amplitudRuido)
        {
            string ruta = CarpetaTexturas + "/" + nombre + ".png";
            if (File.Exists(ruta))
            {
                return AssetDatabase.LoadAssetAtPath<Texture2D>(ruta);
            }
            float[,] altura = new float[LadoNormal, LadoNormal];
            for (int y = 0; y < LadoNormal; y++)
            {
                for (int x = 0; x < LadoNormal; x++)
                {
                    float u = x / (float)LadoNormal * celdas;
                    float v = y / (float)LadoNormal * celdas;
                    altura[x, y] = RuidoTileable(u, v, Mathf.RoundToInt(celdas), 11) * 0.7f + RuidoTileable(u * 3f, v * 3f, Mathf.RoundToInt(celdas) * 3, 29) * 0.3f;
                }
            }
            Texture2D textura = new Texture2D(LadoNormal, LadoNormal, TextureFormat.RGBA32, false, true);
            float paso = fuerza * amplitudRuido * LadoNormal;
            for (int y = 0; y < LadoNormal; y++)
            {
                for (int x = 0; x < LadoNormal; x++)
                {
                    float dx = altura[(x + 1) % LadoNormal, y] - altura[(x - 1 + LadoNormal) % LadoNormal, y];
                    float dy = altura[x, (y + 1) % LadoNormal] - altura[x, (y - 1 + LadoNormal) % LadoNormal];
                    Vector3 n = new Vector3(-dx * paso, -dy * paso, 1f).normalized;
                    textura.SetPixel(x, y, new Color(n.x * 0.5f + 0.5f, n.y * 0.5f + 0.5f, n.z * 0.5f + 0.5f, 1f));
                }
            }
            textura.Apply();
            File.WriteAllBytes(ruta, textura.EncodeToPNG());
            Object.DestroyImmediate(textura);
            AssetDatabase.ImportAsset(ruta, ImportAssetOptions.ForceUpdate);
            TextureImporter importador = (TextureImporter)AssetImporter.GetAtPath(ruta);
            importador.textureType = TextureImporterType.NormalMap;
            importador.wrapMode = TextureWrapMode.Repeat;
            importador.mipmapEnabled = true;
            importador.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(ruta);
        }

        private static float Hash(int x, int y, int semilla)
        {
            unchecked
            {
                uint h = (uint)(x * 374761393 + y * 668265263 + semilla * 362437);
                h = (h ^ (h >> 13)) * 1274126177u;
                return ((h ^ (h >> 16)) & 0xFFFF) / 65535f;
            }
        }

        private static float RuidoTileable(float u, float v, int periodo, int semilla)
        {
            periodo = Mathf.Max(1, periodo);
            int x0 = Mathf.FloorToInt(u);
            int y0 = Mathf.FloorToInt(v);
            float fx = u - x0;
            float fy = v - y0;
            fx = fx * fx * (3f - 2f * fx);
            fy = fy * fy * (3f - 2f * fy);
            int xa = ((x0 % periodo) + periodo) % periodo;
            int ya = ((y0 % periodo) + periodo) % periodo;
            int xb = (xa + 1) % periodo;
            int yb = (ya + 1) % periodo;
            float a = Mathf.Lerp(Hash(xa, ya, semilla), Hash(xb, ya, semilla), fx);
            float b = Mathf.Lerp(Hash(xa, yb, semilla), Hash(xb, yb, semilla), fx);
            return Mathf.Lerp(a, b, fy);
        }

        // ================================================================== fotos

        [MenuItem("Mecha Cyberpunk/T14/Fotos despues")]
        public static void Fotos() => TomarFotos("despues_");

        [MenuItem("Mecha Cyberpunk/T14/Fotos antes")]
        public static void FotosAntes() => TomarFotos("antes_");

        private static void TomarFotos(string prefijo)
        {
            AbrirEscena();
            Directory.CreateDirectory(CarpetaFotos);
            CieloRealista cielo = Object.FindFirstObjectByType<CieloRealista>();
            List<(string nombre, Vector3 pos, Vector3 mira)> vistas = new List<(string, Vector3, Vector3)>();
            foreach ((string nombre, Vector3 pies, Vector3 mira) p in Puntos)
            {
                vistas.Add((p.nombre, p.pies + Vector3.up * AlturaOjos, p.pies + Vector3.up * AlturaOjos + p.mira));
            }
            foreach (FueraDelMuroT1543.Vista v in FueraDelMuroT1543.Vistas(FueraDelMuroT1543.LineaDelMuro()))
            {
                vistas.Add((v.Nombre, v.Posicion, v.Objetivo));
            }
            foreach ((string nombre, Vector3 pos, Vector3 mira) v in vistas)
            {
                Foto(prefijo + v.nombre, v.pos, v.mira, cielo);
            }
            SalirEnBatch();
        }

        private static void Foto(string nombre, Vector3 posicion, Vector3 objetivo, CieloRealista cielo)
        {
            GameObject go = new GameObject("CamFotosT1605");
            Camera cam = go.AddComponent<Camera>();
            cam.fieldOfView = 70f;
            cam.farClipPlane = 1500f;
            cam.allowHDR = true;
            UniversalAdditionalCameraData datos = go.AddComponent<UniversalAdditionalCameraData>();
            datos.renderPostProcessing = true;
            go.transform.position = posicion;
            go.transform.LookAt(objetivo);
            if (cielo != null)
            {
                cielo.Actualizar(cam, null, 10f);
            }
            RenderTexture rt = new RenderTexture(AnchoFoto, AltoFoto, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            Texture2D tex = new Texture2D(AnchoFoto, AltoFoto, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, AnchoFoto, AltoFoto), 0, 0);
            tex.Apply();
            File.WriteAllBytes(Path.Combine(CarpetaFotos, nombre + ".png"), tex.EncodeToPNG());
            RenderTexture.active = null;
            cam.targetTexture = null;
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(tex);
            Object.DestroyImmediate(go);
            Debug.Log("[T-1605] foto " + nombre);
        }

        private static void SalirEnBatch()
        {
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }
    }
}
