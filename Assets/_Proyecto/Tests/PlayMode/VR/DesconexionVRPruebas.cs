using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.VR;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.VR
{
    /// <summary>
    /// VR22: visor quitado o desconectado → el juego se pausa y sale el aviso; visor de vuelta + A → recentra y sigue; sin visor mucho rato no hay
    /// errores ni se despausa solo; sin VR no hace nada. El estado del visor se fuerza con <see cref="DesconexionVR.ForzarVisor"/> (simulador).
    /// </summary>
    public class DesconexionVRPruebas : BaseJugadorVR
    {
        private DesconexionVR desconexion;

        public override void Setup()
        {
            base.Setup();
            desconexion = DesconexionVR.Asegurar();
            desconexion.Restablecer();
        }

        public override void TearDown()
        {
            if (desconexion != null)
            {
                desconexion.Restablecer();
            }
            ControlTiempo.SoltarTodo();
            Time.timeScale = 1f;
            AudioListener.pause = false;
            Pausa.Fijar(false);
            base.TearDown();
        }

        // Con el tiempo en 0, Time.time no avanza: se espera en tiempo real.
        private static IEnumerator SegundosReales(float s)
        {
            float fin = Time.realtimeSinceStartup + s;
            while (Time.realtimeSinceStartup < fin)
            {
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator VisorQuitado_Pausa_YVuelveConA_Recentra()
        {
            yield return ArmarVR();
            Assert.AreSame(desconexion, DesconexionVR.Actual);
            Assert.IsFalse(desconexion.Pausado);
            bool controlAntes = controlador.ControlActivo;

            desconexion.ForzarVisor(false);
            yield return Cuadros(3);
            Assert.IsTrue(desconexion.Pausado, "Visor quitado: pausa");
            Assert.AreEqual(0f, Time.timeScale, 1e-4f, "Tiempo en 0");
            Assert.IsTrue(Pausa.Activa, "Pausa.Activa");
            Assert.IsFalse(controlador.ControlActivo, "El jugador queda congelado (o ya lo estaba)");
            StringAssert.Contains(DesconexionVR.TextoDesconectado, desconexion.TextoAviso, "Sale el aviso");
            StringAssert.Contains("pulsa A", desconexion.TextoAviso);

            // A con el visor fuera no hace nada.
            yield return Pulsar(controlDer.primaryButton);
            Assert.IsTrue(desconexion.Pausado, "Sin visor A no quita la pausa");

            // Vuelve con la cabeza girada 90° en el cuarto: al seguir, el rig se recentra para que mire al frente del cuerpo.
            PonerCabeza(new Vector3(0f, 1.6f, 0f), Quaternion.Euler(0f, 90f, 0f));
            desconexion.ForzarVisor(true);
            yield return Cuadros(3);
            Assert.IsTrue(desconexion.Pausado, "Al volver sigue en pausa hasta pulsar A");
            Assert.IsTrue(desconexion.EsperandoBoton, "Espera A");
            StringAssert.Contains(DesconexionVR.TextoDeVuelta, desconexion.TextoAviso);

            yield return SegundosReales(DesconexionVR.SegundosAntesDelBoton + 0.1f);
            int antes = desconexion.Recentrados;
            float frente = jugador.transform.eulerAngles.y;
            yield return Pulsar(controlDer.primaryButton);
            Assert.IsFalse(desconexion.Pausado, "A quita la pausa");
            Assert.AreEqual(antes + 1, desconexion.Recentrados, "Se recentró");
            Assert.AreEqual(1f, Time.timeScale, 1e-4f, "El tiempo vuelve");
            Assert.IsFalse(Pausa.Activa, "Sin pausa");
            Assert.AreEqual(controlAntes, controlador.ControlActivo, "El control del jugador queda como estaba");
            Assert.AreEqual(string.Empty, desconexion.TextoAviso, "Sin aviso");
            Debug.Log("[VR22] Giro del rig tras recentrar: " + adaptador.GiroRig.ToString("0.0") + " (frente " + frente.ToString("0.0") + ")");
            Assert.AreEqual(0f, Mathf.DeltaAngle(adaptador.GiroRig, frente - 90f), 2f, "Recentrado: la cabeza girada 90° queda al frente del cuerpo");

            // Sigue jugable: el stick camina.
            if (!controlAntes)
            {
                yield break;
            }
            Vector3 inicio = jugador.transform.position;
            Stick(false, new Vector2(0f, 1f));
            yield return Segundos(1f);
            Stick(false, Vector2.zero);
            Assert.Greater((jugador.transform.position - inicio).magnitude, 0.5f, "Tras seguir, el jugador camina");
        }

        [UnityTest]
        public IEnumerator SinVisorMuchoRato_SinErroresYSigueEnPausa()
        {
            yield return ArmarVR();
            desconexion.ForzarVisor(false);
            yield return SegundosReales(4f);
            Assert.IsTrue(desconexion.Pausado, "Sigue en pausa");
            Assert.AreEqual(0f, Time.timeScale, 1e-4f);
            Assert.IsFalse(desconexion.EsperandoBoton);
            Assert.IsNotEmpty(desconexion.TextoAviso, "El aviso sigue en la ventana del PC");
            // Un error o excepción en el log ya hace fallar la prueba (LogAssert); los Debug.Log normales del juego no cuentan.

            // Se pone el visor y se lo vuelve a quitar antes de pulsar A: vuelve a "desconectado".
            desconexion.ForzarVisor(true);
            yield return Cuadros(2);
            Assert.IsTrue(desconexion.EsperandoBoton);
            desconexion.ForzarVisor(false);
            yield return Cuadros(2);
            Assert.IsFalse(desconexion.EsperandoBoton);
            Assert.IsTrue(desconexion.Pausado);
            StringAssert.Contains(DesconexionVR.TextoDesconectado, desconexion.TextoAviso);
            // Un error o excepción en el log ya hace fallar la prueba (LogAssert); los Debug.Log normales del juego no cuentan.
        }

        [UnityTest]
        public IEnumerator SinModoVR_NoPausa()
        {
            yield return null;
            ModoVR.Fijar(false);
            desconexion.ForzarVisor(false);
            yield return Cuadros(5);
            Assert.IsFalse(desconexion.Pausado, "Sin VR el visor no pausa");
            Assert.AreEqual(1f, Time.timeScale, 1e-4f);
            Assert.IsFalse(Pausa.Activa);
        }
    }
}
