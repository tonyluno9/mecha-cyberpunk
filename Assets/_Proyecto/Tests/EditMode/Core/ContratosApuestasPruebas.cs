using System;
using MechaCyberpunk.Core;
using NUnit.Framework;

namespace MechaCyberpunk.Tests.Core
{
    public class ContratosApuestasPruebas
    {
        private class CarteraFalsa : ICartera
        {
            public int Dinero { get; private set; }
            public MotivoDinero UltimoMotivo = MotivoDinero.Otro;
            public event Action<int, MotivoDinero> Cambio;
            public CarteraFalsa(int dinero) { Dinero = dinero; }
            public bool PuedePagar(int cantidad) => cantidad > 0 && Dinero >= cantidad;
            public bool IntentarPagar(int cantidad, MotivoDinero motivo)
            {
                if (!PuedePagar(cantidad)) return false;
                Dinero -= cantidad; UltimoMotivo = motivo; Cambio?.Invoke(-cantidad, motivo); return true;
            }
            public void Abonar(int cantidad, MotivoDinero motivo)
            {
                if (cantidad <= 0) return;
                Dinero += cantidad; UltimoMotivo = motivo; Cambio?.Invoke(cantidad, motivo);
            }
            public void CargarDeuda(int cantidad, MotivoDinero motivo)
            {
                if (cantidad <= 0) return;
                Dinero -= cantidad; UltimoMotivo = motivo; Cambio?.Invoke(-cantidad, motivo);
            }
        }

        // Doble que cumple los /// de IApuestasPelea; sirve de ejemplo para el doble del casino.
        private class ApuestasFalsas : IApuestasPelea
        {
            private readonly ICartera cartera;
            public bool Abierta { get; private set; }
            public string Rival { get; private set; } = "";
            public float Momio { get; private set; } = 1f;
            public int ApuestaMaxima { get; private set; }
            public int Apostado { get; private set; }
            public event Action Cambio;
            public event Action<int> Resuelta;

            public ApuestasFalsas(ICartera cartera) { this.cartera = cartera; }

            public void Abrir(string rival, float momio, int maxima)
            {
                Abierta = true; Rival = rival; Momio = momio; ApuestaMaxima = maxima; Apostado = 0;
                Cambio?.Invoke();
            }

            public void Resolver(bool gano)
            {
                if (!Abierta) return;
                int apostado = Apostado;
                int ganado = (int)(apostado * Momio);
                if (gano && apostado > 0) cartera.Abonar(apostado + ganado, MotivoDinero.Apuesta);
                Abierta = false; Rival = ""; Apostado = 0;
                Cambio?.Invoke();
                if (apostado > 0) Resuelta?.Invoke(gano ? ganado : -apostado);
            }

            public bool PuedeApostar(int monto) =>
                Abierta && Apostado == 0 && monto > 0 && monto <= ApuestaMaxima && cartera.PuedePagar(monto);

            public bool Apostar(int monto)
            {
                if (!PuedeApostar(monto) || !cartera.IntentarPagar(monto, MotivoDinero.Apuesta)) return false;
                Apostado = monto; Cambio?.Invoke(); return true;
            }

            public bool Cancelar()
            {
                if (!Abierta || Apostado == 0) return false;
                cartera.Abonar(Apostado, MotivoDinero.Apuesta);
                Apostado = 0; Cambio?.Invoke(); return true;
            }
        }

        private CarteraFalsa cartera;
        private ApuestasFalsas apuestas;

        [SetUp]
        public void SetUp()
        {
            cartera = new CarteraFalsa(1000);
            apuestas = new ApuestasFalsas(cartera);
        }

        [Test]
        public void Apostar_SinPeleaAbierta_FallaYNoCobra()
        {
            Assert.IsFalse(apuestas.PuedeApostar(100));
            Assert.IsFalse(apuestas.Apostar(100));
            Assert.AreEqual(1000, cartera.Dinero);
            Assert.AreEqual(0, apuestas.Apostado);
        }

        [Test]
        public void Apostar_Abierta_CobraConMotivoApuestaYGuardaApostado()
        {
            apuestas.Abrir("Cuervo", 1f, 500);
            Assert.IsTrue(apuestas.Apostar(250));
            Assert.AreEqual(750, cartera.Dinero);
            Assert.AreEqual(MotivoDinero.Apuesta, cartera.UltimoMotivo);
            Assert.AreEqual(250, apuestas.Apostado);
        }

        [Test]
        public void Apostar_DosVecesEnLaMismaPelea_LaSegundaFalla()
        {
            apuestas.Abrir("Cuervo", 1f, 500);
            Assert.IsTrue(apuestas.Apostar(100));
            Assert.IsFalse(apuestas.PuedeApostar(100));
            Assert.IsFalse(apuestas.Apostar(100));
            Assert.AreEqual(900, cartera.Dinero);
            Assert.AreEqual(100, apuestas.Apostado);
        }

        [Test]
        public void Apostar_MontoCeroNegativoOMayorAlTope_Falla()
        {
            apuestas.Abrir("Cuervo", 1f, 500);
            Assert.IsFalse(apuestas.Apostar(0));
            Assert.IsFalse(apuestas.Apostar(-5));
            Assert.IsFalse(apuestas.Apostar(501));
            Assert.AreEqual(1000, cartera.Dinero);
            Assert.IsTrue(apuestas.Apostar(500));
        }

        [Test]
        public void Apostar_CarteraNoAlcanza_FallaSinCobrar()
        {
            apuestas.Abrir("Cuervo", 1f, 5000);
            Assert.IsFalse(apuestas.Apostar(1001));
            Assert.AreEqual(1000, cartera.Dinero);
        }

        [Test]
        public void Cancelar_ConApuesta_DevuelveElDineroConMotivoApuesta()
        {
            apuestas.Abrir("Cuervo", 1f, 500);
            apuestas.Apostar(300);
            cartera.UltimoMotivo = MotivoDinero.Otro;
            Assert.IsTrue(apuestas.Cancelar());
            Assert.AreEqual(1000, cartera.Dinero);
            Assert.AreEqual(MotivoDinero.Apuesta, cartera.UltimoMotivo);
            Assert.AreEqual(0, apuestas.Apostado);
            Assert.IsFalse(apuestas.Cancelar());
        }

        [Test]
        public void Cancelar_SinPeleaAbierta_Falla()
        {
            apuestas.Abrir("Cuervo", 1f, 500);
            apuestas.Apostar(300);
            apuestas.Resolver(true);
            Assert.IsFalse(apuestas.Cancelar());
        }

        [Test]
        public void Resuelta_GanaYPierde_AvisaElCambioNeto()
        {
            int neto = 0;
            int avisos = 0;
            apuestas.Resuelta += n => { neto = n; avisos++; };

            apuestas.Abrir("Cuervo", 1f, 500);
            apuestas.Apostar(250);
            apuestas.Resolver(true);
            Assert.AreEqual(250, neto);
            Assert.AreEqual(1250, cartera.Dinero);

            apuestas.Abrir("Toro", 1f, 500);
            apuestas.Apostar(250);
            apuestas.Resolver(false);
            Assert.AreEqual(-250, neto);
            Assert.AreEqual(1000, cartera.Dinero);
            Assert.AreEqual(2, avisos);
        }

        [Test]
        public void Resuelta_SinApuesta_NoAvisa()
        {
            int avisos = 0;
            apuestas.Resuelta += _ => avisos++;
            apuestas.Abrir("Cuervo", 1f, 500);
            apuestas.Resolver(true);
            Assert.AreEqual(0, avisos);
        }

        [Test]
        public void Cambio_AlAbrirApostarYCancelar_Avisa()
        {
            int cambios = 0;
            apuestas.Cambio += () => cambios++;
            apuestas.Abrir("Cuervo", 1f, 500);
            apuestas.Apostar(100);
            apuestas.Cancelar();
            Assert.AreEqual(3, cambios);
        }
    }
}
