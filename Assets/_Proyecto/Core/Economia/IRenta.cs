using System;

namespace MechaCyberpunk.Core
{
    /// <summary>
    /// La renta de la casa (se cobra cada 7 días). La implementa Tony (<c>ServicioRenta</c>, Economia/, dentro de
    /// <c>ServiciosEconomia.prefab</c> anidado en <c>JugadorAPie.prefab</c>, T-107) y se busca en
    /// <c>Registro&lt;IRenta&gt;</c>. Con el taller bloqueado no se repara ni se mejora el mecha, pero sí se puede
    /// sacar y pelear dañado. Alan puede leerla para mostrar el bloqueo en el mundo (opcional); no la implementes.
    /// </summary>
    public interface IRenta
    {
        /// <summary>Lo que cuesta cada periodo de renta ($).</summary>
        int Monto { get; }

        /// <summary>
        /// El <c>IRelojMundo.Dia</c> (número de día) en cuyo inicio, al pasar las 00:00, se cobra la siguiente renta.
        /// </summary>
        int DiaProximoCobro { get; }

        /// <summary>Renta vencida sin pagar ($). 0 si no se debe nada.</summary>
        int Adeudo { get; }

        /// <summary>True si <see cref="Adeudo"/> es mayor que 0: el casero bloqueó el taller.</summary>
        bool TallerBloqueado { get; }

        /// <summary>Se avisa tras cualquier cambio de <see cref="Monto"/>, <see cref="DiaProximoCobro"/>, <see cref="Adeudo"/> o <see cref="TallerBloqueado"/>.</summary>
        event Action Cambio;

        /// <summary>
        /// Cobra todo el <see cref="Adeudo"/> de la cartera con <see cref="MotivoDinero.Renta"/> solo si alcanza y
        /// desbloquea el taller. Devuelve false (y no cambia nada) si no alcanza o no hay adeudo.
        /// </summary>
        bool PagarAdeudo();
    }
}
