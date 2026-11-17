using System.Linq;
using MechaCyberpunk.Arte.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>T-1318: pretiles saltables, sin colliders invisibles de borde y azoteas con el material nuevo.</summary>
    public class TechosT1318Pruebas
    {
        private Scene escena;

        [OneTimeSetUp]
        public void Abrir()
        {
            escena = EditorSceneManager.OpenScene("Assets/_Proyecto/Escenas/Ciudad_Prologo.unity", OpenSceneMode.Additive);
            Physics.SyncTransforms();
        }

        [OneTimeTearDown]
        public void Cerrar() => EditorSceneManager.CloseScene(escena, true);

        private Transform[] Todos() => escena.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).ToArray();

        private static float AlturaSalto()
        {
            ScriptableObject datos = AssetDatabase.LoadAssetAtPath<ScriptableObject>("Assets/_Proyecto/Personaje/Datos/DatosPersonaje_Base.asset");
            Assert.IsNotNull(datos, "Falta DatosPersonaje_Base");
            return new SerializedObject(datos).FindProperty("alturaSalto").floatValue;
        }

        [Test]
        public void Pretiles_ColliderNoPasaDeLoQueSeSalta()
        {
            float salto = AlturaSalto();
            Transform[] pretiles = Todos().Where(t => t.name == ColocarT1318Techos.NombrePretil).ToArray();
            Assert.Greater(pretiles.Length, 400, "Debe haber pretiles en la ciudad");
            foreach (Transform p in pretiles)
            {
                foreach (Collider c in p.GetComponents<Collider>())
                {
                    Assert.LessOrEqual(ColocarT1318Techos.Limites(c).size.y, salto - 0.049f, ColocarS2.Ruta(p) + " choca a " + ColocarT1318Techos.Limites(c).size.y + " m y el salto es " + salto);
                }
            }
        }

        [Test]
        public void Pretiles_ElPrefabTieneElColliderBajo()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ColocarT1318Techos.RutaPrefabPretil);
            BoxCollider c = prefab.GetComponent<BoxCollider>();
            Assert.LessOrEqual(c.size.y, AlturaSalto() - 0.049f);
            Assert.GreaterOrEqual(prefab.GetComponentInChildren<MeshRenderer>(true).bounds.size.y, 0.9f, "La malla visible no se toca");
        }

        [Test]
        public void Techos_SinCollidersInvisiblesDeBordeNiSobreLaMalla()
        {
            Assert.IsEmpty(ColocarT1318Techos.InvisiblesDeBorde().Where(c => c.gameObject.scene == escena).Select(c => ColocarS2.Ruta(c.transform)).ToArray(), "Colliders invisibles de muro o borde");
            // Ningún collider de azotea sube por encima de su malla visible.
            foreach (BoxCollider c in Todos().Select(t => t.GetComponent<BoxCollider>()).Where(ColocarT1318Techos.EsAzotea))
            {
                Assert.LessOrEqual(ColocarT1318Techos.Limites(c).max.y, c.GetComponent<Renderer>().bounds.max.y + 0.03f, ColocarS2.Ruta(c.transform));
            }
        }

        [Test]
        public void Azoteas_TienenLaLosaConElMaterialNuevo()
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(KitS2c.Carpeta + "/S2c_Azotea.mat");
            Assert.IsNotNull(mat);
            BoxCollider[] cuerpos = Todos().Select(t => t.GetComponent<BoxCollider>()).Where(ColocarT1318Techos.EsAzotea).ToArray();
            Assert.Greater(cuerpos.Length, 80);
            foreach (BoxCollider c in cuerpos)
            {
                Transform losa = c.transform.parent.Cast<Transform>().FirstOrDefault(h => h.name == ColocarT1318Techos.NombreLosa && Mathf.Abs(h.position.x - ColocarT1318Techos.Limites(c).center.x) < 0.5f && Mathf.Abs(h.position.z - ColocarT1318Techos.Limites(c).center.z) < 0.5f);
                Assert.IsNotNull(losa, "Sin losa de azotea en " + ColocarS2.Ruta(c.transform) + " b " + ColocarT1318Techos.Limites(c) + " rb " + c.GetComponent<Renderer>().bounds + " esc " + c.transform.lossyScale + " hijos " + string.Join(",", c.transform.parent.Cast<Transform>().Select(h => h.name + h.position)));
                Assert.AreEqual(mat, losa.GetComponent<MeshRenderer>().sharedMaterial);
                Assert.IsNull(losa.GetComponent<Collider>(), "La losa no lleva collider");
                Assert.GreaterOrEqual(losa.position.y, ColocarT1318Techos.Limites(c).max.y, "La losa va sobre la cara de arriba");
            }
        }
    }
}
