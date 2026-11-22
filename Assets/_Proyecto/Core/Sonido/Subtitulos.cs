using System;
using UnityEngine;

namespace MechaCyberpunk.Core
{
    /// <summary>
    /// T-1624 (T15): línea de voz del mundo con subtítulo. Quien habla (pandilleros, vecinos, vendedores) publica la línea con su
    /// posición; la UI decide si la muestra (cerca, a la vista y con la opción encendida). <paramref name="duracion"/> &lt;= 0: la UI la
    /// calcula por el largo del texto (o por el clip de audio si lo hay).
    /// </summary>
    public static class Subtitulos
    {
        public struct Linea
        {
            public string Hablante;
            public string Texto;
            public Vector3 Posicion;
            public float Duracion;
            public Color ColorHablante;
        }

        public static event Action<Linea> Publicada;

        public static void Publicar(string hablante, string texto, Vector3 posicion, float duracion = 0f, Color? color = null)
        {
            Publicada?.Invoke(new Linea
            {
                Hablante = hablante,
                Texto = texto,
                Posicion = posicion,
                Duracion = duracion,
                ColorHablante = color ?? new Color(1f, 0.8f, 0.2f)
            });
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar() => Publicada = null;
    }
}
