using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-1405: las "cintas negras" de K13 son cables normales muy cerca de la camara. Diagnosticar mide la distancia de cada segmento de cable a la camara
    /// (Logs/t1405-diagnostico.txt) y lista los renderers en la esquina superior derecha (el triangulo claro). Fotos mueve la camara a un punto sin cables a menos de 4 m
    /// (Logs/t1405-camara.txt; constantes CamaraDesde/CamaraHacia) y toma docs/capturas/t-1405/cerro-k13.png (corrida aparte).
    /// Aplicar solo apaga (reversible, Logs/t1405-apagados.txt) cuadros planos sueltos en esa esquina; no toca cables.
    /// Orden: Diagnosticar, Aplicar, Fotos (Fotos en otra corrida de Unity).
    /// </summary>
    public static class ZonaCerroT1405
    {
        public const string RutaDiagnostico = "Logs/t1405-diagnostico.txt";
        public const string RutaCamara = "Logs/t1405-camara.txt";
        public const string RutaApagados = "Logs/t1405-apagados.txt";

        public static readonly Vector3 K13Desde = ZonaCerroT1394.K13Desde;
        public static readonly Vector3 K13Hacia = ZonaCerroT1394.K13Hacia;
        public const float CampoFoto = ZonaCerroT1394.CampoFoto;

        /// <summary>Posicion elegida por Fotos()/ElegirCamara(). Las demas fotos del reporte pueden usarlas tras llamar a ElegirCamara().</summary>
        public static Vector3 CamaraDesde = K13Desde;
        public static Vector3 CamaraHacia = K13Hacia;

        private const float DistanciaLibre = 4f;
        private const float Radio = 6f;
        private const float Paso = 1.5f;
        private const float Lejos = 400f;

        private struct Segmento
        {
            public Vector3 A, B;
            public string Ruta, Malla;
        }

        private static float DistSeg(Vector3 p, Vector3 a, Vector3 b)
        {
            Vector3 ab = b - a;
            float l2 = ab.sqrMagnitude;
            float t = l2 < 1e-6f ? 0f : Mathf.Clamp01(Vector3.Dot(p - a, ab) / l2);
            return Vector3.Distance(p, a + ab * t);
        }

        private static bool Finito(Vector3 v)
        {
            return !(float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z) || float.IsInfinity(v.x) || float.IsInfinity(v.y) || float.IsInfinity(v.z));
        }

        private static string RutaCompleta(Transform t)
        {
            StringBuilder sb = new StringBuilder(t.name);
            for (Transform p = t.parent; p != null; p = p.parent)
            {
                sb.Insert(0, p.name + "/");
            }
            return sb.ToString();
        }

        /// <summary>Todos los segmentos de todas las mallas con "Cable" en el nombre del objeto o de la malla (escena, activas o no).</summary>
        private static List<Segmento> Segmentos(StringBuilder sb)
        {
            List<Segmento> lista = new List<Segmento>();
            int mallas = 0, noLegibles = 0;
            foreach (MeshFilter mf in UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                Mesh m = mf.sharedMesh;
                if (m == null || !(mf.name.IndexOf("Cable", StringComparison.OrdinalIgnoreCase) >= 0 || m.name.IndexOf("Cable", StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    continue;
                }
                mallas++;
                if (!m.isReadable)
                {
                    noLegibles++;
                    sb.AppendLine("AVISO: malla no legible " + m.name + " en " + RutaCompleta(mf.transform));
                    continue;
                }
                Vector3[] v = m.vertices;
                string ruta = RutaCompleta(mf.transform);
                if (v.Length % 4 == 0 && v.Length >= 8)
                {
                    // Cables hechos de aros de 4 vertices: segmento = centro de aro k a centro de aro k+1 (si estan a menos de 20 m; si no, es otro cable).
                    int aros = v.Length / 4;
                    Vector3[] c = new Vector3[aros];
                    for (int k = 0; k < aros; k++)
                    {
                        c[k] = mf.transform.TransformPoint((v[4 * k] + v[4 * k + 1] + v[4 * k + 2] + v[4 * k + 3]) * 0.25f);
                    }
                    for (int k = 0; k + 1 < aros; k++)
                    {
                        if (Finito(c[k]) && Finito(c[k + 1]) && Vector3.Distance(c[k], c[k + 1]) < 20f)
                        {
                            lista.Add(new Segmento { A = c[k], B = c[k + 1], Ruta = ruta, Malla = m.name });
                        }
                    }
                }
                // Tambien los vertices sueltos como puntos (cubre mallas que no son aros).
                int salto = Mathf.Max(1, v.Length / 4000);
                for (int i = 0; i < v.Length; i += salto)
                {
                    Vector3 w = mf.transform.TransformPoint(v[i]);
                    if (Finito(w))
                    {
                        lista.Add(new Segmento { A = w, B = w, Ruta = ruta, Malla = m.name });
                    }
                }
            }
            sb.AppendLine("Mallas de cables: " + mallas + " (no legibles " + noLegibles + "), segmentos " + lista.Count + ".");
            return lista;
        }

        private static float MinDistancia(List<Segmento> segs, Vector3 p)
        {
            float min = float.MaxValue;
            foreach (Segmento s in segs)
            {
                min = Mathf.Min(min, DistSeg(p, s.A, s.B));
            }
            return min;
        }

        private static Camera CrearCamara(Vector3 desde, Vector3 hacia)
        {
            GameObject go = new GameObject("CamaraT1405");
            Camera cam = go.AddComponent<Camera>();
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = Lejos;
            cam.fieldOfView = CampoFoto;
            cam.aspect = 1376f / 768f;
            cam.transform.position = desde;
            cam.transform.LookAt(hacia);
            return cam;
        }

        private static bool EnEsquina(Camera cam, Renderer r, out float dist, out Vector3 vp)
        {
            Vector3 c = r.bounds.center;
            dist = Vector3.Distance(c, K13Desde);
            vp = cam.WorldToViewportPoint(c);
            return dist >= 20f && dist <= 600f && vp.z > 0f && vp.x > 0.6f && vp.x <= 1f && vp.y > 0.55f && vp.y <= 1f;
        }

        // ================================================================ Diagnosticar

        [MenuItem("Mecha Cyberpunk/T1405/Cerro camara: diagnosticar")]
        public static void Diagnosticar()
        {
            ColocarS2.Abrir();
            Physics.SyncTransforms();
            CultureInfo ci = CultureInfo.InvariantCulture;
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("T-1405 diagnostico. Camara K13 " + K13Desde + " mira a " + K13Hacia + ", campo " + CampoFoto + ".");
            List<Segmento> segs = Segmentos(sb);
            Camera cam = CrearCamara(K13Desde, K13Hacia);
            Plane[] planos = GeometryUtility.CalculateFrustumPlanes(cam);

            sb.AppendLine();
            sb.AppendLine("=== Cables a menos de 4 m del centro de la camara (distancia minima por objeto; EnFrustum = el segmento mas cercano toca el frustum) ===");
            var porMalla = segs.GroupBy(s => s.Ruta).Select(g =>
            {
                Segmento mejor = g.OrderBy(s => DistSeg(K13Desde, s.A, s.B)).First();
                float d = DistSeg(K13Desde, mejor.A, mejor.B);
                Bounds bb = new Bounds(mejor.A, Vector3.zero);
                bb.Encapsulate(mejor.B);
                bb.Expand(0.1f);
                return new { Ruta = g.Key, Malla = mejor.Malla, D = d, A = mejor.A, B = mejor.B, Frustum = GeometryUtility.TestPlanesAABB(planos, bb) };
            }).Where(x => x.D < DistanciaLibre).OrderBy(x => x.D).ToList();
            foreach (var x in porMalla)
            {
                sb.AppendLine(x.D.ToString("0.00", ci) + " m | EnFrustum=" + x.Frustum + " | " + x.Ruta + " | malla " + x.Malla + " | segmento " + x.A.ToString("0.0") + " -> " + x.B.ToString("0.0"));
            }
            sb.AppendLine("Objetos de cable a menos de 4 m: " + porMalla.Count + ".");
            int cercanosFrustum = 0;
            foreach (Segmento s in segs)
            {
                Bounds bb = new Bounds(s.A, Vector3.zero);
                bb.Encapsulate(s.B);
                bb.Expand(0.1f);
                if (DistSeg(K13Desde, s.A, s.B) < DistanciaLibre && GeometryUtility.TestPlanesAABB(planos, bb))
                {
                    cercanosFrustum++;
                }
            }
            sb.AppendLine("Segmentos a menos de 4 m dentro del frustum: " + cercanosFrustum + ".");

            sb.AppendLine();
            sb.AppendLine("=== Renderers con centro proyectado x>0.6, y>0.55 (arriba a la derecha), 20 a 600 m ===");
            List<string> filas = new List<string>();
            foreach (Renderer r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (r == null || !r.enabled || !r.gameObject.activeInHierarchy)
                {
                    continue;
                }
                float dist;
                Vector3 vp;
                if (!EnEsquina(cam, r, out dist, out vp))
                {
                    continue;
                }
                Mesh m = null;
                MeshFilter f = r.GetComponent<MeshFilter>();
                if (f != null)
                {
                    m = f.sharedMesh;
                }
                filas.Add(dist.ToString("000.0", ci) + " m | vp (" + vp.x.ToString("0.00", ci) + ", " + vp.y.ToString("0.00", ci) + ") | " + RutaCompleta(r.transform)
                    + " | malla " + (m != null ? m.name + " tri " + (m.triangles.Length / 3) : "(sin)") + " | tam " + r.bounds.size.ToString("0.0") + " | escala " + r.transform.lossyScale.ToString("0.###")
                    + " | mat " + string.Join(",", r.sharedMaterials.Select(x => x != null ? x.name + "[" + (x.shader != null ? x.shader.name : "?") + "]" : "null")));
            }
            foreach (string s in filas.OrderBy(x => x, StringComparer.Ordinal))
            {
                sb.AppendLine(s);
            }
            sb.AppendLine("Renderers en la esquina: " + filas.Count + ".");
            UnityEngine.Object.DestroyImmediate(cam.gameObject);
            Escribir(RutaDiagnostico, sb);
            Debug.Log("[ZonaCerroT1405] Diagnostico en " + RutaDiagnostico + " (" + porMalla.Count + " objetos de cable a menos de 4 m, " + filas.Count + " renderers en la esquina).");
        }

        // ================================================================ Aplicar

        /// <summary>Apaga (enabled=false, reversible) cuadros planos sueltos (grosor menor a 2 cm, lado mayor a 3 m) en la esquina superior derecha de K13. Vuelve a encender los de la corrida anterior primero.</summary>
        [MenuItem("Mecha Cyberpunk/T1405/Cerro camara: aplicar")]
        public static void Aplicar()
        {
            ColocarS2.Abrir();
            Physics.SyncTransforms();
            StringBuilder sb = new StringBuilder("T-1405: renderers apagados (rutas completas). Volver a correr Aplicar los enciende antes de evaluar.\n");
            string previo = Path.Combine(Directory.GetParent(Application.dataPath).FullName, RutaApagados);
            if (File.Exists(previo))
            {
                foreach (string l in File.ReadAllLines(previo).Skip(1))
                {
                    if (string.IsNullOrWhiteSpace(l))
                    {
                        continue;
                    }
                    GameObject g = GameObject.Find(l.Trim());
                    Renderer rr = g != null ? g.GetComponent<Renderer>() : null;
                    if (rr != null)
                    {
                        rr.enabled = true;
                    }
                }
            }
            Camera cam = CrearCamara(K13Desde, K13Hacia);
            string[] protegidos = { "Calle", "Arroyo", "Banqueta", "Mar", "Agua", "Bruma", "Cielo", "Skyline", "Piso", "Suelo", "Losa", "Techo", "Muro", "Fondo", "Luz", "Halo", "Neon" };
            int apagados = 0;
            foreach (Renderer r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (r == null || !r.enabled || !r.gameObject.activeInHierarchy)
                {
                    continue;
                }
                float dist;
                Vector3 vp;
                if (!EnEsquina(cam, r, out dist, out vp))
                {
                    continue;
                }
                Vector3 s = r.bounds.size;
                float[] d = { s.x, s.y, s.z };
                Array.Sort(d);
                string ruta = RutaCompleta(r.transform);
                if (d[0] < 0.02f && d[2] > 3f && !protegidos.Any(p => ruta.IndexOf(p, StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    r.enabled = false;
                    sb.AppendLine(ruta);
                    apagados++;
                }
            }
            UnityEngine.Object.DestroyImmediate(cam.gameObject);
            Escribir(RutaApagados, sb);
            Debug.Log("[ZonaCerroT1405] Apagados " + apagados + " cuadros planos en la esquina (Logs/t1405-apagados.txt). La escena no se guarda.");
        }

        // ================================================================ Fotos

        /// <summary>Busca en una rejilla de +-6 m (paso 1.5 m, a 1.7, 1.95 y 2.2 m sobre el piso) el punto mas cercano a K13 sin cables a menos de 4 m y sin colliders encima. Deja CamaraDesde/CamaraHacia.</summary>
        public static bool ElegirCamara(StringBuilder sb)
        {
            CultureInfo ci = CultureInfo.InvariantCulture;
            List<Segmento> segs = Segmentos(sb);
            sb.AppendLine("Camara original " + K13Desde + ": cable mas cercano a " + MinDistancia(segs, K13Desde).ToString("0.00", ci) + " m.");
            float[] alturas = { 1.7f, 1.95f, 2.2f };
            Vector3 mejor = K13Desde, respaldo = K13Desde;
            float mejorCosto = float.MaxValue, mejorDist = -1f, respaldoDist = -1f;
            int libres = 0;
            for (float dx = -Radio; dx <= Radio + 0.01f; dx += Paso)
            {
                for (float dz = -Radio; dz <= Radio + 0.01f; dz += Paso)
                {
                    foreach (float h in alturas)
                    {
                        Vector3 p0 = new Vector3(K13Desde.x + dx, K13Desde.y + 3f, K13Desde.z + dz);
                        RaycastHit suelo;
                        if (!Physics.Raycast(p0, Vector3.down, out suelo, 12f, ~0, QueryTriggerInteraction.Ignore))
                        {
                            continue;
                        }
                        Vector3 p = new Vector3(p0.x, suelo.point.y + h, p0.z);
                        Vector3 hacia = K13Hacia + (p - K13Desde);
                        bool tapada = Physics.CheckSphere(p, 0.8f, ~0, QueryTriggerInteraction.Ignore) || Physics.Raycast(p, (hacia - p).normalized, 6f, ~0, QueryTriggerInteraction.Ignore);
                        if (tapada)
                        {
                            continue;
                        }
                        float d = MinDistancia(segs, p);
                        if (d > respaldoDist)
                        {
                            respaldoDist = d;
                            respaldo = p;
                        }
                        if (d < DistanciaLibre)
                        {
                            continue;
                        }
                        libres++;
                        float costo = new Vector2(dx, dz).magnitude + Mathf.Abs(h - 1.85f);
                        if (costo < mejorCosto)
                        {
                            mejorCosto = costo;
                            mejor = p;
                            mejorDist = d;
                        }
                    }
                }
            }
            sb.AppendLine("Puntos libres (sin cable a menos de 4 m ni colliders): " + libres + ".");
            if (mejorDist < 0f)
            {
                sb.AppendLine("AVISO: no hay punto libre; se usa el de cable mas lejano (" + respaldoDist.ToString("0.00", ci) + " m).");
                mejor = respaldo;
                mejorDist = respaldoDist;
            }
            CamaraDesde = mejor;
            CamaraHacia = K13Hacia + (mejor - K13Desde);
            sb.AppendLine("ELEGIDA: desde " + CamaraDesde.ToString("0.00") + " hacia " + CamaraHacia.ToString("0.00") + " (cable mas cercano " + mejorDist.ToString("0.00", ci) + " m).");
            return mejorDist >= DistanciaLibre;
        }

        [MenuItem("Mecha Cyberpunk/T1405/Cerro camara: fotos")]
        public static void Fotos()
        {
            ColocarS2.Abrir();
            Directory.CreateDirectory("docs/capturas/t-1405");
            Physics.SyncTransforms();
            StringBuilder sb = new StringBuilder("T-1405 camara de K13 sin cables cerca\n");
            bool libre = ElegirCamara(sb);
            sb.AppendLine("Libre de cables a 4 m: " + libre);
            Escribir(RutaCamara, sb);
            ColocarS2cLook.Foto(CamaraDesde, CamaraHacia, "docs/capturas/t-1405/cerro-k13-juego.png", CampoFoto);
            ColocarS2.LadoALado("imageassets/referencias/look/K13_ColoniaCerro.png", "docs/capturas/t-1405/cerro-k13-juego.png", "docs/capturas/t-1405/cerro-k13.png");
            EditorSceneManager.OpenScene(ColocarS2.RutaEscena, OpenSceneMode.Single);
        }

        private static void Escribir(string ruta, StringBuilder sb)
        {
            string completa = Path.Combine(Directory.GetParent(Application.dataPath).FullName, ruta);
            Directory.CreateDirectory(Path.GetDirectoryName(completa));
            File.WriteAllText(completa, sb.ToString());
        }
    }
}
