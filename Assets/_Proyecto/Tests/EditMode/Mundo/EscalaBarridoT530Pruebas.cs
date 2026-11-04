using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>
    /// T-530 (plan de la noche 3-nov, paso 1): la ciudad sigue más allá de lo jugable (sin retenes, sin "ALTO" ni muros en las salidas),
    /// el Tianguis sin poste en la calle, sin puestos encimados ni flotando, sin hologramas de relleno, puentes de Neón Alto que no
    /// atraviesan edificios, robots de Nova más altos que una persona y cámara más cerca.
    /// </summary>
    public class EscalaBarridoT530Pruebas
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private Scene escena;

        [OneTimeSetUp]
        public void Abrir()
        {
            escena = EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Additive);
        }

        [OneTimeTearDown]
        public void Cerrar()
        {
            EditorSceneManager.CloseScene(escena, true);
        }

        private Transform Raiz(string nombre)
        {
            return escena.GetRootGameObjects().First(g => g.name == nombre).transform;
        }

        private static Bounds Caja(Transform t)
        {
            Renderer[] rs = t.GetComponentsInChildren<Renderer>().Where(r => r.enabled && !(r is ParticleSystemRenderer)).ToArray();
            Bounds b = rs[0].bounds;
            foreach (Renderer r in rs)
            {
                b.Encapsulate(r.bounds);
            }
            return b;
        }

        [Test]
        public void Afueras_SeisSalidasConCalleEdificiosYLimiteInvisible_SinRetenesNiAlto()
        {
            Transform afueras = Raiz("T530_Afueras");
            for (int i = 0; i < 6; i++)
            {
                Transform s = afueras.Find("Salida_" + i);
                Assert.IsNotNull(s, "Salida " + i);
                Assert.IsNotNull(s.Find("Calle"));
                Assert.GreaterOrEqual(s.Cast<Transform>().Count(t => t.name == "Edificio"), 8, "Cuadras de relleno en la salida " + i);
                Assert.IsTrue(s.Cast<Transform>().Any(t => t.name == "LimiteInvisible" && t.GetComponent<BoxCollider>() != null && t.GetComponent<Renderer>() == null));
            }
            Transform finales = Raiz("Barrio").Find("Finales");
            foreach (string n in new[] { "Final_Reten", "Final_Obra", "Final_Reten_Norte_Oeste", "Final_Reten_Norte_Este", "Final_Obra_Sur" })
            {
                Assert.IsFalse(finales.Find(n).gameObject.activeSelf, n);
            }
            Assert.IsFalse(escena.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<UnityEngine.UI.Text>())
                .Any(t => t.text.Contains("CORPORATIVO")), "Sin letrero ALTO · CONTROL CORPORATIVO");
        }

        [Test]
        public void Afueras_LasSalidasNoTienenMuro()
        {
            Physics.SyncTransforms();
            // Del borde de la ciudad hacia afuera, a 1 m del piso, por el centro de cada salida: nada sólido en 90 m.
            foreach ((Vector3 p, Vector3 d) in new[]
            {
                (new Vector3(120f, 1f, 0f), Vector3.right), (new Vector3(-120f, 1f, 0f), Vector3.left),
                (new Vector3(-47f, 1f, 75f), Vector3.forward), (new Vector3(47f, 1f, 75f), Vector3.forward),
                (new Vector3(-47f, 1f, -75f), Vector3.back), (new Vector3(47f, 1f, -75f), Vector3.back),
            })
            {
                bool choca = Physics.Raycast(p, d, out RaycastHit h, 90f, ~0, QueryTriggerInteraction.Ignore);
                Assert.IsFalse(choca && h.collider.gameObject.scene == escena && h.collider.GetComponentInParent<Rigidbody>() == null,
                    "Muro en la salida " + p + ": " + (choca ? h.collider.name : ""));
            }
        }

        [Test]
        public void Tianguis_ArcoFueraDelCruce_SinHologramasNiProyectores()
        {
            foreach (Transform pilar in Raiz("GTA_T407").Find("Tianguis/ArcoTianguis").Cast<Transform>().Where(t => t.name == "Pilar"))
            {
                Assert.IsFalse(pilar.position.x > -54f && pilar.position.x < -40f, "Pilar en medio de la calle que cruza");
            }
            Assert.IsFalse(Raiz("T503_Tianguis").Find("Hologramas").gameObject.activeSelf);
            Assert.IsFalse(Raiz("T503_Tianguis").Find("HologramasPuestos").gameObject.activeSelf);
            Assert.IsFalse(escena.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>()).Any(t => t.name.StartsWith("CY01_ProyectorHolo")));
        }

        [Test]
        public void Tianguis_PuestosEnElPisoYSinTraslaparse()
        {
            Transform[] puestos = Raiz("T512_LuzZonas").Find("Tianguis").Cast<Transform>()
                .Concat(Raiz("T503_Tianguis").Find("Puestos").Cast<Transform>())
                .Concat(Raiz("GTA_T407").Find("Tianguis/Puestos").Cast<Transform>())
                .Where(t => t.gameObject.activeInHierarchy).ToArray();
            Assert.GreaterOrEqual(puestos.Length, 18, "Siguen habiendo muchos puestos");
            Bounds[] cajas = puestos.Select(Caja).ToArray();
            for (int i = 0; i < puestos.Length; i++)
            {
                Assert.Less(cajas[i].min.y, 0.35f, puestos[i].name + " flota");
                for (int j = i + 1; j < puestos.Length; j++)
                {
                    Bounds a = cajas[i];
                    Bounds b = cajas[j];
                    bool traslape = a.min.x + 0.15f < b.max.x && b.min.x + 0.15f < a.max.x && a.min.z + 0.15f < b.max.z && b.min.z + 0.15f < a.max.z;
                    Assert.IsFalse(traslape, puestos[i].name + " " + puestos[i].position + " encima de " + puestos[j].name + " " + puestos[j].position);
                }
            }
        }

        [Test]
        public void Letreros_PlanosDeLugaresApagados()
        {
            foreach (Transform t in escena.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).Where(t => t.GetComponent("LetreroLugar") != null))
            {
                Assert.IsFalse(t.gameObject.activeSelf, t.name + " de " + t.parent.name);
            }
        }

        [Test]
        public void NeonAlto_PuentesDelAnchoDeLaCalle()
        {
            foreach (Transform p in Raiz("T505_NeonAlto").Find("Piezas").Cast<Transform>().Where(t => t.name.StartsWith("N02")))
            {
                Bounds b = Caja(p);
                Assert.Greater(b.min.x, 33f, "No entra a los edificios del oeste");
                Assert.Less(b.max.x, 61f, "No entra a los edificios del este");
            }
        }

        [Test]
        public void RobotsDeNova_MasAltosQueUnaPersona()
        {
            foreach (Transform g in Raiz("T510_Guardias").Cast<Transform>())
            {
                Assert.That(Caja(g).size.y, Is.InRange(2.1f, 2.3f), g.name);
            }
        }

        [Test]
        public void CamaraHombro_MasCerca()
        {
            SerializedObject so = new SerializedObject(AssetDatabase.LoadMainAssetAtPath("Assets/_Proyecto/Personaje/Datos/ConfigCamaraHombro_Base.asset"));
            Assert.That(so.FindProperty("distanciaNormal").floatValue, Is.InRange(1.6f, 2.8f));
        }
    }
}
