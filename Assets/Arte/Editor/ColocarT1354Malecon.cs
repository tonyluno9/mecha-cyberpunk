using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using MechaCyberpunk.Core.Mundo;
using MechaCyberpunk.Mundo.Reloj;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-1354: el Malecón, borde sur de la ciudad (referencias K11, K12 y A11). Avenida costera de 2 carriles por sentido en curva abierta, banqueta de 8 m con
    /// barandal, palmeras y faroles alternados, escaleras a la playa, playa de arena, escollera, muelle con lanchas y puesto de mariscos, faro, fachadas de
    /// 3 a 5 pisos con neón, postes con cables, charcos y el mar (shader de T-1351 por reflexión). Las piezas de Blender (ZonasModulos) y de Tripo
    /// (Assets/Arte/Zonas) que no existan se dejan como "Hueco_CODIGO" vacío y se anotan en el informe y en docs/assets-faltan.md. Idempotente: rehace su raíz.
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ColocarT1354Malecon.Aplicar (y .Fotos, que no guarda).
    /// </summary>
    public static class ColocarT1354Malecon
    {
        public const string NombreRaiz = "T1354_Malecon";
        public const int Semilla = 1354;
        public const float PasoPaseo = 12f;
        public const float PasoEscaleras = 70f;
        private const string CarpetaModulos = "Assets/Arte/ZonasModulos/Prefabs/";
        private const string CarpetaTripo = "Assets/Arte/Zonas";
        private const float XOesteZona = -330f;
        private const float XEsteZona = 175f;
        private const float AnchoUnion = 14f;
        private const float MitadBocaCorredor = 10f;
        private const float XMuelle = 92f;
        private const float XPuesto = 78f;
        private const float YMuelle = -1f;
        private const float LateralBarandal = 15.6f;
        private const float LateralPaseo = 12f;
        private const float LateralBanca = 14.4f;
        private const float AlturaPoste = 6.75f;
        /// <summary>T-1363: los faroles del Malecón alumbran más que los de la ciudad (charcos de sodio en el paseo y el asfalto).</summary>
        private const float FuerzaFarolMalecon = 1.6f;
        private const float EscalaPuesto = 1.8f;

        // Intervalos de x donde hay fachadas (se saltan las bocas de los corredores x = ±47 y el Puerto, que empieza en x = 115).
        private static readonly (float x0, float x1)[] Fachadas = { (-295f, -57f), (-37f, 37f), (57f, 110f) };
        private static readonly Color[] ColoresLancha =
        {
            new Color(0.8f, 0.15f, 0.12f), new Color(0.15f, 0.55f, 0.3f), new Color(0.15f, 0.4f, 0.75f), new Color(0.9f, 0.75f, 0.15f), new Color(0.1f, 0.75f, 0.7f),
        };

        private static readonly HashSet<string> faltan = new HashSet<string>();
        private static readonly Dictionary<string, Bounds> medidas = new Dictionary<string, Bounds>();
        private static readonly Dictionary<string, GameObject> prefabs = new Dictionary<string, GameObject>();

        // ================================================================ Entradas

        [MenuItem("Mecha Cyberpunk/T-1354/Malecon")]
        public static void Aplicar()
        {
            ColocarS2.Abrir();
            ColocarS2.Informe.Clear();
            faltan.Clear();
            medidas.Clear();
            prefabs.Clear();
            KitS2c.Obra o = KitS2c.Empezar(NombreRaiz, Semilla);
            Transform piso = Hijo(o.Raiz, "Piso");
            Transform calles = Hijo(o.Raiz, "Calles");
            Transform paseo = Hijo(o.Raiz, "Paseo");
            Transform costa = Hijo(o.Raiz, "Costa");
            Transform muelle = Hijo(o.Raiz, "Muelle");
            Transform casas = Hijo(o.Raiz, "Fachadas");
            Transform postes = Hijo(o.Raiz, "Postes");
            Transform mar = Hijo(o.Raiz, "Mar");
            Transform orillaLejana = Hijo(o.Raiz, "OtraOrilla");
            Transform luzLocal = Hijo(o.Raiz, "LuzLocal");

            List<Vector3> avenida = ColocarT1354Curva.Avenida();
            List<Vector3> borde = ColocarT1354Curva.BordeMar(XOesteZona, XEsteZona);

            Terreno(o, piso, calles, avenida, borde);
            Rayas(o, piso, avenida);
            Paseo(o, paseo, borde);
            Escolleras(o, costa, borde);
            Muelle(o, muelle, paseo, borde);
            List<Vector3> altos = FachadasCiudad(o, casas);
            Postes(o, postes, avenida, altos);
            Mar(o, mar, borde);
            OtraOrilla(o, orillaLejana);
            VolumenLocal(luzLocal);
            ApagarRellenoT530();
            Sondas(o.Raiz);

            Estaticos(o.Raiz);
            LodDeCasas(casas);
            LodDeTorres(orillaLejana);
            KitS2c.Terminar(o);
            ColocarS2.Informe.AppendLine("T-1354 Malecón: " + o.Casas + " casas, " + o.Neones + " neones, " + o.Postes + " postes, " + o.Cables + " cables, " + o.Charcos + " charcos");
            ColocarS2.Informe.AppendLine("Luces reales: " + o.Raiz.GetComponentsInChildren<Light>(true).Length);
            ColocarS2.Informe.AppendLine("Radio mínimo de la avenida: " + ColocarT1354Curva.RadioMinimo(avenida).ToString("F0") + " m");
            ColocarS2.Informe.AppendLine("Huecos (faltan assets): " + (faltan.Count == 0 ? "ninguno" : string.Join(", ", faltan.OrderBy(f => f))));
            ColocarS2.Guardar("t1354-malecon");
        }

        [MenuItem("Mecha Cyberpunk/T-1354/Fotos del Malecon")]
        public static void Fotos()
        {
            ColocarS2.Abrir();
            const string referencias = "imageassets/referencias/look/";
            (string nombre, string referencia, Vector3 desde, Vector3 hacia, float campo)[] tomas =
            {
                // K11: a 1.75 m, a ~3 m del barandal hacia la avenida (lateral 12.6), mirando al oeste por la tangente con ~8 grados hacia la ciudad.
                ("k11", "K11_Malecon.png", new Vector3(40f, 1.9f, -235.6f), new Vector3(-59f, 1.9f, -221.7f), 60f),
                // K12: mirando al este desde el oeste del puesto (x = 78, a ~8 m): puesto y muelle a la derecha (el mar queda a la derecha), faro al fondo.
                ("k12", "K12_Malecon_Muelle.png", new Vector3(70f, 2.2f, -236f), new Vector3(149f, 2.2f, -250f), 75f),
                // A11: aérea a ~40 m, del lado de la ciudad al oeste del muelle, mirando al suroeste: playa, escollera y la curva.
                ("a11", "A11_Malecon_Aerea.png", new Vector3(-20f, 40f, -200f), new Vector3(-95f, 0f, -250f), 70f),
            };
            Directory.CreateDirectory("Logs");
            foreach (var t in tomas)
            {
                string juego = "Logs/t1354-" + t.nombre + ".png";
                ColocarS2cLook.Foto(DejarLibre(t.desde), t.hacia, juego, t.campo);
                ColocarS2.LadoALado(referencias + t.referencia, juego, "docs/capturas/zonas/malecon-" + t.nombre + ".png");
            }
        }

        /// <summary>Sube la cámara de a 10 m (hasta 6 veces) si quedó dentro de un collider (una grúa, un edificio).</summary>
        private static Vector3 DejarLibre(Vector3 desde)
        {
            Physics.SyncTransforms();
            for (int i = 0; i < 6 && Physics.CheckSphere(desde, 1.5f, ~0, QueryTriggerInteraction.Ignore); i++) // el volumen de luz y las zonas de tráfico son triggers
            {
                desde += Vector3.up * 10f;
            }
            return desde;
        }

        // ================================================================ Terreno: calles, banquetas, playa

        private static void Terreno(KitS2c.Obra o, Transform piso, Transform calles, List<Vector3> avenida, List<Vector3> borde)
        {
            Material asfalto = KitS2c.MatAsfalto();
            // Banqueta del kit, más oscura y gastada, con losetas de 1 m (no de cuadros grandes) y charcos encima.
            Material banqueta = KitS2c.Mat("T1354_Banqueta", () =>
            {
                Material m = new Material(KitS2c.MatBanqueta());
                m.SetColor("_BaseColor", new Color(0.36f, 0.34f, 0.34f));
                m.SetFloat("_Smoothness", 0.3f);
                return m;
            });
            Material arena = KitS2c.Mat("T1354_ArenaClara", () => KitS2c.Lit(new Color(0.42f, 0.36f, 0.28f), 0.15f));
            Material fondo = KitS2c.Mat("T1354_FondoMar", () => KitS2c.Lit(new Color(0.03f, 0.05f, 0.08f), 0.1f));
            const float h = ColocarT1354Curva.YBanqueta;
            const float a = ColocarT1354Curva.YArroyo;

            // Avenida: 16 m de arroyo, banqueta angosta del lado de la ciudad (en tramos: las bocas de U1 y U2 quedan abiertas) y ancha del lado del mar.
            Franja(o, piso, "Avenida", avenida, i => new[] { (-8f, a), (8f, a) }, asfalto, 8f, true);
            foreach ((float x0, float x1) in new[] { (-300f, -57f), (-37f, 37f), (57f, 160f) })
            {
                Franja(o, piso, "BanquetaCiudad", ColocarT1354Curva.PuntosEn(x0, x1, ColocarT1354Curva.PasoX), i => new[] { (-11f, h), (-8f, h), (-8f, a) }, banqueta, 1f, true);
            }
            Franja(o, piso, "BanquetaMar", borde, i => new[] { (8f, a), (8f, h), (ColocarT1354Curva.LateralMuro, h), (ColocarT1354Curva.LateralMuro, ColocarT1354Curva.YPiePlaya) }, banqueta, 1f, true);

            // Playa de arena: del pie del muro a la orilla, que cae en z = -262 donde empieza el agua, y sigue bajando bajo el agua.
            Franja(o, piso, "Playa", borde, i =>
            {
                float s = Mathf.Max(18f, ColocarT1354Curva.LateralOrilla(borde[i].z));
                return new[] { (ColocarT1354Curva.LateralMuro, ColocarT1354Curva.YPiePlaya), (s, ColocarT1354Curva.YAgua), (s + 10f, -5f), (s + 20f, ColocarT1354Curva.YFondoPlaya) };
            }, arena, 6f, true);
            KitS2c.Losa(o, piso, "FondoMar", new Vector3(0f, ColocarT1354Curva.YFondoPlaya - 0.4f, -581f), Quaternion.identity, new Vector3(1400f, 0.2f, 638f), fondo, 20f, false);

            // Uniones: U1 y U2 alargan los corredores sur hasta la avenida; U3 sube a la base del cerro. Cada calle lleva su CalleZona.
            CalleZonaDe(calles, "CalleZona_Avenida", avenida, ColocarT1354Curva.AnchoAvenida);
            foreach ((string nombre, Vector3 punto) in new[] { ("U1", ColocarT1354Curva.U1), ("U2", ColocarT1354Curva.U2) })
            {
                List<Vector3> union = ColocarT1354Curva.Union(punto);
                CalleZonaDe(calles, "CalleZona_" + nombre, union, AnchoUnion);
                // El arroyo y las banquetas llegan solo hasta el borde norte de la avenida (z = -218).
                List<Vector3> hasta = new List<Vector3> { union[0], new Vector3(punto.x, 0f, punto.z + 8f) };
                Franja(o, piso, "Arroyo_" + nombre, hasta, i => new[] { (-7f, a), (7f, a) }, asfalto, 8f, true);
                Franja(o, piso, "BanquetaA_" + nombre, hasta, i => new[] { (-10f, h), (-7f, h), (-7f, a) }, banqueta, 1f, true);
                Franja(o, piso, "BanquetaB_" + nombre, hasta, i => new[] { (7f, a), (7f, h), (10f, h) }, banqueta, 1f, true);
            }
            List<Vector3> u3 = ColocarT1354Curva.U3();
            CalleZonaDe(calles, "CalleZona_U3", u3, AnchoUnion);
            Franja(o, piso, "Arroyo_U3", u3, i => new[] { (-7f, a), (7f, a) }, asfalto, 8f, true);
            Franja(o, piso, "BanquetaA_U3", u3, i => new[] { (-10f, h), (-7f, h), (-7f, a) }, banqueta, 1f, true);
            Franja(o, piso, "BanquetaB_U3", u3, i => new[] { (7f, a), (7f, h), (10f, h) }, banqueta, 1f, true);
        }

        private static void CalleZonaDe(Transform padre, string nombre, List<Vector3> puntos, float ancho)
        {
            GameObject go = new GameObject(nombre);
            go.transform.SetParent(padre, false);
            go.AddComponent<CalleZona>().Definir(puntos, ancho, true, false, "Malecon");
        }

        /// <summary>
        /// Una franja de terreno con perfil: a lo largo de <paramref name="centro"/> y, de lado a lado, los puntos (lateral hacia el mar, y) de <paramref name="perfil"/>
        /// (de la ciudad al mar). La cara de cada tramo mira hacia arriba o hacia el mar; el collider es la propia malla.
        /// </summary>
        private static GameObject Franja(KitS2c.Obra o, Transform padre, string nombre, IList<Vector3> centro, Func<int, (float lateral, float y)[]> perfil, Material material, float metrosUv, bool collider)
        {
            int n = centro.Count;
            Vector3[] derecha = new Vector3[n];
            float[] distancia = new float[n];
            for (int i = 0; i < n; i++)
            {
                Vector3 t = centro[Mathf.Min(i + 1, n - 1)] - centro[Mathf.Max(i - 1, 0)];
                t.y = 0f;
                derecha[i] = ColocarT1354Curva.HaciaElMar(t.normalized);
                distancia[i] = i == 0 ? 0f : distancia[i - 1] + Vector3.Distance(centro[i - 1], centro[i]);
            }
            Vector3 En(int i, (float lateral, float y) p) => new Vector3(centro[i].x, 0f, centro[i].z) + derecha[i] * p.lateral + Vector3.up * p.y;
            List<Vector3> v = new List<Vector3>();
            List<Vector3> nr = new List<Vector3>();
            List<Vector2> uv = new List<Vector2>();
            List<int> tri = new List<int>();
            for (int i = 0; i + 1 < n; i++)
            {
                (float lateral, float y)[] pa = perfil(i);
                (float lateral, float y)[] pb = perfil(i + 1);
                float acumulado = 0f;
                Vector3 derechaMedia = (derecha[i] + derecha[i + 1]).normalized;
                for (int j = 0; j + 1 < pa.Length; j++)
                {
                    float dl = pa[j + 1].lateral - pa[j].lateral;
                    float dy = pa[j + 1].y - pa[j].y;
                    float largo = Mathf.Sqrt(dl * dl + dy * dy);
                    if (largo < 1e-4f)
                    {
                        continue;
                    }
                    Vector3 normal = (derechaMedia * (-dy / largo) + Vector3.up * (dl / largo)).normalized;
                    Vector3 p0 = En(i, pa[j]), p1 = En(i + 1, pb[j]), p2 = En(i + 1, pb[j + 1]), p3 = En(i, pa[j + 1]);
                    int k = v.Count;
                    v.Add(p0); v.Add(p1); v.Add(p2); v.Add(p3);
                    for (int q = 0; q < 4; q++)
                    {
                        nr.Add(normal);
                    }
                    uv.Add(new Vector2(distancia[i] / metrosUv, acumulado / metrosUv));
                    uv.Add(new Vector2(distancia[i + 1] / metrosUv, acumulado / metrosUv));
                    uv.Add(new Vector2(distancia[i + 1] / metrosUv, (acumulado + largo) / metrosUv));
                    uv.Add(new Vector2(distancia[i] / metrosUv, (acumulado + largo) / metrosUv));
                    // Frente de un triángulo: cross(b - a, c - a) apunta hacia quien lo ve.
                    bool alReves = Vector3.Dot(Vector3.Cross(p1 - p0, p2 - p0), normal) < 0f;
                    tri.AddRange(alReves ? new[] { k, k + 2, k + 1, k, k + 3, k + 2 } : new[] { k, k + 1, k + 2, k, k + 2, k + 3 });
                    acumulado += largo;
                }
            }
            Mesh m = new Mesh { name = o.Nombre + "_" + nombre + "_" + o.Mallas.Count, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            m.SetVertices(v);
            m.SetNormals(nr);
            m.SetUVs(0, uv);
            m.SetTriangles(tri, 0);
            m.RecalculateTangents();
            m.RecalculateBounds();
            o.Mallas.Add(m);
            GameObject go = new GameObject(nombre);
            go.transform.SetParent(padre, false);
            go.AddComponent<MeshFilter>().sharedMesh = m;
            go.AddComponent<MeshRenderer>().sharedMaterial = material;
            if (collider)
            {
                go.AddComponent<MeshCollider>().sharedMesh = m;
            }
            return go;
        }

        // ================================================================ Rayas y charcos

        private static void Rayas(KitS2c.Obra o, Transform piso, List<Vector3> avenida)
        {
            const float a = ColocarT1354Curva.YArroyo;
            float largo = ColocarT1354Curva.Longitud(avenida);
            // Centro: doble línea continua; orillas: línea continua; entre carriles: rayas de 1.6 m cada 4 m.
            foreach (float lateral in new[] { -0.2f, 0.2f, -7.7f, 7.7f })
            {
                for (int i = 0; i + 1 < avenida.Count; i++)
                {
                    Vector3 t = (avenida[i + 1] - avenida[i]).normalized;
                    Vector3 d = ColocarT1354Curva.HaciaElMar(t) * lateral;
                    Vector3 p = avenida[i] + d;
                    Vector3 q = avenida[i + 1] + d;
                    p.y = a;
                    q.y = a;
                    KitS2c.Linea(piso, p, q, 0.12f, a);
                }
            }
            foreach (float lateral in new[] { -4f, 4f })
            {
                for (float d = 1f; d + 1.6f < largo; d += 4f)
                {
                    Vector3 p = ColocarT1354Curva.PuntoEn(avenida, d, out Vector3 t) + ColocarT1354Curva.HaciaElMar(t) * lateral;
                    Vector3 q = ColocarT1354Curva.PuntoEn(avenida, d + 1.6f, out Vector3 t2) + ColocarT1354Curva.HaciaElMar(t2) * lateral;
                    p.y = a;
                    q.y = a;
                    KitS2c.Linea(piso, p, q, 0.12f, a);
                }
            }
            // Charcos con reflejo en el asfalto y en las banquetas (K11).
            foreach ((int cuantos, float desde, float hasta, float y) in new[] { (34, -6.5f, 6.5f, a), (26, 9f, 15f, ColocarT1354Curva.YBanqueta), (10, -10.5f, -8.5f, ColocarT1354Curva.YBanqueta) })
            {
                for (int i = 0; i < cuantos; i++)
                {
                    Vector3 p = ColocarT1354Curva.PuntoEn(avenida, o.Rango(4f, largo - 4f), out Vector3 t);
                    p += ColocarT1354Curva.HaciaElMar(t) * o.Rango(desde, hasta);
                    p.y = y;
                    KitS2c.Charco(o, piso, p, o.Rango(1.4f, 4.2f), o.Rango(0.9f, 2.4f), o.Rango(0f, 180f));
                }
            }
        }

        // ================================================================ Paseo: barandal, palmeras, faroles, bancas, escaleras

        private static void Paseo(KitS2c.Obra o, Transform paseo, List<Vector3> borde)
        {
            float largo = ColocarT1354Curva.Longitud(borde);
            float dMuelle = DistanciaEnX(borde, XMuelle);
            float dPuesto = DistanciaEnX(borde, XPuesto);
            float dEspigon = DistanciaEnX(borde, ColocarT1354Curva.U5.x + 8f);
            List<float> escaleras = ColocarT1354Curva.Cada(largo, PasoEscaleras, 40f);
            escaleras.Add(dMuelle);
            escaleras.Add(dEspigon);
            // Las de la playa no se amontonan con las del muelle y el espigón (que bajan a una cubierta, no a la arena).
            escaleras.RemoveAll(d => d != dMuelle && d != dEspigon && (Mathf.Abs(d - dMuelle) < 10f || Mathf.Abs(d - dEspigon) < 10f));
            float anchoEscalera = Medida(Modulo("ML02_EscaleraPlaya")).size.x;
            if (anchoEscalera < 1f)
            {
                anchoEscalera = 4f;
            }

            // Barandal de concreto por el borde del muro, con hueco en las escaleras.
            Bounds bb = Medida(Modulo("ML01_BarandalMalecon"));
            float paso = bb.size.x > 1f ? bb.size.x : 4f;
            int cuantos = 0;
            for (float d = paso * 0.5f; d < largo; d += paso)
            {
                if (escaleras.Any(e => Mathf.Abs(d - e) < anchoEscalera * 0.5f + paso * 0.5f))
                {
                    continue;
                }
                Vector3 p = Punto(borde, d, LateralBarandal, out Quaternion giro);
                Poner(paseo, "ML01_BarandalMalecon", true, p, giro, 1f, null, cuantos == 0);
                cuantos++;
            }

            // Escaleras: la cima al ras de la banqueta (y = 0.2) y el pie en la arena (o en la cubierta del muelle y del espigón del faro).
            foreach (float d in escaleras)
            {
                Vector3 p = Punto(borde, d, ColocarT1354Curva.LateralMuro + 1.5f, out Quaternion giro);
                bool aCubierta = d == dMuelle || d == dEspigon;
                float yPie = aCubierta ? YMuelle : YPlaya(ColocarT1354Curva.LateralMuro + 3.5f, ColocarT1354Curva.LateralOrilla(p.z));
                Escalera(paseo, p, giro * Vector3.forward, ColocarT1354Curva.YBanqueta, yPie, "d=" + d.ToString("F0") + (aCubierta ? " a cubierta" : " a playa"));
            }

            // Palmeras y faroles alternados cada 12 m (Tripo); la zona del puesto y las escaleras quedan libres.
            foreach ((float d, bool palmera) in ColocarT1354Curva.Alternados(largo, PasoPaseo, 6f))
            {
                if (escaleras.Any(e => Mathf.Abs(d - e) < 4f) || Mathf.Abs(d - dPuesto) < 8f || Mathf.Abs(d - dMuelle) < 8f)
                {
                    continue;
                }
                Vector3 p = Punto(borde, d, LateralPaseo, out Quaternion giro);
                if (EnBocaDeUnion(p.x))
                {
                    continue;
                }
                if (palmera)
                {
                    Poner(paseo, "AR03_Palmera", false, p, Quaternion.Euler(0f, o.Rango(0f, 360f), 0f), o.Rango(0.9f, 1.15f), null, true);
                }
                else
                {
                    GameObject farol = Poner(paseo, "ML01_Farol", false, p, giro, 1f, null, true);
                    if (farol != null)
                    {
                        // Sin Light real: el halo cálido hace de luz de sodio.
                        Bounds fb = Medida(prefabs["ML01_Farol"]);
                        Vector3 cabeza = p + Vector3.up * fb.size.y * 0.92f;
                        // Luz de sodio como la de los postes de la ciudad (ColocarS2oLuz): spot hacia abajo, y el halo en la niebla.
                        KitS2c.Luz(farol.transform, cabeza, LightType.Spot, KitS2c.Sodio, ColocarS2oLuz.IntensidadPoste * FuerzaFarolMalecon, ColocarS2oLuz.RangoPoste * 1.25f, ColocarS2oLuz.AnguloPoste, (giro * Vector3.forward * 0.6f + Vector3.down).normalized);
                        KitS2c.Halo(o, farol.transform, cabeza, ColocarT1354Curva.YBanqueta, KitS2c.Sodio);
                    }
                }
            }

            // Bancas mirando a la avenida, entre las escaleras.
            foreach (float d in ColocarT1354Curva.Cada(largo, 36f, 20f))
            {
                if (escaleras.Any(e => Mathf.Abs(d - e) < 6f) || Mathf.Abs(d - dPuesto) < 10f || Mathf.Abs(d - dMuelle) < 10f)
                {
                    continue;
                }
                Vector3 p = Punto(borde, d, LateralBanca, out Quaternion giro);
                if (EnBocaDeUnion(p.x))
                {
                    continue;
                }
                Poner(paseo, "PZ01_Banca", false, p, Quaternion.LookRotation(-(giro * Vector3.forward)), 1f, null, true);
            }
        }

        /// <summary>Altura (y) más alta donde un rayo vertical en (x, z) del punto toca un collider de <paramref name="go"/>; -infinito si no toca.</summary>
        private static float AlturaDe(GameObject go, Vector3 punto)
        {
            float mejor = float.NegativeInfinity;
            foreach (RaycastHit h in Physics.RaycastAll(punto + Vector3.up * 12f, Vector3.down, 30f, ~0, QueryTriggerInteraction.Ignore))
            {
                if (h.collider.transform.IsChildOf(go.transform))
                {
                    mejor = Mathf.Max(mejor, h.point.y);
                }
            }
            return mejor;
        }

        /// <summary>
        /// T-1363: una ML02_EscaleraPlaya (pivote en la base, al centro; 4 x 2.9 x 4 con su caja "Rampa") con la cima al ras de <paramref name="yTope"/> y el pie en
        /// <paramref name="yPie"/>: se gira hasta que baje hacia el mar (por raycasts a lo largo de su eje), se escala en Y y se sube o baja. La medida final de la cima
        /// y del pie (raycasts) queda en el informe.
        /// </summary>
        private static GameObject Escalera(Transform padre, Vector3 centro, Vector3 haciaMar, float yTope, float yPie, string etiqueta)
        {
            GameObject prefab = Modulo("ML02_EscaleraPlaya");
            if (prefab == null)
            {
                faltan.Add("ML02_EscaleraPlaya");
                GameObject h = new GameObject("Hueco_ML02_EscaleraPlaya");
                h.transform.SetParent(padre, false);
                h.transform.position = centro;
                return null;
            }
            haciaMar.y = 0f;
            haciaMar.Normalize();
            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, padre);
            go.transform.rotation = Quaternion.LookRotation(haciaMar);
            go.transform.localScale = Vector3.one;
            go.transform.position = new Vector3(centro.x, 0f, centro.z);
            Vector3 xz = new Vector3(centro.x, 0f, centro.z);
            const float semiLargo = 2f;
            const float inset = 0.4f;
            float hCiudad = 0f, hMar = 0f;
            bool bajaAlMar = false;
            for (int k = 0; k < 4 && !bajaAlMar; k++)
            {
                Physics.SyncTransforms();
                Vector3 dir = go.transform.forward;
                hCiudad = AlturaDe(go, xz - dir * (semiLargo - inset));
                hMar = AlturaDe(go, xz + dir * (semiLargo - inset));
                bajaAlMar = hCiudad > float.NegativeInfinity && hMar > float.NegativeInfinity && hCiudad - hMar > 0.3f;
                if (!bajaAlMar)
                {
                    go.transform.RotateAround(xz, Vector3.up, 90f);
                }
            }
            if (!bajaAlMar)
            {
                // Geometría conocida de la pieza de T-1353 (Assets/Arte/ZonasModulos/Prefabs/ML02_EscaleraPlaya.prefab): pivote en la base, 2.9 m de alto,
                // la caja "Rampa" sube hacia su +Z local. La cima (+Z) mira a la ciudad, se escala en Y al desnivel y la base queda en el pie.
                Bounds b = Medida(prefab);
                go.transform.rotation = Quaternion.LookRotation(-haciaMar);
                go.transform.localScale = new Vector3(1f, (yTope - yPie) / Mathf.Max(0.5f, b.size.y), 1f);
                go.transform.position = new Vector3(centro.x, yPie - b.min.y * go.transform.localScale.y, centro.z);
                Physics.SyncTransforms();
                ColocarS2.Informe.AppendLine("ESCALERA " + etiqueta + ": colocada por su geometría (cima +Z hacia la ciudad), alto " + (yTope - yPie).ToString("F2") + " m");
                return go;
            }
            // La pendiente medida en el eje (a 1.6 m del centro) se extiende a los 2 m de cada extremo.
            float extension = semiLargo / (semiLargo - inset);
            float alturaTotal = (hCiudad - hMar) * extension;
            go.transform.localScale = new Vector3(1f, (yTope - yPie) / alturaTotal, 1f);
            Physics.SyncTransforms();
            Vector3 d2 = go.transform.forward;
            float c2 = AlturaDe(go, xz - d2 * (semiLargo - inset));
            float m2 = AlturaDe(go, xz + d2 * (semiLargo - inset));
            float cimaExtrapolada = c2 + (c2 - m2) * (inset / (2f * (semiLargo - inset)));
            go.transform.position += Vector3.up * (yTope - cimaExtrapolada);
            Physics.SyncTransforms();
            float cima = AlturaDe(go, xz - d2 * (semiLargo - 0.05f));
            float pie = AlturaDe(go, xz + d2 * (semiLargo - 0.05f));
            ColocarS2.Informe.AppendLine("Escalera " + etiqueta + ": cima " + cima.ToString("F2") + " (pedida " + yTope.ToString("F2") + "), pie " + pie.ToString("F2") + " (pedido " + yPie.ToString("F2") + ")"
                + (Mathf.Abs(cima - yTope) > 0.15f || Mathf.Abs(pie - yPie) > 0.3f ? "  <-- REVISAR" : ""));
            return go;
        }

        /// <summary>
        /// Sube o baja una pieza de cubierta (muelle) hasta que su piso caminable (raycast en el centro de su caja) quede en <paramref name="y"/>: las piezas se
        /// ponían por la parte más alta de su caja visible (postes), y el piso quedaba más abajo.
        /// </summary>
        private static void NivelarCubierta(GameObject go, float y)
        {
            if (go == null)
            {
                return;
            }
            Physics.SyncTransforms();
            Renderer[] rs = go.GetComponentsInChildren<Renderer>();
            Bounds b = rs.Length == 0 ? new Bounds(go.transform.position, Vector3.zero) : rs[0].bounds;
            foreach (Renderer r in rs)
            {
                b.Encapsulate(r.bounds);
            }
            float h = AlturaDe(go, b.center);
            if (h == float.NegativeInfinity)
            {
                ColocarS2.Informe.AppendLine("SIN PISO CAMINABLE en " + go.name + " (" + b.center.ToString("F1") + "): la pieza no tiene collider bajo su centro");
                faltan.Add("Piso caminable en " + go.name);
                return;
            }
            go.transform.position += Vector3.up * (y - h);
            Physics.SyncTransforms();
        }

        /// <summary>Punto del paseo a una distancia y un lateral (hacia el mar), a la altura de la banqueta; <paramref name="giro"/> mira al mar.</summary>
        private static Vector3 Punto(IList<Vector3> linea, float distancia, float lateral, out Quaternion giro)
        {
            Vector3 p = ColocarT1354Curva.PuntoEn(linea, distancia, out Vector3 t);
            Vector3 mar = ColocarT1354Curva.HaciaElMar(t);
            giro = Quaternion.LookRotation(mar);
            p += mar * lateral;
            p.y = ColocarT1354Curva.YBanqueta;
            return p;
        }

        /// <summary>Los cruces de U1 y U2 (x = ±47) quedan libres: 16 m de ancho sin farol, palmera ni banca.</summary>
        private static bool EnBocaDeUnion(float x)
        {
            return Mathf.Abs(Mathf.Abs(x) - 47f) < 8f;
        }

        private static float DistanciaEnX(IList<Vector3> linea, float x)
        {
            float d = 0f;
            for (int i = 0; i + 1 < linea.Count; i++)
            {
                float tramo = Vector3.Distance(linea[i], linea[i + 1]);
                if (linea[i + 1].x >= x)
                {
                    float dx = linea[i + 1].x - linea[i].x;
                    return d + (dx < 1e-4f ? 0f : Mathf.Clamp01((x - linea[i].x) / dx) * tramo);
                }
                d += tramo;
            }
            return d;
        }

        // ================================================================ Escolleras y faro

        private static float YPlaya(float lateral, float orilla)
        {
            if (lateral <= ColocarT1354Curva.LateralMuro)
            {
                return ColocarT1354Curva.YPiePlaya;
            }
            if (lateral <= orilla)
            {
                return Mathf.Lerp(ColocarT1354Curva.YPiePlaya, ColocarT1354Curva.YAgua, (lateral - ColocarT1354Curva.LateralMuro) / (orilla - ColocarT1354Curva.LateralMuro));
            }
            if (lateral <= orilla + 10f)
            {
                return Mathf.Lerp(ColocarT1354Curva.YAgua, -5f, (lateral - orilla) / 10f);
            }
            if (lateral <= orilla + 20f)
            {
                return Mathf.Lerp(-5f, ColocarT1354Curva.YFondoPlaya, (lateral - orilla - 10f) / 10f);
            }
            return ColocarT1354Curva.YFondoPlaya;
        }

        private static void Escolleras(KitS2c.Obra o, Transform costa, List<Vector3> borde)
        {
            Bounds roca = Medida(Modulo("ML05_Escollera"));
            float ancho = roca.size.x > 0.5f ? roca.size.x : 4f;
            float alto = roca.size.y > 0.3f ? roca.size.y : 2f;

            // Tramo de rocas contra la orilla (como A11), del oeste.
            float d0 = DistanciaEnX(borde, -250f);
            float d1 = DistanciaEnX(borde, -170f);
            for (float d = d0; d < d1; d += ancho * 0.8f)
            {
                Vector3 c = ColocarT1354Curva.PuntoEn(borde, d, out Vector3 t);
                Vector3 mar = ColocarT1354Curva.HaciaElMar(t);
                float orilla = ColocarT1354Curva.LateralOrilla(c.z);
                foreach (float lateral in new[] { orilla - 2.5f, orilla - 0.5f })
                {
                    Vector3 p = c + mar * lateral;
                    p.y = YPlaya(lateral, orilla) - 0.4f;
                    Poner(costa, "ML05_Escollera", true, p, Quaternion.Euler(0f, o.Rango(0f, 360f), 0f), o.Rango(0.9f, 1.3f), null, d == d0);
                }
            }

            // Espigón que sale al mar al final este: rocas apiladas hasta pasar la orilla y, en la punta, un montículo con el faro encima.
            float x = ColocarT1354Curva.U5.x + 8f;
            float zCentro = ColocarT1354Curva.U5.z;
            float orillaEste = ColocarT1354Curva.LateralOrilla(zCentro);
            float zFinal = zCentro - orillaEste - 14f;
            for (float z = zCentro - ColocarT1354Curva.LateralMuro - 2f; z > zFinal; z -= ancho * 0.8f)
            {
                float suelo = YPlaya(zCentro - z, orillaEste);
                foreach (float dx in new[] { -2f, 2f })
                {
                    for (float y = suelo - 0.4f; y < -2.3f; y += alto * 0.7f)
                    {
                        Poner(costa, "ML05_Escollera", true, new Vector3(x + dx, y, z), Quaternion.Euler(0f, o.Rango(0f, 360f), 0f), o.Rango(0.9f, 1.3f), null, false);
                    }
                }
            }
            Vector3 punta = new Vector3(x, 0f, zFinal - 5f);
            float fondoMar = ColocarT1354Curva.YFondoPlaya - 0.4f;
            (float radio, int cuantas, float y)[] capas =
            {
                (4.6f, 9, fondoMar), (3.1f, 6, fondoMar + alto * 0.65f), (1.5f, 3, fondoMar + alto * 1.3f), (0f, 1, fondoMar + alto * 1.95f),
            };
            foreach ((float radio, int cuantas, float y) in capas)
            {
                for (int k = 0; k < cuantas; k++)
                {
                    float ang = k * Mathf.PI * 2f / cuantas + radio;
                    Vector3 p = punta + new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang)) * radio;
                    p.y = y;
                    Poner(costa, "ML05_Escollera", true, p, Quaternion.Euler(0f, o.Rango(0f, 360f), 0f), o.Rango(0.95f, 1.25f), null, false);
                }
            }
            float cima = fondoMar + alto * 1.95f + alto * 0.75f;
            CaminoAlFaro(costa, x, zCentro, punta, cima);
            GameObject faro = Poner(costa, "ML04_Faro", false, new Vector3(punta.x, cima, punta.z), Quaternion.identity, 1f, null, true);
            if (faro != null)
            {
                Bounds fb = Medida(prefabs["ML04_Faro"]);
                HazDelFaro(o, faro.transform, faro.transform.position + Vector3.up * fb.size.y * 0.85f);
            }
        }

        /// <summary>
        /// T-1363: camino de muelle (ML03) sobre el espigón, desde la escalera que baja del paseo (en Paseo) hasta el pie del faro, con la cubierta en
        /// <see cref="YMuelle"/>. Si la base del faro queda más alta que la cubierta, una escalera ML02 sube del final del camino a la base.
        /// </summary>
        private static void CaminoAlFaro(Transform costa, float x, float zCentro, Vector3 punta, float yFaro)
        {
            Bounds recto = Medida(Modulo("ML03_MuelleRecto"));
            bool largoEnZ = recto.size.z >= recto.size.x;
            float paso = Mathf.Max(recto.size.x, recto.size.z) > 1f ? Mathf.Max(recto.size.x, recto.size.z) : 4f;
            Quaternion haciaSur = Quaternion.Euler(0f, largoEnZ ? 180f : 90f, 0f);
            // La escalera del paseo ocupa 4 m a partir del muro (z = zCentro - 16): el camino sigue desde ahí hasta 5 m antes del centro del faro.
            float z0 = zCentro - ColocarT1354Curva.LateralMuro - 1.5f - 2f - paso * 0.5f;
            float zFin = punta.z + 5f;
            int n = 0;
            for (float z = z0; z > zFin; z -= paso)
            {
                NivelarCubierta(Poner(costa, "ML03_MuelleRecto", true, new Vector3(x, YMuelle, z), haciaSur, 1f, YMuelle, n == 0), YMuelle);
                n++;
            }
            if (yFaro - YMuelle > 0.3f)
            {
                // Sube al faro: la cima (la base del faro) al sur y el pie sobre el último tramo, al norte.
                float zEscalera = z0 - paso * n + paso * 0.5f - 2f;
                Escalera(costa, new Vector3(x, 0f, zEscalera), Vector3.forward, yFaro, YMuelle, "al faro (cima = base del faro, y = " + yFaro.ToString("F2") + ")");
            }
            ColocarS2.Informe.AppendLine("Camino al faro: " + n + " tramos de muelle de z " + z0.ToString("F0") + " a " + zFin.ToString("F0") + "; base del faro y = " + yFaro.ToString("F2"));
        }

        /// <summary>El haz del faro: un cono aditivo (shader HaloLuz) que gira con una luz Spot, ambos hijos de un pivote con GiroHazFaro.</summary>
        private static void HazDelFaro(KitS2c.Obra o, Transform faro, Vector3 lampara)
        {
            GameObject pivote = new GameObject("HazFaro");
            pivote.transform.SetParent(faro, true);
            pivote.transform.position = lampara;
            pivote.AddComponent<MechaCyberpunk.Mundo.Ambiente.GiroHazFaro>();
            GameObject cono = new GameObject("Haz");
            cono.transform.SetParent(pivote.transform, false);
            cono.transform.localRotation = Quaternion.Euler(2f, 250f, 0f);
            Mesh malla = MallaHaz(120f, 2f, 10f);
            malla.name = o.Nombre + "_Haz";
            o.Mallas.Add(malla);
            cono.AddComponent<MeshFilter>().sharedMesh = malla;
            MeshRenderer mr = cono.AddComponent<MeshRenderer>();
            mr.sharedMaterial = KitS2c.Mat("T1354_HazFaroFino", () =>
            {
                Material m = new Material(Shader.Find("MechaCyberpunk/Mundo/HaloLuz"));
                m.SetColor("_Color", new Color(1f, 0.92f, 0.65f));
                m.SetFloat("_Intensidad", 0.15f);
                m.SetFloat("_Borde", 1.2f);
                m.SetFloat("_Caida", 1.3f);
                m.SetFloat("_Suave", 0.3f);
                m.SetTexture("_Ruido", KitS2c.TexMuro());
                return m;
            });
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            KitS2c.Luz(cono.transform, lampara, LightType.Spot, new Color(1f, 0.92f, 0.65f), 4000f, 140f, 28f, cono.transform.forward);
        }

        /// <summary>Cono abierto que sale de la punta (z = 0) y se abre hasta el radio dado en z = largo; la v de la UV va de 0 en la punta a 1 al final.</summary>
        private static Mesh MallaHaz(float largo, float radioInicio, float radioFin)
        {
            const int lados = 20;
            List<Vector3> v = new List<Vector3>();
            List<Vector3> n = new List<Vector3>();
            List<Vector2> uv = new List<Vector2>();
            List<int> t = new List<int>();
            for (int i = 0; i <= lados; i++)
            {
                float a = i * Mathf.PI * 2f / lados;
                Vector3 r = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
                v.Add(r * radioInicio);
                v.Add(r * radioFin + Vector3.forward * largo);
                n.Add(r);
                n.Add(r);
                uv.Add(new Vector2((float)i / lados, 0f));
                uv.Add(new Vector2((float)i / lados, 1f));
                if (i < lados)
                {
                    int k = i * 2;
                    t.AddRange(new[] { k, k + 1, k + 2, k + 2, k + 1, k + 3 });
                }
            }
            Mesh m = new Mesh();
            m.SetVertices(v);
            m.SetNormals(n);
            m.SetUVs(0, uv);
            m.SetTriangles(t, 0);
            m.RecalculateBounds();
            return m;
        }

        // ================================================================ Muelle, lanchas y puesto

        private static void Muelle(KitS2c.Obra o, Transform muelle, Transform paseo, List<Vector3> borde)
        {
            float zAvenida = ColocarT1354Curva.ZCentro(XMuelle);
            float zCosta = zAvenida - ColocarT1354Curva.LateralMuro;
            float zOrilla = zAvenida - ColocarT1354Curva.LateralOrilla(zAvenida);
            Bounds recto = Medida(Modulo("ML03_MuelleRecto"));
            // La pieza mide 3.3 x 4 m: el largo (4 m) es el lado mayor.
            bool largoEnZ = recto.size.z >= recto.size.x;
            float paso = Mathf.Max(recto.size.x, recto.size.z) > 1f ? Mathf.Max(recto.size.x, recto.size.z) : 4f;
            Quaternion haciaSur = Quaternion.Euler(0f, largoEnZ ? 180f : 90f, 0f);
            Quaternion haciaEste = Quaternion.Euler(0f, largoEnZ ? 90f : 0f, 0f);
            int tramos = Mathf.CeilToInt((zCosta - zOrilla + 26f) / paso);
            for (int i = 0; i < tramos; i++)
            {
                NivelarCubierta(Poner(muelle, "ML03_MuelleRecto", true, new Vector3(XMuelle, YMuelle, zCosta - paso * (i + 0.5f)), haciaSur, 1f, YMuelle, i == 0), YMuelle);
            }
            float zEsquina = zCosta - paso * (tramos + 0.5f);
            NivelarCubierta(Poner(muelle, "ML04_MuelleEsquina", true, new Vector3(XMuelle, YMuelle, zEsquina), haciaSur, 1f, YMuelle, true), YMuelle);
            for (int j = 0; j < 3; j++)
            {
                NivelarCubierta(Poner(muelle, "ML03_MuelleRecto", true, new Vector3(XMuelle + paso * (j + 1), YMuelle, zEsquina), haciaEste, 1f, YMuelle, false), YMuelle);
            }

            // 6 lanchas de colores amarradas al lado oeste del muelle, ya en el agua.
            for (int i = 0; i < 6; i++)
            {
                Vector3 p = new Vector3(XMuelle - 4.4f, ColocarT1354Curva.YAgua - 0.15f, zOrilla - 4f - 4.4f * i);
                GameObject lancha = Poner(muelle, "ML02_Lancha", false, p, Quaternion.Euler(0f, 90f + o.Rango(-6f, 6f), 0f), 1f, null, i == 0);
                if (lancha != null)
                {
                    Tenir(lancha, ColoresLancha[i % ColoresLancha.Length], i);
                }
            }

            // Puesto de mariscos grande, pegado al barandal y de frente a la avenida, con hieleras y bancos (sin modelo todavía: huecos).
            Vector3 puesto = Punto(borde, DistanciaEnX(borde, XPuesto), 13.2f, out Quaternion mar);
            GameObject go = Poner(paseo, "ML03_PuestoMariscos", false, puesto, Quaternion.LookRotation(-(mar * Vector3.forward)), EscalaPuesto, null, true);
            if (go != null)
            {
                KitS2c.Luz(go.transform, puesto + Vector3.up * 3.2f, LightType.Point, KitS2c.Calido, 30f, 15f, 0f);
                KitS2c.Halo(o, go.transform, puesto + Vector3.up * 3.2f, ColocarT1354Curva.YBanqueta, KitS2c.Calido);
            }
            Vector3 lado = Vector3.Cross(Vector3.up, mar * Vector3.forward);
            for (int i = 0; i < 4; i++)
            {
                HuecoSimple(paseo, "Hielera", puesto + lado * (2.6f + i * 0.7f) + (mar * Vector3.forward) * 1.4f);
            }
            for (int i = 0; i < 4; i++)
            {
                HuecoSimple(paseo, "BancoPlastico", puesto - lado * (2.4f + i * 0.8f) - (mar * Vector3.forward) * 0.8f);
            }
        }

        private static void Tenir(GameObject lancha, Color color, int indice)
        {
            foreach (Renderer r in lancha.GetComponentsInChildren<Renderer>())
            {
                Material[] ms = r.sharedMaterials;
                if (ms.Length == 0 || ms[0] == null)
                {
                    continue;
                }
                Material origen = ms[0];
                ms[0] = KitS2c.Mat("T1354_Lancha_" + indice + "_" + origen.name, () =>
                {
                    Material m = new Material(origen);
                    m.SetColor("_BaseColor", color);
                    return m;
                });
                r.sharedMaterials = ms;
            }
        }

        // ================================================================ Fachadas de la ciudad, postes y cables

        private const float IntensidadLuzNeon = 2.2f;
        private const float RangoLuzNeon = 6f;
        private static readonly Color NaranjaNeon = new Color(1f, 0.5f, 0.12f);

        /// <summary>
        /// K11 (derecha): edificios de 3 a 5 pisos, angostos y pegados, con la fachada junto a la banqueta; planta baja de cortinas metálicas, neones
        /// en bandera y en pared (magenta, turquesa, naranja), aires acondicionados y luz de foco junto a cada puerta.
        /// </summary>
        private static List<Vector3> FachadasCiudad(KitS2c.Obra o, Transform casas)
        {
            List<Vector3> altos = new List<Vector3>();
            foreach ((float x0, float x1) in Fachadas)
            {
                List<Vector3> linea = ColocarT1354Curva.PuntosEn(x0, x1, 10f);
                for (int i = 0; i + 1 < linea.Count; i++)
                {
                    Vector3 tramo = linea[i + 1] - linea[i];
                    Vector3 normal = ColocarT1354Curva.HaciaElMar(tramo.normalized);
                    Vector3 a = linea[i] + normal * ColocarT1354Curva.LateralCiudad + Vector3.up * ColocarT1354Curva.YBanqueta;
                    Vector3 b = linea[i + 1] + normal * ColocarT1354Curva.LateralCiudad + Vector3.up * ColocarT1354Curva.YBanqueta;
                    altos.AddRange(FrenteMalecon(o, casas, a, b, normal));
                }
            }
            // Las bocas de U1 y U2: fachadas del kit en las dos paredes del último tramo del corredor (donde se apaga el relleno liso de T530).
            foreach (Vector3 u in new[] { ColocarT1354Curva.U1, ColocarT1354Curva.U2 })
            {
                foreach (float lado in new[] { -1f, 1f })
                {
                    float x = u.x + lado * MitadBocaCorredor;
                    Vector3 a = new Vector3(x, ColocarT1354Curva.YBanqueta, ColocarT1354Curva.ZInicioFachadaCorredor);
                    Vector3 b = new Vector3(x, ColocarT1354Curva.YBanqueta, ColocarT1354Curva.ZPlano - ColocarT1354Curva.LateralCiudad + 7f);
                    altos.AddRange(FrenteMalecon(o, casas, a, b, new Vector3(-lado, 0f, 0f)));
                }
            }
            return altos;
        }

        /// <summary>
        /// Un tramo de fachadas del kit (K11): 3 a 5 pisos, planta baja de cortinas, toldos, aires en los pisos altos y letreros de neón en bandera
        /// con su luz chica. Devuelve lo alto de cada casa (para los cables).
        /// </summary>
        private static List<Vector3> FrenteMalecon(KitS2c.Obra o, Transform casas, Vector3 a, Vector3 b, Vector3 normal)
        {
            Vector3 lado = Vector3.Cross(Vector3.up, normal).normalized;
            int primera = casas.childCount;
            List<Vector3> altosTramo = KitS2c.Frente(o, casas, a, b, normal, 7f, k => EstiloMalecon(o));
            for (int c2 = primera; c2 < casas.childCount; c2++)
            {
                foreach (Transform neon in casas.GetChild(c2).GetComponentsInChildren<Transform>().Where(x => x.name == "Neon").ToList())
                {
                    Brillar(neon);
                }
            }
            foreach (Vector3 alto in altosTramo)
            {
                Vector3 centro = new Vector3(alto.x, ColocarT1354Curva.YBanqueta, alto.z) - normal * 0.05f;
                // Aires acondicionados en la pared de los pisos altos.
                int aires = o.Azar.Next(3);
                for (int k = 0; k < aires; k++)
                {
                    Vector3 pared = centro + Vector3.up * (3.1f + 3.0f * k) + normal * 0.3f + lado * o.Rango(-1.6f, 1.6f);
                    KitS2c.Pieza(casas, "C10_AireAcondicionado", pared, Quaternion.LookRotation(-normal), Vector3.one, false);
                }
                // Otro letrero de neón (naranja, magenta o turquesa), en bandera o pegado a la pared.
                if (o.Chance(0.75f))
                {
                    int color = o.Azar.Next(3);
                    Color c = color == 0 ? KitS2c.Magenta : color == 1 ? KitS2c.Turquesa : NaranjaNeon;
                    KitS2c.Neon(o, casas, centro + lado * o.Rango(-1.8f, 1.8f) + Vector3.up * o.Rango(3.2f, 5.2f), normal, lado, c, color != 2 && o.Chance(0.6f));
                    Brillar(casas.GetChild(casas.childCount - 1));
                }
            }
            return altosTramo;
        }

        /// <summary>
        /// T-1363: apaga (SetActive false, sin borrar) los bloques lisos de relleno de T530_Afueras (corredores sur Salida_4 y Salida_5) que quedan detrás
        /// de las fachadas del Malecón, a menos de 60 m de la avenida. Los nombres quedan en el informe.
        /// </summary>
        private static void ApagarRellenoT530()
        {
            Transform afueras = ColocarS2.Buscar("T530_Afueras");
            if (afueras == null)
            {
                ColocarS2.Informe.AppendLine("T530_Afueras no está en la escena: no se apagó nada");
                return;
            }
            List<string> apagados = new List<string>();
            foreach (string salida in new[] { "Salida_4", "Salida_5" })
            {
                Transform s = afueras.Find(salida);
                if (s == null)
                {
                    continue;
                }
                foreach (Transform h in s.Cast<Transform>().ToList())
                {
                    if (!h.gameObject.activeSelf || (h.name != "Edificio" && h.name != "Ventanas"))
                    {
                        continue;
                    }
                    Renderer[] rs = h.GetComponentsInChildren<Renderer>();
                    if (rs.Length == 0)
                    {
                        continue;
                    }
                    Bounds caja = rs[0].bounds;
                    foreach (Renderer r in rs)
                    {
                        caja.Encapsulate(r.bounds);
                    }
                    if (ColocarT1354Curva.RellenoT530Apagable(caja.center))
                    {
                        h.gameObject.SetActive(false);
                        apagados.Add(salida + "/" + h.name + "@" + caja.center.ToString("F0"));
                    }
                }
            }
            ColocarS2.Informe.AppendLine("T530 apagados detrás del Malecón: " + apagados.Count + (apagados.Count == 0 ? "" : " (" + string.Join("; ", apagados) + ")"));
        }

        /// <summary>Emisión alta (HDR x3.5) en los materiales del letrero de neón, con copias de material para no tocar los originales.</summary>
        private static void Brillar(Transform neon)
        {
            // La luz chica del letrero (de KitS2c.Neon) sube de 0.35 a 2.2 y de 3.5 a 6 m: se nota en la pared y en la banqueta.
            foreach (Light luz in neon.GetComponentsInChildren<Light>(true))
            {
                luz.intensity = IntensidadLuzNeon;
                luz.range = RangoLuzNeon;
            }
            foreach (Renderer r in neon.GetComponentsInChildren<Renderer>())
            {
                Material[] ms = r.sharedMaterials;
                for (int i = 0; i < ms.Length; i++)
                {
                    Material origen = ms[i];
                    if (origen == null)
                    {
                        continue;
                    }
                    ms[i] = KitS2c.Mat("T1354_Brillo_" + origen.name, () =>
                    {
                        Material m = new Material(origen);
                        foreach (string propiedad in new[] { "_EmissionColor", "_Color" })
                        {
                            if (m.HasProperty(propiedad))
                            {
                                Color c = m.GetColor(propiedad);
                                m.SetColor(propiedad, new Color(c.r * 3.5f, c.g * 3.5f, c.b * 3.5f, c.a));
                            }
                        }
                        return m;
                    });
                }
                r.sharedMaterials = ms;
            }
        }

        private static KitS2c.Estilo EstiloMalecon(KitS2c.Obra o)
        {
            return new KitS2c.Estilo
            {
                // Casi todas con cortina metálica (local), como K11.
                PlantaBaja = o.Chance(0.85f) ? KitS2c.PlantaLocal : KitS2c.PlantaVivienda,
                PisoAlto = o.Chance(0.15f) ? "C20b_PisoDosVentanas_cy" : KitS2c.PisoFachada,
                ColorPiso = o.Azar.Next(KitS2c.ColoresPiso.Length),
                Pisos = o.Chance(0.15f) ? 5 : o.Chance(0.4f) ? 4 : 3,
                Tinaco = o.Chance(0.5f),
                Foco = true,
                // Cada fachada de adelante lleva al menos un letrero en bandera visible desde la avenida.
                Neon = o.Chance(0.5f) ? KitS2c.Magenta : KitS2c.Turquesa,
                NeonBandera = true,
                Antena = o.Chance(0.25f),
                AireAzotea = o.Chance(0.4f),
                Toldo = o.Chance(0.5f),
            };
        }

        /// <summary>
        /// Postes de madera con lámpara y transformador a los dos lados de la avenida (del lado del mar, entre las palmeras) y muchos cables
        /// colgando de un poste al otro, cruzando la avenida y yendo a las fachadas (K11, K12).
        /// </summary>
        private static void Postes(KitS2c.Obra o, Transform postes, List<Vector3> avenida, List<Vector3> altos)
        {
            float largo = ColocarT1354Curva.Longitud(avenida);
            List<Vector3> ciudad = new List<Vector3>();
            List<Vector3> mar = new List<Vector3>();
            foreach (float d in ColocarT1354Curva.Cada(largo, 30f, 15f))
            {
                Vector3 c = ColocarT1354Curva.PuntoEn(avenida, d, out Vector3 t);
                if (Mathf.Abs(Mathf.Abs(c.x) - 47f) < MitadBocaCorredor + 2f || c.x > 108f)
                {
                    continue;
                }
                Vector3 hacia = ColocarT1354Curva.HaciaElMar(t);
                Vector3 pie = c + hacia * -9.6f;
                pie.y = ColocarT1354Curva.YBanqueta;
                ciudad.Add(KitS2c.Poste(o, postes, pie, hacia, ColocarT1354Curva.YBanqueta, o.Chance(0.7f)));
            }
            foreach (float d in ColocarT1354Curva.Cada(largo, 36f, 33f))
            {
                Vector3 c = ColocarT1354Curva.PuntoEn(avenida, d, out Vector3 t);
                if (EnBocaDeUnion(c.x) || Mathf.Abs(c.x - XPuesto) < 8f || Mathf.Abs(c.x - XMuelle) < 8f)
                {
                    continue;
                }
                Vector3 hacia = ColocarT1354Curva.HaciaElMar(t);
                Vector3 pie = c + hacia * 9.4f;
                pie.y = ColocarT1354Curva.YBanqueta;
                mar.Add(KitS2c.Poste(o, postes, pie, -hacia, ColocarT1354Curva.YBanqueta, true));
            }
            Unir(o, ciudad, 40f);
            Unir(o, mar, 46f);
            // Cruzando la avenida: de cada poste del mar a los dos de la ciudad más cercanos, cuatro cables cada uno.
            foreach (Vector3 m in mar)
            {
                foreach (Vector3 c in ciudad.Where(q => Vector3.Distance(q, m) < 34f).OrderBy(q => Vector3.Distance(q, m)).Take(2))
                {
                    for (int k = 0; k < 4; k++)
                    {
                        Vector3 baja = Vector3.down * (0.35f * k);
                        KitS2c.Cable(o, m + baja, c + baja, o.Rango(1.2f, 2.4f));
                    }
                }
                foreach (Vector3 alto in altos.Where(a => Vector3.Distance(a, m) < 30f).OrderBy(a => Vector3.Distance(a, m)).Take(3))
                {
                    KitS2c.Cable(o, m - Vector3.up * 0.3f, alto, o.Rango(1f, 2f));
                    KitS2c.Cable(o, m - Vector3.up * 0.7f, alto - Vector3.up * 0.4f, o.Rango(1f, 2f));
                }
            }
            foreach (Vector3 punta in ciudad)
            {
                foreach (Vector3 alto in altos.Where(a => Vector3.Distance(a, punta) < 16f).Take(4))
                {
                    KitS2c.Cable(o, punta - Vector3.up * 0.3f, alto, o.Rango(0.3f, 0.8f));
                }
            }
        }

        /// <summary>Tres cables entre postes seguidos de una misma hilera.</summary>
        private static void Unir(KitS2c.Obra o, List<Vector3> hilera, float distanciaMaxima)
        {
            for (int i = 0; i + 1 < hilera.Count; i++)
            {
                if (Vector3.Distance(hilera[i], hilera[i + 1]) >= distanciaMaxima)
                {
                    continue;
                }
                for (int k = 0; k < 3; k++)
                {
                    Vector3 baja = Vector3.down * (0.45f * k);
                    KitS2c.Cable(o, hilera[i] + baja, hilera[i + 1] + baja, o.Rango(0.6f, 1.2f));
                }
            }
        }

        // ================================================================ Mar y sondas

        private static void Mar(KitS2c.Obra o, Transform mar, List<Vector3> borde)
        {
            // Agua de x -700..700 y z -262..-900 (plan de zonas). El shader es de T-1351: se llama por reflexión.
            // Agua de x -700..330 (de 330 al este es del Puerto) y de z -900 hasta -215: por debajo de la arena, la orilla es donde la arena baja de y = -3.
            bool plano = LlamarMar("CrearPlanoMar", mar, new Vector3(-185f, ColocarT1354Curva.YAgua, -557.5f), new Vector2(1030f, 685f), ColocarT1354Curva.YAgua);
            List<Vector3> orilla = new List<Vector3>();
            for (int i = 0; i < borde.Count; i++)
            {
                Vector3 mirada = borde[Mathf.Min(i + 1, borde.Count - 1)] - borde[Mathf.Max(i - 1, 0)];
                Vector3 hacia = ColocarT1354Curva.HaciaElMar(mirada.normalized);
                Vector3 punto = borde[i] + hacia * ColocarT1354Curva.LateralOrilla(borde[i].z);
                punto.y = ColocarT1354Curva.YAgua;
                orilla.Add(punto);
            }
            bool espuma = LlamarMar("CrearOrilla", mar, orilla, ColocarT1354Curva.YAgua, 3f);
            if (!plano)
            {
                new GameObject("Hueco_Mar").transform.SetParent(mar, false);
                faltan.Add("Mar (MechaCyberpunk.Mundo.Editor.CreadorMarT1351.CrearPlanoMar de T-1351)");
            }
            if (!espuma)
            {
                new GameObject("Hueco_Orilla").transform.SetParent(mar, false);
                faltan.Add("Orilla (CreadorMarT1351.CrearOrilla de T-1351)");
            }
        }

        private static bool LlamarMar(string metodo, params object[] argumentos)
        {
            foreach (Assembly ensamblado in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type tipo = ensamblado.GetType("MechaCyberpunk.Mundo.Editor.CreadorMarT1351", false);
                MethodInfo m = tipo?.GetMethod(metodo, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                if (m != null)
                {
                    m.Invoke(null, argumentos);
                    return true;
                }
            }
            return false;
        }

        private static void Sondas(Transform raiz)
        {
            foreach (Vector3 centro in new[] { new Vector3(-120f, 4f, -246f), new Vector3(60f, 4f, -246f) })
            {
                GameObject go = new GameObject("SondaReflejo");
                go.transform.SetParent(raiz, false);
                go.transform.position = centro;
                ReflectionProbe sonda = go.AddComponent<ReflectionProbe>();
                sonda.mode = UnityEngine.Rendering.ReflectionProbeMode.Realtime;
                sonda.refreshMode = UnityEngine.Rendering.ReflectionProbeRefreshMode.OnAwake;
                sonda.resolution = 128;
                sonda.size = new Vector3(170f, 30f, 70f);
            }
        }

        // ================================================================ T-1363: la otra orilla (skyline) y la luz local

        public const string RutaPerfilLuz = "Assets/Arte/Mundo/Zonas/PerfilMalecon_T1363.asset";
        /// <summary>La otra orilla: una losa oscura de x -700 a 330 y z -560 a -700 con torres encima, a unos 330 m de la avenida (más cerca para que entre en el corte de 600 m de las fotos y se lea como bahía, K11).</summary>
        private const float ZOrillaLejana = -560f;
        private const float ZFinOrillaLejana = -700f;
        private const float XOesteOrillaLejana = -700f;
        private const float XEsteOrillaLejana = 330f;
        private const float AltoPisoLejano = 12f;
        private static readonly Color[] ColoresOrillaLejana =
        {
            new Color(1f, 0.55f, 0.2f), new Color(0.1f, 0.95f, 0.88f), new Color(1f, 0.2f, 0.78f), new Color(1f, 0.55f, 0.2f), new Color(0.35f, 0.5f, 1f),
        };

        public const string RutaTexVentanasLejanas = "Assets/Arte/Mundo/Zonas/T1363_VentanasLejanas.png";
        private const int CeldasVentanas = 32;
        private const int PixelesCelda = 16;
        /// <summary>Cada ventana mide 1.5 m: la textura de 32 x 32 celdas cubre 48 m (UV en metros de la malla de caja).</summary>
        private const float MetrosVentana = 1.5f;

        /// <summary>
        /// Textura de ventanas de 512 x 512 (32 x 32 celdas de 16 px): rejilla con celdas encendidas al azar en tonos cálidos (sodio, foco) y fríos
        /// (led, tele), alguna magenta, sobre pared casi negra. Se guarda como asset y se rehace igual cada vez (semilla fija).
        /// </summary>
        public static Texture2D TexturaVentanasLejanas()
        {
            int lado = CeldasVentanas * PixelesCelda;
            Texture2D t = new Texture2D(lado, lado, TextureFormat.RGBA32, false);
            Color pared = new Color(0.015f, 0.017f, 0.025f, 1f);
            System.Random azar = new System.Random(1363);
            Color[] pixeles = new Color[lado * lado];
            for (int i = 0; i < pixeles.Length; i++)
            {
                pixeles[i] = pared;
            }
            for (int cx = 0; cx < CeldasVentanas; cx++)
            {
                for (int cy = 0; cy < CeldasVentanas; cy++)
                {
                    if (azar.NextDouble() > 0.55)
                    {
                        continue;
                    }
                    double tono = azar.NextDouble();
                    Color luz = tono < 0.5 ? new Color(1f, 0.74f, 0.4f) : tono < 0.88 ? new Color(0.62f, 0.82f, 1f) : new Color(1f, 0.35f, 0.8f);
                    luz *= 0.55f + 0.45f * (float)azar.NextDouble();
                    luz.a = 1f;
                    for (int px = 2; px < PixelesCelda - 2; px++)
                    {
                        for (int py = 3; py < PixelesCelda - 3; py++)
                        {
                            pixeles[(cy * PixelesCelda + py) * lado + cx * PixelesCelda + px] = luz;
                        }
                    }
                }
            }
            t.SetPixels(pixeles);
            t.Apply();
            File.WriteAllBytes(RutaTexVentanasLejanas, t.EncodeToPNG());
            Object.DestroyImmediate(t);
            AssetDatabase.ImportAsset(RutaTexVentanasLejanas);
            TextureImporter imp = (TextureImporter)AssetImporter.GetAtPath(RutaTexVentanasLejanas);
            imp.sRGBTexture = true;
            imp.wrapMode = TextureWrapMode.Repeat;
            imp.mipmapEnabled = true;
            imp.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(RutaTexVentanasLejanas);
        }

        /// <summary>
        /// Material que brilla siempre (URP Unlit, color HDR): no usa EmisionNoche, que depende de la hora (_T501Noche) y de un componente en la escena.
        /// De día se ve claro, pero la otra orilla está a 300 m y la niebla la funde.
        /// </summary>
        private static Material MatBrillo(string nombre, Color color, Texture textura)
        {
            return KitS2c.Mat(nombre, () =>
            {
                Material m = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                m.SetColor("_BaseColor", color);
                m.SetTexture("_BaseMap", textura != null ? textura : Texture2D.whiteTexture);
                return m;
            });
        }

        /// <summary>
        /// K11 (izquierda) y K12 (fondo): una ciudad iluminada al otro lado del agua. Torres del kit T20-T23 (como las de Neón Alto) con un material
        /// propio de ventanas encendidas (textura generada, emisión HDR x2.5, ventanas de 1.5 m), pantallas grandes magenta y turquesa en 7 torres, luz roja
        /// en cada azotea, 3 haces verticales tenues (HaloLuz) y una franja de luces cálidas en la orilla que el shader del mar vuelve estelas.
        /// Sin colliders ni sombras; encima, pocas luces puntuales fuertes en la base.
        /// </summary>
        private static void OtraOrilla(KitS2c.Obra o, Transform grupo)
        {
            Material suelo = KitS2c.Mat("T1363_OrillaLejana", () => KitS2c.Lit(new Color(0.02f, 0.025f, 0.035f), 0.1f));
            float largo = XEsteOrillaLejana - XOesteOrillaLejana;
            float centroX = (XEsteOrillaLejana + XOesteOrillaLejana) * 0.5f;
            KitS2c.Losa(o, grupo, "Orilla", new Vector3(centroX, -1f, (ZOrillaLejana + ZFinOrillaLejana) * 0.5f), Quaternion.identity,
                new Vector3(largo, 4f, ZOrillaLejana - ZFinOrillaLejana), suelo, 40f, false);

            // Franja de luces cálidas en el borde de la orilla (sodio y foco, con algo de turquesa y magenta), en tramos de 70 m con huecos de 20 m.
            int tramo = 0;
            for (float x = XOesteOrillaLejana + 30f; x + 70f < XEsteOrillaLejana; x += 90f, tramo++)
            {
                int c = tramo % ColoresOrillaLejana.Length;
                Material m = MatBrillo("T1363b_FranjaOrilla_" + c, ColoresOrillaLejana[c] * 3f, null);
                KitS2c.Losa(o, grupo, "FranjaOrilla", new Vector3(x + 35f, 1.4f, ZOrillaLejana - 0.4f), Quaternion.identity, new Vector3(70f, 0.8f, 0.6f), m, 4f, false);
            }

            Texture2D ventanas = TexturaVentanasLejanas();
            MatBrillo("T1363b_VentanasLejanas", new Color(2.5f, 2.5f, 2.5f, 1f), ventanas);
            Material matRojo = MatBrillo("T1363b_LuzRoja", new Color(4f, 0.15f, 0.1f, 1f), null);
            Material matMagenta = MatBrillo("T1363b_PantallaMagenta", new Color(3.8f, 0.5f, 3f, 1f), null);
            Material matTurquesa = MatBrillo("T1363b_PantallaTurquesa", new Color(0.4f, 3.6f, 3.4f, 1f), null);
            Material matHaz = KitS2c.Mat("T1363b_HazLejano", () =>
            {
                Material m = new Material(Shader.Find("MechaCyberpunk/Mundo/HaloLuz"));
                m.SetColor("_Color", new Color(0.3f, 0.95f, 1f));
                m.SetFloat("_Intensidad", 0.3f);
                m.SetFloat("_Borde", 1.2f);
                m.SetFloat("_Caida", 0.4f);
                m.SetFloat("_Suave", 0.3f);
                m.SetTexture("_Ruido", KitS2c.TexMuro());
                return m;
            });

            System.Random azar = new System.Random(Semilla + 9);
            int torres = 0;
            int pantallas = 0;
            int haces = 0;
            Quaternion sinGiro = Quaternion.identity;
            GameObject t20 = KitS2c.Prefab("T20");
            GameObject t21 = KitS2c.Prefab("T21");
            GameObject t22 = KitS2c.Prefab("T22");
            GameObject t23 = KitS2c.Prefab("T23");
            if (t20 == null || t21 == null || t22 == null || t23 == null)
            {
                faltan.Add("Torres T20-T23 de Neón Alto (Assets/Arte/Exteriores/Prefabs)");
                return;
            }
            for (float x = -330f; x <= 270f; x += azar.Next(20, 36))
            {
                float ancho = 16f + (float)azar.NextDouble() * 18f;
                float fondo = 16f + (float)azar.NextDouble() * 12f;
                float alto = 50f + (float)azar.NextDouble() * 120f;
                float z = ZOrillaLejana - 22f - (float)azar.NextDouble() * 85f;
                Transform torre = new GameObject("Torre_" + torres).transform;
                torre.SetParent(grupo, false);
                torre.position = new Vector3(x, 0.8f, z);
                // Las piezas del kit de Neón Alto (T-541): base T20, pisos T21 con alguna pantalla T22 y remate T23, escaladas a la planta y a la altura.
                // Sus renderers llevan el material de ventanas encendidas (con el tiling de la planta de la torre).
                int pisos = Mathf.Max(1, Mathf.RoundToInt((alto - ColocarT541.AltoBase - ColocarT541.AltoRemate) / AltoPisoLejano));
                float y = TramoLejano(torre, t20, "Base", 0f, ColocarT541.AltoBase, ancho, fondo);
                bool ultimaPantalla = false;
                for (int i = 0; i < pisos; i++)
                {
                    bool pantalla = !ultimaPantalla && i > 0 && azar.NextDouble() < 0.3;
                    y = TramoLejano(torre, pantalla ? t22 : t21, pantalla ? "PisoPantalla" : "Piso", y, AltoPisoLejano, ancho, fondo);
                    ultimaPantalla = pantalla;
                }
                y = TramoLejano(torre, t23, "Remate", y, ColocarT541.AltoRemate, ancho, fondo);
                // La cara de pantallas del kit (la -Z del modelo) mira al norte. Se gira antes de agregar lo demás (que va en coordenadas del mundo).
                torre.rotation = Quaternion.LookRotation(Vector3.back, Vector3.up);
                foreach (Renderer r in torre.GetComponentsInChildren<Renderer>(true))
                {
                    Vector3 tam = r.bounds.size;
                    Material[] ms = new Material[r.sharedMaterials.Length];
                    for (int k = 0; k < ms.Length; k++)
                    {
                        ms[k] = MatVentanasConTiling(ventanas, Mathf.Max(tam.x, tam.z), tam.y);
                    }
                    r.sharedMaterials = ms;
                }
                float cima = 1f + y;
                // Luz roja de aviso en la azotea.
                SinSombra(KitS2c.Losa(o, torre, "LuzRoja", new Vector3(x, cima + 1.2f, z), sinGiro, new Vector3(1.6f, 2.4f, 1.6f), matRojo, 4f, false));
                // Pantalla grande en la cara que mira al norte (a la avenida), pegada a la fachada, en una de cada 3 torres, hasta 8.
                if (torres % 3 == 1 && pantallas < 8)
                {
                    float altoPantalla = Mathf.Min(alto * 0.45f, 30f);
                    float yPantalla = 1f + alto * (0.35f + 0.3f * (float)azar.NextDouble());
                    SinSombra(KitS2c.Losa(o, torre, "Pantalla", new Vector3(x, yPantalla, z + fondo * 0.5f + 0.25f), sinGiro,
                        new Vector3(ancho * 0.7f, altoPantalla, 0.4f), pantallas % 2 == 0 ? matMagenta : matTurquesa, 4f, false));
                    pantallas++;
                }
                // Haz vertical tenue sobre 3 torres altas.
                if (haces < 3 && alto > 110f && torres % 2 == 0)
                {
                    GameObject haz = new GameObject("Haz");
                    haz.transform.SetParent(torre, false);
                    haz.transform.SetPositionAndRotation(new Vector3(x, cima + 2.4f, z), Quaternion.Euler(-90f, 0f, 0f));
                    Mesh malla = MallaHaz(260f, 1.5f, 7f);
                    malla.name = o.Nombre + "_HazLejano_" + haces;
                    o.Mallas.Add(malla);
                    haz.AddComponent<MeshFilter>().sharedMesh = malla;
                    SinSombra(haz).sharedMaterial = matHaz;
                    haces++;
                }
                torres++;
                // Una luz de color fuerte en la base de una de cada tres torres, sobre la losa (para las estelas del mar).
                if (torres % 3 == 1)
                {
                    Color color = ColoresOrillaLejana[(torres / 3) % ColoresOrillaLejana.Length];
                    KitS2c.Luz(torre, new Vector3(x, 6f, ZOrillaLejana - 6f), LightType.Point, color, 40f, 240f, 0f);
                }
            }
            ColocarS2.Informe.AppendLine("Otra orilla (z " + ZOrillaLejana + " a " + ZFinOrillaLejana + "): " + torres + " torres con ventanas emisivas Unlit, "
                + pantallas + " pantallas, " + haces + " haces, " + grupo.GetComponentsInChildren<Light>(true).Length + " luces");
        }

        /// <summary>
        /// Material de ventanas con el tiling de una pieza: se supone que la UV del modelo recorre 0 a 1 la cara (ancho x alto); la textura cubre 48 m
        /// (32 ventanas de 1.5 m), así que el tiling es medida / 48. Se agrupa de a 0.05 para no hacer un material por pieza.
        /// </summary>
        private static Material MatVentanasConTiling(Texture2D ventanas, float ancho, float alto)
        {
            float cubre = MetrosVentana * CeldasVentanas;
            float tx = Mathf.Max(0.05f, Mathf.Round(ancho / cubre * 20f) / 20f);
            float ty = Mathf.Max(0.05f, Mathf.Round(alto / cubre * 20f) / 20f);
            return KitS2c.Mat("T1363b_Ventanas_" + tx.ToString("F2") + "x" + ty.ToString("F2"), () =>
            {
                Material m = new Material(MatBrillo("T1363b_VentanasLejanas", new Color(2.5f, 2.5f, 2.5f, 1f), ventanas));
                m.mainTextureScale = new Vector2(tx, ty);
                return m;
            });
        }

        /// <summary>Una pieza de torre lejana: llena la planta, sin colliders ni sombras (como Pieza de T-541).</summary>
        private static float TramoLejano(Transform torre, GameObject prefab, string nombre, float y, float alto, float ancho, float fondo)
        {
            GameObject g = (GameObject)PrefabUtility.InstantiatePrefab(prefab, torre);
            g.name = nombre;
            g.transform.localRotation = Quaternion.identity;
            g.transform.localScale = Vector3.one;
            Bounds b = ArteT250.Limites(g);
            g.transform.localScale = new Vector3(ancho / Mathf.Max(0.01f, b.size.x), alto / Mathf.Max(0.01f, b.size.y), fondo / Mathf.Max(0.01f, b.size.z));
            g.transform.localPosition = new Vector3(0f, y, 0f);
            foreach (Collider c in g.GetComponentsInChildren<Collider>(true))
            {
                c.enabled = false;
            }
            foreach (Renderer r in g.GetComponentsInChildren<Renderer>(true))
            {
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false;
            }
            return y + alto;
        }

        /// <summary>Quita sombras (emitir y recibir) y deja el MeshRenderer del objeto listo; devuelve el renderer.</summary>
        private static MeshRenderer SinSombra(GameObject go)
        {
            MeshRenderer mr = go.GetComponent<MeshRenderer>();
            if (mr == null)
            {
                mr = go.AddComponent<MeshRenderer>();
            }
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            return mr;
        }

        /// <summary>Cada torre lejana se oculta cuando ocupa menos del 0.3 % de la pantalla (cull por LOD).</summary>
        private static void LodDeTorres(Transform grupo)
        {
            foreach (Transform torre in grupo)
            {
                Renderer[] rs = torre.GetComponentsInChildren<Renderer>();
                if (rs.Length == 0 || !torre.name.StartsWith("Torre_", StringComparison.Ordinal))
                {
                    continue;
                }
                LODGroup lod = torre.gameObject.AddComponent<LODGroup>();
                lod.SetLODs(new[] { new LOD(0.003f, rs) });
                lod.RecalculateBounds();
            }
        }

        /// <summary>
        /// La luz del Malecón: un Volume LOCAL (isGlobal = false, BoxCollider trigger, mezcla de 15 m, prioridad 10) con balance de blancos cálido, menos saturación,
        /// filtro de color sodio y más bloom, como los volúmenes por zona de T-501 y T-507. No toca la luz de los barrios viejos (z &gt; -190).
        /// </summary>
        private static void VolumenLocal(Transform grupo)
        {
            VolumeProfile perfil = PerfilLuz();
            Bounds caja = ColocarT1354Curva.CajaVolumen();
            GameObject go = new GameObject("VolumenMalecon");
            go.transform.SetParent(grupo, false);
            go.transform.position = caja.center;
            BoxCollider collider = go.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.size = caja.size;
            Volume v = go.AddComponent<Volume>();
            v.isGlobal = false;
            v.blendDistance = ColocarT1354Curva.VolumenMezcla;
            v.priority = ColocarT1354Curva.VolumenPrioridad;
            v.weight = 1f;
            v.sharedProfile = perfil;
            ColocarS2.Informe.AppendLine("Volumen local del Malecón: caja " + caja.min + " a " + caja.max + ", mezcla " + ColocarT1354Curva.VolumenMezcla + " m");
        }

        /// <summary>Crea el perfil o lo rehace en su lugar (mismo GUID). Sodio y neón: más cálido, menos azul, algo más de bloom.</summary>
        private static VolumeProfile PerfilLuz()
        {
            VolumeProfile perfil = AssetDatabase.LoadAssetAtPath<VolumeProfile>(RutaPerfilLuz);
            if (perfil == null)
            {
                perfil = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(perfil, RutaPerfilLuz);
            }
            foreach (Object viejo in AssetDatabase.LoadAllAssetsAtPath(RutaPerfilLuz))
            {
                if (viejo is VolumeComponent)
                {
                    Object.DestroyImmediate(viejo, true);
                }
            }
            perfil.components.Clear();
            ColorAdjustments ca = perfil.Add<ColorAdjustments>(true);
            ca.postExposure.Override(0.2f);
            ca.saturation.Override(-6f);
            ca.colorFilter.Override(new Color(1.1f, 0.97f, 0.84f));
            WhiteBalance wb = perfil.Add<WhiteBalance>(true);
            wb.temperature.Override(30f);
            wb.tint.Override(6f);
            Bloom bloom = perfil.Add<Bloom>(true);
            bloom.threshold.Override(0.85f);
            bloom.intensity.Override(1.4f);
            bloom.scatter.Override(0.7f);
            Vignette vi = perfil.Add<Vignette>(true);
            vi.intensity.Override(0.28f);
            foreach (VolumeComponent c in new VolumeComponent[] { ca, wb, bloom, vi })
            {
                AssetDatabase.AddObjectToAsset(c, perfil);
            }
            EditorUtility.SetDirty(perfil);
            AssetDatabase.SaveAssets();
            return perfil;
        }

        // ================================================================ Piezas, medidas y huecos

        private static Transform Hijo(Transform padre, string nombre)
        {
            Transform t = new GameObject(nombre).transform;
            t.SetParent(padre, false);
            return t;
        }

        /// <summary>Pieza modular de Blender (T-1353): Assets/Arte/ZonasModulos/Prefabs/&lt;nombre&gt;.prefab, o null.</summary>
        private static GameObject Modulo(string nombre)
        {
            if (!prefabs.TryGetValue(nombre, out GameObject p))
            {
                p = AssetDatabase.LoadAssetAtPath<GameObject>(CorrerZonasT1350.RutaModulo(nombre));
                prefabs[nombre] = p;
            }
            return p;
        }

        /// <summary>Modelo de Tripo (sesión B) por código dentro de Assets/Arte/Zonas, o null.</summary>
        private static GameObject Tripo(string codigo)
        {
            if (!prefabs.TryGetValue(codigo, out GameObject p))
            {
                p = null;
                if (AssetDatabase.IsValidFolder(CarpetaTripo))
                {
                    foreach (string guid in AssetDatabase.FindAssets(codigo + " t:Prefab", new[] { CarpetaTripo }))
                    {
                        string ruta = AssetDatabase.GUIDToAssetPath(guid);
                        if (Path.GetFileNameWithoutExtension(ruta).StartsWith(codigo, StringComparison.Ordinal))
                        {
                            p = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
                            break;
                        }
                    }
                }
                prefabs[codigo] = p;
            }
            return p;
        }

        /// <summary>Caja de los renderers del prefab con su pivote en el origen (se mide una instancia temporal). Sin prefab: caja vacía.</summary>
        private static Bounds Medida(GameObject prefab)
        {
            if (prefab == null)
            {
                return new Bounds(Vector3.zero, Vector3.zero);
            }
            if (medidas.TryGetValue(prefab.name, out Bounds guardada))
            {
                return guardada;
            }
            GameObject tmp = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            tmp.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            tmp.transform.localScale = Vector3.one;
            Renderer[] rs = tmp.GetComponentsInChildren<Renderer>();
            Bounds b = rs.Length == 0 ? new Bounds(Vector3.zero, Vector3.zero) : rs[0].bounds;
            foreach (Renderer r in rs)
            {
                b.Encapsulate(r.bounds);
            }
            Object.DestroyImmediate(tmp);
            medidas[prefab.name] = b;
            return b;
        }

        /// <summary>
        /// Pone una pieza con la base (o el tope, si se da <paramref name="yTope"/>) en <paramref name="suelo"/>, centrada en su caja medida. Si no existe,
        /// deja un "Hueco_CODIGO" vacío (uno solo cuando <paramref name="conHueco"/>) y lo anota.
        /// </summary>
        private static GameObject Poner(Transform padre, string codigo, bool modulo, Vector3 suelo, Quaternion giro, float escala, float? yTope, bool conHueco)
        {
            GameObject prefab = modulo ? Modulo(codigo) : Tripo(codigo);
            if (prefab == null)
            {
                faltan.Add(codigo);
                if (conHueco)
                {
                    GameObject h = new GameObject("Hueco_" + codigo);
                    h.transform.SetParent(padre, false);
                    h.transform.SetPositionAndRotation(suelo, giro);
                }
                return null;
            }
            Bounds b = Medida(prefab);
            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, padre);
            go.transform.rotation = giro;
            go.transform.localScale = Vector3.one * escala;
            Vector3 ancla = new Vector3(b.center.x, yTope.HasValue ? b.max.y : b.min.y, b.center.z) * escala;
            Vector3 destino = suelo;
            if (yTope.HasValue)
            {
                destino.y = yTope.Value;
            }
            go.transform.position = destino - giro * ancla;
            return go;
        }

        private static void HuecoSimple(Transform padre, string nombre, Vector3 posicion)
        {
            faltan.Add(nombre + " (sin código de modelo)");
            GameObject h = new GameObject("Hueco_" + nombre);
            h.transform.SetParent(padre, false);
            h.transform.position = posicion;
        }

        // ================================================================ Luces, estáticos y LOD

        private static void Estaticos(Transform raiz)
        {
            const StaticEditorFlags banderas = StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.OccluderStatic;
            foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true))
            {
                if (t == raiz || t.GetComponentInParent<MechaCyberpunk.Mundo.Ambiente.GiroHazFaro>() != null || t.GetComponent<Light>() != null || t.GetComponent<LuzNocturna>() != null || t.GetComponent<ReflectionProbe>() != null || t.GetComponent<CalleZona>() != null)
                {
                    continue;
                }
                GameObjectUtility.SetStaticEditorFlags(t.gameObject, banderas);
            }
        }

        /// <summary>LOD en lo grande: cada casa se oculta cuando ocupa menos del 1.5 % de la pantalla.</summary>
        private static void LodDeCasas(Transform casas)
        {
            foreach (Transform casa in casas)
            {
                Renderer[] rs = casa.GetComponentsInChildren<Renderer>();
                if (rs.Length == 0 || !casa.name.StartsWith("Casa_", StringComparison.Ordinal))
                {
                    continue;
                }
                LODGroup lod = casa.gameObject.AddComponent<LODGroup>();
                lod.SetLODs(new[] { new LOD(0.015f, rs) });
                lod.RecalculateBounds();
            }
        }
    }
}
