using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.Personaje
{
    /// <summary>Parámetros del apuntado asistido (T-424): radio de captura y fuerza por nivel, alcance y efecto con el zoom del sniper.</summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Personaje/Apuntado asistido", fileName = "DatosApuntadoAsistido")]
    public class DatosApuntadoAsistido : ScriptableObject
    {
        [Tooltip("Radio de captura alrededor del centro de la retícula con SUAVE (°).")]
        [Range(0.5f, 20f)] [SerializeField] private float radioSuave = 4f;
        [Tooltip("Radio de captura con FUERTE (°).")]
        [Range(0.5f, 30f)] [SerializeField] private float radioFuerte = 8f;
        [Tooltip("Fuerza del jalón con SUAVE (1/s; el error baja con esa rapidez).")]
        [Min(0f)] [SerializeField] private float fuerzaSuave = 1.5f;
        [Tooltip("Fuerza del jalón con FUERTE (1/s).")]
        [Min(0f)] [SerializeField] private float fuerzaFuerte = 4f;
        [Tooltip("Giro máximo por jalón (°/s): nunca es un salto.")]
        [Min(1f)] [SerializeField] private float maximoGradosPorSegundo = 40f;
        [Tooltip("Radio de la marca de la retícula con el asistido APAGADO (°): solo cambia de color, no jala.")]
        [Range(0.5f, 10f)] [SerializeField] private float radioMarca = 2f;
        [Tooltip("Distancia máxima a la que se asiste (m).")]
        [Min(1f)] [SerializeField] private float distanciaMaxima = 40f;
        [Tooltip("Altura del punto al que se apunta sobre los pies del enemigo (m).")]
        [Min(0f)] [SerializeField] private float alturaPecho = 1.2f;
        [Tooltip("Parte de la fuerza que queda con el zoom completo del sniper (0 = nada).")]
        [Range(0f, 1f)] [SerializeField] private float factorConZoom = 0.1f;

        public float RadioMarca => radioMarca;
        public float DistanciaMaxima => distanciaMaxima;
        public float AlturaPecho => alturaPecho;
        public float MaximoGradosPorSegundo => maximoGradosPorSegundo;
        public float FactorConZoom => factorConZoom;

        /// <summary>Radio de captura (°) del nivel; apagado usa solo el radio de la marca.</summary>
        public float Radio(NivelApuntadoAsistido nivel)
        {
            return Mathf.Max(radioMarca, nivel == NivelApuntadoAsistido.Suave ? radioSuave : nivel == NivelApuntadoAsistido.Fuerte ? radioFuerte : 0f);
        }

        /// <summary>Fuerza del jalón (1/s) del nivel; 0 si está apagado.</summary>
        public float Fuerza(NivelApuntadoAsistido nivel)
        {
            return nivel == NivelApuntadoAsistido.Suave ? fuerzaSuave : nivel == NivelApuntadoAsistido.Fuerte ? fuerzaFuerte : 0f;
        }
    }
}
