using System;
using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.Mundo.Lugares
{
    /// <summary>Puerta de un local: decide con <see cref="ReglaPuerta"/> y mueve su <see cref="HojaPuerta"/>. Cumple <c>IPuerta</c>.</summary>
    public class PuertaLocal : MonoBehaviour, IPuerta
    {
        [SerializeField] private LugarMundo lugar;
        [SerializeField] private HojaPuerta hoja;

        // T-536: la puerta se abre sola al acercarte (si el lugar está abierto); también con E como antes.
        private const float RadioAutomatico = 2.6f;
        private const float SegundosEntreIntentos = 0.5f;
        private float proximoIntento;

        /// <summary>T-536: si se abre sola al acercarse el jugador.</summary>
        [Tooltip("Se abre sola cuando el jugador se acerca (si el lugar está abierto).")]
        [SerializeField] private bool abrirAlAcercarse = true;

        public IdLugar Lugar => lugar.Id;
        public bool EstaAbierta => hoja.EstaAbierta;
        public Vector3 Posicion => transform.position;

        public event Action<bool> Cambio;

        private void Awake()
        {
            if (lugar == null || hoja == null)
            {
                Debug.LogError("[Mundo] PuertaLocal en '" + name + "' necesita su lugar y su hoja.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            hoja.Cambio += AlCambiarHoja;
            lugar.EstadoCambio += AlCambiarLugar;
            Registro<IPuerta>.Agregar(this);
        }

        private void OnDisable()
        {
            hoja.Cambio -= AlCambiarHoja;
            lugar.EstadoCambio -= AlCambiarLugar;
            Registro<IPuerta>.Quitar(this);
        }

        private void Update()
        {
            if (!abrirAlAcercarse || hoja == null || hoja.EstaAbierta || Time.time < proximoIntento)
            {
                return;
            }
            proximoIntento = Time.time + SegundosEntreIntentos;
            IJugador jugador = Registro<IJugador>.Actual;
            // Solo desde la calle (adentro se sale con E, como antes).
            if (jugador == null || jugador.Raiz == null || jugador.Modo != ModoJugador.APie || lugar.JugadorDentro)
            {
                return;
            }
            Vector3 d = jugador.Raiz.position - transform.position;
            d.y = 0f;
            if (d.magnitude <= RadioAutomatico)
            {
                IntentarAbrir();
            }
        }

        public ResultadoPuerta IntentarAbrir()
        {
            ResultadoPuerta resultado = ReglaPuerta.Decidir(lugar.AbiertoPorHorario, lugar.CerradoEnDemo, lugar.JugadorDentro, hoja.EstaAbierta);
            if (resultado == ResultadoPuerta.Abierta)
            {
                hoja.Abrir();
            }
            return resultado;
        }

        public void Cerrar()
        {
            hoja.Cerrar();
        }

        private void AlCambiarHoja(bool abierta)
        {
            Cambio?.Invoke(abierta);
        }

        // Un lugar que cierra cierra su puerta, salvo que el jugador esté dentro o alguien esté en el umbral (la hoja se cierra sola al quedar libre).
        private void AlCambiarLugar(bool abierto)
        {
            if (!abierto && !lugar.JugadorDentro && hoja.OcupantesEnUmbral == 0)
            {
                hoja.Cerrar();
            }
        }
    }
}
