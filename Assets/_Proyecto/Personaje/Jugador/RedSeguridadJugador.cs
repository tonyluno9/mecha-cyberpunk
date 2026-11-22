using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Jugador
{
    /// <summary>
    /// Red de seguridad del jugador (bug de CONTINUAR): mientras camina sobre piso válido recuerda el último punto seguro; si cae
    /// debajo del mapa (o mucho más abajo que ese punto), lo regresa ahí. La agrega <see cref="JugadorAPie"/> si el prefab no la trae.
    /// </summary>
    [RequireComponent(typeof(JugadorAPie))]
    public class RedSeguridadJugador : MonoBehaviour
    {
        [Tooltip("Cada cuánto se registra el punto seguro (s).")]
        [Min(0.05f)] [SerializeField] private float segundosEntreRegistros = 0.5f;
        [Tooltip("Si cae más que esto (m) bajo el último punto seguro, se le regresa.")]
        [Min(1f)] [SerializeField] private float caidaMaxima = 30f;
        [Tooltip("Debajo de esta altura (m, mundo) ya está fuera del mapa.")]
        [SerializeField] private float alturaMinimaMapa = -20f;

        private JugadorAPie jugador;
        private CharacterController controlador;
        private bool haySeguro;
        private Pose ultimoSeguro;
        private float proximoRegistro;

        public bool HaySeguro => haySeguro;
        public Pose UltimoSeguro => ultimoSeguro;
        public int Rescates { get; private set; }
        public float AlturaMinimaMapa => alturaMinimaMapa;

        private void Awake()
        {
            jugador = GetComponent<JugadorAPie>();
            controlador = GetComponent<CharacterController>();
        }

        // Mientras no pise nada, el punto seguro es donde lo puso la escena (el inicio de la calle frente a su casa).
        private void Start()
        {
            if (!haySeguro)
            {
                FijarSeguro(new Pose(transform.position, Quaternion.Euler(0f, transform.eulerAngles.y, 0f)));
            }
        }

        /// <summary>Fija el punto seguro (al aparecer o al cargar una partida).</summary>
        public void FijarSeguro(Pose pose)
        {
            ultimoSeguro = pose;
            haySeguro = true;
        }

        private void Update()
        {
            if (jugador.MontableActual != null)
            {
                return;
            }
            Vector3 p = transform.position;
            if (haySeguro && ReglaPosicionSegura.DebeRescatar(p.y, ultimoSeguro.position.y, caidaMaxima, alturaMinimaMapa))
            {
                Rescates++;
                Debug.LogWarning("[Personaje] El jugador cayó fuera del mapa en " + p.ToString("F1") + "; vuelve al último punto seguro " + ultimoSeguro.position.ToString("F1") + ".", this);
                jugador.Colocar(ultimoSeguro);
                return;
            }
            if (Time.time < proximoRegistro || controlador == null || !controlador.enabled || !controlador.isGrounded)
            {
                return;
            }
            proximoRegistro = Time.time + segundosEntreRegistros;
            ITerritorio territorio = Registro<ITerritorio>.Actual;
            bool dentro = territorio == null || !string.IsNullOrEmpty(territorio.ZonaEn(p));
            if (dentro && p.y >= alturaMinimaMapa)
            {
                FijarSeguro(new Pose(p, Quaternion.Euler(0f, transform.eulerAngles.y, 0f)));
            }
        }
    }
}
