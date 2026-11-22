using UnityEngine;

namespace MechaCyberpunk.Core
{
    /// <summary>
    /// T-1620 (T8): topes y tiempos del director de respuesta (policía y NOVA). Vive en Core/Respuesta/Resources/ConfigRespuesta (lo crea
    /// <c>AplicarRespuestaT1620.Aplicar</c>). Sin el asset el director usa <see cref="ParametrosRespuesta.PorOmision"/>.
    /// </summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Core/Config respuesta", fileName = "ConfigRespuesta")]
    public class ConfigRespuesta : ScriptableObject
    {
        public const string Recurso = "ConfigRespuesta";

        [SerializeField] private ParametrosRespuesta parametros = ParametrosRespuesta.PorOmision();

        public ParametrosRespuesta Parametros => parametros;

        /// <summary>Para el editor y las pruebas.</summary>
        public void Reiniciar() => parametros = ParametrosRespuesta.PorOmision();
    }
}
