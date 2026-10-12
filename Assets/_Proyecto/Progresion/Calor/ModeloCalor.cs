using System;
using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.Progresion
{
    /// <summary>Lógica pura del calor policial. Se enfría solo tras <c>retrasoEnfriamiento</c> segundos sin sumar.</summary>
    public class ModeloCalor : ICalor
    {
        private readonly DatosCalor datos;
        private float tiempoSinSumar;

        public float Valor { get; private set; }
        public float Maximo => datos.Maximo;
        public int Nivel { get; private set; }

        public event Action<float> ValorCambio;
        public event Action<int> NivelCambio;

        public ModeloCalor(DatosCalor datos)
        {
            this.datos = datos ?? throw new ArgumentNullException(nameof(datos));
        }

        public void Sumar(float cantidad, MotivoCalor motivo)
        {
            if (cantidad <= 0f) return;
            tiempoSinSumar = 0f;
            Poner(Valor + cantidad);
        }

        public void Enfriar(float cantidad)
        {
            if (cantidad <= 0f) return;
            Poner(Valor - cantidad);
        }

        public void Borrar()
        {
            Poner(0f);
        }

        /// <summary>Avanza el tiempo <paramref name="dt"/> (s). Enfría solo la parte que pasa del retraso.</summary>
        public void Avanzar(float dt)
        {
            if (dt <= 0f) return;
            tiempoSinSumar += dt;
            float exceso = Mathf.Min(dt, tiempoSinSumar - datos.RetrasoEnfriamiento);
            if (exceso > 0f && Valor > 0f) Poner(Valor - datos.EnfriamientoPorSegundo * exceso);
        }

        private void Poner(float nuevo)
        {
            nuevo = Mathf.Clamp(nuevo, 0f, Maximo);
            if (nuevo == Valor) return;
            Valor = nuevo;
            ValorCambio?.Invoke(Valor);
            int nivel = CalcularNivel();
            if (nivel == Nivel) return;
            Nivel = nivel;
            NivelCambio?.Invoke(Nivel);
        }

        private int CalcularNivel()
        {
            int nivel = 0;
            var umbrales = datos.UmbralesNivel;
            for (int i = 0; i < umbrales.Length; i++)
                if (Valor >= umbrales[i]) nivel = i + 1;
            return nivel;
        }
    }
}
