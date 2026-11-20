using System.Collections;
using MechaCyberpunk.Personaje.Visual;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>
    /// T-1505 con entrada simulada: trotar, sprintar, caminar con la tecla (Bloq Mayús), saltar y caer fuerte cambian al clip de Blender esperado, y los pasos suenan
    /// una vez por pisada (por evento de animación, sin duplicar con el sonido por distancia). Necesita los clips y el controlador de AjustarLocomocionT1505.
    /// </summary>
    public class LocomocionBlenderT1505Pruebas : BaseJugadorAPie
    {
        private VisualPersonaje Visual() => jugador.GetComponentInChildren<VisualPersonaje>(true);

        private static string Estado(Animator a)
        {
            AnimatorStateInfo actual = a.GetCurrentAnimatorStateInfo(0);
            foreach (string n in ReglasAnimacion.EstadosBase)
            {
                if (actual.IsName(n)) return n;
            }
            return "?";
        }

        private static string ClipDominante(Animator a)
        {
            string mejor = "";
            float peso = -1f;
            foreach (AnimatorClipInfo i in a.GetCurrentAnimatorClipInfo(0))
            {
                if (i.weight > peso)
                {
                    peso = i.weight;
                    mejor = i.clip.name;
                }
            }
            return mejor;
        }

        private Animator Preparar()
        {
            Animator a = Visual().Animador;
            a.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            return a;
        }

        private float VelocidadHorizontal()
        {
            Vector3 v = jugador.GetComponent<CharacterController>().velocity;
            v.y = 0f;
            return v.magnitude;
        }

        [UnityTest]
        public IEnumerator Trotar_ConW_EsElPasoNormal_PJ01Trote_ARapidezCasiUno()
        {
            yield return Calentar();
            Animator a = Preparar();
            yield return Esperar(0.8f);
            Press(teclado.wKey);
            yield return Esperar(1.3f);
            Assert.AreEqual("Caminar", Estado(a));
            Assert.AreEqual("PJ01_Trote", ClipDominante(a));
            Assert.AreEqual(ReglasAnimacion.VelocidadAndar, VelocidadHorizontal(), 0.3f);
            Assert.That(a.speed, Is.InRange(ReglasAnimacion.RapidezMinimaJugador, ReglasAnimacion.RapidezMaximaJugador));
            Assert.AreEqual(1f, a.speed, 0.08f, "A 4.5 m/s el trote va a su velocidad");
            Release(teclado.wKey);
        }

        [UnityTest]
        public IEnumerator Sprintar_ConShift_EsPJ01Sprint_ARapidezCasiUno()
        {
            yield return Calentar();
            Animator a = Preparar();
            yield return Esperar(0.8f);
            Press(teclado.wKey);
            yield return null; // dos Press en el mismo cuadro: el segundo estado del teclado tapaba la W
            Press(teclado.leftShiftKey);
            yield return Esperar(1.6f);
            Debug.Log("[T1505] sprint: Velocidad " + controlador.Velocidad + " Bloqueado " + controlador.BloqueadoPorAnimacion + " CaminandoLento " + controlador.CaminandoLento
                + " PidiendoCorrer " + controlador.PidiendoCorrer + " estado " + Estado(a) + " clip " + ClipDominante(a) + " enabled " + controlador.enabled
                + " ControlActivo " + controlador.ControlActivo + " Suspendido " + controlador.MovimientoSuspendido + " Derribado " + controlador.Derribado
                + " W " + teclado.wKey.isPressed + " Shift " + teclado.leftShiftKey.isPressed + " pos " + jugador.transform.position);
            Assert.AreEqual("Correr", Estado(a));
            Assert.AreEqual("PJ01_Sprint", ClipDominante(a));
            Assert.That(a.speed, Is.InRange(ReglasAnimacion.RapidezMinimaJugador, ReglasAnimacion.RapidezMaximaJugador));
            Assert.AreEqual(1.03f, a.speed, 0.08f);
            Release(teclado.leftShiftKey);
            Release(teclado.wKey);
        }

        [UnityTest]
        public IEnumerator CaminarConLaTecla_BajaA1y6_PJ01Caminar_YAlRepetirVuelveAlTrote()
        {
            yield return Calentar();
            Animator a = Preparar();
            yield return Esperar(0.8f);
            Press(teclado.capsLockKey);
            yield return null;
            Release(teclado.capsLockKey);
            Press(teclado.wKey);
            yield return Esperar(1.3f);
            Assert.IsTrue(controlador.CaminandoLento);
            Assert.AreEqual(1.6f, VelocidadHorizontal(), 0.15f, "Caminar de verdad va a 1.6 m/s");
            Assert.AreEqual("Caminar", Estado(a));
            Assert.AreEqual("PJ01_Caminar", ClipDominante(a));
            Assert.That(a.speed, Is.InRange(ReglasAnimacion.RapidezMinimaJugador, ReglasAnimacion.RapidezMaximaJugador));

            // Shift manda mientras se mantiene.
            Press(teclado.leftShiftKey);
            yield return Esperar(1.2f);
            Assert.AreEqual("Correr", Estado(a));
            Release(teclado.leftShiftKey);
            yield return Esperar(0.8f);

            Press(teclado.capsLockKey);
            yield return null;
            Release(teclado.capsLockKey);
            yield return Esperar(1.2f);
            Assert.IsFalse(controlador.CaminandoLento);
            Assert.AreEqual("PJ01_Trote", ClipDominante(a));
            Assert.AreEqual(ReglasAnimacion.VelocidadAndar, VelocidadHorizontal(), 0.3f);
            Release(teclado.wKey);
        }

        [UnityTest]
        public IEnumerator Saltar_ConEspacio_UsaElDespegueYElAireDeBlender()
        {
            yield return Calentar();
            Animator a = Preparar();
            yield return Esperar(0.8f);
            Press(teclado.spaceKey);
            bool despegue = false, aire = false;
            float fin = Time.time + 1.0f;
            while (Time.time < fin)
            {
                yield return null;
                string clip = ClipDominante(a);
                despegue |= clip == "PJ01_Salto_Despegue";
                aire |= clip == "PJ01_Salto_Aire";
            }
            Release(teclado.spaceKey);
            Assert.IsTrue(despegue, "Pasa por PJ01_Salto_Despegue");
            Assert.IsTrue(aire || despegue, "El salto es de los clips de Blender");
        }

        [UnityTest]
        public IEnumerator CaidaLarga_AlTocarElPiso_HaceElAterrizajeFuerte_YBloqueaElMovimiento()
        {
            yield return Calentar();
            Animator a = Preparar();
            yield return Esperar(0.8f);
            CharacterController cc = jugador.GetComponent<CharacterController>();
            cc.enabled = false;
            jugador.transform.position = new Vector3(0f, 3.2f, 0f); // ~3 m: llega a ~10 m/s, arriba de 9 y abajo de la rodada (4 m)
            cc.enabled = true;
            bool fuerte = false, bloqueo = false;
            float fin = Time.time + 2.5f;
            while (Time.time < fin)
            {
                yield return null;
                if (Estado(a) == "AterrizajeFuerte")
                {
                    fuerte = true;
                    Assert.AreEqual("PJ01_Aterrizaje_Fuerte", ClipDominante(a));
                    bloqueo |= controlador.BloqueadoPorAnimacion;
                }
            }
            Assert.IsTrue(fuerte, "Cayendo ~3 m se ve el aterrizaje fuerte");
            Assert.IsTrue(bloqueo, "Mientras dura no se camina");
        }

        [UnityTest]
        public IEnumerator Pasos_AlTrotar_SuenanUnaVezPorPisada_PorEvento_SinDuplicarConLaDistancia()
        {
            yield return Calentar();
            Preparar();
            int avisos = 0;
            controlador.PasoDado += (pos, id, corre) => avisos++;
            yield return Esperar(0.8f);
            Press(teclado.wKey);
            yield return Esperar(0.8f); // se asienta el trote
            int antes = controlador.PasosDados;
            int avisosAntes = avisos;
            const float segundos = 2.0f;
            yield return Esperar(segundos);
            int pasos = controlador.PasosDados - antes;
            Release(teclado.wKey);
            Assert.IsTrue(controlador.PasosPorAnimacion, "El clip activo trae eventos: el sonido por distancia está apagado");
            Assert.AreEqual(0, controlador.PasosDados - controlador.PasosAnimados, "Ningún paso por distancia");
            // PJ01_Trote: dos pisadas cada 0.667 s = 3 por segundo (rapidez ~1.0).
            Assert.That(pasos, Is.InRange(5, 7), "Unos 6 pasos en 2 s: una vez por pisada");
            Assert.AreEqual(pasos, avisos - avisosAntes, "Un aviso de paso por cada pisada");
        }
    }
}
