using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using MechaCyberpunk.Core.Mundo;
using MechaCyberpunk.Personaje.Visual;
using MechaCyberpunk.Trafico.Calle;
using UnityEditor;
using UnityEngine;
using A = MechaCyberpunk.Arte.Editor.ColocarT1356Autopista;
using T = MechaCyberpunk.Arte.Editor.ColocarT1356Trazado;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-1464 (look 19-nov, sesión A): la Autopista elevada de K18 y A18, con vida. Todo lo nuevo vive bajo "T1464_Autopista" del prefab T1356_Autopista (se borra y rehace).
    /// Qué hace: (1) abre el ramal curvo de T-1415 (quita la reja y los letreros de OBRA) y le ilumina la segunda boca; (2) faroles de sodio AU01 en el borde sur del tablero cada 20 m
    /// (entre los que ya había); (3) estelas de luz del tráfico: cintas emisivas sin Light sobre los carriles del tablero y del ramal; (4) vida bajo la autopista y en el barrio: chatarra AU03,
    /// carros deshechos y estacionados, tambos, huacales, basura y charcos; (5) las marcas Vista19b_K18 y Vista19b_A18 con su Mira y su Campo.
    /// Orden (cada paso en su propia corrida): 1) Aplicar. 2) BuscarCamara (escena con el prefab ya aplicado; escribe Logs/t1464-camara.txt). 3) PonerMarcas (lee ese log y mueve las marcas).
    /// 4) Validar (Logs/t1464-validar.txt). 5) Vista19b.Fotos -tomas K18,A18. Para fotos con carros y gente: PoblarFoto(...) antes de la foto y QuitarFoto() después.
    /// Composición: no se curva el tablero principal (es recto por z = 250 y no se toca). La K18 usa la rampa norte (z = 232) a la izquierda, una columna del tablero en primer plano y la boca del
    /// túnel principal (-330, 10, 250) a la derecha; la A18 usa además el ramal curvo abierto con su boca iluminada.
    /// </summary>
    public static class ZonaAutopistaT1464
    {
        public const string RutaPrefab = "Assets/Arte/Mundo/Zonas/T1356_Autopista.prefab";
        public const string NombreRaiz = "T1464_Autopista";
        public const string RutaCamara = "Logs/t1464-camara.txt";
        public const string RutaValidar = "Logs/t1464-validar.txt";
        public const string RaizFoto = "T1464_PoblarFoto";
        public const int MaxLucesNuevas = 40;

        private const float MedioTablero = 7.2f;
        private const float Aspecto = 1376f / 768f;
        private const float CampoK18 = 70f;
        private const float CampoA18 = 62f;
        private const float XRamal = -205.82f;
        private static readonly Vector3 Boca = new Vector3(-329f, 13.5f, 250f);
        private static readonly Vector3 DesdeK18Defecto = new Vector3(-170f, 5.98f, 223f);
        private static readonly Vector3 HaciaK18Defecto = new Vector3(-199.82f, 4.72f, 226.07f);
        private static readonly Vector3 DesdeA18Defecto = new Vector3(-205f, 46f, 150f);
        private static readonly Vector3 HaciaA18Defecto = new Vector3(-335f, 12f, 238f);

        private static readonly string[] Carros = { "V01a_CarroViejito", "V01b_CarroPro", "V02", "V03", "V04", "V05", "V06", "V07", "V08", "V09", "V10" };
        private static readonly string[] Gente =
        {
            "NP13_Repartidor_a", "NP14_Oficinista_a", "NP07_Pescador_a", "NP08_Turista_a", "NP09_Corredor_a", "NP10_Elotero_a",
            "NP11_Estibador_a", "NP12_Abuela_a", "NP13_Repartidor_b", "NP14_Oficinista_b", "NP07_Pescador_b", "NP08_Turista_b",
        };

        private static readonly StringBuilder informe = new StringBuilder();
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
        private static readonly Dictionary<GameObject, (Vector3 tam, Vector3 centro)> medidas = new Dictionary<GameObject, (Vector3, Vector3)>();

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
            GameObjectUtility.SetStaticEditorFlags(g, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic);
        }

        private static Bounds? Caja(GameObject go)
        {
            Bounds? caja = null;
            foreach (Renderer r in go.GetComponentsInChildren<Renderer>(true))
            {
                if (r.name.StartsWith("Halo"))
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

        private static GameObject Cargar(string ruta)
        {
            GameObject g = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
            if (g == null)
            {
                informe.AppendLine("FALTA el prefab " + ruta + " (se omite lo que lo usa).");
            }
            return g;
        }

        private static GameObject CargarCalle(string codigo)
        {
            return Cargar("Assets/Arte/Calle/Prefabs/" + codigo + ".prefab");
        }

        private static GameObject CargarCarro(string nombre)
        {
            return Cargar("Assets/Arte/Vehiculos/Prefabs/" + nombre + ".prefab");
        }

        private static (Vector3 tam, Vector3 centro) Medida(GameObject prefab, Transform padre)
        {
            if (medidas.TryGetValue(prefab, out (Vector3 tam, Vector3 centro) ya))
            {
                return ya;
            }
            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, padre);
            go.transform.SetPositionAndRotation(new Vector3(0f, -5000f, 0f), Quaternion.identity);
            go.transform.localScale = Vector3.one;
            Renderer[] rs = go.GetComponentsInChildren<Renderer>();
            (Vector3 tam, Vector3 centro) m = (Vector3.one, Vector3.zero);
            if (rs.Length > 0)
            {
                Bounds b = rs[0].bounds;
                foreach (Renderer r in rs)
                {
                    b.Encapsulate(r.bounds);
                }
                m = (b.size, b.center - go.transform.position);
            }
            Object.DestroyImmediate(go);
            medidas[prefab] = m;
            return m;
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

        /// <summary>Pone la pieza con la base de su caja en <paramref name="suelo"/> (centro en planta), girada <paramref name="yaw"/> y a escala uniforme <paramref name="esc"/>.</summary>
        private static GameObject PonerSuelo(Transform padre, GameObject prefab, Vector3 suelo, float yaw, float esc, bool collider, string nombre, float roll = 0f)
        {
            (Vector3 tam, Vector3 centro) m = Medida(prefab, padre);
            Quaternion giro = Quaternion.Euler(0f, yaw, 0f);
            if (Mathf.Abs(roll) > 0.01f)
            {
                giro *= Quaternion.AngleAxis(roll, m.tam.x >= m.tam.z ? Vector3.right : Vector3.forward);
            }
            Vector3 desp = giro * new Vector3(m.centro.x * esc, m.centro.y * esc - m.tam.y * esc * 0.5f, m.centro.z * esc);
            GameObject go = Poner(padre, prefab, suelo - desp + (Mathf.Abs(roll) > 0.01f ? Vector3.up * 0.15f : Vector3.zero), giro, Vector3.one * esc, collider);
            go.name = nombre;
            return go;
        }

        private static Vector3 Huella(Vector3 tam, float yaw, float esc)
        {
            bool gira = Mathf.Abs(Mathf.Sin(yaw * Mathf.Deg2Rad)) > 0.7f;
            return (gira ? new Vector3(tam.z, tam.y, tam.x) : tam) * esc;
        }

        // ---------------------------------------------------------------- Aplicar

        [MenuItem("Mecha Cyberpunk/T-1464/Autopista (aplicar)")]
        public static void Aplicar()
        {
            informe.Clear();
            medidas.Clear();
            informe.AppendLine("T-1464 Autopista, K18 y A18 con vida");
            GameObject contenido = PrefabUtility.LoadPrefabContents(RutaPrefab);
            try
            {
                Transform raiz = contenido.transform;
                Transform vieja = raiz.Find(NombreRaiz);
                if (vieja != null)
                {
                    Object.DestroyImmediate(vieja.gameObject);
                }
                int lucesAntes = contenido.GetComponentsInChildren<Light>(true).Length;
                KitS2c.Obra o = KitS2c.Empezar(NombreRaiz, 1464, raiz);
                AbrirRamal(raiz);
                BocaRamal(o, Hijo(o.Raiz, "BocaRamal"));
                Faroles(o, raiz, Hijo(o.Raiz, "Faroles"));
                Estelas(o, Hijo(o.Raiz, "Estelas"));
                Vida(o, raiz);
                PonerMarcasEn(o.Raiz);
                KitS2c.Terminar(o);
                int lucesDespues = contenido.GetComponentsInChildren<Light>(true).Length;
                informe.AppendLine("Luces: antes " + lucesAntes + ", despues " + lucesDespues + " (nuevas " + (lucesDespues - lucesAntes) + ", tope " + MaxLucesNuevas + ").");
                if (lucesDespues - lucesAntes > MaxLucesNuevas)
                {
                    Debug.LogError("[Arte] T-1464: se pasa del cupo de luces (" + (lucesDespues - lucesAntes) + " > " + MaxLucesNuevas + ").");
                }
                informe.AppendLine("Objetos nuevos bajo " + NombreRaiz + ": " + o.Raiz.GetComponentsInChildren<Transform>(true).Length + ".");
                PrefabUtility.SaveAsPrefabAsset(contenido, RutaPrefab);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contenido);
            }
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/t1464-aplicar.txt", informe.ToString());
            Debug.Log("[Arte] T-1464 aplicar:\n" + informe);
        }

        // ---------------------------------------------------------------- Ramal abierto y su boca

        private static void AbrirRamal(Transform raiz)
        {
            Transform cierre = raiz.Find("T1415_Autopista/Cierre");
            if (cierre == null)
            {
                informe.AppendLine("Ramal: no hay T1415_Autopista/Cierre (ya estaba abierto o T-1415 no se aplico).");
                return;
            }
            Object.DestroyImmediate(cierre.gameObject);
            informe.AppendLine("Ramal: quitadas la reja DS01 y los letreros de OBRA (el tubo sigue cerrado al fondo; el ramal no esta en el trafico CalleZona).");
        }

        private static void BocaRamal(KitS2c.Obra o, Transform padre)
        {
            float x1 = ZonaAutopistaT1415.XBocaRamal - 0.6f;
            float x2 = ZonaAutopistaT1415.XBocaRamal - ZonaAutopistaT1415.LargoTubo + 0.6f;
            float largo = x1 - x2;
            float xc = (x1 + x2) * 0.5f;
            float y0 = ZonaAutopistaT1415.YBocaRamal;
            float z0 = ZonaAutopistaT1415.ZBocaRamal;
            Material tira = KitS2c.Mat("T1464_TiraTunel", () => KitS2c.Emision(new Color(3.2f, 2.7f, 1.9f), Texture2D.whiteTexture));
            int n = 0;
            foreach (float lado in new[] { -1f, 1f })
            {
                foreach (float alto in new[] { 1.3f, 4.4f })
                {
                    Estatico(KitS2c.Losa(o, padre, "TiraPared_" + n++, new Vector3(xc, y0 + alto, z0 + lado * 6.85f), Quaternion.identity, new Vector3(largo, 0.18f, 0.12f), tira, 1f, false));
                }
                Estatico(KitS2c.Losa(o, padre, "TiraTecho_" + n++, new Vector3(xc, y0 + A.AltoTunel - 0.12f, z0 + lado * 3f), Quaternion.identity, new Vector3(largo, 0.12f, 0.5f), tira, 1f, false));
            }
            float xFondo = ZonaAutopistaT1415.XBocaRamal - ZonaAutopistaT1415.LargoTubo;
            Estatico(KitS2c.Losa(o, padre, "FondoBrillo", new Vector3(xFondo + 0.1f, y0 + 3.3f, z0), Quaternion.identity, new Vector3(0.06f, 5.8f, 13f), tira, 1f, false));
            KitS2c.Luz(padre, new Vector3(ZonaAutopistaT1415.XBocaRamal - 1f, y0 + 5f, z0), LightType.Spot, new Color(1f, 0.85f, 0.55f), 300f, 42f, 110f, new Vector3(1f, -0.35f, 0f));
            informe.AppendLine("Boca del ramal: " + n + " tiras emisivas en el tubo, panel de fondo y 1 spot hacia afuera.");
        }

        // ---------------------------------------------------------------- Faroles de sodio cada 20 m en el borde sur del tablero principal

        private static void Faroles(KitS2c.Obra o, Transform raiz, Transform padre)
        {
            GameObject prefab = Cargar("Assets/Arte/Zonas/AU/AU01_Luminaria.prefab");
            if (prefab == null)
            {
                return;
            }
            Transform mia = o.Raiz;
            List<Transform> todos = raiz.GetComponentsInChildren<Transform>(true).Where(t => t != null && !t.IsChildOf(mia)).ToList();
            List<Vector3> existentes = todos.Where(t => t.name.StartsWith("AU01_Luminaria")).Select(t => t.position).Where(p => p.y > 6f).ToList();
            List<Transform> barreras = todos.Where(t => t.name.StartsWith("AU02_") && !t.name.StartsWith("AU02_Curva")).ToList();
            List<Vector3> rampaDensa = T.Resamplear(T.RampaNorte(), 1f);
            (Vector3 tam, Vector3 centro) m = Medida(prefab, padre);
            float escala = Mathf.Clamp(A.AlturaLuminaria / Mathf.Max(0.01f, m.tam.y), 0.3f, 3f);
            int puestos = 0, omitidos = 0, barrerasQuitadas = 0;
            for (float x = -320f; x <= -125f; x += 20f)
            {
                Vector3 pie = new Vector3(x, T.YTablero, T.ZTablero - (MedioTablero - 0.5f));
                if (existentes.Any(p => T.Planta(p - pie).magnitude < 7f) || rampaDensa.Any(q => Mathf.Abs(q.y - T.YTablero) < 0.6f && T.Planta(q - pie).magnitude < 4f))
                {
                    omitidos++;
                    continue;
                }
                Vector3 haciaCalle = Vector3.forward; // hacia el norte, sobre el asfalto
                GameObject go = Poner(padre, prefab, pie, Quaternion.identity, Vector3.one * escala, false);
                go.name = "AU01_Farol_" + puestos;
                Vector3 brazo = T.Planta(m.centro) * escala;
                if (brazo.magnitude > 0.3f)
                {
                    go.transform.rotation = Quaternion.Euler(0f, Vector3.SignedAngle(brazo, haciaCalle, Vector3.up), 0f);
                }
                Bounds b = Caja(go) ?? new Bounds(pie, Vector3.one);
                float alcance = Mathf.Clamp(Vector3.Dot(b.center - pie, haciaCalle) + Mathf.Abs(b.extents.z), 0.5f, 5f);
                Vector3 cabeza = pie + haciaCalle * alcance;
                cabeza.y = b.max.y - 0.3f;
                Vector3 asfalto = pie + haciaCalle * 4f;
                KitS2c.Luz(go.transform, cabeza, LightType.Spot, KitS2c.Calido, KitS2c.IntensidadPoste * 1.3f, 22f, 100f, asfalto - cabeza);
                KitS2c.Halo(o, go.transform, cabeza, T.YTablero, KitS2c.Sodio);
                Estatico(go);
                // Hueco en la barrera AU02 donde queda el pie del farol (el parapeto invisible sigue cerrando el borde).
                foreach (Transform bt in barreras.Where(t => t != null).ToList())
                {
                    Bounds? bb = Caja(bt.gameObject);
                    if (bb.HasValue && Mathf.Abs(bb.Value.center.y - 10.4f) < 1.2f)
                    {
                        Bounds g = bb.Value;
                        g.Expand(new Vector3(0.1f, 5f, 0.1f));
                        if (g.Contains(new Vector3(pie.x, g.center.y, pie.z)))
                        {
                            Object.DestroyImmediate(bt.gameObject);
                            barrerasQuitadas++;
                        }
                    }
                }
                puestos++;
            }
            informe.AppendLine("Faroles AU01 de sodio nuevos en el borde sur del principal: " + puestos + " (cada 20 m, " + omitidos + " omitidos por haber uno a menos de 7 m o la rampa pegada); barreras AU02 abiertas bajo sus pies: " + barrerasQuitadas + ".");
        }

        // ---------------------------------------------------------------- Estelas de luz (cintas emisivas, sin Light)

        private struct Cinta
        {
            public List<Vector3> Linea;
            public float S0, S1, U, Ancho, Dy;
            public bool CabezaAlFinal;
        }

        private static Texture2D TexEstela()
        {
            const string nombre = "T1464_Estela";
            Texture2D ya = KitS2c.Cargar(nombre);
            if (ya != null)
            {
                return ya;
            }
            Texture2D t = new Texture2D(64, 8, TextureFormat.RGBA32, true);
            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 64; x++)
                {
                    float u = x / 63f;
                    float v = (y + 0.5f) / 8f;
                    float a = Mathf.Lerp(0.05f, 1f, Mathf.Pow(u, 3f));
                    float c = Mathf.Clamp01(1.4f - Mathf.Abs(v * 2f - 1f) * 1.3f);
                    float val = Mathf.Max(0.04f, a * c);
                    t.SetPixel(x, y, new Color(val, val, val, 1f));
                }
            }
            t.Apply();
            return KitS2c.Guardar(nombre, t, true, TextureWrapMode.Clamp);
        }

        private static int CintasMalla(KitS2c.Obra o, Transform padre, string nombre, Material mat, List<Cinta> cintas)
        {
            List<Vector3> vs = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> tris = new List<int>();
            foreach (Cinta c in cintas)
            {
                int n = Mathf.Max(2, Mathf.CeilToInt((c.S1 - c.S0) / 2.5f));
                int baseI = vs.Count;
                for (int i = 0; i <= n; i++)
                {
                    float t = i / (float)n;
                    T.PuntoEn(c.Linea, Mathf.Lerp(c.S0, c.S1, t), out Vector3 pt, out Vector3 tg);
                    Vector3 der = Derecha(tg.normalized);
                    Vector3 centro = pt + der * c.U + Vector3.up * c.Dy;
                    vs.Add(centro - der * c.Ancho * 0.5f);
                    vs.Add(centro + der * c.Ancho * 0.5f);
                    float uu = c.CabezaAlFinal ? t : 1f - t;
                    uvs.Add(new Vector2(uu, 0f));
                    uvs.Add(new Vector2(uu, 1f));
                }
                for (int i = 0; i < n; i++)
                {
                    int a0 = baseI + i * 2, a1 = a0 + 1, b0 = a0 + 2, b1 = a0 + 3;
                    tris.AddRange(new[] { a0, b0, a1, a1, b0, b1 });
                }
            }
            if (vs.Count == 0)
            {
                return 0;
            }
            Mesh m = new Mesh { name = o.Nombre + "_" + nombre, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            m.SetVertices(vs);
            m.SetUVs(0, uvs);
            m.SetTriangles(tris, 0);
            m.RecalculateNormals();
            m.RecalculateBounds();
            o.Mallas.Add(m);
            GameObject go = new GameObject(nombre + "_" + cintas.Count);
            go.transform.SetParent(padre, false);
            go.AddComponent<MeshFilter>().sharedMesh = m;
            MeshRenderer mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            Estatico(go);
            return cintas.Count;
        }

        private static void Estelas(KitS2c.Obra o, Transform padre)
        {
            Texture2D tex = TexEstela();
            Material Hacer(string nombre, Color c) => KitS2c.Mat(nombre, () =>
            {
                Material m = KitS2c.Emision(c, tex);
                m.SetColor("_ColorDia", new Color(0.2f, 0.2f, 0.22f));
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                return m;
            });
            Material blanca = Hacer("T1464_EstelaBlanca", new Color(2.8f, 2.6f, 2.2f));
            Material roja = Hacer("T1464_EstelaRoja", new Color(3.0f, 0.25f, 0.18f));
            List<Cinta> bl = new List<Cinta>();
            List<Cinta> ro = new List<Cinta>();
            float[] alturas = { 0.05f, 0.22f, 0.45f, 0.75f };
            List<Vector3> principal = T.Principal();
            // u negativo = norte (carriles rojos, se alejan al oeste), u positivo = sur (blancos, vienen al este).
            float[] carriles = { -5f, -1.9f, 1.9f, 5f };
            foreach (float carril in carriles)
            {
                for (int pasada = 0; pasada < 2; pasada++)
                {
                    float s = o.Rango(8f, 30f);
                    while (s < 205f)
                    {
                        float largo = o.Rango(12f, 42f);
                        Cinta c = new Cinta
                        {
                            Linea = principal, S0 = s, S1 = Mathf.Min(s + largo, 205f), U = carril + o.Rango(-0.5f, 0.5f),
                            Ancho = o.Rango(0.22f, 0.4f), Dy = o.Uno(alturas), CabezaAlFinal = carril > 0f,
                        };
                        (carril > 0f ? bl : ro).Add(c);
                        s += largo + o.Rango(6f, 30f);
                    }
                }
            }
            List<Vector3> ramal = ZonaAutopistaT1415.Ramal(XRamal);
            float largoRamal = T.Longitud(ramal);
            foreach (float carril in new[] { -2f, 2f })
            {
                for (int pasada = 0; pasada < 3; pasada++)
                {
                    float s = o.Rango(6f, 20f);
                    while (s < largoRamal - 22f)
                    {
                        float largo = o.Rango(14f, 34f);
                        (carril > 0f ? bl : ro).Add(new Cinta
                        {
                            Linea = ramal, S0 = s, S1 = Mathf.Min(s + largo, largoRamal - 20f), U = carril + o.Rango(-0.6f, 0.6f),
                            Ancho = o.Rango(0.22f, 0.4f), Dy = o.Uno(alturas) + 0.02f, CabezaAlFinal = carril > 0f,
                        });
                        s += largo + o.Rango(8f, 26f);
                    }
                }
            }
            int nb = CintasMalla(o, padre, "EstelasBlancas", blanca, bl);
            int nr = CintasMalla(o, padre, "EstelasRojas", roja, ro);
            informe.AppendLine("Estelas de luz: " + nb + " blancas y " + nr + " rojas (cintas emisivas, sin Light) sobre los 4 carriles del tablero (x -322 a -125) y los 2 del ramal.");
        }

        // ---------------------------------------------------------------- Vida: chatarra, carros, tambos, huacales, basura y charcos

        private sealed class Contexto
        {
            public List<Bounds> Ocupados;
            public List<Vector3> Rampa;
            public List<Vector3> Columnas;
            public List<Vector3> CalleOeste;
        }

        private static List<Bounds> Ocupados(Transform raiz, Transform mia)
        {
            Transform acantilado = raiz.Find("Acantilado");
            Transform fondo = raiz.Find("Fondo");
            List<Bounds> lista = new List<Bounds>();
            foreach (Renderer r in raiz.GetComponentsInChildren<Renderer>(false))
            {
                if (!r.gameObject.activeInHierarchy || r.transform.IsChildOf(mia) || r.name.StartsWith("Halo") || r.name.StartsWith("Charco"))
                {
                    continue;
                }
                Bounds b = r.bounds;
                if (b.size.x > 60f || b.size.z > 60f || b.size.y < 0.3f)
                {
                    continue;
                }
                if ((acantilado != null && r.transform.IsChildOf(acantilado)) || (fondo != null && r.transform.IsChildOf(fondo)))
                {
                    continue;
                }
                lista.Add(b);
            }
            return lista;
        }

        private static bool PuntoValido(Vector3 c, float alto, Contexto ctx)
        {
            if (c.x > -212f && Mathf.Abs(c.z - T.ZCalleBaja) < 9.5f)
            {
                return false;
            }
            if (ctx.CalleOeste.Any(q => T.Planta(q - c).magnitude < 9.5f))
            {
                return false;
            }
            if (ctx.Rampa.Any(q => T.Planta(q - c).magnitude < 9.2f && q.y < alto + 1.8f))
            {
                return false;
            }
            return !ctx.Columnas.Any(q => T.Planta(q - c).magnitude < 3.8f);
        }

        private static int Esparcir(KitS2c.Obra o, Transform padre, List<GameObject> prefabs, string nombre, int meta, float altoDeseado, bool collider, Contexto ctx, float margen)
        {
            if (prefabs.Count == 0)
            {
                return 0;
            }
            int puestos = 0;
            for (int intento = 0; intento < meta * 80 && puestos < meta; intento++)
            {
                GameObject prefab = o.Uno(prefabs);
                (Vector3 tam, Vector3 centro) m = Medida(prefab, padre);
                float esc = altoDeseado > 0f ? Mathf.Clamp(altoDeseado / Mathf.Max(0.05f, m.tam.y), 0.3f, 4f) : 1f;
                float yaw = o.Rango(0f, 360f);
                Vector3 c = new Vector3(o.Rango(-305f, -130f), 0f, o.Rango(196f, 262f));
                Vector3 h = Huella(m.tam, yaw, esc);
                if (!PuntoValido(c, h.y, ctx))
                {
                    continue;
                }
                Bounds caja = new Bounds(new Vector3(c.x, h.y * 0.5f, c.z), new Vector3(h.x + margen, h.y, h.z + margen));
                if (ctx.Ocupados.Any(b => b.Intersects(caja)))
                {
                    continue;
                }
                GameObject go = PonerSuelo(padre, prefab, c, yaw, esc, collider, nombre + "_" + puestos);
                Estatico(go);
                ctx.Ocupados.Add(caja);
                puestos++;
            }
            return puestos;
        }

        private static void Vida(KitS2c.Obra o, Transform raiz)
        {
            Transform vida = Hijo(o.Raiz, "Vida");
            Contexto ctx = new Contexto
            {
                Ocupados = Ocupados(raiz, o.Raiz),
                Rampa = T.Resamplear(T.RampaNorte(), 2f),
                Columnas = T.Columnas(T.Principal()).Concat(T.Columnas(T.RampaNorte())).ToList(),
                CalleOeste = T.Resamplear(T.CalleOeste(), 4f),
            };
            informe.AppendLine("Vida: " + ctx.Ocupados.Count + " cajas ocupadas leidas del prefab.");

            List<GameObject> chatarra = new List<GameObject>();
            foreach (string v in new[] { "_a", "_b" })
            {
                GameObject g = Cargar("Assets/Arte/Zonas/AU/AU03_ChatarraPuente" + v + ".prefab");
                if (g != null)
                {
                    chatarra.Add(g);
                }
            }
            int nChat = Esparcir(o, Hijo(vida, "Chatarra_AU03"), chatarra, "AU03N", 10, 0f, true, ctx, 1f);

            GameObject tambo = CargarCalle("C16_Tambo");
            int nTambo = Esparcir(o, Hijo(vida, "Tambos"), tambo == null ? new List<GameObject>() : new List<GameObject> { tambo }, "Tambo", 16, 1f, true, ctx, 0.3f);

            GameObject huacal = CargarCalle("C17_Huacales");
            int nHuacal = Esparcir(o, Hijo(vida, "Cajas"), huacal == null ? new List<GameObject>() : new List<GameObject> { huacal }, "Huacales", 12, 1.1f, true, ctx, 0.3f);

            List<GameObject> basura = new List<GameObject>();
            foreach (string b in new[] { "BS01a_Basura", "BS01b_Basura", "BS01c_Basura", "BS01d_Basura", "BS01e_Basura", "BT01_BoteBasura", "C11_Basurero" })
            {
                GameObject g = CargarCalle(b);
                if (g != null)
                {
                    basura.Add(g);
                }
            }
            int nBasura = Esparcir(o, Hijo(vida, "Basura"), basura, "Basura", 26, 0f, false, ctx, 0.1f);

            // Carros deshechos (ladeados, bajo la rampa y el tablero) y estacionados al norte de la calle baja.
            Transform carros = Hijo(vida, "Carros");
            List<GameObject> viejos = new List<GameObject>();
            foreach (string c in new[] { "V01a_CarroViejito", "V03", "V05", "V07", "V09" })
            {
                GameObject g = CargarCarro(c);
                if (g != null)
                {
                    viejos.Add(g);
                }
            }
            int nDeshechos = 0;
            for (int intento = 0; intento < 600 && nDeshechos < 8 && viejos.Count > 0; intento++)
            {
                GameObject prefab = o.Uno(viejos);
                (Vector3 tam, Vector3 centro) m = Medida(prefab, carros);
                float yaw = o.Rango(0f, 360f);
                Vector3 c = new Vector3(o.Rango(-305f, -135f), 0f, o.Rango(196f, 262f));
                Vector3 h = Huella(m.tam, yaw, 1f);
                if (!PuntoValido(c, h.y + 0.3f, ctx))
                {
                    continue;
                }
                Bounds caja = new Bounds(new Vector3(c.x, h.y * 0.5f, c.z), new Vector3(h.x + 0.8f, h.y, h.z + 0.8f));
                if (ctx.Ocupados.Any(b => b.Intersects(caja)))
                {
                    continue;
                }
                GameObject go = PonerSuelo(padre: carros, prefab: prefab, suelo: c, yaw: yaw, esc: 1f, collider: true, nombre: "CarroDeshecho_" + nDeshechos, roll: o.Rango(5f, 9f) * (o.Chance(0.5f) ? 1f : -1f));
                Estatico(go);
                ctx.Ocupados.Add(caja);
                nDeshechos++;
            }
            int nParked = 0;
            for (float x = -208f; x < -132f && nParked < 4 && viejos.Count > 0; x += o.Rango(13f, 19f))
            {
                GameObject prefab = CargarCarro(Carros[(nParked + 2) % Carros.Length]);
                if (prefab == null)
                {
                    continue;
                }
                (Vector3 tam, Vector3 centro) m = Medida(prefab, carros);
                float yaw = o.Chance(0.5f) ? 90f : -90f;
                Vector3 h = Huella(m.tam, yaw, 1f);
                Vector3 c = new Vector3(x, 0f, T.ZCalleBaja + 7f - 0.4f - h.z * 0.5f - 0.0f);
                c.z = T.ZCalleBaja + 7f + 0.4f + h.z * 0.5f;
                Bounds caja = new Bounds(new Vector3(c.x, h.y * 0.5f, c.z), new Vector3(h.x + 0.4f, h.y, h.z + 0.4f));
                if (ctx.Ocupados.Any(b => b.Intersects(caja)) || ctx.Rampa.Any(q => T.Planta(q - c).magnitude < 9f && q.y < 3f))
                {
                    continue;
                }
                GameObject go = PonerSuelo(carros, prefab, c, yaw, 1f, true, "CarroEstacionadoN_" + nParked);
                Estatico(go);
                ctx.Ocupados.Add(caja);
                nParked++;
            }

            Transform charcos = Hijo(vida, "Charcos");
            int nCharcos = 0;
            for (int intento = 0; intento < 800 && nCharcos < 26; intento++)
            {
                Vector3 c = new Vector3(o.Rango(-300f, -125f), 0f, o.Rango(200f, 262f));
                float largo = o.Rango(3f, 8f), ancho = o.Rango(2f, 5f);
                Bounds caja = new Bounds(new Vector3(c.x, 1f, c.z), new Vector3(largo, 3f, largo));
                if (ctx.Ocupados.Any(b => b.Intersects(caja)) || ctx.Rampa.Any(q => T.Planta(q - c).magnitude < 8.5f && q.y < 3.5f) || ctx.Columnas.Any(q => T.Planta(q - c).magnitude < 3f))
                {
                    continue;
                }
                KitS2c.Charco(o, charcos, c, largo, ancho, o.Rango(0f, 180f));
                nCharcos++;
            }
            informe.AppendLine("Vida: chatarra AU03 " + nChat + ", tambos " + nTambo + ", huacales " + nHuacal + ", basura/botes " + nBasura + ", carros deshechos " + nDeshechos + ", estacionados al norte de la calle baja " + nParked + ", charcos " + nCharcos + ".");
            informe.AppendLine("Peatones: no se ponen fijos; los saca GrupoPeatones desde Calle_Baja/Calle_Oeste (banquetas, zona Autopista). Para fotos: PoblarFoto.");
        }

        // ---------------------------------------------------------------- Marcas de cámara

        private static void Marca(Transform padre, string clave, Vector3 desde, Vector3 hacia, float campo)
        {
            Transform vieja = padre.Find("Vista19b_" + clave);
            if (vieja != null)
            {
                Object.DestroyImmediate(vieja.gameObject);
            }
            Transform m = Hijo(padre, "Vista19b_" + clave);
            m.position = desde;
            Transform mira = Hijo(m, "Mira");
            mira.position = hacia;
            Transform c = Hijo(m, "Campo_" + campo.ToString("0.#", Inv));
            c.localPosition = Vector3.zero;
        }

        private static Dictionary<string, (Vector3 desde, Vector3 hacia)> LeerCamaras()
        {
            Dictionary<string, (Vector3, Vector3)> d = new Dictionary<string, (Vector3, Vector3)>();
            if (!File.Exists(RutaCamara))
            {
                return d;
            }
            foreach (string linea in File.ReadAllLines(RutaCamara))
            {
                foreach (string clave in new[] { "K18", "A18" })
                {
                    if (!linea.StartsWith(clave + " ") || d.ContainsKey(clave))
                    {
                        continue;
                    }
                    Dictionary<string, string> kv = linea.Split(' ').Where(t => t.Contains("=")).GroupBy(t => t.Substring(0, t.IndexOf('='))).ToDictionary(g => g.Key, g => g.First().Substring(g.Key.Length + 1));
                    if (kv.ContainsKey("desde") && kv.ContainsKey("hacia"))
                    {
                        d[clave] = (LeerV3(kv["desde"]), LeerV3(kv["hacia"]));
                    }
                }
            }
            return d;
        }

        private static void PonerMarcasEn(Transform padre)
        {
            Dictionary<string, (Vector3 desde, Vector3 hacia)> c = LeerCamaras();
            (Vector3 desde, Vector3 hacia) k = c.ContainsKey("K18") ? c["K18"] : (DesdeK18Defecto, HaciaK18Defecto);
            (Vector3 desde, Vector3 hacia) a = c.ContainsKey("A18") ? c["A18"] : (DesdeA18Defecto, HaciaA18Defecto);
            Marca(padre, "K18", k.desde, k.hacia, CampoK18);
            Marca(padre, "A18", a.desde, a.hacia, CampoA18);
            informe.AppendLine("Marcas: Vista19b_K18 desde " + V3(k.desde) + " (" + (c.ContainsKey("K18") ? "Logs/t1464-camara.txt" : "valor por defecto") + "), Vista19b_A18 desde " + V3(a.desde) + " (" + (c.ContainsKey("A18") ? "Logs/t1464-camara.txt" : "valor por defecto") + ").");
        }

        [MenuItem("Mecha Cyberpunk/T-1464/Autopista (poner marcas de camara)")]
        public static void PonerMarcas()
        {
            informe.Clear();
            GameObject contenido = PrefabUtility.LoadPrefabContents(RutaPrefab);
            try
            {
                Transform mia = contenido.transform.Find(NombreRaiz);
                if (mia == null)
                {
                    Debug.LogError("[Arte] T-1464 PonerMarcas: no existe " + NombreRaiz + "; corre Aplicar antes.");
                    return;
                }
                PonerMarcasEn(mia);
                PrefabUtility.SaveAsPrefabAsset(contenido, RutaPrefab);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contenido);
            }
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/t1464-marcas.txt", informe.ToString());
            Debug.Log("[Arte] T-1464 marcas:\n" + informe);
        }

        // ---------------------------------------------------------------- Busqueda de camaras (en la escena con el prefab aplicado)

        private static bool EsSueloBajo(Collider c) => c.bounds.max.y < 0.6f;

        private static bool EsTablero(Transform t)
        {
            for (Transform a = t; a != null; a = a.parent)
            {
                if (a.name == "Tablero" || a.name == NombreRaiz || a.name == "T1415_Autopista")
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

        /// <summary>True si algo tapa la linea desde-objetivo a mas de <paramref name="tol"/> m del objetivo. Los parapetos invisibles no cuentan.</summary>
        private static bool Tapado(Vector3 desde, Vector3 objetivo, float tol)
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
                return Vector3.Distance(h.point, objetivo) > tol;
            }
            return false;
        }

        private sealed class Candidato
        {
            public Vector3 Pos, Adelante;
            public float Puntaje;
            public string Detalle;
        }

        private static bool Dentro(Vector3 v) => v.z > 0f && v.x > 0.02f && v.x < 0.98f && v.y > 0.02f && v.y < 0.98f;

        private static Vector3? PuntoTorre()
        {
            Transform torre = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(t => t.name == "TorreReflectores");
            if (torre == null)
            {
                return null;
            }
            Bounds? b = Caja(torre.gameObject);
            return b.HasValue ? new Vector3(b.Value.center.x, b.Value.max.y * 0.9f, b.Value.center.z) : (Vector3?)null;
        }

        [MenuItem("Mecha Cyberpunk/T-1464/Autopista (buscar camaras K18 y A18)")]
        public static void BuscarCamara()
        {
            ColocarS2.Abrir();
            Physics.SyncTransforms();
            StringBuilder s = new StringBuilder("T-1464 BuscarCamara (campo K18 " + CampoK18 + ", A18 " + CampoA18 + ", aspecto 1376/768)\n");
            GameObject go = new GameObject("T1464_CamTemp") { hideFlags = HideFlags.HideAndDontSave };
            try
            {
                Camera cam = go.AddComponent<Camera>();
                cam.enabled = false;
                cam.aspect = Aspecto;
                cam.nearClipPlane = 0.05f;
                cam.farClipPlane = 2000f;
                BuscarK18(cam, s);
                BuscarA18(cam, s);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
            Directory.CreateDirectory("Logs");
            File.WriteAllText(RutaCamara, s.ToString());
            Debug.Log("[Arte] T-1464 BuscarCamara:\n" + s);
        }

        private static void BuscarK18(Camera cam, StringBuilder s)
        {
            cam.fieldOfView = CampoK18;
            Vector3[] puntosBoca = { Boca, Boca + new Vector3(0f, 0f, 3f), Boca + new Vector3(0f, 0f, -3f), Boca + new Vector3(0f, 2f, 0f) };
            List<Vector3> columnas = T.Columnas(T.Principal()).Where(c => c.x > -300f && c.x < -130f).Select(c => new Vector3(c.x, 4.5f, c.z)).ToList();
            List<Vector3> rampa = T.Resamplear(T.RampaNorte(), 5f).Where(p => p.y >= 2.5f && p.y <= 9f).Select(p => p + Vector3.up * 0.3f).ToList();
            List<Vector3> techo = new List<Vector3>();
            for (float x = -325f; x <= -135f; x += 15f)
            {
                techo.Add(new Vector3(x, 8.6f, 250f));
            }
            Vector3? torre = PuntoTorre();
            List<Candidato> todos = new List<Candidato>();
            int sinPiso = 0, chocaCerca = 0, bocaTapada = 0;
            for (float x = -260f; x <= -130f + 0.01f; x += 5f)
            {
                for (float z = 198f; z <= 225f + 0.01f; z += 3f)
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
                        if (puntosBoca.All(pb => Tapado(pos, pb, 1.5f)))
                        {
                            bocaTapada++;
                            continue;
                        }
                        List<Vector3> colLibres = columnas.Where(c => { float d = new Vector2(c.x - pos.x, c.z - pos.z).magnitude; return d >= 5f && d <= 60f && !Tapado(pos, c, 3f); }).ToList();
                        List<Vector3> rampaLibre = rampa.Where(p => !Tapado(pos, p, 3f)).ToList();
                        Vector3 haciaBoca = Boca - pos;
                        float yaw0 = Mathf.Atan2(haciaBoca.x, haciaBoca.z) * Mathf.Rad2Deg;
                        float pitch0 = -Mathf.Atan2(haciaBoca.y, new Vector2(haciaBoca.x, haciaBoca.z).magnitude) * Mathf.Rad2Deg;
                        Candidato mejor = null;
                        for (float izq = 0f; izq <= 44f; izq += 4f)
                        {
                            foreach (float dp in new[] { -8f, -4f, 0f, 4f, 8f })
                            {
                                Quaternion rot = Quaternion.Euler(pitch0 + dp, yaw0 - izq, 0f);
                                Vector3 adelante = rot * Vector3.forward;
                                if (Physics.Raycast(pos, adelante, out RaycastHit golpe, 3f, ~0, QueryTriggerInteraction.Ignore) && !EsSueloBajo(golpe.collider))
                                {
                                    continue;
                                }
                                cam.transform.SetPositionAndRotation(pos, rot);
                                Vector3 vb = cam.WorldToViewportPoint(Boca);
                                float tBoca = vb.z > 0f ? Mathf.Min(Puntua(vb.x, 0.72f, 0.92f, 0.2f), Puntua(vb.y, 0.5f, 0.75f, 0.2f)) : 0f;
                                if (tBoca <= 0.4f)
                                {
                                    continue;
                                }
                                float tCol = 0f;
                                Vector3 vc = Vector3.zero;
                                foreach (Vector3 c in colLibres)
                                {
                                    Vector3 v = cam.WorldToViewportPoint(c);
                                    float t = v.z > 0f && v.y > 0.15f && v.y < 0.85f ? Puntua(v.x, 0.52f, 0.68f, 0.2f) * Mathf.Clamp01(40f / Mathf.Max(8f, v.z)) : 0f;
                                    if (t > tCol)
                                    {
                                        tCol = t;
                                        vc = v;
                                    }
                                }
                                float tRampa = 0f;
                                Vector3 vr = Vector3.zero;
                                foreach (Vector3 r in rampaLibre)
                                {
                                    Vector3 v = cam.WorldToViewportPoint(r);
                                    float t = v.z > 0f && v.y > 0.1f && v.y < 0.75f ? Puntua(v.x, 0.15f, 0.5f, 0.25f) : 0f;
                                    if (t > tRampa)
                                    {
                                        tRampa = t;
                                        vr = v;
                                    }
                                }
                                float tTorre = 0f;
                                if (torre.HasValue)
                                {
                                    Vector3 v = cam.WorldToViewportPoint(torre.Value);
                                    tTorre = v.z > 0f ? Puntua(v.x, 0.1f, 0.4f, 0.3f) * Puntua(v.y, 0.65f, 0.95f, 0.3f) : 0f;
                                }
                                int arriba = techo.Count(p => { Vector3 v = cam.WorldToViewportPoint(p); return v.z > 0f && v.y > 0.65f && v.x > 0.05f; });
                                float tTecho = Mathf.Clamp01(arriba / 4f);
                                float total = 3f * tBoca + 3f * tCol + 2f * tRampa + tTorre + tTecho;
                                if (mejor == null || total > mejor.Puntaje)
                                {
                                    mejor = new Candidato
                                    {
                                        Pos = pos, Adelante = adelante, Puntaje = total,
                                        Detalle = "boca=(" + F(vb.x) + "," + F(vb.y) + ") columna=(" + F(vc.x) + "," + F(vc.y) + ") rampa=(" + F(vr.x) + "," + F(vr.y) + ") torre=" + F(tTorre) + " techo=" + arriba,
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
            s.AppendLine("K18: rechazadas sin piso " + sinPiso + ", collider a menos de 1 m " + chocaCerca + ", boca tapada " + bocaTapada + "; con puntaje " + todos.Count + ". Maximo posible 10.");
            EscribirElegidas(s, "K18", CampoK18, todos);
        }

        private static void BuscarA18(Camera cam, StringBuilder s)
        {
            cam.fieldOfView = CampoA18;
            List<Vector3> cubierta = new List<Vector3>();
            for (float x = -325f; x <= -135f; x += 15f)
            {
                cubierta.Add(new Vector3(x, 10f, 250f));
            }
            List<Vector3> ramal = new List<Vector3>();
            List<Vector3> r = ZonaAutopistaT1415.Ramal(XRamal);
            for (float m = 10f; m < T.Longitud(r) - 2f; m += 10f)
            {
                T.PuntoEn(r, m, out Vector3 pt, out _);
                ramal.Add(pt + Vector3.up * 0.3f);
            }
            Vector3? torre = PuntoTorre();
            List<Candidato> todos = new List<Candidato>();
            int chocaCerca = 0, bocaTapada = 0;
            for (float x = -250f; x <= -110f + 0.01f; x += 10f)
            {
                for (float z = 110f; z <= 200f + 0.01f; z += 10f)
                {
                    foreach (float y in new[] { 35f, 45f, 55f, 65f })
                    {
                        Vector3 pos = new Vector3(x, y, z);
                        if (Physics.CheckSphere(pos, 2f, ~0, QueryTriggerInteraction.Ignore))
                        {
                            chocaCerca++;
                            continue;
                        }
                        if (Tapado(pos, Boca, 4f))
                        {
                            bocaTapada++;
                            continue;
                        }
                        Vector3 haciaBoca = Boca - pos;
                        float yaw0 = Mathf.Atan2(haciaBoca.x, haciaBoca.z) * Mathf.Rad2Deg;
                        float pitch0 = -Mathf.Atan2(haciaBoca.y, new Vector2(haciaBoca.x, haciaBoca.z).magnitude) * Mathf.Rad2Deg;
                        Candidato mejor = null;
                        for (float izq = 6f; izq <= 40f; izq += 4f)
                        {
                            foreach (float dp in new[] { -10f, -6f, -3f, 0f, 3f, 6f })
                            {
                                Quaternion rot = Quaternion.Euler(pitch0 + dp, yaw0 - izq, 0f);
                                cam.transform.SetPositionAndRotation(pos, rot);
                                Vector3 vb = cam.WorldToViewportPoint(Boca);
                                float tBoca = vb.z > 0f ? Mathf.Min(Puntua(vb.x, 0.7f, 0.92f, 0.2f), Puntua(vb.y, 0.3f, 0.65f, 0.2f)) : 0f;
                                if (tBoca <= 0.4f)
                                {
                                    continue;
                                }
                                float minX = 2f, maxX = -1f;
                                int vistos = 0;
                                foreach (Vector3 p in cubierta)
                                {
                                    Vector3 v = cam.WorldToViewportPoint(p);
                                    if (Dentro(v))
                                    {
                                        vistos++;
                                        minX = Mathf.Min(minX, v.x);
                                        maxX = Mathf.Max(maxX, v.x);
                                    }
                                }
                                float tCubierta = vistos >= 5 ? Mathf.Clamp01((maxX - minX) / 0.7f) : 0f;
                                int curva = ramal.Count(p => Dentro(cam.WorldToViewportPoint(p)));
                                float tCurva = ramal.Count == 0 ? 0f : Mathf.Clamp01(curva / (ramal.Count * 0.7f));
                                float tTorre = 0f;
                                if (torre.HasValue)
                                {
                                    Vector3 v = cam.WorldToViewportPoint(torre.Value);
                                    tTorre = v.z > 0f ? Puntua(v.x, 0.02f, 0.35f, 0.3f) * Puntua(v.y, 0.6f, 1f, 0.3f) : 0f;
                                }
                                float total = 3f * tBoca + 3f * tCubierta + 2f * tCurva + tTorre;
                                if (mejor == null || total > mejor.Puntaje)
                                {
                                    mejor = new Candidato
                                    {
                                        Pos = pos, Adelante = rot * Vector3.forward, Puntaje = total,
                                        Detalle = "boca=(" + F(vb.x) + "," + F(vb.y) + ") cubierta=" + F(tCubierta) + " (" + vistos + " pts) curva=" + curva + "/" + ramal.Count + " torre=" + F(tTorre),
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
            s.AppendLine("A18: collider a menos de 2 m " + chocaCerca + ", boca tapada " + bocaTapada + "; con puntaje " + todos.Count + ". Maximo posible 9.");
            EscribirElegidas(s, "A18", CampoA18, todos);
        }

        private static void EscribirElegidas(StringBuilder s, string clave, float campo, List<Candidato> todos)
        {
            if (todos.Count == 0)
            {
                s.AppendLine("SIN CAMARA " + clave + ": ninguna posicion puntua; se queda el valor por defecto.");
                Debug.LogError("[Arte] T-1464 BuscarCamara: sin camara para " + clave + ".");
                return;
            }
            todos = todos.OrderByDescending(c => c.Puntaje).ToList();
            List<Candidato> elegidas = new List<Candidato> { todos[0] };
            foreach (Candidato c in todos.Skip(1))
            {
                if (elegidas.Count >= 4)
                {
                    break;
                }
                if (elegidas.All(e => Vector3.Distance(e.Pos, c.Pos) >= 8f))
                {
                    elegidas.Add(c);
                }
            }
            for (int i = 0; i < elegidas.Count; i++)
            {
                Candidato c = elegidas[i];
                s.AppendLine((i == 0 ? clave : clave.ToLowerInvariant() + i) + " desde=" + V3(c.Pos) + " hacia=" + V3(c.Pos + c.Adelante * 30f) + " campo=" + F(campo) + " puntaje=" + F(c.Puntaje) + " " + c.Detalle);
            }
        }

        // ---------------------------------------------------------------- Ayudante de foto: carros en el tablero y gente en la calle baja

        /// <summary>Pone carros reales sobre el tablero y peatones (NP) en las banquetas de la calle baja que la camara <paramref name="cam"/> ve. Devuelve la raiz; quitala con <see cref="QuitarFoto"/>.</summary>
        public static GameObject PoblarFoto(Camera cam)
        {
            return PoblarFoto(cam.transform.position, cam.transform.position + cam.transform.forward * 30f, cam.fieldOfView);
        }

        public static GameObject PoblarFoto(Vector3 desde, Vector3 hacia, float campo)
        {
            QuitarFoto();
            informe.Clear();
            medidas.Clear();
            GameObject raiz = new GameObject(RaizFoto);
            GameObject go = new GameObject("T1464_CamFoto") { hideFlags = HideFlags.HideAndDontSave };
            try
            {
                Physics.SyncTransforms();
                Camera cam = go.AddComponent<Camera>();
                cam.enabled = false;
                cam.fieldOfView = campo;
                cam.aspect = Aspecto;
                cam.nearClipPlane = 0.05f;
                cam.transform.position = desde;
                cam.transform.LookAt(hacia);
                System.Random azar = new System.Random(14640);

                // Carros sobre el tablero, repartidos por los 4 carriles.
                List<Vector3> carrilesPuntos = new List<Vector3>();
                for (float x = -325f; x <= -125f; x += 11f)
                {
                    foreach (float u in new[] { -5f, -1.9f, 1.9f, 5f })
                    {
                        carrilesPuntos.Add(new Vector3(x + (float)azar.NextDouble() * 6f, T.YTablero + 0.02f, T.ZTablero + u));
                    }
                }
                int carros = 0;
                List<Vector3> usados = new List<Vector3>();
                foreach (Vector3 p in carrilesPuntos.OrderBy(_ => azar.Next()))
                {
                    if (carros >= 8)
                    {
                        break;
                    }
                    Vector3 v = cam.WorldToViewportPoint(p + Vector3.up * 0.8f);
                    float dist = Vector3.Distance(desde, p);
                    if (!Dentro(v) || dist < 10f || dist > 170f || usados.Any(q => Vector3.Distance(q, p) < 14f) || Tapado(desde, p + Vector3.up * 0.8f, 1.5f))
                    {
                        continue;
                    }
                    GameObject prefab = CargarCarro(Carros[(carros * 3 + 2) % Carros.Length]);
                    if (prefab == null)
                    {
                        continue;
                    }
                    (Vector3 tam, Vector3 centro) m = Medida(prefab, raiz.transform);
                    Vector3 dir = p.z < T.ZTablero ? Vector3.left : Vector3.right;
                    float yaw = m.tam.x >= m.tam.z ? Mathf.Atan2(-dir.z, dir.x) * Mathf.Rad2Deg : Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
                    GameObject car = PonerSuelo(raiz.transform, prefab, p, yaw, 1f, false, "Carro_" + carros);
                    car.transform.position += Vector3.up * 0.02f;
                    usados.Add(p);
                    carros++;
                }

                // Peatones en las banquetas de la calle baja (z = 215 +- 8.5, x -210 a -125) y en las de la calle oeste (x = -242 +- 8.5).
                List<Vector3> aceras = new List<Vector3>();
                for (float x = -210f; x <= -125f; x += 5f)
                {
                    aceras.Add(new Vector3(x, 0.2f, T.ZCalleBaja - 8.5f));
                    aceras.Add(new Vector3(x, 0.2f, T.ZCalleBaja + 8.5f));
                }
                for (float z = 120f; z <= 180f; z += 6f)
                {
                    aceras.Add(new Vector3(-242f - 8.5f, 0.2f, z));
                    aceras.Add(new Vector3(-242f + 8.5f, 0.2f, z));
                }
                int gente = 0;
                usados.Clear();
                foreach (Vector3 p0 in aceras.OrderBy(_ => azar.Next()))
                {
                    if (gente >= 10)
                    {
                        break;
                    }
                    Vector3 p = p0 + new Vector3((float)azar.NextDouble() * 3f, 0f, 0f);
                    Vector3 v = cam.WorldToViewportPoint(p + Vector3.up * 1f);
                    float dist = Vector3.Distance(desde, p);
                    if (!Dentro(v) || dist < 4f || dist > 70f || usados.Any(q => Vector3.Distance(q, p) < 6f) || Tapado(desde, p + Vector3.up * 1f, 1.2f))
                    {
                        continue;
                    }
                    GameObject visual = AssetDatabase.LoadAssetAtPath<GameObject>(PersonajesNuevosT1389b.RutaVisual(Gente[gente % Gente.Length]));
                    if (visual == null)
                    {
                        informe.AppendLine("FALTA el visual " + Gente[gente % Gente.Length]);
                        gente++;
                        continue;
                    }
                    bool camina = gente % 2 == 0;
                    bool porX = Mathf.Abs(p.x + 242f) > 12f;
                    Vector3 rumbo = porX ? (azar.Next(2) == 0 ? Vector3.right : Vector3.left) : (azar.Next(2) == 0 ? Vector3.forward : Vector3.back);
                    GameObject pe = (GameObject)PrefabUtility.InstantiatePrefab(visual, raiz.transform);
                    pe.name = "Peaton_" + gente;
                    pe.transform.SetPositionAndRotation(p, Quaternion.LookRotation(rumbo));
                    foreach (Collider c in pe.GetComponentsInChildren<Collider>(true))
                    {
                        c.enabled = false;
                    }
                    foreach (Animator an in pe.GetComponentsInChildren<Animator>(true))
                    {
                        an.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                        if (an.runtimeAnimatorController != null)
                        {
                            foreach (AnimatorControllerParameter par in an.parameters)
                            {
                                if (par.type == AnimatorControllerParameterType.Float && par.name.ToLowerInvariant().Contains("vel"))
                                {
                                    an.SetFloat(par.name, camina ? 1.4f : 0f);
                                }
                            }
                            an.Play(0, 0, (float)azar.NextDouble());
                            an.Update(0f);
                        }
                    }
                    usados.Add(p);
                    gente++;
                }
                informe.AppendLine("PoblarFoto: " + carros + " carros en el tablero y " + gente + " peatones en las banquetas.");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
            Debug.Log("[Arte] T-1464 " + informe);
            return raiz;
        }

        public static void QuitarFoto()
        {
            GameObject r;
            while ((r = GameObject.Find(RaizFoto)) != null)
            {
                Object.DestroyImmediate(r);
            }
        }

        // ---------------------------------------------------------------- Validar

        private static int Hijos(Transform mia, string ruta)
        {
            Transform t = mia.Find(ruta);
            return t == null ? 0 : t.childCount;
        }

        [MenuItem("Mecha Cyberpunk/T-1464/Autopista (validar)")]
        public static void Validar()
        {
            ColocarS2.Abrir();
            Physics.SyncTransforms();
            StringBuilder s = new StringBuilder("T-1464 validar Autopista (K18 y A18 con vida)\n");
            List<string> fallos = new List<string>();
            void Falla(string t) { fallos.Add(t); s.AppendLine("FALLO: " + t); }
            Transform[] todos = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Transform mia = todos.FirstOrDefault(t => t.name == NombreRaiz);
            if (mia == null)
            {
                Falla("no existe " + NombreRaiz + " en la escena (corre Aplicar y reabre la escena).");
                Terminar(s, fallos);
                return;
            }

            // Faroles
            List<Transform> faroles = todos.Where(t => t.name.StartsWith("AU01_Luminaria") && t.position.y > 6f && t.position.x > -335f && t.position.x < -120f).ToList();
            int faroNuevos = mia.GetComponentsInChildren<Transform>(true).Count(t => t.name.StartsWith("AU01_Farol_"));
            s.AppendLine("Faroles AU01 en el tablero entre x -335 y -120: " + faroles.Count + " en total, " + faroNuevos + " nuevos de T-1464.");
            if (faroNuevos < 4 || faroles.Count < 8)
            {
                Falla("pocos faroles de sodio (nuevos " + faroNuevos + ", total " + faroles.Count + "; se piden >= 4 y >= 8).");
            }

            // Estelas
            int est = 0;
            foreach (Transform t in mia.Find("Estelas") == null ? new Transform[0] : mia.Find("Estelas").Cast<Transform>())
            {
                int ix = t.name.LastIndexOf('_');
                if (ix > 0 && int.TryParse(t.name.Substring(ix + 1), out int n))
                {
                    est += n;
                }
            }
            s.AppendLine("Estelas de luz (cintas emisivas): " + est + ", luces en ellas: " + (mia.Find("Estelas") == null ? 0 : mia.Find("Estelas").GetComponentsInChildren<Light>(true).Length));
            if (est < 30)
            {
                Falla("pocas estelas (" + est + " < 30).");
            }
            if (mia.Find("Estelas") != null && mia.Find("Estelas").GetComponentsInChildren<Light>(true).Length > 0)
            {
                Falla("las estelas no deben llevar Light.");
            }

            // Vida
            int chat = Hijos(mia, "Vida/Chatarra_AU03"), tambos = Hijos(mia, "Vida/Tambos"), cajas = Hijos(mia, "Vida/Cajas"), basura = Hijos(mia, "Vida/Basura"), charcos = Hijos(mia, "Vida/Charcos");
            Transform carrosT = mia.Find("Vida/Carros");
            int deshechos = carrosT == null ? 0 : carrosT.Cast<Transform>().Count(t => t.name.StartsWith("CarroDeshecho_"));
            int parked = carrosT == null ? 0 : carrosT.Cast<Transform>().Count(t => t.name.StartsWith("CarroEstacionadoN_"));
            s.AppendLine("Vida nueva: chatarra AU03 " + chat + ", tambos " + tambos + ", huacales " + cajas + ", basura/botes " + basura + ", carros deshechos " + deshechos + ", estacionados al norte " + parked + ", charcos " + charcos + ".");
            if (chat < 6) { Falla("chatarra AU03 nueva " + chat + " < 6."); }
            if (tambos < 8) { Falla("tambos " + tambos + " < 8."); }
            if (cajas < 6) { Falla("huacales " + cajas + " < 6."); }
            if (basura < 12) { Falla("basura " + basura + " < 12."); }
            if (deshechos < 3) { Falla("carros deshechos " + deshechos + " < 3."); }
            if (charcos < 14) { Falla("charcos " + charcos + " < 14."); }
            foreach (Transform t in mia.Find("Vida") == null ? new Transform[0] : mia.Find("Vida").GetComponentsInChildren<Transform>(true))
            {
                if (t.name.StartsWith("CarroDeshecho_") || t.name.StartsWith("Tambo_") || t.name.StartsWith("AU03N_"))
                {
                    Bounds? b = Caja(t.gameObject);
                    if (b.HasValue && (b.Value.min.y < -0.6f || b.Value.min.y > 2f))
                    {
                        Falla(t.name + " flota o esta enterrado (min y " + F(b.Value.min.y) + ").");
                    }
                }
            }

            // Luces
            int luces = mia.GetComponentsInChildren<Light>(true).Length;
            s.AppendLine("Luces nuevas bajo " + NombreRaiz + ": " + luces + " (cupo " + MaxLucesNuevas + "); luces totales en la escena: " + Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length + " (tope seguro 1400).");
            if (luces > MaxLucesNuevas)
            {
                Falla("luces nuevas " + luces + " > cupo " + MaxLucesNuevas + ".");
            }

            // Ramal abierto
            Transform cierre = todos.FirstOrDefault(t => t.name == "Cierre" && t.parent != null && t.parent.name == "T1415_Autopista");
            s.AppendLine("Reja y letreros de OBRA del ramal: " + (cierre == null ? "quitados" : "SIGUEN"));
            if (cierre != null)
            {
                Falla("el ramal sigue con la reja de T-1415.");
            }

            // Peatones y trafico: lo que el juego tiene para esta zona
            CalleZona baja = Object.FindObjectsByType<CalleZona>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(c => c.name == "Calle_Baja");
            s.AppendLine("Calle_Baja: " + (baja == null ? "NO existe" : "banquetas=" + baja.Banquetas + ", autopista=" + baja.Autopista + ", zona=" + baja.Zona) + " (GrupoPeatones saca un peaton por banqueta de calle de zona, hasta 16 en total).");
            if (baja == null || !baja.Banquetas || baja.Autopista)
            {
                Falla("Calle_Baja no tiene banquetas de calle de zona: no saldrian peatones.");
            }
            CalleZona principal = Object.FindObjectsByType<CalleZona>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(c => c.name == "Calle_Principal");
            s.AppendLine("Calle_Principal (trafico en el tablero): " + (principal == null ? "NO existe" : "autopista=" + principal.Autopista));
            if (principal == null || !principal.Autopista)
            {
                Falla("Calle_Principal falta o no es autopista: no habria carros reales en el tablero.");
            }
            ZonaTrafico zonaT = Object.FindObjectsByType<ZonaTrafico>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(z => z.Nombre == "Autopista" && z.Contiene(new Vector3(-200f, 10f, 250f)));
            s.AppendLine("ZonaTrafico Autopista que contiene el tablero en (-200,10,250): " + (zonaT == null ? "NO" : zonaT.name));
            if (zonaT == null)
            {
                Falla("ningun ZonaTrafico 'Autopista' contiene el tablero.");
            }
            ConfigPeatonesZona cfg = AssetDatabase.LoadAssetAtPath<ConfigPeatonesZona>(PeatonesZonaT1409.RutaConfig);
            ConfigPeatonesZona.Zona zp = cfg == null ? null : cfg.Buscar("Autopista");
            s.AppendLine("ConfigPeatonesZona 'Autopista': " + (zp == null ? "NO esta" : zp.entradas.Count + " visuales (peso persona " + zp.pesoPersona + ")") + ". Solo NP13 y NP14 salen ahi por diseno de T-1409; NP01-NP06 no existen como visual.");
            if (zp == null)
            {
                Falla("la zona Autopista no esta en ConfigPeatonesZona.");
            }

            // Proyeccion K18
            Transform k = todos.FirstOrDefault(t => t.name == "Vista19b_K18");
            Transform a = todos.FirstOrDefault(t => t.name == "Vista19b_A18");
            GameObject go = new GameObject("T1464_CamValidar") { hideFlags = HideFlags.HideAndDontSave };
            try
            {
                Camera cam = go.AddComponent<Camera>();
                cam.enabled = false;
                cam.aspect = Aspecto;
                cam.nearClipPlane = 0.05f;
                cam.farClipPlane = 2000f;
                Vector3? torre = PuntoTorre();
                if (k == null || k.Find("Mira") == null)
                {
                    Falla("falta la marca Vista19b_K18 con su Mira.");
                }
                else
                {
                    Vector3 desde = k.position;
                    cam.fieldOfView = CampoK18;
                    cam.transform.SetPositionAndRotation(desde, Quaternion.LookRotation(k.Find("Mira").position - desde));
                    Vector3 vb = cam.WorldToViewportPoint(Boca);
                    bool bocaVista = !Tapado(desde, Boca, 3f) || !Tapado(desde, Boca + new Vector3(0f, 0f, 3f), 3f) || !Tapado(desde, Boca + new Vector3(0f, 0f, -3f), 3f);
                    s.AppendLine("K18 desde " + V3(desde) + ": boca (" + F(vb.x) + ", " + F(vb.y) + ") visible=" + bocaVista + " (pedido x 0.65-0.95, y 0.5-0.85).");
                    if (!(vb.z > 0f && vb.x >= 0.65f && vb.x <= 0.95f && vb.y >= 0.5f && vb.y <= 0.85f) || !bocaVista)
                    {
                        Falla("K18: la boca del tunel no cae arriba de la mitad a la derecha o esta tapada.");
                    }
                    Vector3? mejorCol = null;
                    foreach (Vector3 c in T.Columnas(T.Principal()).Where(q => q.x > -300f && q.x < -120f))
                    {
                        Vector3 p = new Vector3(c.x, 4.5f, c.z);
                        Vector3 v = cam.WorldToViewportPoint(p);
                        if (v.z > 4f && v.z < 60f && v.y > 0.1f && v.y < 0.9f && v.x >= 0.5f && v.x <= 0.72f && !Tapado(desde, p, 3f))
                        {
                            mejorCol = v;
                        }
                    }
                    s.AppendLine("K18: columna del tablero en primer plano: " + (mejorCol.HasValue ? "(" + F(mejorCol.Value.x) + ", " + F(mejorCol.Value.y) + ") a " + F(mejorCol.Value.z) + " m" : "NINGUNA") + " (pedido x 0.5-0.72).");
                    if (!mejorCol.HasValue)
                    {
                        Falla("K18: no hay columna del tablero visible en x 0.5-0.72.");
                    }
                    int rampaOk = T.Resamplear(T.RampaNorte(), 5f).Where(p => p.y >= 2f && p.y <= 9f).Count(p =>
                    {
                        Vector3 v = cam.WorldToViewportPoint(p + Vector3.up * 0.3f);
                        return v.z > 0f && v.x >= 0.08f && v.x <= 0.55f && v.y >= 0.1f && v.y <= 0.8f && !Tapado(desde, p + Vector3.up * 0.3f, 3f);
                    });
                    s.AppendLine("K18: puntos de la rampa norte centro-izquierda (x 0.08-0.55) visibles: " + rampaOk + ".");
                    if (rampaOk < 2)
                    {
                        Falla("K18: la rampa no se ve a la izquierda del centro (" + rampaOk + " puntos).");
                    }
                    if (torre.HasValue)
                    {
                        Vector3 vt = cam.WorldToViewportPoint(torre.Value);
                        s.AppendLine("K18: punta de la torre de reflectores (" + F(vt.x) + ", " + F(vt.y) + ") (pedido x < 0.45, y > 0.6).");
                        if (!(vt.z > 0f && vt.x < 0.45f && vt.y > 0.6f))
                        {
                            Falla("K18: la torre de reflectores no queda arriba a la izquierda.");
                        }
                    }
                    else
                    {
                        Falla("no se encontro TorreReflectores.");
                    }
                    int carriles = 0;
                    for (float x = -325f; x <= -130f; x += 10f)
                    {
                        foreach (float u in new[] { -1.9f, 1.9f })
                        {
                            Vector3 v = cam.WorldToViewportPoint(new Vector3(x, 10.1f, 250f + u));
                            if (Dentro(v))
                            {
                                carriles++;
                            }
                        }
                    }
                    s.AppendLine("K18: puntos de carril (donde van las estelas) dentro del cuadro: " + carriles + ".");
                    if (carriles < 4)
                    {
                        Falla("K18: casi no se ven los carriles del tablero (" + carriles + " puntos).");
                    }
                    int faroK = faroles.Count(t => Dentro(cam.WorldToViewportPoint(t.position + Vector3.up * 8f)));
                    s.AppendLine("K18: faroles del tablero dentro del cuadro: " + faroK + ".");
                    if (faroK < 3)
                    {
                        Falla("K18: se ven menos de 3 faroles.");
                    }
                    // Poblar y quitar la foto
                    GameObject pob = PoblarFoto(desde, k.Find("Mira").position, CampoK18);
                    int nc = pob.transform.Cast<Transform>().Count(t => t.name.StartsWith("Carro_"));
                    int np = pob.transform.Cast<Transform>().Count(t => t.name.StartsWith("Peaton_"));
                    s.AppendLine("K18: PoblarFoto puso " + nc + " carros y " + np + " peatones (AlwaysAnimate en los animators: " + pob.GetComponentsInChildren<Animator>(true).Count(an => an.cullingMode == AnimatorCullingMode.AlwaysAnimate) + ").");
                    if (nc < 2)
                    {
                        Falla("PoblarFoto: menos de 2 carros visibles en K18 (" + nc + ").");
                    }
                    if (np < 2)
                    {
                        Falla("PoblarFoto: menos de 2 peatones visibles en K18 (" + np + ").");
                    }
                    QuitarFoto();
                    if (GameObject.Find(RaizFoto) != null)
                    {
                        Falla("QuitarFoto no quito la raiz " + RaizFoto + ".");
                    }
                }

                if (a == null || a.Find("Mira") == null)
                {
                    Falla("falta la marca Vista19b_A18 con su Mira.");
                }
                else
                {
                    Vector3 desde = a.position;
                    cam.fieldOfView = CampoA18;
                    cam.transform.SetPositionAndRotation(desde, Quaternion.LookRotation(a.Find("Mira").position - desde));
                    Vector3 vb = cam.WorldToViewportPoint(Boca);
                    s.AppendLine("A18 desde " + V3(desde) + ": boca (" + F(vb.x) + ", " + F(vb.y) + ") (pedido x 0.6-0.95, y 0.25-0.75).");
                    if (!(vb.z > 0f && vb.x >= 0.6f && vb.x <= 0.95f && vb.y >= 0.25f && vb.y <= 0.75f))
                    {
                        Falla("A18: la boca no cae a la derecha del cuadro.");
                    }
                    float minX = 2f, maxX = -1f;
                    int vistos = 0;
                    for (float x = -325f; x <= -135f; x += 15f)
                    {
                        Vector3 v = cam.WorldToViewportPoint(new Vector3(x, 10f, 250f));
                        if (Dentro(v))
                        {
                            vistos++;
                            minX = Mathf.Min(minX, v.x);
                            maxX = Mathf.Max(maxX, v.x);
                        }
                    }
                    s.AppendLine("A18: tablero principal dentro del cuadro: " + vistos + " de 13 puntos, de x " + F(minX) + " a " + F(maxX) + " (debe cruzar el cuadro: >= 6 puntos, min < 0.4, max > 0.7).");
                    if (vistos < 6 || minX > 0.4f || maxX < 0.7f)
                    {
                        Falla("A18: la autopista no cruza el cuadro.");
                    }
                    List<Vector3> ramal = ZonaAutopistaT1415.Ramal(XRamal);
                    int curva = 0;
                    for (float m = 10f; m < T.Longitud(ramal) - 2f; m += 10f)
                    {
                        T.PuntoEn(ramal, m, out Vector3 pt, out _);
                        if (Dentro(cam.WorldToViewportPoint(pt)))
                        {
                            curva++;
                        }
                    }
                    s.AppendLine("A18: puntos del ramal curvo dentro del cuadro: " + curva + ".");
                    if (curva < 4)
                    {
                        Falla("A18: el ramal curvo casi no se ve (" + curva + " puntos).");
                    }
                    if (torre.HasValue)
                    {
                        Vector3 vt = cam.WorldToViewportPoint(torre.Value);
                        s.AppendLine("A18: punta de la torre (" + F(vt.x) + ", " + F(vt.y) + ").");
                    }
                }
            }
            finally
            {
                Object.DestroyImmediate(go);
                QuitarFoto();
            }
            Terminar(s, fallos);
        }

        private static void Terminar(StringBuilder s, List<string> fallos)
        {
            s.AppendLine(fallos.Count == 0 ? "VALIDAR: sin fallos" : "VALIDAR: " + fallos.Count + " fallos");
            Directory.CreateDirectory("Logs");
            File.WriteAllText(RutaValidar, s.ToString());
            if (fallos.Count > 0)
            {
                Debug.LogError("[Arte] T-1464 validar: " + fallos.Count + " fallos:\n" + string.Join("\n", fallos));
            }
            else
            {
                Debug.Log("[Arte] T-1464 validar:\n" + s);
            }
        }
    }
}
