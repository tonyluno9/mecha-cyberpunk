using System.Collections.Generic;
using System.IO;
using System.Linq;
using MechaCyberpunk.VR.Manos;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace MechaCyberpunk.VR.Editor
{
    /// <summary>
    /// VR40 (T-manos-vr-24nov): prepara las manos nuevas MN01 sin tocar los FBX ni las texturas de Blender; solo sus import settings y assets
    /// nuevos hechos por código:
    /// <list type="bullet">
    /// <item>Texturas: BaseColor sRGB; Normal como Normal map; Metallic y Roughness lineales (legibles, para combinarlas).</item>
    /// <item>MN01_MetallicSmoothness.png (nueva, en VR/Manos/Materiales): R = metálico, A = 1 − roughness (URP Lit toma la suavidad del alfa).</item>
    /// <item>FBX: escala 1 (se corrige si la mano no mide ~0.2–0.6 m), Generic, sin materiales importados, clips con el nombre limpio
    /// (sin "Armature|") y Loop Time solo en Mano_AbrirCerrar.</item>
    /// <item>MN01_ManosVR.mat (URP Lit con las 4 texturas) y MN01_ManosVR_Protesis.mat (igual, con emisión cian enmascarada por el mapa metálico).</item>
    /// <item>Prefabs VR/Manos/Resources/ManosVR/MN01_ManoVR_R y _L: el modelo desempacado, sin Animator, con <see cref="ManoMN01VR"/>, sus 17
    /// huesos y las 9 poses muestreadas de los clips (los usa <see cref="PunosVisiblesVR"/>).</item>
    /// </list>
    /// Idempotente. Batchmode: -executeMethod MechaCyberpunk.VR.Editor.ImportarManosVR40.Aplicar
    /// </summary>
    public static class ImportarManosVR40
    {
        public const string CarpetaArte = "Assets/Arte/Personajes/PJ01/ManosVR";
        public const string FbxDerecha = CarpetaArte + "/MN01_ManosVR_R.fbx";
        public const string FbxIzquierda = CarpetaArte + "/MN01_ManosVR_L.fbx";
        public const string TexturaBase = CarpetaArte + "/Texturas/MN01_BaseColor.jpg";
        public const string TexturaNormal = CarpetaArte + "/Texturas/MN01_Normal.jpg";
        public const string TexturaMetalica = CarpetaArte + "/Texturas/MN01_Metallic.jpg";
        public const string TexturaRugosidad = CarpetaArte + "/Texturas/MN01_Roughness.jpg";
        public const string CarpetaMateriales = "Assets/_Proyecto/VR/Manos/Materiales";
        public const string TexturaMetalSuave = CarpetaMateriales + "/MN01_MetallicSmoothness.png";
        public const string RutaMaterial = CarpetaMateriales + "/MN01_ManosVR.mat";
        public const string RutaMaterialProtesis = CarpetaMateriales + "/MN01_ManosVR_Protesis.mat";
        public const string CarpetaPrefabs = "Assets/_Proyecto/VR/Manos/Resources/ManosVR";
        public const string PrefabDerecha = CarpetaPrefabs + "/MN01_ManoVR_R.prefab";
        public const string PrefabIzquierda = CarpetaPrefabs + "/MN01_ManoVR_L.prefab";

        private const float MedidaMinima = 0.12f;
        private const float MedidaMaxima = 1.2f;
        private const float EmisionProtesis = 1.2f;

        [MenuItem("Mecha Cyberpunk/VR/Importar manos MN01 (VR40)")]
        public static void Aplicar()
        {
            int errores = 0;
            ImportarTexturas();
            Texture2D metalSuave = CrearMetalSuave();
            errores += ImportarFbx(FbxDerecha) ? 0 : 1;
            errores += ImportarFbx(FbxIzquierda) ? 0 : 1;
            Material normal = CrearMaterial(RutaMaterial, metalSuave, false);
            Material protesis = CrearMaterial(RutaMaterialProtesis, metalSuave, true);
            errores += CrearPrefab(FbxDerecha, PrefabDerecha, true, normal, protesis) ? 0 : 1;
            errores += CrearPrefab(FbxIzquierda, PrefabIzquierda, false, normal, protesis) ? 0 : 1;
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[VR] VR40: manos MN01 importadas" + (errores > 0 ? " con " + errores + " error(es) (ver arriba)." : ": FBX, material, prótesis y 2 prefabs listos."));
        }

        // ---- Texturas ----

        private static void ImportarTexturas()
        {
            Textura(TexturaBase, TextureImporterType.Default, true, false);
            Textura(TexturaNormal, TextureImporterType.NormalMap, false, false);
            Textura(TexturaMetalica, TextureImporterType.Default, false, true);
            Textura(TexturaRugosidad, TextureImporterType.Default, false, true);
        }

        private static void Textura(string ruta, TextureImporterType tipo, bool srgb, bool legible)
        {
            AssetDatabase.ImportAsset(ruta, ImportAssetOptions.ForceSynchronousImport);
            if (!(AssetImporter.GetAtPath(ruta) is TextureImporter imp))
            {
                Debug.LogError("[VR] VR40: no está la textura " + ruta + ".");
                return;
            }
            imp.textureType = tipo;
            imp.sRGBTexture = tipo != TextureImporterType.NormalMap && srgb;
            imp.isReadable = legible;
            imp.mipmapEnabled = true;
            imp.SaveAndReimport();
        }

        // R = G = B = metálico, A = suavidad (1 - rugosidad). Si no miden igual, la rugosidad se muestrea con bilineal.
        private static Texture2D CrearMetalSuave()
        {
            var metal = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturaMetalica);
            var rugosidad = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturaRugosidad);
            if (metal == null || rugosidad == null || !metal.isReadable || !rugosidad.isReadable)
            {
                Debug.LogError("[VR] VR40: faltan Metallic o Roughness legibles; el material queda sin mapa metálico.");
                return null;
            }
            int ancho = metal.width;
            int alto = metal.height;
            var salida = new Texture2D(ancho, alto, TextureFormat.RGBA32, false, true);
            Color[] m = metal.GetPixels();
            var pixeles = new Color[m.Length];
            bool igual = rugosidad.width == ancho && rugosidad.height == alto;
            Color[] r = igual ? rugosidad.GetPixels() : null;
            for (int y = 0; y < alto; y++)
            {
                for (int x = 0; x < ancho; x++)
                {
                    int i = y * ancho + x;
                    float rug = igual ? r[i].r : rugosidad.GetPixelBilinear((x + 0.5f) / ancho, (y + 0.5f) / alto).r;
                    float met = m[i].r;
                    pixeles[i] = new Color(met, met, met, 1f - rug);
                }
            }
            salida.SetPixels(pixeles);
            salida.Apply();
            Directory.CreateDirectory(CarpetaMateriales);
            File.WriteAllBytes(TexturaMetalSuave, salida.EncodeToPNG());
            Object.DestroyImmediate(salida);
            AssetDatabase.ImportAsset(TexturaMetalSuave, ImportAssetOptions.ForceSynchronousImport);
            if (AssetImporter.GetAtPath(TexturaMetalSuave) is TextureImporter imp)
            {
                imp.textureType = TextureImporterType.Default;
                imp.sRGBTexture = false;
                imp.alphaSource = TextureImporterAlphaSource.FromInput;
                imp.alphaIsTransparency = false;
                imp.mipmapEnabled = true;
                imp.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(TexturaMetalSuave);
        }

        // ---- FBX ----

        private static bool ImportarFbx(string ruta)
        {
            AssetDatabase.ImportAsset(ruta, ImportAssetOptions.ForceSynchronousImport);
            if (!(AssetImporter.GetAtPath(ruta) is ModelImporter imp))
            {
                Debug.LogError("[VR] VR40: no está el FBX " + ruta + ".");
                return false;
            }
            imp.globalScale = 1f;
            imp.useFileScale = true;
            imp.animationType = ModelImporterAnimationType.Generic;
            imp.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            imp.importAnimation = true;
            imp.materialImportMode = ModelImporterMaterialImportMode.None;
            imp.importBlendShapes = false;
            imp.importCameras = false;
            imp.importLights = false;
            imp.SaveAndReimport();

            // Escala: la mano con su antebrazo mide ~0.47 m; si sale en cm (o en km) se corrige el factor.
            float medida = Medida(ruta);
            if (medida > MedidaMaxima || (medida > 0f && medida < MedidaMinima))
            {
                float factor = medida > MedidaMaxima ? 0.01f : 100f;
                imp.globalScale = factor;
                imp.SaveAndReimport();
                Debug.LogWarning("[VR] VR40: " + Path.GetFileName(ruta) + " medía " + medida.ToString("0.###") + " m; globalScale = " + factor + " (ahora mide "
                    + Medida(ruta).ToString("0.###") + " m).");
            }

            // Clips: nombre limpio, Loop Time solo en Mano_AbrirCerrar.
            ModelImporterClipAnimation[] clips = imp.defaultClipAnimations;
            if (clips == null || clips.Length == 0)
            {
                Debug.LogError("[VR] VR40: " + ruta + " no trae clips.");
                return false;
            }
            foreach (ModelImporterClipAnimation c in clips)
            {
                c.name = ReglaManosMN01VR.NombreLimpio(c.takeName);
                c.loopTime = c.name == ReglaManosMN01VR.ClipAbrirCerrar;
                c.wrapMode = c.loopTime ? WrapMode.Loop : WrapMode.ClampForever;
            }
            imp.clipAnimations = clips;
            imp.SaveAndReimport();

            string[] nombres = Clips(ruta).Select(c => c.name).ToArray();
            string[] faltan = ReglaManosMN01VR.Clips.Where(n => !nombres.Contains(n)).ToArray();
            if (faltan.Length > 0)
            {
                Debug.LogWarning("[VR] VR40: a " + Path.GetFileName(ruta) + " le faltan los clips " + string.Join(", ", faltan) + " (trae: " + string.Join(", ", nombres) + ").");
            }
            Debug.Log("[VR] VR40: " + Path.GetFileName(ruta) + " Generic, " + nombres.Length + " clips, mide " + Medida(ruta).ToString("0.###") + " m.");
            return true;
        }

        private static float Medida(string ruta)
        {
            var modelo = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
            if (modelo == null)
            {
                return 0f;
            }
            var instancia = (GameObject)PrefabUtility.InstantiatePrefab(modelo);
            float medida = 0f;
            bool hay = false;
            Bounds caja = default;
            foreach (Renderer r in instancia.GetComponentsInChildren<Renderer>())
            {
                if (!hay)
                {
                    caja = r.bounds;
                    hay = true;
                }
                else
                {
                    caja.Encapsulate(r.bounds);
                }
            }
            if (hay)
            {
                medida = Mathf.Max(caja.size.x, Mathf.Max(caja.size.y, caja.size.z));
            }
            Object.DestroyImmediate(instancia);
            return medida;
        }

        /// <summary>Los clips del FBX (sin los de vista previa).</summary>
        public static List<AnimationClip> Clips(string ruta)
        {
            return AssetDatabase.LoadAllAssetsAtPath(ruta).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview__")).ToList();
        }

        // ---- Materiales ----

        private static Material CrearMaterial(string ruta, Texture2D metalSuave, bool protesis)
        {
            Shader lit = Shader.Find("Universal Render Pipeline/Lit");
            if (lit == null)
            {
                Debug.LogError("[VR] VR40: no está el shader URP Lit.");
                return null;
            }
            Directory.CreateDirectory(CarpetaMateriales);
            var mat = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (mat == null)
            {
                mat = new Material(lit);
                AssetDatabase.CreateAsset(mat, ruta);
            }
            mat.shader = lit;
            var baseColor = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturaBase);
            var normal = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturaNormal);
            mat.SetTexture("_BaseMap", baseColor);
            mat.SetTexture("_MainTex", baseColor);
            mat.SetColor("_BaseColor", Color.white);
            mat.SetFloat("_Surface", 0f);
            mat.SetFloat("_WorkflowMode", 1f); // metálico
            if (normal != null)
            {
                mat.SetTexture("_BumpMap", normal);
                mat.SetFloat("_BumpScale", 1f);
                mat.EnableKeyword("_NORMALMAP");
            }
            if (metalSuave != null)
            {
                mat.SetTexture("_MetallicGlossMap", metalSuave);
                mat.SetFloat("_Metallic", 1f);
                mat.SetFloat("_Smoothness", 1f); // la suavidad es la del alfa del mapa (1 - rugosidad)
                mat.SetFloat("_SmoothnessTextureChannel", 0f);
                mat.EnableKeyword("_METALLICSPECGLOSSMAP");
            }
            else
            {
                mat.SetFloat("_Metallic", 0.5f);
                mat.SetFloat("_Smoothness", 0.4f);
            }
            if (protesis)
            {
                // Emisión cian con el mapa metálico como máscara (las piezas de metal de la prótesis y el guante brillan); el pulso va en runtime.
                mat.SetTexture("_EmissionMap", metalSuave);
                mat.SetColor("_EmissionColor", ReglaManosMN01VR.ColorProtesis * EmisionProtesis);
                mat.EnableKeyword("_EMISSION");
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            else
            {
                mat.SetColor("_EmissionColor", Color.black);
                mat.DisableKeyword("_EMISSION");
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;
            }
            EditorUtility.SetDirty(mat);
            return mat;
        }

        // ---- Prefabs ----

        private static bool CrearPrefab(string fbx, string rutaPrefab, bool derecha, Material normal, Material protesis)
        {
            var modelo = AssetDatabase.LoadAssetAtPath<GameObject>(fbx);
            if (modelo == null)
            {
                Debug.LogError("[VR] VR40: no se pudo cargar " + fbx + ".");
                return false;
            }
            var instancia = (GameObject)PrefabUtility.InstantiatePrefab(modelo);
            PrefabUtility.UnpackPrefabInstance(instancia, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            instancia.name = Path.GetFileNameWithoutExtension(rutaPrefab);
            instancia.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            try
            {
                string[] nombres = ReglaManosMN01VR.Huesos(derecha);
                var huesos = new Transform[nombres.Length];
                var todos = instancia.GetComponentsInChildren<Transform>(true);
                var faltan = new List<string>();
                for (int i = 0; i < nombres.Length; i++)
                {
                    huesos[i] = todos.FirstOrDefault(t => t.name == nombres[i]);
                    if (huesos[i] == null)
                    {
                        faltan.Add(nombres[i]);
                    }
                }
                if (faltan.Count > 0)
                {
                    Debug.LogError("[VR] VR40: a " + Path.GetFileName(fbx) + " le faltan los huesos " + string.Join(", ", faltan) + ".");
                    return false;
                }

                // Cada clip de pose se muestrea en su primer frame y se guardan las rotaciones y posiciones locales de los 17 huesos.
                var poses = new List<PoseManoMN01>();
                foreach (AnimationClip clip in Clips(fbx))
                {
                    clip.SampleAnimation(instancia, 0f);
                    poses.Add(new PoseManoMN01
                    {
                        Nombre = clip.name,
                        Rotaciones = huesos.Select(h => h.localRotation).ToArray(),
                        Posiciones = huesos.Select(h => h.localPosition).ToArray(),
                    });
                }
                PoseManoMN01 abierta = poses.FirstOrDefault(p => p.Nombre == ReglaManosMN01VR.ClipAbierta);
                if (abierta != null)
                {
                    for (int i = 0; i < huesos.Length; i++)
                    {
                        huesos[i].localRotation = abierta.Rotaciones[i];
                        huesos[i].localPosition = abierta.Posiciones[i];
                    }
                }

                foreach (Animator a in instancia.GetComponentsInChildren<Animator>(true))
                {
                    Object.DestroyImmediate(a);
                }
                Renderer[] mallas = instancia.GetComponentsInChildren<Renderer>(true);
                foreach (Renderer r in mallas)
                {
                    if (normal != null)
                    {
                        var lista = new Material[Mathf.Max(1, r.sharedMaterials.Length)];
                        for (int i = 0; i < lista.Length; i++)
                        {
                            lista[i] = normal;
                        }
                        r.sharedMaterials = lista;
                    }
                    r.shadowCastingMode = ShadowCastingMode.Off;
                }
                ManoMN01VR mano = instancia.GetComponent<ManoMN01VR>() ?? instancia.AddComponent<ManoMN01VR>();
                mano.Configurar(derecha, huesos, poses.ToArray(), mallas, normal, protesis);

                Directory.CreateDirectory(CarpetaPrefabs);
                PrefabUtility.SaveAsPrefabAsset(instancia, rutaPrefab, out bool ok);
                Debug.Log("[VR] VR40: " + rutaPrefab + (ok ? " guardado" : " NO se guardó") + ": " + huesos.Length + " huesos, " + poses.Count + " poses ("
                    + string.Join(", ", poses.Select(p => p.Nombre)) + "), " + mallas.Length + " malla(s).");
                return ok;
            }
            finally
            {
                Object.DestroyImmediate(instancia);
            }
        }
    }
}
