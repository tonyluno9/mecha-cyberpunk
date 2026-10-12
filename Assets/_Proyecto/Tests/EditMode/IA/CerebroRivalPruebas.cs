using System;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.Rival;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Tests.IA
{
    public class CerebroRivalPruebas
    {
        private const float DistanciaAtaque = 2.6f;
        private const float EsperaMin = 1.2f;
        private const float EsperaMax = 2.5f;
        private const float Margen = 1e-4f;
        private const float DistanciaLejos = 10f;
        private const float DistanciaEnGolpe = 2.3f;

        private DatosRival datos;

        [SetUp]
        public void SetUp()
        {
            datos = ScriptableObject.CreateInstance<DatosRival>();
            SerializedObject so = new SerializedObject(datos);
            so.FindProperty("distanciaAtaque").floatValue = DistanciaAtaque;
            so.FindProperty("distanciaMinima").floatValue = 2f;
            so.FindProperty("esperaEntreAtaquesMin").floatValue = EsperaMin;
            so.FindProperty("esperaEntreAtaquesMax").floatValue = EsperaMax;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(datos);
        }

        private CerebroRival Nuevo(params float[] azares)
        {
            int indice = 0;
            Func<float> azar = () => azares[Math.Min(indice++, azares.Length - 1)];
            return new CerebroRival(datos, azar);
        }

        [Test]
        public void Decidir_LejosDelObjetivo_SeAcerca()
        {
            CerebroRival cerebro = Nuevo(0f);
            Assert.AreEqual(DecisionRival.Acercarse, cerebro.Decidir(DistanciaLejos, false, true, 0.02f));
        }

        [Test]
        public void Decidir_LejosYConLaEsperaCumplida_SigueAcercandoseSinGolpear()
        {
            CerebroRival cerebro = Nuevo(0f);
            Assert.AreEqual(DecisionRival.Acercarse, cerebro.Decidir(DistanciaLejos, false, true, EsperaMax + 1f));
        }

        [Test]
        public void Decidir_EnDistanciaYEsperaCumplida_EmpiezaGolpe()
        {
            CerebroRival cerebro = Nuevo(0f);
            Assert.AreEqual(DecisionRival.EmpezarGolpe, cerebro.Decidir(DistanciaEnGolpe, false, true, EsperaMin + 0.01f));
        }

        [Test]
        public void Decidir_EnDistanciaPeroEsperando_Espera()
        {
            CerebroRival cerebro = Nuevo(0f);
            Assert.AreEqual(DecisionRival.Esperar, cerebro.Decidir(DistanciaEnGolpe, false, true, EsperaMin - 0.1f));
        }

        [Test]
        public void Decidir_JustoEnLaDistanciaDeAtaque_NoSeAcerca()
        {
            CerebroRival cerebro = Nuevo(0f);
            Assert.AreEqual(DecisionRival.Esperar, cerebro.Decidir(DistanciaAtaque, false, true, 0.02f));
        }

        [Test]
        public void Decidir_EsperaVaCorriendoAlAcercarse_AlLlegarGolpeSinEsperarOtraVez()
        {
            CerebroRival cerebro = Nuevo(0f);
            cerebro.Decidir(DistanciaLejos, false, true, EsperaMin + 0.5f);
            Assert.AreEqual(DecisionRival.EmpezarGolpe, cerebro.Decidir(DistanciaEnGolpe, false, true, 0.02f));
        }

        [Test]
        public void Decidir_ObjetivoEnKO_EsperaYNoGastaLaEspera()
        {
            CerebroRival cerebro = Nuevo(0f);
            float antes = cerebro.EsperaRestante;
            Assert.AreEqual(DecisionRival.Esperar, cerebro.Decidir(DistanciaEnGolpe, true, true, EsperaMax + 1f));
            Assert.AreEqual(DecisionRival.Esperar, cerebro.Decidir(DistanciaLejos, true, true, 1f));
            Assert.AreEqual(antes, cerebro.EsperaRestante, Margen);
        }

        [Test]
        public void Decidir_SinControl_EsperaYNoGastaLaEspera()
        {
            CerebroRival cerebro = Nuevo(0f);
            float antes = cerebro.EsperaRestante;
            Assert.AreEqual(DecisionRival.Esperar, cerebro.Decidir(DistanciaEnGolpe, false, false, EsperaMax + 1f));
            Assert.AreEqual(DecisionRival.Esperar, cerebro.Decidir(DistanciaLejos, false, false, 1f));
            Assert.AreEqual(antes, cerebro.EsperaRestante, Margen);
        }

        [Test]
        public void Decidir_DespuesDeGolpear_LaEsperaNuevaSigueElAzar()
        {
            CerebroRival cerebro = Nuevo(0f, 0.5f, 1f);
            Assert.AreEqual(EsperaMin, cerebro.EsperaRestante, Margen);

            cerebro.Decidir(DistanciaEnGolpe, false, true, EsperaMin);
            Assert.AreEqual((EsperaMin + EsperaMax) / 2f, cerebro.EsperaRestante, Margen, "Azar 0.5: a la mitad");

            cerebro.Decidir(DistanciaEnGolpe, false, true, cerebro.EsperaRestante);
            Assert.AreEqual(EsperaMax, cerebro.EsperaRestante, Margen, "Azar 1: la máxima");
        }

        [Test]
        public void Decidir_AzarFueraDeRango_LaEsperaQuedaEntreLaMinimaYLaMaxima()
        {
            CerebroRival cerebro = Nuevo(-3f, 7f, 0.25f);
            Assert.AreEqual(EsperaMin, cerebro.EsperaRestante, Margen);

            cerebro.Decidir(DistanciaEnGolpe, false, true, EsperaMin);
            Assert.AreEqual(EsperaMax, cerebro.EsperaRestante, Margen);
        }

        [Test]
        public void Decidir_ConAzarVerdadero_TodasLasEsperasQuedanEnRango()
        {
            CerebroRival cerebro = new CerebroRival(datos);
            for (int i = 0; i < 200; i++)
            {
                Assert.GreaterOrEqual(cerebro.EsperaRestante, EsperaMin - Margen);
                Assert.LessOrEqual(cerebro.EsperaRestante, EsperaMax + Margen);
                Assert.AreEqual(DecisionRival.EmpezarGolpe, cerebro.Decidir(DistanciaEnGolpe, false, true, EsperaMax));
            }
        }
    }
}
