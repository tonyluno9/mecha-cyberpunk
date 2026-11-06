using System.IO;
using System.Linq;
using System.Text;
using MechaCyberpunk.Trafico.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-810 (noche 6-nov, sesión 2): más variedad en el tráfico mientras llegan V09-V11. Variantes de color de los carros civiles V02, V03 y
    /// V04: una copia de su material con otro tinte y una variante de prefab de su CarroTrafico (mismo carro, mismo comportamiento) en
    /// Assets/Arte/Vehiculos/Variantes/, que se agregan a TraficoCalle.prefabsCarro de Ciudad_Prologo. Llama a <see cref="ImportarS2n.MeterTodo"/>
    /// (que mete V09-V11 si ya llegaron sus zips). Idempotente. Hoja de vista previa en Logs/s2n-carros.png.
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ColocarS2nCarros.Colocar
    /// </summary>
    public static class ColocarS2nCarros
    {
        public const string Carpeta = "Assets/Arte/Vehiculos/Variantes/";

        /// <summary>Carro civil, nombre del color y tinte que multiplica su textura.</summary>
        public static readonly (string codigo, string color, Color tinte)[] Variantes =
        {
            ("V02", "Rojo", new Color(1f, 0.6f, 0.55f)),
            ("V02", "Azul", new Color(0.6f, 0.75f, 1f)),
            ("V03", "Verde", new Color(0.7f, 0.95f, 0.72f)),
            ("V03", "Gris", new Color(0.72f, 0.72f, 0.75f)),
            ("V04", "Naranja", new Color(1f, 0.78f, 0.5f)),
            ("V04", "Turquesa", new Color(0.6f, 0.95f, 0.95f)),
        };

        public static string RutaPrefab(string codigo, string color) => Carpeta + "CarroTrafico_" + codigo + "_" + color + ".prefab";
        public static string RutaMaterial(string codigo, string color) => Carpeta + codigo + "_" + color + ".mat";

        [MenuItem("Mecha Cyberpunk/S2n/8 Variedad de carros del trafico")]
        public static void Colocar()
        {
            ImportarS2n.MeterTodo();
            StringBuilder informe = ImportarS2n.Informe;
            Directory.CreateDirectory(Carpeta);
            foreach (var v in Variantes)
            {
                informe.AppendLine(CrearVariante(v.codigo, v.color, v.tinte));
            }
            AssetDatabase.SaveAssets();
            ImportarS2nCarros.AgregarAlTrafico(Variantes.Select(v => RutaPrefab(v.codigo, v.color)).ToArray());
            File.WriteAllText("Logs/s2n-carros.txt", informe.ToString());
            Debug.Log("[Arte] T-810 carros:\n" + informe);
            Hoja();
        }

        private static string CrearVariante(string codigo, string color, Color tinte)
        {
            Material original = AssetDatabase.LoadAssetAtPath<Material>("Assets/Arte/Vehiculos/" + codigo + "/" + codigo + ".mat");
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CreadorTraficoT511.RutaPrefab(codigo));
            if (original == null || prefab == null)
            {
                return codigo + " " + color + "\tFALTA material o CarroTrafico_" + codigo;
            }
            string rutaMaterial = RutaMaterial(codigo, color);
            Material m = AssetDatabase.LoadAssetAtPath<Material>(rutaMaterial);
            if (m == null)
            {
                m = new Material(original);
                AssetDatabase.CreateAsset(m, rutaMaterial);
            }
            else
            {
                m.CopyPropertiesFromMaterial(original);
            }
            m.SetColor("_BaseColor", tinte);
            EditorUtility.SetDirty(m);

            GameObject instancia = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            int cambiados = 0;
            foreach (Renderer r in instancia.GetComponentsInChildren<Renderer>(true))
            {
                Material[] mats = r.sharedMaterials;
                bool cambia = false;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] == original)
                    {
                        mats[i] = m;
                        cambia = true;
                    }
                }
                if (cambia)
                {
                    r.sharedMaterials = mats;
                    cambiados++;
                }
            }
            PrefabUtility.SaveAsPrefabAsset(instancia, RutaPrefab(codigo, color));
            Object.DestroyImmediate(instancia);
            return codigo + " " + color + "\tvariante " + RutaPrefab(codigo, color) + " (" + cambiados + " renderers con " + m.name + ")";
        }

        /// <summary>Los carros del tráfico (originales y variantes) en fila, con luz de día, en Logs/s2n-carros.png.</summary>
        public static void Hoja()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject sol = new GameObject("Sol");
            Light l = sol.AddComponent<Light>();
            l.type = LightType.Directional;
            l.intensity = 1.4f;
            sol.transform.rotation = Quaternion.Euler(40f, 150f, 0f);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.55f, 0.55f, 0.6f);
            string[] rutas = new[] { "V02", "V03", "V04" }.Select(CreadorTraficoT511.RutaPrefab)
                .Concat(Variantes.Select(v => RutaPrefab(v.codigo, v.color)))
                .Concat(ImportarS2nCarros.Medidas.Select(c => CreadorTraficoT511.RutaPrefab(c.codigo)))
                .Where(r => AssetDatabase.LoadAssetAtPath<GameObject>(r) != null).ToArray();
            for (int i = 0; i < rutas.Length; i++)
            {
                GameObject g = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(rutas[i]));
                g.transform.position = new Vector3((i % 3) * 6f, 0f, (i / 3) * 7f);
                g.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            }
            GameObject camObj = new GameObject("Cam");
            Camera cam = camObj.AddComponent<Camera>();
            camObj.AddComponent<UniversalAdditionalCameraData>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.2f, 0.22f, 0.26f);
            cam.fieldOfView = 50f;
            float filas = Mathf.Ceil(rutas.Length / 3f);
            cam.transform.position = new Vector3(6f, 9f + filas * 2f, -9f);
            cam.transform.LookAt(new Vector3(6f, 0f, (filas - 1) * 3.5f));
            RenderTexture rt = new RenderTexture(1280, 900, 24);
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            Texture2D foto = new Texture2D(1280, 900, TextureFormat.RGB24, false);
            foto.ReadPixels(new Rect(0, 0, 1280, 900), 0, 0);
            foto.Apply();
            File.WriteAllBytes("Logs/s2n-carros.png", foto.EncodeToPNG());
            RenderTexture.active = null;
            rt.Release();
            EditorSceneManager.OpenScene(ColocarS2.RutaEscena, OpenSceneMode.Single);
        }
    }
}
