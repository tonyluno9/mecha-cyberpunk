using System;
using MechaCyberpunk.UI.Inicio;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests
{
    public class ReglaInicioPruebas
    {
        [Test]
        public void ProgresoDeCarga_ConProgresoDeUnity_LlegaAUnoEn09()
        {
            Assert.AreEqual(0f, ReglaInicio.ProgresoDeCarga(0f), 1e-5f);
            Assert.AreEqual(0.5f, ReglaInicio.ProgresoDeCarga(0.45f), 1e-5f);
            Assert.AreEqual(1f, ReglaInicio.ProgresoDeCarga(0.9f), 1e-5f);
            Assert.AreEqual(1f, ReglaInicio.ProgresoDeCarga(1.5f), 1e-5f);
        }

        [Test]
        public void Suavizar_Avanzando_NuncaRetrocedeNiPasaElObjetivo()
        {
            float mostrado = 0f;
            float anterior = 0f;
            for (int i = 0; i < 600; i++)
            {
                mostrado = ReglaInicio.Suavizar(mostrado, 0.7f, 0.016f, 0.5f);
                Assert.GreaterOrEqual(mostrado, anterior, "No retrocede");
                Assert.LessOrEqual(mostrado, 0.7f + 1e-6f, "No pasa el objetivo");
                anterior = mostrado;
            }
            Assert.AreEqual(0.7f, mostrado, 1e-4f, "Termina llegando al objetivo");
        }

        [Test]
        public void Suavizar_ConObjetivoMenorQueLoMostrado_ConservaLoMostrado()
        {
            Assert.AreEqual(0.8f, ReglaInicio.Suavizar(0.8f, 0.3f, 0.016f, 0.5f), 1e-6f);
        }

        [Test]
        public void Suavizar_UnCuadro_NoVaMasRapidoQueLaVelocidad()
        {
            float nuevo = ReglaInicio.Suavizar(0f, 1f, 0.1f, 0.5f);
            Assert.LessOrEqual(nuevo, 0.05f + 1e-6f);
            Assert.Greater(nuevo, 0f, "Nunca se detiene del todo");
        }

        [Test]
        public void PuedeActivar_ExigeCargaBarraYTiempoMinimo()
        {
            Assert.IsFalse(ReglaInicio.PuedeActivar(0.5f, 1f, 5f, 2f), "Unity aún no termina de cargar");
            Assert.IsFalse(ReglaInicio.PuedeActivar(0.9f, 0.5f, 5f, 2f), "La barra aún no se llena");
            Assert.IsFalse(ReglaInicio.PuedeActivar(0.9f, 1f, 1f, 2f), "Falta el tiempo mínimo");
            Assert.IsTrue(ReglaInicio.PuedeActivar(0.9f, 1f, 2f, 2f));
        }

        [Test]
        public void ProgresoSimulado_CreceSinPasarDe09()
        {
            float anterior = -1f;
            for (float t = 0f; t <= 60f; t += 1f)
            {
                float p = ReglaInicio.ProgresoSimulado(t, 10f);
                Assert.GreaterOrEqual(p, anterior);
                Assert.LessOrEqual(p, ReglaInicio.ProgresoAsincronoListo);
                anterior = p;
            }
            Assert.AreEqual(ReglaInicio.ProgresoAsincronoListo, ReglaInicio.ProgresoSimulado(1f, 0f), 1e-6f);
        }

        [Test]
        public void TextoPorcentaje_RedondeaYRecorta()
        {
            Assert.AreEqual("0%", ReglaInicio.TextoPorcentaje(-1f));
            Assert.AreEqual("46%", ReglaInicio.TextoPorcentaje(0.456f));
            Assert.AreEqual("100%", ReglaInicio.TextoPorcentaje(3f));
        }

        [Test]
        public void TextoConPuntos_CicloDeCuatroEstados()
        {
            Assert.AreEqual("CARGANDO", ReglaInicio.TextoConPuntos("CARGANDO", 0f, 0.5f));
            Assert.AreEqual("CARGANDO.", ReglaInicio.TextoConPuntos("CARGANDO", 0.6f, 0.5f));
            Assert.AreEqual("CARGANDO...", ReglaInicio.TextoConPuntos("CARGANDO", 1.6f, 0.5f));
            Assert.AreEqual("CARGANDO", ReglaInicio.TextoConPuntos("CARGANDO", 2.1f, 0.5f));
        }

        [Test]
        public void SiguienteIndice_DaLaVuelta()
        {
            Assert.AreEqual(1, ReglaInicio.SiguienteIndice(0, 3));
            Assert.AreEqual(0, ReglaInicio.SiguienteIndice(2, 3));
            Assert.AreEqual(0, ReglaInicio.SiguienteIndice(5, 0));
        }

        [Test]
        public void MoverIndice_ConVueltaYSinVuelta()
        {
            Assert.AreEqual(2, ReglaInicio.MoverIndice(0, -1, 3, true));
            Assert.AreEqual(0, ReglaInicio.MoverIndice(2, 1, 3, true));
            Assert.AreEqual(0, ReglaInicio.MoverIndice(0, -1, 3, false));
            Assert.AreEqual(2, ReglaInicio.MoverIndice(2, 1, 3, false));
            Assert.AreEqual(0, ReglaInicio.MoverIndice(0, 1, 0, true));
        }

        [Test]
        public void MoverValor_RecortaYNoAcumulaDecimales()
        {
            float v = 0f;
            for (int i = 0; i < 7; i++)
            {
                v = ReglaInicio.MoverValor(v, 1, 0.05f, 0f, 1f);
            }
            Assert.AreEqual(0.35f, v, 1e-6f);
            Assert.AreEqual(1f, ReglaInicio.MoverValor(0.95f, 5, 0.05f, 0f, 1f), 1e-6f);
            Assert.AreEqual(0.25f, ReglaInicio.MoverValor(0.3f, -4, 0.05f, 0.25f, 3f), 1e-6f);
        }

        [Test]
        public void ValorDeFraccion_AlineaAlPaso()
        {
            Assert.AreEqual(0.5f, ReglaInicio.ValorDeFraccion(0.5f, 0.05f, 0f, 1f), 1e-6f);
            Assert.AreEqual(0.55f, ReglaInicio.ValorDeFraccion(0.549f, 0.05f, 0f, 1f), 1e-6f);
            Assert.AreEqual(0f, ReglaInicio.ValorDeFraccion(-3f, 0.05f, 0f, 1f), 1e-6f);
            Assert.AreEqual(3f, ReglaInicio.ValorDeFraccion(9f, 0.05f, 0.25f, 3f), 1e-6f);
        }

        [Test]
        public void TextoResolucionYVolumen_DanFormato()
        {
            Assert.AreEqual("1920 x 1080", ReglaInicio.TextoResolucion(new Vector2Int(1920, 1080)));
            Assert.AreEqual("80%", ReglaInicio.TextoVolumen(0.8f));
        }

        [Test]
        public void ConsejosCarga_ObtenerDaLaVueltaYNuncaVacio()
        {
            Assert.IsNotEmpty(ConsejosCarga.Textos);
            foreach (string consejo in ConsejosCarga.Textos)
            {
                Assert.IsFalse(string.IsNullOrWhiteSpace(consejo));
            }
            Assert.AreEqual(ConsejosCarga.Textos[0], ConsejosCarga.Obtener(ConsejosCarga.Textos.Length));
            Assert.AreEqual(ConsejosCarga.Textos[ConsejosCarga.Textos.Length - 1], ConsejosCarga.Obtener(-1));
        }

        [Test]
        public void RelojGlitch_EsperaDespuesRafagaYVuelveAEsperar()
        {
            RelojGlitch reloj = new RelojGlitch(() => 0.5f, 2f, 2f, 0.4f);
            reloj.Actualizar(1f);
            Assert.IsFalse(reloj.EnRafaga, "Aún espera");
            Assert.AreEqual(0f, reloj.Intensidad);
            reloj.Actualizar(1.1f);
            Assert.IsTrue(reloj.EnRafaga, "Pasaron los 2 s: hay ráfaga");
            Assert.That(reloj.Intensidad, Is.InRange(0.35f, 1f));
            reloj.Actualizar(1f);
            Assert.IsFalse(reloj.EnRafaga, "La ráfaga termina");
            Assert.AreEqual(0f, reloj.Intensidad);
        }

        [Test]
        public void RelojGlitch_SinAzar_Lanza()
        {
            Assert.Throws<ArgumentNullException>(() => new RelojGlitch(null, 1f, 2f, 0.3f));
        }
    }
}
