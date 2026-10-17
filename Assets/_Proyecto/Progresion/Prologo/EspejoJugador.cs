using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Apariencia;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Personaje.Movimiento;
using MechaCyberpunk.UI.Historia;
using UnityEngine;

namespace MechaCyberpunk.Progresion.Prologo
{
    /// <summary>
    /// El espejo de la casa (T-130): un <see cref="IInteractuable"/> que el jugador a pie usa con E para mirarse. Abre la
    /// <see cref="PantallaEspejo"/>, prende la vista previa (que muestra la apariencia del perfil) y quita el control al jugador
    /// hasta que cierra con E o Esc; entonces apaga la vista previa y devuelve el control.
    /// </summary>
    public class EspejoJugador : MonoBehaviour, IInteractuable
    {
        [SerializeField] private PantallaEspejo pantalla;
        [SerializeField] private VistaPreviaApariencia vista;
        [SerializeField] private string textoMirarse = "Mirarte al espejo";
        [Tooltip("Alias que se muestra si no hay perfil registrado.")]
        [SerializeField] private string aliasRespaldo = "SIN ALIAS";

        private bool registrado;
        private bool abierto;
        private ControladorAPie controlCongelado;

        public Vector3 Posicion => transform.position;

        public bool PuedeUsar => Decidir();

        public string Texto => Decidir() ? textoMirarse : string.Empty;

        public bool Abierto => abierto;

        private void Awake()
        {
            if (pantalla == null || vista == null)
            {
                Debug.LogError("[Progresion] Faltan referencias en EspejoJugador de " + name, this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            pantalla.Cerrada += AlCerrarPantalla;
            Registro<IInteractuable>.Agregar(this);
            registrado = true;
        }

        private void OnDisable()
        {
            pantalla.Cerrada -= AlCerrarPantalla;
            if (registrado)
            {
                Registro<IInteractuable>.Quitar(this);
                registrado = false;
            }
            if (abierto)
            {
                Cerrar(true);
            }
        }

        public void Usar()
        {
            if (!Decidir())
            {
                return;
            }
            IPerfilJugador perfil = Registro<IPerfilJugador>.Actual;
            abierto = true;
            vista.Mostrar(true);
            pantalla.Mostrar(vista.Textura, perfil != null ? perfil.Alias : aliasRespaldo);

            JugadorAPie jugador = Registro<IJugador>.Actual as JugadorAPie;
            ControladorAPie controlador = jugador != null ? jugador.GetComponent<ControladorAPie>() : null;
            if (controlador != null && controlador.ControlActivo)
            {
                controlador.ActivarControl(false);
                controlCongelado = controlador;
            }
        }

        private void AlCerrarPantalla()
        {
            Cerrar(false);
        }

        // La E que cierra es la misma que abre: si el control volviera en este mismo cuadro, la interacción la leería otra vez y
        // reabriría el espejo. Por eso el control vuelve un cuadro después (o ya, si el objeto se está apagando).
        private void Cerrar(bool inmediato)
        {
            if (!abierto)
            {
                return;
            }
            abierto = false;
            pantalla.Ocultar();
            vista.Mostrar(false);
            if (inmediato)
            {
                DevolverControl();
            }
            else
            {
                StartCoroutine(DevolverControlDespues());
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

        private bool Decidir()
        {
            JugadorAPie jugador = Registro<IJugador>.Actual as JugadorAPie;
            return !abierto && jugador != null && jugador.Modo == ModoJugador.APie;
        }
    }
}
