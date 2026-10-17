using System.IO;
using UnityEditor;
using UnityEngine;
using Random = System.Random;

namespace MechaCyberpunk.Mundo.Editor
{
    /// <summary>
    /// T-095: grafitis. Genera por código 8 texturas con alfa (estrella, calavera, firma en zigzag, espiral, ojo, rayo, flecha y cruz con aro)
    /// en Mundo/Texturas/ con su material Grafiti_N (URP/Lit con recorte alfa), y pega unos 45 decales (cuads sin collider) en los muros que dan a la
    /// calle de los edificios de la zona pobre (Manzanas 1 y 4) y en las paredes del callejón de la emboscada. Semilla fija: repetir deja lo mismo.
    /// Corre después de VestirCerrados y se puede repetir (usa CreadorTodo.Crear).
    /// Batchmode: -executeMethod MechaCyberpunk.Mundo.Editor.VestirGrafitis.Crear
    /// </summary>
    public static class VestirGrafitis
    {
        public const string NombreGrupo = "Grafitis";
        private const string RutaTexturas = "Assets/_Proyecto/Mundo/Texturas";
        private const string RutaMateriales = "Assets/_Proyecto/Mundo/Materiales";
        private const string RutaPrefabBarrio = "Assets/_Proyecto/Mundo/Prefabs/Barrio.prefab";
        private const int Lado = 256;
        private const int Semilla = 9595;
        private const float BordeManzana = 36.5f;      // un muro de edificio con borde más allá de esto da a la calle (el interior de la manzana llega a 37)
        private const float Separacion = 0.5f;          // m entre el decal y el muro: las fachadas de T-081 sobresalen hasta 0.45 m
        private const float AlturaMinima = 1.2f;
        private const float AlturaMaxima = 2.8f;
        private const float TamanoMinimo = 1.6f;
        private const float TamanoMaximo = 3.2f;
        private const float AnchoMinimoMuro = 4f;
        private const double ProbabilidadPorMuro = 0.95;
        private const float AlturaBanqueta = 0.2f;

        private static readonly Color[] Colores =
        {
            new Color(1f, 0.2f, 0.7f), new Color(0.1f, 0.9f, 1f), new Color(1f, 0.85f, 0.1f), new Color(1f, 0.5f, 0.1f),
            new Color(0.6f, 1f, 0.2f), new Color(0.75f, 0.4f, 1f), new Color(1f, 0.3f, 0.2f), new Color(0.9f, 0.95f, 1f)
        };

        [MenuItem("Mecha Cyberpunk/Crear T-095")]
        public static void Crear()
        {
            if (!Directory.Exists(RutaTexturas))
            {
                Directory.CreateDirectory(RutaTexturas);
            }
            Material[] materiales = new Material[Colores.Length];
            for (int i = 0; i < materiales.Length; i++)
            {
                Texture2D textura = EscribirTextura(RutaTexturas + "/Grafiti_" + i + ".png", Dibujar(i));
                materiales[i] = CrearMaterial(i, textura);
            }
            AssetDatabase.SaveAssets();

            GameObject raiz = PrefabUtility.LoadPrefabContents(RutaPrefabBarrio);
            Transform viejo = raiz.transform.Find(NombreGrupo);
            if (viejo != null)
            {
                Object.DestroyImmediate(viejo.gameObject);
            }
            Transform grupo = new GameObject(NombreGrupo).transform;
            grupo.SetParent(raiz.transform, false);
            Random azar = new Random(Semilla);
            int cuenta = 0;
            foreach (string nombre in new[] { "Manzana_1", "Manzana_4" })
            {
                Transform manzana = raiz.transform.Find(nombre);
                Transform edificios = manzana.Find("Edificios");
                foreach (Transform edificio in edificios)
                {
                    cuenta += PegarEnMuros(grupo, manzana, edificio, materiales, azar);
                }
            }
            cuenta += PegarEnCallejon(grupo, raiz.transform.Find("Manzana_4"), materiales, azar);

            PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefabBarrio);
            PrefabUtility.UnloadPrefabContents(raiz);
            AssetDatabase.SaveAssets();
            Debug.Log("[Mundo] T-095 creada: " + cuenta + " grafitis.");
        }

        // Muros de un edificio que dan a la calle (su borde cae sobre el límite de la manzana), a veces con un grafiti.
        private static int PegarEnMuros(Transform grupo, Transform manzana, Transform edificio, Material[] materiales, Random azar)
        {
            Transform cuerpo = edificio.Find("Cuerpo");
            if (cuerpo == null)
            {
                return 0;
            }
            Vector3 centro = edificio.localPosition + cuerpo.localPosition;
            Vector3 mitad = cuerpo.localScale / 2f;
            int cuenta = 0;
            foreach (Vector3 normal in new[] { Vector3.right, Vector3.left, Vector3.forward, Vector3.back })
            {
                bool enX = normal.x != 0f;
                float borde = enX ? centro.x + normal.x * mitad.x : centro.z + normal.z * mitad.z;
                float ancho = enX ? cuerpo.localScale.z : cuerpo.localScale.x;
                if (Mathf.Abs(borde) < BordeManzana || ancho < AnchoMinimoMuro || azar.NextDouble() > ProbabilidadPorMuro)
                {
                    continue;
                }
                float tamano = Mathf.Min(TamanoMinimo + (float)azar.NextDouble() * (TamanoMaximo - TamanoMinimo), ancho - 0.6f);
                float altura = AlturaMinima + tamano / 2f + (float)azar.NextDouble() * (AlturaMaxima - AlturaMinima - tamano / 2f + 0.01f);
                altura = Mathf.Min(altura, cuerpo.localScale.y - tamano / 2f - 0.2f);
                float desplazamiento = ((float)azar.NextDouble() - 0.5f) * Mathf.Max(0f, ancho - tamano - 0.6f);
                Vector3 local = enX
                    ? new Vector3(borde + normal.x * Separacion, 0f, centro.z + desplazamiento)
                    : new Vector3(centro.x + desplazamiento, 0f, borde + normal.z * Separacion);
                local.y = AlturaBanqueta + altura;
                Decal(grupo, manzana, local, normal, tamano, materiales[azar.Next(materiales.Length)], "Grafiti_" + (grupo.childCount + 1));
                cuenta++;
            }
            return cuenta;
        }

        // Paredes del callejón de la emboscada (T-088): el muro sur mira al +Z y el norte al -Z, más el fondo.
        private static int PegarEnCallejon(Transform grupo, Transform manzana, Material[] materiales, Random azar)
        {
            int cuenta = 0;
            float[] xs = { 12f, 17f, 23f, 29f };
            for (int i = 0; i < xs.Length; i++)
            {
                Decal(grupo, manzana, new Vector3(xs[i], AlturaBanqueta + 2.4f, CreadorBarrio.CallejonZ0 + Separacion), Vector3.forward, 2.2f, materiales[azar.Next(materiales.Length)], "Grafiti_Callejon_S" + i);
                Decal(grupo, manzana, new Vector3(xs[i] + 2f, AlturaBanqueta + 2.4f, CreadorBarrio.CallejonZ1 - Separacion), Vector3.back, 2.2f, materiales[azar.Next(materiales.Length)], "Grafiti_Callejon_N" + i);
                cuenta += 2;
            }
            return cuenta;
        }

        // Cuad con la textura, con su cara visible hacia 'normal'. Unity dibuja el Quad por su lado -Z, así que +Z mira al muro.
        private static void Decal(Transform grupo, Transform manzana, Vector3 posicionManzana, Vector3 normal, float tamano, Material material, string nombre)
        {
            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = nombre;
            Object.DestroyImmediate(quad.GetComponent<Collider>());
            quad.transform.SetParent(grupo, false);
            quad.transform.position = manzana.TransformPoint(posicionManzana);
            quad.transform.rotation = manzana.rotation * Quaternion.LookRotation(-normal, Vector3.up);
            quad.transform.localScale = new Vector3(tamano, tamano, 1f);
            MeshRenderer renderer = quad.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        // ---------------------------------------------------------------- texturas

        private static Color[] Dibujar(int diseno)
        {
            Color[] p = new Color[Lado * Lado];
            Color c = Color.white;
            switch (diseno)
            {
                case 0: // estrella de cinco puntas con aro y gotas
                    for (int i = 0; i < 5; i++)
                    {
                        Vector2 a = Punta(128, 128, 100, i * 144f), b = Punta(128, 128, 100, (i + 1) * 144f);
                        Linea(p, a, b, 9, c);
                    }
                    Anillo(p, 128, 128, 118, 8, c);
                    Gotas(p, new[] { 60, 128, 196 }, 20, 50, c);
                    break;
                case 1: // calavera
                    Disco(p, 128, 150, 78, c);
                    Rect(p, 88, 60, 80, 55, c);
                    Disco(p, 98, 160, 20, Color.clear);
                    Disco(p, 158, 160, 20, Color.clear);
                    Linea(p, new Vector2(128, 130), new Vector2(118, 105), 6, Color.clear);
                    Linea(p, new Vector2(128, 130), new Vector2(138, 105), 6, Color.clear);
                    for (int x = 100; x <= 156; x += 14)
                    {
                        Linea(p, new Vector2(x, 62), new Vector2(x, 85), 4, Color.clear);
                    }
                    break;
                case 2: // firma en zigzag con subrayado
                    Vector2[] zig = { new Vector2(20, 70), new Vector2(50, 190), new Vector2(85, 70), new Vector2(120, 190), new Vector2(155, 70), new Vector2(190, 190), new Vector2(228, 70) };
                    for (int i = 0; i < zig.Length - 1; i++)
                    {
                        Linea(p, zig[i], zig[i + 1], 12, c);
                    }
                    Linea(p, new Vector2(15, 40), new Vector2(240, 55), 8, c);
                    Disco(p, 215, 215, 12, c);
                    break;
                case 3: // espiral
                    Vector2 anterior = new Vector2(128, 128);
                    for (float t = 0f; t < 6.5f * Mathf.PI; t += 0.2f)
                    {
                        float r = 6f + t * 5.6f;
                        Vector2 siguiente = new Vector2(128 + Mathf.Cos(t) * r, 128 + Mathf.Sin(t) * r);
                        Linea(p, anterior, siguiente, 9, c);
                        anterior = siguiente;
                    }
                    break;
                case 4: // ojo con rayos
                    Anillo(p, 128, 128, 70, 10, c);
                    Disco(p, 128, 128, 30, c);
                    for (int i = 0; i < 8; i++)
                    {
                        Vector2 d = new Vector2(Mathf.Cos(i * Mathf.PI / 4f), Mathf.Sin(i * Mathf.PI / 4f));
                        Linea(p, new Vector2(128, 128) + d * 90f, new Vector2(128, 128) + d * 122f, 7, c);
                    }
                    break;
                case 5: // rayo
                    Vector2[] rayo = { new Vector2(150, 240), new Vector2(90, 135), new Vector2(135, 135), new Vector2(100, 20), new Vector2(180, 150), new Vector2(135, 150), new Vector2(150, 240) };
                    for (int i = 0; i < rayo.Length - 1; i++)
                    {
                        Linea(p, rayo[i], rayo[i + 1], 14, c);
                    }
                    break;
                case 6: // flecha hacia arriba
                    Linea(p, new Vector2(128, 30), new Vector2(128, 210), 20, c);
                    Linea(p, new Vector2(128, 225), new Vector2(60, 140), 16, c);
                    Linea(p, new Vector2(128, 225), new Vector2(196, 140), 16, c);
                    break;
                default: // cruz dentro de un aro
                    Anillo(p, 128, 128, 112, 12, c);
                    Linea(p, new Vector2(128, 45), new Vector2(128, 211), 16, c);
                    Linea(p, new Vector2(60, 150), new Vector2(196, 150), 16, c);
                    break;
            }
            Color color = Colores[diseno];
            for (int i = 0; i < p.Length; i++)
            {
                p[i] = p[i].a > 0.5f ? new Color(color.r, color.g, color.b, 1f) : Color.clear;
            }
            return p;
        }

        private static Vector2 Punta(float cx, float cy, float radio, float grados)
        {
            float rad = (grados + 90f) * Mathf.Deg2Rad;
            return new Vector2(cx + Mathf.Cos(rad) * radio, cy + Mathf.Sin(rad) * radio);
        }

        private static void Disco(Color[] p, float cx, float cy, float r, Color color)
        {
            for (int y = Mathf.Max(0, (int)(cy - r)); y <= Mathf.Min(Lado - 1, (int)(cy + r)); y++)
            {
                for (int x = Mathf.Max(0, (int)(cx - r)); x <= Mathf.Min(Lado - 1, (int)(cx + r)); x++)
                {
                    if ((x - cx) * (x - cx) + (y - cy) * (y - cy) <= r * r)
                    {
                        p[y * Lado + x] = color;
                    }
                }
            }
        }

        private static void Anillo(Color[] p, float cx, float cy, float r, float grosor, Color color)
        {
            for (int y = 0; y < Lado; y++)
            {
                for (int x = 0; x < Lado; x++)
                {
                    float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                    if (Mathf.Abs(d - r) <= grosor / 2f)
                    {
                        p[y * Lado + x] = color;
                    }
                }
            }
        }

        private static void Rect(Color[] p, int x0, int y0, int ancho, int alto, Color color)
        {
            for (int y = y0; y < y0 + alto && y < Lado; y++)
            {
                for (int x = x0; x < x0 + ancho && x < Lado; x++)
                {
                    p[y * Lado + x] = color;
                }
            }
        }

        private static void Linea(Color[] p, Vector2 a, Vector2 b, float grosor, Color color)
        {
            int pasos = Mathf.CeilToInt(Vector2.Distance(a, b));
            for (int i = 0; i <= pasos; i++)
            {
                Vector2 punto = Vector2.Lerp(a, b, pasos == 0 ? 0f : i / (float)pasos);
                Disco(p, punto.x, punto.y, grosor / 2f, color);
            }
        }

        private static void Gotas(Color[] p, int[] xs, int yMin, int yMax, Color color)
        {
            foreach (int x in xs)
            {
                Linea(p, new Vector2(x, yMax), new Vector2(x, yMin), 5, color);
                Disco(p, x, yMin, 6, color);
            }
        }

        private static Texture2D EscribirTextura(string ruta, Color[] pixeles)
        {
            Texture2D textura = new Texture2D(Lado, Lado, TextureFormat.RGBA32, false);
            textura.SetPixels(pixeles);
            byte[] bytes = textura.EncodeToPNG();
            Object.DestroyImmediate(textura);
            if (!File.Exists(ruta) || !AreIguales(File.ReadAllBytes(ruta), bytes))
            {
                File.WriteAllBytes(ruta, bytes);
            }
            AssetDatabase.ImportAsset(ruta, ImportAssetOptions.ForceSynchronousImport);
            TextureImporter importador = (TextureImporter)AssetImporter.GetAtPath(ruta);
            if (importador.textureCompression != TextureImporterCompression.Uncompressed || !importador.alphaIsTransparency)
            {
                importador.textureType = TextureImporterType.Default;
                importador.textureCompression = TextureImporterCompression.Uncompressed;
                importador.alphaIsTransparency = true;
                importador.mipmapEnabled = true;
                importador.wrapMode = TextureWrapMode.Clamp;
                importador.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(ruta);
        }

        private static bool AreIguales(byte[] a, byte[] b)
        {
            if (a.Length != b.Length)
            {
                return false;
            }
            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i])
                {
                    return false;
                }
            }
            return true;
        }

        private static Material CrearMaterial(int diseno, Texture2D textura)
        {
            string ruta = RutaMateriales + "/Grafiti_" + diseno + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, ruta);
            }
            material.SetTexture("_BaseMap", textura);
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Smoothness", 0.15f);
            material.SetFloat("_AlphaClip", 1f);
            material.SetFloat("_Cutoff", 0.5f);
            material.EnableKeyword("_ALPHATEST_ON");
            material.renderQueue = 2450;
            material.EnableKeyword("_EMISSION");
            material.SetTexture("_EmissionMap", textura);
            material.SetColor("_EmissionColor", new Color(0.35f, 0.35f, 0.35f));
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            EditorUtility.SetDirty(material);
            return material;
        }
    }
}
