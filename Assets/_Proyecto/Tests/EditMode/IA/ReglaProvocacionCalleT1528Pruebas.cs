using MechaCyberpunk.IA.APie;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.IA
{
    /// <summary>T-1528: los maleantes de calle provocan primero y, al atacar, empiezan limitados y suben con el tiempo.</summary>
    public class ReglaProvocacionCalleT1528Pruebas
    {
        [Test]
        public void Escala_ArrancaLimitada_YLlegaCompleta()
        {
            Assert.AreEqual(ReglaProvocacionCalle.EscalaDanioInicial, ReglaProvocacionCalle.EscalaDanio(0f), 0.0001f);
            Assert.AreEqual(1f, ReglaProvocacionCalle.EscalaDanio(ReglaProvocacionCalle.SegundosRampa), 0.0001f);
            Assert.AreEqual(1f, ReglaProvocacionCalle.EscalaDanio(10000f), 0.0001f);
            Assert.Less(ReglaProvocacionCalle.EscalaDanio(10f), ReglaProvocacionCalle.EscalaDanio(60f));
            Assert.AreEqual(ReglaProvocacionCalle.EscalaPrecisionInicial, ReglaProvocacionCalle.EscalaPrecision(0f), 0.0001f);
            Assert.AreEqual(ReglaProvocacionCalle.RecargaExtraInicial, ReglaProvocacionCalle.RecargaExtra(0f), 0.0001f);
            Assert.AreEqual(0f, ReglaProvocacionCalle.RecargaExtra(ReglaProvocacionCalle.SegundosRampa), 0.0001f);
        }

        [Test]
        public void DanioInicial_NoMataDeUnTiro()
        {
            // Un disparo base de 8 puntos baja a menos de 3 al empezar; una racha tarda en quitar la vida de un jugador de 100.
            Assert.Less(8f * ReglaProvocacionCalle.EscalaDanio(0f), 3f);
        }

        [Test]
        public void Respuesta_SoloSiVieneDelJugadorYEstaCerca()
        {
            Vector3 jugador = Vector3.zero;
            Assert.IsTrue(ReglaProvocacionCalle.EsRespuesta(new Vector3(1f, 0f, 1f), jugador, new Vector3(10f, 0f, 0f)));
            Assert.IsFalse(ReglaProvocacionCalle.EsRespuesta(new Vector3(20f, 0f, 0f), jugador, new Vector3(10f, 0f, 0f)), "el disparo no vino del jugador");
            Assert.IsFalse(ReglaProvocacionCalle.EsRespuesta(Vector3.zero, jugador, new Vector3(80f, 0f, 0f)), "el maleante está muy lejos");
        }

        [Test]
        public void Empujon_EsCasiSinDanio()
        {
            Assert.LessOrEqual(ReglaProvocacionCalle.DanioEmpujon, 1f);
        }
    }
}
