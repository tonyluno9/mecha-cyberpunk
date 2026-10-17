using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using MechaCyberpunk.Combate.Vida;
using MechaCyberpunk.Core;
using MechaCyberpunk.Economia;
using MechaCyberpunk.Mecha;
using MechaCyberpunk.Mecha.Estado;
using MechaCyberpunk.Mecha.Kits;
using MechaCyberpunk.Mecha.Montaje;
using MechaCyberpunk.Mecha.Taller;
using MechaCyberpunk.Pelea.Flujo;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Progresion.Guardado;
using MechaCyberpunk.Progresion.Prologo;
using MechaCyberpunk.Progresion.Pruebas;
using MechaCyberpunk.UI.Cabina;
using MechaCyberpunk.UI.Hud;
using MechaCyberpunk.UI.Peleas;
using MechaCyberpunk.UI.Taller;
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

namespace MechaCyberpunk.Tests.Progresion
{
    /// <summary>
    /// T-151: el ciclo completo de la economía (cierre del bloque 1 de T-F7) en Pruebas_Economia con entrada simulada: comprar un kit,
    /// apostar, pelear y usar el kit, ganar dinero y una pieza, venderla, reparar, perder una apuesta, reparar a medias y guardar y cargar.
    /// Todos los números salen de los assets. Atajos permitidos y anotados: Colocar cerca de un mostrador o del banco, RecibirDanio sobre el
    /// rival solo después de daño real al jugador y dejar la vida del mecha baja antes de la pelea que se pierde.
    /// </summary>
    public class RecorridoEconomiaPruebas : InputTestFixture
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Tony/Pruebas_Economia.unity";
        private const string CarpetaDatos = "Assets/_Proyecto/Economia/Datos/";
        private const string RutaEconomia = CarpetaDatos + "DatosEconomia_Base.asset";
        private const string RutaApuesta = CarpetaDatos + "DatosApuestaPelea_Casera.asset";
        private const string RutaPremio = CarpetaDatos + "DatosPremioPelea_Casera.asset";
        private const string RutaKit = CarpetaDatos + "DatosObjeto_KitReparacion.asset";
        private const string RutaBrazo = CarpetaDatos + "DatosObjeto_BrazoChatarra.asset";
        private const string RutaOfertaKit = CarpetaDatos + "DatosOferta_KitReparacion.asset";
        private const string AvisoBanco = "E · Reparar mecha";
        private const string VariableCaptura = "CAPTURAR_T151";
        private const string RutaCaptura1 = "docs/capturas/T-151-1.png";
        private const string RutaCaptura2 = "docs/capturas/T-151-2.png";
        private const string RutaCaptura3 = "docs/capturas/T-151-3.png";
        private const int AnchoCaptura = 1280;
        private const int AltoCaptura = 720;
        private const int ProfundidadCaptura = 24;
        private const int SaltoDePixeles = 97;
        private const float VariacionMinima = 0.05f;
        private const float DistanciaPlanoCanvas = 0.35f;
        private const int FramesEstables = 10;
        private const int MaxFramesCalentamiento = 1000;
        private const int FramesParaElHud = 5;
        private const int FramesSinMedirTrasEscena = 12;
        private const int FramesSinMedirTrasCaptura = 3;
        private const float DeltaTimeEstable = 0.05f;
        private const float MaxSegundosEstado = 60f;
        private const float MaxSegundosGolpes = 60f;
        private const float MaxSegundosSalida = 25f;
        private const float MaxSegundosCaminando = 10f;
        private const float MaxSegundosRecorrido = 400f;
        private const float SegundosEntreClics = 0.25f;
        private const float DanioMortal = 100000f;
        private const float FraccionVidaParaElKit = 0.7f;
        private const float FraccionVidaFregada = 0.2f;
        private const float MargenVida = 0.6f;
        private const float DistanciaAlMostrador = 2f;
        private const float DistanciaInicialMercado = 4f;
        private const float DistanciaAlBanco = 2f;
        private const float AlturaInicio = 0.05f;
        private const float SegundosAntesDeCapturarKit = 0.5f;

        private Keyboard teclado;
        private Mouse raton;
        private string carpeta;
        private string rutaReal;
        private string huellaReal;
        private JugadorAPie jugador;
        private MostradorPeleas mostradorPeleas;
        private EntradaArena entrada;
        private DirectorPelea director;
        private PantallaApuesta pantallaApuesta;
        private MostradorTienda mercado;
        private PantallaTienda pantallaTienda;
        private BancoTaller banco;
        private PantallaBancoTaller pantallaBanco;
        private MechaPersistente persistente;
        private VidaMecha vidaMecha;
        private UsoKitReparacion usoKit;
        private PantallaKitsCabina pantallaKits;
        private HudAPie hud;
        private ICartera cartera;
        private ServicioInventario inventario;
        private DatosEconomia precios;
        private DatosApuestaPelea reglasApuesta;
        private DatosPremioPelea premio;
        private DatosObjeto kit;
        private DatosObjeto brazo;
        private int framesSinMedir;
        private float danioDeLaPelea;
        private float curadoMedido;
        private readonly MedidorFrames medidor = new MedidorFrames();
        private readonly List<string> errores = new List<string>();
        private readonly List<string> atajos = new List<string>();
        private readonly List<string> tramos = new List<string>();
        private readonly List<ResultadoPelea> terminadas = new List<ResultadoPelea>();
        private readonly List<(int, MotivoDinero)> movimientos = new List<(int, MotivoDinero)>();
        private HashSet<GameObject> raicesIniciales;


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
            carpeta = Path.Combine(Application.temporaryCachePath, "recorrido_economia_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(carpeta);
            ServicioGuardado.RutaForzada = Path.Combine(carpeta, "partida.json");
            rutaReal = Path.Combine(Application.persistentDataPath, "partida.json");
            huellaReal = HuellaDe(rutaReal);
            errores.Clear();
            atajos.Clear();
            tramos.Clear();
            terminadas.Clear();
            movimientos.Clear();
            medidor.Reiniciar();
            framesSinMedir = 0;
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

        private static string HuellaDe(string ruta)
        {
            if (!File.Exists(ruta))
            {
                return "no existe";
            }
            FileInfo info = new FileInfo(ruta);
            return info.Length + " bytes, " + info.LastWriteTimeUtc.Ticks;
        }

        // ---- Esperas y medición ----

        private void Vigilar()
        {
            if (framesSinMedir > 0)
            {
                framesSinMedir--;
            }
            else
            {
                medidor.Registrar(Time.unscaledDeltaTime);
            }
        }

        private IEnumerator Cuadros(int cuantos)
        {
            for (int i = 0; i < cuantos; i++)
            {
                yield return null;
                Vigilar();
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

        // Pulsar y soltar con un cuadro entre los dos y otro después.
        private IEnumerator Tecla(KeyControl tecla)
        {
            Press(tecla);
            yield return null;
            Vigilar();
            Release(tecla);
            yield return null;
            Vigilar();
        }

        private void AnotarTramo(string nombre, float inicio)
        {
            tramos.Add(nombre + ": " + (Time.time - inicio).ToString("F1", CultureInfo.InvariantCulture) + " s");
        }

        private static float DistanciaPlana(Vector3 a, Vector3 b)
        {
            return Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));
        }

        // ---- Preparación ----

        private IEnumerator CargarEscena()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            framesSinMedir = FramesSinMedirTrasEscena;
            int estables = 0;
            int intentos = 0;
            while (estables < FramesEstables && intentos < MaxFramesCalentamiento)
            {
                yield return null;
                Vigilar();
                intentos++;
                estables = Time.deltaTime < DeltaTimeEstable ? estables + 1 : 0;
            }
            jugador = Object.FindFirstObjectByType<JugadorAPie>();
            mostradorPeleas = Object.FindFirstObjectByType<MostradorPeleas>();
            mercado = Object.FindFirstObjectByType<MostradorTienda>();
            banco = Object.FindFirstObjectByType<BancoTaller>();
            hud = Object.FindFirstObjectByType<HudAPie>();
            MontajeMecha montaje = Object.FindFirstObjectByType<MontajeMecha>();
            Assert.IsNotNull(jugador, "Pruebas_Economia trae al jugador");
            Assert.IsNotNull(mostradorPeleas, "Trae el mostrador de peleas");
            Assert.IsNotNull(mercado, "Trae el mostrador del mercado negro");
            Assert.IsNotNull(banco, "El mecha trae el banco del taller");
            Assert.IsNotNull(hud, "El jugador trae el HUD a pie");
            Assert.IsNotNull(montaje, "Trae el mecha de la historia");
            entrada = mostradorPeleas.GetComponent<EntradaArena>();
            director = mostradorPeleas.GetComponentInChildren<DirectorPelea>(true);
            pantallaApuesta = mostradorPeleas.GetComponentInChildren<PantallaApuesta>(true);
            pantallaTienda = mercado.GetComponentInChildren<PantallaTienda>(true);
            pantallaBanco = banco.GetComponentInChildren<PantallaBancoTaller>(true);
            persistente = montaje.GetComponent<MechaPersistente>();
            vidaMecha = montaje.GetComponent<VidaMecha>();
            usoKit = montaje.GetComponentInChildren<UsoKitReparacion>(true);
            pantallaKits = montaje.GetComponentInChildren<PantallaKitsCabina>(true);
            Assert.IsNotNull(entrada, "El mostrador trae la entrada a la arena");
            Assert.IsNotNull(director, "El mostrador trae el flujo casero");
            Assert.IsNotNull(pantallaApuesta, "El mostrador trae la pantalla de apuesta");
            Assert.IsNotNull(pantallaTienda, "El mercado trae su pantalla");
            Assert.IsNotNull(pantallaBanco, "El mecha trae la pantalla del banco");
            Assert.IsNotNull(persistente, "El mecha es persistente");
            Assert.IsNotNull(usoKit, "El mecha trae el uso de kits");
            Assert.IsNotNull(pantallaKits, "El mecha trae la pantalla de kits");
            cartera = Registro<ICartera>.Actual;
            inventario = Registro<ServicioInventario>.Actual;
            Assert.IsNotNull(cartera, "Hay cartera");
            Assert.IsNotNull(inventario, "Hay inventario");
            Assert.IsNotNull(Registro<IApuestasPelea>.Actual, "Hay servicio de apuestas");
            precios = AssetDatabase.LoadAssetAtPath<DatosEconomia>(RutaEconomia);
            reglasApuesta = AssetDatabase.LoadAssetAtPath<DatosApuestaPelea>(RutaApuesta);
            premio = AssetDatabase.LoadAssetAtPath<DatosPremioPelea>(RutaPremio);
            kit = AssetDatabase.LoadAssetAtPath<DatosObjeto>(RutaKit);
            brazo = AssetDatabase.LoadAssetAtPath<DatosObjeto>(RutaBrazo);
            Assert.IsNotNull(precios);
            Assert.IsNotNull(reglasApuesta);
            Assert.IsNotNull(premio);
            Assert.IsNotNull(kit);
            Assert.IsNotNull(brazo);
            cartera.Cambio += (delta, motivo) => movimientos.Add((delta, motivo));
            EventosPelea.PeleaTerminada += terminadas.Add;
            yield return null;
            Vigilar();
        }

        // ---- Entrada ----

        // Pone al jugador a 'lejos' m al sur del mostrador, mirando a él, y camina con W hasta quedar a 2 m.
        private IEnumerator IrAlMostrador(Vector3 posicionMostrador, float lejos, string atajo)
        {
            Vector3 punto = posicionMostrador + new Vector3(0f, AlturaInicio, -lejos);
            jugador.Colocar(new Pose(punto, Quaternion.identity));
            atajos.Add("Colocar (" + atajo + "): a " + lejos.ToString("F0", CultureInfo.InvariantCulture) + " m del mostrador");
            yield return Cuadros(FramesParaElHud);
            Press(teclado.wKey);
            float fin = Time.time + MaxSegundosCaminando;
            while (DistanciaPlana(jugador.Raiz.position, posicionMostrador) > DistanciaAlMostrador && Time.time < fin)
            {
                yield return null;
                Vigilar();
            }
            Release(teclado.wKey);
            yield return null;
            Vigilar();
            Assert.LessOrEqual(DistanciaPlana(jugador.Raiz.position, posicionMostrador), DistanciaAlMostrador + 0.2f,
                "W llevó al jugador a " + atajo);
            yield return Cuadros(FramesParaElHud);
        }

        private IEnumerator IrAlBanco()
        {
            Vector3 punto = Registro<ITallerMecha>.Actual.PuntoInteraccion;
            jugador.Colocar(new Pose(new Vector3(punto.x, AlturaInicio, punto.z + DistanciaAlBanco), Quaternion.Euler(0f, 180f, 0f)));
            atajos.Add("Colocar (banco): a 2 m del banco");
            float fin = Time.time + MaxSegundosCaminando;
            while (Time.time < fin && !hud.TextoAvisoInteraccion.text.StartsWith(AvisoBanco, StringComparison.Ordinal))
            {
                yield return null;
                Vigilar();
            }
            Assert.IsTrue(hud.TextoAvisoInteraccion.text.StartsWith(AvisoBanco, StringComparison.Ordinal),
                "El HUD ofrece usar el banco; dice: " + hud.TextoAvisoInteraccion.text);
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
                Vigilar();
            }
            Release(raton.leftButton);
            yield return null;
            Vigilar();
        }

        private IEnumerator EsperarPelea()
        {
            yield return EsperarHasta(() => jugador.Modo == ModoJugador.Mecha, MaxSegundosEstado, "el jugador va en el mecha");
            yield return EsperarHasta(() => director.Estado == EstadoFlujoPelea.Peleando, MaxSegundosEstado, "la pelea");
            Assert.IsNotNull(director.Rival, "Hay rival");
        }

        private IEnumerator EsperarVueltaAPie()
        {
            yield return EsperarHasta(() => director.Estado == EstadoFlujoPelea.Resultado, MaxSegundosEstado, "el resultado");
            yield return EsperarHasta(() => !entrada.EnCurso, MaxSegundosSalida, "termina la salida");
            yield return null;
            Vigilar();
            Assert.AreEqual(ModoJugador.APie, jugador.Modo, "Volvió a pie");
        }

        // E en el mostrador de peleas: abre la pantalla de apuesta y elige el monto con → (0 = sin apuesta, 1 = el primero...).
        private IEnumerator AbrirApuestaYElegir(int monto)
        {
            Assert.IsTrue(mostradorPeleas.PuedeUsar, "El mostrador ofrece la pelea: " + mostradorPeleas.Texto);
            yield return Tecla(teclado.eKey);
            Assert.IsTrue(pantallaApuesta.Visible, "E abre la pantalla de apuesta");
            int[] montos = reglasApuesta.Montos;
            int indice = Array.IndexOf(montos, monto) + 1;
            Assert.Greater(indice, 0, "El monto $" + monto + " está entre los montos del asset");
            for (int i = 0; i < indice; i++)
            {
                yield return Tecla(teclado.rightArrowKey);
            }
            Assert.AreEqual(monto, pantallaApuesta.Monto, "→ elige $" + monto);
        }

        // ---- Capturas (solo con CAPTURAR_T151=1) ----

        private static bool CapturaPedida => Environment.GetEnvironmentVariable(VariableCaptura) == "1";

        // Los lienzos en Overlay no salen en una cámara a textura: se pasan un instante a ScreenSpaceCamera con la cámara del jugador.
        private void Capturar(string ruta)
        {
            if (!CapturaPedida)
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
            framesSinMedir = FramesSinMedirTrasCaptura;
            Assert.Greater(maximo - minimo, VariacionMinima, "La captura es plana: " + ruta);
        }

        // ---- Tramos ----

        // a) Inicio.
        private void Inicio()
        {
            Assert.AreEqual(precios.DineroInicial, cartera.Dinero, "a) empieza con el dinero inicial del asset");
            Assert.AreEqual(vidaMecha.Maxima, vidaMecha.Actual, MargenVida, "a) el mecha tiene la vida completa");
            Assert.AreEqual(0f, persistente.Danio, MargenVida, "a) sin daño");
            Assert.AreEqual(EstadoBanco.SinDanio, banco.Estado, "a) el mecha está en su taller y no hay nada que reparar");
            foreach (DatosObjeto objeto in inventario.Todos)
            {
                Assert.AreEqual(0, inventario.Cantidad(objeto), "a) inventario vacío: " + objeto.Nombre);
            }
            Assert.AreEqual(0, inventario.Cantidad(kit));
            Assert.AreEqual(0, inventario.Cantidad(brazo));
        }

        // b) Mercado: compra un kit con E, Enter.
        private IEnumerator ComprarElKit()
        {
            int precioKit = AssetDatabase.LoadAssetAtPath<DatosOferta>(RutaOfertaKit).Precio;
            int antes = cartera.Dinero;
            yield return IrAlMostrador(mercado.transform.position, DistanciaInicialMercado, "mercado");
            Assert.IsTrue(mercado.PuedeUsar, "El mercado se ofrece: " + mercado.Texto);
            yield return Tecla(teclado.eKey);
            Assert.IsTrue(pantallaTienda.Visible, "E abre la tienda");
            Assert.AreEqual(PestanaTienda.Comprar, pantallaTienda.Pestana, "Abre en COMPRAR");
            StringAssert.Contains("KIT", pantallaTienda.TextoFila(0).ToUpperInvariant(), "La primera fila es el kit");
            yield return Tecla(teclado.enterKey);
            Assert.AreEqual(antes - precioKit, cartera.Dinero, "b) el kit cuesta $" + precioKit);
            Assert.AreEqual(1, inventario.Cantidad(kit), "b) el kit entró al inventario");
            Assert.AreEqual((-precioKit, MotivoDinero.Compra), movimientos[movimientos.Count - 1], "b) el cobro es una Compra");
            yield return Tecla(teclado.escapeKey);
            yield return Cuadros(FramesParaElHud);
            Assert.IsFalse(pantallaTienda.Visible, "Esc cierra la tienda");
        }

        // c) Apuesta $250, pelea la casera, daño real, un kit con G y gana.
        private IEnumerator ApostarPelearUsarElKitYGanar(int apuesta)
        {
            int dineroAntes = cartera.Dinero;
            yield return IrAlMostrador(mostradorPeleas.transform.position, DistanciaAlMostrador, "mostrador de peleas");
            yield return AbrirApuestaYElegir(apuesta);
            yield return Cuadros(FramesParaElHud);
            Capturar(RutaCaptura1);
            yield return Tecla(teclado.enterKey);
            Assert.IsFalse(pantallaApuesta.Visible, "Enter cierra la pantalla");
            Assert.AreEqual(dineroAntes - apuesta, cartera.Dinero, "c) se cobra lo apostado");
            Assert.AreEqual(apuesta, Registro<IApuestasPelea>.Actual.Apostado, "c) la apuesta queda abierta");
            yield return EsperarPelea();

            // Daño real al jugador hasta el 70 % de la vida: así el kit cura sus 20 puntos completos.
            float limite = vidaMecha.Maxima * FraccionVidaParaElKit;
            yield return ClicsHasta(() => vidaMecha.Actual <= limite || vidaMecha.EstaKO);
            Assert.IsFalse(vidaMecha.EstaKO, "c) y no perdió");
            Assert.LessOrEqual(vidaMecha.Actual, limite, "c) el jugador recibió golpes reales");

            // G una vez: gasta el kit al instante y la vida sube lo que dice la regla (se descuenta el daño recibido en el camino).
            ConfigKitReparacion config = usoKit.Config;
            float esperado = ReglaKitReparacion.Curacion(vidaMecha.Actual, vidaMecha.Maxima, config.FraccionCuracion);
            float danioRecibido = 0f;
            Action<IPeleador, InfoDanio, ResultadoDanio> sumar = (objetivo, info, resultado) =>
            {
                if (objetivo != null && objetivo.Lado == LadoPelea.Jugador)
                {
                    danioRecibido += resultado.DanioAplicado;
                }
            };
            EventosPelea.ImpactoRecibido += sumar;
            float vidaAntesDelKit = vidaMecha.Actual;
            float inicioKit = Time.time;
            yield return Tecla(teclado.gKey);
            Assert.AreEqual(0, inventario.Cantidad(kit), "c) G gasta el único kit al instante");
            Assert.AreEqual(1, usoKit.Usados, "c) un uso en la pelea");
            Assert.IsTrue(usoKit.Curando, "c) empieza a curar");
            yield return Cuadros(FramesParaElHud);
            Capturar(RutaCaptura2);
            yield return EsperarHasta(() => !usoKit.Curando || vidaMecha.EstaKO, config.SegundosCuracion + 5f, "termina la curación");
            EventosPelea.ImpactoRecibido -= sumar;
            Assert.IsFalse(vidaMecha.EstaKO, "c) el kit no dejó a nadie en KO");
            curadoMedido = vidaMecha.Actual - vidaAntesDelKit + danioRecibido;
            Assert.AreEqual(esperado, curadoMedido, MargenVida, "c) la vida subió lo de la regla (20 % de la máxima)");
            Debug.Log("[Prueba T-151] Kit: curó " + curadoMedido.ToString("F1", CultureInfo.InvariantCulture) + " en "
                + (Time.time - inicioKit).ToString("F1", CultureInfo.InvariantCulture) + " s (regla " + esperado.ToString("F1", CultureInfo.InvariantCulture) + ")");

            // Se remata al rival solo después del daño real al jugador.
            ((VidaMecha)director.Rival.Vida).RecibirDanio(new InfoDanio(DanioMortal, TipoDanio.Golpe, Vector3.zero, Vector3.forward, null));
            atajos.Add("RecibirDanio: rival casero rematado tras daño real al mecha");
            yield return EsperarVueltaAPie();
            danioDeLaPelea = persistente.Danio;
        }

        // d) Vende el brazo en el mercado.
        private IEnumerator VenderElBrazo()
        {
            int antes = cartera.Dinero;
            yield return IrAlMostrador(mercado.transform.position, DistanciaInicialMercado, "mercado (venta)");
            Assert.IsTrue(mercado.PuedeUsar, "El mercado se ofrece: " + mercado.Texto);
            yield return Tecla(teclado.eKey);
            Assert.IsTrue(pantallaTienda.Visible, "E abre la tienda");
            Assert.IsTrue(pantallaTienda.PuedeVender, "El mercado negro compra");
            yield return Tecla(teclado.rightArrowKey);
            Assert.AreEqual(PestanaTienda.Vender, pantallaTienda.Pestana, "→ pasa a VENDER");
            Assert.AreEqual(1, pantallaTienda.CantidadFilas, "Solo el brazo");
            yield return Cuadros(FramesParaElHud);
            Capturar(RutaCaptura3);
            yield return Tecla(teclado.enterKey);
            Assert.AreEqual(antes + brazo.ValorVenta, cartera.Dinero, "d) la venta paga el valor del asset ($" + brazo.ValorVenta + ")");
            Assert.AreEqual(0, inventario.Cantidad(brazo), "d) el brazo salió del inventario");
            Assert.AreEqual((brazo.ValorVenta, MotivoDinero.Venta), movimientos[movimientos.Count - 1], "d) el motivo es Venta");
            yield return Cuadros(FramesParaElHud);
            Assert.AreEqual("+" + FormatoDinero.Texto(brazo.ValorVenta) + " · VENTA", hud.TextoAvisoDinero.text, "d) el HUD anuncia la venta");
            yield return Tecla(teclado.escapeKey);
            yield return Cuadros(FramesParaElHud);
        }

        // e) Banco: REPARAR TODO.
        private IEnumerator RepararTodo()
        {
            float danio = persistente.Danio;
            Assert.Greater(danio, 0f, "e) hay daño que reparar");
            int costo = ReglaReparacion.Costo(danio, precios.CostoReparacionPorPunto, precios.CostoReparacionMinimo);
            int antes = cartera.Dinero;
            yield return IrAlBanco();
            StringAssert.StartsWith(AvisoBanco, hud.TextoAvisoInteraccion.text, "e) el HUD ofrece reparar");
            yield return Tecla(teclado.eKey);
            Assert.IsTrue(pantallaBanco.Visible, "E abre la pantalla del banco");
            Assert.AreEqual(0, pantallaBanco.IndiceElegido, "Empieza en REPARAR TODO");
            StringAssert.StartsWith("REPARAR TODO", pantallaBanco.TextoBaseOpcion(0));
            yield return Tecla(teclado.enterKey);
            Assert.AreEqual(antes - costo, cartera.Dinero, "e) cobró exactamente ReglaReparacion ($" + costo + " por " + danio.ToString("F1", CultureInfo.InvariantCulture) + " puntos)");
            Assert.AreEqual((-costo, MotivoDinero.Reparacion), movimientos[movimientos.Count - 1], "e) el motivo es Reparación");
            Assert.AreEqual(vidaMecha.Maxima, vidaMecha.Actual, MargenVida, "e) la vida quedó completa");
            Assert.AreEqual(0f, persistente.Danio, MargenVida);
        }

        // f) Segunda apuesta: se pierde con golpes reales.
        private IEnumerator ApostarYPerder(int apuesta)
        {
            // Atajo anotado: vida inicial baja para que el rival gane en poco tiempo (como en T-145).
            vidaMecha.FijarVida(vidaMecha.Maxima * FraccionVidaFregada);
            atajos.Add("FijarVida: el mecha sale de reparar y entra a la segunda pelea con el 20 % de vida");
            int dineroAntes = cartera.Dinero;
            int brazosAntes = inventario.Cantidad(brazo);
            int resultadosAntes = terminadas.Count;
            yield return IrAlMostrador(mostradorPeleas.transform.position, DistanciaAlMostrador, "mostrador de peleas (2)");
            yield return AbrirApuestaYElegir(apuesta);
            yield return Tecla(teclado.enterKey);
            Assert.AreEqual(dineroAntes - apuesta, cartera.Dinero, "f) se cobra lo apostado");
            yield return EsperarPelea();
            yield return ClicsHasta(() => vidaMecha.EstaKO);
            Assert.IsTrue(vidaMecha.EstaKO, "f) el rival lo dejó en KO con golpes reales");
            yield return EsperarVueltaAPie();
            Assert.AreEqual(resultadosAntes + 1, terminadas.Count, "f) la pelea terminó una vez");
            Assert.AreEqual(LadoPelea.Rival, terminadas[terminadas.Count - 1].Ganador, "f) ganó el rival");
            Assert.AreEqual(dineroAntes - apuesta + premio.PremioPerder, cartera.Dinero, "f) perdió lo apostado y no hay premio");
            Assert.AreEqual(brazosAntes, inventario.Cantidad(brazo), "f) sin pieza");
            Assert.IsFalse(Registro<IApuestasPelea>.Actual.Abierta, "f) no queda apuesta abierta");
            danioDeLaPelea = persistente.Danio;
        }

        // g) Banco: REPARAR LA MITAD.
        private IEnumerator RepararLaMitad()
        {
            float danio = persistente.Danio;
            float vidaAntes = vidaMecha.Actual;
            int puntos = ReglaReparacion.PuntosDeLaMitad(danio, precios.CostoReparacionPorPunto, precios.CostoReparacionMinimo);
            Assert.Greater(puntos, 0, "g) el daño ($" + danio + ") permite reparar la mitad");
            int costo = ReglaReparacion.Costo(puntos, precios.CostoReparacionPorPunto, precios.CostoReparacionMinimo);
            int antes = cartera.Dinero;
            yield return IrAlBanco();
            yield return Tecla(teclado.eKey);
            Assert.IsTrue(pantallaBanco.Visible, "E abre la pantalla del banco");
            Assert.GreaterOrEqual(pantallaBanco.CantidadOpciones, 2, "Hay REPARAR TODO y REPARAR LA MITAD");
            StringAssert.StartsWith("REPARAR LA MITAD", pantallaBanco.TextoBaseOpcion(1));
            yield return Tecla(teclado.downArrowKey);
            Assert.AreEqual(1, pantallaBanco.IndiceElegido, "↓ elige la mitad");
            yield return Tecla(teclado.enterKey);
            Assert.AreEqual(antes - costo, cartera.Dinero, "g) cobró lo de la regla ($" + costo + " por " + puntos + " puntos)");
            Assert.AreEqual((-costo, MotivoDinero.Reparacion), movimientos[movimientos.Count - 1]);
            Assert.AreEqual(vidaAntes + puntos, vidaMecha.Actual, MargenVida, "g) reparó la mitad del daño");
            Assert.AreEqual(danio - puntos, persistente.Danio, MargenVida, "g) queda la otra mitad de daño");
            Assert.IsFalse(vidaMecha.EstaKO, "g) ya no está en KO");
        }

        // h) F5, cambia cosas por código, F9: todo vuelve a lo guardado.
        private IEnumerator GuardarYCargar()
        {
            int dineroGuardado = cartera.Dinero;
            float vidaGuardada = vidaMecha.Actual;
            Assert.AreEqual(0, inventario.Cantidad(kit));
            yield return Tecla(teclado.f5Key);
            Assert.IsTrue(ServicioGuardado.Actual.HayPartida, "h) F5 escribió la partida en la ruta temporal");
            Assert.IsTrue(File.Exists(ServicioGuardado.RutaForzada));

            cartera.Abonar(777, MotivoDinero.Otro);
            Assert.AreEqual(2, inventario.Agregar(kit, 2));
            Assert.AreEqual(1, inventario.Agregar(brazo, 1));
            persistente.Reparar();
            Assert.AreNotEqual(dineroGuardado, cartera.Dinero);

            yield return Tecla(teclado.f9Key);
            Assert.AreEqual(dineroGuardado, cartera.Dinero, "h) F9 devuelve el dinero");
            Assert.AreEqual(0, inventario.Cantidad(kit), "h) F9 devuelve el inventario (kits)");
            Assert.AreEqual(0, inventario.Cantidad(brazo), "h) F9 devuelve el inventario (brazo)");
            Assert.AreEqual(vidaGuardada, vidaMecha.Actual, MargenVida, "h) F9 devuelve la vida del mecha");
            Assert.IsFalse(Registro<IApuestasPelea>.Actual.Abierta, "h) no hay apuesta abierta");
            Assert.AreEqual(0, Registro<IApuestasPelea>.Actual.Apostado, "h) ni apostado");
        }

        // ---- Prueba ----

        [UnityTest]
        public IEnumerator Recorrido_ComprarApostarPelearVenderRepararPerderGuardar_JugandoComoUnaPersona()
        {
            yield return CargarEscena();
            float inicioJuego = Time.time;
            int apuestaGanada = reglasApuesta.Montos[1];
            int apuestaPerdida = reglasApuesta.Montos[0];
            Assert.GreaterOrEqual(apuestaGanada, reglasApuesta.ApuestaMinimaPorPieza, "La primera apuesta lleva la pieza");
            Assert.Less(apuestaPerdida, reglasApuesta.ApuestaMinimaPorPieza, "La segunda no lleva pieza");

            float t = Time.time;
            Inicio();
            AnotarTramo("a) inicio", t);

            t = Time.time;
            int precioKit = AssetDatabase.LoadAssetAtPath<DatosOferta>(RutaOfertaKit).Precio;
            yield return ComprarElKit();
            AnotarTramo("b) comprar el kit", t);

            t = Time.time;
            yield return ApostarPelearUsarElKitYGanar(apuestaGanada);
            int ganancia = (int)Math.Round(apuestaGanada * (double)reglasApuesta.Momio, MidpointRounding.AwayFromZero);
            int dineroEsperado = precios.DineroInicial - precioKit + premio.PremioGanar + ganancia;
            Assert.AreEqual(dineroEsperado, cartera.Dinero, "c) dinero = inicial - kit + premio + apuesta x momio (la apuesta se cobró y se devolvió)");
            Assert.AreEqual(1, inventario.Cantidad(brazo), "c) se llevó el Brazo de chatarra");
            Assert.AreEqual(LadoPelea.Jugador, terminadas[terminadas.Count - 1].Ganador, "c) ganó el jugador");
            AnotarTramo("c) apostar, pelear, kit y ganar", t);

            t = Time.time;
            yield return VenderElBrazo();
            dineroEsperado += brazo.ValorVenta;
            Assert.AreEqual(dineroEsperado, cartera.Dinero);
            AnotarTramo("d) vender el brazo", t);

            t = Time.time;
            int costoTodo = ReglaReparacion.Costo(persistente.Danio, precios.CostoReparacionPorPunto, precios.CostoReparacionMinimo);
            yield return RepararTodo();
            dineroEsperado -= costoTodo;
            Assert.AreEqual(dineroEsperado, cartera.Dinero);
            AnotarTramo("e) reparar todo", t);

            t = Time.time;
            yield return ApostarYPerder(apuestaPerdida);
            dineroEsperado -= apuestaPerdida;
            Assert.AreEqual(dineroEsperado, cartera.Dinero);
            AnotarTramo("f) apostar y perder", t);

            t = Time.time;
            float danioAntesDeLaMitad = persistente.Danio;
            int puntosMitad = ReglaReparacion.PuntosDeLaMitad(danioAntesDeLaMitad, precios.CostoReparacionPorPunto, precios.CostoReparacionMinimo);
            int costoMitad = ReglaReparacion.Costo(puntosMitad, precios.CostoReparacionPorPunto, precios.CostoReparacionMinimo);
            yield return RepararLaMitad();
            dineroEsperado -= costoMitad;
            Assert.AreEqual(dineroEsperado, cartera.Dinero, "g) dinero final calculado desde los assets");
            AnotarTramo("g) reparar la mitad", t);

            t = Time.time;
            yield return GuardarYCargar();
            Assert.AreEqual(dineroEsperado, cartera.Dinero, "h) el dinero final sigue siendo el calculado");
            AnotarTramo("h) guardar y cargar", t);

            // i) Consola, tiempos, frames y la partida real intacta.
            float segundos = Time.time - inicioJuego;
            Debug.Log("[Prueba T-151] Dinero final $" + cartera.Dinero + " (calculado desde los assets $" + dineroEsperado + "), tiempo de juego "
                + segundos.ToString("F1", CultureInfo.InvariantCulture) + " s");
            Debug.Log("[Prueba T-151] Tramos: " + string.Join(" | ", tramos));
            Debug.Log("[Prueba T-151] Atajos: " + string.Join(" | ", atajos));
            Debug.Log("[Prueba T-151] MedidorFrames: " + medidor.Resumen());
            Assert.Less(segundos, MaxSegundosRecorrido, "El recorrido dura menos de " + MaxSegundosRecorrido + " s de juego");
            Assert.IsTrue(medidor.Cumple, "Sin tirones de frames: " + medidor.Resumen());
            Assert.AreEqual(huellaReal, HuellaDe(rutaReal), "La partida real (persistentDataPath) no se tocó");
            Assert.IsEmpty(errores, "Consola sin errores: " + string.Join(" | ", errores));
        }
    }
}
