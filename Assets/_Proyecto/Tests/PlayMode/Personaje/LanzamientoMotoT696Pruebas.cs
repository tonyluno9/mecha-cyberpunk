using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Personaje.Visual;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>T-696: un choque fuerte en moto avienta al conductor, cae, pierde menos vida que un disparo y se levanta.</summary>
    public class LanzamientoMotoT696Pruebas : BaseJugadorAPie
    {
        private GameObject vehiculo;

        public override void TearDown()
        {
            Object.Destroy(vehiculo);
            base.TearDown();
        }

        [UnityTest]
        public IEnumerator ChoqueRapidoEnMoto_AvientaAlConductorQueCaeYSeLevanta()
        {
            yield return Calentar();
            VisualPersonaje visual = jugador.GetComponentInChildren<VisualPersonaje>(true);
            visual.Animador.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            vehiculo = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Proyecto/Vehiculos/Prefabs/Carro_V08.prefab"), new Vector3(0f, 0.2f, 0f), Quaternion.identity);
            yield return Esperar(2f);
            IMontable montable = null;
            foreach (IMontable m in Registro<IMontable>.Todos) { if (m is Component c && c.gameObject == vehiculo) montable = m; }
            Assert.IsNotNull(montable);
            jugadorAPie.Colocar(new Pose(montable.PuntoSubida + new Vector3(-0.6f, 0.05f, 0f), Quaternion.LookRotation(Vector3.right)));
            yield return Esperar(0.3f);
            Assert.IsTrue(jugadorAPie.Subir(montable));
            yield return Esperar(1f);
            IVida vida = jugador.GetComponent<IVida>();
            float vidaAntes = vida.Actual;
            Caja(new Vector3(0f, 1f, 40f), new Vector3(10f, 2f, 1f));
            Press(teclado.wKey);
            float fin = Time.time + 12f;
            while (jugadorAPie.Modo == ModoJugador.Carro && Time.time < fin) yield return null;
            Release(teclado.wKey);
            Assert.AreEqual(ModoJugador.APie, jugadorAPie.Modo, "El choque lo saca de la moto");
            LanzamientoMoto lanzamiento = jugador.GetComponent<LanzamientoMoto>();
            Assert.IsTrue(lanzamiento.Activo, "Sale volando y cae");
            yield return Esperar(0.5f);
            Assert.IsTrue(visual.Caido, "Cae con la caída que ya existe");
            Assert.Less(vidaAntes - vida.Actual, 25f, "Menos daño que un disparo");
            Assert.Greater(vida.Actual, 0f);
            fin = Time.time + 6f;
            while (lanzamiento.Activo && Time.time < fin) yield return null;
            Assert.IsFalse(lanzamiento.Activo);
            Assert.IsFalse(visual.Caido, "Se levanta");
        }
    }
}
