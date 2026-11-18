using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;
using Random = System.Random;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-1364: variedad en la Colonia del Cerro (referencias K13 y A13). Trabaja sobre el prefab T1355_Cerro (no corre el generador):
    /// cuartos de azotea y pisos extra, casas escalonadas, paleta amplia, más detalle de azotea y más cables.
    /// Idempotente: todo lo que agrega lleva el prefijo <see cref="Prefijo"/> y se borra al empezar; los movimientos y escalas a lo que ya
    /// existía se anotan en "Extra_Registro" (ruta por índices de hermano, delta de posición y factor de escala) y se deshacen al empezar.
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.VariarCerroT1364.Aplicar (y .FotosAntes / .FotosDespues).
    /// </summary>
    public static class VariarCerroT1364
    {
        public const string RutaPrefab = "Assets/Arte/Mundo/Zonas/T1355_Cerro.prefab";
        public const string Prefijo = "Extra_";
        public const string NombreRegistro = "Extra_Registro";
        public const int Semilla = 13640;
        public const string CarpetaMateriales = "Assets/_Proyecto/Mundo/Materiales/T1364";
        public const string RutaMallas = "Assets/_Proyecto/Mundo/Datos/Zonas/T1364_Cerro_Mallas.asset";
        public const string RutaInforme = "Logs/t1364-variar.txt";
        public const string CarpetaFotos = "docs/capturas/bugs-18nov-B/";

        public const float FraccionCuarto = 0.28f;
        public const float FraccionPiso = 0.17f;
        public const float ProbabilidadEscalonar = 0.30f;
        public const float MargenVecina = 0.85f;
        public const float MargenLibre = 0.45f;
        public const float MargenCalle = 0.7f;
        public const float DistanciaVecina = 2f;
        public const float DesnivelMaximo = 2.3f;
        public const float FranjaFrente = 2.3f;
        public const long TopeTriangulosDetalle = 4000000;
        public const int TopeTendederoLavadero = 60;
        public const float FachadaAtras = 0.9f;
        public const float FachadaAdelante = 0.45f;

        private const string BaseTinaco = "TK01_BaseTinaco";
        private const float AnchoBaseTinaco = 1.5f;
        private const float AltoBaseTinaco = 0.7f;
        private const string CarpetaTripo = "Assets/Arte/Zonas/CC/";

        /// <summary>Paleta de K13/A13: rosa mexicano, verde agua, mostaza, azul cielo, terracota, blanco hueso, lila, naranja.</summary>
        public static readonly (string nombre, Color color)[] Paleta =
        {
            ("RosaMexicano", new Color(0.92f, 0.22f, 0.52f)), ("VerdeAgua", new Color(0.42f, 0.8f, 0.68f)),
            ("Mostaza", new Color(0.9f, 0.7f, 0.2f)), ("AzulCielo", new Color(0.45f, 0.7f, 0.92f)),
            ("Terracota", new Color(0.8f, 0.4f, 0.3f)), ("BlancoHueso", new Color(0.93f, 0.9f, 0.8f)),
            ("Lila", new Color(0.7f, 0.58f, 0.86f)), ("Naranja", new Color(0.96f, 0.5f, 0.16f)),
        };

        private static readonly string[] TiposDetalle = { "Tinaco", "Aire", "Antena", "Macetas", "Tendedero", "Lavadero" };
        private static readonly float[] PesosDetalle = { 25f, 22f, 18f, 17f, 8f, 10f };

        /// <summary>Una casa del Cerro (kit "Casa_N" o de Tripo "CC0x_CasaCerro") con su caja en planta y su azotea.</summary>
        public sealed class CasaT
        {
            public Transform Raiz;
            public Transform Cimiento;
            public Transform Cuerpo;
            public bool Kit;
            public float Normal;
            public float X0, X1, Z0, Z1, Base, Top;
            public int Color = -1;
            public bool Bloqueada;
            public float Frente => Normal > 0f ? X1 : X0;
            public float Ancho => Z1 - Z0;
        }

        private sealed class Cuenta
        {
            public int Tinte, Escalonadas, Pisos, Cuartos, Detalles, Cables, Reubicados, EscalonesDeshechos, PisosDeshechos, CuartosDeshechos;
            public long TriDetalle;
            public int TendLav;
            public readonly Dictionary<string, int> PorTipo = new Dictionary<string, int>();
            public readonly List<float> Fracciones = new List<float>();
        }

        // ================================================================ Lectura (también la usan las pruebas)

        public static bool Cubierta(Transform t, out Bounds b)
        {
            Renderer[] rs = t.GetComponentsInChildren<Renderer>(true);
            if (rs.Length == 0)
            {
                b = default;
                return false;
            }
            b = rs[0].bounds;
            for (int i = 1; i < rs.Length; i++)
            {
                b.Encapsulate(rs[i].bounds);
            }
            return true;
        }

        public static List<CasaT> Leer(Transform raiz)
        {
            List<CasaT> lista = new List<CasaT>();
            Transform casas = raiz.Find("Casas");
            if (casas == null)
            {
                return lista;
            }
            for (int i = 0; i < casas.childCount; i++)
            {
                Transform t = casas.GetChild(i);
                bool kit = t.name.StartsWith("Casa_");
                bool tripo = t.name.StartsWith("CC01_CasaCerro") || t.name.StartsWith("CC02_CasaCerro");
                if (!kit && !tripo)
                {
                    continue;
                }
                Transform cuerpo = kit ? t.Find("Cuerpo") : null;
                Bounds b = default;
                bool hay = kit ? cuerpo != null && Cubierta(cuerpo, out b) : Cubierta(t, out b);
                if (!hay)
                {
                    continue;
                }
                CasaT c = new CasaT
                {
                    Raiz = t,
                    Cuerpo = cuerpo,
                    Kit = kit,
                    Cimiento = i + 1 < casas.childCount && casas.GetChild(i + 1).name == "Cimiento" ? casas.GetChild(i + 1) : null,
                    X0 = b.min.x, X1 = b.max.x, Z0 = b.min.z, Z1 = b.max.z, Base = b.min.y, Top = b.max.y,
                    Normal = kit ? (t.position.x - b.center.x >= 0f ? 1f : -1f) : (t.forward.x >= 0f ? 1f : -1f),
                };
                lista.Add(c);
            }
            return lista;
        }

        /// <summary>Vecinas de <paramref name="c"/> a menos de <paramref name="maxGap"/> m; borde: 0 x-, 1 x+, 2 z-, 3 z+.</summary>
        public static void Vecinas(CasaT c, List<CasaT> todas, float maxGap, Action<CasaT, int> alEncontrar)
        {
            foreach (CasaT n in todas)
            {
                if (n == c)
                {
                    continue;
                }
                float ovZ = Mathf.Min(c.Z1, n.Z1) - Mathf.Max(c.Z0, n.Z0);
                float ovX = Mathf.Min(c.X1, n.X1) - Mathf.Max(c.X0, n.X0);
                if (ovZ > 0.3f)
                {
                    float g = n.X0 - c.X1;
                    if (g > -0.1f && g < maxGap)
                    {
                        alEncontrar(n, 1);
                    }
                    g = c.X0 - n.X1;
                    if (g > -0.1f && g < maxGap)
                    {
                        alEncontrar(n, 0);
                    }
                }
                if (ovX > 0.3f)
                {
                    float g = n.Z0 - c.Z1;
                    if (g > -0.1f && g < maxGap)
                    {
                        alEncontrar(n, 3);
                    }
                    g = c.Z0 - n.Z1;
                    if (g > -0.1f && g < maxGap)
                    {
                        alEncontrar(n, 2);
                    }
                }
            }
        }

        /// <summary>Bordes de la azotea que dan a otra azotea a menos de <see cref="DistanciaVecina"/> m (x-, x+, z-, z+).</summary>
        public static bool[] Bordes(CasaT c, List<CasaT> todas)
        {
            bool[] r = new bool[4];
            Vecinas(c, todas, DistanciaVecina, (n, e) => r[e] = true);
            return r;
        }

        // ================================================================ Aplicar

        [MenuItem("Mecha Cyberpunk/T1364/Cerro: variar")]
        public static void Aplicar()
        {
            StringBuilder log = new StringBuilder();
            Cuenta cuenta = new Cuenta();
            GameObject contenido = PrefabUtility.LoadPrefabContents(RutaPrefab);
            try
            {
                Transform raiz = contenido.transform;
                Estados.Clear();
                Deshacer(raiz);
                BorrarExtras(raiz);
                Random az = new Random(Semilla);
                Transform detalles = raiz.Find("Detalles");
                List<CasaT> casas = Leer(raiz);
                int kit = casas.Count(c => c.Kit);
                log.AppendLine("Casas: " + casas.Count + " (kit " + kit + ", Tripo " + (casas.Count - kit) + ")");
                foreach (CasaT c in casas)
                {
                    c.Bloqueada = TieneAcceso(detalles, c);
                }

                Retintar(raiz, casas, az, cuenta, log);
                Escalonar(raiz, detalles, casas, az, cuenta);
                casas = Leer(raiz);
                foreach (CasaT c in casas)
                {
                    c.Bloqueada = TieneAcceso(detalles, c);
                }

                List<CasaT> orden = casas.Where(c => c.Kit).OrderBy(c => az.Next()).ToList();
                int cuotaPiso = Mathf.RoundToInt(kit * FraccionPiso);
                int cuotaCuarto = Mathf.RoundToInt(kit * FraccionCuarto);
                HashSet<CasaT> conPiso = new HashSet<CasaT>();
                foreach (CasaT c in orden)
                {
                    if (cuenta.Pisos >= cuotaPiso)
                    {
                        break;
                    }
                    if (SubirPiso(raiz, detalles, c, casas))
                    {
                        conPiso.Add(c);
                        cuenta.Pisos++;
                    }
                }
                ResolverEncimes(raiz, cuenta, log);
                conPiso.Clear();
                casas = Leer(raiz);
                foreach (CasaT c in casas)
                {
                    if (Estados.TryGetValue(c.Raiz, out EstadoCasa ec) && ec.Dy != 0f)
                    {
                        conPiso.Add(orden.First(o => o.Raiz == c.Raiz));
                    }
                }
                Dictionary<Transform, CasaT> porRaiz = casas.ToDictionary(c => c.Raiz);
                foreach (CasaT c in orden)
                {
                    if (cuenta.Cuartos >= cuotaCuarto)
                    {
                        break;
                    }
                    if (conPiso.Contains(c) || !porRaiz.TryGetValue(c.Raiz, out CasaT actual))
                    {
                        continue;
                    }
                    if (PonerCuarto(raiz, detalles, actual, casas, az, cuenta))
                    {
                        cuenta.Cuartos++;
                    }
                }

                casas = Leer(raiz);
                QuitarCuartosEncimados(raiz, casas, cuenta);
                PonerDetalles(raiz, detalles, casas, az, cuenta);
                PonerCables(raiz, casas, az, cuenta);

                Escribir(raiz, casas, cuenta, log);
                PrefabUtility.SaveAsPrefabAsset(contenido, RutaPrefab);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contenido);
            }
            Directory.CreateDirectory("Logs");
            File.WriteAllText(RutaInforme, log.ToString());
            AssetDatabase.SaveAssets();
            Debug.Log("[Arte] T1364:\n" + log);
        }

        // ================================================================ Registro de lo movido

        private static Transform Registro(Transform raiz)
        {
            Transform r = raiz.Find(NombreRegistro);
            if (r == null)
            {
                r = new GameObject(NombreRegistro).transform;
                r.SetParent(raiz, false);
            }
            return r;
        }

        private static string RutaIndices(Transform raiz, Transform t)
        {
            List<int> idx = new List<int>();
            while (t != raiz)
            {
                idx.Add(t.GetSiblingIndex());
                t = t.parent;
            }
            idx.Reverse();
            return string.Join("/", idx);
        }

        private static void Anotar(Transform raiz, Transform t, Vector3 delta, Vector3 factorEscala)
        {
            GameObject e = new GameObject("Extra_Mov|" + RutaIndices(raiz, t) + "|" + t.name);
            e.transform.SetParent(Registro(raiz), false);
            e.transform.localPosition = delta;
            e.transform.localScale = factorEscala;
        }

        private static void Mover(Transform raiz, Transform t, Vector3 delta)
        {
            t.position += delta;
            Anotar(raiz, t, delta, Vector3.one);
        }

        private static void Deshacer(Transform raiz)
        {
            Transform reg = raiz.Find(NombreRegistro);
            if (reg == null)
            {
                return;
            }
            for (int i = reg.childCount - 1; i >= 0; i--)
            {
                Transform e = reg.GetChild(i);
                string[] partes = e.name.Split('|');
                if (partes.Length < 3)
                {
                    continue;
                }
                Transform t = raiz;
                bool ok = true;
                foreach (string s in partes[1].Split('/'))
                {
                    if (!int.TryParse(s, out int idx) || idx >= t.childCount)
                    {
                        ok = false;
                        break;
                    }
                    t = t.GetChild(idx);
                }
                if (!ok || t.name != partes[2])
                {
                    Debug.LogWarning("[Arte] T1364: no encuentro " + e.name + " para deshacer.");
                    continue;
                }
                t.position -= e.localPosition;
                Vector3 f = e.localScale;
                Vector3 s0 = t.localScale;
                t.localScale = new Vector3(s0.x / f.x, s0.y / f.y, s0.z / f.z);
            }
        }

        private static void BorrarExtras(Transform raiz)
        {
            List<Transform> lista = new List<Transform>();
            foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true))
            {
                if (t != raiz && t.name.StartsWith(Prefijo) && !t.parent.name.StartsWith(Prefijo))
                {
                    lista.Add(t);
                }
            }
            foreach (Transform t in lista)
            {
                if (t != null)
                {
                    Object.DestroyImmediate(t.gameObject);
                }
            }
        }

        // ================================================================ Utilidades

        private static bool TieneAcceso(Transform detalles, CasaT c)
        {
            if (detalles == null)
            {
                return false;
            }
            foreach (Transform t in detalles)
            {
                if (t.name.StartsWith("Acceso_") && t.position.x > c.X0 - 0.8f && t.position.x < c.X1 + 0.8f && t.position.z > c.Z0 - 0.3f && t.position.z < c.Z1 + 0.3f)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>Cosas de Detalles (tendederos, lavaderos) sobre la azotea de la casa.</summary>
        private static List<Transform> EnDetalles(Transform detalles, CasaT c)
        {
            List<Transform> r = new List<Transform>();
            if (detalles == null)
            {
                return r;
            }
            foreach (Transform t in detalles)
            {
                if (t.name.StartsWith("Acceso_") || t.name.StartsWith(Prefijo))
                {
                    continue;
                }
                Vector3 p = t.position;
                if (p.x > c.X0 - 0.3f && p.x < c.X1 + 0.3f && p.z > c.Z0 - 0.1f && p.z < c.Z1 + 0.1f && p.y >= c.Top - 0.6f && p.y <= c.Top + 3f)
                {
                    r.Add(t);
                }
            }
            return r;
        }

        private static List<Transform> EnAzotea(CasaT c, float top, bool conExtras = false)
        {
            List<Transform> r = new List<Transform>();
            foreach (Transform t in c.Raiz)
            {
                if (t.name == "Cuerpo" || (!conExtras && t.name.StartsWith(Prefijo)))
                {
                    continue;
                }
                if (Cubierta(t, out Bounds b) && b.min.y >= top - 0.3f)
                {
                    r.Add(t);
                }
            }
            return r;
        }

        private static long Triangulos(GameObject g)
        {
            long n = 0;
            foreach (MeshFilter mf in g.GetComponentsInChildren<MeshFilter>(true))
            {
                Mesh m = mf.sharedMesh;
                if (m == null)
                {
                    continue;
                }
                for (int i = 0; i < m.subMeshCount; i++)
                {
                    n += m.GetSubMesh(i).indexCount / 3;
                }
            }
            return n;
        }

        private static void PonerColor(Material m, Color c)
        {
            m.SetColor(m.HasProperty("_BaseColor") ? "_BaseColor" : "_Color", c);
        }

        // ================================================================ 3. Colores

        private static Material MatTinte(string nombre, Material origen, Color color)
        {
            if (!AssetDatabase.IsValidFolder(CarpetaMateriales))
            {
                AssetDatabase.CreateFolder("Assets/_Proyecto/Mundo/Materiales", "T1364");
            }
            string ruta = CarpetaMateriales + "/" + nombre + ".mat";
            Material m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (m == null)
            {
                m = new Material(origen) { name = nombre };
                AssetDatabase.CreateAsset(m, ruta);
            }
            PonerColor(m, color);
            EditorUtility.SetDirty(m);
            return m;
        }

        private static void Retintar(Transform raiz, List<CasaT> casas, Random az, Cuenta cuenta, StringBuilder log)
        {
            Material origenFachada = null;
            foreach (Renderer r in raiz.GetComponentsInChildren<Renderer>(true))
            {
                Material m = r.sharedMaterial;
                if (m != null && m.name.Contains("C03_Fachada") && !m.name.StartsWith("T1364_"))
                {
                    origenFachada = m;
                    break;
                }
            }
            if (origenFachada == null)
            {
                string guid = AssetDatabase.FindAssets("T1364_Fachada_ t:Material", new[] { CarpetaMateriales }).FirstOrDefault();
                origenFachada = guid == null ? null : AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
            }
            Material origenCuerpo = KitS2c.MatCuerpo(0);
            Material[] cuerpos = new Material[Paleta.Length];
            Material[] fachadas = new Material[Paleta.Length];
            for (int k = 0; k < Paleta.Length; k++)
            {
                Color c = Paleta[k].color;
                cuerpos[k] = MatTinte("T1364_Cuerpo_" + Paleta[k].nombre, origenCuerpo, new Color(c.r * 0.8f, c.g * 0.8f, c.b * 0.8f));
                fachadas[k] = origenFachada == null ? null : MatTinte("T1364_Fachada_" + Paleta[k].nombre, origenFachada, c);
            }
            if (origenFachada == null)
            {
                log.AppendLine("AVISO: no hay material C03_Fachada: solo se tiñen los cuerpos.");
            }
            foreach (CasaT c in casas.Where(x => x.Kit))
            {
                List<int> usados = new List<int>();
                foreach (CasaT n in casas)
                {
                    if (n.Color >= 0)
                    {
                        float ovZ = Mathf.Min(c.Z1, n.Z1) - Mathf.Max(c.Z0, n.Z0);
                        float ovX = Mathf.Min(c.X1, n.X1) - Mathf.Max(c.X0, n.X0);
                        float gx = Mathf.Max(n.X0 - c.X1, c.X0 - n.X1), gz = Mathf.Max(n.Z0 - c.Z1, c.Z0 - n.Z1);
                        if ((ovZ > 0.3f && gx < 3.2f) || (ovX > 0.3f && gz < 0.6f))
                        {
                            usados.Add(n.Color);
                        }
                    }
                }
                List<int> libres = Enumerable.Range(0, Paleta.Length).Where(k => !usados.Contains(k)).ToList();
                if (libres.Count == 0)
                {
                    libres = Enumerable.Range(0, Paleta.Length).ToList();
                }
                c.Color = libres[az.Next(libres.Count)];
                foreach (Renderer r in c.Raiz.GetComponentsInChildren<Renderer>(true))
                {
                    Material[] ms = r.sharedMaterials;
                    bool cambio = false;
                    for (int i = 0; i < ms.Length; i++)
                    {
                        string nombre = ms[i] == null ? "" : ms[i].name;
                        if (nombre.StartsWith("S2c_Cuerpo_") || nombre.StartsWith("T1364_Cuerpo_"))
                        {
                            ms[i] = cuerpos[c.Color];
                            cambio = true;
                        }
                        else if ((nombre.Contains("C03_Fachada") || nombre.StartsWith("T1364_Fachada_")) && fachadas[c.Color] != null)
                        {
                            ms[i] = fachadas[c.Color];
                            cambio = true;
                        }
                    }
                    if (cambio)
                    {
                        r.sharedMaterials = ms;
                    }
                }
                cuenta.Tinte++;
            }
        }

        // ================================================================ 2. Escalonado

        private static void Escalonar(Transform raiz, Transform detalles, List<CasaT> casas, Random az, Cuenta cuenta)
        {
            foreach (CasaT c in casas)
            {
                if (c.Bloqueada || az.NextDouble() >= ProbabilidadEscalonar)
                {
                    continue;
                }
                bool adelante = az.Next(2) == 0;
                float mag = adelante ? Range(az, 0.3f, FachadaAdelante) : Range(az, 0.3f, FachadaAtras);
                float dx = c.Normal * (adelante ? mag : -mag);
                List<Transform> techo = EnDetalles(detalles, c);
                Mover(raiz, c.Raiz, new Vector3(dx, 0f, 0f));
                if (c.Cimiento != null)
                {
                    Mover(raiz, c.Cimiento, new Vector3(dx, 0f, 0f));
                }
                foreach (Transform t in techo)
                {
                    Mover(raiz, t, new Vector3(dx, 0f, 0f));
                }
                cuenta.Escalonadas++;
                Estado(c.Raiz).Dx = dx;
                Estado(c.Raiz).EscTecho = techo;
            }
        }

        // ================================================================ Choques entre cuerpos

        private sealed class EstadoCasa
        {
            public float Dx;
            public List<Transform> EscTecho;
            public float Dy;
            public float Factor;
            public List<Transform> Arriba, PisoTecho;
            public GameObject Copia;
        }

        private static readonly Dictionary<Transform, EstadoCasa> Estados = new Dictionary<Transform, EstadoCasa>();

        private static EstadoCasa Estado(Transform casa)
        {
            if (!Estados.TryGetValue(casa, out EstadoCasa e))
            {
                e = new EstadoCasa();
                Estados[casa] = e;
            }
            return e;
        }

        private static bool Encima(Bounds a, Bounds b)
        {
            const float margen = 0.05f, minimo = 0.1f;
            for (int eje = 0; eje < 3; eje++)
            {
                float solape = Mathf.Min(a.max[eje], b.max[eje]) - Mathf.Max(a.min[eje], b.min[eje]) - 2f * margen;
                if (solape <= minimo)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>Pares de cuerpos (kit) de casas distintas que se enciman más de 0.1 m (con margen de 0.05 m).</summary>
        public static List<(CasaT a, CasaT b)> CuerposEncimados(List<CasaT> casas)
        {
            List<(CasaT, CasaT)> r = new List<(CasaT, CasaT)>();
            List<CasaT> kit = casas.Where(c => c.Kit).ToList();
            List<Bounds> cajas = kit.Select(c => { Cubierta(c.Cuerpo, out Bounds b); return b; }).ToList();
            for (int i = 0; i < kit.Count; i++)
            {
                for (int j = i + 1; j < kit.Count; j++)
                {
                    if (Encima(cajas[i], cajas[j]))
                    {
                        r.Add((kit[i], kit[j]));
                    }
                }
            }
            return r;
        }

        /// <summary>Un cuarto de azotea que se mete en el cuerpo de otra casa se quita.</summary>
        private static void QuitarCuartosEncimados(Transform raiz, List<CasaT> casas, Cuenta cuenta)
        {
            foreach (CasaT c in casas.Where(x => x.Kit))
            {
                foreach (Transform t in c.Raiz.Cast<Transform>().Where(x => x.name.StartsWith(Prefijo + "Cuarto")).ToList())
                {
                    if (!Cubierta(t, out Bounds bc))
                    {
                        continue;
                    }
                    bool choca = casas.Any(o => o.Kit && o != c && Cubierta(o.Cuerpo, out Bounds bo) && Encima(bc, bo));
                    if (choca)
                    {
                        Object.DestroyImmediate(t.gameObject);
                        cuenta.Cuartos--;
                        cuenta.CuartosDeshechos++;
                    }
                }
            }
        }

        private static void DeshacerEscalon(Transform raiz, CasaT c, EstadoCasa e)
        {
            Vector3 d = new Vector3(-e.Dx, 0f, 0f);
            Mover(raiz, c.Raiz, d);
            if (c.Cimiento != null)
            {
                Mover(raiz, c.Cimiento, d);
            }
            foreach (Transform t in e.EscTecho)
            {
                Mover(raiz, t, d);
            }
            e.Dx = 0f;
        }

        private static void DeshacerPiso(Transform raiz, CasaT c, EstadoCasa e)
        {
            foreach (Transform t in e.Arriba.Concat(e.PisoTecho))
            {
                Mover(raiz, t, new Vector3(0f, -e.Dy, 0f));
            }
            Mover(raiz, c.Cuerpo, new Vector3(0f, -e.Dy * 0.5f, 0f));
            Vector3 s0 = c.Cuerpo.localScale;
            c.Cuerpo.localScale = new Vector3(s0.x, s0.y / e.Factor, s0.z);
            Anotar(raiz, c.Cuerpo, Vector3.zero, new Vector3(1f, 1f / e.Factor, 1f));
            if (e.Copia != null)
            {
                Object.DestroyImmediate(e.Copia);
            }
            e.Dy = 0f;
        }

        /// <summary>Deshace el escalonado y luego el piso extra de las casas cuyos cuerpos se enciman, hasta que no quede ningún par.</summary>
        private static void ResolverEncimes(Transform raiz, Cuenta cuenta, StringBuilder log)
        {
            for (int vuelta = 0; vuelta < 20; vuelta++)
            {
                List<CasaT> casas = Leer(raiz);
                List<(CasaT a, CasaT b)> pares = CuerposEncimados(casas);
                if (pares.Count == 0)
                {
                    return;
                }
                bool algo = false;
                foreach ((CasaT a, CasaT b) par in pares)
                {
                    CasaT[] dos = { par.a, par.b };
                    bool hizo = false;
                    foreach (CasaT h in dos)
                    {
                        if (Estados.TryGetValue(h.Raiz, out EstadoCasa e) && e.Dx != 0f)
                        {
                            DeshacerEscalon(raiz, h, e);
                            cuenta.EscalonesDeshechos++;
                            cuenta.Escalonadas--;
                            hizo = true;
                        }
                    }
                    if (!hizo)
                    {
                        foreach (CasaT h in dos)
                        {
                            if (Estados.TryGetValue(h.Raiz, out EstadoCasa e) && e.Dy != 0f)
                            {
                                DeshacerPiso(raiz, h, e);
                                cuenta.PisosDeshechos++;
                                cuenta.Pisos--;
                                hizo = true;
                            }
                        }
                    }
                    algo |= hizo;
                }
                if (!algo)
                {
                    log.AppendLine("AVISO: quedan " + pares.Count + " pares de cuerpos encimados que no vienen de esta tarea.");
                    return;
                }
            }
        }

        private static float Range(Random az, float a, float b) => a + (float)az.NextDouble() * (b - a);

        // ================================================================ 1. Pisos extra

        private static bool SubirPiso(Transform raiz, Transform detalles, CasaT c, List<CasaT> todas)
        {
            if (c.Bloqueada)
            {
                return false;
            }
            Transform pieza = null;
            float altoMax = float.MinValue;
            foreach (Transform t in c.Raiz)
            {
                if ((t.name.StartsWith("C03_Fachada") || t.name.StartsWith("C20b_PisoDosVentanas")) && Cubierta(t, out Bounds bp) && bp.max.y > altoMax)
                {
                    altoMax = bp.max.y;
                    pieza = t;
                }
            }
            if (pieza == null)
            {
                return false;
            }
            Transform visual = pieza.Find("Visual");
            Bounds bv;
            if (!Cubierta(visual != null ? visual : pieza, out bv))
            {
                return false;
            }
            float dy = bv.size.y;
            if (dy < 1.5f || dy > 4f)
            {
                return false;
            }
            float nuevoTop = c.Top + dy;
            bool rompe = false;
            Vecinas(c, todas, DistanciaVecina, (n, e) =>
            {
                if (Mathf.Abs(nuevoTop - n.Top) > DesnivelMaximo)
                {
                    rompe = true;
                }
            });
            if (rompe)
            {
                return false;
            }
            List<Transform> arriba = EnAzotea(c, c.Top);
            List<Transform> techo = EnDetalles(detalles, c);
            foreach (Transform t in arriba)
            {
                Mover(raiz, t, new Vector3(0f, dy, 0f));
            }
            foreach (Transform t in techo)
            {
                Mover(raiz, t, new Vector3(0f, dy, 0f));
            }
            float alto = c.Top - c.Base;
            Vector3 s0 = c.Cuerpo.localScale;
            float factor = (alto + dy) / alto;
            c.Cuerpo.localScale = new Vector3(s0.x, s0.y * factor, s0.z);
            Anotar(raiz, c.Cuerpo, Vector3.zero, new Vector3(1f, factor, 1f));
            Mover(raiz, c.Cuerpo, new Vector3(0f, dy * 0.5f, 0f));
            GameObject copia = Object.Instantiate(pieza.gameObject, c.Raiz);
            copia.name = Prefijo + "Piso_" + pieza.name;
            copia.transform.SetPositionAndRotation(pieza.position + Vector3.up * dy, pieza.rotation);
            copia.transform.localScale = pieza.localScale;
            c.Top = nuevoTop;
            EstadoCasa est = Estado(c.Raiz);
            est.Dy = dy;
            est.Factor = factor;
            est.Arriba = arriba;
            est.PisoTecho = techo;
            est.Copia = copia;
            return true;
        }

        // ================================================================ 1. Cuartos de azotea

        private static bool PonerCuarto(Transform raiz, Transform detalles, CasaT c, List<CasaT> todas, Random az, Cuenta cuenta)
        {
            bool[] bo = Bordes(c, todas);
            float mx0 = c.Normal < 0f ? MargenCalle : (bo[0] ? MargenVecina : MargenLibre);
            float mx1 = c.Normal > 0f ? MargenCalle : (bo[1] ? MargenVecina : MargenLibre);
            float mz0 = bo[2] ? MargenVecina : MargenLibre;
            float mz1 = bo[3] ? MargenVecina : MargenLibre;
            float anchoZ = c.Z1 - c.Z0 - mz0 - mz1;
            float w = Mathf.Min(anchoZ, 4.4f);
            float margenAtras = c.Normal > 0f ? mx0 : mx1;
            float area = (c.X1 - c.X0) * (c.Z1 - c.Z0);
            float d = (c.X1 - c.X0) - margenAtras - FranjaFrente;
            if (w < 2.2f || d < 2.5f)
            {
                return false;
            }
            float f = w * d / area;
            if (f < 0.38f)
            {
                return false;
            }
            if (f > 0.58f)
            {
                d = 0.56f * area / w;
                f = 0.56f;
            }
            string codigo = az.Next(2) == 0 ? "CC01_CasaCerro" : "CC02_CasaCerro";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CarpetaTripo + codigo + ".prefab");
            if (prefab == null)
            {
                return false;
            }
            // Medida del prefab sin escalar ni girar.
            GameObject medida = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            medida.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            bool tiene = Cubierta(medida.transform, out Bounds bm);
            Object.DestroyImmediate(medida);
            if (!tiene || bm.size.x < 0.1f || bm.size.y < 0.1f || bm.size.z < 0.1f)
            {
                return false;
            }
            float h = Range(az, 2.4f, 3.0f);
            float cx = c.Normal > 0f ? c.X1 - FranjaFrente - d * 0.5f : c.X0 + FranjaFrente + d * 0.5f;
            float cz = c.Z0 + mz0 + w * 0.5f + (az.Next(2) == 0 ? 0f : anchoZ - w);
            Rect cuarto = new Rect(cx - d * 0.5f, cz - w * 0.5f, d, w);

            // Lo que ya está en la azotea y estorba al cuarto se corre hacia el frente (queda anotado para deshacerlo).
            List<(Transform t, float dx)> mover = new List<(Transform, float)>();
            float limite = FranjaFrente - 0.15f;
            List<Transform> candidatos = EnAzotea(c, c.Top).Where(t => !t.name.StartsWith("C05_") && !t.name.StartsWith("AzoteaT")).ToList();
            candidatos.AddRange(EnDetalles(detalles, c));
            foreach (Transform t in candidatos)
            {
                if (!Cubierta(t, out Bounds b))
                {
                    continue;
                }
                Rect r = new Rect(b.min.x, b.min.z, b.size.x, b.size.z);
                if (!r.Overlaps(new Rect(cuarto.x - 0.3f, cuarto.y - 0.3f, cuarto.width + 0.6f, cuarto.height + 0.6f)))
                {
                    continue;
                }
                float extension = c.Normal > 0f ? c.X1 - b.min.x : b.max.x - c.X0;
                if (extension > limite)
                {
                    if (b.size.x > limite - 0.5f)
                    {
                        return false;
                    }
                    mover.Add((t, c.Normal * (extension - limite)));
                }
            }
            foreach ((Transform t, float dx) m in mover)
            {
                Mover(raiz, m.t, new Vector3(m.dx, 0f, 0f));
                cuenta.Reubicados++;
            }

            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, c.Raiz);
            go.name = Prefijo + "Cuarto_" + codigo;
            go.transform.rotation = Quaternion.LookRotation(new Vector3(c.Normal, 0f, 0f));
            // Escala en los ejes del modelo: x = ancho de la casa (z del mundo), z = fondo (x del mundo).
            go.transform.localScale = new Vector3(w / bm.size.x, h / bm.size.y, d / bm.size.z);
            go.transform.position = Vector3.zero;
            Cubierta(go.transform, out Bounds bg);
            go.transform.position += new Vector3(cx - bg.center.x, c.Top + 0.02f - bg.min.y, cz - bg.center.z);
            cuenta.Fracciones.Add(f);
            return true;
        }

        // ================================================================ 4. Detalle de azotea

        private static string ElegirTipo(Random az, Cuenta cuenta)
        {
            float total = 0f;
            for (int i = 0; i < TiposDetalle.Length; i++)
            {
                bool pesado = TiposDetalle[i] == "Tendedero" || TiposDetalle[i] == "Lavadero";
                total += pesado && cuenta.TendLav >= TopeTendederoLavadero ? 0f : PesosDetalle[i];
            }
            float x = (float)az.NextDouble() * total;
            for (int i = 0; i < TiposDetalle.Length; i++)
            {
                bool pesado = TiposDetalle[i] == "Tendedero" || TiposDetalle[i] == "Lavadero";
                float p = pesado && cuenta.TendLav >= TopeTendederoLavadero ? 0f : PesosDetalle[i];
                if (x < p)
                {
                    return TiposDetalle[i];
                }
                x -= p;
            }
            return TiposDetalle[0];
        }

        private static GameObject Pieza(Transform padre, string codigo, Vector3 local, Vector3 escala)
        {
            GameObject prefab = codigo.StartsWith("CC0") ? AssetDatabase.LoadAssetAtPath<GameObject>(CarpetaTripo + codigo + ".prefab") : KitS2c.Prefab(codigo);
            if (prefab == null)
            {
                return null;
            }
            GameObject g = (GameObject)PrefabUtility.InstantiatePrefab(prefab, padre);
            g.transform.localPosition = local;
            g.transform.localRotation = Quaternion.identity;
            g.transform.localScale = escala;
            return g;
        }

        /// <summary>Crea el detalle en el origen sin girar (para medirlo); devuelve null si falta un prefab.</summary>
        private static GameObject CrearDetalle(string tipo, Transform padre, Random az)
        {
            GameObject grupo = new GameObject(Prefijo + "Det_" + tipo);
            grupo.transform.SetParent(padre, false);
            grupo.transform.position = Vector3.zero;
            grupo.transform.rotation = Quaternion.identity;
            bool ok = true;
            switch (tipo)
            {
                case "Tinaco":
                    KitS2c.Medida mt = KitS2c.Medir(BaseTinaco);
                    ok = Pieza(grupo.transform, BaseTinaco, Vector3.zero, new Vector3(AnchoBaseTinaco / mt.Caja.size.x, AltoBaseTinaco / mt.Caja.size.y, 1f)) != null
                        && Pieza(grupo.transform, "C09_Tinaco", Vector3.up * 0.7f, Vector3.one * Range(az, 1.05f, 1.25f)) != null;
                    break;
                case "Aire":
                    ok = Pieza(grupo.transform, "C10_AireAcondicionado", Vector3.zero, Vector3.one) != null;
                    break;
                case "Antena":
                    ok = Pieza(grupo.transform, "CY04_AntenaAzotea", Vector3.zero, Vector3.one * 0.8f) != null;
                    break;
                case "Macetas":
                    ok = Pieza(grupo.transform, "C20d_Macetas", Vector3.zero, Vector3.one) != null;
                    break;
                case "Tendedero":
                    ok = Pieza(grupo.transform, "CC04_Tendedero", Vector3.zero, Vector3.one) != null;
                    break;
                default:
                    ok = Pieza(grupo.transform, "CC05_Lavadero", Vector3.zero, Vector3.one) != null;
                    break;
            }
            if (!ok || !Cubierta(grupo.transform, out Bounds b))
            {
                Object.DestroyImmediate(grupo);
                return null;
            }
            if (grupo.GetComponentsInChildren<Collider>(true).Length == 0 && Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z)) > 1f)
            {
                BoxCollider bc = grupo.AddComponent<BoxCollider>();
                bc.center = b.center;
                bc.size = b.size;
            }
            return grupo;
        }

        private static void PonerDetalles(Transform raiz, Transform detalles, List<CasaT> casas, Random az, Cuenta cuenta)
        {
            foreach (CasaT c in casas.Where(x => x.Kit).OrderBy(x => az.Next()))
            {
                if (cuenta.TriDetalle > TopeTriangulosDetalle)
                {
                    break;
                }
                bool[] bo = Bordes(c, casas);
                float mx0 = c.Normal < 0f ? MargenCalle : (bo[0] ? MargenVecina : MargenLibre);
                float mx1 = c.Normal > 0f ? MargenCalle : (bo[1] ? MargenVecina : MargenLibre);
                float mz0 = bo[2] ? MargenVecina : MargenLibre;
                float mz1 = bo[3] ? MargenVecina : MargenLibre;
                double r = az.NextDouble();
                int cantidad = r < 0.5 ? 1 : (r < 0.85 ? 2 : 3);
                for (int k = 0; k < cantidad; k++)
                {
                    List<Rect> ocupado = new List<Rect>();
                    foreach (Transform t in EnAzotea(c, c.Top, true).Concat(EnDetalles(detalles, c)))
                    {
                        if (t.name.StartsWith("AzoteaT") || !Cubierta(t, out Bounds bt))
                        {
                            continue;
                        }
                        ocupado.Add(new Rect(bt.min.x - 0.25f, bt.min.z - 0.25f, bt.size.x + 0.5f, bt.size.z + 0.5f));
                    }
                    string tipo = ElegirTipo(az, cuenta);
                    GameObject g = CrearDetalle(tipo, c.Raiz, az);
                    if (g == null)
                    {
                        continue;
                    }
                    bool alineado = tipo != "Tinaco" && tipo != "Antena";
                    Quaternion giro = alineado ? Quaternion.LookRotation(new Vector3(c.Normal, 0f, 0f)) * Quaternion.Euler(0f, az.Next(2) * 180f, 0f)
                        : Quaternion.Euler(0f, Range(az, 0f, 360f), 0f);
                    g.transform.rotation = giro;
                    Cubierta(g.transform, out Bounds b);
                    float hx = b.size.x * 0.5f, hz = b.size.z * 0.5f;
                    float loX = c.X0 + mx0 + hx, hiX = c.X1 - mx1 - hx, loZ = c.Z0 + mz0 + hz, hiZ = c.Z1 - mz1 - hz;
                    bool puesto = false;
                    if (loX <= hiX && loZ <= hiZ)
                    {
                        for (int intento = 0; intento < 14 && !puesto; intento++)
                        {
                            float px = Range(az, loX, hiX), pz = Range(az, loZ, hiZ);
                            Rect q = new Rect(px - hx, pz - hz, hx * 2f, hz * 2f);
                            if (!ocupado.Any(o => o.Overlaps(q)))
                            {
                                g.transform.position += new Vector3(px - b.center.x, c.Top + 0.02f - b.min.y, pz - b.center.z);
                                puesto = true;
                            }
                        }
                    }
                    if (!puesto)
                    {
                        Object.DestroyImmediate(g);
                        continue;
                    }
                    cuenta.Detalles++;
                    cuenta.PorTipo[tipo] = cuenta.PorTipo.TryGetValue(tipo, out int n) ? n + 1 : 1;
                    cuenta.TriDetalle += Triangulos(g);
                    if (tipo == "Tendedero" || tipo == "Lavadero")
                    {
                        cuenta.TendLav++;
                    }
                }
            }
        }

        // ================================================================ 5. Cables

        private static void PonerCables(Transform raiz, List<CasaT> casas, Random az, Cuenta cuenta)
        {
            Transform postes = raiz.Find("Postes");
            if (postes == null)
            {
                return;
            }
            KitS2c.Obra o = new KitS2c.Obra { Raiz = raiz, Azar = az, Nombre = "T1364_Cerro" };
            List<Vector3> puntas = new List<Vector3>();
            foreach (Transform p in postes)
            {
                Transform luz = p.Find("Luz");
                puntas.Add((luz != null ? luz.position : p.position + Vector3.up * 6f) + Vector3.down * 0.5f);
            }
            List<CasaT> kit = casas.Where(c => c.Kit).ToList();
            HashSet<long> pares = new HashSet<long>();
            for (int i = 0; i < puntas.Count; i++)
            {
                foreach (int j in Enumerable.Range(0, puntas.Count).Where(q => q != i && (puntas[q] - puntas[i]).sqrMagnitude < 45f * 45f)
                    .OrderBy(q => (puntas[q] - puntas[i]).sqrMagnitude).Take(2))
                {
                    long clave = Math.Min(i, j) * 1000L + Math.Max(i, j);
                    if (!pares.Add(clave))
                    {
                        continue;
                    }
                    foreach (float baja in new[] { 0.7f, 0.95f })
                    {
                        KitS2c.Cable(o, puntas[i] + Vector3.down * baja, puntas[j] + Vector3.down * baja, Range(az, 0.6f, 1f));
                    }
                }
                // A fachadas: las casas del rango 4 y 5 por cercanía (el generador ya colgó las 3 más cercanas).
                foreach (CasaT c in kit.Where(x => (new Vector3(x.Frente, puntas[i].y, (x.Z0 + x.Z1) * 0.5f) - puntas[i]).sqrMagnitude < 22f * 22f)
                    .OrderBy(x => (new Vector3(x.Frente, 0f, (x.Z0 + x.Z1) * 0.5f) - new Vector3(puntas[i].x, 0f, puntas[i].z)).sqrMagnitude).Skip(3).Take(2))
                {
                    KitS2c.Cable(o, puntas[i] + Vector3.down * 0.55f, new Vector3(c.Frente + c.Normal * 0.15f, c.Top - 0.4f, (c.Z0 + c.Z1) * 0.5f), Range(az, 0.4f, 0.9f), 0.02f);
                }
            }
            // Cables de una azotea a la de enfrente, cruzando la calle (como en K13).
            int cruces = 0;
            foreach (CasaT c in kit.OrderBy(x => az.Next()))
            {
                if (cruces >= 60)
                {
                    break;
                }
                CasaT otra = kit.Where(b => b.Normal == -c.Normal && Mathf.Abs(b.Frente - c.Frente) > 7f && Mathf.Abs(b.Frente - c.Frente) < 20f
                    && Mathf.Abs((b.Z0 + b.Z1 - c.Z0 - c.Z1) * 0.5f) < 6f && (b.Frente - c.Frente) * c.Normal > 0f).OrderBy(b => az.Next()).FirstOrDefault();
                if (otra == null)
                {
                    continue;
                }
                KitS2c.Cable(o, new Vector3(c.Frente + c.Normal * 0.15f, c.Top - 0.3f, (c.Z0 + c.Z1) * 0.5f),
                    new Vector3(otra.Frente + otra.Normal * 0.15f, otra.Top - 0.3f, (otra.Z0 + otra.Z1) * 0.5f), Range(az, 0.8f, 1.4f), 0.02f);
                cruces++;
            }
            cuenta.Cables = o.Cables;
            if (o.CablesV.Count == 0)
            {
                return;
            }
            Directory.CreateDirectory(Path.GetDirectoryName(RutaMallas));
            Mesh m = AssetDatabase.LoadAssetAtPath<Mesh>(RutaMallas);
            bool nueva = m == null;
            if (nueva)
            {
                m = new Mesh { name = "T1364_Cerro_Cables" };
            }
            m.Clear();
            m.indexFormat = IndexFormat.UInt32;
            m.SetVertices(o.CablesV);
            m.SetNormals(o.CablesN);
            m.SetTriangles(o.CablesI, 0);
            m.RecalculateBounds();
            if (nueva)
            {
                AssetDatabase.CreateAsset(m, RutaMallas);
            }
            else
            {
                EditorUtility.SetDirty(m);
            }
            Transform viejos = raiz.Find("Cables");
            Material mat = viejos != null && viejos.GetComponent<MeshRenderer>() != null ? viejos.GetComponent<MeshRenderer>().sharedMaterial
                : KitS2c.Mat("S2c_Cable", () => KitS2c.Lit(new Color(0.05f, 0.05f, 0.06f), 0.35f, KitS2c.TexMuro()));
            GameObject go = new GameObject(Prefijo + "Cables");
            go.transform.SetParent(raiz, false);
            go.AddComponent<MeshFilter>().sharedMesh = m;
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
        }

        // ================================================================ Informe

        private static void Escribir(Transform raiz, List<CasaT> casas, Cuenta cuenta, StringBuilder log)
        {
            long triExtra = 0;
            int objetos = 0;
            foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true))
            {
                if (t != raiz && t.name.StartsWith(Prefijo) && !t.parent.name.StartsWith(Prefijo) && !t.name.StartsWith("Extra_Mov"))
                {
                    triExtra += Triangulos(t.gameObject);
                    objetos++;
                }
            }
            int conExtra = casas.Count(c => c.Raiz.Cast<Transform>().Any(t => t.name.StartsWith(Prefijo + "Cuarto") || t.name.StartsWith(Prefijo + "Piso")));
            log.AppendLine("1. Cuartos de azotea: " + cuenta.Cuartos + " (fracción de azotea media " + (cuenta.Fracciones.Count == 0 ? 0f : cuenta.Fracciones.Average()).ToString("F2")
                + ", mín " + (cuenta.Fracciones.Count == 0 ? 0f : cuenta.Fracciones.Min()).ToString("F2") + "), cosas reubicadas para dejarles lugar: " + cuenta.Reubicados);
            log.AppendLine("1. Pisos extra: " + cuenta.Pisos);
            log.AppendLine("   Casas con cuarto o piso extra: " + conExtra + " de " + casas.Count + " (" + (100f * conExtra / Mathf.Max(1, casas.Count)).ToString("F1") + " %)");
            log.AppendLine("2. Casas escalonadas: " + cuenta.Escalonadas);
            log.AppendLine("   Deshechos por cuerpos encimados: " + cuenta.EscalonesDeshechos + " escalonados, " + cuenta.PisosDeshechos + " pisos extra, " + cuenta.CuartosDeshechos + " cuartos");
            log.AppendLine("3. Casas reteñidas: " + cuenta.Tinte + " con " + Paleta.Length + " colores; materiales nuevos: " + (Paleta.Length * 2) + " en " + CarpetaMateriales);
            log.AppendLine("4. Detalles de azotea agregados: " + cuenta.Detalles + " (" + string.Join(", ", cuenta.PorTipo.Select(p => p.Key + " " + p.Value)) + "), triángulos de detalles " + cuenta.TriDetalle);
            log.AppendLine("5. Cables nuevos: " + cuenta.Cables);
            log.AppendLine("Objetos Extra_ de primer nivel: " + objetos + ", triángulos agregados: " + triExtra);
        }

        // ================================================================ Fotos

        public static void FotosAntes() => Fotos("antes");

        public static void FotosDespues() => Fotos("despues");

        public static void Fotos(string sufijo)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab);
            GameObject zona = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            GameObject sol = new GameObject("LuzNoche");
            Light luz = sol.AddComponent<Light>();
            luz.type = LightType.Directional;
            luz.color = new Color(0.45f, 0.55f, 0.95f);
            luz.intensity = 0.25f;
            sol.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.1f, 0.14f, 0.28f);
            foreach (Light l in zona.GetComponentsInChildren<Light>(true))
            {
                l.enabled = true;
            }
            Physics.SyncTransforms();
            Directory.CreateDirectory(CarpetaFotos);
            ColocarS2.Foto(ColocarT1355Cerro.K13Desde, ColocarT1355Cerro.K13Hacia, CarpetaFotos + "t1364-k13-" + sufijo + ".png", 66f, 600f);
            ColocarS2.Foto(ColocarT1355Cerro.A13Desde, ColocarT1355Cerro.A13Hacia, CarpetaFotos + "t1364-a13-" + sufijo + ".png", 66f, 600f);
            (float x, float z, Vector3 mira)[] calles =
            {
                (-358.5f, 110f, new Vector3(-360f, 18f, 40f)),
                (-308f, -100f, new Vector3(-306f, 6f, 20f)),
                (-358.5f, 70f, new Vector3(-376f, 20f, 70f)),
            };
            for (int i = 0; i < calles.Length; i++)
            {
                float y = KitS2c.Suelo(new Vector3(calles[i].x, 0f, calles[i].z), 0f) + 1.7f;
                ColocarS2.Foto(new Vector3(calles[i].x, y, calles[i].z), calles[i].mira, CarpetaFotos + "t1364-calle" + (i + 1) + "-" + sufijo + ".png", 70f, 600f);
            }
            Debug.Log("[Arte] T1364: fotos " + sufijo + " listas en " + CarpetaFotos);
        }
    }
}
