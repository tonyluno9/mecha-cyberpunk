using System.Linq;
using MechaCyberpunk.Arte.Editor;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>T-923 (playtest 9-nov): cuarto sin pósters y con la puerta P10, el muro viejo del I+D+I fuera del plano del forro y la entrada de la casa sin el marco liso.</summary>
    public class InterioresT923Pruebas
    {
        [OneTimeSetUp]
        public void Abrir()
        {
            EditorSceneManager.OpenScene(ColocarS2.RutaEscena, OpenSceneMode.Single);
        }

        [Test]
        public void Cuarto_SinPosters()
        {
            Transform cuarto = ColocarS2.Buscar(ColocarT923.RutaCuarto);
            Assert.IsNotNull(cuarto);
            Assert.AreEqual(0, cuarto.GetComponentsInChildren<Transform>(true).Count(t => t.name.StartsWith("Poster")));
        }

        [Test]
        public void PuertaDelCuarto_EsLaP10_ConSuHojaYSuCollider()
        {
            Transform hoja = ColocarS2.Buscar(ColocarT923.RutaCuarto + "/Puerta_Cuarto/Hoja");
            Assert.IsNotNull(hoja);
            Assert.IsNotNull(hoja.Find("Puerta_P10"), "la puerta P10 cuelga de la hoja");
            Assert.IsNull(hoja.Find("Puerta_L02"), "la L02 ya no está");
            Assert.IsNotNull(hoja.GetComponent<BoxCollider>());
            Assert.IsNotNull(hoja.GetComponent<MechaCyberpunk.Mundo.Lugares.HojaPuerta>(), "se abre con la misma HojaPuerta");
            Assert.IsNotNull(hoja.Find("Puerta_P10").GetComponentInChildren<MeshRenderer>());
        }

        [Test]
        public void IdiMuroFondo_NoComparteElPlanoDelForro()
        {
            Transform muro = ColocarS2.Buscar(ColocarT923.RutaIdi + "/MuroFondo");
            Transform forro = ColocarS2.Buscar("S2_Interiores/ConsultorioDoc/Forro/Pared_Fondo");
            float caraDeDentro = muro.GetComponent<Renderer>().bounds.max.z;
            Assert.Less(caraDeDentro, forro.GetComponent<Renderer>().bounds.center.z - 0.02f, "la cara de dentro del muro viejo queda detrás del forro");
        }

        [Test]
        public void EntradaDeLaCasa_ElMarcoLisoViejoNoSeDibuja()
        {
            Transform marco = ColocarS2.Buscar("Barrio/Manzana_4/Lugar_Casa/Puerta/Marco_T536");
            Assert.IsNotNull(marco);
            Assert.IsTrue(marco.GetComponentsInChildren<Renderer>(true).All(r => !r.enabled));
            Assert.IsNull(GameObject.Find(ColocarT923.NombreEntrada), "sin muro de respaldo que estorbe a la E de la puerta");
        }
    }
}
