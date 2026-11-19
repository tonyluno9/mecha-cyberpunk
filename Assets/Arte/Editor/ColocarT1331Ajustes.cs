using System.Collections.Generic;
using System.IO;
using System.Linq;
using MechaCyberpunk.Trafico.Persecucion;
using MechaCyberpunk.Vehiculos.Carro;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-1331 (playtest 17-nov, parte B): ajustes puntuales a Ciudad_Prologo, sin rehacer nada más. Idempotente.
    /// a) toldos que flotan (sin fachada detrás) o que quedan sobre una calle de la red del tráfico: se quitan;
    /// b) fachada y local de la tienda de Tino (CIC): materiales faltantes o rosas se cambian por el ladrillo del kit y los planos encimados (z-fighting) se desactivan;
    /// c) estelas raras: TrailRenderer, líneas que no son cables y partículas con rastro o muy arriba (sobre los techos) se quitan;
    /// d) carros estacionados sobre barriles, puestos u otros objetos: se recorren a lo largo de su eje hasta un lugar libre (o se apagan si no hay);
    /// e) postes con más intensidad, alcance y cono (<see cref="ColocarS2oLuz"/>).
    /// Orden: <see cref="Aplicar"/> y luego <see cref="Fotos"/> (no guarda). Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ColocarT1331Ajustes.Aplicar
    /// </summary>
    public static class ColocarT1331Ajustes
    {
        /// <summary>Distancia (m) a la que se busca una fachada detrás de un toldo.</summary>
        public const float AlcancePared = 0.5f;
        /// <summary>Un toldo con algún punto a menos de esto (m) del eje de una calle del tráfico invade el carril.</summary>
        public const float RadioCarril = 4.5f;
        /// <summary>Lo que se busca como pared tiene al menos este alto (m): un barril o un poste no sostienen un toldo.</summary>
        public const float AltoPared = 2f;
        public const float BuscarCarroHasta = 15f;
        public const float PasoCarro = 0.5f;

        private static readonly string[] SaltarRaicesCarro = { "JugadorAPie", "MechaEstacionado" };

        // ================================================================ Aplicar

        public static void Aplicar()
        {
            ColocarS2.Informe.Clear();
            ColocarS2.Abrir();
            QuitarToldos();
            QuitarCalleComercial();
            Tino();
            Estelas();
            Carros();
            ColocarT1317Ajustes.Postes(ColocarS2oLuz.IntensidadPoste);
            ColocarS2.Guardar("t1331-ajustes");
        }

        // ================================================================ 1. Toldos

        /// <summary>Raíz de un toldo: el del kit en viviendas ("Toldo"), el de T-1320 ("TL01_Toldo"), los de T-501 y T-508 ("Toldo_...").</summary>
        public static bool EsToldo(Transform t)
        {
            return t.name == "Toldo" || t.name == "TL01_Toldo" || t.name.StartsWith("Toldo_");
        }

        /// <summary>Los toldos activos (solo la raíz de cada uno).</summary>
        public static List<Transform> Toldos(bool soloActivos = true)
        {
            List<Transform> todos = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(EsToldo).ToList();
            return todos.Where(t => (!soloActivos || t.gameObject.activeInHierarchy) && !TieneAncestro(t, todos)).ToList();
        }

        private static bool TieneAncestro(Transform t, List<Transform> conjunto)
        {
            for (Transform p = t.parent; p != null; p = p.parent)
            {
                if (conjunto.Contains(p))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Los toldos activos que flotan (ninguna fachada de al menos <see cref="AltoPared"/> m a menos de <see cref="AlcancePared"/> m a los lados) o que invaden una calle
        /// del tráfico (alguna esquina de su base a menos de <see cref="RadioCarril"/> m del eje). Devuelve (toldo, motivo).
        /// </summary>
        public static List<(Transform toldo, string motivo)> ToldosMalos()
        {
            List<(Transform, string)> malos = new List<(Transform, string)>();
            List<Transform> toldos = Toldos();
            GrafoCalles grafo = Grafo();
            List<MeshCollider> temporales = new List<MeshCollider>();
            try
            {
                foreach (MeshFilter mf in Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                {
                    Renderer r = mf.GetComponent<Renderer>();
                    if (mf.sharedMesh != null && UltimasFallasT1412.MallaCocinable(mf.sharedMesh) && r != null && r.enabled && mf.GetComponent<Collider>() == null)
                    {
                        MeshCollider mc = mf.gameObject.AddComponent<MeshCollider>();
                        mc.sharedMesh = mf.sharedMesh;
                        temporales.Add(mc);
                    }
                }
                Physics.SyncTransforms();
                foreach (Transform toldo in toldos)
                {
                    Renderer[] rs = toldo.GetComponentsInChildren<Renderer>().Where(r => r.enabled).ToArray();
                    if (rs.Length == 0)
                    {
                        continue;
                    }
                    Bounds b = rs[0].bounds;
                    foreach (Renderer r in rs)
                    {
                        b.Encapsulate(r.bounds);
                    }
                    if (grafo != null && InvadeCarril(grafo, b))
                    {
                        malos.Add((toldo, "sobre la calle"));
                    }
                    else if (!HayFachada(toldo, b))
                    {
                        malos.Add((toldo, "sin fachada"));
                    }
                }
            }
            finally
            {
                foreach (MeshCollider mc in temporales)
                {
                    Object.DestroyImmediate(mc);
                }
            }
            return malos;
        }

        /// <summary>Revisión (Opus): en la calle comercial K10 (T-1320), los toldos TL01 con las patas en el aire y los puestos con techito parados en el arroyo.</summary>
        public static List<Transform> SobrantesK10()
        {
            Transform k10 = GameObject.Find("T1320_Calles")?.transform.Find("CalleComercial_K10");
            if (k10 == null)
            {
                return new List<Transform>();
            }
            string[] prefijos = { "TL01_Toldo", "C12_PuestoTacos", "C13_PuestoCiber", "CY09_PuestoImplantes", "C17_Huacales" };
            return k10.Cast<Transform>().Where(t => prefijos.Any(p => t.name.StartsWith(p))).ToList();
        }

        private static void QuitarCalleComercial()
        {
            HashSet<Object> referidos = ColocarS2.Referidos();
            List<Transform> sobran = SobrantesK10();
            foreach (Transform t in sobran)
            {
                ColocarS2.Informe.AppendLine("K10: quitado " + t.name + " en " + t.position.ToString("F1"));
                ColocarS2.Quitar(t.gameObject, referidos);
            }
            ColocarS2.Informe.AppendLine("K10: toldos y puestos de la calle quitados: " + sobran.Count);
        }

        private static GrafoCalles Grafo()
        {
            GameObject barrio = GameObject.Find("Barrio");
            return barrio != null ? GestorPersecucion.ArmarCallesDe(barrio.transform) : null;
        }

        private static bool InvadeCarril(GrafoCalles grafo, Bounds b)
        {
            foreach (Vector3 q in new[] { b.center, new Vector3(b.min.x, 0f, b.min.z), new Vector3(b.min.x, 0f, b.max.z), new Vector3(b.max.x, 0f, b.min.z), new Vector3(b.max.x, 0f, b.max.z) })
            {
                if (grafo.DistanciaALaCalle(new Vector3(q.x, 0.2f, q.z)) < RadioCarril)
                {
                    return true;
                }
            }
            return false;
        }

        private static bool HayFachada(Transform toldo, Bounds b)
        {
            HashSet<Collider> propios = new HashSet<Collider>(toldo.GetComponentsInChildren<Collider>(true));
            foreach (float y in new[] { b.center.y, b.max.y - 0.05f })
            {
                for (int k = 0; k < 8; k++)
                {
                    Vector3 dir = Quaternion.Euler(0f, k * 45f, 0f) * Vector3.forward;
                    float alcance = Mathf.Abs(Vector3.Dot(b.extents, new Vector3(Mathf.Abs(dir.x), 0f, Mathf.Abs(dir.z)))) + AlcancePared;
                    Vector3 o = new Vector3(b.center.x, y, b.center.z);
                    foreach (RaycastHit h in Physics.RaycastAll(o, dir, alcance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                    {
                        if (!propios.Contains(h.collider) && !EsToldo(h.collider.transform) && h.collider.bounds.size.y >= AltoPared && Mathf.Abs(h.normal.y) < 0.5f
                            && !h.collider.transform.IsChildOf(toldo))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        private static void QuitarToldos()
        {
            HashSet<Object> referidos = ColocarS2.Referidos();
            List<(Transform toldo, string motivo)> malos = ToldosMalos();
            foreach ((Transform toldo, string motivo) in malos)
            {
                ColocarS2.Informe.AppendLine("Toldo " + motivo + ": " + ColocarS2.Ruta(toldo) + " en " + toldo.position.ToString("F1"));
                ColocarS2.Quitar(toldo.gameObject, referidos);
            }
            ColocarS2.Informe.AppendLine("Toldos quitados: " + malos.Count + " de " + (malos.Count + ToldosBuenos()));
        }

        private static int ToldosBuenos() => Toldos().Count;

        // ================================================================ 2. Tienda de Tino (fachada del CIC)

        /// <summary>Zona de la fachada y el local CIC de Tino (T-531: x -77.35 a -69.25, frente en z 10 hacia la calle -Z).</summary>
        public static readonly Bounds ZonaTino = new Bounds(new Vector3(-73.3f, 2.2f, 10.5f), new Vector3(11f, 5.2f, 9f));
        private const string RutaLadrillo = "Assets/Arte/Interiores/Materiales/S2_Pared_X03.mat";

        public static bool MaterialRoto(Material m)
        {
            if (m == null || m.shader == null)
            {
                return true;
            }
            if (m.shader.name.Contains("InternalError") || !m.shader.isSupported)
            {
                return true;
            }
            if (m.HasProperty("_BaseColor"))
            {
                Color c = m.GetColor("_BaseColor");
                if (m.GetTexture("_BaseMap") == null && c.r > 0.9f && c.g < 0.1f && c.b > 0.9f)
                {
                    return true; // rosa de "falta la textura"
                }
            }
            return false;
        }

        private static Material Ladrillo()
        {
            return AssetDatabase.LoadAssetAtPath<Material>(RutaLadrillo);
        }

        /// <summary>Renderers activos de la zona de Tino.</summary>
        public static List<Renderer> RenderersTino()
        {
            return Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .Where(r => r.enabled && r.gameObject.activeInHierarchy && !(r is ParticleSystemRenderer) && !(r is LineRenderer) && r.bounds.Intersects(ZonaTino)).ToList();
        }

        /// <summary>Renderers de planos delgados que se encimarían en el mismo plano (z-fighting): el menor de cada par. Misma normal, planos a menos de 3 cm y encimados.</summary>
        public static List<Renderer> PlanosEncimados(List<Renderer> renderers)
        {
            List<(Renderer r, int eje, float plano, Bounds b)> planos = new List<(Renderer, int, float, Bounds)>();
            foreach (Renderer r in renderers)
            {
                if (!(r is MeshRenderer) || r.GetComponent<MeshFilter>() == null)
                {
                    continue;
                }
                Bounds b = r.bounds;
                int eje = b.size.x <= b.size.y && b.size.x <= b.size.z ? 0 : b.size.y <= b.size.z ? 1 : 2;
                if (b.size[eje] > 0.06f || b.size.magnitude < 0.5f)
                {
                    continue;
                }
                planos.Add((r, eje, b.center[eje], b));
            }
            List<Renderer> perdedores = new List<Renderer>();
            for (int i = 0; i < planos.Count; i++)
            {
                for (int j = i + 1; j < planos.Count; j++)
                {
                    var a = planos[i];
                    var c = planos[j];
                    if (a.eje != c.eje || Mathf.Abs(a.plano - c.plano) > 0.03f || perdedores.Contains(a.r) || perdedores.Contains(c.r))
                    {
                        continue;
                    }
                    Vector3 min = Vector3.Max(a.b.min, c.b.min);
                    Vector3 max = Vector3.Min(a.b.max, c.b.max);
                    float area = 1f;
                    float areaMenor = 1f;
                    for (int ax = 0; ax < 3; ax++)
                    {
                        if (ax == a.eje)
                        {
                            continue;
                        }
                        area *= Mathf.Max(0f, max[ax] - min[ax]);
                        areaMenor *= Mathf.Min(a.b.size[ax], c.b.size[ax]);
                    }
                    if (areaMenor > 0.01f && area > 0.5f * areaMenor)
                    {
                        float areaA = 1f, areaC = 1f;
                        for (int ax = 0; ax < 3; ax++)
                        {
                            if (ax != a.eje)
                            {
                                areaA *= a.b.size[ax];
                                areaC *= c.b.size[ax];
                            }
                        }
                        perdedores.Add(areaA <= areaC ? a.r : c.r);
                    }
                }
            }
            return perdedores;
        }

        private static void Tino()
        {
            Material ladrillo = Ladrillo();
            int cambiados = 0;
            List<Renderer> renderers = RenderersTino();
            foreach (Renderer r in renderers)
            {
                Material[] mats = r.sharedMaterials;
                bool cambio = false;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (MaterialRoto(mats[i]) && ladrillo != null)
                    {
                        ColocarS2.Informe.AppendLine("  Tino: material roto en " + ColocarS2.Ruta(r.transform) + " [" + i + "] -> S2_Pared_X03");
                        mats[i] = ladrillo;
                        cambio = true;
                        cambiados++;
                    }
                }
                if (cambio)
                {
                    r.sharedMaterials = mats;
                    EditorUtility.SetDirty(r);
                }
            }
            int apagados = 0;
            foreach (Renderer r in PlanosEncimados(renderers))
            {
                r.enabled = false;
                EditorUtility.SetDirty(r);
                apagados++;
                ColocarS2.Informe.AppendLine("  Tino: plano encimado apagado " + ColocarS2.Ruta(r.transform));
            }
            // Revisión (Opus): lo "roto por fuera" eran los paños *_Fuera del forro (ladrillo X03 muy ampliado) y la pilastra y el dintel de T531
            // (textura de ventanas estirada en una caja). Llevan el muro liso de la fachada de al lado.
            Material liso = MaterialFachadaLisa();
            int fachada = 0;
            if (liso != null)
            {
                foreach (Renderer r in renderers.Where(EsFrenteTino))
                {
                    if (r.sharedMaterial != liso)
                    {
                        r.sharedMaterial = liso;
                        EditorUtility.SetDirty(r);
                    }
                    fachada++;
                }
            }
            ColocarS2.Informe.AppendLine("Tino: " + renderers.Count + " renderers revisados, " + cambiados + " materiales cambiados, " + apagados + " planos encimados apagados, "
                + fachada + " paños del frente con " + NombreFachadaLisa);
        }

        public const string NombreFachadaLisa = "T507_C21_PisoLiso_cy_0";

        /// <summary>Paños del frente del CIC vistos desde la calle: forro *_Fuera de TallerTino, pilastras y dintel de T531_Locales/CIC.</summary>
        public static bool EsFrenteTino(Renderer r)
        {
            string ruta = ColocarS2.Ruta(r.transform);
            return (ruta.StartsWith("S2_Interiores/TallerTino/Forro/") && r.name.EndsWith("_Fuera"))
                || (ruta.StartsWith("T531_Locales/CIC/") && (r.name == "Pilastra" || r.name == "Dintel"));
        }

        public static Material MaterialFachadaLisa()
        {
            return Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Select(x => x.sharedMaterial).FirstOrDefault(m => m != null && m.name == NombreFachadaLisa);
        }

        // ================================================================ 3. Estelas

        /// <summary>Altura (m) desde la que una partícula ya es "del techo" y no de la calle.</summary>
        public const float AlturaTecho = 11f;

        /// <summary>Lo que puede verse como una estela rara: TrailRenderer, líneas que no son cables ni series de focos, partículas con rastro o estiradas, y vapor en los techos.</summary>
        public static List<(GameObject objeto, string motivo)> EstelasRaras()
        {
            List<(GameObject, string)> lista = new List<(GameObject, string)>();
            foreach (TrailRenderer t in Object.FindObjectsByType<TrailRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                lista.Add((t.gameObject, "TrailRenderer"));
            }
            // Revisión (Opus): las LineRenderer de la escena son grietas de la cabina del MechaEstacionado y cables: no se tocan.
            foreach (ParticleSystem ps in Object.FindObjectsByType<ParticleSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (ps.transform.root.name == "JugadorAPie" || ps.GetComponentInParent<MechaCyberpunk.Mundo.Ambiente.LluviaSigueJugador>() != null)
                {
                    continue;
                }
                ParticleSystemRenderer pr = ps.GetComponent<ParticleSystemRenderer>();
                if (ps.trails.enabled || (pr != null && pr.renderMode == ParticleSystemRenderMode.Stretch))
                {
                    lista.Add((ps.gameObject, "partículas con rastro"));
                }
                else if (ps.transform.position.y > AlturaTecho && ps.gameObject.name.StartsWith("Vapor"))
                {
                    lista.Add((ps.gameObject, "vapor en el techo"));
                }
            }
            return lista;
        }

        private static void Estelas()
        {
            HashSet<Object> referidos = ColocarS2.Referidos();
            List<(GameObject objeto, string motivo)> lista = EstelasRaras();
            foreach ((GameObject objeto, string motivo) in lista)
            {
                ColocarS2.Informe.AppendLine("Estela rara (" + motivo + "): " + ColocarS2.Ruta(objeto.transform) + " en " + objeto.transform.position.ToString("F1"));
                ColocarS2.Quitar(objeto, referidos);
            }
            ColocarS2.Informe.AppendLine("Estelas raras quitadas: " + lista.Count);
        }

        // ================================================================ 4. Carros sobre objetos

        /// <summary>Los carros y motos de la ciudad que no son del jugador ni de tráfico en vivo (los que aparecen del tráfico ya miran el espacio libre en <c>EspacioLibre</c>).</summary>
        public static List<CarroViejo> CarrosDeLaCiudad()
        {
            return Object.FindObjectsByType<CarroViejo>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .Where(c => c.gameObject.activeInHierarchy && !SaltarRaicesCarro.Contains(c.transform.root.name) && c.GetComponent<MechaCyberpunk.Trafico.Calle.CarroTrafico>() == null).ToList();
        }

        private static bool Caja(Transform carro, out Vector3 centro, out Vector3 mitad, out float minY)
        {
            centro = Vector3.zero;
            mitad = Vector3.zero;
            minY = 0f;
            Collider[] cs = carro.GetComponentsInChildren<Collider>(false).Where(c => c.enabled && !c.isTrigger).ToArray();
            if (cs.Length == 0)
            {
                return false;
            }
            Matrix4x4 aLocal = carro.worldToLocalMatrix;
            Bounds local = default;
            bool primero = true;
            minY = float.MaxValue;
            foreach (Collider c in cs)
            {
                Bounds w = c.bounds;
                minY = Mathf.Min(minY, w.min.y);
                for (int i = 0; i < 8; i++)
                {
                    Vector3 esquina = w.center + Vector3.Scale(w.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                    Vector3 p = aLocal.MultiplyPoint3x4(esquina);
                    if (primero)
                    {
                        local = new Bounds(p, Vector3.zero);
                        primero = false;
                    }
                    else
                    {
                        local.Encapsulate(p);
                    }
                }
            }
            centro = carro.TransformPoint(local.center);
            Vector3 e = local.extents;
            Vector3 s = carro.lossyScale;
            mitad = new Vector3(e.x * Mathf.Abs(s.x), e.y * Mathf.Abs(s.y), e.z * Mathf.Abs(s.z));
            return true;
        }

        /// <summary>Colliders sólidos (que no son del carro) dentro de la caja del carro, sin contar el piso ni la banqueta: los primeros 0.35 m no cuentan.</summary>
        public static List<Collider> Estorbos(Transform carro, Vector3 desplazamiento = default)
        {
            List<Collider> lista = new List<Collider>();
            if (!Caja(carro, out Vector3 centro, out Vector3 mitad, out float minY))
            {
                return lista;
            }
            float piso = 0.35f;
            float alto = Mathf.Max(0.1f, centro.y + mitad.y - (minY + piso));
            Vector3 c = new Vector3(centro.x, minY + piso + alto * 0.5f, centro.z) + desplazamiento;
            Vector3 m = new Vector3(Mathf.Max(0.05f, mitad.x - 0.1f), alto * 0.5f, Mathf.Max(0.05f, mitad.z - 0.1f));
            foreach (Collider h in Physics.OverlapBox(c, m, carro.rotation, ~0, QueryTriggerInteraction.Ignore))
            {
                if (h.enabled && !h.transform.IsChildOf(carro) && !(h.bounds.size.y < 0.3f && h.bounds.max.y < minY + piso))
                {
                    lista.Add(h);
                }
            }
            return lista;
        }

        private static float PisoBajoCarro(Transform carro, Vector3 desplazamiento)
        {
            Vector3 o = carro.position + desplazamiento + Vector3.up * 2f;
            foreach (RaycastHit h in Physics.RaycastAll(o, Vector3.down, 6f, ~0, QueryTriggerInteraction.Ignore).OrderBy(x => x.distance))
            {
                if (!h.collider.transform.IsChildOf(carro))
                {
                    return h.point.y;
                }
            }
            return float.NaN;
        }

        private static void Carros()
        {
            Physics.SyncTransforms();
            int movidos = 0, apagados = 0, bien = 0;
            foreach (CarroViejo carro in CarrosDeLaCiudad())
            {
                Transform t = carro.transform;
                List<Collider> estorbos = Estorbos(t);
                if (estorbos.Count == 0)
                {
                    bien++;
                    continue;
                }
                string quien = string.Join(", ", estorbos.Select(e => e.transform.root.name + "/" + e.name).Distinct().Take(4));
                float pisoOriginal = PisoBajoCarro(t, Vector3.zero);
                Vector3 mejor = Vector3.zero;
                bool hallado = false;
                for (float d = PasoCarro; d <= BuscarCarroHasta && !hallado; d += PasoCarro)
                {
                    foreach (Vector3 eje in new[] { t.forward, -t.forward })
                    {
                        Vector3 desp = eje * d;
                        float piso = PisoBajoCarro(t, desp);
                        if (!float.IsNaN(pisoOriginal) && (float.IsNaN(piso) || Mathf.Abs(piso - pisoOriginal) > 0.25f))
                        {
                            continue;
                        }
                        if (Estorbos(t, desp).Count == 0)
                        {
                            mejor = desp;
                            hallado = true;
                            break;
                        }
                    }
                }
                if (hallado)
                {
                    t.position += mejor;
                    Physics.SyncTransforms();
                    movidos++;
                    ColocarS2.Informe.AppendLine("Carro " + ColocarS2.Ruta(t) + " estaba sobre " + quien + "; se movió " + mejor.magnitude.ToString("F1") + " m a " + t.position.ToString("F1"));
                }
                else
                {
                    t.gameObject.SetActive(false);
                    apagados++;
                    ColocarS2.Informe.AppendLine("Carro " + ColocarS2.Ruta(t) + " estaba sobre " + quien + " y no hay lugar libre cerca: apagado");
                }
            }
            ColocarS2.Informe.AppendLine("Carros: " + bien + " bien, " + movidos + " movidos, " + apagados + " apagados");
        }

        // ================================================================ Fotos

        /// <summary>Capturas de T-1331 (no guarda la escena): la tienda de Tino por fuera y la calle de noche.</summary>
        public static void Fotos()
        {
            ColocarS2.Abrir();
            Directory.CreateDirectory("docs/capturas/t-1331");
            ColocarS2cLook.Foto(new Vector3(-73.3f, 1.7f, 2f), new Vector3(-73.3f, 1.8f, 10f), "docs/capturas/t-1331/tino-frente.png", 60f);
            ColocarS2cLook.Foto(new Vector3(-84f, 2.5f, 3f), new Vector3(-72f, 1.8f, 10.5f), "docs/capturas/t-1331/tino-diagonal.png", 60f);
            Vector3 poste = Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(ColocarS2oLuz.EsPoste)
                .First(l => Mathf.Abs(l.transform.position.x + 52.88f) < 0.5f).transform.position;
            ColocarS2cLook.Foto(poste + new Vector3(4f, -4.7f, 4f), poste + new Vector3(0f, -6.4f, 0f), "docs/capturas/t-1331/calle-postes.png", 80f);
        }
    }
}
