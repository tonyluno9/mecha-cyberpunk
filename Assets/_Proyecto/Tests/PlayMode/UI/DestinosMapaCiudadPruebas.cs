using System;
using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.APie;
using MechaCyberpunk.Progresion.Prologo.Pandillas;
using MechaCyberpunk.UI.Mapa;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.UI
{
    /// <summary>
    /// T-667 y T-668 en la ciudad de verdad (Ciudad_Prologo): los ocho lugares se encuentran en la escena, un clic sobre el icono del casino lo marca y el GPS
    /// traza la ruta por las calles en el mapa grande y en el minimapa. Guarda las capturas docs/capturas/T-667-mapa-iconos.png, T-668-mapa-ruta.png
    /// y T-668-minimapa-ruta.png.
    /// </summary>
    public class DestinosMapaCiudadPruebas : InputTestFixture
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";

        private Mouse raton;

        public override void Setup()
        {
            base.Setup();
            InputSystem.AddDevice<Keyboard>();
            raton = InputSystem.AddDevice<Mouse>();
        }

        public override void TearDown()
        {
            Time.timeScale = 1f;
            PuntoMarcadoMapa.Quitar();
            base.TearDown();
        }

        [UnityTearDown]
        public IEnumerator DescargarEscena()
        {
            foreach (GameObject raiz in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                UnityEngine.Object.Destroy(raiz);
            }
            Registros.LimpiarTodos();
            CatalogoDestinosMapa.Limpiar();
            yield return null;
        }

        private IEnumerator Clic(Vector2 pantalla)
        {
            Set(raton.position, pantalla);
            yield return null;
            Press(raton.leftButton);
            yield return null;
            Release(raton.leftButton);
            yield return null;
        }

        [UnityTest]
        public IEnumerator LosOchoLugares_SeEncuentranEnLaCiudad_ElClicMarcaAlTino_YElGpsTrazaLaRuta()
        {
            yield return EditorSceneManager.LoadSceneInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            float fin = Time.realtimeSinceStartup + 30f;
            DirectorPandillas director = null;
            while (Time.realtimeSinceStartup < fin)
            {
                director = UnityEngine.Object.FindFirstObjectByType<DirectorPandillas>();
                if (director != null && director.Iniciado && Registro<IJugador>.Actual != null)
                {
                    break;
                }
                yield return null;
            }
            Assert.IsNotNull(director, "La ciudad arrancó");
            yield return new WaitForSecondsRealtime(0.5f);
            foreach (PandillaAPie grupo in UnityEngine.Object.FindObjectsByType<PandillaAPie>(FindObjectsSortMode.None))
            {
                grupo.gameObject.SetActive(false);
            }
            IJugador jugador = Registro<IJugador>.Actual;
            Camera camara = jugador.Camara;
            Minimapa minimapa = UnityEngine.Object.FindFirstObjectByType<Minimapa>();
            PantallaMapa mapa = UnityEngine.Object.FindFirstObjectByType<PantallaMapa>();
            Assert.IsNotNull(minimapa);
            Assert.IsNotNull(mapa);
            Assert.IsNotNull(Registro<IRutaCalles>.Actual, "Trafico registró el trazador de rutas por calles");

            foreach (DestinoMapa destino in CatalogoDestinosMapa.Todos)
            {
                Assert.IsTrue(CatalogoDestinosMapa.Posicion(destino.Id, out Vector3 donde), destino.Id + " se encuentra en la ciudad");
                Debug.Log("[Mapa] " + destino.Id + " en " + donde);
            }

            // El jugador en la calle del tianguis, mirando al este; el mapa grande con todos los iconos.
            Vector3 sitio = new Vector3(-20f, 0.5f, 0f);
            jugador.Colocar(new Pose(sitio, Quaternion.LookRotation(Vector3.right)));
            yield return null;
            yield return null;
            mapa.Abrir();
            yield return null;
            yield return null;
            foreach (DestinoMapa destino in CatalogoDestinosMapa.Todos)
            {
                bool tieneIcono = mapa.IconoDestino(destino.Id) != null || CatalogoDestinosMapa.LoDibujaUnaMarca(destino.Id);
                Assert.IsTrue(tieneIcono, "El mapa grande tiene el icono de " + destino.Id);
            }

            // Un clic sobre Tino lo marca; el mapa traza la ruta.
            CatalogoDestinosMapa.Posicion(IdDestinoMapa.Casino, out Vector3 tino); // el casino queda lejos: la ruta se ve larga
            yield return Clic(mapa.PantallaDe(tino));
            Assert.AreEqual(IdDestinoMapa.Casino, PuntoMarcadoMapa.Destino, "El clic sobre el icono marca el casino");
            yield return null;
            Assert.GreaterOrEqual(mapa.Ruta.Count, 2, "El mapa traza la ruta");
            Assert.Greater(mapa.SegmentosRutaVisibles, 0);
            // Otro clic lo quita; las capturas van al final (CapturaUI cambia los canvas y ya no se podría hacer clic).
            yield return Clic(mapa.PantallaDe(tino));
            Assert.IsFalse(PuntoMarcadoMapa.Hay, "Otro clic sobre el casino lo quita");
            CapturaUI.Capturar("docs/capturas/T-667-mapa-iconos.png", camara);
            PuntoMarcadoMapa.MarcarDestino(IdDestinoMapa.Casino, tino);
            yield return null;
            yield return null;
            CapturaUI.Capturar("docs/capturas/T-668-mapa-ruta.png", camara);

            // El minimapa dibuja la misma ruta.
            mapa.Cerrar();
            yield return null;
            yield return null;
            minimapa.Actualizar();
            Assert.Greater(minimapa.SegmentosRutaVisibles, 0, "El minimapa dibuja la ruta GPS");
            Assert.IsTrue(minimapa.PuntoVisible);
            CapturaUI.Capturar("docs/capturas/T-668-minimapa-ruta.png", camara);
        }
    }
}
