using UnityEngine;

namespace MechaCyberpunk.UI.Mapa
{
    /// <summary>
    /// El punto que marcas con un clic en el mapa grande (T-409); el minimapa lo dibuja con su distancia y, con él, la ruta GPS naranja (T-668). Puede ser
    /// un punto libre o un lugar del catálogo (<see cref="Destino"/>). Uno a la vez. Se queda hasta que llegas o lo quitas, y se borra al entrar a Play.
    /// </summary>
    public static class PuntoMarcadoMapa
    {
        /// <summary>A cuántos metros del punto marcado se da por llegado (m).</summary>
        public const float MetrosParaLlegar = 8f;

        public static bool Hay { get; private set; }

        /// <summary>Dónde está (m, mundo, altura 0).</summary>
        public static Vector3 Posicion { get; private set; }

        /// <summary>El lugar marcado, o null si es un punto libre o no hay nada marcado.</summary>
        public static IdDestinoMapa? Destino { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Quitar()
        {
            Hay = false;
            Posicion = Vector3.zero;
            Destino = null;
        }

        public static void Marcar(Vector3 posicion)
        {
            Posicion = new Vector3(posicion.x, 0f, posicion.z);
            Hay = true;
            Destino = null;
        }

        /// <summary>Marca un lugar del catálogo: el GPS traza la ruta hasta él.</summary>
        public static void MarcarDestino(IdDestinoMapa destino, Vector3 posicion)
        {
            Marcar(posicion);
            Destino = destino;
        }

        /// <summary>Si <paramref name="destino"/> es el marcado lo quita; si no, lo marca (clic sobre el icono de un lugar).</summary>
        public static void AlternarDestino(IdDestinoMapa destino, Vector3 posicion)
        {
            if (Hay && Destino == destino)
            {
                Quitar();
            }
            else
            {
                MarcarDestino(destino, posicion);
            }
        }

        /// <summary>True si el jugador en <paramref name="jugador"/> ya llegó al punto marcado (a <see cref="MetrosParaLlegar"/> m o menos).</summary>
        public static bool Llego(Vector3 jugador)
        {
            return Hay && CalculoMinimapa.DistanciaPlana(jugador, Posicion) <= MetrosParaLlegar;
        }
    }
}
