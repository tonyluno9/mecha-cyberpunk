using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using MechaCyberpunk.Core;
using MechaCyberpunk.Progresion.Guardado;
using MechaCyberpunk.Progresion.Opciones;
using MechaCyberpunk.Progresion.Partida;
using MechaCyberpunk.UI.Historia;
using MechaCyberpunk.UI.Inicio;
using MechaCyberpunk.UI.Intro;
using MechaCyberpunk.UI.Titulo;
using MechaCyberpunk.VR;
using MechaCyberpunk.VR.Menus;
using NUnit.Framework;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Tests.VR
{
    /// <summary>
    /// V7: el menú de Inicio (Escenas/Inicio.unity) en VR con el simulador de XR. Sin jugador a pie, MenusVR arma su rig de menús
    /// (cabeza y control derecho). VR15: todo Inicio (logo, menú sobre las calles, carga) va en UNA pantalla grande curva (~105° a 3.5 m)
    /// compuesta como en PC (los Canvas en Screen Space Camera hacia su textura, ninguno como panel aparte); NUEVA PARTIDA, CONTINUAR, OPCIONES y SALIR
    /// se eligen con el láser del control derecho y el gatillo. Al cargar la partida se pasa del rig de menús al del jugador
    /// (AdaptadorVR) sin quedarse en negro ni duplicar cámaras, oídos ni XR Origin; la carga sigue en la pantalla grande hasta que se quita.
    /// La carga usa Pruebas_APie (jugador a pie y piso) en vez de la ciudad entera. Partidas y opciones van a una carpeta temporal:
    /// partida_auto.json y las ranuras de Tony no se tocan.
    /// </summary>
    public class InicioVRPruebas : InputTestFixture
    {
        private const string RutaAPie = "Assets/_Proyecto/Escenas/Tony/Pruebas_APie.unity";
        private static readonly Vector3 CabezaCuarto = new Vector3(0f, 1.6f, 0f);
        private static readonly Vector3 ManoDerechaCuarto = new Vector3(0.2f, 1.2f, 0.3f);
        private static readonly Vector3 ManoIzquierdaCuarto = new Vector3(-0.2f, 1.2f, 0.3f);
        private const float SegundosEntradaMenu = 1.2f;

        private XRSimulatedHMD visor;
        private XRSimulatedController controlIzq;
        private XRSimulatedController controlDer;
        private Keyboard teclado;
        private Mouse raton;
        private string carpeta;
        private SistemaVR sistemaPropio;
        private readonly List<GameObject> extras = new List<GameObject>();
        private int clics;

        public override void Setup()
        {
            base.Setup();
            Pausa.Fijar(false);
            Time.timeScale = 1f;
            SistemaVR.NoIntentarXR = true;
            visor = InputSystem.AddDevice<XRSimulatedHMD>();
            controlIzq = InputSystem.AddDevice<XRSimulatedController>();
            controlDer = InputSystem.AddDevice<XRSimulatedController>();
            InputSystem.SetDeviceUsage(controlIzq, UnityEngine.InputSystem.CommonUsages.LeftHand);
            InputSystem.SetDeviceUsage(controlDer, UnityEngine.InputSystem.CommonUsages.RightHand);
            teclado = InputSystem.AddDevice<Keyboard>();
            raton = InputSystem.AddDevice<Mouse>();
            DescartarEstadoDeMapas(new InputDevice[] { teclado, raton });

            // La partida de Tony no se toca: guardado y opciones en una carpeta temporal.
            carpeta = Path.Combine(Path.GetTempPath(), "iniciovr_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(carpeta);
            ServicioGuardado.RutaForzada = Path.Combine(carpeta, "partida.json");
            ServicioOpciones.FijarRuta(Path.Combine(carpeta, ServicioOpciones.NombreArchivo));

            // SistemaVR pone el rig al jugador a pie de la escena nueva. Si no vive (lo crea el arranque del juego), se crea aquí.
            if (SistemaVR.Instancia == null)
            {
                var objeto = new GameObject("SistemaVR_Prueba");
                Object.DontDestroyOnLoad(objeto);
                sistemaPropio = objeto.AddComponent<SistemaVR>();
            }
            ModoVR.Fijar(true, true);
            EntradaVR.Recrear();
            PonerCabeza(CabezaCuarto, Quaternion.identity);
            PonerMano(false, ManoIzquierdaCuarto, Quaternion.identity);
            PonerMano(true, ManoDerechaCuarto, Quaternion.identity);
        }

        public override void TearDown()
        {
            foreach (GameObject g in extras)
            {
                if (g != null)
                {
                    Object.Destroy(g);
                }
            }
            extras.Clear();
            MenuInicio.AlSalir = null;
            IntroInicio.ReproducirEnEditor = false;
            FlujoPartida.Interceptar(null);
            // Lo pendiente (NUEVA PARTIDA, CONTINUAR) no debe llegar al ServicioGuardado de otra prueba.
            FlujoPartida.ConsumirPendiente(out _, out _);
            EntradaVR.Apagar();
            ModoVR.Fijar(false);
            SistemaVR.NoIntentarXR = false;
            if (sistemaPropio != null)
            {
                Object.Destroy(sistemaPropio.gameObject);
                sistemaPropio = null;
            }
            ServicioGuardado.RutaForzada = null;
            ServicioOpciones.FijarRuta(null);
            Registros.LimpiarTodos();
            EventosLugares.LimpiarSuscriptores();
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
            foreach (PantallaCargando c in Object.FindObjectsByType<PantallaCargando>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                Object.Destroy(c.gameObject);
            }
            foreach (GameObject raiz in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                Object.Destroy(raiz);
            }
            Registros.LimpiarTodos();
            yield return null;
        }

        // ---- Entrada simulada ----

        private void PonerCabeza(Vector3 posicion, Quaternion rotacion)
        {
            Set(visor.centerEyePosition, posicion);
            Set(visor.centerEyeRotation, rotacion);
            Set(visor.devicePosition, posicion);
            Set(visor.deviceRotation, rotacion);
        }

        private void PonerMano(bool derecha, Vector3 posicion, Quaternion rotacion)
        {
            XRSimulatedController c = derecha ? controlDer : controlIzq;
            Set(c.devicePosition, posicion);
            Set(c.deviceRotation, rotacion);
        }

        private IEnumerator Pulsar(ButtonControl boton)
        {
            Press(boton);
            yield return null;
            yield return null;
            Release(boton);
            yield return null;
        }

        private static IEnumerator Cuadros(int n)
        {
            for (int i = 0; i < n; i++)
            {
                yield return null;
            }
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

        private static Vector3 CentroDe(Component c)
        {
            var rt = (RectTransform)c.transform;
            return rt.TransformPoint(rt.rect.center);
        }

        // VR17: el láser sale de ManoVR.Apuntado (hijo de la mano); el giro del control se pone en el espacio del cuarto (el padre de la mano).
        private static Quaternion CuartoDe(Transform laser)
        {
            Transform cuarto = laser.parent;
            if (cuarto != null && cuarto.GetComponent<ManoVR>() != null)
            {
                cuarto = cuarto.parent;
            }
            return cuarto != null ? cuarto.rotation : Quaternion.identity;
        }

        // Gira el control derecho (en su lugar de siempre) para que el láser apunte a un punto del mundo.
        private IEnumerator ApuntarA(Vector3 punto)
        {
            PonerMano(true, ManoDerechaCuarto, Quaternion.identity);
            yield return Cuadros(2);
            Transform mano = MenusVR.Actual.ManoLaser;
            Assert.IsNotNull(mano, "Hay un control del que sale el láser.");
            Quaternion mundo = Quaternion.LookRotation(punto - mano.position);
            Quaternion padre = CuartoDe(mano);
            PonerMano(true, ManoDerechaCuarto, Quaternion.Inverse(padre) * mundo);
            yield return Cuadros(4);
        }

        // VR15: a dónde apuntar para tocar un elemento: si su Canvas va en la pantalla grande, al punto de la curva donde se ve; si no, a su centro.
        private static Vector3 PuntoDe(Component elemento)
        {
            InicioCineVR inicio = InicioCineVR.Actual;
            Canvas lienzo = elemento.GetComponentInParent<Canvas>();
            Canvas raiz = lienzo != null ? lienzo.rootCanvas : null;
            if (inicio != null && inicio.EnPantalla(raiz) && inicio.PuntoEnPantalla((RectTransform)elemento.transform, out Vector3 punto))
            {
                return punto;
            }
            return CentroDe(elemento);
        }

        private IEnumerator PulsarConLaser(Component boton, string nombre)
        {
            yield return ApuntarA(PuntoDe(boton));
            Assert.IsTrue(MenusVR.UIApuntada, "El láser apunta a " + nombre + ".");
            Assert.IsTrue(MenusVR.Actual.LaserVisible, "El láser se ve apuntando a " + nombre + ".");
            yield return Pulsar(controlDer.triggerButton);
            yield return Cuadros(2);
        }

        // Carga Inicio.unity y espera al menú con su primer botón elegido, al rig de menús armado y al menú pasado a panel.
        private IEnumerator CargarInicioVR(Action<MenuInicio> alListo)
        {
            yield return EditorSceneManager.LoadSceneInPlayMode(EscenaInicio.Ruta, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            MenuInicio menu = null;
            yield return EsperarHasta(() =>
            {
                menu = Object.FindFirstObjectByType<MenuInicio>();
                return menu != null && menu.PrimerBoton != null && menu.PrimerBoton.EstaElegido;
            }, "el menú de inicio con su primer botón elegido", 20f);
            menu.SoloAnotarCarga = true;
            yield return EsperarHasta(() => MenusVR.Actual != null && MenusVR.Actual.RigMenusArmado
                && InicioCineVR.Actual != null && InicioCineVR.Actual.EnPantalla(menu.Canvas), "el rig de menús y el menú en la pantalla grande", 5f);
            // Las animaciones de entrada de los paneles y botones.
            yield return new WaitForSecondsRealtime(SegundosEntradaMenu);
            alListo(menu);
        }

        // VR15: el Canvas se dibuja en la pantalla grande (Screen Space Camera sobre la cámara de la UI, que dibuja en la textura de
        // encima), no como panel en el mundo.
        private static void ComprobarEnPantallaGrande(Canvas canvas, string que)
        {
            InicioCineVR inicio = InicioCineVR.Actual;
            Assert.IsNotNull(inicio, "Hay pantalla grande de Inicio.");
            Assert.IsTrue(inicio.Activa, "La pantalla grande de Inicio está activa.");
            Assert.AreEqual(RenderMode.ScreenSpaceCamera, canvas.renderMode, que + " va en Screen Space Camera (compuesto como en PC).");
            Assert.AreSame(inicio.CamaraUI, canvas.worldCamera, que + " lo dibuja la cámara de la UI de la pantalla grande.");
            Assert.IsTrue(inicio.EnPantalla(canvas), que + " va en la pantalla grande.");
            Assert.IsNull(MenusVR.Actual.TipoDe(canvas), que + " no va como panel en el mundo.");
            Assert.AreSame(inicio.TexturaUI, inicio.CamaraUI.targetTexture, "La cámara de la UI dibuja en la textura de encima.");
            Assert.IsTrue(PantallaCineVR.Visible, "La pantalla grande se ve.");
            Assert.AreSame(inicio.TexturaUI, PantallaCineVR.TexturaEncima, "La pantalla grande compone la UI encima del fondo.");
        }

        // VR15: una sola pantalla: ningún Canvas raíz va como panel en el mundo (World Space frente a la cabeza, fundido pegado o casco),
        // y todos los de pantalla van en la pantalla grande.
        private static void ComprobarUnaSolaPantalla(string que)
        {
            int enPantalla = 0;
            foreach (Canvas c in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (c == null || !c.isActiveAndEnabled || !c.isRootCanvas)
                {
                    continue;
                }
                Assert.IsNull(MenusVR.Actual.TipoDe(c), que + ": " + c.name + " no debe ir como panel aparte.");
                if (c.renderMode != RenderMode.WorldSpace)
                {
                    Assert.IsTrue(InicioCineVR.Actual.EnPantalla(c), que + ": " + c.name + " va en la pantalla grande.");
                    enPantalla++;
                }
            }
            Assert.Greater(enPantalla, 0, que + ": hay Canvas en la pantalla grande.");
        }

        // VR15: la pantalla grande es curva, ~105° de ancho, a ~3.5 m de la cabeza (en todo su ancho), alto según 16:9 del arco,
        // centrada en los ojos y enfrente.
        private static void ComprobarPantallaGrande(Transform cabeza)
        {
            Assert.IsTrue(PantallaCineVR.Visible, "La pantalla grande se ve.");
            Vector3 izquierda = PantallaCineVR.PuntoEnUV(new Vector2(0f, 0.5f)) - cabeza.position;
            Vector3 derecha = PantallaCineVR.PuntoEnUV(new Vector2(1f, 0.5f)) - cabeza.position;
            Vector3 centro = PantallaCineVR.Centro - cabeza.position;
            float alto = Vector3.Distance(PantallaCineVR.PuntoEnUV(new Vector2(0.5f, 1f)), PantallaCineVR.PuntoEnUV(new Vector2(0.5f, 0f)));
            float grados = Vector3.Angle(new Vector3(izquierda.x, 0f, izquierda.z), new Vector3(derecha.x, 0f, derecha.z));
            Debug.Log("[VR] Pantalla grande: " + grados.ToString("0.0") + "° de ancho, a " + centro.magnitude.ToString("0.00") + " m, "
                + PantallaCineVR.AnchoMundo.ToString("0.00") + " x " + alto.ToString("0.00") + " m.");
            Assert.AreEqual(PantallaCineVR.Grados, grados, 3f, "La pantalla mide ~105° de ancho.");
            Assert.Greater(grados, 100f, "Llena casi toda la vista.");
            Assert.AreEqual(PantallaCineVR.Radio, new Vector2(centro.x, centro.z).magnitude, 0.2f, "El centro a ~3.5 m.");
            Assert.AreEqual(PantallaCineVR.Radio, new Vector2(izquierda.x, izquierda.z).magnitude, 0.2f, "El borde izquierdo también a ~3.5 m (curva).");
            Assert.AreEqual(PantallaCineVR.Radio, new Vector2(derecha.x, derecha.z).magnitude, 0.2f, "El borde derecho también a ~3.5 m (curva).");
            Assert.AreEqual(16f / 9f, PantallaCineVR.AnchoMundo / alto, 0.05f, "Alto según 16:9 del arco.");
            Assert.AreEqual(0f, centro.y, 0.2f, "Centrada a la altura de los ojos.");
            Vector3 frente = ReglaLienzosVR.FrentePlano(cabeza.forward, cabeza.up);
            Assert.Greater(Vector3.Dot(frente, new Vector3(centro.x, 0f, centro.z).normalized), 0.95f, "Enfrente de la cabeza.");
        }

        // V12: una cámara con RecorridoCallesMenu como la de Menu_Calles (que en batch no se carga), avanzando por una calle de prueba.
        private RecorridoCallesMenu CrearRecorrido()
        {
            var objeto = new GameObject("RecorridoPruebaVR");
            objeto.SetActive(false);
            extras.Add(objeto);
            var objetoCamara = new GameObject("CamaraRecorridoPruebaVR");
            objetoCamara.transform.SetParent(objeto.transform, false);
            Camera camara = objetoCamara.AddComponent<Camera>();
            Transform desde = new GameObject("Desde").transform;
            desde.SetParent(objeto.transform, false);
            desde.position = new Vector3(0f, 3f, 0f);
            Transform hacia = new GameObject("Hacia").transform;
            hacia.SetParent(objeto.transform, false);
            hacia.position = new Vector3(0f, 3f, 40f);
            RecorridoCallesMenu recorrido = objeto.AddComponent<RecorridoCallesMenu>();
            recorrido.Configurar(camara, new[] { desde }, new[] { hacia });
            objeto.SetActive(true);
            return recorrido;
        }

        private static int Activos<T>() where T : Behaviour
        {
            int n = 0;
            foreach (T c in Object.FindObjectsByType<T>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (c.isActiveAndEnabled)
                {
                    n++;
                }
            }
            return n;
        }

        // ---- Pruebas ----

        /// <summary>T96 en VR: HAS MUERTO → MENÚ PRINCIPAL saca la carga y, al llegar al menú, la carga se quita sola (sin quedarse encima).</summary>
        [UnityTest]
        [Timeout(90000)]
        public IEnumerator T96_MuerteAlMenu_EnVR_LaCargaSeQuitaAlLlegarAlMenu()
        {
            FlujoPartida.Interceptar(_ => EditorSceneManager.LoadSceneAsyncInPlayMode(EscenaInicio.Ruta, new LoadSceneParameters(LoadSceneMode.Single)));
            PantallaCargando.Mostrar();
            MechaCyberpunk.Progresion.Recorrido.DirectorModoLibre.IrAlMenu();
            Assert.IsTrue(PantallaCargando.Visible, "Sale la carga al elegir MENÚ PRINCIPAL.");
            yield return EsperarHasta(() => Object.FindFirstObjectByType<MenuInicio>() != null, "el menú principal", 30f);
            yield return EsperarHasta(() => !PantallaCargando.Visible, "que la carga se quite al llegar al menú", 15f);
            Assert.IsNotNull(MenusVR.Actual, "En VR el menú se usa con el rig de menús.");
        }

        [UnityTest]
        [Timeout(180000)]
        public IEnumerator Inicio_MenuEnPanelFrenteALaCabeza_BotonesConLaserYGatillo()
        {
            string pedido = null;
            MenuInicio.AlSalir = motivo => pedido = motivo;
            MenuInicio menu = null;
            yield return CargarInicioVR(m => menu = m);
            MenusVR vr = MenusVR.Actual;

            // El rig de menús: su cámara es la del visor y es la cabeza; un solo XR Origin y un solo oído.
            Camera cabezaRig = vr.CamaraRigMenus;
            Assert.IsNotNull(cabezaRig);
            Assert.AreSame(cabezaRig.transform, vr.Cabeza, "En Inicio la cabeza es la del rig de menús.");
            Assert.AreSame(cabezaRig, SistemaVR.CamaraVR, "La cámara del rig de menús va al visor.");
            Assert.IsTrue(SistemaVR.VaAlVisor(cabezaRig), "La cabeza del rig de menús se dibuja en el visor (allowXRRendering).");
            Assert.IsTrue(cabezaRig.isActiveAndEnabled);
            Assert.IsFalse(cabezaRig.CompareTag("MainCamera"), "El rig de menús no le quita Camera.main a la escena.");
            Assert.AreEqual(1, Activos<XROrigin>(), "Un solo XR Origin (el del rig de menús).");
            Assert.AreEqual(1, Activos<AudioListener>(), "Un solo AudioListener.");
            foreach (Camera c in Camera.allCameras)
            {
                if (c != cabezaRig && c.targetTexture == null)
                {
                    Assert.IsFalse(SistemaVR.VaAlVisor(c), c.name + " no debe verse en el visor (solo la cabeza).");
                }
            }
            Assert.IsTrue(MenusVR.MenuAbierto, "Sin jugador, el panel de Inicio cuenta como menú abierto (quieto, láser visible).");
            // VR15: una sola pantalla grande curva; el menú va en ella (compuesto sobre las calles como en PC), no como panel aparte.
            ComprobarEnPantallaGrande(menu.Canvas, "El menú de inicio");
            ComprobarUnaSolaPantalla("Inicio");
            ComprobarPantallaGrande(vr.Cabeza);

            // Una partida guardada (en la carpeta temporal) para que salga CONTINUAR.
            ServicioGuardado servicio = ServicioGuardado.Actual;
            Assert.IsNotNull(servicio, "Inicio trae el servicio de guardado.");
            StringAssert.StartsWith(carpeta, servicio.RutaRanura(ServicioGuardado.PrimeraRanura), "Las ranuras van a la carpeta temporal.");
            Assert.IsTrue(servicio.Guardar(ServicioGuardado.PrimeraRanura), "Se pudo guardar la ranura 1 (temporal).");
            menu.RefrescarPartida();
            Assert.IsTrue(menu.Continuar.gameObject.activeSelf, "Con partida se ofrece CONTINUAR.");
            yield return new WaitForSecondsRealtime(0.5f);

            // OPCIONES con el láser y el gatillo, y VOLVER.
            yield return PulsarConLaser(menu.BotonOpciones, "OPCIONES");
            Assert.AreEqual(EstadoInicio.Opciones, menu.Estado, "El gatillo sobre OPCIONES abre las opciones.");
            yield return new WaitForSecondsRealtime(0.8f);
            yield return PulsarConLaser(menu.Opciones.Volver, "VOLVER");
            Assert.AreEqual(EstadoInicio.Principal, menu.Estado, "VOLVER con el láser regresa al menú.");
            yield return new WaitForSecondsRealtime(0.8f);

            // SALIR: se pide salir (la prueba lo intercepta, el editor no se cierra).
            yield return PulsarConLaser(menu.Salir, "SALIR");
            Assert.AreEqual("salir", pedido, "SALIR con el láser pide cerrar el juego.");
            Assert.AreEqual(EstadoInicio.Principal, menu.Estado);

            // Apuntando fuera de los botones el gatillo no elige nada.
            // VR15: arriba del borde de arriba de la pantalla grande (no toca nada).
            yield return ApuntarA(PantallaCineVR.Centro + Vector3.up * (PantallaCineVR.AltoMundo * 0.5f + 2f));
            Assert.IsFalse(MenusVR.UIApuntada, "Fuera de la pantalla no apunta a la UI.");
            yield return Pulsar(controlDer.triggerButton);
            yield return Cuadros(2);
            Assert.AreEqual(EstadoInicio.Principal, menu.Estado, "Fuera de los botones el gatillo no hace nada.");

            // CONTINUAR: pide cargar la ciudad con la ranura más reciente y sale la pantalla de carga.
            yield return PulsarConLaser(menu.Continuar, "CONTINUAR");
            Assert.AreEqual(EstadoInicio.Cargando, menu.Estado, "CONTINUAR con el láser va a la carga.");
            Assert.AreEqual(FlujoPartida.RutaEscena, menu.RutaPedida, "CONTINUAR pide cargar la ciudad.");
            Assert.AreEqual(AccionPartida.Cargar, FlujoPartida.Pendiente);
            Assert.AreEqual(ServicioGuardado.PrimeraRanura, FlujoPartida.RanuraPendiente, "con la ranura más reciente.");
            Assert.IsTrue(PantallaCargando.Visible, "Sale la pantalla de carga.");

            // VR15: la pantalla de carga va en la misma pantalla grande (no Overlay, que en el visor no se ve, ni panel aparte).
            Canvas carga = PantallaCargando.Actual.GetComponentInChildren<Canvas>(true);
            Assert.IsNotNull(carga);
            yield return EsperarHasta(() => InicioCineVR.Actual.EnPantalla(carga), "la carga en la pantalla grande", 2f);
            yield return Cuadros(3);
            ComprobarEnPantallaGrande(carga, "La pantalla de carga");
            ComprobarUnaSolaPantalla("La carga");

            // T100: en la pantalla de cine la carga trae su frame de cinemática, encendido y cubriendo el lienzo.
            FotosCarga fotos = carga.GetComponentInChildren<FotosCarga>(true);
            Assert.IsNotNull(fotos, "La carga en VR lleva el frame de cinemática.");
            Assert.IsNotNull(fotos.FotoActual);
            StringAssert.StartsWith("Carga_C", fotos.FotoActual.name);
            RawImage imagen = fotos.GetComponentInChildren<RawImage>(true);
            Assert.IsTrue(imagen.isActiveAndEnabled && imagen.color.a > 0.99f, "El frame se ve.");
            Rect lienzo = ((RectTransform)fotos.transform).rect;
            Assert.GreaterOrEqual(fotos.TamanoFoto.x, lienzo.width - 0.5f, "Cubre el ancho de la pantalla de cine.");
            Assert.GreaterOrEqual(fotos.TamanoFoto.y, lienzo.height - 0.5f, "Cubre el alto de la pantalla de cine.");
        }

        [UnityTest]
        [Timeout(240000)]
        public IEnumerator NuevaPartida_ConLaser_CargaYPasaDelRigDeMenusAlDelJugador()
        {
            MenuInicio menu = null;
            yield return CargarInicioVR(m => menu = m);
            MenusVR vr = MenusVR.Actual;
            GameObject raizRig = vr.RaizRigMenus;
            Assert.IsNotNull(raizRig);

            yield return PulsarConLaser(menu.Nueva, "NUEVA PARTIDA");
            Assert.AreEqual(EstadoInicio.Cargando, menu.Estado, "NUEVA PARTIDA con el láser va a la carga.");
            Assert.AreEqual(FlujoPartida.RutaEscena, menu.RutaPedida, "pide cargar la ciudad");
            Assert.AreEqual(AccionPartida.Nueva, FlujoPartida.Pendiente, "como partida nueva");
            Assert.IsTrue(PantallaCargando.Visible, "sale la pantalla de carga");

            // La carga asíncrona real de la pantalla de carga, con una escena mínima con jugador a pie (no la ciudad entera).
            PantallaCargando pantalla = PantallaCargando.Actual;
            AsyncOperation operacion = EscenaInicio.CargarAsincrono(RutaAPie);
            Assert.IsNotNull(operacion);
            pantalla.Asignar(operacion);
            Canvas carga = pantalla.GetComponentInChildren<Canvas>(true);
            yield return new WaitForSecondsRealtime(0.6f);
            Assert.AreEqual("Inicio", SceneManager.GetActiveScene().name, "Mientras carga sigue Inicio.");
            Assert.IsTrue(vr.RigMenusArmado, "Mientras carga manda el rig de menús.");
            ComprobarEnPantallaGrande(carga, "La pantalla de carga");
            ComprobarUnaSolaPantalla("La carga");

            yield return EsperarHasta(() => operacion.isDone, "que termine la carga de la escena", 60f);
            Assert.AreEqual("Pruebas_APie", SceneManager.GetActiveScene().name, "La escena nueva quedó activa.");
            yield return EsperarHasta(() => AdaptadorVR.Actual != null && AdaptadorVR.Actual.Armado, "el rig VR del jugador de la escena nueva", 10f);
            AdaptadorVR a = AdaptadorVR.Actual;
            yield return Cuadros(3);

            // El rig de menús se fue y manda el del jugador.
            Assert.IsFalse(vr.RigMenusArmado, "El rig de menús se quita al armarse el del jugador.");
            Assert.IsTrue(raizRig == null || !raizRig.activeInHierarchy, "El rig de menús quedó destruido o apagado.");
            Assert.AreSame(a.Camara.transform, vr.Cabeza, "La cabeza es la cámara del jugador.");
            Assert.AreSame(a.ManoDerecha.Apuntado, vr.ManoLaser, "El láser sale del control derecho del jugador (su pose aim, VR17).");

            // VR15: con el rig del jugador la carga sigue en la pantalla grande (frente a la cabeza nueva) hasta que se quita.
            if (PantallaCargando.Visible && carga != null)
            {
                ComprobarEnPantallaGrande(carga, "La carga con el rig del jugador");
                Assert.IsNull(InicioCineVR.Actual.CamaraEscena, "La cabeza del jugador no va al fondo de la pantalla grande.");
                Assert.IsTrue(SistemaVR.VaAlVisor(a.Camara), "La cabeza del jugador va al visor.");
            }

            // La carga termina y se desvanece: no se queda nada encima de la vista.
            yield return EsperarHasta(() => !PantallaCargando.Visible
                && Object.FindObjectsByType<PantallaCargando>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length == 0,
                "que la pantalla de carga se desvanezca y se quite", 15f);
            yield return Cuadros(2);
            Assert.IsFalse(InicioCineVR.Actual.Activa, "Al entrar a la partida la pantalla grande de Inicio se quita.");
            Assert.IsFalse(PantallaCineVR.Visible, "Y no queda pantalla grande delante.");
            Assert.IsTrue(PantallaFundido.Actual == null || PantallaFundido.Actual.Alfa < 0.01f, "Ningún fundido se queda en negro.");

            // Una sola cámara de cabeza (MainCamera y visor), un solo oído, un solo XR Origin.
            var xr = new List<Camera>();
            var principales = new List<Camera>();
            foreach (Camera c in Object.FindObjectsByType<Camera>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (!c.isActiveAndEnabled || c.targetTexture != null)
                {
                    continue;
                }
                Debug.Log("[VR] Cámara habilitada tras la carga: " + c.name + " (tag " + c.tag + ", al visor " + SistemaVR.VaAlVisor(c) + ")");
                if (SistemaVR.VaAlVisor(c))
                {
                    xr.Add(c);
                }
                if (c.CompareTag("MainCamera"))
                {
                    principales.Add(c);
                }
            }
            Assert.AreEqual(1, xr.Count, "Una sola cámara va al visor.");
            Assert.AreSame(a.Camara, xr[0], "La del visor es la cabeza del jugador.");
            Assert.AreEqual(1, principales.Count, "Una sola cámara MainCamera habilitada.");
            Assert.AreSame(a.Camara, principales[0]);
            Assert.AreSame(a.Camara, Camera.main);
            Assert.AreSame(a.Camara, SistemaVR.CamaraVR, "SistemaVR manda la cámara del jugador al visor.");
            Assert.IsTrue(a.Camara.isActiveAndEnabled, "La cámara del rig del jugador queda activa.");
            Assert.AreEqual(1, Activos<AudioListener>(), "Un solo AudioListener.");
            Assert.AreEqual(1, Activos<XROrigin>(), "Un solo XR Origin.");
            Assert.AreSame(a.Origen, Object.FindFirstObjectByType<XROrigin>(FindObjectsInactive.Exclude), "El XR Origin es el del jugador.");

            // El láser sigue vivo en el control del jugador (antes colgaba del control del rig de menús).
            clics = 0;
            Button boton = CrearCanvasConBoton();
            yield return new WaitForSecondsRealtime(0.7f);
            yield return PulsarConLaser(boton, "un botón de prueba");
            Assert.IsTrue(vr.LaserDibujado, "El láser se dibuja en el control del jugador.");
            Assert.AreEqual(1, clics, "El gatillo hace clic con el rig del jugador.");
        }

        [UnityTest]
        [Timeout(180000)]
        public IEnumerator Inicio_CuartoOscuro_CabezaQuietaYCallesEnLaPantallaDeCine()
        {
            MenuInicio menu = null;
            yield return CargarInicioVR(m => menu = m);
            MenusVR vr = MenusVR.Actual;
            Camera cabeza = vr.CamaraRigMenus;
            Assert.IsNotNull(cabeza);

            // La cabeza vive en el cuarto oscuro: lejos de la ciudad, fondo sólido y poca distancia de dibujo.
            Assert.Less(cabeza.transform.position.y, -100f, "El rig de menús está lejos de la ciudad.");
            Assert.AreEqual(CameraClearFlags.SolidColor, cabeza.clearFlags, "Fondo sólido (sin cielo de la ciudad).");
            Assert.LessOrEqual(cabeza.farClipPlane, RigMenusVR.DistanciaDibujo + 0.01f, "No dibuja lejos (la ciudad no se ve).");

            // El recorrido de calles (como el de Menu_Calles) se anima, pero se dibuja en la textura de la pantalla de cine.
            RecorridoCallesMenu recorrido = CrearRecorrido();
            Camera calles = recorrido.Camara;
            yield return EsperarHasta(() => InicioCineVR.Actual != null && InicioCineVR.Actual.CamaraEscena == calles,
                "que la cámara del recorrido vaya a la pantalla de cine", 3f);
            yield return Cuadros(3);
            Assert.IsNotNull(calles.targetTexture, "La cámara del recorrido dibuja en una textura.");
            Assert.AreSame(InicioCineVR.Actual.Textura, calles.targetTexture);
            Assert.IsTrue(PantallaCineVR.Visible, "La pantalla de cine se ve.");
            Assert.AreSame(calles.targetTexture, PantallaCineVR.TexturaActual, "La pantalla de cine muestra la textura del recorrido.");
            Assert.IsFalse(SistemaVR.VaAlVisor(calles), "La cámara del recorrido no va al visor.");
            Assert.IsTrue(SistemaVR.VaAlVisor(cabeza), "Solo la cabeza va al visor.");
            Camera principal = Camera.main;
            if (principal != null && principal != calles)
            {
                Assert.IsNull(principal.targetTexture, "La cámara de Inicio recupera su targetTexture al cambiar al recorrido.");
            }
            // VR15: una sola pantalla grande curva (~105°, a 3.5 m) con el menú compuesto encima de las calles.
            ComprobarPantallaGrande(cabeza.transform);
            Assert.AreSame(InicioCineVR.Actual.TexturaUI, PantallaCineVR.TexturaEncima, "El menú va encima de las calles en la misma pantalla.");

            // Sin tracking nuevo la cabeza no se mueve aunque el recorrido avance.
            Vector3 posicion = cabeza.transform.position;
            Quaternion giro = cabeza.transform.rotation;
            Vector3 camaraAntes = calles.transform.position;
            yield return new WaitForSecondsRealtime(2f);
            Assert.Greater(Vector3.Distance(camaraAntes, calles.transform.position), 0.5f, "El recorrido de calles se anima.");
            Assert.Less(Vector3.Distance(posicion, cabeza.transform.position), 0.001f, "La cabeza no se movió.");
            Assert.Less(Quaternion.Angle(giro, cabeza.transform.rotation), 0.1f, "La cabeza no giró.");

            // El menú: en la pantalla grande, no como panel aparte; nada más va frente a la cabeza.
            Assert.IsTrue(menu.Canvas.enabled, "El menú se ve.");
            ComprobarEnPantallaGrande(menu.Canvas, "El menú de inicio");
            ComprobarUnaSolaPantalla("Inicio con el recorrido");

            // OPCIONES con el láser (hasta el punto de la curva donde se ve el botón) y el gatillo, sin barrer antes el láser:
            // el control se apunta una vez y se aprieta.
            Transform mano = vr.ManoLaser;
            Assert.IsNotNull(mano);
            Vector3 puntoOpciones = PuntoDe(menu.BotonOpciones);
            Assert.IsTrue(PantallaCineVR.Tocar(new Ray(cabeza.transform.position, puntoOpciones - cabeza.transform.position), out _, out Vector3 enCurva, out _),
                "OPCIONES se ve dentro de la pantalla grande.");
            Assert.Less(Vector3.Distance(enCurva, puntoOpciones), 0.05f, "El punto de OPCIONES está sobre la curva.");
            Quaternion mundo = Quaternion.LookRotation(puntoOpciones - mano.position);
            Quaternion padre = CuartoDe(mano);
            PonerMano(true, ManoDerechaCuarto, Quaternion.Inverse(padre) * mundo);
            yield return Cuadros(2);
            Assert.IsTrue(MenusVR.UIApuntada, "El láser apunta a OPCIONES.");
            yield return Pulsar(controlDer.triggerButton);
            yield return Cuadros(2);
            Assert.AreEqual(EstadoInicio.Opciones, menu.Estado, "El gatillo sobre OPCIONES abre las opciones.");
        }

        [UnityTest]
        [Timeout(120000)]
        public IEnumerator Intro_EnVR_VideoEnLaPantallaDeCine_BLaSaltaYSaleElMenu()
        {
            yield return IntroSeSaltaCon(controlDer.secondaryButton, "B");
        }

        [UnityTest]
        [Timeout(120000)]
        public IEnumerator Intro_EnVR_GatilloDerechoLaSalta_SinPulsarElMenu()
        {
            yield return IntroSeSaltaCon(controlDer.triggerButton, "el gatillo derecho");
        }

        // El video del logo en VR: se ve en la pantalla de cine, su Canvas de pantalla completa va oculto, el menú no se ve mientras suena,
        // y el botón lo salta (la misma vía que Enter en PC); después la pantalla muestra las calles y sale el menú debajo.
        private IEnumerator IntroSeSaltaCon(ButtonControl boton, string nombre)
        {
            IntroInicio.ReproducirEnEditor = true;
            yield return EditorSceneManager.LoadSceneInPlayMode(EscenaInicio.Ruta, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            MenuInicio menu = Object.FindFirstObjectByType<MenuInicio>();
            Assert.IsNotNull(menu);
            IntroInicio intro = menu.GetComponent<IntroInicio>();
            Assert.IsNotNull(intro, "Inicio.unity trae IntroInicio.");
            Assert.IsTrue(intro.DebeReproducir, "Inicio.unity trae el video del logo.");
            Assert.IsTrue(menu.IntroEnCurso, "El menú espera a la intro.");
            menu.SoloAnotarCarga = true;
            yield return EsperarHasta(() => MenusVR.Actual != null && MenusVR.Actual.RigMenusArmado && InicioCineVR.Actual != null,
                "el rig de menús con su pantalla de cine", 5f);
            yield return EsperarHasta(() => intro.Fase != FaseIntro.Video || InicioCineVR.Actual.IntroEnPantalla,
                "el video en la pantalla de cine", 5f);
            if (intro.Fase != FaseIntro.Video)
            {
                Assert.Inconclusive("El video del logo terminó o falló antes de verse (¿no se puede reproducir en este editor?).");
            }

            // VR15: el video del logo va en la misma pantalla grande, con su Canvas (negro y video) encima de todo, como en PC.
            Assert.IsNotNull(intro.Textura, "La intro dibuja el video en una textura.");
            Canvas lienzoIntro = null;
            foreach (Canvas c in intro.GetComponentsInChildren<Canvas>(true))
            {
                if (c.name == "IntroInicio_Pantalla")
                {
                    lienzoIntro = c;
                }
            }
            Assert.IsNotNull(lienzoIntro, "La intro arma su Canvas.");
            ComprobarEnPantallaGrande(lienzoIntro, "El video del logo");
            RawImage video = null;
            foreach (RawImage r in lienzoIntro.GetComponentsInChildren<RawImage>(true))
            {
                if (r.texture == intro.Textura)
                {
                    video = r;
                }
            }
            Assert.IsNotNull(video, "El Canvas de la intro muestra el video.");
            Assert.Greater(lienzoIntro.sortingOrder, menu.Canvas.sortingOrder, "El logo tapa el menú mientras suena (como en PC).");
            ComprobarPantallaGrande(MenusVR.Actual.Cabeza);
            ComprobarUnaSolaPantalla("El logo");
            // La cabeza no se mueve con el video.
            Vector3 cabezaVideo = MenusVR.Actual.Cabeza.position;
            yield return Cuadros(10);
            Assert.Less(Vector3.Distance(cabezaVideo, MenusVR.Actual.Cabeza.position), 0.001f, "La cabeza no se mueve con el video.");
            if (intro.Fase != FaseIntro.Video)
            {
                Assert.Inconclusive("El video del logo terminó antes de poder saltarlo.");
            }

            yield return Pulsar(boton);
            yield return EsperarHasta(() => !menu.IntroEnCurso, "que " + nombre + " salte la intro", 3f);
            Assert.IsTrue(intro.Saltada, nombre + " saltó la intro.");
            yield return EsperarHasta(() => menu.PrimerBoton.EstaElegido, "el menú tras la intro", 5f);
            yield return Cuadros(3);
            Assert.AreEqual(EstadoInicio.Principal, menu.Estado, "Saltar la intro no pulsa nada del menú.");
            Assert.IsTrue(menu.Canvas.enabled, "El menú se ve al acabar la intro.");
            Assert.IsFalse(InicioCineVR.Actual.IntroEnPantalla, "El video ya no está en la pantalla.");
            Assert.AreSame(InicioCineVR.Actual.Textura, PantallaCineVR.TexturaActual, "El fondo de la pantalla es la cámara de la escena.");
            Transform cabeza = MenusVR.Actual.Cabeza;
            ComprobarEnPantallaGrande(menu.Canvas, "El menú tras la intro");
            ComprobarPantallaGrande(cabeza);
            ComprobarUnaSolaPantalla("El menú tras la intro");
        }

        // Un Canvas Overlay como los del kit (1920x1080) con un botón de 400x200 px en el centro.
        private Button CrearCanvasConBoton()
        {
            var objeto = new GameObject("CanvasPruebaInicioVR", typeof(RectTransform));
            extras.Add(objeto);
            Canvas canvas = objeto.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            CanvasScaler escala = objeto.AddComponent<CanvasScaler>();
            escala.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            escala.referenceResolution = new Vector2(1920f, 1080f);
            objeto.AddComponent<GraphicRaycaster>();
            var objetoBoton = new GameObject("BotonPruebaInicioVR", typeof(RectTransform), typeof(Image), typeof(Button));
            objetoBoton.transform.SetParent(objeto.transform, false);
            ((RectTransform)objetoBoton.transform).sizeDelta = new Vector2(400f, 200f);
            Button boton = objetoBoton.GetComponent<Button>();
            boton.onClick.AddListener(() => clics++);
            return boton;
        }
    }
}
