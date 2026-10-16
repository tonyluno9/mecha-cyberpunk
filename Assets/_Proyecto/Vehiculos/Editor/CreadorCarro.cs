using System.IO;
using MechaCyberpunk.Vehiculos.Carro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.Vehiculos.Editor
{
    /// <summary>
    /// Crea los assets del carro viejo (T-074): config, controles, materiales greybox y CarroViejo.prefab.
    /// Se puede correr varias veces: el config que ya existe no se pisa.
    /// </summary>
    public static class CreadorCarro
    {
        private const string RutaBase = "Assets/_Proyecto/Vehiculos";
        private const string RutaConfig = RutaBase + "/Datos/ConfigCarro_Viejo.asset";
        private const string RutaControles = RutaBase + "/Carro/ControlesCarro.inputactions";
        private const string RutaConfigCamara = RutaBase + "/Datos/ConfigCamaraCarro_Viejo.asset";
        private const string RutaPrefab = RutaBase + "/Prefabs/CarroViejo.prefab";
        private const string RutaMateriales = RutaBase + "/Materiales";
        private const string RutaMaterialFisico = RutaMateriales + "/Carro_SinFriccion.asset";

        private const float Largo = 4.2f;
        private const float Ancho = 1.8f;
        private const float AlturaPiso = 0.45f;
        private const float AlturaCarroceria = 0.55f;
        private const float AlturaCabina = 0.45f;
        private const float AnchoCabina = 1.5f;
        private const float LargoCabina = 2.2f;
        private const float DesfaseCabinaZ = -0.2f;
        private const float RadioRueda = 0.35f;
        private const float AnchoRueda = 0.24f;
        private const float ViaMitad = 0.78f;
        private const float EjeDelanteroZ = 1.35f;
        private const float EjeTraseroZ = -1.35f;
        // Asiento del conductor (izquierda); el punto de subida está 1 m afuera de la puerta (el costado está en 0.9 m).
        private const float AsientoX = 0.4f;
        private const float AsientoY = 0.75f;
        private const float AsientoZ = -0.1f;
        private const float PuntoSubidaX = 1.9f;
        private const float PlanoCercano = 0.1f;

        // Colocación en las escenas (T-078). La Entrada de la casa está a 1.5 m del borde de la banqueta, el carro queda a 1.2 m del borde.
        private const string NombreCarro = "CarroViejo";
        private const string NombreCamaraLibre = "CamaraLibre";
        private const string PuntoCochera = "Casa_Cochera";
        private const string PuntoPuerta = "Casa_Puerta";
        private const float BordeBanqueta = 1.5f;
        private const float SeparacionBorde = 1.2f;
        private const float AlturaColocado = 0.1f;
        private const float AlturaCamara = 0.05f;
        private static readonly string[] RutasEscenas = { "Assets/_Proyecto/Escenas/Ciudad.unity", "Assets/_Proyecto/Escenas/Pruebas_Alan.unity" };
        private const float DistanciaAlCarro = BordeBanqueta + SeparacionBorde + Ancho / 2f;

        [MenuItem("Mecha Cyberpunk/Crear carro (T-074)")]
        public static void Crear()
        {
            AsegurarCarpeta(RutaBase + "/Datos");
            AsegurarCarpeta(RutaBase + "/Carro");
            AsegurarCarpeta(RutaBase + "/Prefabs");
            AsegurarCarpeta(RutaMateriales);
            ConfigCarro config = CrearConfig();
            InputActionAsset controles = CrearControles();
            ConfigCamaraCarro configCamara = CrearConfigCamara();
            Material carroceria = Material("Carro_Carroceria", new Color(0.45f, 0.18f, 0.12f), 0.3f);
            Material cabina = Material("Carro_Cabina", new Color(0.2f, 0.25f, 0.3f), 0.5f);
            Material rueda = Material("Carro_Rueda", new Color(0.05f, 0.05f, 0.06f), 0.1f);
            PhysicsMaterial sinFriccion = CrearMaterialFisico();
            AssetDatabase.SaveAssets();
            CrearPrefab(config, configCamara, controles, carroceria, cabina, rueda, sinFriccion);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Vehiculos] T-074 creada.");
        }

        /// <summary>
        /// T-078: estaciona el carro viejo en Ciudad.unity y Pruebas_Alan.unity, en la calle junto a Casa_Cochera, mirando al norte,
        /// con su costado del conductor (PuntoSubida) del lado de la banqueta de la casa. En Pruebas_Alan, la cámara libre
        /// empieza en la puerta de la casa mirando al carro. Se puede correr varias veces: reemplaza el carro anterior.
        /// </summary>
        public static void ColocarEnEscenas()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab);
            if (prefab == null)
            {
                Crear();
                prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab);
            }
            foreach (string ruta in RutasEscenas)
            {
                Scene escena = EditorSceneManager.OpenScene(ruta, OpenSceneMode.Single);
                foreach (GameObject raiz in escena.GetRootGameObjects())
                {
                    if (raiz.name == NombreCarro)
                    {
                        Object.DestroyImmediate(raiz);
                    }
                }
                GameObject cochera = GameObject.Find(PuntoCochera);
                GameObject puerta = GameObject.Find(PuntoPuerta);
                if (cochera == null || puerta == null)
                {
                    throw new System.InvalidOperationException("Falta " + PuntoCochera + " o " + PuntoPuerta + " en " + ruta + "; corre CreadorBarrio antes.");
                }
                Vector3 haciaCalle = cochera.transform.rotation * Vector3.forward;
                Vector3 posicion = cochera.transform.position + haciaCalle * DistanciaAlCarro + Vector3.up * AlturaColocado;
                GameObject carro = (GameObject)PrefabUtility.InstantiatePrefab(prefab, escena);
                carro.transform.SetPositionAndRotation(posicion, Quaternion.identity);

                if (ruta.EndsWith("Pruebas_Alan.unity"))
                {
                    ColocarCamara(puerta.transform.position, posicion);
                }
                EditorSceneManager.MarkSceneDirty(escena);
                EditorSceneManager.SaveScene(escena);
            }
            Debug.Log("[Vehiculos] T-078: carro viejo estacionado frente a la casa.");
        }

        private static void ColocarCamara(Vector3 origen, Vector3 carro)
        {
            Transform camara = null;
            foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (t.name == NombreCamaraLibre && t.parent == null)
                {
                    camara = t;
                }
            }
            if (camara == null)
            {
                throw new System.InvalidOperationException("Pruebas_Alan no tiene " + NombreCamaraLibre + ".");
            }
            Vector3 hacia = carro - origen;
            hacia.y = 0f;
            camara.SetPositionAndRotation(origen + Vector3.up * AlturaCamara, Quaternion.LookRotation(hacia, Vector3.up));
        }

        private static void AsegurarCarpeta(string ruta)
        {
            if (!Directory.Exists(ruta))
            {
                Directory.CreateDirectory(ruta);
            }
        }

        private static ConfigCarro CrearConfig()
        {
            ConfigCarro config = AssetDatabase.LoadAssetAtPath<ConfigCarro>(RutaConfig);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<ConfigCarro>();
                AssetDatabase.CreateAsset(config, RutaConfig);
            }
            return config;
        }

        private static ConfigCamaraCarro CrearConfigCamara()
        {
            ConfigCamaraCarro config = AssetDatabase.LoadAssetAtPath<ConfigCamaraCarro>(RutaConfigCamara);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<ConfigCamaraCarro>();
                AssetDatabase.CreateAsset(config, RutaConfigCamara);
            }
            return config;
        }

        private static InputActionAsset CrearControles()
        {
            InputActionAsset asset = ScriptableObject.CreateInstance<InputActionAsset>();
            asset.name = "ControlesCarro";
            InputActionMap mapa = asset.AddActionMap("Carro");
            InputAction acelerar = mapa.AddAction("Acelerar", InputActionType.Button);
            acelerar.AddBinding("<Keyboard>/w");
            acelerar.AddBinding("<Keyboard>/upArrow");
            InputAction frenar = mapa.AddAction("Frenar", InputActionType.Button);
            frenar.AddBinding("<Keyboard>/s");
            frenar.AddBinding("<Keyboard>/downArrow");
            InputAction girar = mapa.AddAction("Girar", InputActionType.Value, expectedControlLayout: "Axis");
            girar.AddCompositeBinding("1DAxis")
                .With("Negative", "<Keyboard>/a")
                .With("Positive", "<Keyboard>/d");
            girar.AddCompositeBinding("1DAxis")
                .With("Negative", "<Keyboard>/leftArrow")
                .With("Positive", "<Keyboard>/rightArrow");
            mapa.AddAction("FrenoMano", InputActionType.Button, "<Keyboard>/space");
            mapa.AddAction("CambiarCamara", InputActionType.Button, "<Keyboard>/c");
            mapa.AddAction("Mirar", InputActionType.Value, "<Mouse>/delta", expectedControlLayout: "Vector2");
            File.WriteAllText(RutaControles, asset.ToJson());
            Object.DestroyImmediate(asset);
            AssetDatabase.ImportAsset(RutaControles);
            return AssetDatabase.LoadAssetAtPath<InputActionAsset>(RutaControles);
        }

        private static Material Material(string nombre, Color color, float suavidad)
        {
            string ruta = RutaMateriales + "/" + nombre + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, ruta);
            }
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", suavidad);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static PhysicsMaterial CrearMaterialFisico()
        {
            PhysicsMaterial material = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(RutaMaterialFisico);
            if (material == null)
            {
                material = new PhysicsMaterial("Carro_SinFriccion");
                AssetDatabase.CreateAsset(material, RutaMaterialFisico);
            }
            material.dynamicFriction = 0f;
            material.staticFriction = 0f;
            material.frictionCombine = PhysicsMaterialCombine.Minimum;
            material.bounciness = 0f;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static GameObject Caja(string nombre, Transform padre, Vector3 centro, Vector3 tamano, Material material,
            PhysicsMaterial fisico)
        {
            GameObject caja = GameObject.CreatePrimitive(PrimitiveType.Cube);
            caja.name = nombre;
            caja.transform.SetParent(padre, false);
            caja.transform.localPosition = centro;
            caja.transform.localScale = tamano;
            caja.GetComponent<Renderer>().sharedMaterial = material;
            caja.GetComponent<BoxCollider>().sharedMaterial = fisico;
            return caja;
        }

        private static Transform CrearRueda(string nombre, Transform padre, float x, float z, Material material)
        {
            GameObject eje = new GameObject(nombre);
            eje.transform.SetParent(padre, false);
            eje.transform.localPosition = new Vector3(x, RadioRueda, z);

            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            visual.name = "Visual";
            Object.DestroyImmediate(visual.GetComponent<Collider>());
            visual.transform.SetParent(eje.transform, false);
            visual.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            // El cilindro de Unity mide 2 m de alto y 1 m de diámetro.
            visual.transform.localScale = new Vector3(RadioRueda * 2f, AnchoRueda / 2f, RadioRueda * 2f);
            visual.GetComponent<Renderer>().sharedMaterial = material;
            return eje.transform;
        }

        private static void CrearPrefab(ConfigCarro config, ConfigCamaraCarro configCamara, InputActionAsset controles, Material carroceria, Material cabina,
            Material rueda, PhysicsMaterial fisico)
        {
            GameObject raiz = new GameObject("CarroViejo");
            Rigidbody cuerpo = raiz.AddComponent<Rigidbody>();
            cuerpo.mass = config.Masa;
            cuerpo.interpolation = RigidbodyInterpolation.Interpolate;
            cuerpo.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            Caja("Carroceria", raiz.transform, new Vector3(0f, AlturaPiso + AlturaCarroceria / 2f, 0f),
                new Vector3(Ancho, AlturaCarroceria, Largo), carroceria, fisico);
            Caja("Cabina", raiz.transform, new Vector3(0f, AlturaPiso + AlturaCarroceria + AlturaCabina / 2f, DesfaseCabinaZ),
                new Vector3(AnchoCabina, AlturaCabina, LargoCabina), cabina, fisico);

            Transform di = CrearRueda("Rueda_DI", raiz.transform, -ViaMitad, EjeDelanteroZ, rueda);
            Transform dd = CrearRueda("Rueda_DD", raiz.transform, ViaMitad, EjeDelanteroZ, rueda);
            Transform ti = CrearRueda("Rueda_TI", raiz.transform, -ViaMitad, EjeTraseroZ, rueda);
            Transform td = CrearRueda("Rueda_TD", raiz.transform, ViaMitad, EjeTraseroZ, rueda);

            Transform asiento = new GameObject("Asiento").transform;
            asiento.SetParent(raiz.transform, false);
            asiento.localPosition = new Vector3(-AsientoX, AsientoY, AsientoZ);
            Transform puntoSubida = new GameObject("PuntoSubida").transform;
            puntoSubida.SetParent(raiz.transform, false);
            puntoSubida.localPosition = new Vector3(-PuntoSubidaX, 0f, AsientoZ);

            GameObject camaraObjeto = new GameObject("CamaraCarro");
            camaraObjeto.transform.SetParent(raiz.transform, false);
            camaraObjeto.transform.localPosition = asiento.localPosition + Vector3.up * configCamara.AlturaOjos;
            CamaraCarro camaraCarro = camaraObjeto.AddComponent<CamaraCarro>();
            Camera camara = camaraObjeto.GetComponent<Camera>();
            camara.nearClipPlane = PlanoCercano;
            camara.enabled = false;
            camara.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            AudioListener oyente = camaraObjeto.AddComponent<AudioListener>();
            oyente.enabled = false;
            SerializedObject soCamara = new SerializedObject(camaraCarro);
            soCamara.FindProperty("config").objectReferenceValue = configCamara;
            soCamara.ApplyModifiedPropertiesWithoutUndo();

            CarroViejo carro = raiz.AddComponent<CarroViejo>();
            SerializedObject so = new SerializedObject(carro);
            so.FindProperty("asiento").objectReferenceValue = asiento;
            so.FindProperty("puntoSubida").objectReferenceValue = puntoSubida;
            so.FindProperty("camaraCarro").objectReferenceValue = camaraCarro;
            so.FindProperty("config").objectReferenceValue = config;
            so.FindProperty("controles").objectReferenceValue = controles;
            so.FindProperty("ruedaDelanteraIzquierda").objectReferenceValue = di;
            so.FindProperty("ruedaDelanteraDerecha").objectReferenceValue = dd;
            so.FindProperty("ruedaTraseraIzquierda").objectReferenceValue = ti;
            so.FindProperty("ruedaTraseraDerecha").objectReferenceValue = td;
            so.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefab);
            Object.DestroyImmediate(raiz);
        }
    }
}
