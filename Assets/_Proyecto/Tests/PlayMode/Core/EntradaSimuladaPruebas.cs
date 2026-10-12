using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Core
{
    /// <summary>
    /// Ejemplo de pruebas jugables con entrada simulada. Copia este patrón para las pruebas de PlayMode:
    /// InputTestFixture crea dispositivos falsos, se presionan teclas y se espera con yield.
    /// </summary>
    public class EntradaSimuladaPruebas : InputTestFixture
    {
        private const float DuracionMovimiento = 0.5f;

        /// <summary>Componente de prueba: avanza en +Z mientras la tecla W está presionada.</summary>
        private class AvanzaConW : MonoBehaviour
        {
            public float velocidad = 2f;

            private void Update()
            {
                Keyboard teclado = Keyboard.current;
                if (teclado != null && teclado.wKey.isPressed)
                {
                    transform.position += Vector3.forward * (velocidad * Time.deltaTime);
                }
            }
        }

        [Test]
        public void TeclaW_PresionarYSoltar_CambiaEstado()
        {
            Keyboard teclado = InputSystem.AddDevice<Keyboard>();

            Press(teclado.wKey);
            Assert.IsTrue(teclado.wKey.isPressed);

            Release(teclado.wKey);
            Assert.IsFalse(teclado.wKey.isPressed);
        }

        [UnityTest]
        public IEnumerator ObjetoConW_MantenerWMedioSegundo_Avanza()
        {
            Keyboard teclado = InputSystem.AddDevice<Keyboard>();
            GameObject objeto = new GameObject("PruebaAvanzaConW");
            AvanzaConW componente = objeto.AddComponent<AvanzaConW>();
            Vector3 inicio = objeto.transform.position;

            Press(teclado.wKey);
            float transcurrido = 0f;
            while (transcurrido < DuracionMovimiento)
            {
                transcurrido += Time.deltaTime;
                yield return null;
            }
            Release(teclado.wKey);

            float avance = objeto.transform.position.z - inicio.z;
            Object.Destroy(objeto);
            Assert.Greater(avance, componente.velocidad * DuracionMovimiento * 0.5f);
        }

        [UnityTest]
        public IEnumerator ObjetoConW_SinPresionarW_NoAvanza()
        {
            InputSystem.AddDevice<Keyboard>();
            GameObject objeto = new GameObject("PruebaSinW");
            objeto.AddComponent<AvanzaConW>();

            for (int i = 0; i < 10; i++)
            {
                yield return null;
            }

            float z = objeto.transform.position.z;
            Object.Destroy(objeto);
            Assert.AreEqual(0f, z);
        }
    }
}
