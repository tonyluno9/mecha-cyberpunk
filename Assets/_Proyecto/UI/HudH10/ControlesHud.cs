using UnityEngine;
using UnityEngine.InputSystem;

namespace MechaCyberpunk.UI.Hud
{
    /// <summary>
    /// T-726: dónde están las acciones reales del Input System que usa la interfaz: el mapa "APie" del jugador (para mostrar sus teclas aunque todavía
    /// no haya un jugador cargado, como en el menú de inicio) y el mapa "Interfaz" (saltar cinemática). El asset es UI/Resources/ControlesHud.asset
    /// (lo crea CreadorControlesHud); <see cref="Actual"/> lo carga una vez.
    /// </summary>
    public class ControlesHud : ScriptableObject
    {
        public const string NombreRecurso = "ControlesHud";
        public const string MapaAPie = "APie";
        public const string MapaInterfaz = "Interfaz";
        public const string AccionSaltarCinematica = "SaltarCinematica";

        [SerializeField] private InputActionAsset aPie;
        [SerializeField] private InputActionAsset interfaz;

        private static ControlesHud actual;

        public static ControlesHud Actual
        {
            get
            {
                if (actual == null)
                {
                    actual = Resources.Load<ControlesHud>(NombreRecurso);
                }
                return actual;
            }
        }

        public InputActionAsset AssetInterfaz => interfaz;

        /// <summary>La acción del mapa "APie" con ese nombre (la del asset, sin instanciar), o null.</summary>
        public InputAction AccionAPie(string nombre) => aPie != null ? aPie.FindAction(MapaAPie + "/" + nombre, false) : null;

        /// <summary>La acción del mapa "Interfaz" con ese nombre (la del asset, sin instanciar), o null.</summary>
        public InputAction AccionInterfaz(string nombre) => interfaz != null ? interfaz.FindAction(MapaInterfaz + "/" + nombre, false) : null;

#if UNITY_EDITOR
        /// <summary>Solo para el creador del editor.</summary>
        public void Llenar(InputActionAsset aPieAsset, InputActionAsset interfazAsset)
        {
            aPie = aPieAsset;
            interfaz = interfazAsset;
        }
#endif
    }
}
