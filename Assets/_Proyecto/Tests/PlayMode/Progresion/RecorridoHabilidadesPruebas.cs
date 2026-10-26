using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using MechaCyberpunk.Combate.Habilidades;
using MechaCyberpunk.Combate.Vida;
using MechaCyberpunk.Core;
using MechaCyberpunk.Economia;
using MechaCyberpunk.IA.Rival;
using MechaCyberpunk.Mecha;
using MechaCyberpunk.Mecha.Encendido;
using MechaCyberpunk.Mecha.Estado;
using MechaCyberpunk.Mecha.Llamado;
using MechaCyberpunk.Mecha.Montaje;
using MechaCyberpunk.Mecha.Taller;
using MechaCyberpunk.Pelea.Datos;
using MechaCyberpunk.Pelea.Flujo;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Progresion;
using MechaCyberpunk.Progresion.Guardado;
using MechaCyberpunk.Progresion.Prologo;
using MechaCyberpunk.Progresion.Pruebas;
using MechaCyberpunk.UI;
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
    /// T-180: el camino de las habilidades (cierre del bloque 4 de T-F7) en Pruebas_Economia, de noche y en Libre, con entrada simulada:
    /// comprar munición en el mercado negro, comprar BRAZO IZQ. MILITAR I y TORSO TECH I en el taller, verlas en la cabina, usar el
    /// misil con su enfriamiento hasta quedarse sin munición y pelear a puños, cubrirse con el escudo, esquivar la embestida de DON
    /// FIERRO y guardar y cargar la munición. Los números salen de los assets. Atajos permitidos y anotados: Colocar cerca del banco,
    /// del mostrador, del mercado o del mecha; dinero por código; RecibirDanio al rival solo después de daño real al jugador; Poner en
    /// las ligas solo donde se indica; la munición y el dinero por código solo antes de cargar.
    /// </summary>
    public class RecorridoHabilidadesPruebas : InputTestFixture
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Tony/Pruebas_Economia.unity";
        private const string RutaEconomia = "Assets/_Proyecto/Economia/Datos/DatosEconomia_Base.asset";
        private const string RutaCalendario = "Assets/_Proyecto/Pelea/Datos/CalendarioLigas_Base.asset";
        private const string RutaDatosJugador = "Assets/_Proyecto/Mecha/Datos/DatosMecha_Jugador.asset";
        private const string RutaConfigPantalla = "Assets/_Proyecto/UI/Datos/ConfigPantallaHabilidades_Base.asset";
        private const string RutaMunicionMisil = "Assets/_Proyecto/Economia/Datos/DatosObjeto_MunicionMisil.asset";
        private const string RutaMunicionCeldas = "Assets/_Proyecto/Economia/Datos/DatosObjeto_MunicionCeldas.asset";
        private const string AvisoPiezas = "E · Taller · piezas";
        private const string AvisoReparar = "E · Reparar mecha";
        private const string AliasPrimero = "TORNILLO";
        private const string AliasSegundo = "LA RATA";
        private const string AliasJefe = "DON FIERRO";
        private const string TituloPiezas = "PIEZAS DEL MECHA";
        private const string TextoComprar = " · COMPRAR · ";
        private const string IdBrazoIzqMilitar1 = "pieza_brazoizq_militar_1";
        private const string IdTorsoTech1 = "pieza_torso_tech_1";
        private const string DiferenciaBrazo = "HABILIDAD 1 · MISIL I · MISILES";
        private const string DiferenciaTorso = "HABILIDAD 2 · ESCUDO I · CELDAS";
        private const string FilaBrazoListo = "1 · MISIL I · ×";
        private const string FilaTorsoListo = "2 · ESCUDO I · ×";
        private const string VariableCaptura = "CAPTURAR_T180";
        private const string RutaCaptura1 = "docs/capturas/T-180-1.png";
        private const string RutaCaptura2 = "docs/capturas/T-180-2.png";
        private const string RutaCaptura3 = "docs/capturas/T-180-3.png";
        private const int DineroDePrueba = 3000;
        private const int Sotano = 1;
        private const int Calle = 2;
        private const int RivalesPorLiga = 5;
        private const int NumeroRanuras = 5;
        private const int MisilesAComprar = 3;
        private const int CeldasAComprar = 2;
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
        private const float MaxSegundosGolpes = 120f;
        private const float MaxSegundosSalida = 30f;
        private const float MaxSegundosCaminando = 10f;
        private const float MaxSegundosEncendido = 20f;
        private const float MaxSegundosRegreso = 30f;
        private const float MaxSegundosHabilidad = 25f;
        private const float MaxSegundosJefe = 60f;
        private const float MaxSegundosRecorrido = 1700f;
        private const float SegundosEntreClics = 0.25f;
        private const float RetrasoTecla = 0.1f;
        private const float SegundosAsentar = 0.5f;
        private const float DanioMortal = 100000f;
        private const float FraccionDanioBuscada = 0.9f;
        private const float MargenVida = 0.6f;
        private const float Tolerancia = 1e-3f;
        private const float ToleranciaEnfriamiento = 0.35f;
        private const float DistanciaParaElMisil = 3f;
        private const float SegundosMisilEnVuelo = 0.04f;
        private const float DistanciaAlMostrador = 2f;
        private const float DistanciaAlBanco = 2f;
        private const float DistanciaAlMercado = 2f;
        private const float DistanciaAlMecha = 5f;
        private const float DistanciaParaSubir = 2f;
        private const float AlturaInicio = 0.05f;

        private struct Impacto
        {
            public float Cantidad;
            public float Aplicado;
            public TipoBloqueo Bloqueo;
            public TipoDanio Tipo;
            public float Tiempo;
        }

        private Keyboard teclado;
        private Mouse raton;
        private string carpeta;
        private string rutaReal;
        private string huellaReal;
        private JugadorAPie jugador;
        private MostradorPeleas mostrador;
        private MostradorTienda mercado;
        private EntradaArena entrada;
        private DirectorPelea director;
        private PantallaApuesta pantallaApuesta;
        private PantallaCartelera pantallaCartelera;
        private PantallaTienda pantallaTienda;
        private BancoTaller banco;
        private PantallaBancoTaller pantallaBanco;
        private PantallaPiezasTaller pantallaPiezas;
        private PantallaPiezasCabina pantallaCabina;
        private PantallaHabilidadesCabina pantallaHabilidades;
        private EquipoMecha equipo;
        private MontajeMecha montaje;
        private EncendidoMecha encendido;
        private LlamadoMecha llamado;
        private MechaPersistente persistente;
        private VidaMecha vidaMecha;
        private UsoHabilidades uso;
        private LanzadorHabilidades lanzador;
        private HudAPie hud;
        private ICartera cartera;
        private ServicioInventario inventario;
        private ServicioLigas ligas;
        private DatosEconomia precios;
        private CalendarioLigas calendario;
        private DatosMecha datosJugador;
        private ConfigPantallaHabilidades configPantalla;
        private DatosObjeto misiles;
        private DatosObjeto celdas;
        private DatosPieza brazoIzqSerie;
        private DatosPieza torsoSerie;
        private DatosPieza brazoIzqMilitar1;
        private DatosPieza torsoTech1;
        private int dineroEsperado;
        private int framesSinMedir;
        private readonly MedidorFrames medidor = new MedidorFrames();
        private readonly List<string> errores = new List<string>();
        private readonly List<string> atajos = new List<string>();
        private readonly List<string> tramos = new List<string>();
        private readonly List<ResultadoPelea> terminadas = new List<ResultadoPelea>();
        private readonly List<Impacto> golpesAlRival = new List<Impacto>();
        private readonly List<Impacto> golpesAlJugador = new List<Impacto>();
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
            carpeta = Path.Combine(Application.temporaryCachePath, "recorrido_habilidades_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(carpeta);
            ServicioGuardado.RutaForzada = Path.Combine(carpeta, "partida.json");
            rutaReal = Path.Combine(Application.persistentDataPath, "partida.json");
            huellaReal = HuellaDe(rutaReal);
            errores.Clear();
            atajos.Clear();
            tramos.Clear();
            terminadas.Clear();
            golpesAlRival.Clear();
            golpesAlJugador.Clear();
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

        private void RegistrarImpacto(IPeleador objetivo, InfoDanio info, ResultadoDanio resultado)
        {
            if (objetivo == null)
            {
                return;
            }
            Impacto impacto = new Impacto
            {
                Cantidad = info.Cantidad, Aplicado = resultado.DanioAplicado, Bloqueo = resultado.Bloqueo, Tipo = info.Tipo, Tiempo = Time.time
            };
            if (objetivo.Lado == LadoPelea.Rival && info.Atacante != null && info.Atacante.Lado == LadoPelea.Jugador)
            {
                golpesAlRival.Add(impacto);
            }
            else if (objetivo.Lado == LadoPelea.Jugador && info.Atacante != null && info.Atacante.Lado == LadoPelea.Rival)
            {
                golpesAlJugador.Add(impacto);
            }
        }

        private static string Num(float valor, string formato = "F2")
        {
            return valor.ToString(formato, CultureInfo.InvariantCulture);
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

        private IEnumerator Esperar(float segundos)
        {
            float fin = Time.time + segundos;
            while (Time.time < fin)
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

        private IEnumerator Repetir(KeyControl tecla, int veces)
        {
            for (int i = 0; i < veces; i++)
            {
                yield return Tecla(tecla);
            }
        }

        private void AnotarTramo(string nombre, float inicio)
        {
            tramos.Add(nombre + ": " + Num(Time.time - inicio, "F1") + " s");
        }

        private static float DistanciaPlana(Vector3 a, Vector3 b)
        {
            return Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));
        }

        // ---- Preparación ----

        private DatosPieza Pieza(string id)
        {
            DatosPieza p = equipo.Catalogo.Buscar(id);
            Assert.IsNotNull(p, "Existe la pieza " + id);
            return p;
        }

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
            mercado = Object.FindFirstObjectByType<MostradorTienda>();
            banco = Object.FindFirstObjectByType<BancoTaller>();
            hud = Object.FindFirstObjectByType<HudAPie>();
            montaje = Object.FindFirstObjectByType<MontajeMecha>();
            Assert.IsNotNull(jugador, "Pruebas_Economia trae al jugador");
            Assert.IsNotNull(mostrador, "Trae el mostrador de peleas");
            Assert.IsNotNull(mercado, "Trae el mercado negro");
            Assert.IsNotNull(banco, "El mecha trae el banco del taller");
            Assert.IsNotNull(hud, "El jugador trae el HUD a pie");
            Assert.IsNotNull(montaje, "Trae el mecha de la historia");
            entrada = mostrador.GetComponent<EntradaArena>();
            director = mostrador.GetComponentInChildren<DirectorPelea>(true);
            pantallaApuesta = mostrador.GetComponentInChildren<PantallaApuesta>(true);
            pantallaCartelera = mostrador.GetComponentInChildren<PantallaCartelera>(true);
            pantallaTienda = mercado.GetComponentInChildren<PantallaTienda>(true);
            pantallaBanco = banco.GetComponentInChildren<PantallaBancoTaller>(true);
            pantallaPiezas = banco.GetComponentInChildren<PantallaPiezasTaller>(true);
            pantallaCabina = montaje.GetComponentInChildren<PantallaPiezasCabina>(true);
            pantallaHabilidades = montaje.GetComponentInChildren<PantallaHabilidadesCabina>(true);
            equipo = montaje.GetComponent<EquipoMecha>();
            encendido = montaje.GetComponent<EncendidoMecha>();
            llamado = montaje.GetComponent<LlamadoMecha>();
            persistente = montaje.GetComponent<MechaPersistente>();
            vidaMecha = montaje.GetComponent<VidaMecha>();
            uso = montaje.GetComponent<UsoHabilidades>();
            lanzador = montaje.GetComponent<LanzadorHabilidades>();
            Assert.IsNotNull(entrada);
            Assert.IsNotNull(director);
            Assert.IsNotNull(pantallaApuesta);
            Assert.IsNotNull(pantallaCartelera);
            Assert.IsNotNull(pantallaTienda, "El mercado trae su pantalla");
            Assert.IsNotNull(pantallaBanco);
            Assert.IsNotNull(pantallaPiezas, "El banco trae PIEZAS DEL MECHA");
            Assert.IsNotNull(pantallaCabina, "El mecha trae la pantalla de piezas de la cabina");
            Assert.IsNotNull(pantallaHabilidades, "El mecha trae la pantalla de habilidades de la cabina");
            Assert.IsNotNull(equipo);
            Assert.IsNotNull(encendido);
            Assert.IsNotNull(llamado);
            Assert.IsNotNull(persistente);
            Assert.IsNotNull(uso, "El mecha trae UsoHabilidades");
            Assert.IsNotNull(lanzador, "El mecha trae LanzadorHabilidades");
            cartera = Registro<ICartera>.Actual;
            inventario = Registro<ServicioInventario>.Actual;
            ligas = Registro<ServicioLigas>.Actual;
            Assert.IsNotNull(cartera);
            Assert.IsNotNull(inventario);
            Assert.IsNotNull(ligas);
            precios = AssetDatabase.LoadAssetAtPath<DatosEconomia>(RutaEconomia);
            calendario = AssetDatabase.LoadAssetAtPath<CalendarioLigas>(RutaCalendario);
            datosJugador = AssetDatabase.LoadAssetAtPath<DatosMecha>(RutaDatosJugador);
            configPantalla = AssetDatabase.LoadAssetAtPath<ConfigPantallaHabilidades>(RutaConfigPantalla);
            misiles = AssetDatabase.LoadAssetAtPath<DatosObjeto>(RutaMunicionMisil);
            celdas = AssetDatabase.LoadAssetAtPath<DatosObjeto>(RutaMunicionCeldas);
            Assert.IsNotNull(precios);
            Assert.IsNotNull(calendario);
            Assert.IsNotNull(datosJugador);
            Assert.IsNotNull(configPantalla);
            Assert.IsNotNull(misiles);
            Assert.IsNotNull(celdas);
            Assert.AreEqual(EtapaPrologo.Libre, Registro<IHistoria>.Actual.Etapa, "La escena arranca en Libre");
            Assert.IsTrue(Registro<IRelojMundo>.Actual.EsDeNoche, "La escena arranca de noche");
            brazoIzqSerie = equipo.Catalogo.DeSerie(RanuraPieza.BrazoIzquierdo);
            torsoSerie = equipo.Catalogo.DeSerie(RanuraPieza.Torso);
            brazoIzqMilitar1 = Pieza(IdBrazoIzqMilitar1);
            torsoTech1 = Pieza(IdTorsoTech1);
            EventosPelea.PeleaTerminada += terminadas.Add;
            EventosPelea.ImpactoRecibido += RegistrarImpacto;
            yield return null;
            Vigilar();
        }

        // ---- Entrada: caminar y colocar ----

        private IEnumerator IrAlMostrador()
        {
            Vector3 punto = mostrador.transform.position + new Vector3(0f, AlturaInicio, -DistanciaAlMostrador);
            jugador.Colocar(new Pose(punto, Quaternion.identity));
            atajos.Add("Colocar (mostrador de peleas): a 2 m del mostrador");
            yield return Cuadros(FramesParaElHud);
            Assert.LessOrEqual(DistanciaPlana(jugador.Raiz.position, mostrador.transform.position), DistanciaAlMostrador + 0.2f);
        }

        private IEnumerator IrAlMercado()
        {
            Vector3 punto = mercado.transform.position + new Vector3(0f, AlturaInicio, -DistanciaAlMercado);
            jugador.Colocar(new Pose(punto, Quaternion.identity));
            atajos.Add("Colocar (mercado negro): a 2 m del mostrador");
            yield return Cuadros(FramesParaElHud);
            Assert.IsTrue(mercado.PuedeUsar, "El mercado se ofrece: " + mercado.Texto);
        }

        private IEnumerator IrAlBanco(string aviso)
        {
            Vector3 punto = Registro<ITallerMecha>.Actual.PuntoInteraccion;
            jugador.Colocar(new Pose(new Vector3(punto.x, AlturaInicio, punto.z + DistanciaAlBanco), Quaternion.Euler(0f, 180f, 0f)));
            atajos.Add("Colocar (banco): a 2 m del banco");
            float fin = Time.time + MaxSegundosCaminando;
            while (Time.time < fin && !hud.TextoAvisoInteraccion.text.StartsWith(aviso, StringComparison.Ordinal))
            {
                yield return null;
                Vigilar();
            }
            Assert.IsTrue(hud.TextoAvisoInteraccion.text.StartsWith(aviso, StringComparison.Ordinal),
                "El HUD ofrece '" + aviso + "'; dice: " + hud.TextoAvisoInteraccion.text);
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

        // ---- Entrada: la cartelera y la pelea ----

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

        private IEnumerator AbrirCarteleraYElegir(OpcionCartelera opcion)
        {
            yield return IrAlMostrador();
            Assert.IsTrue(mostrador.PuedeUsar, "El mostrador ofrece las peleas: " + mostrador.Texto);
            yield return Tecla(teclado.eKey);
            Assert.IsTrue(pantallaCartelera.Visible, "E abre la cartelera");
            int objetivo = -1;
            for (int i = 0; i < mostrador.Opciones.Count; i++)
            {
                if (mostrador.Opciones[i].Equals(opcion)) objetivo = i;
            }
            Assert.GreaterOrEqual(objetivo, 0, "La opción está en la cartelera");
            int intentos = 0;
            while (pantallaCartelera.IndiceElegido != objetivo && intentos++ <= mostrador.Opciones.Count)
            {
                yield return Tecla(teclado.downArrowKey);
            }
            Assert.AreEqual(objetivo, pantallaCartelera.IndiceElegido, "↓ llega a la fila de " + opcion);
        }

        private IEnumerator RetarSinApostar()
        {
            yield return Tecla(teclado.enterKey);
            Assert.IsFalse(pantallaCartelera.Visible, "Enter cierra la cartelera");
            Assert.IsTrue(pantallaApuesta.Visible, "Los carteles traen su pantalla de apuesta");
            Assert.AreEqual(0, pantallaApuesta.Monto, "La apuesta empieza en $0");
            int dineroAntes = cartera.Dinero;
            yield return Tecla(teclado.enterKey);
            Assert.IsFalse(pantallaApuesta.Visible, "Enter cierra la pantalla de apuesta");
            Assert.AreEqual(dineroAntes, cartera.Dinero, "Sin apuesta no se cobra");
        }

        private IEnumerator EsperarPelea(string alias)
        {
            yield return EsperarHasta(() => jugador.Modo == ModoJugador.Mecha, MaxSegundosEstado, "el jugador va en el mecha");
            yield return EsperarHasta(() => director.Estado == EstadoFlujoPelea.Peleando, MaxSegundosEstado, "la pelea");
            Assert.IsNotNull(director.Rival, "Hay rival");
            Assert.AreEqual(alias, director.Rival.Nombre, "El rival es el del cartel");
            yield return EsperarHasta(() => uso.EnPelea, MaxSegundosEstado, "las habilidades saben que hay pelea");
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

        private IEnumerator TerminarGanando(int premio, string etapa)
        {
            RematarAlRival();
            yield return EsperarVueltaAPie();
            Assert.AreEqual(LadoPelea.Jugador, terminadas[terminadas.Count - 1].Ganador, etapa + " ganó el jugador");
            dineroEsperado += premio;
            Assert.AreEqual(dineroEsperado, cartera.Dinero, etapa + " el premio del cartel ($" + premio + ")");
        }

        // Banco: REPARAR TODO si hay daño (cobra lo de la regla).
        private IEnumerator RepararTodo()
        {
            float danio = persistente.Danio;
            if (danio < ReglaReparacion.DanioMinimo)
            {
                yield break;
            }
            int costo = ReglaReparacion.Costo(danio, precios.CostoReparacionPorPunto, precios.CostoReparacionMinimo);
            int antes = cartera.Dinero;
            yield return IrAlBanco(AvisoReparar);
            yield return Tecla(teclado.eKey);
            Assert.IsTrue(pantallaBanco.Visible, "E abre la pantalla del banco");
            StringAssert.StartsWith("REPARAR TODO", pantallaBanco.TextoBaseOpcion(0));
            yield return Tecla(teclado.enterKey);
            Assert.AreEqual(antes - costo, cartera.Dinero, "Reparar cobró exactamente ReglaReparacion ($" + costo + " por "
                + Num(danio, "F1") + " puntos)");
            dineroEsperado -= costo;
            Assert.AreEqual(vidaMecha.Maxima, vidaMecha.Actual, MargenVida, "La vida quedó completa");
            Assert.AreEqual(0f, persistente.Danio, MargenVida);
            if (pantallaBanco.Visible)
            {
                yield return Tecla(teclado.escapeKey);
            }
            yield return Cuadros(FramesParaElHud);
        }

        // ---- Entrada: el mercado negro ----

        private int FilaTienda(DatosObjeto objeto)
        {
            for (int i = 0; i < pantallaTienda.CantidadFilas; i++)
            {
                if (pantallaTienda.TextoFila(i).Contains(objeto.Nombre.ToUpperInvariant() + " "))
                {
                    return i;
                }
            }
            return -1;
        }

        // Elige la fila del objeto en COMPRAR y compra una: cobra el precio de su oferta y suma uno al inventario.
        private IEnumerator ComprarMunicion(DatosObjeto objeto)
        {
            int fila = FilaTienda(objeto);
            Assert.GreaterOrEqual(fila, 0, "b) el mercado ofrece " + objeto.Nombre);
            int n = pantallaTienda.CantidadFilas;
            yield return Repetir(teclado.downArrowKey, (fila - pantallaTienda.IndiceElegido + n) % n);
            Assert.AreEqual(fila, pantallaTienda.IndiceElegido);
            int dineroAntes = cartera.Dinero;
            int cantidadAntes = inventario.Cantidad(objeto);
            yield return Tecla(teclado.enterKey);
            Assert.AreEqual(dineroAntes - objeto.Oferta.Precio, cartera.Dinero, "b) comprar " + objeto.Nombre + " cobra $" + objeto.Oferta.Precio);
            dineroEsperado -= objeto.Oferta.Precio;
            Assert.AreEqual(cantidadAntes + 1, inventario.Cantidad(objeto), "b) y suma uno al inventario");
        }

        // ---- Entrada: PIEZAS DEL MECHA ----

        private IEnumerator AbrirPiezas()
        {
            yield return IrAlBanco(AvisoPiezas);
            yield return Tecla(teclado.eKey);
            Assert.IsTrue(pantallaPiezas.Visible, "E abre PIEZAS DEL MECHA");
            Assert.AreEqual(TituloPiezas, pantallaPiezas.Titulo);
            Assert.AreEqual(NivelPantallaPiezas.Ranuras, pantallaPiezas.Nivel);
            Assert.AreEqual(NumeroRanuras, pantallaPiezas.CantidadFilas, "Una fila por ranura");
        }

        private IEnumerator CerrarPiezas()
        {
            if (pantallaPiezas.Nivel == NivelPantallaPiezas.Piezas)
            {
                yield return Tecla(teclado.escapeKey);
            }
            yield return Tecla(teclado.escapeKey);
            Assert.IsFalse(pantallaPiezas.Visible, "Esc cierra PIEZAS DEL MECHA");
            yield return Cuadros(FramesParaElHud);
        }

        private IEnumerator IrAFila(int fila)
        {
            yield return Repetir(teclado.upArrowKey, pantallaPiezas.CantidadFilas);
            yield return Repetir(teclado.downArrowKey, fila);
            Assert.AreEqual(fila, pantallaPiezas.IndiceElegido);
        }

        private IEnumerator EntrarARanura(RanuraPieza ranura)
        {
            yield return IrAFila((int)ranura);
            yield return Tecla(teclado.enterKey);
            Assert.AreEqual(NivelPantallaPiezas.Piezas, pantallaPiezas.Nivel, "Enter entra a la ranura " + ranura);
        }

        private int FilaConTexto(string inicio, string fin)
        {
            for (int i = 0; i < pantallaPiezas.CantidadFilas; i++)
            {
                string texto = pantallaPiezas.TextoBaseFila(i);
                if (texto.StartsWith(inicio, StringComparison.Ordinal) && texto.EndsWith(fin, StringComparison.Ordinal))
                {
                    return i;
                }
            }
            return -1;
        }

        // Entra a la ranura de la pieza, elige su fila, comprueba la diferencia de habilidad que muestra y compra.
        private IEnumerator ComprarPieza(DatosPieza pieza, string diferenciaEsperada)
        {
            yield return EntrarARanura(pieza.Ranura);
            int fila = FilaConTexto(pieza.Nombre + " ·", TextoComprar + FormatoDinero.Texto(pieza.Precio));
            Assert.GreaterOrEqual(fila, 0, "Sale '" + pieza.Nombre + TextoComprar + FormatoDinero.Texto(pieza.Precio) + "' en la lista");
            yield return IrAFila(fila);
            StringAssert.Contains(diferenciaEsperada, pantallaPiezas.Diferencia, "c) la diferencia muestra la habilidad nueva");
            DatosPieza quitada = equipo.Puesta(pieza.Ranura);
            int cantidadQuitada = inventario.Cantidad(quitada);
            int antes = cartera.Dinero;
            yield return Tecla(teclado.enterKey);
            Assert.AreEqual(antes - pieza.Precio, cartera.Dinero, "c) comprar cobró el precio de " + pieza.Nombre);
            dineroEsperado -= pieza.Precio;
            Assert.AreEqual(pieza, equipo.Puesta(pieza.Ranura), pieza.Nombre + " quedó puesta");
            Assert.AreEqual(cantidadQuitada + 1, inventario.Cantidad(quitada), "La que se quitó entró al inventario");
            StringAssert.Contains(pieza.Nombre, pantallaPiezas.Aviso, "El aviso dice cuál se puso");
        }

        // ---- Entrada: subir, encender y bajar ----

        private IEnumerator SubirAlMecha()
        {
            Transform mecha = montaje.transform;
            jugador.Colocar(new Pose(mecha.position + mecha.forward * DistanciaAlMecha, Quaternion.LookRotation(-mecha.forward)));
            atajos.Add("Colocar (mecha): a 5 m frente al mecha");
            yield return Cuadros(FramesParaElHud);
            Press(teclado.wKey);
            float fin = Time.time + MaxSegundosCaminando;
            while (Time.time < fin && Vector3.Distance(jugador.Raiz.position, montaje.PuntoSubida) >= DistanciaParaSubir)
            {
                yield return null;
                Vigilar();
            }
            Release(teclado.wKey);
            yield return null;
            Vigilar();
            yield return Tecla(teclado.eKey);
            Assert.AreEqual(ModoJugador.Mecha, jugador.Modo, "La E frente al mecha lo sube");
            yield return null;
            Vigilar();
            Assert.AreEqual(PasoEncendido.Paso1, encendido.Paso, "Arriba empieza el encendido");
        }

        // F (luces), Espacio sostenido y Q: el encendido de los tres pasos.
        private IEnumerator Encender()
        {
            yield return Esperar(RetrasoTecla);
            yield return Tecla(teclado.fKey);
            yield return EsperarHasta(() => encendido.Paso == PasoEncendido.Paso2, MaxSegundosEncendido, "paso 2");
            yield return null;
            Vigilar();
            Assert.IsTrue(pantallaCabina.Visible, "Con las luces de la cabina, la pantalla de piezas se prende");
            Press(teclado.spaceKey);
            yield return EsperarHasta(() => encendido.Paso == PasoEncendido.Paso3, MaxSegundosEncendido, "paso 3");
            Release(teclado.spaceKey);
            yield return Esperar(SegundosAsentar);
            yield return Tecla(teclado.qKey);
            yield return EsperarHasta(() => encendido.EstaEncendido, MaxSegundosEncendido, "encendido completo");
        }

        // E baja del mecha y M lo manda de vuelta al taller.
        private IEnumerator BajarYRegresarElMecha()
        {
            yield return EsperarHasta(() => montaje.PuedeBajar, MaxSegundosCaminando, "el mecha puede dejar bajar");
            yield return Tecla(teclado.eKey);
            Assert.AreEqual(ModoJugador.APie, jugador.Modo, "E baja del mecha");
            yield return Cuadros(FramesParaElHud);
            yield return Tecla(teclado.mKey);
            yield return EsperarHasta(() => llamado.Estado == EstadoLlamado.EnTaller, MaxSegundosRegreso, "M regresa el mecha al taller");
            yield return Cuadros(FramesParaElHud);
        }

        // ---- La pantalla de habilidades y las teclas 1 y 2 ----

        private string FilaEsperada(int fila, int municion)
        {
            DatosPieza pieza = fila == 0 ? brazoIzqMilitar1 : torsoTech1;
            return string.Format(configPantalla.FormatoFila, TextoHabilidad.Tecla(pieza.Ranura),
                TextoHabilidad.Etiqueta(pieza.Habilidad, pieza.Nivel), municion);
        }

        private float EnfriamientoEsperado(DatosPieza pieza)
        {
            return ReglaHabilidad.Enfriamiento(pieza.Habilidad.Enfriamiento, ReglaEstadisticasPiezas.ReduccionEnfriamiento(equipo.Puestas));
        }

        private int ImpactosDeTipo(List<Impacto> lista, TipoDanio tipo)
        {
            return lista.Count(g => g.Tipo == tipo);
        }

        // ---- Capturas (solo con CAPTURAR_T180=1) ----

        private static bool CapturaPedida => Environment.GetEnvironmentVariable(VariableCaptura) == "1";

        // Los lienzos en Overlay no salen en una cámara a textura: se pasan un instante a ScreenSpaceCamera con la cámara pedida.
        private void Capturar(Camera camara, string ruta)
        {
            if (!CapturaPedida)
            {
                return;
            }
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

        private Camera CamaraDeCabina()
        {
            return montaje.GetComponentInChildren<Camera>(true);
        }

        // ---- Tramos ----

        // a) Inicio: liga 1, piezas de serie, sin munición, dinero fijado por código.
        private void Inicio()
        {
            Assert.AreEqual(precios.DineroInicial, cartera.Dinero, "a) empieza con el dinero inicial del asset");
            cartera.Abonar(DineroDePrueba - cartera.Dinero, MotivoDinero.Otro);
            atajos.Add("Abonar: dinero inicial fijado en $" + DineroDePrueba + " por código");
            dineroEsperado = DineroDePrueba;
            Assert.AreEqual(dineroEsperado, cartera.Dinero);
            Assert.AreEqual(Sotano, ligas.LigaActual, "a) liga 1");
            Assert.AreEqual(1, ligas.Siguiente, "a) siguiente 1");
            Assert.AreEqual(NumeroRanuras, equipo.Puestas.Count);
            for (int i = 0; i < NumeroRanuras; i++)
            {
                Assert.AreEqual(RamaPieza.DeSerie, equipo.Puestas[i].Rama, "a) la ranura " + (RanuraPieza)i + " es de serie");
            }
            Assert.AreEqual(0, inventario.Cantidad(misiles), "a) sin misiles");
            Assert.AreEqual(0, inventario.Cantidad(celdas), "a) sin celdas");
            Assert.AreEqual(0, inventario.Todos.Count, "a) inventario vacío");
            Assert.IsNull(uso.Habilidad(RanuraPieza.BrazoIzquierdo), "a) las piezas de serie no traen habilidad");
            Assert.IsNull(uso.Habilidad(RanuraPieza.Torso), "a) ni el torso de serie");
        }

        // b) Mercado negro: 3 misiles y 2 celdas; la pestaña VENDER no ofrece munición.
        private IEnumerator ComprarMunicionEnElMercado()
        {
            yield return IrAlMercado();
            yield return Tecla(teclado.eKey);
            Assert.IsTrue(pantallaTienda.Visible, "E abre la tienda");
            Assert.AreEqual(PestanaTienda.Comprar, pantallaTienda.Pestana);
            for (int i = 0; i < MisilesAComprar; i++)
            {
                yield return ComprarMunicion(misiles);
            }
            for (int i = 0; i < CeldasAComprar; i++)
            {
                yield return ComprarMunicion(celdas);
            }
            Assert.AreEqual(MisilesAComprar, inventario.Cantidad(misiles), "b) 3 misiles");
            Assert.AreEqual(CeldasAComprar, inventario.Cantidad(celdas), "b) 2 celdas");
            StringAssert.Contains(FormatoDinero.Texto(cartera.Dinero), pantallaTienda.Dinero, "b) la tienda muestra el dinero que queda");
            StringAssert.Contains("LLEVAS " + MisilesAComprar + "/" + misiles.Maximo, pantallaTienda.TextoFila(FilaTienda(misiles)));
            if (pantallaTienda.PuedeVender)
            {
                yield return Tecla(teclado.rightArrowKey);
                Assert.AreEqual(PestanaTienda.Vender, pantallaTienda.Pestana, "→ pasa a VENDER");
                for (int i = 0; i < pantallaTienda.CantidadFilas; i++)
                {
                    string fila = pantallaTienda.TextoFila(i);
                    Assert.IsFalse(fila.Contains(misiles.Nombre.ToUpperInvariant()), "b) VENDER no ofrece misiles: " + fila);
                    Assert.IsFalse(fila.Contains(celdas.Nombre.ToUpperInvariant()), "b) VENDER no ofrece celdas: " + fila);
                }
            }
            yield return Tecla(teclado.escapeKey);
            yield return Cuadros(FramesParaElHud);
            Assert.IsFalse(pantallaTienda.Visible, "Esc cierra la tienda");
        }

        // c) Taller: BRAZO IZQ. MILITAR I y TORSO TECH I; la diferencia mostró la habilidad.
        private IEnumerator ComprarLasPiezasConHabilidad()
        {
            yield return AbrirPiezas();
            yield return ComprarPieza(brazoIzqMilitar1, DiferenciaBrazo);
            Assert.AreEqual(NivelPantallaPiezas.Ranuras, pantallaPiezas.Nivel, "Tras comprar vuelve a las ranuras");
            yield return ComprarPieza(torsoTech1, DiferenciaTorso);
            Assert.AreEqual(1, inventario.Cantidad(brazoIzqSerie), "c) el brazo de serie quedó en el inventario");
            Assert.AreEqual(1, inventario.Cantidad(torsoSerie), "c) el torso de serie quedó en el inventario");
            Assert.AreEqual(brazoIzqMilitar1.Habilidad, uso.Habilidad(RanuraPieza.BrazoIzquierdo));
            Assert.AreEqual(torsoTech1.Habilidad, uso.Habilidad(RanuraPieza.Torso));
            yield return CerrarPiezas();
        }

        // d) Sube, enciende y mira la pantalla de habilidades en gris; 1 fuera de pelea no gasta.
        private IEnumerator VerLaCabina()
        {
            yield return SubirAlMecha();
            yield return Encender();
            Assert.IsTrue(pantallaHabilidades.Visible, "d) la pantalla de habilidades se prende con el encendido");
            Assert.AreEqual("1 · MISIL I · ×" + MisilesAComprar, pantallaHabilidades.TextoFila(0));
            Assert.AreEqual("2 · ESCUDO I · ×" + CeldasAComprar, pantallaHabilidades.TextoFila(1));
            Assert.AreEqual(FilaEsperada(0, MisilesAComprar), pantallaHabilidades.TextoFila(0), "d) coincide con el formato del asset");
            Assert.AreEqual(configPantalla.ColorGris, pantallaHabilidades.ColorFila(0), "d) fuera de pelea la fila 1 va en gris");
            Assert.AreEqual(configPantalla.ColorGris, pantallaHabilidades.ColorFila(1), "d) y la fila 2 también");
            Assert.AreEqual(MotivoHabilidad.FueraDePelea, uso.Motivo(RanuraPieza.BrazoIzquierdo));
            yield return Tecla(teclado.digit1Key);
            yield return Tecla(teclado.digit2Key);
            yield return Cuadros(FramesParaElHud);
            Assert.AreEqual(MisilesAComprar, inventario.Cantidad(misiles), "d) 1 fuera de pelea no gasta");
            Assert.AreEqual(CeldasAComprar, inventario.Cantidad(celdas), "d) 2 fuera de pelea no gasta");
            Assert.IsFalse(lanzador.EscudoActivo, "d) ni prende el escudo");
            yield return BajarYRegresarElMecha();
        }

        // Usa la habilidad del brazo con la tecla 1 y devuelve cuántas municiones quedan.
        private IEnumerator DispararMisil(int municionEsperadaDespues, int impactosPrevios, string rutaCaptura = null)
        {
            Assert.AreEqual(MotivoHabilidad.Puede, uso.Motivo(RanuraPieza.BrazoIzquierdo), "e) el misil está listo");
            yield return Tecla(teclado.digit1Key);
            Assert.AreEqual(municionEsperadaDespues, inventario.Cantidad(misiles), "e) 1 gastó un misil");
            if (rutaCaptura != null)
            {
                yield return Esperar(SegundosMisilEnVuelo);
                Assert.IsNotNull(Object.FindFirstObjectByType<ProyectilHabilidad>(), "e) el misil sigue en vuelo");
                Capturar(CamaraDeCabina(), rutaCaptura);
            }
            Assert.AreEqual(municionEsperadaDespues, uso.Municion(RanuraPieza.BrazoIzquierdo));
            Assert.AreEqual(EnfriamientoEsperado(brazoIzqMilitar1), uso.EnfriamientoTotal(RanuraPieza.BrazoIzquierdo), Tolerancia,
                "e) el enfriamiento es el del asset con la reducción de la pieza tech");
            yield return EsperarHasta(() => ImpactosDeTipo(golpesAlRival, TipoDanio.Disparo) > impactosPrevios, MaxSegundosHabilidad,
                "el misil pega al rival");
            Impacto pegado = golpesAlRival.Last(g => g.Tipo == TipoDanio.Disparo);
            float danioMisil = brazoIzqMilitar1.Habilidad.Danio * ReglaHabilidad.Potencia(brazoIzqMilitar1.Nivel);
            Assert.AreEqual(danioMisil, pegado.Cantidad, Tolerancia, "e) el misil lleva el daño del asset (" + Num(danioMisil) + ")");
            Assert.Greater(pegado.Aplicado, 0f, "e) y el rival lo recibe");
        }

        // e) TORNILLO: misil, enfriamiento, otra vez, tercer misil, sin munición y a puños.
        private IEnumerator PelearConMisiles()
        {
            CartelPelea cartel = calendario.Cartel(Sotano, 1);
            OpcionCartelera opcion = Opcion(TipoOpcionCartelera.Siguiente, Sotano, 1);
            Assert.AreEqual(AliasPrimero, mostrador.AliasDe(opcion));
            golpesAlRival.Clear();
            golpesAlJugador.Clear();
            yield return AbrirCarteleraYElegir(opcion);
            yield return RetarSinApostar();
            yield return EsperarPelea(AliasPrimero);
            Assert.AreEqual(configPantalla.TextoListo, pantallaHabilidades.TextoEstado(0), "e) en pelea la fila 1 dice LISTO");
            Assert.AreEqual(brazoIzqMilitar1.Habilidad.ColorRama, pantallaHabilidades.ColorFila(0), "e) y se pinta del color de su rama");
            VidaMecha vidaRival = (VidaMecha)director.Rival.Vida;
            float vidaRivalAntes = vidaRival.Actual;

            // Primer misil: con el rival ya parado frente al mecha. Si el rival camina hacia el misil, a veces el barrido del misil lo
            // ve ya traslapado y explota en el origen del mundo sin dañar (hallazgo anotado en la bitácora, no se arregla aquí).
            // Sale, pega y la fila queda enfriando.
            yield return EsperarHasta(() => Vector3.Distance(vidaRival.transform.position, montaje.transform.position) <= DistanciaParaElMisil
                || vidaMecha.EstaKO, MaxSegundosHabilidad, "el rival se acerca");
            float lanzado = Time.time;
            yield return DispararMisil(MisilesAComprar - 1, 0, RutaCaptura1);
            Assert.AreEqual(vidaRivalAntes - golpesAlRival.Last(g => g.Tipo == TipoDanio.Disparo).Aplicado, vidaRival.Actual,
                1f + Tolerancia, "e) el rival perdió el daño del misil (más lo que le pegó el jugador, que no golpeó)");
            Assert.AreEqual(MotivoHabilidad.Enfriando, uso.Motivo(RanuraPieza.BrazoIzquierdo), "e) la fila 1 está enfriando");
            StringAssert.EndsWith(" s", pantallaHabilidades.TextoEstado(0), "e) la fila 1 muestra los segundos");
            Assert.Greater(pantallaHabilidades.Barra(0), 0f, "e) y la barra de enfriamiento");

            // 1 otra vez antes del enfriamiento no gasta.
            yield return Tecla(teclado.digit1Key);
            Assert.AreEqual(MisilesAComprar - 1, inventario.Cantidad(misiles), "e) 1 antes del enfriamiento no gasta");
            Assert.AreEqual(ImpactosDeTipo(golpesAlRival, TipoDanio.Disparo), 1, "e) ni sale otro misil");

            // Tras el enfriamiento (medido) dispara el segundo.
            yield return EsperarHasta(() => uso.Motivo(RanuraPieza.BrazoIzquierdo) == MotivoHabilidad.Puede || vidaMecha.EstaKO,
                MaxSegundosHabilidad, "termina el enfriamiento del misil");
            Assert.IsFalse(vidaMecha.EstaKO, "e) el jugador sigue en pie");
            float medido = Time.time - lanzado;
            Assert.AreEqual(EnfriamientoEsperado(brazoIzqMilitar1), medido, ToleranciaEnfriamiento,
                "e) el enfriamiento medido es el calculado (" + Num(medido) + " s contra " + Num(EnfriamientoEsperado(brazoIzqMilitar1)) + " s)");
            Debug.Log("[Prueba T-180] Enfriamiento del misil: medido " + Num(medido, "F3") + " s, calculado "
                + Num(EnfriamientoEsperado(brazoIzqMilitar1), "F3") + " s, base " + Num(brazoIzqMilitar1.Habilidad.Enfriamiento, "F3")
                + " s, reduccion " + Num(ReglaEstadisticasPiezas.ReduccionEnfriamiento(equipo.Puestas), "F3"));
            yield return DispararMisil(MisilesAComprar - 2, 1);

            // Tercer misil: la fila queda sin munición.
            lanzado = Time.time;
            yield return EsperarHasta(() => uso.Motivo(RanuraPieza.BrazoIzquierdo) == MotivoHabilidad.Puede || vidaMecha.EstaKO,
                MaxSegundosHabilidad, "termina el segundo enfriamiento");
            Assert.IsFalse(vidaMecha.EstaKO, "e) el jugador sigue en pie");
            yield return DispararMisil(0, 2);
            Assert.AreEqual(0, inventario.Cantidad(misiles), "e) se acabaron los misiles");
            yield return Cuadros(FramesParaElHud);
            Assert.AreEqual("1 · MISIL I · ×0", pantallaHabilidades.TextoFila(0));
            Assert.AreEqual(configPantalla.TextoSinMunicion, pantallaHabilidades.TextoEstado(0), "e) la fila 1 dice SIN MUNICIÓN");
            Assert.AreEqual(3, ImpactosDeTipo(golpesAlRival, TipoDanio.Disparo), "e) pegaron los 3 misiles");

            // Sin munición: a puños. Un golpe limpio lleva el daño compuesto de las piezas.
            yield return EsperarHasta(() => uso.Motivo(RanuraPieza.BrazoIzquierdo) == MotivoHabilidad.SinMunicion || vidaMecha.EstaKO,
                MaxSegundosHabilidad, "sin munición el motivo es SinMunicion");
            Assert.AreEqual(MotivoHabilidad.SinMunicion, uso.Motivo(RanuraPieza.BrazoIzquierdo));
            yield return Tecla(teclado.digit1Key);
            Assert.AreEqual(0, inventario.Cantidad(misiles), "e) con 0 misiles 1 no hace nada");
            Assert.AreEqual(3, ImpactosDeTipo(golpesAlRival, TipoDanio.Disparo));
            float limite = vidaMecha.Maxima * FraccionDanioBuscada;
            yield return ClicsHasta(() => (vidaMecha.Actual <= limite || vidaMecha.EstaKO)
                && golpesAlRival.Any(g => g.Tipo == TipoDanio.Golpe && g.Bloqueo == TipoBloqueo.Ninguno));
            Assert.IsFalse(vidaMecha.EstaKO, "e) el jugador no perdió");
            Assert.Less(vidaMecha.Actual, vidaMecha.Maxima, "e) el jugador recibió golpes reales");
            Impacto puno = golpesAlRival.First(g => g.Tipo == TipoDanio.Golpe && g.Bloqueo == TipoBloqueo.Ninguno);
            ValoresMecha compuesto = ReglaEstadisticasPiezas.Componer(datosJugador.Valores, equipo.Puestas);
            Assert.AreEqual(compuesto.danioGolpe, puno.Cantidad, Tolerancia, "e) el puño lleva el daño compuesto de las piezas");
            Capturar(CamaraDeCabina(), RutaCaptura2);
            Debug.Log("[Prueba T-180] Misil: " + Num(brazoIzqMilitar1.Habilidad.Danio) + " de daño x3; puno " + Num(puno.Cantidad));
            yield return TerminarGanando(cartel.premioGanar, "e)");
            Assert.AreEqual(2, ligas.Siguiente, "e) el siguiente pasa a 2");
            yield return RepararTodo();
        }

        // f) LA RATA: el escudo quita la mitad de un golpe real; después del escudo, el golpe es completo.
        private IEnumerator CubrirseConElEscudo()
        {
            CartelPelea cartel = calendario.Cartel(Sotano, 2);
            OpcionCartelera opcion = Opcion(TipoOpcionCartelera.Siguiente, Sotano, 2);
            Assert.AreEqual(AliasSegundo, mostrador.AliasDe(opcion));
            golpesAlRival.Clear();
            golpesAlJugador.Clear();
            yield return AbrirCarteleraYElegir(opcion);
            yield return RetarSinApostar();
            yield return EsperarPelea(AliasSegundo);
            RivalBasico rival = ((VidaMecha)director.Rival.Vida).GetComponent<RivalBasico>();
            Assert.IsNotNull(rival, "f) el rival trae su cerebro");
            Assert.AreEqual(vidaMecha.Maxima, vidaMecha.Actual, MargenVida, "f) el jugador empieza completo");

            // Cuando el rival levanta el brazo (aviso de su golpe) se enciende el escudo con 2.
            yield return EsperarHasta(() => rival.EnAviso || vidaMecha.EstaKO, MaxSegundosHabilidad, "el rival avisa su golpe");
            int impactosAntes = golpesAlJugador.Count;
            Assert.AreEqual(MotivoHabilidad.Puede, uso.Motivo(RanuraPieza.Torso), "f) el escudo está listo");
            yield return Tecla(teclado.digit2Key);
            Assert.IsTrue(lanzador.EscudoActivo, "f) 2 activa el escudo");
            Assert.AreEqual(CeldasAComprar - 1, inventario.Cantidad(celdas), "f) y gasta una celda");
            yield return Cuadros(FramesParaElHud);
            Assert.IsTrue(pantallaHabilidades.FranjaVisible, "f) la franja turquesa del cristal se prende");
            Assert.AreEqual(configPantalla.ColorEscudo, pantallaHabilidades.ColorMarco, "f) y el marco de la pantalla");
            yield return EsperarHasta(() => golpesAlJugador.Count > impactosAntes || !lanzador.EscudoActivo || vidaMecha.EstaKO,
                MaxSegundosHabilidad, "el rival pega con el escudo arriba");
            Assert.Greater(golpesAlJugador.Count, impactosAntes, "f) un golpe real del rival cayó durante el escudo");
            Impacto conEscudo = golpesAlJugador[impactosAntes];
            Assert.AreEqual(TipoBloqueo.Ninguno, conEscudo.Bloqueo, "f) sin guardia: es el escudo y nada más");
            float reduccion = torsoTech1.Habilidad.Reduccion;
            Assert.AreEqual(conEscudo.Cantidad * (1f - reduccion), conEscudo.Aplicado, Tolerancia,
                "f) con escudo el golpe quita la mitad (" + Num(conEscudo.Aplicado) + " de " + Num(conEscudo.Cantidad) + ")");

            // Después del escudo, el golpe cae completo.
            yield return EsperarHasta(() => !lanzador.EscudoActivo || vidaMecha.EstaKO, MaxSegundosHabilidad, "termina el escudo");
            Assert.IsFalse(vidaMecha.EstaKO, "f) el jugador sigue en pie");
            int tras = golpesAlJugador.Count;
            yield return EsperarHasta(() => golpesAlJugador.Count > tras || vidaMecha.EstaKO, MaxSegundosHabilidad, "otro golpe del rival");
            Assert.Greater(golpesAlJugador.Count, tras, "f) el rival volvió a pegar");
            Impacto sinEscudo = golpesAlJugador[tras];
            Assert.AreEqual(TipoBloqueo.Ninguno, sinEscudo.Bloqueo);
            Assert.AreEqual(sinEscudo.Cantidad, sinEscudo.Aplicado, Tolerancia, "f) sin escudo el golpe cae completo (" + Num(sinEscudo.Aplicado) + ")");
            Assert.AreEqual(conEscudo.Cantidad, sinEscudo.Cantidad, Tolerancia, "f) del mismo tamaño que el primero");
            Debug.Log("[Prueba T-180] Escudo: golpe " + Num(conEscudo.Cantidad) + " quitó " + Num(conEscudo.Aplicado) + " con escudo y "
                + Num(sinEscudo.Aplicado) + " sin escudo");
            yield return TerminarGanando(cartel.premioGanar, "f)");
            yield return RepararTodo();
        }

        // g) DON FIERRO: su aviso se ve y su EMBESTIDA llega.
        private IEnumerator PelearConElJefe()
        {
            Assert.IsTrue(ligas.Poner(Sotano, RivalesPorLiga, 0));
            atajos.Add("Poner: liga 1, siguiente 5, 0 campeones");
            CartelPelea cartel = calendario.Cartel(Sotano, RivalesPorLiga);
            OpcionCartelera opcion = Opcion(TipoOpcionCartelera.Siguiente, Sotano, RivalesPorLiga);
            Assert.IsTrue(opcion.EsJefe, "g) el quinto es el jefe");
            Assert.AreEqual(AliasJefe, mostrador.AliasDe(opcion));
            golpesAlRival.Clear();
            golpesAlJugador.Clear();
            yield return AbrirCarteleraYElegir(opcion);
            yield return RetarSinApostar();
            yield return EsperarPelea(AliasJefe);
            VidaMecha vidaRival = (VidaMecha)director.Rival.Vida;
            HabilidadRival habilidad = vidaRival.GetComponent<HabilidadRival>();
            Assert.IsNotNull(habilidad, "g) el jefe trae su habilidad");
            DatosHabilidad embestida = habilidad.Datos.Habilidad;
            float danioEmbestida = embestida.Danio * ReglaHabilidad.Potencia(habilidad.Datos.Nivel);
            bool avisado = false;
            bool lanzada = false;
            float tiempoAviso = 0f;
            float tiempoLanzada = 0f;
            habilidad.AvisoIniciado += () => { avisado = true; tiempoAviso = Time.time; };
            habilidad.Lanzada += () => { lanzada = true; tiempoLanzada = Time.time; };

            // El jugador pelea a puños hasta que el jefe avisa (su enfriamiento tarda y está en rango).
            yield return ClicsHasta(() => avisado || vidaMecha.EstaKO || vidaRival.EstaKO);
            Assert.IsTrue(avisado, "g) el jefe avisa su habilidad");
            Assert.IsFalse(vidaMecha.EstaKO, "g) el jugador sigue en pie");
            Assert.IsTrue(habilidad.EnAviso, "g) el jefe sigue en su aviso");
            Capturar(CamaraDeCabina(), RutaCaptura3);
            int impactosAntes = golpesAlJugador.Count;
            yield return EsperarHasta(() => lanzada || vidaMecha.EstaKO, MaxSegundosJefe, "el jefe lanza su embestida");
            Assert.AreEqual(habilidad.Datos.SegundosAviso, tiempoLanzada - tiempoAviso, ToleranciaEnfriamiento,
                "g) el aviso dura lo del asset (" + Num(tiempoLanzada - tiempoAviso) + " s)");
            yield return EsperarHasta(() => golpesAlJugador.Skip(impactosAntes).Any(g => Mathf.Abs(g.Cantidad - danioEmbestida) < Tolerancia)
                || vidaMecha.EstaKO, MaxSegundosHabilidad, "la embestida llega al jugador");
            Impacto llegada = golpesAlJugador.Skip(impactosAntes).First(g => Mathf.Abs(g.Cantidad - danioEmbestida) < Tolerancia);
            Assert.Greater(llegada.Tiempo - tiempoLanzada, 0f, "g) la embestida llega después de lanzarse");
            Assert.AreEqual(embestida.Danio, llegada.Cantidad, Tolerancia, "g) lleva el daño del asset (" + Num(embestida.Danio) + ")");
            Assert.IsTrue(llegada.Aplicado <= llegada.Cantidad + Tolerancia, "g) el jugador pierde su daño o lo bloquea");
            Assert.Greater(llegada.Aplicado, 0f, "g) sin guardia la embestida quita vida");
            Debug.Log("[Prueba T-180] Jefe: aviso " + Num(tiempoLanzada - tiempoAviso) + " s, embestida " + Num(llegada.Cantidad) + " (aplicado "
                + Num(llegada.Aplicado) + ")");
            Assert.IsFalse(vidaMecha.EstaKO, "g) el jugador sigue en pie tras la embestida");
            yield return TerminarGanando(cartel.premioGanar, "g)");
            Assert.AreEqual(Calle, ligas.LigaActual, "g) vencer al jefe sube a la liga 2");
        }

        // h) F5 guarda con 0 misiles y 1 celda; se cambian por código; F9 los devuelve.
        private IEnumerator GuardarYCargar()
        {
            yield return Cuadros(FramesParaElHud);
            string[] idsGuardados = equipo.Puestas.Select(p => p.Id).ToArray();
            int dineroGuardado = cartera.Dinero;
            int misilesGuardados = inventario.Cantidad(misiles);
            int celdasGuardadas = inventario.Cantidad(celdas);
            var inventarioGuardado = inventario.Todos.ToDictionary(o => o.Id, o => inventario.Cantidad(o));
            Assert.AreEqual(0, misilesGuardados, "h) se guarda sin misiles");
            Assert.AreEqual(CeldasAComprar - 1, celdasGuardadas, "h) y con una celda");
            yield return Tecla(teclado.f5Key);
            Assert.IsTrue(ServicioGuardado.Actual.HayPartida, "h) F5 escribió la partida en la ruta temporal");

            Assert.AreEqual(misiles.Maximo, inventario.Agregar(misiles, misiles.Maximo));
            inventario.Quitar(celdas, celdasGuardadas);
            cartera.Abonar(777, MotivoDinero.Otro);
            atajos.Add("Agregar/Quitar (munición) + Abonar: se cambian la munición y el dinero antes de cargar");
            Assert.AreEqual(misiles.Maximo, inventario.Cantidad(misiles));
            Assert.AreEqual(0, inventario.Cantidad(celdas));
            Assert.AreNotEqual(dineroGuardado, cartera.Dinero);

            yield return Tecla(teclado.f9Key);
            yield return Cuadros(FramesParaElHud * 2);
            Assert.AreEqual(misilesGuardados, inventario.Cantidad(misiles), "h) F9 devuelve los misiles");
            Assert.AreEqual(celdasGuardadas, inventario.Cantidad(celdas), "h) F9 devuelve las celdas");
            Assert.AreEqual(dineroGuardado, cartera.Dinero, "h) F9 devuelve el dinero");
            CollectionAssert.AreEqual(idsGuardados, equipo.Puestas.Select(p => p.Id).ToArray(), "h) F9 devuelve las mismas 5 piezas");
            Assert.AreEqual(inventarioGuardado.Count, inventario.Todos.Count, "h) F9 devuelve el mismo inventario");
            foreach (KeyValuePair<string, int> par in inventarioGuardado)
            {
                DatosObjeto objeto = inventario.Todos.FirstOrDefault(o => o.Id == par.Key);
                Assert.IsNotNull(objeto, "h) sigue en el inventario: " + par.Key);
                Assert.AreEqual(par.Value, inventario.Cantidad(objeto), "h) cantidad de " + par.Key);
            }
            Assert.AreEqual(celdasGuardadas, uso.Municion(RanuraPieza.Torso), "h) la pantalla de la cabina lee la munición cargada");
        }

        // ---- Prueba ----

        [UnityTest]
        [Timeout(1800000)]
        public IEnumerator Recorrido_ComprarMunicionYPiezasUsarMisilEscudoYJefeGuardarYCargar_JugandoComoUnaPersona()
        {
            yield return CargarEscena();
            float inicioJuego = Time.time;

            float t = Time.time;
            Inicio();
            AnotarTramo("a) inicio", t);

            t = Time.time;
            yield return ComprarMunicionEnElMercado();
            AnotarTramo("b) mercado negro: 3 misiles y 2 celdas", t);

            t = Time.time;
            yield return ComprarLasPiezasConHabilidad();
            AnotarTramo("c) taller: brazo militar I y torso tech I", t);

            t = Time.time;
            yield return VerLaCabina();
            AnotarTramo("d) subir, encender y ver las habilidades", t);

            t = Time.time;
            yield return PelearConMisiles();
            AnotarTramo("e) TORNILLO: misiles, enfriamiento y a puños", t);

            t = Time.time;
            yield return CubrirseConElEscudo();
            AnotarTramo("f) LA RATA: escudo", t);

            t = Time.time;
            yield return PelearConElJefe();
            AnotarTramo("g) DON FIERRO: aviso y embestida", t);

            t = Time.time;
            yield return GuardarYCargar();
            AnotarTramo("h) guardar y cargar la munición", t);

            // i) Consola, tiempos, frames y la partida real intacta.
            float segundos = Time.time - inicioJuego;
            string piezasFinales = string.Join(", ", equipo.Puestas.Select(p => p.Id));
            string inventarioFinal = string.Join(", ", inventario.Todos.Select(o => o.Id + " x" + inventario.Cantidad(o)));
            Debug.Log("[Prueba T-180] Dinero final $" + cartera.Dinero + " (calculado desde los assets $" + dineroEsperado + "), liga "
                + ligas.LigaActual + ", tiempo de juego " + Num(segundos, "F1") + " s");
            Debug.Log("[Prueba T-180] Piezas finales: " + piezasFinales + " | Inventario final: " + inventarioFinal);
            Debug.Log("[Prueba T-180] Tramos: " + string.Join(" | ", tramos));
            Debug.Log("[Prueba T-180] Atajos: " + string.Join(" | ", atajos.Distinct()));
            Debug.Log("[Prueba T-180] MedidorFrames: " + medidor.Resumen());
            Assert.AreEqual(dineroEsperado, cartera.Dinero, "i) el dinero final es el calculado desde los assets");
            CollectionAssert.AreEqual(
                new[] { IdBrazoIzqMilitar1, equipo.Catalogo.DeSerie(RanuraPieza.BrazoDerecho).Id, IdTorsoTech1,
                    equipo.Catalogo.DeSerie(RanuraPieza.Piernas).Id, equipo.Catalogo.DeSerie(RanuraPieza.Cabina).Id },
                equipo.Puestas.Select(p => p.Id).ToArray(), "i) las piezas finales");
            Assert.AreEqual(3, inventario.Todos.Count, "i) en el inventario quedan el brazo y el torso de serie y una celda");
            Assert.AreEqual(1, inventario.Cantidad(brazoIzqSerie));
            Assert.AreEqual(1, inventario.Cantidad(torsoSerie));
            Assert.AreEqual(CeldasAComprar - 1, inventario.Cantidad(celdas));
            Assert.AreEqual(0, inventario.Cantidad(misiles));
            Assert.AreEqual(Calle, ligas.LigaActual, "i) liga final");
            Assert.Less(segundos, MaxSegundosRecorrido, "El recorrido dura menos de " + MaxSegundosRecorrido + " s de juego");
            Assert.IsTrue(medidor.Cumple, "Sin tirones de frames: " + medidor.Resumen());
            Assert.AreEqual(huellaReal, HuellaDe(rutaReal), "La partida real (persistentDataPath) no se tocó");
            Assert.IsEmpty(errores, "Consola sin errores: " + string.Join(" | ", errores));
        }
    }
}
