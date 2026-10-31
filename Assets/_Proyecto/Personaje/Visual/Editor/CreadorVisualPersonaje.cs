using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Visual.Editor
{
    /// <summary>
    /// Crea o actualiza el visual de un personaje a partir de una carpeta Assets/Arte/Personajes/&lt;Nombre&gt;/ con los FBX de Mixamo (T-473):
    /// configura la importación Humanoid, arma el Animator Controller y el prefab Personaje/Visual/Prefabs/Visual_&lt;Nombre&gt;.prefab.
    /// Guía de uso en docs/como-meter-personajes.md.
    /// </summary>
    public static class CreadorVisualPersonaje
    {
        public const string CarpetaPersonajes = "Assets/Arte/Personajes";
        public const string CarpetaPrefabs = "Assets/_Proyecto/Personaje/Visual/Prefabs";

        public static string RutaPrefab(string nombre) => CarpetaPrefabs + "/Visual_" + nombre + ".prefab";

        public static string RutaControlador(string nombre) => CarpetaPersonajes + "/" + nombre + "/" + nombre + ".controller";

        [MenuItem("Mecha Cyberpunk/Personajes/Crear visual desde FBX")]
        public static void CrearDesdeSeleccion()
        {
            var nombres = new List<string>();
            foreach (UnityEngine.Object o in Selection.objects)
            {
                string ruta = AssetDatabase.GetAssetPath(o);
                if (AssetDatabase.IsValidFolder(ruta) && Path.GetDirectoryName(ruta).Replace('\\', '/') == CarpetaPersonajes)
                {
                    nombres.Add(Path.GetFileName(ruta));
                }
            }
            if (nombres.Count == 0)
            {
                Debug.LogWarning("[Personaje] Selecciona en el Project una carpeta de " + CarpetaPersonajes + "/<Nombre> con los FBX de Mixamo.");
                return;
            }
            foreach (string n in nombres)
            {
                Crear(n);
            }
        }

        /// <summary>-executeMethod: crea el visual de la carpeta pasada con -nombre X, o de todas las carpetas que tengan FBX si no se pasa.</summary>
        public static void CrearDesdeLineaDeComandos()
        {
            string[] args = Environment.GetCommandLineArgs();
            string unico = null;
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == "-nombre")
                {
                    unico = args[i + 1];
                }
            }
            if (unico != null)
            {
                Crear(unico);
                return;
            }
            foreach (string carpeta in AssetDatabase.GetSubFolders(CarpetaPersonajes))
            {
                string n = Path.GetFileName(carpeta);
                if (AssetDatabase.FindAssets("t:Model", new[] { carpeta }).Length > 0)
                {
                    Crear(n);
                }
            }
        }

        /// <summary>Devuelve el prefab creado, o null si la carpeta no tiene un modelo.</summary>
        public static GameObject Crear(string nombre)
        {
            string carpeta = CarpetaPersonajes + "/" + nombre;
            if (!AssetDatabase.IsValidFolder(carpeta))
            {
                Debug.LogError("[Personaje] No existe la carpeta " + carpeta);
                return null;
            }
            var archivos = new List<string>();
            foreach (string guid in AssetDatabase.FindAssets("t:Model", new[] { carpeta }))
            {
                archivos.Add(AssetDatabase.GUIDToAssetPath(guid));
            }
            var nombresArchivo = new List<string>();
            foreach (string a in archivos) { nombresArchivo.Add(Path.GetFileName(a)); }
            string modeloNombre = ReglasVisual.ElegirModelo(nombresArchivo);
            if (modeloNombre == null)
            {
                Debug.LogError("[Personaje] " + carpeta + " no tiene un modelo (Modelo.fbx o un FBX que no sea una animación).");
                return null;
            }
            string rutaModelo = archivos[nombresArchivo.IndexOf(modeloNombre)];

            // 1) El modelo define el Avatar Humanoid.
            ModelImporter importadorModelo = (ModelImporter)AssetImporter.GetAtPath(rutaModelo);
            importadorModelo.animationType = ModelImporterAnimationType.Human;
            importadorModelo.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importadorModelo.SaveAndReimport();
            Avatar avatar = CargarAvatar(rutaModelo);

            // 2) Las animaciones copian ese Avatar, en el sitio (sin mover la raíz) y con bucle donde toca.
            var clips = new Dictionary<ClipVisual, AnimationClip>();
            for (int i = 0; i < archivos.Count; i++)
            {
                if (archivos[i] == rutaModelo || !ReglasVisual.TryClipDeArchivo(nombresArchivo[i], out ClipVisual tipo))
                {
                    if (archivos[i] != rutaModelo)
                    {
                        Debug.LogWarning("[Personaje] Archivo ignorado (nombre no reconocido): " + archivos[i]);
                    }
                    continue;
                }
                ConfigurarAnimacion(archivos[i], tipo, avatar);
                AnimationClip clip = CargarClip(archivos[i]);
                if (clip != null)
                {
                    clips[tipo] = clip;
                }
            }

            // 3) Controlador.
            AnimatorController controlador = ConstructorControladorVisual.Construir(RutaControlador(nombre), clips, out List<ClipVisual> faltan);
            foreach (ClipVisual f in faltan)
            {
                Debug.LogWarning("[Personaje] " + nombre + ": falta la animación " + f + " (estado sin movimiento).");
            }

            // 4) Prefab.
            GameObject modelo = AssetDatabase.LoadAssetAtPath<GameObject>(rutaModelo);
            GameObject prefab = ArmarPrefab(nombre, modelo, avatar, controlador);
            Debug.Log("[Personaje] Visual_" + nombre + " listo: " + clips.Count + " animaciones, " + faltan.Count + " sin archivo.");
            return prefab;
        }

        /// <summary>Raíz Visual_Nombre con VisualPersonaje y, de hijo, el modelo con su Animator (sin movimiento de raíz).</summary>
        public static GameObject ArmarPrefab(string nombre, GameObject modelo, Avatar avatar, RuntimeAnimatorController controlador)
        {
            if (!AssetDatabase.IsValidFolder(CarpetaPrefabs))
            {
                Directory.CreateDirectory(CarpetaPrefabs);
                AssetDatabase.Refresh();
            }
            GameObject raiz = new GameObject("Visual_" + nombre);
            try
            {
                GameObject hijo = (GameObject)PrefabUtility.InstantiatePrefab(modelo, raiz.transform);
                hijo.name = "Modelo";
                Animator animador = hijo.GetComponent<Animator>();
                if (animador == null)
                {
                    animador = hijo.AddComponent<Animator>();
                }
                if (avatar != null)
                {
                    animador.avatar = avatar;
                }
                animador.runtimeAnimatorController = controlador;
                animador.applyRootMotion = false;
                VisualPersonaje visual = raiz.AddComponent<VisualPersonaje>();
                SerializedObject so = new SerializedObject(visual);
                so.FindProperty("animador").objectReferenceValue = animador;
                so.ApplyModifiedPropertiesWithoutUndo();
                return PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefab(nombre));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(raiz);
            }
        }

        private static void ConfigurarAnimacion(string ruta, ClipVisual tipo, Avatar avatar)
        {
            ModelImporter importador = (ModelImporter)AssetImporter.GetAtPath(ruta);
            importador.animationType = ModelImporterAnimationType.Human;
            importador.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
            importador.sourceAvatar = avatar;
            importador.importAnimation = true;
            ModelImporterClipAnimation[] clips = importador.defaultClipAnimations;
            foreach (ModelImporterClipAnimation c in clips)
            {
                c.name = tipo.ToString();
                c.loopTime = ReglasVisual.EsBucle(tipo);
                c.loopPose = c.loopTime;
                c.lockRootRotation = true;
                c.lockRootHeightY = true;
                c.lockRootPositionXZ = true;
                c.keepOriginalOrientation = true;
                c.keepOriginalPositionY = true;
                c.keepOriginalPositionXZ = true;
            }
            importador.clipAnimations = clips;
            importador.SaveAndReimport();
        }

        private static Avatar CargarAvatar(string ruta)
        {
            foreach (UnityEngine.Object o in AssetDatabase.LoadAllAssetsAtPath(ruta))
            {
                if (o is Avatar a)
                {
                    return a;
                }
            }
            Debug.LogWarning("[Personaje] " + ruta + " no generó un Avatar Humanoid: revisa que el modelo sea un humanoide con esqueleto (Mixamo con 'With Skin').");
            return null;
        }

        private static AnimationClip CargarClip(string ruta)
        {
            foreach (UnityEngine.Object o in AssetDatabase.LoadAllAssetsAtPath(ruta))
            {
                if (o is AnimationClip c && !c.name.StartsWith("__preview__", StringComparison.Ordinal))
                {
                    return c;
                }
            }
            return null;
        }
    }
}
