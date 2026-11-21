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

        /// <summary>El sonido de la onda de una explosión genérica (EXP_ONDA: aterrizajes del super salto, etc.).</summary>
        public const string IdOnda = "EXP_ONDA";

        /// <summary>El fuego chico en bucle de un carro que arde antes de explotar.</summary>
        public const string IdFuegoChico = "FUE_LOOP_CHICO";

        /// <summary>T-1532: el fuego grande en bucle de lo que ya explotó.</summary>
        public const string IdFuegoGrande = "FUE_LOOP_GRANDE";

        /// <summary>T-1532: el fwoosh al prenderse algo; sigue el fuego en bucle.</summary>
        public const string IdFuegoEncender = "FUE_ENCENDER";

        public const string IdCarro = "EXP_CARRO";
        public const string IdBarril = "EXP_BARRIL";
        public const string IdDronRobot = "EXP_DRON_ROBOT";
        public const string IdNave = "EXP_NAVE";
        public const string IdEscombros = "EXP_ESCOMBROS";
        public const string IdCristales = "EXP_CRISTALES";

        /// <summary>Segundos entre el estruendo y los escombros o cristales que lo siguen.</summary>
        public const float RetrasoDetalle = 0.3f;

        /// <summary>T-1532: el estruendo que pide cada tipo de explosión.</summary>
        public static string IdPara(TipoExplosionSonido tipo)
        {
            switch (tipo)
            {
                case TipoExplosionSonido.Carro: return IdCarro;
                case TipoExplosionSonido.Barril: return IdBarril;
                case TipoExplosionSonido.DronRobot: return IdDronRobot;
                case TipoExplosionSonido.Nave: return IdNave;
                default: return IdOnda;
            }
        }

        /// <summary>
        /// T-1532: lo que suena justo después de una explosión grande: escombros siempre, y cristales si había vidrio (carro y nave). El dron y el
        /// barril solo dan escombros (el barril) o nada (el dron, que es chico).
        /// </summary>
        public static string[] IdsDetalle(TipoExplosionSonido tipo)
        {
            switch (tipo)
            {
                case TipoExplosionSonido.Carro:
                case TipoExplosionSonido.Nave:
                    return new[] { IdEscombros, IdCristales };
                case TipoExplosionSonido.Barril:
                    return new[] { IdEscombros };
                default:
                    return new string[0];
            }
        }

        /// <summary>El sonido de la onda de una explosión de vehículo (EXP_ONDA). Se conserva por compatibilidad; el juego pide <see cref="Sonar(TipoExplosionSonido, UnityEngine.Vector3)"/>.</summary>
        public static bool Sonar(UnityEngine.Vector3 donde) => Sonar(TipoExplosionSonido.Onda, donde);

        /// <summary>Suena la explosión del tipo dado en 3D (y sus escombros y cristales) si el tope lo permite. Devuelve true si sonó.</summary>
        public static bool Sonar(TipoExplosionSonido tipo, UnityEngine.Vector3 donde)
        {
            if (!Compartido.Permite(UnityEngine.Time.time))
            {
                return false;
            }
            MechaCyberpunk.Core.Sonidos.ReproducirEn(IdPara(tipo), donde);
            string[] detalle = IdsDetalle(tipo);
            if (detalle.Length > 0)
            {
                DetalleExplosionSonido.Programar(donde, detalle, RetrasoDetalle);
            }
            return true;
        }
    }

    /// <summary>T-1532: qué explosión suena.</summary>
    public enum TipoExplosionSonido
    {
        Onda,
        Carro,
        Barril,
        DronRobot,
        Nave,
    }
}
