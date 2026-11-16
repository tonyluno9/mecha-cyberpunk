using MechaCyberpunk.Core;

namespace MechaCyberpunk.IA.APie
{
    /// <summary>
    /// T-1106: un filtro del daño que recibe un <see cref="PandilleroAPie"/> (escudo del guardia pesado, escudo de energía del jefe final).
    /// Va como componente en el mismo objeto y se consulta antes de aplicar el daño.
    /// </summary>
    public interface IFiltroDanioAPie
    {
        /// <summary>
        /// Devuelve la cantidad de daño que sí entra a la vida (ya con castigo aplicado) y cómo se bloqueó.
        /// </summary>
        /// <param name="p">Quien recibe el daño.</param>
        /// <param name="info">El golpe, con la cantidad ya multiplicada por el castigo.</param>
        /// <param name="bloqueo">Clase de bloqueo para el resultado (chispa y sonido).</param>
        float Filtrar(PandilleroAPie p, InfoDanio info, out TipoBloqueo bloqueo);
    }
}
