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
            string[] textos = ColocarS2.Buscar(ColocarS2tTaller.NombreRaiz).GetComponentsInChildren<UnityEngine.UI.Text>().Select(t => t.text).ToArray();
            CollectionAssert.AreEquivalent(new[] { "EL CANEPA", "TALLER MECÁNICO" }, textos);
            Transform canepa = ColocarS2.Buscar(ColocarS2tTaller.RutaCanepa);
            Animator animador = canepa.GetComponentsInChildren<Animator>().Single();
            StringAssert.Contains("Mecanico", animador.transform.root == canepa.root ? animador.GetComponentInParent<Transform>().parent.parent.name : "", "el modelo es el del Mecánico");
            Assert.IsNotNull(animador.runtimeAnimatorController, "con su controller (Idle)");
            Bounds taller = new Bounds(new Vector3((-37f + -28f) * 0.5f, 2f, (-75.67f + -67.67f) * 0.5f), new Vector3(9f, 4f, 8f));
            Assert.IsTrue(taller.Contains(canepa.position + Vector3.up), "dentro del taller");
        }
    }
}
