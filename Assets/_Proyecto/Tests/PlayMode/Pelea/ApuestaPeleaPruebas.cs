using System.Collections;
using System.Collections.Generic;
using MechaCyberpunk.Combate.Vida;
using MechaCyberpunk.Core;
using MechaCyberpunk.Economia;
using MechaCyberpunk.Pelea.Datos;
using MechaCyberpunk.Pelea.Flujo;
using MechaCyberpunk.Progresion.Pruebas;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Pelea
{
    /// <summary>
    /// Apuestas de T-144 con el flujo real: arena de prueba, MechaJugador, ServiciosEconomia y FlujoPeleaCasera. El daño es real
    /// (VidaMecha.RecibirDanio) y solo se usa para rematar a quien debe caer.
    /// </summary>
    public class ApuestaPeleaPruebas : InputTestFixture
    {
        private const string RutaArena = "Assets/_Proyecto/Pelea/Pruebas/ArenaDePrueba.prefab";
        private const string RutaFlujoCasera = "Assets/_Proyecto/Pelea/Prefabs/FlujoPeleaCasera.prefab";
        private const string RutaJugador = "Assets/_Proyecto/Mecha/Prefabs/MechaJugador.prefab";
        private const string RutaServicios = "Assets/_Proyecto/Economia/Prefabs/ServiciosEconomia.prefab";
        private const string RutaBrazo = "Assets/_Proyecto/Economia/Datos/DatosObjeto_BrazoChatarra.asset";
        private const int FramesEstables = 10;
        private const int MaxFramesCalentamiento = 1000;
        private const float DeltaTimeEstable = 0.05f;
        private const float SegundosPorNumero = 0.2f;
        private const float EsperaMaxima = 15f;
        private const float DanioMortal = 100000f;
        private const int DineroInicial = 500;
        private const int PremioCasera = 600;
        private static readonly Vector3 PosicionArena = new Vector3(1000f, 0f, 0f);

        private Keyboard teclado;
        private readonly List<GameObject> creados = new List<GameObject>();
        private readonly List<string> errores = new List<string>();
        private readonly List<int> resueltas = new List<int>();
        private ConfigFlujoPelea config;
        private ICartera cartera;
        private ServicioApuestas apuestas;
        private ServicioInventario inventario;
        private DatosObjeto brazo;
        private DirectorPelea director;
        private GameObject flujoCreado;

        public override void Setup()
        {
            base.Setup();
            Registros.LimpiarTodos();
            EventosPelea.LimpiarSuscriptores();
            teclado = InputSystem.AddDevice<Keyboard>();
            InputSystem.AddDevice<Mouse>();
            MapasDeAccionesDePrueba.Reiniciar(teclado);
            errores.Clear();
            resueltas.Clear();
            cartera = null;
            apuestas = null;
            inventario = null;
            director = null;
            flujoCreado = null;
            Application.logMessageReceived += RegistrarLog;
        }

        public override void TearDown()
        {
            Application.logMessageReceived -= RegistrarLog;
            foreach (GameObject objeto in creados)
            {
                if (objeto != null) Object.Destroy(objeto);
            }
            creados.Clear();
            if (config != null) Object.Destroy(config);
            Registros.LimpiarTodos();
            EventosPelea.LimpiarSuscriptores();
            MapasDeAccionesDePrueba.AntesDelTearDown(teclado);
            base.TearDown();
            MapasDeAccionesDePrueba.DespuesDelTearDown();
        }

        private void RegistrarLog(string mensaje, string pila, LogType tipo)
        {
            if (tipo == LogType.Error || tipo == LogType.Exception || tipo == LogType.Assert) errores.Add(mensaje);
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

        private GameObject Crear(string ruta, Vector3 posicion)
        {
            GameObject objeto = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(ruta), posicion, Quaternion.identity);
            creados.Add(objeto);
            return objeto;
        }

        private static void FijarFloat(Object objeto, string campo, float valor)
        {
            SerializedObject so = new SerializedObject(objeto);
            so.FindProperty(campo).floatValue = valor;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // Arena, jugador, servicios y flujo casero con tiempos cortos; deja la pelea anunciada (si hay servicio de apuestas).
        private IEnumerator Escenario(bool conServicioApuestas = true)
        {
            yield return Calentar();
            GameObject arena = Crear(RutaArena, PosicionArena);
            Crear(RutaJugador, arena.GetComponent<IArenaPelea>().InicioJugador.position);
            GameObject servicios = Crear(RutaServicios, Vector3.zero);
            cartera = Registro<ICartera>.Actual;
            Assert.IsNotNull(cartera, "ServiciosEconomia registra la cartera");
            Assert.AreEqual(DineroInicial, cartera.Dinero, "Se parte de Ƀ500");
            inventario = Registro<ServicioInventario>.Actual;
            Assert.IsNotNull(inventario, "ServiciosEconomia registra el inventario");
            brazo = AssetDatabase.LoadAssetAtPath<DatosObjeto>(RutaBrazo);
            if (!conServicioApuestas)
            {
                Object.DestroyImmediate(servicios.GetComponent<ServicioApuestas>());
            }
            apuestas = Registro<ServicioApuestas>.Actual;
            if (conServicioApuestas)
            {
                Assert.IsNotNull(apuestas, "ServiciosEconomia registra las apuestas");
                Assert.AreSame(apuestas, Registro<IApuestasPelea>.Actual, "Y como IApuestasPelea");
                apuestas.Resuelta += resueltas.Add;
            }

            config = ScriptableObject.CreateInstance<ConfigFlujoPelea>();
            FijarFloat(config, "segundosPorNumero", SegundosPorNumero);
            FijarFloat(config, "pausaDespuesKO", SegundosPorNumero);

            GameObject padre = new GameObject("PadreInactivo");
            padre.SetActive(false);
            creados.Add(padre);
            flujoCreado = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaFlujoCasera), padre.transform);
            director = flujoCreado.GetComponent<DirectorPelea>();
            SerializedObject soDirector = new SerializedObject(director);
            soDirector.FindProperty("config").objectReferenceValue = config;
            // La presentación del flujo casero dura más de lo que importa aquí.
            soDirector.FindProperty("presentador").objectReferenceValue = null;
            soDirector.ApplyModifiedPropertiesWithoutUndo();
            SerializedObject soPantalla = new SerializedObject(flujoCreado.GetComponentInChildren<PantallaPelea>(true));
            soPantalla.FindProperty("config").objectReferenceValue = config;
            soPantalla.ApplyModifiedPropertiesWithoutUndo();
            flujoCreado.transform.position = PosicionArena;
            flujoCreado.transform.SetParent(null);
            creados.Add(flujoCreado);
            padre.SetActive(true);
            yield return null;
            director.AnunciarApuesta();
        }

        private IEnumerator HastaEstado(EstadoFlujoPelea estado)
        {
            float limite = Time.time + EsperaMaxima;
            while (director.Estado != estado && Time.time < limite) yield return null;
            Assert.AreEqual(estado, director.Estado, "No llegó al estado esperado");
        }

        private static void Matar(IPeleador peleador)
        {
            ((VidaMecha)peleador.Vida).RecibirDanio(
                new InfoDanio(DanioMortal, TipoDanio.Golpe, Vector3.zero, Vector3.forward, null));
        }

        [UnityTest]
        public IEnumerator Apuesta_Anunciada_QuedaAbiertaConElRivalYLosDatosDeLaCasera()
        {
            yield return Escenario();

            Assert.IsTrue(apuestas.Abierta);
            Assert.IsFalse(string.IsNullOrEmpty(apuestas.Rival), "Trae el alias del rival");
            Assert.AreEqual(director.Rival.Nombre, apuestas.Rival);
            Assert.AreEqual(1f, apuestas.Momio);
            Assert.AreEqual(500, apuestas.ApuestaMaxima);
            Assert.AreEqual(0, errores.Count, string.Join("\n", errores));
        }

        [UnityTest]
        public IEnumerator Apuesta_ApuestaYGana_PagaApuestaMomioPremioYEntregaElBrazo()
        {
            yield return Escenario();
            Assert.IsTrue(apuestas.Apostar(250));
            Assert.AreEqual(DineroInicial - 250, cartera.Dinero);
            Assert.AreEqual(250, apuestas.Apostado);

            director.EmpezarPelea();
            yield return HastaEstado(EstadoFlujoPelea.Peleando);
            Matar(director.Rival);
            yield return HastaEstado(EstadoFlujoPelea.Resultado);
            yield return null;

            Assert.AreEqual(DineroInicial - 250 + 500 + PremioCasera, cartera.Dinero);
            Assert.AreEqual(1350, cartera.Dinero);
            Assert.AreEqual(1, inventario.Cantidad(brazo));
            CollectionAssert.AreEqual(new[] { 250 }, resueltas);
            Assert.IsFalse(apuestas.Abierta);
            Assert.AreEqual(0, apuestas.Apostado);
            Assert.AreEqual(0, errores.Count, string.Join("\n", errores));
        }

        [UnityTest]
        public IEnumerator Apuesta_ApuestaYPierde_PierdeLoApostadoSinPiezaNiPremio()
        {
            yield return Escenario();
            Assert.IsTrue(apuestas.Apostar(250));
            var neto = new List<int>();
            director.ApuestaResuelta += neto.Add;

            director.EmpezarPelea();
            yield return HastaEstado(EstadoFlujoPelea.Peleando);
            Matar(director.Jugador);
            yield return HastaEstado(EstadoFlujoPelea.Resultado);
            yield return null;

            Assert.AreEqual(250, cartera.Dinero);
            Assert.AreEqual(0, inventario.Cantidad(brazo));
            CollectionAssert.AreEqual(new[] { -250 }, resueltas);
            CollectionAssert.AreEqual(new[] { -250 }, neto);
            Assert.AreEqual(0, errores.Count, string.Join("\n", errores));
        }

        [UnityTest]
        public IEnumerator Apuesta_SinApostar_GanarSoloPagaElPremioYNoHayAviso()
        {
            yield return Escenario();

            director.EmpezarPelea();
            yield return HastaEstado(EstadoFlujoPelea.Peleando);
            Matar(director.Rival);
            yield return HastaEstado(EstadoFlujoPelea.Resultado);
            yield return null;

            Assert.AreEqual(DineroInicial + PremioCasera, cartera.Dinero);
            Assert.AreEqual(0, inventario.Cantidad(brazo));
            Assert.AreEqual(0, resueltas.Count, "Sin apuesta no se avisa");
            Assert.AreEqual(0, errores.Count, string.Join("\n", errores));
        }

        [UnityTest]
        public IEnumerator Apuesta_CancelarAntesDelConteo_DevuelveElDinero()
        {
            yield return Escenario();
            Assert.IsTrue(apuestas.Apostar(250));

            Assert.IsTrue(apuestas.Cancelar());

            Assert.AreEqual(DineroInicial, cartera.Dinero);
            Assert.AreEqual(0, apuestas.Apostado);
            Assert.IsTrue(apuestas.Abierta, "Sigue abierta: puede volver a apostar");
            Assert.IsTrue(apuestas.Apostar(100));
            Assert.AreEqual(0, errores.Count, string.Join("\n", errores));
        }

        [UnityTest]
        public IEnumerator Apuesta_DespuesDeEmpezarElConteoYDePelea_YaNoSeApuestaNiSeCancela()
        {
            yield return Escenario();
            Assert.IsTrue(apuestas.Apostar(100));

            director.EmpezarPelea();
            Assert.IsFalse(apuestas.Abierta, "Se cierra al empezar el conteo");
            Assert.IsFalse(apuestas.Cancelar(), "No se cancela en el conteo");
            yield return HastaEstado(EstadoFlujoPelea.Peleando);

            Assert.IsFalse(apuestas.PuedeApostar(250));
            Assert.IsFalse(apuestas.Apostar(250), "Apostar después de ¡PELEA! falla");
            Assert.IsFalse(apuestas.Cancelar());
            Assert.AreEqual(DineroInicial - 100, cartera.Dinero);
            Assert.AreEqual(100, apuestas.Apostado);
            Assert.AreEqual(0, errores.Count, string.Join("\n", errores));
        }

        [UnityTest]
        public IEnumerator Apuesta_DestruirElFlujoAMediaPelea_DevuelveLaApuesta()
        {
            yield return Escenario();
            Assert.IsTrue(apuestas.Apostar(250));
            director.EmpezarPelea();
            yield return HastaEstado(EstadoFlujoPelea.Peleando);
            Assert.AreEqual(DineroInicial - 250, cartera.Dinero);

            Object.Destroy(flujoCreado);
            yield return null;
            yield return null;

            Assert.AreEqual(DineroInicial, cartera.Dinero);
            Assert.AreEqual(0, apuestas.Apostado);
            Assert.IsFalse(apuestas.Pendiente);
            Assert.AreEqual(0, resueltas.Count, "Devolver no es resolver");
            Assert.AreEqual(0, errores.Count, string.Join("\n", errores));
        }

        [UnityTest]
        public IEnumerator Apuesta_SinServicioDeApuestas_LaPeleaYElPremioSiguenComoAntes()
        {
            yield return Escenario(false);
            Assert.IsNull(apuestas);
            Assert.IsFalse(director.AnunciarApuesta(), "Sin servicio no anuncia nada");

            director.EmpezarPelea();
            yield return HastaEstado(EstadoFlujoPelea.Peleando);
            Matar(director.Rival);
            yield return HastaEstado(EstadoFlujoPelea.Resultado);
            yield return null;

            Assert.AreEqual(DineroInicial + PremioCasera, cartera.Dinero);
            Assert.AreEqual(0, errores.Count, string.Join("\n", errores));
        }
    }
}
