using System.Collections.Generic;
using System.Linq;
using MechaCyberpunk.Mundo.Ambiente;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>Sesión 2 del playtest del 5-nov (docs/playtest-05nov.md): lo que ColocarS2 dejó en Ciudad_Prologo.</summary>
    public class MundoS2Pruebas
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const int CapaPostes = 5;
        private const float BrazoMinimo = 0.6f;   // m: la luz queda en la punta del brazo, no sobre el tubo

        private Scene escena;
        private List<Transform> todos;

        [OneTimeSetUp]
        public void Abrir()
        {
            escena = EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Additive);
            todos = escena.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).ToList();
        }

        [OneTimeTearDown]
        public void Cerrar()
        {
            EditorSceneManager.CloseScene(escena, true);
        }

        private IEnumerable<T> Activos<T>() where T : Component
        {
            return todos.Where(t => t.gameObject.activeInHierarchy).SelectMany(t => t.GetComponents<T>());
        }

        [Test]
        public void Lluvia_NoHayNiParticulasNiSalpicaduras()
        {
            Assert.IsEmpty(Activos<LluviaSigueJugador>(), "lluvia que sigue al jugador");
            Assert.IsEmpty(Activos<SalpicadurasLluvia>(), "salpicaduras");
            Assert.IsFalse(LluviaSigueJugador.Activa, "el interruptor de la lluvia empieza apagado");
        }

        [Test]
        public void Letreros_SoloQuedanLosModelos()
        {
            string[] textos = { "CAFE", "INTERNET", "CANTINA", "ABARROTES", "PULQUERIA", "TACOS", "ELOTES", "FARMACIA" };
            Transform[] planos = todos.Where(t => t.gameObject.activeInHierarchy && t.name.StartsWith("Letrero_") && textos.Any(x => t.name.Contains(x))).ToArray();
            Assert.IsEmpty(planos, "letreros de primitivos: " + string.Join(", ", planos.Take(5).Select(t => t.name)));
            Assert.IsEmpty(Activos<TextMesh>().Where(t => t.transform.root.name != "MechaEstacionado"), "textos 3D sueltos");
        }

        [Test]
        public void Tianguis_SinArcoNiSeriesDeFocos()
        {
            Assert.IsFalse(todos.Any(t => t.gameObject.activeInHierarchy && t.name == "ArcoTianguis"), "arco EL TIANGUIS");
            Assert.IsFalse(todos.Any(t => t.gameObject.activeInHierarchy && t.name.StartsWith("Serie_") && t.parent != null && t.parent.name == "Focos"
                && (t.root.name == "AMB_Noche" || t.root.name == "T503_Tianguis")), "series de focos del tianguis");
        }

        [Test]
        public void TextosFlotantes_NoHayCanvasDeMundoConLetreros()
        {
            string[] quitados = { "LetreroElFox", "LetreroArmas", "Texto letrero" };
            Assert.IsFalse(todos.Any(t => t.gameObject.activeInHierarchy && quitados.Contains(t.name)));
            Transform vendedor = todos.FirstOrDefault(t => t.name == "Nombre" && t.parent != null && t.parent.name == "Vendedor");
            Assert.IsTrue(vendedor == null || !vendedor.gameObject.activeInHierarchy, "VENDEDOR flotando");
        }

        [Test]
        public void Postes_LaLuzSaleDeLaLamparaYNoIluminaElTubo()
        {
            Transform[] farolas = todos.Where(t => t.name.StartsWith("Farola_") && t.parent != null && t.parent.name == "Farolas").ToArray();
            Assert.Greater(farolas.Length, 40);
            int revisadas = 0;
            foreach (Transform f in farolas)
            {
                Transform luz = f.Find("Luz");
                MeshFilter mf = f.GetComponentsInChildren<MeshFilter>(true).FirstOrDefault(m => m.sharedMesh != null && m.GetComponent<Renderer>().enabled && m.gameObject.activeInHierarchy);
                if (luz == null || mf == null)
                {
                    continue;
                }
                Bounds b = mf.GetComponent<Renderer>().bounds;
                Vector3 baseTubo = mf.sharedMesh.vertices.Select(v => mf.transform.TransformPoint(v)).Where(v => v.y < b.min.y + 0.5f).Aggregate(Vector3.zero, (a, v) => a + v);
                int n = mf.sharedMesh.vertices.Count(v => mf.transform.TransformPoint(v).y < b.min.y + 0.5f);
                baseTubo /= n;
                float brazo = new Vector2(luz.position.x - baseTubo.x, luz.position.z - baseTubo.z).magnitude;
                Assert.Greater(brazo, BrazoMinimo, f.name + ": la luz está sobre el tubo");
                Assert.Less(luz.position.y, b.max.y, f.name + ": la luz queda debajo de la cabeza");
                Assert.Greater(Vector3.Dot(luz.forward, Vector3.down), 0.95f, f.name + " apunta hacia abajo");
                uint capas = luz.GetComponent<Light>().GetUniversalAdditionalLightData().renderingLayers;
                Assert.AreEqual(0u, capas & (1u << CapaPostes), f.name + ": su luz no debe iluminar su poste");
                Assert.AreEqual(1u << CapaPostes, (uint)mf.GetComponent<Renderer>().renderingLayerMask, f.name + ": el poste va en su capa");
                revisadas++;
            }
            Assert.Greater(revisadas, 40);
        }

        [Test]
        public void Personajes_AnimadosQuedanALaAlturaDelJugador()
        {
            // Alturas de pie medidas en Play con la malla animada (ColocarS2.Personajes): la escala del Modelo las lleva a la del jugador.
            foreach ((string prefab, float medida, float relativa) in MechaCyberpunk.Arte.Editor.ColocarS2.Personajes)
            {
                GameObject go = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Proyecto/Personaje/Visual/Prefabs/" + prefab + ".prefab");
                Assert.IsNotNull(go, prefab);
                float altura = medida * go.transform.Find("Giro/Modelo").localScale.y;
                Assert.AreEqual(MechaCyberpunk.Arte.Editor.ColocarS2.AlturaJugador * relativa, altura, 0.03f, prefab + " fuera de escala contra el jugador");
                Assert.That(relativa, Is.InRange(0.95f, 1.3f), prefab + ": ningún adulto enano ni gigante");
            }
            Transform guardias = escena.GetRootGameObjects().First(g => g.name == "T510_Guardias").transform;
            foreach (Animator a in guardias.GetComponentsInChildren<Animator>(true))
            {
                Assert.AreEqual(1.4f * 1.25f / 1.49f, a.transform.lossyScale.y, 0.02f, "los guardias ya no se agrandan en la escena");
            }
        }
    }
}
