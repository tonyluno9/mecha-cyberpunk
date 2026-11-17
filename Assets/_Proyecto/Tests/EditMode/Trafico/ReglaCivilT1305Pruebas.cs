using MechaCyberpunk.Core;
using MechaCyberpunk.Trafico.Peatones;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.Trafico
{
    /// <summary>T-1305: la lógica pura del civil agredido (revancha o huida) y la atribución del daño al jugador.</summary>
    public class ReglaCivilT1305Pruebas
    {
        private const float Alcance = 1.5f;
        private const float Probabilidad = 0.3f;
        private const int Golpes = 2;
        private const float Espera = 0.7f;
        private const float Maximo = 3f;
        private const float Paso = 0.1f;

        private static ReglaCivil Crear(float azar) => new ReglaCivil(() => azar, Probabilidad, Alcance, Golpes, Espera, Maximo);

        [Test]
        public void Herido_AzarBajoYJugadorCerca_EntraEnRevancha()
        {
            Assert.AreEqual(EstadoCivil.Revancha, Crear(0.1f).Herido(1f));
        }

        [Test]
        public void Herido_AzarAlto_Huye()
        {
            Assert.AreEqual(EstadoCivil.Huyendo, Crear(0.9f).Herido(1f));
        }

        [Test]
        public void Herido_JugadorLejos_HuyeAunqueElAzarDiga()
        {
            Assert.AreEqual(EstadoCivil.Huyendo, Crear(0.1f).Herido(10f));
            Assert.AreEqual(EstadoCivil.Huyendo, Crear(0.1f).Herido(null));
        }

        [Test]
        public void Avanzar_EnRevancha_DaDosGolpesEspaciadosYLuegoHuye()
        {
            ReglaCivil regla = Crear(0.1f);
            regla.Herido(1f);
            int golpes = 0;
            for (float t = 0f; t < Maximo; t += Paso)
            {
                if (regla.Avanzar(Paso, 1f))
                {
                    golpes++;
                }
            }
            Assert.AreEqual(Golpes, golpes);
            Assert.AreEqual(EstadoCivil.Huyendo, regla.Estado);
        }

        [Test]
        public void Avanzar_JugadorSeAleja_NoGolpeaYAlFinalHuye()
        {
            ReglaCivil regla = Crear(0.1f);
            regla.Herido(1f);
            int golpes = 0;
            for (float t = 0f; t < Maximo + 1f; t += Paso)
            {
                if (regla.Avanzar(Paso, 5f))
                {
                    golpes++;
                }
            }
            Assert.AreEqual(0, golpes);
            Assert.AreEqual(EstadoCivil.Huyendo, regla.Estado);
        }

        [Test]
        public void Calmar_VuelveANormal()
        {
            ReglaCivil regla = Crear(0.9f);
            regla.Herido(1f);
            regla.Calmar();
            Assert.AreEqual(EstadoCivil.Normal, regla.Estado);
        }

        [Test]
        public void EsAgresionDelJugador_BalaQueVieneDelJugador_Si()
        {
            Vector3 jugador = Vector3.zero;
            Vector3 punto = new Vector3(0f, ReglaCivil.AlturaOjosJugador, 20f);
            Assert.IsTrue(ReglaCivil.EsAgresionDelJugador(TipoDanio.Disparo, punto, Vector3.forward, jugador, false));
        }

        [Test]
        public void EsAgresionDelJugador_BalaDeOtroLado_No()
        {
            Vector3 punto = new Vector3(0f, ReglaCivil.AlturaOjosJugador, 20f);
            Assert.IsFalse(ReglaCivil.EsAgresionDelJugador(TipoDanio.Disparo, punto, Vector3.right, Vector3.zero, false));
        }

        [Test]
        public void EsAgresionDelJugador_GolpeConJugadorPegado_Si_YLejosNo()
        {
            Assert.IsTrue(ReglaCivil.EsAgresionDelJugador(TipoDanio.Golpe, new Vector3(0f, 1f, 1f), Vector3.forward, Vector3.zero, false));
            Assert.IsFalse(ReglaCivil.EsAgresionDelJugador(TipoDanio.Golpe, new Vector3(0f, 1f, 10f), Vector3.forward, Vector3.zero, false));
        }

        [Test]
        public void EsAgresionDelJugador_ConAtacanteOSinJugador_No()
        {
            Vector3 punto = new Vector3(0f, ReglaCivil.AlturaOjosJugador, 5f);
            Assert.IsFalse(ReglaCivil.EsAgresionDelJugador(TipoDanio.Disparo, punto, Vector3.forward, Vector3.zero, true));
            Assert.IsFalse(ReglaCivil.EsAgresionDelJugador(TipoDanio.Disparo, punto, Vector3.forward, null, false));
        }
    }
}
