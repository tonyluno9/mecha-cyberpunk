using MechaCyberpunk.Personaje.Habilidades;
using MechaCyberpunk.UI.Hud;
using NUnit.Framework;

namespace MechaCyberpunk.Tests.UI
{
    /// <summary>T-1302: las teclas de siempre de la barra de prótesis (T-1329: sin tecla por habilidad) y su columna a la izquierda.</summary>
    public class BarraHabilidadesT1302Pruebas
    {
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
