using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace MechaCyberpunk.IA.Editor
{
    /// <summary>
    /// T-1321: el policía apunta y camina. En Policia.controller el estado ApuntarPistola era un solo clip (pose quieta), así que al avanzar apuntando se deslizaba.
    /// Lo cambia por un árbol 1D sobre Velocidad: quieto = PJ01_Pistola_Apuntar, caminando = PJ01_Pistola_Caminar, corriendo = PJ01_Pistola_Correr (clips que ya existen).
    /// Se puede repetir (si ya es un árbol, lo reconstruye). Batchmode: -executeMethod MechaCyberpunk.IA.Editor.PoliciaApuntarCaminarT1321.Aplicar
    /// </summary>
    public static class PoliciaApuntarCaminarT1321
    {
        public const string RutaControlador = "Assets/Arte/Personajes/Policia/Policia.controller";
        public const string CarpetaClips = "Assets/Arte/Personajes/PJ01/Clips";
        public const string EstadoApuntar = "ApuntarPistola";
        public const string ParametroVelocidad = "Velocidad";
        public const string NombreArbol = "ApuntarPistolaCaminar";
        public const float VelocidadCaminar = 1.4f;
        public const float VelocidadCorrer = 3.6f;

        public static void Aplicar()
        {
            AnimatorController controlador = AssetDatabase.LoadAssetAtPath<AnimatorController>(RutaControlador);
            if (controlador == null)
            {
                Debug.LogError("[IA] T-1321: falta " + RutaControlador);
                return;
            }
            AnimationClip quieto = Clip("PJ01_Pistola_Apuntar");
            AnimationClip camina = Clip("PJ01_Pistola_Caminar");
            AnimationClip corre = Clip("PJ01_Pistola_Correr");
            if (quieto == null || camina == null || corre == null)
            {
                Debug.LogError("[IA] T-1321: faltan clips de PJ01; no se cambia nada.");
                return;
            }
            AnimatorState estado = BuscarEstado(controlador);
            if (estado == null)
            {
                Debug.LogError("[IA] T-1321: Policia.controller no tiene el estado " + EstadoApuntar);
                return;
            }
            if (estado.motion is BlendTree viejo)
            {
                AssetDatabase.RemoveObjectFromAsset(viejo);
                Object.DestroyImmediate(viejo, true);
            }
            BlendTree arbol = new BlendTree
            {
                name = NombreArbol,
                blendType = BlendTreeType.Simple1D,
                blendParameter = ParametroVelocidad,
                useAutomaticThresholds = false,
                hideFlags = HideFlags.HideInHierarchy
            };
            arbol.AddChild(quieto, 0f);
            arbol.AddChild(camina, VelocidadCaminar);
            arbol.AddChild(corre, VelocidadCorrer);
            AssetDatabase.AddObjectToAsset(arbol, controlador);
            estado.motion = arbol;
            EditorUtility.SetDirty(controlador);
            AssetDatabase.SaveAssets();
            Debug.Log("[IA] T-1321: el policía camina apuntando (árbol de Velocidad en " + EstadoApuntar + ").");
        }

        private static AnimatorState BuscarEstado(AnimatorController controlador)
        {
            foreach (AnimatorControllerLayer capa in controlador.layers)
            {
                foreach (ChildAnimatorState hijo in capa.stateMachine.states)
                {
                    if (hijo.state.name == EstadoApuntar)
                    {
                        return hijo.state;
                    }
                }
            }
            return null;
        }

        private static AnimationClip Clip(string nombre)
        {
            foreach (string guid in AssetDatabase.FindAssets(nombre + " t:Model", new[] { CarpetaClips }))
            {
                string ruta = AssetDatabase.GUIDToAssetPath(guid);
                foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(ruta))
                {
                    if (o is AnimationClip c && c.name == nombre)
                    {
                        return c;
                    }
                }
            }
            return null;
        }
    }
}
