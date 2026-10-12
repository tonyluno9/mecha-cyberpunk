using UnityEngine;

namespace MechaCyberpunk.Mecha
{
    /// <summary>Tiempos, alturas y textos del encendido de 3 pasos de la cabina (T-065).</summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Config encendido mecha", fileName = "ConfigEncendido")]
    public class ConfigEncendido : ScriptableObject
    {
        [Tooltip("Paso 1: lo que tarda en animarse después de confirmar la entrada (s).")]
        [Min(0f)] [SerializeField] private float segundosPaso1 = 1f;
        [Tooltip("Paso 2: lo que hay que mantener la tecla seguida para levantar el mecha (s).")]
        [Min(0f)] [SerializeField] private float segundosMantenerPaso2 = 1.5f;
        [Tooltip("Paso 3: lo que tarda en subir la persiana después de desbloquear el cristal (s).")]
        [Min(0f)] [SerializeField] private float segundosPaso3 = 1.2f;
        [Tooltip("Pausa entre un paso y el siguiente (s).")]
        [Min(0f)] [SerializeField] private float pausaEntrePasos = 0.3f;
        [Tooltip("Desplazamiento vertical de la cabina agachada respecto a su altura normal (m, negativo baja).")]
        [SerializeField] private float desplazamientoAgachado = -1.2f;
        [Tooltip("Cuánto sube la persiana al abrir el cristal (m).")]
        [Min(0f)] [SerializeField] private float alturaPersiana = 1.1f;
        [SerializeField] private string textoPaso1 = "PASO 1/3 · [F] CONFIRMAR ENTRADA";
        [SerializeField] private string textoPaso2 = "PASO 2/3 · MANTÉN [ESPACIO] · LEVANTAR";
        [SerializeField] private string textoPaso3 = "PASO 3/3 · [Q] DESBLOQUEAR CRISTAL";
        [SerializeField] private string textoEncendido = "SISTEMAS EN LÍNEA";

        public float SegundosPaso1 => segundosPaso1;
        public float SegundosMantenerPaso2 => segundosMantenerPaso2;
        public float SegundosPaso3 => segundosPaso3;
        public float PausaEntrePasos => pausaEntrePasos;
        public float DesplazamientoAgachado => desplazamientoAgachado;
        public float AlturaPersiana => alturaPersiana;
        public string TextoPaso1 => textoPaso1;
        public string TextoPaso2 => textoPaso2;
        public string TextoPaso3 => textoPaso3;
        public string TextoEncendido => textoEncendido;
    }
}
