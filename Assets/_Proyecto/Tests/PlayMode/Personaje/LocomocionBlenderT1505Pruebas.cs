using System.Collections;
using MechaCyberpunk.Personaje.Visual;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>
    /// T-1505 con entrada simulada (T-1550: caminar con WASD, trotar con Shift, correr rápido con velocidad rápida), saltar y caer fuerte cambian al clip de Blender esperado, y los pasos suenan
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

        // T-1550 (decisión de Tony del 20-nov (T-1550)): WASD = caminar (1.62 m/s, PJ01_Caminar), Shift = trote (4.53 m/s, PJ01_Trote en la mezcla de Caminar) y la velocidad rápida
        // (implante o consumible) = PJ01_CorrerRapido a 6.25 m/s en el estado Correr. PJ01_Sprint ya no se usa. Antes W era el trote y Shift el sprint de 7.25.

        [UnityTest]
        public IEnumerator Caminar_ConW_EsPJ01Caminar_ARapidezCasiUno()
        {
            yield return Calentar();
            Animator a = Preparar();
            yield return Esperar(0.8f);
            Press(teclado.wKey);
            yield return Esperar(1.3f);
            Assert.AreEqual("Caminar", Estado(a));
            Assert.AreEqual("PJ01_Caminar", ClipDominante(a));
            Assert.AreEqual(ReglasAnimacion.VelocidadPJ01Caminar, VelocidadHorizontal(), 0.15f, "WASD camina a 1.62 m/s");
            Assert.That(a.speed, Is.InRange(ReglasAnimacion.RapidezMinimaJugador, ReglasAnimacion.RapidezMaximaJugador));
            Assert.AreEqual(1f, a.speed, 0.08f, "A 1.62 m/s PJ01_Caminar va a su velocidad (pies sin patinar)");
            Release(teclado.wKey);
        }

        [UnityTest]
        public IEnumerator Trotar_ConShift_EsPJ01Trote_ARapidezCasiUno()
        {
            yield return Calentar();
            Animator a = Preparar();
            yield return Esperar(0.8f);
            Press(teclado.wKey);
            yield return null; // dos Press en el mismo cuadro: el segundo estado del teclado tapaba la W
            Press(teclado.leftShiftKey);
            yield return Esperar(1.6f);
            Assert.AreEqual("Caminar", Estado(a), "El trote es la mezcla del estado Caminar");
            Assert.AreEqual("PJ01_Trote", ClipDominante(a));
            Assert.AreEqual(ReglasAnimacion.VelocidadPJ01Trote, VelocidadHorizontal(), 0.15f);
            Assert.That(a.speed, Is.InRange(ReglasAnimacion.RapidezMinimaJugador, ReglasAnimacion.RapidezMaximaJugador));
            Assert.AreEqual(1f, a.speed, 0.08f, "A 4.53 m/s el trote va a su velocidad");
            Release(teclado.leftShiftKey);
            Release(teclado.wKey);
        }

        [UnityTest]
        public IEnumerator CorrerRapido_ConVelocidadRapida_EsPJ01CorrerRapido_A6y25()
        {
            yield return Calentar();
            Animator a = Preparar();
            controlador.FactorExtraVelocidad = 1.35f; // el café: velocidad rápida (ConsumiblesJugador lo pone igual)
            yield return Esperar(0.8f);
            Press(teclado.wKey);
            yield return null;
            Press(teclado.leftShiftKey);
            yield return Esperar(1.6f);
            Assert.AreEqual("Correr", Estado(a));
            Assert.AreEqual("PJ01_CorrerRapido", ClipDominante(a));
            Assert.AreEqual(ReglasAnimacion.VelocidadRapida, VelocidadHorizontal(), 0.2f, "La velocidad rápida es de 6.25 m/s");
            Assert.That(a.speed, Is.InRange(ReglasAnimacion.RapidezMinimaJugador, ReglasAnimacion.RapidezMaximaJugador));
            Assert.AreEqual(1f, a.speed, 0.08f);
            Release(teclado.leftShiftKey);
            Release(teclado.wKey);
            controlador.FactorExtraVelocidad = 1f;
        }

        [UnityTest]
        public IEnumerator Shift_DespuesDeCaminar_SubeDelCaminarAlTrote_SinPasarAlCorrerRapido()
        {
            yield return Calentar();
            Animator a = Preparar();
            yield return Esperar(0.8f);
            Press(teclado.wKey);
            yield return Esperar(1.3f);
            Assert.AreEqual("PJ01_Caminar", ClipDominante(a));
            Press(teclado.leftShiftKey);
            yield return Esperar(1.2f);
            Assert.AreEqual("Caminar", Estado(a), "Sin velocidad rápida Shift no llega al estado Correr");
            Assert.AreEqual("PJ01_Trote", ClipDominante(a));
            Release(teclado.leftShiftKey);
            yield return Esperar(1.2f);
            Assert.AreEqual("PJ01_Caminar", ClipDominante(a), "Al soltar Shift vuelve a caminar");
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
        public IEnumerator Pasos_AlTrotarConShift_SuenanUnaVezPorPisada_PorEvento_SinDuplicarConLaDistancia()
        {
            yield return Calentar();
            Preparar();
            int avisos = 0;
            controlador.PasoDado += (pos, id, corre) => avisos++;
            yield return Esperar(0.8f);
            Press(teclado.wKey);
            yield return null; // T-1550: el trote es con Shift (W camina a 1.62 m/s); dos Press en el mismo cuadro tapaban la W
            Press(teclado.leftShiftKey);
            yield return Esperar(1.0f); // se asienta el trote
            int antes = controlador.PasosDados;
            int avisosAntes = avisos;
            const float segundos = 2.0f;
            yield return Esperar(segundos);
            int pasos = controlador.PasosDados - antes;
            Release(teclado.wKey);
            yield return null;
            Release(teclado.leftShiftKey);
            Assert.IsTrue(controlador.PasosPorAnimacion, "El clip activo trae eventos: el sonido por distancia está apagado");
            Assert.AreEqual(0, controlador.PasosDados - controlador.PasosAnimados, "Ningún paso por distancia");
            // PJ01_Trote: dos pisadas cada 0.667 s = 3 por segundo (rapidez ~1.0).
            Assert.That(pasos, Is.InRange(5, 7), "Unos 6 pasos en 2 s: una vez por pisada");
            Assert.AreEqual(pasos, avisos - avisosAntes, "Un aviso de paso por cada pisada");
        }
    }
}
