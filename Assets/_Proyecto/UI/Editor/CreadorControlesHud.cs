using System.IO;
using MechaCyberpunk.UI.Hud;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MechaCyberpunk.UI.Editor
{
    /// <summary>
    /// T-726: crea UI/Resources/ControlesInterfaz.inputactions (mapa "Interfaz", acción SaltarCinematica con Enter) y UI/Resources/ControlesHud.asset, que
    /// apunta a ese asset y al ControlesAPie.inputactions real del jugador. Idempotente.
    /// Batchmode: -executeMethod MechaCyberpunk.UI.Editor.CreadorControlesHud.Crear
    /// </summary>
    public static class CreadorControlesHud
    {
        private const string RutaInterfaz = "Assets/_Proyecto/UI/Resources/ControlesInterfaz.inputactions";
        private const string RutaAPie = "Assets/_Proyecto/Personaje/Entrada/ControlesAPie.inputactions";
        private const string RutaConfig = "Assets/_Proyecto/UI/Resources/ControlesHud.asset";

        [MenuItem("Mecha Cyberpunk/Crear T-726 (controles del HUD)")]
        public static void Crear()
        {
            if (!File.Exists(RutaInterfaz))
            {
                InputActionAsset nuevo = ScriptableObject.CreateInstance<InputActionAsset>();
                nuevo.name = "ControlesInterfaz";
                InputActionMap mapa = nuevo.AddActionMap(ControlesHud.MapaInterfaz);
                InputAction saltar = mapa.AddAction(ControlesHud.AccionSaltarCinematica, InputActionType.Button);
                saltar.AddBinding("<Keyboard>/enter");
                saltar.AddBinding("<Keyboard>/numpadEnter");
                File.WriteAllText(RutaInterfaz, nuevo.ToJson());
                Object.DestroyImmediate(nuevo);
            }
            AssetDatabase.ImportAsset(RutaInterfaz, ImportAssetOptions.ForceSynchronousImport);
            InputActionAsset interfaz = AssetDatabase.LoadAssetAtPath<InputActionAsset>(RutaInterfaz);
            InputActionAsset aPie = AssetDatabase.LoadAssetAtPath<InputActionAsset>(RutaAPie);
            if (interfaz == null || aPie == null)
            {
                Debug.LogError("[UI] T-726: falta " + (interfaz == null ? RutaInterfaz : RutaAPie));
                return;
            }
            ControlesHud config = AssetDatabase.LoadAssetAtPath<ControlesHud>(RutaConfig);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<ControlesHud>();
                AssetDatabase.CreateAsset(config, RutaConfig);
            }
            config.Llenar(aPie, interfaz);
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            Debug.Log("[UI] T-726 creada: " + RutaConfig);
        }
    }
}
