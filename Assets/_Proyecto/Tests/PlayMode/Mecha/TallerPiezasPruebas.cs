using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MechaCyberpunk.Combate.Vida;
using MechaCyberpunk.Core;
using MechaCyberpunk.Economia;
using MechaCyberpunk.Mecha;
using MechaCyberpunk.Mecha.Estado;
using MechaCyberpunk.Mecha.Llamado;
using MechaCyberpunk.Mecha.Montaje;
using MechaCyberpunk.Mecha.Taller;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.UI.Hud;
using MechaCyberpunk.UI.Taller;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Mecha
{
    /// <summary>
    /// T-168: comprar y cambiar piezas en el banco del taller, en Pruebas_Calle con el mecha real. Las elecciones son por código;
    /// el aviso del banco y la pantalla se comprueban con la tecla E simulada.
    /// </summary>
    public class TallerPiezasPruebas : InputTestFixture
    {
        private const string RutaCalle = "Assets/_Proyecto/Escenas/Tony/Pruebas_Calle.unity";
        private const int FramesEstables = 10;
        private const int MaxFramesCalentamiento = 1000;
        private const float DeltaTimeEstable = 0.05f;
        private const float MaxSegundosCaminar = 10f;
        private const float MargenLlegada = 2f;
        private const float VidaDanada = 35f;
        private const int DineroInicial = 500;
        private const int DineroDeLaPrueba = 1000;

        private sealed class LigasDePrueba : IProgresoLigas
        {
            public LigasDePrueba(int liga) => LigaActual = liga;
            public int CantidadLigas => 4;
            public int LigaActual { get; }
            public int CampeonesVencidos => 0;
            public string NombreLiga(int liga) => "Liga " + liga;
            public event Action Cambio { add { } remove { } }
            public event Action<int> CampeonVencido { add { } remove { } }
        }

        private Keyboard teclado;
        private JugadorAPie jugador;
        private MontajeMecha montaje;
        private LlamadoMecha llamado;
        private VidaMecha vida;
        private BancoTaller banco;
        private TallerPiezas taller;
        private EquipoMecha equipo;
        private ICartera cartera;
        private ServicioInventario inventario;
        private HudAPie hud;
        private readonly List<(int, MotivoDinero)> cambios = new List<(int, MotivoDinero)>();

        public override void Setup()
        {
            base.Setup();
            Registros.LimpiarTodos();
            teclado = InputSystem.AddDevice<Keyboard>();
            InputSystem.AddDevice<Mouse>();
            cambios.Clear();
        }

        public override void TearDown()
        {
            Registros.LimpiarTodos();
            base.TearDown();
        }

        [UnityTearDown]
        public IEnumerator DescargarEscena()
        {
            foreach (GameObject raiz in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                UnityEngine.Object.Destroy(raiz);
            }
            yield return null;
            Registros.LimpiarTodos();
        }

        private static IEnumerator Calentar()
        {
            int estables = 0;
            int intentos = 0;
            while (estables < FramesEstables && intentos < MaxFramesCalentamiento)
            {
                yield return null;
                intentos++;
                estables = Time.deltaTime < DeltaTimeEstable ? estables + 1 : 0;
            }
        }

        private IEnumerator CargarCalle(int dinero = DineroInicial)
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(RutaCalle, new LoadSceneParameters(LoadSceneMode.Single));
            yield return Calentar();
            jugador = UnityEngine.Object.FindFirstObjectByType<JugadorAPie>();
            montaje = UnityEngine.Object.FindFirstObjectByType<MontajeMecha>();
            llamado = montaje.GetComponent<LlamadoMecha>();
            vida = montaje.GetComponent<VidaMecha>();
            banco = montaje.GetComponent<BancoTaller>();
            taller = montaje.GetComponent<TallerPiezas>();
            equipo = montaje.GetComponent<EquipoMecha>();
            hud = UnityEngine.Object.FindFirstObjectByType<HudAPie>();
            cartera = Registro<ICartera>.Actual;
            inventario = Registro<ServicioInventario>.Actual;
            Assert.IsNotNull(taller, "MechaEstacionado trae el taller de piezas");
            Assert.IsNotNull(equipo);
            Assert.IsNotNull(cartera, "El jugador trae la cartera");
            Assert.IsNotNull(inventario, "El jugador trae el inventario");
            Assert.IsTrue(banco.Registrado);
            if (dinero > DineroInicial) cartera.Abonar(dinero - DineroInicial, MotivoDinero.Otro);
            else if (dinero < DineroInicial) cartera.CargarDeuda(DineroInicial - dinero, MotivoDinero.Otro);
            Assert.AreEqual(dinero, cartera.Dinero);
            cartera.Cambio += (diferencia, motivo) => cambios.Add((diferencia, motivo));
            yield return null;
        }

        private IEnumerator IrAlBanco()
        {
            ITallerMecha puntoTaller = Registro<ITallerMecha>.Actual;
            Vector3 punto = puntoTaller.PuntoInteraccion;
            jugador.Colocar(new Pose(new Vector3(punto.x, 0.05f, punto.z + 4f), Quaternion.Euler(0f, 180f, 0f)));
            yield return null;
            Press(teclado.wKey);
            float fin = Time.time + MaxSegundosCaminar;
            while (Time.time < fin && jugador.Raiz.position.z > punto.z + MargenLlegada - 0.5f)
            {
                yield return null;
            }
            Release(teclado.wKey);
            yield return null;
            Assert.Less(Vector3.Distance(jugador.Raiz.position, punto), 2.5f, "Llegó al banco");
        }

        private IEnumerator Tecla(KeyControl tecla)
        {
            Press(tecla);
            yield return null;
            Release(tecla);
            yield return null;
        }

        private OpcionPieza Opcion(RanuraPieza ranura, string id)
        {
            List<OpcionPieza> opciones = taller.Opciones(ranura);
            OpcionPieza opcion = opciones.FirstOrDefault(o => o.Pieza != null && o.Pieza.Id == id);
            Assert.IsNotNull(opcion.Pieza, "Hay una opción para " + id);
            return opcion;
        }

        private DatosPieza Pieza(string id)
        {
            DatosPieza p = equipo.Catalogo.Buscar(id);
            Assert.IsNotNull(p, "Existe la pieza " + id);
            return p;
        }

        [UnityTest]
        public IEnumerator Comprar_PiernasCuerpoACuerpoI_CobraLasPonePasaLaDeSerieAlInventarioYSePuedeVolver()
        {
            yield return CargarCalle(DineroDeLaPrueba);
            DatosPieza serie = Pieza("pieza_piernas_serie_0");
            DatosPieza comprada = Pieza("pieza_piernas_cuerpoacuerpo_1");
            float vidaAntes = vida.Maxima;
            Assert.AreEqual(serie, equipo.Puesta(RanuraPieza.Piernas));

            ResultadoTallerPiezas compra = taller.Elegir(Opcion(RanuraPieza.Piernas, comprada.Id));

            Assert.IsTrue(compra.Hecho);
            Assert.AreEqual(serie, compra.Quitada);
            Assert.IsFalse(compra.SeVendio);
            Assert.AreEqual(DineroDeLaPrueba - 500, cartera.Dinero, "Costó $500");
            Assert.AreEqual((-500, MotivoDinero.Compra), cambios.Single());
            Assert.AreEqual(comprada, equipo.Puesta(RanuraPieza.Piernas));
            Assert.AreEqual(1, inventario.Cantidad(serie), "La de serie quedó en el inventario");
            Assert.AreEqual(vidaAntes + comprada.BonoVida, vida.Maxima, 1e-3f, "El mecha usa las piezas puestas");

            ResultadoTallerPiezas vuelta = taller.Elegir(Opcion(RanuraPieza.Piernas, serie.Id));

            Assert.IsTrue(vuelta.Hecho);
            Assert.AreEqual(serie, equipo.Puesta(RanuraPieza.Piernas));
            Assert.AreEqual(0, inventario.Cantidad(serie));
            Assert.AreEqual(1, inventario.Cantidad(comprada), "Las de cuerpo a cuerpo quedaron en el inventario");
            Assert.AreEqual(DineroDeLaPrueba - 500, cartera.Dinero, "Poner del inventario es gratis");
            Assert.AreEqual(1, cambios.Count, "Y no movió dinero");
            Assert.AreEqual(vidaAntes, vida.Maxima, 1e-3f);
        }

        [UnityTest]
        public IEnumerator Comprar_ConLaPiezaQuitadaSinLugar_PagaSuValorDeVenta()
        {
            yield return CargarCalle(DineroDeLaPrueba);
            DatosPieza serie = Pieza("pieza_piernas_serie_0");
            Assert.AreEqual(2, inventario.Agregar(serie, 2), "El inventario ya tiene el máximo de piernas de serie");
            cambios.Clear();

            ResultadoTallerPiezas compra = taller.Elegir(Opcion(RanuraPieza.Piernas, "pieza_piernas_cuerpoacuerpo_1"));

            Assert.IsTrue(compra.Hecho);
            Assert.IsTrue(compra.SeVendio);
            Assert.AreEqual(100, compra.Vendida);
            Assert.AreEqual(2, inventario.Cantidad(serie), "No cupo");
            Assert.AreEqual(DineroDeLaPrueba - 500 + 100, cartera.Dinero);
            CollectionAssert.AreEqual(new[] { (-500, MotivoDinero.Compra), (100, MotivoDinero.Venta) }, cambios);
        }

        [UnityTest]
        public IEnumerator Comprar_SinDinero_NoCambiaNada()
        {
            yield return CargarCalle(400);
            DatosPieza serie = Pieza("pieza_piernas_serie_0");
            OpcionPieza opcion = Opcion(RanuraPieza.Piernas, "pieza_piernas_cuerpoacuerpo_1");
            Assert.IsFalse(opcion.PuedeElegir);
            Assert.AreEqual(MotivoNoElegible.NoAlcanza, opcion.Motivo);

            ResultadoTallerPiezas resultado = taller.Elegir(opcion);

            Assert.IsFalse(resultado.Hecho);
            Assert.AreEqual(400, cartera.Dinero);
            Assert.AreEqual(serie, equipo.Puesta(RanuraPieza.Piernas));
            Assert.AreEqual(0, inventario.Cantidad(serie));
            Assert.AreEqual(0, cambios.Count);
        }

        [UnityTest]
        public IEnumerator Comprar_ConLaOpcionVieja_SiYaNoAlcanza_NoCobra()
        {
            yield return CargarCalle(DineroDeLaPrueba);
            OpcionPieza opcion = Opcion(RanuraPieza.Piernas, "pieza_piernas_cuerpoacuerpo_1");
            Assert.IsTrue(opcion.PuedeElegir);
            cartera.CargarDeuda(700, MotivoDinero.Otro);
            cambios.Clear();

            ResultadoTallerPiezas resultado = taller.Elegir(opcion);

            Assert.IsFalse(resultado.Hecho, "Con $300 la opción vieja ya no vale");
            Assert.AreEqual(300, cartera.Dinero);
            Assert.AreEqual(0, cambios.Count);
        }

        [UnityTest]
        public IEnumerator Liga_SinServicioEsLiga1_ConServicioSeAbrenMasNiveles()
        {
            yield return CargarCalle(5000);
            foreach (IProgresoLigas real in Registro<IProgresoLigas>.Todos.ToList())
            {
                Registro<IProgresoLigas>.Quitar(real);
            }
            Assert.IsNull(Registro<IProgresoLigas>.Actual, "Sin servicio de ligas registrado");
            Assert.AreEqual(1, TallerPiezas.LigaActual);
            OpcionPieza nivel2 = Opcion(RanuraPieza.Piernas, "pieza_piernas_cuerpoacuerpo_2");
            Assert.AreEqual(TipoOpcionPieza.Bloqueada, nivel2.Tipo);
            Assert.AreEqual("SE ABRE EN LA LIGA 2", nivel2.Texto);
            Assert.IsFalse(taller.Elegir(nivel2).Hecho);
            Assert.AreEqual(5000, cartera.Dinero);
            Assert.IsFalse(taller.Opciones(RanuraPieza.Piernas).Any(o => o.Pieza.Nivel >= 3));

            Registro<IProgresoLigas>.Agregar(new LigasDePrueba(3));

            Assert.AreEqual(3, TallerPiezas.LigaActual);
            OpcionPieza ahora = Opcion(RanuraPieza.Piernas, "pieza_piernas_cuerpoacuerpo_3");
            Assert.AreEqual(TipoOpcionPieza.Comprar, ahora.Tipo);
            Assert.IsTrue(taller.Elegir(ahora).Hecho);
            Assert.AreEqual(5000 - 1200, cartera.Dinero, "Nivel III cuesta $1,200");
        }

        [UnityTest]
        public IEnumerator ConRentaVencida_NoSeCompraNiSePone()
        {
            yield return CargarCalle(DineroDeLaPrueba);
            DatosPieza serie = Pieza("pieza_piernas_serie_0");
            DatosPieza otras = Pieza("pieza_piernas_tech_1");
            Assert.AreEqual(1, inventario.Agregar(otras, 1));
            IRenta renta = Registro<IRenta>.Actual;
            Assert.IsNotNull(renta);
            ((IGuardable)renta).Cargar("{\"diaProximoCobro\":15,\"adeudo\":500}");
            Assert.IsTrue(renta.TallerBloqueado);
            cambios.Clear();

            List<OpcionPieza> opciones = taller.Opciones(RanuraPieza.Piernas);
            Assert.IsTrue(opciones.Count > 0);
            Assert.IsTrue(opciones.All(o => !o.PuedeElegir), "Nada se elige con el taller bloqueado");
            Assert.IsFalse(taller.Elegir(Opcion(RanuraPieza.Piernas, "pieza_piernas_cuerpoacuerpo_1")).Hecho);
            Assert.IsFalse(taller.Elegir(Opcion(RanuraPieza.Piernas, otras.Id)).Hecho);

            Assert.AreEqual(serie, equipo.Puesta(RanuraPieza.Piernas));
            Assert.AreEqual(1, inventario.Cantidad(otras));
            Assert.AreEqual(DineroDeLaPrueba, cartera.Dinero);
            Assert.AreEqual(0, cambios.Count);
        }

        [UnityTest]
        public IEnumerator ConElMechaEnLaCalle_NoHaceNada()
        {
            yield return CargarCalle(DineroDeLaPrueba);
            yield return IrAlBanco();
            Assert.IsTrue(taller.Disponible);
            yield return Tecla(teclado.mKey);
            float fin = Time.time + llamado.Config.SegundosLlegada + MargenLlegada;
            while (Time.time < fin && llamado.Estado != EstadoLlamado.EnCalle)
            {
                yield return null;
            }
            Assert.AreEqual(EstadoLlamado.EnCalle, llamado.Estado);
            yield return null;

            Assert.IsFalse(taller.Disponible);
            Assert.AreEqual(0, taller.Opciones(RanuraPieza.Piernas).Count);
            Assert.IsFalse(taller.Elegir(new OpcionPieza(TipoOpcionPieza.Comprar, "x", Pieza("pieza_piernas_cuerpoacuerpo_1"), 500, true,
                MotivoNoElegible.Ninguno, 0f, 0f, 0f)).Hecho);
            Assert.AreEqual(DineroDeLaPrueba, cartera.Dinero);
        }

        [UnityTest]
        public IEnumerator Banco_SinDanio_DiceETallerPiezasYAvisaAlUsarlo()
        {
            yield return CargarCalle();
            int avisos = 0;
            banco.PiezasPedidas += () => avisos++;
            yield return IrAlBanco();

            Assert.AreEqual(EstadoBanco.Piezas, banco.Estado);
            Assert.AreEqual("E · Taller · piezas", hud.TextoAvisoInteraccion.text);
            yield return Tecla(teclado.eKey);

            Assert.AreEqual(1, avisos, "E avisa a la pantalla de piezas");
            Assert.AreEqual(DineroInicial, cartera.Dinero);
            PantallaBancoTaller pantalla = banco.GetComponentInChildren<PantallaBancoTaller>(true);
            Assert.IsFalse(pantalla.Visible, "La pantalla de reparar no se abre sin daño");
        }

        [UnityTest]
        public IEnumerator Banco_SinNadieQueEscuche_ElUsoNoHaceNada()
        {
            yield return CargarCalle();
            yield return IrAlBanco();

            yield return Tecla(teclado.eKey);

            Assert.AreEqual(DineroInicial, cartera.Dinero);
            Assert.AreEqual(vida.Maxima, vida.Actual, 1e-3f);
        }

        [UnityTest]
        public IEnumerator Banco_ConDanio_LasOpcionesTerminanEnPiezasDelMecha()
        {
            yield return CargarCalle();
            vida.FijarVida(VidaDanada);
            int avisos = 0;
            banco.PiezasPedidas += () => avisos++;
            yield return IrAlBanco();

            Assert.AreEqual("E · Reparar mecha · $650", hud.TextoAvisoInteraccion.text);
            yield return Tecla(teclado.eKey);
            PantallaBancoTaller pantalla = banco.GetComponentInChildren<PantallaBancoTaller>(true);
            Assert.IsTrue(pantalla.Visible);
            int ultima = pantalla.CantidadOpciones - 1;
            Assert.AreEqual("PIEZAS DEL MECHA", pantalla.TextoBaseOpcion(ultima));
            Assert.AreEqual(banco.CalcularOpciones().Count, pantalla.CantidadOpciones);
            Assert.AreEqual(TipoReparacion.Piezas, banco.CalcularOpciones().Last().Tipo);

            for (int i = 0; i < ultima; i++)
            {
                yield return Tecla(teclado.downArrowKey);
            }
            yield return Tecla(teclado.enterKey);

            Assert.AreEqual(1, avisos, "Elegir PIEZAS DEL MECHA avisa");
            Assert.AreEqual(DineroInicial, cartera.Dinero, "No cobra");
            Assert.AreEqual(VidaDanada, vida.Actual, 1e-3f, "No repara");
        }
    }
}
