using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Visual.Editor
{
    /// <summary>
    /// T-561: pone el override del papel (sobre el controlador base) en cada prefab Visual_*, apaga el IK de manos de emergencia,
    /// agrega el IK de pies y mirada, y ajusta las velocidades de los clips de caminar y correr medidas del propio clip. Se puede repetir.
    /// Batchmode: -executeMethod MechaCyberpunk.Personaje.Visual.Editor.ConectarControladorBase.Conectar
    /// </summary>
    public static class ConectarControladorBase
    {
        /// <summary>Papel de cada modelo. Un modelo sin papel propio usa NPC.</summary>
        public static readonly (string Modelo, string Papel, bool MiraAlJugador)[] Reparto =
        {
            ("woman", "Jugador", false), ("woman2", "Aliados", true), ("man", "SKULLS", true), ("SkullHombre", "SKULLS", true),
            ("RobotHombre", "Robots", false), ("GuardiaNova", "Guardias", true), ("Doctor", "NPC", true),
            ("Hacker", "SNAKES", true), ("Inventor", "Profes", true),
        };

        [MenuItem("Mecha Cyberpunk/Personajes/T-561 Conectar controlador base")]
        public static void Conectar()
        {
            ConstructorControladorBase.Construir();
            // T-562: velocidades medidas con MedicionClips (el pie apoyado respecto de la cadera), no con el Medir viejo que daba 1.18 y 2.11.
            float caminar = ReglasAnimacion.VelocidadClipCaminar;
            float correr = ReglasAnimacion.VelocidadClipCorrer;
            FijarEscalaAgachado();
            foreach ((string modelo, string papel, bool mira) in Reparto)
            {
                ConectarPrefab(modelo, papel, mira, caminar, correr);
            }
            AssetDatabase.SaveAssets();
        }

        private const string RutaDatosCubierta = "Assets/_Proyecto/Personaje/Datos/Resources/DatosCubierta.asset";

        /// <summary>T-562: Cobertura_Entrar ya trae la pose agachada; aplanar el modelo encima la deformaba.</summary>
        public static void FijarEscalaAgachado()
        {
            Object datos = AssetDatabase.LoadAssetAtPath<Object>(RutaDatosCubierta);
            var so = new SerializedObject(datos);
            so.FindProperty("escalaAgachado").floatValue = 1f;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(datos);
        }

        private static void ConectarPrefab(string modelo, string papel, bool mira, float caminar, float correr)
        {
            string ruta = CreadorVisualPersonaje.RutaPrefab(modelo);
            GameObject contenido = PrefabUtility.LoadPrefabContents(ruta);
            try
            {
                Animator animador = contenido.GetComponentInChildren<Animator>(true);
                animador.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(ConstructorControladorBase.RutaOverride(papel));
                animador.applyRootMotion = false;
                ApuntadoIK apuntado = contenido.GetComponentInChildren<ApuntadoIK>(true);
                if (apuntado != null)
                {
                    apuntado.ManosPorIK = false;
                }
                IKPiesYMirada ik = animador.GetComponent<IKPiesYMirada>();
                if (ik == null)
                {
                    ik = animador.gameObject.AddComponent<IKPiesYMirada>();
                }
                ik.MirarAlJugador = mira;
                contenido.GetComponent<VisualPersonaje>().ConfigurarClips(caminar, correr, ReglasAnimacion.RapidezMaximaLocomocion);
                if (modelo != "woman" && contenido.GetComponent<VariacionNPC>() == null)
                {
                    contenido.AddComponent<VariacionNPC>();
                }
                PrefabUtility.SaveAsPrefabAsset(contenido, ruta);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contenido);
            }
        }

        /// <summary>
        /// Velocidad (m/s) a la que avanza un clip In Place: lo que se mueve hacia atrás el pie más bajo (el que está apoyado) respecto del cuerpo.
        /// Se muestrea el clip sobre el modelo woman, que mide lo mismo que los demás.
        /// </summary>
        public static float Medir(string clip)
        {
            AnimationClip c = ConstructorControladorBase.Clip(clip);
            GameObject modelo = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(CreadorVisualPersonaje.RutaPrefab("woman")));
            try
            {
                Animator a = modelo.GetComponentInChildren<Animator>();
                a.enabled = false;
                Transform izq = a.GetBoneTransform(HumanBodyBones.LeftFoot);
                Transform der = a.GetBoneTransform(HumanBodyBones.RightFoot);
                Transform cadera = a.GetBoneTransform(HumanBodyBones.Hips);
                const float paso = 1f / 60f;
                var posiciones = new List<Vector3>();
                var apoyado = new List<bool>();
                for (float t = 0f; t <= c.length; t += paso)
                {
                    c.SampleAnimation(a.gameObject, t);
                    Vector3 li = a.transform.InverseTransformPoint(izq.position) - a.transform.InverseTransformPoint(cadera.position) + Vector3.up * a.transform.InverseTransformPoint(cadera.position).y;
                    Vector3 ld = a.transform.InverseTransformPoint(der.position) - a.transform.InverseTransformPoint(cadera.position) + Vector3.up * a.transform.InverseTransformPoint(cadera.position).y;
                    bool izqBaja = li.y < ld.y;
                    Vector3 p = izqBaja ? li : ld;
                    posiciones.Add(new Vector3(p.x, 0f, p.z));
                    apoyado.Add(izqBaja);
                }
                float suma = 0f;
                int n = 0;
                for (int i = 1; i < posiciones.Count; i++)
                {
                    if (apoyado[i] != apoyado[i - 1])
                    {
                        continue;
                    }
                    suma += (posiciones[i] - posiciones[i - 1]).magnitude / paso;
                    n++;
                }
                return n > 0 ? suma / n : 0f;
            }
            finally
            {
                Object.DestroyImmediate(modelo);
            }
        }
    }
}
