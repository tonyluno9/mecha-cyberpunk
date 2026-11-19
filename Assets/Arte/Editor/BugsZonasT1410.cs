using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MechaCyberpunk.Mundo.Ambiente;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-1410: arregla los bugs reales del mapa que dejó la noche del 19-nov (docs/fallas-editmode-19nov.md). Todo es idempotente y cada arreglo va en su try/catch.
    /// Aplicar (Logs/t1410-aplicar.txt) corre, en este orden:
    /// 1) Puerto: vuelve a correr ZonaPuertoT1402.Aplicar (que ya no apaga contenedores: corre las pilas PU08 a un hueco libre) y asegura collider en tarimas, huacales, tambos y montacargas.
    /// 2) Barriles: todo tambo/barril de las zonas con collider y BarrilExplosivo.
    /// 3) Greybox: mallas con material liso (Basura_* del Puerto, etc.) reciben el .mat con textura de la carpeta de su modelo.
    /// 4) Semáforo de la Autopista (K18): sin SemaforoCiclo (un ES01 dentro de un prefab no pasa PiezasCalleT910).
    /// 5) Cerro: barrido de T-1360 en bucle; props sin collider reciben una caja, carros en el carril se apagan.
    /// 6) Pisos de respaldo de las uniones (escena): su cara superior queda a 0.01 m bajo la calle (LineasS2n).
    /// 7) Escena: toldos sin fachada o sobre la calle, carros sobre objetos, piezas flotando y piezas encimadas; el cambio se aplica al prefab de la zona
    ///    (si el objeto no es de un prefab de Assets/Arte/Mundo/, al objeto de la escena, que se guarda).
    /// Métodos sueltos: AplicarPuerto, AplicarBarriles, AplicarGreybox, AplicarSemaforo, AplicarCerro, AplicarRespaldo, AplicarEscena.
    /// Correr con Unity cerrado: -executeMethod MechaCyberpunk.Arte.Editor.BugsZonasT1410.Aplicar
    /// </summary>
    public static class BugsZonasT1410
    {
        public const string CarpetaZonas = "Assets/Arte/Mundo/Zonas";
        private const string RutaPuerto = CarpetaZonas + "/T1357_Puerto.prefab";
        private const string RutaAutopista = CarpetaZonas + "/T1356_Autopista.prefab";
        private const string RutaCerro = CarpetaZonas + "/T1355_Cerro.prefab";
        private const string PrefijoMundo = "Assets/Arte/Mundo/";
        private const int MaxPasadasEscena = 5;
        private const int MaxPasadasCerro = 8;
        private const float BuscarCarroHasta = 6f;
        private const float PasoCarro = 0.5f;

        private static readonly StringBuilder informe = new StringBuilder();
        private static readonly string[] PrefijosDetalle = { "PU06_Pallets", "C17_Huacales", "C16_Tambo", "PU05_Montacargas" };
        private static readonly string[] MaterialesDecal = { "Charco", "Publico", "Grafiti", "Tragamonedas", "Madera_Oscura", "Metal_Oscuro" };
        private static readonly string[] NombresSinCaja = { "Cable", "Halo", "Luz", "Neon", "Charco", "Linea", "Reflector", "VentanaCalida", "FocoPared", "Hueco" };

        // ---------------------------------------------------------------- entrada

        private static void Nota(string t)
        {
            informe.AppendLine(t);
        }

        private static void Correr(string nombre, Action accion)
        {
            Nota("== " + nombre);
            try
            {
                accion();
            }
            catch (Exception e)
            {
                Nota("FALLO en " + nombre + ": " + e);
                Debug.LogError("[BugsZonasT1410] " + nombre + ": " + e);
            }
        }

        private static void Sesion(Action cuerpo)
        {
            informe.Clear();
            Nota("T-1410 bugs de las zonas");
            try
            {
                cuerpo();
            }
            finally
            {
                Directory.CreateDirectory("Logs");
                File.WriteAllText("Logs/t1410-aplicar.txt", informe.ToString());
                Debug.Log("[BugsZonasT1410] " + informe);
            }
        }

        [MenuItem("Mecha Cyberpunk/T-1410/Bugs de las zonas (aplicar todo)")]
        public static void Aplicar()
        {
            Sesion(() =>
            {
                Correr("1 Puerto (T-1402 sin apagar contenedores, colliders de detalles)", Puerto);
                Correr("2 Barriles con collider y BarrilExplosivo", Barriles);
                Correr("3 Mallas con material liso", Greybox);
                Correr("4 Semáforo de la Autopista", Semaforo);
                Correr("5 Cerro: barrido sin collider y carros en el carril", Cerro);
                Correr("6 Pisos de respaldo de las uniones", Respaldo);
                Correr("7 Escena: toldos, carros, flotantes y encimados", Escena);
            });
        }

        public static void AplicarPuerto() { Sesion(() => Correr("Puerto", Puerto)); }

        public static void AplicarBarriles() { Sesion(() => Correr("Barriles", Barriles)); }

        public static void AplicarGreybox() { Sesion(() => Correr("Greybox", Greybox)); }

        public static void AplicarSemaforo() { Sesion(() => Correr("Semáforo", Semaforo)); }

        public static void AplicarCerro() { Sesion(() => Correr("Cerro", Cerro)); }

        public static void AplicarRespaldo() { Sesion(() => Correr("Respaldo", Respaldo)); }

        public static void AplicarEscena() { Sesion(() => Correr("Escena", Escena)); }

        // ---------------------------------------------------------------- ayudantes de prefab

        private static string[] Zonas()
        {
            return Directory.GetFiles(CarpetaZonas, "*.prefab").Select(f => f.Replace('\\', '/')).OrderBy(f => f).ToArray();
        }

        /// <summary>Abre el prefab, corre la edición (devuelve cuántos cambios hizo) y lo guarda solo si hubo cambios.</summary>
        private static int EditarPrefab(string ruta, Func<GameObject, int> editar)
        {
            GameObject raiz = PrefabUtility.LoadPrefabContents(ruta);
            int n = 0;
            try
            {
                n = editar(raiz);
                if (n > 0)
                {
                    PrefabUtility.SaveAsPrefabAsset(raiz, ruta);
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
            return n;
        }

        /// <summary>Una BoxCollider en la raíz de la pieza con la caja de las mallas que se ven (en espacio local de la pieza). False si no hay mallas.</summary>
        private static bool PonerCajaLocal(Transform pieza)
        {
            bool primero = true;
            Bounds caja = default;
            foreach (MeshFilter mf in pieza.GetComponentsInChildren<MeshFilter>(false))
            {
                MeshRenderer mr = mf.GetComponent<MeshRenderer>();
                if (mf.sharedMesh == null || mr == null || !mr.enabled || NombresSinCaja.Any(n => mf.name.StartsWith(n)) || mr.bounds.size.magnitude < 0.15f)
                {
                    continue;
                }
                Matrix4x4 m = pieza.worldToLocalMatrix * mf.transform.localToWorldMatrix;
                Bounds mb = mf.sharedMesh.bounds;
                for (int i = 0; i < 8; i++)
                {
                    Vector3 esq = mb.center + Vector3.Scale(mb.extents, new Vector3((i & 1) == 0 ? -1f : 1f, (i & 2) == 0 ? -1f : 1f, (i & 4) == 0 ? -1f : 1f));
                    Vector3 p = m.MultiplyPoint3x4(esq);
                    if (primero)
                    {
                        caja = new Bounds(p, Vector3.zero);
                        primero = false;
                    }
                    else
                    {
                        caja.Encapsulate(p);
                    }
                }
            }
            if (primero)
            {
                return false;
            }
            BoxCollider bc = pieza.gameObject.AddComponent<BoxCollider>();
            bc.center = caja.center;
            bc.size = caja.size;
            return true;
        }

        /// <summary>
        /// Deja la pieza con al menos un collider encendido (enciende los apagados y, si no hay ninguno, le pone una caja) y, si es un tambo o barril de ciudad,
        /// con BarrilExplosivo. Con <paramref name="soloSiFalta"/> no enciende los apagados (solo exige que exista alguno). Devuelve true si cambió algo.
        /// </summary>
        public static bool AsegurarColliderYBarril(GameObject go, bool soloSiFalta = false)
        {
            bool cambio = false;
            Collider[] cs = go.GetComponentsInChildren<Collider>(true);
            if (soloSiFalta)
            {
                if (cs.Length == 0 && PonerCajaLocal(go.transform))
                {
                    cambio = true;
                }
            }
            else if (!cs.Any(c => c.enabled && !c.isTrigger))
            {
                foreach (Collider c in cs)
                {
                    if (!c.isTrigger && !c.enabled)
                    {
                        c.enabled = true;
                        cambio = true;
                    }
                }
                if (!go.GetComponentsInChildren<Collider>(true).Any(c => c.enabled && !c.isTrigger) && PonerCajaLocal(go.transform))
                {
                    cambio = true;
                }
            }
            if (ColocarT1317Ajustes.EsBarrilDeCiudad(go.transform)
                && go.GetComponent<MechaCyberpunk.Vehiculos.Efectos.BarrilExplosivo>() == null && go.GetComponentInParent<MechaCyberpunk.Vehiculos.Efectos.BarrilExplosivo>() == null)
            {
                go.AddComponent<MechaCyberpunk.Vehiculos.Efectos.BarrilExplosivo>();
                cambio = true;
            }
            return cambio;
        }

        // ---------------------------------------------------------------- 1. Puerto

        private static void Puerto()
        {
            // ZonaPuertoT1402.Aplicar restaura los contenedores que apagó una corrida vieja (RestaurarContenedores) y ya no apaga ninguno.
            ZonaPuertoT1402.Aplicar();
            Nota("ZonaPuertoT1402.Aplicar vuelto a correr (ver Logs/t1402-aplicar.txt: pilas corridas u omitidas).");
            int n = EditarPrefab(RutaPuerto, raiz =>
            {
                int cambios = 0;
                foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true))
                {
                    if (PrefijosDetalle.Any(p => t.name.StartsWith(p)) && PrefabUtility.GetNearestPrefabInstanceRoot(t) == t.gameObject)
                    {
                        if (AsegurarColliderYBarril(t.gameObject))
                        {
                            cambios++;
                            Nota("  collider/barril asegurado en " + ColocarS2.Ruta(t) + " " + t.position.ToString("F1"));
                        }
                    }
                }
                return cambios;
            });
            Nota("Puerto: " + n + " piezas de detalle con collider o BarrilExplosivo nuevo.");
        }

        // ---------------------------------------------------------------- 2. Barriles

        private static void Barriles()
        {
            int total = 0;
            foreach (string ruta in Zonas())
            {
                int n = EditarPrefab(ruta, raiz =>
                {
                    int c = 0;
                    foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true).Where(ColocarT1317Ajustes.EsBarrilDeCiudad).ToList())
                    {
                        if (AsegurarColliderYBarril(t.gameObject, true))
                        {
                            c++;
                        }
                    }
                    return c;
                });
                if (n > 0)
                {
                    Nota("  " + Path.GetFileName(ruta) + ": " + n + " barriles/tambos arreglados");
                }
                total += n;
            }
            Nota("Barriles/tambos con BarrilExplosivo o collider nuevo: " + total);
        }

        // ---------------------------------------------------------------- 3. Greybox

        private static bool TieneTextura(Material m)
        {
            Shader s = m.shader;
            for (int i = 0; i < s.GetPropertyCount(); i++)
            {
                if (s.GetPropertyType(i) == UnityEngine.Rendering.ShaderPropertyType.Texture && m.GetTexture(s.GetPropertyName(i)) != null)
                {
                    return true;
                }
            }
            return false;
        }

        private static bool EsEfecto(Material m)
        {
            if (m.HasProperty("_EmissionColor") && m.GetColor("_EmissionColor").maxColorComponent > 0.3f)
            {
                return true;
            }
            if (m.HasProperty("_Surface") && m.GetFloat("_Surface") > 0.5f)
            {
                return true;
            }
            string sh = m.shader.name;
            return sh.Contains("Unlit") || sh.Contains("Particles") || sh.Contains("Sprites") || sh.StartsWith("MechaCyberpunk/Torre") || sh == "MechaCyberpunk/Mar"
                   || sh == "MechaCyberpunk/Mundo/EmisionNoche" || sh.Contains("TextMeshPro") || sh.Contains("UI/");
        }

        private static string RutaFuente(GameObject go)
        {
            GameObject f = PrefabUtility.GetCorrespondingObjectFromOriginalSource(go);
            return f == null ? "" : AssetDatabase.GetAssetPath(f);
        }

        private static bool Texturado(Material m)
        {
            return m != null && m.shader != null && m.shader.isSupported && m.HasProperty("_BaseMap") && m.GetTexture("_BaseMap") != null;
        }

        /// <summary>El .mat con textura de la carpeta del modelo (o la de arriba): primero el que se llama como el modelo, luego como el material liso, y si solo hay uno con textura, ese.</summary>
        private static Material BuscarConTextura(Renderer r, Material actual)
        {
            string fuente = "";
            for (Transform t = r.transform; t != null; t = t.parent)
            {
                fuente = RutaFuente(t.gameObject);
                if (fuente.StartsWith("Assets/Arte/"))
                {
                    break;
                }
            }
            if (!fuente.StartsWith("Assets/Arte/"))
            {
                return null;
            }
            string carpeta = Path.GetDirectoryName(fuente).Replace('\\', '/');
            string modelo = Path.GetFileNameWithoutExtension(fuente);
            string nombre = actual != null ? actual.name.Replace(" (Instance)", "") : r.name;
            foreach (string c in new[] { carpeta, (Path.GetDirectoryName(carpeta) ?? carpeta).Replace('\\', '/') })
            {
                if (!AssetDatabase.IsValidFolder(c))
                {
                    continue;
                }
                List<Material> con = AssetDatabase.FindAssets("t:Material", new[] { c }).Select(AssetDatabase.GUIDToAssetPath)
                    .Select(AssetDatabase.LoadAssetAtPath<Material>).Where(Texturado).ToList();
                Material m = con.FirstOrDefault(x => x.name == modelo) ?? con.FirstOrDefault(x => x.name == nombre)
                             ?? con.FirstOrDefault(x => x.name.Contains(modelo) || modelo.Contains(x.name)) ?? con.FirstOrDefault(x => x.name.Contains(nombre) || nombre.Contains(x.name));
                if (m != null)
                {
                    return m;
                }
                if (con.Count == 1 && c == carpeta)
                {
                    return con[0];
                }
            }
            return null;
        }

        private static void Greybox()
        {
            int total = 0, sinArreglo = 0;
            foreach (string ruta in Zonas())
            {
                int sin = 0;
                int n = EditarPrefab(ruta, raiz =>
                {
                    int cambios = 0;
                    foreach (Renderer r in raiz.GetComponentsInChildren<Renderer>(false))
                    {
                        if (!(r is MeshRenderer) || !r.enabled || !r.gameObject.activeInHierarchy)
                        {
                            continue;
                        }
                        Vector3 tam = r.bounds.size;
                        if (Mathf.Min(tam.x, Mathf.Min(tam.y, tam.z)) < 0.12f)
                        {
                            continue;
                        }
                        MeshFilter mf = r.GetComponent<MeshFilter>();
                        if (mf == null || mf.sharedMesh == null)
                        {
                            continue;
                        }
                        Material[] ms = r.sharedMaterials;
                        bool modificado = false;
                        for (int i = 0; i < ms.Length; i++)
                        {
                            Material m = ms[i];
                            if (m == null || MaterialesDecal.Any(d => m.name.Contains(d)) || EsEfecto(m) || TieneTextura(m))
                            {
                                continue;
                            }
                            Material nuevo = BuscarConTextura(r, m);
                            if (nuevo != null)
                            {
                                ms[i] = nuevo;
                                modificado = true;
                            }
                            else
                            {
                                sin++;
                                Nota("  SIN ARREGLO (no hallé .mat con textura): " + ColocarS2.Ruta(r.transform) + " | " + m.name);
                            }
                        }
                        if (modificado)
                        {
                            r.sharedMaterials = ms;
                            cambios++;
                        }
                    }
                    return cambios;
                });
                if (n > 0 || sin > 0)
                {
                    Nota("  " + Path.GetFileName(ruta) + ": " + n + " mallas con material nuevo, " + sin + " sin arreglo");
                }
                total += n;
                sinArreglo += sin;
            }
            Nota("Mallas con material con textura: " + total + "; sin arreglo: " + sinArreglo);
        }

        // ---------------------------------------------------------------- 4. Semáforo

        private static void Semaforo()
        {
            int n = EditarPrefab(RutaAutopista, raiz =>
            {
                int c = 0;
                foreach (SemaforoCiclo s in raiz.GetComponentsInChildren<SemaforoCiclo>(true).ToList())
                {
                    Nota("  SemaforoCiclo quitado de " + ColocarS2.Ruta(s.transform) + ": el ES01 anidado en un prefab no pasa PiezasCalleT910 (queda fijo)");
                    Object.DestroyImmediate(s);
                    c++;
                }
                return c;
            });
            Nota("Semáforos de la Autopista sin ciclo: " + n);
        }

        // ---------------------------------------------------------------- 5. Cerro

        private static string RutaRel(Transform t, Transform raiz)
        {
            StringBuilder sb = new StringBuilder(t.name);
            for (Transform u = t.parent; u != null && u != raiz; u = u.parent)
            {
                sb.Insert(0, u.name + "/");
            }
            return sb.ToString();
        }

        private static Vector3 CentroRend(Transform t)
        {
            Renderer[] rs = t.GetComponentsInChildren<Renderer>(true);
            if (rs.Length == 0)
            {
                return t.position;
            }
            Bounds b = rs[0].bounds;
            foreach (Renderer r in rs)
            {
                b.Encapsulate(r.bounds);
            }
            return b.center;
        }

        private static Transform BuscarPorRuta(Transform raiz, string ruta, Vector3 pos)
        {
            string ultimo = ruta.Contains("/") ? ruta.Substring(ruta.LastIndexOf('/') + 1) : ruta;
            Transform mejor = null;
            float md = float.MaxValue;
            foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true))
            {
                if (t == raiz || t.name != ultimo || RutaRel(t, raiz) != ruta)
                {
                    continue;
                }
                float d = (CentroRend(t) - pos).sqrMagnitude;
                if (d < md)
                {
                    md = d;
                    mejor = t;
                }
            }
            return mejor;
        }

        private static List<int> IndicesDesde(Transform t, Transform raiz, List<string> nombres)
        {
            List<int> idx = new List<int>();
            for (Transform u = t; u != raiz; u = u.parent)
            {
                idx.Add(u.GetSiblingIndex());
                nombres.Add(u.name);
            }
            idx.Reverse();
            nombres.Reverse();
            return idx;
        }

        private static Transform Resolver(GameObject raiz, int[] idx, string[] nombres)
        {
            Transform t = raiz.transform;
            for (int i = 0; i < idx.Length; i++)
            {
                if (idx[i] >= t.childCount)
                {
                    return null;
                }
                t = t.GetChild(idx[i]);
                if (t.name != nombres[i])
                {
                    return null;
                }
            }
            return t;
        }

        private sealed class TrabajoCerro
        {
            public string Tipo;
            public string Ruta;
            public int[] Indices;
            public string[] Nombres;
        }

        private static void Cerro()
        {
            int sinCollider = 0, carros = 0;
            for (int pasada = 1; pasada <= MaxPasadasCerro; pasada++)
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaCerro);
                if (prefab == null)
                {
                    Nota("FALTA " + RutaCerro);
                    return;
                }
                GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                Physics.SyncTransforms();
                List<TrabajoCerro> trabajos = new List<TrabajoCerro>();
                try
                {
                    List<BarridoZonasT1360.Hallazgo> hall = BarridoZonasT1360.BarrerZona(inst)
                        .Where(h => (h.tipo == BarridoZonasT1360.TipoSinCollider && !h.ruta.Contains("CC04_Tendedero")) || h.tipo == BarridoZonasT1360.TipoCarro).ToList();
                    foreach (BarridoZonasT1360.Hallazgo h in hall)
                    {
                        Transform t = BuscarPorRuta(inst.transform, h.ruta, h.posicion);
                        if (t == null)
                        {
                            Nota("  no hallé en el prefab: " + h.ruta);
                            continue;
                        }
                        List<string> nombres = new List<string>();
                        List<int> idx = IndicesDesde(t, inst.transform, nombres);
                        trabajos.Add(new TrabajoCerro { Tipo = h.tipo, Ruta = h.ruta, Indices = idx.ToArray(), Nombres = nombres.ToArray() });
                    }
                }
                finally
                {
                    Object.DestroyImmediate(inst);
                }
                Nota("Cerro pasada " + pasada + ": " + trabajos.Count + " hallazgos del barrido (sin collider o carro en el carril)");
                if (trabajos.Count == 0)
                {
                    break;
                }
                int hechos = EditarPrefab(RutaCerro, raiz =>
                {
                    int c = 0;
                    foreach (TrabajoCerro w in trabajos)
                    {
                        Transform t = Resolver(raiz, w.Indices, w.Nombres);
                        if (t == null)
                        {
                            Nota("  no resolví " + w.Ruta);
                            continue;
                        }
                        if (w.Tipo == BarridoZonasT1360.TipoCarro)
                        {
                            if (t.gameObject.activeSelf)
                            {
                                t.gameObject.SetActive(false);
                                carros++;
                                c++;
                                Nota("  carro en el carril apagado: " + w.Ruta);
                            }
                        }
                        else
                        {
                            bool hecho = false;
                            foreach (Collider col in t.GetComponentsInChildren<Collider>(true))
                            {
                                if (!col.isTrigger && !col.enabled)
                                {
                                    col.enabled = true;
                                    hecho = true;
                                }
                            }
                            if (!hecho)
                            {
                                hecho = PonerCajaLocal(t);
                            }
                            if (hecho)
                            {
                                sinCollider++;
                                c++;
                                Nota("  collider puesto: " + w.Ruta);
                            }
                            else
                            {
                                Nota("  SIN ARREGLO (sin mallas utilizables): " + w.Ruta);
                            }
                        }
                    }
                    return c;
                });
                if (hechos == 0)
                {
                    Nota("Cerro: la pasada " + pasada + " no pudo arreglar nada; se detiene.");
                    break;
                }
            }
            Nota("Cerro: " + sinCollider + " props con collider nuevo, " + carros + " carros apagados.");
        }

        // ---------------------------------------------------------------- 6. Pisos de respaldo

        private static void Respaldo()
        {
            EditorSceneManager.OpenScene(ColocarS2.RutaEscena, OpenSceneMode.Single);
            int cambios = 0;
            foreach (Transform t in ColocarS2.Todos().Where(x => x.name.StartsWith("PisoRespaldo_")).ToList())
            {
                BoxCollider bc = t.GetComponent<BoxCollider>();
                if (bc == null)
                {
                    continue;
                }
                float alto = bc.size.y * Mathf.Abs(t.lossyScale.y);
                Vector3 p = t.position;
                float y = -ColocarT1365Uniones.BajoCalle - alto * 0.5f;
                if (Mathf.Abs(p.y - y) > 0.0005f)
                {
                    p.y = y;
                    t.position = p;
                    cambios++;
                    Nota("  " + t.name + ": cara superior ahora en y " + (-ColocarT1365Uniones.BajoCalle).ToString("F3"));
                }
            }
            if (cambios > 0)
            {
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            }
            Nota("Pisos de respaldo movidos: " + cambios);
        }

        // ---------------------------------------------------------------- 7. Escena (toldos, carros, flotantes, encimados)

        private sealed class Cambio
        {
            public string Accion;
            public int[] Indices;
            public string[] Nombres;
            public Vector3 DeltaLocalRaiz;
            public string Ruta;
        }

        private static readonly Dictionary<string, List<Cambio>> porPrefab = new Dictionary<string, List<Cambio>>();
        private static readonly List<Transform> tratados = new List<Transform>();
        private static bool escenaSucia;

        private static bool Tratado(Transform t)
        {
            return tratados.Any(x => x == t || t.IsChildOf(x) || x.IsChildOf(t));
        }

        private static bool Mapear(Transform t, out string ruta, out Transform raizEscena, out int[] idx, out string[] nombres)
        {
            ruta = null;
            raizEscena = null;
            idx = null;
            nombres = null;
            GameObject raiz = PrefabUtility.GetOutermostPrefabInstanceRoot(t.gameObject);
            if (raiz == null)
            {
                return false;
            }
            GameObject fuente = PrefabUtility.GetCorrespondingObjectFromSource(raiz);
            if (fuente == null)
            {
                return false;
            }
            ruta = AssetDatabase.GetAssetPath(fuente);
            if (!ruta.StartsWith(PrefijoMundo) || !ruta.EndsWith(".prefab"))
            {
                return false;
            }
            raizEscena = raiz.transform;
            List<string> n = new List<string>();
            List<int> l = new List<int>();
            for (Transform u = t; u != raizEscena; u = u.parent)
            {
                if (u == null)
                {
                    return false;
                }
                l.Add(u.GetSiblingIndex());
                n.Add(u.name);
            }
            l.Reverse();
            n.Reverse();
            idx = l.ToArray();
            nombres = n.ToArray();
            return true;
        }

        /// <summary>Anota apagar o mover (delta en mundo) para el prefab de la zona; si el objeto no es de un prefab de Assets/Arte/Mundo/, lo hace en la escena.</summary>
        private static bool Registrar(Transform t, string accion, Vector3 deltaMundo, string motivo)
        {
            if (t == null || Tratado(t))
            {
                return false;
            }
            tratados.Add(t);
            string desc = accion + " (" + motivo + ") " + ColocarS2.Ruta(t) + " en " + t.position.ToString("F1");
            if (Mapear(t, out string ruta, out Transform raizEscena, out int[] idx, out string[] nombres))
            {
                if (!porPrefab.TryGetValue(ruta, out List<Cambio> lista))
                {
                    lista = new List<Cambio>();
                    porPrefab[ruta] = lista;
                }
                lista.Add(new Cambio { Accion = accion, Indices = idx, Nombres = nombres, DeltaLocalRaiz = raizEscena.InverseTransformVector(deltaMundo), Ruta = desc });
                Nota("  [" + Path.GetFileName(ruta) + "] " + desc);
                return true;
            }
            if (accion == "apagar")
            {
                t.gameObject.SetActive(false);
            }
            else
            {
                t.position += deltaMundo;
            }
            escenaSucia = true;
            Nota("  [escena] " + desc);
            return true;
        }

        private static int AplicarCambios()
        {
            int total = 0;
            foreach (KeyValuePair<string, List<Cambio>> kv in porPrefab)
            {
                List<Cambio> lista = kv.Value;
                total += EditarPrefab(kv.Key, raiz =>
                {
                    int c = 0;
                    foreach (Cambio cambio in lista)
                    {
                        Transform t = Resolver(raiz, cambio.Indices, cambio.Nombres);
                        if (t == null)
                        {
                            Nota("  NO RESUELTO en " + Path.GetFileName(kv.Key) + ": " + cambio.Ruta);
                            continue;
                        }
                        if (cambio.Accion == "apagar")
                        {
                            t.gameObject.SetActive(false);
                        }
                        else
                        {
                            t.position += raiz.transform.TransformVector(cambio.DeltaLocalRaiz);
                        }
                        c++;
                    }
                    return c;
                });
            }
            return total;
        }

        private static float PisoBajo(Transform carro, Vector3 desplazamiento)
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

        private static int ToldosEscena()
        {
            int n = 0;
            foreach ((Transform toldo, string motivo) in ColocarT1331Ajustes.ToldosMalos())
            {
                if (Registrar(toldo, "apagar", Vector3.zero, "toldo " + motivo))
                {
                    n++;
                }
            }
            return n;
        }

        private static int CarrosEscena()
        {
            int n = 0;
            Physics.SyncTransforms();
            foreach (Component carro in ColocarT1331Ajustes.CarrosDeLaCiudad().Cast<Component>().ToList())
            {
                Transform t = carro.transform;
                List<Collider> estorbos = ColocarT1331Ajustes.Estorbos(t);
                if (estorbos.Count == 0)
                {
                    continue;
                }
                string quien = string.Join(", ", estorbos.Select(e => e.transform.root.name + "/" + e.name).Distinct().Take(3));
                float pisoOriginal = PisoBajo(t, Vector3.zero);
                Vector3 mejor = Vector3.zero;
                bool hallado = false;
                for (float d = PasoCarro; d <= BuscarCarroHasta && !hallado; d += PasoCarro)
                {
                    foreach (Vector3 eje in new[] { t.forward, -t.forward })
                    {
                        Vector3 desp = eje * d;
                        float piso = PisoBajo(t, desp);
                        if (!float.IsNaN(pisoOriginal) && (float.IsNaN(piso) || Mathf.Abs(piso - pisoOriginal) > 0.25f))
                        {
                            continue;
                        }
                        if (ColocarT1331Ajustes.Estorbos(t, desp).Count == 0)
                        {
                            mejor = desp;
                            hallado = true;
                            break;
                        }
                    }
                }
                if (hallado)
                {
                    if (Registrar(t, "mover", mejor, "carro sobre " + quien + ", se mueve " + mejor.magnitude.ToString("F1") + " m"))
                    {
                        n++;
                    }
                }
                else if (Registrar(t, "apagar", Vector3.zero, "carro sobre " + quien + " y sin lugar libre"))
                {
                    n++;
                }
            }
            return n;
        }

        private static int FlotantesEscena()
        {
            int n = 0;
            foreach ((Transform pieza, Bounds limites, float hueco) in FlotantesS2t.Buscar())
            {
                bool ok;
                if (!float.IsInfinity(hueco) && hueco <= FlotantesS2t.BajadaMaxima)
                {
                    ok = Registrar(pieza, "mover", Vector3.down * hueco, "flotaba " + hueco.ToString("F2") + " m, se asienta en el techo o piso de abajo");
                }
                else
                {
                    ok = Registrar(pieza, "apagar", Vector3.zero, "flota sin apoyo en " + limites.center.ToString("F1"));
                }
                if (ok)
                {
                    n++;
                }
            }
            return n;
        }

        private static bool EsNueva(Transform t)
        {
            for (Transform u = t; u != null; u = u.parent)
            {
                if (u.name.Length >= 5 && u.name[0] == 'T' && int.TryParse(u.name.Substring(1, 4), out int num) && num >= 1380)
                {
                    return true;
                }
            }
            return false;
        }

        private static float Volumen(Transform t)
        {
            Renderer[] rs = t.GetComponentsInChildren<Renderer>().Where(r => r.enabled).ToArray();
            if (rs.Length == 0)
            {
                return 0f;
            }
            Bounds b = rs[0].bounds;
            foreach (Renderer r in rs)
            {
                b.Encapsulate(r.bounds);
            }
            return b.size.x * b.size.y * b.size.z;
        }

        private static int EncimadosEscena()
        {
            int n = 0;
            foreach (RevisarCiudadT995.Encimado e in RevisarCiudadT995.Encimados())
            {
                Transform a = e.A, b = e.B;
                Transform quitar;
                if (e.Tipo == "dentro-de-fachada" || b == null)
                {
                    quitar = a;
                }
                else if (EsNueva(a) != EsNueva(b))
                {
                    quitar = EsNueva(a) ? a : b;
                }
                else if (e.Tipo == "duplicado")
                {
                    quitar = b;
                }
                else
                {
                    quitar = Volumen(a) <= Volumen(b) ? a : b;
                }
                if (Registrar(quitar, "apagar", Vector3.zero, e.Tipo + " " + e.Medida.ToString("F2") + (e.Detalle != null ? " {" + e.Detalle + "}" : "")))
                {
                    n++;
                }
            }
            return n;
        }

        private static void Escena()
        {
            int total = 0;
            for (int pasada = 1; pasada <= MaxPasadasEscena; pasada++)
            {
                EditorSceneManager.OpenScene(ColocarS2.RutaEscena, OpenSceneMode.Single);
                Physics.SyncTransforms();
                porPrefab.Clear();
                tratados.Clear();
                escenaSucia = false;
                int toldos = 0, carros = 0, flotantes = 0, encimados = 0;
                try { toldos = ToldosEscena(); } catch (Exception ex) { Nota("FALLO toldos: " + ex); }
                try { carros = CarrosEscena(); } catch (Exception ex) { Nota("FALLO carros: " + ex); }
                try { flotantes = FlotantesEscena(); } catch (Exception ex) { Nota("FALLO flotantes: " + ex); }
                try { encimados = EncimadosEscena(); } catch (Exception ex) { Nota("FALLO encimados: " + ex); }
                int n = toldos + carros + flotantes + encimados;
                Nota("Escena pasada " + pasada + ": toldos " + toldos + ", carros " + carros + ", flotantes " + flotantes + ", encimados " + encimados);
                if (escenaSucia)
                {
                    EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                    EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
                }
                int aplicados = AplicarCambios();
                total += aplicados;
                if (n == 0)
                {
                    break;
                }
                if (aplicados == 0 && !escenaSucia)
                {
                    Nota("Escena: la pasada " + pasada + " no pudo aplicar nada; se detiene.");
                    break;
                }
            }
            AssetDatabase.SaveAssets();
            Nota("Escena: " + total + " cambios aplicados a los prefabs de zona.");
        }
    }
}
