using MechaCyberpunk.Trafico.Persecucion;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests
{
    /// <summary>T-1492: dónde se pone el marcador del dron que dispara (sobre él, o en el borde con flecha).</summary>
    public class MarcadorDronT1492Pruebas
    {
        private const float Ancho = 1920f;
        private const float Alto = 1080f;

        [Test]
        public void Colocar_DronEnPantalla_MarcaSobreEl()
        {
            Vector2 p = CalculoMarcadorDron.Colocar(new Vector3(0.5f, 0.5f, 10f), Ancho, Alto, out bool fuera, out _);
            Assert.IsFalse(fuera);
            Assert.AreEqual(960f, p.x, 0.01f);
            Assert.AreEqual(540f, p.y, 0.01f);
        }

        [Test]
        public void Colocar_ArribaALaDerecha_YEstaArribaEnPantalla()
        {
            Vector2 p = CalculoMarcadorDron.Colocar(new Vector3(0.75f, 0.9f, 10f), Ancho, Alto, out bool fuera, out _);
            Assert.IsFalse(fuera);
            Assert.AreEqual(1440f, p.x, 0.01f);
            Assert.AreEqual(108f, p.y, 0.01f, "La Y de pantalla crece hacia abajo");
        }

        [Test]
        public void Colocar_FueraALaDerecha_VaEnElBordeDerechoApuntandoALaDerecha()
        {
            Vector2 p = CalculoMarcadorDron.Colocar(new Vector3(1.6f, 0.5f, 10f), Ancho, Alto, out bool fuera, out float grados);
            Assert.IsTrue(fuera);
            Assert.AreEqual(Ancho - CalculoMarcadorDron.MargenBorde, p.x, 0.01f);
            Assert.AreEqual(540f, p.y, 0.01f);
            Assert.AreEqual(90f, grados, 0.01f);
        }

        [Test]
        public void Colocar_FueraArriba_ApuntaArriba()
        {
            Vector2 p = CalculoMarcadorDron.Colocar(new Vector3(0.5f, 1.8f, 10f), Ancho, Alto, out bool fuera, out float grados);
            Assert.IsTrue(fuera);
            Assert.AreEqual(CalculoMarcadorDron.MargenBorde, p.y, 0.01f);
            Assert.AreEqual(0f, grados, 0.01f);
        }

        [Test]
        public void Colocar_DetrasDeLaCamara_ElMarcadorSeInvierteYApuntaAtras()
        {
            // Un dron justo detrás a la derecha se ve en el vista a la izquierda (x < 0.5) con z negativa: el marcador va a la derecha.
            Vector2 p = CalculoMarcadorDron.Colocar(new Vector3(0.3f, 0.5f, -5f), Ancho, Alto, out bool fuera, out float grados);
            Assert.IsTrue(fuera, "Detrás nunca está en pantalla");
            Assert.Greater(p.x, Ancho * 0.5f);
            Assert.AreEqual(90f, grados, 0.01f);
        }

        [Test]
        public void Colocar_JustoDetras_SePoneAbajo()
        {
            Vector2 p = CalculoMarcadorDron.Colocar(new Vector3(0.5f, 0.5f, -5f), Ancho, Alto, out bool fuera, out float grados);
            Assert.IsTrue(fuera);
            Assert.Greater(p.y, Alto * 0.5f);
            Assert.AreEqual(180f, Mathf.Abs(grados), 0.01f);
        }

        [Test]
        public void Colocar_FueraEnDiagonal_QuedaDentroDelMargen()
        {
            Vector2 p = CalculoMarcadorDron.Colocar(new Vector3(3f, 3f, 10f), Ancho, Alto, out bool fuera, out _);
            Assert.IsTrue(fuera);
            Assert.LessOrEqual(p.x, Ancho - CalculoMarcadorDron.MargenBorde + 0.01f);
            Assert.GreaterOrEqual(p.y, CalculoMarcadorDron.MargenBorde - 0.01f);
        }

        [Test]
        public void Amenazando_AvisandoODespuesDeDispararPoco()
        {
            Assert.IsTrue(CalculoMarcadorDron.Amenazando(true, 10f, -1f, 1.5f), "Avisando con la luz roja");
            Assert.IsTrue(CalculoMarcadorDron.Amenazando(false, 10f, 9f, 1.5f), "Disparó hace 1 s");
            Assert.IsFalse(CalculoMarcadorDron.Amenazando(false, 10f, 8f, 1.5f), "Disparó hace 2 s");
            Assert.IsFalse(CalculoMarcadorDron.Amenazando(false, 10f, -1f, 1.5f), "Nunca ha disparado");
        }
    }
}
