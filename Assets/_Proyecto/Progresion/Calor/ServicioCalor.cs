using System;
using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.Progresion
{
    /// <summary>
    /// El calor policial del jugador en la escena (T-067): envuelve <see cref="ModeloCalor"/>, lo enfría con el tiempo de juego
    /// y se registra como <see cref="ICalor"/>. Va en ServiciosProgresion.prefab: un servicio por jugador.
    /// </summary>
    public class ServicioCalor : MonoBehaviour, ICalor
    {
        [SerializeField] private DatosCalor datos;

        private ModeloCalor modelo;

        public float Valor => modelo != null ? modelo.Valor : 0f;
        public float Maximo => datos != null ? datos.Maximo : 0f;
        public int Nivel => modelo != null ? modelo.Nivel : 0;

        public event Action<float> ValorCambio;
        public event Action<int> NivelCambio;

        private void Awake()
        {
            if (datos == null)
            {
                Debug.LogError("[Progresion] ServicioCalor en " + name + " no tiene DatosCalor.", this);
                enabled = false;
                return;
            }
            modelo = new ModeloCalor(datos);
            modelo.ValorCambio += AlCambiarValor;
            modelo.NivelCambio += AlCambiarNivel;
        }

        private void OnEnable()
        {
            if (modelo != null) Registro<ICalor>.Agregar(this);
        }

        private void OnDisable()
        {
            Registro<ICalor>.Quitar(this);
        }

        private void Update()
        {
            modelo.Avanzar(Time.deltaTime);
        }

        /// <summary>Suma calor. Con corporaciones en nivel -1 o -2 (reputación registrada) la cantidad se multiplica (T-182).</summary>
        public void Sumar(float cantidad, MotivoCalor motivo)
        {
            if (modelo == null) return;
            IReputacion reputacion = Registro<IReputacion>.Actual;
            if (reputacion != null)
            {
                DatosReputacion d = reputacion is ServicioReputacion servicio ? servicio.Datos : null;
                cantidad *= ReglaReputacion.MultiplicadorCalor(d, reputacion.Nivel(TipoReputacion.Corporaciones));
            }
            modelo.Sumar(cantidad, motivo);
        }

        public void Enfriar(float cantidad) => modelo?.Enfriar(cantidad);

        public void Borrar() => modelo?.Borrar();

        private void AlCambiarValor(float valor) => ValorCambio?.Invoke(valor);

        private void AlCambiarNivel(int nivel)
        {
            if (nivel > nivelAnterior)
            {
                Sonidos.Reproducir("CAL_SUBE_NIVEL");
            }
            else if (nivel < nivelAnterior)
            {
                Sonidos.Reproducir("CAL_BAJA_NIVEL");
            }
            nivelAnterior = nivel;
            NivelCambio?.Invoke(nivel);
        }

        private int nivelAnterior;
    }
}
