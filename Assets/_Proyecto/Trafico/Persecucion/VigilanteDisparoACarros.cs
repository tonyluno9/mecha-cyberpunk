using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Trafico.Testigos;
using MechaCyberpunk.Vehiculos.Carro;
using UnityEngine;

namespace MechaCyberpunk.Trafico.Persecucion
{
    /// <summary>
    /// T-1322: si le disparas a un carro (que no sea patrulla) con la policía mirando, te ponen en búsqueda en ese momento (2 estrellas), sin esperar a que explote.
    /// Escucha los disparos (<see cref="EventosRuido.Disparo"/>) y cada <see cref="SegundosEntreRevisiones"/> s compara la vida de los carros con la de la vez anterior
    /// (<see cref="ReglaDisparoACarro"/>). La lista de carros se refresca más despacio. Lo agrega <see cref="GestorPersecucion"/>.
    /// </summary>
    public class VigilanteDisparoACarros : MonoBehaviour
    {
        private const float SegundosEntreRevisiones = 0.15f;
        private const float SegundosEntreListas = 1.5f;

        private readonly List<DanioCarro> carros = new List<DanioCarro>();
        private readonly List<float> vidas = new List<float>();
        private TestigosDelito testigos;
        private float ultimoDisparo = -100f;
        private float proximaRevision;
        private float proximaLista;

        /// <summary>Cuántas veces puso en búsqueda por un disparo a un carro (para pruebas).</summary>
        public int Reacciones { get; private set; }

        public void Configurar(TestigosDelito quienVe) => testigos = quienVe;

        private void OnEnable() => EventosRuido.Disparo += AlDisparar;

        private void OnDisable() => EventosRuido.Disparo -= AlDisparar;

        private void AlDisparar(Vector3 posicion)
        {
            ultimoDisparo = Time.time;
            proximaRevision = 0f; // revisa ya: el daño de la bala llega en el mismo cuadro o el siguiente
        }

        private void Update()
        {
            if (Time.time < proximaRevision)
            {
                return;
            }
            proximaRevision = Time.time + SegundosEntreRevisiones;
            if (Time.time >= proximaLista)
            {
                proximaLista = Time.time + SegundosEntreListas;
                Refrescar();
            }
            IJugador jugador = Registro<IJugador>.Actual;
            ICalor calor = Registro<ICalor>.Actual;
            for (int i = carros.Count - 1; i >= 0; i--)
            {
                DanioCarro carro = carros[i];
                if (carro == null)
                {
                    carros.RemoveAt(i);
                    vidas.RemoveAt(i);
                    continue;
                }
                float antes = vidas[i];
                float ahora = carro.Vida;
                vidas[i] = ahora;
                if (ahora != antes)
                {
                    Revisar(carro, antes, ahora, jugador, calor);
                }
            }
        }

        private void Refrescar()
        {
            foreach (DanioCarro c in FindObjectsByType<DanioCarro>(FindObjectsSortMode.None))
            {
                if (!carros.Contains(c))
                {
                    carros.Add(c);
                    vidas.Add(c.Vida);
                }
            }
        }

        private void Revisar(DanioCarro carro, float antes, float ahora, IJugador jugador, ICalor calor)
        {
            if (jugador == null || jugador.Raiz == null || calor == null || testigos == null || carro.GetComponent<PatrullaMundo>() != null)
            {
                return; // la patrulla tiene su propia regla (2 al dispararle, 3 al reventarla)
            }
            Vector3 posicion = jugador.Raiz.position;
            float distancia = Vector3.Distance(carro.transform.position, posicion);
            if (ReglaDisparoACarro.Cuenta(Time.time - ultimoDisparo, antes, ahora, distancia) && testigos.VeUnPolicia(posicion))
            {
                Reacciones++;
                EstrellasBusqueda.SubirA(calor, EstrellasBusqueda.EstrellasPorDispararACarroFrenteAPolicia);
            }
        }
    }
}
