using System;

namespace MechaCyberpunk.IA.APie
{
    /// <summary>
    /// T-1623: la cola de voces de los pandilleros, sin Unity: nunca hablan dos a la vez. Una voz deja ocupado el canal un rato (lo que dura una frase más un hueco);
    /// lo ambiental (platicar) espera además un silencio extra para no llenar el aire; una voz con prioridad (último aviso, huir, ganamos) puede cortar a una normal.
    /// El tiempo entra como parámetro.
    /// </summary>
    public sealed class ReglaVocesPandilla
    {
        /// <summary>Lo que dura una frase (s): las voces miden de 0.8 a 1.3 s.</summary>
        public const float SegundosOcupada = 1.4f;

        /// <summary>Hueco de silencio entre dos voces (s).</summary>
        public const float SegundosHueco = 0.5f;

        /// <summary>Silencio extra que pide la plática ambiental antes de hablar (s).</summary>
        public const float SegundosExtraAmbiente = 3f;

        /// <summary>Una voz con prioridad no corta a otra con prioridad hasta pasado este tiempo (s).</summary>
        public const float SegundosEntrePrioridades = 0.6f;

        /// <summary>Mínimo y máximo (s) entre dos pláticas ambientales de un mismo pandillero.</summary>
        public const float MinimoPlatica = 15f;
        public const float MaximoPlatica = 40f;

        private float inicioUltima = float.NegativeInfinity;
        private bool ultimaPrioridad;
        private string ultimoId;

        public string UltimoId => ultimoId;

        /// <summary>Si una voz puede sonar ahora.</summary>
        public bool Permite(float ahora, bool esAmbiente, bool prioridad)
        {
            float transcurrido = ahora - inicioUltima;
            if (prioridad)
            {
                return !(ultimaPrioridad && transcurrido < SegundosEntrePrioridades);
            }
            float libre = SegundosOcupada + SegundosHueco + (esAmbiente ? SegundosExtraAmbiente : 0f);
            return transcurrido >= libre;
        }

        /// <summary>Si puede sonar, la registra y devuelve true.</summary>
        public bool Intentar(float ahora, string id, bool esAmbiente, bool prioridad)
        {
            if (!Permite(ahora, esAmbiente, prioridad))
            {
                return false;
            }
            inicioUltima = ahora;
            ultimaPrioridad = prioridad;
            ultimoId = id;
            return true;
        }

        public void Reiniciar()
        {
            inicioUltima = float.NegativeInfinity;
            ultimaPrioridad = false;
            ultimoId = null;
        }

        /// <summary>Segundos al azar hasta la siguiente plática de un pandillero (15 a 40 s); <paramref name="azar01"/> está en [0, 1).</summary>
        public static float SegundosHastaPlatica(float azar01)
        {
            float t = Math.Max(0f, Math.Min(0.9999f, azar01));
            return MinimoPlatica + t * (MaximoPlatica - MinimoPlatica);
        }
    }
}
