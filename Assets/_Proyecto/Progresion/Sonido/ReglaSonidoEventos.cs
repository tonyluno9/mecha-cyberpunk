using System.Collections.Generic;

namespace MechaCyberpunk.Progresion.Sonido
{
    /// <summary>T-1535: reglas puras de los sonidos de eventos del juego (misiones, implantes, arresto). Sin Unity, para probarlas en EditMode.</summary>
    public static class ReglaSonidoEventos
    {
        public const string MisionFallida = "UI_OBJETIVO_FALLIDO";
        public const string Temporizador = "MIS_TEMPORIZADOR";
        public const string Taladro = "IMP_INSTALAR_TALADRO";
        public const string Sinapsis = "IMP_SINAPSIS";
        public const string Clic = "TIE_EQUIPAR";
        public const string EscudoGolpe = "EXO_ESCUDO_GOLPE";
        public const string VisorActivar = "EXO_VISOR_ACTIVAR";
        public const string Muerte = "PAN_HAS_MUERTO";
        public const string Esposas = "PAN_ARRESTADO_ESPOSAS";
        public const string GolpeArresto = "MEL_GOLPE";
        public const string SirenaArresto = "POL_SIRENA_LOOP";

        /// <summary>Con qué retraso suenan las esposas tras el golpe del arresto (s).</summary>
        public const float SegundosEsposas = 0.35f;

        /// <summary>Qué tan rápido se aleja la sirena de la patrulla que se lleva al arrestado (m/s) y cuánto dura (s).</summary>
        public const float VelocidadSirenaArresto = 20f;
        public const float SegundosSirenaArresto = 4f;

        /// <summary>Cuando quedan estos segundos o menos de una misión con límite de tiempo, suena el temporizador (s).</summary>
        public const float UmbralTemporizador = 10f;

        /// <summary>Cada cuántos segundos suena el temporizador en esa cuenta final (el sonido dura 1.8 s) (s).</summary>
        public const float IntervaloTemporizador = 2f;

        /// <summary>Un paso de una secuencia: qué suena y cuántos segundos después de empezar.</summary>
        public readonly struct Paso
        {
            public Paso(string id, float segundos)
            {
                Id = id;
                Segundos = segundos;
            }

            public string Id { get; }
            public float Segundos { get; }
        }

        /// <summary>Instalar un implante con el Doc: el taladro (6.9 s) con un clic al empezar, dos pitidos de sinapsis casi al terminar y el clic final.</summary>
        public static IReadOnlyList<Paso> InstalarImplante { get; } = new[]
        {
            new Paso(Taladro, 0f),
            new Paso(Clic, 0.2f),
            new Paso(Sinapsis, 6.2f),
            new Paso(Sinapsis, 6.8f),
            new Paso(Clic, 7.4f),
        };

        /// <summary>Si toca un tic del temporizador: la misión tiene límite (quedan segundos), faltan <see cref="UmbralTemporizador"/> o menos y pasó el intervalo desde el último.</summary>
        public static bool TocaTemporizador(float segundosRestantes, float ahora, float ultimoTic)
        {
            return segundosRestantes > 0f && segundosRestantes <= UmbralTemporizador && ahora - ultimoTic >= IntervaloTemporizador;
        }
    }
}
