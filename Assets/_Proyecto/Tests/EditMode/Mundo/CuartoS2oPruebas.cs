using System.Linq;
using MechaCyberpunk.Arte.Editor;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>T-886 (playtest 7-nov): el cuarto del jugador más grande, como R10 (5 x 4 m), con el espejo donde estaba.</summary>
    public class CuartoS2oPruebas
    {
        private Transform cuarto;

        [OneTimeSetUp]
        public void Abrir()
        {
            EditorSceneManager.OpenScene(ColocarS2.RutaEscena, OpenSceneMode.Single);
            cuarto = ColocarS2.Buscar(ColocarS2oCuarto.RutaCuarto);
        }

        [Test]
        public void Cuarto_DeCincoPorCuatro()
        {
            Bounds piso = cuarto.Find("Piso").GetComponent<Renderer>().bounds;
            Assert.AreEqual(5f, piso.size.x, 0.05f, "ancho (antes 3)");
            Assert.AreEqual(4f, piso.size.z, 0.05f, "fondo");
            Assert.AreEqual(ColocarS2oCuarto.X0, cuarto.Find("Pared_Izquierda").GetComponent<Renderer>().bounds.center.x, 0.05f);
            uint capa = cuarto.Find("Pared_Derecha").GetComponent<Renderer>().renderingLayerMask;
            foreach (string n in new[] { "Piso", "Techo", "Pared_Fondo", "Pared_Izquierda", "Pared_Puerta_Izq" })
            {
                Assert.AreEqual(capa, cuarto.Find(n).GetComponent<Renderer>().renderingLayerMask, n + ": la alumbran las luces del cuarto");
            }
            BoxCollider zona = cuarto.Find("ZonaInterior").GetComponent<BoxCollider>();
            Assert.IsTrue(zona.bounds.Contains(new Vector3(ColocarS2oCuarto.X0 + 0.5f, cuarto.position.y + 1f, -73.5f)), "la zona cubre la parte nueva");
        }

        [Test]
        public void Muebles_DentroYComoR10_ElEspejoNoSeMovio()
        {
            Bounds dentro = new Bounds();
            dentro.SetMinMax(new Vector3(ColocarS2oCuarto.X0, 0f, ColocarS2oCuarto.Z0), new Vector3(ColocarS2oCuarto.X1, 3f, ColocarS2oCuarto.Z1));
            foreach (var m in ColocarS2oCuarto.Muebles)
            {
                Transform t = cuarto.Find(m.nombre);
                Assert.IsNotNull(t, m.nombre);
                Bounds b = t.GetComponentsInChildren<Renderer>().Select(r => r.bounds).Aggregate((a, c) => { a.Encapsulate(c); return a; });
                Assert.IsTrue(dentro.Contains(b.center), m.nombre + " dentro del cuarto");
            }
            Assert.Less(cuarto.Find("H02_Cama").position.x, cuarto.Find("I02").position.x, "la cama a la izquierda, el escritorio a la derecha");
            Transform espejo = ColocarS2.Buscar("S2t_Espejo/Vidrio_Reflejo");
            Assert.AreEqual(-63.2f, espejo.position.x, 0.02f, "el espejo se queda igual");
            Assert.AreEqual(-75.06f, espejo.position.z, 0.02f);
            foreach (LineRenderer c in cuarto.GetComponentsInChildren<LineRenderer>(true).Where(c => c.name == "Cable"))
            {
                Assert.IsTrue(c.useWorldSpace && dentro.Contains(c.GetPosition(0)), "los cables cuelgan dentro del cuarto (antes estaban en x = 1000)");
            }
        }
    }
}
