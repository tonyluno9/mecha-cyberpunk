using MechaCyberpunk.Mundo.Ambiente;
using UnityEngine;

namespace MechaCyberpunk.Mundo.Datos
{
    /// <summary>Sensación de los letreros de neón del barrio: cuánto bajan de día y cómo parpadean los viejos.</summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Config neón", fileName = "ConfigNeon_Nuevo")]
    public class ConfigNeon : ScriptableObject
    {
        [Tooltip("Fracción del brillo que conserva un letrero de día, de 0 (apagado) a 1 (igual que de noche).")]
        [SerializeField, Range(0f, 1f)] private float nivelDia = 0.1f;

        [Tooltip("Fracción del brillo que conserva un letrero de un local cerrado, de noche (el casino).")]
        [SerializeField, Range(0f, 1f)] private float nivelCerrado = 0.35f;

        [Tooltip("Fracción del brillo que queda durante un apagón del parpadeo (casi apagado, no negro).")]
        [SerializeField, Range(0f, 1f)] private float nivelApagon = 0.04f;

        [Tooltip("Uno de cada tantos letreros parpadea como neón viejo.")]
        [SerializeField, Min(1)] private int unoDeCada = 6;

        [Tooltip("Tiempo prendido entre ráfagas de apagones: mínimo (s).")]
        [SerializeField, Min(0.1f)] private float encendidoMinimo = ParametrosParpadeo.EncendidoMinimoPorDefecto;

        [Tooltip("Tiempo prendido entre ráfagas de apagones: máximo (s).")]
        [SerializeField, Min(0.1f)] private float encendidoMaximo = ParametrosParpadeo.EncendidoMaximoPorDefecto;

        [Tooltip("Apagones por ráfaga: mínimo.")]
        [SerializeField, Min(1)] private int apagonesMinimos = ParametrosParpadeo.ApagonesMinimosPorDefecto;

        [Tooltip("Apagones por ráfaga: máximo.")]
        [SerializeField, Min(1)] private int apagonesMaximos = ParametrosParpadeo.ApagonesMaximosPorDefecto;

        [Tooltip("Duración de un apagón: mínimo (s).")]
        [SerializeField, Min(0.01f)] private float apagadoMinimo = ParametrosParpadeo.ApagadoMinimoPorDefecto;

        [Tooltip("Duración de un apagón: máximo (s).")]
        [SerializeField, Min(0.01f)] private float apagadoMaximo = ParametrosParpadeo.ApagadoMaximoPorDefecto;

        [Tooltip("Tiempo prendido entre dos apagones de una ráfaga: mínimo (s).")]
        [SerializeField, Min(0.01f)] private float entreMinimo = ParametrosParpadeo.EntreMinimoPorDefecto;

        [Tooltip("Tiempo prendido entre dos apagones de una ráfaga: máximo (s).")]
        [SerializeField, Min(0.01f)] private float entreMaximo = ParametrosParpadeo.EntreMaximoPorDefecto;

        public float NivelDia => nivelDia;
        public float NivelCerrado => nivelCerrado;
        public float NivelApagon => nivelApagon;
        public int UnoDeCada => unoDeCada;

        /// <summary>Los tiempos del parpadeo para <see cref="CalculoParpadeo"/>.</summary>
        public ParametrosParpadeo Parametros => new ParametrosParpadeo(encendidoMinimo, encendidoMaximo, apagonesMinimos,
            apagonesMaximos, apagadoMinimo, apagadoMaximo, entreMinimo, entreMaximo);
    }
}
