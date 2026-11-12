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
    /// T-962 (A3): el jugador (PJ01) muere con los clips de Blender y su root motion mueve la cápsula: al caer el cuerpo no se separa de ella y, al levantarse
    /// (VidaAPie.Restaurar, lo que hace la clínica), vuelve a Idle con la cadera encima de la cápsula, sin deslizarse de regreso.
    /// </summary>
    public class MuerteRaizT962Pruebas : BaseJugadorAPie
    {
        private const string RutaVisual = "Assets/_Proyecto/Personaje/Visual/Prefabs/Visual_PJ01.prefab";
        private const float DanioKO = 100000f;
        private const float RecorridoMinimo = 0.4f;
        private const float RecorridoMaximo = 1.6f;
        private const float SeparacionMaxima = 0.35f;
        private VisualPersonaje visual;
        private Animator animador;

        public override void Setup()
        {
            base.Setup();
            foreach (VisualPersonaje viejo in jugador.GetComponentsInChildren<VisualPersonaje>(true))
            {
                Object.DestroyImmediate(viejo.gameObject);
            }
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaVisual);
            visual = Object.Instantiate(prefab, jugador.transform).GetComponent<VisualPersonaje>();
            animador = visual.Animador;
            animador.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            jugador.GetComponent<AnimacionJugador>().AlCambiarModelo(animador);
        }

        private bool EnBase(string nombre) => animador.GetCurrentAnimatorStateInfo(0).IsName(nombre);

        private bool EnMuerte() => EnBase("MuerteAdelante") || EnBase("MuerteAtras") || EnBase("MuerteDesmayo");

        private IEnumerator Hasta(System.Func<bool> condicion, string que, float limite)
        {
            float fin = Time.time + limite;
            while (!condicion() && Time.time < fin)
            {
                yield return null;
            }
            Assert.IsTrue(condicion(), "No llegó: " + que);
        }

        private float SeparacionCaderaCapsula()
        {
            Vector3 cadera = animador.GetBoneTransform(HumanBodyBones.Hips).position;
            Vector3 capsula = jugador.transform.position;
            return new Vector2(cadera.x - capsula.x, cadera.z - capsula.z).magnitude;
        }

        [UnityTest]
        public IEnumerator Morir_LaCapsulaSigueAlCuerpo_YAlLevantarseLaCaderaQuedaEncima()
        {
            yield return new WaitForSeconds(0.3f);
            RaizCaidaJugador raiz = animador.GetComponent<RaizCaidaJugador>();
            Assert.IsNotNull(raiz, "AnimacionJugador agrega RaizCaidaJugador al Animator");
            Vector3 antes = jugador.transform.position;
            VidaAPie vida = jugador.GetComponent<VidaAPie>();
            vida.RecibirDanio(new InfoDanio(DanioKO, TipoDanio.Disparo, jugador.transform.position + Vector3.up, Vector3.forward, null));
            yield return Hasta(EnMuerte, "estado de muerte", 3f);
            AnimatorClipInfo[] clips = animador.GetCurrentAnimatorClipInfo(0);
            StringAssert.StartsWith("PJ01_Muerte_", clips[clips.Length - 1].clip.name, "usa la muerte de Blender");
            // La muerte de Blender dura 1.3 a 1.5 s.
            yield return new WaitForSeconds(1.8f);
            Vector3 movido = jugador.transform.position - antes;
            float plano = new Vector2(movido.x, movido.z).magnitude;
            Debug.Log("[Prueba] T-962 la cápsula se movió " + plano.ToString("F2") + " m al morir; cadera a " + SeparacionCaderaCapsula().ToString("F2") + " m de la cápsula.");
            Assert.That(plano, Is.InRange(RecorridoMinimo, RecorridoMaximo), "la cápsula sigue al cuerpo al caer (m)");
            Assert.Less(SeparacionCaderaCapsula(), SeparacionMaxima + 0.5f, "el cuerpo tumbado no queda lejos de la cápsula (m)");

            vida.Restaurar();
            yield return Hasta(() => EnBase("LevantarseBocaArriba") || EnBase("LevantarseBocaAbajo"), "levantarse", 4f);
            yield return Hasta(() => EnBase("Idle"), "Idle al terminar de levantarse", 8f);
            yield return new WaitForSeconds(0.3f);
            Debug.Log("[Prueba] T-962 de pie: cadera a " + SeparacionCaderaCapsula().ToString("F2") + " m de la cápsula.");
            Assert.Less(SeparacionCaderaCapsula(), SeparacionMaxima, "de pie, la cadera está encima de la cápsula (m)");
        }
    }
}
