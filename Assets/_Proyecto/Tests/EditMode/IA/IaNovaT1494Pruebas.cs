using MechaCyberpunk.IA.APie;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests
{
    /// <summary>T-1494: reglas puras de la percepción de disparos del guardia de Nova y del ciclo del perro robot.</summary>
    public class IaNovaT1494Pruebas
    {
        private static readonly Vector3 Origen = Vector3.zero;
        private static readonly Vector3 Lejos = new Vector3(0f, 0f, 60f);

        [Test]
        public void Percepcion_SinAviso_EstaTranquila()
        {
            ReglaPercepcionDisparo r = new ReglaPercepcionDisparo(6f, 2f);
            r.Avanzar(1f, false, Lejos, Origen);
            Assert.AreEqual(ReglaPercepcionDisparo.Estado.Tranquilo, r.Actual);
            Assert.IsFalse(r.Activa);
        }

        [Test]
        public void Percepcion_AvisoSinVer_InvestigaLaUltimaPosicion()
        {
            ReglaPercepcionDisparo r = new ReglaPercepcionDisparo(6f, 2f);
            r.Avisar(Lejos);
            r.Avanzar(0.1f, false, Lejos, Origen);
            Assert.AreEqual(ReglaPercepcionDisparo.Estado.Investigando, r.Actual);
            Assert.AreEqual(Lejos, r.Ultima);
        }

        [Test]
        public void Percepcion_LlegaSinVer_BuscaYLuegoSeRinde()
        {
            ReglaPercepcionDisparo r = new ReglaPercepcionDisparo(6f, 2f);
            r.Avisar(Lejos);
            r.Avanzar(0.1f, false, Lejos, new Vector3(0f, 0f, 59f));
            Assert.AreEqual(ReglaPercepcionDisparo.Estado.Buscando, r.Actual);
            r.Avanzar(5f, false, Lejos, new Vector3(0f, 0f, 59f));
            Assert.AreEqual(ReglaPercepcionDisparo.Estado.Buscando, r.Actual);
            r.Avanzar(1.5f, false, Lejos, new Vector3(0f, 0f, 59f));
            Assert.AreEqual(ReglaPercepcionDisparo.Estado.Tranquilo, r.Actual);
        }

        [Test]
        public void Percepcion_Ve_ActualizaLaUltimaPosicion()
        {
            ReglaPercepcionDisparo r = new ReglaPercepcionDisparo(6f, 2f);
            r.Avisar(Lejos);
            Vector3 movido = new Vector3(10f, 0f, 50f);
            r.Avanzar(0.1f, true, movido, Origen);
            Assert.AreEqual(ReglaPercepcionDisparo.Estado.Viendo, r.Actual);
            Assert.AreEqual(movido, r.Ultima);
        }

        [Test]
        public void Percepcion_PierdeLaVista_VuelveAInvestigarDondeLoVioPorUltimaVez()
        {
            ReglaPercepcionDisparo r = new ReglaPercepcionDisparo(6f, 2f);
            r.Avisar(Lejos);
            Vector3 visto = new Vector3(10f, 0f, 50f);
            r.Avanzar(0.1f, true, visto, Origen);
            r.Avanzar(0.1f, false, new Vector3(30f, 0f, 30f), Origen);
            Assert.AreEqual(ReglaPercepcionDisparo.Estado.Investigando, r.Actual);
            Assert.AreEqual(visto, r.Ultima);
        }

        [Test]
        public void Percepcion_NuncaLlega_SeRindeALosSegundosMaximos()
        {
            ReglaPercepcionDisparo r = new ReglaPercepcionDisparo(6f, 2f);
            r.Avisar(Lejos);
            r.Avanzar(ReglaPercepcionDisparo.SegundosMaxInvestigar + 1f, false, Lejos, Origen);
            Assert.IsFalse(r.Activa);
        }

        [Test]
        public void Perro_Lejos_Patrulla()
        {
            ReglaPerroNova r = new ReglaPerroNova(20f, 1.4f);
            r.Avanzar(0.1f, new ReglaPerroNova.Entrada(50f, false, true));
            Assert.AreEqual(ReglaPerroNova.Fase.Patrulla, r.Actual);
        }

        [Test]
        public void Perro_DentroDelRadio_Persigue()
        {
            ReglaPerroNova r = new ReglaPerroNova(20f, 1.4f);
            r.Avanzar(0.1f, new ReglaPerroNova.Entrada(15f, false, true));
            Assert.AreEqual(ReglaPerroNova.Fase.Persigue, r.Actual);
        }

        [Test]
        public void Perro_EnAlerta_PersigueDesdeLejos()
        {
            ReglaPerroNova r = new ReglaPerroNova(20f, 1.4f);
            r.Avanzar(0.1f, new ReglaPerroNova.Entrada(80f, true, true));
            Assert.AreEqual(ReglaPerroNova.Fase.Persigue, r.Actual);
        }

        [Test]
        public void Perro_CicloCompleto_AvisaMuerdeYSeRecupera()
        {
            ReglaPerroNova r = new ReglaPerroNova(20f, 1.4f, 0.3f, 0.2f, 1f);
            ReglaPerroNova.Entrada cerca = new ReglaPerroNova.Entrada(1f, false, true);
            r.Avanzar(0.1f, cerca); // Patrulla -> Persigue
            r.Avanzar(0.1f, cerca); // Persigue -> Aviso
            Assert.AreEqual(ReglaPerroNova.Fase.Aviso, r.Actual);
            Assert.IsFalse(r.Avanzar(0.2f, cerca).Mordida);
            Assert.IsTrue(r.Avanzar(0.2f, cerca).Mordida);
            Assert.AreEqual(ReglaPerroNova.Fase.Impulso, r.Actual);
            r.Avanzar(0.3f, cerca);
            Assert.AreEqual(ReglaPerroNova.Fase.Recupera, r.Actual);
            Assert.IsFalse(r.Avanzar(0.5f, cerca).Mordida);
            r.Avanzar(0.6f, cerca);
            Assert.AreEqual(ReglaPerroNova.Fase.Persigue, r.Actual);
        }

        [Test]
        public void Perro_SeAlejaMucho_SinAlerta_SueltaLaPersecucion()
        {
            ReglaPerroNova r = new ReglaPerroNova(20f, 1.4f);
            r.Avanzar(0.1f, new ReglaPerroNova.Entrada(15f, false, true));
            r.Avanzar(0.1f, new ReglaPerroNova.Entrada(40f, false, true));
            Assert.AreEqual(ReglaPerroNova.Fase.Patrulla, r.Actual);
        }

        [Test]
        public void Perro_JugadorNoAtacable_NoPersigue()
        {
            ReglaPerroNova r = new ReglaPerroNova(20f, 1.4f);
            r.Avanzar(0.1f, new ReglaPerroNova.Entrada(5f, true, false));
            Assert.AreEqual(ReglaPerroNova.Fase.Patrulla, r.Actual);
        }
    }
}
