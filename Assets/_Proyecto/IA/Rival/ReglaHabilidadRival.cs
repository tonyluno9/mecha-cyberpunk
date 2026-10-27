using System;

namespace MechaCyberpunk.IA.Rival
{
    /// <summary>
    /// Lógica pura de la habilidad de un jefe (T-178): cuenta el enfriamiento, lleva el aviso y decide el lanzamiento.
    /// El enfriamiento avanza como fracción, así que si el jefe entra en furia a medias el resto corre más rápido.
    /// El primer uso llega un enfriamiento completo después de <see cref="Reiniciar"/>.
    /// </summary>
    public class ReglaHabilidadRival
    {
        private readonly float enfriamientoBase;
        private readonly float factor;
        private readonly float factorFuria;
        private readonly float segundosAviso;
        private float progreso;
        private float avisoRestante;

        /// <param name="enfriamientoBase">Enfriamiento de la habilidad (s).</param>
        /// <param name="factor">Multiplica el enfriamiento (veces).</param>
        /// <param name="factorFuria">Se multiplica encima en furia (veces).</param>
        /// <param name="segundosAviso">Duración del aviso (s).</param>
        public ReglaHabilidadRival(float enfriamientoBase, float factor, float factorFuria, float segundosAviso)
        {
            this.enfriamientoBase = Math.Max(0f, enfriamientoBase);
            this.factor = Math.Max(0f, factor);
            this.factorFuria = Math.Max(0f, factorFuria);
            this.segundosAviso = Math.Max(0f, segundosAviso);
        }

        public bool Avisando { get; private set; }

        /// <summary>Cuánto del enfriamiento ya pasó, de 0 a 1.</summary>
        public float Progreso => progreso;

        /// <summary>Segundos que faltan para terminar el aviso (0 si no está avisando).</summary>
        public float AvisoRestante => Avisando ? avisoRestante : 0f;

        /// <summary>Enfriamiento total (s) con o sin furia.</summary>
        public float Enfriamiento(bool enFuria) => enfriamientoBase * factor * (enFuria ? factorFuria : 1f);

        /// <summary>Pelea nueva: el enfriamiento vuelve a empezar y el aviso se descarta.</summary>
        public void Reiniciar()
        {
            progreso = 0f;
            avisoRestante = 0f;
            Avisando = false;
        }

        /// <summary>
        /// Avanza <paramref name="dt"/> segundos. <paramref name="puedeEmpezar"/>: hay control, no está aturdido, no prepara un golpe
        /// y el objetivo está a distancia. <paramref name="puedeSeguir"/>: solo hay control y no está aturdido ni en KO.
        /// </summary>
        public PasoHabilidadRival Avanzar(float dt, bool enFuria, bool puedeEmpezar, bool puedeSeguir)
        {
            dt = Math.Max(0f, dt);
            if (Avisando)
            {
                if (!puedeSeguir)
                {
                    Reiniciar();
                    return PasoHabilidadRival.AvisoCortado;
                }
                avisoRestante -= dt;
                if (avisoRestante <= 0f)
                {
                    Reiniciar();
                    return PasoHabilidadRival.Lanzar;
                }
                return PasoHabilidadRival.Nada;
            }
            float total = Enfriamiento(enFuria);
            progreso = total <= 0f ? 1f : Math.Min(1f, progreso + dt / total);
            if (progreso >= 1f && puedeEmpezar)
            {
                Avisando = true;
                avisoRestante = segundosAviso;
                return PasoHabilidadRival.EmpezoAviso;
            }
            return PasoHabilidadRival.Nada;
        }
    }
}
