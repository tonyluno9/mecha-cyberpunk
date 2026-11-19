using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.Trafico.Persecucion
{
    /// <summary>T-1476: sirenas de las patrullas. Un bucle por patrulla que persigue (lo limita la mezcla) y un "bloop" corto al empezar.</summary>
    public static class SonidosPolicia
    {
        public const string IdSirenaLoop = "POL_SIRENA_LOOP";
        public const string IdSirenaCorta = "POL_SIRENA_CORTA";

        /// <summary>Arranca el bloop corto y el bucle que sigue a la patrulla. Devuelve el bucle (o <see cref="Sonidos.SinBucle"/>).</summary>
        public static int IniciarSirena(Transform patrulla)
        {
            Sonidos.ReproducirEn(IdSirenaCorta, patrulla.position);
            return Sonidos.ReproducirBucle(IdSirenaLoop, patrulla);
        }

        public static void DetenerSirena(int bucle)
        {
            if (bucle != Sonidos.SinBucle)
            {
                Sonidos.DetenerBucle(bucle);
            }
        }
    }
}
