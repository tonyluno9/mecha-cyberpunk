using System.Linq;
using MechaCyberpunk.Arte.Editor;
using MechaCyberpunk.Core;
using MechaCyberpunk.Mundo.Lugares;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>T-884 (playtest 7-nov): casino con puerta automática de cristal y los modelos de Tony I11-I16 adentro.</summary>
    public class CasinoS2oPruebas
    {
        [OneTimeSetUp]
        public void Abrir() => EditorSceneManager.OpenScene(ColocarS2.RutaEscena, OpenSceneMode.Single);

        [Test]
        public void Puerta_CristalCorredizaYAutomatica()
        {
            HojaPuerta hoja = ColocarS2.Buscar(ColocarS2oCasino.RutaHoja).GetComponent<HojaPuerta>();
            Assert.IsTrue(hoja.Corrediza);
            Assert.Greater(hoja.DesplazamientoCorredizo.magnitude, 2f, "se hace a un lado todo el vano");
            Assert.AreEqual(ColocarS2oCasino.RutaMaterialCristal, AssetDatabase.GetAssetPath(hoja.GetComponent<Renderer>().sharedMaterial));
            // PuertaAutomatica es de Progresion (no la referencia este ensamblado): se busca por nombre.
            Component auto = ColocarS2.Buscar(ColocarS2oCasino.RutaLugar).GetComponents<Component>().FirstOrDefault(c => c.GetType().Name == "PuertaAutomatica");
            Assert.IsNotNull(auto, "se abre sola al acercarte");
            Assert.AreEqual((int)IdLugar.Casino, new SerializedObject(auto).FindProperty("lugar").intValue);
        }

        [Test]
        public void Salon_ConLosModelosDeTony()
        {
            Transform raiz = ColocarS2.Buscar(ColocarS2oCasino.Raiz);
            int Cuantos(string codigo) => raiz.Cast<Transform>().Count(t => t.name == codigo);
            Assert.AreEqual(1, Cuantos("I16"), "arco dorado");
            Assert.AreEqual(ColocarS2oCasino.Mesas.Length, Cuantos("I11"), "mesas de blackjack");
            Assert.AreEqual(ColocarS2oCasino.Mesas.Length * 3, Cuantos("I12"), "sillas");
            Assert.AreEqual(ColocarS2oCasino.Mesas.Length, Cuantos("I13"), "candiles");
            Assert.AreEqual(1, Cuantos("I14"), "barra");
            Assert.AreEqual(3, Cuantos("I15"), "bancos");
            Transform mob = ColocarS2.Buscar(ColocarS2oCasino.RutaMobiliario);
            Assert.IsFalse(mob.Cast<Transform>().Any(t => t.name == "MesaCartas" && t.gameObject.activeSelf), "sin las mesas de cubos");
        }
    }
}
