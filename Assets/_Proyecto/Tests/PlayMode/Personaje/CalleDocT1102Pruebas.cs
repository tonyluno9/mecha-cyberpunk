using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MechaCyberpunk.Personaje.Movimiento;
using MechaCyberpunk.Progresion.Pruebas;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>
    /// T-1102: en Ciudad_Prologo (escena real) el jugador, con entrada simulada (W), recorre la calle de x = -110.5 por las dos banquetas de
    /// la avenida (z = -12) al sur (z = -82) sin atorarse (W con Shift: a paso de caminata 70 m no caben en 25 s), entra y sale del andador al poniente, y de la casa del jugador a la puerta del Doc
    /// no hay un bloqueo nuevo (la escena no hornea NavMesh: se barre una cápsula del tamaño del jugador por los tramos de la ruta).
    /// </summary>
    public class CalleDocT1102Pruebas : InputTestFixture
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const float XBanquetaOriente = -105.5f;
        private const float XBanquetaPoniente = -115.2f;
        private const float XCapsula = -105.9f;
        private const float ZInicio = -12f;
        private const float ZFin = -82f;
        private const float AvanceMinimo = 60f;
        private const float SegundosMaximos = 25f;
        private const float RadioJugador = 0.3f;
        private const float AlturaJugador = 1.8f;
        private const float LlegadaAndador = 0.8f;
        private const float XAndador = -118f;
        private const float ZAndador = -61.5f;

        private Keyboard teclado;
        private GameObject jugador;
        private ControladorAPie controlador;
        private CharacterController cc;

        [UnitySetUp]
        public IEnumerator DescargarCiudadPrevia()
        {
            yield return DescargarCiudades("CalleDocT1102_VaciaAntes");
        }

        [UnityTearDown]
        public IEnumerator DescargarCiudadAlTerminar()
        {
            yield return DescargarCiudades("CalleDocT1102_VaciaDespues");
        }

        private static IEnumerator DescargarCiudades(string nombreVacia)
        {
            bool hayCiudad = false;
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                hayCiudad |= SceneManager.GetSceneAt(i).path.Contains("Ciudad");
            }
            if (!hayCiudad)
            {
                yield break;
            }
            SceneManager.SetActiveScene(SceneManager.CreateScene(nombreVacia));
            for (int i = SceneManager.sceneCount - 1; i >= 0; i--)
            {
                Scene s = SceneManager.GetSceneAt(i);
                if (s.path.Contains("Ciudad"))
                {
                    yield return SceneManager.UnloadSceneAsync(s);
                }
            }
        }

        public override void TearDown()
        {
            MapasDeAccionesDePrueba.AntesDelTearDown(teclado != null ? new InputDevice[] { teclado } : new InputDevice[0]);
            base.TearDown();
            MapasDeAccionesDePrueba.DespuesDelTearDown();
        }

        private IEnumerator Cargar()
        {
            teclado = InputSystem.AddDevice<Keyboard>();
            InputSystem.AddDevice<Mouse>();
            MapasDeAccionesDePrueba.Reiniciar(teclado);
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            controlador = Object.FindFirstObjectByType<ControladorAPie>();
            Assert.IsNotNull(controlador, "Ciudad_Prologo debe traer al jugador a pie");
            jugador = controlador.gameObject;
            cc = jugador.GetComponent<CharacterController>();
            foreach (string nombre in new[] { "DirectorPrologo", "Emboscada", "MostradorPeleas" })
            {
                GameObject g = GameObject.Find(nombre);
                if (g != null)
                {
                    g.SetActive(false);
                }
            }
            int frames = 0;
            while (!controlador.ControlActivo && frames++ < 200)
            {
                yield return null;
            }
            Assert.IsTrue(controlador.ControlActivo, "El jugador debe poder moverse en la escena");
            int estables = 0;
            int intentos = 0;
            while (estables < 10 && intentos++ < 1000)
            {
                yield return null;
                estables = Time.deltaTime < 0.05f ? estables + 1 : 0;
            }
        }

        private static float Piso(float x, float z)
        {
            Physics.SyncTransforms();
            RaycastHit[] hits = Physics.RaycastAll(new Vector3(x, 3f, z), Vector3.down, 6f, ~0, QueryTriggerInteraction.Ignore);
            Assert.IsNotEmpty(hits, "Sin piso en (" + x + ", " + z + ")");
            return hits.Max(h => h.point.y);
        }

        private void Poner(float x, float z, float haciaX, float haciaZ)
        {
            cc.enabled = false;
            jugador.transform.position = new Vector3(x, Piso(x, z) + 0.05f, z);
            jugador.transform.rotation = Quaternion.LookRotation(new Vector3(haciaX - x, 0f, haciaZ - z));
            cc.enabled = true;
            controlador.ReiniciarMovimiento();
        }

        /// <summary>Diagnóstico: lo que toca el jugador (cápsula un poco más ancha) en su posición.</summary>
        private string Entorno()
        {
            Vector3 p = jugador.transform.position;
            Collider[] cs = Physics.OverlapCapsule(p + Vector3.up * 0.45f, p + Vector3.up * 1.4f, 0.7f, ~0, QueryTriggerInteraction.Collide)
                .Where(c => c.transform.root != jugador.transform).ToArray();
            return p.ToString("F2") + " cerca: " + string.Join(" | ", cs.Select(c => c.transform.root.name + "/" + c.name + (c.isTrigger ? "(trigger)" : "") + " " + c.GetType().Name + " " + c.bounds.min.ToString("F2") + ".." + c.bounds.max.ToString("F2")));
        }

        private float recorrido;
        private readonly System.Text.StringBuilder traza = new System.Text.StringBuilder();
        private bool llego;

        /// <summary>Camina con W y Shift en línea recta (hacia donde se puso al jugador) hasta llegar al destino o agotar el tiempo.</summary>
        private IEnumerator Caminar(float x, float z, float segundos)
        {
            recorrido = 0f;
            llego = false;
            traza.Clear();
            int cuadro = 0;
            Press(teclado.wKey);
            yield return null;
            Press(teclado.leftShiftKey);
            Vector3 anterior = jugador.transform.position;
            float fin = Time.time + segundos;
            while (Time.time < fin)
            {
                Vector3 p = jugador.transform.position;
                Vector3 falta = new Vector3(x - p.x, 0f, z - p.z);
                if (falta.magnitude < LlegadaAndador)
                {
                    llego = true;
                    break;
                }
                yield return null;
                if (cuadro++ % 30 == 0)
                {
                    traza.Append(jugador.transform.position.x.ToString("F1")).Append(',').Append(jugador.transform.position.z.ToString("F1")).Append(' ');
                }
                Vector3 ahora = jugador.transform.position;
                recorrido += new Vector3(ahora.x - anterior.x, 0f, ahora.z - anterior.z).magnitude;
                anterior = ahora;
            }
            Release(teclado.leftShiftKey);
            Release(teclado.wKey);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Banquetas_OrienteYPoniente_AvanzaSesentaMetrosSinAtorarse()
        {
            yield return Cargar();
            foreach (float x in new[] { XBanquetaOriente, XBanquetaPoniente })
            {
                Poner(x, ZInicio, x, ZFin);
                float t0 = Time.time;
                yield return Caminar(x, ZFin, SegundosMaximos);
                float t = Time.time - t0;
                Assert.GreaterOrEqual(recorrido, AvanceMinimo, "Metros recorridos por la banqueta x=" + x + " en " + t.ToString("F1") + " s. " + Entorno() + " traza " + traza);
                Assert.LessOrEqual(t, SegundosMaximos + 0.5f, "Tiempo por la banqueta x=" + x);
            }
        }

        [UnityTest]
        public IEnumerator Andador_EntraAlPonienteYSale()
        {
            yield return Cargar();
            Poner(XBanquetaOriente, ZAndador, XAndador, ZAndador);
            yield return Caminar(XAndador, ZAndador, SegundosMaximos);
            Vector3 pa = jugador.transform.position;
            Assert.IsTrue(llego, "Debe llegar al poniente del andador (x " + pa.x.ToString("F1") + "). " + Entorno());
            // El movimiento sigue la orientación con que se puso al jugador: se le da la vuelta hacia el oriente.
            Poner(pa.x, pa.z, XBanquetaOriente, ZAndador);
            yield return Caminar(XBanquetaOriente, ZAndador, SegundosMaximos);
            Assert.IsTrue(llego, "Debe volver por el andador al oriente. " + Entorno());
        }

        [UnityTest]
        public IEnumerator DeLaCasaAlDoc_NoHayBloqueoNuevo()
        {
            yield return Cargar();
            // Puerta de la casa del jugador (x = -57, z -77), calle de la casa al norte, avenida al poniente y puerta del Doc (-102, -10).
            Vector2[] ruta =
            {
                new Vector2(-55.5f, -77f), new Vector2(-47f, -77f), new Vector2(-47f, -3f), new Vector2(-90f, -3f), new Vector2(-90f, -9.1f), new Vector2(-102f, -9.1f),
                new Vector2(-102f, -9.7f),
            };
            Physics.SyncTransforms();
            for (int i = 0; i + 1 < ruta.Length; i++)
            {
                Assert.IsNull(Bloqueo(ruta[i], ruta[i + 1]), "Bloqueo en el tramo " + i + " de la casa al Doc");
            }
            // Desde la calle nueva a la avenida y a la puerta del Doc.
            Vector2[] porLaCalle = { new Vector2(XCapsula, -30f), new Vector2(XCapsula, -9.1f), new Vector2(-102f, -9.1f), new Vector2(-102f, -9.7f) };
            for (int i = 0; i + 1 < porLaCalle.Length; i++)
            {
                Assert.IsNull(Bloqueo(porLaCalle[i], porLaCalle[i + 1]), "Bloqueo en el tramo " + i + " de la calle nueva al Doc");
            }
        }

        /// <summary>Barre la cápsula del jugador (sobre el escalón de la banqueta) de a a b; devuelve el nombre de lo que la frena o null.</summary>
        private string Bloqueo(Vector2 a, Vector2 b)
        {
            float ya = Piso(a.x, a.y), yb = Piso(b.x, b.y);
            Vector3 desde = new Vector3(a.x, Mathf.Max(ya, yb) + 0.45f, a.y);
            Vector3 dir = new Vector3(b.x - a.x, 0f, b.y - a.y);
            float largo = dir.magnitude;
            dir /= largo;
            Vector3 p0 = desde + Vector3.up * RadioJugador, p1 = desde + Vector3.up * (AlturaJugador - RadioJugador - 0.45f);
            RaycastHit[] hits = Physics.CapsuleCastAll(p0, p1, RadioJugador, dir, largo, ~0, QueryTriggerInteraction.Ignore)
                .Where(h => h.collider.transform.root != jugador.transform && h.distance > 0f).OrderBy(h => h.distance).ToArray();
            return hits.Length == 0 ? null : hits[0].collider.name + " en " + hits[0].point.ToString("F1") + " (" + hits[0].collider.transform.root.name + ")";
        }
    }
}
