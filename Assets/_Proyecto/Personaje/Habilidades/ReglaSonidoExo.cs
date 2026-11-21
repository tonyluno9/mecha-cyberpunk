namespace MechaCyberpunk.Personaje.Habilidades
{
    /// <summary>T-1535: los sonidos del exo y las prótesis (LEEME_SONIDOS 20-nov, "Exo e implantes"). Reglas puras, para probarlas sin escena.</summary>
    public static class ReglaSonidoExo
    {
        /// <summary>Llevar activa una prótesis (piernas o brazos): golpe suave que se repite; reemplaza al golpe del brazo robótico.</summary>
        public const string Puesto = "EXO_PUESTO_LOOP";
        public const string SaltoSuper = "EXO_PIERNAS_SALTO_SUPER";
        public const string Servos = "EXO_SERVOS_CAMINAR";
        public const string ServosFuerza = "ROB_SERVOS";
        /// <summary>Habilidad sin implante o sin energía (recargando).</summary>
        public const string Error = "UI_ERROR";
        /// <summary>Aterrizar de un súper salto: el golpe del aterrizaje más una onda bajita.</summary>
        public const string OndaAterrizaje = "EXP_ONDA";

        /// <summary>Cada cuántos segundos suena el golpe del exo puesto mientras una prótesis está activa (s).</summary>
        public const float SegundosEntrePulsos = 2.5f;

        /// <summary>
        /// El sonido al activar una habilidad: piernas = servos del exo, brazos = servos de fuerza. La cabeza (el visor) y los ataques (láser, misiles)
        /// ya tienen el suyo (el visor lo pide SonidosEventos al cambiar VisorJugador.ModoActivo; los ataques, al disparar): null.
        /// </summary>
        public static string AlActivar(HabilidadProtesis habilidad)
        {
            switch (habilidad)
            {
                case HabilidadProtesis.Piernas: return Servos;
                case HabilidadProtesis.Brazos: return ServosFuerza;
                default: return null;
            }
        }

        /// <summary>Si toca un golpe del exo puesto: hay una prótesis de piernas o brazos activa y pasó el intervalo desde el último.</summary>
        public static bool TocaPulso(bool piernasActiva, bool brazosActiva, float ahora, float ultimo)
        {
            return (piernasActiva || brazosActiva) && ahora - ultimo >= SegundosEntrePulsos;
        }
    }
}
