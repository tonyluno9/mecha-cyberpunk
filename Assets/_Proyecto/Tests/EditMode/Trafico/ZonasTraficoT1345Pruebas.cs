using System.Collections.Generic;
using MechaCyberpunk.Trafico.Calle;
using MechaCyberpunk.Trafico.Datos;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Tests
{
    public class ZonasTraficoT1345Pruebas
    {
        private readonly List<GameObject> creados = new List<GameObject>();

        [TearDown]
        public void LimpiarCreados()
        {
            foreach (GameObject g in creados)
            {
                Object.DestroyImmediate(g);
            }
            creados.Clear();
        }

        private CarroTrafico NuevoCarro(string nombre)
        {
            GameObject g = new GameObject(nombre);
            creados.Add(g);
            return g.AddComponent<CarroTrafico>();
        }

        [Test]
        public void Elegir_SinZona_EsIgualQueGeneralPorIndice()
        {
            List<CarroTrafico> general = new List<CarroTrafico> { NuevoCarro("a"), NuevoCarro("b"), NuevoCarro("c") };
            for (int i = 0; i < 10; i++)
            {
                Assert.AreSame(general[i % 3], TraficoCalle.Elegir(general, null, 1f, 0f, i));
                Assert.AreSame(general[i % 3], TraficoCalle.Elegir(general, new List<CarroTrafico>(), 1f, 0f, i));
            }
        }

        [Test]
        public void Elegir_MezclaUno_SiempreDeLaZona()
        {
            List<CarroTrafico> general = new List<CarroTrafico> { NuevoCarro("g") };
            List<CarroTrafico> zona = new List<CarroTrafico> { NuevoCarro("z1"), NuevoCarro("z2") };
            for (int i = 0; i < 8; i++)
            {
                Assert.AreSame(zona[i % 2], TraficoCalle.Elegir(general, zona, 1f, 0.999f, i));
            }
        }

        [Test]
        public void Elegir_MezclaCero_NuncaDeLaZona()
        {
            List<CarroTrafico> general = new List<CarroTrafico> { NuevoCarro("g1"), NuevoCarro("g2") };
            List<CarroTrafico> zona = new List<CarroTrafico> { NuevoCarro("z") };
            for (int i = 0; i < 8; i++)
            {
                Assert.AreSame(general[i % 2], TraficoCalle.Elegir(general, zona, 0f, 0f, i));
            }
        }

        [Test]
        public void Contiene_CajaGirada_RespetaLaRotacion()
        {
            GameObject g = new GameObject("zona");
            creados.Add(g);
            ZonaTrafico zona = g.AddComponent<ZonaTrafico>();
            zona.Definir("Prueba", new Vector3(40f, 10f, 10f));
            g.transform.position = new Vector3(100f, 0f, 0f);
            Assert.IsTrue(zona.Contiene(new Vector3(115f, 0f, 0f)), "sin girar, 15 m en X cabe");
            Assert.IsFalse(zona.Contiene(new Vector3(100f, 0f, 15f)), "sin girar, 15 m en Z no cabe");
            g.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            Assert.IsFalse(zona.Contiene(new Vector3(115f, 0f, 0f)), "girada 90 grados, 15 m en X ya no cabe");
            Assert.IsTrue(zona.Contiene(new Vector3(100f, 0f, 15f)), "girada 90 grados, 15 m en Z cabe");
            Assert.IsFalse(zona.Contiene(new Vector3(100f, 8f, 0f)), "fuera en altura");
        }

        [Test]
        public void Config_ExisteConLasSeisZonasYSinCarrosNulos()
        {
            ConfigCarrosZonas config = Resources.Load<ConfigCarrosZonas>("ConfigCarrosZonas");
            Assert.IsNotNull(config);
            Assert.AreEqual(6, config.Zonas.Count);
            foreach (string nombre in new[] { "Deshuesadero", "Malecon", "NeonAlto", "LasLomas", "Avenida", "Puerto" })
            {
                ConfigCarrosZonas.Zona z = config.Buscar(nombre);
                Assert.IsNotNull(z, nombre);
                Assert.Greater(z.carros.Count, 0, nombre);
                CollectionAssert.DoesNotContain(z.carros, null, nombre);
            }
            Assert.Greater(config.ExtrasGenerales.Count, 0);
            CollectionAssert.DoesNotContain(config.ExtrasGenerales, null);
        }

        [Test]
        public void Prefabs_ExistenLosSeisDeZonaYLosCarrosV12aV24()
        {
            foreach (string nombre in new[] { "Deshuesadero", "Malecon", "NeonAlto", "LasLomas", "Avenida", "Puerto" })
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Proyecto/Trafico/Prefabs/Zonas/ZonaTrafico_" + nombre + ".prefab");
                Assert.IsNotNull(prefab, nombre);
                Assert.AreEqual(nombre, prefab.GetComponent<ZonaTrafico>().Nombre);
            }
            for (int n = 12; n <= 24; n++)
            {
                Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<CarroTrafico>("Assets/_Proyecto/Trafico/Prefabs/CarroTrafico_V" + n + ".prefab"), "V" + n);
            }
        }
    }
}
