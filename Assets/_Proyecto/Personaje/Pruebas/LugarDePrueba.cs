using System;
using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Pruebas
{
    /// <summary>
    /// Doble de prueba de un lugar de Alan (solo para escenas y pruebas de Tony). Se porta como el ILugar real,
    /// pero el "abierto por horario" se cambia a mano y no necesita reloj.
    /// </summary>
    public class LugarDePrueba : MonoBehaviour, ILugar
    {
        [SerializeField] private IdLugar idLugar = IdLugar.Bar;
        [SerializeField] private string nombre = "Local de prueba";
        [SerializeField] private HorarioLugar horario = new HorarioLugar(0f, 0f, true);
        [Tooltip("Cierre que no depende de la hora (cerrado en la demo).")]
        [SerializeField] private bool cerradoEnDemo;
        [Tooltip("Punto frente a la puerta, del lado de la calle.")]
        [SerializeField] private Transform entrada;

        [Tooltip("Si el horario dejaría abrir ahora. Se cambia a mano con PonerAbiertoPorHorario.")]
        [SerializeField] private bool abiertoPorHorario = true;
        private bool estabaAbierto;

        public IdLugar Id => idLugar;
        public string Nombre => nombre;
        public HorarioLugar Horario => horario;
        public bool CerradoEnDemo => cerradoEnDemo;
        public bool AbiertoPorHorario => abiertoPorHorario;
        public bool EstaAbierto => !cerradoEnDemo && abiertoPorHorario;
        public bool JugadorDentro { get; private set; }
        public Vector3 PosicionEntrada => entrada != null ? entrada.position : transform.position;

        public event Action<bool> EstadoCambio;

        private void Awake()
        {
            if (entrada == null)
            {
                Debug.LogError("[Personaje] Falta la entrada en LugarDePrueba de " + name, this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            estabaAbierto = EstaAbierto;
            Registro<ILugar>.Agregar(this);
        }

        private void OnDisable()
        {
            Registro<ILugar>.Quitar(this);
        }

        /// <summary>Define qué lugar es. Úsalo justo después de instanciar, antes de que alguien lo consulte.</summary>
        public void Configurar(IdLugar id, string nuevoNombre, HorarioLugar nuevoHorario, bool nuevoCerradoEnDemo)
        {
            idLugar = id;
            nombre = nuevoNombre;
            horario = nuevoHorario;
            cerradoEnDemo = nuevoCerradoEnDemo;
            estabaAbierto = EstaAbierto;
        }

        /// <summary>Cambia a mano si el horario dejaría abrir ahora. Avisa solo si <see cref="EstaAbierto"/> cambió de verdad.</summary>
        public void PonerAbiertoPorHorario(bool abierto)
        {
            abiertoPorHorario = abierto;
            bool ahora = EstaAbierto;
            if (ahora == estabaAbierto) return;
            estabaAbierto = ahora;
            EstadoCambio?.Invoke(ahora);
            EventosLugares.PublicarEstadoLugar(idLugar, ahora);
        }

        /// <summary>Lo llama la zona del interior.</summary>
        public void PonerJugadorDentro(bool dentro)
        {
            JugadorDentro = dentro;
        }
    }
}
