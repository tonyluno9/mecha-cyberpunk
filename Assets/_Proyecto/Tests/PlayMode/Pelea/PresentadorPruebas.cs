using System.Collections;
using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Pelea.Datos;
using MechaCyberpunk.Pelea.Flujo;
using MechaCyberpunk.Progresion;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace MechaCyberpunk.Tests.Pelea
{
    /// <summary>Presentador de T-070: FlujoPeleaTutorial.prefab real con arena, MechaJugador y RivalTutorial.</summary>
    public class PresentadorPruebas : InputTestFixture
    {
        private const string RutaArena = "Assets/_Proyecto/Pelea/Pruebas/ArenaDePrueba.prefab";
        private const string RutaFlujo = "Assets/_Proyecto/Pelea/Prefabs/FlujoPeleaTutorial.prefab";
        private const string RutaJugador = "Assets/_Proyecto/Mecha/Prefabs/MechaJugador.prefab";
        private const string RutaServicios = "Assets/_Proyecto/Progresion/Prefabs/ServiciosProgresion.prefab";
        private const string RutaConfigPresentador = "Assets/_Proyecto/Pelea/Datos/ConfigPresentador_Tutorial.asset";
        private const int FramesEstables = 10;
        private const int MaxFramesCalentamiento = 1000;
        private const float DeltaTimeEstable = 0.05f;
        private const float SegundosPorLinea = 0.3f;
        private const float SegundosPorNumero = 0.2f;
        private const float EsperaMaxima = 15f;
        private const int LineasEsperadas = 4;
        private static readonly Vector3 PosicionArena = new Vector3(1000f, 0f, 0f);

        private readonly List<GameObject> creados = new List<GameObject>();
        private readonly List<string> lineas = new List<string>();
        private readonly List<string> textosEnPantalla = new List<string>();
        private readonly List<string> errores = new List<string>();
        private ConfigFlujoPelea configFlujo;
        private ConfigPresentador configPresentador;
        private DirectorPelea director;

        public override void Setup()
        {
            base.Setup();
            Registros.LimpiarTodos();
            EventosPelea.LimpiarSuscriptores();
            InputSystem.AddDevice<Keyboard>();
            InputSystem.AddDevice<Mouse>();
            lineas.Clear();
            textosEnPantalla.Clear();
            errores.Clear();
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
            if (configFlujo != null) Object.Destroy(configFlujo);
            if (configPresentador != null) Object.Destroy(configPresentador);
            Registros.LimpiarTodos();
            EventosPelea.LimpiarSuscriptores();
            base.TearDown();
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

        // Arma arena, jugador y el flujo del tutorial con tiempos cortos (cambia los config antes del Awake del director).
        private IEnumerator Escenario(string aliasPerfil)
        {
            yield return Calentar();
            GameObject arena = Crear(RutaArena, PosicionArena);
            Crear(RutaJugador, arena.GetComponent<IArenaPelea>().InicioJugador.position);
            if (aliasPerfil != null)
            {
                Crear(RutaServicios, Vector3.zero).GetComponent<ServicioPerfil>().Fijar("Juan", aliasPerfil);
            }

            configFlujo = ScriptableObject.CreateInstance<ConfigFlujoPelea>();
            FijarFloat(configFlujo, "segundosPorNumero", SegundosPorNumero);
            FijarFloat(configFlujo, "pausaDespuesKO", SegundosPorNumero);
            configPresentador = Object.Instantiate(AssetDatabase.LoadAssetAtPath<ConfigPresentador>(RutaConfigPresentador));
            FijarFloat(configPresentador, "segundosPorLinea", SegundosPorLinea);

            GameObject padre = new GameObject("PadreInactivo");
            padre.SetActive(false);
            creados.Add(padre);
            GameObject flujo = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaFlujo), padre.transform);
            director = flujo.GetComponent<DirectorPelea>();
            SerializedObject soDirector = new SerializedObject(director);
            soDirector.FindProperty("config").objectReferenceValue = configFlujo;
            soDirector.FindProperty("presentador").objectReferenceValue = configPresentador;
            soDirector.ApplyModifiedPropertiesWithoutUndo();
            SerializedObject soPantalla = new SerializedObject(flujo.GetComponentInChildren<PantallaPelea>(true));
            soPantalla.FindProperty("config").objectReferenceValue = configFlujo;
            soPantalla.ApplyModifiedPropertiesWithoutUndo();
            flujo.transform.position = PosicionArena;
            flujo.transform.SetParent(null);
            creados.Add(flujo);
            director.LineaDicha += linea =>
            {
                lineas.Add(linea);
                textosEnPantalla.Add(director.Pantalla.Texto);
            };
            padre.SetActive(true);
        }

        private IEnumerator HastaEstado(EstadoFlujoPelea estado)
        {
            float limite = Time.time + EsperaMaxima;
            while (director.Estado != estado && Time.time < limite) yield return null;
            Assert.AreEqual(estado, director.Estado, "No llegó al estado esperado");
        }

        [UnityTest]
        public IEnumerator Presentacion_ConPerfilLaChispa_MuestraLasCuatroLineasEnOrdenYElGritoLlevaElAlias()
        {
            yield return Escenario("LA CHISPA");
            yield return HastaEstado(EstadoFlujoPelea.Peleando);

            Assert.AreEqual(LineasEsperadas, lineas.Count);
            Assert.AreEqual("¡BIENVENIDOS AL CLANDESTINO!", lineas[0]);
            Assert.AreEqual("POR EL CAMPEONATO DEL BARRIO...", lineas[1]);
            StringAssert.Contains("EL CAMPEÓN", lineas[2]);
            StringAssert.Contains("LA CHISPA", lineas[3]);
            CollectionAssert.AreEqual(lineas, textosEnPantalla, "La pantalla muestra lo mismo que dice el presentador");
        }

        [UnityTest]
        public IEnumerator Presentacion_SinPerfilRegistrado_DiceNovato()
        {
            yield return Escenario(null);
            yield return HastaEstado(EstadoFlujoPelea.Peleando);

            Assert.AreEqual(LineasEsperadas, lineas.Count);
            StringAssert.Contains("NOVATO", lineas[3]);
        }

        [UnityTest]
        public IEnumerator Presentacion_ElGritoSaleMasGrandeYDeOtroColorQueLasDemas()
        {
            yield return Escenario("LA CHISPA");
            Text texto = director.Pantalla.GetComponentInChildren<Text>(true);
            float limite = Time.time + EsperaMaxima;
            while (lineas.Count < 2 && Time.time < limite) yield return null;
            int tamanoNormal = texto.fontSize;
            Color colorNormal = texto.color;
            while (lineas.Count < LineasEsperadas && Time.time < limite) yield return null;
            Assert.AreEqual(LineasEsperadas, lineas.Count);
            Assert.Greater(texto.fontSize, tamanoNormal);
            Assert.AreNotEqual(colorNormal, texto.color);
        }

        [UnityTest]
        public IEnumerator Presentacion_NingunPeleadorTieneControlHastaElFinDelConteo()
        {
            yield return Escenario("LA CHISPA");
            yield return null;
            Assert.AreEqual(EstadoFlujoPelea.Presentacion, director.Estado);
            int cuadrosEnPresentacion = 0;
            while (director.Estado == EstadoFlujoPelea.Presentacion)
            {
                Assert.IsFalse(director.Jugador.ControlActivo, "El jugador tiene control en la presentación");
                Assert.IsFalse(director.Rival.ControlActivo, "El rival tiene control en la presentación");
                cuadrosEnPresentacion++;
                yield return null;
            }
            Assert.Greater(cuadrosEnPresentacion, 1);
            Assert.AreEqual(EstadoFlujoPelea.Conteo, director.Estado, "Después de la presentación viene el conteo");
            while (director.Estado == EstadoFlujoPelea.Conteo)
            {
                Assert.IsFalse(director.Jugador.ControlActivo);
                yield return null;
            }
            Assert.AreEqual(EstadoFlujoPelea.Peleando, director.Estado);
            Assert.IsTrue(director.Jugador.ControlActivo, "La pelea normal empieza con control");
            Assert.IsTrue(director.Rival.ControlActivo);
            Assert.AreEqual(0, errores.Count, string.Join("\n", errores));
        }
    }
}
