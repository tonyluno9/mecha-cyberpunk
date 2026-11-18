using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>Cómo choca un prop de zona.</summary>
    public enum TipoColliderZona
    {
        Caja,
        Malla,
        Poste
    }

    /// <summary>Un prop de zona: nombre del zip y del modelo, eje y medida (m) a la que se escala, collider y alto del tronco (Poste).</summary>
    public sealed class EntradaZonaT1340
    {
        public readonly string Nombre;
        public readonly ArteT250.Eje Eje;
        public readonly float Medida;
        public readonly TipoColliderZona TipoCollider;
        /// <summary>Alto de la cápsula del tronco (m) en los Poste; 0 = alto completo del modelo.</summary>
        public readonly float AltoTronco;

        public EntradaZonaT1340(string nombre, ArteT250.Eje eje, float medida, TipoColliderZona tipoCollider, float altoTronco = 0f)
        {
            Nombre = nombre;
            Eje = eje;
            Medida = medida;
            TipoCollider = tipoCollider;
            AltoTronco = altoTronco;
        }

        /// <summary>Las dos primeras letras del nombre: AR, PZ, ...</summary>
        public string Zona => Nombre.Substring(0, 2);

        public override string ToString()
        {
            return Nombre;
        }
    }

    /// <summary>
    /// T-1340: importa los props de zonas nuevos de imageassets/modelos-nuevos/nuevos-17nov/ (zips de Tripo): FBX, texturas, mapa metal/suavidad,
    /// material URP Lit, prefab con colliders y capturas con el jugador al lado. Los códigos (PU01, CC.., LO..) chocan con modelos viejos,
    /// por eso no usa ArteT250.Modelo ni ZonaDe. Idempotente.
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ImportarZonasT1340.ImportarYCapturar
    /// </summary>
    public static class ImportarZonasT1340
    {
        public const string CarpetaZips = "imageassets/modelos-nuevos/nuevos-17nov/";
        public const string CarpetaSalida = "Assets/Arte/Zonas/";
        public const string RutaInforme = "Logs/t1340-zonas.txt";
        public const string RutaJugador = "Assets/_Proyecto/Personaje/Prefabs/JugadorAPie.prefab";

        private const int TriangulosParaLod = 5000;
        private const float MedidaTexturaGrande = 4f;
        private const int TamanoTexturaGrande = 2048;
        private const int TamanoTexturaChica = 1024;
        private const int TamanoMetalSuave = 1024;
        private const float SuavidadMaterial = 0.8f;
        private const float MetalicoMaterial = 1f;
        private const float AlturaBase = 0.6f;
        private const float TroncoDesde = 1.2f;
        private const float TroncoHasta = 2.2f;
        private const float RadioTroncoMinimo = 0.06f;
        private const float RadioTroncoMaximo = 0.6f;
        private const float SeparacionJugador = 0.8f;
        private const float IntensidadLuz = 1f;
        private static readonly Vector3 DireccionCamara = new Vector3(0.6f, 0.35f, -1f);

        public static readonly EntradaZonaT1340[] Entradas =
        {
            new EntradaZonaT1340("AR01_Arbol", ArteT250.Eje.Alto, 5f, TipoColliderZona.Poste, 3f),
            new EntradaZonaT1340("AR02_Arbol", ArteT250.Eje.Alto, 8f, TipoColliderZona.Poste, 3.5f),
            new EntradaZonaT1340("AR03_Palmera", ArteT250.Eje.Alto, 9f, TipoColliderZona.Poste, 8f),
            new EntradaZonaT1340("AU01_Luminaria", ArteT250.Eje.Alto, 11f, TipoColliderZona.Poste),
            new EntradaZonaT1340("CC01_CasaCerro", ArteT250.Eje.Alto, 6f, TipoColliderZona.Malla),
            new EntradaZonaT1340("CC02_CasaCerro", ArteT250.Eje.Alto, 6f, TipoColliderZona.Malla),
            new EntradaZonaT1340("CC04_Tendedero", ArteT250.Eje.Alto, 2.2f, TipoColliderZona.Malla),
            new EntradaZonaT1340("CC05_Lavadero", ArteT250.Eje.Alto, 1.2f, TipoColliderZona.Caja),
            new EntradaZonaT1340("CH01_Catedral", ArteT250.Eje.Alto, 30f, TipoColliderZona.Malla),
            new EntradaZonaT1340("CH02_EdificioColonial", ArteT250.Eje.Alto, 9f, TipoColliderZona.Malla),
            new EntradaZonaT1340("CH04_FarolColonial", ArteT250.Eje.Alto, 1.3f, TipoColliderZona.Caja), // farol de pared con brazo, no de piso
            new EntradaZonaT1340("IN01_Nave", ArteT250.Eje.Alto, 10f, TipoColliderZona.Malla),
            new EntradaZonaT1340("IN02_Chatarra", ArteT250.Eje.Alto, 4f, TipoColliderZona.Malla),
            new EntradaZonaT1340("IN03_Tanques", ArteT250.Eje.Alto, 8f, TipoColliderZona.Malla),
            new EntradaZonaT1340("LO01_CasaModerna", ArteT250.Eje.Alto, 7f, TipoColliderZona.Malla),
            new EntradaZonaT1340("LO02_CasaModerna", ArteT250.Eje.Alto, 7f, TipoColliderZona.Malla),
            new EntradaZonaT1340("LO03_CasaModerna", ArteT250.Eje.Alto, 7f, TipoColliderZona.Malla),
            new EntradaZonaT1340("LO04_Caseta", ArteT250.Eje.Alto, 3f, TipoColliderZona.Malla),
            new EntradaZonaT1340("LO05_Camara", ArteT250.Eje.Alto, 2.8f, TipoColliderZona.Poste),
            new EntradaZonaT1340("LO06_Arbusto", ArteT250.Eje.Alto, 1.2f, TipoColliderZona.Caja),
            new EntradaZonaT1340("ML01_Farol", ArteT250.Eje.Alto, 4.5f, TipoColliderZona.Poste),
            new EntradaZonaT1340("ML02_Lancha", ArteT250.Eje.Largo, 6f, TipoColliderZona.Malla),
            new EntradaZonaT1340("ML03_PuestoMariscos", ArteT250.Eje.Alto, 2.6f, TipoColliderZona.Malla),
            new EntradaZonaT1340("ML04_Faro", ArteT250.Eje.Alto, 18f, TipoColliderZona.Malla),
            new EntradaZonaT1340("PU01_Grua", ArteT250.Eje.Alto, 40f, TipoColliderZona.Malla),
            new EntradaZonaT1340("PU02_Barco", ArteT250.Eje.Largo, 120f, TipoColliderZona.Malla),
            new EntradaZonaT1340("PU03_Bodega", ArteT250.Eje.Alto, 8f, TipoColliderZona.Malla),
            new EntradaZonaT1340("PU04_Bodega", ArteT250.Eje.Alto, 8f, TipoColliderZona.Malla),
            new EntradaZonaT1340("PU05_Montacargas", ArteT250.Eje.Alto, 2.2f, TipoColliderZona.Caja),
            new EntradaZonaT1340("PU06_Pallets", ArteT250.Eje.Alto, 1.6f, TipoColliderZona.Caja),
            new EntradaZonaT1340("PZ01_Banca", ArteT250.Eje.Largo, 1.8f, TipoColliderZona.Caja),
            new EntradaZonaT1340("PZ02_Kiosco", ArteT250.Eje.Alto, 5.5f, TipoColliderZona.Malla),
            new EntradaZonaT1340("PZ03_Jardinera", ArteT250.Eje.Largo, 1.8f, TipoColliderZona.Caja),
        };

        public static readonly StringBuilder Informe = new StringBuilder();

        public static string RutaCarpeta(string nombre)
        {
            return CarpetaSalida + nombre.Substring(0, 2) + "/" + nombre;
        }

        public static string RutaPrefab(string nombre)
        {
            return CarpetaSalida + nombre.Substring(0, 2) + "/" + nombre + ".prefab";
        }

        public static string RutaFbx(string nombre)
        {
            return RutaCarpeta(nombre) + "/" + nombre + ".fbx";
        }

        public static string RutaMaterial(string nombre)
        {
            return RutaCarpeta(nombre) + "/" + nombre + ".mat";
        }

        private static string RutaTextura(string nombre, string sufijo)
        {
            return RutaCarpeta(nombre) + "/" + nombre + sufijo;
        }

        public static void ImportarYCapturar()
        {
            Importar();
            Capturar();
        }

        // ------------------------------------------------------------------ importar

        [MenuItem("Mecha Cyberpunk/T-1340/Importar props de zonas")]
        public static void Importar()
        {
            Informe.Clear();
            foreach (EntradaZonaT1340 e in Entradas)
            {
                Desempacar(e);
            }
            AssetDatabase.Refresh();
            foreach (EntradaZonaT1340 e in Entradas)
            {
                if (!File.Exists(RutaFbx(e.Nombre)))
                {
                    continue;
                }
                ConfigurarImportadores(e);
                Material material = CrearMaterial(e);
                Informe.AppendLine(CrearPrefab(e, material));
            }
            AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Logs");
            File.WriteAllText(RutaInforme, Informe.ToString());
            Debug.Log("[Arte] T-1340 importados:\n" + Informe);
        }

        private static void Desempacar(EntradaZonaT1340 e)
        {
            string zip = CarpetaZips + e.Nombre + ".zip";
            if (!File.Exists(zip))
            {
                Informe.AppendLine(e.Nombre + "\tFALTA ZIP " + zip);
                return;
            }
            Directory.CreateDirectory(RutaCarpeta(e.Nombre));
            byte[] metalico = null;
            byte[] rugosidad = null;
            string rutaMapa = RutaTextura(e.Nombre, "_metalsuave.png");
            using (ZipArchive z = ZipFile.OpenRead(zip))
            {
                foreach (ZipArchiveEntry a in z.Entries)
                {
                    string n = a.Name.ToLowerInvariant();
                    string destino = null;
                    if (n.EndsWith(".fbx"))
                    {
                        destino = RutaFbx(e.Nombre);
                    }
                    else if (n.EndsWith("_0_0.jpg"))
                    {
                        destino = RutaTextura(e.Nombre, "_basecolor.jpg");
                    }
                    else if (n.EndsWith("_0_3.jpg"))
                    {
                        destino = RutaTextura(e.Nombre, "_normal.jpg");
                    }
                    else if (n.Contains("metallic") && n.EndsWith(".jpg") && !File.Exists(rutaMapa))
                    {
                        metalico = LeerEntrada(a);
                    }
                    else if (n.Contains("roughness") && n.EndsWith(".jpg") && !File.Exists(rutaMapa))
                    {
                        rugosidad = LeerEntrada(a);
                    }
                    if (destino == null || (File.Exists(destino) && new FileInfo(destino).Length == a.Length))
                    {
                        continue;
                    }
                    a.ExtractToFile(destino, true);
                }
            }
            if (File.Exists(rutaMapa))
            {
                return;
            }
            if (metalico == null || rugosidad == null)
            {
                Informe.AppendLine(e.Nombre + "\tFALTA metallic o roughness en el zip");
                return;
            }
            File.WriteAllBytes(rutaMapa, MapaMetalSuave(metalico, rugosidad));
        }

        private static byte[] LeerEntrada(ZipArchiveEntry a)
        {
            using (Stream s = a.Open())
            using (MemoryStream m = new MemoryStream())
            {
                s.CopyTo(m);
                return m.ToArray();
            }
        }

        /// <summary>R=G=B=metálico, A=1-rugosidad, a 1024².</summary>
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
                    float v = (y + 0.5f) / TamanoMetalSuave;
                    float met = m.GetPixelBilinear(u, v).r;
                    float rug = r.GetPixelBilinear(u, v).r;
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

        private static void ConfigurarImportadores(EntradaZonaT1340 e)
        {
            int tamano = e.Medida >= MedidaTexturaGrande ? TamanoTexturaGrande : TamanoTexturaChica;
            foreach (string sufijo in new[] { "_basecolor.jpg", "_normal.jpg", "_metalsuave.png" })
            {
                string ruta = RutaTextura(e.Nombre, sufijo);
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

            string rutaFbx = RutaFbx(e.Nombre);
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
            if (e.TipoCollider == TipoColliderZona.Poste && !imp.isReadable)
            {
                imp.isReadable = true; // los Poste leen los vértices para ubicar base y tronco
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

        private static int Triangulos(GameObject fbx)
        {
            int total = 0;
            foreach (MeshFilter f in fbx.GetComponentsInChildren<MeshFilter>(true))
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

        private static Material CrearMaterial(EntradaZonaT1340 e)
        {
            string ruta = RutaMaterial(e.Nombre);
            Material material = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, ruta);
            }
            Texture2D color = AssetDatabase.LoadAssetAtPath<Texture2D>(RutaTextura(e.Nombre, "_basecolor.jpg"));
            Texture2D normal = AssetDatabase.LoadAssetAtPath<Texture2D>(RutaTextura(e.Nombre, "_normal.jpg"));
            Texture2D metalSuave = AssetDatabase.LoadAssetAtPath<Texture2D>(RutaTextura(e.Nombre, "_metalsuave.png"));
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

        private static string CrearPrefab(EntradaZonaT1340 e, Material material)
        {
            GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>(RutaFbx(e.Nombre));
            if (fbx == null)
            {
                return e.Nombre + "\tFALTA FBX";
            }
            GameObject raiz = new GameObject(e.Nombre);
            GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(fbx);
            visual.name = "Visual";
            visual.transform.SetParent(raiz.transform, false);
            foreach (Renderer r in visual.GetComponentsInChildren<Renderer>(true))
            {
                r.sharedMaterials = Enumerable.Repeat(material, Mathf.Max(1, r.sharedMaterials.Length)).ToArray();
            }

            Bounds original = ArteT250.Limites(raiz);
            float actual = e.Eje == ArteT250.Eje.Alto ? original.size.y : Mathf.Max(original.size.x, original.size.z);
            float escala = actual > 0.0001f ? e.Medida / actual : 1f;
            visual.transform.localScale = Vector3.one * escala;
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
                case TipoColliderZona.Malla:
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
                default:
                    CrearColisionPoste(e, raiz, visual, final);
                    break;
            }

            string rutaPrefab = RutaPrefab(e.Nombre);
            Directory.CreateDirectory(Path.GetDirectoryName(rutaPrefab));
            PrefabUtility.SaveAsPrefabAsset(raiz, rutaPrefab);
            Object.DestroyImmediate(raiz);
            ModelImporter imp = (ModelImporter)AssetImporter.GetAtPath(RutaFbx(e.Nombre));
            return string.Format("{0}\toriginal {1:F2}x{2:F2}x{3:F2}\tescala {4:F3}\tfinal {5:F2}x{6:F2}x{7:F2}\ttriangulos {8}\tcollider {9}\tLOD {10}",
                e.Nombre, original.size.x, original.size.y, original.size.z, escala, final.size.x, final.size.y, final.size.z,
                Triangulos(fbx), e.TipoCollider, imp.generateMeshLods ? "si" : "no");
        }

        /// <summary>Hijo "Colision": BoxCollider "base" con lo que hay bajo 0.6 m y CapsuleCollider del tronco (vértices entre 1.2 y 2.2 m).</summary>
        private static void CrearColisionPoste(EntradaZonaT1340 e, GameObject raiz, GameObject visual, Bounds final)
        {
            List<Vector3> vertices = new List<Vector3>();
            foreach (MeshFilter f in visual.GetComponentsInChildren<MeshFilter>(true))
            {
                if (f.sharedMesh == null)
                {
                    continue;
                }
                Matrix4x4 m = f.transform.localToWorldMatrix; // la raíz está en el origen: mundo = espacio de la raíz
                foreach (Vector3 v in f.sharedMesh.vertices)
                {
                    vertices.Add(m.MultiplyPoint3x4(v));
                }
            }

            GameObject colision = new GameObject("Colision");
            colision.transform.SetParent(raiz.transform, false);

            Bounds b = LimitesDe(vertices.Where(v => v.y < AlturaBase),
                new Bounds(new Vector3(final.center.x, AlturaBase * 0.5f, final.center.z), new Vector3(RadioTroncoMinimo * 2f, AlturaBase, RadioTroncoMinimo * 2f)));
            GameObject objetoBase = new GameObject("base");
            objetoBase.transform.SetParent(colision.transform, false);
            BoxCollider caja = objetoBase.AddComponent<BoxCollider>();
            caja.center = b.center;
            caja.size = b.size;

            Bounds t = LimitesDe(vertices.Where(v => v.y >= TroncoDesde && v.y <= TroncoHasta), b);
            float radio = Mathf.Clamp((t.extents.x + t.extents.z) * 0.5f, RadioTroncoMinimo, RadioTroncoMaximo);
            float alto = e.AltoTronco > 0f ? e.AltoTronco : final.size.y;
            GameObject objetoTronco = new GameObject("tronco");
            objetoTronco.transform.SetParent(colision.transform, false);
            CapsuleCollider capsula = objetoTronco.AddComponent<CapsuleCollider>();
            capsula.direction = 1;
            capsula.radius = radio;
            capsula.height = Mathf.Max(alto, radio * 2f);
            capsula.center = new Vector3(t.center.x, capsula.height * 0.5f, t.center.z);
        }

        private static Bounds LimitesDe(IEnumerable<Vector3> puntos, Bounds siVacio)
        {
            bool primero = true;
            Bounds b = siVacio;
            foreach (Vector3 p in puntos)
            {
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
            return b;
        }

        // ------------------------------------------------------------------ capturas

        /// <summary>docs/capturas/T-1340-&lt;Nombre&gt;.png: el prop con el jugador a su lado (x = límites.max.x + 0.8), desde una diagonal frontal.</summary>
        [MenuItem("Mecha Cyberpunk/T-1340/Capturar props de zonas")]
        public static void Capturar()
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
            GameObject jugadorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaJugador);
            StringBuilder resumen = new StringBuilder();
            foreach (EntradaZonaT1340 e in Entradas)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab(e.Nombre));
                if (prefab == null)
                {
                    resumen.AppendLine(e.Nombre + "\tsin captura: falta el prefab");
                    continue;
                }
                GameObject prop = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                Bounds b = ArteT250.Limites(prop);
                GameObject jugador = null;
                if (jugadorPrefab != null)
                {
                    jugador = (GameObject)PrefabUtility.InstantiatePrefab(jugadorPrefab);
                    jugador.transform.position = new Vector3(b.max.x + SeparacionJugador, 0f, 0f);
                    jugador.transform.rotation = Quaternion.LookRotation(new Vector3(DireccionCamara.x, 0f, DireccionCamara.z));
                    Bounds bj = ArteT250.Limites(jugador);
                    resumen.AppendLine(e.Nombre + "\talto limites jugador " + bj.size.y.ToString("F2"));
                    b.Encapsulate(bj);
                }
                ArteT250.Encuadrar(camara, b, DireccionCamara);
                ArteT250.Guardar(camara, Path.Combine(ArteT250.CarpetaCapturas, "T-1340-" + e.Nombre + ".png"));
                Object.DestroyImmediate(prop);
                if (jugador != null)
                {
                    Object.DestroyImmediate(jugador);
                }
            }
            Object.DestroyImmediate(camaraObjeto);
            Directory.CreateDirectory("Logs");
            File.AppendAllText(RutaInforme, "\nCapturas:\n" + resumen);
            Debug.Log("[Arte] T-1340 capturas:\n" + resumen);
        }
    }
}
