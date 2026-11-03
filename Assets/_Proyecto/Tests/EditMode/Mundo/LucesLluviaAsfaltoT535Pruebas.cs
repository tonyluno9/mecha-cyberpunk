using System.Linq;
using MechaCyberpunk.Mundo.Reloj;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>
    /// T-535 (plan de la noche 3-nov, paso 1): las luces de noche se apagan poco a poco al amanecer y prenden un poco antes de anochecer;
    /// el asfalto tiene textura con detalle y normal map; la luz de cada farola sale del foco; cada puesto del Tianguis tiene su luz.
    /// </summary>
    public class LucesLluviaAsfaltoT535Pruebas
    {
        [Test]
        public void LuzNocturna_AlAmanecer_SeApagaPocoAPoco()
        {
            Assert.AreEqual(1f, CalculoLuzNocturna.Factor(23f, 6f, 20f), 0.001f, "De noche, prendida");
            Assert.AreEqual(1f, CalculoLuzNocturna.Factor(6f, 6f, 20f), 0.001f, "Recién amaneció: sigue prendida");
            Assert.That(CalculoLuzNocturna.Factor(6.5f, 6f, 20f), Is.InRange(0.3f, 0.7f), "Media hora después: a medias");
            Assert.AreEqual(0f, CalculoLuzNocturna.Factor(12f, 6f, 20f), 0.001f, "Mediodía: apagada");
            Assert.That(CalculoLuzNocturna.Factor(19.75f, 6f, 20f), Is.InRange(0.3f, 0.7f), "Prende un poco antes de anochecer");
        }

        [Test]
        public void Asfalto_ConTexturaNormalYLoMojadoQueBrilla()
        {
            Material m = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Proyecto/Mundo/Materiales/Barrio_Asfalto.mat");
            Assert.IsNotNull(m.GetTexture("_BaseMap"), "Con textura (antes el color liso)");
            Assert.IsNotNull(m.GetTexture("_BumpMap"), "Con normal map (grietas y parches)");
            Assert.IsTrue(m.IsKeywordEnabled("_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A"), "Lo mojado brilla");
            Assert.That(m.GetTextureScale("_BaseMap").x, Is.InRange(20f, 50f), "Mosaico de unos 8 m");
        }

        [Test]
        public void Escena_FarolasConLuzEnElFoco_YPuestosConLuz()
        {
            Scene escena = EditorSceneManager.OpenScene("Assets/_Proyecto/Escenas/Ciudad_Prologo.unity", OpenSceneMode.Additive);
            try
            {
                Transform farolas = escena.GetRootGameObjects().First(g => g.name == "Barrio").transform.Find("Farolas");
                int revisadas = 0;
                foreach (Transform f in farolas)
                {
                    Light l = f.GetComponentInChildren<Light>(true);
                    Renderer poste = f.GetComponentsInChildren<MeshRenderer>().Where(r => r.enabled).OrderByDescending(r => r.bounds.size.y).FirstOrDefault();
                    if (l == null || poste == null)
                    {
                        continue;
                    }
                    Assert.Greater(l.transform.position.y, poste.bounds.max.y - 1.6f, f.name + ": la luz va arriba, en el foco");
                    revisadas++;
                }
                Assert.GreaterOrEqual(revisadas, 40, "Farolas con luz");
                Light[] puestos = escena.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Light>(true)).Where(l => l.name == "Luz_Puesto_T535").ToArray();
                Assert.GreaterOrEqual(puestos.Length, 15, "Cada puesto del Tianguis con su luz");
                Assert.IsTrue(puestos.All(l => l.shadows == LightShadows.None && l.GetComponent<LuzNocturna>() != null), "Sin sombras y solo de noche");
                Assert.Greater(puestos.Select(l => l.color).Distinct().Count(), 2, "Del color de lo que vende cada puesto");
            }
            finally
            {
                EditorSceneManager.CloseScene(escena, true);
            }
        }
    }
}
