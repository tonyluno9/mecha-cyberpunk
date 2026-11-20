using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Tipo = MechaCyberpunk.Mundo.Editor.CalculoEdificiosT1502.Tipo;

namespace MechaCyberpunk.Mundo.Editor
{
    /// <summary>
    /// T-1523: Ciudad_Prologo sin formas básicas de Unity y sin objetos flotando.
    /// Orden en batchmode (cada paso es idempotente):
    ///   1. -executeMethod MechaCyberpunk.Mundo.Editor.PrimitivasT1523.Diagnostico  -> Logs/t1523-primitivas.txt y docs/primitivas-20nov-B.md (no cambia nada)
    ///   2. -executeMethod MechaCyberpunk.Mundo.Editor.PrimitivasT1523.Aplicar      -> cambia la escena, la guarda y vuelve a correr el diagnóstico
    /// Una primitiva se cambia apagando su MeshRenderer (el collider se queda) y poniendo al lado un modelo real; nada se borra.
    /// Se dejan a propósito (y se cuentan aparte): decales planos (líneas, charcos, ventanas), luces y rótulos, losas planas, el muro
    /// perimetral (T-1524), estructura de interiores y las tragamonedas (esperan el arte TR01/TR02, ver docs/assets-faltan.md).
    /// </summary>
    public static class PrimitivasT1523
    {
        public const string RutaLog = "Logs/t1523-primitivas.txt";
        public const string RutaAntes = "Logs/t1523-antes.txt";
        public const string RutaDoc = "docs/primitivas-20nov-B.md";
        public const string PrefijoProp = "T1523_Prop_";

        // ---- Umbrales (m) ----
        private const float AltoEdificio = 6f;
        private const float LadoMinimoEdificio = 4f;
        private const float AltoCasaBaja = 3f;
        private const float LadoMinimoCasaBaja = 3f;
        private const float LadoLargoCasaBaja = 5f;
        private const float EspesorLosa = 0.35f;
        private const float LargoMinimoLosa = 1.5f;
        private const float TamanoMaximoGrupo = 5f;
        private const float HuecoFlotante = 0.3f;
        private const float HuecoMaximoAsentar = 15f;
        private const float AlturaMaximaCielo = 60f;
        private const float MargenContacto = 0.3f;
        private const float TamanoMaximoFlotante = 40f;
        private const float TamanoMaximoRaizItem = 6f;
        private const float CeldaRejilla = 8f;
        private const int CeldasMaximasRejilla = 400;
        private const float EstiramientoProp = 1.8f;
        private const float AlturaPoste = 2.5f;

        private const string Calle = "Assets/Arte/Calle/Prefabs/";
        private const string PrefabBanca = "Assets/Arte/Zonas/PZ/PZ01_Banca.prefab";
        private const string PrefabMesa = "Assets/Arte/TallerCasa/Prefabs/H04_MesaSillas.prefab";
        private const string PrefabPuesto = Calle + "C12_PuestoTacos.prefab";
        private const string PrefabToldo = Calle + "TL01_Toldo.prefab";
        private const string PrefabBolardo = Calle + "BL01_Bolardo.prefab";
        private const string PrefabPoste = Calle + "C07_PosteLuz.prefab";
        private const string PrefabTambo = Calle + "C16_Tambo.prefab";
        private const string PrefabHuacales = Calle + "C17_Huacales.prefab";
        private const string PrefabBarrera = "Assets/Arte/Zonas/CB/CB01_BarreraConcreto_a.prefab";

        private static readonly string[] ClavesLuzRotulo =
        {
            "foco", "lente", "aviso", "tira", "neon", "franja", "pantalla", "letrero", "luz", "halo", "linea", "cebra", "charco",
            "ventana", "humo", "rotulo", "cartel", "anuncio", "interiorcalido", "led", "brillo", "estela", "faro"
        };

        private static readonly string[] ClavesPerimetral = { "t1513", "muroperimetral", "muro_perimetral", "perimetral", "mu01", "mu02", "mu03", "murociudad" };

        private static readonly string[] ClavesSinFlotar =
        {
            "dron", "nave", "aereo", "aerea", "patrulla", "carro", "auto_", "moto", "camion", "peaton", "civil", "pandill", "jugador", "mecha",
            "perro", "personaje", "trafico", "autopista", "puente", "pasarela", "cable", "skyline", "nube", "cielo", "estrella", "luna", "sol_",
            "niebla", "fondo", "horizonte", "muro", "mu0", "globo", "holo", "lampara", "letrero", "neon", "anuncio", "pantalla", "luz",
            "toldo", "farol", "semaforo", "vehiculo", "amb_", "reflejo", "probe"
        };

        private static readonly string[] ClavesAltoPermitido =
        {
            "edificio", "torre", "antena", "nave", "dron", "luz", "ed0", "ed1", "t1502_modelo", "skyline", "fondo", "horizonte", "cielo", "nube",
            "estrella", "luna", "niebla", "mu0", "muro", "autopista", "grua", "aereo", "aerea", "humo", "holo", "anuncio", "pantalla", "neon",
            "letrero", "piso", "fachada", "pretil", "cornisa", "azotea", "c20", "c21", "c22", "c05", "c03", "c01", "c02", "nv0", "p11", "p12",
            "lo0", "cc0", "ch0", "ml0", "pu0", "tinaco", "bolardo", "tk0", "cy04"
        };

        public enum Cat
        {
            /// <summary>Cubo de edificio o casa: se cambia por un modelo (ED, NV, P, LO, CC...).</summary>
            Edificio,
            /// <summary>Cubo "Edificio" de un barrio con kit de fachadas: se apaga el render si el kit lo tapa.</summary>
            EdificioConKit,
            /// <summary>Prop o pieza chica: se cambia por un prop real o se apaga.</summary>
            Prop,
            DecalPlano,
            LuzRotulo,
            LosaPlana,
            MuroPerimetral,
            MuroOtro,
            EstructuraInterior,
            PendienteArte
        }

        public class Item
        {
            public MeshRenderer Renderer;
            public string Ruta;
            public string Raiz;
            public string Barrio;
            public string Malla;
            public Bounds Caja;
            public Cat Cat;
            public string Accion = "";
        }

        private class Medida
        {
            public GameObject Prefab;
            public Vector3 Tam;
            public Vector3 Desfase; // (centro x, base y, centro z) del modelo sin girar a escala 1
        }

        private class Flotante
        {
            public Transform Raiz;
            public string Ruta;
            public Bounds Caja;
            public float Hueco;
            public string Motivo;
            public string Accion = "";
        }

        // ================= Menús y puntos de entrada =================

        [MenuItem("Mecha Cyberpunk/T-1523 Diagnostico de primitivas")]
        public static void Diagnostico()
        {
            Scene escena = ReemplazarEdificiosT1502.Abrir();
            Physics.SyncTransforms();
            List<Item> items = Escanear(escena);
            List<Flotante> flotantes = BuscarFlotantes(escena, false);
            EscribirAntesSiFalta(items);
            Dictionary<string, int> otras = ContarOtrasEscenas();
            ReemplazarEdificiosT1502.Abrir();
            Informar(items, flotantes, otras);
            Debug.Log("[T-1523] Diagnostico: " + items.Count + " primitivas visibles (" + Violaciones(items) + " por cambiar), "
                + flotantes.Count + " flotantes/altos; ver " + RutaLog + " y " + RutaDoc);
        }

        [MenuItem("Mecha Cyberpunk/T-1523 Aplicar sin primitivas")]
        public static void Aplicar()
        {
            Scene escena = ReemplazarEdificiosT1502.Abrir();
            Physics.SyncTransforms();
            EscribirAntesSiFalta(Escanear(escena));

            int edificios = CambiarEdificios(escena);
            int encimados = ReemplazarEdificiosT1512.QuitarEncimados(escena);
            Physics.SyncTransforms();
            int props = CambiarProps(escena, out int propsApagados);
            Physics.SyncTransforms();
            List<Flotante> flotantes = BuscarFlotantes(escena, true);

            EditorSceneManager.MarkSceneDirty(escena);
            EditorSceneManager.SaveScene(escena);
            Debug.Log("[T-1523] Aplicar: " + edificios + " edificios con modelo, " + encimados + " modelos apagados por encimarse, "
                + props + " props con modelo, " + propsApagados + " props apagados sin equivalente, "
                + flotantes.Count(f => f.Accion.StartsWith("asentado")) + " flotantes asentados, "
                + flotantes.Count(f => f.Accion.StartsWith("apagado")) + " flotantes apagados");
            Diagnostico();
        }

        // ================= Escaneo =================

        private static bool EsIncorporada(string ruta)
        {
            return !string.IsNullOrEmpty(ruta) && (ruta.StartsWith("Library/") || ruta.Contains("unity default resources") || ruta.Contains("unity_builtin_extra"));
        }

        /// <summary>Malla generada por código (sin archivo) con pocos vértices: una caja.</summary>
        private static bool EsCajaProcedural(Mesh m)
        {
            return m.vertexCount > 0 && m.vertexCount <= 36;
        }

        public static List<Item> Escanear(Scene escena)
        {
            Dictionary<string, Vector3> centros = ReemplazarEdificiosT1502.CentrosBarrio(escena);
            List<Item> lista = new List<Item>();
            foreach (GameObject raiz in escena.GetRootGameObjects())
            {
                foreach (MeshRenderer mr in raiz.GetComponentsInChildren<MeshRenderer>(false))
                {
                    if (!mr.enabled)
                    {
                        continue;
                    }
                    MeshFilter mf = mr.GetComponent<MeshFilter>();
                    if (mf == null || mf.sharedMesh == null)
                    {
                        continue;
                    }
                    string rutaMalla = AssetDatabase.GetAssetPath(mf.sharedMesh);
                    if (!EsIncorporada(rutaMalla) && !(string.IsNullOrEmpty(rutaMalla) && EsCajaProcedural(mf.sharedMesh)))
                    {
                        continue;
                    }
                    string ruta = RutaDe(mr.transform);
                    if (ruta.Contains(ReemplazarEdificiosT1502.PrefijoModelo) || ruta.Contains(PrefijoProp))
                    {
                        continue;
                    }
                    Bounds caja = mr.bounds;
                    Item it = new Item
                    {
                        Renderer = mr,
                        Ruta = ruta,
                        Raiz = raiz.name,
                        Caja = caja,
                        Malla = string.IsNullOrEmpty(mf.sharedMesh.name) ? "Procedural" : mf.sharedMesh.name,
                        Barrio = BarrioDe(caja.center, centros)
                    };
                    it.Cat = Clasificar(it);
                    lista.Add(it);
                }
            }
            return lista;
        }

        private static bool Tiene(string texto, params string[] claves)
        {
            string t = texto.ToLowerInvariant();
            foreach (string c in claves)
            {
                if (t.Contains(c))
                {
                    return true;
                }
            }
            return false;
        }

        private static Cat Clasificar(Item it)
        {
            string ruta = it.Ruta;
            Vector3 s = it.Caja.size;
            string nombre = it.Renderer.name;
            if (Tiene(ruta, ClavesPerimetral))
            {
                return Cat.MuroPerimetral;
            }
            if (Tiene(ruta, "tragamonedas"))
            {
                return Cat.PendienteArte;
            }
            if (it.Malla == "Quad")
            {
                return Cat.DecalPlano;
            }
            if (Tiene(nombre, ClavesLuzRotulo))
            {
                return Cat.LuzRotulo;
            }
            Tipo tipo = CalculoEdificiosT1502.Clasificar(ruta, s);
            switch (tipo)
            {
                case Tipo.EdificioAfueras:
                case Tipo.Torre:
                case Tipo.BloqueGrande:
                    return Cat.Edificio;
                case Tipo.EdificioConKit:
                    return Cat.EdificioConKit;
                case Tipo.Muro:
                    return Cat.MuroOtro;
                case Tipo.LocalConInterior:
                    return Cat.EstructuraInterior;
            }
            float menor = Mathf.Min(s.x, Mathf.Min(s.y, s.z));
            float largoSuelo = Mathf.Max(s.x, s.z);
            if (menor <= EspesorLosa && largoSuelo >= LargoMinimoLosa)
            {
                return Cat.LosaPlana;
            }
            if (s.y >= AltoEdificio && Mathf.Min(s.x, s.z) >= LadoMinimoEdificio)
            {
                return Cat.Edificio;
            }
            if (Tiene(ruta, "interior", "clandestino", "casino") && Mathf.Max(s.x, Mathf.Max(s.y, s.z)) >= 2.5f)
            {
                return Cat.EstructuraInterior;
            }
            if (s.y >= AltoCasaBaja && Mathf.Min(s.x, s.z) >= LadoMinimoCasaBaja && largoSuelo >= LadoLargoCasaBaja)
            {
                return Cat.Edificio;
            }
            return Cat.Prop;
        }

        /// <summary>Verdadero si la categoría se cuenta como "primitiva por cambiar" (las demás se dejan a propósito).</summary>
        private static bool EsPorCambiar(Cat c)
        {
            return c == Cat.Edificio || c == Cat.EdificioConKit || c == Cat.Prop;
        }

        private static int Violaciones(List<Item> items)
        {
            return items.Count(i => EsPorCambiar(i.Cat));
        }

        // ================= Edificios =================

        private static int CambiarEdificios(Scene escena)
        {
            Dictionary<string, Vector3> centros = ReemplazarEdificiosT1502.CentrosBarrio(escena);
            int cambiados = 0;
            int apagadosKit = 0;
            float original = ReemplazarEdificiosT1502.LimiteDistorsion;
            try
            {
                for (int pasada = 0; pasada < 2; pasada++)
                {
                    Dictionary<string, List<ReemplazarEdificiosT1502.Medida>> paletas = CargarPaletas(pasada == 1);
                    ReemplazarEdificiosT1502.LimiteDistorsion = pasada == 0 ? CalculoEdificiosT1512.DistorsionPrimeraPasada : CalculoEdificiosT1502.DistorsionMaxima;
                    foreach (Item it in Escanear(escena).Where(x => x.Cat == Cat.Edificio).ToList())
                    {
                        ReemplazarEdificiosT1502.Primitivo p = new ReemplazarEdificiosT1502.Primitivo
                        {
                            Renderer = it.Renderer,
                            Ruta = it.Ruta,
                            Barrio = it.Barrio,
                            Caja = HastaElPiso(it),
                            Tipo = Tipo.EdificioAfueras,
                            TienePendiente = false
                        };
                        if (ReemplazarEdificiosT1502.Cambiar(p, paletas, centros) == null)
                        {
                            cambiados++;
                            Transform marca = it.Renderer.transform.Find(ReemplazarEdificiosT1502.NombrePendiente);
                            if (marca != null)
                            {
                                Object.DestroyImmediate(marca.gameObject);
                            }
                        }
                    }
                }
            }
            finally
            {
                ReemplazarEdificiosT1502.LimiteDistorsion = original;
            }

            // Cubos "Edificio" de barrio con kit de fachadas: si el kit ya los cubre, el render del cubo sobra.
            foreach (Item it in Escanear(escena).Where(x => x.Cat == Cat.EdificioConKit).ToList())
            {
                if (CubiertoPorOtrosRenderers(it))
                {
                    it.Renderer.enabled = false;
                    apagadosKit++;
                }
            }
            Debug.Log("[T-1523] Edificios: " + cambiados + " cubos con modelo, " + apagadosKit + " cubos con kit apagados (render)");
            return cambiados;
        }

        /// <summary>Caja del cubo, alargada hacia abajo hasta el piso si flota más de 0.3 m.</summary>
        private static Bounds HastaElPiso(Item it)
        {
            Bounds b = it.Caja;
            float hueco;
            if (HuecoAlPiso(it.Renderer.transform, b, out hueco) && hueco > HuecoFlotante && hueco <= HuecoMaximoAsentar)
            {
                Vector3 min = b.min;
                Vector3 max = b.max;
                min.y -= hueco;
                b.SetMinMax(min, max);
            }
            return b;
        }

        private static bool CubiertoPorOtrosRenderers(Item it)
        {
            Bounds b = it.Caja;
            b.Expand(1f);
            int propios = 0;
            int ajenos = 0;
            foreach (Renderer r in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
            {
                if (!r.enabled || !r.gameObject.activeInHierarchy || r == it.Renderer || !b.Intersects(r.bounds))
                {
                    continue;
                }
                MeshFilter mf = r.GetComponent<MeshFilter>();
                if (mf == null || mf.sharedMesh == null || EsIncorporada(AssetDatabase.GetAssetPath(mf.sharedMesh)))
                {
                    propios++;
                }
                else
                {
                    ajenos++;
                }
            }
            return ajenos >= 4;
        }

        private static Dictionary<string, List<ReemplazarEdificiosT1502.Medida>> CargarPaletas(bool conAnteriores)
        {
            Dictionary<string, ReemplazarEdificiosT1502.Medida> cache = new Dictionary<string, ReemplazarEdificiosT1502.Medida>();
            Dictionary<string, List<ReemplazarEdificiosT1502.Medida>> r = new Dictionary<string, List<ReemplazarEdificiosT1502.Medida>>();
            foreach (string barrio in CalculoEdificiosT1512.BarriosConPaleta())
            {
                IEnumerable<string> rutas = CalculoEdificiosT1512.RutasPara(barrio);
                if (conAnteriores)
                {
                    rutas = rutas.Concat(AnterioresDe(barrio));
                }
                r[barrio] = ReemplazarEdificiosT1502.Medidas(rutas.Distinct().ToArray(), cache);
            }
            IEnumerable<string> todas = CalculoEdificiosT1512.RutasPara("");
            if (conAnteriores)
            {
                todas = todas.Concat(ReemplazarEdificiosT1502.EstiloNeon).Concat(ReemplazarEdificiosT1502.EstiloSuburbios).Concat(ReemplazarEdificiosT1502.EstiloMercado);
            }
            r[""] = ReemplazarEdificiosT1502.Medidas(todas.Distinct().ToArray(), cache);
            return r;
        }

        private static IEnumerable<string> AnterioresDe(string barrio)
        {
            switch (barrio)
            {
                case "NeonAlto": return ReemplazarEdificiosT1502.EstiloNeon;
                case "Suburbios": return ReemplazarEdificiosT1502.EstiloSuburbios;
                case "Tianguis":
                case "Mercado": return ReemplazarEdificiosT1502.EstiloMercado;
                default: return new string[0];
            }
        }

        // ================= Props =================

        private static int CambiarProps(Scene escena, out int apagados)
        {
            apagados = 0;
            int cambiados = 0;
            List<Item> todos = Escanear(escena);
            HashSet<Renderer> primitivas = new HashSet<Renderer>(todos.Select(x => (Renderer)x.Renderer));
            Dictionary<Transform, bool> cacheGrupo = new Dictionary<Transform, bool>();
            Dictionary<Transform, List<Item>> grupos = new Dictionary<Transform, List<Item>>();
            foreach (Item it in todos.Where(x => x.Cat == Cat.Prop))
            {
                Transform g = RaizDeGrupo(it.Renderer.transform, primitivas, cacheGrupo);
                List<Item> l;
                if (!grupos.TryGetValue(g, out l))
                {
                    l = new List<Item>();
                    grupos[g] = l;
                }
                l.Add(it);
            }

            Dictionary<string, Medida> medidas = new Dictionary<string, Medida>();
            foreach (KeyValuePair<Transform, List<Item>> par in grupos)
            {
                Transform grupo = par.Key;
                List<Item> miembros = par.Value;
                Bounds g = miembros[0].Caja;
                foreach (Item m in miembros)
                {
                    g.Encapsulate(m.Caja);
                }
                bool poste;
                string ruta = ElegirProp(grupo, miembros, g.size, out poste);
                if (ruta == null)
                {
                    foreach (Item m in miembros)
                    {
                        m.Renderer.enabled = false;
                    }
                    apagados += miembros.Count;
                    Debug.Log("[T-1523] Prop apagado (sin equivalente): " + RutaDe(grupo) + " " + ReemplazarEdificiosT1502.Tam(g.size) + " x" + miembros.Count);
                    continue;
                }
                Medida med;
                if (!medidas.TryGetValue(ruta, out med))
                {
                    med = Medir(ruta);
                    medidas[ruta] = med;
                }
                if (med == null)
                {
                    Debug.LogWarning("[T-1523] Falta el prefab " + ruta + ": se deja " + RutaDe(grupo));
                    continue;
                }
                ColocarProp(escena, grupo, miembros, g, med, poste);
                cambiados++;
            }
            Debug.Log("[T-1523] Props: " + grupos.Count + " grupos (" + cambiados + " con modelo, " + apagados + " piezas apagadas)");
            return cambiados;
        }

        /// <summary>Sube por la jerarquía mientras el padre solo tenga primitivas visibles y quepa en TamanoMaximoGrupo.</summary>
        private static Transform RaizDeGrupo(Transform t, HashSet<Renderer> primitivas, Dictionary<Transform, bool> cache)
        {
            Transform g = t;
            while (g.parent != null && g.parent.parent != null && PadreValido(g.parent, primitivas, cache))
            {
                g = g.parent;
            }
            return g;
        }

        private static bool PadreValido(Transform p, HashSet<Renderer> primitivas, Dictionary<Transform, bool> cache)
        {
            bool ok;
            if (cache.TryGetValue(p, out ok))
            {
                return ok;
            }
            ok = false;
            Renderer[] rs = p.GetComponentsInChildren<Renderer>(false);
            Bounds b = new Bounds();
            bool hay = false;
            bool todoPrimitivo = true;
            foreach (Renderer r in rs)
            {
                if (!r.enabled)
                {
                    continue;
                }
                if (!primitivas.Contains(r))
                {
                    todoPrimitivo = false;
                    break;
                }
                if (!hay)
                {
                    b = r.bounds;
                    hay = true;
                }
                else
                {
                    b.Encapsulate(r.bounds);
                }
            }
            if (todoPrimitivo && hay && Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z)) <= TamanoMaximoGrupo)
            {
                ok = true;
            }
            cache[p] = ok;
            return ok;
        }

        /// <summary>Ruta del prefab que sustituye al grupo, o null si no hay un equivalente razonable (entonces se apaga).</summary>
        private static string ElegirProp(Transform grupo, List<Item> miembros, Vector3 s, out bool poste)
        {
            poste = false;
            StringBuilder nombres = new StringBuilder(grupo.name);
            foreach (Item m in miembros)
            {
                nombres.Append('/').Append(m.Renderer.name);
            }
            string n = nombres.ToString();
            float alto = s.y;
            float largo = Mathf.Max(s.x, s.z);
            float ancho = Mathf.Min(s.x, s.z);
            bool cilindros = miembros.All(m => m.Malla == "Cylinder");
            bool esferas = miembros.All(m => m.Malla == "Sphere" || m.Malla == "Capsule");

            if (Tiene(n, "manta", "toldo"))
            {
                return PrefabToldo;
            }
            if (Tiene(n, "puesto") && largo >= 1.5f)
            {
                return PrefabPuesto;
            }
            if (Tiene(n, "banco", "banca", "taburete", "silla", "asiento", "respaldo"))
            {
                return PrefabBanca;
            }
            if (Tiene(n, "mesa", "barra", "mostrador") && largo >= 0.8f)
            {
                return PrefabMesa;
            }
            if (Tiene(n, "poste", "bolardo", "cordon", "basep", "columna", "pilastra"))
            {
                poste = true;
                return alto >= AlturaPoste ? PrefabPoste : PrefabBolardo;
            }
            if (esferas)
            {
                return null;
            }
            if (cilindros && alto >= 0.5f && alto <= 1.8f && largo <= 1.4f)
            {
                return PrefabTambo;
            }
            if (alto <= 2.2f && Mathf.Max(alto, largo) <= 2.5f)
            {
                return PrefabHuacales;
            }
            if (alto >= 0.6f && alto <= 2f && largo >= 2f && ancho <= 1.2f)
            {
                return PrefabBarrera;
            }
            return null;
        }

        private static void ColocarProp(Scene escena, Transform grupo, List<Item> miembros, Bounds g, Medida m, bool poste)
        {
            int giro = (m.Tam.x >= m.Tam.z) == (g.size.x >= g.size.z) ? 0 : 90;
            float tx = giro == 0 ? g.size.x : g.size.z;
            float tz = giro == 0 ? g.size.z : g.size.x;
            float sx = tx / Mathf.Max(0.01f, m.Tam.x);
            float sy = g.size.y / Mathf.Max(0.01f, m.Tam.y);
            float sz = tz / Mathf.Max(0.01f, m.Tam.z);
            Vector3 esc;
            if (poste)
            {
                float lado = Mathf.Clamp(Mathf.Min(sx, sz), sy * 0.5f, sy * 1.5f);
                esc = new Vector3(lado, sy, lado);
            }
            else
            {
                float u = Mathf.Min(sx, Mathf.Min(sy, sz));
                esc = new Vector3(Mathf.Clamp(sx, u, u * EstiramientoProp), Mathf.Clamp(sy, u, u * EstiramientoProp), Mathf.Clamp(sz, u, u * EstiramientoProp));
            }

            Transform padre = grupo.parent;
            GameObject modelo = padre != null
                ? (GameObject)PrefabUtility.InstantiatePrefab(m.Prefab, padre)
                : (GameObject)PrefabUtility.InstantiatePrefab(m.Prefab, escena);
            modelo.name = PrefijoProp + m.Prefab.name + "_" + grupo.name;
            Quaternion rot = Quaternion.Euler(0f, giro, 0f);
            Vector3 baseCentro = new Vector3(g.center.x, g.min.y, g.center.z);
            Vector3 mundo = baseCentro - rot * Vector3.Scale(m.Desfase, esc);
            modelo.transform.SetPositionAndRotation(mundo, rot);
            Vector3 ps = padre != null ? padre.lossyScale : Vector3.one;
            modelo.transform.localScale = new Vector3(esc.x / NoCero(ps.x), esc.y / NoCero(ps.y), esc.z / NoCero(ps.z));
            modelo.isStatic = true;

            bool tieneCollider = modelo.GetComponentInChildren<Collider>(true) != null || miembros.Any(i => i.Renderer.GetComponent<Collider>() != null);
            if (!tieneCollider)
            {
                BoxCollider bc = modelo.AddComponent<BoxCollider>();
                bc.size = m.Tam;
                bc.center = new Vector3(m.Desfase.x, m.Desfase.y + m.Tam.y * 0.5f, m.Desfase.z);
            }
            foreach (Item i in miembros)
            {
                i.Renderer.enabled = false;
            }
        }

        private static float NoCero(float v)
        {
            return Mathf.Abs(v) < 0.0001f ? 1f : v;
        }

        private static Medida Medir(string ruta)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
            if (prefab == null)
            {
                return null;
            }
            GameObject temporal = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            temporal.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            temporal.transform.localScale = Vector3.one;
            Renderer[] rs = temporal.GetComponentsInChildren<Renderer>(true).Where(x => x is MeshRenderer || x is SkinnedMeshRenderer).ToArray();
            Medida r = null;
            if (rs.Length > 0)
            {
                Bounds b = rs[0].bounds;
                foreach (Renderer x in rs)
                {
                    b.Encapsulate(x.bounds);
                }
                r = new Medida { Prefab = prefab, Tam = b.size, Desfase = new Vector3(b.center.x, b.min.y, b.center.z) };
            }
            Object.DestroyImmediate(temporal);
            return r;
        }

        // ================= Flotantes y cosas muy altas =================

        private static bool HuecoAlPiso(Transform propio, Bounds b, out float hueco)
        {
            hueco = float.MaxValue;
            Vector3 origen = new Vector3(b.center.x, b.min.y + 0.05f, b.center.z);
            RaycastHit[] golpes = Physics.RaycastAll(origen, Vector3.down, 400f, ~0, QueryTriggerInteraction.Ignore);
            float mejor = float.MaxValue;
            foreach (RaycastHit h in golpes)
            {
                if (h.collider.transform.IsChildOf(propio) || h.normal.y < 0.3f)
                {
                    continue;
                }
                if (h.distance < mejor)
                {
                    mejor = h.distance;
                }
            }
            if (mejor == float.MaxValue)
            {
                return false;
            }
            hueco = Mathf.Max(0f, mejor - 0.05f);
            return true;
        }

        private static List<Flotante> BuscarFlotantes(Scene escena, bool aplicar)
        {
            List<Flotante> salida = new List<Flotante>();
            List<MeshRenderer> todos = new List<MeshRenderer>();
            foreach (GameObject raiz in escena.GetRootGameObjects())
            {
                foreach (MeshRenderer mr in raiz.GetComponentsInChildren<MeshRenderer>(false))
                {
                    if (mr.enabled && mr.GetComponent<MeshFilter>() != null && mr.GetComponent<MeshFilter>().sharedMesh != null)
                    {
                        todos.Add(mr);
                    }
                }
            }

            // Rejilla en XZ para saber qué hay pegado a cada objeto.
            Dictionary<long, List<int>> rejilla = new Dictionary<long, List<int>>();
            List<int> grandes = new List<int>();
            for (int i = 0; i < todos.Count; i++)
            {
                Bounds b = todos[i].bounds;
                int x0 = Mathf.FloorToInt(b.min.x / CeldaRejilla);
                int x1 = Mathf.FloorToInt(b.max.x / CeldaRejilla);
                int z0 = Mathf.FloorToInt(b.min.z / CeldaRejilla);
                int z1 = Mathf.FloorToInt(b.max.z / CeldaRejilla);
                if ((long)(x1 - x0 + 1) * (z1 - z0 + 1) > CeldasMaximasRejilla)
                {
                    grandes.Add(i);
                    continue;
                }
                for (int x = x0; x <= x1; x++)
                {
                    for (int z = z0; z <= z1; z++)
                    {
                        long k = Llave(x, z);
                        List<int> l;
                        if (!rejilla.TryGetValue(k, out l))
                        {
                            l = new List<int>();
                            rejilla[k] = l;
                        }
                        l.Add(i);
                    }
                }
            }

            Dictionary<Transform, Bounds?> cacheCaja = new Dictionary<Transform, Bounds?>();
            HashSet<Transform> raices = new HashSet<Transform>();
            foreach (MeshRenderer mr in todos)
            {
                raices.Add(RaizDeItem(mr.transform, cacheCaja));
            }

            foreach (Transform raiz in raices)
            {
                string ruta = RutaDe(raiz);
                Bounds? cajaN = CajaDe(raiz, cacheCaja);
                if (cajaN == null || Tiene(ruta, ClavesSinFlotar) || raiz.GetComponentInChildren<Animator>() != null || raiz.GetComponentInChildren<Rigidbody>() != null)
                {
                    continue;
                }
                Bounds caja = cajaN.Value;
                if (Mathf.Max(caja.size.x, caja.size.z) > TamanoMaximoFlotante)
                {
                    continue;
                }
                if (Pegado(raiz, caja, todos, rejilla, grandes))
                {
                    continue;
                }
                float hueco;
                bool hayPiso = HuecoAlPiso(raiz, caja, out hueco);
                bool alto = caja.max.y > AlturaMaximaCielo && !Tiene(ruta, ClavesAltoPermitido);
                bool flota = caja.min.y > HuecoFlotante && (!hayPiso || hueco > HuecoFlotante);
                if (!alto && !flota)
                {
                    continue;
                }
                Flotante f = new Flotante { Raiz = raiz, Ruta = ruta, Caja = caja, Hueco = hayPiso ? hueco : -1f };
                f.Motivo = alto ? "sobre " + AlturaMaximaCielo + " m (" + caja.max.y.ToString("F0") + ")" : (hayPiso ? "flota " + hueco.ToString("F1") + " m sobre el piso" : "flota sin piso debajo (min y " + caja.min.y.ToString("F1") + ")");
                if (hayPiso && hueco > HuecoFlotante && hueco <= HuecoMaximoAsentar && !alto)
                {
                    f.Accion = "asentado";
                    if (aplicar)
                    {
                        raiz.position += Vector3.down * hueco;
                    }
                }
                else
                {
                    f.Accion = "apagado";
                    if (aplicar)
                    {
                        foreach (Renderer r in raiz.GetComponentsInChildren<Renderer>(false))
                        {
                            r.enabled = false;
                        }
                    }
                }
                if (!aplicar)
                {
                    f.Accion = "(previsto) " + f.Accion;
                }
                salida.Add(f);
            }
            if (aplicar)
            {
                Physics.SyncTransforms();
            }
            return salida;
        }

        private static long Llave(int x, int z)
        {
            return ((long)x << 32) ^ (uint)z;
        }

        private static bool Pegado(Transform raiz, Bounds caja, List<MeshRenderer> todos, Dictionary<long, List<int>> rejilla, List<int> grandes)
        {
            Bounds b = caja;
            b.Expand(MargenContacto * 2f);
            int x0 = Mathf.FloorToInt(b.min.x / CeldaRejilla);
            int x1 = Mathf.FloorToInt(b.max.x / CeldaRejilla);
            int z0 = Mathf.FloorToInt(b.min.z / CeldaRejilla);
            int z1 = Mathf.FloorToInt(b.max.z / CeldaRejilla);
            for (int x = x0; x <= x1; x++)
            {
                for (int z = z0; z <= z1; z++)
                {
                    List<int> l;
                    if (!rejilla.TryGetValue(Llave(x, z), out l))
                    {
                        continue;
                    }
                    foreach (int i in l)
                    {
                        MeshRenderer o = todos[i];
                        if (o != null && !o.transform.IsChildOf(raiz) && b.Intersects(o.bounds))
                        {
                            return true;
                        }
                    }
                }
            }
            foreach (int i in grandes)
            {
                MeshRenderer o = todos[i];
                if (o != null && !o.transform.IsChildOf(raiz) && b.Intersects(o.bounds))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>La raíz de un objeto "suelto": su prefab instanciado más externo, o el ancestro más alto que aún mide poco.</summary>
        private static Transform RaizDeItem(Transform t, Dictionary<Transform, Bounds?> cache)
        {
            GameObject prefab = PrefabUtility.GetOutermostPrefabInstanceRoot(t.gameObject);
            if (prefab != null)
            {
                return prefab.transform;
            }
            Transform g = t;
            while (g.parent != null && g.parent.parent != null)
            {
                Bounds? b = CajaDe(g.parent, cache);
                if (b == null || Mathf.Max(b.Value.size.x, Mathf.Max(b.Value.size.y, b.Value.size.z)) > TamanoMaximoRaizItem)
                {
                    break;
                }
                g = g.parent;
            }
            return g;
        }

        private static Bounds? CajaDe(Transform t, Dictionary<Transform, Bounds?> cache)
        {
            Bounds? b;
            if (cache.TryGetValue(t, out b))
            {
                return b;
            }
            b = null;
            Renderer[] rs = t.GetComponentsInChildren<Renderer>(false);
            foreach (Renderer r in rs)
            {
                if (!r.enabled)
                {
                    continue;
                }
                if (b == null)
                {
                    b = r.bounds;
                }
                else
                {
                    Bounds acumulado = b.Value;
                    acumulado.Encapsulate(r.bounds);
                    b = acumulado;
                }
            }
            cache[t] = b;
            return b;
        }

        // ================= Informes =================

        private static void EscribirAntesSiFalta(List<Item> items)
        {
            if (File.Exists(RutaAntes))
            {
                return;
            }
            StringBuilder sb = new StringBuilder();
            foreach (IGrouping<Cat, Item> g in items.GroupBy(i => i.Cat))
            {
                sb.AppendLine(g.Key + "\t" + g.Count());
            }
            sb.AppendLine("Total\t" + items.Count);
            ReemplazarEdificiosT1502.Escribir(RutaAntes, sb.ToString());
        }

        private static Dictionary<string, int> LeerAntes()
        {
            Dictionary<string, int> r = new Dictionary<string, int>();
            if (!File.Exists(RutaAntes))
            {
                return r;
            }
            foreach (string linea in File.ReadAllLines(RutaAntes))
            {
                string[] p = linea.Split('\t');
                int n;
                if (p.Length == 2 && int.TryParse(p[1], out n))
                {
                    r[p[0]] = n;
                }
            }
            return r;
        }

        /// <summary>Cuántas primitivas visibles hay en las otras escenas del build (solo cuenta; no cambia nada).</summary>
        private static Dictionary<string, int> ContarOtrasEscenas()
        {
            Dictionary<string, int> r = new Dictionary<string, int>();
            foreach (EditorBuildSettingsScene s in EditorBuildSettings.scenes)
            {
                if (!s.enabled || s.path == CreadorPrologoCiudad.RutaEscena || !File.Exists(s.path))
                {
                    continue;
                }
                Scene escena = EditorSceneManager.OpenScene(s.path, OpenSceneMode.Single);
                int n = 0;
                foreach (GameObject raiz in escena.GetRootGameObjects())
                {
                    foreach (MeshRenderer mr in raiz.GetComponentsInChildren<MeshRenderer>(false))
                    {
                        MeshFilter mf = mr.GetComponent<MeshFilter>();
                        if (mr.enabled && mf != null && mf.sharedMesh != null && EsIncorporada(AssetDatabase.GetAssetPath(mf.sharedMesh)))
                        {
                            n++;
                        }
                    }
                }
                r[s.path] = n;
            }
            return r;
        }

        private static string Cubeta(float lado)
        {
            if (lado < 0.5f) return "<0.5 m";
            if (lado < 2f) return "0.5-2 m";
            if (lado < 6f) return "2-6 m";
            if (lado < 15f) return "6-15 m";
            return ">15 m";
        }

        private static void Informar(List<Item> items, List<Flotante> flotantes, Dictionary<string, int> otras)
        {
            List<string> runtime = BuscarRuntime();
            StringBuilder log = new StringBuilder();
            log.AppendLine("T-1523: primitivas visibles en " + CreadorPrologoCiudad.RutaEscena + ": " + items.Count + " (por cambiar: " + Violaciones(items) + ")");
            log.AppendLine("categoria\tmalla\tbarrio\traiz\ttamano (m)\truta");
            foreach (Item i in items.OrderBy(x => x.Cat).ThenBy(x => x.Barrio).ThenBy(x => x.Ruta))
            {
                log.AppendLine(i.Cat + "\t" + i.Malla + "\t" + i.Barrio + "\t" + i.Raiz + "\t" + ReemplazarEdificiosT1502.Tam(i.Caja.size) + "\t" + i.Ruta);
            }
            log.AppendLine();
            log.AppendLine("Flotantes o muy altos: " + flotantes.Count);
            log.AppendLine("accion\tmotivo\ttamano (m)\truta");
            foreach (Flotante f in flotantes.OrderBy(x => x.Ruta))
            {
                log.AppendLine(f.Accion + "\t" + f.Motivo + "\t" + ReemplazarEdificiosT1502.Tam(f.Caja.size) + "\t" + f.Ruta);
            }
            log.AppendLine();
            log.AppendLine("Geometria creada por codigo de runtime (Assets/_Proyecto, sin Editor/Tests/Pruebas): " + runtime.Count);
            foreach (string l in runtime)
            {
                log.AppendLine(l);
            }
            ReemplazarEdificiosT1502.Escribir(RutaLog, log.ToString());

            Dictionary<string, int> antes = LeerAntes();
            StringBuilder md = new StringBuilder();
            md.AppendLine("# Primitivas de Unity en el build (T-1523)");
            md.AppendLine();
            md.AppendLine("Generado por `PrimitivasT1523.Diagnostico` (Mundo/Editor). Lista completa en `" + RutaLog + "`.");
            md.AppendLine();
            md.AppendLine("## Por que T-1501, T-1502 y T-1512 no se vieron en el build");
            md.AppendLine();
            md.AppendLine("- T-1502 y T-1512 solo cambiaban cubos llamados `Edificio` dentro de las afueras (`T530_Afueras`). `CalculoEdificiosT1502.Clasificar` devuelve `Ninguno` para cilindros, esferas, props y piezas chicas, y los cubos `Edificio` de barrio con kit, las torres, los muros y los locales con interior solo se anotaban. Ese era el 39 de \"39 cubos cambiados por ED\", y de esos T-1512 apago 17 por encimarse.");
            md.AppendLine("- En la escena la mayoria de lo \"basico\" que queda son decales planos (Quad: lineas de calle, charcos, ventanas con luz, pantallas), luces (focos, lentes), rotulos y piezas de interiores; los edificios de barrio son piezas del kit de fachadas (C20, C21, ... modelos reales pero cuadrados) y no cubos.");
            md.AppendLine("- Ninguna de las tareas miraba las otras escenas del build. `Inicio.unity` arma su fondo de ciudad por codigo con cajas (`UI/Inicio/FondoCiudad3D.cs`, malla `Inicio_Caja`): es el \"edificios de cubos\" que se ve en el menu. Ver la tabla de abajo.");
            md.AppendLine("- Los scripts de runtime con `CreatePrimitive` crean efectos, marcadores y pickups, no la ciudad (lista abajo); el maletin del asalto y los fajos de dinero si son formas basicas visibles.");
            md.AppendLine("- Los render de los cubos se apagan, no se borran: eso no revive en el build. Si algo reaparecio, es de otra categoria de esta lista.");
            md.AppendLine();
            md.AppendLine("## Antes / despues (Ciudad_Prologo, MeshRenderer visibles con malla incorporada o caja procedural)");
            md.AppendLine();
            md.AppendLine("\"Antes\" es la primera corrida de `Diagnostico` o `Aplicar` (`" + RutaAntes + "`; borrarlo reinicia la linea base). \"Despues\" es esta corrida.");
            md.AppendLine();
            md.AppendLine("| Categoria | Que se hace | Antes | Despues |");
            md.AppendLine("| --- | --- | --- | --- |");
            foreach (Cat c in System.Enum.GetValues(typeof(Cat)))
            {
                int a;
                string antesTxt = antes.TryGetValue(c.ToString(), out a) ? a.ToString() : "-";
                md.AppendLine("| " + c + " | " + QueSeHace(c) + " | " + antesTxt + " | " + items.Count(i => i.Cat == c) + " |");
            }
            int aTot;
            md.AppendLine("| **Total visibles** | | " + (antes.TryGetValue("Total", out aTot) ? aTot.ToString() : "-") + " | " + items.Count + " |");
            md.AppendLine("| **Por cambiar (Edificio + EdificioConKit + Prop)** | objetivo 0 | " + AntesPorCambiar(antes) + " | " + Violaciones(items) + " |");
            md.AppendLine("| Flotantes o muy altos | asentar o apagar | - | " + flotantes.Count + " |");
            md.AppendLine();
            md.AppendLine("## Por barrio y tamano (solo las que faltan por cambiar)");
            md.AppendLine();
            md.AppendLine("| Barrio | " + string.Join(" | ", new[] { "<0.5 m", "0.5-2 m", "2-6 m", "6-15 m", ">15 m" }) + " | Total |");
            md.AppendLine("| --- | --- | --- | --- | --- | --- | --- |");
            foreach (IGrouping<string, Item> g in items.Where(i => EsPorCambiar(i.Cat)).GroupBy(i => string.IsNullOrEmpty(i.Barrio) ? "(sin barrio)" : i.Barrio).OrderBy(g => g.Key))
            {
                md.Append("| " + g.Key);
                foreach (string cub in new[] { "<0.5 m", "0.5-2 m", "2-6 m", "6-15 m", ">15 m" })
                {
                    md.Append(" | " + g.Count(i => Cubeta(Mathf.Max(i.Caja.size.x, Mathf.Max(i.Caja.size.y, i.Caja.size.z))) == cub));
                }
                md.AppendLine(" | " + g.Count() + " |");
            }
            md.AppendLine();
            md.AppendLine("## Zonas (raiz de la jerarquia) con mas primitivas por cambiar");
            md.AppendLine();
            foreach (IGrouping<string, Item> g in items.Where(i => EsPorCambiar(i.Cat)).GroupBy(i => i.Raiz).OrderByDescending(g => g.Count()).Take(15))
            {
                md.AppendLine("- `" + g.Key + "`: " + g.Count());
            }
            md.AppendLine();
            md.AppendLine("## Otras escenas del build (solo cuenta; no se cambian)");
            md.AppendLine();
            foreach (KeyValuePair<string, int> par in otras.OrderBy(p => p.Key))
            {
                md.AppendLine("- `" + par.Key + "`: " + par.Value + " MeshRenderer visibles con malla incorporada");
            }
            md.AppendLine("- `Inicio.unity` ademas crea cajas por codigo al arrancar (`FondoCiudad3D`): ese fondo es una decision de arte del menu, no se toco.");
            md.AppendLine();
            md.AppendLine("## Geometria creada por codigo en runtime");
            md.AppendLine();
            md.AppendLine("Busqueda de `CreatePrimitive` y `new Mesh(` en `Assets/_Proyecto` sin `Editor/`, `Tests/` ni `Pruebas/`. Ninguna crea edificios de la ciudad; las que si se ven en juego como objeto solido son el maletin del asalto (`MaletinAsalto`, existe `Assets/Arte/Objetos/MA01_Maletin.prefab`), los fajos y municion en el piso (`DineroEnPiso`, `MunicionEnPiso`) y el fondo del menu.");
            md.AppendLine();
            foreach (string l in runtime)
            {
                md.AppendLine("- `" + l + "`");
            }
            md.AppendLine();
            md.AppendLine("## Flotantes o sobre " + AlturaMaximaCielo + " m (primeros 60)");
            md.AppendLine();
            foreach (Flotante f in flotantes.OrderBy(x => x.Ruta).Take(60))
            {
                md.AppendLine("- " + f.Accion + ": `" + f.Ruta + "` " + f.Motivo);
            }
            ReemplazarEdificiosT1502.Escribir(RutaDoc, md.ToString());
        }

        private static string AntesPorCambiar(Dictionary<string, int> antes)
        {
            if (antes.Count == 0)
            {
                return "-";
            }
            int a;
            int n = 0;
            foreach (Cat c in new[] { Cat.Edificio, Cat.EdificioConKit, Cat.Prop })
            {
                if (antes.TryGetValue(c.ToString(), out a))
                {
                    n += a;
                }
            }
            return n.ToString();
        }

        private static string QueSeHace(Cat c)
        {
            switch (c)
            {
                case Cat.Edificio: return "modelo ED/NV/P/LO/CC segun barrio";
                case Cat.EdificioConKit: return "se apaga el render si el kit lo cubre";
                case Cat.Prop: return "prop real equivalente o se apaga";
                case Cat.DecalPlano: return "se deja (linea, charco, ventana, pantalla)";
                case Cat.LuzRotulo: return "se deja (foco, lente, neon, rotulo)";
                case Cat.LosaPlana: return "se deja (piso, banqueta, calle)";
                case Cat.MuroPerimetral: return "T-1524";
                case Cat.MuroOtro: return "se anota (muro largo)";
                case Cat.EstructuraInterior: return "se deja (paredes de interiores y locales)";
                case Cat.PendienteArte: return "esperan TR01/TR02 (assets-faltan.md)";
                default: return "";
            }
        }

        private static List<string> BuscarRuntime()
        {
            List<string> r = new List<string>();
            if (!Directory.Exists("Assets/_Proyecto"))
            {
                return r;
            }
            foreach (string archivo in Directory.GetFiles("Assets/_Proyecto", "*.cs", SearchOption.AllDirectories).OrderBy(x => x))
            {
                string a = archivo.Replace('\\', '/');
                if (a.Contains("/Editor/") || a.Contains("/Tests/") || a.Contains("/Pruebas/"))
                {
                    continue;
                }
                string[] lineas = File.ReadAllLines(archivo);
                for (int i = 0; i < lineas.Length; i++)
                {
                    if (lineas[i].Contains("CreatePrimitive") || Regex.IsMatch(lineas[i], @"new Mesh\s*[\({]"))
                    {
                        r.Add(a + ":" + (i + 1) + " " + lineas[i].Trim());
                    }
                }
            }
            return r;
        }

        // ================= Utilidades =================

        private static string BarrioDe(Vector3 p, Dictionary<string, Vector3> centros)
        {
            string mejor = "";
            float d = float.MaxValue;
            foreach (KeyValuePair<string, Vector3> c in centros)
            {
                float dd = new Vector2(c.Value.x - p.x, c.Value.z - p.z).sqrMagnitude;
                if (dd < d)
                {
                    d = dd;
                    mejor = c.Key;
                }
            }
            return mejor;
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
    }
}
