using System.Collections;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.Personaje.Movimiento;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>
    /// T-1001: al apuntar la cámara se acerca y pasa al hombro derecho (CAM05): el personaje queda a la izquierda y la cruceta libre. Si hay
    /// pared a la derecha cambia al hombro izquierdo, sin atravesarla. Con CAPTURAR_S1=1 deja docs/capturas/s1d-T-1001-*.png.
    /// </summary>
    public class CamaraHombroT1001Pruebas : BaseJugadorAPie
    {
        private CamaraHombro Hombro => jugador.GetComponentInChildren<CamaraHombro>();
        private Camera Camara => Hombro.GetComponent<Camera>();

        [UnityTest]
        public IEnumerator Apuntar_SeAcercaAlHombroDerecho_YAlSoltarVuelve()
        {
            Caja(new Vector3(0f, 1f, 14f), new Vector3(6f, 2f, 0.5f));
            yield return Calentar();
            yield return Esperar(0.5f);
            Assert.IsTrue(jugador.GetComponent<ArmasAPie>().Equipar(ArmaAPie.Pistola));
            Camera camara = Camara;
            Vector3 normal = jugador.transform.InverseTransformPoint(camara.transform.position);
            float centroNormal = camara.WorldToViewportPoint(jugador.transform.position + Vector3.up * 0.9f).x;
            Assert.That(centroNormal, Is.InRange(0.4f, 0.5f), "CAM04: casi centrado, un poco a la izquierda");
            CapturaS1.FotoCamara("s1d-T-1001-sin-apuntar", camara);

            Press(raton.rightButton);
            yield return Esperar(0.12f);
            Vector3 aMedias = jugador.transform.InverseTransformPoint(camara.transform.position);
            yield return Esperar(1.0f);
            Vector3 apuntando = jugador.transform.InverseTransformPoint(camara.transform.position);
            Debug.Log("[T1001] normal " + normal + " a medias " + aMedias + " apuntando " + apuntando);
            Assert.That(apuntando.x, Is.InRange(0.55f, 0.95f), "Sobre el hombro derecho");
            Assert.That(apuntando.z, Is.InRange(-2.0f, -1.0f), "Se acercó (de la cintura para arriba)");
            Assert.Greater(aMedias.z, normal.z + 0.05f, "La transición es suave: a medias va entre los dos encuadres");
            Assert.Less(aMedias.z, apuntando.z - 0.05f, "La transición es suave: no salta");
            Vector3 hombro = camara.WorldToViewportPoint(jugador.transform.position + Vector3.up * 1.3f);
            Assert.Less(hombro.x, 0.4f, "El personaje queda a la izquierda: no tapa la cruceta");
            Assert.Greater(Hombro.LadoActual, 0.95f, "Sin pared se queda en el hombro derecho");
            CapturaS1.FotoCamara("s1d-T-1001-apuntando", camara);

            Release(raton.rightButton);
            yield return Esperar(1.2f);
            Vector3 suelta = jugador.transform.InverseTransformPoint(camara.transform.position);
            Assert.AreEqual(normal.x, suelta.x, 0.05f, "Al soltar vuelve al encuadre de caminar");
            Assert.AreEqual(normal.z, suelta.z, 0.1f, "Al soltar vuelve a la distancia de caminar");
        }

        [UnityTest]
        public IEnumerator Apuntar_ConParedALaDerecha_CambiaAlHombroIzquierdoSinAtravesarla()
        {
            // Muro a 0.6 m a la derecha del jugador.
            Caja(new Vector3(0.85f, 2f, 0f), new Vector3(0.5f, 4f, 14f));
            yield return Calentar();
            yield return Esperar(0.5f);
            Assert.IsTrue(jugador.GetComponent<ArmasAPie>().Equipar(ArmaAPie.Pistola));
            Press(raton.rightButton);
            yield return Esperar(1.2f);
            Camera camara = Camara;
            Vector3 local = jugador.transform.InverseTransformPoint(camara.transform.position);
            Debug.Log("[T1001] con pared " + local + " lado " + Hombro.LadoActual);
            Assert.Less(Hombro.LadoActual, 0.05f, "Con pared a la derecha pasa al hombro izquierdo");
            Assert.Less(local.x, -0.4f, "La cámara queda a la izquierda del personaje");
            Assert.Less(camara.transform.position.x, 0.6f, "No atraviesa la pared");
            Assert.Greater(camara.WorldToViewportPoint(jugador.transform.position + Vector3.up * 1.3f).x, 0.6f, "El personaje queda a la derecha de la pantalla");
            CapturaS1.FotoCamara("s1d-T-1001-pared-derecha", camara);
            Release(raton.rightButton);
        }
    }
}
