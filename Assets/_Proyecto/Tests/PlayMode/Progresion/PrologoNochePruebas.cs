using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using MechaCyberpunk.Combate.Vida;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.Datos;
using MechaCyberpunk.Mecha;
using MechaCyberpunk.Mecha.Encendido;
using MechaCyberpunk.Mecha.Llamado;
using MechaCyberpunk.Mecha.Montaje;
using MechaCyberpunk.Pelea.Emboscada;
using MechaCyberpunk.Pelea.Flujo;
using MechaCyberpunk.Personaje;
using MechaCyberpunk.Personaje.Interaccion;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Progresion;
using MechaCyberpunk.Progresion.Guardado;
using MechaCyberpunk.Progresion.Prologo;
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
    /// T-132: la noche del prólogo de punta a punta en Pruebas_Prologo, jugada con entrada simulada (clics y movimiento del ratón, W, M, E,
    /// F, Espacio y Q). Atajos permitidos y anotados en el registro: rematar a un rival con daño directo cuando el mecha ya recibió golpes
    /// reales, y acortar caminatas de más de 40 m con IJugador.Colocar hasta 10 m antes del objetivo. El cambio de escena se intercepta.
    /// </summary>
    public class PrologoNochePruebas : InputTestFixture
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Tony/Pruebas_Prologo.unity";
        private const string RutaPrefabDirector = "Assets/_Proyecto/Progresion/Prologo/Prefabs/DirectorPrologo.prefab";
        private const string RutaControlesAPie = "Assets/_Proyecto/Personaje/Datos/ConfigControlesAPie_Base.asset";
        private const string RutaControlesMecha = "Assets/_Proyecto/Mecha/Datos/ConfigControlesMecha_Base.asset";
        private const string RutaPiedad = "Assets/_Proyecto/IA/Datos/ConfigPiedadRival_Base.asset";
        private const string IdSalida = "Clandestino_Salida";
        private const string IdCama = "Casa_Cama";
        private const string IdCarro = "Prologo_Carro";
        private const string IdEsquina = "Prologo_Esquina_1";
        private const string Alias = "LA CHISPA";
        private const string TextoCarro = "Ve a tu carro";
        private const string TextoRegreso = "Regresa a casa en tu carro";
        private const string TextoCama = "Duerme en tu cama";
        private const string RutaFlashbackPruebas = "Assets/_Proyecto/Escenas/Tony/Pruebas_Flashback.unity";
        private const string VariableCaptura = "CAPTURAR_T132";
        private const string RutaCaptura = "docs/capturas/T-132-1.png";
        private const int PremioTutorial = 1500;
        private const int FramesEstables = 10;
        private const int MaxFramesCalentamiento = 1000;
        private const int FramesParaElHud = 8;
        private const float DeltaTimeEstable = 0.05f;
        private const float HoraNoche = 22f;
        private const float MargenHora = 0.5f;
        private const float MaxSegundosNoche = 200f;
        private const float MaxSegundosEstado = 60f;
        private const float MaxSegundosGolpes = 40f;
        private const float MaxSegundosTramo = 40f;
        private const float MaxSegundosLlamado = 15f;
        private const float MaxSegundosEncender = 30f;
        private const float SegundosPeleaReal = 30f;
        private const float MaxSegundosPelea = 70f;
        private const float SegundosEntreClics = 0.25f;
        private const float DanioMortal = 100000f;
        private const float FraccionDanioBuscada = 0.9f;
        private const float ToleranciaJugador = 0.6f;
        private const float ToleranciaPunto = 0.5f;
        private const float ToleranciaQuieto = 0.05f;
        private const float AlturaJugador = 0.05f;
        private const float DistanciaLlegada = 1f;
        private const float DistanciaJuntoAlMecha = 1.2f;
        private const float MetrosParaAtajo = 40f;
        private const float MetrosTrasAtajo = 10f;
        private const float XEsperaLlamado = 40f;
        private const float ZEsperaLlamado = 42f;
        private const float GananciaGiro = 0.4f;
        private const float AnguloParaCaminar = 60f;
        private const float GradosMediaVuelta = 180f;
        private const float AnguloMirarListo = 2f;
        private const float GradosMinimosGiro = 0.5f;
        private const float DistanciaCamaraDeLaCama = 1.8f;
        private const float SegundosLibre = 1.5f;
        private const int AnchoCaptura = 1280;
        private const int AltoCaptura = 720;
        private const int ProfundidadCaptura = 24;
        private const int SaltoDePixeles = 97;
        private const float VariacionMinima = 0.05f;
        private const float DistanciaPlanoCanvas = 0.35f;

        private Mouse raton;
        private Keyboard teclado;
        private string carpeta;
        private JugadorAPie jugador;
        private InteraccionAPie interaccion;
        private MontajeMecha montaje;
        private LlamadoMecha llamado;
        private EncendidoMecha encendido;
        private VidaMecha vidaMecha;
        private DirectorPrologo prologo;
        private DirectorPelea flujo;
        private DirectorEmboscada emboscada;
        private HudAPie hud;
        private ServicioHistoria historia;
        private ICartera cartera;
        private IRelojMundo reloj;
        private float sensibilidadAPie;
        private float sensibilidadMecha;
        private float piso;
        private bool avanzando;
        private float vidaMinima;
        private float yawMechaAnterior;
        private float giroPedidoPendiente;
        private readonly List<string> errores = new List<string>();
        private readonly List<string> rutasPedidas = new List<string>();
        private readonly List<string> lineas = new List<string>();
        private readonly List<string> atajos = new List<string>();

        public override void Setup()
        {
            // Otras pruebas (sin InputTestFixture) dejan acciones prendidas con un estado viejo: se apagan y se descarta su resolución.
            InputSystem.DisableAllEnabledActions();
            base.Setup();
            Registros.LimpiarTodos();
            EventosPelea.LimpiarSuscriptores();
            teclado = InputSystem.AddDevice<Keyboard>();
            raton = InputSystem.AddDevice<Mouse>();
            ReiniciarMapasDeAcciones(new InputDevice[] { teclado, raton });
            carpeta = Path.Combine(Application.temporaryCachePath, "prologo_noche_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(carpeta);
            ServicioGuardado.RutaForzada = Path.Combine(carpeta, "partida.json");
            CambioEscena.Interceptar(ruta => rutasPedidas.Add(ruta));
            CambioEscena.ConsumirTransicion();
            errores.Clear();
            rutasPedidas.Clear();
            lineas.Clear();
            atajos.Clear();
            avanzando = false;
            giroPedidoPendiente = 0f;
            vidaMinima = float.MaxValue;
            Application.logMessageReceived += RegistrarError;
        }

        // Los mapas de acciones (assets) guardan el estado de resolución anterior al reinicio del fixture. Cambiar `devices` en un
        // mapa apagado descarta ese estado: la escena los resuelve de nuevo al prenderlos.
        private static void ReiniciarMapasDeAcciones(InputDevice[] dispositivos)
        {
            foreach (string guid in AssetDatabase.FindAssets("t:InputActionAsset"))
            {
                InputActionAsset asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset == null)
                {
                    continue;
                }
                asset.Disable();
                asset.devices = dispositivos;
                asset.devices = null;
            }
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
            // Deja los mapas apagados y sin dispositivos antes y después de que el fixture reinicie el Input System
            // (si no, las pruebas siguientes heredan controles ya liberados: ArgumentNullException statePtr).
            InputSystem.DisableAllEnabledActions();
            ReiniciarMapasDeAcciones(new InputDevice[] { teclado, raton });
            base.TearDown();
            ReiniciarMapasDeAcciones(new InputDevice[0]);
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

        // ---- Preparación ----

        // Carga la escena. Con 'alias' lo pone en el perfil antes de que corra la presentación. 'estable' espera cuadros sin saltos.
        private IEnumerator CargarEscena(bool conAlias, bool quitarDirector = false)
        {
            sensibilidadAPie = AssetDatabase.LoadAssetAtPath<ConfigControlesAPie>(RutaControlesAPie).SensibilidadRaton;
            sensibilidadMecha = AssetDatabase.LoadAssetAtPath<ConfigControlesMecha>(RutaControlesMecha).SensibilidadRaton;
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));

            jugador = Object.FindFirstObjectByType<JugadorAPie>();
            prologo = Object.FindFirstObjectByType<DirectorPrologo>();
            montaje = Object.FindFirstObjectByType<MontajeMecha>();
            emboscada = Object.FindFirstObjectByType<DirectorEmboscada>();
            hud = Object.FindFirstObjectByType<HudAPie>();
            interaccion = Object.FindFirstObjectByType<InteraccionAPie>();
            Assert.IsNotNull(jugador, "Pruebas_Prologo trae al jugador");
            Assert.IsNotNull(prologo, "Pruebas_Prologo trae el director del prólogo");
            Assert.IsNotNull(montaje, "Pruebas_Prologo trae el mecha de la historia");
            Assert.IsNotNull(emboscada, "Pruebas_Prologo trae la emboscada");
            Assert.IsNotNull(hud, "El jugador trae el HUD a pie");
            Assert.IsNotNull(interaccion, "El jugador trae la interacción");
            flujo = prologo.GetComponentInChildren<DirectorPelea>(true);
            Assert.IsNotNull(flujo, "El director trae el flujo del tutorial");
            llamado = montaje.GetComponent<LlamadoMecha>();
            encendido = montaje.GetComponent<EncendidoMecha>();
            vidaMecha = montaje.GetComponent<VidaMecha>();
            historia = Registro<IHistoria>.Actual as ServicioHistoria;
            cartera = Registro<ICartera>.Actual;
            reloj = Registro<IRelojMundo>.Actual;
            Assert.IsNotNull(historia, "Hay ServicioHistoria");
            Assert.IsNotNull(cartera, "Hay cartera");
            Assert.IsNotNull(reloj, "Hay reloj");
            piso = AssetDatabase.LoadAssetAtPath<ConfigPiedadRival>(RutaPiedad).FraccionPiso * vidaMecha.Maxima;
            if (conAlias)
            {
                ServicioPerfil perfil = Registro<IPerfilJugador>.Actual as ServicioPerfil;
                Assert.IsNotNull(perfil, "Hay perfil");
                perfil.Fijar("Juan", Alias);
            }
            flujo.LineaDicha += lineas.Add;
            if (quitarDirector)
            {
                // Se quita el director de la escena (que arrancaría la noche) antes de que corra su primer cuadro.
                Object.Destroy(prologo);
                prologo = null;
            }
            yield return Esperar(FramesEstables, true);
        }

        // Espera N cuadros; 'estables' los cuenta solo si el cuadro no fue largo (arranque del editor).
        private IEnumerator Esperar(int cuadros, bool estables)
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

        private IEnumerator EsperarSegundos(float segundos)
        {
            float fin = Time.time + segundos;
            while (Time.time < fin)
            {
                yield return null;
                Vigilar();
            }
        }

        private void Vigilar()
        {
            if (vidaMecha != null)
            {
                vidaMinima = Mathf.Min(vidaMinima, vidaMecha.Actual);
            }
        }

        private IEnumerator EsperarHasta(Func<bool> condicion, float maxSegundos, string que)
        {
            float fin = Time.time + maxSegundos;
            while (!condicion() && Time.time < fin)
            {
                yield return null;
                Vigilar();
            }
            Assert.IsTrue(condicion(), "Se esperaba: " + que);
        }

        private IEnumerator Tecla(KeyControl tecla)
        {
            Press(tecla);
            yield return null;
            Vigilar();
            Release(tecla);
            yield return null;
            Vigilar();
        }

        private static float DistanciaPlana(Vector3 a, Vector3 b)
        {
            Vector3 delta = a - b;
            delta.y = 0f;
            return delta.magnitude;
        }

        private static IPuntoHistoria PuntoHistoria(string id)
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

        private static ILugar LugarCasa()
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

        private static IObjetivos Objetivos => Registro<IObjetivos>.Actual;

        private static void Matar(IPeleador peleador)
        {
            IRecibeDanio receptor = peleador.Vida as IRecibeDanio ?? peleador.Raiz.GetComponentInChildren<IRecibeDanio>();
            Assert.IsNotNull(receptor, "El rival recibe daño");
            receptor.RecibirDanio(new InfoDanio(DanioMortal, TipoDanio.Golpe, Vector3.zero, Vector3.forward, null));
        }

        private void AssertSinErrores()
        {
            Assert.AreEqual(0, errores.Count, string.Join("\n", errores));
        }

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

        // Camina hacia un punto girando con el ratón en proporción al ángulo; termina al llegar o cuando 'antes' se cumple.
        private IEnumerator CaminarA(Vector3 destino, float distancia, string que, Func<bool> antes = null)
        {
            float fin = Time.time + MaxSegundosTramo;
            while (DistanciaPlana(jugador.Raiz.position, destino) > distancia && (antes == null || !antes()))
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
                Vigilar();
            }
            Avanzar(false);
            yield return null;
        }

        // Atajo anotado: una caminata de más de 40 m se acorta con Colocar hasta 10 m antes del objetivo, mirando a él.
        private IEnumerator CaminarOAcortar(Vector3 destino, float distancia, string que, Func<bool> antes = null)
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
            yield return CaminarA(destino, distancia, que, antes);
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
                Vigilar();
            }
            yield return null;
        }

        // ---- Entrada: mecha ----

        private IEnumerator Encender()
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
            yawMechaAnterior = montaje.transform.eulerAngles.y;
        }

        private static bool Existe(IPeleador peleador)
        {
            return peleador is Component componente && componente != null;
        }

        private List<IPeleador> RivalesEnPie(IReadOnlyList<IPeleador> rivales)
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

        private IPeleador MasCercano(List<IPeleador> rivales)
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
        private void ApuntarYGolpear(IPeleador rival, bool clic)
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

        // ---- Pasos de la noche ----

        // 1) Del arranque a la pelea ganada: el mecha en la arena, la presentación con el alias, el premio y la salida a pie.
        private IEnumerator PelearElCampeonato()
        {
            Assert.AreEqual(1, Registro<IHistoria>.Cantidad, "Una sola historia");
            yield return EsperarHasta(() => jugador.Modo == ModoJugador.Mecha, MaxSegundosEstado, "el jugador va en el mecha");
            Assert.AreEqual(EtapaPrologo.PeleaTutorial, historia.Etapa, "La etapa es la pelea del tutorial");
            Assert.IsTrue(reloj.EsDeNoche, "Es de noche");
            Assert.AreEqual(HoraNoche, reloj.Hora, MargenHora, "Son las 22:00");
            Assert.IsFalse(emboscada.Habilitada, "En la pelea la emboscada está apagada");
            Assert.AreEqual(EstadoLlamado.EnArena, llamado.Estado, "El mecha está en la arena");
            int dineroAntes = cartera.Dinero;

            yield return EsperarHasta(() => lineas.Count >= 4, MaxSegundosEstado, "las cuatro líneas del presentador");
            StringAssert.Contains(Alias, lineas[3], "El grito de la presentación lleva el alias del perfil");
            yield return EsperarHasta(() => flujo.Estado == EstadoFlujoPelea.Peleando, MaxSegundosEstado, "la pelea");

            yield return RecibirGolpesReales();
            Matar(flujo.Rival);
            atajos.Add("RecibirDanio: rival del tutorial rematado tras daño real al mecha");
            yield return EsperarHasta(() => flujo.Estado == EstadoFlujoPelea.Resultado, MaxSegundosEstado, "el resultado");
            yield return EsperarHasta(() => historia.Etapa == EtapaPrologo.SalidaClandestino, MaxSegundosEstado, "la etapa de salida");
            yield return null;

            Assert.AreEqual(dineroAntes + PremioTutorial, cartera.Dinero, "Ganó el premio de $1,500");
            Assert.AreEqual(ModoJugador.APie, jugador.Modo, "El jugador quedó a pie");
            IPuntoAparicion salida = Aparicion(IdSalida);
            Assert.IsNotNull(salida, "Existe Clandestino_Salida");
            Assert.Less(DistanciaPlana(jugador.Raiz.position, salida.Pose.position), ToleranciaJugador, "A pie en Clandestino_Salida");
            Assert.IsTrue(emboscada.Habilitada, "Saliendo del clandestino la emboscada se prende");
            Assert.AreEqual(EstadoEmboscada.Esperando, emboscada.Estado);
            VerificarObjetivo(TextoCarro, PuntoHistoria(IdCarro).Pose.position);
        }

        private void VerificarObjetivo(string texto, Vector3 posicion)
        {
            Assert.IsTrue(Objetivos.HayObjetivo, "Hay objetivo: " + texto);
            Assert.AreEqual(texto, Objetivos.Actual.Texto);
            Assert.IsTrue(Objetivos.Actual.TienePosicion, "Apunta a una posición");
            Assert.Less(Vector3.Distance(Objetivos.Actual.Posicion, posicion), ToleranciaPunto, "En el punto esperado");
        }

        private IEnumerator RecibirGolpesReales()
        {
            float limite = vidaMecha.Maxima * FraccionDanioBuscada;
            bool clic = false;
            float proximoClic = 0f;
            float fin = Time.time + MaxSegundosGolpes;
            while (vidaMecha.Actual > limite && !vidaMecha.EstaKO && Time.time < fin)
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
            Assert.Less(vidaMecha.Actual, vidaMecha.Maxima, "El jugador recibió golpes reales");
            Assert.IsFalse(vidaMecha.EstaKO, "Y no perdió");
        }

        // 2) Caminando hacia el carro se activa la emboscada.
        private IEnumerator CaminarAlCarro()
        {
            // Un cuadro más para que el HUD muestre el objetivo, y la captura del criterio visual.
            yield return Esperar(FramesParaElHud, false);
            Assert.AreEqual(TextoCarro, hud.TextoObjetivo.text, "El HUD dice 'Ve a tu carro'");
            Assert.IsTrue(hud.GrupoObjetivo.activeInHierarchy, "El objetivo está a la vista");
            Assert.IsTrue(hud.Flecha.gameObject.activeInHierarchy, "Con su flecha");
            Capturar();

            Vector3 carro = PuntoHistoria(IdCarro).Pose.position;
            yield return CaminarOAcortar(carro, DistanciaLlegada, "el carro", () => emboscada.Estado != EstadoEmboscada.Esperando);
            Assert.AreNotEqual(EstadoEmboscada.Esperando, emboscada.Estado, "Al acercarse al callejón la pandilla se activa");
            yield return null;
            Assert.AreEqual(EtapaPrologo.Emboscada, historia.Etapa, "La etapa es la emboscada");
            Assert.IsFalse(emboscada.Habilitada, "Ya empezó: queda apagada");
            yield return EsperarHasta(() => emboscada.Estado == EstadoEmboscada.EsperandoMecha, MaxSegundosEstado, "la pandilla termina de aparecer");
        }

        // 3) La emboscada: llama al mecha (M) en la zona del callejón, sube (E), enciende y gana.
        private IEnumerator SuperarLaEmboscada()
        {
            yield return CaminarA(new Vector3(XEsperaLlamado, AlturaJugador, ZEsperaLlamado), DistanciaLlegada, "la zona de llamado");
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
            float proximoClic = 0f;
            float inicioPelea = Time.time;
            float fin = Time.time + MaxSegundosPelea;
            bool rematados = false;
            while (emboscada.Estado != EstadoEmboscada.Superada && Time.time < fin)
            {
                List<IPeleador> enPie = RivalesEnPie(emboscada.Rivales);
                IPeleador rival = MasCercano(enPie);
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
            Assert.AreEqual(EstadoEmboscada.Superada, emboscada.Estado, "La pandilla cayó");
            yield return null;
            Assert.AreEqual(1, terminadas, "Terminada llega una sola vez");
            Assert.AreEqual(EtapaPrologo.RegresoACasa, historia.Etapa, "La etapa es el regreso a casa");
            VerificarObjetivo(TextoRegreso, PuntoHistoria(IdEsquina).Pose.position);
        }

        // 4) Baja del mecha, lo devuelve y camina a la esquina y a la casa; al entrar, la etapa pasa a Dormir.
        private IEnumerator RegresarACasa()
        {
            yield return EsperarHasta(() => montaje.PuedeBajar, 5f, "el mecha puede bajar (detenido)");
            yield return Tecla(teclado.eKey);
            Assert.AreEqual(ModoJugador.APie, jugador.Modo, "E baja del mecha");
            yield return Tecla(teclado.mKey);

            Vector3 esquina = PuntoHistoria(IdEsquina).Pose.position;
            yield return CaminarOAcortar(esquina, DistanciaLlegada, "la esquina", () => HayObjetivoDeCasa());
            yield return EsperarHasta(() => HayObjetivoDeCasa(), 3f, "el objetivo pasa a la casa");
            Assert.AreEqual(EtapaPrologo.RegresoACasa, historia.Etapa, "Sigue el regreso a casa");
            Assert.AreEqual(TextoRegreso, Objetivos.Actual.Texto);
            Assert.AreEqual(IdLugar.Casa, Objetivos.Actual.Lugar);

            ILugar casa = LugarCasa();
            Assert.IsNotNull(casa, "Existe el lugar Casa");
            yield return CaminarOAcortar(casa.PosicionEntrada, DistanciaLlegada, "la puerta de la casa");
            CamaJugador cama = Object.FindFirstObjectByType<CamaJugador>();
            Assert.IsNotNull(cama, "Pruebas_Prologo trae la cama");
            yield return CaminarA(cama.Posicion - new Vector3(0f, 0f, DistanciaCamaraDeLaCama), DistanciaLlegada, "dentro de la casa",
                () => historia.Etapa == EtapaPrologo.Dormir);
            yield return EsperarHasta(() => historia.Etapa == EtapaPrologo.Dormir, 3f, "entrar a la casa lleva a dormir");
            yield return null;
            Assert.IsTrue(Objetivos.HayObjetivo);
            Assert.AreEqual(TextoCama, Objetivos.Actual.Texto, "El objetivo es la cama");
            Assert.IsTrue(Objetivos.Actual.TienePosicion);
            Assert.Less(DistanciaPlana(Objetivos.Actual.Posicion, cama.Posicion), ToleranciaPunto, "En la cama");
        }

        private static bool HayObjetivoDeCasa()
        {
            return Objetivos.HayObjetivo && !Objetivos.Actual.TienePosicion && Objetivos.Actual.Lugar == IdLugar.Casa;
        }

        // 5) E en la cama pide la escena del flashback.
        private IEnumerator Dormir()
        {
            CamaJugador cama = Object.FindFirstObjectByType<CamaJugador>();
            yield return CaminarA(cama.Posicion - new Vector3(0f, 0f, DistanciaCamaraDeLaCama), DistanciaLlegada, "junto a la cama");
            yield return MirarA(cama.Posicion, "la cama");
            yield return EsperarHasta(() => interaccion.TipoCandidato == TipoCandidatoAPie.Interactuable, 3f, "la interacción ofrece la cama");
            yield return EsperarHasta(() => hud.TextoAvisoInteraccion.text.Contains("Dormir"), 3f, "el HUD ofrece dormir");
            yield return Tecla(teclado.eKey);
            yield return EsperarHasta(() => rutasPedidas.Count > 0, MaxSegundosEstado, "la cama pide el flashback");
            Assert.AreEqual(RutaFlashbackPruebas, rutasPedidas[0], "Pide la escena del flashback de pruebas");
            Assert.AreEqual(EtapaPrologo.Flashback, historia.Etapa, "La etapa guardada es el flashback");
        }

        // Captura de lo que ve el jugador (solo con CAPTURAR_T132=1). Los lienzos en Overlay no salen en una cámara a textura:
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

        // Escribe una partida con esa etapa y marca una transición pendiente, como si se llegara desde otra escena.
        private void PrepararPartidaGuardada(EtapaPrologo etapa)
        {
            PiezaGuardada pieza = new PiezaGuardada { clave = ServicioHistoria.Clave, datos = "{\"etapa\":\"" + etapa + "\"}" };
            File.WriteAllText(ServicioGuardado.RutaForzada, ArchivoPartida.Escribir(new[] { pieza }, DateTime.UtcNow.ToString("o")));
            CambioEscena.Ir("transicion-de-prueba");
            rutasPedidas.Clear();
        }

        // ---- Pruebas ----

        [UnityTest]
        public IEnumerator Noche_DesdeElPlayEnPruebasPrologo_PeleaSaleSeEmboscaRegresaYDuerme()
        {
            yield return CargarEscena(true);
            float inicio = Time.time;

            yield return PelearElCampeonato();
            yield return CaminarAlCarro();
            yield return SuperarLaEmboscada();
            yield return RegresarACasa();
            yield return Dormir();

            float segundos = Time.time - inicio;
            Debug.Log("[Prueba T-132] Noche completa: " + segundos.ToString("F1") + " s de juego, vida mínima del mecha " + vidaMinima.ToString("F1")
                + " de " + vidaMecha.Maxima.ToString("F0") + " (piso " + piso.ToString("F1") + "). Atajos: " + string.Join(" | ", atajos));
            Assert.Less(segundos, MaxSegundosNoche, "La noche dura menos de 200 s de juego");
            Assert.Greater(vidaMinima, 0f, "La vida del mecha nunca llegó a 0");
            AssertSinErrores();
        }

        [UnityTest]
        public IEnumerator EtapaSinPartidaLibre_ElDirectorNoHaceNada_YLaEmboscadaSigueHabilitada()
        {
            yield return CargarEscena(false, true);
            // Se pone un director nuevo del prefab, con el valor de fábrica.
            GameObject libre = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefabDirector));
            libre.transform.position = new Vector3(0f, -100f, 0f);
            DirectorPrologo director = libre.GetComponent<DirectorPrologo>();
            Assert.AreEqual(EtapaPrologo.Libre, director.EtapaSinPartida, "El prefab trae Libre");
            Vector3 antes = jugador.Raiz.position;
            EtapaPrologo etapaAntes = historia.Etapa;

            yield return EsperarSegundos(SegundosLibre);
            Assert.IsTrue(director.Iniciado, "El director corrió su arranque");
            Assert.IsFalse(director.Activo, "Y no tomó ninguna etapa");
            Assert.IsFalse(Objetivos.HayObjetivo, "No fija objetivos");
            Assert.Less(DistanciaPlana(antes, jugador.Raiz.position), ToleranciaQuieto, "No mueve al jugador");
            Assert.AreEqual(ModoJugador.APie, jugador.Modo);
            Assert.AreEqual(etapaAntes, historia.Etapa, "No cambia la etapa");
            Assert.IsTrue(emboscada.Habilitada, "La emboscada sigue habilitada");
            Assert.IsFalse(libre.GetComponentInChildren<EntradaArena>(true).EnCurso, "No empezó ninguna pelea");
            AssertSinErrores();
        }

        [UnityTest]
        public IEnumerator Reanudar_RegresoACasa_AparecenEnLaSalidaConElObjetivoDeRegreso()
        {
            PrepararPartidaGuardada(EtapaPrologo.RegresoACasa);
            yield return CargarEscena(false);
            yield return EsperarSegundos(SegundosLibre);

            Assert.AreEqual(EtapaPrologo.RegresoACasa, historia.Etapa, "La etapa cargada se queda");
            Assert.AreEqual(EtapaPrologo.RegresoACasa, prologo.Etapa);
            IPuntoAparicion salida = Aparicion(IdSalida);
            Assert.Less(DistanciaPlana(jugador.Raiz.position, salida.Pose.position), ToleranciaJugador, "El jugador está en Clandestino_Salida");
            Assert.AreEqual(ModoJugador.APie, jugador.Modo);
            VerificarObjetivo(TextoRegreso, PuntoHistoria(IdEsquina).Pose.position);
            Assert.IsFalse(emboscada.Habilitada, "La emboscada no salta en el regreso");
            Assert.IsFalse(prologo.GetComponentInChildren<EntradaArena>(true).EnCurso, "No repite la pelea");
            AssertSinErrores();
        }

        [UnityTest]
        public IEnumerator Reanudar_Emboscada_SeRepiteDesdeLaSalidaSinRetrocederLaEtapaGuardada()
        {
            PrepararPartidaGuardada(EtapaPrologo.Emboscada);
            yield return CargarEscena(false);
            yield return EsperarSegundos(SegundosLibre);

            Assert.AreEqual(EtapaPrologo.Emboscada, historia.Etapa, "La etapa guardada no retrocede");
            Assert.AreEqual(EtapaPrologo.SalidaClandestino, prologo.Etapa, "El director repite la salida");
            Assert.Less(DistanciaPlana(jugador.Raiz.position, Aparicion(IdSalida).Pose.position), ToleranciaJugador, "En Clandestino_Salida");
            VerificarObjetivo(TextoCarro, PuntoHistoria(IdCarro).Pose.position);
            Assert.IsTrue(emboscada.Habilitada, "La emboscada se repite");
            Assert.AreEqual(EstadoEmboscada.Esperando, emboscada.Estado);
            AssertSinErrores();
        }

        [UnityTest]
        public IEnumerator Reanudar_Dormir_AparecenEnLaCamaConElObjetivoDeDormir()
        {
            PrepararPartidaGuardada(EtapaPrologo.Dormir);
            yield return CargarEscena(false);
            yield return EsperarSegundos(SegundosLibre);

            Assert.AreEqual(EtapaPrologo.Dormir, historia.Etapa);
            Assert.Less(DistanciaPlana(jugador.Raiz.position, Aparicion(IdCama).Pose.position), ToleranciaJugador, "El jugador está en Casa_Cama");
            Assert.AreEqual(TextoCama, Objetivos.Actual.Texto);
            Assert.IsFalse(emboscada.Habilitada, "La emboscada está apagada");
            AssertSinErrores();
        }

        [UnityTest]
        public IEnumerator Reanudar_EtapaFueraDeLaNoche_ApagaLaEmboscadaYNoHaceMas()
        {
            PrepararPartidaGuardada(EtapaPrologo.PeleaCasera);
            yield return CargarEscena(false);
            yield return EsperarSegundos(SegundosLibre);

            Assert.AreEqual(EtapaPrologo.PeleaCasera, historia.Etapa);
            Assert.IsFalse(prologo.Activo, "El director no lleva esa etapa");
            Assert.IsFalse(emboscada.Habilitada, "En cualquier otra etapa la emboscada queda deshabilitada");
            Assert.IsFalse(Objetivos.HayObjetivo, "Sin objetivo del director");
            AssertSinErrores();
        }
    }
}
