using System.IO;
using System.Linq;
using MechaCyberpunk.Mundo.Ambiente;
using MechaCyberpunk.Mundo.Datos;
using MechaCyberpunk.Mundo.Reloj;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// Sesión 2 del 7-nov (docs/plan-look-ciudad.md §2): el look de K01 para toda la ciudad. Posproceso de noche (ACES, bloom suave, gradación
    /// azul en sombras y naranja en luces), niebla azul más densa de noche, contornos finos de pantalla completa en el renderer y el reflejo de
    /// los charcos. Idempotente: vuelve a poner los mismos valores. Además trae la foto de comparación con la noche puesta y el reflejo pintado.
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ColocarS2cLook.Aplicar
    /// </summary>
    public static class ColocarS2cLook
    {
        public const string RutaPerfilNoche = "Assets/_Proyecto/Mundo/Datos/PerfilNoche.asset";
        public const string RutaCicloDia = "Assets/_Proyecto/Mundo/Datos/ConfigCicloDia_Mundo.asset";
        public const string RutaConfigCharcos = "Assets/_Proyecto/Mundo/Datos/ConfigCharcos_Mundo.asset";
        public const string RutaMaterialContornos = "Assets/_Proyecto/Mundo/Materiales/S2c_Contornos.mat";
        public const string NombreContornos = "S2c_Contornos";
        public const string RaizLook = "S2c_Look";
        public static readonly string[] Renderers = { "Assets/Settings/PC_Renderer.asset", "Assets/Settings/Medio_Renderer.asset" };
        /// <summary>Los volúmenes de zona: cada barrio se distingue por el color de su luz (§1), pero suave, sobre el tinte de toda la ciudad.</summary>
        public static readonly (string ruta, Color filtro)[] PerfilesLocales =
        {
            ("Assets/Arte/Suburbios/PerfilSuburbios.asset", new Color(1f, 0.94f, 0.86f)),
            ("Assets/Arte/Callejon/PerfilCallejon.asset", new Color(1f, 0.9f, 0.98f)),
            ("Assets/Arte/Tianguis/PerfilTianguis.asset", new Color(1f, 0.92f, 0.97f)),
            ("Assets/Arte/Exteriores/PerfilNeonAlto.asset", new Color(0.985f, 1f, 0.99f)),
        };

        /// <summary>Niebla de noche (exponencial al cuadrado): a ~60 m la calle ya se ve azul y a ~120 m se pierde, como en K01.</summary>
        public const float DensidadNieblaNoche = 0.02f;
        public static readonly Color ColorNieblaNoche = new Color(0.12f, 0.17f, 0.3f);
        public const float HoraFoto = 23f;
        public const float ExposicionNeonAlto = 0.8f;
        /// <summary>Para revisar: guarda la textura del reflejo de los charcos de cada foto en Logs/reflejo-*.png.</summary>
        public static bool GuardarReflejo = true;
        public static readonly Color AmbienteNoche = new Color(0.36f, 0.37f, 0.44f);
        public static readonly Color NubesDomo = new Color(0.06f, 0.08f, 0.14f);
        public static readonly Color BrilloCiudad = new Color(0.24f, 0.3f, 0.42f);

        [MenuItem("Mecha Cyberpunk/S2c/0 Look K01 (posproceso, niebla, contornos, charcos)")]
        public static void Aplicar()
        {
            ColocarS2.Informe.Clear();
            Posproceso();
            Niebla();
            Contornos();
            ColocarS2.Abrir();
            Reflejo();
            Domo();
            ColocarS2.Guardar("s2c-look");
        }

        // ---------------------------------------------------------------- Posproceso

        private static void Posproceso()
        {
            VolumeProfile noche = AssetDatabase.LoadAssetAtPath<VolumeProfile>(RutaPerfilNoche);
            Tonemapping tono = Componente<Tonemapping>(noche);
            tono.mode.Override(TonemappingMode.ACES);
            Bloom bloom = Componente<Bloom>(noche);
            bloom.threshold.Override(0.95f);
            bloom.intensity.Override(1.1f);
            bloom.scatter.Override(0.78f);
            bloom.tint.Override(Color.white);
            bloom.highQualityFiltering.Override(true);
            ColorAdjustments color = Componente<ColorAdjustments>(noche);
            color.postExposure.Override(2.5f);
            color.contrast.Override(12f);
            color.saturation.Override(4f);
            color.colorFilter.Override(new Color(0.96f, 0.98f, 1f));
            SplitToning partido = Componente<SplitToning>(noche);
            partido.shadows.Override(new Color(0.46f, 0.5f, 0.6f));
            partido.highlights.Override(new Color(0.9f, 0.68f, 0.48f));
            partido.balance.Override(-10f);
            WhiteBalance balance = Componente<WhiteBalance>(noche);
            balance.temperature.Override(9f);
            balance.tint.Override(2f);
            Vignette vineta = Componente<Vignette>(noche);
            vineta.intensity.Override(0.3f);
            vineta.smoothness.Override(0.5f);
            vineta.color.Override(new Color(0.02f, 0.03f, 0.07f));
            if (noche.TryGet(out ChromaticAberration aberracion))
            {
                aberracion.intensity.Override(0.04f);
            }
            if (noche.TryGet(out FilmGrain grano))
            {
                grano.intensity.Override(0.08f);
            }
            if (noche.TryGet(out ShadowsMidtonesHighlights smh))
            {
                smh.shadows.Override(new Vector4(0.94f, 0.98f, 1.06f, -0.05f));
                smh.midtones.Override(new Vector4(1f, 0.98f, 1f, 0f));
                smh.highlights.Override(new Vector4(1.04f, 0.98f, 0.94f, 0f));
            }
            EditorUtility.SetDirty(noche);
            // Los volúmenes de zona ya no tiñen encima (el tinte de toda la ciudad sale del global): solo una viñeta y un balance leves.
            foreach ((string ruta, Color filtro) in PerfilesLocales)
            {
                VolumeProfile p = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ruta);
                if (p == null)
                {
                    continue;
                }
                if (p.TryGet(out ColorAdjustments ca))
                {
                    ca.colorFilter.Override(filtro);
                    ca.postExposure.overrideState = false;
                    if (ruta.Contains("NeonAlto"))
                    {
                        // Neón Alto ya trae muchas luces blancas: con la exposición de toda la ciudad se quema. Más oscuro y más saturado (K03).
                        ca.postExposure.Override(ExposicionNeonAlto);
                    }
                    ca.saturation.Override(ruta.Contains("NeonAlto") ? 10f : Mathf.Clamp(ca.saturation.value, 0f, 8f));
                    ca.contrast.overrideState = false;
                }
                if (p.TryGet(out WhiteBalance wb))
                {
                    wb.temperature.Override(Mathf.Clamp(wb.temperature.value, -8f, 8f));
                    wb.tint.Override(Mathf.Clamp(wb.tint.value, -6f, 6f));
                    if (ruta.Contains("NeonAlto"))
                    {
                        // Neón Alto magenta (M11): tinte hacia el magenta y un poco frío.
                        wb.temperature.Override(-4f);
                        wb.tint.Override(8f);
                    }
                }
                if (p.TryGet(out Vignette v))
                {
                    v.intensity.overrideState = false;
                }
                if (p.TryGet(out ShadowsMidtonesHighlights s))
                {
                    s.active = false;
                }
                EditorUtility.SetDirty(p);
                ColocarS2.Informe.AppendLine("  perfil de zona neutro: " + ruta);
            }
            AssetDatabase.SaveAssets();
            ColocarS2.Informe.AppendLine("Posproceso de noche: ACES, bloom 1.1 (umbral 0.95), sombras azules, luces naranjas, viñeta 0.3");
        }

        private static T Componente<T>(VolumeProfile perfil) where T : VolumeComponent
        {
            if (!perfil.TryGet(out T c))
            {
                c = perfil.Add<T>(true);
                AssetDatabase.AddObjectToAsset(c, perfil);
            }
            c.active = true;
            return c;
        }

        // ---------------------------------------------------------------- Niebla

        private static void Niebla()
        {
            ConfigCicloDia config = AssetDatabase.LoadAssetAtPath<ConfigCicloDia>(RutaCicloDia);
            SerializedObject so = new SerializedObject(config);
            SerializedProperty densidad = so.FindProperty("densidadNiebla");
            AnimationCurve curva = densidad.animationCurveValue;
            // La noche entera (de 21:00 a 4:30) con la misma niebla: si no, a las 22 h sale la mitad entre la tarde y la medianoche.
            foreach (float hora in new[] { 4.5f, 21f })
            {
                if (!curva.keys.Any(k => Mathf.Abs(k.time - hora) < 0.01f))
                {
                    curva.AddKey(hora, DensidadNieblaNoche);
                }
            }
            Keyframe[] llaves = curva.keys;
            for (int i = 0; i < llaves.Length; i++)
            {
                if (llaves[i].time <= 4.5f || llaves[i].time >= 21f)
                {
                    llaves[i].value = DensidadNieblaNoche;
                    llaves[i].inTangent = 0f;
                    llaves[i].outTangent = 0f;
                }
            }
            curva.keys = llaves;
            densidad.animationCurveValue = curva;
            NocheAzul(so.FindProperty("colorNiebla"));
            // El cielo de noche es del color de la niebla (CieloDiurnoPruebas): así el horizonte se funde.
            NocheAzul(so.FindProperty("colorCielo"));
            // Ambiente de noche más claro y menos azul: en K01 las paredes en sombra se ven con su color.
            DeNoche(so.FindProperty("colorAmbiente"), AmbienteNoche);
            // El brillo de la ciudad en el horizonte: azul grisáceo como la niebla de K01 (antes morado).
            so.FindProperty("colorBrilloCiudad").colorValue = BrilloCiudad;
            so.FindProperty("fuerzaBrilloCiudad").floatValue = 0.22f;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            ColocarS2.Informe.AppendLine("Niebla de noche: " + DensidadNieblaNoche + " " + ColorNieblaNoche);
        }

        private static void NocheAzul(SerializedProperty color) => DeNoche(color, ColorNieblaNoche);

        private static void DeNoche(SerializedProperty color, Color nuevo)
        {
            Gradient g = color.gradientValue;
            GradientColorKey[] claves = g.colorKeys;
            for (int i = 0; i < claves.Length; i++)
            {
                // Fracción del día: 0 = medianoche. Las llaves de noche (antes de ~4:30 y después de ~21:00) van al azul de K01.
                if (claves[i].time <= 0.19f || claves[i].time >= 0.875f)
                {
                    claves[i].color = nuevo;
                }
            }
            g.SetKeys(claves, g.alphaKeys);
            color.gradientValue = g;
        }

        // ---------------------------------------------------------------- Contornos

        private static void Contornos()
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(RutaMaterialContornos);
            if (material == null)
            {
                material = new Material(Shader.Find("MechaCyberpunk/Mundo/Contornos")) { name = NombreContornos };
                AssetDatabase.CreateAsset(material, RutaMaterialContornos);
            }
            material.shader = Shader.Find("MechaCyberpunk/Mundo/Contornos");
            material.SetColor("_ColorLinea", new Color(0.05f, 0.05f, 0.09f));
            material.SetFloat("_Fuerza", 0.7f);
            material.SetFloat("_Grosor", 1f);
            material.SetFloat("_UmbralProfundidad", 0.05f);
            material.SetFloat("_UmbralNormal", 0.3f);
            material.SetFloat("_Inicio", 22f);
            material.SetFloat("_Fin", 95f);
            EditorUtility.SetDirty(material);
            foreach (string ruta in Renderers)
            {
                UniversalRendererData datos = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(ruta);
                if (datos == null)
                {
                    continue;
                }
                FullScreenPassRendererFeature f = datos.rendererFeatures.OfType<FullScreenPassRendererFeature>().FirstOrDefault(x => x.name == NombreContornos);
                if (f == null)
                {
                    f = ScriptableObject.CreateInstance<FullScreenPassRendererFeature>();
                    f.name = NombreContornos;
                    AssetDatabase.AddObjectToAsset(f, datos);
                    datos.rendererFeatures.Add(f);
                    SerializedObject so = new SerializedObject(datos);
                    SerializedProperty mapa = so.FindProperty("m_RendererFeatureMap");
                    AssetDatabase.TryGetGUIDAndLocalFileIdentifier(f, out string _, out long id);
                    mapa.arraySize = datos.rendererFeatures.Count;
                    mapa.GetArrayElementAtIndex(mapa.arraySize - 1).longValue = id;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
                f.injectionPoint = FullScreenPassRendererFeature.InjectionPoint.BeforeRenderingPostProcessing;
                f.fetchColorBuffer = true;
                f.requirements = ScriptableRenderPassInput.Depth | ScriptableRenderPassInput.Normal;
                f.passMaterial = material;
                f.passIndex = 0;
                f.SetActive(true);
                EditorUtility.SetDirty(f);
                datos.SetDirty();
                EditorUtility.SetDirty(datos);
                ColocarS2.Informe.AppendLine("Contornos en " + ruta);
            }
            AssetDatabase.SaveAssets();
        }

        // ---------------------------------------------------------------- Reflejo de los charcos

        private static void Reflejo()
        {
            ConfigCharcos config = AssetDatabase.LoadAssetAtPath<ConfigCharcos>(RutaConfigCharcos);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<ConfigCharcos>();
                AssetDatabase.CreateAsset(config, RutaConfigCharcos);
            }
            GameObject raiz = GameObject.Find(RaizLook) ?? new GameObject(RaizLook);
            if (!raiz.TryGetComponent(out ReflejoCharcos reflejo))
            {
                reflejo = raiz.AddComponent<ReflejoCharcos>();
            }
            SerializedObject so = new SerializedObject(reflejo);
            so.FindProperty("config").objectReferenceValue = config;
            so.ApplyModifiedPropertiesWithoutUndo();
            ColocarS2.Informe.AppendLine("Reflejo de charcos en " + RaizLook);
        }

        // El domo de nubes de noche (CieloNubes): sin el brillo morado, azul grisáceo como la niebla.
        private static void Domo()
        {
            foreach (CieloNubes domo in Object.FindObjectsByType<CieloNubes>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                Material m = domo.GetComponent<MeshRenderer>() != null ? domo.GetComponent<MeshRenderer>().sharedMaterial : null;
                if (m == null)
                {
                    continue;
                }
                m.SetColor("_ColorNubes", NubesDomo);
                m.SetColor("_ColorBrillo", BrilloCiudad);
                EditorUtility.SetDirty(m);
                ColocarS2.Informe.AppendLine("Domo de nubes: " + AssetDatabase.GetAssetPath(m));
            }
            AssetDatabase.SaveAssets();
        }

        // ---------------------------------------------------------------- Fotos

        /// <summary>
        /// Foto de noche como en el juego: pone la hora <see cref="HoraFoto"/> con CicloDiaNoche (niebla, luz, volúmenes) y pinta antes el
        /// reflejo de los charcos para esta cámara. No guarda la escena. 1376x768, igual que las imágenes de referencia.
        /// </summary>
        public static void Foto(Vector3 desde, Vector3 hacia, string ruta, float campo = 55f, float hora = HoraFoto)
        {
            CicloDiaNoche ciclo = Object.FindFirstObjectByType<CicloDiaNoche>();
            if (ciclo != null)
            {
                ciclo.Aplicar(hora);
                ColocarS2nCielo.SeguirNoche();
            }
            // En el juego LuzNocturna prende de noche las farolas y los letreros; en el editor no corre: se prenden para la foto.
            bool noche = hora >= 19f || hora < 6f;
            foreach (LuzNocturna ln in Object.FindObjectsByType<LuzNocturna>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                Light l = new SerializedObject(ln).FindProperty("luz").objectReferenceValue as Light;
                if (l != null)
                {
                    l.enabled = noche;
                }
            }
            GameObject go = new GameObject("CamaraS2c");
            Camera cam = go.AddComponent<Camera>();
            UniversalAdditionalCameraData datos = go.AddComponent<UniversalAdditionalCameraData>();
            datos.renderPostProcessing = true;
            datos.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 600f;
            cam.fieldOfView = campo;
            cam.transform.position = desde;
            cam.transform.LookAt(hacia);
            const int ancho = 1376;
            const int alto = 768;
            RenderTexture rt = new RenderTexture(ancho, alto, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt;
            cam.aspect = (float)ancho / alto;
            ReflejoCharcos reflejo = Object.FindFirstObjectByType<ReflejoCharcos>();
            if (reflejo != null && reflejo.Actualizar(cam))
            {
                reflejo.Camara.Render();
                if (GuardarReflejo)
                {
                    RenderTexture rr = reflejo.Camara.targetTexture;
                    RenderTexture.active = rr;
                    Texture2D tr = new Texture2D(rr.width, rr.height, TextureFormat.RGBAFloat, false);
                    tr.ReadPixels(new Rect(0, 0, rr.width, rr.height), 0, 0);
                    tr.Apply();
                    File.WriteAllBytes("Logs/reflejo-" + Path.GetFileName(ruta), tr.EncodeToPNG());
                    Object.DestroyImmediate(tr);
                    RenderTexture.active = null;
                }
            }
            cam.Render();
            RenderTexture.active = rt;
            Texture2D foto = new Texture2D(ancho, alto, TextureFormat.RGB24, false);
            foto.ReadPixels(new Rect(0, 0, ancho, alto), 0, 0);
            foto.Apply();
            Directory.CreateDirectory(Path.GetDirectoryName(ruta));
            File.WriteAllBytes(ruta, foto.EncodeToPNG());
            RenderTexture.active = null;
            cam.targetTexture = null;
            Object.DestroyImmediate(foto);
            rt.Release();
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(go);
            if (reflejo != null)
            {
                reflejo.Soltar();
            }
        }
    }
}
