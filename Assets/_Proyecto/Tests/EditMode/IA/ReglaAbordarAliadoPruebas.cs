using MechaCyberpunk.IA.Aliados;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.IA
{
    /// <summary>T-706: reglas puras de cuándo y por dónde el aliado se sube a tu carro.</summary>
    public class ReglaAbordarAliadoPruebas
    {
        [Test]
        public void PuedeSubir_SoloSiSigueNoEsMotoHayLugarVaCercaYElCarroEstaQuieto()
        {
            Assert.IsTrue(ReglaAbordarAliado.PuedeSubir(true, false, true, 5f, 0f));
            Assert.IsFalse(ReglaAbordarAliado.PuedeSubir(false, false, true, 5f, 0f), "Esperando no sube");
            Assert.IsFalse(ReglaAbordarAliado.PuedeSubir(true, true, true, 5f, 0f), "En la moto no sube");
            Assert.IsFalse(ReglaAbordarAliado.PuedeSubir(true, false, false, 5f, 0f), "Sin lugar no sube");
            Assert.IsFalse(ReglaAbordarAliado.PuedeSubir(true, false, true, ReglaAbordarAliado.DistanciaMaxima + 1f, 0f), "Lejos no sube");
            Assert.IsFalse(ReglaAbordarAliado.PuedeSubir(true, false, true, 5f, ReglaAbordarAliado.RapidezMaximaParaSubir + 1f), "Con el carro en marcha no sube");
        }

        [Test]
        public void DebeRendirse_SiElCarroArrancaOTardaDemasiado()
        {
            Assert.IsFalse(ReglaAbordarAliado.DebeRendirse(0f, 1f));
            Assert.IsTrue(ReglaAbordarAliado.DebeRendirse(ReglaAbordarAliado.RapidezParaRendirse + 1f, 1f));
            Assert.IsTrue(ReglaAbordarAliado.DebeRendirse(0f, ReglaAbordarAliado.SegundosMaximosAcercando + 0.1f));
            Assert.Less(ReglaAbordarAliado.RapidezMaximaParaSubir, ReglaAbordarAliado.RapidezParaRendirse);
        }

        [Test]
        public void PuertaCopiloto_EsElEspejoDeLaDelConductorRespectoDelCarro()
        {
            GameObject marco = new GameObject("Marco");
            try
            {
                marco.transform.SetPositionAndRotation(new Vector3(10f, 0f, 5f), Quaternion.Euler(0f, 90f, 0f));
                Vector3 conductor = marco.transform.TransformPoint(new Vector3(-1.2f, 0f, 0.3f));
                Vector3 copiloto = ReglaAbordarAliado.PuertaCopiloto(conductor, marco.transform);
                Vector3 local = marco.transform.InverseTransformPoint(copiloto);
                Assert.AreEqual(1.2f, local.x, 0.001f, "Del otro lado");
                Assert.AreEqual(0.3f, local.z, 0.001f, "A la misma altura del carro");
                Vector3 asiento = marco.transform.TransformPoint(new Vector3(0.6f, 0.9f, 0.1f));
                Vector3 pies = ReglaAbordarAliado.AsientoCopiloto(marco.transform.TransformPoint(new Vector3(-0.6f, 0.9f, 0.1f)), marco.transform);
                Assert.AreEqual(asiento.x, pies.x, 0.001f);
                Assert.AreEqual(asiento.y - ReglaAbordarAliado.CaidaAlAsiento, pies.y, 0.001f, "Los pies van más abajo del asiento");
            }
            finally
            {
                Object.DestroyImmediate(marco);
            }
        }

        [Test]
        public void PuntoPuertaYSalida_UnoPegadoAlCarroYElOtroAfuera()
        {
            Vector3 asiento = new Vector3(0.6f, 0f, 0f);
            Vector3 puerta = new Vector3(2f, 0f, 0f);
            Vector3 pegado = ReglaAbordarAliado.PuntoPuerta(puerta, asiento);
            Assert.AreEqual(2f - ReglaAbordarAliado.RetiroPuerta, pegado.x, 0.001f, "Arrimado hacia el asiento");
            Vector3 salida = ReglaAbordarAliado.PuntoSalida(pegado, asiento);
            Assert.AreEqual(pegado.x + ReglaAbordarAliado.SalidaDePuerta, salida.x, 0.001f, "Sale hacia afuera");
        }
    }
}
