namespace MechaCyberpunk.Combate.Golpe
{
    /// <summary>
    /// Lógica pura del tiempo de un golpe: Listo, Preparando (hasta el impacto) y Recuperando (la recarga).
    /// El impacto ocurre una sola vez, en el <see cref="Avanzar"/> que cruza el final de la preparación.
    /// </summary>
    public class CicloGolpe
    {
        private readonly float preparacion;
        private readonly float recarga;
        private float preparacionActual;
        private float transcurrido;

        public CicloGolpe(float preparacion, float recarga)
        {
            this.preparacion = preparacion;
            this.recarga = recarga;
            preparacionActual = preparacion;
        }

        /// <summary>Duración de la preparación del golpe en curso (o del último): la de siempre o la que se pidió esta vez (s).</summary>
        public float PreparacionActual => preparacionActual;

        public FaseGolpe Fase { get; private set; }

        /// <summary>Avance de la fase actual de 0 a 1. En Listo es 0.</summary>
        public float Progreso
        {
            get
            {
                switch (Fase)
                {
                    case FaseGolpe.Preparando:
                        return Limitar01(transcurrido / preparacionActual);
                    case FaseGolpe.Recuperando:
                        return Limitar01(transcurrido / recarga);
                    default:
                        return 0f;
                }
            }
        }

        /// <summary>Empieza un golpe. Solo funciona en Listo; devuelve si empezó.</summary>
        public bool IntentarGolpear()
        {
            return IntentarGolpear(preparacion);
        }

        /// <summary>Empieza un golpe con una preparación distinta solo esta vez (T-202). Solo funciona en Listo.</summary>
        public bool IntentarGolpear(float preparacionEstaVez)
        {
            if (Fase != FaseGolpe.Listo)
            {
                return false;
            }
            preparacionActual = preparacionEstaVez;
            Fase = FaseGolpe.Preparando;
            transcurrido = 0f;
            return true;
        }

        /// <summary>Avanza el tiempo. Devuelve true solo en el frame del impacto.</summary>
        public bool Avanzar(float dt)
        {
            if (Fase == FaseGolpe.Listo || dt <= 0f)
            {
                return false;
            }

            bool impacto = false;
            transcurrido += dt;
            if (Fase == FaseGolpe.Preparando && transcurrido >= preparacionActual)
            {
                transcurrido -= preparacionActual;
                Fase = FaseGolpe.Recuperando;
                impacto = true;
            }
            if (Fase == FaseGolpe.Recuperando && transcurrido >= recarga)
            {
                Fase = FaseGolpe.Listo;
                transcurrido = 0f;
            }
            return impacto;
        }

        /// <summary>Cancela el golpe en curso sin impacto.</summary>
        public void Reiniciar()
        {
            Fase = FaseGolpe.Listo;
            transcurrido = 0f;
        }

        private static float Limitar01(float valor)
        {
            return valor < 0f ? 0f : (valor > 1f ? 1f : valor);
        }
    }
}
