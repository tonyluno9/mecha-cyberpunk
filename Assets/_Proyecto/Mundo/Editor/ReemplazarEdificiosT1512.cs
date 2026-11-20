using System.Collections.Generic;
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
    /// T-1512 parte B: segunda vuelta de T-1502 con los edificios ED01..ED12 (Assets/Arte/Zonas/ED/). Retoma los cubos "Edificio" de las afueras
    /// que T-1502 dejó con la marca T1502_Pendiente (y cualquiera que siga visible), con la paleta de ED de su barrio. Misma lógica de ajuste,
    /// asentado y frente que T-1502 (se reutiliza ReemplazarEdificiosT1502.Cambiar); primero acepta poco estiramiento y luego el de T-1502.
    /// Los edificios de barrio con kit de fachadas o LODGroup, los locales con interior, las torres del skyline y el muro (otra tarea)
    /// no se tocan: el Diagnóstico los lista.
    /// Orden en batchmode (después de ImportarEdificiosT1512.Aplicar; cada paso se puede repetir):
    ///   1. -executeMethod MechaCyberpunk.Mundo.Editor.ReemplazarEdificiosT1512.Aplicar      -> cambia la escena y la guarda
    ///   2. -executeMethod MechaCyberpunk.Mundo.Editor.ReemplazarEdificiosT1512.Diagnostico  -> Logs/t1512-diagnostico.txt
    /// </summary>
    public static class ReemplazarEdificiosT1512
    {
        public const string RutaDiagnostico = "Logs/t1512-diagnostico.txt";

        /// <summary>Cuántas primitivas visibles quedan por tipo y cuántos cubos de las afueras siguen sin modelo.</summary>
        public class Resumen
        {
            public Dictionary<Tipo, int> PorTipo = new Dictionary<Tipo, int>();
            public int AfuerasSinModelo;
            public int AfuerasAnotados;
            public List<string> RutasSinModelo = new List<string>();
        }

        [MenuItem("Mecha Cyberpunk/T-1512 Reemplazar edificios con ED")]
        public static void Aplicar()
        {
            Scene escena = ReemplazarEdificiosT1502.Abrir();
            Dictionary<string, List<ReemplazarEdificiosT1502.Medida>> paletas = CargarPaletas();
            Dictionary<string, Vector3> centros = ReemplazarEdificiosT1502.CentrosBarrio(escena);
            int cambiados = 0;
            float original = ReemplazarEdificiosT1502.LimiteDistorsion;
            try
            {
                foreach (float limite in new[] { CalculoEdificiosT1512.DistorsionPrimeraPasada, CalculoEdificiosT1502.DistorsionMaxima })
                {
                    ReemplazarEdificiosT1502.LimiteDistorsion = limite;
                    foreach (ReemplazarEdificiosT1502.Primitivo p in ReemplazarEdificiosT1502.Escanear(escena).Where(x => x.Tipo == Tipo.EdificioAfueras).ToList())
                    {
                        Transform cubo = p.Renderer.transform;
                        if (ReemplazarEdificiosT1502.Cambiar(p, paletas, centros) == null)
                        {
                            cambiados++;
                            Transform marca = cubo.Find(ReemplazarEdificiosT1502.NombrePendiente);
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
            int apagados = QuitarEncimados(escena);
            EditorSceneManager.MarkSceneDirty(escena);
            EditorSceneManager.SaveScene(escena);
            Debug.Log("[Mundo] T-1512: " + cambiados + " edificios cambiados por ED; " + apagados + " apagados por caer encima de otro modelo");
            Diagnostico();
        }


        /// <summary>Margen con que se mide el encime (igual que la prueba: cajas encogidas 1 m por lado).</summary>
        private const float HolguraEncime = -2f;

        /// <summary>En las afueras hay cubos de primitivas encimados entre sí (dos salidas que ponían edificio en el mismo lugar), así que
        /// su modelo ED cae encima de otro modelo. Ese hueco ya lo ocupa el otro: el ED se apaga (no se borra) y se anota. Los ED se revisan
        /// en orden y uno ya apagado no cuenta. Devuelve cuántos se apagaron.</summary>
        internal static int QuitarEncimados(Scene escena)
        {
            var modelos = new List<Transform>();
            foreach (GameObject raiz in escena.GetRootGameObjects())
            {
                foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name.StartsWith("T1502_Modelo_") && t.gameObject.activeInHierarchy)
                    {
                        modelos.Add(t);
                    }
                }
            }
            int apagados = 0;
            foreach (Transform m in modelos.Where(x => x.name.Contains("_ED")).ToList())
            {
                Transform otro = ConQuien(m, modelos);
                if (otro != null)
                {
                    Debug.Log("[Mundo] T-1512: se apaga " + Ruta(m) + ", encimado con " + Ruta(otro));
                    m.gameObject.SetActive(false);
                    modelos.Remove(m);
                    apagados++;
                }
            }
            return apagados;
        }

        private static string Ruta(Transform t) => (t.parent != null ? t.parent.name + "/" : "") + t.name + " " + Caja(t).center.ToString("F0") + " " + Caja(t).size.ToString("F0");

        private static bool Encima(Transform m, List<Transform> todos) => ConQuien(m, todos) != null;

        private static Transform ConQuien(Transform m, List<Transform> todos)
        {
            Bounds a = Caja(m);
            a.Expand(HolguraEncime);
            foreach (Transform o in todos)
            {
                if (o == m || o.IsChildOf(m) || m.IsChildOf(o))
                {
                    continue;
                }
                Bounds b = Caja(o);
                b.Expand(HolguraEncime);
                if (a.Intersects(b))
                {
                    return o;
                }
            }
            return null;
        }

        private static Bounds Caja(Transform t)
        {
            Renderer[] rs = t.GetComponentsInChildren<Renderer>(false);
            if (rs.Length == 0)
            {
                return new Bounds(t.position, Vector3.zero);
            }
            Bounds b = rs[0].bounds;
            foreach (Renderer r in rs)
            {
                b.Encapsulate(r.bounds);
            }
            return b;
        }

        [MenuItem("Mecha Cyberpunk/T-1512 Diagnostico de primitivas")]
        public static void Diagnostico()
        {
            Scene escena = ReemplazarEdificiosT1502.Abrir();
            List<ReemplazarEdificiosT1502.Primitivo> lista = ReemplazarEdificiosT1502.Escanear(escena);
            Resumen r = Contar(lista);
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("T-1512 diagnostico: primitivas de edificio visibles en " + CreadorPrologoCiudad.RutaEscena);
            foreach (KeyValuePair<Tipo, int> par in r.PorTipo.OrderBy(x => x.Key))
            {
                sb.AppendLine(par.Key + "\t" + par.Value + "\t" + CalculoEdificiosT1502.Uso(par.Key));
            }
            sb.AppendLine("Cubos de las afueras sin modelo (con marca T1502_Pendiente, no entraron ni con 2.4 de estiramiento): " + r.AfuerasAnotados);
            sb.AppendLine("Cubos de las afueras sin modelo y sin marca: " + r.AfuerasSinModelo);
            sb.AppendLine();
            sb.Append(ReemplazarEdificiosT1502.Texto("Detalle", lista));
            ReemplazarEdificiosT1502.Escribir(RutaDiagnostico, sb.ToString());
            Debug.Log("[Mundo] T-1512 diagnostico: " + r.AfuerasAnotados + " cubos de las afueras quedan anotados, " + r.AfuerasSinModelo + " sin marca; ver " + RutaDiagnostico);
        }

        /// <summary>Cuenta por tipo las primitivas visibles; las de las afueras se separan en anotadas (con marca) y sin marca.</summary>
        public static Resumen Contar(IEnumerable<ReemplazarEdificiosT1502.Primitivo> primitivas)
        {
            Resumen r = new Resumen();
            foreach (ReemplazarEdificiosT1502.Primitivo p in primitivas)
            {
                int n;
                r.PorTipo.TryGetValue(p.Tipo, out n);
                r.PorTipo[p.Tipo] = n + 1;
                if (p.Tipo == Tipo.EdificioAfueras)
                {
                    if (p.TienePendiente)
                    {
                        r.AfuerasAnotados++;
                    }
                    else
                    {
                        r.AfuerasSinModelo++;
                        r.RutasSinModelo.Add(p.Ruta);
                    }
                }
            }
            return r;
        }

        private static Dictionary<string, List<ReemplazarEdificiosT1502.Medida>> CargarPaletas()
        {
            Dictionary<string, ReemplazarEdificiosT1502.Medida> cache = new Dictionary<string, ReemplazarEdificiosT1502.Medida>();
            Dictionary<string, List<ReemplazarEdificiosT1502.Medida>> r = new Dictionary<string, List<ReemplazarEdificiosT1502.Medida>>();
            foreach (string barrio in CalculoEdificiosT1512.BarriosConPaleta())
            {
                r[barrio] = ReemplazarEdificiosT1502.Medidas(CalculoEdificiosT1512.RutasPara(barrio), cache);
            }
            r[""] = ReemplazarEdificiosT1502.Medidas(CalculoEdificiosT1512.RutasPara(""), cache);
            return r;
        }
    }
}
