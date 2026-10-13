using MechaCyberpunk.Core;
using MechaCyberpunk.Mundo.Lugares;
using NUnit.Framework;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>Casos de ReglaPuerta: el orden de decisión definido en IPuerta.IntentarAbrir.</summary>
    public class ReglaPuertaPruebas
    {
        [Test]
        public void Decidir_YaAbierta_DaYaAbiertaAunqueTodoLoDemasLaCierre()
        {
            Assert.AreEqual(ResultadoPuerta.YaAbierta, ReglaPuerta.Decidir(false, true, false, true));
            Assert.AreEqual(ResultadoPuerta.YaAbierta, ReglaPuerta.Decidir(true, false, true, true));
        }

        [Test]
        public void Decidir_JugadorDentroFueraDeHorario_Abre()
        {
            Assert.AreEqual(ResultadoPuerta.Abierta, ReglaPuerta.Decidir(false, false, true, false));
        }

        [Test]
        public void Decidir_JugadorDentroYCerradoEnDemo_Abre()
        {
            Assert.AreEqual(ResultadoPuerta.Abierta, ReglaPuerta.Decidir(false, true, true, false));
        }

        [Test]
        public void Decidir_CerradoEnDemoEnHorario_DaBloqueada()
        {
            Assert.AreEqual(ResultadoPuerta.Bloqueada, ReglaPuerta.Decidir(true, true, false, false));
        }

        [Test]
        public void Decidir_CerradoEnDemoFueraDeHorario_DaBloqueadaYNoPorHorario()
        {
            Assert.AreEqual(ResultadoPuerta.Bloqueada, ReglaPuerta.Decidir(false, true, false, false));
        }

        [Test]
        public void Decidir_FueraDeHorario_DaCerradaPorHorario()
        {
            Assert.AreEqual(ResultadoPuerta.CerradaPorHorario, ReglaPuerta.Decidir(false, false, false, false));
        }

        [Test]
        public void Decidir_EnHorarioYSinCierres_Abre()
        {
            Assert.AreEqual(ResultadoPuerta.Abierta, ReglaPuerta.Decidir(true, false, false, false));
        }
    }
}
