using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Personaje.Movimiento;
using MechaCyberpunk.UI.Historia;
using UnityEngine;

namespace MechaCyberpunk.Progresion.Prologo
{
    /// <summary>
    /// La cama de la casa (T-130): un <see cref="IInteractuable"/> que el jugador a pie usa con E. Dormir hace un fundido, adelanta el
    /// reloj a la hora de despertar, guarda la partida y regresa. En la etapa <see cref="EtapaPrologo.Dormir"/> lleva al flashback.
    /// Antes de llegar a casa (PeleaTutorial a RegresoACasa) y en la pelea casera avisa por qué no se puede.
    /// </summary>
    public class CamaJugador : MonoBehaviour, IInteractuable
    {
        [Tooltip("Rutas de las escenas. Demo por defecto; Pruebas_Prologo usa DatosEscenas_Pruebas.")]
        [SerializeField] private DatosEscenas datosEscenas;
        [Tooltip("PantallasHistoria.prefab: se instancia una vez si la escena no trae pantalla de fundido.")]
        [SerializeField] private GameObject prefabPantallas;
        [Tooltip("Hora a la que se despierta (h).")]
        [Range(0f, 23.99f)] [SerializeField] private float horaDespertar = 8f;
        [Tooltip("Tiempo que se queda en negro antes de volver (s).")]
        [Min(0f)] [SerializeField] private float segundosEnNegro = 0.8f;
        [Tooltip("{0} = hora a la que se despierta.")]
        [SerializeField] private string textoDormir = "Dormir · hasta las {0}";
        [SerializeField] private string textoDormirHistoria = "Dormir";
        [SerializeField] private string textoAhoraNo = "Ahora no puedes dormir";
        [SerializeField] private string textoPrimeroCasera = "Primero tu pelea casera";

        private bool registrado;
        private bool durmiendo;
        private ControladorAPie controlCongelado;

        public Vector3 Posicion => transform.position;

        public bool PuedeUsar => Decidir(out _);

        public string Texto
        {
            get
            {
                Decidir(out string texto);
                return texto;
            }
        }

        /// <summary>True desde que se acepta dormir hasta que termina el fundido de vuelta.</summary>
        public bool Durmiendo => durmiendo;

        private void Awake()
        {
            if (datosEscenas == null)
            {
                Debug.LogError("[Progresion] Falta DatosEscenas en CamaJugador de " + name, this);
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
            if (durmiendo)
            {
                StopAllCoroutines();
                durmiendo = false;
                SoltarControl();
            }
        }

        public void Usar()
        {
            if (!PuedeUsar)
            {
                return;
            }
            IHistoria historia = Registro<IHistoria>.Actual;
            durmiendo = true;
            if (historia != null && historia.Etapa == EtapaPrologo.Dormir)
            {
                StartCoroutine(DormirAlFlashback());
            }
            else
            {
                StartCoroutine(DormirHastaLaManana());
            }
        }

        // Decide si se puede usar y qué dice el aviso. Texto vacío = no se ofrece.
        private bool Decidir(out string texto)
        {
            texto = string.Empty;
            JugadorAPie jugador = Registro<IJugador>.Actual as JugadorAPie;
            if (durmiendo || jugador == null || jugador.Modo != ModoJugador.APie)
            {
                return false;
            }

            IHistoria historia = Registro<IHistoria>.Actual;
            EtapaPrologo etapa = historia != null ? historia.Etapa : EtapaPrologo.Libre;
            if (etapa == EtapaPrologo.Dormir)
            {
                texto = textoDormirHistoria;
                return true;
            }
            if (etapa == EtapaPrologo.PeleaCasera)
            {
                texto = textoPrimeroCasera;
                return false;
            }
            if (etapa >= EtapaPrologo.PeleaTutorial && etapa <= EtapaPrologo.RegresoACasa)
            {
                texto = textoAhoraNo;
                return false;
            }
            texto = string.Format(textoDormir, HorarioLugar.TextoHora(horaDespertar));
            return true;
        }

        private IEnumerator DormirAlFlashback()
        {
            PantallaFundido fundido = ObtenerFundido();
            CongelarControl();
            if (fundido != null)
            {
                yield return fundido.Fundir(1f, SegundosSalida(fundido));
            }
            ServicioHistoria historia = Registro<IHistoria>.Actual as ServicioHistoria;
            if (historia == null || !historia.Avanzar(EtapaPrologo.Flashback))
            {
                Debug.LogError("[Progresion] CamaJugador no pudo avanzar la historia al flashback.", this);
            }
            // Ir guarda la partida y carga la escena; el negro se queda hasta que la escena nueva reemplace esta.
            CambioEscena.Ir(datosEscenas.RutaPara(EtapaPrologo.Flashback));
            durmiendo = false;
        }

        private IEnumerator DormirHastaLaManana()
        {
            PantallaFundido fundido = ObtenerFundido();
            CongelarControl();
            if (fundido != null)
            {
                yield return fundido.Fundir(1f, SegundosSalida(fundido));
            }
            IRelojMundo reloj = Registro<IRelojMundo>.Actual;
            if (reloj != null)
            {
                reloj.AvanzarHasta(horaDespertar);
            }
            // T-448: con las mejoras de la cama, despiertas curado y con menos calor.
            AplicarMejorasDeLaCama();
            // T-412: dormir escribe el AUTOGUARDADO (y sale "GUARDADO").
            MechaCyberpunk.Progresion.Partida.Autoguardado.Escribir();
            yield return new WaitForSecondsRealtime(segundosEnNegro);
            if (fundido != null)
            {
                float segundosEntrada = fundido.Config != null ? fundido.Config.SegundosEntrada : 0f;
                yield return fundido.Fundir(0f, segundosEntrada);
            }
            SoltarControl();
            durmiendo = false;
        }

        // T-448: las mejoras de la cama de la guarida: cura la vida y borra una parte (o todo) del calor policial.
        private static void AplicarMejorasDeLaCama()
        {
            IGuarida guarida = Registro<IGuarida>.Actual;
            if (guarida == null)
            {
                return;
            }
            IJugador jugador = Registro<IJugador>.Actual;
            if (guarida.CamaCura && jugador != null && jugador.Raiz != null)
            {
                jugador.Raiz.GetComponentInChildren<MechaCyberpunk.Personaje.Vida.VidaAPie>()?.CurarTodo();
            }
            ICalor calor = Registro<ICalor>.Actual;
            if (calor != null && guarida.CamaBajaCalor > 0f)
            {
                if (guarida.CamaBajaCalor >= 1f)
                {
                    calor.Borrar();
                }
                else
                {
                    calor.Enfriar(calor.Valor * guarida.CamaBajaCalor);
                }
            }
        }

        private static float SegundosSalida(PantallaFundido fundido)
        {
            return fundido.Config != null ? fundido.Config.SegundosSalida : 0f;
        }

        private PantallaFundido ObtenerFundido()
        {
            if (PantallaFundido.Actual != null)
            {
                return PantallaFundido.Actual;
            }
            if (prefabPantallas == null)
            {
                Debug.LogWarning("[Progresion] CamaJugador sin pantalla de fundido: duerme sin fundido.", this);
                return null;
            }
            GameObject nuevas = Instantiate(prefabPantallas);
            nuevas.name = prefabPantallas.name;
            return nuevas.GetComponentInChildren<PantallaFundido>(true);
        }

        // Mientras duerme el jugador no camina ni mira. Solo se suelta si este componente fue quien lo congeló.
        private void CongelarControl()
        {
            JugadorAPie jugador = Registro<IJugador>.Actual as JugadorAPie;
            ControladorAPie controlador = jugador != null ? jugador.GetComponent<ControladorAPie>() : null;
            if (controlador != null && controlador.ControlActivo)
            {
                controlador.ActivarControl(false);
                controlCongelado = controlador;
            }
        }

        private void SoltarControl()
        {
            if (controlCongelado != null)
            {
                controlCongelado.ActivarControl(true);
                controlCongelado = null;
            }
        }
    }
}
