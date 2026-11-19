using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MechaCyberpunk.Core.Mundo;
using MechaCyberpunk.Core.Trepar;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-1360: barrido automático de bugs de las zonas nuevas (Colonia del Cerro y Puerto). Solo detecta y fotografía; no arregla nada.
    /// <see cref="BarrerZona"/> recibe la zona ya instanciada en una escena y devuelve los hallazgos agrupados (a menos de
    /// <see cref="RadioAgrupar"/> m, mismo tipo). <see cref="Barrer"/> hace todas las zonas, escribe Logs/t1360-barrido-*.json y .md
    /// y toma fotos; <see cref="FotosDespues"/> repite las fotos desde la misma cámara tras los arreglos.
    /// Batch: -executeMethod MechaCyberpunk.Arte.Editor.BarridoZonasT1360.Barrer [-sufijo antes|despues].
    /// </summary>
    public static class BarridoZonasT1360
    {
        // ------------------------------------------------------------------ tipos de hallazgo
        public const string TipoFlota = "flota";
        public const string TipoEnterrado = "enterrado";
        public const string TipoEncimado = "encimado";
        public const string TipoParpadea = "parpadea";
        public const string TipoSinCollider = "sin collider";
        public const string TipoColliderNoCorresponde = "collider que no corresponde";
        public const string TipoMuroInvisible = "muro invisible";
        public const string TipoRampaRota = "rampa rota";
        public const string TipoTexturaRota = "textura rota";
        public const string TipoEscalon = "escalón";
        public const string TipoHueco = "hueco";
        public const string TipoPendiente = "pendiente";
        public const string TipoEscaleraSalto = "escalera con salto";
        public const string TipoVacio = "hueco al vacío";
        public const string TipoAtrapado = "atrapado";
        public const string TipoNoCabeJugador = "no cabe el jugador";
        public const string TipoNoCabeCarro = "no cabe un carro";
        public const string TipoAzoteaAislada = "azotea aislada";
        public const string TipoPretil = "pretil que bloquea";
        public const string TipoContenedor = "contenedor no escalable";
        public const string TipoCarro = "carro en el carril";
        public const string TipoPeaton = "ruta de peatón atorada";

        // ------------------------------------------------------------------ números del barrido (todos en m salvo que se diga)
        public const float RadioAgrupar = 3f;
        public const float TolerFlota = 0.08f;
        public const float TolerEnterrado = 0.25f;
        public const float UmbralEncimado = 0.15f;
        public const float UmbralEncimadoCasas = 0.3f;
        public const float PasoCalle = 0.25f;
        public const float EscalonCalle = 0.3f;
        public const float PendienteCarro = 0.15f;
        public const float StepOffset = 0.35f;
        public const float RadioJugador = 0.35f;
        public const float AltoJugador = 1.8f;
        public const float AgarreMaximo = 2.3f;
        public const float CaidaMaxima = 3f;
        public const float HuecoSaltoMaximo = 2f;
        public const float BajadaSaltoMaxima = 4f;
        public const float AnchoMinJugador = 0.9f;
        public const float AnchoMinCarro = 3f;
        public const int MaxRayosVacio = 30000;
        public const int FotosPorTipo = 6;
        public const int TopMd = 40;

        private const float PasoVacioMinimo = 2f;
        private const float AltoRayoVacio = 80f;
        private const float CeldaIndice = 8f;
        private const float GrosorPlano = 0.05f;
        private const float AreaPlanos = 0.2f;
        private const float AreaCajas = 1f;
        private const float CoplanarTol = 0.01f;
        private const float PretilAlto = 0.45f;
        private const float VecinaAzotea = 4f;
        private const float MargenCollider = 0.5f;
        private const float RatioVolumenMin = 0.4f;
        private const float TamSolido = 0.8f;
        private const float AltoSolido = 0.4f;
        private const float AguaBajoMax = 3f;

        private const string RutaFotos = "docs/capturas/bugs-18nov-B";
        private static readonly string[] RutasZonas =
        {
            "Assets/Arte/Mundo/Zonas/T1355_Cerro.prefab",
            "Assets/Arte/Mundo/Zonas/T1357_Puerto.prefab",
        };

        private static readonly string[] Contenedores =
            { "Casas", "Postes", "Carros", "Detalles", "Contenedores", "Bodegas", "Muelle", "Escaleras" };

        private static readonly RaycastHit[] bufHits = new RaycastHit[512];
        private static readonly Collider[] bufCols = new Collider[512];
        private static readonly Collider[] bufEsfera = new Collider[64];

        // ------------------------------------------------------------------ datos de salida

        [Serializable]
        public class Hallazgo
        {
            public string zona;
            public string tipo;
            public string ruta;
            public Vector3 posicion;
            public string detalle;
            /// <summary>Gravedad numérica del hallazgo (m, m², %...): sirve para ordenar los peores.</summary>
            public float valor;
            /// <summary>Cuántos hallazgos del mismo tipo a menos de <see cref="RadioAgrupar"/> m se juntaron en este.</summary>
            public int cantidad = 1;
            public bool conCamara;
            public Vector3 camaraPosicion;
            public Vector3 camaraAngulos;
            public int fotoN;
            public string foto;
        }

        [Serializable]
        public class ArchivoHallazgos
        {
            public string zona;
            public string sufijo;
            public List<Hallazgo> hallazgos = new List<Hallazgo>();
        }

        // ------------------------------------------------------------------ contexto

        private sealed class Prop
        {
            public Transform T;
            public string Nombre;
            public readonly List<Renderer> Rs = new List<Renderer>();
            public readonly List<Collider> Cs = new List<Collider>();
            public readonly HashSet<Collider> Propios = new HashSet<Collider>();
            public bool TieneRs;
            public bool TieneCs;
            public Bounds RB;
            public Bounds CB;
            public Bounds B;
            public bool TieneB;
            public bool EsCimiento => Nombre.StartsWith("Cimiento", StringComparison.Ordinal);
        }

        private sealed class Ctx
        {
            public Transform Raiz;
            public string Zona;
            public readonly List<Hallazgo> Salida = new List<Hallazgo>();
            public readonly List<Prop> Props = new List<Prop>();
            public readonly Dictionary<Transform, Prop> PorRaiz = new Dictionary<Transform, Prop>();
            public readonly Dictionary<Collider, Prop> PropDe = new Dictionary<Collider, Prop>();
            public readonly List<Collider> Colliders = new List<Collider>();
            public readonly List<Renderer> Rs = new List<Renderer>();
            public readonly List<Bounds> RsB = new List<Bounds>();
            public readonly Dictionary<long, List<int>> Grid = new Dictionary<long, List<int>>();
            public readonly Dictionary<int, bool> Ign = new Dictionary<int, bool>();
            public readonly Dictionary<int, bool> Carro = new Dictionary<int, bool>();
            public readonly HashSet<int> TmpSet = new HashSet<int>();
            public Bounds Limites;
            public bool TieneLimites;
            public Transform Mar;
            public bool EsPuerto => Zona.IndexOf("Puerto", StringComparison.OrdinalIgnoreCase) >= 0;
            public bool EsCerro => Zona.IndexOf("Cerro", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        // ------------------------------------------------------------------ puntos de entrada

        /// <summary>Batch: barre las zonas, escribe JSON, informe y fotos (sufijo con -sufijo, por defecto "antes").</summary>
        public static void Barrer()
        {
            string sufijo = LeerSufijo();
            string raizProyecto = RaizProyecto();
            Directory.CreateDirectory(Path.Combine(raizProyecto, "Logs"));
            List<ArchivoHallazgos> archivos = new List<ArchivoHallazgos>();
            foreach (string ruta in RutasZonas)
            {
                GameObject zona = AbrirZona(ruta);
                if (zona == null)
                {
                    Debug.LogError("[BarridoZonasT1360] No se pudo abrir " + ruta);
                    continue;
                }
                ArchivoHallazgos archivo = new ArchivoHallazgos { zona = NombreZona(zona.name), sufijo = sufijo };
                try
                {
                    archivo.hallazgos.AddRange(BarrerZona(zona));
                    foreach (Hallazgo h in archivo.hallazgos)
                    {
                        AsignarCamara(h);
                    }
                    SeleccionarFotos(archivo, sufijo);
                    ArteT250.PonerLuz();
                    TomarFotos(archivo, raizProyecto, sufijo);
                }
                catch (Exception e)
                {
                    Debug.LogError("[BarridoZonasT1360] Falló la zona " + archivo.zona + ": " + e);
                }
                File.WriteAllText(Path.Combine(raizProyecto, "Logs", "t1360-barrido-" + archivo.zona + "-" + sufijo + ".json"),
                    JsonUtility.ToJson(archivo, true));
                archivos.Add(archivo);
            }
            File.WriteAllText(Path.Combine(raizProyecto, "Logs", "t1360-barrido-" + sufijo + ".md"), Informe(archivos, sufijo));
            Debug.Log("[BarridoZonasT1360] Listo (" + sufijo + "): " + string.Join(", ", archivos.Select(a => a.zona + "=" + a.hallazgos.Count)));
        }

        /// <summary>Batch: relee el JSON "antes" y toma las mismas fotos, desde la misma cámara, con sufijo "despues".</summary>
        public static void FotosDespues()
        {
            string raizProyecto = RaizProyecto();
            foreach (string ruta in RutasZonas)
            {
                string zonaNombre = NombreZona(Path.GetFileNameWithoutExtension(ruta));
                string json = Path.Combine(raizProyecto, "Logs", "t1360-barrido-" + zonaNombre + "-antes.json");
                if (!File.Exists(json))
                {
                    Debug.LogError("[BarridoZonasT1360] Falta " + json);
                    continue;
                }
                ArchivoHallazgos antes = JsonUtility.FromJson<ArchivoHallazgos>(File.ReadAllText(json));
                GameObject zona = AbrirZona(ruta);
                if (zona == null)
                {
                    continue;
                }
                ArteT250.PonerLuz();
                TomarFotos(antes, raizProyecto, "despues");
            }
        }

        /// <summary>Todas las revisiones sobre una zona ya instanciada. Sin fotos ni archivos.</summary>
        public static IReadOnlyList<Hallazgo> BarrerZona(GameObject raiz)
        {
            Ctx c = Armar(raiz);
            RevisarApoyo(c);
            RevisarEncimados(c);
            RevisarParpadeo(c);
            RevisarColliders(c);
            RevisarMuros(c);
            RevisarRampas(c);
            RevisarTexturas(c);
            RevisarCalles(c);
            RevisarEscaleras(c);
            RevisarVacioYAtrapado(c);
            RevisarCarros(c);
            RevisarPeatones(c);
            if (c.EsPuerto)
            {
                RevisarPasillosPuerto(c);
                RevisarContenedores(c);
            }
            if (c.EsCerro)
            {
                RevisarParkourCerro(c);
            }
            return Agrupar(c.Salida);
        }

        /// <summary>Se puede saltar de una azotea a otra: hueco horizontal ≤ 2 m y sube ≤ 2.3 m (con agarre) o baja ≤ 4 m.</summary>
        public static bool Saltable(float hueco, float desnivel)
        {
            return hueco <= HuecoSaltoMaximo && desnivel <= AgarreMaximo && desnivel >= -BajadaSaltoMaxima;
        }

        /// <summary>Puntos cada <paramref name="paso"/> m a lo largo de la línea, desplazados <paramref name="lateral"/> m a la derecha de la marcha.</summary>
        public static List<Vector3> Muestrear(IList<Vector3> puntos, float paso, float lateral)
        {
            List<Vector3> res = new List<Vector3>();
            float s = 0f;
            for (int i = 1; i < puntos.Count; i++)
            {
                Vector3 a = puntos[i - 1];
                Vector3 b = puntos[i];
                Vector3 h = new Vector3(b.x - a.x, 0f, b.z - a.z);
                float largo = h.magnitude;
                if (largo < 1e-4f)
                {
                    continue;
                }
                h /= largo;
                Vector3 perp = new Vector3(h.z, 0f, -h.x);
                while (s <= largo)
                {
                    res.Add(Vector3.Lerp(a, b, s / largo) + perp * lateral);
                    s += paso;
                }
                s -= largo;
            }
            return res;
        }

        // ------------------------------------------------------------------ armado del contexto

        private static string NombreZona(string nombre)
        {
            string n = nombre.Replace("(Clone)", "").Trim();
            if (n.Length > 2 && n[0] == 'T' && char.IsDigit(n[1]))
            {
                int guion = n.IndexOf('_');
                if (guion > 0 && guion < n.Length - 1)
                {
                    n = n.Substring(guion + 1);
                }
            }
            return n;
        }

        private static bool EsContenedor(string nombre)
        {
            return nombre.StartsWith("Bloque", StringComparison.Ordinal) || Array.IndexOf(Contenedores, nombre) >= 0;
        }

        private static bool NombreIgnorado(string n)
        {
            return n.StartsWith("Linea", StringComparison.Ordinal) || n.StartsWith("Charco", StringComparison.Ordinal)
                || n.StartsWith("VentanaCalida", StringComparison.Ordinal) || n.StartsWith("Halo", StringComparison.Ordinal)
                || n.StartsWith("Cable", StringComparison.Ordinal) || n.StartsWith("Huecos", StringComparison.Ordinal)
                || n.StartsWith("Hueco_", StringComparison.Ordinal) || n.StartsWith("Luz", StringComparison.Ordinal)
                || n.StartsWith("Neon", StringComparison.Ordinal) || n.StartsWith("Reflector", StringComparison.Ordinal)
                || n.StartsWith("FocoPared", StringComparison.Ordinal);
        }

        private static bool Ign(Ctx c, Transform t)
        {
            int id = t.GetInstanceID();
            if (c.Ign.TryGetValue(id, out bool r))
            {
                return r;
            }
            r = false;
            for (Transform u = t; u != null && u != c.Raiz; u = u.parent)
            {
                if (NombreIgnorado(u.name))
                {
                    r = true;
                    break;
                }
            }
            c.Ign[id] = r;
            return r;
        }

        private static bool EsCarro(Ctx c, Transform t)
        {
            int id = t.GetInstanceID();
            if (c.Carro.TryGetValue(id, out bool r))
            {
                return r;
            }
            r = false;
            for (Transform u = t; u != null && u != c.Raiz; u = u.parent)
            {
                if (u.name == "Carros" || u.name.StartsWith("CarroViejo", StringComparison.Ordinal))
                {
                    r = true;
                    break;
                }
            }
            c.Carro[id] = r;
            return r;
        }

        /// <summary>La raíz de prop de un objeto: el hijo directo de un contenedor (Casas, Postes, Bloque...). Null si no está en uno.</summary>
        private static Transform PropRaiz(Transform hoja, Transform raiz)
        {
            for (Transform t = hoja; t != null && t != raiz; t = t.parent)
            {
                Transform p = t.parent;
                if (p != null && p != raiz && EsContenedor(p.name))
                {
                    return t;
                }
            }
            return null;
        }

        private static Prop ObtenerProp(Ctx c, Transform hoja)
        {
            Transform pr = PropRaiz(hoja, c.Raiz);
            if (pr == null)
            {
                return null;
            }
            if (!c.PorRaiz.TryGetValue(pr, out Prop p))
            {
                p = new Prop { T = pr, Nombre = pr.name };
                c.PorRaiz[pr] = p;
                c.Props.Add(p);
            }
            return p;
        }

        private static Ctx Armar(GameObject raizGo)
        {
            Physics.SyncTransforms();
            Ctx c = new Ctx { Raiz = raizGo.transform, Zona = NombreZona(raizGo.name) };
            foreach (Transform t in c.Raiz.GetComponentsInChildren<Transform>(false))
            {
                if (t.name == "Mar" && c.Mar == null)
                {
                    c.Mar = t;
                }
            }
            foreach (Renderer r in c.Raiz.GetComponentsInChildren<Renderer>(false))
            {
                if (!r.enabled || r is ParticleSystemRenderer || r is TrailRenderer || r is LineRenderer || Ign(c, r.transform))
                {
                    continue;
                }
                int idx = c.Rs.Count;
                c.Rs.Add(r);
                c.RsB.Add(r.bounds);
                if (!c.TieneLimites)
                {
                    c.Limites = r.bounds;
                    c.TieneLimites = true;
                }
                else
                {
                    c.Limites.Encapsulate(r.bounds);
                }
                Prop p = ObtenerProp(c, r.transform);
                if (p != null)
                {
                    p.Rs.Add(r);
                }
                Indexar(c, idx, r.bounds);
            }
            foreach (Collider col in c.Raiz.GetComponentsInChildren<Collider>(false))
            {
                if (!col.enabled || col.isTrigger || Ign(c, col.transform) || col.GetComponent<PuntoTrepar>() != null)
                {
                    continue;
                }
                c.Colliders.Add(col);
                Prop p = ObtenerProp(c, col.transform);
                if (p != null)
                {
                    p.Cs.Add(col);
                    p.Propios.Add(col);
                    c.PropDe[col] = p;
                }
            }
            foreach (Prop p in c.Props)
            {
                foreach (Renderer r in p.Rs)
                {
                    if (!p.TieneRs)
                    {
                        p.RB = r.bounds;
                        p.TieneRs = true;
                    }
                    else
                    {
                        p.RB.Encapsulate(r.bounds);
                    }
                }
                foreach (Collider col in p.Cs)
                {
                    if (!p.TieneCs)
                    {
                        p.CB = col.bounds;
                        p.TieneCs = true;
                    }
                    else
                    {
                        p.CB.Encapsulate(col.bounds);
                    }
                }
                p.TieneB = p.TieneRs || p.TieneCs;
                p.B = p.TieneRs ? p.RB : p.CB;
            }
            return c;
        }

        private static long Clave(int cx, int cz)
        {
            return ((long)cx << 32) ^ (uint)cz;
        }

        private static void Indexar(Ctx c, int idx, Bounds b)
        {
            int x0 = Mathf.FloorToInt(b.min.x / CeldaIndice);
            int x1 = Mathf.FloorToInt(b.max.x / CeldaIndice);
            int z0 = Mathf.FloorToInt(b.min.z / CeldaIndice);
            int z1 = Mathf.FloorToInt(b.max.z / CeldaIndice);
            for (int x = x0; x <= x1; x++)
            {
                for (int z = z0; z <= z1; z++)
                {
                    long k = Clave(x, z);
                    if (!c.Grid.TryGetValue(k, out List<int> l))
                    {
                        l = new List<int>();
                        c.Grid[k] = l;
                    }
                    l.Add(idx);
                }
            }
        }

        /// <summary>Índices de renderers cuyas celdas tocan <paramref name="b"/> (puede traer de más: filtra con Intersects).</summary>
        private static List<int> Candidatos(Ctx c, Bounds b, List<int> salida)
        {
            salida.Clear();
            c.TmpSet.Clear();
            int x0 = Mathf.FloorToInt(b.min.x / CeldaIndice);
            int x1 = Mathf.FloorToInt(b.max.x / CeldaIndice);
            int z0 = Mathf.FloorToInt(b.min.z / CeldaIndice);
            int z1 = Mathf.FloorToInt(b.max.z / CeldaIndice);
            for (int x = x0; x <= x1; x++)
            {
                for (int z = z0; z <= z1; z++)
                {
                    if (c.Grid.TryGetValue(Clave(x, z), out List<int> l))
                    {
                        foreach (int i in l)
                        {
                            if (c.TmpSet.Add(i))
                            {
                                salida.Add(i);
                            }
                        }
                    }
                }
            }
            return salida;
        }

        // ------------------------------------------------------------------ utilidades

        private static string Ruta(Transform t, Transform raiz)
        {
            if (t == null)
            {
                return "";
            }
            StringBuilder sb = new StringBuilder(t.name);
            for (Transform u = t.parent; u != null && u != raiz; u = u.parent)
            {
                sb.Insert(0, u.name + "/");
            }
            return sb.ToString();
        }

        private static void Add(Ctx c, string tipo, string ruta, Vector3 pos, string detalle, float valor)
        {
            if (float.IsNaN(valor) || float.IsInfinity(valor))
            {
                valor = 999f;
            }
            c.Salida.Add(new Hallazgo { zona = c.Zona, tipo = tipo, ruta = ruta, posicion = pos, detalle = detalle, valor = valor });
        }

        private static string F(float v)
        {
            return v.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
        }

        /// <summary>Primer piso (collider no trigger) bajo <paramref name="origen"/> que no sea propio ni ignorado.</summary>
        private static bool Piso(Ctx c, Vector3 origen, float max, ICollection<Collider> propios, bool sinCarros, out RaycastHit mejor)
        {
            mejor = default;
            int n = Physics.RaycastNonAlloc(origen, Vector3.down, bufHits, max, ~0, QueryTriggerInteraction.Ignore);
            float d = float.MaxValue;
            bool ok = false;
            for (int i = 0; i < n; i++)
            {
                RaycastHit h = bufHits[i];
                if (h.distance >= d)
                {
                    continue;
                }
                Collider col = h.collider;
                if ((propios != null && propios.Contains(col)) || Ign(c, col.transform) || (sinCarros && EsCarro(c, col.transform)))
                {
                    continue;
                }
                d = h.distance;
                mejor = h;
                ok = true;
            }
            return ok;
        }

        /// <summary>Primer golpe horizontal; devuelve false si no hay nada a <paramref name="max"/> m.</summary>
        private static bool Golpe(Ctx c, Vector3 origen, Vector3 dir, float max, bool sinCarros, out float dist)
        {
            dist = max;
            int n = Physics.RaycastNonAlloc(origen, dir, bufHits, max, ~0, QueryTriggerInteraction.Ignore);
            bool ok = false;
            for (int i = 0; i < n; i++)
            {
                RaycastHit h = bufHits[i];
                if (h.distance >= dist || Ign(c, h.collider.transform) || (sinCarros && EsCarro(c, h.collider.transform)))
                {
                    continue;
                }
                dist = h.distance;
                ok = true;
            }
            return ok;
        }

        private static bool HayAlgoEn(Ctx c, Vector3 punto, ICollection<Collider> propios)
        {
            int n = Physics.OverlapSphereNonAlloc(punto, 0.03f, bufEsfera, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                if (!propios.Contains(bufEsfera[i]) && !Ign(c, bufEsfera[i].transform))
                {
                    return true;
                }
            }
            return false;
        }

        private static Vector3[] Cinco(Bounds b, float f)
        {
            Vector3 m = b.center;
            Vector3 e = b.extents * f;
            return new[]
            {
                new Vector3(m.x, 0f, m.z), new Vector3(m.x + e.x, 0f, m.z + e.z), new Vector3(m.x - e.x, 0f, m.z + e.z),
                new Vector3(m.x + e.x, 0f, m.z - e.z), new Vector3(m.x - e.x, 0f, m.z - e.z),
            };
        }

        private static bool EsRampa(Collider col)
        {
            return col.transform.name.StartsWith("Rampa", StringComparison.Ordinal);
        }

        private static bool EsCasa(Prop p)
        {
            return p.Nombre.StartsWith("Casa_", StringComparison.Ordinal) || p.Nombre.StartsWith("CC0", StringComparison.Ordinal);
        }

        private static float GapXZ(Bounds a, Bounds b)
        {
            float gx = Mathf.Max(0f, Mathf.Max(a.min.x - b.max.x, b.min.x - a.max.x));
            float gz = Mathf.Max(0f, Mathf.Max(a.min.z - b.max.z, b.min.z - a.max.z));
            return Mathf.Sqrt(gx * gx + gz * gz);
        }

        // ------------------------------------------------------------------ 1 y 2: flota y enterrado

        private static void RevisarApoyo(Ctx c)
        {
            foreach (Prop p in c.Props)
            {
                if (!p.TieneB)
                {
                    continue;
                }
                Bounds b = p.B;
                if (p.Nombre.StartsWith("PU02_Barco", StringComparison.Ordinal) && c.Mar != null)
                {
                    RevisarBarco(c, p);
                    continue;
                }
                float f = p.Nombre.StartsWith("PU01_Grua", StringComparison.Ordinal) ? 0.9f : 0.6f;
                Vector3[] pts = Cinco(b, f);
                Vector3 baseCentro = new Vector3(b.center.x, b.min.y, b.center.z);
                if (!p.EsCimiento)
                {
                    int n = 0;
                    float suma = 0f;
                    foreach (Vector3 pt in pts)
                    {
                        Vector3 origen = new Vector3(pt.x, b.max.y + 0.05f, pt.z);
                        if (Piso(c, origen, 600f, p.Propios, false, out RaycastHit h) && h.point.y > b.min.y + TolerEnterrado)
                        {
                            n++;
                            suma += h.point.y - b.min.y;
                        }
                    }
                    if (n >= 3)
                    {
                        Add(c, TipoEnterrado, Ruta(p.T, c.Raiz), baseCentro, "enterrado (" + F(suma / n) + " m)", suma / n);
                        continue;
                    }
                }
                int cuantos = 0;
                float total = 0f;
                foreach (Vector3 pt in pts)
                {
                    Vector3 origen = new Vector3(pt.x, b.min.y + 0.05f, pt.z);
                    float d;
                    if (HayAlgoEn(c, origen, p.Propios))
                    {
                        d = 0f;
                    }
                    else if (Piso(c, origen, 600f, p.Propios, false, out RaycastHit h))
                    {
                        d = Mathf.Max(0f, h.distance - 0.05f);
                    }
                    else
                    {
                        d = 999f;
                    }
                    if (d > TolerFlota)
                    {
                        cuantos++;
                        total += d;
                    }
                }
                if (cuantos >= 3)
                {
                    float delta = total / cuantos;
                    Add(c, TipoFlota, Ruta(p.T, c.Raiz), baseCentro, "flota (" + F(delta) + " m)", delta);
                }
            }
        }

        private static void RevisarBarco(Ctx c, Prop p)
        {
            Renderer mar = c.Mar.GetComponentInChildren<Renderer>();
            float agua = mar != null ? mar.bounds.max.y : c.Mar.position.y;
            float fondo = p.B.min.y;
            Vector3 pos = new Vector3(p.B.center.x, fondo, p.B.center.z);
            if (fondo > agua + 0.2f)
            {
                Add(c, TipoFlota, Ruta(p.T, c.Raiz), pos, "barco sobre el agua (" + F(fondo - agua) + " m)", fondo - agua);
            }
            else if (fondo < agua - AguaBajoMax)
            {
                Add(c, TipoEnterrado, Ruta(p.T, c.Raiz), pos, "barco bajo el agua (" + F(agua - fondo) + " m)", agua - fondo);
            }
        }

        // ------------------------------------------------------------------ 3: encimados

        private static bool Soportado(Collider col)
        {
            if (col is BoxCollider || col is SphereCollider || col is CapsuleCollider)
            {
                return true;
            }
            MeshCollider mc = col as MeshCollider;
            return mc != null && mc.convex;
        }

        private static void RevisarEncimados(Ctx c)
        {
            HashSet<long> vistos = new HashSet<long>();
            foreach (Collider a in c.Colliders)
            {
                if (!c.PropDe.TryGetValue(a, out Prop pa) || !Soportado(a) || EsRampa(a))
                {
                    continue;
                }
                Bounds ab = a.bounds;
                int n = Physics.OverlapBoxNonAlloc(ab.center, ab.extents, bufCols, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore);
                for (int i = 0; i < n; i++)
                {
                    Collider b = bufCols[i];
                    if (b == a || !c.PropDe.TryGetValue(b, out Prop pb) || pb == pa || !Soportado(b) || EsRampa(b) || pa.EsCimiento || pb.EsCimiento)
                    {
                        continue;
                    }
                    int ia = a.GetInstanceID();
                    int ib = b.GetInstanceID();
                    long k = ia < ib ? ((long)ia << 32) ^ (uint)ib : ((long)ib << 32) ^ (uint)ia;
                    if (!vistos.Add(k))
                    {
                        continue;
                    }
                    if (!Physics.ComputePenetration(a, a.transform.position, a.transform.rotation, b, b.transform.position,
                            b.transform.rotation, out Vector3 dir, out float dist) || dist <= UmbralEncimado)
                    {
                        continue;
                    }
                    if (EsCasa(pa) && EsCasa(pb) && dist < UmbralEncimadoCasas)
                    {
                        continue;
                    }
                    Add(c, TipoEncimado, Ruta(a.transform, c.Raiz), ab.center,
                        "penetra " + F(dist) + " m en " + Ruta(b.transform, c.Raiz), dist);
                }
            }
        }

        // ------------------------------------------------------------------ 4: parpadeo

        private sealed class RInfo
        {
            public Bounds B;
            public Transform Raiz;
            public bool Alineado;
            public int Plano = -1;
            public bool Caja;
        }

        private static bool Alineado(Transform t)
        {
            Vector3[] ejes = { t.right, t.up, t.forward };
            foreach (Vector3 e in ejes)
            {
                if (Mathf.Max(Mathf.Abs(e.x), Mathf.Max(Mathf.Abs(e.y), Mathf.Abs(e.z))) < 0.999f)
                {
                    return false;
                }
            }
            return true;
        }

        private static Transform TopHijo(Transform raiz, Transform t)
        {
            while (t.parent != null && t.parent != raiz)
            {
                t = t.parent;
            }
            return t;
        }

        private static float Solape(Bounds a, Bounds b, int eje)
        {
            return Mathf.Min(a.max[eje], b.max[eje]) - Mathf.Max(a.min[eje], b.min[eje]);
        }

        private static void RevisarParpadeo(Ctx c)
        {
            RInfo[] infos = new RInfo[c.Rs.Count];
            for (int i = 0; i < infos.Length; i++)
            {
                Renderer r = c.Rs[i];
                Bounds b = c.RsB[i];
                Prop p = ObtenerPropSinCrear(c, r.transform);
                RInfo info = new RInfo { B = b, Raiz = p != null ? p.T : TopHijo(c.Raiz, r.transform), Alineado = Alineado(r.transform) };
                int minEje = 0;
                for (int e = 1; e < 3; e++)
                {
                    if (b.size[e] < b.size[minEje])
                    {
                        minEje = e;
                    }
                }
                if (b.size[minEje] < GrosorPlano)
                {
                    info.Plano = minEje;
                }
                else
                {
                    MeshFilter mf = r.GetComponent<MeshFilter>();
                    info.Caja = mf != null && mf.sharedMesh != null && mf.sharedMesh.vertexCount <= 24;
                }
                infos[i] = info;
            }
            List<int> cand = new List<int>();
            for (int i = 0; i < infos.Length; i++)
            {
                RInfo a = infos[i];
                if (!a.Alineado)
                {
                    continue;
                }
                Bounds q = a.B;
                q.Expand(0.02f);
                foreach (int j in Candidatos(c, q, cand).ToArray())
                {
                    if (j <= i)
                    {
                        continue;
                    }
                    RInfo b = infos[j];
                    if (!b.Alineado || !a.B.Intersects(q) || !b.B.Intersects(q))
                    {
                        continue;
                    }
                    if (a.Plano >= 0 && a.Plano == b.Plano)
                    {
                        int e0 = (a.Plano + 1) % 3;
                        int e1 = (a.Plano + 2) % 3;
                        float area = Mathf.Max(0f, Solape(a.B, b.B, e0)) * Mathf.Max(0f, Solape(a.B, b.B, e1));
                        if (Mathf.Abs(a.B.center[a.Plano] - b.B.center[a.Plano]) < CoplanarTol && area > AreaPlanos)
                        {
                            AddParpadeo(c, i, j, area, "dos planos coplanares, solape " + F(area) + " m2");
                        }
                    }
                    else if (a.Caja && b.Caja && a.Raiz != b.Raiz)
                    {
                        for (int eje = 0; eje < 3; eje++)
                        {
                            int e0 = (eje + 1) % 3;
                            int e1 = (eje + 2) % 3;
                            float area = Mathf.Max(0f, Solape(a.B, b.B, e0)) * Mathf.Max(0f, Solape(a.B, b.B, e1));
                            if (area <= AreaCajas)
                            {
                                continue;
                            }
                            bool mismoMax = Mathf.Abs(a.B.max[eje] - b.B.max[eje]) < CoplanarTol;
                            bool mismoMin = eje != 1 && Mathf.Abs(a.B.min[eje] - b.B.min[eje]) < CoplanarTol;
                            if (mismoMax || mismoMin)
                            {
                                AddParpadeo(c, i, j, area, "caras de cajas en el mismo plano (eje " + "xyz"[eje] + "), solape " + F(area) + " m2");
                                break;
                            }
                        }
                    }
                }
            }
        }

        private static Prop ObtenerPropSinCrear(Ctx c, Transform t)
        {
            Transform pr = PropRaiz(t, c.Raiz);
            return pr != null && c.PorRaiz.TryGetValue(pr, out Prop p) ? p : null;
        }

        private static void AddParpadeo(Ctx c, int i, int j, float area, string detalle)
        {
            Bounds a = c.RsB[i];
            Bounds b = c.RsB[j];
            Vector3 centro = (Vector3.Max(a.min, b.min) + Vector3.Min(a.max, b.max)) * 0.5f;
            Add(c, TipoParpadea, Ruta(c.Rs[i].transform, c.Raiz), centro, detalle + " con " + Ruta(c.Rs[j].transform, c.Raiz), area);
        }

        // ------------------------------------------------------------------ 5: sin collider y collider que no corresponde

        private static void RevisarColliders(Ctx c)
        {
            foreach (Prop p in c.Props)
            {
                if (p.TieneRs && p.Cs.Count == 0 && p.T.GetComponentInChildren<PuntoTrepar>(true) == null)
                {
                    float mayor = 0f;
                    foreach (Renderer r in p.Rs)
                    {
                        Vector3 s = r.bounds.size;
                        if (s.y > AltoSolido && Mathf.Max(s.x, Mathf.Max(s.y, s.z)) > TamSolido)
                        {
                            mayor = Mathf.Max(mayor, Mathf.Max(s.x, Mathf.Max(s.y, s.z)));
                        }
                    }
                    if (mayor > 0f)
                    {
                        Add(c, TipoSinCollider, Ruta(p.T, c.Raiz), p.RB.center, "sin collider (" + F(mayor) + " m)", mayor);
                    }
                    continue;
                }
                if (!p.TieneRs || p.Cs.Count == 0)
                {
                    continue;
                }
                bool soloCajas = true;
                bool hay = false;
                Bounds cb = default;
                foreach (Collider col in p.Cs)
                {
                    if (EsRampa(col))
                    {
                        continue;
                    }
                    if (!(col is BoxCollider))
                    {
                        soloCajas = false;
                        break;
                    }
                    if (!hay)
                    {
                        cb = col.bounds;
                        hay = true;
                    }
                    else
                    {
                        cb.Encapsulate(col.bounds);
                    }
                }
                float volR = p.RB.size.x * p.RB.size.y * p.RB.size.z;
                if (!soloCajas || !hay || volR < 0.001f)
                {
                    continue;
                }
                float exceso = Mathf.Max(
                    Mathf.Max(p.RB.min.x - cb.min.x, cb.max.x - p.RB.max.x),
                    Mathf.Max(Mathf.Max(p.RB.min.y - cb.min.y, cb.max.y - p.RB.max.y),
                        Mathf.Max(p.RB.min.z - cb.min.z, cb.max.z - p.RB.max.z)));
                float ratio = cb.size.x * cb.size.y * cb.size.z / volR;
                if (exceso > MargenCollider)
                {
                    Add(c, TipoColliderNoCorresponde, Ruta(p.T, c.Raiz), cb.center, "el collider sale " + F(exceso) + " m del modelo", exceso);
                }
                else if (ratio < RatioVolumenMin)
                {
                    Add(c, TipoColliderNoCorresponde, Ruta(p.T, c.Raiz), cb.center,
                        "el collider cubre " + F(ratio * 100f) + " % del modelo", 1f - ratio);
                }
            }
        }

        // ------------------------------------------------------------------ 6: muros invisibles y rampas

        private static void RevisarMuros(Ctx c)
        {
            List<int> cand = new List<int>();
            foreach (Collider col in c.Colliders)
            {
                if (EsRampa(col))
                {
                    continue;
                }
                c.PropDe.TryGetValue(col, out Prop p);
                Bounds cb = col.bounds;
                if (p != null && !p.TieneRs)
                {
                    Add(c, TipoMuroInvisible, Ruta(col.transform, c.Raiz), cb.center, "collider sin renderer en su prop", cb.size.magnitude);
                    continue;
                }
                bool alguno = false;
                Bounds u = default;
                foreach (int j in Candidatos(c, cb, cand))
                {
                    if (!c.RsB[j].Intersects(cb))
                    {
                        continue;
                    }
                    if (!alguno)
                    {
                        u = c.RsB[j];
                        alguno = true;
                    }
                    else
                    {
                        u.Encapsulate(c.RsB[j]);
                    }
                }
                if (!alguno)
                {
                    Add(c, TipoMuroInvisible, Ruta(col.transform, c.Raiz), cb.center, "ningún renderer toca el collider", cb.size.magnitude);
                    continue;
                }
                float ex = Mathf.Max(
                    Mathf.Max(u.min.x - cb.min.x, cb.max.x - u.max.x),
                    Mathf.Max(Mathf.Max(u.min.y - cb.min.y, cb.max.y - u.max.y), Mathf.Max(u.min.z - cb.min.z, cb.max.z - u.max.z)));
                if (ex > MargenCollider)
                {
                    Add(c, TipoMuroInvisible, Ruta(col.transform, c.Raiz), cb.center, "el collider sale " + F(ex) + " m de lo visible", ex);
                }
            }
        }

        private static void RevisarRampas(Ctx c)
        {
            foreach (Collider col in c.Colliders)
            {
                if (!EsRampa(col))
                {
                    continue;
                }
                Vector3 tam = col is BoxCollider bc ? Vector3.Scale(bc.size, col.transform.lossyScale) : col.bounds.size;
                string ruta = Ruta(col.transform, c.Raiz);
                if (Mathf.Min(Mathf.Abs(tam.x), Mathf.Min(Mathf.Abs(tam.y), Mathf.Abs(tam.z))) < 0.01f)
                {
                    Add(c, TipoRampaRota, ruta, col.bounds.center, "rampa de tamaño 0", 999f);
                    continue;
                }
                if (!c.PropDe.TryGetValue(col, out Prop p) || !p.TieneRs)
                {
                    continue;
                }
                Bounds r = col.bounds;
                float bajo = r.min.y - p.RB.min.y;
                float alto = p.RB.max.y - r.max.y;
                float peor = Mathf.Max(Mathf.Max(bajo, alto), Mathf.Max(-bajo, -alto));
                if (peor > MargenCollider)
                {
                    Add(c, TipoRampaRota, ruta, r.center,
                        "rampa y=" + F(r.min.y) + ".." + F(r.max.y) + " vs escalera y=" + F(p.RB.min.y) + ".." + F(p.RB.max.y), peor);
                }
            }
        }

        // ------------------------------------------------------------------ 7: texturas

        private static string ProblemaMaterial(Material m)
        {
            if (m == null)
            {
                return "material nulo";
            }
            Shader s = m.shader;
            if (s == null || s.name.Contains("InternalErrorShader"))
            {
                return "shader de error (" + m.name + ")";
            }
            if (s.name == "Universal Render Pipeline/Lit")
            {
                bool tex = (m.HasProperty("_BaseMap") && m.GetTexture("_BaseMap") != null)
                    || (m.HasProperty("_MainTex") && m.GetTexture("_MainTex") != null);
                Color col = m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor") : Color.white;
                bool blanco = col.r > 0.98f && col.g > 0.98f && col.b > 0.98f;
                if (!tex && blanco && !m.IsKeywordEnabled("_EMISSION"))
                {
                    return "URP Lit blanco sin textura (" + m.name + ")";
                }
            }
            return null;
        }

        private static void RevisarTexturas(Ctx c)
        {
            for (int i = 0; i < c.Rs.Count; i++)
            {
                Renderer r = c.Rs[i];
                Material[] ms = r.sharedMaterials;
                if (ms.Length == 0)
                {
                    continue;
                }
                foreach (Material m in ms)
                {
                    string problema = ProblemaMaterial(m);
                    if (problema != null)
                    {
                        Add(c, TipoTexturaRota, Ruta(r.transform, c.Raiz), c.RsB[i].center, problema, 1f);
                        break;
                    }
                }
            }
        }

        // ------------------------------------------------------------------ 8: calles y escaleras

        private static void RevisarCalles(Ctx c)
        {
            foreach (CalleZona cz in c.Raiz.GetComponentsInChildren<CalleZona>(false))
            {
                if (cz.Cantidad < 2)
                {
                    continue;
                }
                List<Vector3> eje = cz.Puntos();
                float o = Mathf.Max(0f, cz.Ancho * 0.5f - 1f);
                float[] laterales = o > 0.01f ? new[] { 0f, o, -o } : new[] { 0f };
                string ruta = Ruta(cz.transform, c.Raiz);
                foreach (float lat in laterales)
                {
                    List<Vector3> pts = Muestrear(eje, PasoCalle, lat);
                    float[] ys = new float[pts.Count];
                    for (int i = 0; i < pts.Count; i++)
                    {
                        Vector3 s = pts[i];
                        if (!Piso(c, s + Vector3.up * 3f, 20f, null, true, out RaycastHit h))
                        {
                            ys[i] = float.NaN;
                            Add(c, TipoHueco, ruta, s, "sin piso (lateral " + F(lat) + " m)", 1f);
                            continue;
                        }
                        ys[i] = h.point.y;
                        if (i > 0 && !float.IsNaN(ys[i - 1]) && XZ(pts[i - 1], s) < 0.5f)
                        {
                            float d = Mathf.Abs(ys[i] - ys[i - 1]);
                            if (d > EscalonCalle)
                            {
                                Add(c, TipoEscalon, ruta, h.point, "escalón (" + F(d) + " m, lateral " + F(lat) + " m)", d);
                            }
                        }
                        const int ventana = 8;
                        if (lat == 0f && i >= ventana && !float.IsNaN(ys[i - ventana]))
                        {
                            float dist = XZ(pts[i - ventana], s);
                            if (dist > 0.5f)
                            {
                                float pendiente = Mathf.Abs(ys[i] - ys[i - ventana]) / dist;
                                if (pendiente > PendienteCarro)
                                {
                                    Add(c, TipoPendiente, ruta, h.point, "pendiente " + F(pendiente * 100f) + " %", pendiente * 100f);
                                }
                            }
                        }
                    }
                }
            }
        }

        private static float XZ(Vector3 a, Vector3 b)
        {
            return new Vector2(a.x - b.x, a.z - b.z).magnitude;
        }

        private static void RevisarEscaleras(Ctx c)
        {
            foreach (Prop p in c.Props)
            {
                if (!p.TieneB || !p.Nombre.StartsWith("CE01", StringComparison.Ordinal) || p.Cs.Any(EsRampa))
                {
                    continue;
                }
                Bounds b = p.B;
                bool largoX = b.size.x >= b.size.z;
                float largo = largoX ? b.size.x : b.size.z;
                int pasos = Mathf.CeilToInt(largo / PasoCalle);
                float prev = float.NaN;
                for (int i = 0; i <= pasos; i++)
                {
                    float t = Mathf.Min(i * PasoCalle, largo);
                    float y = float.NaN;
                    for (int k = -1; k <= 1; k++)
                    {
                        float x = largoX ? b.min.x + t : b.center.x + k * RadioJugador;
                        float z = largoX ? b.center.z + k * RadioJugador : b.min.z + t;
                        if (Piso(c, new Vector3(x, b.max.y + 1f, z), b.size.y + 3f, null, false, out RaycastHit h))
                        {
                            y = float.IsNaN(y) ? h.point.y : Mathf.Max(y, h.point.y);
                        }
                    }
                    if (!float.IsNaN(prev) && !float.IsNaN(y) && Mathf.Abs(y - prev) > StepOffset)
                    {
                        Vector3 pos = largoX ? new Vector3(b.min.x + t, y, b.center.z) : new Vector3(b.center.x, y, b.min.z + t);
                        Add(c, TipoEscaleraSalto, Ruta(p.T, c.Raiz), pos, "salto de " + F(Mathf.Abs(y - prev)) + " m (stepOffset " + F(StepOffset) + ")",
                            Mathf.Abs(y - prev));
                    }
                    prev = y;
                }
            }
        }

        // ------------------------------------------------------------------ 9: vacío y atrapado

        private static void RevisarVacioYAtrapado(Ctx c)
        {
            if (!c.TieneLimites)
            {
                return;
            }
            Bounds zb = c.Limites;
            float paso = Mathf.Max(PasoVacioMinimo, Mathf.Sqrt(zb.size.x * zb.size.z / MaxRayosVacio));
            int nx = Mathf.Max(1, Mathf.FloorToInt(zb.size.x / paso));
            int nz = Mathf.Max(1, Mathf.FloorToInt(zb.size.z / paso));
            bool[,] hay = new bool[nx, nz];
            float[,] ys = new float[nx, nz];
            float[,] ny = new float[nx, nz];
            Renderer mar = c.Mar != null ? c.Mar.GetComponentInChildren<Renderer>() : null;
            float alto = zb.max.y + AltoRayoVacio;
            float largoRayo = zb.size.y + AltoRayoVacio + 500f;
            for (int i = 0; i < nx; i++)
            {
                for (int j = 0; j < nz; j++)
                {
                    float x = zb.min.x + (i + 0.5f) * paso;
                    float z = zb.min.z + (j + 0.5f) * paso;
                    if (Piso(c, new Vector3(x, alto, z), largoRayo, null, false, out RaycastHit h))
                    {
                        hay[i, j] = true;
                        ys[i, j] = h.point.y;
                        ny[i, j] = h.normal.y;
                    }
                }
            }
            for (int i = 0; i < nx; i++)
            {
                for (int j = 0; j < nz; j++)
                {
                    float x = zb.min.x + (i + 0.5f) * paso;
                    float z = zb.min.z + (j + 0.5f) * paso;
                    if (!hay[i, j])
                    {
                        int vecinos = 0;
                        float suma = 0f;
                        for (int di = -1; di <= 1; di++)
                        {
                            for (int dj = -1; dj <= 1; dj++)
                            {
                                int a = i + di;
                                int b = j + dj;
                                if ((di != 0 || dj != 0) && a >= 0 && b >= 0 && a < nx && b < nz && hay[a, b])
                                {
                                    vecinos++;
                                    suma += ys[a, b];
                                }
                            }
                        }
                        bool enMar = mar != null && mar.bounds.Contains(new Vector3(x, mar.bounds.center.y, z));
                        if (vecinos >= 6 && !enMar)
                        {
                            Add(c, TipoVacio, c.Zona, new Vector3(x, suma / vecinos, z), "sin nada bajo (" + vecinos + " de 8 vecinas sí tienen piso)", vecinos);
                        }
                        continue;
                    }
                    if (ny[i, j] > 0.7f)
                    {
                        RevisarAtrapado(c, new Vector3(x, ys[i, j], z));
                    }
                }
            }
        }

        private static void RevisarAtrapado(Ctx c, Vector3 suelo)
        {
            Vector3 pies = suelo + Vector3.up * 0.05f;
            if (Cabe(pies))
            {
                bool salida = false;
                for (int k = 0; k < 8 && !salida; k++)
                {
                    float ang = k * 45f * Mathf.Deg2Rad;
                    Vector3 p = pies + new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang)) * 1.5f;
                    Vector3 origen = new Vector3(p.x, pies.y + AgarreMaximo, p.z);
                    if (Physics.CheckSphere(origen, 0.05f, ~0, QueryTriggerInteraction.Ignore))
                    {
                        continue;
                    }
                    if (!Piso(c, origen, AgarreMaximo + CaidaMaxima + 0.1f, null, false, out RaycastHit h))
                    {
                        continue;
                    }
                    float dy = h.point.y - pies.y;
                    if (dy > StepOffset || (dy >= -CaidaMaxima && Cabe(h.point + Vector3.up * 0.05f)))
                    {
                        salida = true;
                    }
                }
                if (!salida)
                {
                    Add(c, TipoAtrapado, c.Zona, suelo, "cabe parado y ninguna de las 8 direcciones a 1.5 m tiene salida", 1f);
                }
            }
        }

        private static bool Cabe(Vector3 pies)
        {
            return !Physics.CheckCapsule(pies + Vector3.up * RadioJugador, pies + Vector3.up * (AltoJugador - RadioJugador), RadioJugador, ~0,
                QueryTriggerInteraction.Ignore);
        }

        // ------------------------------------------------------------------ 10: pasillos (Puerto)

        private static void RevisarPasillosPuerto(Ctx c)
        {
            Dictionary<Transform, Bounds> bloques = new Dictionary<Transform, Bounds>();
            foreach (Prop p in c.Props)
            {
                if (!p.TieneB || p.T.parent == null || !p.T.parent.name.StartsWith("Bloque", StringComparison.Ordinal))
                {
                    continue;
                }
                if (bloques.TryGetValue(p.T.parent, out Bounds b))
                {
                    b.Encapsulate(p.B);
                    bloques[p.T.parent] = b;
                }
                else
                {
                    bloques[p.T.parent] = p.B;
                }
            }
            List<KeyValuePair<Transform, Bounds>> lista = bloques.ToList();
            const float gapMax = 6f;
            for (int i = 0; i < lista.Count; i++)
            {
                for (int j = i + 1; j < lista.Count; j++)
                {
                    Bounds a = lista[i].Value;
                    Bounds b = lista[j].Value;
                    for (int eje = 0; eje < 3; eje += 2)
                    {
                        int otro = 2 - eje;
                        float gap = Mathf.Max(b.min[eje] - a.max[eje], a.min[eje] - b.max[eje]);
                        float solape = Solape(a, b, otro);
                        if (gap <= 0f || gap > gapMax || solape <= 0.5f)
                        {
                            continue;
                        }
                        float medio = gap == b.min[eje] - a.max[eje] ? (a.max[eje] + b.min[eje]) * 0.5f : (b.max[eje] + a.min[eje]) * 0.5f;
                        float ini = Mathf.Max(a.min[otro], b.min[otro]);
                        float fin = Mathf.Min(a.max[otro], b.max[otro]);
                        float y = Mathf.Min(a.min.y, b.min.y) + 1f;
                        float minAncho = float.MaxValue;
                        Vector3 donde = Vector3.zero;
                        for (int s = 0; s < 3; s++)
                        {
                            float t = Mathf.Lerp(ini, fin, 0.1f + 0.4f * s);
                            Vector3 o = eje == 0 ? new Vector3(medio, y, t) : new Vector3(t, y, medio);
                            Vector3 d = eje == 0 ? Vector3.right : Vector3.forward;
                            if (Golpe(c, o, d, gapMax, false, out float d1) && Golpe(c, o, -d, gapMax, false, out float d2) && d1 + d2 < minAncho)
                            {
                                minAncho = d1 + d2;
                                donde = o;
                            }
                        }
                        if (minAncho < AnchoMinJugador)
                        {
                            Add(c, TipoNoCabeJugador, Ruta(lista[i].Key, c.Raiz) + " | " + lista[j].Key.name, donde,
                                "ancho libre a 1 m de alto " + F(minAncho) + " m (mínimo " + F(AnchoMinJugador) + " m)", AnchoMinJugador - minAncho);
                        }
                    }
                }
            }
            foreach (CalleZona cz in c.Raiz.GetComponentsInChildren<CalleZona>(false))
            {
                if (cz.Cantidad < 2)
                {
                    continue;
                }
                float tope = cz.Ancho * 0.5f + 3f;
                foreach (Vector3 s in Muestrear(cz.Puntos(), 4f, 0f))
                {
                    if (!Piso(c, s + Vector3.up * 3f, 20f, null, true, out RaycastHit h))
                    {
                        continue;
                    }
                    Vector3 o = h.point + Vector3.up;
                    Vector3 pt = Vector3.zero;
                    float ancho = ObtenerAnchoLibre(c, o, cz.Puntos(), s, tope, out pt);
                    if (ancho < AnchoMinCarro)
                    {
                        Add(c, TipoNoCabeCarro, Ruta(cz.transform, c.Raiz), o, "ancho libre " + F(ancho) + " m (mínimo " + F(AnchoMinCarro) + " m)",
                            AnchoMinCarro - ancho);
                    }
                }
            }
        }

        private static float ObtenerAnchoLibre(Ctx c, Vector3 origen, List<Vector3> eje, Vector3 muestra, float tope, out Vector3 dirUsada)
        {
            // Dirección del tramo más cercano a la muestra.
            Vector3 dir = Vector3.forward;
            float mejor = float.MaxValue;
            for (int i = 1; i < eje.Count; i++)
            {
                Vector3 m = (eje[i - 1] + eje[i]) * 0.5f;
                float d = XZ(m, muestra);
                if (d < mejor)
                {
                    mejor = d;
                    dir = new Vector3(eje[i].x - eje[i - 1].x, 0f, eje[i].z - eje[i - 1].z).normalized;
                }
            }
            Vector3 perp = new Vector3(dir.z, 0f, -dir.x);
            dirUsada = perp;
            Golpe(c, origen, perp, tope, true, out float d1);
            Golpe(c, origen, -perp, tope, true, out float d2);
            return d1 + d2;
        }

        // ------------------------------------------------------------------ 11: parkour

        private static void RevisarParkourCerro(Ctx c)
        {
            List<Prop> az = c.Props.Where(p => p.TieneB && (p.Nombre.StartsWith("AzoteaT", StringComparison.Ordinal)
                || p.Nombre.StartsWith("CC01", StringComparison.Ordinal) || p.Nombre.StartsWith("CC02", StringComparison.Ordinal))).ToList();
            List<Prop> pretiles = c.Props.Where(p => p.TieneB && p.Nombre.StartsWith("C05_PretilAzotea", StringComparison.Ordinal)).ToList();
            PuntoTrepar[] trepar = c.Raiz.GetComponentsInChildren<PuntoTrepar>(false);
            List<Prop> escaleras = c.Props.Where(p => p.TieneB && p.Nombre.StartsWith("CE", StringComparison.Ordinal)).ToList();
            HashSet<Prop> pretilesYa = new HashSet<Prop>();
            foreach (Prop a in az)
            {
                Bounds ab = a.B;
                float techoA = ab.max.y;
                bool entrada = false;
                bool pegada = false;
                int vecinas = 0;
                foreach (Prop b in az)
                {
                    if (b == a)
                    {
                        continue;
                    }
                    float gap = GapXZ(ab, b.B);
                    if (gap >= VecinaAzotea)
                    {
                        continue;
                    }
                    vecinas++;
                    if (Saltable(gap, techoA - b.B.max.y))
                    {
                        entrada = true;
                    }
                    if (gap <= 0.5f)
                    {
                        pegada = true;
                    }
                }
                if (pegada)
                {
                    foreach (Prop pr in pretiles)
                    {
                        Bounds pb = pr.B;
                        bool sobreA = pb.center.x > ab.min.x - 0.5f && pb.center.x < ab.max.x + 0.5f && pb.center.z > ab.min.z - 0.5f
                            && pb.center.z < ab.max.z + 0.5f && Mathf.Abs(pb.min.y - techoA) <= 0.6f;
                        if (sobreA && pb.size.y > PretilAlto && pretilesYa.Add(pr))
                        {
                            Add(c, TipoPretil, Ruta(pr.T, c.Raiz), pb.center, "pretil de " + F(pb.size.y) + " m con vecina pegada (máximo " + F(PretilAlto) + " m)",
                                pb.size.y - PretilAlto);
                        }
                    }
                }
                if (entrada)
                {
                    continue;
                }
                bool acceso = false;
                foreach (PuntoTrepar pt in trepar)
                {
                    Vector3 s = pt.Salida;
                    if (s.x > ab.min.x - 1.5f && s.x < ab.max.x + 1.5f && s.z > ab.min.z - 1.5f && s.z < ab.max.z + 1.5f
                        && Mathf.Abs(s.y - techoA) <= 1.5f)
                    {
                        acceso = true;
                        break;
                    }
                }
                if (!acceso)
                {
                    foreach (Prop e in escaleras)
                    {
                        if (GapXZ(ab, e.B) < 1.5f && Mathf.Abs(e.B.max.y - techoA) <= 1.5f)
                        {
                            acceso = true;
                            break;
                        }
                    }
                }
                if (!acceso)
                {
                    Vector3[] lados =
                    {
                        new Vector3(ab.max.x + 1f, 0f, ab.center.z), new Vector3(ab.min.x - 1f, 0f, ab.center.z),
                        new Vector3(ab.center.x, 0f, ab.max.z + 1f), new Vector3(ab.center.x, 0f, ab.min.z - 1f),
                    };
                    foreach (Vector3 l in lados)
                    {
                        if (Piso(c, new Vector3(l.x, techoA + 1f, l.z), 400f, null, false, out RaycastHit h) && techoA - h.point.y <= AgarreMaximo)
                        {
                            acceso = true;
                            break;
                        }
                    }
                }
                if (!acceso)
                {
                    Add(c, TipoAzoteaAislada, Ruta(a.T, c.Raiz), new Vector3(ab.center.x, techoA, ab.center.z),
                        vecinas + " azoteas a menos de " + F(VecinaAzotea) + " m y ninguna se puede saltar; sin PuntoTrepar ni escalera", Mathf.Max(1f, ab.size.y));
                }
            }
        }

        private static void RevisarContenedores(Ctx c)
        {
            List<Prop> cont = c.Props.Where(p => p.TieneB && p.Nombre.StartsWith("PT01", StringComparison.Ordinal)).ToList();
            if (cont.Count == 0)
            {
                return;
            }
            PuntoTrepar[] trepar = c.Raiz.GetComponentsInChildren<PuntoTrepar>(false);
            List<Prop> escalones = c.Props.Where(p => p.TieneB && !p.Nombre.StartsWith("PT01", StringComparison.Ordinal)).ToList();
            float[] suelo = new float[cont.Count];
            bool[] alcanzado = new bool[cont.Count];
            Queue<int> cola = new Queue<int>();
            for (int i = 0; i < cont.Count; i++)
            {
                Bounds b = cont[i].B;
                suelo[i] = Piso(c, new Vector3(b.center.x, b.min.y + 0.05f, b.center.z), 400f, cont[i].Propios, false, out RaycastHit h)
                    ? h.point.y : b.min.y;
                bool semilla = b.max.y - suelo[i] <= AgarreMaximo;
                foreach (PuntoTrepar pt in trepar)
                {
                    Vector3 s = pt.Salida;
                    if (s.x > b.min.x - 1f && s.x < b.max.x + 1f && s.z > b.min.z - 1f && s.z < b.max.z + 1f && Mathf.Abs(s.y - b.max.y) <= 1f)
                    {
                        semilla = true;
                        break;
                    }
                }
                if (!semilla)
                {
                    foreach (Prop e in escalones)
                    {
                        float altoE = e.B.max.y - suelo[i];
                        if (altoE >= 0.3f && altoE <= AgarreMaximo && GapXZ(b, e.B) < 1f && b.max.y - e.B.max.y <= AgarreMaximo)
                        {
                            semilla = true;
                            break;
                        }
                    }
                }
                if (semilla)
                {
                    alcanzado[i] = true;
                    cola.Enqueue(i);
                }
            }
            while (cola.Count > 0)
            {
                int i = cola.Dequeue();
                for (int j = 0; j < cont.Count; j++)
                {
                    if (!alcanzado[j] && GapXZ(cont[i].B, cont[j].B) <= 1f && cont[j].B.max.y - cont[i].B.max.y <= AgarreMaximo)
                    {
                        alcanzado[j] = true;
                        cola.Enqueue(j);
                    }
                }
            }
            for (int i = 0; i < cont.Count; i++)
            {
                if (!alcanzado[i])
                {
                    Bounds b = cont[i].B;
                    Add(c, TipoContenedor, Ruta(cont[i].T, c.Raiz), b.center, "tope a " + F(b.max.y - suelo[i]) + " m del suelo sin PuntoTrepar ni contenedor/escalón que llegue",
                        b.max.y - suelo[i]);
                }
            }
        }

        // ------------------------------------------------------------------ 12: carros y peatones

        private static void RevisarCarros(Ctx c)
        {
            CalleZona[] calles = c.Raiz.GetComponentsInChildren<CalleZona>(false);
            foreach (Prop p in c.Props)
            {
                bool carro = p.Nombre.StartsWith("CarroViejo", StringComparison.Ordinal)
                    || (p.T.parent != null && p.T.parent.name == "Carros" && p.Nombre.StartsWith("Carro", StringComparison.Ordinal));
                if (!carro || !p.TieneB)
                {
                    continue;
                }
                Bounds b = p.B;
                foreach (CalleZona cz in calles)
                {
                    float min = float.MaxValue;
                    foreach (Vector3 s in Muestrear(cz.Puntos(), 0.5f, 0f))
                    {
                        float dx = Mathf.Max(0f, Mathf.Max(b.min.x - s.x, s.x - b.max.x));
                        float dz = Mathf.Max(0f, Mathf.Max(b.min.z - s.z, s.z - b.max.z));
                        min = Mathf.Min(min, Mathf.Sqrt(dx * dx + dz * dz));
                    }
                    if (min < 1f)
                    {
                        Add(c, TipoCarro, Ruta(p.T, c.Raiz), b.center, "a " + F(min) + " m del eje de " + cz.name, 1f - min);
                        break;
                    }
                }
            }
        }

        private static void RevisarPeatones(Ctx c)
        {
            // Las rutas de PeatonMundo se generan al jugar; aquí se revisa la línea de banqueta de cada CalleZona, que es por donde caminan.
            foreach (CalleZona cz in c.Raiz.GetComponentsInChildren<CalleZona>(false))
            {
                if (!cz.Banquetas || cz.Autopista || cz.Cantidad < 2)
                {
                    continue;
                }
                float lat = Mathf.Max(0.5f, cz.Ancho * 0.5f - 0.75f);
                foreach (float l in new[] { lat, -lat })
                {
                    foreach (Vector3 s in Muestrear(cz.Puntos(), 3f, l))
                    {
                        if (!Piso(c, s + Vector3.up * 3f, 20f, null, true, out RaycastHit h))
                        {
                            continue;
                        }
                        Vector3 p0 = h.point + Vector3.up * 0.35f;
                        if (Physics.CheckCapsule(p0, p0 + Vector3.up * 1.1f, 0.25f, ~0, QueryTriggerInteraction.Ignore))
                        {
                            Add(c, TipoPeaton, Ruta(cz.transform, c.Raiz), h.point, "un collider ocupa la banqueta (lateral " + F(l) + " m)", 1f);
                        }
                    }
                }
            }
        }

        // ------------------------------------------------------------------ agrupar, cámaras y fotos

        private static List<Hallazgo> Agrupar(List<Hallazgo> todos)
        {
            List<Hallazgo> res = new List<Hallazgo>();
            foreach (IGrouping<string, Hallazgo> g in todos.GroupBy(q => q.tipo))
            {
                Dictionary<(int, int, int), List<Hallazgo>> celdas = new Dictionary<(int, int, int), List<Hallazgo>>();
                foreach (Hallazgo h in g.OrderByDescending(x => x.valor))
                {
                    (int, int, int) k = CeldaDe(h.posicion);
                    Hallazgo semilla = null;
                    for (int dx = -1; dx <= 1 && semilla == null; dx++)
                    {
                        for (int dy = -1; dy <= 1 && semilla == null; dy++)
                        {
                            for (int dz = -1; dz <= 1 && semilla == null; dz++)
                            {
                                if (celdas.TryGetValue((k.Item1 + dx, k.Item2 + dy, k.Item3 + dz), out List<Hallazgo> l))
                                {
                                    semilla = l.FirstOrDefault(s => (s.posicion - h.posicion).sqrMagnitude < RadioAgrupar * RadioAgrupar);
                                }
                            }
                        }
                    }
                    if (semilla != null)
                    {
                        semilla.cantidad += h.cantidad;
                        continue;
                    }
                    if (!celdas.TryGetValue(k, out List<Hallazgo> lista))
                    {
                        lista = new List<Hallazgo>();
                        celdas[k] = lista;
                    }
                    lista.Add(h);
                    res.Add(h);
                }
            }
            return res;
        }

        private static (int, int, int) CeldaDe(Vector3 p)
        {
            return (Mathf.FloorToInt(p.x / RadioAgrupar), Mathf.FloorToInt(p.y / RadioAgrupar), Mathf.FloorToInt(p.z / RadioAgrupar));
        }

        /// <summary>Cámara a 6-15 m mirando al hallazgo: prueba 8 direcciones y distancias hasta hallar una sin pared en medio.</summary>
        private static void AsignarCamara(Hallazgo h)
        {
            Vector3 mira = h.posicion + Vector3.up;
            float[] distancias = { 10f, 7f, 14f, 6f };
            Vector3 elegida = mira + new Vector3(0.7f, 0.5f, 0.7f) * 10f;
            bool ok = false;
            foreach (float d in distancias)
            {
                for (int k = 0; k < 8 && !ok; k++)
                {
                    float ang = (45f + k * 45f) * Mathf.Deg2Rad;
                    Vector3 cam = mira + new Vector3(Mathf.Cos(ang) * d, d * 0.45f, Mathf.Sin(ang) * d);
                    if (!Physics.Linecast(mira, cam, ~0, QueryTriggerInteraction.Ignore))
                    {
                        elegida = cam;
                        ok = true;
                    }
                }
                if (ok)
                {
                    break;
                }
            }
            h.conCamara = true;
            h.camaraPosicion = elegida;
            h.camaraAngulos = Quaternion.LookRotation(mira - elegida).eulerAngles;
        }

        private static string Slug(string tipo)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char ch in tipo.ToLowerInvariant())
            {
                switch (ch)
                {
                    case 'á': sb.Append('a'); break;
                    case 'é': sb.Append('e'); break;
                    case 'í': sb.Append('i'); break;
                    case 'ó': sb.Append('o'); break;
                    case 'ú': sb.Append('u'); break;
                    case 'ñ': sb.Append('n'); break;
                    case ' ': sb.Append('-'); break;
                    default: sb.Append(ch); break;
                }
            }
            return sb.ToString();
        }

        private static string NombreFoto(Hallazgo h, string sufijo)
        {
            return h.zona + "-" + Slug(h.tipo) + "-" + h.fotoN + "-" + sufijo + ".png";
        }

        private static void SeleccionarFotos(ArchivoHallazgos archivo, string sufijo)
        {
            foreach (IGrouping<string, Hallazgo> g in archivo.hallazgos.GroupBy(q => q.tipo))
            {
                int n = 0;
                foreach (Hallazgo h in g.OrderByDescending(x => x.valor).ThenByDescending(x => x.cantidad).Take(FotosPorTipo))
                {
                    h.fotoN = ++n;
                    h.foto = RutaFotos + "/" + NombreFoto(h, sufijo);
                }
            }
        }

        private static void TomarFotos(ArchivoHallazgos archivo, string raizProyecto, string sufijo)
        {
            GameObject go = new GameObject("CamaraBarrido");
            try
            {
                Camera cam = go.AddComponent<Camera>();
                cam.fieldOfView = 60f;
                cam.nearClipPlane = 0.1f;
                cam.farClipPlane = 1500f;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.35f, 0.4f, 0.5f);
                foreach (Hallazgo h in archivo.hallazgos)
                {
                    if (h.fotoN <= 0 || !h.conCamara)
                    {
                        continue;
                    }
                    cam.transform.position = h.camaraPosicion;
                    cam.transform.rotation = Quaternion.Euler(h.camaraAngulos);
                    ArteT250.Guardar(cam, Path.Combine(raizProyecto, RutaFotos, NombreFoto(h, sufijo)));
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        // ------------------------------------------------------------------ escenas, informe y argumentos

        private static GameObject AbrirZona(string ruta)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
            if (prefab == null)
            {
                return null;
            }
            GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            Physics.SyncTransforms();
            return inst;
        }

        private static string RaizProyecto()
        {
            return Directory.GetParent(Application.dataPath).FullName;
        }

        private static string LeerSufijo()
        {
            string[] a = Environment.GetCommandLineArgs();
            for (int i = 0; i < a.Length - 1; i++)
            {
                if (a[i] == "-sufijo")
                {
                    return a[i + 1];
                }
            }
            return "antes";
        }

        private static string Informe(List<ArchivoHallazgos> archivos, string sufijo)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("# Barrido T-1360 (" + sufijo + ")");
            sb.AppendLine();
            foreach (ArchivoHallazgos a in archivos)
            {
                sb.AppendLine("## " + a.zona);
                sb.AppendLine();
                sb.AppendLine("| Tipo | Grupos | Total de hallazgos |");
                sb.AppendLine("| --- | ---: | ---: |");
                foreach (IGrouping<string, Hallazgo> g in a.hallazgos.GroupBy(q => q.tipo).OrderByDescending(gg => gg.Sum(q => q.cantidad)))
                {
                    sb.AppendLine("| " + g.Key + " | " + g.Count() + " | " + g.Sum(h => h.cantidad) + " |");
                }
                sb.AppendLine();
                foreach (IGrouping<string, Hallazgo> g in a.hallazgos.GroupBy(q => q.tipo).OrderBy(gg => gg.Key))
                {
                    sb.AppendLine("### " + a.zona + ": " + g.Key);
                    sb.AppendLine();
                    sb.AppendLine("| Ruta | Posición | Detalle | x | Foto |");
                    sb.AppendLine("| --- | --- | --- | ---: | --- |");
                    foreach (Hallazgo h in g.OrderByDescending(x => x.valor).ThenByDescending(x => x.cantidad).Take(TopMd))
                    {
                        sb.AppendLine("| " + h.ruta.Replace("|", "/") + " | (" + F(h.posicion.x) + ", " + F(h.posicion.y) + ", " + F(h.posicion.z) + ") | "
                            + h.detalle.Replace("|", "/") + " | " + h.cantidad + " | " + (string.IsNullOrEmpty(h.foto) ? "" : Path.GetFileName(h.foto)) + " |");
                    }
                    sb.AppendLine();
                }
            }
            return sb.ToString();
        }
    }
}
