using MechaCyberpunk.UI.Rueda;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.UI
{
    /// <summary>T-991: la lógica pura de la rueda de armas y objetos (ángulo a casilla, zona muerta, disponibilidad y resultado al soltar).</summary>
    public class ReglaRuedaArmasPruebas
    {
        private static readonly float[] Angulos = { 288f, 324f, 0f, 36f, 72f, 108f, 144f, 180f, 216f, 252f };

        private static Vector2 Cursor(float grados, float radio)
        {
            float a = grados * Mathf.Deg2Rad;
            return new Vector2(Mathf.Sin(a), Mathf.Cos(a)) * radio;
        }

        [Test]
        public void AnguloDeCasilla_SonLosDiezDelDiseno()
        {
            for (int i = 0; i < Angulos.Length; i++)
            {
                Assert.AreEqual(Angulos[i], ReglaRuedaArmas.AnguloDeCasilla(i), 0.001f, "casilla " + i);
            }
        }

        [Test]
        public void CasillaEnAngulo_CadaAnguloDelDisenoDaSuCasilla()
        {
            for (int i = 0; i < Angulos.Length; i++)
            {
                Assert.AreEqual(i, ReglaRuedaArmas.CasillaEnAngulo(Angulos[i]), "ángulo " + Angulos[i]);
                Assert.AreEqual(i, ReglaRuedaArmas.CasillaEnAngulo(Angulos[i] + 17f), "17° después de " + Angulos[i]);
                Assert.AreEqual(i, ReglaRuedaArmas.CasillaEnAngulo(Angulos[i] - 17f), "17° antes de " + Angulos[i]);
            }
        }

        [Test]
        public void CasillaEnAngulo_DaLaVueltaEnElCero()
        {
            Assert.AreEqual(2, ReglaRuedaArmas.CasillaEnAngulo(359f), "Escopeta (arriba)");
            Assert.AreEqual(2, ReglaRuedaArmas.CasillaEnAngulo(1f));
            Assert.AreEqual(2, ReglaRuedaArmas.CasillaEnAngulo(-5f));
            Assert.AreEqual(0, ReglaRuedaArmas.CasillaEnAngulo(288f + 360f));
        }

        [Test]
        public void AnguloDeCursor_ArribaEsCeroYSigueElReloj()
        {
            Assert.AreEqual(0f, ReglaRuedaArmas.AnguloDeCursor(new Vector2(0f, 1f)), 0.001f);
            Assert.AreEqual(90f, ReglaRuedaArmas.AnguloDeCursor(new Vector2(1f, 0f)), 0.001f);
            Assert.AreEqual(180f, ReglaRuedaArmas.AnguloDeCursor(new Vector2(0f, -1f)), 0.001f);
            Assert.AreEqual(270f, ReglaRuedaArmas.AnguloDeCursor(new Vector2(-1f, 0f)), 0.001f);
        }

        [Test]
        public void CasillaResaltada_ElCursorSobreCadaCasillaLaResalta()
        {
            for (int i = 0; i < Angulos.Length; i++)
            {
                Assert.AreEqual(i, ReglaRuedaArmas.CasillaResaltada(Cursor(Angulos[i], 100f), null), "casilla " + i);
            }
        }

        [Test]
        public void CasillaResaltada_DentroDeLaZonaMuertaNoResaltaNada()
        {
            Assert.AreEqual(-1, ReglaRuedaArmas.CasillaResaltada(Vector2.zero, null));
            Assert.AreEqual(-1, ReglaRuedaArmas.CasillaResaltada(Cursor(324f, ReglaRuedaArmas.ZonaMuerta - 1f), null));
            Assert.AreEqual(1, ReglaRuedaArmas.CasillaResaltada(Cursor(324f, ReglaRuedaArmas.ZonaMuerta + 1f), null));
        }

        [Test]
        public void CasillaResaltada_UnaCasillaNoDisponibleNoSeElige()
        {
            System.Func<int, bool> sinPistola = c => c != 1;
            Assert.AreEqual(-1, ReglaRuedaArmas.CasillaResaltada(Cursor(324f, 120f), sinPistola));
            Assert.AreEqual(2, ReglaRuedaArmas.CasillaResaltada(Cursor(0f, 120f), sinPistola));
        }

        [Test]
        public void MoverCursor_SeQuedaDentroDelRadioMaximo()
        {
            Vector2 c = ReglaRuedaArmas.MoverCursor(Vector2.zero, new Vector2(1000f, 0f));
            Assert.AreEqual(ReglaRuedaArmas.RadioMaximo, c.magnitude, 0.001f);
            c = ReglaRuedaArmas.MoverCursor(new Vector2(50f, 0f), new Vector2(-20f, 10f));
            Assert.AreEqual(new Vector2(30f, 10f), c);
        }

        [Test]
        public void Resultado_ArmaObjetoONada()
        {
            SeleccionRueda pistola = ReglaRuedaArmas.Resultado(1);
            Assert.AreEqual(TipoSeleccionRueda.Arma, pistola.Tipo);
            Assert.AreEqual(1, pistola.Indice);
            SeleccionRueda venda = ReglaRuedaArmas.Resultado(7);
            Assert.AreEqual(TipoSeleccionRueda.Objeto, venda.Tipo);
            Assert.AreEqual(2, venda.Indice, "Venda es el tercer objeto del catálogo");
            Assert.AreEqual(TipoSeleccionRueda.Nada, ReglaRuedaArmas.Resultado(-1).Tipo);
            Assert.AreEqual(TipoSeleccionRueda.Nada, ReglaRuedaArmas.Resultado(10).Tipo);
        }

        [Test]
        public void ArmasArribaYObjetosAbajo()
        {
            for (int i = 0; i < Angulos.Length; i++)
            {
                bool arriba = Mathf.Cos(Angulos[i] * Mathf.Deg2Rad) > -0.01f;
                Assert.AreEqual(arriba, ReglaRuedaArmas.EsArma(i), "casilla " + i + " (" + Angulos[i] + "°)");
            }
        }
    }
}
