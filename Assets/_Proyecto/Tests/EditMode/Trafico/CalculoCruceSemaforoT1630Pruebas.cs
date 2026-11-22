using MechaCyberpunk.Trafico.Calle;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.Trafico
{
    /// <summary>T-1630: el ciclo de dos ejes de un cruce, cuándo debe detenerse un carro y qué poste le toca obedecer.</summary>
    public class CalculoCruceSemaforoT1630Pruebas
    {
        private const float Verde = 8f;
        private const float Ambar = 2f;
        private const float Despeje = 1.5f;
        private const float Paso = 0.05f;
        private const float Margen = 4.5f;
        private const float Alcance = 40f;

        private static CalculoCruceSemaforo Cruce() => new CalculoCruceSemaforo(Verde, Ambar, Despeje);

        [Test]
        public void Ciclo_EsElDobleDeVerdeAmbarYDespeje()
        {
            Assert.AreEqual(2f * (Verde + Ambar + Despeje), Cruce().Ciclo, 0.0001f);
        }

        [Test]
        public void FaseEn_UnEje_VaVerdeAmbarRojo()
        {
            CalculoCruceSemaforo c = Cruce();
            Assert.AreEqual(FaseCruce.Verde, c.FaseEn(0, 0f));
            Assert.AreEqual(FaseCruce.Verde, c.FaseEn(0, Verde - 0.1f));
            Assert.AreEqual(FaseCruce.Ambar, c.FaseEn(0, Verde + 0.1f));
            Assert.AreEqual(FaseCruce.Rojo, c.FaseEn(0, Verde + Ambar + 0.1f));
            Assert.AreEqual(FaseCruce.Verde, c.FaseEn(0, c.Ciclo + 0.1f), "El ciclo se repite");
        }

        [Test]
        public void FaseEn_ElOtroEje_ArrancaEnRojoYSeTurnaConElPrimero()
        {
            CalculoCruceSemaforo c = Cruce();
            Assert.AreEqual(FaseCruce.Rojo, c.FaseEn(1, 0f));
            Assert.AreEqual(FaseCruce.Verde, c.FaseEn(1, c.Ciclo * 0.5f + 0.1f), "Cuando el primero acaba su turno, el segundo empieza");
        }

        [Test]
        public void FaseEn_NuncaHayDosEjesPasandoALaVez_YHayRojoParaTodos()
        {
            CalculoCruceSemaforo c = Cruce();
            bool todoRojo = false;
            for (float t = 0f; t < c.Ciclo * 2f; t += Paso)
            {
                FaseCruce a = c.FaseEn(0, t);
                FaseCruce b = c.FaseEn(1, t);
                Assert.IsTrue(a == FaseCruce.Rojo || b == FaseCruce.Rojo, "A los " + t + " s los dos ejes pueden pasar");
                todoRojo |= a == FaseCruce.Rojo && b == FaseCruce.Rojo;
            }
            Assert.IsTrue(todoRojo, "Entre un eje y el otro el cruce se despeja");
        }

        [Test]
        public void FaseEn_CadaEjeEstaEnVerdeLoMismoTiempo()
        {
            CalculoCruceSemaforo c = Cruce();
            float verde0 = 0f;
            float verde1 = 0f;
            for (float t = 0f; t < c.Ciclo; t += Paso)
            {
                verde0 += c.FaseEn(0, t) == FaseCruce.Verde ? Paso : 0f;
                verde1 += c.FaseEn(1, t) == FaseCruce.Verde ? Paso : 0f;
            }
            Assert.AreEqual(Verde, verde0, 0.2f);
            Assert.AreEqual(Verde, verde1, 0.2f);
        }

        [Test]
        public void DebeDetenerse_EnVerde_Nunca()
        {
            Assert.IsFalse(CalculoCruceSemaforo.DebeDetenerse(FaseCruce.Verde, 10f, 8f, 4f));
            Assert.IsFalse(CalculoCruceSemaforo.DebeDetenerse(FaseCruce.Verde, 0f, 0f, 4f));
        }

        [Test]
        public void DebeDetenerse_EnRojo_SiNoRebasoLaLinea()
        {
            Assert.IsTrue(CalculoCruceSemaforo.DebeDetenerse(FaseCruce.Rojo, 20f, 8f, 4f));
            Assert.IsTrue(CalculoCruceSemaforo.DebeDetenerse(FaseCruce.Rojo, 0f, 0f, 4f), "Parado en la línea sigue esperando");
            Assert.IsTrue(CalculoCruceSemaforo.DebeDetenerse(FaseCruce.Rojo, -1f, 0.5f, 4f), "Se pasó un poco al frenar: sigue esperando");
            Assert.IsFalse(CalculoCruceSemaforo.DebeDetenerse(FaseCruce.Rojo, -4f, 6f, 4f), "Ya está cruzando: no se queda en medio");
        }

        [Test]
        public void DebeDetenerse_EnAmbar_SoloSiAlcanzaAFrenar()
        {
            // A 8 m/s con 4 m/s² frena en 8 m.
            Assert.IsTrue(CalculoCruceSemaforo.DebeDetenerse(FaseCruce.Ambar, 12f, 8f, 4f));
            Assert.IsFalse(CalculoCruceSemaforo.DebeDetenerse(FaseCruce.Ambar, 5f, 8f, 4f), "Ya no alcanza: pasa");
            Assert.IsTrue(CalculoCruceSemaforo.DebeDetenerse(FaseCruce.Ambar, 1f, 0f, 4f), "Parado en la línea no arranca en ámbar");
        }

        [Test]
        public void Controla_PosteAlFrenteQueLeHablaAlCarro_DaLosMetrosALaLinea()
        {
            bool controla = CalculoCruceSemaforo.Controla(Vector3.zero, Vector3.forward, new Vector3(3f, 0f, 20f), Vector3.forward, Margen, Alcance, out float metros);
            Assert.IsTrue(controla);
            Assert.AreEqual(20f - Margen, metros, 0.001f);
        }

        [Test]
        public void Controla_PosteDeOtroSentido_NoEsDelCarro()
        {
            Assert.IsFalse(CalculoCruceSemaforo.Controla(Vector3.zero, Vector3.forward, new Vector3(3f, 0f, 20f), Vector3.back, Margen, Alcance, out _), "Le habla a los de enfrente");
            Assert.IsFalse(CalculoCruceSemaforo.Controla(Vector3.zero, Vector3.forward, new Vector3(3f, 0f, 20f), Vector3.right, Margen, Alcance, out _), "Le habla a la calle cruzada");
        }

        [Test]
        public void Controla_PosteDetrasMuyLejosOALoLejos_NoEsDelCarro()
        {
            Assert.IsFalse(CalculoCruceSemaforo.Controla(new Vector3(0f, 0f, 30f), Vector3.forward, new Vector3(3f, 0f, 20f), Vector3.forward, Margen, Alcance, out _), "Ya lo pasó");
            Assert.IsFalse(CalculoCruceSemaforo.Controla(Vector3.zero, Vector3.forward, new Vector3(3f, 0f, 90f), Vector3.forward, Margen, Alcance, out _), "Todavía está lejos");
            Assert.IsFalse(CalculoCruceSemaforo.Controla(Vector3.zero, Vector3.forward, new Vector3(CalculoCruceSemaforo.LateralMaximo + 5f, 0f, 20f), Vector3.forward, Margen, Alcance, out _), "Está en otra calle");
        }

        [Test]
        public void Controla_CarroParadoEnLaLinea_SigueTeniendoAlPoste()
        {
            // Para en la línea de parada: su centro queda Margen m antes del poste.
            bool controla = CalculoCruceSemaforo.Controla(new Vector3(0f, 0f, 15.5f), Vector3.forward, new Vector3(3f, 0f, 20f), Vector3.forward, Margen, Alcance, out float metros);
            Assert.IsTrue(controla);
            Assert.AreEqual(0f, metros, 0.001f);
        }

        [Test]
        public void Controla_CarroQueGiraUnPoco_SigueObedeciendo_PeroNoUnoDeLado()
        {
            Vector3 adelanteTorcido = Quaternion.Euler(0f, 20f, 0f) * Vector3.forward;
            Assert.IsTrue(CalculoCruceSemaforo.Controla(Vector3.zero, adelanteTorcido, new Vector3(3f, 0f, 20f), Vector3.forward, Margen, Alcance, out _));
            Vector3 adelanteDeLado = Quaternion.Euler(0f, 60f, 0f) * Vector3.forward;
            Assert.IsFalse(CalculoCruceSemaforo.Controla(Vector3.zero, adelanteDeLado, new Vector3(3f, 0f, 20f), Vector3.forward, Margen, Alcance, out _));
        }
    }
}
