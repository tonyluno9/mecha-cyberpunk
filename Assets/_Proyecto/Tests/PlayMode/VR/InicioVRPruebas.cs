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
    /// (cabeza y control derecho): el menú sale como panel a 1.5 m frente a la cabeza y NUEVA PARTIDA, CONTINUAR, OPCIONES y SALIR
    /// se eligen con el láser del control derecho y el gatillo. Al cargar la partida se pasa del rig de menús al del jugador
    /// (AdaptadorVR) sin quedarse en negro ni duplicar cámaras, oídos ni XR Origin. La pantalla de carga va como panel frente a la cabeza.
    /// La carga usa Pruebas_APie (jugador a pie y piso) en vez de la ciudad entera. Partidas y opciones van a una carpeta temporal:
    /// partida_auto.json y las ranuras de Tony no se tocan.
    /// </summary>
    public class InicioVRPruebas : InputTestFixture
    {
        private const string RutaAPie = "Assets/_Proyecto/Escenas/Tony/Pruebas_APie.unity";
        private static readonly Vector3 CabezaCuarto = new Vector3(0f, 1.6f, 0f);
        private static readonly Vector3 ManoDerechaCuarto = new Vector3(0.2f, 1.2f, 0.3f);
        private static readonly Vector3 ManoIzquierdaCuarto = new Vector3(-0.2f, 1.2f, 0.3f);
        private const float ToleranciaDistancia = 0.15f;
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

        // Gira el control derecho (en su lugar de siempre) para que el láser apunte a un punto del mundo.
        private IEnumerator ApuntarA(Vector3 punto)
        {
            PonerMano(true, ManoDerechaCuarto, Quaternion.identity);
            yield return Cuadros(2);
            Transform mano = MenusVR.Actual.ManoLaser;
            Assert.IsNotNull(mano, "Hay un control del que sale el láser.");
            Quaternion mundo = Quaternion.LookRotation(punto - mano.position);
            Quaternion padre = mano.parent != null ? mano.parent.rotation : Quaternion.identity;
            PonerMano(true, ManoDerechaCuarto, Quaternion.Inverse(padre) * mundo);
            yield return Cuadros(4);
        }

        private IEnumerator PulsarConLaser(Component boton, string nombre)
        {
            yield return ApuntarA(CentroDe(boton));
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
                && menu.Canvas.renderMode == RenderMode.WorldSpace, "el rig de menús y el menú en panel", 5f);
            // Las animaciones de entrada de los paneles y botones.
            yield return new WaitForSecondsRealtime(SegundosEntradaMenu);
            alListo(menu);
        }

        // Un Canvas a 1.5 m frente a la cabeza, mirando hacia ella, de 1.6 m de ancho.
        private static void ComprobarPanelFrente(Canvas canvas, Transform cabeza, string que)
        {
            Assert.AreEqual(RenderMode.WorldSpace, canvas.renderMode, que + " pasa a World Space (el Overlay no se ve en el visor).");
            Assert.AreEqual(TipoLienzoVR.Frente, MenusVR.Actual.TipoDe(canvas), que + " va como panel frente a la cabeza.");
            Vector3 hacia = canvas.transform.position - cabeza.position;
            Vector3 frente = ReglaLienzosVR.FrentePlano(cabeza.forward, cabeza.up);
            float ancho = ((RectTransform)canvas.transform).rect.width * canvas.transform.lossyScale.x;
            Debug.Log("[VR] " + que + " a " + hacia.magnitude.ToString("0.00") + " m de la cabeza; ancho " + ancho.ToString("0.00") + " m.");
            Assert.AreEqual(ReglaLienzosVR.DistanciaFrente, hacia.magnitude, ToleranciaDistancia, que + " a 1.5 m de la cabeza.");
            Assert.Greater(Vector3.Dot(frente, hacia.normalized), 0.95f, que + " enfrente de la cabeza.");
            Assert.Greater(Vector3.Dot(canvas.transform.forward, hacia.normalized), 0.95f, que + " de cara a la cabeza (se lee derecho).");
            Assert.AreEqual(ReglaLienzosVR.AnchoFrente, ancho, 0.15f, que + ": 1920 px ≈ 1.6 m.");
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
            Assert.AreEqual(StereoTargetEyeMask.Both, cabezaRig.stereoTargetEye);
            Assert.IsTrue(cabezaRig.isActiveAndEnabled);
            Assert.IsFalse(cabezaRig.CompareTag("MainCamera"), "El rig de menús no le quita Camera.main a la escena.");
            Assert.AreEqual(1, Activos<XROrigin>(), "Un solo XR Origin (el del rig de menús).");
            Assert.AreEqual(1, Activos<AudioListener>(), "Un solo AudioListener.");
            foreach (Camera c in Camera.allCameras)
            {
                if (c != cabezaRig && c.targetTexture == null)
                {
                    Assert.AreEqual(StereoTargetEyeMask.None, c.stereoTargetEye, c.name + " no debe verse en el visor.");
                }
            }
            Assert.IsTrue(MenusVR.MenuAbierto, "Sin jugador, el panel de Inicio cuenta como menú abierto (quieto, láser visible).");
            ComprobarPanelFrente(menu.Canvas, vr.Cabeza, "El menú de inicio");

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
            yield return ApuntarA(CentroDe(menu.Continuar) + Vector3.up * 3f);
            Assert.IsFalse(MenusVR.UIApuntada, "Fuera del panel no apunta a la UI.");
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

            // La pantalla de carga: panel a 1.5 m frente a la cabeza, no Overlay (que en el visor no se ve).
            Canvas carga = PantallaCargando.Actual.GetComponentInChildren<Canvas>(true);
            Assert.IsNotNull(carga);
            yield return EsperarHasta(() => carga.renderMode == RenderMode.WorldSpace, "la carga en World Space", 2f);
            yield return Cuadros(3);
            ComprobarPanelFrente(carga, vr.Cabeza, "La pantalla de carga");
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
            ComprobarPanelFrente(carga, vr.Cabeza, "La pantalla de carga");

            yield return EsperarHasta(() => operacion.isDone, "que termine la carga de la escena", 60f);
            Assert.AreEqual("Pruebas_APie", SceneManager.GetActiveScene().name, "La escena nueva quedó activa.");
            yield return EsperarHasta(() => AdaptadorVR.Actual != null && AdaptadorVR.Actual.Armado, "el rig VR del jugador de la escena nueva", 10f);
            AdaptadorVR a = AdaptadorVR.Actual;
            yield return Cuadros(3);

            // El rig de menús se fue y manda el del jugador.
            Assert.IsFalse(vr.RigMenusArmado, "El rig de menús se quita al armarse el del jugador.");
            Assert.IsTrue(raizRig == null || !raizRig.activeInHierarchy, "El rig de menús quedó destruido o apagado.");
            Assert.AreSame(a.Camara.transform, vr.Cabeza, "La cabeza es la cámara del jugador.");
            Assert.AreSame(a.ManoDerecha.transform, vr.ManoLaser, "El láser sale del control derecho del jugador.");

            // La carga termina y se desvanece: no se queda nada encima de la vista.
            yield return EsperarHasta(() => !PantallaCargando.Visible
                && Object.FindObjectsByType<PantallaCargando>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length == 0,
                "que la pantalla de carga se desvanezca y se quite", 15f);
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
                Debug.Log("[VR] Cámara habilitada tras la carga: " + c.name + " (tag " + c.tag + ", ojos " + c.stereoTargetEye + ")");
                if (c.stereoTargetEye != StereoTargetEyeMask.None)
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
