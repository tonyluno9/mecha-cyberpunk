using System;
using System.Collections;
using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Economia;
using MechaCyberpunk.Personaje;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Personaje.Movimiento;
using MechaCyberpunk.Progresion.Pruebas;
using MechaCyberpunk.UI;
using MechaCyberpunk.UI.Armas;
using MechaCyberpunk.UI.Kit;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Tests.UI
{
    /// <summary>
    /// T-814: el banco de armas de Tino en una escena armada aquí (piso, JugadorAPie.prefab, cartera de prueba y el banco). E abre el menú y
    /// congela al jugador; Enter compra el cargador (nivel I: inventario y dinero); → pasa a la escopeta (imagen holográfica); arrastrar gira el
    /// holograma; Esc cierra, el control vuelve y sale el letrero ARMA MEJORADA. Con CAPTURAR_S2N_ARMAS=1 guarda docs/capturas/s2n-menu-armas-*.png.
    /// </summary>
    public class MenuArmasPruebas : InputTestFixture
    {
        private const string RutaJugador = "Assets/_Proyecto/Personaje/Prefabs/JugadorAPie.prefab";
        private const string RutaConfig = "Assets/_Proyecto/UI/Datos/ConfigMenuArmas_Base.asset";
        private const string RutaEscopeta = "Assets/_Proyecto/Personaje/Datos/Armas/DatosArma_escopeta_0.asset";
        private const string VariableCaptura = "CAPTURAR_S2N_ARMAS";
        private const int DineroInicial = 500;
        private const int PrecioPistolaI = 150;
        private const int PrecioEscopetaI = 250;
        private const float SegundosEspera = 5f;

        private sealed class CarteraDoble : ICartera
        {
            public int Dinero { get; set; }

            public event Action<int, MotivoDinero> Cambio;

            public bool PuedePagar(int cantidad) => cantidad >= 0 && Dinero >= cantidad;

            public bool IntentarPagar(int cantidad, MotivoDinero motivo)
            {
                if (!PuedePagar(cantidad))
                {
                    return false;
                }
                Dinero -= cantidad;
                Cambio?.Invoke(-cantidad, motivo);
                return true;
            }

            public void Abonar(int cantidad, MotivoDinero motivo)
            {
                Dinero += cantidad;
                Cambio?.Invoke(cantidad, motivo);
            }

            public void CargarDeuda(int cantidad, MotivoDinero motivo) => Abonar(-cantidad, motivo);
        }

        private Keyboard teclado;
        private Mouse raton;
        private readonly HashSet<GameObject> raicesIniciales = new HashSet<GameObject>();
        private readonly List<string> errores = new List<string>();
        private ICartera cartera;

        public override void Setup()
        {
            InputSystem.DisableAllEnabledActions();
            base.Setup();
            Registros.LimpiarTodos();
            teclado = InputSystem.AddDevice<Keyboard>();
            raton = InputSystem.AddDevice<Mouse>();
            MapasDeAccionesDePrueba.Reiniciar(teclado, raton);
            raicesIniciales.Clear();
            raicesIniciales.UnionWith(SceneManager.GetActiveScene().GetRootGameObjects());
            errores.Clear();
            Application.logMessageReceived += RegistrarError;
        }

        public override void TearDown()
        {
            Application.logMessageReceived -= RegistrarError;
            Registros.LimpiarTodos();
            MapasDeAccionesDePrueba.AntesDelTearDown(teclado, raton);
            base.TearDown();
            MapasDeAccionesDePrueba.DespuesDelTearDown();
        }

        [UnityTearDown]
        public IEnumerator DescargarEscena()
        {
            foreach (GameObject raiz in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                if (!raicesIniciales.Contains(raiz))
                {
                    Object.Destroy(raiz);
                }
            }
            yield return null;
            Registros.LimpiarTodos();
        }

        private void RegistrarError(string mensaje, string pila, LogType tipo)
        {
            if (tipo == LogType.Error || tipo == LogType.Exception || tipo == LogType.Assert)
            {
                errores.Add(mensaje);
            }
        }

        private static IEnumerator EsperarHasta(Func<bool> condicion, string que)
        {
            float limite = Time.realtimeSinceStartup + SegundosEspera;
            while (!condicion())
            {
                if (Time.realtimeSinceStartup > limite)
                {
                    Assert.Fail("No pasó a tiempo: " + que);
                }
                yield return null;
            }
        }

        private IEnumerator Pulsar(UnityEngine.InputSystem.Controls.ButtonControl tecla)
        {
            Press(tecla);
            yield return null;
            Release(tecla);
            yield return null;
        }

        private static void Capturar(string nombre)
        {
            if (Environment.GetEnvironmentVariable(VariableCaptura) != "1")
            {
                return;
            }
            GameObject go = new GameObject("CamaraCaptura");
            go.transform.position = new Vector3(0f, -1000f, 0f);
            Camera camara = go.AddComponent<Camera>();
            camara.clearFlags = CameraClearFlags.SolidColor;
            camara.backgroundColor = Color.black;
            camara.farClipPlane = 5f;
            CapturaUI.Capturar("docs/capturas/" + nombre, camara);
            Object.Destroy(go);
        }

        [UnityTest]
        public IEnumerator Banco_ComprarCargador_SubeNivelCobraYAlSalirDevuelveElControl()
        {
            ConfigMenuArmas config = AssetDatabase.LoadAssetAtPath<ConfigMenuArmas>(RutaConfig);
            Assert.IsNotNull(config, "Existe la config del menú");

            GameObject piso = GameObject.CreatePrimitive(PrimitiveType.Plane);
            piso.transform.localScale = Vector3.one * 3f;
            GameObject jugadorGo = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaJugador), new Vector3(0f, 0.05f, 0f), Quaternion.identity);
            GameObject bancoGo = new GameObject("BancoArmas");
            bancoGo.SetActive(false);
            bancoGo.transform.position = new Vector3(0f, 0f, 1.5f);
            BancoArmas banco = bancoGo.AddComponent<BancoArmas>();
            banco.Configurar(config);
            bancoGo.SetActive(true);
            yield return null;
            yield return null;

            // El prefab puede traer su propia cartera; si no, la de prueba.
            if (Registro<ICartera>.Actual == null)
            {
                Registro<ICartera>.Agregar(new CarteraDoble());
            }
            cartera = Registro<ICartera>.Actual;
            cartera.Abonar(DineroInicial - cartera.Dinero, MotivoDinero.Otro);
            ServicioInventario inventario = Registro<ServicioInventario>.Actual;
            ArsenalJugador arsenal = Registro<ArsenalJugador>.Actual;
            ControladorAPie controlador = jugadorGo.GetComponent<ControladorAPie>();
            Assert.IsNotNull(inventario, "El jugador trae inventario");
            Assert.IsNotNull(arsenal, "El jugador trae arsenal");
            Assert.IsNotNull(controlador);
            Assert.AreEqual(0, arsenal.Nivel(ArmaAPie.Pistola), "La pistola empieza en nivel 0");
            inventario.Agregar(AssetDatabase.LoadAssetAtPath<DatosArma>(RutaEscopeta), 1);
            yield return new WaitForSecondsRealtime(0.3f);
            Assert.AreEqual(config.TextoAviso, banco.Texto);
            Assert.IsTrue(banco.PuedeUsar, "El banco se puede usar a pie");
            // T-865: la pantalla ya está armada (escondida) antes del primer E: abre al instante.
            Assert.IsNotNull(banco.Pantalla, "la pantalla se arma al entrar");
            Assert.IsNotNull(banco.Pantalla.Canvas, "con su lienzo ya construido");
            Assert.IsFalse(banco.Pantalla.Visible, "pero escondida");

            // E abre el menú y congela al jugador.
            yield return Pulsar(teclado.eKey);
            yield return EsperarHasta(() => banco.PantallaAbierta, "abrir el banco con E");
            PantallaMejoraArmas pantalla = banco.Pantalla;
            Assert.IsTrue(pantalla.Visible);
            Assert.IsFalse(controlador.ControlActivo, "El jugador queda congelado");
            Assert.AreEqual(2, pantalla.CantidadArmas, "Pistola y escopeta (solo las que traes)");
            Assert.AreEqual(ArmaAPie.Pistola, pantalla.ArmaElegida.Arma);
            Assert.AreEqual(PiezaArma.Cargador, pantalla.PiezaElegida, "Empieza en la pieza que sigue");
            Assert.AreEqual(EstadoPieza.Disponible, pantalla.Estado(PiezaArma.Cargador));
            Assert.AreEqual(EstadoPieza.Bloqueada, pantalla.Estado(PiezaArma.Canon));
            Assert.AreEqual(EstadoPieza.Bloqueada, pantalla.Estado(PiezaArma.Mira));
            StringAssert.Contains("PRIMERO EL CARGADOR", pantalla.EtiquetaPieza(PiezaArma.Canon));
            StringAssert.Contains("Ƀ150", pantalla.EtiquetaPieza(PiezaArma.Cargador));
            StringAssert.Contains("Ƀ500", pantalla.Dinero);
            StringAssert.StartsWith("CARGADOR 8 → 10", pantalla.TextoTabla(1), "La tabla muestra el cargador antes y después");

            VistaHologramaArma vista = pantalla.Vista;
            Assert.IsTrue(vista.ConModelo, "La pistola se ve con su modelo W01");
            Assert.IsNotNull(vista.Textura);
            Assert.IsTrue(vista.TienePieza(PiezaArma.Cargador) && vista.TienePieza(PiezaArma.Canon) && vista.TienePieza(PiezaArma.Mira));
            yield return new WaitForSecondsRealtime(0.8f);
            Assert.Greater(vista.Separacion(PiezaArma.Cargador), 0.9f, "El cargador se separa del arma");
            Assert.Less(vista.Separacion(PiezaArma.Canon), 0.1f, "El cañón se queda en su lugar");
            Capturar("ui-banco-1.png");

            // Enter compra el cargador: nivel I, cobra el precio de la oferta.
            yield return Pulsar(teclado.enterKey);
            Assert.AreEqual(1, arsenal.Nivel(ArmaAPie.Pistola), "La pistola sube a nivel I");
            Assert.AreEqual(DineroInicial - PrecioPistolaI, cartera.Dinero, "Cobra Ƀ150");
            Assert.AreEqual(EstadoPieza.Instalada, pantalla.Estado(PiezaArma.Cargador));
            Assert.AreEqual(EstadoPieza.Disponible, pantalla.Estado(PiezaArma.Canon));
            StringAssert.Contains("INSTALADO", pantalla.Aviso);
            // T-895: al mejorar, la franja recorre el arma, el cargador regresa y luego destella.
            Assert.IsTrue(vista.Escaneando, "El escaneo arranca al mejorar");
            yield return new WaitForSecondsRealtime(config.DuracionEscaneo * 0.5f);
            Assert.Greater(vista.AvanceEscaneo, 0.1f, "La franja avanza");
            Capturar("ui-banco-escaneo.png");
            yield return EsperarHasta(() => !vista.Escaneando, "que termine el escaneo");
            yield return new WaitForSecondsRealtime(0.6f);
            Assert.Less(vista.Separacion(PiezaArma.Cargador), 0.1f, "El cargador regresa a su lugar");
            yield return new WaitForSecondsRealtime(0.15f);
            Capturar("ui-banco-2.png");

            // Enter otra vez sobre el cargador no cobra.
            yield return Pulsar(teclado.enterKey);
            Assert.AreEqual(DineroInicial - PrecioPistolaI, cartera.Dinero, "Lo instalado no se vuelve a cobrar");

            // ↓ pasa al cañón: se separa y ya se puede comprar.
            yield return Pulsar(teclado.downArrowKey);
            Assert.AreEqual(PiezaArma.Canon, pantalla.PiezaElegida);
            StringAssert.Contains("Ƀ300", pantalla.TextoBoton);
            yield return new WaitForSecondsRealtime(0.8f);
            Assert.Greater(vista.Separacion(PiezaArma.Canon), 0.9f, "El cañón se separa");
            Assert.Less(vista.Separacion(PiezaArma.Cargador), 0.1f, "El cargador regresa");
            Capturar("ui-banco-3.png");

            // ↓ pasa a la mira: bloqueada (primero el cañón); Enter no cobra.
            yield return Pulsar(teclado.downArrowKey);
            Assert.AreEqual(PiezaArma.Mira, pantalla.PiezaElegida);
            StringAssert.Contains("PRIMERO EL CAÑÓN", pantalla.TextoBoton);
            yield return Pulsar(teclado.enterKey);
            Assert.AreEqual(DineroInicial - PrecioPistolaI, cartera.Dinero, "Una pieza bloqueada no se cobra");
            Assert.AreEqual(1, arsenal.Nivel(ArmaAPie.Pistola));
            yield return new WaitForSecondsRealtime(0.8f);
            Assert.Greater(vista.Separacion(PiezaArma.Mira), 0.9f, "La mira se separa");
            Capturar("ui-banco-6.png");

            // Arrastrar sobre el holograma lo gira.
            float giroAntes = vista.Giro;
            Set(raton.position, new Vector2(Screen.width * 0.5f, Screen.height * 0.5f));
            yield return null;
            Press(raton.leftButton);
            yield return null;
            Set(raton.delta, new Vector2(-200f, 0f));
            yield return null;
            Release(raton.leftButton);
            yield return null;
            Assert.Greater(Mathf.Abs(Mathf.DeltaAngle(giroAntes, vista.Giro)), 30f, "Arrastrar gira el holograma");

            // → pasa a la escopeta: T-942, ahora también en 3D y con sus piezas partidas de la malla.
            yield return Pulsar(teclado.rightArrowKey);
            Assert.AreEqual(ArmaAPie.Escopeta, pantalla.ArmaElegida.Arma);
            Assert.IsTrue(vista.ConModelo, "La escopeta se ve con su modelo W02");
            Assert.IsTrue(vista.TienePieza(PiezaArma.Cargador) && vista.TienePieza(PiezaArma.Canon) && vista.TienePieza(PiezaArma.Mira), "La escopeta tiene sus tres piezas");
            Assert.AreEqual(PiezaArma.Cargador, pantalla.PiezaElegida);
            yield return new WaitForSecondsRealtime(0.8f);
            Assert.Greater(vista.Separacion(PiezaArma.Cargador), 0.9f, "El cargador de la escopeta se separa");
            Capturar("ui-banco-4.png");
            // Comprar el cargador de la escopeta: usa sus estadísticas y precio por nivel (DatosArma_escopeta_1).
            int dineroAntes = cartera.Dinero;
            yield return Pulsar(teclado.enterKey);
            Assert.AreEqual(1, arsenal.Nivel(ArmaAPie.Escopeta), "La escopeta sube a nivel I");
            Assert.AreEqual(PrecioEscopetaI, dineroAntes - cartera.Dinero, "Cobra el precio de DatosArma_escopeta_1");
            yield return EsperarHasta(() => !vista.Escaneando, "que termine el escaneo de la escopeta");

            // T-900: el filo de la casilla sale del nivel real; las pestañas y categorías filtran la lista sin perder las armas.
            Assert.AreEqual(TrazoH10.Rareza(1), pantalla.ColorFilo(0), "La pistola en nivel I lleva el filo de ese nivel");
            pantalla.FijarCategoria(ArmaAPie.Escopeta);
            Assert.AreEqual(1, pantalla.CantidadArmas, "Solo la escopeta");
            Assert.AreEqual(ArmaAPie.Escopeta, pantalla.ArmaElegida.Arma);
            pantalla.FijarCategoria(ArmaAPie.Escopeta);
            Assert.AreEqual(2, pantalla.CantidadArmas, "Elegir la misma categoría quita el filtro");
            pantalla.FijarFiltro(FiltroArmas.AlMaximo);
            Assert.AreEqual(0, pantalla.CantidadArmas, "Ninguna arma al máximo");
            pantalla.FijarFiltro(FiltroArmas.Todas);
            Assert.AreEqual(2, pantalla.CantidadArmas);
            yield return null;

            // Esc cierra; el control vuelve un cuadro después y sale el letrero ARMA MEJORADA.
            yield return Pulsar(teclado.escapeKey);
            Assert.IsFalse(banco.PantallaAbierta, "Esc cierra el banco");
            Assert.IsFalse(pantalla.Visible);
            yield return null;
            Assert.IsTrue(controlador.ControlActivo, "El control vuelve al jugador");
            Assert.IsTrue(pantalla.LetreroVisible, "Sale el letrero ARMA MEJORADA");
            StringAssert.Contains("ESCOPETA I", pantalla.Letrero);
            yield return new WaitForSecondsRealtime(0.4f);
            Capturar("ui-banco-5.png");
            yield return new WaitForSecondsRealtime(config.DuracionLetrero);
            Assert.IsFalse(pantalla.LetreroVisible, "El letrero se va solo");
            Assert.IsEmpty(errores, "Sin errores: " + string.Join(" | ", errores));
        }
    }
}
