using System;
using MechaCyberpunk.Core;
using MechaCyberpunk.Trafico.Persecucion;
using NUnit.Framework;

namespace MechaCyberpunk.Tests.Trafico
{
    /// <summary>T-1303: pegarle a la policía te pone en la primera estrella de inmediato; si ya te buscan no suma más.</summary>
    public class PrimeraEstrellaT1303Pruebas
    {
        private sealed class CalorDePrueba : ICalor
        {
            public float Valor { get; private set; }
            public float Maximo => 100f;
            public int Nivel => Valor >= 30f ? 1 : 0;
            public MotivoCalor UltimoMotivo;
            public event Action<float> ValorCambio;
            public event Action<int> NivelCambio;
            public void Sumar(float cantidad, MotivoCalor motivo)
            {
                UltimoMotivo = motivo;
                Valor = Math.Min(Maximo, Valor + cantidad);
                ValorCambio?.Invoke(Valor);
                NivelCambio?.Invoke(Nivel);
            }
            public void Enfriar(float cantidad) => Valor = Math.Max(0f, Valor - cantidad);
            public void Borrar() => Valor = 0f;
        }

        [Test]
        public void SinBusqueda_SubeJustoALaPrimeraEstrella_SinPreguntarATestigos()
        {
            CalorDePrueba calor = new CalorDePrueba();
            ReglaPrimeraEstrella.Subir(calor);
            Assert.AreEqual(1, calor.Nivel);
            Assert.AreEqual(30f, calor.Valor, 0.01f, "Sube en pasos del 5 % hasta el umbral, no más");
            Assert.AreEqual(MotivoCalor.Mision, calor.UltimoMotivo, "La víctima es la policía: no hacen falta testigos");
        }

        [Test]
        public void YaConBusqueda_NoSumaNada()
        {
            CalorDePrueba calor = new CalorDePrueba();
            calor.Sumar(50f, MotivoCalor.Pelea);
            ReglaPrimeraEstrella.Subir(calor);
            Assert.AreEqual(50f, calor.Valor, 0.01f);
            ReglaPrimeraEstrella.Subir(null);
        }
    }
}
