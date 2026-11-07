using System;
using System.Collections;
using System.Linq;
using MechaCyberpunk.Core;
using System.IO;
using MechaCyberpunk.IA.APie;
using MechaCyberpunk.Progresion.Guardado;
using MechaCyberpunk.Progresion.Prologo.Pandillas;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Tests.Progresion
{
    /// <summary>
    /// T-713: la misión de los SKULLS del Tianguis en la Ciudad real. Salen todos a la vez (de día también), no reaparecen al morir ni al irte y regresar,
    /// y al matarlos a todos la misión avanza: sale el jefe, y al vencerlo el Tianguis es tuyo. F6 con el teclado simulado salta hasta el paso.
    /// </summary>
    public class SkullsTianguisT713Pruebas : InputTestFixture
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const int Total = 7;
        private Keyboard teclado;
        private Func<float> azarAnterior;
        private string carpetaGuardado;

        public override void Setup()
        {
            base.Setup();
            teclado = InputSystem.AddDevice<Keyboard>();
            InputSystem.AddDevice<Mouse>();
        }

        public override void TearDown()
        {
            Time.timeScale = 1f;
            if (azarAnterior != null)
            {
                PistolaPandillero.Azar = azarAnterior;
            }
            base.TearDown();
        }

        [UnityTearDown]
        public IEnumerator DescargarEscena()
        {
            foreach (GameObject raiz in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                Object.DestroyImmediate(raiz); // inmediato: el jugador comparte las acciones de entrada y un Destroy diferido las apagaría para la prueba que sigue
            }
            Registros.LimpiarTodos();
            ServicioGuardado.RutaForzada = null;
            if (carpetaGuardado != null && Directory.Exists(carpetaGuardado))
            {
                Directory.Delete(carpetaGuardado, true);
            }
            yield return null;
        }

        private static IEnumerator EsperarHasta(Func<bool> condicion, string que, float segundos)
        {
            float fin = Time.realtimeSinceStartup + segundos;
            while (!condicion())
            {
                Assert.Less(Time.realtimeSinceStartup, fin, "Tiempo agotado esperando: " + que);
                yield return null;
            }
        }

        private IEnumerator Pulsar(ButtonControl tecla)
        {
            Press(tecla);
            yield return null;
            Release(tecla);
            yield return null;
        }

        private static PandillaAPie[] GruposTianguis() => Object.FindObjectsByType<PandillaAPie>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(g => g.Pandilla == IdPandilla.Calaveras && g.ModoMision).ToArray();

        private static int EnPie() => GruposTianguis().Sum(g => g.gameObject.activeInHierarchy ? g.Presentes : 0);

        private static string Objetivo => Registro<IObjetivos>.Actual.Actual.Texto;

        private static IEnumerator Esperar(float segundos)
        {
            float fin = Time.time + segundos;
            while (Time.time < fin)
            {
                yield return null;
            }
        }

        // Los mata con daño directo, como una bala del jugador (cada uno cae donde está, dentro de la zona).
        private static void Matar(PandilleroAPie p)
        {
            p.RecibirDanio(new InfoDanio(100000f, TipoDanio.Disparo, p.transform.position + Vector3.up, Vector3.forward, null));
        }

        private static IEnumerator Alejar(IJugador jugador, Vector3 desde)
        {
            jugador.Colocar(new Pose(desde + new Vector3(0f, 0f, -140f), Quaternion.identity));
            yield return null;
        }

        private IEnumerator EmpezarEnElTianguis(bool deDia)
        {
            yield return null;
            carpetaGuardado = Path.Combine(Application.temporaryCachePath, "skulls_tianguis_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(carpetaGuardado);
            ServicioGuardado.RutaForzada = Path.Combine(carpetaGuardado, "partida.json");
            yield return EditorSceneManager.LoadSceneInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            azarAnterior = PistolaPandillero.Azar;
            PistolaPandillero.Azar = () => 1f; // no le pegan al jugador mientras se prueba
            DirectorPandillas director = null;
            yield return EsperarHasta(() =>
            {
                director = Object.FindFirstObjectByType<DirectorPandillas>();
                return director != null && director.Iniciado && Registro<IJugador>.Actual != null && Registro<ITerritorio>.Actual != null
                    && Registro<IRelojMundo>.Actual != null && Registro<IObjetivos>.Actual != null;
            }, "el director arrancó", 40f);
            IRelojMundo reloj = Registro<IRelojMundo>.Actual;
            reloj.AvanzarHasta(deDia ? 12f : 23f);
            reloj.Pausar(true);
            yield return null;
            Assert.AreEqual(!deDia, reloj.EsDeNoche);
            for (int i = 0; i < 10 && director.Paso != PasoHistoria.Tianguis; i++)
            {
                yield return Pulsar(teclado.f6Key);
            }
            Assert.AreEqual(PasoHistoria.Tianguis, director.Paso, "F6 llevó la historia al Tianguis");
        }

        // Foto desde arriba de todos los SKULLS de la misión juntos (con CAPTURAR_S1=1).
        private IEnumerator Capturar(string archivo)
        {
            if (Environment.GetEnvironmentVariable("CAPTURAR_S1") != "1")
            {
                yield break;
            }
            Vector3[] puntos = GruposTianguis().SelectMany(g => g.Pandilleros).Where(p => p.gameObject.activeInHierarchy).Select(p => p.transform.position).ToArray();
            Bounds caja = new Bounds(puntos[0], Vector3.zero);
            foreach (Vector3 p in puntos)
            {
                caja.Encapsulate(p);
            }
            GameObject objeto = new GameObject("CamaraCaptura");
            Camera camara = objeto.AddComponent<Camera>();
            camara.farClipPlane = 400f;
            float radio = Mathf.Max(caja.extents.magnitude, 8f);
            // La posición (en un anillo alrededor del grupo) desde la que se ven más SKULLS sin nada sólido en medio.
            Vector3 mejor = caja.center + new Vector3(radio, radio, -radio);
            int vistos = -1;
            Physics.SyncTransforms();
            foreach (float vuelta in new[] { 1.2f, 1.8f, 2.6f })
            {
                foreach (float altura in new[] { 4f, 8f, 14f })
                {
                    for (int grados = 0; grados < 360; grados += 20)
                    {
                        Vector3 candidata = caja.center + Quaternion.Euler(0f, grados, 0f) * Vector3.forward * radio * vuelta + Vector3.up * altura;
                        if (Physics.CheckSphere(candidata, 0.6f, ~0, QueryTriggerInteraction.Ignore))
                        {
                            continue;
                        }
                        int cuenta = puntos.Count(p => !Physics.Linecast(candidata, p + Vector3.up * 1.2f, ~0, QueryTriggerInteraction.Ignore));
                        if (cuenta > vistos)
                        {
                            vistos = cuenta;
                            mejor = candidata;
                        }
                    }
                }
            }
            objeto.transform.position = mejor;
            objeto.transform.LookAt(caja.center + Vector3.up);
            yield return null;
            RenderTexture rt = new RenderTexture(1280, 720, 24);
            camara.targetTexture = rt;
            camara.Render();
            RenderTexture.active = rt;
            Texture2D foto = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            foto.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            foto.Apply();
            camara.targetTexture = null;
            RenderTexture.active = null;
            System.IO.File.WriteAllBytes(System.IO.Path.Combine(Application.dataPath, "..", "docs", "capturas", archivo), foto.EncodeToPNG());
            Object.Destroy(foto);
            Object.Destroy(rt);
            Object.Destroy(objeto);
        }

        private IEnumerator MisionCompleta(bool deDia)
        {
            yield return EmpezarEnElTianguis(deDia);
            DirectorPandillas director = Object.FindFirstObjectByType<DirectorPandillas>();
            IJugador jugador = Registro<IJugador>.Actual;
            ITerritorio territorio = Registro<ITerritorio>.Actual;
            Vector3 inicio = jugador.Raiz.position;

            // 1) Salen todos a la vez.
            yield return EsperarHasta(() => EnPie() == Total, "los " + Total + " SKULLS a la vez (hay " + EnPie() + ")", 5f);
            Assert.AreEqual(Total, EnPie(), "Salen los " + Total + " SKULLS de la misión juntos (" + (deDia ? "de día" : "de noche") + ")");
            Assert.AreEqual(DirectorPandillas.TextoTianguis(0, Total), Objetivo);
            yield return Capturar(deDia ? "s1c-T-713-skulls-dia.png" : "s1c-T-713-skulls-noche.png");

            // 2) Matan a 3, te vas lejos mucho tiempo y vuelves: no reaparecen.
            PandilleroAPie[] todos = GruposTianguis().SelectMany(g => g.Pandilleros).ToArray();
            Assert.AreEqual(Total, todos.Length);
            for (int i = 0; i < 3; i++)
            {
                Matar(todos[i]);
            }
            yield return EsperarHasta(() => Objetivo == DirectorPandillas.TextoTianguis(3, Total), "el objetivo cuenta 3/" + Total + " (dice " + Objetivo + ")", 3f);
            yield return Alejar(jugador, inicio);
            Time.timeScale = 20f;
            yield return Esperar(150f); // 7.5 s reales: pasa de sobra el tiempo de reaparición de los grupos normales
            Time.timeScale = 1f;
            Assert.AreEqual(Total - 3, EnPie(), "Los 3 vencidos no reaparecen aunque pase el tiempo y estés lejos");
            jugador.Colocar(new Pose(inicio, Quaternion.identity));
            yield return Esperar(1.5f);
            Assert.AreEqual(Total - 3, EnPie(), "Ni al regresar");

            // 3) Matan a los demás: la misión avanza (sale el jefe y su paso).
            foreach (PandilleroAPie p in todos.Where(p => p.gameObject.activeSelf && !p.EstaKO).ToArray())
            {
                Matar(p);
            }
            yield return EsperarHasta(() => director.Paso == PasoHistoria.Jefe, "el paso del jefe al vencer a todos", 5f);
            Assert.AreEqual(DirectorPandillas.TextoJefe, Objetivo);
            Assert.IsTrue(territorio.JefeDisponible("tianguis"));
            Assert.AreEqual(0, EnPie(), "Todos vencidos");

            // 4) No vuelven tampoco en el paso del jefe; y el jefe cae: el Tianguis es tuyo.
            yield return Alejar(jugador, inicio);
            Time.timeScale = 20f;
            yield return Esperar(150f);
            Time.timeScale = 1f;
            Assert.AreEqual(0, EnPie(), "Nadie reaparece en el paso del jefe");
            JefePandilla jefe = Object.FindObjectsByType<JefePandilla>(FindObjectsInactive.Include, FindObjectsSortMode.None).First(j => j.Zona == "tianguis");
            yield return EsperarHasta(() => jefe.Vivo, "aparece el jefe", 10f);
            jefe.Jefe.RecibirDanio(new InfoDanio(100000f, TipoDanio.Disparo, jefe.Jefe.transform.position, Vector3.forward, null));
            yield return EsperarHasta(() => director.Paso == PasoHistoria.NeonAlto, "la misión del Tianguis termina", 5f);
            Assert.AreEqual(IdPandilla.Jugador, territorio.Dueno("tianguis"));
        }

        [UnityTest]
        public IEnumerator DeNoche_SalenTodosJuntos_NoReaparecen_YLaMisionAvanzaAlMatarlos()
        {
            yield return MisionCompleta(false);
        }

        [UnityTest]
        public IEnumerator DeDia_SalenTodosJuntos_NoReaparecen_YLaMisionAvanzaAlMatarlos()
        {
            yield return MisionCompleta(true);
        }
    }
}
