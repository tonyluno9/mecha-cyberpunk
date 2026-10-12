using System;

namespace MechaCyberpunk.Core
{
    /// <summary>
    /// El objetivo actual de la historia, solo lectura para el mundo. Se busca en <c>Registro&lt;IObjetivos&gt;</c>;
    /// lo implementa Tony (Progresion/) y es quien lo fija.
    /// Para dibujarlo, el mundo usa <see cref="Objetivo.Posicion"/> si <see cref="Objetivo.TienePosicion"/>;
    /// si no, busca en <c>Registro&lt;ILugar&gt;</c> el lugar con ese <see cref="Objetivo.Lugar"/> como <c>Id</c>
    /// y usa su <c>PosicionEntrada</c>.
    /// La registra <c>ServicioObjetivos</c> (Progresion/, tarea T-055). Para dibujar el objetivo usa <see cref="UbicacionObjetivo.Resolver"/> con <c>Registro&lt;ILugar&gt;.Todos</c>.
    /// </summary>
    public interface IObjetivos
    {
        /// <summary>True si hay un objetivo fijado.</summary>
        bool HayObjetivo { get; }

        /// <summary>Objetivo actual. <see cref="Objetivo.Ninguno"/> si no hay.</summary>
        Objetivo Actual { get; }

        /// <summary>Se avisa con el nuevo objetivo (o <see cref="Objetivo.Ninguno"/>) cada vez que cambia.</summary>
        event Action<Objetivo> Cambio;
    }
}
