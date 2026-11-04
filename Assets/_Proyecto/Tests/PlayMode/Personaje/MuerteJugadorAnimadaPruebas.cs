using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Personaje.Vida;
using MechaCyberpunk.Personaje.Visual;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>
    /// T-562: el jugador muere con el clip de muerte, y al despertar (la clínica restaura la vida) sigue tumbado el retardo y pasa por
    /// el estado de levantarse antes de volver a Idle. Se usa VidaAPie.Restaurar, que es lo que llama DoctorClinica.
    /// </summary>
    public class MuerteJugadorAnimadaPruebas : BaseJugadorAPie
    {
        private const string RutaVisual = "Assets/_Proyecto/Personaje/Visual/Prefabs/Visual_woman.prefab";
        private const float DanioKO = 100000f;
        private VisualPersonaje visual;
        private Animator animador;

        public override void Setup()
        {
            base.Setup();
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaVisual);
            visual = Object.Instantiate(prefab, jugador.transform).GetComponent<VisualPersonaje>();
            animador = visual.Animador;
            animador.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            jugador.GetComponent<AnimacionJugador>().AlCambiarModelo(animador);
        }

        private bool EnBase(string nombre)
        {
            AnimatorStateInfo actual = animador.GetCurrentAnimatorStateInfo(0);
            return actual.IsName(nombre) || (animador.IsInTransition(0) && animador.GetNextAnimatorStateInfo(0).IsName(nombre));
        }

        private bool EnMuerte() => EnBase("MuerteAdelante") || EnBase("MuerteAtras") || EnBase("MuerteDesmayo");

        private bool EnLevantarse() => EnBase("LevantarseBocaArriba") || EnBase("LevantarseBocaAbajo");

        private IEnumerator Hasta(System.Func<bool> condicion, string que, float limite)
        {
            float fin = Time.time + limite;
            while (!condicion() && Time.time < fin)
            {
                yield return null;
            }
            Assert.IsTrue(condicion(), "No llegó: " + que);
        }

        [UnityTest]
        public IEnumerator Morir_CaeConElClipDeMuerte_DespiertaYSeLevantaAntesDeVolverAIdle()
        {
            yield return new WaitForSeconds(0.3f);
            VidaAPie vida = jugador.GetComponent<VidaAPie>();
            vida.RecibirDanio(new InfoDanio(DanioKO, TipoDanio.Disparo, jugador.transform.position + Vector3.up, Vector3.forward, null));
            Assert.IsTrue(vida.EstaKO);
            yield return Hasta(EnMuerte, "estado de muerte", 3f);

            vida.Restaurar();
            Assert.IsFalse(vida.EstaKO);
            yield return new WaitForSeconds(ReglasAnimacion.RetardoLevantarse * 0.5f);
            Assert.IsTrue(visual.Caido, "sigue tumbado durante el retardo");
            Assert.IsFalse(EnLevantarse(), "todavía no se levanta durante el retardo");
            yield return Hasta(EnLevantarse, "estado de levantarse", 3f);
            Assert.IsFalse(visual.Caido);
            yield return Hasta(() => EnBase("Idle"), "Idle al terminar de levantarse", 8f);
        }
    }
}
