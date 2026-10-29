using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MechaCyberpunk.Combate.Golpe;
using MechaCyberpunk.Combate.Lectura;
using MechaCyberpunk.Combate.Vida;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.Adaptacion;
using MechaCyberpunk.IA.Datos;
using MechaCyberpunk.IA.Rival;
using MechaCyberpunk.Pelea.Datos;
using MechaCyberpunk.Pelea.Flujo;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.IA
{
    /// <summary>
    /// T-200: el rival de liga mira cómo pelea el jugador (entrada simulada: clic izquierdo, clic derecho y R) y lo apunta en su memoria.
    /// Atajos permitidos y anotados: Time.timeScale, RecibirDanio al rival solo después de daño real del jugador y Restaurar la vida del
    /// jugador antes de una revancha (el daño persiste, T-103).
    /// </summary>
    public class AdaptacionRivalPruebas : InputTestFixture
    {
        private const string RutaPrefabArena = "Assets/_Proyecto/Pelea/Pruebas/ArenaDePrueba.prefab";
        private const string RutaPrefabFlujo = "Assets/_Proyecto/Pelea/Prefabs/FlujoPelea.prefab";
        private const string RutaPrefabJugador = "Assets/_Proyecto/Mecha/Prefabs/MechaJugador.prefab";
        private const string RutaPrefabLiga11 = "Assets/_Proyecto/IA/Prefabs/Ligas/RivalLiga1_1.prefab";
        private const string RutaPrefabLiga12 = "Assets/_Proyecto/IA/Prefabs/Ligas/RivalLiga1_2.prefab";
        private const string RutaPrefabBasico = "Assets/_Proyecto/IA/Prefabs/RivalBasico.prefab";
        private const string RutaDatosBase = "Assets/_Proyecto/IA/Datos/DatosAdaptacionRival_Base.asset";
        private const string IdLiga11 = "DatosRival_Liga1_1";
        private const string IdLiga12 = "DatosRival_Liga1_2";
        private const int FramesEstables = 10;
        private const int MaxFramesCalentamiento = 1000;
        private const float DeltaTimeEstable = 0.05f;

        private const float EscalaTiempo = 4f;
        private const float EsperaMaxima = 15f;
        private const float LimiteSegundos = 120f;
        private const float DanioMortal = 100000f;
        private const float SegundosEntreClics = 0.1f;
        private const float SegundosPelea = 6f;
        private const int GolpesParaPunos = 10;
        private const int GolpesRivalParaCubrirse = 6;
        private const int ConectadosParaCastigar = 6;
        private const float Tolerancia = 1e-3f;
        private static readonly Vector3 PosicionArena = new Vector3(1000f, 0f, 0f);
        private static readonly Vector3 PosicionOtroRival = new Vector3(3000f, 0f, 0f);

        private Keyboard teclado;
        private Mouse raton;
        private readonly List<GameObject> creados = new List<GameObject>();
        private readonly List<string> errores = new List<string>();
        private readonly List<string> mensajes = new List<string>();
        private readonly List<ResultadoPelea> resultados = new List<ResultadoPelea>();
        private readonly List<float> danioAlJugador = new List<float>();
        private readonly List<float> danioAlRival = new List<float>();
        private ConfigFlujoPelea configPrueba;
        private GameObject jugador;
        private VidaMecha vidaJugador;
        private GolpeMecha golpeJugador;
        private DirectorPelea director;
        private MemoriaRivales memoria;
        private float escalaOriginal;
        private int impactosJugador;
        private int golpesRivalRecibidos;

        public override void Setup()
        {
            base.Setup();
            EventosPelea.LimpiarSuscriptores();
            teclado = InputSystem.AddDevice<Keyboard>();
            raton = InputSystem.AddDevice<Mouse>();
            errores.Clear();
            mensajes.Clear();
            resultados.Clear();
            danioAlJugador.Clear();
            danioAlRival.Clear();
            impactosJugador = 0;
            golpesRivalRecibidos = 0;
            memoria = null;
            escalaOriginal = Time.timeScale;
            Application.logMessageReceived += RegistrarMensaje;
            EventosPelea.PeleaTerminada += resultados.Add;
            EventosPelea.ImpactoRecibido += AnotarImpacto;
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
            if (configPrueba != null)
            {
                Object.Destroy(configPrueba);
            }
            EventosPelea.LimpiarSuscriptores();
            base.TearDown();
        }

        private void RegistrarMensaje(string mensaje, string pila, LogType tipo)
        {
            if (tipo == LogType.Error || tipo == LogType.Exception || tipo == LogType.Assert)
            {
                errores.Add(mensaje);
            }
            else if (tipo == LogType.Log && mensaje.StartsWith("[IA]"))
            {
                mensajes.Add(mensaje);
            }
        }

        // Daño real entre los dos mechas (con atacante), para comparar peleas distintas.
        private void AnotarImpacto(IPeleador objetivo, InfoDanio info, ResultadoDanio resultado)
        {
            if (director == null || info.Atacante == null || resultado.DanioAplicado <= 0f || resultado.Bloqueo != TipoBloqueo.Ninguno)
            {
                return;
            }
            if (objetivo == director.Jugador)
            {
                danioAlJugador.Add(resultado.DanioAplicado);
            }
            else if (objetivo == director.Rival)
            {
                danioAlRival.Add(resultado.DanioAplicado);
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

        private static void FijarFloat(Object objeto, string campo, float valor)
        {
            SerializedObject so = new SerializedObject(objeto);
            so.FindProperty(campo).floatValue = valor;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // Arena, jugador y FlujoPelea con el rival indicado; con o sin MemoriaRivales y con otro rival que se adapta en la escena.
        private IEnumerator Escenario(string rutaPrefabRival, bool conMemoria, bool conOtroRival = false)
        {
            yield return Calentar();
            if (conMemoria)
            {
                GameObject servicios = new GameObject("MemoriaRivales");
                creados.Add(servicios);
                memoria = servicios.AddComponent<MemoriaRivales>();
            }
            if (conOtroRival)
            {
                GameObject otro = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefabLiga12), PosicionOtroRival, Quaternion.identity);
                creados.Add(otro);
            }
            GameObject arena = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefabArena), PosicionArena, Quaternion.identity);
            creados.Add(arena);
            Pose inicio = arena.GetComponent<IArenaPelea>().InicioJugador;
            jugador = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefabJugador), inicio.position, inicio.rotation);
            creados.Add(jugador);
            vidaJugador = jugador.GetComponent<VidaMecha>();
            golpeJugador = jugador.GetComponent<GolpeMecha>();
            golpeJugador.Impacto += () => impactosJugador++;

            configPrueba = ScriptableObject.CreateInstance<ConfigFlujoPelea>();
            FijarFloat(configPrueba, "segundosPorNumero", 0.3f);
            FijarFloat(configPrueba, "pausaDespuesKO", 0.3f);
            FijarFloat(configPrueba, "segundosTextoPelea", 0.3f);

            GameObject padre = new GameObject("PadreInactivo");
            padre.SetActive(false);
            GameObject flujo = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefabFlujo), padre.transform);
            creados.Add(flujo);
            creados.Add(padre);
            director = flujo.GetComponent<DirectorPelea>();
            SerializedObject soDirector = new SerializedObject(director);
            soDirector.FindProperty("config").objectReferenceValue = configPrueba;
            soDirector.FindProperty("prefabRival").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(rutaPrefabRival);
            soDirector.ApplyModifiedPropertiesWithoutUndo();
            SerializedObject soPantalla = new SerializedObject(flujo.GetComponentInChildren<PantallaPelea>(true));
            soPantalla.FindProperty("config").objectReferenceValue = configPrueba;
            soPantalla.ApplyModifiedPropertiesWithoutUndo();
            flujo.transform.position = PosicionArena;
            flujo.transform.SetParent(null);
            yield return null;
            yield return null;
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

        private static void Danar(IPeleador peleador, float cantidad)
        {
            ((VidaMecha)peleador.Vida).RecibirDanio(
                new InfoDanio(cantidad, TipoDanio.Golpe, Vector3.zero, Vector3.forward, null));
        }

        private AdaptacionRival Adaptacion() => ((Component)director.Rival).GetComponent<AdaptacionRival>();
        private RivalBasico Basico() => ((Component)director.Rival).GetComponent<RivalBasico>();

        private IEnumerator ClicIzquierdo()
        {
            Press(raton.leftButton);
            yield return null;
            Release(raton.leftButton);
            yield return Esperar(SegundosEntreClics);
        }

        // El rival cae por daño después de que el jugador lo golpeó de verdad, y la pelea llega al resultado.
        private IEnumerator TerminarGanando()
        {
            Danar(director.Rival, DanioMortal);
            yield return HastaEstado(EstadoFlujoPelea.Resultado);
        }

        // Clic izquierdo seguido con la guardia abajo hasta tirar GolpesParaPunos golpes y pasar el tiempo mínimo; después gana.
        private IEnumerator PelearConPunos()
        {
            yield return HastaEstado(EstadoFlujoPelea.Peleando);
            Time.timeScale = EscalaTiempo;
            float inicio = Time.time;
            float limite = Time.time + LimiteSegundos;
            while ((impactosJugador < GolpesParaPunos || Time.time - inicio < SegundosPelea) && Time.time < limite)
            {
                yield return ClicIzquierdo();
                Assert.IsFalse(vidaJugador.EstaKO, "El jugador cayó antes de terminar");
            }
            Assert.GreaterOrEqual(impactosJugador, GolpesParaPunos);
        }

        [UnityTest]
        public IEnumerator Punos_ClicSeguidoGuardiaAbajo_PerfilIgualALosImpactosYAprendeGuardia()
        {
            yield return Escenario(RutaPrefabLiga11, true);
            AdaptacionRival adaptacion = null;
            IReadOnlyList<CambioAdaptacion> cambios = null;
            yield return HastaEstado(EstadoFlujoPelea.Conteo);
            adaptacion = Adaptacion();
            adaptacion.Aprendio += lista => cambios = lista;
            Assert.AreEqual(IdLiga11, adaptacion.IdRival);
            Assert.AreEqual(0, adaptacion.Nivel(TipoAdaptacion.Guardia));

            yield return PelearConPunos();
            PerfilEstilo perfil = adaptacion.PerfilActual;
            Assert.IsNotNull(perfil, "Observa mientras pelea");
            Assert.AreEqual(impactosJugador, perfil.GolpesLanzados, "Cuenta cada Impacto del GolpeMecha");
            int lanzados = perfil.GolpesLanzados;
            int conectados = perfil.GolpesConectados;
            Debug.Log("[Prueba] puños: lanzados " + lanzados + ", conectados " + conectados + ", castigando " + perfil.ConectadosCastigando
                + ", golpes del rival " + perfil.GolpesRival + ", cubiertos " + perfil.GolpesRivalCubiertos);

            yield return TerminarGanando();

            Assert.IsNull(adaptacion.PerfilActual, "Fuera de pelea no hay perfil");
            MemoriaRival recuerdo = memoria.Obtener(IdLiga11);
            Assert.AreEqual(1, recuerdo.evidencias[(int)TipoAdaptacion.Guardia]);
            Assert.AreEqual(0, recuerdo.evidencias[(int)TipoAdaptacion.Fintas], "Con la guardia abajo ningún golpe llegó cubierto");
            // Con puños seguidos algunos golpes caen justo después del golpe del rival: Retirada sale solo si el perfil lo dice.
            DatosAdaptacionRival datos = AssetDatabase.LoadAssetAtPath<DatosAdaptacionRival>(RutaDatosBase);
            bool esperaRetirada = ReglaEstilo.Vio(TipoAdaptacion.Retirada, perfil, resultados[0].DuracionSegundos, datos);
            Assert.AreEqual(esperaRetirada ? 1 : 0, recuerdo.evidencias[(int)TipoAdaptacion.Retirada]);
            Assert.AreEqual(1, recuerdo.peleas);
            Assert.AreEqual(1, adaptacion.Nivel(TipoAdaptacion.Guardia));
            Assert.IsNotNull(cambios, "Avisó Aprendio al cerrar la pelea");
            bool hayGuardia = false;
            foreach (CambioAdaptacion cambio in cambios)
            {
                if (cambio.Tipo == TipoAdaptacion.Guardia)
                {
                    hayGuardia = true;
                    Assert.AreEqual(0, cambio.NivelAntes);
                    Assert.AreEqual(1, cambio.NivelDespues);
                }
            }
            Assert.IsTrue(hayGuardia, "Aprendio trae Guardia 0 a 1");
            Assert.IsTrue(mensajes.Exists(m => m.Contains("aprendió: GUARDIA I")), string.Join("\n", mensajes));
            Assert.IsTrue(mensajes.Exists(m => m.Contains("observó: lanzados " + lanzados)), string.Join("\n", mensajes));
            Debug.Log("[Prueba] evidencia {" + recuerdo.evidencias[0] + ", " + recuerdo.evidencias[1] + ", " + recuerdo.evidencias[2] + "}");
            Assert.AreEqual(0, errores.Count, string.Join("\n", errores));
        }

        [UnityTest]
        public IEnumerator Revancha_R_RecuerdaYPeleaIgualQueAntes()
        {
            yield return Escenario(RutaPrefabLiga11, true);
            yield return HastaEstado(EstadoFlujoPelea.Conteo);
            AdaptacionRival adaptacion = Adaptacion();
            int cambiosDeNivel = 0;
            adaptacion.NivelesCambiados += () => cambiosDeNivel++;

            yield return PelearConPunos();
            Assert.AreEqual(0, cambiosDeNivel, "Los niveles no cambian a media pelea");
            yield return TerminarGanando();
            Assert.AreEqual(1, adaptacion.Nivel(TipoAdaptacion.Guardia));
            List<float> danioJugador1 = new List<float>(danioAlJugador);
            List<float> danioRival1 = new List<float>(danioAlRival);
            Assert.Greater(danioJugador1.Count + danioRival1.Count, 0, "Hubo golpes que comparar");

            vidaJugador.Restaurar();
            danioAlJugador.Clear();
            danioAlRival.Clear();
            Press(teclado.rKey);
            yield return null;
            Release(teclado.rKey);
            yield return null;
            yield return null;
            Assert.AreEqual(EstadoFlujoPelea.Conteo, director.Estado);
            Assert.AreEqual(1, adaptacion.Nivel(TipoAdaptacion.Guardia), "Al preparar la revancha recuerda");
            Assert.IsNotNull(adaptacion.PerfilActual, "Al preparar se arma con un perfil nuevo");
            Assert.AreEqual(0, adaptacion.PerfilActual.GolpesLanzados);

            yield return HastaEstado(EstadoFlujoPelea.Peleando);
            Time.timeScale = EscalaTiempo;
            int antes = impactosJugador;
            float limite = Time.time + LimiteSegundos;
            while ((impactosJugador - antes < GolpesParaPunos) && Time.time < limite)
            {
                yield return ClicIzquierdo();
            }

            Assert.AreEqual(impactosJugador - antes, adaptacion.PerfilActual.GolpesLanzados);
            // Nada de lo aprendido cambia todavía su forma de pelear: cada golpe recibido o dado hace el mismo daño.
            Assert.Greater(danioAlJugador.Count + danioAlRival.Count, 0);
            foreach (float d in danioAlJugador)
            {
                Assert.AreEqual(danioJugador1.Count > 0 ? danioJugador1[0] : d, d, Tolerancia, "El rival pega igual");
            }
            foreach (float d in danioAlRival)
            {
                Assert.AreEqual(danioRival1.Count > 0 ? danioRival1[0] : d, d, Tolerancia, "El jugador pega igual");
            }
            Assert.AreEqual(0, errores.Count, string.Join("\n", errores));
        }

        [UnityTest]
        public IEnumerator Cubrirse_ClicDerechoSostenido_SubeFintas()
        {
            yield return Escenario(RutaPrefabLiga11, true);
            yield return HastaEstado(EstadoFlujoPelea.Peleando);
            AdaptacionRival adaptacion = Adaptacion();
            Time.timeScale = EscalaTiempo;
            EventosPelea.ImpactoRecibido += (objetivo, info, r) =>
            {
                if (objetivo == director.Jugador && info.Atacante != null)
                {
                    golpesRivalRecibidos++;
                }
            };
            yield return ClicIzquierdo();
            Press(raton.rightButton);
            float limite = Time.time + LimiteSegundos;
            while ((golpesRivalRecibidos < GolpesRivalParaCubrirse) && Time.time < limite)
            {
                yield return null;
                Assert.IsFalse(vidaJugador.EstaKO, "El jugador cayó");
            }
            PerfilEstilo perfil = adaptacion.PerfilActual;
            Debug.Log("[Prueba] cubrirse: rival " + perfil.GolpesRival + ", cubiertos " + perfil.GolpesRivalCubiertos + ", lanzados "
                + perfil.GolpesLanzados);
            Assert.GreaterOrEqual(perfil.GolpesRival, GolpesRivalParaCubrirse);
            Assert.GreaterOrEqual(perfil.GolpesRivalCubiertos, perfil.GolpesRival / 2);
            Release(raton.rightButton);

            yield return TerminarGanando();

            MemoriaRival recuerdo = memoria.Obtener(IdLiga11);
            Assert.AreEqual(1, recuerdo.evidencias[(int)TipoAdaptacion.Fintas]);
            Assert.AreEqual(1, adaptacion.Nivel(TipoAdaptacion.Fintas));
            Assert.AreEqual(0, adaptacion.Nivel(TipoAdaptacion.Guardia), "Casi no tiró puños");
            Assert.IsTrue(mensajes.Exists(m => m.Contains("aprendió: FINTAS I")), string.Join("\n", mensajes));
            Assert.AreEqual(0, errores.Count, string.Join("\n", errores));
        }

        [UnityTest]
        public IEnumerator Castigar_GolpeJustoDespuesDelDelRival_SubeRetirada()
        {
            yield return Escenario(RutaPrefabLiga11, true);
            yield return HastaEstado(EstadoFlujoPelea.Peleando);
            AdaptacionRival adaptacion = Adaptacion();
            RivalBasico basico = Basico();
            Time.timeScale = EscalaTiempo;
            float limite = Time.time + LimiteSegundos;
            int ciclos = 0;
            while (adaptacion.PerfilActual.ConectadosCastigando < ConectadosParaCastigar && Time.time < limite && ciclos < 200)
            {
                // Espera a que el rival golpee (empieza a recuperarse) y contesta de inmediato.
                while (basico.Fase != FaseGolpe.Recuperando && Time.time < limite)
                {
                    yield return null;
                }
                yield return ClicIzquierdo();
                // Espera a que termine su recuperación antes del siguiente ciclo.
                while (basico.Fase == FaseGolpe.Recuperando && Time.time < limite)
                {
                    yield return null;
                }
                ciclos++;
                Assert.IsFalse(vidaJugador.EstaKO, "El jugador cayó");
            }
            PerfilEstilo perfil = adaptacion.PerfilActual;
            Debug.Log("[Prueba] castigar: conectados " + perfil.GolpesConectados + ", castigando " + perfil.ConectadosCastigando
                + ", lanzados " + perfil.GolpesLanzados + ", ciclos " + ciclos);
            Assert.GreaterOrEqual(perfil.ConectadosCastigando, ConectadosParaCastigar);
            Assert.AreEqual(perfil.GolpesConectados, perfil.ConectadosCastigando, "Cada conectado fue justo después de su golpe");

            yield return TerminarGanando();

            Assert.AreEqual(1, memoria.Obtener(IdLiga11).evidencias[(int)TipoAdaptacion.Retirada]);
            Assert.AreEqual(1, adaptacion.Nivel(TipoAdaptacion.Retirada));
            Assert.IsTrue(mensajes.Exists(m => m.Contains("aprendió: RETIRADA I")), string.Join("\n", mensajes));
            Assert.AreEqual(0, errores.Count, string.Join("\n", errores));
        }

        [UnityTest]
        public IEnumerator Memoria_GuardarOlvidarCargar_DevuelveLosNiveles()
        {
            yield return Escenario(RutaPrefabLiga11, true);
            yield return HastaEstado(EstadoFlujoPelea.Conteo);
            AdaptacionRival adaptacion = Adaptacion();
            Assert.AreSame(memoria, MemoriaRivales.Actual);
            Assert.AreEqual("adaptacion", memoria.ClaveGuardado);
            Assert.IsTrue(Registro<IGuardable>.Todos.Contains(memoria));
            yield return PelearConPunos();
            yield return TerminarGanando();
            Assert.AreEqual(1, adaptacion.Nivel(TipoAdaptacion.Guardia));

            string texto = memoria.Guardar();
            StringAssert.Contains(IdLiga11, texto);
            memoria.OlvidarTodo();
            Assert.AreEqual(0, adaptacion.Nivel(TipoAdaptacion.Guardia), "Olvidar baja el nivel fuera de pelea");
            memoria.Cargar(texto);
            Assert.AreEqual(1, adaptacion.Nivel(TipoAdaptacion.Guardia), "Cargar devuelve el nivel");
            Assert.GreaterOrEqual(adaptacion.Activas, 1, "Guardia siempre; Retirada si los golpes cayeron justo después de los del rival");
            Assert.AreEqual(0, errores.Count, string.Join("\n", errores));
        }

        [UnityTest]
        public IEnumerator SinMemoriaRivales_LaRevanchaRecuerdaYUnaInstanciaNuevaEmpiezaEnCero()
        {
            yield return Escenario(RutaPrefabLiga11, false);
            yield return HastaEstado(EstadoFlujoPelea.Conteo);
            AdaptacionRival adaptacion = Adaptacion();
            Assert.IsNull(MemoriaRivales.Actual);
            yield return PelearConPunos();
            yield return TerminarGanando();
            Assert.AreEqual(1, adaptacion.Nivel(TipoAdaptacion.Guardia), "Sin servicio recuerda en su memoria propia");

            vidaJugador.Restaurar();
            Press(teclado.rKey);
            yield return null;
            Release(teclado.rKey);
            yield return null;
            yield return null;
            Assert.AreEqual(1, adaptacion.Nivel(TipoAdaptacion.Guardia), "La revancha recuerda");

            GameObject nuevo = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefabLiga11), PosicionOtroRival, Quaternion.identity);
            creados.Add(nuevo);
            yield return null;
            yield return null;
            AdaptacionRival otra = nuevo.GetComponent<AdaptacionRival>();
            Assert.AreEqual(0, otra.Nivel(TipoAdaptacion.Guardia), "Una instancia nueva empieza en 0");
            Assert.AreEqual(0, otra.Activas);
            Assert.AreEqual(0, errores.Count, string.Join("\n", errores));
        }

        [UnityTest]
        public IEnumerator OtroRival_EnLaEscena_NoAprendeNada()
        {
            yield return Escenario(RutaPrefabLiga11, true, true);
            yield return HastaEstado(EstadoFlujoPelea.Conteo);
            Assert.AreEqual(2, Registro<ILecturaAdaptacion>.Cantidad, "Los dos rivales de liga se registran");
            AdaptacionRival peleando = Adaptacion();
            AdaptacionRival otro = null;
            foreach (ILecturaAdaptacion lectura in Registro<ILecturaAdaptacion>.Todos)
            {
                if (!ReferenceEquals(lectura, peleando))
                {
                    otro = (AdaptacionRival)lectura;
                }
            }
            Assert.IsNotNull(otro);
            Assert.AreEqual(IdLiga12, otro.IdRival);
            int avisos = 0;
            otro.Aprendio += lista => avisos++;

            yield return PelearConPunos();
            yield return TerminarGanando();

            Assert.AreEqual(1, peleando.Nivel(TipoAdaptacion.Guardia));
            Assert.AreEqual(0, otro.Activas);
            Assert.AreEqual(0, avisos, "El otro rival no cerró ninguna pelea");
            Assert.IsNull(otro.PerfilActual);
            Assert.IsNull(memoria.Buscar(IdLiga12), "Nada se guardó del otro rival");
            Assert.AreEqual(0, errores.Count, string.Join("\n", errores));
        }

        [UnityTest]
        public IEnumerator RivalBasico_SinAdaptacion_NoSeRegistraEnLaLectura()
        {
            yield return Escenario(RutaPrefabBasico, true);
            yield return HastaEstado(EstadoFlujoPelea.Conteo);

            Assert.IsNull(((Component)director.Rival).GetComponent<AdaptacionRival>());
            Assert.AreEqual(0, Registro<ILecturaAdaptacion>.Cantidad);
            yield return HastaEstado(EstadoFlujoPelea.Peleando);
            yield return TerminarGanando();
            Assert.AreEqual(0, memoria.Cantidad, "Nada se apunta de un rival sin adaptación");
            Assert.AreEqual(0, Registro<ILecturaAdaptacion>.Cantidad);
            Assert.AreEqual(0, errores.Count, string.Join("\n", errores));
        }

        [UnityTest]
        public IEnumerator Registro_AlDestruirseElRival_SeQuita()
        {
            yield return Escenario(RutaPrefabLiga11, true);
            yield return HastaEstado(EstadoFlujoPelea.Conteo);
            Assert.AreEqual(1, Registro<ILecturaAdaptacion>.Cantidad);
            GameObject rival = ((Component)director.Rival).gameObject;

            Object.Destroy(rival);
            yield return null;

            Assert.AreEqual(0, Registro<ILecturaAdaptacion>.Cantidad);
        }
    }
}
