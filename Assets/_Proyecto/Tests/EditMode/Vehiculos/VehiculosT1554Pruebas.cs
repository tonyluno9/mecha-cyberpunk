using System.Collections.Generic;
using System.IO;
using System.Linq;
using MechaCyberpunk.Trafico.Datos;
using MechaCyberpunk.Vehiculos;
using MechaCyberpunk.Vehiculos.Carro;
using MechaCyberpunk.Vehiculos.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Tests.Vehiculos
{
    /// <summary>
    /// T-1554: lo que deja VehiculosT1554.Aplicar. Bisagra del cofre en PA01, PA02 y V22; V16 a V19 coherentes con la escala de su raíz;
    /// rotores de drones sin override de posición y con el pivote centrado; sin carros viejos de una pieza en Barrio ni en Ciudad_Prologo; sin V10 ni V11 en las listas de tráfico.
    /// </summary>
    public class VehiculosT1554Pruebas
    {
        private const float MargenSuspension = 0.02f;
        private const float MargenRadio = 0.02f;
        private const float MargenAsiento = 0.03f;
        private const float MargenPunto = 0.05f;

        private static GameObject Cargar(string codigo)
        {
            GameObject g = AssetDatabase.LoadAssetAtPath<GameObject>(VehiculosT1554.RutaPrefab(codigo));
            Assert.IsNotNull(g, "Falta el prefab de " + codigo);
            return g;
        }

        // ------------------------------------------------------------------ cofres

        [Test]
        public void Cofre_PA01PA02YV22_TienenBisagraConCofreYEnchufadaAPuertas([ValueSource(typeof(VehiculosT1554), nameof(VehiculosT1554.CarrosConCofreNuevo))] string codigo)
        {
            GameObject raiz = PrefabUtility.LoadPrefabContents(VehiculosT1554.RutaPrefab(codigo));
            try
            {
                Transform bisagra = VehiculosT1554.BisagraCofreDe(raiz);
                Assert.IsNotNull(bisagra, codigo + ": Visual_T511/Bisagra_Cofre con su Cofre");
                Assert.IsNotNull(bisagra.Find("Cofre").GetComponent<MeshFilter>(), codigo + ": el Cofre tiene malla");
                PuertasCarro puertas = raiz.GetComponent<PuertasCarro>();
                Assert.IsNotNull(puertas, codigo + ": PuertasCarro");
                Assert.AreEqual(bisagra, new SerializedObject(puertas).FindProperty("bisagraCofre").objectReferenceValue, codigo + ": bisagraCofre enchufada");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
        }

        [Test]
        public void Cofre_PA01PA02YV22_BisagraArribaYAlFrenteDeLaCabina([ValueSource(typeof(VehiculosT1554), nameof(VehiculosT1554.CarrosConCofreNuevo))] string codigo)
        {
            GameObject raiz = PrefabUtility.LoadPrefabContents(VehiculosT1554.RutaPrefab(codigo));
            try
            {
                Transform bisagra = VehiculosT1554.BisagraCofreDe(raiz);
                Assert.IsNotNull(bisagra, codigo);
                Bounds carro = VehiculosT1554.LimitesVisual(raiz);
                Vector3 local = raiz.transform.InverseTransformPoint(bisagra.position);
                Assert.Greater(local.z, 0f, codigo + ": la bisagra está en la mitad delantera (el frente es +Z)");
                Assert.Greater(local.y, carro.size.y * 0.3f, codigo + ": la bisagra está arriba, no en el piso");
                Assert.Less(Mathf.Abs(local.x), carro.size.x * 0.25f, codigo + ": la bisagra está al centro");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
        }

        // ------------------------------------------------------------------ V16 a V19

        [Test]
        public void Grandes_EscalaDeRaiz([ValueSource(typeof(VehiculosT1554), nameof(VehiculosT1554.Grandes))] (string codigo, float escala) g)
        {
            Assert.AreEqual(g.escala, Cargar(g.codigo).transform.localScale.x, 0.005f, g.codigo);
        }

        [Test]
        public void Grandes_RigidbodyConLaMasaDeSuConfig([ValueSource(typeof(VehiculosT1554), nameof(VehiculosT1554.Grandes))] (string codigo, float escala) g)
        {
            GameObject carro = Cargar(g.codigo);
            ConfigCarro config = CarrosPAT1510.ConfigDe(carro);
            Assert.IsNotNull(config, g.codigo);
            Assert.AreEqual(config.Masa, carro.GetComponent<Rigidbody>().mass, 0.5f, g.codigo);
        }

        [Test]
        public void Grandes_LlantaTocaElPisoConElResorteEnReposo([ValueSource(typeof(VehiculosT1554), nameof(VehiculosT1554.Grandes))] (string codigo, float escala) g)
        {
            GameObject raiz = PrefabUtility.LoadPrefabContents(VehiculosT1554.RutaPrefab(g.codigo));
            try
            {
                float esperada = CarrosPAT1510.AlturaAnclaEnReposo(CarrosPAT1510.ConfigDe(raiz));
                foreach (string rueda in CreadorCarrosPiezasT742.NombresRuedas)
                {
                    Assert.AreEqual(esperada, VehiculosT1554.AnclaSobreLlanta(raiz, rueda), MargenSuspension, g.codigo + " " + rueda + ": ancla sobre la llanta (m, mundo)");
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
        }

        [Test]
        public void Grandes_RuedasVisualesEnMetrosDelMundo([ValueSource(typeof(VehiculosT1554), nameof(VehiculosT1554.Grandes))] (string codigo, float escala) g)
        {
            GameObject raiz = PrefabUtility.LoadPrefabContents(VehiculosT1554.RutaPrefab(g.codigo));
            try
            {
                SerializedObject so = new SerializedObject(raiz.GetComponent<RuedasVisualesCarro>());
                Assert.AreEqual(VehiculosT1554.RadioVisibleLlanta(raiz, "Rueda_DI"), so.FindProperty("radio").floatValue, MargenRadio, g.codigo + ": radio visible (m)");
                Assert.AreEqual(VehiculosT1554.DistanciaEjesMundo(raiz), so.FindProperty("distanciaEjes").floatValue, 0.05f, g.codigo + ": distancia entre ejes (m)");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
        }

        [Test]
        public void Grandes_AsientoYCamaraAlturaDelModeloYPuertaAUnMetro([ValueSource(typeof(VehiculosT1554), nameof(VehiculosT1554.Grandes))] (string codigo, float escala) g)
        {
            GameObject raiz = PrefabUtility.LoadPrefabContents(VehiculosT1554.RutaPrefab(g.codigo));
            try
            {
                Bounds modelo = VehiculosT1554.LimitesVisual(raiz);
                CamaraCarro camara = raiz.GetComponentInChildren<CamaraCarro>(true);
                Transform asiento = raiz.transform.Find("Asiento_Conductor") ?? raiz.transform.Find("Asiento");
                Transform punto = raiz.transform.Find("Punto_Puerta") ?? raiz.transform.Find("PuntoSubida");
                Assert.IsNotNull(asiento, g.codigo + ": asiento");
                Assert.IsNotNull(punto, g.codigo + ": punto de puerta");
                float ojos = camara.Config.AlturaOjos;
                Assert.AreEqual(Mathf.Max(0.3f, modelo.size.y * 0.78f - ojos) + ojos, asiento.position.y + ojos, MargenAsiento, g.codigo + ": ojos en primera persona (m)");
                Assert.AreEqual(modelo.size.y * 0.62f, camara.AlturaFocoActual, 0.02f, g.codigo + ": foco de la tercera persona (m)");
                float separacion = Mathf.Abs(punto.position.x) - modelo.size.x * 0.5f;
                Assert.AreEqual(1f, separacion, MargenPunto, g.codigo + ": separación del costado al punto de puerta (m)");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
        }

        // ------------------------------------------------------------------ drones

        [Test]
        public void Drones_RotoresSinOverrideDePosicion()
        {
            string[] prefabs = VehiculosT1554.PrefabsDeDrones();
            Assert.IsNotEmpty(prefabs);
            foreach (string ruta in prefabs)
            {
                GameObject raiz = PrefabUtility.LoadPrefabContents(ruta);
                try
                {
                    foreach (Transform rotor in VehiculosT1554.RotoresDe(raiz))
                    {
                        Assert.IsFalse(VehiculosT1554.TienePosicionSobrescrita(rotor), Path.GetFileName(ruta) + ": " + rotor.name + " tiene la posición cambiada respecto al FBX");
                    }
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(raiz);
                }
            }
        }

        [TestCase("DR01")]
        [TestCase("DR02_a")]
        [TestCase("DR02_b")]
        [TestCase("DR03_a")]
        [TestCase("DR04_a")]
        [TestCase("E13")]
        [TestCase("NV01")]
        public void Drones_RotoresConElPivoteCentradoYElEjeYDelDisco(string codigo)
        {
            GameObject raiz = PrefabUtility.LoadPrefabContents(VehiculosT1554.CarpetaDrones + "Prefabs/Dron_" + codigo + ".prefab");
            try
            {
                List<Transform> rotores = VehiculosT1554.RotoresDe(raiz);
                Assert.IsNotEmpty(rotores, codigo + ": rotores Rotor_*");
                foreach (Transform rotor in rotores)
                {
                    Assert.IsTrue(VehiculosT1554.DescentradoDelRotor(rotor, out float descentrado, out float radio, out bool ejeY), codigo + " " + rotor.name + ": sin malla");
                    Assert.Greater(radio, 0f, codigo + " " + rotor.name);
                    Assert.LessOrEqual(descentrado / radio, 0.06f, codigo + " " + rotor.name + ": el centro de la malla está lejos del eje de giro (tiembla)");
                    Assert.IsTrue(ejeY, codigo + " " + rotor.name + ": el eje Y local no es el del disco");
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
        }

        // ------------------------------------------------------------------ carros viejos en Barrio y Ciudad_Prologo

        [Test]
        public void Barrio_SinInstanciasDeCarrosViejosDeUnaPieza()
        {
            GameObject barrio = PrefabUtility.LoadPrefabContents(VehiculosT1554.RutaBarrio);
            try
            {
                Assert.AreEqual(0, VehiculosT1554.ContarViejos(new[] { barrio }), "Barrio.prefab todavía tiene Vxx.prefab viejos");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(barrio);
            }
        }

        [Test]
        public void CiudadPrologo_SinPrefabsViejosDeUnaPiezaEnElArchivo()
        {
            string texto = File.ReadAllText(VehiculosT1554.RutaEscenaPrologo);
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { VehiculosT1554.CarpetaPrefabsViejos }))
            {
                Assert.IsFalse(texto.Contains("guid: " + guid), "Ciudad_Prologo.unity todavía usa " + AssetDatabase.GUIDToAssetPath(guid));
            }
        }

        [Test]
        public void Barrio_ElArchivoYaNoUsaLosPrefabsViejosDeUnaPieza()
        {
            string texto = File.ReadAllText(VehiculosT1554.RutaBarrio);
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { VehiculosT1554.CarpetaPrefabsViejos }))
            {
                Assert.IsFalse(texto.Contains("guid: " + guid), "Barrio.prefab todavía usa " + AssetDatabase.GUIDToAssetPath(guid));
            }
        }

        // ------------------------------------------------------------------ V10 y V11 fuera de las listas

        [Test]
        public void ZonasEnCodigo_SinV10NiV11()
        {
            foreach (var zona in DisenoZonasTrafico.Zonas)
            {
                foreach (string codigo in VehiculosT1554.CodigosDescartados)
                {
                    CollectionAssert.DoesNotContain(zona.carros, codigo, "zona " + zona.nombre);
                }
            }
            foreach (string codigo in VehiculosT1554.CodigosDescartados)
            {
                CollectionAssert.DoesNotContain(DisenoZonasTrafico.Extras, codigo);
                CollectionAssert.DoesNotContain(DisenoZonasTrafico.CarrosNuevos, codigo);
            }
        }

        [Test]
        public void AssetsDeDatosDeTrafico_SinReferenciasAV10NiV11()
        {
            HashSet<string> rutas = VehiculosT1554.RutasDescartadas();
            foreach (string guid in AssetDatabase.FindAssets("t:ScriptableObject", new[] { "Assets/_Proyecto/Trafico", "Assets/_Proyecto/Vehiculos/Datos" }))
            {
                string ruta = AssetDatabase.GUIDToAssetPath(guid);
                ScriptableObject so = AssetDatabase.LoadAssetAtPath<ScriptableObject>(ruta);
                if (so == null)
                {
                    continue;
                }
                Assert.AreEqual(0, VehiculosT1554.ReferenciasEnListas(new SerializedObject(so), rutas, false), ruta + " todavía lista V10 o V11");
            }
        }

        [Test]
        public void CiudadPrologo_SinReferenciasAV10NiV11EnLosGuidsDeSusPrefabs()
        {
            string texto = File.ReadAllText(VehiculosT1554.RutaEscenaPrologo);
            foreach (string ruta in VehiculosT1554.RutasDescartadas())
            {
                string guid = AssetDatabase.AssetPathToGUID(ruta);
                if (string.IsNullOrEmpty(guid))
                {
                    continue;
                }
                Assert.IsFalse(texto.Contains("guid: " + guid), "Ciudad_Prologo.unity todavía referencia " + ruta);
            }
        }

        [Test]
        public void ReferenciasEnListas_ContarYQuitar_FuncionaEnUnaCopia()
        {
            ConfigCarrosZonas original = Resources.Load<ConfigCarrosZonas>("ConfigCarrosZonas");
            Assert.IsNotNull(original, "falta Resources/ConfigCarrosZonas");
            ConfigCarrosZonas copia = Object.Instantiate(original);
            try
            {
                SerializedObject so = new SerializedObject(copia);
                SerializedProperty extras = so.FindProperty("extrasGenerales");
                GameObject v10 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Proyecto/Trafico/Prefabs/CarroTrafico_V10.prefab");
                Assert.IsNotNull(v10);
                int antes = extras.arraySize;
                extras.arraySize++;
                extras.GetArrayElementAtIndex(extras.arraySize - 1).objectReferenceValue = v10.GetComponent<MechaCyberpunk.Trafico.Calle.CarroTrafico>();
                so.ApplyModifiedPropertiesWithoutUndo();
                HashSet<string> rutas = VehiculosT1554.RutasDescartadas();
                Assert.AreEqual(1, VehiculosT1554.ReferenciasEnListas(so, rutas, false));
                Assert.AreEqual(1, VehiculosT1554.ReferenciasEnListas(so, rutas, true));
                Assert.AreEqual(0, VehiculosT1554.ReferenciasEnListas(so, rutas, false));
                Assert.AreEqual(antes, new SerializedObject(copia).FindProperty("extrasGenerales").arraySize, "la lista vuelve a su tamaño");
            }
            finally
            {
                Object.DestroyImmediate(copia);
            }
        }
    }
}
