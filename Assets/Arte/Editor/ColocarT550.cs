using System.IO;
using System.Linq;
using System.Text;
using MechaCyberpunk.Core;
using MechaCyberpunk.Mundo.Lugares;
using MechaCyberpunk.Progresion.Prologo.Cuartos;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-550: sistema de cuartos aparte. Cada interior es un cuarto cerrado (piso, 4 paredes y techo con UV en metros) lejos de la ciudad, con su
    /// <see cref="CuartoInterior"/>, su ZonaInterior y dos <see cref="PuertaCuarto"/>. Aquí se arma la habitación (3 x 4 x 2.7 m) y se muda lo que
    /// vivía en el interior viejo de la casa (cama que guarda, espejo, taller del mecha). Idempotente. Los otros cuatro interiores se arman con
    /// <see cref="CrearCuarto"/> + <see cref="ConectarPuertas"/>.
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ColocarT550.Colocar
    /// </summary>
    public static class ColocarT550
    {
        public const string NombreRaiz = "T550_Cuartos";
        public const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        /// <summary>Los cuartos están en una fila a 1000 m de la ciudad, uno cada 20 m en X.</summary>
        public static readonly Vector3 OrigenCuartos = new Vector3(1000f, 0f, 1000f);
        public const float SeparacionCuartos = 20f;
        /// <summary>Capa de render (bit) de los cuartos: el sol y la luna de la ciudad no la iluminan (Light Layers de URP).</summary>
        public const int CapaRenderCuartos = 4;

        private const string Taller = "Assets/Arte/TallerCasa/Prefabs/";
        private const string Interiores = "Assets/Arte/Interiores/Prefabs/";
        private const string Materiales = "Assets/Arte/Interiores/Materiales/";
        private static readonly Color Calido = new Color(1f, 0.74f, 0.45f);
        private static readonly Color Turquesa = new Color(0.25f, 1f, 0.92f);
        private static readonly Color Magenta = new Color(1f, 0.22f, 0.75f);

        private static readonly StringBuilder Informe = new StringBuilder();

        /// <summary>Lo que devuelve armar un cuarto vacío: su raíz, su carpeta de luces y sus puntos.</summary>
        public sealed class Cuarto
        {
            public Transform Raiz;
            public Transform Luces;
            public Transform Aparicion;
            public Transform PuertaInterior;
            public CuartoInterior Datos;
            public Vector3 Medidas;
        }

        [MenuItem("Mecha Cyberpunk/T-550/Cuartos (habitación)")]
        public static void Colocar()
        {
            Informe.Clear();
            EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
            Object.DestroyImmediate(GameObject.Find(NombreRaiz));
            Object.DestroyImmediate(GameObject.Find("T546_Interiores/Habitacion"));
            Transform raiz = new GameObject(NombreRaiz).transform;
            Habitacion(raiz);
            AislarDelSol();
            UnityEngine.SceneManagement.Scene escena = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(escena);
            EditorSceneManager.SaveScene(escena);
            AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/t550-colocar.txt", Informe.ToString());
            Debug.Log("[Arte] T-550 listo:\n" + Informe);
        }

        // ---------------------------------------------------------------- Habitación

        private static void Habitacion(Transform raiz)
        {
            Transform casa = Buscar("Barrio/Manzana_4/Lugar_Casa");
            Cuarto c = CrearCuarto(raiz, "Cuarto_Habitacion", 0, new Vector3(3f, 2.7f, 4f), 1, 1, 1.0f, IdLugar.Casa);
            Transform r = c.Raiz;
            Material madera = Mat("T550_Madera", new Color(0.30f, 0.18f, 0.09f));

            // Puerta interior (en la pared sur, a la derecha de quien entra).
            Puerta(r, madera, new Vector3(1f, 0f, -2f), 0f);

            // Muebles. Los modelos I miran hacia -Z; 'Contra' los pega a la pared mirando hacia 'frente'.
            Pieza(r, "I01", new Vector3(-1.5f, 0f, -1.35f), Vector3.right, 0.02f);                 // ropero, pared izquierda cerca de la puerta
            GameObject cama = Poner(r, Taller + "H02_Cama.prefab", Vector3.zero, 0f);
            cama.transform.localScale = Vector3.one * 0.85f;
            cama.name = "H02_Cama";
            cama.transform.SetPositionAndRotation(r.position, Quaternion.Euler(0f, 270f, 0f));
            Bounds bc = ArteT250.Limites(cama);
            Vector3 centroCama = r.TransformPoint(new Vector3(-0.8f, 0f, 1.15f));   // pegada a la pared izquierda, cabecera hacia el fondo
            cama.transform.position += new Vector3(centroCama.x - bc.center.x, r.position.y - bc.min.y, centroCama.z - bc.center.z);
            Pieza(r, "I04", new Vector3(0.1f, 0.95f, 2f), Vector3.back, 0f);                        // ventana con barrotes, pared del fondo
            GameObject buro = Poner(r, Taller + "H11_Buro.prefab", new Vector3(0.22f, 0f, 1.78f), 0f);
            buro.name = "H11_Buro";
            Pieza(r, "I02", new Vector3(1.5f, 0f, 1.15f), Vector3.left, 0f);                        // escritorio con CRT, pared derecha
            Libre(r, "I03", new Vector3(0.3f, 0f, 1.15f), 270f);                                     // silla mirando al escritorio
            Libre(r, "I05", new Vector3(1.1f, 0f, 0.05f), 200f, 0.5f);          // mochila (el modelo mide 1 m: se achica a una mochila)                                    // mochila junto al escritorio

            // Pósters planos de T-546 en la pared (izquierda sobre la cama y fondo).
            Cuadro(r, "Poster_1", new Vector3(-1.49f, 1.75f, 0.9f), Vector3.right, new Vector2(0.62f, 0.87f));
            Cuadro(r, "Poster_2", new Vector3(-1.49f, 1.7f, 1.75f), Vector3.right, new Vector2(0.55f, 0.77f));
            Cuadro(r, "Poster_3", new Vector3(-1.1f, 1.8f, 1.99f), Vector3.back, new Vector2(0.62f, 0.87f));

            // Luz: foco cálido colgando del techo (A05 chica) + lámpara del buró + neón que entra por la ventana + brillo del CRT.
            GameObject foco = Poner(r, "Assets/Arte/Clandestino/Prefabs/A05_LamparaColgante.prefab", new Vector3(0f, 2.7f, 0.2f), 0f, colgar: true);
            Escalar(foco, 0.4f);
            foco.name = "FocoColgante";
            Foco(r, "FocoColgante_Luz", new Vector3(0f, 2.05f, 0.2f), Calido, 1.2f, 6f, c.Luces);
            Foco(r, "Relleno_Calido", new Vector3(0f, 1.3f, -1.2f), Calido, 0.5f, 5f, c.Luces);
            Foco(r, "Lampara_Buro", new Vector3(0.22f, 0.95f, 1.78f), Calido, 1.4f, 2.5f, c.Luces);
            Foco(r, "NeonVentana_Magenta", new Vector3(-0.25f, 1.7f, 2.4f), Magenta, 1.4f, 4f, c.Luces);
            Foco(r, "NeonVentana_Turquesa", new Vector3(0.55f, 1.7f, 2.4f), Turquesa, 1.3f, 4f, c.Luces);
            Foco(r, "BrilloCRT", new Vector3(1.0f, 1.1f, 1.15f), Turquesa, 0.9f, 2.2f, c.Luces);
            Lampara(r, new Vector3(0.22f, 0.62f, 1.78f));
            Tubo(r, madera, new Vector3(-1.46f, 1.35f, -1.96f), 2.7f);
            Tubo(r, madera, new Vector3(1.46f, 1.35f, 1.96f), 2.7f);

            Mudanza(casa, c);
            ConectarPuertas(c, casa);
            Informe.AppendLine("Habitación: 3 x 4 x 2.7 m en " + r.position + "; puerta interior en la pared sur; cama/espejo/taller mudados");
        }

        // ---------------------------------------------------------------- Sistema de cuartos (reutilizable)

        /// <summary>
        /// Arma un cuarto vacío y cerrado: piso, techo y 4 paredes (UV en metros, mosaico de ImportarT550.MetrosPorMosaico), colliders, ZonaInterior del lugar
        /// y su CuartoInterior. Origen del cuarto: OrigenCuartos + (indice * SeparacionCuartos, 0, 0). La puerta interior queda en la pared sur (z = -largo/2).
        /// </summary>
        public static Cuarto CrearCuarto(Transform padre, string nombre, int indice, Vector3 medidas, int mosaicoPared, int mosaicoTecho, float colorPiso, IdLugar lugar)
        {
            GameObject go = new GameObject(nombre);
            go.transform.SetParent(padre, false);
            go.transform.position = OrigenCuartos + Vector3.right * (indice * SeparacionCuartos);
            Transform r = go.transform;
            float a = medidas.x * 0.5f;
            float l = medidas.z * 0.5f;
            float h = medidas.y;
            Material pared = AssetDatabase.LoadAssetAtPath<Material>(ImportarT550.RutaMaterialPared(mosaicoPared));
            pared.SetColor("_BaseColor", new Color(0.62f, 0.58f, 0.55f)); // el mosaico X0n es muy claro: se oscurece para que la luz c�lida no lo queme
            EditorUtility.SetDirty(pared);
            AssetDatabase.SaveAssetIfDirty(pared);
            Material techo = Mat("T550_Techo_X0" + mosaicoTecho, new Color(0.55f, 0.5f, 0.45f), ImportarT550.RutaMaterialPared(mosaicoTecho));
            Material piso = Mat("T550_Piso_X0" + mosaicoPared, new Color(0.42f, 0.4f, 0.38f) * colorPiso, ImportarT550.RutaMaterialPared(mosaicoPared));
            Vector3 p(float x, float y, float z) => r.position + new Vector3(x, y, z);
            Superficie(r, "Piso", piso, p(-a, 0, -l), p(-a, 0, l), p(a, 0, l), p(a, 0, -l), Vector3.up, -0.1f);
            Superficie(r, "Techo", techo, p(-a, h, -l), p(a, h, -l), p(a, h, l), p(-a, h, l), Vector3.down, 0.1f);
            Superficie(r, "Pared_Fondo", pared, p(-a, 0, l), p(-a, h, l), p(a, h, l), p(a, 0, l), Vector3.back, 0.1f);
            Superficie(r, "Pared_Puerta", pared, p(a, 0, -l), p(a, h, -l), p(-a, h, -l), p(-a, 0, -l), Vector3.forward, -0.1f);
            Superficie(r, "Pared_Izquierda", pared, p(-a, 0, -l), p(-a, h, -l), p(-a, h, l), p(-a, 0, l), Vector3.right, -0.1f);
            Superficie(r, "Pared_Derecha", pared, p(a, 0, l), p(a, h, l), p(a, h, -l), p(a, 0, -l), Vector3.left, 0.1f);

            Transform luces = new GameObject("Luces").transform;
            luces.SetParent(r, false);
            Transform aparicion = new GameObject("Aparicion").transform;
            aparicion.SetParent(r, false);
            aparicion.localPosition = new Vector3(a - 0.5f, 0.05f, -l + 1.5f);
            aparicion.localRotation = Quaternion.identity; // +Z: hacia dentro del cuarto
            Transform salidaExterior = new GameObject("SalidaExterior").transform;
            salidaExterior.SetParent(r, false);
            CuartoInterior datos = r.gameObject.AddComponent<CuartoInterior>();
            SerializedObject so = new SerializedObject(datos);
            so.FindProperty("lugar").enumValueIndex = (int)lugar;
            so.FindProperty("aparicionInterior").objectReferenceValue = aparicion;
            so.FindProperty("salidaExterior").objectReferenceValue = salidaExterior;
            so.ApplyModifiedPropertiesWithoutUndo();

            // La zona cubre todo el cuarto: ahí LugarMundo.JugadorDentro es verdadero.
            GameObject zonaGo = new GameObject("ZonaInterior");
            zonaGo.transform.SetParent(r, false);
            zonaGo.transform.localPosition = new Vector3(0f, h * 0.5f, 0f);
            BoxCollider caja = zonaGo.AddComponent<BoxCollider>();
            caja.isTrigger = true;
            caja.size = new Vector3(medidas.x - 0.1f, h - 0.1f, medidas.z - 0.1f);
            zonaGo.AddComponent<ZonaInterior>();

            return new Cuarto { Raiz = r, Luces = luces, Aparicion = aparicion, Datos = datos, Medidas = medidas };
        }

        /// <summary>
        /// Conecta el cuarto con su lugar: la ZonaInterior apunta al LugarMundo y a sus luces; la salida exterior queda frente a la puerta de la calle,
        /// mirando a la calle; la puerta de afuera (trigger detrás de la hoja) y la interior (junto a la puerta del cuarto) cruzan con fundido.
        /// </summary>
        public static void ConectarPuertas(Cuarto c, Transform lugarCalle)
        {
            LugarMundo lugar = lugarCalle.GetComponent<LugarMundo>();
            Light[] luces = c.Luces.GetComponentsInChildren<Light>(true);
            SerializedObject zo = new SerializedObject(c.Raiz.Find("ZonaInterior").GetComponent<ZonaInterior>());
            zo.FindProperty("lugar").objectReferenceValue = lugar;
            SerializedProperty arr = zo.FindProperty("luces");
            arr.arraySize = luces.Length;
            for (int i = 0; i < luces.Length; i++)
            {
                arr.GetArrayElementAtIndex(i).objectReferenceValue = luces[i];
            }
            zo.ApplyModifiedPropertiesWithoutUndo();

            Transform entrada = lugarCalle.Find("Entrada");
            Transform hoja = lugarCalle.Find("Puerta/Hoja");
            Vector3 haciaLaCalle = entrada.position - hoja.position;
            haciaLaCalle.y = 0f;
            Transform salida = c.Raiz.Find("SalidaExterior");
            salida.SetPositionAndRotation(entrada.position, Quaternion.LookRotation(haciaLaCalle.normalized, Vector3.up));

            GameObject prefabPantallas = null;
            CamaJugadorRef(out prefabPantallas);

            // Puerta de afuera: detrás de la hoja, dentro del pasillo de la entrada.
            GameObject fuera = new GameObject("PuertaCuarto_" + lugar.name);
            fuera.transform.SetParent(c.Raiz.parent, false);
            fuera.transform.position = hoja.position - haciaLaCalle.normalized * 1.5f + Vector3.up * 1.2f;
            fuera.transform.rotation = Quaternion.LookRotation(haciaLaCalle.normalized, Vector3.up);
            BoxCollider cf = fuera.AddComponent<BoxCollider>();
            cf.isTrigger = true;
            cf.size = new Vector3(1.6f, 2.2f, 0.8f);
            ConfigurarPuerta(fuera.AddComponent<PuertaCuarto>(), c.Datos, false, prefabPantallas);

            // Puerta interior: justo delante de la puerta del cuarto, en la pared sur.
            GameObject dentro = new GameObject("PuertaCuarto_Interior");
            dentro.transform.SetParent(c.Raiz, false);
            dentro.transform.localPosition = new Vector3(c.Medidas.x * 0.5f - 0.5f, 1.1f, -c.Medidas.z * 0.5f + 0.3f);
            BoxCollider cd = dentro.AddComponent<BoxCollider>();
            cd.isTrigger = true;
            cd.size = new Vector3(1.0f, 2.2f, 0.5f);
            ConfigurarPuerta(dentro.AddComponent<PuertaCuarto>(), c.Datos, true, prefabPantallas);
            c.PuertaInterior = dentro.transform;
        }

        private static void ConfigurarPuerta(PuertaCuarto puerta, CuartoInterior datos, bool esSalida, GameObject prefabPantallas)
        {
            SerializedObject so = new SerializedObject(puerta);
            so.FindProperty("cuarto").objectReferenceValue = datos;
            so.FindProperty("esSalida").boolValue = esSalida;
            so.FindProperty("prefabPantallas").objectReferenceValue = prefabPantallas;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CamaJugadorRef(out GameObject prefabPantallas)
        {
            prefabPantallas = null;
            Component cama = Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(m => m != null && m.GetType().Name == "CamaJugador");
            if (cama != null)
            {
                prefabPantallas = new SerializedObject(cama).FindProperty("prefabPantallas").objectReferenceValue as GameObject;
            }
        }

        // ---------------------------------------------------------------- Mudanza del interior viejo

        // La cama que guarda, el espejo de creación y el taller del mecha vivían en el interior viejo: se mudan al cuarto (cama, espejo) o a la banqueta (mecha).
        private static void Mudanza(Transform casa, Cuarto c)
        {
            Transform r = c.Raiz;
            Component cama = Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None).First(m => m != null && m.GetType().Name == "CamaJugador");
            cama.transform.position = r.position + new Vector3(-0.45f, 0f, 0.05f);
            foreach (Collider col in cama.GetComponentsInChildren<Collider>(true))
            {
                col.enabled = false; // el collider de la cama ahora es el del modelo H02
            }
            // Donde despierta el jugador (y El Doc lo deja) ahora es junto a la cama del cuarto, mirando a la cama.
            GameObject despertar = GameObject.Find("Casa_Cama");
            if (despertar != null)
            {
                despertar.transform.SetPositionAndRotation(cama.transform.position, Quaternion.identity);
            }
            Transform zonaVieja = casa.Find("Interior/ZonaInterior");
            if (zonaVieja != null && !zonaVieja.gameObject.activeSelf)
            {
                return; // la mudanza ya se hizo (esto vuelve a armar el cuarto sin mover otra vez el espejo ni el taller)
            }
            Component espejo = Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None).First(m => m != null && m.GetType().Name == "EspejoJugador");
            // El marco queda 1.5 m al -X de la raíz: con -90° de giro queda 1.5 m al -Z, contra la pared sur.
            espejo.transform.SetPositionAndRotation(r.position + new Vector3(-0.2f, 0.2f - 0f, -2f + 1.5f + 0.05f), Quaternion.Euler(0f, espejo.transform.eulerAngles.y - 90f, 0f));

            // Mecha y banco del taller a la banqueta de la casa (el cuarto no cabe un mecha).
            Vector3 delta = new Vector3(13.5f, 0f, -16.5f);
            Transform interior = casa.Find("Interior");
            Mover(interior.Find("TallerMecha"), delta);
            Mover(interior.Find("Taller/Banco"), delta);
            Mover(interior.Find("Taller/Banco_Visual_T250"), delta);
            GameObject mecha = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects().FirstOrDefault(o => o.name == "MechaEstacionado");
            if (mecha != null)
            {
                Mover(mecha.transform, delta);
            }
            // El interior viejo ya no se pisa: su zona y sus luces se apagan.
            zonaVieja = interior.Find("ZonaInterior");
            if (zonaVieja != null)
            {
                zonaVieja.gameObject.SetActive(false);
            }
            Transform lucesViejas = interior.Find("Luces");
            if (lucesViejas != null)
            {
                lucesViejas.gameObject.SetActive(false);
            }
        }

        private static void Mover(Transform t, Vector3 delta)
        {
            if (t != null)
            {
                t.position += delta;
            }
        }

        // ---------------------------------------------------------------- Luz: el sol y la luna no entran a los cuartos

        // Light Layers de URP: los cuartos usan la capa de render 4 y las luces direccionales de la ciudad la excluyen.
        public static void AislarDelSol()
        {
            uint capa = 1u << CapaRenderCuartos;
            foreach (Renderer rend in GameObject.Find(NombreRaiz).GetComponentsInChildren<Renderer>(true))
            {
                rend.renderingLayerMask = capa;
            }
            foreach (Light luz in GameObject.Find(NombreRaiz).GetComponentsInChildren<Light>(true))
            {
                luz.GetUniversalAdditionalLightData().renderingLayers = capa;
            }
            foreach (Light luz in Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (luz.type == LightType.Directional)
                {
                    UniversalAdditionalLightData d = luz.GetUniversalAdditionalLightData();
                    d.renderingLayers = d.renderingLayers & ~capa;
                }
            }
        }

        // ---------------------------------------------------------------- Ayudas

        public static Transform Buscar(string ruta)
        {
            int i = ruta.IndexOf('/');
            GameObject r = GameObject.Find(i < 0 ? ruta : ruta.Substring(0, i));
            return r == null ? null : i < 0 ? r.transform : r.transform.Find(ruta.Substring(i + 1));
        }

        public static Material Mat(string nombre, Color color, string copiarTexturaDe = null)
        {
            string ruta = Materiales + nombre + ".mat";
            Material m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (m == null)
            {
                m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(m, ruta);
            }
            m.SetColor("_BaseColor", color);
            m.SetFloat("_Smoothness", 0.1f);
            if (copiarTexturaDe != null)
            {
                Material origen = AssetDatabase.LoadAssetAtPath<Material>(copiarTexturaDe);
                m.SetTexture("_BaseMap", origen.GetTexture("_BaseMap"));
                m.SetTextureScale("_BaseMap", origen.GetTextureScale("_BaseMap"));
            }
            EditorUtility.SetDirty(m);
            AssetDatabase.SaveAssetIfDirty(m);
            return m;
        }

        // Superficie plana con UV en metros, mirando hacia 'dentro'. Collider: caja delgada hacia el lado contrario ('grosor' con signo en la dirección de dentro).
        public static void Superficie(Transform padre, string nombre, Material m, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, Vector3 dentro, float grosor)
        {
            Mesh mesh = new Mesh { name = nombre };
            Vector3[] vs = { p0, p1, p2, p3 };
            Vector3 horiz = Vector3.Cross(Vector3.up, dentro);
            if (horiz.sqrMagnitude < 0.01f)
            {
                horiz = Vector3.right;
            }
            horiz.Normalize();
            Vector3 vert = Vector3.Cross(dentro, horiz).normalized;
            Vector2[] uvs = vs.Select(p => new Vector2(Vector3.Dot(p - p0, horiz), Vector3.Dot(p - p0, vert))).ToArray();
            Vector3 n = Vector3.Cross(p1 - p0, p2 - p0);
            int[] tris = Vector3.Dot(n, dentro) >= 0f ? new[] { 0, 1, 2, 0, 2, 3 } : new[] { 0, 2, 1, 0, 3, 2 };
            // Se guarda en local al padre.
            mesh.vertices = vs.Select(p => padre.InverseTransformPoint(p)).ToArray();
            mesh.uv = uvs;
            mesh.triangles = tris;
            mesh.normals = new[] { dentro, dentro, dentro, dentro }.Select(d => padre.InverseTransformDirection(d)).ToArray();
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            GameObject go = new GameObject(nombre);
            go.transform.SetParent(padre, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = m;
            BoxCollider caja = go.AddComponent<BoxCollider>();
            Bounds b = mesh.bounds;
            Vector3 loc = padre.InverseTransformDirection(dentro);
            Vector3 size = b.size;
            Vector3 eje = new Vector3(Mathf.Abs(loc.x), Mathf.Abs(loc.y), Mathf.Abs(loc.z));
            caja.size = new Vector3(eje.x > 0.5f ? 0.2f : size.x, eje.y > 0.5f ? 0.2f : size.y, eje.z > 0.5f ? 0.2f : size.z);
            caja.center = b.center - loc * 0.1f;
        }

        public static GameObject Poner(Transform padre, string ruta, Vector3 posLocal, float giro, bool colgar = false)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
            if (prefab == null)
            {
                Informe.AppendLine("  FALTA " + ruta);
                return null;
            }
            GameObject g = (GameObject)PrefabUtility.InstantiatePrefab(prefab, padre);
            g.transform.SetPositionAndRotation(padre.TransformPoint(posLocal), Quaternion.Euler(0f, giro, 0f));
            if (colgar)
            {
                Bounds b = ArteT250.Limites(g);
                g.transform.position += Vector3.up * (padre.TransformPoint(posLocal).y - b.max.y);
                foreach (Collider col in g.GetComponentsInChildren<Collider>())
                {
                    Object.DestroyImmediate(col);
                }
            }
            return g;
        }

        // Escala sin mover la parte de arriba (lo que cuelga del techo).
        public static void Escalar(GameObject g, float factor)
        {
            Bounds antes = ArteT250.Limites(g);
            g.transform.localScale *= factor;
            Bounds despues = ArteT250.Limites(g);
            g.transform.position += Vector3.up * (antes.max.y - despues.max.y);
        }

        // Pone un mueble I## contra la pared: 'sobre' es el punto de la pared a lo largo de la que va (el mueble se separa hacia 'frente' lo que mide de fondo), mirando hacia 'frente'.
        public static void Pieza(Transform r, string codigo, Vector3 sobre, Vector3 frente, float separacion)
        {
            GameObject g = Poner(r, Interiores + codigo + ".prefab", Vector3.zero, 0f);
            if (g == null)
            {
                return;
            }
            g.name = codigo;
            g.transform.rotation = Quaternion.LookRotation(-frente, Vector3.up);
            Bounds b = ArteT250.Limites(g);
            float fondo = Mathf.Abs(frente.x) > 0.5f ? b.size.x : b.size.z;
            Vector3 centro = r.TransformPoint(sobre) + frente * (fondo * 0.5f + separacion);
            g.transform.position = new Vector3(g.transform.position.x, r.position.y + sobre.y, g.transform.position.z);
            Bounds b2 = ArteT250.Limites(g);
            g.transform.position += new Vector3(centro.x - b2.center.x, 0f, centro.z - b2.center.z);
            Informe.AppendLine("  " + codigo + " en " + r.InverseTransformPoint(g.transform.position).ToString("F2") + " tam " + b2.size.ToString("F2"));
        }

        // Pone un mueble I## en el piso, con su centro en planta en 'centroLocal' y girado 'yaw' grados.
        public static void Libre(Transform r, string codigo, Vector3 centroLocal, float yaw, float escala = 1f)
        {
            GameObject g = Poner(r, Interiores + codigo + ".prefab", Vector3.zero, yaw);
            if (g == null)
            {
                return;
            }
            g.name = codigo;
            g.transform.localScale *= escala;
            Vector3 centro = r.TransformPoint(centroLocal);
            Bounds b = ArteT250.Limites(g);
            g.transform.position = new Vector3(centro.x - (b.center.x - g.transform.position.x), r.position.y + centroLocal.y, centro.z - (b.center.z - g.transform.position.z));
            Informe.AppendLine("  " + codigo + " en " + r.InverseTransformPoint(g.transform.position).ToString("F2") + " tam " + ArteT250.Limites(g).size.ToString("F2"));
        }

        public static void Foco(Transform r, string nombre, Vector3 posLocal, Color color, float intensidad, float alcance, Transform carpeta)
        {
            GameObject go = new GameObject(nombre);
            go.transform.SetParent(carpeta, false);
            go.transform.position = r.TransformPoint(posLocal);
            Light l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = color;
            l.intensity = intensidad;
            l.range = alcance;
            l.shadows = LightShadows.None;
        }

        public static void Lampara(Transform r, Vector3 posLocal)
        {
            GameObject cil = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cil.name = "Lampara_Buro_Pantalla";
            Object.DestroyImmediate(cil.GetComponent<Collider>());
            cil.transform.SetParent(r, false);
            cil.transform.position = r.TransformPoint(posLocal) + Vector3.up * 0.1f;
            cil.transform.localScale = new Vector3(0.16f, 0.1f, 0.16f);
            Material m = Mat("T550_LamparaEmisiva", new Color(1f, 0.85f, 0.55f));
            m.EnableKeyword("_EMISSION");
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            m.SetColor("_EmissionColor", new Color(1f, 0.7f, 0.35f) * 2.5f);
            EditorUtility.SetDirty(m);
            cil.GetComponent<Renderer>().sharedMaterial = m;
        }

        // Tubería vertical de esquina (como en la referencia).
        public static void Tubo(Transform r, Material m, Vector3 posLocal, float alto)
        {
            GameObject cil = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cil.name = "Tuberia";
            Object.DestroyImmediate(cil.GetComponent<Collider>());
            cil.transform.SetParent(r, false);
            cil.transform.position = r.TransformPoint(posLocal);
            cil.transform.localScale = new Vector3(0.07f, alto * 0.5f, 0.07f);
            cil.GetComponent<Renderer>().sharedMaterial = Mat("T550_Tuberia", new Color(0.12f, 0.12f, 0.13f));
        }

        // Puerta del cuarto: marco y hoja de madera pegados a la pared sur, hacia dentro del cuarto.
        public static void Puerta(Transform r, Material madera, Vector3 posLocal, float giro)
        {
            Transform p = new GameObject("PuertaInterior").transform;
            p.SetParent(r, false);
            p.position = r.TransformPoint(posLocal);
            Material hoja = Mat("T550_PuertaHoja", new Color(0.18f, 0.2f, 0.24f));
            Cubo(p, "Hoja", hoja, new Vector3(0f, 1.0f, 0.04f), new Vector3(0.9f, 2.0f, 0.06f));
            Cubo(p, "MarcoIzq", madera, new Vector3(-0.5f, 1.05f, 0.05f), new Vector3(0.1f, 2.1f, 0.12f));
            Cubo(p, "MarcoDer", madera, new Vector3(0.5f, 1.05f, 0.05f), new Vector3(0.1f, 2.1f, 0.12f));
            Cubo(p, "MarcoSup", madera, new Vector3(0f, 2.1f, 0.05f), new Vector3(1.1f, 0.1f, 0.12f));
            Cubo(p, "Perilla", Mat("T550_Perilla", new Color(0.8f, 0.7f, 0.3f)), new Vector3(0.32f, 1.0f, 0.1f), new Vector3(0.08f, 0.08f, 0.08f));
        }

        public static void Cubo(Transform padre, string nombre, Material m, Vector3 posLocal, Vector3 tam)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = nombre;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(padre, false);
            go.transform.localPosition = posLocal;
            go.transform.localScale = tam;
            go.GetComponent<Renderer>().sharedMaterial = m;
        }

        // Cuadro plano (póster) pegado a la pared, mirando hacia 'frente'.
        public static void Cuadro(Transform r, string nombre, Vector3 posLocal, Vector3 frente, Vector2 tam)
        {
            GameObject q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            q.name = nombre;
            Object.DestroyImmediate(q.GetComponent<Collider>());
            q.transform.SetParent(r, false);
            q.transform.SetPositionAndRotation(r.TransformPoint(posLocal), Quaternion.LookRotation(-frente, Vector3.up));
            q.transform.localScale = new Vector3(tam.x, tam.y, 1f);
            Renderer rend = q.GetComponent<Renderer>();
            rend.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(Materiales + "T546_Poster_T546_Poster_" + nombre.Substring(nombre.Length - 1) + ".mat");
            rend.shadowCastingMode = ShadowCastingMode.Off;
        }
    }
}
