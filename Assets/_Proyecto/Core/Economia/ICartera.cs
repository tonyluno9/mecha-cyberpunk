using System;

namespace MechaCyberpunk.Core
{
    /// <summary>
    /// El dinero del jugador (enteros). Se busca en <c>Registro&lt;ICartera&gt;</c>.
    /// El dinero puede quedar negativo (deuda) solo por <see cref="CargarDeuda"/>.
    /// La registra <c>ServicioCartera</c> (Economia/, T-102) dentro de <c>ServiciosEconomia.prefab</c> anidado en
    /// <c>JugadorAPie.prefab</c>; mientras no esté en main usa un doble. Es también <see cref="IGuardable"/> con clave <c>cartera</c>.
    /// </summary>
    public interface ICartera
    {
        /// <summary>Dinero actual. Puede ser negativo.</summary>
        int Dinero { get; }

        /// <summary>Se avisa tras cada cambio: diferencia con signo (negativa al cobrar) y su motivo.</summary>
        event Action<int, MotivoDinero> Cambio;

        /// <summary>True si <paramref name="cantidad"/> es mayor que 0 y el dinero alcanza. No cambia nada.</summary>
        bool PuedePagar(int cantidad);

        /// <summary>Cobra solo si alcanza. Con cantidad menor o igual a 0 devuelve false y no cambia nada.</summary>
        bool IntentarPagar(int cantidad, MotivoDinero motivo);

        /// <summary>Suma dinero. Ignora cantidades menores o iguales a 0.</summary>
        void Abonar(int cantidad, MotivoDinero motivo);

        /// <summary>Resta aunque el dinero quede negativo (reparaciones, el doctor). Ignora cantidades menores o iguales a 0.</summary>
        void CargarDeuda(int cantidad, MotivoDinero motivo);
    }
}
