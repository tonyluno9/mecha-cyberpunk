using System;
using MechaCyberpunk.Trafico.Datos;
using MechaCyberpunk.Trafico.Peatones;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.Trafico
{
    /// <summary>Reglas de la caminata de un peatón (T-330) y números de ConfigPeatones.</summary>
    public class CalculoPeatonPruebas
    {
        private const float Velocidad = 2f;
        private const float EsperaFija = 3f;
        private const float Paso = 0.1f;
        private const float Tolerancia = 0.001f;
        private const int Semilla = 7;

        private static CalculoPeaton Crear(float[] tramos, float esperaMin = EsperaFija, float esperaMax = EsperaFija, int semilla = Semilla)
        {
            return new CalculoPeaton(tramos, Velocidad, esperaMin, esperaMax, new System.Random(semilla));
        }

        [Test]
        public void Avanzar_UnTramoDe10m_Termina_EnElTiempoEsperado()
        {
            CalculoPeaton peaton = Crear(new[] { 10f, 10f });
            peaton.Avanzar(4.9f);
            Assert.AreEqual(0, peaton.Segmento);
            Assert.AreEqual(9.8f / 10f, peaton.Fraccion, Tolerancia);
            peaton.Avanzar(0.2f);
            Assert.AreEqual(1, peaton.Segmento, "A los 5 s (10 m a 2 m/s) ya está en el tramo siguiente");
            Assert.IsTrue(peaton.Esperando, "Llegó a una esquina y espera");
        }

        [Test]
        public void Avanzar_EnUnaEsquina_EsperaElTiempoYLuegoSigue()
        {
            CalculoPeaton peaton = Crear(new[] { 10f, 10f });
            peaton.Avanzar(5f);
            Assert.IsTrue(peaton.Esperando);
            peaton.Avanzar(EsperaFija - Paso);
            Assert.IsTrue(peaton.Esperando, "Todavía falta un poco de espera");
            Assert.AreEqual(0f, peaton.Fraccion, Tolerancia, "Esperando no se mueve");
            peaton.Avanzar(Paso + 1f);
            Assert.IsFalse(peaton.Esperando);
            Assert.AreEqual(2f / 10f, peaton.Fraccion, Tolerancia, "Con el segundo que sobró camina 2 m");
        }

        [Test]
        public void Avanzar_AlFinalDeLaRuta_DaLaVueltaSinEsperar()
        {
            CalculoPeaton peaton = Crear(new[] { 10f });
            peaton.Avanzar(5f);
            Assert.AreEqual(-1, peaton.Sentido);
            Assert.IsFalse(peaton.Esperando, "En un final no espera");
            Assert.AreEqual(0, peaton.Segmento);
        }

        [Test]
        public void Avanzar_DeIdaYVuelta_RegresaAlPrimerTramoEnSentidoInverso()
        {
            CalculoPeaton peaton = Crear(new[] { 10f, 10f });
            peaton.Avanzar(5f + EsperaFija + 5f);
            Assert.AreEqual(-1, peaton.Sentido);
            Assert.AreEqual(1, peaton.Segmento);
            peaton.Avanzar(5f);
            Assert.AreEqual(0, peaton.Segmento, "Regresa por el primer tramo");
            Assert.IsTrue(peaton.Esperando);
        }

        [Test]
        public void Avanzar_ConLaMismaSemilla_RepiteElRecorrido()
        {
            CalculoPeaton a = Crear(new[] { 5f, 5f, 5f }, 1f, 4f);
            CalculoPeaton b = Crear(new[] { 5f, 5f, 5f }, 1f, 4f);
            for (int i = 0; i < 2000; i++)
            {
                a.Avanzar(Paso);
                b.Avanzar(Paso);
                Assert.AreEqual(a.Segmento, b.Segmento);
                Assert.AreEqual(a.Fraccion, b.Fraccion, 0f);
                Assert.AreEqual(a.Sentido, b.Sentido);
            }
        }

        [Test]
        public void Avanzar_ConUnPasoMuyGrande_NoSeSaltaLasEsperas()
        {
            CalculoPeaton lento = Crear(new[] { 10f, 10f, 10f });
            CalculoPeaton grande = Crear(new[] { 10f, 10f, 10f });
            for (int i = 0; i < 1026; i++)
            {
                lento.Avanzar(0.05f);
            }
            for (int i = 0; i < 10; i++)
            {
                grande.Avanzar(5f);
            }
            grande.Avanzar(1.3f);   // 10 × 5 s + 1.3 s = 51.3 s, lejos de una frontera de tramo o espera
            Assert.AreEqual(lento.Segmento, grande.Segmento);
            Assert.AreEqual(lento.Sentido, grande.Sentido);
            Assert.AreEqual(lento.Fraccion, grande.Fraccion, 0.02f);
        }

        [Test]
        public void Constructor_ConDatosInvalidos_Lanza()
        {
            Assert.Throws<ArgumentException>(() => Crear(new float[0]));
            Assert.Throws<ArgumentException>(() => Crear(new[] { 10f, 0f }));
            Assert.Throws<ArgumentException>(() => new CalculoPeaton(new[] { 10f }, 0f, 1f, 2f, new System.Random(1)));
            Assert.Throws<ArgumentException>(() => new CalculoPeaton(new[] { 10f }, 1f, 3f, 2f, new System.Random(1)));
        }

        [Test]
        public void ConfigPeatones_PorDefecto_HayMasDeNocheQueDeDiaYEnRango()
        {
            ConfigPeatones config = ScriptableObject.CreateInstance<ConfigPeatones>();
            try
            {
                Assert.Greater(config.Cantidad(true), config.Cantidad(false));
                Assert.GreaterOrEqual(config.Cantidad(false), 12);
                Assert.LessOrEqual(config.Cantidad(true), 30);
                Assert.IsNotNull(config.Crear(new[] { 5f }, new System.Random(1)));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void Avanzar_ConMultiplicador_CaminaMasRapido()
        {
            CalculoPeaton normal = Crear(new[] { 100f });
            CalculoPeaton corriendo = Crear(new[] { 100f });
            corriendo.MultiplicadorVelocidad = 2.5f;
            normal.Avanzar(10f);
            corriendo.Avanzar(10f);
            Assert.AreEqual(20f / 100f, normal.Fraccion, Tolerancia);
            Assert.AreEqual(50f / 100f, corriendo.Fraccion, Tolerancia, "A 2.5 veces la velocidad recorre 2.5 veces más");
        }

        [Test]
        public void InvertirSentido_AMitadDeTramo_QuedaEnElMismoLugarYRegresa()
        {
            CalculoPeaton peaton = Crear(new[] { 10f, 10f });
            peaton.Avanzar(1.5f);
            Vector3[] ruta = { Vector3.zero, new Vector3(10f, 0f, 0f), new Vector3(10f, 0f, 10f) };
            Vector3 antes = RutasBanqueta.Posicion(ruta, peaton);
            peaton.InvertirSentido();
            Vector3 despues = RutasBanqueta.Posicion(ruta, peaton);
            Assert.AreEqual(0f, Vector3.Distance(antes, despues), Tolerancia, "No se mueve al dar la vuelta");
            Assert.AreEqual(-1, peaton.Sentido);
            peaton.Avanzar(1f);
            Assert.Less(RutasBanqueta.Posicion(ruta, peaton).x, antes.x, "Ahora va hacia el punto de donde venía");
        }

        [Test]
        public void InvertirSentido_EnUnaEsquinaEsperando_PasaAlTramoAnterior()
        {
            CalculoPeaton peaton = Crear(new[] { 10f, 10f });
            peaton.Avanzar(5f);
            Assert.IsTrue(peaton.Esperando);
            Vector3[] ruta = { Vector3.zero, new Vector3(10f, 0f, 0f), new Vector3(10f, 0f, 10f) };
            Vector3 antes = RutasBanqueta.Posicion(ruta, peaton);
            peaton.InvertirSentido();
            peaton.CancelarEspera();
            Assert.AreEqual(0f, Vector3.Distance(antes, RutasBanqueta.Posicion(ruta, peaton)), Tolerancia, "Sigue en la esquina");
            Assert.AreEqual(0, peaton.Segmento, "Vuelve al tramo de donde venía");
            peaton.Avanzar(1f);
            Assert.Less(RutasBanqueta.Posicion(ruta, peaton).x, antes.x);
        }

        [Test]
        public void InvertirSentido_EnElExtremoDeLaRuta_NoCambiaNada()
        {
            CalculoPeaton peaton = Crear(new[] { 10f });
            int segmento = peaton.Segmento;
            peaton.InvertirSentido();
            Assert.AreEqual(1, peaton.Sentido, "Sin otro lado no da la vuelta");
            Assert.AreEqual(segmento, peaton.Segmento);
        }
    }
}
