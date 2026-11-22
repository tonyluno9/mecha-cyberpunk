using UnityEngine;

namespace MechaCyberpunk.IA.APie
{
    /// <summary>T-1651 (T26): las frases con subtítulo de la advertencia. Los IDs de voz son los de <see cref="VocesPandilla"/> (SKU_PROVOCAR, SKU_ULTIMO_AVISO, SKU_INSULTO). Español de barrio.</summary>
    public static class FrasesConfrontacion
    {
        public static readonly string[] ProvocarSkulls =
        {
            "¿Qué onda, carnal? ¿Andas perdido?", "Oye tú, ¿qué buscas por aquí?", "Esta es zona de los Skulls, compa.", "Ni te arrimes, wey.", "¿Qué ves, qué ves?"
        };

        public static readonly string[] InsultoSkulls =
        {
            "Pinche mirón.", "¿Te crees muy salsa, o qué?", "Aquí no eres bienvenido, wey.", "Muévete o te movemos."
        };

        public static readonly string[] UltimoAvisoSkulls =
        {
            "Última vez: lárgate de aquí.", "Te doy chance de irte, ahorita.", "Ya te dije, órale, fuera de mi barrio."
        };

        public static readonly string[] ProvocarSnakes =
        {
            "Esta calle es de las Víboras, bro.", "¿Quién te dio permiso de pasar?", "Aquí se paga por pasar, compa.", "Te estás metiendo donde no, wey."
        };

        public static readonly string[] InsultoSnakes =
        {
            "Pinche rata, lárgate.", "Se te nota lo perdido, carnal.", "¿Qué, te quedaste sordo?"
        };

        public static readonly string[] UltimoAvisoSnakes =
        {
            "Última, ¿eh? O te vamos a sacar.", "Se te acabó el tiempo, wey."
        };

        public static readonly string[] AvisoGuardia =
        {
            "Oiga, guarde esa arma ahorita.", "Aquí no se saca el arma, joven.", "Guárdela o la tenemos que sacar a usted."
        };

        public static readonly string[] UltimoAvisoGuardia =
        {
            "Última advertencia: guarde eso."
        };

        public static string Elegir(string[] lista, ref int ultimo)
        {
            if (lista == null || lista.Length == 0)
            {
                return string.Empty;
            }
            int i = Random.Range(0, lista.Length);
            if (lista.Length > 1 && i == ultimo)
            {
                i = (i + 1) % lista.Length;
            }
            ultimo = i;
            return lista[i];
        }
    }
}
