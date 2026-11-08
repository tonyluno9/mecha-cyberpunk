using System.Collections;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.Personaje.Visual;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>
    /// T-734 (A4): la pistola del jugador aparece con el evento de Pistola_Sacar (0.30 s) y desaparece con el de Pistola_Guardar (0.53 s). Mide los tiempos
    /// y cubre soltar el apuntado antes de 0.30 s, disparar al instante y los visuales de aliados y enemigos. Capturas en docs/capturas/pj01-evento-*.png.
    /// </summary>
    public class EventosPistolaT734Pruebas : BaseJugadorAPie
    {
        // El evento llega en el cuadro en que el Animator pasa esos segundos, mas el cuadro en que el estado empieza: se pide +-0.12 s.
        private const float Tolerancia = 0.12f;

        private VisualPersonaje visual;
        private Animator anim;

        private IEnumerator Preparar()
        {
            yield return Calentar();
            visual = jugador.GetComponentInChildren<VisualPersonaje>(true);
            anim = visual.Animador;
            anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            Assert.IsTrue(jugador.GetComponent<ArmasAPie>().Equipar(ArmaAPie.Pistola));
            yield return Esperar(0.6f);
        }

        private GameObject Pistola() => visual.GetComponentInChildren<ApuntadoIK>(true).SocketArma.Find("Pistola").gameObject;

        private void Foto(string nombre)
        {
            CapturaS1.Foto("pj01-evento-" + nombre, jugador.transform, new Vector3(3.2f, 1.1f, 1.2f), new Vector3(0f, 0.9f, 0.2f), 40f, 700, 700);
        }

        // Segundos desde ahora hasta que la pistola cambia a <paramref name="activa"/> (con tope).
        private IEnumerator HastaPistola(bool activa, float tope, System.Action<float> resultado)
        {
            float t0 = Time.time;
            while (Pistola().activeInHierarchy != activa && Time.time - t0 < tope)
            {
                yield return null;
            }
            resultado(Pistola().activeInHierarchy == activa ? Time.time - t0 : -1f);
        }

        [UnityTest]
        public IEnumerator Sacar_ActivaLaPistolaALos030Segundos_YGuardarLaDesactivaALos053()
        {
            yield return Preparar();
            Assert.IsFalse(Pistola().activeInHierarchy);
            Press(raton.rightButton);
            float sacar = -1f;
            float t0 = Time.time;
            bool vistaAntes = false;
            while (Time.time - t0 < 0.2f)
            {
                yield return null;
                vistaAntes |= Pistola().activeInHierarchy;
            }
            Assert.IsFalse(vistaAntes, "A los 0.2 s la pistola aun no sale: la mano esta bajando a la cadera");
            Foto("0-antes-de-030");
            yield return HastaPistola(true, 1f, t => sacar = t);
            sacar += 0.2f;
            Assert.AreEqual(0.30f, sacar, Tolerancia, "La pistola aparece a los 0.30 s de Pistola_Sacar (medido " + sacar + ")");
            Assert.IsTrue(visual.ArmaEnMano);
            yield return Esperar(0.1f);
            Foto("1-despues-de-030");
            yield return Esperar(1.0f);
            Assert.IsTrue(Pistola().activeInHierarchy, "Apuntando se queda");

            Release(raton.rightButton);
            float guardar = -1f;
            yield return HastaPistola(false, 1.5f, t => guardar = t);
            Assert.AreEqual(0.53f, guardar, Tolerancia + 0.04f, "La pistola desaparece a los 0.53 s de Pistola_Guardar (medido " + guardar + ")");
            Assert.IsFalse(visual.ArmaEnMano);
            Foto("2-guardada");
        }

        [UnityTest]
        public IEnumerator SoltarElApuntadoAntesDe030_LaPistolaNuncaSale()
        {
            yield return Preparar();
            Press(raton.rightButton);
            yield return Esperar(0.12f);
            Release(raton.rightButton);
            bool vista = false;
            float t0 = Time.time;
            while (Time.time - t0 < 1.8f)
            {
                yield return null;
                vista |= Pistola().activeInHierarchy;
            }
            Assert.IsFalse(vista, "Soltar antes de 0.30 s: ni el evento de Sacar (que sigue sonando en la mezcla) ni nada la enciende");
            Assert.IsFalse(visual.ArmaEnMano);
        }

        [UnityTest]
        public IEnumerator VolverAApuntarMientrasSeGuarda_LaVuelveASacar_YNoSeQuedaApagada()
        {
            yield return Preparar();
            Press(raton.rightButton);
            yield return Esperar(1.3f);
            Assert.IsTrue(Pistola().activeInHierarchy);
            Release(raton.rightButton);
            yield return Esperar(0.25f);
            Press(raton.rightButton);
            yield return Esperar(1.4f);
            Assert.IsTrue(Pistola().activeInHierarchy, "Si se vuelve a apuntar al guardar, la pistola esta de vuelta");
            Assert.IsTrue(visual.ArmaEnMano);
            Release(raton.rightButton);
            yield return Esperar(1.6f);
            Assert.IsFalse(Pistola().activeInHierarchy);
        }

        [UnityTest]
        public IEnumerator DispararAlInstante_LaPistolaSaleSinEsperarAlEvento_YSeGuardaDespues()
        {
            yield return Preparar();
            Assert.IsFalse(Pistola().activeInHierarchy);
            Press(raton.leftButton);
            yield return null;
            Release(raton.leftButton);
            yield return null;
            yield return null;
            Assert.IsTrue(Pistola().activeInHierarchy, "Disparar sin apuntar la saca al instante (no hay 0.30 s de espera)");
            Foto("3-disparo-instante");
            yield return Esperar(VisualPersonaje.SegundosArmaTrasAccion + 0.2f);
            float guardar = -1f;
            yield return HastaPistola(false, 1.5f, t => guardar = t);
            Assert.Greater(guardar, 0f, "Pasada la espera se guarda con Pistola_Guardar");
            Assert.Less(guardar, 0.53f + Tolerancia + 0.2f);
        }

        [UnityTest]
        public IEnumerator ConOtraArma_LaPistolaNoSeToca()
        {
            yield return Preparar();
            Assert.IsTrue(jugador.GetComponent<ArmasAPie>().Equipar(ArmaAPie.Punos));
            Press(raton.rightButton);
            yield return Esperar(1.0f);
            Release(raton.rightButton);
            Assert.IsFalse(Pistola().activeInHierarchy, "Con los puños la pistola no se ve aunque se apunte");
        }

        [UnityTest]
        public IEnumerator AliadosYEnemigos_ComparteElControladorBase_YLosEventosNoDejanAvisosNiRompenNada()
        {
            yield return Calentar();
            string[] visuales = { "Visual_woman2", "Visual_man", "Visual_SkullHombre", "Visual_GuardiaNova", "Visual_Doctor", "Visual_RobotHombre" };
            string fallos = "";
            for (int i = 0; i < visuales.Length; i++)
            {
                GameObject go = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Proyecto/Personaje/Visual/Prefabs/" + visuales[i] + ".prefab"),
                    new Vector3(8f + i * 2f, 0.05f, 6f), Quaternion.identity);
                VisualPersonaje v = go.GetComponent<VisualPersonaje>();
                v.Animador.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                v.LeerDelPadre = false;
                if (v.Animador.GetComponent<ReceptorEventosArma>() == null) { fallos += visuales[i] + " sin receptor; "; }
                yield return Esperar(0.3f);
                v.TipoArma = TipoArmaVisual.Pistola;
                v.Apuntando = true;
                float t0 = Time.time;
                while (!v.ArmaEnMano && Time.time - t0 < 1f) { yield return null; }
                float sacar = Time.time - t0;
                if (!v.ArmaEnMano || Mathf.Abs(sacar - 0.30f) > Tolerancia) { fallos += visuales[i] + " sacar=" + sacar.ToString("F2") + "; "; }
                yield return Esperar(0.9f);
                v.Apuntando = false;
                v.TipoArma = TipoArmaVisual.Ninguna;
                t0 = Time.time;
                while (v.ArmaEnMano && Time.time - t0 < 1.5f) { yield return null; }
                float guardar = Time.time - t0;
                if (v.ArmaEnMano || Mathf.Abs(guardar - 0.53f) > Tolerancia + 0.04f) { fallos += visuales[i] + " guardar=" + guardar.ToString("F2") + "; "; }
                Object.Destroy(go);
            }
            Assert.AreEqual("", fallos);
            LogAssert.NoUnexpectedReceived();
        }
    }
}
