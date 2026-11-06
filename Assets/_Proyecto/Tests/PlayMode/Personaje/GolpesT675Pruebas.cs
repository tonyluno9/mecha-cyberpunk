using System.Collections;
using System.Collections.Generic;
using MechaCyberpunk.Combate.Golpe;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.Personaje.Movimiento;
using MechaCyberpunk.Personaje.Visual;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>T-675: sin spam de golpes (el siguiente espera a que acabe la animación del anterior) y la reacción al daño es corta y se corta con un golpe o un esquive.</summary>
    public class GolpesT675Pruebas : BaseJugadorAPie
    {
        private VisualPersonaje Visual()
        {
            VisualPersonaje v = jugador.GetComponentInChildren<VisualPersonaje>(true);
            v.Animador.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            return v;
        }

        [UnityTest]
        public IEnumerator SpamDeClic_ElSiguienteGolpeEsperaALaAnimacionDelAnterior_YNoSeTraba()
        {
            yield return Calentar();
            VisualPersonaje visual = Visual();
            GolpeAPie golpe = jugador.GetComponent<GolpeAPie>();
            yield return Esperar(0.5f);
            var inicios = new List<float>();
            var finAnimacion = new List<float>();
            FaseGolpe previa = FaseGolpe.Listo;
            float fin = Time.time + 5f;
            while (Time.time < fin)
            {
                Press(raton.leftButton);
                yield return null;
                Release(raton.leftButton);
                yield return null;
                if (golpe.Fase == FaseGolpe.Preparando && previa != FaseGolpe.Preparando)
                {
                    inicios.Add(Time.time);
                    finAnimacion.Add(Time.time);
                }
                previa = golpe.Fase;
                if (visual.GolpeEnCurso && finAnimacion.Count > 0)
                {
                    finAnimacion[finAnimacion.Count - 1] = Time.time;
                }
            }
            Assert.GreaterOrEqual(inicios.Count, 4, "Sigue pegando (no se traba): " + inicios.Count + " golpes en 5 s");
            for (int i = 1; i < inicios.Count; i++)
            {
                Assert.GreaterOrEqual(inicios[i], finAnimacion[i - 1] - 0.001f, "El golpe " + i + " no empieza antes de que termine la animación del anterior");
                Assert.Greater(inicios[i] - inicios[i - 1], 0.35f, "Hay un hueco real entre golpes");
            }
        }

        [UnityTest]
        public IEnumerator ReaccionAlDano_EsCorta_ConUnGolpeSeCorta_YConUnEsquiveSeCorta()
        {
            yield return Calentar();
            VisualPersonaje visual = Visual();
            yield return Esperar(0.5f);

            visual.Herir(-visual.transform.forward);
            yield return Esperar(0.15f);
            Assert.IsTrue(visual.ReaccionEnCurso, "Al recibir un golpe se ve la reacción");
            CapturaS1.Foto("s1n-T-675-reaccion", jugador.transform, new Vector3(2.6f, 1.0f, 0.3f), new Vector3(0f, 0.9f, 0.3f), 45f, 700, 700);
            float t0 = Time.time;
            while (visual.ReaccionEnCurso && Time.time - t0 < 3f) { yield return null; }
            float duracion = Time.time - t0 + 0.15f;
            Assert.Less(duracion, 0.9f, "La reacción es corta (" + duracion + " s)");
            yield return Esperar(0.3f);

            // Cortada por un golpe.
            visual.Herir(-visual.transform.forward);
            yield return Esperar(0.1f);
            Assert.IsTrue(visual.ReaccionEnCurso);
            Press(raton.leftButton);
            yield return null;
            Release(raton.leftButton);
            yield return Esperar(0.25f);
            Assert.IsFalse(visual.ReaccionEnCurso, "Un golpe corta la reacción");
            CapturaS1.Foto("s1n-T-675-golpe-corta-reaccion", jugador.transform, new Vector3(2.6f, 1.0f, 0.3f), new Vector3(0f, 0.9f, 0.3f), 45f, 700, 700);
            Assert.IsTrue(visual.GolpeEnCurso || jugador.GetComponent<GolpeAPie>().Fase != FaseGolpe.Listo, "Y el golpe sale");
            yield return Esperar(1.2f);

            // Cortada por un esquive (agachado + ESPACIO).
            Press(teclado.leftCtrlKey);
            yield return null;
            Release(teclado.leftCtrlKey);
            yield return Esperar(0.4f);
            visual.Herir(-visual.transform.forward, true);
            yield return Esperar(0.1f);
            Assert.IsTrue(visual.ReaccionEnCurso);
            Press(teclado.spaceKey);
            yield return null;
            Release(teclado.spaceKey);
            yield return Esperar(0.15f);
            Assert.IsTrue(jugador.GetComponent<CubiertaYRodada>().Rodando, "El esquive arrancó");
            Assert.IsFalse(visual.ReaccionEnCurso, "Un esquive corta la reacción");
        }

        [UnityTest]
        public IEnumerator HeridasSeguidas_NadieQuedaTrabado_ElTorsoVuelveASuPose()
        {
            yield return Calentar();
            VisualPersonaje visual = Visual();
            yield return Esperar(0.5f);
            for (int i = 0; i < 30; i++)
            {
                visual.Herir(i % 2 == 0 ? visual.transform.forward : -visual.transform.forward, i % 3 == 0);
                yield return Esperar(0.05f);
            }
            yield return Esperar(1.2f);
            Assert.IsFalse(visual.ReaccionEnCurso, "Tras muchas heridas la reacción termina sola");
            Assert.IsFalse(visual.GolpeEnCurso);
            // Y puede pegar de nuevo.
            GolpeAPie golpe = jugador.GetComponent<GolpeAPie>();
            Press(raton.leftButton);
            yield return null;
            Release(raton.leftButton);
            yield return null;
            Assert.AreNotEqual(FaseGolpe.Listo, golpe.Fase, "Puede devolver el golpe");
        }
    }
}
