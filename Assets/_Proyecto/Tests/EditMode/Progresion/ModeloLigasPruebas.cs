using MechaCyberpunk.Progresion;
using NUnit.Framework;

namespace MechaCyberpunk.Tests.Progresion
{
    /// <summary>ModeloLigas (T-154): avance por rivales, campeones, revanchas y límites.</summary>
    public class ModeloLigasPruebas
    {
        private ModeloLigas modelo;
        private int cambios;
        private int campeonesAvisados;
        private int ultimaLiga;

        [SetUp]
        public void Preparar()
        {
            modelo = new ModeloLigas(4, 5);
            cambios = 0;
            campeonesAvisados = 0;
            ultimaLiga = 0;
            modelo.Cambio += () => cambios++;
            modelo.CampeonVencido += liga =>
            {
                campeonesAvisados++;
                ultimaLiga = liga;
            };
        }

        private void VencerLiga(int liga)
        {
            for (int i = 1; i <= 5; i++) modelo.RegistrarVictoria(liga, i);
        }

        [Test]
        public void Nuevo_EmpiezaEnLiga1Siguiente1SinCampeones()
        {
            Assert.AreEqual(1, modelo.LigaActual);
            Assert.AreEqual(1, modelo.Siguiente);
            Assert.AreEqual(0, modelo.CampeonesVencidos);
            Assert.IsTrue(modelo.EsJefe(5));
            Assert.IsFalse(modelo.EsJefe(4));
        }

        [Test]
        public void RegistrarVictoria_Del1Al4_Avanza()
        {
            for (int i = 1; i <= 4; i++)
            {
                Assert.AreEqual(ResultadoVictoria.Avanzo, modelo.RegistrarVictoria(1, i));
                Assert.AreEqual(i + 1, modelo.Siguiente);
                Assert.IsTrue(modelo.Vencido(1, i));
            }
            Assert.AreEqual(1, modelo.LigaActual);
            Assert.AreEqual(4, cambios);
            Assert.AreEqual(0, campeonesAvisados);
        }

        [Test]
        public void RegistrarVictoria_ConElCampeon_AbreLaSiguienteLigaYAvisaUnaVez()
        {
            VencerLiga(1);
            Assert.AreEqual(2, modelo.LigaActual);
            Assert.AreEqual(1, modelo.Siguiente);
            Assert.AreEqual(1, modelo.CampeonesVencidos);
            Assert.AreEqual(1, campeonesAvisados);
            Assert.AreEqual(1, ultimaLiga);
        }

        [Test]
        public void RegistrarVictoria_Revancha_NoAvisaNiCambiaNada()
        {
            VencerLiga(1);
            int cambiosAntes = cambios;
            Assert.AreEqual(ResultadoVictoria.Nada, modelo.RegistrarVictoria(1, 5));
            Assert.AreEqual(ResultadoVictoria.Nada, modelo.RegistrarVictoria(1, 2));
            Assert.AreEqual(2, modelo.LigaActual);
            Assert.AreEqual(1, modelo.Siguiente);
            Assert.AreEqual(1, modelo.CampeonesVencidos);
            Assert.AreEqual(1, campeonesAvisados);
            Assert.AreEqual(cambiosAntes, cambios);
        }

        [Test]
        public void PuedeRetar_RivalSalteadoOLigaMasAlta_EsFalsoYNoCambiaNada()
        {
            modelo.RegistrarVictoria(1, 1);
            Assert.AreEqual(2, modelo.Siguiente);
            Assert.IsFalse(modelo.PuedeRetar(1, 3));
            Assert.IsTrue(modelo.PuedeRetar(1, 2));
            Assert.IsTrue(modelo.PuedeRetar(1, 1));
            VencerLiga(1);
            Assert.AreEqual(2, modelo.LigaActual);
            Assert.IsFalse(modelo.PuedeRetar(3, 1));
            int cambiosAntes = cambios;
            Assert.AreEqual(ResultadoVictoria.Nada, modelo.RegistrarVictoria(3, 1));
            Assert.AreEqual(ResultadoVictoria.Nada, modelo.RegistrarVictoria(2, 3));
            Assert.AreEqual(2, modelo.LigaActual);
            Assert.AreEqual(1, modelo.Siguiente);
            Assert.AreEqual(cambiosAntes, cambios);
        }

        [Test]
        public void PuedeRetar_FueraDeRango_EsFalso()
        {
            Assert.IsFalse(modelo.PuedeRetar(0, 1));
            Assert.IsFalse(modelo.PuedeRetar(5, 1));
            Assert.IsFalse(modelo.PuedeRetar(1, 0));
            Assert.IsFalse(modelo.PuedeRetar(1, 6));
            Assert.IsFalse(modelo.Vencido(1, 6));
        }

        [Test]
        public void RegistrarVictoria_CampeonDeLaUltimaLiga_QuedaCampeonDelClandestino()
        {
            VencerLiga(1);
            VencerLiga(2);
            VencerLiga(3);
            for (int i = 1; i <= 4; i++) modelo.RegistrarVictoria(4, i);
            Assert.AreEqual(ResultadoVictoria.CampeonClandestino, modelo.RegistrarVictoria(4, 5));
            Assert.AreEqual(4, modelo.LigaActual);
            Assert.AreEqual(0, modelo.Siguiente);
            Assert.AreEqual(4, modelo.CampeonesVencidos);
            Assert.AreEqual(4, campeonesAvisados);
            Assert.AreEqual(4, ultimaLiga);
            Assert.IsTrue(modelo.Vencido(4, 5));
            Assert.AreEqual(ResultadoVictoria.Nada, modelo.RegistrarVictoria(4, 5));
        }

        [Test]
        public void Poner_ConValoresValidos_FijaElEstadoSinAvisarCampeon()
        {
            Assert.IsTrue(modelo.Poner(3, 4, 2));
            Assert.AreEqual(3, modelo.LigaActual);
            Assert.AreEqual(4, modelo.Siguiente);
            Assert.AreEqual(2, modelo.CampeonesVencidos);
            Assert.AreEqual(1, cambios);
            Assert.AreEqual(0, campeonesAvisados);
            Assert.IsTrue(modelo.Poner(4, 0, 4));
        }

        [Test]
        public void Poner_FueraDeRango_NoCambiaNada()
        {
            modelo.Poner(2, 3, 1);
            int cambiosAntes = cambios;
            Assert.IsFalse(modelo.Poner(0, 1, 0));
            Assert.IsFalse(modelo.Poner(5, 1, 0));
            Assert.IsFalse(modelo.Poner(2, 0, 1), "siguiente 0 solo en la ultima liga");
            Assert.IsFalse(modelo.Poner(2, 6, 1));
            Assert.IsFalse(modelo.Poner(2, 3, -1));
            Assert.IsFalse(modelo.Poner(2, 3, 5));
            Assert.AreEqual(2, modelo.LigaActual);
            Assert.AreEqual(3, modelo.Siguiente);
            Assert.AreEqual(1, modelo.CampeonesVencidos);
            Assert.AreEqual(cambiosAntes, cambios);
        }

        [Test]
        public void Reiniciar_VuelveAlInicio()
        {
            VencerLiga(1);
            modelo.Reiniciar();
            Assert.AreEqual(1, modelo.LigaActual);
            Assert.AreEqual(1, modelo.Siguiente);
            Assert.AreEqual(0, modelo.CampeonesVencidos);
            Assert.AreEqual(1, campeonesAvisados, "reiniciar no avisa campeon");
        }
    }
}
