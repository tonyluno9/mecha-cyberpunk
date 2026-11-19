using System.Collections.Generic;
using System.Linq;
using MechaCyberpunk.Core.Mundo;
using MechaCyberpunk.Trafico.Calle;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-1364: coloca en la ciudad las zonas de tráfico (ZonaTrafico_*, una caja alineada a los ejes cada una) y las plazas del Malecón (T-1341) con su piso de adoquín.
    /// Las cajas no se enciman: <see cref="ZonaTrafico.De"/> devuelve la primera activa, así que cada punto debe caer en una sola.
    /// Las plazas se buscan por rejilla y se aceptan solo si su huella no toca calles (CalleZona y las calles de la cuadrícula: a más de ancho/2 + 1 m de la línea),
    /// no tiene colliders encima (fachadas, palmeras, faroles, barandal) y el piso queda a la altura de la banqueta. Lo que no cabe se anota en el informe.
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ColocarT1364ZonasTrafico.Aplicar
    /// </summary>
    public static class ColocarT1364ZonasTrafico
    {
        public const string NombreRaiz = "T1364_ZonasTrafico";
        public const int Semilla = 1364;
        private const string CarpetaZonas = "Assets/_Proyecto/Trafico/Prefabs/Zonas/ZonaTrafico_";
        private const string CarpetaPlazas = "Assets/Arte/Zonas/PZ/Plazas/";
        private const string RutaAdoquin = "Assets/Arte/ZonasModulos/Texturas/T06_Adoquin.png";
        private const float YBanqueta = 0.2f;
        private const float TolerAltura = 0.1f;
        private const float MargenCalle = 1f;
        private const float MargenChoque = 0.3f;

        /// <summary>Una zona de tráfico: nombre de la zona (casa con ConfigCarrosZonas), nombre del objeto y la caja en mundo.</summary>
        public readonly struct Caja
        {
            public readonly string Zona;
            public readonly string Objeto;
            public readonly Vector3 Min;
            public readonly Vector3 Max;

            public Caja(string zona, string objeto, Vector3 min, Vector3 max)
            {
                Zona = zona;
                Objeto = objeto;
                Min = min;
                Max = max;
            }

            public Vector3 Centro => (Min + Max) * 0.5f;
            public Vector3 Tamano => Max - Min;

            public bool Contiene(Vector3 p)
            {
                return p.x >= Min.x && p.x <= Max.x && p.y >= Min.y && p.y <= Max.y && p.z >= Min.z && p.z <= Max.z;
            }

            public bool Toca(Caja otra)
            {
                return Min.x <= otra.Max.x && Max.x >= otra.Min.x && Min.y <= otra.Max.y && Max.y >= otra.Min.y && Min.z <= otra.Max.z && Max.z >= otra.Min.z;
            }
        }

        /// <summary>
        /// Las cajas de las zonas. Malecón se corta en x = 115 (ahí empieza el Puerto); la cubierta de la autopista (y 8..20) queda sobre el Deshuesadero (y -2..7.5);
        /// la rampa este (x 280..320) es Avenida de piso a techo; Neón Alto y la avenida central no se tocan (47 / 46.5); Las Lomas empieza en z = 88.5.
        /// </summary>
        public static readonly Caja[] Cajas =
        {
            new Caja("Malecon", "ZonaTrafico_Malecon", new Vector3(-330f, -5f, -255f), new Vector3(115f, 10f, -200f)),
            new Caja("Puerto", "ZonaTrafico_Puerto", new Vector3(115.5f, -5f, -250f), new Vector3(330f, 15f, -45f)),
            new Caja("NeonAlto", "ZonaTrafico_NeonAlto", new Vector3(47f, -2f, 0f), new Vector3(135f, 12f, 88f)),
            new Caja("NeonAlto", "ZonaTrafico_NeonAlto_CorredorEste", new Vector3(135.5f, -2f, -14f), new Vector3(280f, 12f, 14f)),
            new Caja("Avenida", "ZonaTrafico_Avenida_Autopista", new Vector3(-330f, 8f, 235f), new Vector3(280f, 20f, 265f)),
            new Caja("Avenida", "ZonaTrafico_Avenida_RampaEste", new Vector3(280.5f, -2f, 60f), new Vector3(320f, 20f, 265f)),
            new Caja("Avenida", "ZonaTrafico_Avenida_Centro", new Vector3(-250f, -2f, -14f), new Vector3(46.5f, 6f, 14f)),
            new Caja("Deshuesadero", "ZonaTrafico_Deshuesadero", new Vector3(-330f, -2f, 205f), new Vector3(280f, 7.5f, 290f)),
            new Caja("LasLomas", "ZonaTrafico_LasLomas_Oeste", new Vector3(-57f, -2f, 88.5f), new Vector3(-37f, 12f, 201f)),
            new Caja("LasLomas", "ZonaTrafico_LasLomas_Este", new Vector3(37f, -2f, 88.5f), new Vector3(57f, 12f, 201f)),
        };

        /// <summary>El nombre de zona del punto según la lista (la primera caja que lo contiene), o null. Función pura: la misma regla que ZonaTrafico.De.</summary>
        public static string ZonaDe(IReadOnlyList<Caja> cajas, Vector3 punto)
        {
            for (int i = 0; i < cajas.Count; i++)
            {
                if (cajas[i].Contiene(punto))
                {
                    return cajas[i].Zona;
                }
            }
            return null;
        }

        /// <summary>Pares de cajas que se tocan (lista vacía si no hay encimes).</summary>
        public static List<(Caja, Caja)> Encimes(IReadOnlyList<Caja> cajas)
        {
            List<(Caja, Caja)> pares = new List<(Caja, Caja)>();
            for (int i = 0; i < cajas.Count; i++)
            {
                for (int j = i + 1; j < cajas.Count; j++)
                {
                    if (cajas[i].Toca(cajas[j]))
                    {
                        pares.Add((cajas[i], cajas[j]));
                    }
                }
            }
            return pares;
        }

        // ================================================================ Entrada

        [MenuItem("Mecha Cyberpunk/T-1364/Zonas de trafico y plazas")]
        public static void Aplicar()
        {
            ColocarS2.Abrir();
            ColocarS2.Informe.Clear();
            KitS2c.Obra o = KitS2c.Empezar(NombreRaiz, Semilla);
            Transform zonas = Hijo(o.Raiz, "Zonas");
            Transform plazas = Hijo(o.Raiz, "Plazas");

            ColocarZonas(zonas);
            ColocarPlazas(o, plazas);

            KitS2c.Terminar(o);
            ColocarS2.Guardar("t1364-zonas-trafico");
        }

        // ================================================================ Zonas de tráfico

        private static void ColocarZonas(Transform padre)
        {
            foreach (Caja c in Cajas)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CarpetaZonas + c.Zona + ".prefab");
                if (prefab == null)
                {
                    ColocarS2.Informe.AppendLine("Falta el prefab de la zona " + c.Zona);
                    continue;
                }
                GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, padre);
                go.name = c.Objeto;
                go.transform.SetPositionAndRotation(c.Centro, Quaternion.identity);
                go.GetComponent<ZonaTrafico>().Definir(c.Zona, c.Tamano);
                ColocarS2.Informe.AppendLine("Zona " + c.Objeto + " (" + c.Zona + "): centro " + c.Centro + ", tamaño " + c.Tamano);
            }
            ColocarS2.Informe.AppendLine("Cajas que se enciman: " + Encimes(Cajas).Count);
        }

        // ================================================================ Plazas

        private struct Sitio
        {
            public Vector3 Centro;
            public Quaternion Giro;
            public float Costo;
        }

        private static void ColocarPlazas(KitS2c.Obra o, Transform padre)
        {
            Physics.SyncTransforms();
            List<Vector3> avenida = ColocarT1354Curva.Avenida();
            List<(Vector3 a, Vector3 b, float mitad)> calles = CallesDeLaZona();
            Material adoquin = KitS2c.Mat("T1364_Adoquin", () =>
                KitS2c.Lit(new Color(0.78f, 0.74f, 0.7f), 0.25f, AssetDatabase.LoadAssetAtPath<Texture2D>(RutaAdoquin)));

            // Laterales: distancia a la línea de la avenida, positivo hacia el mar y negativo hacia la ciudad.
            // Kiosco del lado de la ciudad cerca de x = -150; esquinas junto a U1 y U2 del lado de la ciudad; banquetas a lo largo del paseo.
            Poner(o, padre, avenida, calles, adoquin, "Plaza_Kiosco", "Plaza_Kiosco", -150f, -290f, -60f, -12f, -60f, -4f);
            Poner(o, padre, avenida, calles, adoquin, "Plaza_Esquina", "Plaza_Esquina_U1", -47f, -90f, -10f, -11f, -30f, -1f);
            Poner(o, padre, avenida, calles, adoquin, "Plaza_Esquina", "Plaza_Esquina_U2", 47f, 10f, 100f, -11f, -30f, -1f);
            Poner(o, padre, avenida, calles, adoquin, "Plaza_Banqueta", "Plaza_Banqueta_1", -230f, -290f, -170f, 9.5f, 13.5f, 0.5f);
            Poner(o, padre, avenida, calles, adoquin, "Plaza_Banqueta", "Plaza_Banqueta_2", -110f, -170f, -60f, 9.5f, 13.5f, 0.5f);
            Poner(o, padre, avenida, calles, adoquin, "Plaza_Banqueta", "Plaza_Banqueta_3", 75f, 60f, 110f, 9.5f, 13.5f, 0.5f);
        }

        /// <summary>
        /// Busca el mejor sitio de la plaza (x de x0 a x1 cada 2 m; lateral de latDesde a latHasta con el paso dado) y la pone con su losa de adoquín.
        /// El mejor es el más cerca de objetivoX y de la avenida. La plaza mira (-Z) hacia la avenida.
        /// </summary>
        private static void Poner(KitS2c.Obra o, Transform padre, List<Vector3> avenida, List<(Vector3 a, Vector3 b, float mitad)> calles, Material adoquin,
            string prefabNombre, string objeto, float objetivoX, float x0, float x1, float latDesde, float latHasta, float latPaso)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CarpetaPlazas + prefabNombre + ".prefab");
            if (prefab == null)
            {
                ColocarS2.Informe.AppendLine(objeto + ": falta el prefab " + prefabNombre);
                return;
            }
            Bounds huella = MedirHuella(prefab, padre, out float alto);

            Sitio? mejor = null;
            for (float x = x0; x <= x1; x += 2f)
            {
                Vector3 p = ColocarT1354Curva.PuntoEn(avenida, DistanciaEnX(avenida, x), out Vector3 t);
                Vector3 mar = ColocarT1354Curva.HaciaElMar(t);
                for (float lat = latDesde; latPaso > 0f ? lat <= latHasta + 0.001f : lat >= latHasta - 0.001f; lat += latPaso)
                {
                    // El -Z de la plaza mira a la avenida: hacia la ciudad si está del lado del mar, hacia el mar si está del lado de la ciudad.
                    Quaternion giro = Quaternion.LookRotation(lat >= 0f ? mar : -mar);
                    Vector3 centro = p + mar * lat + giro * new Vector3(huella.center.x, 0f, huella.center.z);
                    if (!Cabe(centro, giro, huella.size, alto, calles, out float suelo))
                    {
                        continue;
                    }
                    centro.y = suelo;
                    float costo = Mathf.Abs(centro.x - objetivoX) + 0.5f * Mathf.Abs(lat);
                    if (mejor == null || costo < mejor.Value.Costo)
                    {
                        mejor = new Sitio { Centro = centro, Giro = giro, Costo = costo };
                    }
                }
            }

            if (mejor == null)
            {
                ColocarS2.Informe.AppendLine(objeto + ": NO CUPO (huella " + huella.size.x.ToString("F1") + " x " + huella.size.z.ToString("F1") +
                    " m; sin sitio libre de calles, fachadas, palmeras y faroles en x " + x0 + ".." + x1 + "). Queda sin colocar.");
                return;
            }

            Sitio s = mejor.Value;
            // El pivote de la plaza es el centro de su piso; el centro de la huella puede estar corrido: se resta ese corrimiento.
            Vector3 pivote = s.Centro - s.Giro * new Vector3(huella.center.x, 0f, huella.center.z);
            GameObject plaza = (GameObject)PrefabUtility.InstantiatePrefab(prefab, padre);
            plaza.name = objeto;
            plaza.transform.SetPositionAndRotation(pivote, s.Giro);
            KitS2c.Losa(o, plaza.transform, "PisoAdoquin", s.Centro + Vector3.up * 0.02f, s.Giro, new Vector3(huella.size.x + 0.4f, 0.06f, huella.size.z + 0.4f), adoquin, 2f, false);
            Physics.SyncTransforms();
            int choques = Choques(plaza.transform, s.Centro, s.Giro, huella.size, alto);
            ColocarS2.Informe.AppendLine(objeto + ": pivote " + pivote + ", huella " + huella.size.x.ToString("F1") + " x " + huella.size.z.ToString("F1") +
                " m, piso a y " + s.Centro.y.ToString("F2") + ", colliders ajenos encima: " + choques);
        }

        /// <summary>Huella de la plaza en su sistema local: instancia temporal en el origen, sin giro, y suma de los bounds de sus Renderers.</summary>
        private static Bounds MedirHuella(GameObject prefab, Transform padre, out float alto)
        {
            GameObject temp = (GameObject)PrefabUtility.InstantiatePrefab(prefab, padre);
            temp.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            Renderer[] rs = temp.GetComponentsInChildren<Renderer>();
            Bounds b = new Bounds(Vector3.zero, new Vector3(10f, 0f, 10f));
            if (rs.Length > 0)
            {
                b = rs[0].bounds;
                foreach (Renderer r in rs.Skip(1))
                {
                    b.Encapsulate(r.bounds);
                }
            }
            alto = Mathf.Clamp(b.max.y, 1f, 6f);
            Object.DestroyImmediate(temp);
            return b;
        }

        /// <summary>Si la huella (en centro, giro) queda sobre banqueta a la altura correcta, lejos de las calles y sin colliders encima.</summary>
        private static bool Cabe(Vector3 centro, Quaternion giro, Vector3 tamano, float alto, List<(Vector3 a, Vector3 b, float mitad)> calles, out float suelo)
        {
            suelo = YBanqueta;
            Vector3 mx = giro * new Vector3(tamano.x * 0.5f, 0f, 0f);
            Vector3 mz = giro * new Vector3(0f, 0f, tamano.z * 0.5f);
            List<Vector3> puntos = new List<Vector3>();
            for (int i = -2; i <= 2; i++)
            {
                for (int j = -2; j <= 2; j++)
                {
                    puntos.Add(centro + mx * (i / 2f) + mz * (j / 2f));
                }
            }

            // Calles: toda la huella a más de ancho/2 + 1 m de cada línea.
            foreach (Vector3 q in puntos)
            {
                foreach ((Vector3 a, Vector3 b, float mitad) in calles)
                {
                    if (DistanciaPlanta(q, a, b) < mitad + MargenCalle)
                    {
                        return false;
                    }
                }
            }

            // Piso a la altura de la banqueta en todos los puntos.
            float suma = 0f;
            foreach (Vector3 q in puntos)
            {
                if (!Physics.Raycast(new Vector3(q.x, 3f, q.z), Vector3.down, out RaycastHit hit, 6f, ~0, QueryTriggerInteraction.Ignore) || Mathf.Abs(hit.point.y - YBanqueta) > TolerAltura)
                {
                    return false;
                }
                suma += hit.point.y;
            }
            suelo = suma / puntos.Count;

            Vector3 mitadCaja = new Vector3(tamano.x * 0.5f + MargenChoque, (alto - 0.45f) * 0.5f, tamano.z * 0.5f + MargenChoque);
            Vector3 centroCaja = new Vector3(centro.x, suelo + 0.45f + mitadCaja.y, centro.z);
            return Physics.OverlapBox(centroCaja, mitadCaja, giro, ~0, QueryTriggerInteraction.Ignore).Length == 0;
        }

        /// <summary>Colliders que tocan la plaza ya puesta y no son suyos (comprobación final para el informe).</summary>
        private static int Choques(Transform plaza, Vector3 centro, Quaternion giro, Vector3 tamano, float alto)
        {
            Vector3 mitadCaja = new Vector3(tamano.x * 0.5f, (alto - 0.45f) * 0.5f, tamano.z * 0.5f);
            Vector3 centroCaja = new Vector3(centro.x, centro.y + 0.45f + mitadCaja.y, centro.z);
            return Physics.OverlapBox(centroCaja, mitadCaja, giro, ~0, QueryTriggerInteraction.Ignore).Count(c => !c.transform.IsChildOf(plaza));
        }

        /// <summary>Todas las calles de las zonas (CalleZona, línea y mitad del ancho) y las de la cuadrícula del Malecón: corredores x = ±47 y la cruzada z = -201 (14 m).</summary>
        private static List<(Vector3 a, Vector3 b, float mitad)> CallesDeLaZona()
        {
            List<(Vector3 a, Vector3 b, float mitad)> lista = new List<(Vector3 a, Vector3 b, float mitad)>();
            foreach (CalleZona c in Object.FindObjectsByType<CalleZona>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                List<Vector3> pts = c.Puntos();
                for (int i = 0; i + 1 < pts.Count; i++)
                {
                    lista.Add((pts[i], pts[i + 1], c.Ancho * 0.5f));
                }
            }
            foreach (float x in new[] { -47f, 47f })
            {
                lista.Add((new Vector3(x, 0f, -201f), new Vector3(x, 0f, -100f), 7f));
            }
            lista.Add((new Vector3(-330f, 0f, -201f), new Vector3(330f, 0f, -201f), 7f));
            return lista;
        }

        private static float DistanciaPlanta(Vector3 p, Vector3 a, Vector3 b)
        {
            Vector2 pa = new Vector2(p.x - a.x, p.z - a.z);
            Vector2 ab = new Vector2(b.x - a.x, b.z - a.z);
            float t = ab.sqrMagnitude < 1e-6f ? 0f : Mathf.Clamp01(Vector2.Dot(pa, ab) / ab.sqrMagnitude);
            return (pa - ab * t).magnitude;
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

        private static Transform Hijo(Transform padre, string nombre)
        {
            Transform t = new GameObject(nombre).transform;
            t.SetParent(padre, false);
            return t;
        }
    }
}
