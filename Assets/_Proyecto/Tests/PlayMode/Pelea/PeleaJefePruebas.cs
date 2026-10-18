using System.Collections;
using System.IO;
using System.Collections.Generic;
using MechaCyberpunk.Combate.Vida;
using MechaCyberpunk.Core;
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
    /// <summary>T-161: letrero "PELEA DE JEFE" y "¡VENCISTE AL CAMPEÓN!" en la pantalla de la arena, con los flujos reales.</summary>
    public class PeleaJefePruebas : InputTestFixture
    {
        private const string RutaArena = "Assets/_Proyecto/Pelea/Pruebas/ArenaDePrueba.prefab";
        private const string RutaFlujoCasera = "Assets/_Proyecto/Pelea/Prefabs/FlujoPeleaCasera.prefab";
        private const string RutaFlujoTutorial = "Assets/_Proyecto/Pelea/Prefabs/FlujoPeleaTutorial.prefab";
        private const string RutaJugador = "Assets/_Proyecto/Mecha/Prefabs/MechaJugador.prefab";
        private const string RutaServicios = "Assets/_Proyecto/Economia/Prefabs/ServiciosEconomia.prefab";
        private const string RutaRivalBasico = "Assets/_Proyecto/IA/Prefabs/RivalBasico.prefab";
        private const int FramesEstables = 10;
        private const int MaxFramesCalentamiento = 1000;
        private const float DeltaTimeEstable = 0.05f;
        private const float SegundosPorNumero = 0.3f;
        private const float SegundosPorLinea = 0.4f;
        private const float EsperaMaxima = 20f;
        private const float DanioMortal = 100000f;
        private const int PremioCartel = 900;
        private const string RutaCaptura = "docs/capturas/T-161-1.png";
        private const int AnchoCaptura = 1280;
        private const int AltoCaptura = 720;
        private static readonly Vector3 PosicionArena = new Vector3(1000f, 0f, 0f);

        private readonly List<GameObject> creados = new List<GameObject>();
        private readonly List<string> errores = new List<string>();
        private ConfigFlujoPelea config;
        private ConfigPresentador configPresentador;
        private DirectorPelea director;
        private GameObject prefabRival;
        private string jefeEnPresentacion;
        private string jefeEnConteo;
        private string jefeEnPelea;

        public override void Setup()
        {
            base.Setup();
            Registros.LimpiarTodos();
            EventosPelea.LimpiarSuscriptores();
            InputSystem.AddDevice<Keyboard>();
            InputSystem.AddDevice<Mouse>();
            errores.Clear();
            jefeEnPresentacion = null;
            jefeEnConteo = null;
            jefeEnPelea = null;
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

        private IEnumerator Escenario(string rutaFlujo, bool quitarPresentador = true)
        {
            yield return Calentar();
            GameObject arena = Crear(RutaArena, PosicionArena);
            Crear(RutaJugador, arena.GetComponent<IArenaPelea>().InicioJugador.position);
            Crear(RutaServicios, Vector3.zero);
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

        private CartelPelea Cartel(bool esJefe, int liga = 1)
        {
            configPresentador = ScriptableObject.CreateInstance<ConfigPresentador>();
            SerializedObject so = new SerializedObject(configPresentador);
            SerializedProperty lineas = so.FindProperty("lineas");
            lineas.arraySize = 1;
            lineas.GetArrayElementAtIndex(0).stringValue = "{rival} ENTRA A LA JAULA";
            so.FindProperty("indiceGrito").intValue = -1;
            so.FindProperty("segundosPorLinea").floatValue = SegundosPorLinea;
            so.ApplyModifiedPropertiesWithoutUndo();
            return CartelPelea.Crear(prefabRival, PremioCartel, 0, liga, esJefe, null, configPresentador);
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

        // Anota lo que dice el letrero de jefe en la presentación, en el conteo y al empezar la pelea.
        private IEnumerator RecorrerHastaPelea()
        {
            director.EmpezarPelea();
            yield return HastaEstado(EstadoFlujoPelea.Presentacion);
            yield return null;
            jefeEnPresentacion = director.Pantalla.Jefe;
            yield return HastaEstado(EstadoFlujoPelea.Conteo);
            yield return null;
            jefeEnConteo = director.Pantalla.Jefe;
            yield return HastaEstado(EstadoFlujoPelea.Peleando);
            yield return null;
            jefeEnPelea = director.Pantalla.Jefe;
        }

        [UnityTest]
        public IEnumerator Jefe_CartelDeJefeYGana_LetreroEnPresentacionYConteoSeOcultaYAlGanarCelebra()
        {
            yield return Escenario(RutaFlujoCasera);
            Assert.IsTrue(director.PrepararCartel(Cartel(true, 2), 1f));

            yield return RecorrerHastaPelea();
            Assert.AreEqual(config.TextoJefe, jefeEnPresentacion);
            Assert.AreEqual("PELEA DE JEFE", jefeEnPresentacion);
            Assert.AreEqual("PELEA DE JEFE", jefeEnConteo);
            Assert.AreEqual(string.Empty, jefeEnPelea, "Se oculta con ¡PELEA!");
            Assert.AreEqual(config.TextoPelea, director.Pantalla.Texto);

            Matar(director.Rival);
            yield return HastaEstado(EstadoFlujoPelea.Resultado);
            yield return null;
            Assert.AreEqual("¡VENCISTE AL CAMPEÓN!", director.Pantalla.Jefe);
            Assert.AreEqual(config.TextoGanaste, director.Pantalla.Texto);
            Assert.AreEqual(0, errores.Count, string.Join("\n", errores));
        }

        [UnityTest]
        public IEnumerator Jefe_CartelDeJefeYPierde_NoCelebra()
        {
            yield return Escenario(RutaFlujoCasera);
            Assert.IsTrue(director.PrepararCartel(Cartel(true, 2), 1f));

            yield return RecorrerHastaPelea();
            Assert.AreEqual("PELEA DE JEFE", jefeEnPresentacion);
            Matar(director.Jugador);
            yield return HastaEstado(EstadoFlujoPelea.Resultado);
            yield return null;

            Assert.AreEqual(config.TextoPerdiste, director.Pantalla.Texto);
            Assert.AreEqual(string.Empty, director.Pantalla.Jefe);
            Assert.AreEqual(0, errores.Count, string.Join("\n", errores));
        }

        [UnityTest]
        public IEnumerator Jefe_CartelNormal_NuncaHayLetrero()
        {
            yield return Escenario(RutaFlujoCasera);
            Assert.IsTrue(director.PrepararCartel(Cartel(false, 1), 1f));

            yield return RecorrerHastaPelea();
            Assert.AreEqual(string.Empty, jefeEnPresentacion);
            Assert.AreEqual(string.Empty, jefeEnConteo);
            Assert.AreEqual(string.Empty, jefeEnPelea);
            Matar(director.Rival);
            yield return HastaEstado(EstadoFlujoPelea.Resultado);
            yield return null;

            Assert.AreEqual(config.TextoGanaste, director.Pantalla.Texto);
            Assert.AreEqual(string.Empty, director.Pantalla.Jefe);
            Assert.AreEqual(0, errores.Count, string.Join("\n", errores));
        }

        [UnityTest]
        public IEnumerator Jefe_PeleaCaseraSinCartel_NuncaHayLetrero()
        {
            yield return Escenario(RutaFlujoCasera);

            director.EmpezarPelea();
            yield return HastaEstado(EstadoFlujoPelea.Conteo);
            yield return null;
            Assert.AreEqual(string.Empty, director.Pantalla.Jefe);
            yield return HastaEstado(EstadoFlujoPelea.Peleando);
            Matar(director.Rival);
            yield return HastaEstado(EstadoFlujoPelea.Resultado);
            yield return null;

            Assert.AreEqual(config.TextoGanaste, director.Pantalla.Texto);
            Assert.AreEqual(string.Empty, director.Pantalla.Jefe);
            Assert.AreEqual(0, errores.Count, string.Join("\n", errores));
        }

        [UnityTest]
        public IEnumerator Jefe_Tutorial_MuestraPeleaDeJefeEnLaPresentacion()
        {
            yield return Escenario(RutaFlujoTutorial, false);

            yield return HastaEstado(EstadoFlujoPelea.Presentacion);
            yield return null;
            Assert.AreEqual("PELEA DE JEFE", director.Pantalla.Jefe);
            yield return HastaEstado(EstadoFlujoPelea.Peleando);
            yield return null;
            Assert.AreEqual(string.Empty, director.Pantalla.Jefe);
            Assert.AreEqual(0, errores.Count, string.Join("\n", errores));
        }

        [UnityTest]
        public IEnumerator Captura_PeleaDeJefe_LetreroVistoDesdeLaCabina()
        {
            yield return Escenario(RutaFlujoCasera);
            Assert.IsTrue(director.PrepararCartel(Cartel(true, 2), 1f));
            director.EmpezarPelea();
            yield return HastaEstado(EstadoFlujoPelea.Presentacion);
            yield return null;
            Assert.AreEqual("PELEA DE JEFE", director.Pantalla.Jefe);

            // WaitForEndOfFrame no corre en batchmode: se renderiza la cámara del jugador a una textura y se guarda.
            Camera camara = Camera.main;
            Assert.IsNotNull(camara, "Falta la cámara de la cabina");
            RenderTexture destino = new RenderTexture(AnchoCaptura, AltoCaptura, 24);
            RenderTexture anterior = camara.targetTexture;
            camara.targetTexture = destino;
            camara.Render();
            camara.targetTexture = anterior;
            RenderTexture.active = destino;
            Texture2D imagen = new Texture2D(AnchoCaptura, AltoCaptura, TextureFormat.RGB24, false);
            imagen.ReadPixels(new Rect(0f, 0f, AnchoCaptura, AltoCaptura), 0, 0);
            imagen.Apply();
            RenderTexture.active = null;
            Directory.CreateDirectory(Path.GetDirectoryName(RutaCaptura));
            File.WriteAllBytes(RutaCaptura, imagen.EncodeToPNG());
            Object.Destroy(imagen);
            Object.Destroy(destino);
            Assert.IsTrue(File.Exists(RutaCaptura), "Se guardó la captura");
        }
    }
}
