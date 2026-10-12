using MechaCyberpunk.UI.Hud;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.UI
{
    public class CalculoFlechaObjetivoPruebas
    {
        private const float Tolerancia = 0.01f;

        [Test]
        public void Angulo_ObjetivoEnfrente_EsCero()
        {
            float angulo = CalculoFlechaObjetivo.Angulo(Vector3.zero, Vector3.forward, new Vector3(0f, 0f, 30f));
            Assert.AreEqual(0f, angulo, Tolerancia);
        }

        [Test]
        public void Angulo_ObjetivoALaDerecha_EsMasNoventa()
        {
            float angulo = CalculoFlechaObjetivo.Angulo(Vector3.zero, Vector3.forward, new Vector3(30f, 0f, 0f));
            Assert.AreEqual(90f, angulo, Tolerancia);
        }

        [Test]
        public void Angulo_ObjetivoALaIzquierda_EsMenosNoventa()
        {
            float angulo = CalculoFlechaObjetivo.Angulo(Vector3.zero, Vector3.forward, new Vector3(-30f, 0f, 0f));
            Assert.AreEqual(-90f, angulo, Tolerancia);
        }

        [Test]
        public void Angulo_ObjetivoAtras_EsCientoOchentaConCualquierSigno()
        {
            float angulo = CalculoFlechaObjetivo.Angulo(Vector3.zero, Vector3.forward, new Vector3(0f, 0f, -30f));
            Assert.AreEqual(180f, Mathf.Abs(angulo), Tolerancia);
        }

        [Test]
        public void Angulo_IgnoraLaAlturaDelObjetivoYLaInclinacionDeLaCamara()
        {
            Vector3 camara = new Vector3(0f, 1.65f, 0f);
            Vector3 mirandoAbajo = new Vector3(0f, -0.7f, 0.7f);
            float conAltura = CalculoFlechaObjetivo.Angulo(camara, mirandoAbajo, new Vector3(30f, 50f, 0f));
            float alRas = CalculoFlechaObjetivo.Angulo(camara, Vector3.forward, new Vector3(30f, 0f, 0f));
            Assert.AreEqual(90f, conAltura, Tolerancia);
            Assert.AreEqual(alRas, conAltura, Tolerancia);
        }

        [Test]
        public void Angulo_CamaraGirada_SeMideDesdeHaciaDondeMira()
        {
            // Mirando a +X, el objetivo en +Z queda a la izquierda.
            float angulo = CalculoFlechaObjetivo.Angulo(Vector3.zero, Vector3.right, new Vector3(0f, 0f, 30f));
            Assert.AreEqual(-90f, angulo, Tolerancia);
        }

        [Test]
        public void Angulo_ObjetivoJustoDebajo_NoDaNaN()
        {
            float angulo = CalculoFlechaObjetivo.Angulo(new Vector3(0f, 1.65f, 0f), Vector3.forward, Vector3.zero);
            Assert.AreEqual(0f, angulo, Tolerancia);
        }

        [Test]
        public void TextoDistancia_CuarentaYCincoMetros_Dice45m()
        {
            Assert.AreEqual("45 m", CalculoFlechaObjetivo.TextoDistancia(45f));
            Assert.AreEqual("45 m", CalculoFlechaObjetivo.TextoDistancia(44.6f));
        }

        [Test]
        public void TextoDistancia_MilDoscientos_Dice1punto2km()
        {
            Assert.AreEqual("1.2 km", CalculoFlechaObjetivo.TextoDistancia(1200f));
        }

        [Test]
        public void TextoDistancia_ExactamenteMil_YaEsKilometro()
        {
            Assert.AreEqual("1.0 km", CalculoFlechaObjetivo.TextoDistancia(1000f));
            Assert.AreEqual("999 m", CalculoFlechaObjetivo.TextoDistancia(999f));
        }

        [Test]
        public void TextoDistancia_CeroONegativo_Dice0m()
        {
            Assert.AreEqual("0 m", CalculoFlechaObjetivo.TextoDistancia(0f));
            Assert.AreEqual("0 m", CalculoFlechaObjetivo.TextoDistancia(-5f));
        }
    }
}
