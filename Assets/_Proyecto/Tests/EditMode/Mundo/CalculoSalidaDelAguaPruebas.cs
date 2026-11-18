using MechaCyberpunk.Mundo.Ambiente;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>T-1358: lógica pura de la salida del agua.</summary>
    public class CalculoSalidaDelAguaPruebas
    {
        [Test]
        public void EstaEnAgua_BajoElNivel_EsVerdadero()
        {
            Assert.IsTrue(CalculoSalidaDelAgua.EstaEnAgua(-3f, -2.5f));
            Assert.IsFalse(CalculoSalidaDelAgua.EstaEnAgua(-2.2f, -2.5f));
        }

        [Test]
        public void Acumular_EnAguaSuma_FueraReinicia()
        {
            float t = CalculoSalidaDelAgua.Acumular(0f, true, 1.5f);
            t = CalculoSalidaDelAgua.Acumular(t, true, 1.5f);
            Assert.AreEqual(3f, t, 0.0001f);
            Assert.AreEqual(0f, CalculoSalidaDelAgua.Acumular(t, false, 0.5f));
        }

        [Test]
        public void PuntoMasCercano_ElijeElMasCercanoEnPlanta()
        {
            Vector3[] puntos = { new Vector3(0f, 0.3f, -215f), new Vector3(100f, 0.3f, -215f) };
            Assert.AreEqual(puntos[1], CalculoSalidaDelAgua.PuntoMasCercano(new Vector3(90f, -3f, -270f), puntos));
            Assert.AreEqual(puntos[0], CalculoSalidaDelAgua.PuntoMasCercano(new Vector3(10f, -3f, -260f), puntos));
        }

        [Test]
        public void PuntoMasCercano_SinPuntos_DevuelveLaPosicion()
        {
            Vector3 p = new Vector3(1f, -3f, 2f);
            Assert.AreEqual(p, CalculoSalidaDelAgua.PuntoMasCercano(p, new Vector3[0]));
        }
    }
}
