using System.Collections.Generic;
using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.Progresion.Prologo.Pandillas
{
    /// <summary>
    /// Abre sola la puerta de un lugar cuando el jugador a pie se acerca (T-407): un jugador nuevo se quedaba frente a la puerta cerrada del
    /// mercado negro sin saber que era con E. La E sigue funcionando. Usa solo el contrato IPuerta.
    /// </summary>
    public class PuertaAutomatica : MonoBehaviour
    {
        [SerializeField] private IdLugar lugar = IdLugar.MercadoNegro;
        [Min(0.5f)] [Tooltip("Distancia a la puerta a la que se abre (m).")]
        [SerializeField] private float radio = 4f;
        [Min(0.05f)] [Tooltip("Cada cuántos segundos se revisa (s).")]
        [SerializeField] private float segundosRevision = 0.2f;

        private float proximaRevision;

        private void Update()
        {
            if (Time.time < proximaRevision)
            {
                return;
            }
            proximaRevision = Time.time + segundosRevision;
            IJugador jugador = Registro<IJugador>.Actual;
            if (jugador == null || jugador.Modo != ModoJugador.APie)
            {
                return;
            }
            IReadOnlyList<IPuerta> puertas = Registro<IPuerta>.Todos;
            for (int i = 0; i < puertas.Count; i++)
            {
                IPuerta p = puertas[i];
                if (p.Lugar != lugar || p.EstaAbierta)
                {
                    continue;
                }
                Vector3 d = p.Posicion - jugador.Raiz.position;
                d.y = 0f;
                if (d.sqrMagnitude <= radio * radio)
                {
                    p.IntentarAbrir();
                }
            }
        }
    }
}
