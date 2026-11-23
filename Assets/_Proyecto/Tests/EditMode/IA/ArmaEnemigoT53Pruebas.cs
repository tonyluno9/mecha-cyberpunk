using MechaCyberpunk.IA.APie;
using NUnit.Framework;

namespace MechaCyberpunk.Tests.IA
{
    /// <summary>T-53: el enemigo saca el arma, gasta el cargador, recarga y guarda como el jugador.</summary>
    public class ArmaEnemigoT53Pruebas
    {
        private static ReglaArmaEnemigo Nueva(TipoArmaEnemigo tipo = TipoArmaEnemigo.Pistola) => new ReglaArmaEnemigo(tipo, 2f, 1f, 3f);

        [Test]
        public void Guardada_SinPelea_NoPuedeDisparar()
        {
            ReglaArmaEnemigo r = Nueva();
            r.Avanzar(0.1f, false, false);
            Assert.AreEqual(FaseArmaEnemigo.Guardada, r.Fase);
            Assert.IsFalse(r.PuedeDisparar);
        }

        [Test]
        public void EnPelea_PrimeroSacaElArma_YNoDisparaHastaEstarEnPose()
        {
            ReglaArmaEnemigo r = Nueva();
            ReglaArmaEnemigo.Salida s = r.Avanzar(0.1f, true, false);
            Assert.IsTrue(s.EmpezoSacar);
            Assert.AreEqual(FaseArmaEnemigo.Sacando, r.Fase);
            Assert.IsFalse(r.PuedeDisparar);
            r.Avanzar(0.1f, true, true);
            Assert.AreEqual(FaseArmaEnemigo.Lista, r.Fase);
            Assert.IsTrue(r.PuedeDisparar);
        }

        [Test]
        public void Sacando_SinPoseDelAnimador_PasaAListaAlAcabarseElTiempoMaximo()
        {
            ReglaArmaEnemigo r = Nueva();
            r.Avanzar(0.1f, true, false);
            r.Avanzar(1.1f, true, false);
            Assert.AreEqual(FaseArmaEnemigo.Lista, r.Fase);
        }

        [Test]
        public void Cargador_TieneLaCapacidadDelArma()
        {
            Assert.AreEqual(12, ReglaArmaEnemigo.Capacidad(TipoArmaEnemigo.Pistola));
            Assert.AreEqual(6, ReglaArmaEnemigo.Capacidad(TipoArmaEnemigo.Escopeta));
            Assert.AreEqual(30, ReglaArmaEnemigo.Capacidad(TipoArmaEnemigo.Subfusil));
            Assert.AreEqual(6, Nueva(TipoArmaEnemigo.Escopeta).Municion);
        }

        [Test]
        public void VaciarElCargador_Recarga_YNoDisparaHastaTerminar()
        {
            ReglaArmaEnemigo r = Nueva(TipoArmaEnemigo.Escopeta);
            r.Avanzar(0.1f, true, false);
            r.Avanzar(0.1f, true, true);
            for (int i = 0; i < 6; i++)
            {
                Assert.IsTrue(r.RegistrarDisparo(), "bala " + i);
            }
            Assert.AreEqual(0, r.Municion);
            Assert.IsFalse(r.PuedeDisparar);
            Assert.IsFalse(r.RegistrarDisparo(), "sin balas no dispara");
            ReglaArmaEnemigo.Salida s = r.Avanzar(0.1f, true, true);
            Assert.IsTrue(s.EmpezoRecargar);
            Assert.AreEqual(FaseArmaEnemigo.Recargando, r.Fase);
            r.Avanzar(1.5f, true, true);
            Assert.IsFalse(r.PuedeDisparar, "la recarga dura 2 s");
            r.Avanzar(0.6f, true, true);
            Assert.AreEqual(FaseArmaEnemigo.Lista, r.Fase);
            Assert.AreEqual(6, r.Municion);
            Assert.IsTrue(r.PuedeDisparar);
        }

        [Test]
        public void DejarDePelear_GuardaElArmaDespuesDelTiempo()
        {
            ReglaArmaEnemigo r = Nueva();
            r.Avanzar(0.1f, true, false);
            r.Avanzar(0.1f, true, true);
            Assert.IsFalse(r.Avanzar(2f, false, true).Guardar);
            Assert.IsTrue(r.ArmaFuera);
            Assert.IsTrue(r.Avanzar(1.2f, false, true).Guardar);
            Assert.AreEqual(FaseArmaEnemigo.Guardada, r.Fase);
            Assert.IsFalse(r.ArmaFuera);
        }

        [Test]
        public void VolverAPelear_ReiniciaElContadorDeGuardar()
        {
            ReglaArmaEnemigo r = Nueva();
            r.Avanzar(0.1f, true, false);
            r.Avanzar(0.1f, true, true);
            r.Avanzar(2.5f, false, true);
            r.Avanzar(0.1f, true, true);
            Assert.IsFalse(r.Avanzar(2.5f, false, true).Guardar);
        }

        [Test]
        public void ElegirArma_LosSkullsVarian_YElResto()
        {
            Assert.AreEqual(TipoArmaEnemigo.Pistola, PandilleroAPie.ElegirArma(true, 1, 0.1f));
            Assert.AreEqual(TipoArmaEnemigo.Subfusil, PandilleroAPie.ElegirArma(true, 1, 0.6f));
            Assert.AreEqual(TipoArmaEnemigo.Escopeta, PandilleroAPie.ElegirArma(true, 1, 0.9f));
            Assert.AreEqual(TipoArmaEnemigo.Pistola, PandilleroAPie.ElegirArma(false, 1, 0.9f), "policías y SNAKES: pistola");
            Assert.AreEqual(TipoArmaEnemigo.Escopeta, PandilleroAPie.ElegirArma(false, 8, 0.1f), "varios perdigones: escopeta");
        }
    }
}
