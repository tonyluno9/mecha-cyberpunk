using UnityEngine;

namespace MechaCyberpunk.VR
{
    /// <summary>
    /// Assets que usa el modo VR (Resources/RecursosVR). Lo llena el menú Mecha Cyberpunk/VR/Configurar VR
    /// con los prefabs de los Starter Assets del XR Interaction Toolkit.
    /// </summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/VR/Recursos VR")]
    public class RecursosVR : ScriptableObject
    {
        public const string Ruta = "RecursosVR";

        [Tooltip("Modelo del control izquierdo (Starter Assets: XR Controller Left).")]
        public GameObject controlIzquierdo;
        [Tooltip("Modelo del control derecho (Starter Assets: XR Controller Right).")]
        public GameObject controlDerecho;
        [Tooltip("Viñeta anti-mareo (Starter Assets: TunnelingVignette).")]
        public GameObject vinetaTunel;
        [Tooltip("Simulador de XR (sample XR Device Simulator), solo para probar sin visor con -vrsim.")]
        public GameObject simuladorXR;
        [Tooltip("Acciones por defecto de XRI (Starter Assets), para que los modelos de los controles animen sus botones.")]
        public UnityEngine.InputSystem.InputActionAsset accionesXRI;

        private static RecursosVR cargados;

        public static RecursosVR Cargar()
        {
            if (cargados == null)
            {
                cargados = Resources.Load<RecursosVR>(Ruta);
            }
            return cargados;
        }
    }
}
