using System.Collections;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.Personaje.Pruebas;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>T-400: cámara de hombro, movimiento relativo a la cámara y disparo apuntando, con el prefab real y entrada simulada.</summary>
    public class TerceraPersonaPruebas : BaseJugadorAPie
    {
        private static float DistanciaHorizontal(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }

        [UnityTest]
        public IEnumerator CaminarYCamara()
        {
            yield return Calentar();
            Vector3 inicio = jugador.transform.position;
            Vector3 haciaDondeMira = Vector3.ProjectOnPlane(controlador.Camara.forward, Vector3.up).normalized;

            Press(teclado.wKey);
            yield return Esperar(1f);
            Release(teclado.wKey);
            yield return Esperar(0.4f);

            Vector3 avance = jugador.transform.position - inicio;
            avance.y = 0f;
            Assert.GreaterOrEqual(Vector3.Dot(avance, haciaDondeMira), 3f, "Avanzó al menos 3 m hacia donde mira la cámara");

            Transform camara = controlador.Camara;
            float distancia = DistanciaHorizontal(camara.position, jugador.transform.position);
            Assert.That(distancia, Is.InRange(2.5f, 4f), "La cámara queda entre 2.5 y 4 m del jugador");
            Vector3 relativa = camara.position - jugador.transform.position;
            Assert.Less(Vector3.Dot(relativa, haciaDondeMira), 0f, "La cámara queda atrás");
            Assert.Greater(Vector3.Dot(relativa, Vector3.Cross(Vector3.up, haciaDondeMira)), 0f, "La cámara queda a la derecha");

            Press(raton.rightButton);
            yield return Esperar(0.7f);
            Assert.IsTrue(controlador.Apuntando);
            Assert.Less(DistanciaHorizontal(camara.position, jugador.transform.position), 2f, "Apuntando la cámara se acerca");
            Release(raton.rightButton);
        }

        [UnityTest]
        public IEnumerator ApuntarYDisparar()
        {
            yield return Calentar();
            GameObject objetivo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            objetivo.transform.position = new Vector3(0f, 4f, 8f);
            objetivo.transform.localScale = new Vector3(3f, 8f, 1f);
            MunecoAPie muneco = objetivo.AddComponent<MunecoAPie>();
            Physics.SyncTransforms();
            yield return null;
            float maxima = muneco.Maxima;

            Press(teclado.digit2Key);
            yield return null;
            Release(teclado.digit2Key);
            yield return null;
            Assert.AreEqual(ArmaAPie.Pistola, jugador.GetComponent<ArmasAPie>().ArmaActiva);

            Press(raton.rightButton);
            yield return Esperar(0.5f);
            Press(raton.leftButton);
            yield return null;
            Release(raton.leftButton);
            yield return Esperar(0.2f);
            Release(raton.rightButton);

            Assert.Less(muneco.Actual, maxima, "El disparo apuntando le bajó la vida al muñeco");
            Object.Destroy(objetivo);
        }
    }
}
