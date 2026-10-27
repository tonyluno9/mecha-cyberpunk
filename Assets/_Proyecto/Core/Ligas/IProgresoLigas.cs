using System;

namespace MechaCyberpunk.Core
{
    /// <summary>
    /// Lectura del progreso de las ligas del clandestino: sótano, calle, distrito y campeonato. Cada liga tiene 5 rivales
    /// y el último es su campeón (pelea de jefe); vencerlo abre la siguiente liga.
    /// Solo lectura: la implementa Tony (<c>ServicioLigas</c>, Progresion/, en <c>ServiciosProgresion.prefab</c>, T-154, clave de
    /// guardado <c>ligas</c>) y se busca con <c>Registro&lt;IProgresoLigas&gt;</c>. Solo Tony registra victorias; Alan solo lee
    /// (por ejemplo para un cartel "LIGA CALLE" en el clandestino). El bloque de piezas toma <see cref="LigaActual"/> como el
    /// nivel máximo de pieza que se puede comprar. Sin <see cref="IProgresoLigas"/> registrado, quien lea debe actuar como
    /// liga 1 sin campeones vencidos.
    /// </summary>
    public interface IProgresoLigas
    {
        /// <summary>Cuántas ligas hay (4 en el juego).</summary>
        int CantidadLigas { get; }

        /// <summary>
        /// La liga más alta abierta, de 1 a <see cref="CantidadLigas"/> (1 = sótano). Nunca baja. Al vencer al campeón de la
        /// última liga sigue siendo la última.
        /// </summary>
        int LigaActual { get; }

        /// <summary>
        /// Campeones de liga vencidos, de 0 a <see cref="CantidadLigas"/>. Igual a <see cref="CantidadLigas"/> significa
        /// "campeón del clandestino".
        /// </summary>
        int CampeonesVencidos { get; }

        /// <summary>
        /// Nombre de la liga para pantallas ("SÓTANO"). Cadena vacía (nunca null) si <paramref name="liga"/> está fuera de
        /// 1..<see cref="CantidadLigas"/>.
        /// </summary>
        string NombreLiga(int liga);

        /// <summary>Se avisa tras cualquier cambio de lo anterior, también al cargar una partida.</summary>
        event Action Cambio;

        /// <summary>
        /// Se avisa al vencer por primera vez al campeón de una liga, con el número de esa liga. No se avisa al cargar una
        /// partida ni en una revancha. Si es la última liga, el jugador es campeón del clandestino.
        /// </summary>
        event Action<int> CampeonVencido;
    }
}
