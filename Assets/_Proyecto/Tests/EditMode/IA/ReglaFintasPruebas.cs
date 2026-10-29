using System.Collections.Generic;
using MechaCyberpunk.IA.Adaptacion;
using NUnit.Framework;

namespace MechaCyberpunk.Tests.IA
{
    /// <summary>T-202: la regla pura de las FINTAS del rival (azar inyectado).</summary>
    public class ReglaFintasPruebas
    {
        private static readonly float[] Extra = { 0.25f, 0.4f };
        private static readonly float[] Amago = { 0.2f, 0.35f };
        private const float Fraccion = 0.6f;
        private const float Espera = 0.3f;
        private const float Tolerancia = 1e-5f;

        private readonly Queue<float> valores = new Queue<float>();
        private float constante;
        private bool usarCola;

        private float Azar() => usarCola && valores.Count > 0 ? valores.Dequeue() : constante;

        private ReglaFintas Nueva() => new ReglaFintas(Extra, Amago, Fraccion, Espera, Azar);

        [SetUp]
        public void SetUp()
        {
            valores.Clear();
            constante = 0f;
            usarCola = false;
        }

        [Test]
        public void NivelCero_SinExtraYNuncaAmaga()
        {
            ReglaFintas regla = Nueva();
            for (int i = 0; i < 50; i++)
            {
                constante = i % 2;
                Assert.IsFalse(regla.Empezar(0, out float extra));
                Assert.AreEqual(0f, extra);
            }
        }

        [Test]
        public void NivelUno_AzarUno_ExtraEsElMaximoYNoAmaga()
        {
            constante = 1f;
            Assert.IsFalse(Nueva().Empezar(1, out float extra));
            Assert.AreEqual(0.25f, extra, Tolerancia);
        }

        [Test]
        public void NivelDos_AzarUno_ExtraEsElMaximoDelNivelDos()
        {
            constante = 1f;
            Assert.IsFalse(Nueva().Empezar(2, out float extra));
            Assert.AreEqual(0.4f, extra, Tolerancia);
        }

        [Test]
        public void Extra_EsAzarPorElMaximo_YNuncaSalePorDebajoDeCeroNiPorEncimaDelMaximo()
        {
            ReglaFintas regla = Nueva();
            // El segundo valor (0.99) hace que nunca amague y se vea el extra.
            usarCola = true;
            foreach (float azar in new[] { 0f, 0.3f, 0.5f, 1f, 2f, -1f })
            {
                valores.Enqueue(azar);
                valores.Enqueue(0.99f);
                regla.Empezar(1, out float extra);
                Assert.GreaterOrEqual(extra, 0f);
                Assert.LessOrEqual(extra, 0.25f + Tolerancia);
            }
            valores.Enqueue(0.5f);
            valores.Enqueue(0.99f);
            regla.Empezar(1, out float medio);
            Assert.AreEqual(0.125f, medio, Tolerancia);
        }

        [Test]
        public void AzarCero_Amaga_ConExtraCero()
        {
            constante = 0f;
            Assert.IsTrue(Nueva().Empezar(1, out float extra));
            Assert.AreEqual(0f, extra, "El amago se ve como un golpe de serie hasta que se cancela");
        }

        [Test]
        public void AzarJustoEnLaProbabilidad_NoAmaga()
        {
            usarCola = true;
            valores.Enqueue(0.5f);
            valores.Enqueue(0.2f);
            Assert.IsFalse(Nueva().Empezar(1, out _), "Amaga solo si el azar es menor que la probabilidad");
        }

        [Test]
        public void NuncaDosAmagosSeguidos_AunConAzarSiempreCero()
        {
            ReglaFintas regla = Nueva();
            constante = 0f;
            bool anterior = false;
            for (int i = 0; i < 20; i++)
            {
                bool amago = regla.Empezar(1, out _);
                Assert.IsFalse(amago && anterior, "Dos amagos seguidos en el golpe " + i);
                anterior = amago;
            }
        }

        [Test]
        public void ElGolpeQueSigueAUnAmago_EsRealYSuAvisoNoBajaDeLaSerie()
        {
            ReglaFintas regla = Nueva();
            usarCola = true;
            valores.Enqueue(0f);
            valores.Enqueue(0f);
            Assert.IsTrue(regla.Empezar(1, out _));
            valores.Enqueue(0.4f);
            valores.Enqueue(0f);
            Assert.IsFalse(regla.Empezar(1, out float extra), "Tras un amago el golpe es real");
            Assert.GreaterOrEqual(extra, 0f, "El extra nunca es negativo: el aviso no baja de la preparación de serie");
            Assert.AreEqual(0.1f, extra, Tolerancia);
        }

        [Test]
        public void TrasUnGolpeReal_VuelveAPoderAmagar()
        {
            ReglaFintas regla = Nueva();
            constante = 0f;
            Assert.IsTrue(regla.Empezar(1, out _));
            Assert.IsFalse(regla.Empezar(1, out _));
            Assert.IsTrue(regla.Empezar(1, out _));
        }

        [Test]
        public void Reiniciar_OlvidaElAmagoAnterior()
        {
            ReglaFintas regla = Nueva();
            constante = 0f;
            Assert.IsTrue(regla.Empezar(1, out _));
            regla.Reiniciar();
            Assert.IsTrue(regla.Empezar(1, out _));
        }

        [Test]
        public void NivelDos_UsaLaProbabilidadDelNivelDos()
        {
            usarCola = true;
            valores.Enqueue(0.5f);
            valores.Enqueue(0.3f);
            Assert.IsTrue(Nueva().Empezar(2, out _), "0.3 es menor que 0.35");
            valores.Enqueue(0.5f);
            valores.Enqueue(0.3f);
            Assert.IsFalse(Nueva().Empezar(1, out _), "0.3 no es menor que 0.2");
        }

        [Test]
        public void SegundosDeAmago_EsLaFraccionDeLaPreparacionDeSerie()
        {
            Assert.AreEqual(0.36f, Nueva().SegundosDeAmago(0.6f), Tolerancia);
            Assert.AreEqual(Espera, Nueva().EsperaTrasAmago, Tolerancia);
        }
    }
}
