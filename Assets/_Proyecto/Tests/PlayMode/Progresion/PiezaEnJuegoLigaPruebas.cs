using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using MechaCyberpunk.Combate.Vida;
using MechaCyberpunk.Core;
using MechaCyberpunk.Economia;
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
    /// T-171: la pieza de rama que pone en juego la liga 1 (PIERNAS CUERPO A CUERPO I desde Ƀ250), en Pruebas_Economia con entrada
    /// simulada: retar a TORNILLO desde la cartelera, apostar, ganar, ver la pieza en el inventario y ponerla desde PIEZAS DEL MECHA
    /// del banco; apostar menos de la mínima no la da; la revancha de la cartelera no anuncia ni da pieza. Atajos permitidos y
    /// anotados: Colocar cerca del mostrador o del banco, dinero por código, y RecibirDanio sobre el rival solo después de daño real al jugador.
    /// </summary>
    public class PiezaEnJuegoLigaPruebas : InputTestFixture
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Tony/Pruebas_Economia.unity";
        private const string RutaEconomia = "Assets/_Proyecto/Economia/Datos/DatosEconomia_Base.asset";
        private const string RutaCalendario = "Assets/_Proyecto/Pelea/Datos/CalendarioLigas_Base.asset";
        private const string AvisoPiezas = "E · Taller · piezas";
        private const string AvisoReparar = "E · Reparar mecha";
        private const string AliasPrimero = "TORNILLO";
        private const string IdPieza = "pieza_piernas_cuerpoacuerpo_1";
        private const string NombrePieza = "PIERNAS CUERPO A CUERPO I";
        private const string TextoMinima = "Ƀ250";
        private const int Sotano = 1;
        private const int DineroDePrueba = 3000;
        private const int ApuestaConPieza = 250;
        private const int ApuestaSinPieza = 100;
        private const int ApuestaRevancha = 500;
        private const int RanuraPiernas = (int)RanuraPieza.Piernas;
        private const int FilasRanuras = 5;
        private const int FramesEstables = 10;
        private const int MaxFramesCalentamiento = 1000;
        private const int FramesParaElHud = 5;
        private const float DeltaTimeEstable = 0.05f;
        private const float MaxSegundosEstado = 90f;
        private const float MaxSegundosGolpes = 90f;
        private const float MaxSegundosSalida = 30f;
        private const float MaxSegundosCaminando = 10f;
        private const float SegundosEntreClics = 0.25f;
        private const float DanioMortal = 100000f;
        private const float FraccionDanioBuscada = 0.9f;
        private const float MargenVida = 0.6f;
        private const float DistanciaAlMostrador = 2f;
        private const float DistanciaAlBanco = 2f;
        private const float AlturaInicio = 0.05f;

        private Keyboard teclado;
        private Mouse raton;
        private string carpeta;
        private JugadorAPie jugador;
        private MostradorPeleas mostrador;
        private EntradaArena entrada;
        private DirectorPelea director;
        private PantallaApuesta pantallaApuesta;
        private PantallaCartelera pantallaCartelera;
        private BancoTaller banco;
        private PantallaBancoTaller pantallaBanco;
        private PantallaPiezasTaller pantallaPiezas;
        private EquipoMecha equipo;
        private MechaPersistente persistente;
        private VidaMecha vidaMecha;
        private HudAPie hud;
        private ICartera cartera;
        private ServicioInventario inventario;
        private ServicioLigas ligas;
        private DatosEconomia precios;
        private CalendarioLigas calendario;
        private DatosPieza pieza;
        private int dineroEsperado;
        private readonly List<string> errores = new List<string>();
        private readonly List<string> atajos = new List<string>();
        private readonly List<ResultadoPelea> terminadas = new List<ResultadoPelea>();
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
            carpeta = Path.Combine(Application.temporaryCachePath, "pieza_en_juego_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(carpeta);
            ServicioGuardado.RutaForzada = Path.Combine(carpeta, "partida.json");
            errores.Clear();
            atajos.Clear();
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

        // ---- Esperas y entrada ----

        private IEnumerator Cuadros(int cuantos)
        {
            for (int i = 0; i < cuantos; i++)
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

        private IEnumerator Tecla(KeyControl tecla)
        {
            Press(tecla);
            yield return null;
            Release(tecla);
            yield return null;
        }

        private IEnumerator Repetir(KeyControl tecla, int veces)
        {
            for (int i = 0; i < veces; i++)
            {
                yield return Tecla(tecla);
            }
        }

        private static float DistanciaPlana(Vector3 a, Vector3 b) => Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));

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
            pantallaPiezas = banco.GetComponentInChildren<PantallaPiezasTaller>(true);
            equipo = banco.GetComponent<EquipoMecha>();
            persistente = montaje.GetComponent<MechaPersistente>();
            vidaMecha = montaje.GetComponent<VidaMecha>();
            Assert.IsNotNull(entrada);
            Assert.IsNotNull(director);
            Assert.IsNotNull(pantallaApuesta);
            Assert.IsNotNull(pantallaCartelera);
            Assert.IsNotNull(pantallaBanco);
            Assert.IsNotNull(pantallaPiezas, "El banco trae PIEZAS DEL MECHA");
            Assert.IsNotNull(equipo);
            cartera = Registro<ICartera>.Actual;
            inventario = Registro<ServicioInventario>.Actual;
            ligas = Registro<ServicioLigas>.Actual;
            Assert.IsNotNull(cartera);
            Assert.IsNotNull(inventario);
            Assert.IsNotNull(ligas);
            precios = AssetDatabase.LoadAssetAtPath<DatosEconomia>(RutaEconomia);
            calendario = AssetDatabase.LoadAssetAtPath<CalendarioLigas>(RutaCalendario);
            Assert.IsNotNull(precios);
            Assert.IsNotNull(calendario);
            pieza = equipo.Catalogo.Buscar(IdPieza);
            Assert.IsNotNull(pieza, "Existe " + IdPieza);
            Assert.AreEqual(NombrePieza, pieza.Nombre);
            Assert.AreEqual(EtapaPrologo.Libre, Registro<IHistoria>.Actual.Etapa, "La escena arranca en Libre");
            EventosPelea.PeleaTerminada += terminadas.Add;
            cartera.Abonar(DineroDePrueba - cartera.Dinero, MotivoDinero.Otro);
            atajos.Add("Abonar: dinero fijado en $" + DineroDePrueba + " por código");
            dineroEsperado = DineroDePrueba;
            Assert.AreEqual(0, inventario.Cantidad(pieza), "Empieza sin la pieza");
            yield return null;
        }

        private IEnumerator IrAlMostrador()
        {
            Vector3 punto = mostrador.transform.position + new Vector3(0f, AlturaInicio, -DistanciaAlMostrador);
            jugador.Colocar(new Pose(punto, Quaternion.identity));
            atajos.Add("Colocar (mostrador de peleas): a 2 m del mostrador");
            yield return Cuadros(FramesParaElHud);
            Assert.LessOrEqual(DistanciaPlana(jugador.Raiz.position, mostrador.transform.position), DistanciaAlMostrador + 0.2f);
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
            }
            Release(raton.leftButton);
            yield return null;
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
            Assert.Fail("No está en la cartelera: " + tipo + " " + liga + "-" + indice);
            return default;
        }

        // E abre la cartelera, ↓ llega a la fila y Enter reta: queda abierta la pantalla de apuesta.
        private IEnumerator RetarDesdeLaCartelera(OpcionCartelera opcion)
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
            Assert.AreEqual(objetivo, pantallaCartelera.IndiceElegido);
            yield return Tecla(teclado.enterKey);
            Assert.IsFalse(pantallaCartelera.Visible, "Enter cierra la cartelera");
            Assert.IsTrue(pantallaApuesta.Visible, "Los carteles traen su pantalla de apuesta");
        }

        // → elige el monto en la pantalla de apuesta y Enter pelea.
        private IEnumerator ElegirMontoYPelear(int monto)
        {
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

        // Pelea con golpes reales hasta el 90 % de vida y remata al rival.
        private IEnumerator PelearYGanar()
        {
            yield return EsperarHasta(() => jugador.Modo == ModoJugador.Mecha, MaxSegundosEstado, "el jugador va en el mecha");
            yield return EsperarHasta(() => director.Estado == EstadoFlujoPelea.Peleando, MaxSegundosEstado, "la pelea");
            Assert.AreEqual(AliasPrimero, director.Rival.Nombre, "El rival es el del cartel");
            float limite = vidaMecha.Maxima * FraccionDanioBuscada;
            yield return ClicsHasta(() => vidaMecha.Actual <= limite || vidaMecha.EstaKO);
            Assert.Less(vidaMecha.Actual, vidaMecha.Maxima, "El jugador recibió golpes reales");
            Assert.IsFalse(vidaMecha.EstaKO, "Y no perdió");
            ((VidaMecha)director.Rival.Vida).RecibirDanio(new InfoDanio(DanioMortal, TipoDanio.Golpe, Vector3.zero, Vector3.forward, null));
            atajos.Add("RecibirDanio: rival rematado tras daño real al mecha");
            yield return EsperarHasta(() => director.Estado == EstadoFlujoPelea.Resultado, MaxSegundosEstado, "el resultado");
            yield return EsperarHasta(() => !entrada.EnCurso, MaxSegundosSalida, "termina la salida");
            yield return null;
            Assert.AreEqual(ModoJugador.APie, jugador.Modo, "Volvió a pie");
            Assert.AreEqual(LadoPelea.Jugador, terminadas[terminadas.Count - 1].Ganador, "Ganó el jugador");
        }

        // Retar a TORNILLO (siguiente de la liga 1), apostar y ganar.
        private IEnumerator GanarAlPrimero(int monto)
        {
            CartelPelea cartel = calendario.Cartel(Sotano, 1);
            DatosApuestaPelea reglas = cartel.apuesta;
            int ganancia = (int)Math.Round(monto * (double)reglas.Momio, MidpointRounding.AwayFromZero);
            yield return RetarDesdeLaCartelera(Opcion(TipoOpcionCartelera.Siguiente, Sotano, 1));
            StringAssert.Contains(NombrePieza, pantallaApuesta.Pieza, "La pantalla de apuesta anuncia la pieza en juego");
            StringAssert.Contains(TextoMinima, pantallaApuesta.Pieza, "Y la mínima para llevársela");
            yield return ElegirMontoYPelear(monto);
            yield return PelearYGanar();
            dineroEsperado += cartel.premioGanar + ganancia;
            Assert.AreEqual(dineroEsperado, cartera.Dinero, "Premio del cartel + apuesta x momio");
        }

        // Banco: REPARAR TODO (el banco ofrece las piezas solo con el mecha sano).
        private IEnumerator RepararTodo()
        {
            float danio = persistente.Danio;
            if (danio <= 0f)
            {
                yield break;
            }
            int costo = ReglaReparacion.Costo(danio, precios.CostoReparacionPorPunto, precios.CostoReparacionMinimo);
            yield return IrAlBanco(AvisoReparar);
            yield return Tecla(teclado.eKey);
            Assert.IsTrue(pantallaBanco.Visible, "E abre la pantalla del banco");
            yield return Tecla(teclado.enterKey);
            dineroEsperado -= costo;
            Assert.AreEqual(dineroEsperado, cartera.Dinero, "Reparar cobró lo de ReglaReparacion");
            Assert.AreEqual(vidaMecha.Maxima, vidaMecha.Actual, MargenVida, "La vida quedó completa");
            if (pantallaBanco.Visible)
            {
                yield return Tecla(teclado.escapeKey);
            }
            yield return Cuadros(FramesParaElHud);
        }

        // ---- Pruebas ----

        [UnityTest]
        [Timeout(900000)]
        public IEnumerator A_ApostarLaMinimaYGanar_LlevaLaPiezaAlInventario_YSePoneDesdePiezasDelMecha()
        {
            yield return CargarEscena();
            yield return GanarAlPrimero(ApuestaConPieza);
            Assert.AreEqual(1, inventario.Cantidad(pieza), "La pieza ganada llegó al inventario");

            yield return RepararTodo();
            yield return IrAlBanco(AvisoPiezas);
            yield return Tecla(teclado.eKey);
            Assert.IsTrue(pantallaPiezas.Visible, "E abre PIEZAS DEL MECHA");
            yield return Repetir(teclado.upArrowKey, FilasRanuras);
            yield return Repetir(teclado.downArrowKey, RanuraPiernas);
            yield return Tecla(teclado.enterKey);
            Assert.AreEqual(NivelPantallaPiezas.Piezas, pantallaPiezas.Nivel, "Enter entra a la ranura de las piernas");
            int fila = -1;
            for (int i = 0; i < pantallaPiezas.CantidadFilas; i++)
            {
                if (pantallaPiezas.TextoBaseFila(i).StartsWith(NombrePieza + " ·", StringComparison.Ordinal)
                    && pantallaPiezas.TextoBaseFila(i).EndsWith("PONER · TIENES 1", StringComparison.Ordinal)) fila = i;
            }
            Assert.GreaterOrEqual(fila, 0, "La pieza ganada sale entre las piernas con PONER · TIENES 1 (la otra fila es la de COMPRAR)");
            StringAssert.EndsWith("PONER · TIENES 1", pantallaPiezas.TextoBaseFila(fila), "Se pone sin pagar");
            yield return Repetir(teclado.upArrowKey, pantallaPiezas.CantidadFilas);
            yield return Repetir(teclado.downArrowKey, fila);
            int dineroAntes = cartera.Dinero;
            yield return Tecla(teclado.enterKey);
            Assert.AreEqual(pieza, equipo.Puesta(RanuraPieza.Piernas), "Las piernas del mecha son las ganadas");
            Assert.AreEqual(dineroAntes, cartera.Dinero, "Ponerla no cobra");
            Assert.AreEqual(0, inventario.Cantidad(pieza), "Al ponerla sale del inventario");
            Assert.IsEmpty(errores, "Consola sin errores: " + string.Join(" | ", errores));
            Debug.Log("[Prueba T-171] Atajos: " + string.Join(" | ", atajos));
        }

        [UnityTest]
        [Timeout(900000)]
        public IEnumerator B_ApostarMenosQueLaMinima_NoDaLaPieza()
        {
            yield return CargarEscena();
            CartelPelea cartel = calendario.Cartel(Sotano, 1);
            Assert.Less(ApuestaSinPieza, cartel.apuesta.ApuestaMinimaPorPieza);
            CollectionAssert.Contains(cartel.apuesta.Montos, ApuestaSinPieza, "La pantalla ofrece apostar $" + ApuestaSinPieza);
            yield return GanarAlPrimero(ApuestaSinPieza);
            Assert.AreEqual(0, inventario.Cantidad(pieza), "Con Ƀ100 no se lleva la pieza");
            Assert.IsEmpty(errores, "Consola sin errores: " + string.Join(" | ", errores));
        }

        [UnityTest]
        [Timeout(900000)]
        public IEnumerator C_LaRevanchaDeLaCartelera_NoAnunciaPieza_NiDaOtra_YLaRConLaPeleaDeLigaEstaApagada()
        {
            yield return CargarEscena();
            Assert.IsFalse(director.PermiteRevancha, "La pelea del mostrador no ofrece revancha con R: no hay segunda pieza por ahí");
            yield return GanarAlPrimero(ApuestaConPieza);
            Assert.AreEqual(1, inventario.Cantidad(pieza));

            yield return RepararTodo();
            CartelPelea cartel = calendario.Cartel(Sotano, 1);
            OpcionCartelera revancha = Opcion(TipoOpcionCartelera.Revancha, Sotano, 1);
            yield return RetarDesdeLaCartelera(revancha);
            Assert.AreEqual(string.Empty, pantallaApuesta.Pieza, "La revancha no anuncia pieza");
            yield return ElegirMontoYPelear(ApuestaRevancha);
            yield return PelearYGanar();
            int ganancia = (int)Math.Round(ApuestaRevancha * (double)cartel.apuesta.Momio, MidpointRounding.AwayFromZero);
            dineroEsperado += revancha.Premio + ganancia;
            Assert.AreEqual(dineroEsperado, cartera.Dinero, "La revancha paga la mitad del premio y la apuesta");
            Assert.AreEqual(1, inventario.Cantidad(pieza), "La revancha ganada con Ƀ500 no da otra pieza");
            Assert.IsNotNull(cartel.apuesta.PiezaEnJuego, "El asset de la liga conserva su pieza (la copia sin pieza es de ejecución)");

            // Una pelea nueva de la liga 1 vuelve a anunciar la pieza.
            yield return RepararTodo();
            yield return RetarDesdeLaCartelera(Opcion(TipoOpcionCartelera.Siguiente, Sotano, 2));
            StringAssert.Contains(NombrePieza, pantallaApuesta.Pieza, "Un rival nuevo de la liga 1 sí anuncia su pieza");
            yield return Tecla(teclado.escapeKey);
            Assert.IsEmpty(errores, "Consola sin errores: " + string.Join(" | ", errores));
        }
    }
}
