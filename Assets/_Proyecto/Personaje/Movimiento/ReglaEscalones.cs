using UnityEngine;

namespace MechaCyberpunk.Personaje.Movimiento
{
    /// <summary>
    /// T-1515: lógica pura para saber si el jugador sube o baja escalones de concreto (no la escalera de mano, que es TreparAPie). Un escalón es un salto de
    /// altura de <see cref="AlturaMinima"/> a <see cref="AlturaMaxima"/> entre el piso de abajo y el de adelante, con los dos pisos planos: una rampa tiene el piso
    /// inclinado y un muro pasa de la altura máxima.
    /// </summary>
    public static class ReglaEscalones
    {
        /// <summary>Desnivel (m) más chico y más grande entre el piso de aquí y el de adelante para contar como escalón (stepOffset del jugador 0.35).</summary>
        public const float AlturaMinima = 0.1f, AlturaMaxima = 0.4f;
        /// <summary>Componente vertical mínima de la normal para que un piso cuente como plano (pisada de escalón): más inclinado es rampa.</summary>
        public const float NormalPlanaMinima = 0.95f;
        /// <summary>Segundos seguidos de escalón detectado para empezar a usar los clips (una banqueta suelta no los activa), y segundos sin detectarlo para dejarlos.</summary>
        public const float SegundosParaEntrar = 0.2f, SegundosParaSalir = 0.35f;

        /// <summary>1 si el piso de adelante es un escalón más alto, -1 si es más bajo, 0 si no es escalón.</summary>
        public static int Clasificar(float alturaAqui, float alturaAdelante, float normalYAqui, float normalYAdelante)
        {
            if (normalYAqui < NormalPlanaMinima || normalYAdelante < NormalPlanaMinima)
            {
                return 0;
            }
            float d = alturaAdelante - alturaAqui;
            if (Mathf.Abs(d) < AlturaMinima || Mathf.Abs(d) > AlturaMaxima)
            {
                return 0;
            }
            return d > 0f ? 1 : -1;
        }
    }

    /// <summary>T-1515: filtra la detección cuadro a cuadro de escalones: entra tras <see cref="ReglaEscalones.SegundosParaEntrar"/> y sale tras <see cref="ReglaEscalones.SegundosParaSalir"/>.</summary>
    public class SeguidorEscalones
    {
        private int candidato;
        private float segundosCandidato;
        private float segundosSinDeteccion;

        /// <summary>0 nada, 1 subiendo, -1 bajando.</summary>
        public int Actual { get; private set; }

        public int Avanzar(int detectado, float dt)
        {
            if (detectado != 0)
            {
                segundosSinDeteccion = 0f;
                if (detectado != candidato)
                {
                    candidato = detectado;
                    segundosCandidato = 0f;
                }
                segundosCandidato += dt;
                if (segundosCandidato >= ReglaEscalones.SegundosParaEntrar)
                {
                    Actual = detectado;
                }
                return Actual;
            }
            candidato = 0;
            segundosCandidato = 0f;
            segundosSinDeteccion += dt;
            if (segundosSinDeteccion >= ReglaEscalones.SegundosParaSalir)
            {
                Actual = 0;
            }
            return Actual;
        }

        public void Reiniciar()
        {
            candidato = 0;
            segundosCandidato = 0f;
            segundosSinDeteccion = 0f;
            Actual = 0;
        }
    }
}
