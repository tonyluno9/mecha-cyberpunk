using MechaCyberpunk.Combate.Habilidades;
using NUnit.Framework;

namespace MechaCyberpunk.Tests
{
    public class ReglaHabilidadPruebas
    {
        private static MotivoHabilidad Decidir(bool tiene = true, bool enPelea = true, bool control = true, bool aturdido = false,
            bool guardia = false, float enfriando = 0f, int municion = 3)
        {
            return ReglaHabilidad.Decidir(tiene, enPelea, control, aturdido, guardia, enfriando, municion);
        }

        [Test]
        public void Decidir_TodoEnOrden_Puede()
        {
            Assert.AreEqual(MotivoHabilidad.Puede, Decidir());
        }

        [Test]
        public void Decidir_CadaMotivoSaleEnSuOrden()
        {
            Assert.AreEqual(MotivoHabilidad.SinHabilidad, Decidir(tiene: false, enPelea: false, control: false, aturdido: true, guardia: true, enfriando: 2f, municion: 0));
            Assert.AreEqual(MotivoHabilidad.FueraDePelea, Decidir(enPelea: false, control: false, aturdido: true, guardia: true, enfriando: 2f, municion: 0));
            Assert.AreEqual(MotivoHabilidad.SinControl, Decidir(control: false, aturdido: true, guardia: true, enfriando: 2f, municion: 0));
            Assert.AreEqual(MotivoHabilidad.Aturdido, Decidir(aturdido: true, guardia: true, enfriando: 2f, municion: 0));
            Assert.AreEqual(MotivoHabilidad.GuardiaArriba, Decidir(guardia: true, enfriando: 2f, municion: 0));
            Assert.AreEqual(MotivoHabilidad.Enfriando, Decidir(enfriando: 2f, municion: 0));
            Assert.AreEqual(MotivoHabilidad.SinMunicion, Decidir(municion: 0));
        }

        [Test]
        public void Potencia_PorNivel()
        {
            Assert.AreEqual(1.0f, ReglaHabilidad.Potencia(1), 0.0001f);
            Assert.AreEqual(1.25f, ReglaHabilidad.Potencia(2), 0.0001f);
            Assert.AreEqual(1.5f, ReglaHabilidad.Potencia(3), 0.0001f);
            Assert.AreEqual(1.75f, ReglaHabilidad.Potencia(4), 0.0001f);
        }

        [Test]
        public void Enfriamiento_ReduccionPedidaMayorQueElTope_SeLimitaA40Porciento()
        {
            Assert.AreEqual(4.8f, ReglaHabilidad.Enfriamiento(8f, 0.6f), 0.0001f);
            Assert.AreEqual(8f, ReglaHabilidad.Enfriamiento(8f, -1f), 0.0001f);
            Assert.AreEqual(6.8f, ReglaHabilidad.Enfriamiento(8f, 0.15f), 0.0001f);
        }

        [Test]
        public void Avanzar_NoBajaDeCero()
        {
            Assert.AreEqual(0f, ReglaHabilidad.Avanzar(0.1f, 0.5f), 0f);
            Assert.AreEqual(1.5f, ReglaHabilidad.Avanzar(2f, 0.5f), 0.0001f);
        }
    }
}
