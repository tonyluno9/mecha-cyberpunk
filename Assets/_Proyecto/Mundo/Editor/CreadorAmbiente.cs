using System.IO;
using MechaCyberpunk.Mundo.Datos;
using MechaCyberpunk.Mundo.Pruebas;
using MechaCyberpunk.Mundo.Reloj;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.Mundo.Editor
{
    /// <summary>
    /// Crea el ambiente de noche de T-079: perfiles de posproceso (bloom, ACES, color, viñeta), charcos en las calles,
    /// sondas de reflejo horneadas, los Volumes y las vistas de referencia en Ciudad y Pruebas_Alan, y prende el posproceso
    /// en las cámaras de Alan. Se puede correr varias veces: los perfiles y la config de vistas que ya existen no se pisan,
    /// y las sondas solo se hornean si su textura no existe (<see cref="HornearReflejos"/> las rehace).
    /// Batchmode: -executeMethod MechaCyberpunk.Mundo.Editor.CreadorAmbiente.Crear
    /// </summary>
    public static class CreadorAmbiente
    {
        private const string RutaMundo = "Assets/_Proyecto/Mundo";
        private const string RutaDatos = RutaMundo + "/Datos";
        private const string RutaReflejos = RutaMundo + "/Reflejos";
        private const string RutaMateriales = RutaMundo + "/Materiales";
        private const string RutaPerfilNoche = RutaDatos + "/PerfilNoche.asset";
        private const string RutaPerfilDia = RutaDatos + "/PerfilDia.asset";
        private const string RutaConfigVistas = RutaDatos + "/ConfigVistasReferencia.asset";
        private const string RutaConfigCiclo = RutaDatos + "/ConfigCicloDia_Mundo.asset";
        private const string RutaMaterialCharco = RutaMateriales + "/Barrio_Charco.mat";
        private const string RutaMaterialCielo = RutaMateriales + "/Cielo_Procedural.mat";
        private const float GrosorAtmosferaCielo = 0.6f;
        private const string RutaPrefabBarrio = RutaMundo + "/Prefabs/Barrio.prefab";
        private const string RutaPrefabCamaraLibre = RutaMundo + "/Prefabs/CamaraLibre.prefab";
        private const string RutaPrefabCarro = "Assets/_Proyecto/Vehiculos/Prefabs/CarroViejo.prefab";
        private static readonly string[] RutasEscenas =
        {
            "Assets/_Proyecto/Escenas/Ciudad.unity",
            "Assets/_Proyecto/Escenas/Pruebas_Alan.unity"
        };

        // Hora con la que se hornean las sondas (noche cerrada, con los neones prendidos).
        private const float HoraHorneado = 22f;

        // Charcos: quads planos apenas sobre el asfalto (m). La calle principal corre en X (z = 0) y dos calles cruzan en x = ±47.
        private const string NombreCharcos = "Charcos";
        private const int SemillaCharcos = 79;
        private const float AlturaCharco = 0.025f;
        private const float MitadCalle = 5f;
        private const float CentroCalleCruce = 47f;
        private const float MitadLargoBarrioX = 125f;
        private const float MitadLargoBarrioZ = 80f;
        private const float ZonaCruce = 10f;
        private const int CharcosCallePrincipal = 22;
        private const int CharcosCallesCruce = 10;
        private const int CharcosPorCruce = 4;
        private const float AnchoMinimoCharco = 2f;
        private const float AnchoMaximoCharco = 6f;
        private const float LargoMinimoCharco = 1.5f;
        private const float LargoMaximoCharco = 4f;
        private const float GradosPorVuelta = 360f;

        // Sondas de reflejo (m).
        private const string NombreSondas = "Sondas";
        private const int ResolucionSonda = 128;
        private const float DistanciaMinimaSonda = 0.3f;
        private const float DistanciaMaximaSonda = 150f;
        private const float MezclaSonda = 4f;

        private struct DatosSonda
        {
            public string nombre;
            public Vector3 posicion;
            public Vector3 tamano;
        }

        private static readonly DatosSonda[] Sondas =
        {
            new DatosSonda { nombre = "Sonda_CruceOeste", posicion = new Vector3(-47f, 3f, 0f), tamano = new Vector3(40f, 14f, 40f) },
            new DatosSonda { nombre = "Sonda_CruceEste", posicion = new Vector3(47f, 3f, 0f), tamano = new Vector3(40f, 14f, 40f) },
            new DatosSonda { nombre = "Sonda_FrenteClandestino", posicion = new Vector3(0f, 4f, 4f), tamano = new Vector3(50f, 16f, 16f) },
            new DatosSonda { nombre = "Sonda_FrenteBar", posicion = new Vector3(0f, 4f, -4f), tamano = new Vector3(50f, 16f, 16f) }
        };

        [MenuItem("Mecha Cyberpunk/Crear T-079 (ambiente de noche)")]
        public static void Crear()
        {
            AsegurarCarpeta(RutaReflejos);
            ActualizarConfigCiclo();
            CrearPerfil(RutaPerfilNoche, true);
            CrearPerfil(RutaPerfilDia, false);
            CrearConfigVistas();
            Material charco = CrearMaterialCharco();
            CrearMaterialCielo();
            AssetDatabase.SaveAssets();

            PrenderPosproceso(RutaPrefabCamaraLibre);
            PrenderPosproceso(RutaPrefabCarro);
            PonerCharcos(charco);
            AssetDatabase.Refresh();

            foreach (string ruta in RutasEscenas)
            {
                PrepararEscena(ruta);
            }
            AssetDatabase.SaveAssets();
            AsegurarEmision();
            Debug.Log("[Mundo] T-079 creada.");
        }

        /// <summary>Borra las texturas de las sondas y las hornea de nuevo (con el barrio como esté ahora). Lo corren las tareas que cambian el barrio.</summary>
        [MenuItem("Mecha Cyberpunk/Hornear reflejos (T-079)")]
        public static void HornearReflejos()
        {
            foreach (DatosSonda sonda in Sondas)
            {
                AssetDatabase.DeleteAsset(RutaTextura(sonda.nombre));
            }
            Crear();
        }

        private static string RutaTextura(string nombre) => RutaReflejos + "/" + nombre + ".exr";

        private static void AsegurarCarpeta(string ruta)
        {
            if (!AssetDatabase.IsValidFolder(ruta))
            {
                AssetDatabase.CreateFolder(Path.GetDirectoryName(ruta).Replace('\\', '/'), Path.GetFileName(ruta));
            }
        }

        // ---------------------------------------------------------------- ciclo del día

        // La niebla, el ambiente y la curva del peso de noche cambiaron en T-079: se copian del código una sola vez
        // (mientras el asset no tenga el gradiente colorCielo de T-310; esto reemplaza la niebla y el ambiente de día por los nuevos); después los valores afinados a mano no se pisan.
        private static void ActualizarConfigCiclo()
        {
            ConfigCicloDia config = AssetDatabase.LoadAssetAtPath<ConfigCicloDia>(RutaConfigCiclo);
            if (config == null)
            {
                AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<ConfigCicloDia>(), RutaConfigCiclo);
                return;
            }
            if (!File.ReadAllText(RutaConfigCiclo).Contains("colorCielo:"))
            {
                ConfigCicloDia nueva = ScriptableObject.CreateInstance<ConfigCicloDia>();
                EditorUtility.CopySerialized(nueva, config);
                Object.DestroyImmediate(nueva);
                config.name = Path.GetFileNameWithoutExtension(RutaConfigCiclo);
                EditorUtility.SetDirty(config);
            }
        }

        // ---------------------------------------------------------------- perfiles de posproceso

        private static VolumeProfile CrearPerfil(string ruta, bool noche)
        {
            VolumeProfile perfil = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ruta);
            if (perfil == null)
            {
                perfil = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(perfil, ruta);
            }

            if (Agregar(perfil, out Bloom bloom))
            {
                bloom.threshold.Override(noche ? 1f : 1.1f);
                bloom.intensity.Override(noche ? 2f : 0.3f);
                bloom.scatter.Override(noche ? 0.7f : 0.5f);
                bloom.tint.Override(Color.white);
            }
            if (Agregar(perfil, out Tonemapping tonemapping))
            {
                tonemapping.mode.Override(TonemappingMode.ACES);
            }
            if (Agregar(perfil, out ColorAdjustments color))
            {
                color.postExposure.Override(noche ? 2.2f : 2f);
                color.contrast.Override(noche ? 15f : 15f);
                color.saturation.Override(noche ? 25f : 12f);
                color.colorFilter.Override(noche ? new Color(0.82f, 0.9f, 1f) : new Color(1f, 0.98f, 0.94f));
            }
            if (Agregar(perfil, out Vignette vineta))
            {
                vineta.intensity.Override(noche ? 0.28f : 0.15f);
                vineta.smoothness.Override(noche ? 0.45f : 0.4f);
                vineta.color.Override(Color.black);
            }
            EditorUtility.SetDirty(perfil);
            return perfil;
        }

        // Agrega el componente al perfil (y como subasset) si no estaba; devuelve true si lo acaba de crear.
        private static bool Agregar<T>(VolumeProfile perfil, out T componente) where T : VolumeComponent
        {
            if (perfil.TryGet(out componente))
            {
                return false;
            }
            componente = perfil.Add<T>(true);
            componente.active = true;
            AssetDatabase.AddObjectToAsset(componente, perfil);
            return true;
        }

        private static ConfigVistasReferencia CrearConfigVistas()
        {
            ConfigVistasReferencia config = AssetDatabase.LoadAssetAtPath<ConfigVistasReferencia>(RutaConfigVistas);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<ConfigVistasReferencia>();
                AssetDatabase.CreateAsset(config, RutaConfigVistas);
            }
            return config;
        }

        // ---------------------------------------------------------------- cámaras

        private static void PrenderPosproceso(string rutaPrefab)
        {
            GameObject raiz = PrefabUtility.LoadPrefabContents(rutaPrefab);
            foreach (Camera camara in raiz.GetComponentsInChildren<Camera>(true))
            {
                UniversalAdditionalCameraData datos = camara.GetUniversalAdditionalCameraData();
                if (!datos.renderPostProcessing)
                {
                    datos.renderPostProcessing = true;
                    PrefabUtility.SaveAsPrefabAsset(raiz, rutaPrefab);
                }
            }
            PrefabUtility.UnloadPrefabContents(raiz);
        }

        // El keyword _EMISSION se pierde al reimportar si la bandera de GI es None (URP la vuelve a calcular): por eso emisión en tiempo real, y se pone al final.
        private static void AsegurarEmision()
        {
            foreach (string guia in AssetDatabase.FindAssets("t:Material", new[] { RutaMateriales }))
            {
                string ruta = AssetDatabase.GUIDToAssetPath(guia);
                string nombre = Path.GetFileNameWithoutExtension(ruta);
                if (!nombre.StartsWith("Barrio_Letrero_") && !nombre.StartsWith("Bar_Neon") && !nombre.StartsWith("CasinoInt_Neon"))
                {
                    continue;
                }
                Material material = AssetDatabase.LoadAssetAtPath<Material>(ruta);
                material.EnableKeyword("_EMISSION");
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                EditorUtility.SetDirty(material);
            }
            AssetDatabase.SaveAssets();
        }

        // ---------------------------------------------------------------- charcos

        private static Material CrearMaterialCharco()
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(RutaMaterialCharco);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, RutaMaterialCharco);
            }
            material.SetColor("_BaseColor", new Color(0.015f, 0.018f, 0.028f));
            material.SetFloat("_Smoothness", 0.97f);
            material.SetFloat("_Metallic", 0f);
            EditorUtility.SetDirty(material);
            return material;
        }

        // T-310: skybox procedural; CicloDiaNoche le pone tinte, exposición y sol según la hora en una copia en ejecución.
        private static void CrearMaterialCielo()
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(RutaMaterialCielo);
            if (material == null)
            {
                material = new Material(Shader.Find("Skybox/Procedural"));
                AssetDatabase.CreateAsset(material, RutaMaterialCielo);
            }
            material.SetFloat("_AtmosphereThickness", GrosorAtmosferaCielo);
            EditorUtility.SetDirty(material);
        }

        private static void PonerCharcos(Material material)
        {
            GameObject barrio = PrefabUtility.LoadPrefabContents(RutaPrefabBarrio);
            Transform calles = barrio.transform.Find("Calles");
            Transform anterior = calles.Find(NombreCharcos);
            if (anterior != null)
            {
                Object.DestroyImmediate(anterior.gameObject);
            }

            Transform grupo = new GameObject(NombreCharcos).transform;
            grupo.SetParent(calles, false);
            System.Random azar = new System.Random(SemillaCharcos);
            int numero = 0;
            for (int i = 0; i < CharcosCallePrincipal; i++)
            {
                float x = Rango(azar, -MitadLargoBarrioX, MitadLargoBarrioX);
                Charco(grupo, material, azar, ref numero, x, Rango(azar, -MitadCalle, MitadCalle));
            }
            foreach (float signo in new[] { -1f, 1f })
            {
                for (int i = 0; i < CharcosCallesCruce; i++)
                {
                    float z = Rango(azar, ZonaCruce, MitadLargoBarrioZ) * (azar.NextDouble() < 0.5 ? -1f : 1f);
                    Charco(grupo, material, azar, ref numero, signo * CentroCalleCruce + Rango(azar, -MitadCalle, MitadCalle), z);
                }
                for (int i = 0; i < CharcosPorCruce; i++)
                {
                    Charco(grupo, material, azar, ref numero, signo * CentroCalleCruce + Rango(azar, -ZonaCruce, ZonaCruce), Rango(azar, -MitadCalle, MitadCalle));
                }
            }
            PrefabUtility.SaveAsPrefabAsset(barrio, RutaPrefabBarrio);
            PrefabUtility.UnloadPrefabContents(barrio);
        }

        private static float Rango(System.Random azar, float minimo, float maximo)
        {
            return minimo + (float)azar.NextDouble() * (maximo - minimo);
        }

        private static void Charco(Transform padre, Material material, System.Random azar, ref int numero, float x, float z)
        {
            GameObject charco = GameObject.CreatePrimitive(PrimitiveType.Quad);
            charco.name = "Charco_" + numero++;
            Object.DestroyImmediate(charco.GetComponent<Collider>());
            charco.transform.SetParent(padre, false);
            charco.transform.localPosition = new Vector3(x, AlturaCharco, z);
            charco.transform.localRotation = Quaternion.Euler(90f, Rango(azar, 0f, GradosPorVuelta), 0f);
            charco.transform.localScale = new Vector3(Rango(azar, AnchoMinimoCharco, AnchoMaximoCharco), Rango(azar, LargoMinimoCharco, LargoMaximoCharco), 1f);
            charco.GetComponent<MeshRenderer>().sharedMaterial = material;
        }

        // ---------------------------------------------------------------- escenas

        private static void PrepararEscena(string ruta)
        {
            Scene escena = EditorSceneManager.OpenScene(ruta, OpenSceneMode.Single);
            CicloDiaNoche ciclo = Object.FindFirstObjectByType<CicloDiaNoche>();
            // Los assets se cargan por ruta aquí: abrir la escena puede descartar los objetos que se crearon en memoria.
            VolumeProfile noche = AssetDatabase.LoadAssetAtPath<VolumeProfile>(RutaPerfilNoche);
            VolumeProfile dia = AssetDatabase.LoadAssetAtPath<VolumeProfile>(RutaPerfilDia);
            ConfigVistasReferencia vistas = AssetDatabase.LoadAssetAtPath<ConfigVistasReferencia>(RutaConfigVistas);

            Volume volumeNoche = CrearVolume("PosprocesoNoche", noche, 1, 1f);
            Volume volumeDia = CrearVolume("PosprocesoDia", dia, 0, 0f);
            SerializedObject so = new SerializedObject(ciclo);
            so.FindProperty("volumeNoche").objectReferenceValue = volumeNoche;
            so.FindProperty("volumeDia").objectReferenceValue = volumeDia;
            so.FindProperty("cielo").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Material>(RutaMaterialCielo);
            so.ApplyModifiedPropertiesWithoutUndo();

            VistasReferencia componente = Object.FindFirstObjectByType<VistasReferencia>();
            if (componente == null)
            {
                componente = new GameObject("VistasReferencia").AddComponent<VistasReferencia>();
            }
            SerializedObject soVistas = new SerializedObject(componente);
            soVistas.FindProperty("config").objectReferenceValue = vistas;
            soVistas.ApplyModifiedPropertiesWithoutUndo();

            PonerSondas(ciclo);
            EditorSceneManager.MarkSceneDirty(escena);
            EditorSceneManager.SaveScene(escena);
        }

        private static Volume CrearVolume(string nombre, VolumeProfile perfil, int prioridad, float peso)
        {
            GameObject objeto = GameObject.Find(nombre);
            if (objeto == null)
            {
                objeto = new GameObject(nombre);
            }
            Volume volume = objeto.GetComponent<Volume>();
            if (volume == null)
            {
                volume = objeto.AddComponent<Volume>();
            }
            volume.isGlobal = true;
            volume.priority = prioridad;
            volume.weight = peso;
            volume.sharedProfile = perfil;
            return volume;
        }

        // ---------------------------------------------------------------- sondas de reflejo

        private static void PonerSondas(CicloDiaNoche ciclo)
        {
            GameObject grupo = GameObject.Find(NombreSondas);
            if (grupo == null)
            {
                grupo = new GameObject(NombreSondas);
            }
            foreach (DatosSonda datos in Sondas)
            {
                Transform existente = grupo.transform.Find(datos.nombre);
                GameObject objeto = existente != null ? existente.gameObject : new GameObject(datos.nombre);
                objeto.transform.SetParent(grupo.transform, false);
                objeto.transform.position = datos.posicion;
                ReflectionProbe sonda = objeto.GetComponent<ReflectionProbe>();
                if (sonda == null)
                {
                    sonda = objeto.AddComponent<ReflectionProbe>();
                }
                sonda.mode = ReflectionProbeMode.Custom;
                sonda.resolution = ResolucionSonda;
                sonda.hdr = true;
                sonda.boxProjection = true;
                sonda.center = Vector3.zero;
                sonda.size = datos.tamano;
                sonda.nearClipPlane = DistanciaMinimaSonda;
                sonda.farClipPlane = DistanciaMaximaSonda;
                sonda.blendDistance = MezclaSonda;
                sonda.clearFlags = UnityEngine.Rendering.ReflectionProbeClearFlags.SolidColor;

                string ruta = RutaTextura(datos.nombre);
                if (AssetDatabase.LoadAssetAtPath<Texture>(ruta) == null)
                {
                    Hornear(sonda, ruta, ciclo);
                }
                sonda.customBakedTexture = AssetDatabase.LoadAssetAtPath<Texture>(ruta);
            }
        }

        // Hornea la sonda con la escena como a las 22:00: se aplica la hora y después se devuelve todo como estaba para no ensuciar la escena.
        private static void Hornear(ReflectionProbe sonda, string ruta, CicloDiaNoche ciclo)
        {
            ConfigCicloDia config = AssetDatabase.LoadAssetAtPath<ConfigCicloDia>(RutaConfigCiclo);
            Light luz = (Light)new SerializedObject(ciclo).FindProperty("luz").objectReferenceValue;
            Camera principal = Camera.main;

            AmbientMode modoAmbiente = RenderSettings.ambientMode;
            Color ambiente = RenderSettings.ambientLight;
            bool niebla = RenderSettings.fog;
            FogMode modoNiebla = RenderSettings.fogMode;
            Color colorNiebla = RenderSettings.fogColor;
            float densidadNiebla = RenderSettings.fogDensity;
            Color colorLuz = luz.color;
            float intensidadLuz = luz.intensity;
            Quaternion rotacionLuz = luz.transform.rotation;
            Color fondo = principal != null ? principal.backgroundColor : Color.black;

            // Las luces de las farolas están apagadas en el prefab (LuzNocturna las prende al correr): se prenden solo durante el horneado.
            System.Collections.Generic.List<Light> apagadas = new System.Collections.Generic.List<Light>();
            foreach (Light farola in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (farola.type == LightType.Point && !farola.enabled && farola.GetComponentInParent<LuzNocturna>() != null)
                {
                    farola.enabled = true;
                    apagadas.Add(farola);
                }
            }

            ciclo.Aplicar(HoraHorneado);
            sonda.backgroundColor = config.ColorNiebla(HoraHorneado);
            Lightmapping.BakeReflectionProbe(sonda, ruta);

            foreach (Light farola in apagadas)
            {
                farola.enabled = false;
            }
            RenderSettings.ambientMode = modoAmbiente;
            RenderSettings.ambientLight = ambiente;
            RenderSettings.fog = niebla;
            RenderSettings.fogMode = modoNiebla;
            RenderSettings.fogColor = colorNiebla;
            RenderSettings.fogDensity = densidadNiebla;
            luz.color = colorLuz;
            luz.intensity = intensidadLuz;
            luz.transform.rotation = rotacionLuz;
            if (principal != null)
            {
                principal.backgroundColor = fondo;
            }

            AssetDatabase.ImportAsset(ruta);
            TextureImporter importador = AssetImporter.GetAtPath(ruta) as TextureImporter;
            if (importador != null && importador.textureShape != TextureImporterShape.TextureCube)
            {
                importador.textureShape = TextureImporterShape.TextureCube;
                importador.SaveAndReimport();
            }
        }
    }
}
