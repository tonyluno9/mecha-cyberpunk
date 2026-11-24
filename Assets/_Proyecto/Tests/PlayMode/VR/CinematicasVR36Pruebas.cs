using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.VR;
using MechaCyberpunk.VR.Cinematicas;
using MechaCyberpunk.VR.Menus;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Tests.VR
{
    /// <summary>
    /// VR36: durante una cinemática, en el visor todo alrededor de la pantalla grande es negro: la cabeza solo dibuja la capa UI (la pantalla
    /// grande y los paneles), con fondo negro. Entre cinemáticas el negro se mantiene un momento; con la pausa se quita; al terminar todo vuelve.
    /// Entre clips el último cuadro se queda en la pantalla (sin cortes).
    /// </summary>
    public class CinematicasVR36Pruebas : BaseJugadorVR
    {
        public override void TearDown()
        {
            NegroCineVR.Forzado = false;
            Pausa.Fijar(false);
            base.TearDown();
        }

        [Test]
        public void MantenerEnCine_SoloEntreClipsYPocoTiempo()
        {
            Assert.IsTrue(ReglaCineVR.MantenerEnCine(true, false, 0.1f), "Entre un clip y el siguiente se queda el último cuadro.");
            Assert.IsFalse(ReglaCineVR.MantenerEnCine(false, false, 0.1f), "Sin cinemática se quita.");
            Assert.IsFalse(ReglaCineVR.MantenerEnCine(true, false, ReglaCineVR.SegundosSinCorte + 0.1f), "Parado mucho tiempo, se quita.");
            Assert.IsFalse(ReglaCineVR.MantenerEnCine(true, true, 0f), "Sonando no es 'mantener': ya está en la pantalla.");
        }

        [UnityTest]
        public IEnumerator Cinematica_AlrededorNegroYAlTerminarVuelve()
        {
            yield return ArmarVR();
            yield return Cuadros(3);
            Camera cabeza = SistemaVR.CamaraVR;
            Assert.IsNotNull(cabeza, "Hay cámara del visor.");
            Assert.IsNotNull(NegroCineVR.Asegurar());
            int mascara = cabeza.cullingMask;
            CameraClearFlags fondo = cabeza.clearFlags;
            float lejos = cabeza.farClipPlane;

            NegroCineVR.Forzado = true;
            yield return Cuadros(3);
            Assert.IsTrue(NegroCineVR.Activo);
            Assert.AreEqual(1 << PantallaCineVR.CapaUI, cabeza.cullingMask, "Solo la capa UI: nada del juego ni del HUD de casco.");
            Assert.AreEqual(CameraClearFlags.SolidColor, cabeza.clearFlags);
            Assert.AreEqual(Color.black, cabeza.backgroundColor, "Fondo negro.");
            Assert.LessOrEqual(cabeza.farClipPlane, NegroCineVR.LejosNegro + 0.01f);

            // La pantalla grande va en la capa UI: se sigue viendo en negro.
            var textura = new Texture2D(16, 9);
            var dueno = new GameObject("DuenoPruebaVR36");
            extras.Add(dueno);
            PantallaCineVR.Mostrar(dueno, textura);
            yield return null;
            foreach (MeshRenderer r in PantallaCineVR.Raiz.GetComponentsInChildren<MeshRenderer>(true))
            {
                Assert.AreEqual(PantallaCineVR.CapaUI, r.gameObject.layer, "La pantalla grande se ve con la cabeza en negro.");
            }
            PantallaCineVR.Ocultar(dueno);

            // Con la pausa abierta el negro se quita (menús y láser como siempre).
            Pausa.Fijar(true);
            yield return Cuadros(2);
            Assert.IsFalse(NegroCineVR.Activo, "En pausa el juego se ve.");
            Pausa.Fijar(false);
            yield return Cuadros(2);
            Assert.IsTrue(NegroCineVR.Activo);

            // Al terminar se mantiene un momento (la siguiente de la fila) y luego todo vuelve.
            NegroCineVR.Forzado = false;
            yield return Cuadros(2);
            Assert.IsTrue(NegroCineVR.Activo, "Entre cinemáticas sigue en negro un momento: van de corrido.");
            yield return Segundos(NegroCineVR.SegundosRetencion + 0.3f);
            Assert.IsFalse(NegroCineVR.Activo);
            Assert.AreEqual(mascara, cabeza.cullingMask, "Al terminar la cabeza vuelve a dibujar todo.");
            Assert.AreEqual(fondo, cabeza.clearFlags);
            Assert.AreEqual(lejos, cabeza.farClipPlane, 1e-4f);
            Object.Destroy(textura);
        }
    }
}
