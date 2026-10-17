using System;
using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.Economia;
using MechaCyberpunk.Pelea.Flujo;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Personaje.Movimiento;
using MechaCyberpunk.UI.Peleas;
using UnityEngine;

namespace MechaCyberpunk.Progresion.Prologo
{
    /// <summary>
    /// El mostrador de peleas del clandestino (T-129, D-tony-66): un <see cref="IInteractuable"/> donde el jugador a pie pulsa E para
    /// pelear la pelea casera con su mecha de la historia. Lleva dentro su <see cref="EntradaArena"/> y su flujo casero (hijo).
    /// El texto y el permiso salen, en este orden: entrada en curso o jugador no a pie, etapa de la historia, hora del reloj y estado del mecha.
    /// </summary>
    [RequireComponent(typeof(EntradaArena))]
    public class MostradorPeleas : MonoBehaviour, IInteractuable
    {
        [SerializeField] private EntradaArena entrada;
        [Tooltip("Flujo de la pelea casera (FlujoPeleaCasera, hijo de este objeto).")]
        [SerializeField] private DirectorPelea flujo;
        [Tooltip("Premio que paga el flujo: sirve para mostrar el monto en el aviso.")]
        [SerializeField] private DatosPremioPelea premio;
        [SerializeField] private string textoOfrecer = "Pelea casera · premio {0}";
        [SerializeField] private string textoSinPremio = "Pelea casera";
        [SerializeField] private string textoSinPelea = "Hoy no hay peleas caseras";
        [Tooltip("{0} = hora a la que abre el clandestino.")]
        [SerializeField] private string textoDeDia = "Peleas caseras de noche · abre {0}";
        [SerializeField] private string textoMechaNoListo = "Tu mecha no está listo · repáralo en el taller";

        [Tooltip("Opcional: pantalla de apuesta (PantallaApuesta, hija de este objeto). Sin ella, E pelea directo.")]
        [SerializeField] private PantallaApuesta pantallaApuesta;

        private bool registrado;
        private bool apuestaAbierta;
        private ControladorAPie controlCongelado;

        /// <summary>True mientras la pantalla de apuesta está abierta.</summary>
        public bool ApuestaAbierta => apuestaAbierta;

        /// <summary>Se avisa al terminar la pelea y la salida, con su resultado (para el director de la demo).</summary>
        public event Action<ResultadoPelea> PeleaTerminada;

        public Vector3 Posicion => transform.position;

        public bool PuedeUsar => !apuestaAbierta && Decidir(out _);

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
            if (entrada == null)
            {
                entrada = GetComponent<EntradaArena>();
            }
            if (entrada == null || flujo == null)
            {
                Debug.LogError("[Progresion] Faltan referencias en MostradorPeleas de " + name, this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            entrada.Terminada += AlTerminarEntrada;
            Registro<IInteractuable>.Agregar(this);
            registrado = true;
        }

        private void OnDisable()
        {
            entrada.Terminada -= AlTerminarEntrada;
            if (registrado)
            {
                Registro<IInteractuable>.Quitar(this);
                registrado = false;
            }
            if (apuestaAbierta)
            {
                CerrarApuesta(true);
                flujo.CancelarApuesta();
            }
        }

        private void AlTerminarEntrada(ResultadoPelea resultado)
        {
            PeleaTerminada?.Invoke(resultado);
        }

        public void Usar()
        {
            if (!PuedeUsar)
            {
                return;
            }
            if (!AbrirApuesta())
            {
                entrada.Empezar(flujo);
            }
        }

        // T-145: con datos de apuesta y un servicio de apuestas, E anuncia la pelea y abre la pantalla en vez de pelear directo.
        private bool AbrirApuesta()
        {
            DatosApuestaPelea datos = flujo.Apuesta;
            IApuestasPelea apuestas = Registro<IApuestasPelea>.Actual;
            if (pantallaApuesta == null || datos == null || apuestas == null || !flujo.AnunciarApuesta())
            {
                return false;
            }
            apuestaAbierta = true;
            int premioGanar = premio != null ? premio.PremioGanar : 0;
            pantallaApuesta.Mostrar(apuestas.Rival, premioGanar, apuestas.Momio, datos.Montos, datos.PiezaEnJuego,
                datos.ApuestaMinimaPorPieza, apuestas.PuedeApostar, AlPelear, AlSalirDeLaApuesta);

            JugadorAPie jugador = Registro<IJugador>.Actual as JugadorAPie;
            ControladorAPie controlador = jugador != null ? jugador.GetComponent<ControladorAPie>() : null;
            if (controlador != null && controlador.ControlActivo)
            {
                controlador.ActivarControl(false);
                controlCongelado = controlador;
            }
            return true;
        }

        private void AlPelear(int monto)
        {
            IApuestasPelea apuestas = Registro<IApuestasPelea>.Actual;
            if (monto > 0 && (apuestas == null || !apuestas.Apostar(monto)))
            {
                pantallaApuesta.MostrarAviso(pantallaApuesta.TextoNoAlcanza);
                return;
            }
            pantallaApuesta.Ocultar();
            apuestaAbierta = false;
            if (entrada.Empezar(flujo))
            {
                // El control sigue apagado: subir al mecha lo apaga y bajar lo devuelve.
                controlCongelado = null;
                return;
            }
            // No empezó (por ejemplo, sin mecha): se devuelve lo apostado y el control.
            flujo.CancelarApuesta();
            DevolverControlDespues();
        }

        private void AlSalirDeLaApuesta()
        {
            CerrarApuesta(false);
            flujo.CancelarApuesta();
        }

        // La E que cierra es la misma que abre: el control vuelve un cuadro después para que no reabra la pantalla (D-tony-67).
        private void CerrarApuesta(bool inmediato)
        {
            pantallaApuesta.Ocultar();
            apuestaAbierta = false;
            if (inmediato)
            {
                DevolverControl();
            }
            else
            {
                DevolverControlDespues();
            }
        }

        private void DevolverControlDespues()
        {
            if (isActiveAndEnabled)
            {
                StartCoroutine(EsperarYDevolverControl());
            }
            else
            {
                DevolverControl();
            }
        }

        private IEnumerator EsperarYDevolverControl()
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

        // Decide si se puede usar y qué dice el aviso. Texto vacío = no se ofrece.
        private bool Decidir(out string texto)
        {
            texto = string.Empty;
            JugadorAPie jugador = Registro<IJugador>.Actual as JugadorAPie;
            if (entrada.EnCurso || jugador == null || jugador.Modo != ModoJugador.APie)
            {
                return false;
            }

            IHistoria historia = Registro<IHistoria>.Actual;
            if (historia != null && historia.Etapa != EtapaPrologo.PeleaCasera && historia.Etapa != EtapaPrologo.Libre)
            {
                texto = textoSinPelea;
                return false;
            }

            IRelojMundo reloj = Registro<IRelojMundo>.Actual;
            if (reloj != null && !reloj.EsDeNoche)
            {
                texto = string.Format(textoDeDia, HorarioLugar.TextoHora(reloj.HoraAnochecer));
                return false;
            }

            if (!MechaListo())
            {
                texto = textoMechaNoListo;
                return false;
            }

            texto = premio != null ? string.Format(textoOfrecer, FormatoDinero.Texto(premio.PremioGanar)) : textoSinPremio;
            return true;
        }

        // La pelea casera se puede perder: con el mecha en KO (o que no se puede subir) no se ofrece hasta repararlo.
        private static bool MechaListo()
        {
            foreach (IMontable candidato in Registro<IMontable>.Todos)
            {
                if (candidato != null && candidato.Modo == ModoJugador.Mecha)
                {
                    return candidato.PuedeSubir;
                }
            }
            return false;
        }
    }
}
