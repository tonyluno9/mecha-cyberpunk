using UnityEngine;

namespace MechaCyberpunk.Core
{
    /// <summary>Fachada estática de <see cref="ISonidos"/>: si no hay gestor en la escena no hace nada y no avisa.</summary>
    public static class Sonidos
    {
        /// <summary>Valor que devuelve <see cref="ReproducirBucle"/> cuando no suena nada.</summary>
        public const int SinBucle = -1;

        public static void Reproducir(string id) => Registro<ISonidos>.Actual?.Reproducir(id);

        public static void ReproducirEn(string id, Vector3 posicion) => Registro<ISonidos>.Actual?.ReproducirEn(id, posicion);

        public static int ReproducirBucle(string id, Transform sigue)
        {
            ISonidos gestor = Registro<ISonidos>.Actual;
            return gestor != null ? gestor.ReproducirBucle(id, sigue) : SinBucle;
        }

        public static void DetenerBucle(int bucle) => Registro<ISonidos>.Actual?.DetenerBucle(bucle);

        public static void FijarTono(int bucle, float tono) => Registro<ISonidos>.Actual?.FijarTono(bucle, tono);

        public static float Volumen(SeccionSonido seccion)
        {
            ISonidos gestor = Registro<ISonidos>.Actual;
            return gestor != null ? gestor.Volumen(seccion) : 0f;
        }

        public static void FijarVolumen(SeccionSonido seccion, float volumen) => Registro<ISonidos>.Actual?.FijarVolumen(seccion, volumen);
    }
}
