using UnityEngine;

namespace MechaCyberpunk.Personaje.Combate
{
    /// <summary>Un impacto de bala o perdigón (T-420): dónde, sobre qué cara, si pegó a una persona y si la dejó en KO.</summary>
    public readonly struct ImpactoBala
    {
        public Vector3 Punto { get; }
        public Vector3 Normal { get; }
        public bool EnPersona { get; }
        public bool Mato { get; }

        public ImpactoBala(Vector3 punto, Vector3 normal, bool enPersona, bool mato)
        {
            Punto = punto;
            Normal = normal;
            EnPersona = enPersona;
            Mato = mato;
        }
    }
}
