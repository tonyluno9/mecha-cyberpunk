using System.IO;
using MechaCyberpunk.Progresion;
using MechaCyberpunk.Progresion.Guardado;
using MechaCyberpunk.Progresion.Opciones;
using MechaCyberpunk.Progresion.Sonido;
using MechaCyberpunk.UI.Titulo;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.UI.Inicio.Editor
{
    /// <summary>
    /// Crea la escena de la pantalla de inicio (Escenas/Inicio.unity) con su cámara, el fondo 3D, el posprocesado (brillo de neón,
    /// viñeta, aberración), los servicios que necesita (opciones, guardado, perfil, sonidos), la creación de personaje y el menú. También
    /// crea los materiales: UI/Resources/UIAditivo.mat y los de UI/Inicio/Materiales. Idempotente: vuelve a armar la escena entera.
    /// Batchmode: -executeMethod MechaCyberpunk.UI.Inicio.Editor.CreadorEscenaInicio.Crear
    /// </summary>
    public static class CreadorEscenaInicio
    {
        public const string RutaEscena = EscenaInicio.Ruta;
        public const string RutaMaterialAditivoUI = "Assets/_Proyecto/UI/Resources/UIAditivo.mat";
        public const string CarpetaMateriales = "Assets/_Proyecto/UI/Inicio/Materiales";
        public const string RutaLit = CarpetaMateriales + "/Inicio_Lit.mat";
        public const string RutaAditivo3D = CarpetaMateriales + "/Inicio_Aditivo3D.mat";
        public const string RutaVolumen = "Assets/_Proyecto/UI/Inicio/Datos/VolumenInicio.asset";
        private const string RutaCatalogo = "Assets/_Proyecto/Progresion/Datos/CatalogoSonidos.asset";

        [MenuItem("Mecha Cyberpunk/Inicio/Crear escena de inicio")]
        public static void Crear()
        {
            Directory.CreateDirectory(CarpetaMateriales);
            Directory.CreateDirectory(Path.GetDirectoryName(RutaVolumen));
            CrearMateriales();
            VolumeProfile perfilVolumen = CrearVolumen();

            Scene escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject camaraObjeto = new GameObject("Camara", typeof(Camera), typeof(AudioListener));
            camaraObjeto.tag = "MainCamera";
            Camera camara = camaraObjeto.GetComponent<Camera>();
            camara.fieldOfView = 62f;
            camara.nearClipPlane = 0.3f;
            camara.allowHDR = true;
            UniversalAdditionalCameraData datos = camaraObjeto.AddComponent<UniversalAdditionalCameraData>();
            datos.renderPostProcessing = true;
            datos.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;

            GameObject luna = new GameObject("Luna", typeof(Light));
            luna.transform.rotation = Quaternion.Euler(48f, -28f, 0f);
            Light luz = luna.GetComponent<Light>();
            luz.type = LightType.Directional;
            luz.color = new Color(0.45f, 0.55f, 1f);
            luz.intensity = 0.25f;
            luz.shadows = LightShadows.None;

            GameObject volumenObjeto = new GameObject("VolumenInicio", typeof(Volume));
            Volume volumen = volumenObjeto.GetComponent<Volume>();
            volumen.isGlobal = true;
            volumen.sharedProfile = perfilVolumen;

            GameObject fondoObjeto = new GameObject("FondoCiudad3D");
            FondoCiudad3D fondo = fondoObjeto.AddComponent<FondoCiudad3D>();
            fondo.Configurar(AssetDatabase.LoadAssetAtPath<Material>(RutaLit), AssetDatabase.LoadAssetAtPath<Material>(RutaAditivo3D), camara);

            GameObject servicios = new GameObject("Servicios");
            servicios.AddComponent<ServicioOpciones>();
            servicios.AddComponent<ServicioGuardado>();
            GameObject gestorObjeto = new GameObject("GestorSonidos");
            gestorObjeto.transform.SetParent(servicios.transform, false);
            GestorSonidos gestor = gestorObjeto.AddComponent<GestorSonidos>();
            AsignarReferencia(gestor, "catalogo", AssetDatabase.LoadAssetAtPath<CatalogoSonidos>(RutaCatalogo));
            GameObject perfilObjeto = new GameObject("ServicioPerfil");
            perfilObjeto.transform.SetParent(servicios.transform, false);
            ServicioPerfil perfil = perfilObjeto.AddComponent<ServicioPerfil>();
            string[] guias = AssetDatabase.FindAssets("t:DatosPerfil");
            if (guias.Length > 0)
            {
                AsignarReferencia(perfil, "datos", AssetDatabase.LoadAssetAtPath<DatosPerfil>(AssetDatabase.GUIDToAssetPath(guias[0])));
            }
            else
            {
                Debug.LogError("[UI] CreadorEscenaInicio: no hay ningún DatosPerfil; la creación de personaje no funcionará.");
            }

            GameObject creacionPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Proyecto/UI/Titulo/PantallaCreacionPandilla.prefab");
            PantallaCreacionPandilla creacion = null;
            if (creacionPrefab != null)
            {
                GameObject instancia = (GameObject)PrefabUtility.InstantiatePrefab(creacionPrefab);
                creacion = instancia.GetComponent<PantallaCreacionPandilla>();
            }
            else
            {
                Debug.LogError("[UI] CreadorEscenaInicio: falta PantallaCreacionPandilla.prefab.");
            }

            GameObject menuObjeto = new GameObject("MenuInicio");
            MenuInicio menu = menuObjeto.AddComponent<MenuInicio>();
            AsignarReferencia(menu, "fondo", fondo);
            AsignarReferencia(menu, "creacion", creacion);

            Directory.CreateDirectory(Path.GetDirectoryName(RutaEscena));
            EditorSceneManager.SaveScene(escena, RutaEscena);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[UI] Escena de inicio creada en " + RutaEscena);
        }

        private static void AsignarReferencia(Object objetivo, string campo, Object valor)
        {
            SerializedObject so = new SerializedObject(objetivo);
            SerializedProperty propiedad = so.FindProperty(campo);
            if (propiedad == null)
            {
                Debug.LogError("[UI] CreadorEscenaInicio: " + objetivo.GetType().Name + " no tiene el campo " + campo);
                return;
            }
            propiedad.objectReferenceValue = valor;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void CrearMateriales()
        {
            Directory.CreateDirectory(CarpetaMateriales);
            Material aditivoUI = Cargar(RutaMaterialAditivoUI, "MechaCyberpunk/UI/Aditivo");
            aditivoUI.renderQueue = 3000;
            Material lit = Cargar(RutaLit, "Universal Render Pipeline/Lit");
            lit.EnableKeyword("_EMISSION");
            lit.SetColor("_EmissionColor", Color.black);
            lit.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            Material aditivo3D = Cargar(RutaAditivo3D, "MechaCyberpunk/Inicio/Aditivo3D");
            aditivo3D.renderQueue = 3000;
            EditorUtility.SetDirty(aditivoUI);
            EditorUtility.SetDirty(lit);
            EditorUtility.SetDirty(aditivo3D);
            AssetDatabase.SaveAssets();
        }

        private static Material Cargar(string ruta, string nombreShader)
        {
            Shader shader = Shader.Find(nombreShader);
            if (shader == null)
            {
                throw new FileNotFoundException("No se encontró el shader " + nombreShader);
            }
            Material material = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, ruta);
            }
            material.shader = shader;
            return material;
        }

        private static VolumeProfile CrearVolumen()
        {
            VolumeProfile perfil = AssetDatabase.LoadAssetAtPath<VolumeProfile>(RutaVolumen);
            if (perfil != null)
            {
                AssetDatabase.DeleteAsset(RutaVolumen);
            }
            perfil = ScriptableObject.CreateInstance<VolumeProfile>();
            perfil.name = "VolumenInicio";
            AssetDatabase.CreateAsset(perfil, RutaVolumen);

            Bloom bloom = Agregar<Bloom>(perfil);
            bloom.threshold.Override(0.85f);
            bloom.intensity.Override(1.35f);
            bloom.scatter.Override(0.72f);
            bloom.tint.Override(new Color(0.9f, 0.85f, 1f));

            Vignette vineta = Agregar<Vignette>(perfil);
            vineta.intensity.Override(0.32f);
            vineta.smoothness.Override(0.55f);

            ColorAdjustments colores = Agregar<ColorAdjustments>(perfil);
            colores.postExposure.Override(0.15f);
            colores.contrast.Override(14f);
            colores.saturation.Override(14f);

            // T-941: sin aberración cromática ni grano, el menú se ve nítido.

            Tonemapping tono = Agregar<Tonemapping>(perfil);
            tono.mode.Override(TonemappingMode.ACES);

            EditorUtility.SetDirty(perfil);
            AssetDatabase.SaveAssets();
            return perfil;
        }

        private static T Agregar<T>(VolumeProfile perfil) where T : VolumeComponent
        {
            T componente = perfil.Add<T>(true);
            AssetDatabase.AddObjectToAsset(componente, perfil);
            return componente;
        }
    }
}
