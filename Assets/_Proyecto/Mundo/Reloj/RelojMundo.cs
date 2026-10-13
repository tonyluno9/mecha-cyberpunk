using System;
using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Mundo.Datos;
using UnityEngine;

namespace MechaCyberpunk.Mundo.Reloj
{
    /// <summary>
    /// El reloj del mundo de la escena: avanza con el tiempo real según <see cref="ConfigRelojMundo"/> y se publica
    /// como <see cref="IRelojMundo"/> en <c>Registro&lt;IRelojMundo&gt;</c>. Uno por escena.
    /// </summary>
    public class RelojMundo : MonoBehaviour, IRelojMundo
    {
        [SerializeField] private ConfigRelojMundo config;

        [Tooltip("Multiplicador del tiempo del reloj (1 = normal). Las herramientas de prueba lo suben.")]
        [SerializeField, Min(0f)] private float escalaTiempo = 1f;

        private readonly List<EventoReloj> eventos = new List<EventoReloj>();
        private CalculoReloj calculo;

        public float Hora => calculo.Hora;
        public int Dia => calculo.Dia;
        public FranjaDia Franja => calculo.EsDeNoche ? FranjaDia.Noche : FranjaDia.Dia;
        public bool EsDeNoche => calculo.EsDeNoche;
        public float HoraAmanecer => calculo.HoraAmanecer;
        public float HoraAnochecer => calculo.HoraAnochecer;
        public bool Pausado => calculo.Pausado;

        /// <summary>Multiplicador del tiempo del reloj (1 = normal).</summary>
        public float EscalaTiempo
        {
            get => escalaTiempo;
            set => escalaTiempo = Mathf.Max(0f, value);
        }

        public event Action<int> HoraEnteraCambio;
        public event Action<FranjaDia> FranjaCambio;
        public event Action<int> DiaCambio;

        private void Awake()
        {
            if (config == null)
            {
                Debug.LogError("[Mundo] RelojMundo en '" + name + "' necesita su ConfigRelojMundo.", this);
                enabled = false;
                return;
            }
            calculo = new CalculoReloj(config.HoraAmanecer, config.HoraAnochecer, config.MinutosRealesDia,
                config.MinutosRealesNoche, config.HoraInicial, config.DiaInicial);
        }

        private void OnEnable()
        {
            if (calculo != null)
            {
                Registro<IRelojMundo>.Agregar(this);
            }
        }

        private void OnDisable()
        {
            Registro<IRelojMundo>.Quitar(this);
        }

        private void Update()
        {
            eventos.Clear();
            calculo.Avanzar(Time.deltaTime * escalaTiempo, eventos);
            Publicar();
        }

        public void Pausar(bool pausado) => calculo.Pausar(pausado);

        public void AvanzarHasta(float hora)
        {
            eventos.Clear();
            calculo.AvanzarHasta(hora, eventos);
            Publicar();
        }

        private void Publicar()
        {
            for (int i = 0; i < eventos.Count; i++)
            {
                EventoReloj evento = eventos[i];
                switch (evento.Tipo)
                {
                    case TipoEventoReloj.HoraEntera:
                        HoraEnteraCambio?.Invoke(evento.Valor);
                        break;
                    case TipoEventoReloj.Franja:
                        FranjaCambio?.Invoke(evento.Valor == 1 ? FranjaDia.Noche : FranjaDia.Dia);
                        break;
                    case TipoEventoReloj.Dia:
                        DiaCambio?.Invoke(evento.Valor);
                        break;
                }
            }
            eventos.Clear();
        }
    }
}
