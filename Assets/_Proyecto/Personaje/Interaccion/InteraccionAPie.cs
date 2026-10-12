using System;
using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Personaje.Movimiento;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MechaCyberpunk.Personaje.Interaccion
{
    /// <summary>
    /// Interacción del jugador a pie con las puertas: cada frame elige la puerta cerrada más cercana y de frente
    /// (buscando en Registro&lt;IPuerta&gt;, sin asumir nada de su jerarquía) y con Interactuar (E) intenta abrirla.
    /// </summary>
    [RequireComponent(typeof(JugadorAPie))]
    [RequireComponent(typeof(ControladorAPie))]
    public class InteraccionAPie : MonoBehaviour
    {
        [SerializeField] private ConfigInteraccionAPie config;
        [SerializeField] private InputActionReference interactuar;

        private JugadorAPie jugador;
        private ControladorAPie controlador;
        private readonly List<IPuerta> puertas = new List<IPuerta>();
        private readonly List<Vector3> puntos = new List<Vector3>();

        /// <summary>La puerta que E intentaría abrir ahora, o null.</summary>
        public IPuerta PuertaCandidata { get; private set; }

        /// <summary>El lugar al que da la puerta candidata, o null si no hay puerta o su lugar no está registrado.</summary>
        public ILugar LugarCandidato { get; private set; }

        /// <summary>Se dispara cada vez que el jugador intenta abrir una puerta, con la puerta y su resultado.</summary>
        public event Action<IPuerta, ResultadoPuerta> PuertaIntentada;

        private void Awake()
        {
            jugador = GetComponent<JugadorAPie>();
            controlador = GetComponent<ControladorAPie>();
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
            PuertaCandidata = null;
            LugarCandidato = null;
        }

        private void Update()
        {
            if (!controlador.ControlActivo || jugador.Camara == null)
            {
                PuertaCandidata = null;
                LugarCandidato = null;
                return;
            }

            ElegirCandidata();
            if (PuertaCandidata != null && interactuar.action.WasPressedThisFrame())
            {
                IPuerta puerta = PuertaCandidata;
                ResultadoPuerta resultado = puerta.IntentarAbrir();
                Debug.Log("[Personaje] Puerta " + puerta.Lugar + ": " + resultado);
                PuertaIntentada?.Invoke(puerta, resultado);
            }
        }

        private void ElegirCandidata()
        {
            puertas.Clear();
            puntos.Clear();
            IReadOnlyList<IPuerta> todas = Registro<IPuerta>.Todos;
            for (int i = 0; i < todas.Count; i++)
            {
                if (todas[i].EstaAbierta) continue;
                puertas.Add(todas[i]);
                puntos.Add(todas[i].Posicion);
            }

            int indice = SelectorInteraccion.Elegir(transform.position, jugador.Camara.transform.forward, puntos,
                config.Alcance, config.AnguloMaximo);
            PuertaCandidata = indice >= 0 ? puertas[indice] : null;
            LugarCandidato = PuertaCandidata != null ? BuscarLugar(PuertaCandidata.Lugar) : null;
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
