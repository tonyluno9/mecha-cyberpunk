using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// S2 zona 6 (docs/playtest-05nov.md): las seis calles de salida de T-530 (T530_Afueras/Salida_*) eran cubos con un plano de ventanas. Cada edificio se
    /// viste en su cara a la calle con el kit modular: planta baja C01/C02/C03 (locales con cortina, viviendas), franja C23, pisos C20a/C20b/C21 y cornisa
    /// C22; en la azotea tinacos C09, aires C10 y antenas CY04; cables cruzando la calle. Cada edificio lleva LODGroup (LOD0 el kit, LOD1 el cubo y su plano
    /// de ventanas de antes), como el kit de pisos de T-419. No se tocan calles, banquetas, colisiones ni la ruta de la demo. Idempotente (rehace S2_Calles).
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ColocarS2Calles.Colocar
    /// </summary>
    public static class ColocarS2Calles
    {
        public const string NombreRaiz = "S2_Calles";
        private const string C = "Assets/Arte/Calle/Prefabs/";
        private const float Banqueta = 0.2f;
        private const float AltoPlantaBaja = 3.6f;
        private const float AltoFranja = 0.45f;
        private const float AltoPiso = 3.3f;
        private const float AnchoLocal = 4.1f;
        private const float AnchoPiso = 4.2f;
        private const float AltoCornisa = 0.6f;
        private const float LodKit = 0.06f;        // fracción de pantalla hasta la que se ve el kit
        private const float LodLejos = 0.004f;
        private static readonly string[] PlantasBajas = { "C01_FachadaLocal_cy", "C02_FachadaVivienda_cy", "C01_FachadaLocal_cy", "C03_FachadaPisoAlto_cy" };
        private static readonly string[] Pisos = { "C20b_PisoDosVentanas_cy", "C20a_PisoVentana_cy", "C20b_PisoDosVentanas_cy", "C21_PisoLiso_cy", "C20a_PisoVentana_cy" };
        private static readonly Dictionary<string, GameObject> Prefabs = new Dictionary<string, GameObject>();

        [MenuItem("Mecha Cyberpunk/S2/6 Calles de salida con el kit")]
        public static void Colocar()
        {
            ColocarS2.Informe.Clear();
            Prefabs.Clear();
            ColocarS2.Abrir();
            Object.DestroyImmediate(GameObject.Find(NombreRaiz));
            Transform raiz = new GameObject(NombreRaiz).transform;
            Transform afueras = ColocarS2.Buscar("T530_Afueras");
            int edificios = 0, piezas = 0;
            System.Random azar = new System.Random(502);
            foreach (Transform salida in afueras.Cast<Transform>().Where(t => t.name.StartsWith("Salida_")))
            {
                Transform calle = salida.Find("Calle");
                if (calle == null)
                {
                    continue;
                }
                Bounds bc = calle.GetComponent<Renderer>().bounds;
                bool ejeX = bc.size.x > bc.size.z;
                Transform grupo = new GameObject(salida.name).transform;
                grupo.SetParent(raiz, false);
                List<Transform> ventanas = salida.Cast<Transform>().Where(t => t.name == "Ventanas").ToList();
                List<(Vector3 punto, Vector3 dir)> anclas = new List<(Vector3, Vector3)>();
                foreach (Transform ed in salida.Cast<Transform>().Where(t => t.name == "Edificio"))
                {
                    Bounds b = ed.GetComponent<Renderer>().bounds;
                    // Cara del edificio que mira a la calle.
                    // Un edificio atravesado sobre el eje de la calle es el remate del fondo: se viste su cara que mira hacia el barrio.
                    bool remate = ejeX ? Mathf.Abs(b.center.z - bc.center.z) < bc.extents.z : Mathf.Abs(b.center.x - bc.center.x) < bc.extents.x;
                    bool caraEnZ = remate ? !ejeX : ejeX;
                    Vector3 dir = remate
                        ? (ejeX ? new Vector3(-Mathf.Sign(b.center.x), 0f, 0f) : new Vector3(0f, 0f, -Mathf.Sign(b.center.z)))
                        : (ejeX ? new Vector3(0f, 0f, Mathf.Sign(bc.center.z - b.center.z)) : new Vector3(Mathf.Sign(bc.center.x - b.center.x), 0f, 0f));
                    float cara = caraEnZ ? (dir.z > 0f ? b.max.z : b.min.z) : (dir.x > 0f ? b.max.x : b.min.x);
                    float a0 = caraEnZ ? b.min.x : b.min.z;
                    float a1 = caraEnZ ? b.max.x : b.max.z;
                    Transform kit = new GameObject("Kit_" + edificios).transform;
                    kit.SetParent(grupo, false);
                    piezas += Vestir(kit, caraEnZ, dir, cara, a0, a1, b.max.y, azar);
                    piezas += Azotea(kit, b, azar);
                    // LOD: de cerca el kit; de lejos el cubo y su plano de ventanas de antes.
                    Transform vent = ventanas.OrderBy(v => (v.position - new Vector3(b.center.x, v.position.y, b.center.z)).sqrMagnitude).FirstOrDefault();
                    List<Renderer> lejos = new List<Renderer> { ed.GetComponent<Renderer>() };
                    if (vent != null && (vent.position - b.center).sqrMagnitude < b.extents.sqrMagnitude * 1.5f)
                    {
                        lejos.Add(vent.GetComponent<Renderer>());
                    }
                    LODGroup lod = kit.gameObject.AddComponent<LODGroup>();
                    Renderer[] cerca = kit.GetComponentsInChildren<Renderer>().Concat(new[] { ed.GetComponent<Renderer>() }).ToArray();
                    lod.SetLODs(new[] { new LOD(LodKit, cerca), new LOD(LodLejos, lejos.ToArray()) });
                    lod.RecalculateBounds();
                    if (!remate)
                    {
                        anclas.Add((new Vector3(ejeX ? b.center.x : cara, 0f, ejeX ? cara : b.center.z), dir));
                    }
                    edificios++;
                }
                piezas += Cables(grupo, anclas, ejeX, bc);
            }
            ColocarS2.Informe.AppendLine("Calles de salida: " + edificios + " edificios vestidos con " + piezas + " piezas del kit");
            ColocarS2.Guardar("s2-calles");
        }

        internal static int Vestir(Transform kit, bool ejeX, Vector3 dir, float cara, float a0, float a1, float techo, System.Random azar)
        {
            int n = 0;
            float ancho = a1 - a0;
            Quaternion giro = Quaternion.LookRotation(-dir, Vector3.up); // el kit mira a -Z: así su frente mira a la calle
            int locales = Mathf.Max(1, Mathf.RoundToInt(ancho / AnchoLocal));
            float paso = ancho / locales;
            int inicio = azar.Next(PlantasBajas.Length);
            for (int i = 0; i < locales; i++)
            {
                n += Pieza(kit, PlantasBajas[(inicio + i) % PlantasBajas.Length], ejeX, dir, cara, giro, a0 + paso * i, a0 + paso * (i + 1), Banqueta, Banqueta + AltoPlantaBaja, 0.7f) ? 1 : 0;
            }
            float y = Banqueta + AltoPlantaBaja;
            n += Pieza(kit, "C23_Franja", ejeX, dir, cara, giro, a0, a1, y - 0.1f, y + AltoFranja, 0.25f) ? 1 : 0;
            y += AltoFranja;
            int columnas = Mathf.Max(1, Mathf.RoundToInt(ancho / AnchoPiso));
            float pasoPiso = ancho / columnas;
            int filas = Mathf.Max(0, Mathf.FloorToInt((techo - AltoCornisa - y) / AltoPiso));
            float altoFila = filas > 0 ? (techo - AltoCornisa - y) / filas : 0f;
            for (int f = 0; f < filas; f++)
            {
                int tipo = azar.Next(Pisos.Length);
                for (int c = 0; c < columnas; c++)
                {
                    string pieza = Pisos[(tipo + c + f) % Pisos.Length];
                    n += Pieza(kit, pieza, ejeX, dir, cara, giro, a0 + pasoPiso * c, a0 + pasoPiso * (c + 1), y + altoFila * f, y + altoFila * (f + 1), 0.45f) ? 1 : 0;
                }
            }
            n += Pieza(kit, "C22_Cornisa_cy", ejeX, dir, cara, giro, a0, a1, techo - AltoCornisa, techo + 0.15f, 0.3f) ? 1 : 0;
            return n;
        }

        // Estira una pieza del kit al rectángulo [a, b] x [ya, yb] de la cara, con 'fondo' metros de grueso hacia la calle.
        private static bool Pieza(Transform padre, string nombre, bool ejeX, Vector3 dir, float cara, Quaternion giro, float a, float b, float ya, float yb, float fondo)
        {
            GameObject prefab = Prefab(nombre);
            if (prefab == null || b - a < 0.3f || yb - ya < 0.2f)
            {
                return false;
            }
            GameObject g = (GameObject)PrefabUtility.InstantiatePrefab(prefab, padre);
            g.name = nombre;
            foreach (Collider col in g.GetComponentsInChildren<Collider>())
            {
                Object.DestroyImmediate(col);
            }
            g.transform.rotation = Quaternion.identity;
            Bounds l = ArteT250.Limites(g);   // sin giro: ancho en X, alto en Y, fondo en Z
            Vector3 s = g.transform.localScale;
            g.transform.localScale = new Vector3(s.x * (b - a) / Mathf.Max(l.size.x, 0.01f), s.y * (yb - ya) / Mathf.Max(l.size.y, 0.01f), s.z * fondo / Mathf.Max(l.size.z, 0.01f));
            g.transform.rotation = giro;
            Bounds w = ArteT250.Limites(g);
            float medio = (a + b) * 0.5f;
            Vector3 mover;
            if (ejeX)
            {
                float atras = dir.z > 0f ? w.min.z : w.max.z;
                mover = new Vector3(medio - w.center.x, ya - w.min.y, cara + dir.z * 0.02f - atras);
            }
            else
            {
                float atras = dir.x > 0f ? w.min.x : w.max.x;
                mover = new Vector3(cara + dir.x * 0.02f - atras, ya - w.min.y, medio - w.center.z);
            }
            g.transform.position += mover;
            foreach (Renderer r in g.GetComponentsInChildren<Renderer>())
            {
                r.shadowCastingMode = ShadowCastingMode.Off;
            }
            GameObjectUtility.SetStaticEditorFlags(g, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic);
            return true;
        }

        internal static int Azotea(Transform kit, Bounds b, System.Random azar)
        {
            int n = 0;
            string[] cosas = { "C09_Tinaco", "C09_Tinaco", "C10_AireAcondicionado", "CY04_AntenaAzotea" };
            int cuantas = 2 + azar.Next(3);
            for (int i = 0; i < cuantas; i++)
            {
                string cosa = cosas[azar.Next(cosas.Length)];
                Vector3 p = new Vector3(Mathf.Lerp(b.min.x + 1.5f, b.max.x - 1.5f, (float)azar.NextDouble()), 0f, Mathf.Lerp(b.min.z + 1.5f, b.max.z - 1.5f, (float)azar.NextDouble()));
                GameObject g = ColocarS2Interiores.Modelo(kit, C + cosa, p, azar.Next(4) * 90f, b.max.y, 1f, sinCollider: true);
                if (g != null)
                {
                    foreach (Renderer r in g.GetComponentsInChildren<Renderer>())
                    {
                        r.shadowCastingMode = ShadowCastingMode.Off;
                    }
                    n++;
                }
            }
            return n;
        }

        // Cables de luz cruzando la calle entre edificios de enfrente, cada 2 edificios, a 6-9 m de alto.
        private static int Cables(Transform grupo, List<(Vector3 punto, Vector3 dir)> anclas, bool ejeX, Bounds calle)
        {
            Material negro = ColocarT550.Mat("S2_Cable", new Color(0.04f, 0.04f, 0.045f));
            List<(Vector3 punto, Vector3 dir)> lado1 = anclas.Where(a => (ejeX ? a.dir.z : a.dir.x) > 0f).OrderBy(a => ejeX ? a.punto.x : a.punto.z).ToList();
            List<(Vector3 punto, Vector3 dir)> lado2 = anclas.Where(a => (ejeX ? a.dir.z : a.dir.x) < 0f).OrderBy(a => ejeX ? a.punto.x : a.punto.z).ToList();
            int n = 0;
            for (int i = 0; i < lado1.Count; i += 2)
            {
                Vector3 p1 = lado1[i].punto;
                var otro = lado2.OrderBy(a => Mathf.Abs((ejeX ? a.punto.x - p1.x : a.punto.z - p1.z))).FirstOrDefault();
                if (otro.dir == Vector3.zero)
                {
                    continue;
                }
                for (int k = 0; k < 2; k++)
                {
                    float alto = 6.5f + k * 1.3f + (i % 3) * 0.4f;
                    Vector3 a = p1 + Vector3.up * alto;
                    Vector3 b = otro.punto + Vector3.up * (alto + 0.3f);
                    GameObject go = new GameObject("Cable");
                    go.transform.SetParent(grupo, false);
                    LineRenderer lr = go.AddComponent<LineRenderer>();
                    const int Tramos = 14;
                    lr.positionCount = Tramos + 1;
                    for (int t = 0; t <= Tramos; t++)
                    {
                        float u = (float)t / Tramos;
                        lr.SetPosition(t, Vector3.Lerp(a, b, u) + Vector3.down * Mathf.Sin(u * Mathf.PI) * 0.9f);
                    }
                    lr.widthMultiplier = 0.035f;
                    lr.sharedMaterial = negro;
                    lr.shadowCastingMode = ShadowCastingMode.Off;
                    n++;
                }
            }
            return n;
        }

        private static GameObject Prefab(string nombre)
        {
            if (!Prefabs.TryGetValue(nombre, out GameObject p))
            {
                p = AssetDatabase.LoadAssetAtPath<GameObject>(C + nombre + ".prefab");
                Prefabs[nombre] = p;
            }
            return p;
        }
    }
}
