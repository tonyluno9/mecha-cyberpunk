using System.Linq;
using MechaCyberpunk.Arte.Editor;
using MechaCyberpunk.Mundo.Ambiente;
using MechaCyberpunk.Mundo.Reloj;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>
    /// S2c (7-nov), paso 1 de docs/plan-look-ciudad.md: la calle de prueba de K01 en la manzana de la casa y el look de la ciudad (contornos,
    /// charcos que reflejan, niebla azul, posproceso).
    /// </summary>
    public class CalleDoradaS2cPruebas
    {
        private const float RadioJugador = 0.4f;
        private const float AlturaJugador = 1.8f;
        private Transform raiz;

        [OneTimeSetUp]
        public void Abrir()
        {
            EditorSceneManager.OpenScene(ColocarS2.RutaEscena, OpenSceneMode.Single);
            raiz = GameObject.Find(ColocarS2cCalle.Raiz)?.transform;
        }

        [Test]
        public void Calle_ExisteConCasasALosDosLados()
        {
            Assert.IsNotNull(raiz, "falta " + ColocarS2cCalle.Raiz);
            Transform casas = raiz.Find("Casas");
            int sur = casas.Cast<Transform>().Count(c => Mathf.Abs(c.position.z - ColocarS2cCalle.ZSur) < 0.1f);
            int norte = casas.Cast<Transform>().Count(c => Mathf.Abs(c.position.z - ColocarS2cCalle.ZNorte) < 0.1f);
            Assert.GreaterOrEqual(sur, 12, "casas del lado sur");
            Assert.GreaterOrEqual(norte, 12, "casas del lado norte");
        }

        [Test]
        public void Calle_AngostaComoK01()
        {
            Assert.AreEqual(11.5f, ColocarS2cCalle.ZNorte - ColocarS2cCalle.ZSur, 0.01f, "de fachada a fachada");
            Bounds arroyo = raiz.Find("Piso/Arroyo").GetComponent<Renderer>().bounds;
            Assert.AreEqual(7.5f, arroyo.size.z, 0.05f, "arroyo de 7.5 m (banquetas de 2 m)");
            Bounds banqueta = raiz.Find("Piso/BanquetaSur").GetComponent<Renderer>().bounds;
            Assert.AreEqual(ColocarS2cCalle.AltoBanqueta, banqueta.max.y - arroyo.max.y, 0.01f, "la banqueta sube su guarnición");
        }

        [Test]
        public void Casas_DeDosPisosConPiezasDelKit()
        {
            foreach (Transform casa in raiz.Find("Casas").Cast<Transform>().Where(c => c.name.StartsWith("Casa_")))
            {
                string[] piezas = casa.GetComponentsInChildren<Transform>().Select(t => PrefabUtility.GetCorrespondingObjectFromSource(t.gameObject)?.name).Where(n => n != null).ToArray();
                Assert.IsTrue(piezas.Any(n => n.StartsWith("C01") || n.StartsWith("C02")), casa.name + ": planta baja del kit");
                Assert.IsTrue(piezas.Any(n => n.StartsWith("C03") || n.StartsWith("C20")), casa.name + ": piso alto del kit");
                Bounds b = Limites(casa);
                Assert.That(b.max.y - b.min.y, Is.InRange(5.5f, 10.5f), casa.name + ": dos pisos (con tinaco)");
            }
        }

        [Test]
        public void Casas_NoInvadenElArroyo()
        {
            Bounds arroyo = raiz.Find("Piso/Arroyo").GetComponent<Renderer>().bounds;
            float zSur = arroyo.min.z, zNorte = arroyo.max.z;
            foreach (Collider c in raiz.Find("Casas").GetComponentsInChildren<Collider>().Where(c => c.enabled))
            {
                Bounds b = c.bounds;
                bool sobreArroyo = b.max.z > zSur + 0.05f && b.min.z < zNorte - 0.05f && b.min.x < ColocarS2cCalle.XOriente - 0.5f && b.max.x > ColocarS2cCalle.XPoniente + 0.5f;
                Assert.IsFalse(sobreArroyo, ColocarS2.Ruta(c.transform) + " se mete al arroyo");
            }
        }

        [Test]
        public void Calle_SeCruzaDeCalleACalleSinChocar()
        {
            Physics.SyncTransforms();
            float piso = raiz.Find("Piso/Arroyo").GetComponent<Renderer>().bounds.max.y;
            Vector3 abajo = new Vector3(ColocarS2cCalle.XBordeOriente + 1f, piso + RadioJugador + 0.25f, ColocarS2cCalle.ZCentro);
            Vector3 arriba = abajo + Vector3.up * (AlturaJugador - 2f * RadioJugador);
            float largo = ColocarS2cCalle.XBordeOriente - ColocarS2cCalle.XBordePoniente - 2f;
            RaycastHit[] golpes = Physics.CapsuleCastAll(abajo, arriba, RadioJugador, Vector3.left, largo, ~(1 << ReflejoCharcos.CapaCharcos), QueryTriggerInteraction.Ignore);
            Assert.IsEmpty(golpes.Select(g => ColocarS2.Ruta(g.collider.transform)).ToArray(), "la calle de la casa y la del poniente se conectan");
        }

        [Test]
        public void EdificiosDeEnMedio_Apagados()
        {
            foreach (string e in ColocarS2cCalle.Edificios)
            {
                Transform t = ColocarS2.Buscar(e);
                Assert.IsTrue(t == null || !t.gameObject.activeInHierarchy, e + " sigue prendido");
            }
        }

        [Test]
        public void NadaFlota_EnLaCaraNorteDelCallejon()
        {
            foreach (Renderer r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                         .Where(r => r.transform.root.name == "Barrio" && ColocarS2cCalle.CaraCallejon.Contains(new Vector3(r.bounds.center.x, 0f, r.bounds.center.z))))
            {
                Assert.LessOrEqual(r.bounds.max.y, ColocarS2cCalle.AzoteaCasas + 0.01f, ColocarS2.Ruta(r.transform) + " queda arriba de las azoteas");
            }
        }

        [Test]
        public void Postes_LuzDeSodioBajoLaLamparaConHaloYDeNoche()
        {
            Transform[] postes = raiz.Find("Postes").Cast<Transform>().ToArray();
            Assert.GreaterOrEqual(postes.Length, 4);
            foreach (Transform p in postes)
            {
                Light l = p.GetComponentsInChildren<Light>(true).Single(x => x.type == LightType.Spot);
                Assert.Greater(Vector3.Dot(l.transform.forward, Vector3.down), 0.99f, p.name + ": apunta al piso");
                Assert.Greater(l.transform.position.y, 5.5f, p.name + ": sale de la cabeza de la lámpara");
                uint capas = (uint)l.GetUniversalAdditionalLightData().renderingLayers;
                Assert.AreEqual(0u, capas & (1u << ColocarS2.CapaPostes), p.name + ": no ilumina su propio poste");
                Assert.IsTrue(p.GetComponentsInChildren<LuzNocturna>(true).Any(n => new SerializedObject(n).FindProperty("luz").objectReferenceValue == l), p.name + ": se prende de noche");
                Renderer halo = p.Find("Halo").GetComponent<Renderer>();
                Assert.AreEqual("MechaCyberpunk/Mundo/HaloLuz", halo.sharedMaterial.shader.name);
                Assert.AreEqual(ShadowCastingMode.Off, halo.shadowCastingMode);
            }
        }

        [Test]
        public void Charcos_ReflejanYVanEnLaCapaWater()
        {
            Renderer[] charcos = raiz.GetComponentsInChildren<Renderer>().Where(r => r.name == "Charco").ToArray();
            Assert.GreaterOrEqual(charcos.Length, 10);
            foreach (Renderer c in charcos)
            {
                Assert.AreEqual(ReflejoCharcos.CapaCharcos, c.gameObject.layer, "la cámara del reflejo no ve los charcos");
                Assert.AreEqual("MechaCyberpunk/Mundo/Charco", c.sharedMaterial.shader.name);
            }
            ReflejoCharcos reflejo = Object.FindFirstObjectByType<ReflejoCharcos>();
            Assert.IsNotNull(reflejo, "la escena tiene el reflejo de los charcos");
            Assert.IsNotNull(new SerializedObject(reflejo).FindProperty("config").objectReferenceValue, "con su ConfigCharcos");
        }

        [Test]
        public void Cables_ColganEntrePostesYCasas()
        {
            MeshFilter cables = raiz.Find("Cables").GetComponent<MeshFilter>();
            Assert.IsNotNull(cables.sharedMesh);
            Assert.IsTrue(AssetDatabase.Contains(cables.sharedMesh), "la malla de los cables es un asset");
            Assert.Greater(cables.sharedMesh.vertexCount, 1000, "decenas de cables");
        }

        [Test]
        public void Neones_YVentanasCalidas_SoloDeNoche()
        {
            Renderer[] caras = raiz.GetComponentsInChildren<Renderer>().Where(r => r.name == "Cara" || r.name == "VentanaCalida").ToArray();
            Assert.GreaterOrEqual(caras.Count(r => r.name == "Cara"), 8, "letreros de neón chicos");
            Assert.GreaterOrEqual(caras.Count(r => r.name == "VentanaCalida"), 15, "ventanas cálidas al azar");
            Assert.IsTrue(caras.All(r => r.sharedMaterial.shader.name == "MechaCyberpunk/Mundo/EmisionNoche"));
        }

        [Test]
        public void Contornos_EnElRendererAntesDelPosproceso()
        {
            UniversalRendererData datos = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(ColocarS2cLook.Renderers[0]);
            FullScreenPassRendererFeature f = datos.rendererFeatures.OfType<FullScreenPassRendererFeature>().SingleOrDefault(x => x.name == ColocarS2cLook.NombreContornos);
            Assert.IsNotNull(f, "falta el pase de contornos en " + ColocarS2cLook.Renderers[0]);
            Assert.IsTrue(f.isActive);
            Assert.AreEqual(FullScreenPassRendererFeature.InjectionPoint.BeforeRenderingPostProcessing, f.injectionPoint);
            Assert.IsTrue((f.requirements & ScriptableRenderPassInput.Normal) != 0 && (f.requirements & ScriptableRenderPassInput.Depth) != 0, "lee profundidad y normales");
            Assert.AreEqual("MechaCyberpunk/Mundo/Contornos", f.passMaterial.shader.name);
        }

        [Test]
        public void PerfilNoche_GradacionAzulEnSombrasYNaranjaEnLuces()
        {
            VolumeProfile noche = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ColocarS2cLook.RutaPerfilNoche);
            Assert.IsTrue(noche.TryGet(out SplitToning partido) && partido.active);
            Assert.Greater(partido.shadows.value.b, partido.shadows.value.r, "sombras azules");
            Assert.Greater(partido.highlights.value.r, partido.highlights.value.b, "luces naranjas");
            Assert.IsTrue(noche.TryGet(out Tonemapping tono) && tono.mode.value == TonemappingMode.ACES);
        }

        private static Bounds Limites(Transform t)
        {
            Renderer[] rs = t.GetComponentsInChildren<Renderer>();
            Bounds b = rs[0].bounds;
            foreach (Renderer r in rs)
            {
                b.Encapsulate(r.bounds);
            }
            return b;
        }
    }
}
