using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using MechaCyberpunk.Combate.Vida;
using MechaCyberpunk.Core;
using MechaCyberpunk.Economia;
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
    /// T-173: el camino de las piezas (cierre del bloque 3 de T-F7) en Pruebas_Economia, de noche y en Libre, con entrada simulada:
    /// comprar la primera pieza en el taller, ver el nivel siguiente bloqueado, encender y medir la velocidad, ganar la pieza en
    /// juego apostando, mezclar ramas en los brazos, notar el daño peleando, vender lo viejo en el mercado negro, subir de liga,
    /// quedar bloqueado por renta y guardar y cargar. Los números salen de los assets. Atajos permitidos y anotados: Colocar cerca
    /// del banco, del mostrador, del mercado o del mecha; dinero por código; RecibirDanio al rival solo después de daño real al
    /// jugador; Poner en las ligas, el adeudo de renta y la vida por código solo donde se indica.
    /// </summary>
    public class RecorridoPiezasPruebas : InputTestFixture
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Tony/Pruebas_Economia.unity";
        private const string RutaEconomia = "Assets/_Proyecto/Economia/Datos/DatosEconomia_Base.asset";
        private const string RutaCalendario = "Assets/_Proyecto/Pelea/Datos/CalendarioLigas_Base.asset";
        private const string RutaDatosJugador = "Assets/_Proyecto/Mecha/Datos/DatosMecha_Jugador.asset";
        private const string AvisoPiezas = "E · Taller · piezas";
        private const string AvisoReparar = "E · Reparar mecha";
        private const string AliasPrimero = "TORNILLO";
        private const string AliasSegundo = "LA RATA";
        private const string TituloPiezas = "PIEZAS DEL MECHA";
        private const string TextoComprar = " · COMPRAR · ";
        private const string TextoPonerTienes = " · PONER · TIENES ";
        private const string TextoSeAbre = " · SE ABRE EN LA LIGA ";
        private const string TextoBloqueado = "TALLER BLOQUEADO";
        private const string IdPiernasCuerpo1 = "pieza_piernas_cuerpoacuerpo_1";
        private const string IdBrazoIzqFuego1 = "pieza_brazoizq_fuego_1";
        private const string IdBrazoDerMilitar1 = "pieza_brazoder_militar_1";
        private const string VariableCaptura = "CAPTURAR_T173";
        private const string RutaCaptura1 = "docs/capturas/T-173-1.png";
        private const string RutaCaptura2 = "docs/capturas/T-173-2.png";
        private const string RutaCaptura3 = "docs/capturas/T-173-3.png";
        private const int DineroDePrueba = 3000;
        private const int Sotano = 1;
        private const int Liga3 = 3;
        private const int NumeroRanuras = 5;
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
        private const float MaxSegundosRecorrido = 1700f;
        private const float SegundosEntreClics = 0.25f;
        private const float SegundosCaminandoEnMecha = 3.5f;
        private const float VentanaMedicion = 1f;
        private const float ToleranciaVelocidad = 0.08f;
        private const float RetrasoTecla = 0.1f;
        private const float SegundosAsentar = 0.5f;
        private const float DanioMortal = 100000f;
        private const float DanioGuardado = 20f;
        private const float FraccionDanioBuscada = 0.9f;
        private const float MargenVida = 0.6f;
        private const float Tolerancia = 1e-3f;
        private const float DistanciaAlMostrador = 2f;
        private const float DistanciaAlBanco = 2f;
        private const float DistanciaAlMercado = 2f;
        private const float DistanciaAlMecha = 5f;
        private const float DistanciaParaSubir = 2f;
        private const float AlturaInicio = 0.05f;
        private const int DineroDeLaRenta = 0;

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
        private TallerPiezas taller;
        private EquipoMecha equipo;
        private MontajeMecha montaje;
        private EncendidoMecha encendido;
        private LlamadoMecha llamado;
        private MechaPersistente persistente;
        private VidaMecha vidaMecha;
        private HudAPie hud;
        private ICartera cartera;
        private ServicioInventario inventario;
        private ServicioLigas ligas;
        private DatosEconomia precios;
        private CalendarioLigas calendario;
        private DatosMecha datosJugador;
        private DatosPieza piernasSerie;
        private DatosPieza piernasCuerpo1;
        private DatosPieza brazoIzqSerie;
        private DatosPieza brazoDerSerie;
        private DatosPieza brazoIzqFuego1;
        private DatosPieza brazoDerMilitar1;
        private int dineroEsperado;
        private int framesSinMedir;
        private float velocidadMedida;
        private readonly MedidorFrames medidor = new MedidorFrames();
        private readonly List<string> errores = new List<string>();
        private readonly List<string> atajos = new List<string>();
        private readonly List<string> tramos = new List<string>();
        private readonly List<ResultadoPelea> terminadas = new List<ResultadoPelea>();
        private readonly List<(float cantidad, float aplicado, TipoBloqueo bloqueo)> golpesAlRival =
            new List<(float, float, TipoBloqueo)>();
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
            carpeta = Path.Combine(Application.temporaryCachePath, "recorrido_piezas_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(carpeta);
            ServicioGuardado.RutaForzada = Path.Combine(carpeta, "partida.json");
            rutaReal = Path.Combine(Application.persistentDataPath, "partida.json");
            huellaReal = HuellaDe(rutaReal);
            errores.Clear();
            atajos.Clear();
            tramos.Clear();
            terminadas.Clear();
            golpesAlRival.Clear();
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
            if (objetivo != null && objetivo.Lado == LadoPelea.Rival && info.Atacante != null && info.Atacante.Lado == LadoPelea.Jugador)
            {
                golpesAlRival.Add((info.Cantidad, resultado.DanioAplicado, resultado.Bloqueo));
            }
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
            tramos.Add(nombre + ": " + (Time.time - inicio).ToString("F1", CultureInfo.InvariantCulture) + " s");
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
            taller = montaje.GetComponent<TallerPiezas>();
            equipo = montaje.GetComponent<EquipoMecha>();
            encendido = montaje.GetComponent<EncendidoMecha>();
            llamado = montaje.GetComponent<LlamadoMecha>();
            persistente = montaje.GetComponent<MechaPersistente>();
            vidaMecha = montaje.GetComponent<VidaMecha>();
            Assert.IsNotNull(entrada);
            Assert.IsNotNull(director);
            Assert.IsNotNull(pantallaApuesta);
            Assert.IsNotNull(pantallaCartelera);
            Assert.IsNotNull(pantallaTienda, "El mercado trae su pantalla");
            Assert.IsNotNull(pantallaBanco);
            Assert.IsNotNull(pantallaPiezas, "El banco trae PIEZAS DEL MECHA");
            Assert.IsNotNull(pantallaCabina, "El mecha trae la pantalla de piezas de la cabina");
            Assert.IsNotNull(taller);
            Assert.IsNotNull(equipo);
            Assert.IsNotNull(encendido);
            Assert.IsNotNull(llamado);
            Assert.IsNotNull(persistente);
            cartera = Registro<ICartera>.Actual;
            inventario = Registro<ServicioInventario>.Actual;
            ligas = Registro<ServicioLigas>.Actual;
            Assert.IsNotNull(cartera);
            Assert.IsNotNull(inventario);
            Assert.IsNotNull(ligas);
            precios = AssetDatabase.LoadAssetAtPath<DatosEconomia>(RutaEconomia);
            calendario = AssetDatabase.LoadAssetAtPath<CalendarioLigas>(RutaCalendario);
            datosJugador = AssetDatabase.LoadAssetAtPath<DatosMecha>(RutaDatosJugador);
            Assert.IsNotNull(precios);
            Assert.IsNotNull(calendario);
            Assert.IsNotNull(datosJugador);
            Assert.AreEqual(EtapaPrologo.Libre, Registro<IHistoria>.Actual.Etapa, "La escena arranca en Libre");
            Assert.IsTrue(Registro<IRelojMundo>.Actual.EsDeNoche, "La escena arranca de noche");
            piernasSerie = equipo.Catalogo.DeSerie(RanuraPieza.Piernas);
            brazoIzqSerie = equipo.Catalogo.DeSerie(RanuraPieza.BrazoIzquierdo);
            brazoDerSerie = equipo.Catalogo.DeSerie(RanuraPieza.BrazoDerecho);
            piernasCuerpo1 = Pieza(IdPiernasCuerpo1);
            brazoIzqFuego1 = Pieza(IdBrazoIzqFuego1);
            brazoDerMilitar1 = Pieza(IdBrazoDerMilitar1);
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

        // Clics hasta que el jugador recibe daño real (90 % de su vida) y, si se pide, hasta que su golpe cae al rival sin bloqueo.
        private IEnumerator PelearConGolpesReales(bool esperarGolpeLimpio)
        {
            float limite = vidaMecha.Maxima * FraccionDanioBuscada;
            yield return ClicsHasta(() => (vidaMecha.Actual <= limite || vidaMecha.EstaKO)
                && (!esperarGolpeLimpio || golpesAlRival.Any(g => g.bloqueo == TipoBloqueo.Ninguno)));
            Assert.Less(vidaMecha.Actual, vidaMecha.Maxima, "El jugador recibió golpes reales");
            Assert.IsFalse(vidaMecha.EstaKO, "Y no perdió");
            if (esperarGolpeLimpio)
            {
                Assert.IsTrue(golpesAlRival.Any(g => g.bloqueo == TipoBloqueo.Ninguno), "Un golpe del jugador cayó al rival sin bloqueo");
            }
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
                + danio.ToString("F1", CultureInfo.InvariantCulture) + " puntos)");
            dineroEsperado -= costo;
            Assert.AreEqual(vidaMecha.Maxima, vidaMecha.Actual, MargenVida, "La vida quedó completa");
            Assert.AreEqual(0f, persistente.Danio, MargenVida);
            if (pantallaBanco.Visible)
            {
                yield return Tecla(teclado.escapeKey);
            }
            yield return Cuadros(FramesParaElHud);
        }

        // ---- Entrada: PIEZAS DEL MECHA ----

        // Con el mecha sano, E en el banco abre PIEZAS DEL MECHA.
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

        // ↑ hasta arriba y ↓ hasta la fila (la lista no da la vuelta).
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

        // Entra a la ranura de la pieza, compra la fila "NOMBRE · COMPRAR · $precio" y comprueba cobro y pieza puesta.
        private IEnumerator ComprarPieza(DatosPieza pieza)
        {
            yield return EntrarARanura(pieza.Ranura);
            int fila = FilaConTexto(pieza.Nombre + " ·", TextoComprar + FormatoDinero.Texto(pieza.Precio));
            Assert.GreaterOrEqual(fila, 0, "Sale '" + pieza.Nombre + TextoComprar + FormatoDinero.Texto(pieza.Precio) + "' en la lista");
            yield return IrAFila(fila);
            DatosPieza quitada = equipo.Puesta(pieza.Ranura);
            int cantidadQuitada = inventario.Cantidad(quitada);
            int antes = cartera.Dinero;
            yield return Tecla(teclado.enterKey);
            Assert.AreEqual(antes - pieza.Precio, cartera.Dinero, "Comprar cobró el precio de " + pieza.Nombre);
            dineroEsperado -= pieza.Precio;
            Assert.AreEqual(pieza, equipo.Puesta(pieza.Ranura), pieza.Nombre + " quedó puesta");
            Assert.AreEqual(cantidadQuitada + 1, inventario.Cantidad(quitada), "La que se quitó entró al inventario");
            Assert.AreEqual(NivelPantallaPiezas.Ranuras, pantallaPiezas.Nivel, "Tras comprar vuelve a las ranuras");
            StringAssert.Contains(pieza.Nombre, pantallaPiezas.Aviso, "El aviso dice cuál se puso");
        }

        // Pone una pieza del inventario (sin cobrar).
        private IEnumerator PonerDelInventario(DatosPieza pieza)
        {
            yield return EntrarARanura(pieza.Ranura);
            int fila = FilaConTexto(pieza.Nombre + " ·", TextoPonerTienes + inventario.Cantidad(pieza));
            Assert.GreaterOrEqual(fila, 0, "Sale '" + pieza.Nombre + "' con PONER en la lista");
            yield return IrAFila(fila);
            DatosPieza quitada = equipo.Puesta(pieza.Ranura);
            int cantidadQuitada = inventario.Cantidad(quitada);
            int dineroAntes = cartera.Dinero;
            int cantidadPieza = inventario.Cantidad(pieza);
            yield return Tecla(teclado.enterKey);
            Assert.AreEqual(dineroAntes, cartera.Dinero, "Ponerla no cobra");
            Assert.AreEqual(pieza, equipo.Puesta(pieza.Ranura), pieza.Nombre + " quedó puesta");
            Assert.AreEqual(cantidadPieza - 1, inventario.Cantidad(pieza), "Sale del inventario");
            Assert.AreEqual(cantidadQuitada + 1, inventario.Cantidad(quitada), "La que se quitó entra al inventario");
        }

        // ---- Entrada: subir, encender, caminar y bajar ----

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

        // F (luces), Espacio sostenido y Q: el encendido de los tres pasos. Se detiene en el paso 3 si pide dejarlo a medias.
        private IEnumerator Encender(bool hastaElFinal)
        {
            yield return Esperar(RetrasoTecla);
            yield return Tecla(teclado.fKey);
            yield return EsperarHasta(() => encendido.Paso == PasoEncendido.Paso2, MaxSegundosEncendido, "paso 2");
            yield return null;
            Vigilar();
            Assert.IsTrue(pantallaCabina.Visible, "Con las luces de la cabina, la pantalla de piezas se prende");
            Assert.AreEqual(NumeroRanuras, pantallaCabina.Texto.text.Split('\n').Length, "Lista las 5 piezas:\n" + pantallaCabina.Texto.text);
            foreach (DatosPieza puesta in equipo.Puestas)
            {
                StringAssert.Contains(puesta.Nombre, pantallaCabina.Texto.text, "La pantalla lista " + puesta.Nombre);
            }
            Press(teclado.spaceKey);
            yield return EsperarHasta(() => encendido.Paso == PasoEncendido.Paso3, MaxSegundosEncendido, "paso 3");
            Release(teclado.spaceKey);
            yield return Esperar(SegundosAsentar);
            Assert.IsTrue(pantallaCabina.Visible, "Sigue en el paso 3");
            if (!hastaElFinal)
            {
                yield break;
            }
            yield return Tecla(teclado.qKey);
            yield return EsperarHasta(() => encendido.EstaEncendido, MaxSegundosEncendido, "encendido completo");
        }

        // W sostenida: la velocidad en el último segundo es la compuesta por las piezas.
        private IEnumerator MedirVelocidadEnMecha()
        {
            Press(teclado.wKey);
            float t = 0f;
            float inicioVentana = 0f;
            Vector3 inicio = Vector3.zero;
            bool abierta = false;
            while (t < SegundosCaminandoEnMecha)
            {
                yield return null;
                Vigilar();
                t += Time.deltaTime;
                if (!abierta && t >= SegundosCaminandoEnMecha - VentanaMedicion)
                {
                    abierta = true;
                    inicioVentana = t;
                    inicio = montaje.transform.position;
                }
            }
            velocidadMedida = DistanciaPlana(montaje.transform.position, inicio) / (t - inicioVentana);
            Release(teclado.wKey);
            yield return Esperar(VentanaMedicion);
        }

        // E baja del mecha y M lo manda de vuelta al taller (viene solo, subiendo al cielo).
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

        // ---- Capturas (solo con CAPTURAR_T173=1) ----

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

        // ---- Tramos ----

        // a) Inicio: liga 1, las 5 piezas de serie, dinero fijado por código.
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
                Assert.AreEqual((RanuraPieza)i, equipo.Puestas[i].Ranura);
            }
            Assert.AreEqual(0f, persistente.Danio, MargenVida, "a) mecha sin daño");
            Assert.AreEqual(datosJugador.Valores.vidaMaxima, vidaMecha.Maxima, Tolerancia, "a) la vida es la del mecha de serie");
            Assert.AreEqual(EstadoBanco.Piezas, banco.Estado, "a) sin daño, el banco ofrece las piezas");
            Assert.AreEqual(0, inventario.Todos.Count, "a) inventario vacío");
        }

        // b) Taller: compra las piernas cuerpo a cuerpo I; las de nivel II salen bloqueadas.
        private IEnumerator ComprarLaPrimeraPieza()
        {
            yield return AbrirPiezas();
            StringAssert.Contains(FormatoDinero.Texto(cartera.Dinero), pantallaPiezas.Dinero, "b) la pantalla muestra el dinero");
            yield return EntrarARanura(RanuraPieza.Piernas);
            int bloqueadas = 0;
            for (int i = 0; i < pantallaPiezas.CantidadFilas; i++)
            {
                string texto = pantallaPiezas.TextoBaseFila(i);
                if (texto.Contains(TextoSeAbre))
                {
                    StringAssert.EndsWith(TextoSeAbre + "2", texto, "b) lo bloqueado es del nivel II");
                    bloqueadas++;
                }
            }
            int esperadas = equipo.Catalogo.De(RanuraPieza.Piernas).Count(p => p.Rama != RamaPieza.DeSerie && p.Nivel == 2);
            Assert.AreEqual(esperadas, bloqueadas, "b) las piernas de nivel II salen SE ABRE EN LA LIGA 2");
            Assert.GreaterOrEqual(FilaConTexto(piernasCuerpo1.Nombre + " ·", TextoComprar + FormatoDinero.Texto(piernasCuerpo1.Precio)), 0);

            int filaBloqueada = FilaConTexto("PIERNAS CUERPO A CUERPO II ·", TextoSeAbre + "2");
            Assert.GreaterOrEqual(filaBloqueada, 0, "b) PIERNAS CUERPO A CUERPO II sale bloqueada");
            yield return IrAFila(filaBloqueada);
            int antes = cartera.Dinero;
            yield return Tecla(teclado.enterKey);
            Assert.AreEqual(antes, cartera.Dinero, "b) Enter en una pieza bloqueada no cobra");
            Assert.AreEqual(piernasSerie, equipo.Puesta(RanuraPieza.Piernas), "b) ni la pone");
            Assert.AreEqual(NivelPantallaPiezas.Piezas, pantallaPiezas.Nivel);

            yield return Tecla(teclado.escapeKey);
            yield return ComprarPieza(piernasCuerpo1);
            Assert.AreEqual(1, inventario.Cantidad(piernasSerie), "b) las piernas de serie quedaron en el inventario");
            ValoresMecha esperados = ReglaEstadisticasPiezas.Componer(datosJugador.Valores, equipo.Puestas);
            Assert.AreEqual(datosJugador.Valores.vidaMaxima + piernasCuerpo1.BonoVida, esperados.vidaMaxima, Tolerancia);
            Assert.AreEqual(esperados.vidaMaxima, vidaMecha.Maxima, Tolerancia, "b) el mecha tiene la vida compuesta");
            yield return CerrarPiezas();
        }

        // c) Sube al mecha, lo enciende y mide la velocidad con W sostenida.
        private IEnumerator SubirEncenderYMedir()
        {
            yield return SubirAlMecha();
            yield return Encender(true);
            StringAssert.Contains(piernasCuerpo1.Nombre, pantallaCabina.Texto.text, "c) la pantalla de la cabina lista las piernas nuevas");
            yield return MedirVelocidadEnMecha();
            float compuesta = equipo.Valores.velocidadMaxima;
            Assert.AreEqual(datosJugador.Valores.velocidadMaxima + piernasCuerpo1.BonoVelocidad, compuesta, Tolerancia);
            Assert.AreEqual(compuesta, velocidadMedida, ToleranciaVelocidad,
                "c) la velocidad medida es la compuesta (" + compuesta.ToString("F2", CultureInfo.InvariantCulture) + " m/s)");
            Debug.Log("[Prueba T-173] Velocidad: medida " + velocidadMedida.ToString("F3", CultureInfo.InvariantCulture) + " m/s, compuesta "
                + compuesta.ToString("F3", CultureInfo.InvariantCulture) + " m/s, de serie "
                + datosJugador.Valores.velocidadMaxima.ToString("F3", CultureInfo.InvariantCulture) + " m/s");
            yield return BajarYRegresarElMecha();
        }

        // d) TORNILLO: apuesta la mínima por pieza y gana; la pieza en juego llega al inventario. Repara.
        private IEnumerator ApostarYGanarLaPieza()
        {
            CartelPelea cartel = calendario.Cartel(Sotano, 1);
            DatosApuestaPelea reglas = cartel.apuesta;
            DatosPieza enJuego = reglas.PiezaEnJuego as DatosPieza;
            Assert.IsNotNull(enJuego, "d) la apuesta de la liga 1 trae una pieza en juego");
            Assert.AreEqual(IdPiernasCuerpo1, enJuego.Id, "d) la pieza en juego de la liga 1");
            int apuesta = reglas.Montos.Where(m => m >= reglas.ApuestaMinimaPorPieza && m <= reglas.ApuestaMaxima).Min();
            Assert.GreaterOrEqual(apuesta, reglas.ApuestaMinimaPorPieza);
            int ganancia = (int)Math.Round(apuesta * (double)reglas.Momio, MidpointRounding.AwayFromZero);
            OpcionCartelera opcion = Opcion(TipoOpcionCartelera.Siguiente, Sotano, 1);
            Assert.AreEqual(AliasPrimero, mostrador.AliasDe(opcion));
            int cantidadAntes = inventario.Cantidad(enJuego);
            yield return AbrirCarteleraYElegir(opcion);
            yield return RetarYElegirMonto(apuesta);
            yield return EsperarPelea(AliasPrimero);
            yield return PelearConGolpesReales(false);
            RematarAlRival();
            yield return EsperarVueltaAPie();
            Assert.AreEqual(LadoPelea.Jugador, terminadas[terminadas.Count - 1].Ganador, "d) ganó el jugador");
            dineroEsperado += cartel.premioGanar + ganancia;
            Assert.AreEqual(dineroEsperado, cartera.Dinero, "d) dinero = antes + premio + apuesta x momio ($" + cartel.premioGanar + " + $"
                + apuesta + " x " + reglas.Momio.ToString("F1", CultureInfo.InvariantCulture) + ")");
            Assert.AreEqual(cantidadAntes + 1, inventario.Cantidad(enJuego), "d) la pieza en juego llegó al inventario");
            Assert.AreEqual(enJuego, equipo.Puesta(RanuraPieza.Piernas), "d) y es la misma que ya llevaba puesta: queda en el inventario");
            Assert.AreEqual(2, ligas.Siguiente, "d) el siguiente pasa a 2");
            Assert.AreEqual(Sotano, ligas.LigaActual);
            yield return RepararTodo();
        }

        // e) Brazos de dos ramas, ponerlos y quitarlos del inventario, encenderlo para verlos y notar el daño peleando.
        private IEnumerator MezclarRamasEnLosBrazos()
        {
            yield return AbrirPiezas();
            yield return ComprarPieza(brazoIzqFuego1);
            yield return ComprarPieza(brazoDerMilitar1);
            Assert.AreEqual(1, inventario.Cantidad(brazoIzqSerie), "e) el brazo izquierdo de serie quedó en el inventario");
            Assert.AreEqual(1, inventario.Cantidad(brazoDerSerie), "e) el brazo derecho de serie quedó en el inventario");
            Assert.AreEqual(RamaPieza.Fuego, equipo.Puesta(RanuraPieza.BrazoIzquierdo).Rama);
            Assert.AreEqual(RamaPieza.Militar, equipo.Puesta(RanuraPieza.BrazoDerecho).Rama);

            // Se pone otra vez el brazo de serie (sin cobrar) y luego el de fuego (sin cobrar): el inventario se mueve en los dos sentidos.
            int dineroAntes = cartera.Dinero;
            yield return PonerDelInventario(brazoIzqSerie);
            Assert.AreEqual(1, inventario.Cantidad(brazoIzqFuego1), "e) el de fuego quedó en el inventario");
            yield return PonerDelInventario(brazoIzqFuego1);
            Assert.AreEqual(dineroAntes, cartera.Dinero, "e) cambiar entre las que ya tienes no cobra");
            Assert.AreEqual(brazoIzqFuego1, equipo.Puesta(RanuraPieza.BrazoIzquierdo));
            Assert.AreEqual(1, inventario.Cantidad(brazoIzqSerie), "e) el brazo de serie volvió al inventario");
            Assert.AreEqual(0, inventario.Cantidad(brazoIzqFuego1));

            // Captura 1: la lista del brazo derecho con las ramas mezcladas y el nivel siguiente bloqueado.
            yield return EntrarARanura(RanuraPieza.BrazoDerecho);
            Assert.GreaterOrEqual(FilaConTexto("BRAZO DER. MILITAR II ·", TextoSeAbre + "2"), 0, "e) el nivel II sigue bloqueado");
            yield return Cuadros(FramesParaElHud);
            Capturar(jugador.Camara, RutaCaptura1);
            yield return CerrarPiezas();

            ValoresMecha compuesto = ReglaEstadisticasPiezas.Componer(datosJugador.Valores, equipo.Puestas);
            Assert.AreEqual(datosJugador.Valores.danioGolpe + brazoIzqFuego1.BonoDanio + brazoDerMilitar1.BonoDanio, compuesto.danioGolpe, Tolerancia);

            // Captura 2: la cabina con los brazos de dos ramas durante el encendido.
            yield return SubirAlMecha();
            yield return Encender(false);
            Capturar(montaje.GetComponentInChildren<Camera>(true), RutaCaptura2);
            yield return Tecla(teclado.qKey);
            yield return EsperarHasta(() => encendido.EstaEncendido, MaxSegundosEncendido, "encendido completo");
            yield return BajarYRegresarElMecha();

            // La siguiente pelea: un golpe limpio al rival quita el daño compuesto.
            CartelPelea cartel = calendario.Cartel(Sotano, 2);
            OpcionCartelera opcion = Opcion(TipoOpcionCartelera.Siguiente, Sotano, 2);
            Assert.AreEqual(AliasSegundo, mostrador.AliasDe(opcion));
            golpesAlRival.Clear();
            yield return AbrirCarteleraYElegir(opcion);
            yield return RetarYElegirMonto(0);
            yield return EsperarPelea(AliasSegundo);
            yield return PelearConGolpesReales(true);
            var limpio = golpesAlRival.First(g => g.bloqueo == TipoBloqueo.Ninguno);
            Assert.AreEqual(compuesto.danioGolpe, limpio.cantidad, Tolerancia, "e) el golpe lleva el daño compuesto");
            Assert.AreEqual(compuesto.danioGolpe, limpio.aplicado, Tolerancia, "e) y el rival lo recibe completo");
            RematarAlRival();
            yield return EsperarVueltaAPie();
            Assert.AreEqual(LadoPelea.Jugador, terminadas[terminadas.Count - 1].Ganador, "e) ganó el jugador");
            dineroEsperado += cartel.premioGanar;
            Assert.AreEqual(dineroEsperado, cartera.Dinero, "e) el premio de LA RATA sin apuesta");
            Assert.AreEqual(3, ligas.Siguiente);
            Debug.Log("[Prueba T-173] Golpe al rival: " + limpio.cantidad.ToString("F2", CultureInfo.InvariantCulture)
                + " (compuesto " + compuesto.danioGolpe.ToString("F2", CultureInfo.InvariantCulture) + ", de serie "
                + datosJugador.Valores.danioGolpe.ToString("F2", CultureInfo.InvariantCulture) + ")");
        }

        // f) Mercado negro: vende las piernas de serie y la pieza repetida.
        private IEnumerator VenderLoViejo()
        {
            yield return IrAlMercado();
            yield return Tecla(teclado.eKey);
            Assert.IsTrue(pantallaTienda.Visible, "E abre la tienda");
            Assert.IsTrue(pantallaTienda.PuedeVender, "El mercado negro compra");
            yield return Tecla(teclado.rightArrowKey);
            Assert.AreEqual(PestanaTienda.Vender, pantallaTienda.Pestana, "→ pasa a VENDER");
            Assert.AreEqual(inventario.Todos.Count, pantallaTienda.CantidadFilas, "f) una fila por pieza del inventario");
            yield return Cuadros(FramesParaElHud);
            Capturar(jugador.Camara, RutaCaptura3);
            yield return VenderFila(piernasSerie);
            yield return VenderFila(piernasCuerpo1);
            Assert.AreEqual(0, inventario.Cantidad(piernasSerie), "f) las piernas de serie ya no están");
            Assert.AreEqual(0, inventario.Cantidad(piernasCuerpo1), "f) la repetida ya no está");
            Assert.AreEqual(piernasCuerpo1, equipo.Puesta(RanuraPieza.Piernas), "f) las puestas siguen puestas");
            yield return Tecla(teclado.escapeKey);
            yield return Cuadros(FramesParaElHud);
            Assert.IsFalse(pantallaTienda.Visible, "Esc cierra la tienda");
        }

        private IEnumerator VenderFila(DatosPieza pieza)
        {
            int fila = -1;
            for (int i = 0; i < pantallaTienda.CantidadFilas; i++)
            {
                if (pantallaTienda.TextoFila(i).Contains(pieza.Nombre.ToUpperInvariant() + " "))
                {
                    fila = i;
                }
            }
            Assert.GreaterOrEqual(fila, 0, "f) " + pieza.Nombre + " sale en la lista de venta");
            int n = pantallaTienda.CantidadFilas;
            yield return Repetir(teclado.downArrowKey, (fila - pantallaTienda.IndiceElegido + n) % n);
            Assert.AreEqual(fila, pantallaTienda.IndiceElegido);
            int antes = cartera.Dinero;
            yield return Tecla(teclado.enterKey);
            Assert.AreEqual(antes + pieza.ValorVenta, cartera.Dinero, "f) la venta paga su valor ($" + pieza.ValorVenta + ")");
            dineroEsperado += pieza.ValorVenta;
            yield return Cuadros(FramesParaElHud);
            Assert.AreEqual("+" + FormatoDinero.Texto(pieza.ValorVenta) + " · VENTA", hud.TextoAvisoDinero.text, "f) el HUD anuncia la venta");
        }

        // g) Liga 3 por código: el nivel III se compra y el IV sale bloqueado.
        private IEnumerator SubirDeLiga()
        {
            yield return RepararTodo();
            Assert.IsTrue(ligas.Poner(Liga3, 1, Liga3 - 1));
            atajos.Add("Poner: liga 3, siguiente 1, 2 campeones");
            yield return Cuadros(FramesParaElHud);
            Assert.AreEqual(Liga3, TallerPiezas.LigaActual);
            yield return AbrirPiezas();
            yield return EntrarARanura(RanuraPieza.Piernas);
            DatosPieza[] tienda = equipo.Catalogo.De(RanuraPieza.Piernas).Where(p => p.Rama != RamaPieza.DeSerie && p != piernasCuerpo1).ToArray();
            int compras = 0;
            int bloqueadas = 0;
            foreach (DatosPieza p in tienda)
            {
                if (p.Nivel <= Liga3)
                {
                    Assert.GreaterOrEqual(FilaConTexto(p.Nombre + " ·", TextoComprar + FormatoDinero.Texto(p.Precio)), 0,
                        "g) " + p.Nombre + " se compra");
                    compras++;
                }
                else
                {
                    Assert.GreaterOrEqual(FilaConTexto(p.Nombre + " ·", TextoSeAbre + (Liga3 + 1)), 0,
                        "g) " + p.Nombre + " sale SE ABRE EN LA LIGA 4");
                    bloqueadas++;
                }
            }
            Assert.AreEqual(compras + bloqueadas, pantallaPiezas.CantidadFilas, "g) la lista no trae nada más");
            Assert.Greater(compras, 0);
            Assert.Greater(bloqueadas, 0);
            int fila = FilaConTexto("PIERNAS CUERPO A CUERPO III ·", TextoComprar + FormatoDinero.Texto(1200));
            Assert.GreaterOrEqual(fila, 0, "g) PIERNAS CUERPO A CUERPO III ya se compra");
            yield return CerrarPiezas();
        }

        // h) Renta vencida y sin dinero (por código): el taller no se abre y nada se compra.
        private IEnumerator QuedarBloqueadoPorRenta()
        {
            IRenta renta = Registro<IRenta>.Actual;
            Assert.IsNotNull(renta, "El jugador trae la renta");
            int dineroGuardado = cartera.Dinero;
            string rentaSana = "{\"diaProximoCobro\":" + renta.DiaProximoCobro + ",\"adeudo\":0}";
            ((IGuardable)renta).Cargar("{\"diaProximoCobro\":" + renta.DiaProximoCobro + ",\"adeudo\":" + precios.Renta + "}");
            cartera.CargarDeuda(dineroGuardado, MotivoDinero.Otro);
            atajos.Add("Cargar (renta): adeudo de $" + precios.Renta + " y dinero en Ƀ0 por código");
            Assert.AreEqual(DineroDeLaRenta, cartera.Dinero);
            Assert.IsTrue(renta.TallerBloqueado);
            DatosPieza[] puestasAntes = equipo.Puestas.ToArray();
            yield return IrAlBanco(TextoBloqueado);
            Assert.AreEqual(EstadoBanco.Bloqueado, banco.Estado);
            Assert.AreEqual(banco.Texto, hud.TextoAvisoInteraccion.text, "h) el HUD dice lo de hoy");
            StringAssert.Contains(FormatoDinero.Texto(precios.Renta), hud.TextoAvisoInteraccion.text);
            yield return Tecla(teclado.eKey);
            Assert.IsFalse(pantallaPiezas.Visible, "h) PIEZAS DEL MECHA no se abre");
            Assert.IsFalse(pantallaBanco.Visible, "h) y tampoco la pantalla del banco");
            Assert.AreEqual(DineroDeLaRenta, cartera.Dinero, "h) no se cobró nada");
            CollectionAssert.AreEqual(puestasAntes, equipo.Puestas.ToArray(), "h) las piezas siguen igual");
            foreach (RanuraPieza ranura in Enum.GetValues(typeof(RanuraPieza)))
            {
                foreach (OpcionPieza o in taller.Opciones(ranura))
                {
                    Assert.IsFalse(o.PuedeElegir, "h) ninguna opción se puede elegir: " + o.Pieza.Nombre);
                }
            }
            ((IGuardable)renta).Cargar(rentaSana);
            cartera.Abonar(dineroGuardado, MotivoDinero.Otro);
            atajos.Add("Cargar (renta) + Abonar: se limpia el adeudo y vuelve el dinero");
            Assert.IsFalse(renta.TallerBloqueado);
            Assert.AreEqual(dineroEsperado, cartera.Dinero);
            yield return Cuadros(FramesParaElHud);
        }

        // i) F5 guarda con 20 de daño; se cambian las piezas y el dinero; F9 carga todo.
        private IEnumerator GuardarYCargar()
        {
            vidaMecha.FijarVida(vidaMecha.Maxima - DanioGuardado);
            atajos.Add("FijarVida: el mecha queda con 20 de daño antes de guardar");
            Assert.AreEqual(DanioGuardado, persistente.Danio, MargenVida);
            string[] idsGuardados = equipo.Puestas.Select(p => p.Id).ToArray();
            float vidaMaximaGuardada = vidaMecha.Maxima;
            int dineroGuardado = cartera.Dinero;
            var inventarioGuardado = inventario.Todos.ToDictionary(o => o.Id, o => inventario.Cantidad(o));
            yield return Tecla(teclado.f5Key);
            Assert.IsTrue(ServicioGuardado.Actual.HayPartida, "i) F5 escribió la partida en la ruta temporal");

            equipo.PonerDeSerie();
            cartera.Abonar(777, MotivoDinero.Otro);
            atajos.Add("PonerDeSerie + Abonar: se cambian las piezas y el dinero antes de cargar");
            Assert.AreNotEqual(vidaMaximaGuardada, vidaMecha.Maxima, "i) con las de serie la vida máxima es otra");
            Assert.AreNotEqual(dineroGuardado, cartera.Dinero);

            yield return Tecla(teclado.f9Key);
            yield return Cuadros(FramesParaElHud * 2);
            CollectionAssert.AreEqual(idsGuardados, equipo.Puestas.Select(p => p.Id).ToArray(), "i) F9 devuelve las mismas 5 piezas");
            Assert.AreEqual(vidaMaximaGuardada, vidaMecha.Maxima, Tolerancia, "i) F9 devuelve la misma vida máxima");
            Assert.AreEqual(DanioGuardado, persistente.Danio, MargenVida, "i) F9 devuelve los 20 de daño");
            Assert.AreEqual(dineroGuardado, cartera.Dinero, "i) F9 devuelve el dinero");
            Assert.AreEqual(inventarioGuardado.Count, inventario.Todos.Count, "i) F9 devuelve el mismo inventario");
            foreach (KeyValuePair<string, int> par in inventarioGuardado)
            {
                DatosObjeto objeto = inventario.Todos.FirstOrDefault(o => o.Id == par.Key);
                Assert.IsNotNull(objeto, "i) sigue en el inventario: " + par.Key);
                Assert.AreEqual(par.Value, inventario.Cantidad(objeto), "i) cantidad de " + par.Key);
            }
        }

        // ---- Prueba ----

        [UnityTest]
        [Timeout(1800000)]
        public IEnumerator Recorrido_ComprarGanarMezclarVenderSubirBloquearYGuardar_JugandoComoUnaPersona()
        {
            yield return CargarEscena();
            float inicioJuego = Time.time;

            float t = Time.time;
            Inicio();
            AnotarTramo("a) inicio", t);

            t = Time.time;
            yield return ComprarLaPrimeraPieza();
            AnotarTramo("b) comprar piernas cuerpo a cuerpo I y ver el nivel II bloqueado", t);

            t = Time.time;
            yield return SubirEncenderYMedir();
            AnotarTramo("c) subir, encender y medir la velocidad", t);

            t = Time.time;
            yield return ApostarYGanarLaPieza();
            AnotarTramo("d) apostar a TORNILLO, ganar la pieza y reparar", t);

            t = Time.time;
            yield return MezclarRamasEnLosBrazos();
            AnotarTramo("e) brazos de fuego y militar, encendido y pelea", t);

            t = Time.time;
            yield return VenderLoViejo();
            AnotarTramo("f) mercado negro", t);

            t = Time.time;
            yield return SubirDeLiga();
            AnotarTramo("g) liga 3 en el taller", t);

            t = Time.time;
            yield return QuedarBloqueadoPorRenta();
            AnotarTramo("h) renta vencida", t);

            t = Time.time;
            yield return GuardarYCargar();
            AnotarTramo("i) guardar y cargar", t);

            // j) Consola, tiempos, frames y la partida real intacta.
            float segundos = Time.time - inicioJuego;
            string piezasFinales = string.Join(", ", equipo.Puestas.Select(p => p.Id));
            string inventarioFinal = string.Join(", ", inventario.Todos.Select(o => o.Id + " x" + inventario.Cantidad(o)));
            Debug.Log("[Prueba T-173] Dinero final $" + cartera.Dinero + " (calculado desde los assets $" + dineroEsperado + "), liga "
                + ligas.LigaActual + ", tiempo de juego " + segundos.ToString("F1", CultureInfo.InvariantCulture) + " s");
            Debug.Log("[Prueba T-173] Piezas finales: " + piezasFinales + " | Inventario final: " + inventarioFinal);
            Debug.Log("[Prueba T-173] Tramos: " + string.Join(" | ", tramos));
            Debug.Log("[Prueba T-173] Atajos: " + string.Join(" | ", atajos.Distinct()));
            Debug.Log("[Prueba T-173] MedidorFrames: " + medidor.Resumen());
            Assert.AreEqual(dineroEsperado, cartera.Dinero, "j) el dinero final es el calculado desde los assets");
            CollectionAssert.AreEqual(
                new[] { IdBrazoIzqFuego1, IdBrazoDerMilitar1, equipo.Catalogo.DeSerie(RanuraPieza.Torso).Id, IdPiernasCuerpo1,
                    equipo.Catalogo.DeSerie(RanuraPieza.Cabina).Id },
                equipo.Puestas.Select(p => p.Id).ToArray(), "j) las piezas finales");
            Assert.AreEqual(2, inventario.Todos.Count, "j) en el inventario solo quedan los dos brazos de serie");
            Assert.AreEqual(1, inventario.Cantidad(brazoIzqSerie));
            Assert.AreEqual(1, inventario.Cantidad(brazoDerSerie));
            Assert.AreEqual(Liga3, ligas.LigaActual, "j) liga final");
            Assert.Less(segundos, MaxSegundosRecorrido, "El recorrido dura menos de " + MaxSegundosRecorrido + " s de juego");
            Assert.IsTrue(medidor.Cumple, "Sin tirones de frames: " + medidor.Resumen());
            Assert.AreEqual(huellaReal, HuellaDe(rutaReal), "La partida real (persistentDataPath) no se tocó");
            Assert.IsEmpty(errores, "Consola sin errores: " + string.Join(" | ", errores));
        }
    }
}
