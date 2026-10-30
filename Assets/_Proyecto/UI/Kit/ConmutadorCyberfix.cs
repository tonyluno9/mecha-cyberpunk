using System;
using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.UI.Kit
{
    /// <summary>Fila de SÍ / NO (T-408): Enter, Espacio, el clic o ← → la cambian.</summary>
    public class ConmutadorCyberfix : BotonCyberfix
    {
        public const string TextoSi = "SÍ";
        public const string TextoNo = "NO";

        [SerializeField] private bool valor;

        public bool Valor => valor;

        public event Action<bool> CambioValor;

        public void Configurar(bool nuevoValor)
        {
            valor = nuevoValor;
            Refrescar();
        }

        public override bool Cambiar(int direccion)
        {
            if (!IsInteractable() || direccion == 0)
            {
                return false;
            }
            Sonidos.Reproducir(SonidoMover);
            Voltear();
            return true;
        }

        protected override void AlAceptar() => Voltear();

        private void Voltear()
        {
            valor = !valor;
            Refrescar();
            CambioValor?.Invoke(valor);
        }

        protected override string TextoDelValor() => valor ? TextoSi : TextoNo;
    }
}
