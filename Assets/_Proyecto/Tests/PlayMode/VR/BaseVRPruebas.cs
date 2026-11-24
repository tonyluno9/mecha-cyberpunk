using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Movimiento;
using MechaCyberpunk.VR;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.VR
{
    /// <summary>VR1: rig en el jugador, cabeza a la altura de PJ01, caminar hacia donde mira la cabeza, giro de 45°, cuerpo solo sombra.</summary>
    public class BaseVRPruebas : BaseJugadorVR
    {
        [UnityTest]
        public IEnumerator RigArmadoConCabezaALaAlturaDePJ01()
        {
            yield return ArmarVR();
            Camera cam = adaptador.Camara;
            Assert.AreEqual(adaptador.Ajuste, cam.transform.parent, "La cámara del jugador debe colgar del rig VR.");
            CamaraHombro hombro = cam.GetComponent<CamaraHombro>();
            Assert.IsTrue(hombro == null || !hombro.enabled, "La cámara de hombro debe apagarse en VR.");
            float altura = cam.transform.position.y - jugador.transform.position.y;
            Debug.Log("[VR] Altura de los ojos sobre los pies: " + altura.ToString("0.000"));
            Assert.AreEqual(AdaptadorVR.AlturaOjos, altura, 0.05f);
            Assert.AreEqual(jugador.transform.position.x, cam.transform.position.x, 0.02f, "La cabeza va sobre la cápsula.");
            Assert.AreEqual(jugador.transform.position.z, cam.transform.position.z, 0.02f, "La cabeza va sobre la cápsula.");
        }

        [UnityTest]
        public IEnumerator StickIzquierdoCaminaHaciaDondeMiraLaCabeza()
        {
            yield return ArmarVR();
            // La cabeza mira a +X (90°): el stick hacia adelante camina hacia +X.
            PonerCabeza(new Vector3(0f, 1.6f, 0f), Quaternion.Euler(0f, 90f, 0f));
            yield return Cuadros(3);
            Vector3 inicio = jugador.transform.position;
            Stick(false, new Vector2(0f, 1f));
            yield return Segundos(1.5f);
            Stick(false, Vector2.zero);
            Vector3 avance = jugador.transform.position - inicio;
            Debug.Log("[VR] Avance con el stick en 1.5 s: " + avance.ToString("F2"));
            Assert.Greater(avance.x, 1.5f, "Debe avanzar hacia +X (a 1.62 m/s).");
            Assert.Less(Mathf.Abs(avance.z), 0.3f);
            Assert.Less(avance.x, 2.6f, "Caminar es a 1.62 m/s, no a trote.");
        }

        [UnityTest]
        public IEnumerator ClicDelStickCorreATrote()
        {
            yield return ArmarVR();
            Vector3 inicio = jugador.transform.position;
            Mantener(controlIzq.primary2DAxisClick);
            Stick(false, new Vector2(0f, 1f));
            yield return Segundos(1.5f);
            Stick(false, Vector2.zero);
            Soltar(controlIzq.primary2DAxisClick);
            float avance = (jugador.transform.position - inicio).magnitude;
            Debug.Log("[VR] Avance corriendo en 1.5 s: " + avance.ToString("F2"));
            Assert.Greater(avance, 4.5f, "Con el clic del stick corre (4.53 m/s).");
        }

        [UnityTest]
        public IEnumerator StickDerechoGira45GradosPorPaso()
        {
            yield return ArmarVR();
            float antes = adaptador.GiroRig;
            Vector3 cabezaAntes = adaptador.Camara.transform.position;
            Stick(true, new Vector2(1f, 0f));
            yield return Cuadros(10);
            Stick(true, Vector2.zero);
            yield return Cuadros(3);
            Assert.AreEqual(45f, Mathf.DeltaAngle(antes, adaptador.GiroRig), 0.01f, "Un paso de giro son 45°.");
            Stick(true, new Vector2(1f, 0f));
            yield return Cuadros(3);
            Stick(true, Vector2.zero);
            yield return Cuadros(3);
            Assert.AreEqual(90f, Mathf.DeltaAngle(antes, adaptador.GiroRig), 0.01f, "Mantener el stick no repite: hay que soltarlo.");
            Vector3 cabezaDespues = adaptador.Camara.transform.position;
            Assert.Less(Vector2.Distance(new Vector2(cabezaAntes.x, cabezaAntes.z), new Vector2(cabezaDespues.x, cabezaDespues.z)), 0.05f, "Gira alrededor de la cabeza.");
        }

        [UnityTest]
        public IEnumerator CuerpoSoloDaSombraYLosControlesSeVen()
        {
            yield return ArmarVR();
            int cuerpo = 0;
            foreach (Renderer r in jugador.GetComponentsInChildren<Renderer>(true))
            {
                if (r.transform.IsChildOf(adaptador.Rig) || r is ParticleSystemRenderer || r is TrailRenderer || r is LineRenderer)
                {
                    continue;
                }
                cuerpo++;
                Assert.AreEqual(ShadowCastingMode.ShadowsOnly, r.shadowCastingMode, r.name + " del cuerpo se vería desde la cabeza.");
            }
            Assert.Greater(cuerpo, 0);
            Assert.IsNotNull(adaptador.ManoDerecha.GetComponentInChildren<Renderer>(), "El control derecho de los Starter Assets debe verse.");
            Assert.IsNotNull(adaptador.ManoIzquierda.GetComponentInChildren<Renderer>(), "El control izquierdo de los Starter Assets debe verse.");
        }

        [UnityTest]
        public IEnumerator LaManoSigueAlControlYMideSuVelocidad()
        {
            yield return ArmarVR();
            Vector3 desde = new Vector3(0.2f, 1.2f, 0.2f);
            Vector3 hasta = new Vector3(0.2f, 1.2f, 0.8f);
            PonerMano(true, desde, Quaternion.identity);
            yield return Cuadros(3);
            float maxima = 0f;
            float t = 0f;
            while (t < 0.2f)
            {
                t += Time.deltaTime;
                PonerMano(true, Vector3.Lerp(desde, hasta, t / 0.2f), Quaternion.identity);
                yield return null;
                maxima = Mathf.Max(maxima, adaptador.ManoDerecha.VelocidadRelativa.magnitude);
            }
            Debug.Log("[VR] Velocidad medida de la mano: " + maxima.ToString("0.00") + " m/s (esperada 3)");
            Assert.AreEqual(3f, maxima, 1f);
        }
    }

    /// <summary>VR1: sin visor el juego sigue igual (sin rig).</summary>
    public class SinVisorPruebas : BaseJugadorVR
    {
        [UnityTest]
        public IEnumerator SinModoVRNoSeArmaElRig()
        {
            ModoVR.Fijar(false);
            GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Proyecto/Personaje/Prefabs/JugadorAPie.prefab");
            jugador = Object.Instantiate(prefab, new Vector3(0f, 0.05f, 0f), Quaternion.identity);
            jugador.AddComponent<AdaptadorVR>();
            yield return Cuadros(5);
            Assert.IsNull(jugador.GetComponent<AdaptadorVR>(), "Sin VR el adaptador se quita solo.");
            Camera cam = jugador.GetComponentInChildren<Camera>();
            Assert.IsNotNull(cam.GetComponent<CamaraHombro>());
            Assert.IsTrue(cam.GetComponent<CamaraHombro>().enabled, "La cámara de tercera persona sigue igual.");
        }
    }
}
