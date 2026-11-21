using MechaCyberpunk.Personaje.Visual;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Movimiento
{
    /// <summary>
    /// T-1550 (decisión de Tony del 20-nov): WASD camina (1.6 m/s), Shift trota (4.5 m/s) y la velocidad rápida (implante de piernas, habilidad de piernas o consumible) es la carrera
    /// "sin gravedad" de PJ01_CorrerRapido a 6.25 m/s. Lógica pura: dice a qué velocidad objetivo se corre con Shift para que, con cualquier bono de velocidad, los pies coincidan con el clip.
    /// </summary>
    public static class ReglaVelocidadRapida
    {
        /// <summary>Velocidad (m/s) de la carrera rápida: la del clip PJ01_CorrerRapido.</summary>
        public const float Velocidad = ReglasAnimacion.VelocidadRapida;

        /// <summary>Un bono de velocidad solo cuenta como "rápido" si pasa de este multiplicador (evita errores de redondeo en 1).</summary>
        public const float MargenBono = 0.001f;

        /// <summary>True si algún bono (implante, habilidad o consumible) acelera la carrera: el producto de los multiplicadores pasa de 1.</summary>
        public static bool Activa(float multiplicadorImplante, float factorHabilidad, float factorExtra)
        {
            return multiplicadorImplante * factorHabilidad * factorExtra > 1f + MargenBono;
        }

        /// <summary>
        /// Velocidad objetivo de correr (Shift) ANTES de aplicar <paramref name="factorExtra"/> (el consumible, que ControladorAPie multiplica después sobre todo el movimiento).
        /// Sin bono es la base (4.5). Con bono es la base por los multiplicadores, pero nunca menos de <see cref="Velocidad"/> una vez aplicado el consumible: así la carrera rápida se ve
        /// siempre con PJ01_CorrerRapido a su velocidad.
        /// </summary>
        public static float VelocidadCorrer(float baseCorrer, float multiplicadorImplante, float factorHabilidad, float factorExtra)
        {
            float conBonos = baseCorrer * multiplicadorImplante * factorHabilidad;
            if (!Activa(multiplicadorImplante, factorHabilidad, factorExtra))
            {
                return conBonos;
            }
            float extra = Mathf.Max(0.01f, factorExtra);
            return Mathf.Max(conBonos * extra, Velocidad) / extra;
        }
    }
}
