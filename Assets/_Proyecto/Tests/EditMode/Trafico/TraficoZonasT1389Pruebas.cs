using System.Linq;
using MechaCyberpunk.Arte.Editor;
using MechaCyberpunk.Trafico.Calle;
using MechaCyberpunk.Trafico.Datos;
using MechaCyberpunk.Trafico.Persecucion;
using MechaCyberpunk.Vehiculos.Carro;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Tests
{
    /// <summary>T-1389a: todos los carros nuevos entran al tráfico por zona, las cajas de zona no se encimen y Carro_V11 es un carro completo.</summary>
    public class TraficoZonasT1389Pruebas
    {
        private const string RutaV11 = "Assets/_Proyecto/Vehiculos/Prefabs/Carro_V11.prefab";

        [Test]
        public void Diseno_CadaCarroNuevoApareceEnAlMenosUnaZonaOEnLosExtras()
        {
            foreach (string codigo in DisenoZonasTrafico.CarrosNuevos)
            {
                bool enZona = DisenoZonasTrafico.Zonas.Any(z => z.carros.Contains(codigo));
                Assert.IsTrue(enZona || DisenoZonasTrafico.Extras.Contains(codigo), codigo + " no sale en ninguna zona");
            }
        }

        [Test]
        public void Diseno_LasCuatroZonasNuevasTienenCajaYCarros()
        {
            foreach (string nombre in new[] { "Malecon", "Cerro", "Autopista", "Puerto" })
            {
                Assert.IsTrue(ColocarT1364ZonasTrafico.Cajas.Any(c => c.Zona == nombre), nombre);
                Assert.IsTrue(DisenoZonasTrafico.Zonas.Any(z => z.nombre == nombre), nombre);
            }
        }

        [Test]
        public void Diseno_CadaCajaTieneSuZonaYViceversa()
        {
            foreach (var c in ColocarT1364ZonasTrafico.Cajas)
            {
                Assert.IsTrue(DisenoZonasTrafico.Zonas.Any(z => z.nombre == c.Zona), "caja sin zona: " + c.Zona);
            }
            foreach (var z in DisenoZonasTrafico.Zonas)
            {
                Assert.IsTrue(ColocarT1364ZonasTrafico.Cajas.Any(c => c.Zona == z.nombre), "zona sin caja: " + z.nombre);
            }
        }

        [Test]
        public void Diseno_LasCajasNoSeEncimen()
        {
            var cajas = ColocarT1364ZonasTrafico.Cajas;
            for (int i = 0; i < cajas.Length; i++)
            {
                for (int j = i + 1; j < cajas.Length; j++)
                {
                    Assert.IsFalse(cajas[i].Toca(cajas[j]), cajas[i].Objeto + " se encima con " + cajas[j].Objeto);
                }
            }
        }

        [Test]
        public void Diseno_CadaZonaCubreSuAreaDelPlan()
        {
            // Puntos de docs/plan-zonas-18nov.md: calles de zona, puertas y rampas.
            (string zona, Vector3 punto)[] puntos =
            {
                ("Malecon", new Vector3(-47f, 0f, -215f)), ("Malecon", new Vector3(47f, 0f, -230f)), ("Malecon", new Vector3(-300f, 0f, -205f)), ("Malecon", new Vector3(100f, 0f, -250f)),
                ("Puerto", new Vector3(160f, 0f, -215f)), ("Puerto", new Vector3(300f, 0f, -45f)), ("Puerto", new Vector3(325f, -1f, -245f)),
                ("Cerro", new Vector3(-305f, 0f, 0f)), ("Cerro", new Vector3(-360f, 10f, 175f)), ("Cerro", new Vector3(-470f, 0f, -160f)),
                ("Autopista", new Vector3(-330f, 10f, 250f)), ("Autopista", new Vector3(30f, 10f, 250f)), ("Autopista", new Vector3(240f, 10f, 250f)),
                ("Autopista", new Vector3(300f, 5f, 190f)), ("Autopista", new Vector3(300f, 0f, 60f)),
            };
            foreach (var p in puntos)
            {
                Assert.IsTrue(ColocarT1364ZonasTrafico.Cajas.Any(c => c.Zona == p.zona && c.Contiene(p.punto)), p.zona + " no cubre " + p.punto);
            }
        }

        [Test]
        public void Diseno_ElCerroNoLlevaCamionesNiTrailer()
        {
            var cerro = DisenoZonasTrafico.Zonas.First(z => z.nombre == "Cerro");
            foreach (string pesado in new[] { "V16", "V17", "V18" })
            {
                CollectionAssert.DoesNotContain(cerro.carros, pesado);
            }
        }

        [Test]
        public void Diseno_PuertoTieneTrailer_YMaleconSuperdeportivosYMotos()
        {
            CollectionAssert.Contains(DisenoZonasTrafico.Zonas.First(z => z.nombre == "Puerto").carros, "V18");
            var malecon = DisenoZonasTrafico.Zonas.First(z => z.nombre == "Malecon").carros;
            foreach (string c in new[] { "V22", "V23", "V24", "MT01", "MT02" })
            {
                CollectionAssert.Contains(malecon, c);
            }
        }

        [Test]
        public void Config_CoincideConElDisenoYTodosLosCarrosNuevosSalen()
        {
            ConfigCarrosZonas config = Resources.Load<ConfigCarrosZonas>("ConfigCarrosZonas");
            Assert.IsNotNull(config);
            foreach (var d in DisenoZonasTrafico.Zonas)
            {
                ConfigCarrosZonas.Zona z = config.Buscar(d.nombre);
                Assert.IsNotNull(z, d.nombre);
                CollectionAssert.DoesNotContain(z.carros, null, d.nombre);
                Assert.AreEqual(d.carros.Length, z.carros.Count, d.nombre);
            }
            foreach (string codigo in DisenoZonasTrafico.CarrosNuevos)
            {
                string ruta = "Assets/_Proyecto/Trafico/Prefabs/CarroTrafico_" + codigo + ".prefab";
                CarroTrafico carro = AssetDatabase.LoadAssetAtPath<CarroTrafico>(ruta);
                Assert.IsNotNull(carro, ruta);
                bool sale = config.Zonas.Any(z => z.carros.Contains(carro)) || config.ExtrasGenerales.Contains(carro);
                Assert.IsTrue(sale, codigo + " no sale en el tráfico");
            }
        }

        [Test]
        public void Prefabs_ExisteUnaZonaTraficoPorZona()
        {
            foreach (string nombre in DisenoZonasTrafico.NombresDeZonas())
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Proyecto/Trafico/Prefabs/Zonas/ZonaTrafico_" + nombre + ".prefab");
                Assert.IsNotNull(prefab, nombre);
                Assert.AreEqual(nombre, prefab.GetComponent<ZonaTrafico>().Nombre);
            }
        }

        [Test]
        public void CarroV11_TieneRuedasColliderYRigidbodyComoLosDemas()
        {
            GameObject v11 = AssetDatabase.LoadAssetAtPath<GameObject>(RutaV11);
            Assert.IsNotNull(v11, "falta " + RutaV11);
            GameObject referencia = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Proyecto/Vehiculos/Prefabs/Carro_V12.prefab");
            Assert.IsNotNull(referencia);
            Assert.IsNotNull(v11.GetComponent<Rigidbody>(), "Rigidbody");
            Assert.IsNotNull(v11.GetComponent<RuedasVisualesCarro>(), "RuedasVisualesCarro");
            Assert.Greater(v11.GetComponentsInChildren<Collider>(true).Length, 0, "colliders");
            Assert.GreaterOrEqual(v11.GetComponentsInChildren<Transform>(true).Count(t => t.name.StartsWith("Rueda_")), 4, "anclas de ruedas");
            Assert.AreEqual(referencia.GetComponentsInChildren<Collider>(true).Length, v11.GetComponentsInChildren<Collider>(true).Length, "mismos colliders que V12");
        }

        [Test]
        public void PatrullaCamioneta_ExisteYEstaConectadaAlGestor()
        {
            GameObject patrulla = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Proyecto/Trafico/Prefabs/PatrullaCamioneta.prefab");
            Assert.IsNotNull(patrulla);
            PatrullaMundo mundo = patrulla.GetComponent<PatrullaMundo>();
            Assert.IsNotNull(mundo);
            Assert.IsNotNull(patrulla.GetComponent<Rigidbody>());
            Assert.Greater(patrulla.GetComponentsInChildren<Collider>(true).Length, 0);
            GameObject barrio = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Proyecto/Mundo/Prefabs/Barrio.prefab");
            GestorPersecucion gestor = barrio.GetComponentInChildren<GestorPersecucion>(true);
            Assert.IsNotNull(gestor);
            Assert.AreEqual(mundo, new SerializedObject(gestor).FindProperty("prefabCamioneta").objectReferenceValue);
        }
    }
}
