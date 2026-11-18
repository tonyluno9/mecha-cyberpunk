using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using MechaCyberpunk.Core.Mundo;
using MechaCyberpunk.Core.Trepar;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-1357: el Puerto (rectángulo x 115..330, z -45..-250; muelle al sur y al este; puertas U5 y U8). Patio de concreto mojado, bloques de
    /// contenedores de 40' escalonados (con escaleras y pallets para treparlos), bodegas, grúas, barco, montacargas, bitas, reja y postes.
    /// El acomodo (bloques y calles) es puro y lo prueban las pruebas de EditMode; la escena se rehace en la raíz T1357_Puerto.
    /// Piezas por nombre; si falta el modelo queda un "Hueco_CODIGO" vacío (docs/assets-faltan.md). Nunca corre CreadorTodo.
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ColocarT1357Puerto.Aplicar (y .Fotos)
    /// </summary>
    public static class ColocarT1357Puerto
    {
        public const string Raiz = "T1357_Puerto";
        public const int Semilla = 13570;

        // ---------------------------------------------------------------- Acomodo puro (lo prueban las pruebas)

        public const float X0 = 115f, X1 = 330f, ZNorte = -45f, ZSur = -250f;
        public const float AnchoCalle = 12f;
        public const float LargoCont = 12.19f, AnchoCont = 2.44f, AltoCont = 2.59f;
        public const float PasoFila = 2.55f;
        public const int Filas = 4;
        public const float LargoBloque = 24.6f, FondoBloque = Filas * PasoFila;
        public const float PasilloX = 3.6f, PasilloZ = 3.5f;
        public static readonly Vector3 U5 = new Vector3(160f, 0f, -215f);
        public static readonly Vector3 U8 = new Vector3(300f, 0f, -45f);

        /// <summary>Un bloque de contenedores: 2 contenedores de largo (en x) y 4 filas (en z); la fila 0 es la del sur y sube hacia el norte.</summary>
        public readonly struct Bloque
        {
            public readonly Vector2 Centro;
            public readonly int[] AlturaFila;

            public Bloque(Vector2 centro, int[] alturaFila)
            {
                Centro = centro;
                AlturaFila = alturaFila;
            }

            public Vector2 Tam => new Vector2(LargoBloque, FondoBloque);
            public float ZFila(int fila) => Centro.y - FondoBloque * 0.5f + PasoFila * (fila + 0.5f);
        }

        /// <summary>Las calles internas de 12 m. Todas se unen a otra; las puntas U5 y U8 son las puertas.</summary>
        public static List<Vector3[]> Calles()
        {
            return new List<Vector3[]>
            {
                new[] { U8, new Vector3(300f, 0f, -80f), new Vector3(160f, 0f, -80f), U5 },
                new[] { U5, new Vector3(300f, 0f, -215f), new Vector3(300f, 0f, -80f) },
                new[] { new Vector3(160f, 0f, -150f), new Vector3(300f, 0f, -150f) },
                new[] { new Vector3(190f, 0f, -215f), new Vector3(190f, 0f, -232f), new Vector3(300f, 0f, -232f), new Vector3(300f, 0f, -215f) },
            };
        }

        /// <summary>Bloques de contenedores: campo norte 4x4 (con una plaza libre al centro) y campo sur 4x3.</summary>
        public static List<Bloque> Bloques()
        {
            System.Random azar = new System.Random(Semilla);
            List<Bloque> lista = new List<Bloque>();
            float[] xs = new float[4];
            for (int c = 0; c < 4; c++)
            {
                xs[c] = 187f + c * (LargoBloque + PasilloX);
            }
            for (int f = 0; f < 4; f++)
            {
                for (int c = 0; c < 4; c++)
                {
                    if (f == 1 && c == 1)
                    {
                        continue; // plaza abierta del patio norte (montacargas y pallets)
                    }
                    lista.Add(new Bloque(new Vector2(xs[c], -92.5f - f * (FondoBloque + PasilloZ)), Alturas(azar)));
                }
            }
            for (int f = 0; f < 3; f++)
            {
                for (int c = 0; c < 4; c++)
                {
                    lista.Add(new Bloque(new Vector2(xs[c], -165f - f * (FondoBloque + PasilloZ)), Alturas(azar)));
                }
            }
            return lista;
        }

        // Escalonado: la fila 0 es de 1; cada fila sube 0 o 1 contenedores (máximo 4), así cada escalón se trepa con una escalera de 2.6 m.
        private static int[] Alturas(System.Random azar)
        {
            int[] h = new int[Filas];
            h[0] = 1;
            for (int i = 1; i < Filas; i++)
            {
                h[i] = Mathf.Min(4, h[i - 1] + (azar.NextDouble() < 0.6 ? 1 : 0));
            }
            return h;
        }

        /// <summary>Distancia en planta de un punto a un segmento.</summary>
        public static float DistanciaASegmento(Vector3 p, Vector3 a, Vector3 b)
        {
            Vector2 q = new Vector2(p.x, p.z), s = new Vector2(a.x, a.z), t = new Vector2(b.x, b.z);
            Vector2 d = t - s;
            float k = d.sqrMagnitude < 1e-6f ? 0f : Mathf.Clamp01(Vector2.Dot(q - s, d) / d.sqrMagnitude);
            return Vector2.Distance(q, s + d * k);
        }

        /// <summary>Distancia en planta de un punto a una polilínea.</summary>
        public static float DistanciaACalle(Vector3 p, Vector3[] calle)
        {
            float m = float.MaxValue;
            for (int i = 1; i < calle.Length; i++)
            {
                m = Mathf.Min(m, DistanciaASegmento(p, calle[i - 1], calle[i]));
            }
            return m;
        }

        // ---------------------------------------------------------------- Escena

        private const float YPatio = 0f, YMuelle = -1f, YMar = -3f, MuelleSur = -238f, MuelleEste = 318f;
        private static readonly Dictionary<string, GameObject> prefabs = new Dictionary<string, GameObject>();
        private static readonly List<string> huecos = new List<string>();

        [MenuItem("Mecha Cyberpunk/Zonas/T-1357 Puerto")]
        public static void Aplicar()
        {
            ColocarS2.Informe.Clear();
            huecos.Clear();
            prefabs.Clear();
            ColocarS2.Abrir();
            KitS2c.Obra o = KitS2c.Empezar(Raiz, Semilla);
            Transform r = o.Raiz;
            Piso(o, r);
            Calles(o, r);
            Bloques(o, r);
            Bodegas(o, r);
            Muelle(o, r);
            Entrada(o, r);
            Postes(o, r);
            Patio(o, r);
            CrearMar(r);
            foreach (Transform t in r.GetComponentsInChildren<Transform>(true))
            {
                if (t.GetComponent<Light>() == null)
                {
                    GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.BatchingStatic | StaticEditorFlags.ReflectionProbeStatic);
                }
            }
            KitS2c.Terminar(o);
            ColocarS2.Informe.AppendLine("Puerto: " + Bloques().Count + " bloques, " + o.Postes + " postes, " + o.Charcos + " charcos, luces " + r.GetComponentsInChildren<Light>(true).Length);
            ColocarS2.Informe.AppendLine("Huecos: " + (huecos.Count == 0 ? "ninguno" : string.Join(", ", huecos.Distinct())));
            ColocarS2.Guardar("t1357-puerto");
        }

        private static void Piso(KitS2c.Obra o, Transform r)
        {
            Material concreto = KitS2c.Mat("T1357_Concreto", () => KitS2c.Lit(new Color(0.4f, 0.41f, 0.45f), 0.78f, KitS2c.TexMuro()));
            float ancho = MuelleEste - X0, fondo = ZNorte - MuelleSur;
            KitS2c.Losa(o, r, "Patio", new Vector3(X0 + ancho * 0.5f, YPatio - 1.5f, ZNorte - fondo * 0.5f), Quaternion.identity, new Vector3(ancho, 3f, fondo), concreto, 6f, true);
            KitS2c.Losa(o, r, "MuelleSur", new Vector3((X0 + X1) * 0.5f, YMuelle - 1.5f, (MuelleSur + ZSur) * 0.5f), Quaternion.identity, new Vector3(X1 - X0, 3f, MuelleSur - ZSur), concreto, 6f, true);
            KitS2c.Losa(o, r, "MuelleEste", new Vector3((MuelleEste + X1) * 0.5f, YMuelle - 1.5f, (ZNorte + MuelleSur) * 0.5f), Quaternion.identity, new Vector3(X1 - MuelleEste, 3f, ZNorte - MuelleSur), concreto, 6f, true);
            // Rampita de 1 m del patio al muelle para caminar (escalón en y -1).
            Material bordillo = KitS2c.Mat("T1357_Bordillo", () => KitS2c.Lit(new Color(0.55f, 0.52f, 0.2f), 0.4f, KitS2c.TexMuro()));
            KitS2c.Losa(o, r, "BordilloSur", new Vector3((X0 + X1) * 0.5f, YMuelle + 0.2f, ZSur + 0.25f), Quaternion.identity, new Vector3(X1 - X0, 0.4f, 0.5f), bordillo, 2f, true);
            KitS2c.Losa(o, r, "BordilloEste", new Vector3(X1 - 0.25f, YMuelle + 0.2f, (ZNorte + ZSur) * 0.5f + 2f), Quaternion.identity, new Vector3(0.5f, 0.4f, ZNorte - ZSur - 4f), bordillo, 2f, true);
        }

        private static void Calles(KitS2c.Obra o, Transform r)
        {
            Material asfalto = KitS2c.MatAsfalto();
            int n = 0;
            foreach (Vector3[] calle in Calles())
            {
                n++;
                GameObject go = new GameObject("Calle_" + n);
                go.transform.SetParent(r, false);
                go.AddComponent<CalleZona>().Definir(calle, AnchoCalle, false, false, "Puerto");
                for (int i = 1; i < calle.Length; i++)
                {
                    Vector3 a = calle[i - 1], b = calle[i];
                    Vector3 d = b - a;
                    float largo = d.magnitude + AnchoCalle;
                    KitS2c.Losa(o, go.transform, "Tramo", (a + b) * 0.5f + Vector3.up * (YPatio + 0.04f), Quaternion.LookRotation(d.normalized) * Quaternion.Euler(0f, 90f, 0f),
                        new Vector3(largo, 0.08f, AnchoCalle), asfalto, 8f, false);
                    for (float t = 4f; t < d.magnitude - 3f; t += 6f)
                    {
                        Vector3 p = a + d.normalized * t;
                        KitS2c.Linea(go.transform, p, p + d.normalized * 3f, 0.15f, YPatio + 0.08f);
                    }
                }
            }
        }

        private static void Bloques(KitS2c.Obra o, Transform r)
        {
            Color[] colores =
            {
                new Color(0.62f, 0.22f, 0.14f), new Color(0.16f, 0.42f, 0.26f), new Color(0.14f, 0.28f, 0.58f),
                new Color(0.85f, 0.45f, 0.1f), new Color(0.1f, 0.5f, 0.5f),
            };
            Transform padre = new GameObject("Contenedores").transform;
            padre.SetParent(r, false);
            int b = 0;
            foreach (Bloque bl in Bloques())
            {
                b++;
                GameObject g = new GameObject("Bloque_" + b);
                g.transform.SetParent(padre, false);
                List<Renderer> rend = new List<Renderer>();
                for (int f = 0; f < Filas; f++)
                {
                    float z = bl.ZFila(f);
                    for (int c = 0; c < 2; c++)
                    {
                        float x = bl.Centro.x + (c == 0 ? -1f : 1f) * LargoCont * 0.5f;
                        int h = c == 1 && o.Chance(0.3f) ? Mathf.Max(1, bl.AlturaFila[f] - 1) : bl.AlturaFila[f];
                        for (int k = 0; k < h; k++)
                        {
                            GameObject cont = Colocar(g.transform, "PT01_Contenedor40", new Vector3(x, YPatio + k * AltoCont, z), o.Chance(0.12f) ? 180f : 0f, LargoCont, 0f, true, false);
                            Tintar(cont, colores[o.Azar.Next(colores.Length)], o.Azar.Next(2));
                            rend.AddRange(cont.GetComponentsInChildren<Renderer>());
                        }
                        BoxCollider bc = g.AddComponent<BoxCollider>();
                        bc.center = new Vector3(x, YPatio + h * AltoCont * 0.5f, z) - g.transform.position;
                        bc.size = new Vector3(LargoCont, h * AltoCont, AnchoCont);
                    }
                }
                if (rend.Count > 0)
                {
                    LODGroup lg = g.AddComponent<LODGroup>();
                    lg.SetLODs(new[] { new LOD(0.012f, rend.ToArray()) });
                    lg.RecalculateBounds();
                }
                Escaleras(g.transform, bl);
                // Pallets al sur de la fila 0 (escalón de 1.2 m) en la columna oeste.
                Colocar(g.transform, "PU06_Pallets", new Vector3(bl.Centro.x - LargoCont * 0.5f, YPatio, bl.ZFila(0) - PasoFila * 0.5f - 0.9f), 0f, 0f, 1.2f, false, false);
            }
        }

        // Escalera de 2.6 m desde el suelo a la fila 0 (columna este) y una por escalón entre filas, en la columna oeste (tienen el mismo escalón).
        private static void Escaleras(Transform g, Bloque bl)
        {
            Escalera(g, "Escalera_Suelo", new Vector3(bl.Centro.x + LargoCont * 0.5f, YPatio, bl.ZFila(0) - PasoFila * 0.5f - 0.15f), AltoCont);
            for (int f = 0; f < Filas - 1; f++)
            {
                if (bl.AlturaFila[f + 1] > bl.AlturaFila[f])
                {
                    float y = YPatio + bl.AlturaFila[f] * AltoCont;
                    Escalera(g, "Escalera_Fila" + f, new Vector3(bl.Centro.x - LargoCont * 0.5f, y, bl.ZFila(f) + PasoFila * 0.5f - 0.1f), AltoCont);
                }
            }
        }

        private static void Escalera(Transform padre, string nombre, Vector3 pie, float alto)
        {
            GameObject go = new GameObject(nombre);
            go.transform.SetParent(padre, false);
            go.transform.SetPositionAndRotation(pie, Quaternion.LookRotation(Vector3.forward));
            PuntoTrepar p = go.AddComponent<PuntoTrepar>();
            SerializedObject so = new SerializedObject(p);
            so.FindProperty("tipo").enumValueIndex = (int)TipoTrepar.Escalera;
            so.FindProperty("alturaSubida").floatValue = alto;
            so.FindProperty("salida").vector3Value = new Vector3(0f, alto, 0.8f);
            so.ApplyModifiedPropertiesWithoutUndo();
            p.AjustarTrigger();
        }

        private static void Bodegas(KitS2c.Obra o, Transform r)
        {
            Transform padre = new GameObject("Bodegas").transform;
            padre.SetParent(r, false);
            // Fila norte, de frente al sur (calle z = -80).
            string[] codigos = { "PU03_Bodega", "PU04_Bodega", "PU03_Bodega" };
            for (int i = 0; i < 3; i++)
            {
                float x = 195f + i * 40f;
                Colocar(padre, codigos[i], new Vector3(x, YPatio, -66f), 180f, 32f, 0f, false, false);
                Color c = i == 1 ? KitS2c.Turquesa : KitS2c.Magenta;
                KitS2c.Neon(o, padre, new Vector3(x, 7f, -78.5f), Vector3.back, Vector3.right, c, true);
                KitS2c.Luz(padre, new Vector3(x - 12f, 10f, -78f), LightType.Spot, KitS2c.Fria, 600f, 30f, 80f, new Vector3(0f, -1f, -0.6f));
            }
            // Columna oeste, de frente al este (calle x = 160).
            for (int i = 0; i < 3; i++)
            {
                Colocar(padre, i == 1 ? "PU04_Bodega" : "PU03_Bodega", new Vector3(132f, YPatio, -112f - i * 32f), 90f, 28f, 0f, false, false);
            }
        }

        private static void Muelle(KitS2c.Obra o, Transform r)
        {
            Transform padre = new GameObject("Muelle").transform;
            padre.SetParent(r, false);
            for (float x = 200f; x <= 300f; x += 50f)
            {
                Colocar(padre, "PU01_Grua", new Vector3(x, YMuelle, ZSur + 6f), 0f, 0f, 42f, false, true);
                LucesRojas(padre, new Vector3(x, YMuelle + 40f, ZSur + 6f));
            }
            Colocar(padre, "PU02_Barco", new Vector3(240f, YMar - 2f, ZSur - 28f), 0f, 130f, 0f, false, false);
            for (float x = X0 + 8f; x < X1; x += 12f)
            {
                Colocar(padre, "PT02_BitaAmarre", new Vector3(x, YMuelle, ZSur + 1.5f), 0f, 0f, 0.7f, false, false);
            }
            for (float z = ZNorte - 12f; z > ZSur + 8f; z -= 12f)
            {
                Colocar(padre, "PT02_BitaAmarre", new Vector3(X1 - 1.5f, YMuelle, z), 90f, 0f, 0.7f, false, false);
            }
            // Cadena de cuerda y reja del muelle no se pone: el muro invisible del borde es de T-1358.
        }

        private static void LucesRojas(Transform padre, Vector3 cima)
        {
            Material rojo = KitS2c.Mat("T1357_LuzRoja", () =>
            {
                Material m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                m.SetColor("_BaseColor", new Color(0.3f, 0.02f, 0.02f));
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", new Color(4f, 0.1f, 0.1f));
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
                return m;
            });
            for (int i = -1; i <= 1; i += 2)
            {
                GameObject s = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Object.DestroyImmediate(s.GetComponent<Collider>());
                s.name = "LuzRojaGrua";
                s.transform.SetParent(padre, true);
                s.transform.position = cima + new Vector3(i * 6f, 0f, 0f);
                s.transform.localScale = Vector3.one * 0.5f;
                s.GetComponent<MeshRenderer>().sharedMaterial = rojo;
                s.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }

        private static void Entrada(KitS2c.Obra o, Transform r)
        {
            Transform padre = new GameObject("Entrada").transform;
            padre.SetParent(r, false);
            // Caseta junto a la calle de U5; la pluma no tiene modelo: queda su hueco.
            Colocar(padre, "LO04_Caseta", new Vector3(U5.x + 14f, YPatio, U5.z - 8f), 0f, 0f, 3.2f, false, false);
            Hueco(padre, "PluminaEntrada", new Vector3(U5.x + 14f, YPatio, U5.z));
            // Reja en el perímetro de tierra: norte (con el hueco de U8) y oeste.
            for (float x = X0 + 2f; x < X1 - 2f; x += 4f)
            {
                if (x > U8.x - 8f && x < U8.x + 8f)
                {
                    continue;
                }
                Colocar(padre, "DS01_RejaPuas", new Vector3(x, YPatio, ZNorte - 0.5f), 180f, 4f, 0f, true, false);
            }
            for (float z = ZNorte - 4f; z > U5.z + 12f; z -= 4f)
            {
                Colocar(padre, "DS01_RejaPuas", new Vector3(X0 + 0.5f, YPatio, z), -90f, 4f, 0f, true, false);
            }
        }

        private static void Postes(KitS2c.Obra o, Transform r)
        {
            Transform padre = new GameObject("Postes").transform;
            padre.SetParent(r, false);
            (Vector3 pie, Vector3 haciaCalle)[] ps =
            {
                (new Vector3(167f, YPatio, -87f), new Vector3(-1f, 0f, 1f)), (new Vector3(293f, YPatio, -87f), new Vector3(1f, 0f, 1f)),
                (new Vector3(293f, YPatio, -209f), new Vector3(1f, 0f, -1f)), (new Vector3(167f, YPatio, -209f), new Vector3(-1f, 0f, -1f)),
            };
            List<Vector3> puntas = new List<Vector3>();
            foreach (var p in ps)
            {
                puntas.Add(KitS2c.Poste(o, padre, p.pie, p.haciaCalle, YPatio, false));
            }
            for (int i = 0; i < puntas.Count; i++)
            {
                KitS2c.Cable(o, puntas[i], puntas[(i + 1) % puntas.Count], 1.5f);
            }
        }

        // Montacargas, pallets, barriles y la plaza abierta del patio norte; charcos por todo el patio.
        private static void Patio(KitS2c.Obra o, Transform r)
        {
            Transform padre = new GameObject("Patio").transform;
            padre.SetParent(r, false);
            Vector3 plaza = new Vector3(187f + LargoBloque + PasilloX, YPatio, -92.5f - (FondoBloque + PasilloZ));
            Colocar(padre, "PU05_Montacargas", plaza + new Vector3(-4f, 0f, 2f), 35f, 0f, 2.6f, false, false);
            Colocar(padre, "PU05_Montacargas", new Vector3(175f, YPatio, -197f), -20f, 0f, 2.6f, false, false);
            for (int i = 0; i < 6; i++)
            {
                Colocar(padre, "PU06_Pallets", plaza + new Vector3(o.Rango(-8f, 8f), 0f, o.Rango(-3f, 5f)), o.Rango(0f, 180f), 0f, 1.2f, false, false);
            }
            for (int i = 0; i < 14; i++)
            {
                Colocar(padre, "C16_Tambo", new Vector3(o.Rango(170f, 290f), YPatio, o.Rango(-208f, -200f)), o.Rango(0f, 360f), 0f, 1.0f, false, false);
            }
            for (int i = 0; i < 34; i++)
            {
                Vector3 p = new Vector3(o.Rango(X0 + 10f, MuelleEste - 6f), YPatio, o.Rango(ZNorte - 10f, MuelleSur + 4f));
                if (Bloques().Any(b => Mathf.Abs(p.x - b.Centro.x) < LargoBloque * 0.5f + 1f && Mathf.Abs(p.z - b.Centro.y) < FondoBloque * 0.5f + 1f))
                {
                    continue;
                }
                KitS2c.Charco(o, padre, p, o.Rango(1.6f, 4.5f), o.Rango(1f, 2.6f), o.Rango(0f, 180f));
            }
        }

        private static void CrearMar(Transform r)
        {
            Type t = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("MechaCyberpunk.Mundo.Editor.CreadorMarT1351")).FirstOrDefault(x => x != null);
            MethodInfo m = t?.GetMethod("CrearPlanoMar", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (m == null)
            {
                Hueco(r, "Mar_T1351", new Vector3(565f, YMar, -472.5f));
                return;
            }
            // Mar del este: x 330..800, z -45..-900 (el del sur con x < 330 es del Malecón, T-1354).
            m.Invoke(null, new object[] { r, new Vector3(565f, YMar, -472.5f), new Vector2(470f, 855f), YMar });
        }

        // ---------------------------------------------------------------- Ayudas

        private static GameObject Hueco(Transform padre, string codigo, Vector3 pos)
        {
            huecos.Add(codigo);
            GameObject h = new GameObject("Hueco_" + codigo);
            h.transform.SetParent(padre, false);
            h.transform.position = pos;
            return h;
        }

        private static GameObject Buscar(string codigo)
        {
            if (prefabs.TryGetValue(codigo, out GameObject ya))
            {
                return ya;
            }
            GameObject p = AssetDatabase.FindAssets(codigo + " t:Prefab")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(ruta => { string n = Path.GetFileNameWithoutExtension(ruta); return n == codigo || n.StartsWith(codigo + "_"); })
                .Select(AssetDatabase.LoadAssetAtPath<GameObject>).FirstOrDefault();
            prefabs[codigo] = p;
            return p;
        }

        /// <summary>
        /// Pone la pieza con la base centrada en <paramref name="pie"/>. <paramref name="largo"/> (m) escala para que el lado más largo en
        /// planta mida eso; <paramref name="alto"/> para que mida eso de alto. Con <paramref name="largoEnX"/> el lado largo queda en x.
        /// Sin modelo deja un hueco vacío.
        /// </summary>
        private static GameObject Colocar(Transform padre, string codigo, Vector3 pie, float yaw, float largo, float alto, bool collider, bool largoEnX)
        {
            GameObject p = Buscar(codigo);
            if (p == null)
            {
                return Hueco(padre, codigo, pie);
            }
            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(p, padre);
            go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            go.transform.localScale = Vector3.one;
            Bounds b = Medir(go);
            if (largoEnX && b.size.z > b.size.x)
            {
                yaw += 90f;
            }
            float f = 1f;
            if (largo > 0f)
            {
                f = largo / Mathf.Max(b.size.x, b.size.z);
            }
            else if (alto > 0f)
            {
                f = alto / b.size.y;
            }
            go.transform.localScale = Vector3.one * f;
            go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            b = Medir(go);
            go.transform.position += new Vector3(pie.x - b.center.x, pie.y - b.min.y, pie.z - b.center.z);
            if (!collider)
            {
                foreach (Collider c in go.GetComponentsInChildren<Collider>(true))
                {
                    c.enabled = false;
                }
            }
            return go;
        }

        private static Bounds Medir(GameObject go)
        {
            Renderer[] rs = go.GetComponentsInChildren<Renderer>(true);
            Bounds b = rs.Length > 0 ? rs[0].bounds : new Bounds(go.transform.position, Vector3.one);
            foreach (Renderer r in rs)
            {
                b.Encapsulate(r.bounds);
            }
            return b;
        }

        // Pocos materiales compartidos (SRP Batcher): un clon por color y variación de desgaste.
        private static void Tintar(GameObject cont, Color color, int variante)
        {
            if (cont.name.StartsWith("Hueco_"))
            {
                return;
            }
            foreach (Renderer rend in cont.GetComponentsInChildren<Renderer>())
            {
                Material[] ms = rend.sharedMaterials;
                for (int i = 0; i < ms.Length; i++)
                {
                    Material origen = ms[i];
                    if (origen == null)
                    {
                        continue;
                    }
                    string nombre = "T1357_Cont_" + ColorUtility.ToHtmlStringRGB(color) + "_" + variante + "_" + origen.name;
                    ms[i] = KitS2c.Mat(nombre, () =>
                    {
                        Material m = new Material(origen);
                        m.SetColor("_BaseColor", color * (variante == 0 ? 1f : 0.78f));
                        return m;
                    });
                }
                rend.sharedMaterials = ms;
            }
        }

        // ---------------------------------------------------------------- Fotos

        [MenuItem("Mecha Cyberpunk/Zonas/T-1357 Fotos del Puerto")]
        public static void Fotos()
        {
            ColocarS2.Abrir();
            // K15: pasillo a nivel de piso (calle de x = 160 hacia el sur), contenedores a la izquierda, bodegas a la derecha, grúas y barco al fondo.
            ColocarS2cLook.Foto(new Vector3(160f, 1.7f, -95f), new Vector3(215f, 14f, -262f), "docs/capturas/zonas/puerto-juego-k15.png", 62f);
            ColocarS2.LadoALado("imageassets/referencias/look/K15_Puerto.png", "docs/capturas/zonas/puerto-juego-k15.png", "docs/capturas/zonas/puerto-k15.png");
            // A15: aérea.
            ColocarS2cLook.Foto(new Vector3(105f, 120f, -20f), new Vector3(230f, 0f, -165f), "docs/capturas/zonas/puerto-juego-a15.png", 60f);
            ColocarS2.LadoALado("imageassets/referencias/look/A15_Puerto_Aerea.png", "docs/capturas/zonas/puerto-juego-a15.png", "docs/capturas/zonas/puerto-a15.png");
            EditorSceneManager.OpenScene(ColocarS2.RutaEscena, OpenSceneMode.Single);
        }
    }
}
