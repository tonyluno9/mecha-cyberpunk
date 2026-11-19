using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>Un prop de la tanda del 18-nov: código, eje y medida (m); si Dimensiones no es cero, escala cada eje a esa caja (largo en el eje horizontal más largo).</summary>
    public sealed class EntradaZonaT1382
    {
        public readonly string Codigo;
        public readonly ArteT250.Eje Eje;
        public readonly float Medida;
        public readonly TipoColliderZona TipoCollider;
        public readonly Vector3 Dimensiones;

        public EntradaZonaT1382(string codigo, ArteT250.Eje eje, float medida, TipoColliderZona tipoCollider, Vector3 dimensiones = default)
        {
            Codigo = codigo;
            Eje = eje;
            Medida = medida;
            TipoCollider = tipoCollider;
            Dimensiones = dimensiones;
        }

        /// <summary>Grupo de zona: AU, CC, ML, PU.</summary>
        public string Grupo => Codigo.Substring(0, 2);

        public string Nombre(string variante)
        {
            return Codigo + "_" + variante;
        }
    }

    /// <summary>
    /// T-1382: importa los props de la tanda del 18-nov (zips de Tripo ya descomprimidos en Assets/Arte/Zonas/&lt;grupo&gt;/Fuentes18nov~/),
    /// las texturas de calle T07-T09 con sus materiales y el fondo SK01_Skyline con su prefab de arco.
    /// Idempotente. Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ImportarZonasT1382.Aplicar y luego .Fotos
    /// </summary>
    public static class ImportarZonasT1382
    {
        public const string CarpetaZonas = "Assets/Arte/Zonas/";
        public const string CarpetaFuentes = "Fuentes18nov~";
        public const string CarpetaCalle = "Assets/Arte/Materiales/Calle18nov/";
        public const string CarpetaFondos = "Assets/Arte/Fondos/";
        public const string RutaInforme = "Logs/t1382-importar.txt";
        public const string CarpetaFotos = "docs/capturas/t-1382";

        private static readonly string[] Variantes = { "a", "b" };

        private const int TriangulosParaLod = 5000;
        private const float MedidaTexturaGrande = 4f;
        private const int TamanoTexturaGrande = 2048;
        private const int TamanoTexturaChica = 1024;
        private const int TamanoMetalSuave = 1024;
        private const float SuavidadMaterial = 0.8f;
        private const float MetalicoMaterial = 1f;
        private const float LadoCuboReferencia = 1.8f;
        private const float SeparacionCubo = 0.8f;
        private const float IntensidadLuz = 1f;
        private static readonly Vector3 DireccionCamara = new Vector3(0.6f, 0.35f, -1f);

        // Calle
        private const float MetrosPorTextura = 4f;
        private const float FuerzaNormalCalle = 2f;

        // Skyline
        private const float RadioSkyline = 900f;
        private const float AltoSkyline = 180f;
        private const float ArcoSkylineGrados = 120f;
        private const int SegmentosSkyline = 48;
        private const float BrilloSkyline = 1.6f;

        /// <summary>Códigos de la tanda que el importador no toca (personajes, drones, nave: los rigea Blender en otra tarea) o a los que les falta archivo.</summary>
        public static readonly string[] Omitidos =
        {
            "ML06_EdificioMalecon: solo hay .png (_a y _b), faltan los zips con el modelo",
            "DR02_DronPesado, DR03_DronCarga, DR04_DronExplorador: drones, los rigea Blender (DR03 y DR04 ademas sin zip de la variante _b)",
            "NV03_NaveNova: nave, otra tarea",
            "E15..E20 y NP07..NP14: personajes, los rigea Blender (E17, E18, NP10, NP12 sin zip _b; E19_a tiene un segundo zip _a_2)"
        };

        public static readonly EntradaZonaT1382[] Entradas =
        {
            new EntradaZonaT1382("AU02_BarreraConcreto", ArteT250.Eje.Largo, 3f, TipoColliderZona.Caja),
            new EntradaZonaT1382("AU03_ChatarraPuente", ArteT250.Eje.Largo, 4.5f, TipoColliderZona.Caja),
            new EntradaZonaT1382("CC06_CasaCerro", ArteT250.Eje.Alto, 7.5f, TipoColliderZona.Malla),
            new EntradaZonaT1382("CC07_CasaCerro", ArteT250.Eje.Alto, 7.5f, TipoColliderZona.Malla),
            new EntradaZonaT1382("CC08_MuroContencion", ArteT250.Eje.Largo, 6f, TipoColliderZona.Caja),
            new EntradaZonaT1382("ML05_EdificioMalecon", ArteT250.Eje.Alto, 17f, TipoColliderZona.Malla),
            new EntradaZonaT1382("ML07_Hielera", ArteT250.Eje.Alto, 0.8f, TipoColliderZona.Caja),
            new EntradaZonaT1382("PU07_PlumaCaseta", ArteT250.Eje.Alto, 3f, TipoColliderZona.Malla),
            new EntradaZonaT1382("PU08_ContenedorGraffiti", ArteT250.Eje.Largo, 12.2f, TipoColliderZona.Caja, new Vector3(12.2f, 2.59f, 2.44f)),
        };

        private static readonly string[] Calles = { "T07_AsfaltoMojado", "T08_LaminaOxidada", "T09_ConcretoManchado" };

        public static readonly StringBuilder Informe = new StringBuilder();

        // ------------------------------------------------------------------ rutas

        public static string RutaCarpeta(EntradaZonaT1382 e, string v)
        {
            return CarpetaZonas + e.Grupo + "/" + e.Nombre(v);
        }

        public static string RutaPrefab(EntradaZonaT1382 e, string v)
        {
            return CarpetaZonas + e.Grupo + "/" + e.Nombre(v) + ".prefab";
        }

        private static string RutaFuente(EntradaZonaT1382 e, string v)
        {
            return CarpetaZonas + e.Grupo + "/" + CarpetaFuentes + "/" + e.Nombre(v) + "/";
        }

        private static string RutaArchivo(EntradaZonaT1382 e, string v, string sufijo)
        {
            return RutaCarpeta(e, v) + "/" + e.Nombre(v) + sufijo;
        }

        // ------------------------------------------------------------------ aplicar

        [MenuItem("Mecha Cyberpunk/T-1382/Aplicar importacion 18-nov")]
        public static void Aplicar()
        {
            Informe.Clear();
            Informe.AppendLine("T-1382: tamano en metros (x y z), triangulos y collider de cada prefab");
            foreach (string o in Omitidos)
            {
                Informe.AppendLine("OMITIDO\t" + o);
            }

            foreach (EntradaZonaT1382 e in Entradas)
            {
                foreach (string v in Variantes)
                {
                    Desempacar(e, v);
                }
            }
            AssetDatabase.Refresh();
            foreach (EntradaZonaT1382 e in Entradas)
            {
                foreach (string v in Variantes)
                {
                    if (!File.Exists(RutaArchivo(e, v, ".fbx")))
                    {
                        continue;
                    }
                    ConfigurarImportadores(e, v);
                    Material material = CrearMaterial(e, v);
                    Informe.AppendLine(CrearPrefab(e, v, material));
                }
            }

            AplicarCalle();
            AplicarSkyline();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Directory.CreateDirectory("Logs");
            File.WriteAllText(RutaInforme, Informe.ToString());
            Debug.Log("[Arte] T-1382 importado:\n" + Informe);
        }

        // ------------------------------------------------------------------ props

        private static void Desempacar(EntradaZonaT1382 e, string v)
        {
            string origen = RutaFuente(e, v);
            string nombre = e.Nombre(v);
            if (!File.Exists(origen + nombre + ".fbx"))
            {
                Informe.AppendLine(nombre + "\tFALTA fuente " + origen + nombre + ".fbx");
                return;
            }
            Directory.CreateDirectory(RutaCarpeta(e, v));
            CopiarSiFalta(origen + nombre + ".fbx", RutaArchivo(e, v, ".fbx"));
            CopiarSiFalta(origen + "basecolor.jpg", RutaArchivo(e, v, "_basecolor.jpg"));
            CopiarSiFalta(origen + "normal.jpg", RutaArchivo(e, v, "_normal.jpg"));
            string rutaMapa = RutaArchivo(e, v, "_metalsuave.png");
            if (File.Exists(rutaMapa))
            {
                return;
            }
            if (!File.Exists(origen + "metallic.jpg") || !File.Exists(origen + "roughness.jpg"))
            {
                Informe.AppendLine(nombre + "\tFALTA metallic o roughness");
                return;
            }
            File.WriteAllBytes(rutaMapa, MapaMetalSuave(File.ReadAllBytes(origen + "metallic.jpg"), File.ReadAllBytes(origen + "roughness.jpg")));
        }

        private static void CopiarSiFalta(string origen, string destino)
        {
            if (!File.Exists(origen))
            {
                Informe.AppendLine("FALTA " + origen);
                return;
            }
            if (File.Exists(destino) && new FileInfo(destino).Length == new FileInfo(origen).Length)
            {
                return;
            }
            File.Copy(origen, destino, true);
        }

        /// <summary>R=G=B=metalico, A=1-rugosidad, a 1024.</summary>
        private static byte[] MapaMetalSuave(byte[] metalico, byte[] rugosidad)
        {
            Texture2D m = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            Texture2D r = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            m.LoadImage(metalico);
            r.LoadImage(rugosidad);
            Texture2D salida = new Texture2D(TamanoMetalSuave, TamanoMetalSuave, TextureFormat.RGBA32, false);
            Color32[] p = new Color32[TamanoMetalSuave * TamanoMetalSuave];
            for (int y = 0; y < TamanoMetalSuave; y++)
            {
                for (int x = 0; x < TamanoMetalSuave; x++)
                {
                    float u = (x + 0.5f) / TamanoMetalSuave;
                    float w = (y + 0.5f) / TamanoMetalSuave;
                    float met = m.GetPixelBilinear(u, w).r;
                    float rug = r.GetPixelBilinear(u, w).r;
                    p[y * TamanoMetalSuave + x] = (Color32)new Color(met, met, met, 1f - rug);
                }
            }
            salida.SetPixels32(p);
            salida.Apply();
            byte[] png = salida.EncodeToPNG();
            Object.DestroyImmediate(m);
            Object.DestroyImmediate(r);
            Object.DestroyImmediate(salida);
            return png;
        }

        private static void ConfigurarImportadores(EntradaZonaT1382 e, string v)
        {
            int tamano = e.Medida >= MedidaTexturaGrande ? TamanoTexturaGrande : TamanoTexturaChica;
            foreach (string sufijo in new[] { "_basecolor.jpg", "_normal.jpg", "_metalsuave.png" })
            {
                string ruta = RutaArchivo(e, v, sufijo);
                if (!File.Exists(ruta))
                {
                    continue;
                }
                AssetDatabase.ImportAsset(ruta);
                TextureImporter t = AssetImporter.GetAtPath(ruta) as TextureImporter;
                if (t == null)
                {
                    continue;
                }
                bool cambio = t.maxTextureSize != tamano;
                t.maxTextureSize = tamano;
                if (sufijo == "_normal.jpg" && t.textureType != TextureImporterType.NormalMap)
                {
                    t.textureType = TextureImporterType.NormalMap;
                    cambio = true;
                }
                if (sufijo == "_metalsuave.png" && (t.sRGBTexture || t.alphaSource != TextureImporterAlphaSource.FromInput))
                {
                    t.sRGBTexture = false;
                    t.alphaSource = TextureImporterAlphaSource.FromInput;
                    cambio = true;
                }
                if (cambio)
                {
                    t.SaveAndReimport();
                }
            }

            string rutaFbx = RutaArchivo(e, v, ".fbx");
            ModelImporter imp = AssetImporter.GetAtPath(rutaFbx) as ModelImporter;
            if (imp == null)
            {
                return;
            }
            bool cambioModelo = false;
            if (imp.materialImportMode != ModelImporterMaterialImportMode.None)
            {
                imp.materialImportMode = ModelImporterMaterialImportMode.None;
                cambioModelo = true;
            }
            if (imp.importCameras || imp.importLights)
            {
                imp.importCameras = false;
                imp.importLights = false;
                cambioModelo = true;
            }
            if (cambioModelo)
            {
                imp.SaveAndReimport();
            }
            GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>(rutaFbx);
            if (fbx != null && Triangulos(fbx) > TriangulosParaLod && !imp.generateMeshLods)
            {
                imp.generateMeshLods = true;
                imp.SaveAndReimport();
            }
        }

        private static int Triangulos(GameObject objeto)
        {
            int total = 0;
            foreach (MeshFilter f in objeto.GetComponentsInChildren<MeshFilter>(true))
            {
                if (f.sharedMesh == null)
                {
                    continue;
                }
                for (int i = 0; i < f.sharedMesh.subMeshCount; i++)
                {
                    total += (int)f.sharedMesh.GetIndexCount(i) / 3;
                }
            }
            return total;
        }

        private static Material CrearMaterial(EntradaZonaT1382 e, string v)
        {
            string ruta = RutaArchivo(e, v, ".mat");
            Material material = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, ruta);
            }
            Texture2D color = AssetDatabase.LoadAssetAtPath<Texture2D>(RutaArchivo(e, v, "_basecolor.jpg"));
            Texture2D normal = AssetDatabase.LoadAssetAtPath<Texture2D>(RutaArchivo(e, v, "_normal.jpg"));
            Texture2D metalSuave = AssetDatabase.LoadAssetAtPath<Texture2D>(RutaArchivo(e, v, "_metalsuave.png"));
            material.SetTexture("_BaseMap", color);
            material.SetTexture("_MainTex", color);
            material.SetColor("_BaseColor", Color.white);
            if (normal != null)
            {
                material.SetTexture("_BumpMap", normal);
                material.EnableKeyword("_NORMALMAP");
            }
            if (metalSuave != null)
            {
                material.SetTexture("_MetallicGlossMap", metalSuave);
                material.EnableKeyword("_METALLICSPECGLOSSMAP");
            }
            material.SetFloat("_Smoothness", SuavidadMaterial);
            material.SetFloat("_Metallic", MetalicoMaterial);
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static string CrearPrefab(EntradaZonaT1382 e, string v, Material material)
        {
            string nombre = e.Nombre(v);
            GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>(RutaArchivo(e, v, ".fbx"));
            if (fbx == null)
            {
                return nombre + "\tFALTA FBX";
            }
            GameObject raiz = new GameObject(nombre);
            GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(fbx);
            visual.name = "Visual";
            visual.transform.SetParent(raiz.transform, false);
            foreach (Renderer r in visual.GetComponentsInChildren<Renderer>(true))
            {
                r.sharedMaterials = Enumerable.Repeat(material, Mathf.Max(1, r.sharedMaterials.Length)).ToArray();
            }

            Bounds original = ArteT250.Limites(raiz);
            Vector3 escalaVector;
            if (e.Dimensiones != Vector3.zero)
            {
                bool largoEnX = original.size.x >= original.size.z;
                Vector3 destino = largoEnX ? e.Dimensiones : new Vector3(e.Dimensiones.z, e.Dimensiones.y, e.Dimensiones.x);
                escalaVector = new Vector3(
                    original.size.x > 0.0001f ? destino.x / original.size.x : 1f,
                    original.size.y > 0.0001f ? destino.y / original.size.y : 1f,
                    original.size.z > 0.0001f ? destino.z / original.size.z : 1f);
            }
            else
            {
                float actual = e.Eje == ArteT250.Eje.Alto ? original.size.y : Mathf.Max(original.size.x, original.size.z);
                escalaVector = Vector3.one * (actual > 0.0001f ? e.Medida / actual : 1f);
            }
            visual.transform.localScale = escalaVector;
            Bounds escalado = ArteT250.Limites(raiz);
            visual.transform.localPosition = new Vector3(-escalado.center.x, -escalado.min.y, -escalado.center.z);
            Bounds final = ArteT250.Limites(raiz);

            switch (e.TipoCollider)
            {
                case TipoColliderZona.Caja:
                    BoxCollider caja = raiz.AddComponent<BoxCollider>();
                    caja.center = final.center;
                    caja.size = final.size;
                    break;
                default:
                    foreach (MeshFilter f in visual.GetComponentsInChildren<MeshFilter>(true))
                    {
                        if (f.sharedMesh == null)
                        {
                            continue;
                        }
                        MeshCollider mc = f.gameObject.AddComponent<MeshCollider>();
                        mc.sharedMesh = f.sharedMesh;
                        mc.convex = false;
                    }
                    break;
            }

            string rutaPrefab = RutaPrefab(e, v);
            PrefabUtility.SaveAsPrefabAsset(raiz, rutaPrefab);
            Object.DestroyImmediate(raiz);
            ModelImporter imp = (ModelImporter)AssetImporter.GetAtPath(RutaArchivo(e, v, ".fbx"));
            return string.Format("{0}\toriginal {1:F2}x{2:F2}x{3:F2}\tfinal {4:F2}x{5:F2}x{6:F2}\ttriangulos {7}\tcollider {8}\tLOD {9}",
                nombre, original.size.x, original.size.y, original.size.z, final.size.x, final.size.y, final.size.z,
                Triangulos(fbx), e.TipoCollider == TipoColliderZona.Caja ? "BoxCollider" : "MeshCollider", imp.generateMeshLods ? "si" : "no");
        }

        // ------------------------------------------------------------------ calle T07-T09

        private static void AplicarCalle()
        {
            foreach (string t in Calles)
            {
                foreach (string v in Variantes)
                {
                    string baseRuta = CarpetaCalle + t + "_" + v + ".png";
                    if (!File.Exists(baseRuta))
                    {
                        Informe.AppendLine(t + "_" + v + "\tFALTA textura " + baseRuta);
                        continue;
                    }
                    string normalRuta = CarpetaCalle + t + "_" + v + "_normal.png";
                    if (!File.Exists(normalRuta))
                    {
                        File.WriteAllBytes(normalRuta, NormalDesdeAltura(File.ReadAllBytes(baseRuta)));
                    }
                    AssetDatabase.ImportAsset(baseRuta);
                    AssetDatabase.ImportAsset(normalRuta);
                    ConfigurarTexturaCalle(baseRuta, false);
                    ConfigurarTexturaCalle(normalRuta, true);

                    // _a es la variante principal (la que mejor se ve: manchas y reflejos mas claros); _b va con sufijo.
                    string sufijoMaterial = v == "a" ? "" : "_b";
                    string rutaMaterial = CarpetaCalle + "M_" + t + sufijoMaterial + ".mat";
                    Material m = AssetDatabase.LoadAssetAtPath<Material>(rutaMaterial);
                    if (m == null)
                    {
                        m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                        AssetDatabase.CreateAsset(m, rutaMaterial);
                    }
                    float metalico = 0f;
                    float suavidad = 0.25f;
                    if (t.StartsWith("T07"))
                    {
                        suavidad = 0.8f;
                    }
                    else if (t.StartsWith("T08"))
                    {
                        metalico = 0.5f;
                        suavidad = 0.35f;
                    }
                    Texture2D color = AssetDatabase.LoadAssetAtPath<Texture2D>(baseRuta);
                    Texture2D normal = AssetDatabase.LoadAssetAtPath<Texture2D>(normalRuta);
                    m.SetTexture("_BaseMap", color);
                    m.SetTexture("_MainTex", color);
                    m.SetColor("_BaseColor", Color.white);
                    m.SetTexture("_BumpMap", normal);
                    m.SetFloat("_BumpScale", 1f);
                    m.EnableKeyword("_NORMALMAP");
                    m.SetFloat("_Metallic", metalico);
                    m.SetFloat("_Smoothness", suavidad);
                    // Tiling para una repeticion cada 4 m, suponiendo UV de 1 unidad por metro (los suelos del proyecto).
                    Vector2 tiling = Vector2.one / MetrosPorTextura;
                    m.SetTextureScale("_BaseMap", tiling);
                    m.SetTextureScale("_MainTex", tiling);
                    m.SetTextureScale("_BumpMap", tiling);
                    m.enableInstancing = true;
                    EditorUtility.SetDirty(m);
                    Informe.AppendLine("M_" + t + sufijoMaterial + "\tmetalico " + metalico.ToString("F2") + "\tsuavidad " + suavidad.ToString("F2") + "\ttiling " + tiling.x.ToString("F3") + "\tnormal generado de la altura");
                }
            }
        }

        private static void ConfigurarTexturaCalle(string ruta, bool esNormal)
        {
            TextureImporter t = AssetImporter.GetAtPath(ruta) as TextureImporter;
            if (t == null)
            {
                return;
            }
            bool cambio = false;
            if (esNormal && t.textureType != TextureImporterType.NormalMap)
            {
                t.textureType = TextureImporterType.NormalMap;
                cambio = true;
            }
            if (!esNormal && t.alphaSource != TextureImporterAlphaSource.None)
            {
                t.alphaSource = TextureImporterAlphaSource.None; // algunas vienen en RGBA sin alfa util
                cambio = true;
            }
            if (t.wrapMode != TextureWrapMode.Repeat)
            {
                t.wrapMode = TextureWrapMode.Repeat;
                cambio = true;
            }
            if (t.maxTextureSize != TamanoTexturaChica)
            {
                t.maxTextureSize = TamanoTexturaChica;
                cambio = true;
            }
            if (cambio)
            {
                t.SaveAndReimport();
            }
        }

        /// <summary>Mapa normal (formato PNG, codificado RGB) desde la luminancia de la imagen, con Sobel y envoltura (la textura repite).</summary>
        private static byte[] NormalDesdeAltura(byte[] png)
        {
            Texture2D origen = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            origen.LoadImage(png);
            int ancho = origen.width;
            int alto = origen.height;
            Color32[] px = origen.GetPixels32();
            float[] h = new float[px.Length];
            for (int i = 0; i < px.Length; i++)
            {
                h[i] = (0.299f * px[i].r + 0.587f * px[i].g + 0.114f * px[i].b) / 255f;
            }
            Color32[] salida = new Color32[px.Length];
            for (int y = 0; y < alto; y++)
            {
                for (int x = 0; x < ancho; x++)
                {
                    int xm = (x + ancho - 1) % ancho;
                    int xp = (x + 1) % ancho;
                    int ym = (y + alto - 1) % alto;
                    int yp = (y + 1) % alto;
                    float dx = (h[y * ancho + xp] - h[y * ancho + xm]) * FuerzaNormalCalle;
                    float dy = (h[yp * ancho + x] - h[ym * ancho + x]) * FuerzaNormalCalle;
                    Vector3 n = new Vector3(-dx, -dy, 1f).normalized;
                    salida[y * ancho + x] = new Color32(
                        (byte)Mathf.RoundToInt((n.x * 0.5f + 0.5f) * 255f),
                        (byte)Mathf.RoundToInt((n.y * 0.5f + 0.5f) * 255f),
                        (byte)Mathf.RoundToInt((n.z * 0.5f + 0.5f) * 255f),
                        255);
                }
            }
            Texture2D resultado = new Texture2D(ancho, alto, TextureFormat.RGBA32, false);
            resultado.SetPixels32(salida);
            resultado.Apply();
            byte[] bytes = resultado.EncodeToPNG();
            Object.DestroyImmediate(origen);
            Object.DestroyImmediate(resultado);
            return bytes;
        }

        // ------------------------------------------------------------------ skyline

        private static void AplicarSkyline()
        {
            Material[] materiales = new Material[2];
            for (int i = 0; i < Variantes.Length; i++)
            {
                string v = Variantes[i];
                string rutaTextura = CarpetaFondos + "SK01_Skyline_" + v + ".png";
                if (!File.Exists(rutaTextura))
                {
                    Informe.AppendLine("SK01_Skyline_" + v + "\tFALTA " + rutaTextura);
                    continue;
                }
                AssetDatabase.ImportAsset(rutaTextura);
                TextureImporter t = AssetImporter.GetAtPath(rutaTextura) as TextureImporter;
                if (t != null)
                {
                    t.wrapMode = TextureWrapMode.Clamp;
                    t.alphaSource = TextureImporterAlphaSource.None;
                    t.maxTextureSize = TamanoTexturaGrande;
                    t.mipmapEnabled = true;
                    t.SaveAndReimport();
                }
                string rutaMaterial = CarpetaFondos + (v == "a" ? "M_SK01_Skyline.mat" : "M_SK01_Skyline_b.mat");
                Material m = AssetDatabase.LoadAssetAtPath<Material>(rutaMaterial);
                if (m == null)
                {
                    m = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                    AssetDatabase.CreateAsset(m, rutaMaterial);
                }
                Texture2D textura = AssetDatabase.LoadAssetAtPath<Texture2D>(rutaTextura);
                m.SetTexture("_BaseMap", textura);
                m.SetTexture("_MainTex", textura);
                // La imagen no trae alfa: el cielo es negro, asi que se mezcla en aditivo (el negro no suma) y el brillo sale del color HDR.
                m.SetColor("_BaseColor", new Color(BrilloSkyline, BrilloSkyline, BrilloSkyline, 1f));
                m.SetFloat("_Surface", 1f);
                m.SetFloat("_Blend", 2f);
                m.SetFloat("_SrcBlend", 1f);
                m.SetFloat("_DstBlend", 1f);
                m.SetFloat("_SrcBlendAlpha", 1f);
                m.SetFloat("_DstBlendAlpha", 1f);
                m.SetFloat("_ZWrite", 0f);
                m.SetFloat("_Cull", 0f);
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                m.SetOverrideTag("RenderType", "Transparent");
                m.renderQueue = 3000;
                m.SetShaderPassEnabled("ShadowCaster", false);
                EditorUtility.SetDirty(m);
                materiales[i] = m;
                Informe.AppendLine("M_SK01_Skyline" + (v == "a" ? "" : "_b") + "\tUnlit aditivo, brillo " + BrilloSkyline.ToString("F1"));
            }
            if (materiales[0] == null)
            {
                return;
            }

            string rutaMalla = CarpetaFondos + "SK01_Skyline_Arco.asset";
            Mesh malla = AssetDatabase.LoadAssetAtPath<Mesh>(rutaMalla);
            Mesh nueva = CrearMallaArco();
            if (malla == null)
            {
                AssetDatabase.CreateAsset(nueva, rutaMalla);
                malla = nueva;
            }
            else
            {
                malla.Clear();
                malla.vertices = nueva.vertices;
                malla.normals = nueva.normals;
                malla.uv = nueva.uv;
                malla.triangles = nueva.triangles;
                malla.RecalculateBounds();
                Object.DestroyImmediate(nueva);
                EditorUtility.SetDirty(malla);
            }

            GameObject raiz = new GameObject("SK01_Skyline");
            GameObject arco = new GameObject("Arco");
            arco.transform.SetParent(raiz.transform, false);
            arco.AddComponent<MeshFilter>().sharedMesh = malla;
            MeshRenderer r = arco.AddComponent<MeshRenderer>();
            r.sharedMaterial = materiales[0];
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            r.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            r.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            PrefabUtility.SaveAsPrefabAsset(raiz, CarpetaFondos + "SK01_Skyline.prefab");
            Object.DestroyImmediate(raiz);
            Informe.AppendLine("SK01_Skyline.prefab\tarco " + ArcoSkylineGrados + " grados, radio " + RadioSkyline + " m, alto " + AltoSkyline
                + " m, base en y=0, centro del cilindro en el origen del prefab, arco hacia +Z; triangulos " + SegmentosSkyline * 2 + "\tsin collider, sin sombras");
        }

        /// <summary>Tira de cilindro vista desde dentro: x = R sen(a), z = R cos(a), centrada en +Z; u crece de izquierda a derecha para quien mira hacia +Z.</summary>
        private static Mesh CrearMallaArco()
        {
            int columnas = SegmentosSkyline + 1;
            Vector3[] vertices = new Vector3[columnas * 2];
            Vector3[] normales = new Vector3[columnas * 2];
            Vector2[] uv = new Vector2[columnas * 2];
            for (int i = 0; i < columnas; i++)
            {
                float u = (float)i / SegmentosSkyline;
                float angulo = (u - 0.5f) * ArcoSkylineGrados * Mathf.Deg2Rad;
                float x = Mathf.Sin(angulo) * RadioSkyline;
                float z = Mathf.Cos(angulo) * RadioSkyline;
                Vector3 haciaCentro = new Vector3(-Mathf.Sin(angulo), 0f, -Mathf.Cos(angulo));
                vertices[i * 2] = new Vector3(x, 0f, z);
                vertices[i * 2 + 1] = new Vector3(x, AltoSkyline, z);
                normales[i * 2] = haciaCentro;
                normales[i * 2 + 1] = haciaCentro;
                uv[i * 2] = new Vector2(u, 0f);
                uv[i * 2 + 1] = new Vector2(u, 1f);
            }
            int[] triangulos = new int[SegmentosSkyline * 6];
            for (int i = 0; i < SegmentosSkyline; i++)
            {
                int a = i * 2;
                int b = a + 1;
                int c = a + 2;
                int d = a + 3;
                // Sentido horario visto desde el centro del cilindro (la cara que se ve desde dentro).
                triangulos[i * 6] = a;
                triangulos[i * 6 + 1] = b;
                triangulos[i * 6 + 2] = c;
                triangulos[i * 6 + 3] = c;
                triangulos[i * 6 + 4] = b;
                triangulos[i * 6 + 5] = d;
            }
            Mesh malla = new Mesh();
            malla.name = "SK01_Skyline_Arco";
            malla.vertices = vertices;
            malla.normals = normales;
            malla.uv = uv;
            malla.triangles = triangulos;
            malla.RecalculateBounds();
            return malla;
        }

        // ------------------------------------------------------------------ fotos

        /// <summary>docs/capturas/t-1382/&lt;Codigo&gt;_&lt;a|b&gt;.png: el prop con un cubo de 1.8 m al lado, desde una diagonal frontal; y SK01_Skyline.png desde el centro del arco.</summary>
        [MenuItem("Mecha Cyberpunk/T-1382/Fotos")]
        public static void Fotos()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            ArteT250.PonerLuz();
            GameObject sol = GameObject.Find("Sol");
            if (sol != null)
            {
                sol.GetComponent<Light>().intensity = IntensidadLuz;
            }
            GameObject camaraObjeto = new GameObject("Camara");
            Camera camara = camaraObjeto.AddComponent<Camera>();
            StringBuilder resumen = new StringBuilder();
            foreach (EntradaZonaT1382 e in Entradas)
            {
                foreach (string v in Variantes)
                {
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab(e, v));
                    if (prefab == null)
                    {
                        resumen.AppendLine(e.Nombre(v) + "\tsin foto: falta el prefab");
                        continue;
                    }
                    GameObject prop = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                    Bounds b = ArteT250.Limites(prop);
                    GameObject cubo = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cubo.name = "Cubo1_8m";
                    cubo.transform.localScale = Vector3.one * LadoCuboReferencia;
                    cubo.transform.position = new Vector3(b.max.x + SeparacionCubo + LadoCuboReferencia * 0.5f, LadoCuboReferencia * 0.5f, 0f);
                    b.Encapsulate(ArteT250.Limites(cubo));
                    ArteT250.Encuadrar(camara, b, DireccionCamara);
                    ArteT250.Guardar(camara, Path.Combine(CarpetaFotos, e.Nombre(v) + ".png"));
                    Object.DestroyImmediate(prop);
                    Object.DestroyImmediate(cubo);
                }
            }

            GameObject skyline = AssetDatabase.LoadAssetAtPath<GameObject>(CarpetaFondos + "SK01_Skyline.prefab");
            if (skyline != null)
            {
                GameObject instancia = (GameObject)PrefabUtility.InstantiatePrefab(skyline);
                camara.clearFlags = CameraClearFlags.SolidColor;
                camara.backgroundColor = Color.black;
                camara.fieldOfView = 70f;
                camara.farClipPlane = 3000f;
                camara.transform.position = new Vector3(0f, 2f, 0f);
                camara.transform.rotation = Quaternion.LookRotation(Vector3.forward + Vector3.up * 0.25f);
                ArteT250.Guardar(camara, Path.Combine(CarpetaFotos, "SK01_Skyline.png"));
                Object.DestroyImmediate(instancia);
            }
            Object.DestroyImmediate(camaraObjeto);
            Directory.CreateDirectory("Logs");
            File.AppendAllText(RutaInforme, "\nFotos:\n" + resumen);
            Debug.Log("[Arte] T-1382 fotos hechas en " + CarpetaFotos + "\n" + resumen);
        }
    }
}
