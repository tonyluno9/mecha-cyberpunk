using MechaCyberpunk.Personaje.Habilidades;
using MechaCyberpunk.UI.Hud;
using NUnit.Framework;

namespace MechaCyberpunk.Tests.UI
{
    /// <summary>T-1302: las teclas de siempre de la barra de prótesis (Q piernas, C brazos, X cabeza; láser y misiles conservan 4 y 5) y su columna a la izquierda.</summary>
    public class BarraHabilidadesT1302Pruebas
    {
        [Test]
        public void TeclaPorOmision_QCX_YLasDeAtaqueConservanSusNumeros()
        {
            Assert.AreEqual("Q", BarraHabilidades.TeclaPorOmision(HabilidadProtesis.Piernas));
            Assert.AreEqual("C", BarraHabilidades.TeclaPorOmision(HabilidadProtesis.Brazos));
            Assert.AreEqual("X", BarraHabilidades.TeclaPorOmision(HabilidadProtesis.Cabeza));
            Assert.AreEqual("4", BarraHabilidades.TeclaPorOmision(HabilidadProtesis.Laser));
            Assert.AreEqual("5", BarraHabilidades.TeclaPorOmision(HabilidadProtesis.Misiles));
        }

        [Test]
        public void Disposicion_ColumnaIzquierda_CabenLasCincoCasillasSinSalirseDeLaPantalla()
        {
            float fin = BarraHabilidades.DesdeArriba + 5 * BarraHabilidades.Alto + 4 * BarraHabilidades.Hueco;
            Assert.Less(fin, 1080f - 232f - 40f, "Las cinco terminan arriba de la ayuda de controles de abajo a la izquierda");
            Assert.Greater(BarraHabilidades.DesdeArriba, 340f + 44f, "Empieza debajo de la renta, arriba a la izquierda");
            Assert.Less(BarraHabilidades.MargenIzquierdo + BarraHabilidades.Ancho, 540f, "Queda en el lateral, lejos del centro");
        }
    }
}
