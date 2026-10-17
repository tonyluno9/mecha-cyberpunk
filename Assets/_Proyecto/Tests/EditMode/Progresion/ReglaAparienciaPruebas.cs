using MechaCyberpunk.Core;
using MechaCyberpunk.Progresion;
using NUnit.Framework;

namespace MechaCyberpunk.Tests.Progresion
{
    public class ReglaAparienciaPruebas
    {
        [Test]
        public void Normalizar_ConIndicesNegativos_LosPasaACero()
        {
            AparienciaJugador r = ReglaApariencia.Normalizar(new AparienciaJugador(-1, -5, -2));
            Assert.AreEqual(new AparienciaJugador(0, 0, 0), r);
        }

        [Test]
        public void Normalizar_ConSoloUnoNegativo_ConservaLosDemas()
        {
            AparienciaJugador r = ReglaApariencia.Normalizar(new AparienciaJugador(2, -1, 3));
            Assert.AreEqual(new AparienciaJugador(2, 0, 3), r);
        }

        [Test]
        public void Normalizar_ConIndicesValidosOGrandes_NoLosCambia()
        {
            AparienciaJugador r = ReglaApariencia.Normalizar(new AparienciaJugador(0, 7, 3));
            Assert.AreEqual(new AparienciaJugador(0, 7, 3), r);
        }
    }
}
