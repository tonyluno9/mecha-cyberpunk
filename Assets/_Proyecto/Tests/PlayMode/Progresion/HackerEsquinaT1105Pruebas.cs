using System;
using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.Progresion.Apoyo;
using MechaCyberpunk.Progresion.Pruebas;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Tests.Progresion
{
    /// <summary>
    /// T-1105: SERGIO (EL HACKER) se movió a la esquina noroeste del casino con su escritorio. Su interacción se movió con él: desde 1.5 m, del lado
    /// abierto de su silla, E lo usa (subtítulo) y el aviso del HUD lo ofrece.
    /// </summary>
    public class HackerEsquinaT1105Pruebas : InputTestFixture
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const string IdHacker = "hacker";
        private const float MetrosAlHacker = 1.5f;
        private const float AlturaPiso = 0.2f;

        private Keyboard teclado;
        private Mouse raton;

        public override void Setup()
        {
            InputSystem.DisableAllEnabledActions();
            base.Setup();
            teclado = InputSystem.AddDevice<Keyboard>();
            raton = InputSystem.AddDevice<Mouse>();
            MapasDeAccionesDePrueba.Reiniciar(teclado, raton);
        }

        public override void TearDown()
        {
            MapasDeAccionesDePrueba.AntesDelTearDown(teclado, raton);
            base.TearDown();
            MapasDeAccionesDePrueba.DespuesDelTearDown();
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

        [UnityTest]
        public IEnumerator Hacker_SeHablaConEDesdeUnMetroYMedioEnSuEsquina()
        {
            yield return EditorSceneManager.LoadSceneInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            float fin = Time.realtimeSinceStartup + 30f;
            while (Registro<IJugador>.Actual == null && Time.realtimeSinceStartup < fin)
            {
                yield return null;
            }
            IJugador jugador = Registro<IJugador>.Actual;
            Assert.IsNotNull(jugador, "hay jugador");
            yield return new WaitForSecondsRealtime(1f);

            PersonajeApoyo hacker = null;
            foreach (PersonajeApoyo p in Object.FindObjectsByType<PersonajeApoyo>(FindObjectsSortMode.None))
            {
                if (p.Datos.Id == IdHacker)
                {
                    hacker = p;
                }
            }
            Assert.IsNotNull(hacker, "existe EL HACKER");
            Vector3 posicion = hacker.transform.position;
            Assert.Less(posicion.x, 64f, "SERGIO está en la esquina oeste del casino (" + posicion + ")");

            // Del lado abierto de su silla (hacia el salón, +x), mirándolo.
            Vector3 frente = new Vector3(posicion.x + MetrosAlHacker, AlturaPiso + 0.1f, posicion.z);
            jugador.Colocar(new Pose(frente, Quaternion.LookRotation(Vector3.left, Vector3.up)));
            yield return null;
            yield return null;
            yield return new WaitForSecondsRealtime(0.3f);
            Assert.AreEqual(MetrosAlHacker, Vector3.Distance(new Vector3(jugador.Raiz.position.x, 0f, jugador.Raiz.position.z), new Vector3(posicion.x, 0f, posicion.z)), 0.4f,
                "el jugador quedó a 1.5 m");
            Assert.IsTrue(hacker.PuedeUsar, "el aviso del HUD ofrece hablar con SERGIO");

            string usado = null;
            hacker.Usado += id => usado = id;
            Press(teclado.eKey);
            yield return null;
            Release(teclado.eKey);
            yield return null;
            Assert.AreEqual(IdHacker, usado, "E usa a SERGIO");
            Assert.IsNotEmpty(hacker.UltimoTexto, "SERGIO dice algo");
        }
    }
}
