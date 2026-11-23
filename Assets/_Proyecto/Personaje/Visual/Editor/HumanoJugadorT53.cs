using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Visual.Editor
{
    /// <summary>
    /// T-53: todos los humanos (SKULLS, SNAKES, policías, guardias humanos de Nova y civiles) usan EXACTAMENTE las animaciones del jugador.
    /// Crea Override_HumanoJugador (copia clip por clip de Override_Jugador) y se lo pone a los visuales humanos, que además se marcan ComoJugador
    /// (VisualPersonaje.ConfigurarComoJugador: mismas velocidades de clip, umbrales, IK y rapideces). Se puede repetir.
    /// Orden: 1) ConstructorControladorBase.Construir (solo si cambió el controlador)  2) Aplicar.
    /// Batchmode: -executeMethod MechaCyberpunk.Personaje.Visual.Editor.HumanoJugadorT53.Aplicar
    /// </summary>
    public static class HumanoJugadorT53
    {
        public const string NombreOverride = "HumanoJugador";
        public static readonly string RutaOverride = ConstructorControladorBase.RutaOverride(NombreOverride);
        public const string CarpetaVisuales = "Assets/_Proyecto/Personaje/Visual/Prefabs/";
        public const string RutaNota = "docs/capturas/_tareas/T53/clips_override.txt";

        /// <summary>
        /// Visuales humanos que pasan a Override_HumanoJugador. Quedan fuera (lo propio): robots y guardias de Nova (E15, E16, E18, perro y mesera robot), los jefes
        /// (Override_Jefe), el matón E20 (golpe de brazo robótico), el hacker (teclea) y los visuales del jugador (woman, PJ01, Prueba).
        /// </summary>
        public static readonly string[] Visuales =
        {
            "Visual_SkullHombre", "Visual_E19_PandilleroSkulls_a_2", "Visual_E19_PandilleroSkulls_b", "Visual_man",
            "Visual_E10_PandilleroSnakes", "Visual_GuardiaNova", "Visual_E12_GuardiaPesadoNova",
            "Visual_Policia", "Visual_Mecanico",
            "Visual_Doctor", "Visual_Inventor", "Visual_AL10", "Visual_AL11", "Visual_woman2",
            "Visual_NP07_Pescador_a", "Visual_NP07_Pescador_b", "Visual_NP08_Turista_a", "Visual_NP08_Turista_b", "Visual_NP09_Corredor_a", "Visual_NP09_Corredor_b",
            "Visual_NP10_Elotero_a", "Visual_NP11_Estibador_a", "Visual_NP11_Estibador_b", "Visual_NP12_Abuela_a", "Visual_NP13_Repartidor_a", "Visual_NP13_Repartidor_b",
            "Visual_NP14_Oficinista_a", "Visual_NP14_Oficinista_b",
        };

        [MenuItem("Mecha Cyberpunk/Personajes/T-53 Humanos con las animaciones del jugador")]
        public static void Aplicar()
        {
            AnimatorOverrideController ov = CrearOverride();
            int cambiados = 0;
            if (ov != null)
            {
                foreach (string nombre in Visuales)
                {
                    if (AsignarAlVisual(nombre, ov))
                    {
                        cambiados++;
                    }
                }
            }
            EscribirNota(ov);
            AssetDatabase.SaveAssets();
            Debug.Log("[Personaje] T-53: Override_HumanoJugador listo y asignado a " + cambiados + " de " + Visuales.Length + " visuales.");
        }

        /// <summary>Pares (clip base, clip) de un override, por nombre del clip base.</summary>
        public static Dictionary<string, AnimationClip> Pares(AnimatorOverrideController o)
        {
            var lista = new List<KeyValuePair<AnimationClip, AnimationClip>>();
            o.GetOverrides(lista);
            var mapa = new Dictionary<string, AnimationClip>();
            foreach (KeyValuePair<AnimationClip, AnimationClip> par in lista)
            {
                if (par.Key != null)
                {
                    mapa[par.Key.name] = par.Value != null ? par.Value : par.Key;
                }
            }
            return mapa;
        }

        public static AnimatorOverrideController CrearOverride()
        {
            var jugador = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(ConstructorControladorBase.RutaOverride("Jugador"));
            if (jugador == null)
            {
                Debug.LogError("[Personaje] T-53: falta Override_Jugador (corre ConstructorControladorBase.Construir).");
                return null;
            }
            var ov = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(RutaOverride);
            if (ov == null)
            {
                ov = new AnimatorOverrideController(jugador.runtimeAnimatorController);
                AssetDatabase.CreateAsset(ov, RutaOverride);
            }
            ov.runtimeAnimatorController = jugador.runtimeAnimatorController;
            Dictionary<string, AnimationClip> delJugador = Pares(jugador);
            var pares = new List<KeyValuePair<AnimationClip, AnimationClip>>();
            ov.GetOverrides(pares);
            for (int i = 0; i < pares.Count; i++)
            {
                AnimationClip clave = pares[i].Key;
                pares[i] = new KeyValuePair<AnimationClip, AnimationClip>(clave, delJugador.TryGetValue(clave.name, out AnimationClip clip) ? clip : clave);
            }
            ov.ApplyOverrides(pares);
            EditorUtility.SetDirty(ov);
            return ov;
        }

        private static bool AsignarAlVisual(string nombre, AnimatorOverrideController ov)
        {
            string ruta = CarpetaVisuales + nombre + ".prefab";
            if (!File.Exists(ruta))
            {
                Debug.LogWarning("[Personaje] T-53: no existe " + ruta);
                return false;
            }
            GameObject raiz = PrefabUtility.LoadPrefabContents(ruta);
            try
            {
                Animator animador = raiz.GetComponentInChildren<Animator>(true);
                VisualPersonaje visual = raiz.GetComponent<VisualPersonaje>();
                if (animador == null || visual == null)
                {
                    Debug.LogWarning("[Personaje] T-53: " + nombre + " no tiene Animator o VisualPersonaje.");
                    return false;
                }
                if (animador.avatar != null && !animador.avatar.isHuman)
                {
                    Debug.LogWarning("[Personaje] T-53: " + nombre + " NO es Humanoid (Generic): los clips del jugador no le sirven.");
                    return false;
                }
                animador.runtimeAnimatorController = ov;
                visual.ComoJugador = true;
                PrefabUtility.SaveAsPrefabAsset(raiz, ruta);
                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
        }

        private static void EscribirNota(AnimatorOverrideController ov)
        {
            if (ov == null)
            {
                return;
            }
            var sb = new StringBuilder("# clip base del controlador -> clip del override HumanoJugador (igual al del jugador)\n");
            foreach (KeyValuePair<string, AnimationClip> par in Pares(ov).OrderBy(p => p.Key))
            {
                sb.Append(par.Key).Append(" -> ").Append(par.Value != null ? par.Value.name : "(sin clip)").Append('\n');
            }
            Directory.CreateDirectory(Path.GetDirectoryName(RutaNota));
            File.WriteAllText(RutaNota, sb.ToString());
        }
    }
}
