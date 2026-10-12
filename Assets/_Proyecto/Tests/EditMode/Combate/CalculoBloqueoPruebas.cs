using MechaCyberpunk.Combate.Bloqueo;
using MechaCyberpunk.Core;
using NUnit.Framework;

namespace MechaCyberpunk.Tests.Combate
{
    public class CalculoBloqueoPruebas
    {
        private const float Danio = 10f;
        private const float Ventana = 0.2f;
        private const float Reduccion = 0.7f;

        private static (float Danio, TipoBloqueo Tipo) Calcular(bool arriba, float segundos)
            => CalculoBloqueo.Calcular(Danio, arriba, segundos, Ventana, Reduccion);

        [Test]
        public void GuardiaAbajo_DanioCompletoYNinguno()
        {
            var resultado = Calcular(false, 0f);
            Assert.AreEqual(Danio, resultado.Danio);
            Assert.AreEqual(TipoBloqueo.Ninguno, resultado.Tipo);
        }

        [Test]
        public void GuardiaSubiendo_DanioCompletoAunqueLosSegundosSeanAltos()
        {
            var resultado = Calcular(false, 5f);
            Assert.AreEqual(Danio, resultado.Danio);
            Assert.AreEqual(TipoBloqueo.Ninguno, resultado.Tipo);
        }

        [Test]
        public void DentroDeLaVentana_CeroYPerfecto()
        {
            var recien = Calcular(true, 0f);
            Assert.AreEqual(0f, recien.Danio);
            Assert.AreEqual(TipoBloqueo.Perfecto, recien.Tipo);

            var casi = Calcular(true, Ventana - 0.001f);
            Assert.AreEqual(0f, casi.Danio);
            Assert.AreEqual(TipoBloqueo.Perfecto, casi.Tipo);
        }

        [Test]
        public void FueraDeLaVentana_DanioReducidoYNormal()
        {
            var resultado = Calcular(true, Ventana + 0.001f);
            Assert.AreEqual(Danio * (1f - Reduccion), resultado.Danio, 1e-4f);
            Assert.AreEqual(TipoBloqueo.Normal, resultado.Tipo);

            var largo = Calcular(true, 10f);
            Assert.AreEqual(3f, largo.Danio, 1e-4f);
            Assert.AreEqual(TipoBloqueo.Normal, largo.Tipo);
        }

        [Test]
        public void BordeExactoDeLaVentana_SiempreDaElMismoResultado()
        {
            // Convención: con exactamente la ventana ya es Normal (Perfecto es estrictamente menor que la ventana).
            for (int i = 0; i < 3; i++)
            {
                var borde = Calcular(true, Ventana);
                Assert.AreEqual(TipoBloqueo.Normal, borde.Tipo);
                Assert.AreEqual(3f, borde.Danio, 1e-4f);
            }
        }

        [Test]
        public void ReduccionTotal_NoDejaPasarDanio()
        {
            var resultado = CalculoBloqueo.Calcular(Danio, true, 1f, Ventana, 1f);
            Assert.AreEqual(0f, resultado.Danio);
            Assert.AreEqual(TipoBloqueo.Normal, resultado.Tipo);
        }
    }
}
