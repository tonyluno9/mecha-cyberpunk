using System.Collections;
using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.APie;
using MechaCyberpunk.UI.Hud;
using MechaCyberpunk.UI.Mapa;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Progresion
{
    /// <summary>
    /// T-404 en Ciudad_Prologo real: el HUD dice la zona, siete vencidos sacan al jefe, vencerlo conquista la zona y su grupo no reaparece,
    /// y M abre y cierra el mapa. Atajos anotados: el jugador se coloca con Colocar y los vencidos extra se registran por el contrato;
    /// el jefe y los pandilleros del centro caen por daño directo (IRecibeDanio).
    /// </summary>
    public class TerritorioPruebas : InputTestFixture
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const float DanoMatar = 100000f;
        private const float EscalaTiempo = 5f;
        private const float SegundosSinReaparecer = 45f;
        private const float LimiteEspera = 20f;

        private Keyboard teclado;

        public override void Setup()
        {
            base.Setup();
            teclado = InputSystem.AddDevice<Keyboard>();
        }

        public override void TearDown()
        {
            Time.timeScale = 1f;
            base.TearDown();
        }

        private static IEnumerator EsperarHasta(System.Func<bool> condicion, string que)
        {
            float fin = Time.realtimeSinceStartup + LimiteEspera;
            while (!condicion())
            {
                Assert.Less(Time.realtimeSinceStartup, fin, "Tiempo agotado esperando: " + que);
                yield return null;
            }
        }

        private IEnumerator Pulsar(UnityEngine.InputSystem.Controls.KeyControl tecla)
        {
            Press(tecla);
            yield return null;
            Release(tecla);
            yield return null;
        }

        [UnityTest]
        public IEnumerator HudYMapa()
        {
            yield return EditorSceneManager.LoadSceneInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return EsperarHasta(() => Registro<IJugador>.Actual != null && Registro<ITerritorio>.Actual != null, "jugador y territorio");
            ITerritorio territorio = Registro<ITerritorio>.Actual;
            IJugador jugador = Registro<IJugador>.Actual;
            HudTerritorio hud = Object.FindFirstObjectByType<HudTerritorio>();
            PantallaMapa mapa = Object.FindFirstObjectByType<PantallaMapa>();
            Assert.IsNotNull(hud);
            Assert.IsNotNull(mapa);

            // Jugador en el centro: el HUD dice EL TIANGUIS.
            jugador.Colocar(new Pose(new Vector3(0f, 0.5f, 0f), Quaternion.identity));
            yield return EsperarHasta(() => hud.TextoZona.text.Contains("EL TIANGUIS"), "el HUD dice EL TIANGUIS");
            StringAssert.Contains("SKULLS", hud.TextoZona.text);

            // El mapa: M abre, M cierra, Esc cierra.
            yield return Pulsar(teclado.mKey);
            Assert.IsTrue(mapa.Abierto, "M abre el mapa.");
            yield return Pulsar(teclado.mKey);
            Assert.IsFalse(mapa.Abierto, "M cierra el mapa.");
            yield return Pulsar(teclado.mKey);
            Assert.IsTrue(mapa.Abierto);
            yield return Pulsar(teclado.escapeKey);
            Assert.IsFalse(mapa.Abierto, "Esc cierra el mapa.");

            // Los pandilleros del centro (zona del tianguis) caen: cada uno suma 10.
            int caidos = 0;
            List<PandillaAPie> delCentro = new List<PandillaAPie>();
            foreach (PandillaAPie grupo in Object.FindObjectsByType<PandillaAPie>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (territorio.ZonaEn(grupo.Centro) != "tianguis")
                {
                    continue;
                }
                delCentro.Add(grupo);
                foreach (PandilleroAPie p in grupo.Pandilleros)
                {
                    if (p.gameObject.activeSelf && !p.EstaKO)
                    {
                        p.RecibirDanio(new InfoDanio(DanoMatar, TipoDanio.Disparo, p.transform.position, Vector3.forward, null));
                        caidos++;
                    }
                }
            }
            Assert.Greater(delCentro.Count, 0, "Hay grupos en el centro.");
            Assert.AreEqual(caidos * 10, territorio.Respeto("tianguis"));
            while (territorio.Respeto("tianguis") < 70)
            {
                territorio.RegistrarVencido(Vector3.zero, IdPandilla.Calaveras, false);
            }
            Assert.IsTrue(territorio.JefeDisponible("tianguis"));

            // El jefe aparece y se vence.
            JefePandilla punto = null;
            foreach (JefePandilla j in Object.FindObjectsByType<JefePandilla>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (j.Zona == "tianguis")
                {
                    punto = j;
                }
            }
            Assert.IsNotNull(punto);
            yield return EsperarHasta(() => punto.Vivo, "aparece el jefe");
            Assert.AreEqual(250f, punto.Jefe.Maxima, 0.01f);
            punto.Jefe.RecibirDanio(new InfoDanio(DanoMatar, TipoDanio.Disparo, punto.Jefe.transform.position, Vector3.forward, null));
            yield return null;
            Assert.AreEqual(IdPandilla.Jugador, territorio.Dueno("tianguis"));
            Assert.AreEqual(100, territorio.Respeto("tianguis"));
            yield return null;
            StringAssert.Contains("ES TUYO", hud.TextoAviso.text);

            // El grupo vacío de una zona tuya no reaparece (45 s de juego, acelerados).
            jugador.Colocar(new Pose(new Vector3(-94f, 0.5f, -47f), Quaternion.identity));
            Time.timeScale = EscalaTiempo;
            float fin = Time.time + SegundosSinReaparecer;
            while (Time.time < fin)
            {
                yield return null;
            }
            Time.timeScale = 1f;
            foreach (PandillaAPie grupo in delCentro)
            {
                Assert.AreEqual(0, grupo.Presentes, "El grupo de una zona conquistada no reaparece.");
            }
        }
    }
}
