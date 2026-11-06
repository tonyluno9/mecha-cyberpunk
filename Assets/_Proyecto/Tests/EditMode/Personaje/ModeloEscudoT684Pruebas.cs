using MechaCyberpunk.Personaje.Vida;
using NUnit.Framework;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>T-684: reglas del escudo de energía (C# puro).</summary>
    public class ModeloEscudoT684Pruebas
    {
        [Test]
        public void Absorber_MenosQueElEscudo_NoPasaNadaALaVida()
        {
            var e = new ModeloEscudo(30f, 10f, 4f);
            Assert.AreEqual(0f, e.Absorber(20f), 0.0001f);
            Assert.AreEqual(10f, e.Actual, 0.0001f);
        }

        [Test]
        public void Absorber_MasQueElEscudo_DevuelveElResto()
        {
            var e = new ModeloEscudo(30f, 10f, 4f);
            Assert.AreEqual(15f, e.Absorber(45f), 0.0001f);
            Assert.AreEqual(0f, e.Actual, 0.0001f);
            Assert.AreEqual(7f, e.Absorber(7f), 0.0001f, "Sin escudo todo pasa");
        }

        [Test]
        public void Absorber_AvisaConLoAbsorbido()
        {
            var e = new ModeloEscudo(30f, 10f, 4f);
            float avisado = 0f;
            e.Absorbio += p => avisado += p;
            e.Absorber(12f);
            e.Absorber(50f);
            Assert.AreEqual(30f, avisado, 0.0001f);
        }

        [Test]
        public void Recarga_NoEmpiezaAntesDeLaEspera()
        {
            var e = new ModeloEscudo(30f, 10f, 4f);
            e.Absorber(30f);
            e.Avanzar(3.9f);
            Assert.AreEqual(0f, e.Actual, 0.0001f);
        }

        [Test]
        public void Recarga_DespuesDeLaEspera_SubeALaVelocidadDeLosDatosYNoPasaDelMaximo()
        {
            var e = new ModeloEscudo(30f, 10f, 4f);
            e.Absorber(30f);
            e.Avanzar(4f);
            e.Avanzar(1f);
            Assert.AreEqual(10f, e.Actual, 0.01f);
            e.Avanzar(10f);
            Assert.AreEqual(30f, e.Actual, 0.0001f);
        }

        [Test]
        public void Recarga_UnGolpeNuevoReiniciaLaEspera()
        {
            var e = new ModeloEscudo(30f, 10f, 4f);
            e.Absorber(10f);
            e.Avanzar(3f);
            e.Absorber(5f);
            e.Avanzar(3f);
            Assert.AreEqual(15f, e.Actual, 0.0001f, "Pasaron 3 s desde el último golpe: aún no recarga");
        }

        [Test]
        public void Mejorar_SubeElMaximoYLaRecarga()
        {
            var e = new ModeloEscudo(30f, 10f, 4f);
            e.MejorarMaximo(20f);
            Assert.AreEqual(50f, e.Maximo, 0.0001f);
            Assert.AreEqual(50f, e.Actual, 0.0001f, "Los puntos nuevos vienen llenos");
            e.MejorarRecarga(5f);
            Assert.AreEqual(15f, e.RecargaPorSegundo, 0.0001f);
            e.Absorber(50f);
            e.Avanzar(4f);
            e.Avanzar(2f);
            Assert.AreEqual(30f, e.Actual, 0.01f, "Recarga 15 por segundo");
        }

        [Test]
        public void Mejorar_ValoresNoPositivos_Ignora()
        {
            var e = new ModeloEscudo(30f, 10f, 4f);
            e.MejorarMaximo(-5f);
            e.MejorarRecarga(0f);
            Assert.AreEqual(30f, e.Maximo, 0.0001f);
            Assert.AreEqual(10f, e.RecargaPorSegundo, 0.0001f);
        }

        [Test]
        public void Recuperar_NoPasaDelMaximo()
        {
            var e = new ModeloEscudo(30f, 10f, 4f);
            e.Absorber(25f);
            e.Recuperar(100f);
            Assert.AreEqual(30f, e.Actual, 0.0001f);
        }

        [Test]
        public void SinEscudo_Maximo0_TodoPasaALaVida()
        {
            var e = new ModeloEscudo(0f, 10f, 4f);
            Assert.AreEqual(9f, e.Absorber(9f), 0.0001f);
        }

        [Test]
        public void FijarBonos_NoLlenaElEscudoYRespetaElTope()
        {
            var e = new ModeloEscudo(30f, 10f, 4f);
            e.Absorber(20f);
            e.FijarBonos(10f, 2f);
            Assert.AreEqual(40f, e.Maximo, 0.0001f);
            Assert.AreEqual(12f, e.RecargaPorSegundo, 0.0001f);
            Assert.AreEqual(10f, e.Actual, 0.0001f);
        }

        [Test]
        public void VidaRegenerable_Curar_NoPasaDelMaximoNiRevive()
        {
            var v = new ModeloVidaRegenerable(100f, 5f, 6f);
            v.Aplicar(30f);
            Assert.AreEqual(20f, v.Curar(20f), 0.0001f);
            Assert.AreEqual(10f, v.Curar(50f), 0.0001f);
            v.Aplicar(1000f);
            Assert.AreEqual(0f, v.Curar(10f), 0.0001f, "Un KO no se cura con objetos");
        }
    }
}
