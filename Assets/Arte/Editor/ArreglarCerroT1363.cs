using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MechaCyberpunk.Core.Mundo;
using MechaCyberpunk.Core.Trepar;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-1363: arregla en el prefab T1355_Cerro los bugs que encontraron el barrido (T-1360) y los recorridos (T-1361): cimientos sin collider,
    /// carros en el carril, escaleras largas que no llegan a la calle, accesos a azoteas que no llegan, azoteas aisladas, saltos de parkour bloqueados
    /// y postes con un muro invisible. Todo se mide con rayos sobre una instancia del prefab en una escena vacía y se guarda en el prefab.
    /// Idempotente. Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ArreglarCerroT1363.FotosAntes (antes de arreglar) y
    /// MechaCyberpunk.Arte.Editor.ArreglarCerroT1363.Aplicar (arregla, verifica y toma las fotos de después). Informe: Logs/t1363-arreglos.txt.
    /// </summary>
    public static class ArreglarCerroT1363
    {
        public const string RutaPrefab = "Assets/Arte/Mundo/Zonas/T1355_Cerro.prefab";
        public const string RutaInforme = "Logs/t1363-arreglos.txt";
        public const string RutaBarridoAntes = "Logs/t1360-barrido-Cerro-antes.json";
        public const string CarpetaFotos = "docs/capturas/bugs-18nov-B";

        /// <summary>Del borde de la calle (m): el carro estacionado deja este margen entre su costado y la banqueta.</summary>
        public const float MargenCarroBanqueta = 1.25f;
        public const float SeparacionPosteCarro = 0.5f;
        public const float DeslizeMaximoCarro = 12f;
        public const float AnchoMinimoPoste = 0.3f;
        public const float AnchoMaximoPoste = 1f;
        public const float SeparacionBordeProp = 1f;
        public const float HuecoVecina = 2f;
        public const float HuecoPegada = 0.6f;
        public const float DesnivelPretil = 0.6f;
        public const float AlcanceCalleAcceso = 12f;
        public const float AlturaMaximaAcceso = 12f;
        public const float SalidaAcceso = 0.8f;
        public const float SalidaMinimaTecho = 0.35f;
        public const float HolguraRampa = 0.02f;
        public const float ExtensionRampa = 1.35f;
        public const float DistanciaCarroEscalera = 6f;
        public const float AlcanceLateralEscalera = 15f;
        public const float SaltoMaximoRampa = 0.25f;
        public const float DesnivelSaltable = 2.3f;
        public const float PasoLibreBorde = 2.5f;
        public const float AlturaPasoLibre = 1.8f;

        private const float RadioCapsula = 0.35f;
        private const float PasoAccesoPeldano = 0.5f;
        private const float PrimerPeldano = 0.4f;
        private const float RielLateral = 0.25f;
        private const float RielAtras = 0.1f;

        private static StringBuilder informe;
        private static readonly RaycastHit[] bufRayos = new RaycastHit[1024];
        private static readonly Collider[] bufColliders = new Collider[512];

        private sealed class Ctx
        {
            public GameObject Raiz;
            public PhysicsScene Fisica;
            public bool Contenidos;
            public Transform Terreno;
            public Transform Calles;
            public Transform Casas;
            public Transform Escaleras;
            public Transform Postes;
            public Transform Carros;
            public Transform Detalles;
            public readonly List<CalleZona> Zonas = new List<CalleZona>();
            public readonly Dictionary<CalleZona, List<Vector3>> Puntos = new Dictionary<CalleZona, List<Vector3>>();
        }

        private sealed class Techo
        {
            public Transform Raiz;
            public Bounds B;
            public bool Kit;
            public float Y => B.max.y;
        }

        private struct Par
        {
            public Techo A;
            public Techo B;
            public int Eje;
            public float Signo;
            public float Hueco;
            public float PerpMin;
            public float PerpMax;
        }

        private sealed class Movible
        {
            public Transform[] Partes;
            public Techo Casa;
        }

        private struct Foto
        {
            public string Arreglo;
            public int N;
            public Vector3 Objetivo;
            public Vector3 Desplazamiento;
        }

        private static readonly Foto[] Fotos =
        {
            new Foto { Arreglo = "carros", N = 1, Objetivo = new Vector3(-360f, 12.8f, 139.2f), Desplazamiento = new Vector3(0f, 7f, -10f) },
            new Foto { Arreglo = "carros", N = 2, Objetivo = new Vector3(-305f, 1f, 118f), Desplazamiento = new Vector3(0f, 7f, -10f) },
            new Foto { Arreglo = "escaleras", N = 1, Objetivo = new Vector3(-353.5f, 12.9f, 120f), Desplazamiento = new Vector3(-7f, 4f, 0f) },
            new Foto { Arreglo = "escaleras", N = 2, Objetivo = new Vector3(-317.5f, 0.2f, 120f), Desplazamiento = new Vector3(9f, 4f, 0f) },
            new Foto { Arreglo = "accesos", N = 1, Objetivo = new Vector3(-366.5f, 20f, 59.9f), Desplazamiento = new Vector3(7f, 3f, 3f) },
            new Foto { Arreglo = "azoteas", N = 1, Objetivo = new Vector3(-329.3f, 11f, 128.2f), Desplazamiento = new Vector3(7f, 5f, -3f) },
            new Foto { Arreglo = "azoteas", N = 2, Objetivo = new Vector3(-424.3f, 34.4f, 0f), Desplazamiento = new Vector3(8f, 6f, -4f) },
            new Foto { Arreglo = "saltos", N = 1, Objetivo = new Vector3(-349f, 21f, -20f), Desplazamiento = new Vector3(0f, 6f, -9f) },
            new Foto { Arreglo = "saltos", N = 2, Objetivo = new Vector3(-340f, 20.5f, -17.5f), Desplazamiento = new Vector3(0f, 6f, -9f) },
            new Foto { Arreglo = "postes", N = 1, Objetivo = new Vector3(-310.2f, 4f, 118f), Desplazamiento = new Vector3(0f, 5f, -9f) },
            new Foto { Arreglo = "postes", N = 2, Objetivo = new Vector3(-365.2f, 13f, 165f), Desplazamiento = new Vector3(0f, 4f, -10f) },
        };

        // ================================================================ puntos de entrada

        /// <summary>Batch: fotos "antes" de los puntos de cada arreglo (no toca el prefab).</summary>
        public static void FotosAntes()
        {
            Ctx c = Abrir(false);
            ArteT250.PonerLuz();
            TomarFotos("antes");
            Object.DestroyImmediate(c.Raiz);
        }

        /// <summary>Batch: fotos "despues" de los mismos puntos sobre el prefab ya arreglado.</summary>
        public static void FotosDespues()
        {
            Ctx c = Abrir(false);
            ArteT250.PonerLuz();
            TomarFotos("despues");
            Object.DestroyImmediate(c.Raiz);
        }

        /// <summary>Batch: aplica todos los arreglos al prefab, verifica con el barrido y toma las fotos de después.</summary>
        public static void Aplicar()
        {
            informe = new StringBuilder();
            Log("T-1363: arreglos del Cerro (" + RutaPrefab + ")");
            Ctx c = Abrir(true);
            try
            {
                ArreglarCimientos(c);
                ArreglarPostes(c);
                ArreglarCarros(c);
                ArreglarEscaleras(c);
                ArreglarAccesos(c);
                ArreglarSaltos(c);
                AislarAzoteas(c);
                Physics.SyncTransforms();
                bool ok;
                GameObject guardado = PrefabUtility.SaveAsPrefabAsset(c.Raiz, RutaPrefab, out ok);
                Log(ok && guardado != null ? "Prefab guardado." : "ERROR: no se pudo guardar el prefab.");
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            catch (Exception e)
            {
                Log("ERROR: " + e);
                Debug.LogError("[ArreglarCerroT1363] " + e);
            }
            finally
            {
                if (c.Raiz != null)
                {
                    PrefabUtility.UnloadPrefabContents(c.Raiz);
                }
            }
            Verificar();
            Escribir();
        }

        // ================================================================ contexto

        private static Ctx Abrir(bool contenidos)
        {
            GameObject inst;
            if (contenidos)
            {
                // Los mismos objetos del prefab (sus fileID no cambian): se ajustan, no se recrean.
                inst = PrefabUtility.LoadPrefabContents(RutaPrefab);
            }
            else
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab);
                if (prefab == null)
                {
                    throw new InvalidOperationException("Falta " + RutaPrefab);
                }
                inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            }
            Physics.SyncTransforms();
            Ctx c = new Ctx
            {
                Raiz = inst,
                Fisica = inst.scene.GetPhysicsScene(),
                Contenidos = contenidos,
                Terreno = inst.transform.Find("Terreno"),
                Calles = inst.transform.Find("Calles"),
                Casas = inst.transform.Find("Casas"),
                Escaleras = inst.transform.Find("Escaleras"),
                Postes = inst.transform.Find("Postes"),
                Carros = inst.transform.Find("Carros"),
                Detalles = inst.transform.Find("Detalles"),
            };
            foreach (CalleZona cz in inst.GetComponentsInChildren<CalleZona>(false))
            {
                c.Zonas.Add(cz);
                c.Puntos[cz] = cz.Puntos();
            }
            return c;
        }

        private static void Log(string texto)
        {
            if (informe != null)
            {
                informe.AppendLine(texto);
            }
            Debug.Log("[ArreglarCerroT1363] " + texto);
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

        private static string Ruta(Transform t, Transform raiz)
        {
            StringBuilder sb = new StringBuilder(t.name);
            for (Transform u = t.parent; u != null && u != raiz; u = u.parent)
            {
                sb.Insert(0, u.name + "/");
            }
            return sb.ToString();
        }

        private static float Eje(Vector3 v, int eje)
        {
            return eje == 0 ? v.x : v.z;
        }

        private static Bounds? Limites(params Transform[] ts)
        {
            Bounds? b = null;
            foreach (Transform t in ts)
            {
                foreach (Renderer r in t.GetComponentsInChildren<Renderer>(false))
                {
                    if (r.name.StartsWith("Halo", StringComparison.Ordinal) || r.name.StartsWith("Luz", StringComparison.Ordinal))
                    {
                        continue;
                    }
                    if (b.HasValue)
                    {
                        Bounds u = b.Value;
                        u.Encapsulate(r.bounds);
                        b = u;
                    }
                    else
                    {
                        b = r.bounds;
                    }
                }
            }
            return b;
        }

        private static RaycastHit[] Rayos(Ctx c, Vector3 origen, float distancia)
        {
            int n = c.Fisica.Raycast(origen, Vector3.down, bufRayos, distancia, ~0, QueryTriggerInteraction.Ignore);
            RaycastHit[] r = new RaycastHit[n];
            Array.Copy(bufRayos, r, n);
            return r;
        }

        /// <summary>Piso de la calle o la ladera (arroyo, banquetas, terreno) en x, z: el más alto bajo <paramref name="yDesde"/>.</summary>
        private static bool Piso(Ctx c, float x, float z, float yDesde, out float y)
        {
            y = float.MinValue;
            bool ok = false;
            foreach (RaycastHit h in Rayos(c, new Vector3(x, yDesde, z), 400f))
            {
                Transform t = h.collider.transform;
                if ((t.IsChildOf(c.Terreno) || t.IsChildOf(c.Calles)) && h.point.y > y)
                {
                    y = h.point.y;
                    ok = true;
                }
            }
            return ok;
        }

        /// <summary>Techo (cara de arriba de algún collider de las casas) en x, z: el más alto bajo <paramref name="yDesde"/>.</summary>
        private static bool Techo_(Ctx c, float x, float z, float yDesde, out float y)
        {
            y = float.MinValue;
            bool ok = false;
            foreach (RaycastHit h in Rayos(c, new Vector3(x, yDesde, z), 200f))
            {
                if (h.collider.transform.IsChildOf(c.Casas) && h.normal.y > 0.5f && h.point.y > y)
                {
                    y = h.point.y;
                    ok = true;
                }
            }
            return ok;
        }

        /// <summary>Distancia en planta de q al eje (punto más cercano y tangente en planta).</summary>
        private static float Proyectar(IList<Vector3> pts, Vector3 q, out Vector3 punto, out Vector3 tangente)
        {
            float mejor = float.MaxValue;
            punto = default;
            tangente = Vector3.forward;
            Vector2 p = new Vector2(q.x, q.z);
            for (int i = 0; i + 1 < pts.Count; i++)
            {
                Vector2 a = new Vector2(pts[i].x, pts[i].z);
                Vector2 b = new Vector2(pts[i + 1].x, pts[i + 1].z);
                Vector2 ab = b - a;
                float l2 = ab.sqrMagnitude;
                if (l2 < 0.000001f)
                {
                    continue;
                }
                float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / l2);
                Vector2 f = a + ab * t;
                float d = (p - f).magnitude;
                if (d < mejor)
                {
                    mejor = d;
                    punto = new Vector3(f.x, Mathf.Lerp(pts[i].y, pts[i + 1].y, t), f.y);
                    Vector2 tn = ab.normalized;
                    tangente = new Vector3(tn.x, 0f, tn.y);
                }
            }
            return mejor;
        }

        private static CalleZona CalleMasCercana(Ctx c, Vector3 q, out float distancia, out Vector3 punto, out Vector3 tangente)
        {
            CalleZona mejor = null;
            distancia = float.MaxValue;
            punto = default;
            tangente = Vector3.forward;
            foreach (CalleZona cz in c.Zonas)
            {
                float d = Proyectar(c.Puntos[cz], q, out Vector3 p, out Vector3 t);
                if (d < distancia)
                {
                    distancia = d;
                    mejor = cz;
                    punto = p;
                    tangente = t;
                }
            }
            return mejor;
        }

        // ================================================================ 1. cimientos sin collider

        private static void ArreglarCimientos(Ctx c)
        {
            int n = 0;
            foreach (Transform t in c.Casas)
            {
                if (!t.name.StartsWith("Cimiento", StringComparison.Ordinal) || t.GetComponent<Collider>() != null)
                {
                    continue;
                }
                MeshFilter mf = t.GetComponent<MeshFilter>();
                if (mf == null || mf.sharedMesh == null)
                {
                    continue;
                }
                BoxCollider caja = t.gameObject.AddComponent<BoxCollider>();
                caja.center = mf.sharedMesh.bounds.center;
                caja.size = mf.sharedMesh.bounds.size;
                n++;
            }
            Log("1. Cimientos: " + n + " BoxCollider agregados (antes sin collider; el collider es el de los limites de su malla).");
        }

        // ================================================================ 7. postes

        private static void ArreglarPostes(Ctx c)
        {
            int n = 0;
            int movidos = 0;
            foreach (Transform p in c.Postes)
            {
                if (!p.name.StartsWith("Poste_", StringComparison.Ordinal))
                {
                    continue;
                }
                Transform vis = p.Find("Visual");
                Renderer r = vis != null ? vis.GetComponent<Renderer>() : null;
                BoxCollider caja = p.GetComponent<BoxCollider>();
                if (r == null)
                {
                    continue;
                }
                if (caja == null)
                {
                    caja = p.gameObject.AddComponent<BoxCollider>();
                }
                Vector3 antes = caja.bounds.size;
                Bounds b = r.bounds;
                Vector2 centro = new Vector2(p.position.x, p.position.z);
                float ancho = AnchoMinimoPoste;
                MeshFilter mf = vis.GetComponent<MeshFilter>();
                if (mf != null && mf.sharedMesh != null)
                {
                    bool hay = false;
                    Bounds pie = default;
                    foreach (Vector3 v in mf.sharedMesh.vertices)
                    {
                        Vector3 w = vis.TransformPoint(v);
                        if (w.y > b.min.y + 0.3f)
                        {
                            continue;
                        }
                        if (!hay)
                        {
                            pie = new Bounds(w, Vector3.zero);
                            hay = true;
                        }
                        else
                        {
                            pie.Encapsulate(w);
                        }
                    }
                    if (hay)
                    {
                        centro = new Vector2(pie.center.x, pie.center.z);
                        ancho = Mathf.Clamp(Mathf.Max(pie.size.x, pie.size.z) + 0.1f, AnchoMinimoPoste, AnchoMaximoPoste);
                    }
                }
                Vector3 mundoCentro = new Vector3(centro.x, b.min.y + b.size.y * 0.5f, centro.y);
                Vector3 escala = p.lossyScale;
                caja.center = p.InverseTransformPoint(mundoCentro);
                caja.size = new Vector3(ancho / Mathf.Max(0.0001f, Mathf.Abs(escala.x)), b.size.y / Mathf.Max(0.0001f, Mathf.Abs(escala.y)),
                    ancho / Mathf.Max(0.0001f, Mathf.Abs(escala.z)));
                n++;
                // Que no quede en el arroyo: el eje de la calle más cercana más medio ancho de calle.
                CalleZona cz = CalleMasCercana(c, p.position, out float distancia, out Vector3 punto, out Vector3 tangente);
                if (cz != null && distancia < cz.Ancho * 0.5f + 0.2f)
                {
                    Vector3 fuera = new Vector3(p.position.x - punto.x, 0f, p.position.z - punto.z);
                    fuera = fuera.sqrMagnitude < 0.0001f ? new Vector3(-tangente.z, 0f, tangente.x) : fuera.normalized;
                    Vector3 nueva = new Vector3(punto.x, p.position.y, punto.z) + fuera * (cz.Ancho * 0.5f + 0.7f);
                    Log("7. " + p.name + ": estaba a " + F(distancia) + " m del eje de " + cz.name + " (arroyo); se pasa a " + V(nueva));
                    p.position = nueva;
                    movidos++;
                }
                if (n <= 3)
                {
                    Log("7. " + p.name + ": collider " + V(antes) + " -> " + V(caja.bounds.size));
                }
            }
            Physics.SyncTransforms();
            Log("7. Postes: " + n + " con el collider del tamano del poste (sin el halo); " + movidos + " movidos fuera del arroyo.");
        }

        // ================================================================ 2. carros

        private static void ArreglarCarros(Ctx c)
        {
            List<Vector3> troncos = new List<Vector3>();
            foreach (Transform p in c.Postes)
            {
                Collider col = p.GetComponent<Collider>();
                if (p.name.StartsWith("Poste_", StringComparison.Ordinal) && col != null)
                {
                    troncos.Add(col.bounds.center);
                }
            }
            // Puntos que deben quedar libres a lo largo de la calle: extremos de cada escalera larga y cada Acceso_*.
            List<Vector3> libres = new List<Vector3>();
            foreach (IGrouping<int, Transform> cadena in c.Escaleras.Cast<Transform>().Where(m => m.name == "CE01_EscaleraCerro").GroupBy(m => Mathf.RoundToInt(m.position.z)))
            {
                float xMin = cadena.Min(m => m.position.x) - 2f;
                float xMax = cadena.Max(m => m.position.x) + 2f;
                libres.Add(new Vector3(xMin, 0f, cadena.Key));
                libres.Add(new Vector3(xMax, 0f, cadena.Key));
            }
            foreach (Transform a in c.Detalles)
            {
                if (a.name.StartsWith("Acceso_", StringComparison.Ordinal))
                {
                    libres.Add(a.position);
                }
            }
            foreach (Transform car in c.Carros)
            {
                if (!car.name.StartsWith("CarroViejo", StringComparison.Ordinal))
                {
                    continue;
                }
                CalleZona cz = CalleMasCercana(c, car.position, out float distanciaAntes, out Vector3 p, out Vector3 t);
                if (cz == null)
                {
                    continue;
                }
                Vector3 izq = new Vector3(-t.z, 0f, t.x);
                float lado = Vector3.Dot(car.position - p, izq) >= 0f ? 1f : -1f;
                Vector3 antes = car.position;
                float giroAntes = car.eulerAngles.y;
                BoxCollider bc = car.GetComponent<BoxCollider>();
                Vector3 tam = bc != null ? Vector3.Scale(bc.size, car.lossyScale) : Vector3.one * 2f;
                tam = new Vector3(Mathf.Abs(tam.x), Mathf.Abs(tam.y), Mathf.Abs(tam.z));
                bool largoEnX = tam.x >= tam.z;
                float medioLargo = Mathf.Max(tam.x, tam.z) * 0.5f;
                float medioAncho = Mathf.Min(tam.x, tam.z) * 0.5f;
                float lateral = Mathf.Min(cz.Ancho * 0.5f - MargenCarroBanqueta, cz.Ancho * 0.5f - medioAncho - 0.05f);
                Vector3 avance = Vector3.Dot(car.forward, t) >= 0f ? t : -t;
                if (largoEnX)
                {
                    avance = Vector3.Dot(car.right, t) >= 0f ? t : -t;
                }

                // Desliza el carro a lo largo de la calle hasta que no choque con un poste y quede en tramo recto.
                float elegido = float.NaN;
                for (float paso = 0f; paso <= DeslizeMaximoCarro && float.IsNaN(elegido); paso += 0.5f)
                {
                    foreach (float s in new[] { paso, -paso })
                    {
                        Vector3 centro = p + t * s + izq * (lado * lateral);
                        if (!TramoRecto(c.Puntos[cz], centro, t, lateral, medioLargo))
                        {
                            continue;
                        }
                        bool choca = false;
                        foreach (Vector3 libre in libres)
                        {
                            Vector3 rl = libre - centro;
                            if (Mathf.Abs(Vector3.Dot(rl, t)) < DistanciaCarroEscalera && Mathf.Abs(Vector3.Dot(rl, izq)) < AlcanceLateralEscalera)
                            {
                                choca = true;
                                break;
                            }
                        }
                        foreach (Vector3 poste in troncos)
                        {
                            if (choca)
                            {
                                break;
                            }
                            Vector3 rel = poste - centro;
                            if (Mathf.Abs(Vector3.Dot(rel, t)) < medioLargo + SeparacionPosteCarro && Mathf.Abs(Vector3.Dot(rel, izq)) < medioAncho + SeparacionPosteCarro)
                            {
                                choca = true;
                                break;
                            }
                        }
                        if (!choca)
                        {
                            elegido = s;
                            break;
                        }
                    }
                }
                if (float.IsNaN(elegido))
                {
                    Log("2. " + car.name + ": no se encontro sitio sin postes a " + F(DeslizeMaximoCarro) + " m; se deja en el punto base.");
                    elegido = 0f;
                }
                Vector3 pos = p + t * elegido + izq * (lado * lateral);
                float yDelante = pos.y, yAtras = pos.y;
                bool tieneDelante = Piso(c, pos.x + avance.x * medioLargo * 0.85f, pos.z + avance.z * medioLargo * 0.85f, antes.y + 3f, out yDelante);
                bool tieneAtras = Piso(c, pos.x - avance.x * medioLargo * 0.85f, pos.z - avance.z * medioLargo * 0.85f, antes.y + 3f, out yAtras);
                if (!tieneDelante || !tieneAtras)
                {
                    Log("2. " + car.name + ": sin piso medido (delante " + tieneDelante + ", atras " + tieneAtras + "); se usa la altura del eje.");
                    yDelante = yAtras = p.y;
                }
                Vector3 dir = avance * (medioLargo * 1.7f) + Vector3.up * (yDelante - yAtras);
                Quaternion giro = Quaternion.LookRotation(dir, Vector3.up);
                if (largoEnX)
                {
                    giro *= Quaternion.Euler(0f, -90f, 0f);
                }
                car.SetPositionAndRotation(new Vector3(pos.x, (yDelante + yAtras) * 0.5f + 0.05f, pos.z), giro);
                Log("2. " + car.name + ": " + V(antes) + " (a " + F(distanciaAntes) + " m del eje, giro " + F(giroAntes) + ") -> " + V(car.position)
                    + " (a " + F(lateral) + " m del eje de " + cz.name + ", paralelo a la calle, desliza " + F(elegido) + " m, pendiente "
                    + F((yDelante - yAtras) / (medioLargo * 1.7f) * 100f) + " %)");
            }
            Physics.SyncTransforms();
        }

        /// <summary>El tramo de calle alrededor de centro (medioLargo a cada lado) es recto y centro queda a lateral del eje.</summary>
        private static bool TramoRecto(IList<Vector3> pts, Vector3 centro, Vector3 tangente, float lateral, float medioLargo)
        {
            foreach (float s in new[] { -medioLargo, 0f, medioLargo })
            {
                Vector3 q = centro + tangente * s;
                float d = Proyectar(pts, q, out Vector3 punto, out Vector3 t);
                if (Mathf.Abs(d - lateral) > 0.1f || Vector3.Dot(t, tangente) < 0.999f)
                {
                    return false;
                }
            }
            return true;
        }

        // ================================================================ 3. escaleras largas

        private static void ArreglarEscaleras(Ctx c)
        {
            GameObject prefabModulo = AssetDatabase.LoadAssetAtPath<GameObject>(CorrerZonasT1350.RutaModulo("CE01_EscaleraCerro"));
            BoxCollider rampaPrefab = prefabModulo != null ? prefabModulo.GetComponentInChildren<BoxCollider>(true) : null;
            if (rampaPrefab == null)
            {
                Log("3. ERROR: el modulo CE01_EscaleraCerro no tiene Rampa.");
                return;
            }
            float angulo = Mathf.Abs(Normalizar(rampaPrefab.transform.localEulerAngles.x));
            float largoModulo = rampaPrefab.size.z * Mathf.Cos(angulo * Mathf.Deg2Rad);
            float subidaLocal = rampaPrefab.size.z * Mathf.Sin(angulo * Mathf.Deg2Rad);
            Log("3. Modulo CE01: largo " + F(largoModulo) + " m, subida local " + F(subidaLocal) + " m (la rampa del modelo va a " + F(angulo) + " grados).");

            List<Transform> modulos = new List<Transform>();
            List<Transform> rieles = new List<Transform>();
            foreach (Transform t in c.Escaleras)
            {
                if (t.name == "CE01_EscaleraCerro")
                {
                    modulos.Add(t);
                }
                else if (t.name == "CE03_BarandalTuboEscalera")
                {
                    rieles.Add(t);
                }
            }
            foreach (IGrouping<int, Transform> cadena in modulos.GroupBy(m => Mathf.RoundToInt(m.position.z)).OrderByDescending(g => g.Key))
            {
                List<Transform> orden = cadena.OrderBy(m => m.position.x).ToList();
                int n = orden.Count;
                float z = cadena.Key;
                float xArriba = orden[0].position.x - largoModulo * 0.5f;
                float xAbajo = orden[n - 1].position.x + largoModulo * 0.5f;
                if (!Piso(c, xArriba - 0.6f, z, orden[0].position.y + 8f, out float hArriba) || !Piso(c, xAbajo + 0.6f, z, orden[n - 1].position.y + 8f, out float hAbajo))
                {
                    Log("3. Cadena z=" + F(z) + ": no se midio el piso de un extremo; se salta.");
                    continue;
                }
                float subidaModulo = (hArriba - hAbajo) / n;
                float ys = subidaModulo / subidaLocal;
                Log("3. Cadena z=" + F(z) + ": " + n + " modulos de x=" + F(xArriba) + " a " + F(xAbajo) + "; calle arriba y=" + F(hArriba) + ", abajo y=" + F(hAbajo)
                    + "; escala y " + F(ys));
                for (int k = 0; k < n; k++)
                {
                    Transform m = orden[k];
                    float yFin = Mathf.Lerp(hArriba, hAbajo, (k + 1f) / n);
                    Vector3 antes = m.position;
                    float yAntes = m.localScale.y;
                    m.position = new Vector3(antes.x, yFin, antes.z);
                    m.localScale = new Vector3(m.localScale.x, ys, m.localScale.z);
                    if (k == 0 || k == n - 1)
                    {
                        Log("3.   modulo " + k + ": y " + F(antes.y) + " -> " + F(yFin) + ", escala y " + F(yAntes) + " -> " + F(ys));
                    }
                }
                foreach (Transform riel in rieles)
                {
                    float dz = Mathf.Abs(riel.position.z - z);
                    if (dz < 1f || dz > 2f)
                    {
                        continue;
                    }
                    int k = Mathf.Clamp(Mathf.FloorToInt((riel.position.x - xArriba) / largoModulo), 0, n - 1);
                    float yFin = Mathf.Lerp(hArriba, hAbajo, (k + 1f) / n);
                    riel.position = new Vector3(riel.position.x, yFin, riel.position.z);
                    riel.localScale = new Vector3(riel.localScale.x, ys, riel.localScale.z);
                }
                Physics.SyncTransforms();
                Func<float, float> linea = x => Mathf.Lerp(hArriba, hAbajo, (x - xArriba) / (xAbajo - xArriba));
                for (int k = 0; k < n; k++)
                {
                    CalibrarRampa(orden[k], rampaPrefab, linea, z, largoModulo);
                }
                // Medicion final de los extremos de la cadena.
                float yTop = MedirRampa(orden[0], orden[0].position.x - largoModulo * 0.5f + 0.1f, z);
                float yBot = MedirRampa(orden[n - 1], orden[n - 1].position.x + largoModulo * 0.5f - 0.1f, z);
                // Continuidad: el piso mas alto de las rampas de la cadena cada 0.25 m; ninguna union con salto mayor a SaltoMaximoRampa.
                float anterior = float.NaN;
                float peorSalto = 0f;
                int huecos = 0;
                for (float xm = xArriba + 0.05f; xm <= xAbajo - 0.05f; xm += 0.25f)
                {
                    float ym = float.NaN;
                    foreach (Transform m in orden)
                    {
                        float v = MedirRampa(m, xm, z);
                        if (!float.IsNaN(v) && (float.IsNaN(ym) || v > ym))
                        {
                            ym = v;
                        }
                    }
                    if (float.IsNaN(ym))
                    {
                        huecos++;
                        continue;
                    }
                    if (!float.IsNaN(anterior))
                    {
                        peorSalto = Mathf.Max(peorSalto, Mathf.Abs(ym - anterior));
                    }
                    anterior = ym;
                }
                Log("3.   continuidad z=" + F(z) + ": salto maximo " + F(peorSalto) + " m (limite " + F(SaltoMaximoRampa) + "), puntos sin rampa " + huecos);
                Log("3.   extremos: arriba rampa y=" + F(yTop) + " vs calle " + F(hArriba) + "; abajo rampa y=" + F(yBot) + " vs piso " + F(hAbajo));
            }
            Log("3. Escaleras/CE01_EscaleraCerro_N (36 m, MeshCollider): no es una malla vieja; es el nucleo de concreto de cada escalera (Terreno.Escalera, 90 peldanos),"
                + " creado en la misma pasada que la cadena de modulos y 0.5 m mas abajo que la linea de peldanos; no duplica el piso porque la rampa de los modulos queda arriba. Se deja.");
        }

        private static float Normalizar(float grados)
        {
            return grados > 180f ? grados - 360f : grados;
        }

        /// <summary>Altura de la cara de arriba de la rampa del modulo en (x, z) o NaN.</summary>
        private static float MedirRampa(Transform modulo, float x, float z)
        {
            BoxCollider r = RampaDe(modulo);
            if (r == null)
            {
                return float.NaN;
            }
            Ray rayo = new Ray(new Vector3(x, modulo.position.y + 30f, z), Vector3.down);
            return r.Raycast(rayo, out RaycastHit h, 60f) ? h.point.y : float.NaN;
        }

        private static BoxCollider RampaDe(Transform modulo)
        {
            foreach (BoxCollider b in modulo.GetComponentsInChildren<BoxCollider>(true))
            {
                if (b.name.StartsWith("Rampa", StringComparison.Ordinal))
                {
                    return b;
                }
            }
            return null;
        }

        /// <summary>
        /// La rampa del modulo es una caja inclinada bajo un padre con escala distinta en cada eje: Unity no la deforma, solo la inclina y la escala,
        /// asi que no sigue la subida real. Se ajustan su inclinacion y su altura local (Newton con rayos) hasta que su cara de arriba pase por la linea
        /// de la calle en los dos extremos del modulo. Con el largo un poco mayor se traslapa con la vecina.
        /// </summary>
        private static void CalibrarRampa(Transform modulo, BoxCollider rampaPrefab, Func<float, float> linea, float z, float largoModulo)
        {
            BoxCollider r = RampaDe(modulo);
            if (r == null)
            {
                Log("3.   " + modulo.name + ": sin Rampa.");
                return;
            }
            r.enabled = true;
            r.size = new Vector3(rampaPrefab.size.x, rampaPrefab.size.y, rampaPrefab.size.z * ExtensionRampa);
            float xc = modulo.position.x;
            float xu = xc - largoModulo * 0.5f + 0.3f;
            float xd = xc + largoModulo * 0.5f - 0.3f;
            float tu = linea(xu);
            float td = linea(xd);
            Transform tr = r.transform;
            float a = Normalizar(tr.localEulerAngles.x);
            float b = tr.localPosition.y;
            Vector3 p0 = tr.localPosition;

            Func<float, float, Vector2> medir = (aa, bb) =>
            {
                tr.localRotation = Quaternion.Euler(aa, 0f, 0f);
                tr.localPosition = new Vector3(p0.x, bb, p0.z);
                Physics.SyncTransforms();
                return new Vector2(MedirRampa(modulo, xu, z), MedirRampa(modulo, xd, z));
            };

            const float da0 = 0.5f;
            const float db0 = 0.05f;
            bool convergio = false;
            for (int it = 0; it < 12; it++)
            {
                Vector2 m = medir(a, b);
                if (float.IsNaN(m.x) || float.IsNaN(m.y))
                {
                    break;
                }
                Vector2 f = new Vector2(m.x - tu, m.y - td);
                if (Mathf.Abs(f.x) < HolguraRampa && Mathf.Abs(f.y) < HolguraRampa)
                {
                    convergio = true;
                    break;
                }
                Vector2 ma = medir(a + da0, b);
                Vector2 mb = medir(a, b + db0);
                if (float.IsNaN(ma.x) || float.IsNaN(ma.y) || float.IsNaN(mb.x) || float.IsNaN(mb.y))
                {
                    break;
                }
                float j11 = (ma.x - m.x) / da0, j21 = (ma.y - m.y) / da0;
                float j12 = (mb.x - m.x) / db0, j22 = (mb.y - m.y) / db0;
                float det = j11 * j22 - j12 * j21;
                if (Mathf.Abs(det) < 0.000001f)
                {
                    break;
                }
                float da = -(j22 * f.x - j12 * f.y) / det;
                float db = -(-j21 * f.x + j11 * f.y) / det;
                a += Mathf.Clamp(da, -15f, 15f);
                b += Mathf.Clamp(db, -1f, 1f);
            }
            Vector2 fin = medir(a, b);
            if (!convergio && !(Mathf.Abs(fin.x - tu) < 0.05f && Mathf.Abs(fin.y - td) < 0.05f))
            {
                Log("3.   " + modulo.name + " x=" + F(xc) + ": rampa sin calibrar (mide " + F(fin.x) + "/" + F(fin.y) + ", objetivo " + F(tu) + "/" + F(td) + ").");
            }
        }

        // ================================================================ 4 y 5. accesos a azoteas

        private static void ArreglarAccesos(Ctx c)
        {
            int n = 0;
            foreach (Transform t in c.Detalles)
            {
                if (!t.name.StartsWith("Acceso_", StringComparison.Ordinal))
                {
                    continue;
                }
                PuntoTrepar p = t.GetComponent<PuntoTrepar>();
                if (p == null)
                {
                    continue;
                }
                Vector3 salida = p.Salida;
                if (!Techo_(c, salida.x, salida.z, salida.y + 30f, out float techo))
                {
                    Log("4. " + t.name + ": no se encontro techo bajo su salida " + V(salida) + "; se deja.");
                    continue;
                }
                float alto = techo - t.position.y;
                float antes = p.AlturaSubida;
                if (Mathf.Abs(antes - alto) > 0.01f)
                {
                    Log("4. " + t.name + ": sube " + F(antes) + " m, la azotea esta a " + F(alto) + " m (y=" + F(techo) + ") -> alturaSubida " + F(alto));
                }
                AjustarAcceso(t.gameObject, alto);
                n++;
            }
            Log("4. Accesos revisados: " + n + " (alturaSubida, salida, rieles y peldanos hasta la azotea).");
        }

        /// <summary>Pone la altura de subida, el trigger, los dos rieles y los peldanos de un acceso, con la pose que ya tiene.</summary>
        private static void AjustarAcceso(GameObject go, float alto)
        {
            PuntoTrepar p = go.GetComponent<PuntoTrepar>();
            SerializedObject so = new SerializedObject(p);
            so.FindProperty("alturaSubida").floatValue = alto;
            Vector3 salida = so.FindProperty("salida").vector3Value;
            salida.y = alto;
            if (salida.z < 0.1f)
            {
                salida.z = SalidaAcceso;
            }
            so.FindProperty("salida").vector3Value = salida;
            so.ApplyModifiedPropertiesWithoutUndo();
            p.AjustarTrigger();

            Transform raiz = go.transform;
            Vector3 pie = raiz.position;
            Vector3 atras = -raiz.forward * RielAtras;
            List<Transform> rieles = new List<Transform>();
            List<Transform> peldanos = new List<Transform>();
            foreach (Transform h in raiz)
            {
                if (h.name == "Riel")
                {
                    rieles.Add(h);
                }
                else if (h.name == "Peldano")
                {
                    peldanos.Add(h);
                }
            }
            for (int i = 0; i < rieles.Count; i++)
            {
                Transform riel = rieles[i];
                MeshFilter mf = riel.GetComponent<MeshFilter>();
                float meshAlto = mf != null && mf.sharedMesh != null ? mf.sharedMesh.bounds.size.y : alto;
                float lado = i % 2 == 0 ? -1f : 1f;
                riel.rotation = Quaternion.identity;
                riel.localScale = new Vector3(1f, alto / Mathf.Max(0.01f, meshAlto), 1f);
                riel.position = pie + raiz.right * (lado * RielLateral) + atras + Vector3.up * (alto * 0.5f);
            }
            int necesarios = 0;
            for (float y = PrimerPeldano; y < alto; y += PasoAccesoPeldano)
            {
                necesarios++;
            }
            Transform plantilla = peldanos.Count > 0 ? peldanos[0] : null;
            while (peldanos.Count < necesarios && plantilla != null)
            {
                Transform nuevo = Object.Instantiate(plantilla.gameObject, raiz).transform;
                nuevo.name = "Peldano";
                peldanos.Add(nuevo);
            }
            for (int k = 0; k < peldanos.Count; k++)
            {
                // Los que sobran se desactivan (no se borra nada que ya existia).
                peldanos[k].gameObject.SetActive(k < necesarios);
                peldanos[k].rotation = raiz.rotation * Quaternion.Euler(0f, 90f, 0f);
                peldanos[k].position = pie + atras + Vector3.up * (PrimerPeldano + k * PasoAccesoPeldano);
            }
        }

        private static void AislarAzoteas(Ctx c)
        {
            Physics.SyncTransforms();
            GameObject plantilla = null;
            foreach (Transform t in c.Detalles)
            {
                if (t.name.StartsWith("Acceso_", StringComparison.Ordinal) && t.GetComponent<PuntoTrepar>() != null && t.Find("Riel") != null && t.Find("Peldano") != null)
                {
                    plantilla = t.gameObject;
                    break;
                }
            }
            if (plantilla == null)
            {
                Log("5. ERROR: no hay un Acceso_* para duplicar.");
                return;
            }
            string jsonAntes = Path.Combine(RaizProyecto(), RutaBarridoAntes);
            if (!File.Exists(jsonAntes))
            {
                Log("5. ERROR: falta " + RutaBarridoAntes + " (de ahi salen las azoteas aisladas).");
                return;
            }
            BarridoZonasT1360.Hallazgo[] aisladas = JsonUtility.FromJson<BarridoZonasT1360.ArchivoHallazgos>(File.ReadAllText(jsonAntes)).hallazgos
                .Where(h => h.tipo == BarridoZonasT1360.TipoAzoteaAislada).ToArray();
            Log("5. Azoteas aisladas segun el barrido T-1360 (json antes): " + aisladas.Length);
            int agregados = 0;
            int sinCalle = 0;
            int noCaminable = 0;
            int sinSitio = 0;
            int ya = 0;
            Vector3[] lados = { Vector3.right, Vector3.left, Vector3.forward, Vector3.back };
            foreach (BarridoZonasT1360.Hallazgo h in aisladas)
            {
                Transform casa = null;
                Bounds b = default;
                foreach (Transform t in c.Casas)
                {
                    if (!t.name.StartsWith("CC0", StringComparison.Ordinal))
                    {
                        continue;
                    }
                    Bounds? lim = Limites(t);
                    if (lim.HasValue && Mathf.Abs(lim.Value.center.x - h.posicion.x) < 0.3f && Mathf.Abs(lim.Value.center.z - h.posicion.z) < 0.3f
                        && Mathf.Abs(lim.Value.max.y - h.posicion.y) < 0.3f)
                    {
                        casa = t;
                        b = lim.Value;
                        break;
                    }
                }
                if (casa == null)
                {
                    Log("5. " + h.ruta + " " + V(h.posicion) + ": no se encontro la casa; se salta.");
                    continue;
                }
                float techo = b.max.y;
                // Techo caminable: casi todos los puntos de la rejilla de 3x3 a la altura del punto mas alto.
                int planos = 0;
                for (int ix = -1; ix <= 1; ix++)
                {
                    for (int iz = -1; iz <= 1; iz++)
                    {
                        float x = b.center.x + ix * (b.extents.x - 0.7f);
                        float z = b.center.z + iz * (b.extents.z - 0.7f);
                        if (Techo_(c, x, z, techo + 3f, out float y) && Mathf.Abs(techo - y) <= SalidaMinimaTecho)
                        {
                            planos++;
                        }
                    }
                }
                if (planos < 5)
                {
                    Log("5. " + casa.name + " " + V(b.center) + ": techo no caminable (" + planos + "/9 puntos planos a la altura maxima " + F(techo) + "); sin acceso.");
                    noCaminable++;
                    continue;
                }
                bool yaTiene = false;
                foreach (Transform t in c.Detalles)
                {
                    PuntoTrepar pt = t.GetComponent<PuntoTrepar>();
                    if (pt != null && t.name.StartsWith("Acceso_T1363", StringComparison.Ordinal) && b.Contains(new Vector3(pt.Salida.x, b.center.y, pt.Salida.z)))
                    {
                        yaTiene = true;
                        break;
                    }
                }
                if (yaTiene)
                {
                    ya++;
                    continue;
                }
                // Lado: el que da a la calle (el eje mas cercano), si no el que apoya en otra azotea vecina a menos de 6 m.
                int mejor = -1;
                float mejorPuntaje = float.MaxValue;
                float mejorPiso = 0f;
                Vector3 mejorPos = default;
                bool mejorEsCalle = false;
                for (int i = 0; i < lados.Length; i++)
                {
                    Vector3 o = lados[i];
                    bool enX = i < 2;
                    float semi = enX ? b.extents.x : b.extents.z;
                    float largoLado = enX ? b.size.z : b.size.x;
                    foreach (float desfase in new[] { largoLado * 0.5f - 0.5f, -(largoLado * 0.5f - 0.5f), 0f })
                    {
                        Vector3 baseLado = b.center + o * (semi + 0.05f) + (enX ? Vector3.forward : Vector3.right) * desfase;
                        Vector3 punto = new Vector3(baseLado.x, techo, baseLado.z);
                        float dCalle;
                        CalleMasCercana(c, punto, out dCalle, out Vector3 pc, out Vector3 tc);
                        bool calle = dCalle <= AlcanceCalleAcceso;
                        // Piso en el pie: la calle, la ladera o una azotea vecina.
                        float yPiso = float.MinValue;
                        bool hay = false;
                        foreach (RaycastHit rh in Rayos(c, new Vector3(punto.x, techo - 0.5f, punto.z), 40f))
                        {
                            Transform tt = rh.collider.transform;
                            bool valido = tt.IsChildOf(c.Terreno) || tt.IsChildOf(c.Calles) || (tt.IsChildOf(c.Casas) && !tt.IsChildOf(casa));
                            if (valido && rh.normal.y > 0.5f && rh.point.y > yPiso)
                            {
                                yPiso = rh.point.y;
                                hay = true;
                            }
                        }
                        if (!hay || techo - yPiso > AlturaMaximaAcceso || techo - yPiso < 1f)
                        {
                            continue;
                        }
                        // Sitio libre para los rieles y el cuerpo del jugador al pie de la escalera.
                        Vector3 centroCaja = new Vector3(punto.x, yPiso + 1.6f, punto.z) + o * 0.55f;
                        Vector3 mitades = enX ? new Vector3(0.4f, 0.8f, 0.5f) : new Vector3(0.5f, 0.8f, 0.4f);
                        bool libre = true;
                        int nCaja = c.Fisica.OverlapBox(centroCaja, mitades, bufColliders, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore);
                        for (int ic = 0; ic < nCaja; ic++)
                        {
                            Collider col = bufColliders[ic];
                            Transform tt = col.transform;
                            if (!tt.IsChildOf(c.Terreno) && !tt.IsChildOf(c.Calles) && !tt.IsChildOf(casa))
                            {
                                libre = false;
                                break;
                            }
                            if (tt.IsChildOf(c.Terreno))
                            {
                                libre = false;
                                break;
                            }
                        }
                        // Salida: 0.8 m dentro de la pared, sobre el techo.
                        Vector3 interior = punto - o * (0.05f + SalidaAcceso);
                        if (!libre || !Techo_(c, interior.x, interior.z, techo + 3f, out float yS) || Mathf.Abs(yS - techo) > SalidaMinimaTecho)
                        {
                            continue;
                        }
                        float puntaje = calle ? dCalle : 1000f + dCalle;
                        if (puntaje < mejorPuntaje)
                        {
                            mejorPuntaje = puntaje;
                            mejor = i;
                            mejorPiso = yPiso;
                            mejorPos = new Vector3(punto.x, yPiso, punto.z);
                            mejorEsCalle = calle;
                        }
                    }
                }
                if (mejor < 0)
                {
                    Log("5. " + casa.name + " " + V(b.center) + ": sin lado libre con calle ni azotea vecina a menos de 6 m; sin acceso.");
                    sinSitio++;
                    continue;
                }
                if (!mejorEsCalle)
                {
                    // Solo apoya en una azotea vecina: si la calle esta a mas de 6 m del borde, se anota.
                    sinCalle++;
                }
                GameObject go = Object.Instantiate(plantilla, c.Detalles);
                go.name = "Acceso_T1363_" + agregados + "_" + Mathf.RoundToInt(b.center.x) + "_" + Mathf.RoundToInt(b.center.z);
                Vector3 o2 = lados[mejor];
                go.transform.SetPositionAndRotation(mejorPos, Quaternion.LookRotation(-o2));
                AjustarAcceso(go, techo - mejorPiso);
                Log("5. " + casa.name + " " + V(b.center) + ": acceso " + go.name + " en el lado " + V(o2) + (mejorEsCalle ? " (da a la calle)" : " (apoya en azotea vecina)")
                    + ", sube " + F(techo - mejorPiso) + " m.");
                agregados++;
            }
            Log("5. Accesos nuevos: " + agregados + " (de ellos " + sinCalle + " apoyan en una azotea vecina); ya tenian: " + ya + "; techo no caminable: " + noCaminable
                + "; sin sitio: " + sinSitio + ".");
            Physics.SyncTransforms();
        }

        // ================================================================ 6. saltos de parkour

        private static List<Techo> Techos(Ctx c)
        {
            List<Techo> lista = new List<Techo>();
            foreach (Transform h in c.Casas)
            {
                if (h.name.StartsWith("Casa_", StringComparison.Ordinal))
                {
                    Transform cuerpo = h.Find("Cuerpo");
                    BoxCollider bc = cuerpo != null ? cuerpo.GetComponent<BoxCollider>() : null;
                    if (bc != null)
                    {
                        lista.Add(new Techo { Raiz = h, B = bc.bounds, Kit = true });
                    }
                }
                else if (h.name.StartsWith("CC0", StringComparison.Ordinal))
                {
                    Bounds? b = Limites(h);
                    if (b.HasValue)
                    {
                        lista.Add(new Techo { Raiz = h, B = b.Value });
                    }
                }
            }
            return lista;
        }

        private static List<Par> Pares(List<Techo> techos)
        {
            List<Par> pares = new List<Par>();
            for (int i = 0; i < techos.Count; i++)
            {
                Bounds a = techos[i].B;
                for (int j = 0; j < techos.Count; j++)
                {
                    if (i == j)
                    {
                        continue;
                    }
                    Bounds b = techos[j].B;
                    if (Mathf.Abs(a.center.x - b.center.x) > 40f || Mathf.Abs(a.center.z - b.center.z) > 40f)
                    {
                        continue;
                    }
                    float gx = Mathf.Max(0f, Mathf.Max(a.min.x - b.max.x, b.min.x - a.max.x));
                    float gz = Mathf.Max(0f, Mathf.Max(a.min.z - b.max.z, b.min.z - a.max.z));
                    if ((gx > 0.001f && gz > 0.001f) || (gx <= 0.001f && gz <= 0.001f))
                    {
                        continue;
                    }
                    int eje = gx > gz ? 0 : 1;
                    float hueco = Mathf.Max(gx, gz);
                    if (hueco >= HuecoVecina)
                    {
                        continue;
                    }
                    float signo = Eje(b.min, eje) >= Eje(a.max, eje) - 0.001f ? 1f : -1f;
                    int perp = 1 - eje;
                    float pMin = Mathf.Max(Eje(a.min, perp), Eje(b.min, perp));
                    float pMax = Mathf.Min(Eje(a.max, perp), Eje(b.max, perp));
                    if (pMax - pMin < 0.3f)
                    {
                        continue;
                    }
                    pares.Add(new Par { A = techos[i], B = techos[j], Eje = eje, Signo = signo, Hueco = hueco, PerpMin = pMin, PerpMax = pMax });
                }
            }
            return pares;
        }

        private static void ArreglarSaltos(Ctx c)
        {
            Physics.SyncTransforms();
            List<Techo> techos = Techos(c);
            List<Par> pares = Pares(techos);
            Log("6. Azoteas: " + techos.Count + "; pares a menos de " + F(HuecoVecina) + " m: " + pares.Count);

            // Cosas que estorban en la azotea: tinaco (con su base), aire, antena, tendedero, lavadero.
            List<Movible> movibles = new List<Movible>();
            foreach (Techo t in techos)
            {
                if (!t.Kit)
                {
                    continue;
                }
                List<Transform> tinaco = new List<Transform>();
                foreach (Transform h in t.Raiz)
                {
                    if (h.name == "BaseTinaco" || h.name == "C09_Tinaco")
                    {
                        tinaco.Add(h);
                    }
                    else if (h.name == "C10_AireAcondicionado" || h.name == "CY04_AntenaAzotea")
                    {
                        movibles.Add(new Movible { Partes = new[] { h }, Casa = t });
                    }
                }
                if (tinaco.Count > 0)
                {
                    movibles.Add(new Movible { Partes = tinaco.ToArray(), Casa = t });
                }
            }
            foreach (Transform d in c.Detalles)
            {
                if (!d.name.StartsWith("CC04_Tendedero", StringComparison.Ordinal) && !d.name.StartsWith("CC05_Lavadero", StringComparison.Ordinal))
                {
                    continue;
                }
                Bounds? b = Limites(d);
                if (!b.HasValue)
                {
                    continue;
                }
                Techo dueno = null;
                float menor = float.MaxValue;
                foreach (Techo t in techos)
                {
                    Vector3 q = b.Value.center;
                    if (q.x >= t.B.min.x - 0.3f && q.x <= t.B.max.x + 0.3f && q.z >= t.B.min.z - 0.3f && q.z <= t.B.max.z + 0.3f)
                    {
                        float dy = Mathf.Abs(b.Value.min.y - t.Y);
                        if (dy < menor && dy < 1.5f)
                        {
                            menor = dy;
                            dueno = t;
                        }
                    }
                }
                if (dueno != null)
                {
                    movibles.Add(new Movible { Partes = new[] { d }, Casa = dueno });
                }
            }

            int movidos = 0;
            int apretados = 0;
            foreach (Movible m in movibles)
            {
                foreach (Par par in pares)
                {
                    if (par.A != m.Casa)
                    {
                        continue;
                    }
                    Bounds? lim = Limites(m.Partes);
                    if (!lim.HasValue)
                    {
                        continue;
                    }
                    Bounds mb = lim.Value;
                    int perp = 1 - par.Eje;
                    if (Eje(mb.max, perp) < par.PerpMin - 0.5f || Eje(mb.min, perp) > par.PerpMax + 0.5f)
                    {
                        continue;
                    }
                    float borde = par.Signo > 0f ? Eje(par.A.B.max, par.Eje) : Eje(par.A.B.min, par.Eje);
                    float dentro = par.Signo > 0f ? borde - Eje(mb.max, par.Eje) : Eje(mb.min, par.Eje) - borde;
                    if (dentro >= SeparacionBordeProp)
                    {
                        continue;
                    }
                    float mover = SeparacionBordeProp - dentro;
                    float lejos = par.Signo > 0f ? Eje(mb.min, par.Eje) - Eje(par.A.B.min, par.Eje) : Eje(par.A.B.max, par.Eje) - Eje(mb.max, par.Eje);
                    if (lejos - mover < 0.1f)
                    {
                        mover = Mathf.Max(0f, lejos - 0.1f);
                        apretados++;
                    }
                    Vector3 delta = par.Eje == 0 ? new Vector3(-par.Signo * mover, 0f, 0f) : new Vector3(0f, 0f, -par.Signo * mover);
                    foreach (Transform parte in m.Partes)
                    {
                        parte.position += delta;
                    }
                    movidos++;
                    Log("6. " + Ruta(m.Partes[0], c.Raiz.transform) + " a " + F(dentro) + " m del borde que da a otra azotea (hueco " + F(par.Hueco) + " m): se mueve " + F(mover) + " m hacia dentro.");
                }
            }
            Physics.SyncTransforms();
            Log("6. Props movidos hacia dentro: " + movidos + " (" + apretados + " sin espacio para toda la separacion).");

            // Pretiles entre azoteas pegadas casi al mismo nivel.
            int pretiles = 0;
            foreach (Par par in pares)
            {
                if (par.Hueco > HuecoVecina || Mathf.Abs(par.A.Y - par.B.Y) > DesnivelSaltable || !par.A.Kit)
                {
                    continue;
                }
                float borde = par.Signo > 0f ? Eje(par.A.B.max, par.Eje) : Eje(par.A.B.min, par.Eje);
                foreach (Transform h in par.A.Raiz)
                {
                    if (h.name != "C05_PretilAzotea" || !h.gameObject.activeSelf)
                    {
                        continue;
                    }
                    Bounds? pb = Limites(h);
                    Vector3 centro = pb.HasValue ? pb.Value.center : h.position;
                    if (Mathf.Abs(Eje(centro, par.Eje) - borde) < 0.8f && Eje(centro, 1 - par.Eje) >= par.PerpMin - 0.3f && Eje(centro, 1 - par.Eje) <= par.PerpMax + 0.3f)
                    {
                        h.gameObject.SetActive(false);
                        pretiles++;
                    }
                }
            }
            Log("6. Pretiles C05 desactivados en los bordes enfrentados (hueco <= " + F(HuecoVecina) + " m, desnivel <= " + F(DesnivelSaltable) + " m): " + pretiles);

            // Revision con capsulas (radio 0.35, alto 1.8, desde 0.05 sobre la azotea) del paso de 2.5 m a cada lado del borde.
            int estorbos = 0;
            int movidosPaso = 0;
            HashSet<Transform> yaMovidas = new HashSet<Transform>();
            foreach (Par par in pares)
            {
                if (par.B.Y - par.A.Y > DesnivelSaltable)
                {
                    continue;
                }
                float borde = par.Signo > 0f ? Eje(par.A.B.max, par.Eje) : Eje(par.A.B.min, par.Eje);
                float centroPerp = (par.PerpMin + par.PerpMax) * 0.5f;
                bool tocoAlgo = false;
                for (float s = -PasoLibreBorde; s <= par.Hueco + PasoLibreBorde + 0.01f; s += 0.4f)
                {
                    foreach (float off in new[] { -0.5f, 0f, 0.5f })
                    {
                        float perp = par.PerpMax - par.PerpMin >= 1.7f ? centroPerp + off : centroPerp;
                        float along = borde + par.Signo * s;
                        bool ladoB = s > par.Hueco * 0.5f;
                        float yPie = (ladoB ? par.B.Y : par.A.Y) + 0.05f;
                        Vector3 pos = par.Eje == 0 ? new Vector3(along, yPie, perp) : new Vector3(perp, yPie, along);
                        Vector3 p0 = pos + Vector3.up * RadioCapsula;
                        Vector3 p1 = pos + Vector3.up * (AlturaPasoLibre - RadioCapsula);
                        int nCaps = c.Fisica.OverlapCapsule(p0, p1, RadioCapsula, bufColliders, ~0, QueryTriggerInteraction.Ignore);
                        Collider[] golpes = new Collider[nCaps];
                        Array.Copy(bufColliders, golpes, nCaps);
                        foreach (Collider col in golpes)
                        {
                            Transform tt = col.transform;
                            if (!col.enabled || tt.IsChildOf(c.Terreno) || tt.IsChildOf(c.Calles) || tt.IsChildOf(c.Escaleras) || tt.IsChildOf(c.Postes) || tt.IsChildOf(c.Carros))
                            {
                                continue;
                            }
                            if (tt.IsChildOf(c.Detalles))
                            {
                                // Lo sólido de Detalles (lavadero) se aparta 1.5 m del paso, una vez; solo la ropa (tendedero) se queda sin collider.
                                Transform prop = tt;
                                while (prop.parent != null && prop.parent != c.Detalles)
                                {
                                    prop = prop.parent;
                                }
                                if (!prop.name.StartsWith("CC04_Tendedero", StringComparison.Ordinal) && yaMovidas.Add(prop))
                                {
                                    Techo casaDetalle = Vector3.Distance(prop.position, par.A.B.center) < Vector3.Distance(prop.position, par.B.B.center) ? par.A : par.B;
                                    float ladoDetalle = Eje(col.bounds.center, par.Eje) < Eje(casaDetalle.B.center, par.Eje) ? 1f : -1f;
                                    prop.position += (par.Eje == 0 ? new Vector3(ladoDetalle, 0f, 0f) : new Vector3(0f, 0f, ladoDetalle)) * 1.5f;
                                    movidosPaso++;
                                    tocoAlgo = true;
                                    Log("6. " + Ruta(prop, c.Raiz.transform) + " estorba el paso entre azoteas " + par.A.Raiz.name + " -> " + par.B.Raiz.name + ": se aparta 1.5 m.");
                                    continue;
                                }
                                if (!prop.name.StartsWith("CC04_Tendedero", StringComparison.Ordinal))
                                {
                                    continue; // ya se apartó: no se le quita el collider a algo sólido
                                }
                                col.enabled = false;
                                estorbos++;
                                tocoAlgo = true;
                                Log("6. " + Ruta(tt, c.Raiz.transform) + " estorba el paso entre azoteas " + par.A.Raiz.name + " -> " + par.B.Raiz.name + " en " + V(pos) + ": collider apagado.");
                                continue;
                            }
                            // Parte de una de las dos casas (tinaco, aire, antena, cuarto Extra_, pretil, fachada que sobresale).
                            Transform parte = tt;
                            while (parte.parent != null && parte.parent != par.A.Raiz && parte.parent != par.B.Raiz && parte.parent != c.Raiz.transform)
                            {
                                parte = parte.parent;
                            }
                            if (parte.parent != par.A.Raiz && parte.parent != par.B.Raiz)
                            {
                                continue;
                            }
                            // Las paredes nunca pierden su collider (la fachada de la casa más alta es donde el jugador se agarra del borde);
                            // solo se quitan pretiles del borde y cosas de azotea.
                            if (parte.name == "Cuerpo" || parte.name.StartsWith("AzoteaT", StringComparison.Ordinal) || EsPared(parte.name))
                            {
                                continue;
                            }
                            bool movible = parte.name == "BaseTinaco" || parte.name.StartsWith("C09", StringComparison.Ordinal) || parte.name.StartsWith("C10", StringComparison.Ordinal)
                                || parte.name.StartsWith("CY04", StringComparison.Ordinal) || parte.name.StartsWith("Extra_", StringComparison.Ordinal);
                            if (movible && yaMovidas.Add(parte))
                            {
                                // 1 m hacia el centro de su casa, a lo largo del eje del paso.
                                Techo casa = parte.parent == par.A.Raiz ? par.A : par.B;
                                float lado = Eje(col.bounds.center, par.Eje) < Eje(casa.B.center, par.Eje) ? 1f : -1f;
                                parte.position += par.Eje == 0 ? new Vector3(lado, 0f, 0f) : new Vector3(0f, 0f, lado);
                                movidosPaso++;
                                tocoAlgo = true;
                                Log("6. " + Ruta(parte, c.Raiz.transform) + " estorba el paso entre azoteas " + par.A.Raiz.name + " -> " + par.B.Raiz.name + ": se mueve 1 m hacia dentro.");
                            }
                            else
                            {
                                col.enabled = false;
                                estorbos++;
                                tocoAlgo = true;
                                Log("6. " + Ruta(tt, c.Raiz.transform) + " estorba el paso entre azoteas " + par.A.Raiz.name + " -> " + par.B.Raiz.name + " en " + V(pos) + ": collider apagado.");
                            }
                        }
                    }
                }
                if (tocoAlgo)
                {
                    Physics.SyncTransforms();
                }
            }
            Physics.SyncTransforms();
            Log("6. Partes de casas movidas 1 m del paso: " + movidosPaso);
            Log("6. Colliders de Detalles apagados por estorbar el paso: " + estorbos);
        }

        // ================================================================ verificacion

        private static void Verificar()
        {
            Ctx c = Abrir(false);
            try
            {
                IReadOnlyList<BarridoZonasT1360.Hallazgo> despues = BarridoZonasT1360.BarrerZona(c.Raiz);
                Dictionary<string, int[]> cuenta = new Dictionary<string, int[]>();
                string json = Path.Combine(RaizProyecto(), RutaBarridoAntes);
                if (File.Exists(json))
                {
                    BarridoZonasT1360.ArchivoHallazgos antes = JsonUtility.FromJson<BarridoZonasT1360.ArchivoHallazgos>(File.ReadAllText(json));
                    foreach (BarridoZonasT1360.Hallazgo h in antes.hallazgos)
                    {
                        Sumar(cuenta, h.tipo, 0, h.cantidad);
                    }
                }
                foreach (BarridoZonasT1360.Hallazgo h in despues)
                {
                    Sumar(cuenta, h.tipo, 1, h.cantidad);
                }
                Log("Barrido (total de hallazgos antes -> despues):");
                foreach (KeyValuePair<string, int[]> par in cuenta.OrderBy(p => p.Key))
                {
                    Log("  " + par.Key + ": " + par.Value[0] + " -> " + par.Value[1]);
                }
                foreach (BarridoZonasT1360.Hallazgo h in despues)
                {
                    if (h.tipo == BarridoZonasT1360.TipoSinCollider || h.tipo == BarridoZonasT1360.TipoCarro || h.tipo == BarridoZonasT1360.TipoPeaton
                        || h.tipo == BarridoZonasT1360.TipoEscaleraSalto || h.tipo == BarridoZonasT1360.TipoAtrapado || h.tipo == BarridoZonasT1360.TipoEncimado
                        || h.tipo == BarridoZonasT1360.TipoRampaRota)
                    {
                        Log("  queda: " + h.tipo + " " + h.ruta + " " + V(h.posicion) + " " + h.detalle);
                    }
                }
                RevisarBanquetas(c);
                ArteT250.PonerLuz();
                TomarFotos("despues");
            }
            catch (Exception e)
            {
                Log("ERROR en la verificacion: " + e);
                Debug.LogError("[ArreglarCerroT1363] " + e);
            }
            finally
            {
                Object.DestroyImmediate(c.Raiz);
            }
        }

        private static void Sumar(Dictionary<string, int[]> cuenta, string tipo, int indice, int cantidad)
        {
            if (!cuenta.TryGetValue(tipo, out int[] v))
            {
                v = new int[2];
                cuenta[tipo] = v;
            }
            v[indice] += Mathf.Max(1, cantidad);
        }

        /// <summary>8. Quien ocupa la banqueta: capsulas a lo largo de la linea de peatones del barrido (ancho/2 - 0.75) y de la banqueta real (en medio de los 1.5 m).</summary>
        private static void RevisarBanquetas(Ctx c)
        {
            Dictionary<string, int> quien = new Dictionary<string, int>();
            foreach (CalleZona cz in c.Zonas)
            {
                if (!cz.Banquetas || cz.Autopista || cz.Cantidad < 2)
                {
                    continue;
                }
                foreach (float lateral in new[] { cz.Ancho * 0.5f - 0.75f, cz.Ancho * 0.5f + 0.75f })
                {
                    foreach (float signo in new[] { 1f, -1f })
                    {
                        foreach (Vector3 s in BarridoZonasT1360.Muestrear(c.Puntos[cz], 3f, lateral * signo))
                        {
                            if (!Piso(c, s.x, s.z, s.y + 3f, out float y))
                            {
                                continue;
                            }
                            Vector3 p0 = new Vector3(s.x, y + 0.35f, s.z);
                            foreach (Collider col in Physics.OverlapCapsule(p0, p0 + Vector3.up * 1.1f, 0.25f, ~0, QueryTriggerInteraction.Ignore))
                            {
                                Transform raiz = col.transform;
                                while (raiz.parent != null && raiz.parent != c.Raiz.transform && raiz.parent.parent != c.Raiz.transform)
                                {
                                    raiz = raiz.parent;
                                }
                                string clave = "lateral " + F(lateral) + " " + (raiz.parent != null ? raiz.parent.name + "/" : "") + (raiz.name.Contains("_") ? raiz.name.Substring(0, raiz.name.LastIndexOf('_')) : raiz.name);
                                quien[clave] = quien.TryGetValue(clave, out int n) ? n + 1 : 1;
                            }
                        }
                    }
                }
            }
            Log("8. Colliders que ocupan la linea de peatones (3.75 m del eje = borde del arroyo) y la banqueta real (5.25 m):");
            foreach (KeyValuePair<string, int> par in quien.OrderBy(p => p.Key))
            {
                Log("  " + par.Key + ": " + par.Value + " muestras");
            }
        }

        // ================================================================ fotos

        private static void TomarFotos(string sufijo)
        {
            GameObject go = new GameObject("CamaraT1363");
            try
            {
                Camera cam = go.AddComponent<Camera>();
                cam.fieldOfView = 60f;
                cam.nearClipPlane = 0.1f;
                cam.farClipPlane = 1500f;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.35f, 0.4f, 0.5f);
                Physics.SyncTransforms();
                foreach (Foto f in Fotos)
                {
                    Vector3 pos = f.Objetivo + f.Desplazamiento;
                    int subidas = 0;
                    while (Physics.CheckSphere(pos, 0.6f, ~0, QueryTriggerInteraction.Ignore) && subidas < 20)
                    {
                        pos += Vector3.up * 1.5f;
                        subidas++;
                    }
                    cam.transform.position = pos;
                    cam.transform.LookAt(f.Objetivo);
                    string ruta = Path.Combine(RaizProyecto(), CarpetaFotos, "t1363-" + f.Arreglo + "-" + f.N + "-" + sufijo + ".png");
                    ArteT250.Guardar(cam, ruta);
                }
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        /// <summary>Fachadas, pisos, franjas y puertas del kit: paredes que nunca deben perder su collider.</summary>
        private static bool EsPared(string nombre)
        {
            return nombre.StartsWith("C01_", StringComparison.Ordinal) || nombre.StartsWith("C02_", StringComparison.Ordinal) || nombre.StartsWith("C03_", StringComparison.Ordinal)
                || nombre.StartsWith("C20", StringComparison.Ordinal) || nombre.StartsWith("C23", StringComparison.Ordinal) || nombre == "Puerta" || nombre == "Cimiento"
                || nombre == "Visual" || nombre.StartsWith("CC01_CasaCerro", StringComparison.Ordinal) || nombre.StartsWith("CC02_CasaCerro", StringComparison.Ordinal);
        }
    }
}
