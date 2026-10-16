using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Interaccion;

namespace MechaCyberpunk.Personaje.Jugador
{
    /// <summary>Lógica pura: ¿qué hace la E? Depende del modo, del KO, del candidato y de lo que dice el montable.</summary>
    public static class ReglaMontar
    {
        /// <summary>
        /// A pie: abre la puerta candidata o sube al montable candidato si puede subir. En carro o mecha: baja si el
        /// montable puede bajar y, si no, avisa que no puede. Con un interactuable candidato lo usa si puede y, si no, lo niega.
        /// En KO no hace nada.
        /// </summary>
        public static AccionMontar Decidir(ModoJugador modo, bool estaKO, TipoCandidatoAPie candidato, bool montablePuedeSubir, bool montablePuedeBajar,
            bool interactuablePuedeUsar = false)
        {
            if (estaKO)
            {
                return AccionMontar.Nada;
            }
            if (modo != ModoJugador.APie)
            {
                return montablePuedeBajar ? AccionMontar.Bajar : AccionMontar.NoPuedeBajar;
            }
            switch (candidato)
            {
                case TipoCandidatoAPie.Puerta:
                    return AccionMontar.AbrirPuerta;
                case TipoCandidatoAPie.Montable:
                    return montablePuedeSubir ? AccionMontar.Subir : AccionMontar.Nada;
                case TipoCandidatoAPie.Interactuable:
                    return interactuablePuedeUsar ? AccionMontar.Usar : AccionMontar.UsarNegado;
                default:
                    return AccionMontar.Nada;
            }
        }
    }
}
