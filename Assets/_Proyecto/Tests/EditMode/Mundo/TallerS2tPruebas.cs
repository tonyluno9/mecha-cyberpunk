using System.Linq;
using MechaCyberpunk.Arte.Editor;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>T-862 (playtest 6-nov tarde): el taller con un solo letrero (EL CANEPA / TALLER MECÁNICO) y Canepa con el modelo del Mecánico.</summary>
    public class TallerS2tPruebas
    {
        [Test]
        public void Taller_UnLetreroYElMecanicoDentro()
        {
            EditorSceneManager.OpenScene(ColocarS2.RutaEscena, OpenSceneMode.Single);
            Transform[] activos = ColocarS2.Todos().Where(t => t.gameObject.activeInHierarchy).ToArray();
            Assert.IsFalse(activos.Any(t => t.name.StartsWith("CY02_PantallaVertical") && ColocarS2tTaller.FrentePorton.Contains(t.position)), "sin pantalla a media puerta");
            Assert.IsFalse(activos.Any(t => t.name == "L11" && ColocarS2tTaller.ArribaDelTaller.Contains(t.position)), "sin el letrero atravesado");
            // T-883: el letrero es el L15 de Tony (EL CANEPA / TALLER MECÁNICO pintado); el C15a con texto ya no se ve.
            Transform raiz = ColocarS2.Buscar(ColocarS2tTaller.NombreRaiz);
            Assert.AreEqual(1, raiz.Cast<Transform>().Count(t => t.name == "Letrero_L15" && t.gameObject.activeSelf), "un solo letrero");
            Assert.IsFalse(raiz.GetComponentsInChildren<UnityEngine.UI.Text>().Any(), "sin el texto encima");
            Transform c15a = ColocarS2.Buscar(ColocarS2tTaller.RutaLetrero);
            Assert.IsTrue(c15a == null || !c15a.gameObject.activeInHierarchy);
            Transform canepa = ColocarS2.Buscar(ColocarS2tTaller.RutaCanepa);
            Animator animador = canepa.GetComponentsInChildren<Animator>().Single();
            StringAssert.Contains("Mecanico", animador.transform.root == canepa.root ? animador.GetComponentInParent<Transform>().parent.parent.name : "", "el modelo es el del Mecánico");
            Assert.IsNotNull(animador.runtimeAnimatorController, "con su controller (Idle)");
            Bounds taller = new Bounds();
            taller.SetMinMax(new Vector3(ColocarS2nPuertas.TallerX0, 0f, ColocarS2nPuertas.TallerZ0), new Vector3(ColocarS2nPuertas.TallerX1, ColocarS2nPuertas.TallerTecho, ColocarS2nPuertas.TallerZ1));
            Assert.IsTrue(taller.Contains(canepa.position + Vector3.up), "dentro del taller");
        }

        [Test]
        public void Taller_AEscalaReal_CabeUnCarroConEspacio()
        {
            // T-883: antes 9 x 8 m y 4.6 de alto, con el carro del elevador llenándolo.
            Assert.GreaterOrEqual(ColocarS2nPuertas.TallerX1 - ColocarS2nPuertas.TallerX0, 14f, "fondo");
            Assert.GreaterOrEqual(ColocarS2nPuertas.TallerZ1 - ColocarS2nPuertas.TallerZ0, 7.5f, "ancho");
            Assert.GreaterOrEqual(ColocarS2nPuertas.TallerTecho, 5f, "alto");
            Assert.AreEqual(1.52f, ColocarS2tTaller.EscalaModelo, 0.001f, "Canepa a 1.80 m animado (1.29 lo dejaba de 1.53)");
        }
    }
}
