using System.Collections;
using MechaCyberpunk.Mundo.Ambiente;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>T-425: el decorado chico recibe corte por distancia; lo grande, lo que se mueve y lo que ya tiene LOD no.</summary>
    public class CulingDecoradoPruebas
    {
        private GameObject raiz;

        [TearDown]
        public void Limpiar()
        {
            if (raiz != null)
            {
                Object.Destroy(raiz);
            }
        }

        [UnityTest]
        public IEnumerator CortaSoloElDecoradoChico()
        {
            raiz = new GameObject("RaizPrueba");
            GameObject chico = Cubo("Chico", 1f);
            GameObject grande = Cubo("Grande", 10f);
            GameObject conFisica = Cubo("ConFisica", 1f);
            conFisica.AddComponent<Rigidbody>();
            GameObject conLod = Cubo("ConLod", 1f);
            LODGroup existente = conLod.AddComponent<LODGroup>();
            existente.SetLODs(new[] { new LOD(0.01f, new Renderer[] { conLod.GetComponent<MeshRenderer>() }) });
            yield return null;

            CulingDecorado culing = new GameObject("CulingPrueba").AddComponent<CulingDecorado>();
            culing.transform.SetParent(raiz.transform);
            yield return null;
            culing.Aplicar();

            Assert.IsNotNull(chico.GetComponent<LODGroup>(), "el chico debe recibir LODGroup");
            Assert.IsNull(grande.GetComponent<LODGroup>(), "el grande no");
            Assert.IsNull(conFisica.GetComponent<LODGroup>(), "lo que tiene física no");
            Assert.AreEqual(0.01f, conLod.GetComponent<LODGroup>().GetLODs()[0].screenRelativeTransitionHeight, 0.0001f, "el LOD propio no se toca");
            float altura = chico.GetComponent<LODGroup>().GetLODs()[0].screenRelativeTransitionHeight;
            Assert.Greater(altura, 0f);
            Assert.Less(altura, 0.05f);
        }

        private GameObject Cubo(string nombre, float lado)
        {
            GameObject cubo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cubo.name = nombre;
            cubo.transform.SetParent(raiz.transform);
            cubo.transform.localScale = Vector3.one * lado;
            Object.Destroy(cubo.GetComponent<Collider>());
            return cubo;
        }
    }
}
