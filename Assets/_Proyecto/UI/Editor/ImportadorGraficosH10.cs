using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.UI.Editor
{
    /// <summary>
    /// T-725: toma los gráficos del chat de arte (imageassets/ui/hud/H20 a H26, líneas turquesa sobre negro puro), les quita el negro
    /// (el brillo pasa a alfa), los recorta y los deja como sprites en UI/Iconos/H10/. Los íconos, las estrellas y el muñequito quedan
    /// blancos para que el HUD los tiñe; las barras y el marco conservan su color. El muñequito se parte en cabeza, torso, brazos y piernas
    /// (una línea y un relleno por parte) con las mismas líneas de H24. Idempotente.
    /// Batchmode: -executeMethod MechaCyberpunk.UI.Editor.ImportadorGraficosH10.CrearT725
    /// </summary>
    public static class ImportadorGraficosH10
    {
        public const string Carpeta = "Assets/_Proyecto/UI/Iconos/H10/";
        private const float PisoNegro = 0.07f;
        private const float RangoAlfa = 0.55f;
        private const float UmbralRecorte = 0.10f;
        private const float UmbralPared = 0.30f;

        /// <summary>Nombres de los sprites que deja (sin .png).</summary>
        public static readonly string[] Objetos = { "Obj_BebidaEnergetica", "Obj_Papitas", "Obj_Venda", "Obj_Cafe", "Obj_Bateria", "Obj_Granada" };
        public static readonly string[] Partes = { "Cabeza", "Torso", "Brazos", "Piernas" };

        private sealed class Imagen
        {
            public int Ancho;
            public int Alto;
            public Color32[] Pixeles; // fila 0 = abajo (como Unity)
        }

        [MenuItem("Mecha Cyberpunk/Crear T-725 (gráficos del HUD)")]
        public static void CrearT725()
        {
            Importar();
            CreadorHudH10.Crear();
        }

        /// <summary>Dónde están los PNG del chat de arte: imageassets/ui/hud/ del proyecto o, si no, el de la carpeta principal.</summary>
        public static string CarpetaOrigen()
        {
            string raiz = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string[] candidatos =
            {
                Path.Combine(raiz, "imageassets/ui/hud"),
                Path.GetFullPath(Path.Combine(raiz, "../mecha-cyberpunk/imageassets/ui/hud")),
            };
            foreach (string c in candidatos)
            {
                if (Directory.Exists(c))
                {
                    return c;
                }
            }
            return null;
        }

        public static bool Importar()
        {
            string origen = CarpetaOrigen();
            if (origen == null)
            {
                Debug.LogError("[UI] No encuentro imageassets/ui/hud/ (H20-H26).");
                return false;
            }
            Directory.CreateDirectory(Carpeta);

            Imagen h20 = Leer(origen, "H20_BarrasVida");
            Imagen h21 = Leer(origen, "H21_BarrasVacias");
            Imagen h22 = Leer(origen, "H22_MarcoMinimapa");
            Imagen h23 = Leer(origen, "H23_IconosObjetos");
            Imagen h24 = Leer(origen, "H24_Munequito");
            Imagen h25 = Leer(origen, "H25_Estrella");
            Imagen h26 = Leer(origen, "H26_CasillaObjeto");
            if (h20 == null || h21 == null || h22 == null || h23 == null || h24 == null || h25 == null || h26 == null)
            {
                return false;
            }

            // Barras: el marco vacío de H21 (sin el número) y el relleno liso de H20.
            Guardar("Barra_MarcoEscudo", Reducir(Recortar(h21, Caja(415, 315, 700, 372), false), 300));
            Guardar("Barra_MarcoVida", Reducir(Recortar(h21, Caja(415, 380, 995, 455), false), 600));
            Guardar("Barra_Emblema", Reducir(Recortar(h21, Caja(280, 315, 402, 450), false), 160));
            Guardar("Barra_RellenoEscudo", Recortar(h20, Caja(514, 338, 732, 354), false));
            Guardar("Barra_RellenoVida", Recortar(h20, Caja(516, 388, 884, 411), false));

            Guardar("Minimapa_Marco", Reducir(Recortar(h22, Caja(0, 0, 1024, 1024), false), 300));
            Guardar("Casilla_Objeto", Reducir(Recortar(h26, Caja(290, 290, 720, 730), false), 256));

            // Íconos: cuadrícula de 3 columnas por 2 filas.
            int ancho = h23.Ancho / 3;
            int alto = h23.Alto / 2;
            for (int i = 0; i < Objetos.Length; i++)
            {
                int c = i % 3;
                int f = i / 3;
                Guardar(Objetos[i], Reducir(Recortar(h23, Caja(c * ancho, f * alto, (c + 1) * ancho, (f + 1) * alto), true), 200));
            }

            // Estrellas: dos cuadros iguales alrededor de cada una.
            Guardar("Estrella_Llena", Reducir(Recortar(h25, Caja(352, 220, 672, 540), true, false), 128));
            Guardar("Estrella_Contorno", Reducir(Recortar(h25, Caja(704, 220, 1024, 540), true, false), 128));

            ImportarMunequito(h24);
            AssetDatabase.Refresh();
            AjustarBordes();
            Debug.Log("[UI] T-725: gráficos del HUD importados de " + origen);
            return true;
        }

        // ---- Muñequito ----

        private static void ImportarMunequito(Imagen original)
        {
            Imagen blanco = Recortar(original, Caja(0, 0, original.Ancho, original.Alto), true, false);
            RectInt caja = CajaDeContenido(blanco);
            bool[] interior = Interior(blanco);
            for (int z = 0; z < Partes.Length; z++)
            {
                Imagen linea = Nuevo(caja.width, caja.height);
                Imagen relleno = Nuevo(caja.width, caja.height);
                for (int y = 0; y < caja.height; y++)
                {
                    for (int x = 0; x < caja.width; x++)
                    {
                        int sx = caja.x + x;
                        int sy = caja.y + y;
                        int yArriba = blanco.Alto - 1 - sy;
                        if (ZonaDe(sx, yArriba) != z)
                        {
                            continue;
                        }
                        int origenIndice = sy * blanco.Ancho + sx;
                        linea.Pixeles[y * caja.width + x] = blanco.Pixeles[origenIndice];
                        if (interior[origenIndice])
                        {
                            relleno.Pixeles[y * caja.width + x] = new Color32(255, 255, 255, 255);
                        }
                    }
                }
                Guardar("Cuerpo_" + Partes[z] + "_Linea", Reducir(linea, 360));
                Guardar("Cuerpo_" + Partes[z] + "_Relleno", Reducir(relleno, 360));
            }
        }

        /// <summary>Qué parte del cuerpo es el punto (x, y de arriba hacia abajo) de H24 (896 x 1200): 0 cabeza, 1 torso, 2 brazos, 3 piernas.</summary>
        public static int ZonaDe(int x, int yArriba)
        {
            if (yArriba < 262)
            {
                return 0;
            }
            bool bajoCadera = yArriba >= 560;
            int izquierda = bajoCadera ? 322 : 350;
            int derecha = bajoCadera ? 574 : 546;
            bool brazo = yArriba < 720 && (x < izquierda || x > derecha);
            if (brazo)
            {
                return 2;
            }
            return yArriba < 575 ? 1 : 3;
        }

        // Pixeles que quedan dentro de la silueta cerrada (no tocan el exterior sin cruzar una línea).
        private static bool[] Interior(Imagen imagen)
        {
            int w = imagen.Ancho;
            int h = imagen.Alto;
            bool[] exterior = new bool[w * h];
            Stack<int> pila = new Stack<int>();
            for (int x = 0; x < w; x++)
            {
                Sembrar(imagen, exterior, pila, x, 0);
                Sembrar(imagen, exterior, pila, x, h - 1);
            }
            for (int y = 0; y < h; y++)
            {
                Sembrar(imagen, exterior, pila, 0, y);
                Sembrar(imagen, exterior, pila, w - 1, y);
            }
            while (pila.Count > 0)
            {
                int p = pila.Pop();
                int px = p % w;
                int py = p / w;
                if (px > 0) Sembrar(imagen, exterior, pila, px - 1, py);
                if (px < w - 1) Sembrar(imagen, exterior, pila, px + 1, py);
                if (py > 0) Sembrar(imagen, exterior, pila, px, py - 1);
                if (py < h - 1) Sembrar(imagen, exterior, pila, px, py + 1);
            }
            bool[] dentro = new bool[w * h];
            for (int i = 0; i < dentro.Length; i++)
            {
                dentro[i] = !exterior[i] && imagen.Pixeles[i].a < UmbralPared * 255f;
            }
            return dentro;
        }

        private static void Sembrar(Imagen imagen, bool[] visto, Stack<int> pila, int x, int y)
        {
            int i = y * imagen.Ancho + x;
            if (visto[i] || imagen.Pixeles[i].a >= UmbralPared * 255f)
            {
                return;
            }
            visto[i] = true;
            pila.Push(i);
        }

        // ---- Pixeles ----

        private static RectInt Caja(int x0, int y0, int x1, int y1) => new RectInt(x0, y0, x1 - x0, y1 - y0); // y de arriba hacia abajo

        private static Imagen Nuevo(int ancho, int alto) => new Imagen { Ancho = ancho, Alto = alto, Pixeles = new Color32[ancho * alto] };

        private static Imagen Leer(string carpeta, string nombre)
        {
            string ruta = Path.Combine(carpeta, nombre + ".png");
            if (!File.Exists(ruta))
            {
                Debug.LogError("[UI] Falta " + ruta);
                return null;
            }
            Texture2D textura = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            textura.LoadImage(File.ReadAllBytes(ruta));
            Imagen imagen = new Imagen { Ancho = textura.width, Alto = textura.height, Pixeles = textura.GetPixels32() };
            UnityEngine.Object.DestroyImmediate(textura);
            return imagen;
        }

        // Quita el negro (brillo a alfa) de la caja pedida (y de arriba hacia abajo); con recortar, la deja justa al contenido.
        private static Imagen Recortar(Imagen origen, RectInt cajaArriba, bool blanquear, bool recortar = true)
        {
            int x0 = Mathf.Clamp(cajaArriba.x, 0, origen.Ancho);
            int x1 = Mathf.Clamp(cajaArriba.xMax, 0, origen.Ancho);
            int y1 = Mathf.Clamp(origen.Alto - cajaArriba.y, 0, origen.Alto);
            int y0 = Mathf.Clamp(origen.Alto - cajaArriba.yMax, 0, origen.Alto);
            Imagen salida = Nuevo(x1 - x0, y1 - y0);
            for (int y = 0; y < salida.Alto; y++)
            {
                for (int x = 0; x < salida.Ancho; x++)
                {
                    Color32 c = origen.Pixeles[(y0 + y) * origen.Ancho + x0 + x];
                    float m = Mathf.Max(c.r, Mathf.Max(c.g, c.b)) / 255f;
                    float a = Mathf.Clamp01((m - PisoNegro) / RangoAlfa);
                    if (a <= 0f || m <= 0f)
                    {
                        salida.Pixeles[y * salida.Ancho + x] = new Color32(255, 255, 255, 0);
                        continue;
                    }
                    byte r = blanquear ? (byte)255 : (byte)Mathf.Min(255f, c.r / m);
                    byte g = blanquear ? (byte)255 : (byte)Mathf.Min(255f, c.g / m);
                    byte b = blanquear ? (byte)255 : (byte)Mathf.Min(255f, c.b / m);
                    salida.Pixeles[y * salida.Ancho + x] = new Color32(r, g, b, (byte)Mathf.RoundToInt(a * 255f));
                }
            }
            return recortar ? Ajustar(salida) : salida;
        }

        private static RectInt CajaDeContenido(Imagen imagen)
        {
            int minX = imagen.Ancho, minY = imagen.Alto, maxX = -1, maxY = -1;
            byte umbral = (byte)(UmbralRecorte * 255f);
            for (int y = 0; y < imagen.Alto; y++)
            {
                for (int x = 0; x < imagen.Ancho; x++)
                {
                    if (imagen.Pixeles[y * imagen.Ancho + x].a > umbral)
                    {
                        minX = Mathf.Min(minX, x);
                        maxX = Mathf.Max(maxX, x);
                        minY = Mathf.Min(minY, y);
                        maxY = Mathf.Max(maxY, y);
                    }
                }
            }
            return maxX < 0 ? new RectInt(0, 0, imagen.Ancho, imagen.Alto) : new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1);
        }

        private static Imagen Ajustar(Imagen imagen)
        {
            RectInt caja = CajaDeContenido(imagen);
            Imagen salida = Nuevo(caja.width, caja.height);
            for (int y = 0; y < caja.height; y++)
            {
                Array.Copy(imagen.Pixeles, (caja.y + y) * imagen.Ancho + caja.x, salida.Pixeles, y * caja.width, caja.width);
            }
            return salida;
        }

        // Promedia bloques enteros para que el sprite no se vea con dientes al achicarlo (con alfa premultiplicado).
        private static Imagen Reducir(Imagen imagen, int ladoMayor)
        {
            int factor = Mathf.Max(1, Mathf.RoundToInt(Mathf.Max(imagen.Ancho, imagen.Alto) / (float)ladoMayor));
            if (factor <= 1)
            {
                return imagen;
            }
            Imagen salida = Nuevo(Mathf.Max(1, imagen.Ancho / factor), Mathf.Max(1, imagen.Alto / factor));
            for (int y = 0; y < salida.Alto; y++)
            {
                for (int x = 0; x < salida.Ancho; x++)
                {
                    float r = 0f, g = 0f, b = 0f, a = 0f;
                    for (int j = 0; j < factor; j++)
                    {
                        for (int i = 0; i < factor; i++)
                        {
                            Color32 c = imagen.Pixeles[(y * factor + j) * imagen.Ancho + x * factor + i];
                            float peso = c.a / 255f;
                            r += c.r * peso;
                            g += c.g * peso;
                            b += c.b * peso;
                            a += peso;
                        }
                    }
                    float n = factor * factor;
                    if (a <= 0f)
                    {
                        salida.Pixeles[y * salida.Ancho + x] = new Color32(255, 255, 255, 0);
                        continue;
                    }
                    salida.Pixeles[y * salida.Ancho + x] = new Color32((byte)(r / a), (byte)(g / a), (byte)(b / a), (byte)Mathf.RoundToInt(a / n * 255f));
                }
            }
            return salida;
        }

        private static void Guardar(string nombre, Imagen imagen)
        {
            Texture2D textura = new Texture2D(imagen.Ancho, imagen.Alto, TextureFormat.RGBA32, false);
            textura.SetPixels32(imagen.Pixeles);
            textura.Apply();
            File.WriteAllBytes(Carpeta + nombre + ".png", textura.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(textura);
        }

        // El marco del minimapa va en 9-slice (las esquinas marcadas no se estiran).
        private static void AjustarBordes()
        {
            Ajustar9Slice(Carpeta + "Minimapa_Marco.png", 0.12f);
        }

        private static void Ajustar9Slice(string ruta, float fraccion)
        {
            AssetDatabase.ImportAsset(ruta, ImportAssetOptions.ForceSynchronousImport);
            TextureImporter importador = AssetImporter.GetAtPath(ruta) as TextureImporter;
            if (importador == null)
            {
                return;
            }
            Texture2D textura = AssetDatabase.LoadAssetAtPath<Texture2D>(ruta);
            float borde = Mathf.Round(textura.width * fraccion);
            importador.spriteBorder = new Vector4(borde, borde, borde, borde);
            importador.SaveAndReimport();
        }
    }
}
