using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Visual.Editor
{
    /// <summary>
    /// Visual de prueba (T-473): usa el muñeco de cubos de T-400 (sin Avatar Humanoid, no hay FBX en el proyecto) con el mismo controlador
    /// y prefab que tendrá un personaje de Mixamo. Reutiliza los clips del muñeco y genera por código los que le faltan
    /// (RecibirGolpe, ApuntarLarga, DispararLarga, Cubrirse, Rodar). También saca la captura docs/capturas/T-473-1.png.
    /// </summary>
    public static class CreadorVisualPrueba
    {
        public const string Nombre = "Prueba";
        public const string CarpetaClips = "Assets/_Proyecto/Personaje/Visual/Prefabs/ClipsPrueba";
        public const string RutaControlador = "Assets/_Proyecto/Personaje/Visual/Prefabs/ControladorVisual_Prueba.controller";
        private const string ClipsMuneco = "Assets/Arte/Personajes/MunecoHumano/Clips/";
        private const string ModeloMuneco = "Assets/Arte/Personajes/MunecoHumano/ModeloHumano.prefab";

        private const string Torso = "Cadera/Torso";
        private const string BrazoSupDer = "Cadera/Torso/BrazoSupDer";
        private const string BrazoInfDer = "Cadera/Torso/BrazoSupDer/BrazoInfDer";
        private const string BrazoSupIzq = "Cadera/Torso/BrazoSupIzq";
        private const string BrazoInfIzq = "Cadera/Torso/BrazoSupIzq/BrazoInfIzq";
        private const string PiernaSupDer = "Cadera/PiernaSupDer";
        private const string PiernaInfDer = "Cadera/PiernaSupDer/PiernaInfDer";
        private const string PiernaSupIzq = "Cadera/PiernaSupIzq";
        private const string PiernaInfIzq = "Cadera/PiernaSupIzq/PiernaInfIzq";

        [MenuItem("Mecha Cyberpunk/Personajes/Crear visual de prueba")]
        public static void Crear()
        {
            if (!AssetDatabase.IsValidFolder(CarpetaClips))
            {
                System.IO.Directory.CreateDirectory(CarpetaClips);
                AssetDatabase.Refresh();
            }
            var clips = new Dictionary<ClipVisual, AnimationClip>
            {
                [ClipVisual.Idle] = Cargar("Quieto"),
                [ClipVisual.Caminar] = Cargar("Caminar"),
                [ClipVisual.Correr] = Cargar("Correr"),
                [ClipVisual.Saltar] = Cargar("Saltar"),
                [ClipVisual.Golpe] = Cargar("Golpe"),
                [ClipVisual.Caer] = Cargar("KO"),
                [ClipVisual.ApuntarPistola] = Cargar("Apuntar"),
                [ClipVisual.DispararPistola] = Cargar("Disparo"),
                [ClipVisual.Recargar] = Cargar("Recarga"),
                [ClipVisual.RecibirGolpe] = Generar("RecibirGolpe", 0.5f, false,
                    X(Torso, 0f, 0f, 0.1f, -25f, 0.5f, 0f), X(BrazoSupDer, 0f, 0f, 0.1f, 30f, 0.5f, 0f), X(BrazoSupIzq, 0f, 0f, 0.1f, 30f, 0.5f, 0f)),
                [ClipVisual.ApuntarLarga] = Generar("ApuntarLarga", 0.4f, true,
                    X(BrazoSupDer, 0f, -85f, 0.4f, -85f), X(BrazoInfDer, 0f, -15f, 0.4f, -15f), X(BrazoSupIzq, 0f, -80f, 0.4f, -80f), X(BrazoInfIzq, 0f, -20f, 0.4f, -20f)),
                [ClipVisual.DispararLarga] = Generar("DispararLarga", 0.25f, false,
                    X(BrazoSupDer, 0f, -85f, 0.05f, -100f, 0.25f, -85f), X(BrazoSupIzq, 0f, -80f, 0.05f, -95f, 0.25f, -80f), X(Torso, 0f, 0f, 0.05f, -6f, 0.25f, 0f)),
                [ClipVisual.Cubrirse] = Generar("Cubrirse", 0.5f, true,
                    Y("Cadera", 0f, 0.55f, 0.5f, 0.55f), X(PiernaSupDer, 0f, -75f, 0.5f, -75f), X(PiernaInfDer, 0f, 100f, 0.5f, 100f),
                    X(PiernaSupIzq, 0f, -75f, 0.5f, -75f), X(PiernaInfIzq, 0f, 100f, 0.5f, 100f), X(Torso, 0f, 20f, 0.5f, 20f)),
                [ClipVisual.Rodar] = Generar("Rodar", 0.6f, false,
                    X("Cadera", 0f, 0f, 0.6f, 360f), X(PiernaSupDer, 0f, -90f, 0.6f, -90f), X(PiernaInfDer, 0f, 110f, 0.6f, 110f),
                    X(PiernaSupIzq, 0f, -90f, 0.6f, -90f), X(PiernaInfIzq, 0f, 110f, 0.6f, 110f), X(Torso, 0f, 40f, 0.6f, 40f)),
            };
            AnimatorController controlador = ConstructorControladorVisual.Construir(RutaControlador, clips, out _);
            GameObject modelo = AssetDatabase.LoadAssetAtPath<GameObject>(ModeloMuneco);
            CreadorVisualPersonaje.ArmarPrefab(Nombre, modelo, null, controlador);
            AssetDatabase.SaveAssets();
            Debug.Log("[Personaje] Visual_Prueba creado.");
        }

        /// <summary>Captura 1920x1080 con el visual de prueba en 5 poses (cada una muestreando su clip). Ruta absoluta de salida en -salida.</summary>
        public static void Capturar()
        {
            string salida = "docs/capturas/T-473-1.png";
            string[] args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == "-salida") { salida = args[i + 1]; }
            }
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CreadorVisualPersonaje.RutaPrefab(Nombre));
            AnimatorController c = AssetDatabase.LoadAssetAtPath<AnimatorController>(RutaControlador);
            ClipVisual[] poses = { ClipVisual.Idle, ClipVisual.Correr, ClipVisual.ApuntarPistola, ClipVisual.ApuntarLarga, ClipVisual.Cubrirse, ClipVisual.Rodar, ClipVisual.Golpe };
            float[] tiempos = { 0f, 0.15f, 0.2f, 0.2f, 0.2f, 0.18f, 0.1f };
            for (int i = 0; i < poses.Length; i++)
            {
                GameObject g = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                g.transform.position = new Vector3((i - (poses.Length - 1) * 0.5f) * 1.3f, 0f, 0f);
                g.transform.rotation = Quaternion.Euler(0f, 140f, 0f);
                AnimationClip clip = null;
                foreach (ChildAnimatorState s in c.layers[0].stateMachine.states)
                {
                    if (s.state.name == poses[i].ToString()) { clip = s.state.motion as AnimationClip; }
                }
                Transform modelo = g.transform.Find("Modelo");
                if (clip != null) { clip.SampleAnimation(modelo.gameObject, tiempos[i]); }
            }
            GameObject suelo = GameObject.CreatePrimitive(PrimitiveType.Plane);
            suelo.transform.localScale = new Vector3(2f, 1f, 1f);
            Shader lit = Shader.Find("Universal Render Pipeline/Lit");
            suelo.GetComponent<Renderer>().sharedMaterial = new Material(lit) { color = new Color(0.15f, 0.15f, 0.2f) };
            GameObject luz = new GameObject("Luz");
            Light l = luz.AddComponent<Light>();
            l.type = LightType.Directional;
            luz.transform.rotation = Quaternion.Euler(40f, -30f, 0f);
            GameObject cam = new GameObject("Camara");
            Camera camara = cam.AddComponent<Camera>();
            camara.clearFlags = CameraClearFlags.SolidColor;
            camara.backgroundColor = new Color(0.05f, 0.06f, 0.1f);
            cam.transform.position = new Vector3(0f, 1.3f, -9f);
            cam.transform.LookAt(new Vector3(0f, 0.85f, 0f));
            camara.fieldOfView = 36f;
            RenderTexture rt = new RenderTexture(1920, 1080, 24);
            camara.targetTexture = rt;
            camara.Render();
            RenderTexture.active = rt;
            Texture2D tex = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
            tex.Apply();
            System.IO.File.WriteAllBytes(salida, tex.EncodeToPNG());
            RenderTexture.active = null;
            Debug.Log("[Personaje] Captura guardada en " + salida);
        }

        private static AnimationClip Cargar(string nombre) => AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipsMuneco + nombre + ".anim");

        private struct Curva
        {
            public string Ruta;
            public string Propiedad;
            public AnimationCurve Valores;
        }

        private static Curva X(string ruta, params float[] tiempoValor) => Nueva(ruta, "localEulerAnglesRaw.x", tiempoValor);

        private static Curva Y(string ruta, params float[] tiempoValor) => Nueva(ruta, "m_LocalPosition.y", tiempoValor);

        private static Curva Nueva(string ruta, string propiedad, float[] tv)
        {
            var llaves = new Keyframe[tv.Length / 2];
            for (int i = 0; i < llaves.Length; i++)
            {
                llaves[i] = new Keyframe(tv[i * 2], tv[i * 2 + 1]);
            }
            return new Curva { Ruta = ruta, Propiedad = propiedad, Valores = new AnimationCurve(llaves) };
        }

        private static AnimationClip Generar(string nombre, float duracion, bool bucle, params Curva[] curvas)
        {
            string ruta = CarpetaClips + "/" + nombre + ".anim";
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ruta);
            if (clip == null)
            {
                clip = new AnimationClip { name = nombre, frameRate = 30f };
                AssetDatabase.CreateAsset(clip, ruta);
            }
            clip.ClearCurves();
            foreach (Curva curva in curvas)
            {
                AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(curva.Ruta, typeof(Transform), curva.Propiedad), curva.Valores);
            }
            AnimationClipSettings ajustes = AnimationUtility.GetAnimationClipSettings(clip);
            ajustes.loopTime = bucle;
            AnimationUtility.SetAnimationClipSettings(clip, ajustes);
            EditorUtility.SetDirty(clip);
            return clip;
        }
    }
}
