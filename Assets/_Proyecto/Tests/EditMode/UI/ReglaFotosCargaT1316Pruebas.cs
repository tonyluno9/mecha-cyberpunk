using MechaCyberpunk.UI.Inicio;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.UI
{
    /// <summary>T-1316: las fotos de la pantalla de carga se turnan con fundido, crecen despacio y el paneo nunca destapa la orilla.</summary>
    public class ReglaFotosCargaT1316Pruebas
    {
        private const int Fotos = 6;

        [Test]
        public void AlEmpezar_SeVeLaPrimeraSinFundido()
        {
            ReglaFotosCarga.Estado e = ReglaFotosCarga.Calcular(0f, Fotos, 3);
            Assert.AreEqual(3, e.Actual);
            Assert.AreEqual(-1, e.Anterior);
            Assert.AreEqual(1f, e.AlfaActual);
        }

        [Test]
        public void AlCambiar_LaSiguienteEntraConFundidoSobreLaAnterior()
        {
            float t = ReglaFotosCarga.SegundosPorFoto + ReglaFotosCarga.SegundosFundido * 0.5f;
            ReglaFotosCarga.Estado e = ReglaFotosCarga.Calcular(t, Fotos, 5);
            Assert.AreEqual(0, e.Actual, "después de la última sigue la primera");
            Assert.AreEqual(5, e.Anterior);
            Assert.Greater(e.AlfaActual, 0.2f);
            Assert.Less(e.AlfaActual, 0.8f);
            Assert.Greater(e.TiempoAnterior, ReglaFotosCarga.SegundosPorFoto, "la de abajo sigue su movimiento");
            ReglaFotosCarga.Estado fin = ReglaFotosCarga.Calcular(ReglaFotosCarga.SegundosPorFoto + ReglaFotosCarga.SegundosFundido + 0.1f, Fotos, 5);
            Assert.AreEqual(-1, fin.Anterior, "terminado el fundido ya solo hay una");
            Assert.AreEqual(1f, fin.AlfaActual);
        }

        [Test]
        public void T1330_SinZoomNiPaneo_LaFotoNoSeRecorta([Values(0, 1, 2, 3, 4, 5)] int indice)
        {
            for (float t = 0f; t <= ReglaFotosCarga.SegundosPorFoto + ReglaFotosCarga.SegundosFundido; t += 0.25f)
            {
                Assert.AreEqual(1f, ReglaFotosCarga.Escala(t), 1e-4f, "escala a los " + t + " s");
                Assert.AreEqual(Vector2.zero, ReglaFotosCarga.Paneo(indice, t), "paneo a los " + t + " s");
            }
        }
    }
}
