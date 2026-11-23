using MechaCyberpunk.IA.APie;
using NUnit.Framework;

namespace MechaCyberpunk.Tests.EditMode.IA
{
    /// <summary>T-1659 (T41): te ven y te miran; si te quedas cerca ~20 s uno se acerca, luego empuja; solo pelean si atacas; si te alejas se acaba.</summary>
    public class ReglaConfrontacionPruebas
    {
        // detección 20 m, 20 s cerca, cerca 8 m, dejar de seguir 14 m, retirada 3 s, 2 empujones máx., contacto 2.5 m
        private static ReglaConfrontacion Crear() => new ReglaConfrontacion(new ReglaConfrontacion.Parametros(20f, 20f, 8f, 14f, 3f, 2, 2.5f));

        private static ReglaConfrontacion.Entrada En(float distancia, bool alerta = true, bool hostil = false)
            => new ReglaConfrontacion.Entrada { Distancia = distancia, Alerta = alerta, Hostil = hostil };

        private static ReglaConfrontacion EnAcercamiento()
        {
            ReglaConfrontacion r = Crear();
            r.Avanzar(0.1f, En(6f));
            r.Avanzar(20.1f, En(6f));
            return r;
        }

        [Test]
        public void Avanzar_JugadorVisto_MiranPeroNoAtacan()
        {
            ReglaConfrontacion r = Crear();
            r.Avanzar(0.1f, En(18f));
            Assert.AreEqual(ReglaConfrontacion.Fase.Mirando, r.Actual);
        }

        [Test]
        public void Avanzar_JugadorFueraDeVista_SigueEnReposo()
        {
            ReglaConfrontacion r = Crear();
            r.Avanzar(0.1f, En(25f));
            Assert.AreEqual(ReglaConfrontacion.Fase.Reposo, r.Actual);
        }

        [Test]
        public void Avanzar_MirandoMuchoTiempoPeroLejos_NuncaSeAcercan()
        {
            ReglaConfrontacion r = Crear();
            r.Avanzar(0.1f, En(15f));
            r.Avanzar(60f, En(15f));
            Assert.AreEqual(ReglaConfrontacion.Fase.Mirando, r.Actual);
        }

        [Test]
        public void Avanzar_VeintesSegundosCerca_UnoSeAcerca()
        {
            ReglaConfrontacion r = Crear();
            r.Avanzar(0.1f, En(6f));
            r.Avanzar(19f, En(6f));
            Assert.AreEqual(ReglaConfrontacion.Fase.Mirando, r.Actual);
            r.Avanzar(1.1f, En(6f));
            Assert.AreEqual(ReglaConfrontacion.Fase.Acercamiento, r.Actual);
        }

        [Test]
        public void Avanzar_SalirDeCercaReiniciaElReloj()
        {
            ReglaConfrontacion r = Crear();
            r.Avanzar(0.1f, En(6f));
            r.Avanzar(15f, En(6f));
            r.Avanzar(1f, En(12f));
            r.Avanzar(10f, En(6f));
            Assert.AreEqual(ReglaConfrontacion.Fase.Mirando, r.Actual);
            Assert.AreEqual(10f, r.SegundosCerca, 0.01f);
        }

        [Test]
        public void Avanzar_AcercandoseYLlega_PasaAEmpujon()
        {
            ReglaConfrontacion r = EnAcercamiento();
            r.Avanzar(1f, En(4f));
            Assert.AreEqual(ReglaConfrontacion.Fase.Acercamiento, r.Actual);
            r.Avanzar(1f, En(2.4f));
            Assert.AreEqual(ReglaConfrontacion.Fase.Empujon, r.Actual);
        }

        [Test]
        public void Avanzar_AcercandoseYTeAlejasMasDeCatorce_SeAcabaTodo()
        {
            ReglaConfrontacion r = EnAcercamiento();
            r.Avanzar(1f, En(15f));
            Assert.AreEqual(ReglaConfrontacion.Fase.Retirada, r.Actual);
        }

        [Test]
        public void Avanzar_AlejarseDeMirandoSinAcercarse_VuelvenAReposo()
        {
            ReglaConfrontacion r = Crear();
            r.Avanzar(0.1f, En(10f));
            r.Avanzar(1f, En(21f));
            Assert.AreEqual(ReglaConfrontacion.Fase.Reposo, r.Actual);
        }

        [Test]
        public void Avanzar_RetiradaYJugadorSigueVisible_NoVuelveAMirarHastaQueSeVa()
        {
            ReglaConfrontacion r = EnAcercamiento();
            r.Avanzar(1f, En(15f));
            r.Avanzar(10f, En(10f));
            Assert.AreEqual(ReglaConfrontacion.Fase.Retirada, r.Actual);
            r.Avanzar(10f, En(25f));
            Assert.AreEqual(ReglaConfrontacion.Fase.Reposo, r.Actual);
        }

        [Test]
        public void Avanzar_JugadorAtacaEnCualquierFase_PasaAPeleaYAlCalmarseVuelveAReposo()
        {
            ReglaConfrontacion r = Crear();
            r.Avanzar(0.1f, En(18f));
            r.Avanzar(0.1f, En(18f, true, true));
            Assert.AreEqual(ReglaConfrontacion.Fase.Pelea, r.Actual);
            r.Avanzar(0.1f, En(18f, true, false));
            Assert.AreEqual(ReglaConfrontacion.Fase.Reposo, r.Actual);
        }

        [Test]
        public void Avanzar_EmpujonesMaximos_SeAburrenYSeRetiran()
        {
            ReglaConfrontacion r = EnAcercamiento();
            r.Avanzar(1f, En(2f));
            Assert.AreEqual(ReglaConfrontacion.Fase.Empujon, r.Actual);
            r.RegistrarEmpujon();
            r.RegistrarEmpujon();
            r.Avanzar(0.1f, En(2f));
            Assert.AreEqual(ReglaConfrontacion.Fase.Retirada, r.Actual);
        }

        [Test]
        public void Avanzar_EmpujandoYTeAlejas_SeAcaba()
        {
            ReglaConfrontacion r = EnAcercamiento();
            r.Avanzar(1f, En(2f));
            r.Avanzar(1f, En(16f));
            Assert.AreEqual(ReglaConfrontacion.Fase.Retirada, r.Actual);
        }
    }
}
