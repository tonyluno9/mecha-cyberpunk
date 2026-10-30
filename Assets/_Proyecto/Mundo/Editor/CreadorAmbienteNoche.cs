using System.Collections.Generic;
using System.IO;
using System.Reflection;
using MechaCyberpunk.Mundo.Ambiente;
using MechaCyberpunk.Mundo.Datos;
using MechaCyberpunk.Mundo.Reloj;
using UnityEditor;
using UnityEditor.Rendering.Universal.ShaderGUI;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.Mundo.Editor
{
    /// <summary>
    /// Ambientación de noche de Ciudad_Prologo (rama tony/ambientacion-noche). Agrega todo bajo la raíz "AMB_Noche" de la escena
    /// sin tocar el arte de T-250 ni los objetos de Alan: nubes, salpicaduras, halos de niebla de los neones, luces de letreros y
    /// puestos, focos sobre la calle de los tacos, cables poste-fachada en catenaria, vapor de los puestos, sondas de reflejo cada
    /// 20 m y el Adaptive Probe Volume. También ajusta assets compartidos: calidad Ultra y Medio, PerfilNoche (tono dividido,
    /// aberración, grano), la luna, el mapa de charcos del asfalto y la niebla baja iluminada. Se puede repetir: rehace AMB_Noche.
    /// Batchmode: -executeMethod MechaCyberpunk.Mundo.Editor.CreadorAmbienteNoche.Todo (Crear + HornearApv + HornearSondas).
    /// </summary>
    public static class CreadorAmbienteNoche
    {
        public const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        public const string NombreRaiz = "AMB_Noche";

        private const string RutaMundo = "Assets/_Proyecto/Mundo";
        private const string RutaMateriales = RutaMundo + "/Materiales";
        private const string RutaTexturas = RutaMundo + "/Texturas";
        private const string RutaReflejos = RutaMundo + "/Reflejos";
        private const string RutaConfig = RutaMundo + "/Datos/ConfigAmbienteNoche_Mundo.asset";
        private const string RutaConfigCiclo = RutaMundo + "/Datos/ConfigCicloDia_Mundo.asset";
        private const string RutaPerfilNoche = RutaMundo + "/Datos/PerfilNoche.asset";
        private const string RutaIluminacion = RutaMundo + "/Datos/Iluminacion_CiudadPrologo.lighting";
        private const string RutaPrefabBarrio = RutaMundo + "/Prefabs/Barrio.prefab";
        private const string RutaBakingSet = "Assets/_Proyecto/Escenas/Ciudad_Prologo/Ciudad_Prologo Baking Set.asset";
        private const string RutaShaderNubes = "MechaCyberpunk/NubesNoche";
        private const string RutaHule = "Assets/Arte/Materiales/Hule_Negro.mat";
        private const string RutaTexturaNube = RutaTexturas + "/Niebla_Nube.png";
        private const string RutaLluviaNiebla = RutaMateriales + "/Lluvia_Niebla.mat";

        private const string RutaPcRp = "Assets/Settings/PC_RPAsset.asset";
        private const string RutaMedioRp = "Assets/Settings/Medio_RPAsset.asset";
        private const string NombreUltra = "Ultra";
        private const string NombreMedio = "Medio";
        private const string NombreCalidadVieja = "PC";

        private const float HoraHorneado = 23f;
        private const float AlturaBanqueta = 0.2f;
        private const float MitadCalleConBanquetas = 9.5f;  // las fachadas de la calle principal están en |z| ~ 9.6 m
        private const float LargoCallePrincipal = 75f;       // |x| hasta donde se ponen luces de letrero

        // ---------------------------------------------------------------- todo

        [MenuItem("Mecha Cyberpunk/Ambiente/Todo (crear, hornear APV y sondas)")]
        public static void Todo()
        {
            Crear();
            HornearApv();
            HornearSondas();
            Debug.Log("[Mundo] Ambiente de noche: todo listo.");
        }

        [MenuItem("Mecha Cyberpunk/Ambiente/Crear ambiente de noche")]
        public static void Crear()
        {
            AsegurarCarpeta(RutaReflejos);
            ConfigurarCalidad();
            AjustarPerfilNoche();
            AjustarLuna();
            AjustarAsfalto();
            AjustarNieblaBaja();
            CrearConfig();
            CrearMateriales();
            AssetDatabase.SaveAssets();

            Scene escena = EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
            GameObject anterior = GameObject.Find(NombreRaiz);
            if (anterior != null)
            {
                Object.DestroyImmediate(anterior);
            }
            // Los assets se vuelven a cargar por ruta: abrir la escena descarta los objetos creados en memoria antes.
            ConfigAmbienteNoche config = AssetDatabase.LoadAssetAtPath<ConfigAmbienteNoche>(RutaConfig);
            Materiales m = CargarMateriales();
            Transform raiz = new GameObject(NombreRaiz).transform;

            PonerNubes(raiz, config, m.Nubes);
            PonerSalpicaduras(raiz, config, m.Salpicadura);
            List<(Transform letrero, Color color)> letreros = LetrerosDeLaCalle();
            PonerLucesLetreros(raiz, letreros);
            PonerHalos(raiz, letreros, m.Halo);
            PonerPuestos(raiz, m.Vapor);
            PonerFocos(raiz, m.Foco, m.Hule);
            PonerCablesFachada(raiz, m.Hule);
            PonerSondas(raiz);
            PonerApv(raiz);

            EditorSceneManager.MarkSceneDirty(escena);
            EditorSceneManager.SaveScene(escena);
            AssetDatabase.SaveAssets();
            Debug.Log("[Mundo] Ambiente de noche creado en " + RutaEscena);
        }

        private static void AsegurarCarpeta(string ruta)
        {
            if (!AssetDatabase.IsValidFolder(ruta))
            {
                AssetDatabase.CreateFolder(Path.GetDirectoryName(ruta).Replace('\\', '/'), Path.GetFileName(ruta));
            }
        }

        // ---------------------------------------------------------------- calidad (punto 1)

        // Ultra es el nivel "PC" de antes con sombras de 4096, HDR y APV; Medio es una copia más barata. Mobile no cambia.
        private static void ConfigurarCalidad()
        {
            UniversalRenderPipelineAsset ultra = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(RutaPcRp);
            AjustarRp(ultra, 4096, 4096, 3, 80f, 4);

            if (AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(RutaMedioRp) == null)
            {
                AssetDatabase.CopyAsset(RutaPcRp, RutaMedioRp);
            }
            UniversalRenderPipelineAsset medio = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(RutaMedioRp);
            AjustarRp(medio, 2048, 1024, 2, 50f, 2);

            Object ajustes = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/QualitySettings.asset")[0];
            SerializedObject so = new SerializedObject(ajustes);
            SerializedProperty niveles = so.FindProperty("m_QualitySettings");
            int indiceUltra = BuscarNivel(niveles, NombreUltra);
            if (indiceUltra < 0)
            {
                indiceUltra = BuscarNivel(niveles, NombreCalidadVieja);
                niveles.GetArrayElementAtIndex(indiceUltra).FindPropertyRelative("name").stringValue = NombreUltra;
            }
            niveles.GetArrayElementAtIndex(indiceUltra).FindPropertyRelative("customRenderPipeline").objectReferenceValue = ultra;
            if (BuscarNivel(niveles, NombreMedio) < 0)
            {
                niveles.InsertArrayElementAtIndex(indiceUltra);  // duplica Ultra en indiceUltra + 1
            }
            int indiceMedio = BuscarNivel(niveles, NombreMedio);
            if (indiceMedio < 0)
            {
                indiceMedio = indiceUltra + 1;
            }
            SerializedProperty nivelMedio = niveles.GetArrayElementAtIndex(indiceMedio);
            nivelMedio.FindPropertyRelative("name").stringValue = NombreMedio;
            nivelMedio.FindPropertyRelative("customRenderPipeline").objectReferenceValue = medio;
            nivelMedio.FindPropertyRelative("lodBias").floatValue = 1f;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static int BuscarNivel(SerializedProperty niveles, string nombre)
        {
            for (int i = 0; i < niveles.arraySize; i++)
            {
                if (niveles.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue == nombre)
                {
                    return i;
                }
            }
            return -1;
        }

        private static void AjustarRp(UniversalRenderPipelineAsset rp, int sombraPrincipal, int sombrasAdicionales, int calidadSuave, float distancia, int cascadas)
        {
            SerializedObject so = new SerializedObject(rp);
            so.FindProperty("m_MainLightShadowmapResolution").intValue = sombraPrincipal;
            so.FindProperty("m_AdditionalLightsShadowmapResolution").intValue = sombrasAdicionales;
            so.FindProperty("m_SoftShadowsSupported").boolValue = true;
            so.FindProperty("m_SoftShadowQuality").intValue = calidadSuave;
            so.FindProperty("m_ShadowDistance").floatValue = distancia;
            so.FindProperty("m_ShadowCascadeCount").intValue = cascadas;
            so.FindProperty("m_ColorGradingMode").intValue = (int)ColorGradingMode.HighDynamicRange;
            so.FindProperty("m_LightProbeSystem").intValue = (int)LightProbeSystem.ProbeVolumes;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(rp);
        }

        // ---------------------------------------------------------------- posproceso (punto 6)

        // Se agregan los componentes que faltan y se ajustan los de T-079 dentro de lo que piden sus pruebas (umbral ~1, viñeta < 0.5, filtro frío).
        private static void AjustarPerfilNoche()
        {
            VolumeProfile perfil = AssetDatabase.LoadAssetAtPath<VolumeProfile>(RutaPerfilNoche);
            if (perfil.TryGet(out Bloom bloom))
            {
                bloom.threshold.Override(1f);
                bloom.intensity.Override(2.6f);
                bloom.scatter.Override(0.75f);
            }
            if (perfil.TryGet(out ColorAdjustments color))
            {
                color.contrast.Override(22f);
                color.saturation.Override(10f);
            }
            if (perfil.TryGet(out Vignette vineta))
            {
                vineta.intensity.Override(0.3f);
            }
            SplitToning tono = Obtener<SplitToning>(perfil);
            tono.shadows.Override(new Color(0.3f, 0.55f, 0.58f));
            tono.highlights.Override(new Color(1f, 0.45f, 0.55f));
            tono.balance.Override(-15f);
            ShadowsMidtonesHighlights smh = Obtener<ShadowsMidtonesHighlights>(perfil);
            smh.shadows.Override(new Vector4(0.95f, 1f, 1f, -0.3f));
            smh.midtones.Override(new Vector4(1f, 0.95f, 1.02f, -0.18f));
            smh.highlights.Override(new Vector4(1.05f, 0.95f, 0.95f, 0.05f));
            ChromaticAberration aberracion = Obtener<ChromaticAberration>(perfil);
            aberracion.intensity.Override(0.06f);
            FilmGrain grano = Obtener<FilmGrain>(perfil);
            grano.type.Override(FilmGrainLookup.Thin1);
            grano.intensity.Override(0.18f);
            grano.response.Override(0.8f);
            EditorUtility.SetDirty(perfil);
        }

        private static T Obtener<T>(VolumeProfile perfil) where T : VolumeComponent
        {
            if (!perfil.TryGet(out T componente))
            {
                componente = perfil.Add<T>(true);
                componente.active = true;
                AssetDatabase.AddObjectToAsset(componente, perfil);
            }
            return componente;
        }

        // ---------------------------------------------------------------- luna (punto 2)

        // La luna baja de 1.1 a 0.55 de noche (claves antes de las 06:00 y después de las 19:30). Las pruebas de T-310 no miran la intensidad.
        private const float IntensidadLuna = 0.55f;
        private const float FinNoche = 5.5f;
        private const float InicioNoche = 19.5f;

        private static void AjustarLuna()
        {
            ConfigCicloDia ciclo = AssetDatabase.LoadAssetAtPath<ConfigCicloDia>(RutaConfigCiclo);
            SerializedObject so = new SerializedObject(ciclo);
            SerializedProperty curva = so.FindProperty("intensidadLuz");
            AnimationCurve intensidad = curva.animationCurveValue;
            Keyframe[] claves = intensidad.keys;
            for (int i = 0; i < claves.Length; i++)
            {
                if (claves[i].time <= FinNoche || claves[i].time >= InicioNoche)
                {
                    claves[i].value = IntensidadLuna;
                }
            }
            intensidad.keys = claves;
            curva.animationCurveValue = intensidad;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---------------------------------------------------------------- piso mojado (punto 5)

        private const string RutaMapaCharcos = RutaTexturas + "/AMB_AsfaltoCharcos.png";
        private const int LadoMapaCharcos = 512;
        private const float SuavidadSeca = 0.6f;
        private static readonly Vector2 RepeticionAsfalto = new Vector2(34f, 22f);   // el cubo del asfalto mide 272 x 178 m: ~8 m por mosaico

        // Mapa metálico-suavidad: R = 0 (no metálico), A = suavidad (charcos 1, asfalto mojado 0.6-0.8). Se multiplica por _Smoothness.
        private static void AjustarAsfalto()
        {
            Texture2D mapa = GuardarTextura(RutaMapaCharcos, LadoMapaCharcos, (x, y) =>
            {
                float n = Ruido.Fbm(x, y, LadoMapaCharcos, 6, 4, 17);
                float charco = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.56f, 0.64f, n));
                float grano = Ruido.Fbm(x, y, LadoMapaCharcos, 48, 2, 5);
                float suavidad = Mathf.Lerp(SuavidadSeca + 0.2f * grano, 1f, charco);
                return new Color(0f, 0f, 0f, suavidad);
            }, true);
            Material asfalto = AssetDatabase.LoadAssetAtPath<Material>(RutaMateriales + "/Barrio_Asfalto.mat");
            asfalto.SetTexture("_MetallicGlossMap", mapa);
            asfalto.SetTextureScale("_BaseMap", RepeticionAsfalto);
            asfalto.SetFloat("_Smoothness", 0.95f);
            asfalto.SetFloat("_Metallic", 0f);
            asfalto.SetFloat("_SmoothnessTextureChannel", 0f);
            asfalto.EnableKeyword("_METALLICSPECGLOSSMAP");
            asfalto.DisableKeyword("_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A");
            EditorUtility.SetDirty(asfalto);
        }

        // ---------------------------------------------------------------- niebla baja (punto 3)

        // La niebla baja de T-096 pasa a Particles/Simple Lit: así la tiñen las luces de los neones y farolas que tiene cerca.
        private static void AjustarNieblaBaja()
        {
            Material niebla = AssetDatabase.LoadAssetAtPath<Material>(RutaLluviaNiebla);
            Shader iluminada = Shader.Find("Universal Render Pipeline/Particles/Simple Lit");
            if (niebla.shader != iluminada)
            {
                Texture textura = niebla.GetTexture("_BaseMap");
                niebla.shader = iluminada;
                niebla.SetTexture("_BaseMap", textura);
            }
            PrepararParticula(niebla, false, true, 2f);
            niebla.SetColor("_BaseColor", new Color(1.4f, 1.4f, 1.6f, 1f));
            EditorUtility.SetDirty(niebla);
        }

        // ---------------------------------------------------------------- assets propios

        private static ConfigAmbienteNoche CrearConfig()
        {
            ConfigAmbienteNoche config = AssetDatabase.LoadAssetAtPath<ConfigAmbienteNoche>(RutaConfig);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<ConfigAmbienteNoche>();
                AssetDatabase.CreateAsset(config, RutaConfig);
            }
            return config;
        }

        private struct Materiales
        {
            public Material Nubes;
            public Material Salpicadura;
            public Material Halo;
            public Material Vapor;
            public Material Foco;
            public Material Hule;
        }

        private const string RutaRuido = RutaTexturas + "/AMB_RuidoNubes.png";
        private const string RutaAnillo = RutaTexturas + "/AMB_Salpicadura.png";
        private const int LadoRuido = 256;
        private const int LadoAnillo = 64;

        private static Materiales CrearMateriales()
        {
            Texture2D ruido = GuardarTextura(RutaRuido, LadoRuido, (x, y) =>
            {
                float n = Ruido.Fbm(x, y, LadoRuido, 4, 5, 31);
                return new Color(n, n, n, 1f);
            }, true);
            Texture2D anillo = GuardarTextura(RutaAnillo, LadoAnillo, (x, y) =>
            {
                float mitad = LadoAnillo / 2f;
                float d = Mathf.Sqrt((x - mitad) * (x - mitad) + (y - mitad) * (y - mitad)) / mitad;
                float a = Mathf.Clamp01(1f - Mathf.Abs(d - 0.8f) / 0.15f);
                return new Color(1f, 1f, 1f, a * a);
            }, false);
            Texture2D nube = AssetDatabase.LoadAssetAtPath<Texture2D>(RutaTexturaNube);

            Materiales m = new Materiales();
            m.Nubes = Material("AMB_Nubes", Shader.Find(RutaShaderNubes));
            m.Nubes.SetTexture("_Ruido", ruido);
            m.Nubes.SetColor("_ColorNubes", new Color(0.1f, 0.09f, 0.17f));
            m.Nubes.SetColor("_ColorBrillo", new Color(0.62f, 0.24f, 0.52f));
            m.Nubes.SetFloat("_Cobertura", 0.58f);
            m.Nubes.SetFloat("_Suavidad", 0.25f);
            m.Nubes.renderQueue = (int)RenderQueue.Transparent - 100;

            m.Salpicadura = Material("AMB_Salpicadura", Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            m.Salpicadura.SetTexture("_BaseMap", anillo);
            PrepararParticula(m.Salpicadura, false, false, 0f);

            m.Halo = Material("AMB_HaloNiebla", Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            m.Halo.SetTexture("_BaseMap", nube);
            PrepararParticula(m.Halo, true, true, 3f);

            m.Vapor = Material("AMB_Vapor", Shader.Find("Universal Render Pipeline/Particles/Simple Lit"));
            m.Vapor.SetTexture("_BaseMap", nube);
            PrepararParticula(m.Vapor, false, true, 1f);

            m.Foco = Material("AMB_Foco", Shader.Find("Universal Render Pipeline/Lit"));
            m.Foco.SetColor("_BaseColor", new Color(0.2f, 0.15f, 0.1f));
            m.Foco.SetColor("_EmissionColor", new Color(1f, 0.62f, 0.3f) * 5f);
            m.Foco.EnableKeyword("_EMISSION");
            m.Foco.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            EditorUtility.SetDirty(m.Foco);

            m.Hule = AssetDatabase.LoadAssetAtPath<Material>(RutaHule);
            return m;
        }

        private static Materiales CargarMateriales()
        {
            return new Materiales
            {
                Nubes = Cargar("AMB_Nubes"),
                Salpicadura = Cargar("AMB_Salpicadura"),
                Halo = Cargar("AMB_HaloNiebla"),
                Vapor = Cargar("AMB_Vapor"),
                Foco = Cargar("AMB_Foco"),
                Hule = AssetDatabase.LoadAssetAtPath<Material>(RutaHule),
            };
        }

        private static Material Cargar(string nombre) => AssetDatabase.LoadAssetAtPath<Material>(RutaMateriales + "/" + nombre + ".mat");

        private static Material Material(string nombre, Shader shader)
        {
            string ruta = RutaMateriales + "/" + nombre + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, ruta);
            }
            else if (material.shader != shader)
            {
                material.shader = shader;
            }
            EditorUtility.SetDirty(material);
            return material;
        }

        // Partícula transparente (alfa o aditiva) con partículas suaves y desvanecido cerca de la cámara (m).
        private static void PrepararParticula(Material material, bool aditiva, bool suave, float desvanecerCamara)
        {
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", aditiva ? 2f : 0f);
            material.SetFloat("_ZWrite", 0f);
            material.SetFloat("_SoftParticlesEnabled", suave ? 1f : 0f);
            material.SetFloat("_SoftParticlesNearFadeDistance", 0f);
            material.SetFloat("_SoftParticlesFarFadeDistance", 1.5f);
            material.SetFloat("_CameraFadingEnabled", desvanecerCamara > 0f ? 1f : 0f);
            material.SetFloat("_CameraNearFadeDistance", desvanecerCamara * 0.3f);
            material.SetFloat("_CameraFarFadeDistance", Mathf.Max(desvanecerCamara, 0.01f));
            BaseShaderGUI.SetMaterialKeywords(material, null, ParticleGUI.SetMaterialKeywords);
            EditorUtility.SetDirty(material);
        }

        private static Texture2D GuardarTextura(string ruta, int lado, System.Func<int, int, Color> pixel, bool repetir)
        {
            Texture2D textura = new Texture2D(lado, lado, TextureFormat.RGBA32, false, true);
            Color[] pixeles = new Color[lado * lado];
            for (int y = 0; y < lado; y++)
            {
                for (int x = 0; x < lado; x++)
                {
                    pixeles[y * lado + x] = pixel(x, y);
                }
            }
            textura.SetPixels(pixeles);
            byte[] bytes = textura.EncodeToPNG();
            Object.DestroyImmediate(textura);
            if (!File.Exists(ruta) || !System.Linq.Enumerable.SequenceEqual(File.ReadAllBytes(ruta), bytes))
            {
                File.WriteAllBytes(ruta, bytes);
            }
            AssetDatabase.ImportAsset(ruta, ImportAssetOptions.ForceSynchronousImport);
            TextureImporter importador = (TextureImporter)AssetImporter.GetAtPath(ruta);
            TextureWrapMode modo = repetir ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            if (importador.sRGBTexture || importador.wrapMode != modo || !importador.alphaIsTransparency)
            {
                importador.sRGBTexture = false;
                importador.alphaIsTransparency = true;
                importador.wrapMode = modo;
                importador.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(ruta);
        }

        // ---------------------------------------------------------------- nubes (punto 2)

        private static void PonerNubes(Transform raiz, ConfigAmbienteNoche config, Material material)
        {
            GameObject domo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            domo.name = "Nubes";
            Object.DestroyImmediate(domo.GetComponent<Collider>());
            domo.transform.SetParent(raiz, false);
            domo.transform.localScale = Vector3.one * (config.RadioDomo * 2f);
            MeshRenderer render = domo.GetComponent<MeshRenderer>();
            render.sharedMaterial = material;
            render.shadowCastingMode = ShadowCastingMode.Off;
            render.receiveShadows = false;
            render.lightProbeUsage = LightProbeUsage.Off;
            render.reflectionProbeUsage = ReflectionProbeUsage.Off;
            CieloNubes nubes = domo.AddComponent<CieloNubes>();
            SerializedObject so = new SerializedObject(nubes);
            so.FindProperty("config").objectReferenceValue = config;
            so.FindProperty("ciclo").objectReferenceValue = AssetDatabase.LoadAssetAtPath<ConfigCicloDia>(RutaConfigCiclo);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---------------------------------------------------------------- salpicaduras (punto 4)

        private static void PonerSalpicaduras(Transform raiz, ConfigAmbienteNoche config, Material material)
        {
            LluviaSigueJugador lluvia = Object.FindFirstObjectByType<LluviaSigueJugador>();
            SalpicadurasLluvia salpicaduras = new GameObject("Salpicaduras").AddComponent<SalpicadurasLluvia>();
            salpicaduras.transform.SetParent(raiz, false);
            SerializedObject so = new SerializedObject(salpicaduras);
            so.FindProperty("config").objectReferenceValue = config;
            so.FindProperty("lluvia").objectReferenceValue = lluvia;
            so.FindProperty("material").objectReferenceValue = material;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---------------------------------------------------------------- letreros (puntos 3 y 7)

        private const float AdelanteLetrero = 0.9f;      // m delante de la cara del letrero
        private const float IntensidadLetrero = 7f;
        private const float AlcanceLetrero = 10f;

        // Los letreros de la calle principal (las fachadas a |z| ~ 9.6 m y |x| <= 75 m), con el color de su primer neón.
        private static List<(Transform, Color)> LetrerosDeLaCalle()
        {
            List<(Transform, Color)> lista = new List<(Transform, Color)>();
            Transform grupo = BuscarEnEscena("LetrerosNeon");
            foreach (Transform letrero in grupo)
            {
                Vector3 p = letrero.position;
                if (Mathf.Abs(p.x) > LargoCallePrincipal || Mathf.Abs(p.z) > MitadCalleConBanquetas + 1.5f || Mathf.Abs(letrero.forward.z) < 0.5f)
                {
                    continue;
                }
                lista.Add((letrero, ColorNeon(letrero)));
            }
            return lista;
        }

        private static Color ColorNeon(Transform letrero)
        {
            foreach (Renderer render in letrero.GetComponentsInChildren<Renderer>())
            {
                if (!render.enabled)
                {
                    continue;
                }
                foreach (Material material in render.sharedMaterials)
                {
                    if (material != null && material.name.StartsWith("Neon_") && material.HasProperty("_EmissionColor"))
                    {
                        Color c = material.GetColor("_EmissionColor");
                        return c / Mathf.Max(c.maxColorComponent, 0.001f);
                    }
                }
            }
            return new Color(1f, 0.3f, 0.8f);
        }

        private static void PonerLucesLetreros(Transform raiz, List<(Transform letrero, Color color)> letreros)
        {
            Transform grupo = Grupo(raiz, "LucesLetreros");
            foreach ((Transform letrero, Color color) in letreros)
            {
                Vector3 posicion = letrero.position + letrero.forward * AdelanteLetrero;
                Luz(grupo, "Luz_" + letrero.name, posicion, color, IntensidadLetrero, AlcanceLetrero, null);
            }
        }

        private const int ParticulasHalo = 3;
        private const float TamanoHalo = 5f;
        private const float AlfaHalo = 0.09f;

        // "Planos de niebla" que se tiñen con el neón: unas pocas partículas grandes, suaves y aditivas que flotan delante del letrero.
        private static void PonerHalos(Transform raiz, List<(Transform letrero, Color color)> letreros, Material material)
        {
            Transform grupo = Grupo(raiz, "NieblaNeon");
            foreach ((Transform letrero, Color color) in letreros)
            {
                GameObject objeto = new GameObject("Halo_" + letrero.name);
                objeto.transform.SetParent(grupo, false);
                objeto.transform.position = letrero.position + letrero.forward * 1.8f;
                ParticleSystem sistema = objeto.AddComponent<ParticleSystem>();
                ParticleSystem.MainModule main = sistema.main;
                main.loop = true;
                main.prewarm = true;
                main.startLifetime = 12f;
                main.startSpeed = 0.05f;
                main.startSize = new ParticleSystem.MinMaxCurve(TamanoHalo * 0.7f, TamanoHalo);
                main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
                main.startColor = new Color(color.r, color.g, color.b, AlfaHalo);
                main.maxParticles = ParticulasHalo * 2;
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                ParticleSystem.EmissionModule emision = sistema.emission;
                emision.rateOverTime = ParticulasHalo / 12f;
                ParticleSystem.ShapeModule forma = sistema.shape;
                forma.shapeType = ParticleSystemShapeType.Box;
                forma.scale = new Vector3(3f, 1.5f, 1.5f);
                ParticleSystem.ColorOverLifetimeModule colorVida = sistema.colorOverLifetime;
                colorVida.enabled = true;
                colorVida.color = Desvanecer();
                ParticleSystemRenderer render = objeto.GetComponent<ParticleSystemRenderer>();
                render.sharedMaterial = material;
                render.shadowCastingMode = ShadowCastingMode.Off;
                render.receiveShadows = false;
            }
        }

        private static Gradient Desvanecer()
        {
            Gradient g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.25f), new GradientAlphaKey(1f, 0.75f), new GradientAlphaKey(0f, 1f) });
            return g;
        }

        // ---------------------------------------------------------------- puestos: luz y vapor (puntos 7 y 8)

        private static readonly Color ColorPuesto = new Color(1f, 0.68f, 0.38f);

        private static void PonerPuestos(Transform raiz, Material vapor)
        {
            Transform grupo = Grupo(raiz, "Puestos");
            List<Transform> puestos = new List<Transform>();
            foreach (Transform t in BuscarEnEscena("PuestosTacos"))
            {
                puestos.Add(t);
            }
            foreach (Transform t in BuscarEnEscena("PuestosCallejon"))
            {
                if (t.name.StartsWith("Puesto_"))
                {
                    puestos.Add(t);
                }
            }
            foreach (Transform puesto in puestos)
            {
                Luz(grupo, "Luz_" + puesto.name, puesto.position + Vector3.up * 2.4f, ColorPuesto, 6f, 7f, null);
                Vapor(grupo, puesto, vapor);
            }
        }

        private static void Vapor(Transform grupo, Transform puesto, Material material)
        {
            GameObject objeto = new GameObject("Vapor_" + puesto.name);
            objeto.transform.SetParent(grupo, false);
            objeto.transform.position = puesto.position + Vector3.up * 1.3f;
            objeto.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
            ParticleSystem sistema = objeto.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = sistema.main;
            main.loop = true;
            main.prewarm = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(2.5f, 4f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.4f, 0.8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.4f, 0.8f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = new Color(0.85f, 0.85f, 0.9f, 0.22f);
            main.maxParticles = 40;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            ParticleSystem.EmissionModule emision = sistema.emission;
            emision.rateOverTime = 8f;
            ParticleSystem.ShapeModule forma = sistema.shape;
            forma.shapeType = ParticleSystemShapeType.Cone;
            forma.angle = 12f;
            forma.radius = 0.35f;
            ParticleSystem.SizeOverLifetimeModule tamano = sistema.sizeOverLifetime;
            tamano.enabled = true;
            tamano.size = new ParticleSystem.MinMaxCurve(3f, AnimationCurve.Linear(0f, 0.35f, 1f, 1f));
            ParticleSystem.VelocityOverLifetimeModule deriva = sistema.velocityOverLifetime;
            deriva.enabled = true;
            deriva.space = ParticleSystemSimulationSpace.World;
            deriva.x = new ParticleSystem.MinMaxCurve(0.1f, 0.35f);
            deriva.y = new ParticleSystem.MinMaxCurve(0f, 0f);
            deriva.z = new ParticleSystem.MinMaxCurve(-0.1f, 0.1f);
            ParticleSystem.ColorOverLifetimeModule color = sistema.colorOverLifetime;
            color.enabled = true;
            color.color = Desvanecer();
            ParticleSystemRenderer render = objeto.GetComponent<ParticleSystemRenderer>();
            render.sharedMaterial = material;
            render.shadowCastingMode = ShadowCastingMode.Off;
            render.receiveShadows = false;
        }

        // ---------------------------------------------------------------- focos sobre la calle de los tacos (punto 8)

        private static readonly float[] XFocos = { -26f, -16f, -5f, 5f, 16f, 26f };
        private const float AlturaFocos = 6.6f;
        private const float DesfaseFocos = 3f;      // m en X entre los dos extremos: las series cruzan en diagonal
        private const float CombaFocos = 0.9f;
        private const int FocosPorSerie = 13;
        private const float TamanoFoco = 0.13f;
        private static readonly Color ColorFocos = new Color(1f, 0.7f, 0.4f);

        private static void PonerFocos(Transform raiz, Material foco, Material hule)
        {
            Transform grupo = Grupo(raiz, "Focos");
            Mesh esfera = Resources.GetBuiltinResource<Mesh>("New-Sphere.fbx");
            for (int s = 0; s < XFocos.Length; s++)
            {
                float signo = s % 2 == 0 ? 1f : -1f;
                Vector3 desde = new Vector3(XFocos[s], AlturaBanqueta + AlturaFocos, MitadCalleConBanquetas);
                Vector3 hasta = new Vector3(XFocos[s] + signo * DesfaseFocos, AlturaBanqueta + AlturaFocos - 0.3f, -MitadCalleConBanquetas);
                Transform serie = new GameObject("Serie_" + s).transform;
                serie.SetParent(grupo, false);
                Vector3[] puntos = Catenaria(desde, hasta, CombaFocos, 24);
                Cable(serie, "Cable", puntos, hule, 0.02f);
                List<Renderer> bulbos = new List<Renderer>();
                for (int i = 1; i <= FocosPorSerie; i++)
                {
                    float t = i / (FocosPorSerie + 1f);
                    GameObject b = new GameObject("Foco_" + i);
                    b.transform.SetParent(serie, false);
                    b.transform.position = PuntoCatenaria(desde, hasta, CombaFocos, t) + Vector3.down * 0.08f;
                    b.transform.localScale = Vector3.one * TamanoFoco;
                    b.AddComponent<MeshFilter>().sharedMesh = esfera;
                    MeshRenderer r = b.AddComponent<MeshRenderer>();
                    r.sharedMaterial = foco;
                    r.shadowCastingMode = ShadowCastingMode.Off;
                    bulbos.Add(r);
                }
                Luz(serie, "Luz_A", PuntoCatenaria(desde, hasta, CombaFocos, 0.3f) + Vector3.down * 0.4f, ColorFocos, 4f, 8f, null);
                Luz(serie, "Luz_B", PuntoCatenaria(desde, hasta, CombaFocos, 0.7f) + Vector3.down * 0.4f, ColorFocos, 4f, 8f, bulbos.ToArray());
            }
        }

        // ---------------------------------------------------------------- cables poste-fachada (punto 8)

        private const float AlturaCablePoste = 6.3f;
        private const float AlcanceCablesFachada = 110f;

        private static void PonerCablesFachada(Transform raiz, Material hule)
        {
            Transform grupo = Grupo(raiz, "CablesFachada");
            System.Random azar = new System.Random(7);
            int numero = 0;
            foreach (Transform farola in BuscarEnEscena("Farolas"))
            {
                Vector3 p = farola.position;
                if (!farola.name.StartsWith("Farola_") || Mathf.Abs(Mathf.Abs(p.z) - 7.6f) > 0.2f || Mathf.Abs(p.x) > AlcanceCablesFachada)
                {
                    continue;
                }
                float lado = Mathf.Sign(p.z);
                foreach (float dx in new[] { -3.5f, 4f })
                {
                    Vector3 desde = new Vector3(p.x, AlturaBanqueta + AlturaCablePoste - (float)azar.NextDouble() * 0.4f, p.z);
                    Vector3 hasta = new Vector3(p.x + dx + (float)azar.NextDouble(), AlturaBanqueta + 4.8f + (float)azar.NextDouble() * 2.5f, lado * (MitadCalleConBanquetas + 0.1f));
                    float comba = 0.25f + (float)azar.NextDouble() * 0.35f;
                    Cable(grupo, "CableFachada_" + (++numero), Catenaria(desde, hasta, comba, 12), hule, 0.03f);
                }
            }
        }

        // Catenaria normalizada: la caída respecto a la cuerda es comba * (cosh(k) - cosh(k (2t - 1))) / (cosh(k) - 1), máxima a la mitad.
        private const float FormaCatenaria = 1.6f;

        private static Vector3 PuntoCatenaria(Vector3 desde, Vector3 hasta, float comba, float t)
        {
            float k = FormaCatenaria;
            float caida = comba * (float)((System.Math.Cosh(k) - System.Math.Cosh(k * (2f * t - 1f))) / (System.Math.Cosh(k) - 1.0));
            return Vector3.Lerp(desde, hasta, t) + Vector3.down * caida;
        }

        private static Vector3[] Catenaria(Vector3 desde, Vector3 hasta, float comba, int puntos)
        {
            Vector3[] lista = new Vector3[puntos];
            for (int i = 0; i < puntos; i++)
            {
                lista[i] = PuntoCatenaria(desde, hasta, comba, i / (puntos - 1f));
            }
            return lista;
        }

        private static void Cable(Transform padre, string nombre, Vector3[] puntos, Material material, float ancho)
        {
            GameObject objeto = new GameObject(nombre);
            objeto.transform.SetParent(padre, false);
            LineRenderer linea = objeto.AddComponent<LineRenderer>();
            linea.useWorldSpace = true;
            linea.positionCount = puntos.Length;
            linea.SetPositions(puntos);
            linea.widthMultiplier = ancho;
            linea.numCapVertices = 2;
            linea.shadowCastingMode = ShadowCastingMode.Off;
            linea.receiveShadows = false;
            linea.sharedMaterial = material;
        }

        // ---------------------------------------------------------------- luces

        // Luz Point de tiempo real, sin sombras, que LuzNocturna prende de noche (con el brillo de los renderers que se le pasen).
        private static Light Luz(Transform padre, string nombre, Vector3 posicion, Color color, float intensidad, float alcance, Renderer[] brillos)
        {
            GameObject objeto = new GameObject(nombre);
            objeto.transform.SetParent(padre, false);
            objeto.transform.position = posicion;
            Light luz = objeto.AddComponent<Light>();
            luz.type = LightType.Point;
            luz.color = color;
            luz.intensity = intensidad;
            luz.range = alcance;
            luz.shadows = LightShadows.None;
            luz.lightmapBakeType = LightmapBakeType.Realtime;
            luz.enabled = false;
            LuzNocturna nocturna = objeto.AddComponent<LuzNocturna>();
            SerializedObject so = new SerializedObject(nocturna);
            so.FindProperty("luz").objectReferenceValue = luz;
            SerializedProperty lista = so.FindProperty("renderers");
            Renderer[] renders = brillos ?? new Renderer[0];
            lista.arraySize = renders.Length;
            for (int i = 0; i < renders.Length; i++)
            {
                lista.GetArrayElementAtIndex(i).objectReferenceValue = renders[i];
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            return luz;
        }

        private static Transform Grupo(Transform raiz, string nombre)
        {
            Transform grupo = new GameObject(nombre).transform;
            grupo.SetParent(raiz, false);
            return grupo;
        }

        private static Transform BuscarEnEscena(string nombre)
        {
            foreach (GameObject raiz in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name == nombre)
                    {
                        return t;
                    }
                }
            }
            throw new System.InvalidOperationException("Ciudad_Prologo no trae '" + nombre + "'.");
        }

        // ---------------------------------------------------------------- sondas cada 20 m (punto 5)

        private const float InicioSondas = -120f;
        private const float PasoSondas = 20f;
        private const int CantidadSondas = 13;
        private static readonly Vector3 TamanoSonda = new Vector3(24f, 14f, 20f);

        private static void PonerSondas(Transform raiz)
        {
            Transform grupo = Grupo(raiz, "SondasCalle");
            for (int i = 0; i < CantidadSondas; i++)
            {
                GameObject objeto = new GameObject(NombreSonda(i));
                objeto.transform.SetParent(grupo, false);
                objeto.transform.position = new Vector3(InicioSondas + i * PasoSondas, 3.5f, 0f);
                ReflectionProbe sonda = objeto.AddComponent<ReflectionProbe>();
                sonda.mode = ReflectionProbeMode.Custom;
                sonda.resolution = 128;
                sonda.hdr = true;
                sonda.boxProjection = true;
                sonda.size = TamanoSonda;
                sonda.importance = 2;
                sonda.blendDistance = 3f;
                sonda.nearClipPlane = 0.3f;
                sonda.farClipPlane = 150f;
                sonda.clearFlags = ReflectionProbeClearFlags.Skybox;
                sonda.customBakedTexture = AssetDatabase.LoadAssetAtPath<Texture>(RutaSonda(i));
            }
        }

        private static string NombreSonda(int i) => "AMB_Sonda_" + i.ToString("00");

        private static string RutaSonda(int i) => RutaReflejos + "/" + NombreSonda(i) + ".exr";

        /// <summary>Hornea las sondas de AMB_Noche a las 23:00 con las farolas y las luces nuevas prendidas.</summary>
        [MenuItem("Mecha Cyberpunk/Ambiente/Hornear sondas de la calle")]
        public static void HornearSondas()
        {
            Scene escena = EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
            using (new EscenaDeNoche())
            {
                Transform grupo = BuscarEnEscena("SondasCalle");
                for (int i = 0; i < grupo.childCount; i++)
                {
                    ReflectionProbe sonda = grupo.GetChild(i).GetComponent<ReflectionProbe>();
                    string ruta = RutaSonda(i);
                    Lightmapping.BakeReflectionProbe(sonda, ruta);
                    AssetDatabase.ImportAsset(ruta);
                    TextureImporter importador = AssetImporter.GetAtPath(ruta) as TextureImporter;
                    if (importador != null && importador.textureShape != TextureImporterShape.TextureCube)
                    {
                        importador.textureShape = TextureImporterShape.TextureCube;
                        importador.SaveAndReimport();
                    }
                    sonda.customBakedTexture = AssetDatabase.LoadAssetAtPath<Texture>(ruta);
                }
            }
            EditorSceneManager.MarkSceneDirty(escena);
            EditorSceneManager.SaveScene(escena);
            Debug.Log("[Mundo] Sondas de la calle horneadas.");
        }

        // Pone la escena como a las 23:00 (ciclo, farolas y luces nuevas prendidas) y al terminar la deja como estaba.
        private sealed class EscenaDeNoche : System.IDisposable
        {
            private readonly AmbientMode modoAmbiente = RenderSettings.ambientMode;
            private readonly Color ambiente = RenderSettings.ambientLight;
            private readonly bool niebla = RenderSettings.fog;
            private readonly FogMode modoNiebla = RenderSettings.fogMode;
            private readonly Color colorNiebla = RenderSettings.fogColor;
            private readonly float densidadNiebla = RenderSettings.fogDensity;
            private readonly Material cielo = RenderSettings.skybox;
            private readonly Light sol;
            private readonly Color colorSol;
            private readonly float intensidadSol;
            private readonly Quaternion rotacionSol;
            private readonly List<Light> prendidas = new List<Light>();

            public EscenaDeNoche()
            {
                CicloDiaNoche ciclo = Object.FindFirstObjectByType<CicloDiaNoche>();
                sol = (Light)new SerializedObject(ciclo).FindProperty("luz").objectReferenceValue;
                colorSol = sol.color;
                intensidadSol = sol.intensity;
                rotacionSol = sol.transform.rotation;
                foreach (Light luz in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
                {
                    if (luz.type == LightType.Point && !luz.enabled && luz.GetComponentInParent<LuzNocturna>() != null)
                    {
                        luz.enabled = true;
                        prendidas.Add(luz);
                    }
                }
                ciclo.Aplicar(HoraHorneado);
            }

            public List<Light> Prendidas => prendidas;

            public void Dispose()
            {
                foreach (Light luz in prendidas)
                {
                    luz.enabled = false;
                }
                RenderSettings.ambientMode = modoAmbiente;
                RenderSettings.ambientLight = ambiente;
                RenderSettings.fog = niebla;
                RenderSettings.fogMode = modoNiebla;
                RenderSettings.fogColor = colorNiebla;
                RenderSettings.fogDensity = densidadNiebla;
                RenderSettings.skybox = cielo;
                sol.color = colorSol;
                sol.intensity = intensidadSol;
                sol.transform.rotation = rotacionSol;
            }
        }

        // ---------------------------------------------------------------- APV (punto 7)

        private static readonly Vector3 CentroApv = new Vector3(0f, 14f, 0f);
        private static readonly Vector3 TamanoApv = new Vector3(285f, 36f, 195f);
        private const float DistanciaEntreSondasApv = 3f;    // m entre sondas en el nivel más fino
        private const float RebotesLuces = 0.6f;              // fracción del rebote de las luces que se hornea (de día también queda)

        private static void PonerApv(Transform raiz)
        {
            GameObject objeto = new GameObject("APV");
            objeto.transform.SetParent(raiz, false);
            objeto.transform.position = CentroApv;
            ProbeVolume volumen = objeto.AddComponent<ProbeVolume>();
            volumen.mode = ProbeVolume.Mode.Local;
            volumen.size = TamanoApv;
        }

        /// <summary>
        /// Hornea el Adaptive Probe Volume de Ciudad_Prologo a las 23:00 con sky occlusion (el ambiente del ciclo sigue cambiando con la hora):
        /// marca el barrio como Contribute GI (recibe GI de sondas, sin lightmaps), pone en Mixed solo durante el horneado las luces que
        /// LuzNocturna prende y los neones como emisión horneada, y deja todo como estaba.
        /// </summary>
        [MenuItem("Mecha Cyberpunk/Ambiente/Hornear APV")]
        public static void HornearApv()
        {
            MarcarBarrioEstatico();
            Scene escena = EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
            Lightmapping.lightingSettings = Iluminacion();
            AsegurarBakingSet(escena);

            List<(Material material, MaterialGlobalIlluminationFlags flags)> neones = new List<(Material, MaterialGlobalIlluminationFlags)>();
            foreach (Renderer render in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            {
                foreach (Material material in render.sharedMaterials)
                {
                    if (material != null && material.IsKeywordEnabled("_EMISSION") && material.name.StartsWith("Neon_") && !neones.Exists(n => n.material == material))
                    {
                        neones.Add((material, material.globalIlluminationFlags));
                        material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
                    }
                }
            }

            using (EscenaDeNoche noche = new EscenaDeNoche())
            {
                List<(Light luz, LightmapBakeType tipo, float rebote)> luces = new List<(Light, LightmapBakeType, float)>();
                foreach (Light luz in noche.Prendidas)
                {
                    luces.Add((luz, luz.lightmapBakeType, luz.bounceIntensity));
                    luz.lightmapBakeType = LightmapBakeType.Mixed;
                    luz.bounceIntensity = RebotesLuces;
                }
                bool horneado = Lightmapping.Bake();
                foreach ((Light luz, LightmapBakeType tipo, float rebote) in luces)
                {
                    luz.lightmapBakeType = tipo;
                    luz.bounceIntensity = rebote;
                }
                Debug.Log("[Mundo] APV horneado: " + horneado + ", luces en el horneado: " + luces.Count + ", neones: " + neones.Count);
            }
            foreach ((Material material, MaterialGlobalIlluminationFlags flags) in neones)
            {
                material.globalIlluminationFlags = flags;
            }
            EditorSceneManager.MarkSceneDirty(escena);
            EditorSceneManager.SaveScene(escena);
            AssetDatabase.SaveAssets();
        }

        private static LightingSettings Iluminacion()
        {
            LightingSettings ajustes = AssetDatabase.LoadAssetAtPath<LightingSettings>(RutaIluminacion);
            if (ajustes == null)
            {
                ajustes = new LightingSettings { name = Path.GetFileNameWithoutExtension(RutaIluminacion) };
                AssetDatabase.CreateAsset(ajustes, RutaIluminacion);
            }
            ajustes.bakedGI = true;
            ajustes.realtimeGI = false;
            ajustes.autoGenerate = false;
            ajustes.lightmapper = LightingSettings.Lightmapper.ProgressiveGPU;
            ajustes.mixedBakeMode = MixedLightingMode.IndirectOnly;
            ajustes.directSampleCount = 32;
            ajustes.indirectSampleCount = 256;
            ajustes.environmentSampleCount = 128;
            ajustes.maxBounces = 2;
            ajustes.lightmapResolution = 1f;
            EditorUtility.SetDirty(ajustes);
            return ajustes;
        }

        // El baking set lo crea la ventana de Lighting; en batch no hay ventana, así que se crea aquí (SetDefaults y singleSceneMode son internos).
        private static void AsegurarBakingSet(Scene escena)
        {
            ProbeVolumeBakingSet set = AssetDatabase.LoadAssetAtPath<ProbeVolumeBakingSet>(RutaBakingSet);
            if (set == null)
            {
                AsegurarCarpeta(Path.GetDirectoryName(RutaBakingSet).Replace('\\', '/'));
                set = ScriptableObject.CreateInstance<ProbeVolumeBakingSet>();
                typeof(ProbeVolumeBakingSet).GetMethod("SetDefaults", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(set, null);
                set.name = Path.GetFileNameWithoutExtension(RutaBakingSet);
                AssetDatabase.CreateAsset(set, RutaBakingSet);
            }
            typeof(ProbeVolumeBakingSet).GetField("singleSceneMode", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(set, true);
            set.TryAddScene(AssetDatabase.AssetPathToGUID(escena.path));
            set.skyOcclusion = true;
            set.minDistanceBetweenProbes = DistanciaEntreSondasApv;
            set.simplificationLevels = 3;
            EditorUtility.SetDirty(set);
            AssetDatabase.SaveAssets();
        }

        // Contribute GI en los renderers del barrio (recibiendo GI de las sondas, no de lightmaps). Lluvia y partículas no cuentan.
        private static void MarcarBarrioEstatico()
        {
            GameObject barrio = PrefabUtility.LoadPrefabContents(RutaPrefabBarrio);
            int marcados = 0;
            foreach (MeshRenderer render in barrio.GetComponentsInChildren<MeshRenderer>(true))
            {
                GameObject objeto = render.gameObject;
                StaticEditorFlags flags = GameObjectUtility.GetStaticEditorFlags(objeto);
                if ((flags & StaticEditorFlags.ContributeGI) == 0 || render.receiveGI != ReceiveGI.LightProbes)
                {
                    GameObjectUtility.SetStaticEditorFlags(objeto, flags | StaticEditorFlags.ContributeGI);
                    render.receiveGI = ReceiveGI.LightProbes;
                    marcados++;
                }
            }
            if (marcados > 0)
            {
                PrefabUtility.SaveAsPrefabAsset(barrio, RutaPrefabBarrio);
            }
            PrefabUtility.UnloadPrefabContents(barrio);
            Debug.Log("[Mundo] Barrio: " + marcados + " renderers marcados como Contribute GI.");
        }

        // ---------------------------------------------------------------- ruido tileable

        private static class Ruido
        {
            // Ruido de valor con interpolación suave sobre una rejilla periódica (se repite cada 'lado' píxeles), en octavas.
            public static float Fbm(int x, int y, int lado, int celdas, int octavas, int semilla)
            {
                float suma = 0f;
                float amplitud = 0.5f;
                float total = 0f;
                for (int o = 0; o < octavas; o++)
                {
                    int c = celdas << o;
                    suma += amplitud * Valor(x * c / (float)lado, y * c / (float)lado, c, semilla + o * 101);
                    total += amplitud;
                    amplitud *= 0.5f;
                }
                return suma / total;
            }

            private static float Valor(float u, float v, int periodo, int semilla)
            {
                int x0 = Mathf.FloorToInt(u);
                int y0 = Mathf.FloorToInt(v);
                float fx = u - x0;
                float fy = v - y0;
                fx = fx * fx * (3f - 2f * fx);
                fy = fy * fy * (3f - 2f * fy);
                float a = Hash(x0, y0, periodo, semilla);
                float b = Hash(x0 + 1, y0, periodo, semilla);
                float c = Hash(x0, y0 + 1, periodo, semilla);
                float d = Hash(x0 + 1, y0 + 1, periodo, semilla);
                return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
            }

            private static float Hash(int x, int y, int periodo, int semilla)
            {
                x = ((x % periodo) + periodo) % periodo;
                y = ((y % periodo) + periodo) % periodo;
                unchecked
                {
                    uint h = (uint)(x * 374761393 + y * 668265263 + semilla * 982451653);
                    h = (h ^ (h >> 13)) * 1274126177u;
                    h ^= h >> 16;
                    return (h & 0xFFFFFF) / (float)0xFFFFFF;
                }
            }
        }
    }
}
