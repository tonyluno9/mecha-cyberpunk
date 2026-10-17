using System;
using MechaCyberpunk.Core;
using MechaCyberpunk.Economia;
using MechaCyberpunk.Pelea.Flujo;
using MechaCyberpunk.Personaje.Jugador;
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

        private bool registrado;

        /// <summary>Se avisa al terminar la pelea y la salida, con su resultado (para el director de la demo).</summary>
        public event Action<ResultadoPelea> PeleaTerminada;

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
        }

        private void AlTerminarEntrada(ResultadoPelea resultado)
        {
            PeleaTerminada?.Invoke(resultado);
        }

        public void Usar()
        {
            if (PuedeUsar)
            {
                entrada.Empezar(flujo);
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
