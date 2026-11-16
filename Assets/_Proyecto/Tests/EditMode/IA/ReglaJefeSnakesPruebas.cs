using System;
using System.Collections.Generic;
using MechaCyberpunk.IA.APie;
using NUnit.Framework;

namespace MechaCyberpunk.Tests
{
    /// <summary>T-1108: el patrón del jefe SNAKES (fases, tiempos, alternancia, refuerzos al 50% y castigo solo tras la estocada).</summary>
    public class ReglaJefeSnakesPruebas
    {
        private const float Dt = 0.01f;
        private const float Tolerancia = 0.03f;

        private static ReglaJefeSnakes Crear(float azar) => new ReglaJefeSnakes(ReglaJefeSnakes.Parametros.Diseno, () => azar);

        private static ReglaJefeSnakes.Entrada En(float distancia, float vida = 1f, bool enPelea = true)
        {
            return new ReglaJefeSnakes.Entrada { Distancia = distancia, VidaFraccion = vida, EnPelea = enPelea };
        }

        [Test]
        public void Tajos_ConElJugadorEncima_AvisoTresTajosCadaCuartoDeSegundoYRecuperacion()
        {
            ReglaJefeSnakes r = Crear(0.1f); // planea tajos
            List<float> tajos = new List<float>();
            List<ReglaJefeSnakes.Fase> fases = new List<ReglaJefeSnakes.Fase> { r.Actual };
            float t = 0f;
            float inicioAviso = -1f;
            float finRecupera = -1f;
            while (t < 4f && finRecupera < 0f)
            {
                ReglaJefeSnakes.Salida s = r.Avanzar(Dt, En(2f));
                t += Dt;
                if (s.Tajo)
                {
                    tajos.Add(t);
                }
                if (r.Actual != fases[fases.Count - 1])
                {
                    fases.Add(r.Actual);
                    if (r.Actual == ReglaJefeSnakes.Fase.AvisoTajos)
                    {
                        inicioAviso = t;
                    }
                    if (tajos.Count == 3 && r.Actual == ReglaJefeSnakes.Fase.Acecho)
                    {
                        finRecupera = t;
                    }
                }
            }
            CollectionAssert.AreEqual(new[]
            {
                ReglaJefeSnakes.Fase.Acecho, ReglaJefeSnakes.Fase.AvisoTajos, ReglaJefeSnakes.Fase.Tajos, ReglaJefeSnakes.Fase.RecuperaTajos,
                ReglaJefeSnakes.Fase.Acecho
            }, fases, "Acecho, aviso, tajos, recuperación y vuelve al acecho");
            Assert.AreEqual(3, tajos.Count, "tres tajos");
            Assert.AreEqual(inicioAviso + 0.45f, tajos[0], Tolerancia, "el primer tajo cae al terminar el aviso de 0.45 s");
            Assert.AreEqual(0.25f, tajos[1] - tajos[0], Tolerancia, "un tajo cada 0.25 s");
            Assert.AreEqual(0.25f, tajos[2] - tajos[1], Tolerancia);
            Assert.AreEqual(0.9f, finRecupera - tajos[2], Tolerancia, "se recupera 0.9 s");
        }

        [Test]
        public void Estocada_DesdeSeisYMedioMetros_AvisoSeisDecimasEmbestidaMedioSegundoYRecuperacionCastigable()
        {
            ReglaJefeSnakes r = Crear(0.9f); // planea estocada
            int avisos = 0;
            int embestidas = 0;
            float t = 0f;
            float inicioAviso = -1f;
            float inicioEmbestida = -1f;
            float inicioRecupera = -1f;
            float finRecupera = -1f;
            ReglaJefeSnakes.Fase anterior = r.Actual;
            while (t < 5f && finRecupera < 0f)
            {
                ReglaJefeSnakes.Salida s = r.Avanzar(Dt, En(6.5f));
                t += Dt;
                avisos += s.EmpezoAvisoEstocada ? 1 : 0;
                embestidas += s.EmpezoEstocada ? 1 : 0;
                Assert.AreEqual(r.Actual == ReglaJefeSnakes.Fase.RecuperaEstocada, r.Castigable, "solo se castiga en la recuperación de la estocada");
                if (r.Actual != anterior)
                {
                    if (r.Actual == ReglaJefeSnakes.Fase.AvisoEstocada) inicioAviso = t;
                    if (r.Actual == ReglaJefeSnakes.Fase.Estocada) inicioEmbestida = t;
                    if (r.Actual == ReglaJefeSnakes.Fase.RecuperaEstocada) inicioRecupera = t;
                    if (r.Actual == ReglaJefeSnakes.Fase.Acecho && inicioRecupera > 0f) finRecupera = t;
                    anterior = r.Actual;
                }
            }
            Assert.AreEqual(1, avisos, "el aviso de la estocada se anuncia una sola vez");
            Assert.AreEqual(1, embestidas, "una sola embestida");
            Assert.AreEqual(0.6f, inicioEmbestida - inicioAviso, Tolerancia, "aviso de 0.6 s");
            Assert.AreEqual(0.5f, inicioRecupera - inicioEmbestida, Tolerancia, "embiste 0.5 s");
            Assert.AreEqual(1.4f, finRecupera - inicioRecupera, Tolerancia, "recuperación de 1.4 s");
            Assert.IsTrue(r.UltimaFueEstocada);
            Assert.AreEqual(ReglaJefeSnakes.Ataque.Tajos, r.Planeado, "después de una estocada nunca viene otra");
        }

        [Test]
        public void Estocada_FueraDeCincoASieteMetros_NoLaLanza()
        {
            foreach (float distancia in new[] { 3f, 4.9f, 7.1f, 20f })
            {
                ReglaJefeSnakes r = Crear(0.9f);
                for (int i = 0; i < 500; i++)
                {
                    r.Avanzar(Dt, En(distancia));
                    Assert.AreNotEqual(ReglaJefeSnakes.Fase.AvisoEstocada, r.Actual, "a " + distancia + " m no hay estocada");
                }
            }
        }

        [Test]
        public void Ataques_NuncaDosEstocadasSeguidas()
        {
            Random azar = new Random(7);
            ReglaJefeSnakes r = new ReglaJefeSnakes(ReglaJefeSnakes.Parametros.Diseno, () => (float)azar.NextDouble());
            List<ReglaJefeSnakes.Ataque> ataques = new List<ReglaJefeSnakes.Ataque>();
            ReglaJefeSnakes.Fase anterior = r.Actual;
            for (int i = 0; i < 60000; i++)
            {
                // El jugador se queda donde el jefe lo necesita: encima para tajos, a seis metros y medio para la estocada.
                float distancia = r.Planeado == ReglaJefeSnakes.Ataque.Tajos ? 2f : 6.5f;
                r.Avanzar(Dt, En(distancia));
                if (r.Actual != anterior)
                {
                    if (r.Actual == ReglaJefeSnakes.Fase.AvisoTajos) ataques.Add(ReglaJefeSnakes.Ataque.Tajos);
                    if (r.Actual == ReglaJefeSnakes.Fase.AvisoEstocada) ataques.Add(ReglaJefeSnakes.Ataque.Estocada);
                    anterior = r.Actual;
                }
            }
            Assert.Greater(ataques.Count, 30, "hizo muchos ataques");
            Assert.Greater(ataques.FindAll(a => a == ReglaJefeSnakes.Ataque.Estocada).Count, 5, "y varias estocadas");
            for (int i = 1; i < ataques.Count; i++)
            {
                Assert.IsFalse(ataques[i] == ReglaJefeSnakes.Ataque.Estocada && ataques[i - 1] == ReglaJefeSnakes.Ataque.Estocada, "dos estocadas seguidas en el ataque " + i);
            }
        }

        [Test]
        public void Refuerzos_AlCincuentaPorCiento_UnaVezYAvisosMasCortos()
        {
            ReglaJefeSnakes r = Crear(0.1f);
            Assert.IsFalse(r.Avanzar(Dt, En(8f, 0.51f)).LlamarRefuerzos, "con más del 50% no los llama");
            Assert.AreEqual(1f, r.FactorAviso, 0.0001f);
            Assert.AreEqual(0.45f, r.DuracionAvisoTajos, 0.0001f);
            Assert.IsTrue(r.Avanzar(Dt, En(8f, 0.5f)).LlamarRefuerzos, "al 50% los llama");
            Assert.IsTrue(r.RefuerzosLlamados);
            Assert.IsFalse(r.Avanzar(Dt, En(8f, 0.3f)).LlamarRefuerzos, "solo una vez");
            Assert.IsFalse(r.Avanzar(Dt, En(8f, 0.1f)).LlamarRefuerzos, "solo una vez");
            Assert.AreEqual(0.75f, r.FactorAviso, 0.0001f);
            Assert.AreEqual(0.45f * 0.75f, r.DuracionAvisoTajos, 0.0001f, "aviso de tajos 25% más corto");
            Assert.AreEqual(0.6f * 0.75f, r.DuracionAvisoEstocada, 0.0001f, "aviso de estocada 25% más corto");
        }

        [Test]
        public void Acecho_CambiaDeSentidoCadaUnoYMedioADosYMedioSegundos()
        {
            ReglaJefeSnakes r = Crear(0.1f); // sentido inicial +1, plan de tajos, cambia cada 1.6 s
            Assert.AreEqual(1, r.Sentido);
            for (float t = 0f; t < 1.5f; t += Dt) r.Avanzar(Dt, En(8f));
            Assert.AreEqual(1, r.Sentido, "antes de 1.5 s no cambia");
            for (float t = 0f; t < 0.2f; t += Dt) r.Avanzar(Dt, En(8f));
            Assert.AreEqual(-1, r.Sentido, "cambió pasado el primer intervalo");
        }

        [Test]
        public void SinPelea_VuelveAlAcechoYNoAtaca()
        {
            ReglaJefeSnakes r = Crear(0.1f);
            for (int i = 0; i < 70; i++) r.Avanzar(Dt, En(2f)); // entra al aviso de tajos
            Assert.AreEqual(ReglaJefeSnakes.Fase.AvisoTajos, r.Actual);
            ReglaJefeSnakes.Salida s = r.Avanzar(Dt, En(2f, 1f, false));
            Assert.AreEqual(ReglaJefeSnakes.Fase.Acecho, r.Actual, "el jugador se fue: cancela");
            Assert.IsFalse(s.Tajo);
            Assert.AreEqual(0f, r.ProgresoAviso);
        }

        [Test]
        public void ProgresoAviso_SubeDeCeroAUnoDuranteElAviso()
        {
            ReglaJefeSnakes r = Crear(0.1f);
            while (r.Actual != ReglaJefeSnakes.Fase.AvisoTajos) r.Avanzar(Dt, En(2f));
            for (int i = 0; i < 22; i++) r.Avanzar(Dt, En(2f));
            Assert.AreEqual(0.5f, r.ProgresoAviso, 0.1f, "a la mitad del aviso");
        }
    }
}
