using System.IO;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Visual.Editor
{
    /// <summary>
    /// T-744: AL10 y AL11 (Tripo, mismo esqueleto de 78 huesos que PJ01) como los aliados que te siguen. Mismo patrón que CreadorPJ01T732: Humanoid con el avatar
    /// de su esqueleto, escala del import (userData "escala=") que lo deja en 1.80 m animado, material URP con sus texturas y prefab Visual_ALxx (copia de Visual_woman2,
    /// que ya trae el controlador del papel Aliados). Aliado.prefab usa AL10 y Aliado_AL11.prefab (copia con AL11) es el segundo aliado de PandillaJugador.
    /// Batchmode: -executeMethod MechaCyberpunk.Personaje.Visual.Editor.CreadorAliadosT744.Crear [-escalaAL10 1.2] [-escalaAL11 1.2]
    /// </summary>
    public static class CreadorAliadosT744
    {
        public static readonly string[] Nombres = { "AL10", "AL11" };
        public const string RutaAliado1 = "Assets/_Proyecto/IA/Prefabs/Aliado.prefab";
        public const string RutaAliado2 = "Assets/_Proyecto/IA/Prefabs/Aliado_AL11.prefab";
        public const string NombreSocket = "ManoDer";

        public static string RutaFbx(string n) => "Assets/Arte/Personajes/" + n + "/" + n + ".fbx";
        public static string RutaMaterial(string n) => "Assets/Arte/Personajes/" + n + "/Materiales/" + n + "_Base.mat";
        public static string RutaTexColor(string n) => "Assets/Arte/Personajes/" + n + "/Texturas/" + n + "_basecolor.jpg";
        public static string RutaTexNormal(string n) => "Assets/Arte/Personajes/" + n + "/Texturas/" + n + "_normal.jpg";
        public static string RutaVisual(string n) => "Assets/_Proyecto/Personaje/Visual/Prefabs/Visual_" + n + ".prefab";

        [MenuItem("Mecha Cyberpunk/T-744/Crear aliados AL10 y AL11")]
        public static void Crear()
        {
            foreach (string n in Nombres)
            {
                Importar(n);
                Material material = CrearMaterial(n);
                CrearVisual(n, material);
            }
            AssetDatabase.SaveAssets();
            PonerModelo(RutaAliado1, "AL10", null);
            PonerModelo(RutaAliado2, "AL11", RutaAliado1);
            AssetDatabase.SaveAssets();
        }

        public static void Importar(string n)
        {
            var imp = (ModelImporter)AssetImporter.GetAtPath(RutaFbx(n));
            imp.animationType = ModelImporterAnimationType.Human;
            imp.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            imp.importAnimation = false;
            string[] args = System.Environment.GetCommandLineArgs();
            string escala = null;
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == "-escala" + n) { escala = args[i + 1]; }
            }
            if (escala == null && !imp.userData.StartsWith("escala="))
            {
                imp.userData = "escala=1";
                imp.SaveAndReimport();
                float alto = CreadorVisualTripo.AltoDelModelo(RutaFbx(n));
                escala = (CreadorVisualTripo.AlturaObjetivo / alto).ToString("0.#####", System.Globalization.CultureInfo.InvariantCulture);
            }
            if (escala != null) { imp.userData = "escala=" + escala; }
            imp.SaveAndReimport();
            Avatar avatar = CreadorPJ01T732.AvatarDe(RutaFbx(n));
            if (avatar == null || !avatar.isHuman) { throw new System.Exception("T-744: " + n + " no generó un Avatar Humanoid válido."); }
            Debug.Log("T-744: " + n + " " + imp.userData + ", scale " + imp.globalScale);
        }

        private static Material CrearMaterial(string n)
        {
            var normal = (TextureImporter)AssetImporter.GetAtPath(RutaTexNormal(n));
            if (normal.textureType != TextureImporterType.NormalMap)
            {
                normal.textureType = TextureImporterType.NormalMap;
                normal.SaveAndReimport();
            }
            Directory.CreateDirectory(Path.GetDirectoryName(RutaMaterial(n)));
            Material m = AssetDatabase.LoadAssetAtPath<Material>(RutaMaterial(n));
            if (m == null)
            {
                m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(m, RutaMaterial(n));
            }
            m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(RutaTexColor(n)));
            m.SetColor("_BaseColor", Color.white);
            m.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(RutaTexNormal(n)));
            m.EnableKeyword("_NORMALMAP");
            m.SetFloat("_Smoothness", 0.25f);
            m.SetFloat("_Metallic", 0f);
            EditorUtility.SetDirty(m);
            return m;
        }

        private static void CrearVisual(string n, Material material)
        {
            string ruta = RutaVisual(n);
            string fbx = RutaFbx(n);
            if (AssetDatabase.LoadAssetAtPath<GameObject>(ruta) == null)
            {
                AssetDatabase.CopyAsset(CreadorVisualPersonaje.RutaPrefab("woman2"), ruta);
            }
            GameObject contenido = PrefabUtility.LoadPrefabContents(ruta);
            try
            {
                contenido.name = "Visual_" + n;
                Transform giro = contenido.transform.Find("Giro");
                Transform viejo = giro.Find("Modelo");
                GameObject origen = PrefabUtility.GetCorrespondingObjectFromSource(viejo.gameObject);
                if (origen != null && AssetDatabase.GetAssetPath(origen) == fbx)
                {
                    foreach (SkinnedMeshRenderer r in viejo.GetComponentsInChildren<SkinnedMeshRenderer>(true)) { r.sharedMaterial = material; }
                    PrefabUtility.SaveAsPrefabAsset(contenido, ruta);
                    return;
                }
                Animator animadorViejo = viejo.GetComponent<Animator>();
                GameObject nuevo = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(fbx), giro);
                nuevo.name = "Modelo";
                Animator animador = nuevo.GetComponent<Animator>();
                if (animador == null) { animador = nuevo.AddComponent<Animator>(); }
                animador.avatar = CreadorPJ01T732.AvatarDe(fbx);
                animador.runtimeAnimatorController = animadorViejo.runtimeAnimatorController;
                animador.applyRootMotion = false;
                animador.cullingMode = animadorViejo.cullingMode;
                ApuntadoIK ikViejo = animadorViejo.GetComponent<ApuntadoIK>();
                ApuntadoIK ik = nuevo.AddComponent<ApuntadoIK>();
                if (ikViejo != null) { EditorUtility.CopySerialized(ikViejo, ik); }
                IKPiesYMirada piesViejo = animadorViejo.GetComponent<IKPiesYMirada>();
                IKPiesYMirada pies = nuevo.AddComponent<IKPiesYMirada>();
                if (piesViejo != null) { EditorUtility.CopySerialized(piesViejo, pies); }
                Transform mano = animador.GetBoneTransform(HumanBodyBones.RightHand);
                if (mano == null) { throw new System.Exception("T-744: el avatar de " + n + " no tiene RightHand."); }
                Transform socket = mano.Find(NombreSocket);
                if (socket == null)
                {
                    socket = new GameObject(NombreSocket).transform;
                    socket.SetParent(mano, false);
                }
                ik.ConfigurarSocket(socket);
                foreach (SkinnedMeshRenderer r in nuevo.GetComponentsInChildren<SkinnedMeshRenderer>(true)) { r.sharedMaterial = material; }
                var sv = new SerializedObject(contenido.GetComponent<VisualPersonaje>());
                sv.FindProperty("animador").objectReferenceValue = animador;
                sv.ApplyModifiedPropertiesWithoutUndo();
                Object.DestroyImmediate(viejo.gameObject);
                PrefabUtility.SaveAsPrefabAsset(contenido, ruta);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contenido);
            }
        }

        /// <summary>Cambia el "Modelo" de un prefab de aliado por Visual_&lt;nombre&gt;; si <paramref name="copiaDe"/> no es null y el prefab no existe, lo copia de ahí.</summary>
        private static void PonerModelo(string rutaAliado, string nombre, string copiaDe)
        {
            if (copiaDe != null && AssetDatabase.LoadAssetAtPath<GameObject>(rutaAliado) == null)
            {
                AssetDatabase.CopyAsset(copiaDe, rutaAliado);
            }
            GameObject visual = AssetDatabase.LoadAssetAtPath<GameObject>(RutaVisual(nombre));
            GameObject contenido = PrefabUtility.LoadPrefabContents(rutaAliado);
            try
            {
                Transform viejo = contenido.transform.Find("Modelo");
                if (viejo != null && PrefabUtility.GetCorrespondingObjectFromSource(viejo.gameObject) == visual)
                {
                    return;
                }
                ConectarVisualesT520.Reemplazar(contenido, viejo, visual, "Modelo", false);
                PrefabUtility.SaveAsPrefabAsset(contenido, rutaAliado);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contenido);
            }
        }

        /// <summary>El segundo aliado de la pandilla de la escena del prólogo (PandillaJugador.prefabAliadoSegundo).</summary>
        [MenuItem("Mecha Cyberpunk/T-744/Conectar segundo aliado en la escena")]
        public static void ConectarEscena()
        {
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(ConectarVisualesT520.RutaEscena, UnityEditor.SceneManagement.OpenSceneMode.Single);
            GameObject segundo = AssetDatabase.LoadAssetAtPath<GameObject>(RutaAliado2);
            bool cambio = false;
            foreach (MonoBehaviour m in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (m == null || m.GetType().Name != "PandillaJugador") { continue; }
                var so = new SerializedObject(m);
                SerializedProperty p = so.FindProperty("prefabAliadoSegundo");
                if (p.objectReferenceValue != segundo)
                {
                    p.objectReferenceValue = segundo;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    cambio = true;
                }
            }
            if (cambio)
            {
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
                UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
            }
        }
    }
}
