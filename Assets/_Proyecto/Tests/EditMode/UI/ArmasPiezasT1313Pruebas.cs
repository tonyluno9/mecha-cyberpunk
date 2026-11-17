using System.Collections.Generic;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.UI;
using MechaCyberpunk.UI.Armas;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Tests.UI
{
    /// <summary>T-1313: los prefabs W05-W08 con sus piezas reales de Blender, el tambor del revólver y del lanzagranadas, y los nombres de pieza del banco de Tino.</summary>
    public class ArmasPiezasT1313Pruebas
    {
        private const float ToleranciaBoca = 0.02f;
        private const float ToleranciaTambor = 0.001f;
        private const string RutaConfig = "Assets/_Proyecto/UI/Datos/ConfigMenuArmas_Base.asset";

        private static GameObject Prefab(string carpeta) => AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Arte/Armas/" + carpeta + "/" + carpeta + ".prefab");

        [TestCase("W05_Subfusil")]
        [TestCase("W06_Revolver")]
        [TestCase("W07_RiflePlasma")]
        [TestCase("W08_Lanzagranadas")]
        public void Prefab_TieneModeloConArmazonYCanon_CanonAlFrente_BocaEnLaPunta(string carpeta)
        {
            GameObject prefab = Prefab(carpeta);
            Assert.IsNotNull(prefab, carpeta);
            Transform raiz = prefab.transform;
            Transform modelo = raiz.Find("Modelo");
            Assert.IsNotNull(modelo, "Modelo");
            Assert.IsNotNull(Buscar(modelo, "Armazon"), "Armazon");
            Transform canon = Buscar(modelo, "Canon");
            Assert.IsNotNull(canon, "Canon");
            Bounds b = Limites(raiz, modelo);
            Assert.Greater(b.size.z, b.size.x, "El largo mayor va en Z (cañón a +Z)");
            Assert.Greater(b.size.z, b.size.y, "El largo mayor va en Z (cañón a +Z)");
            Assert.Greater(b.max.z, -b.min.z, "El cañón sale hacia +Z desde la empuñadura");
            Transform boca = Buscar(raiz, "Boca");
            Assert.IsNotNull(boca, "Boca");
            Bounds bc = Limites(raiz, canon);
            Assert.Less(Mathf.Abs(raiz.InverseTransformPoint(boca.position).z - bc.max.z), ToleranciaBoca, "Boca a menos de 2 cm de la punta del cañón");
            foreach (MeshFilter f in prefab.GetComponentsInChildren<MeshFilter>(true))
            {
                Assert.IsTrue(AssetDatabase.GetAssetPath(f.sharedMesh).EndsWith("_Piezas.fbx"), f.name + " viene del FBX de piezas");
            }
        }

        [TestCase("W06_Revolver")]
        [TestCase("W08_Lanzagranadas")]
        public void Prefab_TamborUnicoConPivoteEnSuEje_NoSeBambolea(string carpeta)
        {
            GameObject prefab = Prefab(carpeta);
            Transform raiz = prefab.transform;
            int cuantos = 0;
            foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true))
            {
                cuantos += t.name == TamborArma.NombreTambor ? 1 : 0;
            }
            Assert.AreEqual(1, cuantos, "Un solo objeto Tambor");
            GameObject copia = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            try
            {
                Transform tambor = Buscar(copia.transform, TamborArma.NombreTambor);
                MeshFilter filtro = tambor.GetComponent<MeshFilter>();
                Assert.IsNotNull(filtro, "El tambor tiene su malla");
                Assert.IsTrue(AssetDatabase.GetAssetPath(filtro.sharedMesh).EndsWith("_Piezas.fbx"), "El tambor viene del FBX de piezas");
                Vector3 Centro() => tambor.TransformPoint(filtro.sharedMesh.bounds.center);
                Vector3 antes = Centro();
                Quaternion reposo = tambor.localRotation;
                Vector3 eje = tambor.parent.InverseTransformDirection(copia.transform.forward).normalized;
                tambor.localRotation = Quaternion.AngleAxis(60f, eje) * reposo;
                Assert.Less(Vector3.Distance(antes, Centro()), ToleranciaTambor, "Al girar 60 grados sobre el eje del cañón el centro del tambor no se mueve (m)");
            }
            finally
            {
                Object.DestroyImmediate(copia);
            }
        }

        [TestCase(ArmaAPie.Subfusil, "Cargador", "Canon", "Mira", true)]
        [TestCase(ArmaAPie.Revolver, "Tambor", "Canon", "", false)]
        [TestCase(ArmaAPie.Lanzagranadas, "Tambor", "Canon", "", false)]
        [TestCase(ArmaAPie.RiflePlasma, "Celda", "Canon", "Mira", true)]
        public void Banco_Entrada_TieneNombresDePiezaYExistenEnElPrefab(ArmaAPie arma, string cargador, string canon, string mira, bool tieneMira)
        {
            ConfigMenuArmas config = AssetDatabase.LoadAssetAtPath<ConfigMenuArmas>(RutaConfig);
            EntradaArmaMenu entrada = null;
            foreach (EntradaArmaMenu e in config.Armas)
            {
                if (e.Arma == arma)
                {
                    entrada = e;
                }
            }
            Assert.IsNotNull(entrada, arma.ToString());
            Assert.AreEqual(cargador, entrada.NombrePieza(PiezaArma.Cargador));
            Assert.AreEqual(canon, entrada.NombrePieza(PiezaArma.Canon));
            Assert.AreEqual(mira, entrada.NombrePieza(PiezaArma.Mira));
            Assert.AreEqual(tieneMira, entrada.TieneMira);
            Assert.IsNotNull(entrada.Modelo);
            Assert.IsNotNull(Buscar(entrada.Modelo.transform, cargador), cargador + " existe en el modelo");
            Assert.IsNotNull(Buscar(entrada.Modelo.transform, canon), canon + " existe en el modelo");
            if (tieneMira)
            {
                Assert.IsNotNull(Buscar(entrada.Modelo.transform, mira), mira + " existe en el modelo");
            }
        }

        private static Bounds Limites(Transform raiz, Transform parte)
        {
            Bounds b = new Bounds();
            bool primero = true;
            List<Vector3> v = new List<Vector3>();
            foreach (MeshFilter f in parte.GetComponentsInChildren<MeshFilter>(true))
            {
                Matrix4x4 m = raiz.worldToLocalMatrix * f.transform.localToWorldMatrix;
                v.Clear();
                f.sharedMesh.GetVertices(v);
                foreach (Vector3 p in v)
                {
                    Vector3 q = m.MultiplyPoint3x4(p);
                    if (primero)
                    {
                        b = new Bounds(q, Vector3.zero);
                        primero = false;
                    }
                    else
                    {
                        b.Encapsulate(q);
                    }
                }
            }
            return b;
        }

        private static Transform Buscar(Transform raiz, string nombre)
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
