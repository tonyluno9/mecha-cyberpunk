using MechaCyberpunk.Mundo.Reloj;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.Mundo
{
    public class NieblaPorAlturaPruebas
    {
        [Test]
        public void Factor_ANivelDeCalle_EsUno()
        {
            Assert.AreEqual(1f, CalculoNieblaAltura.Factor(0f), 1e-5f);
            Assert.AreEqual(1f, CalculoNieblaAltura.Factor(-5f), 1e-5f);
            Assert.AreEqual(1f, CalculoNieblaAltura.Factor(CalculoNieblaAltura.AlturaCalle), 1e-5f);
        }

        [Test]
        public void Factor_Entre40Y80Metros_EstaEntre35Y45Porciento()
        {
            for (float h = 40f; h <= 80f; h += 5f)
            {
                float f = CalculoNieblaAltura.Factor(h);
                Assert.GreaterOrEqual(f, 0.35f - 1e-5f, "h=" + h);
                Assert.LessOrEqual(f, 0.45f + 1e-5f, "h=" + h);
            }
        }

        [Test]
        public void Factor_Desde150Metros_EsBajo()
        {
            Assert.LessOrEqual(CalculoNieblaAltura.Factor(150f), 0.2f);
            Assert.LessOrEqual(CalculoNieblaAltura.Factor(400f), 0.2f);
        }

        [Test]
        public void Factor_SubiendoNuncaAumenta()
        {
            float anterior = CalculoNieblaAltura.Factor(0f);
            for (float h = 1f; h <= 300f; h += 1f)
            {
                float f = CalculoNieblaAltura.Factor(h);
                Assert.LessOrEqual(f, anterior + 1e-6f, "h=" + h);
                anterior = f;
            }
        }

        [Test]
        public void Oculto_ConHisteresis_NoParpadea()
        {
            Assert.IsFalse(CalculoOcultarAltura.Oculto(false, 24f, 25f, 3f));
            Assert.IsTrue(CalculoOcultarAltura.Oculto(false, 26f, 25f, 3f));
            Assert.IsTrue(CalculoOcultarAltura.Oculto(true, 23f, 25f, 3f), "sigue oculto dentro de la histéresis");
            Assert.IsFalse(CalculoOcultarAltura.Oculto(true, 21f, 25f, 3f));
        }

        [Test]
        public void OcultoPorDebajo_SoloSeVeDesdeArriba()
        {
            Assert.IsTrue(CalculoOcultarAltura.OcultoPorDebajo(false, 10f, 25f, 3f));
            Assert.IsFalse(CalculoOcultarAltura.OcultoPorDebajo(true, 40f, 25f, 3f));
            Assert.IsTrue(CalculoOcultarAltura.OcultoPorDebajo(true, 27f, 25f, 3f), "sigue oculto dentro de la histéresis");
        }

        [Test]
        public void OcultarPorAltura_SoloArriba_SeVeSoloDesdeArribaYSeRestaura()
        {
            var go = new GameObject("piso", typeof(MeshRenderer));
            var host = new GameObject("host");
            OcultarPorAltura o = host.AddComponent<OcultarPorAltura>();
            var so = new UnityEditor.SerializedObject(o);
            so.FindProperty("renderers").arraySize = 1;
            so.FindProperty("renderers").GetArrayElementAtIndex(0).objectReferenceValue = go.GetComponent<MeshRenderer>();
            so.FindProperty("umbral").floatValue = 25f;
            so.FindProperty("soloArriba").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();
            o.Aplicar(5f);
            Assert.IsFalse(go.GetComponent<MeshRenderer>().enabled, "abajo se oculta");
            o.Aplicar(60f);
            Assert.IsTrue(go.GetComponent<MeshRenderer>().enabled, "arriba se ve");
            o.Aplicar(5f);
            o.Aplicar(float.NegativeInfinity);
            Assert.IsTrue(go.GetComponent<MeshRenderer>().enabled, "restaurar lo deja visible");
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(host);
        }

        [Test]
        public void OcultarPorAltura_ApagaYEnciendeLosRenderers()
        {
            var go = new GameObject("fondo", typeof(MeshRenderer));
            var host = new GameObject("host");
            OcultarPorAltura o = host.AddComponent<OcultarPorAltura>();
            var so = new UnityEditor.SerializedObject(o);
            so.FindProperty("renderers").arraySize = 1;
            so.FindProperty("renderers").GetArrayElementAtIndex(0).objectReferenceValue = go.GetComponent<MeshRenderer>();
            so.FindProperty("umbral").floatValue = 25f;
            so.ApplyModifiedPropertiesWithoutUndo();
            o.Aplicar(10f);
            Assert.IsTrue(go.GetComponent<MeshRenderer>().enabled);
            o.Aplicar(60f);
            Assert.IsFalse(go.GetComponent<MeshRenderer>().enabled);
            o.Aplicar(5f);
            Assert.IsTrue(go.GetComponent<MeshRenderer>().enabled);
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(host);
        }

        [Test]
        public void Fijar_LimitaEntreCeroYUno()
        {
            NieblaPorAltura.Fijar(3f);
            Assert.AreEqual(1f, NieblaPorAltura.FactorActual, 1e-5f);
            NieblaPorAltura.Fijar(0.4f);
            Assert.AreEqual(0.4f, NieblaPorAltura.FactorActual, 1e-5f);
            NieblaPorAltura.Fijar(1f);
        }
    }
}
