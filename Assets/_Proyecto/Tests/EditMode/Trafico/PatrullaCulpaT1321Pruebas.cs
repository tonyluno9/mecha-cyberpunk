using MechaCyberpunk.Trafico.Persecucion;
using NUnit.Framework;

namespace MechaCyberpunk.Tests.Trafico
{
    /// <summary>T-1321: el daño a una patrulla solo es delito del jugador si no es recién aparecida, ni choque con algo ajeno, y el jugador está cerca.</summary>
    public class PatrullaCulpaT1321Pruebas
    {
        [Test]
        public void CuentaComoDelito_RecienAparecida_NoCuenta()
        {
            Assert.IsFalse(ReglaCulpaPatrulla.CuentaComoDelito(1f, 100f));
        }

        [Test]
        public void CuentaComoDelito_ChoqueAjenoReciente_NoCuenta()
        {
            Assert.IsFalse(ReglaCulpaPatrulla.CuentaComoDelito(60f, 0.5f));
        }

        [Test]
        public void CuentaComoDelito_ViejaYSinChoqueAjeno_Cuenta()
        {
            Assert.IsTrue(ReglaCulpaPatrulla.CuentaComoDelito(60f, 100f));
        }

        [Test]
        public void CuentaComoDelito_JugadorLejos_NoCuenta()
        {
            Assert.IsFalse(ReglaCulpaPatrulla.CuentaComoDelito(60f, 100f, 120f, 80f));
            Assert.IsTrue(ReglaCulpaPatrulla.CuentaComoDelito(60f, 100f, 20f, 80f));
        }

        [Test]
        public void CuentaComoDelito_ChoqueAjenoYaPaso_VuelveAContar()
        {
            Assert.IsTrue(ReglaCulpaPatrulla.CuentaComoDelito(60f, ReglaCulpaPatrulla.SegundosVentanaAjena + 0.1f));
        }
    }
}
