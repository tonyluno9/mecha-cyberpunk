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
        public IEnumerator Camara_SinMuros_QuedaCentradaAltaYAtras()
        {
            yield return Calentar();
            yield return Esperar(0.5f);
            Vector3 p = jugador.GetComponentInChildren<CamaraHombro>().transform.position;
            Vector3 local = jugador.transform.InverseTransformPoint(p);
            Assert.AreEqual(0f, local.x, 0.05f, "T-699: centrada detrás del jugador");
            Assert.Greater(local.y, 2.5f, "T-699: alta");
            Assert.Less(local.z, -6f, "Atrás del jugador (T-699: como H10)");
            Assert.Greater(local.z, -7.2f);
        }

        [UnityTest]
        public IEnumerator Camara_BajoUnTecho_NoSubePorEncimaDelTecho()
        {
            // T-699: la cámara va alta (3.4 m); en una casa con techo a 2.6 m no puede quedar fuera, arriba del techo.
            Caja(new Vector3(0f, 2.8f, 0f), new Vector3(30f, 0.4f, 30f));
            yield return Calentar();
            yield return Esperar(0.5f);
            Vector3 p = jugador.GetComponentInChildren<CamaraHombro>().transform.position;
            Assert.Less(p.y, 2.6f, "La cámara quedó por encima del techo: " + p);
        }
    }
}
