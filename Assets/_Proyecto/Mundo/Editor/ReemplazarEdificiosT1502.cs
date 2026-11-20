using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Tipo = MechaCyberpunk.Mundo.Editor.CalculoEdificiosT1502.Tipo;

namespace MechaCyberpunk.Mundo.Editor
{
    /// <summary>
    /// T-1502: edificios de Ciudad_Prologo que siguen siendo primitivas (cubos, mallas generadas) cambiados por modelos que ya existen en Assets/Arte.
    /// Orden en batchmode (cada uno se puede repetir):
    ///   1. -executeMethod MechaCyberpunk.Mundo.Editor.ReemplazarEdificiosT1502.Listar     -> Logs/t1502-primitivos.txt
    ///   2. -executeMethod MechaCyberpunk.Mundo.Editor.ReemplazarEdificiosT1502.Reemplazar -> cambia la escena, guarda y deja Logs/t1502-pendientes.txt
    /// Los cubos no se borran: se apaga su MeshRenderer (el collider se queda) para no romper scripts que los busquen.
    /// Solo se tocan los cubos "Edificio" de las afueras; lo demás (edificios con kit de fachadas, locales con interior, torres, muros) se anota.
    /// </summary>
    public static class ReemplazarEdificiosT1502
    {
        public const string RutaListado = "Logs/t1502-primitivos.txt";
        public const string RutaPendientes = "Logs/t1502-pendientes.txt";
        public const string PrefijoModelo = "T1502_Modelo";
        public const string NombrePendiente = "T1502_Pendiente";
        private const float ToleranciaEscalaPadre = 0.01f;
        /// <summary>Estiramiento máximo que acepta Cambiar; T-1512 lo baja en su primera pasada.</summary>
        internal static float LimiteDistorsion = CalculoEdificiosT1502.DistorsionMaxima;
        private const float MargenVentanas = 3f;
        private const float MargenCosto = 0.2f;
        private const float AzarPosicionX = 7f;
        private const float AzarPosicionZ = 13f;

        private static readonly string[] ClavesBarrio = { "NeonAlto", "Suburbios", "Tianguis", "Mercado" };

        private const string Calle = "Assets/Arte/Calle/Prefabs/";
        private const string Zonas = "Assets/Arte/Zonas/";
        private const string Exteriores = "Assets/Arte/Exteriores/Prefabs/";

        // Edificios que ya existen, por estilo de barrio. Los que no estén importados se saltan.
        private static readonly string[] EstiloNeon =
        {
            Calle + "NV01_FachadaOficinas.prefab", Exteriores + "P11_Fachada.prefab", Exteriores + "P12_Fachada.prefab",
            Zonas + "LO/LO01_CasaModerna.prefab", Zonas + "LO/LO02_CasaModerna.prefab", Zonas + "LO/LO03_CasaModerna.prefab"
        };
        private static readonly string[] EstiloSuburbios =
        {
            Zonas + "CC/CC01_CasaCerro.prefab", Zonas + "CC/CC02_CasaCerro.prefab", Zonas + "CC/CC06_CasaCerro_a.prefab",
            Zonas + "CC/CC06_CasaCerro_b.prefab", Zonas + "CC/CC07_CasaCerro_a.prefab", Zonas + "CC/CC07_CasaCerro_b.prefab",
            Zonas + "LO/LO01_CasaModerna.prefab"
        };
        private static readonly string[] EstiloMercado =
        {
            Zonas + "CH/CH02_EdificioColonial.prefab", Zonas + "ML/ML05_EdificioMalecon_a.prefab", Zonas + "ML/ML05_EdificioMalecon_b.prefab",
            Zonas + "PU/PU03_Bodega.prefab", Zonas + "PU/PU04_Bodega.prefab", Zonas + "CC/CC06_CasaCerro_a.prefab"
        };

        public class Primitivo
        {
            public MeshRenderer Renderer;
            public string Ruta;
            public string Barrio;
            public Bounds Caja;
            public Tipo Tipo;
            public bool TienePendiente;
        }

        internal class Medida
        {
            public GameObject Prefab;
            public Vector3 Tam;
            public Vector3 Desfase; // (centro x, base y, centro z) del modelo respecto de su pivote, sin girar
        }

        [MenuItem("Mecha Cyberpunk/T-1502 Listar primitivas")]
        public static void Listar()
        {
            Scene escena = Abrir();
            List<Primitivo> lista = Escanear(escena);
            Escribir(RutaListado, Texto("Primitivas de edificios en " + CreadorPrologoCiudad.RutaEscena, lista));
            Debug.Log("[Mundo] T-1502 listado: " + lista.Count + " primitivas en " + RutaListado);
        }

        [MenuItem("Mecha Cyberpunk/T-1502 Reemplazar edificios")]
        public static void Reemplazar()
        {
            Scene escena = Abrir();
            List<Primitivo> antes = Escanear(escena);
            Escribir(RutaListado, Texto("Primitivas de edificios (antes de reemplazar) en " + CreadorPrologoCiudad.RutaEscena, antes));

            Dictionary<string, List<Medida>> paletas = CargarPaletas();
            Dictionary<string, Vector3> centros = CentrosBarrio(escena);
            StringBuilder informe = new StringBuilder();
            int cambiados = 0;
            int pendientes = 0;
            foreach (Primitivo p in antes.Where(x => x.Tipo == Tipo.EdificioAfueras && !x.TienePendiente))
            {
                string motivo = Cambiar(p, paletas, centros);
                if (motivo == null)
                {
                    cambiados++;
                }
                else
                {
                    pendientes++;
                    informe.AppendLine("PENDIENTE\t" + p.Ruta + "\t" + p.Barrio + "\t" + Tam(p.Caja.size) + "\t" + motivo);
                }
            }

            EditorSceneManager.MarkSceneDirty(escena);
            EditorSceneManager.SaveScene(escena);

            List<Primitivo> despues = Escanear(escena);
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("T-1502: reemplazados " + cambiados + ", pendientes " + pendientes + " de " + (cambiados + pendientes) + " edificios de las afueras");
            sb.Append(informe);
            sb.AppendLine();
            sb.Append(Texto("Primitivas visibles que quedan (anotadas por tipo)", despues));
            Escribir(RutaPendientes, sb.ToString());
            Debug.Log("[Mundo] T-1502: " + cambiados + " reemplazados, " + pendientes + " pendientes; ver " + RutaPendientes);
        }

        /// <summary>Todas las primitivas visibles de la escena que parecen edificio, muro o bloque. Los cubos ya cambiados no aparecen (su MeshRenderer está apagado).</summary>
        public static List<Primitivo> Escanear(Scene escena)
        {
            Dictionary<string, Vector3> centros = CentrosBarrio(escena);
            List<Primitivo> lista = new List<Primitivo>();
            foreach (GameObject raiz in escena.GetRootGameObjects())
            {
                foreach (MeshRenderer mr in raiz.GetComponentsInChildren<MeshRenderer>(false))
                {
                    if (!mr.enabled)
                    {
                        continue;
                    }
                    MeshFilter mf = mr.GetComponent<MeshFilter>();
                    if (mf == null || mf.sharedMesh == null || !CalculoEdificiosT1502.EsMallaPrimitiva(AssetDatabase.GetAssetPath(mf.sharedMesh)))
                    {
                        continue;
                    }
                    string ruta = RutaDe(mr.transform);
                    Bounds caja = mr.bounds;
                    Tipo tipo = CalculoEdificiosT1502.Clasificar(ruta, caja.size);
                    if (tipo == Tipo.Ninguno)
                    {
                        continue;
                    }
                    lista.Add(new Primitivo
                    {
                        Renderer = mr,
                        Ruta = ruta,
                        Caja = caja,
                        Tipo = tipo,
                        Barrio = BarrioDe(caja.center, centros),
                        TienePendiente = mr.transform.Find(NombrePendiente) != null
                    });
                }
            }
            return lista;
        }

        // Devuelve null si cambió el edificio, o el motivo si queda pendiente.
        internal static string Cambiar(Primitivo p, Dictionary<string, List<Medida>> paletas, Dictionary<string, Vector3> centros)
        {
            Transform cubo = p.Renderer.transform;
            Transform padre = cubo.parent;
            if (padre == null || cubo.GetComponentInParent<LODGroup>() != null)
            {
                return Pendiente(cubo, "dentro de un LODGroup o sin padre");
            }
            Vector3 ps = padre.lossyScale;
            if (Mathf.Abs(ps.x - 1f) > ToleranciaEscalaPadre || Mathf.Abs(ps.y - 1f) > ToleranciaEscalaPadre || Mathf.Abs(ps.z - 1f) > ToleranciaEscalaPadre)
            {
                return Pendiente(cubo, "el padre está escalado");
            }
            Vector3 e = cubo.eulerAngles;
            if (!CalculoEdificiosT1502.EsMultiplo90(e.x) || !CalculoEdificiosT1502.EsMultiplo90(e.y) || !CalculoEdificiosT1502.EsMultiplo90(e.z))
            {
                return Pendiente(cubo, "girado fuera de múltiplos de 90");
            }

            Bounds b = p.Caja;
            Vector3 frente = Frente(cubo, b);
            bool sobreZ = Mathf.Abs(frente.z) > 0.5f;
            float anchoFrente = sobreZ ? b.size.x : b.size.z;
            float fondo = sobreZ ? b.size.z : b.size.x;

            List<Medida> paleta;
            if (!paletas.TryGetValue(p.Barrio, out paleta) || paleta.Count == 0)
            {
                paletas.TryGetValue("", out paleta);
            }
            if (paleta == null || paleta.Count == 0)
            {
                return Pendiente(cubo, "no hay edificios importados en la paleta");
            }

            List<KeyValuePair<Medida, (int tramos, Vector3 escala, float costo)>> opciones = new List<KeyValuePair<Medida, (int, Vector3, float)>>();
            foreach (Medida m in paleta)
            {
                int tramos;
                Vector3 escala;
                float costo;
                if (CalculoEdificiosT1502.Ajustar(m.Tam, anchoFrente, fondo, b.size.y, out tramos, out escala, out costo, LimiteDistorsion))
                {
                    opciones.Add(new KeyValuePair<Medida, (int, Vector3, float)>(m, (tramos, escala, costo)));
                }
            }
            if (opciones.Count == 0)
            {
                return Pendiente(cubo, "ningún modelo llena " + Tam(b.size) + " sin pasar la escala o el estiramiento permitidos");
            }
            float mejor = opciones.Min(o => o.Value.costo);
            List<KeyValuePair<Medida, (int tramos, Vector3 escala, float costo)>> buenas = opciones.Where(o => o.Value.costo <= mejor + MargenCosto).ToList();
            int indice = Mathf.Abs(Mathf.RoundToInt(b.center.x * AzarPosicionX + b.center.z * AzarPosicionZ)) % buenas.Count;
            Medida elegido = buenas[indice].Key;
            int n = buenas[indice].Value.tramos;
            Vector3 esc = buenas[indice].Value.escala;

            Quaternion giro = Quaternion.LookRotation(frente, Vector3.up);
            Vector3 eje = sobreZ ? Vector3.right : Vector3.forward;
            Vector3 baseCentro = new Vector3(b.center.x, b.min.y, b.center.z);
            for (int i = 0; i < n; i++)
            {
                float desplazamiento = ((i + 0.5f) / n - 0.5f) * anchoFrente;
                GameObject modelo = (GameObject)PrefabUtility.InstantiatePrefab(elegido.Prefab, padre);
                modelo.name = PrefijoModelo + "_" + elegido.Prefab.name + "_" + i;
                modelo.transform.localScale = esc;
                Vector3 mundo = baseCentro + eje * desplazamiento - giro * Vector3.Scale(elegido.Desfase, esc);
                modelo.transform.SetPositionAndRotation(mundo, giro);
                modelo.isStatic = true;
            }

            // El collider del cubo se queda; solo se apaga lo que se ve.
            p.Renderer.enabled = false;
            foreach (Transform hermano in padre.Cast<Transform>().ToList())
            {
                if (hermano.name == "Ventanas" && hermano.gameObject.activeSelf)
                {
                    Renderer rv = hermano.GetComponent<Renderer>();
                    if (rv != null && new Bounds(b.center, b.size + Vector3.one * MargenVentanas).Intersects(rv.bounds))
                    {
                        hermano.gameObject.SetActive(false);
                    }
                }
            }
            return null;
        }

        private static string Pendiente(Transform cubo, string motivo)
        {
            if (cubo.Find(NombrePendiente) == null)
            {
                GameObject marca = new GameObject(NombrePendiente);
                marca.transform.SetParent(cubo, false);
            }
            return motivo;
        }

        // El frente mira a la calle más cercana del mismo grupo; si no hay, al centro del mapa.
        private static Vector3 Frente(Transform cubo, Bounds b)
        {
            Vector3 dir = -b.center;
            float mejor = float.MaxValue;
            foreach (Transform s in cubo.parent)
            {
                if (s.name != "Calle" && s.name != "CalleCruzada")
                {
                    continue;
                }
                Renderer r = s.GetComponent<Renderer>();
                if (r == null)
                {
                    continue;
                }
                Vector3 cercano = r.bounds.ClosestPoint(b.center);
                float d = (cercano - b.center).sqrMagnitude;
                if (d > 0.0001f && d < mejor)
                {
                    mejor = d;
                    dir = cercano - b.center;
                }
            }
            dir.y = 0f;
            return CalculoEdificiosT1502.Cardinal(dir);
        }

        private static Dictionary<string, List<Medida>> CargarPaletas()
        {
            Dictionary<string, Medida> cache = new Dictionary<string, Medida>();
            Dictionary<string, List<Medida>> r = new Dictionary<string, List<Medida>>();
            r["NeonAlto"] = Medidas(EstiloNeon, cache);
            r["Suburbios"] = Medidas(EstiloSuburbios, cache);
            r["Tianguis"] = Medidas(EstiloMercado, cache);
            r["Mercado"] = r["Tianguis"];
            r[""] = Medidas(EstiloNeon.Concat(EstiloSuburbios).Concat(EstiloMercado).Distinct().ToArray(), cache);
            return r;
        }

        internal static List<Medida> Medidas(string[] rutas, Dictionary<string, Medida> cache)
        {
            List<Medida> lista = new List<Medida>();
            foreach (string ruta in rutas)
            {
                Medida m;
                if (!cache.TryGetValue(ruta, out m))
                {
                    m = Medir(ruta);
                    cache[ruta] = m;
                }
                // Solo sirven los que son de tamaño de edificio.
                if (m != null && m.Tam.y >= 5f && Mathf.Max(m.Tam.x, m.Tam.z) >= 5f)
                {
                    lista.Add(m);
                }
            }
            return lista;
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
            Medida m = null;
            if (rs.Length > 0)
            {
                Bounds b = rs[0].bounds;
                foreach (Renderer x in rs)
                {
                    b.Encapsulate(x.bounds);
                }
                m = new Medida { Prefab = prefab, Tam = b.size, Desfase = new Vector3(b.center.x, b.min.y, b.center.z) };
            }
            Object.DestroyImmediate(temporal);
            return m;
        }

        internal static Dictionary<string, Vector3> CentrosBarrio(Scene escena)
        {
            Dictionary<string, Vector3> centros = new Dictionary<string, Vector3>();
            foreach (GameObject raiz in escena.GetRootGameObjects())
            {
                Consider(raiz.transform, centros);
                foreach (Transform hijo in raiz.transform)
                {
                    Consider(hijo, centros);
                }
            }
            return centros;
        }

        private static void Consider(Transform t, Dictionary<string, Vector3> centros)
        {
            foreach (string clave in ClavesBarrio)
            {
                if (!centros.ContainsKey(clave) && t.name.Contains(clave))
                {
                    Renderer[] rs = t.GetComponentsInChildren<Renderer>(false);
                    if (rs.Length > 0)
                    {
                        Bounds b = rs[0].bounds;
                        foreach (Renderer r in rs)
                        {
                            b.Encapsulate(r.bounds);
                        }
                        centros[clave] = b.center;
                    }
                }
            }
        }

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

        internal static Scene Abrir()
        {
            Scene activa = SceneManager.GetActiveScene();
            if (activa.path == CreadorPrologoCiudad.RutaEscena && activa.isLoaded)
            {
                return activa;
            }
            return EditorSceneManager.OpenScene(CreadorPrologoCiudad.RutaEscena, OpenSceneMode.Single);
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

        internal static string Tam(Vector3 v)
        {
            return v.x.ToString("F1") + "x" + v.y.ToString("F1") + "x" + v.z.ToString("F1");
        }

        internal static string Texto(string titulo, List<Primitivo> lista)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(titulo + ": " + lista.Count);
            sb.AppendLine("nombre\truta\tbarrio\ttamano (m)\ttipo\tpara que sirve");
            foreach (Primitivo p in lista.OrderBy(x => x.Tipo).ThenBy(x => x.Ruta))
            {
                sb.AppendLine(p.Renderer.name + "\t" + p.Ruta + "\t" + p.Barrio + "\t" + Tam(p.Caja.size) + "\t" + p.Tipo + (p.TienePendiente ? " (sin modelo)" : "") + "\t" + CalculoEdificiosT1502.Uso(p.Tipo));
            }
            return sb.ToString();
        }

        internal static void Escribir(string ruta, string texto)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ruta));
            File.WriteAllText(ruta, texto, new UTF8Encoding(false));
        }
    }
}
