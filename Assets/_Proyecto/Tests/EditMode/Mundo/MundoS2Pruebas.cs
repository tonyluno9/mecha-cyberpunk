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
                Assert.AreEqual(MechaCyberpunk.Arte.Editor.ColocarS2.AlturaJugador * 1.25f / 1.36f, a.transform.lossyScale.y, 0.02f, "los guardias ya no se agrandan en la escena");
            }
        }

        [Test]
        public void Interiores_ConsultorioYTallerDeTino_SegunSuReferencia()
        {
            Transform raiz = escena.GetRootGameObjects().First(g => g.name == "S2_Interiores").transform;
            Transform doc = raiz.Find("ConsultorioDoc");
            foreach (string pieza in new[] { "I06", "I07", "I08", "I09", "I10", "T08_RepisaTaller", "CY05_ServidorCallejero" })
            {
                Assert.IsNotNull(doc.Find(pieza), "Consultorio: " + pieza);
            }
            Transform tino = raiz.Find("TallerTino");
            foreach (string pieza in new[] { "T03_BancoTrabajo", "I21", "I20", "I02", "CY07b_Dron", "SerieFocos", "Pizarron" })
            {
                Assert.IsNotNull(tino.Find(pieza), "Taller de Tino: " + pieza);
            }
            // El Doc y Tino no se movieron (ruta de la demo) y quedan dentro de su local, con el vano de entrada libre.
            Transform joel = escena.GetRootGameObjects().First(g => g.name == "GTA_T417").transform.Find("Apoyo_JOEL");
            Assert.That(joel.position.x, Is.InRange(-104f, -96f));
            Assert.That(joel.position.z, Is.InRange(-16f, -10.2f));
            foreach (Transform local in new[] { doc, tino })
            {
                Assert.IsNotNull(local.Find("Forro/Frente_Izq_Dentro"), local.name + " con frente cerrado");
                Assert.IsNotNull(local.Find("Forro/Pared_Fondo"), local.name + " con su pared del kit X");
            }
            Transform tablero = todos.FirstOrDefault(t => t.name == "Letrero" && t.parent != null && t.parent.name == "MostradorClinica");
            Assert.IsTrue(tablero == null || !tablero.GetComponent<Renderer>().enabled, "sin el letrero de cubos de la clínica");
        }

        [Test]
        public void Casa_FachadaDelKit_TableroSinCuboBlanco_Y_CuartoSegunLaReferencia()
        {
            Transform casa = escena.GetRootGameObjects().First(g => g.name == "S2_Casa").transform;
            Assert.GreaterOrEqual(casa.Find("Fachada").childCount, 12, "fachada armada con el kit");
            Transform lugar = escena.GetRootGameObjects().First(g => g.name == "Barrio").transform.Find("Manzana_4/Lugar_Casa");
            Assert.IsFalse(lugar.Find("FachadaIzquierda").GetComponent<Renderer>().enabled, "el cubo de la fachada ya no se ve");
            Assert.IsTrue(lugar.Find("FachadaIzquierda").GetComponent<Collider>().enabled, "pero sigue chocando");
            Transform tablero = escena.GetRootGameObjects().First(g => g.name == "GTA_T426").transform.Find("TableroEncargos");
            Assert.IsFalse(tablero.GetComponent<Renderer>().enabled, "sin el cubo blanco del tablero");
            Assert.IsNotNull(casa.Find("Tablero/Tablero_CY10"));

            Transform cuarto = escena.GetRootGameObjects().First(g => g.name == "T550_Cuartos").transform.Find("Cuarto_Habitacion");
            Bounds Limites(Transform t) => t.GetComponentsInChildren<Renderer>().Select(r => r.bounds).Aggregate((a, b) => { a.Encapsulate(b); return a; });
            Bounds mochila = Limites(cuarto.Find("I05"));
            Assert.That(mochila.size.y, Is.InRange(0.4f, 0.65f), "mochila de tamaño real");
            Assert.Greater(cuarto.InverseTransformPoint(Limites(cuarto.Find("I01")).center).x, 0.5f, "ropero a la derecha, como en la referencia");
            Assert.IsFalse(Limites(cuarto.Find("I01")).Contains(cuarto.Find("Aparicion").position + Vector3.up), "el jugador no aparece dentro del ropero");
            Bounds puerta = Limites(cuarto.Find("Puerta_Cuarto/Hoja/Puerta_L02")); // T-804: la hoja L02 gira en su bisagra
            Assert.That(puerta.size.y, Is.InRange(2f, 2.3f), "puerta de verdad de unos 2.1 m");
            Assert.LessOrEqual(puerta.size.x, 1.2f);
            Assert.IsNotNull(cuarto.Find("S2_Cuarto/Foco_Pelon"));
            foreach (MonoBehaviour p in todos.SelectMany(x => x.GetComponents<MonoBehaviour>()).Where(m => m != null && m.GetType().Name == "PuertaCuarto"))
            {
                UnityEditor.SerializedObject so = new UnityEditor.SerializedObject(p);
                float total = so.FindProperty("segundosAlNegro").floatValue + so.FindProperty("segundosEnNegro").floatValue + so.FindProperty("segundosDesdeNegro").floatValue;
                Assert.LessOrEqual(total, 0.7f, p.name + ": fundido corto y limpio");
            }
        }

        [Test]
        public void Callejon_PuertasConLamparaYCosasPegadasALosMuros()
        {
            Transform c = escena.GetRootGameObjects().First(g => g.name == "S2_Callejon").transform;
            Assert.GreaterOrEqual(c.GetComponentsInChildren<Transform>().Count(t => t.name == "N01"), 2, "lámparas de jaula sobre las puertas");
            Assert.IsNotNull(c.Find("L02_PuertaTrasera"));
            foreach (Transform t in c)
            {
                if (t.name == "Luces")
                {
                    continue;
                }
                Bounds b = t.GetComponentsInChildren<Renderer>().Select(r => r.bounds).Aggregate((a, x) => { a.Encapsulate(x); return a; });
                bool pegado = b.min.z < MechaCyberpunk.Arte.Editor.ColocarS2Callejon.MuroSur + 1.3f || b.max.z > MechaCyberpunk.Arte.Editor.ColocarS2Callejon.MuroNorte - 1.3f;
                Assert.IsTrue(pegado, t.name + " estorba en el centro del callejón (ruta de la demo)");
            }
        }

        [Test]
        public void CallesDeSalida_CadaEdificioVestidoConElKit_ConLodYSinTocarLaCalle()
        {
            Transform afueras = escena.GetRootGameObjects().First(g => g.name == "T530_Afueras").transform;
            Transform calles = escena.GetRootGameObjects().First(g => g.name == "S2_Calles").transform;
            int edificios = afueras.GetComponentsInChildren<Transform>(true).Count(t => t.name == "Edificio");
            LODGroup[] kits = calles.GetComponentsInChildren<LODGroup>(true);
            Assert.AreEqual(edificios, kits.Length, "cada cubo de las salidas lleva su kit");
            foreach (LODGroup lod in kits)
            {
                LOD[] lods = lod.GetLODs();
                Assert.AreEqual(2, lods.Length, lod.name);
                Assert.Greater(lods[0].renderers.Length, 5, lod.name + ": de cerca se ve el kit (planta baja, pisos, cornisa)");
                Assert.IsTrue(lods[0].renderers.Any(r => r.name.StartsWith("C0") || r.transform.parent.name.StartsWith("C0")), lod.name + " con locales o viviendas en la planta baja");
            }
            Assert.IsEmpty(calles.GetComponentsInChildren<Collider>(true), "el kit no estorba: las calles y colisiones son las de T-530");
            Assert.Greater(calles.GetComponentsInChildren<LineRenderer>(true).Length, 20, "cables cruzando las calles");
        }
    }
}
