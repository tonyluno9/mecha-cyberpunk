using MechaCyberpunk.Mundo.Ambiente;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>S2c (7-nov): la cámara del reflejo de los charcos va en el ojo reflejado en el piso.</summary>
    public class ReglaReflejoPlanoPruebas
    {
        [Test]
        public void Reflejar_CambiaSoloLaAlturaRespectoAlPlano()
        {
            Vector3 r = ReglaReflejoPlano.Reflejar(new Vector3(3f, 2.5f, -4f), 0.2f);
            Assert.AreEqual(3f, r.x, 0.0001f);
            Assert.AreEqual(-2.1f, r.y, 0.0001f);
            Assert.AreEqual(-4f, r.z, 0.0001f);
        }

        [Test]
        public void Giro_MiraArribaSiLaCamaraMiraAbajo()
        {
            Vector3 adelante = new Vector3(0f, -0.3f, 1f).normalized;
            Quaternion g = ReglaReflejoPlano.Giro(adelante, new Vector3(0f, 1f, 0.3f).normalized);
            Vector3 f = g * Vector3.forward;
            Assert.AreEqual(-adelante.y, f.y, 0.0001f, "el frente reflejado sube lo que bajaba el original");
            Assert.AreEqual(adelante.z, f.z, 0.0001f);
        }

        [Test]
        public void PuntoDelPiso_SeVeIgualDesdeLosDosOjos()
        {
            // Un punto del charco está a la misma distancia del ojo y del ojo reflejado (por eso el shader puede proyectarlo en la cámara del reflejo).
            Vector3 ojo = new Vector3(1f, 2.4f, -3f);
            Vector3 punto = new Vector3(4f, 0.2f, 8f);
            Vector3 reflejado = ReglaReflejoPlano.Reflejar(ojo, 0.2f);
            Assert.AreEqual(Vector3.Distance(ojo, punto), Vector3.Distance(reflejado, punto), 0.0001f);
        }
    }
}
