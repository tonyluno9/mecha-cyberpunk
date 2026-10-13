using System.Collections;
using System.Collections.Generic;
using MechaCyberpunk.Core;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Arena
{
    /// <summary>
    /// T-044: tres peleas seguidas con revancha en la escena real Clandestino.unity, hablando con la pelea solo por Core.
    /// </summary>
    public class PeleaClandestinoPruebas : InputTestFixture
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Clandestino.unity";
        private const float DanioMortal = 100000f;
        private const float Margen = 0.1f;
        private const float EsperaMaxima = 30f;
        private const float ReintentoR = 0.5f;

        private Keyboard teclado;
        private int iniciadas;
        private readonly List<int> conteos = new List<int>();
        private readonly List<ResultadoPelea> resultados = new List<ResultadoPelea>();
        private readonly List<string> errores = new List<string>();
        private IPeleador jugador;
        private IPeleador rival;

        public override void Setup()
        {
            base.Setup();
            EventosPelea.LimpiarSuscriptores();
            teclado = InputSystem.AddDevice<Keyboard>();
            InputSystem.AddDevice<Mouse>();
            iniciadas = 0;
            conteos.Clear();
            resultados.Clear();
            errores.Clear();
            Application.logMessageReceived += RegistrarLog;
            EventosPelea.ConteoCambio += conteos.Add;
            EventosPelea.PeleaIniciada += SumarInicio;
            EventosPelea.PeleaTerminada += resultados.Add;
        }

        public override void TearDown()
        {
            Application.logMessageReceived -= RegistrarLog;
            Scene escena = SceneManager.GetSceneByPath(RutaEscena);
            if (escena.IsValid())
            {
                foreach (GameObject raiz in escena.GetRootGameObjects())
                {
                    Object.Destroy(raiz);
                }
            }
            EventosPelea.LimpiarSuscriptores();
            base.TearDown();
        }

        private void SumarInicio() => iniciadas++;

        private void RegistrarLog(string mensaje, string pila, LogType tipo)
        {
            if (tipo == LogType.Error || tipo == LogType.Exception || tipo == LogType.Assert)
            {
                errores.Add(mensaje);
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

        private void BuscarPeleadores()
        {
            jugador = null;
            rival = null;
            foreach (MonoBehaviour m in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (m is IPeleador p)
                {
                    if (p.Lado == LadoPelea.Jugador)
                    {
                        jugador = p;
                    }
                    else
                    {
                        rival = p;
                    }
                }
            }
        }

        private IEnumerator HastaCondicion(System.Func<bool> condicion, string motivo)
        {
            float fin = Time.realtimeSinceStartup + EsperaMaxima;
            while (!condicion() && Time.realtimeSinceStartup < fin)
            {
                yield return null;
            }
            Assert.IsTrue(condicion(), "Tiempo agotado: " + motivo);
        }

        private IEnumerator HastaPeleaIniciada(int cantidad)
        {
            yield return HastaCondicion(() => iniciadas >= cantidad, "PeleaIniciada " + cantidad);
        }

        private static void Matar(IPeleador peleador)
        {
            IRecibeDanio receptor = peleador.Raiz.GetComponentInChildren<IRecibeDanio>();
            Assert.IsNotNull(receptor, "El peleador " + peleador.Nombre + " no tiene IRecibeDanio");
            receptor.RecibirDanio(new InfoDanio(DanioMortal, TipoDanio.Golpe, peleador.Raiz.position, Vector3.forward, null));
        }

        private IEnumerator PulsarR()
        {
            Press(teclado.rKey);
            yield return null;
            Release(teclado.rKey);
            yield return null;
        }

        private IEnumerator TerminarPelea(int numero, LadoPelea ganador)
        {
            Matar(ganador == LadoPelea.Jugador ? rival : jugador);
            yield return HastaCondicion(() => resultados.Count >= numero, "PeleaTerminada " + numero);
            Assert.AreEqual(numero, resultados.Count, "PeleaTerminada debe llegar una sola vez por pelea");
            Assert.AreEqual(ganador, resultados[numero - 1].Ganador);
        }

        private IEnumerator Revancha(int peleasHechas)
        {
            int conteosAntes = conteos.Count;
            float proximo = 0f;
            float fin = Time.realtimeSinceStartup + EsperaMaxima;
            while (conteos.Count == conteosAntes && Time.realtimeSinceStartup < fin)
            {
                if (Time.realtimeSinceStartup >= proximo)
                {
                    yield return PulsarR();
                    proximo = Time.realtimeSinceStartup + ReintentoR;
                }
                yield return null;
            }
            Assert.Greater(conteos.Count, conteosAntes, "R no arrancó una revancha");
            Assert.AreEqual(peleasHechas, iniciadas, "No debe iniciar la pelea antes del conteo");
            BuscarPeleadores();
            VerificarInicio("revancha");
        }

        private void VerificarInicio(string cuando)
        {
            IArenaPelea arena = Registro<IArenaPelea>.Actual;
            Assert.IsNotNull(arena, "No hay arena registrada");
            Assert.Less(Vector3.Distance(jugador.Raiz.position, arena.InicioJugador.position), Margen, "Jugador fuera de InicioJugador (" + cuando + ")");
            Assert.Less(Vector3.Distance(rival.Raiz.position, arena.InicioRival.position), Margen, "Rival fuera de InicioRival (" + cuando + ")");
            Assert.IsFalse(jugador.ControlActivo, "El jugador tiene control durante el conteo (" + cuando + ")");
            Assert.IsFalse(rival.ControlActivo, "El rival tiene control durante el conteo (" + cuando + ")");
            Assert.AreEqual(jugador.Vida.Maxima, jugador.Vida.Actual, 0.001f, "Vida del jugador incompleta (" + cuando + ")");
            Assert.AreEqual(rival.Vida.Maxima, rival.Vida.Actual, 0.001f, "Vida del rival incompleta (" + cuando + ")");
        }

        [UnityTest]
        public IEnumerator TresPeleasSeguidasConRevancha_GanaJugadorGanaRivalGanaJugador_SinErrores()
        {
            yield return EditorSceneManager.LoadSceneInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));

            // Conteo inicial: jugador y rival en sus inicios, sin control.
            yield return HastaCondicion(() =>
            {
                BuscarPeleadores();
                return jugador != null && rival != null && conteos.Count > 0;
            }, "jugador, rival y conteo");
            Assert.AreEqual(0, iniciadas, "La pelea inició antes de que se pudiera comprobar el conteo");
            VerificarInicio("inicio");
            Assert.AreEqual(1, Camera.allCameras.Length, "Debe haber una sola cámara activa (la de la cabina)");

            // Pelea 1: gana el jugador.
            yield return HastaPeleaIniciada(1);
            Assert.IsTrue(jugador.ControlActivo);
            Assert.IsTrue(rival.ControlActivo);
            yield return TerminarPelea(1, LadoPelea.Jugador);
            yield return Revancha(1);

            // Pelea 2: gana el rival.
            yield return HastaPeleaIniciada(2);
            yield return TerminarPelea(2, LadoPelea.Rival);
            yield return Revancha(2);

            // Pelea 3: gana el jugador.
            yield return HastaPeleaIniciada(3);
            yield return TerminarPelea(3, LadoPelea.Jugador);

            yield return Esperar(0.3f);
            Assert.AreEqual(3, resultados.Count);
            Assert.AreEqual(0, errores.Count, string.Join(" | ", errores));
        }
    }
}
