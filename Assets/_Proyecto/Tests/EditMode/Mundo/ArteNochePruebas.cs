using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>
    /// T-419: los 39 modelos de la noche tienen prefab, el Barrio lleva las fachadas _cy en lugar de las de T-250 y los props de calle
    /// no quedan encima de las puertas de los lugares; el dron de la policía usa CY07b.
    /// </summary>
    public class ArteNochePruebas
    {
        private const string RutaBarrio = "Assets/_Proyecto/Mundo/Prefabs/Barrio.prefab";
        private const string RutaDron = "Assets/_Proyecto/Trafico/Prefabs/Dron.prefab";
        private const string RutaJaula = "Assets/_Proyecto/Arena/Prefabs/JaulaClandestino.prefab";
        private const float DistanciaPuerta = 3f;
        private const float AltoMaximoCalle = 4f;

        private static readonly string[] Codigos =
        {
            "Clandestino/Prefabs/A08_Marcador", "Clandestino/Prefabs/A11a_PortalSotano", "Clandestino/Prefabs/A11b_EscaleraSotano",
            "Calle/Prefabs/C01_FachadaLocal_cy", "Calle/Prefabs/C02_FachadaVivienda_cy", "Calle/Prefabs/C03_FachadaPisoAlto_cy",
            "Calle/Prefabs/C13_PuestoCiber", "Calle/Prefabs/C16_Tambo", "Calle/Prefabs/C17_Huacales", "Calle/Prefabs/C18_BarreraCalle",
            "Calle/Prefabs/C19_Moto", "Calle/Prefabs/C20a_PisoVentana_cy", "Calle/Prefabs/C20b_PisoDosVentanas_cy", "Calle/Prefabs/C20d_Macetas",
            "Calle/Prefabs/C21_PisoLiso_cy", "Calle/Prefabs/C22_Cornisa_cy", "Calle/Prefabs/C23_Franja",
            "Calle/Prefabs/CY01_ProyectorHolo", "Calle/Prefabs/CY02_PantallaVertical", "Calle/Prefabs/CY03_MazoCables", "Calle/Prefabs/CY04_AntenaAzotea",
            "Calle/Prefabs/CY05_ServidorCallejero", "Calle/Prefabs/CY06_Expendedora", "Calle/Prefabs/CY07a_PlataformaDron", "Calle/Prefabs/CY07b_Dron",
            "Calle/Prefabs/CY08_CamaraCorporativa", "Calle/Prefabs/CY09_PuestoImplantes", "Calle/Prefabs/CY10_CargadorCalle",
            "Exteriores/Prefabs/E01_EntradaCasino", "Exteriores/Prefabs/E02_PortonMercadoNegro", "Exteriores/Prefabs/E03_PantallaGigante",
            "TallerCasa/Prefabs/H06_TeleVieja", "TallerCasa/Prefabs/H07_Sofa", "TallerCasa/Prefabs/H08_EstufaTarja", "TallerCasa/Prefabs/H09_RepisaVeladoras",
            "TallerCasa/Prefabs/H11_Buro", "TallerCasa/Prefabs/T06_Soldadora", "TallerCasa/Prefabs/T07_GruaMotor", "TallerCasa/Prefabs/T08_RepisaTaller",
        };

        [Test]
        public void Prefabs_Los39_TienenVisualConMaterialURP()
        {
            Assert.AreEqual(39, Codigos.Length);
            foreach (string codigo in Codigos)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Arte/" + codigo + ".prefab");
                Assert.IsNotNull(prefab, codigo + " no tiene prefab");
                Transform visual = prefab.transform.Find("Visual");
                Assert.IsNotNull(visual, codigo + " no tiene hijo Visual");
                Renderer r = visual.GetComponentInChildren<Renderer>();
                Assert.IsNotNull(r, codigo + " no tiene renderer");
                Assert.AreEqual("Universal Render Pipeline/Lit", r.sharedMaterial.shader.name, codigo);
                Assert.IsNotNull(r.sharedMaterial.GetTexture("_BaseMap"), codigo + " sin textura");
            }
        }

        [Test]
        public void Barrio_FachadasT250_ApagadasYLasCyEncendidas()
        {
            GameObject barrio = AssetDatabase.LoadAssetAtPath<GameObject>(RutaBarrio);
            Transform[] todos = barrio.GetComponentsInChildren<Transform>(true);
            Transform[] viejas = todos.Where(t => t.name == "C01_FachadaLocal" || t.name == "C02_FachadaVivienda" || t.name == "C03_FachadaPisoAlto").ToArray();
            Transform[] nuevas = todos.Where(t => t.name.EndsWith("_cy") && t.name.StartsWith("C0")).ToArray();
            Assert.Greater(viejas.Length, 100);
            Assert.AreEqual(viejas.Length, nuevas.Length, "Cada fachada de T-250 debe tener su _cy");
            Assert.IsTrue(viejas.All(t => !t.gameObject.activeSelf), "Las fachadas de T-250 se apagan, no se borran");
            Assert.IsTrue(nuevas.All(t => t.gameObject.activeSelf));
        }

        [Test]
        public void Barrio_PropsDeCalle_NoTapanPuertasNiSonGigantes()
        {
            GameObject barrio = AssetDatabase.LoadAssetAtPath<GameObject>(RutaBarrio);
            Transform grupo = barrio.transform.Find("ArteNoche");
            Assert.IsNotNull(grupo, "Falta el grupo ArteNoche");
            Vector3[] puertas = barrio.GetComponentsInChildren<Transform>(true).Where(t => t.name == "Puerta" && t.parent.name.StartsWith("Lugar_")).Select(t => t.position).ToArray();
            Assert.GreaterOrEqual(puertas.Length, 5);
            int props = 0;
            foreach (string sub in new[] { "Calle_suburbios", "Calle_tianguis", "Calle_neon", "Tianguis", "Barreras", "Motos", "Mercado" })
            {
                Transform g = grupo.Find(sub);
                Assert.IsNotNull(g, "Falta " + sub);
                foreach (Transform prop in g)
                {
                    props++;
                    foreach (Vector3 p in puertas)
                    {
                        Vector2 d = new Vector2(prop.position.x - p.x, prop.position.z - p.z);
                        Assert.Greater(d.magnitude, DistanciaPuerta, prop.name + " queda sobre una puerta en " + p);
                    }
                    Assert.Less(prop.position.y, 0.5f, prop.name + " flota");
                    Assert.Less(prop.lossyScale.y, AltoMaximoCalle, prop.name + " es gigante");
                }
            }
            Assert.Greater(props, 100);
        }

        [Test]
        public void Dron_UsaCY07b_YApagaElGreybox()
        {
            GameObject dron = AssetDatabase.LoadAssetAtPath<GameObject>(RutaDron);
            Transform visual = dron.transform.Find("Visual_Noche");
            Assert.IsNotNull(visual);
            Assert.IsNotNull(visual.GetComponentInChildren<MeshRenderer>());
            Transform cuerpo = dron.transform.Find("Cuerpo");
            Assert.IsFalse(cuerpo.GetComponent<MeshRenderer>().enabled);
        }

        [Test]
        public void Jaula_TieneMarcadorYEscalera()
        {
            GameObject jaula = AssetDatabase.LoadAssetAtPath<GameObject>(RutaJaula);
            Transform grupo = jaula.transform.Find("ArteNoche");
            Assert.IsNotNull(grupo);
            Assert.AreEqual(2, grupo.Cast<Transform>().Count(t => t.name.StartsWith("A08_Marcador")));
            Assert.AreEqual(1, grupo.Cast<Transform>().Count(t => t.name.StartsWith("A11b_EscaleraSotano")));
        }
    }
}
