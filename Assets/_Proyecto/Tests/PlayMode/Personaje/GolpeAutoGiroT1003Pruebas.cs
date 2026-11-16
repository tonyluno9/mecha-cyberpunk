using System.Collections;
using MechaCyberpunk.Personaje.Pruebas;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>T-1003: al golpear, el cuerpo se gira hacia el oponente cercano al frente y lo golpea.</summary>
    public class GolpeAutoGiroT1003Pruebas : BaseJugadorAPie
    {
        private GameObject muneco;
        private MunecoAPie vidaMuneco;

        public override void TearDown()
        {
            if (muneco != null) Object.Destroy(muneco);
            base.TearDown();
        }

        private void PonerMuneco(float distancia, float grados)
        {
            muneco = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            Vector3 dir = Quaternion.Euler(0f, grados, 0f) * Vector3.forward;
            muneco.transform.position = jugador.transform.position + dir * distancia + Vector3.up * 0.9f;
            muneco.transform.localScale = new Vector3(0.7f, 0.9f, 0.7f);
            vidaMuneco = muneco.AddComponent<MunecoAPie>();
            Physics.SyncTransforms();
        }

        private IEnumerator Golpear()
        {
            Press(raton.leftButton);
            yield return null;
            Release(raton.leftButton);
            yield return Esperar(0.4f);
        }

        [UnityTest]
        public IEnumerator Muneco_A1punto5mY50Grados_SeGiraYRecibeDanio()
        {
            yield return Calentar();
            PonerMuneco(1.5f, 50f);
            yield return null;
            float antes = vidaMuneco.Actual;
            float yawAntes = jugador.transform.eulerAngles.y;
            yield return Golpear();
            Assert.Less(vidaMuneco.Actual, antes, "el golpe conecta aunque el muñeco no estaba justo al frente");
            Assert.AreEqual(50f, Mathf.DeltaAngle(yawAntes, jugador.transform.eulerAngles.y), 1f, "el cuerpo quedó mirando al muñeco");
        }

        [UnityTest]
        public IEnumerator Muneco_A6m_NoHaceGirar()
        {
            yield return Calentar();
            PonerMuneco(6f, 20f);
            yield return null;
            float yawAntes = jugador.transform.eulerAngles.y;
            yield return Golpear();
            Assert.AreEqual(0f, Mathf.DeltaAngle(yawAntes, jugador.transform.eulerAngles.y), 0.01f);
        }

        [UnityTest]
        public IEnumerator Muneco_Detras_NoHaceGirar()
        {
            yield return Calentar();
            PonerMuneco(1.5f, 180f);
            yield return null;
            float yawAntes = jugador.transform.eulerAngles.y;
            float antes = vidaMuneco.Actual;
            yield return Golpear();
            Assert.AreEqual(0f, Mathf.DeltaAngle(yawAntes, jugador.transform.eulerAngles.y), 0.01f);
            Assert.AreEqual(antes, vidaMuneco.Actual, "no se le pega al que está detrás");
        }

        [UnityTest]
        public IEnumerator Muneco_ConMuroEnMedio_NoHaceGirar()
        {
            yield return Calentar();
            PonerMuneco(2f, 30f);
            GameObject muro = GameObject.CreatePrimitive(PrimitiveType.Cube);
            muro.transform.position = jugador.transform.position + Quaternion.Euler(0f, 30f, 0f) * Vector3.forward * 1f + Vector3.up;
            muro.transform.localScale = new Vector3(2f, 3f, 0.2f);
            muro.transform.rotation = Quaternion.Euler(0f, 30f, 0f);
            Physics.SyncTransforms();
            yield return null;
            float yawAntes = jugador.transform.eulerAngles.y;
            yield return Golpear();
            Object.Destroy(muro);
            Assert.AreEqual(0f, Mathf.DeltaAngle(yawAntes, jugador.transform.eulerAngles.y), 0.01f);
        }
    }
}
