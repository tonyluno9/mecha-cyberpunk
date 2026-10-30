using System.Collections;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using MechaCyberpunk.Combate.Vida;
using MechaCyberpunk.Core;
using MechaCyberpunk.Pelea.Datos;
using MechaCyberpunk.Pelea.Flujo;
using MechaCyberpunk.Progresion;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Pelea
{
    /// <summary>Premios de T-106: arena, MechaJugador, ServiciosEconomia (cartera) y los flujos reales (prefabs).</summary>
    public class PremioPeleaPruebas : InputTestFixture
    {
        private const string RutaArena = "Assets/_Proyecto/Pelea/Pruebas/ArenaDePrueba.prefab";
        private const string RutaFlujo = "Assets/_Proyecto/Pelea/Prefabs/FlujoPelea.prefab";
        private const string RutaFlujoTutorial = "Assets/_Proyecto/Pelea/Prefabs/FlujoPeleaTutorial.prefab";
        private const string RutaJugador = "Assets/_Proyecto/Mecha/Prefabs/MechaJugador.prefab";
        private const string RutaServicios = "Assets/_Proyecto/Economia/Prefabs/ServiciosEconomia.prefab";
        private const string RutaConfigPresentador = "Assets/_Proyecto/Pelea/Datos/ConfigPresentador_Tutorial.asset";
        private const string RutaCaptura = "docs/capturas/T-106-1.png";
        private const string VariableCaptura = "CAPTURAR_T106";
        private const int AnchoCaptura = 1280;
        private const int AltoCaptura = 720;
        private const int ProfundidadCaptura = 24;
        private const int FramesEstables = 10;
        private const int MaxFramesCalentamiento = 1000;
        private const float DeltaTimeEstable = 0.05f;
        private const float SegundosPorNumero = 0.2f;
        private const float SegundosPorLinea = 0.2f;
        private const float EsperaMaxima = 15f;
        private const float DanioMortal = 100000f;
        private const int PremioCasera = 600;
        private const int PremioTutorial = 1500;
        private static readonly Vector3 PosicionArena = new Vector3(1000f, 0f, 0f);

        private Keyboard teclado;
        private readonly List<GameObject> creados = new List<GameObject>();
        private readonly List<string> avisos = new List<string>();
        private readonly List<string> errores = new List<string>();
        private readonly List<(int, MotivoDinero)> cambios = new List<(int, MotivoDinero)>();
        private readonly List<int> premiosPagados = new List<int>();
        private ConfigFlujoPelea config;
        private ConfigPresentador configPresentador;
        private ICartera cartera;
        private DirectorPelea director;

        public override void Setup()
        {
            base.Setup();
            Registros.LimpiarTodos();
            EventosPelea.LimpiarSuscriptores();
            teclado = InputSystem.AddDevice<Keyboard>();
            InputSystem.AddDevice<Mouse>();
            avisos.Clear();
            errores.Clear();
            cambios.Clear();
            premiosPagados.Clear();
            cartera = null;
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
            if (tipo == LogType.Warning) avisos.Add(mensaje);
            else if (tipo == LogType.Error || tipo == LogType.Exception || tipo == LogType.Assert) errores.Add(mensaje);
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

        // Arma arena, jugador, (opcional) la cartera y el flujo con tiempos cortos; el config se cambia antes del Awake del director.
        private IEnumerator Escenario(string rutaFlujo, bool conCartera = true)
        {
            yield return Calentar();
            GameObject arena = Crear(RutaArena, PosicionArena);
            Crear(RutaJugador, arena.GetComponent<IArenaPelea>().InicioJugador.position);
            if (conCartera)
            {
                Crear(RutaServicios, Vector3.zero);
                cartera = Registro<ICartera>.Actual;
                Assert.IsNotNull(cartera, "ServiciosEconomia registra la cartera");
                cartera.Cambio += (monto, motivo) => cambios.Add((monto, motivo));
            }

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
            if (rutaFlujo == RutaFlujoTutorial)
            {
                configPresentador = Object.Instantiate(AssetDatabase.LoadAssetAtPath<ConfigPresentador>(RutaConfigPresentador));
                FijarFloat(configPresentador, "segundosPorLinea", SegundosPorLinea);
                soDirector.FindProperty("presentador").objectReferenceValue = configPresentador;
            }
            soDirector.ApplyModifiedPropertiesWithoutUndo();
            SerializedObject soPantalla = new SerializedObject(flujo.GetComponentInChildren<PantallaPelea>(true));
            soPantalla.FindProperty("config").objectReferenceValue = config;
            soPantalla.ApplyModifiedPropertiesWithoutUndo();
            flujo.transform.position = PosicionArena;
            flujo.transform.SetParent(null);
            creados.Add(flujo);
            director.PremioPagado += premiosPagados.Add;
            padre.SetActive(true);
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

        // Solo con CAPTURAR_T106=1: dibuja lo que ve la cámara del jugador (GANASTE y la línea del premio) y lo guarda como PNG.
        private void TomarCaptura()
        {
            Camera camara = Camera.main;
            if (camara == null) camara = Object.FindFirstObjectByType<Camera>();
            Assert.IsNotNull(camara, "Hay una cámara para la captura");
            RenderTexture rt = new RenderTexture(AnchoCaptura, AltoCaptura, ProfundidadCaptura);
            Texture2D imagen = new Texture2D(AnchoCaptura, AltoCaptura, TextureFormat.RGB24, false);
            RenderTexture anterior = RenderTexture.active;
            // Para que el texto se lea limpio se ocultan las mallas 3D (rival, cubos, suelo): la pantalla es uGUI y no usa Renderer.
            List<Renderer> ocultos = new List<Renderer>();
            foreach (Renderer malla in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            {
                if (!malla.enabled) continue;
                malla.enabled = false;
                ocultos.Add(malla);
            }
            camara.targetTexture = rt;
            camara.Render();
            foreach (Renderer malla in ocultos) { if (malla != null) malla.enabled = true; }
            RenderTexture.active = rt;
            imagen.ReadPixels(new Rect(0f, 0f, AnchoCaptura, AltoCaptura), 0, 0);
            imagen.Apply();
            camara.targetTexture = null;
            RenderTexture.active = anterior;
            string ruta = Path.GetFullPath(Path.Combine(Application.dataPath, "..", RutaCaptura));
            Directory.CreateDirectory(Path.GetDirectoryName(ruta));
            File.WriteAllBytes(ruta, imagen.EncodeToPNG());
            Object.Destroy(rt);
            Object.Destroy(imagen);
        }

        private IEnumerator PulsarTecla(Key tecla)
        {
            Press(teclado[tecla]);
            yield return null;
            Release(teclado[tecla]);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Premio_GanaElJugador_AbonaExactamenteUnaVezConMotivoPremioYLaPantallaLoDice()
        {
            yield return Escenario(RutaFlujo);
            int antes = cartera.Dinero;
            yield return HastaEstado(EstadoFlujoPelea.Peleando);
            Matar(director.Rival);
            yield return HastaEstado(EstadoFlujoPelea.Resultado);
            for (int i = 0; i < 10; i++) yield return null;

            if (System.Environment.GetEnvironmentVariable(VariableCaptura) == "1") TomarCaptura();

            Assert.AreEqual(antes + PremioCasera, cartera.Dinero);
            Assert.AreEqual(1, cambios.Count, "Un solo abono");
            Assert.AreEqual(PremioCasera, cambios[0].Item1);
            Assert.AreEqual(MotivoDinero.Premio, cambios[0].Item2);
            CollectionAssert.AreEqual(new[] { PremioCasera }, premiosPagados);
            Assert.AreEqual("GANASTE", director.Pantalla.Texto);
            Assert.AreEqual("+Ƀ600", director.Pantalla.Premio);
            Assert.AreEqual(0, errores.Count, string.Join("\n", errores));
        }

        [UnityTest]
        public IEnumerator Premio_PierdeElJugador_NoCambiaElDineroNiHayLineaDePremio()
        {
            yield return Escenario(RutaFlujo);
            int antes = cartera.Dinero;
            yield return HastaEstado(EstadoFlujoPelea.Peleando);
            Matar(director.Jugador);
            yield return HastaEstado(EstadoFlujoPelea.Resultado);
            for (int i = 0; i < 10; i++) yield return null;

            Assert.AreEqual("PERDISTE", director.Pantalla.Texto);
            Assert.AreEqual(antes, cartera.Dinero);
            Assert.AreEqual(0, cambios.Count);
            Assert.AreEqual(0, premiosPagados.Count);
            Assert.AreEqual(string.Empty, director.Pantalla.Premio);
            Assert.AreEqual(0, errores.Count, string.Join("\n", errores));
        }

        [UnityTest]
        public IEnumerator Premio_GanaRevanchaYGanaOtraVez_PagaDosVecesYLaLineaSeBorraAlEmpezarLaRevancha()
        {
            yield return Escenario(RutaFlujo);
            int antes = cartera.Dinero;
            yield return HastaEstado(EstadoFlujoPelea.Peleando);
            Matar(director.Rival);
            yield return HastaEstado(EstadoFlujoPelea.Resultado);
            yield return null;
            Assert.AreEqual("+Ƀ600", director.Pantalla.Premio);

            yield return PulsarTecla(Key.R);
            Assert.AreNotEqual(EstadoFlujoPelea.Resultado, director.Estado, "La revancha empezó");
            Assert.AreEqual(string.Empty, director.Pantalla.Premio, "La línea se borró al empezar la revancha");
            yield return HastaEstado(EstadoFlujoPelea.Peleando);
            Matar(director.Rival);
            yield return HastaEstado(EstadoFlujoPelea.Resultado);
            yield return null;

            Assert.AreEqual(antes + 2 * PremioCasera, cartera.Dinero);
            Assert.AreEqual(2, cambios.Count);
            Assert.AreEqual("+Ƀ600", director.Pantalla.Premio);
            Assert.AreEqual(0, errores.Count, string.Join("\n", errores));
        }

        [UnityTest]
        public IEnumerator Premio_SinServiciosEconomia_UnSoloAvisoYLaPeleaTerminaNormal()
        {
            LogAssert.Expect(LogType.Warning, new Regex("no encontró la cartera"));
            yield return Escenario(RutaFlujo, false);
            yield return HastaEstado(EstadoFlujoPelea.Peleando);
            Matar(director.Rival);
            yield return HastaEstado(EstadoFlujoPelea.Resultado);
            yield return null;
            Assert.AreEqual("GANASTE", director.Pantalla.Texto);
            Assert.AreEqual(string.Empty, director.Pantalla.Premio, "Sin cartera no se muestra premio");

            yield return PulsarTecla(Key.R);
            yield return HastaEstado(EstadoFlujoPelea.Peleando);
            Matar(director.Rival);
            yield return HastaEstado(EstadoFlujoPelea.Resultado);
            yield return null;

            Assert.AreEqual(1, avisos.FindAll(a => a.Contains("no encontró la cartera")).Count, "Avisa una sola vez");
            Assert.AreEqual(0, premiosPagados.Count);
            Assert.AreEqual(0, errores.Count, string.Join("\n", errores));
        }

        [UnityTest]
        public IEnumerator Premio_FlujoTutorial_GanarPaga1500()
        {
            yield return Escenario(RutaFlujoTutorial);
            int antes = cartera.Dinero;
            yield return HastaEstado(EstadoFlujoPelea.Peleando);
            Matar(director.Rival);
            yield return HastaEstado(EstadoFlujoPelea.Resultado);
            yield return null;

            Assert.AreEqual(antes + PremioTutorial, cartera.Dinero);
            Assert.AreEqual(1, cambios.Count);
            Assert.AreEqual(MotivoDinero.Premio, cambios[0].Item2);
            Assert.AreEqual("+Ƀ1,500", director.Pantalla.Premio);
            Assert.AreEqual(0, errores.Count, string.Join("\n", errores));
        }
    }
}
