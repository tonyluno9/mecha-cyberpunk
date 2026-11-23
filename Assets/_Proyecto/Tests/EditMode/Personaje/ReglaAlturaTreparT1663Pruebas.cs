using MechaCyberpunk.Personaje.Movimiento;
using NUnit.Framework;

namespace MechaCyberpunk.Tests.Personaje
{
    public class ReglaAlturaTreparT1663Pruebas
    {
        [Test]
        public void Evaluar_BordeBajo_Permitido()
        {
            Assert.AreEqual(MotivoTrepar.Permitido, ReglaAlturaTrepar.Evaluar(1.2f, 2.5f));
        }

        [Test]
        public void Evaluar_ExactamenteEnElUmbral_Permitido()
        {
            Assert.AreEqual(MotivoTrepar.Permitido, ReglaAlturaTrepar.Evaluar(2.5f, 2.5f));
        }

        [Test]
        public void Evaluar_Azotea_DemasiadoAlto()
        {
            Assert.AreEqual(MotivoTrepar.DemasiadoAlto, ReglaAlturaTrepar.Evaluar(6f, 2.5f));
        }

        [Test]
        public void Evaluar_UmbralConfigurable_CambiaResultado()
        {
            Assert.AreEqual(MotivoTrepar.DemasiadoAlto, ReglaAlturaTrepar.Evaluar(2.6f, 2.5f));
            Assert.AreEqual(MotivoTrepar.Permitido, ReglaAlturaTrepar.Evaluar(2.6f, 3f));
        }
    }
}
