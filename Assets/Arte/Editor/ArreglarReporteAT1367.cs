using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MechaCyberpunk.Core.Mundo;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-1367: arregla en los prefabs del Cerro y del Puerto los bugs que reportó la sesión A (ladera dentro del túnel de la autopista,
    /// escalones en la unión U4 / calle base, puerta norte del Puerto tapada, parpadeo). Abre cada prefab con LoadPrefabContents y ajusta
    /// los MISMOS objetos (nada se destruye ni se recrea: la escena guarda referencias por fileID). Idempotente.
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ArreglarReporteAT1367.Aplicar (y .FotosAntes / .FotosDespues).
    /// Se corre DESPUÉS de ArreglarCerroT1363, VariarCerroT1364, RendimientoCerroT1365 y ArreglarPuertoT1366.
    /// </summary>
    public static class ArreglarReporteAT1367
    {
        public const string RutaCerro = "Assets/Arte/Mundo/Zonas/T1355_Cerro.prefab";
        public const string RutaPuerto = "Assets/Arte/Mundo/Zonas/T1357_Puerto.prefab";
        public const string RutaLadera = "Assets/_Proyecto/Mundo/Datos/Zonas/T1367_Ladera.asset";
        public const string RutaCalles = "Assets/_Proyecto/Mundo/Datos/Zonas/T1367_Calles_Mallas.asset";
        public const string RutaInforme = "Logs/t1367-arreglos.txt";
        public const string CarpetaFotos = "docs/capturas/bugs-18nov-B";

        // ---------------------------------------------------------------- túnel de la autopista (m)
        /// <summary>Eje del túnel en planta (x, z), de la boca de entrada a la salida (mira al sur).</summary>
        public static readonly Vector2[] EjeTunel =
        {
            new Vector2(-330f, 250f), new Vector2(-345f, 230f), new Vector2(-357f, 210f), new Vector2(-360f, 193f), new Vector2(-360f, 185f),
        };
        public const float MedioAnchoTunel = 8f;
        public const float MargenTubo = 0.5f;
        public const float PisoTunel = 10f;
        public const float TechoTunel = 16.6f;
        /// <summary>Altura mínima a la que sube la ladera sobre el túnel: techo + 0.3 m.</summary>
        public const float TechoLibre = 16.9f;
        /// <summary>La rejilla de la ladera es de 4 m: se suben también los vértices a esta distancia más del tubo, para que ningún triángulo lo cruce.</summary>
        public const float MargenMalla = 4f;
        /// <summary>Entre medio ancho + 0.5 m y esta distancia más, la subida baja suave hasta la altura original (m).</summary>
        public const float CaidaSuave = 5.5f;
        /// <summary>Margen a cada lado de la calle que cuenta como corredor (banqueta + 0.0), y cuánto queda el terreno bajo la calle (m).</summary>
        public const float MargenCorredor = 1.5f;
        public const float BajoCalle = 0.15f;

        /// <summary>Corredor de una calle: su eje (con alturas, en mundo) y su semiancho (ancho / 2 + 1.5 m).</summary>
        public sealed class Corredor
        {
            public Vector3[] Puntos;
            public float Semiancho;
        }

        public static float Suave(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }

        /// <summary>Altura de la calle más cercana si (x, z) cae dentro de su corredor.</summary>
        public static bool AlturaCalle(IList<Corredor> calles, float x, float z, out float y)
        {
            y = 0f;
            float mejor = float.MaxValue;
            Vector2 q = new Vector2(x, z);
            foreach (Corredor c in calles)
            {
                for (int i = 1; i < c.Puntos.Length; i++)
                {
                    Vector2 a = new Vector2(c.Puntos[i - 1].x, c.Puntos[i - 1].z), b = new Vector2(c.Puntos[i].x, c.Puntos[i].z);
                    Vector2 ab = b - a;
                    float s = ab.sqrMagnitude < 1e-6f ? 0f : Mathf.Clamp01(Vector2.Dot(q - a, ab) / ab.sqrMagnitude);
                    float d = Vector2.Distance(q, a + ab * s);
                    if (d <= c.Semiancho && d < mejor)
                    {
                        mejor = d;
                        y = Mathf.Lerp(c.Puntos[i - 1].y, c.Puntos[i].y, s);
                    }
                }
            }
            return mejor < float.MaxValue;
        }

        public static List<Corredor> Corredores(GameObject raiz)
        {
            return raiz.GetComponentsInChildren<CalleZona>(false)
                .Where(cz => !cz.Autopista)
                .Select(cz => new Corredor { Puntos = cz.Puntos().ToArray(), Semiancho = cz.Ancho * 0.5f + MargenCorredor }).ToList();
        }

        /// <summary>La malla original de la ladera (T1355): de ahí parte siempre el arreglo, para ser idempotente.</summary>
        public const string RutaMallasOriginal = "Assets/_Proyecto/Mundo/Datos/Zonas/T1355_Cerro_Mallas.asset";
        public const float ToleranciaPiso = 0.02f;
        // Prisma libre de la boca de salida.
        public static readonly Vector3 BocaMin = new Vector3(-368f, 8.6f, 185f);
        public static readonly Vector3 BocaMax = new Vector3(-352f, 17.7f, 193f);

        // ---------------------------------------------------------------- unión U4 con la calle base (m)
        public const float XBase = -305f;
        public const float MedioArroyoBase = 4.5f;
        public const float XInicioU4 = -305f, XFinU4 = -249f, MedioArroyoU4 = 7f;
        public const float MargenArroyo = 0.05f;
        public const float SeparacionArroyos = 0.008f;
        public const float SeparacionPlanos = 0.012f;
        public const float ToleranciaCoplanar = 0.004f;
        public static readonly Vector3 PuntoEscaleraCE03 = new Vector3(-320f, 1.4f, 3.8f);

        // ---------------------------------------------------------------- puerta norte del Puerto (m)
        public static readonly Vector3 CentroPuerta = new Vector3(300f, 0f, -50f);
        /// <summary>Paso libre exigido: 10 m de ancho (x) por 20 m (z, de -60 a -40).</summary>
        public static readonly Vector3 MitadPaso = new Vector3(5f, 1f, 10f);
        /// <summary>Banda que se despeja (un poco más ancha que el paso).</summary>
        public static readonly Vector3 MitadBanda = new Vector3(6f, 1f, 10f);
        public static readonly Vector3 PuntoGolpe = new Vector3(304.3f, 0f, -56.2f);
        public static readonly Vector3 PuntoAtoro = new Vector3(295f, 0f, -85.2f);
        /// <summary>Caja de un carro: 2.5 de ancho (x), 2 de alto, 4.5 de largo (z).</summary>
        public static readonly Vector3 MitadCarro = new Vector3(1.25f, 1f, 2.25f);
        /// <summary>El centro de las cajas queda a esta altura: el asfalto (y ~ 0.08) no cuenta como obstáculo.</summary>
        public const float AltoCentroCaja = 1.3f;

        private const float MargenAlejar = 0.3f;
        private static StringBuilder informe = new StringBuilder();
        private static readonly RaycastHit[] bufHits = new RaycastHit[256];
        private static readonly Collider[] bufCols = new Collider[512];

        // ================================================================ lógica pura (la usan las pruebas)

        /// <summary>Distancia en planta al eje del túnel, o infinito si queda antes de la boca de entrada o después de la salida.</summary>
        public static float DistanciaAlEje(float x, float z)
        {
            float mejor = float.MaxValue;
            Vector2 q = new Vector2(x, z);
            for (int i = 0; i + 1 < EjeTunel.Length; i++)
            {
                Vector2 a = EjeTunel[i], b = EjeTunel[i + 1];
                Vector2 ab = b - a;
                float t = Vector2.Dot(q - a, ab) / ab.sqrMagnitude;
                if ((i == 0 && t < 0f) || (i == EjeTunel.Length - 2 && t > 1f))
                {
                    continue;
                }
                mejor = Mathf.Min(mejor, Vector2.Distance(q, a + ab * Mathf.Clamp01(t)));
            }
            return mejor;
        }

        /// <summary>Un punto (en mundo) dentro del volumen del tubo: sobre el piso, bajo el techo + 0.3 m y a menos de medio ancho + 0.5 m del eje.</summary>
        public static bool DentroDelTubo(Vector3 p)
        {
            return p.y > PisoTunel + ToleranciaPiso && p.y < TechoLibre - 0.001f && DistanciaAlEje(p.x, p.z) <= MedioAnchoTunel + MargenTubo;
        }

        /// <summary>Un punto (en mundo) estrictamente dentro del prisma libre de la boca de salida.</summary>
        public static bool DentroDeLaBoca(Vector3 p)
        {
            return p.x > BocaMin.x && p.x < BocaMax.x && p.y > BocaMin.y && p.y < BocaMax.y && p.z > BocaMin.z && p.z < BocaMax.z;
        }

        private static bool EnHuellaBoca(Vector3 p)
        {
            return p.x >= BocaMin.x && p.x <= BocaMax.x && p.z >= BocaMin.z && p.z <= BocaMax.z;
        }

        private static bool EnMargenTubo(Vector3 p)
        {
            return DistanciaAlEje(p.x, p.z) <= MedioAnchoTunel + MargenTubo + MargenMalla;
        }

        /// <summary>Cuántos vértices de la malla (en mundo con <paramref name="aMundo"/>) están dentro del tubo.</summary>
        public static int VerticesEnTubo(Mesh m, Matrix4x4 aMundo)
        {
            return m.vertices.Count(v => DentroDelTubo(aMundo.MultiplyPoint3x4(v)));
        }

        /// <summary>Cuántos vértices de la malla están dentro del prisma de la boca.</summary>
        public static int VerticesEnBoca(Mesh m, Matrix4x4 aMundo)
        {
            return m.vertices.Count(v => DentroDeLaBoca(aMundo.MultiplyPoint3x4(v)));
        }

        /// <summary>Punto (mundo) dentro del arroyo de U4 (con un margen de 5 cm hacia dentro).</summary>
        public static bool EnArroyoU4(Vector3 p)
        {
            return p.x >= XInicioU4 && p.x <= XFinU4 && Mathf.Abs(p.z) < MedioArroyoU4 - MargenArroyo;
        }

        /// <summary>Punto (mundo) dentro del arroyo de la calle base (con un margen de 5 cm hacia dentro).</summary>
        public static bool EnArroyoBase(Vector3 p)
        {
            return Mathf.Abs(p.x - XBase) < MedioArroyoBase - MargenArroyo;
        }

        /// <summary>
        /// Copia de la ladera sin nada dentro del túnel ni de la boca: quita los triángulos que tocan el prisma de la boca y sube a
        /// <see cref="TechoLibre"/> los vértices que quedan en el tubo (más el margen de una celda). Normales suaves y bounds recalculados.
        /// </summary>
        public static Mesh CorregirLadera(Mesh origen, Matrix4x4 aMundo, IList<Corredor> calles, out int trianguloQuitados, out int verticesSubidos)
        {
            Vector3[] v = origen.vertices;
            Vector3[] w = v.Select(p => aMundo.MultiplyPoint3x4(p)).ToArray();
            Vector2[] uv = origen.uv;
            Color[] colores = origen.colors;
            int[] t = origen.triangles;
            List<int> tri = new List<int>();
            trianguloQuitados = 0;
            for (int k = 0; k + 2 < t.Length; k += 3)
            {
                Vector3 mn = Vector3.Min(w[t[k]], Vector3.Min(w[t[k + 1]], w[t[k + 2]]));
                Vector3 mx = Vector3.Max(w[t[k]], Vector3.Max(w[t[k + 1]], w[t[k + 2]]));
                bool toca = mx.x > BocaMin.x && mn.x < BocaMax.x && mx.y > BocaMin.y && mn.y < BocaMax.y && mx.z > BocaMin.z && mn.z < BocaMax.z;
                if (toca)
                {
                    trianguloQuitados++;
                    continue;
                }
                tri.Add(t[k]);
                tri.Add(t[k + 1]);
                tri.Add(t[k + 2]);
            }
            verticesSubidos = 0;
            for (int i = 0; i < w.Length; i++)
            {
                float d = DistanciaAlEje(w[i].x, w[i].z);
                if (d > MedioAnchoTunel + MargenTubo + CaidaSuave || w[i].y >= TechoLibre || EnHuellaBoca(w[i]))
                {
                    continue;
                }
                // Dentro del tubo (+0.5 m) sube a TechoLibre; más afuera interpola hacia la altura original (sin paredes verticales).
                float k = 1f - Suave((d - (MedioAnchoTunel + MargenTubo)) / CaidaSuave);
                float y = Mathf.Lerp(w[i].y, TechoLibre, k);
                // Nunca por encima de la superficie de una calle del corredor: se deja como estaba o 15 cm bajo la calle.
                if (calles != null && AlturaCalle(calles, w[i].x, w[i].z, out float yCalle) && y > yCalle)
                {
                    y = w[i].y <= yCalle ? w[i].y : yCalle - BajoCalle;
                }
                if (y > w[i].y + 0.001f)
                {
                    w[i].y = y;
                    verticesSubidos++;
                }
                else if (y < w[i].y - 0.001f)
                {
                    w[i].y = y;
                }
            }
            // Compacta: solo los vértices que usa algún triángulo.
            int[] nuevo = Enumerable.Repeat(-1, v.Length).ToArray();
            List<Vector3> vs = new List<Vector3>();
            List<Vector2> us = new List<Vector2>();
            List<Color> cs = new List<Color>();
            Matrix4x4 aLocal = aMundo.inverse;
            List<int> fin = new List<int>(tri.Count);
            foreach (int idx in tri)
            {
                if (nuevo[idx] < 0)
                {
                    nuevo[idx] = vs.Count;
                    vs.Add(aLocal.MultiplyPoint3x4(w[idx]));
                    if (uv.Length == v.Length)
                    {
                        us.Add(uv[idx]);
                    }
                    if (colores.Length == v.Length)
                    {
                        cs.Add(colores[idx]);
                    }
                }
                fin.Add(nuevo[idx]);
            }
            Mesh m = new Mesh { name = "T1367_Ladera", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            m.SetVertices(vs);
            if (us.Count == vs.Count)
            {
                m.SetUVs(0, us);
            }
            if (cs.Count == vs.Count)
            {
                m.SetColors(cs);
            }
            m.SetTriangles(fin, 0);
            m.SetNormals(NormalesSuaves(vs, fin));
            m.RecalculateBounds();
            return m;
        }

        /// <summary>Normales promediadas por posición (la malla repite vértices por triángulo).</summary>
        private static List<Vector3> NormalesSuaves(List<Vector3> vs, List<int> tri)
        {
            Dictionary<Vector3Int, Vector3> suma = new Dictionary<Vector3Int, Vector3>();
            Func<Vector3, Vector3Int> llave = p => new Vector3Int(Mathf.RoundToInt(p.x * 100f), Mathf.RoundToInt(p.y * 100f), Mathf.RoundToInt(p.z * 100f));
            for (int k = 0; k + 2 < tri.Count; k += 3)
            {
                Vector3 n = Vector3.Cross(vs[tri[k + 1]] - vs[tri[k]], vs[tri[k + 2]] - vs[tri[k]]);
                for (int e = 0; e < 3; e++)
                {
                    Vector3Int key = llave(vs[tri[k + e]]);
                    suma[key] = suma.TryGetValue(key, out Vector3 s) ? s + n : n;
                }
            }
            List<Vector3> res = new List<Vector3>(vs.Count);
            foreach (Vector3 p in vs)
            {
                Vector3 n = suma.TryGetValue(llave(p), out Vector3 s) ? s : Vector3.up;
                res.Add(n.sqrMagnitude < 1e-12f ? Vector3.up : n.normalized);
            }
            return res;
        }

        /// <summary>Copia de la malla sin los triángulos cuyo centroide (en mundo) cumple <paramref name="quitar"/>. Compacta los vértices.</summary>
        public static Mesh QuitarTriangulos(Mesh origen, Matrix4x4 aMundo, Func<Vector3, bool> quitar, string nombre, out int quitados)
        {
            Vector3[] v = origen.vertices;
            Vector3[] n = origen.normals;
            Vector2[] uv = origen.uv;
            int[] t = origen.triangles;
            List<int> tri = new List<int>();
            quitados = 0;
            for (int k = 0; k + 2 < t.Length; k += 3)
            {
                Vector3 c = (aMundo.MultiplyPoint3x4(v[t[k]]) + aMundo.MultiplyPoint3x4(v[t[k + 1]]) + aMundo.MultiplyPoint3x4(v[t[k + 2]])) / 3f;
                if (quitar(c))
                {
                    quitados++;
                    continue;
                }
                tri.Add(t[k]);
                tri.Add(t[k + 1]);
                tri.Add(t[k + 2]);
            }
            int[] nuevo = Enumerable.Repeat(-1, v.Length).ToArray();
            List<Vector3> vs = new List<Vector3>();
            List<Vector3> ns = new List<Vector3>();
            List<Vector2> us = new List<Vector2>();
            List<int> fin = new List<int>(tri.Count);
            foreach (int idx in tri)
            {
                if (nuevo[idx] < 0)
                {
                    nuevo[idx] = vs.Count;
                    vs.Add(v[idx]);
                    if (n.Length == v.Length)
                    {
                        ns.Add(n[idx]);
                    }
                    if (uv.Length == v.Length)
                    {
                        us.Add(uv[idx]);
                    }
                }
                fin.Add(nuevo[idx]);
            }
            Mesh m = new Mesh { name = nombre, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            m.SetVertices(vs);
            if (ns.Count == vs.Count)
            {
                m.SetNormals(ns);
            }
            if (us.Count == vs.Count)
            {
                m.SetUVs(0, us);
            }
            m.SetTriangles(fin, 0);
            if (ns.Count != vs.Count)
            {
                m.RecalculateNormals();
            }
            m.RecalculateBounds();
            return m;
        }

        /// <summary>Cuántos triángulos de la malla tienen su centroide (en mundo) en la región.</summary>
        public static int TriangulosEn(Mesh m, Matrix4x4 aMundo, Func<Vector3, bool> region)
        {
            Vector3[] v = m.vertices;
            int[] t = m.triangles;
            int n = 0;
            for (int k = 0; k + 2 < t.Length; k += 3)
            {
                Vector3 c = (aMundo.MultiplyPoint3x4(v[t[k]]) + aMundo.MultiplyPoint3x4(v[t[k + 1]]) + aMundo.MultiplyPoint3x4(v[t[k + 2]])) / 3f;
                if (region(c))
                {
                    n++;
                }
            }
            return n;
        }

        /// <summary>
        /// Área (m²) de los triángulos horizontales de A cuyo centroide cae dentro de un triángulo horizontal de B y a menos de
        /// <paramref name="tolerancia"/> de su plano: planos coplanares encimados (parpadean).
        /// </summary>
        public static float AreaCoplanar(Mesh a, Matrix4x4 ma, Mesh b, Matrix4x4 mb, float tolerancia)
        {
            Vector3[] va = a.vertices.Select(p => ma.MultiplyPoint3x4(p)).ToArray();
            int[] ta = a.triangles;
            Vector3[] vb = b.vertices.Select(p => mb.MultiplyPoint3x4(p)).ToArray();
            int[] tb = b.triangles;
            float total = 0f;
            for (int i = 0; i + 2 < ta.Length; i += 3)
            {
                Vector3 p0 = va[ta[i]], p1 = va[ta[i + 1]], p2 = va[ta[i + 2]];
                Vector3 nrm = Vector3.Cross(p1 - p0, p2 - p0);
                if (Mathf.Abs(nrm.normalized.y) < 0.9f)
                {
                    continue;
                }
                Vector3 c = (p0 + p1 + p2) / 3f;
                for (int j = 0; j + 2 < tb.Length; j += 3)
                {
                    Vector3 q0 = vb[tb[j]], q1 = vb[tb[j + 1]], q2 = vb[tb[j + 2]];
                    if (!YEnTriangulo(q0, q1, q2, c.x, c.z, out float yb) || Mathf.Abs(yb - c.y) > tolerancia)
                    {
                        continue;
                    }
                    total += nrm.magnitude * 0.5f;
                    break;
                }
            }
            return total;
        }

        private static bool YEnTriangulo(Vector3 a, Vector3 b, Vector3 c, float x, float z, out float y)
        {
            y = 0f;
            float den = (b.z - c.z) * (a.x - c.x) + (c.x - b.x) * (a.z - c.z);
            if (Mathf.Abs(den) < 1e-9f)
            {
                return false;
            }
            float l1 = ((b.z - c.z) * (x - c.x) + (c.x - b.x) * (z - c.z)) / den;
            float l2 = ((c.z - a.z) * (x - c.x) + (a.x - c.x) * (z - c.z)) / den;
            float l3 = 1f - l1 - l2;
            if (l1 < 0f || l2 < 0f || l3 < 0f)
            {
                return false;
            }
            y = l1 * a.y + l2 * b.y + l3 * c.y;
            return true;
        }

        /// <summary>Colliders sólidos (no trigger, activos) dentro de una caja alineada a los ejes, en la física de la escena de <paramref name="escena"/>.</summary>
        public static List<Collider> ColisionadoresEn(PhysicsScene escena, Vector3 centro, Vector3 mitad)
        {
            Physics.SyncTransforms();
            int n = escena.OverlapBox(centro, mitad, bufCols, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore);
            List<Collider> res = new List<Collider>();
            for (int i = 0; i < n; i++)
            {
                if (bufCols[i] != null && bufCols[i].enabled)
                {
                    res.Add(bufCols[i]);
                }
            }
            return res;
        }

        /// <summary>Caja de un carro (2.5 x 2 x 4.5 m) apoyada sobre el asfalto en el punto dado.</summary>
        public static List<Collider> ColisionadoresDeCarro(PhysicsScene escena, Vector3 punto)
        {
            return ColisionadoresEn(escena, new Vector3(punto.x, AltoCentroCaja, punto.z), MitadCarro);
        }

        /// <summary>Colliders dentro del paso libre de 10 m de la puerta norte (z de -60 a -40).</summary>
        public static List<Collider> ColisionadoresDelPaso(PhysicsScene escena)
        {
            return ColisionadoresEn(escena, new Vector3(CentroPuerta.x, AltoCentroCaja, CentroPuerta.z), MitadPaso);
        }

        // ================================================================ puntos de entrada

        /// <summary>Batch: aplica los arreglos a los dos prefabs, guarda y escribe el informe.</summary>
        public static void Aplicar()
        {
            informe = new StringBuilder();
            Log("T-1367: arreglos de la respuesta a la sesión A (18-nov C)");
            AplicarCerro();
            AplicarPuerto();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Escribir();
        }

        public static void FotosAntes()
        {
            Fotos("antes");
        }

        public static void FotosDespues()
        {
            Fotos("despues");
        }

        // ================================================================ Cerro

        private static void AplicarCerro()
        {
            GameObject raiz = PrefabUtility.LoadPrefabContents(RutaCerro);
            try
            {
                PhysicsScene fisica = raiz.scene.GetPhysicsScene();
                Physics.SyncTransforms();
                Log("== Cerro ==");
                Ladera(raiz);
                Physics.SyncTransforms();
                UnionU4(raiz, fisica);
                Physics.SyncTransforms();
                EscaleraCE03(raiz);
                SepararCoplanares(raiz);
                FocosYCasas(raiz, fisica);
                Physics.SyncTransforms();
                bool ok;
                PrefabUtility.SaveAsPrefabAsset(raiz, RutaCerro, out ok);
                Log(ok ? "Prefab del Cerro guardado." : "ERROR: no se pudo guardar el prefab del Cerro.");
            }
            catch (Exception e)
            {
                Log("ERROR en el Cerro: " + e);
                Debug.LogError("[ArreglarReporteAT1367] " + e);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
        }

        private static Mesh GuardarMalla(Mesh nueva, string ruta, string nombre)
        {
            nueva.name = nombre;
            Mesh existente = AssetDatabase.LoadAllAssetsAtPath(ruta).OfType<Mesh>().FirstOrDefault(m => m.name == nombre);
            if (existente != null)
            {
                EditorUtility.CopySerialized(nueva, existente);
                Object.DestroyImmediate(nueva);
                EditorUtility.SetDirty(existente);
                return existente;
            }
            if (File.Exists(Path.Combine(RaizProyecto(), ruta)))
            {
                AssetDatabase.AddObjectToAsset(nueva, ruta);
            }
            else
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(RaizProyecto(), ruta)));
                AssetDatabase.CreateAsset(nueva, ruta);
            }
            AssetDatabase.SaveAssets();
            return nueva;
        }

        private static void Asignar(Transform t, Mesh m)
        {
            MeshFilter mf = t.GetComponent<MeshFilter>();
            if (mf != null)
            {
                mf.sharedMesh = m;
            }
            MeshCollider mc = t.GetComponent<MeshCollider>();
            if (mc != null)
            {
                mc.sharedMesh = null;
                mc.sharedMesh = m;
            }
        }

        // 1: la ladera sale del tubo de la autopista y deja libre la boca de salida.
        private static void Ladera(GameObject raiz)
        {
            Transform t = raiz.transform.Find("Terreno/Ladera");
            MeshFilter mf = t != null ? t.GetComponent<MeshFilter>() : null;
            if (mf == null || mf.sharedMesh == null)
            {
                Log("ERROR: falta Terreno/Ladera.");
                return;
            }
            Matrix4x4 aMundo = t.localToWorldMatrix;
            Mesh origen = AssetDatabase.LoadAllAssetsAtPath(RutaMallasOriginal).OfType<Mesh>().OrderByDescending(m => m.vertexCount).FirstOrDefault();
            if (origen == null)
            {
                Log("ERROR: no se encontró la malla original T1355_Terreno en " + RutaMallasOriginal + "; la ladera no se toca.");
                return;
            }
            int antesTubo = VerticesEnTubo(origen, aMundo), antesBoca = VerticesEnBoca(origen, aMundo);
            Mesh nueva = CorregirLadera(origen, aMundo, Corredores(raiz), out int quitados, out int subidos);
            Mesh guardada = GuardarMalla(nueva, RutaLadera, "T1367_Ladera");
            Asignar(t, guardada);
            Log("Ladera: antes " + antesTubo + " vértices en el tubo y " + antesBoca + " en la boca. " + quitados + " triángulos quitados en la boca, "
                + subidos + " vértices subidos a y = " + F(TechoLibre) + ". Ahora " + VerticesEnTubo(guardada, aMundo) + " en el tubo y " + VerticesEnBoca(guardada, aMundo)
                + " en la boca (" + guardada.vertexCount + " vértices). Malla: " + RutaLadera);
        }

        // 2: unión U4 con la calle base: arroyos a y = 0 sin escalón, banquetas fuera de los arroyos, arroyos encimados separados.
        private static void UnionU4(GameObject raiz, PhysicsScene fisica)
        {
            Transform baseArr = raiz.transform.Find("Calles/Calle_Base/Arroyo");
            Transform baseBan = raiz.transform.Find("Calles/Calle_Base/Banquetas");
            Transform u4Arr = raiz.transform.Find("Calles/Calle_U4/Arroyo");
            Transform u4Ban = raiz.transform.Find("Calles/Calle_U4/Banquetas");
            Transform zigArr = raiz.transform.Find("Calles/Calle_Zigzag/Arroyo");
            if (baseArr == null || baseBan == null || u4Arr == null || u4Ban == null)
            {
                Log("ERROR: faltan los objetos de Calle_Base o Calle_U4.");
                return;
            }
            MedirU4(fisica, "antes");
            // La banqueta de la calle base no cruza el arroyo de U4, y la de U4 no entra al arroyo de la calle base.
            QuitarBanqueta(baseBan, EnArroyoU4, "T1367_Calle_Base_Banquetas");
            QuitarBanqueta(u4Ban, EnArroyoBase, "T1367_Calle_U4_Banquetas");
            // La cara de arriba de los arroyos queda en y = 0 en la unión (si algún vértice estaba fuera de ±2 cm).
            foreach (Transform arr in new[] { baseArr, u4Arr })
            {
                AplanarUnion(arr);
            }
            // Los arroyos que se enciman (mismo plano) se separan 8 mm: sube el de menor jerarquía (U4 sobre la base, el zigzag sobre ambos).
            List<Transform> arroyos = new List<Transform> { baseArr, u4Arr };
            if (zigArr != null)
            {
                arroyos.Add(zigArr);
            }
            for (int i = 1; i < arroyos.Count; i++)
            {
                for (int j = 0; j < i; j++)
                {
                    Mesh a = arroyos[i].GetComponent<MeshFilter>().sharedMesh, b = arroyos[j].GetComponent<MeshFilter>().sharedMesh;
                    float area = AreaCoplanar(a, arroyos[i].localToWorldMatrix, b, arroyos[j].localToWorldMatrix, ToleranciaCoplanar);
                    if (area > 0.05f)
                    {
                        Vector3 p = arroyos[i].position;
                        arroyos[i].position = new Vector3(p.x, p.y + SeparacionArroyos, p.z);
                        Log("Arroyos: " + arroyos[i].parent.name + " se sube " + F(SeparacionArroyos) + " m sobre " + arroyos[j].parent.name + " (coplanares " + F(area) + " m2).");
                    }
                }
            }
            Physics.SyncTransforms();
            MedirU4(fisica, "despues");
        }

        private static void QuitarBanqueta(Transform banqueta, Func<Vector3, bool> region, string nombre)
        {
            MeshFilter mf = banqueta.GetComponent<MeshFilter>();
            Matrix4x4 aMundo = banqueta.localToWorldMatrix;
            int dentro = TriangulosEn(mf.sharedMesh, aMundo, region);
            Mesh nueva = QuitarTriangulos(mf.sharedMesh, aMundo, region, nombre, out int quitados);
            Mesh guardada = GuardarMalla(nueva, RutaCalles, nombre);
            Asignar(banqueta, guardada);
            Log("Banquetas " + banqueta.parent.name + ": " + dentro + " triángulos dentro del arroyo vecino, " + quitados + " quitados. Malla: " + RutaCalles + " / " + nombre);
        }

        private static void AplanarUnion(Transform arroyo)
        {
            MeshFilter mf = arroyo.GetComponent<MeshFilter>();
            Matrix4x4 aMundo = arroyo.localToWorldMatrix, aLocal = aMundo.inverse;
            Vector3[] v = mf.sharedMesh.vertices;
            int cambios = 0;
            for (int i = 0; i < v.Length; i++)
            {
                Vector3 w = aMundo.MultiplyPoint3x4(v[i]);
                bool enUnion = w.x >= XBase - MedioArroyoBase - 8f && w.x <= XBase + 20f && Mathf.Abs(w.z) <= MedioArroyoU4 + 2f;
                if (enUnion && Mathf.Abs(w.y) > ToleranciaPiso && Mathf.Abs(w.y) < 0.6f)
                {
                    w.y = 0f;
                    v[i] = aLocal.MultiplyPoint3x4(w);
                    cambios++;
                }
            }
            if (cambios == 0)
            {
                Log("Arroyo " + arroyo.parent.name + ": ya está a y = 0 en la unión (sin vértices que bajar).");
                return;
            }
            Mesh nueva = Object.Instantiate(mf.sharedMesh);
            nueva.vertices = v;
            nueva.RecalculateNormals();
            nueva.RecalculateBounds();
            Mesh guardada = GuardarMalla(nueva, RutaCalles, "T1367_" + arroyo.parent.name + "_Arroyo");
            Asignar(arroyo, guardada);
            Log("Arroyo " + arroyo.parent.name + ": " + cambios + " vértices de la unión bajados a y = 0.");
        }

        private static void MedirU4(PhysicsScene fisica, string etapa)
        {
            StringBuilder sb = new StringBuilder("Rayos sobre la unión (" + etapa + "), a lo largo de z = 0 y z = 3.4: ");
            foreach (float z in new[] { 0f, 3.4f })
            {
                float previa = float.NaN;
                for (float x = -318f; x <= -285f; x += 0.5f)
                {
                    int n = fisica.Raycast(new Vector3(x, 3f, z), Vector3.down, bufHits, 6f, ~0, QueryTriggerInteraction.Ignore);
                    float yMax = float.NegativeInfinity;
                    string nombre = "";
                    for (int i = 0; i < n; i++)
                    {
                        if (bufHits[i].point.y > yMax)
                        {
                            yMax = bufHits[i].point.y;
                            nombre = bufHits[i].collider.transform.parent != null ? bufHits[i].collider.transform.parent.name + "/" + bufHits[i].collider.name : bufHits[i].collider.name;
                        }
                    }
                    if (n > 0 && (float.IsNaN(previa) || Mathf.Abs(yMax - previa) > 0.02f))
                    {
                        sb.Append("[z " + F(z) + " x " + F(x) + " y " + F(yMax) + " " + nombre + "] ");
                        previa = yMax;
                    }
                }
            }
            Log(sb.ToString());
        }

        // 2: la escalera CE03 del arranque no puede quedar en un arroyo.
        private static void EscaleraCE03(GameObject raiz)
        {
            List<Vector3[]> ejes = new List<Vector3[]>();
            List<float> mitades = new List<float>();
            foreach (CalleZona cz in raiz.GetComponentsInChildren<CalleZona>(false))
            {
                ejes.Add(cz.Puntos().ToArray());
                mitades.Add(cz.Ancho * 0.5f + (cz.Banquetas ? 1.5f : 0f));
            }
            int movidos = 0, revisados = 0;
            foreach (Transform t in raiz.GetComponentsInChildren<Transform>(false))
            {
                if (t.name != "CE03_BarandalTuboEscalera")
                {
                    continue;
                }
                Vector3 p = t.position;
                if (Vector2.Distance(new Vector2(p.x, p.z), new Vector2(PuntoEscaleraCE03.x, PuntoEscaleraCE03.z)) > 3f || Mathf.Abs(p.y - PuntoEscaleraCE03.y) > 1.5f)
                {
                    continue;
                }
                revisados++;
                float mejor = float.MaxValue;
                Vector3 hacia = Vector3.zero;
                float mitad = 0f;
                for (int k = 0; k < ejes.Count; k++)
                {
                    for (int i = 1; i < ejes[k].Length; i++)
                    {
                        Vector2 a = new Vector2(ejes[k][i - 1].x, ejes[k][i - 1].z), b = new Vector2(ejes[k][i].x, ejes[k][i].z), q = new Vector2(p.x, p.z);
                        Vector2 ab = b - a;
                        float s = ab.sqrMagnitude < 1e-6f ? 0f : Mathf.Clamp01(Vector2.Dot(q - a, ab) / ab.sqrMagnitude);
                        Vector2 cerca = a + ab * s;
                        float d = Vector2.Distance(q, cerca);
                        if (d - mitades[k] < mejor)
                        {
                            mejor = d - mitades[k];
                            Vector2 fuera = q - cerca;
                            hacia = fuera.sqrMagnitude < 1e-6f ? Vector3.left : new Vector3(fuera.x, 0f, fuera.y).normalized;
                            mitad = mitades[k];
                        }
                    }
                }
                if (mejor < 0.3f)
                {
                    float mover = 0.3f - mejor;
                    t.position = p + hacia * mover;
                    movidos++;
                    Log("CE03 en " + V(p) + ": estaba a " + F(mejor) + " m del borde de la calle (media calle + banqueta " + F(mitad) + "); se mueve " + F(mover) + " m hacia afuera.");
                }
                else
                {
                    Log("CE03 en " + V(p) + ": queda a " + F(mejor) + " m del borde de la banqueta de la calle más cercana; no está dentro de ningún arroyo, sin cambios.");
                }
            }
            if (revisados == 0)
            {
                Log("CE03 cerca de " + V(PuntoEscaleraCE03) + ": no hay ninguna (ya se había movido o no existe).");
            }
        }

        // 4: pares coplanares (planos y cajas de casas pegadas) separados 1.2 cm.
        private sealed class RInfo
        {
            public MeshRenderer R;
            public Bounds B;
            public int Plano = -1;
            public bool Caja;
            public Transform Casa;
            public LODGroup Lod;
        }

        private static readonly string[] NombresIgnorados = { "Linea", "Charco", "Ventana", "Halo", "Cable", "Hueco", "Luz", "Neon", "Reflector", "FocoPared" };

        private static bool Alineado(Transform t)
        {
            foreach (Vector3 e in new[] { t.right, t.up, t.forward })
            {
                if (Mathf.Max(Mathf.Abs(e.x), Mathf.Max(Mathf.Abs(e.y), Mathf.Abs(e.z))) < 0.999f)
                {
                    return false;
                }
            }
            return true;
        }

        private static Transform Nivel2(Transform raiz, Transform t)
        {
            while (t.parent != null && t.parent.parent != raiz)
            {
                t = t.parent;
            }
            return t;
        }

        private static float Solape(Bounds a, Bounds b, int eje)
        {
            return Mathf.Min(a.max[eje], b.max[eje]) - Mathf.Max(a.min[eje], b.min[eje]);
        }

        private static void SepararCoplanares(GameObject raiz)
        {
            Transform r = raiz.transform;
            List<RInfo> infos = new List<RInfo>();
            Dictionary<Transform, Bounds> casas = new Dictionary<Transform, Bounds>();
            foreach (MeshRenderer mr in raiz.GetComponentsInChildren<MeshRenderer>(false))
            {
                if (!mr.enabled || !Alineado(mr.transform))
                {
                    continue;
                }
                MeshFilter mf = mr.GetComponent<MeshFilter>();
                if (mf == null || mf.sharedMesh == null)
                {
                    continue;
                }
                Transform raizProp = Nivel2(r, mr.transform);
                string grupo = raizProp.parent != null ? raizProp.parent.name : "";
                if (grupo == "Calles" || grupo == "Terreno" || NombresIgnorados.Any(n => mr.name.StartsWith(n, StringComparison.Ordinal)))
                {
                    continue;
                }
                RInfo info = new RInfo { R = mr, B = mr.bounds, Casa = raizProp, Lod = mr.GetComponentInParent<LODGroup>() };
                int minEje = 0;
                for (int e = 1; e < 3; e++)
                {
                    if (info.B.size[e] < info.B.size[minEje])
                    {
                        minEje = e;
                    }
                }
                if (info.B.size[minEje] < 0.05f)
                {
                    info.Plano = minEje;
                }
                else
                {
                    info.Caja = mf.sharedMesh.vertexCount <= 24;
                }
                if (casas.TryGetValue(raizProp, out Bounds cb))
                {
                    cb.Encapsulate(info.B);
                    casas[raizProp] = cb;
                }
                else
                {
                    casas[raizProp] = info.B;
                }
                infos.Add(info);
            }
            infos.Sort((a, b) => a.B.min.x.CompareTo(b.B.min.x));
            int movidos = 0;
            for (int pasada = 0; pasada < 3; pasada++)
            {
                int enPasada = 0;
                for (int i = 0; i < infos.Count; i++)
                {
                    RInfo a = infos[i];
                    for (int j = i + 1; j < infos.Count && infos[j].B.min.x <= a.B.max.x + 0.02f; j++)
                    {
                        RInfo b = infos[j];
                        if (a.Lod != null && a.Lod == b.Lod)
                        {
                            continue;
                        }
                        Bounds q = a.B;
                        q.Expand(0.02f);
                        if (!q.Intersects(b.B))
                        {
                            continue;
                        }
                        Vector3 delta = Vector3.zero;
                        if (a.Plano >= 0 && a.Plano == b.Plano)
                        {
                            int e0 = (a.Plano + 1) % 3, e1 = (a.Plano + 2) % 3;
                            float area = Mathf.Max(0f, Solape(a.B, b.B, e0)) * Mathf.Max(0f, Solape(a.B, b.B, e1));
                            if (Mathf.Abs(a.B.center[a.Plano] - b.B.center[a.Plano]) < 0.01f && area > 0.2f)
                            {
                                float signo = 1f;
                                if (a.Plano != 1)
                                {
                                    float hacia = casas[b.Casa].center[a.Plano] - b.B.center[a.Plano];
                                    signo = hacia < 0f ? -1f : 1f;
                                }
                                delta[a.Plano] = signo * SeparacionPlanos;
                            }
                        }
                        else if (a.Caja && b.Caja && a.Casa != b.Casa)
                        {
                            for (int eje = 0; eje < 3; eje++)
                            {
                                int e0 = (eje + 1) % 3, e1 = (eje + 2) % 3;
                                float area = Mathf.Max(0f, Solape(a.B, b.B, e0)) * Mathf.Max(0f, Solape(a.B, b.B, e1));
                                if (area <= 1f)
                                {
                                    continue;
                                }
                                if (Mathf.Abs(a.B.max[eje] - b.B.max[eje]) < 0.01f)
                                {
                                    delta[eje] = -SeparacionPlanos;
                                    break;
                                }
                                if (eje != 1 && Mathf.Abs(a.B.min[eje] - b.B.min[eje]) < 0.01f)
                                {
                                    delta[eje] = SeparacionPlanos;
                                    break;
                                }
                            }
                        }
                        if (delta != Vector3.zero)
                        {
                            b.R.transform.position += delta;
                            Bounds nb = b.B;
                            nb.center += delta;
                            b.B = nb;
                            enPasada++;
                        }
                    }
                }
                movidos += enPasada;
                if (enPasada == 0)
                {
                    break;
                }
            }
            Log("Parpadeo: " + movidos + " piezas separadas " + F(SeparacionPlanos) + " m de otra coplanar (planos y caras de cajas de casas pegadas). Los Arroyo se tratan aparte.");
        }

        // 4: FocoPared metido en la pared y CC01/CC02 enterradas.
        private static bool TerrenoY(PhysicsScene fisica, float x, float z, out float y)
        {
            int n = fisica.Raycast(new Vector3(x, 400f, z), Vector3.down, bufHits, 800f, ~0, QueryTriggerInteraction.Ignore);
            y = float.NegativeInfinity;
            for (int i = 0; i < n; i++)
            {
                if (bufHits[i].collider.name == "Ladera" && bufHits[i].point.y > y)
                {
                    y = bufHits[i].point.y;
                }
            }
            return !float.IsNegativeInfinity(y);
        }

        private static Bounds? Caja(Transform t)
        {
            Bounds? b = null;
            foreach (Renderer r in t.GetComponentsInChildren<Renderer>(false))
            {
                if (r is MeshRenderer)
                {
                    if (b == null)
                    {
                        b = r.bounds;
                    }
                    else
                    {
                        Bounds x = b.Value;
                        x.Encapsulate(r.bounds);
                        b = x;
                    }
                }
            }
            return b;
        }

        private static void FocosYCasas(GameObject raiz, PhysicsScene fisica)
        {
            const float TolerPared = 0.05f, AlRas = 0.01f, TolerEnterrada = 0.3f, TopeSubida = 3f;
            Transform casas = raiz.transform.Find("Casas");
            if (casas == null)
            {
                return;
            }
            int focos = 0, revisados = 0;
            foreach (Transform foco in raiz.GetComponentsInChildren<Transform>(false).Where(t => t.name == "FocoPared").ToList())
            {
                Bounds? fb = Caja(foco);
                Transform casa = Nivel2(raiz.transform, foco);
                if (fb == null || casa.parent != casas)
                {
                    continue;
                }
                revisados++;
                Bounds f = fb.Value;
                // Paredes de la casa: sus piezas "Fachada" o "Cuerpo" con collider que se solapan con el foco en los otros dos ejes.
                for (int eje = 0; eje < 3; eje += 2)
                {
                    int o0 = (eje + 1) % 3, o1 = (eje + 2) % 3;
                    float mejorPen = 0f;
                    float signoFuera = 0f;
                    foreach (BoxCollider bc in casa.GetComponentsInChildren<BoxCollider>(false))
                    {
                        if (!bc.name.Contains("Fachada") && !bc.name.StartsWith("Cuerpo", StringComparison.Ordinal))
                        {
                            continue;
                        }
                        Bounds w = bc.bounds;
                        if (Solape(w, f, o0) < 0.05f || Solape(w, f, o1) < 0.05f)
                        {
                            continue;
                        }
                        float fuera = f.center[eje] - w.center[eje] >= 0f ? 1f : -1f;
                        float caraPared = fuera > 0f ? w.max[eje] : w.min[eje];
                        float caraFoco = fuera > 0f ? f.min[eje] : f.max[eje];
                        float pen = (caraPared - caraFoco) * fuera;
                        if (pen > mejorPen && Mathf.Abs(f.center[eje] - w.center[eje]) > w.extents[eje] - 0.3f)
                        {
                            mejorPen = pen;
                            signoFuera = fuera;
                        }
                    }
                    if (mejorPen > TolerPared)
                    {
                        Vector3 mov = Vector3.zero;
                        mov[eje] = signoFuera * (mejorPen - AlRas);
                        foco.position += mov;
                        focos++;
                        break;
                    }
                }
            }
            Log("FocoPared: " + revisados + " revisados, " + focos + " estaban más de " + F(TolerPared) + " m dentro de la pared y se sacaron al ras.");
            int subidas = 0, vistas = 0;
            foreach (Transform cc in casas)
            {
                if (!cc.name.StartsWith("CC01_CasaCerro", StringComparison.Ordinal) && !cc.name.StartsWith("CC02_CasaCerro", StringComparison.Ordinal))
                {
                    continue;
                }
                Bounds? cb = Caja(cc);
                if (cb == null)
                {
                    continue;
                }
                vistas++;
                Bounds b = cb.Value;
                float baseY = b.min.y;
                float[] ys = new float[5];
                bool todos = TerrenoY(fisica, b.center.x, b.center.z, out ys[0]);
                int k = 1;
                foreach (float sx in new[] { -1f, 1f })
                {
                    foreach (float sz in new[] { -1f, 1f })
                    {
                        todos &= TerrenoY(fisica, b.center.x + sx * (b.extents.x - 0.3f), b.center.z + sz * (b.extents.z - 0.3f), out ys[k]);
                        k++;
                    }
                }
                if (!todos)
                {
                    continue;
                }
                float centro = ys[0];
                float esquinaBaja = Mathf.Min(Mathf.Min(ys[1], ys[2]), Mathf.Min(ys[3], ys[4]));
                float hundida = centro - baseY;
                if (hundida > TolerEnterrada)
                {
                    // Sube al ras del terreno, sin dejar flotando (más de 30 cm) la esquina más baja.
                    float subir = Mathf.Clamp(Mathf.Min(hundida - TolerEnterrada, esquinaBaja - baseY + TolerEnterrada), 0f, TopeSubida);
                    if (subir > 0.001f)
                    {
                        cc.position += Vector3.up * subir;
                        subidas++;
                    }
                }
            }
            Log("CC01/CC02: " + vistas + " revisadas, " + subidas + " con la base más de " + F(TolerEnterrada) + " m bajo el terreno se subieron al ras (sin dejar la esquina baja flotando más de " + F(TolerEnterrada) + " m).");
        }

        // ================================================================ Puerto

        private static void AplicarPuerto()
        {
            GameObject raiz = PrefabUtility.LoadPrefabContents(RutaPuerto);
            try
            {
                PhysicsScene fisica = raiz.scene.GetPhysicsScene();
                Physics.SyncTransforms();
                Log("== Puerto ==");
                Log("Antes: paso de la puerta norte con " + ColisionadoresDelPaso(fisica).Count + " colliders; (304.3,-56.2): " + ColisionadoresDeCarro(fisica, PuntoGolpe).Count + "; (295,-85.2): " + ColisionadoresDeCarro(fisica, PuntoAtoro).Count + ".");
                Despejar(raiz, fisica, new Vector3(CentroPuerta.x, AltoCentroCaja, CentroPuerta.z), MitadBanda, "paso de la puerta norte (x 294 a 306, z -60 a -40)");
                Despejar(raiz, fisica, new Vector3(PuntoGolpe.x, AltoCentroCaja, PuntoGolpe.z), MitadCarro, "golpe al entrar (304.3, -56.2)");
                Despejar(raiz, fisica, new Vector3(PuntoAtoro.x, AltoCentroCaja, PuntoAtoro.z), MitadCarro, "atoro en el patio (295, -85.2)");
                Physics.SyncTransforms();
                Log("Después: paso de la puerta norte con " + ColisionadoresDelPaso(fisica).Count + " colliders; (304.3,-56.2): " + ColisionadoresDeCarro(fisica, PuntoGolpe).Count + "; (295,-85.2): " + ColisionadoresDeCarro(fisica, PuntoAtoro).Count + ".");
                bool ok;
                PrefabUtility.SaveAsPrefabAsset(raiz, RutaPuerto, out ok);
                Log(ok ? "Prefab del Puerto guardado." : "ERROR: no se pudo guardar el prefab del Puerto.");
            }
            catch (Exception e)
            {
                Log("ERROR en el Puerto: " + e);
                Debug.LogError("[ArreglarReporteAT1367] " + e);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
        }

        private static readonly string[] GruposMovibles = { "Entrada", "Postes", "Bodegas", "Muelle" };

        private static Bounds? CajaColliders(Transform t)
        {
            Bounds? b = null;
            foreach (Collider c in t.GetComponentsInChildren<Collider>(false))
            {
                if (!c.enabled || c.isTrigger)
                {
                    continue;
                }
                if (b == null)
                {
                    b = c.bounds;
                }
                else
                {
                    Bounds x = b.Value;
                    x.Encapsulate(c.bounds);
                    b = x;
                }
            }
            return b;
        }

        /// <summary>
        /// Saca de la caja todo lo que se pueda mover (reja, caseta, postes, pallets, tambos): lo corre en x hasta dejar su collider fuera de la caja.
        /// Lo que no se puede mover (bloques de contenedores, bodegas, patio) solo se anota.
        /// </summary>
        private static void Despejar(GameObject raiz, PhysicsScene fisica, Vector3 centro, Vector3 mitad, string etiqueta)
        {
            HashSet<Transform> sinResolver = new HashSet<Transform>();
            for (int pasada = 0; pasada < 4; pasada++)
            {
                List<Collider> cols = ColisionadoresEn(fisica, centro, mitad);
                HashSet<Transform> raices = new HashSet<Transform>();
                foreach (Collider c in cols)
                {
                    Transform p = Nivel2(raiz.transform, c.transform);
                    if (p != raiz.transform && p.parent != null)
                    {
                        raices.Add(p);
                    }
                }
                int movidos = 0;
                foreach (Transform p in raices)
                {
                    string grupo = p.parent != null ? p.parent.name : "";
                    bool edificio = p.name.StartsWith("PU03_Bodega", StringComparison.Ordinal) || p.name.StartsWith("PU04_Bodega", StringComparison.Ordinal) || p.name.StartsWith("PU01_Grua", StringComparison.Ordinal);
                    if (!GruposMovibles.Contains(grupo) || edificio)
                    {
                        if (sinResolver.Add(p))
                        {
                            Log("  " + etiqueta + ": NO se puede mover " + grupo + "/" + p.name + " (collider en " + V(p.position) + ").");
                        }
                        continue;
                    }
                    Bounds? cb = CajaColliders(p);
                    if (cb == null)
                    {
                        continue;
                    }
                    Bounds b = cb.Value;
                    float dx = b.center.x >= centro.x ? 1f : -1f;
                    float shift = dx > 0f ? (centro.x + mitad.x + MargenAlejar) - b.min.x : (centro.x - mitad.x - MargenAlejar) - b.max.x;
                    p.position += new Vector3(shift, 0f, 0f);
                    movidos++;
                    Log("  " + etiqueta + ": " + grupo + "/" + p.name + " movido " + F(shift) + " m en x (de " + V(b.center) + ").");
                }
                Physics.SyncTransforms();
                if (movidos == 0)
                {
                    break;
                }
            }
            Log(etiqueta + ": " + ColisionadoresEn(fisica, centro, mitad).Count + " colliders quedan en la caja.");
        }

        // ================================================================ informe y fotos

        private static void Log(string texto)
        {
            informe.AppendLine(texto);
            Debug.Log("[ArreglarReporteAT1367] " + texto);
        }

        private static void Escribir()
        {
            string ruta = Path.Combine(RaizProyecto(), RutaInforme);
            Directory.CreateDirectory(Path.GetDirectoryName(ruta));
            File.WriteAllText(ruta, informe.ToString());
        }

        private static string RaizProyecto()
        {
            return Directory.GetParent(Application.dataPath).FullName;
        }

        private static string F(float v)
        {
            return v.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
        }

        private static string V(Vector3 v)
        {
            return "(" + F(v.x) + ", " + F(v.y) + ", " + F(v.z) + ")";
        }

        private struct Toma
        {
            public int N;
            public Vector3 Objetivo;
            public Vector3 Desplazamiento;
        }

        // Cada toma queda a 8-15 m del objetivo.
        private static readonly Toma[] Tomas =
        {
            new Toma { N = 1, Objetivo = new Vector3(-359.2f, 12.5f, 195f), Desplazamiento = new Vector3(0f, 4f, -12f) },
            new Toma { N = 2, Objetivo = new Vector3(-298f, 0.2f, 0f), Desplazamiento = new Vector3(8f, 5f, -8f) },
            new Toma { N = 3, Objetivo = new Vector3(300f, 1f, -50f), Desplazamiento = new Vector3(0f, 6f, 12f) },
            new Toma { N = 4, Objetivo = new Vector3(-302.75f, 0f, 0f), Desplazamiento = new Vector3(5f, 5f, -8f) },
            new Toma { N = 5, Objetivo = PuntoEscaleraCE03, Desplazamiento = new Vector3(-6f, 4f, -7f) },
            new Toma { N = 6, Objetivo = new Vector3(-330.3f, 2.8f, -12.9f), Desplazamiento = new Vector3(9f, 3f, -7f) },
        };

        private static void Fotos(string sufijo)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            foreach (string ruta in new[] { RutaCerro, RutaPuerto })
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
                if (prefab != null)
                {
                    PrefabUtility.InstantiatePrefab(prefab);
                }
            }
            ArteT250.PonerLuz();
            Physics.SyncTransforms();
            GameObject go = new GameObject("CamaraT1367");
            try
            {
                Camera cam = go.AddComponent<Camera>();
                cam.fieldOfView = 60f;
                cam.nearClipPlane = 0.1f;
                cam.farClipPlane = 1500f;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.35f, 0.4f, 0.5f);
                foreach (Toma t in Tomas)
                {
                    Vector3 pos = t.Objetivo + t.Desplazamiento;
                    int subidas = 0;
                    while (Physics.CheckSphere(pos, 0.6f, ~0, QueryTriggerInteraction.Ignore) && subidas < 20)
                    {
                        pos += Vector3.up * 1.5f;
                        subidas++;
                    }
                    cam.transform.position = pos;
                    cam.transform.LookAt(t.Objetivo);
                    string ruta = Path.Combine(RaizProyecto(), CarpetaFotos, "t1367-" + t.N + "-" + sufijo + ".png");
                    ArteT250.Guardar(cam, ruta);
                }
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
