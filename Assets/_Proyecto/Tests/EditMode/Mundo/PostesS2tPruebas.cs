using System.Linq;
using MechaCyberpunk.Arte.Editor;
using MechaCyberpunk.Mundo.Reloj;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>T-855 (playtest 6-nov tarde): la luz de cada poste (C07 y N09) sale de su lámpara, apunta al piso y se prende de noche.</summary>
    public class PostesS2tPruebas
    {
        [OneTimeSetUp]
        public void Abrir() => EditorSceneManager.OpenScene(ColocarS2.RutaEscena, OpenSceneMode.Single);

        [Test]
        public void CadaPoste_TieneSuLuzBajoLaLamparaApuntandoAlPiso()
        {
            Transform[] postes = PostesS2t.Postes();
            Assert.Greater(postes.Length, 50, "farolas C07 y postes N09 de la ciudad");
            Assert.IsTrue(postes.Any(p => p.name.StartsWith("N09")), "también los N09");
            foreach (Transform poste in postes)
            {
                MeshFilter mf = PostesS2t.ModeloVisible(poste);
                var cabezas = PostesS2t.Cabezas(mf.sharedMesh, mf.transform.localToWorldMatrix);
                Light[] luces = poste.GetComponentsInChildren<Light>(true);
                Assert.GreaterOrEqual(luces.Length, cabezas.Count, poste.name + ": una luz por lámpara");
                foreach (var c in cabezas)
                {
                    Light l = luces.OrderBy(x => Vector3.Distance(x.transform.position, c)).First();
                    Assert.Less(Vector3.Distance(l.transform.position, c), 0.05f, poste.name + ": la luz está bajo la lámpara");
                    Assert.Greater(Vector3.Dot(l.transform.forward, Vector3.down), 0.99f, poste.name + ": apunta al piso");
                    Assert.LessOrEqual(l.spotAngle, PostesS2t.AnguloCono + 0.01f, poste.name + ": cono cerrado");
                    uint capas = (uint)l.GetUniversalAdditionalLightData().renderingLayers;
                    Assert.AreEqual(0u, capas & (1u << ColocarS2.CapaPostes), poste.name + ": no ilumina su propio poste");
                    bool laPrenden = poste.GetComponentsInChildren<LuzNocturna>(true).Any(n => new SerializedObject(n).FindProperty("luz").objectReferenceValue == l);
                    Assert.IsTrue(laPrenden, poste.name + ": se prende de noche");
                }
            }
        }

        [Test]
        public void Cabezas_DelC07_EstaEnElBrazoNoEnElTubo()
        {
            Transform poste = PostesS2t.Postes().First(p => p.name.StartsWith("Farola_"));
            MeshFilter mf = PostesS2t.ModeloVisible(poste);
            Vector3 c = PostesS2t.Cabezas(mf.sharedMesh, mf.transform.localToWorldMatrix)[0];
            Bounds b = mf.GetComponent<Renderer>().bounds;
            Assert.Greater(new Vector2(c.x - poste.position.x, c.z - poste.position.z).magnitude, 0.8f, "lejos del tubo");
            Assert.Greater(c.y, b.min.y + b.size.y * 0.7f, "arriba, en la cabeza");
        }
    }
}
