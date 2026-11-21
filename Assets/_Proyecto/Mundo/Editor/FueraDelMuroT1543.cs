using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Mundo.Editor
{
    /// <summary>
    /// T-1543: la ciudad dentro del muro, sin nada detrás y sin huecos. Trabaja sobre Ciudad_Prologo.
    /// - Diagnostico: solo informa (Logs/t1543-diagnostico.txt): lo que hay afuera del muro, los pisos que sobresalen, los huecos de adentro y la franja junto al muro.
    /// - Aplicar: 1) apaga (SetActive false, no borra) todo lo que está afuera y recorta los pisos a (muro + 3 m); 2) rellena los huecos y la franja con edificios reales
    ///   (ED, NV01, P11, P12, LO, CC, CH02, ML05, PU03/04 según el barrio), pegados a sus vecinos y al muro, de espaldas al muro y con el frente a la calle;
    ///   3) revisa torres MU02 (4 esquinas y 2 por puerta) y les pone un reflector (Light spot) hacia afuera y abajo. Idempotente.
    /// Orden en batchmode (Unity cerrado):
    ///   1. -executeMethod MechaCyberpunk.Mundo.Editor.FueraDelMuroT1543.Diagnostico    -> Logs/t1543-diagnostico.txt (no guarda)
    ///   2. -executeMethod MechaCyberpunk.Mundo.Editor.FueraDelMuroT1543.Aplicar        -> cambia, guarda, Logs/t1543-aplicar.txt y Logs/t1543-diagnostico-despues.txt
    ///   3. -executeMethod MechaCyberpunk.Mundo.Editor.FueraDelMuroT1543.ListaCapturas  -> Logs/t1543-capturas.txt
    ///   4. -executeMethod MechaCyberpunk.Mundo.Editor.FotosT1543.Tomar                 -> docs/capturas/_21nov/muro1543_*.png
    /// Debug.Log con el prefijo [T-1543]. No toca el trazo ni la altura del muro (T-1513, T-1524).
    /// </summary>
    public static class FueraDelMuroT1543
    {
        public const string Etiqueta = "[T-1543] ";
        public const string RaizRelleno = "T1543_Relleno";
        public const string RaizLuces = "T1543_MuroLuces";
        public const string RutaDiagnostico = "Logs/t1543-diagnostico.txt";
        public const string RutaDespues = "Logs/t1543-diagnostico-despues.txt";
        public const string RutaAplicar = "Logs/t1543-aplicar.txt";
        public const string RutaCapturas = "Logs/t1543-capturas.txt";

        /// <summary>Grosor del muro (m) si no se puede medir de las piezas MU01.</summary>
        public const float GrosorPorOmision = 3f;

        /// <summary>Hueco que las pruebas aceptan que quede (m2): menos que un lote chico.</summary>
        public const float HuecoMaximoM2 = 120f;

        /// <summary>Lo que se ve y se mide en el plano: más bajo que esto es piso, no ocupa.</summary>
        private const float AlturaPiso = 1f;
        private const float TechoPiso = 1.5f;
        private const float HuellaMinimaOcupante = 1f;
        private const float AlturaMaximaOcupante = 3.5f;
        private const float EscalaDomo = 600f;
        private const float LadoMaximoReserva = 15f;
        private const float MargenBanqueta = 3f;
        private const float LargoCalle = 60f;
        private const float AnchoCalleMaximo = 24f;
        private const float AlcanceMarca = 2.5f;
        private const float DistanciaFranja = 14f;

        // Reflectores de las torres (ver la nota en Torres: no se pudo medir en Unity).
        private static readonly Color ColorReflector = new Color(1f, 0.72f, 0.4f);
        private const float IntensidadReflector = 600f;
        private const float AlcanceReflector = 45f;
        private const float AnguloReflector = 75f;
        private const float AnguloInteriorReflector = 40f;
        private const float InclinacionReflector = -0.6f;

        private static readonly Regex Logica = new Regex(
            "Aparicion|Spawn|Mision|Marcador|Objetivo|Lugar|Entrada|Interaccion|Zona|Trigger|Jugador|Mecha|Tienda|Casino|Arena|Pelea|Rival|Guardia|Policia|Enemigo|Peaton|Trafico|Nodo|Cobertura|Salida",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex Protegidos = new Regex(
            "^(cielo|sky|skybox|domo|nube|nubes|estrellas|horizonte|camara|cinemachine|jugador|mechajugador|amb_|sondas|posproceso|ciclodianoche|relojmundo|luznoche|probevolume)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        internal enum TipoPiso
        {
            Calle,
            Banqueta,
            Marca,
            Otro
        }

        internal class Piso
        {
            public Bounds B;
            public TipoPiso Tipo;
            public bool Bloque;
            public bool Alargado;
        }

        /// <summary>Todo lo que mide el barrido sobre la escena (con el muro ya fijado).</summary>
        internal class Mapa
        {
            public Rect Linea;
            public Rect Interior;
            public float Grosor;
            public List<Piso> Pisos = new List<Piso>();
            public Indice Ocupantes = new Indice();
            public List<Bounds> Reservas = new List<Bounds>();
            public int Activos;
        }

        /// <summary>Cajas en una rejilla XZ de 8 m para preguntar rápido qué hay cerca de un punto.</summary>
        internal class Indice
        {
            private const float Lado = 8f;
            private const int MaxCeldas = 64;
            private readonly Dictionary<long, List<int>> celdas = new Dictionary<long, List<int>>();
            private readonly List<Bounds> cajas = new List<Bounds>();
            private readonly List<int> grandes = new List<int>();

            public int Cuenta { get { return cajas.Count; } }

            private static long Llave(int x, int z)
            {
                return ((long)x << 32) ^ (uint)z;
            }

            public void Agregar(Bounds b)
            {
                int i = cajas.Count;
                cajas.Add(b);
                int x0 = Mathf.FloorToInt(b.min.x / Lado), x1 = Mathf.FloorToInt(b.max.x / Lado);
                int z0 = Mathf.FloorToInt(b.min.z / Lado), z1 = Mathf.FloorToInt(b.max.z / Lado);
                if ((long)(x1 - x0 + 1) * (z1 - z0 + 1) > MaxCeldas)
                {
                    grandes.Add(i);
                    return;
                }
                for (int x = x0; x <= x1; x++)
                {
                    for (int z = z0; z <= z1; z++)
                    {
                        List<int> l;
                        if (!celdas.TryGetValue(Llave(x, z), out l))
                        {
                            l = new List<int>();
                            celdas[Llave(x, z)] = l;
                        }
                        l.Add(i);
                    }
                }
            }

            private IEnumerable<int> Candidatos(Vector2 p, float radio)
            {
                int x0 = Mathf.FloorToInt((p.x - radio) / Lado), x1 = Mathf.FloorToInt((p.x + radio) / Lado);
                int z0 = Mathf.FloorToInt((p.y - radio) / Lado), z1 = Mathf.FloorToInt((p.y + radio) / Lado);
                for (int x = x0; x <= x1; x++)
                {
                    for (int z = z0; z <= z1; z++)
                    {
                        List<int> l;
                        if (celdas.TryGetValue(Llave(x, z), out l))
                        {
                            foreach (int i in l)
                            {
                                yield return i;
                            }
                        }
                    }
                }
                foreach (int i in grandes)
                {
                    yield return i;
                }
            }

            /// <summary>Hay una caja a menos de radio (en XZ) del punto cuya altura toca [yMin, yMax].</summary>
            public bool Cerca(Vector2 p, float radio, float yMin, float yMax)
            {
                foreach (int i in Candidatos(p, radio))
                {
                    Bounds b = cajas[i];
                    if (b.max.y > yMin && b.min.y < yMax && CalculoMuroT1543.DistanciaXZ(b, p) < radio)
                    {
                        return true;
                    }
                }
                return false;
            }

            /// <summary>La caja a se mete más de minimo en los tres ejes en alguna de las guardadas.</summary>
            public bool Choca(Bounds a, float minimo)
            {
                Vector2 c = new Vector2(a.center.x, a.center.z);
                float r = Mathf.Max(a.extents.x, a.extents.z) + 1f;
                foreach (int i in Candidatos(c, r))
                {
                    Bounds b = cajas[i];
                    float ix = Mathf.Min(a.max.x, b.max.x) - Mathf.Max(a.min.x, b.min.x);
                    float iy = Mathf.Min(a.max.y, b.max.y) - Mathf.Max(a.min.y, b.min.y);
                    float iz = Mathf.Min(a.max.z, b.max.z) - Mathf.Max(a.min.z, b.min.z);
                    if (ix > minimo && iy > minimo && iz > minimo)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        // ================================================================== entradas

        [MenuItem("Mecha Cyberpunk/T-1543 Muro: diagnostico")]
        public static void Diagnostico()
        {
            Scene escena = ReemplazarEdificiosT1502.Abrir();
            Barrido barrido = new Barrido(LineaDelMuro(), GrosorDelMuro(), false);
            barrido.Correr(escena);
            Mapa mapa = Medir(escena);
            string texto = Informe("ANTES", escena, barrido, mapa);
            Escribir(RutaDiagnostico, texto);
            Debug.Log(Etiqueta + "diagnostico escrito en " + RutaDiagnostico + ": " + barrido.Resumen());
        }

        [MenuItem("Mecha Cyberpunk/T-1543 Muro: aplicar")]
        public static void Aplicar()
        {
            Scene escena = ReemplazarEdificiosT1502.Abrir();
            Rect linea = LineaDelMuro();
            float grosor = GrosorDelMuro();
            StringBuilder log = new StringBuilder();
            log.AppendLine("T-1543 la ciudad dentro del muro (" + DateTime.Now.ToString("yyyy-MM-dd HH:mm") + ")");
            log.AppendLine("Linea del muro: x " + linea.xMin + ".." + linea.xMax + ", z " + linea.yMin + ".." + linea.yMax + "; grosor " + grosor.ToString("F2") + " m");

            // 0. Deshacer lo de una corrida anterior (los objetos apagados se quedan apagados).
            foreach (string nombre in new[] { RaizRelleno, RaizLuces })
            {
                GameObject viejo = GameObject.Find(nombre);
                while (viejo != null)
                {
                    Object.DestroyImmediate(viejo);
                    viejo = GameObject.Find(nombre);
                }
            }
            Physics.SyncTransforms();

            // Diagnostico de antes (solo informa).
            Barrido antes = new Barrido(linea, grosor, false);
            antes.Correr(escena);
            Escribir(RutaDiagnostico, Informe("ANTES", escena, antes, Medir(escena)));

            // 1. Afuera.
            Barrido real = new Barrido(linea, grosor, true);
            real.Correr(escena);
            Physics.SyncTransforms();
            log.AppendLine();
            log.AppendLine("=== 1. AFUERA ===");
            log.AppendLine(real.Resumen());
            foreach (KeyValuePair<string, int> kv in real.PorRaiz.OrderByDescending(x => x.Value))
            {
                log.AppendLine("  " + kv.Key + ": " + kv.Value + " objetos apagados");
            }
            Debug.Log(Etiqueta + "afuera: " + real.Resumen());

            // 2. Huecos y franja.
            Mapa mapa = Medir(escena);
            RellenoInforme relleno = Rellenar(escena, mapa);
            log.AppendLine();
            log.AppendLine("=== 2. HUECOS Y FRANJA ===");
            log.AppendLine("Celdas de hueco: " + relleno.Celdas + " (" + (relleno.Celdas * CalculoMuroT1543.Celda * CalculoMuroT1543.Celda).ToString("F0") + " m2), zonas " + relleno.Zonas
                + ", reservadas por gameplay " + relleno.Reservadas + " celdas");
            log.AppendLine("Lotes rellenados: " + relleno.Lotes + ", edificios puestos: " + relleno.Edificios + ", edificios descartados por encimarse: " + relleno.Encimados);
            log.AppendLine("Lotes sin cabida (ningun modelo cupo o se redujo a nada): " + relleno.SinCabida.Count);
            foreach (string s in relleno.SinCabida)
            {
                log.AppendLine("  " + s);
            }
            Debug.Log(Etiqueta + "relleno: " + relleno.Lotes + " lotes, " + relleno.Edificios + " edificios, " + relleno.SinCabida.Count + " sin cabida");

            // 3. Torres y reflectores.
            TorresInforme torres = Torres(escena, mapa);
            log.AppendLine();
            log.AppendLine("=== 3. TORRES Y REFLECTORES ===");
            log.AppendLine("Torres MU02 en la escena: " + torres.Total + " (esquinas " + torres.Esquinas + ", a los lados de puertas " + torres.Salidas + "); agregadas por faltar: " + torres.Agregadas);
            log.AppendLine("Esquinas sin torre: " + (torres.EsquinasSinTorre.Count == 0 ? "ninguna" : string.Join("; ", torres.EsquinasSinTorre)));
            log.AppendLine("Reflectores (Light spot): " + torres.Luces + ". Modelo de reflector: no existe en Assets (solo CY01_ProyectorHolo, que es holograma): solo la Light. Concertina: no existe modelo, no se puso.");
            Debug.Log(Etiqueta + "torres: " + torres.Total + ", reflectores " + torres.Luces + ", agregadas " + torres.Agregadas);

            Physics.SyncTransforms();
            EditorSceneManager.MarkSceneDirty(escena);
            EditorSceneManager.SaveScene(escena);

            Barrido despues = new Barrido(linea, grosor, false);
            despues.Correr(escena);
            Mapa mapaDespues = Medir(escena);
            Escribir(RutaDespues, Informe("DESPUES", escena, despues, mapaDespues));
            log.AppendLine();
            log.AppendLine("=== DESPUES ===");
            log.AppendLine("Activos aun afuera: " + despues.FueraActivos + "; huecos: " + BuscarHuecos(mapaDespues).Count + " celdas; ver " + RutaDespues);
            Escribir(RutaAplicar, log.ToString());
            Debug.Log(Etiqueta + "aplicado; ver " + RutaAplicar + " y " + RutaDespues);
        }

        [MenuItem("Mecha Cyberpunk/T-1543 Muro: lista de capturas")]
        public static void ListaCapturas()
        {
            ReemplazarEdificiosT1502.Abrir();
            Rect linea = LineaDelMuro();
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("T-1543 vistas para capturas (posicion -> mira a; fov). Las toma FotosT1543.Tomar en docs/capturas/_21nov/.");
            foreach (Vista v in Vistas(linea))
            {
                sb.AppendLine(v.Nombre + "\t" + v.Posicion.ToString("F1") + " -> " + v.Objetivo.ToString("F1") + "\tfov " + v.Fov + "\t" + v.Nota);
            }
            Escribir(RutaCapturas, sb.ToString());
            Debug.Log(Etiqueta + "capturas listadas en " + RutaCapturas);
        }

        // ================================================================== muro

        /// <summary>Rectángulo del trazo del muro: de los tramos viejos (T530_Afueras/Orillas/Muro); si no hay, el de T-1513 (x +-135, z +-88).</summary>
        public static Rect LineaDelMuro()
        {
            List<Transform> muros = MuroCiudadT1513.MurosViejos();
            if (muros.Count > 0)
            {
                return CalculoMuroT1543.RectDeSegmentos(muros.Select(MuroCiudadT1513.Segmento).ToList());
            }
            return Rect.MinMaxRect(-135f, -88f, 135f, 88f);
        }

        /// <summary>Grosor de las piezas MU01 puestas (lado corto de su caja); GrosorPorOmision si no hay.</summary>
        public static float GrosorDelMuro()
        {
            GameObject raiz = GameObject.Find(MuroCiudadT1513.NombreRaiz);
            if (raiz == null)
            {
                return GrosorPorOmision;
            }
            List<float> lados = new List<float>();
            foreach (Transform t in raiz.transform)
            {
                if (!t.name.StartsWith("Muro_"))
                {
                    continue;
                }
                Renderer[] rs = t.GetComponentsInChildren<Renderer>();
                if (rs.Length == 0)
                {
                    continue;
                }
                Bounds b = rs[0].bounds;
                foreach (Renderer r in rs)
                {
                    b.Encapsulate(r.bounds);
                }
                lados.Add(Mathf.Min(b.size.x, b.size.z));
            }
            if (lados.Count == 0)
            {
                return GrosorPorOmision;
            }
            lados.Sort();
            return Mathf.Clamp(lados[lados.Count / 2], 1f, 8f);
        }

        // ================================================================== medir la escena

        private static void Escribir(string ruta, string texto)
        {
            ReemplazarEdificiosT1502.Escribir(ruta, texto);
        }

        private static bool EsEfecto(Renderer r)
        {
            return r is ParticleSystemRenderer || r is LineRenderer || r is TrailRenderer;
        }

        private static string Texto(Transform t, Renderer r)
        {
            StringBuilder sb = new StringBuilder(t.name);
            Transform p = t.parent;
            for (int i = 0; i < 3 && p != null; i++)
            {
                sb.Append('/').Append(p.name);
                p = p.parent;
            }
            if (r != null && r.sharedMaterial != null)
            {
                sb.Append('/').Append(r.sharedMaterial.name);
            }
            return sb.ToString().ToLowerInvariant();
        }

        private static TipoPiso ClasificarPiso(Transform t, Renderer r, Bounds b, out bool alargado)
        {
            string s = Texto(t, r);
            float largo = Mathf.Max(b.size.x, b.size.z);
            float corto = Mathf.Min(b.size.x, b.size.z);
            alargado = largo >= LargoCalle && corto <= AnchoCalleMaximo;
            if (s.Contains("banqueta") || s.Contains("acera") || s.Contains("guarnicion"))
            {
                return TipoPiso.Banqueta;
            }
            if (s.Contains("raya") || s.Contains("linea") || s.Contains("cebra") || s.Contains("marca"))
            {
                return TipoPiso.Marca;
            }
            if (s.Contains("asfalto") || s.Contains("calle") || s.Contains("cruce") || s.Contains("camino") || s.Contains("carril")
                || s.Contains("avenida") || s.Contains("glorieta") || s.Contains("rotonda") || s.Contains("pavimento") || s.Contains("vialidad"))
            {
                return TipoPiso.Calle;
            }
            return TipoPiso.Otro;
        }

        internal static Mapa Medir(Scene escena)
        {
            Mapa m = new Mapa();
            m.Linea = LineaDelMuro();
            m.Grosor = GrosorDelMuro();
            m.Interior = CalculoMuroT1543.Expandir(m.Linea, -(m.Grosor * 0.5f + 0.1f));
            foreach (GameObject raiz in escena.GetRootGameObjects())
            {
                foreach (Renderer r in raiz.GetComponentsInChildren<Renderer>(false))
                {
                    if (!r.enabled || !r.gameObject.activeInHierarchy || EsEfecto(r))
                    {
                        continue;
                    }
                    Bounds b = r.bounds;
                    if (b.size.x > EscalaDomo || b.size.z > EscalaDomo)
                    {
                        if (b.size.y >= AlturaPiso)
                        {
                            continue; // cúpula del cielo y similares
                        }
                    }
                    m.Activos++;
                    if (b.size.y < AlturaPiso && b.max.y <= TechoPiso)
                    {
                        bool alargado;
                        TipoPiso tipo = ClasificarPiso(r.transform, r, b, out alargado);
                        m.Pisos.Add(new Piso { B = b, Tipo = tipo, Bloque = tipo == TipoPiso.Banqueta && b.size.x >= 20f && b.size.z >= 20f, Alargado = alargado });
                    }
                    else if (EsOcupante(b))
                    {
                        m.Ocupantes.Agregar(b);
                    }
                }
                foreach (Collider c in raiz.GetComponentsInChildren<Collider>(false))
                {
                    if (c.enabled && c.isTrigger && c.gameObject.activeInHierarchy && Mathf.Max(c.bounds.size.x, c.bounds.size.z) <= LadoMaximoReserva)
                    {
                        Bounds rb = c.bounds;
                        rb.Expand(2f);
                        m.Reservas.Add(rb);
                    }
                }
                foreach (MonoBehaviour mb in raiz.GetComponentsInChildren<MonoBehaviour>(false))
                {
                    if (mb == null || !mb.enabled)
                    {
                        continue;
                    }
                    string tipo = mb.GetType().Name;
                    if (tipo.Contains("Aparicion") || tipo.Contains("Spawn") || tipo.Contains("Mision") || tipo.Contains("Marcador") || tipo.Contains("Objetivo"))
                    {
                        m.Reservas.Add(new Bounds(mb.transform.position, new Vector3(4f, 10f, 4f)));
                    }
                }
            }
            return m;
        }

        private static bool EsOcupante(Bounds b)
        {
            return b.size.y >= AlturaPiso && b.min.y < AlturaMaximaOcupante && Mathf.Max(b.size.x, b.size.z) >= HuellaMinimaOcupante;
        }

        private static bool Contiene(Bounds b, Vector2 p)
        {
            return p.x >= b.min.x && p.x <= b.max.x && p.y >= b.min.z && p.y <= b.max.z;
        }

        /// <summary>Altura del suelo en el punto: lo más alto de los pisos bajo él (banqueta 0.2), 0 si no hay.</summary>
        private static float AlturaSuelo(Mapa m, Vector2 p)
        {
            float y = 0f;
            foreach (Piso f in m.Pisos)
            {
                if (f.Tipo != TipoPiso.Marca && Contiene(f.B, p) && f.B.max.y > y)
                {
                    y = f.B.max.y;
                }
            }
            return y;
        }

        /// <summary>El punto es calle o banqueta (la franja de 3 m de cada manzana). Las manzanas (banqueta grande) por dentro son lote.</summary>
        private static bool EsCalleOBanqueta(Mapa m, Vector2 p)
        {
            bool calle = false;
            bool interiorDeManzana = false;
            foreach (Piso f in m.Pisos)
            {
                if (f.Tipo == TipoPiso.Marca)
                {
                    Bounds e = f.B;
                    e.Expand(new Vector3(AlcanceMarca * 2f, 0f, AlcanceMarca * 2f));
                    if (Contiene(e, p))
                    {
                        calle = true;
                    }
                    continue;
                }
                if (!Contiene(f.B, p))
                {
                    continue;
                }
                switch (f.Tipo)
                {
                    case TipoPiso.Banqueta:
                        if (f.Bloque)
                        {
                            Bounds interior = f.B;
                            interior.Expand(new Vector3(-MargenBanqueta * 2f, 0f, -MargenBanqueta * 2f));
                            if (Contiene(interior, p))
                            {
                                interiorDeManzana = true;
                            }
                            else
                            {
                                return true;
                            }
                        }
                        else
                        {
                            return true;
                        }
                        break;
                    case TipoPiso.Calle:
                        calle = true;
                        break;
                    default:
                        if (f.Alargado)
                        {
                            calle = true;
                        }
                        break;
                }
            }
            return interiorDeManzana ? false : calle;
        }

        private static bool Reservado(Mapa m, Vector2 p, float radio)
        {
            foreach (Bounds b in m.Reservas)
            {
                if (CalculoMuroT1543.DistanciaXZ(b, p) < radio)
                {
                    return true;
                }
            }
            return false;
        }

        // ================================================================== huecos

        /// <summary>Celdas de 4 m con suelo que no es calle ni banqueta y nada encima (3 m de alto) en 3 m a la redonda. Quita las reservadas por gameplay.</summary>
        internal static List<Vector2Int> BuscarHuecos(Mapa m)
        {
            int reservadas;
            return BuscarHuecos(m, out reservadas);
        }

        internal static List<Vector2Int> BuscarHuecos(Mapa m, out int reservadas)
        {
            reservadas = 0;
            List<Vector2Int> huecos = new List<Vector2Int>();
            Vector2Int c0 = CalculoMuroT1543.CeldaDe(m.Interior.min);
            Vector2Int c1 = CalculoMuroT1543.CeldaDe(m.Interior.max);
            for (int cx = c0.x; cx <= c1.x; cx++)
            {
                for (int cz = c0.y; cz <= c1.y; cz++)
                {
                    Vector2Int celda = new Vector2Int(cx, cz);
                    Vector2 p = CalculoMuroT1543.CentroDeCelda(celda);
                    if (!m.Interior.Contains(p) || EsCalleOBanqueta(m, p))
                    {
                        continue;
                    }
                    float y = AlturaSuelo(m, p);
                    if (m.Ocupantes.Cerca(p, CalculoMuroT1543.RadioHueco, y + 0.3f, y + CalculoMuroT1543.AltoHueco))
                    {
                        continue;
                    }
                    if (Reservado(m, p, CalculoMuroT1543.RadioHueco))
                    {
                        reservadas++;
                        continue;
                    }
                    huecos.Add(celda);
                }
            }
            return huecos;
        }

        public class ZonaHueco
        {
            public int Celdas;
            public Bounds Caja;
            public string Barrio;
            public bool JuntoAlMuro;
            public float Area { get { return Celdas * CalculoMuroT1543.Celda * CalculoMuroT1543.Celda; } }
        }

        /// <summary>Zonas de hueco de la escena abierta, de mayor a menor (para el informe y las pruebas).</summary>
        public static List<ZonaHueco> MedirHuecos(Scene escena)
        {
            Mapa m = Medir(escena);
            return Zonas(m, BuscarHuecos(m));
        }

        private static List<ZonaHueco> Zonas(Mapa m, List<Vector2Int> huecos)
        {
            List<ZonaHueco> zonas = new List<ZonaHueco>();
            foreach (List<Vector2Int> comp in CalculoMuroT1543.Componentes(huecos))
            {
                Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
                Vector2 max = new Vector2(float.MinValue, float.MinValue);
                bool muro = false;
                foreach (Vector2Int c in comp)
                {
                    Vector2 p = CalculoMuroT1543.CentroDeCelda(c);
                    min = Vector2.Min(min, p);
                    max = Vector2.Max(max, p);
                    float d = Mathf.Min(Mathf.Min(p.x - m.Linea.xMin, m.Linea.xMax - p.x), Mathf.Min(p.y - m.Linea.yMin, m.Linea.yMax - p.y));
                    if (d < m.Grosor * 0.5f + DistanciaFranja)
                    {
                        muro = true;
                    }
                }
                Vector2 centro = (min + max) * 0.5f;
                Bounds caja = new Bounds(new Vector3(centro.x, 0f, centro.y), new Vector3(max.x - min.x + CalculoMuroT1543.Celda, 0f, max.y - min.y + CalculoMuroT1543.Celda));
                zonas.Add(new ZonaHueco { Celdas = comp.Count, Caja = caja, Barrio = CalculoMuroT1543.Barrio(centro), JuntoAlMuro = muro });
            }
            return zonas;
        }

        // ================================================================== afuera

        /// <summary>Recorre la escena y decide qué queda afuera del muro. Con aplicar = false solo cuenta y lista.</summary>
        internal class Barrido
        {
            public readonly Rect Linea;
            public readonly float Margen;
            public readonly bool Aplicar;
            public int NodosApagados;
            public int ComponentesApagados;
            public int FueraActivos;
            public readonly List<string> Recortados = new List<string>();
            public readonly List<string> NoRecortados = new List<string>();
            public readonly List<string> Asoman = new List<string>();
            public readonly List<string> NodosLista = new List<string>();
            public readonly List<string> PisosQueSobresalen = new List<string>();
            public readonly Dictionary<string, int> PorRaiz = new Dictionary<string, int>();
            private readonly Dictionary<Transform, Caja> cache = new Dictionary<Transform, Caja>();

            private struct Caja
            {
                public bool Hay;
                public Bounds B;
            }

            public Barrido(Rect linea, float grosor, bool aplicar)
            {
                Linea = linea;
                Margen = grosor + 1f;
                Aplicar = aplicar;
            }

            public string Resumen()
            {
                return (Aplicar ? "apagados " : "por apagar ") + NodosApagados + " objetos y " + ComponentesApagados + " componentes; pisos recortados "
                    + Recortados.Count + " (sin poder recortar " + NoRecortados.Count + "); asoman por el muro " + Asoman.Count;
            }

            public void Correr(Scene escena)
            {
                cache.Clear();
                foreach (GameObject raiz in escena.GetRootGameObjects())
                {
                    Decidir(raiz.transform, raiz.name);
                }
                // Lo que sigue activo con su centro afuera (para el informe y las pruebas).
                FueraActivos = ActivosAfuera(escena, Linea, Margen).Count;
            }

            private static bool ProtegidoLocal(Transform t)
            {
                string n = t.name;
                if (n == MuroCiudadT1513.NombreRaiz || n == MuroCierreT1524.NombreRaiz || n == RaizRelleno || n == RaizLuces || n == "LimiteInvisible")
                {
                    return true;
                }
                if (n == "Muro" && t.parent != null && t.parent.name == "Orillas")
                {
                    return true;
                }
                if (Protegidos.IsMatch(n))
                {
                    return true;
                }
                return t.GetComponent<Camera>() != null;
            }

            private static bool Cuenta(Renderer r)
            {
                if (r == null || !r.enabled || !r.gameObject.activeInHierarchy || EsEfecto(r))
                {
                    return false;
                }
                Bounds b = r.bounds;
                return !((b.size.x > EscalaDomo || b.size.z > EscalaDomo) && b.size.y >= AlturaPiso);
            }

            private Caja CajaDe(Transform t)
            {
                Caja c;
                if (cache.TryGetValue(t, out c))
                {
                    return c;
                }
                c = default;
                if (t.gameObject.activeSelf && !ProtegidoLocal(t))
                {
                    foreach (Renderer r in t.GetComponents<Renderer>())
                    {
                        if (Cuenta(r))
                        {
                            Unir(ref c, r.bounds);
                        }
                    }
                    foreach (Collider col in t.GetComponents<Collider>())
                    {
                        if (col.enabled && !col.isTrigger && col.bounds.size.x < EscalaDomo && col.bounds.size.z < EscalaDomo)
                        {
                            Unir(ref c, col.bounds);
                        }
                    }
                    Light l = t.GetComponent<Light>();
                    if (l != null && l.enabled && l.type != LightType.Directional)
                    {
                        Unir(ref c, new Bounds(t.position, Vector3.one * 0.1f));
                    }
                    foreach (Transform h in t)
                    {
                        Caja ch = CajaDe(h);
                        if (ch.Hay)
                        {
                            Unir(ref c, ch.B);
                        }
                    }
                }
                cache[t] = c;
                return c;
            }

            private static void Unir(ref Caja c, Bounds b)
            {
                if (!c.Hay)
                {
                    c.Hay = true;
                    c.B = b;
                }
                else
                {
                    c.B.Encapsulate(b);
                }
            }

            private void Decidir(Transform t, string raiz)
            {
                if (!t.gameObject.activeSelf || ProtegidoLocal(t))
                {
                    return;
                }
                Caja c = CajaDe(t);
                if (!c.Hay)
                {
                    return;
                }
                if (CalculoMuroT1543.CajaFuera(Linea, Margen, c.B))
                {
                    ApagarArbol(t, raiz);
                    return;
                }
                EvaluarPropios(t, raiz);
                List<Transform> hijos = new List<Transform>();
                foreach (Transform h in t)
                {
                    hijos.Add(h);
                }
                foreach (Transform h in hijos)
                {
                    Decidir(h, raiz);
                }
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

            private static bool TieneLogica(Transform t)
            {
                foreach (Collider c in t.GetComponentsInChildren<Collider>(false))
                {
                    if (c.isTrigger && Mathf.Max(c.bounds.size.x, c.bounds.size.z) <= LadoMaximoReserva)
                    {
                        return true;
                    }
                }
                foreach (MonoBehaviour mb in t.GetComponentsInChildren<MonoBehaviour>(false))
                {
                    if (mb != null && Logica.IsMatch(mb.GetType().Name))
                    {
                        return true;
                    }
                }
                return false;
            }

            /// <summary>Algún descendiente es del muro, un límite o el cielo: el objeto no se apaga entero.</summary>
            private static bool TieneProtegidos(Transform t)
            {
                foreach (Transform d in t.GetComponentsInChildren<Transform>(true))
                {
                    if (d != t && ProtegidoLocal(d))
                    {
                        return true;
                    }
                }
                return false;
            }

            private void ApagarArbol(Transform t, string raiz)
            {
                if (!t.gameObject.activeSelf || ProtegidoLocal(t))
                {
                    return;
                }
                if (!TieneLogica(t) && !TieneProtegidos(t))
                {
                    Contar(t, raiz);
                    if (Aplicar)
                    {
                        t.gameObject.SetActive(false);
                    }
                    return;
                }
                // Con lógica de juego adentro: no se apaga el objeto, solo lo que se ve y se pisa.
                ApagarVisuales(t);
                List<Transform> hijos = new List<Transform>();
                foreach (Transform h in t)
                {
                    hijos.Add(h);
                }
                foreach (Transform h in hijos)
                {
                    ApagarArbol(h, raiz);
                }
            }

            private void Contar(Transform t, string raiz)
            {
                NodosApagados++;
                int n;
                PorRaiz.TryGetValue(raiz, out n);
                PorRaiz[raiz] = n + 1;
                if (NodosLista.Count < 400)
                {
                    NodosLista.Add(Ruta(t));
                }
            }

            private void ApagarVisuales(Transform t)
            {
                foreach (Renderer r in t.GetComponents<Renderer>())
                {
                    if (Cuenta(r))
                    {
                        ComponentesApagados++;
                        if (Aplicar)
                        {
                            r.enabled = false;
                        }
                    }
                }
                foreach (Collider col in t.GetComponents<Collider>())
                {
                    if (col.enabled && !col.isTrigger)
                    {
                        ComponentesApagados++;
                        if (Aplicar)
                        {
                            col.enabled = false;
                        }
                    }
                }
                Light l = t.GetComponent<Light>();
                if (l != null && l.enabled && l.type != LightType.Directional)
                {
                    ComponentesApagados++;
                    if (Aplicar)
                    {
                        l.enabled = false;
                    }
                }
            }

            /// <summary>Apaga un objeto solo (si no tiene hijos ni lógica) o solo el componente.</summary>
            private void ApagarPropio(Transform t, string raiz, Behaviour comp, Renderer rend, Collider col)
            {
                if (t.childCount == 0 && !TieneLogica(t))
                {
                    Contar(t, raiz);
                    if (Aplicar)
                    {
                        t.gameObject.SetActive(false);
                    }
                    return;
                }
                ComponentesApagados++;
                if (Aplicar)
                {
                    if (comp != null) comp.enabled = false;
                    if (rend != null) rend.enabled = false;
                    if (col != null) col.enabled = false;
                }
            }

            private void EvaluarPropios(Transform t, string raiz)
            {
                foreach (Renderer r in t.GetComponents<Renderer>())
                {
                    if (!Cuenta(r) || !t.gameObject.activeSelf)
                    {
                        continue;
                    }
                    Bounds b = r.bounds;
                    bool plano = b.size.y < AlturaPiso && b.max.y <= TechoPiso;
                    if (CalculoMuroT1543.CajaFuera(Linea, Margen, b))
                    {
                        ApagarPropio(t, raiz, null, r, null);
                    }
                    else if (plano)
                    {
                        if (CalculoMuroT1543.Excede(Linea, CalculoMuroT1543.HolguraPiso, b))
                        {
                            PisosQueSobresalen.Add(Ruta(t) + " " + b.min.ToString("F0") + ".." + b.max.ToString("F0"));
                            RecortarPiso(t, raiz, r, b);
                        }
                    }
                    else if (CalculoMuroT1543.CentroFuera(Linea, Margen, b.center))
                    {
                        ApagarPropio(t, raiz, null, r, null);
                    }
                    else if (CalculoMuroT1543.Excede(Linea, Margen, b))
                    {
                        Asoman.Add(Ruta(t) + " " + b.min.ToString("F0") + ".." + b.max.ToString("F0"));
                    }
                }
                if (!t.gameObject.activeSelf)
                {
                    return;
                }
                foreach (Collider col in t.GetComponents<Collider>())
                {
                    if (col.enabled && !col.isTrigger && col.bounds.size.x < EscalaDomo && col.bounds.size.z < EscalaDomo
                        && CalculoMuroT1543.CentroFuera(Linea, Margen, col.bounds.center))
                    {
                        ApagarPropio(t, raiz, null, null, col);
                        if (!t.gameObject.activeSelf)
                        {
                            return;
                        }
                    }
                }
                Light l = t.GetComponent<Light>();
                if (l != null && l.enabled && l.type != LightType.Directional && CalculoMuroT1543.CentroFuera(Linea, Margen, t.position))
                {
                    ApagarPropio(t, raiz, l, null, null);
                }
            }

            private static int EjeLocal(Transform t, Vector3 ejeMundo)
            {
                Vector3 l = t.InverseTransformDirection(ejeMundo);
                float ax = Mathf.Abs(l.x), ay = Mathf.Abs(l.y), az = Mathf.Abs(l.z);
                return ax >= ay && ax >= az ? 0 : (ay >= az ? 1 : 2);
            }

            private void RecortarPiso(Transform t, string raiz, Renderer r, Bounds b)
            {
                bool alineado = Vector3.Dot(t.up, Vector3.up) > 0.999f
                    && (Mathf.Abs(t.right.x) > 0.999f || Mathf.Abs(t.right.z) > 0.999f);
                if (!alineado)
                {
                    NoRecortados.Add(Ruta(t) + " (girado: no se recorta)");
                    return;
                }
                Vector2 min, max;
                if (!CalculoMuroT1543.Recortar(Linea, CalculoMuroT1543.HolguraPiso, b, out min, out max))
                {
                    ApagarPropio(t, raiz, null, r, null);
                    return;
                }
                if (!Aplicar)
                {
                    Recortados.Add(Ruta(t));
                    return;
                }
                int ix = EjeLocal(t, Vector3.right);
                int iz = EjeLocal(t, Vector3.forward);
                if (ix == iz || b.size.x < 0.01f || b.size.z < 0.01f)
                {
                    NoRecortados.Add(Ruta(t) + " (ejes)");
                    return;
                }
                Vector3 escala = t.localScale;
                escala[ix] *= (max.x - min.x) / b.size.x;
                escala[iz] *= (max.y - min.y) / b.size.z;
                t.localScale = escala;
                Physics.SyncTransforms();
                Bounds nueva = r.bounds;
                t.position += new Vector3((min.x + max.x) * 0.5f - nueva.center.x, 0f, (min.y + max.y) * 0.5f - nueva.center.z);
                Physics.SyncTransforms();
                Recortados.Add(Ruta(t));
            }
        }

        /// <summary>Renderers y luces activos cuyo centro queda fuera del muro (para pruebas): ruta del objeto y su centro.</summary>
        public static List<string> ActivosAfuera(Scene escena, Rect linea, float margen)
        {
            List<string> lista = new List<string>();
            foreach (GameObject raiz in escena.GetRootGameObjects())
            {
                foreach (Renderer r in raiz.GetComponentsInChildren<Renderer>(false))
                {
                    if (!r.enabled || !r.gameObject.activeInHierarchy || EsEfecto(r) || EnProtegido(r.transform))
                    {
                        continue;
                    }
                    Bounds b = r.bounds;
                    if ((b.size.x > EscalaDomo || b.size.z > EscalaDomo) && b.size.y >= AlturaPiso)
                    {
                        continue;
                    }
                    if (CalculoMuroT1543.CentroFuera(linea, margen, b.center))
                    {
                        lista.Add(RutaDe(r.transform) + " " + b.center.ToString("F0"));
                    }
                }
                foreach (Light l in raiz.GetComponentsInChildren<Light>(false))
                {
                    if (l.enabled && l.type != LightType.Directional && l.gameObject.activeInHierarchy && !EnProtegido(l.transform)
                        && CalculoMuroT1543.CentroFuera(linea, margen, l.transform.position))
                    {
                        lista.Add(RutaDe(l.transform) + " (luz) " + l.transform.position.ToString("F0"));
                    }
                }
            }
            return lista;
        }

        private static bool EnProtegido(Transform t)
        {
            for (Transform p = t; p != null; p = p.parent)
            {
                string n = p.name;
                if (n == MuroCiudadT1513.NombreRaiz || n == MuroCierreT1524.NombreRaiz || n == RaizRelleno || n == RaizLuces || n == "LimiteInvisible"
                    || (n == "Muro" && p.parent != null && p.parent.name == "Orillas") || Protegidos.IsMatch(n) || p.GetComponent<Camera>() != null)
                {
                    return true;
                }
            }
            return false;
        }

        private static string RutaDe(Transform t)
        {
            StringBuilder sb = new StringBuilder(t.name);
            for (Transform p = t.parent; p != null; p = p.parent)
            {
                sb.Insert(0, p.name + "/");
            }
            return sb.ToString();
        }

        // ================================================================== relleno

        internal class RellenoInforme
        {
            public int Celdas;
            public int Zonas;
            public int Reservadas;
            public int Lotes;
            public int Edificios;
            public int Encimados;
            public List<string> SinCabida = new List<string>();
        }

        private static string[] Anteriores(string clave)
        {
            switch (clave)
            {
                case "NeonAlto": return ReemplazarEdificiosT1502.EstiloNeon;
                case "Suburbios": return ReemplazarEdificiosT1502.EstiloSuburbios;
                default: return ReemplazarEdificiosT1502.EstiloMercado;
            }
        }

        private static Dictionary<string, List<ReemplazarEdificiosT1502.Medida>> CargarPaletas()
        {
            Dictionary<string, ReemplazarEdificiosT1502.Medida> cache = new Dictionary<string, ReemplazarEdificiosT1502.Medida>();
            Dictionary<string, List<ReemplazarEdificiosT1502.Medida>> r = new Dictionary<string, List<ReemplazarEdificiosT1502.Medida>>();
            foreach (string clave in new[] { "NeonAlto", "Suburbios", "Mercado" })
            {
                string[] rutas = CalculoEdificiosT1512.RutasPara(clave).Concat(Anteriores(clave)).Distinct().ToArray();
                r[clave] = ReemplazarEdificiosT1502.Medidas(rutas, cache);
            }
            return r;
        }

        private static bool Libre(Mapa m, Vector2 p)
        {
            if (!m.Interior.Contains(p) || EsCalleOBanqueta(m, p) || Reservado(m, p, 1f))
            {
                return false;
            }
            float y = AlturaSuelo(m, p);
            return !m.Ocupantes.Cerca(p, 0.05f, y + 0.3f, y + AlturaMaximaOcupante);
        }

        /// <summary>El borde del lado está libre en todo su largo (muestras cada metro, 0.1 m hacia adentro).</summary>
        private static bool BordeLibre(Mapa m, Rect r, CalculoMuroT1543.Lado lado)
        {
            bool enX = lado == CalculoMuroT1543.Lado.Este || lado == CalculoMuroT1543.Lado.Oeste;
            float largo = enX ? r.height : r.width;
            int n = Mathf.Max(2, Mathf.CeilToInt(largo));
            for (int i = 0; i <= n; i++)
            {
                float f = Mathf.Lerp(0.25f, Mathf.Max(0.25f, largo - 0.25f), i / (float)n);
                Vector2 p;
                switch (lado)
                {
                    case CalculoMuroT1543.Lado.Este: p = new Vector2(r.xMax - 0.1f, r.yMin + f); break;
                    case CalculoMuroT1543.Lado.Oeste: p = new Vector2(r.xMin + 0.1f, r.yMin + f); break;
                    case CalculoMuroT1543.Lado.Norte: p = new Vector2(r.xMin + f, r.yMax - 0.1f); break;
                    default: p = new Vector2(r.xMin + f, r.yMin + 0.1f); break;
                }
                if (!Libre(m, p))
                {
                    return false;
                }
            }
            return true;
        }

        private static Rect Mover(Rect r, CalculoMuroT1543.Lado lado, float d)
        {
            switch (lado)
            {
                case CalculoMuroT1543.Lado.Este: return Rect.MinMaxRect(r.xMin, r.yMin, r.xMax + d, r.yMax);
                case CalculoMuroT1543.Lado.Oeste: return Rect.MinMaxRect(r.xMin - d, r.yMin, r.xMax, r.yMax);
                case CalculoMuroT1543.Lado.Norte: return Rect.MinMaxRect(r.xMin, r.yMin, r.xMax, r.yMax + d);
                default: return Rect.MinMaxRect(r.xMin, r.yMin - d, r.xMax, r.yMax);
            }
        }

        /// <summary>Retrae cada lado hasta que no pise calle, banqueta ni otros edificios, y luego lo estira hasta tocar al vecino (máximo 4 m por lado).</summary>
        private static bool AjustarLados(Mapa m, ref Rect r)
        {
            CalculoMuroT1543.Lado[] lados = { CalculoMuroT1543.Lado.Este, CalculoMuroT1543.Lado.Oeste, CalculoMuroT1543.Lado.Norte, CalculoMuroT1543.Lado.Sur };
            for (int pasada = 0; pasada < 2; pasada++)
            {
                foreach (CalculoMuroT1543.Lado lado in lados)
                {
                    float retraido = 0f;
                    while (!BordeLibre(m, r, lado))
                    {
                        if (retraido >= CalculoMuroT1543.AjusteMaximoLado)
                        {
                            return false;
                        }
                        r = Mover(r, lado, -CalculoMuroT1543.PasoAjuste);
                        retraido += CalculoMuroT1543.PasoAjuste;
                        if (r.width < 3f || r.height < 3f)
                        {
                            return false;
                        }
                    }
                    float estirado = 0f;
                    while (estirado < CalculoMuroT1543.AjusteMaximoLado)
                    {
                        Rect t = Mover(r, lado, CalculoMuroT1543.PasoAjuste);
                        if (!BordeLibre(m, t, lado))
                        {
                            break;
                        }
                        r = t;
                        estirado += CalculoMuroT1543.PasoAjuste;
                    }
                }
            }
            return r.width >= 3f && r.height >= 3f;
        }

        /// <summary>El frente del lote: de espaldas al muro si lo toca, si no hacia el lado con más calle, si no hacia el centro del mapa.</summary>
        private static Vector3 ElegirFrente(Mapa m, Rect r)
        {
            float tocar = m.Grosor * 0.5f + 5f;
            float dE = m.Linea.xMax - r.xMax, dO = r.xMin - m.Linea.xMin, dN = m.Linea.yMax - r.yMax, dS = r.yMin - m.Linea.yMin;
            float mejor = tocar;
            Vector3 frente = Vector3.zero;
            if (dE < mejor) { mejor = dE; frente = Vector3.left; }
            if (dO < mejor) { mejor = dO; frente = Vector3.right; }
            if (dN < mejor) { mejor = dN; frente = Vector3.back; }
            if (dS < mejor) { frente = Vector3.forward; }
            if (frente != Vector3.zero)
            {
                return frente;
            }
            int[] calle = new int[4];
            Vector3[] dirs = { Vector3.right, Vector3.left, Vector3.forward, Vector3.back };
            for (int k = 0; k < 4; k++)
            {
                bool enX = k < 2;
                float largo = enX ? r.height : r.width;
                for (float f = 1f; f < largo; f += 2f)
                {
                    Vector2 p;
                    switch (k)
                    {
                        case 0: p = new Vector2(r.xMax + 2f, r.yMin + f); break;
                        case 1: p = new Vector2(r.xMin - 2f, r.yMin + f); break;
                        case 2: p = new Vector2(r.xMin + f, r.yMax + 2f); break;
                        default: p = new Vector2(r.xMin + f, r.yMin - 2f); break;
                    }
                    if (EsCalleOBanqueta(m, p))
                    {
                        calle[k]++;
                    }
                }
            }
            int mejorK = 0;
            for (int k = 1; k < 4; k++)
            {
                if (calle[k] > calle[mejorK])
                {
                    mejorK = k;
                }
            }
            if (calle[mejorK] > 0)
            {
                return dirs[mejorK];
            }
            Vector2 haciaCentro = m.Linea.center - r.center;
            return Mathf.Abs(haciaCentro.x) > Mathf.Abs(haciaCentro.y) ? new Vector3(Mathf.Sign(haciaCentro.x), 0f, 0f) : new Vector3(0f, 0f, Mathf.Sign(haciaCentro.y));
        }

        internal static RellenoInforme Rellenar(Scene escena, Mapa m)
        {
            RellenoInforme info = new RellenoInforme();
            int reservadas;
            List<Vector2Int> huecos = BuscarHuecos(m, out reservadas);
            info.Celdas = huecos.Count;
            info.Reservadas = reservadas;
            info.Zonas = CalculoMuroT1543.Componentes(huecos).Count;
            GameObject raiz = new GameObject(RaizRelleno);
            SceneManager.MoveGameObjectToScene(raiz, escena);
            raiz.isStatic = true;
            if (huecos.Count == 0)
            {
                return info;
            }
            Dictionary<string, List<ReemplazarEdificiosT1502.Medida>> paletas = CargarPaletas();
            List<RectInt> rects = CalculoMuroT1543.Rectangulos(huecos);
            rects.Sort((a, b) => (b.width * b.height).CompareTo(a.width * a.height));
            int lote = 0;
            foreach (RectInt rc in rects)
            {
                Rect r = CalculoMuroT1543.EnMetros(rc);
                string barrio = CalculoMuroT1543.Barrio(r.center);
                string etiqueta = "lote " + r.center.ToString("F0") + " " + r.width.ToString("F0") + "x" + r.height.ToString("F0") + " m, " + barrio;
                if (!AjustarLados(m, ref r))
                {
                    info.SinCabida.Add(etiqueta + ": no hay lado libre (pisa calle, banqueta u otro edificio)");
                    continue;
                }
                List<ReemplazarEdificiosT1502.Medida> paleta;
                if (!paletas.TryGetValue(CalculoMuroT1543.ClavePaleta(barrio), out paleta) || paleta.Count == 0)
                {
                    paleta = paletas.Values.FirstOrDefault(p => p.Count > 0);
                }
                if (paleta == null || paleta.Count == 0)
                {
                    info.SinCabida.Add(etiqueta + ": no hay modelos de edificio importados");
                    continue;
                }
                Vector3 frente = ElegirFrente(m, r);
                bool frenteEnZ = Mathf.Abs(frente.z) > 0.5f;
                float ancho = frenteEnZ ? r.width : r.height;
                float fondo = frenteEnZ ? r.height : r.width;
                List<CalculoMuroT1543.Modelo> modelos = paleta.Select(x => new CalculoMuroT1543.Modelo { Tam = x.Tam }).ToList();
                int semilla = Mathf.Abs(Mathf.RoundToInt(r.center.x * 7f + r.center.y * 13f));
                List<CalculoMuroT1543.Ajuste> reparto = CalculoMuroT1543.RepartirFrente(modelos, ancho, fondo, semilla);
                if (reparto.Count == 0)
                {
                    info.SinCabida.Add(etiqueta + ": ningun modelo llena " + ancho.ToString("F1") + "x" + fondo.ToString("F1") + " m con estiramiento hasta " + CalculoMuroT1543.EstiramientoMaximo);
                    continue;
                }
                lote++;
                GameObject contenedor = new GameObject("Lote_" + lote + "_" + barrio);
                contenedor.transform.SetParent(raiz.transform, false);
                contenedor.isStatic = true;
                Quaternion rot = Quaternion.LookRotation(frente, Vector3.up);
                Vector3 der = Vector3.Cross(Vector3.up, frente);
                float w = ancho / reparto.Count;
                int puestos = 0;
                for (int k = 0; k < reparto.Count; k++)
                {
                    CalculoMuroT1543.Ajuste aj = reparto[k];
                    ReemplazarEdificiosT1502.Medida modelo = paleta[aj.Indice];
                    float lateral = -ancho * 0.5f + (k + 0.5f) * w;
                    Vector2 c2 = r.center + new Vector2(der.x, der.z) * lateral;
                    float y = AlturaSuelo(m, c2);
                    Vector3 baseCentro = new Vector3(c2.x, y, c2.y);
                    GameObject pieza = (GameObject)PrefabUtility.InstantiatePrefab(modelo.Prefab, contenedor.transform);
                    pieza.name = "Relleno_" + modelo.Prefab.name + "_" + k;
                    pieza.transform.SetPositionAndRotation(baseCentro - rot * Vector3.Scale(modelo.Desfase, aj.Escala), rot);
                    pieza.transform.localScale = aj.Escala;
                    pieza.isStatic = true;
                    if (pieza.GetComponentInChildren<Collider>(true) == null)
                    {
                        BoxCollider bc = pieza.AddComponent<BoxCollider>();
                        bc.size = modelo.Tam;
                        bc.center = new Vector3(modelo.Desfase.x, modelo.Desfase.y + modelo.Tam.y * 0.5f, modelo.Desfase.z);
                    }
                    Physics.SyncTransforms();
                    bool encima = false;
                    List<Bounds> cajas = new List<Bounds>();
                    foreach (Renderer rd in pieza.GetComponentsInChildren<Renderer>(false))
                    {
                        if (EsEfecto(rd))
                        {
                            continue;
                        }
                        cajas.Add(rd.bounds);
                        if (EsOcupante(rd.bounds) && m.Ocupantes.Choca(rd.bounds, 0.3f))
                        {
                            encima = true;
                        }
                    }
                    if (encima)
                    {
                        Object.DestroyImmediate(pieza);
                        info.Encimados++;
                        continue;
                    }
                    foreach (Bounds cb in cajas)
                    {
                        if (EsOcupante(cb))
                        {
                            m.Ocupantes.Agregar(cb);
                        }
                    }
                    puestos++;
                }
                if (puestos == 0)
                {
                    Object.DestroyImmediate(contenedor);
                    info.SinCabida.Add(etiqueta + ": todos los edificios se encimaban con otro");
                    continue;
                }
                info.Lotes++;
                info.Edificios += puestos;
            }
            return info;
        }

        // ================================================================== torres y reflectores

        internal class TorresInforme
        {
            public int Total;
            public int Esquinas;
            public int Salidas;
            public int Agregadas;
            public int Luces;
            public List<string> EsquinasSinTorre = new List<string>();
        }

        /// <summary>
        /// Revisa que haya torre MU02 en las 4 esquinas y dos por puerta (si falta alguna, la pone con el mismo plan de T-1513) y les pone un reflector:
        /// una Light spot cálida arriba de la torre, hacia afuera y abajo. No hay modelo de reflector en Assets, así que no se agrega ninguno.
        /// Los valores de la luz (intensidad, alcance, ángulo) no se pudieron medir con Unity: ajustar a ojo con las capturas.
        /// </summary>
        internal static TorresInforme Torres(Scene escena, Mapa m)
        {
            TorresInforme info = new TorresInforme();
            GameObject luces = new GameObject(RaizLuces);
            SceneManager.MoveGameObjectToScene(luces, escena);
            GameObject t1513 = GameObject.Find(MuroCiudadT1513.NombreRaiz);
            List<Transform> torres = new List<Transform>();
            if (t1513 != null)
            {
                foreach (Transform h in t1513.transform)
                {
                    if (h.name.StartsWith("Torre") && h.gameObject.activeSelf)
                    {
                        torres.Add(h);
                    }
                }
            }

            List<Transform> muros = MuroCiudadT1513.MurosViejos();
            if (muros.Count > 0)
            {
                GameObject prefabTorre = MuroCiudadT1513.Cargar(MuroCiudadT1513.PrefabTorre);
                Vector3 tam = MuroCiudadT1513.Medir(prefabTorre);
                CalculoMuroT1513.Plan plan = CalculoMuroT1513.Planear(muros.Select(MuroCiudadT1513.Segmento).ToList(), new Vector2(tam.x, tam.z));
                int n = 0;
                foreach (CalculoMuroT1513.Torre t in plan.Torres)
                {
                    bool hay = torres.Any(x => DistanciaATorre(x, t.Centro) < 3f);
                    if (!hay)
                    {
                        GameObject g = MuroCiudadT1513.Colocar(prefabTorre, luces.transform, (t.Esquina ? "TorreEsquina_falta_" : "TorreSalida_falta_") + n, t.Centro, Vector3.forward, 1f, 1f, false, 0f);
                        torres.Add(g.transform);
                        info.Agregadas++;
                    }
                    n++;
                }
                info.Esquinas = plan.Torres.Count(x => x.Esquina);
                info.Salidas = plan.Torres.Count(x => !x.Esquina);
            }
            info.Total = torres.Count;

            Vector3[] esquinas =
            {
                new Vector3(m.Linea.xMin, 0f, m.Linea.yMin), new Vector3(m.Linea.xMax, 0f, m.Linea.yMin),
                new Vector3(m.Linea.xMin, 0f, m.Linea.yMax), new Vector3(m.Linea.xMax, 0f, m.Linea.yMax)
            };
            foreach (Vector3 e in esquinas)
            {
                bool hay = torres.Any(x => DistanciaATorre(x, e) < 4f);
                if (!hay)
                {
                    info.EsquinasSinTorre.Add(e.ToString("F0"));
                }
            }

            foreach (Transform torre in torres)
            {
                Bounds? cb = CentroDe(torre);
                if (!cb.HasValue)
                {
                    continue;
                }
                Bounds b = cb.Value;
                Vector3 afuera = CalculoMuroT1543.DireccionAfuera(b.center, m.Linea);
                GameObject g = new GameObject("Reflector_" + torre.name);
                g.transform.SetParent(luces.transform, false);
                float saliente = Mathf.Min(b.extents.x, b.extents.z) * 0.8f;
                g.transform.position = new Vector3(b.center.x, b.max.y - 0.6f, b.center.z) + afuera * saliente;
                g.transform.rotation = Quaternion.LookRotation(new Vector3(afuera.x * 0.8f, InclinacionReflector, afuera.z * 0.8f));
                Light l = g.AddComponent<Light>();
                l.type = LightType.Spot;
                l.color = ColorReflector;
                l.intensity = IntensidadReflector;
                l.range = AlcanceReflector;
                l.spotAngle = AnguloReflector;
                l.innerSpotAngle = AnguloInteriorReflector;
                l.shadows = LightShadows.None;
                info.Luces++;
            }
            return info;
        }

        private static float DistanciaATorre(Transform torre, Vector3 punto)
        {
            Bounds? b = CentroDe(torre);
            return b.HasValue ? Vector2.Distance(new Vector2(b.Value.center.x, b.Value.center.z), new Vector2(punto.x, punto.z)) : float.MaxValue;
        }

        private static Bounds? CentroDe(Transform t)
        {
            Renderer[] rs = t.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0)
            {
                return null;
            }
            Bounds b = rs[0].bounds;
            foreach (Renderer r in rs)
            {
                b.Encapsulate(r.bounds);
            }
            return b;
        }

        // ================================================================== informe

        private static string Informe(string titulo, Scene escena, Barrido b, Mapa m)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("T-1543 diagnostico " + titulo + " (" + DateTime.Now.ToString("yyyy-MM-dd HH:mm") + ")");
            sb.AppendLine("Linea del muro: x " + m.Linea.xMin + ".." + m.Linea.xMax + ", z " + m.Linea.yMin + ".." + m.Linea.yMax + "; grosor " + m.Grosor.ToString("F2")
                + " m; margen de 'fuera' " + b.Margen.ToString("F2") + " m");
            sb.AppendLine("Renderers activos medidos: " + m.Activos + ", pisos (planos) " + m.Pisos.Count + " (calle " + m.Pisos.Count(x => x.Tipo == TipoPiso.Calle)
                + ", banqueta " + m.Pisos.Count(x => x.Tipo == TipoPiso.Banqueta) + " de ellos manzanas " + m.Pisos.Count(x => x.Bloque) + ", marcas " + m.Pisos.Count(x => x.Tipo == TipoPiso.Marca)
                + ", otros " + m.Pisos.Count(x => x.Tipo == TipoPiso.Otro) + "), ocupantes " + m.Ocupantes.Cuenta + ", reservas de gameplay " + m.Reservas.Count);
            sb.AppendLine();
            sb.AppendLine("== 1. FUERA DEL MURO ==");
            sb.AppendLine(b.Resumen());
            sb.AppendLine("Renderers/luces activos con su centro afuera (ahora): " + b.FueraActivos);
            sb.AppendLine("Por raiz de la escena (objetos que se apagan):");
            foreach (KeyValuePair<string, int> kv in b.PorRaiz.OrderByDescending(x => x.Value))
            {
                sb.AppendLine("  " + kv.Key + ": " + kv.Value);
            }
            sb.AppendLine("Pisos/terreno que se extienden mas alla de muro + " + CalculoMuroT1543.HolguraPiso + " m (" + b.PisosQueSobresalen.Count + "):");
            foreach (string s in b.PisosQueSobresalen.Take(60))
            {
                sb.AppendLine("  " + s);
            }
            sb.AppendLine("Pisos que no se pueden recortar por estar girados (" + b.NoRecortados.Count + "):");
            foreach (string s in b.NoRecortados.Take(30))
            {
                sb.AppendLine("  " + s);
            }
            sb.AppendLine("Objetos altos que asoman por el muro con el centro adentro (se dejan; " + b.Asoman.Count + "):");
            foreach (string s in b.Asoman.Take(60))
            {
                sb.AppendLine("  " + s);
            }
            sb.AppendLine("Primeros objetos a apagar:");
            foreach (string s in b.NodosLista.Take(150))
            {
                sb.AppendLine("  " + s);
            }
            sb.AppendLine();
            sb.AppendLine("== 2. OBJETOS DE GAMEPLAY AFUERA (no se apagan; solo lo que se ve) ==");
            foreach (string s in GameplayAfuera(escena, m.Linea, b.Margen).Take(100))
            {
                sb.AppendLine("  " + s);
            }
            sb.AppendLine();
            sb.AppendLine("== 3. HUECOS DENTRO DEL MURO (rejilla " + CalculoMuroT1543.Celda + " m, radio " + CalculoMuroT1543.RadioHueco + " m, alto " + CalculoMuroT1543.AltoHueco + " m) ==");
            int reservadas;
            List<Vector2Int> huecos = BuscarHuecos(m, out reservadas);
            List<ZonaHueco> zonas = Zonas(m, huecos);
            sb.AppendLine("Celdas hueco: " + huecos.Count + " (" + huecos.Count * CalculoMuroT1543.Celda * CalculoMuroT1543.Celda + " m2) en " + zonas.Count + " zonas; reservadas por gameplay: " + reservadas + " celdas");
            foreach (ZonaHueco z in zonas.Take(80))
            {
                sb.AppendLine("  " + z.Area.ToString("F0") + " m2, centro " + z.Caja.center.ToString("F0") + ", tam " + z.Caja.size.x.ToString("F0") + "x" + z.Caja.size.z.ToString("F0")
                    + ", " + z.Barrio + (z.JuntoAlMuro ? ", junto al muro" : ""));
            }
            float franja = zonas.Where(z => z.JuntoAlMuro).Sum(z => z.Area);
            sb.AppendLine("Franja entre la ultima cuadra y el muro (zonas junto al muro a menos de " + DistanciaFranja + " m): " + franja.ToString("F0") + " m2 en "
                + zonas.Count(z => z.JuntoAlMuro) + " zonas");
            return sb.ToString();
        }

        private static List<string> GameplayAfuera(Scene escena, Rect linea, float margen)
        {
            List<string> lista = new List<string>();
            foreach (GameObject raiz in escena.GetRootGameObjects())
            {
                foreach (MonoBehaviour mb in raiz.GetComponentsInChildren<MonoBehaviour>(false))
                {
                    if (mb == null || !mb.enabled || EnProtegido(mb.transform) || !Logica.IsMatch(mb.GetType().Name))
                    {
                        continue;
                    }
                    if (CalculoMuroT1543.CentroFuera(linea, margen, mb.transform.position))
                    {
                        lista.Add(mb.GetType().Name + " en " + RutaDe(mb.transform) + " " + mb.transform.position.ToString("F0"));
                    }
                }
                foreach (Collider c in raiz.GetComponentsInChildren<Collider>(false))
                {
                    if (c.enabled && c.isTrigger && !EnProtegido(c.transform) && CalculoMuroT1543.CentroFuera(linea, margen, c.bounds.center))
                    {
                        lista.Add("Trigger " + RutaDe(c.transform) + " " + c.bounds.center.ToString("F0"));
                    }
                }
            }
            return lista;
        }

        // ================================================================== vistas para capturas

        public struct Vista
        {
            public string Nombre;
            public Vector3 Posicion;
            public Vector3 Objetivo;
            public float Fov;
            public string Nota;
        }

        /// <summary>Las vistas de A24, K32, K33, K34 y una aérea completa, sacadas de la línea del muro (siempre desde adentro).</summary>
        public static List<Vista> Vistas(Rect l)
        {
            float cx = l.center.x;
            float cz = l.center.y;
            Vector3 puertaNorte = new Vector3(47f, 0f, l.yMax);
            List<Transform> muros = MuroCiudadT1513.MurosViejos();
            if (muros.Count > 0)
            {
                GameObject raiz = GameObject.Find(MuroCiudadT1513.NombreRaiz);
                if (raiz != null)
                {
                    Transform p = raiz.transform.Cast<Transform>().Where(h => h.name.StartsWith("Puerta_") && Mathf.Abs(h.position.z - l.yMax) < 6f).OrderBy(h => h.position.x).LastOrDefault();
                    if (p != null)
                    {
                        puertaNorte = new Vector3(p.position.x, 0f, l.yMax);
                    }
                }
            }
            return new List<Vista>
            {
                new Vista { Nombre = "aerea_completa", Posicion = new Vector3(cx, 330f, cz - 250f), Objetivo = new Vector3(cx, 0f, cz + 10f), Fov = 55f, Nota = "toda la ciudad dentro del muro; afuera solo cielo" },
                new Vista { Nombre = "A24_muro_aerea", Posicion = new Vector3(l.xMin + 75f, 48f, l.yMax - 38f), Objetivo = new Vector3(l.xMin + 4f, 6f, l.yMax - 2f), Fov = 60f, Nota = "aerea desde adentro siguiendo el muro norte hasta la torre de la esquina noroeste" },
                new Vista { Nombre = "K32_muro_calle", Posicion = new Vector3(l.xMin + 30f, 4f, cz + 10f), Objetivo = new Vector3(l.xMin, 6f, cz + 10f), Fov = 65f, Nota = "a nivel de calle junto al muro oeste, casas pegadas al muro" },
                new Vista { Nombre = "K33_muro_puerta", Posicion = new Vector3(puertaNorte.x, 5f, l.yMax - 32f), Objetivo = new Vector3(puertaNorte.x, 6f, l.yMax), Fov = 65f, Nota = "la calle de frente a la puerta MU03 del norte, torre a cada lado con reflector" },
                new Vista { Nombre = "K34_muro_esquina", Posicion = new Vector3(l.xMax - 30f, 10f, l.yMin + 30f), Objetivo = new Vector3(l.xMax, 6f, l.yMin), Fov = 60f, Nota = "la esquina sureste con su torre MU02" }
            };
        }
    }
}
