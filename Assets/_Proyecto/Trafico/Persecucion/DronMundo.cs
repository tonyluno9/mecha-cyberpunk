using System;
using MechaCyberpunk.Trafico.Datos;
using UnityEngine;

namespace MechaCyberpunk.Trafico.Persecucion
{
    /// <summary>
    /// Un dron de la policía (T-341): vuela sobre el jugador y se le acerca hasta la distancia de seguimiento, con su luz apuntando hacia abajo.
    /// Sin colliders: no estorba ni empuja nada. El daño y la captura quedan fuera de la tarea.
    /// </summary>
    public class DronMundo : MonoBehaviour
    {
        private const float GradosPorSegundoGiro = 180f;

        private CalculoPersecucion calculo;
        private ConfigPersecucion config;
        private Func<Vector3?> posicionJugador;

        public void Preparar(CalculoPersecucion reglas, ConfigPersecucion numeros, Func<Vector3?> dondeEstaElJugador)
        {
            calculo = reglas;
            config = numeros;
            posicionJugador = dondeEstaElJugador;
        }

        private void Update()
        {
            Vector3? jugador = posicionJugador?.Invoke();
            if (calculo == null || !jugador.HasValue)
            {
                return;
            }
            Vector3 meta = jugador.Value + Vector3.up * config.AlturaDron;
            Vector3 haciaMeta = meta - transform.position;
            float paso = calculo.Paso(true, haciaMeta.magnitude, Time.deltaTime);
            if (paso > 0f)
            {
                transform.position += haciaMeta.normalized * paso;
            }
            Vector3 haciaJugador = jugador.Value - transform.position;
            haciaJugador.y = 0f;
            if (haciaJugador.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(haciaJugador, Vector3.up), GradosPorSegundoGiro * Time.deltaTime);
            }
        }
    }
}
