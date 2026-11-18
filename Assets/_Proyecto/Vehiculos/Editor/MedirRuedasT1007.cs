using System.IO;
using System.Linq;
using System.Text;
using MechaCyberpunk.Vehiculos.Carro;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Vehiculos.Editor
{
    /// <summary>
    /// T-1007: mide el hueco de cada rueda (el paso de rueda de la carrocería, sin llantas) y deja la llanta visual al tamaño de ese hueco: la llanta llena el
    /// hueco sin salirse ni hundirse y toca el piso. El hueco sale de los vértices de la malla de la carrocería en el marco de la raíz del carro (x derecha, y arriba, z adelante):
    /// - Holgura vertical: del centro de la llanta al techo del paso de rueda (el vértice más bajo sobre la llanta en una franja de <see cref="AnchoFranja"/>).
    /// - Holgura horizontal: del centro de la llanta al vértice más cercano adelante y atrás, a su altura.
    /// El radio del hueco es la menor de las tres (la llanta no puede pasar de ahí) y la llanta mide <see cref="Relleno"/> de ese radio. La llanta se sube para tocar el piso
    /// (su parte baja en y = 0 de la raíz, que es donde las ruedas de la física apoyan) y se alinea con el centro del hueco hacia adelante y atrás.
    /// Carros: V01a, V01b, V05 y la patrulla (llanta RU01, cuatro). Motos: V07, V08 y C19 (llanta RU02); en las motos el hueco es el radio de la llanta horneada que
    /// <see cref="CreadorMotosPiezasT743"/> borra (RadioCorte) y C19, sin llanta horneada, no tiene hueco: su llanta se mide contra su cuerpo.
    /// Batchmode: -executeMethod MechaCyberpunk.Vehiculos.Editor.MedirRuedasT1007.Medir (solo mide) o .Ajustar (mide y ajusta). Escribe Logs/t1007-ruedas.txt.
    /// </summary>
    public static class MedirRuedasT1007
    {
        public const string RutaPatrulla = "Assets/_Proyecto/Trafico/Prefabs/Patrulla.prefab";
        // T-1343: V01a, V01b y V05 ya traen sus llantas de Blender ajustadas por el modelador; no se miden ni se reescalan aquí.
        public static readonly string[] CodigosCarros = { };
        public static readonly string[] NombresRuedas = { "Rueda_DI", "Rueda_DD", "Rueda_TI", "Rueda_TD" };

        /// <summary>Qué parte del hueco llena la llanta (0.93 = la llanta mide el 93 % del radio del hueco, con un poco de luz alrededor).</summary>
        public const float Relleno = 0.9f;
        /// <summary>Margen (fracción del radio del hueco) que acepta la prueba: la llanta no puede quedar más chica ni más grande que esto.</summary>
        public const float Margen = 0.15f;
        /// <summary>Ancho (m) de la franja con la que se busca el techo y los lados del paso de rueda.</summary>
        public const float AnchoFranja = 0.05f;
        private const float MinimoLateral = 0.1f;

        public readonly struct Hueco
        {
            /// <summary>Radio (m) del hueco: la mitad del ancho del paso de rueda a la altura del centro de la llanta (promedio de adelante y atrás).</summary>
            public readonly float Radio;
            /// <summary>Distancia (m) del centro de la llanta al borde del hueco hacia adelante y hacia atrás.</summary>
            public readonly float Adelante;
            public readonly float Atras;
            /// <summary>Distancia (m) del centro de la llanta al techo del paso de rueda (informativo: la llanta asoma por delante de la salpicadera).</summary>
            public readonly float Techo;

            public Hueco(float adelante, float atras, float techo)
            {
                Adelante = adelante;
                Atras = atras;
                Techo = techo;
                Radio = (adelante + atras) * 0.5f;
            }

            /// <summary>Cuánto hay que mover la llanta hacia adelante (m, negativo = atrás) para quedar al centro del hueco.</summary>
            public float DesplazamientoZ => (Adelante - Atras) * 0.5f;
        }

        // Cuadrícula (m) con la que se pinta el costado de la carrocería y alcance (m) del marco que se mira alrededor del centro de la llanta.
        private const float Celda = 0.005f;
        private const float Alcance = 0.7f;
        private const float GradosAdelante = 0f;
        private const float GradosArriba = 90f;
        private const float GradosAtras = 180f;
        public static float DepurarRadio;
        public static string DepurarNombre = "x";

        private static void VolcarImagen(bool[,] p, int n, float radio)
        {
            var tex = new Texture2D(n, n);
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    float dz = (i + 0.5f) * Celda - Alcance;
                    float dy = (j + 0.5f) * Celda - Alcance;
                    bool aro = Mathf.Abs(Mathf.Sqrt(dz * dz + dy * dy) - radio) < Celda;
                    tex.SetPixel(i, j, aro ? Color.red : p[i, j] ? Color.white : Color.black);
                }
            }
            Directory.CreateDirectory("Logs");
            File.WriteAllBytes("Logs/t1007-" + DepurarNombre + ".png", tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        /// <summary>Triángulos de las mallas en el marco de <paramref name="raiz"/>.</summary>
        public static Vector3[][] TriangulosEn(Transform raiz, MeshFilter[] mallas)
        {
            var lista = new System.Collections.Generic.List<Vector3[]>();
            foreach (MeshFilter m in mallas.Where(x => x.sharedMesh != null))
            {
                Vector3[] v = m.sharedMesh.vertices.Select(p => raiz.InverseTransformPoint(m.transform.TransformPoint(p))).ToArray();
                int[] t = m.sharedMesh.triangles;
                for (int i = 0; i + 2 < t.Length; i += 3)
                {
                    lista.Add(new[] { v[t[i]], v[t[i + 1]], v[t[i + 2]] });
                }
            }
            return lista.ToArray();
        }

        /// <summary>
        /// Mide el paso de rueda alrededor de <paramref name="centro"/> (marco de la raíz; z adelante, y arriba, x lateral) del lado <paramref name="signoLateral"/>.
        /// Pinta en una cuadrícula (z, y) todo triángulo de la carrocería cuyo centro está del plano del centro de la llanta hacia afuera (la piel del costado y la salpicadera);
        /// lo que queda sin pintar alrededor del centro es el hueco. Desde el centro se camina adelante, atrás y arriba hasta chocar con lo pintado.
        /// </summary>
        public static Hueco MedirHueco(Vector3[][] triangulos, Vector3 centro, float signoLateral, float radioDepuracion = 0f)
        {
            int n = Mathf.CeilToInt(2f * Alcance / Celda);
            var pintado = new bool[n, n];
            float x0 = Mathf.Abs(centro.x);
            foreach (Vector3[] t in triangulos)
            {
                if ((t[0].x + t[1].x + t[2].x) / 3f * signoLateral >= x0)
                {
                    Pintar(pintado, t, centro, n);
                }
            }
            if (radioDepuracion > 0f)
            {
                VolcarImagen(pintado, n, radioDepuracion);
            }
            return new Hueco(Caminar(pintado, GradosAdelante), Caminar(pintado, GradosAtras), Caminar(pintado, GradosArriba));
        }

        private static float Caminar(bool[,] pintado, float grados)
        {
            float rad = grados * Mathf.Deg2Rad;
            float dz = Mathf.Cos(rad);
            float dy = Mathf.Sin(rad);
            float r = 0f;
            while (r < Alcance - Celda)
            {
                int i = Mathf.FloorToInt((r * dz + Alcance) / Celda);
                int j = Mathf.FloorToInt((r * dy + Alcance) / Celda);
                if (pintado[i, j])
                {
                    break;
                }
                r += Celda;
            }
            return r;
        }

        private static void Pintar(bool[,] pintado, Vector3[] t, Vector3 centro, int n)
        {
            Vector2 a = new Vector2(t[0].z - centro.z, t[0].y - centro.y);
            Vector2 b = new Vector2(t[1].z - centro.z, t[1].y - centro.y);
            Vector2 c = new Vector2(t[2].z - centro.z, t[2].y - centro.y);
            int i0 = Mathf.Max(0, Mathf.FloorToInt((Mathf.Min(a.x, Mathf.Min(b.x, c.x)) + Alcance) / Celda));
            int i1 = Mathf.Min(n - 1, Mathf.FloorToInt((Mathf.Max(a.x, Mathf.Max(b.x, c.x)) + Alcance) / Celda));
            int j0 = Mathf.Max(0, Mathf.FloorToInt((Mathf.Min(a.y, Mathf.Min(b.y, c.y)) + Alcance) / Celda));
            int j1 = Mathf.Min(n - 1, Mathf.FloorToInt((Mathf.Max(a.y, Mathf.Max(b.y, c.y)) + Alcance) / Celda));
            for (int i = i0; i <= i1; i++)
            {
                for (int j = j0; j <= j1; j++)
                {
                    Vector2 p = new Vector2((i + 0.5f) * Celda - Alcance, (j + 0.5f) * Celda - Alcance);
                    float d1 = Lado(p, a, b);
                    float d2 = Lado(p, b, c);
                    float d3 = Lado(p, c, a);
                    bool negativo = d1 < 0f || d2 < 0f || d3 < 0f;
                    bool positivo = d1 > 0f || d2 > 0f || d3 > 0f;
                    if (!(negativo && positivo))
                    {
                        pintado[i, j] = true;
                    }
                }
            }
        }

        private static float Lado(Vector2 p, Vector2 a, Vector2 b) => (p.x - b.x) * (a.y - b.y) - (a.x - b.x) * (p.y - b.y);

        public static string RutaCarro(string codigo) => CreadorCarrosPiezasT742.RutaPrefab(codigo);

        public static string[] RutasCarros() => CodigosCarros.Select(RutaCarro).Append(RutaPatrulla).ToArray();

        [MenuItem("Mecha Cyberpunk/T-1007/Medir ruedas")]
        public static void Medir() => Correr(false);

        [MenuItem("Mecha Cyberpunk/T-1007/Ajustar ruedas al hueco")]
        public static void Ajustar() => Correr(true);

        private static void Correr(bool ajustar)
        {
            var informe = new StringBuilder();
            informe.AppendLine("carro	rueda	radioLlanta	huecoAdelante	huecoAtras	radioHueco	techo	radioNuevo	desplazamientoZ");
            foreach (string ruta in RutasCarros())
            {
                GameObject raiz = PrefabUtility.LoadPrefabContents(ruta);
                try
                {
                    MedirYAjustarCarro(raiz, ajustar, informe);
                    if (ajustar)
                    {
                        PrefabUtility.SaveAsPrefabAsset(raiz, ruta);
                    }
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(raiz);
                }
            }
            if (ajustar)
            {
                AssetDatabase.SaveAssets();
            }
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/t1007-ruedas.txt", informe.ToString());
            Debug.Log("[Vehiculos] T-1007:\n" + informe);
        }

        /// <summary>La malla de la carrocería sin ruedas (Visual_T511/Carroceria) del carro por piezas.</summary>
        public static MeshFilter[] MallasCarroceria(GameObject raiz)
        {
            Transform carroceria = raiz.transform.Find(CreadorCarrosPiezasT742.NombreVisual + "/Carroceria");
            return carroceria == null ? new MeshFilter[0] : carroceria.GetComponentsInChildren<MeshFilter>(true);
        }

        private static void MedirYAjustarCarro(GameObject raiz, bool ajustar, StringBuilder informe)
        {
            Vector3[][] triangulos = TriangulosEn(raiz.transform, MallasCarroceria(raiz));
            RuedasVisualesCarro ruedas = raiz.GetComponent<RuedasVisualesCarro>();
            int n = NombresRuedas.Length;
            var direccion = new Transform[n];
            var modelo = new Transform[n];
            var centro = new Vector3[n];
            var desplazamiento = new float[n];
            var hueco = new Hueco[n];
            float radioInicial = 0f;
            for (int i = 0; i < n; i++)
            {
                direccion[i] = raiz.transform.Find(NombresRuedas[i] + "/Direccion");
                modelo[i] = direccion[i].Find("Giro/Modelo");
                Bounds b = LimitesEn(modelo[i].gameObject, raiz.transform);
                centro[i] = b.center;
                radioInicial += b.extents.y / n;
            }
            // Punto fijo: la llanta (todas del mismo radio) mide Relleno del hueco medio, con su centro a la altura del radio (apoyada en y = 0) y centrada en su hueco.
            float radio = radioInicial;
            int vueltas = ajustar ? IteracionesAjuste : 1;
            for (int v = 0; v < vueltas; v++)
            {
                float suma = 0f;
                for (int i = 0; i < n; i++)
                {
                    Vector3 c = centro[i];
                    c.y = ajustar ? radio : c.y;
                    c.z += desplazamiento[i];
                    hueco[i] = MedirHueco(triangulos, c, Mathf.Sign(c.x), v == 0 && i == 0 && Depurar ? radio : 0f);
                    suma += hueco[i].Radio / n;
                    if (ajustar)
                    {
                        desplazamiento[i] = Mathf.Clamp(desplazamiento[i] + hueco[i].DesplazamientoZ, -DesplazamientoMaximo, DesplazamientoMaximo);
                    }
                }
                if (ajustar)
                {
                    radio = Mathf.Min(suma * Relleno, radioInicial * CrecimientoMaximo);
                }
            }
            for (int i = 0; i < n; i++)
            {
                if (ajustar)
                {
                    ReEscalar(raiz.transform, direccion[i], modelo[i], radio / radioInicial, radio, desplazamiento[i]);
                }
                informe.AppendLine(string.Join("\t", raiz.name, NombresRuedas[i], radioInicial.ToString("F3"), hueco[i].Adelante.ToString("F3"), hueco[i].Atras.ToString("F3"),
                    hueco[i].Radio.ToString("F3"), hueco[i].Techo.ToString("F3"), (ajustar ? radio : radioInicial).ToString("F3"), desplazamiento[i].ToString("F3")));
            }
            if (ajustar && ruedas != null)
            {
                var so = new SerializedObject(ruedas);
                so.FindProperty("radio").floatValue = radio;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private const int IteracionesAjuste = 10;

        // Cambia el tamaño de la llanta, la sube para que su parte baja quede en y = 0 de la raíz del carro y la centra hacia adelante y atrás en el hueco.
        private static void ReEscalar(Transform raiz, Transform direccion, Transform modelo, float factor, float radioNuevo, float desplazamientoZ)
        {
            modelo.localScale *= factor;
            modelo.localPosition *= factor;
            Vector3 enRaiz = raiz.InverseTransformPoint(direccion.position);
            enRaiz.y = radioNuevo;
            enRaiz.z += desplazamientoZ;
            direccion.position = raiz.TransformPoint(enRaiz);
        }

        /// <summary>Pone true para volcar Logs/t1007-&lt;carro&gt;.png (el hueco en blanco y negro con el círculo de la llanta en rojo).</summary>
        public static bool Depurar = true;

        /// <summary>La llanta no crece más de esta fracción de su radio (una llanta enorme dejaría de verse como la del carro).</summary>
        public const float CrecimientoMaximo = 1.25f;
        /// <summary>Lo más (m) que se mueve la llanta hacia adelante o atrás para centrarse en el hueco.</summary>
        public const float DesplazamientoMaximo = 0.08f;

        /// <summary>Límites exactos (por vértices) de las mallas de <paramref name="objeto"/> en el marco de <paramref name="marco"/>.</summary>
        public static Bounds LimitesEn(GameObject objeto, Transform marco)
        {
            Bounds b = default;
            bool primero = true;
            foreach (MeshFilter f in objeto.GetComponentsInChildren<MeshFilter>(true))
            {
                if (f.sharedMesh == null)
                {
                    continue;
                }
                Matrix4x4 aMarco = marco.worldToLocalMatrix * f.transform.localToWorldMatrix;
                foreach (Vector3 v in f.sharedMesh.vertices)
                {
                    Vector3 p = aMarco.MultiplyPoint3x4(v);
                    if (primero)
                    {
                        b = new Bounds(p, Vector3.zero);
                        primero = false;
                    }
                    else
                    {
                        b.Encapsulate(p);
                    }
                }
            }
            return b;
        }
    }
}
