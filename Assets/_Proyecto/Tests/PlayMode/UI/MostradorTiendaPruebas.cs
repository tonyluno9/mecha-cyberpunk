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
    /// T-146: comprar kits en el mostrador del mercado negro de Pruebas_Economia. Todo con entrada simulada (W, E, flechas, Enter, Esc);
    /// lo único que no es entrada es dejar el dinero de salida y cerrar el lugar de prueba.
    /// </summary>
    public class MostradorTiendaPruebas : InputTestFixture
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Tony/Pruebas_Economia.unity";
        private const string RutaKit = "Assets/_Proyecto/Economia/Datos/DatosObjeto_KitReparacion.asset";
        private const string TextoHudAbierto = "E · El Mercado";
        private const string TextoCerrado = "CERRADO · abre 09:00";
        private const string TextoKit = "KIT DE REPARACIÓN";
        private const string TextoPrecio = "Ƀ250";
        private const string TextoLleva0 = "LLEVAS 0/3";
        private const string TextoMaximo = "LLEVAS EL MÁXIMO";
        private const string TextoNoAlcanza = "NO TE ALCANZA";
        private const string TextoNada = "NO TIENES NADA QUE LE INTERESE";
        private const int DineroInicial = 500;
        private const int DineroDeSobra = 1000;
        private const int DineroPoco = 100;
        private const int DineroDeuda = -200;
        private const int PrecioKit = 250;
        private const int FilasNoArmas = 5;
        private const int FilasDeArmas = 5; // T-416: de cada arma solo se ofrece la siguiente (escopeta, fusil, sniper, PISTOLA I y PUÑOS I)
        private const int FilasDelMercado = FilasNoArmas + FilasDeArmas;
        private const int OfertasDelMercado = 6 + 19;
        private const int MaximoMisiles = 6;
        private const int MaximoKits = 3;
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
        private const string VariableCaptura = "CAPTURAR_T146";
        private const string RutaCaptura = "docs/capturas/T-146-1.png";
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
        private readonly List<string> errores = new List<string>();

        public override void Setup()
        {
            InputSystem.DisableAllEnabledActions();
            base.Setup();
            Registros.LimpiarTodos();
            teclado = InputSystem.AddDevice<Keyboard>();
            raton = InputSystem.AddDevice<Mouse>();
            MapasDeAccionesDePrueba.Reiniciar(teclado, raton);
            carpeta = Path.Combine(Application.temporaryCachePath, "mostrador_tienda_" + Guid.NewGuid().ToString("N"));
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
            Assert.IsNotNull(cartera, "Hay cartera");
            Assert.IsNotNull(inventario, "Hay inventario");
            Assert.AreEqual(DineroInicial, cartera.Dinero, "Se empieza con Ƀ500");
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
        public IEnumerator Comprar_ConDinero_TresKitsYElCuartoDiceMaximo()
        {
            yield return CargarEscena();
            PonerDinero(DineroDeSobra);
            yield return IrAlMostradorConW();

            Assert.AreEqual(TextoHudAbierto, hud.TextoAvisoInteraccion.text, "El HUD ofrece el mercado negro");
            yield return AbrirPantalla();
            Assert.AreEqual("EL MERCADO", pantalla.Titulo, "Dice qué tienda es");
            Assert.AreEqual(FilasDelMercado, pantalla.CantidadFilas, "Vende el kit y las 4 municiones");
            StringAssert.Contains(TextoKit, pantalla.TextoFila(0), "Lista el kit de reparación");
            StringAssert.Contains(TextoPrecio, pantalla.TextoFila(0), "A Ƀ250");
            StringAssert.Contains(TextoLleva0, pantalla.TextoFila(0), "No lleva ninguno");
            Assert.AreEqual("TIENES Ƀ1,000", pantalla.Dinero, "Muestra el dinero de arriba");
            Assert.AreEqual(PestanaTienda.Comprar, pantalla.Pestana, "Abre en COMPRAR");

            yield return Tecla(teclado.enterKey);
            Assert.AreEqual(DineroDeSobra - PrecioKit, cartera.Dinero, "Cobra Ƀ250");
            Assert.AreEqual(1, inventario.Cantidad(kit), "El kit entra al inventario");
            StringAssert.Contains("LLEVAS 1/3", pantalla.TextoFila(0), "La fila se actualiza");
            Assert.AreEqual("TIENES Ƀ750", pantalla.Dinero, "El dinero se actualiza");
            yield return Cuadros(FramesParaElHud);
            CapturarCabina(RutaCaptura);

            yield return Tecla(teclado.enterKey);
            yield return Tecla(teclado.enterKey);
            Assert.AreEqual(DineroDeSobra - PrecioKit * MaximoKits, cartera.Dinero, "Tres kits cuestan Ƀ750");
            Assert.AreEqual(MaximoKits, inventario.Cantidad(kit), "Lleva tres kits");

            yield return Tecla(teclado.enterKey);
            Assert.AreEqual(TextoMaximo, pantalla.Aviso, "El cuarto Enter dice que lleva el máximo");
            Assert.AreEqual(DineroDeSobra - PrecioKit * MaximoKits, cartera.Dinero, "No se cobra el cuarto");
            Assert.AreEqual(MaximoKits, inventario.Cantidad(kit), "Siguen siendo tres");
            Assert.IsEmpty(errores, "Sin errores: " + string.Join(" | ", errores));
        }

        // T-177: municiones del mercado negro: precio de la oferta y cuántas caben (el máximo de su objeto).
        private static readonly string[] NombresMunicion = { "MISIL", "CARGAS", "CELDAS", "COMBUSTIBLE" };
        private static readonly int[] PreciosMunicion = { 80, 50, 40, 60 };

        private int FilaCon(string texto)
        {
            for (int i = 0; i < pantalla.CantidadFilas; i++)
            {
                if (pantalla.TextoFila(i).ToUpperInvariant().Contains(texto))
                {
                    return i;
                }
            }
            Assert.Fail("No hay fila con '" + texto + "'");
            return -1;
        }

        [UnityTest]
        public IEnumerator Municiones_ElMercadoListaLas4ConSuPrecio()
        {
            yield return CargarEscena();
            PonerDinero(DineroDeSobra);
            yield return IrAlMostradorConW();
            yield return AbrirPantalla();

            for (int i = 0; i < NombresMunicion.Length; i++)
            {
                int fila = FilaCon(NombresMunicion[i]);
                StringAssert.Contains("Ƀ" + PreciosMunicion[i], pantalla.TextoFila(fila), NombresMunicion[i] + " con su precio");
            }
            Assert.IsEmpty(errores, "Sin errores: " + string.Join(" | ", errores));
        }

        [UnityTest]
        public IEnumerator Municiones_MisilesHastaSeisCobraCadaUnoYElSieteAvisaElMaximo()
        {
            yield return CargarEscena();
            PonerDinero(DineroDeSobra);
            yield return IrAlMostradorConW();
            yield return AbrirPantalla();
            int fila = FilaCon("MISIL");
            for (int i = 0; i < fila; i++)
            {
                yield return Tecla(teclado.downArrowKey);
            }
            Assert.AreEqual(fila, pantalla.IndiceElegido, "Eligió la fila de los misiles");
            DatosObjeto misil = null;
            foreach (DatosObjeto objeto in mostrador.EnVenta)
            {
                if (objeto != null && objeto.Id == "municion_misil") misil = objeto;
            }
            Assert.IsNotNull(misil, "Los misiles están a la venta");

            for (int comprados = 1; comprados <= MaximoMisiles; comprados++)
            {
                yield return Tecla(teclado.enterKey);
                Assert.AreEqual(DineroDeSobra - PreciosMunicion[0] * comprados, cartera.Dinero, "Cobra cada misil: " + comprados);
                Assert.AreEqual(comprados, inventario.Cantidad(misil), "Entra al inventario: " + comprados);
            }

            yield return Tecla(teclado.enterKey);
            Assert.AreEqual(TextoMaximo, pantalla.Aviso, "El séptimo avisa el máximo");
            Assert.AreEqual(DineroDeSobra - PreciosMunicion[0] * MaximoMisiles, cartera.Dinero, "No cobra el séptimo");
            Assert.AreEqual(MaximoMisiles, inventario.Cantidad(misil), "Siguen siendo seis");
            Assert.IsEmpty(errores, "Sin errores: " + string.Join(" | ", errores));
        }

        [UnityTest]
        public IEnumerator Comprar_SinDineroOConDeuda_DiceNoTeAlcanzaYNadaCambia()
        {
            yield return CargarEscena();
            PonerDinero(DineroPoco);
            yield return IrAlMostradorConW();
            yield return AbrirPantalla();

            yield return Tecla(teclado.enterKey);
            Assert.AreEqual(TextoNoAlcanza, pantalla.Aviso, "Con Ƀ100 no alcanza");
            Assert.AreEqual(DineroPoco, cartera.Dinero, "No se cobra");
            Assert.AreEqual(0, inventario.Cantidad(kit), "No entra nada");

            PonerDinero(DineroDeuda);
            yield return Tecla(teclado.enterKey);
            Assert.AreEqual(TextoNoAlcanza, pantalla.Aviso, "Con deuda tampoco");
            Assert.AreEqual(DineroDeuda, cartera.Dinero, "La deuda no cambia");
            Assert.AreEqual(0, inventario.Cantidad(kit), "No entra nada con deuda");
            Assert.IsEmpty(errores, "Sin errores: " + string.Join(" | ", errores));
        }

        [UnityTest]
        public IEnumerator Esc_CierraYElJugadorVuelveAMoverseSinReabrir()
        {
            yield return CargarEscena();
            yield return IrAlMostradorConW();
            yield return AbrirPantalla();
            Assert.IsFalse(controlador.ControlActivo, "Abierta, congela al jugador");

            Vector3 antes = jugador.Raiz.position;
            Press(teclado.wKey);
            yield return Cuadros(FramesParaElHud * 4);
            Release(teclado.wKey);
            yield return null;
            Assert.Less(DistanciaPlana(antes, jugador.Raiz.position), MetrosMinimosAlMoverse, "Con la pantalla abierta, W no mueve al jugador");

            yield return Tecla(teclado.rightArrowKey);
            Assert.AreEqual(PestanaTienda.Vender, pantalla.Pestana, "→ pasa a VENDER");
            StringAssert.Contains(TextoNada, pantalla.TextoFila(0), "VENDER sin nada dice que no le interesa");
            yield return Tecla(teclado.leftArrowKey);
            Assert.AreEqual(PestanaTienda.Comprar, pantalla.Pestana, "← vuelve a COMPRAR");

            yield return Tecla(teclado.escapeKey);
            yield return Cuadros(FramesParaElHud);
            Assert.IsFalse(pantalla.Visible, "Esc cierra la pantalla");
            Assert.IsFalse(mostrador.PantallaAbierta);
            Assert.IsTrue(controlador.ControlActivo, "El control vuelve");

            antes = jugador.Raiz.position;
            Press(teclado.wKey);
            yield return new WaitForSeconds(SegundosCaminando);
            Release(teclado.wKey);
            yield return null;
            Assert.GreaterOrEqual(DistanciaPlana(antes, jugador.Raiz.position), MetrosMinimosAlMoverse, "Con W el jugador vuelve a moverse");
            Assert.IsFalse(pantalla.Visible, "La E que abrió no reabre la pantalla");
            Assert.IsEmpty(errores, "Sin errores: " + string.Join(" | ", errores));
        }

        [UnityTest]
        public IEnumerator LugarCerrado_DiceCerradoYENoAbreNada()
        {
            yield return CargarEscena();
            LugarDePrueba lugar = null;
            foreach (ILugar candidato in Registro<ILugar>.Todos)
            {
                if (candidato.Id == IdLugar.MercadoNegro)
                {
                    lugar = candidato as LugarDePrueba;
                }
            }
            Assert.IsNotNull(lugar, "La escena trae el lugar MercadoNegro de prueba");
            lugar.Configurar(IdLugar.MercadoNegro, "El Mercado", new HorarioLugar(HoraAbre, HoraCierra), false);
            lugar.PonerAbiertoPorHorario(false);
            Assert.IsFalse(lugar.EstaAbierto, "El lugar está cerrado");

            yield return IrAlMostradorConW();
            Assert.AreEqual(TextoCerrado, hud.TextoAvisoInteraccion.text, "El aviso dice cuándo abre");
            Assert.IsFalse(mostrador.PuedeUsar, "Cerrado no se usa");

            yield return Tecla(teclado.eKey);
            Assert.IsFalse(pantalla.Visible, "E no abre nada");
            Assert.IsTrue(controlador.ControlActivo, "El jugador sigue libre");

            lugar.PonerAbiertoPorHorario(true);
            yield return Cuadros(FramesParaElHud);
            Assert.AreEqual(TextoHudAbierto, hud.TextoAvisoInteraccion.text, "Al abrir vuelve a ofrecerse");
            Assert.IsEmpty(errores, "Sin errores: " + string.Join(" | ", errores));
        }

        [UnityTest]
        public IEnumerator Registro_ElMostradorRegistraSuTiendaYListaElKitYLasMuniciones()
        {
            yield return CargarEscena();
            Assert.AreEqual(1, Registro<ITienda>.Cantidad, "Hay una tienda registrada");
            ITienda tienda = Registro<ITienda>.Actual;
            Assert.AreSame(mostrador.Tienda, tienda, "Es la del mostrador");
            Assert.AreEqual(IdLugar.MercadoNegro, tienda.Lugar, "Es la del mercado negro");
            Assert.AreEqual(OfertasDelMercado, tienda.Ofertas.Count, "La tienda vende kit, 4 municiones y el brazo");

            Assert.AreEqual(FilasNoArmas + 19, mostrador.EnVenta.Count, "El brazo no tiene objeto de inventario");
            Assert.AreSame(kit, mostrador.EnVenta[0]);
            yield return IrAlMostradorConW();
            yield return AbrirPantalla();
            Assert.AreEqual(FilasDelMercado, pantalla.CantidadFilas, "La pantalla lista el kit y las municiones");
            StringAssert.DoesNotContain("BRAZO", pantalla.TextoFila(0), "Sin brazo");

            Object.Destroy(mostrador.gameObject);
            yield return null;
            yield return null;
            Assert.AreEqual(0, Registro<ITienda>.Cantidad, "Al destruirlo se quita del registro");
            Assert.IsEmpty(errores, "Sin errores: " + string.Join(" | ", errores));
        }
    }
}
