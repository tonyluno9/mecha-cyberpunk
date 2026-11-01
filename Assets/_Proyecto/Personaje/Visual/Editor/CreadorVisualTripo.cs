using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Visual.Editor
{
    /// <summary>
    /// T-520: arma el visual de los personajes de Tripo (woman, man, woman2) cuyos clips vienen dentro del propio FBX: importación Humanoid
    /// con el avatar de su esqueleto, escala a 1.8 m, clips renombrados a los estados de <see cref="ReglasVisual"/> (Idle sale de la pose
    /// inicial de "agree"; lo que no existe usa Idle), material propio con la textura del .fbm, controlador y prefab Visual_&lt;Nombre&gt;.
    /// Batchmode: -executeMethod MechaCyberpunk.Personaje.Visual.Editor.CreadorVisualTripo.CrearTodos
    /// </summary>
    public static class CreadorVisualTripo
    {
        public const string Carpeta = CreadorVisualPersonaje.CarpetaPersonajes;
        public const float AlturaObjetivo = 1.8f;
        public static readonly string[] Nombres = { "woman", "man", "woman2" };

        private const int CuadrosIdle = 2;
        private const int CuadrosInicioIdle = 0;
        private const float FactorUmbralCorrer = 0.5f;

        public static string RutaModelo(string nombre) => Carpeta + "/" + nombre + "/" + nombre + ".fbx";

        public static string RutaMaterialBase(string nombre) => Carpeta + "/" + nombre + "/Materiales/" + nombre + "_Base.mat";

        [MenuItem("Mecha Cyberpunk/Personajes/T-520 Crear visuales de Tripo")]
        public static void CrearTodos()
        {
            foreach (string n in Nombres)
            {
                Crear(n);
            }
            AssetDatabase.SaveAssets();
        }

        public static GameObject Crear(string nombre)
        {
            string ruta = RutaModelo(nombre);
            ModelImporter imp = (ModelImporter)AssetImporter.GetAtPath(ruta);
            if (imp == null)
            {
                Debug.LogError("[Personaje] No existe " + ruta);
                return null;
            }

            // 1) Humanoid con el avatar de su propio esqueleto, a escala 1 para medir.
            imp.animationType = ModelImporterAnimationType.Human;
            imp.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            imp.importAnimation = true;
            imp.userData = "escala=1";
            imp.SaveAndReimport();
            float alto = AltoDelModelo(ruta);
            float escala = alto > 0.01f ? AlturaObjetivo / alto : 1f;
            // PostprocesadorAssetsT250 aplica esta escala al importar (PostprocesadorArte fuerza globalScale a 1).
            if (!imp.generateMeshLods && CuentaTriangulos(ruta) > TriangulosParaMeshLod)
            {
                imp.generateMeshLods = true;
            }
            imp.userData = "escala=" + escala.ToString("0.#####", System.Globalization.CultureInfo.InvariantCulture);

            // 2) Clips: los de la toma, renombrados; Idle = primeros cuadros de "agree".
            ModelImporterClipAnimation[] porDefecto = imp.defaultClipAnimations;
            var lista = new List<ModelImporterClipAnimation>();
            foreach (ModelImporterClipAnimation d in porDefecto)
            {
                ModelImporterClipAnimation c = Ajustar(d, d.takeName.StartsWith("agree") ? NombreIdle : null);
                if (d.takeName.StartsWith("walk")) { c.name = ClipVisual.Caminar.ToString(); c.loopTime = true; lista.Add(c); }
                else if (d.takeName.StartsWith("run")) { c.name = ClipVisual.Correr.ToString(); c.loopTime = true; lista.Add(c); }
                else if (d.takeName.StartsWith("afraid")) { c.name = ClipVisual.Cubrirse.ToString(); c.loopTime = true; lista.Add(c); }
                else if (d.takeName.StartsWith("agree"))
                {
                    // ModelImporterClipAnimation es una clase: la copia completa sale de otra lectura de los clips por defecto.
                    foreach (ModelImporterClipAnimation otro in imp.defaultClipAnimations)
                    {
                        if (otro.takeName == d.takeName)
                        {
                            ModelImporterClipAnimation completo = Ajustar(otro, "AcuerdoCompleto");
                            lista.Add(completo);
                        }
                    }
                    c.name = ClipVisual.Idle.ToString();
                    c.firstFrame = d.firstFrame + CuadrosInicioIdle;
                    c.lastFrame = c.firstFrame + CuadrosIdle;
                    c.loopTime = true;
                    lista.Add(c);
                }
            }
            imp.clipAnimations = lista.ToArray();
            imp.SaveAndReimport();

            Avatar avatar = null;
            var clips = new Dictionary<ClipVisual, AnimationClip>();
            foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(ruta))
            {
                if (o is Avatar a) { avatar = a; }
                else if (o is AnimationClip c && !c.name.StartsWith("__preview__") && System.Enum.TryParse(c.name, out ClipVisual tipo))
                {
                    clips[tipo] = c;
                }
            }
            if (avatar == null || !avatar.isHuman)
            {
                Debug.LogError("[Personaje] " + nombre + ": no se generó un Avatar Humanoid válido.");
                return null;
            }
            // Animaciones de Mixamo (docs/animaciones-faltan.md) en Assets/Arte/Personajes/Animaciones/: valen para los tres personajes.
            Dictionary<ClipVisual, AnimationClip> extras = AnimacionesExtra(nombre == Nombres[0] ? avatar : AvatarDe(Nombres[0]));
            foreach (KeyValuePair<ClipVisual, AnimationClip> extra in extras)
            {
                clips[extra.Key] = extra.Value;
            }
            // Con las dos animaciones de apuntar reales, las manos ya no se llevan por IK.
            bool tieneApuntarDeVerdad = extras.ContainsKey(ClipVisual.ApuntarPistola) && extras.ContainsKey(ClipVisual.ApuntarLarga);
            // Lo que no tiene animación se queda en Idle (apuntar lo hace ApuntadoIK; caer, VisualPersonaje).
            foreach (ClipVisual tipo in System.Enum.GetValues(typeof(ClipVisual)))
            {
                if (!clips.ContainsKey(tipo) && clips.ContainsKey(ClipVisual.Idle))
                {
                    clips[tipo] = clips[ClipVisual.Idle];
                }
            }

            // 3) Controlador (el umbral definitivo se pone al medir los clips).
            AnimatorController controlador = ConstructorControladorVisual.Construir(CreadorVisualPersonaje.RutaControlador(nombre), clips, out List<ClipVisual> faltan);
            foreach (ClipVisual f in faltan)
            {
                Debug.LogWarning("[Personaje] " + nombre + ": falta la animación " + f + ".");
            }

            // 4) Material con la textura y prefab.
            Material material = AsegurarMaterialBase(nombre);
            GameObject modelo = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
            // Si el prefab ya existe se edita en su lugar (conserva sus objetos y lo que cuelga de ellos en los prefabs del juego).
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CreadorVisualPersonaje.RutaPrefab(nombre))
                ?? CreadorVisualPersonaje.ArmarPrefab(nombre, modelo, avatar, controlador);
            AjustarPrefab(prefab, material, 0f, 0f, tieneApuntarDeVerdad);
            // 5) La velocidad real de los clips humanoides (el Avatar los reescala) se mide con movimiento de raíz y define el umbral.
            MedirVelocidades(prefab, out float vCaminar, out float vCorrer);
            float umbral = vCaminar > 0f && vCorrer > vCaminar ? Mathf.Lerp(vCaminar, vCorrer, FactorUmbralCorrer) : ReglasVisual.UmbralCorrer;
            ConstructorControladorVisual.Construir(CreadorVisualPersonaje.RutaControlador(nombre), clips, out _, umbral);
            AjustarPrefab(prefab, material, vCaminar, vCorrer, tieneApuntarDeVerdad);
            Debug.Log("[Personaje] T-520 Visual_" + nombre + ": alto " + alto + " m -> escala " + escala + ", Caminar " + vCaminar.ToString("0.00") + " m/s, Correr " + vCorrer.ToString("0.00") + " m/s, umbral " + umbral.ToString("0.00"));
            return prefab;
        }

        public const string NombreSocket = "ManoDer";
        private const string NombreIdle = "Idle";
        private const float MedirCiclosCaminar = 3f;
        private const float MedirCiclosCorrer = 4f;
        private const float PasoMedicion = 1f / 60f;

        /// <summary>Velocidad (m/s) a la que avanza cada clip con el movimiento de raíz del Animator (Humanoid reescala los clips por el avatar).</summary>
        public static void MedirVelocidades(GameObject prefab, out float caminar, out float correr)
        {
            GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            try
            {
                Animator a = inst.GetComponentInChildren<Animator>();
                a.applyRootMotion = true;
                caminar = MedirEstado(a, "Caminar", 1f, MedirCiclosCaminar);
                correr = MedirEstado(a, "Correr", 20f, MedirCiclosCorrer);
            }
            finally
            {
                Object.DestroyImmediate(inst);
            }
        }

        private static float MedirEstado(Animator a, string estado, float velocidad, float ciclos)
        {
            a.Rebind();
            a.SetFloat(ReglasVisual.Velocidad, velocidad);
            a.Play(estado, 0, 0f);
            a.Update(0f);
            AnimationClip clip = null;
            foreach (AnimationClip c in a.runtimeAnimatorController.animationClips)
            {
                if (c.name == estado) { clip = c; }
            }
            float duracion = clip != null ? clip.length * ciclos : 2f;
            Vector3 inicio = a.transform.position;
            float t = 0f;
            while (t < duracion)
            {
                a.Update(PasoMedicion);
                t += PasoMedicion;
            }
            Vector3 d = a.transform.position - inicio;
            return new Vector3(d.x, 0f, d.z).magnitude / t;
        }

        private static ModelImporterClipAnimation Ajustar(ModelImporterClipAnimation d, string nuevoNombre)
        {
            ModelImporterClipAnimation c = d;
            if (nuevoNombre != null)
            {
                c.name = nuevoNombre;
            }
            c.loopPose = true;
            c.lockRootRotation = true;
            c.lockRootHeightY = true;
            c.lockRootPositionXZ = false;
            c.keepOriginalOrientation = true;
            c.keepOriginalPositionY = false;
            c.keepOriginalPositionXZ = true;
            c.heightFromFeet = true;
            return c;
        }

        public const string CarpetaAnimaciones = Carpeta + "/Animaciones";
        private const int TriangulosParaMeshLod = 15000;

        private static Avatar AvatarDe(string nombre)
        {
            foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(RutaModelo(nombre)))
            {
                if (o is Avatar a) { return a; }
            }
            return null;
        }

        /// <summary>Los FBX de animación de la carpeta común (ApuntarPistola.fbx, Golpe.fbx...), configurados Humanoid con el avatar dado.</summary>
        public static Dictionary<ClipVisual, AnimationClip> AnimacionesExtra(Avatar avatar)
        {
            var salida = new Dictionary<ClipVisual, AnimationClip>();
            if (!AssetDatabase.IsValidFolder(CarpetaAnimaciones))
            {
                return salida;
            }
            foreach (string guid in AssetDatabase.FindAssets("t:Model", new[] { CarpetaAnimaciones }))
            {
                string ruta = AssetDatabase.GUIDToAssetPath(guid);
                if (!ReglasVisual.TryClipDeArchivo(Path.GetFileName(ruta), out ClipVisual tipo))
                {
                    Debug.LogWarning("[Personaje] Animación ignorada (nombre no reconocido): " + ruta);
                    continue;
                }
                CreadorVisualPersonaje.ConfigurarAnimacion(ruta, tipo, avatar);
                AnimationClip clip = CreadorVisualPersonaje.CargarClip(ruta);
                if (clip != null)
                {
                    salida[tipo] = clip;
                }
            }
            return salida;
        }

        private static int CuentaTriangulos(string ruta)
        {
            int total = 0;
            foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(ruta))
            {
                if (o is Mesh m) { total += m.triangles.Length / 3; }
            }
            return total;
        }

        /// <summary>Altura (m) del modelo en pose de reposo, medida con el renderer.</summary>
        public static float AltoDelModelo(string rutaFbx)
        {
            GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(rutaFbx);
            GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(go);
            try
            {
                Bounds? total = null;
                foreach (Renderer r in inst.GetComponentsInChildren<Renderer>())
                {
                    total = total.HasValue ? Encapsular(total.Value, r.bounds) : r.bounds;
                }
                return total.HasValue ? total.Value.size.y : 0f;
            }
            finally
            {
                Object.DestroyImmediate(inst);
            }
        }

        private static Bounds Encapsular(Bounds a, Bounds b)
        {
            a.Encapsulate(b);
            return a;
        }

        public static Material AsegurarMaterialBase(string nombre)
        {
            string ruta = RutaMaterialBase(nombre);
            Material m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (m == null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ruta));
                m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(m, ruta);
            }
            Texture2D textura = null;
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { Carpeta + "/" + nombre }))
            {
                textura = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(guid));
                break;
            }
            m.SetTexture("_BaseMap", textura);
            m.SetColor("_BaseColor", Color.white);
            m.SetFloat("_Smoothness", 0.25f);
            m.SetFloat("_Metallic", 0f);
            EditorUtility.SetDirty(m);
            return m;
        }

        private static void AjustarPrefab(GameObject prefab, Material material, float vCaminar, float vCorrer, bool apuntarDeVerdad)
        {
            string ruta = AssetDatabase.GetAssetPath(prefab);
            GameObject contenido = PrefabUtility.LoadPrefabContents(ruta);
            try
            {
                Animator animador = contenido.GetComponentInChildren<Animator>(true);
                foreach (SkinnedMeshRenderer r in contenido.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    r.sharedMaterial = material;
                }
                ApuntadoIK apuntado = animador.GetComponent<ApuntadoIK>();
                if (apuntado == null)
                {
                    apuntado = animador.gameObject.AddComponent<ApuntadoIK>();
                }
                Transform mano = animador.GetBoneTransform(HumanBodyBones.RightHand);
                Transform socket = mano != null ? mano.Find(NombreSocket) : null;
                if (mano != null && socket == null)
                {
                    socket = new GameObject(NombreSocket).transform;
                    socket.SetParent(mano, false);
                }
                apuntado.ConfigurarSocket(socket);
                apuntado.ManosPorIK = !apuntarDeVerdad;
                // El modelo de Tripo mira a -Z: un nodo Giro de 180° lo deja mirando al frente de la raíz (el Modelo conserva su rotación local).
                Transform giro = contenido.transform.Find("Giro");
                if (giro == null)
                {
                    giro = new GameObject("Giro").transform;
                    giro.SetParent(contenido.transform, false);
                    giro.localRotation = Quaternion.Euler(0f, 180f, 0f);
                    animador.transform.SetParent(giro, false);
                }
                contenido.GetComponent<VisualPersonaje>().ConfigurarClips(vCaminar, vCorrer);
                PrefabUtility.SaveAsPrefabAsset(contenido, ruta);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contenido);
            }
        }
    }
}
