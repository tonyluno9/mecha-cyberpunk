using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MechaCyberpunk.Core.Mundo;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-1360: barrido automático de bugs del mapa nuevo (Malecón, Cerro, Autopista, Puerto y las franjas de 30 m alrededor de las uniones U1..U9).
    /// <see cref="Barrer"/> abre Ciudad_Prologo, revisa nueve cosas (flotantes y enterrados, encimados y z-fighting, huecos y vacíos, escalones,
    /// muros invisibles, texturas rotas, calles que no conectan, islas sin salida y luces que no corresponden) y escribe Logs/barrido-mapa.json y
    /// Logs/barrido-mapa.txt. NO guarda la escena ni modifica nada (los colliders temporales y el NavMesh en memoria se quitan al terminar).
    /// <see cref="Fotos"/> toma una captura de cada hallazgo de gravedad 3 o 4 (máx 60, agrupando los que están a menos de 8 m).
    /// Gravedad: 1 aviso, 2 raro, 3 se nota jugando, 4 bloquea o rompe.
    /// </summary>
    public static class BarridoMapaT1360
    {
        public const string RutaJson = "Logs/barrido-mapa.json";
        public const string RutaTexto = "Logs/barrido-mapa.txt";
        public const string CarpetaFotos = "docs/capturas/bugs-18nov";
        public const int MaximoFotos = 60;
        public const float RadioAgrupado = 8f;
        public const float DistanciaFoto = 6f;
        public const float FranjaUnion = 30f;

        public const float UmbralEscalon = 0.12f;
        public const float UmbralEscalonGrave = 0.35f;
        public const float UmbralFlotante = 0.12f;
        public const float UmbralEnterrado = 0.3f;
        public const float UmbralHueco = 0.5f;
        public const float UmbralPisoLinea = 0.3f;

        private const float HoraDeFoto = 14f;
        private const float EmisionMinima = 0.05f;

        private static readonly string[] RaicesIgnoradas = { "JugadorAPie", "AMB_", "Trafico", "MechaEstacionado", "T541_Torres", "S2_PandillasAcoso" };
        private static readonly string[] NombresIgnorados = { "Cable", "Nube", "Charco", "Linea", "Cebra", "Raya", "Sombra", "Brillo", "Cielo", "Fondo", "Halo", "Decal", "Agua", "Mar" };
        private static readonly string[] NombresSolidos = { "Casa", "Muro", "Columna", "Barandal", "Contenedor", "Poste", "Edificio", "Fachada", "Pared", "Pilar" };
        private static readonly string[] NombresLampara = { "Farola", "Lampara", "Lámpara", "Foco", "Poste", "Neon", "Neón", "Letrero", "Luminaria", "Faro", "Holo", "Pantalla", "Semaforo", "Semáforo", "Luz" };
        private static readonly string[] NombresVehiculo = { "Carro", "Moto", "Vehiculo", "Vehículo", "Camion", "Camión" };

        // Zonas nuevas (plan-zonas-18nov): nombre, x mínima, x máxima, z mínima, z máxima. La primera que contiene el punto gana.
        private static readonly (string nombre, float x0, float x1, float z0, float z1)[] Rectangulos =
        {
            ("Malecon", -330f, 175f, -262f, -205f),
            ("Puerto", 115f, 345f, -262f, -45f),
            ("Cerro", -480f, -295f, -170f, 215f),
            ("Autopista", -330f, 345f, 235f, 290f),
            ("Autopista", 285f, 345f, 0f, 235f),
        };

        // Puntos de unión (plan-zonas): a menos de FranjaUnion de uno de ellos, fuera de las zonas nuevas, es "Union-Uk".
        private static readonly (string nombre, Vector3 punto)[] Uniones =
        {
            ("Union-U1", new Vector3(-47f, 0f, -213f)), ("Union-U2", new Vector3(47f, 0f, -213f)),
            ("Union-U3", new Vector3(-300f, 0f, -205f)), ("Union-U3", new Vector3(-330f, 0f, -150f)), ("Union-U3", new Vector3(-305f, 0f, -120f)),
            ("Union-U4", new Vector3(-277f, 0f, 0f)), ("Union-U5", new Vector3(160f, 0f, -215f)),
            ("Union-U6", new Vector3(-330f, 10f, 250f)), ("Union-U6", new Vector3(-360f, 10f, 185f)),
            ("Union-U7", new Vector3(-107f, 0f, 201f)), ("Union-U7", new Vector3(-120f, 0f, 215f)),
            ("Union-U8", new Vector3(300f, 0f, 190f)), ("Union-U8", new Vector3(300f, 0f, 60f)), ("Union-U8", new Vector3(300f, 0f, 0f)), ("Union-U8", new Vector3(300f, 0f, -45f)),
            ("Union-U9", new Vector3(275f, 0f, 0f)),
        };

        // Calles de las afueras viejas (aproximadas, del plan): sirven para ver si una calle de zona termina en otra y para muestrear los cruces.
        private static readonly (Vector3 a, Vector3 b)[] CallesViejas =
        {
            (new Vector3(-47f, 0f, -201f), new Vector3(-47f, 0f, 201f)), (new Vector3(47f, 0f, -201f), new Vector3(47f, 0f, 201f)),
            (new Vector3(-249f, 0f, 0f), new Vector3(249f, 0f, 0f)),
            (new Vector3(-107f, 0f, -194f), new Vector3(107f, 0f, -194f)), (new Vector3(-107f, 0f, 194f), new Vector3(107f, 0f, 194f)),
            (new Vector3(-107f, 0f, 160f), new Vector3(-107f, 0f, 201f)),
            (new Vector3(-242f, 0f, -60f), new Vector3(-242f, 0f, 60f)), (new Vector3(242f, 0f, -60f), new Vector3(242f, 0f, 60f)),
        };

        [System.Serializable]
        public sealed class Hallazgo
        {
            public string tipo;
            public int gravedad;
            public string zona;
            public Vector3 posicion;
            public string objeto;
            public string detalle;
            public int cuenta = 1;
        }

        [System.Serializable]
        public sealed class Informe
        {
            public List<Hallazgo> hallazgos = new List<Hallazgo>();
        }

        // ---------------------------------------------------------------- Funciones puras

        /// <summary>Zona de un punto: Malecon, Cerro, Autopista, Puerto, Union-Uk (franja de 30 m), Viejo (afueras viejas) o Fuera.</summary>
        public static string ZonaPorPosicion(Vector3 p)
        {
            foreach (var r in Rectangulos)
            {
                if (p.x >= r.x0 && p.x <= r.x1 && p.z >= r.z0 && p.z <= r.z1)
                {
                    return r.nombre;
                }
            }
            foreach (var u in Uniones)
            {
                Vector2 d = new Vector2(p.x - u.punto.x, p.z - u.punto.z);
                if (d.sqrMagnitude <= FranjaUnion * FranjaUnion)
                {
                    return u.nombre;
                }
            }
            return Mathf.Abs(p.x) <= 260f && p.z >= -210f && p.z <= 210f ? "Viejo" : "Fuera";
        }

        /// <summary>true si la zona es de las que se revisan (zonas nuevas y franjas de unión).</summary>
        public static bool EnAlcance(string zona)
        {
            return zona != "Viejo" && zona != "Fuera";
        }

        /// <summary>Índices (de la segunda muestra del par) donde la altura salta más que el umbral. Las muestras sin piso (NaN) cortan la comparación.</summary>
        public static List<int> EscalonesEnPerfil(IList<float> alturas, float umbral)
        {
            List<int> salida = new List<int>();
            bool hayPrevio = false;
            float previo = 0f;
            for (int i = 0; i < alturas.Count; i++)
            {
                float h = alturas[i];
                if (float.IsNaN(h))
                {
                    hayPrevio = false;
                    continue;
                }
                if (hayPrevio && Mathf.Abs(h - previo) > umbral)
                {
                    salida.Add(i);
                }
                previo = h;
                hayPrevio = true;
            }
            return salida;
        }

        /// <summary>Gravedad de un salto de altura (m): más de 0.35 es 4, más de 0.12 es 3, menos 0.</summary>
        public static int GravedadEscalon(float salto)
        {
            float s = Mathf.Abs(salto);
            return s > UmbralEscalonGrave ? 4 : (s > UmbralEscalon ? 3 : 0);
        }

        /// <summary>Intersección sobre unión de dos cajas (0..1). Una caja plana cuenta con 1 cm de grosor mínimo.</summary>
        public static float IoU(Bounds a, Bounds b)
        {
            Vector3 sa = Vector3.Max(a.size, Vector3.one * 0.01f);
            Vector3 sb = Vector3.Max(b.size, Vector3.one * 0.01f);
            Vector3 min = Vector3.Max(a.min, b.min);
            Vector3 max = Vector3.Min(a.max, b.max);
            Vector3 i = max - min;
            if (i.x <= 0f || i.y <= 0f || i.z <= 0f)
            {
                return 0f;
            }
            float inter = i.x * i.y * i.z;
            float union = sa.x * sa.y * sa.z + sb.x * sb.y * sb.z - inter;
            return union <= 0f ? 0f : inter / union;
        }

        /// <summary>Agrupa puntos: cada grupo nace del primer punto libre y toma los libres a menos de <paramref name="radio"/> de él. Devuelve índices.</summary>
        public static List<List<int>> Agrupar(IList<Vector3> puntos, float radio)
        {
            List<List<int>> grupos = new List<List<int>>();
            bool[] usado = new bool[puntos.Count];
            for (int i = 0; i < puntos.Count; i++)
            {
                if (usado[i])
                {
                    continue;
                }
                List<int> g = new List<int> { i };
                usado[i] = true;
                for (int j = i + 1; j < puntos.Count; j++)
                {
                    if (!usado[j] && Vector3.Distance(puntos[i], puntos[j]) < radio)
                    {
                        g.Add(j);
                        usado[j] = true;
                    }
                }
                grupos.Add(g);
            }
            return grupos;
        }

        /// <summary>Área (m²) en que se traslapan dos cajas mirando por el eje <paramref name="eje"/> (0 x, 1 y, 2 z). 0 si no se traslapan.</summary>
        public static float AreaTraslape(Bounds a, Bounds b, int eje)
        {
            float area = 1f;
            for (int k = 0; k < 3; k++)
            {
                if (k == eje)
                {
                    continue;
                }
                float o = Mathf.Min(a.max[k], b.max[k]) - Mathf.Max(a.min[k], b.min[k]);
                if (o <= 0f)
                {
                    return 0f;
                }
                area *= o;
            }
            return area;
        }

        // ---------------------------------------------------------------- Barrido

        private sealed class Contexto
        {
            public readonly List<Hallazgo> Lista = new List<Hallazgo>();
            public readonly Dictionary<string, Hallazgo> Ultimo = new Dictionary<string, Hallazgo>();
            public Renderer[] Renderers;
            public Bounds[] Cajas;
        }

        /// <summary>Corre el barrido y escribe Logs/barrido-mapa.json y .txt. No guarda la escena.</summary>
        public static void Barrer()
        {
            Informe informe = Calcular();
            Escribir(informe);
            Debug.Log("[T-1360] Barrido: " + informe.hallazgos.Count + " hallazgos (" + informe.hallazgos.Count(h => h.gravedad >= 3) + " de gravedad 3 o más). " + RutaTexto);
        }

        /// <summary>Resultado del barrido sin escribir archivos.</summary>
        public static Informe Calcular()
        {
            ColocarS2.Abrir();
            Contexto cx = new Contexto();
            cx.Renderers = Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .Where(r => r.enabled && !(r is ParticleSystemRenderer) && !(r is TrailRenderer) && !(r is LineRenderer) && !Ignorado(r.transform) && EnAlcance(ZonaPorPosicion(r.bounds.center)))
                .ToArray();
            cx.Cajas = cx.Renderers.Select(r => r.bounds).ToArray();

            Paso("flotantes", cx, Flotantes);
            Paso("encimados", cx, Encimados);
            Paso("texturas", cx, Texturas);
            Paso("muros", cx, Muros);
            Paso("luces", cx, Luces);
            Paso("islas", cx, Islas);
            Paso("zfighting", cx, ZFighting);

            List<MeshCollider> temporales = ColocarTemporales();
            try
            {
                Paso("enterrados", cx, Enterrados);
                Paso("huecos", cx, Huecos);
                Paso("vacios", cx, Vacios);
                Paso("escalones", cx, Escalones);
                Paso("conexiones", cx, Conexiones);
            }
            finally
            {
                foreach (MeshCollider mc in temporales)
                {
                    if (mc != null)
                    {
                        Object.DestroyImmediate(mc);
                    }
                }
            }
            cx.Lista.Sort((a, b) => b.gravedad != a.gravedad ? b.gravedad.CompareTo(a.gravedad) : string.CompareOrdinal(a.tipo, b.tipo));
            return new Informe { hallazgos = cx.Lista };
        }

        private static void Paso(string nombre, Contexto cx, System.Action<Contexto> paso)
        {
            try
            {
                paso(cx);
            }
            catch (System.Exception e)
            {
                Debug.LogError("[T-1360] Falló la revisión " + nombre + ": " + e);
                Agregar(cx, "error-del-barrido", 1, Vector3.zero, nombre, e.GetType().Name + ": " + e.Message);
            }
        }

        private static Hallazgo Agregar(Contexto cx, string tipo, int gravedad, Vector3 pos, string objeto, string detalle, string clave = null)
        {
            if (clave != null && cx.Ultimo.TryGetValue(tipo + "|" + clave, out Hallazgo previo) && Vector3.Distance(previo.posicion, pos) < 4f)
            {
                previo.cuenta++;
                previo.gravedad = Mathf.Max(previo.gravedad, gravedad);
                return previo;
            }
            Hallazgo h = new Hallazgo { tipo = tipo, gravedad = Mathf.Clamp(gravedad, 1, 4), zona = ZonaPorPosicion(pos), posicion = pos, objeto = objeto, detalle = detalle };
            cx.Lista.Add(h);
            if (clave != null)
            {
                cx.Ultimo[tipo + "|" + clave] = h;
            }
            return h;
        }

        private static bool Ignorado(Transform t)
        {
            string raiz = t.root.name;
            return RaicesIgnoradas.Any(n => raiz.StartsWith(n));
        }

        private static bool NombreIgnorado(Transform t)
        {
            return NombresIgnorados.Any(n => t.name.Contains(n));
        }

        private static string Ruta(Transform t)
        {
            StringBuilder sb = new StringBuilder(t.name);
            for (Transform p = t.parent; p != null; p = p.parent)
            {
                sb.Insert(0, p.name + "/");
            }
            return sb.ToString();
        }

        private static List<MeshCollider> ColocarTemporales()
        {
            List<MeshCollider> lista = new List<MeshCollider>();
            foreach (MeshFilter mf in Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                Renderer r = mf.GetComponent<Renderer>();
                if (mf.sharedMesh != null && r != null && r.enabled && mf.GetComponent<Collider>() == null && !Ignorado(mf.transform))
                {
                    MeshCollider mc = mf.gameObject.AddComponent<MeshCollider>();
                    mc.sharedMesh = mf.sharedMesh;
                    lista.Add(mc);
                }
            }
            Physics.SyncTransforms();
            return lista;
        }

        private static bool ColliderIgnorado(Collider c)
        {
            return Ignorado(c.transform) || NombresVehiculo.Any(n => c.name.Contains(n)) || c.name.Contains("Cebra") || c.name.Contains("Raya");
        }

        /// <summary>Y del primer piso hacia abajo desde <paramref name="yDesde"/> (NaN si no hay nada en <paramref name="alcance"/> m).</summary>
        private static float Piso(float x, float z, float yDesde, float alcance, Transform propio = null)
        {
            RaycastHit[] hits = Physics.RaycastAll(new Vector3(x, yDesde, z), Vector3.down, alcance, ~0, QueryTriggerInteraction.Ignore);
            float mejor = float.NaN;
            float dist = float.MaxValue;
            foreach (RaycastHit h in hits)
            {
                if (h.distance >= dist || ColliderIgnorado(h.collider) || (propio != null && h.collider.transform.IsChildOf(propio)))
                {
                    continue;
                }
                dist = h.distance;
                mejor = h.point.y;
            }
            return mejor;
        }

        // 1a. Flotantes: reusa FlotantesS2t.Buscar.
        private static void Flotantes(Contexto cx)
        {
            foreach (var f in FlotantesS2t.Buscar())
            {
                Vector3 pos = f.limites.center;
                if (!EnAlcance(ZonaPorPosicion(pos)) || Ignorado(f.pieza))
                {
                    continue;
                }
                float hueco = f.hueco;
                Agregar(cx, "flotante", float.IsInfinity(hueco) || hueco > 1f ? 4 : 3, pos, Ruta(f.pieza), "nada debajo a " + (float.IsInfinity(hueco) ? "más de lo medido" : hueco.ToString("F2") + " m"));
            }
        }

        // 1b. Enterrados: la base de un renderer queda más de 0.3 m bajo el piso en todos sus puntos de apoyo.
        private static void Enterrados(Contexto cx)
        {
            foreach (Renderer r in cx.Renderers)
            {
                Bounds b = r.bounds;
                if (NombreIgnorado(r.transform) || b.size.magnitude < 0.3f || b.size.magnitude > 40f || b.size.y < 0.15f)
                {
                    continue;
                }
                float yInicio = b.max.y + 0.5f;
                float peor = float.MaxValue;
                bool alguno = false;
                Vector3 e = new Vector3(b.extents.x * 0.6f, 0f, b.extents.z * 0.6f);
                Vector3[] puntos = { b.center, b.center + e, b.center - e, b.center + new Vector3(e.x, 0f, -e.z), b.center + new Vector3(-e.x, 0f, e.z) };
                foreach (Vector3 p in puntos)
                {
                    float y = Piso(p.x, p.z, yInicio, yInicio - b.min.y + 3f, r.transform);
                    if (!float.IsNaN(y))
                    {
                        alguno = true;
                        peor = Mathf.Min(peor, y - b.min.y);
                    }
                }
                if (alguno && peor > UmbralEnterrado)
                {
                    bool todo = false;
                    float yc = Piso(b.center.x, b.center.z, yInicio, yInicio - b.min.y + 3f, r.transform);
                    todo = !float.IsNaN(yc) && b.max.y < yc;
                    Agregar(cx, todo ? "enterrado-total" : "enterrado", todo ? 4 : 3, b.center, Ruta(r.transform), "base " + peor.ToString("F2") + " m bajo el piso");
                }
            }
        }

        // 2a. Encimados (RevisarCiudadT995) filtrados a las zonas.
        private static void Encimados(Contexto cx)
        {
            foreach (RevisarCiudadT995.Encimado e in RevisarCiudadT995.Encimados())
            {
                if (e.A == null || !EnAlcance(ZonaPorPosicion(e.A.position)) || Ignorado(e.A))
                {
                    continue;
                }
                int g = e.Tipo == "encimado" && e.Medida > 0.5f ? 3 : 2;
                Agregar(cx, e.Tipo, g, e.A.position, Ruta(e.A) + (e.B != null ? " <-> " + Ruta(e.B) : ""), e.Tipo + " " + e.Medida.ToString("F2") + (e.Detalle != null ? " {" + e.Detalle + "}" : ""));
            }
        }

        // 2b. z-fighting: caras coplanares (mismo lado, a menos de 2 cm) de cajas que se traslapan más de 0.25 m².
        private static void ZFighting(Contexto cx)
        {
            int n = cx.Renderers.Length;
            int[] orden = Enumerable.Range(0, n).OrderBy(i => cx.Cajas[i].min.x).ToArray();
            int encontrados = 0;
            for (int ii = 0; ii < n && encontrados < 300; ii++)
            {
                int i = orden[ii];
                Renderer ra = cx.Renderers[i];
                if (NombreIgnorado(ra.transform))
                {
                    continue;
                }
                Bounds a = cx.Cajas[i];
                for (int jj = ii + 1; jj < n && cx.Cajas[orden[jj]].min.x <= a.max.x; jj++)
                {
                    int j = orden[jj];
                    Renderer rb = cx.Renderers[j];
                    Bounds b = cx.Cajas[j];
                    if (NombreIgnorado(rb.transform) || ra.transform.IsChildOf(rb.transform) || rb.transform.IsChildOf(ra.transform) || !a.Intersects(b))
                    {
                        continue;
                    }
                    for (int eje = 0; eje < 3; eje++)
                    {
                        bool coplanar = Mathf.Abs(a.max[eje] - b.max[eje]) < 0.02f || Mathf.Abs(a.min[eje] - b.min[eje]) < 0.02f;
                        if (!coplanar)
                        {
                            continue;
                        }
                        float area = AreaTraslape(a, b, eje);
                        if (area > 0.25f)
                        {
                            bool distintas = ra.transform.root != rb.transform.root || PrefabUtility.GetNearestPrefabInstanceRoot(ra.gameObject) != PrefabUtility.GetNearestPrefabInstanceRoot(rb.gameObject);
                            Vector3 centro = (Vector3.Max(a.min, b.min) + Vector3.Min(a.max, b.max)) * 0.5f;
                            Agregar(cx, "z-fighting", distintas ? 3 : 2, centro, Ruta(ra.transform) + " <-> " + Ruta(rb.transform), "caras coplanares eje " + "xyz"[eje] + ", " + area.ToString("F2") + " m2");
                            encontrados++;
                            break;
                        }
                    }
                }
            }
        }

        // 3a. Huecos: rejilla de 2 m a lo largo y a lo ancho de cada calle de zona (y banquetas), y calles viejas cerca de uniones.
        private static void Huecos(Contexto cx)
        {
            foreach (CalleZona calle in CalleZona.Activas)
            {
                if (calle == null || calle.Cantidad < 2)
                {
                    continue;
                }
                float semi = calle.Ancho * 0.5f + (calle.Banquetas ? 3f : 0f);
                HuecosEnLinea(cx, calle.Puntos(), semi, "calle " + calle.name, true);
            }
            foreach (var v in CallesViejas)
            {
                HuecosEnLinea(cx, new List<Vector3> { v.a, v.b }, 8f, "calle vieja " + v.a.ToString("F0") + "->" + v.b.ToString("F0"), false);
            }
        }

        private static void HuecosEnLinea(Contexto cx, List<Vector3> puntos, float semi, string nombre, bool esZona)
        {
            foreach (var m in AlLargo(puntos, 2f))
            {
                if (!esZona && !ZonaPorPosicion(m.punto).StartsWith("Union"))
                {
                    continue;
                }
                for (float l = -semi; l <= semi + 0.01f; l += 2f)
                {
                    Vector3 p = m.punto + m.lateral * l;
                    float y = Piso(p.x, p.z, m.punto.y + 3f, 13f);
                    bool centro = Mathf.Abs(l) < 0.01f;
                    if (float.IsNaN(y) || y < m.punto.y - UmbralHueco)
                    {
                        Agregar(cx, "hueco", 4, new Vector3(p.x, m.punto.y, p.z), nombre, float.IsNaN(y) ? "sin piso" : "piso " + (m.punto.y - y).ToString("F2") + " m abajo de lo esperado", nombre);
                    }
                    else if (esZona && centro && Mathf.Abs(y - m.punto.y) > UmbralPisoLinea)
                    {
                        Agregar(cx, "piso-no-coincide", 3, new Vector3(p.x, m.punto.y, p.z), nombre, "piso real y=" + y.ToString("F2") + ", la línea dice y=" + m.punto.y.ToString("F2"), nombre);
                    }
                }
            }
        }

        // 3b. Vacíos: rejilla de 4 m en cada zona; sin nada debajo hasta y=-10 (el agua cuenta como piso), junto a piso real.
        private static void Vacios(Contexto cx)
        {
            foreach (var r in Rectangulos)
            {
                int nx = Mathf.CeilToInt((r.x1 - r.x0) / 4f) + 1;
                int nz = Mathf.CeilToInt((r.z1 - r.z0) / 4f) + 1;
                float[,] ys = new float[nx, nz];
                for (int i = 0; i < nx; i++)
                {
                    for (int k = 0; k < nz; k++)
                    {
                        ys[i, k] = Piso(r.x0 + i * 4f, r.z0 + k * 4f, 40f, 50f);
                    }
                }
                for (int i = 0; i < nx; i++)
                {
                    for (int k = 0; k < nz; k++)
                    {
                        if (!float.IsNaN(ys[i, k]))
                        {
                            continue;
                        }
                        bool junto = false;
                        for (int di = -1; di <= 1 && !junto; di++)
                        {
                            for (int dk = -1; dk <= 1 && !junto; dk++)
                            {
                                int a = i + di;
                                int c = k + dk;
                                junto = a >= 0 && c >= 0 && a < nx && c < nz && !float.IsNaN(ys[a, c]) && ys[a, c] > -0.5f;
                            }
                        }
                        if (junto)
                        {
                            Agregar(cx, "vacio", 4, new Vector3(r.x0 + i * 4f, 0f, r.z0 + k * 4f), r.nombre, "sin nada debajo hasta y=-10 junto a piso transitable", r.nombre);
                        }
                    }
                }
            }
        }

        // 4. Escalones: perfil cada 1 m en el centro y a +-3 m (y banquetas) de cada calle de zona y de las calles viejas en las uniones.
        private static void Escalones(Contexto cx)
        {
            foreach (CalleZona calle in CalleZona.Activas)
            {
                if (calle == null || calle.Cantidad < 2)
                {
                    continue;
                }
                float semi = calle.Ancho * 0.5f;
                float lado = Mathf.Min(3f, Mathf.Max(0.5f, semi - 0.5f));
                List<float> offsets = new List<float> { -lado, 0f, lado };
                if (calle.Banquetas)
                {
                    offsets.Add(-(semi + 1.5f));
                    offsets.Add(semi + 1.5f);
                }
                PerfilesEnLinea(cx, calle.Puntos(), offsets, "calle " + calle.name, true);
            }
            foreach (var v in CallesViejas)
            {
                PerfilesEnLinea(cx, new List<Vector3> { v.a, v.b }, new List<float> { -7.5f, -3f, 0f, 3f, 7.5f }, "calle vieja " + v.a.ToString("F0") + "->" + v.b.ToString("F0"), false);
            }
        }

        private static void PerfilesEnLinea(Contexto cx, List<Vector3> puntos, List<float> offsets, string nombre, bool esZona)
        {
            List<(Vector3 punto, Vector3 lateral)> muestras = AlLargo(puntos, 1f).ToList();
            foreach (float off in offsets)
            {
                List<float> perfil = new List<float>(muestras.Count);
                foreach (var m in muestras)
                {
                    Vector3 p = m.punto + m.lateral * off;
                    bool enAlcance = esZona || ZonaPorPosicion(m.punto).StartsWith("Union");
                    perfil.Add(enAlcance ? Piso(p.x, p.z, m.punto.y + 3f, 13f) : float.NaN);
                }
                foreach (int i in EscalonesEnPerfil(perfil, UmbralEscalon))
                {
                    float salto = perfil[i] - perfil[i - 1];
                    int g = GravedadEscalon(salto);
                    if (g > 0)
                    {
                        Vector3 p = muestras[i].punto + muestras[i].lateral * off;
                        Agregar(cx, "escalon", g, new Vector3(p.x, perfil[i], p.z), nombre, "salto " + salto.ToString("F2") + " m (lateral " + off.ToString("F1") + ")", nombre + off.ToString("F1"));
                    }
                }
            }
        }

        // Puntos cada 'paso' m a lo largo de una línea, con y interpolada y el lateral horizontal (perpendicular).
        private static IEnumerable<(Vector3 punto, Vector3 lateral)> AlLargo(List<Vector3> puntos, float paso)
        {
            for (int s = 0; s < puntos.Count - 1; s++)
            {
                Vector3 a = puntos[s];
                Vector3 b = puntos[s + 1];
                Vector3 dir = b - a;
                float largo = new Vector2(dir.x, dir.z).magnitude;
                if (largo < 0.01f)
                {
                    continue;
                }
                Vector3 lateral = new Vector3(dir.z, 0f, -dir.x).normalized;
                int pasos = Mathf.Max(1, Mathf.FloorToInt(largo / paso));
                for (int i = 0; i <= pasos; i++)
                {
                    if (i == 0 && s > 0)
                    {
                        continue; // el extremo ya salió con el tramo anterior
                    }
                    yield return (Vector3.Lerp(a, b, i / (float)pasos), lateral);
                }
            }
        }

        // 5. Muros invisibles, colliders que no corresponden y renderers sólidos sin collider.
        private static void Muros(Contexto cx)
        {
            foreach (Collider c in Object.FindObjectsByType<Collider>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (c.isTrigger || c is CharacterController || c is WheelCollider || Ignorado(c.transform) || !EnAlcance(ZonaPorPosicion(c.bounds.center)))
                {
                    continue;
                }
                string ruta = Ruta(c.transform);
                if (ruta.Contains("LimiteInvisible") || ruta.Contains("Limite") || ruta.Contains("Rampa"))
                {
                    continue;
                }
                Renderer propio = c.GetComponent<Renderer>();
                bool conRenderer = propio != null || c.GetComponentsInChildren<Renderer>(false).Any(r => r.enabled);
                Bounds cb = c.bounds;
                if (!conRenderer)
                {
                    if (!Cubierto(cx, cb))
                    {
                        int g = cb.size.y > 2f && Mathf.Max(cb.size.x, cb.size.z) > 3f ? 4 : 3;
                        Agregar(cx, "muro-invisible", g, cb.center, ruta, "collider " + cb.size.ToString("F1") + " sin renderer ni nada visible que lo cubra");
                    }
                }
                else if (propio != null && !(c is MeshCollider))
                {
                    float iou = IoU(cb, propio.bounds);
                    if (iou < 0.4f)
                    {
                        Agregar(cx, "collider-no-corresponde", iou < 0.15f ? 3 : 2, cb.center, ruta, "IoU " + iou.ToString("F2") + " entre collider " + cb.size.ToString("F1") + " y renderer " + propio.bounds.size.ToString("F1"));
                    }
                }
            }
            foreach (Renderer r in cx.Renderers)
            {
                Bounds b = r.bounds;
                if (Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z)) <= 2f || !NombresSolidos.Any(n => r.name.Contains(n) || r.transform.root != r.transform && r.transform.parent != null && r.transform.parent.name.Contains(n)))
                {
                    continue;
                }
                if (r.GetComponentInParent<Collider>() != null || r.GetComponentInChildren<Collider>() != null)
                {
                    continue;
                }
                Collider[] cerca = Physics.OverlapBox(b.center, b.extents * 0.5f, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore);
                if (cerca.Any(c => !(c is CharacterController)))
                {
                    continue;
                }
                Agregar(cx, "falta-collider", 3, b.center, Ruta(r.transform), "renderer sólido de " + b.size.ToString("F1") + " m sin collider");
            }
        }

        // Fracción de 27 puntos de la caja que quedan dentro de alguna caja de renderer: >= 0.6 se considera cubierta.
        private static bool Cubierto(Contexto cx, Bounds cb)
        {
            List<Bounds> cerca = new List<Bounds>();
            foreach (Bounds b in cx.Cajas)
            {
                if (b.Intersects(cb))
                {
                    cerca.Add(b);
                }
            }
            if (cerca.Count == 0)
            {
                return false;
            }
            int dentro = 0;
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    for (int k = 0; k < 3; k++)
                    {
                        Vector3 p = cb.min + Vector3.Scale(cb.size, new Vector3(0.1f + 0.4f * i, 0.1f + 0.4f * j, 0.1f + 0.4f * k));
                        if (cerca.Any(b => b.Contains(p)))
                        {
                            dentro++;
                        }
                    }
                }
            }
            return dentro / 27f >= 0.6f;
        }

        // 6. Texturas rotas (RevisarCiudadT995) y materiales nulos o con shader de error.
        private static void Texturas(Contexto cx)
        {
            HashSet<Renderer> avisados = new HashSet<Renderer>();
            foreach (RevisarCiudadT995.Rota r in RevisarCiudadT995.TexturasRotas())
            {
                if (r.Renderer == null || !EnAlcance(ZonaPorPosicion(r.Renderer.bounds.center)))
                {
                    continue;
                }
                string m = (r.Motivo ?? "").ToLowerInvariant();
                int g = m.Contains("nul") || m.Contains("error") || m.Contains("shader") ? 3 : 2;
                avisados.Add(r.Renderer);
                Agregar(cx, "textura-rota", g, r.Renderer.bounds.center, Ruta(r.Renderer.transform), r.Motivo + " [" + r.Indice + "]");
            }
            foreach (Renderer r in cx.Renderers)
            {
                if (avisados.Contains(r))
                {
                    continue;
                }
                Material[] ms = r.sharedMaterials;
                for (int i = 0; i < ms.Length; i++)
                {
                    if (ms[i] == null || ms[i].shader == null || ms[i].shader.name == "Hidden/InternalErrorShader")
                    {
                        Agregar(cx, "material-roto", 3, r.bounds.center, Ruta(r.transform), (ms[i] == null ? "material nulo" : "shader de error") + " [" + i + "]");
                    }
                }
            }
        }

        // 7. Calles que no conectan: extremos sin otra calle a menos de 2 m.
        private static void Conexiones(Contexto cx)
        {
            List<CalleZona> calles = CalleZona.Activas.Where(c => c != null && c.Cantidad >= 2).ToList();
            foreach (CalleZona calle in calles)
            {
                List<Vector3> pts = calle.Puntos();
                foreach (Vector3 extremo in new[] { pts[0], pts[pts.Count - 1] })
                {
                    float d = float.MaxValue;
                    foreach (CalleZona otra in calles)
                    {
                        if (otra == calle)
                        {
                            continue;
                        }
                        List<Vector3> op = otra.Puntos();
                        for (int i = 0; i < op.Count - 1; i++)
                        {
                            d = Mathf.Min(d, DistanciaASegmento(extremo, op[i], op[i + 1]));
                        }
                    }
                    foreach (var v in CallesViejas)
                    {
                        d = Mathf.Min(d, DistanciaASegmento(extremo, v.a, v.b));
                    }
                    foreach (var u in Uniones)
                    {
                        d = Mathf.Min(d, Vector3.Distance(extremo, u.punto));
                    }
                    if (d >= 2f)
                    {
                        Agregar(cx, "calle-sin-conexion", d >= CalleZona.DistanciaUnion ? 4 : 3, extremo, "calle " + calle.name, "el extremo está a " + d.ToString("F1") + " m de otra calle (debe ser menos de 2 m)");
                    }
                }
            }
        }

        private static float DistanciaASegmento(Vector3 p, Vector3 a, Vector3 b)
        {
            Vector3 ab = b - a;
            float t = ab.sqrMagnitude < 1e-6f ? 0f : Mathf.Clamp01(Vector3.Dot(p - a, ab) / ab.sqrMagnitude);
            Vector3 q = a + ab * t;
            float dy = Mathf.Abs(p.y - q.y);
            return dy > 4f ? float.MaxValue : Vector3.Distance(p, q);
        }

        // 8. Islas sin salida: NavMesh en memoria con los colliders estáticos; desde el cruce (0,0.2,0) busca puntos de calle y piso sin camino.
        private static void Islas(Contexto cx)
        {
            Bounds lim = new Bounds(new Vector3(-67f, 4f, 10f), new Vector3(840f, 40f, 570f));
            List<NavMeshBuildSource> fuentes = new List<NavMeshBuildSource>();
            NavMeshBuilder.CollectSources(lim, ~0, NavMeshCollectGeometry.PhysicsColliders, 0, new List<NavMeshBuildMarkup>(), fuentes);
            fuentes.RemoveAll(s =>
            {
                Collider c = s.component as Collider;
                return c != null && (c.isTrigger || c.attachedRigidbody != null || c is CharacterController || Ignorado(c.transform));
            });
            NavMeshBuildSettings ajustes = NavMesh.CreateSettings();
            ajustes.agentRadius = 0.4f;
            ajustes.agentHeight = 1.8f;
            ajustes.agentClimb = 0.45f;
            ajustes.agentSlope = 40f;
            ajustes.minRegionArea = 0.5f;
            NavMeshData datos = NavMeshBuilder.BuildNavMeshData(ajustes, fuentes, lim, Vector3.zero, Quaternion.identity);
            NavMeshDataInstance instancia = NavMesh.AddNavMeshData(datos);
            try
            {
                if (!NavMesh.SamplePosition(new Vector3(0f, 0.2f, 0f), out NavMeshHit inicio, 3f, NavMesh.AllAreas))
                {
                    Agregar(cx, "isla-sin-navmesh", 4, new Vector3(0f, 0.2f, 0f), "cruce (0,0.2,0)", "no hay NavMesh en el cruce: no se pudo revisar la conectividad (" + fuentes.Count + " fuentes)");
                    return;
                }
                List<Vector3> candidatos = new List<Vector3>();
                foreach (CalleZona calle in CalleZona.Activas)
                {
                    if (calle != null && calle.Cantidad >= 2)
                    {
                        foreach (var m in AlLargo(calle.Puntos(), 6f))
                        {
                            candidatos.Add(m.punto);
                        }
                    }
                }
                foreach (string zona in new[] { "Malecon", "Puerto" })
                {
                    var r = Rectangulos.First(q => q.nombre == zona);
                    for (float x = r.x0; x <= r.x1; x += 6f)
                    {
                        for (float z = r.z0; z <= r.z1; z += 6f)
                        {
                            float y = Piso(x, z, 5f, 12f);
                            if (!float.IsNaN(y) && y > -3.5f && y < 1f)
                            {
                                candidatos.Add(new Vector3(x, y, z));
                            }
                        }
                    }
                }
                NavMeshPath camino = new NavMeshPath();
                List<Vector3> islas = new List<Vector3>();
                foreach (Vector3 c in candidatos)
                {
                    if (!EnAlcance(ZonaPorPosicion(c)) || !NavMesh.SamplePosition(c, out NavMeshHit h, 1.5f, NavMesh.AllAreas))
                    {
                        continue;
                    }
                    if (!NavMesh.CalculatePath(inicio.position, h.position, NavMesh.AllAreas, camino) || camino.status != NavMeshPathStatus.PathComplete)
                    {
                        islas.Add(h.position);
                    }
                }
                foreach (List<int> g in Agrupar(islas, 12f))
                {
                    Vector3 pos = islas[g[0]];
                    Hallazgo h = Agregar(cx, "isla-sin-salida", g.Count >= 10 ? 4 : 3, pos, "NavMesh", "región de " + g.Count + " muestras (cada 6 m) sin camino desde el cruce (0,0.2,0)");
                    h.cuenta = g.Count;
                }
            }
            finally
            {
                NavMesh.RemoveNavMeshData(instancia);
            }
        }

        // 9. Luces que no corresponden.
        private static void Luces(Contexto cx)
        {
            foreach (Light l in Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (!l.enabled || l.type == LightType.Directional || l.type == LightType.Area || Ignorado(l.transform) || !EnAlcance(ZonaPorPosicion(l.transform.position)))
                {
                    continue;
                }
                Vector3 p = l.transform.position;
                if (!HayLamparaCerca(cx, p))
                {
                    Agregar(cx, "luz-sin-lampara", 2, p, Ruta(l.transform), "ningún objeto emisivo ni lámpara a menos de 1.5 m");
                }
                if (l.type == LightType.Spot && l.transform.forward.y > 0.7f)
                {
                    Agregar(cx, "spot-hacia-arriba", 2, p, Ruta(l.transform), "el spot apunta hacia arriba (forward.y " + l.transform.forward.y.ToString("F2") + ")");
                }
                foreach (Collider c in Physics.OverlapSphere(p, 0.05f, ~0, QueryTriggerInteraction.Ignore))
                {
                    bool convexo = c is BoxCollider || c is SphereCollider || c is CapsuleCollider || (c is MeshCollider mc && mc.convex);
                    if (convexo && !l.transform.IsChildOf(c.transform.root) && (c.ClosestPoint(p) - p).sqrMagnitude < 1e-6f)
                    {
                        Agregar(cx, "luz-dentro-de-geometria", 3, p, Ruta(l.transform), "la luz está dentro del collider " + Ruta(c.transform));
                        break;
                    }
                }
            }
        }

        private static bool HayLamparaCerca(Contexto cx, Vector3 p)
        {
            for (int i = 0; i < cx.Renderers.Length; i++)
            {
                if (cx.Cajas[i].SqrDistance(p) > 2.25f)
                {
                    continue;
                }
                Renderer r = cx.Renderers[i];
                if (NombresLampara.Any(n => r.name.Contains(n) || (r.transform.parent != null && r.transform.parent.name.Contains(n))))
                {
                    return true;
                }
                foreach (Material m in r.sharedMaterials)
                {
                    if (m != null && m.HasProperty("_EmissionColor") && m.IsKeywordEnabled("_EMISSION"))
                    {
                        Color e = m.GetColor("_EmissionColor");
                        if (Mathf.Max(e.r, Mathf.Max(e.g, e.b)) > EmisionMinima)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        // ---------------------------------------------------------------- Salida

        private static void Escribir(Informe informe)
        {
            Directory.CreateDirectory("Logs");
            File.WriteAllText(RutaJson, JsonUtility.ToJson(informe, true));
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Barrido del mapa nuevo (T-1360): " + informe.hallazgos.Count + " hallazgos");
            for (int g = 4; g >= 1; g--)
            {
                List<Hallazgo> deGravedad = informe.hallazgos.Where(h => h.gravedad == g).ToList();
                sb.AppendLine();
                sb.AppendLine("=== Gravedad " + g + " (" + deGravedad.Count + ") ===");
                foreach (Hallazgo h in deGravedad)
                {
                    sb.AppendLine("[" + h.tipo + "] " + h.zona + " " + h.posicion.ToString("F1") + (h.cuenta > 1 ? " x" + h.cuenta : "") + " | " + h.objeto + " | " + h.detalle);
                }
            }
            File.WriteAllText(RutaTexto, sb.ToString());
        }

        /// <summary>Una captura por grupo de hallazgos de gravedad 3 o 4 (máx 60) en docs/capturas/bugs-18nov/barrido-n.png. Lee Logs/barrido-mapa.json (corre Barrer antes).</summary>
        public static void Fotos()
        {
            if (!File.Exists(RutaJson))
            {
                Debug.LogError("[T-1360] Falta " + RutaJson + ": corre Barrer primero.");
                return;
            }
            Informe informe = JsonUtility.FromJson<Informe>(File.ReadAllText(RutaJson));
            List<Hallazgo> graves = informe.hallazgos.Where(h => h.gravedad >= 3 && h.tipo != "error-del-barrido").OrderByDescending(h => h.gravedad).ToList();
            List<List<int>> grupos = Agrupar(graves.Select(h => h.posicion).ToList(), RadioAgrupado);
            ColocarS2.Abrir();
            Directory.CreateDirectory(CarpetaFotos);
            StringBuilder indice = new StringBuilder();
            int n = 0;
            foreach (List<int> g in grupos)
            {
                if (n >= MaximoFotos)
                {
                    break;
                }
                n++;
                Hallazgo h = graves[g[0]];
                Vector3 hacia = h.posicion;
                Vector3 desde = hacia + new Vector3(3.5f, 3.5f, -3.5f).normalized * DistanciaFoto;
                string ruta = CarpetaFotos + "/barrido-" + n + ".png";
                ColocarS2cLook.Foto(desde, hacia, ruta, 60f, HoraDeFoto);
                indice.AppendLine("barrido-" + n + ".png | " + h.zona + " " + hacia.ToString("F1") + " | " + string.Join("; ", g.Select(i => graves[i].tipo + " (" + graves[i].gravedad + ")").Distinct()) + " | " + h.objeto);
            }
            File.WriteAllText(CarpetaFotos + "/barrido-indice.txt", indice.ToString());
            Debug.Log("[T-1360] " + n + " capturas en " + CarpetaFotos);
        }
    }
}
