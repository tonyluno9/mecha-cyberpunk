using Unity.Cinemachine;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Movimiento
{
    /// <summary>
    /// T-1485: contrato común del temblor de cámara. Quien quiera sacudir la cámara a pie (disparo, golpe, daño) pone un
    /// <see cref="CinemachineImpulseSource"/> en su objeto y llama a <see cref="Preparar"/> una vez y a <see cref="Generar"/> cada vez.
    /// La cámara a pie (<see cref="CamaraHombro"/>) tiene un CinemachineImpulseListener en el canal <see cref="Canal"/>.
    /// </summary>
    public static class TemblorCamara
    {
        /// <summary>Canal de impulso (máscara de bits del listener y del source).</summary>
        public const int Canal = 1;

        // Amplitudes recomendadas (fuerza que se pasa a Generar; el listener le aplica su ganancia).
        public const float DisparoChico = 0.25f;
        public const float DisparoGrande = 0.6f;
        public const float Golpe = 0.8f;
        public const float Danio = 1.0f;

        /// <summary>Deja el source listo: canal común, impulso uniforme (no depende de la distancia) y forma corta.</summary>
        public static void Preparar(CinemachineImpulseSource fuente, float duracion = 0.2f, CinemachineImpulseDefinition.ImpulseShapes forma = CinemachineImpulseDefinition.ImpulseShapes.Bump)
        {
            fuente.ImpulseDefinition.ImpulseChannel = Canal;
            fuente.ImpulseDefinition.ImpulseType = CinemachineImpulseDefinition.ImpulseTypes.Uniform;
            fuente.ImpulseDefinition.ImpulseShape = forma;
            fuente.ImpulseDefinition.ImpulseDuration = duracion;
        }

        /// <summary>Lanza un temblor de la fuerza dada (ver constantes) en la dirección dada (ej. Vector3.down para un tiro, la del golpe para un impacto).</summary>
        public static void Generar(CinemachineImpulseSource fuente, float fuerza, Vector3 direccion)
        {
            if (fuente == null)
            {
                return;
            }
            fuente.GenerateImpulse(direccion.sqrMagnitude > 0.0001f ? direccion.normalized * fuerza : Vector3.up * fuerza);
        }
    }
}
