using System.Collections.Generic;
using System.IO;
using System.Linq;
using MechaCyberpunk.Mundo.Ambiente;
using MechaCyberpunk.Mundo.Reloj;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// Sesión 2 del 7-nov (docs/plan-look-ciudad.md §2–3): las piezas con las que se arma una calle como K01. Casas de 2 pisos con el kit C
    /// (planta baja C01/C02, piso alto C03 en colores, pretil, tinaco sobre base de ladrillo, foco de pared, letrero de neón chico y ventanas
    /// cálidas al azar), banquetas y arroyo con medida en metros, postes C07 con su luz, halo y transformador, cables colgantes, charcos que
    /// reflejan, líneas, alcantarillas y basura. Todo es código de editor: lo que se genera (mallas, texturas, materiales) queda como asset.
    /// Lo usan ColocarS2cCalle (la calle de prueba) y el kit de manzanas.
    /// </summary>
    public static class KitS2c
    {
        public const string Carpeta = "Assets/Arte/Mundo/S2c";
        public const string CarpetaTexturas = Carpeta + "/Texturas";
        public const string CarpetaMallas = Carpeta + "/Mallas";
        private const string CarpetaPrefabs = "Assets/Arte/Calle/Prefabs/";

        public static readonly Color Magenta = new Color(1f, 0.2f, 0.78f);
        public static readonly Color Turquesa = new Color(0.1f, 0.95f, 0.88f);
        public static readonly Color Sodio = new Color(1f, 0.6f, 0.22f);
        public static readonly Color Calido = new Color(1f, 0.72f, 0.42f);
        /// <summary>Luz fría de Nova (K06): blanca azulada.</summary>
        public static readonly Color Fria = new Color(0.72f, 0.86f, 1f);
        /// <summary>Luz de sodio de un poste en una calle angosta (las farolas de las avenidas de 14 m usan 700).</summary>
        /// <summary>Las piezas C del kit miden 4 m de planta baja; las casas de K01 son un poco más bajas (planta de ~3.5 m).</summary>
        public const float EscalaAlto = 0.88f;
        public const float IntensidadPoste = 340f;

        /// <summary>Colores de los pisos altos (sobre el C03 crema): terracota, turquesa apagado, crema y verde agua (§1).</summary>
        public static readonly (string nombre, Color color)[] ColoresPiso =
        {
            ("Crema", new Color(1f, 0.97f, 0.9f)), ("Terracota", new Color(0.95f, 0.6f, 0.46f)),
            ("Turquesa", new Color(0.58f, 0.86f, 0.84f)), ("VerdeAgua", new Color(0.72f, 0.92f, 0.78f)),
        };

        // ================================================================ Contexto de una obra

        /// <summary>Una obra: su raíz en la escena, el azar con semilla y las mallas que genera (van a un solo asset al terminar).</summary>
        public sealed class Obra
        {
            public Transform Raiz;
            public System.Random Azar;
            public string Nombre;
            public readonly List<Mesh> Mallas = new List<Mesh>();
            public readonly List<Vector3> CablesV = new List<Vector3>();
            public readonly List<Vector3> CablesN = new List<Vector3>();
            public readonly List<int> CablesI = new List<int>();
            public int Casas;
            public int Ventanas;
            public int Neones;
            public int Postes;
            public int Cables;
            public int Charcos;

            public float Rango(float a, float b) => a + (float)Azar.NextDouble() * (b - a);
            public bool Chance(float p) => Azar.NextDouble() < p;
            public T Uno<T>(IReadOnlyList<T> lista) => lista[Azar.Next(lista.Count)];
        }

        /// <summary>Rehace la raíz <paramref name="nombre"/> de la escena (la borra si ya estaba).</summary>
        public static Obra Empezar(string nombre, int semilla, Transform padre = null)
        {
            Directory.CreateDirectory(CarpetaTexturas);
            Directory.CreateDirectory(CarpetaMallas);
            AssetDatabase.Refresh();
            GameObject vieja = padre == null ? GameObject.Find(nombre) : padre.Find(nombre)?.gameObject;
            if (vieja != null)
            {
                Object.DestroyImmediate(vieja);
            }
            GameObject raiz = new GameObject(nombre);
            if (padre != null)
            {
                raiz.transform.SetParent(padre, false);
            }
            return new Obra { Raiz = raiz.transform, Azar = new System.Random(semilla), Nombre = nombre };
        }

        /// <summary>Junta los cables en una malla y guarda todas las mallas de la obra en Mallas/&lt;nombre&gt;.asset.</summary>
        public static void Terminar(Obra obra)
        {
            if (obra.CablesV.Count > 0)
            {
                Mesh m = new Mesh { name = obra.Nombre + "_Cables", indexFormat = IndexFormat.UInt32 };
                m.SetVertices(obra.CablesV);
                m.SetNormals(obra.CablesN);
                m.SetTriangles(obra.CablesI, 0);
                m.RecalculateBounds();
                GameObject go = new GameObject("Cables");
                go.transform.SetParent(obra.Raiz, false);
                go.AddComponent<MeshFilter>().sharedMesh = m;
                go.AddComponent<MeshRenderer>().sharedMaterial = Mat("S2c_Cable", () => Lit(new Color(0.05f, 0.05f, 0.06f), 0.35f, TexMuro()));
                obra.Mallas.Add(m);
            }
            string ruta = CarpetaMallas + "/" + obra.Nombre + ".asset";
            AssetDatabase.DeleteAsset(ruta);
            if (obra.Mallas.Count > 0)
            {
                AssetDatabase.CreateAsset(obra.Mallas[0], ruta);
                foreach (Mesh m in obra.Mallas.Skip(1))
                {
                    AssetDatabase.AddObjectToAsset(m, ruta);
                }
                AssetDatabase.SaveAssets();
            }
        }

        // ================================================================ Piezas del kit

        public static GameObject Prefab(string codigo)
        {
            GameObject p = AssetDatabase.LoadAssetAtPath<GameObject>(CarpetaPrefabs + codigo + ".prefab");
            if (p == null)
            {
                string guid = AssetDatabase.FindAssets(codigo + " t:Prefab").FirstOrDefault(g => Path.GetFileNameWithoutExtension(AssetDatabase.GUIDToAssetPath(g)) == codigo);
                p = guid == null ? null : AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
            }
            if (p == null)
            {
                Debug.LogError("[Arte] KitS2c: no encuentro el prefab " + codigo);
            }
            return p;
        }

        public static GameObject Pieza(Transform padre, string codigo, Vector3 posicion, Quaternion giro, Vector3 escala, bool conCollider = true)
        {
            GameObject p = Prefab(codigo);
            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(p, padre);
            go.transform.SetPositionAndRotation(posicion, giro);
            go.transform.localScale = escala;
            if (!conCollider)
            {
                foreach (Collider c in go.GetComponentsInChildren<Collider>(true))
                {
                    c.enabled = false;
                }
            }
            return go;
        }

        /// <summary>Medidas de una pieza del kit (sin girar): caja visible, plano del muro de enfrente (frente a -Z) y huecos de ventana.</summary>
        public sealed class Medida
        {
            public Bounds Caja;
            public float Muro;
            public readonly List<(Rect rect, float fondo)> Huecos = new List<(Rect, float)>();
        }

        private static readonly Dictionary<string, Medida> Medidas = new Dictionary<string, Medida>();

        /// <summary>Para revisar la medida: guarda Logs/profundidad-&lt;codigo&gt;.png (gris = muro, claro = hundido, oscuro = saliente).</summary>
        public static bool GuardarProfundidad;

        /// <summary>
        /// Mide la pieza disparando rayos desde el frente (-Z) en una rejilla de 5 cm: el plano del muro es la profundidad más común; los huecos
        /// (ventanas, puertas, cortinas) son lo que queda al menos 3 cm más adentro, juntando los pedazos que separan los barrotes.
        /// </summary>
        public static Medida Medir(string codigo)
        {
            if (Medidas.TryGetValue(codigo, out Medida ya))
            {
                return ya;
            }
            Medida m = new Medida();
            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(Prefab(codigo));
            go.transform.position = new Vector3(0f, -5000f, 0f);
            Renderer[] rs = go.GetComponentsInChildren<Renderer>();
            Bounds b = rs[0].bounds;
            foreach (Renderer r in rs)
            {
                b.Encapsulate(r.bounds);
            }
            List<MeshCollider> temporales = new List<MeshCollider>();
            foreach (MeshFilter mf in go.GetComponentsInChildren<MeshFilter>())
            {
                MeshCollider mc = mf.gameObject.AddComponent<MeshCollider>();
                mc.sharedMesh = mf.sharedMesh;
                temporales.Add(mc);
            }
            foreach (Collider c in go.GetComponentsInChildren<Collider>().Where(c => !(c is MeshCollider)))
            {
                c.enabled = false;
            }
            Physics.SyncTransforms();
            const float paso = 0.05f;
            int nx = Mathf.CeilToInt(b.size.x / paso);
            int ny = Mathf.CeilToInt(b.size.y / paso);
            float[,] z = new float[nx, ny];
            Dictionary<int, int> cuenta = new Dictionary<int, int>();
            for (int i = 0; i < nx; i++)
            {
                for (int j = 0; j < ny; j++)
                {
                    Vector3 o = new Vector3(b.min.x + (i + 0.5f) * paso, b.min.y + (j + 0.5f) * paso, b.min.z - 1f);
                    z[i, j] = float.NaN;
                    if (Physics.Raycast(o, Vector3.forward, out RaycastHit h, b.size.z + 2f) && temporales.Contains(h.collider as MeshCollider))
                    {
                        z[i, j] = h.point.z;
                        int k = Mathf.RoundToInt(h.point.z * 100f);
                        cuenta[k] = cuenta.TryGetValue(k, out int c) ? c + 1 : 1;
                    }
                }
            }
            m.Muro = cuenta.Count == 0 ? b.min.z : cuenta.OrderByDescending(p => p.Value).First().Key / 100f;
            if (GuardarProfundidad)
            {
                Texture2D t = new Texture2D(nx, ny, TextureFormat.RGB24, false);
                for (int i = 0; i < nx; i++)
                {
                    for (int j = 0; j < ny; j++)
                    {
                        float d = float.IsNaN(z[i, j]) ? 0f : Mathf.Clamp01(0.5f + (z[i, j] - m.Muro) * 5f);
                        t.SetPixel(i, j, float.IsNaN(z[i, j]) ? Color.red : new Color(d, d, d));
                    }
                }
                File.WriteAllBytes("Logs/profundidad-" + codigo + ".png", t.EncodeToPNG());
                Object.DestroyImmediate(t);
            }
            // Huecos hundidos (ventana o puerta metida en el muro) y ventanas salientes (marco con barrotes que sale del muro, como en C03).
            foreach (bool saliente in new[] { false, true })
            {
                bool[,] hueco = new bool[nx, ny];
                for (int i = 0; i < nx; i++)
                {
                    for (int j = 0; j < ny; j++)
                    {
                        float dz = float.IsNaN(z[i, j]) ? 0f : z[i, j] - m.Muro;
                        hueco[i, j] = saliente ? dz < -0.025f && dz > -0.35f : dz > 0.03f && dz < 0.6f;
                    }
                }
                Componentes(m, z, hueco, nx, ny, paso, b, go.transform.position, saliente);
            }
            b.center -= go.transform.position;
            m.Caja = b;
            Object.DestroyImmediate(go);
            Medidas[codigo] = m;
            ColocarS2.Informe.AppendLine("  medida " + codigo + ": muro z=" + m.Muro.ToString("0.00") + " caja=" + b.size.ToString("0.00") + " huecos=" +
                                         string.Join(" ", m.Huecos.Select(h => "[" + h.rect.x.ToString("0.0") + "," + h.rect.y.ToString("0.0") + " " + h.rect.width.ToString("0.0") + "x" + h.rect.height.ToString("0.0") + " f" + h.fondo.ToString("0.00") + "]")));
            return m;
        }

        private static void Componentes(Medida m, float[,] z, bool[,] hueco, int nx, int ny, float paso, Bounds b, Vector3 origen, bool saliente)
        {
            // Cierre: los barrotes parten la ventana en tiras; se juntan con una vecindad de 3 celdas.
            bool[,] visto = new bool[nx, ny];
            int vecindad = saliente ? 1 : 3;
            for (int i = 0; i < nx; i++)
            {
                for (int j = 0; j < ny; j++)
                {
                    if (!hueco[i, j] || visto[i, j])
                    {
                        continue;
                    }
                    Queue<Vector2Int> cola = new Queue<Vector2Int>();
                    cola.Enqueue(new Vector2Int(i, j));
                    visto[i, j] = true;
                    int x0 = i, x1 = i, y0 = j, y1 = j, n = 0;
                    List<float> fondos = new List<float>();
                    while (cola.Count > 0)
                    {
                        Vector2Int c = cola.Dequeue();
                        n++;
                        fondos.Add(z[c.x, c.y]);
                        x0 = Mathf.Min(x0, c.x); x1 = Mathf.Max(x1, c.x); y0 = Mathf.Min(y0, c.y); y1 = Mathf.Max(y1, c.y);
                        for (int dx = -vecindad; dx <= vecindad; dx++)
                        {
                            for (int dy = -vecindad; dy <= vecindad; dy++)
                            {
                                int a = c.x + dx, e = c.y + dy;
                                if (a >= 0 && e >= 0 && a < nx && e < ny && hueco[a, e] && !visto[a, e])
                                {
                                    visto[a, e] = true;
                                    cola.Enqueue(new Vector2Int(a, e));
                                }
                            }
                        }
                    }
                    Rect r = new Rect(b.min.x + x0 * paso - origen.x, b.min.y + y0 * paso - origen.y, (x1 - x0 + 1) * paso, (y1 - y0 + 1) * paso);
                    float llenado = n / (float)((x1 - x0 + 1) * (y1 - y0 + 1));
                    bool forma = saliente ? r.width >= 0.45f && r.height >= 0.55f && r.width <= 2.4f : r.width >= 0.4f && r.height >= 0.4f;
                    if (forma && llenado > 0.25f)
                    {
                        fondos.Sort();
                        // Hundido: el vidrio es lo más profundo. Saliente: la cara del marco (la textura de la ventana lleva sus barrotes).
                        m.Huecos.Add((r, saliente ? fondos[fondos.Count / 2] - 0.012f : fondos[(int)(fondos.Count * 0.85f)] - 0.012f));
                    }
                }
            }
        }

        // ================================================================ Casas y frentes de cuadra

        /// <summary>Cómo es una casa: planta baja (C01 local o C02 vivienda), piso alto (C03 con color) y lo que lleva.</summary>
        public sealed class Estilo
        {
            public string PlantaBaja = "C02_FachadaVivienda_cy";
            public string PisoAlto = "C03_FachadaPisoAlto_cy";
            public int ColorPiso;
            public bool Tinaco = true;
            public bool Foco = true;
            public Color? Neon;
            public bool NeonBandera = true;
            public bool Antena;
            public bool AireAzotea;
            public float LucesVentanas = 0.45f;
        }

        public static Estilo EstiloAlAzar(Obra o)
        {
            return new Estilo
            {
                PlantaBaja = o.Chance(0.45f) ? "C01_FachadaLocal_cy" : "C02_FachadaVivienda_cy",
                PisoAlto = o.Chance(0.12f) ? "C20b_PisoDosVentanas_cy" : "C03_FachadaPisoAlto_cy",
                ColorPiso = o.Azar.Next(ColoresPiso.Length),
                Tinaco = o.Chance(0.75f),
                Foco = o.Chance(0.65f),
                Neon = o.Chance(0.4f) ? (o.Chance(0.55f) ? Magenta : Turquesa) : (Color?)null,
                NeonBandera = o.Chance(0.6f),
                Antena = o.Chance(0.2f),
                AireAzotea = o.Chance(0.35f),
            };
        }

        /// <summary>
        /// Una casa de 2 pisos con el frente sobre la línea de fachada. <paramref name="centro"/> es el punto medio del frente al nivel de la
        /// banqueta, <paramref name="normal"/> apunta a la calle, <paramref name="ancho"/> y <paramref name="fondo"/> en metros. Devuelve la
        /// altura de la azotea.
        /// </summary>
        public static float Casa(Obra o, Transform padre, Vector3 centro, Vector3 normal, float ancho, float fondo, Estilo e)
        {
            o.Casas++;
            normal.y = 0f;
            normal.Normalize();
            Quaternion giro = Quaternion.LookRotation(-normal);
            Vector3 lado = Vector3.Cross(Vector3.up, normal).normalized;
            GameObject casa = new GameObject("Casa_" + o.Casas);
            casa.transform.SetParent(padre, false);
            casa.transform.position = centro;

            Medida mb = Medir(e.PlantaBaja);
            float altoBaja = mb.Caja.size.y * EscalaAlto;
            GameObject baja = Pieza(casa.transform, e.PlantaBaja, centro + normal * mb.Muro, giro, new Vector3(ancho / mb.Caja.size.x, EscalaAlto, 1f));
            Ventanas(o, baja.transform, mb, e.LucesVentanas * 0.5f, 1.2f);

            Medida ma = Medir(e.PisoAlto);
            bool panel = ma.Caja.size.z < 1.2f;
            float altoAlto = ma.Caja.size.y * EscalaAlto;
            Vector3 baseAlto = centro + Vector3.up * altoBaja;
            GameObject alto = Pieza(casa.transform, e.PisoAlto, baseAlto + normal * ma.Muro, giro, new Vector3(ancho / ma.Caja.size.x, EscalaAlto, 1f));
            Pintar(alto, e.ColorPiso);
            Ventanas(o, alto.transform, ma, e.LucesVentanas, 0f);
            float azotea = altoBaja + altoAlto;

            // Cuerpo detrás de las piezas: llena el fondo de la cuadra (de atrás y de arriba es lo que se ve).
            Vector3 cuerpoCentro = centro - normal * (fondo * 0.5f + 0.02f) + Vector3.up * (azotea - 0.02f) * 0.5f;
            Losa(o, casa.transform, "Cuerpo", cuerpoCentro, giro, new Vector3(ancho - 0.04f, azotea - 0.02f, fondo - 0.04f),
                MatCuerpo(e.ColorPiso), 3f, true);
            if (panel)
            {
                // El panel C20 es delgado: el muro del piso alto lo pone el cuerpo; arriba de la planta baja va una franja.
                Pieza(casa.transform, "C23_Franja", centro + Vector3.up * (altoBaja - 0.3f) + normal * 0.1f, giro, new Vector3(ancho / 4.43f, 1f, 1f), false);
            }

            // Pretil al frente de la azotea.
            int nPretil = Mathf.Max(1, Mathf.RoundToInt(ancho / 1.88f));
            for (int i = 0; i < nPretil; i++)
            {
                float t = (i + 0.5f) / nPretil - 0.5f;
                Pieza(casa.transform, "C05_PretilAzotea", centro + Vector3.up * azotea + lado * (t * ancho) - normal * 0.2f, giro,
                    new Vector3(ancho / nPretil / 1.88f, 1f, 1f), false);
            }
            // Tinaco sobre su base de ladrillo, atrás en la azotea.
            if (e.Tinaco)
            {
                float a = o.Rango(-0.3f, 0.3f) * ancho;
                float f = Mathf.Min(fondo - 1.2f, o.Rango(1.4f, 2.4f));
                Vector3 p = centro + lado * a - normal * f + Vector3.up * azotea;
                Losa(o, casa.transform, "BaseTinaco", p + Vector3.up * 0.35f, giro, new Vector3(1.5f, 0.7f, 1.9f), MatLadrillo(), 1f, false);
                Pieza(casa.transform, "C09_Tinaco", p + Vector3.up * 0.7f, giro * Quaternion.Euler(0f, o.Rango(0f, 360f), 0f), Vector3.one * o.Rango(1.05f, 1.25f), false);
            }
            if (e.AireAzotea)
            {
                Pieza(casa.transform, "C10_AireAcondicionado", centro + lado * o.Rango(-0.35f, 0.35f) * ancho - normal * 0.9f + Vector3.up * azotea, giro, Vector3.one, false);
            }
            if (e.Antena)
            {
                Pieza(casa.transform, "CY04_AntenaAzotea", centro + lado * o.Rango(-0.4f, 0.4f) * ancho - normal * o.Rango(1.5f, 2.5f) + Vector3.up * azotea, giro, Vector3.one * 0.8f, false);
            }
            // Foco de pared junto a la puerta.
            if (e.Foco)
            {
                float s = o.Chance(0.5f) ? 1f : -1f;
                Vector3 p = centro + lado * (s * ancho * 0.36f) + normal * 0.18f + Vector3.up * (2.55f * EscalaAlto);
                FocoPared(o, casa.transform, p, giro);
            }
            if (e.Neon.HasValue)
            {
                float s = o.Chance(0.5f) ? 1f : -1f;
                Vector3 p = centro + lado * (s * ancho * 0.42f) + Vector3.up * (altoBaja + o.Rango(0.2f, 0.9f));
                Neon(o, casa.transform, p, normal, lado, e.Neon.Value, e.NeonBandera);
            }
            return azotea;
        }

        /// <summary>
        /// Llena el tramo de a a b con casas (anchos de 4 a 6 m, la última ajustada al largo). <paramref name="normal"/> apunta a la calle.
        /// Devuelve los puntos de lo alto del frente de cada casa (para colgarles cables).
        /// </summary>
        public static List<Vector3> Frente(Obra o, Transform padre, Vector3 a, Vector3 b, Vector3 normal, float fondo, System.Func<int, Estilo> estilos)
        {
            List<Vector3> altos = new List<Vector3>();
            Vector3 d = b - a;
            d.y = 0f;
            float largo = d.magnitude;
            d /= largo;
            float x = 0f;
            int i = 0;
            while (largo - x > 0.5f)
            {
                float ancho = o.Rango(4.2f, 6.2f);
                if (largo - x - ancho < 3.6f)
                {
                    ancho = largo - x;
                }
                Vector3 c = a + d * (x + ancho * 0.5f);
                float alto = Casa(o, padre, c, normal, ancho, fondo, estilos(i));
                altos.Add(c + Vector3.up * (alto - 0.4f) + normal * 0.05f);
                x += ancho;
                i++;
            }
            return altos;
        }

        private static void Pintar(GameObject pieza, int color)
        {
            if (color == 0)
            {
                return;
            }
            foreach (Renderer r in pieza.GetComponentsInChildren<Renderer>())
            {
                Material[] ms = r.sharedMaterials;
                for (int i = 0; i < ms.Length; i++)
                {
                    Material origen = ms[i];
                    if (origen == null)
                    {
                        continue;
                    }
                    (string nombre, Color c) = ColoresPiso[color];
                    ms[i] = Mat("S2c_" + origen.name + "_" + nombre, () =>
                    {
                        Material m = new Material(origen);
                        m.SetColor("_BaseColor", c);
                        return m;
                    });
                }
                r.sharedMaterials = ms;
            }
        }

        /// <summary>Ventanas cálidas: en cada hueco medido, un vidrio que brilla de noche detrás de los barrotes (al azar).</summary>
        private static void Ventanas(Obra o, Transform pieza, Medida m, float probabilidad, float altoMinimo)
        {
            foreach ((Rect r, float fondo) in m.Huecos)
            {
                if (r.yMin < altoMinimo || r.height > 2.4f || !o.Chance(probabilidad))
                {
                    continue;
                }
                GameObject v = GameObject.CreatePrimitive(PrimitiveType.Quad);
                Object.DestroyImmediate(v.GetComponent<Collider>());
                v.name = "VentanaCalida";
                v.transform.SetParent(pieza, false);
                // El quad mira a -Z local (su cara visible), como el frente de la pieza; queda 1 cm antes del vidrio.
                v.transform.localPosition = new Vector3(r.center.x, r.center.y, fondo);
                v.transform.localRotation = Quaternion.identity;
                v.transform.localScale = new Vector3(r.width * 0.96f, r.height * 0.96f, 1f);
                MeshRenderer mr = v.GetComponent<MeshRenderer>();
                mr.sharedMaterial = MatVentana(o.Azar.Next(3));
                mr.shadowCastingMode = ShadowCastingMode.Off;
                o.Ventanas++;
            }
        }

        // ================================================================ Detalles de pared

        public static void FocoPared(Obra o, Transform padre, Vector3 p, Quaternion giro)
        {
            GameObject f = Pieza(padre, "N01", p, giro, Vector3.one, false);
            f.name = "FocoPared";
            Luz(f.transform, p + giro * Vector3.back * 0.25f, LightType.Point, Calido, 4.5f, 6.5f, 0f);
        }

        /// <summary>Letrero de neón chico sin texto (§1): caja C15b con su cara de neón; de bandera (sale de la pared) o plano.</summary>
        public static void Neon(Obra o, Transform padre, Vector3 p, Vector3 normal, Vector3 lado, Color color, bool bandera)
        {
            o.Neones++;
            GameObject raiz = new GameObject("Neon");
            raiz.transform.SetParent(padre, false);
            Quaternion giro = bandera ? Quaternion.LookRotation(lado) : Quaternion.LookRotation(-normal);
            Vector3 centro = bandera ? p + normal * 0.62f : p + normal * 0.16f;
            raiz.transform.SetPositionAndRotation(centro, giro);
            GameObject caja = Pieza(raiz.transform, "C15b_LetreroBandera", centro - Vector3.up * 0.42f, giro, new Vector3(0.75f, 0.62f, 1f), false);
            caja.name = "Caja";
            Material cara = MatNeon(color, o.Azar.Next(3));
            foreach (float s in bandera ? new[] { -1f, 1f } : new[] { -1f })
            {
                GameObject q = GameObject.CreatePrimitive(PrimitiveType.Quad);
                Object.DestroyImmediate(q.GetComponent<Collider>());
                q.name = "Cara";
                q.transform.SetParent(raiz.transform, false);
                q.transform.localPosition = new Vector3(0f, -0.05f, s * 0.16f);
                q.transform.localRotation = s < 0f ? Quaternion.identity : Quaternion.Euler(0f, 180f, 0f);
                q.transform.localScale = new Vector3(0.52f, 0.6f, 1f);
                q.GetComponent<MeshRenderer>().sharedMaterial = cara;
                q.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            }
            Luz(raiz.transform, centro + normal * 0.45f, LightType.Point, color, 0.35f, 3.5f, 0f);
        }

        /// <summary>Una luz que se prende de noche (LuzNocturna la apaga de día).</summary>
        public static Light Luz(Transform padre, Vector3 p, LightType tipo, Color color, float intensidad, float rango, float angulo, Vector3? mira = null)
        {
            GameObject go = new GameObject("Luz");
            go.transform.SetParent(padre, true);
            go.transform.position = p;
            if (mira.HasValue)
            {
                go.transform.rotation = Quaternion.LookRotation(mira.Value);
            }
            Light l = go.AddComponent<Light>();
            l.type = tipo;
            l.color = color;
            l.intensity = intensidad;
            l.range = rango;
            if (tipo == LightType.Spot)
            {
                l.spotAngle = angulo;
                l.innerSpotAngle = angulo * 0.55f;
            }
            l.shadows = LightShadows.None;
            // Todas las luces chicas alumbran también la capa de los postes (los postes solo no reciben la luz de su propia lámpara).
            UniversalAdditionalLightData d = l.GetUniversalAdditionalLightData();
            d.renderingLayers = uint.MaxValue;
            l.enabled = false;
            LuzNocturna ln = go.AddComponent<LuzNocturna>();
            SerializedObject so = new SerializedObject(ln);
            so.FindProperty("luz").objectReferenceValue = l;
            so.ApplyModifiedPropertiesWithoutUndo();
            return l;
        }

        // ================================================================ Postes, cables y halos

        /// <summary>
        /// Poste C07 con el brazo hacia la calle, su luz de sodio desde la cabeza de la lámpara (como T-855), el halo en la niebla, la mica que
        /// brilla y, si toca, transformador. Devuelve la punta del poste (para los cables).
        /// </summary>
        public static Vector3 Poste(Obra o, Transform padre, Vector3 pie, Vector3 haciaCalle, float piso, bool transformador, Color? color = null)
        {
            o.Postes++;
            haciaCalle.y = 0f;
            haciaCalle.Normalize();
            GameObject poste = Pieza(padre, "C07_PosteLuz", pie, Quaternion.identity, Vector3.one);
            poste.name = "Poste_" + o.Postes;
            MeshFilter mf = poste.GetComponentsInChildren<MeshFilter>().OrderByDescending(m => m.sharedMesh.vertexCount).First();
            if (ColocarS2.CabezaLampara(mf, out Vector3 cabeza))
            {
                Vector3 brazo = cabeza - pie;
                brazo.y = 0f;
                float giro = Vector3.SignedAngle(brazo, haciaCalle, Vector3.up);
                poste.transform.rotation = Quaternion.Euler(0f, giro, 0f);
                ColocarS2.CabezaLampara(mf, out cabeza);
            }
            else
            {
                cabeza = pie + Vector3.up * 6.6f + haciaCalle * 1.2f;
            }
            foreach (Renderer r in poste.GetComponentsInChildren<Renderer>())
            {
                r.renderingLayerMask = 1u << ColocarS2.CapaPostes;
            }
            Light l = Luz(poste.transform, cabeza, LightType.Spot, color ?? Sodio, IntensidadPoste, 20f, 112f, Vector3.down);
            UniversalAdditionalLightData datosLuz = l.GetUniversalAdditionalLightData();
            datosLuz.renderingLayers = uint.MaxValue & ~(1u << ColocarS2.CapaPostes);
            Halo(o, poste.transform, cabeza, piso, color ?? Sodio);
            if (transformador)
            {
                Vector3 atras = -haciaCalle;
                Pieza(poste.transform, "C08_Transformador", pie + atras * 0.62f + Vector3.up * 4.3f, Quaternion.LookRotation(haciaCalle), Vector3.one * 0.9f, false);
            }
            return pie + Vector3.up * 6.75f;
        }

        public static void Halo(Obra o, Transform padre, Vector3 cabeza, float piso, Color color)
        {
            float alto = cabeza.y - piso;
            GameObject h = new GameObject("Halo");
            h.transform.SetParent(padre, true);
            h.transform.position = new Vector3(cabeza.x, piso, cabeza.z);
            h.AddComponent<MeshFilter>().sharedMesh = MallaCono();
            MeshRenderer mr = h.AddComponent<MeshRenderer>();
            mr.sharedMaterial = Mat(color == Sodio ? "S2c_Halo" : "S2c_Halo_Frio", () =>
            {
                Material m = new Material(Shader.Find("MechaCyberpunk/Mundo/HaloLuz"));
                m.SetColor("_Color", color);
                m.SetFloat("_Intensidad", 0.2f);
                m.SetFloat("_Borde", 3.4f);
                m.SetFloat("_Caida", 2f);
                m.SetTexture("_Ruido", TexMuro());
                m.SetTextureScale("_Ruido", new Vector2(3f, 1f));
                return m;
            });
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;
            h.transform.localScale = new Vector3(alto * 0.62f, alto, alto * 0.62f);
            // Mica de la lámpara: un óvalo que brilla debajo de la cabeza.
            GameObject mica = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.DestroyImmediate(mica.GetComponent<Collider>());
            mica.name = "Mica";
            mica.transform.SetParent(padre, true);
            mica.transform.position = cabeza + Vector3.up * 0.06f;
            mica.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
            mica.transform.localScale = new Vector3(0.42f, 0.3f, 1f);
            mica.GetComponent<MeshRenderer>().sharedMaterial = Mat("S2c_Mica", () => Emision(new Color(4f, 2.6f, 1.2f), TexOvalo()));
            mica.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
        }

        private static Mesh cono;

        /// <summary>Cono abierto de alto 1 y radio de base 1 (la punta mide 0.06), con UV.y 0 arriba y 1 abajo.</summary>
        private static Mesh MallaCono()
        {
            string ruta = CarpetaMallas + "/S2c_Cono.asset";
            if (cono != null)
            {
                return cono;
            }
            cono = AssetDatabase.LoadAssetAtPath<Mesh>(ruta);
            if (cono != null)
            {
                return cono;
            }
            const int lados = 28;
            const float punta = 0.06f;
            List<Vector3> v = new List<Vector3>();
            List<Vector3> n = new List<Vector3>();
            List<Vector2> uv = new List<Vector2>();
            List<int> t = new List<int>();
            for (int i = 0; i <= lados; i++)
            {
                float a = i * Mathf.PI * 2f / lados;
                Vector3 r = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                Vector3 normal = (r + Vector3.up * (1f - punta)).normalized;
                v.Add(r * punta + Vector3.up);
                v.Add(r);
                n.Add(normal);
                n.Add(normal);
                uv.Add(new Vector2(i / (float)lados, 0f));
                uv.Add(new Vector2(i / (float)lados, 1f));
                if (i < lados)
                {
                    int k = i * 2;
                    t.AddRange(new[] { k, k + 2, k + 1, k + 1, k + 2, k + 3 });
                }
            }
            cono = new Mesh { name = "S2c_Cono" };
            cono.SetVertices(v);
            cono.SetNormals(n);
            cono.SetUVs(0, uv);
            cono.SetTriangles(t, 0);
            cono.RecalculateBounds();
            AssetDatabase.CreateAsset(cono, ruta);
            return cono;
        }

        /// <summary>Un cable colgante de a a b (tubo de 4 lados); se junta con los demás en una sola malla al terminar la obra.</summary>
        public static void Cable(Obra o, Vector3 a, Vector3 b, float flecha, float radio = 0.03f)
        {
            o.Cables++;
            int tramos = Mathf.Clamp(Mathf.CeilToInt(Vector3.Distance(a, b) / 1.2f), 6, 24);
            Vector3[] p = CalculoCatenaria.Puntos(a, b, flecha, tramos);
            const int lados = 4;
            int inicio = o.CablesV.Count;
            for (int i = 0; i < p.Length; i++)
            {
                Vector3 dir = (i < p.Length - 1 ? p[i + 1] - p[i] : p[i] - p[i - 1]).normalized;
                Vector3 u = Vector3.Cross(dir, Vector3.up).normalized;
                if (u.sqrMagnitude < 0.01f)
                {
                    u = Vector3.right;
                }
                Vector3 w = Vector3.Cross(u, dir).normalized;
                for (int k = 0; k < lados; k++)
                {
                    float ang = k * Mathf.PI * 2f / lados;
                    Vector3 n = u * Mathf.Cos(ang) + w * Mathf.Sin(ang);
                    o.CablesV.Add(p[i] + n * radio - o.Raiz.position);
                    o.CablesN.Add(n);
                }
            }
            for (int i = 0; i < p.Length - 1; i++)
            {
                for (int k = 0; k < lados; k++)
                {
                    int a0 = inicio + i * lados + k;
                    int a1 = inicio + i * lados + (k + 1) % lados;
                    int b0 = a0 + lados;
                    int b1 = a1 + lados;
                    o.CablesI.AddRange(new[] { a0, b0, a1, a1, b0, b1 });
                }
            }
        }

        // ================================================================ Piso: losas, líneas, charcos

        /// <summary>Caja con UV en metros (una textura cada <paramref name="metrosUv"/> m en todas sus caras) y BoxCollider si se pide.</summary>
        public static GameObject Losa(Obra o, Transform padre, string nombre, Vector3 centro, Quaternion giro, Vector3 tam, Material material, float metrosUv, bool collider)
        {
            GameObject go = new GameObject(nombre);
            go.transform.SetParent(padre, false);
            go.transform.SetPositionAndRotation(centro, giro);
            Mesh m = MallaCaja(tam, metrosUv);
            m.name = o.Nombre + "_" + nombre + "_" + o.Mallas.Count;
            o.Mallas.Add(m);
            go.AddComponent<MeshFilter>().sharedMesh = m;
            go.AddComponent<MeshRenderer>().sharedMaterial = material;
            if (collider)
            {
                go.AddComponent<BoxCollider>().size = tam;
            }
            return go;
        }

        private static Mesh MallaCaja(Vector3 tam, float metrosUv)
        {
            Vector3 h = tam * 0.5f;
            List<Vector3> v = new List<Vector3>();
            List<Vector3> n = new List<Vector3>();
            List<Vector2> uv = new List<Vector2>();
            List<int> t = new List<int>();
            void Cara(Vector3 normal, Vector3 eu, Vector3 ev, float su, float sv)
            {
                int k = v.Count;
                Vector3 c = Vector3.Scale(normal, h);
                Vector3 du = eu * (su * 0.5f);
                Vector3 dv = ev * (sv * 0.5f);
                v.Add(c - du - dv); v.Add(c + du - dv); v.Add(c + du + dv); v.Add(c - du + dv);
                for (int i = 0; i < 4; i++)
                {
                    n.Add(normal);
                }
                uv.Add(new Vector2(0f, 0f)); uv.Add(new Vector2(su / metrosUv, 0f)); uv.Add(new Vector2(su / metrosUv, sv / metrosUv)); uv.Add(new Vector2(0f, sv / metrosUv));
                t.AddRange(new[] { k, k + 1, k + 2, k, k + 2, k + 3 });
            }
            Cara(Vector3.up, Vector3.right, Vector3.back, tam.x, tam.z);
            Cara(Vector3.down, Vector3.right, Vector3.forward, tam.x, tam.z);
            Cara(Vector3.forward, Vector3.left, Vector3.up, tam.x, tam.y);
            Cara(Vector3.back, Vector3.right, Vector3.up, tam.x, tam.y);
            Cara(Vector3.right, Vector3.forward, Vector3.up, tam.z, tam.y);
            Cara(Vector3.left, Vector3.back, Vector3.up, tam.z, tam.y);
            Mesh m = new Mesh();
            m.SetVertices(v);
            m.SetNormals(n);
            m.SetUVs(0, uv);
            m.SetTriangles(t, 0);
            m.RecalculateTangents();
            m.RecalculateBounds();
            return m;
        }

        /// <summary>Una tira pintada en el piso (línea de la calle), 1 cm sobre el arroyo.</summary>
        public static void Linea(Transform padre, Vector3 a, Vector3 b, float ancho, float piso)
        {
            GameObject q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.DestroyImmediate(q.GetComponent<Collider>());
            q.name = "Linea";
            q.transform.SetParent(padre, true);
            Vector3 d = b - a;
            q.transform.position = (a + b) * 0.5f + Vector3.up * (piso + 0.008f - (a.y + b.y) * 0.5f);
            q.transform.rotation = Quaternion.LookRotation(Vector3.down, d.normalized);
            q.transform.localScale = new Vector3(ancho, d.magnitude, 1f);
            MeshRenderer mr = q.GetComponent<MeshRenderer>();
            mr.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Arte/Calle/Lineas/M03_LineaBlanca.mat");
            mr.shadowCastingMode = ShadowCastingMode.Off;
        }

        /// <summary>Charco que refleja (capa Water, que la cámara del reflejo no ve), con una de 4 formas.</summary>
        public static void Charco(Obra o, Transform padre, Vector3 centro, float largo, float ancho, float giro)
        {
            o.Charcos++;
            GameObject q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.DestroyImmediate(q.GetComponent<Collider>());
            q.name = "Charco";
            q.layer = ReflejoCharcos.CapaCharcos;
            q.transform.SetParent(padre, true);
            q.transform.position = centro + Vector3.up * 0.012f;
            q.transform.rotation = Quaternion.Euler(90f, giro, 0f);
            q.transform.localScale = new Vector3(ancho, largo, 1f);
            MeshRenderer mr = q.GetComponent<MeshRenderer>();
            int forma = o.Azar.Next(4);
            mr.sharedMaterial = Mat("S2c_Charco_" + forma, () =>
            {
                Material m = new Material(Shader.Find("MechaCyberpunk/Mundo/Charco"));
                m.SetTexture("_Mascara", TexCharco(forma));
                return m;
            });
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;
            q.AddComponent<CharcoVisible>();
        }

        /// <summary>Altura del piso (rayo hacia abajo); <paramref name="siNo"/> si no hay nada.</summary>
        public static float Suelo(Vector3 p, float siNo)
        {
            Physics.SyncTransforms();
            RaycastHit[] hits = Physics.RaycastAll(new Vector3(p.x, 30f, p.z), Vector3.down, 40f);
            return hits.Length == 0 ? siNo : hits.Max(h => h.point.y);
        }

        // ================================================================ Materiales y texturas

        public static Material Mat(string nombre, System.Func<Material> crear)
        {
            string ruta = Carpeta + "/" + nombre + ".mat";
            Material m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (m != null)
            {
                return m;
            }
            m = crear();
            m.name = nombre;
            AssetDatabase.CreateAsset(m, ruta);
            return m;
        }

        /// <summary>Borra los materiales generados para que se vuelvan a hacer con los valores de ahora (los usa la obra que se rehace).</summary>
        public static void OlvidarMateriales(params string[] prefijos)
        {
            foreach (string g in AssetDatabase.FindAssets("t:Material", new[] { Carpeta }))
            {
                string ruta = AssetDatabase.GUIDToAssetPath(g);
                if (prefijos.Any(p => Path.GetFileName(ruta).StartsWith(p)))
                {
                    AssetDatabase.DeleteAsset(ruta);
                }
            }
            cono = null;
        }

        public static Material Lit(Color color, float suavidad, Texture textura = null)
        {
            Material m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            m.SetColor("_BaseColor", color);
            m.SetFloat("_Smoothness", suavidad);
            if (textura != null)
            {
                m.SetTexture("_BaseMap", textura);
            }
            return m;
        }

        public static Material Emision(Color colorNoche, Texture textura)
        {
            Material m = new Material(Shader.Find("MechaCyberpunk/Mundo/EmisionNoche"));
            m.SetColor("_Color", colorNoche);
            m.SetTexture("_BaseMap", textura);
            return m;
        }

        public static Material MatAsfalto()
        {
            return Mat("S2c_Asfalto", () =>
            {
                Material m = new Material(AssetDatabase.LoadAssetAtPath<Material>("Assets/_Proyecto/Mundo/Materiales/Barrio_Asfalto.mat"));
                m.SetTextureScale("_BaseMap", Vector2.one);
                m.SetTextureScale("_BumpMap", Vector2.one);
                m.SetFloat("_Smoothness", 0.7f);
                m.SetColor("_BaseColor", new Color(0.55f, 0.55f, 0.6f));
                return m;
            });
        }

        public static Material MatBanqueta()
        {
            return Mat("S2c_Banqueta", () =>
            {
                Material m = new Material(AssetDatabase.LoadAssetAtPath<Material>("Assets/Arte/Calle/Banqueta/T506_Barrio_Banqueta_T01_1x10.mat"));
                m.SetTextureScale("_BaseMap", Vector2.one);
                m.SetTextureScale("_BumpMap", Vector2.one);
                m.SetFloat("_Smoothness", 0.6f);
                m.SetColor("_BaseColor", new Color(0.5f, 0.5f, 0.52f));
                return m;
            });
        }

        public static Material MatCuerpo(int color)
        {
            (string nombre, Color c) = ColoresPiso[color];
            return Mat("S2c_Cuerpo_" + nombre, () => Lit(new Color(c.r * 0.78f, c.g * 0.78f, c.b * 0.78f), 0.15f, TexMuro()));
        }

        public static Material MatLadrillo() => Mat("S2c_Ladrillo", () => Lit(Color.white, 0.12f, TexLadrillo()));

        private static Material MatVentana(int variante)
        {
            Color[] colores = { new Color(2.6f, 1.55f, 0.7f), new Color(2.2f, 1.6f, 0.95f), new Color(2.9f, 1.4f, 0.55f) };
            return Mat("S2c_Ventana_" + variante, () => Emision(colores[variante], TexVentana(variante)));
        }

        private static Material MatNeon(Color color, int forma)
        {
            string nombre = color == Magenta ? "Magenta" : "Turquesa";
            return Mat("S2c_Neon_" + nombre + "_" + forma, () =>
            {
                Material m = Emision(color * 1.7f, TexNeon(forma));
                m.SetColor("_ColorDia", new Color(0.12f, 0.12f, 0.14f));
                return m;
            });
        }

        // ---- texturas que se dibujan en código (se guardan como PNG y se importan)

        private static Texture2D Guardar(string nombre, Texture2D t, bool srgb, TextureWrapMode envolver)
        {
            string ruta = CarpetaTexturas + "/" + nombre + ".png";
            File.WriteAllBytes(ruta, t.EncodeToPNG());
            Object.DestroyImmediate(t);
            AssetDatabase.ImportAsset(ruta);
            TextureImporter imp = (TextureImporter)AssetImporter.GetAtPath(ruta);
            imp.sRGBTexture = srgb;
            imp.wrapMode = envolver;
            imp.mipmapEnabled = true;
            imp.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(ruta);
        }

        private static Texture2D Cargar(string nombre) => AssetDatabase.LoadAssetAtPath<Texture2D>(CarpetaTexturas + "/" + nombre + ".png");

        private static float Ruido(float x, float y) => Mathf.PerlinNoise(x, y);

        public static Texture2D TexCharco(int forma)
        {
            string nombre = "S2c_Charco_" + forma;
            Texture2D ya = Cargar(nombre);
            if (ya != null)
            {
                return ya;
            }
            const int n = 256;
            System.Random r = new System.Random(100 + forma);
            List<Vector3> gotas = new List<Vector3>();
            int cuantas = 3 + r.Next(4);
            for (int i = 0; i < cuantas; i++)
            {
                gotas.Add(new Vector3(0.3f + 0.4f * (float)r.NextDouble(), 0.25f + 0.5f * (float)r.NextDouble(), 0.12f + 0.14f * (float)r.NextDouble()));
            }
            Texture2D t = new Texture2D(n, n, TextureFormat.RGBA32, true);
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    float u = x / (float)n, v = y / (float)n;
                    float campo = gotas.Sum(g => Mathf.Exp(-((u - g.x) * (u - g.x) + (v - g.y) * (v - g.y)) / (g.z * g.z)));
                    campo += (Ruido(u * 9f + forma * 7f, v * 9f) - 0.5f) * 0.45f;
                    float borde = Mathf.Min(Mathf.Min(u, 1f - u), Mathf.Min(v, 1f - v)) * 8f;
                    float a = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((campo - 0.55f) * 5f)) * Mathf.Clamp01(borde);
                    t.SetPixel(x, y, new Color(a, a, a, a));
                }
            }
            t.Apply();
            return Guardar(nombre, t, false, TextureWrapMode.Clamp);
        }

        private static Texture2D TexVentana(int variante)
        {
            string nombre = "S2c_Ventana_" + variante;
            Texture2D ya = Cargar(nombre);
            if (ya != null)
            {
                return ya;
            }
            const int n = 128;
            Texture2D t = new Texture2D(n, n, TextureFormat.RGBA32, true);
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    float u = x / (float)n, v = y / (float)n;
                    // Luz de cuarto: más fuerte arriba al centro, con una cortina a un lado (cada variante distinta).
                    float luz = 0.55f + 0.45f * v - 0.35f * Mathf.Abs(u - 0.5f);
                    float cortina = variante == 0 ? Mathf.SmoothStep(0.25f, 0.33f, u) : variante == 1 ? 1f - Mathf.SmoothStep(0.62f, 0.7f, u) * 0.75f : 0.85f + 0.15f * Mathf.Sin(u * 40f);
                    float c = Mathf.Clamp01(luz) * Mathf.Lerp(0.35f, 1f, cortina) * (0.85f + 0.15f * Ruido(u * 6f, v * 6f + variante));
                    // Barrotes y marco oscuros (la ventana iluminada tapa los de la pieza): 5 verticales, uno horizontal.
                    bool barrote = (u * 6f) % 1f < 0.14f || Mathf.Abs(v - 0.55f) < 0.025f || u < 0.05f || u > 0.95f || v < 0.05f || v > 0.95f;
                    if (barrote)
                    {
                        c = 0.04f;
                    }
                    t.SetPixel(x, y, new Color(c, c * 0.95f, c * 0.88f, 1f));
                }
            }
            t.Apply();
            return Guardar(nombre, t, true, TextureWrapMode.Clamp);
        }

        private static Texture2D TexNeon(int forma)
        {
            string nombre = "S2c_Neon_" + forma;
            Texture2D ya = Cargar(nombre);
            if (ya != null)
            {
                return ya;
            }
            const int n = 128;
            Texture2D t = new Texture2D(n, n, TextureFormat.RGBA32, true);
            float Tubo(float d) => Mathf.Clamp01(1f - Mathf.Abs(d) / 0.035f) + 0.25f * Mathf.Clamp01(1f - Mathf.Abs(d) / 0.12f);
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    float u = x / (float)n * 2f - 1f, v = y / (float)n * 2f - 1f;
                    // Marco redondeado y un glifo sin letras (onda, rayo o círculo).
                    float marco = Mathf.Max(Mathf.Abs(u), Mathf.Abs(v)) - 0.82f;
                    float glifo = forma == 0 ? v - 0.35f * Mathf.Sin(u * 5f) : forma == 1 ? Mathf.Abs(u) + Mathf.Abs(v) * 0.6f - 0.5f : new Vector2(u, v).magnitude - 0.45f;
                    float c = Mathf.Max(Tubo(marco), Mathf.Abs(u) < 0.62f && Mathf.Abs(v) < 0.62f ? Tubo(glifo) : 0f);
                    float fondo = 0.03f;
                    t.SetPixel(x, y, new Color(Mathf.Max(c, fondo), Mathf.Max(c, fondo), Mathf.Max(c, fondo), 1f));
                }
            }
            t.Apply();
            return Guardar(nombre, t, true, TextureWrapMode.Clamp);
        }

        /// <summary>Cruz de farmacia de neón (tubo con halo) sobre negro.</summary>
        public static Texture2D TexCruz()
        {
            const string nombre = "S2c_Cruz";
            Texture2D ya = Cargar(nombre);
            if (ya != null)
            {
                return ya;
            }
            const int n = 128;
            Texture2D t = new Texture2D(n, n, TextureFormat.RGBA32, true);
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    float u = x / (float)n * 2f - 1f, v = y / (float)n * 2f - 1f;
                    float brazo = Mathf.Min(Mathf.Max(Mathf.Abs(u) - 0.8f, Mathf.Abs(v) - 0.26f), Mathf.Max(Mathf.Abs(u) - 0.26f, Mathf.Abs(v) - 0.8f));
                    float c = Mathf.Clamp01(1f - Mathf.Abs(brazo) / 0.06f) + 0.35f * Mathf.Clamp01(1f - Mathf.Max(brazo, 0f) / 0.2f) * (brazo < 0f ? 1f : 0.5f);
                    t.SetPixel(x, y, new Color(c, c, c, 1f));
                }
            }
            t.Apply();
            return Guardar(nombre, t, true, TextureWrapMode.Clamp);
        }

        public static Texture2D TexOvalo()
        {
            const string nombre = "S2c_Ovalo";
            Texture2D ya = Cargar(nombre);
            if (ya != null)
            {
                return ya;
            }
            const int n = 64;
            Texture2D t = new Texture2D(n, n, TextureFormat.RGBA32, true);
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    float u = x / (float)n * 2f - 1f, v = y / (float)n * 2f - 1f;
                    float c = Mathf.Clamp01(1.2f - new Vector2(u, v).magnitude * 1.25f);
                    t.SetPixel(x, y, new Color(c, c, c, 1f));
                }
            }
            t.Apply();
            return Guardar(nombre, t, true, TextureWrapMode.Clamp);
        }

        private static Texture2D TexLadrillo()
        {
            const string nombre = "S2c_Ladrillo";
            Texture2D ya = Cargar(nombre);
            if (ya != null)
            {
                return ya;
            }
            const int n = 256;
            Texture2D t = new Texture2D(n, n, TextureFormat.RGBA32, true);
            System.Random r = new System.Random(7);
            Color[] tonos = Enumerable.Range(0, 64).Select(i => Color.Lerp(new Color(0.52f, 0.24f, 0.16f), new Color(0.68f, 0.36f, 0.24f), (float)r.NextDouble())).ToArray();
            const int filas = 8;
            for (int y = 0; y < n; y++)
            {
                int fila = y * filas / n;
                float fy = (y * filas / (float)n) % 1f;
                for (int x = 0; x < n; x++)
                {
                    float desplaza = fila % 2 == 0 ? 0f : 0.5f;
                    float bx = (x / (float)n * 4f + desplaza);
                    int col = Mathf.FloorToInt(bx);
                    float fx = bx % 1f;
                    bool junta = fy < 0.1f || fx < 0.05f;
                    Color c = junta ? new Color(0.55f, 0.52f, 0.48f) : tonos[(fila * 7 + col) % tonos.Length] * (0.88f + 0.24f * Ruido(x * 0.08f, y * 0.08f));
                    c.a = 1f;
                    t.SetPixel(x, y, c);
                }
            }
            t.Apply();
            return Guardar(nombre, t, true, TextureWrapMode.Repeat);
        }

        private static Texture2D TexMuro()
        {
            const string nombre = "S2c_Muro";
            Texture2D ya = Cargar(nombre);
            if (ya != null)
            {
                return ya;
            }
            const int n = 256;
            Texture2D t = new Texture2D(n, n, TextureFormat.RGBA32, true);
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    float u = x / (float)n, v = y / (float)n;
                    // Aplanado pintado y sucio: manchas grandes, escurrido vertical y grano.
                    float manchas = Ruido(u * 3f, v * 3f) * 0.5f + Ruido(u * 11f, v * 11f) * 0.25f;
                    float escurrido = Ruido(u * 40f, v * 2f) * 0.12f;
                    float c = Mathf.Clamp01(0.78f + manchas * 0.28f - escurrido);
                    t.SetPixel(x, y, new Color(c, c * 0.98f, c * 0.95f, 1f));
                }
            }
            t.Apply();
            return Guardar(nombre, t, true, TextureWrapMode.Repeat);
        }
    }
}
