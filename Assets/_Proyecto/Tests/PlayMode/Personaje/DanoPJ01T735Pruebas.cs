using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.Personaje.Movimiento;
using MechaCyberpunk.Personaje.Vida;
using MechaCyberpunk.Personaje.Visual;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>
    /// T-735 (A5): el jugador se estremece con Pistola_Dano_Frente, Espalda o Fuerte en una capa aditiva y sigue apuntando y disparando.
    /// Direccion y umbral de daño fuerte (25 % de la vida) por el evento de impacto real. Capturas en docs/capturas/pj01-dano-*.png.
    /// </summary>
    public class DanoPJ01T735Pruebas : BaseJugadorAPie
    {
        private VisualPersonaje visual;
        private Animator anim;
        private VidaAPie vida;

        private IEnumerator Preparar(bool pistola)
        {
            yield return Calentar();
            visual = jugador.GetComponentInChildren<VisualPersonaje>(true);
            anim = visual.Animador;
            anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            vida = jugador.GetComponent<VidaAPie>();
            if (pistola)
            {
                Assert.IsTrue(jugador.GetComponent<ArmasAPie>().Equipar(ArmaAPie.Pistola));
            }
            yield return Esperar(0.8f);
        }

        private static string Estado(Animator a, int capa)
        {
            AnimatorStateInfo info = a.IsInTransition(capa) ? a.GetNextAnimatorStateInfo(capa) : a.GetCurrentAnimatorStateInfo(capa);
            foreach (string n in new[] { "Vacio", "DanoFrente", "DanoEspalda", "DanoFuerte", "PistolaApuntar", "PistolaAgachado", "PistolaDisparo", "PistolaSacar", "PistolaIdle" })
            {
                if (info.IsName(n)) { return n; }
            }
            return "?";
        }

        // Un impacto como el que publica el combate: el jugador lo escucha (AnimacionJugador). De frente = el golpe viaja contra su frente.
        private void Impacto(bool deFrente, float fraccionDeVida)
        {
            Vector3 direccion = deFrente ? -jugador.transform.forward : jugador.transform.forward;
            var info = new InfoDanio(vida.Maxima * fraccionDeVida, TipoDanio.Disparo, jugador.transform.position + Vector3.up, direccion, null);
            EventosPelea.PublicarImpacto(null, info, new ResultadoDanio(info.Cantidad, TipoBloqueo.Ninguno, false));
        }

        private IEnumerator HastaDano(string estado, float tope)
        {
            float t0 = Time.time;
            while (Estado(anim, 2) != estado && Time.time - t0 < tope)
            {
                yield return null;
            }
        }

        private void Foto(string nombre)
        {
            CapturaS1.Foto("pj01-dano-" + nombre, jugador.transform, new Vector3(3.2f, 1.1f, 1.2f), new Vector3(0f, 0.9f, 0.2f), 40f, 700, 700);
        }

        [UnityTest]
        public IEnumerator Direccion_DeFrenteFrente_DeEspaldaEspalda_PorElImpactoReal()
        {
            yield return Preparar(false);
            Impacto(true, 0.05f);
            yield return HastaDano("DanoFrente", 0.3f);
            Assert.AreEqual("DanoFrente", Estado(anim, 2), "Un golpe que viaja contra su frente es de frente");
            yield return Esperar(0.12f);
            Foto("frente");
            yield return Esperar(0.7f);
            Assert.AreEqual("Vacio", Estado(anim, 2), "Termina y la capa vuelve a vacio");

            Impacto(false, 0.05f);
            yield return HastaDano("DanoEspalda", 0.3f);
            Assert.AreEqual("DanoEspalda", Estado(anim, 2), "Un golpe que viaja con su frente le pega por la espalda");
            yield return Esperar(0.12f);
            Foto("espalda");
            yield return Esperar(0.7f);
            Assert.AreEqual("Vacio", Estado(anim, 2));
        }

        [UnityTest]
        public IEnumerator Umbral_UnaCuartaParteDeLaVidaOMas_EsDanoFuerte()
        {
            yield return Preparar(false);
            Impacto(true, 0.24f);
            yield return HastaDano("DanoFrente", 0.3f);
            Assert.AreEqual("DanoFrente", Estado(anim, 2), "Con 24 % de la vida aun es normal");
            yield return Esperar(0.9f);
            Impacto(true, 0.25f);
            yield return HastaDano("DanoFuerte", 0.3f);
            Assert.AreEqual("DanoFuerte", Estado(anim, 2), "Con 25 % o mas es fuerte");
            yield return Esperar(0.12f);
            Foto("fuerte");
            yield return Esperar(0.9f);
            Impacto(false, 0.6f);
            yield return HastaDano("DanoFuerte", 0.3f);
            Assert.AreEqual("DanoFuerte", Estado(anim, 2), "Fuerte manda sobre la direccion (tambien de espalda)");
        }

        [UnityTest]
        public IEnumerator ApuntandoYDisparando_ElEstremecimientoNoQuitaLaPose_ElJugadorSigueDisparando()
        {
            yield return Preparar(true);
            ArmasAPie armas = jugador.GetComponent<ArmasAPie>();
            Press(raton.rightButton);
            yield return Esperar(1.4f);
            Assert.AreEqual("PistolaApuntar", Estado(anim, 1));
            float pesoArriba = visual.PesoCapaArriba;

            Impacto(true, 0.1f);
            yield return HastaDano("DanoFrente", 0.3f);
            yield return Esperar(0.1f);
            Assert.AreEqual("DanoFrente", Estado(anim, 2));
            Assert.AreEqual("PistolaApuntar", Estado(anim, 1), "Mientras se estremece sigue en la pose de apuntar (no la reemplaza)");
            Assert.AreEqual(pesoArriba, visual.PesoCapaArriba, 0.05f);
            Assert.IsTrue(visual.ArmaEnMano, "La pistola sigue en la mano");
            Foto("apuntando");

            int antes = armas.Balas;
            Press(raton.leftButton);
            yield return null;
            Release(raton.leftButton);
            yield return Esperar(0.1f);
            Assert.AreEqual(antes - 1, armas.Balas, "Dispara mientras se estremece");
            Assert.IsTrue(Estado(anim, 1) == "PistolaDisparo" || Estado(anim, 1) == "PistolaApuntar", "El disparo suena en la capa de arriba: " + Estado(anim, 1));

            Release(raton.rightButton);
        }

        [UnityTest]
        public IEnumerator NoHayDosReacciones_UnGolpeNuevoReemplazaAlAnterior_YSiempreVuelveAVacio()
        {
            yield return Preparar(false);
            for (int i = 0; i < 12; i++)
            {
                Impacto(i % 2 == 0, i % 3 == 0 ? 0.3f : 0.05f);
                yield return Esperar(0.04f);
                // Una sola reaccion a la vez: la capa de dano tiene un solo estado de dano; la de arriba no juega ninguno.
                Assert.IsFalse(Estado(anim, 1).StartsWith("Dano"));
            }
            yield return Esperar(1.2f);
            Assert.AreEqual("Vacio", Estado(anim, 2), "Tras muchos golpes nada queda trabado");
            Assert.IsFalse(visual.ReaccionEnCurso);
        }

        [UnityTest]
        public IEnumerator UnGolpeDePunosCortaLaReaccion_ElEsquiveTambien_T675()
        {
            yield return Preparar(false);
            Impacto(true, 0.05f);
            yield return Esperar(0.1f);
            Assert.IsTrue(visual.ReaccionEnCurso);
            visual.Golpear(false);
            yield return Esperar(0.25f);
            Assert.IsFalse(visual.ReaccionEnCurso, "Pegar corta el estremecimiento: no se encima con el golpe");
            yield return Esperar(1.2f);
            Press(teclado.leftCtrlKey);
            yield return null;
            Release(teclado.leftCtrlKey);
            yield return Esperar(0.5f);
            Impacto(true, 0.3f);
            yield return Esperar(0.1f);
            Assert.IsTrue(visual.ReaccionEnCurso);
            Press(teclado.spaceKey);
            yield return null;
            Release(teclado.spaceKey);
            yield return Esperar(0.2f);
            Assert.IsTrue(jugador.GetComponent<CubiertaYRodada>().Rodando);
            Assert.IsFalse(visual.ReaccionEnCurso, "Rodar corta el estremecimiento");
        }

        [UnityTest]
        public IEnumerator Caido_LaCapaDeDanoSeApaga_ParaNoMoverElCuerpoTumbado()
        {
            yield return Preparar(false);
            Assert.AreEqual(1f, visual.PesoCapaDano, 0.05f);
            visual.Caido = true;
            yield return Esperar(0.6f);
            Assert.AreEqual(0f, visual.PesoCapaDano, 0.05f);
            visual.Caido = false;
            yield return Esperar(6f);
            Assert.AreEqual(1f, visual.PesoCapaDano, 0.05f, "Al levantarse vuelve");
        }

        [UnityTest]
        public IEnumerator AliadosYEnemigos_ConElMismoControlador_SeEstremecenSinPerderLaPoseDeApuntar()
        {
            yield return Calentar();
            string[] visuales = { "Visual_woman2", "Visual_man", "Visual_SkullHombre", "Visual_GuardiaNova", "Visual_Doctor", "Visual_RobotHombre" };
            string fallos = "";
            for (int i = 0; i < visuales.Length; i++)
            {
                GameObject go = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Proyecto/Personaje/Visual/Prefabs/" + visuales[i] + ".prefab"),
                    new Vector3(8f + i * 2f, 0.05f, 6f), Quaternion.identity);
                VisualPersonaje v = go.GetComponent<VisualPersonaje>();
                Animator a = v.Animador;
                a.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                v.LeerDelPadre = false;
                v.TipoArma = TipoArmaVisual.Pistola;
                v.Apuntando = true;
                yield return Esperar(1.4f);
                v.Herir(-v.transform.forward);
                float t0 = Time.time;
                while (Estado(a, 2) != "DanoFrente" && Time.time - t0 < 0.4f) { yield return null; }
                if (Estado(a, 2) != "DanoFrente") { fallos += visuales[i] + " sin dano; "; }
                if (Estado(a, 1) != "PistolaApuntar") { fallos += visuales[i] + " perdio la pose=" + Estado(a, 1) + "; "; }
                yield return Esperar(0.8f);
                if (Estado(a, 2) != "Vacio") { fallos += visuales[i] + " trabado=" + Estado(a, 2) + "; "; }
                Object.Destroy(go);
            }
            Assert.AreEqual("", fallos);
        }
    }
}
