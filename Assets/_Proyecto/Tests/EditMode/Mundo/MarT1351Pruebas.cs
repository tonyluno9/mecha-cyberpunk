using System.Collections.Generic;
using MechaCyberpunk.Mundo.Editor;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>T-1351: el shader del mar compila, el material lo usa, y el plano queda en la capa Water con el tamaño pedido.</summary>
    public class MarT1351Pruebas
    {
        private readonly List<GameObject> creados = new List<GameObject>();

        [TearDown]
        public void Limpiar()
        {
            foreach (GameObject objeto in creados)
            {
                if (objeto != null)
                {
                    Object.DestroyImmediate(objeto);
                }
            }
            creados.Clear();
        }

        [Test]
        public void Shader_Existe_YEsSoportado()
        {
            Shader shader = Shader.Find(CreadorMarT1351.NombreShader);
            Assert.IsNotNull(shader, "No existe el shader MechaCyberpunk/Mar");
            Assert.IsTrue(shader.isSupported, "El shader del mar no compila o no es soportado");
        }

        [Test]
        public void Material_UsaElShaderDelMar()
        {
            Material material = CreadorMarT1351.ObtenerMaterialMar();
            Assert.IsNotNull(material);
            Assert.AreEqual(CreadorMarT1351.NombreShader, material.shader.name);
            Assert.AreEqual(0f, material.GetFloat("_SoloEspuma"));
        }

        [Test]
        public void PlanoMar_EstaEnCapaWater_SinColliderYConElTamanoPedido()
        {
            GameObject padre = new GameObject("PadreMar");
            creados.Add(padre);
            GameObject mar = CreadorMarT1351.CrearPlanoMar(padre.transform, new Vector3(10f, 0f, -20f), new Vector2(200f, 120f), -1.5f);
            int capa = LayerMask.NameToLayer("Water");
            Assert.AreEqual(capa >= 0 ? capa : 4, mar.layer);
            Assert.IsNull(mar.GetComponent<Collider>());
            Assert.AreSame(padre.transform, mar.transform.parent);
            Assert.AreEqual(-1.5f, mar.transform.position.y, 1e-4f);
            Bounds caja = mar.GetComponent<MeshFilter>().sharedMesh.bounds;
            Assert.AreEqual(200f, caja.size.x, 1e-2f);
            Assert.AreEqual(120f, caja.size.z, 1e-2f);
            Assert.AreEqual(CreadorMarT1351.NombreShader, mar.GetComponent<MeshRenderer>().sharedMaterial.shader.name);
        }

        [Test]
        public void Orilla_CreaFranjaEnCapaWaterConMaterialDeEspuma()
        {
            GameObject padre = new GameObject("PadreOrilla");
            creados.Add(padre);
            List<Vector3> linea = new List<Vector3> { new Vector3(0, 0, 0), new Vector3(10, 0, 0), new Vector3(20, 0, 5) };
            GameObject orilla = CreadorMarT1351.CrearOrilla(padre.transform, linea, 0f, 2f);
            Assert.IsNotNull(orilla);
            Mesh malla = orilla.GetComponent<MeshFilter>().sharedMesh;
            Assert.AreEqual(6, malla.vertexCount);
            Assert.AreEqual(1f, orilla.GetComponent<MeshRenderer>().sharedMaterial.GetFloat("_SoloEspuma"));
        }
    }
}
