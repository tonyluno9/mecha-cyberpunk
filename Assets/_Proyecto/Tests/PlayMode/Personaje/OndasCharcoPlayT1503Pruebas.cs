using System.Collections;
using MechaCyberpunk.Personaje.Jugador;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>T-1503: pisar un charco (quad sin collider, capa Water, nombre Charco*) agrega una onda a las pisadas que dibuja el shader.</summary>
    public class OndasCharcoPlayT1503Pruebas : BaseJugadorAPie
    {
        private GameObject charco;

        public override void TearDown()
        {
            if (charco != null)
            {
                Object.Destroy(charco);
            }
            base.TearDown();
        }

        [UnityTest]
        public IEnumerator Charco_CaminarSobreEl_AgregaOndas()
        {
            charco = GameObject.CreatePrimitive(PrimitiveType.Quad);
            charco.name = "Charco_T1503";
            charco.layer = 4;
            Object.DestroyImmediate(charco.GetComponent<Collider>());
            charco.transform.SetPositionAndRotation(new Vector3(0f, 0.025f, 5f), Quaternion.Euler(90f, 0f, 0f));
            charco.transform.localScale = new Vector3(20f, 30f, 1f);
            EfectosPieJugador efectos = jugador.GetComponent<EfectosPieJugador>();
            Assert.IsNotNull(efectos, "el jugador a pie lleva EfectosPieJugador");
            yield return Calentar();
            Assert.AreEqual(0, efectos.Ondas.Guardadas + efectos.Anillos.Activos, "antes de pisar no hay ondas");
            Press(teclado.wKey);
            yield return Esperar(2f);
            Release(teclado.wKey);
            Assert.Greater(efectos.Ondas.Guardadas + efectos.Anillos.Activos, 0, "pisar el charco agrega una onda (shader de agua o anillo de piso, T-1525)");
        }
    }
}
