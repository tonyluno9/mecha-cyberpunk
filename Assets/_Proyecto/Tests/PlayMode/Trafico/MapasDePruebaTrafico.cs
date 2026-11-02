using UnityEditor;
using UnityEngine.InputSystem;

namespace MechaCyberpunk.Tests.Trafico
{
    /// <summary>
    /// Copia de Progresion.Pruebas.MapasDeAccionesDePrueba para las pruebas de Trafico (no pueden referenciar Progresion).
    /// Los .inputactions son assets: guardan qué controles resolvieron en la prueba anterior. Sin descartar ese estado, después de
    /// una prueba que carga Ciudad_Prologo el jugador y los carros dejan de ver las teclas del teclado nuevo.
    /// <see cref="Reiniciar"/> en el Setup (tras añadir los dispositivos), <see cref="AntesDelTearDown"/> antes de base.TearDown()
    /// y <see cref="DespuesDelTearDown"/> después.
    /// </summary>
    public static class MapasDePruebaTrafico
    {
        public static void Reiniciar(params InputDevice[] dispositivos)
        {
            Descartar(dispositivos);
        }

        public static void AntesDelTearDown(params InputDevice[] dispositivos)
        {
            InputSystem.DisableAllEnabledActions();
            Descartar(dispositivos);
        }

        public static void DespuesDelTearDown()
        {
            Descartar(new InputDevice[0]);
        }

        private static void Descartar(InputDevice[] dispositivos)
        {
            foreach (string guid in AssetDatabase.FindAssets("t:InputActionAsset"))
            {
                InputActionAsset asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset == null)
                {
                    continue;
                }
                asset.Disable();
                asset.devices = dispositivos;
                asset.devices = null;
            }
        }
    }
}
