using System;
using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.Economia
{
    /// <summary>
    /// El dinero del jugador en la escena (T-102): envuelve <see cref="ModeloCartera"/>, se registra como <see cref="ICartera"/>
    /// y se guarda como <see cref="IGuardable"/> (clave "cartera"). Va en ServiciosEconomia.prefab: un servicio por jugador.
    /// </summary>
    public class ServicioCartera : MonoBehaviour, ICartera, IGuardable
    {
        public const string Clave = "cartera";

        [Serializable]
        private class DatosGuardados
        {
            public int dinero;
        }

        [SerializeField] private DatosEconomia datos;

        private ModeloCartera modelo;

        public DatosEconomia Datos => datos;
        public int Dinero => modelo != null ? modelo.Dinero : 0;
        public string ClaveGuardado => Clave;

        public event Action<int, MotivoDinero> Cambio;

        private void Awake()
        {
            if (datos == null)
            {
                Debug.LogError("[Economia] ServicioCartera en " + name + " no tiene DatosEconomia.", this);
                enabled = false;
                return;
            }
            modelo = new ModeloCartera(datos.DineroInicial);
            modelo.Cambio += AlCambiar;
        }

        private void OnEnable()
        {
            if (modelo == null) return;
            Registro<ICartera>.Agregar(this);
            Registro<IGuardable>.Agregar(this);
        }

        private void OnDisable()
        {
            Registro<ICartera>.Quitar(this);
            Registro<IGuardable>.Quitar(this);
        }

        public bool PuedePagar(int cantidad) => modelo != null && modelo.PuedePagar(cantidad);

        public bool IntentarPagar(int cantidad, MotivoDinero motivo) => modelo != null && modelo.IntentarPagar(cantidad, motivo);

        public void Abonar(int cantidad, MotivoDinero motivo) => modelo?.Abonar(cantidad, motivo);

        public void CargarDeuda(int cantidad, MotivoDinero motivo) => modelo?.CargarDeuda(cantidad, motivo);

        public string Guardar() => JsonUtility.ToJson(new DatosGuardados { dinero = Dinero });

        public void Cargar(string texto)
        {
            if (modelo == null) return;
            DatosGuardados leidos = null;
            try
            {
                leidos = string.IsNullOrWhiteSpace(texto) ? null : JsonUtility.FromJson<DatosGuardados>(texto);
            }
            catch (ArgumentException)
            {
                leidos = null;
            }
            if (leidos == null)
            {
                Debug.LogWarning("[Economia] La cartera no entendió los datos guardados; no se cambia nada.");
                return;
            }
            modelo.Restablecer(leidos.dinero);
        }

        private void AlCambiar(int diferencia, MotivoDinero motivo) => Cambio?.Invoke(diferencia, motivo);
    }
}
