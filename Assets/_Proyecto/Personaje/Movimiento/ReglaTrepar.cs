using UnityEngine;

namespace MechaCyberpunk.Personaje.Movimiento
{
    /// <summary>Lógica pura de trepar: estados Libre / EnEscalera / Agarre, con la subida por escalera y el tiempo del agarre.</summary>
    public class ReglaTrepar
    {
        private float segundosAgarre;
        private float tiempoAgarre;
        private float altura;
        private float velocidad;

        public EstadoTrepar Estado { get; private set; } = EstadoTrepar.Libre;
        /// <summary>Metros subidos desde el pie de la escalera.</summary>
        public float Progreso { get; private set; }
        /// <summary>Avance del agarre de 0 a 1.</summary>
        public float ProgresoAgarre => segundosAgarre > 0f ? Mathf.Clamp01(tiempoAgarre / segundosAgarre) : 1f;

        public void IniciarEscalera(float alturaSubida, float velocidadSubida, float progresoInicial)
        {
            Estado = EstadoTrepar.EnEscalera;
            altura = alturaSubida;
            velocidad = velocidadSubida;
            Progreso = Mathf.Clamp(progresoInicial, 0f, alturaSubida);
        }

        /// <summary>Avanza la subida. Con W sube, con S baja; en el suelo con S o con salto se suelta; en la cima pide pasar al agarre.</summary>
        public AccionTrepar AvanzarEscalera(float dt, float entradaVertical, bool enPiso, bool saltar)
        {
            if (Estado != EstadoTrepar.EnEscalera)
            {
                return AccionTrepar.Nada;
            }
            if (saltar || (entradaVertical < 0f && enPiso && Progreso <= 0f))
            {
                Soltar();
                return AccionTrepar.Solto;
            }
            Progreso = Mathf.Clamp(Progreso + Mathf.Clamp(entradaVertical, -1f, 1f) * velocidad * dt, 0f, altura);
            return Progreso >= altura ? AccionTrepar.LlegoCima : AccionTrepar.Nada;
        }

        public void IniciarAgarre(float segundos)
        {
            Estado = EstadoTrepar.Agarre;
            segundosAgarre = Mathf.Max(0f, segundos);
            tiempoAgarre = 0f;
        }

        /// <summary>Avanza el agarre; devuelve true cuando termina y el estado vuelve a Libre.</summary>
        public bool AvanzarAgarre(float dt)
        {
            if (Estado != EstadoTrepar.Agarre)
            {
                return false;
            }
            tiempoAgarre += dt;
            if (tiempoAgarre >= segundosAgarre)
            {
                Soltar();
                return true;
            }
            return false;
        }

        public void Soltar()
        {
            Estado = EstadoTrepar.Libre;
            Progreso = 0f;
            tiempoAgarre = 0f;
            segundosAgarre = 0f;
        }
    }
}
