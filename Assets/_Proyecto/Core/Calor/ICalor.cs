using System;

namespace MechaCyberpunk.Core
{
    /// <summary>
    /// El calor policial del jugador. Se busca en <c>Registro&lt;ICalor&gt;</c>. Lo implementa Tony (Progresion/).
    /// Los drones y patrullas de Alan persiguen según <see cref="Nivel"/>; el mundo puede llamar a
    /// <see cref="Enfriar"/> (escondites), <see cref="Borrar"/> (llegar a casa) y <see cref="Sumar"/> (chocar con una patrulla).
    /// Por ahora nadie la registra en <c>Registro&lt;ICalor&gt;</c>: lo hará la tarea de la fase T-F4/T-F5 de Tony; no la leas de <c>Registro&lt;ICalor&gt;</c> todavía.
    /// Contrato: el valor nunca sale de [0, <see cref="Maximo"/>]; cantidades menores o iguales a 0 no hacen nada.
    /// </summary>
    public interface ICalor
    {
        /// <summary>Calor actual, de 0 a <see cref="Maximo"/> (puntos).</summary>
        float Valor { get; }

        /// <summary>Calor máximo (puntos).</summary>
        float Maximo { get; }

        /// <summary>0 sin calor; 1 a 3 según los umbrales (se está en el nivel N desde el N-ésimo umbral).</summary>
        int Nivel { get; }

        /// <summary>Se avisa con el nuevo valor cada vez que <see cref="Valor"/> cambia.</summary>
        event Action<float> ValorCambio;

        /// <summary>Se avisa con el nuevo nivel solo cuando el nivel cambia.</summary>
        event Action<int> NivelCambio;

        /// <summary>Sube el calor (se recorta a <see cref="Maximo"/>). Ignora cantidades menores o iguales a 0.</summary>
        void Sumar(float cantidad, MotivoCalor motivo);

        /// <summary>Baja el calor (se recorta a 0). Ignora cantidades menores o iguales a 0.</summary>
        void Enfriar(float cantidad);

        /// <summary>Deja el calor en 0 (llegar a casa).</summary>
        void Borrar();
    }
}
