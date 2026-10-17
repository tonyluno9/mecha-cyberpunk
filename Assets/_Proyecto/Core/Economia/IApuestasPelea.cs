using System;

namespace MechaCyberpunk.Core
{
    /// <summary>
    /// La apuesta del jugador sobre SU próxima pelea del clandestino (nunca sobre peleas ajenas). La implementa Tony
    /// (<c>ServicioApuestas</c>, Economia/, dentro de <c>ServiciosEconomia.prefab</c>, T-144) y se busca en
    /// <c>Registro&lt;IApuestasPelea&gt;</c>. Quien anuncia la próxima pelea y la resuelve es Tony (el mostrador de peleas y el
    /// flujo de pelea), nadie más. Alan puede leerla y llamar <see cref="Apostar"/> y <see cref="Cancelar"/> desde el casino
    /// para tomar apuestas, sin implementar nada. Perder significa perder lo apostado y el premio de la pelea.
    /// Las apuestas no se guardan. Mientras no esté en main, usa un doble.
    /// </summary>
    public interface IApuestasPelea
    {
        /// <summary>
        /// True si hay una próxima pelea anunciada que todavía no empieza. Solo entonces se puede apostar o cancelar.
        /// </summary>
        bool Abierta { get; }

        /// <summary>Alias del rival de la pelea abierta. Vacío si no está <see cref="Abierta"/>.</summary>
        string Rival { get; }

        /// <summary>
        /// Lo que gana el jugador por cada peso apostado si gana la pelea (1.0 = paga uno a uno: apuesta $250, recibe $500
        /// en total, contando lo que apostó). Siempre mayor que 0.
        /// </summary>
        float Momio { get; }

        /// <summary>Tope de la apuesta en esta pelea ($).</summary>
        int ApuestaMaxima { get; }

        /// <summary>Lo apostado en la pelea abierta ($). 0 si nada. Solo hay una apuesta por pelea.</summary>
        int Apostado { get; }

        /// <summary>
        /// True si está <see cref="Abierta"/>, <see cref="Apostado"/> es 0, <paramref name="monto"/> es mayor que 0 y no
        /// pasa de <see cref="ApuestaMaxima"/>, y la cartera alcanza (<see cref="ICartera.PuedePagar"/>). No cambia nada.
        /// </summary>
        bool PuedeApostar(int monto);

        /// <summary>
        /// Si <see cref="PuedeApostar"/>, cobra <paramref name="monto"/> con
        /// <see cref="ICartera.IntentarPagar"/> y <see cref="MotivoDinero.Apuesta"/>, lo deja en <see cref="Apostado"/> y
        /// devuelve true. Si no, devuelve false y no cambia nada.
        /// </summary>
        bool Apostar(int monto);

        /// <summary>
        /// Mientras está <see cref="Abierta"/> y hay apuesta, devuelve lo apostado a la cartera con
        /// <see cref="MotivoDinero.Apuesta"/>, deja <see cref="Apostado"/> en 0 y devuelve true. Si no, false sin cambiar nada.
        /// </summary>
        bool Cancelar();

        /// <summary>
        /// Se avisa tras cualquier cambio de <see cref="Abierta"/>, <see cref="Rival"/>, <see cref="Momio"/>,
        /// <see cref="ApuestaMaxima"/> o <see cref="Apostado"/>.
        /// </summary>
        event Action Cambio;

        /// <summary>
        /// Se avisa al terminar la pelea, solo si había apuesta, con el cambio neto para el jugador: positivo lo ganado
        /// sin contar lo que se le devuelve (apuesta $250 con momio 1.0 y gana: +250), negativo lo perdido (pierde: -250).
        /// Sin apuesta no se avisa.
        /// </summary>
        event Action<int> Resuelta;
    }
}
