using System;

namespace MechaCyberpunk.Core
{
    /// <summary>
    /// Las opciones del jugador que leen otras áreas (T-411, D-tony-243): la sensibilidad del ratón y si se invierte el eje Y.
    /// La implementa <c>ServicioOpciones</c> (Progresion) y va en <c>Registro&lt;IOpciones&gt;</c>. La leen ControladorAPie (Personaje)
    /// y CamaraCarro (Vehiculos): multiplican su sensibilidad por <see cref="Sensibilidad"/> y voltean Y si <see cref="InvertirY"/>.
    /// Sin servicio registrado se usa 1 y Y normal.
    /// </summary>
    public interface IOpciones
    {
        /// <summary>Multiplicador de la sensibilidad del ratón, de 0.25 a 3 (1 = la de los datos).</summary>
        float Sensibilidad { get; }

        /// <summary>True: mover el ratón hacia arriba mira hacia abajo.</summary>
        bool InvertirY { get; }

        /// <summary>Apuntado asistido al apuntar con clic derecho (T-424). Sin servicio se usa Suave.</summary>
        NivelApuntadoAsistido ApuntadoAsistido { get; }

        /// <summary>Se dispara al cambiar cualquier opción.</summary>
        event Action Cambio;
    }
}
