using System.Collections.Generic;
using System.IO;
using System.Linq;
using MechaCyberpunk.Core.Mundo;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using T = MechaCyberpunk.Arte.Editor.ColocarT1356Trazado;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-1356: autopista elevada manejable del borde norte (z = 250, y = 10), túnel al cerro, rampas norte y este, conector, lote baldío debajo y fondo al norte.
    /// Idempotente: Aplicar borra y rehace solo la raíz T1356_Autopista. Tablero, curvas, rampas y túnel son barridos generados con MeshCollider continuo;
    /// las piezas de Blender (AU01 a AU06, en Assets/Arte/ZonasModulos/Prefabs) y la luminaria de Tripo (AU01_Luminaria, en Assets/Arte/Zonas) se colocan por sus
    /// medidas; si una falta se deja un GameObject Hueco_&lt;CODIGO&gt; (sin primitivos) y queda en Logs/t1356-autopista.txt y docs/assets-faltan.md.
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ColocarT1356Autopista.Aplicar (y .Fotos).
    /// </summary>
    public static class ColocarT1356Autopista
    {
        public const string NombreRaiz = "T1356_Autopista";
        public const float SeparacionLucesTunel = 14f;
        public const float SeparacionPostes = 22f;
        public const float SeparacionLuminarias = 30f;
        public const float AlturaLuminaria = 9f;
        public const float GruesoTablero = 1.2f;
        public const float AltoParapeto = 0.9f;
        public const float AltoTunel = 6.5f;
        public const float SeparacionLucesTubo = 8f;
        /// <summary>Cara este del acantilado: 1 m detrás del frente de la boca (x = -330) para que la pieza AU06 no choque con la roca.</summary>
        public const float CaraAcantiladoX = -331f;
        public const float AltoAcantilado = 45f;
        public const float AltoBordeSinTerreno = 25f;
        private const float MedioAsfalto = 6.5f;
        private const float MedioTablero = 7.2f;
        private const string CarpetaModulos = "Assets/Arte/ZonasModulos/Prefabs/";
        private const string CarpetaTripo = "Assets/Arte/Zonas";
        private const string Recta = "AU01_AutopistaRecta";
        private const string Columna = "AU04_ColumnaAutopista";
        private const string Guardarrail = "AU05_Guardarrail";
        private const string Entrada = "AU06_EntradaTunel";
        private const string Luminaria = "AU01_Luminaria";

        private static readonly HashSet<string> faltan = new HashSet<string>();
        private static int luces;

        [MenuItem("Mecha Cyberpunk/T-1356/Autopista elevada")]
        public static void Aplicar()
        {
            ColocarS2.Informe.Clear();
            faltan.Clear();
            luces = 0;
            ColocarS2.Abrir();
            KitS2c.Obra o = KitS2c.Empezar(NombreRaiz, 1356);
            Transform raiz = o.Raiz;

            List<Vector3> principal = T.Principal();
            List<Vector3> rampaNorte = T.RampaNorte();
            List<Vector3> tunel = T.Tunel();
            List<Vector3> calleEste = T.CalleEste();
            List<Vector3> conector = T.ConectorEste();

            Transform tablero = Hijo(raiz, "Tablero");
            bool piezasRecta = Cargar(Recta, false) != null;
            Tramo(o, tablero, "Principal", principal, piezasRecta ? T.IndiceFinRecta : -1);
            if (piezasRecta)
            {
                Rectas(o, tablero, principal.GetRange(0, T.IndiceFinRecta + 1));
            }
            Tramo(o, tablero, "RampaNorte", rampaNorte, -1);
            Piso(o, tablero, "CalleEste", calleEste);
            Piso(o, tablero, "ConectorEste", conector);
            Tunel(o, Hijo(raiz, "Tunel"), tunel);
            Columnas(o, Hijo(raiz, "Columnas"), new[] { principal, rampaNorte });
            GuardarrailVisual(o, Hijo(raiz, "Guardarrail"), new[] { principal, rampaNorte });
            Luminarias(o, Hijo(raiz, "Luminarias"), new[] { principal, rampaNorte });
            ApagarEdificioDeLaUnion();
            Acantilado(o, Hijo(raiz, "Acantilado"));
            Bajo(o, Hijo(raiz, "Bajo"), rampaNorte);
            Fondo(o, Hijo(raiz, "Fondo"));
            Calles(Hijo(raiz, "Calles"), principal, rampaNorte, tunel, calleEste, conector);

            KitS2c.Terminar(o);
            ColocarS2.Informe.AppendLine("Luces reales: " + luces);
            ColocarS2.Informe.AppendLine("Huecos: " + (faltan.Count == 0 ? "ninguno" : string.Join(", ", faltan.OrderBy(f => f))));
            ColocarS2.Guardar("t1356-autopista");
        }

        // ---------------------------------------------------------------- Capturas

        /// <summary>Avisa en Logs si algo con collider (que no sea piso a ras) queda a menos de 5 m de la cámara o la envuelve.</summary>
        private static void RevisarCamara(Vector3 camara)
        {
            Physics.SyncTransforms();
            foreach (Collider c in Physics.OverlapSphere(camara, 5f))
            {
                if (c.bounds.max.y < 0.5f)
                {
                    continue;
                }
                string aviso = "[T1356] CAMARA " + camara + " tapada por " + c.transform.root.name + "/" + c.name + " " + c.bounds;
                ColocarS2.Informe.AppendLine(aviso);
                Debug.LogWarning(aviso);
            }
        }

        /// <summary>K18 (desde una calle baja, rampa al centro) y A18 (aérea), a docs/capturas/zonas/autopista-k18.png y -a18.png.</summary>
        [MenuItem("Mecha Cyberpunk/T-1356/Fotos")]
        public static void Fotos()
        {
            ColocarS2.Abrir();
            Directory.CreateDirectory("docs/capturas/zonas");
            Directory.CreateDirectory("Logs");
            // K18: a pie del barrio de abajo (1.7 m), al sur de la rampa norte, mirando al oeste-noroeste ~12 grados hacia arriba: columna cerca a la derecha,
            // autopista arriba y túnel en el acantilado al fondo.
            Vector3 camaraK18 = new Vector3(-45f, 1.7f, 208f);
            RevisarCamara(camaraK18);
            ColocarS2cLook.Foto(camaraK18, new Vector3(-145f, 22f, 222f), "Logs/autopista-k18-juego.png", 65f);
            ColocarS2.LadoALado("imageassets/referencias/look/K18_AutopistaElevada.png", "Logs/autopista-k18-juego.png", "docs/capturas/zonas/autopista-k18.png");
            // A18: al este-sureste del túnel y por encima: autopista curva entrando al túnel del acantilado, rampa abajo, casitas en primer plano.
            Vector3 camaraA18 = new Vector3(-200f, 55f, 215f);
            RevisarCamara(camaraA18);
            ColocarS2cLook.Foto(camaraA18, new Vector3(-330f, 12f, 250f), "Logs/autopista-a18-juego.png", 62f);
            ColocarS2.LadoALado("imageassets/referencias/look/A18_Autopista_Aerea.png", "Logs/autopista-a18-juego.png", "docs/capturas/zonas/autopista-a18.png");
        }

        // ---------------------------------------------------------------- Tablero

        private static Transform Hijo(Transform padre, string nombre)
        {
            Transform t = new GameObject(nombre).transform;
            t.SetParent(padre, false);
            return t;
        }

        private static Material Concreto() => KitS2c.Mat("T1356_Concreto", () =>
        {
            Material m = new Material(AssetDatabase.LoadAssetAtPath<Material>("Assets/_Proyecto/Mundo/Materiales/Barrio_Concreto.mat"));
            m.SetFloat("_Smoothness", 0.25f);
            m.SetColor("_BaseColor", new Color(0.5f, 0.5f, 0.54f));
            return m;
        });

        private static Material Tierra() => KitS2c.Mat("T1356_Tierra", () => KitS2c.Lit(new Color(0.16f, 0.15f, 0.15f), 0.2f, KitS2c.TexMuro()));

        private static Material LuzTubo() => KitS2c.Mat("T1356_LuzTubo", () => KitS2c.Emision(new Color(2.6f, 2.3f, 1.8f), Texture2D.whiteTexture));

        /// <summary>
        /// Barrido de un rectángulo (u lateral a la derecha, v arriba, en m, respecto a la línea) a lo largo de la línea. Lleva MeshCollider continuo si se pide.
        /// Los ejes se toman en planta: la sección se queda a nivel aunque el tablero suba.
        /// </summary>
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
            GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic);
            return go;
        }

        /// <summary>
        /// Tablero elevado: losa de concreto, capa de asfalto y parapeto de concreto a cada lado (collider continuo). Si hay piezas AU01 en
        /// <paramref name="hastaRecta"/> (índice) el tablero generado de esa recta no se ve (solo el collider). El parapeto se ve solo si falta AU05.
        /// </summary>
        private static void Tramo(KitS2c.Obra o, Transform padre, string nombre, List<Vector3> p, int hastaRecta)
        {
            Transform t = Hijo(padre, nombre);
            bool piezasGuardarrail = Cargar(Guardarrail, false) != null;
            if (hastaRecta > 0)
            {
                // Recta con piezas: collider continuo sin malla visible; curva y bajada generadas se ven.
                Barrer(o, t, "Losa_Recta", p.GetRange(0, hastaRecta + 1), -MedioTablero, MedioTablero, -GruesoTablero, -0.05f, Concreto(), true, false);
                Barrer(o, t, "Asfalto_Recta", p.GetRange(0, hastaRecta + 1), -MedioAsfalto, MedioAsfalto, -0.05f, 0f, KitS2c.MatAsfalto(), true, false);
                List<Vector3> resto = p.GetRange(hastaRecta, p.Count - hastaRecta);
                Barrer(o, t, "Losa", resto, -MedioTablero, MedioTablero, -GruesoTablero, -0.05f, Concreto(), true, true);
                Barrer(o, t, "Asfalto", resto, -MedioAsfalto, MedioAsfalto, -0.05f, 0f, KitS2c.MatAsfalto(), true, true);
            }
            else
            {
                Barrer(o, t, "Losa", p, -MedioTablero, MedioTablero, -GruesoTablero, -0.05f, Concreto(), true, true);
                Barrer(o, t, "Asfalto", p, -MedioAsfalto, MedioAsfalto, -0.05f, 0f, KitS2c.MatAsfalto(), true, true);
            }
            foreach (float lado in new[] { -1f, 1f })
            {
                float a = lado * MedioAsfalto;
                float b = lado * MedioTablero;
                Barrer(o, t, lado < 0 ? "Parapeto_Izq" : "Parapeto_Der", p, Mathf.Min(a, b), Mathf.Max(a, b), 0f, AltoParapeto, Concreto(), true, !piezasGuardarrail);
            }
        }

        /// <summary>Calle a nivel del piso con banquetas de 3 m (0.2 m): asfalto y banquetas con collider.</summary>
        private static void Piso(KitS2c.Obra o, Transform padre, string nombre, List<Vector3> p)
        {
            Transform t = Hijo(padre, nombre);
            Barrer(o, t, "Asfalto", p, -7f, 7f, -0.6f, 0.02f, KitS2c.MatAsfalto(), true, true);
            Barrer(o, t, "Banqueta_Izq", p, -10f, -7f, -0.6f, 0.2f, KitS2c.MatBanqueta(), true, true, 4f);
            Barrer(o, t, "Banqueta_Der", p, 7f, 10f, -0.6f, 0.2f, KitS2c.MatBanqueta(), true, true, 4f);
        }

        /// <summary>Piezas AU01 encadenadas de a a b por sus medidas (se estiran un poco para cerrar sin huecos).</summary>
        private static void Rectas(KitS2c.Obra o, Transform padre, List<Vector3> p)
        {
            GameObject prefab = Cargar(Recta, false);
            Vector3 a = p[0];
            Vector3 b = p[p.Count - 1];
            Medir(prefab, out Vector3 tam, out Vector3 centro);
            // AU01 (Blender) mide 12 de ancho en X por 8 de largo en Z: el sentido de manejo es su Z aunque sea la medida menor.
            bool largoX = false;
            float largo = largoX ? tam.x : tam.z;
            float ancho = largoX ? tam.z : tam.x;
            Vector3 dir = (b - a).normalized;
            float L = Vector3.Distance(a, b);
            int n = Mathf.Max(1, Mathf.RoundToInt(L / largo));
            float esLargo = L / (n * largo);
            float esAncho = ancho > 0.01f && ancho > 0.7f * T.AnchoCalle && ancho < 1.5f * T.AnchoCalle ? T.AnchoCalle / ancho : 1f;
            Vector3 escala = largoX ? new Vector3(esLargo, 1f, esAncho) : new Vector3(esAncho, 1f, esLargo);
            Quaternion giro = Quaternion.LookRotation(largoX ? Vector3.Cross(dir, Vector3.up) : dir);
            float baja = centro.y + tam.y * 0.5f;
            Transform r = Hijo(padre, "Rectas_AU01");
            for (int k = 0; k < n; k++)
            {
                Vector3 c = a + dir * (largo * esLargo * (k + 0.5f));
                Poner(r, prefab, new Vector3(c.x, a.y - baja, c.z), giro, escala, false).name = "AU01_" + k;
            }
        }

        // ---------------------------------------------------------------- Túnel

        private static void Tunel(KitS2c.Obra o, Transform padre, List<Vector3> p)
        {
            Material concreto = Concreto();
            Barrer(o, padre, "Piso", p, -8f, 8f, -GruesoTablero, -0.05f, concreto, true, true);
            Barrer(o, padre, "Asfalto", p, -MedioAsfalto, MedioAsfalto, -0.05f, 0f, KitS2c.MatAsfalto(), true, true);
            Barrer(o, padre, "Pared_Izq", p, -8f, -7f, -GruesoTablero, AltoTunel, concreto, true, true);
            Barrer(o, padre, "Pared_Der", p, 7f, 8f, -GruesoTablero, AltoTunel, concreto, true, true);
            Barrer(o, padre, "Techo", p, -8f, 8f, AltoTunel, AltoTunel + 1.2f, concreto, true, true);
            // Luces de tubo en hilera (emisivas) al centro del techo; pocas Light reales.
            float largo = T.Longitud(p);
            int franjas = Mathf.Max(2, Mathf.FloorToInt(largo / SeparacionLucesTubo));
            for (int k = 0; k < franjas; k++)
            {
                T.PuntoEn(p, largo * (k + 0.5f) / franjas, out Vector3 pt, out Vector3 tg);
                KitS2c.Losa(o, padre, "LuzTubo_" + k, pt + Vector3.up * (AltoTunel - 0.1f), Quaternion.LookRotation(tg), new Vector3(0.6f, 0.15f, 3f), LuzTubo(), 1f, false);
            }
            int luzTunel = Mathf.Max(3, Mathf.CeilToInt(largo / SeparacionLucesTunel));
            for (int k = 0; k < luzTunel; k++)
            {
                T.PuntoEn(p, largo * (k + 0.5f) / luzTunel, out Vector3 pt, out _);
                KitS2c.Luz(padre, pt + Vector3.up * (AltoTunel - 0.8f), LightType.Point, new Color(1f, 0.92f, 0.75f), 160f, 24f, 0f);
                luces++;
            }
            // Bocas: el lado de afuera (+Z de la pieza) mira a donde sale el tráfico.
            Boca(padre, p[0], Vector3.right, "Entrada");
            Boca(padre, p[p.Count - 1], Vector3.back, "Salida");
        }

        private static void Boca(Transform padre, Vector3 punto, Vector3 afuera, string nombre)
        {
            GameObject prefab = Cargar(Entrada, false);
            if (prefab == null)
            {
                Hueco(padre, "AU06", punto, nombre);
                return;
            }
            Medir(prefab, out Vector3 tam, out Vector3 centro);
            Quaternion giro = Quaternion.LookRotation(afuera);
            Vector3 pos = punto - afuera * (tam.z * 0.5f);
            pos.y = punto.y - (centro.y - tam.y * 0.5f);
            Poner(padre, prefab, pos, giro, Vector3.one, true).name = "AU06_" + nombre;
        }

        // ---------------------------------------------------------------- Columnas, guardarraíl, luminarias

        private static void Columnas(KitS2c.Obra o, Transform padre, IEnumerable<List<Vector3>> lineas)
        {
            GameObject prefab = Cargar(Columna, false);
            foreach (List<Vector3> linea in lineas)
            {
                foreach (Vector3 c in T.Columnas(linea))
                {
                    if (prefab == null)
                    {
                        Hueco(padre, "AU04", new Vector3(c.x, 0f, c.z), "Columna");
                        continue;
                    }
                    Medir(prefab, out Vector3 tam, out Vector3 centro);
                    float alto = c.y - GruesoTablero;
                    Vector3 esc = new Vector3(1f, alto / Mathf.Max(0.01f, tam.y), 1f);
                    Vector3 tg = Tangente(linea, c);
                    Vector3 lat = new Vector3(tg.z, 0f, -tg.x);
                    Quaternion giro = Quaternion.LookRotation(tam.x >= tam.z ? Vector3.Cross(lat, Vector3.up) : lat);
                    Poner(padre, prefab, new Vector3(c.x, -(centro.y - tam.y * 0.5f) * esc.y, c.z), giro, esc, true).name = "AU04_Columna";
                }
            }
        }

        private static Vector3 Tangente(List<Vector3> linea, Vector3 punto)
        {
            int mejor = 1;
            float d = float.MaxValue;
            for (int i = 1; i < linea.Count; i++)
            {
                Vector3 m = (linea[i - 1] + linea[i]) * 0.5f;
                float dd = T.Planta(m - punto).sqrMagnitude;
                if (dd < d)
                {
                    d = dd;
                    mejor = i;
                }
            }
            return T.Planta(linea[mejor] - linea[mejor - 1]).normalized;
        }

        private static void GuardarrailVisual(KitS2c.Obra o, Transform padre, IEnumerable<List<Vector3>> lineas)
        {
            GameObject prefab = Cargar(Guardarrail, false);
            if (prefab == null)
            {
                Hueco(padre, "AU05", T.U6Boca, "Guardarrail");
                return;
            }
            Medir(prefab, out Vector3 tam, out Vector3 centro);
            float largo = Mathf.Max(0.5f, tam.x);
            foreach (List<Vector3> linea in lineas)
            {
                float total = T.Longitud(linea);
                int n = Mathf.Max(1, Mathf.RoundToInt(total / largo));
                for (int k = 0; k < n; k++)
                {
                    T.PuntoEn(linea, total * (k + 0.5f) / n, out Vector3 pt, out Vector3 tg);
                    Vector3 der = new Vector3(tg.z, 0f, -tg.x);
                    foreach (float lado in new[] { -1f, 1f })
                    {
                        Vector3 afuera = der * lado;
                        Vector3 pos = pt + afuera * (MedioTablero - tam.z * 0.5f);
                        pos.y = pt.y - (centro.y - tam.y * 0.5f);
                        Poner(padre, prefab, pos, Quaternion.LookRotation(afuera), new Vector3(total / n / largo, 1f, 1f), false).name = "AU05_" + k;
                    }
                }
            }
        }

        private static void Luminarias(KitS2c.Obra o, Transform padre, IEnumerable<List<Vector3>> lineas)
        {
            GameObject prefab = Cargar(Luminaria, true);
            List<(Vector3 punto, Vector3 tangente)> todas = new List<(Vector3, Vector3)>();
            foreach (List<Vector3> linea in lineas)
            {
                todas.AddRange(T.Luminarias(linea, SeparacionLuminarias));
            }
            Vector3 tam = Vector3.one;
            Vector3 centro = Vector3.zero;
            float escala = 1f;
            if (prefab != null)
            {
                Medir(prefab, out tam, out centro);
                escala = Mathf.Clamp(AlturaLuminaria / Mathf.Max(0.01f, tam.y), 0.3f, 3f);
            }
            for (int i = 0; i < todas.Count; i++)
            {
                Vector3 pt = todas[i].punto;
                Vector3 der = new Vector3(todas[i].tangente.z, 0f, -todas[i].tangente.x);
                float lado = i % 2 == 0 ? 1f : -1f;
                Vector3 pie = pt + der * (lado * (MedioTablero - 0.5f));
                if (prefab == null)
                {
                    Hueco(padre, "AU01", pie, "Luminaria");
                    continue;
                }
                Vector3 haciaCalle = -der * lado;
                GameObject go = Poner(padre, prefab, pie, Quaternion.identity, Vector3.one * escala, false);
                go.name = "AU01_Luminaria_" + i;
                // El brazo es lo que sobresale del pie en planta: se gira hacia la calle.
                Vector3 brazo = T.Planta(centro) * escala;
                if (brazo.magnitude > 0.3f)
                {
                    go.transform.rotation = Quaternion.Euler(0f, Vector3.SignedAngle(brazo, haciaCalle, Vector3.up), 0f);
                }
                Bounds b = go.GetComponentsInChildren<Renderer>().Select(r => r.bounds).Aggregate((x, y) => { x.Encapsulate(y); return x; });
                // El foco está en la punta del brazo (lo más lejano al pie hacia la vía) y su spot apunta al asfalto, al carril de ese lado.
                float alcance = Vector3.Dot(b.center - pie, haciaCalle) + Mathf.Abs(b.extents.x * haciaCalle.x) + Mathf.Abs(b.extents.z * haciaCalle.z);
                alcance = Mathf.Clamp(alcance, 0.5f, 5f);
                Vector3 cabeza = pie + haciaCalle * alcance;
                cabeza.y = b.max.y - 0.3f;
                Vector3 asfalto = pie + haciaCalle * (MedioTablero - 0.5f - 3.5f);
                asfalto.y = pt.y;
                KitS2c.Luz(go.transform, cabeza, LightType.Spot, KitS2c.Calido, KitS2c.IntensidadPoste * 1.3f, 22f, 100f, asfalto - cabeza);
                luces++;
                KitS2c.Halo(o, go.transform, cabeza, pt.y, KitS2c.Sodio);
            }
        }

        // ---------------------------------------------------------------- Acantilado del cerro

        /// <summary>
        /// Masa de roca de x -480 a -330 y z 205 a 290 que sube a ~45 m, con la cara este vertical en x = -331 (ahí entra el túnel) y unida por el sur
        /// al terreno del cerro (altura medida con rayos en z = 205). UV en espacio de mundo, material ZM_Roca y MeshCollider.
        /// </summary>
        private static void Acantilado(KitS2c.Obra o, Transform padre)
        {
            Physics.SyncTransforms();
            List<float> xs = new List<float>();
            for (float x = -480f; x <= -340f; x += 10f)
            {
                xs.Add(x);
            }
            xs.AddRange(new[] { -335f, CaraAcantiladoX, CaraAcantiladoX + 0.5f });
            List<float> zs = new List<float>();
            for (float z = 205f; z <= 290f; z += 5f)
            {
                zs.Add(z);
            }
            int nx = xs.Count;
            int nz = zs.Count;
            float[] borde = new float[nx];
            for (int i = 0; i < nx; i++)
            {
                RaycastHit[] golpes = Physics.RaycastAll(new Vector3(xs[i], 300f, 205f), Vector3.down, 400f);
                borde[i] = golpes.Length == 0 ? AltoBordeSinTerreno : Mathf.Max(AltoBordeSinTerreno, golpes.Max(g => g.point.y));
            }
            List<Vector3> v = new List<Vector3>();
            List<Vector2> uv = new List<Vector2>();
            for (int i = 0; i < nx; i++)
            {
                for (int j = 0; j < nz; j++)
                {
                    float x = xs[i];
                    float z = zs[j];
                    float tope = AltoAcantilado + 5f * Mathf.Sin(x * 0.07f) * Mathf.Cos(z * 0.09f);
                    float t = Mathf.SmoothStep(0f, 1f, (z - 205f) / 25f);
                    float h = Mathf.Lerp(borde[i], tope, t);
                    if (i == nx - 1)
                    {
                        h = -0.5f;
                    }
                    v.Add(new Vector3(x, h, z));
                    uv.Add(new Vector2(x / 8f, (z + h) / 8f));
                }
            }
            List<int> tris = new List<int>();
            for (int i = 0; i < nx - 1; i++)
            {
                for (int j = 0; j < nz - 1; j++)
                {
                    int a = i * nz + j;
                    int b = a + 1;
                    int c = a + nz;
                    int d = c + 1;
                    tris.AddRange(new[] { a, b, c, b, d, c });
                }
            }
            Mesh m = new Mesh { name = o.Nombre + "_Acantilado", indexFormat = IndexFormat.UInt32 };
            m.SetVertices(v);
            m.SetUVs(0, uv);
            m.SetTriangles(tris, 0);
            m.RecalculateNormals();
            m.RecalculateTangents();
            m.RecalculateBounds();
            o.Mallas.Add(m);
            GameObject go = new GameObject("Acantilado");
            go.transform.SetParent(padre, false);
            go.AddComponent<MeshFilter>().sharedMesh = m;
            Material roca = AssetDatabase.LoadAssetAtPath<Material>("Assets/Arte/ZonasModulos/Materiales/ZM_Roca.mat");
            go.AddComponent<MeshRenderer>().sharedMaterial = roca != null ? roca : KitS2c.Lit(new Color(0.12f, 0.12f, 0.14f), 0.1f, KitS2c.TexMuro());
            go.AddComponent<MeshCollider>().sharedMesh = m;
            GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic);
        }

        /// <summary>El remate de T530_Afueras/Salida_2 (x -111, z 201 a 215) toca la unión U7: se apaga si pisa el enlace.</summary>
        private static void ApagarEdificioDeLaUnion()
        {
            Transform ed = ColocarS2.Buscar("T530_Afueras/Salida_2/Edificio");
            if (ed == null)
            {
                ColocarS2.Informe.AppendLine("T530_Afueras/Salida_2/Edificio: no está (nada que apagar)");
                return;
            }
            Renderer[] rs = ed.GetComponentsInChildren<Renderer>(true);
            Collider[] cs = ed.GetComponentsInChildren<Collider>(true);
            Bounds? caja = null;
            foreach (Bounds b in rs.Select(r => r.bounds).Concat(cs.Select(c => c.bounds)))
            {
                Bounds w = b;
                if (caja.HasValue)
                {
                    w = caja.Value;
                    w.Encapsulate(b);
                }
                caja = w;
            }
            bool pisa = caja.HasValue && caja.Value.min.x < -85f && caja.Value.max.x > -135f && caja.Value.min.z < 235f && caja.Value.max.z > 195f;
            if (pisa)
            {
                ed.gameObject.SetActive(false);
            }
            ColocarS2.Informe.AppendLine("T530_Afueras/Salida_2/Edificio " + (pisa ? "apagado (tocaba la unión U7): " : "no toca la unión: ") + (caja.HasValue ? caja.Value.ToString() : "sin medidas"));
        }

        // ---------------------------------------------------------------- Debajo: barrio bajo, chatarra, casitas

        private static void Bajo(KitS2c.Obra o, Transform padre, List<Vector3> rampaNorte)
        {
            // Piso de tierra: queda 2 cm bajo el piso de la ciudad, así no compite con el suelo que ya haya.
            KitS2c.Losa(o, padre, "SueloBaldio", new Vector3(7f, -0.52f, 246f), Quaternion.identity, new Vector3(676f, 1f, 86f), Tierra(), 8f, true);
            List<Vector3> columnas = T.Columnas(rampaNorte);
            columnas.AddRange(T.Columnas(T.Principal()));
            Transform casas = Hijo(padre, "Casitas");
            Transform calles = Hijo(padre, "CallesBarrio");
            Transform postes = Hijo(padre, "Postes");
            Transform chatarra = Hijo(padre, "Chatarra");
            System.Func<int, KitS2c.Estilo> estilo = i =>
            {
                KitS2c.Estilo e = KitS2c.EstiloAlAzar(o);
                e.Pisos = o.Chance(0.35f) ? 2 : 1;
                return e;
            };
            // Dos manzanas a los lados de la rampa norte, cada una con su calle (z = 216) entre dos hileras de casitas, con postes y cables.
            (float x0, float x1)[] bloques = { (-290f, -135f), (40f, 225f) };
            foreach ((float x0, float x1) in bloques)
            {
                KitS2c.Losa(o, calles, "Calle_" + (int)x0, new Vector3((x0 + x1) * 0.5f, -0.03f, 216f), Quaternion.identity, new Vector3(x1 - x0, 0.1f, 8f), KitS2c.MatAsfalto(), 8f, true);
                KitS2c.Frente(o, casas, new Vector3(x0, 0f, 210f), new Vector3(x1, 0f, 210f), Vector3.forward, 6f, estilo);
                KitS2c.Frente(o, casas, new Vector3(x0, 0f, 222f), new Vector3(x1, 0f, 222f), Vector3.back, 12f, estilo);
                Vector3? anterior = null;
                for (float x = x0 + 8f; x < x1 - 4f; x += SeparacionPostes)
                {
                    Vector3 punta = KitS2c.Poste(o, postes, new Vector3(x, 0f, 211f), Vector3.forward, 0f, o.Chance(0.4f));
                    luces++;
                    if (anterior.HasValue)
                    {
                        KitS2c.Cable(o, anterior.Value, punta, 0.6f);
                    }
                    anterior = punta;
                }
            }
            // Hilera al norte de la autopista, de cara al tablero.
            KitS2c.Frente(o, casas, new Vector3(-320f, 0f, 266f), new Vector3(325f, 0f, 266f), Vector3.back, 12f, estilo);
            // Chatarra, basura y barriles en los patios de abajo del tablero y delante de la hilera norte.
            string[] basura = { "BS01a_Basura", "BS01b_Basura", "BS01c_Basura", "BS01d_Basura", "BS01e_Basura" };
            string[] cosas = { "C16_Tambo", "C11_Basurero", "BT01_BoteBasura", "C17_Huacales" };
            GameObject chatarraTripo = Cargar("IN02_Chatarra", true);
            if (chatarraTripo == null)
            {
                Hueco(chatarra, "IN02", new Vector3(0f, 0f, 240f), "Chatarra");
            }
            int puestos = 0;
            for (int intento = 0; intento < 2500 && puestos < 480; intento++)
            {
                // Mitad debajo del tablero (z 236 a 258) y el resto en los patios delante de la hilera norte.
                bool norte = o.Chance(0.2f);
                Vector3 p = new Vector3(o.Rango(-320f, 235f), 0f, norte ? o.Rango(258f, 264f) : o.Rango(236f, 258f));
                if (Cerca(rampaNorte, p, 14f) || columnas.Any(q => Mathf.Abs(q.x - p.x) < 6f && Mathf.Abs(q.z - p.z) < 3f))
                {
                    continue;
                }
                Quaternion giro = Quaternion.Euler(0f, o.Rango(0f, 360f), 0f);
                if (puestos % 5 == 0 && chatarraTripo != null)
                {
                    GameObject c = Poner(chatarra, chatarraTripo, p, giro, Vector3.one, false);
                    Bounds b = c.GetComponentsInChildren<Renderer>().Select(r => r.bounds).Aggregate((x, y) => { x.Encapsulate(y); return x; });
                    c.transform.position += Vector3.up * (0.02f - b.min.y);
                }
                else
                {
                    KitS2c.Pieza(chatarra, puestos % 3 == 0 ? o.Uno(cosas) : o.Uno(basura), p, giro, Vector3.one, false);
                }
                puestos++;
            }
            GameObject[] carros =
            {
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Arte/Vehiculos/Prefabs/V01a_CarroViejito.prefab"),
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Arte/Vehiculos/Prefabs/V01b_CarroPro.prefab"),
            };
            for (int intento = 0, k = 0; k < 30 && intento < 800; intento++)
            {
                Vector3 p = new Vector3(o.Rango(-300f, 225f), 0f, o.Chance(0.7f) ? o.Rango(236f, 257f) : 216f + o.Rango(-2f, 2f));
                bool enCalle = Mathf.Abs(p.z - 216f) < 3f;
                if (Cerca(rampaNorte, p, 16f) || columnas.Any(q => Mathf.Abs(q.x - p.x) < 8f && Mathf.Abs(q.z - p.z) < 4f) || (enCalle && !EnBloque(p.x, bloques)))
                {
                    continue;
                }
                GameObject prefab = carros[k % carros.Length];
                if (prefab == null)
                {
                    break;
                }
                Quaternion giro = Quaternion.Euler(0f, enCalle ? (o.Chance(0.5f) ? 90f : -90f) : o.Rango(0f, 360f), 0f);
                GameObject c = Poner(chatarra, prefab, p, giro, Vector3.one, true);
                c.name = "CarroViejo_" + k++;
                Bounds b = c.GetComponentsInChildren<Renderer>().Select(r => r.bounds).Aggregate((x, y) => { x.Encapsulate(y); return x; });
                c.transform.position += Vector3.up * (0.02f - b.min.y);
            }
            // Charcos que reflejan la luz cálida (A18/K18): en las calles y en los patios.
            for (int k = 0, intento = 0; k < 70 && intento < 600; intento++)
            {
                bool enCalle = o.Chance(0.35f);
                Vector3 p = enCalle ? new Vector3(o.Rango(-285f, 220f), 0.03f, 216f + o.Rango(-2.5f, 2.5f)) : new Vector3(o.Rango(-300f, 230f), 0.03f, o.Rango(236f, 258f));
                if (Cerca(rampaNorte, p, 12f) || (enCalle && !EnBloque(p.x, bloques)))
                {
                    continue;
                }
                KitS2c.Charco(o, chatarra, p, o.Rango(2f, 5f), o.Rango(1.5f, 3.5f), o.Rango(0f, 180f));
                k++;
            }
        }

        private static bool EnBloque(float x, (float x0, float x1)[] bloques)
        {
            return bloques.Any(b => x > b.x0 + 3f && x < b.x1 - 3f);
        }


        private static bool Cerca(List<Vector3> linea, Vector3 p, float distancia)
        {
            return linea.Any(q => T.Planta(q - p).magnitude < distancia);
        }

        // ---------------------------------------------------------------- Fondo al norte

        /// <summary>Torres lejanas (kit T20, T21, T23 como T-541) más allá del límite norte, para que no se vea el vacío.</summary>
        private static void Fondo(KitS2c.Obra o, Transform padre)
        {
            GameObject t20 = Cargar("T20", false, "Assets/Arte/Exteriores/Prefabs/");
            GameObject t21 = Cargar("T21", false, "Assets/Arte/Exteriores/Prefabs/");
            GameObject t23 = Cargar("T23", false, "Assets/Arte/Exteriores/Prefabs/");
            if (t20 == null || t21 == null || t23 == null)
            {
                Hueco(padre, "FondoTorres", new Vector3(0f, 0f, 310f), "Fondo");
                return;
            }
            for (float x = -330f; x <= 345f; x += 34f)
            {
                Transform torre = Hijo(padre, "Torre_" + (int)x);
                torre.position = new Vector3(x + o.Rango(-6f, 6f), 0f, o.Rango(300f, 345f));
                float ancho = o.Rango(22f, 34f);
                float fondo = o.Rango(22f, 34f);
                int pisos = o.Azar.Next(5, 14);
                float y = Capa(torre, t20, "Base", 0f, 12f, ancho, fondo);
                for (int i = 0; i < pisos; i++)
                {
                    y = Capa(torre, t21, "Piso", y, 6f, ancho, fondo);
                }
                Capa(torre, t23, "Remate", y, 12f, ancho, fondo);
                torre.rotation = Quaternion.LookRotation(Vector3.back, Vector3.up);
            }
        }

        private static float Capa(Transform torre, GameObject prefab, string nombre, float y, float alto, float ancho, float fondo)
        {
            GameObject g = (GameObject)PrefabUtility.InstantiatePrefab(prefab, torre);
            g.name = nombre;
            g.transform.localRotation = Quaternion.identity;
            g.transform.localScale = Vector3.one;
            Bounds b = g.GetComponentsInChildren<Renderer>().Select(r => r.bounds).Aggregate((p, q) => { p.Encapsulate(q); return p; });
            g.transform.localScale = new Vector3(ancho / Mathf.Max(0.01f, b.size.x), alto / Mathf.Max(0.01f, b.size.y), fondo / Mathf.Max(0.01f, b.size.z));
            g.transform.localPosition = new Vector3(0f, y, 0f);
            foreach (Renderer r in g.GetComponentsInChildren<Renderer>())
            {
                r.shadowCastingMode = ShadowCastingMode.Off;
            }
            foreach (Collider c in g.GetComponentsInChildren<Collider>())
            {
                c.enabled = false;
            }
            GameObjectUtility.SetStaticEditorFlags(g, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic);
            return y + alto;
        }

        // ---------------------------------------------------------------- Calles (contrato Core)

        private static void Calles(Transform padre, List<Vector3> principal, List<Vector3> rampaNorte, List<Vector3> tunel, List<Vector3> calleEste, List<Vector3> conector)
        {
            Calle(padre, "Calle_Principal", principal, false, true);
            Calle(padre, "Calle_RampaNorte", rampaNorte, false, true);
            Calle(padre, "Calle_Tunel", tunel, false, true);
            Calle(padre, "Calle_Este_x300", calleEste, true, false);
            Calle(padre, "Calle_ConectorU9", conector, true, false);
        }

        private static void Calle(Transform padre, string nombre, List<Vector3> puntos, bool banquetas, bool autopista)
        {
            GameObject go = new GameObject(nombre);
            go.transform.SetParent(padre, false);
            go.AddComponent<CalleZona>().Definir(puntos, T.AnchoCalle, banquetas, autopista, "Autopista");
        }

        // ---------------------------------------------------------------- Piezas

        private static GameObject Cargar(string codigo, bool tripo, string carpeta = CarpetaModulos)
        {
            if (!tripo)
            {
                return AssetDatabase.LoadAssetAtPath<GameObject>(carpeta + codigo + ".prefab");
            }
            if (!Directory.Exists(CarpetaTripo))
            {
                return null;
            }
            string guid = AssetDatabase.FindAssets(codigo + " t:Prefab", new[] { CarpetaTripo })
                .FirstOrDefault(g => Path.GetFileNameWithoutExtension(AssetDatabase.GUIDToAssetPath(g)).StartsWith(codigo));
            return guid == null ? null : AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
        }

        /// <summary>Tamaño de la caja visible de la pieza y de su centro respecto al pivote (sin girar, escala 1).</summary>
        private static void Medir(GameObject prefab, out Vector3 tam, out Vector3 centro)
        {
            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
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

        private static void Hueco(Transform padre, string codigo, Vector3 pos, string nombre)
        {
            faltan.Add("Hueco_" + codigo);
            if (padre.Find("Hueco_" + codigo) != null)
            {
                return;
            }
            GameObject h = new GameObject("Hueco_" + codigo);
            h.transform.SetParent(padre, false);
            h.transform.position = pos;
        }
    }
}
