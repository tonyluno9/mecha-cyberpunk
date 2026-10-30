using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using MechaCyberpunk.Combate.Vida;
using MechaCyberpunk.Core;
using MechaCyberpunk.Mecha.Estado;
using MechaCyberpunk.Mecha.Taller;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Personaje.Movimiento;
using MechaCyberpunk.Progresion.Pruebas;
using MechaCyberpunk.UI.Hud;
using MechaCyberpunk.UI.Taller;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Tests.Mecha
{
    /// <summary>
    /// T-149: reparar a medias en el banco del taller, en Pruebas_Economia con el jugador real y entrada simulada
    /// (E abre la pantalla, ↑ ↓ eligen, Enter confirma, Esc sale).
    /// </summary>
    public class ReparacionParcialPruebas : InputTestFixture
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Tony/Pruebas_Economia.unity";
        private const string VariableCaptura = "CAPTURAR_T149";
        private const string RutaCaptura = "docs/capturas/T-149-1.png";
        private const string TextoTodo600 = "REPARAR TODO · Ƀ600";
        private const string TextoMitad300 = "REPARAR LA MITAD · Ƀ300";
        private const string TextoTodo300 = "REPARAR TODO · Ƀ300";
        private const string TextoMitad150 = "REPARAR LA MITAD · Ƀ150";
        private const string TextoAlcanza = "CON LO QUE TIENES · Ƀ120 (+12)";
        private const string PrefijoAlcanza = "CON LO QUE TIENES";
        private const string PrefijoAviso = "E ·";
        private const string AdeudoDeRenta = "{\"diaProximoCobro\":15,\"adeudo\":500}";
        private const int FramesEstables = 10;
        private const int MaxFramesCalentamiento = 1000;
        private const float DeltaTimeEstable = 0.05f;
        private const float VidaMaximaEsperada = 100f;
        private const float MargenVida = 0.01f;
        private const float DistanciaAlBanco = 2f;
        private const float AlturaJugador = 0.05f;
        private const float SegundosEspera = 3f;
        private const float DistanciaPlanoCanvas = 1f;
        private const int AnchoCaptura = 1280;
        private const int AltoCaptura = 720;
        private const int ProfundidadCaptura = 24;

        private Keyboard teclado;
        private Mouse raton;
        private JugadorAPie jugador;
        private VidaMecha vida;
        private BancoTaller banco;
        private PantallaBancoTaller pantalla;
        private ICartera cartera;
        private HudAPie hud;
        private readonly List<string> errores = new List<string>();

        public override void Setup()
        {
            InputSystem.DisableAllEnabledActions();
            base.Setup();
            Registros.LimpiarTodos();
            teclado = InputSystem.AddDevice<Keyboard>();
            raton = InputSystem.AddDevice<Mouse>();
            MapasDeAccionesDePrueba.Reiniciar(teclado, raton);
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
                Object.Destroy(raiz);
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

        // Carga Pruebas_Economia, deja el mecha con esa vida y la cartera con ese dinero, y pone al jugador frente al banco.
        private IEnumerator Escenario(float vidaMecha, int dinero)
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            int estables = 0;
            int intentos = 0;
            while (estables < FramesEstables && intentos < MaxFramesCalentamiento)
            {
                yield return null;
                intentos++;
                estables = Time.deltaTime < DeltaTimeEstable ? estables + 1 : 0;
            }
            jugador = Object.FindFirstObjectByType<JugadorAPie>();
            banco = Object.FindFirstObjectByType<BancoTaller>();
            hud = Object.FindFirstObjectByType<HudAPie>();
            cartera = Registro<ICartera>.Actual;
            Assert.IsNotNull(jugador, "Pruebas_Economia trae el jugador");
            Assert.IsNotNull(banco, "El mecha trae el banco del taller");
            Assert.IsNotNull(cartera, "El jugador trae la cartera");
            Assert.IsTrue(banco.Registrado, "El banco se registra como interactuable");
            pantalla = banco.GetComponentInChildren<PantallaBancoTaller>(true);
            Assert.IsNotNull(pantalla, "El mecha trae la pantalla del banco");
            vida = banco.GetComponent<VidaMecha>();
            Assert.AreEqual(VidaMaximaEsperada, vida.Maxima, MargenVida, "El mecha tiene 100 de vida máxima");

            vida.FijarVida(vidaMecha);
            PonerDinero(dinero);
            yield return IrAlBanco();
        }

        private void PonerDinero(int dinero)
        {
            int diferencia = dinero - cartera.Dinero;
            if (diferencia > 0)
            {
                cartera.Abonar(diferencia, MotivoDinero.Otro);
            }
            else if (diferencia < 0)
            {
                cartera.CargarDeuda(-diferencia, MotivoDinero.Otro);
            }
            Assert.AreEqual(dinero, cartera.Dinero);
        }

        // Coloca al jugador cerca del punto del banco y espera a que el HUD ofrezca la interacción.
        private IEnumerator IrAlBanco()
        {
            Vector3 punto = Registro<ITallerMecha>.Actual.PuntoInteraccion;
            jugador.Colocar(new Pose(new Vector3(punto.x, AlturaJugador, punto.z + DistanciaAlBanco), Quaternion.Euler(0f, 180f, 0f)));
            float fin = Time.time + SegundosEspera;
            while (Time.time < fin && !hud.TextoAvisoInteraccion.text.StartsWith(PrefijoAviso, StringComparison.Ordinal))
            {
                yield return null;
            }
            Assert.IsTrue(hud.TextoAvisoInteraccion.text.StartsWith(PrefijoAviso, StringComparison.Ordinal),
                "El HUD ofrece usar el banco; dice: " + hud.TextoAvisoInteraccion.text);
        }

        private IEnumerator Tecla(KeyControl tecla)
        {
            Press(tecla);
            yield return null;
            Release(tecla);
            yield return null;
        }

        private List<string> TextosBase()
        {
            List<string> textos = new List<string>();
            for (int i = 0; i < pantalla.CantidadOpciones; i++)
            {
                textos.Add(pantalla.TextoBaseOpcion(i));
            }
            return textos;
        }

        [UnityTest]
        public IEnumerator A_Con40De100YMil_MuestraTodoYMitad_ElegirLaMitadDejaVida70YDinero700()
        {
            yield return Escenario(40f, 1000);

            yield return Tecla(teclado.eKey);
            Assert.IsTrue(pantalla.Visible, "E abre la pantalla del banco");
            Assert.AreEqual(1000, cartera.Dinero, "Abrir la pantalla no cobra");
            Assert.AreEqual(40f, vida.Actual, MargenVida, "Abrir la pantalla no repara");
            Assert.AreEqual(3, pantalla.CantidadOpciones, "Todo, mitad y PIEZAS DEL MECHA (T-168)");
            Assert.AreEqual(TextoTodo600, TextosBase()[0]);
            Assert.AreEqual(TextoMitad300, TextosBase()[1]);
            foreach (string texto in TextosBase())
            {
                Assert.IsFalse(texto.StartsWith(PrefijoAlcanza, StringComparison.Ordinal), "No aparece CON LO QUE TIENES");
            }
            Assert.AreEqual(0, pantalla.IndiceElegido, "Empieza en REPARAR TODO");
            Assert.AreEqual("VIDA 40 / 100", pantalla.Vida);

            yield return Tecla(teclado.downArrowKey);
            Assert.AreEqual(1, pantalla.IndiceElegido);
            yield return Tecla(teclado.enterKey);

            Assert.IsFalse(pantalla.Visible, "Enter cierra la pantalla");
            Assert.AreEqual(70f, vida.Actual, MargenVida, "Vida 70");
            Assert.AreEqual(700, cartera.Dinero, "Dinero Ƀ700");
            Assert.IsEmpty(errores, "consola sin errores: " + string.Join(" | ", errores));
        }

        [UnityTest]
        public IEnumerator B_Con70De100Y120_AparecenLasTres_ConLoQueTienesDejaVida82YDinero0()
        {
            yield return Escenario(70f, 120);

            yield return Tecla(teclado.eKey);
            Assert.IsTrue(pantalla.Visible);
            Assert.AreEqual(4, pantalla.CantidadOpciones, "Las tres de reparar y PIEZAS DEL MECHA (T-168)");
            Assert.AreEqual(TextoTodo300, TextosBase()[0]);
            Assert.AreEqual(TextoMitad150, TextosBase()[1]);
            Assert.AreEqual(TextoAlcanza, TextosBase()[2]);

            yield return Tecla(teclado.downArrowKey);
            yield return Tecla(teclado.downArrowKey);
            yield return Tecla(teclado.downArrowKey);
            Assert.AreEqual(3, pantalla.IndiceElegido, "↓ no pasa de la última opción (PIEZAS DEL MECHA)");
            yield return Tecla(teclado.upArrowKey);
            Assert.AreEqual(2, pantalla.IndiceElegido, "↑ vuelve a CON LO QUE TIENES");
            yield return Tecla(teclado.enterKey);

            Assert.AreEqual(82f, vida.Actual, MargenVida, "Vida 82");
            Assert.AreEqual(0, cartera.Dinero, "Dinero Ƀ0: pagó sin deuda");
            Assert.IsEmpty(errores, "consola sin errores: " + string.Join(" | ", errores));
        }

        [UnityTest]
        public IEnumerator C_Con70De100YMenos50_NoOfreceConLoQueTienes_RepararTodoQuedaEnMenos350()
        {
            yield return Escenario(70f, -50);

            yield return Tecla(teclado.eKey);
            Assert.IsTrue(pantalla.Visible);
            Assert.AreEqual(3, pantalla.CantidadOpciones, "Todo y mitad y PIEZAS DEL MECHA; sin CON LO QUE TIENES");
            Assert.AreEqual(TextoTodo300, TextosBase()[0]);
            Assert.AreEqual(TextoMitad150, TextosBase()[1]);
            Assert.AreEqual("DINERO -Ƀ50", pantalla.Dinero);
            Assert.Greater(pantalla.ColorDinero.r, pantalla.ColorDinero.g, "La deuda se pinta en rojo");

            yield return Tecla(teclado.enterKey);

            Assert.AreEqual(VidaMaximaEsperada, vida.Actual, MargenVida, "Vida 100");
            Assert.AreEqual(-350, cartera.Dinero, "Sigue siendo fiado: -Ƀ50 - Ƀ300");
            Assert.IsEmpty(errores, "consola sin errores: " + string.Join(" | ", errores));
        }

        [UnityTest]
        public IEnumerator D_EscNoCobraNiReparaYElJugadorVuelveAMoverse()
        {
            yield return Escenario(40f, 1000);
            ControladorAPie controlador = jugador.GetComponent<ControladorAPie>();
            Assert.IsTrue(controlador.ControlActivo);

            yield return Tecla(teclado.eKey);
            Assert.IsTrue(pantalla.Visible);
            Assert.IsFalse(controlador.ControlActivo, "La pantalla congela al jugador");

            yield return Tecla(teclado.escapeKey);
            yield return null;
            yield return null;
            Assert.IsFalse(pantalla.Visible, "Esc cierra la pantalla");
            Assert.AreEqual(1000, cartera.Dinero, "No cobró");
            Assert.AreEqual(40f, vida.Actual, MargenVida, "No reparó");
            Assert.IsTrue(controlador.ControlActivo, "El jugador vuelve a moverse");

            Vector3 antes = jugador.Raiz.position;
            Press(teclado.sKey);
            float fin = Time.time + 0.5f;
            while (Time.time < fin)
            {
                yield return null;
            }
            Release(teclado.sKey);
            yield return null;
            Assert.Greater(Vector3.Distance(antes, jugador.Raiz.position), 0.1f, "S mueve al jugador otra vez");
            Assert.IsEmpty(errores, "consola sin errores: " + string.Join(" | ", errores));
        }

        [UnityTest]
        public IEnumerator D_ConElTallerBloqueadoPorRenta_ENoAbreLaPantalla_YConDineroPagaLaRentaComoAntes()
        {
            yield return Escenario(40f, 1000);
            IRenta renta = Registro<IRenta>.Actual;
            Assert.IsNotNull(renta, "El jugador trae la renta");
            ((IGuardable)renta).Cargar(AdeudoDeRenta);
            PonerDinero(0);
            Assert.IsTrue(renta.TallerBloqueado);
            yield return null;

            yield return Tecla(teclado.eKey);
            Assert.IsFalse(pantalla.Visible, "Bloqueado: no abre la pantalla");
            Assert.AreEqual(0, cartera.Dinero, "No cobró");
            Assert.AreEqual(40f, vida.Actual, MargenVida, "No reparó");

            PonerDinero(600);
            yield return null;
            yield return Tecla(teclado.eKey);
            Assert.IsFalse(pantalla.Visible, "Pagar la renta no abre la pantalla");
            Assert.AreEqual(100, cartera.Dinero, "Pagó la renta de Ƀ500");
            Assert.IsFalse(renta.TallerBloqueado);
            Assert.AreEqual(40f, vida.Actual, MargenVida, "Pagar la renta no repara");

            yield return null;
            yield return Tecla(teclado.eKey);
            Assert.IsTrue(pantalla.Visible, "Con la renta pagada, E abre la pantalla");
            Assert.IsEmpty(errores, "consola sin errores: " + string.Join(" | ", errores));
        }

        [UnityTest]
        public IEnumerator SinPantallaEnLaEscena_ERepararTodoComoAntes()
        {
            yield return Escenario(40f, 1000);
            pantalla.gameObject.SetActive(false);
            yield return null;

            yield return Tecla(teclado.eKey);
            Assert.AreEqual(VidaMaximaEsperada, vida.Actual, MargenVida, "Sin pantalla repara todo");
            Assert.AreEqual(400, cartera.Dinero, "Cobra el costo completo");
            Assert.IsEmpty(errores, "consola sin errores: " + string.Join(" | ", errores));
        }

        [UnityTest]
        public IEnumerator Captura_PantallaDelBancoConLasTresOpciones_GuardaT149Uno()
        {
            if (Environment.GetEnvironmentVariable(VariableCaptura) != "1")
            {
                Assert.Ignore("Sin " + VariableCaptura + "=1 no se regraba la captura.");
            }
            yield return Escenario(70f, 120);
            yield return Tecla(teclado.eKey);
            Assert.AreEqual(4, pantalla.CantidadOpciones);
            yield return Tecla(teclado.downArrowKey);
            yield return null;

            // Los lienzos en Overlay no salen en una cámara a textura: se pasan un instante a ScreenSpaceCamera.
            Camera camara = jugador.Camara;
            List<Canvas> movidos = new List<Canvas>();
            foreach (Canvas lienzo in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                if (lienzo.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    lienzo.renderMode = RenderMode.ScreenSpaceCamera;
                    lienzo.worldCamera = camara;
                    lienzo.planeDistance = DistanciaPlanoCanvas;
                    movidos.Add(lienzo);
                }
            }
            RenderTexture destino = new RenderTexture(AnchoCaptura, AltoCaptura, ProfundidadCaptura);
            Texture2D imagen = new Texture2D(AnchoCaptura, AltoCaptura, TextureFormat.RGB24, false);
            RenderTexture anterior = RenderTexture.active;
            camara.targetTexture = destino;
            camara.Render();
            RenderTexture.active = destino;
            imagen.ReadPixels(new Rect(0f, 0f, AnchoCaptura, AltoCaptura), 0, 0);
            imagen.Apply();
            camara.targetTexture = null;
            RenderTexture.active = anterior;
            foreach (Canvas lienzo in movidos)
            {
                lienzo.renderMode = RenderMode.ScreenSpaceOverlay;
            }
            string completa = Path.GetFullPath(Path.Combine(Application.dataPath, "..", RutaCaptura));
            Directory.CreateDirectory(Path.GetDirectoryName(completa));
            File.WriteAllBytes(completa, imagen.EncodeToPNG());
            Object.Destroy(imagen);
            destino.Release();
            Object.Destroy(destino);
        }
    }
}
