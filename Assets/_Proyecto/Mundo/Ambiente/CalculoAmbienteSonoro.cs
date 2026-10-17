using UnityEngine;

namespace MechaCyberpunk.Mundo.Ambiente
{
    /// <summary>Volúmenes base y factores de la mezcla de la ciudad (T-097), tomados de ConfigAudioCiudad.</summary>
    public readonly struct NivelesAmbiente
    {
        public readonly float Lluvia;
        public readonly float Neon;
        public readonly float Trafico;
        public readonly float Musica;
        public readonly float FactorInterior;       // cuánto del sonido de afuera se oye dentro de un local (0 a 1)
        public readonly float FactorTraficoNoche;   // cuánto tráfico queda de noche (0 a 1)

        public NivelesAmbiente(float lluvia, float neon, float trafico, float musica, float factorInterior, float factorTraficoNoche)
        {
            Lluvia = lluvia;
            Neon = neon;
            Trafico = trafico;
            Musica = musica;
            FactorInterior = factorInterior;
            FactorTraficoNoche = factorTraficoNoche;
        }
    }

    /// <summary>Volumen objetivo de cada capa del ambiente sonoro.</summary>
    public readonly struct MezclaAmbiente
    {
        public readonly float Lluvia;
        public readonly float Neon;
        public readonly float Trafico;
        public readonly float Musica;

        public MezclaAmbiente(float lluvia, float neon, float trafico, float musica)
        {
            Lluvia = lluvia;
            Neon = neon;
            Trafico = trafico;
            Musica = musica;
        }
    }

    /// <summary>Reglas puras de la mezcla (T-097), sin Unity salvo Mathf, para probarlas en EditMode.</summary>
    public static class CalculoAmbienteSonoro
    {
        /// <summary>
        /// Afuera suenan la lluvia (si llueve), el zumbido del neón (de noche) y el tráfico (menos de noche). Dentro de un local queda la
        /// música y el resto se amortigua por el factor interior; el zumbido del neón no se oye adentro.
        /// </summary>
        public static MezclaAmbiente Calcular(bool jugadorDentro, bool deNoche, bool hayLluvia, NivelesAmbiente niveles)
        {
            float interior = jugadorDentro ? niveles.FactorInterior : 1f;
            float lluvia = hayLluvia ? niveles.Lluvia * interior : 0f;
            float neon = deNoche && !jugadorDentro ? niveles.Neon : 0f;
            float trafico = niveles.Trafico * interior * (deNoche ? niveles.FactorTraficoNoche : 1f);
            float musica = jugadorDentro ? niveles.Musica : 0f;
            return new MezclaAmbiente(lluvia, neon, trafico, musica);
        }

        /// <summary>Acerca un volumen a su objetivo a un ritmo de 1 unidad de volumen cada 'segundosCambio' segundos.</summary>
        public static float Acercar(float actual, float objetivo, float dt, float segundosCambio)
        {
            float paso = segundosCambio <= 0f ? float.MaxValue : dt / segundosCambio;
            return Mathf.MoveTowards(actual, objetivo, paso);
        }
    }
}
