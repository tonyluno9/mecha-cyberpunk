using System;
using System.Collections.Generic;
using MechaCyberpunk.Trafico.Datos;
using UnityEngine;

namespace MechaCyberpunk.Trafico.Persecucion
{
    /// <summary>
    /// Una patrulla (T-341): recorre el <see cref="GrafoCalles"/> hacia el cruce más cercano al jugador, siempre sobre el eje de las calles,
    /// y se queda donde la distancia de seguimiento se lo permita. Sus luces rojas y azules alternan con <see cref="ConfigPersecucion.PeriodoSirena"/>.
    /// Sin colliders.
    /// </summary>
    public class PatrullaMundo : MonoBehaviour
    {
        private const float GradosPorSegundoGiro = 360f;
        private const float DistanciaMinimaDeNodo = 0.01f;

        [Tooltip("Luces de la sirena: la primera es roja y la segunda azul; alternan.")]
        [SerializeField] private Light luzRoja;
        [SerializeField] private Light luzAzul;

        private CalculoPersecucion calculo;
        private ConfigPersecucion config;
        private GrafoCalles calles;
        private Func<Vector3?> posicionJugador;
        private readonly List<int> camino = new List<int>();
        private int indiceEnCamino;
        private int nodoMeta = -1;
        private Vector3 posicion;

        public void Preparar(CalculoPersecucion reglas, ConfigPersecucion numeros, GrafoCalles grafo, int nodoInicial, Func<Vector3?> dondeEstaElJugador)
        {
            calculo = reglas;
            config = numeros;
            calles = grafo;
            posicionJugador = dondeEstaElJugador;
            posicion = grafo.Nodo(nodoInicial);
            camino.Clear();
            camino.Add(nodoInicial);
            indiceEnCamino = 0;
            nodoMeta = nodoInicial;
            transform.position = posicion;
        }

        private void Update()
        {
            if (calculo == null)
            {
                return;
            }
            Sirena();
            Vector3? jugador = posicionJugador?.Invoke();
            if (!jugador.HasValue)
            {
                return;
            }
            int nuevaMeta = calles.MasCercano(jugador.Value);
            if (nuevaMeta != nodoMeta)
            {
                Replanear(nuevaMeta);
            }
            Avanzar(jugador.Value, Time.deltaTime);
        }

        // La patrulla puede estar a medio tramo: el camino nuevo parte del nodo al que iba (o del último nodo si ya iba para atrás).
        private void Replanear(int meta)
        {
            int desde = camino[Mathf.Min(indiceEnCamino, camino.Count - 1)];
            List<int> nuevo = calles.Camino(desde, meta);
            if (nuevo.Count == 0)
            {
                return;
            }
            camino.Clear();
            camino.AddRange(nuevo);
            indiceEnCamino = 0;
            nodoMeta = meta;
        }

        private void Avanzar(Vector3 jugador, float dt)
        {
            float restante = calculo.Paso(false, DistanciaPlano(posicion, jugador), dt);
            while (restante > 0f && indiceEnCamino < camino.Count)
            {
                Vector3 nodo = calles.Nodo(camino[indiceEnCamino]);
                Vector3 haciaNodo = nodo - posicion;
                float distancia = haciaNodo.magnitude;
                if (distancia <= DistanciaMinimaDeNodo || distancia <= restante)
                {
                    posicion = nodo;
                    restante -= distancia;
                    indiceEnCamino++;
                    continue;
                }
                Vector3 direccion = haciaNodo / distancia;
                posicion += direccion * restante;
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(direccion, Vector3.up), GradosPorSegundoGiro * dt);
                restante = 0f;
            }
            transform.position = posicion;
        }

        private void Sirena()
        {
            bool rojaPrendida = Mathf.FloorToInt(Time.time / config.PeriodoSirena) % 2 == 0;
            if (luzRoja != null)
            {
                luzRoja.enabled = rojaPrendida;
            }
            if (luzAzul != null)
            {
                luzAzul.enabled = !rojaPrendida;
            }
        }

        private static float DistanciaPlano(Vector3 a, Vector3 b)
        {
            a.y = b.y;
            return Vector3.Distance(a, b);
        }
    }
}
