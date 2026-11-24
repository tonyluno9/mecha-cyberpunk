using MechaCyberpunk.Core;

namespace MechaCyberpunk.Progresion.Sonido
{
    /// <summary>
    /// T-musica-24nov: lógica pura de la música por fases. Qué pista toca cada <see cref="FaseJuego.Fase"/>, que nunca vuelva a una fase anterior
    /// mientras el jugador sigue vivo (sí desde HAS MUERTO: REINTENTAR vuelve a SKULLS), cuánto baja en un interior y con la narración.
    /// </summary>
    public static class ReglaMusicaFases
    {
        public const string PianoIntro = "MUS_PIANO_INTRO";
        public const string CampoTiro = "MUS_CAMPO_TIRO";
        public const string Skulls = "MUS_FASE_SKULLS";
        public const string Policia = "MUS_FASE_POLICIA";
        public const string Nova = "MUS_FASE_NOVA";

        /// <summary>Todas las pistas (para el importador).</summary>
        public static readonly string[] Pistas = { PianoIntro, CampoTiro, Skulls, Policia, Nova };

        /// <summary>Lo que dura un cruce entre dos pistas (s, la tarea pide 2 a 3).</summary>
        public const float SegundosCruce = 2.5f;
        /// <summary>Lo que baja la música mientras habla la narración: -10 dB.</summary>
        public const float DecibelesNarracion = -10f;
        /// <summary>Lo que baja la música de las fases del modo libre en un interior (Doc, Tino): -12 dB.</summary>
        public const float DecibelesInterior = -12f;
        /// <summary>Lo que tarda en bajar o subir por interior o narración (s).</summary>
        public const float SegundosAjuste = 0.8f;

        /// <summary>La pista de cada fase.</summary>
        public static string PistaDe(FaseJuego.Fase fase)
        {
            switch (fase)
            {
                case FaseJuego.Fase.CampoTiro: return CampoTiro;
                case FaseJuego.Fase.Skulls: return Skulls;
                case FaseJuego.Fase.Policia: return Policia;
                case FaseJuego.Fase.Nova: return Nova;
                default: return PianoIntro; // Guiada y Muerto
            }
        }

        /// <summary>
        /// La fase que debe sonar si suena <paramref name="sonando"/> y el juego pide <paramref name="pedida"/>: avanza siempre; no vuelve atrás,
        /// salvo desde <see cref="FaseJuego.Fase.Muerto"/> (REINTENTAR) y entre la guiada y el campo de tiro (las dos son la parte guiada).
        /// </summary>
        public static FaseJuego.Fase Elegir(FaseJuego.Fase sonando, FaseJuego.Fase pedida)
        {
            if (pedida >= sonando || sonando == FaseJuego.Fase.Muerto)
            {
                return pedida;
            }
            bool guiadas = sonando <= FaseJuego.Fase.CampoTiro && pedida <= FaseJuego.Fase.CampoTiro;
            return guiadas ? pedida : sonando;
        }

        /// <summary>True si la fase es del modo libre (SKULLS, policía, NOVA): solo esas bajan en un interior.</summary>
        public static bool EsModoLibre(FaseJuego.Fase fase) => fase >= FaseJuego.Fase.Skulls && fase != FaseJuego.Fase.Muerto;

        /// <summary>True si el lugar es un interior (todo lo que no es la calle).</summary>
        public static bool EsInterior(IdLugar lugar) => lugar != IdLugar.Calle && lugar != IdLugar.Ninguno;

        /// <summary>Factor lineal (0 a 1) de volumen por el lugar y la fase.</summary>
        public static float FactorInterior(IdLugar lugar, FaseJuego.Fase fase)
        {
            return EsInterior(lugar) && EsModoLibre(fase) ? Lineal(DecibelesInterior) : 1f;
        }

        /// <summary>Factor lineal por la narración.</summary>
        public static float FactorNarracion(bool narrando) => narrando ? Lineal(DecibelesNarracion) : 1f;

        /// <summary>Decibeles a factor lineal.</summary>
        public static float Lineal(float decibeles) => (float)System.Math.Pow(10.0, decibeles / 20.0);
    }
}
