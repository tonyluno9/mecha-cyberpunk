using System;
using System.Collections;
using System.IO;
using MechaCyberpunk.Core;
using MechaCyberpunk.Economia;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.Personaje.Jugador;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Tests.UI
{
    /// <summary>
    /// T-1013: capturas en la calle de Ciudad_Prologo, de día, del rifle de plasma (cargando y con el rayo), del arco de la granada y de la explosión del
    /// lanzagranadas, con el HUD. Solo corre con CAPTURAR_T991=1; deja docs/capturas/t1013/.
    /// </summary>
    public class CapturasArmasT1013Pruebas : InputTestFixture
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const float MetrosALaCalle = 9f;
        private Keyboard teclado;
        private Mouse raton;

        public override void Setup()
        {
            base.Setup();
            teclado = InputSystem.AddDevice<Keyboard>();
            raton = InputSystem.AddDevice<Mouse>();
        }

        [UnityTearDown]
        public IEnumerator DescargarEscena()
        {
            foreach (GameObject raiz in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                Object.Destroy(raiz);
            }
            Registros.LimpiarTodos();
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

        private static void Capturar(string nombre)
        {
            Directory.CreateDirectory(Path.Combine("docs", "capturas", "t1013"));
            CapturaUI.Capturar(Path.Combine("docs", "capturas", "t1013", nombre), Camera.main);
        }

        [UnityTest]
        public IEnumerator Capturas_PlasmaGranadaYLanzagranadasEnLaCalle()
        {
            if (Environment.GetEnvironmentVariable("CAPTURAR_T991") != "1")
            {
                Assert.Ignore("Sin CAPTURAR_T991=1 no se sacan capturas.");
            }
            yield return EditorSceneManager.LoadSceneInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            MechaCyberpunk.Progresion.Prologo.Pandillas.DirectorPandillas historia = null;
            yield return EsperarHasta(() =>
            {
                historia = Object.FindFirstObjectByType<MechaCyberpunk.Progresion.Prologo.Pandillas.DirectorPandillas>();
                return historia != null && historia.Iniciado && Registro<IJugador>.Actual != null && Registro<ServicioInventario>.Actual != null;
            }, "la escena arrancó", 30f);
            for (int i = 0; i < 12 && historia.Paso != MechaCyberpunk.Progresion.Prologo.Pandillas.PasoHistoria.Libre; i++)
            {
                Press(teclado.f6Key);
                yield return null;
                Release(teclado.f6Key);
                yield return null;
            }
            Registro<IRelojMundo>.Actual?.AvanzarHasta(16f);
            IJugador j = Registro<IJugador>.Actual;
            foreach (IPuntoAparicion p in Registro<IPuntoAparicion>.Todos)
            {
                if (p.Id == "Casa_Puerta")
                {
                    Vector3 sitio = p.Pose.position + p.Pose.rotation * Vector3.forward * MetrosALaCalle + Vector3.up * 0.05f;
                    j.Colocar(new Pose(sitio, p.Pose.rotation));
                }
            }
            ArsenalJugador arsenal = j.Raiz.GetComponentInChildren<ArsenalJugador>();
            ServicioInventario inventario = Registro<ServicioInventario>.Actual;
            foreach (ArmaAPie arma in new[] { ArmaAPie.RiflePlasma, ArmaAPie.Granada, ArmaAPie.Lanzagranadas })
            {
                inventario.Agregar(arsenal.Catalogo.Buscar(arma, 0), 1);
            }
            yield return new WaitForSecondsRealtime(0.5f);
            // En batchmode no se dibuja la cámara cada cuadro: sin esto el Animator no mueve los huesos y el arma queda donde no se ve.
            foreach (Animator a in j.Raiz.GetComponentsInChildren<Animator>())
            {
                a.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            }
            ArmasAPie armas = j.Raiz.GetComponentInChildren<ArmasAPie>();
            ArmasEspecialesAPie especiales = j.Raiz.GetComponentInChildren<ArmasEspecialesAPie>();

            // Para la foto se esconde a quien quede pegado al jugador (la acompañante tapa el cañón).
            foreach (SkinnedMeshRenderer r in Object.FindObjectsByType<SkinnedMeshRenderer>(FindObjectsSortMode.None))
            {
                if (r.transform.root != j.Raiz.root && Vector3.Distance(r.bounds.center, j.Raiz.position) < 3f)
                {
                    r.enabled = false;
                }
            }

            // Rifle de plasma: cargando y con el rayo.
            Assert.IsTrue(armas.Equipar(ArmaAPie.RiflePlasma));
            yield return new WaitForSecondsRealtime(0.5f);
            Press(raton.rightButton);
            yield return new WaitForSecondsRealtime(0.8f);
            Press(raton.leftButton);
            yield return new WaitForSecondsRealtime(2f);
            Assert.AreEqual(EstadoPlasma.Cargando, especiales.EstadoPlasma);
            Capturar("plasma-cargando.png");
            Transform bola = j.Raiz.Find("CargaPlasma");
            Transform boca = j.Raiz.GetComponentInChildren<PistolaAPie>().BocaDe(ArmaAPie.RiflePlasma);
            Assert.IsTrue(bola.gameObject.activeInHierarchy, "Cargando se ve la bola de energía");
            Assert.Less(Vector3.Distance(bola.position, boca.position), 0.15f, "La bola está en la boca del rifle");
            yield return new WaitForSecondsRealtime(1.6f);
            Assert.AreEqual(EstadoPlasma.Rayo, especiales.EstadoPlasma);
            Capturar("plasma-rayo.png");
            Release(raton.leftButton);
            yield return new WaitForSecondsRealtime(4.5f);
            Release(raton.rightButton);
            yield return new WaitForSecondsRealtime(0.5f);

            // Granada: el arco al apuntar.
            Assert.IsTrue(armas.Equipar(ArmaAPie.Granada));
            yield return new WaitForSecondsRealtime(0.5f);
            Press(raton.rightButton);
            yield return new WaitForSecondsRealtime(0.8f);
            Assert.IsTrue(especiales.ArcoVisible);
            Capturar("granada-arco.png");
            Release(raton.rightButton);
            yield return new WaitForSecondsRealtime(0.4f);

            // Lanzagranadas: el disparo y la explosión.
            Assert.IsTrue(armas.Equipar(ArmaAPie.Lanzagranadas));
            yield return new WaitForSecondsRealtime(0.5f);
            Press(raton.rightButton);
            yield return new WaitForSecondsRealtime(0.8f);
            int antes = ExplosionAPie.Cuantas;
            Press(raton.leftButton);
            yield return null;
            Release(raton.leftButton);
            yield return EsperarHasta(() => ExplosionAPie.Cuantas > antes, "la explosión", 6f);
            yield return new WaitForSecondsRealtime(0.25f);
            Capturar("lanzagranadas-explosion.png");
            Release(raton.rightButton);
        }
    }
}
