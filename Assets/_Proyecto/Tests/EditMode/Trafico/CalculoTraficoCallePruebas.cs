using System;
using MechaCyberpunk.Trafico.Calle;
using NUnit.Framework;

namespace MechaCyberpunk.Tests.Trafico
{
    /// <summary>T-451: el carro de la calle acelera hasta su crucero, frena si hay algo adelante, se detiene, pita bloqueado y se asusta con un disparo.</summary>
    public class CalculoTraficoCallePruebas
    {
        private const float Crucero = 8f;
        private const float Frenado = 10f;
        private const float DistanciaFrenado = 14f;
        private const float DistanciaParada = 4f;
        private const float Paso = 0.1f;
        private const float Tolerancia = 0.001f;

        private static CalculoTraficoCalle Crear() =>
            new CalculoTraficoCalle(Crucero, 4f, Frenado, DistanciaFrenado, DistanciaParada, 1.5f, 3f);

        private static void Correr(CalculoTraficoCalle c, float segundos, float distancia)
        {
            for (float t = 0f; t < segundos; t += Paso)
            {
                c.Avanzar(Paso, distancia);
            }
        }

        [Test]
        public void VelocidadObjetivo_SegunLaDistanciaAlObstaculo()
        {
            CalculoTraficoCalle c = Crear();
            Assert.AreEqual(Crucero, c.VelocidadObjetivo(float.MaxValue), Tolerancia, "Sin obstáculo, a su crucero");
            Assert.AreEqual(Crucero, c.VelocidadObjetivo(DistanciaFrenado), Tolerancia);
            Assert.AreEqual(0f, c.VelocidadObjetivo(DistanciaParada), Tolerancia, "En la distancia de parada se detiene");
            Assert.AreEqual(0f, c.VelocidadObjetivo(1f), Tolerancia);
            Assert.AreEqual(Crucero / 2f, c.VelocidadObjetivo((DistanciaFrenado + DistanciaParada) / 2f), Tolerancia, "Entre las dos, frena parejo");
        }

        [Test]
        public void Avanzar_SinObstaculo_AceleraHastaElCrucero_YNoPasa()
        {
            CalculoTraficoCalle c = Crear();
            Correr(c, 1f, float.MaxValue);
            Assert.AreEqual(4f, c.Velocidad, 0.01f, "A 4 m/s² en 1 s va a 4 m/s");
            Correr(c, 5f, float.MaxValue);
            Assert.AreEqual(Crucero, c.Velocidad, Tolerancia);
        }

        [Test]
        public void Avanzar_ConUnObstaculoCerca_FrenaYSeDetiene()
        {
            CalculoTraficoCalle c = Crear();
            Correr(c, 3f, float.MaxValue);
            Assert.AreEqual(Crucero, c.Velocidad, Tolerancia);
            Correr(c, 3f, 3f);
            Assert.AreEqual(0f, c.Velocidad, Tolerancia, "Con algo a 3 m se detiene");
        }

        [Test]
        public void Avanzar_BloqueadoMuchoRato_PitaUnaVezYNoSeguido()
        {
            CalculoTraficoCalle c = Crear();
            Correr(c, 3f, 2f);
            Assert.IsTrue(c.Bloqueado);
            Assert.IsTrue(c.TomarPitido(), "Después de 1.5 s bloqueado, pita");
            Assert.IsFalse(c.TomarPitido(), "Tomarlo lo apaga");
            Correr(c, 1f, 2f);
            Assert.IsFalse(c.TomarPitido(), "No vuelve a pitar antes del mínimo entre pitidos");
            Correr(c, 4f, 2f);
            Assert.IsTrue(c.TomarPitido(), "Pasado el mínimo, pita otra vez");
        }

        [Test]
        public void Avanzar_AlLiberarse_VuelveAAcelerar_YDejaDeEstarBloqueado()
        {
            CalculoTraficoCalle c = Crear();
            Correr(c, 3f, 2f);
            Correr(c, 2f, float.MaxValue);
            Assert.Greater(c.Velocidad, 5f);
            Assert.IsFalse(c.Bloqueado);
        }

        [Test]
        public void Asustar_SeDetieneUnRatoYPitaUnaVez()
        {
            CalculoTraficoCalle c = Crear();
            Correr(c, 3f, float.MaxValue);
            c.Asustar(2f);
            Assert.IsTrue(c.EnPanico);
            Correr(c, 1.5f, float.MaxValue);
            Assert.AreEqual(0f, c.Velocidad, Tolerancia, "Con el susto frena aunque la calle esté libre");
            Assert.IsTrue(c.TomarPitido(), "Pita una vez");
            Assert.IsFalse(c.TomarPitido());
            Correr(c, 1f, float.MaxValue);
            Assert.IsFalse(c.EnPanico, "Pasado el susto vuelve a andar");
            Correr(c, 3f, float.MaxValue);
            Assert.Greater(c.Velocidad, 5f);
        }

        [Test]
        public void Constructor_ConDatosInvalidos_Lanza()
        {
            Assert.Throws<ArgumentException>(() => new CalculoTraficoCalle(0f, 4f, 10f, 14f, 4f, 1f, 3f));
            Assert.Throws<ArgumentException>(() => new CalculoTraficoCalle(8f, 4f, 10f, 4f, 4f, 1f, 3f), "Frenado mayor que parada");
            Assert.Throws<ArgumentException>(() => new CalculoTraficoCalle(8f, 0f, 10f, 14f, 4f, 1f, 3f));
        }
    }
}
