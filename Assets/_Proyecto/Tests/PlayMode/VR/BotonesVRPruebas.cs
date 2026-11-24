using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.APie;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.VR;
using MechaCyberpunk.VR.Menus;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Tests.VR
{
    /// <summary>
    /// VR17–VR19 y VR27 con el simulador: el clic del stick derecho salta, A suelta el cargador (y ya no salta), Y es pausa, la rueda de armas se elige con la mano izquierda o el stick izquierdo, el grip izquierdo
    /// agarra con el implante, el giro suave es proporcional al stick (a medias más lento que a fondo, sin saltos) y el láser y el cañón salen
    /// de <see cref="ManoVR.Apuntado"/> (la pose aim).
    /// </summary>
    public class BotonesVRPruebas : BaseJugadorVR
    {
        private const string RutaSkull = "Assets/_Proyecto/IA/Prefabs/PandilleroArmado_Skulls.prefab";

        private ModoGiroVR giroAntes;
        private float velocidadAntes;
        private bool vinetaAntes;
        private PandilleroAPie skull;

        public override void Setup()
        {
            base.Setup();
            OpcionesVR.Recargar();
            giroAntes = OpcionesVR.Giro;
            velocidadAntes = OpcionesVR.VelocidadGiro;
            vinetaAntes = OpcionesVR.Vineta;
            OpcionesVR.FijarGiro(OpcionesVR.GiroPorDefecto);
            OpcionesVR.FijarVelocidadGiro(OpcionesVR.VelocidadGiroPorDefecto);
            AgarreImplanteVR.ForzarImplante = null;
            ManoVR.ApuntadoSinPose = null;
        }

        public override void TearDown()
        {
            if (skull != null)
            {
                Object.Destroy(skull.gameObject);
            }
            AgarreImplanteVR.ForzarImplante = null;
            ManoVR.ApuntadoSinPose = null;
            OpcionesVR.FijarGiro(giroAntes);
            OpcionesVR.FijarVelocidadGiro(velocidadAntes);
            OpcionesVR.FijarVineta(vinetaAntes);
            base.TearDown();
        }

        [UnityTest]
        public IEnumerator ClicStickDerecho_Salta()
        {
            yield return ArmarVR();
            float suelo = jugador.transform.position.y;
            yield return Pulsar(controlDer.primary2DAxisClick);
            float maxima = suelo;
            float fin = Time.time + 0.6f;
            while (Time.time < fin)
            {
                maxima = Mathf.Max(maxima, jugador.transform.position.y);
                yield return null;
            }
            Debug.Log("[VR] Salto con el clic del stick derecho: " + (maxima - suelo).ToString("0.00") + " m.");
            Assert.Greater(maxima - suelo, 0.2f, "El clic del stick derecho salta.");
        }

        [UnityTest]
        public IEnumerator A_SueltaElCargadorYNoSalta()
        {
            CargadorPistola.MunicionInfinita = false;
            yield return ArmarVR();
            ArmasAPie armas = jugador.GetComponent<ArmasAPie>();
            PistolaAPie pistola = jugador.GetComponent<PistolaAPie>();
            if (armas.ArmaActiva != ArmaAPie.Pistola)
            {
                Assert.IsTrue(armas.Equipar(ArmaAPie.Pistola), "El jugador debe poder sacar la pistola.");
            }
            yield return Cuadros(3);
            yield return Pulsar(controlDer.triggerButton);
            yield return Cuadros(2);
            Assert.AreEqual(pistola.TamanoCargador - 1, pistola.Balas, "Antes de recargar falta una bala.");
            float suelo = jugador.transform.position.y;
            yield return Pulsar(controlDer.primaryButton);
            RecargaManualVR recarga = jugador.GetComponent<RecargaManualVR>();
            Assert.IsNotNull(recarga);
            Assert.AreEqual(EstadoCargadorVR.Fuera, recarga.Estado, "A (derecho) suelta el cargador.");
            float maxima = suelo;
            for (int i = 0; i < 20; i++)
            {
                maxima = Mathf.Max(maxima, jugador.transform.position.y);
                yield return null;
            }
            Assert.Less(maxima - suelo, 0.1f, "A ya no salta.");
        }

        [UnityTest]
        public IEnumerator Y_EsPausaYXLaRueda()
        {
            yield return ArmarVR();
            Mantener(controlIzq.secondaryButton);
            yield return Cuadros(1);
            Assert.IsTrue(EntradaVR.Sostenido(EntradaVR.Menu), "Y (izquierdo) es la acción de pausa, como Menú.");
            Soltar(controlIzq.secondaryButton);
            yield return Cuadros(1);
            Assert.IsFalse(EntradaVR.Sostenido(EntradaVR.Menu));
            UnityEngine.InputSystem.InputAction rueda = AdaptadorVR.BuscarAccionAPie(MechaCyberpunk.UI.Rueda.RuedaArmasObjetos.NombreAccion);
            Assert.IsNotNull(rueda, "Falta la acción Rueda del mapa APie.");
            bool conX = false;
            foreach (UnityEngine.InputSystem.InputBinding b in rueda.bindings)
            {
                conX |= b.path == EntradaVR.RutaRueda;
            }
            Assert.IsTrue(conX, "X (mantener) es la rueda de armas.");
        }

        [Test]
        public void RuedaConManoIzquierda_ElCursorSigueALaManoYAlStick()
        {
            const float radio = 160f;
            var regla = new ReglaRuedaManoVR();
            Vector3 inicio = new Vector3(-0.2f, 1.2f, 0.3f);
            regla.Abrir(inicio);
            Assert.AreEqual(Vector2.zero, regla.Avanzar(inicio, Vector3.right, Vector3.up, Vector2.zero, radio), "Sin mover la mano el cursor queda al centro.");

            Vector2 derecha = regla.Avanzar(inicio + Vector3.right * ReglaRuedaManoVR.MetrosAlBorde, Vector3.right, Vector3.up, Vector2.zero, radio);
            Assert.AreEqual(radio, derecha.x, 0.5f, "Mover la mano 12 cm a la derecha lleva el cursor al borde derecho.");
            Assert.AreEqual(0f, derecha.y, 0.5f);

            Vector2 arriba = regla.Avanzar(inicio + Vector3.up * 0.06f + Vector3.forward * 0.3f, Vector3.right, Vector3.up, Vector2.zero, radio);
            Assert.AreEqual(radio * 0.5f, arriba.y, 0.5f, "Hacia arriba sube; lo que se mueve hacia adelante no cuenta.");
            Assert.AreEqual(0f, arriba.x, 0.5f);

            Vector2 lejos = regla.Avanzar(inicio + Vector3.left, Vector3.right, Vector3.up, Vector2.zero, radio);
            Assert.AreEqual(radio, lejos.magnitude, 0.5f, "No sale del borde.");

            // El stick izquierdo manda sobre la mano y la mano sigue desde ahí.
            Vector3 aqui = inicio + Vector3.left;
            Vector2 conStick = regla.Avanzar(aqui, Vector3.right, Vector3.up, new Vector2(0f, -1f), radio);
            Assert.AreEqual(new Vector2(0f, -radio), conStick, "El stick abajo pone el cursor abajo.");
            Vector2 soltado = regla.Avanzar(aqui, Vector3.right, Vector3.up, Vector2.zero, radio);
            Assert.AreEqual(conStick, soltado, "Al soltar el stick el cursor se queda donde estaba.");
            Assert.Less(regla.Avanzar(aqui, Vector3.right, Vector3.up, new Vector2(0.1f, 0.1f), radio).y, -radio * 0.9f, "Un stick casi suelto no mueve.");
        }

        [UnityTest]
        public IEnumerator GripIzquierdo_AgarraConElImplante()
        {
            AgarreImplanteVR.ForzarImplante = true;
            yield return ArmarVR();
            AgarreImplanteVR agarre = jugador.GetComponent<AgarreImplanteVR>();
            Assert.IsNotNull(agarre);
            GameObject molde = AssetDatabase.LoadAssetAtPath<GameObject>(RutaSkull);
            Assert.IsNotNull(molde, "Falta el prefab " + RutaSkull);
            skull = Object.Instantiate(molde, new Vector3(-0.2f, 0.05f, 0.75f), Quaternion.Euler(0f, 180f, 0f)).GetComponent<PandilleroAPie>();
            PonerMano(false, new Vector3(-0.2f, 1.2f, 0.3f), Quaternion.identity);
            yield return Cuadros(4);
            Mantener(controlIzq.gripButton);
            yield return Cuadros(2);
            CuerpoAgarradoVR cuerpo = agarre.Sostenido(false);
            Assert.IsNotNull(cuerpo, "El grip izquierdo agarra al SKULL con el implante.");
            Assert.AreEqual(skull.gameObject, cuerpo.gameObject);
            Assert.IsNull(agarre.Sostenido(true), "La derecha no sostiene nada.");
            Soltar(controlIzq.gripButton);
            yield return Cuadros(2);
            Assert.IsNull(agarre.Sostenido(false), "Al soltar el grip izquierdo lo suelta.");
        }

        [UnityTest]
        public IEnumerator StickDerecho_AMediasGiraMasLentoQueAFondo_SinSaltos()
        {
            Assert.AreEqual(ModoGiroVR.Suave, OpcionesVR.Giro, "VR18: el giro es SUAVE por defecto.");
            OpcionesVR.FijarVineta(false);
            yield return ArmarVR();
            float medias = 0f;
            float maximoCuadroMedias = 0f;
            yield return Girar(0.5f, 0.5f, v => medias = v, m => maximoCuadroMedias = m);
            float fondo = 0f;
            float maximoCuadroFondo = 0f;
            yield return Girar(1f, 0.5f, v => fondo = v, m => maximoCuadroFondo = m);
            Debug.Log("[VR] Giro suave: a medias " + medias.ToString("0") + "°/s, a fondo " + fondo.ToString("0") + "°/s; exceso máximo por cuadro "
                + maximoCuadroMedias.ToString("0.00") + "° / " + maximoCuadroFondo.ToString("0.00") + "°.");
            Assert.Greater(medias, 10f, "A medias sí gira.");
            Assert.Less(medias, fondo * 0.7f, "A medias gira más lento que a fondo.");
            Assert.AreEqual(OpcionesVR.VelocidadGiroPorDefecto, fondo, 25f, "A fondo gira a ~120°/s.");
            Assert.Less(maximoCuadroFondo, 1f, "Sin saltos: ningún cuadro gira más que la velocidad máxima.");
            Assert.Less(maximoCuadroMedias, 1f);
        }

        // Mantiene el stick derecho en x = fuerza unos segundos; devuelve la velocidad media (°/s) y el mayor exceso de un cuadro sobre la máxima (°).
        private IEnumerator Girar(float fuerza, float segundos, System.Action<float> velocidad, System.Action<float> maximoCuadro)
        {
            Stick(true, new Vector2(fuerza, 0f));
            yield return Cuadros(3);
            float inicio = adaptador.GiroRig;
            float previo = inicio;
            float acumulado = 0f;
            float maximo = 0f;
            float t = 0f;
            while (t < segundos)
            {
                yield return null;
                t += Time.deltaTime;
                float paso = Mathf.Abs(Mathf.DeltaAngle(previo, adaptador.GiroRig));
                acumulado += paso;
                // Lo que giró de más sobre lo que da la velocidad máxima en este cuadro (un paso de 45° saldría aquí).
                maximo = Mathf.Max(maximo, paso - OpcionesVR.VelocidadGiro * Time.deltaTime);
                previo = adaptador.GiroRig;
            }
            Stick(true, Vector2.zero);
            yield return Cuadros(3);
            velocidad(acumulado / Mathf.Max(t, 1e-3f));
            maximoCuadro(maximo);
        }

        [Test]
        public void AimRelativaAGrip_EsLaPoseLocalDelApuntado()
        {
            Quaternion grip = Quaternion.Euler(10f, 70f, 0f);
            Vector3 pGrip = new Vector3(0.3f, 1.1f, 0.4f);
            Quaternion aim = grip * Quaternion.Euler(-40f, 0f, 0f);
            Vector3 pAim = pGrip + grip * new Vector3(0f, 0.01f, 0.06f);
            Pose local = ManoVR.AimRelativaAGrip(pGrip, grip, pAim, aim);
            Assert.Less(Vector3.Distance(new Vector3(0f, 0.01f, 0.06f), local.position), 1e-4f);
            Assert.Less(Quaternion.Angle(Quaternion.Euler(-40f, 0f, 0f), local.rotation), 0.01f);
        }

        [UnityTest]
        public IEnumerator ElLaserYElCanonSalenDelApuntado()
        {
            // El simulador no da la pose aim: se finge una (6 cm delante y 35° hacia abajo de la grip) para ver que el rayo la usa.
            Quaternion giroAim = Quaternion.Euler(35f, 0f, 0f);
            Vector3 puntaAim = new Vector3(0f, -0.01f, 0.06f);
            ManoVR.ApuntadoSinPose = new Pose(puntaAim, giroAim);
            yield return ArmarVR();
            yield return Cuadros(3);
            ManoVR mano = adaptador.ManoDerecha;
            Transform apuntado = mano.Apuntado;
            Assert.IsNotNull(apuntado);
            Assert.AreEqual(ManoVR.NombreApuntado, apuntado.name);
            Assert.AreSame(mano.transform, apuntado.parent, "Apuntado es hijo de la mano.");
            Assert.IsFalse(mano.TienePoseAim, "El simulador no da la pose aim.");
            Assert.Less(Vector3.Distance(mano.transform.TransformPoint(puntaAim), apuntado.position), 1e-3f, "Apuntado está en la punta.");
            Assert.AreEqual(35f, Vector3.Angle(mano.transform.forward, apuntado.forward), 0.5f, "Apuntado apunta con la pose aim, no con la grip.");

            Assert.IsNotNull(MenusVR.Actual, "MenusVR se crea solo con VR activo.");
            Assert.AreSame(apuntado, MenusVR.Actual.ManoLaser, "El láser sale de ManoVR.Apuntado.");

            PistolaVR pistolaVR = jugador.GetComponent<PistolaVR>();
            Assert.IsNotNull(pistolaVR);
            ArmasAPie armas = jugador.GetComponent<ArmasAPie>();
            if (armas.ArmaActiva != ArmaAPie.Pistola)
            {
                Assert.IsTrue(armas.Equipar(ArmaAPie.Pistola));
            }
            yield return Cuadros(3);
            Assert.IsNotNull(pistolaVR.Boca);
            Assert.Less(Vector3.Angle(apuntado.forward, pistolaVR.Boca.forward), 1f, "El cañón apunta como el láser (pose aim).");
        }
    }
}
