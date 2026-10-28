using System;

namespace MechaCyberpunk.Core
{
    /// <summary>
    /// Lectura de los implantes instalados en el jugador (4 espacios: ojos, brazos, piernas, sistema nervioso).
    /// Solo lectura: la implementa Tony (<c>ServicioImplantes</c>, Progresion/, en <c>ServiciosProgresion.prefab</c>, T-191,
    /// clave de guardado <c>implantes</c>) y se busca con <c>Registro&lt;IImplantes&gt;</c>. Solo Tony instala (en su clínica,
    /// T-193). Alan solo lee; ejemplos: un vecino que dice "ese trae cromo" con <see cref="Cantidad"/> de 3 o más, o el
    /// letrero de la clínica que se prende con <see cref="Instalado"/>.
    /// Sin <see cref="IImplantes"/> registrado, quien lea debe actuar como si <see cref="Cantidad"/> fuera 0 y
    /// <see cref="Efectos"/> fuera <see cref="EfectosImplantes.Ninguno"/> (así el prólogo y las escenas viejas no cambian).
    /// El implante cosmético de la creación del personaje (<c>AparienciaJugador.Implante</c>) es otra cosa: no cuenta aquí.
    /// </summary>
    public interface IImplantes
    {
        /// <summary>Nivel instalado en el espacio: 0 = nada; 1 o 2 (los niveles son datos de Tony).</summary>
        int Nivel(EspacioImplante espacio);

        /// <summary>Nombre del implante para pantallas (por ejemplo "OJOS DE LECHUZA"). Cadena vacía (nunca null) sin implante.</summary>
        string Nombre(EspacioImplante espacio);

        /// <summary>Cuántos espacios tienen algo instalado (0 a 4).</summary>
        int Cantidad { get; }

        /// <summary>La combinación de los efectos de todo lo instalado. <see cref="EfectosImplantes.Ninguno"/> sin implantes.</summary>
        EfectosImplantes Efectos { get; }

        /// <summary>Se avisa tras cualquier cambio, también al cargar una partida.</summary>
        event Action Cambio;

        /// <summary>
        /// Se avisa con el espacio y el nivel nuevo solo al instalar de verdad. No se avisa al cargar una partida.
        /// </summary>
        event Action<EspacioImplante, int> Instalado;
    }
}
