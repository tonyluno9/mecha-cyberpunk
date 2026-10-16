using MechaCyberpunk.Personaje.Combate;
using NUnit.Framework;

namespace MechaCyberpunk.Tests.Personaje
{
    public class CargadorPistolaPruebas
    {
        private const int Tamano = 8;
        private const float Cadencia = 0.25f;
        private const float Recarga = 1.4f;

        private static CargadorPistola Nuevo() => new CargadorPistola(Tamano, Cadencia, Recarga);

        [Test]
        public void Disparar_OchoVeces_DisparaYElNovenoEsClicVacio()
        {
            CargadorPistola cargador = Nuevo();
            int disparos = 0;
            int vacios = 0;
            cargador.Disparo += () => disparos++;
            cargador.ClicVacio += () => vacios++;
            for (int i = 0; i < Tamano; i++)
            {
                Assert.IsTrue(cargador.IntentarDisparar(), "Disparo " + (i + 1));
                cargador.Avanzar(Cadencia);
            }
            Assert.IsFalse(cargador.IntentarDisparar());
            Assert.AreEqual(Tamano, disparos);
            Assert.AreEqual(1, vacios);
            Assert.AreEqual(0, cargador.Balas);
        }

        [Test]
        public void Disparar_AntesDelTiempoEntreDisparos_NoDispara()
        {
            CargadorPistola cargador = Nuevo();
            Assert.IsTrue(cargador.IntentarDisparar());
            cargador.Avanzar(Cadencia / 2f);
            Assert.IsFalse(cargador.IntentarDisparar());
            Assert.AreEqual(Tamano - 1, cargador.Balas);
            cargador.Avanzar(Cadencia / 2f);
            Assert.IsTrue(cargador.IntentarDisparar());
            Assert.AreEqual(Tamano - 2, cargador.Balas);
        }

        [Test]
        public void Recargar_TardaLoDeLosDatosYLlenaElCargador()
        {
            CargadorPistola cargador = Nuevo();
            int terminadas = 0;
            cargador.RecargaTerminada += () => terminadas++;
            cargador.IntentarDisparar();
            Assert.IsTrue(cargador.EmpezarRecarga());
            Assert.IsTrue(cargador.Recargando);
            cargador.Avanzar(Recarga - 0.05f);
            Assert.IsTrue(cargador.Recargando);
            Assert.AreEqual(Tamano - 1, cargador.Balas);
            cargador.Avanzar(0.05f);
            Assert.IsFalse(cargador.Recargando);
            Assert.AreEqual(Tamano, cargador.Balas);
            Assert.AreEqual(1, terminadas);
        }

        [Test]
        public void Disparar_Recargando_NoDisparaNiAvisaClicVacio()
        {
            CargadorPistola cargador = Nuevo();
            int vacios = 0;
            cargador.ClicVacio += () => vacios++;
            cargador.IntentarDisparar();
            cargador.Avanzar(Cadencia);
            cargador.EmpezarRecarga();
            Assert.IsFalse(cargador.IntentarDisparar());
            Assert.AreEqual(Tamano - 1, cargador.Balas);
            Assert.AreEqual(0, vacios);
        }

        [Test]
        public void Recargar_CargadorLleno_NoHaceNada()
        {
            CargadorPistola cargador = Nuevo();
            int iniciadas = 0;
            cargador.RecargaIniciada += () => iniciadas++;
            Assert.IsFalse(cargador.EmpezarRecarga());
            Assert.IsFalse(cargador.Recargando);
            Assert.AreEqual(0, iniciadas);
        }

        [Test]
        public void Recargar_YaRecargando_NoReinicia()
        {
            CargadorPistola cargador = Nuevo();
            cargador.IntentarDisparar();
            Assert.IsTrue(cargador.EmpezarRecarga());
            cargador.Avanzar(1f);
            Assert.IsFalse(cargador.EmpezarRecarga());
            cargador.Avanzar(Recarga - 1f);
            Assert.IsFalse(cargador.Recargando, "El segundo intento no reinició el tiempo");
        }

        [Test]
        public void Disparar_DespuesDeRecargar_VuelveAHaberOchoDisparos()
        {
            CargadorPistola cargador = Nuevo();
            for (int i = 0; i < Tamano; i++)
            {
                cargador.IntentarDisparar();
                cargador.Avanzar(Cadencia);
            }
            cargador.EmpezarRecarga();
            cargador.Avanzar(Recarga);
            int disparos = 0;
            for (int i = 0; i < Tamano; i++)
            {
                if (cargador.IntentarDisparar())
                {
                    disparos++;
                }
                cargador.Avanzar(Cadencia);
            }
            Assert.AreEqual(Tamano, disparos);
        }
    }
}
