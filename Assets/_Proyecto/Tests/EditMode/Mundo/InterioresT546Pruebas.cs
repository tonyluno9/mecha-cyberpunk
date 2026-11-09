using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>
    /// T-546 (plan de la noche 3-nov, paso 4): los cinco interiores armados con assets que ya había (habitación, consultorio del Doc, taller de
    /// Tino, casino y entrada de armas de El Mercado), con su luz y sus texturas generadas.
    /// </summary>
    public class InterioresT546Pruebas
    {
        private Scene escena;
        private Transform raiz;

        [OneTimeSetUp]
        public void Abrir()
        {
            escena = EditorSceneManager.OpenScene("Assets/_Proyecto/Escenas/Ciudad_Prologo.unity", OpenSceneMode.Additive);
            raiz = escena.GetRootGameObjects().First(g => g.name == "T546_Interiores").transform;
        }

        [OneTimeTearDown]
        public void Cerrar()
        {
            EditorSceneManager.CloseScene(escena, true);
        }

        // S2 (playtest 5-nov): el consultorio y el taller de Tino se rehicieron en S2_Interiores (MundoS2Pruebas); los de T-546 quedan apagados.
        [TestCase("Casino")]
        [TestCase("EntradaArmasMercado")]
        public void CadaInterior_TieneCosasYLuz(string nombre)
        {
            Transform grupo = raiz.Find(nombre);
            Assert.IsNotNull(grupo, nombre);
            Assert.Greater(grupo.GetComponentsInChildren<Renderer>(true).Length, 0, "Algo que ver");
            Assert.Greater(grupo.GetComponentsInChildren<Light>(true).Length, 0, "Su propia luz");
        }

        [Test]
        public void Habitacion_ViveEnSuCuartoAparte_SinPosteresYFocoCalido()
        {
            Assert.IsNull(raiz.Find("Habitacion"), "T-550: la habitación ya es un cuarto aparte (T550_Cuartos)");
            Transform h = escena.GetRootGameObjects().First(g => g.name == "T550_Cuartos").transform.Find("Cuarto_Habitacion");
            // T-923 (playtest 9-nov): los pósters tenían la textura rota y se quitaron del cuarto.
            Assert.IsNull(h.Find("Poster_1"), "T-923: sin pósters");
            // S2 (playtest 5-nov): el foco es pelón y cuelga de su cable, como en habitacion.png.
            Assert.Less(h.Find("S2_Cuarto/Foco_Pelon").lossyScale.y, 0.2f, "Foco chico de cuarto");
            Light foco = h.Find("Luces/FocoColgante_Luz").GetComponent<Light>();
            Assert.Greater(foco.color.r, foco.color.b, "Luz cálida");
        }

        [Test]
        public void Casino_AlfombraEncimaDelPiso_TragamonedasEnHilerasSinSusFocos()
        {
            Transform c = raiz.Find("Casino");
            Transform alfombra = c.Find("AlfombraEstampada");
            Assert.Greater(alfombra.position.y, 0.24f, "Encima del piso del casino (0.24 m), si no queda tapada");
            Material m = alfombra.GetComponent<Renderer>().sharedMaterial;
            Assert.AreEqual("T930_AlfombraGrecas", m.mainTexture.name); // T-930: alfombra de grecas según la referencia
            Assert.IsTrue(m.IsKeywordEnabled("_EMISSION"), "Brilla un poco: con la noche se ve negra");
            Transform[] maquinas = c.Cast<Transform>().Where(t => t.name == "Tragamonedas_Adorno").ToArray();
            Assert.GreaterOrEqual(maquinas.Length, 12, "Hileras de tragamonedas");
            Assert.IsTrue(maquinas.All(t => t.GetComponentsInChildren<Light>(true).Length == 0),
                "Sin foco propio: se comerían el límite de luces de la alfombra");
            Assert.IsTrue(maquinas.All(t => t.GetComponentsInChildren<MonoBehaviour>(true).Length == 0), "Son de adorno: la que cobra es la de siempre");
        }
    }
}
