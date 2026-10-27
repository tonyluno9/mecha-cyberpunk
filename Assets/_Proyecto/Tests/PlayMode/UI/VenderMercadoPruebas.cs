using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using MechaCyberpunk.Core;
using MechaCyberpunk.Economia;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Personaje.Movimiento;
using MechaCyberpunk.Personaje.Pruebas;
using MechaCyberpunk.Progresion.Guardado;
using MechaCyberpunk.Progresion.Pruebas;
using MechaCyberpunk.UI.Hud;
using MechaCyberpunk.UI.Tiendas;
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
    /// T-147: vender piezas y chatarra en el mostrador del mercado negro de Pruebas_Economia. Todo con entrada simulada (W, E, flechas,
    /// Enter, Esc); lo único que no es entrada es dejar el dinero y el inventario de salida y cerrar el lugar de prueba.
    /// </summary>
    public class VenderMercadoPruebas : InputTestFixture
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Tony/Pruebas_Economia.unity";
        private const string RutaKit = "Assets/_Proyecto/Economia/Datos/DatosObjeto_KitReparacion.asset";
        private const string RutaBrazo = "Assets/_Proyecto/Economia/Datos/DatosObjeto_BrazoChatarra.asset";
        private const string RutaChatarra = "Assets/_Proyecto/Economia/Datos/DatosObjeto_Chatarra.asset";
        private const string TextoBrazo = "BRAZO DE CHATARRA";
        private const string TextoNada = "NO TIENES NADA QUE LE INTERESE";
        private const string TextoHudVenta = "+$300 · VENTA";
        private const string TextoCerrado = "CERRADO · abre 09:00";
        private const int DineroInicial = 500;
        private const int DineroPoco = 100;
        private const int PagoBrazo = 300;
        private const float HoraAbre = 9f;
        private const float HoraCierra = 18f;
        private const int FramesEstables = 10;
        private const int MaxFramesCalentamiento = 1000;
        private const int FramesParaElHud = 5;
        private const float DeltaTimeEstable = 0.05f;
        private const float MaxSegundosCaminando = 10f;
        private const float DistanciaDeInicio = 4f;
        private const float DistanciaDeUso = 2f;
        private const float AlturaInicio = 0.05f;
        private const float SegundosCaminando = 0.5f;
        private const float MetrosMinimosAlMoverse = 0.3f;
        private const string VariableCaptura = "CAPTURAR_T147";
        private const string RutaCaptura = "docs/capturas/T-147-1.png";
        private const int AnchoCaptura = 1280;
        private const int AltoCaptura = 720;
        private const int ProfundidadCaptura = 24;
        private const int SaltoDePixeles = 97;
        private const float VariacionMinima = 0.05f;
        private const float DistanciaPlanoCanvas = 0.35f;

        private Keyboard teclado;
        private Mouse raton;
        private string carpeta;
        private JugadorAPie jugador;
        private ControladorAPie controlador;
        private MostradorTienda mostrador;
        private PantallaTienda pantalla;
        private HudAPie hud;
        private ICartera cartera;
        private ServicioInventario inventario;
        private DatosObjeto kit;
        private DatosObjeto brazo;
        private DatosObjeto chatarra;
        private readonly List<string> errores = new List<string>();

        public override void Setup()
        {
            InputSystem.DisableAllEnabledActions();
            base.Setup();
            Registros.LimpiarTodos();
            teclado = InputSystem.AddDevice<Keyboard>();
            raton = InputSystem.AddDevice<Mouse>();
            MapasDeAccionesDePrueba.Reiniciar(teclado, raton);
            carpeta = Path.Combine(Application.temporaryCachePath, "vender_mercado_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(carpeta);
            ServicioGuardado.RutaForzada = Path.Combine(carpeta, "partida.json");
            errores.Clear();
            Application.logMessageReceived += RegistrarError;
        }

        public override void TearDown()
        {
            Application.logMessageReceived -= RegistrarError;
            ServicioGuardado.RutaForzada = null;
            if (Directory.Exists(carpeta))
            {
                Directory.Delete(carpeta, true);
            }
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

        // ---- Preparación y utilidades ----

        private IEnumerator CargarEscena()
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
            mostrador = Object.FindFirstObjectByType<MostradorTienda>();
            hud = Object.FindFirstObjectByType<HudAPie>();
            Assert.IsNotNull(jugador, "Pruebas_Economia trae al jugador");
            Assert.IsNotNull(mostrador, "Pruebas_Economia trae el mostrador del mercado negro");
            Assert.IsNotNull(hud, "El jugador trae el HUD a pie");
            controlador = jugador.GetComponent<ControladorAPie>();
            pantalla = mostrador.GetComponentInChildren<PantallaTienda>(true);
            Assert.IsNotNull(pantalla, "El mostrador trae su pantalla");
            cartera = Registro<ICartera>.Actual;
            inventario = Registro<ServicioInventario>.Actual;
            kit = AssetDatabase.LoadAssetAtPath<DatosObjeto>(RutaKit);
            brazo = AssetDatabase.LoadAssetAtPath<DatosObjeto>(RutaBrazo);
            chatarra = AssetDatabase.LoadAssetAtPath<DatosObjeto>(RutaChatarra);
            Assert.IsNotNull(brazo);
            Assert.IsNotNull(chatarra);
            Assert.IsNotNull(cartera, "Hay cartera");
            Assert.IsNotNull(inventario, "Hay inventario");
            Assert.AreEqual(DineroInicial, cartera.Dinero, "Se empieza con $500");
            yield return null;
        }

        private static float DistanciaPlana(Vector3 a, Vector3 b)
        {
            return Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));
        }

        // Pone al jugador a 4 m al sur del mostrador y camina con W hasta quedar a 2 m; luego deja pasar cuadros para el HUD.
        private IEnumerator IrAlMostradorConW()
        {
            Vector3 punto = mostrador.transform.position + new Vector3(0f, AlturaInicio, -DistanciaDeInicio);
            jugador.Colocar(new Pose(punto, Quaternion.identity));
            yield return Cuadros(FramesParaElHud);
            Press(teclado.wKey);
            float fin = Time.time + MaxSegundosCaminando;
            while (DistanciaPlana(jugador.Raiz.position, mostrador.transform.position) > DistanciaDeUso && Time.time < fin)
            {
                yield return null;
            }
            Release(teclado.wKey);
            yield return null;
            Assert.LessOrEqual(DistanciaPlana(jugador.Raiz.position, mostrador.transform.position), DistanciaDeUso + 0.2f, "W llevó al jugador al mostrador");
            yield return Cuadros(FramesParaElHud);
        }

        private IEnumerator Tecla(KeyControl tecla)
        {
            Press(tecla);
            yield return null;
            Release(tecla);
            yield return null;
        }

        private IEnumerator Cuadros(int cuantos)
        {
            for (int i = 0; i < cuantos; i++)
            {
                yield return null;
            }
        }

        private IEnumerator AbrirPantalla()
        {
            Assert.IsTrue(mostrador.PuedeUsar, "El mostrador se ofrece: " + mostrador.Texto);
            yield return Tecla(teclado.eKey);
            Assert.IsTrue(pantalla.Visible, "E abre la pantalla de la tienda");
            Assert.IsTrue(mostrador.PantallaAbierta);
        }

        private void PonerDinero(int objetivo)
        {
            int diferencia = objetivo - cartera.Dinero;
            if (diferencia > 0)
            {
                cartera.Abonar(diferencia, MotivoDinero.Otro);
            }
            else if (diferencia < 0)
            {
                cartera.CargarDeuda(-diferencia, MotivoDinero.Otro);
            }
            Assert.AreEqual(objetivo, cartera.Dinero, "Dinero de salida");
        }

        private bool Capturando => Environment.GetEnvironmentVariable(VariableCaptura) == "1";

        // Captura de lo que ve el jugador con la pantalla (Overlay): se pasa un instante a ScreenSpaceCamera y se devuelve.
        private void CapturarCabina(string ruta)
        {
            if (!Capturando)
            {
                return;
            }
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
            RenderTexture rt = new RenderTexture(AnchoCaptura, AltoCaptura, ProfundidadCaptura);
            Texture2D imagen = new Texture2D(AnchoCaptura, AltoCaptura, TextureFormat.RGB24, false);
            RenderTexture anterior = RenderTexture.active;
            camara.targetTexture = rt;
            camara.Render();
            RenderTexture.active = rt;
            imagen.ReadPixels(new Rect(0f, 0f, AnchoCaptura, AltoCaptura), 0, 0);
            imagen.Apply();
            camara.targetTexture = null;
            RenderTexture.active = anterior;
            foreach (Canvas lienzo in movidos)
            {
                lienzo.renderMode = RenderMode.ScreenSpaceOverlay;
            }

            float minimo = 1f;
            float maximo = 0f;
            Color32[] pixeles = imagen.GetPixels32();
            for (int i = 0; i < pixeles.Length; i += SaltoDePixeles)
            {
                float brillo = pixeles[i].g / 255f;
                minimo = Mathf.Min(minimo, brillo);
                maximo = Mathf.Max(maximo, brillo);
            }
            string completa = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ruta));
            Directory.CreateDirectory(Path.GetDirectoryName(completa));
            File.WriteAllBytes(completa, imagen.EncodeToPNG());
            Object.Destroy(rt);
            Object.Destroy(imagen);
            Assert.Greater(maximo - minimo, VariacionMinima, "La captura es plana: no se ve nada");
        }


        // ---- Pruebas ----

        [UnityTest]
        public IEnumerator Vender_DosBrazosUnKitYCienPesos_VendeDeUnoEnUnoYElKitSeQueda()
        {
            yield return CargarEscena();
            PonerDinero(DineroPoco);
            Assert.AreEqual(2, inventario.Agregar(brazo, 2));
            Assert.AreEqual(1, inventario.Agregar(kit, 1));
            yield return IrAlMostradorConW();
            yield return AbrirPantalla();
            Assert.IsTrue(pantalla.PuedeVender, "El mercado negro compra");

            yield return Tecla(teclado.rightArrowKey);
            Assert.AreEqual(PestanaTienda.Vender, pantalla.Pestana, "Flecha derecha pasa a VENDER");
            Assert.AreEqual(1, pantalla.CantidadFilas, "Solo aparece el brazo (el kit no se compra)");
            StringAssert.Contains(TextoBrazo, pantalla.TextoFila(0));
            StringAssert.Contains("$300 c/u", pantalla.TextoFila(0));
            StringAssert.Contains("TIENES 2", pantalla.TextoFila(0));
            StringAssert.DoesNotContain("KIT", pantalla.TextoFila(0));
            yield return Cuadros(FramesParaElHud);
            CapturarCabina(RutaCaptura);

            yield return Tecla(teclado.enterKey);
            Assert.AreEqual(DineroPoco + PagoBrazo, cartera.Dinero, "Cobra $300");
            Assert.AreEqual(1, inventario.Cantidad(brazo), "Queda un brazo");
            Assert.AreEqual("TIENES $400", pantalla.Dinero, "El dinero de arriba se actualiza al instante");
            StringAssert.Contains("TIENES 1", pantalla.TextoFila(0));
            yield return Cuadros(FramesParaElHud);
            Assert.AreEqual(TextoHudVenta, hud.TextoAvisoDinero.text, "El HUD a pie anuncia la venta");

            yield return Tecla(teclado.enterKey);
            Assert.AreEqual(DineroPoco + 2 * PagoBrazo, cartera.Dinero, "Segundo brazo: $700");
            Assert.AreEqual(0, inventario.Cantidad(brazo), "Sin brazos");
            Assert.AreEqual(1, pantalla.CantidadFilas);
            Assert.AreEqual(TextoNada, pantalla.TextoFila(0), "Ya no tiene nada que le interese");
            Assert.AreEqual(1, inventario.Cantidad(kit), "El kit sigue en el inventario");

            yield return Tecla(teclado.enterKey);
            Assert.AreEqual(DineroPoco + 2 * PagoBrazo, cartera.Dinero, "Enter sin nada no paga");
            Assert.AreEqual(1, inventario.Cantidad(kit));
            Assert.IsEmpty(errores, "Sin errores: " + string.Join(" | ", errores));
        }

        [UnityTest]
        public IEnumerator Vender_Chatarra_PagaCincuentaYLaFlechaCambiaDeFila()
        {
            yield return CargarEscena();
            inventario.Agregar(brazo, 1);
            inventario.Agregar(chatarra, 3);
            yield return IrAlMostradorConW();
            yield return AbrirPantalla();
            yield return Tecla(teclado.rightArrowKey);
            Assert.AreEqual(2, pantalla.CantidadFilas, "Brazo y chatarra");

            yield return Tecla(teclado.downArrowKey);
            Assert.AreEqual(1, pantalla.IndiceElegido, "Flecha abajo baja a la chatarra");
            yield return Tecla(teclado.enterKey);
            Assert.AreEqual(DineroInicial + 50, cartera.Dinero, "La chatarra paga $50");
            Assert.AreEqual(2, inventario.Cantidad(chatarra));
            Assert.AreEqual(1, inventario.Cantidad(brazo), "El brazo no se toca");
            Assert.IsEmpty(errores, "Sin errores: " + string.Join(" | ", errores));
        }

        [UnityTest]
        public IEnumerator Vender_SinNada_DiceNoTienesNadaYEsc_CierraYElJugadorVuelveAMoverse()
        {
            yield return CargarEscena();
            yield return IrAlMostradorConW();
            yield return AbrirPantalla();
            yield return Tecla(teclado.rightArrowKey);
            Assert.AreEqual(TextoNada, pantalla.TextoFila(0));
            yield return Tecla(teclado.enterKey);
            Assert.AreEqual(DineroInicial, cartera.Dinero, "No hay nada que cobrar");

            yield return Tecla(teclado.escapeKey);
            yield return Cuadros(FramesParaElHud);
            Assert.IsFalse(pantalla.Visible, "Esc cierra");
            Assert.IsTrue(controlador.ControlActivo, "El control vuelve");
            Vector3 antes = jugador.Raiz.position;
            Press(teclado.wKey);
            yield return new WaitForSeconds(SegundosCaminando);
            Release(teclado.wKey);
            yield return null;
            Assert.GreaterOrEqual(DistanciaPlana(antes, jugador.Raiz.position), MetrosMinimosAlMoverse, "Con W vuelve a moverse");
            Assert.IsFalse(pantalla.Visible, "La E que abrio no reabre la pantalla");
            Assert.IsEmpty(errores, "Sin errores: " + string.Join(" | ", errores));
        }

        [UnityTest]
        public IEnumerator Vender_MercadoCerrado_NoAbreLaPantallaYNoSeVende()
        {
            yield return CargarEscena();
            inventario.Agregar(brazo, 1);
            LugarDePrueba lugar = null;
            foreach (ILugar candidato in Registro<ILugar>.Todos)
            {
                if (candidato.Id == IdLugar.MercadoNegro)
                {
                    lugar = candidato as LugarDePrueba;
                }
            }
            Assert.IsNotNull(lugar, "La escena trae el lugar MercadoNegro de prueba");
            lugar.Configurar(IdLugar.MercadoNegro, "Mercado negro", new HorarioLugar(HoraAbre, HoraCierra), false);
            lugar.PonerAbiertoPorHorario(false);

            yield return IrAlMostradorConW();
            Assert.AreEqual(TextoCerrado, hud.TextoAvisoInteraccion.text, "El aviso de cerrado sigue");
            yield return Tecla(teclado.eKey);
            Assert.IsFalse(pantalla.Visible, "E no abre nada");
            Assert.AreEqual(1, inventario.Cantidad(brazo), "No se vendio nada");
            Assert.AreEqual(DineroInicial, cartera.Dinero);
            Assert.IsEmpty(errores, "Sin errores: " + string.Join(" | ", errores));
        }

        [UnityTest]
        public IEnumerator Vender_PantallaSinTiposQueComprar_NoMuestraLaPestanaVender()
        {
            yield return CargarEscena();
            inventario.Agregar(brazo, 1);
            yield return IrAlMostradorConW();
            pantalla.Mostrar(mostrador.Tienda, mostrador.EnVenta, null, new TipoObjeto[0]);
            Assert.IsFalse(pantalla.PuedeVender);
            yield return Tecla(teclado.rightArrowKey);
            Assert.AreEqual(PestanaTienda.Comprar, pantalla.Pestana, "Flecha derecha no hace nada sin VENDER");
            pantalla.Ocultar();
            Assert.IsEmpty(errores, "Sin errores: " + string.Join(" | ", errores));
        }
    }
}
