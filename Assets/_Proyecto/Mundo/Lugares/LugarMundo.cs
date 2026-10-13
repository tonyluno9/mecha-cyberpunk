using System;
using MechaCyberpunk.Core;
using MechaCyberpunk.Mundo.Datos;
using UnityEngine;

namespace MechaCyberpunk.Mundo.Lugares
{
    /// <summary>
    /// Un local del barrio. Lee el reloj del mundo, decide si está abierto y avisa solo cuando cambia.
    /// Se agrega a <c>Registro&lt;ILugar&gt;</c>. <see cref="JugadorDentro"/> lo marca la zona del interior (T-043).
    /// </summary>
    public class LugarMundo : MonoBehaviour, ILugar
    {
        [SerializeField] private DatosLugar datos;
        [Tooltip("Punto frente a la puerta, del lado de la calle.")]
        [SerializeField] private Transform entrada;

        private IRelojMundo reloj;
        private bool ultimoEstado;
        private bool estadoInicializado;
        private bool jugadorDentro;

        public IdLugar Id => datos.Id;
        public string Nombre => datos.Nombre;
        public HorarioLugar Horario => datos.Horario;
        public bool CerradoEnDemo => datos.CerradoEnDemo;
        public bool JugadorDentro => jugadorDentro;
        public Vector3 PosicionEntrada => entrada.position;

        /// <summary>True si el horario lo deja abierto a la hora actual del reloj (sin contar el cierre de la demo).</summary>
        public bool AbiertoPorHorario
        {
            get
            {
                ObtenerReloj();
                return reloj != null && datos.Horario.EstaAbierto(reloj.Hora);
            }
        }

        public bool EstaAbierto => !datos.CerradoEnDemo && AbiertoPorHorario;

        public event Action<bool> EstadoCambio;

        /// <summary>Lo llama la zona del interior (T-043) al entrar o salir el jugador.</summary>
        public void MarcarJugadorDentro(bool dentro)
        {
            jugadorDentro = dentro;
        }

        private void Awake()
        {
            if (datos == null || entrada == null || datos.Id == IdLugar.Ninguno)
            {
                Debug.LogError("[Mundo] LugarMundo en '" + name + "' necesita datos con un IdLugar y su punto de entrada.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            Registro<ILugar>.Agregar(this);
        }

        private void OnDisable()
        {
            Registro<ILugar>.Quitar(this);
        }

        private void Start()
        {
            ultimoEstado = EstaAbierto;
            estadoInicializado = reloj != null;
        }

        private void Update()
        {
            bool estado = EstaAbierto;
            if (!estadoInicializado)
            {
                estadoInicializado = reloj != null;
                ultimoEstado = estado;
                if (estadoInicializado)
                {
                    // El reloj apareció después de Start: los letreros que se pintaron sin reloj se actualizan.
                    EstadoCambio?.Invoke(estado);
                }
                return;
            }
            if (estado == ultimoEstado)
            {
                return;
            }
            ultimoEstado = estado;
            EstadoCambio?.Invoke(estado);
            EventosLugares.PublicarEstadoLugar(datos.Id, estado);
        }

        private void ObtenerReloj()
        {
            if (reloj == null)
            {
                reloj = Registro<IRelojMundo>.Actual;
            }
        }
    }
}
