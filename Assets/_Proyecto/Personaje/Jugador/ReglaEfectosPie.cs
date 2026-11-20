using System;

namespace MechaCyberpunk.Personaje.Jugador
{
    /// <summary>T-1489: cálculos puros de los detalles de sensación a pie: polvo al caer, salpicadura en charcos y temblor al recibir daño.</summary>
    public static class ReglaEfectosPie
    {
        /// <summary>Caída (m) mínima para que se levante polvo.</summary>
        public const float CaidaMinimaPolvo = 1f;
        /// <summary>Caída (m) a partir de la cual el polvo es el máximo.</summary>
        public const float CaidaPolvoMaximo = 6f;
        /// <summary>Partículas de polvo con intensidad 1 y con la caída mínima.</summary>
        public const int PolvoMaximo = 28;
        public const int PolvoMinimo = 6;

        /// <summary>Radio (0 a 0.5, en unidades locales del quad) dentro del cual se cuenta como pisar el charco: la orilla es irregular y no llega a 0.5.</summary>
        public const float RadioCharco = 0.4f;
        /// <summary>Distancia vertical máxima (m) entre el pie y el charco para contar como pisarlo.</summary>
        public const float AlturaMaximaCharco = 0.6f;
        public const int GotasPorPaso = 18;
        public const int GotasPorPasoCorriendo = 30;

        /// <summary>Daño que produce el temblor mínimo y el máximo; la fuerza se escala entre los dos extremos.</summary>
        public const float DanioTemblorMinimo = 1f;
        public const float DanioTemblorMaximo = 40f;
        public const float FuerzaTemblorMinima = 0.4f;
        public const float FuerzaTemblorMaxima = 1.5f;

        /// <summary>Intensidad 0 a 1 del polvo según la caída; 0 si no llega al mínimo.</summary>
        public static float IntensidadPolvo(float caidaMetros)
        {
            if (caidaMetros < CaidaMinimaPolvo)
            {
                return 0f;
            }
            return Math.Clamp((caidaMetros - CaidaMinimaPolvo) / (CaidaPolvoMaximo - CaidaMinimaPolvo), 0f, 1f);
        }

        /// <summary>Cuántas partículas de polvo salen: 0 si no hay polvo, de PolvoMinimo a PolvoMaximo si lo hay.</summary>
        public static int ParticulasPolvo(float caidaMetros)
        {
            if (caidaMetros < CaidaMinimaPolvo)
            {
                return 0;
            }
            return PolvoMinimo + (int)Math.Round(IntensidadPolvo(caidaMetros) * (PolvoMaximo - PolvoMinimo));
        }

        /// <summary>
        /// True si el punto (coordenadas locales del quad del charco: x, y de -0.5 a 0.5) cae dentro del charco
        /// y el pie no está lejos en vertical (<paramref name="diferenciaAltura"/>, m).
        /// </summary>
        public static bool PisaCharco(float localX, float localY, float diferenciaAltura)
        {
            if (Math.Abs(diferenciaAltura) > AlturaMaximaCharco)
            {
                return false;
            }
            return localX * localX + localY * localY <= RadioCharco * RadioCharco;
        }

        public static int GotasSalpicadura(bool corriendo) => corriendo ? GotasPorPasoCorriendo : GotasPorPaso;

        /// <summary>Fuerza del temblor de cámara (0.4 a 1.5) según el daño recibido; 0 si no hubo daño.</summary>
        public static float FuerzaTemblorDanio(float danio)
        {
            if (danio <= 0f)
            {
                return 0f;
            }
            float t = Math.Clamp((danio - DanioTemblorMinimo) / (DanioTemblorMaximo - DanioTemblorMinimo), 0f, 1f);
            return FuerzaTemblorMinima + t * (FuerzaTemblorMaxima - FuerzaTemblorMinima);
        }
    }
}
