using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>T-508: las calles de la demo traen toldos, letreros con texto, balcones, macetas y gente en banquetas, todo sin colliders, y los charcos son reflejos sutiles.</summary>
    public class CallesLlenasT508Pruebas
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const string NombreRaiz = "T508_CallesLlenas";
        private const int ToldosMinimos = 30;
        private const int BalconesMinimos = 20;
        private const int MacetasMinimas = 10;
        private const int GenteMinima = 25;
        private const int GenteMaxima = 48;
        private const float CharcoMaximo = 3.3f;
        private const float AlfaMaximoCharco = 0.5f;
        private const float DistanciaMinimaPuerta = 1.5f;

        private Scene escena;
        private Transform raiz;

        [OneTimeSetUp]
        public void Abrir()
        {
            escena = EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Additive);
            raiz = escena.GetRootGameObjects().First(g => g.name == NombreRaiz).transform;
        }

        [OneTimeTearDown]
        public void Cerrar()
        {
            EditorSceneManager.CloseScene(escena, true);
        }

        private int Contar(string prefijo)
        {
            return raiz.GetComponentsInChildren<Transform>(true).Count(t => t.name.StartsWith(prefijo));
        }

        [Test]
        public void Raiz_TraeToldosLetrerosBalconesMacetasYGente()
        {
            Assert.GreaterOrEqual(Contar("Toldo_"), ToldosMinimos, "toldos");
            Assert.GreaterOrEqual(Contar("Balcon"), BalconesMinimos, "balcones");
            Assert.GreaterOrEqual(Contar("Maceta"), MacetasMinimas, "macetas");
            Assert.That(Contar("Gente_"), Is.InRange(GenteMinima, GenteMaxima), "gente en banquetas");
        }

        [Test]
        public void Letreros_DePrimitivos_YaNoEstan()
        {
            // S2 (playtest 5-nov): los letreros de cubos y planos con texto (CAFÉ, CANTINA, ABARROTES...) se quitaron; solo quedan los modelos L10-L14.
            Assert.AreEqual(0, Contar("Letrero_"), "letreros hechos con primitivos");
        }

        [Test]
        public void Adornos_NoTienenCollidersNiTapanPuertas()
        {
            Assert.IsEmpty(raiz.GetComponentsInChildren<Collider>(true), "los adornos no llevan collider: no tapan caminos ni coberturas");
            Vector3[] puertas = escena.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<MonoBehaviour>(true))
                .Where(m => m != null && m.GetType().Name == "PuertaLocal").Select(m => m.transform.position).ToArray();
            Assert.IsNotEmpty(puertas);
            foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true).Where(x => x.name.StartsWith("Gente_")))
            {
                foreach (Vector3 p in puertas)
                {
                    float d = Vector2.Distance(new Vector2(t.position.x, t.position.z), new Vector2(p.x, p.z));
                    Assert.Greater(d, DistanciaMinimaPuerta, t.name + " muy cerca de una puerta");
                }
            }
        }

        [Test]
        public void Gente_SinSombraConCorteDeDistanciaYAnimacionDeReposo()
        {
            Transform[] gente = raiz.GetComponentsInChildren<Transform>(true).Where(t => t.name.StartsWith("Gente_") && t.GetComponent<LODGroup>() != null).ToArray();
            Assert.GreaterOrEqual(gente.Length, GenteMinima);
            foreach (Transform t in gente)
            {
                SkinnedMeshRenderer cuerpo = t.GetComponentInChildren<SkinnedMeshRenderer>();
                Assert.AreEqual(UnityEngine.Rendering.ShadowCastingMode.Off, cuerpo.shadowCastingMode, t.name);
                Animator anim = t.GetComponentInChildren<Animator>();
                Assert.AreEqual(AnimatorCullingMode.CullCompletely, anim.cullingMode, t.name);
                Assert.IsNotNull(anim.runtimeAnimatorController, t.name + " sin animacion de reposo");
            }
        }

        [Test]
        public void Charcos_SonReflejosSutilesYChicos()
        {
            MeshRenderer[] charcos = escena.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<MeshRenderer>(true))
                .Where(r => r.name.StartsWith("Charco_")).ToArray();
            Assert.GreaterOrEqual(charcos.Length, 50);
            foreach (MeshRenderer r in charcos)
            {
                StringAssert.StartsWith("T508_Charco", r.sharedMaterial.name, r.transform.parent.name + "/" + r.name);
                Assert.LessOrEqual(r.sharedMaterial.GetColor("_BaseColor").a, AlfaMaximoCharco, r.name + " muy opaco");
                Assert.LessOrEqual(Mathf.Max(r.transform.lossyScale.x, r.transform.lossyScale.y), CharcoMaximo, r.name + " muy grande");
                Assert.IsNotNull(r.sharedMaterial.GetTexture("_BaseMap"), r.name + " sin forma irregular");
            }
        }
    }
}
