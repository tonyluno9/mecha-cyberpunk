using System;

namespace MechaCyberpunk.Mecha.Encendido
{
    /// <summary>
    /// Lógica pura del encendido de 3 pasos. Paso 1: tecla, luego anima. Paso 2: mantener una tecla seguida (si se suelta, el avance
    /// vuelve a 0). Paso 3: tecla, luego anima. Entre pasos hay una pausa. Las teclas de otro paso no hacen nada.
    /// </summary>
    public class SecuenciaEncendido
    {
        private enum Fase
        {
            Esperando,
            Animando,
            Pausa
        }

        private readonly float segundosPaso1;
        private readonly float segundosMantenerPaso2;
        private readonly float segundosPaso3;
        private readonly float pausaEntrePasos;

        private PasoEncendido paso = PasoEncendido.Apagado;
        private Fase fase = Fase.Esperando;
        private float tiempo;
        private bool mantenerAnterior;

        public SecuenciaEncendido(float segundosPaso1, float segundosMantenerPaso2, float segundosPaso3, float pausaEntrePasos)
        {
            this.segundosPaso1 = Math.Max(0f, segundosPaso1);
            this.segundosMantenerPaso2 = Math.Max(0f, segundosMantenerPaso2);
            this.segundosPaso3 = Math.Max(0f, segundosPaso3);
            this.pausaEntrePasos = Math.Max(0f, pausaEntrePasos);
        }

        public PasoEncendido Paso => paso;
        public bool EstaEncendido => paso == PasoEncendido.Encendido;

        /// <summary>El paso actual espera que se pulse su tecla.</summary>
        public bool EsperandoTecla => EnPaso && fase == Fase.Esperando;

        /// <summary>La entrada ya se confirmó (el paso 1 empezó a animarse o ya pasó): las luces se prenden.</summary>
        public bool EntradaConfirmada => paso > PasoEncendido.Paso1 || (paso == PasoEncendido.Paso1 && fase != Fase.Esperando);

        /// <summary>Avance del paso actual, de 0 a 1.</summary>
        public float Progreso
        {
            get
            {
                if (paso == PasoEncendido.Encendido)
                {
                    return 1f;
                }
                if (!EnPaso)
                {
                    return 0f;
                }
                if (fase == Fase.Pausa)
                {
                    return 1f;
                }
                if (fase == Fase.Esperando && paso != PasoEncendido.Paso2)
                {
                    return 0f;
                }
                float duracion = Duracion();
                return duracion > 0f ? Math.Min(1f, tiempo / duracion) : 1f;
            }
        }

        public event Action<PasoEncendido> PasoCambio;
        public event Action Completado;

        /// <summary>Se pulsó una tecla de otro paso mientras se esperaba la correcta.</summary>
        public event Action TeclaIncorrecta;

        private bool EnPaso => paso == PasoEncendido.Paso1 || paso == PasoEncendido.Paso2 || paso == PasoEncendido.Paso3;

        public void Reiniciar()
        {
            paso = PasoEncendido.Paso1;
            fase = Fase.Esperando;
            tiempo = 0f;
            mantenerAnterior = false;
            PasoCambio?.Invoke(paso);
        }

        /// <summary>Deja el encendido terminado al instante (arena, arranque directo).</summary>
        public void Completar()
        {
            if (paso == PasoEncendido.Encendido)
            {
                return;
            }
            paso = PasoEncendido.Encendido;
            fase = Fase.Esperando;
            tiempo = 0f;
            PasoCambio?.Invoke(paso);
            Completado?.Invoke();
        }

        /// <param name="dt">Segundos desde el frame anterior.</param>
        /// <param name="confirmar">Se pulsó la tecla del paso 1 en este frame.</param>
        /// <param name="mantenerLevantar">La tecla del paso 2 está presionada.</param>
        /// <param name="cristal">Se pulsó la tecla del paso 3 en este frame.</param>
        public void Avanzar(float dt, bool confirmar, bool mantenerLevantar, bool cristal)
        {
            bool inicioMantener = mantenerLevantar && !mantenerAnterior;
            mantenerAnterior = mantenerLevantar;
            if (!EnPaso)
            {
                return;
            }

            if (fase == Fase.Esperando)
            {
                bool equivocada = (paso != PasoEncendido.Paso1 && confirmar)
                    || (paso != PasoEncendido.Paso2 && inicioMantener)
                    || (paso != PasoEncendido.Paso3 && cristal);
                if (equivocada)
                {
                    TeclaIncorrecta?.Invoke();
                }
                if (paso == PasoEncendido.Paso2)
                {
                    if (!mantenerLevantar)
                    {
                        tiempo = 0f;
                    }
                }
                else if ((paso == PasoEncendido.Paso1 && confirmar) || (paso == PasoEncendido.Paso3 && cristal))
                {
                    fase = Fase.Animando;
                    tiempo = 0f;
                }
            }

            float restante = Math.Max(0f, dt);
            while (EnPaso)
            {
                float objetivo;
                if (fase == Fase.Animando)
                {
                    objetivo = Duracion();
                }
                else if (fase == Fase.Pausa)
                {
                    objetivo = pausaEntrePasos;
                }
                else if (paso == PasoEncendido.Paso2 && mantenerLevantar)
                {
                    objetivo = segundosMantenerPaso2;
                }
                else
                {
                    return;
                }

                float falta = objetivo - tiempo;
                if (restante < falta)
                {
                    tiempo += restante;
                    return;
                }
                restante -= falta;
                tiempo = 0f;
                if (fase == Fase.Pausa)
                {
                    paso++;
                    fase = Fase.Esperando;
                    PasoCambio?.Invoke(paso);
                }
                else if (paso == PasoEncendido.Paso3)
                {
                    paso = PasoEncendido.Encendido;
                    PasoCambio?.Invoke(paso);
                    Completado?.Invoke();
                }
                else
                {
                    fase = Fase.Pausa;
                }
            }
        }

        private float Duracion()
        {
            switch (paso)
            {
                case PasoEncendido.Paso1: return segundosPaso1;
                case PasoEncendido.Paso2: return segundosMantenerPaso2;
                default: return segundosPaso3;
            }
        }
    }
}
