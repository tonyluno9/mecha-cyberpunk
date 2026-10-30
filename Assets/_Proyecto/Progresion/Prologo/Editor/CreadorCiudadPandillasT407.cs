using System.Collections.Generic;
using System.IO;
using MechaCyberpunk.Core;
using MechaCyberpunk.Progresion.Editor;
using MechaCyberpunk.Progresion.Prologo.Pandillas;
using MechaCyberpunk.Progresion.Sonido;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace MechaCyberpunk.Progresion.Prologo.Editor
{
    /// <summary>
    /// T-407 del creador de la ciudad de pandillas: gestor de sonidos, silencio del ambiente viejo, puerta automática, vendedor y letrero del
    /// mercado negro, y el Tianguis (puestos, arco y mantas) bajo GTA_T407. Todo con primitivas: provisional hasta el arte de la LISTA 2.
    /// Los textos que ve el jugador están en las constantes de abajo para cambiarlos en un minuto (el paso 2 del plan los renombra).
    /// </summary>
    public static partial class CreadorCiudadPandillas
    {
        // Textos visibles (paso 2 del plan de la noche: se renombran aquí).
        private const string TextoLetreroMercado = "ARMAS";
        private const string TextoArcoTianguis = "EL TIANGUIS";
        private const string TextoVendedor = "VENDEDOR";

        private const string TipoAmbienteSonoro = "AmbienteSonoro";
        private const string CarpetaMateriales = "Assets/_Proyecto/Progresion/Prologo/Materiales";
        private const int PuestosObjetivo = 12;
        private const float LimiteTianguisX = 40f;
        private const float SeparacionPuestos = 7f;
        private const float PasoBusqueda = 1.5f;
        private const float MargenCalle = 2.6f;
        private const float AlturaArco = 5.5f;
        private const float XArco = -46.5f;
        private const float DistanciaPuertaLibre = 4.5f;

        private static readonly Color[] ColoresLona =
        {
            new Color(0.9f, 0.25f, 0.15f), new Color(0.15f, 0.7f, 0.75f), new Color(0.95f, 0.65f, 0.1f),
            new Color(0.85f, 0.15f, 0.65f), new Color(0.3f, 0.75f, 0.25f), new Color(0.35f, 0.3f, 0.85f),
        };

        private static void T407Interno()
        {
            GameObject anterior = BuscarRaiz(RaizT407);
            if (anterior != null)
            {
                Object.DestroyImmediate(anterior);
            }
            GameObject raiz = new GameObject(RaizT407);

            // 1. Silencio: el ambiente sonoro de Alan (lluvia, neón, tráfico y música de local) se apaga sin borrar nada.
            int apagados = 0;
            foreach (MonoBehaviour mb in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (mb != null && mb.GetType().Name == TipoAmbienteSonoro && mb.enabled)
                {
                    mb.enabled = false;
                    EditorUtility.SetDirty(mb);
                    apagados++;
                }
            }

            // 2. El gestor de sonidos con su catálogo (Ambiente y Música arrancan en 0).
            CreadorCatalogoSonidos.Crear();
            CatalogoSonidos catalogo = AssetDatabase.LoadAssetAtPath<CatalogoSonidos>(CreadorCatalogoSonidos.RutaCatalogo);
            GameObject objetoGestor = new GameObject("GestorSonidos");
            objetoGestor.transform.SetParent(raiz.transform, false);
            GestorSonidos gestor = objetoGestor.AddComponent<GestorSonidos>();
            SerializedObject so = new SerializedObject(gestor);
            so.FindProperty("catalogo").objectReferenceValue = catalogo;
            so.ApplyModifiedPropertiesWithoutUndo();

            // 3. Mercado negro: la puerta se abre sola al acercarse, un vendedor tras el mostrador y un letrero.
            Directory.CreateDirectory(CarpetaMateriales);
            Material neonMagenta = MaterialT407("T407_NeonMagenta", new Color(1f, 0.1f, 0.8f), new Color(1f, 0.1f, 0.8f) * 3f);
            Material neonNaranja = MaterialT407("T407_NeonNaranja", new Color(1f, 0.5f, 0.1f), new Color(1f, 0.5f, 0.1f) * 3f);
            Material madera = MaterialT407("T407_Madera", new Color(0.35f, 0.22f, 0.12f), Color.black);
            Material cajaMat = MaterialT407("T407_Caja", new Color(0.5f, 0.4f, 0.25f), Color.black);
            Material metal = MaterialT407("T407_Metal", new Color(0.18f, 0.18f, 0.22f), Color.black);
            Material ropa = MaterialT407("T407_Vendedor", new Color(0.2f, 0.45f, 0.5f), Color.black);
            Material piel = MaterialT407("T407_Piel", new Color(0.8f, 0.6f, 0.45f), Color.black);
            raiz.AddComponent<PuertaAutomatica>();
            ArmarMercado(raiz.transform, neonNaranja, ropa, piel, metal);

            // 4. El Tianguis: puestos, arco y mantas.
            int puestos = ArmarTianguis(raiz.transform, madera, cajaMat, metal, neonMagenta);
            Debug.Log("[Progresion] CreadorCiudadPandillas.T407: " + apagados + " AmbienteSonoro apagados, gestor con " + catalogo.Entradas.Count
                + " IDs, " + puestos + " puestos en el Tianguis.");
        }

        private static Material MaterialT407(string nombre, Color color, Color emision)
        {
            string ruta = CarpetaMateriales + "/" + nombre + ".mat";
            Material m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (m == null)
            {
                m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(m, ruta);
            }
            m.SetColor("_BaseColor", color);
            m.SetFloat("_Smoothness", 0.2f);
            if (emision.maxColorComponent > 0f)
            {
                m.EnableKeyword("_EMISSION");
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
                m.SetColor("_EmissionColor", emision);
            }
            EditorUtility.SetDirty(m);
            return m;
        }

        private static GameObject CajaT407(Transform padre, string nombre, Vector3 posicion, Vector3 escala, Material material, bool colision, Quaternion? giro = null)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = nombre;
            go.transform.SetParent(padre, false);
            go.transform.position = posicion;
            go.transform.rotation = giro ?? Quaternion.identity;
            go.transform.localScale = escala;
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
            if (!colision)
            {
                Object.DestroyImmediate(go.GetComponent<Collider>());
            }
            return go;
        }

        // Texto en el mundo con uGUI (Canvas World Space), que sí se tapa con lo que tiene delante. El frente del texto mira hacia el giro.
        private const float EscalaTextoMundo = 0.01f;
        private const float ProporcionLetra = 0.7f;

        private static Text TextoT407(Transform padre, string nombre, string texto, Vector3 posicion, Quaternion giro, float altoMetros, Color color, float anchoMetros)
        {
            GameObject go = new GameObject(nombre, typeof(Canvas));
            go.transform.SetParent(padre, false);
            go.transform.SetPositionAndRotation(posicion, giro);
            Canvas lienzo = go.GetComponent<Canvas>();
            lienzo.renderMode = RenderMode.WorldSpace;
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(anchoMetros, altoMetros) / EscalaTextoMundo;
            rt.localScale = Vector3.one * EscalaTextoMundo;
            GameObject hijo = new GameObject("Texto", typeof(Text));
            hijo.transform.SetParent(go.transform, false);
            RectTransform rh = hijo.GetComponent<RectTransform>();
            rh.anchorMin = Vector2.zero;
            rh.anchorMax = Vector2.one;
            rh.offsetMin = Vector2.zero;
            rh.offsetMax = Vector2.zero;
            Text t = hijo.GetComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.text = texto;
            t.alignment = TextAnchor.MiddleCenter;
            t.fontStyle = FontStyle.Bold;
            t.color = color;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.fontSize = Mathf.RoundToInt(altoMetros / EscalaTextoMundo * ProporcionLetra);
            t.raycastTarget = false;
            return t;
        }

        // Mercado negro: vendedor tras el mostrador y letrero sobre él. La dirección "hacia la puerta" sale de la puerta y el centro del lugar.
        private static void ArmarMercado(Transform raiz, Material neon, Material ropa, Material piel, Material metal)
        {
            Transform lugar = BuscarTransform("Lugar_MercadoNegro");
            Transform mostrador = null;
            foreach (MonoBehaviour mb in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (mb != null && mb.GetType().Name == "MostradorTienda" && mb.name.Contains("MercadoNegro"))
                {
                    mostrador = mb.transform;
                }
            }
            Transform puerta = null;
            if (lugar != null)
            {
                foreach (Transform t in lugar.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name == "Puerta")
                    {
                        puerta = t;
                    }
                }
            }
            if (lugar == null || mostrador == null || puerta == null)
            {
                Debug.LogWarning("[Progresion] CreadorCiudadPandillas.T407: sin lugar, puerta o mostrador del mercado negro.");
                return;
            }
            Vector3 haciaPuerta = puerta.position - lugar.position;
            haciaPuerta.y = 0f;
            haciaPuerta.Normalize();
            Quaternion mirandoPuerta = Quaternion.LookRotation(haciaPuerta, Vector3.up);
            Transform grupo = new GameObject("Mercado").transform;
            grupo.SetParent(raiz, false);

            Vector3 pies = mostrador.position - haciaPuerta * 1.1f;
            pies.y = mostrador.position.y;
            GameObject vendedor = new GameObject("Vendedor");
            vendedor.transform.SetParent(grupo, false);
            vendedor.transform.SetPositionAndRotation(pies, mirandoPuerta);
            CajaT407(vendedor.transform, "Cuerpo", pies + Vector3.up * 0.9f, new Vector3(0.6f, 1.0f, 0.4f), ropa, false);
            CajaT407(vendedor.transform, "Piernas", pies + Vector3.up * 0.4f, new Vector3(0.45f, 0.8f, 0.35f), metal, false);
            GameObject cabeza = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            cabeza.name = "Cabeza";
            cabeza.transform.SetParent(vendedor.transform, false);
            cabeza.transform.position = pies + Vector3.up * 1.62f;
            cabeza.transform.localScale = Vector3.one * 0.32f;
            cabeza.GetComponent<MeshRenderer>().sharedMaterial = piel;
            Object.DestroyImmediate(cabeza.GetComponent<Collider>());
            TextoT407(vendedor.transform, "Nombre", TextoVendedor, pies + Vector3.up * 2.05f, mirandoPuerta * Quaternion.Euler(0f, 180f, 0f), 0.22f, new Color(1f, 0.55f, 0.15f), 1.2f);

            // Rack de armas detrás del vendedor: tablero con la escopeta, la pistola y el subfusil a la vista.
            Vector3 lado = Vector3.Cross(Vector3.up, haciaPuerta).normalized;
            Vector3 tablero = pies - haciaPuerta * 0.9f;
            CajaT407(grupo, "RackTablero", tablero + Vector3.up * 1.6f, new Vector3(2.8f, 1.3f, 0.08f), metal, false, mirandoPuerta);
            CajaT407(grupo, "RackNeon", tablero + Vector3.up * 2.28f + haciaPuerta * 0.03f, new Vector3(2.8f, 0.05f, 0.05f), neon, false, mirandoPuerta);
            Material armaMat = MaterialT407("T407_Arma", new Color(0.6f, 0.62f, 0.68f), Color.black);
            float[] largos = { 1.1f, 0.35f, 0.7f, 1.1f, 0.7f };
            for (int i = 0; i < largos.Length; i++)
            {
                Vector3 centroArma = tablero + haciaPuerta * 0.08f + Vector3.up * (2.05f - i * 0.22f) + lado * (i % 2 == 0 ? -0.2f : 0.2f);
                CajaT407(grupo, "Arma_" + i, centroArma, new Vector3(largos[i], 0.07f, 0.06f), armaMat, false, mirandoPuerta * Quaternion.Euler(0f, 90f, 0f));
                CajaT407(grupo, "ArmaAgarre_" + i, centroArma - lado * (largos[i] * 0.2f * (i % 2 == 0 ? -1f : 1f)) + Vector3.down * 0.1f, new Vector3(0.1f, 0.2f, 0.06f), armaMat, false, mirandoPuerta * Quaternion.Euler(0f, 90f, 0f));
            }

            // El interior de Alan ya trae el letrero grande MERCADO NEGRO; aquí va uno chico de ARMAS en el frente del mostrador.
            Vector3 frente = mostrador.position + haciaPuerta * 0.62f + Vector3.up * 0.75f;
            CajaT407(grupo, "LetreroArmasFondo", frente, new Vector3(1.4f, 0.32f, 0.04f), metal, false, mirandoPuerta);
            TextoT407(grupo, "LetreroArmas", TextoLetreroMercado, frente + haciaPuerta * 0.03f, mirandoPuerta * Quaternion.Euler(0f, 180f, 0f), 0.26f, new Color(1f, 0.55f, 0.15f), 1.3f);
            CajaT407(grupo, "NeonLetrero", frente + Vector3.down * 0.2f, new Vector3(1.4f, 0.04f, 0.05f), neon, false, mirandoPuerta);
        }

        // Puestos a lo largo de la calle principal (z = 0), dentro de la zona del Tianguis, en las dos banquetas; el arco en el límite oeste.
        private static int ArmarTianguis(Transform raiz, Material madera, Material cajaMat, Material metal, Material neonMagenta)
        {
            Transform grupo = new GameObject("Tianguis").transform;
            grupo.SetParent(raiz, false);
            Transform puestos = new GameObject("Puestos").transform;
            puestos.SetParent(grupo, false);

            // Ancho de la calle: lo que mide un rayo desde el centro hasta las fachadas.
            float mitad = AnchoDeCalle(0f);
            float z = Mathf.Max(MargenCalle, mitad - MargenCalle);
            List<Vector3> puertas = new List<Vector3>();
            foreach (GameObject r in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
            {
                foreach (Transform t in r.GetComponentsInChildren<Transform>(false))
                {
                    if (t.name == "Puerta" || t.name == "Casa_Puerta")
                    {
                        puertas.Add(t.position);
                    }
                }
            }
            int n = 0;
            int lado = 1;
            float[] desvios = { 0f, -1.5f, -3f };
            for (float x = -LimiteTianguisX; x <= LimiteTianguisX && n < PuestosObjetivo; x += PasoBusqueda)
            {
                bool puesto = false;
                foreach (float desvio in desvios)
                {
                    Vector3 centro = new Vector3(x, 0.2f, lado * (z + desvio));
                    if (!PuestoLibre(centro, puertas))
                    {
                        continue;
                    }
                    n++;
                    CrearPuesto(puestos, n, centro, lado > 0 ? 180f : 0f, ColoresLona[n % ColoresLona.Length], madera, cajaMat, metal, neonMagenta);
                    puesto = true;
                    break;
                }
                if (puesto)
                {
                    lado = -lado;
                    x += SeparacionPuestos - PasoBusqueda;
                }
            }
            ArmarArco(grupo, mitad, metal, neonMagenta);
            ArmarMantas(grupo, mitad, neonMagenta);
            return n;
        }

        private static float AnchoDeCalle(float x)
        {
            float mejor = 10f;
            if (Physics.Raycast(new Vector3(x, 1.5f, 0f), Vector3.forward, out RaycastHit golpe, 40f))
            {
                mejor = golpe.distance;
            }
            if (Physics.Raycast(new Vector3(x, 1.5f, 0f), Vector3.back, out RaycastHit golpe2, 40f))
            {
                mejor = Mathf.Min(mejor, golpe2.distance);
            }
            return mejor;
        }

        private static bool PuestoLibre(Vector3 centro, List<Vector3> puertas)
        {
            foreach (Vector3 p in puertas)
            {
                Vector3 d = p - centro;
                d.y = 0f;
                if (d.magnitude < DistanciaPuertaLibre)
                {
                    return false;
                }
            }
            Collider[] choques = Physics.OverlapBox(centro + Vector3.up * 1.4f, new Vector3(1.8f, 1.0f, 1.2f), Quaternion.identity, ~0, QueryTriggerInteraction.Ignore);
            return choques.Length == 0;
        }

        private static void CrearPuesto(Transform padre, int numero, Vector3 centro, float giroY, Color colorLona, Material madera, Material cajaMat, Material metal, Material neon)
        {
            GameObject puesto = new GameObject("Puesto_" + numero);
            puesto.transform.SetParent(padre, false);
            puesto.transform.position = centro;
            Quaternion giro = Quaternion.Euler(0f, giroY, 0f);
            Vector3 d(float dx, float dy, float dz) => centro + giro * new Vector3(dx, dy, dz);
            Material lona = MaterialT407("T407_Lona" + (numero % ColoresLona.Length), colorLona, colorLona * 0.7f);
            CajaT407(puesto.transform, "Mesa", d(0f, 0.55f, 0f), new Vector3(2.4f, 0.9f, 0.9f), madera, true, giro);
            CajaT407(puesto.transform, "Caja1", d(-0.8f, 1.15f, 0f), new Vector3(0.5f, 0.4f, 0.5f), cajaMat, false, giro);
            CajaT407(puesto.transform, "Caja2", d(0.2f, 1.1f, 0.1f), new Vector3(0.4f, 0.3f, 0.4f), cajaMat, false, giro);
            CajaT407(puesto.transform, "Caja3", d(1.9f, 0.35f, -0.3f), new Vector3(0.6f, 0.6f, 0.6f), cajaMat, false, giro);
            foreach (float px in new[] { -1.35f, 1.35f })
            {
                CajaT407(puesto.transform, "Poste", d(px, 1.25f, -0.55f), new Vector3(0.08f, 2.1f, 0.08f), metal, false, giro);
            }
            CajaT407(puesto.transform, "Lona", d(0f, 2.3f, -0.05f), new Vector3(3.0f, 0.06f, 1.9f), lona, false, giro * Quaternion.Euler(28f, 0f, 0f));
            CajaT407(puesto.transform, "Faldon", d(0f, 1.95f, 0.78f), new Vector3(3.0f, 0.45f, 0.05f), lona, false, giro);
            CajaT407(puesto.transform, "TuboNeon", d(0f, 1.68f, 0.82f), new Vector3(2.6f, 0.05f, 0.05f), neon, false, giro);
            if (numero % 3 == 0)
            {
                GameObject luz = new GameObject("Luz");
                luz.transform.SetParent(puesto.transform, false);
                luz.transform.position = d(0f, 2.0f, 0.4f);
                Light l = luz.AddComponent<Light>();
                l.type = LightType.Point;
                l.color = colorLona;
                l.range = 8f;
                l.intensity = 3f;
                l.shadows = LightShadows.None;
            }
        }

        private static void ArmarArco(Transform grupo, float mitadCalle, Material metal, Material neon)
        {
            Transform arco = new GameObject("ArcoTianguis").transform;
            arco.SetParent(grupo, false);
            arco.position = new Vector3(XArco, 0.2f, 0f);
            float z = mitadCalle - 0.8f;
            foreach (float lado in new[] { -1f, 1f })
            {
                CajaT407(arco, "Pilar", new Vector3(XArco, 0.2f + AlturaArco / 2f, lado * z), new Vector3(0.6f, AlturaArco, 0.6f), metal, false);
            }
            CajaT407(arco, "Viga", new Vector3(XArco, 0.2f + AlturaArco, 0f), new Vector3(0.7f, 1.6f, 2f * z + 0.6f), metal, false);
            CajaT407(arco, "NeonArco", new Vector3(XArco + 0.4f, 0.2f + AlturaArco - 0.95f, 0f), new Vector3(0.06f, 0.08f, 2f * z), neon, false);
            foreach (float cara in new[] { 1f, -1f })
            {
                Quaternion giro = Quaternion.Euler(0f, cara > 0f ? -90f : 90f, 0f);
                TextoT407(arco, "Texto", TextoArcoTianguis, new Vector3(XArco + cara * 0.38f, 0.2f + AlturaArco, 0f), giro, 1.2f, new Color(1f, 0.25f, 0.9f), 8f);
            }
            Light luz = new GameObject("LuzArco").AddComponent<Light>();
            luz.transform.SetParent(arco, false);
            luz.transform.position = new Vector3(XArco + 2f, 0.2f + AlturaArco - 1f, 0f);
            luz.type = LightType.Point;
            luz.color = new Color(1f, 0.15f, 0.8f);
            luz.range = 12f;
            luz.intensity = 4f;
            luz.shadows = LightShadows.None;
        }

        // Mantas magenta de los Calaveras colgadas en las fachadas de las manzanas 2 y 5 que dan a la calle principal.
        private static void ArmarMantas(Transform grupo, float mitadCalle, Material neon)
        {
            Transform mantas = new GameObject("Mantas").transform;
            mantas.SetParent(grupo, false);
            Material tela = MaterialT407("T407_Manta", new Color(0.9f, 0.1f, 0.7f), new Color(0.9f, 0.1f, 0.7f) * 0.8f);
            foreach (float lado in new[] { 1f, -1f })
            {
                foreach (float x in new[] { -30f, -12f, 12f, 30f })
                {
                    Vector3 posicion = new Vector3(x, 4.2f, lado * (mitadCalle - 0.12f));
                    Quaternion giro = Quaternion.LookRotation(new Vector3(0f, 0f, -lado), Vector3.up);
                    CajaT407(mantas, "Manta", posicion, new Vector3(1.4f, 3f, 0.05f), tela, false, giro);
                    CajaT407(mantas, "MantaFranja", posicion + new Vector3(0f, 0f, -lado * 0.03f), new Vector3(0.25f, 3f, 0.05f), neon, false, giro);
                }
            }
        }
    }
}
