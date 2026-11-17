using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.Vehiculos.Efectos;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Vehiculos
{
    /// <summary>T-1317: los barriles dentro del radio de una explosión revientan siempre, en cadena y con un retraso corto; uno fuera del radio no.</summary>
    public class BarrilesCadenaT1317Pruebas
    {
        private GameObject Barril(string nombre, Vector3 pos)
        {
            GameObject b = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            b.name = nombre;
            b.transform.position = pos;
            b.transform.localScale = new Vector3(0.7f, 0.55f, 0.7f);
            b.AddComponent<BarrilExplosivo>();
            return b;
        }

        [UnityTest]
        public IEnumerator BarrilesAMasDeDosMetros_RevientanEnCadena()
        {
            GameObject a = Barril("Barril_A1317", new Vector3(500f, 0.5f, 500f));
            GameObject b = Barril("Barril_B1317", new Vector3(503.5f, 0.5f, 500f));
            Physics.SyncTransforms();
            BarrilExplosivo ba = a.GetComponent<BarrilExplosivo>(), bb = b.GetComponent<BarrilExplosivo>();
            yield return null;

            ba.RecibirDanio(new InfoDanio(100f, TipoDanio.Disparo, a.transform.position, Vector3.forward, null));
            Assert.IsTrue(ba.Exploto, "el primero revienta con el disparo");
            Assert.IsFalse(bb.Exploto, "el segundo espera el retraso de la cadena");
            yield return new WaitForSeconds(BarrilExplosivo.RetrasoCadena + 0.4f);
            Assert.IsTrue(bb.Exploto, "el segundo, a 3.5 m, revienta en cadena");
            Object.Destroy(a);
            Object.Destroy(b);
        }

        [UnityTest]
        public IEnumerator BarrilLejos_FueraDelRadio_NoRevienta()
        {
            GameObject a = Barril("Barril_A1317b", new Vector3(600f, 0.5f, 600f));
            GameObject c = Barril("Barril_C1317b", new Vector3(608f, 0.5f, 600f));
            Physics.SyncTransforms();
            yield return null;
            a.GetComponent<BarrilExplosivo>().RecibirDanio(new InfoDanio(100f, TipoDanio.Disparo, a.transform.position, Vector3.forward, null));
            yield return new WaitForSeconds(BarrilExplosivo.RetrasoCadena + 0.4f);
            Assert.IsFalse(c.GetComponent<BarrilExplosivo>().Exploto, "a 8 m no revienta");
            Object.Destroy(a);
            Object.Destroy(c);
        }
    }
}
