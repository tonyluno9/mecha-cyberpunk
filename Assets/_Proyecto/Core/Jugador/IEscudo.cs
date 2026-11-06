using System;

namespace MechaCyberpunk.Core
{
    /// <summary>
    /// Escudo de energía del jugador a pie (T-684): una barra que absorbe el daño antes que la vida y se recarga sola
    /// después de unos segundos sin recibir daño. Lo implementa <c>EscudoJugador</c> (Personaje/) y se agrega a
    /// <c>Registro&lt;IEscudo&gt;</c> en <c>OnEnable</c> (se quita en <c>OnDisable</c>).
    /// Es mejorable sin tocar su código: el Doc (o cualquier implante) llama a <see cref="MejorarMaximo"/> y
    /// <see cref="MejorarRecarga"/>; las mejoras se guardan en la partida (clave <c>escudo</c>).
    /// </summary>
    public interface IEscudo
    {
        /// <summary>Puntos de escudo ahora.</summary>
        float Actual { get; }

        /// <summary>Máximo actual: el base de los datos más las mejoras.</summary>
        float Maximo { get; }

        /// <summary>Recarga actual (puntos por segundo): la base de los datos más las mejoras.</summary>
        float RecargaPorSegundo { get; }

        /// <summary>Puntos de máximo que suman las mejoras.</summary>
        float BonoMaximo { get; }

        /// <summary>Puntos por segundo de recarga que suman las mejoras.</summary>
        float BonoRecarga { get; }

        /// <summary>Suma puntos al máximo (también llena esos puntos). Ignora valores no positivos.</summary>
        void MejorarMaximo(float puntos);

        /// <summary>Suma puntos por segundo a la recarga. Ignora valores no positivos.</summary>
        void MejorarRecarga(float puntosPorSegundo);

        /// <summary>Recupera escudo de golpe (por ejemplo una batería), sin pasar del máximo.</summary>
        void Recuperar(float puntos);

        /// <summary>Se avisa cuando cambia <see cref="Actual"/>, <see cref="Maximo"/> o la recarga.</summary>
        event Action Cambio;

        /// <summary>Se avisa cuando el escudo absorbe daño, con los puntos que absorbió.</summary>
        event Action<float> Absorbio;
    }
}
