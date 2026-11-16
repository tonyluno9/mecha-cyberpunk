using MechaCyberpunk.Trafico.Persecucion;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.Trafico
{
    /// <summary>T-1006: detección de patrullas atoradas (lógica pura) y suavizado del volante.</summary>
    public class DetectorAtascoT1006Pruebas
    {
        private const float Paso = 0.1f;
        private DetectorAtasco detector;
        private ParametrosAtasco parametros;

        [SetUp]
        public void Preparar()
        {
            parametros = new ParametrosAtasco { VentanaSegundos = 1.5f, DistanciaMinima = 0.6f, ReversasParaRecolocar = 2, SegundosParaRecolocar = 10f, RadioLiberada = 6f };
            detector = new DetectorAtasco(parametros);
        }

        // Avanza el tiempo con la patrulla en el mismo punto; devuelve la primera acción distinta de Ninguna.
        private AccionAtasco Correr(float segundos, Vector3 posicion, bool quiere, int reversas = 0, bool enReversa = false)
        {
            for (float t = 0f; t < segundos; t += Paso)
            {
                AccionAtasco a = detector.Actualizar(posicion, quiere, reversas, enReversa, Paso);
                if (a != AccionAtasco.Ninguna)
                {
                    return a;
                }
            }
            return AccionAtasco.Ninguna;
        }

        [Test]
        public void QuietaQueriendoAvanzar_TrasLaVentana_PideReversa()
        {
            Assert.AreEqual(AccionAtasco.Reversa, Correr(2f, Vector3.zero, true));
            Assert.IsTrue(detector.Atorada);
        }

        [Test]
        public void QuietaPeroSinQuererAvanzar_NoSeConsideraAtorada()
        {
            Assert.AreEqual(AccionAtasco.Ninguna, Correr(30f, Vector3.zero, false));
            Assert.IsFalse(detector.Atorada);
        }

        [Test]
        public void AvanzandoNormal_NuncaPideNada()
        {
            for (int i = 0; i < 400; i++)
            {
                Assert.AreEqual(AccionAtasco.Ninguna, detector.Actualizar(new Vector3(0f, 0f, i * Paso * 5f), true, 0, false, Paso));
            }
        }

        [Test]
        public void EmpujandoDespacio_ConMuyPocoAvance_CuentaComoAtorada()
        {
            AccionAtasco a = AccionAtasco.Ninguna;
            for (int i = 0; i < 40 && a == AccionAtasco.Ninguna; i++)
            {
                a = detector.Actualizar(new Vector3(0f, 0f, i * Paso * 0.1f), true, 0, false, Paso);
            }
            Assert.AreEqual(AccionAtasco.Reversa, a, "0.1 m/s es empujar contra algo");
        }

        [Test]
        public void VariasReversasEnElMismoLugar_PidenRecolocar()
        {
            Assert.AreEqual(AccionAtasco.Reversa, Correr(2f, Vector3.zero, true, 0));
            Assert.AreEqual(AccionAtasco.Ninguna, Correr(0.5f, Vector3.zero, false, 1, true), "El conductor ya dio la primera reversa");
            Assert.AreEqual(AccionAtasco.Reversa, Correr(2f, Vector3.zero, true, 1));
            Assert.AreEqual(AccionAtasco.Recolocar, Correr(0.5f, Vector3.zero, false, 2, true), "La segunda reversa en el mismo lugar ya recoloca");
        }

        [Test]
        public void TiempoLargoEnElMismoLugar_PideRecolocarAunSinReversas()
        {
            Correr(2f, Vector3.zero, true, 0); // abre la zona
            Assert.AreEqual(AccionAtasco.Recolocar, Correr(parametros.SegundosParaRecolocar + 1f, Vector3.zero, false, 0));
        }

        [Test]
        public void AlejarseDelLugar_LiberaYOlvidaLosIntentos()
        {
            Correr(2f, Vector3.zero, true, 1);
            Assert.IsTrue(detector.Atorada);
            detector.Actualizar(new Vector3(0f, 0f, parametros.RadioLiberada + 1f), true, 1, false, Paso);
            Assert.IsFalse(detector.Atorada);
            Assert.AreEqual(0, detector.EventosEnLaZona);
        }

        [Test]
        public void DespuesDeRecolocar_NoVuelveAPedirloDeInmediato()
        {
            Correr(2f, Vector3.zero, true, 0);
            Assert.AreEqual(AccionAtasco.Recolocar, Correr(parametros.SegundosParaRecolocar + 1f, Vector3.zero, false, 0));
            Assert.AreEqual(AccionAtasco.Ninguna, detector.Actualizar(Vector3.zero, false, 0, false, Paso));
        }

        [Test]
        public void Reiniciar_LimpiaElEstado()
        {
            Correr(2f, Vector3.zero, true, 1);
            detector.Reiniciar();
            Assert.IsFalse(detector.Atorada);
            Assert.AreEqual(0, detector.EventosEnLaZona);
        }

        [Test]
        public void Volante_NoCambiaMasRapidoQueElLimite()
        {
            float v = ReglaSuavizadoVolante.Seguir(0f, 1f, 5f, 0.02f);
            Assert.AreEqual(0.1f, v, 1e-5f);
            v = ReglaSuavizadoVolante.Seguir(0.95f, 1f, 5f, 0.02f);
            Assert.AreEqual(1f, v, 1e-5f, "No se pasa de la meta");
        }

        [Test]
        public void Volante_AlternarDeseado_NoTiembla()
        {
            float v = 0f;
            float maxSalto = 0f;
            for (int i = 0; i < 100; i++)
            {
                float nuevo = ReglaSuavizadoVolante.Seguir(v, i % 2 == 0 ? 1f : -1f, 6f, 0.02f);
                maxSalto = Mathf.Max(maxSalto, Mathf.Abs(nuevo - v));
                v = nuevo;
            }
            Assert.LessOrEqual(maxSalto, 6f * 0.02f + 1e-5f);
            Assert.Less(Mathf.Abs(v), 0.2f, "Entre ordenes contrarias el volante se queda cerca del centro");
        }

        [Test]
        public void Volante_FueraDeRango_SeRecorta()
        {
            Assert.AreEqual(1f, ReglaSuavizadoVolante.Seguir(0.99f, 5f, 100f, 1f), 1e-5f);
        }
    }
}
