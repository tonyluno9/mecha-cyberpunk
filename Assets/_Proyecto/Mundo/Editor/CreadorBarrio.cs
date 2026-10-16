using System;
using System.Collections.Generic;
using System.IO;
using MechaCyberpunk.Core;
using MechaCyberpunk.Mundo.Datos;
using MechaCyberpunk.Mundo.Lugares;
using MechaCyberpunk.Mundo.Pruebas;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
using Random = System.Random;

namespace MechaCyberpunk.Mundo.Editor
{
    /// <summary>
    /// Crea los assets de T-040: materiales, cámara libre (prefab, config y controles), prefab del barrio greybox y las
    /// escenas Ciudad y Pruebas_Alan.
    /// AVISO: rehace el prefab Barrio y las escenas Ciudad y Pruebas_Alan desde cero (borra T-041..T-044).
    /// Orden: CreadorArena, CreadorBarrio, CreadorReloj, CreadorLugares, CreadorBar, CreadorPeleaClandestino; usa CreadorTodo.Crear.
    /// Batchmode: -executeMethod MechaCyberpunk.Mundo.Editor.CreadorBarrio.Crear
    /// </summary>
    public static class CreadorBarrio
    {
        private const string RutaMundo = "Assets/_Proyecto/Mundo";
        private const string RutaMateriales = RutaMundo + "/Materiales";
        private const string RutaPrefabBarrio = RutaMundo + "/Prefabs/Barrio.prefab";
        private const string RutaPrefabCamara = RutaMundo + "/Prefabs/CamaraLibre.prefab";
        private const string RutaConfig = RutaMundo + "/Datos/ConfigCamaraLibre_Pruebas.asset";
        private const string RutaControles = RutaMundo + "/Pruebas/ControlesCamaraLibre.inputactions";
        private const string RutaPrefabJaula = "Assets/_Proyecto/Arena/Prefabs/JaulaClandestino.prefab";
        private const string RutaEscenaCiudad = "Assets/_Proyecto/Escenas/Ciudad.unity";
        private const string RutaEscenaPruebas = "Assets/_Proyecto/Escenas/Pruebas_Alan.unity";

        // Trazado (m). 1 unidad = 1 m (D-014).
        private const float LadoManzana = 80f;
        private const float AnchoCalle = 14f;
        private const float AnchoBanqueta = 3f;
        private const float AltoBanqueta = 0.2f;
        private const float PasoManzana = LadoManzana + AnchoCalle;
        private const float MitadManzana = LadoManzana / 2f;
        private const float MitadInterior = MitadManzana - AnchoBanqueta;
        internal const float MitadBarrioX = PasoManzana + MitadManzana;       // 134
        internal const float MitadBarrioZ = PasoManzana / 2f + MitadManzana;  // 87
        private const float LoteMaximo = 26f;
        private const float AlturaMinima = 8f;
        private const float AlturaMaxima = 30f;
        private const float DistanciaEntrada = 1.5f;
        internal const float CallejonX0 = 7f;      // T-088: fondo cerrado del callejón (x local de Manzana_4)
        internal const float CallejonZ0 = -17f;
        internal const float CallejonZ1 = -7f;
        private const float DistanciaCochera = 12f;
        private const float DespejeCochera = 6f;
        private const float AnchoPuerta = 3f;
        private const float AltoPuerta = 3.2f;
        private const float GrosorFachada = 1f;
        private static readonly float[] DesplazamientosFarola = { -33f, -12f, 12f, 33f };

        private static readonly Vector2[] CentrosManzana =
        {
            new Vector2(-PasoManzana, PasoManzana / 2f), new Vector2(0f, PasoManzana / 2f), new Vector2(PasoManzana, PasoManzana / 2f),
            new Vector2(-PasoManzana, -PasoManzana / 2f), new Vector2(0f, -PasoManzana / 2f), new Vector2(PasoManzana, -PasoManzana / 2f)
        };

        private static readonly Dictionary<string, Material> materiales = new Dictionary<string, Material>();
        private static readonly List<Vector3> puntosLibres = new List<Vector3>();
        private static int numeroEdificio;
        private static int numeroFarola;

        [MenuItem("Mecha Cyberpunk/Crear T-040")]
        public static void Crear()
        {
            AsegurarCarpeta(RutaMateriales);
            AsegurarCarpeta(RutaMundo + "/Prefabs");
            AsegurarCarpeta(RutaMundo + "/Datos");
            AsegurarCarpeta(RutaMundo + "/Pruebas");
            CrearMateriales();
            AssetDatabase.SaveAssets();
            VestirFachadas.CrearAssets(); // T-081: texturas de ventanas y materiales Fachada_*
            CrearControles();
            CrearConfig();
            CrearPrefabCamara();
            CrearPrefabBarrio();
            AssetDatabase.Refresh();
            CrearEscenaCiudad();
            CrearEscenaPruebas();
            AssetDatabase.SaveAssets();
            Debug.Log("[Mundo] T-040 creada.");
        }

        // ---------------------------------------------------------------- materiales y assets

        private static void AsegurarCarpeta(string ruta)
        {
            if (!Directory.Exists(ruta))
            {
                Directory.CreateDirectory(ruta);
            }
        }

        private static void CrearMateriales()
        {
            materiales.Clear();
            Mat("Asfalto", new Color(0.04f, 0.045f, 0.055f), 0.85f); // mojado: oscuro y liso (T-079)
            Mat("Banqueta", new Color(0.36f, 0.37f, 0.4f), 0.1f);
            Mat("Marca", new Color(0.8f, 0.75f, 0.35f), 0.2f);
            Mat("Edif_A", new Color(0.22f, 0.24f, 0.29f), 0.1f);
            Mat("Edif_B", new Color(0.3f, 0.28f, 0.27f), 0.1f);
            Mat("Edif_C", new Color(0.18f, 0.21f, 0.28f), 0.1f);
            Mat("Edif_D", new Color(0.27f, 0.22f, 0.26f), 0.1f);
            Mat("Pobre_A", new Color(0.36f, 0.25f, 0.2f), 0f);
            Mat("Pobre_B", new Color(0.3f, 0.3f, 0.24f), 0f);
            Mat("Lamina", new Color(0.4f, 0.34f, 0.28f), 0.3f);
            Mat("Azotea", new Color(0.15f, 0.16f, 0.18f), 0.1f);
            Mat("Casa", new Color(0.45f, 0.3f, 0.2f), 0f);
            Mat("Clandestino", new Color(0.35f, 0.12f, 0.3f), 0.1f);
            Mat("Bar", new Color(0.1f, 0.35f, 0.38f), 0.1f);
            Mat("Casino", new Color(0.55f, 0.42f, 0.12f), 0.3f);
            Mat("Mercado", new Color(0.55f, 0.3f, 0.1f), 0f);
            Mat("PanelPuerta", new Color(0.05f, 0.05f, 0.06f), 0.2f);
            Mat("Letrero_Magenta", new Color(0.9f, 0.1f, 0.6f), 0.2f, new Color(3.2f, 0.2f, 1.9f)); // emisión HDR para el bloom (T-079)
            Mat("Letrero_Cian", new Color(0.1f, 0.8f, 0.9f), 0.2f, new Color(0.2f, 2.6f, 3f));
            Mat("Letrero_Naranja", new Color(0.95f, 0.5f, 0.1f), 0.2f, new Color(3f, 1.2f, 0.1f));
            Mat("Concreto", new Color(0.5f, 0.5f, 0.5f), 0.1f);
            Mat("ConcretoOscuro", new Color(0.3f, 0.3f, 0.32f), 0.1f);
            Mat("Valla", new Color(0.9f, 0.45f, 0.1f), 0.1f);
            Mat("RayaRoja", new Color(0.8f, 0.1f, 0.1f), 0.2f);
            Mat("RayaBlanca", new Color(0.9f, 0.9f, 0.9f), 0.2f);
            Mat("Maquina", new Color(0.8f, 0.65f, 0.1f), 0.2f);
            Mat("Tierra", new Color(0.3f, 0.2f, 0.12f), 0f);
            Mat("Farola", new Color(0.12f, 0.13f, 0.15f), 0.4f);
            Mat("Lampara", new Color(0.9f, 0.85f, 0.6f), 0.2f, new Color(0.2f, 0.18f, 0.08f));
            Mat("Carro_A", new Color(0.4f, 0.1f, 0.1f), 0.5f);
            Mat("Carro_B", new Color(0.1f, 0.2f, 0.4f), 0.5f);
            Mat("Carro_C", new Color(0.55f, 0.55f, 0.5f), 0.5f);
            Mat("Bote", new Color(0.15f, 0.3f, 0.2f), 0.2f);
            Mat("Reja", new Color(0.14f, 0.15f, 0.18f), 0.3f);
            Mat("Patrulla", new Color(0.07f, 0.09f, 0.16f), 0.5f);
            Mat("PatrullaBlanco", new Color(0.8f, 0.82f, 0.85f), 0.4f);
            Mat("TorretaRoja", new Color(0.9f, 0.05f, 0.05f), 0.3f, new Color(3f, 0.1f, 0.1f));
            Mat("TorretaAzul", new Color(0.05f, 0.15f, 0.95f), 0.3f, new Color(0.1f, 0.3f, 3f));
            Mat("AvisoCorp", new Color(0.04f, 0.04f, 0.06f), 0.2f);
        }

        private static Material Mat(string nombre, Color color, float suavidad, Color emision = default)
        {
            string ruta = RutaMateriales + "/Barrio_" + nombre + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, ruta);
            }
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", suavidad);
            if (emision.maxColorComponent > 0f)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emision);
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            }
            EditorUtility.SetDirty(material);
            materiales[nombre] = material;
            return material;
        }

        private static void CrearControles()
        {
            InputActionAsset asset = ScriptableObject.CreateInstance<InputActionAsset>();
            asset.name = "ControlesCamaraLibre";
            InputActionMap mapa = asset.AddActionMap("CamaraLibre");
            InputAction mover = mapa.AddAction("Mover", InputActionType.Value, expectedControlLayout: "Vector2");
            mover.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            mapa.AddAction("Mirar", InputActionType.Value, "<Mouse>/delta", expectedControlLayout: "Vector2");
            mapa.AddAction("Correr", InputActionType.Button, "<Keyboard>/leftShift");
            mapa.AddAction("AlternarVuelo", InputActionType.Button, "<Keyboard>/f");
            mapa.AddAction("Subir", InputActionType.Button, "<Keyboard>/space");
            mapa.AddAction("Bajar", InputActionType.Button, "<Keyboard>/leftCtrl");
            mapa.AddAction("Interactuar", InputActionType.Button, "<Keyboard>/e");
            // Acciones del reloj de prueba (T-041).
            mapa.AddAction("SaltarANoche", InputActionType.Button, "<Keyboard>/n");
            mapa.AddAction("SaltarAMediodia", InputActionType.Button, "<Keyboard>/m");
            mapa.AddAction("AlternarVelocidad", InputActionType.Button, "<Keyboard>/t");
            File.WriteAllText(RutaControles, asset.ToJson());
            Object.DestroyImmediate(asset);
            AssetDatabase.ImportAsset(RutaControles);
        }

        private static void CrearConfig()
        {
            if (AssetDatabase.LoadAssetAtPath<ConfigCamaraLibre>(RutaConfig) == null)
            {
                AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<ConfigCamaraLibre>(), RutaConfig);
            }
        }

        private static void CrearPrefabCamara()
        {
            GameObject raiz = new GameObject("CamaraLibre");
            CharacterController cc = raiz.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.35f;
            cc.center = new Vector3(0f, 0.9f, 0f);
            cc.stepOffset = 0.3f;
            cc.minMoveDistance = 0f;

            GameObject ojos = new GameObject("Camara");
            ojos.tag = "MainCamera";
            ojos.transform.SetParent(raiz.transform, false);
            ojos.transform.localPosition = new Vector3(0f, 1.75f, 0f);
            Camera camara = ojos.AddComponent<Camera>();
            camara.clearFlags = CameraClearFlags.SolidColor;
            camara.backgroundColor = new Color(0.02f, 0.03f, 0.06f);
            camara.fieldOfView = 75f;
            camara.nearClipPlane = 0.05f;
            camara.farClipPlane = 600f;
            ojos.AddComponent<AudioListener>();

            CamaraLibre libre = raiz.AddComponent<CamaraLibre>();
            SerializedObject so = new SerializedObject(libre);
            so.FindProperty("config").objectReferenceValue = AssetDatabase.LoadAssetAtPath<ConfigCamaraLibre>(RutaConfig);
            so.FindProperty("controles").objectReferenceValue = AssetDatabase.LoadAssetAtPath<InputActionAsset>(RutaControles);
            so.FindProperty("camara").objectReferenceValue = camara;
            so.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefabCamara);
            Object.DestroyImmediate(raiz);
        }

        // ---------------------------------------------------------------- piezas básicas

        internal static Material M(string nombre)
        {
            return materiales[nombre];
        }

        internal static GameObject Cubo(string nombre, Transform padre, Vector3 posicion, Vector3 escala, string material, bool conCollider = true)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = nombre;
            go.transform.SetParent(padre, false);
            go.transform.localPosition = posicion;
            go.transform.localScale = escala;
            go.GetComponent<MeshRenderer>().sharedMaterial = M(material);
            if (!conCollider)
            {
                Object.DestroyImmediate(go.GetComponent<Collider>());
            }
            return go;
        }

        internal static Transform Vacio(string nombre, Transform padre, Vector3 posicion, Quaternion rotacion)
        {
            GameObject go = new GameObject(nombre);
            go.transform.SetParent(padre, false);
            go.transform.localPosition = posicion;
            go.transform.localRotation = rotacion;
            return go.transform;
        }

        // ---------------------------------------------------------------- barrio

        private static void CrearPrefabBarrio()
        {
            numeroEdificio = 0;
            numeroFarola = 0;
            puntosLibres.Clear();
            VestirCalle.Preparar(puntosLibres);

            GameObject raiz = new GameObject("Barrio");
            Transform t = raiz.transform;

            CrearSuelo(t);
            for (int i = 0; i < CentrosManzana.Length; i++)
            {
                CrearManzana(t, i);
            }
            CrearFinales(t);
            CrearFarolas(t);
            CrearPuestosTacos(t);
            CrearAmbientacion(t);
            CrearPuntosAparicion(t);
            VestirCalle.Cables(t);
            VestirFachadas.Vestir(t); // T-081: locales, ventanas y cornisas en los lados visibles
            VestirLetreros.Vestir(t); // T-082: letreros de neón del kit en lugar de los cubos greybox

            PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefabBarrio);
            Object.DestroyImmediate(raiz);
        }

        private static void CrearSuelo(Transform padre)
        {
            Transform calles = Vacio("Calles", padre, Vector3.zero, Quaternion.identity);
            Cubo("Asfalto", calles, new Vector3(0f, -0.5f, 0f), new Vector3(2f * MitadBarrioX + 4f, 1f, 2f * MitadBarrioZ + 4f), "Asfalto");

            // Rayas centrales discontinuas, sin collider, salvo dentro de los cruces.
            Transform marcas = Vacio("Marcas", calles, Vector3.zero, Quaternion.identity);
            for (float x = -MitadBarrioX + 4f; x <= MitadBarrioX - 4f; x += 8f)
            {
                if (EsCruceX(x))
                {
                    continue;
                }
                Cubo("Raya", marcas, new Vector3(x, 0.01f, 0f), new Vector3(3f, 0.02f, 0.25f), "Marca", false);
            }
            foreach (float cx in new[] { -PasoManzana / 2f, PasoManzana / 2f })
            {
                for (float z = -MitadBarrioZ + 4f; z <= MitadBarrioZ - 4f; z += 8f)
                {
                    if (Mathf.Abs(z) < AnchoCalle)
                    {
                        continue;
                    }
                    Cubo("Raya", marcas, new Vector3(cx, 0.01f, z), new Vector3(0.25f, 0.02f, 3f), "Marca", false);
                }
            }
        }

        private static bool EsCruceX(float x)
        {
            return Mathf.Abs(Mathf.Abs(x) - PasoManzana / 2f) < AnchoCalle / 2f + 1.5f;
        }

        private static void CrearManzana(Transform padre, int indice)
        {
            Vector2 c = CentrosManzana[indice];
            Transform manzana = Vacio("Manzana_" + (indice + 1), padre, new Vector3(c.x, 0f, c.y), Quaternion.identity);
            Cubo("Banqueta", manzana, new Vector3(0f, AltoBanqueta / 2f, 0f), new Vector3(LadoManzana, AltoBanqueta, LadoManzana), "Banqueta");
            Transform edificios = Vacio("Edificios", manzana, Vector3.zero, Quaternion.identity);
            Random azar = new Random(1000 + indice);
            bool pobre = indice == 0 || indice == 3;
            float hMin = pobre ? AlturaMinima : 14f;
            float hMax = pobre ? 12f : AlturaMaxima;

            switch (indice)
            {
                case 1: // Manzana_2: clandestino al frente y el callejón del mercado negro
                    Lugar(manzana, "Lugar_Clandestino", new Vector2(0f, -24.67f), 24.67f, 24.67f, 16f, new Vector3(0f, 0f, -1f), "Clandestino", "Letrero_Magenta");
                    Lugar(manzana, "Lugar_MercadoNegro", new Vector2(-31f, -21f), 12f, 12f, AlturaMinima, new Vector3(1f, 0f, 0f), "Mercado", "Letrero_Naranja");
                    Rellenar(edificios, -37f, -25f, -37f, -27f, azar, hMin, hMax, pobre);
                    Rellenar(edificios, -37f, -25f, -15f, 37f, azar, hMin, hMax, pobre);
                    Rellenar(edificios, -25f, -19f, -2f, 37f, azar, hMin, hMax, pobre);
                    Rellenar(edificios, -19f, -12.33f, -37f, 37f, azar, hMin, hMax, pobre);
                    Rellenar(edificios, -12.33f, 37f, -12.33f, 37f, azar, hMin, hMax, pobre);
                    Rellenar(edificios, 12.33f, 37f, -37f, -12.33f, azar, hMin, hMax, pobre);
                    CrearPuestosCallejon(manzana);
                    break;
                case 4: // Manzana_5: el bar frente al clandestino
                    Lugar(manzana, "Lugar_Bar", new Vector2(0f, 24.67f), 24.67f, 24.67f, 12f, new Vector3(0f, 0f, 1f), "Bar", "Letrero_Cian");
                    Rellenar(edificios, -37f, -12.33f, -37f, 37f, azar, hMin, hMax, pobre);
                    Rellenar(edificios, -12.33f, 12.33f, -37f, 12.33f, azar, hMin, hMax, pobre);
                    Rellenar(edificios, 12.33f, 37f, -37f, 37f, azar, hMin, hMax, pobre);
                    break;
                case 2: // Manzana_3: el casino a una cuadra, mirando a la calle del oeste
                    Lugar(manzana, "Lugar_Casino", new Vector2(-21f, 0f), 32f, 32f, 28f, new Vector3(-1f, 0f, 0f), "Casino", "Letrero_Naranja");
                    Rellenar(edificios, -37f, -5f, -37f, -16f, azar, hMin, hMax, pobre);
                    Rellenar(edificios, -37f, -5f, 16f, 37f, azar, hMin, hMax, pobre);
                    Rellenar(edificios, -5f, 37f, -37f, 37f, azar, hMin, hMax, pobre);
                    break;
                case 3: // Manzana_4: la casa, en la zona pobre y cerca del fondo de la calle del oeste
                    Lugar(manzana, "Lugar_Casa", new Vector2(31f, -30f), 14f, 12f, 7f, new Vector3(1f, 0f, 0f), "Casa", "Letrero_Naranja");
                    // T-088: el callejón de la emboscada queda libre entre CallejonX0 y la banqueta del este, entre CallejonZ0 y CallejonZ1.
                    Rellenar(edificios, 25f, 37f, -23f, CallejonZ0, azar, hMin, hMax, pobre);
                    Rellenar(edificios, 25f, 37f, CallejonZ1, 37f, azar, hMin, hMax, pobre);
                    Rellenar(edificios, -37f, CallejonX0, -37f, 37f, azar, hMin, hMax, pobre);
                    Rellenar(edificios, CallejonX0, 25f, -37f, CallejonZ0, azar, hMin, hMax, pobre);
                    Rellenar(edificios, CallejonX0, 25f, CallejonZ1, 37f, azar, hMin, hMax, pobre);
                    break;
                default:
                    Rellenar(edificios, -MitadInterior, MitadInterior, -MitadInterior, MitadInterior, azar, hMin, hMax, pobre);
                    break;
            }
        }

        // Llena un rectángulo (coordenadas locales de la manzana) con edificios contiguos de lote chico.
        private static void Rellenar(Transform padre, float x0, float x1, float z0, float z1, Random azar, float hMin, float hMax, bool pobre)
        {
            int nx = Mathf.Max(1, Mathf.CeilToInt((x1 - x0) / LoteMaximo));
            int nz = Mathf.Max(1, Mathf.CeilToInt((z1 - z0) / LoteMaximo));
            float w = (x1 - x0) / nx;
            float d = (z1 - z0) / nz;
            for (int i = 0; i < nx; i++)
            {
                for (int j = 0; j < nz; j++)
                {
                    float altura = hMin + (float)azar.NextDouble() * (hMax - hMin);
                    EdificioPlano(padre, x0 + (i + 0.5f) * w, z0 + (j + 0.5f) * d, w, d, altura, pobre, azar);
                }
            }
        }

        private static void EdificioPlano(Transform padre, float cx, float cz, float w, float d, float altura, bool pobre, Random azar)
        {
            numeroEdificio++;
            string[] pool = pobre ? new[] { "Pobre_A", "Pobre_B" } : new[] { "Edif_A", "Edif_B", "Edif_C", "Edif_D" };
            string material = pool[azar.Next(pool.Length)];
            Transform edificio = Vacio("Edificio_" + numeroEdificio, padre, new Vector3(cx, AltoBanqueta, cz), Quaternion.identity);
            Cubo("Cuerpo", edificio, new Vector3(0f, altura / 2f, 0f), new Vector3(w, altura, d), material);

            float resto = AlturaMaxima - altura;
            float alturaPiso = altura;
            if (resto > 3f && azar.NextDouble() < 0.5)
            {
                float extra = Mathf.Min(resto, 3f + (float)azar.NextDouble() * 5f);
                Cubo("Piso2", edificio, new Vector3(0f, altura + extra / 2f, 0f), new Vector3(w * 0.6f, extra, d * 0.6f), material);
                alturaPiso += extra;
            }

            if (pobre)
            {
                GameObject lamina = Cubo("Lamina", edificio, new Vector3(0f, alturaPiso + 0.5f, 0f), new Vector3(w * 0.8f, 0.1f, d * 0.8f), "Lamina", false);
                lamina.transform.localRotation = Quaternion.Euler(6f + (float)azar.NextDouble() * 8f, 0f, 0f);
                VestirCalle.TinacoPobre(edificio, w, alturaPiso, new Random(8000 + numeroEdificio));
            }
            else
            {
                VestirCalle.AzoteaKit(edificio, w, d, alturaPiso, w * 0.25f, d * 0.25f, new Random(8000 + numeroEdificio));
            }

            if (azar.NextDouble() < 0.45)
            {
                string[] letreros = { "Letrero_Magenta", "Letrero_Cian", "Letrero_Naranja" };
                float alturaLetrero = 4f + (float)azar.NextDouble() * Mathf.Max(0f, Mathf.Min(altura - 6f, 6f));
                bool sobreX = azar.NextDouble() < 0.5;
                float signo = azar.NextDouble() < 0.5 ? 1f : -1f;
                Vector3 posicion = sobreX ? new Vector3(signo * (w / 2f + 0.15f), alturaLetrero, 0f) : new Vector3(0f, alturaLetrero, signo * (d / 2f + 0.15f));
                Vector3 escala = sobreX ? new Vector3(0.3f, 2f, Mathf.Min(d, 5f)) : new Vector3(Mathf.Min(w, 5f), 2f, 0.3f);
                Cubo("Letrero", edificio, posicion, escala, letreros[azar.Next(letreros.Length)], false);
            }
        }

        // Un lugar: edificio con hueco de puerta tapado por un panel, un letrero y una Entrada frente a la puerta.
        // El frente del edificio es +Z local y mira hacia 'frente' (en el espacio de la manzana).
        private static void Lugar(Transform manzana, string nombre, Vector2 centro, float ancho, float fondo, float altura, Vector3 frente, string material, string letrero)
        {
            Transform lugar = Vacio(nombre, manzana, new Vector3(centro.x, AltoBanqueta, centro.y), Quaternion.LookRotation(frente, Vector3.up));
            float zFrente = fondo / 2f;
            float lado = (ancho - AnchoPuerta) / 2f;
            Cubo("Cuerpo", lugar, new Vector3(0f, altura / 2f, -GrosorFachada / 2f), new Vector3(ancho, altura, fondo - GrosorFachada), material);
            Cubo("FachadaIzquierda", lugar, new Vector3(-(AnchoPuerta / 2f + lado / 2f), altura / 2f, zFrente - GrosorFachada / 2f), new Vector3(lado, altura, GrosorFachada), material);
            Cubo("FachadaDerecha", lugar, new Vector3(AnchoPuerta / 2f + lado / 2f, altura / 2f, zFrente - GrosorFachada / 2f), new Vector3(lado, altura, GrosorFachada), material);
            Cubo("Dintel", lugar, new Vector3(0f, AltoPuerta + (altura - AltoPuerta) / 2f, zFrente - GrosorFachada / 2f), new Vector3(AnchoPuerta, altura - AltoPuerta, GrosorFachada), material);
            Cubo("PanelPuerta", lugar, new Vector3(0f, AltoPuerta / 2f, zFrente - GrosorFachada / 2f), new Vector3(AnchoPuerta, AltoPuerta, 0.3f), "PanelPuerta");
            Cubo("Letrero", lugar, new Vector3(0f, AltoPuerta + 1.2f, zFrente + 0.15f), new Vector3(AnchoPuerta + 2f, 1.4f, 0.3f), letrero, false);
            VestirCalle.AzoteaKit(lugar, ancho, fondo, altura, ancho * 0.25f, -fondo * 0.2f, new Random(9000 + (int)(centro.x * 10f) + (int)(centro.y * 100f)));

            Transform entrada = Vacio("Entrada", lugar, new Vector3(0f, 0f, zFrente + DistanciaEntrada), Quaternion.identity);
            puntosLibres.Add(entrada.position);
            if (nombre == "Lugar_Casa")
            {
                // La cochera (T-078): el carro viejo se estaciona ahí; ni carros greybox ni botes a 8 m a su alrededor.
                for (float dz = -DespejeCochera; dz <= DespejeCochera; dz += DespejeCochera)
                {
                    puntosLibres.Add(entrada.position + new Vector3(0f, 0f, DistanciaCochera + dz));
                }
            }
        }

        private static void CrearPuestosCallejon(Transform manzana)
        {
            // Puestos del mercado negro a lo largo del callejón (T-080: puestos de tacos del kit), sin tapar la puerta del mercado.
            VestirCalle.PuestosCallejon(manzana);
        }

        // ---------------------------------------------------------------- finales

        private static void CrearFinales(Transform padre)
        {
            Transform finales = Vacio("Finales", padre, Vector3.zero, Quaternion.identity);
            CrearAutopista(finales);
            CreadorFinales.Crear(finales);
        }

        private static void CrearAutopista(Transform padre)
        {
            Transform t = Vacio("Final_Autopista", padre, Vector3.zero, Quaternion.identity);
            float largo = 2f * MitadBarrioX + 4f;
            Cubo("MuroContencion", t, new Vector3(0f, 2f, MitadBarrioZ + 1f), new Vector3(largo, 4f, 2f), "Concreto");
            Cubo("Calzada", t, new Vector3(0f, 9.75f, MitadBarrioZ + 12f), new Vector3(largo, 1.5f, 22f), "ConcretoOscuro");
            Cubo("BarandalSur", t, new Vector3(0f, 10.9f, MitadBarrioZ + 1.3f), new Vector3(largo, 0.8f, 0.3f), "Concreto");
            Cubo("BarandalNorte", t, new Vector3(0f, 10.9f, MitadBarrioZ + 22.7f), new Vector3(largo, 0.8f, 0.3f), "Concreto");
            for (float x = -130f; x <= 130f; x += 26f)
            {
                Cubo("Pilar", t, new Vector3(x, 4.5f, MitadBarrioZ + 9f), new Vector3(1.6f, 9f, 1.6f), "Concreto");
            }
        }

        // ---------------------------------------------------------------- farolas y ambientación

        private static void CrearFarolas(Transform padre)
        {
            Transform farolas = Vacio("Farolas", padre, Vector3.zero, Quaternion.identity);
            Vector2[] normales = { new Vector2(0f, 1f), new Vector2(0f, -1f), new Vector2(1f, 0f), new Vector2(-1f, 0f) };
            for (int i = 0; i < CentrosManzana.Length; i++)
            {
                Vector2 c = CentrosManzana[i];
                foreach (Vector2 n in normales)
                {
                    if (EsLadoExterior(c, n))
                    {
                        continue;
                    }
                    foreach (float desplazamiento in DesplazamientosFarola)
                    {
                        numeroFarola++;
                        if (VestirCalle.Farola(farolas, numeroFarola, c, i, n, desplazamiento) == null)
                        {
                            numeroFarola--; // cayó junto a una entrada: sin poste
                        }
                    }
                }
            }
        }

        private static bool EsLadoExterior(Vector2 centro, Vector2 normal)
        {
            Vector2 destino = centro + normal * (MitadManzana + AnchoCalle / 2f);
            return Mathf.Abs(destino.x) > MitadBarrioX - 1f || Mathf.Abs(destino.y) > MitadBarrioZ - 1f;
        }

        // Los puestos de tacos del kit: 2 en la banqueta del bar (manzana 5) y 1 en la del clandestino (manzana 2).
        private static void CrearPuestosTacos(Transform padre)
        {
            Transform grupo = VestirCalle.GrupoPuestos(padre);
            VestirCalle.PuestoBanqueta(grupo, CentrosManzana[4], new Vector2(0f, 1f), 20f, "Puesto_Tacos_Bar_1");
            VestirCalle.PuestoBanqueta(grupo, CentrosManzana[4], new Vector2(0f, 1f), -20f, "Puesto_Tacos_Bar_2");
            VestirCalle.PuestoBanqueta(grupo, CentrosManzana[1], new Vector2(0f, -1f), 20f, "Puesto_Tacos_Clandestino");
        }

        // Basureros del kit en la banqueta y carros estacionados junto a la acera, lejos de puertas y del callejón.
        private static void CrearAmbientacion(Transform padre)
        {
            Transform grupo = Vacio("Ambientacion", padre, Vector3.zero, Quaternion.identity);
            puntosLibres.Add(new Vector3(-22f, 0f, 8f)); // boca del callejón
            Vector2[] normales = { new Vector2(0f, 1f), new Vector2(0f, -1f), new Vector2(1f, 0f), new Vector2(-1f, 0f) };
            string[] carros = { "Carro_A", "Carro_B", "Carro_C" };
            Random azar = new Random(77);
            int numero = 0;
            for (int i = 0; i < CentrosManzana.Length; i++)
            {
                Vector2 c = CentrosManzana[i];
                foreach (Vector2 n in normales)
                {
                    if (EsLadoExterior(c, n))
                    {
                        continue;
                    }
                    Vector2 tangente = new Vector2(-n.y, n.x);
                    for (float desplazamiento = -34f; desplazamiento <= 34f; desplazamiento += 8.5f)
                    {
                        Vector2 sobreBanqueta = c + n * (MitadManzana - 0.8f) + tangente * desplazamiento;
                        Vector2 sobreCalle = c + n * (MitadManzana + 1.2f) + tangente * desplazamiento;
                        if (EstaCercaDeUnPunto(sobreBanqueta, 6f))
                        {
                            continue;
                        }
                        double tirada = azar.NextDouble();
                        numero++;
                        if (tirada < 0.3)
                        {
                            if (!VestirCalle.EstaOcupado(sobreBanqueta, 1.1f))
                            {
                                VestirCalle.Basurero(grupo, numero, c, n, desplazamiento, new Random(5000 + numero));
                            }
                        }
                        else if (tirada < 0.5)
                        {
                            bool alargadoEnX = Mathf.Abs(n.y) > 0f;
                            Vector3 escala = alargadoEnX ? new Vector3(4.2f, 1.4f, 1.8f) : new Vector3(1.8f, 1.4f, 4.2f);
                            Cubo("Carro_" + numero, grupo, new Vector3(sobreCalle.x, 0.7f, sobreCalle.y), escala, carros[azar.Next(carros.Length)]);
                        }
                    }
                }
            }
        }

        private static bool EstaCercaDeUnPunto(Vector2 punto, float radio)
        {
            foreach (Vector3 libre in puntosLibres)
            {
                if (Vector2.Distance(punto, new Vector2(libre.x, libre.z)) < radio)
                {
                    return true;
                }
            }
            return false;
        }

        // ---------------------------------------------------------------- puntos de aparición

        private static void CrearPuntosAparicion(Transform padre)
        {
            Transform grupo = Vacio("PuntosAparicion", padre, Vector3.zero, Quaternion.identity);
            Transform casa = padre.Find("Manzana_4/Lugar_Casa/Entrada");
            Transform clandestino = padre.Find("Manzana_2/Lugar_Clandestino/Entrada");
            Vector3 cochera = casa.position + new Vector3(0f, 0f, DistanciaCochera);
            CrearPunto(grupo, "Casa_Puerta", casa.position, casa.rotation, ModoJugador.APie);
            CrearPunto(grupo, "Casa_Cochera", cochera, casa.rotation, ModoJugador.Carro);
            CrearPunto(grupo, "Clandestino_Puerta", clandestino.position, clandestino.rotation, ModoJugador.APie);
        }

        private static void CrearPunto(Transform padre, string id, Vector3 posicion, Quaternion rotacion, ModoJugador modo)
        {
            Transform punto = Vacio(id, padre, Vector3.zero, Quaternion.identity);
            punto.position = posicion;
            punto.rotation = rotacion;
            PuntoAparicion componente = punto.gameObject.AddComponent<PuntoAparicion>();
            SerializedObject so = new SerializedObject(componente);
            so.FindProperty("id").stringValue = id;
            so.FindProperty("modo").enumValueIndex = (int)modo;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---------------------------------------------------------------- escenas

        private static void AplicarAmbienteNocturno()
        {
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.3f, 0.33f, 0.5f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.05f, 0.06f, 0.12f);
            RenderSettings.fogDensity = 0.004f;
        }

        private static void CrearLuna()
        {
            GameObject luna = new GameObject("LuzNoche");
            luna.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            Light luz = luna.AddComponent<Light>();
            luz.type = LightType.Directional;
            luz.color = new Color(0.55f, 0.65f, 1f);
            luz.intensity = 1.2f;
            luz.shadows = LightShadows.Soft;
        }

        private static GameObject InstanciarPrefab(string ruta)
        {
            return (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ruta));
        }

        private static void CrearEscenaCiudad()
        {
            Scene escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            InstanciarPrefab(RutaPrefabBarrio).transform.position = Vector3.zero;
            CrearLuna();
            AplicarAmbienteNocturno();
            EditorSceneManager.SaveScene(escena, RutaEscenaCiudad);
        }

        private static void CrearEscenaPruebas()
        {
            Scene escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            InstanciarPrefab(RutaPrefabBarrio).transform.position = Vector3.zero;

            // La cámara libre en la calle, frente a la casa y mirando a su puerta (la casa mira al +X).
            GameObject camara = InstanciarPrefab(RutaPrefabCamara);
            camara.transform.SetPositionAndRotation(new Vector3(-PasoManzana / 2f, 0.05f, -PasoManzana / 2f - 30f), Quaternion.Euler(0f, 270f, 0f));

            // La jaula del clandestino, lejos del barrio (a más de 100 m).
            GameObject jaula = InstanciarPrefab(RutaPrefabJaula);
            jaula.transform.position = new Vector3(400f, 0f, 0f);

            CrearLuna();
            AplicarAmbienteNocturno();
            EditorSceneManager.SaveScene(escena, RutaEscenaPruebas);
        }
    }
}
