using MechaCyberpunk.Economia;

namespace MechaCyberpunk.Mecha.Taller
{
    /// <summary>Qué ofrece el banco del taller. Función pura.</summary>
    public static class ReglaBancoTaller
    {
        public static EstadoBanco Decidir(int adeudo, int dinero, bool mechaEnTaller, float danio)
        {
            if (adeudo > 0 && dinero >= adeudo) return EstadoBanco.PagarRenta;
            if (adeudo > 0) return EstadoBanco.Bloqueado;
            if (!mechaEnTaller) return EstadoBanco.MechaFuera;
            if (danio < ReglaReparacion.DanioMinimo) return EstadoBanco.SinDanio;
            return EstadoBanco.Reparar;
        }

        /// <summary>True si la E hace algo en ese estado.</summary>
        public static bool PuedeUsar(EstadoBanco estado) => estado == EstadoBanco.PagarRenta || estado == EstadoBanco.Reparar;
    }
}
