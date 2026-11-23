using UnityEngine;

namespace MechaCyberpunk.IA.APie
{
    /// <summary>
    /// T-1701 (T66): reglas puras de los pandilleros de una misión (ModoMision). Cuándo uno está inalcanzable (bajo el mapa, lejos de su zona o
    /// atorado) y a dónde se reubica (la calle cerca del jugador, a la vista). La misión solo cuenta a los vivos y activos.
    /// </summary>
    public static class ReglaMisionPandillero
    {
        /// <summary>Segundos inalcanzable o atorado antes de reubicarlo.</summary>
        public const float SegundosParaReubicar = 20f;
        /// <summary>Cuánto por debajo de la altura de su zona (m) cuenta como bajo el mapa.</summary>
        public const float CaidaBajoMapa = 6f;
        /// <summary>Distancia plana a su zona (m) más allá del radio de territorio que cuenta como fuera del área.</summary>
        public const float MargenFueraDeZona = 40f;
        /// <summary>Si el jugador está más lejos que esto de la zona (m), no se reubica a nadie.</summary>
        public const float JugadorLejosDeLaZona = 100f;
        /// <summary>Lo que se mueve (m) en la ventana para no contar como atorado, y la distancia al jugador desde la que cuenta.</summary>
        public const float AvanceMinimo = 0.5f;
        public const float DistanciaAtasco = 15f;
        /// <summary>Distancia (m) delante del jugador a la que se reubica.</summary>
        public const float DistanciaReubicacion = 8f;

        /// <summary>True si está bajo el mapa o fuera del área de su zona.</summary>
        public static bool FueraDeLugar(Vector3 posicion, Vector3 centroZona, float radioZona)
        {
            if (posicion.y < centroZona.y - CaidaBajoMapa)
            {
                return true;
            }
            Vector3 d = posicion - centroZona;
            d.y = 0f;
            float maximo = radioZona + MargenFueraDeZona;
            return d.sqrMagnitude > maximo * maximo;
        }

        /// <summary>True si lleva toda la ventana casi sin moverse y está lejos del jugador (atorado).</summary>
        public static bool Atorado(Vector3 posicionAhora, Vector3 posicionAntes, Vector3 jugador)
        {
            Vector3 avance = posicionAhora - posicionAntes;
            Vector3 aJugador = jugador - posicionAhora;
            return avance.sqrMagnitude < AvanceMinimo * AvanceMinimo && aJugador.sqrMagnitude > DistanciaAtasco * DistanciaAtasco;
        }

        /// <summary>El punto candidato número <paramref name="intento"/> (0 a 7) para reubicar: en círculo alrededor del jugador, empezando delante de él.</summary>
        public static Vector3 Candidato(Vector3 jugador, Vector3 adelante, int intento)
        {
            Vector3 plano = new Vector3(adelante.x, 0f, adelante.z);
            if (plano.sqrMagnitude < 0.0001f)
            {
                plano = Vector3.forward;
            }
            return jugador + Quaternion.Euler(0f, 45f * intento, 0f) * plano.normalized * DistanciaReubicacion;
        }

        /// <summary>True si el grupo ya terminó: cada pandillero está caído, venció, ya no existe o nunca llegó a aparecer (inactivo con el grupo ya en marcha).</summary>
        public static bool NoBloquea(bool existe, bool vencido, bool ko, bool activo, bool grupoEnMarcha)
        {
            return !existe || vencido || ko || (!activo && grupoEnMarcha);
        }
    }
}
