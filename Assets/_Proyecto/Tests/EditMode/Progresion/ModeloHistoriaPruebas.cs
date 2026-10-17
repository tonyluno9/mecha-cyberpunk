using MechaCyberpunk.Core;
using MechaCyberpunk.Progresion;
using NUnit.Framework;

namespace MechaCyberpunk.Tests.Progresion
{
    /// <summary>ModeloHistoria (T-124): solo avanza hacia adelante y avisa una vez por cambio.</summary>
    public class ModeloHistoriaPruebas
    {
        [Test]
        public void Nuevo_EmpiezaEnNinguna()
        {
            Assert.AreEqual(EtapaPrologo.Ninguna, new ModeloHistoria().Etapa);
        }

        [Test]
        public void Avanzar_AUnaEtapaPosterior_CambiaYAvisaUnaVez()
        {
            var modelo = new ModeloHistoria();
            int avisos = 0;
            EtapaPrologo ultima = EtapaPrologo.Ninguna;
            modelo.EtapaCambio += e =>
            {
                avisos++;
                ultima = e;
            };
            Assert.IsTrue(modelo.Avanzar(EtapaPrologo.PeleaTutorial));
            Assert.AreEqual(EtapaPrologo.PeleaTutorial, modelo.Etapa);
            Assert.AreEqual(1, avisos);
            Assert.AreEqual(EtapaPrologo.PeleaTutorial, ultima);
        }

        [Test]
        public void Avanzar_AUnaEtapaAnteriorOIgual_NoCambiaNiAvisa()
        {
            var modelo = new ModeloHistoria();
            modelo.Avanzar(EtapaPrologo.Emboscada);
            int avisos = 0;
            modelo.EtapaCambio += _ => avisos++;
            Assert.IsFalse(modelo.Avanzar(EtapaPrologo.CreacionPersonaje));
            Assert.IsFalse(modelo.Avanzar(EtapaPrologo.Emboscada));
            Assert.AreEqual(EtapaPrologo.Emboscada, modelo.Etapa);
            Assert.AreEqual(0, avisos);
        }

        [Test]
        public void Reiniciar_DespuesDeAvanzar_VuelveANingunaYAvisa()
        {
            var modelo = new ModeloHistoria();
            modelo.Avanzar(EtapaPrologo.Dormir);
            EtapaPrologo? aviso = null;
            modelo.EtapaCambio += e => aviso = e;
            modelo.Reiniciar();
            Assert.AreEqual(EtapaPrologo.Ninguna, modelo.Etapa);
            Assert.AreEqual(EtapaPrologo.Ninguna, aviso);
        }

        [Test]
        public void Reiniciar_EnNinguna_NoAvisa()
        {
            var modelo = new ModeloHistoria();
            int avisos = 0;
            modelo.EtapaCambio += _ => avisos++;
            modelo.Reiniciar();
            Assert.AreEqual(0, avisos);
        }

        [Test]
        public void Reiniciar_LuegoAvanzar_VuelveAAceptarEtapasBajas()
        {
            var modelo = new ModeloHistoria();
            modelo.Avanzar(EtapaPrologo.Libre);
            modelo.Reiniciar();
            Assert.IsTrue(modelo.Avanzar(EtapaPrologo.CreacionPersonaje));
        }
    }
}
