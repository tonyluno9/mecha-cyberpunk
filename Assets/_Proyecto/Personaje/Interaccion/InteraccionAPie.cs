using System;
using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Personaje.Movimiento;
using MechaCyberpunk.Personaje.Vida;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MechaCyberpunk.Personaje.Interaccion
{
    /// <summary>
    /// Interacción del jugador con la tecla E: a pie elige la puerta cerrada o el montable libre más cercano y de frente
    /// (buscando en Registro&lt;IPuerta&gt; y Registro&lt;IMontable&gt;, sin asumir nada de su jerarquía) y lo usa (también los IInteractuable de Registro&lt;IInteractuable&gt;); dentro de un
    /// montable, la E lo baja. Este componente sigue prendido en carro y mecha porque es el que lee la E.
    /// </summary>
    [RequireComponent(typeof(JugadorAPie))]
    [RequireComponent(typeof(ControladorAPie))]
    public class InteraccionAPie : MonoBehaviour
    {
        [SerializeField] private ConfigInteraccionAPie config;
        [SerializeField] private InputActionReference interactuar;

        private JugadorAPie jugador;
        private ControladorAPie controlador;
        private VidaAPie vida;
        private readonly List<IPuerta> puertas = new List<IPuerta>();
        private readonly List<IMontable> montables = new List<IMontable>();
        private readonly List<IInteractuable> interactuables = new List<IInteractuable>();
        private readonly List<Vector3> puntos = new List<Vector3>();

        /// <summary>La puerta que E intentaría abrir ahora, o null.</summary>
        public IPuerta PuertaCandidata { get; private set; }

        /// <summary>El lugar al que da la puerta candidata, o null si no hay puerta o su lugar no está registrado.</summary>
        public ILugar LugarCandidato { get; private set; }

        /// <summary>El montable al que E subiría ahora, o null.</summary>
        public IMontable MontableCandidato { get; private set; }

        /// <summary>El interactuable que E usaría ahora (aunque no se pueda usar), o null.</summary>
        public IInteractuable InteractuableCandidato { get; private set; }

        /// <summary>Qué hay enfrente: una puerta, un montable, un interactuable o nada.</summary>
        public TipoCandidatoAPie TipoCandidato { get; private set; }

        /// <summary>Se dispara cada vez que el jugador intenta abrir una puerta, con la puerta y su resultado.</summary>
        public event Action<IPuerta, ResultadoPuerta> PuertaIntentada;

        /// <summary>Se dispara una vez por pulsación de E, después de llamar a Usar() del interactuable.</summary>
        public event Action<IInteractuable> InteractuableUsado;

        /// <summary>Se dispara cuando el jugador pulsa E frente a un interactuable que no se puede usar.</summary>
        public event Action<IInteractuable> InteractuableNegado;

        /// <summary>Se dispara cuando el jugador pulsa E dentro de un montable que todavía no puede dejar.</summary>
        public event Action BajarNegado;

        private void Awake()
        {
            jugador = GetComponent<JugadorAPie>();
            controlador = GetComponent<ControladorAPie>();
            vida = GetComponent<VidaAPie>();
            if (config == null || interactuar == null)
            {
                Debug.LogError("[Personaje] Faltan referencias en InteraccionAPie de " + name, this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            interactuar.action.Enable();
        }

        private void OnDisable()
        {
            interactuar.action.Disable();
            LimpiarCandidatos();
        }

        private void Update()
        {
            LimpiarCandidatos();
            bool dentro = jugador.Modo != ModoJugador.APie;
            if (!dentro)
            {
                if (!controlador.ControlActivo || jugador.Camara == null)
                {
                    return;
                }
                ElegirCandidato();
            }

            if (!interactuar.action.WasPressedThisFrame())
            {
                return;
            }

            IMontable montable = dentro ? jugador.MontableActual : MontableCandidato;
            bool estaKO = vida != null && vida.EstaKO;
            AccionMontar accion = ReglaMontar.Decidir(jugador.Modo, estaKO, TipoCandidato,
                montable != null && montable.PuedeSubir, montable != null && montable.PuedeBajar,
                InteractuableCandidato != null && InteractuableCandidato.PuedeUsar);
            switch (accion)
            {
                case AccionMontar.AbrirPuerta:
                    IPuerta puerta = PuertaCandidata;
                    ResultadoPuerta resultado = puerta.IntentarAbrir();
                    Debug.Log("[Personaje] Puerta " + puerta.Lugar + ": " + resultado);
                    PuertaIntentada?.Invoke(puerta, resultado);
                    break;
                case AccionMontar.Subir:
                    jugador.Subir(montable);
                    LimpiarCandidatos();
                    break;
                case AccionMontar.Bajar:
                    jugador.Bajar();
                    break;
                case AccionMontar.NoPuedeBajar:
                    BajarNegado?.Invoke();
                    break;
                case AccionMontar.Usar:
                    IInteractuable usado = InteractuableCandidato;
                    usado.Usar();
                    InteractuableUsado?.Invoke(usado);
                    break;
                case AccionMontar.UsarNegado:
                    InteractuableNegado?.Invoke(InteractuableCandidato);
                    break;
            }
        }

        private void LimpiarCandidatos()
        {
            PuertaCandidata = null;
            LugarCandidato = null;
            MontableCandidato = null;
            InteractuableCandidato = null;
            TipoCandidato = TipoCandidatoAPie.Ninguno;
        }

        // Puertas y montables compiten en la misma lista de puntos: gana el más cercano y de frente.
        private void ElegirCandidato()
        {
            puertas.Clear();
            montables.Clear();
            interactuables.Clear();
            puntos.Clear();
            IReadOnlyList<IPuerta> todasPuertas = Registro<IPuerta>.Todos;
            for (int i = 0; i < todasPuertas.Count; i++)
            {
                if (todasPuertas[i].EstaAbierta) continue;
                puertas.Add(todasPuertas[i]);
                montables.Add(null);
                interactuables.Add(null);
                puntos.Add(todasPuertas[i].Posicion);
            }
            IReadOnlyList<IMontable> todosMontables = Registro<IMontable>.Todos;
            for (int i = 0; i < todosMontables.Count; i++)
            {
                if (!todosMontables[i].PuedeSubir) continue;
                puertas.Add(null);
                montables.Add(todosMontables[i]);
                interactuables.Add(null);
                puntos.Add(todosMontables[i].PuntoSubida);
            }
            IReadOnlyList<IInteractuable> todosInteractuables = Registro<IInteractuable>.Todos;
            for (int i = 0; i < todosInteractuables.Count; i++)
            {
                puertas.Add(null);
                montables.Add(null);
                interactuables.Add(todosInteractuables[i]);
                puntos.Add(todosInteractuables[i].Posicion);
            }

            int indice = SelectorInteraccion.Elegir(transform.position, jugador.Camara.transform.forward, puntos,
                config.Alcance, config.AnguloMaximo);
            if (indice < 0)
            {
                return;
            }
            PuertaCandidata = puertas[indice];
            MontableCandidato = montables[indice];
            InteractuableCandidato = interactuables[indice];
            if (PuertaCandidata != null)
            {
                TipoCandidato = TipoCandidatoAPie.Puerta;
                LugarCandidato = BuscarLugar(PuertaCandidata.Lugar);
            }
            else if (InteractuableCandidato != null)
            {
                TipoCandidato = TipoCandidatoAPie.Interactuable;
            }
            else
            {
                TipoCandidato = TipoCandidatoAPie.Montable;
            }
        }

        private static ILugar BuscarLugar(IdLugar id)
        {
            IReadOnlyList<ILugar> lugares = Registro<ILugar>.Todos;
            for (int i = 0; i < lugares.Count; i++)
            {
                if (lugares[i].Id == id) return lugares[i];
            }
            return null;
        }
    }
}
