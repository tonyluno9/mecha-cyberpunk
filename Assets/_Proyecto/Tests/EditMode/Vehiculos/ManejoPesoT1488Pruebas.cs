using MechaCyberpunk.Vehiculos;
using MechaCyberpunk.Vehiculos.Carro;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Tests.Vehiculos
{
    /// <summary>T-1488: peso, perfiles por tipo, derrape, suspensión que rebota, marchas, temblor e inclinación estable de la moto.</summary>
    public class ManejoPesoT1488Pruebas
    {
        private const float Dt = 0.02f;

        private static ParametrosManejo Parametros(TipoManejo tipo)
        {
            PerfilManejo f = PerfilManejo.Para(tipo);
            return new ParametrosManejo
            {
                Masa = f.Masa,
                VelocidadMaxima = f.VelocidadMaxima,
                VelocidadReversa = f.VelocidadReversa,
                UmbralReversa = 0.5f,
                FuerzaMotor = f.FuerzaMotor,
                FuerzaReversa = f.FuerzaReversa,
                FuerzaFreno = f.FuerzaFreno,
                FuerzaFrenoMano = f.FuerzaFrenoMano,
                ResistenciaRodadura = f.ResistenciaRodadura,
                AnguloDireccionParado = f.AnguloParado,
                AnguloDireccionMaximo = f.AnguloMaximo,
                ExponenteDireccion = f.ExponenteDireccion,
                DistanciaEntreEjes = f.DistanciaEntreEjes,
                AceleracionLateralMaxima = f.AceleracionLateral,
                AgarreLateral = f.AgarreLateral,
                FactorAgarreFrenoMano = 0.25f,
                GananciaEstacionamiento = 40f,
                GiroFrenoMano = f.GiroFrenoMano,
                GananciaContravolante = f.GananciaContravolante,
            };
        }

        // ---------- CalculoPeso ----------

        [Test]
        public void AlturaFuerza_FactorCeroVaAlCentroDeMasa_UnoAlPiso()
        {
            Assert.AreEqual(0.5f, CalculoPeso.AlturaFuerza(0f, 0.5f, 0f), 1e-5f);
            Assert.AreEqual(0f, CalculoPeso.AlturaFuerza(0f, 0.5f, 1f), 1e-5f);
            Assert.AreEqual(0.25f, CalculoPeso.AlturaFuerza(0f, 0.5f, 0.5f), 1e-5f);
        }

        [Test]
        public void TransferenciaCarga_CreceConLaAceleracionYLaAltura()
        {
            float baja = CalculoPeso.TransferenciaCarga(1100f, 10f, 0.4f, 2.7f);
            float alta = CalculoPeso.TransferenciaCarga(1100f, 10f, 0.8f, 2.7f);
            Assert.AreEqual(1100f * 10f * 0.4f / 2.7f, baja, 0.01f);
            Assert.AreEqual(baja * 2f, alta, 0.01f);
            Assert.AreEqual(0f, CalculoPeso.TransferenciaCarga(1100f, 10f, 0.4f, 0f));
        }

        [Test]
        public void CabeceoEstimado_SinFactor_NoCabecea()
        {
            Assert.AreEqual(0f, CalculoPeso.CabeceoEstimadoGrados(20000f, 0.5f, 0f, 18000f, 2.7f), 1e-5f);
            Assert.Greater(CalculoPeso.CabeceoEstimadoGrados(20000f, 0.5f, 0.9f, 18000f, 2.7f), 1f);
        }

        [Test]
        public void FactorAntivuelco_ParadoEsElExtra_ARapidezPlenaEsUno_NuncaBajaDeUno()
        {
            Assert.AreEqual(3f, CalculoPeso.FactorAntivuelco(0f, 3f), 1e-5f);
            Assert.AreEqual(1f, CalculoPeso.FactorAntivuelco(CalculoPeso.RapidezPlenaAntivuelco, 3f), 1e-5f);
            Assert.AreEqual(1f, CalculoPeso.FactorAntivuelco(20f, 3f), 1e-5f);
            Assert.AreEqual(1f, CalculoPeso.FactorAntivuelco(0f, 0.2f), 1e-5f);
        }

        [Test]
        public void TorqueAmortiguacion_SeOponeALaVelocidadAngular()
        {
            Assert.AreEqual(-200f, CalculoPeso.TorqueAmortiguacion(2f, 100f), 1e-4f);
            Assert.AreEqual(200f, CalculoPeso.TorqueAmortiguacion(-2f, 100f), 1e-4f);
            Assert.AreEqual(0f, CalculoPeso.TorqueAmortiguacion(2f, -5f), 1e-4f);
        }

        // ---------- Perfiles por tipo ----------

        [Test]
        public void Perfiles_AceleracionYFrenoSeSientenDistintosPorPeso()
        {
            PerfilManejo compacto = PerfilManejo.Para(TipoManejo.Compacto);
            PerfilManejo sedan = PerfilManejo.Para(TipoManejo.Sedan);
            PerfilManejo camioneta = PerfilManejo.Para(TipoManejo.Camioneta);
            PerfilManejo deportivo = PerfilManejo.Para(TipoManejo.Deportivo);
            PerfilManejo pesado = PerfilManejo.Para(TipoManejo.Pesado);
            Assert.Greater(deportivo.AceleracionMotor, sedan.AceleracionMotor);
            Assert.Greater(sedan.AceleracionMotor, camioneta.AceleracionMotor);
            Assert.Greater(camioneta.AceleracionMotor, pesado.AceleracionMotor);
            Assert.Greater(deportivo.VelocidadMaxima, sedan.VelocidadMaxima);
            Assert.Greater(sedan.VelocidadMaxima, compacto.VelocidadMaxima);
            Assert.Greater(deportivo.DesaceleracionFreno, pesado.DesaceleracionFreno);
            Assert.Greater(pesado.Masa, camioneta.Masa);
            Assert.Greater(camioneta.Masa, sedan.Masa);
            Assert.Greater(sedan.Masa, compacto.Masa);
        }

        [Test]
        public void Perfiles_DireccionRespondeMasEnLigerosQueEnPesados()
        {
            Assert.Greater(PerfilManejo.Para(TipoManejo.Deportivo).RespuestaGiro, PerfilManejo.Para(TipoManejo.Camioneta).RespuestaGiro);
            Assert.Greater(PerfilManejo.Para(TipoManejo.Camioneta).RespuestaGiro, PerfilManejo.Para(TipoManejo.Pesado).RespuestaGiro);
            Assert.Greater(PerfilManejo.Para(TipoManejo.Compacto).VelocidadVolante, PerfilManejo.Para(TipoManejo.Pesado).VelocidadVolante);
        }

        [Test]
        public void Perfiles_CabeceoAlFrenarEsVisibleSinVolcarse()
        {
            foreach (TipoManejo tipo in PerfilManejo.Tipos)
            {
                float grados = PerfilManejo.Para(tipo).CabeceoAlFrenarGrados;
                Assert.GreaterOrEqual(grados, 1f, tipo + " debería cabecear al menos 1°");
                Assert.LessOrEqual(grados, 6f, tipo + " no debería cabecear más de 6°");
            }
        }

        [Test]
        public void Perfiles_SuspensionRebotaPeroSeAsienta()
        {
            foreach (TipoManejo tipo in PerfilManejo.Tipos)
            {
                PerfilManejo f = PerfilManejo.Para(tipo);
                float zeta = CalculoSuspension.RelacionAmortiguamiento(f.Masa, f.Rigidez, f.Amortiguamiento);
                Assert.AreEqual(f.Zeta, zeta, 1e-3f, tipo.ToString());
                Assert.GreaterOrEqual(zeta, 0.3f, tipo + ": amortiguado de más, no rebota");
                Assert.LessOrEqual(zeta, 0.6f, tipo + ": amortiguado de menos, oscila mucho");
                // La rigidez deja el carro hundido lo que ya tienen las ruedas colocadas.
                Assert.AreEqual(PerfilManejo.CaidaReposo, f.Masa * 9.81f / (4f * f.Rigidez), 1e-3f, tipo.ToString());
            }
        }

        [Test]
        public void Perfiles_TodosLosCamposExistenEnConfigCarro()
        {
            ConfigCarro config = ScriptableObject.CreateInstance<ConfigCarro>();
            try
            {
                var so = new SerializedObject(config);
                foreach (TipoManejo tipo in PerfilManejo.Tipos)
                {
                    foreach (var par in PerfilManejo.Para(tipo).Campos())
                    {
                        Assert.IsNotNull(so.FindProperty(par.Key), "Falta el campo " + par.Key + " en ConfigCarro");
                    }
                }
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void Perfiles_ConAceleracionRealistaElMotorNoSuperaLaVelocidadMaxima()
        {
            foreach (TipoManejo tipo in PerfilManejo.Tipos)
            {
                ParametrosManejo p = Parametros(tipo);
                float v = 0f;
                for (int i = 0; i < 3000; i++)
                {
                    float f = CalculoManejo.FuerzaLongitudinal(p, 1f, 0f, 0f, v, Dt);
                    v += f / p.Masa * Dt;
                }
                Assert.LessOrEqual(v, p.VelocidadMaxima + 0.01f, tipo.ToString());
                Assert.Greater(v, p.VelocidadMaxima * 0.9f, tipo + " debería alcanzar casi su máxima en 60 s");
            }
        }

        // ---------- Derrape ----------

        [Test]
        public void VelocidadGiro_ConFrenoDeMano_GiraMasQueSinEl()
        {
            ParametrosManejo p = Parametros(TipoManejo.Deportivo);
            float sin = CalculoManejo.VelocidadGiro(p, 12f, 1f, 0f);
            float con = CalculoManejo.VelocidadGiro(p, 12f, 1f, 1f);
            Assert.AreEqual(CalculoManejo.VelocidadGiro(p, 12f, 1f), sin, 1e-5f);
            Assert.Greater(con, sin);
        }

        [Test]
        public void VelocidadGiro_SinGananciaDeFrenoDeMano_NoCambia()
        {
            ParametrosManejo p = Parametros(TipoManejo.Sedan);
            p.GiroFrenoMano = 0f;
            Assert.AreEqual(CalculoManejo.VelocidadGiro(p, 10f, 1f), CalculoManejo.VelocidadGiro(p, 10f, 1f, 1f), 1e-5f);
        }

        [Test]
        public void CorreccionDerrape_Derrapando_EnderezaHaciaDondeVa()
        {
            ParametrosManejo p = Parametros(TipoManejo.Sedan);
            // Velocidad hacia la derecha del frente: hay que girar a la derecha (positivo).
            Assert.Greater(CalculoManejo.CorreccionDerrape(p, 10f, 4f, 0f, 0f), 0f);
            Assert.Less(CalculoManejo.CorreccionDerrape(p, 10f, -4f, 0f, 0f), 0f);
        }

        [Test]
        public void CorreccionDerrape_AgarreNormalFrenoDeManoLentoOReversa_NoAyuda()
        {
            ParametrosManejo p = Parametros(TipoManejo.Sedan);
            Assert.AreEqual(0f, CalculoManejo.CorreccionDerrape(p, 10f, 0.3f, 0f, 0f), "zona muerta");
            Assert.AreEqual(0f, CalculoManejo.CorreccionDerrape(p, 10f, 4f, 0f, 1f), "con freno de mano el derrape es a propósito");
            Assert.AreEqual(0f, CalculoManejo.CorreccionDerrape(p, 2f, 1f, 0f, 0f), "muy lento");
            Assert.AreEqual(0f, CalculoManejo.CorreccionDerrape(p, -5f, 4f, 0f, 0f), "reversa");
            p.GananciaContravolante = 0f;
            Assert.AreEqual(0f, CalculoManejo.CorreccionDerrape(p, 10f, 4f, 0f, 0f), "ayuda apagada");
        }

        [Test]
        public void CorreccionDerrape_SiYaContravolantea_AyudaMenosYNuncaPasaDelTope()
        {
            ParametrosManejo p = Parametros(TipoManejo.Sedan);
            float sola = CalculoManejo.CorreccionDerrape(p, 10f, 4f, 0f, 0f);
            float contravolanteando = CalculoManejo.CorreccionDerrape(p, 10f, 4f, 1f, 0f);
            Assert.Less(contravolanteando, sola);
            Assert.LessOrEqual(CalculoManejo.CorreccionDerrape(p, 1f, 50f, 0f, 0f) + 0f, CalculoManejo.MaximaCorreccionDerrape + 1e-5f);
        }

        [Test]
        public void CorreccionDerrape_Recuperacion_ElDerrapeBajaAlSimularLaRotacion()
        {
            // El carro va a 12 m/s con 25° de desfase entre el frente y la velocidad; la ayuda lo va girando hacia la velocidad.
            ParametrosManejo p = Parametros(TipoManejo.Sedan);
            float rumbo = 0f;
            const float velocidadAngulo = 25f * Mathf.Deg2Rad;
            float rapidez = 12f;
            float direccionVelocidad = velocidadAngulo;
            for (int i = 0; i < 100; i++)
            {
                float beta = direccionVelocidad - rumbo;
                float vAdelante = rapidez * Mathf.Cos(beta);
                float vLateral = rapidez * Mathf.Sin(beta);
                rumbo += CalculoManejo.CorreccionDerrape(p, vAdelante, vLateral, 0f, 0f) * Dt;
            }
            Assert.Less(Mathf.Abs(direccionVelocidad - rumbo), 12f * Mathf.Deg2Rad, "en 2 s debe recuperar más de la mitad del desfase");
        }

        // ---------- Suspensión ----------

        [Test]
        public void Fuerza_ConRebote_EstirandoseAmortiguaMasQueComprimiendose()
        {
            float estirando = CalculoSuspension.Fuerza(0.1f, -0.5f, 18000f, 2000f, 1.5f);
            float sinRebote = CalculoSuspension.Fuerza(0.1f, -0.5f, 18000f, 2000f, 1f);
            Assert.Less(estirando, sinRebote);
            Assert.AreEqual(CalculoSuspension.Fuerza(0.1f, 1f, 18000f, 2000f), CalculoSuspension.Fuerza(0.1f, 1f, 18000f, 2000f, 1.5f), 1e-3f);
        }

        [Test]
        public void Suspension_TrasUnBache_RebotaYSeAsientaEnMenosDeDosSegundos()
        {
            foreach (TipoManejo tipo in PerfilManejo.Tipos)
            {
                PerfilManejo f = PerfilManejo.Para(tipo);
                float masaEsquina = f.Masa / 4f;
                float equilibrio = PerfilManejo.CaidaReposo;
                float compresion = equilibrio + 0.08f; // empujada de más por un bache
                float velocidad = 0f;                  // positiva comprimiendo
                bool cruzo = false;
                float ultimoFuera = 0f;
                const float paso = 0.002f;
                for (float t = 0f; t < 4f; t += paso)
                {
                    float fuerza = CalculoSuspension.Fuerza(compresion, velocidad, f.Rigidez, f.Amortiguamiento, f.FactorRebote);
                    float a = (masaEsquina * 9.81f - fuerza) / masaEsquina; // positiva: el peso comprime
                    velocidad += a * paso;
                    compresion += velocidad * paso;
                    if (compresion < equilibrio)
                    {
                        cruzo = true;
                    }
                    if (Mathf.Abs(compresion - equilibrio) > 0.005f)
                    {
                        ultimoFuera = t;
                    }
                }
                Assert.IsTrue(cruzo, tipo + " debería rebotar pasando del equilibrio");
                Assert.Less(ultimoFuera, 2f, tipo + " tarda demasiado en asentarse");
            }
        }

        // ---------- Marchas del sonido ----------

        [Test]
        public void SimuladorMarchas_ParadoEnPrimeraConRpmMinimas()
        {
            var m = new SimuladorMarchas(5);
            Assert.AreEqual(SimuladorMarchas.RpmMinima, m.Actualizar(0f, 20f, Dt), 1e-5f);
            Assert.AreEqual(1, m.Marcha);
        }

        [Test]
        public void SimuladorMarchas_AceleraSubiendoMarchasYLasRpmCaenAlCambiar()
        {
            var m = new SimuladorMarchas(5);
            float rpmAntes = 0f;
            bool vioCaida = false;
            int marchaAnterior = 1;
            for (float v = 0f; v <= 20f; v += 0.05f)
            {
                float rpm = m.Actualizar(v, 20f, 0.02f);
                if (m.Marcha > marchaAnterior)
                {
                    Assert.Less(rpm, rpmAntes - 0.05f, "al subir de marcha las rpm deben caer");
                    vioCaida = true;
                }
                marchaAnterior = m.Marcha;
                rpmAntes = rpm;
            }
            Assert.IsTrue(vioCaida);
            Assert.AreEqual(5, m.Marcha);
        }

        [Test]
        public void SimuladorMarchas_ConHisteresis_NoCambiaDeIdaYVueltaEnElBorde()
        {
            var m = new SimuladorMarchas(5);
            float borde = 20f / 5f * SimuladorMarchas.FraccionSubir;
            m.Actualizar(borde + 0.1f, 20f, Dt);
            int marcha = m.Marcha;
            Assert.AreEqual(2, marcha);
            for (int i = 0; i < 20; i++)
            {
                m.Actualizar(borde + (i % 2 == 0 ? -0.2f : 0.2f), 20f, Dt);
                Assert.AreEqual(marcha, m.Marcha);
            }
        }

        [Test]
        public void SimuladorMarchas_Frenando_BajaDeMarcha()
        {
            var m = new SimuladorMarchas(5);
            m.Actualizar(20f, 20f, Dt);
            Assert.AreEqual(5, m.Marcha);
            m.Actualizar(1f, 20f, Dt);
            Assert.Less(m.Marcha, 5);
        }

        // ---------- Temblor y cámara ----------

        [Test]
        public void Temblor_ProporcionalAlImpactoConUmbralYTope()
        {
            Assert.AreEqual(0f, CalculoTemblor.Fuerza(2f, 3f, 18f, 1.5f));
            Assert.AreEqual(0f, CalculoTemblor.Fuerza(3f, 3f, 18f, 1.5f));
            float medio = CalculoTemblor.Fuerza(10.5f, 3f, 18f, 1.5f);
            Assert.AreEqual(0.75f, medio, 1e-5f);
            Assert.AreEqual(1.5f, CalculoTemblor.Fuerza(40f, 3f, 18f, 1.5f), 1e-5f);
            Assert.Greater(CalculoTemblor.Fuerza(15f, 3f, 18f, 1.5f), CalculoTemblor.Fuerza(6f, 3f, 18f, 1.5f));
        }

        [Test]
        public void Fov_SeAbreConLaRapidezConTope()
        {
            Assert.AreEqual(60f, CalculoCamaraCarro.FovPorRapidez(60f, 12f, 0f, 100f), 1e-5f);
            Assert.AreEqual(66f, CalculoCamaraCarro.FovPorRapidez(60f, 12f, 50f, 100f), 1e-5f);
            Assert.AreEqual(72f, CalculoCamaraCarro.FovPorRapidez(60f, 12f, 300f, 100f), 1e-5f);
        }

        // ---------- Moto estable ----------

        [Test]
        public void GradosEstables_ABajaVelocidadNoSeInclina_ARapidezPlenaIgualAGrados()
        {
            Assert.AreEqual(0f, CalculoInclinacionMoto.GradosEstables(1f, 2f, 9.81f, 1f, 32f, 2f, 7f), 1e-5f);
            float pleno = CalculoInclinacionMoto.GradosEstables(10f, 0.5f, 9.81f, 1f, 32f, 2f, 7f);
            Assert.AreEqual(CalculoInclinacionMoto.Grados(10f, 0.5f, 9.81f, 1f, 32f), pleno, 1e-4f);
            float parcial = CalculoInclinacionMoto.GradosEstables(4.5f, 0.5f, 9.81f, 1f, 32f, 2f, 7f);
            Assert.Less(Mathf.Abs(parcial), Mathf.Abs(CalculoInclinacionMoto.Grados(4.5f, 0.5f, 9.81f, 1f, 32f)));
        }

        [Test]
        public void LimitarCambio_NoPasaDelMaximoPorSegundo()
        {
            Assert.AreEqual(1.8f, CalculoInclinacionMoto.LimitarCambio(0f, 30f, 90f, 0.02f), 1e-4f);
            Assert.AreEqual(-1.8f, CalculoInclinacionMoto.LimitarCambio(0f, -30f, 90f, 0.02f), 1e-4f);
            Assert.AreEqual(0.5f, CalculoInclinacionMoto.LimitarCambio(0f, 0.5f, 90f, 0.02f), 1e-4f);
        }

        [Test]
        public void EnMarcha_TieneHisteresis()
        {
            Assert.IsFalse(CalculoInclinacionMoto.EnMarcha(false, 0.9f, 1f, 0.3f));
            Assert.IsTrue(CalculoInclinacionMoto.EnMarcha(false, 1.1f, 1f, 0.3f));
            Assert.IsTrue(CalculoInclinacionMoto.EnMarcha(true, 0.6f, 1f, 0.3f));
            Assert.IsFalse(CalculoInclinacionMoto.EnMarcha(true, 0.2f, 1f, 0.3f));
        }
    }
}
