using System;
using System.Collections;
using System.IO;
using MechaCyberpunk.Core;
using MechaCyberpunk.Economia;
using MechaCyberpunk.Pelea.Datos;
using MechaCyberpunk.Pelea.Flujo;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Personaje.Movimiento;
using MechaCyberpunk.Progresion;
using MechaCyberpunk.Progresion.Guardado;
using MechaCyberpunk.Progresion.Prologo;
using MechaCyberpunk.Progresion.Pruebas;
using MechaCyberpunk.UI.Peleas;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Tests.Progresion
{
    /// <summary>
    /// T-160: la cartelera del mostrador de peleas de Pruebas_Economia (de noche, historia en Libre). Entrada simulada (E, flechas,
    /// Enter, Esc, WASD, ratón); lo único que no es entrada es dejar el progreso de las ligas en un punto con <c>Poner</c>.
    /// Los textos y los premios salen de los assets.
    /// </summary>
    public class CarteleraMostradorPruebas : InputTestFixture
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Tony/Pruebas_Economia.unity";
        private const string RutaCalendario = "Assets/_Proyecto/Pelea/Datos/CalendarioLigas_Base.asset";
        private const string RutaPremioCasera = "Assets/_Proyecto/Economia/Datos/DatosPremioPelea_Casera.asset";
        private const string AliasPrimero = "TORNILLO";
        private const string AliasJefe = "DON FIERRO";
        private const string AliasCasera = "EL CHATARRAS";
        private const string TextoEncabezado = "PELEAS DEL CLANDESTINO · LIGA SÓTANO · CAMPEONES 0/4";
        private const string TextoAyuda = "↑ ↓ ELEGIR · ENTER PELEAR · ESC SALIR";
        private const string TextoMostrador = "Peleas del clandestino";
        private const int DineroBase = 500;
        private const int FramesEstables = 10;
        private const int MaxFramesCalentamiento = 1000;
        private const int FramesParaElHud = 5;
        private const int FramesDeEspera = 6;
        private const float DeltaTimeEstable = 0.05f;
        private const float SegundosCaminando = 0.6f;
        private const float MetrosMinimosAlMoverse = 0.5f;
        private const float MetrosDeMargen = 0.01f;
        private const float GradosDeMargen = 0.01f;
        private const float DeltaRatonGrande = 400f;
        private const float DistanciaAlMostrador = 2f;
        private const float AlturaInicio = 0.05f;
        private const string VariableCaptura = "CAPTURAR_T160";
        private const string RutaCaptura1 = "docs/capturas/T-160-1.png";
        private const string RutaCaptura2 = "docs/capturas/T-160-2.png";
        private const int AnchoCaptura = 1280;
        private const int AltoCaptura = 720;
        private const int ProfundidadCaptura = 24;
        private const int SaltoDePixeles = 97;
        private const float VariacionMinima = 0.05f;
        private const float DistanciaPlanoCanvas = 1f;

        private Keyboard teclado;
        private Mouse raton;
        private string carpeta;
        private JugadorAPie jugador;
        private ControladorAPie controlador;
        private MostradorPeleas mostrador;
        private EntradaArena entrada;
        private DirectorPelea director;
        private PantallaApuesta pantalla;
        private PantallaCartelera cartelera;
        private ICartera cartera;
        private ServicioLigas ligas;
        private CalendarioLigas calendario;
        private DatosPremioPelea premioCasera;
        private readonly System.Collections.Generic.List<string> errores = new System.Collections.Generic.List<string>();
        private System.Collections.Generic.HashSet<GameObject> raicesIniciales;

        public override void Setup()
        {
            raicesIniciales = RaicesDePrueba.Anotar();
            InputSystem.DisableAllEnabledActions();
            base.Setup();
            Registros.LimpiarTodos();
            EventosPelea.LimpiarSuscriptores();
            teclado = InputSystem.AddDevice<Keyboard>();
            raton = InputSystem.AddDevice<Mouse>();
            MapasDeAccionesDePrueba.Reiniciar(teclado, raton);
            carpeta = Path.Combine(Application.temporaryCachePath, "cartelera_mostrador_" + Guid.NewGuid().ToString("N"));
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
            EventosPelea.LimpiarSuscriptores();
            Registros.LimpiarTodos();
            MapasDeAccionesDePrueba.AntesDelTearDown(teclado, raton);
            base.TearDown();
            MapasDeAccionesDePrueba.DespuesDelTearDown();
        }

        [UnityTearDown]
        public IEnumerator DescargarEscena()
        {
            RaicesDePrueba.DestruirNuevas(raicesIniciales);
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
            mostrador = Object.FindFirstObjectByType<MostradorPeleas>();
            Assert.IsNotNull(jugador, "Pruebas_Economia trae al jugador");
            Assert.IsNotNull(mostrador, "Pruebas_Economia trae el mostrador de peleas");
            controlador = jugador.GetComponent<ControladorAPie>();
            entrada = mostrador.GetComponent<EntradaArena>();
            director = mostrador.GetComponentInChildren<DirectorPelea>(true);
            pantalla = mostrador.GetComponentInChildren<PantallaApuesta>(true);
            cartelera = mostrador.GetComponentInChildren<PantallaCartelera>(true);
            cartera = Registro<ICartera>.Actual;
            ligas = Registro<ServicioLigas>.Actual;
            calendario = AssetDatabase.LoadAssetAtPath<CalendarioLigas>(RutaCalendario);
            premioCasera = AssetDatabase.LoadAssetAtPath<DatosPremioPelea>(RutaPremioCasera);
            Assert.IsNotNull(cartelera, "El mostrador trae su cartelera");
            Assert.IsNotNull(cartera, "Hay cartera");
            Assert.IsNotNull(ligas, "Hay ServicioLigas");
            Assert.IsNotNull(calendario, "Existe el calendario");
            Assert.IsNotNull(premioCasera, "Existe el premio de la casera");
            Assert.AreEqual(DineroBase, cartera.Dinero, "Se empieza con $500");
            Assert.AreEqual(EtapaPrologo.Libre, Registro<IHistoria>.Actual.Etapa, "La escena arranca en Libre");
            yield return null;
        }

        private IEnumerator IrAlMostrador()
        {
            Vector3 posicion = mostrador.transform.position + new Vector3(0f, AlturaInicio, -DistanciaAlMostrador);
            jugador.Colocar(new Pose(posicion, Quaternion.identity));
            for (int i = 0; i < FramesParaElHud; i++)
            {
                yield return null;
            }
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

        private IEnumerator AbrirCartelera()
        {
            Assert.IsTrue(mostrador.PuedeUsar, "El mostrador ofrece la cartelera: " + mostrador.Texto);
            yield return Tecla(teclado.eKey);
            Assert.IsTrue(cartelera.Visible, "E abre la cartelera");
            Assert.IsTrue(mostrador.CarteleraAbierta, "El mostrador la tiene abierta");
            Assert.IsFalse(pantalla.Visible, "Todavía no se ve la apuesta");
            Assert.IsFalse(entrada.EnCurso, "Todavía no se pelea");
        }

        private static float DistanciaPlana(Vector3 a, Vector3 b) => Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));

        private bool Capturando => Environment.GetEnvironmentVariable(VariableCaptura) == "1";

        // Captura de lo que ve el jugador con la cartelera (Overlay): se pasa un instante a ScreenSpaceCamera y se devuelve.
        private void Capturar(string ruta)
        {
            if (!Capturando)
            {
                return;
            }
            Camera camara = jugador.Camara;
            System.Collections.Generic.List<Canvas> movidos = new System.Collections.Generic.List<Canvas>();
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
        public IEnumerator Abrir_DeNoche_MuestraEncabezadoYLasTresFilas()
        {
            yield return CargarEscena();
            yield return IrAlMostrador();
            Assert.AreEqual(TextoMostrador, mostrador.Texto, "El aviso del mostrador es el de la cartelera");

            yield return AbrirCartelera();
            CartelPelea cartel = calendario.Cartel(1, 1);
            Assert.AreEqual(TextoEncabezado, cartelera.Encabezado, "Dice liga y campeones");
            Assert.AreEqual(TextoAyuda, cartelera.Ayuda, "Dice las teclas");
            Assert.AreEqual(3, cartelera.CantidadFilas, "Siguiente, casera y la liga que sigue bloqueada");
            Assert.AreEqual("LIGA SÓTANO 1/5 · " + AliasPrimero + " · " + FormatoDinero.Texto(cartel.premioGanar), cartelera.TextoBaseFila(0));
            Assert.AreEqual("PELEA CASERA · " + AliasCasera + " · " + FormatoDinero.Texto(premioCasera.PremioGanar), cartelera.TextoBaseFila(1));
            Assert.AreEqual("LIGA CALLE · BLOQUEADA · VENCE A " + AliasJefe, cartelera.TextoBaseFila(2));
            Assert.AreEqual(0, cartelera.IndiceElegido, "Empieza en la primera pelea");
            Assert.AreEqual(cartelera.ColorElegido, cartelera.ColorFila(0), "La elegida va en neón");
            Assert.AreEqual(cartelera.ColorNormal, cartelera.ColorFila(1), "La otra va normal");
            Assert.AreEqual(cartelera.ColorBloqueada, cartelera.ColorFila(2), "La bloqueada va en gris");
            Assert.AreEqual(DineroBase, cartera.Dinero, "Abrir no cobra");
            yield return Cuadros(FramesParaElHud);
            Capturar(RutaCaptura1);
            Assert.IsEmpty(errores, "Consola sin errores: " + string.Join(" | ", errores));
        }

        [UnityTest]
        public IEnumerator Flechas_NoDanLaVueltaYNoEligenLaBloqueada()
        {
            yield return CargarEscena();
            yield return IrAlMostrador();
            yield return AbrirCartelera();

            yield return Tecla(teclado.upArrowKey);
            Assert.AreEqual(0, cartelera.IndiceElegido, "Arriba de la primera no hay nada");
            yield return Tecla(teclado.downArrowKey);
            Assert.AreEqual(1, cartelera.IndiceElegido, "↓ elige la casera");
            Assert.AreEqual(cartelera.ColorElegido, cartelera.ColorFila(1));
            yield return Tecla(teclado.downArrowKey);
            Assert.AreEqual(1, cartelera.IndiceElegido, "La bloqueada no se elige");
            Assert.AreEqual(cartelera.ColorBloqueada, cartelera.ColorFila(2), "Y sigue en gris");
            yield return Tecla(teclado.upArrowKey);
            Assert.AreEqual(0, cartelera.IndiceElegido, "↑ vuelve a la primera");
            Assert.IsEmpty(errores, "Consola sin errores: " + string.Join(" | ", errores));
        }

        [UnityTest]
        public IEnumerator Enter_ConLaPrimera_AbreLaApuestaConElRivalYElPremioDelCartel()
        {
            yield return CargarEscena();
            yield return IrAlMostrador();
            yield return AbrirCartelera();
            CartelPelea cartel = calendario.Cartel(1, 1);

            yield return Tecla(teclado.enterKey);
            Assert.IsFalse(cartelera.Visible, "Enter cierra la cartelera");
            Assert.IsTrue(pantalla.Visible, "Y abre la apuesta de siempre");
            Assert.IsTrue(mostrador.ApuestaAbierta);
            Assert.AreEqual("CONTRA " + AliasPrimero, pantalla.Rival, "Contra el siguiente rival");
            StringAssert.Contains(FormatoDinero.Texto(cartel.premioGanar), pantalla.Premio, "Con el premio de su cartel");
            Assert.IsFalse(entrada.EnCurso, "Todavía no se pelea: falta la apuesta");

            yield return Tecla(teclado.escapeKey);
            yield return Cuadros(FramesDeEspera);
            Assert.IsFalse(pantalla.Visible, "Esc en la apuesta la cierra");
            Assert.IsFalse(cartelera.Visible, "Y la cartelera no reaparece");
            Assert.IsFalse(mostrador.ApuestaAbierta);
            Assert.IsFalse(mostrador.CarteleraAbierta);
            Assert.IsFalse(entrada.EnCurso, "No pelea");
            Assert.AreEqual(DineroBase, cartera.Dinero, "Sin cobrar");
            Assert.IsNull(director.CartelActual, "El cartel se quitó");
            Assert.IsTrue(controlador.ControlActivo, "El jugador recupera el control");
            Assert.IsEmpty(errores, "Consola sin errores: " + string.Join(" | ", errores));
        }

        [UnityTest]
        public IEnumerator Enter_ConLaCasera_AbreLaApuestaDeElChatarrasConSuPremio()
        {
            yield return CargarEscena();
            yield return IrAlMostrador();
            yield return AbrirCartelera();

            yield return Tecla(teclado.downArrowKey);
            yield return Tecla(teclado.enterKey);
            Assert.IsFalse(cartelera.Visible, "Enter cierra la cartelera");
            Assert.IsTrue(pantalla.Visible, "Abre la apuesta");
            Assert.AreEqual("CONTRA " + AliasCasera, pantalla.Rival, "Contra el rival casero");
            StringAssert.Contains(FormatoDinero.Texto(premioCasera.PremioGanar), pantalla.Premio, "Con el premio de la casera");
            StringAssert.Contains("$600", pantalla.Premio);

            yield return Tecla(teclado.escapeKey);
            yield return Cuadros(FramesDeEspera);
            Assert.IsFalse(pantalla.Visible);
            Assert.AreEqual(DineroBase, cartera.Dinero, "Sin cobrar");
            Assert.IsEmpty(errores, "Consola sin errores: " + string.Join(" | ", errores));
        }

        [UnityTest]
        public IEnumerator Jefe_EsLaPrimeraFilaEnColorDeJefe()
        {
            yield return CargarEscena();
            Assert.IsTrue(ligas.Poner(1, 5, 0));
            yield return IrAlMostrador();
            yield return AbrirCartelera();
            CartelPelea cartel = calendario.Cartel(1, 5);

            Assert.AreEqual("JEFE DE LA LIGA SÓTANO · " + AliasJefe + " · " + FormatoDinero.Texto(cartel.premioGanar), cartelera.TextoBaseFila(0));
            StringAssert.Contains("$2,000", cartelera.TextoBaseFila(0));
            Assert.AreEqual(cartelera.ColorJefe, cartelera.ColorFila(0), "El jefe va en su color aunque sea la elegida");
            Assert.AreNotEqual(cartelera.ColorElegido, cartelera.ColorJefe);
            StringAssert.StartsWith("REVANCHA · ", cartelera.TextoBaseFila(1), "Detrás vienen las revanchas");
            Assert.AreEqual(7, cartelera.CantidadFilas, "Jefe, cuatro revanchas, casera y la liga bloqueada");
            yield return Cuadros(FramesParaElHud);
            Capturar(RutaCaptura2);
            Assert.IsEmpty(errores, "Consola sin errores: " + string.Join(" | ", errores));
        }

        [UnityTest]
        public IEnumerator Revanchas_PaganLaMitadDelPremio()
        {
            yield return CargarEscena();
            Assert.IsTrue(ligas.Poner(1, 3, 0));
            yield return IrAlMostrador();
            yield return AbrirCartelera();

            Assert.AreEqual(5, cartelera.CantidadFilas, "Siguiente, dos revanchas, casera y la liga bloqueada");
            StringAssert.StartsWith("LIGA SÓTANO 3/5 · ", cartelera.TextoBaseFila(0));
            for (int indice = 1; indice <= 2; indice++)
            {
                CartelPelea cartel = calendario.Cartel(1, indice);
                int esperado = Mathf.RoundToInt(cartel.premioGanar * ligas.Datos.FraccionPremioRevancha);
                StringAssert.StartsWith("REVANCHA · ", cartelera.TextoBaseFila(indice));
                StringAssert.EndsWith(FormatoDinero.Texto(esperado), cartelera.TextoBaseFila(indice), "La revancha paga la fracción del premio");
                Assert.Less(esperado, cartel.premioGanar);
            }
            Assert.IsEmpty(errores, "Consola sin errores: " + string.Join(" | ", errores));
        }

        [UnityTest]
        public IEnumerator Abierta_ElJugadorNoSeMueveNiMiraYAlSalirConEscVuelveElControl()
        {
            yield return CargarEscena();
            yield return IrAlMostrador();
            Quaternion giroInicial = jugador.Raiz.rotation;
            Vector3 posicionInicial = jugador.Raiz.position;

            // La E que abre sigue pulsada cuando llega Esc: al cerrar no debe reabrirse.
            Press(teclado.eKey);
            yield return null;
            Assert.IsTrue(cartelera.Visible, "E abre la cartelera");
            Assert.IsFalse(controlador.ControlActivo, "El jugador queda congelado mientras se ve");

            Press(teclado.wKey);
            Set(raton.delta, new Vector2(DeltaRatonGrande, 0f));
            float fin = Time.time + SegundosCaminando;
            while (Time.time < fin)
            {
                yield return null;
            }
            Set(raton.delta, Vector2.zero);
            Release(teclado.wKey);
            yield return null;
            Assert.Less(DistanciaPlana(posicionInicial, jugador.Raiz.position), MetrosDeMargen, "Con W no se mueve");
            Assert.Less(Quaternion.Angle(giroInicial, jugador.Raiz.rotation), GradosDeMargen, "Con el ratón no gira");
            Assert.IsTrue(cartelera.Visible, "Sigue abierta");

            Press(teclado.escapeKey);
            yield return null;
            Release(teclado.escapeKey);
            Release(teclado.eKey);
            yield return Cuadros(FramesDeEspera);
            Assert.IsFalse(cartelera.Visible, "Esc cierra la cartelera");
            Assert.IsFalse(pantalla.Visible, "Y no abre la apuesta");
            Assert.IsFalse(mostrador.CarteleraAbierta);
            Assert.IsFalse(entrada.EnCurso, "No pelea");
            Assert.AreEqual(DineroBase, cartera.Dinero, "Sin cobrar");
            Assert.IsTrue(controlador.ControlActivo, "El jugador recupera el control");

            Vector3 antes = jugador.Raiz.position;
            Press(teclado.wKey);
            fin = Time.time + SegundosCaminando;
            while (Time.time < fin)
            {
                yield return null;
            }
            Release(teclado.wKey);
            yield return null;
            Assert.Greater(DistanciaPlana(antes, jugador.Raiz.position), MetrosMinimosAlMoverse, "Con W el jugador vuelve a moverse");
            Assert.IsFalse(cartelera.Visible, "La E que abrió no reabrió la cartelera");

            // Se puede volver a abrir (el estado quedó limpio).
            yield return IrAlMostrador();
            yield return AbrirCartelera();
            yield return Tecla(teclado.escapeKey);
            Assert.IsFalse(cartelera.Visible, "Otra vez Esc cierra");
            Assert.IsEmpty(errores, "Consola sin errores: " + string.Join(" | ", errores));
        }
    }
}
