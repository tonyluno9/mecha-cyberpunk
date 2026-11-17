using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.Vehiculos.Carro;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>T-1325: una granada destroza el carro (patrulla) de un impacto y mata a las personas cercanas; el carro que explota revienta a los vecinos en cadena y suelta sus llantas.</summary>
    public class ExplosionesT1325Pruebas
    {
        private const string RutaCarro = "Assets/_Proyecto/Vehiculos/Prefabs/CarroViejo.prefab";

        private class Policia : MonoBehaviour, IRecibeDanio
        {
            public float Total;

            public ResultadoDanio RecibirDanio(InfoDanio info)
            {
                Total += info.Cantidad;
                return new ResultadoDanio(info.Cantidad, TipoBloqueo.Ninguno, false);
            }
        }

        private GameObject piso;
        private GameObject a;
        private GameObject b;
        private GameObject lejos;

        private GameObject Carro(Vector3 pos)
        {
            return Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaCarro), pos, Quaternion.identity);
        }

        [SetUp]
        public void Preparar()
        {
            Registros.LimpiarTodos();
            piso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piso.transform.position = new Vector3(0f, -0.5f, 0f);
            piso.transform.localScale = new Vector3(200f, 1f, 200f);
            a = Carro(new Vector3(0f, 0.6f, 0f));
            b = Carro(new Vector3(3.2f, 0.6f, 0f));
            lejos = Carro(new Vector3(40f, 0.6f, 0f));
            Physics.SyncTransforms();
        }

        [TearDown]
        public void Limpiar()
        {
            Object.DestroyImmediate(a);
            Object.DestroyImmediate(b);
            Object.DestroyImmediate(lejos);
            Object.DestroyImmediate(piso);
            Registros.LimpiarTodos();
        }

        [UnityTest]
        public IEnumerator Granada_JuntoAlCarro_LoDestrozaMataAlPoliciaYExplotaAlVecino()
        {
            GameObject poli = new GameObject("Policia1325");
            poli.transform.position = new Vector3(-2f, 1f, 0f);
            poli.AddComponent<CapsuleCollider>();
            Policia p = poli.AddComponent<Policia>();
            yield return null;
            Physics.SyncTransforms();

            ExplosionAPie.Explotar(new Vector3(-1.5f, 0.5f, 0f), 5f, 150f, null);

            DanioCarro da = a.GetComponent<DanioCarro>(), db = b.GetComponent<DanioCarro>(), dl = lejos.GetComponent<DanioCarro>();
            Assert.IsTrue(da.Explotado, "La granada destroza el carro de un impacto");
            Assert.GreaterOrEqual(p.Total, ExplosionAPie.DanioLetal, "El policía cercano recibe daño letal");
            Assert.IsFalse(db.Explotado, "El vecino espera el retraso de la cadena");
            yield return new WaitForSeconds(0.15f + 0.5f);
            Assert.IsTrue(db.Explotado, "El carro de al lado explota en cadena");
            Assert.IsFalse(dl.Explotado, "El carro lejano no explota");
            Assert.IsNotNull(da.Fuego, "El carro destrozado queda ardiendo");
            Object.Destroy(poli);
        }

        [UnityTest]
        public IEnumerator CarroQueExplota_SueltaSusLlantasComoPiezas()
        {
            yield return null;
            GameObject ancla = new GameObject("Rueda_DI");
            ancla.transform.SetParent(a.transform, false);
            GameObject giro = new GameObject("Giro");
            giro.transform.SetParent(ancla.transform, false);
            GameObject modelo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            modelo.name = "Modelo";
            Object.DestroyImmediate(modelo.GetComponent<Collider>());
            modelo.transform.SetParent(giro.transform, false);
            modelo.transform.localScale = new Vector3(0.6f, 0.1f, 0.6f);
            DanioCarro da = a.GetComponent<DanioCarro>();

            da.Detonar(0f);

            Assert.IsTrue(da.Explotado);
            Assert.AreEqual(1, da.PiezasLanzadas, "La llanta se suelta como pieza");
            Assert.IsNull(modelo.transform.parent, "La pieza ya no es hija del carro");
            Assert.IsNotNull(modelo.GetComponent<Rigidbody>(), "La pieza sale con física");
            Assert.Greater(modelo.GetComponent<Rigidbody>().linearVelocity.magnitude, 1f, "Sale volando");
        }

        [Test]
        public void EsLetal_DentroDeLaFraccionDelRadio()
        {
            Assert.IsTrue(ExplosionAPie.EsLetal(2f, 5f, 0.6f));
            Assert.IsFalse(ExplosionAPie.EsLetal(4f, 5f, 0.6f));
        }
    }
}
