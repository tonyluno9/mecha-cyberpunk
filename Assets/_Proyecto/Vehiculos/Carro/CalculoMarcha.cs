using System;

namespace MechaCyberpunk.Vehiculos.Carro
{
    /// <summary>
    /// T-658: suaviza el cambio de adelante a atrás. Con el carro en marcha, la tecla contraria solo frena; cuando llega a parar espera un
    /// momento y luego el motor sube poco a poco hasta su fuerza (sin tirones). Arrancar en reversa desde parado también sube suave.
    /// Lógica pura con estado (el tiempo entra como parámetro).
    /// </summary>
    public sealed class CalculoMarcha
    {
        private bool cambiando;
        private float espera;
        private float rampa;

        /// <summary>Lo que falta de la espera de cambio de sentido (s); 0 si no hay cambio en curso.</summary>
        public bool EnCambio => cambiando;

        /// <summary>Vuelve al estado de reposo (al soltar el control del carro).</summary>
        public void Reiniciar()
        {
            cambiando = false;
            espera = 0f;
            rampa = 0f;
        }

        /// <summary>
        /// Factor (0 a 1) por el que se multiplica la fuerza del motor. 0 mientras frena contra el movimiento o espera parado,
        /// sube suave después, y vale 1 cuando el carro ya va en el sentido que se pide.
        /// </summary>
        public float Factor(in ParametrosManejo p, float acelerar, float frenar, float velocidadAdelante, float dt)
        {
            int sentido = acelerar > 0f ? 1 : (frenar > 0f ? -1 : 0);
            if (sentido == 0)
            {
                Reiniciar();
                return 1f;
            }
            bool contra = (sentido > 0 && velocidadAdelante < -p.UmbralReversa) || (sentido < 0 && velocidadAdelante > p.UmbralReversa);
            if (contra)
            {
                cambiando = true;
                espera = 0f;
                rampa = 0f;
                return 0f;
            }
            bool yaVaEnElSentido = (sentido > 0 && velocidadAdelante > p.UmbralReversa) || (sentido < 0 && velocidadAdelante < -p.UmbralReversa);
            if (yaVaEnElSentido)
            {
                cambiando = false;
                rampa = 1f;
                return 1f;
            }
            if (cambiando)
            {
                espera += dt;
                if (espera < p.EsperaCambioSentido)
                {
                    return 0f;
                }
            }
            else if (sentido > 0)
            {
                return 1f;
            }
            rampa = p.RampaMarcha > 0f ? Math.Min(1f, rampa + dt / p.RampaMarcha) : 1f;
            if (rampa >= 1f)
            {
                cambiando = false;
            }
            return rampa * rampa * (3f - 2f * rampa);
        }
    }
}
