using System;
using System.Collections;
using System.IO;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Movimiento;
using MechaCyberpunk.Progresion.Guardado;
using MechaCyberpunk.Progresion.Opciones;
using MechaCyberpunk.Progresion.Prologo.Pandillas;
using MechaCyberpunk.UI.Kit;
using MechaCyberpunk.UI.MenuPausa;
using MechaCyberpunk.UI.Opciones;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.UI
{
    /// <summary>
    /// T-411 en Ciudad_Prologo con rutas temporales: Esc → pausa → ↓ hasta OPCIONES → Enter; ↓ ↓ a VOLUMEN GENERAL y cuatro ← lo dejan en
    /// 60 % en el gestor al momento (EFECTOS sigue en 80 %); ↑ a INVERTIR Y y Enter; Esc vuelve a la pausa y Esc continúa; el ratón hacia
    /// arriba baja la cámara y con 2x gira el doble; CALIDAD BAJA pone 0.75 en la copia de URP sin tocar el asset del proyecto; un
    /// ServicioOpciones nuevo con el mismo archivo lee lo mismo. Con CAPTURAR_T411=1 guarda docs/capturas/T-411-1 en 1080p y 1440p.
    /// </summary>
    public class OpcionesPruebas : InputTestFixture
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const string VariableCaptura = "CAPTURAR_T411";
        private const float HoraNoche = 22f;
        private const float DeltaRaton = 20f;
        private const float Tolerancia = 0.001f;

        private Keyboard teclado;
        private Mouse raton;
        private string carpeta;

        public override void Setup()
        {
            base.Setup();
            teclado = InputSystem.AddDevice<Keyboard>();
            raton = InputSystem.AddDevice<Mouse>();
            DescartarEstadoDeMapas(new InputDevice[] { teclado, raton });
            carpeta = Path.Combine(Path.GetTempPath(), "t411_" + Guid.NewGuid().ToString("N"));
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

        // Cambiar `devices` en un mapa apagado descarta su estado de resolución: la escena los resuelve de nuevo al prenderlos.
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
                UnityEngine.Object.Destroy(raiz);
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

        private IEnumerator Pulsar(ButtonControl tecla)
        {
            Press(tecla);
            yield return null;
            Release(tecla);
            yield return null;
            yield return null;
        }

        private static void Capturar(string nombre, Camera camara)
        {
            if (Environment.GetEnvironmentVariable(VariableCaptura) != "1")
            {
                return;
            }
            CapturaUI.Capturar("docs/capturas/" + nombre + ".png", camara);
            CapturaUI.Capturar2K("docs/capturas/" + nombre + "-1440.png", camara);
        }

        // Mueve el ratón hacia arriba un cuadro y devuelve cuánto cambió la inclinación (positivo: la cámara bajó).
        private IEnumerator MoverRatonArriba(ControladorAPie controlador, Action<float> resultado)
        {
            yield return null;
            float antes = controlador.Inclinacion;
            Set(raton.delta, new Vector2(0f, DeltaRaton));
            yield return null;
            Set(raton.delta, Vector2.zero);
            yield return null;
            resultado(controlador.Inclinacion - antes);
        }

        [UnityTest]
        public IEnumerator RecorridoOpciones()
        {
            yield return EditorSceneManager.LoadSceneInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return EsperarHasta(() =>
            {
                DirectorPandillas d = UnityEngine.Object.FindFirstObjectByType<DirectorPandillas>();
                return d != null && d.Iniciado && Registro<IJugador>.Actual != null && Registro<IOpciones>.Actual is ServicioOpciones
                    && Registro<ISonidos>.Actual != null;
            }, "la ciudad arrancó", 30f);
            yield return new WaitForSecondsRealtime(0.5f);
            IJugador jugador = Registro<IJugador>.Actual;
            ServicioOpciones servicio = (ServicioOpciones)Registro<IOpciones>.Actual;
            PantallaPausa pausa = UnityEngine.Object.FindFirstObjectByType<PantallaPausa>();
            PantallaOpciones opciones = UnityEngine.Object.FindFirstObjectByType<PantallaOpciones>(FindObjectsInactive.Include);
            ControladorAPie controlador = jugador.Raiz.GetComponentInChildren<ControladorAPie>();
            Assert.IsNotNull(pausa, "La ciudad trae la pausa");
            Assert.IsNotNull(opciones, "La ciudad trae las opciones");
            Registro<IRelojMundo>.Actual?.AvanzarHasta(HoraNoche);
            Assert.AreEqual(0.8f, Sonidos.Volumen(SeccionSonido.General), Tolerancia, "Las opciones por defecto se aplican al arrancar");
            Assert.AreEqual(0.8f, Sonidos.Volumen(SeccionSonido.Efectos), Tolerancia);

            // a. Esc → pausa → ↓ hasta OPCIONES → Enter.
            yield return Pulsar(teclado.escapeKey);
            Assert.IsTrue(pausa.Abierta, "Esc pausa");
            Assert.IsTrue(pausa.Opciones.interactable, "OPCIONES se puede elegir");
            for (int i = 0; i < 3; i++)
            {
                yield return Pulsar(teclado.downArrowKey);
            }
            Assert.AreSame(pausa.Opciones, pausa.Menu.Elegido, "↓ ↓ ↓ llega a OPCIONES");
            yield return Pulsar(teclado.enterKey);
            Assert.IsTrue(opciones.Abierta, "Enter abre las opciones");
            Assert.IsTrue(PilaPantallas.EsArriba(opciones), "Las opciones quedan encima de la pausa");
            Assert.AreSame(opciones.Sensibilidad, opciones.Menu.Elegido, "Empieza en SENSIBILIDAD");
            Assert.AreEqual("1.00x", opciones.Sensibilidad.TextoValor.text);

            // b. ↓ ↓ a VOLUMEN GENERAL y cuatro ←: 60 % en el gestor al momento; EFECTOS no cambia.
            yield return Pulsar(teclado.downArrowKey);
            yield return Pulsar(teclado.downArrowKey);
            yield return Pulsar(teclado.downArrowKey);
            Assert.AreSame(opciones.Volumen(SeccionSonido.General), opciones.Menu.Elegido, "↓ ↓ ↓ llega a VOLUMEN GENERAL");
            for (int i = 0; i < 4; i++)
            {
                yield return Pulsar(teclado.leftArrowKey);
            }
            Assert.AreEqual(0.6f, Sonidos.Volumen(SeccionSonido.General), Tolerancia, "Cuatro ← dejan el general en 60 %");
            Assert.AreEqual(0.6f, servicio.Volumen(SeccionSonido.General), Tolerancia);
            Assert.AreEqual(0.8f, Sonidos.Volumen(SeccionSonido.Efectos), Tolerancia, "EFECTOS no cambia");
            Assert.AreEqual("60 %", opciones.Volumen(SeccionSonido.General).TextoValor.text);
            Capturar("T-411-1", jugador.Camara);

            // c. ↑ a INVERTIR Y (va arriba de los volúmenes) y Enter.
            yield return Pulsar(teclado.upArrowKey);
            yield return Pulsar(teclado.upArrowKey);
            Assert.AreSame(opciones.InvertirY, opciones.Menu.Elegido, "↑ llega a INVERTIR Y");
            yield return Pulsar(teclado.enterKey);
            Assert.IsTrue(servicio.InvertirY, "Enter invierte Y");
            Assert.AreEqual("SÍ", opciones.InvertirY.TextoValor.text);

            // d. Esc vuelve a la pausa (con OPCIONES elegido) y Esc continúa.
            yield return Pulsar(teclado.escapeKey);
            Assert.IsFalse(opciones.Abierta, "Esc cierra las opciones");
            Assert.IsTrue(pausa.Abierta, "Y vuelve a la pausa");
            Assert.AreSame(pausa.Opciones, pausa.Menu.Elegido);
            yield return Pulsar(teclado.escapeKey);
            Assert.IsFalse(pausa.Abierta, "Esc continúa");
            Assert.AreEqual(1f, Time.timeScale);
            Assert.IsTrue(controlador.ControlActivo);

            // e. Con Y invertida el ratón hacia arriba baja la cámara; con 2x gira el doble.
            float cambio1 = 0f;
            yield return MoverRatonArriba(controlador, v => cambio1 = v);
            Assert.Greater(cambio1, 0f, "Con INVERTIR Y, el ratón hacia arriba baja la cámara");
            servicio.FijarSensibilidad(2f);
            float cambio2 = 0f;
            yield return MoverRatonArriba(controlador, v => cambio2 = v);
            Assert.AreEqual(cambio1 * 2f, cambio2, Mathf.Abs(cambio1) * 0.05f, "Con 2x el mismo movimiento gira el doble");

            // f. CALIDAD BAJA: la copia de URP pasa a 0.75 y el asset del proyecto no cambia.
            UniversalRenderPipelineAsset proyecto = GraphicsSettings.defaultRenderPipeline as UniversalRenderPipelineAsset;
            Assert.IsNotNull(proyecto, "El proyecto usa URP");
            float escalaProyecto = proyecto.renderScale;
            servicio.FijarCalidad(CalidadGrafica.Baja);
            Assert.IsNotNull(servicio.CopiaRender);
            Assert.AreNotSame(proyecto, servicio.CopiaRender, "La calidad cambia una copia");
            Assert.AreSame(servicio.CopiaRender, QualitySettings.renderPipeline, "La copia es la que dibuja");
            Assert.AreEqual(0.75f, servicio.CopiaRender.renderScale, Tolerancia);
            Assert.AreEqual(40f, servicio.CopiaRender.shadowDistance, Tolerancia);
            Assert.AreEqual(escalaProyecto, proyecto.renderScale, Tolerancia, "El asset de URP del proyecto queda igual");

            // g. Un servicio nuevo con el mismo archivo lee lo mismo.
            GameObject otro = new GameObject("OtroServicioOpciones");
            ServicioOpciones nuevo = otro.AddComponent<ServicioOpciones>();
            Assert.AreEqual(2f, nuevo.Sensibilidad, Tolerancia);
            Assert.IsTrue(nuevo.InvertirY);
            Assert.AreEqual(0.6f, nuevo.Volumen(SeccionSonido.General), Tolerancia);
            Assert.AreEqual(0.8f, nuevo.Volumen(SeccionSonido.Efectos), Tolerancia);
            Assert.AreEqual(CalidadGrafica.Baja, nuevo.Calidad);
            UnityEngine.Object.Destroy(otro);
            yield return null;
        }
    }
}
