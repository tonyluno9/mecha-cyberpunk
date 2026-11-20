using System.Collections.Generic;
using System.Linq;
using MechaCyberpunk.Arte.Editor;
using MechaCyberpunk.Core;
using MechaCyberpunk.Core.Mundo;
using MechaCyberpunk.Personaje.Visual;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Tests
{
    public class PeatonesZonaT1409Pruebas
    {
        private class ZonaFalsa : IZonaDelMundo
        {
            public string NombreZona { get; set; }
            public Rect Caja;
            public bool ContienePunto(Vector3 p) => Caja.Contains(new Vector2(p.x, p.z));
        }

        [SetUp]
        public void SetUp() => LimpiarRegistro();

        [TearDown]
        public void TearDown() => LimpiarRegistro();

        private static void LimpiarRegistro()
        {
            foreach (IZonaDelMundo z in Registro<IZonaDelMundo>.Todos.ToList())
            {
                Registro<IZonaDelMundo>.Quitar(z);
            }
        }

        [Test]
        public void Elegir_TiradaCero_DaLaPersona()
        {
            Assert.AreEqual(SelectorPeatonZona.Persona, SelectorPeatonZona.Elegir(new[] { 1f, 1f }, 2f, 0f));
        }

        [Test]
        public void Elegir_RecorreLosPesosEnOrden()
        {
            float[] pesos = { 1f, 3f };
            // total 6: persona [0,2), variante 0 [2,3), variante 1 [3,6)
            Assert.AreEqual(SelectorPeatonZona.Persona, SelectorPeatonZona.Elegir(pesos, 2f, 0.3f));
            Assert.AreEqual(0, SelectorPeatonZona.Elegir(pesos, 2f, 0.4f));
            Assert.AreEqual(1, SelectorPeatonZona.Elegir(pesos, 2f, 0.6f));
            Assert.AreEqual(1, SelectorPeatonZona.Elegir(pesos, 2f, 1f));
        }

        [Test]
        public void Elegir_PesoCeroNoSale_YTodoCeroDaPersona()
        {
            Assert.AreEqual(1, SelectorPeatonZona.Elegir(new[] { 0f, 5f }, 0f, 0.01f));
            Assert.AreEqual(SelectorPeatonZona.Persona, SelectorPeatonZona.Elegir(new[] { 0f, -1f }, 0f, 0.5f));
        }

        [Test]
        public void Elegir_ConMuchasTiradas_RespetaLasProporciones()
        {
            var azar = new System.Random(7);
            float[] pesos = { 1f, 3f };
            int[] cuenta = new int[3];
            const int tiradas = 20000;
            for (int i = 0; i < tiradas; i++)
            {
                cuenta[SelectorPeatonZona.Elegir(pesos, 2f, (float)azar.NextDouble()) + 1]++;
            }
            Assert.AreEqual(2f / 6f, cuenta[0] / (float)tiradas, 0.02f);
            Assert.AreEqual(3f / 6f, cuenta[2] / (float)tiradas, 0.02f);
        }

        [Test]
        public void BuscarZona_IgnoraMayusculas_YDevuelveMenosUnoSinZona()
        {
            string[] nombres = { "Malecon", "Puerto" };
            Assert.AreEqual(1, SelectorPeatonZona.BuscarZona(nombres, "puerto"));
            Assert.AreEqual(-1, SelectorPeatonZona.BuscarZona(nombres, "Cerro"));
            Assert.AreEqual(-1, SelectorPeatonZona.BuscarZona(nombres, null));
            Assert.AreEqual(-1, SelectorPeatonZona.BuscarZona(null, "Puerto"));
        }

        [Test]
        public void ZonasMundo_NombreEn_DevuelveLaZonaDelPunto_OnullFuera()
        {
            Registro<IZonaDelMundo>.Agregar(new ZonaFalsa { NombreZona = "Puerto", Caja = new Rect(0, 0, 10, 10) });
            Assert.AreEqual("Puerto", ZonasMundo.NombreEn(new Vector3(5, 0, 5)));
            Assert.IsNull(ZonasMundo.NombreEn(new Vector3(50, 0, 50)));
        }

        [Test]
        public void ConfigPeatonesZona_ZonaDesconocida_NoHayZona()
        {
            ConfigPeatonesZona config = ScriptableObject.CreateInstance<ConfigPeatonesZona>();
            try
            {
                GameObject v = config.Elegir("Nada", 0.5f, out bool hayZona);
                Assert.IsFalse(hayZona);
                Assert.IsNull(v);
                v = config.Elegir(null, 0.5f, out hayZona);
                Assert.IsFalse(hayZona);
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void PeatonVariado_SinConfigZona_ElegirPorZona_UsaLaListaGeneral()
        {
            GameObject visual = new GameObject("VisualPrueba");
            GameObject raiz = new GameObject("PeatonPrueba");
            try
            {
                PeatonVariado p = raiz.AddComponent<PeatonVariado>();
                p.ConfigurarVariantes(new[] { visual }, 1f);
                p.ElegirPorZona("Malecon", 0f, 0f, 0.5f);
                Assert.IsNotNull(p.VarianteElegida, "sin config debe comportarse como T-1389b");
            }
            finally
            {
                Object.DestroyImmediate(visual);
                Object.DestroyImmediate(raiz);
            }
        }

        [Test]
        public void PeatonVariado_ConConfigZona_ElegirPorZona_CreaElVisualDeLaZona_YZonaDesconocidaUsaLaGeneral()
        {
            GameObject pescador = new GameObject("Pescador");
            new GameObject("MarcaPescador").transform.SetParent(pescador.transform);
            GameObject general = new GameObject("General");
            GameObject raiz = new GameObject("PeatonPrueba");
            GameObject raiz2 = new GameObject("PeatonPrueba2");
            ConfigPeatonesZona config = ScriptableObject.CreateInstance<ConfigPeatonesZona>();
            try
            {
                var zona = new ConfigPeatonesZona.Zona { nombre = "Malecon", pesoPersona = 0f };
                zona.entradas.Add(new ConfigPeatonesZona.Entrada { visual = pescador, peso = 1f });
                config.Definir(new List<ConfigPeatonesZona.Zona> { zona });

                PeatonVariado p = raiz.AddComponent<PeatonVariado>();
                p.ConfigurarVariantes(new[] { general }, 1f);
                p.ConfigurarZona(config);
                p.ElegirPorZona("Malecon", 0.5f, 0f, 0f);
                Assert.IsNotNull(p.VarianteElegida);
                Assert.IsNotNull(p.VarianteElegida.transform.Find("MarcaPescador"), "debe ser el visual de la zona, no el general");

                PeatonVariado q = raiz2.AddComponent<PeatonVariado>();
                q.ConfigurarVariantes(new[] { general }, 1f);
                q.ConfigurarZona(config);
                q.ElegirPorZona(null, 0.5f, 0f, 0f);
                Assert.IsNotNull(q.VarianteElegida, "sin zona usa la lista general");
            }
            finally
            {
                Object.DestroyImmediate(pescador);
                Object.DestroyImmediate(general);
                Object.DestroyImmediate(raiz);
                Object.DestroyImmediate(raiz2);
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        [Ignore("T-1482: zonas nuevas quitadas del mapa (revertir mapa 19-nov)")]
        public void Diseno_CubreMalecon_Puerto_Cerro_Autopista_ConLosPersonajesPedidos()
        {
            string[] zonas = PeatonesZonaT1409.Diseno.Select(d => d.zona).ToArray();
            foreach (string z in new[] { "Malecon", "Puerto", "Cerro", "Autopista" })
            {
                CollectionAssert.Contains(zonas, z);
            }
            AssertTiene("Malecon", "NP07", "NP08", "NP09", "NP13", "NP14");
            AssertTiene("Puerto", "NP11", "NP13", "NP14");
            AssertTiene("Cerro", "NP10", "NP12", "NP13", "NP14");
            AssertTiene("Autopista", "NP13", "NP14");
            Assert.IsFalse(PeatonesZonaT1409.VariantesDe("Puerto").Any(v => v.variante.StartsWith("NP07")), "el pescador no sale en el puerto");
        }

        private static void AssertTiene(string zona, params string[] codigos)
        {
            var variantes = PeatonesZonaT1409.VariantesDe(zona);
            Assert.IsNotNull(variantes, zona);
            foreach (string c in codigos)
            {
                Assert.IsTrue(variantes.Any(v => v.variante.StartsWith(c + "_") && v.peso > 0f), zona + " sin " + c);
            }
        }

        [Test]
        [Ignore("T-1482: zonas nuevas quitadas del mapa (revertir mapa 19-nov)")]
        public void ConfigBase_SiExiste_CubreLas4ZonasNuevas_YPeatonLaTiene()
        {
            ConfigPeatonesZona config = AssetDatabase.LoadAssetAtPath<ConfigPeatonesZona>(PeatonesZonaT1409.RutaConfig);
            if (config == null)
            {
                Assert.Ignore("Falta correr PeatonesZonaT1409.Aplicar.");
            }
            foreach (string z in new[] { "Malecon", "Puerto", "Cerro", "Autopista" })
            {
                ConfigPeatonesZona.Zona zona = config.Buscar(z);
                Assert.IsNotNull(zona, z);
                Assert.IsTrue(zona.entradas.All(e => e.visual != null), z + " con visual vacío");
                Assert.AreEqual(PeatonesZonaT1409.VariantesDe(z).Count, zona.entradas.Count, z);
            }
            GameObject peaton = AssetDatabase.LoadAssetAtPath<GameObject>(PeatonesZonaT1409.RutaPeaton);
            Assert.AreEqual(config, peaton.GetComponent<PeatonVariado>().ConfigZona);
        }
    }
}
