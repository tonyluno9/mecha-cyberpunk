using System;
using MechaCyberpunk.Core;

namespace MechaCyberpunk.Trafico.Testigos
{
    /// <summary>
    /// Envuelve al <see cref="ICalor"/> real (T-661): el calor solo sube si alguien vio el delito. Lo que no ve nadie no suma; lo que ve un civil
    /// suma cuando termina de llamar. Mientras te buscan el calor no baja solo (<see cref="Mantener"/>): la policía no se va hasta que te arresten
    /// o escapes (T-665). Se registra después del real, así que el último de <c>Registro&lt;ICalor&gt;</c> es este.
    /// </summary>
    public sealed class CalorConTestigos : ICalor
    {
        private const float Tolerancia = 0.01f;
        private readonly ICalor interno;
        private readonly TestigosDelito testigos;
        private float piso;

        public CalorConTestigos(ICalor real, TestigosDelito quienVe)
        {
            interno = real;
            testigos = quienVe;
            interno.ValorCambio += ReenviarValor;
            interno.NivelCambio += ReenviarNivel;
        }

        public ICalor Interno => interno;

        /// <summary>El calor mínimo que se sostiene mientras te buscan (puntos).</summary>
        public float Piso => piso;

        public float Valor => interno.Valor;
        public float Maximo => interno.Maximo;
        public int Nivel => interno.Nivel;

        public event Action<float> ValorCambio;
        public event Action<int> NivelCambio;

        public void Soltar()
        {
            interno.ValorCambio -= ReenviarValor;
            interno.NivelCambio -= ReenviarNivel;
        }

        public void Sumar(float cantidad, MotivoCalor motivo)
        {
            if (cantidad <= 0f)
            {
                return;
            }
            if (testigos.Evaluar(cantidad, motivo) == ResultadoTestigos.Pasa)
            {
                SumarDirecto(cantidad, motivo);
            }
        }

        /// <summary>Suma sin preguntar por testigos (ya los hubo).</summary>
        public void SumarDirecto(float cantidad, MotivoCalor motivo)
        {
            interno.Sumar(cantidad, motivo);
            piso = Math.Max(piso, interno.Valor);
        }

        public void Enfriar(float cantidad)
        {
            if (cantidad <= 0f)
            {
                return;
            }
            piso = Math.Max(0f, piso - cantidad);
            interno.Enfriar(cantidad);
        }

        public void Borrar()
        {
            piso = 0f;
            interno.Borrar();
        }

        /// <summary>Si el enfriamiento natural bajó el calor por debajo del piso mientras te buscan, lo devuelve al piso.</summary>
        public void Mantener()
        {
            if (interno.Nivel > 0 && piso > interno.Valor + Tolerancia)
            {
                interno.Sumar(piso - interno.Valor, MotivoCalor.Otro);
            }
        }

        private void ReenviarValor(float v) => ValorCambio?.Invoke(v);

        private void ReenviarNivel(int n) => NivelCambio?.Invoke(n);
    }
}
