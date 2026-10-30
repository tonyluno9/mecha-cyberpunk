using UnityEngine;

namespace MechaCyberpunk.Core
{
    /// <summary>
    /// Contrato del gestor de sonidos (T-407): se pide un sonido por su ID de la LISTA 1 (docs/listas-equipo-cyberfix.md). Un ID sin archivo
    /// no suena y no da error. Se usa por la fachada <see cref="Sonidos"/>.
    /// </summary>
    public interface ISonidos
    {
        /// <summary>Suena el ID en 2D.</summary>
        void Reproducir(string id);

        /// <summary>Suena el ID en 3D en esa posición.</summary>
        void ReproducirEn(string id, Vector3 posicion);

        /// <summary>Empieza un bucle que sigue a un transform (null: suena en 2D). Devuelve su identificador, o -1 si no suena.</summary>
        int ReproducirBucle(string id, Transform sigue);

        void DetenerBucle(int bucle);

        /// <summary>Cambia el tono (pitch) de un bucle.</summary>
        void FijarTono(int bucle, float tono);

        float Volumen(SeccionSonido seccion);

        void FijarVolumen(SeccionSonido seccion, float volumen);
    }
}
