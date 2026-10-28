using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.Combate.Lectura
{
    /// <summary>
    /// Lo que los OJOS del jugador pueden leer de un rival (T-194, D-tony-168). Vive en Combate, no en Core. Quien la implementa se
    /// agrega a <c>Registro&lt;ILecturaRival&gt;</c> y se quita al destruirse. Solo lectura: no cambia cómo decide el rival.
    /// </summary>
    public interface ILecturaRival
    {
        /// <summary>Nombre en pantallas.</summary>
        string Nombre { get; }

        /// <summary>Transform raíz del rival (para medir distancia y ángulo).</summary>
        Transform Raiz { get; }

        /// <summary>Su vida.</summary>
        IVida Vida { get; }

        /// <summary>True si es un mecha (los de a pie no se leen en la cabina).</summary>
        bool EsMecha { get; }

        /// <summary>True si ya está en el aviso de un golpe o de una habilidad.</summary>
        bool Preparando { get; }

        /// <summary>
        /// Segundos estimados hasta que empiece su próximo aviso (golpe o habilidad); 0 o más. -1 si no se sabe (fuera de alcance,
        /// sin turno, sin control o sin objetivo).
        /// </summary>
        float SegundosParaAtacar { get; }

        /// <summary>Si lo próximo es un golpe o una habilidad.</summary>
        TipoAtaqueLeido Proximo { get; }
    }
}
