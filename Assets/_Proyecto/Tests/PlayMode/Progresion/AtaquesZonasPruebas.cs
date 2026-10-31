using System;
using System.Collections;
using System.IO;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.APie;
using MechaCyberpunk.Progresion;
using MechaCyberpunk.Progresion.Prologo.Encargos;
using MechaCyberpunk.Progresion.Prologo.Pandillas;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Tests.Progresion
{
    /// <summary>
    /// T-427: ataques rivales a tus zonas como un jugador. No ocurren en la historia ni sin zonas tuyas; con EL TIANGUIS tuyo avisan y marcan, defender
    /// paga Biits y respeto, abandonar baja el respeto y, bajo el umbral, la zona pasa al rival. Atajos: F6 salta la historia, Colocar mueve al jugador.
    /// Con CAPTURAR_T427=1 y el editor abierto deja docs/capturas/T-427-1.png.
    /// </summary>
    public class AtaquesZonasPruebas : InputTestFixture
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private Keyboard teclado;

        public override void Setup()
        {
            base.Setup();
            teclado = InputSystem.AddDevice<Keyboard>();
            InputSystem.AddDevice<Mouse>();
        }

        [UnityTearDown]
        public IEnumerator DescargarEscena()
        {
            foreach (GameObject raiz in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                Object.Destroy(raiz);
            }
            Registros.LimpiarTodos();
            yield return null;
        }

        private static IEnumerator EsperarHasta(Func<bool> condicion, string que, float segundos)
        {
            float fin = Time.realtimeSinceStartup + segundos;
            while (!condicion())
            {
                Assert.Less(Time.realtimeSinceStartup, fin, "Tiempo agotado esperando: " + que);
                yield return null;
            }
        }

        private IEnumerator Pulsar(ButtonControl tecla)
        {
            Press(tecla);
            yield return null;
            Release(tecla);
            yield return null;
        }

        private static string Objetivo => Registro<IObjetivos>.Actual.HayObjetivo ? Registro<IObjetivos>.Actual.Actual.Texto : string.Empty;

        private static IEnumerator Poner(IJugador jugador, Vector3 punto)
        {
            jugador.Colocar(new Pose(punto + Vector3.up * 0.1f, Quaternion.identity));
            yield return null;
            yield return null;
            yield return new WaitForSecondsRealtime(0.2f);
        }

        private static PandilleroAPie Rival(int n)
        {
            foreach (PandilleroAPie p in Object.FindObjectsByType<PandilleroAPie>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (p.name == "RivalAtaque_" + n)
                {
                    return p;
                }
            }
            return null;
        }

        private static Transform Punto(string nombre)
        {
            foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (t.name == nombre)
                {
                    return t;
                }
            }
            return null;
        }

        private static IEnumerator Defender(DirectorAtaques ataques, IJugador jugador, Transform punto)
        {
            Assert.IsTrue(ataques.Atacar("tianguis"));
            yield return Poner(jugador, punto.position);
            yield return EsperarHasta(() => ataques.EnOleada, "la oleada", 5f);
        }

        [UnityTest]
        public IEnumerator DefenderYPerderUnaZona()
        {
            yield return EditorSceneManager.LoadSceneInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            DirectorPandillas historia = null;
            yield return EsperarHasta(() =>
            {
                historia = Object.FindFirstObjectByType<DirectorPandillas>();
                return historia != null && historia.Iniciado && Registro<IJugador>.Actual != null && Registro<ITerritorio>.Actual != null
                    && Registro<ICartera>.Actual != null && Registro<IObjetivos>.Actual != null && Registro<ServicioTerritorio>.Actual != null;
            }, "el director arrancó", 30f);
            DirectorAtaques ataques = Object.FindFirstObjectByType<DirectorAtaques>();
            Assert.IsNotNull(ataques, "La escena trae el DirectorAtaques");
            ServicioTerritorio territorio = Registro<ServicioTerritorio>.Actual;
            IJugador jugador = Registro<IJugador>.Actual;
            ICartera cartera = Registro<ICartera>.Actual;

            // En la historia no ocurre, aunque la zona fuera tuya.
            territorio.Ceder("tianguis", IdPandilla.Jugador, 100);
            Assert.IsFalse(ataques.Atacar("tianguis"), "No hay ataques durante la historia");

            for (int i = 0; i < 12 && historia.Paso != PasoHistoria.Libre; i++)
            {
                yield return Pulsar(teclado.f6Key);
            }
            Assert.AreEqual(PasoHistoria.Libre, historia.Paso);

            // Sin zonas tuyas además de SUBURBIOS tampoco, y SUBURBIOS no se ataca.
            territorio.Ceder("tianguis", IdPandilla.Calaveras, 0);
            territorio.Ceder("neon", IdPandilla.Viboras, 0);
            Assert.IsFalse(ataques.Atacar(), "Sin zonas conquistadas no hay ataques");
            Assert.IsFalse(ataques.Atacar("fosa"));

            // Con EL TIANGUIS tuyo, ataca: aviso, marca y objetivo.
            territorio.Ceder("tianguis", IdPandilla.Jugador, 100);
            Assert.IsTrue(ataques.Atacar());
            Assert.AreEqual("tianguis", ataques.ZonaAtacada);
            Assert.AreEqual(DirectorAtaques.TextoIr("EL TIANGUIS"), Objetivo);
            Assert.IsTrue(Registro<IObjetivos>.Actual.Actual.TienePosicion, "La zona queda marcada en el mapa");
            Assert.LessOrEqual(Objetivo.Length, DirectorAtaques.LargoMaximoObjetivo);
            Assert.IsFalse(ataques.Atacar(), "Un ataque a la vez");
            yield return Capturar();

            // Llega y vence la oleada: paga y sube el respeto.
            int dinero = cartera.Dinero;
            territorio.AjustarRespeto("tianguis", -50);
            Transform punto = Punto("Ataque_tianguis");
            yield return Poner(jugador, punto.position);
            yield return EsperarHasta(() => ataques.EnOleada, "la oleada", 5f);
            for (int i = 1; i <= DirectorAtaques.RivalesOleada; i++)
            {
                PandilleroAPie r = Rival(i);
                Assert.IsTrue(r.gameObject.activeSelf);
                r.RecibirDanio(new InfoDanio(100000f, TipoDanio.Disparo, r.transform.position, Vector3.forward, null));
            }
            yield return EsperarHasta(() => !ataques.HayAtaque, "terminar la defensa", 5f);
            Assert.Greater(cartera.Dinero, dinero, "El premio se paga en Biits");
            Assert.AreEqual(70, territorio.Respeto("tianguis"), "50 más el premio de respeto");
            Assert.IsFalse(Registro<IObjetivos>.Actual.HayObjetivo);

            // Pierde por abandonar: la primera vez baja el respeto; la segunda, bajo el umbral, la zona pasa a los SNAKES.
            territorio.AjustarRespeto("tianguis", 30);
            yield return Defender(ataques, jugador, punto);
            yield return Poner(jugador, punto.position + new Vector3(300f, 0f, 0f));
            yield return EsperarHasta(() => !ataques.HayAtaque, "abandonar", 5f);
            Assert.AreEqual(55, territorio.Respeto("tianguis"));
            Assert.AreEqual(IdPandilla.Jugador, territorio.Dueno("tianguis"));

            yield return Defender(ataques, jugador, punto);
            yield return Poner(jugador, punto.position + new Vector3(300f, 0f, 0f));
            yield return EsperarHasta(() => !ataques.HayAtaque, "abandonar otra vez", 5f);
            Assert.AreEqual(IdPandilla.Viboras, territorio.Dueno("tianguis"), "Bajo el umbral la zona pasa al rival");
        }

        // Foto de la vista Game (solo con CAPTURAR_T427=1 y el editor abierto).
        private static IEnumerator Capturar()
        {
            if (Environment.GetEnvironmentVariable("CAPTURAR_T427") != "1" || Application.isBatchMode)
            {
                yield break;
            }
            yield return new WaitForSecondsRealtime(0.5f);
            yield return new WaitForEndOfFrame();
            Texture2D foto = ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(Path.Combine("docs", "capturas", "T-427-1.png"), foto.EncodeToPNG());
            Object.Destroy(foto);
        }
    }
}
