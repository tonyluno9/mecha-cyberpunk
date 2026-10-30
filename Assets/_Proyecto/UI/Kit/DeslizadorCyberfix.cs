using System;
using System.Globalization;
using MechaCyberpunk.Core;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MechaCyberpunk.UI.Kit
{
    /// <summary>Fila con un valor entre un mínimo y un máximo (T-408): ← → o la rueda lo mueven un paso. De 0 a 1 se muestra en %.</summary>
    public class DeslizadorCyberfix : BotonCyberfix, IScrollHandler
    {
        private const float Porciento = 100f;

        [SerializeField] private float minimo;
        [SerializeField] private float maximo = 1f;
        [Tooltip("Cuánto cambia con cada ← → o vuelta de rueda.")]
        [Min(0.0001f)] [SerializeField] private float paso = 0.1f;
        [SerializeField] private float valor = 1f;

        public float Valor => valor;

        public event Action<float> CambioValor;

        public void Configurar(float nuevoMinimo, float nuevoMaximo, float nuevoPaso, float nuevoValor)
        {
            minimo = nuevoMinimo;
            maximo = Mathf.Max(nuevoMinimo, nuevoMaximo);
            paso = Mathf.Max(0.0001f, nuevoPaso);
            valor = Mathf.Clamp(nuevoValor, minimo, maximo);
            Refrescar();
        }

        public override bool Cambiar(int direccion)
        {
            if (!IsInteractable() || direccion == 0)
            {
                return false;
            }
            float nuevo = Mathf.Clamp(valor + Math.Sign(direccion) * paso, minimo, maximo);
            if (Mathf.Approximately(nuevo, valor))
            {
                return false;
            }
            valor = nuevo;
            Sonidos.Reproducir(SonidoMover);
            Refrescar();
            CambioValor?.Invoke(valor);
            return true;
        }

        public void OnScroll(PointerEventData eventData) => Cambiar(Math.Sign(eventData.scrollDelta.y));

        protected override string TextoDelValor()
        {
            if (minimo >= 0f && maximo <= 1f)
            {
                return Mathf.RoundToInt(valor * Porciento) + " %";
            }
            return valor.ToString("0.##", CultureInfo.InvariantCulture);
        }
    }
}
