using System;

namespace MechaCyberpunk.Progresion
{
    /// <summary>
    /// Lleva los pasos de la misión en curso (T-183). Pura: no sabe nada de escena; quien la usa llama a
    /// <see cref="Cumplir"/>, <see cref="Fallar"/> y <see cref="Avanzar"/>.
    /// </summary>
    public class MaquinaMision
    {
        private float transcurrido;

        /// <summary>La misión en curso o la última que terminó. Null si nunca se empezó una.</summary>
        public DatosMision Datos { get; private set; }

        /// <summary>True si hay una misión empezada y sin terminar.</summary>
        public bool Activa { get; private set; }

        /// <summary>Índice del paso actual (desde 0). Al cumplirse la misión vale la cantidad de pasos.</summary>
        public int IndicePaso { get; private set; }

        /// <summary>Paso actual, o null si no hay misión activa.</summary>
        public PasoMision PasoActual => Activa ? Datos.Pasos[IndicePaso] : null;

        /// <summary>Segundos desde que empezó.</summary>
        public float Transcurrido => transcurrido;

        /// <summary>Segundos que quedan; 0 si no hay límite o no hay misión activa.</summary>
        public float Restante => Activa && Datos.LimiteSegundos > 0f ? Math.Max(0f, Datos.LimiteSegundos - transcurrido) : 0f;

        /// <summary>Se avisa al pasar al siguiente paso (no al empezar ni al terminar).</summary>
        public event Action PasoCambio;

        /// <summary>Se avisa una sola vez al terminar: (éxito, motivo). El motivo es Ninguno si se cumplió.</summary>
        public event Action<bool, MotivoFalloMision> Terminada;

        /// <summary>Empieza una misión. False si ya hay una activa o si no tiene pasos.</summary>
        public bool Empezar(DatosMision datos)
        {
            if (Activa || datos == null || datos.Pasos.Length == 0) return false;
            Datos = datos;
            Activa = true;
            IndicePaso = 0;
            transcurrido = 0f;
            return true;
        }

        /// <summary>Cumple el paso actual. Con el último, la misión termina con éxito. Sin misión activa no hace nada.</summary>
        public void Cumplir()
        {
            if (!Activa) return;
            IndicePaso++;
            if (IndicePaso >= Datos.Pasos.Length)
            {
                Activa = false;
                Terminada?.Invoke(true, MotivoFalloMision.Ninguno);
                return;
            }
            PasoCambio?.Invoke();
        }

        /// <summary>Termina la misión mal. Sin misión activa no hace nada, así que avisa una sola vez.</summary>
        public void Fallar(MotivoFalloMision motivo)
        {
            if (!Activa) return;
            Activa = false;
            Terminada?.Invoke(false, motivo);
        }

        /// <summary>Cuenta el tiempo. Con límite, falla por Tiempo cuando se llega a él (no antes).</summary>
        public void Avanzar(float dt)
        {
            if (!Activa || dt <= 0f) return;
            transcurrido += dt;
            if (Datos.LimiteSegundos > 0f && transcurrido >= Datos.LimiteSegundos) Fallar(MotivoFalloMision.Tiempo);
        }
    }
}
