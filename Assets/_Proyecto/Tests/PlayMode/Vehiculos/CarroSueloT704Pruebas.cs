using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.Vehiculos.Carro;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Vehiculos
{
    /// <summary>T-704: un carro estacionado que flota se baja hasta el suelo con un raycast (al nacer y al estacionar), y uno que ya toca no se mueve.</summary>
    public class CarroSueloT704Pruebas
    {
        private static readonly string[] Codigos = { "V01a", "V02", "V05", "V08", "V09", "V10" };

        private GameObject piso;
        private GameObject carroObjeto;

        [SetUp]
        public void Preparar()
        {
            Registros.LimpiarTodos();
            piso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piso.transform.position = new Vector3(0f, -0.5f, 0f);
            piso.transform.localScale = new Vector3(400f, 1f, 400f);
            Physics.SyncTransforms();
        }

        [TearDown]
        public void Limpiar()
        {
            Object.DestroyImmediate(carroObjeto);
            Object.DestroyImmediate(piso);
            Registros.LimpiarTodos();
        }

        private static float MinY(GameObject carro)
        {
            float menor = float.MaxValue;
            foreach (Renderer r in carro.GetComponentsInChildren<Renderer>())
            {
                if ((r is MeshRenderer || r is SkinnedMeshRenderer) && r.enabled)
                {
                    menor = Mathf.Min(menor, r.bounds.min.y);
                }
            }
            return menor;
        }

        [UnityTest]
        public IEnumerator UnCarroQueFlota_AlNacerSeBajaAlSuelo()
        {
            foreach (string codigo in Codigos)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Proyecto/Vehiculos/Prefabs/Carro_" + codigo + ".prefab");
                Assert.IsNotNull(prefab, codigo);
                carroObjeto = Object.Instantiate(prefab, new Vector3(0f, 1.6f, 0f), Quaternion.identity);
                carroObjeto.GetComponent<Rigidbody>().isKinematic = true; // se queda en el aire: así se ve si el rayo lo baja
                Assert.Greater(MinY(carroObjeto), 1f, codigo + ": empieza flotando");
                yield return null;
                yield return null;
                Assert.AreEqual(0f, MinY(carroObjeto), CarroEstacionado.ToleranciaFlotar + 0.02f, codigo + ": las ruedas tocan el suelo");
                Object.DestroyImmediate(carroObjeto);
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator UnCarroQueYaToca_NoSeMueve()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Proyecto/Vehiculos/Prefabs/Carro_V09.prefab");
            carroObjeto = Object.Instantiate(prefab, new Vector3(0f, 1.6f, 0f), Quaternion.identity);
            carroObjeto.GetComponent<Rigidbody>().isKinematic = true;
            yield return null;
            yield return null;
            CarroEstacionado estacionado = carroObjeto.GetComponent<CarroEstacionado>();
            Vector3 antes = carroObjeto.transform.position;
            Assert.IsFalse(estacionado.PegarAlSuelo(), "Ya toca: no hay nada que bajar");
            Assert.AreEqual(antes, carroObjeto.transform.position);
            Assert.LessOrEqual(estacionado.MedirFlotacion(), CarroEstacionado.ToleranciaFlotar);
        }

        [UnityTest]
        public IEnumerator AlEstacionar_UnCarroQueQuedoFlotandoSeBaja()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Proyecto/Vehiculos/Prefabs/Carro_V10.prefab");
            carroObjeto = Object.Instantiate(prefab, new Vector3(0f, 0.3f, 0f), Quaternion.identity);
            Rigidbody cuerpo = carroObjeto.GetComponent<Rigidbody>();
            cuerpo.isKinematic = true;
            yield return null;
            yield return null;
            CarroEstacionado estacionado = carroObjeto.GetComponent<CarroEstacionado>();
            CarroViejo carro = carroObjeto.GetComponent<CarroViejo>();
            // El jugador se sube y se baja; el carro queda flotando (lo alzamos a mano) y a los 1.5 s de estacionar se vuelve a pegar.
            Assert.IsTrue(carro.IntentarSubir(null), "El jugador sube");
            Assert.IsTrue(carro.IntentarBajar(out _), "Y baja");
            carroObjeto.transform.position += Vector3.up * 1.2f;
            Physics.SyncTransforms();
            Assert.Greater(estacionado.MedirFlotacion(), 1f, "Quedó flotando");
            float fin = Time.time + 2.5f;
            while (Time.time < fin)
            {
                yield return null;
            }
            Assert.LessOrEqual(estacionado.MedirFlotacion(), CarroEstacionado.ToleranciaFlotar + 0.02f, "Al estacionar se pegó al suelo");
        }
    }
}
