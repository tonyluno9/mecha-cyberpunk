using System;
using System.Collections;
using MechaCyberpunk.UI.Inicio;
using MechaCyberpunk.UI.Intro;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.UI
{
    /// <summary>T-1646: la intro del video del logo. Sin video se salta; con la intro simulada se salta con Enter, clic y Escape, y el menú arranca después.</summary>
    public class IntroInicioT1646Pruebas : InputTestFixture
    {
        private Keyboard teclado;
        private Mouse raton;

        public override void Setup()
        {
            base.Setup();
            teclado = InputSystem.AddDevice<Keyboard>();
            raton = InputSystem.AddDevice<Mouse>();
            IntroInicio.ReproducirEnEditor = true;
        }

        public override void TearDown()
        {
            IntroInicio.ReproducirEnEditor = false;
            IntroInicio.SimularSinVideo = false;
            base.TearDown();
        }

        private static IEnumerator EsperarHasta(Func<bool> condicion, string que, float segundos)
        {
            float fin = Time.realtimeSinceStartup + segundos;
            while (!condicion())
            {
                Assert.Less(Time.realtimeSinceStartup, fin, "Tiempo agotado esperando: " + que);
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator SinVideo_NoSeReproduce_YTerminaDeInmediato()
        {
            GameObject go = new GameObject("IntroSinVideo");
            IntroInicio intro = go.AddComponent<IntroInicio>();
            Assert.IsFalse(intro.DebeReproducir, "Sin VideoClip no hay intro");
            int avisos = 0;
            intro.Terminada += () => avisos++;
            intro.Iniciar();
            Assert.AreEqual(FaseIntro.Terminada, intro.Fase);
            yield return EsperarHasta(() => avisos > 0, "Terminada sin video", 2f);
            Assert.AreEqual(1, avisos);
            UnityEngine.Object.Destroy(go);
        }

        [UnityTest]
        public IEnumerator Simulada_EnterSalta_YAvisaUnaVez()
        {
            yield return SaltarCon(() => Press(teclado.enterKey), () => Release(teclado.enterKey));
        }

        [UnityTest]
        public IEnumerator Simulada_ClicSalta()
        {
            yield return SaltarCon(() => Press(raton.leftButton), () => Release(raton.leftButton));
        }

        [UnityTest]
        public IEnumerator Simulada_EscapeSalta()
        {
            yield return SaltarCon(() => Press(teclado.escapeKey), () => Release(teclado.escapeKey));
        }

        private IEnumerator SaltarCon(Action pulsar, Action soltar)
        {
            IntroInicio.SimularSinVideo = true;
            GameObject go = new GameObject("IntroSimulada");
            IntroInicio intro = go.AddComponent<IntroInicio>();
            Assert.IsTrue(intro.DebeReproducir);
            int avisos = 0;
            intro.Terminada += () => avisos++;
            intro.Iniciar();
            Assert.AreEqual(FaseIntro.Video, intro.Fase);
            yield return null;
            yield return null;
            Assert.AreEqual(FaseIntro.Video, intro.Fase, "Sin tocar nada sigue la intro");
            Assert.AreEqual(0, avisos);
            pulsar();
            yield return null;
            soltar();
            yield return EsperarHasta(() => avisos > 0, "la intro avisa que terminó", 2f);
            Assert.AreEqual(FaseIntro.Terminada, intro.Fase);
            Assert.IsTrue(intro.Saltada);
            yield return null;
            yield return null;
            Assert.AreEqual(1, avisos, "Avisa una sola vez");
            UnityEngine.Object.Destroy(go);
        }

        [UnityTest]
        public IEnumerator Menu_EsperaALaIntro_YArrancaDespuesDeSaltarla()
        {
            IntroInicio.SimularSinVideo = true;
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(EscenaInicio.Ruta, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            MenuInicio menu = UnityEngine.Object.FindFirstObjectByType<MenuInicio>();
            Assert.IsNotNull(menu);
            Assert.IsNotNull(menu.GetComponent<IntroInicio>(), "Inicio.unity trae IntroInicio (correr IntroVideoT1646.Aplicar)");
            yield return null;
            Assert.IsTrue(menu.IntroEnCurso, "El menú espera a la intro");
            Assert.IsFalse(menu.PrimerBoton.EstaElegido, "Nada elegido durante la intro");
            yield return new WaitForSecondsRealtime(1.2f);
            Assert.IsTrue(menu.IntroEnCurso);
            Assert.IsFalse(menu.PrimerBoton.EstaElegido);
            Press(teclado.enterKey);
            yield return null;
            Release(teclado.enterKey);
            yield return EsperarHasta(() => !menu.IntroEnCurso, "el menú arranca tras la intro", 3f);
            yield return EsperarHasta(() => menu.PrimerBoton.EstaElegido, "el primer botón del menú elegido", 5f);
            Assert.AreEqual(EstadoInicio.Principal, menu.Estado);
        }
    }
}
