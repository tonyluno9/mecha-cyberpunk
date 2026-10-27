using System;
using MechaCyberpunk.Casino.Datos;
using MechaCyberpunk.Casino.Tragamonedas;
using NUnit.Framework;
using UnityEditor;

namespace MechaCyberpunk.Tests.Casino
{
    /// <summary>Reglas de la tragamonedas (T-300): retorno exacto y medido, determinismo y apuestas sin valor.</summary>
    public class CalculoTragamonedasPruebas
    {
        private const string RutaDatos = "Assets/_Proyecto/Casino/Datos/DatosTragamonedas_Base.asset";
        private const double RetornoMinimo = 0.90;
        private const double RetornoMaximo = 0.97;
        private const int GirosMedidos = 1000000;
        private const int SemillaFija = 20261027;
        private const double ToleranciaMedida = 0.02;
        private const int Apuesta = 50;

        private static DatosTragamonedas Datos()
        {
            DatosTragamonedas datos = AssetDatabase.LoadAssetAtPath<DatosTragamonedas>(RutaDatos);
            Assert.IsNotNull(datos, "Falta " + RutaDatos + "; corre CreadorDatosTragamonedas.Crear.");
            return datos;
        }

        [Test]
        public void RetornoEsperado_ConLosDatosBase_EstaEntre90Y97PorCiento()
        {
            double retorno = Datos().Crear().RetornoEsperado();
            Assert.GreaterOrEqual(retorno, RetornoMinimo);
            Assert.LessOrEqual(retorno, RetornoMaximo);
        }

        [Test]
        public void Girar_UnMillonDeGirosConSemilla_MideElRetornoEsperado()
        {
            CalculoTragamonedas calculo = Datos().Crear();
            Random azar = new Random(SemillaFija);
            long pagado = 0;
            for (int i = 0; i < GirosMedidos; i++)
            {
                pagado += calculo.Pago(calculo.Girar(azar), Apuesta);
            }
            double medido = (double)pagado / ((double)GirosMedidos * Apuesta);
            Assert.AreEqual(calculo.RetornoEsperado(), medido, ToleranciaMedida);
        }

        [Test]
        public void Girar_ConLaMismaSemilla_SacaLosMismosSimbolos()
        {
            CalculoTragamonedas calculo = Datos().Crear();
            Random a = new Random(SemillaFija);
            Random b = new Random(SemillaFija);
            for (int i = 0; i < 100; i++)
            {
                CollectionAssert.AreEqual(calculo.Girar(a), calculo.Girar(b));
            }
        }

        [Test]
        public void Girar_SoloSacaSimbolosDeLaTabla()
        {
            CalculoTragamonedas calculo = Datos().Crear();
            Random azar = new Random(SemillaFija);
            for (int i = 0; i < 1000; i++)
            {
                foreach (int simbolo in calculo.Girar(azar))
                {
                    Assert.GreaterOrEqual(simbolo, 0);
                    Assert.Less(simbolo, calculo.CantidadSimbolos);
                }
            }
        }

        [Test]
        public void Pago_ConApuestaCeroONegativa_NoPagaNada()
        {
            CalculoTragamonedas calculo = Datos().Crear();
            Assert.AreEqual(0, calculo.Pago(new[] { 4, 4, 4 }, 0));
            Assert.AreEqual(0, calculo.Pago(new[] { 4, 4, 4 }, -50));
        }

        [Test]
        public void Pago_TresIguales_PagaElMultiplicadorDelSimbolo()
        {
            DatosTragamonedas datos = Datos();
            CalculoTragamonedas calculo = datos.Crear();
            for (int s = 0; s < datos.Simbolos.Count; s++)
            {
                Assert.AreEqual(Apuesta * datos.Simbolos[s].multiplicadorTres, calculo.Pago(new[] { s, s, s }, Apuesta), "símbolo " + s);
            }
        }

        [Test]
        public void Pago_DosCerezasYOtroSimbolo_PagaElParcial_PeroDosDeOtroSimboloNoPaga()
        {
            DatosTragamonedas datos = Datos();
            CalculoTragamonedas calculo = datos.Crear();
            int cereza = datos.SimboloDosIguales;
            int otro = (cereza + 1) % datos.Simbolos.Count;
            Assert.AreEqual(Apuesta * datos.MultiplicadorDosIguales, calculo.Pago(new[] { cereza, otro, cereza }, Apuesta));
            Assert.AreEqual(0, calculo.Pago(new[] { otro, otro, cereza }, Apuesta));
            Assert.AreEqual(0, calculo.Pago(new[] { otro, cereza, (otro + 1) % datos.Simbolos.Count }, Apuesta));
        }

        [Test]
        public void Constructor_ConDatosInvalidos_Lanza()
        {
            Assert.Throws<ArgumentException>(() => new CalculoTragamonedas(new int[0], new int[0], -1, 0));
            Assert.Throws<ArgumentException>(() => new CalculoTragamonedas(new[] { 1, 0 }, new[] { 1, 1 }, -1, 0));
            Assert.Throws<ArgumentException>(() => new CalculoTragamonedas(new[] { 1, 1 }, new[] { 1 }, -1, 0));
        }
    }
}
