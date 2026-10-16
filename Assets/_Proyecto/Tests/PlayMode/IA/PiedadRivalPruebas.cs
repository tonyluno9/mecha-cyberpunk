using System.Collections;
using System.Collections.Generic;
using MechaCyberpunk.Combate.Vida;
using MechaCyberpunk.Core;
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
    /// Pruebas jugables de T-068: arena, MechaJugador y FlujoPelea reales (prefabs) con tiempos cortos, y el rival
    /// elegido por prueba (RivalTutorial con piedad o RivalBasico sin ella).
    /// </summary>
    public class PiedadRivalPruebas : InputTestFixture
    {
        private const string RutaPrefabArena = "Assets/_Proyecto/Pelea/Pruebas/ArenaDePrueba.prefab";
        private const string RutaPrefabFlujo = "Assets/_Proyecto/Pelea/Prefabs/FlujoPelea.prefab";
        private const string RutaPrefabJugador = "Assets/_Proyecto/Mecha/Prefabs/MechaJugador.prefab";
        private const string RutaPrefabTutorial = "Assets/_Proyecto/IA/Prefabs/RivalTutorial.prefab";
        private const string RutaPrefabBasico = "Assets/_Proyecto/IA/Prefabs/RivalBasico.prefab";
        private const string RutaConfigPiedad = "Assets/_Proyecto/IA/Datos/ConfigPiedadRival_Base.asset";
        private const int FramesEstables = 10;
        private const int MaxFramesCalentamiento = 1000;
        private const float DeltaTimeEstable = 0.05f;

        private const float EscalaTiempo = 4f;
        private const float SegundosDePelea = 120f;
        private const float EsperaMaxima = 15f;
        private const float MargenVida = 1e-3f;
        private const float DanioMortal = 100000f;
        private const float VidaBajaJugador = 20f;
        private const float LimiteGanar = 90f;
        private const float DanioNormalTutorial = 12f;
        private const float SegundosEntreClics = 0.1f;
        private static readonly Vector3 PosicionArena = new Vector3(1000f, 0f, 0f);

        private Keyboard teclado;
        private Mouse raton;
        private readonly List<GameObject> creados = new List<GameObject>();
        private readonly List<string> errores = new List<string>();
        private readonly List<ResultadoPelea> resultados = new List<ResultadoPelea>();
        private ConfigFlujoPelea configPrueba;
        private GameObject jugador;
        private VidaMecha vidaJugador;
        private DirectorPelea director;
        private float escalaOriginal;

        public override void Setup()
        {
            base.Setup();
            EventosPelea.LimpiarSuscriptores();
            teclado = InputSystem.AddDevice<Keyboard>();
            raton = InputSystem.AddDevice<Mouse>();
            errores.Clear();
            resultados.Clear();
            escalaOriginal = Time.timeScale;
            Application.logMessageReceived += RegistrarError;
            EventosPelea.PeleaTerminada += resultados.Add;
        }

        public override void TearDown()
        {
            Time.timeScale = escalaOriginal;
            Application.logMessageReceived -= RegistrarError;
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

        private void RegistrarError(string mensaje, string pila, LogType tipo)
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

        private static void FijarFloat(Object objeto, string campo, float valor)
        {
            SerializedObject so = new SerializedObject(objeto);
            so.FindProperty(campo).floatValue = valor;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // Arena, jugador y FlujoPelea con el rival indicado (el prefab del flujo se cambia antes de su Awake).
        private IEnumerator Escenario(string rutaPrefabRival)
        {
            yield return Calentar();
            GameObject arena = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefabArena), PosicionArena, Quaternion.identity);
            creados.Add(arena);
            Pose inicio = arena.GetComponent<IArenaPelea>().InicioJugador;
            jugador = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefabJugador), inicio.position, inicio.rotation);
            creados.Add(jugador);
            vidaJugador = jugador.GetComponent<VidaMecha>();

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

        private PiedadRival PiedadDelRival() => ((Component)director.Rival).GetComponent<PiedadRival>();

        [UnityTest]
        public IEnumerator Nombre_RivalTutorial_EsElAliasYElJugadorConservaElDeSuMecha()
        {
            yield return Escenario(RutaPrefabTutorial);

            Assert.AreEqual("EL CAMPEÓN", director.Rival.Nombre);
            Assert.AreEqual("Mecha", director.Jugador.Nombre, "El jugador devuelve el nombre de DatosMecha_Jugador");
            Assert.AreEqual(((VidaMecha)director.Jugador.Vida).Datos.Nombre, director.Jugador.Nombre);
            Assert.AreEqual(120f, director.Rival.Vida.Maxima);
        }

        [UnityTest]
        public IEnumerator Piedad_JugadorQuieto120s_NuncaLlegaAKOYNuncaBajaDel15PorCiento()
        {
            yield return Escenario(RutaPrefabTutorial);
            yield return HastaEstado(EstadoFlujoPelea.Peleando);
            Time.timeScale = EscalaTiempo;
            float piso = vidaJugador.Maxima * 0.15f;
            float minima = vidaJugador.Maxima;
            int golpesRecibidos = 0;
            EventosPelea.ImpactoRecibido += (objetivo, info, r) =>
            {
                if (objetivo == director.Jugador && r.DanioAplicado > 0f)
                {
                    golpesRecibidos++;
                }
            };

            float fin = Time.time + SegundosDePelea;
            while (Time.time < fin)
            {
                yield return null;
                minima = Mathf.Min(minima, vidaJugador.Actual);
                Assert.IsFalse(vidaJugador.EstaKO, "El jugador cayó en KO");
            }

            Assert.GreaterOrEqual(minima, piso - MargenVida, "Bajó del 15 %");
            Assert.AreEqual(piso, minima, 0.5f, "La piedad lo deja cerca del piso, no lo regala");
            Assert.Greater(golpesRecibidos, 3, "Sí hubo pelea: el rival lo golpeó varias veces");
            Assert.IsTrue(PiedadDelRival().EstaDebil, "Con tan poca vida el rival está débil");
            Assert.AreEqual(EstadoFlujoPelea.Peleando, director.Estado);
            Assert.AreEqual(0, errores.Count, string.Join("\n", errores));
        }

        [UnityTest]
        public IEnumerator Piedad_DespuesDeDebilitarse_CadaGolpeQuitaComoMuchoLaMitadDelDanioNormal()
        {
            yield return Escenario(RutaPrefabTutorial);
            yield return HastaEstado(EstadoFlujoPelea.Peleando);
            Time.timeScale = EscalaTiempo;
            PiedadRival piedad = PiedadDelRival();
            ConfigPiedadRival config = AssetDatabase.LoadAssetAtPath<ConfigPiedadRival>(RutaConfigPiedad);
            List<float> normales = new List<float>();
            List<float> debiles = new List<float>();
            EventosPelea.ImpactoRecibido += (objetivo, info, r) =>
            {
                if (objetivo == director.Jugador && r.DanioAplicado > 0f)
                {
                    (piedad.EstaDebil ? debiles : normales).Add(r.DanioAplicado);
                }
            };

            float fin = Time.time + SegundosDePelea;
            while (Time.time < fin && debiles.Count < 3)
            {
                yield return null;
            }

            Assert.GreaterOrEqual(debiles.Count, 1, "Se debilitó y pegó");
            Assert.Greater(normales.Count, 0, "Antes de debilitarse pegaba normal");
            foreach (float danio in normales)
            {
                Assert.AreEqual(DanioNormalTutorial, danio, 1e-3f);
            }
            foreach (float danio in debiles)
            {
                Assert.LessOrEqual(danio, DanioNormalTutorial * config.MultiplicadorDanio + 1e-3f, "Pegó más de la mitad");
            }
            Assert.AreEqual(0, errores.Count, string.Join("\n", errores));
        }

        [UnityTest]
        public IEnumerator Piedad_VidaBajaYJugadorClicSeguido_LoDejaEnKOYGana()
        {
            yield return Escenario(RutaPrefabTutorial);
            yield return HastaEstado(EstadoFlujoPelea.Peleando);
            Time.timeScale = EscalaTiempo;
            Danar(director.Jugador, vidaJugador.Maxima - VidaBajaJugador);
            yield return null;
            yield return null;
            Assert.IsTrue(PiedadDelRival().EstaDebil, "Con la vida baja el rival se debilita");

            float fin = Time.time + LimiteGanar;
            while (!director.Rival.Vida.EstaKO && Time.time < fin)
            {
                Press(raton.leftButton);
                yield return null;
                Release(raton.leftButton);
                yield return Esperar(SegundosEntreClics);
                Assert.IsFalse(vidaJugador.EstaKO, "El jugador cayó");
            }

            Assert.IsTrue(director.Rival.Vida.EstaKO, "El jugador no logró el KO");
            Assert.IsFalse(vidaJugador.EstaKO);
            yield return HastaEstado(EstadoFlujoPelea.Resultado);
            Assert.AreEqual(LadoPelea.Jugador, resultados[0].Ganador);
            Assert.AreEqual(0, errores.Count, string.Join("\n", errores));
        }

        [UnityTest]
        public IEnumerator Control_RivalBasicoSinPiedad_ElJugadorQuietoLlegaAKO()
        {
            yield return Escenario(RutaPrefabBasico);
            yield return HastaEstado(EstadoFlujoPelea.Peleando);
            Time.timeScale = EscalaTiempo;
            Assert.IsNull(((Component)director.Rival).GetComponent<PiedadRival>(), "El rival normal no tiene piedad");

            float fin = Time.time + SegundosDePelea;
            while (!vidaJugador.EstaKO && Time.time < fin)
            {
                yield return null;
            }

            Assert.IsTrue(vidaJugador.EstaKO, "Sin piedad el jugador quieto pierde");
            yield return HastaEstado(EstadoFlujoPelea.Resultado);
            Assert.AreEqual(LadoPelea.Rival, resultados[0].Ganador);
        }

        [UnityTest]
        public IEnumerator Revancha_R_QuitaLaDebilidadDelRival()
        {
            yield return Escenario(RutaPrefabTutorial);
            yield return HastaEstado(EstadoFlujoPelea.Peleando);
            PiedadRival piedad = PiedadDelRival();
            RivalBasico rival = piedad.GetComponent<RivalBasico>();
            VidaMecha vidaRival = piedad.GetComponent<VidaMecha>();
            Danar(director.Jugador, vidaJugador.Maxima - VidaBajaJugador);
            yield return null;
            yield return null;
            Assert.IsTrue(piedad.EstaDebil);
            Assert.Less(rival.MultiplicadorDanio, 1f);
            Assert.Greater(vidaRival.MultiplicadorDanioRecibido, 1f);

            Danar(director.Rival, DanioMortal);
            yield return HastaEstado(EstadoFlujoPelea.Resultado);
            Assert.IsTrue(piedad.EstaDebil, "Sigue débil hasta la revancha");

            Press(teclado.rKey);
            yield return null;
            Release(teclado.rKey);
            yield return null;
            yield return null;

            Assert.AreEqual(EstadoFlujoPelea.Conteo, director.Estado);
            Assert.IsFalse(piedad.EstaDebil, "La revancha quita la debilidad");
            Assert.AreEqual(1f, rival.MultiplicadorDanio);
            Assert.AreEqual(1f, rival.MultiplicadorEspera);
            Assert.AreEqual(1f, rival.MultiplicadorVelocidad);
            Assert.AreEqual(1f, vidaRival.MultiplicadorDanioRecibido);
            Assert.AreEqual(vidaJugador.Maxima, vidaJugador.Actual, "El jugador también volvió a su vida máxima");
            Assert.AreEqual(0, errores.Count, string.Join("\n", errores));
        }
    }
}
