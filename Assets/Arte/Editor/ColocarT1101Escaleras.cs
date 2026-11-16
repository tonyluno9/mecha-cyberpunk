using System.Collections.Generic;
using System.IO;
using System.Linq;
using MechaCyberpunk.Core;
using MechaCyberpunk.Core.Trepar;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-1101: escaleras de mano nuevas en casas de 1 a 3 pisos sin acceso a la azotea. Sondea por física las paredes de las casas de
    /// S2c_Suburbios, S2c_CalleDorada, S2c_Mercado y Barrio, y elige sitios con pared lisa (sin puerta, ventana ni props encima), banqueta
    /// libre delante, techo plano con piso y espacio arriba, lejos de puertas interactuables y de otros accesos. Cada escalera lleva un
    /// modelo visible (mismo greybox metálico de ColocarS2Techos) y un <see cref="PuntoTrepar"/> orientado hacia la pared.
    /// Idempotente: rehace su raíz <see cref="Raiz"/> y sus mallas en <see cref="RutaMallas"/>. Se corre después de ColocarS2Techos.
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ColocarT1101Escaleras.Colocar
    /// </summary>
    public static class ColocarT1101Escaleras
    {
        public const string Raiz = "T1101_Escaleras";
        private const string RutaCarpeta = "Assets/Arte/Mundo/S2b/";
        private const string RutaMallas = RutaCarpeta + "T1101Mallas";
        private const string RutaMaterial = RutaCarpeta + "S2b_EscaleraGreybox.mat";

        private const float MinPuerta = 4f;
        private const float MinOtroAcceso = 5f;
        private const float MinSolo = 6f;
        private const float AlturaMinima = 2.6f;
        private const float AlturaMaxima = 11f;
        private const float SueloMaxY = 1.2f;
        private const float FondoBanqueta = 1.7f;
        private const float MitadAncho = 0.5f;
        private const float PasoMuestra = 0.75f;
        private const float PasoEscalera = 0.3f;
        private const float XCalleDoc = -110.5f;
        private const float MargenCalleDoc = 8f;
        private const int MetaPorZona = 5;
        private const float TolTecho = 0.1f;

        private static readonly Vector3[] Dirs = { Vector3.right, Vector3.left, Vector3.forward, Vector3.back };
        private static readonly string[] Zonas = { "S2c_Suburbios", "S2c_CalleDorada", "S2c_Mercado", "Barrio" };

        private sealed class Techo
        {
            public Transform casa;
            public Bounds b;
            public float top;
            public string zona;
            public bool usado;
        }

        private sealed class Candidato
        {
            public Techo techo;
            public Vector3 n;
            public Vector3 w;
            public float y0;
            public float alto;
        }

        private static List<Bounds> obstaculos;
        private static List<string> nombresObstaculos;
        private static List<Transform> duenosObstaculos;
        private static readonly Dictionary<string, int> bloqueadores = new Dictionary<string, int>();
        private static List<Vector3> puertas;
        private static List<Vector3> accesos;
        private static int contadorMallas;

        [MenuItem("Mecha Cyberpunk/S2/T-1101 Escaleras de mano en casas")]
        public static void Colocar()
        {
            ColocarS2.Informe.Clear();
            ColocarS2.Abrir();
            foreach (GameObject g in EditorSceneManager.GetActiveScene().GetRootGameObjects().Where(g => g.name == Raiz).ToList())
            {
                Object.DestroyImmediate(g);
            }
            Directory.CreateDirectory(RutaCarpeta);
            if (AssetDatabase.IsValidFolder(RutaMallas))
            {
                AssetDatabase.DeleteAsset(RutaMallas);
            }
            AssetDatabase.CreateFolder(RutaCarpeta.TrimEnd('/'), "T1101Mallas");
            Transform raiz = new GameObject(Raiz).transform;
            Physics.SyncTransforms();

            List<Techo> techos = RecogerTechos();
            Reunir();
            List<Candidato> candidatos = Sondear(techos);
            ColocarS2.Informe.AppendLine("Candidatos con pared lisa, banqueta, techo con piso y lejos de puertas: " + candidatos.Count);

            Material mat = AssetDatabase.LoadAssetAtPath<Material>(RutaMaterial);
            Dictionary<string, int> porZona = Zonas.ToDictionary(z => z, z => 0);
            List<Vector3> nuevos = new List<Vector3>();
            bool avanzo = true;
            while (avanzo)
            {
                avanzo = false;
                foreach (string zona in Zonas)
                {
                    if (porZona[zona] >= MetaPorZona)
                    {
                        continue;
                    }
                    Candidato mejor = null;
                    float mejorDist = 0f;
                    foreach (Candidato c in candidatos.Where(c => c.techo.zona == zona && !c.techo.usado))
                    {
                        float d = DistanciaMinima(c.w, nuevos, accesos);
                        if (d >= MinOtroAcceso && DistanciaMinima(c.w, nuevos, null) >= MinSolo && d > mejorDist)
                        {
                            mejorDist = d;
                            mejor = c;
                        }
                    }
                    if (mejor == null)
                    {
                        continue;
                    }
                    mejor.techo.usado = true;
                    porZona[zona]++;
                    nuevos.Add(mejor.w);
                    Crear(raiz, "Escalera_" + zona + "_" + porZona[zona].ToString("00"), mejor, mat);
                    ColocarS2.Informe.AppendLine("Escalera " + zona + " en " + mejor.techo.casa.parent.name + "/" + mejor.techo.casa.name + " " + mejor.w.ToString("F1") + " alto " + mejor.alto.ToString("F2"));
                    avanzo = true;
                }
            }
            ColocarS2.Informe.AppendLine("Escaleras nuevas: " + nuevos.Count + " (" + string.Join(", ", porZona.Select(k => k.Key + "=" + k.Value)) + ")");
            ColocarS2.Guardar("t1101-escaleras");
        }

        private static float DistanciaMinima(Vector3 p, List<Vector3> a, List<Vector3> b)
        {
            float m = float.MaxValue;
            foreach (List<Vector3> lista in new[] { a, b })
            {
                if (lista == null)
                {
                    continue;
                }
                foreach (Vector3 q in lista)
                {
                    m = Mathf.Min(m, Vector2.Distance(new Vector2(p.x, p.z), new Vector2(q.x, q.z)));
                }
            }
            return m;
        }

        // ------------------------------------------------------------------ datos de la escena

        private static List<Techo> RecogerTechos()
        {
            List<Techo> lista = new List<Techo>();
            foreach (GameObject g in EditorSceneManager.GetActiveScene().GetRootGameObjects())
            {
                if (!Zonas.Contains(g.name))
                {
                    continue;
                }
                bool casas = g.name != "Barrio";
                foreach (BoxCollider c in g.GetComponentsInChildren<BoxCollider>(false))
                {
                    if (!c.enabled || c.isTrigger || c.transform.parent == null || c.transform.parent == g.transform)
                    {
                        continue;
                    }
                    Bounds b = c.bounds;
                    bool vale = casas
                        ? c.name == "Cuerpo" && c.transform.parent.name.StartsWith("Casa_")
                        : c.name.StartsWith("Cuerpo") && c.transform.parent.name.StartsWith("Edificio_") && b.max.y <= 11.01f && b.max.y >= 3f && b.size.x >= 3f && b.size.z >= 3f;
                    if (!vale)
                    {
                        continue;
                    }
                    Renderer r = c.GetComponent<Renderer>();
                    float top = r != null ? r.bounds.max.y : b.max.y;
                    lista.Add(new Techo { casa = c.transform.parent, b = b, top = top, zona = g.name });
                }
            }
            // Casas contenidas en otras (duplicadas): se descartan las menores.
            return lista.Where(t => !lista.Any(o => o != t && o.top >= t.top - 0.01f && Contiene(o.b, t.b))).ToList();
        }

        private static bool Contiene(Bounds grande, Bounds chico)
        {
            float dx = Mathf.Min(grande.max.x, chico.max.x) - Mathf.Max(grande.min.x, chico.min.x);
            float dz = Mathf.Min(grande.max.z, chico.max.z) - Mathf.Max(grande.min.z, chico.min.z);
            return dx > 0f && dz > 0f && dx * dz >= 0.7f * chico.size.x * chico.size.z && (grande.size.x * grande.size.z > chico.size.x * chico.size.z + 0.01f || grande.center != chico.center);
        }

        private static void Reunir()
        {
            obstaculos = new List<Bounds>();
            nombresObstaculos = new List<string>();
            duenosObstaculos = new List<Transform>();
            foreach (Renderer r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (!(r is MeshRenderer || r is SkinnedMeshRenderer) || !r.enabled)
                {
                    continue;
                }
                Bounds b = r.bounds;
                if (b.size.y < 0.5f || b.size.x > 40f || b.size.z > 40f || b.max.y < 0.3f)
                {
                    continue;
                }
                obstaculos.Add(b);
                duenosObstaculos.Add(r.transform);
                nombresObstaculos.Add(r.transform.parent != null ? r.transform.parent.name + "/" + r.name : r.name);
            }
            puertas = new List<Vector3>();
            foreach (MonoBehaviour mb in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (mb is IPuerta p)
                {
                    puertas.Add(p.Posicion);
                }
                else if (mb is IInteractuable i)
                {
                    puertas.Add(i.Posicion);
                }
            }
            accesos = Object.FindObjectsByType<PuntoTrepar>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Select(p => p.transform.position).ToList();
            ColocarS2.Informe.AppendLine("Puertas e interactuables: " + puertas.Count + ", accesos existentes: " + accesos.Count + ", obstáculos visibles: " + obstaculos.Count);
        }

        // ------------------------------------------------------------------ sondeo

        private static List<Candidato> Sondear(List<Techo> techos)
        {
            List<Candidato> lista = new List<Candidato>();
            Dictionary<string, int> rechazos = new Dictionary<string, int>();
            foreach (Techo t in techos)
            {
                if (TieneAcceso(t))
                {
                    Rechazar(rechazos, "casa con acceso");
                    continue;
                }
                for (int lado = 0; lado < 4; lado++)
                {
                    Vector3 n = Dirs[lado];
                    Vector3 u = Vector3.Cross(Vector3.up, n);
                    float centro = Vector3.Dot(t.b.center, u);
                    float mitad = Mathf.Abs(u.x) * t.b.extents.x + Mathf.Abs(u.z) * t.b.extents.z;
                    float cn = Vector3.Dot(t.b.center, n) + Mathf.Abs(n.x) * t.b.extents.x + Mathf.Abs(n.z) * t.b.extents.z;
                    for (float a = centro - mitad + 1f; a <= centro + mitad - 1f; a += PasoMuestra)
                    {
                        Vector3 w = u * a + n * cn;
                        string motivo = Evaluar(t, n, u, w, out float y0, out float alto);
                        if (motivo != null)
                        {
                            Rechazar(rechazos, t.zona + ": " + motivo);
                            continue;
                        }
                        lista.Add(new Candidato { techo = t, n = n, w = new Vector3(w.x, y0, w.z), y0 = y0, alto = alto });
                    }
                }
            }
            ColocarS2.Informe.AppendLine("Bloqueadores: " + string.Join(", ", bloqueadores.OrderByDescending(k => k.Value).Take(12).Select(k => k.Key + "=" + k.Value)));
            ColocarS2.Informe.AppendLine("Candidatos por zona: " + string.Join(", ", lista.GroupBy(c => c.techo.zona).Select(g => g.Key + "=" + g.Count() + " (casas " + g.Select(c => c.techo).Distinct().Count() + ")")));
            ColocarS2.Informe.AppendLine("Rechazos: " + string.Join(", ", rechazos.OrderByDescending(k => k.Value).Take(40).Select(k => k.Key + "=" + k.Value)));
            return lista;
        }

        private static void Rechazar(Dictionary<string, int> d, string motivo)
        {
            d[motivo] = d.TryGetValue(motivo, out int k) ? k + 1 : 1;
        }

        private static bool TieneAcceso(Techo t)
        {
            Bounds ampliado = t.b;
            ampliado.Expand(new Vector3(1.5f, 100f, 1.5f));
            return accesos.Any(p => ampliado.Contains(new Vector3(p.x, ampliado.center.y, p.z)));
        }

        private static bool Suelo(Vector3 p, out float y)
        {
            y = 0f;
            if (!Physics.Raycast(new Vector3(p.x, 2.5f, p.z), Vector3.down, out RaycastHit hit, 5f, ~0, QueryTriggerInteraction.Ignore))
            {
                return false;
            }
            y = hit.point.y;
            return hit.normal.y >= 0.8f && hit.point.y <= SueloMaxY;
        }

        /// <summary>Devuelve null si el sitio sirve, o el motivo del rechazo.</summary>
        private static string Evaluar(Techo t, Vector3 n, Vector3 u, Vector3 w, out float y0, out float alto)
        {
            alto = 0f;
            if (w.x > XCalleDoc - MargenCalleDoc && w.x < XCalleDoc + MargenCalleDoc)
            {
                y0 = 0f;
                return "calle del Doc (T-1102)";
            }
            if (!Suelo(w + n * 0.4f, out y0))
            {
                return "sin suelo";
            }
            foreach (Vector3 p in puertas)
            {
                if (Vector2.Distance(new Vector2(p.x, p.z), new Vector2(w.x, w.z)) < MinPuerta)
                {
                    return "cerca de puerta";
                }
            }
            // Banqueta: suelo parejo en todo el ancho y al menos FondoBanqueta de fondo (deja >= 1,2 m libres de paso).
            for (float o = -MitadAncho; o <= MitadAncho + 0.01f; o += MitadAncho)
            {
                for (float d = 0.4f; d <= FondoBanqueta; d += 0.65f)
                {
                    if (!Suelo(w + u * o + n * d, out float ys) || Mathf.Abs(ys - y0) > 0.12f)
                    {
                        return "banqueta angosta o desnivel";
                    }
                }
            }
            // Techo: se mide el piso real arriba, detrás del muro.
            Vector3 s = w - n * 0.8f;
            if (!Physics.Raycast(new Vector3(s.x, t.top + 1.5f, s.z), Vector3.down, out RaycastHit techo, 3f, ~0, QueryTriggerInteraction.Ignore)
                || techo.normal.y < 0.9f || Mathf.Abs(techo.point.y - t.top) > TolTecho)
            {
                return "techo sin piso en la salida";
            }
            alto = techo.point.y - y0;
            if (alto < AlturaMinima || alto > AlturaMaxima)
            {
                return "altura fuera de rango";
            }
            for (float d = 0.8f; d <= 2.4f; d += 0.8f)
            {
                Vector3 p = w - n * d;
                if (!Physics.Raycast(new Vector3(p.x, t.top + 1.5f, p.z), Vector3.down, out RaycastHit h, 3f, ~0, QueryTriggerInteraction.Ignore) || Mathf.Abs(h.point.y - techo.point.y) > 0.08f)
                {
                    return "techo irregular";
                }
            }
            if (Physics.CheckCapsule(new Vector3(s.x, techo.point.y + 0.4f, s.z), new Vector3(s.x, techo.point.y + 1.6f, s.z), 0.3f, ~0, QueryTriggerInteraction.Ignore))
            {
                return "salida ocupada arriba";
            }
            // Pared lisa: la pared devuelve el rayo a 0,5 m en 3 alturas y 3 posiciones (sin vanos ni salientes).
            foreach (float o in new[] { -MitadAncho, 0f, MitadAncho })
            {
                foreach (float h in new[] { 0.4f, alto * 0.5f, alto - 0.4f })
                {
                    Vector3 origen = w + u * o + n * 0.5f + Vector3.up * (y0 + h);
                    if (!Physics.Raycast(origen, -n, out RaycastHit hit, 0.9f, ~0, QueryTriggerInteraction.Ignore) || Mathf.Abs(hit.distance - 0.5f) > 0.1f)
                    {
                        return "pared irregular (puerta, ventana o saliente)";
                    }
                }
            }
            // Nada visible ni sólido pegado a la pared en todo el alto, ni en la banqueta delante hasta 2,5 m.
            if (Ocupado(t.casa, w, n, -0.05f, 0.4f, y0 + 0.25f, techo.point.y - 0.35f, MitadAncho - 0.05f))
            {
                return "algo en la pared (ventana, aire, letrero)";
            }
            if (Ocupado(t.casa, w, n, 0.1f, FondoBanqueta, y0 + 0.25f, y0 + 2.5f, MitadAncho - 0.05f))
            {
                return "prop en la banqueta";
            }
            return null;
        }

        private static bool Ocupado(Transform casa, Vector3 w, Vector3 n, float d0, float d1, float yMin, float yMax, float mitadLargo)
        {
            // Sólidos: desde 0,1 m de la pared (la pared misma es sólida). Modelos visibles: desde d0, ignorando el cuerpo de la propia casa.
            float dFisico = Mathf.Max(d0, 0.1f);
            Vector3 centro = w + n * ((dFisico + d1) * 0.5f);
            centro.y = (yMin + yMax) * 0.5f;
            Vector3 mitad = Mathf.Abs(n.x) > 0.5f
                ? new Vector3((d1 - dFisico) * 0.5f, (yMax - yMin) * 0.5f, mitadLargo)
                : new Vector3(mitadLargo, (yMax - yMin) * 0.5f, (d1 - dFisico) * 0.5f);
            if (Physics.CheckBox(centro, mitad, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore))
            {
                return true;
            }
            centro = w + n * ((d0 + d1) * 0.5f);
            centro.y = (yMin + yMax) * 0.5f;
            mitad = Mathf.Abs(n.x) > 0.5f
                ? new Vector3((d1 - d0) * 0.5f, (yMax - yMin) * 0.5f, mitadLargo)
                : new Vector3(mitadLargo, (yMax - yMin) * 0.5f, (d1 - d0) * 0.5f);
            Bounds caja = new Bounds(centro, mitad * 2f);
            for (int i = 0; i < obstaculos.Count; i++)
            {
                if (obstaculos[i].Intersects(caja) && !(duenosObstaculos[i].IsChildOf(casa) && duenosObstaculos[i].name.StartsWith("Cuerpo")))
                {
                    string k = nombresObstaculos[i];
                    int cut = k.IndexOf('_');
                    k = k.Length > 40 ? k.Substring(0, 40) : k;
                    bloqueadores[k] = bloqueadores.TryGetValue(k, out int q) ? q + 1 : 1;
                    return true;
                }
            }
            return false;
        }

        // ------------------------------------------------------------------ crear

        private static void Crear(Transform padre, string nombre, Candidato c, Material mat)
        {
            GameObject go = new GameObject(nombre);
            go.transform.SetParent(padre, false);
            go.transform.SetPositionAndRotation(c.w, Quaternion.LookRotation(-c.n));
            PuntoTrepar p = go.AddComponent<PuntoTrepar>();
            SerializedObject so = new SerializedObject(p);
            so.FindProperty("tipo").enumValueIndex = (int)TipoTrepar.Escalera;
            so.FindProperty("alturaSubida").floatValue = c.alto;
            so.FindProperty("salida").vector3Value = new Vector3(0f, c.alto, 0.8f);
            so.ApplyModifiedPropertiesWithoutUndo();
            p.AjustarTrigger();
            MallaEscalera(go.transform, c.alto, mat).name = "EscaleraDeMano";
        }

        private static GameObject MallaEscalera(Transform padre, float alt, Material mat)
        {
            Mesh cubo = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
            List<CombineInstance> partes = new List<CombineInstance>();
            const float distancia = 0.13f;
            float alto = alt + 0.9f;
            foreach (float x in new[] { -0.3f, 0.3f })
            {
                Agregar(partes, cubo, new Vector3(x, alto * 0.5f, -distancia), new Vector3(0.06f, alto, 0.05f));
                for (float y = 0.4f; y < alto; y += 1.5f)
                {
                    Agregar(partes, cubo, new Vector3(x, y, -distancia * 0.5f), new Vector3(0.04f, 0.04f, distancia));
                }
            }
            for (float y = PasoEscalera; y < alto; y += PasoEscalera)
            {
                Agregar(partes, cubo, new Vector3(0f, y, -distancia), new Vector3(0.6f, 0.03f, 0.03f));
            }
            Mesh m = new Mesh();
            m.CombineMeshes(partes.ToArray(), true, true);
            m.name = "escalera_" + (++contadorMallas).ToString("000");
            AssetDatabase.CreateAsset(m, RutaMallas + "/" + m.name + ".asset");
            GameObject go = new GameObject("Visual");
            go.transform.SetParent(padre, false);
            go.AddComponent<MeshFilter>().sharedMesh = m;
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            return go;
        }

        private static void Agregar(List<CombineInstance> partes, Mesh malla, Vector3 centro, Vector3 escala)
        {
            partes.Add(new CombineInstance { mesh = malla, transform = Matrix4x4.TRS(centro, Quaternion.identity, escala) });
        }
    }
}
