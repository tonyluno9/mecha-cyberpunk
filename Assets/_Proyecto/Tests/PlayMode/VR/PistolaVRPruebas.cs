using System.Collections;
using MechaCyberpunk.CampoTiro;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.VR;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.VR
{
    /// <summary>VR2: la pistola en la mano derecha (apunta con el control, dispara con el gatillo; VR27: A suelta el cargador y A otra vez recarga) y los golpes con las manos.</summary>
    public class PistolaVRPruebas : BaseJugadorVR
    {
        private static readonly Vector3 ManoDerReposo = new Vector3(0.2f, 1.2f, 0.3f);
        private static readonly Vector3 ManoIzqReposo = new Vector3(-0.2f, 1.2f, 0.2f);

        private PistolaVR pistolaVR;
        private PistolaAPie pistola;
        private ArmasAPie armas;
        private GolpesManosVR golpes;

        private IEnumerator ArmarConPistola()
        {
            CargadorPistola.MunicionInfinita = false;
            yield return ArmarVR();
            pistolaVR = jugador.GetComponent<PistolaVR>();
            golpes = jugador.GetComponent<GolpesManosVR>();
            pistola = jugador.GetComponent<PistolaAPie>();
            armas = jugador.GetComponent<ArmasAPie>();
            Assert.IsNotNull(pistolaVR, "PistolaVR debe agregarse sola al armarse el rig.");
            Assert.IsNotNull(golpes, "GolpesManosVR debe agregarse solo al armarse el rig.");
            if (armas.ArmaActiva != ArmaAPie.Pistola)
            {
                Assert.IsTrue(armas.Equipar(ArmaAPie.Pistola), "El jugador debe poder sacar la pistola.");
            }
            yield return Cuadros(3);
        }

        private IEnumerator CrearObjetivo(Vector3 posicion, System.Action<ObjetivoTiro> listo)
        {
            ObjetivoTiro objetivo = ObjetivoTiro.Crear(null, TipoObjetivoTiro.Centinela, null, posicion, Quaternion.identity);
            extras.Add(objetivo.gameObject);
            for (int i = 0; i < 30 && !objetivo.EstaListo; i++)
            {
                yield return null;
            }
            Assert.IsTrue(objetivo.EstaListo, "El objetivo de tiro no se armó.");
            yield return Cuadros(2);
            listo(objetivo);
        }

        [UnityTest]
        public IEnumerator PistolaCuelgaDeLaManoDerechaYApuntaConElControl()
        {
            yield return ArmarConPistola();
            Assert.IsTrue(pistolaVR.ArmaVisible, "La pistola debe verse en la mano.");
            Assert.AreEqual(ArmaAPie.Pistola, pistolaVR.ArmaMostrada);
            Assert.IsTrue(pistolaVR.Arma.IsChildOf(adaptador.ManoDerecha.transform), "La pistola debe colgar de la mano derecha del rig.");
            Assert.AreEqual(pistolaVR.Boca, pistola.BocaExterna, "Las balas deben salir de la boca del arma en la mano.");
            Renderer[] renders = pistolaVR.Arma.GetComponentsInChildren<Renderer>();
            Assert.Greater(renders.Length, 0, "La pistola debe tener el modelo real.");
            yield return Segundos(0.7f); // el adaptador vuelve a ocultar el cuerpo cada 0.5 s
            foreach (Renderer r in renders)
            {
                Assert.AreNotEqual(ShadowCastingMode.ShadowsOnly, r.shadowCastingMode, r.name + " de la pistola no se vería.");
            }

            Quaternion giro = Quaternion.Euler(15f, 40f, 0f);
            PonerMano(true, ManoDerReposo, giro);
            yield return Cuadros(3);
            Vector3 esperado = adaptador.Rig.rotation * giro * Quaternion.Euler(PistolaVR.InclinacionGrados, 0f, 0f) * Vector3.forward;
            float angulo = Vector3.Angle(esperado, pistolaVR.Boca.forward);
            Debug.Log("[VR] Ángulo entre el control y el cañón: " + angulo.ToString("0.00") + "°");
            Assert.Less(angulo, 2f, "El cañón debe apuntar hacia donde apunta el control.");
            Assert.Less(Vector3.Distance(pistolaVR.Arma.position, adaptador.ManoDerecha.transform.TransformPoint(PistolaVR.PosicionEnMano)), 0.01f);
        }

        [UnityTest]
        public IEnumerator GatilloDerechoDisparaDesdeElCanonYGastaBala()
        {
            yield return ArmarConPistola();
            // La cabeza mira hacia atrás: la bala debe ir por el arma, no por la vista.
            PonerCabeza(new Vector3(0f, 1.6f, 0f), Quaternion.Euler(0f, 180f, 0f));
            PonerMano(true, ManoDerReposo, Quaternion.identity);
            yield return Cuadros(5);
            Transform boca = pistolaVR.Boca;
            Vector3 frente = Vector3.ProjectOnPlane(boca.forward, Vector3.up).normalized;
            Vector3 lugar = boca.position + frente * 6f;
            lugar.y = jugador.transform.position.y + 1f; // el maniquí sin modelo queda centrado en su base
            ObjetivoTiro objetivo = null;
            yield return CrearObjetivo(lugar, o => objetivo = o);

            Vector3 origenTrazo = Vector3.zero;
            int trazos = 0;
            System.Action<ArmaAPie, Vector3, Vector3> alTrazo = (arma, desde, hasta) =>
            {
                origenTrazo = desde;
                trazos++;
            };
            pistola.Trazo += alTrazo;
            float vidaAntes = objetivo.Vida;
            int balasAntes = pistola.Balas;
            int disparosAntes = pistolaVR.DisparosHechos;
            Vector3 bocaAlDisparar = pistolaVR.Boca.position;
            yield return Pulsar(controlDer.triggerButton);
            yield return Cuadros(2);
            pistola.Trazo -= alTrazo;

            Collider cajaT = objetivo.GetComponent<Collider>();
            Debug.Log("[VR] Diagnóstico tiro: boca " + bocaAlDisparar.ToString("F2") + " frente " + pistolaVR.Boca.forward.ToString("F2") + ", caja " + (cajaT != null ? cajaT.bounds.ToString() : "sin collider"));
            Debug.Log("[VR] Vida del objetivo: " + vidaAntes + " -> " + objetivo.Vida + "; balas " + balasAntes + " -> " + pistola.Balas);
            Assert.AreEqual(disparosAntes + 1, pistolaVR.DisparosHechos, "El gatillo derecho debe disparar una vez.");
            Assert.AreEqual(balasAntes - 1, pistola.Balas, "El tiro gasta una bala del cargador.");
            Assert.Greater(trazos, 0, "El tiro debe dejar su trazo.");
            Assert.Less(Vector3.Distance(origenTrazo, bocaAlDisparar), 0.05f, "La bala sale de la boca del arma en la mano.");
            Assert.Less(objetivo.Vida, vidaAntes, "El objetivo delante del cañón debe perder vida.");
        }

        [UnityTest]
        public IEnumerator BotonA_SueltaElCargador_YAOtraVezRecarga()
        {
            yield return ArmarConPistola();
            RecargaManualVR recarga = jugador.GetComponent<RecargaManualVR>();
            Assert.IsNotNull(recarga, "RecargaManualVR debe agregarse sola al armarse el rig.");
            yield return Pulsar(controlDer.triggerButton);
            yield return Cuadros(2);
            Assert.AreEqual(pistola.TamanoCargador - 1, pistola.Balas, "Antes de recargar debe faltar una bala.");
            yield return Pulsar(controlDer.primaryButton);
            Assert.AreEqual(EstadoCargadorVR.Fuera, recarga.Estado, "A (VR27) suelta el cargador.");
            Assert.IsFalse(pistola.Recargando, "Soltar el cargador aún no recarga.");
            int disparos = pistolaVR.DisparosHechos;
            yield return Pulsar(controlDer.triggerButton);
            Assert.AreEqual(disparos, pistolaVR.DisparosHechos, "Sin cargador el gatillo no dispara.");
            yield return Pulsar(controlDer.primaryButton);
            Assert.IsTrue(pistola.Recargando, "A otra vez con el cargador fuera recarga directo (no se queda atorado).");
            float fin = Time.time + 6f;
            while (pistola.Recargando && Time.time < fin)
            {
                yield return null;
            }
            Assert.IsFalse(pistola.Recargando, "La recarga debe terminar.");
            Assert.AreEqual(pistola.TamanoCargador, pistola.Balas, "Tras recargar el cargador queda lleno.");
        }

        [UnityTest]
        public IEnumerator GolpeRapidoConLaManoHaceDanio()
        {
            yield return ArmarConPistola();
            PonerMano(false, ManoIzqReposo, Quaternion.identity);
            ObjetivoTiro objetivo = null;
            Vector3 p = jugador.transform.position;
            yield return CrearObjetivo(new Vector3(p.x - 0.2f, p.y + 1f, p.z + 1.3f), o => objetivo = o);
            yield return Cuadros(5);
            float vidaAntes = objetivo.Vida;
            // 0.8 m en 0.12 s: unos 6.7 m/s.
            yield return MoverMano(false, ManoIzqReposo, ManoIzqReposo + new Vector3(0f, 0f, 0.8f), 0.12f, Quaternion.identity);
            yield return Cuadros(3);
            Collider cajaObj = objetivo.GetComponent<Collider>();
            Debug.Log("[VR] Diagnóstico golpe: mano " + adaptador.ManoIzquierda.Palma.ToString("F2") + ", caja " + (cajaObj != null ? cajaObj.bounds.ToString() : "sin collider")
                + ", capa " + objetivo.gameObject.layer + ", control " + controlador.ControlActivo + ", bloqueada " + controlador.EntradaBloqueada);
            Debug.Log("[VR] Golpe rápido: " + golpes.Golpes + " golpes, " + golpes.UltimaVelocidad.ToString("0.0") + " m/s, daño " + golpes.UltimoDanio.ToString("0.0") + "; vida " + vidaAntes + " -> " + objetivo.Vida);
            Assert.AreEqual(1, golpes.Golpes, "Un movimiento rápido contra el objetivo es un golpe (uno solo).");
            Assert.Greater(golpes.UltimaVelocidad, ReglaGolpeManoVR.VelocidadMinima);
            Assert.IsFalse(golpes.UltimoConDerecha);
            Assert.Less(objetivo.Vida, vidaAntes, "El golpe rápido debe quitar vida.");
        }

        [UnityTest]
        public IEnumerator GolpeLentoNoHaceDanio()
        {
            yield return ArmarConPistola();
            PonerMano(false, ManoIzqReposo, Quaternion.identity);
            ObjetivoTiro objetivo = null;
            Vector3 p = jugador.transform.position;
            yield return CrearObjetivo(new Vector3(p.x - 0.2f, p.y + 1f, p.z + 1.3f), o => objetivo = o);
            yield return Cuadros(5);
            float vidaAntes = objetivo.Vida;
            // 0.8 m en 1.5 s: unos 0.53 m/s (menos de 0.8 m/s).
            yield return MoverMano(false, ManoIzqReposo, ManoIzqReposo + new Vector3(0f, 0f, 0.8f), 1.5f, Quaternion.identity);
            yield return Cuadros(3);
            Assert.AreEqual(0, golpes.Golpes, "Tocar despacio no es un golpe.");
            Assert.AreEqual(vidaAntes, objetivo.Vida, 1e-4f, "Tocar despacio no quita vida.");
        }

        [UnityTest]
        public IEnumerator GolpeRapidoContraElPropioCuerpoNoCuenta()
        {
            yield return ArmarConPistola();
            Vector3 desde = new Vector3(-0.45f, 1.0f, 0.05f);
            Vector3 hasta = new Vector3(0.45f, 1.0f, 0.05f);
            PonerMano(false, desde, Quaternion.identity);
            yield return Cuadros(5);
            yield return MoverMano(false, desde, hasta, 0.12f, Quaternion.identity);
            yield return Cuadros(3);
            Assert.AreEqual(0, golpes.Golpes, "La mano no le pega al propio jugador.");
        }

        [Test]
        public void FactorDeRapidez_VaDeUnoADos()
        {
            Assert.IsFalse(ReglaGolpeManoVR.Golpea(0.7f));
            Assert.IsTrue(ReglaGolpeManoVR.Golpea(0.9f));
            Assert.AreEqual(1f, ReglaGolpeManoVR.Factor(1.6f), 1e-5f);
            Assert.AreEqual(1.5f, ReglaGolpeManoVR.Factor(4.5f), 1e-5f);
            Assert.AreEqual(2f, ReglaGolpeManoVR.Factor(20f), 1e-5f);
        }
    }
}
