using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using MechaCyberpunk.Combate.Vida;
using MechaCyberpunk.Core;
using MechaCyberpunk.Economia;
using MechaCyberpunk.IA.Datos;
using MechaCyberpunk.Mecha;
using MechaCyberpunk.Mecha.Encendido;
using MechaCyberpunk.Mecha.Estado;
using MechaCyberpunk.Mecha.Llamado;
using MechaCyberpunk.Mecha.Montaje;
using MechaCyberpunk.Mecha.Taller;
using MechaCyberpunk.Pelea.Emboscada;
using MechaCyberpunk.Pelea.Flujo;
using MechaCyberpunk.Personaje;
using MechaCyberpunk.Personaje.Interaccion;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Progresion;
using MechaCyberpunk.Progresion.Guardado;
using MechaCyberpunk.Progresion.Prologo;
using MechaCyberpunk.Progresion.Pruebas;
using MechaCyberpunk.UI.Creacion;
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
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Tests.Progresion
{
    /// <summary>
    /// T-135: el prólogo completo (cierre de T-F6 del lado de Tony), de CreacionPersonaje.unity a "continuará" y a la etapa libre, con
    /// entrada simulada (texto, flechas, Enter, W, ratón, M, E, F, Espacio, Q, Esc) y cambios de escena REALES (sin interceptar).
    /// Atajos permitidos y anotados: rematar a un rival con RecibirDanio tras daño real al mecha, acortar caminatas de más de 40 m
    /// con IJugador.Colocar (solo en el regreso a casa y al clandestino) y adelantar el reloj si el mostrador sigue cerrado.
    /// Los tramos de la salida y la emboscada se caminan sin atajos, siguiendo la posición del objetivo.
    /// </summary>
    public class RecorridoPrologoPruebas : InputTestFixture
    {
        protected const string RutaCreacion = "Assets/_Proyecto/Escenas/Tony/CreacionPersonaje.unity";
        protected const string RutaPrologo = "Assets/_Proyecto/Escenas/Tony/Pruebas_Prologo.unity";
        protected const string RutaFlashback = "Assets/_Proyecto/Escenas/Tony/Pruebas_Flashback.unity";
        protected const string RutaControlesAPie = "Assets/_Proyecto/Personaje/Datos/ConfigControlesAPie_Base.asset";
        protected const string RutaControlesMecha = "Assets/_Proyecto/Mecha/Datos/ConfigControlesMecha_Base.asset";
        protected const string RutaPiedad = "Assets/_Proyecto/IA/Datos/ConfigPiedadRival_Base.asset";
        protected const string IdSalida = "Clandestino_Salida";
        protected const string IdCama = "Casa_Cama";
        protected const string IdPuerta = "Casa_Puerta";
        protected const string IdCarro = "Prologo_Carro";
        protected const string Nombre = "Juan";
        protected const string AliasEscrito = "El Rayo";
        protected const string AliasGritado = "EL RAYO";
        protected const string TextoCarro = "Ve a tu carro";
        protected const string TextoRegreso = "Regresa a casa en tu carro";
        protected const string TextoCama = "Duerme en tu cama";
        protected const string TextoClandestino = "Ve al clandestino: tu primera pelea casera";
        protected const string TextoMostrador = "Habla con el encargado (E)";
        protected const string VariableCaptura = "CAPTURAR_T135";
        protected const string RutaCaptura1 = "docs/capturas/T-135-1.png";
        protected const string RutaCaptura2 = "docs/capturas/T-135-2.png";
        protected const string RutaCaptura3 = "docs/capturas/T-135-3.png";
        protected const string RutaCaptura4 = "docs/capturas/T-135-4.png";
        protected const string RutaCaptura5 = "docs/capturas/T-135-5.png";
        protected const int PremioTutorial = 1500;
        protected const int PremioCasero = 600;
        protected const int DineroInicial = 500;
        protected const int CaraElegida = 1;
        protected const int RopaElegida = 1;
        protected const int ImplanteElegido = 1;
        protected const int FramesEstables = 10;
        protected const int MaxFramesCalentamiento = 1000;
        protected const int FramesParaElHud = 8;
        protected const int FramesSinMedirTrasCaptura = 3;
        protected const int FramesSinMedirTrasEscena = 12;
        protected const int AnchoCaptura = 1280;
        protected const int AltoCaptura = 720;
        protected const int ProfundidadCaptura = 24;
        protected const int SaltoDePixeles = 97;
        protected const float VariacionMinima = 0.05f;
        protected const float DistanciaPlanoCanvas = 0.35f;
        protected const float DeltaTimeEstable = 0.05f;
        protected const float HoraNoche = 22f;
        protected const float HoraDespertar = 19.5f;
        protected const float HoraAdelantada = 20.5f;
        protected const float ToleranciaHora = 0.1f;
        protected const float MargenHora = 0.5f;
        protected const float PorcentajeDespertar = 45f;
        protected const float ToleranciaPorcentaje = 0.5f;
        protected const float MaxSegundosJuego = 240f;
        protected const float MaxSegundosEstado = 60f;
        protected const float MaxSegundosEscena = 90f;
        protected const float MaxSegundosGolpes = 60f;
        protected const float MaxSegundosTramo = 60f;
        protected const float MaxSegundosLlamado = 15f;
        protected const float MaxSegundosEncender = 30f;
        protected const float MaxSegundosPelea = 70f;
        protected const float SegundosPeleaReal = 30f;
        protected const float SegundosEntreClics = 0.25f;
        protected const float SegundosEnterEsc = 4.2f;
        protected const float SegundosMoverse = 0.6f;
        protected const float SegundosAntesCaptura3 = 2f;
        protected const float DanioMortal = 100000f;
        protected const float FraccionDanioBuscada = 0.9f;
        protected const float ToleranciaJugador = 0.6f;
        protected const float ToleranciaPunto = 0.5f;
        protected const float AlturaJugador = 0.05f;
        protected const float DistanciaLlegada = 1f;
        protected const float DistanciaJuntoAlMecha = 1.2f;
        protected const float DistanciaAlMostrador = 1.8f;
        protected const float DistanciaAlBanco = 2.2f;
        protected const float DistanciaCamaraDeLaCama = 1.8f;
        protected const float MetrosParaAtajo = 40f;
        protected const float MetrosTrasAtajo = 10f;
        protected const float MetrosAdentro = 6f;
        protected const float MetrosAdentroDeLaPuerta = 3.5f;
        protected const float MetrosMinimosAlMoverse = 0.5f;
        protected const float XEsperaLlamado = 40f;
        protected const float ZEsperaLlamado = 42f;
        protected const float GananciaGiro = 0.4f;
        protected const float AnguloParaCaminar = 60f;
        protected const float GradosMediaVuelta = 180f;
        protected const float AnguloMirarListo = 2f;
        protected const float GradosMinimosGiro = 0.5f;
        protected const float SegundosSaltarFlashback = 1f;
        protected const float MaxSegundosFlashback = 40f;

        protected Mouse raton;
        protected Keyboard teclado;
        protected string carpeta;
        protected string rutaReal;
        protected string huellaReal;
        protected JugadorAPie jugador;
        protected InteraccionAPie interaccion;
        protected MontajeMecha montaje;
        protected LlamadoMecha llamado;
        protected EncendidoMecha encendido;
        protected VidaMecha vidaMecha;
        protected MechaPersistente persistente;
        protected BancoTaller banco;
        protected DirectorPrologo prologo;
        protected DirectorPelea flujo;
        protected DirectorPelea flujoCasero;
        protected DirectorEmboscada emboscada;
        protected MostradorPeleas mostrador;
        protected PantallaContinuara pantallaContinuara;
        protected PantallaSubtitulos subtitulos;
        protected PantallaCreacion pantallaCreacion;
        protected DirectorFlashback directorFlashback;
        protected HudAPie hud;
        protected float sensibilidadAPie;
        protected float sensibilidadMecha;
        protected float piso;
        protected bool avanzando;
        protected bool vigilarVida;
        protected float vidaMinima;
        protected float yawMechaAnterior;
        protected float giroPedidoPendiente;
        protected int framesSinMedir;
        protected EtapaPrologo ultimaEtapa;
        protected readonly MedidorFrames medidor = new MedidorFrames();
        protected readonly List<string> errores = new List<string>();
        protected readonly List<string> lineas = new List<string>();
        protected readonly List<string> atajos = new List<string>();
        protected readonly List<EtapaPrologo> etapasVistas = new List<EtapaPrologo>();
        protected readonly List<string> tramos = new List<string>();
        protected HashSet<GameObject> raicesIniciales;


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
            carpeta = Path.Combine(Application.temporaryCachePath, "recorrido_prologo_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(carpeta);
            ServicioGuardado.RutaForzada = Path.Combine(carpeta, "partida.json");
            rutaReal = Path.Combine(Application.persistentDataPath, "partida.json");
            huellaReal = HuellaDe(rutaReal);
            CambioEscena.Interceptar(null);
            CambioEscena.ConsumirTransicion();
            errores.Clear();
            lineas.Clear();
            atajos.Clear();
            etapasVistas.Clear();
            tramos.Clear();
            medidor.Reiniciar();
            avanzando = false;
            vigilarVida = true;
            vidaMinima = float.MaxValue;
            giroPedidoPendiente = 0f;
            framesSinMedir = 0;
            ultimaEtapa = EtapaPrologo.Ninguna;
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

        protected void RegistrarError(string mensaje, string pila, LogType tipo)
        {
            if (tipo == LogType.Error || tipo == LogType.Exception || tipo == LogType.Assert)
            {
                errores.Add(mensaje);
            }
        }

        protected static string HuellaDe(string ruta)
        {
            if (!File.Exists(ruta))
            {
                return "no existe";
            }
            FileInfo info = new FileInfo(ruta);
            return info.Length + " bytes, " + info.LastWriteTimeUtc.Ticks;
        }

        // ---- Esperas y medición ----

        // Cada cuadro del recorrido pasa por aquí: mide el frame, sigue la vida del mecha y anota los cambios de etapa.
        protected void Vigilar()
        {
            if (framesSinMedir > 0)
            {
                framesSinMedir--;
            }
            else
            {
                medidor.Registrar(Time.unscaledDeltaTime);
            }
            if (vigilarVida && vidaMecha != null)
            {
                vidaMinima = Mathf.Min(vidaMinima, vidaMecha.Actual);
            }
            VigilarEtapa();
        }

        // Anota cada etapa nueva de la historia (también durante las cargas de escena, donde no se mide el frame).
        protected void VigilarEtapa()
        {
            IHistoria historia = Registro<IHistoria>.Actual;
            if (historia != null && historia.Etapa != EtapaPrologo.Ninguna && historia.Etapa != ultimaEtapa)
            {
                ultimaEtapa = historia.Etapa;
                etapasVistas.Add(ultimaEtapa);
            }
        }

        protected IEnumerator Cuadros(int cuadros, bool estables)
        {
            int cuenta = 0;
            int intentos = 0;
            while (cuenta < cuadros && intentos < MaxFramesCalentamiento)
            {
                yield return null;
                intentos++;
                cuenta = !estables || Time.deltaTime < DeltaTimeEstable ? cuenta + 1 : 0;
                Vigilar();
            }
        }

        protected IEnumerator EsperarSegundos(float segundos)
        {
            float fin = Time.time + segundos;
            while (Time.time < fin)
            {
                yield return null;
                Vigilar();
            }
        }

        protected IEnumerator EsperarHasta(Func<bool> condicion, float maxSegundos, string que)
        {
            float fin = Time.time + maxSegundos;
            while (!condicion() && Time.time < fin)
            {
                yield return null;
                Vigilar();
            }
            Assert.IsTrue(condicion(), "Se esperaba: " + que);
        }

        protected IEnumerator Tecla(KeyControl tecla)
        {
            Press(tecla);
            yield return null;
            Vigilar();
            Release(tecla);
            yield return null;
            Vigilar();
        }

        protected IEnumerator Escribir(string texto)
        {
            foreach (char letra in texto)
            {
                InputSystem.QueueTextEvent(teclado, letra);
            }
            yield return null;
            Vigilar();
            yield return null;
            Vigilar();
        }

        protected void AnotarTramo(string nombre, float inicio)
        {
            tramos.Add(nombre + ": " + (Time.time - inicio).ToString("F1", CultureInfo.InvariantCulture) + " s");
        }

        // Espera a que la escena pedida sea la activa (cambio real) y deja pasar unos cuadros sin medir el tirón de la carga.
        protected IEnumerator EsperarEscena(string ruta, string que)
        {
            float fin = Time.realtimeSinceStartup + MaxSegundosEscena;
            while (SceneManager.GetActiveScene().path != ruta && Time.realtimeSinceStartup < fin)
            {
                yield return null;
                VigilarEtapa();
            }
            Assert.AreEqual(ruta, SceneManager.GetActiveScene().path, "Se cargó la escena: " + que);
            framesSinMedir = FramesSinMedirTrasEscena;
            yield return Cuadros(FramesEstables, true);
            Assert.AreEqual(ServicioGuardado.RutaForzada, ServicioGuardado.Actual.RutaArchivo, "La escena guarda en la ruta temporal");
            Assert.AreEqual(1, OyentesActivos(), "Un solo AudioListener activo en " + que);
        }

        protected static int OyentesActivos()
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

        // ---- Enlaces con la escena actual ----

        protected IEnumerator EntrarAPrologo(string que)
        {
            yield return EsperarEscena(EscenaPrologo, que);
            yield return EsperarHasta(() => Object.FindFirstObjectByType<JugadorAPie>() != null, MaxSegundosEstado, "el jugador de " + que);
            EnlazarPrologo();
            Assert.AreEqual(1, Registro<IJugador>.Cantidad, "Un solo IJugador en " + que);
        }

        protected void EnlazarPrologo()
        {
            sensibilidadAPie = AssetDatabase.LoadAssetAtPath<ConfigControlesAPie>(RutaControlesAPie).SensibilidadRaton;
            sensibilidadMecha = AssetDatabase.LoadAssetAtPath<ConfigControlesMecha>(RutaControlesMecha).SensibilidadRaton;
            jugador = Object.FindFirstObjectByType<JugadorAPie>();
            prologo = Object.FindFirstObjectByType<DirectorPrologo>();
            montaje = Object.FindFirstObjectByType<MontajeMecha>();
            emboscada = Object.FindFirstObjectByType<DirectorEmboscada>();
            mostrador = Object.FindFirstObjectByType<MostradorPeleas>();
            hud = Object.FindFirstObjectByType<HudAPie>();
            interaccion = Object.FindFirstObjectByType<InteraccionAPie>();
            Assert.IsNotNull(jugador, "Pruebas_Prologo trae al jugador");
            Assert.IsNotNull(prologo, "Pruebas_Prologo trae el director del prólogo");
            Assert.IsNotNull(montaje, "Pruebas_Prologo trae el mecha de la historia");
            Assert.IsNotNull(emboscada, "Pruebas_Prologo trae la emboscada");
            Assert.IsNotNull(mostrador, "Pruebas_Prologo trae el mostrador");
            Assert.IsNotNull(hud, "El jugador trae el HUD a pie");
            Assert.IsNotNull(interaccion, "El jugador trae la interacción");
            flujo = prologo.GetComponentInChildren<DirectorPelea>(true);
            flujoCasero = mostrador.GetComponentInChildren<DirectorPelea>(true);
            pantallaContinuara = prologo.GetComponentInChildren<PantallaContinuara>(true);
            subtitulos = prologo.GetComponentInChildren<PantallaSubtitulos>(true);
            Assert.IsNotNull(flujo, "El director trae el flujo del tutorial");
            Assert.IsNotNull(flujoCasero, "El mostrador trae el flujo casero");
            Assert.IsNotNull(pantallaContinuara, "El director trae la pantalla de continuará");
            Assert.IsNotNull(subtitulos, "El director trae los subtítulos");
            llamado = montaje.GetComponent<LlamadoMecha>();
            encendido = montaje.GetComponent<EncendidoMecha>();
            vidaMecha = montaje.GetComponent<VidaMecha>();
            persistente = montaje.GetComponent<MechaPersistente>();
            banco = montaje.GetComponent<BancoTaller>();
            Assert.IsNotNull(persistente, "El mecha de la historia es persistente");
            Assert.IsNotNull(banco, "El mecha de la historia trae el banco del taller");
            piso = AssetDatabase.LoadAssetAtPath<ConfigPiedadRival>(RutaPiedad).FraccionPiso * vidaMecha.Maxima;
            flujo.LineaDicha += lineas.Add;
            avanzando = false;
        }

        protected static IObjetivos Objetivos => Registro<IObjetivos>.Actual;
        protected static ICartera Cartera => Registro<ICartera>.Actual;
        protected static IRelojMundo Reloj => Registro<IRelojMundo>.Actual;
        protected static ServicioHistoria Historia => Registro<IHistoria>.Actual as ServicioHistoria;

        protected static float DistanciaPlana(Vector3 a, Vector3 b)
        {
            Vector3 delta = a - b;
            delta.y = 0f;
            return delta.magnitude;
        }

        protected static IPuntoHistoria PuntoHistoria(string id)
        {
            foreach (IPuntoHistoria punto in Registro<IPuntoHistoria>.Todos)
            {
                if (punto.Id == id)
                {
                    return punto;
                }
            }
            return null;
        }

        protected static IPuntoAparicion Aparicion(string id)
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

        protected static ILugar LugarCasa()
        {
            foreach (ILugar lugar in Registro<ILugar>.Todos)
            {
                if (lugar.Id == IdLugar.Casa)
                {
                    return lugar;
                }
            }
            return null;
        }

        protected static void Matar(IPeleador peleador)
        {
            IRecibeDanio receptor = peleador.Vida as IRecibeDanio ?? peleador.Raiz.GetComponentInChildren<IRecibeDanio>();
            Assert.IsNotNull(receptor, "El rival recibe daño");
            receptor.RecibirDanio(new InfoDanio(DanioMortal, TipoDanio.Golpe, Vector3.zero, Vector3.forward, null));
        }

        protected void VerificarObjetivo(string texto, Vector3 posicion)
        {
            Assert.IsTrue(Objetivos.HayObjetivo, "Hay objetivo: " + texto);
            Assert.AreEqual(texto, Objetivos.Actual.Texto);
            Assert.IsTrue(Objetivos.Actual.TienePosicion, "Apunta a una posición");
            Assert.Less(Vector3.Distance(Objetivos.Actual.Posicion, posicion), ToleranciaPunto, "En el punto esperado");
        }

        // ---- Entrada: caminar ----

        protected void Avanzar(bool avanzar)
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

        protected float AnguloHacia(Vector3 destino)
        {
            Transform camara = jugador.Camara.transform;
            return CalculoFlechaObjetivo.Angulo(camara.position, camara.forward, destino);
        }

        // Un cuadro de caminata hacia un punto: gira con el ratón en proporción al ángulo y avanza si ya mira hacia allá.
        protected void PasoHacia(Vector3 destino)
        {
            float angulo = AnguloHacia(destino);
            float delta = Mathf.Clamp(angulo * GananciaGiro, -GradosMediaVuelta, GradosMediaVuelta) / sensibilidadAPie;
            Set(raton.delta, new Vector2(delta, 0f));
            Avanzar(Mathf.Abs(angulo) < AnguloParaCaminar);
        }

        protected IEnumerator CaminarA(Vector3 destino, float distancia, string que, Func<bool> antes = null)
        {
            float fin = Time.time + MaxSegundosTramo;
            while (DistanciaPlana(jugador.Raiz.position, destino) > distancia && (antes == null || !antes()))
            {
                if (Time.time > fin)
                {
                    Avanzar(false);
                    Assert.Fail("No llegó a: " + que + " (posición " + jugador.Raiz.position + ")");
                }
                PasoHacia(destino);
                yield return null;
                Vigilar();
            }
            Avanzar(false);
            yield return null;
            Vigilar();
        }

        // Camina SIN atajos hacia donde apunta la flecha del objetivo (se relee cada cuadro) hasta que se cumpla 'hasta'.
        protected IEnumerator CaminarSiguiendoElObjetivo(Func<bool> hasta, string que)
        {
            float fin = Time.time + MaxSegundosTramo;
            while (!hasta())
            {
                if (Time.time > fin || !Objetivos.HayObjetivo || !Objetivos.Actual.TienePosicion)
                {
                    Avanzar(false);
                    Assert.Fail("No se cumplió: " + que + " (posición " + jugador.Raiz.position + ", objetivo "
                        + (Objetivos.HayObjetivo ? Objetivos.Actual.Texto : "ninguno") + ")");
                }
                PasoHacia(Objetivos.Actual.Posicion);
                yield return null;
                Vigilar();
            }
            Avanzar(false);
            yield return null;
            Vigilar();
        }

        // Atajo anotado: una caminata de más de 40 m se acorta con Colocar hasta 10 m antes del objetivo, mirando a él.
        protected IEnumerator CaminarOAcortar(Vector3 destino, float distancia, string que, Func<bool> antes = null)
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
                atajos.Add("Colocar (" + que + "): caminata de " + lejos.ToString("F0", CultureInfo.InvariantCulture) + " m acortada a "
                    + MetrosTrasAtajo.ToString("F0", CultureInfo.InvariantCulture) + " m");
                yield return null;
                Vigilar();
            }
            yield return CaminarA(destino, distancia, que, antes);
        }

        protected IEnumerator MirarA(Vector3 destino, string que)
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
                Vigilar();
            }
            yield return null;
            Vigilar();
        }

        // ---- Entrada: mecha ----

        protected IEnumerator Encender()
        {
            PasoEncendido ultimoTocado = PasoEncendido.Apagado;
            bool espacioPulsado = false;
            float fin = Time.time + MaxSegundosEncender;
            while (!encendido.EstaEncendido && Time.time < fin)
            {
                PasoEncendido paso = encendido.Paso;
                if (paso == PasoEncendido.Paso1 && ultimoTocado != PasoEncendido.Paso1)
                {
                    ultimoTocado = PasoEncendido.Paso1;
                    yield return Tecla(teclado.fKey);
                    continue;
                }
                if (paso == PasoEncendido.Paso2 && !espacioPulsado)
                {
                    espacioPulsado = true;
                    Press(teclado.spaceKey);
                }
                if (paso == PasoEncendido.Paso3)
                {
                    if (espacioPulsado)
                    {
                        espacioPulsado = false;
                        Release(teclado.spaceKey);
                        // Un cuadro: el siguiente Press copia el estado del teclado y, sin procesar este Release, volvería a pegar Espacio.
                        yield return null;
                        Vigilar();
                    }
                    if (ultimoTocado != PasoEncendido.Paso3)
                    {
                        ultimoTocado = PasoEncendido.Paso3;
                        yield return Tecla(teclado.qKey);
                        continue;
                    }
                }
                yield return null;
                Vigilar();
            }
            if (espacioPulsado)
            {
                Release(teclado.spaceKey);
            }
            Assert.IsTrue(encendido.EstaEncendido, "La cabina quedó encendida");
            yield return null;
            Vigilar();
            yawMechaAnterior = montaje.transform.eulerAngles.y;
        }

        protected static bool Existe(IPeleador peleador)
        {
            return peleador is Component componente && componente != null;
        }

        protected List<IPeleador> RivalesEnPie(IReadOnlyList<IPeleador> rivales)
        {
            List<IPeleador> lista = new List<IPeleador>();
            foreach (IPeleador rival in rivales)
            {
                if (Existe(rival) && !rival.Vida.EstaKO)
                {
                    lista.Add(rival);
                }
            }
            return lista;
        }

        protected IPeleador MasCercano(List<IPeleador> rivales)
        {
            IPeleador mejor = null;
            float distanciaMejor = float.MaxValue;
            foreach (IPeleador rival in rivales)
            {
                float distancia = DistanciaPlana(rival.Raiz.position, montaje.transform.position);
                if (distancia < distanciaMejor)
                {
                    distanciaMejor = distancia;
                    mejor = rival;
                }
            }
            return mejor;
        }

        // Un cuadro de pelea: el mecha gira hacia el rival con delta de ratón proporcional al ángulo que falta y hace clic seguido.
        protected void ApuntarYGolpear(IPeleador rival, bool clic)
        {
            float yaw = montaje.transform.eulerAngles.y;
            giroPedidoPendiente -= Mathf.DeltaAngle(yawMechaAnterior, yaw);
            yawMechaAnterior = yaw;

            Vector3 hacia = rival.Raiz.position - montaje.transform.position;
            hacia.y = 0f;
            float angulo = Vector3.SignedAngle(montaje.transform.forward, hacia, Vector3.up);
            float falta = angulo - giroPedidoPendiente;
            if (Mathf.Abs(falta) > GradosMinimosGiro)
            {
                Set(raton.delta, new Vector2(falta / sensibilidadMecha, 0f));
                giroPedidoPendiente += falta;
            }
            if (clic)
            {
                Press(raton.leftButton);
            }
            else
            {
                Release(raton.leftButton);
            }
        }

        // Clics al aire hasta que se cumpla la condición (o se acabe el tiempo): el rival golpea de verdad.
        protected IEnumerator ClicsHasta(Func<bool> condicion)
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
                Vigilar();
            }
            Release(raton.leftButton);
            yield return null;
            Vigilar();
        }

        // ---- Capturas (solo con CAPTURAR_T135=1) ----

        protected virtual bool CapturaPedida => Environment.GetEnvironmentVariable(VariableCaptura) == "1";
        protected virtual string EscenaPrologo => RutaPrologo;
        protected virtual string EscenaFlashback => RutaFlashback;
        protected virtual float LimiteSegundosJuego => MaxSegundosJuego;

        // Los lienzos en Overlay no salen en una cámara a textura: se pasan un instante a ScreenSpaceCamera con esa cámara.
        // T-136: CreacionPersonaje.unity trae DatosEscenas_Demo (las escenas de Alan). T-135 sigue con las de Tony: se cambia el dato en memoria.
        protected virtual string RutaDatosEscenas => "Assets/_Proyecto/Progresion/Datos/DatosEscenas_Pruebas.asset";

        protected void UsarEscenasDeLaPrueba()
        {
            DatosEscenas datos = AssetDatabase.LoadAssetAtPath<DatosEscenas>(RutaDatosEscenas);
            Assert.IsNotNull(datos, "Existe " + RutaDatosEscenas);
            typeof(PantallaCreacion).GetField("datosEscenas", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(pantallaCreacion, datos);
        }

        protected virtual string RutaDeCaptura(string ruta)
        {
            return ruta;
        }

        protected void Capturar(Camera camara, string ruta, bool camaraPropia = false)
        {
            ruta = RutaDeCaptura(ruta);
            if (ruta == null || !CapturaPedida)
            {
                return;
            }
            GameObject propia = null;
            if (camaraPropia)
            {
                propia = new GameObject("CamCaptura", typeof(Camera));
                camara = propia.GetComponent<Camera>();
                camara.clearFlags = CameraClearFlags.SolidColor;
                camara.backgroundColor = Color.black;
            }
            List<Canvas> movidos = new List<Canvas>();
            foreach (Canvas lienzo in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                if (lienzo.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    lienzo.renderMode = RenderMode.ScreenSpaceCamera;
                    lienzo.worldCamera = camara;
                    lienzo.planeDistance = camaraPropia ? 1f : DistanciaPlanoCanvas;
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
            if (propia != null)
            {
                Object.Destroy(propia);
            }
            framesSinMedir = FramesSinMedirTrasCaptura;
            Assert.Greater(maximo - minimo, VariacionMinima, "La captura es plana: " + ruta);
        }

        // ---- Tramos ----

        // a) Creación de personaje en CreacionPersonaje.unity: nombre, alias, cara, ropa, implante y EMPEZAR.
        protected IEnumerator CrearPersonaje()
        {
            float inicio = Time.time;
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(RutaCreacion, new LoadSceneParameters(LoadSceneMode.Single));
            framesSinMedir = FramesSinMedirTrasEscena;
            yield return Cuadros(FramesEstables, true);
            pantallaCreacion = Object.FindFirstObjectByType<PantallaCreacion>();
            Assert.IsNotNull(pantallaCreacion, "CreacionPersonaje trae PantallaCreacion");
            UsarEscenasDeLaPrueba();
            Assert.AreEqual(PantallaCreacion.Panel.Creacion, pantallaCreacion.PanelActual, "Sin partida arranca en la creación");
            Assert.AreEqual(1, OyentesActivos(), "Un solo AudioListener en la creación");

            yield return Escribir(Nombre);
            Assert.AreEqual(Nombre, pantallaCreacion.Nombre);
            yield return Tecla(teclado.tabKey);
            Assert.AreEqual(PantallaCreacion.FilaAlias, pantallaCreacion.Fila);
            yield return Escribir(AliasEscrito);
            Assert.AreEqual(AliasEscrito, pantallaCreacion.Alias);
            yield return Tecla(teclado.tabKey);
            yield return Tecla(teclado.rightArrowKey);
            Assert.AreEqual(CaraElegida, pantallaCreacion.Cara, "Cambió la cara");
            yield return Tecla(teclado.downArrowKey);
            yield return Tecla(teclado.rightArrowKey);
            Assert.AreEqual(RopaElegida, pantallaCreacion.Ropa, "Cambió la ropa");
            yield return Tecla(teclado.downArrowKey);
            yield return Tecla(teclado.rightArrowKey);
            Assert.AreEqual(ImplanteElegido, pantallaCreacion.Implante, "Cambió el implante");
            yield return Tecla(teclado.downArrowKey);
            Assert.AreEqual(PantallaCreacion.FilaEmpezar, pantallaCreacion.Fila, "Listo para EMPEZAR");
            yield return Cuadros(5, false);
            Capturar(null, RutaCaptura1, true);
            yield return Cuadros(2, false);

            yield return Tecla(teclado.enterKey);
            yield return EntrarAPrologo("el prólogo tras la creación");
            AnotarTramo("a) creación de personaje", inicio);

            IPerfilJugador perfil = Registro<IPerfilJugador>.Actual;
            Assert.AreEqual(Nombre, perfil.Nombre, "El perfil conserva el nombre");
            Assert.AreEqual(AliasEscrito, perfil.Alias, "El perfil conserva el alias");
            Assert.AreEqual(new AparienciaJugador(CaraElegida, RopaElegida, ImplanteElegido), perfil.Apariencia, "Y la apariencia elegida");
            Assert.AreEqual(EtapaPrologo.PeleaTutorial, Historia.Etapa, "Empieza en la pelea del tutorial");
        }

        // b) La pelea de campeonato: la presentación grita el alias, el jugador pelea y gana; sale a pie.
        protected IEnumerator PelearElCampeonato()
        {
            float inicio = Time.time;
            yield return EsperarHasta(() => jugador.Modo == ModoJugador.Mecha, MaxSegundosEstado, "el jugador va en el mecha");
            Assert.AreEqual(EtapaPrologo.PeleaTutorial, Historia.Etapa, "La etapa es la pelea del tutorial");
            Assert.IsTrue(Reloj.EsDeNoche, "Es de noche");
            Assert.AreEqual(HoraNoche, Reloj.Hora, MargenHora, "Son las 22:00");
            Assert.IsFalse(emboscada.Habilitada, "En la pelea la emboscada está apagada");
            Assert.AreEqual(EstadoLlamado.EnArena, llamado.Estado, "El mecha está en la arena");
            int dineroAntes = Cartera.Dinero;

            yield return EsperarHasta(() => lineas.Count >= 4, MaxSegundosEstado, "las cuatro líneas del presentador");
            StringAssert.Contains(AliasGritado, lineas[3], "El grito de la presentación lleva el alias del jugador");
            yield return Cuadros(FramesParaElHud, false);
            Capturar(jugador.Camara, RutaCaptura2);
            yield return EsperarHasta(() => flujo.Estado == EstadoFlujoPelea.Peleando, MaxSegundosEstado, "la pelea");

            yield return RecibirGolpesReales();
            Matar(flujo.Rival);
            atajos.Add("RecibirDanio: rival del tutorial rematado tras daño real al mecha");
            yield return EsperarHasta(() => flujo.Estado == EstadoFlujoPelea.Resultado, MaxSegundosEstado, "el resultado");
            yield return EsperarHasta(() => Historia.Etapa == EtapaPrologo.SalidaClandestino, MaxSegundosEstado, "la etapa de salida");
            yield return null;
            Vigilar();

            Assert.AreEqual(dineroAntes + PremioTutorial, Cartera.Dinero, "Ganó el premio de $1,500");
            Assert.AreEqual(ModoJugador.APie, jugador.Modo, "El jugador quedó a pie");
            IPuntoAparicion salida = Aparicion(IdSalida);
            Assert.IsNotNull(salida, "Existe Clandestino_Salida");
            Assert.Less(DistanciaPlana(jugador.Raiz.position, salida.Pose.position), ToleranciaJugador, "A pie en Clandestino_Salida");
            Assert.IsTrue(emboscada.Habilitada, "Saliendo del clandestino la emboscada se prende");
            Assert.AreEqual(EstadoEmboscada.Esperando, emboscada.Estado);
            VerificarObjetivo(TextoCarro, PuntoHistoria(IdCarro).Pose.position);
            AnotarTramo("b) pelea de campeonato", inicio);
        }

        protected IEnumerator RecibirGolpesReales()
        {
            float limite = vidaMecha.Maxima * FraccionDanioBuscada;
            yield return ClicsHasta(() => vidaMecha.Actual <= limite || vidaMecha.EstaKO);
            Assert.Less(vidaMecha.Actual, vidaMecha.Maxima, "El jugador recibió golpes reales");
            Assert.IsFalse(vidaMecha.EstaKO, "Y no perdió");
        }

        // c) Salida y emboscada: camina siguiendo la flecha, se activa la pandilla, M, E, encendido y gana.
        protected float metrosHastaEmboscada;

        // Camina siguiendo la flecha hasta que la emboscada se activa.
        protected virtual IEnumerator LlegarALaEmboscada()
        {
            Vector3 puntoInicial = jugador.Raiz.position;
            yield return CaminarSiguiendoElObjetivo(() => emboscada.Estado != EstadoEmboscada.Esperando, "la emboscada se activa al acercarse al callejón");
            metrosHastaEmboscada = DistanciaPlana(puntoInicial, jugador.Raiz.position);
        }

        protected virtual IEnumerator IrALaZonaDeLlamado()
        {
            yield return CaminarA(new Vector3(XEsperaLlamado, AlturaJugador, ZEsperaLlamado), DistanciaLlegada, "la zona de llamado");
        }

        protected virtual IEnumerator SalirYSuperarLaEmboscada()
        {
            float inicio = Time.time;
            yield return Cuadros(FramesParaElHud, false);
            Assert.AreEqual(TextoCarro, hud.TextoObjetivo.text, "El HUD dice 'Ve a tu carro'");
            Assert.IsTrue(hud.GrupoObjetivo.activeInHierarchy, "El objetivo está a la vista");
            Assert.IsTrue(hud.Flecha.gameObject.activeInHierarchy, "Con su flecha");

            yield return LlegarALaEmboscada();
            Assert.AreNotEqual(EstadoEmboscada.Esperando, emboscada.Estado, "Al acercarse al callejón la pandilla se activa");
            yield return null;
            Vigilar();
            Assert.AreEqual(EtapaPrologo.Emboscada, Historia.Etapa, "La etapa es la emboscada");
            Assert.IsFalse(emboscada.Habilitada, "Ya empezó: queda apagada");
            yield return EsperarHasta(() => emboscada.Estado == EstadoEmboscada.EsperandoMecha, MaxSegundosEstado, "la pandilla termina de aparecer");
            float finCaminata = Time.time;
            Debug.Log("[Prueba T-135] Caminata sin atajos hasta la emboscada: " + metrosHastaEmboscada.ToString("F0", CultureInfo.InvariantCulture) + " m en "
                + (finCaminata - inicio).ToString("F1", CultureInfo.InvariantCulture) + " s");

            yield return IrALaZonaDeLlamado();
            yield return Tecla(teclado.mKey);
            Assert.AreEqual(EstadoLlamado.Llegando, llamado.Estado, "M dentro de la zona llama al mecha");
            yield return EsperarHasta(() => llamado.Estado == EstadoLlamado.EnCalle, MaxSegundosLlamado, "el mecha llega");

            yield return CaminarA(montaje.PuntoSubida, DistanciaJuntoAlMecha, "junto al mecha");
            yield return MirarA(montaje.PuntoSubida, "la escotilla del mecha");
            yield return EsperarHasta(() => interaccion.TipoCandidato == TipoCandidatoAPie.Montable, 3f, "la interacción ofrece el mecha");
            yield return Tecla(teclado.eKey);
            Assert.AreEqual(ModoJugador.Mecha, jugador.Modo, "E sube al mecha");
            yield return Encender();
            Assert.AreEqual(EstadoEmboscada.Peleando, emboscada.Estado, "Con el mecha encendido la pandilla pelea");

            int terminadas = 0;
            emboscada.Terminada += () => terminadas++;
            giroPedidoPendiente = 0f;
            yawMechaAnterior = montaje.transform.eulerAngles.y;
            bool clic = false;
            bool capturada = false;
            float proximoClic = 0f;
            float inicioPelea = Time.time;
            float fin = Time.time + MaxSegundosPelea;
            bool rematados = false;
            while (emboscada.Estado != EstadoEmboscada.Superada && Time.time < fin)
            {
                List<IPeleador> enPie = RivalesEnPie(emboscada.Rivales);
                IPeleador rival = MasCercano(enPie);
                if (!capturada && enPie.Count > 0 && Time.time - inicioPelea > SegundosAntesCaptura3)
                {
                    capturada = true;
                    Capturar(jugador.Camara, RutaCaptura3);
                }
                if (!rematados && Time.time - inicioPelea > SegundosPeleaReal && vidaMecha.Actual < vidaMecha.Maxima && enPie.Count > 0)
                {
                    rematados = true;
                    foreach (IPeleador restante in enPie)
                    {
                        Matar(restante);
                    }
                    atajos.Add("RecibirDanio: " + enPie.Count + " pandillero(s) rematados tras daño real al mecha");
                }
                else if (rival != null)
                {
                    if (Time.time >= proximoClic)
                    {
                        clic = !clic;
                        proximoClic = Time.time + SegundosEntreClics / 2f;
                    }
                    ApuntarYGolpear(rival, clic);
                }
                yield return null;
                Vigilar();
            }
            Release(raton.leftButton);
            yield return null;
            Vigilar();
            Assert.AreEqual(EstadoEmboscada.Superada, emboscada.Estado, "La pandilla cayó");
            yield return null;
            Vigilar();
            Assert.AreEqual(1, terminadas, "Terminada llega una sola vez");
            Assert.AreEqual(EtapaPrologo.RegresoACasa, Historia.Etapa, "La etapa es el regreso a casa");
            VerificarObjetivo(TextoRegreso, PuntoHistoria("Prologo_Esquina_1").Pose.position);
            AnotarTramo("c) salida y emboscada (caminata " + (finCaminata - inicio).ToString("F1", CultureInfo.InvariantCulture) + " s)", inicio);
        }

        protected static bool HayObjetivoDeCasa()
        {
            return Objetivos.HayObjetivo && !Objetivos.Actual.TienePosicion && Objetivos.Actual.Lugar == IdLugar.Casa;
        }

        // d) Regreso: baja del mecha y lo devuelve (M), camina a la esquina y a la casa; al entrar la etapa pasa a Dormir.
        // Pruebas_Prologo no trae carro (es de Alan, T-136): se regresa a pie siguiendo el mismo objetivo.
        protected virtual IEnumerator RegresarACasa()
        {
            float inicio = Time.time;
            yield return EsperarHasta(() => montaje.PuedeBajar, 5f, "el mecha puede bajar (detenido)");
            yield return Tecla(teclado.eKey);
            Assert.AreEqual(ModoJugador.APie, jugador.Modo, "E baja del mecha");
            yield return Tecla(teclado.mKey);

            Vector3 esquina = PuntoHistoria("Prologo_Esquina_1").Pose.position;
            yield return CaminarOAcortar(esquina, DistanciaLlegada, "la esquina", () => HayObjetivoDeCasa());
            yield return EsperarHasta(() => HayObjetivoDeCasa(), 3f, "el objetivo pasa a la casa");
            Assert.AreEqual(EtapaPrologo.RegresoACasa, Historia.Etapa, "Sigue el regreso a casa");
            Assert.AreEqual(TextoRegreso, Objetivos.Actual.Texto);
            Assert.AreEqual(IdLugar.Casa, Objetivos.Actual.Lugar);

            ILugar casa = LugarCasa();
            Assert.IsNotNull(casa, "Existe el lugar Casa");
            yield return CaminarOAcortar(casa.PosicionEntrada, DistanciaLlegada, "la puerta de la casa");
            CamaJugador cama = Object.FindFirstObjectByType<CamaJugador>();
            Assert.IsNotNull(cama, "Pruebas_Prologo trae la cama");
            yield return CaminarA(cama.Posicion - new Vector3(0f, 0f, DistanciaCamaraDeLaCama), DistanciaLlegada, "dentro de la casa",
                () => Historia.Etapa == EtapaPrologo.Dormir);
            yield return EsperarHasta(() => Historia.Etapa == EtapaPrologo.Dormir, 3f, "entrar a la casa lleva a dormir");
            yield return null;
            Vigilar();
            Assert.AreEqual(TextoCama, Objetivos.Actual.Texto, "El objetivo es la cama");
            Assert.IsTrue(Objetivos.Actual.TienePosicion);
            Assert.Less(DistanciaPlana(Objetivos.Actual.Posicion, cama.Posicion), ToleranciaPunto, "En la cama");
            AnotarTramo("d) regreso a casa", inicio);
        }

        // Deja al jugador mirando la cama a distancia de interacción.
        protected virtual IEnumerator IrALaCama()
        {
            CamaJugador cama = Object.FindFirstObjectByType<CamaJugador>();
            yield return CaminarA(cama.Posicion - new Vector3(0f, 0f, DistanciaCamaraDeLaCama), DistanciaLlegada, "junto a la cama");
            yield return MirarA(cama.Posicion, "la cama");
        }

        // e) Dormir: E en la cama carga el flashback real; mira el primer plano completo y salta el resto con Espacio mantenido.
        protected virtual IEnumerator DormirYVerElFlashback()
        {
            float inicio = Time.time;
            yield return IrALaCama();
            yield return EsperarHasta(() => interaccion.TipoCandidato == TipoCandidatoAPie.Interactuable, 3f, "la interacción ofrece la cama");
            yield return EsperarHasta(() => hud.TextoAvisoInteraccion.text.Contains("Dormir"), 3f, "el HUD ofrece dormir");
            yield return Tecla(teclado.eKey);
            AnotarTramo("e1) ir a la cama y dormir", inicio);

            float inicioFlashback = Time.time;
            yield return EsperarEscena(EscenaFlashback, "el flashback");
            directorFlashback = Object.FindFirstObjectByType<DirectorFlashback>();
            Assert.IsNotNull(directorFlashback, "Pruebas_Flashback trae el director");
            Assert.AreEqual(1, CamarasActivas(), "Una sola cámara activa en el flashback");
            Assert.AreEqual(EtapaPrologo.Flashback, Historia.Etapa, "La etapa es el flashback");
            yield return EsperarHasta(() => directorFlashback.IndicePlano >= 0, MaxSegundosFlashback, "el primer plano");
            yield return EsperarHasta(() => PantallaFundido.Actual != null && PantallaFundido.Actual.Alfa < 0.001f, MaxSegundosFlashback, "termina el fundido de entrada");
            Assert.IsTrue(directorFlashback.Subtitulos.SubtituloVisible, "El primer plano trae subtítulo");
            Capturar(directorFlashback.Camara, RutaCaptura4);

            // Mira el primer plano completo; cuando empieza el segundo, salta el resto manteniendo Espacio.
            yield return EsperarHasta(() => directorFlashback.IndicePlano >= 1, MaxSegundosFlashback, "pasa al segundo plano");
            Assert.IsFalse(teclado.spaceKey.isPressed, "Espacio no quedó pegado de la cabina (el flashback necesita un cambio nuevo de la tecla)");
            float inicioSalto = Time.time;
            Press(teclado.spaceKey);
            float fin = Time.time + MaxSegundosFlashback;
            bool saltado = false;
            while (!saltado && Time.time < fin)
            {
                yield return null;
                Vigilar();
                // Al saltar el director cambia de escena y se destruye: se lee antes de que eso pase.
                saltado = directorFlashback == null || directorFlashback.Saltado;
            }
            Release(teclado.spaceKey);
            Assert.IsTrue(saltado, "Mantener Espacio salta el resto del flashback");
            Assert.GreaterOrEqual(Time.time - inicioSalto, SegundosSaltarFlashback - 0.1f, "No salta antes de mantener un segundo");
            float segundosFlashback = Time.time - inicioFlashback;
            tramos.Add("e2) flashback (primer plano completo + salto): " + segundosFlashback.ToString("F1", CultureInfo.InvariantCulture) + " s");
        }

        protected static int CamarasActivas()
        {
            int camaras = 0;
            foreach (Camera camara in Object.FindObjectsByType<Camera>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (camara.enabled && camara.gameObject.activeInHierarchy)
                {
                    camaras++;
                }
            }
            return camaras;
        }

        // f) El presente: despierta, repara en el banco, va al clandestino y gana la casera.
        protected virtual IEnumerator DespertarReparaYPelearLaCasera()
        {
            float inicio = Time.time;
            yield return EntrarAPrologo("el prólogo tras el flashback");
            yield return EsperarHasta(() => prologo.Iniciado && Historia.Etapa == EtapaPrologo.PeleaCasera, MaxSegundosEstado, "el despertar");

            IPuntoAparicion cama = Aparicion(IdCama);
            Assert.IsNotNull(cama, "Existe Casa_Cama");
            Assert.Less(DistanciaPlana(jugador.Raiz.position, cama.Pose.position), ToleranciaJugador - 0.1f, "El jugador despierta en Casa_Cama");
            Assert.AreEqual(ModoJugador.APie, jugador.Modo);
            Assert.AreEqual(DineroInicial, Cartera.Dinero, "Despierta con $500");
            Assert.AreEqual(PorcentajeDespertar, vidaMecha.Actual / vidaMecha.Maxima * 100f, ToleranciaPorcentaje, "Mecha al 45 %");
            Assert.AreEqual(EstadoLlamado.EnTaller, llamado.Estado, "Y en su taller");
            Assert.AreEqual(HoraDespertar, Reloj.Hora, ToleranciaHora, "Son las 19:30");
            Assert.AreEqual(0f, Registro<ICalor>.Actual.Valor, "Calor en 0");
            Assert.IsTrue(subtitulos.SubtituloVisible, "El aviso está visible");
            yield return Cuadros(FramesParaElHud, false);
            Assert.AreEqual(TextoClandestino, hud.TextoObjetivo.text, "El HUD dice a dónde ir");
            AnotarTramo("f1) despertar", inicio);

            // Reparar en el banco: el dinero baja el costo exacto.
            float inicioBanco = Time.time;
            Assert.Greater(persistente.Danio, 0f, "El mecha despertó dañado");
            Vector3 puerta = Aparicion(IdPuerta).Pose.position;
            Vector3 punto = banco.Posicion;
            yield return CaminarA(puerta + new Vector3(0f, 0f, MetrosAdentroDeLaPuerta), DistanciaLlegada, "adentro de la puerta de la casa");
            yield return CaminarA(puerta, DistanciaLlegada, "la puerta de la casa");
            yield return CaminarA(new Vector3(punto.x, AlturaJugador, puerta.z), DistanciaLlegada, "la calle frente al taller");
            yield return CaminarOAcortar(punto, DistanciaAlBanco, "el banco del taller");
            yield return MirarA(punto, "el banco del taller");
            yield return EsperarHasta(() => interaccion.TipoCandidato == TipoCandidatoAPie.Interactuable, 3f, "la interacción ofrece el banco");
            Assert.IsTrue(banco.PuedeUsar, "El banco ofrece reparar: " + banco.Texto);
            int costo = banco.Costo;
            Assert.Greater(costo, 0, "Reparar cuesta");
            yield return Tecla(teclado.eKey);
            yield return Tecla(teclado.enterKey); // T-149: E abre la pantalla del banco; Enter elige "REPARAR TODO".
            Assert.AreEqual(DineroInicial - costo, Cartera.Dinero, "Dinero = 500 - costo exacto de la reparación (" + costo + ")");
            Assert.AreEqual(vidaMecha.Maxima, vidaMecha.Actual, 0.01f, "Vida al máximo");
            AnotarTramo("f2) reparar en el banco (costo $" + costo + ")", inicioBanco);

            // Al clandestino: el objetivo pasa al mostrador; E; gana la casera.
            float inicioCasera = Time.time;
            IPuntoAparicion salida = Aparicion(IdSalida);
            Assert.IsNotNull(salida, "Existe Clandestino_Salida");
            Assert.AreEqual(TextoClandestino, Objetivos.Actual.Texto, "Antes de entrar el objetivo es el clandestino");
            yield return CaminarOAcortar(salida.Pose.position, DistanciaLlegada, "la entrada del clandestino");
            yield return CaminarA(salida.Pose.position + new Vector3(0f, 0f, MetrosAdentro), DistanciaLlegada, "adentro del clandestino");
            yield return null;
            Vigilar();
            Assert.AreEqual(TextoMostrador, Objetivos.Actual.Texto, "Al entrar al clandestino el objetivo pasa al mostrador");
            Assert.Less(Vector3.Distance(Objetivos.Actual.Posicion, mostrador.Posicion), ToleranciaPunto, "El mostrador");
            yield return CaminarA(mostrador.Posicion, DistanciaAlMostrador, "el mostrador");
            yield return MirarA(mostrador.Posicion, "el mostrador");
            if (!Reloj.EsDeNoche)
            {
                Reloj.AvanzarHasta(HoraAdelantada);
                atajos.Add("AvanzarHasta(" + HoraAdelantada.ToString("F1", CultureInfo.InvariantCulture) + "): todavía era de día al llegar al mostrador");
                yield return null;
                Vigilar();
            }
            yield return EsperarHasta(() => mostrador.PuedeUsar, 3f, "el mostrador ofrece la pelea: " + mostrador.Texto);
            yield return EsperarHasta(() => interaccion.TipoCandidato == TipoCandidatoAPie.Interactuable, 3f, "la interacción ofrece el mostrador");

            vigilarVida = false;
            int dineroAntes = Cartera.Dinero;
            yield return Tecla(teclado.eKey);
            yield return Tecla(teclado.enterKey); // T-145: la pantalla de apuesta, sin apostar
            yield return EsperarHasta(() => jugador.Modo == ModoJugador.Mecha, MaxSegundosEstado, "el jugador va en el mecha");
            yield return EsperarHasta(() => flujoCasero.Estado == EstadoFlujoPelea.Peleando, MaxSegundosEstado, "la pelea casera");
            float limite = vidaMecha.Maxima * FraccionDanioBuscada;
            yield return ClicsHasta(() => vidaMecha.Actual <= limite || vidaMecha.EstaKO);
            Assert.Less(vidaMecha.Actual, vidaMecha.Maxima, "El jugador recibió golpes reales de la casera");
            Assert.IsFalse(vidaMecha.EstaKO, "Y no perdió");
            ((VidaMecha)flujoCasero.Rival.Vida).RecibirDanio(new InfoDanio(DanioMortal, TipoDanio.Golpe, Vector3.zero, Vector3.forward, null));
            atajos.Add("RecibirDanio: rival casero rematado tras daño real al mecha");
            yield return EsperarHasta(() => pantallaContinuara.Visible, MaxSegundosEstado, "la pantalla de continuará");
            yield return null;
            Vigilar();
            Assert.AreEqual(dineroAntes + PremioCasero, Cartera.Dinero, "Ganar la casera paga $600");
            AnotarTramo("f3) ir al clandestino y ganar la casera", inicioCasera);
        }

        // g) "Continuará": resumen, Esc al título, CONTINUAR vuelve a la pantalla y Enter sigue en la etapa libre.
        protected IEnumerator TerminarEnContinuara()
        {
            float inicio = Time.time;
            Assert.AreEqual(EtapaPrologo.Continuara, Historia.Etapa, "La etapa es continuará");
            Assert.IsFalse(Objetivos.HayObjetivo, "Sin objetivo");
            Assert.AreEqual(AliasGritado, pantallaContinuara.Alias.ToUpperInvariant(), "Muestra el alias");
            Assert.AreEqual(FormatoDinero.Texto(Cartera.Dinero), pantallaContinuara.Dinero, "Y el dinero");
            StringAssert.Contains("Ganaste", pantallaContinuara.Resultado);
            Assert.AreEqual(1, OyentesActivos(), "Un solo AudioListener activo");
            Assert.AreEqual(1, Registro<IJugador>.Cantidad, "Un solo IJugador");
            yield return Cuadros(5, false);
            Capturar(jugador.Camara, RutaCaptura5);

            yield return EsperarSegundos(SegundosEnterEsc);
            Assert.IsTrue(pantallaContinuara.TeclasActivas, "Pasaron 4 s: las teclas funcionan");
            yield return Tecla(teclado.escapeKey);

            // Esc: vuelve al título. Con partida guardada aparece CONTINUAR.
            yield return EsperarEscena(RutaCreacion, "el título");
            pantallaCreacion = Object.FindFirstObjectByType<PantallaCreacion>();
            Assert.IsNotNull(pantallaCreacion, "El título trae PantallaCreacion");
            UsarEscenasDeLaPrueba();
            Assert.AreEqual(PantallaCreacion.Panel.Titulo, pantallaCreacion.PanelActual, "Con partida guardada aparece el título");
            bool hayContinuar = false;
            foreach (Text texto in Object.FindObjectsByType<Text>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                hayContinuar |= texto.text.Contains("CONTINUAR");
            }
            Assert.IsTrue(hayContinuar, "El título muestra CONTINUAR");
            Assert.AreEqual(0, pantallaCreacion.OpcionTitulo, "CONTINUAR es la opción elegida");

            yield return Tecla(teclado.enterKey);
            yield return EntrarAPrologo("el prólogo tras CONTINUAR");
            yield return EsperarHasta(() => prologo.Iniciado && pantallaContinuara.Visible, MaxSegundosEstado, "continuará a la vista tras CONTINUAR");
            Assert.AreEqual(EtapaPrologo.Continuara, Historia.Etapa, "La partida sigue en continuará");
            Assert.AreEqual(AliasGritado, pantallaContinuara.Alias.ToUpperInvariant(), "Con el alias");

            yield return EsperarSegundos(SegundosEnterEsc);
            Assert.IsTrue(pantallaContinuara.TeclasActivas, "Pasaron 4 s: las teclas funcionan");
            yield return Tecla(teclado.enterKey);
            Assert.AreEqual(EtapaPrologo.Libre, Historia.Etapa, "Enter sigue jugando: etapa libre");
            Assert.IsFalse(pantallaContinuara.Visible, "La pantalla se cierra");
            Assert.AreEqual(ModoJugador.APie, jugador.Modo);
            Vector3 antes = jugador.Raiz.position;
            Press(teclado.wKey);
            yield return EsperarSegundos(SegundosMoverse);
            Release(teclado.wKey);
            yield return null;
            Vigilar();
            Assert.Greater(DistanciaPlana(antes, jugador.Raiz.position), MetrosMinimosAlMoverse, "El jugador se mueve con W");
            AnotarTramo("g) continuará, título, CONTINUAR y etapa libre", inicio);
        }

        // ---- Prueba ----

        [UnityTest]
        [Timeout(1500000)]
        public IEnumerator Prologo_DeLaCreacionAContinuara_ConEntradaSimuladaYCambiosDeEscenaReales()
        {
            float inicioJuego = Time.time;

            yield return CrearPersonaje();
            float inicioNoche = Time.time;
            yield return PelearElCampeonato();
            yield return SalirYSuperarLaEmboscada();
            yield return RegresarACasa();
            yield return DormirYVerElFlashback();
            float vidaMinimaNoche = vidaMinima;
            float segundosNoche = Time.time - inicioNoche;
            yield return DespertarReparaYPelearLaCasera();
            yield return TerminarEnContinuara();

            float segundos = Time.time - inicioJuego;
            Debug.Log("[Prueba T-135] Tiempo de juego total " + segundos.ToString("F1", CultureInfo.InvariantCulture) + " s (noche hasta el flashback "
                + segundosNoche.ToString("F1", CultureInfo.InvariantCulture) + " s). Tramos: " + string.Join(" | ", tramos));
            Debug.Log("[Prueba T-135] Etapas: " + string.Join(" > ", etapasVistas) + ". Vida mínima del mecha antes de la casera "
                + vidaMinima.ToString("F1", CultureInfo.InvariantCulture) + " (piso " + piso.ToString("F1", CultureInfo.InvariantCulture) + "). Atajos: " + string.Join(" | ", atajos));
            Debug.Log("[Prueba T-135] Frames: " + medidor.Resumen());
            Debug.Log("[Prueba T-135] partida.json real: antes '" + huellaReal + "', después '" + HuellaDe(rutaReal) + "'");

            CollectionAssert.AreEqual(new[]
            {
                EtapaPrologo.PeleaTutorial, EtapaPrologo.SalidaClandestino, EtapaPrologo.Emboscada, EtapaPrologo.RegresoACasa,
                EtapaPrologo.Dormir, EtapaPrologo.Flashback, EtapaPrologo.Despertar, EtapaPrologo.PeleaCasera, EtapaPrologo.Continuara,
                EtapaPrologo.Libre,
            }, etapasVistas, "Las etapas pasan en orden: " + string.Join(", ", etapasVistas));
            Assert.Greater(vidaMinimaNoche, 0f, "La vida del mecha nunca llegó a 0 en la noche");
            Assert.Greater(vidaMinima, 0f, "La vida del mecha nunca llegó a 0 antes de la casera");
            Assert.Less(segundos, LimiteSegundosJuego, "El prólogo dura menos de " + LimiteSegundosJuego + " s de juego");
            Assert.IsTrue(medidor.Cumple, "Frames del recorrido: " + medidor.Resumen());
            Assert.AreEqual(huellaReal, HuellaDe(rutaReal), "La partida real (persistentDataPath/partida.json) no se tocó");
            Assert.AreEqual(0, errores.Count, string.Join("\n", errores));
        }
    }
}
