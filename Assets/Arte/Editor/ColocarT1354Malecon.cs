using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using MechaCyberpunk.Core.Mundo;
using MechaCyberpunk.Mundo.Reloj;
using UnityEditor;
using UnityEngine;
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
        public const int MaximoLuces = 10;
        public const float PasoPaseo = 12f;
        public const float PasoEscaleras = 80f;
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

        // Intervalos de x donde hay fachadas (se saltan las bocas de los corredores x = ±47 y el Puerto, que empieza en x = 115).
        private static readonly (float x0, float x1)[] Fachadas = { (-295f, -57f), (-37f, 37f), (57f, 105f) };
        private static readonly Color[] ColoresLancha =
        {
            new Color(0.8f, 0.15f, 0.12f), new Color(0.15f, 0.55f, 0.3f), new Color(0.15f, 0.4f, 0.75f), new Color(0.9f, 0.75f, 0.15f), new Color(0.1f, 0.75f, 0.7f),
        };

        private static readonly HashSet<string> faltan = new HashSet<string>();
        private static readonly Dictionary<string, Bounds> medidas = new Dictionary<string, Bounds>();
        private static readonly Dictionary<string, GameObject> prefabs = new Dictionary<string, GameObject>();
        private static readonly List<Light> protegidas = new List<Light>();

        // ================================================================ Entradas

        [MenuItem("Mecha Cyberpunk/T-1354/Malecon")]
        public static void Aplicar()
        {
            ColocarS2.Abrir();
            ColocarS2.Informe.Clear();
            faltan.Clear();
            medidas.Clear();
            prefabs.Clear();
            protegidas.Clear();
            KitS2c.Obra o = KitS2c.Empezar(NombreRaiz, Semilla);
            Transform piso = Hijo(o.Raiz, "Piso");
            Transform calles = Hijo(o.Raiz, "Calles");
            Transform paseo = Hijo(o.Raiz, "Paseo");
            Transform costa = Hijo(o.Raiz, "Costa");
            Transform muelle = Hijo(o.Raiz, "Muelle");
            Transform casas = Hijo(o.Raiz, "Fachadas");
            Transform postes = Hijo(o.Raiz, "Postes");
            Transform mar = Hijo(o.Raiz, "Mar");

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
            Sondas(o.Raiz);

            LimitarLuces(o.Raiz);
            Estaticos(o.Raiz);
            LodDeCasas(casas);
            KitS2c.Terminar(o);
            ColocarS2.Informe.AppendLine("T-1354 Malecón: " + o.Casas + " casas, " + o.Neones + " neones, " + o.Postes + " postes, " + o.Cables + " cables, " + o.Charcos + " charcos");
            ColocarS2.Informe.AppendLine("Luces reales: " + o.Raiz.GetComponentsInChildren<Light>(true).Length + " (máximo " + MaximoLuces + ")");
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
                // K11: a nivel de banqueta del lado del mar, mirando al oeste por la avenida con el mar a la izquierda.
                ("k11", "K11_Malecon.png", new Vector3(40f, 1.9f, -238f), new Vector3(-60f, 3.2f, -229f), 62f),
                // K12: junto al puesto, mirando al muelle y al faro (el faro está al final este: la toma mira al este-sureste).
                ("k12", "K12_Malecon_Muelle.png", new Vector3(62f, 1.9f, -236f), new Vector3(125f, 0f, -262f), 62f),
                // A11: aérea desde el mar con la costa a la derecha.
                ("a11", "A11_Malecon_Aerea.png", new Vector3(-150f, 55f, -300f), new Vector3(-60f, 0f, -228f), 62f),
            };
            Directory.CreateDirectory("Logs");
            foreach (var t in tomas)
            {
                string juego = "Logs/t1354-" + t.nombre + ".png";
                ColocarS2cLook.Foto(t.desde, t.hacia, juego, t.campo);
                ColocarS2.LadoALado(referencias + t.referencia, juego, "docs/capturas/zonas/malecon-" + t.nombre + ".png");
            }
        }

        // ================================================================ Terreno: calles, banquetas, playa

        private static void Terreno(KitS2c.Obra o, Transform piso, Transform calles, List<Vector3> avenida, List<Vector3> borde)
        {
            Material asfalto = KitS2c.MatAsfalto();
            Material banqueta = KitS2c.MatBanqueta();
            Material arena = KitS2c.Mat("T1354_Arena", () => KitS2c.Lit(new Color(0.16f, 0.14f, 0.13f), 0.15f));
            Material fondo = KitS2c.Mat("T1354_FondoMar", () => KitS2c.Lit(new Color(0.03f, 0.05f, 0.08f), 0.1f));
            const float h = ColocarT1354Curva.YBanqueta;
            const float a = ColocarT1354Curva.YArroyo;

            // Avenida: 16 m de arroyo, banqueta angosta del lado de la ciudad (en tramos: las bocas de U1 y U2 quedan abiertas) y ancha del lado del mar.
            Franja(o, piso, "Avenida", avenida, i => new[] { (-8f, a), (8f, a) }, asfalto, 8f, true);
            foreach ((float x0, float x1) in new[] { (-300f, -57f), (-37f, 37f), (57f, 160f) })
            {
                Franja(o, piso, "BanquetaCiudad", ColocarT1354Curva.PuntosEn(x0, x1, ColocarT1354Curva.PasoX), i => new[] { (-11f, h), (-8f, h), (-8f, a) }, banqueta, 2f, true);
            }
            Franja(o, piso, "BanquetaMar", borde, i => new[] { (8f, a), (8f, h), (ColocarT1354Curva.LateralMuro, h), (ColocarT1354Curva.LateralMuro, ColocarT1354Curva.YPiePlaya) }, banqueta, 2f, true);

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
                Franja(o, piso, "BanquetaA_" + nombre, hasta, i => new[] { (-10f, h), (-7f, h), (-7f, a) }, banqueta, 2f, true);
                Franja(o, piso, "BanquetaB_" + nombre, hasta, i => new[] { (7f, a), (7f, h), (10f, h) }, banqueta, 2f, true);
            }
            List<Vector3> u3 = ColocarT1354Curva.U3();
            CalleZonaDe(calles, "CalleZona_U3", u3, AnchoUnion);
            Franja(o, piso, "Arroyo_U3", u3, i => new[] { (-7f, a), (7f, a) }, asfalto, 8f, true);
            Franja(o, piso, "BanquetaA_U3", u3, i => new[] { (-10f, h), (-7f, h), (-7f, a) }, banqueta, 2f, true);
            Franja(o, piso, "BanquetaB_U3", u3, i => new[] { (7f, a), (7f, h), (10f, h) }, banqueta, 2f, true);
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
            for (int i = 0; i < 18; i++)
            {
                Vector3 p = ColocarT1354Curva.PuntoEn(avenida, o.Rango(4f, largo - 4f), out Vector3 t);
                p += ColocarT1354Curva.HaciaElMar(t) * o.Rango(-6.5f, 6.5f);
                p.y = a;
                KitS2c.Charco(o, piso, p, o.Rango(1.6f, 4.2f), o.Rango(1f, 2.4f), o.Rango(0f, 180f));
            }
        }

        // ================================================================ Paseo: barandal, palmeras, faroles, bancas, escaleras

        private static void Paseo(KitS2c.Obra o, Transform paseo, List<Vector3> borde)
        {
            float largo = ColocarT1354Curva.Longitud(borde);
            float dMuelle = DistanciaEnX(borde, XMuelle);
            float dPuesto = DistanciaEnX(borde, XPuesto);
            List<float> escaleras = ColocarT1354Curva.Cada(largo, PasoEscaleras, 40f);
            escaleras.Add(dMuelle);
            escaleras.RemoveAll(d => Mathf.Abs(d - dMuelle) < 20f && d != dMuelle);
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

            // Escaleras a la playa, con la cima a la altura de la banqueta.
            foreach (float d in escaleras)
            {
                Vector3 p = Punto(borde, d, ColocarT1354Curva.LateralMuro + 1.5f, out Quaternion giro);
                Poner(paseo, "ML02_EscaleraPlaya", true, p, giro, 1f, ColocarT1354Curva.YBanqueta, true);
            }

            // Palmeras y faroles alternados cada 12 m (Tripo); la zona del puesto y las escaleras quedan libres.
            foreach ((float d, bool palmera) in ColocarT1354Curva.Alternados(largo, PasoPaseo, 6f))
            {
                if (escaleras.Any(e => Mathf.Abs(d - e) < 4f) || Mathf.Abs(d - dPuesto) < 8f || Mathf.Abs(d - dMuelle) < 8f)
                {
                    continue;
                }
                Vector3 p = Punto(borde, d, LateralPaseo, out Quaternion giro);
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
                        KitS2c.Halo(o, farol.transform, p + Vector3.up * fb.size.y * 0.92f, ColocarT1354Curva.YBanqueta, KitS2c.Sodio);
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
                Poner(paseo, "PZ01_Banca", false, p, Quaternion.LookRotation(-(giro * Vector3.forward)), 1f, null, true);
            }
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

            // Escollera que sale al mar al final este, con el faro al fondo.
            float x = ColocarT1354Curva.U5.x + 8f;
            float zCentro = ColocarT1354Curva.U5.z;
            float orillaEste = ColocarT1354Curva.LateralOrilla(zCentro);
            float zFinal = ColocarT1354Curva.ZAgua - 26f;
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
            GameObject faro = Poner(costa, "ML04_Faro", false, new Vector3(x, -2.3f, zFinal - 2f), Quaternion.identity, 1f, null, true);
            if (faro != null)
            {
                Bounds fb = Medida(prefabs["ML04_Faro"]);
                // Una luz real: el haz. El giro necesita un componente de runtime (no es de esta tarea): queda fijo apuntando al oeste.
                Light haz = KitS2c.Luz(faro.transform, faro.transform.position + Vector3.up * fb.size.y * 0.85f, LightType.Spot, new Color(1f, 0.92f, 0.65f), 4000f, 140f, 30f, new Vector3(-1f, -0.02f, -0.25f));
                protegidas.Add(haz);
            }
        }

        // ================================================================ Muelle, lanchas y puesto

        private static void Muelle(KitS2c.Obra o, Transform muelle, Transform paseo, List<Vector3> borde)
        {
            float zCosta = ColocarT1354Curva.ZCentro(XMuelle) - ColocarT1354Curva.LateralMuro;
            Bounds recto = Medida(Modulo("ML03_MuelleRecto"));
            float paso = recto.size.x > 1f ? recto.size.x : 4f;
            const int tramos = 10;
            Quaternion haciaSur = Quaternion.Euler(0f, 90f, 0f);
            for (int i = 0; i < tramos; i++)
            {
                Vector3 p = new Vector3(XMuelle, YMuelle, zCosta - paso * (i + 0.5f));
                Poner(muelle, "ML03_MuelleRecto", true, p, haciaSur, 1f, YMuelle, i == 0);
            }
            float zEsquina = zCosta - paso * (tramos + 0.5f);
            Poner(muelle, "ML04_MuelleEsquina", true, new Vector3(XMuelle, YMuelle, zEsquina), haciaSur, 1f, YMuelle, true);
            for (int j = 0; j < 3; j++)
            {
                Poner(muelle, "ML03_MuelleRecto", true, new Vector3(XMuelle + paso * (j + 1), YMuelle, zEsquina), Quaternion.Euler(0f, 180f, 0f), 1f, YMuelle, false);
            }

            // Lanchas de colores amarradas al lado oeste del muelle (colores en Unity).
            for (int i = 0; i < ColoresLancha.Length; i++)
            {
                Vector3 p = new Vector3(XMuelle - 5f, ColocarT1354Curva.YAgua - 0.15f, zCosta - 6f - 8f * i);
                GameObject lancha = Poner(muelle, "ML02_Lancha", false, p, Quaternion.Euler(0f, 90f + o.Rango(-8f, 8f), 0f), 1f, null, i == 0);
                if (lancha != null)
                {
                    Tenir(lancha, ColoresLancha[i], i);
                }
            }

            // Puesto de mariscos en la banqueta junto al muelle, de frente a la avenida, con hieleras y bancos (sin modelo todavía: huecos).
            Vector3 puesto = Punto(borde, DistanciaEnX(borde, XPuesto), 11f, out Quaternion mar);
            GameObject go = Poner(paseo, "ML03_PuestoMariscos", false, puesto, Quaternion.LookRotation(-(mar * Vector3.forward)), 1f, null, true);
            if (go != null)
            {
                Light bombilla = KitS2c.Luz(go.transform, puesto + Vector3.up * 2.4f, LightType.Point, KitS2c.Calido, 6f, 9f, 0f);
                protegidas.Add(bombilla);
            }
            Vector3 lado = Vector3.Cross(Vector3.up, mar * Vector3.forward);
            for (int i = 0; i < 3; i++)
            {
                HuecoSimple(paseo, "Hielera", puesto + lado * (2.2f + i * 0.7f) + (mar * Vector3.forward) * 1.2f);
            }
            for (int i = 0; i < 2; i++)
            {
                HuecoSimple(paseo, "BancoPlastico", puesto - lado * (2.0f + i * 0.8f) - (mar * Vector3.forward) * 0.6f);
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

        private static List<Vector3> FachadasCiudad(KitS2c.Obra o, Transform casas)
        {
            List<Vector3> altos = new List<Vector3>();
            foreach ((float x0, float x1) in Fachadas)
            {
                List<Vector3> linea = ColocarT1354Curva.PuntosEn(x0, x1, 10f);
                for (int i = 0; i + 1 < linea.Count; i++)
                {
                    Vector3 tramo = linea[i + 1] - linea[i];
                    Vector3 hacia = ColocarT1354Curva.HaciaElMar(tramo.normalized);
                    Vector3 a = linea[i] + hacia * ColocarT1354Curva.LateralCiudad + Vector3.up * ColocarT1354Curva.YBanqueta;
                    Vector3 b = linea[i + 1] + hacia * ColocarT1354Curva.LateralCiudad + Vector3.up * ColocarT1354Curva.YBanqueta;
                    Vector3 normal = hacia;
                    Vector3 lado = Vector3.Cross(Vector3.up, normal).normalized;
                    List<Vector3> altosTramo = KitS2c.Frente(o, casas, a, b, normal, 8f, k => EstiloMalecon(o));
                    foreach (Vector3 alto in altosTramo)
                    {
                        altos.Add(alto);
                        if (o.Chance(0.45f))
                        {
                            KitS2c.Pieza(casas, "C10_AireAcondicionado", alto - Vector3.up * 3.4f + normal * 0.3f + lado * o.Rango(-1.4f, 1.4f), Quaternion.LookRotation(-normal), Vector3.one, false);
                        }
                    }
                }
            }
            return altos;
        }

        private static KitS2c.Estilo EstiloMalecon(KitS2c.Obra o)
        {
            return new KitS2c.Estilo
            {
                PlantaBaja = o.Chance(0.6f) ? KitS2c.PlantaLocal : KitS2c.PlantaVivienda,
                PisoAlto = o.Chance(0.15f) ? "C20b_PisoDosVentanas_cy" : KitS2c.PisoFachada,
                ColorPiso = o.Azar.Next(KitS2c.ColoresPiso.Length),
                Pisos = 3 + o.Azar.Next(3),
                Tinaco = o.Chance(0.5f),
                Foco = false,
                Neon = o.Chance(0.75f) ? (o.Chance(0.5f) ? KitS2c.Magenta : KitS2c.Turquesa) : (Color?)null,
                NeonBandera = o.Chance(0.7f),
                Antena = o.Chance(0.25f),
                AireAzotea = o.Chance(0.4f),
                Toldo = o.Chance(0.3f),
            };
        }

        private static void Postes(KitS2c.Obra o, Transform postes, List<Vector3> avenida, List<Vector3> altos)
        {
            float largo = ColocarT1354Curva.Longitud(avenida);
            List<Vector3> puntas = new List<Vector3>();
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
                puntas.Add(KitS2c.Poste(o, postes, pie, hacia, ColocarT1354Curva.YBanqueta, o.Chance(0.55f)));
            }
            for (int i = 0; i + 1 < puntas.Count; i++)
            {
                if (Vector3.Distance(puntas[i], puntas[i + 1]) < 40f)
                {
                    for (int k = 0; k < 3; k++)
                    {
                        Vector3 sube = Vector3.down * 0.45f * k;
                        KitS2c.Cable(o, puntas[i] + sube, puntas[i + 1] + sube, o.Rango(0.6f, 1.2f));
                    }
                }
            }
            foreach (Vector3 punta in puntas)
            {
                foreach (Vector3 alto in altos.Where(a => Vector3.Distance(a, punta) < 16f).Take(3))
                {
                    KitS2c.Cable(o, punta - Vector3.up * 0.3f, alto, o.Rango(0.3f, 0.8f));
                }
            }
        }

        // ================================================================ Mar y sondas

        private static void Mar(KitS2c.Obra o, Transform mar, List<Vector3> borde)
        {
            // Agua de x -700..700 y z -262..-900 (plan de zonas). El shader es de T-1351: se llama por reflexión.
            bool plano = LlamarMar("CrearPlanoMar", mar, new Vector3(0f, ColocarT1354Curva.YAgua, -581f), new Vector2(1400f, 638f), ColocarT1354Curva.YAgua);
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
                p = AssetDatabase.LoadAssetAtPath<GameObject>(CarpetaModulos + nombre + ".prefab");
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

        /// <summary>
        /// Deja a lo más <see cref="MaximoLuces"/> Light reales: las protegidas (faro, puesto) y las de postes repartidas a lo largo; las de neones y
        /// focos de las fachadas se quitan (ya brillan por su emisión).
        /// </summary>
        private static void LimitarLuces(Transform raiz)
        {
            List<Light> todas = raiz.GetComponentsInChildren<Light>(true).ToList();
            List<Light> deCalle = todas.Where(l => !protegidas.Contains(l) && l.GetComponentInParent<LuzNocturna>() != null && EsDePoste(l)).OrderBy(l => l.transform.position.x).ToList();
            int cupo = Mathf.Max(0, MaximoLuces - protegidas.Count(l => l != null));
            HashSet<Light> quedan = new HashSet<Light>(protegidas.Where(l => l != null));
            for (int i = 0; i < Mathf.Min(cupo, deCalle.Count); i++)
            {
                quedan.Add(deCalle[Mathf.Min(deCalle.Count - 1, Mathf.FloorToInt((i + 0.5f) * deCalle.Count / Mathf.Min(cupo, deCalle.Count)))]);
            }
            foreach (Light l in todas)
            {
                if (l != null && !quedan.Contains(l))
                {
                    Object.DestroyImmediate(l.gameObject);
                }
            }
        }

        private static bool EsDePoste(Light l)
        {
            for (Transform t = l.transform; t != null; t = t.parent)
            {
                if (t.name.StartsWith("Poste_", StringComparison.Ordinal))
                {
                    return true;
                }
            }
            return false;
        }

        private static void Estaticos(Transform raiz)
        {
            const StaticEditorFlags banderas = StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.OccluderStatic;
            foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true))
            {
                if (t == raiz || t.GetComponent<Light>() != null || t.GetComponent<LuzNocturna>() != null || t.GetComponent<ReflectionProbe>() != null || t.GetComponent<CalleZona>() != null)
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
