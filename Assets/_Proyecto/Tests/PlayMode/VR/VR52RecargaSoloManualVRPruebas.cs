using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.VR;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.VR
{
    /// <summary>
    /// VR52: en VR el arma no se recarga sola. Vaciar el cargador deja el arma vacía (el gatillo hace el clic vacío), aparece el aviso
    /// "A: RECARGAR" y A + el gesto recargan. En PC la recarga automática sigue igual; la granada se repone sola también en VR.
    /// </summary>
    public class VR52RecargaSoloManualVRPruebas : BaseJugadorVR
    {
        [Test]
        public void RecargaAutomatica_EnPCSi_EnVRSoloLaGranada()
        {
            ModoVR.Fijar(false);
            Assert.IsTrue(PistolaAPie.RecargaAutomatica(ArmaAPie.Pistola), "En PC no cambia nada.");
            Assert.IsTrue(PistolaAPie.RecargaAutomatica(ArmaAPie.Fusil));
            ModoVR.Fijar(true, true);
            Assert.IsFalse(PistolaAPie.RecargaAutomatica(ArmaAPie.Pistola), "En VR la pistola no recarga sola.");
            Assert.IsFalse(PistolaAPie.RecargaAutomatica(ArmaAPie.Lanzagranadas));
            Assert.IsTrue(PistolaAPie.RecargaAutomatica(ArmaAPie.Granada), "La granada se repone sola.");
            Assert.IsTrue(ReglaRecargaManualVR.MostrarAviso(0, false));
            Assert.IsFalse(ReglaRecargaManualVR.MostrarAviso(0, true), "Recargando no hay aviso.");
            Assert.IsFalse(ReglaRecargaManualVR.MostrarAviso(3, false));
        }

        [UnityTest]
        public IEnumerator VaciarElCargador_NoRecargaSolo_AvisaA_YARecarga()
        {
            CargadorPistola.MunicionInfinita = false;
            yield return ArmarVR();
            ArmasAPie armas = jugador.GetComponent<ArmasAPie>();
            PistolaAPie pistola = jugador.GetComponent<PistolaAPie>();
            AvisoRecargaVR aviso = jugador.GetComponent<AvisoRecargaVR>();
            Assert.IsNotNull(aviso, "AvisoRecargaVR se agrega solo al armarse el rig.");
            if (armas.ArmaActiva != ArmaAPie.Pistola)
            {
                Assert.IsTrue(armas.Equipar(ArmaAPie.Pistola));
            }
            PonerMano(true, new Vector3(0.2f, 1.2f, 0.3f), Quaternion.identity);
            yield return Cuadros(4);
            Assert.IsFalse(aviso.Visible, "Con balas no hay aviso.");

            float fin = Time.time + 15f;
            while (pistola.Balas > 0 && Time.time < fin)
            {
                yield return Pulsar(controlDer.triggerButton);
                yield return Segundos(0.25f);
            }
            Assert.AreEqual(0, pistola.Balas, "El cargador se vacía disparando.");
            // Gatillo con el cargador vacío: clic vacío, sin recarga automática.
            yield return Pulsar(controlDer.triggerButton);
            yield return Segundos(0.5f);
            Assert.IsFalse(pistola.Recargando, "En VR no recarga solo.");
            Assert.AreEqual(0, pistola.Balas);
            Assert.IsTrue(aviso.Visible, "Aparece el aviso.");
            Assert.AreEqual(AvisoRecargaVR.TextoRecargar, aviso.Texto);

            // A suelta el cargador y A otra vez recarga directo (el gesto completo lo prueban GestosArmaVRPruebas).
            yield return Pulsar(controlDer.primaryButton);
            yield return Cuadros(2);
            Assert.IsFalse(aviso.Visible, "Con el cargador fuera el aviso se quita.");
            yield return Pulsar(controlDer.primaryButton);
            yield return Cuadros(2);
            Assert.IsTrue(pistola.Recargando, "A recarga.");
            fin = Time.time + 5f;
            while (pistola.Recargando && Time.time < fin)
            {
                yield return null;
            }
            Assert.AreEqual(pistola.TamanoCargador, pistola.Balas, "Recargó con A.");
        }
    }
}
