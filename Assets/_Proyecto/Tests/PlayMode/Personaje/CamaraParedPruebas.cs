using System.Collections;
using MechaCyberpunk.Personaje.Movimiento;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>
    /// T-530: la cámara del hombro no se mete en la pared cuando el jugador está pegado a ella (el hombro quedaba dentro del muro
    /// y el rayo de la cámara salía de adentro).
    /// </summary>
    public class CamaraParedPruebas : BaseJugadorAPie
    {
        [UnityTest]
        public IEnumerator Camara_JugadorPegadoAMuroDerecho_NoQuedaDentroDelMuro()
        {
            // Muro a la derecha del jugador (x > 0.38), del alto de un piso.
            Caja(new Vector3(0.88f, 2f, 0f), new Vector3(1f, 4f, 12f));
            yield return Calentar();
            yield return Esperar(0.5f);
            Camera camara = jugador.GetComponentInChildren<CamaraHombro>().GetComponent<Camera>();
            Vector3 p = camara != null ? camara.transform.position : jugador.GetComponentInChildren<CamaraHombro>().transform.position;
            Assert.Less(p.x, 0.38f, "La cámara quedó dentro del muro: " + p);
            Assert.IsFalse(Physics.CheckSphere(p, 0.05f, ~0, QueryTriggerInteraction.Ignore) && p.x > 0.38f);
        }

        [UnityTest]
        public IEnumerator Camara_SinMuros_QuedaAlHombroYAtras()
        {
            yield return Calentar();
            yield return Esperar(0.5f);
            Vector3 p = jugador.GetComponentInChildren<CamaraHombro>().transform.position;
            Vector3 local = jugador.transform.InverseTransformPoint(p);
            Assert.Greater(local.x, 0.3f, "Al hombro derecho");
            Assert.Less(local.z, -2f, "Atrás del jugador");
            Assert.Greater(local.z, -3f, "Más cerca que antes (3.2 m)");
        }
    }
}
