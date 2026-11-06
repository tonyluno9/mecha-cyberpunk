using System.IO;
using MechaCyberpunk.Personaje;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-808: los huecos de bala. Si están imageassets/vfx/D01_HuecoConcreto.png y D02_HuecoMetal.png (fondo blanco), los pasa a
    /// Assets/Arte/VFX/Huecos/ a 256 px (el 70 % del centro) quitándoles el blanco (alfa = lo que no es blanco, con el color "despegado" del blanco y un borde redondo
    /// que se desvanece); si no están y tampoco hay copia, hace unos de respaldo en código. Luego arma los materiales HuecoBala_Concreto y
    /// HuecoBala_Metal, la ConfigHuecosBala de Resources y deja invisible Greybox_Marca (el cubito de PistolaAPie). Idempotente: solo vuelve a
    /// procesar una imagen si la de imageassets es más nueva.
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ImportarS2nVfx.Importar
    /// </summary>
    public static class ImportarS2nVfx
    {
        public const string CarpetaOrigen = "imageassets/vfx/";
        public const string CarpetaDestino = "Assets/Arte/VFX/Huecos/";
        public const string RutaConfig = "Assets/_Proyecto/Personaje/Datos/Resources/ConfigHuecosBala.asset";
        public const string RutaMaterialConcreto = "Assets/_Proyecto/Personaje/Materiales/HuecoBala_Concreto.mat";
        public const string RutaMaterialMetal = "Assets/_Proyecto/Personaje/Materiales/HuecoBala_Metal.mat";
        public const string RutaMarca = "Assets/_Proyecto/Personaje/Materiales/Greybox_Marca.mat";
        public const string ShaderHueco = "MechaCyberpunk/Personaje/HuecoBala";
        public const string ShaderInvisible = "MechaCyberpunk/Personaje/Invisible";
        public static readonly string[] Archivos = { "D01_HuecoConcreto.png", "D02_HuecoMetal.png" };
        private const int Lado = 256;
        private const float GananciaAlfa = 2.2f;
        private const float BordeInterior = 0.40f;
        private const float BordeExterior = 0.49f;
        // Las imágenes traen el hueco al centro con mucho blanco alrededor: se toma el 70 % del medio.
        private const float Recorte = 0.15f;

        [MenuItem("Mecha Cyberpunk/S2n/Huecos de bala (D01-D02, T-808)")]
        public static void Importar()
        {
            Directory.CreateDirectory(CarpetaDestino);
            for (int i = 0; i < Archivos.Length; i++)
            {
                string origen = CarpetaOrigen + Archivos[i];
                string destino = CarpetaDestino + Archivos[i];
                if (File.Exists(origen))
                {
                    if (!File.Exists(destino) || File.GetLastWriteTimeUtc(origen) > File.GetLastWriteTimeUtc(destino))
                    {
                        File.WriteAllBytes(destino, QuitarBlanco(File.ReadAllBytes(origen)).EncodeToPNG());
                    }
                }
                else if (!File.Exists(destino))
                {
                    File.WriteAllBytes(destino, Respaldo(i == 1).EncodeToPNG());
                    Debug.LogWarning("[Arte] Falta " + origen + ": hueco de respaldo hecho en código.");
                }
            }
            AssetDatabase.Refresh();
            foreach (string archivo in Archivos)
            {
                Configurar(CarpetaDestino + archivo);
            }

            Material concreto = CrearMaterial(RutaMaterialConcreto, CarpetaDestino + Archivos[0]);
            Material metal = CrearMaterial(RutaMaterialMetal, CarpetaDestino + Archivos[1]);
            ConfigHuecosBala config = AssetDatabase.LoadAssetAtPath<ConfigHuecosBala>(RutaConfig);
            if (config == null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(RutaConfig));
                config = ScriptableObject.CreateInstance<ConfigHuecosBala>();
                AssetDatabase.CreateAsset(config, RutaConfig);
            }
            SerializedObject so = new SerializedObject(config);
            so.FindProperty("materialConcreto").objectReferenceValue = concreto;
            so.FindProperty("materialMetal").objectReferenceValue = metal;
            so.ApplyModifiedPropertiesWithoutUndo();

            Material marca = AssetDatabase.LoadAssetAtPath<Material>(RutaMarca);
            Shader invisible = Shader.Find(ShaderInvisible);
            if (marca != null && invisible != null && marca.shader != invisible)
            {
                marca.shader = invisible;
                EditorUtility.SetDirty(marca);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[Arte] Huecos de bala listos en " + CarpetaDestino + " y " + RutaConfig);
        }

        // Baja a 256 px promediando y quita el fondo blanco: c = a·x + (1 − a)·blanco ⇒ x = (c − (1 − a)) / a.
        private static Texture2D QuitarBlanco(byte[] png)
        {
            Texture2D fuente = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            fuente.LoadImage(png);
            Texture2D salida = new Texture2D(Lado, Lado, TextureFormat.RGBA32, true);
            int paso = Mathf.Max(1, Mathf.RoundToInt(fuente.width * (1f - 2f * Recorte) / Lado));
            Color[] pixeles = new Color[Lado * Lado];
            for (int y = 0; y < Lado; y++)
            {
                for (int x = 0; x < Lado; x++)
                {
                    Color suma = Color.clear;
                    int cuantos = 0;
                    int fx = Mathf.FloorToInt((Recorte + (float)x / Lado * (1f - 2f * Recorte)) * fuente.width);
                    int fy = Mathf.FloorToInt((Recorte + (float)y / Lado * (1f - 2f * Recorte)) * fuente.height);
                    for (int dy = 0; dy < paso; dy++)
                    {
                        for (int dx = 0; dx < paso; dx++)
                        {
                            suma += fuente.GetPixel(Mathf.Min(fx + dx, fuente.width - 1), Mathf.Min(fy + dy, fuente.height - 1));
                            cuantos++;
                        }
                    }
                    Color c = suma / cuantos;
                    float blanco = Mathf.Min(c.r, Mathf.Min(c.g, c.b));
                    float alfa = Mathf.Clamp01((1f - blanco) * GananciaAlfa) * Redondo(x, y);
                    Color color = alfa > 0.001f
                        ? new Color(Despegar(c.r, alfa), Despegar(c.g, alfa), Despegar(c.b, alfa), alfa)
                        : new Color(0f, 0f, 0f, 0f);
                    pixeles[y * Lado + x] = color;
                }
            }
            salida.SetPixels(pixeles);
            salida.Apply();
            Object.DestroyImmediate(fuente);
            return salida;
        }

        private static float Despegar(float canal, float alfa) => Mathf.Clamp01((canal - (1f - alfa)) / alfa);

        private static float Redondo(int x, int y)
        {
            float dx = (x + 0.5f) / Lado - 0.5f;
            float dy = (y + 0.5f) / Lado - 0.5f;
            float d = Mathf.Sqrt(dx * dx + dy * dy);
            return 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(BordeInterior, BordeExterior, d));
        }

        // Respaldo sin imagen: agujero negro, anillo roto (gris en concreto, plateado en metal) y polvo alrededor.
        private static Texture2D Respaldo(bool metal)
        {
            Texture2D t = new Texture2D(Lado, Lado, TextureFormat.RGBA32, true);
            System.Random azar = new System.Random(metal ? 2 : 1);
            Color anillo = metal ? new Color(0.62f, 0.64f, 0.68f) : new Color(0.55f, 0.52f, 0.48f);
            Color[] pixeles = new Color[Lado * Lado];
            for (int y = 0; y < Lado; y++)
            {
                for (int x = 0; x < Lado; x++)
                {
                    float dx = (x + 0.5f) / Lado - 0.5f;
                    float dy = (y + 0.5f) / Lado - 0.5f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float ruido = (float)azar.NextDouble();
                    Color c;
                    if (d < 0.06f)
                    {
                        c = new Color(0.03f, 0.03f, 0.03f, 1f);
                    }
                    else if (d < 0.16f)
                    {
                        float hondo = Mathf.InverseLerp(0.06f, 0.16f, d);
                        c = Color.Lerp(new Color(0.1f, 0.1f, 0.1f), anillo, hondo) * (0.85f + 0.15f * ruido);
                        c.a = 1f;
                    }
                    else
                    {
                        float polvo = Mathf.Clamp01(1f - Mathf.InverseLerp(0.16f, 0.4f, d)) * (ruido > 0.7f ? 0.8f : 0.25f);
                        c = new Color(0.2f, 0.19f, 0.18f, polvo);
                    }
                    pixeles[y * Lado + x] = c;
                }
            }
            t.SetPixels(pixeles);
            t.Apply();
            return t;
        }

        private static void Configurar(string ruta)
        {
            TextureImporter importador = AssetImporter.GetAtPath(ruta) as TextureImporter;
            if (importador == null)
            {
                return;
            }
            bool cambio = importador.textureType != TextureImporterType.Default || !importador.alphaIsTransparency || !importador.mipmapEnabled
                || importador.wrapMode != TextureWrapMode.Clamp || importador.maxTextureSize != Lado;
            if (!cambio)
            {
                return;
            }
            importador.textureType = TextureImporterType.Default;
            importador.alphaIsTransparency = true;
            importador.mipmapEnabled = true;
            importador.wrapMode = TextureWrapMode.Clamp;
            importador.maxTextureSize = Lado;
            importador.SaveAndReimport();
        }

        private static Material CrearMaterial(string ruta, string rutaTextura)
        {
            Shader shader = Shader.Find(ShaderHueco);
            Material m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (m == null)
            {
                m = new Material(shader);
                AssetDatabase.CreateAsset(m, ruta);
            }
            if (m.shader != shader)
            {
                m.shader = shader;
            }
            m.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(rutaTextura);
            m.color = Color.white;
            EditorUtility.SetDirty(m);
            return m;
        }
    }
}
