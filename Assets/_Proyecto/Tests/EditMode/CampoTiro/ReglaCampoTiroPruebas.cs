using MechaCyberpunk.CampoTiro;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests
{
    public class ReglaCampoTiroPruebas
    {
        [Test]
        public void Posiciones_TresObjetivosMedia_CentradosYADistanciaDeLaBarrera()
        {
            Vector3[] posiciones = ReglaCampoTiro.Posiciones(3, DistanciaTiro.Media);
            Assert.AreEqual(3, posiciones.Length);
            Assert.AreEqual(-ReglaCampoTiro.SeparacionObjetivos, posiciones[0].x, 0.001f);
            Assert.AreEqual(0f, posiciones[1].x, 0.001f);
            Assert.AreEqual(ReglaCampoTiro.SeparacionObjetivos, posiciones[2].x, 0.001f);
            Assert.AreEqual(ReglaCampoTiro.ZBarrera + ReglaCampoTiro.Metros(DistanciaTiro.Media), posiciones[1].z, 0.001f);
        }

        [Test]
        public void Posiciones_CuantosFueraDeRango_SeLimita()
        {
            Assert.AreEqual(ReglaCampoTiro.MaximoObjetivos, ReglaCampoTiro.Posiciones(99, DistanciaTiro.Cerca).Length);
            Assert.AreEqual(ReglaCampoTiro.MinimoObjetivos, ReglaCampoTiro.Posiciones(0, DistanciaTiro.Cerca).Length);
        }

        [Test]
        public void Posiciones_DistanciasMasLejosEstanMasLejos_YDentroDeLaSala()
        {
            float cerca = ReglaCampoTiro.Metros(DistanciaTiro.Cerca);
            float media = ReglaCampoTiro.Metros(DistanciaTiro.Media);
            float lejos = ReglaCampoTiro.Metros(DistanciaTiro.Lejos);
            Assert.Less(cerca, media);
            Assert.Less(media, lejos);
            Assert.Less(ReglaCampoTiro.ZBarrera + lejos, ReglaCampoTiro.FondoAdelante);
        }

        [Test]
        public void DebeReaparecer_SoloCuandoPasaElTiempo()
        {
            Assert.IsFalse(ReglaCampoTiro.DebeReaparecer(ReglaCampoTiro.SegundosReaparicion - 0.1f));
            Assert.IsTrue(ReglaCampoTiro.DebeReaparecer(ReglaCampoTiro.SegundosReaparicion));
        }

        [Test]
        public void Precision_SinDisparos_EsCero_YConAciertosEsLaFraccion()
        {
            Assert.AreEqual(0f, ReglaCampoTiro.Precision(0, 0));
            Assert.AreEqual(0.6f, ReglaCampoTiro.Precision(3, 5), 0.001f);
            Assert.AreEqual(1f, ReglaCampoTiro.Precision(9, 5), 0.001f);
        }

        [Test]
        public void LimitarAlJugador_PasarLaBarrera_LoDejaAntesDeElla()
        {
            Vector3 limitado = ReglaCampoTiro.LimitarAlJugador(new Vector3(0f, 0f, ReglaCampoTiro.ZBarrera + 5f));
            Assert.Less(limitado.z, ReglaCampoTiro.ZBarrera);
            Assert.IsTrue(ReglaCampoTiro.EstaLimitado(new Vector3(0f, 0f, 0f)));
            Assert.IsFalse(ReglaCampoTiro.EstaLimitado(new Vector3(0f, 0f, ReglaCampoTiro.ZInicioJugador)));
        }

        [Test]
        public void LimitarAlJugador_ParedesLaterales_NoDejanSalir()
        {
            Vector3 limitado = ReglaCampoTiro.LimitarAlJugador(new Vector3(100f, 0f, ReglaCampoTiro.ZInicioJugador));
            Assert.Less(limitado.x, ReglaCampoTiro.MitadAncho);
        }

        [Test]
        public void Vida_TodosLosTipos_SonPositivos()
        {
            foreach (TipoObjetivoTiro tipo in System.Enum.GetValues(typeof(TipoObjetivoTiro)))
            {
                Assert.Greater(ReglaCampoTiro.Vida(tipo), 0f, tipo.ToString());
                Assert.IsNotEmpty(ReglaCampoTiro.Nombre(tipo));
            }
        }
    }
}
