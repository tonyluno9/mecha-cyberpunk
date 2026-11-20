using MechaCyberpunk.Casino.Meseras;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests
{
    /// <summary>T-1495: la mesera calibra su giro para no caminar de espaldas.</summary>
    public class CorreccionGiroMeseraPruebas
    {
        [Test]
        public void CorreccionDeGiro_CuerpoYaDeFrente_EsCero()
        {
            Assert.AreEqual(0f, CalculoRondaMesera.CorreccionDeGiro(Vector3.forward, Vector3.forward), 0.01f);
        }

        [Test]
        public void CorreccionDeGiro_CuerpoDeEspaldas_GiraMediaVuelta()
        {
            Assert.AreEqual(180f, Mathf.Abs(CalculoRondaMesera.CorreccionDeGiro(Vector3.forward, Vector3.back)), 0.01f);
        }

        [Test]
        public void CorreccionDeGiro_CuerpoGiradoANoventaGrados_CompensaConNoventaAlOtroLado()
        {
            // El cuerpo mira a la derecha de la raíz (+90°): hay que girar el modelo -90°.
            Assert.AreEqual(-90f, CalculoRondaMesera.CorreccionDeGiro(Vector3.forward, Vector3.right), 0.01f);
        }

        [Test]
        public void CorreccionDeGiro_AplicadaDejaElCuerpoAlFrente()
        {
            Vector3 raiz = new Vector3(1f, 0f, 1f).normalized;
            Vector3 cuerpo = Quaternion.Euler(0f, 130f, 0f) * raiz;
            float c = CalculoRondaMesera.CorreccionDeGiro(raiz, cuerpo);
            Vector3 corregido = Quaternion.Euler(0f, c, 0f) * cuerpo;
            Assert.Greater(Vector3.Dot(corregido, raiz), 0.999f);
        }

        [Test]
        public void CorreccionDeGiro_SinDireccion_EsCero()
        {
            Assert.AreEqual(0f, CalculoRondaMesera.CorreccionDeGiro(Vector3.zero, Vector3.forward));
            Assert.AreEqual(0f, CalculoRondaMesera.CorreccionDeGiro(Vector3.forward, Vector3.up));
        }
    }
}
