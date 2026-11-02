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
    /// T-508 (pulido visual, punto 3: calles más llenas sin moverlas). Idempotente. Recorre las calles de la ruta de la demo (Suburbios, calle principal con
    /// El Tianguis, callejón, entrada de El Mercado y Neón Alto), busca las fachadas con rayos y les cuelga toldos de lona o lámina (varios colores sucios,
    /// con prioridad sobre las puertas), letreros salientes perpendiculares con texto en español (de neón o pintados), balcones con barandal en los pisos
    /// altos y macetas; en las banquetas pone macetas, tambos C16, huacales C17 y gente parada (platicando o recargada) con los visuales de T-520 y su
    /// animación de reposo. Todo es de adorno: sin colliders (no tapa caminos, puertas, personajes de la historia, puntos de aparición ni coberturas) y
    /// cuelga de <see cref="NombreRaiz"/>. Además cambia los charcos de colores sólidos de T-501 a T-507 por reflejos sutiles de forma irregular.
    /// </summary>
    public static class ColocarT508
    {
        public const string NombreRaiz = "T508_CallesLlenas";
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const string CarpetaTexturas = "Assets/Arte/Exteriores/Texturas";
        private const string CarpetaMateriales = "Assets/Arte/Exteriores/Materiales";
        private const string RutaShader = "MechaCyberpunk/MuroMundo";
        private const string RutaTexturaCharco = CarpetaTexturas + "/T508_CharcoSutil.png";
        private const string CarpetaVisuales = "Assets/_Proyecto/Personaje/Visual/Prefabs/";
        private const string CarpetaPersonajes = "Assets/Arte/Personajes/Materiales/";

        /// <summary>Gente que se coloca como máximo (el costo es de huesos), alto máximo de un charco en m y lado mayor máximo.</summary>
        public const int GenteMaxima = 48;
        public const float CharcoMaximo = 3.2f;
        public const float AlfaMaximoCharco = 0.5f;

        private sealed class Tramo
        {
            public string Zona;
            public Vector3 Origen;
            public Vector3 Dir;
            public float Largo;
            public int MaxGente;

            public Tramo(string zona, float x, float z, Vector3 dir, float largo, int maxGente)
            {
                Zona = zona;
                Origen = new Vector3(x, 0f, z);
                Dir = dir;
                Largo = largo;
                MaxGente = maxGente;
            }
        }

        private sealed class Muro
        {
            public Vector3 Punto;      // en la pared, a 1.4 m
            public Vector3 Normal;     // de la pared hacia la calle
            public Vector3 Tangente;   // a lo largo de la calle
            public float S;
            public bool Puerta;
            public bool Alto;          // hay pared hasta 8 m
            public string Zona;
            public float Distancia;    // de la línea central a la pared
        }

        private static readonly Tramo[] Tramos =
        {
            new Tramo("suburbios", -131f, 0f, Vector3.right, 84f, 9),
            new Tramo("tianguis", -47f, 0f, Vector3.right, 94f, 16),
            new Tramo("cruzada", -47f, 12f, Vector3.forward, 68f, 4),
            new Tramo("cruzada", -47f, -80f, Vector3.forward, 68f, 3),
            new Tramo("callejon", -87f, -59f, Vector3.right, 28f, 3),
            new Tramo("mercado", -22f, 11f, Vector3.forward, 27f, 6),
            new Tramo("neon", 47f, 8f, Vector3.forward, 78f, 8),
        };

        private static readonly string[] TextosNeon =
        {
            "FARMACIA", "TACOS", "CIBER", "ABARROTES", "TAQUERIA", "CANTINA", "IMPLANTES", "HOTEL", "LOTERIA", "ELOTES", "CAFE INTERNET", "REFACCIONES",
        };
        private static readonly string[] TextosPintados =
        {
            "TORTILLERIA", "PULQUERIA", "FONDA DOÑA LUPE".Replace("Ñ", "N"), "PAPELERIA", "FERRETERIA", "TAMALES", "MICHELADAS", "COCINA ECONOMICA", "DENTISTA", "MECANICO", "JUGOS Y LICUADOS", "ABARROTES",
        };
        private static readonly Color[] ColoresNeon =
        {
            new Color(1f, 0.15f, 0.8f), new Color(0.1f, 0.95f, 0.85f), new Color(0.5f, 1f, 0.3f), new Color(1f, 0.55f, 0.15f), new Color(0.35f, 0.5f, 1f),
        };
        // Lona sucia: guinda, verde botella, mostaza, azul gris, naranja quemado, crema sucio.
        private static readonly Color[] ColoresLona =
        {
            new Color(0.62f, 0.2f, 0.18f), new Color(0.2f, 0.42f, 0.32f), new Color(0.78f, 0.58f, 0.18f), new Color(0.24f, 0.36f, 0.52f), new Color(0.78f, 0.38f, 0.16f), new Color(0.74f, 0.68f, 0.52f),
        };
        private static readonly Color[] ColoresPintura =
        {
            new Color(0.85f, 0.78f, 0.45f), new Color(0.85f, 0.85f, 0.8f), new Color(0.7f, 0.3f, 0.25f), new Color(0.35f, 0.62f, 0.6f),
        };
        private static readonly string[] MaterialesPersona =
        {
            "woman2_Peaton1", "woman2_Peaton2", "woman2_Peaton3", "woman2_Peaton4", "woman2_Vecina", "woman2_Vendedor", "woman2_Apoyo_doc", "woman2_Apoyo_hacker", "woman2_Apoyo_mecanico",
        };

        private static readonly StringBuilder Informe = new StringBuilder();
        private static readonly Dictionary<string, Material> Cache = new Dictionary<string, Material>();
        private static readonly Dictionary<string, int> Cuentas = new Dictionary<string, int>();
        private static Transform raiz;
        private static List<Bounds> propios;

        [MenuItem("Mecha Cyberpunk/T-508/Calles llenas")]
        public static void Colocar()
        {
            Informe.Clear();
            Cache.Clear();
            Cuentas.Clear();
            EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
            GameObject vieja = GameObject.Find(NombreRaiz);
            if (vieja != null)
            {
                Object.DestroyImmediate(vieja);
            }
            Physics.SyncTransforms();
            Directory.CreateDirectory(CarpetaTexturas);
            ColocarNoche.ReunirObstaculos();
            propios = new List<Bounds>();
            ToldosPuestos.Clear();
            totalGente = 0;
            raiz = new GameObject(NombreRaiz).transform;

            ArreglarCharcos();
            foreach (Tramo tramo in Tramos)
            {
                foreach (int lado in new[] { 1, -1 })
                {
                    List<Muro> muros = Escanear(tramo, lado);
                    Cuentas["muros_" + tramo.Zona] = Cuenta("muros_" + tramo.Zona) + muros.Count;
                    Transform g = Grupo(tramo.Zona + (lado > 0 ? "_A" : "_B"));
                    PonerToldos(g, muros, tramo, lado);
                    PonerLetreros(g, muros, tramo, lado);
                    PonerBalcones(g, muros, tramo, lado);
                    PonerMacetas(g, muros, tramo, lado);
                    PonerProps(g, muros, tramo, lado);
                    PonerGente(g, muros, tramo, lado);
                }
            }
            PonerClientesDePuestos();

            foreach (KeyValuePair<string, int> kv in Cuentas.OrderBy(k => k.Key))
            {
                Informe.AppendLine(kv.Key + ": " + kv.Value);
            }
            UnityEngine.SceneManagement.Scene escena = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(escena);
            EditorSceneManager.SaveScene(escena);
            AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/t508-colocar.txt", Informe.ToString());
            Debug.Log("[Arte] T-508:\n" + Informe);
        }

        // ------------------------------------------------------------------ utilidades

        private static int Cuenta(string clave)
        {
            return Cuentas.TryGetValue(clave, out int n) ? n : 0;
        }

        private static void Suma(string clave)
        {
            Cuentas[clave] = Cuenta(clave) + 1;
        }

        private static Transform Grupo(string nombre)
        {
            Transform t = new GameObject(nombre).transform;
            t.SetParent(raiz, false);
            return t;
        }

        private static int Hash(string s)
        {
            int h = 17;
            foreach (char ch in s)
            {
                h = h * 31 + ch;
            }
            return h & 0x7fffffff;
        }

        private static float Azar(string clave)
        {
            return (Hash(clave) % 10007) / 10007f;
        }

        private static string RutaPrefab(string codigo)
        {
            string n = ImportarT500.RutasPrefab.FirstOrDefault(r => Path.GetFileNameWithoutExtension(r) == codigo);
            return n ?? ArteNoche.Buscar(codigo).RutaPrefab;
        }

        private static Texture2D Tex(string nombre)
        {
            return AssetDatabase.LoadAssetAtPath<Texture2D>(CarpetaTexturas + "/" + nombre + ".png");
        }

        private static Material Mat(string nombre, string textura, Color tinte, Vector2 escala, float brillo, float metal)
        {
            if (Cache.TryGetValue(nombre, out Material hecho))
            {
                return hecho;
            }
            string ruta = CarpetaMateriales + "/" + nombre + ".mat";
            Material m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (m == null)
            {
                m = new Material(Shader.Find(RutaShader));
                AssetDatabase.CreateAsset(m, ruta);
            }
            m.shader = Shader.Find(RutaShader);
            tinte.a = 1f;
            m.SetColor("_BaseColor", tinte);
            m.SetTexture("_BaseMap", Tex(textura));
            m.SetTexture("_TopMap", Tex(textura));
            m.SetVector("_Escala", new Vector4(escala.x, escala.y, 0f, 0f));
            m.SetFloat("_Smoothness", brillo);
            m.SetFloat("_Metallic", metal);
            EditorUtility.SetDirty(m);
            Cache[nombre] = m;
            return m;
        }

        private static Material Brillo(string nombre, Color color)
        {
            if (Cache.TryGetValue(nombre, out Material hecho))
            {
                return hecho;
            }
            string ruta = CarpetaMateriales + "/" + nombre + ".mat";
            Material m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (m == null)
            {
                m = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                AssetDatabase.CreateAsset(m, ruta);
            }
            m.SetColor("_BaseColor", color);
            EditorUtility.SetDirty(m);
            Cache[nombre] = m;
            return m;
        }

        private static string Clave(Color c)
        {
            return ColorUtility.ToHtmlStringRGB(c);
        }

        private static GameObject Pieza(Transform padre, string nombre, PrimitiveType tipo, Vector3 pos, Quaternion rot, Vector3 escala, Material mat)
        {
            GameObject c = GameObject.CreatePrimitive(tipo);
            Object.DestroyImmediate(c.GetComponent<Collider>());
            c.name = nombre;
            c.transform.SetParent(padre, false);
            c.transform.SetPositionAndRotation(pos, rot);
            c.transform.localScale = escala;
            MeshRenderer mr = c.GetComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            return c;
        }

        private static Vector3 En(Muro m, float afuera, float lateral, float altura)
        {
            return new Vector3(m.Punto.x, 0f, m.Punto.z) + m.Normal * afuera + m.Tangente * lateral + Vector3.up * altura;
        }

        // ------------------------------------------------------------------ fachadas

        private static List<Muro> Escanear(Tramo tramo, int lado)
        {
            var lista = new List<Muro>();
            Vector3 perp = Vector3.Cross(Vector3.up, tramo.Dir) * lado;
            for (float s = 0.5f; s < tramo.Largo; s += 1f)
            {
                Vector3 centro = tramo.Origen + tramo.Dir * s;
                Vector3 desde = centro + Vector3.up * 1.4f;
                if (Physics.CheckSphere(desde, 0.3f, ~0, QueryTriggerInteraction.Ignore))
                {
                    continue;
                }
                if (!Physics.Raycast(desde, perp, out RaycastHit golpe, 16f, ~0, QueryTriggerInteraction.Ignore) || Vector3.Dot(golpe.normal, -perp) < 0.92f || golpe.collider.attachedRigidbody != null)
                {
                    continue;
                }
                bool plana = true;
                bool alto = true;
                foreach (float h in new[] { 2.6f, 3.6f, 4.4f })
                {
                    plana &= Physics.Raycast(new Vector3(desde.x, h, desde.z), perp, out RaycastHit g2, 16f, ~0, QueryTriggerInteraction.Ignore) && Mathf.Abs(g2.distance - golpe.distance) < 0.25f;
                }
                foreach (float h in new[] { 6.5f, 8.2f })
                {
                    alto &= Physics.Raycast(new Vector3(desde.x, h, desde.z), perp, out RaycastHit g3, 16f, ~0, QueryTriggerInteraction.Ignore) && Mathf.Abs(g3.distance - golpe.distance) < 0.25f;
                }
                if (!plana)
                {
                    continue;
                }
                Vector3 plano = new Vector3(golpe.point.x, 0f, golpe.point.z);
                bool puerta = ColocarNoche.puertas.Any(p => Vector3.Distance(new Vector3(p.x, 0f, p.z), plano) < 1.6f);
                lista.Add(new Muro
                {
                    Punto = golpe.point,
                    Normal = -perp,
                    Tangente = tramo.Dir,
                    S = s,
                    Puerta = puerta,
                    Alto = alto,
                    Zona = tramo.Zona,
                    Distancia = golpe.distance,
                });
            }
            return lista;
        }

        /// <summary>Hay sitio colgado de la pared: ningún obstáculo salvo el propio edificio (y los azulejos planos de la fachada) toca la caja.</summary>
        private static bool LibreColgante(Muro m, Bounds caja)
        {
            Vector3 dentro = m.Punto - m.Normal * 0.05f;
            foreach (Bounds b in ColocarNoche.obstaculos.Concat(propios))
            {
                if (!b.Intersects(caja) || b.Contains(dentro))
                {
                    continue;
                }
                Vector3 e = b.size;
                bool plano = Mathf.Abs(Vector3.Dot(m.Normal, Vector3.right)) > 0.5f ? (e.x < 1f && e.z > 2.5f) : (e.z < 1f && e.x > 2.5f);
                if (plano)
                {
                    continue;
                }
                return false;
            }
            return true;
        }

        private static Bounds Caja(Muro m, float afuera0, float afuera1, float lateral, float y0, float y1)
        {
            Vector3 a = En(m, afuera0, -lateral, y0);
            Vector3 b = En(m, afuera1, lateral, y1);
            Bounds caja = new Bounds(a, Vector3.zero);
            caja.Encapsulate(b);
            return caja;
        }

        private static Bounds CajaSuelo(Vector3 centro, float radio, float alto)
        {
            return new Bounds(centro + Vector3.up * alto * 0.5f, new Vector3(radio * 2f, alto, radio * 2f));
        }

        // ------------------------------------------------------------------ toldos

        private static void PonerToldos(Transform padre, List<Muro> muros, Tramo tramo, int lado)
        {
            float ultimo = -99f;
            int n = 0;
            // Primero sobre las puertas, luego los demás huecos cada 11 m.
            foreach (bool conPuerta in new[] { true, false })
            {
                foreach (Muro m in muros)
                {
                    if (m.Puerta != conPuerta || Mathf.Abs(m.S - ultimo) < (conPuerta ? 3.2f : 8f) || (conPuerta && !Mathf.Approximately(0f, 0f) && false))
                    {
                        continue;
                    }
                    if (!conPuerta && Azar("toldo" + tramo.Zona + m.S + lado) < 0.1f)
                    {
                        continue;
                    }
                    float ancho = 2.2f + Azar("anchot" + m.S + tramo.Zona) * 1.2f;
                    float altura = 2.95f;
                    Bounds caja = Caja(m, 0.02f, 1.35f, ancho * 0.5f, altura - 0.5f, altura + 0.3f);
                    if (!LibreColgante(m, caja) || ToldoCercano(m, tramo.Zona, lado))
                    {
                        continue;
                    }
                    CrearToldo(padre, m, ancho, altura, tramo.Zona + lado + m.S);
                    propios.Add(caja);
                    ultimo = m.S;
                    n++;
                }
                ultimo = -99f;
            }
            Cuentas["toldos_" + tramo.Zona] = Cuenta("toldos_" + tramo.Zona) + n;
        }

        private static readonly List<Vector3> ToldosPuestos = new List<Vector3>();

        private static bool ToldoCercano(Muro m, string zona, int lado)
        {
            Vector3 p = En(m, 0f, 0f, 0f);
            foreach (Vector3 q in ToldosPuestos)
            {
                if (Vector3.Distance(p, q) < 3f)
                {
                    return true;
                }
            }
            ToldosPuestos.Add(p);
            return false;
        }

        private static void CrearToldo(Transform padre, Muro m, float ancho, float altura, string semilla)
        {
            bool lamina = Azar("lam" + semilla) < 0.28f;
            Quaternion rot = Quaternion.LookRotation(m.Normal, Vector3.up);
            Quaternion inclinado = rot * Quaternion.Euler(lamina ? 9f : 15f, 0f, 0f);
            GameObject g = new GameObject(lamina ? "Toldo_Lamina" : "Toldo_Lona");
            g.transform.SetParent(padre, false);
            float fondo = 1.3f;
            Vector3 centro = En(m, fondo * 0.5f, 0f, altura);
            Material mat;
            if (lamina)
            {
                Color[] laminas = { new Color(0.55f, 0.5f, 0.45f), new Color(0.6f, 0.38f, 0.28f), new Color(0.42f, 0.5f, 0.52f) };
                Color c = laminas[Hash(semilla) % laminas.Length];
                mat = Mat("T508_ToldoLamina_" + Clave(c), "T506_Lamina", c, new Vector2(1.5f, 1.5f), 0.3f, 0.4f);
                Pieza(g.transform, "Lamina", PrimitiveType.Cube, centro, inclinado, new Vector3(ancho, 0.07f, fondo), mat);
            }
            else
            {
                Color c = ColoresLona[Hash(semilla) % ColoresLona.Length] * 1.45f;
                mat = Mat("T508_ToldoLona_" + Clave(c), "T506_Lona", c, new Vector2(1.2f, 1.2f), 0.1f, 0f);
                Pieza(g.transform, "Lona", PrimitiveType.Cube, centro, inclinado, new Vector3(ancho, 0.05f, fondo), mat);
                Color c2 = Color.Lerp(c, new Color(0.9f, 0.85f, 0.7f), 0.55f);
                Material mat2 = Mat("T508_ToldoFaldon_" + Clave(c2), "T506_Lona", c2, new Vector2(0.6f, 0.6f), 0.1f, 0f);
                Vector3 borde = centro + inclinado * new Vector3(0f, 0f, fondo * 0.5f);
                Pieza(g.transform, "Faldon", PrimitiveType.Cube, borde + Vector3.down * 0.11f, rot, new Vector3(ancho, 0.22f, 0.03f), mat2);
            }
            // Dos tirantes del borde a la pared.
            Material metal = Mat("T508_Tirante", "T506_Metal", new Color(0.18f, 0.18f, 0.2f), new Vector2(1f, 1f), 0.3f, 0.5f);
            foreach (float lat in new[] { -ancho * 0.45f, ancho * 0.45f })
            {
                Vector3 a = En(m, 0.03f, lat, altura + 0.35f);
                Vector3 b = En(m, fondo - 0.05f, lat, altura - 0.3f);
                Pieza(g.transform, "Tirante", PrimitiveType.Cube, (a + b) * 0.5f, Quaternion.LookRotation(b - a, Vector3.up), new Vector3(0.03f, 0.03f, Vector3.Distance(a, b)), metal);
            }
            Suma("piezas_toldo");
        }

        // ------------------------------------------------------------------ letreros salientes

        private static void PonerLetreros(Transform padre, List<Muro> muros, Tramo tramo, int lado)
        {
            float ultimo = -99f;
            int n = 0;
            foreach (Muro m in muros)
            {
                if (m.S - ultimo < 7f || Azar("let" + tramo.Zona + lado + m.S) < 0.2f)
                {
                    continue;
                }
                float altura = 3.9f + Azar("lh" + m.S + lado) * 0.7f;
                Bounds caja = Caja(m, 0.02f, 1.6f, 0.3f, altura - 0.95f, altura + 0.3f);
                if (!LibreColgante(m, caja))
                {
                    continue;
                }
                CrearLetrero(padre, m, altura, tramo.Zona + lado + m.S);
                propios.Add(caja);
                ultimo = m.S;
                n++;
            }
            Cuentas["letreros_" + tramo.Zona] = Cuenta("letreros_" + tramo.Zona) + n;
        }

        private static void CrearLetrero(Transform padre, Muro m, float altura, string semilla)
        {
            bool neon = Azar("neon" + semilla) < 0.6f;
            string texto = neon ? TextosNeon[Hash(semilla) % TextosNeon.Length] : TextosPintados[Hash(semilla) % TextosPintados.Length];
            GameObject g = new GameObject("Letrero_" + texto);
            g.transform.SetParent(padre, false);
            Quaternion haciaCalle = Quaternion.LookRotation(m.Normal, Vector3.up);
            Quaternion deCanto = Quaternion.LookRotation(m.Tangente, Vector3.up);
            Material metal = Mat("T508_Tirante", "T506_Metal", new Color(0.18f, 0.18f, 0.2f), new Vector2(1f, 1f), 0.3f, 0.5f);
            float largo = 1.4f;
            float alto = 0.66f;
            // Brazo de la pared y dos cadenas.
            Pieza(g.transform, "Brazo", PrimitiveType.Cube, En(m, largo * 0.5f + 0.05f, 0f, altura), haciaCalle, new Vector3(0.05f, 0.05f, largo + 0.1f), metal);
            foreach (float afuera in new[] { 0.2f, largo - 0.1f })
            {
                Pieza(g.transform, "Cadena", PrimitiveType.Cube, En(m, afuera, 0f, altura - 0.12f), haciaCalle, new Vector3(0.025f, 0.24f, 0.025f), metal);
            }
            Vector3 centro = En(m, 0.05f + largo * 0.5f, 0f, altura - 0.24f - alto * 0.5f);
            Color fondo;
            Color letra;
            if (neon)
            {
                letra = ColoresNeon[Hash(semilla + "c") % ColoresNeon.Length];
                fondo = new Color(0.07f, 0.07f, 0.1f);
                Material tablero = Mat("T508_TableroNeon", "T506_Metal", fondo, new Vector2(1f, 1f), 0.3f, 0.4f);
                Pieza(g.transform, "Tablero", PrimitiveType.Cube, centro, deCanto, new Vector3(largo, alto, 0.08f), tablero);
                Material tubo = Brillo("T508_Tubo_" + Clave(letra), letra * 1.6f);
                foreach (float dy in new[] { alto * 0.5f - 0.03f, -alto * 0.5f + 0.03f })
                {
                    foreach (float cara in new[] { 0.05f, -0.05f })
                    {
                        Pieza(g.transform, "Tubo", PrimitiveType.Cube, centro + Vector3.up * dy + m.Tangente * cara, deCanto, new Vector3(largo - 0.1f, 0.025f, 0.02f), tubo);
                    }
                }
            }
            else
            {
                Color pintura = ColoresPintura[Hash(semilla + "p") % ColoresPintura.Length];
                letra = pintura.grayscale > 0.55f ? new Color(0.12f, 0.1f, 0.1f) : new Color(0.95f, 0.9f, 0.75f);
                Material tablero = Mat("T508_TableroPintado_" + Clave(pintura), "T506_Lamina", pintura, new Vector2(1.2f, 1.2f), 0.25f, 0.2f);
                Pieza(g.transform, "Tablero", PrimitiveType.Cube, centro, deCanto, new Vector3(largo, alto, 0.07f), tablero);
            }
            foreach (float cara in new[] { 1f, -1f })
            {
                Vector3 lado = m.Tangente * cara;
                Texto(g.transform, texto, centro + lado * 0.05f, Quaternion.LookRotation(-lado, Vector3.up), letra, largo * 0.92f, alto * 0.6f);
            }
            Suma("piezas_letrero");
            Suma(neon ? "estilo_neon" : "estilo_pintado");
        }

        private static void Texto(Transform padre, string texto, Vector3 pos, Quaternion haciaFuera, Color color, float anchoMax, float altoMax)
        {
            float alto = Mathf.Min(altoMax, anchoMax / 4f);
            Material mat = MaterialTexto(texto, color);
            // El quad se ve desde su -Z: mira hacia dentro de la pared del letrero.
            Pieza(padre, "Texto", PrimitiveType.Quad, pos, haciaFuera, new Vector3(alto * 4f, alto, 1f), mat);
        }

        private static Material MaterialTexto(string texto, Color color)
        {
            string nombre = "T508_Txt_" + texto.Replace(" ", "_") + "_" + Clave(color);
            if (Cache.TryGetValue(nombre, out Material hecho))
            {
                return hecho;
            }
            string ruta = CarpetaMateriales + "/" + nombre + ".mat";
            Material m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (m == null)
            {
                m = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                AssetDatabase.CreateAsset(m, ruta);
            }
            m.SetTexture("_BaseMap", TexturaTexto(texto));
            m.SetColor("_BaseColor", color);
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f);
            m.SetOverrideTag("RenderType", "Transparent");
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)RenderQueue.Transparent;
            m.SetShaderPassEnabled("ShadowCaster", false);
            EditorUtility.SetDirty(m);
            Cache[nombre] = m;
            return m;
        }

        /// <summary>Dibuja el texto con una TextMesh y una cámara ortográfica a una imagen 512x128 (blanco con alfa) en Assets/Arte/Exteriores/Texturas.</summary>
        private static Texture2D TexturaTexto(string texto)
        {
            string ruta = CarpetaTexturas + "/T508_Txt_" + texto.Replace(" ", "_") + ".png";
            Texture2D previa = AssetDatabase.LoadAssetAtPath<Texture2D>(ruta);
            if (previa != null)
            {
                return previa;
            }
            const int w = 512;
            const int h = 128;
            Vector3 lejos = new Vector3(0f, -800f, 0f);
            GameObject go = new GameObject("TmpTexto");
            go.transform.position = lejos;
            TextMesh tm = go.AddComponent<TextMesh>();
            tm.text = texto;
            tm.fontSize = 96;
            tm.characterSize = 0.1f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = Color.white;
            tm.fontStyle = FontStyle.Bold;
            Bounds caja = go.GetComponent<MeshRenderer>().bounds;
            GameObject cg = new GameObject("TmpCamara");
            Camera cam = cg.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = Mathf.Max(caja.size.x / 8f * 1.08f, caja.size.y / 2f * 1.15f);
            cam.transform.position = caja.center + new Vector3(0f, 0f, -5f);
            cam.transform.rotation = Quaternion.identity;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 20f;
            RenderTexture rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            Texture2D foto = new Texture2D(w, h, TextureFormat.RGBA32, false);
            foto.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            Color32[] px = foto.GetPixels32();
            for (int i = 0; i < px.Length; i++)
            {
                byte l = (byte)Mathf.Max(px[i].r, Mathf.Max(px[i].g, px[i].b));
                px[i] = new Color32(255, 255, 255, l);
            }
            foto.SetPixels32(px);
            foto.Apply();
            File.WriteAllBytes(ruta, foto.EncodeToPNG());
            RenderTexture.active = null;
            Object.DestroyImmediate(foto);
            rt.Release();
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(cg);
            AssetDatabase.ImportAsset(ruta, ImportAssetOptions.ForceUpdate);
            TextureImporter imp = (TextureImporter)AssetImporter.GetAtPath(ruta);
            imp.alphaIsTransparency = true;
            imp.wrapMode = TextureWrapMode.Clamp;
            imp.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(ruta);
        }

        // ------------------------------------------------------------------ balcones

        private static void PonerBalcones(Transform padre, List<Muro> muros, Tramo tramo, int lado)
        {
            float ultimo = -99f;
            int n = 0;
            foreach (Muro m in muros)
            {
                if (!m.Alto || m.S - ultimo < 12f || Azar("bal" + tramo.Zona + lado + m.S) < 0.4f)
                {
                    continue;
                }
                float y0 = Azar("by" + m.S + lado) < 0.5f ? 6.1f : 7.3f;
                Bounds caja = Caja(m, 0.02f, 1.1f, 1.15f, y0 - 0.2f, y0 + 1.2f);
                if (!LibreColgante(m, caja))
                {
                    continue;
                }
                CrearBalcon(padre, m, y0, tramo.Zona + lado + m.S);
                propios.Add(caja);
                ultimo = m.S;
                n++;
            }
            Cuentas["balcones_" + tramo.Zona] = Cuenta("balcones_" + tramo.Zona) + n;
        }

        private static void CrearBalcon(Transform padre, Muro m, float y0, string semilla)
        {
            GameObject g = new GameObject("Balcon");
            g.transform.SetParent(padre, false);
            Quaternion rot = Quaternion.LookRotation(m.Normal, Vector3.up);
            Material losa = Mat("T508_BalconLosa", "T506_Losa", new Color(0.55f, 0.52f, 0.5f), new Vector2(2f, 2f), 0.15f, 0f);
            Material fierro = Mat("T508_Barandal", "T506_Metal", new Color(0.12f, 0.14f, 0.15f), new Vector2(1f, 1f), 0.35f, 0.5f);
            const float ancho = 2.2f;
            const float fondo = 0.95f;
            const float alto = 1.0f;
            Pieza(g.transform, "Losa", PrimitiveType.Cube, En(m, fondo * 0.5f, 0f, y0), rot, new Vector3(ancho, 0.14f, fondo), losa);
            // Pasamanos y travesaño frontal, laterales y barrotes.
            foreach (float h in new[] { alto, alto * 0.5f })
            {
                Pieza(g.transform, "Riel", PrimitiveType.Cube, En(m, fondo - 0.03f, 0f, y0 + h), rot, new Vector3(ancho, 0.045f, 0.045f), fierro);
                foreach (float lat in new[] { -ancho * 0.5f + 0.03f, ancho * 0.5f - 0.03f })
                {
                    Pieza(g.transform, "RielLateral", PrimitiveType.Cube, En(m, fondo * 0.5f, lat, y0 + h), rot, new Vector3(0.045f, 0.045f, fondo), fierro);
                }
            }
            for (int i = 0; i < 6; i++)
            {
                float lat = Mathf.Lerp(-ancho * 0.5f + 0.03f, ancho * 0.5f - 0.03f, i / 5f);
                Pieza(g.transform, "Barrote", PrimitiveType.Cube, En(m, fondo - 0.03f, lat, y0 + alto * 0.5f), rot, new Vector3(0.035f, alto, 0.035f), fierro);
            }
            foreach (float lat in new[] { -ancho * 0.5f + 0.03f, ancho * 0.5f - 0.03f })
            {
                Pieza(g.transform, "Poste", PrimitiveType.Cube, En(m, 0.03f, lat, y0 + alto * 0.5f), rot, new Vector3(0.04f, alto, 0.04f), fierro);
            }
            // Dos macetas sobre la losa, y a veces ropa tendida.
            CrearMaceta(g.transform, En(m, 0.3f, -0.6f, y0 + 0.07f), 0.7f, semilla + "a");
            CrearMaceta(g.transform, En(m, 0.3f, 0.5f, y0 + 0.07f), 0.55f, semilla + "b");
            if (Azar("ropa" + semilla) < 0.5f)
            {
                Color c = ColoresLona[Hash(semilla + "r") % ColoresLona.Length];
                Material tela = Mat("T508_Ropa_" + Clave(c), "T506_Lona", c, new Vector2(1f, 1f), 0.05f, 0f);
                Pieza(g.transform, "Ropa", PrimitiveType.Cube, En(m, fondo - 0.1f, 0.2f, y0 + alto - 0.28f), rot, new Vector3(0.5f, 0.5f, 0.015f), tela);
            }
            Suma("balcones");
        }

        // ------------------------------------------------------------------ macetas

        private static void CrearMaceta(Transform padre, Vector3 suelo, float escala, string semilla)
        {
            Color barro = Color.Lerp(new Color(0.75f, 0.4f, 0.26f), new Color(0.6f, 0.3f, 0.2f), Azar("barro" + semilla));
            Material mb = Mat("T508_Maceta_" + Clave(barro), "T506_Revoque", barro, new Vector2(0.5f, 0.5f), 0.1f, 0f);
            Color verde = Color.Lerp(new Color(0.2f, 0.5f, 0.2f), new Color(0.35f, 0.6f, 0.25f), Azar("verde" + semilla));
            Material mv = Mat("T508_Planta_" + Clave(verde), "T506_Lona", verde, new Vector2(0.6f, 0.6f), 0.1f, 0f);
            float alto = 0.34f * escala;
            Pieza(padre, "Maceta", PrimitiveType.Cylinder, suelo + Vector3.up * alto * 0.5f, Quaternion.identity, new Vector3(0.34f * escala, alto * 0.5f, 0.34f * escala), mb);
            Pieza(padre, "Planta", PrimitiveType.Sphere, suelo + Vector3.up * (alto + 0.14f * escala), Quaternion.identity, new Vector3(0.42f, 0.36f, 0.42f) * escala, mv);
            if (Azar("p2" + semilla) < 0.5f)
            {
                Pieza(padre, "Planta", PrimitiveType.Sphere, suelo + Vector3.up * (alto + 0.3f * escala) + Vector3.right * 0.05f, Quaternion.identity, new Vector3(0.26f, 0.26f, 0.26f) * escala, mv);
            }
        }

        private static void PonerMacetas(Transform padre, List<Muro> muros, Tramo tramo, int lado)
        {
            float ultimo = -99f;
            int n = 0;
            foreach (Muro m in muros)
            {
                if (m.S - ultimo < 7f || Azar("mac" + tramo.Zona + lado + m.S) < 0.3f)
                {
                    continue;
                }
                for (int k = 0; k < 2; k++)
                {
                    Vector3 pos = new Vector3(m.Punto.x, 0f, m.Punto.z) + m.Normal * 0.62f + m.Tangente * (k * 0.55f);
                    float suelo = ColocarNoche.Suelo(pos);
                    if (float.IsNaN(suelo))
                    {
                        continue;
                    }
                    pos.y = suelo;
                    Bounds caja = CajaSuelo(pos, 0.25f, 0.8f);
                    if (!ColocarNoche.Libre(caja) || propios.Any(b => b.Intersects(caja)))
                    {
                        continue;
                    }
                    CrearMaceta(padre, pos, 0.9f + 0.4f * Azar("ms" + m.S + k), tramo.Zona + m.S + k);
                    propios.Add(caja);
                    n++;
                    ultimo = m.S;
                }
            }
            Cuentas["macetas_" + tramo.Zona] = Cuenta("macetas_" + tramo.Zona) + n;
        }

        // ------------------------------------------------------------------ tambos y huacales

        private static void PonerProps(Transform padre, List<Muro> muros, Tramo tramo, int lado)
        {
            float ultimo = -99f;
            int n = 0;
            string[] codigos = { "C16_Tambo", "C17_Huacales", "C16_Tambo", "C17_Huacales", "N06" };
            foreach (Muro m in muros)
            {
                if (m.S - ultimo < 9f || Azar("prop" + tramo.Zona + lado + m.S) < 0.4f)
                {
                    continue;
                }
                string codigo = codigos[Hash(tramo.Zona + lado + m.S) % codigos.Length];
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab(codigo));
                if (prefab == null)
                {
                    continue;
                }
                float escala = codigo == "N06" ? 0.7f : 1f;
                Vector3 tam = prefab.GetComponent<BoxCollider>().size * escala;
                float giro = Quaternion.LookRotation(m.Normal).eulerAngles.y + (Azar("pg" + m.S) - 0.5f) * 50f;
                Vector3 pos = new Vector3(m.Punto.x, 0f, m.Punto.z) + m.Normal * (0.3f + Mathf.Max(tam.x, tam.z) * 0.5f + 0.2f);
                float suelo = ColocarNoche.Suelo(pos);
                if (float.IsNaN(suelo))
                {
                    continue;
                }
                pos.y = suelo;
                Bounds caja = ColocarNoche.CajaGirada(tam, pos, giro);
                if (!ColocarNoche.Libre(caja) || propios.Any(b => b.Intersects(caja)))
                {
                    continue;
                }
                GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, padre);
                go.name = codigo + "_T508";
                go.transform.localScale = Vector3.one * escala;
                go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, giro, 0f));
                foreach (Collider c in go.GetComponentsInChildren<Collider>())
                {
                    Object.DestroyImmediate(c);
                }
                propios.Add(caja);
                ultimo = m.S;
                n++;
            }
            Cuentas["props_" + tramo.Zona] = Cuenta("props_" + tramo.Zona) + n;
        }

        // ------------------------------------------------------------------ gente

        private static GameObject Persona(Transform padre, string visual, int material, Vector3 pos, Vector3 mira, string nombre)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CarpetaVisuales + visual + ".prefab");
            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, padre);
            go.name = "Gente_" + nombre;
            mira.y = 0f;
            go.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(mira.sqrMagnitude > 0.001f ? mira.normalized : Vector3.forward, Vector3.up));
            foreach (Collider c in go.GetComponentsInChildren<Collider>(true))
            {
                Object.DestroyImmediate(c);
            }
            SkinnedMeshRenderer cuerpo = go.GetComponentInChildren<SkinnedMeshRenderer>();
            if (cuerpo != null)
            {
                if (material >= 0 && visual == "Visual_woman2")
                {
                    Material mat = AssetDatabase.LoadAssetAtPath<Material>(CarpetaPersonajes + MaterialesPersona[material % MaterialesPersona.Length] + ".mat");
                    if (mat != null)
                    {
                        cuerpo.sharedMaterial = mat;
                    }
                }
                cuerpo.shadowCastingMode = ShadowCastingMode.Off;
                cuerpo.updateWhenOffscreen = false;
                // Fuera de pantalla no se anima y de lejos no se dibuja (60 FPS).
                LODGroup grupo = go.AddComponent<LODGroup>();
                grupo.SetLODs(new[] { new LOD(0.045f, new Renderer[] { cuerpo }) });
                grupo.RecalculateBounds();
            }
            Animator anim = go.GetComponentInChildren<Animator>();
            if (anim != null)
            {
                anim.cullingMode = AnimatorCullingMode.CullCompletely;
            }
            return go;
        }

        private static readonly Dictionary<string, int> GentePorZona = new Dictionary<string, int>();
        private static int totalGente;

        private static bool HayLugarParaGente(Vector3 pos)
        {
            Bounds caja = CajaSuelo(pos, 0.38f, 1.8f);
            return ColocarNoche.Libre(caja) && !propios.Any(b => b.Intersects(caja));
        }

        private static void PonerGente(Transform padre, List<Muro> muros, Tramo tramo, int lado)
        {
            float ultimo = -99f;
            int maxLado = Mathf.Max(1, tramo.MaxGente / 2);
            int n = 0;
            string[] visuales = { "Visual_woman2", "Visual_woman2", "Visual_woman", "Visual_woman2" };
            foreach (Muro m in muros)
            {
                if (n >= maxLado || totalGente >= GenteMaxima || m.S - ultimo < 11f || Azar("gen" + tramo.Zona + lado + m.S) < 0.45f)
                {
                    continue;
                }
                bool pareja = Azar("par" + tramo.Zona + lado + m.S) < 0.55;
                // Recargado: espalda a 0.45 m de la pared; platicando: dos personas frente a frente a 1.1 m de la pared.
                float afuera = pareja ? 1.15f : 0.68f;
                Vector3 baseP = new Vector3(m.Punto.x, 0f, m.Punto.z) + m.Normal * afuera;
                Vector3 a = baseP + m.Tangente * (pareja ? -0.45f : 0f);
                Vector3 b = baseP + m.Tangente * 0.45f;
                float sueloA = ColocarNoche.Suelo(a);
                float sueloB = ColocarNoche.Suelo(b);
                if (float.IsNaN(sueloA) || float.IsNaN(sueloB))
                {
                    continue;
                }
                a.y = sueloA;
                b.y = sueloB;
                if (!HayLugarParaGente(a) || (pareja && !HayLugarParaGente(b)))
                {
                    continue;
                }
                int h = Hash(tramo.Zona + lado + m.S);
                if (pareja)
                {
                    Persona(padre, visuales[h % visuales.Length], h % 9, a, b - a + m.Normal * 0.25f, "Platica_" + tramo.Zona + "_" + n);
                    Persona(padre, visuales[(h / 7) % visuales.Length], (h / 3) % 9, b, a - b + m.Normal * 0.15f, "Platica_" + tramo.Zona + "_" + n + "b");
                    propios.Add(CajaSuelo(a, 0.4f, 1.8f));
                    propios.Add(CajaSuelo(b, 0.4f, 1.8f));
                    totalGente += 2;
                    Suma("gente_" + tramo.Zona);
                    Suma("gente_" + tramo.Zona);
                    Suma("gente_parejas");
                }
                else
                {
                    Persona(padre, visuales[h % visuales.Length], h % 9, a, m.Normal, "Recargada_" + tramo.Zona + "_" + n);
                    propios.Add(CajaSuelo(a, 0.4f, 1.8f));
                    totalGente++;
                    Suma("gente_" + tramo.Zona);
                    Suma("gente_solas");
                }
                ultimo = m.S;
                n++;
            }
        }

        /// <summary>Clientes junto a los puestos del Tianguis: uno de pie delante de cada tercer puesto.</summary>
        private static void PonerClientesDePuestos()
        {
            Transform g = Grupo("Clientes");
            int i = 0;
            foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Where(x => x.name.StartsWith("Puesto_") && x.parent != null && x.parent.name == "Puestos").OrderBy(x => x.name))
            {
                if (totalGente >= GenteMaxima || (i++ % 3) != 0)
                {
                    continue;
                }
                foreach (Vector3 dir in new[] { Vector3.forward, Vector3.back, Vector3.right, Vector3.left })
                {
                    Vector3 pos = new Vector3(t.position.x, 0f, t.position.z) + dir * 1.9f;
                    float suelo = ColocarNoche.Suelo(pos);
                    if (float.IsNaN(suelo) || !HayLugarParaGente(new Vector3(pos.x, suelo, pos.z)))
                    {
                        continue;
                    }
                    pos.y = suelo;
                    int h = Hash(t.name);
                    Persona(g, h % 3 == 0 ? "Visual_woman" : "Visual_woman2", h % 9, pos, -dir, "Cliente_" + t.name);
                    propios.Add(CajaSuelo(pos, 0.4f, 1.8f));
                    totalGente++;
                    Suma("gente_clientes");
                    break;
                }
            }
        }

        // ------------------------------------------------------------------ charcos

        private static Texture2D TexturaCharco()
        {
            Texture2D t = AssetDatabase.LoadAssetAtPath<Texture2D>(RutaTexturaCharco);
            if (t != null)
            {
                return t;
            }
            const int n = 128;
            Texture2D nueva = new Texture2D(n, n, TextureFormat.RGBA32, false);
            // Borde irregular: radio = 0.55 + suma de ondas en el ángulo; el centro es opaco y el borde se desvanece.
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    float dx = (x + 0.5f) / n * 2f - 1f;
                    float dy = (y + 0.5f) / n * 2f - 1f;
                    float ang = Mathf.Atan2(dy, dx);
                    float radio = 0.58f + 0.1f * Mathf.Sin(ang * 3f + 1f) + 0.07f * Mathf.Sin(ang * 5f + 2.3f) + 0.05f * Mathf.Sin(ang * 8f + 0.4f) + 0.03f * Mathf.Sin(ang * 13f);
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01((radio - d) / 0.28f);
                    a = a * a * (3f - 2f * a);
                    nueva.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }
            nueva.Apply();
            File.WriteAllBytes(RutaTexturaCharco, nueva.EncodeToPNG());
            Object.DestroyImmediate(nueva);
            AssetDatabase.ImportAsset(RutaTexturaCharco, ImportAssetOptions.ForceUpdate);
            TextureImporter imp = (TextureImporter)AssetImporter.GetAtPath(RutaTexturaCharco);
            imp.alphaIsTransparency = true;
            imp.wrapMode = TextureWrapMode.Clamp;
            imp.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(RutaTexturaCharco);
        }

        private static Material MaterialCharco(Color color, bool oscuro)
        {
            string nombre = "T508_Charco_" + (oscuro ? "Mojado" : Clave(color));
            if (Cache.TryGetValue(nombre, out Material hecho))
            {
                return hecho;
            }
            string ruta = CarpetaMateriales + "/" + nombre + ".mat";
            Material m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (m == null)
            {
                m = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                AssetDatabase.CreateAsset(m, ruta);
            }
            m.SetTexture("_BaseMap", TexturaCharco());
            // Reflejo sutil: color casi sin brillo y muy transparente; el mojado oscuro solo ensombrece.
            m.SetColor("_BaseColor", oscuro ? new Color(0.02f, 0.03f, 0.05f, 0.4f) : new Color(color.r * 0.55f, color.g * 0.55f, color.b * 0.55f, 0.22f));
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f);
            m.SetOverrideTag("RenderType", "Transparent");
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)RenderQueue.Transparent;
            m.SetShaderPassEnabled("ShadowCaster", false);
            EditorUtility.SetDirty(m);
            Cache[nombre] = m;
            return m;
        }

        /// <summary>Los charcos de T-501 a T-507 (discos de color sólido, muy grandes) pasan a reflejos sutiles: forma irregular, mucha transparencia y la mitad de tamaño.</summary>
        private static void ArreglarCharcos()
        {
            int n = 0;
            foreach (MeshRenderer r in Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                Material viejo = r.sharedMaterial;
                if (viejo == null || r.transform.IsChildOf(raiz) || !(r.name.StartsWith("Charco_") || viejo.name.Contains("Charco")))
                {
                    continue;
                }
                if (viejo.name.StartsWith("T508_Charco"))
                {
                    continue;
                }
                Color color = Color.black;
                bool oscuro = true;
                if (viejo.HasProperty("_EmissionColor") && viejo.IsKeywordEnabled("_EMISSION") && viejo.GetColor("_EmissionColor").maxColorComponent > 0.05f)
                {
                    color = viejo.GetColor("_EmissionColor");
                    oscuro = false;
                }
                else if (viejo.shader.name.Contains("Unlit") && viejo.HasProperty("_BaseColor"))
                {
                    color = viejo.GetColor("_BaseColor");
                    oscuro = color.maxColorComponent < 0.08f;
                }
                if (!oscuro)
                {
                    float max = Mathf.Max(0.01f, color.maxColorComponent);
                    color = new Color(Mathf.Round(color.r / max * 4f) / 4f, Mathf.Round(color.g / max * 4f) / 4f, Mathf.Round(color.b / max * 4f) / 4f);
                }
                r.sharedMaterial = MaterialCharco(color, oscuro);
                r.shadowCastingMode = ShadowCastingMode.Off;
                Vector3 e = r.transform.localScale;
                float mayor = Mathf.Max(e.x, e.y);
                float factor = Mathf.Min(0.5f, CharcoMaximo / Mathf.Max(0.01f, mayor));
                r.transform.localScale = new Vector3(e.x * factor, e.y * factor, e.z);
                EditorUtility.SetDirty(r);
                n++;
            }
            Cuentas["charcos_cambiados"] = n;
        }
    }
}
