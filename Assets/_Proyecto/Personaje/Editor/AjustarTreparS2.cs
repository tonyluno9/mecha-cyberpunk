using System.Linq;
using MechaCyberpunk.Personaje.Movimiento;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MechaCyberpunk.Personaje.Editor
{
    /// <summary>
    /// S2: crea ConfigTrepar_Base.asset y agrega TreparAPie al prefab JugadorAPie existente, sin regenerarlo.
    /// Batchmode: -executeMethod MechaCyberpunk.Personaje.Editor.AjustarTreparS2.Ajustar
    /// </summary>
    public static class AjustarTreparS2
    {
        private const string RutaConfig = "Assets/_Proyecto/Personaje/Datos/ConfigTrepar_Base.asset";
        private const string RutaPrefab = "Assets/_Proyecto/Personaje/Prefabs/JugadorAPie.prefab";
        private const string RutaEntrada = "Assets/_Proyecto/Personaje/Entrada/ControlesAPie.inputactions";

        [MenuItem("Mecha Cyberpunk/Ajustar S2 trepar")]
        public static void Ajustar()
        {
            ConfigTrepar config = AssetDatabase.LoadAssetAtPath<ConfigTrepar>(RutaConfig);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<ConfigTrepar>();
                AssetDatabase.CreateAsset(config, RutaConfig);
            }

            GameObject raiz = PrefabUtility.LoadPrefabContents(RutaPrefab);
            try
            {
                TreparAPie trepar = raiz.GetComponent<TreparAPie>();
                if (trepar == null)
                {
                    trepar = raiz.AddComponent<TreparAPie>();
                }
                SerializedObject so = new SerializedObject(trepar);
                so.FindProperty("config").objectReferenceValue = config;
                so.FindProperty("mover").objectReferenceValue = Referencia("Mover");
                so.FindProperty("saltar").objectReferenceValue = Referencia("Saltar");
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefab);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[Personaje] TreparAPie agregado a JugadorAPie.prefab (S2).");
        }

        private static InputActionReference Referencia(string nombreAccion)
        {
            return AssetDatabase.LoadAllAssetsAtPath(RutaEntrada)
                .OfType<InputActionReference>()
                .First(r => r.action != null && r.action.name == nombreAccion);
        }
    }
}
