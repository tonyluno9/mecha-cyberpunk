using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MechaCyberpunk.Core.Mundo;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-1388: el detalle de las uniones entre las zonas nuevas (Malecón, Cerro, Autopista, Puerto) y los barrios viejos, en un prefab
    /// aparte (Assets/Arte/Mundo/Zonas/T1388_Uniones.prefab) que la escena Ciudad_Prologo instancia una sola vez.
    /// Todo el detalle va dentro del prefab (la escena solo guarda la referencia).
    ///
    /// Métodos (batchmode, cada uno en su propia corrida de Unity):
    ///   MechaCyberpunk.Arte.Editor.UnionesT1388.Aplicar   arma el prefab y lo instancia (si ya estaba, lo reemplaza). Escribe Logs/t1388-uniones.txt.
    ///   MechaCyberpunk.Arte.Editor.UnionesT1388.Validar   huecos, escalones, flotantes y colliders sin malla: Logs/t1388-validar.txt. No guarda.
    ///   MechaCyberpunk.Arte.Editor.UnionesT1388.Fotos     docs/capturas/t-1388/&lt;union&gt;-&lt;sentido&gt;.png a 1.7 m. No guarda.
    ///
    /// Variantes de los assets de T-1382 (se ven en imageassets/modelos-nuevos/nuevos-18nov/):
    ///   CC06 y CC07 "_a": volumen compacto con lona azul y sin escalera que sale a la banqueta (la "_b" de CC06 trae una escalera lateral).
    ///   ML05 "_b": colores y neones más cercanos a K11 (amarillo, azul y naranja con tejas) que la "_a" rosa y turquesa.
    ///   AU02, AU03, PU08 "_a": las de por defecto.
    /// </summary>
    public static class UnionesT1388
    {
        public const string NombreRaiz = "T1388_Uniones";
        public const string RutaPrefab = "Assets/Arte/Mundo/Zonas/T1388_Uniones.prefab";

        private const string Variante = "_a";
        private const string VarianteML05 = "_b";
        private const int Semilla = 1388;
        private const int TopeLuces = 38;
        private const float Escalon = 0.3f;
        private const string CarpetaFotos = "docs/capturas/t-1388/";
        private const string MatConcreto = "Assets/Arte/Materiales/Calle18nov/M_T09_ConcretoManchado.mat";
        private const string MatMojado = "Assets/Arte/Materiales/Calle18nov/M_T07_AsfaltoMojado.mat";

        private static readonly string[] ModelosCarro =
        {
            "Carro_V01a", "Carro_V02", "Carro_V03", "Carro_V04", "Carro_V05", "Carro_V06", "Carro_V07", "Carro_V08", "Carro_V12", "Carro_V13",
        };

        private static readonly string[] Contenedores = { "PT01_Contenedor40", "PT01_Contenedor40_Azul", "PT01_Contenedor40_Naranja", "PT01_Contenedor40_Rojo", "PT01_Contenedor40_Verde" };

        // ================================================================ Trazado de las uniones

        /// <summary>
        /// Una calle de enlace: línea del centro en planta (y = 0), ancho de calle y el estilo de cada lado. Cada lado puede cambiar de estilo
        /// ("A" antes de <see cref="Corte"/> y "B" después, mezclados en ±12 m) para que no se note dónde termina el barrio y empieza la zona.
        /// Izquierda y derecha son según el sentido de la línea (izquierda = (-dz, dx)).
        /// </summary>
        private sealed class Tramo
        {
            public string Nombre;
            public Vector3[] Pts;
            public float Ancho = 14f;
            public string IzqA, IzqB, DerA, DerB;
            public float Corte;
            public int Carros = 3;
            public bool Poste = true;
            public bool CosturaIni;
            public bool CosturaFin;
            public float Largo;
            private float[] acum;

            public void Calcular()
            {
                acum = new float[Pts.Length];
                for (int i = 1; i < Pts.Length; i++)
                {
                    acum[i] = acum[i - 1] + Vector3.Distance(Pts[i - 1], Pts[i]);
                }
                Largo = acum[Pts.Length - 1];
            }

            public void Muestra(float s, out Vector3 pos, out Vector3 dir)
            {
                s = Mathf.Clamp(s, 0f, Largo);
                for (int i = 1; i < Pts.Length; i++)
                {
                    if (s <= acum[i] || i == Pts.Length - 1)
                    {
                        float t = (s - acum[i - 1]) / Mathf.Max(0.001f, acum[i] - acum[i - 1]);
                        pos = Vector3.Lerp(Pts[i - 1], Pts[i], Mathf.Clamp01(t));
                        dir = Pts[i] - Pts[i - 1];
                        dir.y = 0f;
                        dir.Normalize();
                        return;
                    }
                }
                pos = Pts[0];
                dir = Vector3.forward;
            }

            public float Distancia(Vector3 p)
            {
                float mejor = float.MaxValue;
                for (int i = 1; i < Pts.Length; i++)
                {
                    mejor = Mathf.Min(mejor, DistanciaSegmento(p, Pts[i - 1], Pts[i]));
                }
                return mejor;
            }

            public string Estilo(bool izq, float s, System.Random azar)
            {
                string a = izq ? IzqA : DerA;
                string b = izq ? IzqB : DerB;
                if (string.IsNullOrEmpty(b))
                {
                    return a;
                }
                float p = Mathf.Clamp01((s - (Corte - 12f)) / 24f);
                return azar.NextDouble() < p ? b : a;
            }

            public float LatBanqueta => Ancho * 0.5f + 1.5f;
            public float LatFachada => Ancho * 0.5f + 3f;
            public float LatCarro => Ancho * 0.5f - 0.7f;
        }

        private static List<Tramo> tramos;

        private static List<Tramo> Tramos()
        {
            if (tramos != null)
            {
                return tramos;
            }
            tramos = new List<Tramo>
            {
                // Corredores sur: del barrio (z -130) a la avenida del Malecón (z -218). Hacia el sur cambia de barrio a Malecón.
                new Tramo { Nombre = "U1", Pts = new[] { V(-47f, -130f), V(-47f, -218f) }, IzqA = "Barrio", IzqB = "Malecon", DerA = "Barrio", DerB = "Malecon", Corte = 70f, Carros = 0, CosturaFin = true },
                new Tramo { Nombre = "U2", Pts = new[] { V(47f, -130f), V(47f, -218f) }, IzqA = "Barrio", IzqB = "Malecon", DerA = "Barrio", DerB = "Malecon", Corte = 70f, Carros = 0, CosturaFin = true },
                // U3: del Malecón (-300, -205) a la calle base del Cerro.
                new Tramo { Nombre = "U3", Pts = new[] { V(-300f, -205f), V(-330f, -150f), V(-305f, -120f) }, IzqA = "Malecon", IzqB = "Cerro", DerA = "Malecon", DerB = "Cerro", Corte = 35f, Carros = 2, CosturaIni = true, CosturaFin = true },
                // U4: del corredor oeste a la calle base. Calle oeste (x -242) y calle base (x -305).
                new Tramo { Nombre = "U4", Pts = new[] { V(-232f, 0f), V(-305f, 0f) }, IzqA = "Barrio", IzqB = "Cerro", DerA = "Barrio", DerB = "Cerro", Corte = 40f, Carros = 2, CosturaIni = true, CosturaFin = true },
                new Tramo { Nombre = "CalleOeste", Pts = new[] { V(-242f, -64f), V(-242f, 64f) }, IzqA = "Cerro", DerA = "Barrio", Carros = 4 },
                new Tramo { Nombre = "CalleBase", Pts = new[] { V(-305f, -118f), V(-305f, 70f) }, IzqA = "Cerro", DerA = "Barrio", Carros = 5 },
                // Cruzada norte y rampa norte (U7).
                new Tramo { Nombre = "CruzadaNorte", Pts = new[] { V(-30f, 194f), V(-107f, 194f) }, IzqA = "Barrio", DerA = "Barrio", Carros = 3 },
                new Tramo { Nombre = "U7", Pts = new[] { V(-107f, 194f), V(-120f, 215f) }, IzqA = "Barrio", IzqB = "Autopista", DerA = "Barrio", DerB = "Autopista", Corte = 8f, Carros = 0, CosturaFin = true },
                // Conector este (U9) y calle este hacia el puerta norte del Puerto (U8).
                new Tramo { Nombre = "U9", Pts = new[] { V(242f, 0f), V(300f, 0f) }, IzqA = "Barrio", IzqB = "Puerto", DerA = "Barrio", DerB = "Puerto", Corte = 35f, Carros = 2, CosturaIni = true, CosturaFin = true },
                new Tramo { Nombre = "U8", Pts = new[] { V(300f, 50f), V(300f, -42f) }, Ancho = 12f, IzqA = "Autopista", IzqB = "Puerto", DerA = "Barrio", DerB = "Puerto", Corte = 55f, Carros = 3, CosturaFin = true },
                // Puerta oeste del Puerto (U5), por fuera: avenida del Malecón, la línea real se toma de la CalleZona.
                new Tramo { Nombre = "U5", Pts = new[] { V(100f, -215f), V(158f, -215f) }, Ancho = 16f, IzqA = "Malecon", IzqB = "Puerto", DerA = "Mar", Corte = 30f, Carros = 2, CosturaFin = true },
            };
            foreach (Tramo t in tramos)
            {
                t.Calcular();
            }
            return tramos;
        }

        /// <summary>Cruces donde hay semáforos (esquinas en diagonal).</summary>
        private static readonly Vector3[] Cruces =
        {
            new Vector3(-242f, 0f, 0f), new Vector3(-305f, 0f, 0f), new Vector3(-47f, 0f, -194f), new Vector3(47f, 0f, -194f), new Vector3(300f, 0f, 0f),
        };

        private static Vector3 V(float x, float z) => new Vector3(x, 0f, z);

        // ================================================================ Estado de la corrida

        private static readonly Dictionary<string, GameObject> prefabs = new Dictionary<string, GameObject>();
        private static readonly Dictionary<GameObject, Vector3> tamanos = new Dictionary<GameObject, Vector3>();
        private static readonly HashSet<string> faltan = new HashSet<string>();
        private static readonly Dictionary<string, int> cuenta = new Dictionary<string, int>();
        private static readonly Dictionary<string, int> rechazos = new Dictionary<string, int>();
        private static Transform raizObra;

        private static void Contar(Dictionary<string, int> d, string clave)
        {
            d[clave] = d.TryGetValue(clave, out int n) ? n + 1 : 1;
        }

        // ================================================================ Aplicar

        public static void Aplicar()
        {
            ColocarS2.Abrir();
            ColocarS2.Informe.Clear();
            prefabs.Clear();
            tamanos.Clear();
            faltan.Clear();
            cuenta.Clear();
            rechazos.Clear();
            Tramos();
            UsarCalleZonaDeLaAvenida();
            Physics.SyncTransforms();

            KitS2c.Obra o = KitS2c.Empezar(NombreRaiz, Semilla);
            raizObra = o.Raiz;
            Transform banquetas = Hijo(o.Raiz, "Banquetas");
            Transform arroyos = Hijo(o.Raiz, "Arroyos");
            Transform fachadas = Hijo(o.Raiz, "Fachadas");
            Transform postes = Hijo(o.Raiz, "Postes");
            Transform aceras = Hijo(o.Raiz, "Banqueta_Props");
            Transform carros = Hijo(o.Raiz, "Carros");
            Transform cruces = Hijo(o.Raiz, "Semaforos");

            foreach (Tramo t in Tramos())
            {
                Banquetas(o, banquetas, t);
                Arroyos(o, arroyos, t);
                Fachadas(o, fachadas, t, true);
                Fachadas(o, fachadas, t, false);
                Postes(o, postes, t);
                Aceras(o, aceras, t);
                Carros(o, carros, t);
                Physics.SyncTransforms();
            }
            Semaforos(o, cruces);

            Estaticos(o.Raiz);
            KitS2c.Terminar(o);
            PrefabUtility.SaveAsPrefabAssetAndConnect(o.Raiz.gameObject, RutaPrefab, InteractionMode.AutomatedAction);

            ColocarS2.Informe.AppendLine("T-1388: prefab " + RutaPrefab);
            ColocarS2.Informe.AppendLine("Casas del kit: " + o.Casas + ", neones: " + o.Neones + ", postes: " + o.Postes + ", cables: " + o.Cables);
            ColocarS2.Informe.AppendLine("Luces reales: " + Luces(o.Raiz) + " (tope " + TopeLuces + ")");
            ColocarS2.Informe.AppendLine("Objetos: " + o.Raiz.GetComponentsInChildren<Transform>(true).Length);
            ColocarS2.Informe.AppendLine("Colocados: " + string.Join(", ", cuenta.OrderBy(k => k.Key).Select(k => k.Key + "=" + k.Value)));
            ColocarS2.Informe.AppendLine("Rechazados (chocaban, sin piso o en calle): " + string.Join(", ", rechazos.OrderBy(k => k.Key).Select(k => k.Key + "=" + k.Value)));
            ColocarS2.Informe.AppendLine("Assets que faltan (se usó el kit en su lugar): " + (faltan.Count == 0 ? "ninguno" : string.Join(", ", faltan.OrderBy(f => f))));
            ColocarS2.Guardar("t1388-uniones");
        }

        /// <summary>U5 sigue la avenida real del Malecón (CalleZona_Avenida) entre x 100 y 158, si está en la escena.</summary>
        private static void UsarCalleZonaDeLaAvenida()
        {
            Tramo u5 = Tramos().First(t => t.Nombre == "U5");
            foreach (CalleZona cz in Object.FindObjectsByType<CalleZona>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (cz.name != "CalleZona_Avenida")
                {
                    continue;
                }
                List<Vector3> p = cz.Puntos().Where(q => q.x >= 100f && q.x <= 158f).OrderBy(q => q.x).Select(q => new Vector3(q.x, 0f, q.z)).ToList();
                if (p.Count >= 2)
                {
                    u5.Pts = p.ToArray();
                    u5.Calcular();
                    ColocarS2.Informe.AppendLine("U5 sigue CalleZona_Avenida: " + p.Count + " puntos de " + p[0] + " a " + p[p.Count - 1]);
                }
                return;
            }
            ColocarS2.Informe.AppendLine("U5: no hay CalleZona_Avenida en la escena, se usa la recta z = -215");
        }

        // ================================================================ Piso: banquetas y arroyo

        /// <summary>
        /// Banqueta con guarnición donde falta: se mira con un rayo al centro de cada banqueta cada 2 m. Si no hay piso o solo hay el asfalto de
        /// respaldo (bajo 0.1 m), se pone una losa de M_T09 con su collider y tapa a 0.2 m, como las banquetas de las zonas (sin escalón).
        /// </summary>
        private static void Banquetas(KitS2c.Obra o, Transform padre, Tramo t)
        {
            Material mat = Cargar<Material>(MatConcreto) ?? KitS2c.MatBanqueta();
            for (int lado = -1; lado <= 1; lado += 2)
            {
                bool izq = lado < 0;
                string estilo = izq ? t.IzqA : t.DerA;
                if (estilo == "Mar")
                {
                    continue;
                }
                for (float s = 1f; s < t.Largo - 1f; s += 2f)
                {
                    t.Muestra(s, out Vector3 p, out Vector3 d);
                    Vector3 lat = Lateral(d, izq);
                    Vector3 c = p + lat * t.LatBanqueta;
                    if (EnOtraCalle(c, 0.2f, t))
                    {
                        continue;
                    }
                    bool hay = Piso(c, 0f, out float y, out Collider col);
                    bool respaldo = hay && (y < 0.1f);
                    if (hay && !respaldo)
                    {
                        continue;
                    }
                    if (hay && y < -0.3f)
                    {
                        continue;
                    }
                    GameObject losa = KitS2c.Losa(o, padre, "Banqueta_" + t.Nombre, new Vector3(c.x, 0.05f, c.z), Quaternion.LookRotation(d), new Vector3(3f, 0.3f, 2.05f), mat, 2f, true);
                    Contar(cuenta, "banqueta (losas de 2 m)");
                }
            }
        }

        /// <summary>Parches de asfalto mojado (M_T07) en las costuras: 10 m sobre la calle, a 2 cm, sin collider, para que no se note el empalme.</summary>
        private static void Arroyos(KitS2c.Obra o, Transform padre, Tramo t)
        {
            Material mat = Cargar<Material>(MatMojado) ?? KitS2c.MatAsfalto();
            foreach ((bool pone, float s) in new[] { (t.CosturaIni, 5f), (t.CosturaFin, t.Largo - 5f) })
            {
                if (!pone)
                {
                    continue;
                }
                t.Muestra(s, out Vector3 p, out Vector3 d);
                if (!Piso(p, 0f, out float y, out Collider _) || y < -0.1f || y > 0.3f)
                {
                    Contar(rechazos, "arroyo (sin piso)");
                    continue;
                }
                KitS2c.Losa(o, padre, "Arroyo_" + t.Nombre, new Vector3(p.x, y + 0.02f, p.z), Quaternion.LookRotation(d), new Vector3(t.Ancho - 1.2f, 0.02f, 10f), mat, 4f, false);
                Contar(cuenta, "arroyo mojado");
            }
        }

        // ================================================================ Fachadas y edificios de la zona

        private static void Fachadas(KitS2c.Obra o, Transform padre, Tramo t, bool izq)
        {
            float s = 2f;
            for (int guardia = 0; s < t.Largo - 2f && guardia < 300; guardia++)
            {
                t.Muestra(s, out Vector3 p, out Vector3 d);
                Vector3 lat = Lateral(d, izq);
                string estilo = t.Estilo(izq, s, o.Azar);
                if (string.IsNullOrEmpty(estilo) || estilo == "Mar")
                {
                    s += 6f;
                    continue;
                }
                Vector3 ancla = p + lat * t.LatFachada;
                float usado = PonerFachada(o, padre, t, estilo, ancla, -lat, d, s);
                s += usado > 0f ? usado + (float)o.Azar.NextDouble() * 1.2f : 3.5f;
            }
        }

        /// <summary>Un edificio o fachada del estilo pedido, con su cara a la calle sobre la línea <paramref name="ancla"/>. Devuelve el ancho usado (0 si no cupo).</summary>
        private static float PonerFachada(KitS2c.Obra o, Transform padre, Tramo t, string estilo, Vector3 ancla, Vector3 normal, Vector3 dir, float s)
        {
            GameObject go = null;
            float ancho = 0f;
            switch (estilo)
            {
                case "Cerro":
                {
                    string[] c = { "CC06_CasaCerro" + Variante, "CC07_CasaCerro" + Variante, "CC01_CasaCerro", "CC02_CasaCerro" };
                    GameObject pf = Cargar(o.Uno(c));
                    ancho = o.Rango(6.5f, 9f);
                    go = pf == null ? null : Fachada(padre, pf, ancla, normal, ancho, false);
                    if (go != null)
                    {
                        AdornoNeon(o, go, ancla, normal, dir, 0.3f);
                    }
                    break;
                }
                case "Malecon":
                {
                    GameObject pf = Cargar("ML05_EdificioMalecon" + VarianteML05) ?? Cargar("ML05_EdificioMalecon" + Variante);
                    ancho = o.Rango(8.5f, 10f);
                    go = pf == null ? null : Fachada(padre, pf, ancla, normal, ancho, false);
                    if (go != null)
                    {
                        AdornoNeon(o, go, ancla, normal, dir, 0.6f);
                    }
                    break;
                }
                case "Puerto":
                {
                    double r = o.Azar.NextDouble();
                    if (r < 0.3)
                    {
                        GameObject pf = Cargar(o.Chance(0.5f) ? "PU03_Bodega" : "PU04_Bodega");
                        ancho = o.Rango(11f, 14f);
                        go = pf == null ? null : Fachada(padre, pf, ancla, normal, ancho, false);
                        if (go != null)
                        {
                            AdornoNeon(o, go, ancla, normal, dir, 0.5f);
                        }
                    }
                    else
                    {
                        GameObject pf = o.Chance(0.3f) ? Cargar("PU08_ContenedorGraffiti" + Variante) : null;
                        pf = pf ?? Cargar(o.Uno(Contenedores));
                        ancho = 12.2f;
                        go = pf == null ? null : Fachada(padre, pf, ancla, normal, ancho, true);
                        if (go != null && o.Chance(0.4f))
                        {
                            Apilar(padre, pf, go);
                        }
                    }
                    break;
                }
                case "Autopista":
                {
                    GameObject pf = Cargar(o.Chance(0.6f) ? "AU02_BarreraConcreto" + Variante : "AU03_ChatarraPuente" + Variante);
                    ancho = o.Rango(5.5f, 7f);
                    go = pf == null ? null : Fachada(padre, pf, ancla - normal * 0f, normal, ancho, false);
                    break;
                }
                default:
                    break;
            }
            if (go != null)
            {
                Contar(cuenta, "fachada " + estilo);
                return ancho;
            }
            // Sin asset (o no cupo): el kit de casas de los barrios.
            if (estilo != "Barrio" && estilo != "Autopista")
            {
                faltanSi(estilo);
            }
            return CasaKit(o, padre, t, ancla, normal, dir, estilo);
        }

        private static void faltanSi(string estilo)
        {
            Contar(rechazos, "fachada " + estilo + " (asset o espacio)");
        }

        /// <summary>Casa del kit de los barrios (planta baja y pisos, neón chico), si cabe. Devuelve el ancho usado o 0.</summary>
        private static float CasaKit(KitS2c.Obra o, Transform padre, Tramo t, Vector3 ancla, Vector3 normal, Vector3 dir, string estilo)
        {
            float ancho = o.Rango(5f, 7f);
            const float fondo = 8f;
            Vector3 tang = new Vector3(-normal.z, 0f, normal.x);
            if (!Piso(ancla, 0f, out float y, out Collider _) || y < -0.15f || y > 0.5f)
            {
                Contar(rechazos, "casa kit (sin piso)");
                return 0f;
            }
            foreach (float k in new[] { -0.5f, 0.5f })
            {
                if (!Piso(ancla + tang * (k * ancho), 0f, out float y2, out Collider _) || Mathf.Abs(y2 - y) > 0.25f)
                {
                    Contar(rechazos, "casa kit (desnivel)");
                    return 0f;
                }
            }
            Vector3 centroCaja = ancla - normal * (fondo * 0.5f) + Vector3.up * (y + 2f);
            if (OcupadaCaja(centroCaja, new Vector3(ancho * 0.5f - 0.1f, 1.5f, fondo * 0.5f - 0.1f), Quaternion.LookRotation(normal), null)
                || EnOtraCalle(ancla, 0.5f, t) || EnCalleZona(ancla, 0.3f))
            {
                Contar(rechazos, "casa kit (choca)");
                return 0f;
            }
            KitS2c.Estilo e = KitS2c.EstiloAlAzar(o);
            e.Foco = false;
            e.Pisos = o.Chance(0.4f) ? 3 : 2;
            if (Luces(o.Raiz) >= TopeLuces)
            {
                e.Neon = null;
            }
            KitS2c.Casa(o, padre, new Vector3(ancla.x, y, ancla.z), normal, ancho, fondo, e);
            Contar(cuenta, "casa kit " + estilo);
            return ancho;
        }

        /// <summary>Fachada con la cara (-Z del modelo) a la calle y la parte de atrás sobre la línea. Mide, escala, asienta en el piso y revisa choques.</summary>
        private static GameObject Fachada(Transform padre, GameObject prefab, Vector3 ancla, Vector3 normal, float anchoObjetivo, bool largoAlLado)
        {
            Vector3 tam = TamLocal(prefab);
            if (tam.x < 0.1f || tam.z < 0.1f)
            {
                return null;
            }
            Quaternion rot = Quaternion.LookRotation(-normal);
            float dimLado = tam.x;
            if (largoAlLado && tam.z > tam.x)
            {
                rot *= Quaternion.Euler(0f, 90f, 0f);
                dimLado = tam.z;
            }
            float escala = Mathf.Clamp(anchoObjetivo / dimLado, 0.6f, 1.7f);
            float fondoModelo = (largoAlLado && tam.z > tam.x) ? tam.x : tam.z;
            Vector3 centro = ancla - normal * (fondoModelo * escala * 0.5f + 0.1f);
            return Poner(padre, prefab, centro, rot, escala, true, false);
        }

        /// <summary>Un segundo contenedor encima del primero (misma huella).</summary>
        private static void Apilar(Transform padre, GameObject prefab, GameObject abajo)
        {
            Bounds b = Caja(abajo);
            GameObject arriba = (GameObject)PrefabUtility.InstantiatePrefab(prefab, padre);
            arriba.transform.SetPositionAndRotation(abajo.transform.position, abajo.transform.rotation);
            arriba.transform.localScale = abajo.transform.localScale;
            Bounds b2 = Caja(arriba);
            arriba.transform.position += new Vector3(0f, b.max.y - b2.min.y, 0f);
            Physics.SyncTransforms();
            if (Choca(arriba))
            {
                Object.DestroyImmediate(arriba);
                return;
            }
            Contar(cuenta, "contenedor apilado");
        }

        private static void AdornoNeon(KitS2c.Obra o, GameObject go, Vector3 ancla, Vector3 normal, Vector3 dir, float probabilidad)
        {
            if (!o.Chance(probabilidad) || Luces(o.Raiz) >= TopeLuces)
            {
                return;
            }
            Bounds b = Caja(go);
            Vector3 tang = new Vector3(-normal.z, 0f, normal.x);
            Vector3 p = new Vector3(ancla.x, b.min.y + 3.3f, ancla.z) + tang * o.Rango(-1.5f, 1.5f);
            KitS2c.Neon(o, go.transform, p, normal, tang, o.Chance(0.5f) ? KitS2c.Magenta : KitS2c.Turquesa, true);
        }

        // ================================================================ Postes con cables

        private static void Postes(KitS2c.Obra o, Transform padre, Tramo t)
        {
            if (!t.Poste)
            {
                return;
            }
            Vector3? anteriorIzq = null;
            Vector3? anteriorDer = null;
            for (float s = 12f; s < t.Largo - 8f; s += 40f)
            {
                t.Muestra(s, out Vector3 p, out Vector3 d);
                Vector3 izq = Lateral(d, true);
                Vector3 pieIzq = p + izq * t.LatBanqueta;
                Vector3 pieDer = p - izq * t.LatBanqueta;
                if (!PuedePoste(t, pieIzq) || !PuedePoste(t, pieDer))
                {
                    Contar(rechazos, "poste (par)");
                    continue;
                }
                Vector3 puntaIzq = PonerPoste(o, padre, pieIzq, -izq, true);
                Vector3 puntaDer = PonerPoste(o, padre, pieDer, izq, false);
                KitS2c.Cable(o, puntaIzq, puntaDer, 1.2f);
                KitS2c.Cable(o, puntaIzq - Vector3.up * 0.45f, puntaDer - Vector3.up * 0.45f, 1.0f);
                if (anteriorIzq.HasValue)
                {
                    KitS2c.Cable(o, anteriorIzq.Value, puntaIzq, 1.1f);
                    KitS2c.Cable(o, anteriorDer.Value, puntaDer, 1.1f);
                }
                anteriorIzq = puntaIzq;
                anteriorDer = puntaDer;
                Contar(cuenta, "par de postes con cables");
            }
        }

        private static bool PuedePoste(Tramo t, Vector3 pie)
        {
            if (!Piso(pie, 0f, out float y, out Collider _) || y < 0.05f || y > 0.5f)
            {
                return false;
            }
            return !EnOtraCalle(pie, 1.5f, t) && !EnCalleZona(pie, 0.3f) && !OcupadaCaja(new Vector3(pie.x, y + 2f, pie.z), new Vector3(0.4f, 1.5f, 0.4f), Quaternion.identity, null);
        }

        private static Vector3 PonerPoste(KitS2c.Obra o, Transform padre, Vector3 pie, Vector3 haciaCalle, bool conLuz)
        {
            Piso(pie, 0f, out float y, out Collider _);
            Vector3 punta = KitS2c.Poste(o, padre, new Vector3(pie.x, y, pie.z), haciaCalle, y, false);
            Transform poste = padre.GetChild(padre.childCount - 1);
            if (!conLuz || Luces(o.Raiz) > TopeLuces)
            {
                foreach (Light l in poste.GetComponentsInChildren<Light>(true).ToArray())
                {
                    Object.DestroyImmediate(l.gameObject);
                }
                Transform halo = poste.Find("Halo");
                if (halo != null)
                {
                    Object.DestroyImmediate(halo.gameObject);
                }
            }
            return punta;
        }

        // ================================================================ Detalle de banqueta, alcantarillas y carros

        private static void Aceras(KitS2c.Obra o, Transform padre, Tramo t)
        {
            string[] basura = { "BS01a_Basura", "BS01b_Basura", "BS01c_Basura", "BS01d_Basura", "BS01e_Basura" };
            for (int lado = 0; lado < 2; lado++)
            {
                bool izq = lado == 0;
                string estilo = izq ? t.IzqA : t.DerA;
                if (estilo == "Mar")
                {
                    continue;
                }
                float s = o.Rango(4f, 9f);
                while (s < t.Largo - 4f)
                {
                    t.Muestra(s, out Vector3 p, out Vector3 d);
                    Vector3 lat = Lateral(d, izq);
                    Vector3 acera = p + lat * (t.LatBanqueta + o.Rango(-0.5f, 0.6f));
                    Quaternion haciaCalle = Quaternion.LookRotation(lat);
                    double r = o.Azar.NextDouble();
                    if (r < 0.2)
                    {
                        // Bolardos en la guarnición.
                        for (int k = -1; k <= 1; k += 2)
                        {
                            Colocar(padre, "BL01_Bolardo", p + lat * (t.Ancho * 0.5f + 0.5f) + d * (k * 1.4f), haciaCalle, 1f, t, "bolardo");
                        }
                    }
                    else if (r < 0.4)
                    {
                        Colocar(padre, "BT01_BoteBasura", acera, haciaCalle, 1f, t, "bote de basura");
                    }
                    else if (r < 0.58)
                    {
                        Colocar(padre, o.Uno(basura), acera, Quaternion.Euler(0f, o.Rango(0f, 360f), 0f), 1f, t, "basura");
                    }
                    else if (r < 0.72)
                    {
                        Colocar(padre, "C20d_Macetas", acera, haciaCalle, 1f, t, "macetas");
                    }
                    else if (r < 0.82)
                    {
                        Colocar(padre, o.Chance(0.5f) ? "C16_Tambo" : "C17_Huacales", acera, Quaternion.Euler(0f, o.Rango(0f, 360f), 0f), 1f, t, "tambo o huacales");
                    }
                    else if (estilo == "Malecon" || estilo == "Puerto" && o.Chance(0.3f))
                    {
                        Colocar(padre, "AR03_Palmera", acera, Quaternion.Euler(0f, o.Rango(0f, 360f), 0f), o.Rango(0.9f, 1.1f), t, "palmera");
                    }
                    // Alcantarilla al ras en el costado de la calle (a 5.8 m del eje, fuera del carril de circulación).
                    if (o.Chance(0.35f))
                    {
                        Colocar(padre, "AL01_Alcantarilla", p + lat * Mathf.Min(5.8f, t.Ancho * 0.5f - 1f), Quaternion.Euler(0f, o.Rango(0f, 90f), 0f), 1f, t, "alcantarilla", false, true);
                    }
                    s += o.Rango(9f, 16f);
                }
            }
        }

        private static void Carros(KitS2c.Obra o, Transform padre, Tramo t)
        {
            int puestos = 0;
            for (int intento = 0; intento < t.Carros * 8 && puestos < t.Carros; intento++)
            {
                float s = o.Rango(10f, Mathf.Max(11f, t.Largo - 10f));
                bool izq = o.Chance(0.5f);
                t.Muestra(s, out Vector3 p, out Vector3 d);
                Vector3 pos = p + Lateral(d, izq) * t.LatCarro;
                if (EnOtraCalle(pos, 2.5f, t))
                {
                    continue;
                }
                Quaternion rot = Quaternion.LookRotation(o.Chance(0.5f) ? d : -d);
                GameObject car = Colocar(padre, o.Uno(ModelosCarro), pos, rot, 1f, t, "carro", true, false, true);
                if (car != null)
                {
                    foreach (Rigidbody rb in car.GetComponentsInChildren<Rigidbody>(true))
                    {
                        rb.isKinematic = true;
                    }
                    puestos++;
                }
            }
        }

        private static void Semaforos(KitS2c.Obra o, Transform padre)
        {
            foreach (Vector3 c in Cruces)
            {
                foreach (Vector3 esquina in new[] { new Vector3(8.5f, 0f, 8.5f), new Vector3(-8.5f, 0f, -8.5f) })
                {
                    Vector3 pos = c + esquina;
                    if (!Piso(pos, 0f, out float y, out Collider _) || y < 0.1f || y > 0.4f)
                    {
                        Contar(rechazos, "semaforo (sin banqueta)");
                        continue;
                    }
                    GameObject pf = Cargar("ES01_Semaforo");
                    if (pf == null)
                    {
                        continue;
                    }
                    GameObject s = Poner(padre, pf, pos, Quaternion.LookRotation(c - pos), 1f, true, false);
                    Contar(s != null ? cuenta : rechazos, "semaforo");
                }
            }
        }

        // ================================================================ Colocar con revisión

        /// <summary>Un prop del kit de calle (o de zona) en un punto: lo asienta en el piso y lo descarta si choca o cae en otra calle.</summary>
        private static GameObject Colocar(Transform padre, string codigo, Vector3 pos, Quaternion rot, float escala, Tramo t, string clase, bool carro = false, bool alRas = false, bool exentoDeCarril = false)
        {
            GameObject pf = Cargar(codigo);
            if (pf == null)
            {
                return null;
            }
            if (!alRas && (EnOtraCalle(pos, 1f, t) || (!exentoDeCarril && EnCalleZona(pos, 0.3f))))
            {
                Contar(rechazos, clase + " (en calle)");
                return null;
            }
            GameObject go = Poner(padre, pf, pos, rot, escala, !alRas, alRas);
            Contar(go != null ? cuenta : rechazos, clase);
            return go;
        }

        /// <summary>
        /// Instancia el prefab con su centro (en planta) en <paramref name="centro"/>, asentado sobre el piso que hay ahí (rayo hacia abajo).
        /// Si no hay piso a ±0.6 m del nivel de calle, o si choca con otro collider, lo borra y devuelve null.
        /// </summary>
        private static GameObject Poner(Transform padre, GameObject prefab, Vector3 centro, Quaternion rot, float escala, bool revisar, bool sinColliders)
        {
            if (!Piso(centro, 0f, out float y, out Collider _) || y < -0.15f || y > 0.6f)
            {
                return null;
            }
            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, padre);
            go.transform.SetPositionAndRotation(new Vector3(centro.x, y, centro.z), rot);
            go.transform.localScale = Vector3.one * escala;
            Bounds b = Caja(go);
            go.transform.position += new Vector3(centro.x - b.center.x, y - 0.02f - b.min.y, centro.z - b.center.z);
            if (sinColliders)
            {
                foreach (Collider c in go.GetComponentsInChildren<Collider>(true))
                {
                    c.enabled = false;
                }
            }
            Physics.SyncTransforms();
            if (revisar && Choca(go))
            {
                Object.DestroyImmediate(go);
                return null;
            }
            return go;
        }

        // ================================================================ Mediciones

        private static T Cargar<T>(string ruta) where T : Object
        {
            T a = AssetDatabase.LoadAssetAtPath<T>(ruta);
            if (a == null)
            {
                faltan.Add(ruta);
            }
            return a;
        }

        private static GameObject Cargar(string codigo)
        {
            if (prefabs.TryGetValue(codigo, out GameObject ya))
            {
                return ya;
            }
            string carpeta = codigo.StartsWith("PT01") ? "Assets/Arte/Zonas/Modulos/" : codigo.StartsWith("PU08") || codigo.StartsWith("PU0") || codigo.StartsWith("AR") || codigo.StartsWith("AU") || codigo.StartsWith("CC") || codigo.StartsWith("ML") || codigo.StartsWith("PZ")
                ? "Assets/Arte/Zonas/" + codigo.Substring(0, 2) + "/"
                : null;
            GameObject pf = null;
            if (carpeta != null)
            {
                pf = AssetDatabase.LoadAssetAtPath<GameObject>(carpeta + codigo + ".prefab");
            }
            else
            {
                pf = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Arte/Calle/Prefabs/" + codigo + ".prefab");
            }
            if (pf == null && codigo.StartsWith("Carro_"))
            {
                pf = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Proyecto/Vehiculos/Prefabs/" + codigo + ".prefab");
            }
            if (pf == null)
            {
                faltan.Add(codigo);
            }
            prefabs[codigo] = pf;
            return pf;
        }

        private static Vector3 TamLocal(GameObject prefab)
        {
            if (tamanos.TryGetValue(prefab, out Vector3 ya))
            {
                return ya;
            }
            GameObject tmp = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            tmp.transform.SetPositionAndRotation(new Vector3(0f, -4000f, 0f), Quaternion.identity);
            Vector3 tam = Caja(tmp).size;
            Object.DestroyImmediate(tmp);
            tamanos[prefab] = tam;
            return tam;
        }

        private static Bounds Caja(GameObject go)
        {
            Renderer[] rs = go.GetComponentsInChildren<Renderer>(false);
            if (rs.Length == 0)
            {
                return new Bounds(go.transform.position, Vector3.zero);
            }
            Bounds b = rs[0].bounds;
            foreach (Renderer r in rs)
            {
                b.Encapsulate(r.bounds);
            }
            return b;
        }

        private static Transform Hijo(Transform padre, string nombre)
        {
            Transform t = new GameObject(nombre).transform;
            t.SetParent(padre, false);
            return t;
        }

        private static Vector3 Lateral(Vector3 dir, bool izq)
        {
            Vector3 l = new Vector3(-dir.z, 0f, dir.x);
            return izq ? l : -l;
        }

        private static float DistanciaSegmento(Vector3 p, Vector3 a, Vector3 b)
        {
            p.y = 0f;
            a.y = 0f;
            b.y = 0f;
            Vector3 ab = b - a;
            float l2 = ab.sqrMagnitude;
            float u = l2 < 0.0001f ? 0f : Mathf.Clamp01(Vector3.Dot(p - a, ab) / l2);
            return Vector3.Distance(p, a + ab * u);
        }

        private static int Luces(Transform raiz) => raiz.GetComponentsInChildren<Light>(true).Length;

        /// <summary>
        /// Un collider es "piso" si es una losa o tira delgada y ancha (calle, banqueta, parche de respaldo) o el terreno del cerro: no cuenta
        /// lo que se camina por encima (bolardos, postes, puestos).
        /// </summary>
        private static bool EsPiso(Collider c)
        {
            Vector3 s = c.bounds.size;
            if (s.y < 0.6f && Mathf.Max(s.x, s.z) > 1.5f)
            {
                return true;
            }
            string n = c.name;
            return n.Contains("Terreno") || n.Contains("Ladera");
        }

        /// <summary>Piso más alto bajo el punto, entre yRef + 6 y yRef - 8 m. false si no hay piso.</summary>
        private static bool Piso(Vector3 p, float yRef, out float y, out Collider col)
        {
            y = 0f;
            col = null;
            RaycastHit[] hs = Physics.RaycastAll(new Vector3(p.x, yRef + 6f, p.z), Vector3.down, 14f, ~0, QueryTriggerInteraction.Ignore);
            float mejor = float.MinValue;
            foreach (RaycastHit h in hs)
            {
                if (EsPiso(h.collider) && h.point.y > mejor)
                {
                    mejor = h.point.y;
                    col = h.collider;
                }
            }
            if (col == null)
            {
                return false;
            }
            y = mejor;
            return true;
        }

        private static bool EnOtraCalle(Vector3 p, float margen, Tramo propio)
        {
            foreach (Tramo t in Tramos())
            {
                if (t != propio && t.Distancia(p) < t.Ancho * 0.5f + margen)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>Dentro del carril de alguna CalleZona al nivel del suelo (las elevadas se ignoran).</summary>
        private static bool EnCalleZona(Vector3 p, float margen)
        {
            foreach (CalleZona cz in Object.FindObjectsByType<CalleZona>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                List<Vector3> pts = cz.Puntos();
                for (int i = 1; i < pts.Count; i++)
                {
                    if (Mathf.Abs((pts[i].y + pts[i - 1].y) * 0.5f) > 3f)
                    {
                        continue;
                    }
                    if (DistanciaSegmento(p, pts[i - 1], pts[i]) < cz.Ancho * 0.5f - margen - 0.2f)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private static bool OcupadaCaja(Vector3 centro, Vector3 mitad, Quaternion rot, Transform ignora)
        {
            Physics.SyncTransforms();
            foreach (Collider c in Physics.OverlapBox(centro, mitad, rot, ~0, QueryTriggerInteraction.Ignore))
            {
                if (ignora != null && c.transform.IsChildOf(ignora))
                {
                    continue;
                }
                if (EsPiso(c))
                {
                    continue;
                }
                return true;
            }
            return false;
        }

        /// <summary>¿El objeto ya puesto choca con algún collider que no sea piso ni suyo? La caja se achica 0.1 m y empieza 0.45 m sobre su base.</summary>
        private static bool Choca(GameObject go)
        {
            Bounds b = Caja(go);
            if (b.size == Vector3.zero)
            {
                return false;
            }
            float y0 = b.min.y + 0.45f;
            if (b.max.y <= y0 + 0.05f)
            {
                return false;
            }
            Vector3 centro = new Vector3(b.center.x, (y0 + b.max.y) * 0.5f, b.center.z);
            Vector3 mitad = new Vector3(Mathf.Max(0.05f, b.extents.x - 0.1f), (b.max.y - y0) * 0.5f, Mathf.Max(0.05f, b.extents.z - 0.1f));
            foreach (Collider c in Physics.OverlapBox(centro, mitad, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore))
            {
                if (c.transform.IsChildOf(go.transform) || EsPiso(c))
                {
                    continue;
                }
                if (c.bounds.max.y < b.min.y + 0.45f)
                {
                    continue;
                }
                return true;
            }
            return false;
        }

        private static void Estaticos(Transform raiz)
        {
            const StaticEditorFlags banderas = StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic;
            foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true))
            {
                if (t == raiz || t.GetComponent<Light>() != null || t.GetComponent<MechaCyberpunk.Mundo.Reloj.LuzNocturna>() != null)
                {
                    continue;
                }
                GameObjectUtility.SetStaticEditorFlags(t.gameObject, banderas);
            }
        }

        // ================================================================ Validar

        /// <summary>
        /// Recorre cada unión con un rayo cada 2 m a lo largo y a lo ancho (calle y banquetas) y busca huecos, escalones de más de 0.3 m,
        /// objetos que flotan más de 0.1 m (o están enterrados más de 0.3 m) y colliders sin malla visible. Escribe Logs/t1388-validar.txt. No guarda.
        /// </summary>
        public static void Validar()
        {
            ColocarS2.Abrir();
            Physics.SyncTransforms();
            Tramos();
            UsarCalleZonaDeLaAvenida();
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("T-1388 validar uniones");
            int huecos = 0, respaldo = 0, escalones = 0, flotan = 0, enterrados = 0, sinMalla = 0;
            const int maximoLineas = 60;

            foreach (Tramo t in Tramos())
            {
                int columnas = Mathf.CeilToInt((t.Ancho + 6f) / 2f) + 1;
                int filas = Mathf.CeilToInt(t.Largo / 2f) + 1;
                float[,] ys = new float[filas, columnas];
                for (int i = 0; i < filas; i++)
                {
                    t.Muestra(Mathf.Min(i * 2f, t.Largo), out Vector3 p, out Vector3 d);
                    Vector3 izq = Lateral(d, true);
                    for (int j = 0; j < columnas; j++)
                    {
                        float lat = -(t.Ancho * 0.5f + 3f) + j * 2f;
                        Vector3 q = p + izq * lat;
                        if (!Piso(q, 0f, out float y, out Collider col))
                        {
                            ys[i, j] = float.NaN;
                            huecos++;
                            if (huecos <= maximoLineas)
                            {
                                sb.AppendLine("HUECO " + t.Nombre + " s=" + (i * 2) + " lat=" + lat.ToString("F0") + " en " + q.ToString("F1"));
                            }
                            continue;
                        }
                        ys[i, j] = y;
                        if (col.name.StartsWith("PisoRespaldo"))
                        {
                            respaldo++;
                            if (respaldo <= maximoLineas)
                            {
                                sb.AppendLine("SOLO RESPALDO (sin calle ni banqueta) " + t.Nombre + " s=" + (i * 2) + " lat=" + lat.ToString("F0") + " en " + q.ToString("F1") + " y=" + y.ToString("F2"));
                            }
                        }
                    }
                }
                for (int i = 0; i < filas; i++)
                {
                    for (int j = 0; j < columnas; j++)
                    {
                        if (float.IsNaN(ys[i, j]))
                        {
                            continue;
                        }
                        foreach ((int di, int dj) in new[] { (1, 0), (0, 1) })
                        {
                            int a = i + di, b = j + dj;
                            if (a >= filas || b >= columnas || float.IsNaN(ys[a, b]))
                            {
                                continue;
                            }
                            float dif = Mathf.Abs(ys[a, b] - ys[i, j]);
                            if (dif > Escalon)
                            {
                                escalones++;
                                if (escalones <= maximoLineas)
                                {
                                    sb.AppendLine("ESCALON " + t.Nombre + " " + dif.ToString("F2") + " m entre s=" + (i * 2) + "/lat#" + j + " y s=" + (a * 2) + "/lat#" + b);
                                }
                            }
                        }
                    }
                }
            }

            GameObject raiz = GameObject.Find(NombreRaiz);
            if (raiz == null)
            {
                sb.AppendLine("NO HAY " + NombreRaiz + " en la escena: corre Aplicar primero.");
            }
            else
            {
                string[] excusados = { "Cables", "Luz", "Neon", "Halo", "Letrero", "Arroyo", "Banqueta_", "Foco" };
                foreach (Transform contenedor in raiz.transform)
                {
                    if (contenedor.name == "Cables" || contenedor.name == "Arroyos" || contenedor.name == "Banquetas")
                    {
                        continue;
                    }
                    foreach (Transform prop in contenedor)
                    {
                        if (excusados.Any(x => prop.name.StartsWith(x)))
                        {
                            continue;
                        }
                        Renderer[] rs = prop.GetComponentsInChildren<Renderer>(false);
                        if (rs.Length == 0)
                        {
                            continue;
                        }
                        Bounds b = rs[0].bounds;
                        foreach (Renderer r in rs)
                        {
                            b.Encapsulate(r.bounds);
                        }
                        if (!Piso(new Vector3(b.center.x, 0f, b.center.z), 0f, out float y, out Collider _))
                        {
                            continue;
                        }
                        float sobre = b.min.y - y;
                        if (sobre > 0.1f && prop.name != "Cuerpo")
                        {
                            flotan++;
                            if (flotan <= maximoLineas)
                            {
                                sb.AppendLine("FLOTA " + sobre.ToString("F2") + " m: " + Ruta(prop) + " en " + prop.position.ToString("F1"));
                            }
                        }
                        else if (-sobre > 0.3f)
                        {
                            enterrados++;
                            if (enterrados <= maximoLineas)
                            {
                                sb.AppendLine("ENTERRADO " + (-sobre).ToString("F2") + " m: " + Ruta(prop) + " en " + prop.position.ToString("F1"));
                            }
                        }
                    }
                }
                foreach (Collider c in raiz.GetComponentsInChildren<Collider>(true))
                {
                    if (!c.enabled || c.isTrigger)
                    {
                        continue;
                    }
                    Bounds cb = c.bounds;
                    bool visible = c.GetComponentsInChildren<Renderer>(false).Any(r => r.enabled && r.bounds.Intersects(cb));
                    if (!visible)
                    {
                        sinMalla++;
                        if (sinMalla <= maximoLineas)
                        {
                            sb.AppendLine("COLLIDER SIN MALLA " + Ruta(c.transform) + " en " + c.transform.position.ToString("F1"));
                        }
                    }
                }
                sb.AppendLine("Luces del prefab: " + Luces(raiz.transform) + " (tope " + TopeLuces + ")");
                sb.AppendLine("Objetos: " + raiz.GetComponentsInChildren<Transform>(true).Length);
            }
            sb.AppendLine();
            sb.AppendLine("RESUMEN: huecos " + huecos + ", solo-respaldo " + respaldo + ", escalones>0.3 m " + escalones + ", flotantes>0.1 m " + flotan + ", enterrados>0.3 m " + enterrados + ", colliders sin malla " + sinMalla);
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/t1388-validar.txt", sb.ToString());
            Debug.Log("[Arte] t1388-validar:\n" + sb);
        }

        private static string Ruta(Transform t)
        {
            string r = t.name;
            for (Transform p = t.parent; p != null && p.name != NombreRaiz; p = p.parent)
            {
                r = p.name + "/" + r;
            }
            return r;
        }

        // ================================================================ Fotos

        /// <summary>Por unión: (nombre, punto del lado de la zona, punto del lado del barrio), en planta.</summary>
        private static readonly (string nombre, Vector3 zona, Vector3 barrio)[] Vistas =
        {
            ("U1-sur-oeste", new Vector3(-47f, 0f, -212f), new Vector3(-47f, 0f, -170f)),
            ("U2-sur-este", new Vector3(47f, 0f, -212f), new Vector3(47f, 0f, -170f)),
            ("U3-malecon-cerro", new Vector3(-308f, 0f, -128f), new Vector3(-300f, 0f, -200f)),
            ("U4-oeste-cerro", new Vector3(-298f, 0f, 0f), new Vector3(-236f, 0f, 0f)),
            ("U4-calle-oeste", new Vector3(-242f, 0f, 56f), new Vector3(-242f, 0f, -56f)),
            ("U4-calle-base", new Vector3(-305f, 0f, 60f), new Vector3(-305f, 0f, -110f)),
            ("U5-malecon-puerto", new Vector3(152f, 0f, -215f), new Vector3(104f, 0f, -215f)),
            ("U7-rampa-norte", new Vector3(-118f, 0f, 212f), new Vector3(-100f, 0f, 190f)),
            ("U8-puerta-puerto", new Vector3(300f, 0f, -38f), new Vector3(300f, 0f, 40f)),
            ("U9-conector-este", new Vector3(294f, 0f, 0f), new Vector3(248f, 0f, 0f)),
        };

        public static void Fotos()
        {
            ColocarS2.Abrir();
            Physics.SyncTransforms();
            Directory.CreateDirectory(CarpetaFotos);
            foreach ((string nombre, Vector3 zona, Vector3 barrio) in Vistas)
            {
                Vector3 ojoZona = ConAltura(zona, 1.7f);
                Vector3 ojoBarrio = ConAltura(barrio, 1.7f);
                ColocarS2cLook.Foto(ojoZona, ConAltura(barrio, 2.5f), CarpetaFotos + nombre + "-zona-a-barrio.png", 70f);
                ColocarS2cLook.Foto(ojoBarrio, ConAltura(zona, 2.5f), CarpetaFotos + nombre + "-barrio-a-zona.png", 70f);
            }
        }

        private static Vector3 ConAltura(Vector3 p, float altura)
        {
            float y = Piso(p, 0f, out float suelo, out Collider _) ? suelo : 0f;
            return new Vector3(p.x, y + altura, p.z);
        }
    }
}
