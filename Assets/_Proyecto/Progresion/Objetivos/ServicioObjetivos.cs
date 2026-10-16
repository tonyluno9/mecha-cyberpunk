using System;
using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.Progresion
{
    /// <summary>
    /// El objetivo de la historia en la escena: registra <see cref="IObjetivos"/>, deja fijar y quitar el objetivo y
    /// sigue al jugador para saber distancia, si se aleja y cuándo llegó (D-tony-21).
    /// </summary>
    public class ServicioObjetivos : MonoBehaviour, IObjetivos
    {
        [SerializeField] private ConfigSeguimientoObjetivo config;

        private readonly ModeloObjetivos modelo = new ModeloObjetivos();
        private SeguimientoObjetivo seguimiento;
        private bool suscrito;

        public bool HayObjetivo => modelo.HayObjetivo;
        public Objetivo Actual => modelo.Actual;

        public event Action<Objetivo> Cambio
        {
            add => modelo.Cambio += value;
            remove => modelo.Cambio -= value;
        }

        /// <summary>Seguimiento del jugador respecto al objetivo actual (eventos <c>SeAlejaCambio</c> y <c>Llegada</c>).</summary>
        public SeguimientoObjetivo Seguimiento => seguimiento;

        public float Distancia => seguimiento.Distancia;
        public bool SeAleja => seguimiento.SeAleja;
        public bool Llego => seguimiento.Llego;
        public bool PosicionResuelta => seguimiento.PosicionResuelta;
        public Vector3 Posicion => seguimiento.Posicion;

        private void Awake()
        {
            if (config == null)
            {
                Debug.LogError("[Progresion] ServicioObjetivos en " + name + " no tiene ConfigSeguimientoObjetivo.", this);
                enabled = false;
                return;
            }
            seguimiento = new SeguimientoObjetivo(config.RadioLlegada, config.MargenAlejarse, config.MargenVolver,
                config.TiempoMinimoAlejandose);
        }

        private void OnEnable()
        {
            modelo.Cambio += AlCambiarObjetivo;
            EventosLugares.JugadorEntro += AlEntrarLugar;
            suscrito = true;
            Registro<IObjetivos>.Agregar(this);
        }

        private void OnDisable()
        {
            if (!suscrito) return;
            suscrito = false;
            modelo.Cambio -= AlCambiarObjetivo;
            EventosLugares.JugadorEntro -= AlEntrarLugar;
            Registro<IObjetivos>.Quitar(this);
        }

        /// <summary>Fija el objetivo. <see cref="Objetivo.Ninguno"/> equivale a <see cref="Quitar"/>.</summary>
        public void Fijar(Objetivo objetivo) => modelo.Fijar(objetivo);

        /// <summary>Quita el objetivo.</summary>
        public void Quitar() => modelo.Quitar();

        private void AlCambiarObjetivo(Objetivo objetivo) => seguimiento.Reiniciar();

        private void AlEntrarLugar(IdLugar lugar)
        {
            if (modelo.HayObjetivo && !modelo.Actual.TienePosicion && modelo.Actual.Lugar == lugar)
            {
                seguimiento.MarcarLlegada();
            }
        }

        private void Update()
        {
            if (!modelo.HayObjetivo) return;

            if (UbicacionObjetivo.Resolver(modelo.Actual, Registro<ILugar>.Todos, out Vector3 posicion))
            {
                seguimiento.FijarPosicion(posicion);
            }
            else
            {
                seguimiento.PerderPosicion();
            }

            IJugador jugador = Registro<IJugador>.Actual;
            if (jugador == null || jugador.Raiz == null) return;
            seguimiento.Avanzar(Time.deltaTime, jugador.Raiz.position);
        }
    }
}
