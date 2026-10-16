using System;

namespace MechaCyberpunk.Core
{
    /// <summary>
    /// En qué etapa del prólogo va la partida. La implementa Tony (<c>ServicioHistoria</c>, Progresion/, dentro de
    /// <c>ServiciosProgresion.prefab</c> anidado en <c>JugadorAPie.prefab</c>, T-124) y la avanza el director del prólogo
    /// (T-132); se busca en <c>Registro&lt;IHistoria&gt;</c>. Alan solo la lee para que su mundo acompañe a la historia.
    /// Ejemplos: en <see cref="EtapaPrologo.RegresoACasa"/> y después, el carro del jugador estacionado cerca del clandestino;
    /// en <see cref="EtapaPrologo.PeleaTutorial"/> y <see cref="EtapaPrologo.PeleaCasera"/>, el clandestino abierto; casino y
    /// mercado negro cerrados en toda la demo. Sin <c>IHistoria</c> registrada, el mundo se comporta como en
    /// <see cref="EtapaPrologo.Libre"/>. Es <see cref="IGuardable"/> con clave <c>historia</c>.
    /// </summary>
    public interface IHistoria
    {
        /// <summary>Etapa actual del prólogo.</summary>
        EtapaPrologo Etapa { get; }

        /// <summary>
        /// Se avisa una vez por cada cambio de <see cref="Etapa"/>, con la etapa nueva; también al cargar una partida si la
        /// etapa cambió.
        /// </summary>
        event Action<EtapaPrologo> EtapaCambio;
    }
}
