using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.Personaje.Movimiento;
using MechaCyberpunk.Personaje.Vida;
using MechaCyberpunk.Personaje.Visual;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>T-1515: cobertura de PJ01 (quieto, asomarse al apuntar, regresar al soltar) y daño de lado, con entrada simulada y el impacto real.</summary>
    public class PendientesPJ01T1515Pruebas : BaseJugadorAPie
    {
        private VisualPersonaje visual;
        private Animator anim;
        private VidaAPie vida;

        public override void Setup()
        {
            base.Setup();
            EventosPelea.LimpiarSuscriptores();
            visual = jugador.GetComponentInChildren<VisualPersonaje>(true);
            anim = visual != null ? visual.Animador : null;
            vida = jugador.GetComponent<VidaAPie>();
        }

        public override void TearDown()
        {
            EventosPelea.LimpiarSuscriptores();
            base.TearDown();
        }

        private static bool EnEstado(Animator a, int capa, string nombre)
        {
            return a.GetCurrentAnimatorStateInfo(capa).IsName(nombre) || (a.IsInTransition(capa) && a.GetNextAnimatorStateInfo(capa).IsName(nombre));
        }

        private IEnumerator Pulsar(Key tecla)
        {
            InputSystem.QueueStateEvent(teclado, new KeyboardState(tecla));
            yield return null;
            InputSystem.QueueStateEvent(teclado, new KeyboardState());
            yield return null;
        }

        private static IEnumerator Hasta(System.Func<bool> condicion, float tope)
        {
            float fin = Time.time + tope;
            while (!condicion() && Time.time < fin)
            {
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator Cobertura_PegadoAUnaPared_JuegaQuieto_AlApuntarSeAsoma_YAlSoltarRegresa()
        {
            yield return Calentar();
            Caja(new Vector3(0f, 1f, 1.2f), new Vector3(6f, 2f, 0.4f));
            Physics.SyncTransforms();
            yield return null;
            CubiertaYRodada cubierta = jugador.GetComponent<CubiertaYRodada>();
            Assert.IsNotNull(anim, "el jugador tiene Animator");

            yield return Pulsar(Key.V);
            Assert.IsTrue(cubierta.Cubierto, "V se pega a la pared");
            yield return Hasta(() => EnEstado(anim, 0, "CoberturaMantener"), 3f);
            Assert.IsTrue(EnEstado(anim, 0, "CoberturaMantener"), "quieto en cobertura (PJ01_Cobertura_Quieto)");
            Assert.AreEqual("PJ01_Cobertura_Quieto", anim.GetCurrentAnimatorClipInfo(0).Length > 0 ? anim.GetCurrentAnimatorClipInfo(0)[0].clip.name : "");

            jugador.GetComponent<ArmasAPie>().Equipar(ArmaAPie.Pistola);
            Press(raton.rightButton);
            yield return Hasta(() => EnEstado(anim, 0, "CoberturaAsomarse"), 2f);
            Assert.IsTrue(EnEstado(anim, 0, "CoberturaAsomarse"), "al apuntar se asoma");

            Release(raton.rightButton);
            yield return Hasta(() => EnEstado(anim, 0, "CoberturaRegresar"), 2f);
            Assert.IsTrue(EnEstado(anim, 0, "CoberturaRegresar"), "al soltar regresa");
            yield return Hasta(() => EnEstado(anim, 0, "CoberturaMantener"), 3f);
            Assert.IsTrue(EnEstado(anim, 0, "CoberturaMantener"), "y vuelve a quieto");
        }

        // Un impacto como el que publica el combate. La dirección es la del golpe: hacia donde viaja la bala.
        private void Impacto(Vector3 direccion)
        {
            var info = new InfoDanio(vida.Maxima * 0.05f, TipoDanio.Disparo, jugador.transform.position + Vector3.up, direccion, null);
            EventosPelea.PublicarImpacto(null, info, new ResultadoDanio(info.Cantidad, TipoBloqueo.Ninguno, false));
        }

        private IEnumerator RunDanoDeLado(float haciaDerecha, string esperado)
        {
            yield return Calentar();
            Assert.IsNotNull(anim, "el jugador tiene Animator");
            int capa = anim.GetLayerIndex(ReglasAnimacion.CapaDano);
            Assert.GreaterOrEqual(capa, 0);
            yield return Esperar(0.5f);
            Vector3 derecha = jugador.transform.right;
            Impacto(derecha * haciaDerecha);
            yield return Hasta(() => EnEstado(anim, capa, esperado), 0.5f);
            Assert.IsTrue(EnEstado(anim, capa, esperado), "una bala que viaja hacia " + (haciaDerecha > 0 ? "la derecha" : "la izquierda") + " juega " + esperado);
            yield return Esperar(1f);
            Assert.IsTrue(EnEstado(anim, capa, "Vacio"), "termina y la capa vuelve a vacio");
        }

        [UnityTest]
        public IEnumerator Dano_BalaHaciaLaDerecha_LePegaPorLaIzquierda() => RunDanoDeLado(1f, "DanoIzquierda");

        [UnityTest]
        public IEnumerator Dano_BalaHaciaLaIzquierda_LePegaPorLaDerecha() => RunDanoDeLado(-1f, "DanoDerecha");

        [UnityTest]
        public IEnumerator Dano_DeFrente_SigueSiendoDeFrente()
        {
            yield return Calentar();
            int capa = anim.GetLayerIndex(ReglasAnimacion.CapaDano);
            yield return Esperar(0.5f);
            Impacto(-jugador.transform.forward);
            yield return Hasta(() => EnEstado(anim, capa, "DanoFrente"), 0.5f);
            Assert.IsTrue(EnEstado(anim, capa, "DanoFrente"));
        }
    }
}
