using UnityEngine;

namespace MechaCyberpunk.Mecha
{
    /// <summary>Textos del banco del taller (T-109). {0} es el dinero con formato ("$650").</summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Config banco taller", fileName = "ConfigBancoTaller")]
    public class ConfigBancoTaller : ScriptableObject
    {
        [SerializeField] private string textoPagarRenta = "Pagar renta · {0}";
        [SerializeField] private string textoBloqueado = "TALLER BLOQUEADO · debes {0} de renta";
        [SerializeField] private string textoMechaFuera = "Tu mecha no está en el taller";
        [SerializeField] private string textoSinDanio = "Mecha sin daños";
        [SerializeField] private string textoReparar = "Reparar mecha · {0}";
        [Tooltip("Distancia máxima al estacionamiento (m) para contar el mecha como en el taller cuando no tiene LlamadoMecha.")]
        [Min(0.5f)] [SerializeField] private float radioEnTaller = 3f;

        public string TextoPagarRenta => textoPagarRenta;
        public string TextoBloqueado => textoBloqueado;
        public string TextoMechaFuera => textoMechaFuera;
        public string TextoSinDanio => textoSinDanio;
        public string TextoReparar => textoReparar;
        public float RadioEnTaller => radioEnTaller;
    }
}
