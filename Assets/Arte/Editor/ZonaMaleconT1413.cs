using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-1413: Malecón, muelle K12. Corre DESPUÉS de ZonaMaleconT1384, T1393, T1400 y T1404; edita el prefab T1354_Malecon (todo lo nuevo cuelga de "T1413_Malecon").
    /// 1) Cámara K12 nueva: en la banqueta del mar al este del muelle, mirando al oeste (mar a la izquierda), con el puesto de mariscos de primer plano.
    /// 2) Puesto de mariscos ML03 junto al arranque del muelle (al este de la escalera, sin taparla) con hieleras ML07, huacales C17 y tambos C16, y tres puestos más a lo largo del paseo.
    /// 3) Lanchas ML02 de colores también en el lado este del muelle (ya había 6 al oeste) y bitas PT02 en la cubierta.
    /// 4) Un segundo faro ML04 lejano al oeste, sobre un islote de escollera ML05, con su haz de luz, para que el faro se vea al fondo del cuadro de K12 (el faro de T-1354 está al este, a espaldas de la cámara).
    /// 5) A11: cámara aérea sobre el mar, al sureste del muelle, mirando al oeste-noroeste (curva de la avenida, paseo con faroles, playa, muelle y fachadas).
    /// Métodos: Aplicar, Validar y, en corrida aparte, Fotos. Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ZonaMaleconT1413.Aplicar
    /// </summary>
    public static class ZonaMaleconT1413
    {
        public const string RutaPrefab = ZonaMaleconT1393.RutaPrefab;
        public const string NombreRaiz = "T1413_Malecon";

        /// <summary>Variante de ML07 (hielera con banquitos): la _a (hielera turquesa con tapa blanca) por defecto y la _b (turquesa con naranja, más pesada) en alternancia: la _b queda más cerca del puesto ML03.</summary>
        public const string Variante = "_a";
        public const string VarianteAlterna = "_b";

        private const string RutaHielera = "Assets/Arte/Zonas/ML/ML07_Hielera";
        private const string RutaPuesto = "Assets/Arte/Zonas/ML/ML03_PuestoMariscos.prefab";
        private const string RutaLancha = "Assets/Arte/Zonas/ML/ML02_Lancha.prefab";
        private const string RutaFaro = "Assets/Arte/Zonas/ML/ML04_Faro.prefab";

        private const float XMuelle = 92f;
        private const float YMuelle = -1f;
        private const float LateralPuesto = 13.4f;
        private const float EscalaPuesto = 1.8f;
        private const int TopeLucesNuevas = 12;

        // Islote del faro lejano (en el agua, al suroeste de la cámara K12).
        private static readonly Vector2 CentroIslote = new Vector2(-60f, -335f);
        private const float EscalaRoca = 2.2f;
        private const float EscalaFaro = 1.7f;

        private static readonly Color[] ColoresLancha =
        {
            new Color(0.15f, 0.4f, 0.75f), new Color(0.9f, 0.75f, 0.15f), new Color(0.8f, 0.15f, 0.12f), new Color(0.1f, 0.75f, 0.7f),
        };

        private static readonly StringBuilder Informe = new StringBuilder();

        // ================================================================ Cámaras

        private static void Linea(float x, out Vector3 centro, out Vector3 tang, out Vector3 mar)
        {
            float z = ColocarT1354Curva.ZCentro(x);
            float dz = (ColocarT1354Curva.ZCentro(x + 1f) - ColocarT1354Curva.ZCentro(x - 1f)) * 0.5f;
            tang = new Vector3(1f, 0f, dz).normalized;
            mar = ColocarT1354Curva.HaciaElMar(tang);
            centro = new Vector3(x, 0f, z);
        }

        private static Vector3 PuntoLateral(float x, float lateral, float y)
        {
            Linea(x, out Vector3 c, out _, out Vector3 mar);
            Vector3 p = c + mar * lateral;
            p.y = y;
            return p;
        }

        /// <summary>K12: en la banqueta del mar (lateral 10) a 16 m al este del muelle, a 2.3 m de alto, mirando al oeste con el mar a la izquierda.</summary>
        internal static Vector3 CamaraK12 => PuntoLateral(108f, 10f, 2.3f);
        internal static Vector3 MiraK12 => PuntoLateral(20f, 12f, 2.5f);
        internal const float CampoK12 = 70f;

        /// <summary>A11: sobre el agua al sureste del muelle, mirando al oeste-noroeste (el mar a la izquierda, la ciudad a la derecha).</summary>
        internal static readonly Vector3 CamaraA11 = new Vector3(122f, 34f, -276f);
        internal static readonly Vector3 MiraA11 = new Vector3(42f, 0f, -226f);
        internal const float CampoA11 = 70f;

        // ================================================================ Aplicar

        [MenuItem("Mecha Cyberpunk/T-1413/Malecon K12")]
        public static void Aplicar()
        {
            Informe.Clear();
            GameObject contenido = PrefabUtility.LoadPrefabContents(RutaPrefab);
            try
            {
                Transform raiz = contenido.transform;
                int luzAntes = raiz.GetComponentsInChildren<Light>(true).Length;
                Transform vieja = raiz.Find(NombreRaiz);
                if (vieja != null)
                {
                    Object.DestroyImmediate(vieja.gameObject);
                }
                Transform mio = new GameObject(NombreRaiz).transform;
                mio.SetParent(raiz, false);
                KitS2c.Obra obra = new KitS2c.Obra { Raiz = mio, Azar = new System.Random(1413), Nombre = "T1413_Extras" };

                QuitarEstorbosDeCamara(raiz);
                PonerLanchasYBitas(raiz, mio);
                PonerPuestos(obra, raiz, mio);
                PonerFaroLejano(obra, mio);
                KitS2c.Terminar(obra);
                MarcarEstaticos(mio);

                int luzDespues = raiz.GetComponentsInChildren<Light>(true).Length;
                int nuevas = mio.GetComponentsInChildren<Light>(true).Length;
                Informe.AppendLine("Luces antes " + luzAntes + ", después " + luzDespues + "; nuevas de T-1413: " + nuevas + " (tope " + TopeLucesNuevas + ", global 40).");
                if (nuevas > TopeLucesNuevas)
                {
                    Informe.AppendLine("ERROR: más luces nuevas del tope.");
                }
                Informe.AppendLine("Variantes: ML07 " + Variante + " con " + VarianteAlterna + " alternadas (ver docstring). ML03 y ML02 no tienen variantes.");
                PrefabUtility.SaveAsPrefabAsset(contenido, RutaPrefab);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contenido);
            }
            Escribir("t1413-aplicar");
        }

        // ================================================================ Estorbos de la cámara

        /// <summary>Las palmeras y faroles (AR03_Palmera, ML01_Farol) del paseo que caen sobre la cámara K12 o en los primeros 6 m de su mirada se apagan por nombre exacto y se anota cuáles.</summary>
        private static void QuitarEstorbosDeCamara(Transform raiz)
        {
            Vector3 cam = CamaraK12;
            Vector3 mira = (MiraK12 - cam);
            mira.y = 0f;
            mira.Normalize();
            int n = 0;
            foreach (string ruta in new[] { "Paseo", ZonaMaleconT1393.NombreRaiz + "/Paseo" })
            {
                Transform g = raiz.Find(ruta);
                if (g == null)
                {
                    continue;
                }
                foreach (Transform t in g.Cast<Transform>().ToList())
                {
                    if (!t.gameObject.activeSelf || !(t.name.StartsWith("AR03_Palmera", StringComparison.Ordinal) || t.name.StartsWith("ML01_Farol", StringComparison.Ordinal)))
                    {
                        continue;
                    }
                    Bounds b = ZonaMaleconT1393.CajaDe(t.gameObject);
                    Vector3 d = b.center - cam;
                    d.y = 0f;
                    float delante = Vector3.Dot(d, mira);
                    float lado = Mathf.Abs(Vector3.Cross(mira, d).y);
                    bool estorba = d.magnitude < 3.5f || (delante > 0f && delante < 6f && lado < 1.6f);
                    if (estorba)
                    {
                        t.gameObject.SetActive(false);
                        n++;
                        Informe.AppendLine("Apagado " + ruta + "/" + t.name + " (a " + d.magnitude.ToString("F1") + " m de la cámara K12, tapaba el cuadro).");
                    }
                }
            }
            Informe.AppendLine("Estorbos de la cámara K12 apagados: " + n + ".");
        }

        // ================================================================ Lanchas y bitas

        private static GameObject Cargar(string ruta)
        {
            GameObject p = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
            if (p == null)
            {
                Informe.AppendLine("FALTA: " + ruta + ".");
            }
            return p;
        }

        /// <summary>Pone el prefab con su base (min.y) en suelo.y y el centro de su caja en suelo.xz. Sin revisar choques.</summary>
        private static GameObject Instanciar(GameObject prefab, Transform padre, Vector3 suelo, Quaternion giro, float escala, string nombre)
        {
            Bounds mb = ZonaMaleconT1393.Medida(prefab);
            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, padre);
            go.name = nombre;
            go.transform.rotation = giro;
            go.transform.localScale = Vector3.one * escala;
            Vector3 ancla = new Vector3(mb.center.x, mb.min.y, mb.center.z) * escala;
            go.transform.position = suelo - giro * ancla;
            return go;
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
                ms[0] = KitS2c.Mat("T1413_Lancha_" + indice + "_" + origen.name, () =>
                {
                    Material m = new Material(origen);
                    m.SetColor("_BaseColor", color);
                    return m;
                });
                r.sharedMaterials = ms;
            }
        }

        private static void PonerLanchasYBitas(Transform raiz, Transform mio)
        {
            GameObject lancha = Cargar(RutaLancha);
            GameObject recto = Cargar(CorrerZonasT1350.RutaModulo("ML03_MuelleRecto"));
            GameObject bita = Cargar(CorrerZonasT1350.RutaModulo("PT02_BitaAmarre"));
            if (recto == null)
            {
                Informe.AppendLine("Sin ML03_MuelleRecto no se sabe dónde acaba el muelle: sin lanchas ni bitas nuevas.");
                return;
            }
            Transform grupo = new GameObject("Muelle").transform;
            grupo.SetParent(mio, false);
            Bounds rb = ZonaMaleconT1393.Medida(recto);
            float paso = Mathf.Max(rb.size.x, rb.size.z) > 1f ? Mathf.Max(rb.size.x, rb.size.z) : 4f;
            float ancho = Mathf.Min(rb.size.x, rb.size.z) > 1f ? Mathf.Min(rb.size.x, rb.size.z) : 3.3f;
            // Mismas cuentas que ColocarT1354Malecon.Muelle: el tronco va al sur desde el muro y la esquina cierra en el último tramo.
            float zAvenida = ColocarT1354Curva.ZCentro(XMuelle);
            float zCosta = zAvenida - ColocarT1354Curva.LateralMuro;
            float zOrilla = zAvenida - ColocarT1354Curva.LateralOrilla(zAvenida);
            int tramos = Mathf.CeilToInt((zCosta - zOrilla + 26f) / paso);
            float zEsquina = zCosta - paso * (tramos + 0.5f);
            Informe.AppendLine("Muelle: ancho " + ancho.ToString("F1") + " m, tronco de z " + zCosta.ToString("F1") + " a " + zEsquina.ToString("F1") + ", orilla z " + zOrilla.ToString("F1") + ".");

            if (lancha != null)
            {
                // Seis lanchas de la ronda 1 al oeste; aquí cuatro al este, a la misma distancia (4.4 m del eje) y con el rumbo opuesto, sin tocar la esquina del muelle.
                int n = 0;
                for (int i = 0; i < 4; i++)
                {
                    float z = zOrilla - 4f - 4.4f * i;
                    if (z - 2.6f < zEsquina + paso * 0.5f + 2f)
                    {
                        Informe.AppendLine("Lancha este " + i + " omitida: toca la esquina del muelle (z " + z.ToString("F1") + ").");
                        continue;
                    }
                    Vector3 p = new Vector3(XMuelle + 4.4f, ColocarT1354Curva.YAgua - 0.15f, z);
                    float giroExtra = ((i * 37) % 11 - 5) * 1.2f;
                    GameObject go = Instanciar(lancha, grupo, p, Quaternion.Euler(0f, 270f + giroExtra, 0f), 1f, "ML02_Lancha_Este_" + i);
                    Tenir(go, ColoresLancha[i % ColoresLancha.Length], i);
                    n++;
                }
                Informe.AppendLine("Lanchas ML02 nuevas al lado este: " + n + " (las 6 del oeste de T-1354 siguen).");
            }
            if (bita != null)
            {
                int n = 0;
                foreach (float lado in new[] { -1f, 1f })
                {
                    for (int k = 0; k < 3; k++)
                    {
                        float z = zOrilla - 6f - 9f * k;
                        if (z < zEsquina + paso * 0.5f + 3f)
                        {
                            continue;
                        }
                        Vector3 p = new Vector3(XMuelle + lado * (ancho * 0.5f - 0.35f), YMuelle, z);
                        Instanciar(bita, grupo, p, Quaternion.Euler(0f, 90f * k, 0f), 1f, "PT02_Bita_" + (lado < 0 ? "O" : "E") + k);
                        n++;
                    }
                }
                Informe.AppendLine("Bitas PT02 en la cubierta del muelle: " + n + ".");
            }
        }

        // ================================================================ Puestos

        private static List<Bounds> OcupadoPaseo(Transform raiz)
        {
            List<Bounds> lista = new List<Bounds>();
            foreach (string grupo in new[] { "Paseo", "Postes", "Muelle", ZonaMaleconT1393.Raiz1384, ZonaMaleconT1393.NombreRaiz + "/Paseo" })
            {
                Transform g = raiz.Find(grupo);
                if (g == null)
                {
                    continue;
                }
                foreach (Transform t in g)
                {
                    // El barandal corre a lo largo del muro (lateral 15.6) y su caja alineada a los ejes se come la banqueta: no cuenta.
                    if (!t.gameObject.activeInHierarchy || t.name.StartsWith("ML01_Barandal", StringComparison.Ordinal) || t.GetComponentInChildren<Renderer>() == null)
                    {
                        continue;
                    }
                    lista.Add(ZonaMaleconT1393.CajaDe(t.gameObject));
                }
            }
            return lista;
        }

        private static void PonerPuestos(KitS2c.Obra obra, Transform raiz, Transform mio)
        {
            GameObject puesto = Cargar(RutaPuesto);
            if (puesto == null)
            {
                return;
            }
            Transform grupo = new GameObject("Puestos").transform;
            grupo.SetParent(mio, false);
            GameObject hA = Cargar(RutaHielera + Variante + ".prefab");
            GameObject hB = Cargar(RutaHielera + VarianteAlterna + ".prefab");
            GameObject huacales = KitS2c.Prefab("C17_Huacales");
            GameObject tambo = KitS2c.Prefab("C16_Tambo");
            GameObject escalera = Cargar(CorrerZonasT1350.RutaModulo("ML02_EscaleraPlaya"));
            float mediaEscalera = escalera != null ? Mathf.Max(2f, ZonaMaleconT1393.Medida(escalera).size.x * 0.5f) : 2.5f;
            Bounds pb = ZonaMaleconT1393.Medida(puesto);
            float mediaPuesto = pb.size.x * EscalaPuesto * 0.5f;
            Informe.AppendLine("ML03_PuestoMariscos mide " + pb.size.ToString("F2") + " m a escala 1; a " + EscalaPuesto + " ocupa " + (mediaPuesto * 2f).ToString("F1") + " m de ancho.");

            List<Bounds> ocupado = OcupadoPaseo(raiz);
            Vector3 cam = CamaraK12;
            // (x deseado, rango de búsqueda, con luz). El primero va junto al arranque del muelle, al este de la escalera.
            (float x, float rango, bool luz)[] pedidos = { (XMuelle + mediaEscalera + mediaPuesto + 2f, 6f, true), (64f, 6f, true), (24f, 8f, true), (-14f, 8f, false) };
            int nPuestos = 0, nLuces = 0, nHieleras = 0, nCajas = 0;
            for (int k = 0; k < pedidos.Length; k++)
            {
                (float xd, float rango, bool conLuz) = pedidos[k];
                GameObject go = null;
                Vector3 suelo = Vector3.zero;
                Vector3 haciaMar = Vector3.back;
                float xFinal = xd;
                for (int paso = 0; paso <= (int)(rango * 4f) && go == null; paso++)
                {
                    float x = xd + (paso % 2 == 0 ? 1 : -1) * (paso + 1) / 2 * 0.5f;
                    if (Mathf.Abs(Mathf.Abs(x) - 47f) < 10f || x > 106f)
                    {
                        continue;
                    }
                    if (Mathf.Abs(x - XMuelle) < mediaEscalera + mediaPuesto + 1.2f)
                    {
                        continue;
                    }
                    Linea(x, out Vector3 centro, out _, out Vector3 mar);
                    suelo = centro + mar * LateralPuesto;
                    suelo.y = ColocarT1354Curva.YBanqueta;
                    if (Vector2.Distance(new Vector2(suelo.x, suelo.z), new Vector2(cam.x, cam.z)) < mediaPuesto + 2.4f)
                    {
                        continue;
                    }
                    haciaMar = mar;
                    go = ZonaMaleconT1393.Colocar(puesto, grupo, suelo, Quaternion.LookRotation(-mar), EscalaPuesto, ocupado, "ML03_PuestoMariscos_" + k);
                    xFinal = x;
                }
                if (go == null)
                {
                    Informe.AppendLine("Puesto " + k + " omitido: no hay hueco libre cerca de x " + xd.ToString("F0") + " (rango " + rango + ").");
                    continue;
                }
                nPuestos++;
                Informe.AppendLine("Puesto " + k + " en x " + xFinal.ToString("F1") + " (pedido " + xd.ToString("F1") + "), suelo " + suelo.ToString("F1") + ".");
                if (conLuz && nLuces < 3)
                {
                    Vector3 lampara = suelo + Vector3.up * 3.2f;
                    KitS2c.Luz(go.transform, lampara, LightType.Point, KitS2c.Calido, 30f, 15f, 0f);
                    KitS2c.Halo(obra, go.transform, lampara, ColocarT1354Curva.YBanqueta, KitS2c.Calido);
                    nLuces++;
                }

                // Accesorios a los dos lados del puesto (a lo largo de la banqueta), con 0.6 m de aire entre cajas.
                Vector3 lado = Vector3.Cross(Vector3.up, haciaMar).normalized;
                (GameObject prefab, float lateralMar, int signo, string nombre)[] props =
                {
                    (k % 2 == 0 ? hB : hA, 0.2f, 1, "ML07_Hielera"),
                    (huacales, 0.1f, 1, "C17_Huacales"),
                    (k % 2 == 0 ? hA : hB, 0.0f, -1, "ML07_Hielera"),
                    (tambo, -0.2f, -1, "C16_Tambo"),
                };
                float[] llevados = { 0f, 0f };
                for (int j = 0; j < props.Length; j++)
                {
                    GameObject pp = props[j].prefab;
                    if (pp == null)
                    {
                        continue;
                    }
                    Bounds mb = ZonaMaleconT1393.Medida(pp);
                    int lado01 = props[j].signo > 0 ? 0 : 1;
                    float medio = Mathf.Max(mb.size.x, mb.size.z) * 0.5f;
                    float dist = mediaPuesto + 0.9f + llevados[lado01] + medio;
                    Vector3 p = suelo + lado * (props[j].signo * dist) + haciaMar * props[j].lateralMar;
                    p.y = ColocarT1354Curva.YBanqueta;
                    Quaternion giro = Quaternion.LookRotation(-haciaMar) * Quaternion.Euler(0f, ((k * 7 + j * 13) % 9 - 4) * 5f, 0f);
                    GameObject o = ZonaMaleconT1393.Colocar(pp, grupo, p, giro, 1f, ocupado, props[j].nombre + "_" + k + "_" + j);
                    if (o == null)
                    {
                        continue;
                    }
                    llevados[lado01] += medio * 2f + 0.6f;
                    if (props[j].nombre.StartsWith("ML07", StringComparison.Ordinal)) { nHieleras++; } else { nCajas++; }
                }
            }
            Informe.AppendLine("Puestos de mariscos nuevos: " + nPuestos + " (con " + nLuces + " luces cálidas), hieleras ML07: " + nHieleras + ", huacales y tambos: " + nCajas + ". Bancos de plástico: vienen en el modelo ML03 y en ML07.");
        }

        // ================================================================ Faro lejano

        private static void PonerFaroLejano(KitS2c.Obra obra, Transform mio)
        {
            GameObject faro = Cargar(RutaFaro);
            GameObject roca = Cargar(CorrerZonasT1350.RutaModulo("ML05_Escollera"));
            if (faro == null || roca == null)
            {
                Informe.AppendLine("Sin faro lejano: falta " + (faro == null ? "ML04_Faro " : "") + (roca == null ? "ML05_Escollera" : "") + ".");
                return;
            }
            Transform grupo = new GameObject("FaroLejano").transform;
            grupo.SetParent(mio, false);
            Bounds rb = ZonaMaleconT1393.Medida(roca);
            float alto = Mathf.Max(0.5f, rb.size.y) * EscalaRoca;
            float fondo = ColocarT1354Curva.YFondoPlaya - 0.4f;
            // Capas de rocas apiladas con el radio cada vez menor hasta pasar 1.2 m sobre el agua.
            float meta = ColocarT1354Curva.YAgua + 1.2f;
            int capas = Mathf.Clamp(Mathf.CeilToInt((meta - fondo) / (alto * 0.65f)) + 1, 3, 9);
            System.Random azar = new System.Random(14130);
            float cima = fondo;
            int piezas = 0;
            for (int c = 0; c < capas; c++)
            {
                float radio = c == capas - 1 ? 0f : EscalaRoca * 2.4f * (1f - (float)c / (capas - 1));
                int cuantas = c == capas - 1 ? 1 : Mathf.Max(3, Mathf.RoundToInt(radio * 1.4f));
                float y = fondo + alto * 0.65f * c;
                for (int k = 0; k < cuantas; k++)
                {
                    float ang = k * Mathf.PI * 2f / cuantas + c;
                    Vector3 p = new Vector3(CentroIslote.x + Mathf.Cos(ang) * radio, y, CentroIslote.y + Mathf.Sin(ang) * radio);
                    float giro = (float)azar.NextDouble() * 360f;
                    Instanciar(roca, grupo, p, Quaternion.Euler(0f, giro, 0f), EscalaRoca, "ML05_Roca_" + c + "_" + k);
                    piezas++;
                }
                cima = y + alto * 0.75f;
            }
            Vector3 pie = new Vector3(CentroIslote.x, cima, CentroIslote.y);
            GameObject f = Instanciar(faro, grupo, pie, Quaternion.identity, EscalaFaro, "ML04_Faro_Lejano");
            Bounds fb = ZonaMaleconT1393.CajaDe(f);
            Vector3 lampara = new Vector3(fb.center.x, fb.min.y + fb.size.y * 0.88f, fb.center.z);
            Informe.AppendLine("Faro lejano: islote de " + piezas + " rocas ML05 en " + CentroIslote + ", cima y " + cima.ToString("F1") + ", faro de " + fb.size.y.ToString("F1") + " m, lámpara y " + lampara.y.ToString("F1") + ".");

            Transform luz = new GameObject("Luz").transform;
            luz.SetParent(grupo, false);
            KitS2c.Halo(obra, luz, lampara + Vector3.up * 1.5f, lampara.y - 6f, KitS2c.Sodio);

            // Haz: cono estático que sale de la lámpara hacia el este-noreste (cruza el cuadro de K12 de izquierda a derecha), sin Light (cuentan las del tope).
            Shader sh = Shader.Find("MechaCyberpunk/Mundo/HaloLuz");
            if (sh == null)
            {
                Informe.AppendLine("FALTA el shader MechaCyberpunk/Mundo/HaloLuz: sin haz del faro lejano (queda el halo de la linterna).");
                return;
            }
            Material mat = KitS2c.Mat("T1413_HazFaroLejano", () =>
            {
                Material m = new Material(sh);
                m.SetColor("_Color", KitS2c.Calido);
                m.SetFloat("_Intensidad", 0.4f);
                m.SetFloat("_Borde", 1.2f);
                m.SetFloat("_Caida", 1.3f);
                m.SetFloat("_Suave", 0.3f);
                m.SetTexture("_Ruido", KitS2c.TexMuro());
                return m;
            });
            Mesh malla = MallaCono(140f, 1.5f, 12f);
            malla.name = "T1413_HazFaroLejano";
            obra.Mallas.Add(malla);
            GameObject haz = new GameObject("Haz");
            haz.transform.SetParent(grupo, false);
            Vector3 dir = new Vector3(120f, -2f, 45f).normalized;
            haz.transform.SetPositionAndRotation(lampara, Quaternion.LookRotation(dir));
            haz.AddComponent<MeshFilter>().sharedMesh = malla;
            MeshRenderer mr = haz.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            Informe.AppendLine("Haz estático del faro lejano: cono de 140 m hacia " + dir.ToString("F2") + " (no gira: no hace falta, es fondo a 200 m).");
        }

        /// <summary>Cono abierto que sale de la punta (z = 0) y se abre hasta radioFin en z = largo; la v de la UV va de 0 a 1.</summary>
        private static Mesh MallaCono(float largo, float radioInicio, float radioFin)
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
            }
            for (int i = 0; i < lados; i++)
            {
                int a = i * 2;
                t.AddRange(new[] { a, a + 1, a + 2, a + 1, a + 3, a + 2 });
                t.AddRange(new[] { a, a + 2, a + 1, a + 1, a + 2, a + 3 });
            }
            Mesh m = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt16 };
            m.SetVertices(v);
            m.SetNormals(n);
            m.SetUVs(0, uv);
            m.SetTriangles(t, 0);
            m.RecalculateBounds();
            return m;
        }

        // ================================================================ Estáticos

        private static void MarcarEstaticos(Transform mio)
        {
            const StaticEditorFlags banderas = StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.OccluderStatic;
            foreach (Transform t in mio.GetComponentsInChildren<Transform>(true))
            {
                if (t == mio || t.GetComponent<Light>() != null)
                {
                    continue;
                }
                GameObjectUtility.SetStaticEditorFlags(t.gameObject, banderas);
            }
        }

        // ================================================================ Validar

        [MenuItem("Mecha Cyberpunk/T-1413/Validar")]
        public static void Validar()
        {
            Informe.Clear();
            GameObject contenido = PrefabUtility.LoadPrefabContents(RutaPrefab);
            try
            {
                Transform raiz = contenido.transform;
                Transform mio = raiz.Find(NombreRaiz);
                if (mio == null)
                {
                    Informe.AppendLine("ERROR: no hay " + NombreRaiz + "; corre Aplicar.");
                    Escribir("t1413-validar");
                    return;
                }
                int problemas = 0;
                int luces = mio.GetComponentsInChildren<Light>(true).Length;
                int objetos = mio.GetComponentsInChildren<Transform>(true).Length;
                Informe.AppendLine("Luces nuevas: " + luces + " (tope " + TopeLucesNuevas + "); luces de la zona: " + raiz.GetComponentsInChildren<Light>(true).Length);
                Informe.AppendLine("Objetos en " + NombreRaiz + ": " + objetos + " (tope 3000)");
                if (luces > TopeLucesNuevas) { problemas++; Informe.AppendLine("ERROR: demasiadas luces."); }
                if (objetos > 3000) { problemas++; Informe.AppendLine("ERROR: demasiados objetos."); }

                // Huecos de piezas que faltan en el muelle y la costa (las zonas negras).
                foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name.StartsWith("Hueco_ML0", StringComparison.Ordinal) && t.gameObject.activeInHierarchy && t.parent != null && (t.parent.name == "Muelle" || t.parent.name == "Costa"))
                    {
                        problemas++;
                        Informe.AppendLine("HUECO sin modelo en " + t.parent.name + ": " + t.name + " en " + t.position.ToString("F1"));
                    }
                }

                // Puestos: sobre la banqueta, en su franja, sin encimarse entre sí ni con la escalera, y con la cámara libre.
                Transform puestos = mio.Find("Puestos");
                List<(string nombre, Bounds caja)> cajas = new List<(string, Bounds)>();
                if (puestos != null)
                {
                    foreach (Transform t in puestos)
                    {
                        Bounds b = ZonaMaleconT1393.CajaDe(t.gameObject);
                        cajas.Add((t.name, b));
                        float z = ColocarT1354Curva.ZCentro(b.center.x);
                        float lateral = z - b.center.z;
                        if (b.min.y > 0.5f || b.min.y < -0.2f) { problemas++; Informe.AppendLine("FLOTA/ENTERRADO: " + t.name + " min.y=" + b.min.y.ToString("F2")); }
                        if (lateral < 10f || lateral > 16.5f) { problemas++; Informe.AppendLine("FUERA DE LA BANQUETA DEL MAR: " + t.name + " lateral " + lateral.ToString("F1")); }
                    }
                    for (int i = 0; i < cajas.Count; i++)
                    {
                        Bounds ci = cajas[i].caja;
                        ci.Expand(-0.1f);
                        for (int j = i + 1; j < cajas.Count; j++)
                        {
                            if (ci.Intersects(cajas[j].caja)) { problemas++; Informe.AppendLine("ENCIMADO: " + cajas[i].nombre + " con " + cajas[j].nombre); }
                        }
                    }
                    Informe.AppendLine("Puestos y accesorios: " + cajas.Count + " (" + cajas.Count(c => c.nombre.StartsWith("ML03", StringComparison.Ordinal)) + " puestos ML03).");
                }
                else { problemas++; Informe.AppendLine("ERROR: no hay Puestos."); }

                // Contra lo que ya había en el paseo (menos el barandal): sin atravesar; la escalera del muelle con 0.8 m de paso.
                List<Bounds> existentes = OcupadoPaseo(raiz);
                foreach (var c in cajas)
                {
                    Bounds chica = c.caja;
                    chica.Expand(-0.1f);
                    foreach (Bounds e in existentes)
                    {
                        if (chica.Intersects(e)) { problemas++; Informe.AppendLine("ATRAVIESA algo del paseo: " + c.nombre + " con caja en " + e.center.ToString("F1")); break; }
                    }
                }
                foreach (Transform t in raiz.GetComponentsInChildren<Transform>(false))
                {
                    if (t.name.StartsWith("ML02_EscaleraPlaya", StringComparison.Ordinal) && t.GetComponentInChildren<Renderer>() != null)
                    {
                        Bounds eb = ZonaMaleconT1393.CajaDe(t.gameObject);
                        eb.Expand(new Vector3(1.6f, 0f, 1.6f));
                        foreach (var c in cajas)
                        {
                            if (eb.Intersects(c.caja)) { problemas++; Informe.AppendLine("TAPA UNA ESCALERA: " + c.nombre + " con " + t.name + " en " + t.position.ToString("F1")); }
                        }
                    }
                }

                // Lanchas y bitas del muelle.
                Transform muelle = mio.Find("Muelle");
                if (muelle != null)
                {
                    foreach (Transform t in muelle)
                    {
                        Bounds b = ZonaMaleconT1393.CajaDe(t.gameObject);
                        bool lancha = t.name.StartsWith("ML02_Lancha", StringComparison.Ordinal);
                        float esperado = lancha ? ColocarT1354Curva.YAgua - 0.15f : YMuelle;
                        if (Mathf.Abs(b.min.y - esperado) > 0.6f) { problemas++; Informe.AppendLine("FLOTA/ENTERRADO: " + t.name + " min.y=" + b.min.y.ToString("F2") + " (esperado " + esperado.ToString("F2") + ")"); }
                    }
                    Informe.AppendLine("Muelle nuevo: " + muelle.childCount + " piezas (lanchas y bitas).");
                }

                // Faro lejano: base en el fondo y cima sobre el agua.
                Transform fl = mio.Find("FaroLejano");
                if (fl != null)
                {
                    Transform f = fl.Find("ML04_Faro_Lejano");
                    if (f != null)
                    {
                        Bounds fb = ZonaMaleconT1393.CajaDe(f.gameObject);
                        if (fb.min.y < ColocarT1354Curva.YAgua) { problemas++; Informe.AppendLine("FARO LEJANO bajo el agua: min.y=" + fb.min.y.ToString("F2")); }
                        Informe.AppendLine("Faro lejano: base y " + fb.min.y.ToString("F1") + ", alto " + fb.size.y.ToString("F1") + " m.");
                    }
                    else { problemas++; Informe.AppendLine("ERROR: falta ML04_Faro_Lejano."); }
                }
                else { Informe.AppendLine("AVISO: sin FaroLejano (faltaba un modelo, ver Aplicar)."); }

                // Cámaras: ninguna dentro de un objeto sólido pequeño y K12 con la mirada libre 3 m.
                foreach ((string nombre, Vector3 p) in new[] { ("K12", CamaraK12), ("A11", CamaraA11) })
                {
                    foreach (Renderer r in raiz.GetComponentsInChildren<Renderer>(false))
                    {
                        Bounds b = r.bounds;
                        if (b.size.x > 150f || b.size.z > 150f) { continue; }
                        if (b.Contains(p)) { problemas++; Informe.AppendLine("CÁMARA " + nombre + " DENTRO de " + r.transform.name + " (" + r.transform.parent?.name + ")"); }
                    }
                }
                Vector3 dirK12 = (MiraK12 - CamaraK12); dirK12.y = 0f; dirK12.Normalize();
                foreach (Renderer r in raiz.GetComponentsInChildren<Renderer>(false))
                {
                    Bounds b = r.bounds;
                    if (b.size.x > 150f || b.size.z > 150f || b.max.y < 0.8f || b.min.y > 4f) { continue; }
                    Vector3 d = b.center - CamaraK12; d.y = 0f;
                    float delante = Vector3.Dot(d, dirK12);
                    if (delante > 0f && delante < 3f && Mathf.Abs(Vector3.Cross(dirK12, d).y) < 1.2f) { problemas++; Informe.AppendLine("TAPA LA CÁMARA K12: " + r.transform.name + " a " + delante.ToString("F1") + " m"); }
                }

                Informe.AppendLine("Problemas: " + problemas);
                Informe.AppendLine("Tamaño del prefab (MB): " + (new FileInfo(RutaPrefab).Length / 1048576f).ToString("F1") + " (tope 60)");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contenido);
            }
            Escribir("t1413-validar");
        }

        // ================================================================ Fotos

        [MenuItem("Mecha Cyberpunk/T-1413/Fotos")]
        public static void Fotos()
        {
            ColocarS2.Abrir();
            const string referencias = "imageassets/referencias/look/";
            (string nombre, string referencia, Vector3 desde, Vector3 hacia, float campo)[] tomas =
            {
                ("k12", "K12_Malecon_Muelle.png", CamaraK12, MiraK12, CampoK12),
                ("a11", "A11_Malecon_Aerea.png", CamaraA11, MiraA11, CampoA11),
            };
            Directory.CreateDirectory("docs/capturas/t-1413");
            Directory.CreateDirectory("Logs");
            foreach (var t in tomas)
            {
                string juego = "Logs/t1413-" + t.nombre + ".png";
                ColocarS2cLook.Foto(t.desde, t.hacia, juego, t.campo);
                ColocarS2.LadoALado(referencias + t.referencia, juego, "docs/capturas/t-1413/malecon-" + t.nombre + ".png");
            }
        }

        private static void Escribir(string nombre)
        {
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/" + nombre + ".txt", Informe.ToString());
            AssetDatabase.SaveAssets();
            Debug.Log("[Arte] " + nombre + ":\n" + Informe);
        }
    }
}
