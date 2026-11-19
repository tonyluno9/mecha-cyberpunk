namespace MechaCyberpunk.Vehiculos.Efectos
{
    /// <summary>
    /// T-1472: tope de sonidos de explosión en cadena (carros, barriles, dron): no más de <see cref="MaxEnVentana"/> dentro de <see cref="Ventana"/>
    /// segundos y al menos <see cref="Separacion"/> segundos entre una y otra, para que diez explosiones del mismo frame no se sumen en un estruendo.
    /// Lógica pura: recibe el tiempo. <see cref="Compartido"/> es el que usa el juego.
    /// </summary>
    public sealed class LimiteSonidoExplosion
    {
        public const int MaxEnVentana = 3;
        public const float Ventana = 0.6f;
        public const float Separacion = 0.08f;

        /// <summary>El límite que comparten todas las explosiones de vehículos.</summary>
        public static readonly LimiteSonidoExplosion Compartido = new LimiteSonidoExplosion();

        private readonly float[] tiempos = new float[MaxEnVentana];
        private int cuantos;

        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Limpiar() => Compartido.Reiniciar();

        /// <summary>true si esta explosión puede sonar ahora (y la cuenta); false si se omite.</summary>
        public bool Permite(float ahora)
        {
            int vivos = 0;
            float ultimo = float.NegativeInfinity;
            for (int i = 0; i < cuantos; i++)
            {
                if (ahora - tiempos[i] < Ventana)
                {
                    tiempos[vivos++] = tiempos[i];
                    ultimo = tiempos[i] > ultimo ? tiempos[i] : ultimo;
                }
            }
            cuantos = vivos;
            if (cuantos >= MaxEnVentana || ahora - ultimo < Separacion)
            {
                return false;
            }
            tiempos[cuantos++] = ahora;
            return true;
        }

        public void Reiniciar() => cuantos = 0;

        /// <summary>El sonido de la onda de una explosión de vehículo.</summary>
        public const string IdOnda = "EXP_ONDA";

        /// <summary>El fuego chico en bucle de un carro que arde.</summary>
        public const string IdFuegoChico = "FUE_LOOP_CHICO";

        /// <summary>Suena la explosión en 3D si el tope lo permite. Devuelve true si sonó.</summary>
        public static bool Sonar(UnityEngine.Vector3 donde)
        {
            if (!Compartido.Permite(UnityEngine.Time.time))
            {
                return false;
            }
            MechaCyberpunk.Core.Sonidos.ReproducirEn(IdOnda, donde);
            return true;
        }
    }
}
