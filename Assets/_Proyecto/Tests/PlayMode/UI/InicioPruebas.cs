using System;
using System.Collections;
using System.IO;
using MechaCyberpunk.Core;
using MechaCyberpunk.Progresion.Guardado;
using MechaCyberpunk.Progresion.Opciones;
using MechaCyberpunk.Progresion.Partida;
using MechaCyberpunk.UI.Creditos;
using MechaCyberpunk.UI.Inicio;
using MechaCyberpunk.UI.Kit;
using MechaCyberpunk.UI.Titulo;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.UI
{
    /// <summary>
    /// La pantalla de inicio (Escenas/Inicio.unity) jugada con entrada simulada: fondo 3D vivo, logo, botones con teclado, ratón y
    /// control, opciones que se guardan, créditos, creación de personaje, CONTINUAR con la ranura más reciente, salir y la pantalla de
    /// carga asíncrona. Con CAPTURAR_INICIO=1 guarda las fotos en Logs/capturas-inicio/.
    /// </summary>
    public class InicioPruebas : InputTestFixture
    {
        private const string VariableCaptura = "CAPTURAR_INICIO";
        private const string EscenaPequena = "Assets/_Proyecto/Escenas/Tony/CreacionPersonaje.unity";

        private Keyboard teclado;
        private Mouse raton;
        private string carpeta;

        public override void Setup()
        {
            base.Setup();
            teclado = InputSystem.AddDevice<Keyboard>();
            raton = InputSystem.AddDevice<Mouse>();
            DescartarEstadoDeMapas(new InputDevice[] { teclado, raton });
            carpeta = Path.Combine(Path.GetTempPath(), "inicio_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(carpeta);
            ServicioGuardado.RutaForzada = Path.Combine(carpeta, "partida.json");
            ServicioOpciones.FijarRuta(Path.Combine(carpeta, ServicioOpciones.NombreArchivo));
        }

        public override void TearDown()
        {
            ServicioGuardado.RutaForzada = null;
            ServicioOpciones.FijarRuta(null);
            MenuInicio.AlSalir = null;
            FlujoPartida.Interceptar(null);
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
            if (PantallaCargando.Actual != null)
            {
                UnityEngine.Object.Destroy(PantallaCargando.Actual.gameObject);
            }
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

        private static void Capturar(string nombre)
        {
            if (Environment.GetEnvironmentVariable(VariableCaptura) != "1" || Camera.main == null)
            {
                return;
            }
            CapturaUI.Capturar(Path.GetFullPath("Logs/capturas-inicio/" + nombre + ".png"), Camera.main);
        }

        // Carga Inicio.unity y espera a que el menú esté listo con su primer botón elegido.
        private IEnumerator CargarInicio(Action<MenuInicio> alListo)
        {
            yield return EditorSceneManager.LoadSceneInPlayMode(EscenaInicio.Ruta, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            MenuInicio menu = null;
            yield return EsperarHasta(() =>
            {
                menu = UnityEngine.Object.FindFirstObjectByType<MenuInicio>();
                return menu != null && menu.PrimerBoton != null && menu.PrimerBoton.EstaElegido;
            }, "el menú de inicio con su primer botón elegido", 20f);
            menu.SoloAnotarCarga = true;
            alListo(menu);
        }

        [UnityTest]
        public IEnumerator Arranque_FondoVivoLogoYBotonesSinPartida()
        {
            MenuInicio menu = null;
            yield return CargarInicio(m => menu = m);
            Assert.AreEqual(EstadoInicio.Principal, menu.Estado);
            Assert.IsTrue(menu.Fondo.Construido);
            Assert.Greater(menu.Fondo.Edificios, 30, "La ciudad tiene edificios");
            Assert.Greater(menu.Fondo.Anuncios, 5, "La ciudad tiene anuncios de neón");
            Assert.Greater(menu.Fondo.Vehiculos, 20, "Hay tráfico");
            Assert.IsTrue(RenderSettings.fog, "Hay niebla");
            Assert.IsNotNull(menu.Logo.Principal);
            Assert.AreEqual("CYBERFIX", menu.Logo.Principal.text);
            Assert.IsFalse(menu.HayPartida);
            Assert.IsFalse(menu.Continuar.gameObject.activeSelf, "Sin partida no se ofrece CONTINUAR");
            Assert.AreEqual(MenuInicio.TextoJugar, menu.Nueva.Etiqueta);
            Assert.IsTrue(menu.Nueva.EstaElegido, "El primer botón queda elegido");
            Assert.IsNotNull(EventSystem.current, "Hay EventSystem");
            Assert.IsTrue(Cursor.visible, "El ratón se ve");

            Vector3 antes = menu.Fondo.Camara.transform.position;
            yield return new WaitForSecondsRealtime(1.5f);
            Assert.Greater(Vector3.Distance(antes, menu.Fondo.Camara.transform.position), 0.01f, "La cámara se mueve sola");
            yield return new WaitForSecondsRealtime(0.5f);
            Capturar("01-principal");
        }

        [UnityTest]
        public IEnumerator Teclado_AbajoElegirEnterOpciones_EscVuelve()
        {
            MenuInicio menu = null;
            yield return CargarInicio(m => menu = m);
            yield return Pulsar(teclado.downArrowKey);
            Assert.IsTrue(menu.BotonOpciones.EstaElegido, "Abajo elige OPCIONES");
            Assert.IsFalse(menu.Nueva.EstaElegido, "Solo hay un botón elegido");
            yield return Pulsar(teclado.upArrowKey);
            Assert.IsTrue(menu.Nueva.EstaElegido, "Arriba vuelve a JUGAR");
            yield return Pulsar(teclado.downArrowKey);
            yield return Pulsar(teclado.enterKey);
            Assert.AreEqual(EstadoInicio.Opciones, menu.Estado);
            yield return EsperarHasta(() => menu.Opciones.Pestanas[0].EstaElegido, "la primera pestaña elegida", 3f);
            yield return new WaitForSecondsRealtime(0.6f);
            Capturar("02-opciones-graficos");
            yield return Pulsar(teclado.escapeKey);
            Assert.AreEqual(EstadoInicio.Principal, menu.Estado);
            yield return EsperarHasta(() => menu.BotonOpciones.EstaElegido, "OPCIONES elegido al volver", 3f);
        }

        [UnityTest]
        public IEnumerator Opciones_PestanasYVolumen_SeGuardanEnElServicio()
        {
            MenuInicio menu = null;
            yield return CargarInicio(m => menu = m);
            ServicioOpciones servicio = (ServicioOpciones)Registro<IOpciones>.Actual;
            Assert.IsNotNull(servicio);
            float antes = servicio.Volumen(SeccionSonido.General);
            yield return Pulsar(teclado.downArrowKey);
            yield return Pulsar(teclado.enterKey);
            yield return EsperarHasta(() => menu.Opciones.Pestanas[0].EstaElegido, "la primera pestaña elegida", 3f);

            yield return Pulsar(teclado.eKey);
            Assert.AreEqual(PestanaOpciones.Audio, menu.Opciones.Pestana, "E pasa a AUDIO");
            Assert.IsTrue(menu.Opciones.Volumenes[0].transform.parent.gameObject.activeSelf);
            yield return new WaitForSecondsRealtime(0.6f);
            yield return Pulsar(teclado.downArrowKey);
            Assert.IsTrue(menu.Opciones.Volumenes[0].EstaElegida, "Abajo desde la pestaña elige el primer volumen");
            yield return Pulsar(teclado.rightArrowKey);
            Assert.AreEqual(antes + ReglaOpciones.PasoVolumen, servicio.Volumen(SeccionSonido.General), 1e-4f, "→ sube el volumen general y el servicio lo aplica");
            Assert.AreEqual(ReglaInicio.TextoVolumen(antes + ReglaOpciones.PasoVolumen), menu.Opciones.Volumenes[0].TextoValor);
            Capturar("03-opciones-audio");
            Assert.IsTrue(File.Exists(ServicioOpciones.Ruta), "Se guardó opciones.json");

            yield return Pulsar(teclado.eKey);
            Assert.AreEqual(PestanaOpciones.Controles, menu.Opciones.Pestana);
            yield return new WaitForSecondsRealtime(0.4f);
            yield return Pulsar(teclado.downArrowKey);
            Assert.IsTrue(menu.Opciones.Sensibilidad.EstaElegida, "Elegido: " + EventSystem.current.currentSelectedGameObject);
            float sens = servicio.Sensibilidad;
            yield return Pulsar(teclado.rightArrowKey);
            Assert.AreEqual(sens + ReglaOpciones.PasoSensibilidad, servicio.Sensibilidad, 1e-4f, "→ sube la sensibilidad");
            yield return Pulsar(teclado.downArrowKey);
            Assert.IsTrue(menu.Opciones.InvertirY.EstaElegida);
            Assert.IsFalse(servicio.InvertirY);
            yield return Pulsar(teclado.enterKey);
            Assert.IsTrue(servicio.InvertirY, "Enter en el interruptor lo activa");
            Capturar("04-opciones-controles");

            yield return Pulsar(teclado.qKey);
            yield return Pulsar(teclado.qKey);
            Assert.AreEqual(PestanaOpciones.Graficos, menu.Opciones.Pestana, "Q vuelve a GRÁFICOS");
            yield return Pulsar(teclado.downArrowKey);
            Assert.IsTrue(menu.Opciones.Calidad.EstaElegida);
            int calidad = (int)servicio.Calidad;
            yield return Pulsar(teclado.leftArrowKey);
            Assert.AreEqual(Mathf.Max(0, calidad - 1), (int)servicio.Calidad, "← baja la calidad gráfica");
        }

        [UnityTest]
        public IEnumerator Raton_HoverEligeYClicAbreOpcionesYVolver()
        {
            MenuInicio menu = null;
            yield return CargarInicio(m => menu = m);
            yield return MoverRatonA(menu.BotonOpciones.transform);
            Assert.IsTrue(menu.BotonOpciones.EstaElegido, "El ratón encima elige el botón");
            Assert.IsFalse(menu.Nueva.EstaElegido);
            Press(raton.leftButton);
            yield return null;
            Release(raton.leftButton);
            yield return null;
            yield return null;
            Assert.AreEqual(EstadoInicio.Opciones, menu.Estado, "El clic abre las opciones");
            yield return new WaitForSecondsRealtime(0.6f);
            yield return MoverRatonA(menu.Opciones.Pestanas[1].transform);
            Press(raton.leftButton);
            yield return null;
            Release(raton.leftButton);
            yield return null;
            Assert.AreEqual(PestanaOpciones.Audio, menu.Opciones.Pestana, "Clic en la pestaña AUDIO");
            yield return MoverRatonA(menu.Opciones.Volver.transform);
            Press(raton.leftButton);
            yield return null;
            Release(raton.leftButton);
            yield return null;
            yield return null;
            Assert.AreEqual(EstadoInicio.Principal, menu.Estado, "VOLVER regresa al menú");
        }

        private IEnumerator MoverRatonA(Transform objetivo)
        {
            Vector2 pantalla = RectTransformUtility.WorldToScreenPoint(null, objetivo.position);
            Set(raton.position, pantalla);
            yield return null;
            yield return null;
        }

        [UnityTest]
        public IEnumerator Control_CruzetaYBotonA_NavegaYAcepta()
        {
            Gamepad control = InputSystem.AddDevice<Gamepad>();
            MenuInicio menu = null;
            yield return CargarInicio(m => menu = m);
            Press(control.dpad.down);
            yield return null;
            Release(control.dpad.down);
            yield return null;
            yield return null;
            Assert.IsTrue(menu.BotonOpciones.EstaElegido, "La cruceta baja a OPCIONES");
            Press(control.buttonSouth);
            yield return null;
            Release(control.buttonSouth);
            yield return null;
            yield return null;
            Assert.AreEqual(EstadoInicio.Opciones, menu.Estado, "A acepta");
            yield return new WaitForSecondsRealtime(0.5f);
            Press(control.buttonEast);
            yield return null;
            Release(control.buttonEast);
            yield return null;
            Assert.AreEqual(EstadoInicio.Principal, menu.Estado, "B regresa");
            Assert.AreEqual(MenuInicio.AyudaControl, menu.Ayuda.text, "La ayuda cambia a los botones del control");
        }

        [UnityTest]
        public IEnumerator Creditos_EnterAbreYEscRegresaConCreditosElegido()
        {
            MenuInicio menu = null;
            yield return CargarInicio(m => menu = m);
            Assert.IsTrue(PantallaTitulo.HayEnlaceCreditos, "Los créditos están enlazados");
            yield return Pulsar(teclado.downArrowKey);
            yield return Pulsar(teclado.downArrowKey);
            Assert.IsTrue(menu.Creditos.EstaElegido);
            yield return Pulsar(teclado.enterKey);
            Assert.AreEqual(EstadoInicio.Creditos, menu.Estado);
            PantallaCreditos pantalla = null;
            yield return EsperarHasta(() =>
            {
                pantalla = UnityEngine.Object.FindFirstObjectByType<PantallaCreditos>();
                return pantalla != null && pantalla.Abierta;
            }, "la pantalla de créditos", 5f);
            yield return new WaitForSecondsRealtime(1.0f);
            Capturar("05-creditos");
            yield return Pulsar(teclado.escapeKey);
            Assert.AreEqual(EstadoInicio.Principal, menu.Estado);
            yield return EsperarHasta(() => menu.Creditos.EstaElegido, "CRÉDITOS elegido al volver", 3f);
        }

        [UnityTest]
        public IEnumerator Salir_AbajoAbajoAbajoEnter_PideSalir()
        {
            string pedido = null;
            MenuInicio.AlSalir = motivo => pedido = motivo;
            MenuInicio menu = null;
            yield return CargarInicio(m => menu = m);
            for (int i = 0; i < 3; i++)
            {
                yield return Pulsar(teclado.downArrowKey);
            }
            Assert.IsTrue(menu.Salir.EstaElegido);
            yield return Pulsar(teclado.enterKey);
            Assert.AreEqual("salir", pedido, "SALIR cierra el juego");
        }

        [UnityTest]
        public IEnumerator NuevaPartida_AbreCreacionEmpezarPideCargarCiudad_EscRegresa()
        {
            MenuInicio menu = null;
            yield return CargarInicio(m => menu = m);
            yield return Pulsar(teclado.enterKey);
            Assert.AreEqual(EstadoInicio.Creacion, menu.Estado, "JUGAR abre la creación de personaje");
            Assert.IsTrue(menu.Creacion.Abierta);
            yield return new WaitForSecondsRealtime(0.6f);
            Capturar("06-creacion");
            yield return Pulsar(teclado.escapeKey);
            Assert.AreEqual(EstadoInicio.Principal, menu.Estado, "Esc regresa al menú");
            Assert.IsFalse(menu.Creacion.Abierta);
            yield return EsperarHasta(() => menu.Nueva.EstaElegido, "JUGAR elegido al volver", 3f);

            yield return Pulsar(teclado.enterKey);
            Assert.IsTrue(menu.Creacion.Abierta);
            menu.Creacion.Empezar.Pulsar();
            yield return null;
            Assert.AreEqual(FlujoPartida.RutaEscena, menu.RutaPedida, "EMPEZAR pide cargar la ciudad");
            Assert.AreEqual(AccionPartida.Nueva, FlujoPartida.Pendiente, "Como partida nueva");
            Assert.IsTrue(PantallaCargando.Visible, "Sale la pantalla de carga");
        }

        [UnityTest]
        public IEnumerator Continuar_ConPartidaGuardada_OfreceYPideCargarLaRanura()
        {
            yield return EditorSceneManager.LoadSceneInPlayMode(EscenaInicio.Ruta, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            ServicioGuardado servicio = null;
            yield return EsperarHasta(() =>
            {
                servicio = ServicioGuardado.Actual;
                return servicio != null && UnityEngine.Object.FindFirstObjectByType<MenuInicio>() != null;
            }, "los servicios de la escena", 20f);
            Assert.IsTrue(servicio.Guardar(ServicioGuardado.PrimeraRanura), "Se pudo guardar la ranura 1");
            MenuInicio menu = UnityEngine.Object.FindFirstObjectByType<MenuInicio>();
            menu.SoloAnotarCarga = true;
            menu.RefrescarPartida();
            Assert.IsTrue(menu.HayPartida);
            Assert.AreEqual(ServicioGuardado.PrimeraRanura, menu.RanuraContinuar);
            Assert.IsTrue(menu.Continuar.gameObject.activeSelf, "Con partida se ofrece CONTINUAR");
            Assert.AreEqual(MenuInicio.TextoNueva, menu.Nueva.Etiqueta, "y NUEVA PARTIDA");
            Assert.IsFalse(string.IsNullOrEmpty(menu.Continuar.Subtexto.text), "CONTINUAR muestra el resumen");
            yield return EsperarHasta(() => menu.PrimerBoton.EstaElegido, "el primer botón elegido", 5f);
            Assert.AreSame(menu.Continuar, menu.PrimerBoton);
            Capturar("07-principal-con-partida");
            yield return Pulsar(teclado.enterKey);
            Assert.AreEqual(FlujoPartida.RutaEscena, menu.RutaPedida, "CONTINUAR pide cargar la ciudad");
            Assert.AreEqual(AccionPartida.Cargar, FlujoPartida.Pendiente);
            Assert.AreEqual(ServicioGuardado.PrimeraRanura, FlujoPartida.RanuraPendiente, "con la ranura más reciente");
            Assert.AreEqual(EstadoInicio.Cargando, menu.Estado);
            Assert.IsTrue(PantallaCargando.Visible);
        }

        [UnityTest]
        public IEnumerator Cargando_Asincrono_BarraSubeYLaEscenaSeActivaAlFinal()
        {
            yield return EditorSceneManager.LoadSceneInPlayMode(EscenaInicio.Ruta, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            PantallaCargando carga = PantallaCargando.Mostrar();
            AsyncOperation operacion = EscenaInicio.CargarAsincrono(EscenaPequena);
            Assert.IsNotNull(operacion);
            Assert.IsFalse(operacion.allowSceneActivation, "Se carga sin activar");
            carga.Asignar(operacion);
            yield return new WaitForSecondsRealtime(1.0f);
            Assert.IsFalse(carga.EscenaActivada, "Antes del tiempo mínimo no se activa la escena");
            Assert.Greater(carga.Progreso, 0f, "La barra avanza");
            Assert.IsNotEmpty(carga.ConsejoActual, "Hay un consejo en pantalla");
            Assert.AreEqual("Inicio", SceneManager.GetActiveScene().name, "Sigue la escena de inicio");
            Capturar("08-carga");
            float anterior = carga.Progreso;
            yield return new WaitForSecondsRealtime(0.4f);
            Assert.GreaterOrEqual(carga.Progreso, anterior, "La barra no retrocede");
            yield return EsperarHasta(() => carga.EscenaActivada, "la activación de la escena", 20f);
            Assert.GreaterOrEqual(carga.Progreso, 0.99f, "La escena se activa con la barra llena");
            Assert.GreaterOrEqual(carga.Segundos, PantallaCargando.SegundosMinimos, "Respeta el tiempo mínimo");
            yield return EsperarHasta(() => operacion.isDone, "que termine la carga", 20f);
            Assert.AreEqual("CreacionPersonaje", SceneManager.GetActiveScene().name, "La escena nueva quedó activa");
        }

        [UnityTest]
        public IEnumerator Cargando_SinOperacion_AvanzaSolaYRotaConsejos()
        {
            yield return EditorSceneManager.LoadSceneInPlayMode(EscenaInicio.Ruta, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            PantallaCargando carga = PantallaCargando.Mostrar();
            Assert.IsFalse(carga.ActivoAsincrono);
            string primero = carga.ConsejoActual;
            yield return new WaitForSecondsRealtime(1.2f);
            Assert.Greater(carga.Progreso, 0.02f, "La barra avanza sin operación");
            Assert.Less(carga.Progreso, 0.91f, "y no pasa del 90 % sin jugador");
            Assert.AreEqual(ReglaInicio.TextoPorcentaje(carga.Progreso), carga.TextoPorcentaje);
            yield return new WaitForSecondsRealtime(PantallaCargando.SegundosPorConsejo);
            Assert.AreNotEqual(primero, carga.ConsejoActual, "El consejo cambia");
        }
    }
}
