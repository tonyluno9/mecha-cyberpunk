using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-881 (playtest 7-nov, sesión 2): en los postes solo brilla el cristal de la lámpara. Los modelos C07 y N09 son una sola malla con un
    /// solo material (el cristal no es pieza aparte: queda anotado para Tony en el reporte, para separarlo en Blender). Mientras, se lee la
    /// malla: los triángulos de la cara de abajo de cada cabeza de lámpara (los de <see cref="PostesS2t.Cabezas"/>) son el cristal, y se
    /// pintan en una máscara de emisión del tamaño de la textura del modelo. El material del modelo brilla solo ahí. Se quita la "Mica" (el
    /// óvalo de primitivo que ponía KitS2c bajo la cabeza y que se veía como un hueco). Idempotente.
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ColocarS2oPostes.Colocar
    /// </summary>
    public static class ColocarS2oPostes
    {
        public const string Carpeta = "Assets/Arte/Mundo/S2o/Postes";
        public const int Lado = 1024;
        /// <summary>Del cristal: qué tanto mira al piso su cara (componente y de la normal) y qué tan lejos de la cabeza puede estar (m).</summary>
        public const float MiraAbajo = -0.35f;
        public const float RadioCristal = 0.55f;
        public const float AltoCristal = 0.4f;
        public static readonly Color BrilloSodio = new Color(1f, 0.55f, 0.18f) * 1.6f;
        public static readonly Color BrilloN09 = new Color(0.72f, 0.93f, 1f) * 1.3f;

        /// <summary>El cristal también se ve en la textura del modelo: en el C07 es el panel naranja (saturado) y en el N09 la barra blanca.</summary>
        public static bool CristalC07(Color c) => c.maxColorComponent > 0.6f && (c.maxColorComponent - Mathf.Min(c.r, c.g, c.b)) / c.maxColorComponent > 0.45f;
        public static bool CristalN09(Color c) => c.grayscale > 0.68f;

        public static readonly (string modelo, string material, string textura, Color brillo, System.Func<Color, bool> esCristal)[] Modelos =
        {
            ("Assets/Arte/Calle/C07_PosteLuz/C07_PosteLuz.fbx", "Assets/Arte/Calle/C07_PosteLuz/C07_PosteLuz.mat", "Assets/Arte/Calle/C07_PosteLuz/C07_PosteLuz_basecolor.jpg", BrilloSodio, CristalC07),
            ("Assets/Arte/Calle/N09/N09.fbx", "Assets/Arte/Calle/N09/N09.mat", "Assets/Arte/Calle/N09/N09_basecolor.jpg", BrilloN09, CristalN09),
        };

        [MenuItem("Mecha Cyberpunk/S2o/2 Postes: solo brilla el cristal")]
        public static void Colocar()
        {
            ColocarS2.Informe.Clear();
            Directory.CreateDirectory(Carpeta);
            foreach ((string modelo, string material, string textura, Color brillo, System.Func<Color, bool> esCristal) in Modelos)
            {
                // La malla con el giro de su FBX (la raíz de Tripo viene girada 90° en X): "abajo" es el abajo del poste parado.
                GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>(modelo);
                MeshFilter filtro = fbx != null ? fbx.GetComponentsInChildren<MeshFilter>(true).OrderByDescending(m => m.sharedMesh.vertexCount).FirstOrDefault() : null;
                Mesh malla = filtro != null ? filtro.sharedMesh : null;
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(material);
                if (malla == null || mat == null)
                {
                    ColocarS2.Informe.AppendLine("FALTA " + modelo + " o " + material);
                    continue;
                }
                Texture2D mascara = Mascara(malla, Parado(filtro.transform), textura, esCristal, out int triangulos);
                string ruta = Carpeta + "/" + Path.GetFileNameWithoutExtension(modelo) + "_Cristal.png";
                File.WriteAllBytes(ruta, mascara.EncodeToPNG());
                Object.DestroyImmediate(mascara);
                AssetDatabase.ImportAsset(ruta);
                TextureImporter imp = (TextureImporter)AssetImporter.GetAtPath(ruta);
                imp.sRGBTexture = true;
                imp.mipmapEnabled = true;
                imp.SaveAndReimport();
                // Con la bandera de GI en None, URP vuelve a calcular el keyword al reimportar y lo pierde (ver CreadorAmbiente): tiempo real.
                mat.SetTexture("_EmissionMap", AssetDatabase.LoadAssetAtPath<Texture2D>(ruta));
                mat.SetColor("_EmissionColor", brillo);
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                mat.EnableKeyword("_EMISSION");
                EditorUtility.SetDirty(mat);
                ColocarS2.Informe.AppendLine(Path.GetFileName(modelo) + ": cristal de " + triangulos + " triángulos en " + ruta);
            }
            AssetDatabase.SaveAssets();
            ColocarS2.Abrir();
            int micas = 0;
            foreach (Transform t in ColocarS2.Todos().Where(t => t.name == "Mica" && t.gameObject.activeSelf).ToArray())
            {
                t.gameObject.SetActive(false);
                micas++;
            }
            ColocarS2.Informe.AppendLine("Micas de primitivo apagadas: " + micas);
            ColocarS2.Guardar("s2o-postes");
        }

        /// <summary>Los triángulos del cristal: miran al piso y están en la cabeza de una lámpara (coordenadas de la malla).</summary>
        public static List<int> Cristal(Mesh malla, Matrix4x4 parado)
        {
            List<Vector3> cabezas = PostesS2t.Cabezas(malla, parado);
            Vector3[] v = malla.vertices.Select(x => parado.MultiplyPoint3x4(x)).ToArray();
            int[] t = malla.triangles;
            List<int> salida = new List<int>();
            for (int i = 0; i < t.Length; i += 3)
            {
                Vector3 a = v[t[i]], b = v[t[i + 1]], c = v[t[i + 2]];
                Vector3 n = Vector3.Cross(b - a, c - a);
                if (n.sqrMagnitude < 1e-12f)
                {
                    continue;
                }
                n.Normalize();
                Vector3 centro = (a + b + c) / 3f;
                bool enCabeza = cabezas.Any(h => new Vector2(centro.x - h.x, centro.z - h.z).magnitude < RadioCristal && centro.y - h.y < AltoCristal && centro.y - h.y > -0.1f);
                if (enCabeza && n.y < MiraAbajo)
                {
                    salida.Add(i);
                }
            }
            return salida;
        }

        /// <summary>De la malla al poste parado con su raíz en el origen (la matriz de la malla dentro de su FBX).</summary>
        public static Matrix4x4 Parado(Transform malla)
        {
            Matrix4x4 m = Matrix4x4.identity;
            for (Transform t = malla; t != null; t = t.parent)
            {
                m = (t.parent == null ? Matrix4x4.Rotate(t.localRotation) * Matrix4x4.Scale(t.localScale) : Matrix4x4.TRS(t.localPosition, t.localRotation, t.localScale)) * m;
            }
            return m;
        }

        private static Texture2D Mascara(Mesh malla, Matrix4x4 parado, string textura, System.Func<Color, bool> esCristal, out int triangulos)
        {
            Color[] px = new Color[Lado * Lado];
            Vector2[] uv = malla.uv;
            int[] t = malla.triangles;
            List<int> cristal = Cristal(malla, parado);
            triangulos = cristal.Count;
            foreach (int i in cristal)
            {
                Pintar(px, uv[t[i]] * Lado, uv[t[i + 1]] * Lado, uv[t[i + 2]] * Lado);
            }
            // Dos pasadas de engorde: que el borde del cristal no deje una línea oscura con el filtrado.
            for (int k = 0; k < 2; k++)
            {
                Color[] copia = (Color[])px.Clone();
                for (int y = 1; y < Lado - 1; y++)
                {
                    for (int x = 1; x < Lado - 1; x++)
                    {
                        if (copia[y * Lado + x].r > 0f)
                        {
                            continue;
                        }
                        if (copia[y * Lado + x - 1].r > 0f || copia[y * Lado + x + 1].r > 0f || copia[(y - 1) * Lado + x].r > 0f || copia[(y + 1) * Lado + x].r > 0f)
                        {
                            px[y * Lado + x] = Color.white;
                        }
                    }
                }
            }
            // Solo donde la textura del modelo también es cristal (en la cabeza hay metal que mira abajo).
            Texture2D color = new Texture2D(2, 2);
            color.LoadImage(File.ReadAllBytes(textura));
            for (int y = 0; y < Lado; y++)
            {
                for (int x = 0; x < Lado; x++)
                {
                    int i = y * Lado + x;
                    if (px[i].r > 0f && !esCristal(color.GetPixelBilinear((x + 0.5f) / Lado, (y + 0.5f) / Lado)))
                    {
                        px[i] = Color.black;
                    }
                    px[i].a = 1f;
                }
            }
            Object.DestroyImmediate(color);
            Texture2D tex = new Texture2D(Lado, Lado, TextureFormat.RGBA32, false);
            tex.SetPixels(px);
            tex.Apply();
            return tex;
        }

        private static void Pintar(Color[] px, Vector2 a, Vector2 b, Vector2 c)
        {
            int x0 = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(a.x, b.x, c.x)), 0, Lado - 1);
            int x1 = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(a.x, b.x, c.x)), 0, Lado - 1);
            int y0 = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(a.y, b.y, c.y)), 0, Lado - 1);
            int y1 = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(a.y, b.y, c.y)), 0, Lado - 1);
            float area = (b.x - a.x) * (c.y - a.y) - (c.x - a.x) * (b.y - a.y);
            if (Mathf.Abs(area) < 1e-6f)
            {
                return;
            }
            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                    float w0 = ((b.x - p.x) * (c.y - p.y) - (c.x - p.x) * (b.y - p.y)) / area;
                    float w1 = ((c.x - p.x) * (a.y - p.y) - (a.x - p.x) * (c.y - p.y)) / area;
                    float w2 = 1f - w0 - w1;
                    if (w0 >= -0.01f && w1 >= -0.01f && w2 >= -0.01f)
                    {
                        px[y * Lado + x] = Color.white;
                    }
                }
            }
        }

        /// <summary>De cerca y de noche, un C07 y un N09 (docs/capturas/s2o-poste-*.png).</summary>
        public static void Fotos(string cuando)
        {
            Transform[] postes = PostesS2t.Postes();
            List<(string nombre, Transform poste)> tomas = new List<(string, Transform)>
            {
                ("calle", ColocarS2.Buscar(ColocarS2cCalle.Raiz + "/Postes/Poste_2")),
                ("farola", postes.FirstOrDefault(x => x.name.StartsWith("Farola_"))),
                ("n09", postes.FirstOrDefault(x => x.name.StartsWith("N09_"))),
            };
            foreach ((string nombre, Transform p) in tomas)
            {
                MeshFilter mf = p != null ? PostesS2t.ModeloVisible(p) : null;
                if (mf == null)
                {
                    continue;
                }
                Vector3 cabeza = PostesS2t.Cabezas(mf.sharedMesh, mf.transform.localToWorldMatrix).First();
                Vector3 haciaFuera = new Vector3(cabeza.x - p.position.x, 0f, cabeza.z - p.position.z).normalized;
                Vector3 lado = Vector3.Cross(Vector3.up, haciaFuera);
                // Desde la banqueta, a la altura de los ojos, unos metros adelante: se ve la cara de abajo de la lámpara.
                ColocarS2cLook.Foto(new Vector3(cabeza.x, p.position.y + 1.7f, cabeza.z) + haciaFuera * 3.5f + lado * 2.5f, cabeza, "docs/capturas/s2o-poste-" + nombre + "-" + cuando + ".png", 45f);
            }
        }
    }
}
