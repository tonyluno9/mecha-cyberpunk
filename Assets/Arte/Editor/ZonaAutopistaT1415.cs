using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using MechaCyberpunk.Core.Mundo;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using A = MechaCyberpunk.Arte.Editor.ColocarT1356Autopista;
using T = MechaCyberpunk.Arte.Editor.ColocarT1356Trazado;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-1415 (Autopista K18, ronda 7): RAMAL elevado en curva que sale del tablero principal (z = 250) en x = X0 (cerca de -205), da una S de radio 60 hacia el norte
    /// y entra a una SEGUNDA boca de túnel en un peñasco nuevo pegado al cerro (x = -316, z = 281, a unos 34 m de la boca principal), a 10 a 12.5 m de alto sobre 3 columnas
    /// gruesas AU04. El tablero principal NO se cambia, salvo abrir su parapeto y sus barreras AU02 del lado norte en la salida (parapeto rehecho con el mismo corte).
    /// El ramal queda CERRADO: reja DS01 con letreros de OBRA justo pasada la bifurcación, y no entra al tráfico. Todo lo nuevo vive bajo "T1415_Autopista" (se borra y rehace).
    /// Orden (cada una en su propia corrida de Unity): 1) Aplicar. 2) Validar. 3) BuscarCamara (con el prefab ya aplicado). 4) Fotos (lee Logs/t1415-camara.txt).
    /// </summary>
    public static class ZonaAutopistaT1415
    {
        public const string RutaPrefab = "Assets/Arte/Mundo/Zonas/T1356_Autopista.prefab";
        public const string NombreRaiz = "T1415_Autopista";
        public const string RutaCamara = "Logs/t1415-camara.txt";

        // Trazado del ramal
        public const float X0Deseado = -205f;
        public const float RadioRamal = 60f;
        public const float ZBocaRamal = 281f;
        public const float XBocaRamal = -316f;
        public const float YSalida = 9.98f;      // 2 cm bajo el tablero principal: al ras y sin competir en profundidad
        public const float YBocaRamal = 12.5f;
        public const float SubidaDesde = 56f;
        public const float SubidaLargo = 60f;
        public const float LargoTubo = 18f;
        public const float SeparacionColumnasRamal = 26f;
        public const float DiametroColumnaRamal = 3.2f;
        public const int MaxLucesNuevas = 8;

        private const float MedioTablero = 7.2f;
        private const float MedioAsfalto = 6.5f;
        private const float Campo = 70f;
        private const float Aspecto = 1376f / 768f;
        private const string RutaBarrera = "Assets/Arte/Zonas/AU/AU02_BarreraConcreto_a.prefab";
        private const string RutaLuminaria = "Assets/Arte/Zonas/AU/AU01_Luminaria.prefab";
        private const float AltoBarrera = 0.9f;
        private const float AnchoMaxBarrera = 0.7f;
        // Peñasco: cuadro en planta (x0, x1, z0, z1) donde se come lo que haya.
        private const float PX0 = -346f;
        private const float PX1 = -316.5f;
        private const float PZ0 = 258f;
        private const float PZ1 = 304f;

        private static readonly StringBuilder informe = new StringBuilder();
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        // ---------------------------------------------------------------- Utilidades

        private static string F(float v) => v.ToString("F2", Inv);

        private static string V3(Vector3 v) => F(v.x) + ";" + F(v.y) + ";" + F(v.z);

        private static Vector3 LeerV3(string s)
        {
            string[] p = s.Split(';');
            return new Vector3(float.Parse(p[0], Inv), float.Parse(p[1], Inv), float.Parse(p[2], Inv));
        }

        private static float Puntua(float v, float lo, float hi, float tol)
        {
            if (v >= lo && v <= hi)
            {
                return 1f;
            }
            float d = v < lo ? lo - v : v - hi;
            return Mathf.Max(0f, 1f - d / tol);
        }

        private static Vector3 Derecha(Vector3 tg) => new Vector3(tg.z, 0f, -tg.x);

        private static Transform Hijo(Transform padre, string nombre)
        {
            Transform t = new GameObject(nombre).transform;
            t.SetParent(padre, false);
            return t;
        }

        private static void Estatico(GameObject g)
        {
            GameObjectUtility.SetStaticEditorFlags(g, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic);
        }

        private static Bounds? Caja(GameObject go, bool soloVisible)
        {
            Bounds? caja = null;
            foreach (Renderer r in go.GetComponentsInChildren<Renderer>(true))
            {
                if (soloVisible && (!r.enabled || r.name.StartsWith("Halo")))
                {
                    continue;
                }
                if (caja.HasValue)
                {
                    Bounds w = caja.Value;
                    w.Encapsulate(r.bounds);
                    caja = w;
                }
                else
                {
                    caja = r.bounds;
                }
            }
            return caja;
        }

        private static Material Concreto() => KitS2c.Mat("T1356_Concreto", () =>
        {
            Material m = new Material(AssetDatabase.LoadAssetAtPath<Material>("Assets/_Proyecto/Mundo/Materiales/Barrio_Concreto.mat"));
            m.SetFloat("_Smoothness", 0.25f);
            m.SetColor("_BaseColor", new Color(0.5f, 0.5f, 0.54f));
            return m;
        });

        private static Material LuzTubo() => KitS2c.Mat("T1356_LuzTubo", () => KitS2c.Emision(new Color(2.6f, 2.3f, 1.8f), Texture2D.whiteTexture));

        private static GameObject Cargar(string ruta)
        {
            GameObject g = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
            if (g == null)
            {
                informe.AppendLine("FALTA el prefab " + ruta + " (se omite lo que lo usa).");
            }
            return g;
        }

        /// <summary>Caja de los renderers de la pieza (sin girar, escala 1) y su centro respecto al pivote. Se instancia bajo <paramref name="padre"/> (o en la escena activa) y se borra.</summary>
        private static void Medir(GameObject prefab, Transform padre, out Vector3 tam, out Vector3 centro)
        {
            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, padre);
            go.transform.SetPositionAndRotation(new Vector3(0f, -5000f, 0f), Quaternion.identity);
            go.transform.localScale = Vector3.one;
            Renderer[] rs = go.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0)
            {
                tam = Vector3.one;
                centro = Vector3.zero;
            }
            else
            {
                Bounds b = rs[0].bounds;
                foreach (Renderer r in rs)
                {
                    b.Encapsulate(r.bounds);
                }
                tam = b.size;
                centro = b.center - go.transform.position;
            }
            Object.DestroyImmediate(go);
        }

        private static GameObject Poner(Transform padre, GameObject prefab, Vector3 pos, Quaternion giro, Vector3 escala, bool conCollider)
        {
            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, padre);
            go.transform.SetPositionAndRotation(pos, giro);
            go.transform.localScale = escala;
            if (!conCollider)
            {
                foreach (Collider c in go.GetComponentsInChildren<Collider>(true))
                {
                    c.enabled = false;
                }
            }
            return go;
        }

        // ---------------------------------------------------------------- Barrera AU02 (mismas medidas que T-1386, para que la rejilla coincida)

        private sealed class Barrera
        {
            public GameObject Prefab;
            public Vector3 Tam;
            public Vector3 Centro;
            public float Esc;
            public float Paso;
            public bool LargoX;
            public float Largo;
            public float Ancho;
        }

        private static Barrera CargarBarrera(Transform padre)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaBarrera);
            if (prefab == null)
            {
                return null;
            }
            Medir(prefab, padre, out Vector3 tam, out Vector3 centro);
            Barrera b = new Barrera { Prefab = prefab, Tam = tam, Centro = centro };
            b.LargoX = tam.x >= tam.z;
            b.Largo = b.LargoX ? tam.x : tam.z;
            b.Ancho = b.LargoX ? tam.z : tam.x;
            b.Esc = Mathf.Clamp(Mathf.Min(AltoBarrera / Mathf.Max(0.05f, tam.y), AnchoMaxBarrera / Mathf.Max(0.05f, b.Ancho)), 0.3f, 3f);
            b.Paso = Mathf.Max(0.5f, b.Largo * b.Esc);
            return b;
        }

        /// <summary>Paso real de las barreras del tablero principal (T-1386 las repartió en toda su longitud con este paso).</summary>
        private static float PasoReal(Barrera b)
        {
            float paso = b == null ? 2f : b.Paso;
            float total = T.Longitud(T.Principal());
            int n = Mathf.Max(1, Mathf.RoundToInt(total / paso));
            return total / n;
        }

        // ---------------------------------------------------------------- Trazado del ramal (puro)

        /// <summary>
        /// Centro del ramal: sale tangente del tablero en (x0, 250), arco a la derecha (norte) de radio 60, arco a la izquierda de radio 60 y recta hasta la boca (-316, 281).
        /// y = 9.98 (al ras del tablero) hasta la bifurcación y sube con smoothstep a 12.5 m (pendiente máxima ~6.5 %).
        /// </summary>
        public static List<Vector3> Ramal(float x0)
        {
            float r = RadioRamal;
            float theta = Mathf.Acos(1f - (ZBocaRamal - T.ZTablero) / (2f * r)) * Mathf.Rad2Deg;
            float xFinArcos = x0 - 2f * r * Mathf.Sin(theta * Mathf.Deg2Rad);
            List<Vector3> p = new List<Vector3> { new Vector3(x0, 0f, T.ZTablero) };
            p.AddRange(T.Arco(x0, T.ZTablero + r, r, -90f, -90f - theta, 6f, 0f));
            p.AddRange(T.Arco(xFinArcos, ZBocaRamal - r, r, 90f - theta, 90f, 6f, 0f));
            p[p.Count - 1] = new Vector3(xFinArcos, 0f, ZBocaRamal);
            for (float x = xFinArcos - 10f; x > XBocaRamal + 1f; x -= 10f)
            {
                p.Add(new Vector3(x, 0f, ZBocaRamal));
            }
            p.Add(new Vector3(XBocaRamal, 0f, ZBocaRamal));
            float s = 0f;
            for (int i = 0; i < p.Count; i++)
            {
                if (i > 0)
                {
                    s += T.Planta(p[i] - p[i - 1]).magnitude;
                }
                float t = Mathf.Clamp01((s - SubidaDesde) / SubidaLargo);
                float k = t * t * (3f - 2f * t);
                p[i] = new Vector3(p[i].x, Mathf.Lerp(YSalida, YBocaRamal, k), p[i].z);
            }
            return p;
        }

        private sealed class Plano
        {
            public float X0;
            public float PasoReal;
            public float XB;      // x donde el centro del ramal queda a 14.4 m del eje del tablero
            public float XBG;     // arista de la rejilla de barreras a partir de la cual vuelve el borde del tablero principal
            public float SSur;    // metros del ramal donde empieza su parapeto sur
            public float SReja;
            public float Largo;
            public List<Vector3> Ramal;
            public List<Vector3> Dens;
            public List<Vector3> Columnas = new List<Vector3>();
        }

        private static float SEnX(List<Vector3> p, float x)
        {
            float acum = 0f;
            for (int i = 1; i < p.Count; i++)
            {
                float l = Vector3.Distance(p[i - 1], p[i]);
                if (p[i].x <= x)
                {
                    float f = Mathf.Abs(p[i - 1].x - p[i].x) < 0.0001f ? 0f : (p[i - 1].x - x) / (p[i - 1].x - p[i].x);
                    return acum + l * Mathf.Clamp01(f);
                }
                acum += l;
            }
            return acum;
        }

        private static Plano Calcular(float pr)
        {
            Plano pl = new Plano { PasoReal = pr };
            pl.X0 = -330f + Mathf.Round((X0Deseado + 330f) / pr) * pr;
            pl.Ramal = Ramal(pl.X0);
            pl.Largo = T.Longitud(pl.Ramal);
            pl.Dens = T.Resamplear(pl.Ramal, 1f);
            pl.XB = pl.X0 - 45f;
            foreach (Vector3 q in pl.Dens)
            {
                if (q.z - T.ZTablero >= 14.4f)
                {
                    pl.XB = q.x;
                    break;
                }
            }
            pl.XBG = -330f + Mathf.Ceil((pl.XB + 330f) / pr) * pr;
            pl.SSur = SEnX(pl.Dens, pl.XBG);
            pl.SReja = pl.SSur + 6f;
            for (float s = 58f; s <= pl.Largo - 8f; s += SeparacionColumnasRamal)
            {
                T.PuntoEn(pl.Ramal, s, out Vector3 pt, out _);
                pl.Columnas.Add(pt);
            }
            return pl;
        }

        private static float YEn(Plano pl, float s)
        {
            T.PuntoEn(pl.Ramal, s, out Vector3 pt, out _);
            return pt.y;
        }

        // ---------------------------------------------------------------- Barrido de una sección (copia de ColocarT1356Autopista.Barrer, que es privada)

        private static GameObject Barrer(KitS2c.Obra o, Transform padre, string nombre, IList<Vector3> p, float u0, float u1, float v0, float v1, Material mat, bool collider, bool visible, float metrosUv = 8f)
        {
            int n = p.Count;
            Vector3[] lat = new Vector3[n];
            for (int i = 0; i < n; i++)
            {
                Vector3 t = Vector3.zero;
                if (i > 0)
                {
                    t += T.Planta(p[i] - p[i - 1]).normalized;
                }
                if (i < n - 1)
                {
                    t += T.Planta(p[i + 1] - p[i]).normalized;
                }
                t.Normalize();
                lat[i] = new Vector3(t.z, 0f, -t.x);
            }
            float[,] esq = { { u0, v0 }, { u1, v0 }, { u1, v1 }, { u0, v1 } };
            float[] perim = new float[5];
            for (int e = 0; e < 4; e++)
            {
                perim[e + 1] = perim[e] + Mathf.Abs(esq[(e + 1) % 4, 0] - esq[e, 0]) + Mathf.Abs(esq[(e + 1) % 4, 1] - esq[e, 1]);
            }
            List<Vector3> vs = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> tris = new List<int>();
            float s = 0f;
            for (int i = 0; i < n; i++)
            {
                if (i > 0)
                {
                    s += Vector3.Distance(p[i - 1], p[i]);
                }
                for (int e = 0; e < 4; e++)
                {
                    int f = (e + 1) % 4;
                    vs.Add(p[i] + lat[i] * esq[e, 0] + Vector3.up * esq[e, 1]);
                    vs.Add(p[i] + lat[i] * esq[f, 0] + Vector3.up * esq[f, 1]);
                    uvs.Add(new Vector2(s / metrosUv, perim[e] / metrosUv));
                    uvs.Add(new Vector2(s / metrosUv, perim[e + 1] / metrosUv));
                }
            }
            for (int i = 0; i < n - 1; i++)
            {
                for (int e = 0; e < 4; e++)
                {
                    int a0 = (i * 4 + e) * 2;
                    int a1 = a0 + 1;
                    int b0 = ((i + 1) * 4 + e) * 2;
                    int b1 = b0 + 1;
                    tris.AddRange(new[] { a0, a1, b0, a1, b1, b0 });
                }
            }
            Mesh m = new Mesh { name = o.Nombre + "_" + nombre + "_" + o.Mallas.Count, indexFormat = IndexFormat.UInt32 };
            m.SetVertices(vs);
            m.SetUVs(0, uvs);
            m.SetTriangles(tris, 0);
            m.RecalculateNormals();
            m.RecalculateTangents();
            m.RecalculateBounds();
            o.Mallas.Add(m);
            GameObject go = new GameObject(nombre);
            go.transform.SetParent(padre, false);
            go.AddComponent<MeshFilter>().sharedMesh = m;
            if (visible)
            {
                go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            }
            if (collider)
            {
                go.AddComponent<MeshCollider>().sharedMesh = m;
            }
            Estatico(go);
            return go;
        }

        private static Vector3 Lateral(List<Vector3> p, int i)
        {
            Vector3 t = Vector3.zero;
            if (i > 0)
            {
                t += T.Planta(p[i] - p[i - 1]).normalized;
            }
            if (i < p.Count - 1)
            {
                t += T.Planta(p[i + 1] - p[i]).normalized;
            }
            t.Normalize();
            return new Vector3(t.z, 0f, -t.x);
        }

        private static bool EnOtroTablero(Vector3 punto, float y, List<Vector3> otraDensa)
        {
            return otraDensa.Any(q => Mathf.Abs(q.y - y) < 0.6f && T.Planta(q - punto).magnitude < MedioTablero - 0.2f);
        }

        // ---------------------------------------------------------------- Aplicar

        [MenuItem("Mecha Cyberpunk/T-1415/Autopista ramal curvo (aplicar)")]
        public static void Aplicar()
        {
            informe.Clear();
            informe.AppendLine("T-1415 Autopista, ramal curvo (ronda 7)");
            GameObject contenido = PrefabUtility.LoadPrefabContents(RutaPrefab);
            try
            {
                Transform raiz = contenido.transform;
                if (raiz.position != Vector3.zero || raiz.rotation != Quaternion.identity || raiz.localScale != Vector3.one)
                {
                    informe.AppendLine("ERROR: la raíz del prefab no está en el origen; no se hace nada.");
                    Debug.LogError("[Arte] T-1415: raíz del prefab movida.");
                    return;
                }
                Transform vieja = raiz.Find(NombreRaiz);
                if (vieja != null)
                {
                    Object.DestroyImmediate(vieja.gameObject);
                }
                int lucesAntes = contenido.GetComponentsInChildren<Light>(true).Length;
                Barrera au02 = CargarBarrera(raiz);
                if (au02 == null)
                {
                    informe.AppendLine("FALTA " + RutaBarrera + ": sin barreras AU02 (el parapeto con collider sigue cerrando el borde, pero no se ve).");
                }
                float pr = PasoReal(au02);
                Plano pl = Calcular(pr);
                informe.AppendLine("Paso real de las barreras del principal: " + F(pr) + " m. X0 (salida, en arista de la rejilla) = " + F(pl.X0) + "; centro a 14.4 m del eje en x = " + F(pl.XB)
                    + "; el borde del principal vuelve en x = " + F(pl.XBG) + " (parapeto sur del ramal desde s = " + F(pl.SSur) + "); reja en s = " + F(pl.SReja) + "; largo del ramal " + F(pl.Largo) + " m.");
                informe.AppendLine("Radio mínimo " + F(T.RadioMin(pl.Ramal)) + " m, pendiente máxima " + F(T.PendienteMax(pl.Ramal) * 100f) + " %, y de " + F(pl.Ramal[0].y) + " a " + F(pl.Ramal[pl.Ramal.Count - 1].y) + ".");

                KitS2c.Obra o = KitS2c.Empezar(NombreRaiz, 1415, raiz);
                Despejar(raiz, pl);
                AbrirTableroPrincipal(raiz, o, pl, au02);
                Cubierta(o, Hijo(o.Raiz, "Ramal"), pl, au02);
                Columnas(o, Hijo(o.Raiz, "Columnas"), pl);
                Luminarias(o, Hijo(o.Raiz, "Luminarias"), pl);
                Cierre(o, Hijo(o.Raiz, "Cierre"), pl);
                Boca(o, Hijo(o.Raiz, "Boca"));
                KitS2c.Terminar(o);
                int lucesDespues = contenido.GetComponentsInChildren<Light>(true).Length;
                informe.AppendLine("Luces: antes " + lucesAntes + ", después " + lucesDespues + " (nuevas " + (lucesDespues - lucesAntes) + ", tope " + MaxLucesNuevas + ").");
                int objetos = o.Raiz.GetComponentsInChildren<Transform>(true).Length;
                informe.AppendLine("Objetos nuevos bajo " + NombreRaiz + ": " + objetos + " (tope 3000).");
                PrefabUtility.SaveAsPrefabAsset(contenido, RutaPrefab);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contenido);
            }
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/t1415-aplicar.txt", informe.ToString());
            Debug.Log("[Arte] T-1415 aplicar:\n" + informe);
        }

        // ---------------------------------------------------------------- Despejar lo viejo que estorba

        private static Bounds? CajaDe(Transform t)
        {
            return Caja(t.gameObject, false);
        }

        /// <summary>True si la caja (de algo viejo) estorba al ramal: debajo de la cubierta o en una columna o en el peñasco.</summary>
        private static bool Choca(Bounds b, Plano pl)
        {
            if (b.max.x > PX0 - 2f && b.min.x < PX1 + 2f && b.max.z > PZ0 - 2f && b.min.z < PZ1 + 2f)
            {
                return true;
            }
            for (int i = 0; i < pl.Dens.Count; i += 2)
            {
                Vector3 q = pl.Dens[i];
                float dx = Mathf.Max(b.min.x - q.x, 0f, q.x - b.max.x);
                float dz = Mathf.Max(b.min.z - q.z, 0f, q.z - b.max.z);
                if (Mathf.Sqrt(dx * dx + dz * dz) < MedioTablero + 1.8f && b.max.y > q.y - 2.3f)
                {
                    return true;
                }
            }
            foreach (Vector3 c in pl.Columnas)
            {
                float dx = Mathf.Max(b.min.x - c.x, 0f, c.x - b.max.x);
                float dz = Mathf.Max(b.min.z - c.z, 0f, c.z - b.max.z);
                if (Mathf.Sqrt(dx * dx + dz * dz) < 6.5f)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>Quita por nombre las casas, postes y chatarra viejos que caen en el corredor del ramal, y apaga las torres del fondo que quedan en el camino. Todo va al informe.</summary>
        private static void Despejar(Transform raiz, Plano pl)
        {
            string[] contenedores = { "Bajo/Casitas", "Bajo/Postes", "Bajo/Chatarra", "T1386_Autopista/Casas", "T1386_Autopista/Chatarra_AU03" };
            foreach (string ruta in contenedores)
            {
                Transform c = raiz.Find(ruta);
                if (c == null)
                {
                    informe.AppendLine("Despejar: no existe " + ruta + " (nada que quitar ahí).");
                    continue;
                }
                List<string> quitados = new List<string>();
                foreach (Transform hijo in c.Cast<Transform>().ToList())
                {
                    Bounds? b = CajaDe(hijo);
                    if (b.HasValue && Choca(b.Value, pl))
                    {
                        quitados.Add(hijo.name);
                        Object.DestroyImmediate(hijo.gameObject);
                    }
                }
                informe.AppendLine("Despejar " + ruta + ": " + quitados.Count + " quitados" + (quitados.Count > 0 ? " (" + string.Join(", ", quitados.Take(30)) + ")" : "") + ".");
            }
            Transform fondo = raiz.Find("Fondo");
            if (fondo != null)
            {
                foreach (Transform torre in fondo.Cast<Transform>().ToList())
                {
                    Bounds? b = CajaDe(torre);
                    if (torre.gameObject.activeSelf && b.HasValue)
                    {
                        Bounds ancha = b.Value;
                        ancha.Expand(new Vector3(6f, 0f, 6f));
                        Bounds plana = new Bounds(new Vector3(ancha.center.x, 0f, ancha.center.z), new Vector3(ancha.size.x, 400f, ancha.size.z));
                        bool estorba = plana.Intersects(new Bounds(new Vector3((PX0 + PX1) * 0.5f, 0f, (PZ0 + PZ1) * 0.5f), new Vector3(PX1 - PX0, 400f, PZ1 - PZ0)));
                        for (int i = 0; i < pl.Dens.Count && !estorba; i += 3)
                        {
                            Vector3 q = pl.Dens[i];
                            estorba = plana.Contains(new Vector3(q.x, 0f, q.z)) || plana.SqrDistance(new Vector3(q.x, 0f, q.z)) < 6f * 6f;
                        }
                        if (estorba)
                        {
                            torre.gameObject.SetActive(false);
                            informe.AppendLine("Se apagó " + raiz.name + "/Fondo/" + torre.name + " para dejar sitio al ramal.");
                        }
                    }
                }
            }
        }

        // ---------------------------------------------------------------- Abrir el borde norte del tablero principal en la salida

        /// <summary>
        /// Rehace el parapeto norte (Izq) del tablero principal igual que ColocarT1356Autopista.Tramo (collider continuo sin renderer, lo cubren las barreras AU02),
        /// pero sin él entre x = XBG y x = X0 (la salida). Quita las barreras AU02 del borde norte en ese tramo y los faroles AU01 norte que caerían sobre el ramal.
        /// </summary>
        private static void AbrirTableroPrincipal(Transform raiz, KitS2c.Obra o, Plano pl, Barrera au02)
        {
            Transform principal = raiz.Find("Tablero/Principal");
            int viejos = 0;
            if (principal != null)
            {
                foreach (Transform hijo in principal.Cast<Transform>().ToList())
                {
                    if (hijo.name.StartsWith("Parapeto_Izq_"))
                    {
                        informe.AppendLine("Parapeto norte viejo quitado: Tablero/Principal/" + hijo.name + " (se rehace debajo con el corte de la salida).");
                        Object.DestroyImmediate(hijo.gameObject);
                        viejos++;
                    }
                }
            }
            if (viejos == 0)
            {
                informe.AppendLine("Parapeto norte viejo: ya no estaba (corrida repetida o prefab distinto); se rehace igual.");
            }
            Transform t = Hijo(o.Raiz, "PrincipalParapetoNorte");
            List<Vector3> linea = T.Principal();
            List<Vector3> otraDensa = T.Resamplear(T.RampaNorte(), 1f);
            float total = T.Longitud(linea);
            float sW = pl.XBG + 330f;
            float sE = pl.X0 + 330f;
            List<List<Vector3>> tramos = new List<List<Vector3>>
            {
                T.Resamplear(T.Subtramo(linea, 0f, sW), 2f),
                T.Resamplear(T.Subtramo(linea, sE, total), 2f),
            };
            Material concreto = Concreto();
            int numero = 0;
            foreach (List<Vector3> densa in tramos)
            {
                List<Vector3> corrida = new List<Vector3>();
                for (int i = 0; i <= densa.Count; i++)
                {
                    bool libre = i < densa.Count && densa[i].y >= 1f && !EnOtroTablero(densa[i] + Lateral(densa, i) * (-(MedioTablero - 0.35f)), densa[i].y, otraDensa);
                    if (libre)
                    {
                        corrida.Add(densa[i]);
                        continue;
                    }
                    if (corrida.Count >= 2)
                    {
                        Barrer(o, t, "ParapetoNorte_" + numero++, corrida, -MedioTablero, -MedioAsfalto, 0f, A.AltoParapeto, concreto, true, false);
                    }
                    corrida.Clear();
                }
            }
            informe.AppendLine("Parapeto norte del principal rehecho en " + numero + " tramos con collider y sin renderer; abierto de x = " + F(pl.XBG) + " a x = " + F(pl.X0) + ".");

            int barrerasQuitadas = 0;
            int farolesQuitados = 0;
            foreach (Transform h in raiz.GetComponentsInChildren<Transform>(true).ToList())
            {
                if (h == null || h.IsChildOf(o.Raiz))
                {
                    continue;
                }
                bool esBarrera = h.name.StartsWith("AU02_") && !h.name.StartsWith("AU02_Curva");
                bool esFarol = h.name.StartsWith("AU01_Luminaria");
                if (!esBarrera && !esFarol)
                {
                    continue;
                }
                Bounds? b = CajaDe(h);
                if (!b.HasValue)
                {
                    continue;
                }
                Vector3 c = b.Value.center;
                if (esBarrera && c.z > 254.5f && c.z < 259.5f && Mathf.Abs(c.y - 10f) < 2f && c.x > pl.XBG + 0.01f && c.x < pl.X0 - 0.01f)
                {
                    Object.DestroyImmediate(h.gameObject);
                    barrerasQuitadas++;
                }
                else if (esFarol && c.z > 252f && c.z < 264f && c.y > 6f && c.x > pl.XBG - 5f && c.x < pl.X0 + 2.5f)
                {
                    informe.AppendLine("Farol del principal quitado (cae sobre la salida): " + h.name + " en " + c.ToString("F1"));
                    Object.DestroyImmediate(h.gameObject);
                    farolesQuitados++;
                }
            }
            informe.AppendLine("Barreras AU02 norte del principal quitadas en la salida: " + barrerasQuitadas + "; faroles quitados: " + farolesQuitados + ".");
        }

        // ---------------------------------------------------------------- Cubierta del ramal: viga, losa, asfalto, parapetos y barreras

        private static void Cubierta(KitS2c.Obra o, Transform t, Plano pl, Barrera au02)
        {
            Material concreto = Concreto();
            List<Vector3> p = pl.Ramal;
            // Viga central: solo desde s = 30 (donde ya no cae bajo la viga del principal).
            Barrer(o, t, "Viga", T.Resamplear(T.Subtramo(p, 30f, pl.Largo), 5f), -3f, 3f, -A.GruesoTablero - 0.7f, -A.GruesoTablero, concreto, false, true);
            Barrer(o, t, "Losa", p, -MedioTablero, MedioTablero, -A.GruesoTablero, -0.05f, concreto, true, true);
            Barrer(o, t, "Asfalto", p, -MedioAsfalto, MedioAsfalto, -0.05f, 0f, KitS2c.MatAsfalto(), true, true);
            // Parapetos con collider continuo y sin renderer (los cubren las barreras AU02): norte (u positivo) desde s = 0; sur (u negativo) desde que el ramal se separa del principal.
            List<Vector3> norte = T.Resamplear(p, 2f);
            Barrer(o, t, "Parapeto_Norte", norte, MedioAsfalto, MedioTablero, 0f, A.AltoParapeto, concreto, true, false);
            List<Vector3> sur = T.Resamplear(T.Subtramo(p, pl.SSur, pl.Largo), 2f);
            Barrer(o, t, "Parapeto_Sur", sur, -MedioTablero, -MedioAsfalto, 0f, A.AltoParapeto, concreto, true, false);
            informe.AppendLine("Cubierta del ramal: losa y asfalto con collider (visibles), viga desde s = 30, parapeto norte de s = 0 a " + F(pl.Largo) + " y sur de s = " + F(pl.SSur) + " a " + F(pl.Largo) + ".");

            if (au02 == null)
            {
                return;
            }
            Transform b = Hijo(t, "Barreras_AU02");
            int n1 = BarrerasLinea(b, au02, T.Subtramo(p, 0f, pl.Largo), 1f, 0);
            int n2 = BarrerasLinea(b, au02, T.Subtramo(p, pl.SSur, pl.Largo), -1f, n1);
            informe.AppendLine("Barreras AU02 del ramal: " + n1 + " al norte y " + n2 + " al sur (paso " + F(au02.Paso) + " m, escala " + F(au02.Esc) + ", variante _a).");
        }

        /// <summary>Barreras AU02 pegadas al borde exterior (lado +1 = u positivo = norte del ramal) a lo largo de la línea; sin collider (el parapeto lo cierra).</summary>
        private static int BarrerasLinea(Transform padre, Barrera b, List<Vector3> linea, float lado, int desde)
        {
            float total = T.Longitud(linea);
            int n = Mathf.Max(1, Mathf.RoundToInt(total / b.Paso));
            float escLargo = total / n / b.Paso;
            for (int k = 0; k < n; k++)
            {
                T.PuntoEn(linea, total * (k + 0.5f) / n, out Vector3 pt, out Vector3 tg);
                Vector3 afuera = Derecha(tg) * lado;
                Quaternion giro = b.LargoX ? Quaternion.LookRotation(Vector3.Cross(tg, Vector3.up)) : Quaternion.LookRotation(tg);
                Vector3 escala = b.LargoX ? new Vector3(b.Esc * escLargo, b.Esc, b.Esc) : new Vector3(b.Esc, b.Esc, b.Esc * escLargo);
                Vector3 centroMundo = pt + afuera * (MedioTablero - b.Ancho * b.Esc * 0.5f) + Vector3.up * (b.Tam.y * b.Esc * 0.5f);
                Vector3 desplazado = giro * Vector3.Scale(b.Centro, escala);
                GameObject g = Poner(padre, b.Prefab, centroMundo - desplazado, giro, escala, false);
                g.name = "AU02R_" + (desde + k);
                Estatico(g);
            }
            return n;
        }

        // ---------------------------------------------------------------- Columnas gruesas

        private static void Columnas(KitS2c.Obra o, Transform padre, Plano pl)
        {
            GameObject prefab = Cargar(CorrerZonasT1350.RutaModulo("AU04_ColumnaAutopista"));
            if (prefab == null)
            {
                return;
            }
            Medir(prefab, padre, out Vector3 tam, out Vector3 centro);
            Material concreto = Concreto();
            foreach (Vector3 c in pl.Columnas)
            {
                float alto = c.y - A.GruesoTablero - 0.3f;
                float grueso = Mathf.Clamp(DiametroColumnaRamal / Mathf.Max(0.01f, Mathf.Max(tam.x, tam.z)), 1f, 5f);
                Vector3 esc = new Vector3(grueso, alto / Mathf.Max(0.01f, tam.y), grueso);
                T.PuntoEn(pl.Ramal, SEnPunto(pl, c), out _, out Vector3 tg);
                Vector3 lat = Derecha(tg);
                Quaternion giro = Quaternion.LookRotation(tam.x >= tam.z ? Vector3.Cross(lat, Vector3.up) : lat);
                GameObject col = Poner(padre, prefab, new Vector3(c.x, -(centro.y - tam.y * 0.5f) * esc.y, c.z), giro, esc, true);
                col.name = "AU04_ColumnaRamal";
                Estatico(col);
                GameObject cab = KitS2c.Losa(o, padre, "CabezalRamal", new Vector3(c.x, c.y - A.GruesoTablero - 0.5f, c.z), Quaternion.LookRotation(tg),
                    new Vector3(MedioTablero * 1.5f, 1f, DiametroColumnaRamal + 0.6f), concreto, 4f, false);
                Estatico(cab);
            }
            informe.AppendLine("Columnas AU04 gruesas (" + F(DiametroColumnaRamal) + " m) con cabezal: " + pl.Columnas.Count + ", cada " + F(SeparacionColumnasRamal) + " m, en " + string.Join(" ", pl.Columnas.Select(c => c.ToString("F1"))) + ".");
        }

        private static float SEnPunto(Plano pl, Vector3 punto)
        {
            float mejor = 0f;
            float d = float.MaxValue;
            float acum = 0f;
            for (int i = 0; i < pl.Dens.Count; i++)
            {
                if (i > 0)
                {
                    acum += Vector3.Distance(pl.Dens[i - 1], pl.Dens[i]);
                }
                float dd = T.Planta(pl.Dens[i] - punto).sqrMagnitude;
                if (dd < d)
                {
                    d = dd;
                    mejor = acum;
                }
            }
            return mejor;
        }

        // ---------------------------------------------------------------- Faroles de sodio AU01 cada 30 m (borde norte)

        private static void Luminarias(KitS2c.Obra o, Transform padre, Plano pl)
        {
            GameObject prefab = Cargar(RutaLuminaria);
            if (prefab == null)
            {
                return;
            }
            Medir(prefab, padre, out Vector3 tam, out Vector3 centro);
            float escala = Mathf.Clamp(A.AlturaLuminaria / Mathf.Max(0.01f, tam.y), 0.3f, 3f);
            int n = 0;
            for (float s = 14f; s < pl.Largo - 12f; s += 30f)
            {
                T.PuntoEn(pl.Ramal, s, out Vector3 pt, out Vector3 tg);
                Vector3 der = Derecha(tg);
                Vector3 pie = pt + der * (MedioTablero - 0.5f);
                Vector3 haciaCalle = -der;
                GameObject go = Poner(padre, prefab, pie, Quaternion.identity, Vector3.one * escala, false);
                go.name = "AU01_LuminariaRamal_" + n;
                Vector3 brazo = T.Planta(centro) * escala;
                if (brazo.magnitude > 0.3f)
                {
                    go.transform.rotation = Quaternion.Euler(0f, Vector3.SignedAngle(brazo, haciaCalle, Vector3.up), 0f);
                }
                Bounds b = go.GetComponentsInChildren<Renderer>().Select(r => r.bounds).Aggregate((x, y) => { x.Encapsulate(y); return x; });
                float alcance = Vector3.Dot(b.center - pie, haciaCalle) + Mathf.Abs(b.extents.x * haciaCalle.x) + Mathf.Abs(b.extents.z * haciaCalle.z);
                alcance = Mathf.Clamp(alcance, 0.5f, 5f);
                Vector3 cabeza = pie + haciaCalle * alcance;
                cabeza.y = b.max.y - 0.3f;
                Vector3 asfalto = pie + haciaCalle * (MedioTablero - 0.5f - 3.5f);
                asfalto.y = pt.y;
                KitS2c.Luz(go.transform, cabeza, LightType.Spot, KitS2c.Calido, KitS2c.IntensidadPoste * 1.3f, 22f, 100f, asfalto - cabeza);
                KitS2c.Halo(o, go.transform, cabeza, pt.y, KitS2c.Sodio);
                Estatico(go);
                n++;
            }
            informe.AppendLine("Faroles AU01 de sodio en el ramal: " + n + ", cada 30 m, en el borde norte (escala " + F(escala) + ").");
        }

        // ---------------------------------------------------------------- Cierre: reja DS01 y letreros de OBRA

        private static readonly string[] GlifosO = { "01110", "10001", "10001", "10001", "10001", "10001", "01110" };
        private static readonly string[] GlifosB = { "11110", "10001", "10001", "11110", "10001", "10001", "11110" };
        private static readonly string[] GlifosR = { "11110", "10001", "10001", "11110", "10100", "10010", "10001" };
        private static readonly string[] GlifosA = { "01110", "10001", "10001", "11111", "10001", "10001", "10001" };
        private static readonly string[] GlifosC = { "01110", "10001", "10000", "10000", "10000", "10001", "01110" };
        private static readonly string[] GlifosE = { "11111", "10000", "10000", "11110", "10000", "10000", "11111" };
        private static readonly string[] GlifosD = { "11110", "10001", "10001", "10001", "10001", "10001", "11110" };

        private static string[] Glifo(char c)
        {
            switch (c)
            {
                case 'O': return GlifosO;
                case 'B': return GlifosB;
                case 'R': return GlifosR;
                case 'A': return GlifosA;
                case 'C': return GlifosC;
                case 'E': return GlifosE;
                default: return GlifosD;
            }
        }

        private static void Texto(Texture2D tex, string texto, int escala, int yBase, Color color)
        {
            int ancho = texto.Length * 5 * escala + (texto.Length - 1) * escala;
            int x0 = (tex.width - ancho) / 2;
            for (int i = 0; i < texto.Length; i++)
            {
                string[] g = Glifo(texto[i]);
                for (int fila = 0; fila < 7; fila++)
                {
                    for (int col = 0; col < 5; col++)
                    {
                        if (g[fila][col] != '1')
                        {
                            continue;
                        }
                        for (int dy = 0; dy < escala; dy++)
                        {
                            for (int dx = 0; dx < escala; dx++)
                            {
                                int px = x0 + i * 6 * escala + col * escala + dx;
                                int py = yBase + (6 - fila) * escala + dy;
                                tex.SetPixel(px, py, color);
                            }
                        }
                    }
                }
            }
        }

        private static Texture2D TexLetrero()
        {
            const string nombre = "T1415_LetreroObra";
            Texture2D ya = KitS2c.Cargar(nombre);
            if (ya != null)
            {
                return ya;
            }
            const int w = 256;
            const int h = 128;
            Texture2D t = new Texture2D(w, h, TextureFormat.RGBA32, true);
            Color amarillo = new Color(1f, 0.78f, 0.04f);
            Color negro = new Color(0.04f, 0.04f, 0.04f);
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    bool borde = x < 6 || y < 6 || x >= w - 6 || y >= h - 6;
                    t.SetPixel(x, y, borde ? negro : amarillo);
                }
            }
            Texto(t, "OBRA", 8, 52, negro);
            Texto(t, "CERRADO", 4, 14, negro);
            t.Apply();
            return KitS2c.Guardar(nombre, t, true, TextureWrapMode.Clamp);
        }

        /// <summary>Panel de dos caras (una cada lado) con UV 0 a 1; su frente (+z local) se ve de cara a quien lo mira de ese lado.</summary>
        private static GameObject Panel(KitS2c.Obra o, Transform padre, string nombre, Vector3 centro, Quaternion giro, float ancho, float alto, Material mat)
        {
            float w = ancho * 0.5f;
            float h = alto * 0.5f;
            Vector3[] v =
            {
                new Vector3(-w, -h, 0f), new Vector3(-w, h, 0f), new Vector3(w, h, 0f), new Vector3(w, -h, 0f),
                new Vector3(-w, -h, 0f), new Vector3(-w, h, 0f), new Vector3(w, h, 0f), new Vector3(w, -h, 0f),
            };
            Vector2[] uv =
            {
                new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(0f, 0f),
                new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(1f, 0f),
            };
            Vector3[] n =
            {
                Vector3.forward, Vector3.forward, Vector3.forward, Vector3.forward,
                Vector3.back, Vector3.back, Vector3.back, Vector3.back,
            };
            Mesh m = new Mesh { name = o.Nombre + "_" + nombre + "_" + o.Mallas.Count };
            m.SetVertices(v);
            m.SetUVs(0, uv);
            m.SetNormals(n);
            m.SetTriangles(new[] { 2, 1, 0, 2, 0, 3, 4, 5, 6, 4, 6, 7 }, 0);
            m.RecalculateBounds();
            o.Mallas.Add(m);
            GameObject go = new GameObject(nombre);
            go.transform.SetParent(padre, false);
            go.transform.SetPositionAndRotation(centro, giro);
            go.AddComponent<MeshFilter>().sharedMesh = m;
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            Estatico(go);
            return go;
        }

        private static void Cierre(KitS2c.Obra o, Transform padre, Plano pl)
        {
            T.PuntoEn(pl.Ramal, pl.SReja, out Vector3 pt, out Vector3 tg);
            Vector3 der = Derecha(tg);
            const float ancho = 2f * MedioTablero;
            const float altoReja = 2.6f;
            GameObject reja = Cargar(CorrerZonasT1350.RutaModulo("DS01_RejaPuas"));
            Transform grupo = Hijo(padre, "Reja");
            grupo.position = pt;
            grupo.rotation = Quaternion.LookRotation(tg);
            if (reja != null)
            {
                Medir(reja, padre, out Vector3 tam, out Vector3 centro);
                bool largoX = tam.x >= tam.z;
                float largo = largoX ? tam.x : tam.z;
                float esc = Mathf.Clamp(altoReja / Mathf.Max(0.05f, tam.y), 0.5f, 3f);
                int n = Mathf.Max(1, Mathf.CeilToInt(ancho / Mathf.Max(0.5f, largo * esc)));
                float escLargo = ancho / (n * Mathf.Max(0.05f, largo));
                Quaternion giro = largoX ? Quaternion.LookRotation(tg) : Quaternion.LookRotation(der);
                Vector3 escala = largoX ? new Vector3(escLargo, esc, esc) : new Vector3(esc, esc, escLargo);
                for (int k = 0; k < n; k++)
                {
                    Vector3 c = pt + der * (-MedioTablero + (k + 0.5f) * ancho / n);
                    Vector3 centroMundo = new Vector3(c.x, pt.y + tam.y * esc * 0.5f, c.z);
                    Vector3 desplazado = giro * Vector3.Scale(centro, escala);
                    GameObject g = Poner(grupo, reja, centroMundo - desplazado, giro, escala, false);
                    g.name = "DS01_RejaPuas_" + k;
                    Estatico(g);
                }
                informe.AppendLine("Reja DS01_RejaPuas: " + n + " piezas de " + F(esc * tam.y) + " m de alto a lo ancho de los " + F(ancho) + " m del tablero, en s = " + F(pl.SReja) + " (" + pt.ToString("F1") + ").");
            }
            // Collider continuo de la reja (sin huecos): una caja que coincide con la hilera visible.
            BoxCollider bc = grupo.gameObject.AddComponent<BoxCollider>();
            bc.center = new Vector3(0f, altoReja * 0.5f, 0f);
            bc.size = new Vector3(ancho, altoReja, 0.6f);
            // Letreros de OBRA delante de la reja, de cara a quien llega (viene del este y va al oeste).
            Material mat = KitS2c.Mat("T1415_LetreroObra", () => KitS2c.Emision(new Color(1.7f, 1.45f, 0.5f), TexLetrero()));
            Quaternion haciaQuienLlega = Quaternion.LookRotation(-tg);
            for (int i = 0; i < 2; i++)
            {
                float u = i == 0 ? -3.6f : 3.6f;
                Vector3 c = pt + der * u - tg * 0.4f + Vector3.up * 1.55f;
                Panel(o, grupo, "Letrero_OBRA_" + i, c, haciaQuienLlega, 3.0f, 1.5f, mat);
            }
            informe.AppendLine("Letreros de OBRA: 2 paneles de 3 x 1.5 m ('OBRA / CERRADO', emisivos de noche) pegados a la reja por el lado de quien llega.");
        }

        // ---------------------------------------------------------------- Peñasco, segunda boca y tubo corto

        private static float Ruido(float a, float b)
        {
            float s = Mathf.Sin(a * 12.9898f + b * 78.233f) * 43758.5453f;
            return s - Mathf.Floor(s);
        }

        private static void Boca(KitS2c.Obra o, Transform padre)
        {
            // --- tubo corto (cerrado al fondo): de la boca (-316) hacia el oeste LargoTubo m, a y = YBocaRamal
            List<Vector3> eje = new List<Vector3>();
            for (int k = 0; k <= 3; k++)
            {
                eje.Add(new Vector3(XBocaRamal - LargoTubo * k / 3f, YBocaRamal, ZBocaRamal));
            }
            Transform tubo = Hijo(padre, "Tubo");
            Material concreto = Concreto();
            Barrer(o, tubo, "Piso", eje, -8f, 8f, -A.GruesoTablero, -0.05f, concreto, true, true);
            Barrer(o, tubo, "Asfalto", eje, -MedioAsfalto, MedioAsfalto, -0.05f, 0f, KitS2c.MatAsfalto(), true, true);
            Barrer(o, tubo, "Pared_Izq", eje, -8f, -7f, -A.GruesoTablero, A.AltoTunel, concreto, true, true);
            Barrer(o, tubo, "Pared_Der", eje, 7f, 8f, -A.GruesoTablero, A.AltoTunel, concreto, true, true);
            Barrer(o, tubo, "Techo", eje, -8f, 8f, A.AltoTunel, A.AltoTunel + 1.2f, concreto, true, true);
            float xFondo = XBocaRamal - LargoTubo;
            GameObject fondo = KitS2c.Losa(o, tubo, "Fondo", new Vector3(xFondo - 0.4f, YBocaRamal + (A.AltoTunel + 1.2f - A.GruesoTablero) * 0.5f, ZBocaRamal), Quaternion.identity,
                new Vector3(0.8f, A.AltoTunel + 1.2f + A.GruesoTablero, 16f), concreto, 4f, true);
            Estatico(fondo);
            GameObject brillo = KitS2c.Losa(o, tubo, "FondoLuz", new Vector3(xFondo + 0.03f, YBocaRamal + 3f, ZBocaRamal), Quaternion.identity, new Vector3(0.06f, 3.6f, 7f), LuzTubo(), 1f, false);
            Estatico(brillo);
            int franjas = Mathf.Max(2, Mathf.FloorToInt(LargoTubo / A.SeparacionLucesTubo));
            for (int k = 0; k < franjas; k++)
            {
                float x = XBocaRamal - LargoTubo * (k + 0.5f) / franjas;
                Estatico(KitS2c.Losa(o, tubo, "LuzTubo_" + k, new Vector3(x, YBocaRamal + A.AltoTunel - 0.1f, ZBocaRamal), Quaternion.LookRotation(Vector3.left), new Vector3(0.6f, 0.15f, 3f), LuzTubo(), 1f, false));
            }
            KitS2c.Luz(tubo, new Vector3(XBocaRamal - 8f, YBocaRamal + A.AltoTunel - 0.8f, ZBocaRamal), LightType.Point, new Color(1f, 0.92f, 0.75f), 160f, 24f, 0f);
            KitS2c.Luz(tubo, new Vector3(XBocaRamal - 1f, YBocaRamal + 5f, ZBocaRamal), LightType.Spot, new Color(1f, 0.85f, 0.55f), 260f, 36f, 105f, new Vector3(1f, -0.4f, 0f));

            // --- marco de la boca AU06 (frente en x = -316, mirando al este, como la boca principal)
            GameObject prefab = Cargar(CorrerZonasT1350.RutaModulo("AU06_EntradaTunel"));
            if (prefab != null)
            {
                Medir(prefab, padre, out Vector3 tam, out Vector3 centro);
                Quaternion giro = Quaternion.LookRotation(Vector3.right);
                Vector3 punto = new Vector3(XBocaRamal, YBocaRamal, ZBocaRamal);
                Vector3 pos = punto - Vector3.right * (tam.z * 0.5f);
                pos.y = punto.y - (centro.y - tam.y * 0.5f);
                GameObject marco = Poner(padre, prefab, pos, giro, Vector3.one, false);
                marco.name = "AU06_BocaRamal";
                Estatico(marco);
            }
            Penasco(o, padre);
            informe.AppendLine("Segunda boca: frente en (" + F(XBocaRamal) + ", " + F(YBocaRamal) + ", " + F(ZBocaRamal) + "), tubo de " + F(LargoTubo) + " m cerrado al fondo (el ramal queda cerrado con reja), 2 luces nuevas dentro.");
        }

        /// <summary>
        /// Masa de roca (heightfield como el acantilado de T-1356, con la misma roca T1386_Roca y UV de mundo) de x -346 a -316.5 y z 258 a 304 que sale del cerro, con la cara este
        /// vertical en x = -317 y el hueco de la boca (16 m de ancho, del piso del tubo a su techo). Sobre el tubo sube a 24.5 m o más (techo del tubo a 20.2 m).
        /// </summary>
        private static void Penasco(KitS2c.Obra o, Transform padre)
        {
            float[] xs = { PX0, -340f, -334f, -328f, -322f, XBocaRamal - 1f, PX1 };
            float[] zs = { PZ0, 262f, 267f, ZBocaRamal - A.BocaMitadAncho, ZBocaRamal + A.BocaMitadAncho, 295f, 300f, PZ1 };
            float[] gx = { -0.5f, 14f, 26f, 26f, 26f, 26f, -0.5f };
            float[] fz = { -0.5f, 9f, 21f, 26f, 26f, 21f, 9f, -0.5f };
            int nx = xs.Length;
            int nz = zs.Length;
            float sobreTecho = YBocaRamal + A.AltoTunel + 1.2f + A.MargenTechoRoca;
            List<Vector3> v = new List<Vector3>();
            List<Vector2> uv = new List<Vector2>();
            for (int i = 0; i < nx; i++)
            {
                for (int j = 0; j < nz; j++)
                {
                    float x = xs[i];
                    float z = zs[j];
                    float h = Mathf.Min(gx[i], fz[j]);
                    if (h > 1f)
                    {
                        h += (Ruido(x, z) - 0.5f) * 3f;
                    }
                    bool sobreElTubo = x >= XBocaRamal - LargoTubo - 1f && x <= XBocaRamal && z >= ZBocaRamal - 12f && z <= ZBocaRamal + 12f;
                    if (sobreElTubo)
                    {
                        h = Mathf.Max(h, sobreTecho);
                    }
                    if (i == nx - 1)
                    {
                        h = -0.5f;
                    }
                    v.Add(new Vector3(x, h, z));
                    uv.Add(new Vector2((x + 0.5f * z) / 8f, (0.5f * z + h) / 8f));
                }
            }
            List<int> tris = new List<int>();
            float yBajo = YBocaRamal - A.GruesoTablero;
            float yAlto = YBocaRamal + A.AltoTunel + 1.2f;
            for (int i = 0; i < nx - 1; i++)
            {
                for (int j = 0; j < nz - 1; j++)
                {
                    int a = i * nz + j;
                    int b = a + 1;
                    int c = a + nz;
                    int d = c + 1;
                    bool cara = i == nx - 2;
                    if (cara && zs[j] >= ZBocaRamal - A.BocaMitadAncho - 0.01f && zs[j + 1] <= ZBocaRamal + A.BocaMitadAncho + 0.01f)
                    {
                        float xCara = v[a].x;
                        int k0 = v.Count;
                        v.Add(new Vector3(xCara, -0.5f, zs[j]));
                        v.Add(new Vector3(xCara, -0.5f, zs[j + 1]));
                        v.Add(new Vector3(xCara, yBajo, zs[j]));
                        v.Add(new Vector3(xCara, yBajo, zs[j + 1]));
                        v.Add(new Vector3(xCara, yAlto, zs[j]));
                        v.Add(new Vector3(xCara, yAlto, zs[j + 1]));
                        v.Add(new Vector3(xCara, Mathf.Max(yAlto, v[a].y), zs[j]));
                        v.Add(new Vector3(xCara, Mathf.Max(yAlto, v[b].y), zs[j + 1]));
                        for (int q = 0; q < 8; q++)
                        {
                            uv.Add(new Vector2((v[k0 + q].z + 0.5f * v[k0 + q].x) / 8f, (0.5f * v[k0 + q].z + v[k0 + q].y) / 8f));
                        }
                        tris.AddRange(new[] { k0, k0 + 2, k0 + 1, k0 + 1, k0 + 2, k0 + 3, k0 + 4, k0 + 6, k0 + 5, k0 + 5, k0 + 6, k0 + 7 });
                        continue;
                    }
                    tris.AddRange(new[] { a, b, c, b, d, c });
                }
            }
            Mesh m = new Mesh { name = o.Nombre + "_Penasco", indexFormat = IndexFormat.UInt32 };
            m.SetVertices(v);
            m.SetUVs(0, uv);
            m.SetTriangles(tris, 0);
            m.RecalculateNormals();
            m.RecalculateTangents();
            m.RecalculateBounds();
            o.Mallas.Add(m);
            GameObject go = new GameObject("Penasco");
            go.transform.SetParent(padre, false);
            go.AddComponent<MeshFilter>().sharedMesh = m;
            Material roca = AssetDatabase.LoadAssetAtPath<Material>("Assets/Arte/Mundo/S2c/T1386_Roca.mat");
            if (roca == null)
            {
                roca = AssetDatabase.LoadAssetAtPath<Material>("Assets/Arte/Mundo/S2c/T1356_Roca.mat");
            }
            if (roca == null)
            {
                informe.AppendLine("FALTAN T1386_Roca y T1356_Roca: el peñasco usa un gris liso de reserva.");
                roca = KitS2c.Mat("T1415_RocaReserva", () => KitS2c.Lit(new Color(0.3f, 0.3f, 0.32f), 0.1f, KitS2c.TexMuro()));
            }
            go.AddComponent<MeshRenderer>().sharedMaterial = roca;
            go.AddComponent<MeshCollider>().sharedMesh = m;
            Estatico(go);
        }

        // ---------------------------------------------------------------- Validar

        private static bool VisibleOCubierto(Collider c, Transform mia)
        {
            Renderer r = c.GetComponent<Renderer>();
            if (r != null && r.enabled)
            {
                return true;
            }
            foreach (Renderer h in c.GetComponentsInChildren<Renderer>(false))
            {
                if (h.enabled)
                {
                    return true;
                }
            }
            return false;
        }

        [MenuItem("Mecha Cyberpunk/T-1415/Autopista ramal curvo (validar)")]
        public static void Validar()
        {
            StringBuilder s = new StringBuilder("T-1415 validar Autopista, ramal curvo\n");
            int fallos = 0;
            int avisos = 0;
            ColocarS2.Abrir();
            Physics.SyncTransforms();
            Transform mia = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(t => t.name == NombreRaiz);
            if (mia == null)
            {
                s.AppendLine("FALLO: no existe " + NombreRaiz + " en la escena (corre Aplicar y reabre la escena).");
                Directory.CreateDirectory("Logs");
                File.WriteAllText("Logs/t1415-validar.txt", s.ToString());
                Debug.LogError("[Arte] T-1415 validar:\n" + s);
                return;
            }
            Barrera au02 = CargarBarrera(null);
            float pr = PasoReal(au02);
            Plano pl = Calcular(pr);
            s.AppendLine("X0 = " + F(pl.X0) + ", XB = " + F(pl.XB) + ", XBG = " + F(pl.XBG) + ", SSur = " + F(pl.SSur) + ", SReja = " + F(pl.SReja) + ", largo = " + F(pl.Largo) + ", paso AU02 = " + F(pr));

            // 1) Trazado
            float radio = T.RadioMin(pl.Ramal);
            float pend = T.PendienteMax(pl.Ramal);
            s.AppendLine("Radio mínimo " + F(radio) + " m (60 a 90), pendiente máxima " + F(pend * 100f) + " % (tope 9), alto del tablero de " + F(pl.Ramal[0].y) + " a " + F(pl.Ramal[pl.Ramal.Count - 1].y) + " m.");
            if (radio < 58f || pend > T.PendienteMaxima || pl.SReja > SubidaDesde - 0.5f)
            {
                s.AppendLine("FALLO: radio, pendiente o la reja cae fuera de la parte plana.");
                fallos++;
            }
            for (int i = 1; i < pl.Columnas.Count; i++)
            {
                float d = Vector3.Distance(T.Planta(pl.Columnas[i]), T.Planta(pl.Columnas[i - 1]));
                if (d < 24f || d > 30f)
                {
                    s.AppendLine("AVISO: columnas " + (i - 1) + " y " + i + " separadas " + F(d) + " m en planta (25 a 30 pedidos; el arco las acorta).");
                    avisos++;
                }
            }

            // 2) Perfil de rayos cada 1 m (tres carriles), unión al ras y sin escalones
            int malos = 0;
            float maxDif = 0f;
            float[] anterior = { float.NaN, float.NaN, float.NaN };
            float[] esperadoAnt = { 0f, 0f, 0f };
            float maxEscalon = 0f;
            float[] us = { -4.5f, 0f, 4.5f };
            for (float m = 0f; m <= pl.Largo - 0.5f; m += 1f)
            {
                T.PuntoEn(pl.Ramal, m, out Vector3 q, out Vector3 tg);
                Vector3 der = Derecha(tg);
                bool cercaReja = Mathf.Abs(m - pl.SReja) < 1.2f;
                for (int k = 0; k < 3; k++)
                {
                    Vector3 origen = q + der * us[k] + Vector3.up * 3f;
                    if (cercaReja)
                    {
                        anterior[k] = float.NaN;
                        continue;
                    }
                    RaycastHit[] hs = Physics.RaycastAll(origen, Vector3.down, 8f, ~0, QueryTriggerInteraction.Ignore).OrderBy(h => h.distance).ToArray();
                    if (hs.Length == 0)
                    {
                        if (malos++ < 12)
                        {
                            s.AppendLine("FALLO: sin piso en s = " + F(m) + ", u = " + F(us[k]));
                        }
                        fallos++;
                        anterior[k] = float.NaN;
                        continue;
                    }
                    RaycastHit top = hs[0];
                    float dif = Mathf.Abs(top.point.y - q.y);
                    maxDif = Mathf.Max(maxDif, dif);
                    if (dif > 0.03f)
                    {
                        if (malos++ < 12)
                        {
                            s.AppendLine("FALLO: la superficie en s = " + F(m) + ", u = " + F(us[k]) + " está a " + F(top.point.y) + " y el ramal pide " + F(q.y) + " (" + top.collider.name + ").");
                        }
                        fallos++;
                    }
                    bool mio = top.collider.transform.IsChildOf(mia);
                    if (mio && !VisibleOCubierto(top.collider, mia))
                    {
                        if (malos++ < 12)
                        {
                            s.AppendLine("FALLO: collider sin malla visible en s = " + F(m) + ": " + top.collider.name);
                        }
                        fallos++;
                    }
                    if (mio && !float.IsNaN(anterior[k]))
                    {
                        float escalon = Mathf.Abs((top.point.y - anterior[k]) - (q.y - esperadoAnt[k]));
                        maxEscalon = Mathf.Max(maxEscalon, escalon);
                        if (escalon > 0.04f)
                        {
                            if (malos++ < 12)
                            {
                                s.AppendLine("FALLO: escalón de " + F(escalon) + " m en s = " + F(m) + ", u = " + F(us[k]));
                            }
                            fallos++;
                        }
                    }
                    anterior[k] = mio ? top.point.y : float.NaN;
                    esperadoAnt[k] = q.y;
                }
            }
            s.AppendLine("Perfil de rayos cada 1 m (u -4.5, 0, 4.5): diferencia máxima contra el trazado " + F(maxDif * 100f) + " cm (tope 3), escalón máximo " + F(maxEscalon * 100f) + " cm (tope 4); unión al ras: la superficie en s = 0 a 6 m queda dentro de 3 cm del tablero principal.");

            // 3) Reja: ningún rayo pasa
            T.PuntoEn(pl.Ramal, pl.SReja, out Vector3 qr, out Vector3 tgr);
            Vector3 derr = Derecha(tgr);
            int pasan = 0;
            for (float u = -7.0f; u <= 7.01f; u += 0.5f)
            {
                foreach (float alto in new[] { 0.3f, 1.0f, 1.8f, 2.4f })
                {
                    Vector3 origen = qr - tgr * 3f + derr * u + Vector3.up * alto;
                    if (!Physics.Raycast(origen, tgr, 6f, ~0, QueryTriggerInteraction.Ignore))
                    {
                        pasan++;
                    }
                }
            }
            s.AppendLine("Reja: rayos que la cruzan sin tocar collider: " + pasan + " (0 esperado; el ramal queda cerrado a los carros).");
            fallos += pasan > 0 ? 1 : 0;
            Transform cierre = mia.Find("Cierre/Reja");
            // Las piezas DS01 son prefabs con la malla en hijos de otro nombre: se cuentan los renderers visibles que cuelgan de un "DS01_RejaPuas_n".
            int piezasReja = cierre == null ? 0 : cierre.GetComponentsInChildren<MeshRenderer>(true).Count(r => r.enabled && r.GetComponentsInParent<Transform>(true).Any(a => a != cierre && a.name.StartsWith("DS01_RejaPuas_")));
            int letreros = mia.GetComponentsInChildren<Transform>(true).Count(t => t.name.StartsWith("Letrero_OBRA_"));
            s.AppendLine("Piezas de reja visibles: " + piezasReja + "; letreros de OBRA: " + letreros + " (2).");
            if (piezasReja == 0 || letreros != 2)
            {
                s.AppendLine("FALLO: falta la reja visible o los letreros.");
                fallos++;
            }

            // 4) Parapetos: ramal con parapeto en todo su borde, principal abierto solo en la salida
            int huecosRamal = 0;
            for (float m = 0f; m <= pl.Largo - 1.5f; m += 2f)
            {
                T.PuntoEn(pl.Ramal, m, out Vector3 q, out Vector3 tg);
                Vector3 der = Derecha(tg);
                foreach (float lado in new[] { 1f, -1f })
                {
                    if (lado < 0f && m < pl.SSur + 0.5f)
                    {
                        continue;
                    }
                    Vector3 p = q + der * (lado * (MedioTablero - 0.35f)) + Vector3.up * 0.5f;
                    // Esfera de 0.6 m: el parapeto mide 0.7 m de grueso y una esfera chica queda DENTRO de su volumen sin tocar ninguna cara (un MeshCollider no convexo no la detecta).
                    if (!Physics.OverlapSphere(p, 0.6f, ~0, QueryTriggerInteraction.Ignore).Any(c => c.name.StartsWith("Parapeto")))
                    {
                        if (huecosRamal++ < 8)
                        {
                            s.AppendLine("FALLO: borde del ramal sin parapeto en s = " + F(m) + (lado > 0 ? " (norte)" : " (sur)"));
                        }
                    }
                }
            }
            fallos += huecosRamal;
            int huecosPrincipal = 0;
            int cerrado = 0;
            for (float x = -290f; x <= 235f; x += 1f)
            {
                Vector3 p = new Vector3(x, 10.5f, T.ZTablero + MedioTablero - 0.35f);
                bool hay = Physics.OverlapSphere(p, 0.6f, ~0, QueryTriggerInteraction.Ignore).Any(c => c.name.StartsWith("ParapetoNorte_") || c.name.StartsWith("Parapeto_Izq_"));
                bool enSalida = x > pl.XBG + 1.5f && x < pl.X0 - 1.5f;
                if (enSalida && hay)
                {
                    cerrado++;
                }
                else if (!enSalida && !hay && (x < pl.XBG - 0.5f || x > pl.X0 + 0.5f))
                {
                    if (huecosPrincipal++ < 8)
                    {
                        s.AppendLine("FALLO: el borde norte del principal no tiene parapeto en x = " + F(x));
                    }
                }
            }
            fallos += huecosPrincipal + (cerrado > 0 ? 1 : 0);
            s.AppendLine("Borde norte del principal: " + huecosPrincipal + " huecos fuera de la salida y " + cerrado + " puntos con parapeto dentro de la salida (x " + F(pl.XBG) + " a " + F(pl.X0) + "; los dos deben ser 0).");
            int barrerasSalida = mia.GetComponentsInChildren<Transform>(true).Count(t => t.name.StartsWith("AU02R_"));
            int viejasEnSalida = Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .Count(t => t.name.StartsWith("AU02_") && !t.IsChildOf(mia) && t.position.z > 254.5f && t.position.z < 259.5f && t.position.x > pl.XBG + 1f && t.position.x < pl.X0 - 1f && Mathf.Abs(t.position.y - 10f) < 2f);
            s.AppendLine("Barreras AU02 del ramal: " + barrerasSalida + "; barreras viejas que quedan en la salida: " + viejasEnSalida + " (0).");
            fallos += viejasEnSalida > 0 ? 1 : 0;
            if (au02 != null && barrerasSalida == 0)
            {
                s.AppendLine("FALLO: no hay barreras AU02R en el ramal.");
                fallos++;
            }

            // 5) Todo collider propio tiene malla visible (los parapetos los cubren las barreras)
            int sinMalla = 0;
            foreach (Collider c in mia.GetComponentsInChildren<Collider>(true))
            {
                // Los colliders apagados (piezas de Poner sin collider: faroles, reja, barreras) no cuentan.
                if (!c.enabled || c.name.StartsWith("Parapeto") || c.name.StartsWith("ParapetoNorte_") || c.name == "Reja")
                {
                    continue;
                }
                if (!VisibleOCubierto(c, mia))
                {
                    if (sinMalla++ < 8)
                    {
                        s.AppendLine("FALLO: collider sin malla visible: " + c.name);
                    }
                }
            }
            fallos += sinMalla;

            // 6) Nada flotando ni enterrado
            int flota = 0;
            foreach (Renderer r in mia.GetComponentsInChildren<Renderer>(true))
            {
                string n = r.name;
                Bounds b = r.bounds;
                if (n == "AU04_ColumnaRamal")
                {
                    if (b.min.y < -0.15f || b.min.y > 0.25f)
                    {
                        s.AppendLine("FLOTA o enterrada: " + n + " (base en y " + F(b.min.y) + ")");
                        flota++;
                    }
                }
                else if (n == "CabezalRamal")
                {
                    float deck = YEn(pl, SEnPunto(pl, b.center));
                    if (Mathf.Abs(b.max.y - (deck - A.GruesoTablero)) > 0.1f)
                    {
                        s.AppendLine("FLOTA o enterrado: cabezal con tope " + F(b.max.y) + " y losa en " + F(deck - A.GruesoTablero));
                        flota++;
                    }
                }
                else if (n.StartsWith("AU02R_") || n.StartsWith("DS01_RejaPuas_"))
                {
                    float deck = YEn(pl, SEnPunto(pl, b.center));
                    if (Mathf.Abs(b.min.y - deck) > 0.15f)
                    {
                        s.AppendLine("FLOTA o enterrada: " + n + " (base " + F(b.min.y) + ", tablero " + F(deck) + ")");
                        flota++;
                    }
                }
            }
            foreach (Transform f in mia.GetComponentsInChildren<Transform>(true).Where(t => t.name.StartsWith("AU01_LuminariaRamal_")))
            {
                Bounds? b = Caja(f.gameObject, true);
                if (b.HasValue)
                {
                    float deck = YEn(pl, SEnPunto(pl, b.Value.center));
                    if (Mathf.Abs(b.Value.min.y - deck) > 0.25f)
                    {
                        s.AppendLine("FLOTA o enterrado: " + f.name + " (base " + F(b.Value.min.y) + ", tablero " + F(deck) + ")");
                        flota++;
                    }
                }
            }
            int faroles = mia.GetComponentsInChildren<Transform>(true).Count(t => t.name.StartsWith("AU01_LuminariaRamal_"));
            int columnas = mia.GetComponentsInChildren<Transform>(true).Count(t => t.name == "AU04_ColumnaRamal");
            s.AppendLine("Columnas " + columnas + " (esperadas " + pl.Columnas.Count + "), faroles " + faroles + ", objetos flotando o enterrados: " + flota + ".");
            fallos += flota + (columnas != pl.Columnas.Count ? 1 : 0) + (faroles == 0 ? 1 : 0);

            // 7) Segunda boca: el tubo y la roca encima, y el cierre (no es callejón: nadie llega, hay reja antes)
            int tuboMal = 0;
            float yTechoAbajo = YBocaRamal + A.AltoTunel;
            float yTechoArriba = YBocaRamal + A.AltoTunel + 1.2f;
            for (float x = XBocaRamal - 3f; x >= XBocaRamal - LargoTubo + 2f; x -= 3f)
            {
                // Hacia arriba desde dentro del tubo se toca la cara de abajo del techo (y = piso + AltoTunel), no la de arriba.
                if (!Physics.Raycast(new Vector3(x, YBocaRamal + 1f, ZBocaRamal), Vector3.up, out RaycastHit techo, 30f, ~0, QueryTriggerInteraction.Ignore) || techo.collider.name != "Techo" || Mathf.Abs(techo.point.y - yTechoAbajo) > 0.3f)
                {
                    tuboMal++;
                    s.AppendLine("FALLO: sobre el tubo en x = " + F(x) + " no hay techo a la altura esperada (" + F(yTechoAbajo) + ").");
                    continue;
                }
                // Hacia abajo desde arriba: la primera superficie es la roca y más abajo la cara de arriba del techo.
                RaycastHit[] bajan = Physics.RaycastAll(new Vector3(x, 80f, ZBocaRamal), Vector3.down, 80f, ~0, QueryTriggerInteraction.Ignore).OrderBy(h => h.distance).ToArray();
                bool rocaEncima = bajan.Length > 0 && bajan[0].collider.name == "Penasco" && bajan[0].point.y >= yTechoArriba + 0.5f;
                bool techoDebajo = bajan.Any(h => h.collider.name == "Techo" && Mathf.Abs(h.point.y - yTechoArriba) <= 0.3f);
                if (!rocaEncima || !techoDebajo)
                {
                    tuboMal++;
                    s.AppendLine("FALLO: sobre el techo del tubo en x = " + F(x) + (rocaEncima ? " no se ve la cara de arriba del techo." : " no hay roca (el túnel quedaría descubierto)."));
                }
            }
            fallos += tuboMal;
            s.AppendLine("Tubo corto de " + F(LargoTubo) + " m con techo y roca encima: " + (tuboMal == 0 ? "bien" : tuboMal + " fallos") + ". Cerrado al fondo y con reja antes: no hay callejón al que se pueda entrar en carro; no está en el tráfico (CalleZona).");
            Vector3 frente = new Vector3(XBocaRamal + 3f, YBocaRamal + 2f, ZBocaRamal);
            if (Physics.Raycast(frente, Vector3.left, out RaycastHit golpe, 6f, ~0, QueryTriggerInteraction.Ignore) && golpe.distance < 4.5f)
            {
                s.AppendLine("FALLO: la boca está tapada por " + golpe.collider.name + " a " + F(golpe.distance) + " m.");
                fallos++;
            }
            int bocasTrafico = Object.FindObjectsByType<CalleZona>(FindObjectsInactive.Include, FindObjectsSortMode.None).Count(c => c.transform.IsChildOf(mia));
            s.AppendLine("Calles de tráfico del ramal: " + bocasTrafico + " (0: fuera del tráfico).");
            fallos += bocasTrafico > 0 ? 1 : 0;

            // 8) Luces y materiales
            Light[] luces = mia.GetComponentsInChildren<Light>(true);
            s.AppendLine("Luces nuevas: " + luces.Length + " (tope " + MaxLucesNuevas + ").");
            fallos += luces.Length > MaxLucesNuevas ? 1 : 0;
            foreach (Light l in luces)
            {
                if (l.shadows != LightShadows.None)
                {
                    s.AppendLine("FALLO: luz con sombras: " + l.name);
                    fallos++;
                }
            }
            int sinMat = mia.GetComponentsInChildren<Renderer>(true).Count(r => r.sharedMaterial == null);
            s.AppendLine("Renderers sin material: " + sinMat + ".");
            fallos += sinMat;

            // 9) Choques con lo viejo (cajas de lo visible, en el prefab)
            GameObject contenido = PrefabUtility.LoadPrefabContents(RutaPrefab);
            try
            {
                Transform raiz = contenido.transform;
                Transform miaP = raiz.Find(NombreRaiz);
                if (miaP == null)
                {
                    s.AppendLine("FALLO: el prefab no tiene " + NombreRaiz);
                    fallos++;
                }
                else
                {
                    List<Bounds> viejas = new List<Bounds>();
                    Transform acantilado = raiz.Find("Acantilado");
                    Transform fondo = raiz.Find("Fondo");
                    Transform tablero = raiz.Find("Tablero");
                    foreach (Renderer r in raiz.GetComponentsInChildren<Renderer>(false))
                    {
                        if (!r.enabled || !r.gameObject.activeInHierarchy || r.transform.IsChildOf(miaP) || r.name == "Cables" || r.name.StartsWith("Halo") || r.name.StartsWith("Haz") || r.name.StartsWith("Charco") || r.name.StartsWith("Linea"))
                        {
                            continue;
                        }
                        Bounds b = r.bounds;
                        if (b.size.x > 60f || b.size.z > 60f || b.size.y < 0.4f || (acantilado != null && r.transform.IsChildOf(acantilado)) || (fondo != null && r.transform.IsChildOf(fondo))
                            || (tablero != null && r.transform.IsChildOf(tablero)))
                        {
                            continue;
                        }
                        viejas.Add(b);
                    }
                    int choques = 0;
                    foreach (Renderer r in miaP.GetComponentsInChildren<Renderer>(true))
                    {
                        string n = r.name;
                        if (n.StartsWith("Halo") || n.StartsWith("Letrero") || n == "Penasco" || n.StartsWith("Parapeto") || n == "Losa" || n == "Asfalto" || n == "Viga" || n.StartsWith("AU02R_"))
                        {
                            continue;
                        }
                        Bounds b = r.bounds;
                        b.Expand(-0.1f);
                        if (viejas.Any(v => v.Intersects(b)))
                        {
                            if (choques++ < 10)
                            {
                                s.AppendLine("CHOQUE: " + n + " en " + b.center.ToString("F1") + " se encima con algo viejo visible.");
                            }
                        }
                    }
                    Transform pen = miaP.Find("Boca/Penasco");
                    if (pen != null)
                    {
                        Bounds bp = pen.GetComponent<Renderer>().bounds;
                        int encima = viejas.Count(v => v.Intersects(bp));
                        if (encima > 0)
                        {
                            s.AppendLine("CHOQUE: el peñasco (caja " + bp.center.ToString("F1") + ") toca la caja de " + encima + " cosas viejas visibles.");
                            choques += encima;
                        }
                    }
                    int sobreRampa = 0;
                    foreach (Bounds v in viejas)
                    {
                        // Las columnas del propio tablero principal (en su eje z = 250, de menos de 4 m de ancho) lo sostienen: no estorban.
                        if (Mathf.Abs(v.center.z - T.ZTablero) < 0.6f && v.size.x < 4f && v.size.z < 4f && v.max.y < YSalida)
                        {
                            continue;
                        }
                        for (int i = 0; i < pl.Dens.Count; i += 2)
                        {
                            Vector3 q = pl.Dens[i];
                            if (v.max.y > q.y - 2.2f && v.min.y < q.y + 1f && T.Planta(v.ClosestPoint(new Vector3(q.x, v.center.y, q.z)) - q).magnitude < MedioTablero - 0.3f)
                            {
                                if (sobreRampa++ < 6)
                                {
                                    s.AppendLine("CHOQUE: algo viejo visible en " + v.center.ToString("F1") + " cae dentro del volumen de la cubierta en s = " + i);
                                }
                                break;
                            }
                        }
                    }
                    s.AppendLine("Choques con lo viejo: " + choques + " propios y " + sobreRampa + " bajo la cubierta.");
                    fallos += choques + sobreRampa;
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contenido);
            }
            s.AppendLine("Avisos: " + avisos + ".");
            s.AppendLine(fallos == 0 ? "VALIDAR: sin fallos" : "VALIDAR: " + fallos + " fallos");
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/t1415-validar.txt", s.ToString());
            Debug.Log("[Arte] T-1415 validar:\n" + s);
        }

        // ---------------------------------------------------------------- Búsqueda de cámara (después de Aplicar, con la escena abierta)

        private sealed class Candidato
        {
            public Vector3 Pos;
            public Vector3 Adelante;
            public float Puntaje;
            public string Detalle;
            public Vector3 Mira => Pos + Adelante * 30f;
        }

        private static bool EsSueloBajo(Collider c) => c.bounds.max.y < 0.6f;

        private static bool EsTablero(Transform t)
        {
            for (Transform a = t; a != null; a = a.parent)
            {
                if (a.name == "Tablero" || a.name == NombreRaiz)
                {
                    return true;
                }
            }
            return false;
        }

        private static bool Piso(float x, float z, out float y)
        {
            y = 0f;
            RaycastHit[] hs = Physics.RaycastAll(new Vector3(x, 40f, z), Vector3.down, 60f, ~0, QueryTriggerInteraction.Ignore);
            if (hs.Length == 0)
            {
                return false;
            }
            RaycastHit bajo = hs.OrderBy(h => h.point.y).First();
            if (bajo.point.y > 0.6f || bajo.normal.y < 0.7f)
            {
                return false;
            }
            foreach (RaycastHit h in hs)
            {
                if (h.point.y > 0.6f && !EsTablero(h.collider.transform))
                {
                    return false;
                }
            }
            y = bajo.point.y;
            return true;
        }

        private static bool Ocluido(Vector3 desde, Vector3 objetivo, float tolerancia)
        {
            if (Physics.Linecast(desde, objetivo, out RaycastHit hit, ~0, QueryTriggerInteraction.Ignore))
            {
                return Vector3.Distance(hit.point, objetivo) > tolerancia;
            }
            return false;
        }

        /// <summary>
        /// Como Ocluido, pero ignora los parapetos invisibles (los cubren las barreras) y, a menos de <paramref name="cerca"/> m del objetivo, los colliders propios (peñasco, marco y tubo de la boca):
        /// esos no tapan la boca, la forman. Lo demás (el tablero principal, casas, la losa del ramal) sí tapa.
        /// </summary>
        private static bool OcluidoVista(Vector3 desde, Vector3 objetivo, float tolerancia, Transform mia, float cerca)
        {
            Vector3 d = objetivo - desde;
            float largo = d.magnitude;
            if (largo < 0.01f)
            {
                return false;
            }
            foreach (RaycastHit h in Physics.RaycastAll(desde, d / largo, largo, ~0, QueryTriggerInteraction.Ignore).OrderBy(x => x.distance))
            {
                if (h.collider.name.StartsWith("Parapeto"))
                {
                    continue;
                }
                float aObjetivo = Vector3.Distance(h.point, objetivo);
                if (aObjetivo <= tolerancia)
                {
                    return false;
                }
                if (mia != null && h.collider.transform.IsChildOf(mia) && aObjetivo <= cerca)
                {
                    continue;
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// Rejilla de cámaras sobre piso caminable (x -300 a -170, z 205 a 275, alturas 2, 4 y 6 m, campo 70, aspecto 1376/768). Puntúa: la segunda boca arriba a la derecha (3), una columna gruesa
        /// en primer plano a la derecha, a 5 a 22 m (3), la curva del ramal a la vista (2) y la rampa norte a la izquierda (1). Escribe Logs/t1415-camara.txt (mejor y 5 siguientes).
        /// </summary>
        [MenuItem("Mecha Cyberpunk/T-1415/Autopista ramal curvo (buscar camara)")]
        public static void BuscarCamara()
        {
            ColocarS2.Abrir();
            Physics.SyncTransforms();
            Barrera au02 = CargarBarrera(null);
            Plano pl = Calcular(PasoReal(au02));
            StringBuilder s = new StringBuilder("T-1415 BuscarCamara: a nivel de la calle baja: rejilla x -300..-170 cada 5 m, z 205..235 cada 3 m, alturas 2 a 6 m sobre el piso, campo 70, aspecto 1376/768\n");
            Transform mia = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(t => t.name == NombreRaiz);
            // Centro real de la boca (media altura del tubo) y tres puntos más (a los lados y arriba): con ver uno basta (la boca puede verse parcial).
            Vector3 boca = new Vector3(XBocaRamal + 1.5f, YBocaRamal + A.AltoTunel * 0.5f, ZBocaRamal);
            Vector3[] puntosBoca = { boca, boca + new Vector3(0f, 0f, 4f), boca + new Vector3(0f, 0f, -4f), boca + new Vector3(0f, 2f, 0f) };
            // Columnas del ramal: se apunta a su cabezal (el tablero principal, que es un muro, las tapa casi siempre) y las del principal, de abajo.
            List<Vector3> columnas = pl.Columnas.Select(c => new Vector3(c.x, c.y - A.GruesoTablero - 1.2f, c.z)).ToList();
            columnas.AddRange(T.Columnas(T.Principal()).Where(c => c.x > -300f && c.x < -150f).Select(c => new Vector3(c.x, 5f, c.z)));
            List<Vector3> curva = new List<Vector3>();
            for (float m = 20f; m <= pl.Largo - 2f; m += 8f)
            {
                T.PuntoEn(pl.Ramal, m, out Vector3 pt, out _);
                curva.Add(pt + Vector3.up * 1.2f);
            }
            List<Vector3> rampa = T.Resamplear(T.RampaNorte(), 5f).Where(p => p.y >= 3f && p.y <= 7f).Select(p => new Vector3(p.x, p.y + 0.3f, p.z)).ToList();
            s.AppendLine("Columnas candidatas: " + columnas.Count + " (ramal " + pl.Columnas.Count + "); puntos de la curva: " + curva.Count + "; rampa: " + rampa.Count + "; boca: " + boca.ToString("F1"));
            List<Candidato> todos = new List<Candidato>();
            int sinPiso = 0, chocaCerca = 0, bocaTapada = 0, probadas = 0;
            GameObject go = new GameObject("T1415_CamTemp") { hideFlags = HideFlags.HideAndDontSave };
            try
            {
                Camera cam = go.AddComponent<Camera>();
                cam.enabled = false;
                cam.fieldOfView = Campo;
                cam.aspect = Aspecto;
                cam.nearClipPlane = 0.05f;
                cam.farClipPlane = 2000f;
                for (float x = -300f; x <= -170f + 0.01f; x += 5f)
                {
                    for (float z = 205f; z <= 235f + 0.01f; z += 3f)
                    {
                        if (!Piso(x, z, out float suelo))
                        {
                            sinPiso++;
                            continue;
                        }
                        foreach (float altura in new[] { 2f, 3f, 4f, 5f, 6f })
                        {
                            Vector3 pos = new Vector3(x, suelo + altura, z);
                            if (Physics.OverlapSphere(pos, 1f, ~0, QueryTriggerInteraction.Ignore).Any(c => !EsSueloBajo(c)))
                            {
                                chocaCerca++;
                                continue;
                            }
                            if (puntosBoca.All(pb => OcluidoVista(pos, pb, 1.5f, mia, 8f)))
                            {
                                bocaTapada++;
                                continue;
                            }
                            // La columna es opcional (el tablero principal la tapa casi siempre desde la calle baja); si se ve, puntúa.
                            List<Vector3> columnasLibres = columnas.Where(c => { float d = new Vector2(c.x - pos.x, c.z - pos.z).magnitude; return d >= 5f && d <= 70f && !OcluidoVista(pos, c, 3f, mia, 0f); }).ToList();
                            List<Vector3> curvaLibre = curva.Where(p => !OcluidoVista(pos, p, 3f, mia, 0f)).ToList();
                            List<Vector3> rampaLibre = rampa.Where(p => !Ocluido(pos, p, 3f)).ToList();
                            probadas++;
                            Vector3 haciaBoca = boca - pos;
                            float yaw0 = Mathf.Atan2(haciaBoca.x, haciaBoca.z) * Mathf.Rad2Deg;
                            float pitch0 = -Mathf.Atan2(haciaBoca.y, new Vector2(haciaBoca.x, haciaBoca.z).magnitude) * Mathf.Rad2Deg;
                            Candidato mejor = null;
                            for (float izq = 8f; izq <= 36f; izq += 4f)
                            {
                                foreach (float dp in new[] { -6f, -3f, 0f, 3f, 6f })
                                {
                                    Quaternion rot = Quaternion.Euler(pitch0 + dp, yaw0 - izq, 0f);
                                    Vector3 adelante = rot * Vector3.forward;
                                    if (Physics.Raycast(pos, adelante, out RaycastHit golpe, 3f, ~0, QueryTriggerInteraction.Ignore) && !EsSueloBajo(golpe.collider))
                                    {
                                        continue;
                                    }
                                    cam.transform.SetPositionAndRotation(pos, rot);
                                    Vector3 vb = cam.WorldToViewportPoint(boca);
                                    float tBoca = vb.z > 0f ? Mathf.Min(Puntua(vb.x, 0.62f, 0.9f, 0.25f), Puntua(vb.y, 0.5f, 0.78f, 0.25f)) : 0f;
                                    if (tBoca <= 0.3f)
                                    {
                                        continue;
                                    }
                                    float tCol = 0f;
                                    Vector3 vc = Vector3.zero;
                                    foreach (Vector3 c in columnasLibres)
                                    {
                                        Vector3 v = cam.WorldToViewportPoint(c);
                                        float t = v.z > 0f && v.y > 0.1f && v.y < 0.9f ? Puntua(v.x, 0.5f, 0.78f, 0.25f) : 0f;
                                        if (t > tCol)
                                        {
                                            tCol = t;
                                            vc = v;
                                        }
                                    }
                                    int visibles = 0;
                                    foreach (Vector3 p in curvaLibre)
                                    {
                                        Vector3 v = cam.WorldToViewportPoint(p);
                                        if (v.z > 0f && v.x > 0.03f && v.x < 0.98f && v.y > 0.03f && v.y < 0.97f)
                                        {
                                            visibles++;
                                        }
                                    }
                                    float tCurva = curva.Count == 0 ? 0f : Mathf.Clamp01(visibles / (float)curva.Count * 1.5f);
                                    float tRampa = 0f;
                                    Vector3 vr = Vector3.zero;
                                    foreach (Vector3 r in rampaLibre)
                                    {
                                        Vector3 v = cam.WorldToViewportPoint(r);
                                        float t = v.z > 0f && v.y > 0.05f && v.y < 0.8f ? Puntua(v.x, 0.15f, 0.5f, 0.25f) : 0f;
                                        if (t > tRampa)
                                        {
                                            tRampa = t;
                                            vr = v;
                                        }
                                    }
                                    float total = 3f * tBoca + 3f * tCol + 2f * tCurva + 1f * tRampa - (visibles == 0 ? 2f : 0f);
                                    if (mejor == null || total > mejor.Puntaje)
                                    {
                                        mejor = new Candidato
                                        {
                                            Pos = pos,
                                            Adelante = adelante,
                                            Puntaje = total,
                                            Detalle = "boca=(" + F(vb.x) + "," + F(vb.y) + ") columna=(" + F(vc.x) + "," + F(vc.y) + ") curva=" + F(tCurva) + " rampa=(" + F(vr.x) + "," + F(vr.y) + ")",
                                        };
                                    }
                                }
                            }
                            if (mejor != null)
                            {
                                todos.Add(mejor);
                            }
                        }
                    }
                }
                s.AppendLine("Rechazadas: sin piso caminable " + sinPiso + ", collider a menos de 1 m " + chocaCerca + ", boca tapada " + bocaTapada + ". Posiciones puntuadas: " + probadas + ", con puntaje: " + todos.Count + ".");
                if (todos.Count == 0)
                {
                    s.AppendLine("SIN CAMARA: ninguna posición puntúa. Revisar las rechazadas.");
                    Directory.CreateDirectory("Logs");
                    File.WriteAllText(RutaCamara, s.ToString());
                    Debug.LogError("[Arte] T-1415 BuscarCamara sin resultado:\n" + s);
                    return;
                }
                todos = todos.OrderByDescending(c => c.Puntaje).ToList();
                s.AppendLine("Puntaje máximo posible: 9 (boca 3, columna 3, curva 2, rampa 1).");
                List<Candidato> elegidas = new List<Candidato> { todos[0] };
                foreach (Candidato c in todos.Skip(1))
                {
                    if (elegidas.Count >= 6)
                    {
                        break;
                    }
                    if (elegidas.Any(e => Vector3.Distance(e.Pos, c.Pos) < 8f))
                    {
                        continue;
                    }
                    elegidas.Add(c);
                }
                for (int i = 0; i < elegidas.Count; i++)
                {
                    Candidato c = elegidas[i];
                    s.AppendLine((i == 0 ? "CAM0 (elegida)" : "CAM" + i) + " desde=" + V3(c.Pos) + " hacia=" + V3(c.Mira) + " campo=" + F(Campo) + " puntaje=" + F(c.Puntaje) + " " + c.Detalle);
                }
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
            Directory.CreateDirectory("Logs");
            File.WriteAllText(RutaCamara, s.ToString());
            Debug.Log("[Arte] T-1415 BuscarCamara:\n" + s);
        }

        private static List<(Vector3 desde, Vector3 hacia, float puntaje)> LeerCamaras()
        {
            List<(Vector3, Vector3, float)> lista = new List<(Vector3, Vector3, float)>();
            if (!File.Exists(RutaCamara))
            {
                return lista;
            }
            foreach (string linea in File.ReadAllLines(RutaCamara))
            {
                Dictionary<string, string> kv = linea.Split(' ').Where(t => t.Contains("=")).GroupBy(t => t.Substring(0, t.IndexOf('='))).ToDictionary(g => g.Key, g => g.First().Substring(g.Key.Length + 1));
                if (linea.StartsWith("CAM") && kv.ContainsKey("desde") && kv.ContainsKey("hacia"))
                {
                    lista.Add((LeerV3(kv["desde"]), LeerV3(kv["hacia"]), kv.ContainsKey("puntaje") ? float.Parse(kv["puntaje"], Inv) : 0f));
                }
            }
            return lista;
        }

        // ---------------------------------------------------------------- Fotos

        /// <summary>k18 con la mejor cámara de Logs/t1415-camara.txt y k18b con la segunda. No guarda nada; corrida de Unity aparte de Aplicar.</summary>
        [MenuItem("Mecha Cyberpunk/T-1415/Autopista ramal curvo (fotos)")]
        public static void Fotos()
        {
            List<(Vector3 desde, Vector3 hacia, float puntaje)> camaras = LeerCamaras();
            if (camaras.Count == 0)
            {
                Debug.LogError("[Arte] T-1415 fotos: no hay cámaras en " + RutaCamara + ". Corre Aplicar y BuscarCamara antes.");
                return;
            }
            ColocarS2.Abrir();
            Directory.CreateDirectory("docs/capturas/t-1415");
            Directory.CreateDirectory("Logs");
            string[] nombres = { "k18", "k18b" };
            for (int i = 0; i < Mathf.Min(2, camaras.Count); i++)
            {
                string juego = "Logs/t1415-" + nombres[i] + "-juego.png";
                ColocarS2cLook.Foto(camaras[i].desde, camaras[i].hacia, juego, Campo);
                ColocarS2.LadoALado("imageassets/referencias/look/K18_AutopistaElevada.png", juego, "docs/capturas/t-1415/" + nombres[i] + ".png");
            }
        }
    }
}
