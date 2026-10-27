using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Progresion;
using MechaCyberpunk.UI.Historia;
using MechaCyberpunk.UI.Hud;
using MechaCyberpunk.UI.Misiones;
using UnityEngine;

namespace MechaCyberpunk.UI.Clinica
{
    /// <summary>
    /// Mostrador de la clínica (T-193, D-tony-81): un <see cref="IInteractuable"/> donde el jugador a pie pulsa E para abrir la
    /// <see cref="PantallaClinica"/>. Abierto según el <see cref="ILugar"/> con <see cref="IdLugar.Clinica"/> (sin ese lugar, abierto).
    /// Al confirmar instala y cobra con <see cref="ServicioImplantes"/>, funde a negro, adelanta el reloj, el doc dice una línea
    /// y el HUD avisa. Sin <see cref="PantallaFundido"/> en la escena instala sin fundido.
    /// </summary>
    public class MostradorClinica : MonoBehaviour, IInteractuable
    {
        [SerializeField] private ConfigPantallaClinica config;
        [SerializeField] private PantallaClinica pantalla;

        private readonly CongelaControlAPie congela = new CongelaControlAPie();
        private bool registrado;
        private bool abierta;
        private bool operando;

        /// <summary>True mientras la pantalla de la clínica está abierta.</summary>
        public bool PantallaAbierta => abierta;

        /// <summary>True desde que se confirma hasta que termina la operación.</summary>
        public bool Operando => operando;

        public PantallaClinica Pantalla => pantalla;

        public Vector3 Posicion => transform.position;

        public bool PuedeUsar => !abierta && !operando && Decidir(out _);

        public string Texto
        {
            get
            {
                Decidir(out string texto);
                return texto;
            }
        }

        private void Awake()
        {
            if (config == null || pantalla == null)
            {
                Debug.LogError("[UI] Faltan referencias en MostradorClinica de " + name, this);
                enabled = false;
            }
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
            if (operando)
            {
                StopAllCoroutines();
                operando = false;
                PantallaFundido.Actual?.Fijar(0f);
                congela.Liberar();
            }
        }

        public void Usar()
        {
            if (!PuedeUsar)
            {
                return;
            }
            abierta = true;
            congela.Congelar();
            pantalla.Mostrar(Registro<ServicioImplantes>.Actual, AlInstalar, AlSalir);
        }

        private void AlSalir()
        {
            Cerrar(false);
        }

        // La E que abre no cierra, y el control vuelve un cuadro después de salir para que ninguna tecla reabra la pantalla.
        private void Cerrar(bool inmediato)
        {
            pantalla.Ocultar();
            abierta = false;
            if (!inmediato && isActiveAndEnabled)
            {
                StartCoroutine(DevolverControlDespues());
            }
            else
            {
                congela.Liberar();
            }
        }

        private IEnumerator DevolverControlDespues()
        {
            yield return null;
            congela.Liberar();
        }

        private void AlInstalar(DatosImplante implante)
        {
            ServicioImplantes servicio = Registro<ServicioImplantes>.Actual;
            ResultadoImplante resultado = servicio.Instalar(implante);
            if (resultado != ResultadoImplante.Instalado)
            {
                pantalla.MostrarAviso(config.TextoAvisoSinDinero);
                return;
            }
            pantalla.Ocultar();
            abierta = false;
            StartCoroutine(Operar(implante));
        }

        private IEnumerator Operar(DatosImplante implante)
        {
            operando = true;
            PantallaFundido fundido = PantallaFundido.Actual;
            if (fundido != null)
            {
                yield return fundido.Fundir(1f, config.SegundosFundidoNegro);
                yield return new WaitForSecondsRealtime(config.SegundosEnNegro);
            }
            IRelojMundo reloj = Registro<IRelojMundo>.Actual;
            reloj?.AvanzarHasta(reloj.Hora + config.HorasOperacion);
            if (fundido != null)
            {
                yield return fundido.Fundir(0f, config.SegundosFundidoVuelta);
            }

            JugadorAPie jugador = Registro<IJugador>.Actual as JugadorAPie;
            HudAPie hud = jugador != null ? jugador.GetComponentInChildren<HudAPie>(true) : null;
            hud?.MostrarAvisoImplante(implante.Nombre);
            PantallaSubtitulos subtitulos = FindFirstObjectByType<PantallaSubtitulos>();
            if (subtitulos != null)
            {
                subtitulos.Mostrar(config.TextoSubtitulo);
                yield return new WaitForSecondsRealtime(config.SegundosSubtitulo);
                subtitulos.Ocultar();
            }
            yield return null;
            congela.Liberar();
            operando = false;
        }

        // Decide si se puede usar y qué dice el aviso. Texto vacío = no se ofrece.
        private bool Decidir(out string texto)
        {
            texto = string.Empty;
            JugadorAPie jugador = Registro<IJugador>.Actual as JugadorAPie;
            if (jugador == null || jugador.Modo != ModoJugador.APie || Registro<ServicioImplantes>.Actual == null)
            {
                return false;
            }
            foreach (ILugar lugar in Registro<ILugar>.Todos)
            {
                if (lugar != null && lugar.Id == IdLugar.Clinica && !lugar.EstaAbierto)
                {
                    texto = config.TextoCerrado;
                    return false;
                }
            }
            texto = config.TextoMostrador;
            return true;
        }
    }
}
