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
        [Tooltip("Opción de la pantalla del banco: todo el daño. {0} = costo con $.")]
        [SerializeField] private string formatoOpcionTodo = "REPARAR TODO · {0}";
        [Tooltip("Opción de la pantalla del banco: la mitad del daño. {0} = costo con $.")]
        [SerializeField] private string formatoOpcionMitad = "REPARAR LA MITAD · {0}";
        [Tooltip("Opción de la pantalla del banco: lo que alcanza. {0} = costo con $, {1} = puntos de vida que sube.")]
        [SerializeField] private string formatoOpcionConLoQueTienes = "CON LO QUE TIENES · {0} (+{1})";
        [Tooltip("Aviso del banco sin daño, cuando el mecha tiene taller de piezas (T-168). El HUD le pone el prefijo \"E · \".")]
        [SerializeField] private string textoPiezas = "Taller · piezas";
        [Tooltip("Última opción de la pantalla del banco: abre las piezas del mecha (T-168).")]
        [SerializeField] private string textoOpcionPiezas = "PIEZAS DEL MECHA";
        [Tooltip("Distancia máxima al estacionamiento (m) para contar el mecha como en el taller cuando no tiene LlamadoMecha.")]
        [Min(0.5f)] [SerializeField] private float radioEnTaller = 3f;

        public string TextoPagarRenta => textoPagarRenta;
        public string TextoBloqueado => textoBloqueado;
        public string TextoMechaFuera => textoMechaFuera;
        public string TextoSinDanio => textoSinDanio;
        public string TextoReparar => textoReparar;
        public string FormatoOpcionTodo => formatoOpcionTodo;
        public string FormatoOpcionMitad => formatoOpcionMitad;
        public string FormatoOpcionConLoQueTienes => formatoOpcionConLoQueTienes;
        public string TextoPiezas => textoPiezas;
        public string TextoOpcionPiezas => textoOpcionPiezas;
        public float RadioEnTaller => radioEnTaller;
    }
}
