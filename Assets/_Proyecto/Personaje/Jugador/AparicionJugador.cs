using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Jugador
{
    /// <summary>
    /// Coloca al jugador en el punto de aparición (<see cref="IPuntoAparicion"/>) cuyo Id sea <c>idAparicion</c>.
    /// Lo busca en <c>Start</c> y, si todavía no existe, espera a que se registre durante unos segundos; pasado ese
    /// tiempo escribe un aviso y el jugador se queda donde está. Con el id vacío no hace nada.
    /// Aparecer dentro de un carro o un mecha todavía no existe (T-F4): se coloca a pie y se avisa una vez.
    /// </summary>
    [RequireComponent(typeof(JugadorAPie))]
    public class AparicionJugador : MonoBehaviour
    {
        [Tooltip("Id del punto de aparición donde empieza el jugador (por ejemplo Casa_Puerta). Vacío: se queda donde está.")]
        [SerializeField] private string idAparicion = string.Empty;
        [Tooltip("Segundos que espera a que el punto se registre antes de rendirse (s).")]
        [Min(0.1f)] [SerializeField] private float segundosEspera = 1f;

        private IJugador jugador;
        private bool pendiente;
        private bool cancelada;
        private float restante;

        /// <summary>Id del punto de aparición. Se puede cambiar justo después de instanciar, antes del primer frame.</summary>
        public string IdAparicion
        {
            get => idAparicion;
            set => idAparicion = value ?? string.Empty;
        }

        /// <summary>True si ya está esperando que aparezca el punto.</summary>
        public bool EstaEsperando => pendiente;

        /// <summary>No coloca al jugador (T-412: al cargar una partida se queda donde se guardó).</summary>
        public void Cancelar()
        {
            cancelada = true;
            pendiente = false;
        }

        private void Awake()
        {
            jugador = GetComponent<IJugador>();
        }

        private void OnEnable()
        {
            Registro<IPuntoAparicion>.Agregado += AlAgregarPunto;
        }

        private void OnDisable()
        {
            Registro<IPuntoAparicion>.Agregado -= AlAgregarPunto;
        }

        private void Start()
        {
            if (string.IsNullOrEmpty(idAparicion) || cancelada)
            {
                return;
            }
            pendiente = true;
            restante = segundosEspera;
            Buscar();
        }

        private void Update()
        {
            if (!pendiente)
            {
                return;
            }
            restante -= Time.deltaTime;
            if (restante <= 0f)
            {
                pendiente = false;
                Debug.LogWarning("[Personaje] No hay punto de aparición con id '" + idAparicion + "'; el jugador se queda donde está.", this);
            }
        }

        private void AlAgregarPunto(IPuntoAparicion punto)
        {
            if (pendiente && punto != null && punto.Id == idAparicion)
            {
                Colocar(punto);
            }
        }

        private void Buscar()
        {
            System.Collections.Generic.IReadOnlyList<IPuntoAparicion> puntos = Registro<IPuntoAparicion>.Todos;
            for (int i = 0; i < puntos.Count; i++)
            {
                if (puntos[i] != null && puntos[i].Id == idAparicion)
                {
                    Colocar(puntos[i]);
                    return;
                }
            }
        }

        private void Colocar(IPuntoAparicion punto)
        {
            pendiente = false;
            if (punto.Modo != ModoJugador.APie)
            {
                Debug.LogWarning("[Personaje] El punto '" + idAparicion + "' es de modo " + punto.Modo
                    + "; aparecer dentro de un montable aún no existe, se coloca a pie.", this);
            }
            jugador.Colocar(punto.Pose);
        }
    }
}
