using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using MechaCyberpunk.Combate.Vida;
using MechaCyberpunk.Core;
using MechaCyberpunk.Economia;
using MechaCyberpunk.Mecha;
using MechaCyberpunk.Mecha.Taller;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Personaje.Movimiento;
using MechaCyberpunk.Progresion.Pruebas;
using MechaCyberpunk.UI.Hud;
using MechaCyberpunk.UI.Taller;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Tests.UI
{
    /// <summary>
    /// T-169: pantalla "PIEZAS DEL MECHA" del banco del taller, en Pruebas_Economia con el jugador real y entrada simulada
    /// (W hasta el banco, E, ↑ ↓, Enter, →, ←, Esc).
    /// </summary>
    public class PantallaPiezasPruebas : InputTestFixture
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Tony/Pruebas_Economia.unity";
        private const string VariableCaptura = "CAPTURAR_T169";
        private const string RutaCaptura1 = "docs/capturas/T-169-1.png";
        private const string RutaCaptura2 = "docs/capturas/T-169-2.png";
        private const string AvisoPiezas = "E · Taller · piezas";
        private const string AdeudoDeRenta = "{\"diaProximoCobro\":15,\"adeudo\":500}";
        private const int FramesEstables = 10;
        private const int MaxFramesCalentamiento = 1000;
        private const float DeltaTimeEstable = 0.05f;
        private const float MaxSegundosCaminar = 10f;
        private const float MargenLlegada = 2f;
        private const float DistanciaInicial = 4f;
        private const float AlturaJugador = 0.05f;
        private const float SegundosEspera = 3f;
        private const float SegundosCalentamiento = 2.5f;
        private const float DistanciaPlanoCanvas = 0.35f;
        private const int DineroDeLaPrueba = 1000;
        private const int AnchoCaptura = 1280;
        private const int AltoCaptura = 720;
        private const int ProfundidadCaptura = 24;
        private const int Piernas = (int)RanuraPieza.Piernas;
        private const int BrazoIzquierdo = (int)RanuraPieza.BrazoIzquierdo;
        private const string IdPiernasCuerpoACuerpo1 = "pieza_piernas_cuerpoacuerpo_1";
        private const string IdPiernasSerie = "pieza_piernas_serie_0";

        private Keyboard teclado;
        private Mouse raton;
        private JugadorAPie jugador;
        private ControladorAPie controlador;
        private BancoTaller banco;
        private TallerPiezas taller;
        private EquipoMecha equipo;
        private PantallaPiezasTaller pantalla;
        private ICartera cartera;
        private ServicioInventario inventario;
        private HudAPie hud;
        private readonly MedidorFrames medidor = new MedidorFrames(SegundosCalentamiento);
        private readonly List<string> errores = new List<string>();

        public override void Setup()
        {
            InputSystem.DisableAllEnabledActions();
            base.Setup();
            Registros.LimpiarTodos();
            teclado = InputSystem.AddDevice<Keyboard>();
            raton = InputSystem.AddDevice<Mouse>();
            MapasDeAccionesDePrueba.Reiniciar(teclado, raton);
            errores.Clear();
            medidor.Reiniciar();
            Application.logMessageReceived += RegistrarError;
        }

        public override void TearDown()
        {
            Application.logMessageReceived -= RegistrarError;
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

        private IEnumerator Escenario(int dinero)
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
            banco = Object.FindFirstObjectByType<BancoTaller>();
            hud = Object.FindFirstObjectByType<HudAPie>();
            cartera = Registro<ICartera>.Actual;
            inventario = Registro<ServicioInventario>.Actual;
            Assert.IsNotNull(jugador, "Pruebas_Economia trae el jugador");
            Assert.IsNotNull(banco, "El mecha trae el banco del taller");
            Assert.IsNotNull(cartera, "El jugador trae la cartera");
            Assert.IsNotNull(inventario, "El jugador trae el inventario");
            controlador = jugador.GetComponent<ControladorAPie>();
            taller = banco.GetComponent<TallerPiezas>();
            equipo = banco.GetComponent<EquipoMecha>();
            Assert.IsNotNull(taller, "El mecha trae el taller de piezas");
            Assert.IsNotNull(equipo, "El mecha trae el equipo de piezas");
            pantalla = banco.GetComponentInChildren<PantallaPiezasTaller>(true);
            Assert.IsNotNull(pantalla, "El mecha trae la pantalla de piezas");
            Assert.IsFalse(pantalla.Visible);
            PonerDinero(dinero);
            yield return null;
        }

        private void PonerDinero(int dinero)
        {
            int diferencia = dinero - cartera.Dinero;
            if (diferencia > 0)
            {
                cartera.Abonar(diferencia, MotivoDinero.Otro);
            }
            else if (diferencia < 0)
            {
                cartera.CargarDeuda(-diferencia, MotivoDinero.Otro);
            }
            Assert.AreEqual(dinero, cartera.Dinero);
        }

        // W hasta el banco, como un jugador.
        private IEnumerator IrAlBanco()
        {
            Vector3 punto = Registro<ITallerMecha>.Actual.PuntoInteraccion;
            jugador.Colocar(new Pose(new Vector3(punto.x, AlturaJugador, punto.z + DistanciaInicial), Quaternion.Euler(0f, 180f, 0f)));
            yield return null;
            Press(teclado.wKey);
            float fin = Time.time + MaxSegundosCaminar;
            while (Time.time < fin && jugador.Raiz.position.z > punto.z + MargenLlegada - 0.5f)
            {
                yield return Cuadro();
            }
            Release(teclado.wKey);
            fin = Time.time + SegundosEspera;
            while (Time.time < fin && hud.TextoAvisoInteraccion.text != AvisoPiezas)
            {
                yield return Cuadro();
            }
            Assert.Less(Vector3.Distance(jugador.Raiz.position, punto), 2.5f, "Llegó al banco");
        }

        private object Cuadro()
        {
            medidor.Registrar(Time.unscaledDeltaTime);
            return null;
        }

        private IEnumerator Tecla(KeyControl tecla)
        {
            Press(tecla);
            yield return Cuadro();
            Release(tecla);
            yield return Cuadro();
        }

        private IEnumerator AbrirPantalla()
        {
            yield return IrAlBanco();
            Assert.AreEqual(AvisoPiezas, hud.TextoAvisoInteraccion.text);
            yield return Tecla(teclado.eKey);
            Assert.IsTrue(pantalla.Visible, "E abre PIEZAS DEL MECHA");
            Assert.AreEqual(NivelPantallaPiezas.Ranuras, pantalla.Nivel);
        }

        private IEnumerator Repetir(KeyControl tecla, int veces)
        {
            for (int i = 0; i < veces; i++)
            {
                yield return Tecla(tecla);
            }
        }

        // Desde las ranuras: baja hasta la ranura y entra con Enter.
        private IEnumerator EntrarARanura(int ranura)
        {
            Assert.AreEqual(NivelPantallaPiezas.Ranuras, pantalla.Nivel);
            yield return Repetir(teclado.upArrowKey, 5);
            yield return Repetir(teclado.downArrowKey, ranura);
            Assert.AreEqual(ranura, pantalla.IndiceElegido);
            yield return Tecla(teclado.enterKey);
            Assert.AreEqual(NivelPantallaPiezas.Piezas, pantalla.Nivel, "Enter entra a la ranura");
        }

        private int BuscarFila(string prefijo)
        {
            for (int i = 0; i < pantalla.CantidadFilas; i++)
            {
                if (pantalla.TextoBaseFila(i).StartsWith(prefijo, StringComparison.Ordinal))
                {
                    return i;
                }
            }
            Assert.Fail("No hay fila que empiece con '" + prefijo + "': " + string.Join(" | ", Filas()));
            return -1;
        }

        private List<string> Filas()
        {
            var textos = new List<string>();
            for (int i = 0; i < pantalla.CantidadFilas; i++)
            {
                textos.Add(pantalla.TextoBaseFila(i));
            }
            return textos;
        }

        // Baja a la fila que empieza con ese texto.
        private IEnumerator IrAFila(string prefijo)
        {
            int destino = BuscarFila(prefijo);
            yield return Repetir(teclado.upArrowKey, pantalla.CantidadFilas);
            yield return Repetir(teclado.downArrowKey, destino);
            Assert.AreEqual(destino, pantalla.IndiceElegido);
        }

        // Entra a la ranura, elige la fila y deja la pantalla en las ranuras.
        private IEnumerator Comprar(int ranura, string prefijoFila)
        {
            yield return EntrarARanura(ranura);
            yield return IrAFila(prefijoFila);
            yield return Tecla(teclado.enterKey);
            Assert.AreEqual(NivelPantallaPiezas.Ranuras, pantalla.Nivel, "Elegir vuelve a las ranuras");
        }

        private static string SinEtiquetas(string texto) => Regex.Replace(texto, "<.*?>", string.Empty);

        private DatosPieza Pieza(string id)
        {
            DatosPieza pieza = equipo.Catalogo.Buscar(id);
            Assert.IsNotNull(pieza, "Existe la pieza " + id);
            return pieza;
        }

        [UnityTest]
        public IEnumerator A_ConMilYLiga1_ElBancoOfreceLasPiezasYSeAbrenLas5RanurasDeSerie()
        {
            yield return Escenario(DineroDeLaPrueba);
            Assert.AreEqual(1, TallerPiezas.LigaActual);

            yield return IrAlBanco();
            Assert.AreEqual(AvisoPiezas, hud.TextoAvisoInteraccion.text, "W hasta el banco: el HUD ofrece las piezas");
            yield return Tecla(teclado.eKey);

            Assert.IsTrue(pantalla.Visible);
            Assert.AreEqual("PIEZAS DEL MECHA", pantalla.Titulo);
            Assert.AreEqual(5, pantalla.CantidadFilas);
            for (int i = 0; i < 5; i++)
            {
                StringAssert.Contains("DE SERIE", pantalla.TextoBaseFila(i), "Ranura " + i);
            }
            Assert.AreEqual("BRAZO IZQ. DE SERIE", pantalla.TextoBaseFila(0));
            Assert.AreEqual("PIERNAS DE SERIE", pantalla.TextoBaseFila(3));
            Assert.AreEqual("VIDA 100 · DAÑO 10 · VELOCIDAD 4.0", pantalla.Estadisticas);
            Assert.AreEqual("DINERO $1,000", pantalla.Dinero);
            Assert.AreEqual(0, pantalla.IndiceElegido);
            Assert.IsFalse(controlador.ControlActivo, "La pantalla congela al jugador");

            yield return Tecla(teclado.downArrowKey);
            Assert.AreEqual(1, pantalla.IndiceElegido);
            yield return Repetir(teclado.downArrowKey, 10);
            Assert.AreEqual(4, pantalla.IndiceElegido, "↓ no da la vuelta");
            yield return Repetir(teclado.upArrowKey, 10);
            Assert.AreEqual(0, pantalla.IndiceElegido, "↑ no da la vuelta");
            Assert.IsEmpty(errores, "consola sin errores: " + string.Join(" | ", errores));
        }

        [UnityTest]
        public IEnumerator B_Piernas_ListaNivelIGrisIIYComprarCuerpoACuerpoICobra500()
        {
            yield return Escenario(DineroDeLaPrueba);
            yield return AbrirPantalla();

            yield return Repetir(teclado.downArrowKey, 3);
            Assert.AreEqual(3, pantalla.IndiceElegido);
            yield return Tecla(teclado.enterKey);

            Assert.AreEqual(NivelPantallaPiezas.Piezas, pantalla.Nivel);
            Assert.AreEqual(8, pantalla.CantidadFilas, "4 de nivel I y 4 de nivel II");
            List<string> filas = Filas();
            Assert.AreEqual(4, filas.Count(f => f.EndsWith("COMPRAR · $500", StringComparison.Ordinal)), string.Join(" | ", filas));
            Assert.AreEqual(4, filas.Count(f => f.EndsWith("SE ABRE EN LA LIGA 2", StringComparison.Ordinal)), string.Join(" | ", filas));
            foreach (string rama in new[] { "MILITAR", "TECH", "CUERPO A CUERPO", "FUEGO" })
            {
                Assert.IsTrue(filas.Any(f => f.StartsWith("PIERNAS " + rama + " I · COMPRAR · $500", StringComparison.Ordinal)), rama);
            }
            for (int i = 0; i < 4; i++)
            {
                Color cerrada = pantalla.ColorFila(4 + i);
                Assert.AreEqual(cerrada.r, cerrada.g, 0.05f, "Las de la liga que sigue están en gris");
                Assert.AreEqual(cerrada.g, cerrada.b, 0.06f, "Las de la liga que sigue están en gris");
            }

            yield return IrAFila("PIERNAS CUERPO A CUERPO I ·");
            string diferencia = SinEtiquetas(pantalla.Diferencia);
            Assert.AreEqual("VIDA +10 · VEL +0.25", diferencia);
            StringAssert.Contains("<color=#", pantalla.Diferencia, "Lo que sube va coloreado");
            yield return Tecla(teclado.enterKey);

            Assert.AreEqual(NivelPantallaPiezas.Ranuras, pantalla.Nivel, "Elegir vuelve a las ranuras");
            Assert.AreEqual(500, cartera.Dinero);
            Assert.AreEqual("DINERO $500", pantalla.Dinero);
            Assert.AreEqual("PIERNAS CUERPO A CUERPO I", pantalla.TextoBaseFila(3));
            Assert.AreEqual("VIDA 110 · DAÑO 10 · VELOCIDAD 4.25", pantalla.Estadisticas);
            Assert.AreEqual("PUESTA: PIERNAS CUERPO A CUERPO I", pantalla.Aviso);
            Assert.AreEqual(Pieza(IdPiernasCuerpoACuerpo1), equipo.Puesta(RanuraPieza.Piernas));
            Assert.AreEqual(3, pantalla.IndiceElegido, "Sigue sobre la ranura que cambió");
            Assert.IsEmpty(errores, "consola sin errores: " + string.Join(" | ", errores));
        }

        [UnityTest]
        public IEnumerator B_LaDiferenciaMarcaLoQueBajaConMenos()
        {
            yield return Escenario(DineroDeLaPrueba);
            yield return AbrirPantalla();
            yield return Comprar(BrazoIzquierdo, "BRAZO IZQ. MILITAR I ·");
            Assert.AreEqual("VIDA 102 · DAÑO 13 · VELOCIDAD 4.0", pantalla.Estadisticas);

            yield return EntrarARanura(BrazoIzquierdo);
            yield return IrAFila("BRAZO IZQ. TECH I ·");

            StringAssert.StartsWith("VIDA +2 · DAÑO −1", SinEtiquetas(pantalla.Diferencia), "Tech I contra militar I: más vida, menos daño");
            StringAssert.Contains("<color=#", pantalla.Diferencia);
            Assert.IsEmpty(errores, "consola sin errores: " + string.Join(" | ", errores));
        }

        [UnityTest]
        public IEnumerator I_LaDiferenciaMuestraLaHabilidadQueCambia_YElEnfriamiento()
        {
            yield return Escenario(DineroDeLaPrueba);
            yield return AbrirPantalla();

            // De serie (sin habilidad) a militar I: trae el misil y no toca los enfriamientos.
            yield return EntrarARanura(BrazoIzquierdo);
            yield return IrAFila("BRAZO IZQ. MILITAR I ·");
            string haciaMisil = SinEtiquetas(pantalla.Diferencia);
            StringAssert.Contains("HABILIDAD 1 · MISIL I · MISILES", haciaMisil);
            StringAssert.DoesNotContain("ENFRIAMIENTO", haciaMisil);
            StringAssert.Contains("<color=#", pantalla.Diferencia, "La habilidad va coloreada");
            yield return Tecla(teclado.enterKey);
            Assert.AreEqual(NivelPantallaPiezas.Ranuras, pantalla.Nivel);

            // De militar I a tech I: cambia de habilidad y acorta 3 % los enfriamientos.
            DatosPieza tech = Pieza("pieza_brazoizq_tech_1");
            yield return EntrarARanura(BrazoIzquierdo);
            yield return IrAFila("BRAZO IZQ. TECH I ·");
            string haciaTech = SinEtiquetas(pantalla.Diferencia);
            StringAssert.Contains("HABILIDAD 1 · " + tech.Habilidad.Nombre + " I · " + tech.Municion.Nombre.ToUpperInvariant(), haciaTech);
            StringAssert.Contains("ENFRIAMIENTO −3 %", haciaTech);

            // De militar I a la pieza de serie que quedó en el inventario: se pierde la habilidad.
            yield return IrAFila("BRAZO IZQ. DE SERIE");
            string haciaSerie = SinEtiquetas(pantalla.Diferencia);
            StringAssert.Contains("SIN HABILIDAD", haciaSerie);
            Assert.IsEmpty(errores, "consola sin errores: " + string.Join(" | ", errores));
        }

        [UnityTest]
        public IEnumerator C_SinDinero_DiceNoTeAlcanzaYNoCambiaNada_ConLaGrisNoHaceNada()
        {
            yield return Escenario(DineroDeLaPrueba);
            yield return AbrirPantalla();
            yield return EntrarARanura(Piernas);
            DatosPieza serie = Pieza(IdPiernasSerie);

            PonerDinero(300);
            yield return Cuadro();
            yield return Cuadro();
            yield return IrAFila("PIERNAS MILITAR I ·");
            yield return Tecla(teclado.enterKey);

            Assert.AreEqual("NO TE ALCANZA", pantalla.Aviso);
            Assert.AreEqual(NivelPantallaPiezas.Piezas, pantalla.Nivel, "Sigue en la lista");
            Assert.AreEqual(300, cartera.Dinero);
            Assert.AreEqual(serie, equipo.Puesta(RanuraPieza.Piernas));
            Assert.AreEqual(0, inventario.Cantidad(serie));

            yield return IrAFila("PIERNAS MILITAR II ·");
            StringAssert.EndsWith("SE ABRE EN LA LIGA 2", pantalla.TextoBaseFila(pantalla.IndiceElegido));
            yield return Tecla(teclado.enterKey);

            Assert.AreEqual(NivelPantallaPiezas.Piezas, pantalla.Nivel);
            Assert.AreEqual(string.Empty, pantalla.Aviso, "La gris no hace nada");
            Assert.AreEqual(300, cartera.Dinero);
            Assert.AreEqual(serie, equipo.Puesta(RanuraPieza.Piernas));
            Assert.IsEmpty(errores, "consola sin errores: " + string.Join(" | ", errores));
        }

        [UnityTest]
        public IEnumerator D_PonerLaDeSerieDelInventario_NoCobra_YLaCompradaQuedaEnElInventario()
        {
            yield return Escenario(DineroDeLaPrueba);
            yield return AbrirPantalla();
            yield return Comprar(Piernas, "PIERNAS CUERPO A CUERPO I ·");
            Assert.AreEqual(500, cartera.Dinero);

            yield return EntrarARanura(Piernas);
            StringAssert.EndsWith("PONER · TIENES 1", pantalla.TextoBaseFila(0));
            StringAssert.StartsWith("PIERNAS DE SERIE", pantalla.TextoBaseFila(0));
            Assert.AreEqual("VIDA −10 · VEL −0.25", SinEtiquetas(pantalla.Diferencia));
            yield return Tecla(teclado.enterKey);

            Assert.AreEqual(NivelPantallaPiezas.Ranuras, pantalla.Nivel);
            Assert.AreEqual(500, cartera.Dinero, "Poner del inventario no cobra");
            Assert.AreEqual("PIERNAS DE SERIE", pantalla.TextoBaseFila(3));
            Assert.AreEqual("PUESTA: PIERNAS DE SERIE", pantalla.Aviso);
            Assert.AreEqual("VIDA 100 · DAÑO 10 · VELOCIDAD 4.0", pantalla.Estadisticas);
            Assert.AreEqual(1, inventario.Cantidad(Pieza(IdPiernasCuerpoACuerpo1)));
            Assert.IsEmpty(errores, "consola sin errores: " + string.Join(" | ", errores));
        }

        [UnityTest]
        public IEnumerator D_SiLaViejaNoCabeEnElInventario_AvisaQueLaVendio()
        {
            yield return Escenario(DineroDeLaPrueba);
            DatosPieza serie = Pieza(IdPiernasSerie);
            Assert.AreEqual(2, inventario.Agregar(serie, 2), "El inventario ya tiene el máximo de piernas de serie");
            yield return AbrirPantalla();

            yield return EntrarARanura(Piernas);
            Assert.AreEqual(0, pantalla.IndiceElegido);
            yield return IrAFila("PIERNAS CUERPO A CUERPO I ·");
            yield return Tecla(teclado.enterKey);

            Assert.AreEqual("PUESTA: PIERNAS CUERPO A CUERPO I · +$100 · VENDISTE PIERNAS DE SERIE", pantalla.Aviso);
            Assert.AreEqual(DineroDeLaPrueba - 500 + 100, cartera.Dinero);
            Assert.AreEqual(2, inventario.Cantidad(serie));
            Assert.IsEmpty(errores, "consola sin errores: " + string.Join(" | ", errores));
        }

        [UnityTest]
        public IEnumerator E_EscRegresaYCierra_ElJugadorVuelveAMoverse_YNadaReabreLaPantalla()
        {
            yield return Escenario(DineroDeLaPrueba);
            yield return AbrirPantalla();
            yield return EntrarARanura(Piernas);

            yield return Tecla(teclado.escapeKey);
            Assert.IsTrue(pantalla.Visible);
            Assert.AreEqual(NivelPantallaPiezas.Ranuras, pantalla.Nivel, "Esc desde las piezas regresa a las ranuras");
            Assert.AreEqual(3, pantalla.IndiceElegido);
            Assert.AreEqual(DineroDeLaPrueba, cartera.Dinero);

            yield return Tecla(teclado.rightArrowKey);
            Assert.AreEqual(NivelPantallaPiezas.Piezas, pantalla.Nivel, "→ entra");
            yield return Tecla(teclado.leftArrowKey);
            Assert.AreEqual(NivelPantallaPiezas.Ranuras, pantalla.Nivel, "← regresa");

            yield return Tecla(teclado.escapeKey);
            Assert.IsFalse(pantalla.Visible, "Esc otra vez cierra");
            for (int i = 0; i < 10; i++)
            {
                yield return Cuadro();
            }
            Assert.IsFalse(pantalla.Visible, "Nada la reabre");
            Assert.IsTrue(controlador.ControlActivo, "El jugador vuelve a moverse");
            Assert.AreEqual(DineroDeLaPrueba, cartera.Dinero);

            Vector3 antes = jugador.Raiz.position;
            Press(teclado.aKey);
            float fin = Time.time + 0.5f;
            while (Time.time < fin)
            {
                yield return Cuadro();
            }
            Release(teclado.aKey);
            yield return Cuadro();
            Assert.Greater(Vector3.Distance(antes, jugador.Raiz.position), 0.1f, "A mueve al jugador otra vez");
            Assert.IsFalse(pantalla.Visible);
            Assert.IsEmpty(errores, "consola sin errores: " + string.Join(" | ", errores));
        }

        [UnityTest]
        public IEnumerator F_ConRentaVencidaSinDinero_ElBancoDiceLoDeHoyYLaPantallaNoSeAbre()
        {
            yield return Escenario(DineroDeLaPrueba);
            IRenta renta = Registro<IRenta>.Actual;
            Assert.IsNotNull(renta, "El jugador trae la renta");
            ((IGuardable)renta).Cargar(AdeudoDeRenta);
            PonerDinero(0);
            Assert.IsTrue(renta.TallerBloqueado);
            yield return Cuadro();

            Vector3 punto = Registro<ITallerMecha>.Actual.PuntoInteraccion;
            jugador.Colocar(new Pose(new Vector3(punto.x, AlturaJugador, punto.z + MargenLlegada), Quaternion.Euler(0f, 180f, 0f)));
            yield return Cuadro();
            yield return Cuadro();
            Assert.AreEqual(EstadoBanco.Bloqueado, banco.Estado);
            yield return Tecla(teclado.eKey);

            Assert.IsFalse(pantalla.Visible, "Con la renta vencida la pantalla no se abre");
            Assert.AreEqual(0, cartera.Dinero);
            Assert.IsEmpty(errores, "consola sin errores: " + string.Join(" | ", errores));
        }

        [UnityTest]
        public IEnumerator H_DesdeElBancoConDanio_PiezasDelMechaAbreLaPantallaSinEntrarSola()
        {
            yield return Escenario(DineroDeLaPrueba);
            VidaMecha vida = banco.GetComponent<VidaMecha>();
            vida.FijarVida(40f);
            yield return IrAlBancoConDanio();

            yield return Tecla(teclado.eKey);
            PantallaBancoTaller reparar = banco.GetComponentInChildren<PantallaBancoTaller>(true);
            Assert.IsTrue(reparar.Visible);
            yield return Repetir(teclado.downArrowKey, reparar.CantidadOpciones - 1);
            Assert.AreEqual("PIEZAS DEL MECHA", reparar.TextoBaseOpcion(reparar.IndiceElegido));
            yield return Tecla(teclado.enterKey);

            Assert.IsFalse(reparar.Visible);
            Assert.IsTrue(pantalla.Visible, "PIEZAS DEL MECHA abre la pantalla de piezas");
            Assert.AreEqual(NivelPantallaPiezas.Ranuras, pantalla.Nivel, "El mismo Enter no entra a una ranura");
            Assert.AreEqual(DineroDeLaPrueba, cartera.Dinero, "No repara ni cobra");
            for (int i = 0; i < 8; i++)
            {
                yield return Cuadro();
                Assert.IsFalse(controlador.ControlActivo, "El jugador sigue congelado mientras la pantalla está abierta");
            }
            Assert.AreEqual(40f, vida.Actual, 0.01f);
            yield return Tecla(teclado.escapeKey);
            for (int i = 0; i < 4; i++)
            {
                yield return Cuadro();
            }
            Assert.IsFalse(pantalla.Visible);
            Assert.IsTrue(controlador.ControlActivo, "Al cerrar el control vuelve");
            Assert.IsEmpty(errores, "consola sin errores: " + string.Join(" | ", errores));
        }

        private IEnumerator IrAlBancoConDanio()
        {
            Vector3 punto = Registro<ITallerMecha>.Actual.PuntoInteraccion;
            jugador.Colocar(new Pose(new Vector3(punto.x, AlturaJugador, punto.z + MargenLlegada), Quaternion.Euler(0f, 180f, 0f)));
            float fin = Time.time + SegundosEspera;
            while (Time.time < fin && !hud.TextoAvisoInteraccion.text.StartsWith("E ·", StringComparison.Ordinal))
            {
                yield return Cuadro();
            }
            StringAssert.StartsWith("E ·", hud.TextoAvisoInteraccion.text);
        }

        [UnityTest]
        public IEnumerator G_AbrirYCerrarVariasVeces_SinTironesYSinErroresEnConsola()
        {
            yield return Escenario(DineroDeLaPrueba);
            float fin = Time.time + SegundosCalentamiento;
            while (Time.time < fin)
            {
                yield return Cuadro();
            }
            yield return IrAlBanco();
            for (int vuelta = 0; vuelta < 3; vuelta++)
            {
                yield return Tecla(teclado.eKey);
                Assert.IsTrue(pantalla.Visible);
                yield return EntrarARanura(Piernas);
                yield return Tecla(teclado.escapeKey);
                yield return Tecla(teclado.escapeKey);
                Assert.IsFalse(pantalla.Visible);
                for (int i = 0; i < 5; i++)
                {
                    yield return Cuadro();
                }
            }
            Debug.Log("[Prueba T-169] MedidorFrames: " + medidor.Resumen());
            Assert.IsTrue(medidor.Cumple, "Sin tirones de frames: " + medidor.Resumen());
            Assert.IsEmpty(errores, "consola sin errores: " + string.Join(" | ", errores));
        }

        [UnityTest]
        public IEnumerator Captura_RanurasYPiezas_GuardaT169UnoYDos()
        {
            if (Environment.GetEnvironmentVariable(VariableCaptura) != "1")
            {
                Assert.Ignore("Sin " + VariableCaptura + "=1 no se regraban las capturas.");
            }
            yield return Escenario(DineroDeLaPrueba + 3000);
            yield return AbrirPantalla();
            yield return Comprar(BrazoIzquierdo, "BRAZO IZQ. MILITAR I ·");
            yield return Comprar(Piernas, "PIERNAS CUERPO A CUERPO I ·");
            yield return Cuadro();
            Capturar(RutaCaptura1);

            yield return EntrarARanura(BrazoIzquierdo);
            yield return IrAFila("BRAZO IZQ. TECH I ·");
            yield return Cuadro();
            Capturar(RutaCaptura2);
        }

        [UnityTest]
        public IEnumerator Captura_PiezaConHabilidad_GuardaT177Tres()
        {
            if (Environment.GetEnvironmentVariable("CAPTURAR_T177") != "1")
            {
                Assert.Ignore("Sin CAPTURAR_T177=1 no se regraba la captura.");
            }
            yield return Escenario(DineroDeLaPrueba);
            yield return AbrirPantalla();
            yield return Comprar(BrazoIzquierdo, "BRAZO IZQ. MILITAR I ·");
            yield return EntrarARanura(BrazoIzquierdo);
            yield return IrAFila("BRAZO IZQ. TECH I ·");
            yield return Cuadro();
            Capturar("docs/capturas/T-177-3.png");
        }

        // Los lienzos en Overlay no salen en una cámara a textura: se pasan un instante a ScreenSpaceCamera.
        private void Capturar(string ruta)
        {
            Camera camara = jugador.Camara;
            var movidos = new List<Canvas>();
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
            RenderTexture destino = new RenderTexture(AnchoCaptura, AltoCaptura, ProfundidadCaptura);
            Texture2D imagen = new Texture2D(AnchoCaptura, AltoCaptura, TextureFormat.RGB24, false);
            RenderTexture anterior = RenderTexture.active;
            camara.targetTexture = destino;
            camara.Render();
            RenderTexture.active = destino;
            imagen.ReadPixels(new Rect(0f, 0f, AnchoCaptura, AltoCaptura), 0, 0);
            imagen.Apply();
            camara.targetTexture = null;
            RenderTexture.active = anterior;
            foreach (Canvas lienzo in movidos)
            {
                lienzo.renderMode = RenderMode.ScreenSpaceOverlay;
            }
            string completa = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ruta));
            Directory.CreateDirectory(Path.GetDirectoryName(completa));
            File.WriteAllBytes(completa, imagen.EncodeToPNG());
            Object.Destroy(imagen);
            destino.Release();
            Object.Destroy(destino);
        }
    }
}
