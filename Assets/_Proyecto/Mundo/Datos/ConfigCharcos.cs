using UnityEngine;

namespace MechaCyberpunk.Mundo.Datos
{
    /// <summary>
    /// Presentación de los charcos que reflejan (docs/plan-look-ciudad.md §2.3): la cámara del reflejo planar que usa
    /// <see cref="Ambiente.ReflejoCharcos"/> y que leen los materiales MechaCyberpunk/Mundo/Charco.
    /// </summary>
    [CreateAssetMenu(fileName = "ConfigCharcos", menuName = "Mecha Cyberpunk/Mundo/Config Charcos")]
    public class ConfigCharcos : ScriptableObject
    {
        [Tooltip("Altura del piso de la calle si no hay piso debajo de la cámara (m).")]
        [SerializeField] private float alturaPlano = 0.02f;
        [Tooltip("El plano del reflejo sigue al piso que hay debajo de la cámara solo entre estas alturas (m): así no lo mueve el techo de un carro.")]
        [SerializeField] private float pisoMinimo = -0.5f;
        [SerializeField] private float pisoMaximo = 0.6f;
        [Tooltip("Hasta dónde busca el piso debajo de la cámara (m).")]
        [Min(0.5f)] [SerializeField] private float buscarPiso = 8f;
        [Tooltip("Tamaño de la textura del reflejo respecto a la pantalla (fracción).")]
        [Range(0.25f, 1f)] [SerializeField] private float escalaResolucion = 0.5f;
        [Tooltip("Lo más lejos que se ve en el reflejo (m).")]
        [Min(5f)] [SerializeField] private float lejos = 120f;
        [Tooltip("Campo de visión extra de la cámara del reflejo para que no se corte en las orillas (°).")]
        [Range(0f, 20f)] [SerializeField] private float margenCampo = 6f;
        [Tooltip("El plano de corte va así de abajo del piso (m), para que el pie de las paredes sí salga en el reflejo.")]
        [Range(0f, 0.2f)] [SerializeField] private float separacionCorte = 0.02f;

        public float AlturaPlano => alturaPlano;
        public float PisoMinimo => pisoMinimo;
        public float PisoMaximo => pisoMaximo;
        public float BuscarPiso => buscarPiso;
        public float EscalaResolucion => escalaResolucion;
        public float Lejos => lejos;
        public float MargenCampo => margenCampo;
        public float SeparacionCorte => separacionCorte;
    }
}
