using MechaCyberpunk.Progresion;
using NUnit.Framework;

namespace MechaCyberpunk.Tests.Progresion
{
    /// <summary>T-448: el precio de cada nivel de la guarida y el valor de cada efecto según el nivel.</summary>
    public class ReglaGuaridaPruebas
    {
        private static readonly float[] Factores = { 1f, 2f, 4f };
        private const float Tolerancia = 0.0001f;

        [Test]
        public void PrecioSiguiente_SubeConElNivelYAlMaximoEsCero()
        {
            Assert.AreEqual(500, ReglaGuarida.PrecioSiguiente(500, 0, Factores, 3));
            Assert.AreEqual(1000, ReglaGuarida.PrecioSiguiente(500, 1, Factores, 3));
            Assert.AreEqual(2000, ReglaGuarida.PrecioSiguiente(500, 2, Factores, 3));
            Assert.AreEqual(0, ReglaGuarida.PrecioSiguiente(500, 3, Factores, 3), "Al máximo ya no hay más");
        }

        [Test]
        public void PrecioSiguiente_ConDatosInvalidos_EsCero()
        {
            Assert.AreEqual(0, ReglaGuarida.PrecioSiguiente(0, 0, Factores, 3));
            Assert.AreEqual(0, ReglaGuarida.PrecioSiguiente(500, -1, Factores, 3));
            Assert.AreEqual(0, ReglaGuarida.PrecioSiguiente(500, 0, null, 3));
            Assert.AreEqual(0, ReglaGuarida.PrecioSiguiente(500, 0, new float[0], 3));
        }

        [Test]
        public void PrecioSiguiente_SiFaltanFactores_UsaElUltimo()
        {
            Assert.AreEqual(1000, ReglaGuarida.PrecioSiguiente(500, 2, new[] { 1f, 2f }, 3));
        }

        [Test]
        public void PorNivel_EnElNivelCeroDaElValorSinMejora_YDespuesLaTabla()
        {
            float[] tabla = { 0.08f, 0.16f, 0.25f };
            Assert.AreEqual(0f, ReglaGuarida.PorNivel(tabla, 0, 0f), Tolerancia);
            Assert.AreEqual(0.08f, ReglaGuarida.PorNivel(tabla, 1, 0f), Tolerancia);
            Assert.AreEqual(0.16f, ReglaGuarida.PorNivel(tabla, 2, 0f), Tolerancia);
            Assert.AreEqual(0.25f, ReglaGuarida.PorNivel(tabla, 3, 0f), Tolerancia);
            Assert.AreEqual(0.25f, ReglaGuarida.PorNivel(tabla, 9, 0f), Tolerancia, "Un nivel de más usa el último");
            Assert.AreEqual(1f, ReglaGuarida.PorNivel(null, 2, 1f), Tolerancia, "Sin tabla, el valor sin mejora");
        }

        [Test]
        public void RecortarNivel_DejaElNivelEnSuRango()
        {
            Assert.AreEqual(0, ReglaGuarida.RecortarNivel(-4, 3));
            Assert.AreEqual(2, ReglaGuarida.RecortarNivel(2, 3));
            Assert.AreEqual(3, ReglaGuarida.RecortarNivel(10, 3));
        }
    }
}
