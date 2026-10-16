using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using MechaCyberpunk.Combate.Vida;
using MechaCyberpunk.Core;
using MechaCyberpunk.Mecha.Llamado;
using MechaCyberpunk.Mecha.Montaje;
using MechaCyberpunk.Pelea.Flujo;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Progresion.Guardado;
using MechaCyberpunk.Progresion.Prologo;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Tests.Progresion
{
    /// <summary>
    /// T-128: el mecha de la historia entra a la arena en Pruebas_Prologo. Se juega con entrada simulada (clics del ratón y R); lo único
    /// que no es entrada es rematar al rival con daño directo cuando el jugador ya recibió golpes reales (el tutorial no se puede perder).
    /// </summary>
    public class EntradaArenaPruebas : InputTestFixture
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Tony/Pruebas_Prologo.unity";
        private const string IdSalida = "Clandestino_Salida";
        private const int FramesEstables = 10;
        private const int MaxFramesCalentamiento = 1000;
        private const float DeltaTimeEstable = 0.05f;
        private const float MaxSegundosEstado = 60f;
        private const float MaxSegundosGolpes = 40f;
        private const float MaxSegundosSalida = 20f;
        private const float SegundosEntreClics = 0.25f;
        private const float DanioMortal = 100000f;
        private const float ToleranciaMecha = 0.1f;
        private const float ToleranciaJugador = 0.5f;
        private const float ToleranciaVida = 0.01f;
        private const float FraccionDanioBuscada = 0.9f;
        private const string VariableCaptura = "CAPTURAR_T128";
        private const string RutaCaptura = "docs/capturas/T-128-1.png";
        private const int AnchoCaptura = 1280;
        private const int AltoCaptura = 720;
        private const int ProfundidadCaptura = 24;
        private const int SaltoDePixeles = 97;
        private const float VariacionMinima = 0.05f;

        private Mouse raton;
        private Keyboard teclado;
        private string carpeta;
        private JugadorAPie jugador;
        private EntradaArena entrada;
        private DirectorPelea director;
        private MontajeMecha montaje;
        private LlamadoMecha llamado;
        private VidaMecha vidaMecha;
        private readonly List<string> errores = new List<string>();
        private readonly List<ResultadoPelea> terminadas = new List<ResultadoPelea>();

        public override void Setup()
        {
            base.Setup();
            Registros.LimpiarTodos();
            EventosPelea.LimpiarSuscriptores();
            teclado = InputSystem.AddDevice<Keyboard>();
            raton = InputSystem.AddDevice<Mouse>();
            carpeta = Path.Combine(Application.temporaryCachePath, "entrada_arena_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(carpeta);
            ServicioGuardado.RutaForzada = Path.Combine(carpeta, "partida.json");
            errores.Clear();
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
            base.TearDown();
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

        // ---- Preparación y utilidades ----

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
            entrada = Object.FindFirstObjectByType<EntradaArena>();
            director = Object.FindFirstObjectByType<DirectorPelea>();
            montaje = Object.FindFirstObjectByType<MontajeMecha>();
            Assert.IsNotNull(jugador, "Pruebas_Prologo trae al jugador");
            Assert.IsNotNull(entrada, "Pruebas_Prologo trae EntradaArena");
            Assert.IsNotNull(director, "Pruebas_Prologo trae el flujo de la pelea tutorial");
            Assert.IsNotNull(montaje, "Pruebas_Prologo trae el mecha de la historia");
            llamado = montaje.GetComponent<LlamadoMecha>();
            vidaMecha = montaje.GetComponent<VidaMecha>();
            EventosPelea.PeleaTerminada += terminadas.Add;
            yield return null;
        }

        private static int OyentesActivos()
        {
            int oyentes = 0;
            foreach (AudioListener oyente in Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (oyente.enabled && oyente.gameObject.activeInHierarchy)
                {
                    oyentes++;
                }
            }
            return oyentes;
        }

        private static int RivalesVivos()
        {
            int cuenta = 0;
            foreach (MonoBehaviour candidato in Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            {
                if (candidato is IPeleador peleador && peleador.Lado == LadoPelea.Rival)
                {
                    cuenta++;
                }
            }
            return cuenta;
        }

        // Espera una condición revisando en cada cuadro que haya exactamente un oído activo.
        private IEnumerator EsperarHasta(Func<bool> condicion, float maxSegundos, string que)
        {
            float fin = Time.time + maxSegundos;
            while (!condicion() && Time.time < fin)
            {
                Assert.AreEqual(1, OyentesActivos(), "Un solo AudioListener activo mientras se espera: " + que);
                yield return null;
            }
            Assert.IsTrue(condicion(), "Se esperaba: " + que);
        }

        private static float DistanciaHorizontal(Vector3 a, Vector3 b)
        {
            return Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));
        }

        private static IPuntoAparicion Aparicion(string id)
        {
            foreach (IPuntoAparicion punto in Registro<IPuntoAparicion>.Todos)
            {
                if (punto.Id == id)
                {
                    return punto;
                }
            }
            return null;
        }

        private static void Matar(IPeleador peleador)
        {
            ((VidaMecha)peleador.Vida).RecibirDanio(new InfoDanio(DanioMortal, TipoDanio.Golpe, Vector3.zero, Vector3.forward, null));
        }

        // Con la pelea en curso: clics al aire hasta que el rival lo golpea de verdad y deja al mecha con 90 % de vida o menos.
        private IEnumerator RecibirGolpesReales()
        {
            float limite = vidaMecha.Maxima * FraccionDanioBuscada;
            bool clic = false;
            float proximoClic = 0f;
            float fin = Time.time + MaxSegundosGolpes;
            while (vidaMecha.Actual > limite && !vidaMecha.EstaKO && Time.time < fin)
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
            Assert.Less(vidaMecha.Actual, vidaMecha.Maxima, "El jugador recibió golpes reales");
            Assert.IsFalse(vidaMecha.EstaKO, "Y no perdió");
        }

        private void Capturar()
        {
            if (Environment.GetEnvironmentVariable(VariableCaptura) != "1")
            {
                return;
            }
            Camera camara = jugador.Camara;
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

            float minimo = 1f;
            float maximo = 0f;
            Color32[] pixeles = imagen.GetPixels32();
            for (int i = 0; i < pixeles.Length; i += SaltoDePixeles)
            {
                float brillo = pixeles[i].g / 255f;
                minimo = Mathf.Min(minimo, brillo);
                maximo = Mathf.Max(maximo, brillo);
            }
            string completa = Path.GetFullPath(Path.Combine(Application.dataPath, "..", RutaCaptura));
            Directory.CreateDirectory(Path.GetDirectoryName(completa));
            File.WriteAllBytes(completa, imagen.EncodeToPNG());
            Object.Destroy(rt);
            Object.Destroy(imagen);
            Assert.Greater(maximo - minimo, VariacionMinima, "La captura es plana: no se ve nada");
        }

        // Una entrada completa: empezar, pelear, ganar y salir. Devuelve en 'vidaFinal' la vida con la que terminó el mecha.
        private IEnumerator JugarUnaEntrada(bool revisarTodo, Action<float> alTerminar)
        {
            Assert.IsTrue(entrada.Empezar(director), "Empezar acepta al jugador a pie");
            Assert.IsTrue(entrada.EnCurso, "EnCurso desde que empieza");
            Assert.IsFalse(entrada.Empezar(director), "Una segunda Empezar con otra en curso devuelve false");

            yield return EsperarHasta(() => jugador.Modo == ModoJugador.Mecha, MaxSegundosEstado, "el jugador va en el mecha");
            yield return null;
            Assert.AreEqual(EstadoLlamado.EnArena, llamado.Estado, "El mecha quedó en la arena");
            Assert.Less(DistanciaHorizontal(montaje.transform.position, director.Arena.InicioJugador.position), ToleranciaMecha,
                "El mecha está en el InicioJugador");
            Assert.IsFalse(entrada.Empezar(director), "Con el jugador en el mecha, Empezar devuelve false");

            yield return EsperarHasta(() => director.Estado == EstadoFlujoPelea.Presentacion || director.Estado == EstadoFlujoPelea.Conteo,
                MaxSegundosEstado, "hay presentación o conteo");
            if (revisarTodo)
            {
                Assert.AreEqual(EstadoFlujoPelea.Presentacion, director.Estado, "El tutorial trae presentación");
                Assert.IsNotEmpty(director.Pantalla.Texto, "La pantalla de la arena dice algo durante la presentación");
                yield return null;
                Capturar();
            }
            yield return EsperarHasta(() => director.Estado == EstadoFlujoPelea.Conteo, MaxSegundosEstado, "el conteo");
            yield return EsperarHasta(() => director.Estado == EstadoFlujoPelea.Peleando, MaxSegundosEstado, "la pelea");
            Assert.AreEqual(1, RivalesVivos(), "Un solo rival en escena");

            yield return RecibirGolpesReales();
            ICalor calor = Registro<ICalor>.Actual;
            if (calor != null)
            {
                Assert.AreEqual(0f, calor.Valor, "El calor sigue en 0 durante la pelea");
            }
            Matar(director.Rival);
            yield return EsperarHasta(() => director.Estado == EstadoFlujoPelea.Resultado, MaxSegundosEstado, "el resultado");
            float vidaFinal = vidaMecha.Actual;

            // R en el resultado no arranca una revancha y la pantalla no la ofrece.
            Press(teclado.rKey);
            yield return null;
            Release(teclado.rKey);
            yield return null;
            Assert.AreEqual(EstadoFlujoPelea.Resultado, director.Estado, "R no arranca una revancha");
            Assert.AreEqual(string.Empty, director.Pantalla.Revancha, "R: revancha no aparece");

            yield return EsperarHasta(() => !entrada.EnCurso, MaxSegundosSalida, "termina la salida");
            Assert.GreaterOrEqual(terminadas.Count, 1, "Hubo resultado de la pelea");
            alTerminar(vidaFinal);
        }

        // ---- Pruebas ----

        [UnityTest]
        public IEnumerator Empezar_DesdeAPie_PeleaConElMechaDeLaHistoriaYSaleAPieEnLaSalida()
        {
            yield return CargarEscena();
            Assert.AreEqual(ModoJugador.APie, jugador.Modo, "Empieza a pie");
            Assert.AreEqual(EstadoLlamado.EnTaller, llamado.Estado, "El mecha empieza en el taller");

            float vidaFinal = 0f;
            yield return JugarUnaEntrada(true, vida => vidaFinal = vida);
            yield return null;

            Assert.AreEqual(1, terminadas.Count, "La pelea terminó una vez");
            Assert.AreEqual(LadoPelea.Jugador, terminadas[0].Ganador, "Ganó el jugador");
            Assert.AreEqual(ModoJugador.APie, jugador.Modo, "El jugador quedó a pie");
            IPuntoAparicion salida = Aparicion(IdSalida);
            Assert.IsNotNull(salida, "Existe Clandestino_Salida");
            Assert.Less(DistanciaHorizontal(jugador.Raiz.position, salida.Pose.position), ToleranciaJugador, "A menos de 0.5 m de la salida");
            Assert.AreEqual(EstadoLlamado.EnTaller, llamado.Estado, "El mecha volvió al taller");
            Assert.Less(Vector3.Distance(montaje.transform.position, Registro<ITallerMecha>.Actual.Estacionamiento.position),
                ToleranciaMecha + 0.05f, "El mecha está en el estacionamiento");
            Assert.AreEqual(vidaFinal, vidaMecha.Actual, ToleranciaVida, "El mecha conserva la vida con que terminó");
            Assert.Less(vidaMecha.Actual, vidaMecha.Maxima, "Con el daño que recibió");
            Assert.AreEqual(0, RivalesVivos(), "El rival ya no existe");
            Assert.IsNull(director.Rival, "El director soltó al rival");
            Assert.AreEqual(EstadoFlujoPelea.Preparando, director.Estado, "El flujo quedó en Preparando");
            Assert.AreEqual(1, OyentesActivos(), "Un solo AudioListener activo al final");
            Assert.IsEmpty(errores, "Consola sin errores: " + string.Join(" | ", errores));
        }

        [UnityTest]
        public IEnumerator Empezar_DosVeces_LaSegundaFuncionaIgualConUnSoloRival()
        {
            yield return CargarEscena();

            float primera = 0f;
            yield return JugarUnaEntrada(false, vida => primera = vida);
            yield return null;
            Assert.AreEqual(ModoJugador.APie, jugador.Modo);
            Assert.AreEqual(0, RivalesVivos(), "Sin rival entre peleas");

            float segunda = 0f;
            yield return JugarUnaEntrada(false, vida => segunda = vida);
            yield return null;
            Assert.AreEqual(2, terminadas.Count, "Dos peleas terminadas");
            Assert.AreEqual(ModoJugador.APie, jugador.Modo, "A pie otra vez");
            Assert.AreEqual(EstadoLlamado.EnTaller, llamado.Estado, "Mecha otra vez en el taller");
            Assert.LessOrEqual(segunda, primera + ToleranciaVida, "El daño se acumula, no se restaura");
            Assert.AreEqual(0, RivalesVivos(), "Sin rival al final");
            Assert.IsEmpty(errores, "Consola sin errores: " + string.Join(" | ", errores));
        }

        [UnityTest]
        public IEnumerator Empezar_ConElJugadorEnElMecha_DevuelveFalseYNoCambiaNada()
        {
            yield return CargarEscena();
            Assert.IsTrue(entrada.Empezar(director));
            yield return EsperarHasta(() => jugador.Modo == ModoJugador.Mecha, MaxSegundosEstado, "el jugador va en el mecha");

            Assert.IsFalse(entrada.Empezar(director), "Con el jugador en el mecha devuelve false");
            Assert.AreEqual(ModoJugador.Mecha, jugador.Modo);
            Assert.AreEqual(1, RivalesVivos(), "Sigue un solo rival");
            Assert.IsEmpty(errores, "Consola sin errores: " + string.Join(" | ", errores));
        }

        [UnityTest]
        public IEnumerator Empezar_SinMechaEnLaEscena_AvisaConErrorYDevuelveFalse()
        {
            yield return CargarEscena();
            Object.Destroy(montaje.gameObject);
            yield return null;
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("EntradaArena no encontró el mecha"));

            Assert.IsFalse(entrada.Empezar(director), "Sin mecha no empieza");
            Assert.IsFalse(entrada.EnCurso);
            Assert.AreEqual(ModoJugador.APie, jugador.Modo);
        }
    }
}
