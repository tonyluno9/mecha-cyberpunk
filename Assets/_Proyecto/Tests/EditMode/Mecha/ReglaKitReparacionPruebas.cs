using MechaCyberpunk.Mecha.Kits;
using NUnit.Framework;

namespace MechaCyberpunk.Tests.Mecha
{
    public class ReglaKitReparacionPruebas
    {
        private const float Maxima = 100f;
        private const float Fraccion = 0.2f;
        private const int Tope = 2;
        private const float Margen = 0.001f;

        private static MotivoKit Decidir(bool enPelea = true, bool enKO = false, float vida = 50f, int kits = 3, int usados = 0, bool curando = false)
        {
            return ReglaKitReparacion.Decidir(enPelea, enKO, vida, Maxima, kits, usados, Tope, curando);
        }

        [Test]
        public void Decidir_EnPeleaConVidaBajaYKits_Puede()
        {
            Assert.AreEqual(MotivoKit.Puede, Decidir());
        }

        [Test]
        public void Decidir_FueraDePelea_NoPuede()
        {
            Assert.AreEqual(MotivoKit.FueraDePelea, Decidir(enPelea: false));
        }

        [Test]
        public void Decidir_EnKO_NoPuede()
        {
            Assert.AreEqual(MotivoKit.EnKO, Decidir(enKO: true, vida: 0f));
        }

        [Test]
        public void Decidir_ConVidaCompleta_NoPuede()
        {
            Assert.AreEqual(MotivoKit.VidaCompleta, Decidir(vida: Maxima));
        }

        [Test]
        public void Decidir_SinKits_NoPuede()
        {
            Assert.AreEqual(MotivoKit.SinKits, Decidir(kits: 0));
        }

        [Test]
        public void Decidir_AlTopePorPelea_NoPuede()
        {
            Assert.AreEqual(MotivoKit.Puede, Decidir(usados: Tope - 1));
            Assert.AreEqual(MotivoKit.LimitePorPelea, Decidir(usados: Tope));
        }

        [Test]
        public void Decidir_ConOtroKitEnCurso_NoPuede()
        {
            Assert.AreEqual(MotivoKit.EnCurso, Decidir(curando: true));
        }

        [Test]
        public void Curacion_ConVida50_CuraElVeintePorCiento()
        {
            Assert.AreEqual(20f, ReglaKitReparacion.Curacion(50f, Maxima, Fraccion), Margen);
        }

        [Test]
        public void Curacion_ConVida95_CuraSoloLoQueFalta()
        {
            Assert.AreEqual(5f, ReglaKitReparacion.Curacion(95f, Maxima, Fraccion), Margen);
        }

        [Test]
        public void Curacion_ConVidaCompleta_NoCura()
        {
            Assert.AreEqual(0f, ReglaKitReparacion.Curacion(Maxima, Maxima, Fraccion), Margen);
        }

        [Test]
        public void CuracionDelCuadro_RepartePorTiempoYNoPasaDeLoRestante()
        {
            Assert.AreEqual(2f, ReglaKitReparacion.CuracionDelCuadro(20f, 20f, 1.5f, 0.15f), Margen);
            Assert.AreEqual(1f, ReglaKitReparacion.CuracionDelCuadro(20f, 1f, 1.5f, 0.15f), Margen);
        }

        [Test]
        public void CuracionDelCuadro_SumaElTotalEnLosSegundosPedidos()
        {
            float restante = 20f;
            float acumulado = 0f;
            const float dt = 0.1f;
            for (int i = 0; i < 15; i++)
            {
                float paso = ReglaKitReparacion.CuracionDelCuadro(20f, restante, 1.5f, dt);
                restante -= paso;
                acumulado += paso;
            }
            Assert.AreEqual(20f, acumulado, 0.01f);
        }
    }
}
