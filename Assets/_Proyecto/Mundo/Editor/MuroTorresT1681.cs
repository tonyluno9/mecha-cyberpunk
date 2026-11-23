using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.Mundo.Editor
{
    /// <summary>
    /// T35 (T-1681): une cada torre MU02 del muro con sus tramos vecinos, sin rendijas visibles ni pasables.
    /// - Diagnostico: por torre (junto a puertas y en esquinas) mide la separacion entre su malla y los tramos vecinos:
    ///   rendijas visibles (rayos contra copias con MeshCollider de las mallas de alrededor, desde adentro a 1.7 m, en varios angulos)
    ///   y pasables (CheckCapsule del jugador r 0.35 h 1.8 y raycasts horizontales cada 0.25 m contra la fisica real). Logs/t1681-torres.txt.
    /// - Aplicar: cierra cada rendija con tramos MU01 (el mismo modelo del muro) y su collider, y guarda.
    /// - Fotos: capturas a la altura del jugador de cada union torre-puerta en docs/capturas/_tareas/T35/ (corrida APARTE).
    /// </summary>
    public static class MuroTorresT1681
    {
        public const string RutaLog = "Logs/t1681-torres.txt";
        public const string CarpetaFotos = "docs/capturas/_tareas/T35";
        public const string Etiqueta = "[T-1681] ";
        public const string PrefijoPieza = "UnionTorre_T1681_";

        /// <summary>Rendija visible minima que cuenta (m).</summary>
        public const float RendijaMin = 0.05f;
        public const float RadioJugador = 0.35f;
        public const float AltoJugador = 1.8f;

        private const float PasoFino = 0.02f;
        private const float PasoRayo = 0.25f;
        private const float Margen = 5f;          // cuanto se mira a cada lado de la torre (m)
        private const float DistOjo = 8f;         // de adentro del muro a la linea (m)
        private static readonly float[] AlturasOjo = { 0.5f, 1.0f, 1.7f, 3.0f };
        private static readonly float[] Angulos = { 0f, 30f, -30f, 55f, -55f };

        public class Rendija
        {
            public string Torre;
            public string Lado;
            public float TIni;
            public float TFin;
            public float Angulo;
            public float Altura;
            public float Ancho => TFin - TIni;
            public Vector3 Centro;
        }

        public class Medida
        {
            public int Torres;
            public List<Rendija> Visibles = new List<Rendija>();
            public List<string> Pasables = new List<string>();
            public List<string> RayosLibres = new List<string>();
            public int Rayos;
            public int Capsulas;
            public StringBuilder Detalle = new StringBuilder();
        }

        // ------------------------------------------------------------------ entradas

        [MenuItem("Mecha Cyberpunk/T-1681 Muro torres: diagnostico")]
        public static void Diagnostico()
        {
            ReemplazarEdificiosT1502.Abrir();
            Medida m = Medir();
            Escribir(Informe("DIAGNOSTICO", m));
            Debug.Log(Etiqueta + "diagnostico: " + m.Torres + " torres, " + m.Visibles.Count + " rendijas visibles > " + RendijaMin + " m, " + m.Pasables.Count
                + " capsulas libres, " + m.RayosLibres.Count + " rayos libres; ver " + RutaLog);
        }

        /// <summary>Cuanto entra cada tramo MU01 dentro de la torre vecina (m): el cuerpo de MU02 es mas angosto que su base y deja rendija.</summary>
        public const float SolapeTorre = 1.8f;
        /// <summary>Cuanto entra cada tramo MU01 en el marco de la puerta MU03 vecina (m).</summary>
        public const float SolapePuerta = 0.25f;
        private const float TolPegado = 0.15f;

        [MenuItem("Mecha Cyberpunk/T-1681 Muro torres: aplicar")]
        public static void Aplicar()
        {
            Scene escena = ReemplazarEdificiosT1502.Abrir();
            Medida antes = Medir();
            StringBuilder log = new StringBuilder();
            MuroPerimetroT1601.Resultado r = MuroPerimetroT1601.Analizar();
            int alargados = 0;
            int colliders = 0;
            HashSet<Transform> tocados = new HashSet<Transform>();
            foreach (MuroPerimetroT1601.Pieza otra in TorresPorLado(r).Concat(r.Piezas.Where(p => p.Tipo == MuroPerimetroT1601.Tipo.Puerta)))
            {
                CalculoMuroT1524.Lado lado = r.Lados[otra.Lado];
                float solape = otra.Tipo == MuroPerimetroT1601.Tipo.Torre ? SolapeTorre : SolapePuerta;
                foreach (MuroPerimetroT1601.Pieza tramo in r.Piezas.Where(p => p.Tipo == MuroPerimetroT1601.Tipo.Tramo && p.Lado == otra.Lado))
                {
                    int haciaFin;
                    if (Mathf.Abs(tramo.Fin - otra.Ini) < TolPegado)
                    {
                        haciaFin = 1;
                    }
                    else if (Mathf.Abs(tramo.Ini - otra.Fin) < TolPegado)
                    {
                        haciaFin = -1;
                    }
                    else
                    {
                        continue;
                    }
                    Alargar(tramo.T, lado, haciaFin, solape, log, otra.T.name);
                    alargados++;
                    tocados.Add(tramo.T);
                }
            }
            // collider del mismo modelo en cada tramo alargado (las piezas de T-1513 venian con los colliders apagados)
            foreach (Transform t in tocados)
            {
                foreach (Collider c in t.GetComponentsInChildren<Collider>(true))
                {
                    if (!c.enabled)
                    {
                        c.enabled = true;
                        colliders++;
                    }
                }
            }
            Physics.SyncTransforms();
            Medida despues = Medir();
            Escribir(Informe("ANTES de aplicar", antes) + "\n=== CAMBIOS ===\nTramos MU01 alargados: " + alargados + " (colliders prendidos: " + colliders + ")\n" + log + "\n"
                + Informe("DESPUES de aplicar", despues));
            EditorSceneManager.MarkSceneDirty(escena);
            EditorSceneManager.SaveScene(escena);
            Debug.Log(Etiqueta + "aplicar: " + alargados + " tramos alargados, " + colliders + " colliders; rendijas visibles " + antes.Visibles.Count + " -> " + despues.Visibles.Count
                + ", capsulas libres " + antes.Pasables.Count + " -> " + despues.Pasables.Count + ", rayos libres " + antes.RayosLibres.Count + " -> " + despues.RayosLibres.Count);
        }

        /// <summary>Estira el tramo por su eje largo <paramref name="metros"/> hacia su extremo final (haciaFin 1) o inicial (-1); el otro extremo no se mueve.</summary>
        private static void Alargar(Transform t, CalculoMuroT1524.Lado lado, int haciaFin, float metros, StringBuilder log, string contra)
        {
            float ini;
            float fin;
            Extremos(t, lado, out ini, out fin);
            float largo = fin - ini;
            Vector3[] ejes = { t.right, t.up, t.forward };
            int eje = 0;
            for (int i = 1; i < 3; i++)
            {
                if (Mathf.Abs(Vector3.Dot(ejes[i], lado.Eje)) > Mathf.Abs(Vector3.Dot(ejes[eje], lado.Eje)))
                {
                    eje = i;
                }
            }
            Vector3 escala = t.localScale;
            escala[eje] *= (largo + metros) / largo;
            t.localScale = escala;
            float ini2;
            float fin2;
            Extremos(t, lado, out ini2, out fin2);
            float mover = haciaFin > 0 ? ini - ini2 : fin - fin2;
            t.position += lado.Eje * mover;
            Extremos(t, lado, out ini2, out fin2);
            log.AppendLine("  " + lado.Nombre + " " + t.name + " junto a " + contra + ": t " + ini.ToString("F2") + ".." + fin.ToString("F2") + " -> " + ini2.ToString("F2") + ".." + fin2.ToString("F2"));
        }

        private static void Extremos(Transform t, CalculoMuroT1524.Lado lado, out float ini, out float fin)
        {
            Renderer[] rs = t.GetComponentsInChildren<Renderer>();
            Bounds b = rs[0].bounds;
            foreach (Renderer rd in rs)
            {
                b.Encapsulate(rd.bounds);
            }
            float c = Vector3.Dot(b.center - lado.Centro, lado.Eje);
            float m = Mathf.Abs(lado.Eje.x) * b.extents.x + Mathf.Abs(lado.Eje.z) * b.extents.z;
            ini = c - m;
            fin = c + m;
        }

        [MenuItem("Mecha Cyberpunk/T-1681 Muro torres: fotos")]
        public static void Fotos()
        {
            ReemplazarEdificiosT1502.Abrir();
            Directory.CreateDirectory(CarpetaFotos);
            MuroPerimetroT1601.Resultado r = MuroPerimetroT1601.Analizar();
            int n = 0;
            foreach (MuroPerimetroT1601.Pieza torre in Torres(r))
            {
                CalculoMuroT1524.Lado lado = r.Lados[torre.Lado];
                MuroPerimetroT1601.Pieza puerta = r.Piezas.Where(p => p.Tipo == MuroPerimetroT1601.Tipo.Puerta && p.Lado == torre.Lado)
                    .OrderBy(p => Mathf.Min(Mathf.Abs(p.Ini - torre.Fin), Mathf.Abs(torre.Ini - p.Fin))).FirstOrDefault();
                if (puerta == null || Mathf.Min(Mathf.Abs(puerta.Ini - torre.Fin), Mathf.Abs(torre.Ini - puerta.Fin)) > 5f)
                {
                    continue;
                }
                // las dos uniones de la torre, cada una vista desde adentro en diagonal (como en la captura 06), a la altura del jugador:
                // "puerta" = la union con el tramo que va a la puerta; "muro" = la union con el muro que sigue
                bool puertaAntes = puerta.Fin <= torre.Ini + 1f;
                float bordePuerta = puertaAntes ? torre.Ini : torre.Fin;
                float bordeMuro = puertaAntes ? torre.Fin : torre.Ini;
                float s = puertaAntes ? -1f : 1f; // de la torre hacia la puerta
                Foto("torre_" + n + "_" + lado.Nombre + "_" + torre.T.name + "_puerta", lado.Punto(bordePuerta + s * 5f) - lado.Fuera * 5f + Vector3.up * 1.7f,
                    lado.Punto(bordePuerta) + Vector3.up * 2.2f, 70f);
                Foto("torre_" + n + "_" + lado.Nombre + "_" + torre.T.name + "_muro", lado.Punto(bordeMuro - s * 5f) - lado.Fuera * 5f + Vector3.up * 1.7f,
                    lado.Punto(bordeMuro) + Vector3.up * 2.2f, 70f);
                n++;
            }
            // esquinas: desde adentro a la altura del jugador y desde arriba
            int k = 0;
            foreach (MuroPerimetroT1601.Pieza torre in Torres(r).Where(p => p.T.name.StartsWith("TorreEsquina")))
            {
                Bounds b = new Bounds(torre.T.position, Vector3.zero);
                foreach (Renderer rd in torre.T.GetComponentsInChildren<Renderer>())
                {
                    b.Encapsulate(rd.bounds);
                }
                Vector3 c = new Vector3(b.center.x, 0f, b.center.z);
                Vector3 dentro = new Vector3(-Mathf.Sign(c.x), 0f, -Mathf.Sign(c.z));
                Foto("esquina_" + k + "_" + torre.T.name + "_pie", c + dentro * 9f + Vector3.up * 1.7f, c + Vector3.up * 2.5f, 75f);
                Foto("esquina_" + k + "_" + torre.T.name + "_arriba", c + dentro * 3f + Vector3.up * 40f, c, 50f);
                k++;
            }
            Debug.Log(Etiqueta + "fotos: " + n + " uniones torre-puerta, " + k + " esquinas");
        }

        // ------------------------------------------------------------------ medicion

        public static List<MuroPerimetroT1601.Pieza> Torres(MuroPerimetroT1601.Resultado r)
        {
            return r.Piezas.Where(p => p.Tipo == MuroPerimetroT1601.Tipo.Torre).OrderBy(p => p.Lado).ThenBy(p => p.Ini).ToList();
        }

        /// <summary>Cada torre medida en cada lado cuya linea pasa por ella: las de esquina cuentan en los dos lados que juntan.</summary>
        public static List<MuroPerimetroT1601.Pieza> TorresPorLado(MuroPerimetroT1601.Resultado r)
        {
            List<MuroPerimetroT1601.Pieza> lista = new List<MuroPerimetroT1601.Pieza>();
            foreach (MuroPerimetroT1601.Pieza torre in Torres(r))
            {
                Renderer[] rs = torre.T.GetComponentsInChildren<Renderer>();
                Bounds b = rs[0].bounds;
                foreach (Renderer rd in rs)
                {
                    b.Encapsulate(rd.bounds);
                }
                for (int i = 0; i < r.Lados.Length; i++)
                {
                    CalculoMuroT1524.Lado lado = r.Lados[i];
                    float cP = Vector3.Dot(b.center - lado.Centro, lado.Fuera);
                    float cT = Vector3.Dot(b.center - lado.Centro, lado.Eje);
                    if (Mathf.Abs(cP) > 1f || Mathf.Abs(cT) > lado.Mitad + 1f)
                    {
                        continue;
                    }
                    float mitadT = Mathf.Abs(lado.Eje.x) * b.extents.x + Mathf.Abs(lado.Eje.z) * b.extents.z;
                    float mitadP = Mathf.Abs(lado.Fuera.x) * b.extents.x + Mathf.Abs(lado.Fuera.z) * b.extents.z;
                    lista.Add(new MuroPerimetroT1601.Pieza
                    {
                        T = torre.T, Tipo = MuroPerimetroT1601.Tipo.Torre, Lado = i, Ini = cT - mitadT, Fin = cT + mitadT, Perp = cP, Grosor = mitadP * 2f,
                        BaseY = b.min.y, TopY = b.max.y,
                    });
                }
            }
            return lista.OrderBy(p => p.Lado).ThenBy(p => p.Ini).ToList();
        }

        public static Medida Medir()
        {
            MuroPerimetroT1601.Resultado r = MuroPerimetroT1601.Analizar();
            Medida m = new Medida();
            List<MuroPerimetroT1601.Pieza> torres = TorresPorLado(r);
            m.Torres = Torres(r).Count;
            Physics.SyncTransforms();
            foreach (MuroPerimetroT1601.Pieza torre in torres)
            {
                CalculoMuroT1524.Lado lado = r.Lados[torre.Lado];
                string nombre = lado.Nombre + " " + torre.T.name;
                List<MuroPerimetroT1601.Pieza> vecinas = r.Piezas.Where(p => p != torre && p.Lado == torre.Lado && p.Fin > torre.Ini - Margen && p.Ini < torre.Fin + Margen)
                    .OrderBy(p => p.Ini).ToList();
                m.Detalle.AppendLine(nombre + ": t " + torre.Ini.ToString("F2") + ".." + torre.Fin.ToString("F2") + " perp " + torre.Perp.ToString("F2") + " grosor " + torre.Grosor.ToString("F2")
                    + " alto " + (torre.TopY - torre.BaseY).ToString("F1"));
                foreach (MuroPerimetroT1601.Pieza v in vecinas)
                {
                    float sep = v.Ini >= torre.Ini ? v.Ini - torre.Fin : torre.Ini - v.Fin;
                    m.Detalle.AppendLine("   vecina " + v.T.parent.name + "/" + v.T.name + " t " + v.Ini.ToString("F2") + ".." + v.Fin.ToString("F2") + " perp " + v.Perp.ToString("F2")
                        + " grosor " + v.Grosor.ToString("F2") + " sep(caja) " + sep.ToString("F2"));
                }
                // tramo del lado que se revisa: la torre y 5 m a cada lado, sin el vano de las puertas
                // en las esquinas no se pasa de la linea del lado de junto (mas alla de la esquina ya es afuera o el otro lado)
                float t0 = Mathf.Max(torre.Ini - Margen, -lado.Mitad + 1f);
                float t1 = Mathf.Min(torre.Fin + Margen, lado.Mitad - 1f);
                List<Vector2> vanos = r.Piezas.Where(p => p.Tipo == MuroPerimetroT1601.Tipo.Puerta && p.Lado == torre.Lado).Select(p => new Vector2(p.Ini + 0.3f, p.Fin - 0.3f)).ToList();
                System.Func<float, bool> enVano = t => vanos.Any(v => t > v.x && t < v.y);

                MedirVisibles(m, torre, lado, nombre, t0, t1, enVano);
                MedirPasables(m, lado, nombre, t0, t1, enVano);
            }
            // sin repetir la misma rendija contada desde dos torres vecinas
            m.Visibles = m.Visibles.GroupBy(v => v.Lado + Mathf.Round(v.TIni * 10f) + "_" + v.Angulo + "_" + v.Altura).Select(g => g.First()).ToList();
            m.Pasables = m.Pasables.Distinct().ToList();
            m.RayosLibres = m.RayosLibres.Distinct().ToList();
            return m;
        }

        /// <summary>Copia las mallas cercanas con MeshCollider en una escena aparte y lanza rayos desde adentro: los que no chocan son rendijas visibles.</summary>
        private static void MedirVisibles(Medida m, MuroPerimetroT1601.Pieza torre, CalculoMuroT1524.Lado lado, string nombre, float t0, float t1, System.Func<float, bool> enVano)
        {
            Scene previa = EditorSceneManager.NewPreviewScene();
            try
            {
                PhysicsScene fisica = previa.GetPhysicsScene();
                Vector3 a = lado.Punto(t0);
                Vector3 b = lado.Punto(t1);
                Bounds zona = new Bounds((a + b) * 0.5f + Vector3.up * 8f, Vector3.zero);
                zona.Encapsulate(a - lado.Fuera * (DistOjo + 12f));
                zona.Encapsulate(b + lado.Fuera * 8f + Vector3.up * 16f);
                zona.Encapsulate(a + lado.Fuera * 8f - Vector3.up * 2f);
                zona.Encapsulate(b - lado.Fuera * (DistOjo + 12f));
                zona.Expand(new Vector3(12f, 0f, 12f));
                int copias = 0;
                foreach (MeshRenderer rd in Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                {
                    if (!rd.enabled || !rd.bounds.Intersects(zona))
                    {
                        continue;
                    }
                    MeshFilter mf = rd.GetComponent<MeshFilter>();
                    if (mf == null || mf.sharedMesh == null)
                    {
                        continue;
                    }
                    GameObject g = new GameObject("c");
                    SceneManager.MoveGameObjectToScene(g, previa);
                    g.transform.SetPositionAndRotation(rd.transform.position, rd.transform.rotation);
                    g.transform.localScale = rd.transform.lossyScale;
                    g.AddComponent<MeshCollider>().sharedMesh = mf.sharedMesh;
                    copias++;
                }
                Physics.SyncTransforms();
                foreach (float ang in Angulos)
                {
                    Vector3 dir = Quaternion.AngleAxis(ang, Vector3.up) * lado.Fuera;
                    float dist = DistOjo / Mathf.Cos(ang * Mathf.Deg2Rad);
                    foreach (float h in AlturasOjo)
                    {
                        float abierto = float.NaN;
                        for (float t = t0; t <= t1 + 0.001f; t += PasoFino)
                        {
                            bool libre = !enVano(t);
                            if (libre)
                            {
                                Vector3 cruce = lado.Punto(t) + Vector3.up * h;
                                Vector3 o = cruce - dir * dist;
                                libre = !fisica.Raycast(o, dir, dist + 8f, ~0, QueryTriggerInteraction.Ignore)
                                    && !fisica.Raycast(cruce + dir * 8f, -dir, dist + 8f, ~0, QueryTriggerInteraction.Ignore);
                            }
                            if (libre && float.IsNaN(abierto))
                            {
                                abierto = t;
                            }
                            else if (!libre && !float.IsNaN(abierto))
                            {
                                Agregar(m, nombre, lado, abierto, t, ang, h);
                                abierto = float.NaN;
                            }
                        }
                        if (!float.IsNaN(abierto))
                        {
                            Agregar(m, nombre, lado, abierto, t1, ang, h);
                        }
                    }
                }
                m.Detalle.AppendLine("   (mallas copiadas para los rayos: " + copias + ")");
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(previa);
            }
        }

        private static void Agregar(Medida m, string nombre, CalculoMuroT1524.Lado lado, float ini, float fin, float ang, float h)
        {
            if (fin - ini <= RendijaMin + 0.001f)
            {
                return;
            }
            m.Visibles.Add(new Rendija { Torre = nombre, Lado = lado.Nombre, TIni = ini, TFin = fin, Angulo = ang, Altura = h, Centro = lado.Punto((ini + fin) * 0.5f) });
        }

        /// <summary>Contra la fisica real: capsula del jugador sobre la linea del muro y rayos horizontales de adentro hacia afuera cada 0.25 m.</summary>
        private static void MedirPasables(Medida m, CalculoMuroT1524.Lado lado, string nombre, float t0, float t1, System.Func<float, bool> enVano)
        {
            for (float t = t0; t <= t1 + 0.001f; t += PasoRayo)
            {
                Vector3 p = lado.Punto(t);
                float piso = PisoEn(p);
                foreach (float perp in new[] { -0.6f, 0f, 0.6f })
                {
                    Vector3 c = p + lado.Fuera * perp;
                    Vector3 abajo = new Vector3(c.x, piso + 0.15f + RadioJugador, c.z);
                    Vector3 arriba = new Vector3(c.x, piso + 0.15f + AltoJugador - RadioJugador, c.z);
                    m.Capsulas++;
                    if (!Physics.CheckCapsule(abajo, arriba, RadioJugador, ~0, QueryTriggerInteraction.Ignore))
                    {
                        m.Pasables.Add(nombre + " t " + t.ToString("F2") + " perp " + perp.ToString("F1") + " " + c.ToString("F1"));
                    }
                }
                if (enVano(t))
                {
                    continue; // la puerta tiene su collider solido; igual se cuenta con la capsula
                }
                foreach (float h in new[] { 0.5f, 1.0f, 1.7f })
                {
                    m.Rayos++;
                    Vector3 o = p - lado.Fuera * 4f + Vector3.up * (piso + h);
                    if (!Physics.Raycast(o, lado.Fuera, 10f, ~0, QueryTriggerInteraction.Ignore))
                    {
                        m.RayosLibres.Add(nombre + " t " + t.ToString("F2") + " h " + h + " piso " + piso.ToString("F2"));
                        if (m.RayosLibres.Count(x => x.StartsWith(nombre + " ")) == 1)
                        {
                            Vector3 o2 = o - lado.Fuera * 10f;
                            string hits = string.Join(", ", Physics.RaycastAll(o2, lado.Fuera, 40f, ~0, QueryTriggerInteraction.Ignore).OrderBy(x => x.distance)
                                .Select(x => x.collider.name + "(" + (x.collider.transform.parent != null ? x.collider.transform.parent.name : "") + ")@" + (x.distance - 10f).ToString("F1")));
                            string dentro = string.Join(", ", Physics.OverlapSphere(o, 0.1f, ~0, QueryTriggerInteraction.Ignore).Select(x => x.name));
                            m.Detalle.AppendLine("   rayo libre en " + o.ToString("F1") + " -> hits desde 10 m antes: " + hits + " | origen dentro de: " + dentro);
                        }
                    }
                }
            }
        }

        private static float PisoEn(Vector3 p)
        {
            float y = float.MinValue;
            foreach (RaycastHit h in Physics.RaycastAll(new Vector3(p.x, 30f, p.z), Vector3.down, 80f, ~0, QueryTriggerInteraction.Ignore))
            {
                if (h.normal.y > 0.5f && h.point.y <= 1f && h.point.y > y)
                {
                    y = h.point.y;
                }
            }
            return y == float.MinValue ? 0f : y;
        }

        // ------------------------------------------------------------------ informe y fotos

        private static string Informe(string titulo, Medida m)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("=== T-1681 " + titulo + " (" + System.DateTime.Now.ToString("yyyy-MM-dd HH:mm") + ") ===");
            sb.AppendLine("Torres: " + m.Torres + "; rendijas visibles > " + RendijaMin + " m: " + m.Visibles.Count + "; capsulas libres: " + m.Pasables.Count + " de " + m.Capsulas
                + "; rayos libres: " + m.RayosLibres.Count + " de " + m.Rayos);
            foreach (IGrouping<string, Rendija> g in m.Visibles.GroupBy(v => v.Torre))
            {
                sb.AppendLine("  " + g.Key + ": " + g.Count() + " rendijas");
                foreach (Rendija v in g.OrderBy(v => v.TIni).Take(25))
                {
                    sb.AppendLine("     t " + v.TIni.ToString("F2") + ".." + v.TFin.ToString("F2") + " (" + v.Ancho.ToString("F2") + " m) ang " + v.Angulo + " h " + v.Altura + " en " + v.Centro.ToString("F1"));
                }
            }
            foreach (string s in m.Pasables)
            {
                sb.AppendLine("  capsula libre: " + s);
            }
            foreach (string s in m.RayosLibres)
            {
                sb.AppendLine("  rayo libre: " + s);
            }
            sb.AppendLine("--- detalle por torre ---");
            sb.Append(m.Detalle);
            return sb.ToString();
        }

        private static void Escribir(string texto)
        {
            Directory.CreateDirectory("Logs");
            File.AppendAllText(RutaLog, texto + "\n");
        }

        private static void Foto(string nombre, Vector3 pos, Vector3 mira, float fov)
        {
            const int Ancho = 1280;
            const int Alto = 720;
            GameObject go = new GameObject("CamFotosT1681");
            Camera cam = go.AddComponent<Camera>();
            cam.fieldOfView = fov;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 1500f;
            go.transform.position = pos;
            go.transform.LookAt(mira);
            RenderTexture rt = new RenderTexture(Ancho, Alto, 24);
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            Texture2D tex = new Texture2D(Ancho, Alto, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, Ancho, Alto), 0, 0);
            tex.Apply();
            File.WriteAllBytes(Path.Combine(CarpetaFotos, nombre + ".png"), tex.EncodeToPNG());
            RenderTexture.active = null;
            cam.targetTexture = null;
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(tex);
            Object.DestroyImmediate(go);
        }
    }
}
