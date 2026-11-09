using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.APie;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.Personaje.Movimiento;
using MechaCyberpunk.Personaje.Pruebas;
using MechaCyberpunk.Personaje.Vida;
using MechaCyberpunk.Personaje.Visual;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem.Controls;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>T-673: CTRL agacha y levanta (alterna); agachado camina lento, con SHIFT más rápido y con ESPACIO rueda; de pie no se rueda; la cobertura baja tapa la vista.</summary>
    public class AgacharseT673Pruebas : BaseJugadorAPie
    {
        private CubiertaYRodada Cubierta => jugador.GetComponent<CubiertaYRodada>();
        private CharacterController Cuerpo => jugador.GetComponent<CharacterController>();

        private IEnumerator Pulsar(KeyControl tecla)
        {
            Press(tecla);
            yield return null;
            Release(tecla);
            yield return null;
        }

        private float Rapidez() => new Vector2(Cuerpo.velocity.x, Cuerpo.velocity.z).magnitude;

        private static string EstadoAgachado(Animator a)
        {
            foreach (string n in new[] { "CoberturaEntrar", "CoberturaMantener", "AgachadoCaminar", "AgachadoQuieto", "Agacharse", "Pararse", "Rodar", "Idle", "Caminar", "Correr" })
            {
                if (a.IsInTransition(0) ? a.GetNextAnimatorStateInfo(0).IsName(n) : a.GetCurrentAnimatorStateInfo(0).IsName(n))
                {
                    return n;
                }
            }
            return "?";
        }

        [UnityTest]
        public IEnumerator Ctrl_AgachaYVuelveAPararse_Alternando()
        {
            yield return Calentar();
            float alta = Cuerpo.height;
            Assert.IsFalse(Cubierta.AgachadoLibre);
            yield return Pulsar(teclado.leftCtrlKey);
            Assert.IsTrue(Cubierta.AgachadoLibre, "CTRL agacha");
            yield return Esperar(0.3f);
            Assert.Less(Cuerpo.height, alta - 0.5f, "La cápsula baja al agacharse");
            yield return Pulsar(teclado.leftCtrlKey);
            yield return Esperar(0.3f);
            Assert.IsFalse(Cubierta.AgachadoLibre, "CTRL otra vez se levanta");
            Assert.AreEqual(alta, Cuerpo.height, 0.01f, "La cápsula vuelve a su altura");
        }

        [UnityTest]
        public IEnumerator Agachado_CaminaLento_ConShiftMasRapido_YDePieNormal()
        {
            yield return Calentar();
            Press(teclado.wKey);
            yield return Esperar(1.2f);
            float dePie = Rapidez();
            Release(teclado.wKey);
            yield return Esperar(0.6f);

            yield return Pulsar(teclado.leftCtrlKey);
            Press(teclado.wKey);
            yield return Esperar(1.2f);
            float agachado = Rapidez();
            Press(teclado.leftShiftKey);
            yield return Esperar(1.2f);
            float agachadoRapido = Rapidez();
            Release(teclado.leftShiftKey);
            Release(teclado.wKey);
            Assert.Greater(dePie, 3.5f, "De pie camina normal");
            Assert.Less(agachado, dePie * 0.5f, "Agachado camina lento (" + agachado + " contra " + dePie + ")");
            Assert.Greater(agachado, 0.8f, "Pero camina");
            Assert.Greater(agachadoRapido, agachado * 1.4f, "Con SHIFT camina agachado más rápido (" + agachadoRapido + ")");
            Assert.Less(agachadoRapido, dePie * 1.1f, "Agachado con SHIFT no supera el paso de pie");
        }

        [UnityTest]
        public IEnumerator Espacio_DePieSalta_AgachadoRueda_YSinAgacharseNoHayRodada()
        {
            yield return Calentar();
            yield return Esperar(0.3f);
            Assert.IsFalse(Cubierta.Rodar(), "De pie no se puede rodar");
            float y0 = jugador.transform.position.y;
            Press(teclado.spaceKey);
            yield return null;
            Release(teclado.spaceKey);
            float maxY = y0;
            for (float tt = 0f; tt < 1.0f; tt += Time.deltaTime)
            {
                yield return null;
                maxY = Mathf.Max(maxY, jugador.transform.position.y);
            }
            Assert.Greater(maxY, y0 + 0.3f, "De pie el espacio salta");
            Assert.IsFalse(Cubierta.Rodando, "De pie el espacio no rueda");
            yield return Esperar(1.2f);

            yield return Pulsar(teclado.leftCtrlKey);
            yield return Esperar(0.3f);
            y0 = jugador.transform.position.y;
            Press(teclado.wKey);
            yield return Esperar(0.3f);
            Press(teclado.spaceKey);
            yield return null;
            Release(teclado.spaceKey);
            yield return null;
            Assert.IsTrue(Cubierta.Rodando, "Agachado el espacio rueda");
            maxY = y0;
            for (float tt = 0f; tt < 0.5f; tt += Time.deltaTime)
            {
                yield return null;
                maxY = Mathf.Max(maxY, jugador.transform.position.y);
            }
            Assert.Less(maxY, y0 + 0.15f, "Agachado el espacio no salta");
            Release(teclado.wKey);
            yield return Esperar(Cubierta.Datos.DuracionRodada + 0.4f);
            Assert.IsFalse(Cubierta.Rodando);
            Assert.IsTrue(Cubierta.AgachadoLibre, "Tras rodar sigue agachado");
        }

        [UnityTest]
        public IEnumerator Techo_ImpideLevantarse()
        {
            yield return Calentar();
            yield return Pulsar(teclado.leftCtrlKey);
            yield return Esperar(0.3f);
            Caja(new Vector3(0f, 1.3f, 0f), new Vector3(3f, 0.2f, 3f)); // techo a 1.2 m: se cabe agachado (1.0 m) pero no de pie
            Physics.SyncTransforms();
            yield return Esperar(0.1f);
            yield return Pulsar(teclado.leftCtrlKey);
            Assert.IsTrue(Cubierta.AgachadoLibre, "Con techo encima no se levanta");
        }

        [UnityTest]
        public IEnumerator Agachado_QuietoUsaAgachadoQuieto_YCaminandoAgachadoCaminar()
        {
            yield return Calentar();
            VisualPersonaje visual = jugador.GetComponentInChildren<VisualPersonaje>(true);
            visual.Animador.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            yield return Pulsar(teclado.leftCtrlKey);
            yield return Esperar(1.4f);
            Assert.IsTrue(visual.Agachado, "El visual sabe que está agachado");
            Assert.AreEqual("AgachadoQuieto", EstadoAgachado(visual.Animador), "Quieto agachado usa Loco_AgachadoIdle (T-733)");
            Press(teclado.wKey);
            yield return Esperar(1.0f);
            Assert.AreEqual("AgachadoCaminar", EstadoAgachado(visual.Animador), "Caminando agachado usa AgachadoCaminar");
            Release(teclado.wKey);
        }

        [UnityTest]
        public IEnumerator Agachado_UnaSolaPulsacionDeDisparar_SePoneDePieYHaceDanioReal()
        {
            yield return Calentar();
            Assert.IsTrue(jugador.GetComponent<ArmasAPie>().Equipar(ArmaAPie.Pistola));
            yield return Esperar(0.8f);
            // Un blanco ancho de frente, para que el daño no dependa del pulso de la cámara.
            GameObject blanco = Caja(new Vector3(0f, 2f, 6f), new Vector3(6f, 4f, 1f));
            MunecoAPie muneco = blanco.AddComponent<MunecoAPie>();
            Physics.SyncTransforms();
            yield return Pulsar(teclado.leftCtrlKey);
            yield return Esperar(0.6f);
            Assert.IsTrue(Cubierta.AgachadoLibre, "Empieza agachado");
            float antes = muneco.Actual;
            int balas = jugador.GetComponent<ArmasAPie>().Balas;
            Press(raton.leftButton);
            yield return null;
            Release(raton.leftButton);
            yield return Esperar(0.4f);
            Assert.IsFalse(Cubierta.AgachadoLibre, "Una pulsación: se para");
            Assert.AreEqual(balas - 1, jugador.GetComponent<ArmasAPie>().Balas, "y dispara con esa misma pulsación");
            Assert.Less(muneco.Actual, antes, "el objetivo recibe daño real con una sola pulsación estando agachado");
        }

        [UnityTest]
        public IEnumerator Agachado_DetrasDeCoberturaBaja_LosEnemigosPierdenLaLineaDeVista()
        {
            yield return Calentar();
            // Cobertura baja (1.0 m) a 1 m delante del jugador y un tirador a 10 m con la boca a 1.4 m.
            Caja(new Vector3(0f, 0.5f, 1f), new Vector3(4f, 1.0f, 0.2f));
            GameObject tirador = new GameObject("TiradorPrueba");
            tirador.transform.position = new Vector3(0f, 0f, 11f);
            PistolaPandillero pistola = tirador.AddComponent<PistolaPandillero>();
            IRecibeDanio objetivo = jugador.GetComponent<VidaAPie>();
            Vector3 boca = tirador.transform.position + Vector3.up * 1.4f;
            const float pechoDePie = 1.2f;

            Physics.SyncTransforms();
            float pechoAlto = ReglaPandilleroAPie.AlturaPechoSegunCuerpo(pechoDePie, Cuerpo.height, 1.8f);
            Vector3 hacia = jugador.transform.position + Vector3.up * pechoAlto - boca;
            Assert.IsTrue(pistola.HayLinea(boca, hacia.normalized, hacia.magnitude, objetivo, out _), "De pie el tirador ve el pecho por encima de la cobertura baja");

            yield return Pulsar(teclado.leftCtrlKey);
            yield return Esperar(0.3f);
            Physics.SyncTransforms();
            float pechoBajo = ReglaPandilleroAPie.AlturaPechoSegunCuerpo(pechoDePie, Cuerpo.height, 1.8f);
            Assert.Less(pechoBajo, pechoAlto - 0.3f, "El pecho baja al agacharse");
            hacia = jugador.transform.position + Vector3.up * pechoBajo - boca;
            Assert.IsFalse(pistola.HayLinea(boca, hacia.normalized, hacia.magnitude, objetivo, out _), "Agachado la cobertura baja le tapa la vista");
            Object.Destroy(tirador);
        }
    }
}
