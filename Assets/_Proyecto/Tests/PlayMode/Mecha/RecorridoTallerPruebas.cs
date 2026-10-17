using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using MechaCyberpunk.Combate.Vida;
using MechaCyberpunk.Core;
using MechaCyberpunk.Economia;
using MechaCyberpunk.Mecha.Encendido;
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

namespace MechaCyberpunk.Tests.Mecha
{
    /// <summary>
    /// T-112: el cierre de T-F5 del lado de Tony jugado como una persona en Pruebas_Calle, con teclado y ratón simulados: pelear y
    /// salir dañado, revancha dañada, regresar el mecha al taller, repararlo pagando, renta vencida y guardar/cargar. Del juego solo
    /// se lee estado para decidir la siguiente entrada. Lo único que no es entrada: dejar caer al rival con daño directo cuando el
    /// jugador ya recibió golpes reales (para que dure poco) y colocar al jugador en el taller al salir de la arena cerrada.
    /// </summary>
    public class RecorridoTallerPruebas : InputTestFixture
    {
        private const string RutaCalle = "Assets/_Proyecto/Escenas/Tony/Pruebas_Calle.unity";
        private const string RutaControlesAPie = "Assets/_Proyecto/Personaje/Datos/ConfigControlesAPie_Base.asset";
        private const int FramesEstables = 10;
        private const int MaxFramesCalentamiento = 1000;
        private const float DeltaTimeEstable = 0.05f;
        private const float MaxSegundosRecorrido = 120f;
        private const float MaxSegundosTramo = 20f;
        private const float MaxSegundosEncender = 30f;
        private const float MaxSegundosEstado = 30f;
        private const float MaxSegundosGolpes = 40f;
        private const float FraccionDanioBuscada = 0.7f;
        private const float DanioMortal = 100000f;
        private const float MargenVida = 0.01f;
        private const int PremioEsperado = 600;
        private const int DineroInicial = 500;
        private const int Renta = 500;
        private const int DiaDeLaRenta = 8;
        private const float VidaGuardada = 40f;
        private const float DistanciaLlegada = 1.2f;
        private const float GananciaGiro = 0.4f;
        private const float AnguloParaCaminar = 60f;
        private const float GradosMediaVuelta = 180f;
        private const float AnguloMirarListo = 2f;
        private const float SegundosEntreClics = 0.25f;
        private const float SegundosCaminarMecha = 1.5f;
        private const float AlturaJugador = 0.05f;
        private const float MargenLlegadaBanco = 2f;
        private const float MaxSegundosCaminarBanco = 10f;
        private const string VariableCaptura = "CAPTURAR_T112";
        private const string RutaCaptura1 = "docs/capturas/T-112-1.png";
        private const string RutaCaptura2 = "docs/capturas/T-112-2.png";
        private const string RutaCaptura3 = "docs/capturas/T-112-3.png";
        private const int AnchoCaptura = 1280;
        private const int AltoCaptura = 720;
        private const int ProfundidadCaptura = 24;
        private const int SaltoDePixeles = 97;
        private const float VariacionMinima = 0.05f;
        private const float DistanciaPlanoCanvas = 1f;

        private Keyboard teclado;
        private Mouse raton;
        private JugadorAPie jugador;
        private InteraccionAPie interaccion;
        private MontajeMecha montaje;
        private LlamadoMecha llamado;
        private EncendidoMecha encendido;
        private VidaMecha vidaMecha;
        private MechaPersistente persistente;
        private BancoTaller banco;
        private DirectorPelea director;
        private ICartera cartera;
        private IRenta renta;
        private IRelojMundo reloj;
        private ServicioPerfil perfil;
        private ServicioGuardado guardado;
        private HudAPie hud;
        private float sensibilidadAPie;
        private float inicioJuego;
        private string carpeta;
        private bool avanzando;
        private readonly List<string> errores = new List<string>();
        private readonly List<(int, MotivoDinero)> cambios = new List<(int, MotivoDinero)>();

        public override void Setup()
        {
            base.Setup();
            Registros.LimpiarTodos();
            teclado = InputSystem.AddDevice<Keyboard>();
            raton = InputSystem.AddDevice<Mouse>();
            carpeta = Path.Combine(Application.temporaryCachePath, "recorrido_taller_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(carpeta);
            errores.Clear();
            cambios.Clear();
            avanzando = false;
            Application.logMessageReceived += RegistrarError;
        }

        public override void TearDown()
        {
            Application.logMessageReceived -= RegistrarError;
            if (Directory.Exists(carpeta))
            {
                Directory.Delete(carpeta, true);
            }
            Registros.LimpiarTodos();
            base.TearDown();
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

        private IEnumerator CargarCalle()
        {
            sensibilidadAPie = AssetDatabase.LoadAssetAtPath<ConfigControlesAPie>(RutaControlesAPie).SensibilidadRaton;
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(RutaCalle, new LoadSceneParameters(LoadSceneMode.Single));
            int estables = 0;
            int intentos = 0;
            while (estables < FramesEstables && intentos < MaxFramesCalentamiento)
            {
                yield return null;
                intentos++;
                estables = Time.deltaTime < DeltaTimeEstable ? estables + 1 : 0;
            }
            jugador = Object.FindFirstObjectByType<JugadorAPie>();
            interaccion = Object.FindFirstObjectByType<InteraccionAPie>();
            montaje = Object.FindFirstObjectByType<MontajeMecha>();
            director = Object.FindFirstObjectByType<DirectorPelea>();
            hud = Object.FindFirstObjectByType<HudAPie>();
            perfil = Object.FindFirstObjectByType<ServicioPerfil>();
            guardado = Object.FindFirstObjectByType<ServicioGuardado>();
            Assert.IsNotNull(jugador, "Pruebas_Calle trae el jugador");
            Assert.IsNotNull(interaccion, "El jugador trae la interacción");
            Assert.IsNotNull(montaje, "Pruebas_Calle trae el mecha");
            Assert.IsNotNull(director, "Pruebas_Calle trae el flujo de la pelea");
            Assert.IsNotNull(hud, "Pruebas_Calle trae el HUD a pie");
            Assert.IsNotNull(perfil, "Hay un servicio de perfil");
            Assert.IsNotNull(guardado, "Hay un servicio de guardado");
            llamado = montaje.GetComponent<LlamadoMecha>();
            encendido = montaje.GetComponent<EncendidoMecha>();
            vidaMecha = montaje.GetComponent<VidaMecha>();
            persistente = montaje.GetComponent<MechaPersistente>();
            banco = montaje.GetComponent<BancoTaller>();
            cartera = Registro<ICartera>.Actual;
            renta = Registro<IRenta>.Actual;
            reloj = Registro<IRelojMundo>.Actual;
            Assert.IsNotNull(cartera, "Hay cartera");
            Assert.IsNotNull(renta, "Hay renta");
            Assert.IsNotNull(reloj, "Hay reloj");
            Assert.IsTrue(banco.Registrado, "El banco se registra como interactuable");
            guardado.FijarRuta(Path.Combine(carpeta, "partida.json"));
            cartera.Cambio += (diferencia, motivo) => cambios.Add((diferencia, motivo));
            yield return null;

            Assert.AreEqual(DineroInicial, cartera.Dinero, "Empieza con $500");
            Assert.AreEqual(EstadoLlamado.EnTaller, llamado.Estado, "El mecha empieza en el taller");
            inicioJuego = Time.time;
        }

        // ---- Utilidades de entrada ----

        private IEnumerator Tecla(KeyControl tecla)
        {
            Press(tecla);
            yield return null;
            Release(tecla);
            yield return null;
        }

        private IEnumerator Esperar(float segundos)
        {
            float fin = Time.time + segundos;
            while (Time.time < fin)
            {
                yield return null;
            }
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

        private static float DistanciaPlana(Vector3 a, Vector3 b)
        {
            Vector3 delta = a - b;
            delta.y = 0f;
            return delta.magnitude;
        }

        private float AnguloHacia(Vector3 destino)
        {
            Transform camara = jugador.Camara.transform;
            return CalculoFlechaObjetivo.Angulo(camara.position, camara.forward, destino);
        }

        private IEnumerator CaminarA(Vector3 destino, float distancia, string que)
        {
            float fin = Time.time + MaxSegundosTramo;
            while (DistanciaPlana(jugador.transform.position, destino) > distancia)
            {
                if (Time.time > fin)
                {
                    Avanzar(false);
                    Assert.Fail("No llegó a: " + que + " (posición " + jugador.transform.position + ")");
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

        // ---- Pasos del recorrido ----

        // a) A pie al mecha, E, y el encendido (F, Espacio mantenido, Q).
        private IEnumerator SubirYEncender()
        {
            Vector3 escotilla = montaje.PuntoSubida;
            yield return CaminarA(escotilla, DistanciaLlegada, "la escotilla del mecha");
            yield return MirarA(escotilla, "la escotilla del mecha");
            yield return EsperarHasta(() => interaccion.TipoCandidato == TipoCandidatoAPie.Montable, 3f, "la interacción ofrece el mecha");
            yield return Tecla(teclado.eKey);
            Assert.AreEqual(ModoJugador.Mecha, jugador.Modo, "E sube al mecha");
            yield return null;

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
            }
            if (espacioPulsado)
            {
                Release(teclado.spaceKey);
            }
            Assert.IsTrue(encendido.EstaEncendido, "La cabina quedó encendida");
            yield return null;
        }

        private IEnumerator CaminarElMecha()
        {
            Press(teclado.wKey);
            yield return Esperar(SegundosCaminarMecha);
            Release(teclado.wKey);
            yield return null;
        }

        private static void Matar(IPeleador peleador)
        {
            ((VidaMecha)peleador.Vida).RecibirDanio(new InfoDanio(DanioMortal, TipoDanio.Golpe, Vector3.zero, Vector3.forward, null));
        }

        // Con la pelea en curso: el rival golpea de verdad al mecha (que hace clics al aire) hasta dejarlo con 70 % de vida o menos.
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
            }
            Release(raton.leftButton);
            yield return null;
            Assert.Less(vidaMecha.Actual, vidaMecha.Maxima, "El jugador recibió golpes reales");
            Assert.IsFalse(vidaMecha.EstaKO, "Y no perdió");
        }

        // Una pelea ganada: espera la pelea, deja caer al rival cuando el jugador ya salió dañado y espera el resultado con el premio.
        private IEnumerator GanarLaPelea(bool esperarGolpes)
        {
            yield return EsperarHasta(() => director.Estado == EstadoFlujoPelea.Peleando, MaxSegundosEstado, "empieza la pelea");
            if (esperarGolpes)
            {
                yield return RecibirGolpesReales();
            }
            int dineroAntes = cartera.Dinero;
            Matar(director.Rival);
            yield return EsperarHasta(() => director.Estado == EstadoFlujoPelea.Resultado, MaxSegundosEstado, "el resultado");
            Assert.AreEqual(dineroAntes + PremioEsperado, cartera.Dinero, "Ganar paga +$600");
        }

        // Camina al banco (el jugador sale de la arena cerrada: se le coloca junto al taller) hasta quedar cerca de su punto.
        private IEnumerator IrAlBanco()
        {
            ITallerMecha taller = Registro<ITallerMecha>.Actual;
            Vector3 punto = taller.PuntoInteraccion;
            jugador.Colocar(new Pose(new Vector3(punto.x, AlturaJugador, punto.z + 4f), Quaternion.Euler(0f, 180f, 0f)));
            yield return null;
            Press(teclado.wKey);
            float fin = Time.time + MaxSegundosCaminarBanco;
            while (Time.time < fin && jugador.Raiz.position.z > punto.z + MargenLlegadaBanco - 0.5f)
            {
                yield return null;
            }
            Release(teclado.wKey);
            yield return null;
            Assert.Less(Vector3.Distance(jugador.Raiz.position, punto), 2.5f, "Llegó al banco");
            yield return null;
        }

        // Captura de lo que ve el jugador (solo con CAPTURAR_T112=1). Los lienzos en Overlay no salen en una cámara a textura:
        // se pasan un instante a ScreenSpaceCamera con esa cámara y se devuelven como estaban.
        private void Capturar(string ruta)
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
            string completa = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ruta));
            Directory.CreateDirectory(Path.GetDirectoryName(completa));
            File.WriteAllBytes(completa, imagen.EncodeToPNG());
            Object.Destroy(rt);
            Object.Destroy(imagen);
            Assert.Greater(maximo - minimo, VariacionMinima, "La captura es plana: no se ve nada");
        }

        private void AssertSinErrores()
        {
            Assert.AreEqual(0, errores.Count, string.Join("\n", errores));
        }

        // ---- Prueba ----

        [UnityTest]
        public IEnumerator Recorrido_PeleaDaniadaRevanchaReparacionRentaYGuardado_JugandoComoUnaPersona()
        {
            yield return CargarCalle();

            // a) Sube al mecha, lo enciende, camina y P: pelea con vida completa; recibe golpes reales y gana.
            yield return SubirYEncender();
            Assert.AreEqual(vidaMecha.Maxima, vidaMecha.Actual, MargenVida, "Sube con la vida completa");
            yield return CaminarElMecha();
            yield return Tecla(teclado.pKey);
            Assert.AreEqual(EstadoFlujoPelea.Conteo, director.Estado, "P empieza el conteo");
            Assert.AreSame(vidaMecha, director.Jugador.Vida, "El director tomó al mecha estacionado como jugador");
            Assert.AreEqual(vidaMecha.Maxima, vidaMecha.Actual, MargenVida, "La pelea empieza con la vida completa");
            yield return GanarLaPelea(true);
            float vidaTrasPelea = vidaMecha.Actual;
            Assert.Less(vidaTrasPelea, vidaMecha.Maxima, "Sale dañado");
            Assert.Greater(vidaTrasPelea, 0f);
            int dineroTrasA = cartera.Dinero;
            Assert.AreEqual(DineroInicial + PremioEsperado, dineroTrasA);

            // b) R: la revancha empieza con la vida con que terminó la pelea y el rival completo; gana otra vez.
            yield return Tecla(teclado.rKey);
            Assert.AreEqual(EstadoFlujoPelea.Conteo, director.Estado, "R vuelve al conteo");
            Assert.AreEqual(vidaTrasPelea, vidaMecha.Actual, MargenVida, "La revancha empieza dañada igual");
            yield return null;
            Capturar(RutaCaptura1);
            Assert.AreEqual(director.Rival.Vida.Maxima, director.Rival.Vida.Actual, MargenVida, "El rival vuelve completo");
            Assert.IsFalse(director.Rival.Vida.EstaKO);
            yield return GanarLaPelea(false);
            Assert.AreEqual(vidaTrasPelea, vidaMecha.Actual, MargenVida, "Ganar la revancha sin golpes no cambia el daño");
            Assert.AreEqual(DineroInicial + PremioEsperado * 2, cartera.Dinero, "Dos premios");

            // c) Baja (E), M devuelve el mecha al taller con la misma vida; el banco cobra el costo exacto de reparar.
            yield return EsperarHasta(() => montaje.PuedeBajar, 5f, "el mecha puede bajar (detenido)");
            yield return Tecla(teclado.eKey);
            Assert.AreEqual(ModoJugador.APie, jugador.Modo, "E baja del mecha");
            yield return null;
            yield return Tecla(teclado.mKey);
            Assert.AreEqual(EstadoLlamado.Regresando, llamado.Estado, "M con el mecha vacío lo manda de vuelta");
            yield return EsperarHasta(() => llamado.Estado == EstadoLlamado.EnTaller, 10f, "el mecha vuelve al taller");
            Assert.AreEqual(vidaTrasPelea, vidaMecha.Actual, MargenVida, "Vuelve al taller con la misma vida");

            yield return IrAlBanco();
            DatosEconomia precios = ((ServicioCartera)cartera).Datos;
            int costo = ReglaReparacion.Costo(persistente.Danio, precios.CostoReparacionPorPunto, precios.CostoReparacionMinimo);
            Assert.Greater(costo, 0, "Hay algo que reparar");
            Assert.AreEqual("E · Reparar mecha · $" + costo, hud.TextoAvisoInteraccion.text, "El aviso dice el costo de ReglaReparacion");
            Capturar(RutaCaptura2);
            int dineroAntesReparar = cartera.Dinero;
            cambios.Clear();
            yield return Tecla(teclado.eKey);
            yield return Tecla(teclado.enterKey); // T-149: E abre la pantalla del banco; Enter elige "REPARAR TODO".
            Assert.AreEqual(dineroAntesReparar - costo, cartera.Dinero, "El dinero baja exactamente el costo");
            Assert.AreEqual(1, cambios.Count);
            Assert.AreEqual((-costo, MotivoDinero.Reparacion), cambios[0]);
            Assert.AreEqual(vidaMecha.Maxima, vidaMecha.Actual, MargenVida, "La vida queda al máximo");
            yield return null;
            Assert.IsTrue(hud.GrupoAvisoDinero.activeInHierarchy, "El HUD muestra el aviso del cobro");
            Capturar(RutaCaptura3);

            // d) Sin dinero y con el día 8: renta vencida, taller bloqueado; con dinero se paga y se desbloquea.
            reloj.Pausar(true);
            for (int i = 0; i < 20 && cartera.Dinero >= Renta; i++)
            {
                yield return Tecla(teclado.digit8Key);
            }
            Assert.Less(cartera.Dinero, Renta, "Quedó con menos de $500");
            for (int i = 0; i < 20 && reloj.Dia < DiaDeLaRenta; i++)
            {
                yield return Tecla(teclado.jKey);
            }
            Assert.AreEqual(DiaDeLaRenta, reloj.Dia, "J llegó al día 8");
            yield return null;
            yield return null;
            Assert.IsTrue(renta.TallerBloqueado, "La renta venció sin dinero");
            Assert.AreEqual(Renta, renta.Adeudo);
            Assert.AreEqual("RENTA VENCIDA · $500", hud.TextoRenta.text, "El HUD avisa la renta vencida");
            Assert.IsTrue(hud.GrupoRenta.activeInHierarchy);
            Assert.IsTrue(hud.TextoAvisoInteraccion.text.StartsWith("TALLER BLOQUEADO"), "El banco dice TALLER BLOQUEADO: " + hud.TextoAvisoInteraccion.text);
            int dineroBloqueado = cartera.Dinero;
            yield return Tecla(teclado.eKey);
            Assert.AreEqual(dineroBloqueado, cartera.Dinero, "Bloqueado: E no cobra");
            Assert.IsTrue(renta.TallerBloqueado);
            for (int i = 0; i < 20 && cartera.Dinero < Renta; i++)
            {
                yield return Tecla(teclado.digit7Key);
            }
            Assert.GreaterOrEqual(cartera.Dinero, Renta, "7 da dinero");
            yield return null;
            Assert.AreEqual("E · Pagar renta · $500", hud.TextoAvisoInteraccion.text);
            int dineroAntesRenta = cartera.Dinero;
            yield return Tecla(teclado.eKey);
            Assert.AreEqual(dineroAntesRenta - Renta, cartera.Dinero, "E paga la renta");
            Assert.IsFalse(renta.TallerBloqueado, "Se desbloquea el taller");
            Assert.AreEqual(0, renta.Adeudo);

            // e) Guardar (F5), cambiarlo todo y cargar (F9).
            vidaMecha.FijarVida(VidaGuardada);
            perfil.Fijar("Ana", "LA CHISPA");
            int dineroGuardado = cartera.Dinero;
            int diaGuardado = reloj.Dia;
            float horaGuardada = reloj.Hora;
            int proximoCobroGuardado = renta.DiaProximoCobro;
            int adeudoGuardado = renta.Adeudo;
            yield return Tecla(teclado.f5Key);
            Assert.IsTrue(guardado.HayPartida, "F5 escribió la partida");

            cartera.Abonar(777, MotivoDinero.Otro);
            persistente.Reparar();
            perfil.Fijar("Beto", "OTRO");
            yield return Tecla(teclado.jKey);
            Assert.AreNotEqual(diaGuardado, reloj.Dia, "J avanzó el día");
            Assert.AreNotEqual(dineroGuardado, cartera.Dinero);
            int cobrosAlCargar = 0;
            Action<int, MotivoDinero> contar = (d, m) => { if (m == MotivoDinero.Renta) cobrosAlCargar++; };
            cartera.Cambio += contar;
            yield return Tecla(teclado.f9Key);
            cartera.Cambio -= contar;

            Assert.AreEqual(dineroGuardado, cartera.Dinero, "F9 devuelve el dinero");
            Assert.AreEqual(VidaGuardada, vidaMecha.Actual, MargenVida, "F9 devuelve la vida del mecha");
            Assert.AreEqual(adeudoGuardado, renta.Adeudo, "F9 devuelve la renta");
            Assert.AreEqual(proximoCobroGuardado, renta.DiaProximoCobro);
            Assert.AreEqual("Ana", perfil.Nombre, "F9 devuelve el perfil");
            Assert.AreEqual("LA CHISPA", perfil.Alias);
            Assert.AreEqual(diaGuardado, reloj.Dia, "F9 devuelve el día");
            Assert.AreEqual(horaGuardada, reloj.Hora, MargenVida, "F9 devuelve la hora");
            Assert.AreEqual(0, cobrosAlCargar, "Cargar no cobra renta");

            // f) Sin errores y en menos de 120 s de juego.
            float segundos = Time.time - inicioJuego;
            Debug.Log("[Prueba T-112] Recorrido a-f: " + segundos.ToString("F1") + " s de juego, daño de la pelea "
                + (vidaMecha.Maxima - vidaTrasPelea).ToString("F1") + ", costo de reparar $" + costo);
            Assert.Less(segundos, MaxSegundosRecorrido, "El recorrido dura menos de 120 s de juego");
            AssertSinErrores();
        }
    }
}
