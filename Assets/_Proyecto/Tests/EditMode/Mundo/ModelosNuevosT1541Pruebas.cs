using System.Linq;
using MechaCyberpunk.Arte.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Tests
{
    /// <summary>
    /// T-1541: granada W11, holograma H11 y entrada del mercado (EM01 + EM02). Las cuentas son puras; las de assets piden haber corrido
    /// ImportarModelosT1541.AplicarGranada e ImportarEntrada (si no, quedan ignoradas con el motivo).
    /// </summary>
    public class ModelosNuevosT1541Pruebas
    {
        private const float MargenGranada = 0.25f;

        [Test]
        public void Mediana_IgnoraLasMedidasSinGolpe()
        {
            Assert.AreEqual(5f, CalculoEntradaMercadoT1541.Mediana(new[] { 0f, 4f, 5f, 9f }, 1f), 0.0001f);
            Assert.AreEqual(7f, CalculoEntradaMercadoT1541.Mediana(new[] { 0f, 0f }, 7f), 0.0001f);
        }

        [Test]
        public void EscalaPorton_CubreElCallejonDeMuroAMuro()
        {
            float largo = 10f;
            float escala = CalculoEntradaMercadoT1541.EscalaPorton(9.4f, largo);
            Assert.AreEqual((9.4f + CalculoEntradaMercadoT1541.MargenMuros) / largo, escala, 0.0001f);
            Assert.GreaterOrEqual(largo * escala, 9.4f, "el portón llega a los dos muros");
        }

        [Test]
        public void EscalaPorton_SeLimita()
        {
            Assert.AreEqual(CalculoEntradaMercadoT1541.EscalaMinima, CalculoEntradaMercadoT1541.EscalaPorton(1f, 10f), 0.0001f);
            Assert.AreEqual(CalculoEntradaMercadoT1541.EscalaMaxima, CalculoEntradaMercadoT1541.EscalaPorton(40f, 10f), 0.0001f);
        }

        [Test]
        public void Patas_QuedanEnLosExtremosYDejanAbiertoElCentro()
        {
            var p = CalculoEntradaMercadoT1541.Patas(10f, 3f);
            Assert.AreEqual(-p.centroA.x, p.centroB.x, 0.0001f);
            Assert.AreEqual(5f, p.centroB.x + p.tamano.x * 0.5f, 0.0001f, "la pata llega al borde del arco");
            Assert.AreEqual(3f, p.tamano.y, 0.0001f);
            Assert.AreEqual(10f - 2f * p.tamano.x, CalculoEntradaMercadoT1541.AberturaLibre(10f), 0.0001f);
            Assert.Greater(CalculoEntradaMercadoT1541.AberturaLibre(10f), 4f, "pasa un carro por el arco");
        }

        [Test]
        public void Traslapan_DetectaCajasEncimaYSeparadas()
        {
            Assert.IsTrue(CalculoEntradaMercadoT1541.Traslapan(Vector2.zero, Vector2.one, new Vector2(1.5f, 0f), Vector2.one));
            Assert.IsFalse(CalculoEntradaMercadoT1541.Traslapan(Vector2.zero, Vector2.one, new Vector2(2.5f, 0f), Vector2.one));
        }

        [Test]
        public void Entradas_SonDeLaCarpetaEMYLaCapturaMiraAlPorton()
        {
            Assert.AreEqual("EM", ImportarModelosT1541.Porton.Grupo);
            Assert.AreEqual("EM", ImportarModelosT1541.Puesto.Grupo);
            Assert.AreEqual("Assets/Arte/Zonas/EM/EM01_PortonMercado_a.prefab", ImportarZonasT1382.RutaPrefab(ImportarModelosT1541.Porton, ImportarModelosT1541.Variante));
            Assert.Less(ImportarModelosT1541.CapturaDesdeAvenida.z, ImportarModelosT1541.ZPorton, "la captura de la avenida está antes del portón");
            Assert.Greater(ImportarModelosT1541.CapturaDesdeDentro.z, ImportarModelosT1541.ZPorton, "la captura de dentro está después del portón");
        }

        [Test]
        public void HologramaDeLaGranada_EsElH11_YElModeloEsElW11()
        {
            Assert.IsTrue(ImportarS2nHologramas.Archivos.Contains("H11_Granada.png"));
            Assert.IsFalse(ImportarS2nHologramas.Archivos.Contains("H08_Granada.png"));
            StringAssert.Contains("W11", ImportarModelosT1541.RutaGranadaPrefab);
        }

        [Test]
        public void PrefabGranada_Mide10cm_ConfigYHologramaApuntanAlW11()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ImportarModelosT1541.RutaGranadaPrefab);
            if (prefab == null)
            {
                Assert.Ignore("falta correr ImportarModelosT1541.AplicarGranada");
            }
            GameObject instancia = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            try
            {
                Bounds b = ArteT250.Limites(instancia);
                float largo = Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z));
                Assert.That(largo, Is.InRange(ImportarModelosT1541.LargoGranada * (1f - MargenGranada), ImportarModelosT1541.LargoGranada * (1f + MargenGranada)), "tamaño real (m)");
                Assert.AreEqual(0, instancia.GetComponentsInChildren<Collider>(true).Length, "la granada del prefab no lleva colliders");
                Assert.IsNotNull(instancia.transform.Find("Boca"), "tiene Boca");
            }
            finally
            {
                Object.DestroyImmediate(instancia);
            }
            Object config = AssetDatabase.LoadMainAssetAtPath("Assets/_Proyecto/Personaje/Datos/Resources/ConfigArmasEspeciales.asset");
            Assert.IsNotNull(config, "existe ConfigArmasEspeciales");
            Assert.AreSame(prefab, new SerializedObject(config).FindProperty("prefabGranada").objectReferenceValue, "la granada que se lanza es el W11");
        }

        [Test]
        public void PrefabsEM_Porton_TieneDosPatasSinCajaEntera_Puesto_TieneCaja()
        {
            GameObject porton = AssetDatabase.LoadAssetAtPath<GameObject>(ImportarZonasT1382.RutaPrefab(ImportarModelosT1541.Porton, ImportarModelosT1541.Variante));
            GameObject puesto = AssetDatabase.LoadAssetAtPath<GameObject>(ImportarZonasT1382.RutaPrefab(ImportarModelosT1541.Puesto, ImportarModelosT1541.Variante));
            if (porton == null || puesto == null)
            {
                Assert.Ignore("falta correr ImportarModelosT1541.ImportarEntrada");
            }
            Assert.AreEqual(2, porton.GetComponentsInChildren<BoxCollider>(true).Length, "dos patas");
            Assert.IsNull(porton.GetComponent<BoxCollider>(), "sin caja entera: taparía el arco");
            Assert.AreEqual(1, puesto.GetComponents<BoxCollider>().Length, "el puesto lleva su caja");
        }
    }
}
