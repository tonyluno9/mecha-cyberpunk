using System.Collections;
using MechaCyberpunk.VR;
using MechaCyberpunk.VR.Manos;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.VR
{
    /// <summary>
    /// T87: en VR las manos MN01 solo muestran la mano con el guante. El antebrazo (Forearm_*) se encoge a ~0 con su origen a unos cm detrás de la
    /// muñeca y la mano se contra-escala: los dedos quedan exactamente donde estaban, las poses siguen funcionando y la malla ya no llega al codo.
    /// </summary>
    public class T87AntebrazoOcultoPruebas : BaseJugadorVR
    {
        private const float ToleranciaPosicion = 0.001f;
        private const float ToleranciaAngulo = 0.5f;

        [Test]
        public void ColapsoAntebrazo_LaManoQuedaIgualEnElMundo()
        {
            Vector3 pos = new Vector3(0f, 0.32f, -0.11f);
            Quaternion rot = Quaternion.Euler(20f, 35f, -10f);
            Vector3 posMano = new Vector3(0f, 0.34f, 0f);
            float s = ReglaManosMN01VR.EscalaAntebrazoOculto;
            ReglaManosMN01VR.ColapsoAntebrazo(pos, rot, posMano, ReglaManosMN01VR.RetrocesoCorteAntebrazo, s, out Vector3 nuevoAntebrazo,
                out Vector3 nuevaMano);
            Vector3 manoAntes = pos + rot * posMano;
            Vector3 manoDespues = nuevoAntebrazo + rot * (nuevaMano * s);
            Assert.Less(Vector3.Distance(manoAntes, manoDespues), 1e-4f, "La muñeca no se mueve.");
            Vector3 corte = nuevoAntebrazo;
            Assert.AreEqual(ReglaManosMN01VR.RetrocesoCorteAntebrazo, Vector3.Distance(corte, manoAntes), 1e-4f, "El corte queda unos cm detrás de la muñeca.");
            Assert.Greater(Vector3.Distance(corte, pos), 0.25f, "El corte no queda en el codo.");
        }

        [UnityTest]
        public IEnumerator ManoSuelta_OcultarAntebrazo_NoMueveDedosYLasPosesSiguen()
        {
            GameObject prefab = Resources.Load<GameObject>(PunosVisiblesVR.RecursoManoDerecha);
            Assert.IsNotNull(prefab, "Falta el prefab de la mano derecha (corre ImportarManosVR40.Aplicar).");
            GameObject objeto = Object.Instantiate(prefab);
            extras.Add(objeto);
            objeto.transform.SetPositionAndRotation(new Vector3(0.3f, 1.2f, 0.5f), Quaternion.Euler(10f, 40f, 5f));
            ManoMN01VR mano = objeto.GetComponent<ManoMN01VR>();
            Assert.IsNotNull(mano);
            Assert.IsTrue(mano.Valida);
            mano.PrepararMallas(objeto.layer);
            Transform antebrazo = mano.Hueso(ReglaManosMN01VR.IndiceAntebrazo);
            Assert.AreEqual("Forearm_R", antebrazo.name);
            Assert.AreSame(antebrazo, mano.Hueso(ReglaManosMN01VR.IndiceMano).parent, "La mano cuelga del antebrazo.");

            // Referencia: antebrazo visible, puño cerrado.
            mano.FijarOcultarAntebrazo(false);
            mano.PonerPose(PoseManoVR.Libre, 1f, 0f);
            Assert.IsFalse(mano.AntebrazoOculto);
            int n = mano.CantidadHuesos;
            var posiciones = new Vector3[n];
            var rotaciones = new Quaternion[n];
            for (int i = 0; i < n; i++)
            {
                posiciones[i] = mano.Hueso(i).position;
                rotaciones[i] = mano.Hueso(i).rotation;
            }
            Vector3 codo = antebrazo.position;
            Assert.AreEqual(1f, antebrazo.lossyScale.x, 0.01f);

            // Oculto: misma pose de la mano y los dedos en el mundo.
            mano.FijarOcultarAntebrazo(true);
            mano.PonerPose(PoseManoVR.Libre, 1f, 0f);
            Assert.IsTrue(mano.AntebrazoOculto);
            Assert.Less(antebrazo.lossyScale.x, 0.01f, "El antebrazo queda encogido.");
            Assert.Greater(Vector3.Distance(antebrazo.position, codo), 0.25f, "El punto de corte va junto a la muñeca, no en el codo.");
            for (int i = ReglaManosMN01VR.IndiceMano; i < n; i++)
            {
                Assert.Less(Vector3.Distance(mano.Hueso(i).position, posiciones[i]), ToleranciaPosicion, "No se mueve " + mano.Hueso(i).name);
                Assert.Less(Quaternion.Angle(mano.Hueso(i).rotation, rotaciones[i]), ToleranciaAngulo, "No gira " + mano.Hueso(i).name);
                Assert.AreEqual(1f, mano.Hueso(i).lossyScale.x, 0.01f, "Escala normal en " + mano.Hueso(i).name);
            }

            // La mezcla sigue funcionando con el antebrazo oculto: de abierta a puño en varios cuadros.
            mano.PonerPose(PoseManoVR.Libre, 0f, 0f);
            for (int k = 0; k < 40; k++)
            {
                mano.Aplicar(PoseManoVR.Libre, 1f, 0f, 0.3f);
            }
            for (int i = ReglaManosMN01VR.IndiceMano; i < n; i++)
            {
                Assert.Less(Vector3.Distance(mano.Hueso(i).position, posiciones[i]), ToleranciaPosicion, "Tras la mezcla, " + mano.Hueso(i).name);
            }
            Assert.Less(antebrazo.lossyScale.x, 0.01f);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ArmarVR_ManosMN01_LaMallaYaNoLlegaAlCodo()
        {
            yield return ArmarVR();
            PunosVisiblesVR punos = jugador.GetComponent<PunosVisiblesVR>();
            Assert.IsNotNull(punos);
            for (int i = 0; i < 60 && !punos.UsaManosMN01; i++)
            {
                yield return null;
            }
            Assert.IsTrue(punos.UsaManosMN01, "Las manos visibles son las MN01.");
            for (int lado = 0; lado < 2; lado++)
            {
                ManoMN01VR mano = punos.ManoMN01(lado == 1);
                Assert.IsTrue(mano.AntebrazoOculto, "En VR el antebrazo va oculto por defecto.");
                SkinnedMeshRenderer malla = mano.GetComponentInChildren<SkinnedMeshRenderer>(true);
                Assert.IsNotNull(malla);
                Transform antebrazo = mano.Hueso(ReglaManosMN01VR.IndiceAntebrazo);
                Transform muneca = mano.Hueso(ReglaManosMN01VR.IndiceMano);

                mano.FijarOcultarAntebrazo(false);
                yield return Segundos(0.2f);
                Vector3 codo = antebrazo.position;
                float largo = Vector3.Distance(codo, muneca.position);
                float conAntebrazo = Mathf.Sqrt(malla.bounds.SqrDistance(codo));

                mano.FijarOcultarAntebrazo(true);
                yield return Segundos(0.2f);
                codo = muneca.position + (antebrazo.position - muneca.position).normalized * largo;
                float sinAntebrazo = Mathf.Sqrt(malla.bounds.SqrDistance(codo));
                Assert.Less(conAntebrazo, 0.1f, "Con el antebrazo visible la malla llega cerca del codo.");
                Assert.Greater(sinAntebrazo, 0.15f, "Con el antebrazo oculto la malla termina en la muñeca (bounds a " + sinAntebrazo + " m del codo).");
            }
        }
    }
}
