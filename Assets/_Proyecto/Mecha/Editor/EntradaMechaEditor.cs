using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MechaCyberpunk.Mecha.Editor
{
    /// <summary>
    /// Agrega acciones a ControlesMecha.inputactions sin regenerar el asset: carga el archivo, agrega solo las acciones que faltan y
    /// conserva los id de las existentes, para no romper las InputActionReference del prefab. Idempotente y reutilizable.
    /// </summary>
    public static class EntradaMechaEditor
    {
        public const string RutaEntrada = "Assets/_Proyecto/Mecha/Entrada/ControlesMecha.inputactions";
        public const string NombreMapa = "Mecha";

        /// <summary>
        /// Agrega la acción con un binding a la ruta dada si el mapa no la tiene (el mapa se crea si no existe; por defecto
        /// "Mecha"). Devuelve true si la agregó.
        /// </summary>
        public static bool AgregarAccion(string nombre, string ruta, InputActionType tipo = InputActionType.Button, string mapaNombre = NombreMapa)
        {
            InputActionAsset asset = InputActionAsset.FromJson(File.ReadAllText(RutaEntrada));
            try
            {
                InputActionMap mapa = asset.FindActionMap(mapaNombre) ?? asset.AddActionMap(mapaNombre);
                if (mapa.FindAction(nombre) != null)
                {
                    return false;
                }
                mapa.AddAction(nombre, tipo, ruta);
                File.WriteAllText(RutaEntrada, asset.ToJson());
            }
            finally
            {
                Object.DestroyImmediate(asset);
            }
            AssetDatabase.ImportAsset(RutaEntrada);
            return true;
        }

        /// <summary>La InputActionReference de una acción del asset (existe tras importarlo).</summary>
        public static InputActionReference Referencia(string nombreAccion)
        {
            return AssetDatabase.LoadAllAssetsAtPath(RutaEntrada)
                .OfType<InputActionReference>()
                .First(r => r.action != null && r.action.name == nombreAccion);
        }
    }
}
