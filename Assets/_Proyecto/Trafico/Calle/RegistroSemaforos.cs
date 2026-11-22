using System.Collections.Generic;
using UnityEngine;

namespace MechaCyberpunk.Trafico.Calle
{
    /// <summary>T-1630: los cruces con semáforo que hay en la escena. Un carro (la patrulla de ronda) pregunta aquí qué luz le toca y a cuántos metros está la línea de parada.</summary>
    public static class RegistroSemaforos
    {
        private static readonly List<SemaforoCruce> activos = new List<SemaforoCruce>();

        public static IReadOnlyList<SemaforoCruce> Activos => activos;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar() => activos.Clear();

        public static void Agregar(SemaforoCruce cruce)
        {
            if (cruce != null && !activos.Contains(cruce))
            {
                activos.Add(cruce);
            }
        }

        public static void Quitar(SemaforoCruce cruce) => activos.Remove(cruce);

        /// <summary>
        /// El semáforo que le toca a un carro en <paramref name="posicion"/> que va hacia <paramref name="adelante"/>: el poste más cercano que le habla. Devuelve false si no hay ninguno al frente.
        /// <paramref name="metros"/> es lo que le falta a su frente hasta la línea de parada.
        /// </summary>
        public static bool Consultar(Vector3 posicion, Vector3 adelante, out FaseCruce fase, out float metros)
        {
            fase = FaseCruce.Verde;
            metros = float.MaxValue;
            bool hay = false;
            for (int i = 0; i < activos.Count; i++)
            {
                SemaforoCruce cruce = activos[i];
                if (cruce == null)
                {
                    continue;
                }
                if (cruce.Consultar(posicion, adelante, out FaseCruce f, out float m) && m < metros)
                {
                    metros = m;
                    fase = f;
                    hay = true;
                }
            }
            return hay;
        }
    }
}
