using System;

namespace MechaCyberpunk.Core
{
    /// <summary>
    /// Lectura de la misión en curso. Solo hay una misión a la vez.
    /// Solo lectura: la implementa Tony (<c>ServicioMisiones</c>, Progresion/, T-184, clave de guardado <c>misiones</c>) y se
    /// busca con <c>Registro&lt;IMisiones&gt;</c>. Alan puede leerla (por ejemplo, patrullas más atentas durante un robo).
    /// Sin <see cref="IMisiones"/> registrado, quien lea debe actuar como si no hubiera misión.
    /// </summary>
    public interface IMisiones
    {
        /// <summary>True si hay una misión aceptada y sin terminar.</summary>
        bool HayMision { get; }

        /// <summary>Clase de la misión en curso. Solo vale con <see cref="HayMision"/>.</summary>
        TipoMision Tipo { get; }

        /// <summary>Título de la misión para pantallas. Cadena vacía (nunca null) si no hay misión.</summary>
        string Titulo { get; }

        /// <summary>Se avisa al aceptar una misión, en cada paso, al terminar y al cargar una partida.</summary>
        event Action Cambio;

        /// <summary>
        /// Se avisa al terminar una misión con su tipo: true si se cumplió, false si falló o se abandonó.
        /// No se avisa al cargar una partida.
        /// </summary>
        event Action<TipoMision, bool> Terminada;
    }
}
