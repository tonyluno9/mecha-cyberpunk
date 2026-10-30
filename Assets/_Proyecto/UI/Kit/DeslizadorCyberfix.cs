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
        [Tooltip("Formato del valor (string.Format con el valor como {0}, p. ej. \"{0:0.00}x\"). Vacío: % de 0 a 1, o el número.")]
        [SerializeField] private string formato = string.Empty;

        public float Valor => valor;

        public event Action<float> CambioValor;

        /// <summary>T-411: cómo se escribe el valor ("{0:0.00}x"). Vacío vuelve al de siempre.</summary>
        public void FijarFormato(string nuevo)
        {
            formato = nuevo ?? string.Empty;
            Refrescar();
        }

        /// <summary>Pone el valor sin avisar (cuando lo cambia otro, no el jugador).</summary>
        public void FijarValor(float nuevo)
        {
            valor = Mathf.Clamp(nuevo, minimo, maximo);
            Refrescar();
        }

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
            Refrescar();
            // T-411: primero se aplica el valor y luego suena, para que un volumen suene ya con el nivel nuevo.
            CambioValor?.Invoke(valor);
            Sonidos.Reproducir(SonidoMover);
            return true;
        }

        public void OnScroll(PointerEventData eventData) => Cambiar(Math.Sign(eventData.scrollDelta.y));

        protected override string TextoDelValor()
        {
            if (!string.IsNullOrEmpty(formato))
            {
                return string.Format(CultureInfo.InvariantCulture, formato, valor);
            }
            if (minimo >= 0f && maximo <= 1f)
            {
                return Mathf.RoundToInt(valor * Porciento) + " %";
            }
            return valor.ToString("0.##", CultureInfo.InvariantCulture);
        }
    }
}
