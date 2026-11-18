using System;
using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.Mundo.Lugares
{
    /// <summary>
    /// T-890: puerta que se abre con E a cualquier hora (el consultorio del Doc: antes no tenía puerta y se entraba siempre; lo que cierra de
    /// noche es el mostrador, no el cuarto). Cumple <c>IPuerta</c> y mueve su <see cref="HojaPuerta"/>.
    /// </summary>
    public class PuertaSinHorario : MonoBehaviour, IPuerta
    {
        [SerializeField] private IdLugar lugar = IdLugar.Clinica;
        [SerializeField] private HojaPuerta hoja;
        [Tooltip("T-1330: siempre abierta; se cierra mientras el jugador está dentro del consultorio y vuelve a abrirse al salir.")]
        [SerializeField] private bool abiertaSalvoDentro = true;

        private bool jugadorDentro;

        public IdLugar Lugar => lugar;
        public bool EstaAbierta => hoja.EstaAbierta;
        public Vector3 Posicion => transform.position;

        public event Action<bool> Cambio;

        private void Awake()
        {
            if (hoja == null)
            {
                Debug.LogError("[Mundo] PuertaSinHorario en '" + name + "' necesita su hoja.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            if (hoja != null)
            {
                hoja.Cambio += AlCambiarHoja;
            }
            Registro<IPuerta>.Agregar(this);
            EventosLugares.JugadorEntro += AlEntrar;
            EventosLugares.JugadorSalio += AlSalir;
        }

        private void Start()
        {
            AplicarEstado();
        }

        private void OnDisable()
        {
            if (hoja != null)
            {
                hoja.Cambio -= AlCambiarHoja;
            }
            Registro<IPuerta>.Quitar(this);
            EventosLugares.JugadorEntro -= AlEntrar;
            EventosLugares.JugadorSalio -= AlSalir;
        }

        /// <summary>T-1330: true si el jugador está dentro del consultorio (la puerta está cerrada para él).</summary>
        public bool JugadorDentro => jugadorDentro;

        private void AlEntrar(IdLugar id)
        {
            if (id == lugar)
            {
                jugadorDentro = true;
                AplicarEstado();
            }
        }

        private void AlSalir(IdLugar id)
        {
            if (id == lugar)
            {
                jugadorDentro = false;
                AplicarEstado();
            }
        }

        // Fuera: abierta y sin cerrarse sola. Dentro: se cierra (con E se abre para salir y se cierra sola después).
        private void AplicarEstado()
        {
            if (!abiertaSalvoDentro || hoja == null)
            {
                return;
            }
            hoja.MantenerAbierta = !jugadorDentro;
            if (jugadorDentro)
            {
                hoja.Cerrar();
            }
            else
            {
                hoja.Abrir();
            }
        }

        public ResultadoPuerta IntentarAbrir()
        {
            if (hoja.EstaAbierta)
            {
                return ResultadoPuerta.YaAbierta;
            }
            hoja.Abrir();
            return ResultadoPuerta.Abierta;
        }

        public void Cerrar()
        {
            hoja.Cerrar();
        }

        private void AlCambiarHoja(bool abierta)
        {
            Cambio?.Invoke(abierta);
        }
    }
}
