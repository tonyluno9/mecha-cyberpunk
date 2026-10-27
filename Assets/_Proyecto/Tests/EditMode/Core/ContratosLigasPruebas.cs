using System;
using MechaCyberpunk.Core;
using NUnit.Framework;

namespace MechaCyberpunk.Tests.Core
{
    /// <summary>Contrato de ligas y de la presentación de pelea. El doble sirve de ejemplo para quien lea el contrato.</summary>
    public class ContratosLigasPruebas
    {
        private class LigasFalsas : IProgresoLigas
        {
            private static readonly string[] Nombres = { "SÓTANO", "CALLE", "DISTRITO", "CAMPEONATO" };
            public int CantidadLigas => Nombres.Length;
            public int LigaActual { get; private set; } = 1;
            public int CampeonesVencidos { get; private set; }
            public event Action Cambio;
            public event Action<int> CampeonVencido;

            public string NombreLiga(int liga) => liga >= 1 && liga <= CantidadLigas ? Nombres[liga - 1] : string.Empty;

            public void VencerCampeon(int liga)
            {
                if (liga != CampeonesVencidos + 1) return; // revancha o liga que aún no toca: no cuenta
                CampeonesVencidos = liga;
                LigaActual = Math.Min(liga + 1, CantidadLigas);
                Cambio?.Invoke();
                CampeonVencido?.Invoke(liga);
            }
        }

        [TearDown]
        public void Limpiar() => EventosPelea.LimpiarSuscriptores();

        [Test]
        public void NombreLiga_FueraDeRango_EsVacio()
        {
            IProgresoLigas ligas = new LigasFalsas();
            Assert.AreEqual("SÓTANO", ligas.NombreLiga(1));
            Assert.AreEqual("CAMPEONATO", ligas.NombreLiga(ligas.CantidadLigas));
            Assert.AreEqual(string.Empty, ligas.NombreLiga(0));
            Assert.AreEqual(string.Empty, ligas.NombreLiga(ligas.CantidadLigas + 1));
        }

        [Test]
        public void CampeonVencido_NoSeAvisaEnRevancha_YCambioSeAvisaAlCambiar()
        {
            var ligas = new LigasFalsas();
            int cambios = 0;
            var vencidos = new System.Collections.Generic.List<int>();
            ligas.Cambio += () => cambios++;
            ligas.CampeonVencido += vencidos.Add;

            ligas.VencerCampeon(1);
            ligas.VencerCampeon(1); // revancha
            Assert.AreEqual(1, cambios);
            CollectionAssert.AreEqual(new[] { 1 }, vencidos);
            Assert.AreEqual(2, ligas.LigaActual);
            Assert.AreEqual(1, ligas.CampeonesVencidos);
        }

        [Test]
        public void VencerLaUltima_LigaActualNoSePasa_YEsCampeonDelClandestino()
        {
            var ligas = new LigasFalsas();
            for (int i = 1; i <= ligas.CantidadLigas; i++) ligas.VencerCampeon(i);
            Assert.AreEqual(ligas.CantidadLigas, ligas.LigaActual);
            Assert.AreEqual(ligas.CantidadLigas, ligas.CampeonesVencidos);
        }

        [Test]
        public void PeleaPresentada_EntregaInfo_YLimpiarSuscriptoresLaQuita()
        {
            InfoPelea recibida = default;
            int llamadas = 0;
            Action<InfoPelea> alPresentar = i => { recibida = i; llamadas++; };
            EventosPelea.PeleaPresentada += alPresentar;

            EventosPelea.PublicarPresentacion(new InfoPelea("Chacal", 2, true));
            Assert.AreEqual(1, llamadas);
            Assert.AreEqual("Chacal", recibida.Rival);
            Assert.AreEqual(2, recibida.Liga);
            Assert.IsTrue(recibida.EsJefe);

            EventosPelea.LimpiarSuscriptores();
            EventosPelea.PublicarPresentacion(new InfoPelea("Otro", 0, false));
            Assert.AreEqual(1, llamadas);
        }
    }
}
