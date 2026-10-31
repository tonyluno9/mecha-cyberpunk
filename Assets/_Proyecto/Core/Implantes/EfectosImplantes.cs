using System;

namespace MechaCyberpunk.Core
{
    /// <summary>
    /// Lo que suman los implantes instalados, como datos simples. Un valor neutro no cambia nada. Quien lo lee
    /// solo aplica los campos que le tocan; este tipo no calcula nada más ni conoce precios.
    /// Ojo: el implante cosmético que se elige al crear el personaje (<c>AparienciaJugador.Implante</c>) es solo
    /// apariencia y NO cuenta como implante instalado: no aparece aquí.
    /// </summary>
    [Serializable]
    public struct EfectosImplantes
    {
        /// <summary>Multiplica el daño del golpe a pie. Neutro: 1. Ejemplo: 1.5 pega 50 % más fuerte.</summary>
        public float multiplicadorGolpeAPie;

        /// <summary>Multiplica la velocidad al correr a pie (caminar no cambia). Neutro: 1. Ejemplo: 1.2.</summary>
        public float multiplicadorCorrer;

        /// <summary>Multiplica la altura del salto a pie. Neutro: 1. Ejemplo: 1.3.</summary>
        public float multiplicadorSalto;

        /// <summary>
        /// Multiplica la preparación del golpe y el tiempo de subir la guardia del mecha del jugador, y DIVIDE la
        /// ventana del bloqueo perfecto. Neutro: 1; menor que 1 es más rápido (0.85 = 15 % menos de espera y más ventana).
        /// </summary>
        public float multiplicadorReaccionMecha;

        /// <summary>True si ves la vida del rival en la cabina. Neutro: false.</summary>
        public bool verVidaRival;

        /// <summary>True si la vida del rival también se ve en números. Neutro: false.</summary>
        public bool vidaRivalEnNumeros;

        /// <summary>Segundos de aviso antes del próximo ataque del rival. Neutro: 0 (no lo ves).</summary>
        public float anticipacionAtaque;

        // Efectos del combate a pie de pandillas (T-430). Neutro: 0 (sin implante); se suman al combinar.

        /// <summary>OJOS: fracción extra de radio y de alcance del apuntado asistido (0.25 = 25 % más). Neutro: 0.</summary>
        public float bonusAlcanceApuntado;

        /// <summary>BRAZOS: fracción que se quita a la patada de cámara al disparar (0.3 = 30 % menos). Neutro: 0.</summary>
        public float reduccionRetroceso;

        /// <summary>PIERNAS: fracción que se quita al enfriamiento de la rodada (0.25 = 25 % menos). Neutro: 0.</summary>
        public float reduccionEnfriamientoRodada;

        /// <summary>SINAPSIS: fracción que se quita al tiempo de recarga del arma a pie (0.2 = 20 % menos). Neutro: 0.</summary>
        public float reduccionRecarga;

        /// <summary>Todo neutro: sin implantes.</summary>
        public static EfectosImplantes Ninguno => new EfectosImplantes
        {
            multiplicadorGolpeAPie = 1f,
            multiplicadorCorrer = 1f,
            multiplicadorSalto = 1f,
            multiplicadorReaccionMecha = 1f,
            verVidaRival = false,
            vidaRivalEnNumeros = false,
            anticipacionAtaque = 0f,
            bonusAlcanceApuntado = 0f,
            reduccionRetroceso = 0f,
            reduccionEnfriamientoRodada = 0f,
            reduccionRecarga = 0f
        };

        /// <summary>
        /// Suma dos conjuntos de efectos: los multiplicadores se multiplican, los bool se combinan con OR y la
        /// anticipación toma el máximo. Combinar con <see cref="Ninguno"/> no cambia nada.
        /// </summary>
        public static EfectosImplantes Combinar(EfectosImplantes a, EfectosImplantes b)
        {
            return new EfectosImplantes
            {
                multiplicadorGolpeAPie = a.multiplicadorGolpeAPie * b.multiplicadorGolpeAPie,
                multiplicadorCorrer = a.multiplicadorCorrer * b.multiplicadorCorrer,
                multiplicadorSalto = a.multiplicadorSalto * b.multiplicadorSalto,
                multiplicadorReaccionMecha = a.multiplicadorReaccionMecha * b.multiplicadorReaccionMecha,
                verVidaRival = a.verVidaRival || b.verVidaRival,
                vidaRivalEnNumeros = a.vidaRivalEnNumeros || b.vidaRivalEnNumeros,
                anticipacionAtaque = Math.Max(a.anticipacionAtaque, b.anticipacionAtaque),
                bonusAlcanceApuntado = a.bonusAlcanceApuntado + b.bonusAlcanceApuntado,
                reduccionRetroceso = Math.Min(0.9f, a.reduccionRetroceso + b.reduccionRetroceso),
                reduccionEnfriamientoRodada = Math.Min(0.9f, a.reduccionEnfriamientoRodada + b.reduccionEnfriamientoRodada),
                reduccionRecarga = Math.Min(0.9f, a.reduccionRecarga + b.reduccionRecarga)
            };
        }
    }
}
