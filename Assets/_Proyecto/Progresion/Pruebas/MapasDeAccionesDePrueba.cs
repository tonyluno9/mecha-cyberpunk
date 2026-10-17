#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.InputSystem;

namespace MechaCyberpunk.Progresion.Pruebas
{
    /// <summary>
    /// Limpia el estado de resolución de los mapas de acciones (InputActionAsset) entre pruebas de PlayMode.
    /// Existe porque los .inputactions son assets: guardan qué controles resolvieron en la prueba anterior, y el InputTestFixture
    /// reinicia el Input System y libera esos controles. Sin descartar ese estado, la prueba siguiente no ve el teclado nuevo o
    /// hereda controles liberados (ArgumentNullException statePtr). Cambiar `devices` en un mapa apagado descarta ese estado.
    /// Llámalo desde cualquier InputTestFixture que instancie un prefab con .inputactions o cargue una escena de Tony:
    /// <see cref="Reiniciar"/> en el Setup (tras añadir los dispositivos), <see cref="AntesDelTearDown"/> justo antes de
    /// base.TearDown() y <see cref="DespuesDelTearDown"/> justo después.
    /// </summary>
    public static class MapasDeAccionesDePrueba
    {
        /// <summary>Setup: descarta el estado de todos los mapas con los dispositivos nuevos del fixture.</summary>
        public static void Reiniciar(params InputDevice[] dispositivos)
        {
            Descartar(dispositivos);
        }

        /// <summary>TearDown, antes de base.TearDown(): apaga toda acción prendida y descarta el estado con los dispositivos del fixture.</summary>
        public static void AntesDelTearDown(params InputDevice[] dispositivos)
        {
            InputSystem.DisableAllEnabledActions();
            Descartar(dispositivos);
        }

        /// <summary>TearDown, después de base.TearDown(): descarta el estado que apuntaba a controles ya liberados.</summary>
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
#endif
