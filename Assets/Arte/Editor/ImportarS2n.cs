using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// Importador de los assets nuevos de la sesión 2 (noche 6-nov). Una entrada por asset: archivo de imageassets/, destino en Assets/ y qué
    /// hacer con él. Si el archivo está, se copia (solo si cambió) y se le ponen sus ajustes de importación; si no está y el destino tampoco,
    /// se hace un respaldo en Unity (textura procedural en PNG). Después se arman los materiales que usan esos archivos. Correrlo dos veces no
    /// cambia nada. Otras tareas agregan sus entradas a <see cref="Entradas"/> (D01/D02 huecos de bala, H01-H04 hologramas, V09-V11 carros).
    /// Ver docs/meter-assets-06nov.md.
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ImportarS2n.MeterTodo
    /// </summary>
    public static class ImportarS2n
    {
        public enum Accion
        {
            /// <summary>Textura de pintura sobre el asfalto: fondo negro = transparente (alfa desde el gris), repetida, con mipmaps.</summary>
            MascaraCalle,
            /// <summary>T-810: carro de Tripo en .zip (fbx y texturas). Lo desempaca y arma <see cref="ImportarS2nCarros"/>; sin zip no hace nada.</summary>
            CarroTripo,
        }

        public readonly struct Entrada
        {
            public readonly string Origen;
            public readonly string Destino;
            public readonly Accion Accion;
            /// <summary>Hace el respaldo en Unity (bytes PNG) cuando no llegó el archivo; null si no hay respaldo.</summary>
            public readonly Func<byte[]> Respaldo;

            public Entrada(string origen, string destino, Accion accion, Func<byte[]> respaldo)
            {
                Origen = origen;
                Destino = destino;
                Accion = accion;
                Respaldo = respaldo;
            }
        }

        public const string CarpetaLineas = "Assets/Arte/Calle/Lineas/";
        public const string TexturaAmarilla = CarpetaLineas + "M01_LineasAmarillas.png";
        public const string TexturaCebra = CarpetaLineas + "M02_CrucePeatonal.png";
        /// <summary>Raya blanca de carril: no hay archivo de Tony; siempre es la hecha en Unity.</summary>
        public const string TexturaBlanca = CarpetaLineas + "M03_LineaBlanca_Unity.png";
        public const string MaterialAmarilla = CarpetaLineas + "M01_LineasAmarillas.mat";
        public const string MaterialCebra = CarpetaLineas + "M02_CrucePeatonal.mat";
        public const string MaterialBlanca = CarpetaLineas + "M03_LineaBlanca.mat";
        /// <summary>Repeticiones de M01 a lo largo de cada tramo amarillo de <see cref="ColocarS2nLineas"/> (menos objetos).</summary>
        public const int RepeticionesAmarilla = 4;

        public static readonly Entrada[] Entradas =
        {
            new Entrada("imageassets/calle/M01_LineasAmarillas.png", TexturaAmarilla, Accion.MascaraCalle, () => Rayas(256, 512, new Color(0.95f, 0.78f, 0.12f), new[] { (0.385f, 0.47f), (0.53f, 0.615f) }, false, 101)),
            new Entrada("imageassets/calle/M02_CrucePeatonal.png", TexturaCebra, Accion.MascaraCalle, () => Rayas(512, 256, Color.white, Enumerable.Range(0, 8).Select(i => (i / 8f + 0.015f, i / 8f + 0.085f)).ToArray(), true, 202)),
            new Entrada(null, TexturaBlanca, Accion.MascaraCalle, () => Rayas(64, 256, new Color(0.92f, 0.92f, 0.9f), new[] { (0.3f, 0.7f) }, false, 303)),
            // T-810: carros nuevos del tráfico (largo y giro en ImportarS2nCarros.Medidas).
            new Entrada("imageassets/modelos3d/V09.zip", ImportarS2nCarros.CarpetaVehiculos + "V09/V09.fbx", Accion.CarroTripo, null),
            new Entrada("imageassets/modelos3d/V10.zip", ImportarS2nCarros.CarpetaVehiculos + "V10/V10.fbx", Accion.CarroTripo, null),
            new Entrada("imageassets/modelos3d/V11_Van.zip", ImportarS2nCarros.CarpetaVehiculos + "V11/V11.fbx", Accion.CarroTripo, null),
        };

        public static readonly StringBuilder Informe = new StringBuilder();

        [MenuItem("Mecha Cyberpunk/S2n/Meter assets nuevos")]
        public static void MeterTodo()
        {
            Informe.Clear();
            foreach (Entrada e in Entradas)
            {
                Meter(e);
            }
            AssetDatabase.Refresh();
            foreach (Entrada e in Entradas)
            {
                Ajustes(e);
            }
            Materiales();
            ImportarS2nCarros.Armar(Entradas.Where(e => e.Accion == Accion.CarroTripo).ToArray());
            AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/s2n-importar.txt", Informe.ToString());
            Debug.Log("[Arte] ImportarS2n:\n" + Informe);
        }

        /// <summary>Copia el archivo si llegó y es distinto del que ya está; si no llegó y no hay nada, hace el respaldo.</summary>
        private static void Meter(Entrada e)
        {
            if (e.Accion == Accion.CarroTripo)
            {
                ImportarS2nCarros.Meter(e);
                return;
            }
            Directory.CreateDirectory(Path.GetDirectoryName(e.Destino));
            if (e.Origen != null && File.Exists(e.Origen))
            {
                byte[] nuevo = File.ReadAllBytes(e.Origen);
                if (File.Exists(e.Destino) && File.ReadAllBytes(e.Destino).SequenceEqual(nuevo))
                {
                    Informe.AppendLine("igual: " + e.Destino);
                    return;
                }
                File.WriteAllBytes(e.Destino, nuevo);
                Informe.AppendLine("copiado: " + e.Origen + " -> " + e.Destino);
                return;
            }
            if (File.Exists(e.Destino))
            {
                Informe.AppendLine("sin archivo, se queda el que hay: " + e.Destino);
                return;
            }
            if (e.Respaldo != null)
            {
                File.WriteAllBytes(e.Destino, e.Respaldo());
                Informe.AppendLine("respaldo hecho en Unity: " + e.Destino);
            }
            else
            {
                Informe.AppendLine("FALTA " + e.Origen + " (sin respaldo)");
            }
        }

        private static void Ajustes(Entrada e)
        {
            if (!(AssetImporter.GetAtPath(e.Destino) is TextureImporter ti))
            {
                return;
            }
            if (e.Accion == Accion.MascaraCalle)
            {
                bool cambia = ti.textureType != TextureImporterType.Default || ti.alphaSource != TextureImporterAlphaSource.FromGrayScale || !ti.alphaIsTransparency
                    || ti.wrapMode != TextureWrapMode.Repeat || !ti.mipmapEnabled || ti.anisoLevel != 8 || !ti.sRGBTexture;
                if (cambia)
                {
                    ti.textureType = TextureImporterType.Default;
                    ti.sRGBTexture = true;
                    ti.alphaSource = TextureImporterAlphaSource.FromGrayScale;
                    ti.alphaIsTransparency = true;
                    ti.wrapMode = TextureWrapMode.Repeat;
                    ti.mipmapEnabled = true;
                    ti.anisoLevel = 8;
                    ti.SaveAndReimport();
                    Informe.AppendLine("ajustes de importación: " + e.Destino);
                }
            }
        }

        // ---------------------------------------------------------------- Materiales

        private static void Materiales()
        {
            Pintura(MaterialAmarilla, TexturaAmarilla, new Vector2(1f, RepeticionesAmarilla));
            // Dos repeticiones a lo ancho: 16 franjas en el ancho de la calle.
            Pintura(MaterialCebra, TexturaCebra, new Vector2(2f, 1f));
            Pintura(MaterialBlanca, TexturaBlanca, Vector2.one);
        }

        /// <summary>Pintura sobre el asfalto: URP/Lit transparente (la luz de la calle la ilumina), sin escribir profundidad.</summary>
        public static Material Pintura(string ruta, string textura, Vector2 repeticion)
        {
            Material m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            bool nuevo = m == null;
            if (nuevo)
            {
                m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            }
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(textura);
            Fijar(m, "_BaseMap", tex);
            Fijar(m, "_MainTex", tex);
            if (m.GetTextureScale("_BaseMap") != repeticion)
            {
                m.SetTextureScale("_BaseMap", repeticion);
                m.SetTextureScale("_MainTex", repeticion);
                EditorUtility.SetDirty(m);
            }
            Fijar(m, "_BaseColor", Color.white);
            Fijar(m, "_Surface", 1f);
            Fijar(m, "_Blend", 0f);
            // Sin "conservar especular": con él, URP premultiplica y suma el brillo del quad entero aunque el alfa sea 0 (se veía una banda
            // clara alrededor de las rayas). Así el negro de la máscara no pinta nada.
            Fijar(m, "_BlendModePreserveSpecular", 0f);
            // Cola Transparent - 50 (URP la saca de _QueueOffset al validar el material).
            Fijar(m, "_QueueOffset", -50f);
            Fijar(m, "_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            Fijar(m, "_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            Fijar(m, "_SrcBlendAlpha", (float)UnityEngine.Rendering.BlendMode.One);
            Fijar(m, "_DstBlendAlpha", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            Fijar(m, "_ZWrite", 0f);
            Fijar(m, "_Smoothness", 0.25f);
            Fijar(m, "_Metallic", 0f);
            Fijar(m, "_ReceiveShadows", 1f);
            if (!m.IsKeywordEnabled("_SURFACE_TYPE_TRANSPARENT"))
            {
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                EditorUtility.SetDirty(m);
            }
            if (m.GetTag("RenderType", false) != "Transparent")
            {
                m.SetOverrideTag("RenderType", "Transparent");
                EditorUtility.SetDirty(m);
            }
            // Justo después de lo opaco: la pintura se dibuja antes que vidrios y humo.
            int cola = (int)UnityEngine.Rendering.RenderQueue.Transparent - 50;
            if (m.renderQueue != cola)
            {
                m.renderQueue = cola;
                EditorUtility.SetDirty(m);
            }
            if (m.GetShaderPassEnabled("ShadowCaster"))
            {
                m.SetShaderPassEnabled("ShadowCaster", false);
                EditorUtility.SetDirty(m);
            }
            if (nuevo)
            {
                AssetDatabase.CreateAsset(m, ruta);
                Informe.AppendLine("material nuevo: " + ruta);
            }
            return m;
        }

        private static void Fijar(Material m, string nombre, float v)
        {
            if (m.HasProperty(nombre) && !Mathf.Approximately(m.GetFloat(nombre), v))
            {
                m.SetFloat(nombre, v);
                EditorUtility.SetDirty(m);
            }
        }

        private static void Fijar(Material m, string nombre, Color v)
        {
            if (m.HasProperty(nombre) && m.GetColor(nombre) != v)
            {
                m.SetColor(nombre, v);
                EditorUtility.SetDirty(m);
            }
        }

        private static void Fijar(Material m, string nombre, Texture v)
        {
            if (m.HasProperty(nombre) && m.GetTexture(nombre) != v)
            {
                m.SetTexture(nombre, v);
                EditorUtility.SetDirty(m);
            }
        }

        // ---------------------------------------------------------------- Respaldos hechos en Unity

        /// <summary>Franjas de pintura gastada sobre negro (negro = transparente). Bandas en fracción del ancho (o del alto si <paramref name="enX"/> es falso, las bandas van en X igual).</summary>
        public static byte[] Rayas(int ancho, int alto, Color pintura, (float a, float b)[] bandas, bool conBorde, int semilla)
        {
            System.Random azar = new System.Random(semilla);
            Texture2D t = new Texture2D(ancho, alto, TextureFormat.RGB24, false);
            Color[] px = new Color[ancho * alto];
            float ruidoX = (float)azar.NextDouble() * 100f, ruidoY = (float)azar.NextDouble() * 100f;
            for (int y = 0; y < alto; y++)
            {
                for (int x = 0; x < ancho; x++)
                {
                    float u = (x + 0.5f) / ancho;
                    float v = (y + 0.5f) / alto;
                    bool enBanda = bandas.Any(b => u >= b.a && u <= b.b);
                    if (conBorde && (v < 0.03f || v > 0.97f))
                    {
                        enBanda = false;
                    }
                    float gasto = Mathf.PerlinNoise(ruidoX + u * 18f, ruidoY + v * 18f) * 0.6f + Mathf.PerlinNoise(ruidoX + u * 70f, ruidoY + v * 70f) * 0.4f;
                    float a = enBanda ? Mathf.Clamp01((gasto - 0.25f) * 3f) : 0f;
                    px[y * ancho + x] = pintura * a;
                }
            }
            t.SetPixels(px);
            t.Apply();
            byte[] png = t.EncodeToPNG();
            Object.DestroyImmediate(t);
            return png;
        }
    }
}
