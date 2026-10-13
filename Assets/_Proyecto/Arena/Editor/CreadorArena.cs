using System.IO;
using MechaCyberpunk.Arena.Jaula;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.Arena.Editor
{
    /// <summary>
    /// Crea los assets de T-020: materiales, prefab de la jaula del clandestino y las escenas Clandestino y Pruebas_Alan.
    /// AVISO: rehace Clandestino y Pruebas_Alan desde cero (borra jugador, flujo y lo que CreadorBarrio puso en Pruebas_Alan).
    /// Orden: CreadorArena, CreadorBarrio, CreadorReloj, CreadorLugares, CreadorBar, CreadorPeleaClandestino; usa CreadorTodo.Crear.
    /// Batchmode: -executeMethod MechaCyberpunk.Arena.Editor.CreadorArena.Crear
    /// </summary>
    public static class CreadorArena
    {
        private const string RutaArena = "Assets/_Proyecto/Arena";
        private const string RutaPrefab = RutaArena + "/Prefabs/JaulaClandestino.prefab";
        private const string RutaMateriales = RutaArena + "/Materiales";
        private const string RutaEscenaClandestino = "Assets/_Proyecto/Escenas/Clandestino.unity";
        private const string RutaEscenaPruebas = "Assets/_Proyecto/Escenas/Pruebas_Alan.unity";

        private const float LadoPiso = 24f;
        private const float MitadPiso = LadoPiso / 2f;
        private const float AlturaReja = 6.5f;
        private const float DistanciaInicios = 12f;
        private const float AlturaCamara = 3.5f;
        private static readonly Vector2 TamanoPantalla = new Vector2(8f, 4.5f);
        private static readonly Vector3 PosicionPantalla = new Vector3(0f, 9f, 12.8f);

        private static Material matPiso;
        private static Material matReja;
        private static Material matGradas;
        private static Material matPublico;
        private static Material matPantalla;

        [MenuItem("Mecha Cyberpunk/Crear T-020")]
        public static void Crear()
        {
            AsegurarCarpeta(RutaArena + "/Prefabs");
            AsegurarCarpeta(RutaMateriales);
            matPiso = CrearMaterial("Jaula_Piso", new Color(0.09f, 0.1f, 0.12f), 0.85f, Color.black);
            matReja = CrearMaterial("Jaula_Reja", new Color(0.18f, 0.2f, 0.24f), 0.4f, Color.black);
            matGradas = CrearMaterial("Jaula_Gradas", new Color(0.07f, 0.07f, 0.09f), 0.1f, Color.black);
            matPublico = CrearMaterial("Jaula_Publico", new Color(0.2f, 0.12f, 0.16f), 0f, Color.black);
            matPantalla = CrearMaterial("Jaula_Pantalla", new Color(0.02f, 0.03f, 0.04f), 0.5f, new Color(0.02f, 0.06f, 0.07f));
            AssetDatabase.SaveAssets();

            CrearPrefab();
            AssetDatabase.Refresh();
            CrearEscena(RutaEscenaClandestino);
            CrearEscena(RutaEscenaPruebas);
            AssetDatabase.SaveAssets();
            Debug.Log("[Arena] T-020 creada.");
        }

        private static void AsegurarCarpeta(string ruta)
        {
            if (!Directory.Exists(ruta))
            {
                Directory.CreateDirectory(ruta);
            }
        }

        private static Material CrearMaterial(string nombre, Color color, float suavidad, Color emision)
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
            if (emision.maxColorComponent > 0f)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emision);
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            }
            EditorUtility.SetDirty(material);
            return material;
        }

        private static GameObject Cubo(string nombre, Transform padre, Vector3 posicion, Vector3 escala, Material material, bool conCollider)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = nombre;
            go.transform.SetParent(padre, false);
            go.transform.localPosition = posicion;
            go.transform.localScale = escala;
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
            if (!conCollider)
            {
                Object.DestroyImmediate(go.GetComponent<Collider>());
            }
            return go;
        }

        private static Transform Vacio(string nombre, Transform padre, Vector3 posicion, Quaternion rotacion)
        {
            GameObject go = new GameObject(nombre);
            go.transform.SetParent(padre, false);
            go.transform.localPosition = posicion;
            go.transform.localRotation = rotacion;
            return go.transform;
        }

        private static void CrearPrefab()
        {
            GameObject raiz = new GameObject("JaulaClandestino");
            Transform t = raiz.transform;

            // Piso: cara superior en y = 0.
            GameObject piso = Cubo("Piso", t, new Vector3(0f, -0.5f, 0f), new Vector3(LadoPiso, 1f, LadoPiso), matPiso, true);

            Transform inicioJugador = Vacio("InicioJugador", t, new Vector3(0f, 0f, -DistanciaInicios / 2f), Quaternion.identity);
            Transform inicioRival = Vacio("InicioRival", t, new Vector3(0f, 0f, DistanciaInicios / 2f), Quaternion.Euler(0f, 180f, 0f));

            CrearReja(t);
            CrearGradas(t);
            Transform pantalla = CrearPantalla(t);
            CrearLuces(t);

            ArenaPelea arena = raiz.AddComponent<ArenaPelea>();
            SerializedObject so = new SerializedObject(arena);
            so.FindProperty("inicioJugador").objectReferenceValue = inicioJugador;
            so.FindProperty("inicioRival").objectReferenceValue = inicioRival;
            so.FindProperty("pantallaArena").objectReferenceValue = pantalla;
            so.FindProperty("piso").objectReferenceValue = piso.transform;
            so.FindProperty("tamanoPantalla").vector2Value = TamanoPantalla;
            so.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefab);
            Object.DestroyImmediate(raiz);
        }

        private static void CrearReja(Transform padre)
        {
            Transform reja = Vacio("Reja", padre, Vector3.zero, Quaternion.identity);

            // Colisiones: cuatro cajas gruesas que cubren todo el perímetro sin huecos (las esquinas se solapan).
            Transform colisiones = Vacio("ColisionReja", reja, Vector3.zero, Quaternion.identity);
            float centro = MitadPiso + 0.5f;
            float largo = LadoPiso + 2f;
            float yCentro = AlturaReja / 2f;
            CrearCajaColision(colisiones, "Norte", new Vector3(0f, yCentro, centro), new Vector3(largo, AlturaReja, 1f));
            CrearCajaColision(colisiones, "Sur", new Vector3(0f, yCentro, -centro), new Vector3(largo, AlturaReja, 1f));
            CrearCajaColision(colisiones, "Este", new Vector3(centro, yCentro, 0f), new Vector3(1f, AlturaReja, largo));
            CrearCajaColision(colisiones, "Oeste", new Vector3(-centro, yCentro, 0f), new Vector3(1f, AlturaReja, largo));

            // Visual: postes cada 4 m y barras horizontales, sin colliders.
            float[] alturasBarras = { 0.8f, 2.6f, 4.4f, 6.2f };
            for (int lado = 0; lado < 4; lado++)
            {
                Quaternion giro = Quaternion.Euler(0f, lado * 90f, 0f);
                Transform ladoT = Vacio("Lado" + lado, reja, Vector3.zero, giro);
                for (int i = 0; i <= 6; i++)
                {
                    float x = -MitadPiso + i * 4f;
                    Cubo("Poste", ladoT, new Vector3(x, AlturaReja / 2f, MitadPiso + 0.5f), new Vector3(0.3f, AlturaReja, 0.3f), matReja, false);
                }
                for (int i = 0; i < alturasBarras.Length; i++)
                {
                    Cubo("Barra", ladoT, new Vector3(0f, alturasBarras[i], MitadPiso + 0.5f), new Vector3(LadoPiso + 0.3f, 0.15f, 0.15f), matReja, false);
                }
            }
        }

        private static void CrearCajaColision(Transform padre, string nombre, Vector3 posicion, Vector3 tamano)
        {
            GameObject go = new GameObject("Colision" + nombre);
            go.transform.SetParent(padre, false);
            go.transform.localPosition = posicion;
            BoxCollider caja = go.AddComponent<BoxCollider>();
            caja.size = tamano;
        }

        private static void CrearGradas(Transform padre)
        {
            Transform gradas = Vacio("Gradas", padre, Vector3.zero, Quaternion.identity);
            const int escalones = 4;
            const float profundidad = 2f;
            const float inicio = MitadPiso + 2.5f;
            const float ancho = 28f;
            for (int lado = 0; lado < 4; lado++)
            {
                Transform ladoT = Vacio("Grada" + lado, gradas, Vector3.zero, Quaternion.Euler(0f, lado * 90f, 0f));
                for (int e = 0; e < escalones; e++)
                {
                    float altura = 1f + e;
                    float z = inicio + e * profundidad + profundidad / 2f;
                    Cubo("Escalon" + e, ladoT, new Vector3(0f, altura / 2f, z), new Vector3(ancho, altura, profundidad), matGradas, false);
                    for (int p = 0; p < 7; p++)
                    {
                        float x = -12f + p * 4f + (e % 2) * 1.5f;
                        GameObject persona = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                        persona.name = "Publico";
                        persona.transform.SetParent(ladoT, false);
                        persona.transform.localPosition = new Vector3(x, altura + 0.9f, z);
                        persona.transform.localScale = new Vector3(0.7f, 0.9f, 0.7f);
                        persona.GetComponent<MeshRenderer>().sharedMaterial = matPublico;
                        Object.DestroyImmediate(persona.GetComponent<Collider>());
                    }
                }
            }
        }

        private static Transform CrearPantalla(Transform padre)
        {
            // La pantalla se inclina hacia el centro de la jaula. Su +Z apunta de la jaula hacia la pantalla.
            Vector3 haciaPantalla = (PosicionPantalla - new Vector3(0f, AlturaCamara, 0f)).normalized;
            Quaternion rotacion = Quaternion.LookRotation(haciaPantalla, Vector3.up);

            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "Pantalla";
            quad.transform.SetParent(padre, false);
            quad.transform.localPosition = PosicionPantalla;
            quad.transform.localRotation = rotacion;
            quad.transform.localScale = new Vector3(TamanoPantalla.x, TamanoPantalla.y, 1f);
            quad.GetComponent<MeshRenderer>().sharedMaterial = matPantalla;
            Object.DestroyImmediate(quad.GetComponent<Collider>());

            return Vacio("PantallaArena", quad.transform.parent, PosicionPantalla, rotacion);
        }

        private static void CrearLuces(Transform padre)
        {
            Transform luces = Vacio("Luces", padre, Vector3.zero, Quaternion.identity);
            CrearLuzPunto(luces, "NeonAzul", new Vector3(-10f, 6f, -10f), new Color(0.1f, 0.35f, 1f));
            CrearLuzPunto(luces, "NeonMagenta", new Vector3(10f, 6f, -10f), new Color(1f, 0.1f, 0.7f));
            CrearLuzPunto(luces, "NeonNaranja", new Vector3(10f, 6f, 10f), new Color(1f, 0.45f, 0.1f));
            CrearLuzPunto(luces, "NeonTurquesa", new Vector3(-10f, 6f, 10f), new Color(0.1f, 1f, 0.85f));

            GameObject cenital = new GameObject("LuzCenital");
            cenital.transform.SetParent(luces, false);
            cenital.transform.localPosition = new Vector3(0f, 14f, 0f);
            cenital.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            Light luz = cenital.AddComponent<Light>();
            luz.type = LightType.Spot;
            luz.color = new Color(0.85f, 0.93f, 1f);
            luz.spotAngle = 100f;
            luz.innerSpotAngle = 60f;
            luz.range = 30f;
            luz.intensity = 450f;
            luz.shadows = LightShadows.None;
        }

        private static void CrearLuzPunto(Transform padre, string nombre, Vector3 posicion, Color color)
        {
            GameObject go = new GameObject(nombre);
            go.transform.SetParent(padre, false);
            go.transform.localPosition = posicion;
            Light luz = go.AddComponent<Light>();
            luz.type = LightType.Point;
            luz.color = color;
            luz.range = 24f;
            luz.intensity = 220f;
            luz.shadows = LightShadows.None;
        }

        private static void CrearEscena(string rutaEscena)
        {
            Scene escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject jaula = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab));
            jaula.transform.position = Vector3.zero;

            GameObject camara = new GameObject("CamaraVistaArena");
            camara.transform.position = new Vector3(0f, AlturaCamara, -DistanciaInicios / 2f);
            camara.transform.LookAt(Vector3.zero);
            Camera c = camara.AddComponent<Camera>();
            c.clearFlags = CameraClearFlags.SolidColor;
            c.backgroundColor = new Color(0.02f, 0.03f, 0.06f);
            c.fieldOfView = 85f;
            c.nearClipPlane = 0.1f;
            c.farClipPlane = 200f;

            // Ambiente bajo y niebla leve (Lighting > Environment de esta escena).
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.12f, 0.13f, 0.2f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.05f, 0.06f, 0.12f);
            RenderSettings.fogDensity = 0.012f;

            EditorSceneManager.SaveScene(escena, rutaEscena);
        }
    }
}
