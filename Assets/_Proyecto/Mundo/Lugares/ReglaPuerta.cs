using MechaCyberpunk.Core;

namespace MechaCyberpunk.Mundo.Lugares
{
    /// <summary>Lógica pura de una puerta: decide qué responde a <c>IntentarAbrir</c> (orden definido en <c>IPuerta</c>).</summary>
    public static class ReglaPuerta
    {
        public static ResultadoPuerta Decidir(bool abiertoPorHorario, bool cerradoEnDemo, bool jugadorDentro, bool yaAbierta)
        {
            if (yaAbierta)
            {
                return ResultadoPuerta.YaAbierta;
            }
            if (jugadorDentro)
            {
                return ResultadoPuerta.Abierta;
            }
            if (cerradoEnDemo)
            {
                return ResultadoPuerta.Bloqueada;
            }
            if (!abiertoPorHorario)
            {
                return ResultadoPuerta.CerradaPorHorario;
            }
            return ResultadoPuerta.Abierta;
        }
    }
}
