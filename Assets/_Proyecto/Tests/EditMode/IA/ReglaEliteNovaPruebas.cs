using MechaCyberpunk.IA.APie;
using NUnit.Framework;

namespace MechaCyberpunk.Tests
{
    /// <summary>
    /// T-1553: el elite de Nova (E18) sin arma: vuela con el jetpack hacia el jugador, aterriza cerca y pelea a puñetazos; se detiene para aterrizar y para golpear
    /// (decisión de Tony 20-nov).
    /// </summary>
    public class ReglaEliteNovaPruebas
    {
        private const float Dt = 0.01f;
        private const float Alcance = 2.1f;

        private static ReglaEliteNova Crear() => new ReglaEliteNova(ReglaEliteNova.Parametros.Diseno);

        private static ReglaEliteNova.Entrada En(float distancia, bool ve = true, bool enPiso = true) =>
            new ReglaEliteNova.Entrada { Distancia = distancia, Ve = ve, EnPiso = enPiso, AlcanceGolpe = Alcance };

        private static void Avanzar(ReglaEliteNova r, float segundos, ReglaEliteNova.Entrada e)
        {
            for (float t = 0f; t < segundos; t += Dt) { r.Avanzar(Dt, e); }
        }

        [Test]
        public void SinVerAlJugador_PatrullaYNoVuela()
        {
            ReglaEliteNova r = Crear();
            Avanzar(r, 5f, En(50f, ve: false));
            Assert.AreEqual(ReglaEliteNova.Fase.Patrulla, r.Actual);
        }

        [Test]
        public void ViendoAlJugadorLejos_DespegaConElJetpack()
        {
            ReglaEliteNova r = Crear();
            ReglaEliteNova.Salida s = r.Avanzar(Dt, En(25f));
            Assert.IsTrue(s.EmpezoVuelo, "arranca E18_Jetpack_Volar");
            Assert.AreEqual(ReglaEliteNova.Fase.Vuela, r.Actual);
            Assert.IsTrue(r.EnElAire);
        }

        [Test]
        public void ViendoAlJugadorCerca_NoVuela_CaminaYPersigue()
        {
            ReglaEliteNova r = Crear();
            ReglaEliteNova.Salida s = r.Avanzar(Dt, En(r.Config.DistanciaVuelo - 1f));
            Assert.IsFalse(s.EmpezoVuelo);
            Assert.AreEqual(ReglaEliteNova.Fase.Persigue, r.Actual);
        }

        [Test]
        public void Volando_SigueEnElAire_HastaLlegarCercaYEntoncesBaja()
        {
            ReglaEliteNova r = Crear();
            r.Avanzar(Dt, En(25f));
            Avanzar(r, 2f, En(12f, enPiso: false));
            Assert.AreEqual(ReglaEliteNova.Fase.Vuela, r.Actual, "a 12 m aún vuela");
            r.Avanzar(Dt, En(r.Config.DistanciaAterrizaje - 0.5f, enPiso: false));
            Assert.AreEqual(ReglaEliteNova.Fase.Aterriza, r.Actual, "cerca: empieza a bajar");
            Assert.IsTrue(r.EnElAire);
        }

        [Test]
        public void Bajando_AlTocarElPiso_AterrizaQuietoElTiempoDelClip_YLuegoPersigue()
        {
            ReglaEliteNova r = Crear();
            r.Avanzar(Dt, En(25f));
            r.Avanzar(Dt, En(3f, enPiso: false));
            Avanzar(r, 0.5f, En(3f, enPiso: false));
            Assert.AreEqual(ReglaEliteNova.Fase.Aterriza, r.Actual, "sin tocar el piso sigue bajando");
            bool empezoAterrizaje = false;
            for (int i = 0; i < 100 && !empezoAterrizaje; i++)
            {
                empezoAterrizaje = r.Avanzar(Dt, En(3f, enPiso: true)).EmpezoAterrizaje;
            }
            Assert.IsTrue(empezoAterrizaje, "toca el piso: E18_Caida_Aterrizaje");
            Assert.IsTrue(r.Quieto, "quieto mientras aterriza");
            Avanzar(r, r.Config.SegundosAterrizaje * 0.5f, En(3f));
            Assert.AreEqual(ReglaEliteNova.Fase.Aterrizaje, r.Actual);
            Avanzar(r, r.Config.SegundosAterrizaje, En(3f));
            Assert.AreEqual(ReglaEliteNova.Fase.Persigue, r.Actual, "termina el clip y pelea");
            Assert.IsFalse(r.Quieto);
        }

        [Test]
        public void Persiguiendo_AlLlegarAlAlcance_SeDetieneYGolpeaEnElCuadroDelPuño()
        {
            ReglaEliteNova r = Crear();
            r.Avanzar(Dt, En(5f)); // Patrulla -> Persigue
            ReglaEliteNova.Salida s = r.Avanzar(Dt, En(Alcance - 0.1f));
            Assert.IsTrue(s.EmpezoGolpe, "empieza E18_Golpe_Der");
            Assert.IsTrue(r.Quieto, "se detiene para golpear");

            float tiempo = 0f;
            int impactos = 0;
            float tImpacto = -1f;
            while (r.Actual == ReglaEliteNova.Fase.AvisoGolpe && tiempo < 3f)
            {
                s = r.Avanzar(Dt, En(Alcance - 0.1f));
                tiempo += Dt;
                if (s.Impacto) { impactos++; tImpacto = tiempo; }
            }
            Assert.AreEqual(1, impactos);
            Assert.AreEqual(16f / 30f, tImpacto, 0.05f, "el puño conecta hacia el cuadro 16");
            Assert.AreEqual(ReglaEliteNova.Fase.Recupera, r.Actual);
            Assert.IsTrue(r.Quieto, "sigue quieto hasta terminar el clip");
        }

        [Test]
        public void Golpe_DuraLosCuarentaYNueveCuadros_YLuegoVuelveAPerseguir()
        {
            ReglaEliteNova r = Crear();
            r.Avanzar(Dt, En(5f));
            r.Avanzar(Dt, En(Alcance - 0.1f));
            float tiempo = 0f;
            while (r.Quieto && tiempo < 5f)
            {
                r.Avanzar(Dt, En(Alcance - 0.1f));
                tiempo += Dt;
            }
            Assert.AreEqual(49f / 30f, tiempo + Dt, 0.06f, "49 cuadros a 30 por segundo");
            Assert.AreEqual(ReglaEliteNova.Fase.Persigue, r.Actual);
            Assert.AreEqual(1, r.Golpes);
        }

        [Test]
        public void SiElJugadorSeAleja_MuchoDespuesDePerseguir_VuelveADespegar()
        {
            ReglaEliteNova r = Crear();
            r.Avanzar(Dt, En(5f));
            Assert.AreEqual(ReglaEliteNova.Fase.Persigue, r.Actual);
            ReglaEliteNova.Salida s = r.Avanzar(Dt, En(r.Config.DistanciaRelanza + 3f));
            Assert.IsTrue(s.EmpezoVuelo);
            Assert.AreEqual(ReglaEliteNova.Fase.Vuela, r.Actual);
        }

        [Test]
        public void PerdiendoAlJugador_VuelveAPatrullar()
        {
            ReglaEliteNova r = Crear();
            r.Avanzar(Dt, En(5f));
            r.Avanzar(Dt, En(5f, ve: false));
            Assert.AreEqual(ReglaEliteNova.Fase.Patrulla, r.Actual);
        }

        [Test]
        public void Interrumpir_CortaElGolpeYElAterrizaje_ParaSeguirPersiguiendo()
        {
            ReglaEliteNova r = Crear();
            r.Avanzar(Dt, En(5f));
            r.Avanzar(Dt, En(Alcance - 0.1f));
            Assert.AreEqual(ReglaEliteNova.Fase.AvisoGolpe, r.Actual);
            r.Interrumpir();
            Assert.AreEqual(ReglaEliteNova.Fase.Persigue, r.Actual);
            Assert.AreEqual(0, r.Golpes, "el golpe interrumpido no conectó");
        }

        [Test]
        public void Interrumpir_EnElAire_NoHaceNada()
        {
            ReglaEliteNova r = Crear();
            r.Avanzar(Dt, En(25f));
            r.Interrumpir();
            Assert.AreEqual(ReglaEliteNova.Fase.Vuela, r.Actual, "un golpe no lo baja del cielo");
        }

        [TestCase(0f, 3f, 6f)]
        [TestCase(3f, 3f, 0f)]
        [TestCase(10f, 3f, -6f)]
        [TestCase(2.5f, 3f, 1f)]
        public void VelocidadVertical_VaHaciaLaAlturaSinPasarseDelTope(float altura, float objetivo, float esperada)
        {
            Assert.AreEqual(esperada, ReglaEliteNova.VelocidadVerticalHacia(altura, objetivo, 6f), 1e-4f);
        }

        [Test]
        public void Reiniciar_VuelveAPatrullaYOlvidaLosGolpes()
        {
            ReglaEliteNova r = Crear();
            r.Avanzar(Dt, En(5f));
            r.Avanzar(Dt, En(Alcance - 0.1f));
            Avanzar(r, 1f, En(Alcance - 0.1f));
            r.Reiniciar();
            Assert.AreEqual(ReglaEliteNova.Fase.Patrulla, r.Actual);
            Assert.AreEqual(0, r.Golpes);
        }
    }
}
