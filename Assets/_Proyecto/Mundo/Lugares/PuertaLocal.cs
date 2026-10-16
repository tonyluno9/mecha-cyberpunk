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
