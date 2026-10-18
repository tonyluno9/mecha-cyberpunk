using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using MechaCyberpunk.Combate.Vida;
using MechaCyberpunk.Core;
using MechaCyberpunk.Economia;
using MechaCyberpunk.IA.Datos;
using MechaCyberpunk.IA.Rival;
using MechaCyberpunk.Mecha;
using MechaCyberpunk.Mecha.Estado;
using MechaCyberpunk.Mecha.Montaje;
using MechaCyberpunk.Mecha.Taller;
using MechaCyberpunk.Pelea.Datos;
using MechaCyberpunk.Pelea.Flujo;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Progresion;
using MechaCyberpunk.Progresion.Guardado;
using MechaCyberpunk.Progresion.Prologo;
using MechaCyberpunk.Progresion.Pruebas;
using MechaCyberpunk.UI.Hud;
using MechaCyberpunk.UI.Peleas;
using MechaCyberpunk.UI.Taller;
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
    /// T-162: el camino de las ligas (cierre del bloque 2 de T-F7) en Pruebas_Economia con entrada simulada: retar desde la cartelera,
    /// apostar, ganar, perder y reintentar, revancha, el jefe en su furia, subir de liga, guardar y cargar, y vencer al campeón de la
    /// última liga. Los números salen de los assets. Atajos permitidos y anotados: Colocar cerca del mostrador o del banco, dinero inicial
    /// por código, FijarVida antes de la pelea que se pierde, Poner en el progreso donde se indica, y RecibirDanio sobre el rival solo
    /// después de daño real al jugador.
    /// </summary>
    public class RecorridoLigasPruebas : InputTestFixture
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Tony/Pruebas_Economia.unity";
        private const string RutaEconomia = "Assets/_Proyecto/Economia/Datos/DatosEconomia_Base.asset";
        private const string RutaCalendario = "Assets/_Proyecto/Pelea/Datos/CalendarioLigas_Base.asset";
        private const string RutaFuria = "Assets/_Proyecto/IA/Datos/ConfigFuriaJefe_Base.asset";
        private const string AvisoBanco = "E · Reparar mecha";
        private const string AliasPrimero = "TORNILLO";
        private const string AliasSegundo = "LA RATA";
        private const string AliasJefe = "DON FIERRO";
        private const string AliasCampeon = "EL CACIQUE";
        private const string TextoLetreroJefe = "PELEA DE JEFE";
        private const string TextoVencisteCampeon = "¡VENCISTE AL CAMPEÓN!";
        private const string TextoCampeonSotano = "¡CAMPEÓN DE LA LIGA SÓTANO!";
        private const string TextoSeAbreCalle = "SE ABRE LA LIGA CALLE";
        private const string TextoCampeonClandestino = "¡CAMPEÓN DEL CLANDESTINO!";
        private const string VariableCaptura = "CAPTURAR_T162";
        private const string RutaCaptura1 = "docs/capturas/T-162-1.png";
        private const string RutaCaptura2 = "docs/capturas/T-162-2.png";
        private const string RutaCaptura3 = "docs/capturas/T-162-3.png";
        private const int DineroDePrueba = 3000;
        private const int Sotano = 1;
        private const int Calle = 2;
        private const int UltimaLiga = 4;
        private const int RivalesPorLiga = 5;
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
        private const float MaxSegundosEstado = 90f;
        private const float MaxSegundosGolpes = 90f;
        private const float MaxSegundosSalida = 30f;
        private const float MaxSegundosCaminando = 10f;
        private const float MaxSegundosAviso = 10f;
        private const float MaxSegundosRecorrido = 1400f;
        private const float SegundosEntreClics = 0.25f;
        private const float DanioMortal = 100000f;
        private const float FraccionDanioBuscada = 0.9f;
        private const float FraccionVidaFregada = 0.2f;
        private const float MargenFuria = 0.1f;
        private const float MargenVida = 0.6f;
        private const float DistanciaAlMostrador = 2f;
        private const float DistanciaAlBanco = 2f;
        private const float AlturaInicio = 0.05f;

        private Keyboard teclado;
        private Mouse raton;
        private string carpeta;
        private string rutaReal;
        private string huellaReal;
        private JugadorAPie jugador;
        private MostradorPeleas mostrador;
        private EntradaArena entrada;
        private DirectorPelea director;
        private PantallaApuesta pantallaApuesta;
        private PantallaCartelera pantallaCartelera;
        private BancoTaller banco;
        private PantallaBancoTaller pantallaBanco;
        private MechaPersistente persistente;
        private VidaMecha vidaMecha;
        private HudAPie hud;
        private ICartera cartera;
        private ServicioLigas ligas;
        private DatosEconomia precios;
        private CalendarioLigas calendario;
        private ConfigFuriaJefe configFuria;
        private int dineroEsperado;
        private int framesSinMedir;
        private bool letreroVisto;
        private bool vencisteVisto;
        private readonly MedidorFrames medidor = new MedidorFrames();
        private readonly List<string> errores = new List<string>();
        private readonly List<string> atajos = new List<string>();
        private readonly List<string> tramos = new List<string>();
        private readonly List<ResultadoPelea> terminadas = new List<ResultadoPelea>();
        private readonly List<int> campeones = new List<int>();
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
            carpeta = Path.Combine(Application.temporaryCachePath, "recorrido_ligas_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(carpeta);
            ServicioGuardado.RutaForzada = Path.Combine(carpeta, "partida.json");
            rutaReal = Path.Combine(Application.persistentDataPath, "partida.json");
            huellaReal = HuellaDe(rutaReal);
            errores.Clear();
            atajos.Clear();
            tramos.Clear();
            terminadas.Clear();
            campeones.Clear();
            medidor.Reiniciar();
            framesSinMedir = 0;
            letreroVisto = false;
            vencisteVisto = false;
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
            if (director != null && director.Pantalla != null)
            {
                letreroVisto |= director.Pantalla.Jefe == TextoLetreroJefe;
                vencisteVisto |= director.Pantalla.Jefe == TextoVencisteCampeon;
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
            mostrador = Object.FindFirstObjectByType<MostradorPeleas>();
            banco = Object.FindFirstObjectByType<BancoTaller>();
            hud = Object.FindFirstObjectByType<HudAPie>();
            MontajeMecha montaje = Object.FindFirstObjectByType<MontajeMecha>();
            Assert.IsNotNull(jugador, "Pruebas_Economia trae al jugador");
            Assert.IsNotNull(mostrador, "Trae el mostrador de peleas");
            Assert.IsNotNull(banco, "El mecha trae el banco del taller");
            Assert.IsNotNull(hud, "El jugador trae el HUD a pie");
            Assert.IsNotNull(montaje, "Trae el mecha de la historia");
            entrada = mostrador.GetComponent<EntradaArena>();
            director = mostrador.GetComponentInChildren<DirectorPelea>(true);
            pantallaApuesta = mostrador.GetComponentInChildren<PantallaApuesta>(true);
            pantallaCartelera = mostrador.GetComponentInChildren<PantallaCartelera>(true);
            pantallaBanco = banco.GetComponentInChildren<PantallaBancoTaller>(true);
            persistente = montaje.GetComponent<MechaPersistente>();
            vidaMecha = montaje.GetComponent<VidaMecha>();
            Assert.IsNotNull(entrada, "El mostrador trae la entrada a la arena");
            Assert.IsNotNull(director, "El mostrador trae el flujo casero");
            Assert.IsNotNull(pantallaApuesta, "El mostrador trae la pantalla de apuesta");
            Assert.IsNotNull(pantallaCartelera, "El mostrador trae la cartelera");
            Assert.IsNotNull(pantallaBanco, "El mecha trae la pantalla del banco");
            Assert.IsNotNull(persistente, "El mecha es persistente");
            cartera = Registro<ICartera>.Actual;
            ligas = Registro<ServicioLigas>.Actual;
            Assert.IsNotNull(cartera, "Hay cartera");
            Assert.IsNotNull(ligas, "Hay ServicioLigas");
            precios = AssetDatabase.LoadAssetAtPath<DatosEconomia>(RutaEconomia);
            calendario = AssetDatabase.LoadAssetAtPath<CalendarioLigas>(RutaCalendario);
            configFuria = AssetDatabase.LoadAssetAtPath<ConfigFuriaJefe>(RutaFuria);
            Assert.IsNotNull(precios);
            Assert.IsNotNull(calendario);
            Assert.IsNotNull(configFuria);
            Assert.AreEqual(EtapaPrologo.Libre, Registro<IHistoria>.Actual.Etapa, "La escena arranca en Libre");
            EventosPelea.PeleaTerminada += terminadas.Add;
            ligas.CampeonVencido += campeones.Add;
            yield return null;
            Vigilar();
        }

        // ---- Entrada ----

        private IEnumerator IrAlMostrador()
        {
            Vector3 punto = mostrador.transform.position + new Vector3(0f, AlturaInicio, -DistanciaAlMostrador);
            jugador.Colocar(new Pose(punto, Quaternion.identity));
            atajos.Add("Colocar (mostrador de peleas): a 2 m del mostrador");
            yield return Cuadros(FramesParaElHud);
            Assert.LessOrEqual(DistanciaPlana(jugador.Raiz.position, mostrador.transform.position), DistanciaAlMostrador + 0.2f);
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

        private OpcionCartelera Opcion(TipoOpcionCartelera tipo, int liga, int indice)
        {
            foreach (OpcionCartelera opcion in mostrador.Opciones)
            {
                if (opcion.Tipo == tipo && opcion.Liga == liga && opcion.Indice == indice)
                {
                    return opcion;
                }
            }
            Assert.Fail("No está en la cartelera: " + tipo + " " + liga + "-" + indice + ". Hay: " + string.Join(", ", mostrador.Opciones));
            return default;
        }

        private int IndiceEnCartelera(OpcionCartelera opcion)
        {
            for (int i = 0; i < mostrador.Opciones.Count; i++)
            {
                if (mostrador.Opciones[i].Equals(opcion))
                {
                    return i;
                }
            }
            Assert.Fail("No está en la cartelera: " + opcion);
            return -1;
        }

        // E en el mostrador abre la cartelera y ↓ llega a la fila de la opción.
        private IEnumerator AbrirCarteleraYElegir(OpcionCartelera opcion)
        {
            yield return IrAlMostrador();
            Assert.IsTrue(mostrador.PuedeUsar, "El mostrador ofrece las peleas: " + mostrador.Texto);
            yield return Tecla(teclado.eKey);
            Assert.IsTrue(pantallaCartelera.Visible, "E abre la cartelera");
            Assert.AreEqual(mostrador.Opciones.Count, pantallaCartelera.CantidadFilas, "Una fila por opción");
            int objetivo = IndiceEnCartelera(opcion);
            int intentos = 0;
            while (pantallaCartelera.IndiceElegido != objetivo && intentos++ <= mostrador.Opciones.Count)
            {
                yield return Tecla(teclado.downArrowKey);
            }
            Assert.AreEqual(objetivo, pantallaCartelera.IndiceElegido, "↓ llega a la fila de " + opcion);
        }

        // Enter reta la fila elegida y la pantalla de apuesta elige el monto con → (0 = sin apuesta); Enter pelea.
        private IEnumerator RetarYElegirMonto(int monto)
        {
            yield return Tecla(teclado.enterKey);
            Assert.IsFalse(pantallaCartelera.Visible, "Enter cierra la cartelera");
            Assert.IsTrue(pantallaApuesta.Visible, "Los carteles traen su pantalla de apuesta");
            int intentos = 0;
            while (pantallaApuesta.Monto != monto && intentos++ < pantallaApuesta.CantidadOpciones)
            {
                yield return Tecla(teclado.rightArrowKey);
            }
            Assert.AreEqual(monto, pantallaApuesta.Monto, "→ elige $" + monto);
            int dineroAntes = cartera.Dinero;
            yield return Tecla(teclado.enterKey);
            Assert.IsFalse(pantallaApuesta.Visible, "Enter cierra la pantalla de apuesta");
            Assert.AreEqual(dineroAntes - monto, cartera.Dinero, "Se cobra lo apostado");
        }

        private IEnumerator EsperarPelea(string alias)
        {
            yield return EsperarHasta(() => jugador.Modo == ModoJugador.Mecha, MaxSegundosEstado, "el jugador va en el mecha");
            yield return EsperarHasta(() => director.Estado == EstadoFlujoPelea.Peleando, MaxSegundosEstado, "la pelea");
            Assert.IsNotNull(director.Rival, "Hay rival");
            Assert.AreEqual(alias, director.Rival.Nombre, "El rival es el del cartel");
        }

        // Daño real al jugador (clics al aire: el rival golpea de verdad) hasta el 90 % de su vida.
        private IEnumerator RecibirGolpesReales()
        {
            float limite = vidaMecha.Maxima * FraccionDanioBuscada;
            yield return ClicsHasta(() => vidaMecha.Actual <= limite || vidaMecha.EstaKO);
            Assert.Less(vidaMecha.Actual, vidaMecha.Maxima, "El jugador recibió golpes reales");
            Assert.IsFalse(vidaMecha.EstaKO, "Y no perdió");
        }

        private void RematarAlRival()
        {
            ((VidaMecha)director.Rival.Vida).RecibirDanio(new InfoDanio(DanioMortal, TipoDanio.Golpe, Vector3.zero, Vector3.forward, null));
            atajos.Add("RecibirDanio: rival rematado tras daño real al mecha");
        }

        private IEnumerator EsperarVueltaAPie()
        {
            yield return EsperarHasta(() => director.Estado == EstadoFlujoPelea.Resultado, MaxSegundosEstado, "el resultado");
            yield return EsperarHasta(() => !entrada.EnCurso, MaxSegundosSalida, "termina la salida");
            yield return null;
            Vigilar();
            Assert.AreEqual(ModoJugador.APie, jugador.Modo, "Volvió a pie");
        }

        // Retar la opción desde la cartelera, pelear (golpes reales) y rematar al rival.
        private IEnumerator RetarYGanar(OpcionCartelera opcion, string alias, int monto)
        {
            yield return AbrirCarteleraYElegir(opcion);
            yield return RetarYElegirMonto(monto);
            yield return EsperarPelea(alias);
            yield return RecibirGolpesReales();
            RematarAlRival();
            yield return EsperarVueltaAPie();
            Assert.AreEqual(LadoPelea.Jugador, terminadas[terminadas.Count - 1].Ganador, "Ganó el jugador contra " + alias);
        }

        // Banco: REPARAR TODO si hay daño (cobra lo de la regla).
        private IEnumerator RepararTodo()
        {
            float danio = persistente.Danio;
            if (danio <= 0f)
            {
                yield break;
            }
            int costo = ReglaReparacion.Costo(danio, precios.CostoReparacionPorPunto, precios.CostoReparacionMinimo);
            int antes = cartera.Dinero;
            yield return IrAlBanco();
            yield return Tecla(teclado.eKey);
            Assert.IsTrue(pantallaBanco.Visible, "E abre la pantalla del banco");
            StringAssert.StartsWith("REPARAR TODO", pantallaBanco.TextoBaseOpcion(0));
            yield return Tecla(teclado.enterKey);
            Assert.AreEqual(antes - costo, cartera.Dinero, "Reparar cobró exactamente ReglaReparacion ($" + costo + " por "
                + danio.ToString("F1", CultureInfo.InvariantCulture) + " puntos)");
            Assert.AreEqual((-costo), cartera.Dinero - antes);
            dineroEsperado -= costo;
            Assert.AreEqual(vidaMecha.Maxima, vidaMecha.Actual, MargenVida, "La vida quedó completa");
            Assert.AreEqual(0f, persistente.Danio, MargenVida);
            if (pantallaBanco.Visible)
            {
                yield return Tecla(teclado.escapeKey);
            }
            yield return Cuadros(FramesParaElHud);
        }

        // ---- Capturas (solo con CAPTURAR_T162=1) ----

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

        // a) Inicio: liga 1, siguiente 1, 0 campeones, dinero fijado por código.
        private void Inicio()
        {
            Assert.AreEqual(precios.DineroInicial, cartera.Dinero, "a) empieza con el dinero inicial del asset");
            cartera.Abonar(DineroDePrueba - cartera.Dinero, MotivoDinero.Otro);
            atajos.Add("Abonar: dinero inicial fijado en $" + DineroDePrueba + " por código");
            dineroEsperado = DineroDePrueba;
            Assert.AreEqual(dineroEsperado, cartera.Dinero);
            Assert.AreEqual(Sotano, ligas.LigaActual, "a) liga 1");
            Assert.AreEqual(1, ligas.Siguiente, "a) siguiente 1");
            Assert.AreEqual(0, ligas.CampeonesVencidos, "a) 0 campeones");
            Assert.AreEqual(0f, persistente.Danio, MargenVida, "a) mecha sin daño");
        }

        // b) TORNILLO con el monto mayor de la apuesta de su cartel.
        private IEnumerator ApostarYGanarElPrimero()
        {
            CartelPelea cartel = calendario.Cartel(Sotano, 1);
            DatosApuestaPelea reglas = cartel.apuesta;
            Assert.IsNotNull(reglas, "El cartel trae sus reglas de apuesta");
            int apuesta = reglas.Montos.Where(m => m <= reglas.ApuestaMaxima).Max();
            int ganancia = (int)Math.Round(apuesta * (double)reglas.Momio, MidpointRounding.AwayFromZero);
            OpcionCartelera opcion = Opcion(TipoOpcionCartelera.Siguiente, Sotano, 1);
            Assert.AreEqual(AliasPrimero, mostrador.AliasDe(opcion), "b) el siguiente es el primer rival");
            yield return RetarYGanar(opcion, AliasPrimero, apuesta);
            dineroEsperado += cartel.premioGanar + ganancia;
            Assert.AreEqual(dineroEsperado, cartera.Dinero, "b) dinero = antes + premio + apuesta x momio ($" + cartel.premioGanar + " + $"
                + apuesta + " x " + reglas.Momio.ToString("F1", CultureInfo.InvariantCulture) + ")");
            Assert.AreEqual(Sotano, ligas.LigaActual);
            Assert.AreEqual(2, ligas.Siguiente, "b) el siguiente pasa a 2");
        }

        // d) LA RATA: se pierde sin apostar, el progreso no cambia; se repara y se gana.
        private IEnumerator PerderYReintentarElSegundo()
        {
            CartelPelea cartel = calendario.Cartel(Sotano, 2);
            OpcionCartelera opcion = Opcion(TipoOpcionCartelera.Siguiente, Sotano, 2);
            Assert.AreEqual(AliasSegundo, mostrador.AliasDe(opcion));
            vidaMecha.FijarVida(vidaMecha.Maxima * FraccionVidaFregada);
            atajos.Add("FijarVida: el mecha sale de reparar y entra a la pelea que se pierde con el 20 % de vida");
            int resultadosAntes = terminadas.Count;
            yield return AbrirCarteleraYElegir(opcion);
            yield return RetarYElegirMonto(0);
            yield return EsperarPelea(AliasSegundo);
            yield return ClicsHasta(() => vidaMecha.EstaKO);
            Assert.IsTrue(vidaMecha.EstaKO, "d) el rival lo dejó en KO con golpes reales");
            yield return EsperarVueltaAPie();
            Assert.AreEqual(resultadosAntes + 1, terminadas.Count, "d) la pelea terminó una vez");
            Assert.AreEqual(LadoPelea.Rival, terminadas[terminadas.Count - 1].Ganador, "d) ganó el rival");
            dineroEsperado += cartel.premioPerder;
            Assert.AreEqual(dineroEsperado, cartera.Dinero, "d) perder sin apostar solo paga el premio de perder del cartel");
            Assert.AreEqual(Sotano, ligas.LigaActual);
            Assert.AreEqual(2, ligas.Siguiente, "d) perder no cambia el progreso");

            yield return RepararTodo();
            yield return RetarYGanar(Opcion(TipoOpcionCartelera.Siguiente, Sotano, 2), AliasSegundo, 0);
            dineroEsperado += cartel.premioGanar;
            Assert.AreEqual(dineroEsperado, cartera.Dinero, "d) el reintento paga el premio del cartel");
            Assert.AreEqual(3, ligas.Siguiente, "d) el siguiente pasa a 3");
        }

        // e) Revancha de TORNILLO: la fracción del premio y el progreso sigue en 3.
        private IEnumerator TomarLaRevancha()
        {
            CartelPelea cartel = calendario.Cartel(Sotano, 1);
            OpcionCartelera opcion = Opcion(TipoOpcionCartelera.Revancha, Sotano, 1);
            int esperado = Mathf.RoundToInt(cartel.premioGanar * ligas.Datos.FraccionPremioRevancha);
            Assert.AreEqual(esperado, opcion.Premio, "e) la cartelera muestra la fracción del premio");
            Assert.Less(esperado, cartel.premioGanar, "e) la revancha paga menos que la pelea");
            Assert.AreEqual(cartel.premioGanar / 2, esperado, "e) la fracción de la revancha es la mitad");
            yield return RetarYGanar(opcion, AliasPrimero, 0);
            dineroEsperado += esperado;
            Assert.AreEqual(dineroEsperado, cartera.Dinero, "e) la revancha paga la mitad del premio ($" + esperado + ")");
            Assert.AreEqual(Sotano, ligas.LigaActual);
            Assert.AreEqual(3, ligas.Siguiente, "e) el progreso no cambia");
        }

        // f) El jefe de la liga Sótano: letrero, furia, victoria, aviso de liga.
        private IEnumerator VencerAlJefe()
        {
            CartelPelea cartel = calendario.Cartel(Sotano, RivalesPorLiga);
            Assert.IsTrue(ligas.Poner(Sotano, RivalesPorLiga, 0));
            atajos.Add("Poner: liga 1, siguiente 5, 0 campeones");
            OpcionCartelera opcion = Opcion(TipoOpcionCartelera.Siguiente, Sotano, RivalesPorLiga);
            Assert.IsTrue(opcion.EsJefe, "f) el quinto es el jefe");
            Assert.AreEqual(AliasJefe, mostrador.AliasDe(opcion));
            yield return AbrirCarteleraYElegir(opcion);
            int fila = pantallaCartelera.IndiceElegido;
            StringAssert.Contains(AliasJefe, pantallaCartelera.TextoBaseFila(fila), "f) la cartelera muestra al jefe");
            Assert.AreEqual(pantallaCartelera.ColorJefe, pantallaCartelera.ColorFila(fila), "f) la fila del jefe lleva el color de jefe");
            yield return Cuadros(FramesParaElHud);
            Capturar(RutaCaptura1);
            letreroVisto = false;
            vencisteVisto = false;
            yield return RetarYElegirMonto(0);
            yield return EsperarPelea(AliasJefe);
            Assert.IsNotNull(director.Pantalla, "Hay pantalla de la arena");

            VidaMecha vidaRival = (VidaMecha)director.Rival.Vida;
            FuriaJefe furia = vidaRival.GetComponent<FuriaJefe>();
            Assert.IsNotNull(furia, "f) el jefe trae la furia");
            bool furiaAvisada = false;
            furia.FuriaIniciada += () => furiaAvisada = true;
            Assert.IsFalse(furia.EnFuria, "f) empieza sin furia");
            yield return RecibirGolpesReales();

            // Se baja al jefe por debajo del umbral de la furia (atajo) y la furia empieza sola.
            float objetivo = vidaRival.Maxima * (configFuria.UmbralVida - MargenFuria);
            vidaRival.RecibirDanio(new InfoDanio(vidaRival.Actual - objetivo, TipoDanio.Golpe, Vector3.zero, Vector3.forward, null));
            atajos.Add("RecibirDanio: el jefe baja de la mitad tras daño real al mecha");
            yield return EsperarHasta(() => furiaAvisada, MaxSegundosEstado, "FuriaIniciada al bajar de la mitad");
            Assert.IsTrue(furia.EnFuria, "f) el jefe está en furia");
            Assert.IsFalse(vidaMecha.EstaKO, "f) el jugador sigue en pie");
            yield return Cuadros(FramesParaElHud);
            Capturar(RutaCaptura2);

            RematarAlRival();
            yield return EsperarVueltaAPie();
            Assert.IsTrue(letreroVisto, "f) la arena dijo \"" + TextoLetreroJefe + "\"");
            Assert.IsTrue(vencisteVisto, "f) la arena dijo \"" + TextoVencisteCampeon + "\"");
            Assert.AreEqual(LadoPelea.Jugador, terminadas[terminadas.Count - 1].Ganador, "f) ganó el jugador");
            dineroEsperado += cartel.premioGanar;
            Assert.AreEqual(dineroEsperado, cartera.Dinero, "f) el jefe paga el premio de su cartel");
            Assert.IsTrue(hud.GrupoAvisoLiga.activeSelf, "f) al bajar a pie el HUD avisa");
            Assert.AreEqual(TextoCampeonSotano, hud.TextoAvisoLiga.text);
            Assert.AreEqual(TextoSeAbreCalle, hud.TextoAvisoLiga2.text);
            yield return Cuadros(FramesParaElHud);
            Capturar(RutaCaptura3);
            Assert.AreEqual(Calle, ligas.LigaActual, "f) sube a la liga 2");
            Assert.AreEqual(1, ligas.Siguiente);
            Assert.AreEqual(1, ligas.CampeonesVencidos);
            CollectionAssert.AreEqual(new[] { Sotano }, campeones, "f) CampeonVencido(1) una sola vez");
        }

        // g) F5 guarda, se cambia todo por código, F9 carga: no vuelve el aviso.
        private IEnumerator GuardarYCargar()
        {
            yield return EsperarHasta(() => !hud.GrupoAvisoLiga.activeSelf, MaxSegundosAviso, "se va el aviso de liga");
            int dineroGuardado = cartera.Dinero;
            Assert.AreEqual(dineroEsperado, dineroGuardado);
            yield return Tecla(teclado.f5Key);
            Assert.IsTrue(ServicioGuardado.Actual.HayPartida, "g) F5 escribió la partida en la ruta temporal");

            ligas.Reiniciar();
            cartera.Abonar(777, MotivoDinero.Otro);
            Assert.AreEqual(1, ligas.LigaActual, "g) las ligas se reiniciaron");
            Assert.AreNotEqual(dineroGuardado, cartera.Dinero);
            atajos.Add("Reiniciar + Abonar: se cambian las ligas y el dinero antes de cargar");

            yield return Tecla(teclado.f9Key);
            Assert.AreEqual(Calle, ligas.LigaActual, "g) F9 devuelve la liga");
            Assert.AreEqual(1, ligas.Siguiente, "g) F9 devuelve el siguiente");
            Assert.AreEqual(1, ligas.CampeonesVencidos, "g) F9 devuelve los campeones");
            Assert.AreEqual(dineroGuardado, cartera.Dinero, "g) F9 devuelve el dinero");
            yield return Cuadros(FramesParaElHud * 2);
            Assert.IsFalse(hud.GrupoAvisoLiga.activeSelf, "g) cargar no vuelve a mostrar el aviso de liga");
            CollectionAssert.AreEqual(new[] { Sotano }, campeones, "g) cargar no avisa campeones");
        }

        // h) El campeón de la última liga.
        private IEnumerator VencerAlCampeon()
        {
            CartelPelea cartel = calendario.Cartel(UltimaLiga, RivalesPorLiga);
            Assert.IsTrue(ligas.Poner(UltimaLiga, RivalesPorLiga, UltimaLiga - 1));
            atajos.Add("Poner: liga 4, siguiente 5, 3 campeones");
            yield return RepararTodo();
            OpcionCartelera opcion = Opcion(TipoOpcionCartelera.Siguiente, UltimaLiga, RivalesPorLiga);
            Assert.IsTrue(opcion.EsJefe);
            Assert.AreEqual(AliasCampeon, mostrador.AliasDe(opcion));
            yield return RetarYGanar(opcion, AliasCampeon, 0);
            dineroEsperado += cartel.premioGanar;
            Assert.AreEqual(dineroEsperado, cartera.Dinero, "h) el campeón paga el premio de su cartel");
            Assert.AreEqual(UltimaLiga, ligas.CampeonesVencidos, "h) 4 campeones");
            Assert.AreEqual(UltimaLiga, ligas.LigaActual);
            Assert.IsTrue(hud.GrupoAvisoLiga.activeSelf, "h) el HUD avisa");
            Assert.AreEqual(TextoCampeonClandestino, hud.TextoAvisoLiga.text);
            Assert.IsFalse(hud.TextoAvisoLiga2.gameObject.activeSelf, "h) sin segunda línea");
            CollectionAssert.AreEqual(new[] { Sotano, UltimaLiga }, campeones, "h) CampeonVencido(4) una vez");

            int revanchas = mostrador.Opciones.Count(o => o.Tipo == TipoOpcionCartelera.Revancha && o.Liga == UltimaLiga);
            Assert.AreEqual(RivalesPorLiga, revanchas, "h) la cartelera ofrece las 5 revanchas de la liga 4");
            Assert.AreEqual(0, mostrador.Opciones.Count(o => o.Tipo == TipoOpcionCartelera.Siguiente), "h) ya no hay siguiente");
            Assert.AreEqual(0, mostrador.Opciones.Count(o => o.Tipo == TipoOpcionCartelera.Bloqueada), "h) ya no hay liga bloqueada");
        }

        // ---- Prueba ----

        [UnityTest]
        [Timeout(1800000)]
        public IEnumerator Recorrido_RetarApostarGanarPerderRevanchaJefeSubirGuardarYCampeon_JugandoComoUnaPersona()
        {
            yield return CargarEscena();
            float inicioJuego = Time.time;

            float t = Time.time;
            Inicio();
            AnotarTramo("a) inicio", t);

            t = Time.time;
            yield return ApostarYGanarElPrimero();
            AnotarTramo("b) retar a TORNILLO, apostar y ganar", t);

            t = Time.time;
            yield return RepararTodo();
            AnotarTramo("c) reparar todo", t);

            t = Time.time;
            yield return PerderYReintentarElSegundo();
            AnotarTramo("d) perder con LA RATA, reparar y ganar", t);

            t = Time.time;
            yield return RepararTodo();
            yield return TomarLaRevancha();
            AnotarTramo("e) revancha de TORNILLO", t);

            t = Time.time;
            yield return RepararTodo();
            yield return VencerAlJefe();
            AnotarTramo("f) DON FIERRO en furia y subir a la liga Calle", t);

            t = Time.time;
            yield return GuardarYCargar();
            AnotarTramo("g) guardar y cargar", t);

            t = Time.time;
            yield return VencerAlCampeon();
            AnotarTramo("h) EL CACIQUE y campeón del clandestino", t);

            // i) Consola, tiempos, frames y la partida real intacta.
            float segundos = Time.time - inicioJuego;
            Debug.Log("[Prueba T-162] Dinero final $" + cartera.Dinero + " (calculado desde los assets $" + dineroEsperado + "), progreso final: liga "
                + ligas.LigaActual + ", siguiente " + ligas.Siguiente + ", campeones " + ligas.CampeonesVencidos + ", tiempo de juego "
                + segundos.ToString("F1", CultureInfo.InvariantCulture) + " s");
            Debug.Log("[Prueba T-162] Tramos: " + string.Join(" | ", tramos));
            Debug.Log("[Prueba T-162] Atajos: " + string.Join(" | ", atajos));
            Debug.Log("[Prueba T-162] MedidorFrames: " + medidor.Resumen());
            Assert.AreEqual(dineroEsperado, cartera.Dinero, "i) el dinero final es el calculado desde los assets");
            Assert.AreEqual(UltimaLiga, ligas.LigaActual, "i) liga final");
            Assert.AreEqual(0, ligas.Siguiente, "i) campeón de la última liga: ya no hay siguiente");
            Assert.AreEqual(UltimaLiga, ligas.CampeonesVencidos, "i) 4 campeones");
            Assert.Less(segundos, MaxSegundosRecorrido, "El recorrido dura menos de " + MaxSegundosRecorrido + " s de juego");
            Assert.IsTrue(medidor.Cumple, "Sin tirones de frames: " + medidor.Resumen());
            Assert.AreEqual(huellaReal, HuellaDe(rutaReal), "La partida real (persistentDataPath) no se tocó");
            Assert.IsEmpty(errores, "Consola sin errores: " + string.Join(" | ", errores));
        }
    }
}
