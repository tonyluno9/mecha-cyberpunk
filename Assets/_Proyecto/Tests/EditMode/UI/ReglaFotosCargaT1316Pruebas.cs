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
        public void CadaFoto_SeAcercaDespacio()
        {
            Assert.AreEqual(ReglaFotosCarga.EscalaInicial, ReglaFotosCarga.Escala(0f), 1e-4f);
            Assert.Greater(ReglaFotosCarga.Escala(4f), ReglaFotosCarga.Escala(1f));
            Assert.LessOrEqual(ReglaFotosCarga.Escala(100f), ReglaFotosCarga.EscalaFinal + 1e-4f);
        }

        [Test]
        public void ElPaneo_NuncaDestapaLaOrilla([Values(0, 1, 2, 3, 4, 5)] int indice)
        {
            // Una foto de 1376x768 que cubre 1920x1080: margen por lado = (escala * tamaño - pantalla) / 2.
            float alto = 1080f, ancho = alto * 1376f / 768f;
            for (float t = 0f; t <= ReglaFotosCarga.SegundosPorFoto + ReglaFotosCarga.SegundosFundido; t += 0.25f)
            {
                float escala = ReglaFotosCarga.Escala(t);
                Vector2 p = ReglaFotosCarga.Paneo(indice, t);
                Assert.LessOrEqual(Mathf.Abs(p.x), (ancho * escala - 1920f) * 0.5f, "x a los " + t + " s");
                Assert.LessOrEqual(Mathf.Abs(p.y), (alto * escala - alto) * 0.5f, "y a los " + t + " s");
            }
            Assert.Greater((ReglaFotosCarga.Paneo(indice, 6f) - ReglaFotosCarga.Paneo(indice, 0f)).magnitude, 20f, "sí se mueve");
        }
    }
}
