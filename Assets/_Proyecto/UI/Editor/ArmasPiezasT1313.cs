using System.Text;
using MechaCyberpunk.Personaje.Combate;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.UI.Editor
{
    /// <summary>
    /// T-1313: lo mismo que T-970 para el subfusil, el revólver, el rifle de plasma y el lanzagranadas (W05-W08): importa sus piezas de Blender,
    /// las pone como "Modelo" de cada prefab y da nombres de pieza al banco. Idempotente.
    /// Batchmode: -executeMethod MechaCyberpunk.UI.Editor.ArmasPiezasT1313.Todo
    /// </summary>
    public static class ArmasPiezasT1313
    {
        private const string Raiz = "Assets/Arte/Armas/";

        public static readonly string[][] Armas =
        {
            new[] { "W05_Subfusil", "W05" }, new[] { "W06_Revolver", "W06" }, new[] { "W07_RiflePlasma", "W07" }, new[] { "W08_Lanzagranadas", "W08" }
        };

        public static string Fbx(string[] a) => Raiz + a[0] + "/" + a[1] + "_Piezas.fbx";
        public static string Prefab(string[] a) => Raiz + a[0] + "/" + a[0] + ".prefab";

        public static void Todo()
        {
            Importar();
            ColocarPrefabs();
            ConfigurarBanco();
            AssetDatabase.SaveAssets();
        }

        public static void Importar()
        {
            Material corte = AssetDatabase.LoadAssetAtPath<Material>(Raiz + "Armas_CorteInterior.mat");
            foreach (string[] a in Armas)
            {
                AssetDatabase.ImportAsset(Fbx(a), ImportAssetOptions.ForceUpdate);
                ModelImporter imp = (ModelImporter)AssetImporter.GetAtPath(Fbx(a));
                imp.animationType = ModelImporterAnimationType.None;
                imp.importAnimation = false;
                imp.importCameras = false;
                imp.importLights = false;
                imp.isReadable = false;
                imp.useFileScale = true;
                imp.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
                imp.materialLocation = ModelImporterMaterialLocation.InPrefab;
                imp.SaveAndReimport();
                Material arma = AssetDatabase.LoadAssetAtPath<Material>(Raiz + a[0] + "/" + a[0] + ".mat");
                string todos = "";
                foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(Fbx(a)))
                {
                    todos += o.GetType().Name + ":" + o.name + " ";
                    if (o is Material m)
                    {
                        Material destino = m.name.StartsWith("Corte_Interior") ? corte : arma;
                        imp.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), m.name), destino);
                    }
                }
                imp.SaveAndReimport();
                Debug.Log("[T1313] " + a[1] + " sub-assets: " + todos);
            }
            AssetDatabase.SaveAssets();
        }

        public static void ColocarPrefabs()
        {
            StringBuilder informe = new StringBuilder();
            foreach (string[] a in Armas)
            {
                ArmasPiezasT970.ArmarPrefab(Prefab(a), Fbx(a), informe);
            }
            Debug.Log("[T1313]\n" + informe);
        }

        /// <summary>
        /// T-1314: las caras de corte de las piezas (Armas_CorteInterior) llevan una textura oscura lisa, para que no cuenten como
        /// material greybox sin arte (SinGreyboxT506Pruebas). El color base queda en blanco: el oscuro lo pone la textura.
        /// </summary>
        public static void TexturaCorte()
        {
            const string rutaTextura = "Assets/Arte/Armas/Armas_CorteInterior_Base.png";
            AssetDatabase.ImportAsset(rutaTextura, ImportAssetOptions.ForceUpdate);
            Texture2D textura = AssetDatabase.LoadAssetAtPath<Texture2D>(rutaTextura);
            Material corte = AssetDatabase.LoadAssetAtPath<Material>("Assets/Arte/Armas/Armas_CorteInterior.mat");
            if (textura == null || corte == null)
            {
                Debug.LogError("[UI] T-1314: falta la textura o el material del corte interior.");
                return;
            }
            corte.SetTexture("_BaseMap", textura);
            corte.SetColor("_BaseColor", Color.white);
            EditorUtility.SetDirty(corte);
            AssetDatabase.SaveAssets();
            Debug.Log("[UI] T-1314: Armas_CorteInterior con textura " + rutaTextura);
        }

        public static void ConfigurarBanco()
        {
            ConfigMenuArmas config = AssetDatabase.LoadAssetAtPath<ConfigMenuArmas>(CreadorMenuArmas.RutaConfig);
            SerializedObject so = new SerializedObject(config);
            SerializedProperty armas = so.FindProperty("armas");
            (ArmaAPie arma, string cargador, string mira, bool tieneMira)[] datos =
            {
                (ArmaAPie.Subfusil, "Cargador", "Mira", true), (ArmaAPie.Revolver, "Tambor", "", false),
                (ArmaAPie.Lanzagranadas, "Tambor", "", false), (ArmaAPie.RiflePlasma, "Celda", "Mira", true)
            };
            foreach (var d in datos)
            {
                for (int i = 0; i < armas.arraySize; i++)
                {
                    SerializedProperty e = armas.GetArrayElementAtIndex(i);
                    if (e.FindPropertyRelative("arma").intValue != (int)d.arma)
                    {
                        continue;
                    }
                    e.FindPropertyRelative("piezaCargador").stringValue = d.cargador;
                    e.FindPropertyRelative("piezaCanon").stringValue = "Canon";
                    e.FindPropertyRelative("piezaMira").stringValue = d.mira;
                    e.FindPropertyRelative("tieneMira").boolValue = d.tieneMira;
                }
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
        }

        /// <summary>Guarda docs/capturas/t-1313/&lt;id&gt;-armada.png y &lt;id&gt;-separada.png (las piezas separadas a mano) de cada arma.</summary>
        public static void Capturar()
        {
            UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene);
            System.IO.Directory.CreateDirectory("docs/capturas/t-1313");
            foreach (string[] a in Armas)
            {
                GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab(a)));
                Foto(a[1].ToLowerInvariant() + "-armada", go);
                Transform modelo = go.transform.Find("Modelo");
                foreach (Transform h in modelo)
                {
                    Vector3 mundo = h.name == "Canon" ? Vector3.forward * 0.12f : h.name == "Mira" ? Vector3.up * 0.1f : h.name == "Armazon" ? Vector3.zero : Vector3.down * 0.12f;
                    h.position += mundo;
                }
                Foto(a[1].ToLowerInvariant() + "-separada", go);
                Object.DestroyImmediate(go);
            }
        }

        private static void Foto(string nombre, GameObject objeto)
        {
            GameObject luz = new GameObject("Luz");
            Light l = luz.AddComponent<Light>();
            l.type = LightType.Directional;
            l.intensity = 1.2f;
            luz.transform.rotation = Quaternion.Euler(40f, -30f, 0f);
            Bounds b = new Bounds(objeto.transform.position, Vector3.zero);
            foreach (Renderer r in objeto.GetComponentsInChildren<Renderer>())
            {
                b.Encapsulate(r.bounds);
            }
            GameObject cam = new GameObject("Camara");
            Camera c = cam.AddComponent<Camera>();
            c.clearFlags = CameraClearFlags.SolidColor;
            c.backgroundColor = new Color(0.45f, 0.45f, 0.48f);
            c.fieldOfView = 30f;
            cam.transform.position = b.center + new Vector3(1f, 0.25f, 0f).normalized * (Mathf.Max(b.size.x, b.size.z) * 2.2f + 0.3f);
            cam.transform.LookAt(b.center);
            RenderTexture rt = new RenderTexture(1280, 720, 24);
            c.targetTexture = rt;
            c.Render();
            RenderTexture.active = rt;
            Texture2D tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            RenderTexture.active = null;
            System.IO.File.WriteAllBytes("docs/capturas/t-1313/" + nombre + ".png", tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(luz);
            Object.DestroyImmediate(cam);
        }
    }
}
