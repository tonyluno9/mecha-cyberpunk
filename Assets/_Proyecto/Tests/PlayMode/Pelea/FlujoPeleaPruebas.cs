using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using MechaCyberpunk.Combate.Vida;
using MechaCyberpunk.Core;
using MechaCyberpunk.Pelea.Datos;
using MechaCyberpunk.Pelea.Flujo;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Pelea
{
    /// <summary>
    /// Pruebas jugables de T-022: arena, MechaJugador y FlujoPelea reales (prefabs) sin campos asignados, con un
    /// ConfigFlujoPelea de prueba de tiempos cortos, y la escena real Pruebas_Tony.
    /// </summary>
    public class FlujoPeleaPruebas : InputTestFixture
    {
        private const string RutaPrefabArena = "Assets/_Proyecto/Pelea/Pruebas/ArenaDePrueba.prefab";
        private const string RutaPrefabFlujo = "Assets/_Proyecto/Pelea/Prefabs/FlujoPelea.prefab";
        private const string RutaPrefabJugador = "Assets/_Proyecto/Mecha/Prefabs/MechaJugador.prefab";
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Pruebas_Tony.unity";
        private const int FramesEstables = 10;
        private const int MaxFramesCalentamiento = 1000;
        private const float DeltaTimeEstable = 0.05f;

        private const float SegundosPorNumero = 0.3f;
        private const float PausaKO = 0.3f;
        private const float SegundosTextoPelea = 0.3f;
        private const float EsperaMaxima = 15f;
        private const float DanioMortal = 100000f;
        private const float MargenPosicion = 0.1f;
        private const float MargenGrados = 5f;
        private const float EsperaCorta = 0.2f;

        private static readonly Vector3 PosicionArena = new Vector3(1000f, 0f, 0f);
        private static readonly Vector3 PosicionArenaLejana = new Vector3(1000f, 0f, 1000f);

        private Keyboard teclado;
        private readonly List<GameObject> creados = new List<GameObject>();
        private readonly List<string> avisos = new List<string>();
        private readonly List<string> errores = new List<string>();
        private readonly List<int> conteos = new List<int>();
        private readonly List<ResultadoPelea> resultados = new List<ResultadoPelea>();
        private ConfigFlujoPelea configPrueba;
        private GameObject jugador;
        private DirectorPelea director;

        public override void Setup()
        {
            base.Setup();
            EventosPelea.LimpiarSuscriptores();
            teclado = InputSystem.AddDevice<Keyboard>();
            InputSystem.AddDevice<Mouse>();
            avisos.Clear();
            errores.Clear();
            conteos.Clear();
            resultados.Clear();
            Application.logMessageReceived += RegistrarAviso;
            EventosPelea.ConteoCambio += conteos.Add;
            EventosPelea.PeleaTerminada += resultados.Add;
        }

        public override void TearDown()
        {
            Application.logMessageReceived -= RegistrarAviso;
            foreach (GameObject objeto in creados)
            {
                if (objeto != null)
                {
                    Object.Destroy(objeto);
                }
            }
            creados.Clear();
            Scene escena = SceneManager.GetSceneByPath(RutaEscena);
            if (escena.IsValid())
            {
                foreach (GameObject raiz in escena.GetRootGameObjects())
                {
                    Object.Destroy(raiz);
                }
            }
            if (configPrueba != null)
            {
                Object.Destroy(configPrueba);
            }
            EventosPelea.LimpiarSuscriptores();
            base.TearDown();
        }

        private void RegistrarAviso(string mensaje, string pila, LogType tipo)
        {
            if (tipo == LogType.Warning)
            {
                avisos.Add(mensaje);
            }
            else if (tipo == LogType.Error || tipo == LogType.Exception || tipo == LogType.Assert)
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

        private T Registrar<T>(T objeto) where T : Object
        {
            if (objeto is GameObject go)
            {
                creados.Add(go);
            }
            return objeto;
        }

        private GameObject CrearArena(Vector3 posicion)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefabArena);
            return Registrar(Object.Instantiate(prefab, posicion, Quaternion.identity));
        }

        private GameObject CrearJugador(Vector3 posicion)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefabJugador);
            jugador = Registrar(Object.Instantiate(prefab, posicion, Quaternion.identity));
            return jugador;
        }

        private static void FijarFloat(Object objeto, string campo, float valor)
        {
            SerializedObject so = new SerializedObject(objeto);
            so.FindProperty(campo).floatValue = valor;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // Instancia FlujoPelea dentro de un padre inactivo para cambiarle el config antes de su Awake.
        private DirectorPelea CrearFlujo(Vector3 posicion, float pausaKO = PausaKO)
        {
            configPrueba = ScriptableObject.CreateInstance<ConfigFlujoPelea>();
            FijarFloat(configPrueba, "segundosPorNumero", SegundosPorNumero);
            FijarFloat(configPrueba, "pausaDespuesKO", pausaKO);
            FijarFloat(configPrueba, "segundosTextoPelea", SegundosTextoPelea);

            GameObject padre = new GameObject("PadreInactivo");
            padre.SetActive(false);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefabFlujo);
            GameObject flujo = Object.Instantiate(prefab, padre.transform);
            Registrar(flujo);
            Registrar(padre);
            director = flujo.GetComponent<DirectorPelea>();
            SerializedObject soDirector = new SerializedObject(director);
            soDirector.FindProperty("config").objectReferenceValue = configPrueba;
            soDirector.ApplyModifiedPropertiesWithoutUndo();
            SerializedObject soPantalla = new SerializedObject(flujo.GetComponentInChildren<PantallaPelea>(true));
            soPantalla.FindProperty("config").objectReferenceValue = configPrueba;
            soPantalla.ApplyModifiedPropertiesWithoutUndo();

            flujo.transform.position = posicion;
            flujo.transform.SetParent(null);
            return director;
        }

        private IEnumerator Escenario(bool conArena = true, float pausaKO = PausaKO)
        {
            yield return Calentar();
            if (conArena)
            {
                GameObject arena = CrearArena(PosicionArena);
                CrearJugador(arena.GetComponent<IArenaPelea>().InicioJugador.position);
            }
            else
            {
                CrearJugador(PosicionArena);
            }
            CrearFlujo(PosicionArena, pausaKO);
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

        private static void Matar(IPeleador peleador)
        {
            ((VidaMecha)peleador.Vida).RecibirDanio(
                new InfoDanio(DanioMortal, TipoDanio.Golpe, Vector3.zero, Vector3.forward, null));
        }

        private IEnumerator PulsarTecla(Key tecla)
        {
            Press(teclado[tecla]);
            yield return null;
            Release(teclado[tecla]);
            yield return null;
        }

        private static float DistanciaAPose(Transform t, Pose pose)
        {
            return Vector3.Distance(t.position, pose.position);
        }

        private static float GradosAPose(Transform t, Pose pose)
        {
            return Quaternion.Angle(t.rotation, pose.rotation);
        }

        [UnityTest]
        public IEnumerator Director_SinCamposAsignados_EncuentraLaArenaYColocaAJugadorYRival()
        {
            yield return Escenario();
            yield return null;
            yield return null;

            IArenaPelea arena = director.Arena;
            Assert.IsNotNull(arena, "Encontró la arena por el registro");
            Assert.AreEqual("Arena de prueba", arena.Nombre);
            Assert.IsNotNull(director.Jugador);
            Assert.IsNotNull(director.Rival, "Creó al rival desde su prefab");
            Assert.AreSame(jugador.transform, director.Jugador.Raiz);
            Assert.Less(DistanciaAPose(director.Jugador.Raiz, arena.InicioJugador), MargenPosicion);
            Assert.Less(GradosAPose(director.Jugador.Raiz, arena.InicioJugador), MargenGrados);
            Assert.Less(DistanciaAPose(director.Rival.Raiz, arena.InicioRival), MargenPosicion);
            Assert.Less(GradosAPose(director.Rival.Raiz, arena.InicioRival), MargenGrados);
            Assert.AreEqual(EstadoFlujoPelea.Conteo, director.Estado);
        }

        [UnityTest]
        public IEnumerator Director_ConDosArenas_UsaLaMasCercana()
        {
            yield return Calentar();
            GameObject arenaA = CrearArena(PosicionArena);
            GameObject arenaB = CrearArena(PosicionArenaLejana);
            CrearJugador(PosicionArena);
            CrearFlujo(PosicionArenaLejana + Vector3.right);
            yield return null;
            yield return null;

            Assert.AreSame(arenaB.GetComponent<IArenaPelea>(), director.Arena);
            Assert.Less(DistanciaAPose(director.Jugador.Raiz, arenaB.GetComponent<IArenaPelea>().InicioJugador), MargenPosicion);
            Assert.Greater(DistanciaAPose(director.Jugador.Raiz, arenaA.GetComponent<IArenaPelea>().InicioJugador), 100f);
        }

        [UnityTest]
        public IEnumerator Director_SinArena_AvisaUnaSolaVezSinExcepcionesYEmpiezaCuandoLlegaUna()
        {
            LogAssert.Expect(LogType.Warning, new Regex("ninguna arena"));
            yield return Escenario(false);
            yield return Esperar(0.5f);

            int avisosArena = avisos.FindAll(a => a.Contains("ninguna arena")).Count;
            Assert.AreEqual(1, avisosArena, "Avisa una sola vez");
            Assert.AreEqual(EstadoFlujoPelea.Preparando, director.Estado);
            Assert.IsNull(director.Rival, "No crea al rival sin arena");
            Assert.AreEqual(0, errores.Count);

            CrearArena(PosicionArena);
            yield return null;
            yield return null;
            Assert.IsNotNull(director.Arena);
            Assert.AreEqual(EstadoFlujoPelea.Conteo, director.Estado, "Empieza al aparecer la arena");
            Assert.AreEqual(1, avisos.FindAll(a => a.Contains("ninguna arena")).Count);
        }

        [UnityTest]
        public IEnumerator Pantalla_AlArrancar_QuedaSobreLaPantallaDeLaArenaYAlineada()
        {
            yield return Escenario();
            yield return null;

            IArenaPelea arena = director.Arena;
            Transform lienzo = director.Pantalla.Lienzo.transform;
            Assert.Less(Vector3.Distance(lienzo.position, arena.PantallaArena.position), MargenPosicion);
            Assert.Less(Quaternion.Angle(lienzo.rotation, arena.PantallaArena.rotation), 1f);
            Assert.Greater(Vector3.Dot(lienzo.position - arena.PantallaArena.position, arena.PantallaArena.forward), -MargenPosicion);
            Assert.LessOrEqual(Vector3.Dot(lienzo.position - arena.PantallaArena.position, arena.PantallaArena.forward), 0f,
                "Sobresale hacia la jaula");
            RectTransform rect = lienzo.GetComponent<RectTransform>();
            Assert.LessOrEqual(rect.sizeDelta.x * lienzo.localScale.x, arena.TamanoPantalla.x + 1e-3f);
            Assert.LessOrEqual(rect.sizeDelta.y * lienzo.localScale.y, arena.TamanoPantalla.y + 1e-3f);
        }

        [UnityTest]
        public IEnumerator Conteo_MantenerW_NoMueveAlJugadorHastaDespuesDePelea()
        {
            yield return Escenario();
            yield return null;
            Vector3 inicio = jugador.transform.position;

            Press(teclado.wKey);
            while (director.Estado == EstadoFlujoPelea.Conteo)
            {
                Assert.AreEqual(0f, Vector3.Distance(inicio, jugador.transform.position), 0.01f, "Se movió durante el conteo");
                yield return null;
            }
            Assert.AreEqual(EstadoFlujoPelea.Peleando, director.Estado);
            Vector3 alEmpezar = jugador.transform.position;

            yield return Esperar(1f);
            Release(teclado.wKey);
            Assert.Greater(Vector3.Distance(alEmpezar, jugador.transform.position), 0.5f, "Después de ¡PELEA! sí se mueve");
        }

        [UnityTest]
        public IEnumerator Pantalla_RivalEnKO_MuestraConteoPeleaKoYGanasteConRevancha()
        {
            yield return Escenario();
            List<string> textos = new List<string>();
            string ultimo = null;
            while (director.Estado == EstadoFlujoPelea.Conteo || director.Estado == EstadoFlujoPelea.Preparando)
            {
                ultimo = Anotar(textos, ultimo);
                yield return null;
            }
            ultimo = Anotar(textos, ultimo);
            CollectionAssert.AreEqual(new[] { "3", "2", "1", "¡PELEA!" }, textos);
            CollectionAssert.AreEqual(new[] { 3, 2, 1, 0 }, conteos);

            Matar(director.Rival);
            while (director.Estado != EstadoFlujoPelea.Resultado)
            {
                ultimo = Anotar(textos, ultimo);
                yield return null;
            }
            ultimo = Anotar(textos, ultimo);
            CollectionAssert.AreEqual(new[] { "3", "2", "1", "¡PELEA!", "KO", "GANASTE" }, textos);
            Assert.AreEqual("R: revancha", director.Pantalla.Revancha);
            Assert.AreEqual(1, resultados.Count, "PeleaTerminada llega una vez");
            Assert.AreEqual(LadoPelea.Jugador, resultados[0].Ganador);
            Assert.Greater(resultados[0].DuracionSegundos, 0f);
        }

        private string Anotar(List<string> textos, string ultimo)
        {
            string actual = director.Pantalla.Texto;
            if (actual.Length > 0 && actual != ultimo)
            {
                textos.Add(actual);
                return actual;
            }
            return ultimo;
        }

        [UnityTest]
        public IEnumerator Pantalla_JugadorEnKO_MuestraPerdisteYGanaElRival()
        {
            yield return Escenario();
            yield return HastaEstado(EstadoFlujoPelea.Peleando);

            Matar(director.Jugador);
            yield return null;
            Assert.AreEqual("KO", director.Pantalla.Texto);
            yield return HastaEstado(EstadoFlujoPelea.Resultado);

            Assert.AreEqual("PERDISTE", director.Pantalla.Texto);
            Assert.AreEqual("R: revancha", director.Pantalla.Revancha);
            Assert.AreEqual(1, resultados.Count);
            Assert.AreEqual(LadoPelea.Rival, resultados[0].Ganador);
        }

        [UnityTest]
        public IEnumerator KO_DespuesDelKO_NiJugadorNiRivalTienenControl()
        {
            yield return Escenario();
            yield return HastaEstado(EstadoFlujoPelea.Peleando);
            Assert.IsTrue(director.Jugador.ControlActivo, "Con ¡PELEA! tienen control");
            Assert.IsTrue(director.Rival.ControlActivo);

            Matar(director.Rival);
            yield return null;
            Assert.AreEqual(EstadoFlujoPelea.KO, director.Estado);
            Assert.IsFalse(director.Jugador.ControlActivo);
            Assert.IsFalse(director.Rival.ControlActivo);

            yield return HastaEstado(EstadoFlujoPelea.Resultado);
            Assert.IsFalse(director.Jugador.ControlActivo);
            Assert.IsFalse(director.Rival.ControlActivo);
        }

        [UnityTest]
        public IEnumerator Revancha_RDuranteConteoOPelea_NoHaceNada()
        {
            yield return Escenario();
            yield return null;
            Assert.AreEqual(EstadoFlujoPelea.Conteo, director.Estado);

            yield return PulsarTecla(Key.R);
            Assert.AreEqual(EstadoFlujoPelea.Conteo, director.Estado);

            yield return HastaEstado(EstadoFlujoPelea.Peleando);
            yield return PulsarTecla(Key.R);
            yield return Esperar(EsperaCorta);
            Assert.AreEqual(EstadoFlujoPelea.Peleando, director.Estado);
            Assert.AreEqual(1, conteos.FindAll(n => n == 3).Count, "No volvió el conteo");
            Assert.IsTrue(director.Jugador.ControlActivo);
        }

        [UnityTest]
        public IEnumerator Revancha_REnElResultado_RivalCompletoYJugadorConservaSuDanioYVuelveElConteo()
        {
            yield return Escenario();
            yield return HastaEstado(EstadoFlujoPelea.Peleando);

            // La pelea deja al rival tirado y al jugador herido y movido de su sitio.
            Vida(director.Jugador).RecibirDanio(new InfoDanio(30f, TipoDanio.Golpe, Vector3.zero, Vector3.forward, null));
            Matar(director.Rival);
            yield return HastaEstado(EstadoFlujoPelea.Resultado);
            director.Jugador.Raiz.GetComponent<CharacterController>().enabled = false;
            director.Jugador.Raiz.position += new Vector3(3f, 0f, 2f);
            director.Jugador.Raiz.GetComponent<CharacterController>().enabled = true;
            Assert.IsTrue(Vida(director.Rival).EstaKO);

            yield return PulsarTecla(Key.R);

            Assert.AreEqual(EstadoFlujoPelea.Conteo, director.Estado);
            Assert.AreEqual(2, conteos.FindAll(n => n == 3).Count, "Vuelve el conteo");
            Assert.AreEqual("3", director.Pantalla.Texto);
            Assert.AreEqual(string.Empty, director.Pantalla.Revancha);
            // D-tony-59: este mecha (arranque Directo) no lleva MechaPersistente: la revancha lo restaura, igual que al rival.
            Assert.AreEqual(Vida(director.Jugador).Maxima, Vida(director.Jugador).Actual, 1e-3f);
            Assert.AreEqual(Vida(director.Rival).Maxima, Vida(director.Rival).Actual, 1e-3f);
            Assert.IsFalse(Vida(director.Rival).EstaKO);
            IArenaPelea arena = director.Arena;
            Assert.Less(DistanciaAPose(director.Jugador.Raiz, arena.InicioJugador), MargenPosicion);
            Assert.Less(DistanciaAPose(director.Rival.Raiz, arena.InicioRival), MargenPosicion);
            Transform visual = director.Rival.Raiz.Find("Visual");
            Assert.Less(Quaternion.Angle(visual.localRotation, Quaternion.identity), 1f, "El rival está de pie");
            Assert.IsFalse(director.Jugador.ControlActivo);
            Assert.IsFalse(director.Rival.ControlActivo);
        }

        private static VidaMecha Vida(IPeleador peleador) => (VidaMecha)peleador.Vida;

        [UnityTest]
        public IEnumerator Revancha_TresPeleasSeguidas_SinErrores()
        {
            yield return Escenario();
            LadoPelea[] ganadores = { LadoPelea.Jugador, LadoPelea.Rival, LadoPelea.Jugador };
            for (int i = 0; i < ganadores.Length; i++)
            {
                yield return HastaEstado(EstadoFlujoPelea.Peleando);
                Matar(ganadores[i] == LadoPelea.Jugador ? director.Rival : director.Jugador);
                yield return HastaEstado(EstadoFlujoPelea.Resultado);
                Assert.AreEqual(i + 1, resultados.Count);
                Assert.AreEqual(ganadores[i], resultados[i].Ganador, "Pelea " + (i + 1));
                if (i < ganadores.Length - 1)
                {
                    yield return PulsarTecla(Key.R);
                }
            }
            yield return Esperar(EsperaCorta);
            Assert.AreEqual(0, errores.Count, "Errores o excepciones en la consola: " + string.Join(" | ", errores));
        }

        // Arena sin pantalla válida, para probar el respaldo.
        private class ArenaSinPantalla : IArenaPelea
        {
            public Transform Pantalla;
            public Vector2 Tamano;
            public string Nombre => "Arena rota";
            public Pose InicioJugador => new Pose(PosicionArena + new Vector3(0f, 0f, -6f), Quaternion.identity);
            public Pose InicioRival => new Pose(PosicionArena + new Vector3(0f, 0f, 6f), Quaternion.Euler(0f, 180f, 0f));
            public Vector3 Centro => PosicionArena;
            public Transform PantallaArena => Pantalla;
            public Vector2 TamanoPantalla => Tamano;
        }

        [UnityTest]
        public IEnumerator Pantalla_KOEnElPrimerSegundoDespuesDePelea_ElKoSiguePuestoHastaElResultado()
        {
            yield return Escenario(true, 1f);
            yield return HastaEstado(EstadoFlujoPelea.Peleando);

            Matar(director.Rival);
            // Pasa de sobra el tiempo del texto "¡PELEA!" (0.3 s) con el KO en pantalla, sin importar si el texto está vacío.
            float fin = Time.time + SegundosTextoPelea + EsperaCorta;
            while (Time.time < fin)
            {
                yield return null;
                Assert.AreEqual(EstadoFlujoPelea.KO, director.Estado);
                Assert.AreEqual("KO", director.Pantalla.Texto, "El KO se borró antes del resultado");
            }
            yield return HastaEstado(EstadoFlujoPelea.Resultado);
            Assert.AreEqual("GANASTE", director.Pantalla.Texto);
        }

        [UnityTest]
        public IEnumerator Pantalla_ArenaSinPantallaNiTamano_AvisaUnaVezYUsaRespaldoVisible()
        {
            LogAssert.Expect(LogType.Warning, new Regex("PantallaArena"));
            ArenaSinPantalla rota = new ArenaSinPantalla { Pantalla = null, Tamano = Vector2.zero };
            Registro<IArenaPelea>.Agregar(rota);
            try
            {
                yield return Escenario(false);
                yield return null;

                Assert.AreSame(rota, director.Arena);
                Transform lienzo = director.Pantalla.Lienzo.transform;
                Assert.IsFalse(float.IsNaN(lienzo.position.x + lienzo.position.y + lienzo.position.z));
                Assert.Greater(lienzo.localScale.x, 0f, "El respaldo tiene tamaño visible");
                Assert.Greater(lienzo.position.y, 3f, "El respaldo queda alto");
                Assert.Greater(lienzo.position.z, rota.InicioRival.position.z - 0.01f, "Detrás del rival, no entre los dos");
                director.Pantalla.Colocar(rota);
                Assert.AreEqual(1, avisos.FindAll(a => a.Contains("PantallaArena")).Count, "Avisa una sola vez");
                Assert.AreEqual(0, errores.Count, string.Join(" | ", errores));
                Assert.AreEqual(EstadoFlujoPelea.Conteo, director.Estado, "La pelea sigue adelante");
            }
            finally
            {
                Registro<IArenaPelea>.Quitar(rota);
            }
        }

        [UnityTest]
        public IEnumerator Pantalla_TamanoCeroConPantallaValida_UsaTamanoDeRespaldo()
        {
            LogAssert.Expect(LogType.Warning, new Regex("PantallaArena"));
            GameObject marca = Registrar(new GameObject("PantallaFalsa"));
            marca.transform.position = PosicionArena + new Vector3(0f, 8f, 12f);
            ArenaSinPantalla rota = new ArenaSinPantalla { Pantalla = marca.transform, Tamano = new Vector2(0f, 3f) };
            Registro<IArenaPelea>.Agregar(rota);
            try
            {
                yield return Escenario(false);
                yield return null;

                Transform lienzo = director.Pantalla.Lienzo.transform;
                Assert.Greater(lienzo.localScale.x, 0f);
                Assert.IsFalse(float.IsInfinity(lienzo.localScale.x) || float.IsNaN(lienzo.localScale.x));
                Assert.Less(Vector3.Distance(lienzo.position, marca.transform.position), MargenPosicion + 0.1f,
                    "Con pantalla válida la coloca ahí aunque el tamaño sea de respaldo");
            }
            finally
            {
                Registro<IArenaPelea>.Quitar(rota);
            }
        }

        [UnityTest]
        public IEnumerator Director_ArenaDesaparece_LaSueltaYEsperaOtraSinUsarLaDestruidaEnLaRevancha()
        {
            LogAssert.Expect(LogType.Warning, new Regex("perdió su arena"));
            yield return Calentar();
            GameObject arena = CrearArena(PosicionArena);
            CrearJugador(arena.GetComponent<IArenaPelea>().InicioJugador.position);
            CrearFlujo(PosicionArena);
            yield return HastaEstado(EstadoFlujoPelea.Peleando);

            Matar(director.Rival);
            yield return HastaEstado(EstadoFlujoPelea.Resultado);
            Object.Destroy(arena);
            yield return null;
            yield return null;
            Assert.IsNull(director.Arena, "Soltó la arena destruida");

            yield return PulsarTecla(Key.R);
            yield return Esperar(EsperaCorta);
            Assert.AreEqual(EstadoFlujoPelea.Resultado, director.Estado, "Sin arena no hay revancha");
            Assert.AreEqual(0, errores.Count, string.Join(" | ", errores));
            Assert.AreEqual(1, avisos.FindAll(a => a.Contains("perdió su arena")).Count);

            GameObject otra = CrearArena(PosicionArenaLejana);
            yield return null;
            Assert.AreSame(otra.GetComponent<IArenaPelea>(), director.Arena, "Tomó la arena nueva");

            yield return PulsarTecla(Key.R);
            Assert.AreEqual(EstadoFlujoPelea.Conteo, director.Estado);
            Assert.Less(DistanciaAPose(director.Jugador.Raiz, director.Arena.InicioJugador), MargenPosicion);
            Assert.AreEqual(0, errores.Count, string.Join(" | ", errores));
        }

        [UnityTest]
        public IEnumerator Director_ApagadoYPrendidoMientrasEsperaArena_SigueEscuchandoLaLlegada()
        {
            LogAssert.Expect(LogType.Warning, new Regex("ninguna arena"));
            yield return Escenario(false);
            yield return Esperar(EsperaCorta);
            Assert.IsNull(director.Arena);

            director.enabled = false;
            yield return null;
            director.enabled = true;
            yield return null;

            CrearArena(PosicionArena);
            yield return null;
            yield return null;
            Assert.IsNotNull(director.Arena, "Siguió escuchando tras apagar y prender");
            Assert.AreEqual(EstadoFlujoPelea.Conteo, director.Estado);
            Assert.AreEqual(0, errores.Count, string.Join(" | ", errores));
        }

        [UnityTest]
        public IEnumerator Escena_TeclaP_IniciaLaPeleaEnLaArenaDePruebaYLlegaAPeleando()
        {
            yield return EditorSceneManager.LoadSceneInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            director = Object.FindFirstObjectByType<DirectorPelea>();
            Assert.IsNotNull(director, "Falta FlujoPelea en Pruebas_Tony");
            yield return Calentar();

            yield return Esperar(EsperaCorta);
            Assert.AreEqual(EstadoFlujoPelea.Preparando, director.Estado, "No arranca sola en esta escena");

            yield return PulsarTecla(Key.P);
            yield return null;
            Assert.AreEqual(EstadoFlujoPelea.Conteo, director.Estado);
            IArenaPelea arena = director.Arena;
            Assert.IsNotNull(arena);
            Assert.Less(DistanciaAPose(director.Jugador.Raiz, arena.InicioJugador), MargenPosicion);
            Assert.Less(DistanciaAPose(director.Rival.Raiz, arena.InicioRival), MargenPosicion);

            yield return HastaEstado(EstadoFlujoPelea.Peleando);
            Assert.IsTrue(director.Jugador.ControlActivo);
            Assert.IsTrue(director.Rival.ControlActivo);
        }
    }
}
