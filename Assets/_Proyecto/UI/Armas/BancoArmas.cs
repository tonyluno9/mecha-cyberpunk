using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Personaje.Movimiento;
using UnityEngine;

namespace MechaCyberpunk.UI.Armas
{
    /// <summary>
    /// El banco de trabajo de Tino (T-814): un <see cref="IInteractuable"/> donde el jugador a pie pulsa E para abrir la
    /// <see cref="PantallaMejoraArmas"/>. Mientras está abierta congela al jugador; el control vuelve un cuadro después de cerrar para que la
    /// misma tecla no lo reabra (D-tony-67). Sin pantalla asignada crea la suya con la config.
    /// </summary>
    public class BancoArmas : MonoBehaviour, IInteractuable
    {
        [SerializeField] private ConfigMenuArmas config;
        [Tooltip("La pantalla que abre. Vacía: el banco crea una hija con la config.")]
        [SerializeField] private PantallaMejoraArmas pantalla;
        [Tooltip("Punto frente al banco donde se para el jugador (opcional; vacío = el propio banco).")]
        [SerializeField] private Transform puntoUso;

        private ControladorAPie controlCongelado;
        private bool registrado;
        private bool abierta;

        public bool PantallaAbierta => abierta;
        public PantallaMejoraArmas Pantalla => pantalla;
        public ConfigMenuArmas Config => config;

        public Vector3 Posicion => puntoUso != null ? puntoUso.position : transform.position;

        public string Texto => config != null ? config.TextoAviso : string.Empty;

        public bool PuedeUsar
        {
            get
            {
                JugadorAPie jugador = Registro<IJugador>.Actual as JugadorAPie;
                return !abierta && config != null && jugador != null && jugador.Modo == ModoJugador.APie;
            }
        }

        /// <summary>Para el creador y las pruebas: fija la config antes de usarse.</summary>
        public void Configurar(ConfigMenuArmas configuracion, Transform punto = null)
        {
            config = configuracion;
            puntoUso = punto;
        }

        private void Awake()
        {
            if (config == null)
            {
                Debug.LogError("[UI] Falta ConfigMenuArmas en BancoArmas de " + name, this);
            }
        }

        // T-865: la pantalla (lienzo, lista y holograma) se arma al entrar a la escena y no al primer E: así abre al instante.
        private void Start()
        {
            if (config != null)
            {
                CrearPantalla();
                pantalla.Precargar();
            }
        }

        private void CrearPantalla()
        {
            if (pantalla == null)
            {
                GameObject go = new GameObject("PantallaMejoraArmas");
                go.transform.SetParent(transform, false);
                pantalla = go.AddComponent<PantallaMejoraArmas>();
            }
            pantalla.Configurar(config);
        }

        private void OnEnable()
        {
            Registro<IInteractuable>.Agregar(this);
            registrado = true;
        }

        private void OnDisable()
        {
            if (registrado)
            {
                Registro<IInteractuable>.Quitar(this);
                registrado = false;
            }
            if (abierta)
            {
                Cerrar(true);
            }
        }

        public void Usar()
        {
            if (!PuedeUsar)
            {
                return;
            }
            CrearPantalla();
            abierta = true;
            pantalla.Mostrar(AlSalir);
            JugadorAPie jugador = Registro<IJugador>.Actual as JugadorAPie;
            ControladorAPie controlador = jugador != null ? jugador.GetComponent<ControladorAPie>() : null;
            if (controlador != null && controlador.ControlActivo)
            {
                controlador.ActivarControl(false);
                controlCongelado = controlador;
            }
        }

        private void AlSalir() => Cerrar(false);

        private void Cerrar(bool inmediato)
        {
            if (pantalla != null)
            {
                pantalla.Ocultar();
            }
            abierta = false;
            if (!inmediato && isActiveAndEnabled)
            {
                StartCoroutine(DevolverControlDespues());
            }
            else
            {
                DevolverControl();
            }
        }

        private IEnumerator DevolverControlDespues()
        {
            yield return null;
            DevolverControl();
        }

        private void DevolverControl()
        {
            if (controlCongelado != null)
            {
                controlCongelado.ActivarControl(true);
                controlCongelado = null;
            }
        }
    }
}
