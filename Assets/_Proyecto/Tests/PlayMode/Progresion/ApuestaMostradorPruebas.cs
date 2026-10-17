using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using MechaCyberpunk.Combate.Vida;
using MechaCyberpunk.Core;
using MechaCyberpunk.Economia;
using MechaCyberpunk.Mecha.Montaje;
using MechaCyberpunk.Pelea.Flujo;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Personaje.Movimiento;
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
    /// T-145: apostar en el mostrador de peleas de Pruebas_Economia. Todo con entrada simulada (W, ratón, E, flechas, Enter, Esc); lo único
    /// que no es entrada es dejar al jugador con poca vida o poco dinero de salida y rematar al rival con daño directo después de daño real.
    /// </summary>
    public class ApuestaMostradorPruebas : InputTestFixture
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Tony/Pruebas_Economia.unity";
        private const string RutaBrazo = "Assets/_Proyecto/Economia/Datos/DatosObjeto_BrazoChatarra.asset";
        private const string TextoRival = "CONTRA EL CHATARRAS";
        private const string TextoPremio = "PREMIO $600";
        private const string TextoMomio = "PAGA 1 A 1";
        private const string TextoPieza = "+ SU BRAZO DE CHATARRA si apuestas $250 o más";
        private const string TextoAyuda = "← → apuesta · Enter pelear · Esc salir";
        private const string TextoApuestaGanada = "APUESTA +$250";
        private const string TextoApuestaPerdida = "APUESTA −$250";
        private const int DineroInicial = 500;
        private const int Apuesta = 250;
        private const int DineroPocoAlcanza = 80;
        private const int DineroTrasGanar = 1350;
        private const int FramesEstables = 10;
        private const int MaxFramesCalentamiento = 1000;
        private const int FramesParaElHud = 5;
        private const int FramesDeEspera = 6;
        private const float DeltaTimeEstable = 0.05f;
        private const float MaxSegundosEstado = 60f;
        private const float MaxSegundosGolpes = 60f;
        private const float MaxSegundosSalida = 25f;
        private const float SegundosEntreClics = 0.25f;
        private const float SegundosSinEfecto = 1.5f;
        private const float SegundosCaminando = 0.5f;
        private const float MetrosMinimosAlMoverse = 0.3f;
        private const float DanioMortal = 100000f;
        private const float FraccionDanioBuscada = 0.9f;
        private const float FraccionVidaFregada = 0.2f;
        private const float DistanciaAlMostrador = 2f;
        private const float AlturaInicio = 0.05f;
        private const string VariableCaptura = "CAPTURAR_T145";
        private const string RutaCaptura1 = "docs/capturas/T-145-1.png";
        private const string RutaCaptura2 = "docs/capturas/T-145-2.png";
        private const int AnchoCaptura = 1280;
        private const int AltoCaptura = 720;
        private const int ProfundidadCaptura = 24;
        private const int SaltoDePixeles = 97;
        private const float VariacionMinima = 0.05f;
        private const float DistanciaPlanoCanvas = 1f;
        private const float DistanciaCamaraPantalla = 9f;

        private Keyboard teclado;
        private Mouse raton;
        private string carpeta;
        private JugadorAPie jugador;
        private MostradorPeleas mostrador;
        private EntradaArena entrada;
        private DirectorPelea director;
        private PantallaApuesta pantalla;
        private VidaMecha vidaMecha;
        private ICartera cartera;
        private ServicioInventario inventario;
        private DatosObjeto brazo;
        private readonly List<string> errores = new List<string>();
        private readonly List<ResultadoPelea> terminadas = new List<ResultadoPelea>();

        public override void Setup()
        {
            InputSystem.DisableAllEnabledActions();
            base.Setup();
            Registros.LimpiarTodos();
            EventosPelea.LimpiarSuscriptores();
            teclado = InputSystem.AddDevice<Keyboard>();
            raton = InputSystem.AddDevice<Mouse>();
            MapasDeAccionesDePrueba.Reiniciar(teclado, raton);
            carpeta = Path.Combine(Application.temporaryCachePath, "apuesta_mostrador_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(carpeta);
            ServicioGuardado.RutaForzada = Path.Combine(carpeta, "partida.json");
            errores.Clear();
            terminadas.Clear();
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
            mostrador = Object.FindFirstObjectByType<MostradorPeleas>();
            MontajeMecha montaje = Object.FindFirstObjectByType<MontajeMecha>();
            Assert.IsNotNull(jugador, "Pruebas_Economia trae al jugador");
            Assert.IsNotNull(mostrador, "Pruebas_Economia trae el mostrador de peleas");
            Assert.IsNotNull(montaje, "Pruebas_Economia trae el mecha de la historia");
            entrada = mostrador.GetComponent<EntradaArena>();
            director = mostrador.GetComponentInChildren<DirectorPelea>(true);
            pantalla = mostrador.GetComponentInChildren<PantallaApuesta>(true);
            Assert.IsNotNull(director, "El mostrador trae el flujo casero");
            Assert.IsNotNull(pantalla, "El mostrador trae la pantalla de apuesta");
            vidaMecha = montaje.GetComponent<VidaMecha>();
            cartera = Registro<ICartera>.Actual;
            inventario = Registro<ServicioInventario>.Actual;
            brazo = AssetDatabase.LoadAssetAtPath<DatosObjeto>(RutaBrazo);
            Assert.IsNotNull(cartera, "Hay cartera");
            Assert.IsNotNull(inventario, "Hay inventario");
            Assert.IsNotNull(Registro<IApuestasPelea>.Actual, "Hay servicio de apuestas");
            Assert.AreEqual(DineroInicial, cartera.Dinero, "Se empieza con $500");
            EventosPelea.PeleaTerminada += terminadas.Add;
            yield return null;
        }

        // Pone al jugador a 2 m al sur del mostrador, mirando hacia él, y deja pasar unos cuadros para que el HUD se actualice.
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

        // E en el mostrador: abre la pantalla de apuesta.
        private IEnumerator AbrirPantalla()
        {
            Assert.IsTrue(mostrador.PuedeUsar, "El mostrador ofrece la pelea: " + mostrador.Texto);
            yield return Tecla(teclado.eKey);
            Assert.IsTrue(pantalla.Visible, "E abre la pantalla de apuesta");
            Assert.IsTrue(mostrador.ApuestaAbierta, "El mostrador la tiene abierta");
            Assert.IsFalse(entrada.EnCurso, "Todavía no se pelea");
        }

        private static float DistanciaPlana(Vector3 a, Vector3 b)
        {
            return Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));
        }

        private IEnumerator EsperarHasta(Func<bool> condicion, float maxSegundos, string que)
        {
            float fin = Time.time + maxSegundos;
            while (!condicion() && Time.time < fin)
            {
                yield return null;
            }
            Assert.IsTrue(condicion(), "Se esperaba: " + que);
        }

        // Clics al aire hasta que se cumpla la condición (o se acabe el tiempo).
        private IEnumerator ClicsHasta(Func<bool> condicion)
        {
            bool clic = false;
            float proximoClic = 0f;
            float fin = Time.time + MaxSegundosGolpes;
            while (!condicion() && Time.time < fin)
            {
                if (Time.time >= proximoClic)
                {
                    clic = !clic;
                    proximoClic = Time.time + SegundosEntreClics / 2f;
                    if (clic) Press(raton.leftButton); else Release(raton.leftButton);
                }
                yield return null;
            }
            Release(raton.leftButton);
            yield return null;
        }

        private IEnumerator EsperarPelea()
        {
            yield return EsperarHasta(() => jugador.Modo == ModoJugador.Mecha, MaxSegundosEstado, "el jugador va en el mecha");
            yield return EsperarHasta(() => director.Estado == EstadoFlujoPelea.Peleando, MaxSegundosEstado, "la pelea");
            Assert.IsNotNull(director.Rival, "Hay rival");
        }

        // Golpes reales al jugador y luego remate al rival con daño directo (el rival casero no tiene piedad: puede ganar).
        private IEnumerator GanarLaPelea()
        {
            float limite = vidaMecha.Maxima * FraccionDanioBuscada;
            yield return ClicsHasta(() => vidaMecha.Actual <= limite || vidaMecha.EstaKO);
            Assert.Less(vidaMecha.Actual, vidaMecha.Maxima, "El jugador recibió golpes reales");
            Assert.IsFalse(vidaMecha.EstaKO, "Y no perdió");
            ((VidaMecha)director.Rival.Vida).RecibirDanio(new InfoDanio(DanioMortal, TipoDanio.Golpe, Vector3.zero, Vector3.forward, null));
        }

        // El rival golpea de verdad hasta dejar al mecha en KO (el mecha empieza con poca vida para no tardar).
        private IEnumerator PerderLaPelea()
        {
            yield return ClicsHasta(() => vidaMecha.EstaKO);
            Assert.IsTrue(vidaMecha.EstaKO, "El rival lo dejó en KO con golpes reales");
        }

        private IEnumerator TerminarSalida()
        {
            yield return EsperarHasta(() => director.Estado == EstadoFlujoPelea.Resultado, MaxSegundosEstado, "el resultado");
        }

        private IEnumerator EsperarFinDeLaEntrada()
        {
            yield return EsperarHasta(() => !entrada.EnCurso, MaxSegundosSalida, "termina la salida");
            yield return null;
        }

        private bool Capturando => Environment.GetEnvironmentVariable(VariableCaptura) == "1";

        // Captura de lo que ve el jugador con la pantalla de apuesta (Overlay): se pasa un instante a ScreenSpaceCamera y se devuelve.
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
            Renderizar(camara, ruta);
            foreach (Canvas lienzo in movidos)
            {
                lienzo.renderMode = RenderMode.ScreenSpaceOverlay;
            }
        }

        // Captura de la pantalla de la arena con una cámara temporal de frente a ella (el lienzo se lee desde su lado de atrás).
        private void CapturarPantallaArena(string ruta)
        {
            if (!Capturando)
            {
                return;
            }
            Transform lienzo = director.Pantalla.Lienzo.transform;
            GameObject objeto = new GameObject("CamaraCapturaT145");
            Camera camara = objeto.AddComponent<Camera>();
            camara.clearFlags = CameraClearFlags.SolidColor;
            camara.backgroundColor = new Color(0.05f, 0.05f, 0.1f);
            camara.nearClipPlane = 0.1f;
            camara.farClipPlane = 100f;
            objeto.transform.SetPositionAndRotation(lienzo.position - lienzo.forward * DistanciaCamaraPantalla, lienzo.rotation);
            Renderizar(camara, ruta);
            Object.Destroy(objeto);
        }

        private static void Renderizar(Camera camara, string ruta)
        {
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
        public IEnumerator Apostar_Ganar_PagaApuestaMomioPremioYEntregaElBrazo()
        {
            yield return CargarEscena();
            yield return IrAlMostrador();
            Assert.AreEqual(0, inventario.Cantidad(brazo), "Sin brazo al inicio");

            yield return AbrirPantalla();
            Assert.AreEqual(TextoRival, pantalla.Rival, "Dice contra quién");
            Assert.AreEqual(TextoPremio, pantalla.Premio, "Dice el premio");
            Assert.AreEqual(TextoMomio, pantalla.Momio, "Dice el momio");
            Assert.AreEqual(TextoPieza, pantalla.Pieza, "Dice la pieza en juego");
            Assert.AreEqual(TextoAyuda, pantalla.Ayuda, "Dice las teclas");
            Assert.AreEqual(4, pantalla.CantidadOpciones, "SIN APUESTA y tres montos");
            Assert.AreEqual(0, pantalla.IndiceElegido, "Empieza en SIN APUESTA");
            Assert.AreEqual(0, pantalla.Monto);

            yield return Tecla(teclado.rightArrowKey);
            Assert.AreEqual(100, pantalla.Monto, "→ elige $100");
            yield return Tecla(teclado.rightArrowKey);
            Assert.AreEqual(Apuesta, pantalla.Monto, "→ → elige $250");
            Assert.AreEqual("[ $250 ]", pantalla.TextoOpcion(2), "El elegido se resalta");
            Assert.AreEqual("$500", pantalla.TextoOpcion(3));
            yield return Cuadros(FramesParaElHud);
            CapturarCabina(RutaCaptura1);

            yield return Tecla(teclado.enterKey);
            Assert.IsFalse(pantalla.Visible, "Enter cierra la pantalla");
            Assert.AreEqual(DineroInicial - Apuesta, cartera.Dinero, "Se cobra lo apostado");
            Assert.IsTrue(entrada.EnCurso, "Empieza la entrada (fundido)");
            yield return EsperarPelea();

            yield return GanarLaPelea();
            yield return TerminarSalida();
            Assert.AreEqual(TextoApuestaGanada, director.Pantalla.Apuesta, "La pantalla de la arena dice la apuesta");
            Assert.AreEqual("+$600", director.Pantalla.Premio, "Y el premio");
            CapturarPantallaArena(RutaCaptura2);
            yield return EsperarFinDeLaEntrada();

            Assert.AreEqual(1, terminadas.Count, "La pelea terminó una vez");
            Assert.AreEqual(LadoPelea.Jugador, terminadas[0].Ganador, "Ganó el jugador");
            Assert.AreEqual(DineroTrasGanar, cartera.Dinero, "$500 - 250 + 500 + 600 = $1,350");
            Assert.AreEqual(1, inventario.Cantidad(brazo), "Se llevó el Brazo de chatarra");
            Assert.AreEqual(ModoJugador.APie, jugador.Modo, "Volvió a pie");
            Assert.IsFalse(Registro<IApuestasPelea>.Actual.Abierta, "No queda apuesta abierta");
            Assert.AreEqual(0, Registro<IApuestasPelea>.Actual.Apostado, "Ni apostado");
            Assert.IsEmpty(errores, "Consola sin errores: " + string.Join(" | ", errores));
        }

        [UnityTest]
        public IEnumerator Apostar_Perder_PierdeLaApuestaYSeVeEnLaPantallaDeLaArena()
        {
            yield return CargarEscena();
            vidaMecha.FijarVida(vidaMecha.Maxima * FraccionVidaFregada);
            yield return IrAlMostrador();

            yield return AbrirPantalla();
            yield return Tecla(teclado.rightArrowKey);
            yield return Tecla(teclado.rightArrowKey);
            Assert.AreEqual(Apuesta, pantalla.Monto, "→ → elige $250");
            yield return Tecla(teclado.enterKey);
            Assert.AreEqual(DineroInicial - Apuesta, cartera.Dinero, "Se cobra lo apostado");
            yield return EsperarPelea();

            yield return PerderLaPelea();
            yield return TerminarSalida();
            Assert.AreEqual(TextoApuestaPerdida, director.Pantalla.Apuesta, "La pantalla de la arena dice lo perdido");
            yield return EsperarFinDeLaEntrada();

            Assert.AreEqual(1, terminadas.Count, "La pelea terminó una vez");
            Assert.AreEqual(LadoPelea.Rival, terminadas[0].Ganador, "Ganó el rival");
            Assert.AreEqual(DineroInicial - Apuesta, cartera.Dinero, "Perdió lo apostado y el premio ($0 al perder)");
            Assert.AreEqual(0, inventario.Cantidad(brazo), "El inventario sigue igual");
            Assert.AreEqual(ModoJugador.APie, jugador.Modo, "Volvió a pie");
            Assert.IsEmpty(errores, "Consola sin errores: " + string.Join(" | ", errores));
        }

        [UnityTest]
        public IEnumerator Salir_ConEsc_NoPeleaNoCobraYElJugadorSeMueveOtraVez()
        {
            yield return CargarEscena();
            yield return IrAlMostrador();
            ControladorAPie controlador = jugador.GetComponent<ControladorAPie>();

            // La E que abre sigue pulsada cuando llega Esc: al cerrar no debe reabrirse.
            Press(teclado.eKey);
            yield return null;
            Assert.IsTrue(pantalla.Visible, "E abre la pantalla");
            Assert.IsFalse(controlador.ControlActivo, "El jugador queda congelado mientras se ve");
            Press(teclado.escapeKey);
            yield return null;
            Release(teclado.escapeKey);
            Release(teclado.eKey);
            yield return Cuadros(FramesDeEspera);

            Assert.IsFalse(pantalla.Visible, "Esc cierra la pantalla");
            Assert.IsFalse(mostrador.ApuestaAbierta, "El mostrador ya no la tiene abierta");
            Assert.IsFalse(entrada.EnCurso, "No pelea");
            Assert.AreEqual(EstadoFlujoPelea.Preparando, director.Estado, "El flujo no arrancó");
            Assert.AreEqual(DineroInicial, cartera.Dinero, "El dinero no cambia");
            Assert.IsFalse(Registro<IApuestasPelea>.Actual.Abierta, "Se cancela lo anunciado");
            Assert.IsTrue(controlador.ControlActivo, "El jugador recupera el control");

            Vector3 antes = jugador.Raiz.position;
            Press(teclado.wKey);
            float fin = Time.time + SegundosCaminando;
            while (Time.time < fin)
            {
                yield return null;
            }
            Release(teclado.wKey);
            yield return null;
            Assert.Greater(DistanciaPlana(antes, jugador.Raiz.position), MetrosMinimosAlMoverse, "Con W el jugador vuelve a moverse");
            Assert.IsFalse(pantalla.Visible, "La E que abrió no reabrió la pantalla");

            // Se puede volver a abrir y a salir otra vez (el anuncio se canceló bien).
            yield return IrAlMostrador();
            yield return AbrirPantalla();
            yield return Tecla(teclado.escapeKey);
            Assert.IsFalse(pantalla.Visible, "Otra vez Esc cierra");
            yield return new WaitForSeconds(SegundosSinEfecto);
            Assert.IsFalse(entrada.EnCurso, "Sigue sin pelear");
            Assert.AreEqual(DineroInicial, cartera.Dinero, "Y sin cobrar");
            Assert.IsEmpty(errores, "Consola sin errores: " + string.Join(" | ", errores));
        }

        [UnityTest]
        public IEnumerator PocoDinero_LasFlechasNoSalenDeSinApuestaYEnterPeleaSinApostar()
        {
            yield return CargarEscena();
            vidaMecha.FijarVida(vidaMecha.Maxima * FraccionVidaFregada);
            Assert.IsTrue(cartera.IntentarPagar(DineroInicial - DineroPocoAlcanza, MotivoDinero.Otro), "Se gastan $420");
            Assert.AreEqual(DineroPocoAlcanza, cartera.Dinero);
            yield return IrAlMostrador();

            yield return AbrirPantalla();
            Assert.AreEqual(pantalla.ColorOpcion(1), pantalla.ColorOpcion(3), "Los montos que no se pagan se ven apagados");
            Assert.AreNotEqual(pantalla.ColorOpcion(0), pantalla.ColorOpcion(1), "Y distintos del elegido");
            yield return Tecla(teclado.rightArrowKey);
            yield return Tecla(teclado.rightArrowKey);
            Assert.AreEqual(0, pantalla.IndiceElegido, "→ no sale de SIN APUESTA");
            yield return Tecla(teclado.leftArrowKey);
            Assert.AreEqual(0, pantalla.IndiceElegido, "← tampoco");

            yield return Tecla(teclado.enterKey);
            Assert.AreEqual(DineroPocoAlcanza, cartera.Dinero, "Sin apuesta no se cobra nada");
            Assert.IsTrue(entrada.EnCurso, "Pelea sin apostar");
            yield return EsperarPelea();
            Assert.AreEqual(0, Registro<IApuestasPelea>.Actual.Apostado, "No hay nada apostado");

            yield return PerderLaPelea();
            yield return TerminarSalida();
            Assert.AreEqual(string.Empty, director.Pantalla.Apuesta, "Sin apuesta la pantalla de la arena no dice nada");
            yield return EsperarFinDeLaEntrada();

            Assert.AreEqual(LadoPelea.Rival, terminadas[0].Ganador, "Perdió");
            Assert.AreEqual(DineroPocoAlcanza, cartera.Dinero, "Dinero tras perder: $80");
            Assert.AreEqual(0, inventario.Cantidad(brazo), "Sin pieza");
            Assert.IsEmpty(errores, "Consola sin errores: " + string.Join(" | ", errores));
        }
    }
}
