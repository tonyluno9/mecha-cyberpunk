using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Visual.Editor
{
    /// <summary>
    /// T-732 (A2): PJ01 (Tripo, 78 huesos, R_Hand = mano derecha) como modelo del jugador. Importa Humanoid con el avatar de su esqueleto, a la escala
    /// que lo deja en 1.80 m ANIMADO (el Scale Factor del import, así R_Hand queda con lossyScale 1: caso A de agarre.txt), material URP con sus texturas,
    /// prefab Visual_PJ01 (copia de Visual_woman con el modelo cambiado) y lo pone como ModeloJugador del prefab del jugador y en el ModeloSegunPerfil de la escena.
    /// Batchmode: -executeMethod MechaCyberpunk.Personaje.Visual.Editor.CreadorPJ01T732.Crear [-escala 1.23]
    /// </summary>
    public static class CreadorPJ01T732
    {
        public const string Nombre = "PJ01";
        public const string RutaFbx = "Assets/Arte/Personajes/PJ01/PJ01.fbx";
        public const string RutaMaterial = "Assets/Arte/Personajes/PJ01/Materiales/PJ01_Base.mat";
        public const string RutaTexColor = "Assets/Arte/Personajes/PJ01/Texturas/PJ01_basecolor.jpg";
        public const string RutaTexNormal = "Assets/Arte/Personajes/PJ01/Texturas/PJ01_normal.jpg";
        public const string RutaPrefab = "Assets/_Proyecto/Personaje/Visual/Prefabs/Visual_PJ01.prefab";
        public const string NombreSocket = "ManoDer";

        [MenuItem("Mecha Cyberpunk/T-732/Crear PJ01 como jugador")]
        public static void Crear()
        {
            Importar();
            Material material = CrearMaterial();
            GameObject visual = CrearPrefab(material);
            PonerEnJugador(visual);
            PonerEnEscena(visual);
            AssetDatabase.SaveAssets();
        }

        /// <summary>Humanoid con su avatar; la escala sale de -escala, de la ya guardada o, la primera vez, de 1.8 / altura en reposo.</summary>
        public static void Importar()
        {
            var imp = (ModelImporter)AssetImporter.GetAtPath(RutaFbx);
            imp.animationType = ModelImporterAnimationType.Human;
            imp.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            imp.importAnimation = false;
            string[] args = System.Environment.GetCommandLineArgs();
            string escala = null;
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == "-escala") { escala = args[i + 1]; }
            }
            if (escala == null && !imp.userData.StartsWith("escala="))
            {
                imp.userData = "escala=1";
                imp.SaveAndReimport();
                float alto = CreadorVisualTripo.AltoDelModelo(RutaFbx);
                escala = (CreadorVisualTripo.AlturaObjetivo / alto).ToString("0.#####", System.Globalization.CultureInfo.InvariantCulture);
            }
            if (escala != null) { imp.userData = "escala=" + escala; }
            imp.SaveAndReimport();
            Avatar avatar = AvatarDe(RutaFbx);
            if (avatar == null || !avatar.isHuman) { throw new System.Exception("T-732: PJ01 no generó un Avatar Humanoid válido."); }
            Debug.Log("T-732: PJ01 " + imp.userData + ", scale " + imp.globalScale);
        }

        public static Avatar AvatarDe(string ruta)
        {
            foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(ruta)) { if (o is Avatar a) { return a; } }
            return null;
        }

        private static Material CrearMaterial()
        {
            var normal = (TextureImporter)AssetImporter.GetAtPath(RutaTexNormal);
            if (normal.textureType != TextureImporterType.NormalMap)
            {
                normal.textureType = TextureImporterType.NormalMap;
                normal.SaveAndReimport();
            }
            Directory.CreateDirectory(Path.GetDirectoryName(RutaMaterial));
            Material m = AssetDatabase.LoadAssetAtPath<Material>(RutaMaterial);
            if (m == null)
            {
                m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(m, RutaMaterial);
            }
            m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(RutaTexColor));
            m.SetColor("_BaseColor", Color.white);
            m.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(RutaTexNormal));
            m.EnableKeyword("_NORMALMAP");
            m.SetFloat("_Smoothness", 0.25f);
            m.SetFloat("_Metallic", 0f);
            EditorUtility.SetDirty(m);
            return m;
        }

        private static GameObject CrearPrefab(Material material)
        {
            string rutaWoman = CreadorVisualPersonaje.RutaPrefab("woman");
            if (AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab) == null)
            {
                AssetDatabase.CopyAsset(rutaWoman, RutaPrefab);
            }
            GameObject contenido = PrefabUtility.LoadPrefabContents(RutaPrefab);
            try
            {
                contenido.name = "Visual_PJ01";
                Transform giro = contenido.transform.Find("Giro");
                Transform viejo = giro.Find("Modelo");
                GameObject origen = viejo != null ? PrefabUtility.GetCorrespondingObjectFromSource(viejo.gameObject) : null;
                if (origen != null && AssetDatabase.GetAssetPath(origen) == RutaFbx)
                {
                    foreach (SkinnedMeshRenderer r in viejo.GetComponentsInChildren<SkinnedMeshRenderer>(true)) { r.sharedMaterial = material; }
                    return PrefabUtility.SaveAsPrefabAsset(contenido, RutaPrefab); // ya es PJ01: solo se repinta el material
                }
                Animator animadorViejo = viejo.GetComponent<Animator>();
                GameObject nuevo = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(RutaFbx), giro);
                nuevo.name = "Modelo";
                Animator animador = nuevo.GetComponent<Animator>();
                if (animador == null) { animador = nuevo.AddComponent<Animator>(); }
                animador.avatar = AvatarDe(RutaFbx);
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
                if (mano == null) { throw new System.Exception("T-732: el avatar de PJ01 no tiene RightHand."); }
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
                return PrefabUtility.SaveAsPrefabAsset(contenido, RutaPrefab);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contenido);
            }
        }

        private static void PonerEnJugador(GameObject visual)
        {
            GameObject contenido = PrefabUtility.LoadPrefabContents(ConectarVisualesT520.RutaJugador);
            try
            {
                Transform viejo = contenido.transform.Find("ModeloJugador");
                if (viejo != null && PrefabUtility.GetCorrespondingObjectFromSource(viejo.gameObject) == visual)
                {
                    return;
                }
                GameObject nuevo = ConectarVisualesT520.Reemplazar(contenido, viejo, visual, "ModeloJugador", true);
                var so = new SerializedObject(contenido.GetComponent("AnimacionJugador"));
                so.FindProperty("animador").objectReferenceValue = nuevo.GetComponentInChildren<Animator>(true);
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(contenido, ConectarVisualesT520.RutaJugador);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contenido);
            }
        }

        /// <summary>El ModeloSegunPerfil de la escena: la cara 0 (el jugador) deja de ser woman y pasa a PJ01; la cara 1 (man) no cambia.</summary>
        private static void PonerEnEscena(GameObject visual)
        {
            EditorSceneManager.OpenScene(ConectarVisualesT520.RutaEscena, OpenSceneMode.Single);
            bool cambio = false;
            foreach (MechaCyberpunk.Personaje.Apariencia.ModeloSegunPerfil m in Object.FindObjectsByType<MechaCyberpunk.Personaje.Apariencia.ModeloSegunPerfil>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var so = new SerializedObject(m);
                SerializedProperty lista = so.FindProperty("modelos");
                if (lista.arraySize > 0 && lista.GetArrayElementAtIndex(0).objectReferenceValue != visual)
                {
                    lista.GetArrayElementAtIndex(0).objectReferenceValue = visual;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    cambio = true;
                }
            }
            if (cambio)
            {
                EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
                EditorSceneManager.SaveOpenScenes();
            }
        }
    }
}
