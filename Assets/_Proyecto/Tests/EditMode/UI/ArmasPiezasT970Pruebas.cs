using System.Collections.Generic;
using MechaCyberpunk.UI.Armas;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Tests.UI
{
    /// <summary>T-970: el nivel máximo por arma (la escopeta no lleva mira) y los prefabs W02-W04 con sus piezas reales de Blender.</summary>
    public class ArmasPiezasT970Pruebas
    {
        private const int SinMira = 2;
        private const float ToleranciaBoca = 0.02f;
        private const string RutaFbxViejo = "Assets/Arte/Armas/{0}/{0}.fbx";

        [Test]
        public void Estado_MaximoDos_LaMiraQuedaBloqueadaAunqueSeaElNivelSiguiente()
        {
            Assert.AreEqual(EstadoPieza.Instalada, ReglaMejoraArma.Estado(PiezaArma.Canon, 2, SinMira));
            Assert.AreEqual(EstadoPieza.Bloqueada, ReglaMejoraArma.Estado(PiezaArma.Mira, 2, SinMira));
            Assert.AreEqual(EstadoPieza.Bloqueada, ReglaMejoraArma.Estado(PiezaArma.Mira, 1, SinMira));
            Assert.AreEqual(EstadoPieza.Disponible, ReglaMejoraArma.Estado(PiezaArma.Canon, 1, SinMira));
            Assert.AreEqual(EstadoPieza.Disponible, ReglaMejoraArma.Estado(PiezaArma.Mira, 2), "Con el máximo normal la mira sigue siendo la siguiente");
        }

        [Test]
        public void HaySiguiente_MaximoDos_NoHayDespuesDelNivelDos()
        {
            Assert.IsTrue(ReglaMejoraArma.HaySiguiente(1, SinMira));
            Assert.IsFalse(ReglaMejoraArma.HaySiguiente(2, SinMira));
            Assert.IsFalse(ReglaMejoraArma.HaySiguiente(3, SinMira), "Una partida guardada con la escopeta en III se ve al máximo");
            Assert.IsFalse(ReglaMejoraArma.HaySiguiente(-1, SinMira));
            Assert.IsTrue(ReglaMejoraArma.HaySiguiente(2));
        }

        [Test]
        public void PasaFiltro_MaximoDos_NivelDosEsAlMaximoYNoMejorable()
        {
            Assert.IsTrue(ReglaMejoraArma.PasaFiltro(FiltroArmas.AlMaximo, 2, SinMira));
            Assert.IsFalse(ReglaMejoraArma.PasaFiltro(FiltroArmas.Mejorables, 2, SinMira));
            Assert.IsTrue(ReglaMejoraArma.PasaFiltro(FiltroArmas.Mejorables, 1, SinMira));
            Assert.IsTrue(ReglaMejoraArma.PasaFiltro(FiltroArmas.AlMaximo, 3, SinMira));
            Assert.IsFalse(ReglaMejoraArma.PasaFiltro(FiltroArmas.AlMaximo, 2), "Con el máximo normal el nivel II no es el máximo");
        }

        [Test]
        public void Siguiente_MaximoDos_NuncaEsLaMira()
        {
            Assert.AreEqual(PiezaArma.Canon, ReglaMejoraArma.Siguiente(1, SinMira));
            Assert.AreEqual(PiezaArma.Canon, ReglaMejoraArma.Siguiente(2, SinMira), "Al máximo se queda en la última pieza que sí existe");
            Assert.AreEqual(PiezaArma.Cargador, ReglaMejoraArma.PiezaQueFalta(PiezaArma.Mira, 0, SinMira));
        }

        [TestCase("W02_Escopeta", "Bomba")]
        [TestCase("W03_Fusil", "Cargador")]
        [TestCase("W04_Francotirador", "Cargador")]
        public void Prefab_TienePiezasRealesYLaBocaEnLaPuntaDelCanon(string carpeta, string cargador)
        {
            string ruta = "Assets/Arte/Armas/" + carpeta + "/" + carpeta + ".prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
            Assert.IsNotNull(prefab, ruta);
            Transform raiz = prefab.transform;
            Transform canon = BuscarHijo(raiz, "Canon");
            Assert.IsNotNull(BuscarHijo(raiz, "Armazon"), "Armazon");
            Assert.IsNotNull(canon, "Canon");
            Assert.IsNotNull(BuscarHijo(raiz, cargador), cargador);
            if (carpeta != "W02_Escopeta")
            {
                Assert.IsNotNull(BuscarHijo(raiz, "Mira"), "Mira");
            }
            Transform boca = BuscarHijo(raiz, "Boca");
            Assert.IsNotNull(boca, "Boca");

            float zMaxima = float.MinValue;
            List<Vector3> vertices = new List<Vector3>();
            foreach (MeshFilter f in canon.GetComponentsInChildren<MeshFilter>(true))
            {
                Matrix4x4 m = raiz.worldToLocalMatrix * f.transform.localToWorldMatrix;
                vertices.Clear();
                f.sharedMesh.GetVertices(vertices);
                foreach (Vector3 v in vertices)
                {
                    zMaxima = Mathf.Max(zMaxima, m.MultiplyPoint3x4(v).z);
                }
            }
            Vector3 bocaLocal = raiz.InverseTransformPoint(boca.position);
            Assert.Less(Mathf.Abs(bocaLocal.z - zMaxima), ToleranciaBoca, "Boca a menos de 2 cm de la punta delantera del cañón");
        }

        [TestCase("W02_Escopeta")]
        [TestCase("W03_Fusil")]
        [TestCase("W04_Francotirador")]
        public void Prefab_NingunRendererUsaElFbxViejoDeUnaPieza(string carpeta)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Arte/Armas/" + carpeta + "/" + carpeta + ".prefab");
            string fbxViejo = string.Format(RutaFbxViejo, carpeta);
            int renderers = 0;
            foreach (MeshFilter f in prefab.GetComponentsInChildren<MeshFilter>(true))
            {
                renderers++;
                Assert.IsNotNull(f.sharedMesh, f.name);
                Assert.AreNotEqual(fbxViejo, AssetDatabase.GetAssetPath(f.sharedMesh), f.name + " usa la malla vieja");
                Assert.IsTrue(AssetDatabase.GetAssetPath(f.sharedMesh).EndsWith("_Piezas.fbx"), f.name + " viene del FBX de piezas");
            }
            Assert.GreaterOrEqual(renderers, 3);
        }

        private static Transform BuscarHijo(Transform raiz, string nombre)
        {
            foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == nombre)
                {
                    return t;
                }
            }
            return null;
        }
    }
}
