using System.IO;
using System.IO.Compression;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-1499: importa MA01_Maletin (zip de Tripo en imageassets/modelos3d/) a Assets/Arte/Objetos/MA01_Maletin/ y arma el prefab
    /// que usa el atraco (MaletinAsalto.Crear lo carga de Resources). Largo real 0.45 m, acostado (el eje más delgado hacia arriba),
    /// con la base en el origen y un BoxCollider. Idempotente.
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ImportarMaletinT1499.Importar
    /// </summary>
    public static class ImportarMaletinT1499
    {
        public const string Zip = "imageassets/modelos3d/MA01_Maletin.zip";
        public const string Carpeta = "Assets/Arte/Objetos/MA01_Maletin/";
        public const string Nombre = "MA01_Maletin";
        public const string RutaFbx = Carpeta + Nombre + ".fbx";
        public const string RutaMaterial = Carpeta + Nombre + ".mat";
        public const string RutaPrefabArte = "Assets/Arte/Objetos/" + Nombre + ".prefab";
        /// <summary>Copia en Resources para que el atraco la cargue en juego (Progresion no referencia Arte).</summary>
        public const string RutaPrefabResources = "Assets/_Proyecto/Progresion/Prologo/Resources/MaletinAsalto.prefab";
        public const float LargoMetros = 0.45f;

        [MenuItem("Mecha Cyberpunk/T-1499/Importar maletín MA01")]
        public static void Importar()
        {
            Desempacar();
            AssetDatabase.Refresh();
            ConfigurarImportadores();
            Material material = CrearMaterial();
            CrearPrefab(material);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Arte] T-1499 maletín MA01 importado");
        }

        /// <summary>imageassets no se versiona: en un worktree está en la carpeta principal del repo (tres niveles arriba de .claude/worktrees/x).</summary>
        private static string BuscarZip()
        {
            foreach (string candidato in new[] { Zip, "../../../" + Zip })
            {
                if (File.Exists(candidato))
                {
                    return candidato;
                }
            }
            return null;
        }

        private static string Ruta(string sufijo) => Carpeta + Nombre + sufijo;

        private static void Desempacar()
        {
            string zip = BuscarZip();
            if (zip == null)
            {
                Debug.LogError("[Arte] T-1499: falta " + Zip);
                return;
            }
            Directory.CreateDirectory(Carpeta);
            byte[] metalico = null;
            byte[] rugosidad = null;
            string rutaMapa = Ruta("_metalsuave.png");
            using (ZipArchive z = ZipFile.OpenRead(zip))
            {
                foreach (ZipArchiveEntry a in z.Entries)
                {
                    string n = a.Name.ToLowerInvariant();
                    string destino = null;
                    if (n.EndsWith(".fbx"))
                    {
                        destino = RutaFbx;
                    }
                    else if (n.EndsWith("_0_0.jpg"))
                    {
                        destino = Ruta("_basecolor.jpg");
                    }
                    else if (n.EndsWith("_0_3.jpg"))
                    {
                        destino = Ruta("_normal.jpg");
                    }
                    else if (n.Contains("metallic") && n.EndsWith(".jpg") && !File.Exists(rutaMapa))
                    {
                        metalico = ImportarZonasT1340.LeerEntrada(a);
                    }
                    else if (n.Contains("roughness") && n.EndsWith(".jpg") && !File.Exists(rutaMapa))
                    {
                        rugosidad = ImportarZonasT1340.LeerEntrada(a);
                    }
                    if (destino == null || (File.Exists(destino) && new FileInfo(destino).Length == a.Length))
                    {
                        continue;
                    }
                    a.ExtractToFile(destino, true);
                }
            }
            if (!File.Exists(rutaMapa) && metalico != null && rugosidad != null)
            {
                File.WriteAllBytes(rutaMapa, ImportarZonasT1340.MapaMetalSuave(metalico, rugosidad));
            }
        }

        private static void ConfigurarImportadores()
        {
            foreach (string sufijo in new[] { "_basecolor.jpg", "_normal.jpg", "_metalsuave.png" })
            {
                string ruta = Ruta(sufijo);
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
                bool cambio = t.maxTextureSize != 1024;
                t.maxTextureSize = 1024;
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
            ModelImporter imp = AssetImporter.GetAtPath(RutaFbx) as ModelImporter;
            if (imp != null && (imp.materialImportMode != ModelImporterMaterialImportMode.None || imp.importCameras || imp.importLights))
            {
                imp.materialImportMode = ModelImporterMaterialImportMode.None;
                imp.importCameras = false;
                imp.importLights = false;
                imp.SaveAndReimport();
            }
        }

        private static Material CrearMaterial()
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(RutaMaterial);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, RutaMaterial);
            }
            Texture2D color = AssetDatabase.LoadAssetAtPath<Texture2D>(Ruta("_basecolor.jpg"));
            Texture2D normal = AssetDatabase.LoadAssetAtPath<Texture2D>(Ruta("_normal.jpg"));
            Texture2D metalSuave = AssetDatabase.LoadAssetAtPath<Texture2D>(Ruta("_metalsuave.png"));
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
            material.SetFloat("_Smoothness", 0.8f);
            material.SetFloat("_Metallic", 1f);
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void CrearPrefab(Material material)
        {
            GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>(RutaFbx);
            if (fbx == null)
            {
                Debug.LogError("[Arte] T-1499: no cargó " + RutaFbx);
                return;
            }
            GameObject raiz = new GameObject("MaletinAsalto");
            GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(fbx);
            visual.name = "Visual";
            visual.transform.SetParent(raiz.transform, false);
            foreach (Renderer r in visual.GetComponentsInChildren<Renderer>(true))
            {
                Material[] ms = new Material[Mathf.Max(1, r.sharedMaterials.Length)];
                for (int i = 0; i < ms.Length; i++)
                {
                    ms[i] = material;
                }
                r.sharedMaterials = ms;
            }

            // Acostado: el eje más delgado hacia arriba.
            Vector3 medidas = ArteT250.Limites(raiz).size;
            if (medidas.x <= medidas.y && medidas.x <= medidas.z)
            {
                visual.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            }
            else if (medidas.z <= medidas.x && medidas.z <= medidas.y)
            {
                visual.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            }
            Bounds girado = ArteT250.Limites(raiz);
            float largo = Mathf.Max(girado.size.x, girado.size.z);
            visual.transform.localScale = Vector3.one * (largo > 0.0001f ? LargoMetros / largo : 1f);
            Bounds escalado = ArteT250.Limites(raiz);
            visual.transform.localPosition = new Vector3(-escalado.center.x, -escalado.min.y, -escalado.center.z);
            Bounds final = ArteT250.Limites(raiz);

            BoxCollider caja = raiz.AddComponent<BoxCollider>();
            caja.center = final.center;
            caja.size = final.size;

            Directory.CreateDirectory(Path.GetDirectoryName(RutaPrefabArte));
            Directory.CreateDirectory(Path.GetDirectoryName(RutaPrefabResources));
            PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefabArte);
            PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefabResources);
            Object.DestroyImmediate(raiz);
            Debug.Log(string.Format("[Arte] T-1499 MA01 original {0:F2}x{1:F2}x{2:F2} final {3:F2}x{4:F2}x{5:F2}",
                medidas.x, medidas.y, medidas.z, final.size.x, final.size.y, final.size.z));
        }
    }
}
