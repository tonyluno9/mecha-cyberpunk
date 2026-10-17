using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using MechaCyberpunk.Combate.Vida;
using MechaCyberpunk.Core;
using MechaCyberpunk.Progresion.Pruebas;
using MechaCyberpunk.Economia;
using MechaCyberpunk.Mecha.Estado;
using MechaCyberpunk.Mecha.Llamado;
using MechaCyberpunk.Mecha.Montaje;
using MechaCyberpunk.Mecha.Taller;
using MechaCyberpunk.Pelea.Flujo;
using MechaCyberpunk.Personaje;
using MechaCyberpunk.Personaje.Interaccion;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Progresion;
using MechaCyberpunk.Progresion.Guardado;
using MechaCyberpunk.Progresion.Prologo;
using MechaCyberpunk.UI.Historia;
using MechaCyberpunk.UI.Hud;
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
    /// T-133: el presente del prólogo en Pruebas_Prologo, jugado con entrada simulada (W, ratón, E, clics, Enter y Esc). Despertar en la cama con
    /// el estado inicial, reparar en el banco, ir al clandestino, la pelea casera ganada y perdida, "continuará" y reanudar. Atajos permitidos y
    /// anotados en el registro: rematar al rival tras daño real al mecha, acortar caminatas de más de 40 m con IJugador.Colocar, adelantar
    /// el reloj a las 20:30 si al llegar al mostrador todavía es de día, y el cambio de escena al título se intercepta.
    /// </summary>
    public class PrologoPresentePruebas : InputTestFixture
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Tony/Pruebas_Prologo.unity";
        private const string RutaPrefabDirector = "Assets/_Proyecto/Progresion/Prologo/Prefabs/DirectorPrologo.prefab";
        private const string RutaDatosEscenasPruebas = "Assets/_Proyecto/Progresion/Datos/DatosEscenas_Pruebas.asset";
        private const string RutaControlesAPie = "Assets/_Proyecto/Personaje/Datos/ConfigControlesAPie_Base.asset";
        private const string IdSalida = "Clandestino_Salida";
        private const string IdCama = "Casa_Cama";
        private const string IdPuerta = "Casa_Puerta";
        private const string Alias = "LA CHISPA";
        private const string TextoAviso = "Tu mecha quedó fregado. Puedes repararlo en el banco del taller antes de pelear.";
        private const string TextoClandestino = "Ve al clandestino: tu primera pelea casera";
        private const string TextoMostrador = "Habla con el encargado (E)";
        private const string VariableCaptura = "CAPTURAR_T133";
        private const string RutaCaptura = "docs/capturas/T-133-1.png";
        private const int DineroInicial = 500;
        private const int DineroDePartida = 2000;
        private const int DiasEntreRentas = 7;
        private const int FramesEstables = 10;
        private const int MaxFramesCalentamiento = 1000;
        private const int FramesParaElHud = 8;
        private const int AnchoCaptura = 1280;
        private const int AltoCaptura = 720;
        private const int ProfundidadCaptura = 24;
        private const int SaltoDePixeles = 97;
        private const float DeltaTimeEstable = 0.05f;
        private const float HoraDespertar = 19.5f;
        private const float HoraAdelantada = 20.5f;
        private const float ToleranciaHora = 0.1f;
        private const float PorcentajeDespertar = 45f;
        private const float ToleranciaPorcentaje = 0.5f;
        private const float FraccionFregado = 0.2f;
        private const float FraccionDanioBuscada = 0.9f;
        private const float DanioMortal = 100000f;
        private const float MaxSegundosEstado = 60f;
        private const float MaxSegundosTramo = 40f;
        private const float MaxSegundosGolpes = 60f;
        private const float MaxSegundosCargar = 90f;
        private const float SegundosEntreClics = 0.25f;
        private const float SegundosEnterEsc = 4.2f;
        private const float SegundosMoverse = 0.6f;
        private const float SegundosLibre = 1.5f;
        private const float ToleranciaJugador = 0.6f;
        private const float ToleranciaPunto = 0.5f;
        private const float AlturaJugador = 0.05f;
        private const float DistanciaLlegada = 1f;
        private const float DistanciaAlMostrador = 1.8f;
        private const float DistanciaAlBanco = 2.2f;
        private const float MetrosParaAtajo = 40f;
        private const float MetrosTrasAtajo = 10f;
        private const float MetrosAdentro = 6f;
        private const float MetrosAdentroDeLaPuerta = 3.5f;
        private const float MetrosMinimosAlMoverse = 0.5f;
        private const float GananciaGiro = 0.4f;
        private const float AnguloParaCaminar = 60f;
        private const float GradosMediaVuelta = 180f;
        private const float AnguloMirarListo = 2f;
        private const float VariacionMinima = 0.05f;
        private const float DistanciaPlanoCanvas = 0.35f;
        private const float CalorDePrueba = 20f;

        private Mouse raton;
        private Keyboard teclado;
        private string carpeta;
        private JugadorAPie jugador;
        private InteraccionAPie interaccion;
        private MontajeMecha montaje;
        private LlamadoMecha llamado;
        private VidaMecha vidaMecha;
        private MechaPersistente persistente;
        private BancoTaller banco;
        private DirectorPrologo prologo;
        private MostradorPeleas mostrador;
        private DirectorPelea flujoCasero;
        private PantallaContinuara pantallaContinuara;
        private PantallaSubtitulos subtitulos;
        private HudAPie hud;
        private ServicioHistoria historia;
        private ICartera cartera;
        private IRelojMundo reloj;
        private float sensibilidadAPie;
        private bool avanzando;
        private readonly List<string> errores = new List<string>();
        private readonly List<string> rutasPedidas = new List<string>();
        private readonly List<string> atajos = new List<string>();
        private HashSet<GameObject> raicesIniciales;


        public override void Setup()
        {
            raicesIniciales = RaicesDePrueba.Anotar();
            // Otras pruebas (sin InputTestFixture) dejan acciones prendidas con un estado viejo: se apagan y se descarta su resolución.
            InputSystem.DisableAllEnabledActions();
            base.Setup();
            Registros.LimpiarTodos();
            EventosPelea.LimpiarSuscriptores();
            teclado = InputSystem.AddDevice<Keyboard>();
            raton = InputSystem.AddDevice<Mouse>();
            MapasDeAccionesDePrueba.Reiniciar(teclado, raton);
            carpeta = Path.Combine(Application.temporaryCachePath, "prologo_presente_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(carpeta);
            ServicioGuardado.RutaForzada = Path.Combine(carpeta, "partida.json");
            CambioEscena.Interceptar(null);
            CambioEscena.ConsumirTransicion();
            errores.Clear();
            rutasPedidas.Clear();
            atajos.Clear();
            avanzando = false;
            Application.logMessageReceived += RegistrarError;
        }

        public override void TearDown()
        {
            Application.logMessageReceived -= RegistrarError;
            ServicioGuardado.RutaForzada = null;
            CambioEscena.Interceptar(null);
            CambioEscena.ConsumirTransicion();
            if (Directory.Exists(carpeta))
            {
                Directory.Delete(carpeta, true);
            }
            EventosPelea.LimpiarSuscriptores();
            Registros.LimpiarTodos();
            // Deja los mapas apagados y sin dispositivos antes y después de que el fixture reinicie el Input System.
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

        private void AssertSinErrores()
        {
            Assert.AreEqual(0, errores.Count, string.Join("\n", errores));
        }

        // ---- Preparación ----

        // Escribe una partida: la etapa, mucho dinero, el mecha a vida completa y una renta con adeudo, para que el estado inicial se note.
        private static void EscribirPartida(EtapaPrologo etapa, int dinero)
        {
            PiezaGuardada[] piezas =
            {
                new PiezaGuardada { clave = ServicioHistoria.Clave, datos = "{\"etapa\":\"" + etapa + "\"}" },
                new PiezaGuardada { clave = ServicioCartera.Clave, datos = "{\"dinero\":" + dinero + "}" },
                new PiezaGuardada { clave = MechaPersistente.Clave, datos = "{\"vida\":100,\"vidaMaxima\":100}" },
                new PiezaGuardada { clave = ServicioRenta.Clave, datos = "{\"diaProximoCobro\":50,\"adeudo\":300}" },
                new PiezaGuardada { clave = ServicioPerfil.Clave, datos = "{\"nombre\":\"Juan\",\"alias\":\"" + Alias + "\",\"cara\":0,\"ropa\":0,\"implante\":0}" },
            };
            File.WriteAllText(ServicioGuardado.RutaForzada, ArchivoPartida.Escribir(piezas, DateTime.UtcNow.ToString("o")));
        }

        // Carga la escena. 'real': con una transición real de CambioEscena.Ir. Si no, se carga directo y la transición se marca a mano
        // (el cambio de escena queda interceptado para ver a dónde se pide ir).
        private IEnumerator CargarEscena(EtapaPrologo etapa, bool real, bool quitarDirector = false, int dinero = DineroDePartida)
        {
            sensibilidadAPie = AssetDatabase.LoadAssetAtPath<ConfigControlesAPie>(RutaControlesAPie).SensibilidadRaton;
            EscribirPartida(etapa, dinero);
            if (real)
            {
                CambioEscena.Ir(RutaEscena);
                yield return EsperarHasta(() => Object.FindFirstObjectByType<JugadorAPie>() != null, MaxSegundosCargar, "la escena");
            }
            else
            {
                CambioEscena.Interceptar(ruta => rutasPedidas.Add(ruta));
                CambioEscena.Ir("transicion-de-prueba");
                rutasPedidas.Clear();
                yield return EditorSceneManager.LoadSceneAsyncInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            }

            jugador = Object.FindFirstObjectByType<JugadorAPie>();
            prologo = Object.FindFirstObjectByType<DirectorPrologo>();
            montaje = Object.FindFirstObjectByType<MontajeMecha>();
            mostrador = Object.FindFirstObjectByType<MostradorPeleas>();
            hud = Object.FindFirstObjectByType<HudAPie>();
            interaccion = Object.FindFirstObjectByType<InteraccionAPie>();
            Assert.IsNotNull(jugador, "Pruebas_Prologo trae al jugador");
            Assert.IsNotNull(prologo, "Pruebas_Prologo trae el director del prólogo");
            Assert.IsNotNull(montaje, "Pruebas_Prologo trae el mecha de la historia");
            Assert.IsNotNull(mostrador, "Pruebas_Prologo trae el mostrador");
            Assert.IsNotNull(hud, "El jugador trae el HUD a pie");
            Assert.IsNotNull(interaccion, "El jugador trae la interacción");
            llamado = montaje.GetComponent<LlamadoMecha>();
            vidaMecha = montaje.GetComponent<VidaMecha>();
            persistente = montaje.GetComponent<MechaPersistente>();
            banco = montaje.GetComponent<BancoTaller>();
            flujoCasero = mostrador.GetComponentInChildren<DirectorPelea>(true);
            pantallaContinuara = prologo.GetComponentInChildren<PantallaContinuara>(true);
            subtitulos = prologo.GetComponentInChildren<PantallaSubtitulos>(true);
            historia = Registro<IHistoria>.Actual as ServicioHistoria;
            cartera = Registro<ICartera>.Actual;
            reloj = Registro<IRelojMundo>.Actual;
            Assert.IsNotNull(flujoCasero, "El mostrador trae el flujo casero");
            Assert.IsNotNull(pantallaContinuara, "El director trae la pantalla de continuará");
            Assert.IsNotNull(subtitulos, "El director trae los subtítulos");
            Assert.IsNotNull(persistente, "El mecha de la historia es persistente");
            Assert.IsNotNull(banco, "El mecha de la historia trae el banco del taller");
            Assert.IsNotNull(historia, "Hay ServicioHistoria");
            Assert.IsNotNull(cartera, "Hay cartera");
            Assert.IsNotNull(reloj, "Hay reloj");
            if (quitarDirector)
            {
                // Se quita el director de la escena (que despertaría al jugador) antes de que corra su primer cuadro.
                Object.Destroy(prologo);
            }
            yield return Esperar(FramesEstables, true);
        }

        private IEnumerator Esperar(int cuadros, bool estables)
        {
            int cuenta = 0;
            int intentos = 0;
            while (cuenta < cuadros && intentos < MaxFramesCalentamiento)
            {
                yield return null;
                intentos++;
                cuenta = !estables || Time.deltaTime < DeltaTimeEstable ? cuenta + 1 : 0;
            }
        }

        private IEnumerator EsperarSegundos(float segundos)
        {
            float fin = Time.time + segundos;
            while (Time.time < fin)
            {
                yield return null;
            }
        }

        private static IEnumerator EsperarHasta(Func<bool> condicion, float maxSegundos, string que)
        {
            float fin = Time.realtimeSinceStartup + maxSegundos;
            while (!condicion() && Time.realtimeSinceStartup < fin)
            {
                yield return null;
            }
            Assert.IsTrue(condicion(), "Se esperaba: " + que);
        }

        private IEnumerator Tecla(KeyControl tecla)
        {
            Press(tecla);
            yield return null;
            Release(tecla);
            yield return null;
        }

        private static float DistanciaPlana(Vector3 a, Vector3 b)
        {
            Vector3 delta = a - b;
            delta.y = 0f;
            return delta.magnitude;
        }

        private static IPuntoAparicion Aparicion(string id)
        {
            foreach (IPuntoAparicion punto in Registro<IPuntoAparicion>.Todos)
            {
                if (punto.Id == id)
                {
                    return punto;
                }
            }
            return null;
        }

        private static IObjetivos Objetivos => Registro<IObjetivos>.Actual;

        // ---- Entrada: caminar ----

        private void Avanzar(bool avanzar)
        {
            if (avanzar == avanzando)
            {
                return;
            }
            avanzando = avanzar;
            if (avanzar)
            {
                Press(teclado.wKey);
            }
            else
            {
                Release(teclado.wKey);
            }
        }

        private float AnguloHacia(Vector3 destino)
        {
            Transform camara = jugador.Camara.transform;
            return CalculoFlechaObjetivo.Angulo(camara.position, camara.forward, destino);
        }

        // Camina hacia un punto girando con el ratón en proporción al ángulo; termina al llegar.
        private IEnumerator CaminarA(Vector3 destino, float distancia, string que)
        {
            float fin = Time.time + MaxSegundosTramo;
            while (DistanciaPlana(jugador.Raiz.position, destino) > distancia)
            {
                if (Time.time > fin)
                {
                    Avanzar(false);
                    Assert.Fail("No llegó a: " + que + " (posición " + jugador.Raiz.position + ")");
                }
                float angulo = AnguloHacia(destino);
                float delta = Mathf.Clamp(angulo * GananciaGiro, -GradosMediaVuelta, GradosMediaVuelta) / sensibilidadAPie;
                Set(raton.delta, new Vector2(delta, 0f));
                Avanzar(Mathf.Abs(angulo) < AnguloParaCaminar);
                yield return null;
            }
            Avanzar(false);
            yield return null;
        }

        // Atajo anotado: una caminata de más de 40 m se acorta con Colocar hasta 10 m antes del objetivo, mirando a él.
        private IEnumerator CaminarOAcortar(Vector3 destino, float distancia, string que)
        {
            Vector3 origen = jugador.Raiz.position;
            float lejos = DistanciaPlana(origen, destino);
            if (lejos > MetrosParaAtajo)
            {
                Vector3 haciaDestino = destino - origen;
                haciaDestino.y = 0f;
                haciaDestino.Normalize();
                Vector3 nueva = destino - haciaDestino * MetrosTrasAtajo;
                nueva.y = AlturaJugador;
                jugador.Colocar(new Pose(nueva, Quaternion.LookRotation(haciaDestino)));
                atajos.Add("Colocar (" + que + "): caminata de " + lejos.ToString("F0") + " m acortada a " + MetrosTrasAtajo.ToString("F0") + " m");
                yield return null;
            }
            yield return CaminarA(destino, distancia, que);
        }

        private IEnumerator MirarA(Vector3 destino, string que)
        {
            float fin = Time.time + MaxSegundosTramo;
            while (Mathf.Abs(AnguloHacia(destino)) > AnguloMirarListo)
            {
                if (Time.time > fin)
                {
                    Assert.Fail("No pudo mirar a: " + que);
                }
                Set(raton.delta, new Vector2(AnguloHacia(destino) * GananciaGiro / sensibilidadAPie, 0f));
                yield return null;
            }
            yield return null;
        }

        // Clics al aire hasta que se cumpla la condición (o se acabe el tiempo): el rival golpea de verdad.
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

        // ---- Pasos ----

        private IEnumerator EsperarDespertar()
        {
            yield return EsperarHasta(() => prologo.Iniciado && historia.Etapa == EtapaPrologo.PeleaCasera, MaxSegundosEstado, "el despertar");
        }

        // Pone al jugador junto al banco, mirando a él, y repara con E.
        private IEnumerator IrAlBanco()
        {
            // La cama está dentro de la casa: se sale por la puerta (muro sur), se cruza la calle y se llega al taller por el sur.
            Vector3 puerta = Aparicion(IdPuerta).Pose.position;
            Vector3 punto = banco.Posicion;
            yield return CaminarA(puerta + new Vector3(0f, 0f, MetrosAdentroDeLaPuerta), DistanciaLlegada, "adentro de la puerta de la casa");
            yield return CaminarA(puerta, DistanciaLlegada, "la puerta de la casa");
            yield return CaminarA(new Vector3(punto.x, AlturaJugador, puerta.z), DistanciaLlegada, "la calle frente al taller");
            yield return CaminarOAcortar(punto, DistanciaAlBanco, "el banco del taller");
            yield return MirarA(punto, "el banco del taller");
            yield return EsperarHasta(() => interaccion.TipoCandidato == TipoCandidatoAPie.Interactuable, 3f, "la interacción ofrece el banco");
        }

        // Del taller o la cama al mostrador: a la salida del clandestino, adentro (el objetivo pasa al mostrador) y junto al encargado.
        private IEnumerator IrAlMostrador()
        {
            IPuntoAparicion salida = Aparicion(IdSalida);
            Assert.IsNotNull(salida, "Existe Clandestino_Salida");
            Assert.AreEqual(TextoClandestino, Objetivos.Actual.Texto, "Antes de entrar el objetivo es el clandestino");
            yield return CaminarOAcortar(salida.Pose.position, DistanciaLlegada, "la entrada del clandestino");
            Vector3 adentro = salida.Pose.position + new Vector3(0f, 0f, MetrosAdentro);
            yield return CaminarA(adentro, DistanciaLlegada, "adentro del clandestino");
            yield return null;
            Assert.AreEqual(TextoMostrador, Objetivos.Actual.Texto, "Al entrar al clandestino el objetivo pasa al mostrador");
            Assert.IsTrue(Objetivos.Actual.TienePosicion, "Y apunta a una posición");
            Assert.Less(Vector3.Distance(Objetivos.Actual.Posicion, mostrador.Posicion), ToleranciaPunto, "El mostrador");

            yield return CaminarA(mostrador.Posicion, DistanciaAlMostrador, "el mostrador");
            yield return MirarA(mostrador.Posicion, "el mostrador");
            if (!reloj.EsDeNoche)
            {
                reloj.AvanzarHasta(HoraAdelantada);
                atajos.Add("AvanzarHasta(" + HoraAdelantada.ToString("F1") + "): todavía era de día al llegar al mostrador");
                yield return null;
            }
            yield return EsperarHasta(() => mostrador.PuedeUsar, 3f, "el mostrador ofrece la pelea: " + mostrador.Texto);
            yield return EsperarHasta(() => interaccion.TipoCandidato == TipoCandidatoAPie.Interactuable, 3f, "la interacción ofrece el mostrador");
        }

        private IEnumerator PelearCasera(bool ganar)
        {
            yield return Tecla(teclado.eKey);
            yield return Tecla(teclado.enterKey); // T-145: la pantalla de apuesta, sin apostar
            yield return EsperarHasta(() => jugador.Modo == ModoJugador.Mecha, MaxSegundosEstado, "el jugador va en el mecha");
            yield return EsperarHasta(() => flujoCasero.Estado == EstadoFlujoPelea.Peleando, MaxSegundosEstado, "la pelea");
            if (ganar)
            {
                // Golpes reales al jugador y luego remate con daño directo (el rival casero no tiene piedad: puede ganar).
                float limite = vidaMecha.Maxima * FraccionDanioBuscada;
                yield return ClicsHasta(() => vidaMecha.Actual <= limite || vidaMecha.EstaKO);
                Assert.Less(vidaMecha.Actual, vidaMecha.Maxima, "El jugador recibió golpes reales");
                Assert.IsFalse(vidaMecha.EstaKO, "Y no perdió");
                ((VidaMecha)flujoCasero.Rival.Vida).RecibirDanio(new InfoDanio(DanioMortal, TipoDanio.Golpe, Vector3.zero, Vector3.forward, null));
                atajos.Add("RecibirDanio: rival casero rematado tras daño real al mecha");
            }
            else
            {
                yield return ClicsHasta(() => vidaMecha.EstaKO);
                Assert.IsTrue(vidaMecha.EstaKO, "El rival lo dejó en KO con golpes reales");
            }
            yield return EsperarHasta(() => pantallaContinuara.Visible, MaxSegundosEstado, "la pantalla de continuará");
            yield return null;
        }

        // Captura de lo que ve el jugador (solo con CAPTURAR_T133=1). Los lienzos en Overlay no salen en una cámara a textura:
        // se pasan un instante a ScreenSpaceCamera con esa cámara y se devuelven como estaban.
        private void Capturar()
        {
            if (Environment.GetEnvironmentVariable(VariableCaptura) != "1")
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
            string completa = Path.GetFullPath(Path.Combine(Application.dataPath, "..", RutaCaptura));
            Directory.CreateDirectory(Path.GetDirectoryName(completa));
            File.WriteAllBytes(completa, imagen.EncodeToPNG());
            Object.Destroy(rt);
            Object.Destroy(imagen);
            Assert.Greater(maximo - minimo, VariacionMinima, "La captura es plana: no se ve nada");
        }

        private static int OyentesActivos()
        {
            int oyentes = 0;
            foreach (AudioListener oyente in Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (oyente.enabled && oyente.gameObject.activeInHierarchy)
                {
                    oyentes++;
                }
            }
            return oyentes;
        }

        // ---- Pruebas ----

        [UnityTest]
        public IEnumerator Presente_Ganar_DespiertaReparaVaAlClandestinoGanaYContinuara()
        {
            yield return CargarEscena(EtapaPrologo.Despertar, true);
            yield return EsperarDespertar();

            // a) El estado inicial, aunque la partida traía 2000 pesos, el mecha completo y una renta con adeudo.
            IPuntoAparicion cama = Aparicion(IdCama);
            Assert.IsNotNull(cama, "Existe Casa_Cama");
            Assert.Less(DistanciaPlana(jugador.Raiz.position, cama.Pose.position), ToleranciaJugador - 0.1f, "El jugador despierta en Casa_Cama");
            Assert.AreEqual(ModoJugador.APie, jugador.Modo);
            Assert.AreEqual(DineroInicial, cartera.Dinero, "Dinero inicial, no el de la partida");
            Assert.AreEqual(PorcentajeDespertar, vidaMecha.Actual / vidaMecha.Maxima * 100f, ToleranciaPorcentaje, "Mecha al 45 %");
            Assert.AreEqual(EstadoLlamado.EnTaller, llamado.Estado, "Y en su taller");
            IRenta renta = Registro<IRenta>.Actual;
            Assert.IsNotNull(renta, "Hay renta");
            Assert.AreEqual(reloj.Dia + DiasEntreRentas, renta.DiaProximoCobro, "Próximo cobro: día actual + 7");
            Assert.AreEqual(0, renta.Adeudo, "Sin adeudo");
            Assert.AreEqual(0f, Registro<ICalor>.Actual.Valor, "Calor en 0");
            Assert.AreEqual(HoraDespertar, reloj.Hora, ToleranciaHora, "Son las 19:30");
            Assert.AreEqual(EtapaPrologo.PeleaCasera, historia.Etapa, "La etapa es la pelea casera");
            StringAssert.Contains("PeleaCasera", File.ReadAllText(ServicioGuardado.RutaForzada), "La etapa quedó guardada");
            Assert.IsTrue(subtitulos.SubtituloVisible, "El aviso está visible");
            Assert.AreEqual(TextoAviso, subtitulos.Subtitulo);

            yield return Esperar(FramesParaElHud, false);
            Assert.AreEqual(TextoClandestino, hud.TextoObjetivo.text, "El HUD dice a dónde ir");
            Assert.IsTrue(hud.GrupoObjetivo.activeInHierarchy, "El objetivo está a la vista");
            PantallaFundido fundido = PantallaFundido.Actual;
            Assert.IsNotNull(fundido, "Hay pantalla de fundido");
            yield return EsperarHasta(() => fundido.Alfa < 0.01f, MaxSegundosEstado, "termina el fundido de entrada");
            Assert.IsTrue(subtitulos.SubtituloVisible, "El aviso sigue a la vista tras el fundido");
            Assert.AreEqual(IdLugar.Clandestino, Objetivos.Actual.Lugar, "Apunta al clandestino");
            Capturar();

            // b) Reparar en el banco aunque no alcance el dinero.
            float danioAntes = persistente.Danio;
            Assert.Greater(danioAntes, 0f, "El mecha despertó dañado");
            yield return IrAlBanco();
            Assert.IsTrue(banco.PuedeUsar, "El banco ofrece reparar: " + banco.Texto);
            int costo = banco.Costo;
            Assert.Greater(costo, 0, "Reparar cuesta");
            yield return Tecla(teclado.eKey);
            yield return Tecla(teclado.enterKey); // T-149: E abre la pantalla del banco; Enter elige "REPARAR TODO".
            Assert.AreEqual(DineroInicial - costo, cartera.Dinero, "Dinero = 500 - costo");
            Assert.AreEqual(vidaMecha.Maxima, vidaMecha.Actual, 0.01f, "Vida al máximo");

            // c) Al clandestino: el objetivo pasa al mostrador; E; gana; continuará.
            yield return IrAlMostrador();
            int dineroAntes = cartera.Dinero;
            yield return PelearCasera(true);
            Assert.AreEqual(EtapaPrologo.Continuara, historia.Etapa, "La etapa es continuará");
            Assert.IsFalse(Objetivos.HayObjetivo, "Sin objetivo");
            Assert.AreEqual(Alias, pantallaContinuara.Alias, "Con el alias");
            Assert.AreEqual(FormatoDinero.Texto(cartera.Dinero), pantallaContinuara.Dinero, "Y el dinero");
            Assert.Greater(cartera.Dinero, dineroAntes, "Ganar pagó el premio");
            StringAssert.Contains("Ganaste", pantallaContinuara.Resultado);
            Assert.AreEqual(1, OyentesActivos(), "Un solo AudioListener activo");

            yield return EsperarSegundos(SegundosEnterEsc);
            Assert.IsTrue(pantallaContinuara.TeclasActivas, "Pasaron 4 s: las teclas funcionan");
            yield return Tecla(teclado.enterKey);
            Assert.AreEqual(EtapaPrologo.Libre, historia.Etapa, "Enter sigue jugando: etapa libre");
            Assert.IsFalse(pantallaContinuara.Visible, "La pantalla se cierra");
            Assert.IsFalse(Objetivos.HayObjetivo, "Sin objetivo");
            Assert.AreEqual(ModoJugador.APie, jugador.Modo);
            Vector3 antes = jugador.Raiz.position;
            Press(teclado.wKey);
            yield return EsperarSegundos(SegundosMoverse);
            Release(teclado.wKey);
            yield return null;
            Assert.Greater(DistanciaPlana(antes, jugador.Raiz.position), MetrosMinimosAlMoverse, "El jugador se mueve con W");
            Debug.Log("[Prueba T-133] Atajos: " + string.Join(" | ", atajos));
            AssertSinErrores();
        }

        [UnityTest]
        public IEnumerator Presente_Perder_ContinuaraDicePerdisteYEscPideElTitulo()
        {
            yield return CargarEscena(EtapaPrologo.Despertar, false);
            yield return EsperarDespertar();
            Assert.AreEqual(DineroInicial, cartera.Dinero);

            vidaMecha.FijarVida(vidaMecha.Maxima * FraccionFregado);
            yield return IrAlMostrador();
            yield return PelearCasera(false);

            Assert.AreEqual(EtapaPrologo.Continuara, historia.Etapa);
            StringAssert.Contains("Perdiste", pantallaContinuara.Resultado, "Dice que perdió");
            Assert.AreEqual(Alias, pantallaContinuara.Alias);
            Assert.AreEqual(FormatoDinero.Texto(cartera.Dinero), pantallaContinuara.Dinero);
            Assert.IsEmpty(rutasPedidas, "Todavía no pide ninguna escena");

            yield return EsperarSegundos(SegundosEnterEsc);
            yield return Tecla(teclado.escapeKey);
            DatosEscenas escenas = AssetDatabase.LoadAssetAtPath<DatosEscenas>(RutaDatosEscenasPruebas);
            Assert.AreEqual(1, rutasPedidas.Count, "Esc pide una escena");
            Assert.AreEqual(escenas.RutaTitulo, rutasPedidas[0], "La del título");
            Assert.AreEqual(EtapaPrologo.Continuara, historia.Etapa, "La partida queda en continuará");
            Debug.Log("[Prueba T-133] Atajos: " + string.Join(" | ", atajos));
            AssertSinErrores();
        }

        [UnityTest]
        public IEnumerator Presente_ReanudarPeleaCasera_NoRepiteElEstadoInicial()
        {
            yield return CargarEscena(EtapaPrologo.PeleaCasera, false);
            yield return EsperarHasta(() => prologo.Iniciado, MaxSegundosEstado, "el director");
            yield return EsperarSegundos(SegundosLibre);

            Assert.AreEqual(EtapaPrologo.PeleaCasera, historia.Etapa);
            Assert.AreEqual(EtapaPrologo.PeleaCasera, prologo.Etapa);
            Assert.AreEqual(DineroDePartida, cartera.Dinero, "El dinero de la partida no se vuelve a poner en 500");
            Assert.AreEqual(vidaMecha.Maxima, vidaMecha.Actual, 0.01f, "El mecha no se vuelve a fregar");
            Assert.AreEqual(50, Registro<IRenta>.Actual.DiaProximoCobro, "La renta de la partida se queda");
            Assert.Less(DistanciaPlana(jugador.Raiz.position, Aparicion(IdCama).Pose.position), ToleranciaJugador, "El jugador está en Casa_Cama");
            Assert.IsFalse(subtitulos.SubtituloVisible, "Sin el aviso del despertar");
            Assert.IsTrue(Objetivos.HayObjetivo);
            Assert.AreEqual(TextoClandestino, Objetivos.Actual.Texto, "Con su objetivo");
            AssertSinErrores();
        }

        [UnityTest]
        public IEnumerator Presente_ReanudarContinuara_VuelveAMostrarLaPantallaYLibreNoHaceNada()
        {
            yield return CargarEscena(EtapaPrologo.Continuara, false);
            yield return EsperarHasta(() => prologo.Iniciado, MaxSegundosEstado, "el director");
            yield return EsperarSegundos(SegundosLibre);

            Assert.AreEqual(EtapaPrologo.Continuara, historia.Etapa);
            Assert.IsTrue(pantallaContinuara.Visible, "Vuelve a mostrar la pantalla");
            Assert.AreEqual(Alias, pantallaContinuara.Alias);
            Assert.AreEqual(DineroDePartida, cartera.Dinero, "Sin tocar el dinero");
            Assert.IsFalse(Objetivos.HayObjetivo);
            AssertSinErrores();
        }

        [UnityTest]
        public IEnumerator Presente_ReanudarLibre_ElDirectorNoHaceNada()
        {
            yield return CargarEscena(EtapaPrologo.Libre, false);
            yield return EsperarHasta(() => prologo.Iniciado, MaxSegundosEstado, "el director");
            Vector3 antes = jugador.Raiz.position;
            yield return EsperarSegundos(SegundosLibre);

            Assert.AreEqual(EtapaPrologo.Libre, historia.Etapa);
            Assert.IsFalse(prologo.Activo, "No lleva ninguna etapa");
            Assert.AreEqual(DineroDePartida, cartera.Dinero);
            Assert.IsFalse(Objetivos.HayObjetivo);
            Assert.IsFalse(pantallaContinuara.Visible);
            Assert.IsFalse(subtitulos.SubtituloVisible);
            Assert.Less(DistanciaPlana(antes, jugador.Raiz.position), 0.05f, "No mueve al jugador");
            AssertSinErrores();
        }

        [UnityTest]
        public IEnumerator Despertar_ConCalorEnLaPartida_LoBorra()
        {
            yield return CargarEscena(EtapaPrologo.Despertar, false, true);
            yield return null;
            ICalor calor = Registro<ICalor>.Actual;
            Assert.IsNotNull(calor, "Hay calor");
            calor.Sumar(CalorDePrueba, MotivoCalor.Otro);
            Assert.Greater(calor.Valor, 0f, "Con calor antes de despertar");
            Assert.AreEqual(DineroDePartida, cartera.Dinero, "Con el dinero de la partida");

            GameObject nuevo = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefabDirector));
            nuevo.transform.position = new Vector3(0f, -100f, 0f);
            prologo = nuevo.GetComponent<DirectorPrologo>();
            yield return EsperarHasta(() => prologo.Iniciado && historia.Etapa == EtapaPrologo.PeleaCasera, MaxSegundosEstado, "el despertar");

            Assert.AreEqual(0f, calor.Valor, "Despertar borra el calor");
            Assert.AreEqual(DineroInicial, cartera.Dinero, "Y pone el dinero inicial");
            Assert.AreEqual(0, Registro<IRenta>.Actual.Adeudo, "Y borra el adeudo de la renta");
            AssertSinErrores();
        }
    }
}
