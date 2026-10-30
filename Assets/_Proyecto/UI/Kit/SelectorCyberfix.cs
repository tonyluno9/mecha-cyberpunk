using System;
using MechaCyberpunk.Core;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MechaCyberpunk.UI.Kit
{
    /// <summary>Fila que elige una opción de una lista (T-408): ← → o la rueda, sin dar la vuelta. Se ve "‹ OPCIÓN ›".</summary>
    public class SelectorCyberfix : BotonCyberfix, IScrollHandler
    {
        [SerializeField] private string[] opciones = Array.Empty<string>();
        [SerializeField] private int indice;

        public int Indice => indice;
        public string Opcion => opciones.Length > 0 ? opciones[indice] : string.Empty;

        public event Action<int> CambioValor;

        public void Configurar(string[] nuevasOpciones, int nuevoIndice)
        {
            opciones = nuevasOpciones ?? Array.Empty<string>();
            indice = opciones.Length > 0 ? Mathf.Clamp(nuevoIndice, 0, opciones.Length - 1) : 0;
            Refrescar();
        }

        public override bool Cambiar(int direccion)
        {
            if (!IsInteractable() || direccion == 0 || opciones.Length == 0)
            {
                return false;
            }
            int nuevo = Mathf.Clamp(indice + Math.Sign(direccion), 0, opciones.Length - 1);
            if (nuevo == indice)
            {
                return false;
            }
            indice = nuevo;
            Sonidos.Reproducir(SonidoMover);
            Refrescar();
            CambioValor?.Invoke(indice);
            return true;
        }

        /// <summary>T-411: Enter o el clic pasan a la siguiente opción (al final vuelve a la primera).</summary>
        protected override void AlAceptar()
        {
            if (opciones.Length < 2)
            {
                return;
            }
            indice = (indice + 1) % opciones.Length;
            Refrescar();
            CambioValor?.Invoke(indice);
        }

        public void OnScroll(PointerEventData eventData) => Cambiar(Math.Sign(eventData.scrollDelta.y));

        protected override string TextoDelValor()
        {
            if (opciones.Length == 0)
            {
                return string.Empty;
            }
            return (indice > 0 ? "‹ " : "  ") + opciones[indice] + (indice < opciones.Length - 1 ? " ›" : "  ");
        }
    }
}
