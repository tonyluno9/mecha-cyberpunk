using System.Collections;
using System.Collections.Generic;
using MechaCyberpunk.Personaje.Visual;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>
    /// T-1550 con entrada simulada (decisión de Tony del 20-nov): el salto empieza con la agachada de 3 cuadros en el piso y despega 0.1 s después de pedirlo; parado usa
    /// SaltoInicio, SaltoAire y SaltoAterrizaje; corriendo (3 m/s o más hacia adelante) usa SaltoCorrerInicio, SaltoCorrerAire y SaltoCorrerAterrizaje sin bloquear el movimiento;
    /// sin input al tocar el piso es PJ01_Salto_Aterrizaje; caer corriendo desde ~1.9 m es la rodada sin bloquear. Necesita haber corrido LocomocionT1550.Aplicar.
    /// </summary>
    public class SaltoLocomocionT1550Pruebas : BaseJugadorAPie
    {
        private const float UmbralDespegue = 0.02f;

        private VisualPersonaje Visual() => jugador.GetComponentInChildren<VisualPersonaje>(true);

        private static string EstadoActual(Animator a)
        {
            AnimatorStateInfo info = a.IsInTransition(0) ? a.GetNextAnimatorStateInfo(0) : a.GetCurrentAnimatorStateInfo(0);
            foreach (string n in ReglasAnimacion.EstadosBase)
            {
                if (info.IsName(n)) return n;
            }
            return "?";
        }

        private Animator Preparar()
        {
            Animator a = Visual().Animador;
            a.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            return a;
        }

        /// <summary>Corre el cuadro a cuadro hasta <paramref name="segundos"/> y anota los estados por los que pasa el Animator, en orden y sin repetir seguidos.</summary>
        private IEnumerator Rastrear(Animator a, float segundos, List<string> estados, List<bool> bloqueado)
        {
            float fin = Time.time + segundos;
            while (Time.time < fin)
            {
                string e = EstadoActual(a);
                if (estados.Count == 0 || estados[estados.Count - 1] != e) estados.Add(e);
                bloqueado.Add(controlador.BloqueadoPorAnimacion);
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator SaltoParado_AgachaEnElPisoYDespegaA0y1Segundos_ConLosTresEstados()
        {
            yield return Calentar();
            Animator a = Preparar();
            yield return Esperar(0.8f);
            float piso = jugador.transform.position.y;
            Press(teclado.spaceKey);
            float pedido = Time.time;
            yield return null;
            Release(teclado.spaceKey);
            float despegue = -1f;
            bool agachadoEnElPiso = false;
            var estados = new List<string>();
            float fin = Time.time + 1.4f;
            while (Time.time < fin)
            {
                float altura = jugador.transform.position.y - piso;
                string e = EstadoActual(a);
                if (estados.Count == 0 || estados[estados.Count - 1] != e) estados.Add(e);
                if (despegue < 0f && altura > UmbralDespegue) despegue = Time.time;
                if (despegue < 0f && e == "SaltoInicio") agachadoEnElPiso = true;
                yield return null;
            }
            Assert.Greater(despegue, 0f, "Despega");
            Assert.That(despegue - pedido, Is.InRange(0.08f, 0.2f), "El impulso sale ~0.1 s después de pedir el salto (3 cuadros)");
            Assert.IsTrue(agachadoEnElPiso, "La agachada (SaltoInicio) empieza en el piso, antes de despegar: " + string.Join(">", estados));
            CollectionAssert.IsSubsetOf(new[] { "SaltoInicio", "SaltoAire", "SaltoAterrizaje" }, estados, "Pasa por las tres fases parado: " + string.Join(">", estados));
            Assert.Less(estados.IndexOf("SaltoInicio"), estados.IndexOf("SaltoAire"));
            Assert.Less(estados.IndexOf("SaltoAire"), estados.IndexOf("SaltoAterrizaje"));
            Assert.IsFalse(estados.Contains("SaltoCorrerInicio"), "Parado no usa el salto corriendo");
        }

        [UnityTest]
        public IEnumerator SaltoParado_SaltoAireNoEsCiclo()
        {
            yield return Calentar();
            Animator a = Preparar();
            yield return Esperar(0.8f);
            Press(teclado.spaceKey);
            yield return null;
            Release(teclado.spaceKey);
            bool visto = false;
            float fin = Time.time + 1.2f;
            while (Time.time < fin)
            {
                foreach (AnimatorClipInfo info in a.GetCurrentAnimatorClipInfo(0))
                {
                    if (info.clip.name == "PJ01_Salto_Aire")
                    {
                        visto = true;
                        Assert.IsFalse(info.clip.isLooping, "PJ01_Salto_Aire no hace ciclo: se queda en la pose de bajada");
                    }
                }
                yield return null;
            }
            Assert.IsTrue(visto, "Se vio PJ01_Salto_Aire");
        }

        [UnityTest]
        public IEnumerator SaltoCorriendo_UsaLosClipsDeCorrer_NoBloquea_YSigueCorriendoAlAterrizar()
        {
            yield return Calentar();
            Animator a = Preparar();
            yield return Esperar(0.8f);
            Press(teclado.wKey);
            yield return null; // dos Press en el mismo cuadro: el segundo estado del teclado tapaba la W
            Press(teclado.leftShiftKey);
            yield return Esperar(1.2f);
            Assert.GreaterOrEqual(controlador.Velocidad.magnitude, ReglasAnimacion.VelocidadSaltoCorriendo, "Va corriendo (trote de Shift)");
            Press(teclado.spaceKey);
            yield return null;
            Release(teclado.spaceKey);
            var estados = new List<string>();
            var bloqueado = new List<bool>();
            yield return Rastrear(a, 1.5f, estados, bloqueado);
            string ruta = string.Join(">", estados);
            Assert.IsTrue(estados.Contains("SaltoCorrerInicio"), "Despegue corriendo: " + ruta);
            Assert.IsTrue(estados.Contains("SaltoCorrerAire"), "Aire corriendo: " + ruta);
            Assert.IsTrue(estados.Contains("SaltoCorrerAterrizaje"), "Aterrizaje corriendo: " + ruta);
            Assert.IsFalse(estados.Contains("SaltoInicio") || estados.Contains("SaltoAterrizaje"), "No mezcla el salto parado: " + ruta);
            Assert.IsFalse(bloqueado.Contains(true), "El salto corriendo no bloquea el movimiento");
            Assert.AreEqual("Caminar", EstadoActual(a), "Vuelve al trote (mezcla de Caminar) sin brinco de pose");
            Assert.GreaterOrEqual(controlador.Velocidad.magnitude, ReglasAnimacion.VelocidadSaltoCorriendo, "Sigue corriendo");
            Release(teclado.wKey);
            yield return null;
            Release(teclado.leftShiftKey);
        }

        [UnityTest]
        public IEnumerator SaltoCorriendo_SinInputAlTocarElPiso_AterrizaConSaltoAterrizaje()
        {
            yield return Calentar();
            Animator a = Preparar();
            yield return Esperar(0.8f);
            Press(teclado.wKey);
            yield return null;
            Press(teclado.leftShiftKey);
            yield return Esperar(1.2f);
            Press(teclado.spaceKey);
            yield return null;
            Release(teclado.spaceKey);
            yield return Esperar(0.12f);
            Release(teclado.wKey); // suelta el avance ya en el aire
            yield return null;
            Release(teclado.leftShiftKey);
            var estados = new List<string>();
            var bloqueado = new List<bool>();
            yield return Rastrear(a, 1.4f, estados, bloqueado);
            string ruta = string.Join(">", estados);
            Assert.IsTrue(estados.Contains("SaltoAterrizaje"), "Sin input al tocar el piso: PJ01_Salto_Aterrizaje: " + ruta);
            Assert.IsFalse(estados.Contains("SaltoCorrerAterrizaje"), "No el aterrizaje corriendo: " + ruta);
            Assert.AreEqual("Idle", EstadoActual(a));
        }

        [UnityTest]
        public IEnumerator CaerCorriendoDesdeAlto_HaceLaRodada_SinBloquearElMovimiento()
        {
            yield return Calentar();
            Animator a = Preparar();
            yield return Esperar(0.8f);
            Press(teclado.wKey);
            yield return null;
            Press(teclado.leftShiftKey);
            yield return Esperar(1.0f);
            CharacterController cc = jugador.GetComponent<CharacterController>();
            Vector3 p = jugador.transform.position;
            cc.enabled = false;
            jugador.transform.position = new Vector3(p.x, p.y + 1.9f, p.z); // ~7.8 m/s al llegar: entre 6 y 9 m/s, corriendo
            cc.enabled = true;
            var estados = new List<string>();
            var bloqueado = new List<bool>();
            yield return Rastrear(a, 2.0f, estados, bloqueado);
            string ruta = string.Join(">", estados);
            Assert.IsTrue(estados.Contains("SaltoCorrerRodada"), "Cayendo corriendo desde ~1.9 m se ve la rodada: " + ruta);
            Assert.IsFalse(estados.Contains("AterrizajeFuerte"), "No el aterrizaje fuerte de tres puntos: " + ruta);
            Assert.IsFalse(bloqueado.Contains(true), "La rodada corriendo no bloquea el movimiento");
            Release(teclado.wKey);
            yield return null;
            Release(teclado.leftShiftKey);
        }
    }
}
