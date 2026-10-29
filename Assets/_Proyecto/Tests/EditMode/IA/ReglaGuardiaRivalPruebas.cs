using MechaCyberpunk.IA.Adaptacion;
using NUnit.Framework;

namespace MechaCyberpunk.Tests.IA
{
    /// <summary>T-201: la regla pura de la guardia del rival (azar y tiempo inyectados).</summary>
    public class ReglaGuardiaRivalPruebas
    {
        private static readonly float[] Probabilidades = { 0.4f, 0.65f };
        private const float Reaccion = 0.06f;
        private const float Sostener = 0.3f;
        private const float Paso = 0.01f;
        private const float Tolerancia = 1e-4f;

        private float azar;
        private ReglaGuardiaRival regla;

        [SetUp]
        public void SetUp()
        {
            azar = 0f;
            regla = new ReglaGuardiaRival(Probabilidades, Reaccion, Sostener, () => azar);
        }

        // Avanza pasos de 0.01 s con el estado dado y devuelve cuánto tardó en pedir sostener (-1 si no lo pidió en el límite).
        private float HastaSostener(int nivel, bool preparando, bool libre, float limite)
        {
            float t = 0f;
            while (t < limite)
            {
                t += Paso;
                if (regla.Actualizar(Paso, nivel, preparando, libre))
                {
                    return t;
                }
            }
            return -1f;
        }

        private void EmpezarGolpe(int nivel, bool libre = true)
        {
            regla.Actualizar(Paso, nivel, false, libre);
            regla.Actualizar(Paso, nivel, true, libre);
        }

        // Sigue sin golpe en preparación hasta que suelta; devuelve los segundos que tardó (2 s de límite).
        private float HastaSoltar()
        {
            float t = 0f;
            bool sostiene = true;
            while (sostiene && t < 2f)
            {
                t += Paso;
                sostiene = regla.Actualizar(Paso, 1, false, true);
            }
            return t;
        }

        [Test]
        public void NivelCero_NuncaSeCubre()
        {
            azar = 0f;
            regla.Actualizar(Paso, 0, false, true);
            Assert.AreEqual(-1f, HastaSostener(0, true, true, 1f));
            Assert.AreEqual(EstadoGuardiaRival.Abajo, regla.Estado);
        }

        [Test]
        public void AzarDebajoDeLaProbabilidad_SeCubre()
        {
            azar = 0.39f;
            EmpezarGolpe(1);
            Assert.AreEqual(EstadoGuardiaRival.EsperandoReaccion, regla.Estado);
        }

        [Test]
        public void AzarEncimaDeLaProbabilidad_NoSeCubre()
        {
            azar = 0.41f;
            EmpezarGolpe(1);
            Assert.AreEqual(EstadoGuardiaRival.Abajo, regla.Estado);
            Assert.AreEqual(-1f, HastaSostener(1, true, true, 1f));
        }

        [Test]
        public void Nivel2_UsaLaProbabilidadDelNivel2()
        {
            azar = 0.5f;
            EmpezarGolpe(1);
            Assert.AreEqual(EstadoGuardiaRival.Abajo, regla.Estado, "0.5 no pasa 0.4");
            regla = new ReglaGuardiaRival(Probabilidades, Reaccion, Sostener, () => azar);
            EmpezarGolpe(2);
            Assert.AreEqual(EstadoGuardiaRival.EsperandoReaccion, regla.Estado, "0.5 pasa 0.65");
        }

        [Test]
        public void Reaccion_PideSostenerExactamenteTrasLaReaccion()
        {
            azar = 0f;
            regla.Actualizar(Paso, 1, false, true);
            Assert.IsFalse(regla.Actualizar(Paso, 1, true, true), "En el cuadro del golpe todavía no");
            float tarda = HastaSostener(1, true, true, 1f);
            Assert.AreEqual(Reaccion, tarda, Tolerancia, "Pide la guardia a los " + Reaccion + " s de verlo");
        }

        [Test]
        public void RivalOcupadoAlReaccionar_NoSeCubreEstaVez()
        {
            azar = 0f;
            regla.Actualizar(Paso, 1, false, true);
            regla.Actualizar(Paso, 1, true, true);
            Assert.AreEqual(-1f, HastaSostener(1, true, false, 1f), "No estaba libre cuando pasó la reacción");
            Assert.AreEqual(EstadoGuardiaRival.Abajo, regla.Estado);
        }

        [Test]
        public void SueltaTrasElImpactoMasSostener()
        {
            azar = 0f;
            EmpezarGolpe(1);
            HastaSostener(1, true, true, 1f);
            // El golpe sigue preparando: sostiene sin límite.
            for (int i = 0; i < 100; i++)
            {
                Assert.IsTrue(regla.Actualizar(Paso, 1, true, true));
            }
            // El impacto: sale de Preparando; sostiene 0.3 s más.
            Assert.IsTrue(regla.Actualizar(Paso, 1, false, true), "En el cuadro del impacto sigue arriba");
            Assert.AreEqual(Sostener, HastaSoltar(), Tolerancia, "Suelta a los " + Sostener + " s del impacto");
            Assert.AreEqual(EstadoGuardiaRival.Abajo, regla.Estado);
        }

        [Test]
        public void DosGolpesSeguidos_AlarganElSostener()
        {
            azar = 0f;
            EmpezarGolpe(1);
            HastaSostener(1, true, true, 1f);
            regla.Actualizar(Paso, 1, false, true);
            // Pasan 0.2 s del primer impacto y empieza otro golpe: no suelta.
            for (int i = 0; i < 20; i++)
            {
                Assert.IsTrue(regla.Actualizar(Paso, 1, false, true));
            }
            regla.Actualizar(Paso, 1, true, true);
            for (int i = 0; i < 50; i++)
            {
                Assert.IsTrue(regla.Actualizar(Paso, 1, true, true), "Con el segundo golpe en preparación sigue sosteniendo");
            }
            regla.Actualizar(Paso, 1, false, true);
            Assert.AreEqual(Sostener, HastaSoltar(), Tolerancia, "El segundo impacto vuelve a contar los " + Sostener + " s");
        }

        [Test]
        public void Reiniciar_BajaYNoCuentaElGolpeEnCurso()
        {
            azar = 0f;
            EmpezarGolpe(1);
            HastaSostener(1, true, true, 1f);
            regla.Reiniciar();
            Assert.AreEqual(EstadoGuardiaRival.Abajo, regla.Estado);
            // El golpe que ya estaba preparando al volver el control no se toma como un golpe nuevo.
            Assert.IsFalse(regla.Actualizar(Paso, 1, true, true));
            Assert.AreEqual(EstadoGuardiaRival.Abajo, regla.Estado);
        }

        [Test]
        public void TiraElAzarUnaVezPorGolpe()
        {
            int tiradas = 0;
            regla = new ReglaGuardiaRival(Probabilidades, Reaccion, Sostener, () => { tiradas++; return 0.99f; });
            EmpezarGolpe(1);
            for (int i = 0; i < 50; i++)
            {
                regla.Actualizar(Paso, 1, true, true);
            }
            Assert.AreEqual(1, tiradas);
        }
    }
}
