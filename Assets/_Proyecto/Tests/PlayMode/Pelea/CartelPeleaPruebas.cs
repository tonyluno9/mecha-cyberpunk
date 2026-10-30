using System;
using System.Collections;
using System.Collections.Generic;
using MechaCyberpunk.Combate.Vida;
using MechaCyberpunk.Core;
using MechaCyberpunk.Economia;
using MechaCyberpunk.Pelea.Datos;
using MechaCyberpunk.Pelea.Flujo;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Tests.Pelea
{
    /// <summary>Carteles de T-155: arena de prueba, MechaJugador, ServiciosEconomia y los flujos reales (casero y tutorial).</summary>
    public class CartelPeleaPruebas : InputTestFixture
    {
        private const string RutaArena = "Assets/_Proyecto/Pelea/Pruebas/ArenaDePrueba.prefab";
        private const string RutaFlujoCasera = "Assets/_Proyecto/Pelea/Prefabs/FlujoPeleaCasera.prefab";
        private const string RutaFlujoTutorial = "Assets/_Proyecto/Pelea/Prefabs/FlujoPeleaTutorial.prefab";
        private const string RutaJugador = "Assets/_Proyecto/Mecha/Prefabs/MechaJugador.prefab";
        private const string RutaServicios = "Assets/_Proyecto/Economia/Prefabs/ServiciosEconomia.prefab";
        private const string RutaRivalBasico = "Assets/_Proyecto/IA/Prefabs/RivalBasico.prefab";
        private const string NombreCasero = "EL CHATARRAS";
        private const int FramesEstables = 10;
        private const int MaxFramesCalentamiento = 1000;
        private const float DeltaTimeEstable = 0.05f;
        private const float SegundosPorNumero = 0.2f;
        private const float SegundosPorLinea = 0.3f;
        private const float EsperaMaxima = 15f;
        private const float DanioMortal = 100000f;
        private const int PremioCasera = 600;
        private const int PremioCartel = 900;
        private const int LigaCartel = 1;
        private const int LigaTutorial = 4;
        private static readonly Vector3 PosicionArena = new Vector3(1000f, 0f, 0f);

        private Keyboard teclado;
        private readonly List<GameObject> creados = new List<GameObject>();
        private readonly List<string> errores = new List<string>();
        private readonly List<InfoPelea> presentadas = new List<InfoPelea>();
        private ConfigFlujoPelea config;
        private ConfigPresentador configPresentador;
        private ICartera cartera;
        private DirectorPelea director;
        private GameObject prefabRival;
        private DobleLigas dobleLigas;

        public override void Setup()
        {
            base.Setup();
            Registros.LimpiarTodos();
            EventosPelea.LimpiarSuscriptores();
            teclado = InputSystem.AddDevice<Keyboard>();
            InputSystem.AddDevice<Mouse>();
            errores.Clear();
            presentadas.Clear();
            cartera = null;
            director = null;
            dobleLigas = null;
            EventosPelea.PeleaPresentada += presentadas.Add;
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
            if (configPresentador != null) Object.Destroy(configPresentador);
            Registros.LimpiarTodos();
            EventosPelea.LimpiarSuscriptores();
            base.TearDown();
        }

        private void RegistrarLog(string mensaje, string pila, LogType tipo)
        {
            if (tipo == LogType.Error || tipo == LogType.Exception || tipo == LogType.Assert) errores.Add(mensaje);
        }

        private sealed class DobleLigas : IProgresoLigas
        {
            public int CantidadLigas => 4;
            public int LigaActual => 1;
            public int CampeonesVencidos => 0;
            public string NombreLiga(int liga) => liga == 1 ? "Sótano" : string.Empty;
#pragma warning disable CS0067
            public event Action Cambio;
            public event Action<int> CampeonVencido;
#pragma warning restore CS0067
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

        // quitarPresentador: la presentación del flujo casero dura más de lo que importa en casi todas las pruebas.
        private IEnumerator Escenario(string rutaFlujo, bool quitarPresentador = true)
        {
            yield return Calentar();
            GameObject arena = Crear(RutaArena, PosicionArena);
            Crear(RutaJugador, arena.GetComponent<IArenaPelea>().InicioJugador.position);
            Crear(RutaServicios, Vector3.zero);
            cartera = Registro<ICartera>.Actual;
            Assert.IsNotNull(cartera, "ServiciosEconomia registra la cartera");
            prefabRival = AssetDatabase.LoadAssetAtPath<GameObject>(RutaRivalBasico);

            config = ScriptableObject.CreateInstance<ConfigFlujoPelea>();
            FijarFloat(config, "segundosPorNumero", SegundosPorNumero);
            FijarFloat(config, "pausaDespuesKO", SegundosPorNumero);

            GameObject padre = new GameObject("PadreInactivo");
            padre.SetActive(false);
            creados.Add(padre);
            GameObject flujo = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(rutaFlujo), padre.transform);
            director = flujo.GetComponent<DirectorPelea>();
            SerializedObject soDirector = new SerializedObject(director);
            soDirector.FindProperty("config").objectReferenceValue = config;
            if (quitarPresentador)
            {
                soDirector.FindProperty("presentador").objectReferenceValue = null;
            }
            soDirector.ApplyModifiedPropertiesWithoutUndo();
            SerializedObject soPantalla = new SerializedObject(flujo.GetComponentInChildren<PantallaPelea>(true));
            soPantalla.FindProperty("config").objectReferenceValue = config;
            soPantalla.ApplyModifiedPropertiesWithoutUndo();
            flujo.transform.position = PosicionArena;
            flujo.transform.SetParent(null);
            creados.Add(flujo);
            padre.SetActive(true);
            yield return null;
        }

        private CartelPelea Cartel(ConfigPresentador presentador = null, bool esJefe = false, int liga = LigaCartel)
        {
            return CartelPelea.Crear(prefabRival, PremioCartel, 0, liga, esJefe, null, presentador);
        }

        private ConfigPresentador CrearPresentador(string linea)
        {
            configPresentador = ScriptableObject.CreateInstance<ConfigPresentador>();
            SerializedObject so = new SerializedObject(configPresentador);
            SerializedProperty lineas = so.FindProperty("lineas");
            lineas.arraySize = 1;
            lineas.GetArrayElementAtIndex(0).stringValue = linea;
            so.FindProperty("indiceGrito").intValue = -1;
            so.FindProperty("segundosPorLinea").floatValue = SegundosPorLinea;
            so.ApplyModifiedPropertiesWithoutUndo();
            return configPresentador;
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

        private IEnumerator PelearYGanar()
        {
            director.EmpezarPelea();
            yield return HastaEstado(EstadoFlujoPelea.Peleando);
            Matar(director.Rival);
            yield return HastaEstado(EstadoFlujoPelea.Resultado);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Cartel_GanaConElCartel_AbonaElPremioDelCartelYLaPantallaLoDice()
        {
            yield return Escenario(RutaFlujoCasera);
            int antes = cartera.Dinero;
            Assert.IsTrue(director.PrepararCartel(Cartel(), 1f));

            yield return PelearYGanar();

            Assert.AreEqual(antes + PremioCartel, cartera.Dinero);
            Assert.AreEqual("+Ƀ900", director.Pantalla.Premio);
            Assert.AreNotEqual(NombreCasero, director.Rival.Nombre, "Peleó contra el rival del cartel");
            Assert.AreEqual(0, errores.Count, string.Join("\n", errores));
        }

        [UnityTest]
        public IEnumerator Cartel_FraccionDelPremio_AbonaLaFraccionRedondeada()
        {
            yield return Escenario(RutaFlujoCasera);
            int antes = cartera.Dinero;
            Assert.IsTrue(director.PrepararCartel(Cartel(), 0.5f));

            yield return PelearYGanar();

            Assert.AreEqual(antes + 450, cartera.Dinero);
            Assert.AreEqual("+Ƀ450", director.Pantalla.Premio);
            Assert.AreEqual(0, errores.Count, string.Join("\n", errores));
        }

        [UnityTest]
        public IEnumerator Cartel_ReiniciarYQuitarCartel_LaSiguientePeleaEsLaCaseraYPaga600()
        {
            yield return Escenario(RutaFlujoCasera);
            Assert.IsTrue(director.PrepararCartel(Cartel(null, true, 2), 1f));
            yield return PelearYGanar();

            Assert.IsTrue(director.Reiniciar());
            Assert.IsNotNull(director.CartelActual, "Reiniciar no quita el cartel");
            Assert.IsTrue(director.QuitarCartel());
            Assert.IsNull(director.CartelActual);
            int antes = cartera.Dinero;

            yield return PelearYGanar();

            Assert.AreEqual(NombreCasero, director.Rival.Nombre);
            Assert.AreEqual(antes + PremioCasera, cartera.Dinero);
            Assert.AreEqual("+Ƀ600", director.Pantalla.Premio);
            Assert.AreEqual(0, presentadas[1].Liga, "Sin cartel, la liga vuelve a la del prefab");
            Assert.IsFalse(presentadas[1].EsJefe);
            Assert.AreEqual(0, errores.Count, string.Join("\n", errores));
        }

        [UnityTest]
        public IEnumerator Cartel_PeleaPresentada_UnaVezPorPeleaConAliasLigaYJefeDelCartel()
        {
            yield return Escenario(RutaFlujoCasera);
            Assert.IsTrue(director.PrepararCartel(Cartel(null, true, 3), 1f));

            director.EmpezarPelea();
            yield return HastaEstado(EstadoFlujoPelea.Peleando);
            Matar(director.Rival);
            yield return HastaEstado(EstadoFlujoPelea.Resultado);
            yield return null;
            for (int i = 0; i < 10; i++) yield return null;

            Assert.AreEqual(1, presentadas.Count, "Una sola vez por pelea");
            Assert.IsFalse(string.IsNullOrEmpty(presentadas[0].Rival));
            Assert.AreEqual(director.Rival.Nombre, presentadas[0].Rival);
            Assert.AreEqual(3, presentadas[0].Liga);
            Assert.IsTrue(presentadas[0].EsJefe);
            Assert.AreEqual(0, errores.Count, string.Join("\n", errores));
        }

        [UnityTest]
        public IEnumerator Cartel_PeleaPresentada_ElTutorialPublicaLiga4YJefe()
        {
            yield return Escenario(RutaFlujoTutorial);
            yield return HastaEstado(EstadoFlujoPelea.Peleando);

            Assert.AreEqual(1, presentadas.Count);
            Assert.AreEqual(LigaTutorial, presentadas[0].Liga);
            Assert.IsTrue(presentadas[0].EsJefe);
            Assert.AreEqual(director.Rival.Nombre, presentadas[0].Rival);
            Assert.IsNull(director.CartelActual);
            Assert.AreEqual(0, errores.Count, string.Join("\n", errores));
        }

        [UnityTest]
        public IEnumerator Cartel_LineaConLiga_ConServicioSeVeElNombreDeLaLiga()
        {
            yield return Escenario(RutaFlujoCasera);
            dobleLigas = new DobleLigas();
            Registro<IProgresoLigas>.Agregar(dobleLigas);
            Assert.IsTrue(director.PrepararCartel(Cartel(CrearPresentador("LIGA {liga}: {rival}")), 1f));

            director.EmpezarPelea();
            yield return HastaEstado(EstadoFlujoPelea.Presentacion);

            Assert.AreEqual("LIGA SÓTANO: " + director.Rival.Nombre.ToUpperInvariant(), director.Pantalla.Texto);
            Registro<IProgresoLigas>.Quitar(dobleLigas);
            Assert.AreEqual(0, errores.Count, string.Join("\n", errores));
        }

        [UnityTest]
        public IEnumerator Cartel_LineaConLiga_SinServicioQuedaVaciaSinErrores()
        {
            yield return Escenario(RutaFlujoCasera);
            Assert.IsTrue(director.PrepararCartel(Cartel(CrearPresentador("LIGA {liga}: {rival}")), 1f));

            director.EmpezarPelea();
            yield return HastaEstado(EstadoFlujoPelea.Presentacion);

            Assert.AreEqual("LIGA : " + director.Rival.Nombre.ToUpperInvariant(), director.Pantalla.Texto);
            Assert.AreEqual(0, errores.Count, string.Join("\n", errores));
        }

        [UnityTest]
        public IEnumerator Cartel_DuranteElConteoOEnElResultado_NoSeAceptaYNadaCambia()
        {
            yield return Escenario(RutaFlujoCasera);
            director.EmpezarPelea();
            yield return HastaEstado(EstadoFlujoPelea.Conteo);
            IPeleador rival = director.Rival;

            Assert.IsFalse(director.PrepararCartel(Cartel(), 1f), "En el conteo no se acepta");
            Assert.IsNull(director.CartelActual);
            Assert.AreSame(rival, director.Rival);

            yield return HastaEstado(EstadoFlujoPelea.Peleando);
            Matar(director.Rival);
            yield return HastaEstado(EstadoFlujoPelea.Resultado);
            yield return null;
            int antes = cartera.Dinero;

            Assert.IsFalse(director.PrepararCartel(Cartel(), 1f), "En el resultado no se acepta");
            Assert.AreSame(rival, director.Rival);
            Assert.AreEqual("+Ƀ600", director.Pantalla.Premio);
            Assert.AreEqual(antes, cartera.Dinero);
            Assert.AreEqual(NombreCasero, director.Rival.Nombre);
            Assert.AreEqual(0, errores.Count, string.Join("\n", errores));
        }

        [UnityTest]
        public IEnumerator Cartel_ConApuestaAnunciada_NoSeAceptaYNadaCambia()
        {
            yield return Escenario(RutaFlujoCasera);
            Assert.IsTrue(director.AnunciarApuesta(), "La casera anuncia su apuesta");
            IPeleador rival = director.Rival;

            Assert.IsFalse(director.PrepararCartel(Cartel(), 1f));
            Assert.IsNull(director.CartelActual);
            Assert.AreSame(rival, director.Rival);
            Assert.IsTrue(rival != null && !ReferenceEquals(((Component)rival).gameObject, null));

            director.CancelarApuesta();
            Assert.IsTrue(director.PrepararCartel(Cartel(), 1f), "Cancelada la apuesta, ya se acepta");
            Assert.AreEqual(0, errores.Count, string.Join("\n", errores));
        }

        [UnityTest]
        public IEnumerator Cartel_CambiarDeCartelAntesDeEmpezar_DestruyeAlRivalAnterior()
        {
            yield return Escenario(RutaFlujoCasera);
            Assert.IsTrue(director.AnunciarApuesta());
            Component rivalCasero = (Component)director.Rival;
            director.CancelarApuesta();

            Assert.IsTrue(director.PrepararCartel(Cartel(), 1f));
            yield return null;

            Assert.IsTrue(rivalCasero == null, "El rival anterior se destruyó");
            Assert.IsNull(director.Rival);
            Assert.AreEqual(0, errores.Count, string.Join("\n", errores));
        }
    }
}
