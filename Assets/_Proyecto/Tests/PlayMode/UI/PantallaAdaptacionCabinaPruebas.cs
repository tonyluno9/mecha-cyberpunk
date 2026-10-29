using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using MechaCyberpunk.Combate.Lectura;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.Adaptacion;
using MechaCyberpunk.Pelea.Datos;
using MechaCyberpunk.Pelea.Flujo;
using MechaCyberpunk.UI;
using MechaCyberpunk.UI.Cabina;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Tests.UI
{
    /// <summary>Un ILecturaAdaptacion de mentira para probar la pantalla sin un rival de verdad.</summary>
    public class DobleAdaptacion : MonoBehaviour, ILecturaAdaptacion
    {
        public readonly int[] niveles = new int[3];
        public string Nombre => name;
        public Transform Raiz => transform;
        public int Nivel(TipoAdaptacion tipo) => niveles[(int)tipo];
        public int Activas => (niveles[0] > 0 ? 1 : 0) + (niveles[1] > 0 ? 1 : 0) + (niveles[2] > 0 ? 1 : 0);
        public event Action<TipoAdaptacion> Usada;
        public event Action<IReadOnlyList<CambioAdaptacion>> Aprendio;
        public event Action NivelesCambiados;

        private void OnEnable() => Registro<ILecturaAdaptacion>.Agregar(this);
        private void OnDisable() => Registro<ILecturaAdaptacion>.Quitar(this);

        public void Usar(TipoAdaptacion tipo) => Usada?.Invoke(tipo);
        public void Aprender(params CambioAdaptacion[] cambios) => Aprendio?.Invoke(cambios);
        public void CambiarNiveles() => NivelesCambiados?.Invoke();
    }

    /// <summary>
    /// T-203: la pantalla de adaptación de la cabina. Con dobles de ILecturaAdaptacion (elección, parpadeo, fin de pelea) y con un rival
    /// de verdad (arena + flujo + MechaJugador + RivalLiga1_1 + MemoriaRivales, entrada simulada con el ratón). Atajo anotado:
    /// RecibirDanio al rival tras daño real del jugador para terminar la pelea, y niveles fijados en MemoriaRivales para la captura 1.
    /// </summary>
    public class PantallaAdaptacionCabinaPruebas : InputTestFixture
    {
        private const string RutaJugador = "Assets/_Proyecto/Mecha/Prefabs/MechaJugador.prefab";
        private const string RutaEstacionado = "Assets/_Proyecto/Mecha/Prefabs/MechaEstacionado.prefab";
        private const string RutaArena = "Assets/_Proyecto/Pelea/Pruebas/ArenaDePrueba.prefab";
        private const string RutaFlujo = "Assets/_Proyecto/Pelea/Prefabs/FlujoPelea.prefab";
        private const string RutaLiga11 = "Assets/_Proyecto/IA/Prefabs/Ligas/RivalLiga1_1.prefab";
        private const string RutaConfig = "Assets/_Proyecto/UI/Datos/ConfigPantallaAdaptacion_Base.asset";
        private const string IdLiga11 = "DatosRival_Liga1_1";
        private const string VariableCaptura = "CAPTURAR_T203";
        private const string RutaCaptura1 = "docs/capturas/T-203-1.png";
        private const string RutaCaptura2 = "docs/capturas/T-203-2.png";
        private const int AnchoCaptura = 1280;
        private const int AltoCaptura = 720;
        private const int FramesEstables = 10;
        private const int MaxFramesCalentamiento = 1000;
        private const float DeltaTimeEstable = 0.05f;
        private const float EsperaReeleccion = 0.7f;
        private const float DistanciaCerca = 10f;
        private const float DistanciaLejos = 30f;
        private const float DistanciaFuera = 61f;
        private const float EscalaTiempo = 4f;
        private const float EsperaMaxima = 15f;
        private const float LimiteSegundos = 120f;
        private const float DanioMortal = 100000f;
        private const float SegundosEntreClics = 0.1f;
        private const int GolpesParaPunos = 10;
        private static readonly Vector3 PosicionArena = new Vector3(1000f, 0f, 0f);
        private static readonly Vector3 PosicionMecha = new Vector3(0f, 0.05f, 0f);

        private Keyboard teclado;
        private Mouse raton;
        private readonly List<GameObject> creados = new List<GameObject>();
        private readonly List<string> errores = new List<string>();
        private ConfigPantallaAdaptacion config;
        private GameObject mecha;
        private PantallaAdaptacionCabina pantalla;
        private ConfigFlujoPelea configFlujo;
        private DirectorPelea director;
        private GolpeEnPrueba golpes;
        private float escalaOriginal;

        private class GolpeEnPrueba
        {
            public int impactos;
        }

        public override void Setup()
        {
            base.Setup();
            Registros.LimpiarTodos();
            EventosPelea.LimpiarSuscriptores();
            teclado = InputSystem.AddDevice<Keyboard>();
            raton = InputSystem.AddDevice<Mouse>();
            errores.Clear();
            escalaOriginal = Time.timeScale;
            config = AssetDatabase.LoadAssetAtPath<ConfigPantallaAdaptacion>(RutaConfig);
            Application.logMessageReceived += RegistrarMensaje;
        }

        public override void TearDown()
        {
            Time.timeScale = escalaOriginal;
            Application.logMessageReceived -= RegistrarMensaje;
            foreach (GameObject objeto in creados)
            {
                if (objeto != null)
                {
                    Object.Destroy(objeto);
                }
            }
            creados.Clear();
            if (configFlujo != null)
            {
                Object.Destroy(configFlujo);
            }
            EventosPelea.LimpiarSuscriptores();
            Registros.LimpiarTodos();
            base.TearDown();
        }

        private void RegistrarMensaje(string mensaje, string pila, LogType tipo)
        {
            if (tipo == LogType.Error || tipo == LogType.Exception || tipo == LogType.Assert)
            {
                errores.Add(mensaje);
            }
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

        private static IEnumerator Esperar(float segundos)
        {
            float fin = Time.time + segundos;
            while (Time.time < fin)
            {
                yield return null;
            }
        }

        // ----- Con dobles -----

        private IEnumerator MechaConDobles(string rutaPrefab)
        {
            yield return Calentar();
            GameObject piso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piso.transform.position = new Vector3(0f, -0.5f, 0f);
            piso.transform.localScale = new Vector3(400f, 1f, 400f);
            creados.Add(piso);
            mecha = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(rutaPrefab), PosicionMecha, Quaternion.identity);
            creados.Add(mecha);
            pantalla = mecha.GetComponentInChildren<PantallaAdaptacionCabina>(true);
            Assert.IsNotNull(pantalla, "El mecha trae la pantalla de adaptación");
            yield return null;
        }

        private DobleAdaptacion NuevoDoble(string nombre, float distancia, int guardia = 0, int fintas = 0, int retirada = 0)
        {
            GameObject objeto = new GameObject(nombre);
            objeto.transform.position = PosicionMecha + Vector3.forward * distancia;
            DobleAdaptacion doble = objeto.AddComponent<DobleAdaptacion>();
            doble.niveles[0] = guardia;
            doble.niveles[1] = fintas;
            doble.niveles[2] = retirada;
            creados.Add(objeto);
            return doble;
        }

        [UnityTest]
        public IEnumerator SinRival_Oculta()
        {
            yield return MechaConDobles(RutaJugador);
            yield return Esperar(EsperaReeleccion);
            Assert.IsFalse(pantalla.Visible);
            Assert.IsNull(pantalla.Rival);
            Assert.AreEqual(0, errores.Count, string.Join("\n", errores));
        }

        [UnityTest]
        public IEnumerator Guardia1_MuestraFilaConHabito()
        {
            yield return MechaConDobles(RutaJugador);
            DobleAdaptacion doble = NuevoDoble("Rival", DistanciaCerca, guardia: 1);
            yield return Esperar(0.1f);
            Assert.IsTrue(pantalla.Visible);
            Assert.AreEqual("TE TIENE ESTUDIADO", pantalla.TextoTitulo);
            Assert.AreEqual(1, pantalla.FilasVisibles);
            Assert.AreEqual("GUARDIA I · TIRAS MUCHOS PUÑOS", pantalla.TextoFila(0));
            Assert.AreSame(doble, pantalla.Rival);

            doble.niveles[1] = 2;
            doble.CambiarNiveles();
            yield return null;
            Assert.AreEqual(2, pantalla.FilasVisibles);
            Assert.AreEqual("FINTAS II · TE CUBRES MUCHO", pantalla.TextoFila(1));
            Assert.AreEqual(0, errores.Count, string.Join("\n", errores));
        }

        [UnityTest]
        public IEnumerator Nivel0_NoSeVeAunqueHayaRival()
        {
            yield return MechaConDobles(RutaJugador);
            NuevoDoble("Rival", DistanciaCerca);
            yield return Esperar(0.1f);
            Assert.IsNotNull(pantalla.Rival);
            Assert.IsFalse(pantalla.Visible);
        }

        [UnityTest]
        public IEnumerator Usada_ParpadeaLaFilaYVuelve()
        {
            yield return MechaConDobles(RutaJugador);
            DobleAdaptacion doble = NuevoDoble("Rival", DistanciaCerca, guardia: 1, fintas: 1);
            yield return Esperar(0.1f);
            Color baseColor = config.ColorBase;
            Assert.AreEqual(baseColor, pantalla.ColorFila(0));
            doble.Usar(TipoAdaptacion.Guardia);
            yield return null;
            Assert.IsTrue(pantalla.Parpadeando(TipoAdaptacion.Guardia));
            Assert.IsFalse(pantalla.Parpadeando(TipoAdaptacion.Fintas), "Solo la usada");
            bool vioColorDeAviso = false;
            float fin = Time.time + config.SegundosParpadeo * 0.9f;
            while (Time.time < fin)
            {
                vioColorDeAviso |= pantalla.ColorFila(0) == config.ColorParpadeo;
                Assert.AreEqual(baseColor, pantalla.ColorFila(1), "La otra fila no cambia");
                yield return null;
            }
            Assert.IsTrue(vioColorDeAviso, "La fila cambió al color del parpadeo");
            yield return Esperar(config.SegundosParpadeo * 0.3f);
            Assert.IsFalse(pantalla.Parpadeando(TipoAdaptacion.Guardia));
            Assert.AreEqual(baseColor, pantalla.ColorFila(0), "Vuelve al color de base");
            Assert.AreEqual(0, errores.Count, string.Join("\n", errores));
        }

        [UnityTest]
        public IEnumerator Aprendio_MuestraLineasYPistaDelPrimeroQueSubio()
        {
            yield return MechaConDobles(RutaJugador);
            DobleAdaptacion doble = NuevoDoble("Rival", DistanciaCerca, fintas: 1);
            yield return Esperar(0.1f);
            doble.Aprender(new CambioAdaptacion(TipoAdaptacion.Fintas, 0, 1), new CambioAdaptacion(TipoAdaptacion.Guardia, 1, 0));
            yield return null;
            Assert.IsTrue(pantalla.FinVisible);
            Assert.AreEqual("APRENDIÓ: FINTAS I", pantalla.TextoFin(0));
            Assert.AreEqual("OLVIDÓ: GUARDIA", pantalla.TextoFin(1));
            Assert.AreEqual("", pantalla.TextoFin(2));
            Assert.AreEqual("NO TE ESCONDAS: ATACA TÚ", pantalla.TextoPista);
            Assert.IsTrue(pantalla.Visible);

            // Se quita al empezar la siguiente pelea.
            EventosPelea.PublicarInicio();
            yield return null;
            Assert.IsFalse(pantalla.FinVisible);
            Assert.AreEqual("", pantalla.TextoFin(0));
            Assert.AreEqual("", pantalla.TextoPista);
            Assert.AreEqual(0, errores.Count, string.Join("\n", errores));
        }

        [UnityTest]
        public IEnumerator Aprendio_SinNingunaActivaSeVeHastaElAvisoYSeApaga()
        {
            yield return MechaConDobles(RutaJugador);
            DobleAdaptacion doble = NuevoDoble("Rival", DistanciaCerca);
            yield return Esperar(0.1f);
            doble.Aprender(new CambioAdaptacion(TipoAdaptacion.Guardia, 1, 0));
            yield return null;
            Assert.IsTrue(pantalla.Visible, "El aviso de fin la deja visible aunque no haya filas");
            Assert.AreEqual("OLVIDÓ: GUARDIA", pantalla.TextoFin(0));
            Assert.AreEqual("", pantalla.TextoPista, "Sin nada que subió no hay pista");
            Time.timeScale = EscalaTiempo;
            yield return Esperar(config.SegundosAvisoFin + 1f);
            Time.timeScale = 1f;
            Assert.IsFalse(pantalla.FinVisible);
            Assert.IsFalse(pantalla.Visible);
            Assert.AreEqual(0, errores.Count, string.Join("\n", errores));
        }

        [UnityTest]
        public IEnumerator DosRivales_ElegidoElMasCercano()
        {
            yield return MechaConDobles(RutaJugador);
            DobleAdaptacion lejos = NuevoDoble("Lejos", DistanciaLejos, retirada: 1);
            DobleAdaptacion cerca = NuevoDoble("Cerca", DistanciaCerca, guardia: 1);
            yield return Esperar(EsperaReeleccion);
            Assert.AreSame(cerca, pantalla.Rival);
            Assert.AreEqual("GUARDIA I · TIRAS MUCHOS PUÑOS", pantalla.TextoFila(0));
            Object.Destroy(cerca.gameObject);
            yield return Esperar(EsperaReeleccion);
            Assert.AreSame(lejos, pantalla.Rival, "Al quitarse el cercano elige al otro");
            Assert.AreEqual("RETIRADA I · LE PEGAS CUANDO FALLA", pantalla.TextoFila(0));
            Assert.AreEqual(0, errores.Count, string.Join("\n", errores));
        }

        [UnityTest]
        public IEnumerator RivalA61Metros_NoCuenta()
        {
            yield return MechaConDobles(RutaJugador);
            // La cámara está a 3.5 m de altura: se mide desde ella.
            float alturaCamara = pantalla.transform.root.GetComponentInChildren<Camera>(true).transform.position.y - PosicionMecha.y;
            float distancia = Mathf.Sqrt(DistanciaFuera * DistanciaFuera - alturaCamara * alturaCamara) + 0.5f;
            NuevoDoble("Lejisimos", distancia, guardia: 1);
            yield return Esperar(EsperaReeleccion);
            Assert.IsNull(pantalla.Rival);
            Assert.IsFalse(pantalla.Visible);
        }

        [UnityTest]
        public IEnumerator CabinaApagada_NoSeVe()
        {
            yield return MechaConDobles(RutaEstacionado);
            NuevoDoble("Rival", DistanciaCerca, guardia: 1);
            yield return Esperar(EsperaReeleccion);
            Assert.IsFalse(pantalla.Visible, "El mecha estacionado tiene la cabina apagada");
            Assert.AreEqual(0, errores.Count, string.Join("\n", errores));
        }

        [UnityTest]
        public IEnumerator DobleDestruido_NoDejaErrores()
        {
            yield return MechaConDobles(RutaJugador);
            DobleAdaptacion doble = NuevoDoble("Rival", DistanciaCerca, guardia: 1);
            yield return Esperar(0.1f);
            Assert.IsTrue(pantalla.Visible);
            Object.Destroy(doble.gameObject);
            yield return Esperar(EsperaReeleccion);
            Assert.IsNull(pantalla.Rival);
            Assert.IsFalse(pantalla.Visible);
            Assert.AreEqual(0, errores.Count, string.Join("\n", errores));
        }

        // ----- Con un rival de verdad -----

        private IEnumerator EscenarioReal(int evidenciaGuardia = 0, int evidenciaFintas = 0)
        {
            yield return Calentar();
            GameObject servicios = new GameObject("MemoriaRivales");
            creados.Add(servicios);
            MemoriaRivales memoria = servicios.AddComponent<MemoriaRivales>();
            MemoriaRival entrada = memoria.Obtener(IdLiga11);
            entrada.evidencias[(int)TipoAdaptacion.Guardia] = evidenciaGuardia;
            entrada.evidencias[(int)TipoAdaptacion.Fintas] = evidenciaFintas;

            GameObject arena = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaArena), PosicionArena, Quaternion.identity);
            creados.Add(arena);
            Pose inicio = arena.GetComponent<IArenaPelea>().InicioJugador;
            mecha = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaJugador), inicio.position, inicio.rotation);
            creados.Add(mecha);
            pantalla = mecha.GetComponentInChildren<PantallaAdaptacionCabina>(true);
            Assert.IsNotNull(pantalla);
            golpes = new GolpeEnPrueba();
            mecha.GetComponent<MechaCyberpunk.Combate.Golpe.GolpeMecha>().Impacto += () => golpes.impactos++;

            configFlujo = ScriptableObject.CreateInstance<ConfigFlujoPelea>();
            FijarFloat(configFlujo, "segundosPorNumero", 0.3f);
            FijarFloat(configFlujo, "pausaDespuesKO", 0.3f);
            FijarFloat(configFlujo, "segundosTextoPelea", 0.3f);

            GameObject padre = new GameObject("PadreInactivo");
            padre.SetActive(false);
            GameObject flujo = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaFlujo), padre.transform);
            creados.Add(flujo);
            creados.Add(padre);
            director = flujo.GetComponent<DirectorPelea>();
            SerializedObject soDirector = new SerializedObject(director);
            soDirector.FindProperty("config").objectReferenceValue = configFlujo;
            soDirector.FindProperty("prefabRival").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(RutaLiga11);
            soDirector.ApplyModifiedPropertiesWithoutUndo();
            SerializedObject soPantalla = new SerializedObject(flujo.GetComponentInChildren<PantallaPelea>(true));
            soPantalla.FindProperty("config").objectReferenceValue = configFlujo;
            soPantalla.ApplyModifiedPropertiesWithoutUndo();
            flujo.transform.position = PosicionArena;
            flujo.transform.SetParent(null);
            yield return null;
            yield return null;
        }

        private static void FijarFloat(Object objeto, string campo, float valor)
        {
            SerializedObject so = new SerializedObject(objeto);
            so.FindProperty(campo).floatValue = valor;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private IEnumerator HastaEstado(EstadoFlujoPelea estado)
        {
            float limite = Time.time + EsperaMaxima;
            while (director.Estado != estado && Time.time < limite)
            {
                yield return null;
            }
            Assert.AreEqual(estado, director.Estado, "No llegó al estado esperado");
        }

        private IEnumerator ClicIzquierdo()
        {
            Press(raton.leftButton);
            yield return null;
            Release(raton.leftButton);
            yield return Esperar(SegundosEntreClics);
        }

        private IEnumerator PelearConPunos()
        {
            yield return HastaEstado(EstadoFlujoPelea.Peleando);
            Time.timeScale = EscalaTiempo;
            float limite = Time.time + LimiteSegundos;
            while (golpes.impactos < GolpesParaPunos && Time.time < limite)
            {
                yield return ClicIzquierdo();
            }
            Assert.GreaterOrEqual(golpes.impactos, GolpesParaPunos);
        }

        private IEnumerator TerminarGanando()
        {
            ((MechaCyberpunk.Combate.Vida.VidaMecha)director.Rival.Vida).RecibirDanio(
                new InfoDanio(DanioMortal, TipoDanio.Golpe, Vector3.zero, Vector3.forward, null));
            yield return HastaEstado(EstadoFlujoPelea.Resultado);
            Time.timeScale = 1f;
            yield return null;
        }

        private IEnumerator Capturar(string ruta)
        {
            if (Environment.GetEnvironmentVariable(VariableCaptura) != "1")
            {
                yield break;
            }
            yield return null;
            Camera camara = mecha.GetComponentInChildren<Camera>();
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
            Directory.CreateDirectory(Path.GetDirectoryName(ruta));
            File.WriteAllBytes(ruta, imagen.EncodeToPNG());
            Object.Destroy(imagen);
            destino.Release();
            Object.Destroy(destino);
        }

        [UnityTest]
        public IEnumerator Real_DosFilasEnPelea()
        {
            yield return EscenarioReal(evidenciaGuardia: 1, evidenciaFintas: 1);
            yield return HastaEstado(EstadoFlujoPelea.Peleando);
            yield return Esperar(0.3f);
            Assert.IsTrue(pantalla.Visible);
            Assert.AreEqual(2, pantalla.FilasVisibles);
            Assert.AreEqual("GUARDIA I · TIRAS MUCHOS PUÑOS", pantalla.TextoFila(0));
            Assert.AreEqual("FINTAS I · TE CUBRES MUCHO", pantalla.TextoFila(1));
            yield return Capturar(RutaCaptura1);
            Assert.AreEqual(0, errores.Count, string.Join("\n", errores));
        }

        [UnityTest]
        public IEnumerator Real_PelearConPunos_ResultadoDiceAprendioYRevanchaMuestraGuardia()
        {
            yield return EscenarioReal();
            yield return HastaEstado(EstadoFlujoPelea.Conteo);
            Assert.IsFalse(pantalla.Visible, "Sin nada aprendido no hay pantalla");
            yield return PelearConPunos();
            yield return TerminarGanando();

            Assert.IsTrue(pantalla.Visible, "En el resultado se ve el aviso");
            Assert.IsTrue(pantalla.FinVisible);
            bool hayLinea = false;
            for (int i = 0; i < 3; i++)
            {
                hayLinea |= pantalla.TextoFin(i) == "APRENDIÓ: GUARDIA I";
            }
            Assert.IsTrue(hayLinea, "Dice APRENDIÓ: GUARDIA I (" + pantalla.TextoFin(0) + " | " + pantalla.TextoFin(1) + ")");
            Assert.AreEqual("APRENDIÓ: GUARDIA I", pantalla.TextoFin(0), "Guardia va primero");
            Assert.AreEqual("PÉGALE JUSTO DESPUÉS DE SU GOLPE", pantalla.TextoPista);
            Assert.GreaterOrEqual(pantalla.FilasVisibles, 1);
            Assert.AreEqual("GUARDIA I · TIRAS MUCHOS PUÑOS", pantalla.TextoFila(0));
            yield return Capturar(RutaCaptura2);

            ((MechaCyberpunk.Combate.Vida.VidaMecha)director.Jugador.Vida).Restaurar();
            Press(teclado.rKey);
            yield return null;
            Release(teclado.rKey);
            yield return null;
            yield return null;
            Assert.AreEqual(EstadoFlujoPelea.Conteo, director.Estado);
            Assert.IsTrue(pantalla.Visible, "En la revancha la fila está desde la presentación");
            Assert.IsFalse(pantalla.FinVisible, "El aviso de fin se quitó");
            Assert.AreEqual("GUARDIA I · TIRAS MUCHOS PUÑOS", pantalla.TextoFila(0));
            Assert.AreEqual(0, errores.Count, string.Join("\n", errores));
        }
    }
}
