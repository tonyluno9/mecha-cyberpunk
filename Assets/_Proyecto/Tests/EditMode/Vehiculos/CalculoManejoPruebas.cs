using MechaCyberpunk.Vehiculos.Carro;
using NUnit.Framework;

namespace MechaCyberpunk.Tests.Vehiculos
{
    public class CalculoManejoPruebas
    {
        private const float Dt = 0.02f;
        private ParametrosManejo p;

        [SetUp]
        public void SetUp()
        {
            p = new ParametrosManejo
            {
                Masa = 1100f,
                VelocidadMaxima = 16f,
                VelocidadReversa = 5f,
                UmbralReversa = 0.5f,
                FuerzaMotor = 6200f,
                FuerzaReversa = 3500f,
                FuerzaFreno = 9000f,
                FuerzaFrenoMano = 6000f,
                ResistenciaRodadura = 500f,
                AnguloDireccionParado = 32f,
                AnguloDireccionMaximo = 10f,
                ExponenteDireccion = 0.5f,
                DistanciaEntreEjes = 2.7f,
                AceleracionLateralMaxima = 9f,
                AgarreLateral = 8f,
                FactorAgarreFrenoMano = 0.25f,
                GananciaEstacionamiento = 40f,
            };
        }

        [Test]
        public void FuerzaLongitudinal_AcelerarParado_EmpujaHaciaAdelante()
        {
            Assert.AreEqual(p.FuerzaMotor, CalculoManejo.FuerzaLongitudinal(p, 1f, 0f, 0f, 0f, Dt), 0.01f);
        }

        [Test]
        public void FuerzaLongitudinal_VelocidadMaxima_MotorNoEmpuja()
        {
            Assert.AreEqual(0f, CalculoManejo.FuerzaLongitudinal(p, 1f, 0f, 0f, 16f, Dt), 0.01f);
            Assert.AreEqual(0f, CalculoManejo.FuerzaLongitudinal(p, 1f, 0f, 0f, 20f, Dt), 0.01f);
        }

        [Test]
        public void FuerzaLongitudinal_MasRapidoMenosEmpuje()
        {
            float lento = CalculoManejo.FuerzaLongitudinal(p, 1f, 0f, 0f, 4f, Dt);
            float rapido = CalculoManejo.FuerzaLongitudinal(p, 1f, 0f, 0f, 12f, Dt);
            Assert.Greater(lento, rapido);
            Assert.Greater(rapido, 0f);
        }

        [Test]
        public void FuerzaLongitudinal_FrenarConMarchaAdelante_FrenaYNoDaReversa()
        {
            float fuerza = CalculoManejo.FuerzaLongitudinal(p, 0f, 1f, 0f, 10f, Dt);
            Assert.AreEqual(-p.FuerzaFreno, fuerza, 0.01f);
        }

        [Test]
        public void FuerzaLongitudinal_FrenarCasiDetenido_DaReversa()
        {
            float fuerza = CalculoManejo.FuerzaLongitudinal(p, 0f, 1f, 0f, 0.3f, Dt);
            Assert.Less(fuerza, 0f);
            Assert.AreEqual(-p.FuerzaReversa, fuerza, p.FuerzaReversa * 0.01f);
        }

        [Test]
        public void FuerzaLongitudinal_ReversaHastaLaVelocidadDeReversa_Corta()
        {
            Assert.Less(CalculoManejo.FuerzaLongitudinal(p, 0f, 1f, 0f, -2f, Dt), 0f);
            Assert.AreEqual(0f, CalculoManejo.FuerzaLongitudinal(p, 0f, 1f, 0f, -5f, Dt), 0.01f);
            Assert.AreEqual(0f, CalculoManejo.FuerzaLongitudinal(p, 0f, 1f, 0f, -7f, Dt), 0.01f);
        }

        [Test]
        public void FuerzaLongitudinal_AcelerarEnReversa_Frena()
        {
            Assert.Greater(CalculoManejo.FuerzaLongitudinal(p, 1f, 0f, 0f, -3f, Dt), 0f);
        }

        [Test]
        public void FuerzaLongitudinal_FrenoNoInvierteLaVelocidadEnUnPaso()
        {
            // A 0.6 m/s el freno de 9000 N cambiaría la velocidad más de lo que falta para parar: la fuerza se limita.
            float fuerza = CalculoManejo.FuerzaLongitudinal(p, 0f, 1f, 1f, 0.6f, Dt);
            float cambio = fuerza / p.Masa * Dt;
            Assert.GreaterOrEqual(0.6f + cambio, 0f);
        }

        [Test]
        public void FuerzaLongitudinal_FrenoDeMano_SumaFrenado()
        {
            float sin = CalculoManejo.FuerzaLongitudinal(p, 0f, 0f, 0f, 10f, Dt);
            float con = CalculoManejo.FuerzaLongitudinal(p, 0f, 0f, 1f, 10f, Dt);
            Assert.Less(con, sin);
        }

        [Test]
        public void AnguloDireccion_VelocidadMaximaEsMenorQueParado()
        {
            float parado = CalculoManejo.AnguloDireccion(p, 0f);
            float maxima = CalculoManejo.AnguloDireccion(p, p.VelocidadMaxima);
            Assert.AreEqual(32f, parado, 0.001f);
            Assert.AreEqual(10f, maxima, 0.001f);
            Assert.Less(maxima, parado);
            Assert.Less(CalculoManejo.AnguloDireccion(p, 12f), CalculoManejo.AnguloDireccion(p, 4f));
        }

        [Test]
        public void VelocidadGiro_Parado_EsCero()
        {
            Assert.AreEqual(0f, CalculoManejo.VelocidadGiro(p, 0f, 1f));
        }

        [Test]
        public void VelocidadGiro_DerechaPositivaYEnReversaInvertida()
        {
            Assert.Greater(CalculoManejo.VelocidadGiro(p, 8f, 1f), 0f);
            Assert.Less(CalculoManejo.VelocidadGiro(p, 8f, -1f), 0f);
            Assert.Less(CalculoManejo.VelocidadGiro(p, -3f, 1f), 0f);
        }

        [Test]
        public void VelocidadGiro_NoPideMasAceleracionLateralQueElAgarre()
        {
            float v = 16f;
            float lateral = CalculoManejo.VelocidadGiro(p, v, 1f) * v;
            Assert.LessOrEqual(lateral, p.AceleracionLateralMaxima + 0.001f);
        }

        [Test]
        public void FuerzaLateral_CancelaElDerrape_YElFrenoDeManoBajaElAgarre()
        {
            float normal = CalculoManejo.FuerzaLateral(p, 1f, 0f, Dt);
            float conFreno = CalculoManejo.FuerzaLateral(p, 1f, 1f, Dt);
            Assert.Less(normal, 0f);
            Assert.Less(conFreno, 0f);
            Assert.Less(System.Math.Abs(conFreno), System.Math.Abs(normal));
            Assert.Greater(CalculoManejo.FuerzaLateral(p, -1f, 0f, Dt), 0f);
        }

        [Test]
        public void FuerzaLateral_NuncaPasaDelAgarreMaximo()
        {
            float fuerza = CalculoManejo.FuerzaLateral(p, 30f, 0f, Dt);
            Assert.AreEqual(-p.Masa * p.AceleracionLateralMaxima, fuerza, 0.01f);
        }

        [Test]
        public void FuerzaEstacionamiento_DetieneElCarro()
        {
            float v = 8f;
            for (int i = 0; i < 100; i++)
            {
                v += CalculoManejo.FuerzaEstacionamiento(p, v, Dt) / p.Masa * Dt;
            }
            Assert.Less(System.Math.Abs(v), 0.01f);
        }

        [Test]
        public void FuerzaEstacionamiento_NuncaInvierteLaVelocidadEnUnPaso()
        {
            float v = 3f;
            float despues = v + CalculoManejo.FuerzaEstacionamiento(p, v, Dt) / p.Masa * Dt;
            Assert.GreaterOrEqual(despues, 0f);
        }
    }
}
