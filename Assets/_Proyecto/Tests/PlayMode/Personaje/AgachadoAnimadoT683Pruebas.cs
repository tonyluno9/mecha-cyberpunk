using System.Collections;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.Personaje.Movimiento;
using MechaCyberpunk.Personaje.Visual;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem.Controls;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>
    /// T-683: agachado con animación real (AgachadoQuieto y AgachadoCaminar), sin pose de arma si no se apunta, con los pies más rápidos con Shift,
    /// y la rodada lenta, sin hundirse en el piso y terminando agachado. Capturas con CAPTURAR_S1=1 en docs/capturas/s1t-T-683-*.png.
    /// </summary>
    public class AgachadoAnimadoT683Pruebas : BaseJugadorAPie
    {
        private IEnumerator Pulsar(KeyControl tecla)
        {
            Press(tecla);
            yield return null;
            Release(tecla);
            yield return null;
        }

        private Animator Anim(out VisualPersonaje visual)
        {
            visual = jugador.GetComponentInChildren<VisualPersonaje>(true);
            visual.Animador.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            return visual.Animador;
        }

        private static string Estado(Animator a, int capa)
        {
            foreach (string n in new[] { "Idle", "Caminar", "Correr", "AgachadoQuieto", "AgachadoCaminar", "PistolaIdle", "PistolaApuntar", "LargaIdle", "LargaApuntar", "Vacio", "BrazosSueltos" })
            {
                if (a.IsInTransition(capa) ? a.GetNextAnimatorStateInfo(capa).IsName(n) : a.GetCurrentAnimatorStateInfo(capa).IsName(n))
                {
                    return n;
                }
            }
            return "?";
        }

        private float Alto(Animator a, HumanBodyBones hueso) => a.GetBoneTransform(hueso).position.y - jugador.transform.position.y;

        // El punto más bajo de los huesos principales respecto del piso (m); negativo = se hunde.
        private float PuntoMasBajo(Animator a)
        {
            float minimo = float.MaxValue;
            foreach (HumanBodyBones h in new[] { HumanBodyBones.Head, HumanBodyBones.Hips, HumanBodyBones.LeftFoot, HumanBodyBones.RightFoot, HumanBodyBones.LeftHand, HumanBodyBones.RightHand, HumanBodyBones.Chest })
            {
                minimo = Mathf.Min(minimo, Alto(a, h));
            }
            return minimo;
        }

        private void Fotos(string nombre)
        {
            CapturaS1.Foto("s1t-T-683-" + nombre, jugador.transform, new Vector3(3.0f, 1.0f, 0.3f), new Vector3(0f, 0.7f, 0.3f), 40f, 700, 700);
        }

        // Cuántos ciclos por segundo avanza el clip de la capa base durante los segundos dados.
        private IEnumerator CiclosPorSegundo(Animator a, float segundos, System.Action<float> resultado)
        {
            float t0 = Time.time;
            float previo = a.GetCurrentAnimatorStateInfo(0).normalizedTime;
            float ciclos = 0f;
            while (Time.time - t0 < segundos)
            {
                yield return null;
                float actual = a.GetCurrentAnimatorStateInfo(0).normalizedTime;
                float d = actual - previo;
                if (d < 0f) { d = 0f; }
                ciclos += d;
                previo = actual;
            }
            resultado(ciclos / Mathf.Max(0.01f, Time.time - t0));
        }

        [UnityTest]
        public IEnumerator AgachadoQuieto_UsaElClipDeAgachado_SinPoseDeArma_BrazosSueltos()
        {
            yield return Calentar();
            Animator a = Anim(out VisualPersonaje visual);
            yield return Esperar(0.6f);
            float cabezaDePie = Alto(a, HumanBodyBones.Head);
            yield return Pulsar(teclado.leftCtrlKey);
            yield return Esperar(1.2f);
            Fotos("agachado-quieto");
            Assert.AreEqual("AgachadoQuieto", Estado(a, 0), "Quieto agachado juega el clip de agachado");
            Assert.AreEqual(1f, visual.PesoCapaBrazos, 0.05f, "Los brazos quedan sueltos (capa Brazos)");
            Assert.AreEqual(0f, visual.PesoCapaArriba, 0.05f, "Sin pose de arma");
            Assert.AreEqual(TipoArmaVisual.Ninguna, visual.TipoArmaAnimador);
            Assert.Less(Alto(a, HumanBodyBones.Head), cabezaDePie - 0.1f, "La cabeza baja (" + Alto(a, HumanBodyBones.Head) + " contra " + cabezaDePie + ")");
            Assert.Greater(Mathf.Min(Alto(a, HumanBodyBones.LeftFoot), Alto(a, HumanBodyBones.RightFoot)), -0.05f, "Los pies no se hunden");
            // Brazos sueltos: las manos cuelgan por debajo del pecho (con la pose de rifle estarían al frente, a la altura del pecho).
            float pecho = Alto(a, HumanBodyBones.Chest);
            Assert.Less(Alto(a, HumanBodyBones.LeftHand), pecho - 0.12f, "La mano izquierda cuelga (mano " + Alto(a, HumanBodyBones.LeftHand) + ", pecho " + pecho + ")");
            Assert.Less(Alto(a, HumanBodyBones.RightHand), pecho - 0.12f, "La mano derecha cuelga (mano " + Alto(a, HumanBodyBones.RightHand) + ", pecho " + pecho + ")");
            yield return Pulsar(teclado.leftCtrlKey);
            yield return Esperar(1.0f);
            Assert.AreEqual(0f, visual.PesoCapaBrazos, 0.05f, "De pie la capa de brazos se apaga");
            Assert.AreEqual(cabezaDePie, Alto(a, HumanBodyBones.Head), 0.1f, "Vuelve a su altura");
        }

        [UnityTest]
        public IEnumerator AgachadoCaminando_ConShiftLosPiesVanMasRapido_SinPoseDeArma()
        {
            yield return Calentar();
            Animator a = Anim(out VisualPersonaje visual);
            Assert.IsTrue(jugador.GetComponent<ArmasAPie>().Equipar(ArmaAPie.Pistola));
            yield return Pulsar(teclado.leftCtrlKey);
            Press(teclado.wKey);
            yield return Esperar(1.0f);
            Assert.AreEqual("AgachadoCaminar", Estado(a, 0));
            Assert.AreEqual(1f, visual.PesoCapaBrazos, 0.05f, "Con el arma en la mano y sin apuntar, los brazos también quedan sueltos");
            Assert.AreEqual(0f, visual.PesoCapaArriba, 0.05f, "Sin apuntar no hay pose de arma");
            Assert.AreNotEqual("PistolaIdle", Estado(a, 1));
            float lento = 0f;
            yield return CiclosPorSegundo(a, 1.0f, v => lento = v);
            Fotos("agachado-caminando");
            Press(teclado.leftShiftKey);
            yield return Esperar(1.0f);
            float rapido = 0f;
            yield return CiclosPorSegundo(a, 1.0f, v => rapido = v);
            Fotos("agachado-caminando-shift");
            Release(teclado.leftShiftKey);
            Release(teclado.wKey);
            Assert.Greater(lento, 0.3f, "Caminando agachado el clip avanza (ciclos/s: " + lento + ")");
            Assert.Greater(rapido, lento * 1.2f, "Con Shift los pies se mueven más rápido: " + rapido + " ciclos/s contra " + lento);
        }

        [UnityTest]
        public IEnumerator AgachadoApuntando_UsaLaPoseDeArma_YElCanonVaAlPuntoDeMira()
        {
            Caja(new Vector3(0f, 3f, 40f), new Vector3(30f, 6f, 0.5f));
            yield return Calentar();
            Animator a = Anim(out VisualPersonaje visual);
            Assert.IsTrue(jugador.GetComponent<ArmasAPie>().Equipar(ArmaAPie.Pistola));
            yield return Pulsar(teclado.leftCtrlKey);
            yield return Esperar(0.6f);
            Press(raton.rightButton);
            yield return Esperar(1.2f);
            Assert.AreEqual(0f, visual.PesoCapaBrazos, 0.05f, "Apuntando los brazos no quedan sueltos");
            Assert.Greater(visual.PesoCapaArriba, 0.9f, "Apuntando agachado los brazos usan la pose de apuntar");
            Assert.AreEqual(TipoArmaVisual.Pistola, visual.TipoArmaAnimador);
            Transform arma = visual.GetComponentInChildren<ApuntadoIK>(true).SocketArma.Find("Pistola");
            Camera cam = jugador.GetComponentInChildren<CamaraHombro>().GetComponent<Camera>();
            Vector3 punto = cam.transform.position + cam.transform.forward * 30f;
            Assert.Less(Vector3.Angle(arma.forward, punto - arma.position), 6f, "Agachado el cañón apunta al punto de mira");
            Fotos("agachado-apuntando");
            Release(raton.rightButton);
        }

        [UnityTest]
        public IEnumerator Rodada_Lenta_SinHundirseEnElPiso_YTerminaAgachado()
        {
            yield return Calentar();
            Animator a = Anim(out VisualPersonaje visual);
            CubiertaYRodada cubierta = jugador.GetComponent<CubiertaYRodada>();
            Assert.GreaterOrEqual(cubierta.Datos.DuracionRodada, 0.7f, "La rodada es más lenta (s)");
            yield return Pulsar(teclado.leftCtrlKey);
            yield return Esperar(0.8f);
            Press(teclado.wKey);
            yield return Esperar(0.4f);
            Vector3 inicio = jugador.transform.position;
            yield return Pulsar(teclado.spaceKey);
            Assert.IsTrue(cubierta.Rodando, "Agachado el espacio rueda");
            float masBajo = float.MaxValue;
            float maximoGiro = 0f;
            int foto = 0;
            float t0 = Time.time;
            Transform modelo = a.transform;
            while (cubierta.Rodando && Time.time - t0 < 3f)
            {
                yield return null;
                masBajo = Mathf.Min(masBajo, PuntoMasBajo(a));
                maximoGiro = Mathf.Max(maximoGiro, Quaternion.Angle(Quaternion.identity, modelo.localRotation));
                float avance = (Time.time - t0) / cubierta.Datos.DuracionRodada;
                if (foto < 3 && avance > 0.2f + foto * 0.25f)
                {
                    foto++;
                    Fotos("rodada-" + foto);
                }
            }
            float duracion = Time.time - t0;
            Release(teclado.wKey);
            Assert.GreaterOrEqual(duracion, 0.6f, "La rodada dura (s): " + duracion);
            Assert.Greater(maximoGiro, 90f, "El muñeco gira al rodar (°)");
            Assert.Greater(masBajo, -0.12f, "Rodando no se hunde en el piso (punto más bajo " + masBajo + " m)");
            Assert.Greater((jugador.transform.position - inicio).magnitude, cubierta.Datos.DistanciaRodada * 0.5f, "Se desplazó");
            yield return Esperar(0.8f);
            Assert.IsTrue(cubierta.AgachadoLibre, "Termina agachado");
            Assert.AreEqual(0f, Quaternion.Angle(Quaternion.identity, modelo.localRotation), 3f, "El muñeco termina derecho");
            Assert.AreEqual(1f, modelo.localScale.y / modelo.localScale.x, 0.05f, "Recupera su forma");
            Assert.Less(Alto(a, HumanBodyBones.Head), 1.45f, "Reacomodado agachado (cabeza " + Alto(a, HumanBodyBones.Head) + " m)");
            Assert.IsTrue(Estado(a, 0) == "AgachadoCaminar" || Estado(a, 0) == "AgachadoQuieto", "Termina en un estado agachado: " + Estado(a, 0));
            Fotos("rodada-final");
        }
    }
}
