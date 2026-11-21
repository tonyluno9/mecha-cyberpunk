using System;

namespace MechaCyberpunk.Vehiculos
{
    /// <summary>
    /// T-1534: cuándo suena cada parte del carro eléctrico (LEEME_SONIDOS: turbinas, sin ralentí). Parado va en silencio; MOVER es un bucle que suena en movimiento
    /// (con histéresis para no prender y apagar), ACELERAR suena al pisar a fondo desde lento y PASAR una vez al llegar a buena velocidad. Pura: el tiempo entra como parámetro.
    /// </summary>
    public sealed class ReglaMotorElectrico
    {
        /// <summary>Rapidez (m/s) desde la que empieza el bucle MOVER.</summary>
        public const float RapidezEncenderMover = 0.8f;
        /// <summary>Rapidez (m/s) por debajo de la que el bucle MOVER se detiene (silencio).</summary>
        public const float RapidezApagarMover = 0.4f;
        /// <summary>Fracción de la rapidez máxima desde la que suena PASAR.</summary>
        public const float FraccionPasar = 0.6f;
        /// <summary>Fracción de la rapidez máxima por debajo de la que PASAR vuelve a quedar listo.</summary>
        public const float FraccionRearmarPasar = 0.35f;
        public const float SegundosEntrePasar = 6f;
        /// <summary>Los dos ACELERAR duran 7 y 10 s: no se repiten antes de este tiempo.</summary>
        public const float SegundosEntreAcelerar = 5f;
        /// <summary>El acelerador suena solo yendo a menos de esta fracción de la rapidez máxima.</summary>
        public const float FraccionMaximaAcelerar = 0.5f;
        public const float PedalAlto = 0.9f;
        public const float PedalBajo = 0.5f;

        /// <summary>Lo que hay que hacer en este paso.</summary>
        public readonly struct Resultado
        {
            public readonly bool IniciarMover;
            public readonly bool DetenerMover;
            public readonly bool Acelerar;
            public readonly bool Pasar;

            public Resultado(bool iniciarMover, bool detenerMover, bool acelerar, bool pasar)
            {
                IniciarMover = iniciarMover;
                DetenerMover = detenerMover;
                Acelerar = acelerar;
                Pasar = pasar;
            }
        }

        private bool moviendo;
        private bool pasarListo = true;
        private float pedalAnterior;
        private float desdeAcelerar = SegundosEntreAcelerar;
        private float desdePasar = SegundosEntrePasar;

        /// <summary>True mientras el bucle MOVER debe estar sonando.</summary>
        public bool Moviendo => moviendo;

        public void Reiniciar()
        {
            moviendo = false;
            pasarListo = true;
            pedalAnterior = 0f;
            desdeAcelerar = SegundosEntreAcelerar;
            desdePasar = SegundosEntrePasar;
        }

        public Resultado Actualizar(float rapidez, float rapidezMaxima, float pedal, float dt)
        {
            desdeAcelerar += Math.Max(0f, dt);
            desdePasar += Math.Max(0f, dt);
            float r = Math.Abs(rapidez);
            bool iniciar = false;
            bool detener = false;
            if (!moviendo && r >= RapidezEncenderMover)
            {
                moviendo = true;
                iniciar = true;
            }
            else if (moviendo && r < RapidezApagarMover)
            {
                moviendo = false;
                detener = true;
            }
            bool acelerar = pedal >= PedalAlto && pedalAnterior < PedalBajo && r <= rapidezMaxima * FraccionMaximaAcelerar && desdeAcelerar >= SegundosEntreAcelerar;
            if (acelerar)
            {
                desdeAcelerar = 0f;
            }
            pedalAnterior = pedal;
            bool pasar = false;
            if (r < rapidezMaxima * FraccionRearmarPasar)
            {
                pasarListo = true;
            }
            else if (pasarListo && r >= rapidezMaxima * FraccionPasar && desdePasar >= SegundosEntrePasar)
            {
                pasarListo = false;
                desdePasar = 0f;
                pasar = true;
            }
            return new Resultado(iniciar, detener, acelerar, pasar);
        }
    }
}
