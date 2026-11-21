using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.Trafico.Persecucion
{
    /// <summary>
    /// T-1476: sirenas de las patrullas. Un bucle por patrulla que persigue (lo limita la mezcla) y un "bloop" corto al empezar.
    /// T-1533: la estática de la radio y la sirena de la torre del muro cuando la búsqueda está al máximo.
    /// </summary>
    public static class SonidosPolicia
    {
        public const string IdSirenaLoop = "POL_SIRENA_LOOP";
        public const string IdSirenaCorta = "POL_SIRENA_CORTA";

        /// <summary>T-1533: la estática sola que suena bajo la voz de la radio de la patrulla.</summary>
        public const string IdRadioEstatica = "POL_RADIO_ESTATICA";

        /// <summary>T-1533: sube una estrella de búsqueda (2D, lo pide el servicio de calor).</summary>
        public const string IdEstrellaSube = "POL_ESTRELLA_SUBE";

        /// <summary>T-1533: sirena de la torre del muro cuando la búsqueda llega al máximo (opcional: una sola).</summary>
        public const string IdSirenaMuro = "POL_SIRENA_MURO";

        /// <summary>Prefijo de las piezas de torre del muro (MU02).</summary>
        public const string PrefijoTorreMuro = "MU02";

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

        /// <summary>
        /// T-1533: arranca la sirena del muro pegada a la torre (pieza MU02) más cercana a <paramref name="jugador"/>. Devuelve el bucle, o
        /// <see cref="Sonidos.SinBucle"/> si no hay torres en la escena o no suena. Se llama solo al llegar al máximo de estrellas, nunca cada cuadro.
        /// </summary>
        public static int IniciarSirenaMuro(Vector3 jugador)
        {
            Transform torre = TorreMasCercana(jugador);
            return torre != null ? Sonidos.ReproducirBucle(IdSirenaMuro, torre) : Sonidos.SinBucle;
        }

        /// <summary>La pieza de torre del muro (nombre que empieza con MU02) más cercana a <paramref name="punto"/>; null si no hay ninguna.</summary>
        public static Transform TorreMasCercana(Vector3 punto)
        {
            Transform mejor = null;
            float menor = float.MaxValue;
            foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (!EsTorreDelMuro(t.name))
                {
                    continue;
                }
                float d = (t.position - punto).sqrMagnitude;
                if (d < menor)
                {
                    menor = d;
                    mejor = t;
                }
            }
            return mejor;
        }

        /// <summary>Regla pura: si el nombre de una pieza es de torre del muro.</summary>
        public static bool EsTorreDelMuro(string nombre) => !string.IsNullOrEmpty(nombre) && nombre.StartsWith(PrefijoTorreMuro, System.StringComparison.Ordinal);
    }
}
