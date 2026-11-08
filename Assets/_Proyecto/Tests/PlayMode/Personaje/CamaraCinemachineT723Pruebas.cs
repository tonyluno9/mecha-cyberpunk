using System.Collections;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.Personaje.Movimiento;
using NUnit.Framework;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>
    /// T-723: la cámara del jugador a pie es de Cinemachine (Brain + vcam con ThirdPersonFollow y Deoccluder), detrás y un poco arriba del hombro
    /// como CAM01_APie.png, el zoom solo cambia al apuntar, no atraviesa paredes y se acerca bajo techos y en cuartos chicos.
    /// Con CAPTURAR_S1=1 deja docs/capturas/s1d-T-723-*.png de la cámara del jugador.
    /// </summary>
    public class CamaraCinemachineT723Pruebas : BaseJugadorAPie
    {
        public const float AlturaPersonaje = 1.7f;
        private CamaraHombro Hombro => jugador.GetComponentInChildren<CamaraHombro>();
        private Camera Camara => Hombro.GetComponent<Camera>();

        /// <summary>Del personaje (1.7 m de alto): fracción de la pantalla que ocupa, centro X y pies/cabeza (viewport, 0 abajo).</summary>
        public static void Medir(GameObject raiz, Camera camara, out float fraccion, out float centroX, out float pies, out float cabeza)
        {
            Vector3 abajo = camara.WorldToViewportPoint(raiz.transform.position);
            Vector3 arriba = camara.WorldToViewportPoint(raiz.transform.position + Vector3.up * AlturaPersonaje);
            pies = abajo.y;
            cabeza = arriba.y;
            fraccion = arriba.y - abajo.y;
            centroX = (abajo.x + arriba.x) * 0.5f;
        }

        [UnityTest]
        public IEnumerator Estructura_UnBrainUnaVcamConSeguirYDesocluir_ySinOtraCamaraPeleando()
        {
            yield return Calentar();
            yield return Esperar(0.4f);
            CamaraHombro hombro = Hombro;
            Assert.IsNotNull(hombro.Brain, "La cámara del jugador tiene CinemachineBrain");
            Assert.IsTrue(hombro.Brain.enabled, "El Brain trabaja con el jugador a pie");
            Assert.IsNotNull(hombro.Vcam, "Hay una cámara virtual");
            Assert.IsNotNull(hombro.Vcam.GetComponent<CinemachineThirdPersonFollow>(), "Detrás del hombro: ThirdPersonFollow");
            Assert.IsNotNull(hombro.Vcam.GetComponent<CinemachineDeoccluder>(), "No atraviesa paredes: Deoccluder");
            Assert.AreEqual(1, Object.FindObjectsByType<CinemachineBrain>(FindObjectsSortMode.None).Length, "Un solo Brain");
            Assert.AreEqual(1, Object.FindObjectsByType<CinemachineCamera>(FindObjectsSortMode.None).Length, "Una sola vcam: no hay dos cámaras peleando");
            Assert.AreSame(hombro.Vcam, hombro.Brain.ActiveVirtualCamera as CinemachineCamera, "El Brain muestra la vcam del jugador");
        }

        [UnityTest]
        public IEnumerator Calle_DetrasYUnPocoArribaDelHombro_PersonajeCentradoYComoCAM01()
        {
            yield return Calentar();
            yield return Esperar(0.6f);
            Camera camara = Camara;
            Medir(jugador, camara, out float fraccion, out float centroX, out float pies, out float cabeza);
            CapturaS1.FotoCamara("s1d-T-723-calle-llano", camara);
            Debug.Log("[T723] local " + jugador.transform.InverseTransformPoint(camara.transform.position) + " fov " + camara.fieldOfView);
            Debug.Log("[T723] fraccion " + fraccion.ToString("F3") + " centroX " + centroX.ToString("F3") + " pies " + pies.ToString("F3") + " cabeza " + cabeza.ToString("F3"));
            Assert.That(fraccion, Is.InRange(0.36f, 0.5f), "El personaje ocupa ~40 % del alto de la pantalla (CAM01)");
            Assert.AreEqual(0.5f, centroX, 0.05f, "Centrado en horizontal");
            Assert.Less(pies, 0.2f, "Los pies quedan en la parte de abajo");
            Assert.That(cabeza, Is.InRange(0.4f, 0.65f), "La cabeza queda cerca del centro: se ve la calle por delante");
            Vector3 local = jugador.transform.InverseTransformPoint(camara.transform.position);
            Assert.That(local.y, Is.InRange(1.6f, 2.0f), "La cámara va un poco arriba del hombro");
            Assert.That(local.z, Is.InRange(-3.8f, -3.0f), "Detrás del personaje");

            controlador.Empujar(0f, 22f);
            yield return Esperar(0.3f);
            Medir(jugador, camara, out float fraccion2, out _, out _, out _);
            Debug.Log("[T723] cerca (mirando abajo) fraccion " + fraccion2.ToString("F3"));
            CapturaS1.FotoCamara("s1d-T-723-cerca-llano", camara);
        }

        [UnityTest]
        public IEnumerator Zoom_SoloCambiaAlApuntar_NoAlCaminarNiCorrer()
        {
            Caja(new Vector3(0f, 1f, 30f), new Vector3(6f, 2f, 0.5f));
            yield return Calentar();
            yield return Esperar(0.5f);
            Camera camara = Camara;
            float fovQuieto = camara.fieldOfView;
            Assert.AreEqual(60f, fovQuieto, 0.5f);

            Press(teclado.wKey);
            yield return Esperar(0.6f);
            Assert.AreEqual(fovQuieto, camara.fieldOfView, 0.01f, "Caminando el zoom no cambia");
            Press(teclado.leftShiftKey);
            yield return Esperar(1.0f);
            Assert.AreEqual(fovQuieto, camara.fieldOfView, 0.01f, "Corriendo el zoom no cambia");
            Release(teclado.leftShiftKey);
            Release(teclado.wKey);
            yield return Esperar(0.3f);

            Assert.IsTrue(jugador.GetComponent<ArmasAPie>().Equipar(ArmaAPie.Pistola));
            Vector3 normal = jugador.transform.InverseTransformPoint(camara.transform.position);
            Press(raton.rightButton);
            yield return Esperar(1.0f);
            Assert.Less(camara.fieldOfView, fovQuieto - 5f, "Apuntando sí hace zoom");
            Vector3 apuntando = jugador.transform.InverseTransformPoint(camara.transform.position);
            Assert.AreEqual(normal.x, apuntando.x, 0.05f, "T-682: apuntando queda centrada");
            Assert.AreEqual(normal.z, apuntando.z, 0.05f, "T-682: misma distancia al apuntar");
            Assert.AreEqual(0.5f, camara.WorldToViewportPoint(jugador.transform.position + Vector3.up * 1.2f).x, 0.03f);
            CapturaS1.FotoCamara("s1d-T-723-apuntando", camara);
            Release(raton.rightButton);
            yield return Esperar(1.0f);
            Assert.AreEqual(fovQuieto, camara.fieldOfView, 0.3f, "Al soltar vuelve al zoom normal");
        }

        [UnityTest]
        public IEnumerator Pared_DetrasDelJugador_LaCamaraNoLaAtraviesaYSeAcerca()
        {
            Caja(new Vector3(0f, 2f, -1.6f), new Vector3(12f, 4f, 0.5f)); // muro a 1.35 m detrás
            yield return Calentar();
            yield return Esperar(0.6f);
            Vector3 p = Camara.transform.position;
            Assert.Greater(p.z, -1.35f, "La cámara no atraviesa el muro: " + p);
            Assert.Less(Vector3.Distance(p, jugador.transform.position + Vector3.up * 1.55f), 3.0f, "Se acercó al jugador");
            Assert.IsFalse(Physics.CheckSphere(p, 0.05f, ~0, QueryTriggerInteraction.Ignore), "La cámara no queda dentro de nada");
        }

        [UnityTest]
        public IEnumerator Cuarto_ChicoConTecho_LaCamaraQuedaDentroDelCuartoYNoVeSoloElTecho()
        {
            // Cuarto de 3 x 3 m con techo a 2.4 m: 4 muros y un techo.
            Caja(new Vector3(0f, 1.2f, -1.6f), new Vector3(3.5f, 2.4f, 0.2f));
            Caja(new Vector3(0f, 1.2f, 1.6f), new Vector3(3.5f, 2.4f, 0.2f));
            Caja(new Vector3(-1.6f, 1.2f, 0f), new Vector3(0.2f, 2.4f, 3.5f));
            Caja(new Vector3(1.6f, 1.2f, 0f), new Vector3(0.2f, 2.4f, 3.5f));
            Caja(new Vector3(0f, 2.5f, 0f), new Vector3(3.5f, 0.2f, 3.5f));
            yield return Calentar();
            yield return Esperar(0.7f);
            Camera camara = Camara;
            Vector3 p = camara.transform.position;
            Assert.That(p.x, Is.InRange(-1.5f, 1.5f), "Dentro del cuarto (x): " + p);
            Assert.That(p.z, Is.InRange(-1.5f, 1.5f), "Dentro del cuarto (z): " + p);
            Assert.Less(p.y, 2.4f, "Bajo el techo: " + p);
            Medir(jugador, camara, out float fraccion, out float centroX, out _, out _);
            Assert.Greater(fraccion, 0.3f, "En el cuarto se ve al personaje, no solo el techo");
            Assert.AreEqual(0.5f, centroX, 0.1f);
            CapturaS1.FotoCamara("s1d-T-723-cuarto-chico", camara);
        }

        [UnityTest]
        public IEnumerator Sin_Control_ElBrainSeApagaYNoMueveLaCamara()
        {
            yield return Calentar();
            yield return Esperar(0.4f);
            Assert.IsTrue(Hombro.Brain.enabled);
            controlador.ActivarControl(false);
            yield return null;
            Assert.IsFalse(Hombro.Brain.enabled, "Sin control (menú, cinemática) el Brain no toca la cámara");
            Camara.transform.position = new Vector3(5f, 7f, 9f);
            yield return Esperar(0.2f);
            Assert.AreEqual(new Vector3(5f, 7f, 9f), Camara.transform.position, "Otro sistema puede mover la cámara sin que Cinemachine pelee");
            controlador.ActivarControl(true);
            yield return Esperar(0.3f);
            Assert.IsTrue(Hombro.Brain.enabled);
            Vector3 local = jugador.transform.InverseTransformPoint(Camara.transform.position);
            Assert.AreEqual(0f, local.x, 0.05f, "Al volver el control la cámara vuelve detrás del jugador");
        }
    }
}
