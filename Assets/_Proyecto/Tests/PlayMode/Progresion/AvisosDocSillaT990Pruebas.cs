using System;
using System.Collections;
using System.IO;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Interaccion;
using MechaCyberpunk.Progresion.Apoyo;
using MechaCyberpunk.Progresion.Prologo.Pandillas;
using MechaCyberpunk.UI.Clinica;
using MechaCyberpunk.UI.Hud;
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
    /// T-990: en Ciudad_Prologo, DE NOCHE (clínica cerrada por horario), al llegar al DOC el HUD H10 dice "[E] IMPLANTES" y la E abre la clínica; al llegar a la silla dice "[E] CURARME" y la E te sienta.
    /// El jugador se coloca con Colocar en el punto donde se para de verdad (frente al Doc / junto a la silla) y mira hacia ellos; la E es una tecla simulada.
    /// Deja docs/capturas/t990/doc.png y silla.png (1920x1080) con CAPTURAR_T990=1.
    /// </summary>
    public class AvisosDocSillaT990Pruebas : InputTestFixture
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

        private static void Capturar(string nombre)
        {
            if (Environment.GetEnvironmentVariable("CAPTURAR_T990") != "1")
            {
                return;
            }
            Directory.CreateDirectory(Path.Combine("docs", "capturas", "t990"));
            MechaCyberpunk.Tests.UI.CapturaUI.Capturar(Path.Combine("docs", "capturas", "t990", nombre), Camera.main);
        }

        private static string Ayudas(HudH10 hud)
        {
            string s = string.Empty;
            foreach (UnityEngine.UI.Text t in hud.TextosAyuda)
            {
                s += "[" + t.text + "]";
            }
            return s;
        }

        private static bool Contiene(HudH10 hud, string texto)
        {
            foreach (UnityEngine.UI.Text t in hud.TextosAyuda)
            {
                if (t.text == texto)
                {
                    return true;
                }
            }
            return false;
        }

        private static void Diagnosticar(string quien, IJugador jugador, InteraccionAPie interaccion, IInteractuable objetivo, HudH10 hud)
        {
            Vector3 delta = objetivo.Posicion - jugador.Raiz.position;
            float angulo = Vector3.Angle(jugador.Camara.transform.forward, delta);
            IInteractuable c = interaccion.InteractuableCandidato;
            Debug.Log("[T-990] " + quien + ": tipo=" + interaccion.TipoCandidato + " candidato=" + (c != null ? c.GetType().Name + " '" + c.Texto + "' PuedeUsar=" + c.PuedeUsar : "null")
                + " | objetivo '" + objetivo.Texto + "' PuedeUsar=" + objetivo.PuedeUsar + " pos=" + objetivo.Posicion + " jugador=" + jugador.Raiz.position
                + " dist=" + delta.magnitude.ToString("F2") + " (alto " + delta.y.ToString("F2") + ") angulo=" + angulo.ToString("F1")
                + " | clinica=" + (Object.FindFirstObjectByType<MostradorClinica>() != null) + " | ayudas=" + Ayudas(hud));
        }

        [UnityTest]
        public IEnumerator FrenteAlDocYLaSillaSalenLasAyudasYLaEFunciona()
        {
            yield return EditorSceneManager.LoadSceneInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            DirectorPandillas historia = null;
            yield return EsperarHasta(() =>
            {
                historia = Object.FindFirstObjectByType<DirectorPandillas>();
                return historia != null && historia.Iniciado && Registro<IJugador>.Actual != null && Registro<ICartera>.Actual != null && Registro<IRelojMundo>.Actual != null;
            }, "el director arrancó", 30f);
            for (int i = 0; i < 12 && historia.Paso != PasoHistoria.Libre; i++)
            {
                yield return Pulsar(teclado.f6Key);
            }
            IJugador jugador = Registro<IJugador>.Actual;
            ICartera cartera = Registro<ICartera>.Actual;
            Registro<IRelojMundo>.Actual.AvanzarHasta(2f); // de noche la clínica está cerrada por horario
            cartera.Abonar(20000 - cartera.Dinero, MotivoDinero.Otro);
            InteraccionAPie interaccion = jugador.Raiz.GetComponentInChildren<InteraccionAPie>();
            HudH10 hud = Object.FindFirstObjectByType<HudH10>();
            Assert.IsNotNull(interaccion);
            Assert.IsNotNull(hud);

            PersonajeApoyo doc = null;
            foreach (PersonajeApoyo p in Object.FindObjectsByType<PersonajeApoyo>(FindObjectsSortMode.None))
            {
                doc = p.Datos.Id == "doc" ? p : doc;
            }
            Assert.IsNotNull(doc, "EL DOC está en la ciudad");
            SillaClinica silla = SillaClinica.Actual;
            Assert.IsNotNull(silla, "La silla de la clínica está en la ciudad");
            MostradorClinica mostrador = Object.FindFirstObjectByType<MostradorClinica>();
            Assert.IsFalse(mostrador.PuedeUsar, "De noche el mostrador de la clínica dice cerrado");
            Debug.Log("[T-990] doc.pos=" + doc.Posicion + " doc.forward=" + doc.transform.forward + " silla.pos=" + silla.Posicion
                + " mostrador=" + (mostrador != null ? mostrador.name + " PuedeUsar=" + mostrador.PuedeUsar + " pos=" + mostrador.Posicion : "null"));

            // EL DOC: se llega por el frente (1.5 m delante de él) mirándolo.
            Vector3 frente = doc.transform.position + doc.transform.forward * 1.5f;
            Vector3 hacia = doc.transform.position - frente;
            hacia.y = 0f;
            jugador.Colocar(new Pose(frente, Quaternion.LookRotation(hacia.normalized, Vector3.up)));
            yield return new WaitForSecondsRealtime(0.6f);
            Diagnosticar("DOC", jugador, interaccion, doc, hud);
            Assert.IsTrue(doc.PuedeUsar, "EL DOC ofrece IMPLANTES aunque la clínica esté cerrada por horario");
            Assert.IsTrue(Contiene(hud, "[E] IMPLANTES"), "Frente al Doc el HUD dice [E] IMPLANTES; dice " + Ayudas(hud));
            Capturar("doc.png");
            yield return Pulsar(teclado.eKey);
            mostrador = Object.FindFirstObjectByType<MostradorClinica>();
            Assert.IsNotNull(mostrador, "La clínica existe");
            yield return EsperarHasta(() => mostrador.PantallaAbierta && mostrador.Pantalla.Visible, "abrir la clínica con E", 5f);
            yield return Pulsar(teclado.escapeKey);
            yield return EsperarHasta(() => !mostrador.PantallaAbierta, "cerrar la clínica", 5f);
            yield return new WaitForSecondsRealtime(0.3f);

            // La silla: se llega por el lado de la salida (donde uno se levanta), mirándola.
            Transform salida = typeof(SillaClinica).GetField("salida", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(silla) as Transform;
            Vector3 junto = salida.position;
            Vector3 haciaSilla = silla.transform.position - junto;
            haciaSilla.y = 0f;
            jugador.Colocar(new Pose(junto, Quaternion.LookRotation(haciaSilla.normalized, Vector3.up)));
            yield return new WaitForSecondsRealtime(0.6f);
            Diagnosticar("SILLA", jugador, interaccion, silla, hud);
            Assert.IsTrue(silla.PuedeUsar, "La silla se puede usar");
            Assert.AreEqual("CURARME", silla.Texto);
            Assert.IsTrue(Contiene(hud, "[E] CURARME"), "Junto a la silla el HUD dice [E] CURARME; dice " + Ayudas(hud));
            Capturar("silla.png");
            yield return Pulsar(teclado.eKey);
            yield return EsperarHasta(() => silla.EnSecuencia, "la E te sienta", 5f);
        }
    }
}
