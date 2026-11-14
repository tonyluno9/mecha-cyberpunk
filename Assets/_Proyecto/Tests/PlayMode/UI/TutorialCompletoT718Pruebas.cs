using System;
using System.Collections;
using System.IO;
using MechaCyberpunk.Core;
using MechaCyberpunk.Economia;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Personaje.Vida;
using MechaCyberpunk.Progresion.Guardado;
using MechaCyberpunk.Progresion.Opciones;
using MechaCyberpunk.Progresion.Prologo.Pandillas;
using MechaCyberpunk.UI.Hud;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Tests.UI
{
    /// <summary>
    /// T-718: el tutorial de la ciudad como un jugador nuevo, con entrada simulada del Input System: moverse, correr, agacharse, golpear, el mapa, el teléfono, los objetos
    /// y el escudo. Cada aviso muestra la tecla real de su acción (GetBindingDisplayString), también si el jugador la cambió. Guardados y opciones en una carpeta temporal.
    /// Atajo anotado: los avisos de apuntar, disparar, recargar, cubrirse y rodar (que se hacen con el arma y una cobertura) se dan por hechos con Regla.Hecha;
    /// su texto sí se comprueba con la tecla real.
    /// </summary>
    public class TutorialCompletoT718Pruebas : InputTestFixture
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private Keyboard teclado;
        private Mouse raton;
        private string carpeta;

        public override void Setup()
        {
            base.Setup();
            teclado = InputSystem.AddDevice<Keyboard>();
            raton = InputSystem.AddDevice<Mouse>();
            DescartarEstadoDeMapas(new InputDevice[] { teclado, raton });
            carpeta = Path.Combine(Path.GetTempPath(), "t718_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(carpeta);
            ServicioGuardado.RutaForzada = Path.Combine(carpeta, "partida.json");
            ServicioOpciones.FijarRuta(Path.Combine(carpeta, ServicioOpciones.NombreArchivo));
        }

        public override void TearDown()
        {
            ServicioGuardado.RutaForzada = null;
            ServicioOpciones.FijarRuta(null);
            Time.timeScale = 1f;
            AudioListener.pause = false;
            Pausa.Fijar(false);
            try
            {
                Directory.Delete(carpeta, true);
            }
            catch (Exception)
            {
                // La carpeta temporal se queda; no importa.
            }
            InputSystem.DisableAllEnabledActions();
            DescartarEstadoDeMapas(new InputDevice[] { teclado, raton });
            base.TearDown();
            DescartarEstadoDeMapas(new InputDevice[0]);
        }

        private static void DescartarEstadoDeMapas(InputDevice[] dispositivos)
        {
            foreach (string guid in AssetDatabase.FindAssets("t:InputActionAsset"))
            {
                InputActionAsset asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset != null)
                {
                    asset.Disable();
                    asset.devices = dispositivos;
                    asset.devices = null;
                }
            }
        }

        [UnityTearDown]
        public IEnumerator DescargarEscena()
        {
            Time.timeScale = 1f;
            foreach (GameObject raiz in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                Object.Destroy(raiz);
            }
            Registros.LimpiarTodos();
            yield return null;
        }

        private static IEnumerator Esperar(Func<bool> condicion, string que, float segundos)
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
            yield return null;
        }

        private static IEnumerator EsperarAviso(TutorialLigero tutorial, AccionTutorial accion)
        {
            yield return Esperar(() => tutorial.Visible && tutorial.Regla.Aviso == accion, "el aviso " + accion, 25f);
        }

        private static void Foto(string nombre)
        {
            if (Environment.GetEnvironmentVariable("CAPTURAR_S1") != "1") return;
            CapturaUI.Capturar("docs/capturas/s1c-T-718-" + nombre + ".png", Registro<IJugador>.Actual.Camara);
        }

        private static void TextoConLaTecla(TutorialLigero tutorial, AccionTutorial accion, string tecla)
        {
            StringAssert.Contains(tecla.ToUpperInvariant(), tutorial.Texto.text, accion + ": el aviso muestra la tecla real (" + tecla + ")");
        }

        [UnityTest]
        public IEnumerator Tutorial_DeMoverseAlEscudo_ConLasTeclasReales()
        {
            yield return EditorSceneManager.LoadSceneInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            DirectorPandillas director = null;
            TutorialLigero tutorial = null;
            yield return Esperar(() =>
            {
                director = Object.FindFirstObjectByType<DirectorPandillas>();
                tutorial = Object.FindFirstObjectByType<TutorialLigero>();
                return director != null && director.Iniciado && Registro<IJugador>.Actual != null && Registro<IOpciones>.Actual is ServicioOpciones
                    && tutorial != null && Registro<ServicioInventario>.Actual != null && Registro<IEscudo>.Actual != null;
            }, "la ciudad arrancó con el tutorial", 40f);
            // Sin la cinemática del asalto: la historia en un paso tranquilo (el asalto tiene su propia prueba).
            director.Cargar("{\"paso\":\"Inventor\"}");
            yield return null;
            IJugador jugador = Registro<IJugador>.Actual;
            ServicioInventario inventario = Registro<ServicioInventario>.Actual;
            inventario.Agregar((DatosConsumible)inventario.Catalogo.Buscar("bebida_energetica"), 2);

            // 1. Moverse.
            yield return EsperarAviso(tutorial, AccionTutorial.Moverse);
            TextoConLaTecla(tutorial, AccionTutorial.Moverse, TeclasHud.TextoMover(TeclasHud.Buscar("Mover"), "?"));
            Press(teclado.wKey);
            yield return null;
            Assert.IsTrue(tutorial.Regla.Vista(AccionTutorial.Moverse), "W da por vista la acción de moverse");

            // 2. Correr: Shift con W.
            yield return EsperarAviso(tutorial, AccionTutorial.Correr);
            TextoConLaTecla(tutorial, AccionTutorial.Correr, TeclasHud.Texto(TeclasHud.Buscar("Correr"), "?"));
            Foto("aviso-correr");
            Press(teclado.leftShiftKey);
            for (int i = 0; i < 6; i++) yield return null;
            Release(teclado.leftShiftKey);
            Release(teclado.wKey);
            Assert.IsTrue(tutorial.Regla.Vista(AccionTutorial.Correr), "Shift + W da por vista la acción de correr");

            // 3. Agacharse: Ctrl.
            yield return EsperarAviso(tutorial, AccionTutorial.Agacharse);
            TextoConLaTecla(tutorial, AccionTutorial.Agacharse, tutorial.TeclaReal(ReglaTutorial.TeclaAgacharse));
            Foto("aviso-agacharse");
            yield return Pulsar(teclado.leftCtrlKey);
            Assert.IsTrue(tutorial.Regla.Vista(AccionTutorial.Agacharse), "Ctrl da por vista la acción de agacharse");
            yield return Pulsar(teclado.leftCtrlKey); // se vuelve a levantar

            // 4. Golpear: clic con los puños.
            yield return EsperarAviso(tutorial, AccionTutorial.Golpear);
            TextoConLaTecla(tutorial, AccionTutorial.Golpear, TeclasHud.Texto(TeclasHud.Buscar("Atacar"), "?"));
            Press(raton.leftButton);
            for (int i = 0; i < 4; i++) yield return null;
            Release(raton.leftButton);
            yield return null;
            Assert.IsTrue(tutorial.Regla.Vista(AccionTutorial.Golpear), "El clic da por vista el golpe");

            // 5. Con la pistola en la mano (la historia la da en el paso Pistola): se ofrecen apuntar, disparar y recargar.
            ArsenalJugador arsenal = Registro<ArsenalJugador>.Actual;
            inventario.Agregar(arsenal.Catalogo.Buscar(ArmaAPie.Pistola, 0), 1);
            jugador.Raiz.GetComponentInChildren<ArmasAPie>().Equipar(ArmaAPie.Pistola); // atajo anotado: el cambio de arma con la tecla 2 se prueba en las pruebas del arsenal
            yield return null;
            Assert.AreEqual(ArmaAPie.Pistola, jugador.Raiz.GetComponentInChildren<ArmasAPie>().ArmaActiva, "La pistola quedó en la mano");
            // Las que se hacen con arma y cobertura: se comprueba el texto con la tecla real y se dan por hechas.
            foreach (AccionTutorial accion in new[] { AccionTutorial.Apuntar, AccionTutorial.Disparar, AccionTutorial.Recargar, AccionTutorial.Cubrirse, AccionTutorial.Rodar })
            {
                yield return EsperarAviso(tutorial, accion);
                StringAssert.Contains(ReglaTutorial.Texto(accion, tutorial.TeclaReal), tutorial.Texto.text);
                tutorial.Regla.Hecha(accion);
                yield return null;
            }

            // 6. El mapa y el teléfono con sus teclas.
            tutorial.Regla.Hecha(AccionTutorial.SubirAlCarro); // sin carro cerca no se ofrece: se salta para seguir (se comprueba en RecorridoDemoPruebas)
            tutorial.Regla.Hecha(AccionTutorial.Robar);
            tutorial.Regla.Hecha(AccionTutorial.Moto);
            yield return EsperarAviso(tutorial, AccionTutorial.Mapa);
            TextoConLaTecla(tutorial, AccionTutorial.Mapa, tutorial.TeclaReal(ReglaTutorial.TeclaMapa));
            yield return Pulsar(teclado.mKey);
            Assert.IsTrue(tutorial.Regla.Vista(AccionTutorial.Mapa));
            yield return Pulsar(teclado.mKey); // se cierra
            yield return EsperarAviso(tutorial, AccionTutorial.Misiones);
            TextoConLaTecla(tutorial, AccionTutorial.Misiones, TeclasHud.Texto(TeclasHud.Buscar("Telefono"), "?"));
            yield return Pulsar(teclado.tKey);
            Assert.IsTrue(tutorial.Regla.Vista(AccionTutorial.Misiones));
            yield return Pulsar(teclado.tKey); // se cierra

            // 7. Los objetos y las armas: se mantiene Tab (la rueda).
            yield return EsperarAviso(tutorial, AccionTutorial.Consumibles);
            Foto("aviso-objetos");
            StringAssert.Contains(tutorial.TeclaReal(ReglaTutorial.TeclaRueda).ToUpperInvariant(), tutorial.Texto.text);
            yield return Pulsar(teclado.tabKey);
            Assert.IsTrue(tutorial.Regla.Vista(AccionTutorial.Consumibles), "Tab (la rueda) da por vistos los objetos");

            // 8. El escudo: un golpe que absorbe.
            yield return EsperarAviso(tutorial, AccionTutorial.Escudo);
            VidaAPie vida = jugador.Raiz.GetComponent<VidaAPie>();
            IEscudo escudo = Registro<IEscudo>.Actual;
            vida.RecibirDanio(new InfoDanio(Mathf.Min(10f, escudo.Actual * 0.5f), TipoDanio.Disparo, jugador.Raiz.position + Vector3.up, Vector3.forward, null));
            yield return null;
            Assert.IsTrue(tutorial.Regla.Vista(AccionTutorial.Escudo), "El escudo que absorbe da por visto su aviso");

            foreach (AccionTutorial accion in Enum.GetValues(typeof(AccionTutorial)))
            {
                Assert.IsTrue(tutorial.Regla.Vista(accion), "Quedó pendiente " + accion);
            }
        }

        [UnityTest]
        public IEnumerator Tutorial_ConLaTeclaCambiada_MuestraLaNueva()
        {
            yield return EditorSceneManager.LoadSceneInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            TutorialLigero tutorial = null;
            yield return Esperar(() =>
            {
                DirectorPandillas d = Object.FindFirstObjectByType<DirectorPandillas>();
                tutorial = Object.FindFirstObjectByType<TutorialLigero>();
                return d != null && d.Iniciado && Registro<IJugador>.Actual != null && Registro<IOpciones>.Actual is ServicioOpciones && tutorial != null;
            }, "la ciudad arrancó con el tutorial", 40f);
            InputAction correr = TeclasHud.Buscar("Correr");
            Assert.IsNotNull(correr);
            Assert.AreEqual("Shift", tutorial.TeclaReal(ReglaTutorial.TeclaCorrer));
            correr.ApplyBindingOverride("<Keyboard>/z");
            try
            {
                StringAssert.Contains("Z", tutorial.TeclaReal(ReglaTutorial.TeclaCorrer).ToUpperInvariant(), "Con la tecla cambiada el aviso muestra la nueva");
                StringAssert.Contains("Z", ReglaTutorial.Texto(AccionTutorial.Correr, tutorial.TeclaReal));
            }
            finally
            {
                correr.RemoveAllBindingOverrides();
            }
        }
    }
}
