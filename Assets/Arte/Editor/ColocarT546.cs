using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-546 (plan de la noche 3-nov, paso 4): interiores armados con lo que hay, como sus referencias (docs/referencias-interiores), a escala de
    /// una persona de 1.8 m. Lo que no hay se deja como hueco limpio (sin cubos) y se anota en docs/interiores-faltan.md. Las texturas que faltaban
    /// (pósters, alfombra, papel picado y pizarrón) salen de Assets/Arte/Exteriores/Fuentes~/generar_interiores.py. Idempotente: todo lo nuevo cuelga
    /// de T546_Interiores; lo viejo que estorba (cubos de greybox) solo se esconde.
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ColocarT546.Colocar
    /// </summary>
    public static class ColocarT546
    {
        public const string NombreRaiz = "T546_Interiores";
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const string Texturas = "Assets/Arte/Interiores/Texturas/";
        private const string Materiales = "Assets/Arte/Interiores/Materiales/";
        private const string Taller = "Assets/Arte/TallerCasa/Prefabs/";
        private const string Calle = "Assets/Arte/Calle/Prefabs/";

        public static readonly Color Calido = new Color(1f, 0.74f, 0.45f);
        public static readonly Color Turquesa = new Color(0.25f, 1f, 0.92f);
        public static readonly Color Magenta = new Color(1f, 0.22f, 0.75f);
        public static readonly Color Dorado = new Color(1f, 0.78f, 0.4f);

        private static readonly StringBuilder Informe = new StringBuilder();
        private static Transform raiz;

        [MenuItem("Mecha Cyberpunk/T-546/Interiores")]
        public static void Colocar()
        {
            Informe.Clear();
            Directory.CreateDirectory(Materiales);
            EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
            GameObject vieja = GameObject.Find(NombreRaiz);
            if (vieja != null)
            {
                Object.DestroyImmediate(vieja);
            }
            raiz = new GameObject(NombreRaiz).transform;
            ArreglarEmisionLetreros();
            Habitacion();
            Consultorio();
            TallerTino();
            Casino();
            EntradaArmas();
            UnityEngine.SceneManagement.Scene escena = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(escena);
            EditorSceneManager.SaveScene(escena);
            AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/t546-colocar.txt", Informe.ToString());
            Debug.Log("[Arte] T-546 listo:\n" + Informe);
        }

        // ---------------------------------------------------------------- Habitación

        private static void Habitacion()
        {
            Transform casa = Buscar("Barrio/Manzana_4/Lugar_Casa");
            Transform grupo = Grupo("Habitacion");
            Transform interior = casa.Find("Interior");
            // Techo a 3 m (el cuarto medía 5.6 de alto): cuarto de una persona, no bodega.
            Caja(grupo, "TechoFalso", MatEscena("T506_Casa_Techo"), new Vector3(-64.8f, 3.25f, -77.05f), new Vector3(7.4f, 0.1f, 12.9f), true);
            foreach (string n in new[] { "Luces/TuboTaller1", "Luces/TuboTaller2", "Sala/Mesa", "Sala/PataMesa", "Sala/Banco1", "Sala/Banco2", "Sala/Cocineta",
                "Sala/Refrigerador", "Sala/Refrigerador_Visual_T250", "Sala/Mesa_Visual_T250", "Sala/H07_Sofa_Noche" })
            {
                Apagar(interior.Find(n));
            }
            foreach (Transform t in interior.Find("Sala").Cast<Transform>().Where(t => t.name.StartsWith("H08_EstufaTarja")).ToList())
            {
                Apagar(t);
            }
            foreach (string n in new[] { "LuzTaller1", "LuzTaller2", "LuzSala", "LuzEntrada" })
            {
                Transform l = interior.Find("Luces/" + n);
                if (l != null)
                {
                    l.position = new Vector3(l.position.x, 2.85f, l.position.z);
                    Light luz = l.GetComponent<Light>();
                    luz.intensity = Mathf.Min(luz.intensity, 1.4f);
                }
            }
            Transform cama = casa.Find("Prologo/CamaJugador");
            foreach (Renderer r in cama.GetComponentsInChildren<Renderer>())
            {
                r.enabled = false; // la cama de cubos: se queda su collider y la del modelo H02 es la que se ve
            }
            // La tele va sobre el banco de trabajo, que es ahora el escritorio.
            Transform tele = interior.Find("Sala/H06_TeleVieja_Noche");
            if (tele != null)
            {
                tele.SetPositionAndRotation(new Vector3(-68.05f, 1.02f, -73.5f), Quaternion.Euler(0f, 90f, 0f));
            }
            Poner(grupo, Taller + "H11_Buro.prefab", new Vector3(-68.05f, 0.2f, -80.75f), 90f);
            GameObject lampara = Poner(grupo, "Assets/Arte/Clandestino/Prefabs/A05_LamparaColgante.prefab", new Vector3(-64.8f, 3.2f, -79f), 0f, colgar: true);
            if (lampara != null)
            {
                Escalar(lampara, 0.4f); // la del clandestino es de bodega: aquí es un foco de cuarto
            }
            Luz(grupo, "FocoColgante", new Vector3(-64.8f, 2.6f, -79f), Calido, 4.5f, 8f);
            Luz(grupo, "FocoEscritorio", new Vector3(-66.8f, 2.4f, -73.5f), Calido, 2.2f, 5f);
            Luz(grupo, "Lampara_Buro", new Vector3(-68.05f, 1.2f, -80.75f), Calido, 1.2f, 2.5f);
            // La ventana a la calle: neón magenta y turquesa que entra.
            Luz(grupo, "NeonVentana_Magenta", new Vector3(-61.7f, 2.2f, -72.4f), Magenta, 2.2f, 4.5f);
            Luz(grupo, "NeonVentana_Turquesa", new Vector3(-61.7f, 2.2f, -81.6f), Turquesa, 2f, 4.5f);
            Cuadro(grupo, "Poster_1", "T546_Poster_1", new Vector3(-68.42f, 1.95f, -76.7f), Vector3.right, new Vector2(0.62f, 0.87f));
            Cuadro(grupo, "Poster_2", "T546_Poster_2", new Vector3(-68.42f, 1.9f, -75.6f), Vector3.right, new Vector2(0.55f, 0.77f));
            Cuadro(grupo, "Poster_3", "T546_Poster_3", new Vector3(-66.4f, 1.95f, -70.66f), Vector3.back, new Vector2(0.62f, 0.87f));
            Informe.AppendLine("Habitación: techo a 3 m, sin cocina ni sofá ni taller; cama, buró, tele en el escritorio, foco colgante, neón de la ventana y 3 pósters");
        }

        // ---------------------------------------------------------------- Consultorio del Doc (I+D+I)

        private static void Consultorio()
        {
            Transform grupo = Grupo("ConsultorioDoc");
            Transform clinica = Buscar("Barrio/Manzana_2/Lugar_MercadoNegro/Interior/Clinica");
            if (clinica != null)
            {
                foreach (string n in new[] { "Camilla_Base", "Camilla_Colchon", "Camilla_Almohada", "Clinica_Mostrador" })
                {
                    Apagar(clinica.Find(n));
                }
                Renderer mesa = clinica.Find("MostradorClinica/Mesa")?.GetComponent<Renderer>();
                if (mesa != null)
                {
                    mesa.enabled = false;
                }
            }
            // Camilla: la cama H02 angosta y más alta; monitores y servidor; puesto de implantes; repisa.
            GameObject camilla = Poner(grupo, Taller + "H02_Cama.prefab", new Vector3(-101.6f, 0.24f, -13.6f), 0f);
            if (camilla != null)
            {
                camilla.name = "Camilla";
                camilla.transform.localScale = new Vector3(0.75f, 1.15f, 1f);
            }
            Poner(grupo, Calle + "CY05_ServidorCallejero.prefab", new Vector3(-98.6f, 0.24f, -15.45f), 180f);
            Poner(grupo, Calle + "CY09_PuestoImplantes.prefab", new Vector3(-103.4f, 0.24f, -11.6f), -90f);
            Poner(grupo, Taller + "T08_RepisaTaller.prefab", new Vector3(-96.45f, 0.24f, -14.2f), 90f);
            Poner(grupo, Calle + "CY02_PantallaVertical.prefab", new Vector3(-96.3f, 1.1f, -11.6f), 90f);
            Luz(grupo, "Luz_Turquesa_Baja", new Vector3(-100.5f, 2.2f, -13.5f), Turquesa, 4f, 7f);
            Transform doc = Buscar("GTA_T417/Apoyo_JOEL");
            if (doc != null)
            {
                doc.SetPositionAndRotation(new Vector3(-100.3f, 0.24f, -13.4f), Quaternion.LookRotation(Vector3.left));
            }
            Informe.AppendLine("Consultorio: camilla (H02), servidor con monitores, puesto de implantes, repisa, pantalla, luz turquesa; el Doc junto a la camilla");
        }

        // ---------------------------------------------------------------- Taller de Tino (CIC)

        private static void TallerTino()
        {
            Transform grupo = Grupo("TallerTino");
            Transform cic = Buscar("GTA_T417/Lugar_CIC");
            if (cic != null)
            {
                foreach (Transform t in cic.Cast<Transform>().Where(t => t.GetComponent<MeshFilter>() != null && t.GetComponent<MeshFilter>().sharedMesh != null
                    && (t.GetComponent<MeshFilter>().sharedMesh.name == "Cube" || t.GetComponent<MeshFilter>().sharedMesh.name == "Cylinder")).ToList())
                {
                    t.GetComponent<Renderer>().enabled = false; // los colliders del mostrador se quedan
                }
            }
            Poner(grupo, Taller + "T01_ParedTaller.prefab", new Vector3(-73.3f, 0.24f, 15.75f), 0f);
            Poner(grupo, Taller + "T03_BancoTrabajo.prefab", new Vector3(-75.4f, 0.24f, 14.5f), 0f);
            Poner(grupo, Taller + "T03_BancoTrabajo.prefab", new Vector3(-73.3f, 0.24f, 12.6f), 0f);
            Poner(grupo, Taller + "T08_RepisaTaller.prefab", new Vector3(-76.95f, 0.24f, 12.8f), -90f);
            Poner(grupo, Taller + "T04_GabineteHerramientas.prefab", new Vector3(-70.1f, 0.24f, 15.3f), 0f);
            Poner(grupo, Taller + "T05_Generador.prefab", new Vector3(-76.4f, 0.24f, 15.3f), 0f);
            Poner(grupo, "Assets/Arte/Callejon/Prefabs/L04_Llantas.prefab", new Vector3(-70.2f, 0.24f, 11.1f), 30f);
            Poner(grupo, Calle + "C16_Tambo.prefab", new Vector3(-76.6f, 0.24f, 10.8f), 0f);
            Cuadro(grupo, "Pizarron", "T546_Pizarron", new Vector3(-69.32f, 1.95f, 13.2f), Vector3.left, new Vector2(1.9f, 1.2f));
            foreach (Vector3 p in new[] { new Vector3(-75f, 3.05f, 12.2f), new Vector3(-72f, 3.1f, 14.2f), new Vector3(-71.5f, 3f, 11.8f) })
            {
                GameObject dron = Poner(grupo, Calle + "CY07b_Dron.prefab", p, 25f, colgar: true);
                if (dron != null)
                {
                    Escalar(dron, 0.6f);
                }
            }
            Serie(grupo, new Vector3(-77.1f, 3.45f, 10.6f), new Vector3(-69.5f, 3.45f, 15.6f), 16);
            Serie(grupo, new Vector3(-77.1f, 3.4f, 15.6f), new Vector3(-69.5f, 3.4f, 10.6f), 16);
            Luz(grupo, "Luz_Serie_1", new Vector3(-75f, 3.1f, 12f), Calido, 1.6f, 5f);
            Luz(grupo, "Luz_Serie_2", new Vector3(-71.5f, 3.1f, 14f), Calido, 1.6f, 5f);
            Transform tino = Buscar("GTA_T417/Apoyo_TINO");
            if (tino != null)
            {
                tino.SetPositionAndRotation(new Vector3(-74.5f, 0.24f, 13.5f), Quaternion.LookRotation(Vector3.back));
            }
            Informe.AppendLine("Taller de Tino: tablero, 2 bancos, repisa, gabinete, generador, llantas, tambo, pizarrón, 3 drones colgando y series de focos; Tino junto al banco");
        }

        // ---------------------------------------------------------------- Casino VivaCoin

        private static void Casino()
        {
            Transform grupo = Grupo("Casino");
            Transform interior = Buscar("Barrio/Manzana_3/Lugar_Casino/Interior");
            Material alfombra = Material("T546_Alfombra", "T546_AlfombraCasino", false, new Vector2(8f, 10f));
            Renderer vieja = interior.Find("Revestimiento/Alfombra")?.GetComponent<Renderer>();
            if (vieja != null)
            {
                vieja.enabled = false;
            }
            GameObject piso = GameObject.CreatePrimitive(PrimitiveType.Quad);
            piso.name = "AlfombraEstampada";
            Object.DestroyImmediate(piso.GetComponent<Collider>());
            piso.transform.SetParent(grupo, false);
            piso.transform.SetPositionAndRotation(new Vector3(72f, 0.245f, 47f), Quaternion.Euler(90f, 0f, 0f));
            piso.transform.localScale = new Vector3(21.6f, 27.6f, 1f);
            piso.GetComponent<Renderer>().sharedMaterial = alfombra;
            // La alfombra brilla un poco con su propio estampado (como bajo las luces de sala de la referencia).
            alfombra.SetTexture("_EmissionMap", alfombra.GetTexture("_BaseMap"));
            alfombra.SetTextureScale("_EmissionMap", new Vector2(8f, 10f));
            alfombra.SetColor("_EmissionColor", new Color(0.7f, 0.6f, 0.6f));
            alfombra.SetKeyword(new UnityEngine.Rendering.LocalKeyword(alfombra.shader, "_EMISSION"), true);
            alfombra.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive; // con None URP quita _EMISSION al reimportar
            EditorUtility.SetDirty(alfombra);
            AssetDatabase.SaveAssetIfDirty(alfombra);

            // Tragamonedas en hileras (copias de adorno: la que cobra es la de siempre).
            GameObject tragamonedas = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Proyecto/Casino/Prefabs/Tragamonedas.prefab");
            int maquinas = 0;
            for (float x = 68.5f; x <= 81.5f; x += 1.6f)
            {
                maquinas += Maquina(grupo, tragamonedas, new Vector3(x, 0.2f, 33.75f), 0f) ? 1 : 0;
            }
            for (float x = 63.5f; x <= 81.5f; x += 1.6f)
            {
                if (x > 69.5f && x < 74.5f)
                {
                    continue; // la cajera
                }
                maquinas += Maquina(grupo, tragamonedas, new Vector3(x, 0.2f, 60.3f), 180f) ? 1 : 0;
            }
            GameObject barra = Poner(grupo, "Assets/_Proyecto/Mundo/Prefabs/Kit/Prop_Barra.prefab", new Vector3(81.6f, 0.2f, 51f), -90f);
            GameObject mesaHacker = Poner(grupo, "Assets/_Proyecto/Mundo/Prefabs/Kit/Prop_Mesa.prefab", new Vector3(80.2f, 0.2f, 40.5f), 0f);
            Material papel = Material("T546_PapelPicado", "T546_PapelPicado", true, new Vector2(4f, 1f));
            foreach (float z in new[] { 38.5f, 44f, 50f, 55.5f })
            {
                Tira(grupo, papel, new Vector3(72f, 4.75f, z), 21f, 1.1f);
            }
            foreach (Vector3 p in new[] { new Vector3(66f, 4.6f, 42f), new Vector3(78f, 4.6f, 42f), new Vector3(66f, 4.6f, 52f), new Vector3(78f, 4.6f, 52f) })
            {
                Luz(grupo, "Luz_Dorada", p, Dorado, 5f, 10f);
            }
            Luz(grupo, "Luz_Magenta", new Vector3(72f, 4.6f, 37f), Magenta, 4f, 10f);
            Luz(grupo, "Luz_Magenta", new Vector3(72f, 4.6f, 57f), Magenta, 4f, 10f);
            Transform hacker = Buscar("GTA_T417/Apoyo_SERGIO");
            if (hacker != null && mesaHacker != null)
            {
                hacker.SetPositionAndRotation(new Vector3(81.4f, 0.2f, 40.5f), Quaternion.LookRotation(Vector3.left));
            }
            Informe.AppendLine("Casino: alfombra estampada, " + maquinas + " tragamonedas en hileras, barra, mesa del Hacker al fondo, papel picado, luces doradas y magenta"
                + (barra == null ? " (FALTA Prop_Barra)" : ""));
        }

        private static bool Maquina(Transform grupo, GameObject prefab, Vector3 p, float giro)
        {
            if (prefab == null)
            {
                return false;
            }
            GameObject g = (GameObject)PrefabUtility.InstantiatePrefab(prefab, grupo);
            PrefabUtility.UnpackPrefabInstance(g, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            g.name = "Tragamonedas_Adorno";
            foreach (MonoBehaviour m in g.GetComponentsInChildren<MonoBehaviour>(true).ToList())
            {
                Object.DestroyImmediate(m);
            }
            foreach (Canvas c in g.GetComponentsInChildren<Canvas>(true).ToList())
            {
                Object.DestroyImmediate(c.gameObject);
            }
            foreach (Collider c in g.GetComponentsInChildren<Collider>(true).Where(c => c.isTrigger).ToList())
            {
                Object.DestroyImmediate(c);
            }
            foreach (Light l in g.GetComponentsInChildren<Light>(true).ToList())
            {
                // Sin su foco: con 18 copias se comen el límite de luces por objeto y la alfombra queda negra.
                Object.DestroyImmediate(l);
            }
            g.transform.SetPositionAndRotation(p, Quaternion.Euler(0f, giro, 0f));
            return true;
        }

        // ---------------------------------------------------------------- Entrada de armas de El Mercado

        private static void EntradaArmas()
        {
            Transform grupo = Grupo("EntradaArmasMercado");
            Transform interior = Buscar("Barrio/Manzana_2/Lugar_MercadoNegro/Interior");
            foreach (Transform t in interior.Find("Mobiliario").GetComponentsInChildren<Transform>().ToList())
            {
                MeshFilter f = t.GetComponent<MeshFilter>();
                if (f != null && f.sharedMesh != null && (f.sharedMesh.name == "Cube" || f.sharedMesh.name == "Cylinder"))
                {
                    t.GetComponent<Renderer>().enabled = false;
                }
            }
            Renderer mostrador = interior.Find("Servicios/MostradorMercadoNegro/Mesa")?.GetComponent<Renderer>();
            if (mostrador != null)
            {
                mostrador.enabled = false;
            }
            Poner(grupo, Taller + "T03_BancoTrabajo.prefab", new Vector3(-27.6f, 0.24f, 26f), 90f);
            Poner(grupo, Taller + "T01_ParedTaller.prefab", new Vector3(-32.5f, 0.24f, 30.65f), 0f);
            Poner(grupo, Taller + "T01_ParedTaller.prefab", new Vector3(-32.5f, 0.24f, 21.45f), 180f);
            Poner(grupo, Taller + "T08_RepisaTaller.prefab", new Vector3(-36.05f, 0.24f, 24.3f), -90f);
            Poner(grupo, Taller + "T08_RepisaTaller.prefab", new Vector3(-36.05f, 0.24f, 27.7f), -90f);
            Poner(grupo, Taller + "T04_GabineteHerramientas.prefab", new Vector3(-34.6f, 0.24f, 30.4f), 0f);
            Poner(grupo, Calle + "C17_Huacales.prefab", new Vector3(-35.4f, 0.24f, 22f), 15f);
            Poner(grupo, Calle + "C17_Huacales.prefab", new Vector3(-29.5f, 0.24f, 30.4f), -10f);
            Poner(grupo, Calle + "C16_Tambo.prefab", new Vector3(-26.9f, 0.24f, 30.3f), 0f);
            Poner(grupo, Calle + "C16_Tambo.prefab", new Vector3(-26.9f, 0.24f, 21.8f), 0f);
            GameObject reja = Poner(grupo, "Assets/Arte/Callejon/Prefabs/L05_RejaPorton.prefab", new Vector3(-26.3f, 0.24f, 29.9f), -90f);
            if (reja != null)
            {
                Escalar(reja, 0.45f);
            }
            Material papel = Material("T546_PapelPicado", "T546_PapelPicado", true, new Vector2(2f, 1f));
            Tira(grupo, papel, new Vector3(-31.3f, 3.85f, 23.6f), 10f, 0.9f);
            Tira(grupo, papel, new Vector3(-31.3f, 3.85f, 28.4f), 10f, 0.9f);
            Serie(grupo, new Vector3(-36.2f, 3.95f, 26f), new Vector3(-26.4f, 3.95f, 26f), 14);
            Luz(grupo, "Luz_Serie", new Vector3(-31.3f, 3.5f, 26f), Calido, 1.8f, 6f);
            Informe.AppendLine("Entrada de armas de El Mercado: mostrador (banco), tableros de herramientas en las paredes, repisas, gabinete, huacales, tambos, reja, papel picado y serie de focos");
        }

        // ---------------------------------------------------------------- Utilidades

        private static Transform Buscar(string ruta)
        {
            int i = ruta.IndexOf('/');
            GameObject r = GameObject.Find(i < 0 ? ruta : ruta.Substring(0, i));
            return r == null ? null : i < 0 ? r.transform : r.transform.Find(ruta.Substring(i + 1));
        }

        private static Transform Grupo(string nombre)
        {
            Transform g = new GameObject(nombre).transform;
            g.SetParent(raiz, false);
            return g;
        }

        private static void Apagar(Transform t)
        {
            if (t != null)
            {
                t.gameObject.SetActive(false);
            }
        }

        private static GameObject Poner(Transform padre, string ruta, Vector3 pos, float giro, bool colgar = false)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
            if (prefab == null)
            {
                Informe.AppendLine("  FALTA " + ruta);
                return null;
            }
            GameObject g = (GameObject)PrefabUtility.InstantiatePrefab(prefab, padre);
            g.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, giro, 0f));
            if (colgar)
            {
                // Lo que cuelga del techo: su parte de arriba a la altura pedida.
                Bounds b = ArteT250.Limites(g);
                g.transform.position += Vector3.up * (pos.y - b.max.y);
                foreach (Collider c in g.GetComponentsInChildren<Collider>())
                {
                    Object.DestroyImmediate(c);
                }
            }
            return g;
        }

        private static void Escalar(GameObject g, float factor)
        {
            Bounds antes = ArteT250.Limites(g);
            g.transform.localScale *= factor;
            Bounds despues = ArteT250.Limites(g);
            g.transform.position += Vector3.up * (antes.max.y - despues.max.y);
        }

        private static void Luz(Transform padre, string nombre, Vector3 pos, Color color, float intensidad, float alcance)
        {
            GameObject go = new GameObject(nombre);
            go.transform.SetParent(padre, false);
            go.transform.position = pos;
            Light l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = color;
            l.intensity = intensidad;
            l.range = alcance;
            l.shadows = LightShadows.None;
        }

        private static void Caja(Transform padre, string nombre, Material m, Vector3 centro, Vector3 tam, bool collider)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = nombre;
            go.transform.SetParent(padre, false);
            go.transform.position = centro;
            go.transform.localScale = tam;
            if (m != null)
            {
                go.GetComponent<Renderer>().sharedMaterial = m;
            }
            if (!collider)
            {
                Object.DestroyImmediate(go.GetComponent<Collider>());
            }
        }

        // Cuadro plano (póster o pizarrón) pegado al muro, mirando hacia 'frente'.
        private static void Cuadro(Transform padre, string nombre, string textura, Vector3 pos, Vector3 frente, Vector2 tam)
        {
            GameObject q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            q.name = nombre;
            Object.DestroyImmediate(q.GetComponent<Collider>());
            q.transform.SetParent(padre, false);
            q.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(-frente, Vector3.up));
            q.transform.localScale = new Vector3(tam.x, tam.y, 1f);
            Renderer r = q.GetComponent<Renderer>();
            r.sharedMaterial = Material("T546_" + nombre.Split('_')[0] + "_" + textura, textura, false, Vector2.one);
            r.shadowCastingMode = ShadowCastingMode.Off;
        }

        // Tira de papel picado a lo ancho (en X), con las dos caras.
        private static void Tira(Transform padre, Material m, Vector3 centro, float largo, float alto)
        {
            GameObject q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            q.name = "PapelPicado";
            Object.DestroyImmediate(q.GetComponent<Collider>());
            q.transform.SetParent(padre, false);
            q.transform.SetPositionAndRotation(centro, Quaternion.identity);
            q.transform.localScale = new Vector3(largo, alto, 1f);
            Renderer r = q.GetComponent<Renderer>();
            r.sharedMaterial = m;
            r.shadowCastingMode = ShadowCastingMode.Off;
        }

        // Serie de focos entre dos puntos, con su curva; los focos brillan (emisión) y no llevan luz cada uno (la ponen 1 o 2 luces cercanas).
        private static void Serie(Transform padre, Vector3 a, Vector3 b, int focos)
        {
            Transform serie = new GameObject("SerieFocos").transform;
            serie.SetParent(padre, false);
            Material foco = Material("T546_Foco", null, false, Vector2.one);
            foco.SetColor("_BaseColor", new Color(1f, 0.85f, 0.55f));
            foco.EnableKeyword("_EMISSION");
            foco.SetColor("_EmissionColor", new Color(1f, 0.75f, 0.4f) * 3f);
            EditorUtility.SetDirty(foco);
            for (int i = 0; i <= focos; i++)
            {
                float t = i / (float)focos;
                Vector3 p = Vector3.Lerp(a, b, t) + Vector3.down * (Mathf.Sin(t * Mathf.PI) * 0.25f);
                GameObject s = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                s.name = "Foco";
                Object.DestroyImmediate(s.GetComponent<Collider>());
                s.transform.SetParent(serie, false);
                s.transform.position = p;
                s.transform.localScale = Vector3.one * 0.07f;
                Renderer r = s.GetComponent<Renderer>();
                r.sharedMaterial = foco;
                r.shadowCastingMode = ShadowCastingMode.Off;
            }
        }

        private static Material Material(string nombre, string textura, bool recorte, Vector2 repeticion)
        {
            string ruta = Materiales + nombre + ".mat";
            Material m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (m == null)
            {
                m = new Material(Shader.Find(recorte ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(m, ruta);
            }
            if (textura != null)
            {
                string rt = Texturas + textura + ".png";
                TextureImporter ti = (TextureImporter)AssetImporter.GetAtPath(rt);
                if (ti != null && ti.wrapMode != TextureWrapMode.Repeat)
                {
                    ti.wrapMode = TextureWrapMode.Repeat;
                    ti.alphaIsTransparency = recorte;
                    ti.SaveAndReimport();
                }
                Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(rt);
                m.SetTexture("_BaseMap", tex);
                m.SetTextureScale("_BaseMap", repeticion);
            }
            m.SetColor("_BaseColor", Color.white);
            if (recorte)
            {
                // Papel picado: recorte por alfa y las dos caras.
                m.SetFloat("_AlphaClip", 1f);
                m.SetFloat("_Cutoff", 0.5f);
                m.EnableKeyword("_ALPHATEST_ON");
                m.SetFloat("_Cull", 0f);
                m.doubleSidedGI = true;
            }
            else if (m.HasProperty("_Smoothness"))
            {
                m.SetFloat("_Smoothness", 0.2f);
            }
            EditorUtility.SetDirty(m);
            AssetDatabase.SaveAssetIfDirty(m);
            return m;
        }

        /// <summary>
        /// Los letreros 3D L10-L14 (T-542) perdían _EMISSION al reimportarse: el postprocesador de URP solo la deja si el GI del material es
        /// emisivo. Se pone en tiempo real y se vuelve a prender la palabra.
        /// </summary>
        private static void ArreglarEmisionLetreros()
        {
            foreach (string codigo in new[] { "L10", "L11", "L12", "L13", "L14" })
            {
                Material m = AssetDatabase.LoadAssetAtPath<Material>("Assets/Arte/Exteriores/" + codigo + "/" + codigo + ".mat");
                if (m == null || m.GetTexture("_EmissionMap") == null)
                {
                    Informe.AppendLine("Sin emisión que arreglar en " + codigo);
                    continue;
                }
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                m.SetKeyword(new UnityEngine.Rendering.LocalKeyword(m.shader, "_EMISSION"), true);
                EditorUtility.SetDirty(m);
                AssetDatabase.SaveAssetIfDirty(m);
            }
        }

        private static Material MatEscena(string nombre)
        {
            return Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Select(r => r.sharedMaterial).FirstOrDefault(m => m != null && m.name == nombre);
        }
    }
}
