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

        // T-582: las piezas de un letrero no se cortan una por una (tubos y marco desaparecían cada una a su distancia).
        [UnityTest]
        public IEnumerator NoCortaLasPiezasDeUnLetrero()
        {
            raiz = new GameObject("RaizPrueba");
            GameObject letrero = new GameObject("Letrero_Prueba");
            letrero.transform.SetParent(raiz.transform);
            GameObject tubo = Cubo("Tubo", 1f);
            tubo.transform.SetParent(letrero.transform);
            GameObject neon = new GameObject("LetreroSinNombreDeLetrero");
            neon.transform.SetParent(raiz.transform);
            // El ParpadeoNeon sin config se queja al despertar; aquí solo importa que marca la pieza como letrero.
            UnityEngine.TestTools.LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("ParpadeoNeon en .* necesita su config"));
            neon.AddComponent<ParpadeoNeon>();
            GameObject marco = Cubo("Marco", 1f);
            marco.transform.SetParent(neon.transform);
            GameObject banca = Cubo("Banca", 1f);
            yield return null;

            CulingDecorado culing = new GameObject("CulingPrueba").AddComponent<CulingDecorado>();
            culing.transform.SetParent(raiz.transform);
            yield return null;
            culing.Aplicar();

            Assert.IsNull(tubo.GetComponent<LODGroup>(), "el tubo de un Letrero_ no se corta");
            Assert.IsNull(marco.GetComponent<LODGroup>(), "la pieza bajo un ParpadeoNeon no se corta");
            Assert.IsNotNull(banca.GetComponent<LODGroup>(), "el decorado normal sigue cortándose");
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
