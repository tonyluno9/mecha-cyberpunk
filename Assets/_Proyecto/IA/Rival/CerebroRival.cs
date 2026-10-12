using System;
using MechaCyberpunk.Core;

namespace MechaCyberpunk.IA.Rival
{
    /// <summary>
    /// Lógica pura del rival: con la distancia al objetivo decide si espera, se acerca o empieza un golpe.
    /// La espera entre golpes corre solo con control y con el objetivo en pie; quien lo usa debe llamar a
    /// <see cref="Decidir"/> únicamente mientras el rival puede golpear (no durante un golpe en curso).
    /// </summary>
    public class CerebroRival
    {
        private readonly DatosRival datos;
        private readonly Func<float> azar;
        private float espera;

        /// <param name="datos">Distancias y esperas del rival.</param>
        /// <param name="azar">Devuelve un número de 0 a 1. Si es null usa <see cref="System.Random"/>.</param>
        public CerebroRival(DatosRival datos, Func<float> azar = null)
        {
            this.datos = datos;
            if (azar == null)
            {
                Random generador = new Random();
                azar = () => (float)generador.NextDouble();
            }
            this.azar = azar;
            espera = ElegirEspera();
        }

        /// <summary>Segundos que faltan para poder empezar el siguiente golpe.</summary>
        public float EsperaRestante => espera;

        /// <summary>Multiplica la espera que se elige entre golpes (1 = normal).</summary>
        public float MultiplicadorEspera { get; set; } = 1f;

        /// <summary>Decide qué hacer. <paramref name="distancia"/> es la horizontal al objetivo (m).</summary>
        public DecisionRival Decidir(float distancia, bool objetivoKO, bool tieneControl, float dt)
        {
            if (!tieneControl || objetivoKO)
            {
                return DecisionRival.Esperar;
            }

            espera -= dt;
            if (distancia > datos.DistanciaAtaque)
            {
                return DecisionRival.Acercarse;
            }
            if (espera <= 0f)
            {
                espera = ElegirEspera();
                return DecisionRival.EmpezarGolpe;
            }
            return DecisionRival.Esperar;
        }

        private float ElegirEspera()
        {
            float minima = datos.EsperaEntreAtaquesMin;
            float maxima = datos.EsperaEntreAtaquesMax;
            float t = Math.Max(0f, Math.Min(1f, azar()));
            return (minima + (maxima - minima) * t) * MultiplicadorEspera;
        }
    }
}
