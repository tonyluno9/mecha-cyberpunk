using System;

namespace MechaCyberpunk.IA.APie
{
    /// <summary>
    /// T-1553: cuándo el robot pesado E16 camina protegiéndose con el escudo (E16_CaminarEscudo, decisión de Tony 20-nov): al acercarse al jugador o al recibir disparos.
    /// Lógica pura. El escudo se levanta en pelea si el jugador está a <see cref="Parametros.DistanciaEscudo"/> o menos, o si lo golpearon en los últimos
    /// <see cref="Parametros.SegundosTrasImpacto"/> s. Para no parpadear en el borde, una vez arriba solo baja cuando el jugador pasa de
    /// DistanciaEscudo x <see cref="Parametros.FactorHisteresis"/> y no hubo impactos recientes.
    /// </summary>
    public sealed class ReglaEscudoArriba
    {
        public readonly struct Parametros
        {
            public readonly float DistanciaEscudo;
            public readonly float SegundosTrasImpacto;
            public readonly float FactorHisteresis;

            public Parametros(float distanciaEscudo, float segundosTrasImpacto, float factorHisteresis)
            {
                DistanciaEscudo = distanciaEscudo;
                SegundosTrasImpacto = segundosTrasImpacto;
                FactorHisteresis = factorHisteresis;
            }

            /// <summary>Se cubre a 12 m o menos y 3 s después de cada impacto; baja pasados 1.25 veces la distancia.</summary>
            public static Parametros Diseno => new Parametros(12f, 3f, 1.25f);
        }

        private readonly Parametros p;
        private float sinImpacto;

        public ReglaEscudoArriba(Parametros parametros)
        {
            p = parametros;
            sinImpacto = parametros.SegundosTrasImpacto + 1f;
        }

        public Parametros Config => p;
        public bool EscudoArriba { get; private set; }

        /// <summary>Un impacto (o un disparo oído del jugador): reinicia el tiempo de cobertura.</summary>
        public void RegistrarImpacto() => sinImpacto = 0f;

        public void Reiniciar()
        {
            EscudoArriba = false;
            sinImpacto = p.SegundosTrasImpacto + 1f;
        }

        /// <summary>Un cuadro. <paramref name="enPelea"/>: provocado y el jugador atacable. <paramref name="bloqueado"/>: está disparando, golpeando o aturdido (el escudo no se anima).</summary>
        public bool Avanzar(float dt, float distancia, bool enPelea, bool bloqueado)
        {
            sinImpacto += dt;
            if (!enPelea || bloqueado)
            {
                EscudoArriba = false;
                return false;
            }
            bool impactoReciente = sinImpacto < p.SegundosTrasImpacto;
            float limite = EscudoArriba ? p.DistanciaEscudo * p.FactorHisteresis : p.DistanciaEscudo;
            EscudoArriba = impactoReciente || distancia <= limite;
            return EscudoArriba;
        }
    }
}
