using System.Collections;
using MechaCyberpunk.VR;
using MechaCyberpunk.VR.Manos;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.VR
{
    /// <summary>VR35: al armarse el rig, cada mano lleva el puño de PJ01 (malla real con piel) y los modelos de los controles se ocultan.</summary>
    public class PunosVisiblesVRPruebas : BaseJugadorVR
    {
        [UnityTest]
        public IEnumerator ArmarVR_CadaMano_LlevaPunoDePJ01YSinControl()
        {
            yield return ArmarVR();
            PunosVisiblesVR punos = jugador.GetComponent<PunosVisiblesVR>();
            Assert.IsNotNull(punos, "PunosVisiblesVR debe agregarse solo al armarse el rig.");
            for (int i = 0; i < 60 && !punos.Listo; i++)
            {
                yield return null;
            }
            Assert.IsTrue(punos.Listo, "Los dos puños deben quedar puestos.");
            foreach (ManoVR mano in new[] { adaptador.ManoIzquierda, adaptador.ManoDerecha })
            {
                Transform puno = mano.transform.Find(PunosVisiblesVR.NombrePuno);
                Assert.IsNotNull(puno, "La mano " + mano.name + " debe tener su puño.");
                SkinnedMeshRenderer[] mallas = puno.GetComponentsInChildren<SkinnedMeshRenderer>();
                Assert.Greater(mallas.Length, 0, "El puño debe tener malla con piel.");
                bool alguna = false;
                foreach (SkinnedMeshRenderer r in mallas)
                {
                    alguna |= r.enabled && r.gameObject.activeInHierarchy && r.sharedMesh != null && r.sharedMesh.vertexCount > 0 && r.sharedMaterial != null;
                }
                Assert.IsTrue(alguna, "El puño de " + mano.name + " debe verse con una malla de verdad.");
                Transform modelo = mano.transform.Find("Modelo");
                Assert.IsTrue(modelo == null || !modelo.gameObject.activeInHierarchy, "El modelo del control de " + mano.name + " debe ocultarse.");
                // El puño queda cerca del control (no en el cuerpo).
                Assert.Less(Vector3.Distance(punos.PuntoNudillos(mano.Derecha), mano.transform.position), 0.2f, "Los nudillos deben quedar en la mano.");
            }
        }
    }
}
