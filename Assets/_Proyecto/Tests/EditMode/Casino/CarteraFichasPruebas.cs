using MechaCyberpunk.Casino.Fichas;
using NUnit.Framework;
using UnityEditor;

namespace MechaCyberpunk.Tests.Casino
{
    /// <summary>La lógica pura de las fichas (T-302): saldo nunca negativo, lotes y valor conservado.</summary>
    public class CarteraFichasPruebas
    {
        private const string RutaDatos = "Assets/_Proyecto/Casino/Datos/DatosFichas_Base.asset";
        private const int Lote = 100;

        [Test]
        public void Nueva_EmpiezaEnCero()
        {
            Assert.AreEqual(0, new CarteraFichas().Saldo);
        }

        [Test]
        public void Comprar_SumaYAvisaConLaDiferencia()
        {
            CarteraFichas c = new CarteraFichas();
            int avisado = 0;
            c.Cambio += d => avisado += d;
            c.Comprar(250);
            Assert.AreEqual(250, c.Saldo);
            Assert.AreEqual(250, avisado);
        }

        [TestCase(0)]
        [TestCase(-5)]
        public void Comprar_CantidadNoPositiva_NoCambiaNada(int cantidad)
        {
            CarteraFichas c = new CarteraFichas();
            c.Comprar(10);
            c.Comprar(cantidad);
            Assert.AreEqual(10, c.Saldo);
        }

        [Test]
        public void Comprar_NoDesbordaElEntero()
        {
            CarteraFichas c = new CarteraFichas();
            c.Comprar(int.MaxValue);
            c.Comprar(50);
            Assert.AreEqual(int.MaxValue, c.Saldo);
        }

        [Test]
        public void Retirar_SinSaldoSuficiente_NoCambiaNadaYNuncaQuedaNegativo()
        {
            CarteraFichas c = new CarteraFichas();
            c.Comprar(40);
            Assert.IsFalse(c.Retirar(41));
            Assert.AreEqual(40, c.Saldo);
            Assert.IsFalse(c.Retirar(0));
            Assert.IsFalse(c.Retirar(-3));
            Assert.AreEqual(40, c.Saldo);
            Assert.IsTrue(c.Retirar(40));
            Assert.AreEqual(0, c.Saldo);
        }

        [Test]
        public void Cobrar_QuitaSoloLotesCompletosYGuardaElResto()
        {
            CarteraFichas c = new CarteraFichas();
            c.Comprar(2 * Lote + 37);
            int quitadas = c.Cobrar(Lote);
            Assert.AreEqual(2 * Lote, quitadas);
            Assert.AreEqual(37, c.Saldo, "El resto que no completa un lote se conserva.");
        }

        [Test]
        public void Cobrar_SinUnLoteCompleto_DevuelveCeroYNoCambiaNada()
        {
            CarteraFichas c = new CarteraFichas();
            c.Comprar(Lote - 1);
            Assert.AreEqual(0, c.Cobrar(Lote));
            Assert.AreEqual(Lote - 1, c.Saldo);
            Assert.AreEqual(0, c.Cobrar(0), "Un lote inválido no cobra nada.");
        }

        [Test]
        public void Restablecer_Negativo_SeRechazaSinCambiarNada()
        {
            CarteraFichas c = new CarteraFichas();
            c.Comprar(30);
            Assert.IsFalse(c.Restablecer(-1));
            Assert.AreEqual(30, c.Saldo);
            Assert.IsTrue(c.Restablecer(75));
            Assert.AreEqual(75, c.Saldo);
        }

        [Test]
        public void DatosBase_ExistenConElCambioDeCienPorCien()
        {
            DatosFichas datos = AssetDatabase.LoadAssetAtPath<DatosFichas>(RutaDatos);
            Assert.IsNotNull(datos, "Falta " + RutaDatos + "; corre CreadorFichas.Crear.");
            Assert.AreEqual(100, datos.DineroPorLote);
            Assert.AreEqual(100, datos.FichasPorLote);
        }
    }
}
