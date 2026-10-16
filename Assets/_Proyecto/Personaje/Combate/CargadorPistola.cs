using System;

namespace MechaCyberpunk.Personaje.Combate
{
    /// <summary>
    /// Lógica pura del cargador de la pistola: balas, cadencia y recarga. No depende de Unity; el tiempo entra por
    /// <see cref="Avanzar"/>. La reserva de balas es infinita en esta fase.
    /// </summary>
    public class CargadorPistola
    {
        private readonly float tiempoEntreDisparos;
        private readonly float tiempoRecarga;
        private float enfriamiento;
        private float recargaRestante;

        public CargadorPistola(int tamano, float tiempoEntreDisparos, float tiempoRecarga)
        {
            if (tamano < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(tamano));
            }
            Tamano = tamano;
            Balas = tamano;
            this.tiempoEntreDisparos = Math.Max(0f, tiempoEntreDisparos);
            this.tiempoRecarga = Math.Max(0f, tiempoRecarga);
        }

        public int Tamano { get; }
        public int Balas { get; private set; }
        public bool Recargando { get; private set; }

        /// <summary>Se avisa en cada disparo con bala.</summary>
        public event Action Disparo;
        /// <summary>Se avisa cuando se intenta disparar con el cargador vacío.</summary>
        public event Action ClicVacio;
        public event Action RecargaIniciada;
        public event Action RecargaTerminada;

        /// <summary>Dispara si hay balas, no recarga y pasó el tiempo entre disparos. Con el cargador vacío avisa el clic vacío.</summary>
        public bool IntentarDisparar()
        {
            if (Recargando)
            {
                return false;
            }
            if (Balas <= 0)
            {
                ClicVacio?.Invoke();
                return false;
            }
            if (enfriamiento > 0f)
            {
                return false;
            }
            Balas--;
            enfriamiento = tiempoEntreDisparos;
            Disparo?.Invoke();
            return true;
        }

        /// <summary>Empieza la recarga. No hace nada si el cargador está lleno o ya recarga.</summary>
        public bool EmpezarRecarga()
        {
            if (Recargando || Balas >= Tamano)
            {
                return false;
            }
            Recargando = true;
            recargaRestante = tiempoRecarga;
            RecargaIniciada?.Invoke();
            return true;
        }

        public void Avanzar(float dt)
        {
            if (enfriamiento > 0f)
            {
                enfriamiento -= dt;
            }
            if (!Recargando)
            {
                return;
            }
            recargaRestante -= dt;
            if (recargaRestante <= 0f)
            {
                Recargando = false;
                Balas = Tamano;
                RecargaTerminada?.Invoke();
            }
        }
    }
}
