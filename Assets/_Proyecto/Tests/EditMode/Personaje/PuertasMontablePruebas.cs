using MechaCyberpunk.Personaje.Interaccion;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>T-655: la puerta del lado opuesto es el espejo de la del conductor respecto del centro del carro.</summary>
    public class PuertasMontablePruebas
    {
        [Test]
        public void Espejar_MarcoGirado_InvierteLaXLocal()
        {
            var marco = new GameObject("Marco").transform;
            marco.SetPositionAndRotation(new Vector3(10f, 0f, 5f), Quaternion.Euler(0f, 90f, 0f));
            Vector3 puerta = marco.TransformPoint(new Vector3(-1.9f, 0f, 0.3f));

            Vector3 espejo = PuertasMontable.Espejar(puerta, marco);

            Vector3 local = marco.InverseTransformPoint(espejo);
            Assert.AreEqual(1.9f, local.x, 1e-4f);
            Assert.AreEqual(0.3f, local.z, 1e-4f);
            Object.DestroyImmediate(marco.gameObject);
        }
    }
}
